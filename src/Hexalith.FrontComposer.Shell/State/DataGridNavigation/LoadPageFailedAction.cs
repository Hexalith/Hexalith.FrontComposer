namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects</c> (or its defensive finally) when
/// <c>IQueryService.QueryAsync</c> throws. Reducer resolves the matching TCS via
/// <c>TrySetException</c> and removes the entry.
/// </summary>
public sealed record LoadPageFailedAction {

    /// <summary>Initializes a new instance of the <see cref="LoadPageFailedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <param name="errorMessage">Human-readable error message (never null or whitespace).</param>
    /// <exception cref="System.ArgumentException">Thrown on invalid <paramref name="viewKey"/> or <paramref name="errorMessage"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative.</exception>
    public LoadPageFailedAction(string viewKey, int skip, string errorMessage, TaskCompletionSource<object>? completion = null) {
        ViewKey = viewKey;
        Skip = skip;
        ErrorMessage = errorMessage;
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

    /// <summary>Gets the human-readable error message.</summary>
    public string ErrorMessage {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Error message cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the originating TCS, used to reject stale terminal actions for superseded same-page requests.</summary>
    public TaskCompletionSource<object>? Completion { get; init; }
}
