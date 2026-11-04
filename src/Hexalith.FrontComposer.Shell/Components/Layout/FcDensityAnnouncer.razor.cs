using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-level aria-live announcer for density transitions (Story 3-3 D20 — promoted from G10
/// during Freya review). Announces <see cref="FrontComposerDensityState.EffectiveDensity"/>
/// transitions to screen readers for both user-driven and viewport-forced changes after the shell has
/// settled. Load-time resolution stays silent: changes applied while density hydration is idle or in
/// progress, and the recompute caused by the first measured viewport tier (which replaces the
/// bootstrap tier cap), are not operator changes and are never announced.
/// </summary>
public partial class FcDensityAnnouncer : ComponentBase, IDisposable {
    private (DensityLevel EffectiveDensity, DensityLevel? UserPreference, HydrationState HydrationState) _previous;
    private bool _hasAnnouncement;
    private string _announcementText = string.Empty;
    private bool _subscribed;
    private int _viewportReports;

    /// <summary>Fluxor state subscription — projects the effective density, preference and hydration state.</summary>
    [Inject] private IStateSelection<FrontComposerDensityState, (DensityLevel EffectiveDensity, DensityLevel? UserPreference, HydrationState HydrationState)> DensitySelection { get; set; } = default!;

    /// <summary>Fluxor action subscription — counts measured viewport tier reports.</summary>
    [Inject] private IActionSubscriber ActionSubscriber { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        DensitySelection.Select(state => (state.EffectiveDensity, state.UserPreference, state.HydrationState));
        _previous = DensitySelection.Value;
        // Fluxor notifies action subscribers before the density effect dispatches the recompute
        // for the same report, so the count already includes the report that caused a recompute.
        ActionSubscriber.SubscribeToAction<ViewportTierChangedAction>(this, _ => Interlocked.Increment(ref _viewportReports));
        DensitySelection.SelectedValueChanged += OnSelectedValueChanged;
        _subscribed = true;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_subscribed) {
            DensitySelection.SelectedValueChanged -= OnSelectedValueChanged;
            ActionSubscriber.UnsubscribeFromAllActions(this);
            _subscribed = false;
        }

        GC.SuppressFinalize(this);
    }

    private void OnSelectedValueChanged(object? sender, (DensityLevel EffectiveDensity, DensityLevel? UserPreference, HydrationState HydrationState) current) {
        (DensityLevel EffectiveDensity, DensityLevel? UserPreference, HydrationState HydrationState) previous = _previous;
        _previous = current;
        if (previous.EffectiveDensity == current.EffectiveDensity) {
            return;
        }

        // Hydration resolves the stored or default density; it is not an operator change.
        if (previous.HydrationState != HydrationState.Hydrated || current.HydrationState != HydrationState.Hydrated) {
            return;
        }

        // A preference change is a user selection. Without one, the change is viewport-forced:
        // the first measured tier only replaces the bootstrap cap, so it stays silent.
        bool userSelection = previous.UserPreference != current.UserPreference;
        if (!userSelection && Volatile.Read(ref _viewportReports) <= 1) {
            return;
        }

        string text = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Localizer["DensityAnnouncementTemplate"].Value,
            LocalizedDensityLabel(current.EffectiveDensity));
        _ = InvokeAsync(() => {
            _announcementText = text;
            _hasAnnouncement = true;
            StateHasChanged();
        });
    }

    private string LocalizedDensityLabel(DensityLevel level) => level switch {
        DensityLevel.Compact => Localizer["DensityCompactLabel"].Value,
        DensityLevel.Comfortable => Localizer["DensityComfortableLabel"].Value,
        DensityLevel.Roomy => Localizer["DensityRoomyLabel"].Value,
        _ => level.ToString(),
    };
}
