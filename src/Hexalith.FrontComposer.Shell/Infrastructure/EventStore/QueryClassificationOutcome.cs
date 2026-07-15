namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>Outcome category for <see cref="EventStoreQueryClassification"/>.</summary>
public enum QueryClassificationOutcome {
    /// <summary>HTTP 200 OK.</summary>
    Ok,

    /// <summary>HTTP 304 Not Modified.</summary>
    NotModified,

    /// <summary>Any non-200 / non-304 response — see <see cref="EventStoreQueryClassification.Failure"/>.</summary>
    Failure,
}
