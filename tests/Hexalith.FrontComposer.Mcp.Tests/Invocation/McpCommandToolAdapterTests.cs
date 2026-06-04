using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
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
                _ => throw new InvalidOperationException("Unexpected ULID allocation."),
            };
        }
    }
}
