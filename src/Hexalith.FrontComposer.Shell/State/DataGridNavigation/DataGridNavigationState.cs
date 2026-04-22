using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor state carrying per-view <see cref="GridViewSnapshot"/>s keyed by
/// <c>"{commandBoundedContext}:{projectionTypeFqn}"</c>. Story 2-2 ships the reducer-only surface
/// (Decision D30); Story 3-6 ADR-050 adds the effect half (persist, hydrate, on-demand restore).
/// </summary>
/// <param name="ViewStates">Immutable map of view-key to captured snapshot.</param>
/// <param name="Cap">
/// LRU eviction cap (Story 2-2 Decision D33). Seeded from <c>FcShellOptions.DataGridNavCap</c>
/// by <see cref="DataGridNavigationFeature"/> at first state construction. Embedding the cap in
/// state (Group D code review resolution of W1) keeps reducers pure and avoids cross-circuit
/// contamination that a mutable process-static would incur.
/// </param>
/// <param name="HydrationState">
/// Transient three-state hydration marker (Story 3-6 D19 / A7). Initial value <see cref="DataGridNavigationHydrationState.Idle"/>;
/// flips <c>Idle → Hydrating → Hydrated</c> via dedicated reducers. NEVER persisted. Re-hydrate
/// via <c>StorageReadyAction</c> only runs when this is <see cref="DataGridNavigationHydrationState.Idle"/>.
/// </param>
public sealed record DataGridNavigationState(
    ImmutableDictionary<string, GridViewSnapshot> ViewStates,
    int Cap = 50,
    DataGridNavigationHydrationState HydrationState = DataGridNavigationHydrationState.Idle);
