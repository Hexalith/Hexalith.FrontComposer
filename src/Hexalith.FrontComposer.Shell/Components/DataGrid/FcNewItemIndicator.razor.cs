using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>Transient DataGrid lane marker for newly confirmed rows outside active filter criteria.</summary>
public partial class FcNewItemIndicator : ComponentBase {
    [Parameter]
    public string IndicatorId { get; set; } = "fc-new-item-indicator";

    [Parameter]
    public string Text { get; set; } = "New item. It may not match current filters yet.";

    protected string DescriptionId => string.Concat(IndicatorId, "-description");
}
