using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Options for the opt-in EventStore-backed FrontComposer communication services.
/// </summary>
public sealed class EventStoreOptions {
    /// <summary>
    /// Gets or sets the EventStore service base address.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the command submission endpoint path.
    /// </summary>
    public string CommandEndpointPath { get; set; } = "/api/v1/commands";

    /// <summary>
    /// Gets or sets the query execution endpoint path.
    /// </summary>
    public string QueryEndpointPath { get; set; } = "/api/v1/queries";

    /// <summary>
    /// Gets or sets the projection changes SignalR hub path.
    /// </summary>
    public string ProjectionChangesHubPath { get; set; } = "/hubs/projection-changes";

    /// <summary>
    /// Gets or sets the outbound HTTP timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of ETag validators allowed on one query request.
    /// </summary>
    public int MaxETagCount { get; set; } = EventStoreValidation.MaxETagValidators;

    /// <summary>
    /// Gets or sets the maximum serialized UTF-8 body size accepted before send.
    /// </summary>
    public int MaxRequestBytes { get; set; } = EventStoreValidation.DefaultMaxRequestBytes;

    /// <summary>
    /// Gets or sets a value indicating whether a bearer token must be provided before sending.
    /// </summary>
    public bool RequireAccessToken { get; set; } = true;

    /// <summary>
    /// Gets or sets the per-operation bearer token provider.
    /// </summary>
    public Func<CancellationToken, ValueTask<string?>>? AccessTokenProvider { get; set; }
}
