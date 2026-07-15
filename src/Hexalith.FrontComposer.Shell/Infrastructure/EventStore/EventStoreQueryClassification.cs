namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 T6 — discriminated outcome for an EventStore query response.
/// </summary>
public sealed record EventStoreQueryClassification {
    private EventStoreQueryClassification(
        QueryClassificationOutcome outcome,
        string? eTag,
        Exception? failure) {
        Outcome = outcome;
        ETag = eTag;
        Failure = failure;
    }

    /// <summary>Gets the outcome category.</summary>
    public QueryClassificationOutcome Outcome { get; }

    /// <summary>Gets the response <c>ETag</c> validator when present.</summary>
    public string? ETag { get; }

    /// <summary>
    /// Gets the typed failure exception when <see cref="Outcome"/> is
    /// <see cref="QueryClassificationOutcome.Failure"/>; otherwise <see langword="null"/>.
    /// </summary>
    public Exception? Failure { get; }

    /// <summary>Builds an OK classification with the response ETag.</summary>
    public static EventStoreQueryClassification Ok(string? eTag)
        => new(QueryClassificationOutcome.Ok, eTag, failure: null);

    /// <summary>Builds a NotModified classification with the response ETag.</summary>
    public static EventStoreQueryClassification NotModified(string? eTag)
        => new(QueryClassificationOutcome.NotModified, eTag, failure: null);

    /// <summary>Builds a failure-classification carrying the typed exception.</summary>
    public static EventStoreQueryClassification FromFailure(Exception exception) {
        ArgumentNullException.ThrowIfNull(exception);
        return new(QueryClassificationOutcome.Failure, eTag: null, exception);
    }
}
