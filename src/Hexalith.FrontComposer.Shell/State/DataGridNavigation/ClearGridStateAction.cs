namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Removes a captured snapshot for a view.</summary>
public sealed record ClearGridStateAction {

    /// <summary>Initializes a new instance of the <see cref="ClearGridStateAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key whose snapshot should be removed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ClearGridStateAction(string viewKey) => ViewKey = viewKey;

    /// <summary>Gets the stable per-view key whose snapshot should be removed.</summary>
    public string ViewKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;
}
