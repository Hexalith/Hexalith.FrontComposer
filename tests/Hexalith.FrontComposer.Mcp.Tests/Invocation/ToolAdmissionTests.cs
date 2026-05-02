using System.Security.Claims;
using System.Diagnostics;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class ToolAdmissionTests {
    [Fact]
    public async Task UnknownTool_ReturnsVisibleSuggestion_WithoutDispatch() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(out RecordingCommandService service);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "billing-payinvoicecommand-execute",
            null,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_tool");
        result.StructuredContent!["requestedToolName"]!.GetValue<string>().ShouldBe("billing-payinvoicecommand-execute");
        result.StructuredContent!["suggestion"]!.GetValue<string>().ShouldBe("Billing.PayInvoiceCommand.Execute");
        result.StructuredContent!["visibleTools"]!.AsArray().Select(v => v!["name"]!.GetValue<string>())
            .ShouldBe(["Billing.PayInvoiceCommand.Execute", "Catalog.LabelProductCommand.Execute"]);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task CaseVariantCanonicalToolName_IsSuggestedButNotExecuted() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(out RecordingCommandService service);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "billing.payinvoicecommand.execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.StructuredContent!["suggestion"]!.GetValue<string>().ShouldBe("Billing.PayInvoiceCommand.Execute");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task PolicyHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new DenyingPolicyGate()));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        string json = result.StructuredContent!.ToJsonString();
        json.ShouldNotContain("RestrictedPolicy");
        result.StructuredContent!["visibleTools"]!.AsArray().Select(v => v!["name"]!.GetValue<string>())
            .ShouldNotContain("Billing.RestrictedCommand.Execute");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task TenantHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => services.AddSingleton<IFrontComposerMcpTenantToolGate>(
                new TenantBoundedContextGate(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Billing"] = "tenant-a",
                    ["Catalog"] = "tenant-b",
                })));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "catalog-labelproductcommand-execute",
            Args("""{"Label":"Widget"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        string json = result.StructuredContent!.ToJsonString();
        json.ShouldContain("Billing.PayInvoiceCommand.Execute");
        json.ShouldNotContain("Catalog.LabelProductCommand.Execute");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task AllowedPolicy_ExecutesAfterAdmissionAndValidation() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new AllowingPolicyGate()));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        service.Dispatched.ShouldBeOfType<RestrictedCommand>();
    }

    [Fact]
    public async Task InvalidPrimitiveType_IsRejectedBeforeCommandConstruction() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(out RecordingCommandService service);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":"not-a-number"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.ValidationFailed);
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task UnknownTool_SanitizesControlCharactersAndTruncatesVisibleList() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out _,
            configureOptions: o => o.MaxVisibleToolListItems = 1);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Bad\u0001Name",
            null,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        string json = result.StructuredContent!.ToJsonString();
        // Control character is dropped by SanitizeDisplayText, leaving the readable remainder.
        json.ShouldContain("BadName");
        json.ShouldNotContain("");
        json.ShouldNotContain("\\u0001");
        json.ShouldContain("visible-list-truncated");
        result.StructuredContent!["visibleTools"]!.AsArray().Count.ShouldBe(1);
        json.ShouldNotContain("tenant-a");
        json.ShouldNotContain("agent-a");
    }

    [Fact]
    public async Task ContextSensitiveDescriptorText_IsHiddenFromVisibleCatalog() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            configureOptions: o => o.Manifests.Add(new McpManifest("frontcomposer.mcp.v1", [
                new McpCommandDescriptor(
                    "Billing.tenant-a.SecretCommand.Execute",
                    typeof(PayInvoiceCommand).FullName!,
                    "Billing",
                    "tenant-a secret",
                    "Do not show agent-a internals.",
                    null,
                    [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                    ["TenantId", "UserId", "MessageId"]),
            ], [])));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.tenant-a.SecretCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        string json = result.StructuredContent!.ToJsonString();
        json.ShouldNotContain("tenant-a secret");
        json.ShouldNotContain("agent-a internals");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task FreshPolicySnapshot_PreventsReplayOfPreviouslyVisibleTool() {
        TogglePolicyGate gate = new(allowed: true);
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(gate));

        FrontComposerMcpResult first = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        first.IsError.ShouldBeFalse();
        gate.Allowed = false;

        FrontComposerMcpResult replay = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        replay.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task UnknownToolRejection_P95_IsBelowOneHundredMilliseconds() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out _,
            configureOptions: o => o.Manifests.Add(LargeCatalogManifest(150)));

        List<long> elapsed = [];
        for (int i = 0; i < 30; i++) {
            Stopwatch sw = Stopwatch.StartNew();
            FrontComposerMcpResult result = await invoker.InvokeAsync(
                $"Billing.Missing{i}.Execute",
                null,
                TestContext.Current.CancellationToken);
            sw.Stop();
            result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
            elapsed.Add(sw.ElapsedMilliseconds);
        }

        long p95 = elapsed.Order().ElementAt((int)Math.Floor((elapsed.Count - 1) * 0.95));
        p95.ShouldBeLessThan(100);
    }

    private static FrontComposerMcpCommandInvoker BuildInvoker(
        out RecordingCommandService service,
        Action<IServiceCollection>? configureServices = null,
        Action<FrontComposerMcpOptions>? configureOptions = null) {
        service = new RecordingCommandService();
        ServiceCollection sc = new();
        sc.AddSingleton<ICommandService>(service);
        sc.Configure<FrontComposerMcpOptions>(o => {
            o.Manifests.Add(Manifest());
            configureOptions?.Invoke(o);
        });
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddSingleton<FrontComposerMcpToolAdmissionService>();
        sc.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        sc.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        configureServices?.Invoke(sc);
        ServiceProvider provider = sc.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                "Pay invoice.",
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"]),
            new McpCommandDescriptor(
                "Billing.RestrictedCommand.Execute",
                typeof(RestrictedCommand).FullName!,
                "Billing",
                "Restricted command",
                "Runs a restricted command.",
                "RestrictedPolicy",
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"]),
            new McpCommandDescriptor(
                "Catalog.LabelProductCommand.Execute",
                typeof(LabelProductCommand).FullName!,
                "Catalog",
                "Label product",
                "Labels a product.",
                null,
                [new McpParameterDescriptor("Label", "String", "string", true, false, "Label", null, [], false)],
                ["TenantId", "UserId", "MessageId"]),
        ], []);

    private static McpManifest LargeCatalogManifest(int count)
        => new("frontcomposer.mcp.v1", [.. Enumerable.Range(0, count).Select(i => new McpCommandDescriptor(
            $"Generated.Tool{i:D3}.Execute",
            typeof(PayInvoiceCommand).FullName!,
            "Generated",
            $"Generated tool {i:D3}",
            "Generated test tool.",
            null,
            [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
            ["TenantId", "UserId", "MessageId"]))], []);

    public sealed class PayInvoiceCommand {
        public string MessageId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
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
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Label { get; set; } = "";
    }

    private sealed class RecordingCommandService : ICommandService {
        public object? Dispatched { get; private set; }
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            DispatchCount++;
            return Task.FromResult(new CommandResult("message-a", "Accepted", "corr-a"));
        }
    }

    private sealed class StaticAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class AllowingPolicyGate : IFrontComposerMcpCommandPolicyGate {
        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class DenyingPolicyGate : IFrontComposerMcpCommandPolicyGate {
        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }

    private sealed class TogglePolicyGate(bool allowed) : IFrontComposerMcpCommandPolicyGate {
        public bool Allowed { get; set; } = allowed;

        public ValueTask<bool> EvaluateAsync(string policyName, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Allowed);
    }

    private sealed class TenantBoundedContextGate(IReadOnlyDictionary<string, string> tenants) : IFrontComposerMcpTenantToolGate {
        public ValueTask<bool> IsVisibleAsync(
            McpCommandDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(
                tenants.TryGetValue(descriptor.BoundedContext, out string? tenant)
                && string.Equals(tenant, context.TenantId, StringComparison.Ordinal));
    }
}
