using System.Globalization;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-5 AC8 / D19 — breadcrumb banner rendered above the <c>FluentDataGrid</c> when the
/// user has an expanded row that the current column-filter predicate has hidden from
/// <c>state.Items</c>. Provides a "Clear filter" affordance that dispatches
/// <see cref="FiltersResetAction"/> (Story 4-3) so the suppressed expansion reappears.
/// </summary>
/// <remarks>
/// <b>V4 Path B contract.</b> Per the 2026-04-25 Inherited Contract Verification, Story 4-3
/// emits no <c>_filterPredicate</c> view-class field. Rather than reopen Story 4-3 to add one,
/// the host view (which has typed access to <c>state.Items</c> and <c>_expandedItem</c>)
/// computes the <see cref="IsHiddenByFilter"/> boolean and passes it to this banner — the
/// banner stays type-agnostic and side-effect-free.
/// </remarks>
public partial class FcExpandedRowHiddenBanner : ComponentBase {
    private string _bannerMessage = string.Empty;

    /// <summary>Stable per-view key (PERSISTED form) — matches the key consumed by <see cref="FiltersResetAction"/> per Story 4-3.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>
    /// True when the host view detects an expanded row that the current filter predicate has
    /// hidden from <c>state.Items</c>. Drives both the render gate and the suppression
    /// announcement; false renders nothing.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public bool IsHiddenByFilter { get; set; }

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        // Single-expand invariant in v1 — count is always "1" per UX-DR17. ICU pluralization
        // bundles with multi-expand v2 commit per the Known Gaps ledger (2026-04-24).
        _bannerMessage = Localizer["ExpandedRowHiddenByFilterBanner", "1"].Value;
    }

    private Task OnClearFilterClickedAsync() {
        Dispatcher.Dispatch(new FiltersResetAction(ViewKey));
        return Task.CompletedTask;
    }
}
