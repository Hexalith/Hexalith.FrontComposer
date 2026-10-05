using System.Globalization;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-4 T1.3 / D11 / AC4 — non-dismissing <c>FluentMessageBar Intent="Info"</c> surfaced when
/// <c>ItemsCount &gt;= FcShellOptions.MaxUnfilteredItems</c> and no real filters are active
/// (search and status chip values count as active filters).
/// </summary>
/// <remarks>
/// Visibility is derived from the component's inputs alone — the generator-emitted view supplies
/// <see cref="ItemsCount"/> and <see cref="AnyRealFilterActive"/>. No Fluxor state subscription
/// so the banner re-evaluates on every parent render.
/// </remarks>
public partial class FcMaxItemsCapNotice : ComponentBase, IDisposable {
    private string _message = string.Empty;
    private bool _wasVisible;
    private string? _lastAnnouncedResult;
    private long _visibleEpisode;
    private string? _activeGroup;

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
    /// view computes this from column filters, search, and status chips while excluding
    /// display-only reserved keys such as hidden columns.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public bool AnyRealFilterActive { get; set; }

    /// <summary>Identity of the completed result that reached the configured limit.</summary>
    [Parameter]
    public string? ResultIdentity { get; set; }

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

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
            cap.ToString(CultureInfo.CurrentCulture)].Value;
        bool visible = Visible;
        if (visible && !_wasVisible) {
            _visibleEpisode++;
        }
        string result = ResultIdentity ?? _visibleEpisode.ToString(CultureInfo.InvariantCulture);
        if (_activeGroup is { } prior && (!visible || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            Announcements.Cancel("projection:" + ViewKey, prior);
            _activeGroup = null;
        }
        if (visible && (!_wasVisible || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            _activeGroup = "limit:" + ViewKey + ":" + _visibleEpisode.ToString(CultureInfo.InvariantCulture) + ":" + result;
            Announcements.Announce(
                "projection:" + ViewKey,
                _activeGroup,
                result + ":" + cap.ToString(CultureInfo.InvariantCulture),
                Localizer["Am09MaxItems", cap],
                terminal: true);
            _lastAnnouncedResult = result;
        }
        _wasVisible = visible;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_activeGroup is { } group) {
            Announcements.Cancel("projection:" + ViewKey, group);
            _activeGroup = null;
        }
        GC.SuppressFinalize(this);
    }
}
