using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class McpCommandToolAdapterTests {
    [Fact]
    public void ProtocolTool_UsesDescriptorSchema_AndHidesServerControlledFields() {
        McpCommandDescriptor descriptor = Descriptor();
        FrontComposerMcpTool tool = new(descriptor);

        Tool protocol = tool.ProtocolTool;

        protocol.Name.ShouldBe("Billing.PayInvoiceCommand.Execute");
        protocol.Title.ShouldBe("Pay invoice");
        protocol.InputSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        JsonElement properties = protocol.InputSchema.GetProperty("properties");
        properties.TryGetProperty("Amount", out JsonElement amount).ShouldBeTrue();
        amount.GetProperty("type").GetString().ShouldBe("number");
        protocol.InputSchema.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ShouldBe(["Amount"]);
        properties.TryGetProperty("TenantId", out _).ShouldBeFalse();
        properties.TryGetProperty("UserId", out _).ShouldBeFalse();
        properties.TryGetProperty("MessageId", out _).ShouldBeFalse();
        properties.TryGetProperty("CommandId", out _).ShouldBeFalse();
        properties.TryGetProperty("CorrelationId", out _).ShouldBeFalse();
        properties.TryGetProperty("UnsupportedPayload", out _).ShouldBeFalse();
        tool.Metadata.Single().ShouldBeSameAs(descriptor);
    }

    [Fact]
    public async Task InvokeAsync_ValidArguments_DispatchesCommandAndReturnsProtocolAcknowledgement() {
        RecordingCommandService dispatcher = new();
        CountingUlidFactory ulids = new();
        await using ServiceProvider provider = BuildServices(dispatcher, ulids);
        FrontComposerMcpTool tool = new(Descriptor());

        CallToolResult result = await tool.InvokeAsync(
            Request(provider, Args("""{"Amount":42}""")),
            TestContext.Current.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeFalse();
        TextContentBlock text = result.Content.Single().ShouldBeOfType<TextContentBlock>();
        text.Text.ShouldBe("Command acknowledged.");
        JsonElement structured = result.StructuredContent.ShouldNotBeNull();
        structured.GetProperty("status").GetString().ShouldBe("Accepted");
        structured.GetProperty("messageId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        structured.GetProperty("correlationId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        PayInvoiceCommand command = dispatcher.Dispatched.ShouldBeOfType<PayInvoiceCommand>();
        command.Amount.ShouldBe(42);
        command.TenantId.ShouldBe("tenant-a");
        command.UserId.ShouldBe("agent-a");
        command.MessageId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        command.CommandId.ShouldBe(command.MessageId);
        command.CorrelationId.ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        ulids.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("UserId")]
    [InlineData("MessageId")]
    [InlineData("CommandId")]
    [InlineData("CorrelationId")]
    public async Task InvokeAsync_ServerControlledArgument_ReturnsValidationErrorBeforeDispatch(string fieldName) {
        RecordingCommandService dispatcher = new();
        CountingUlidFactory ulids = new();
        await using ServiceProvider provider = BuildServices(dispatcher, ulids);
        FrontComposerMcpTool tool = new(Descriptor());

        CallToolResult result = await tool.InvokeAsync(
            Request(provider, Args($$"""{"Amount":42,"{{fieldName}}":"attacker"}""")),
            TestContext.Current.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeTrue();
        TextContentBlock text = result.Content.Single().ShouldBeOfType<TextContentBlock>();
        text.Text.ShouldBe("Request failed.");
        text.Text.ShouldNotContain("attacker");
        result.StructuredContent.ShouldBeNull();
        dispatcher.Dispatched.ShouldBeNull();
        ulids.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CallToolHandler_ListTools_AppendsSingleLifecycleToolWithCanonicalSchema() {
        await using ServiceProvider provider = BuildMcpHandlerServices(new LifecycleAwareCommandService());
        using IServiceScope scope = provider.CreateScope();

        ListToolsResult result = await InvokeListToolsHandlerAsync(
            ListRequest(scope.ServiceProvider),
            TestContext.Current.CancellationToken);

        result.Tools.Select(t => t.Name).ShouldBe([
            "Billing.PayInvoiceCommand.Execute",
            "frontcomposer.lifecycle.subscribe",
        ]);
        provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>()
            .Commands.Select(d => d.ProtocolName)
            .ShouldBe(["Billing.PayInvoiceCommand.Execute"]);

        Tool lifecycle = result.Tools.Single(t => t.Name == "frontcomposer.lifecycle.subscribe");
        lifecycle.InputSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        JsonElement properties = lifecycle.InputSchema.GetProperty("properties");
        properties.GetProperty("correlationId").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("messageId").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("correlationId").GetProperty("maxLength").GetInt32().ShouldBe(64);
        properties.GetProperty("messageId").GetProperty("maxLength").GetInt32().ShouldBe(64);
        properties.GetProperty("correlationId").GetProperty("pattern").GetString()
            .ShouldBe("^[0-9A-HJKMNP-TV-Z]{26}$");
        properties.GetProperty("messageId").GetProperty("pattern").GetString()
            .ShouldBe("^[0-9A-HJKMNP-TV-Z]{26}$");
        JsonElement oneOf = lifecycle.InputSchema.GetProperty("oneOf");
        oneOf.GetArrayLength().ShouldBe(2);
        oneOf[0].GetProperty("required").EnumerateArray().Select(v => v.GetString()).ShouldBe(["correlationId"]);
        oneOf[1].GetProperty("required").EnumerateArray().Select(v => v.GetString()).ShouldBe(["messageId"]);
    }

    [Fact]
    public async Task ListToolsHandler_MissingRequestServices_ReturnsEmptyToolList() {
        ListToolsResult result = await InvokeListToolsHandlerAsync(
            ListRequest(null),
            TestContext.Current.CancellationToken);

        result.Tools.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("tenant")]
    [InlineData("unexpected")]
    public async Task ListToolsHandler_InvalidContext_ReturnsEmptyToolListWithoutProtocolError(string failure) {
        MutableAgentContextAccessor accessor = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(new LifecycleAwareCommandService(), accessor);
        using IServiceScope scope = provider.CreateScope();

        accessor.ThrowAuthFailed = string.Equals(failure, "auth", StringComparison.Ordinal);
        accessor.ThrowTenantMissing = string.Equals(failure, "tenant", StringComparison.Ordinal);
        accessor.ThrowUnexpected = string.Equals(failure, "unexpected", StringComparison.Ordinal);
        ListToolsResult result = await InvokeListToolsHandlerAsync(
            ListRequest(scope.ServiceProvider),
            TestContext.Current.CancellationToken);

        result.Tools.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListToolsHandler_TenantDeniedCatalog_ReturnsOnlyLifecycleToolForValidContext() {
        await using ServiceProvider provider = BuildMcpHandlerServices(
            new LifecycleAwareCommandService(),
            configureServices: services => services.AddSingleton<IFrontComposerMcpTenantToolGate>(new DenyingTenantToolGate()));
        using IServiceScope scope = provider.CreateScope();

        ListToolsResult result = await InvokeListToolsHandlerAsync(
            ListRequest(scope.ServiceProvider),
            TestContext.Current.CancellationToken);

        result.Tools.Select(t => t.Name).ShouldBe(["frontcomposer.lifecycle.subscribe"]);
    }

    [Fact]
    public async Task ListToolsHandler_TenantGateException_IsSanitizedAndTreatedAsHidden() {
        CapturingLogger<FrontComposerMcpToolAdmissionService> logger = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(
            new LifecycleAwareCommandService(),
            configureServices: services => {
                services.AddSingleton<IFrontComposerMcpTenantToolGate>(new ThrowingTenantToolGate());
                services.AddSingleton<ILogger<FrontComposerMcpToolAdmissionService>>(logger);
            });
        using IServiceScope scope = provider.CreateScope();

        ListToolsResult result = await InvokeListToolsHandlerAsync(
            ListRequest(scope.ServiceProvider),
            TestContext.Current.CancellationToken);

        result.Tools.Select(t => t.Name).ShouldBe(["frontcomposer.lifecycle.subscribe"]);
        logger.Entries.Single().Message.ShouldContain("bounded context Billing");
        logger.Entries.Single().Message.ShouldNotContain("tenant-a");
        logger.Entries.Single().Message.ShouldNotContain("agent-a");
        logger.Entries.Single().Message.ShouldNotContain("super-secret");
        logger.Entries.Single().Exception.ShouldBeNull();
    }

    [Fact]
    public async Task CallToolHandler_NonLifecycleToolRoutesToCommandInvoker() {
        LifecycleAwareCommandService dispatcher = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(dispatcher);
        using IServiceScope scope = provider.CreateScope();

        CallToolResult result = await InvokeCallToolHandlerAsync(
            CallRequest(scope.ServiceProvider, "Billing.PayInvoiceCommand.Execute", Args("""{"Amount":42}""")),
            TestContext.Current.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeFalse();
        JsonElement structured = result.StructuredContent.ShouldNotBeNull();
        structured.GetProperty("state").GetString().ShouldBe("Acknowledged");
        structured.GetProperty("messageId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        structured.GetProperty("correlationId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        dispatcher.DispatchCount.ShouldBe(1);
        dispatcher.Dispatched.ShouldBeOfType<PayInvoiceCommand>().Amount.ShouldBe(42);
    }

    [Fact]
    public async Task CallToolHandler_LifecycleRouteRequiresContextBeforeHandleLookup() {
        LifecycleAwareCommandService dispatcher = new();
        MutableAgentContextAccessor accessor = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(dispatcher, accessor);
        using IServiceScope scope = provider.CreateScope();

        CallToolResult acknowledgement = await InvokeCallToolHandlerAsync(
            CallRequest(scope.ServiceProvider, "Billing.PayInvoiceCommand.Execute", Args("""{"Amount":42}""")),
            TestContext.Current.CancellationToken);
        acknowledgement.IsError.GetValueOrDefault().ShouldBeFalse();

        accessor.ThrowAuthFailed = true;
        CallToolResult knownHandle = await InvokeCallToolHandlerAsync(
            CallRequest(
                scope.ServiceProvider,
                "frontcomposer.lifecycle.subscribe",
                Args("""{"correlationId":"01JZ0R5K9N8W4Y7V3Q2P6C1A0D"}""")),
            TestContext.Current.CancellationToken);
        CallToolResult unknownHandle = await InvokeCallToolHandlerAsync(
            CallRequest(
                scope.ServiceProvider,
                "frontcomposer.lifecycle.subscribe",
                Args("""{"correlationId":"01JZ0R5K9N8W4Y7V3Q2P6C1A0E"}""")),
            TestContext.Current.CancellationToken);

        knownHandle.IsError.GetValueOrDefault().ShouldBeTrue();
        unknownHandle.IsError.GetValueOrDefault().ShouldBeTrue();
        knownHandle.StructuredContent.ShouldBeNull();
        unknownHandle.StructuredContent.ShouldBeNull();
        knownHandle.Content.Single().ShouldBeOfType<TextContentBlock>().Text.ShouldBe("Request failed.");
        unknownHandle.Content.Single().ShouldBeOfType<TextContentBlock>().Text.ShouldBe("Request failed.");
        dispatcher.DispatchCount.ShouldBe(1);

        accessor.ThrowAuthFailed = false;
        accessor.ThrowTenantMissing = true;
        CallToolResult tenantMissing = await InvokeCallToolHandlerAsync(
            CallRequest(
                scope.ServiceProvider,
                "frontcomposer.lifecycle.subscribe",
                Args("""{"correlationId":"01JZ0R5K9N8W4Y7V3Q2P6C1A0D"}""")),
            TestContext.Current.CancellationToken);

        tenantMissing.IsError.GetValueOrDefault().ShouldBeTrue();
        tenantMissing.StructuredContent.ShouldBeNull();
        tenantMissing.Content.Single().ShouldBeOfType<TextContentBlock>().Text.ShouldBe("Request failed.");
    }

    [Fact]
    public async Task CallToolHandler_LifecycleRouteRedactsUnexpectedContextAccessorException() {
        // P42: a host context accessor that throws a non-FrontComposerMcpException (e.g. a bare
        // InvalidOperationException) must collapse to the sanitized AuthFailed surface without
        // leaking the exception message — proving the catch-all branch keeps the redaction contract.
        LifecycleAwareCommandService dispatcher = new();
        MutableAgentContextAccessor accessor = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(dispatcher, accessor);
        using IServiceScope scope = provider.CreateScope();

        await InvokeCallToolHandlerAsync(
            CallRequest(scope.ServiceProvider, "Billing.PayInvoiceCommand.Execute", Args("""{"Amount":42}""")),
            TestContext.Current.CancellationToken);

        accessor.ThrowUnexpected = true;
        CallToolResult result = await InvokeCallToolHandlerAsync(
            CallRequest(
                scope.ServiceProvider,
                "frontcomposer.lifecycle.subscribe",
                Args("""{"correlationId":"01JZ0R5K9N8W4Y7V3Q2P6C1A0D"}""")),
            TestContext.Current.CancellationToken);

        result.IsError.GetValueOrDefault().ShouldBeTrue();
        result.StructuredContent.ShouldBeNull();
        TextContentBlock text = result.Content.Single().ShouldBeOfType<TextContentBlock>();
        text.Text.ShouldBe("Request failed.");
        text.Text.ShouldNotContain("super-secret-tenant");
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task CallToolHandler_LifecycleReadReturnsNestedRetrySnapshot() {
        LifecycleAwareCommandService dispatcher = new();
        await using ServiceProvider provider = BuildMcpHandlerServices(dispatcher);
        using IServiceScope scope = provider.CreateScope();

        await InvokeCallToolHandlerAsync(
            CallRequest(scope.ServiceProvider, "Billing.PayInvoiceCommand.Execute", Args("""{"Amount":42}""")),
            TestContext.Current.CancellationToken);

        CallToolResult snapshot = await InvokeCallToolHandlerAsync(
            CallRequest(
                scope.ServiceProvider,
                "frontcomposer.lifecycle.subscribe",
                Args("""{"messageId":"01JZ0R5K9N8W4Y7V3Q2P6C1A0C"}""")),
            TestContext.Current.CancellationToken);

        snapshot.IsError.GetValueOrDefault().ShouldBeFalse();
        JsonElement structured = snapshot.StructuredContent.ShouldNotBeNull();
        structured.GetProperty("state").GetString().ShouldBe("Confirmed");
        structured.GetProperty("terminal").GetBoolean().ShouldBeTrue();
        structured.GetProperty("messageId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0C");
        structured.GetProperty("correlationId").GetString().ShouldBe("01JZ0R5K9N8W4Y7V3Q2P6C1A0D");
        structured.GetProperty("retry").GetProperty("retryAfterMs").GetInt32().ShouldBe(321);
        structured.GetProperty("retry").GetProperty("maxLongPollMs").GetInt32().ShouldBe(1000);
        structured.TryGetProperty("retryAfterMs", out _).ShouldBeFalse();
        structured.TryGetProperty("maxLongPollMs", out _).ShouldBeFalse();
        structured.ToString().ShouldNotContain("tenant-a");
        structured.ToString().ShouldNotContain("agent-a");
        structured.ToString().ShouldNotContain("42");
    }

    private static ServiceProvider BuildServices(ICommandService dispatcher, IUlidFactory ulids) {
        ServiceCollection services = [];
        services.AddSingleton(dispatcher);
        services.AddSingleton(ulids);
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        services.AddSingleton<IFrontComposerMcpAgentContextAccessor, StaticAgentContextAccessor>();
        services.AddSingleton<FrontComposerMcpCommandInvoker>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildMcpHandlerServices(
        LifecycleAwareCommandService dispatcher,
        MutableAgentContextAccessor? accessor = null,
        Action<IServiceCollection>? configureServices = null) {
        ServiceCollection services = [];
        services.AddSingleton<ICommandService>(dispatcher);
        services.AddSingleton<IQueryService, NoopQueryService>();
        services.AddSingleton<ILifecycleStateService, RecordingLifecycleStateService>();
        services.AddSingleton<IUlidFactory, CountingUlidFactory>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        services.AddSingleton<IFrontComposerMcpAgentContextAccessor>(accessor ?? new MutableAgentContextAccessor());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<FrontComposerMcpToolAdmissionService>();
        services.AddScoped<IFrontComposerMcpVisibleToolCatalogProvider>(sp => sp.GetRequiredService<FrontComposerMcpToolAdmissionService>());
        services.AddScoped<FrontComposerMcpCommandInvoker>();
        services.AddScoped<FrontComposerMcpLifecycleTracker>();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static async ValueTask<ListToolsResult> InvokeListToolsHandlerAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken) {
        MethodInfo method = typeof(FrontComposerMcpServiceCollectionExtensions)
            .GetMethod("ListToolsAsync", BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
        return await ((ValueTask<ListToolsResult>)method.Invoke(null, [request, cancellationToken])!)
            .ConfigureAwait(false);
    }

    private static async ValueTask<CallToolResult> InvokeCallToolHandlerAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken) {
        MethodInfo method = typeof(FrontComposerMcpServiceCollectionExtensions)
            .GetMethod("CallToolAsync", BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
        return await ((ValueTask<CallToolResult>)method.Invoke(null, [request, cancellationToken])!)
            .ConfigureAwait(false);
    }

    private static RequestContext<CallToolRequestParams> Request(
        IServiceProvider services,
        IDictionary<string, JsonElement> arguments) {
        var request = new JsonRpcRequest {
            Id = new RequestId("test-request"),
            Method = RequestMethods.ToolsCall,
        };
        var context = new RequestContext<CallToolRequestParams>(
            Substitute.For<McpServer>(),
            request,
            new CallToolRequestParams {
                Name = "Billing.PayInvoiceCommand.Execute",
                Arguments = arguments,
            }) {
            Services = services,
        };
        return context;
    }

    private static RequestContext<ListToolsRequestParams> ListRequest(IServiceProvider? services) {
        var request = new JsonRpcRequest {
            Id = new RequestId("test-list"),
            Method = RequestMethods.ToolsList,
        };
        var context = new RequestContext<ListToolsRequestParams>(
            Substitute.For<McpServer>(),
            request,
            new ListToolsRequestParams()) {
            Services = services,
        };
        return context;
    }

    private static RequestContext<CallToolRequestParams> CallRequest(
        IServiceProvider services,
        string name,
        IDictionary<string, JsonElement> arguments) {
        var request = new JsonRpcRequest {
            Id = new RequestId("test-call"),
            Method = RequestMethods.ToolsCall,
        };
        var context = new RequestContext<CallToolRequestParams>(
            Substitute.For<McpServer>(),
            request,
            new CallToolRequestParams {
                Name = name,
                Arguments = arguments,
            }) {
            Services = services,
        };
        return context;
    }

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [Descriptor()], []);

    private static McpCommandDescriptor Descriptor()
        => new(
            "Billing.PayInvoiceCommand.Execute",
            typeof(PayInvoiceCommand).FullName!,
            "Billing",
            "Pay invoice",
            "Pays an invoice.",
            null,
            [
                new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
                new McpParameterDescriptor("UnsupportedPayload", "Object", "object", false, true, "Unsupported payload", null, [], true),
            ],
            ["TenantId", "UserId", "MessageId", "CommandId", "CorrelationId"]);

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int Amount { get; set; }
    }

    private sealed class RecordingCommandService : ICommandService {
        public object? Dispatched { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            string messageId = ReadString(command, nameof(PayInvoiceCommand.MessageId)) ?? "message-a";
            string correlationId = ReadString(command, nameof(PayInvoiceCommand.CorrelationId)) ?? "corr-a";
            return Task.FromResult(new CommandResult(messageId, "Accepted", correlationId));
        }

        private static string? ReadString<TCommand>(TCommand command, string propertyName)
            where TCommand : class
            => command.GetType().GetProperty(propertyName)?.GetValue(command) as string;
    }

    private sealed class LifecycleAwareCommandService : ICommandServiceWithLifecycle {
        public object? Dispatched { get; private set; }

        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            DispatchCount++;
            string messageId = ReadString(command, nameof(PayInvoiceCommand.MessageId)) ?? "message-a";
            string correlationId = ReadString(command, nameof(PayInvoiceCommand.CorrelationId)) ?? "corr-a";
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
            return Task.FromResult(new CommandResult(
                messageId,
                "Accepted",
                correlationId,
                RetryAfter: TimeSpan.FromMilliseconds(321)));
        }

        private static string? ReadString<TCommand>(TCommand command, string propertyName)
            where TCommand : class
            => command.GetType().GetProperty(propertyName)?.GetValue(command) as string;
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

        public void Transition(
            string correlationId,
            CommandLifecycleState newState,
            string? messageId,
            bool idempotencyResolved) {
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

    private sealed class MutableAgentContextAccessor : IFrontComposerMcpAgentContextAccessor {
        public bool ThrowAuthFailed { get; set; }

        public bool ThrowUnexpected { get; set; }

        public bool ThrowTenantMissing { get; set; }

        public FrontComposerMcpAgentContext GetContext() {
            if (ThrowAuthFailed) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
            }

            if (ThrowTenantMissing) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.TenantMissing);
            }

            if (ThrowUnexpected) {
                // A host accessor that throws a non-FrontComposerMcpException must not leak its
                // message through the MCP transport (P42 catch-all collapses to AuthFailed).
                throw new InvalidOperationException("super-secret-tenant accessor failure for agent-a");
            }

            return new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
        }
    }

    private sealed class NoopQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new QueryResult<T>([], 0, "etag-a"));
    }

    private sealed class DisposableAction(Action dispose) : IDisposable {
        public void Dispose() => dispose();
    }

    private sealed class StaticAgentContextAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class CountingUlidFactory : IUlidFactory {
        private int _next;

        public int Count => _next;

        public string NewUlid() {
            int value = Interlocked.Increment(ref _next);
            return value switch {
                1 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
                2 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0D",
                3 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0E",
                _ => throw new InvalidOperationException("Unexpected ULID allocation."),
            };
        }
    }

    private sealed class DenyingTenantToolGate : IFrontComposerMcpTenantToolGate {
        public ValueTask<bool> IsVisibleAsync(
            McpCommandDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }

    private sealed class ThrowingTenantToolGate : IFrontComposerMcpTenantToolGate {
        public ValueTask<bool> IsVisibleAsync(
            McpCommandDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("super-secret tenant-a agent-a policy failure");
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
