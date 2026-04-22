using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Serialisation shape for a per-view <see cref="GridViewSnapshot"/> (Story 3-6 D5 / ADR-050).
/// Uses <see cref="Dictionary{TKey, TValue}"/> (not <see cref="IImmutableDictionary{TKey, TValue}"/>)
/// for <see cref="System.Text.Json.JsonSerializer"/> compatibility without custom converters —
/// matches the <c>NavigationPersistenceBlob.CollapsedGroups</c> precedent (Story 3-2 D21).
/// </summary>
/// <remarks>
/// Conversion helpers <see cref="FromSnapshot"/> + <see cref="ToSnapshot"/> sit on the effect
/// boundary so reducers continue to see immutable snapshots while the wire format stays mutable.
/// Schema is pinned via <c>GridViewPersistenceBlobSchemaLockedTests.BlobSerializesToExpectedJsonShape</c>.
/// </remarks>
/// <param name="ScrollTop">Vertical scroll offset (non-negative finite).</param>
/// <param name="Filters">Active filter values keyed by column name.</param>
/// <param name="SortColumn">Column key currently sorted by, or <see langword="null"/> when unsorted.</param>
/// <param name="SortDescending">Whether the sort is descending.</param>
/// <param name="ExpandedRowId">Identifier of the expanded row, or <see langword="null"/>.</param>
/// <param name="SelectedRowId">Identifier of the selected row, or <see langword="null"/>.</param>
/// <param name="CapturedAt">UTC timestamp when the snapshot was captured.</param>
public sealed record GridViewPersistenceBlob(
    double ScrollTop,
    Dictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? ExpandedRowId,
    string? SelectedRowId,
    DateTimeOffset CapturedAt) {
    /// <summary>
    /// Converts a <see cref="GridViewSnapshot"/> to its persistence-wire shape.
    /// The snapshot's <see cref="IImmutableDictionary{TKey, TValue}"/> is copied into a new
    /// <see cref="Dictionary{TKey, TValue}"/> with ordinal string comparison.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <returns>A blob carrying the same field values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public static GridViewPersistenceBlob FromSnapshot(GridViewSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);
        Dictionary<string, string> filters = new(snapshot.Filters.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> kvp in snapshot.Filters) {
            filters[kvp.Key] = kvp.Value;
        }

        return new GridViewPersistenceBlob(
            ScrollTop: snapshot.ScrollTop,
            Filters: filters,
            SortColumn: snapshot.SortColumn,
            SortDescending: snapshot.SortDescending,
            ExpandedRowId: snapshot.ExpandedRowId,
            SelectedRowId: snapshot.SelectedRowId,
            CapturedAt: snapshot.CapturedAt);
    }

    /// <summary>
    /// Converts the persisted blob back to an immutable <see cref="GridViewSnapshot"/>.
    /// A null or missing <c>Filters</c> dictionary is treated as empty (defensive — post-3-6
    /// writes always carry a dictionary).
    /// </summary>
    /// <returns>A fully-initialised snapshot.</returns>
    public GridViewSnapshot ToSnapshot() {
        IImmutableDictionary<string, string> filters = Filters is null
            ? ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal)
            : ImmutableDictionary.CreateRange(StringComparer.Ordinal, Filters);
        // Normalise CapturedAt to UTC and clamp ScrollTop to a finite non-negative value.
        // Prevents the GridViewSnapshot init-setter from rejecting blobs with logically-equivalent
        // non-zero offsets (+00:00 vs Z) or values drifted from tampering / migration.
        DateTimeOffset capturedAtUtc = CapturedAt.ToUniversalTime();
        double scrollTop = double.IsFinite(ScrollTop) && ScrollTop >= 0 ? ScrollTop : 0;
        return new GridViewSnapshot(
            scrollTop: scrollTop,
            filters: filters,
            sortColumn: SortColumn,
            sortDescending: SortDescending,
            expandedRowId: ExpandedRowId,
            selectedRowId: SelectedRowId,
            capturedAt: capturedAtUtc);
    }
}
