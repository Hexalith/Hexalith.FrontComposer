using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Rendering;
using Hexalith.FrontComposer.Mcp.Schema;
using Hexalith.FrontComposer.Mcp.Tests.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// AC1 / AC2 / AC5 / T3 — wire McpSchemaNegotiator into FrontComposerMcpProjectionReader. The
/// reader must run negotiation between visibility/tenant checks and query dispatch, surface
/// sanitized schema categories via the failure mapper, preserve Story 8-2 hidden/unknown
/// precedence, and keep admitted queries bounded.
/// </summary>
public sealed class ProjectionReaderSchemaGateTests {
    [Fact]
    public async Task SchemaGate_RunsAfterVisibility_BeforeQueryDispatch() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: SchemaHintFor("stale-client"),
            serverFingerprint: SchemaHintFor("server-current"));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.SchemaMismatch);
        _ = result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("schema-mismatch");
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldStartWith("HFC-MCP-PROJECTION-SCHEMA-");
        query.CallCount.ShouldBe(0, "AC1: schema-mismatch must short-circuit before query dispatch.");
    }

    [Fact]
    public async Task HiddenPrecedence_WinsOverSchemaMismatch() {
        // AC2 anchor: a hidden resource AND a stale client schema. The reader must collapse to
        // the hidden-equivalent unknown_resource public payload, not surface schema-mismatch.
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: SchemaHintFor("stale-client"),
            serverFingerprint: SchemaHintFor("server-current"),
            configureServices: services => services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(
                new ToggleResourceVisibilityGate(visible: false)));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_resource");
        result.StructuredContent["isHiddenEquivalent"]!.GetValue<bool>().ShouldBeTrue();
        // No schema details bleed.
        result.StructuredContent.ToJsonString().ShouldNotContain("schema-mismatch");
        result.StructuredContent.ToJsonString().ShouldNotContain("HFC-SCHEMA-");
        query.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmittedSchema_DispatchesBoundedQuery(bool additive)
    {
        const string Uri = "frontcomposer://Billing/projections/InvoiceProjection";
        CountingQueryService query = new();
        CapturingLogger<SchemaNegotiationRuntimeGate> schemaLogger = new();
        SchemaFingerprint serverFingerprint = SchemaHintFor("server-current");
        InvocationSchemaBaselineProvider? baseline = additive ? new(new SchemaContractDocument(
            "frontcomposer.schema.contract.v1", SchemaContractFamily.ProjectionResource,
            Uri, "frontcomposer.projection-resource.v1", "Billing", typeof(InvoiceProjection).FullName!, Uri,
            [new SchemaFieldContract("Number", "String", "string", true, false, "Number")],
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> { ["renderStrategy"] = nameof(McpProjectionRenderStrategy.Default) })) : null;
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: baseline?.Fingerprint ?? serverFingerprint,
            serverFingerprint: serverFingerprint,
            configureServices: services => {
                _ = services.Configure<FrontComposerMcpOptions>(options => {
                    options.DefaultResourceTake = 20;
                    options.MaxResourceTake = 10;
                });
                if (baseline is not null) {
                    _ = services.AddSingleton<ISchemaBaselineProvider>(baseline);
                    _ = services.AddSingleton<ILogger<SchemaNegotiationRuntimeGate>>(schemaLogger);
                }
            });

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse($"Projection admission returned {result.Category}.");
        if (additive) {
            schemaLogger.Entries.ShouldHaveSingleItem().State["DecisionKind"]
                .ShouldBe(nameof(McpSchemaNegotiationResultKind.CompatibleAdditive));
        }
        query.CallCount.ShouldBe(1);
        QueryRequest request = query.LastRequest.ShouldNotBeNull();
        request.Criteria.Take.ShouldBe(10);
    }

    [Fact]
    public async Task UnsupportedAlgorithm_FromClient_SurfacesSanitizedAgentCategory() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: new SchemaFingerprint("frontcomposer.schema.sha512.future", new string('c', 64)),
            serverFingerprint: SchemaHintFor("server-current"));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unsupported-schema-fingerprint");
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UnknownBaseline_SurfacesSchemaUnavailable() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: SchemaHintFor("baseline-missing"),
            serverFingerprint: null);

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownSchemaBaseline);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("schema-unavailable");
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ZeroSideEffects_OnIncompatibleNegotiation_NoQueryNoRender() {
        // AC1 zero-side-effect tightening: incompatible negotiation must not invoke query
        // execution, lifecycle mutation, cache writes, or renderer buffers.
        CountingQueryService query = new();
        CountingProjectionRenderer renderer = new(McpProjectionRenderResult.Failure(FrontComposerMcpFailureCategory.UnsupportedRender));
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: SchemaHintFor("incompatible"),
            serverFingerprint: SchemaHintFor("server-current"),
            configureServices: s => s.AddSingleton<IFrontComposerMcpProjectionRenderer>(renderer));

        _ = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        query.CallCount.ShouldBe(0);
        renderer.CallCount.ShouldBe(0);
    }

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x')[..64]);

    private static FrontComposerMcpProjectionReader BuildReader(
        IQueryService queryService,
        SchemaFingerprint? clientFingerprintHint,
        SchemaFingerprint? serverFingerprint,
        Action<IServiceCollection>? configureServices = null) {
        ServiceCollection services = [];
        _ = services.AddSingleton(queryService);
        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest(serverFingerprint)));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor(clientFingerprintHint));
        _ = services.AddScoped<FrontComposerMcpProjectionReader>();
        _ = services.AddSingleton<IFrontComposerMcpProjectionRenderer, DefaultFrontComposerMcpProjectionRenderer>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        configureServices?.Invoke(services);
        ServiceProvider provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);
    }

    private static McpManifest Manifest(SchemaFingerprint? serverFingerprint) {
        return new("frontcomposer.mcp.v1", [], [
            new McpResourceDescriptor(
                "frontcomposer://Billing/projections/InvoiceProjection",
                "InvoiceProjection",
                typeof(InvoiceProjection).FullName!,
                "Billing",
                "Invoices",
                null,
                [
                    new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                    new McpParameterDescriptor("Memo", "String", "string", false, true, "Memo", null, [], false),
                ],
                RenderStrategy: McpProjectionRenderStrategy.Default,
                Fingerprint: serverFingerprint),
        ]);
    }

    public sealed record InvoiceProjection(string Number, string? Memo = null);

    private sealed class CountingQueryService : IQueryService {
        public int CallCount { get; private set; }
        public QueryRequest? LastRequest { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            CallCount++;
            LastRequest = request;
            object[] items = [new InvoiceProjection("INV-1")];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    /// <summary>
    /// Mirrors the existing accessor pattern but exposes a client schema fingerprint hint that
    /// T3 will plumb through to the negotiator. Until T3 lands, the hint is unused and the
    /// resulting tests fail because the gate hasn't run.
    /// </summary>
    private sealed class StaticAccessor(SchemaFingerprint? clientFingerprintHint) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(
                "tenant-a",
                "agent-a",
                new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test", nameType: "name", roleType: "role")));

        public SchemaFingerprint? ClientFingerprintHint => clientFingerprintHint;
    }

    private sealed class ToggleResourceVisibilityGate(bool visible) : IFrontComposerMcpResourceVisibilityGate {
        public ValueTask<bool> IsVisibleAsync(McpResourceDescriptor descriptor, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(visible);
    }

    private sealed class AllowAllResourceVisibilityGate : IFrontComposerMcpResourceVisibilityGate {
        public ValueTask<bool> IsVisibleAsync(McpResourceDescriptor descriptor, FrontComposerMcpAgentContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(true);
    }

    private sealed class CountingProjectionRenderer(McpProjectionRenderResult result) : IFrontComposerMcpProjectionRenderer {
        public int CallCount { get; private set; }

        public McpProjectionRenderResult Render(McpProjectionRenderRequest request, FrontComposerMcpOptions options, CancellationToken cancellationToken) {
            CallCount++;
            return result;
        }
    }
}
