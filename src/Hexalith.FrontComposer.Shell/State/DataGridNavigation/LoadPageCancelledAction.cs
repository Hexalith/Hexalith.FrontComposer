namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects</c> on
/// <c>cancellationToken.Register</c> callback OR by the double-registration idempotency guard.
/// Reducer resolves the matching TCS via <c>TrySetCanceled</c> and removes the entry.
/// </summary>
public sealed record LoadPageCancelledAction {

    /// <summary>Initializes a new instance of the <see cref="LoadPageCancelledAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative.</exception>
    public LoadPageCancelledAction(string viewKey, int skip, TaskCompletionSource<object>? completion = null) {
        ViewKey = viewKey;
        Skip = skip;
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

    /// <summary>Gets the originating TCS, used to reject stale terminal actions for superseded same-page requests.</summary>
    public TaskCompletionSource<object>? Completion { get; init; }
}
