namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 T6 — discriminated outcome for an EventStore command response.
/// </summary>
public sealed record EventStoreCommandClassification {
    private EventStoreCommandClassification(
        bool isAccepted,
        string? correlationId,
        Uri? location,
        TimeSpan? retryAfter,
        Exception? failure) {
        IsAccepted = isAccepted;
        CorrelationId = correlationId;
        Location = location;
        RetryAfter = retryAfter;
        Failure = failure;
    }

    /// <summary>Gets a value indicating whether the command was accepted (HTTP 202).</summary>
    public bool IsAccepted { get; }

    /// <summary>Gets the optional correlation identifier returned by the server.</summary>
    public string? CorrelationId { get; }

    /// <summary>Gets the optional status-resource location returned by the server.</summary>
    public Uri? Location { get; }

    /// <summary>Gets the optional retry hint.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Gets the typed failure exception when <see cref="IsAccepted"/> is <see langword="false"/>;
    /// otherwise <see langword="null"/>.
    /// </summary>
    public Exception? Failure { get; }

    /// <summary>Builds an accepted-classification with optional correlation / location / retry metadata.</summary>
    public static EventStoreCommandClassification Accepted(
        string? correlationId = null,
        Uri? location = null,
        TimeSpan? retryAfter = null)
        => new(isAccepted: true, correlationId, location, retryAfter, failure: null);

    /// <summary>Builds a failure-classification carrying the typed exception.</summary>
    public static EventStoreCommandClassification FromFailure(Exception exception) {
        ArgumentNullException.ThrowIfNull(exception);
        return new(isAccepted: false, correlationId: null, location: null, retryAfter: null, failure: exception);
    }
}
