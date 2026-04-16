using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Captures the per-view DataGrid state that FullPage command forms restore on return navigation
/// (Story 2-2 AC7, ADR-015). Carried in <c>DataGridNavigationState</c>.
/// </summary>
public sealed record GridViewSnapshot(
    double ScrollTop,
    ImmutableDictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? ExpandedRowId,
    string? SelectedRowId,
    DateTimeOffset CapturedAt);

/// <summary>Fluxor action — Epic 4 producer; Story 2-2 reducer.</summary>
public sealed record CaptureGridStateAction(string ViewKey, GridViewSnapshot Snapshot);

/// <summary>
/// Story 2-2 renderer dispatches on mount; read-side action (reducer is a pure no-op, D30).
/// </summary>
public sealed record RestoreGridStateAction(string ViewKey);

/// <summary>Removes a captured snapshot for a view.</summary>
public sealed record ClearGridStateAction(string ViewKey);

/// <summary>Prunes snapshots whose <see cref="GridViewSnapshot.CapturedAt"/> is strictly before the threshold.</summary>
public sealed record PruneExpiredAction(DateTimeOffset Threshold);
