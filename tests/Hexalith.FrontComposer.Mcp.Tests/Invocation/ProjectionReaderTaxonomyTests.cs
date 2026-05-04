using System.Security.Claims;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Rendering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class ProjectionReaderTaxonomyTests {
    [Theory]
    [InlineData("", FrontComposerMcpFailureCategory.MalformedRequest, "malformed_resource", "HFC-MCP-PROJECTION-MALFORMED-RESOURCE", false, false)]
    [InlineData("frontcomposer://Billing/projections/MissingProjection", FrontComposerMcpFailureCategory.UnknownResource, "unknown_resource", "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE", false, true)]
    public async Task AdmissionFailures_ReturnDeterministicTaxonomy_AndDoNotQuery(
        string uri,
        FrontComposerMcpFailureCategory expectedCategory,
        string expectedTaxonomy,
        string expectedDocsCode,
        bool expectedRetryable,
        bool expectedRefreshResources) {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(query);

        FrontComposerMcpResult result = await reader.ReadAsync(uri, TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(expectedCategory);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe(expectedTaxonomy);
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldBe(expectedDocsCode);
        result.StructuredContent!["retryable"]!.GetValue<bool>().ShouldBe(expectedRetryable);
        result.StructuredContent!["refreshResources"]!.GetValue<bool>().ShouldBe(expectedRefreshResources);
        result.StructuredContent!.ContainsKey("contentType").ShouldBeFalse();
        result.Text.ShouldBe(result.StructuredContent!["message"]!.GetValue<string>());
        query.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(FrontComposerMcpFailureCategory.ResponseTooLarge, "response_too_large", "HFC-MCP-PROJECTION-RESPONSE-TOO-LARGE", false, false)]
    [InlineData(FrontComposerMcpFailureCategory.UnsupportedRender, "unsupported_render", "HFC-MCP-PROJECTION-UNSUPPORTED-RENDER", false, false)]
    [InlineData(FrontComposerMcpFailureCategory.QueryRejected, "query_failed", "HFC-MCP-PROJECTION-QUERY-FAILED", true, false)]
    [InlineData(FrontComposerMcpFailureCategory.Timeout, "timeout", "HFC-MCP-PROJECTION-TIMEOUT", true, false)]
    [InlineData(FrontComposerMcpFailureCategory.Canceled, "canceled", "HFC-MCP-PROJECTION-CANCELED", true, false)]
    [InlineData(FrontComposerMcpFailureCategory.DegradedResult, "degraded_result", "HFC-MCP-PROJECTION-DEGRADED-RESULT", true, false)]
    // DN-2: PolicyFiltered is hidden-equivalent — its public payload collapses to unknown_resource
    // so an adversary cannot distinguish "policy-blocked" from "does-not-exist".
    [InlineData(FrontComposerMcpFailureCategory.PolicyFiltered, "unknown_resource", "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE", false, true)]
    [InlineData(FrontComposerMcpFailureCategory.DownstreamFailed, "downstream_failed", "HFC-MCP-PROJECTION-DOWNSTREAM-FAILED", true, false)]
    [InlineData(FrontComposerMcpFailureCategory.StaleDescriptor, "stale_descriptor", "HFC-MCP-PROJECTION-STALE-DESCRIPTOR", true, true)]
    public async Task QueryAndRenderFailures_ReturnSanitizedTaxonomy(
        FrontComposerMcpFailureCategory category,
        string expectedTaxonomy,
        string expectedDocsCode,
        bool expectedRetryable,
        bool expectedRefreshResources) {
        FrontComposerMcpProjectionReader reader = category switch {
            FrontComposerMcpFailureCategory.ResponseTooLarge => BuildReader(new CountingQueryService(), configureOptions: o => o.MaxProjectionMarkdownCharacters = 8),
            FrontComposerMcpFailureCategory.UnsupportedRender => BuildReader(new CountingQueryService(), manifest: Manifest(renderStrategy: McpProjectionRenderStrategy.Dashboard)),
            FrontComposerMcpFailureCategory.Timeout => BuildReader(new ThrowingQueryService(new TimeoutException("raw tenant-a timeout"))),
            FrontComposerMcpFailureCategory.Canceled => BuildReader(new ThrowingQueryService(new OperationCanceledException("raw cancel"))),
            FrontComposerMcpFailureCategory.StaleDescriptor => BuildReader(
                new CountingQueryService(),
                configureServices: services => services.AddSingleton<IFrontComposerMcpDescriptorEpochProvider>(
                    new SequenceEpochProvider(
                        new McpDescriptorEpochs(1, 1),
                        new McpDescriptorEpochs(1, 1),
                        new McpDescriptorEpochs(2, 1)))),
            _ => BuildReader(new ThrowingQueryService(new FrontComposerMcpException(category, "raw tenant-a jwt eyJabc.def"))),
        };

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe(expectedTaxonomy);
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldBe(expectedDocsCode);
        result.StructuredContent!["retryable"]!.GetValue<bool>().ShouldBe(expectedRetryable);
        result.StructuredContent!["refreshResources"]!.GetValue<bool>().ShouldBe(expectedRefreshResources);
        result.Text.ShouldBe(result.StructuredContent!["message"]!.GetValue<string>());
        JsonSerializerText(result.StructuredContent!).ShouldNotContain("tenant-a");
        JsonSerializerText(result.StructuredContent!).ShouldNotContain("eyJabc");
    }

    [Theory]
    // DN-2: TenantMissing and AuthFailed are hidden-equivalent — their public payload collapses
    // to unknown_resource so an adversary cannot distinguish "wrong tenant" from "wrong auth"
    // from "does-not-exist". The internal Category is still distinct for telemetry.
    [InlineData("", "agent-a", true, FrontComposerMcpFailureCategory.TenantMissing, "unknown_resource", "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE")]
    [InlineData("tenant-a", "", true, FrontComposerMcpFailureCategory.AuthFailed, "unknown_resource", "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE")]
    [InlineData("tenant-a", "agent-a", false, FrontComposerMcpFailureCategory.AuthFailed, "unknown_resource", "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE")]
    public async Task InvalidAgentContext_ReturnsContextTaxonomy_AndDoesNotQuery(
        string tenantId,
        string userId,
        bool authenticated,
        FrontComposerMcpFailureCategory expectedCategory,
        string expectedTaxonomy,
        string expectedDocsCode) {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            accessor: new StaticAccessor(tenantId, userId, authenticated));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(expectedCategory);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe(expectedTaxonomy);
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldBe(expectedDocsCode);
        result.StructuredContent!["isHiddenEquivalent"]!.GetValue<bool>().ShouldBeTrue();
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ResourceVisibilityDenied_IsHiddenEquivalent_AndDoesNotQueryOrEnumerateSuggestions() {
        CountingQueryService query = new();
        CountingVisibleToolCatalogProvider catalog = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            configureServices: services => {
                services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(
                    new ToggleResourceVisibilityGate(visible: false));
                services.AddSingleton<IFrontComposerMcpVisibleToolCatalogProvider>(catalog);
            });

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_resource");
        result.StructuredContent!["isHiddenEquivalent"]!.GetValue<bool>().ShouldBeTrue();
        query.CallCount.ShouldBe(0);
        catalog.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EpochChangeBeforeQuery_ReturnsStaleWithoutQuery() {
        CountingQueryService query = new();
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            configureServices: services => services.AddSingleton<IFrontComposerMcpDescriptorEpochProvider>(
                new SequenceEpochProvider(
                    new McpDescriptorEpochs(10, 20),
                    new McpDescriptorEpochs(11, 20))));

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.StaleDescriptor);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("stale_descriptor");
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EpochChangeAfterQuery_ReturnsStaleWithoutRenderingPartialOutput() {
        CountingQueryService query = new();
        CountingProjectionRenderer renderer = new(McpProjectionRenderResult.Failure(FrontComposerMcpFailureCategory.UnsupportedRender));
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            manifest: Manifest(renderStrategy: McpProjectionRenderStrategy.Dashboard),
            configureServices: services => {
                services.AddSingleton<IFrontComposerMcpProjectionRenderer>(renderer);
                services.AddSingleton<IFrontComposerMcpDescriptorEpochProvider>(
                    // P-1 sampled epochs at preLookup + postLookup, so this sequence covers
                    // four reads: preLookup, postLookup, preQuery-validate (query runs), preRender-validate (advance).
                    new SequenceEpochProvider(
                        new McpDescriptorEpochs(10, 20),
                        new McpDescriptorEpochs(10, 20),
                        new McpDescriptorEpochs(10, 20),
                        new McpDescriptorEpochs(10, 21)));
            });

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.StaleDescriptor);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("stale_descriptor");
        result.Text.ShouldNotContain("INV-1");
        query.CallCount.ShouldBe(1);
        renderer.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task VisibilityFlipAfterQuery_ReturnsHiddenEquivalentWithoutRenderingPartialOutput() {
        // R2-P6: AC2 requires visibility revalidation before render. Story 8-4a's prior pass added
        // a renderer-call counter for *epoch* advance (P-22); this is the equivalent for a
        // visibility flip. The gate returns visible=true at admission (call #1) and pre-query
        // (call #2), then visible=false at pre-render (call #3). The reader must collapse to the
        // hidden-equivalent UnknownResource public payload and never invoke the renderer.
        CountingQueryService query = new();
        CountingProjectionRenderer renderer = new(McpProjectionRenderResult.Failure(FrontComposerMcpFailureCategory.UnsupportedRender));
        SequenceResourceVisibilityGate gate = new(true, true, false);
        FrontComposerMcpProjectionReader reader = BuildReader(
            query,
            manifest: Manifest(renderStrategy: McpProjectionRenderStrategy.Dashboard),
            configureServices: services => {
                services.AddSingleton<IFrontComposerMcpProjectionRenderer>(renderer);
                services.RemoveAll<IFrontComposerMcpResourceVisibilityGate>();
                services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(gate);
            });

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/InvoiceProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("unknown_resource");
        result.StructuredContent["isHiddenEquivalent"]!.GetValue<bool>().ShouldBeTrue();
        result.Text.ShouldNotContain("INV-1");
        query.CallCount.ShouldBe(1);
        renderer.CallCount.ShouldBe(0);
        gate.CallCount.ShouldBe(3);
    }

    private static FrontComposerMcpProjectionReader BuildReader(
        IQueryService queryService,
        McpManifest? manifest = null,
        IFrontComposerMcpAgentContextAccessor? accessor = null,
        Action<FrontComposerMcpOptions>? configureOptions = null,
        Action<IServiceCollection>? configureServices = null) {
        ServiceCollection services = [];
        services.AddSingleton(queryService);
        services.Configure<FrontComposerMcpOptions>(o => {
            o.Manifests.Add(manifest ?? Manifest());
            configureOptions?.Invoke(o);
        });
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => accessor ?? new StaticAccessor());
        services.AddScoped<FrontComposerMcpProjectionReader>();
        services.AddSingleton<IFrontComposerMcpProjectionRenderer, DefaultFrontComposerMcpProjectionRenderer>();
        // P-3: visibility gate is required by the reader. Tests register a permissive default
        // so every reader build resolves a gate; tests that need restrictive visibility can
        // still override via configureServices.
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        configureServices?.Invoke(services);
        ServiceProvider provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);
    }

    private static McpManifest Manifest(McpProjectionRenderStrategy renderStrategy = McpProjectionRenderStrategy.Default)
        => new("frontcomposer.mcp.v1", [], [
            new McpResourceDescriptor(
                "frontcomposer://Billing/projections/InvoiceProjection",
                "InvoiceProjection",
                typeof(InvoiceProjection).FullName!,
                "Billing",
                "Invoices",
                null,
                [
                    new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                    new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
                ],
                RenderStrategy: renderStrategy),
        ]);

    private static string JsonSerializerText(JsonObject value)
        => value.ToJsonString();

    public sealed record InvoiceProjection(string Number, int Amount);

    private sealed class CountingQueryService : IQueryService {
        public int CallCount { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            CallCount++;
            object[] items = [new InvoiceProjection("INV-1", 42)];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    private sealed class ThrowingQueryService(Exception exception) : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
            => exception is OperationCanceledException
                ? Task.FromCanceled<QueryResult<T>>(new CancellationToken(canceled: true))
                : Task.FromException<QueryResult<T>>(exception);
    }

    private sealed class StaticAccessor(
        string tenantId = "tenant-a",
        string userId = "agent-a",
        bool authenticated = true) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(
                tenantId,
                userId,
                new ClaimsPrincipal(new ClaimsIdentity(
                    authenticationType: authenticated ? "test" : null,
                    nameType: "name",
                    roleType: "role")));
    }

    private sealed class SequenceEpochProvider(params McpDescriptorEpochs[] epochs) : IFrontComposerMcpDescriptorEpochProvider {
        private int _index;

        public McpDescriptorEpochs GetEpochs() {
            // R2-P12: throw on overrun rather than silently returning the last epoch forever.
            // Each test must declare exactly the number of provider calls its scenario triggers
            // (preLookup, postLookup, plus one ValidateSnapshot per query/render gate). An overrun
            // usually indicates a regression that introduces an extra resolution path; failing fast
            // surfaces it immediately.
            if (_index >= epochs.Length) {
                throw new InvalidOperationException(
                    $"SequenceEpochProvider exhausted: requested call #{_index + 1} but only {epochs.Length} epoch(s) declared.");
            }

            return epochs[_index++];
        }
    }

    private sealed class ToggleResourceVisibilityGate(bool visible) : IFrontComposerMcpResourceVisibilityGate {
        public ValueTask<bool> IsVisibleAsync(
            McpResourceDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(visible);
    }

    private sealed class SequenceResourceVisibilityGate(params bool[] decisions) : IFrontComposerMcpResourceVisibilityGate {
        private int _index;

        public int CallCount { get; private set; }

        public ValueTask<bool> IsVisibleAsync(
            McpResourceDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken) {
            // R2-P12 / R2-P6: throw on overrun so a regression that introduces a fourth
            // visibility check (or removes one) surfaces immediately rather than silently
            // returning the last decision forever.
            if (_index >= decisions.Length) {
                throw new InvalidOperationException(
                    $"SequenceResourceVisibilityGate exhausted: requested call #{_index + 1} but only {decisions.Length} decision(s) declared.");
            }

            CallCount++;
            return ValueTask.FromResult(decisions[_index++]);
        }
    }

    private sealed class CountingVisibleToolCatalogProvider : IFrontComposerMcpVisibleToolCatalogProvider {
        public int CallCount { get; private set; }

        public ValueTask<McpVisibleToolCatalog> BuildVisibleCatalogAsync(CancellationToken cancellationToken = default) {
            CallCount++;
            return ValueTask.FromResult(new McpVisibleToolCatalog(
                new McpToolVisibilityContext("tenant-a", "agent-a", new ClaimsPrincipal()),
                [],
                IsTruncated: false));
        }
    }

    private sealed class CountingProjectionRenderer(McpProjectionRenderResult result) : IFrontComposerMcpProjectionRenderer {
        public int CallCount { get; private set; }

        public McpProjectionRenderResult Render(
            McpProjectionRenderRequest request,
            FrontComposerMcpOptions options,
            CancellationToken cancellationToken = default) {
            CallCount++;
            return result;
        }
    }
}
