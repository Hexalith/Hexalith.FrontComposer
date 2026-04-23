using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T4.1 / D6 / AC8 — role-aware loading skeleton rendered by every generated
/// projection view during <c>state.IsLoading</c>. <see cref="Layout"/> picks the
/// role-specific visual shape (table / card / timeline) so the skeleton previews the
/// body it replaces. Outer container carries <c>role="status"</c> + <c>aria-busy="true"</c>.
/// </summary>
/// <remarks>
/// Frozen parameter shape for v1 (Story 4-1 cross-story contract). Story 4.4 column
/// prioritization may wrap the skeleton with a narrower <see cref="ColumnCount"/> without
/// touching this component's API.
/// </remarks>
public partial class FcProjectionLoadingSkeleton {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the number of skeleton columns (capped at 8 by the generator to match
    /// Story 4.4 prioritizer defaults).
    /// </summary>
    [Parameter, EditorRequired]
    public int ColumnCount { get; set; }

    /// <summary>Gets or sets the number of skeleton rows. Defaults to 5.</summary>
    [Parameter]
    public int RowCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the layout variant. Default <see cref="SkeletonLayout.DataGrid"/>;
    /// DetailRecord renders <see cref="SkeletonLayout.Card"/>; Timeline renders
    /// <see cref="SkeletonLayout.Timeline"/>.
    /// </summary>
    [Parameter]
    public SkeletonLayout Layout { get; set; } = SkeletonLayout.DataGrid;

    /// <summary>
    /// Gets or sets an accessible label for screen readers. When <see langword="null"/>
    /// the component resolves <c>FcProjectionLoadingSkeletonAriaLabel</c> with the
    /// projection display label.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the display label inserted into the aria-label. The generator passes
    /// the humanized projection name; adopters composing the skeleton directly may override.
    /// </summary>
    [Parameter]
    public string EntityLabel { get; set; } = "items";

    private string ResolvedAriaLabel => AriaLabel ?? string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        Localizer["FcProjectionLoadingSkeletonAriaLabel"].Value,
        EntityLabel);

    private string CssClass => Layout switch {
        SkeletonLayout.Card => "fc-projection-skeleton fc-projection-skeleton-layout-card",
        SkeletonLayout.Timeline => "fc-projection-skeleton fc-projection-skeleton-layout-timeline",
        _ => "fc-projection-skeleton fc-projection-skeleton-layout-datagrid",
    };

    private string HeaderCellWidth => ColumnCount <= 0 ? "80px" : "calc(" + (100 / Math.Max(ColumnCount, 1)) + "% - 12px)";

    private string BodyCellWidth => ColumnCount <= 0 ? "60px" : "calc(" + (100 / Math.Max(ColumnCount, 1)) + "% - 16px)";
}
