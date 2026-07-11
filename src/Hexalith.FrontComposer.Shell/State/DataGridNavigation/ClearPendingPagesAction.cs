namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 — dispatched from the generated view's <c>DisposeAsync</c>. Reducer sweeps every
/// <c>PendingCompletionsByKey</c> entry whose view-key component matches, calling
/// <c>TrySetCanceled</c> and removing each — preventing orphan TCS entries across route changes.
/// </summary>
public sealed record ClearPendingPagesAction {

    /// <summary>Initializes a new instance of the <see cref="ClearPendingPagesAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key whose pending TCS entries should be swept.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ClearPendingPagesAction(string viewKey) => ViewKey = viewKey;

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
