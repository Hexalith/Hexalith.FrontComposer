using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-level aria-live announcer for density transitions (Story 3-3 D20 — promoted from G10
/// during Freya review). Announces <see cref="FrontComposerDensityState.EffectiveDensity"/>
/// transitions to screen readers for both user-driven and viewport-forced changes. Initial-render
/// announcement is skipped so the page doesn't read "Density set to Comfortable" on every load.
/// </summary>
public partial class FcDensityAnnouncer : ComponentBase, IDisposable {
    private DensityLevel? _previousDensity;
    private bool _hasAnnouncement;
    private string _announcementText = string.Empty;
    private bool _subscribed;

    /// <summary>Fluxor state subscription — projects <see cref="FrontComposerDensityState.EffectiveDensity"/>.</summary>
    [Inject] private IStateSelection<FrontComposerDensityState, DensityLevel> EffectiveSelection { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        EffectiveSelection.Select(state => state.EffectiveDensity);
        _previousDensity = EffectiveSelection.Value;
        EffectiveSelection.SelectedValueChanged += OnSelectedValueChanged;
        _subscribed = true;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_subscribed) {
            EffectiveSelection.SelectedValueChanged -= OnSelectedValueChanged;
            _subscribed = false;
        }

        GC.SuppressFinalize(this);
    }

    private void OnSelectedValueChanged(object? sender, DensityLevel current) {
        if (_previousDensity == current) {
            return;
        }

        _previousDensity = current;
        _announcementText = string.Format(
            System.Globalization.CultureInfo.CurrentUICulture,
            Localizer["DensityAnnouncementTemplate"].Value,
            LocalizedDensityLabel(current));
        _hasAnnouncement = true;
        InvokeAsync(StateHasChanged);
    }

    private string LocalizedDensityLabel(DensityLevel level) => level switch {
        DensityLevel.Compact => Localizer["DensityCompactLabel"].Value,
        DensityLevel.Comfortable => Localizer["DensityComfortableLabel"].Value,
        DensityLevel.Roomy => Localizer["DensityRoomyLabel"].Value,
        _ => level.ToString(),
    };
}
