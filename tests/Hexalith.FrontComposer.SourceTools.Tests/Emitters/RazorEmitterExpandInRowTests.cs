using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-5 T2 / D1-D7 — generator shape for expand-in-row detail on grid strategies.
/// </summary>
public sealed class RazorEmitterExpandInRowTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void DefaultGridStrategy_EmitsExpandedRowStateWiring() {
        string src = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col("Id"), Col("Name")));

        src.ShouldContain("_ephemeralViewKey");
        src.ShouldContain("IState<global::Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowState>");
        src.ShouldContain("ExpandedRowState.StateChanged += OnStateChanged;");
        src.ShouldContain("ExpandedRowState.Value.GetEntry(_ephemeralViewKey)");
    }

    [Fact]
    public void DefaultGridStrategy_EmitsRowClassRowClickAndHiddenBanner() {
        string src = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col("Id"), Col("Name")));

        src.ShouldContain("\"RowClass\"");
        src.ShouldContain("\"OnRowClick\"");
        src.ShouldContain("HandleRowClickAsync(row.Item)");
        src.ShouldContain("FcExpandedRowHiddenBanner");
        src.ShouldContain("\"IsHiddenByFilter\", _expandedItemHiddenByFilter");
    }

    [Fact]
    public void DefaultGridStrategy_EmitsExpandTriggerColumn() {
        string src = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col("Id"), Col("Name")));

        src.ShouldContain("TemplateColumn<OrderProjection>");
        src.ShouldContain("fc-row-action-column");
        src.ShouldContain("fc-expand-button");
        src.ShouldContain("aria-expanded");
        src.ShouldContain("aria-controls");
        src.ShouldContain("_expandPanelId");
        src.ShouldContain("ExpandRowButtonAriaLabelTemplate");
        src.ShouldContain("CollapseRowButtonAriaLabelTemplate");
        src.ShouldContain("ChevronRight");
    }

    [Fact]
    public void DefaultGridStrategy_EmitsDetailWrapperAndFactoredDetailBody() {
        string src = RazorEmitter.Emit(Model(
            ProjectionRenderStrategy.Default,
            Col("Id"),
            Col("Name"),
            Col("Status"),
            Col("CreatedAt"),
            Col("Total"),
            Col("Owner"),
            Col("ShippingStreet", group: "Shipping")));

        src.ShouldContain("FcExpandInRowDetail");
        src.ShouldContain("\"PanelId\", _expandPanelId");
        src.ShouldContain("\"HasExpanded\", _expandedItem is not null");
        src.ShouldContain("DetailPanelAriaLabel");
        src.ShouldContain("ExpandInRowDetailPanelAriaLabelTemplate");
        src.ShouldContain("int seq = 800;");
        src.ShouldContain("var entity = _expandedItem;");
        src.ShouldContain("\"Heading\", \"Shipping\"");
    }

    [Fact]
    public void DefaultGridStrategy_EmitsClickToggleAndDisposeCollapseDispatches() {
        string src = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col("Id"), Col("Name")));

        src.ShouldContain("private System.Threading.Tasks.Task HandleRowClickAsync(OrderProjection row)");
        src.ShouldContain("CollapseRowAction(_ephemeralViewKey)");
        src.ShouldContain("ExpandRowAction(_ephemeralViewKey, key)");
        src.ShouldContain("public async ValueTask DisposeAsync()");
    }

    [Fact]
    public void StatusOverviewStrategy_EmitsExpandTriggerAndDetailWrapper() {
        string src = RazorEmitter.Emit(Model(
            ProjectionRenderStrategy.StatusOverview,
            Col("Id"),
            Col("Status", TypeCategory.Enum),
            Col("Name")));

        src.ShouldContain("OrderProjectionStatusOverviewRow");
        src.ShouldContain("HandleStatusOverviewRowClickAsync");
        src.ShouldContain("TemplateColumn<OrderProjectionStatusOverviewRow>");
        src.ShouldContain("aria-controls\", _expandPanelId");
        src.ShouldContain("FcExpandInRowDetail");
        src.ShouldContain("_expandedStatusOverviewItem?.DetailItem is not null");
        src.ShouldContain("var entity = _expandedStatusOverviewItem.DetailItem;");
    }

    [Theory]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Timeline)]
    public void NonGridDetailStrategies_DoNotEmitExpandInRowMachinery(ProjectionRenderStrategy strategy) {
        string src = RazorEmitter.Emit(Model(strategy, Col("Id"), Col("Name"), Col("CreatedAt", TypeCategory.DateTime)));

        src.ShouldNotContain("FcExpandInRowDetail");
        src.ShouldNotContain("ExpandRowAction");
        src.ShouldNotContain("_ephemeralViewKey");
        src.ShouldNotContain("ExpandedRowState");
    }

    private static RazorModel Model(ProjectionRenderStrategy strategy, params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(columns)),
            strategy);

    private static ColumnModel Col(
        string name,
        TypeCategory category = TypeCategory.Text,
        string? group = null)
        => new(name, name, category, null, false, _emptyBadges, fieldGroup: group);
}
