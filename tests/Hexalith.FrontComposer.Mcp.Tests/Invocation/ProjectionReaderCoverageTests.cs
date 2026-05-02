using System.Globalization;
using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// Story 8-1 closure pass — AC9 (≥2 projections), AC10 unknown-resource, P-12 value-type
/// covariance, P-19 ISO 8601 dates, P-32 malformed URI, AC15 redaction for resource reads.
/// </summary>
public sealed class ProjectionReaderCoverageTests {
    [Fact]
    public async Task UnknownResource_IsRejectedWithGenericFailureText() {
        FrontComposerMcpProjectionReader reader = BuildReader<EmptyQueryService>(out _);
        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/NoSuchProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownResource);
        result.Text.ShouldBe("Request failed.");
    }

    [Fact]
    public async Task EmptyUri_IsRejectedAsMalformedRequest() {
        FrontComposerMcpProjectionReader reader = BuildReader<EmptyQueryService>(out _);
        FrontComposerMcpResult result = await reader.ReadAsync(
            string.Empty,
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.MalformedRequest);
    }

    [Fact]
    public async Task SecondProjection_RendersIsoDates_AndEscapesPipeAndBacktick() {
        // AC9 second projection — covers DateTimeOffset rendering and SanitizeCell escape rules.
        ServiceCollection sc = new();
        sc.AddSingleton<IQueryService>(new EventStreamQueryService());
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(EventManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var reader = ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Audit/projections/EventStreamProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.Text.ShouldContain("# Audit events");
        result.Text.ShouldContain("Total: 1");

        // ISO 8601 date format ('o' specifier) emits ten leading digits then 'T'.
        result.Text.ShouldContain("2026-05-02T", customMessage: $"Body was: {result.Text}");

        // SanitizeCell preserves data through markdown-cell escape; pipe in source becomes \|.
        result.Text.ShouldContain("\\|");

        // Backtick in source must be escaped so it does not start an inline code span.
        result.Text.ShouldContain("\\`");
    }

    [Fact]
    public async Task RedactionMatrix_DoesNotEchoTenantOrJwtFragments() {
        FrontComposerMcpProjectionReader reader = BuildReader<EmptyQueryService>(out _);
        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Billing/projections/NoSuchProjection?tenant=secret-tenant&token=eyJhbGciOiJIUzI1NiJ9.attacker",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Text.ShouldNotContain("secret-tenant");
        result.Text.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9");
        result.Text.ShouldNotContain("attacker");
        result.Text.ShouldNotContain("UnknownResource");
    }

    [Fact]
    public async Task ValueTypeProjection_RendersAllRows() {
        // P-12 — IEnumerable<int> is not IEnumerable<object> via covariance, but the reader
        // must still render value-typed rows. Use a struct projection.
        ServiceCollection sc = new();
        sc.AddSingleton<IQueryService>(new ValueTypeQueryService());
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(MetricManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        var reader = ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);

        FrontComposerMcpResult result = await reader.ReadAsync(
            "frontcomposer://Ops/projections/MetricProjection",
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.Text.ShouldContain("Total: 2");
        result.Text.ShouldContain("metric-a");
        result.Text.ShouldContain("metric-b");
    }

    private static FrontComposerMcpProjectionReader BuildReader<T>(out T queryService)
        where T : class, IQueryService, new() {
        queryService = new T();
        ServiceCollection sc = new();
        sc.AddSingleton<IQueryService>(queryService);
        sc.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(EventManifest()));
        sc.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        sc.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAccessor());
        ServiceProvider provider = sc.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);
    }

    private static McpManifest EventManifest()
        => new("frontcomposer.mcp.v1", [], [
            new McpResourceDescriptor(
                "frontcomposer://Audit/projections/EventStreamProjection",
                "EventStreamProjection",
                typeof(EventStreamProjection).FullName!,
                "Audit",
                "Audit events",
                null,
                [
                    new McpParameterDescriptor("Subject", "String", "string", true, false, "Subject", null, [], false),
                    new McpParameterDescriptor("OccurredAt", "DateTimeOffset", "string", true, false, "Occurred at", null, [], false),
                ]),
        ]);

    private static McpManifest MetricManifest()
        => new("frontcomposer.mcp.v1", [], [
            new McpResourceDescriptor(
                "frontcomposer://Ops/projections/MetricProjection",
                "MetricProjection",
                typeof(MetricProjection).FullName!,
                "Ops",
                "Metrics",
                null,
                [
                    new McpParameterDescriptor("Name", "String", "string", true, false, "Name", null, [], false),
                ]),
        ]);

    public sealed class EventStreamProjection {
        public string Subject { get; set; } = "";
        public DateTimeOffset OccurredAt { get; set; }
    }

    public readonly record struct MetricProjection(string Name);

    private sealed class EmptyQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new QueryResult<T>([], 0, null));
    }

    private sealed class EventStreamQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            object[] items = [
                new EventStreamProjection {
                    Subject = "Pipe`a|b",
                    OccurredAt = new DateTimeOffset(2026, 5, 2, 12, 30, 45, TimeSpan.FromHours(2)),
                },
            ];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    private sealed class ValueTypeQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            MetricProjection[] metrics = [new MetricProjection("metric-a"), new MetricProjection("metric-b")];
            // Cast through object[] so the call into AwaitDynamic exercises non-generic IEnumerable.
            return Task.FromResult(new QueryResult<T>(metrics.Cast<T>().ToArray(), 2, null));
        }
    }

    private sealed class StaticAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }
}
