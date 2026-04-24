using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Captures the per-view DataGrid state that FullPage command forms restore on return navigation
/// (Story 2-2 AC7, ADR-015). Carried in <c>DataGridNavigationState</c>.
/// </summary>
/// <remarks>
/// Equality is structural across all fields, including <see cref="Filters"/>. The synthesized
/// record equality is overridden because <see cref="IImmutableDictionary{TKey, TValue}"/> uses
/// reference equality by default — without the override, two snapshots with logically identical
/// filters but different dictionary instances would compare unequal, breaking Fluxor's
/// duplicate-state-update suppression.
/// </remarks>
public sealed record GridViewSnapshot {
    private readonly double _scrollTop;
    private readonly IImmutableDictionary<string, string> _filters = ImmutableDictionary<string, string>.Empty;
    private readonly DateTimeOffset _capturedAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridViewSnapshot"/> record.
    /// </summary>
    /// <param name="scrollTop">Vertical scroll offset of the DataGrid viewport. Must be a non-negative finite value (not NaN, not infinity).</param>
    /// <param name="filters">Active filter values keyed by column name. Must not be null; pass <see cref="ImmutableDictionary{TKey, TValue}.Empty"/> when there are no filters.</param>
    /// <param name="sortColumn">Column key currently sorted by, or <see langword="null"/> when unsorted.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <param name="expandedRowId">Identifier of the expanded row, or <see langword="null"/> when no row is expanded.</param>
    /// <param name="selectedRowId">Identifier of the selected row, or <see langword="null"/> when no row is selected.</param>
    /// <param name="capturedAt">UTC timestamp when the snapshot was captured. Used for LRU eviction and expiry pruning. Offset MUST be <see cref="TimeSpan.Zero"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="scrollTop"/> is NaN, infinity, or negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filters"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="capturedAt"/> has a non-UTC offset.</exception>
    public GridViewSnapshot(
        double scrollTop,
        IImmutableDictionary<string, string> filters,
        string? sortColumn,
        bool sortDescending,
        string? expandedRowId,
        string? selectedRowId,
        DateTimeOffset capturedAt) {
        ScrollTop = scrollTop;
        Filters = filters;
        SortColumn = sortColumn;
        SortDescending = sortDescending;
        ExpandedRowId = expandedRowId;
        SelectedRowId = selectedRowId;
        CapturedAt = capturedAt;
    }

    /// <summary>Gets the vertical scroll offset of the DataGrid viewport.</summary>
    public double ScrollTop {
        get => _scrollTop;
        init {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) {
                throw new ArgumentOutOfRangeException(nameof(value), value, "ScrollTop must be a non-negative finite value.");
            }

            _scrollTop = value;
        }
    }

    /// <summary>Gets the active filter values keyed by column name.</summary>
    /// <remarks>
    /// Story 4-3 D3 — two reserved keys pack non-column filter state into this dictionary without
    /// bumping the blob schema:
    /// <list type="bullet">
    /// <item><see cref="ReservedFilterKeys.StatusKey"/> (<c>__status</c>): CSV of active <c>BadgeSlot</c> names.</item>
    /// <item><see cref="ReservedFilterKeys.SearchKey"/> (<c>__search</c>): the current global search query.</item>
    /// </list>
    /// Every other key is a declared projection property name. Keys beginning with <c>__</c> are
    /// reserved for framework use.
    /// </remarks>
    public IImmutableDictionary<string, string> Filters {
        get => _filters;
        init => _filters = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the column key currently sorted by, or <see langword="null"/> when unsorted.</summary>
    public string? SortColumn { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    public bool SortDescending { get; init; }

    /// <summary>Gets the identifier of the expanded row, or <see langword="null"/> when no row is expanded.</summary>
    public string? ExpandedRowId { get; init; }

    /// <summary>Gets the identifier of the selected row, or <see langword="null"/> when no row is selected.</summary>
    public string? SelectedRowId { get; init; }

    /// <summary>Gets the UTC timestamp when the snapshot was captured.</summary>
    public DateTimeOffset CapturedAt {
        get => _capturedAt;
        init {
            if (value.Offset != TimeSpan.Zero) {
                throw new ArgumentException("CapturedAt offset must be TimeSpan.Zero (UTC).", nameof(value));
            }

            _capturedAt = value;
        }
    }

    /// <inheritdoc />
    public bool Equals(GridViewSnapshot? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return ScrollTop.Equals(other.ScrollTop)
            && SortDescending == other.SortDescending
            && string.Equals(SortColumn, other.SortColumn, StringComparison.Ordinal)
            && string.Equals(ExpandedRowId, other.ExpandedRowId, StringComparison.Ordinal)
            && string.Equals(SelectedRowId, other.SelectedRowId, StringComparison.Ordinal)
            && CapturedAt.Equals(other.CapturedAt)
            && FiltersEqual(Filters, other.Filters);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + ScrollTop.GetHashCode();
            hash = (hash * 31) + (SortColumn is null ? 0 : StringComparer.Ordinal.GetHashCode(SortColumn));
            hash = (hash * 31) + SortDescending.GetHashCode();
            hash = (hash * 31) + (ExpandedRowId is null ? 0 : StringComparer.Ordinal.GetHashCode(ExpandedRowId));
            hash = (hash * 31) + (SelectedRowId is null ? 0 : StringComparer.Ordinal.GetHashCode(SelectedRowId));
            hash = (hash * 31) + CapturedAt.GetHashCode();
            int filtersHash = 0;
            foreach (KeyValuePair<string, string> kvp in Filters) {
                filtersHash ^= (StringComparer.Ordinal.GetHashCode(kvp.Key) * 397) ^ StringComparer.Ordinal.GetHashCode(kvp.Value);
            }

            hash = (hash * 31) + filtersHash;
            return hash;
        }
    }

    private static bool FiltersEqual(IImmutableDictionary<string, string> left, IImmutableDictionary<string, string> right) {
        if (ReferenceEquals(left, right)) {
            return true;
        }

        if (left.Count != right.Count) {
            return false;
        }

        foreach (KeyValuePair<string, string> kvp in left) {
            if (!right.TryGetValue(kvp.Key, out string? rightValue) || !string.Equals(kvp.Value, rightValue, StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Fluxor action — Epic 4 producer; Story 2-2 reducer.</summary>
public sealed record CaptureGridStateAction {
    private readonly string _viewKey = string.Empty;
    private readonly GridViewSnapshot _snapshot = null!;

    /// <summary>Initializes a new instance of the <see cref="CaptureGridStateAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key that identifies the snapshot bucket.</param>
    /// <param name="snapshot">The captured grid state.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public CaptureGridStateAction(string viewKey, GridViewSnapshot snapshot) {
        ViewKey = viewKey;
        Snapshot = snapshot;
    }

    /// <summary>Gets the stable per-view key that identifies the snapshot bucket.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }

    /// <summary>Gets the captured grid state.</summary>
    public GridViewSnapshot Snapshot {
        get => _snapshot;
        init => _snapshot = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>
/// Story 2-2 renderer dispatches on mount; read-side action (reducer is a pure no-op, D30).
/// </summary>
public sealed record RestoreGridStateAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="RestoreGridStateAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key that identifies which snapshot to restore.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public RestoreGridStateAction(string viewKey) {
        ViewKey = viewKey;
    }

    /// <summary>Gets the stable per-view key that identifies which snapshot to restore.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }
}

/// <summary>Removes a captured snapshot for a view.</summary>
public sealed record ClearGridStateAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ClearGridStateAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key whose snapshot should be removed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ClearGridStateAction(string viewKey) {
        ViewKey = viewKey;
    }

    /// <summary>Gets the stable per-view key whose snapshot should be removed.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }
}

/// <summary>Prunes snapshots whose <see cref="GridViewSnapshot.CapturedAt"/> is strictly before the threshold.</summary>
/// <remarks>
/// <see cref="Threshold"/> offset MUST be <see cref="TimeSpan.Zero"/> (UTC); the ctor rejects other
/// offsets so pruning cannot silently become non-deterministic. <c>default(DateTimeOffset)</c>
/// (= <see cref="DateTimeOffset.MinValue"/>) prunes nothing; <see cref="DateTimeOffset.MaxValue"/>
/// prunes every snapshot.
/// </remarks>
public sealed record PruneExpiredAction {
    private readonly DateTimeOffset _threshold;

    /// <summary>Initializes a new instance of the <see cref="PruneExpiredAction"/> record.</summary>
    /// <param name="threshold">UTC threshold; snapshots captured strictly before this moment are pruned.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="threshold"/> has a non-UTC offset.</exception>
    public PruneExpiredAction(DateTimeOffset threshold) {
        Threshold = threshold;
    }

    /// <summary>Gets the UTC threshold below which snapshots are pruned.</summary>
    public DateTimeOffset Threshold {
        get => _threshold;
        init {
            if (value.Offset != TimeSpan.Zero) {
                throw new ArgumentException("Threshold offset must be TimeSpan.Zero (UTC).", nameof(value));
            }

            _threshold = value;
        }
    }
}
