using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Pure static reducers for <see cref="FrontComposerDensityState"/>.
/// Every reducer consumes the pre-resolved <c>NewEffective</c> value from the action payload —
/// the precedence resolver is NEVER invoked here (ADR-039 purity invariant, enforced by
/// <c>DensityReducerPurityTest</c>).
/// </summary>
public static class DensityReducers {
    /// <summary>
    /// Assigns both fields from the payload (Story 3-3 D3 / AC1). Action producer pre-resolved
    /// <c>NewEffective</c> from current options + viewport + surface.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The user-preference-changed action.</param>
    /// <returns>A new state carrying both the new preference and the pre-resolved effective density.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceUserPreferenceChanged(
        FrontComposerDensityState state,
        UserPreferenceChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with {
            UserPreference = action.NewPreference,
            EffectiveDensity = action.NewEffective,
        };
    }

    /// <summary>
    /// Nulls the user preference and assigns the pre-resolved fall-through effective density
    /// (Story 3-3 D8 / D13 / AC3). Action producer resolved with <c>userPreference = null</c>.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The user-preference-cleared action.</param>
    /// <returns>A new state with <c>UserPreference = null</c>.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceUserPreferenceCleared(
        FrontComposerDensityState state,
        UserPreferenceClearedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with {
            UserPreference = null,
            EffectiveDensity = action.NewEffective,
        };
    }

    /// <summary>
    /// Assigns both fields from the hydrate payload (Story 3-3 D3 / AC3). Producer
    /// (<c>DensityEffects.HandleAppInitialized</c>) resolved <c>NewEffective</c> from the hydrated
    /// preference + current options + current viewport.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The density-hydrated action.</param>
    /// <returns>A new state carrying the hydrated preference and its resolved effective density.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceDensityHydrated(
        FrontComposerDensityState state,
        DensityHydratedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with {
            UserPreference = action.UserPreference,
            EffectiveDensity = action.NewEffective,
        };
    }

    /// <summary>
    /// Assigns only <c>EffectiveDensity</c>; <c>UserPreference</c> is PRESERVED across viewport
    /// transitions so widening back to Desktop re-applies the user's choice automatically
    /// (Story 3-3 D7 / ADR-040).
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The effective-density-recomputed action.</param>
    /// <returns>A new state carrying the recomputed effective density with the user preference intact.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceEffectiveDensityRecomputed(
        FrontComposerDensityState state,
        EffectiveDensityRecomputedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { EffectiveDensity = action.NewEffective };
    }

    /// <summary>
    /// Story 3-1 legacy entry point (Story 3-3 Task 2.4). Treats <c>NewDensity</c> as both the user
    /// preference and the effective density — caller is responsible for having resolved before
    /// dispatch. Kept non-breaking for Story 3-4's command-palette future caller.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The legacy density-changed action.</param>
    /// <returns>A new state with both fields set to <c>action.NewDensity</c>.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceDensityChanged(
        FrontComposerDensityState state,
        DensityChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with {
            UserPreference = action.NewDensity,
            EffectiveDensity = action.NewDensity,
        };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerDensityState.HydrationState"/> from <see cref="DensityHydrationState.Idle"/>
    /// to <see cref="DensityHydrationState.Hydrating"/> at the start of the hydrate path
    /// (Story 3-6 D19). No-op when already <c>Hydrated</c>.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The density-hydrating action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrating</c> when transitioning from <c>Idle</c>.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceDensityHydrating(
        FrontComposerDensityState state,
        DensityHydratingAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == DensityHydrationState.Hydrated
            ? state
            : state with { HydrationState = DensityHydrationState.Hydrating };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerDensityState.HydrationState"/> to <see cref="DensityHydrationState.Hydrated"/>
    /// at the end of the hydrate path (Story 3-6 D19). Called on BOTH happy and fail-closed paths.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The density-hydrated-completed action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrated</c>.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceDensityHydratedCompleted(
        FrontComposerDensityState state,
        DensityHydratedCompletedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == DensityHydrationState.Hydrated
            ? state
            : state with { HydrationState = DensityHydrationState.Hydrated };
    }
}
