using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Dispatched by <c>DataGridNavigationEffects.HandleAppInitialized</c> (per-key during enumeration)
/// and <c>HandleRestoreGridState</c> (on-demand) after successfully reading and deserialising a
/// <see cref="GridViewSnapshot"/> from storage (Story 3-6 D7 / D8 / ADR-050).
/// </summary>
/// <remarks>
/// Reducer <c>DataGridNavigationReducers.ReduceGridViewHydrated</c> inserts the snapshot into
/// <see cref="DataGridNavigationState.ViewStates"/> iff the key is absent — in-memory state wins
/// over storage so a more recent in-circuit capture is not overwritten by a stale cross-tab blob.
/// </remarks>
/// <param name="ViewKey">The <c>"{boundedContext}:{projectionTypeFqn}"</c> Story 2-2 per-view key.</param>
/// <param name="Snapshot">The hydrated snapshot (converted from <c>GridViewPersistenceBlob</c>).</param>
public sealed record GridViewHydratedAction(string ViewKey, GridViewSnapshot Snapshot);

/// <summary>
/// Dispatched by <c>DataGridNavigationEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c>
/// at the start of the hydrate path (Story 3-6 D19 / A7). Reducer flips
/// <see cref="DataGridNavigationState.HydrationState"/> from <see cref="DataGridNavigationHydrationState.Idle"/>
/// to <see cref="DataGridNavigationHydrationState.Hydrating"/>. NEVER persisted.
/// </summary>
public sealed record DataGridNavigationHydratingAction;

/// <summary>
/// Dispatched by <c>DataGridNavigationEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c>
/// as the final step of the hydrate path (Story 3-6 D19 / A7). Reducer flips
/// <see cref="DataGridNavigationState.HydrationState"/> to <see cref="DataGridNavigationHydrationState.Hydrated"/>.
/// Called on BOTH happy path AND fail-closed path.
/// </summary>
public sealed record DataGridNavigationHydratedCompletedAction;
