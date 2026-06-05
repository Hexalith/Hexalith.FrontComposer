using System.Security.Claims;
using System.Diagnostics;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
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
        // Fail-closed parity (AC3): the unknown-tool envelope must not echo the requested name,
        // otherwise an absent tool would be distinguishable from a tenant/policy-hidden one.
        result.StructuredContent!["requestedToolName"].ShouldBeNull();
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
    public async Task CanonicalTenantHiddenTool_ReturnsUnknownToolEnvelopeWithoutSensitiveContent() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => services.AddSingleton<IFrontComposerMcpTenantToolGate>(
                new TenantBoundedContextGate(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Billing"] = "tenant-a",
                    ["Catalog"] = "tenant-b",
                })));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Catalog.LabelProductCommand.Execute",
            Args("""{"Label":"secret-api-key-value"}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        result.Text.ShouldBe("Request failed.");
        string json = result.StructuredContent!.ToJsonString();
        json.ShouldContain("unknown_tool");
        json.ShouldNotContain("Catalog.LabelProductCommand.Execute");
        json.ShouldNotContain("secret-api-key-value");
        json.ShouldNotContain("tenant-a");
        json.ShouldNotContain("agent-a");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task HiddenTool_AndAbsentTool_ProduceStructurallyIdenticalUnknownEnvelope() {
        // AC3 / FC-MCP-SECURITY: a tenant-hidden real command and a never-existed command must yield
        // the same public unknown-tool envelope. A property that appears for one but not the other
        // (notably requestedToolName) is a tool-existence oracle, so the property-name sets must
        // match exactly and requestedToolName must be absent from both.
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out _,
            services => services.AddSingleton<IFrontComposerMcpTenantToolGate>(
                new TenantBoundedContextGate(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Billing"] = "tenant-a",
                    ["Catalog"] = "tenant-b",
                })));

        FrontComposerMcpResult hidden = await invoker.InvokeAsync(
            "Catalog.LabelProductCommand.Execute",
            Args("""{"Label":"Widget"}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpResult absent = await invoker.InvokeAsync(
            "Catalog.GhostCommand.Execute",
            Args("""{"Label":"Widget"}"""),
            TestContext.Current.CancellationToken);

        hidden.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        absent.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);

        string[] hiddenKeys = [.. hidden.StructuredContent!.Select(kvp => kvp.Key).Order(StringComparer.Ordinal)];
        string[] absentKeys = [.. absent.StructuredContent!.Select(kvp => kvp.Key).Order(StringComparer.Ordinal)];
        hiddenKeys.ShouldBe(absentKeys);
        hiddenKeys.ShouldNotContain("requestedToolName");
    }

    [Fact]
    public async Task HiddenTool_WithStaleSchemaFingerprint_StillUsesUnknownToolEnvelope() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => {
                services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ =>
                    new StaticAccessor(clientFingerprintHint: SchemaHintFor("stale-client")));
                services.AddSingleton<IFrontComposerMcpTenantToolGate>(
                    new TenantBoundedContextGate(new Dictionary<string, string>(StringComparer.Ordinal) {
                        ["Billing"] = "tenant-a",
                        ["Catalog"] = "tenant-b",
                    }));
            });

        FrontComposerMcpResult hidden = await invoker.InvokeAsync(
            "Catalog.LabelProductCommand.Execute",
            Args("""{"Label":"Widget"}"""),
            TestContext.Current.CancellationToken);
        FrontComposerMcpResult absent = await invoker.InvokeAsync(
            "Catalog.GhostCommand.Execute",
            Args("""{"Label":"Widget"}"""),
            TestContext.Current.CancellationToken);

        hidden.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        absent.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        hidden.StructuredContent!.ToJsonString().ShouldNotContain("schema-mismatch");
        hidden.StructuredContent.ToJsonString().ShouldNotContain("HFC-SCHEMA-");
        hidden.StructuredContent.ToJsonString().ShouldNotContain("Catalog.LabelProductCommand.Execute");
        hidden.StructuredContent.Select(kvp => kvp.Key).Order(StringComparer.Ordinal)
            .ShouldBe(absent.StructuredContent!.Select(kvp => kvp.Key).Order(StringComparer.Ordinal));
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task PolicyHiddenTool_WithStaleSchemaFingerprint_DoesNotExposeSchemaDetails() {
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            out RecordingCommandService service,
            services => {
                services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ =>
                    new StaticAccessor(clientFingerprintHint: SchemaHintFor("stale-client")));
                services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new DenyingPolicyGate());
            });

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.RestrictedCommand.Execute",
            Args("""{"Amount":42}"""),
            TestContext.Current.CancellationToken);

        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownTool);
        string json = result.StructuredContent!.ToJsonString();
        json.ShouldNotContain("schema-mismatch");
        json.ShouldNotContain("HFC-SCHEMA-");
        json.ShouldNotContain("RestrictedPolicy");
        json.ShouldNotContain("Billing.RestrictedCommand.Execute");
        service.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task CanonicalPolicyHiddenTool_ReturnsUnknownToolEnvelopeWithoutPolicyOrArguments() {
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
        json.ShouldNotContain("Billing.RestrictedCommand.Execute");
        json.ShouldNotContain("RestrictedPolicy");
        json.ShouldNotContain("42");
        json.ShouldNotContain("tenant-a");
        json.ShouldNotContain("agent-a");
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
        // Fail-closed parity (AC3): the requested name is never echoed into the envelope, so neither
        // the readable remainder nor the control character (raw or escaped) may appear. The requested
        // name is still sanitized internally on the resolution — covered by Story11_5ResolutionTests.
        json.ShouldNotContain("BadName");
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
    public async Task BuildVisibleCatalogAsync_ListsGeneratedCommandTools_WithPerDescriptorSchemaInputs() {
        FrontComposerMcpToolAdmissionService admission = BuildAdmission(
            services => services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(new AllowingPolicyGate()));

        McpVisibleToolCatalog catalog = await admission.BuildVisibleCatalogAsync(TestContext.Current.CancellationToken);

        catalog.Tools.Select(t => t.Name).ShouldBe([
            "Billing.PayInvoiceCommand.Execute",
            "Billing.RestrictedCommand.Execute",
            "Catalog.LabelProductCommand.Execute",
        ]);
        McpVisibleToolCatalogEntry entry = catalog.Tools.Single(t => t.Name == "Billing.PayInvoiceCommand.Execute");
        ModelContextProtocol.Protocol.Tool protocol = FrontComposerMcpProtocolMapper.ToProtocolTool(entry);
        protocol.InputSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        JsonElement properties = protocol.InputSchema.GetProperty("properties");
        properties.TryGetProperty("Amount", out JsonElement amount).ShouldBeTrue();
        amount.GetProperty("type").GetString().ShouldBe("number");
        properties.TryGetProperty("TenantId", out _).ShouldBeFalse();
        properties.TryGetProperty("UserId", out _).ShouldBeFalse();
        properties.TryGetProperty("MessageId", out _).ShouldBeFalse();
        properties.TryGetProperty("CommandId", out _).ShouldBeFalse();
        properties.TryGetProperty("CorrelationId", out _).ShouldBeFalse();
        properties.TryGetProperty("UnsupportedPayload", out _).ShouldBeFalse();

        ModelContextProtocol.Protocol.Tool lifecycle = FrontComposerMcpProtocolMapper.ToLifecycleTool(new FrontComposerMcpOptions());
        lifecycle.Name.ShouldBe("frontcomposer.lifecycle.subscribe");
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
        sc.AddSingleton<IUlidFactory, FixedUlidFactory>();
        sc.Configure<FrontComposerMcpOptions>(o => {
            o.Manifests.Add(Manifest());
            configureOptions?.Invoke(o);
        });
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddSingleton<FrontComposerMcpToolAdmissionService>();
        sc.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        sc.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        sc.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        configureServices?.Invoke(sc);
        ServiceProvider provider = sc.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static FrontComposerMcpToolAdmissionService BuildAdmission(Action<IServiceCollection>? configureServices = null) {
        ServiceCollection sc = new();
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddSingleton<FrontComposerMcpToolAdmissionService>();
        sc.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        sc.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        sc.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        configureServices?.Invoke(sc);
        ServiceProvider provider = sc.BuildServiceProvider();
        return provider.GetRequiredService<FrontComposerMcpToolAdmissionService>();
    }

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x').Substring(0, 64));

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                "Pay invoice.",
                null,
                [
                    new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
                    new McpParameterDescriptor("UnsupportedPayload", "Object", "object", false, true, "Unsupported payload", null, [], true),
                ],
                ["TenantId", "UserId", "MessageId", "CommandId", "CorrelationId"]),
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

    private sealed class StaticAccessor(SchemaFingerprint? clientFingerprintHint = null) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));

        public SchemaFingerprint? ClientFingerprintHint => clientFingerprintHint;
    }

    private sealed class FixedUlidFactory : IUlidFactory {
        private int _next;

        public string NewUlid() {
            int value = Interlocked.Increment(ref _next);
            return value switch {
                1 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
                2 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0D",
                _ => "01JZ0R5K9N8W4Y7V3Q2P6C1A0E",
            };
        }
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
