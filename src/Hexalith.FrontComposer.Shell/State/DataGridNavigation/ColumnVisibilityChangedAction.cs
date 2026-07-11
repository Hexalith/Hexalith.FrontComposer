using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D7 / AC5 — dispatched when the user toggles a column's visibility via
/// <c>FcColumnPrioritizer</c>. The PURE reducer reads / writes the CSV at
/// <c>GridViewSnapshot.Filters["__hidden"]</c>; the persistence effect chains
/// <see cref="CaptureGridStateAction"/> separately.
/// </summary>
public sealed record ColumnVisibilityChangedAction {

    /// <summary>Initializes a new instance of the <see cref="ColumnVisibilityChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="columnKey">Declared property name of the toggled column (must not start with <c>__</c>).</param>
    /// <param name="isVisible"><see langword="true"/> to make visible, <see langword="false"/> to hide.</param>
    /// <exception cref="System.ArgumentException">Thrown when either argument is null/empty/whitespace or <paramref name="columnKey"/> starts with <c>__</c>.</exception>
    public ColumnVisibilityChangedAction(string viewKey, string columnKey, bool isVisible) {
        ViewKey = viewKey;
        ColumnKey = columnKey;
        IsVisible = isVisible;
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

    /// <summary>Gets the declared column key.</summary>
    /// <remarks>
    /// Keys starting with <c>__</c> are reserved (see <see cref="ReservedFilterKeys"/> /
    /// <see cref="VirtualizationReservedKeys"/>); the record rejects them at construction to
    /// prevent reserved-key spoofing via direct action dispatch.
    /// </remarks>
    public string ColumnKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Column key cannot be null, empty, or whitespace.", nameof(value));
            }

            if (value.StartsWith("__", System.StringComparison.Ordinal)) {
                throw new System.ArgumentException(
                    "Column key must not start with '__' — that prefix is reserved for framework-managed filter keys.",
                    nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets a value indicating whether the column should be visible after this action.</summary>
    public bool IsVisible { get; init; }
}
