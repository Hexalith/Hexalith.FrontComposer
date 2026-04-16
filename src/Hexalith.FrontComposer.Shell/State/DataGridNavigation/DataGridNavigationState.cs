using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor state carrying per-view <see cref="GridViewSnapshot"/>s keyed by
/// <c>"{commandBoundedContext}:{projectionTypeFqn}"</c>. Story 2-2 ships the reducer-only surface
/// (Decision D30); persistence and DataGrid capture-side wiring land in Story 4.3.
/// </summary>
/// <param name="ViewStates">Immutable map of view-key to captured snapshot.</param>
/// <param name="Cap">
/// LRU eviction cap (Story 2-2 Decision D33). Seeded from <c>FcShellOptions.DataGridNavCap</c>
/// by <see cref="DataGridNavigationFeature"/> at first state construction. Embedding the cap in
/// state (Group D code review resolution of W1) keeps reducers pure and avoids cross-circuit
/// contamination that a mutable process-static would incur.
/// </param>
public sealed record DataGridNavigationState(
    ImmutableDictionary<string, GridViewSnapshot> ViewStates,
    int Cap = 50);
