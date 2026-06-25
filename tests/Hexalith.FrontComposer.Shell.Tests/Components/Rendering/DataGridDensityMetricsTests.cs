using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class DataGridDensityMetricsTests {
    [Theory]
    [InlineData(DensityLevel.Compact, 32f)]
    [InlineData(DensityLevel.Comfortable, 44f)]
    [InlineData(DensityLevel.Roomy, 56f)]
    public void ResolveRowHeightPx_ReturnsVirtualizeRowHeight(DensityLevel density, float expected) {
        DataGridDensityMetrics.ResolveRowHeightPx(density).ShouldBe(expected);
    }
}
