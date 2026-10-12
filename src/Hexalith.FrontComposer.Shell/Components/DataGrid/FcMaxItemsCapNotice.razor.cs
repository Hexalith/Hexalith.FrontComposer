using System.Globalization;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

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
    private IDisposable? _activeClaim;
    private string _boundSurface = string.Empty;
    private long _boundScopeGeneration;
    private bool _disposed;

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

    /// <summary>Generation of the current tenant and user scope.</summary>
    [Parameter]
    public long ScopeGeneration { get; set; }

    /// <summary>Uses the parent surface's persistent status node when true; direct callers own one by default.</summary>
    [Parameter]
    public bool SharedStatusOwner { get; set; }

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>Visible iff <c>ItemsCount &gt;= MaxUnfilteredItems</c> AND no real filter is active.</summary>
    public bool Visible {
        get {
            int cap = ShellOptions.CurrentValue.MaxUnfilteredItems;
            return !AnyRealFilterActive && ItemsCount >= cap;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (_disposed)
        {
            return;
        }
        string surface = "projection:" + ViewKey;
        if (!string.Equals(_boundSurface, surface, StringComparison.Ordinal) || _boundScopeGeneration != ScopeGeneration)
        {
            if (_activeGroup is not null)
            {
                _activeClaim?.Dispose();
                _activeClaim = null;
                _activeGroup = null;
            }
            _boundSurface = surface;
            _boundScopeGeneration = ScopeGeneration;
            _wasVisible = false;
            _lastAnnouncedResult = null;
        }

        int cap = ShellOptions.CurrentValue.MaxUnfilteredItems;
        _message = Localizer[
            "MaxItemsCapNoticeTemplate",
            cap.ToString(CultureInfo.CurrentCulture)].Value;
        bool visible = Visible;
        if (visible && !_wasVisible) {
            _visibleEpisode++;
        }
        string result = (ResultIdentity ?? _visibleEpisode.ToString(CultureInfo.InvariantCulture)) + ":" + cap.ToString(CultureInfo.InvariantCulture);
        if (_activeGroup is not null && (!visible || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            string formerGroup = _activeGroup;
            _activeClaim?.Dispose();
            _activeClaim = null;
            _activeGroup = null;
            if (!visible)
            {
                SurfaceAnnouncementGroupOwnership.For(Announcements).EndTransitionEpisode(
                    _boundSurface, "limit:" + ViewKey, formerGroup);
            }
        }
        if (visible && (!_wasVisible || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            // Observe already pending requests before closing the cap. A slow notice
            // can mount later in this same clock tick and must retain its older order.
            if (Services.GetService(typeof(IState<LoadedPageState>)) is IState<LoadedPageState> pages)
            {
                foreach (var pending in pages.Value.PendingCompletionsByKey)
                {
                    if (string.Equals(pending.Key.ViewKey, ViewKey, StringComparison.Ordinal))
                    {
                        _ = SurfaceAnnouncementGroupOwnership.For(Announcements).ObserveRequest(_boundSurface, pending.Value);
                    }
                }
            }
            string family = "limit:" + ViewKey;
            _activeClaim = SurfaceAnnouncementGroupOwnership.For(Announcements)
                .ClaimTransitionEpisode(_boundSurface, family,
                    family + ":scope:" + ScopeGeneration.ToString(CultureInfo.InvariantCulture) + ":" + result,
                    out string group);
            _activeGroup = group;
            Announcements.Announce(
                _boundSurface,
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
        _disposed = true;
        if (_activeGroup is not null) {
            _activeClaim?.Dispose();
            _activeClaim = null;
            _activeGroup = null;
        }
        GC.SuppressFinalize(this);
    }
}
