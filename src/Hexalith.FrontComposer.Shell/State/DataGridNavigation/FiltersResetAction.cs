namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 D1 / D17 / AC3 — reset dispatched by <c>FcFilterResetButton</c>.
/// Reducer empties every filter / sort field on the snapshot and chains
/// <see cref="ClearGridStateAction"/> so Story 3-6's effect removes the blob.
/// </summary>
public sealed record FiltersResetAction {

    /// <summary>Initializes a new instance of the <see cref="FiltersResetAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public FiltersResetAction(string viewKey) => ViewKey = viewKey;

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
}
