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

    private static ColumnModel BadgeCol()
        => new(
            "Status",
            "Status",
            TypeCategory.Enum,
            null,
            false,
            new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(
                new BadgeMappingEntry("Ready", "Success"),
                new BadgeMappingEntry("NeedsReview", "Warning"))),
            new EquatableArray<string>(ImmutableArray.Create("Ready", "NeedsReview")));

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

    [Fact]
    public void EmitsProjectionGridClassStickyHeaderItemSizeAndDensityKeyTogether() {
        string src = GeneratedRenderTreeText.MaskSequenceArguments(RazorEmitter.Emit(Model(Col("Id"), Col("Name"))));
        int gridIndex = src.IndexOf("builder.OpenComponent<FluentDataGrid<OrderProjection>>(#);", StringComparison.Ordinal);
        gridIndex.ShouldBeGreaterThanOrEqualTo(0);

        string gridBlock = src[gridIndex..src.IndexOf("builder.CloseComponent();", gridIndex, StringComparison.Ordinal)];
        gridBlock.ShouldContain("builder.SetKey(_density);");
        gridBlock.ShouldContain("\"Class\", \"fc-projection-grid\"");
        gridBlock.ShouldContain("\"GenerateHeader\", Microsoft.FluentUI.AspNetCore.Components.DataGridGeneratedHeaderType.Sticky");
        gridBlock.ShouldContain("\"ItemSize\", Hexalith.FrontComposer.Shell.Components.Rendering.DataGridDensityMetrics.ResolveRowHeightPx(_density)");
        src.ShouldContain("return \"fc-datagrid-host fc-projection-grid\";");
    }

    [Fact]
    public void EmitsStatusFilterChipsForBadgeMappedProjection() {
        string src = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));

        src.ShouldContain("private static readonly System.Collections.Generic.IReadOnlyList<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> _statusFilterSlots");
        src.ShouldContain("global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot.Success");
        src.ShouldContain("global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot.Warning");
        src.ShouldContain("FcStatusFilterChips");
        src.ShouldContain("\"AvailableSlots\", _statusFilterSlots");
        src.ShouldContain("\"ActiveSlots\", ActiveStatusSlots(gridSnapshot)");
        src.ShouldContain("ReservedFilterKeys.StatusKey");
        src.ShouldContain("private static System.Collections.Generic.IReadOnlyList<OrderProjection> TemplateItems(System.Collections.Generic.IReadOnlyList<OrderProjection>? items, global::Hexalith.FrontComposer.Contracts.Rendering.GridViewSnapshot? snapshot)");
        src.ShouldContain("items: TemplateItems(state.Items, gridSnapshot)");
        src.ShouldContain("var __detailItems = TemplateItems(state.Items, CurrentGridSnapshot());");
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

    [Fact]
    public void Emit_UsesLiteralRenderTreeSequencesInsteadOfRuntimeCounters() {
        RenderTreeSequenceRewriterTests.ShouldUseLiteralRenderTreeSequences(
            RazorEmitter.Emit(Model(Col("Id"), Col("Name"), BadgeCol())));
    }

    [Fact]
    public void Emit_PicksTheConcreteHiddenColumnAndBadgeSetTypes() {
        string source = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));

        // Story 11.21 CA1859 — concrete return/parameter types; both call sites already hand in a
        // HashSet, and every ResolveHiddenColumns path already produced a string[].
        source.ShouldContain("private static string[] ResolveHiddenColumns(");
        source.ShouldContain("System.Collections.Generic.HashSet<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> activeSlots)");
        source.ShouldNotContain("System.Collections.Generic.ISet<global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot> activeSlots)");
    }

    [Fact]
    public void Emit_TruncateUsesSpanBasedConcatWithIdenticalOutput() {
        string source = RazorEmitter.Emit(Model(Col("Id")));

        // Story 11.21 CA1845 — same characters, one fewer allocation.
        source.ShouldContain("=> value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 1), \"\\u2026\");");
        source.ShouldNotContain("value.Substring(0, maxLength - 1) + ");
    }

    [Fact]
    public void Emit_DefaultFieldRendererIsStaticOnlyWhenItReadsNoInstanceState() {
        // Text columns render from the row alone.
        RazorEmitter.Emit(Model(Col("Id"), Col("Name")))
            .ShouldContain("private static global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");

        // A badge column falls back to the injected shell localizer for unmapped members.
        string withBadge = RazorEmitter.Emit(Model(Col("Id"), BadgeCol()));
        withBadge.ShouldContain("private global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");
        withBadge.ShouldNotContain("private static global::Microsoft.AspNetCore.Components.RenderFragment RenderTemplateDefaultField(");
    }

    [Fact]
    public void Emit_ProjectionTeardownSuppressesFinalization() {
        RazorEmitter.Emit(Model(Col("Id"))).ShouldContain("System.GC.SuppressFinalize(this);");
    }

}
