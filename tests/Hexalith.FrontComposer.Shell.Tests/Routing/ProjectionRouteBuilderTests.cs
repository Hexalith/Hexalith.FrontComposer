using Hexalith.FrontComposer.Shell.Routing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Routing;

public sealed class ProjectionRouteBuilderTests {
    [Theory]
    [InlineData("Counter", "Counter.Domain.Projections.CounterView", "/counter/counter-view")]
    [InlineData("Orders", "Orders.Domain.Projections.OrderLineItemView", "/orders/order-line-item-view")]
    [InlineData("Counter", "CounterView", "/counter/counter-view")]
    [InlineData("Reporting", "Reporting.Projections.XMLReportView", "/reporting/xml-report-view")]
    [InlineData("Commerce", "SKUList", "/commerce/sku-list")]
    [InlineData(" ", "CounterView", "/ /counter-view")]
    [InlineData("Counter", " ", "/counter/ ")]
    [InlineData("Counter", "Counter.Projections.", "/counter/")]
    public void BuildRoute_ValidProjection_PreservesCanonicalContract(
        string boundedContext,
        string projectionType,
        string expected) {
        ProjectionRouteBuilder.BuildRoute(boundedContext, projectionType).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Counter.Domain.Projections.CounterView", "CounterView")]
    [InlineData("CounterView", "CounterView")]
    [InlineData("Reporting.Projections.XMLReportView", "XMLReportView")]
    [InlineData(" ", " ")]
    [InlineData("Counter.Projections.", "")]
    public void ProjectionLabel_ValidProjection_PreservesSimpleNameCasing(string projectionType, string expected) {
        ProjectionRouteBuilder.ProjectionLabel(projectionType).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "CounterView")]
    [InlineData("", "CounterView")]
    [InlineData("Counter", null)]
    [InlineData("Counter", "")]
    public void BuildRoute_NullOrEmptySegment_PreservesExceptionParameter(
        string? boundedContext,
        string? projectionType) {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => ProjectionRouteBuilder.BuildRoute(boundedContext!, projectionType!));

        exception.ParamName.ShouldBe(boundedContext is null or "" ? "boundedContext" : "projectionFqn");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ProjectionLabel_NullOrEmptyProjection_PreservesExceptionParameter(string? projectionType) {
        ArgumentException exception = Should.Throw<ArgumentException>(
            () => ProjectionRouteBuilder.ProjectionLabel(projectionType!));

        exception.ParamName.ShouldBe("projectionFqn");
    }
}
