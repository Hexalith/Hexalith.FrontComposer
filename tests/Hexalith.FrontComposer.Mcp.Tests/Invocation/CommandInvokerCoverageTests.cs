using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// Story 8-1 closure pass tests covering AC8 (three commands), AC10 (unknown tool), AC11 (auth),
/// AC15 (redaction), DN-8-1-1-1 (policy gate fail-closed), and T9 envelope edge cases.
/// </summary>
public sealed class CommandInvokerCoverageTests {
    [Fact]
    public async Task UnknownTool_IsRejectedWithGenericFailureText() {
        var invoker = BuildInvoker(out _);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.NoSuchCommand.Execute",
            null,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.Text.ShouldBe("Request failed.");
    }

    [Fact]
    public async Task NullToolName_IsRejectedAsUnknownTool() {
        var invoker = BuildInvoker(out _);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            null!,
            null,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
    }

    [Fact]
    public async Task DuplicateJsonProperty_IsRejectedBeforeDispatch() {
        var invoker = BuildInvoker(out RecordingCommandService service);

        // Two case-variant keys for the same descriptor parameter trip the OrdinalIgnoreCase
        // duplicate-detection set even though the C# Dictionary stores them as distinct entries.
        Dictionary<string, JsonElement> args = new(StringComparer.Ordinal) {
            ["Amount"] = JsonDocument.Parse("42").RootElement,
            ["amount"] = JsonDocument.Parse("9").RootElement,
        };

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task NestedObjectArgument_IsRejectedBeforeDispatch() {
        var invoker = BuildInvoker(out RecordingCommandService service);
        Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"Amount":{"value":42}}""")!;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task NullJsonForRequiredParameter_IsRejectedBeforeDispatch() {
        var invoker = BuildInvoker(out RecordingCommandService service);
        Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"Amount":null}""")!;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task OversizedArguments_AreRejectedBeforeDispatch() {
        var invoker = BuildInvoker(out RecordingCommandService service, configure: o => o.MaxArgumentBytes = 8);
        Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"Amount":42}""")!;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task CancellationToken_IsHonored_BeforeDispatch() {
        var invoker = BuildInvoker(out RecordingCommandService service);
        Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"Amount":42}""")!;
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // The invoker propagates cancellation through ICommandService.DispatchAsync, where the
        // recording stub honors it. Ensure the result maps to Canceled, not DownstreamFailed.
        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            cts.Token);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.Canceled);
    }

    [Fact]
    public async Task CommandRejectedException_IsTranslatedToProtocolFailure() {
        var invoker = BuildInvoker(out RecordingCommandService service);
        service.RejectNext = true;
        Dictionary<string, JsonElement> args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """{"Amount":42}""")!;

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            args,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.CommandRejected);
        result.Text.ShouldBe("Request failed.");
        result.Text.ShouldNotContain("rejected because");
    }

    [Fact]
    public async Task PolicyProtectedCommand_FailsClosed_WhenNoGateRegistered() {
        // Story 8-1 fail-closed contract (DN-8-1-1-1): a descriptor with AuthorizationPolicyName
        // must not dispatch unless the host wires IFrontComposerMcpCommandPolicyGate.
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(PolicyProtectedManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"Amount":42}""")!,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.PolicyGateMissing);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task PolicyProtectedCommand_DeniedByGate_DoesNotDispatch() {
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(PolicyProtectedManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        sc.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new DenyingGate());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"Amount":42}""")!,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task PolicyProtectedCommand_ApprovedByGate_Dispatches() {
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(PolicyProtectedManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        sc.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new AllowingGate());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"Amount":42}""")!,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        service.Dispatched.ShouldBeOfType<RestrictedCommand>();
    }

    [Fact]
    public async Task SecondCommand_FreeFormString_DispatchesAndCarriesTenantContext() {
        // AC8 second sample command — a string-typed parameter in a different bounded context.
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(LabelManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Catalog.LabelProductCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"Label":"Widget"}""")!,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        LabelProductCommand dispatched = service.Dispatched.ShouldBeOfType<LabelProductCommand>();
        dispatched.Label.ShouldBe("Widget");
        dispatched.TenantId.ShouldBe("tenant-a");
        dispatched.MessageId.ShouldNotBeNullOrWhiteSpace();
        dispatched.MessageId.ShouldBe(dispatched.CommandId);
    }

    [Fact]
    public async Task ThirdCommand_NoArguments_Dispatches() {
        // AC8 third sample command — no parameters; ensures empty-argument path is correct.
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(NoArgManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Catalog.PingCommand.Execute",
            null,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        service.Dispatched.ShouldBeOfType<PingCommand>();
    }

    [Fact]
    public async Task RecordCommand_FailsAsUnsupportedSchema() {
        // P-9 — records lack a public parameterless ctor; surface UnsupportedSchema, not the
        // generic DownstreamFailed catch-all.
        RecordingCommandService service = new();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(RecordManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var invoker = ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Catalog.PositionalRecordCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"Note":"hi"}""")!,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnsupportedSchema);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task FailureText_IsGeneric_AcrossEveryCategory() {
        // AC15 redaction: ensure the user-facing Text is identical for unrelated failure
        // categories so a probing client cannot distinguish e.g. TenantMissing from AuthFailed
        // by string content.
        var invoker = BuildInvoker(out _);

        FrontComposerMcpResult unknown = await invoker.InvokeAsync("missing", null, TestContext.Current.CancellationToken);
        FrontComposerMcpResult validation = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"TenantId":"x"}""")!,
            TestContext.Current.CancellationToken);

        unknown.Text.ShouldBe(validation.Text);
        validation.Text.ShouldBe("Request failed.");
        validation.Text.ShouldNotContain("TenantId");
        validation.Text.ShouldNotContain("ValidationFailed");
        validation.Text.ShouldNotContain("AuthFailed");
    }

    private static FrontComposerMcpCommandInvoker BuildInvoker(
        out RecordingCommandService service,
        Action<FrontComposerMcpOptions>? configure = null) {
        service = new RecordingCommandService();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => {
            o.Manifests.Add(PayInvoiceManifest());
            configure?.Invoke(o);
        });
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static McpManifest PayInvoiceManifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                null,
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"]),
        ], []);

    private static McpManifest PolicyProtectedManifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.RestrictedCommand.Execute",
                typeof(RestrictedCommand).FullName!,
                "Billing",
                "Restricted command",
                null,
                "RestrictedPolicy",
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"]),
        ], []);

    private static McpManifest LabelManifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Catalog.LabelProductCommand.Execute",
                typeof(LabelProductCommand).FullName!,
                "Catalog",
                "Label product",
                null,
                null,
                [new McpParameterDescriptor("Label", "String", "string", true, false, "Label", null, [], false)],
                ["TenantId", "UserId", "MessageId", "CommandId"]),
        ], []);

    private static McpManifest NoArgManifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Catalog.PingCommand.Execute",
                typeof(PingCommand).FullName!,
                "Catalog",
                "Ping",
                null,
                null,
                [],
                ["TenantId", "UserId", "MessageId"]),
        ], []);

    private static McpManifest RecordManifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Catalog.PositionalRecordCommand.Execute",
                typeof(PositionalRecordCommand).FullName!,
                "Catalog",
                "Positional record",
                null,
                null,
                [new McpParameterDescriptor("Note", "String", "string", true, false, "Note", null, [], false)],
                []),
        ], []);

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public int Amount { get; set; }
    }

    public sealed class RestrictedCommand {
        public string MessageId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public int Amount { get; set; }
    }

    public sealed class LabelProductCommand {
        public string MessageId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public sealed class PingCommand {
        public string MessageId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
    }

    public sealed record PositionalRecordCommand(string Note);

    private sealed class RecordingCommandService : ICommandService {
        public object? Dispatched { get; private set; }
        public bool RejectNext { get; set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            cancellationToken.ThrowIfCancellationRequested();
            if (RejectNext) {
                throw new CommandRejectedException("rejected because business rule X failed", "Resolve and retry");
            }

            Dispatched = command;
            return Task.FromResult(new CommandResult("message-a", "Accepted", "corr-a"));
        }
    }

    private sealed class StaticAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class DenyingGate : IFrontComposerMcpCommandPolicyGate {
        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }

    private sealed class AllowingGate : IFrontComposerMcpCommandPolicyGate {
        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }
}
