using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor state carrying per-view <see cref="GridViewSnapshot"/>s keyed by
/// <c>"{commandBoundedContext}:{projectionTypeFqn}"</c>. Story 2-2 ships the reducer-only surface
/// (Decision D30); persistence and DataGrid capture-side wiring land in Story 4.3.
/// </summary>
/// <param name="ViewStates">Immutable map of view-key to captured snapshot.</param>
public sealed record DataGridNavigationState(
    ImmutableDictionary<string, GridViewSnapshot> ViewStates);
