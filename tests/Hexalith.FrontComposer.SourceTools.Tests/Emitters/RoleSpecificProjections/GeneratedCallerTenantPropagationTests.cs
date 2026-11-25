using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters.RoleSpecificProjections;

public sealed class GeneratedCallerTenantPropagationTests {
    private const string DataGridProjection = """
        using Hexalith.FrontComposer.Contracts.Attributes;

        namespace RoleSpecific.Products;

        [BoundedContext("Products")]
        [Projection]
        public partial class DataGridProjection {
            public string Name { get; set; } = string.Empty;
        }
        """;

    [Theory]
    [InlineData(DataGridProjection, "Products.DataGridProjection", true)]
    [InlineData(RoleSpecificTestSources.DashboardProjection, "Metrics.DashboardProjection", true)]
    [InlineData(RoleSpecificTestSources.StatusOverviewProjection, "Tickets.StatusOverviewProjection", true)]
    [InlineData(RoleSpecificTestSources.TimelineProjection, "Audit.TimelineProjection", false)]
    [InlineData(RoleSpecificTestSources.DetailRecordProjection, "Customers.DetailRecordProjection", false)]
    [InlineData(RoleSpecificTestSources.ActionQueueProjection, "Orders.ActionQueueProjection", true)]
    public void GeneratedProjectionCarriesValidatedIdentity(string source, string projection, bool hasPageAndFallback) {
        ArgumentNullException.ThrowIfNull(projection);
        (string generated, _) = RoleSpecificProjectionApprovalTests.RunGenerator(
            source, $"RoleSpecific.{projection}.g.razor.cs", TestContext.Current.CancellationToken);
        string projectionName = projection[(projection.IndexOf('.') + 1)..];

        generated.ShouldContain("""TenantContextAccessor.TryGetContext(RenderContext?.TenantId, "projection-render")""");
        generated.ShouldContain($"new {projectionName}LoadRequestedAction(UlidFactory.NewUlid(), true) " +
            "{ ScopeTenantId = requestScope.TenantId, ScopeUserId = requestScope.UserId, OriginScopeGeneration =");

        if (hasPageAndFallback) {
            generated.ShouldContain("OriginTenantId = requestScope.TenantId, OriginUserId = requestScope.UserId, OriginScopeGeneration =");
            generated.ShouldContain("""ResolveCurrentScope(TenantContextAccessor, RenderContext?.TenantId, "projection-fallback")""");
            generated.ShouldContain("laneScope.TenantId, laneScope.UserId, laneScopeGeneration.ToString");
            generated.ShouldMatch(@"laneScope\.TenantId,\s+laneScope\.UserId,");
        }
    }
}
