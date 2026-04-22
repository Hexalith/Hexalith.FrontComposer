using System.Collections.Immutable;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

/// <summary>
/// Pure reducers for <see cref="FrontComposerCapabilityDiscoveryState"/> (Story 3-5 D8 / ADR-046).
/// </summary>
public static class CapabilityDiscoveryReducers {
    /// <summary>
    /// Publishes the seeded counts snapshot and flips
    /// <see cref="FrontComposerCapabilityDiscoveryState.HydrationState"/> to
    /// <see cref="CapabilityDiscoveryHydrationState.Seeded"/>. Any live updates that landed while
    /// the initial seed was still in flight win for their individual keys.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The seeded action carrying the counts dictionary.</param>
    /// <returns>A new state with seeded counts.</returns>
    [ReducerMethod]
    public static FrontComposerCapabilityDiscoveryState ReduceBadgeCountsSeeded(
        FrontComposerCapabilityDiscoveryState state,
        BadgeCountsSeededAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableDictionary<Type, int> mergedCounts = action.Counts;
        if (!state.Counts.IsEmpty) {
            mergedCounts = action.Counts.SetItems(state.Counts);
        }

        return state with {
            Counts = mergedCounts,
            HydrationState = CapabilityDiscoveryHydrationState.Seeded,
        };
    }

    /// <summary>
    /// Applies a single-key update to <see cref="FrontComposerCapabilityDiscoveryState.Counts"/>
    /// and promotes the hydration lifecycle to <see cref="CapabilityDiscoveryHydrationState.Seeding"/>
    /// until the seed snapshot is finalized.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The change action.</param>
    /// <returns>A new state with the single-key update applied.</returns>
    [ReducerMethod]
    public static FrontComposerCapabilityDiscoveryState ReduceBadgeCountChanged(
        FrontComposerCapabilityDiscoveryState state,
        BadgeCountChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        CapabilityDiscoveryHydrationState hydrationState = state.HydrationState == CapabilityDiscoveryHydrationState.Seeded
            ? CapabilityDiscoveryHydrationState.Seeded
            : CapabilityDiscoveryHydrationState.Seeding;
        return state with {
            Counts = state.Counts.SetItem(action.ProjectionType, action.NewCount),
            HydrationState = hydrationState,
        };
    }

    /// <summary>
    /// Adds the visited capability id to <see cref="FrontComposerCapabilityDiscoveryState.SeenCapabilities"/>.
    /// Idempotent: re-dispatching with an already-seen id returns a state with the same set
    /// instance (<see cref="System.Collections.Immutable.ImmutableHashSet{T}.Add"/> short-circuit).
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The visited action.</param>
    /// <returns>A new state with the visited id in the seen-set.</returns>
    [ReducerMethod]
    public static FrontComposerCapabilityDiscoveryState ReduceCapabilityVisited(
        FrontComposerCapabilityDiscoveryState state,
        CapabilityVisitedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { SeenCapabilities = state.SeenCapabilities.Add(action.CapabilityId) };
    }

    /// <summary>
    /// Replaces <see cref="FrontComposerCapabilityDiscoveryState.SeenCapabilities"/> wholesale
    /// from the hydrated blob.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The hydrated action carrying the seen-set.</param>
    /// <returns>A new state with the hydrated seen-set.</returns>
    [ReducerMethod]
    public static FrontComposerCapabilityDiscoveryState ReduceSeenCapabilitiesHydrated(
        FrontComposerCapabilityDiscoveryState state,
        SeenCapabilitiesHydratedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { SeenCapabilities = action.SeenCapabilities };
    }
}
