using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-4 T5.1 — verifies the generator emits the virtualization attributes
/// (<c>Virtualize</c> / <c>DisplayMode</c> / <c>ItemSize</c> / <c>OverscanCount</c> /
/// <c>ItemKey</c>) and the density-driven <c>SetKey</c> on every grid-rendering strategy.
/// Also pins <c>_itemKeyAccessor</c> resolution precedence per D13 revised:
/// <c>AggregateId</c> &gt; <c>Id</c> &gt; <c>Key</c> &gt; <c>(object)x</c> fallback.
/// </summary>
public sealed class RazorEmitterVirtualizationTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static ColumnModel Col(string name, string? header = null, TypeCategory cat = TypeCategory.Text)
        => new(name, header ?? name, cat, null, false, _emptyBadges);

    private static RazorModel Model(params ColumnModel[] cols)
        => new("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(cols)));

    [Fact]
    public void EmitsVirtualizeAndDisplayModeAndOverscan() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("Name")));
        src.ShouldContain("\"Virtualize\", true");
        src.ShouldContain("DataGridDisplayMode.Table");
        src.ShouldContain("\"OverscanCount\", 3");
    }

    [Fact]
    public void EmitsItemSizeFromDensityMetricsAndSetKeyOnDensity() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("Name")));
        src.ShouldContain("DataGridDensityMetrics.ResolveRowHeightPx(_density)");
        src.ShouldContain("builder.SetKey(_density);");
        src.ShouldContain("_density = RenderContext?.DensityLevel");
    }

    [Theory]
    [InlineData("AggregateId")]
    [InlineData("Id")]
    [InlineData("Key")]
    public void ItemKeyAccessor_PrecedenceOverFallback(string propertyName) {
        string src = RazorEmitter.Emit(Model(Col(propertyName), Col("Other")));
        src.ShouldContain("static x => (object)x." + propertyName + "!");
        src.ShouldContain("\"ItemKey\", (System.Func<OrderProjection, object>)_itemKeyAccessor");
    }

    [Fact]
    public void ItemKeyAccessor_AggregateIdWinsOverId() {
        string src = RazorEmitter.Emit(Model(Col("Id"), Col("AggregateId"), Col("Name")));
        src.ShouldContain("static x => (object)x.AggregateId!");
        src.ShouldNotContain("static x => (object)x.Id!");
    }

    [Fact]
    public void ItemKeyAccessor_FallsBackToIdentityWhenNoMatchingProperty() {
        string src = RazorEmitter.Emit(Model(Col("Name"), Col("Other")));
        src.ShouldContain("static x => (object)x;");
    }
}
