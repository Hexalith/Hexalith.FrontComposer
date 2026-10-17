using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-5 AC8 / D19 — breadcrumb banner rendered above the <c>FluentDataGrid</c> when the
/// user has an expanded row that the current column-filter predicate has hidden from
/// <c>state.Items</c>. Provides a "Clear filter" affordance that dispatches
/// <see cref="FiltersResetAction"/> (Story 4-3) so the suppressed expansion reappears.
/// </summary>
/// <remarks>
/// <b>Path B contract.</b> Per the 2026-04-25 Inherited Contract Verification, Story 4-3
/// emits no <c>_filterPredicate</c> view-class field. Rather than reopen Story 4-3 to add one,
/// the host view (which has typed access to <c>state.Items</c> and <c>_expandedItem</c>)
/// computes the <see cref="IsHiddenByFilter"/> boolean and passes it to this banner — the
/// banner stays type-agnostic and side-effect-free.
/// </remarks>
public partial class FcExpandedRowHiddenBanner : ComponentBase, IDisposable {
    private string _bannerMessage = string.Empty;
    private ElementReference _bannerRef;
    private bool _wasHidden;
    private string _recoveryKey = string.Empty;
    private bool _wasMissing;
    private bool _missingPending;
    private IJSObjectReference? _missingFocusModule;
    private bool _awaitingSettlement;
    private bool _focusPending;
    private bool _focusInFlight;
    private int _focusTimeoutRetriesRemaining;
    private string? _lastAnnouncedResult;
    private long _hiddenEpisode;
    private string? _activeGroup;
    private IDisposable? _activeClaim;
    private SurfaceHiddenFocusOwnership? _focusOwnership;
    private string? _focusGroupKey;
    private string _boundSurface = string.Empty;
    private long _boundScopeGeneration;
    private bool _disposed;
    private readonly string _descriptionId = "fc-hidden-detail-reason-" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);

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

    /// <summary>Whether the expanded item is absent from the completed visible result, regardless of cause.</summary>
    [Parameter]
    public bool IsMissing { get; set; }

    /// <summary>Stable filter/result identity for one hidden-detail transition.</summary>
    [Parameter]
    public string? ResultIdentity { get; set; }

    /// <summary>Generation of the current tenant and user scope.</summary>
    [Parameter]
    public long ScopeGeneration { get; set; }

    /// <summary>Whether the filter result that hides this detail has completed.</summary>
    [Parameter]
    public bool ResultSettled { get; set; } = true;

    /// <summary>Uses the generated projection's existing shared polite status node.</summary>
    [Parameter]
    public bool SharedStatusOwner { get; set; }

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (_disposed)
        {
            return;
        }
        string surface = "projection:" + ViewKey;
        if (!string.Equals(_boundSurface, surface, StringComparison.Ordinal) || _boundScopeGeneration != ScopeGeneration)
        {
            ReleaseAnnouncement();
            ReleaseFocusClaim();
            if (_missingFocusModule is not null && _recoveryKey.Length > 0)
            {
                _ = CancelTrackerAsync(_missingFocusModule, _recoveryKey);
            }
            _recoveryKey = _descriptionId + ":" + ScopeGeneration;
            _boundSurface = surface;
            _boundScopeGeneration = ScopeGeneration;
            _wasHidden = false;
            _wasMissing = false;
            _missingPending = false;
            _awaitingSettlement = false;
            _lastAnnouncedResult = null;
            _focusPending = false;
            _focusTimeoutRetriesRemaining = 0;
        }

        _bannerMessage = IsHiddenByFilter
            ? Localizer["ExpandedRowHiddenByFilterBanner", "1"].Value
            : IsMissing ? Localizer["ExpandedRowUnavailableBanner"].Value : string.Empty;
        bool settledMissing = IsMissing && !IsHiddenByFilter && ResultSettled;
        if (settledMissing && !_wasMissing)
        {
            ReleaseAnnouncement();
            ReleaseFocusClaim();
            _missingPending = true;
            _activeClaim = SurfaceAnnouncementGroupOwnership.For(Announcements)
                .ClaimTransitionEpisode(_boundSurface, "missing:" + ViewKey,
                    "missing:" + ViewKey + ":scope:" + ScopeGeneration + ":result:" + ResultIdentity, out string group);
            _activeGroup = group;
            _focusOwnership = SurfaceHiddenFocusOwnership.For(Announcements);
            _focusGroupKey = group;
            _missingPending = _focusOwnership.Claim(group, this, OnFocusHandoff);
        }
        if (settledMissing)
        {
            _wasMissing = true;
            _wasHidden = false;
            _focusPending = false;
            return;
        }
        if (IsMissing && !IsHiddenByFilter && _wasMissing) return;
        if (!IsMissing)
        {
            _wasMissing = false;
            _missingPending = false;
        }
        bool settledHidden = IsHiddenByFilter && ResultSettled;
        string family = "filter:" + ViewKey;
        if (!settledHidden)
        {
            ReleaseFocusClaim();
            _focusPending = false;
            _focusTimeoutRetriesRemaining = 0;
            _awaitingSettlement = IsHiddenByFilter && _wasHidden;
        }
        if (settledHidden && !_wasHidden) {
            _hiddenEpisode++;
        }
        string result = ResultIdentity ?? _hiddenEpisode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bool newSettledResult = settledHidden && (_awaitingSettlement || !_wasHidden
            || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal));
        if (_activeGroup is not null && (!settledHidden || newSettledResult))
        {
            string formerGroup = _activeGroup;
            ReleaseAnnouncement();
            if (!settledHidden)
            {
                SurfaceAnnouncementGroupOwnership.For(Announcements).EndTransitionEpisode(_boundSurface, family, formerGroup);
            }
        }
        if (newSettledResult)
        {
            _activeClaim = SurfaceAnnouncementGroupOwnership.For(Announcements)
                .ClaimTransitionEpisode(_boundSurface, family,
                    family + ":scope:" + ScopeGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ":result:" + result, out string group);
            _activeGroup = group;
            _awaitingSettlement = false;
        }
        string focusKey = _activeGroup ?? string.Empty;
        bool focusEligible = true;
        if (settledHidden && _activeGroup is not null && !string.Equals(_focusGroupKey, focusKey, StringComparison.Ordinal))
        {
            ReleaseFocusClaim();
            _focusOwnership = SurfaceHiddenFocusOwnership.For(Announcements);
            if (_wasHidden)
            {
                _focusOwnership.MarkSpeechOnly(focusKey);
            }
            focusEligible = _focusOwnership.Claim(focusKey, this, OnFocusHandoff);
            _focusGroupKey = focusKey;
            if (!_wasHidden && !focusEligible)
            {
                _focusPending = false;
            }
        }
        if (newSettledResult) {
            if (!_wasHidden) {
                _focusPending = focusEligible;
                _focusTimeoutRetriesRemaining = _focusPending ? 1 : 0;
            }
            else {
                _focusPending = false;
                _focusTimeoutRetriesRemaining = 0;
                Announcements.Announce(
                    _boundSurface,
                    _activeGroup!,
                    "hidden-detail:" + result,
                    Localizer["Am22DetailHidden"],
                    terminal: true);
            }
            _lastAnnouncedResult = result;
        }

        // An unsettled result cannot erase a previously completed hidden episode.
        // A first hidden result still waits until settlement before taking focus.
        _wasHidden = IsHiddenByFilter && (_wasHidden || ResultSettled);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!_disposed)
        {
            try
            {
                _missingFocusModule ??= await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js").ConfigureAwait(true);
                if (_disposed)
                {
                    await CancelMissingFocusAsync(_missingFocusModule).ConfigureAwait(true);
                    _missingFocusModule = null;
                    return;
                }
                await _missingFocusModule.InvokeVoidAsync("trackDetailFocus", _recoveryKey, ViewKey).ConfigureAwait(true);
                if (_missingPending && IsMissing && !IsHiddenByFilter && ResultSettled
                    && _focusGroupKey is { } missingGroup && _focusOwnership?.TryOwn(missingGroup, this) == true)
                {
                    string surface = _boundSurface;
                    long generation = ScopeGeneration;
                    bool focused = await _missingFocusModule.InvokeAsync<bool>(
                        "focusReplacementHeading", _recoveryKey, _descriptionId).ConfigureAwait(true);
                    if (!_disposed && _missingPending && IsMissing && !IsHiddenByFilter
                        && generation == ScopeGeneration && surface == _boundSurface)
                    {
                        _missingPending = false;
                        _focusOwnership.Complete(missingGroup, this);
                        if (!focused)
                        {
                            Announcements.Announce(surface, missingGroup, "missing-detail", _bannerMessage, terminal: true);
                        }
                    }
                }
            }
            catch (JSDisconnectedException) { AnnounceMissingWithoutFocus(); }
            catch (JSException) { AnnounceMissingWithoutFocus(); }
            catch (InvalidOperationException) { AnnounceMissingWithoutFocus(); }
            catch (OperationCanceledException) { AnnounceMissingWithoutFocus(); }
        }
        if (_disposed || !_focusPending || _focusInFlight || !IsHiddenByFilter || !ResultSettled) {
            return;
        }

        if (_focusGroupKey is null || _focusOwnership?.TryOwn(_focusGroupKey, this) != true)
        {
            // Keep this episode eligible if the current owner leaves before focus
            // or fallback completes. The ownership callback schedules that render.
            return;
        }

        _focusInFlight = true;
        string boundSurface = _boundSurface;
        string? boundResult = _lastAnnouncedResult;
        long boundEpisode = _hiddenEpisode;
        ElementReference banner = _bannerRef;
        IJSObjectReference? focus = null;
        bool retryAfterCleanup = false;
        try {
            focus = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js").ConfigureAwait(true);
            if (IsCurrentFocus(boundSurface, boundResult, boundEpisode))
            {
                bool focused = await focus.InvokeAsync<bool>("focusFirstButtonWithin", banner).ConfigureAwait(true);
                if (IsCurrentFocus(boundSurface, boundResult, boundEpisode))
                {
                    if (focused)
                    {
                        _focusPending = false;
                        _focusOwnership?.Complete(_focusGroupKey!, this);
                    }
                    else
                    {
                        AnnounceFailedFocus(boundSurface, boundResult, boundEpisode);
                    }
                }
            }
        }
        catch (JSDisconnectedException) {
            AnnounceFailedFocus(boundSurface, boundResult, boundEpisode);
        }
        catch (JSException) {
            AnnounceFailedFocus(boundSurface, boundResult, boundEpisode);
        }
        catch (InvalidOperationException) {
            AnnounceFailedFocus(boundSurface, boundResult, boundEpisode);
        }
        catch (OperationCanceledException) {
            if (IsCurrentFocus(boundSurface, boundResult, boundEpisode))
            {
                if (_focusTimeoutRetriesRemaining > 0)
                {
                    _focusTimeoutRetriesRemaining--;
                    retryAfterCleanup = true;
                }
                else
                {
                    AnnounceFailedFocus(boundSurface, boundResult, boundEpisode);
                }
            }
        }
        finally {
            if (focus is not null) {
                try {
                    await focus.DisposeAsync().ConfigureAwait(true);
                }
                catch (JSDisconnectedException) {
                }
                catch (JSException) {
                }
                catch (InvalidOperationException) {
                }
                catch (OperationCanceledException) {
                }
            }
            _focusInFlight = false;
            if (_focusPending && IsHiddenByFilter && ResultSettled && !_disposed)
            {
                retryAfterCleanup = true;
            }
        }
        if (retryAfterCleanup && !_disposed)
        {
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

    private void AnnounceMissingWithoutFocus()
    {
        if (!_disposed && _missingPending && IsMissing && !IsHiddenByFilter && ResultSettled && _activeGroup is { } group)
        {
            _missingPending = false;
            _focusOwnership?.Complete(group, this);
            Announcements.Announce(_boundSurface, group, "missing-detail", _bannerMessage, terminal: true);
        }
    }

    private bool IsCurrentFocus(string boundSurface, string? boundResult, long boundEpisode)
        => !_disposed && _focusPending && IsHiddenByFilter && ResultSettled
            && boundEpisode == _hiddenEpisode
            && string.Equals(boundSurface, _boundSurface, StringComparison.Ordinal)
            && string.Equals(boundResult, _lastAnnouncedResult, StringComparison.Ordinal);

    private void AnnounceFailedFocus(string boundSurface, string? boundResult, long boundEpisode)
    {
        if (!IsCurrentFocus(boundSurface, boundResult, boundEpisode))
        {
            return;
        }

        _focusPending = false;
        _focusOwnership?.Complete(_focusGroupKey!, this);
        Announcements.Announce(_boundSurface, _activeGroup!, "hidden-detail:" + boundResult,
            Localizer["Am22DetailHidden"], terminal: true);
    }

    private Task OnClearFilterClickedAsync() {
        Dispatcher.Dispatch(new FiltersResetAction(ViewKey));
        return Task.CompletedTask;
    }

    private void ReleaseAnnouncement() {
        _activeClaim?.Dispose();
        _activeClaim = null;
        _activeGroup = null;
    }

    private void ReleaseFocusClaim()
    {
        if (_focusGroupKey is { } group)
        {
            _focusOwnership?.Release(group, this);
            _focusGroupKey = null;
        }
    }

    private void OnFocusHandoff(string group)
    {
        if (_disposed || (!_focusPending && !_missingPending) || _focusInFlight || !string.Equals(_focusGroupKey, group, StringComparison.Ordinal))
        {
            return;
        }

        _ = InvokeAsync(() =>
        {
            if (!_disposed && (_focusPending && IsHiddenByFilter || _missingPending && IsMissing) && !_focusInFlight && ResultSettled
                && string.Equals(_focusGroupKey, group, StringComparison.Ordinal))
            {
                StateHasChanged();
            }
        });
    }

    private static async Task CancelTrackerAsync(IJSObjectReference module, string key)
    {
        try { await module.InvokeVoidAsync("cancelReplacementFocus", key).ConfigureAwait(false); }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
        catch (OperationCanceledException) { }
    }

    private async Task CancelMissingFocusAsync(IJSObjectReference module)
    {
        try
        {
            await module.InvokeVoidAsync("cancelReplacementFocus", _recoveryKey).ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
        catch (OperationCanceledException) { }
    }

    /// <inheritdoc />
    public void Dispose() {
        _disposed = true;
        ReleaseAnnouncement();
        ReleaseFocusClaim();
        if (_missingFocusModule is not null)
        {
            _ = CancelMissingFocusAsync(_missingFocusModule);
        }
        GC.SuppressFinalize(this);
    }
}
