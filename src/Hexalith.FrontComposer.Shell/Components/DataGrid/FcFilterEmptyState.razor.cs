using System.Globalization;

using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.6 / D11 / AC6 — filter-active empty state (as opposed to the zero-total
/// <c>FcProjectionEmptyPlaceholder</c>). Activates on
/// <c>filters.Any() &amp;&amp; filteredCount == 0 &amp;&amp; totalCount &gt; 0</c>.
/// </summary>
public partial class FcFilterEmptyState : ComponentBase {
    private string _message = string.Empty;

    /// <summary>Stable per-view key.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Total row count pre-filter.</summary>
    [Parameter]
    [EditorRequired]
    public int TotalCount { get; set; }

    /// <summary>Humanised entity plural.</summary>
    [Parameter]
    [EditorRequired]
    public string EntityPlural { get; set; } = string.Empty;

    /// <summary>Number of currently active filters (for the reset button's aria-label).</summary>
    [Parameter]
    public int ActiveFilterCount { get; set; }

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _message = Localizer[
            "EmptyFilteredStateTemplate",
            EntityPlural,
            TotalCount.ToString(CultureInfo.CurrentUICulture)].Value;
    }
}
