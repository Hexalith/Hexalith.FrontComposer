using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Settings modal content (Story 3-3 D13 / D14 / D15 / D17; AC2, AC4, AC5). Hosts the density radio,
/// the embedded <see cref="FcThemeToggle"/>, a live preview panel, and a "Restore defaults" footer.
/// Opened via <see cref="Microsoft.FluentUI.AspNetCore.Components.IDialogService.ShowDialogAsync{TContent}"/>
/// from <see cref="FcSettingsButton"/> or the <c>Ctrl+,</c> inline handler on
/// <see cref="FrontComposerShell"/>.
/// </summary>
/// <remarks>
/// No "Apply" / "Cancel" buttons — changes take effect immediately per epic AC §126. The
/// <see cref="SelectedDensity"/> setter resolves the new effective density via
/// <see cref="DensityPrecedence.Resolve(DensityLevel?, DensityLevel?, DensitySurface, ViewportTier)"/>
/// BEFORE dispatching — reducers stay pure (ADR-039).
/// </remarks>
public partial class FcSettingsDialog : Fluxor.Blazor.Web.Components.FluxorComponent {
    /// <summary>
    /// Gets or sets the dialog instance cascaded by <see cref="Microsoft.FluentUI.AspNetCore.Components.IDialogService"/>.
    /// Null when the component is rendered standalone (tests).
    /// </summary>
    [CascadingParameter]
    public Microsoft.FluentUI.AspNetCore.Components.IDialogInstance? Dialog { get; set; }

    /// <summary>Injected Fluxor state subscription — re-renders on density preference changes.</summary>
    [Inject] private IState<FrontComposerDensityState> DensityState { get; set; } = default!;

    /// <summary>Injected Fluxor navigation state — consulted for the current viewport tier on every recompute.</summary>
    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    /// <summary>Injected shell options — supplies the deployment-default density to the resolver.</summary>
    [Inject] private IOptions<FcShellOptions> Options { get; set; } = default!;

    /// <summary>Injected Fluxor dispatcher.</summary>
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    /// <summary>Injected ULID factory — shared with Story 3-1 <see cref="FcThemeToggle"/>.</summary>
    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    /// <summary>
    /// Gets or sets the density value bound to the radio group. The getter prefers the user's
    /// explicit preference and falls back to the resolved effective density when no preference
    /// has been recorded. The setter runs the pre-resolver (D3) and dispatches
    /// <see cref="UserPreferenceChangedAction"/> carrying the resolved effective density.
    /// </summary>
    public DensityLevel SelectedDensity {
        get => DensityState.Value.UserPreference ?? DensityState.Value.EffectiveDensity;
        set {
            if (DensityState.Value.UserPreference == value) {
                return;
            }

            DensityLevel newEffective = DensityPrecedence.Resolve(
                userPreference: value,
                deploymentDefault: Options.Value.DefaultDensity,
                surface: DensitySurface.Default,
                tier: NavState.Value.CurrentViewport);

            Dispatcher.Dispatch(new UserPreferenceChangedAction(UlidFactory.NewUlid(), value, newEffective));
        }
    }

    /// <summary>
    /// Gets whether the current viewport is forcing <see cref="DensityLevel.Comfortable"/> and the
    /// user's preferred density would otherwise differ (ADR-040). Used to toggle the inline
    /// <c>FluentMessageBar Info</c> note and the preview panel's "preview only" badge.
    /// </summary>
    public bool IsForcedByViewport {
        get {
            FrontComposerDensityState density = DensityState.Value;
            if (NavState.Value.CurrentViewport > ViewportTier.Tablet) {
                return false;
            }

            DensityLevel userChoice = density.UserPreference ?? Options.Value.DefaultDensity ?? DensityLevel.Comfortable;
            return userChoice != density.EffectiveDensity;
        }
    }

    /// <summary>
    /// Dispatches <see cref="UserPreferenceClearedAction"/> + <see cref="ThemeChangedAction"/> with
    /// <see cref="ThemeValue.System"/> so both axes revert together (Story 3-3 D13).
    /// </summary>
    /// <returns>A completed task — dispatch is synchronous.</returns>
    private Task RestoreDefaultsAsync() {
        DensityLevel newEffective = DensityPrecedence.Resolve(
            userPreference: null,
            deploymentDefault: Options.Value.DefaultDensity,
            surface: DensitySurface.Default,
            tier: NavState.Value.CurrentViewport);

        Dispatcher.Dispatch(new UserPreferenceClearedAction(UlidFactory.NewUlid(), newEffective));
        Dispatcher.Dispatch(new ThemeChangedAction(UlidFactory.NewUlid(), ThemeValue.System));
        return Task.CompletedTask;
    }
}
