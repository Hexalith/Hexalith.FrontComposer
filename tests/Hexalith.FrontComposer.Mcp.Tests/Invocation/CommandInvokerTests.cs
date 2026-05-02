using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class CommandInvokerTests {
    [Fact]
    public async Task InvokeAsync_ValidCommand_DispatchesThroughCommandService_WithTenantContext() {
        RecordingCommandService service = new();
        ServiceProvider provider = Services(service).BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
        Dictionary<string, JsonElement> args = Args("""{"Amount":42}""");

        FrontComposerMcpResult result = await invoker.InvokeAsync("Billing.PayInvoiceCommand.Execute", args, TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        service.Dispatched.ShouldBeOfType<PayInvoiceCommand>();
        PayInvoiceCommand command = (PayInvoiceCommand)service.Dispatched!;
        command.Amount.ShouldBe(42);
        command.TenantId.ShouldBe("tenant-a");
        command.UserId.ShouldBe("agent-a");
        command.MessageId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_SpoofedTenantId_IsRejectedBeforeDispatch() {
        RecordingCommandService service = new();
        ServiceProvider provider = Services(service).BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":42,"TenantId":"attacker"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
        result.Text.ShouldNotContain("attacker");
    }

    private static IServiceCollection Services(ICommandService commandService) {
        var services = new ServiceCollection();
        services.AddSingleton(commandService);
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
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
                ["TenantId", "UserId", "MessageId"]),
        ], []);

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int Amount { get; set; }
    }

    private sealed class RecordingCommandService : ICommandService {
        public object? Dispatched { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            return Task.FromResult(new CommandResult("message-a", "Accepted", "corr-a"));
        }
    }

    private sealed class StaticAgentContextAccessor(string tenantId = "tenant-a", string userId = "agent-a") : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(tenantId, userId, new ClaimsPrincipal(new ClaimsIdentity("test")));
    }
}
