namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 D1 / D12 / AC1 — sort change dispatched by the generated view's
/// <c>FluentDataGrid</c> sort event. Reducer updates <c>GridViewSnapshot.SortColumn</c>
/// / <c>SortDescending</c> and chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record SortChangedAction {

    /// <summary>Initializes a new instance of the <see cref="SortChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="sortColumn">Declared property name of the sort column, or <see langword="null"/> to clear the sort.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public SortChangedAction(string viewKey, string? sortColumn, bool sortDescending) {
        ViewKey = viewKey;
        SortColumn = sortColumn;
        SortDescending = sortDescending;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the declared property name of the sort column, or <see langword="null"/> when the sort is cleared.</summary>
    public string? SortColumn { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    public bool SortDescending { get; init; }
}
