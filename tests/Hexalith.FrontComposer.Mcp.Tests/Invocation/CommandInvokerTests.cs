using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class CommandInvokerTests {
    [Fact]
    public async Task InvokeAsync_ValidCommand_DispatchesThroughCommandService_WithTenantContext() {
        RecordingCommandService service = new();
        CountingUlidFactory ulids = new();
        ServiceProvider provider = Services(service, ulids).BuildServiceProvider();
        FrontComposerMcpCommandInvoker invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
        Dictionary<string, JsonElement> args = Args("""{"Amount":42}""");

        using Activity activity = new("mcp-command-test");
        _ = activity.Start();

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse(result.Category.ToString());
        _ = service.Dispatched.ShouldBeOfType<PayInvoiceCommand>();
        var command = (PayInvoiceCommand)service.Dispatched!;
        command.Amount.ShouldBe(42);
        command.TenantId.ShouldBe("tenant-a");
        command.UserId.ShouldBe("agent-a");
        command.MessageId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        command.CommandId.ShouldBe(command.MessageId);
        command.CorrelationId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        command.CorrelationId.ShouldNotBe(activity.TraceId.ToString());
        ulids.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("UserId")]
    [InlineData("MessageId")]
    [InlineData("CommandId")]
    [InlineData("CorrelationId")]
    public async Task InvokeAsync_ServerControlledField_IsRejectedBeforeIdentityAllocationOrDispatch(string fieldName) {
        RecordingCommandService service = new();
        CountingUlidFactory ulids = new();
        ServiceProvider provider = Services(service, ulids).BuildServiceProvider();
        FrontComposerMcpCommandInvoker invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args($$"""{"Amount":42,"{{fieldName}}":"attacker"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
        result.Text.ShouldNotContain("attacker");
        ulids.Count.ShouldBe(0);
    }

    [Fact]
    public async Task InvokeAsync_MissingUlidFactory_FailsClosedBeforeDispatch() {
        RecordingCommandService service = new();
        ServiceProvider provider = Services(service, ulidFactory: null).BuildServiceProvider();
        FrontComposerMcpCommandInvoker invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnsupportedSchema);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithLifecycleTracker_AcknowledgementCarriesInjectedCommandHandles() {
        RecordingCommandService service = new();
        CountingUlidFactory ulids = new();
        var services = (ServiceCollection)Services(service, ulids);
        CapturingInvokerLogger logger = new();
        _ = services.AddSingleton<ILogger<FrontComposerMcpCommandInvoker>>(logger);
        _ = services.AddSingleton<ILifecycleStateService, RecordingLifecycleStateService>();
        _ = services.AddSingleton<FrontComposerMcpLifecycleTracker>();
        ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpCommandInvoker invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse(logger.LastException?.ToString() ?? result.Category.ToString());
        PayInvoiceCommand command = service.Dispatched.ShouldBeOfType<PayInvoiceCommand>();
        command.MessageId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        command.CorrelationId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        result.StructuredContent!["messageId"]!.GetValue<string>().ShouldBe(command.MessageId);
        result.StructuredContent!["correlationId"]!.GetValue<string>().ShouldBe(command.CorrelationId);
        result.StructuredContent!["lifecycle"]!["uri"]!.GetValue<string>().ShouldEndWith(command.CorrelationId);
        ulids.Count.ShouldBe(2);
    }

    private static IServiceCollection Services(ICommandService commandService, IUlidFactory? ulidFactory) {
        var services = new ServiceCollection();
        _ = services.AddSingleton(commandService);
        if (ulidFactory is not null) {
            _ = services.AddSingleton(ulidFactory);
        }

        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
        return services;
    }

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                "Pay invoice",
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId", "CommandId", "CorrelationId"]),
        ], []);

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int Amount { get; set; }
    }

    private sealed class RecordingCommandService : ICommandServiceWithLifecycle {
        public object? Dispatched { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            string messageId = ReadString(command, nameof(PayInvoiceCommand.MessageId)) ?? "message-a";
            string correlationId = ReadString(command, nameof(PayInvoiceCommand.CorrelationId)) ?? "corr-a";
            return Task.FromResult(new CommandResult(messageId, "Accepted", correlationId));
        }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            Task<CommandResult> result = DispatchAsync(command, cancellationToken);
            string messageId = ReadString(command, nameof(PayInvoiceCommand.MessageId)) ?? "message-a";
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);
            return result;
        }

        private static string? ReadString<TCommand>(TCommand command, string propertyName)
            where TCommand : class
            => command.GetType().GetProperty(propertyName)?.GetValue(command) as string;
    }

    private sealed class StaticAgentContextAccessor(string tenantId = "tenant-a", string userId = "agent-a") : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(tenantId, userId, new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class RecordingLifecycleStateService : ILifecycleStateService {
        private readonly Dictionary<string, List<Action<CommandLifecycleTransition>>> _subscribers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CommandLifecycleState> _states = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _messages = new(StringComparer.Ordinal);

        public IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition) {
            if (!_subscribers.TryGetValue(correlationId, out List<Action<CommandLifecycleTransition>>? callbacks)) {
                callbacks = [];
                _subscribers[correlationId] = callbacks;
            }

            callbacks.Add(onTransition);
            return new DisposableAction(() => callbacks.Remove(onTransition));
        }

        public CommandLifecycleState GetState(string correlationId)
            => _states.GetValueOrDefault(correlationId, CommandLifecycleState.Idle);

        public string? GetMessageId(string correlationId)
            => _messages.GetValueOrDefault(correlationId);

        public IEnumerable<string> GetActiveCorrelationIds() => _states.Keys.ToArray();

        public void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null)
            => Transition(correlationId, newState, messageId, idempotencyResolved: false);

        public void Transition(string correlationId, CommandLifecycleState newState, string? messageId, bool idempotencyResolved) {
            CommandLifecycleState previous = GetState(correlationId);
            _states[correlationId] = newState;
            if (!string.IsNullOrWhiteSpace(messageId)) {
                _messages[correlationId] = messageId;
            }

            if (!_subscribers.TryGetValue(correlationId, out List<Action<CommandLifecycleTransition>>? callbacks)) {
                return;
            }

            CommandLifecycleTransition transition = new(
                correlationId,
                previous,
                newState,
                messageId,
                DateTimeOffset.Parse("2026-06-05T00:00:00Z"),
                DateTimeOffset.Parse("2026-06-05T00:00:00Z"),
                idempotencyResolved);
            foreach (Action<CommandLifecycleTransition> callback in callbacks.ToArray()) {
                callback(transition);
            }
        }

        public void Dispose() {
            _subscribers.Clear();
            _states.Clear();
            _messages.Clear();
        }
    }

    private sealed class DisposableAction(Action dispose) : IDisposable {
        public void Dispose() => dispose();
    }

    private sealed class CapturingInvokerLogger : ILogger<FrontComposerMcpCommandInvoker> {
        public Exception? LastException { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => LastException = exception;
    }

    private sealed class CountingUlidFactory : IUlidFactory {
        private int _next;

        public int Count => _next;

        public string NewUlid() {
            int value = Interlocked.Increment(ref _next);
            return value switch {
                1 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
                2 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0D",
                _ => throw new InvalidOperationException("Unexpected ULID allocation."),
            };
        }
    }
}
