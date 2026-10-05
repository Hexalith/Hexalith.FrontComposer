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
    private bool _focusPending;
    private string? _lastAnnouncedResult;
    private long _hiddenEpisode;
    private string? _activeGroup;
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

    /// <summary>Stable filter/result identity for one hidden-detail transition.</summary>
    [Parameter]
    public string? ResultIdentity { get; set; }

    /// <summary>Whether the filter result that hides this detail has completed.</summary>
    [Parameter]
    public bool ResultSettled { get; set; } = true;

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
        _bannerMessage = Localizer["ExpandedRowHiddenByFilterBanner", "1"].Value;
        bool settledHidden = IsHiddenByFilter && ResultSettled;
        if (settledHidden && !_wasHidden) {
            _hiddenEpisode++;
        }
        string result = ResultIdentity ?? _hiddenEpisode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_activeGroup is { } prior && (!settledHidden || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            Announcements.Cancel("projection:" + ViewKey, prior);
            _activeGroup = null;
        }
        if (settledHidden && (!_wasHidden || !string.Equals(_lastAnnouncedResult, result, StringComparison.Ordinal))) {
            if (!_wasHidden) {
                _focusPending = true;
            }
            else {
                _activeGroup = "filter:" + ViewKey + ":" + _hiddenEpisode.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + result;
                Announcements.Announce(
                    "projection:" + ViewKey,
                    _activeGroup,
                    "hidden-detail:" + result,
                    Localizer["Am22DetailHidden"],
                    terminal: true);
            }
            _lastAnnouncedResult = result;
        }

        _wasHidden = settledHidden;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!_focusPending || !IsHiddenByFilter) {
            return;
        }

        IJSObjectReference? focus = null;
        try {
            focus = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js").ConfigureAwait(true);
            _focusPending = !await focus.InvokeAsync<bool>("focusFirstButtonWithin", _bannerRef).ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
        finally {
            if (focus is not null) {
                try {
                    await focus.DisposeAsync().ConfigureAwait(true);
                }
                catch (JSDisconnectedException) {
                }
            }
        }
    }

    private Task OnClearFilterClickedAsync() {
        Dispatcher.Dispatch(new FiltersResetAction(ViewKey));
        return Task.CompletedTask;
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
