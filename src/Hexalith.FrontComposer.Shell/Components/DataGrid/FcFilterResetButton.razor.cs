using System.Globalization;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.5 / D17 / AC3 — outline button dispatching <see cref="FiltersResetAction"/>.
/// </summary>
public partial class FcFilterResetButton : ComponentBase {
    private string _ariaLabel = string.Empty;

    /// <summary>Stable per-view key.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Number of currently active filters; interpolated into the aria-label.</summary>
    [Parameter]
    public int ActiveFilterCount { get; set; }

    /// <summary>Whether any filter is active (controls the disabled state).</summary>
    [Parameter]
    [EditorRequired]
    public bool HasActiveFilters { get; set; }

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _ariaLabel = Localizer[
            "FilterResetButtonAriaLabelTemplate",
            ActiveFilterCount.ToString(CultureInfo.CurrentUICulture)].Value;
    }

    private Task OnResetClickedAsync() {
        Dispatcher.Dispatch(new FiltersResetAction(ViewKey));
        return Task.CompletedTask;
    }
}
