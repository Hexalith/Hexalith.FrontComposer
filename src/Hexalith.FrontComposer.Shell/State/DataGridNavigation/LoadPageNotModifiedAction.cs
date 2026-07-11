namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 5-2 D4 / AC4 — dispatched by <c>LoadPageEffects</c> when EventStore returns
/// 304 Not Modified for the requested page AND a compatible cached payload was reused via
/// <c>QueryResult&lt;T&gt;.IsNotModified</c>. The reducer resolves the matching TCS from
/// the cached items WITHOUT mutating <c>PagesByKey</c> / <c>TotalCountByKey</c> /
/// <c>LastElapsedMsByKey</c>, so the DataGrid renders no loading flash, no synthetic
/// success transition, no badge animation, and no success toast.
/// </summary>
public sealed record LoadPageNotModifiedAction {

    /// <summary>Initializes a new instance of the <see cref="LoadPageNotModifiedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset matching the triggering <see cref="LoadPageAction"/>.</param>
    /// <param name="cachedItems">The cached row payload reused from the local ETag cache; never null.</param>
    /// <param name="completion">Originating TCS, used to reject stale terminal actions for superseded same-page requests.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="cachedItems"/> is null.</exception>
    public LoadPageNotModifiedAction(
        string viewKey,
        int skip,
        IReadOnlyList<object> cachedItems,
        TaskCompletionSource<object>? completion = null) {
        ViewKey = viewKey;
        Skip = skip;
        CachedItems = cachedItems ?? throw new System.ArgumentNullException(nameof(cachedItems));
        Completion = completion;
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

    /// <summary>Gets the non-negative skip offset.</summary>
    public int Skip {
        get;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            field = value;
        }
    }

    /// <summary>Gets the cached row payload reused from the ETag cache (never null).</summary>
    public IReadOnlyList<object> CachedItems { get; init; } = System.Array.Empty<object>();

    /// <summary>Gets the originating TCS, used to reject stale terminal actions for superseded same-page requests.</summary>
    public TaskCompletionSource<object>? Completion { get; init; }
}
