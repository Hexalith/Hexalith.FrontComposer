using System.Globalization;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-4 T1.3 / D11 / AC4 — non-dismissing <c>FluentMessageBar Intent="Info"</c> surfaced when
/// <c>ItemsCount &gt;= FcShellOptions.MaxUnfilteredItems</c> and no real filters are active
/// (reserved-key entries starting with <c>__</c> are excluded from "real filters").
/// </summary>
/// <remarks>
/// Visibility is derived from the component's inputs alone — the generator-emitted view supplies
/// <see cref="ItemsCount"/> and <see cref="AnyRealFilterActive"/>. No Fluxor state subscription
/// so the banner re-evaluates on every parent render.
/// </remarks>
public partial class FcMaxItemsCapNotice : ComponentBase {
    private string _message = string.Empty;

    /// <summary>Stable per-view key (reserved for future telemetry).</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Current unfiltered row count in the view's state.</summary>
    [Parameter]
    [EditorRequired]
    public int ItemsCount { get; set; }

    /// <summary>
    /// Whether any real column filter / search / status chip is active. The generator-emitted
    /// view computes this by filtering the snapshot's <c>Filters</c> dictionary to exclude
    /// reserved keys starting with <c>__</c> (status, search, hidden columns per Story 4-3 D3
    /// and Story 4-4 D7).
    /// </summary>
    [Parameter]
    [EditorRequired]
    public bool AnyRealFilterActive { get; set; }

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Visible iff <c>ItemsCount &gt;= MaxUnfilteredItems</c> AND no real filter is active.</summary>
    public bool Visible {
        get {
            int cap = ShellOptions.CurrentValue.MaxUnfilteredItems;
            return !AnyRealFilterActive && ItemsCount >= cap;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        int cap = ShellOptions.CurrentValue.MaxUnfilteredItems;
        _message = Localizer[
            "MaxItemsCapNoticeTemplate",
            cap.ToString(CultureInfo.CurrentUICulture)].Value;
    }
}
