using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Rendering;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// AC1 / AC2 / AC5 / T3 — wire McpSchemaNegotiator into FrontComposerMcpProjectionReader. The
/// reader must run negotiation between visibility/tenant checks and query dispatch, surface
/// sanitized schema categories via the failure mapper, preserve Story 8-2 hidden/unknown
/// precedence, and re-run server-side validation on `CompatibleAdditive`.
/// </summary>
public sealed class ProjectionReaderSchemaGateTests {
    [Fact]
    public async Task SchemaGate_RunsAfterVisibility_BeforeQueryDispatch() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(query, clientFingerprintHint: SchemaHintFor("stale-client"));

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

    [Fact]
    public async Task CompatibleAdditive_AdmitsDispatch_AfterRevalidation() {
        // AC5: CompatibleAdditive admits the request, but server-side validation/defaulting
        // must run again before the query is invoked. The validator counter proves revalidation.
        RevalidationCountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: SchemaHintFor("compatible-additive"));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        query.CallCount.ShouldBe(1);
        query.RevalidationCount.ShouldBeGreaterThanOrEqualTo(1, "AC5: validation must re-run before dispatch on CompatibleAdditive.");
    }

    [Fact]
    public async Task UnsupportedAlgorithm_FromClient_SurfacesSanitizedAgentCategory() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            clientFingerprintHint: new SchemaFingerprint("frontcomposer.schema.sha512.future", new string('c', 64)));

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
            clientFingerprintHint: SchemaHintFor("baseline-missing"));

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
            configureServices: s => s.AddSingleton<IFrontComposerMcpProjectionRenderer>(renderer));

        _ = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        query.CallCount.ShouldBe(0);
        renderer.CallCount.ShouldBe(0);
    }

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x')[..64]);

    private static FrontComposerMcpProjectionReader BuildReader(
        IQueryService queryService,
        SchemaFingerprint? clientFingerprintHint = null,
        Action<IServiceCollection>? configureServices = null) {
        ServiceCollection services = [];
        _ = services.AddSingleton(queryService);
        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest(clientFingerprintHint)));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor(clientFingerprintHint));
        _ = services.AddScoped<FrontComposerMcpProjectionReader>();
        _ = services.AddSingleton<IFrontComposerMcpProjectionRenderer, DefaultFrontComposerMcpProjectionRenderer>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        configureServices?.Invoke(services);
        ServiceProvider provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);
    }

    private static McpManifest Manifest(SchemaFingerprint? clientFingerprintHint) {
        SchemaFingerprint? serverFingerprint = clientFingerprintHint?.Value.StartsWith("baseline-missing", StringComparison.Ordinal) == true
            ? null
            : clientFingerprintHint?.Value.StartsWith("compatible-additive", StringComparison.Ordinal) == true
                ? clientFingerprintHint
                : new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, "server-current".PadRight(64, 's')[..64]);
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
                ],
                RenderStrategy: McpProjectionRenderStrategy.Default,
                Fingerprint: serverFingerprint),
        ]);
    }

    public sealed record InvoiceProjection(string Number);

    private sealed class CountingQueryService : IQueryService {
        public int CallCount { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            CallCount++;
            object[] items = [new InvoiceProjection("INV-1")];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    /// <summary>
    /// Counts how many times the host-side validator runs before <see cref="QueryAsync"/>.
    /// AC5 requires re-validation on CompatibleAdditive; the actual hook will be added in T3
    /// — this scaffold uses a sentinel <see cref="QueryRequest.Take"/> bound to detect the
    /// re-validation path once it lands.
    /// </summary>
    private sealed class RevalidationCountingQueryService : IQueryService {
        public int CallCount { get; private set; }

        public int RevalidationCount { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            CallCount++;
            // T3 should pin the take bound through the post-additive validator. Until T3 lands,
            // RevalidationCount stays 0 and this scaffold will fail meaningfully when unskipped.
            if (request.Take is > 0 and <= 1024) {
                RevalidationCount++;
            }

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
