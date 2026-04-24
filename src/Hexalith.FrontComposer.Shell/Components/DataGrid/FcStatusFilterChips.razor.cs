using System.Collections.Generic;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.2 / D5 / D15 / AC2 — status-chip strip. Reads the <c>__status</c> reserved key
/// from the supplied <paramref name="ActiveSlots"/> and renders one <c>FluentBadge</c> per
/// available slot with the appearance toggled via the Story 4-2 <c>SlotAppearanceTable</c>.
/// Click dispatches <see cref="StatusFilterToggledAction"/>.
/// </summary>
public partial class FcStatusFilterChips : ComponentBase {
    private HashSet<BadgeSlot> _activeSet = [];
    private string _groupAriaLabel = string.Empty;

    /// <summary>Stable per-view key used by the filter action.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Slots to render (distinct set from <c>state.Items</c> / chip definition).</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<BadgeSlot> AvailableSlots { get; set; } = [];

    /// <summary>Slots currently marked active (filled appearance + <c>aria-pressed="true"</c>).</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<BadgeSlot> ActiveSlots { get; set; } = [];

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _activeSet = [.. ActiveSlots];
        _groupAriaLabel = Localizer["StatusFilterChipsAriaLabel"].Value;
    }

    private Task OnSlotClickedAsync(BadgeSlot slot) {
        Dispatcher.Dispatch(new StatusFilterToggledAction(ViewKey, slot.ToString()));
        return Task.CompletedTask;
    }

    private static string HumanizeSlotName(BadgeSlot slot) => slot.ToString();
}
