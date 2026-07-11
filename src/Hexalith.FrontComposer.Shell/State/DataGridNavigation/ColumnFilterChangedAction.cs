using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 D1 / AC1 — column-filter change dispatched by <c>FcColumnFilterCell</c>
/// after its 300 ms debounce. Reducer chains into <see cref="CaptureGridStateAction"/>
/// so Story 3-6's effect persists without a parallel write path.
/// </summary>
public sealed record ColumnFilterChangedAction {

    /// <summary>Initializes a new instance of the <see cref="ColumnFilterChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</param>
    /// <param name="columnKey">Declared property name of the filtered column.</param>
    /// <param name="filterValue">The new filter value, or <see langword="null"/> to clear the filter.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> or <paramref name="columnKey"/> is null, empty, or whitespace.</exception>
    public ColumnFilterChangedAction(string viewKey, string columnKey, string? filterValue) {
        ViewKey = viewKey;
        ColumnKey = columnKey;
        FilterValue = filterValue;
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

    /// <summary>Gets the declared property name of the filtered column.</summary>
    /// <remarks>
    /// Review pass 2 — dispatching with a key starting with <c>__</c> is rejected at the record
    /// boundary so reserved-key packing (<see cref="ReservedFilterKeys"/>) cannot be spoofed via a
    /// direct action dispatch that bypasses the reducer/effect guard.
    /// </remarks>
    public string ColumnKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Column key cannot be null, empty, or whitespace.", nameof(value));
            }

            if (value.StartsWith("__", System.StringComparison.Ordinal)) {
                throw new System.ArgumentException(
                    "Column key must not start with '__' — that prefix is reserved for framework-managed filter keys (see ReservedFilterKeys).",
                    nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the filter value, or <see langword="null"/> when the user cleared the input.</summary>
    public string? FilterValue { get; init; }
}
