using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Fluxor action — Epic 4 producer; Story 2-2 reducer.</summary>
public sealed record CaptureGridStateAction {

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
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the captured grid state.</summary>
    public GridViewSnapshot Snapshot {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = null!;
}
