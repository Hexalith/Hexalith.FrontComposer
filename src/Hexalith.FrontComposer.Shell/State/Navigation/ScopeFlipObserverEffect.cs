using Fluxor;

using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Observes the eight high-frequency user-interaction actions (Story 3-6 D13 / ADR-049) and
/// delegates to <see cref="IScopeReadinessGate.EvaluateAsync"/> — the gate handles exactly-once
/// dispatch of <see cref="StorageReadyAction"/> on the first empty-to-authenticated transition.
/// </summary>
/// <remarks>
/// <para>
/// Each handler is a single line — the allowlist lives here so future high-frequency actions
/// opt in with one <c>[EffectMethod]</c> line, not distributed gate semantics.
/// </para>
/// <para>
/// The observed action types are not an exhaustive catalogue — they are the high-frequency surface
/// where a post-prerender scope flip will land within seconds of authentication. Quiet-circuit
/// scenarios are covered by <c>G2</c> gap doc; the <see cref="IScopeReadinessGate"/> abstraction
/// unblocks the eventual <c>IStore.Dispatched</c> wildcard-observer migration.
/// </para>
/// </remarks>
public sealed class ScopeFlipObserverEffect {
    private readonly IScopeReadinessGate _gate;

    /// <summary>Initializes a new instance of the <see cref="ScopeFlipObserverEffect"/> class.</summary>
    /// <param name="gate">The scope-readiness gate that performs scope inspection + exactly-once dispatch.</param>
    public ScopeFlipObserverEffect(IScopeReadinessGate gate) {
        ArgumentNullException.ThrowIfNull(gate);
        _gate = gate;
    }

    /// <summary>Observer for <see cref="BoundedContextChangedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleBoundedContextChanged(BoundedContextChangedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="SidebarToggledAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleSidebarToggled(SidebarToggledAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="NavGroupToggledAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleNavGroupToggled(NavGroupToggledAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="ThemeChangedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleThemeChanged(ThemeChangedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="DensityChangedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleDensityChanged(DensityChangedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="PaletteOpenedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandlePaletteOpened(PaletteOpenedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="PaletteClosedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandlePaletteClosed(PaletteClosedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>Observer for <see cref="CapabilityVisitedAction"/>.</summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleCapabilityVisited(CapabilityVisitedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);

    /// <summary>
    /// Observer for <see cref="AppInitializedAction"/> — seeds the empty-scope observation for
    /// circuits that boot during prerender so the post-auth dispatch path can fire on the first
    /// user interaction (closes the "quiet-circuit" home-page gap, Review Finding D1 / G2).
    /// </summary>
    /// <param name="action">The observed action (unused).</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the gate evaluation.</returns>
    [EffectMethod]
    public Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher)
        => _gate.EvaluateAsync(dispatcher);
}
