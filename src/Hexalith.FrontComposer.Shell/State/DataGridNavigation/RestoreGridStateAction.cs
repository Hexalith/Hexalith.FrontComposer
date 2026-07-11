namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 2-2 renderer dispatches on mount; read-side action (reducer is a pure no-op, D30).
/// </summary>
public sealed record RestoreGridStateAction {

    /// <summary>Initializes a new instance of the <see cref="RestoreGridStateAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key that identifies which snapshot to restore.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public RestoreGridStateAction(string viewKey) => ViewKey = viewKey;

    /// <summary>Gets the stable per-view key that identifies which snapshot to restore.</summary>
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
