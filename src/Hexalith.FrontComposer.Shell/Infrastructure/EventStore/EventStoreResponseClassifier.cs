using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 D12 / T6 — single Shell-side helper that classifies EventStore command and
/// query responses into typed outcomes. Centralised so query clients, badge readers,
/// projection page loaders, and generated forms cannot drift on raw HTTP status parsing,
/// ProblemDetails decoding, <c>Retry-After</c> handling, or <c>ETag</c> extraction.
/// </summary>
/// <remarks>
/// <para>
/// Command and query outcomes are kept separate (Story 5-2 party-mode finding):
/// <see cref="ClassifyCommandAsync"/> emits an exception taxonomy aligned to the
/// command-form UX (validation / warning / auth-redirect / domain-rejection); the
/// query-side <see cref="ClassifyQueryAsync"/> exposes
/// <see cref="QueryResult{T}.NotModified"/> + ETag metadata + a typed
/// <see cref="QueryFailureException"/> for non-200 / non-304 / non-401 statuses.
/// </para>
/// <para>
/// The classifier intentionally treats <c>503 Service Unavailable</c> the same as any other
/// non-204 5xx response: a network-class exception is propagated rather than mapped to a
/// warning banner. Reconnect / polling fallback UX belongs to Stories 5-3 through 5-5.
/// </para>
/// </remarks>
public sealed class EventStoreResponseClassifier {
    private const int MaxProblemDetailsBytes = 64 * 1024;
    private const int MaxProblemDetailsFields = 32;
    private const int MaxProblemDetailsMessagesPerField = 8;
    private const int MaxProblemDetailsGlobalMessages = 16;
    private const int MaxProblemDetailsStringLength = 512;

    private readonly ILogger<EventStoreResponseClassifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreResponseClassifier"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic miss / parse-warning emissions.</param>
    public EventStoreResponseClassifier(ILogger<EventStoreResponseClassifier> logger) {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Classifies an EventStore command response.
    /// </summary>
    /// <param name="response">The HTTP response message — never null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EventStoreCommandClassification"/> describing the response. For 202
    /// Accepted a non-throwing classification is returned with optional correlationId,
    /// location, and retry-after metadata; for 400/401/403/404/409/429 a typed exception is
    /// surfaced through <see cref="EventStoreCommandClassification.Failure"/>.
    /// </returns>
    public async Task<EventStoreCommandClassification> ClassifyCommandAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(response);

        TimeSpan? retryAfter = ResolveRetryAfter(response.Headers.RetryAfter);
        switch (response.StatusCode) {
            case HttpStatusCode.Accepted:
                return EventStoreCommandClassification.Accepted(
                    correlationId: TryGetHeader(response.Headers, "X-Correlation-ID"),
                    location: response.Headers.Location,
                    retryAfter: retryAfter);

            case HttpStatusCode.BadRequest: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreCommandClassification.FromFailure(new CommandValidationException(problem));
            }

            case HttpStatusCode.Unauthorized: {
                await DrainBodyAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreCommandClassification.FromFailure(new AuthRedirectRequiredException());
            }

            case HttpStatusCode.Forbidden: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreCommandClassification.FromFailure(
                    new CommandWarningException(CommandWarningKind.Forbidden, problem));
            }

            case HttpStatusCode.NotFound: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreCommandClassification.FromFailure(
                    new CommandWarningException(CommandWarningKind.NotFound, problem));
            }

            case HttpStatusCode.Conflict: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                string reason = string.IsNullOrWhiteSpace(problem.Title)
                    ? (problem.Detail ?? "The command was rejected by the server.")
                    : problem.Title!;
                string resolution = problem.Detail ?? string.Empty;
                return EventStoreCommandClassification.FromFailure(
                    new CommandRejectedException(reason, resolution));
            }

            case (HttpStatusCode)429: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreCommandClassification.FromFailure(
                    new CommandWarningException(CommandWarningKind.RateLimited, problem, retryAfter));
            }

            default:
                return EventStoreCommandClassification.FromFailure(
                    new HttpRequestException(
                        $"EventStore command did not return 202 Accepted (status {(int)response.StatusCode}).",
                        inner: null,
                        statusCode: response.StatusCode));
        }
    }

    /// <summary>
    /// Classifies an EventStore query response.
    /// </summary>
    /// <param name="response">The HTTP response message — never null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EventStoreQueryClassification"/> describing the response. For
    /// 200 / 304 the classification carries the <c>ETag</c> validator and an
    /// <see cref="EventStoreQueryClassification.Outcome"/> distinguishing fresh vs cached;
    /// for 401/403/404/429 the classification surfaces a typed exception.
    /// </returns>
    public async Task<EventStoreQueryClassification> ClassifyQueryAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(response);

        string? eTag = response.Headers.ETag?.ToString() ?? TryGetHeader(response.Headers, "ETag");
        TimeSpan? retryAfter = ResolveRetryAfter(response.Headers.RetryAfter);

        switch (response.StatusCode) {
            case HttpStatusCode.OK:
                return EventStoreQueryClassification.Ok(eTag);

            case HttpStatusCode.NotModified:
                return EventStoreQueryClassification.NotModified(eTag);

            case HttpStatusCode.Unauthorized: {
                await DrainBodyAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreQueryClassification.FromFailure(new AuthRedirectRequiredException());
            }

            case HttpStatusCode.Forbidden: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreQueryClassification.FromFailure(
                    new QueryFailureException(QueryFailureKind.Forbidden, problem));
            }

            case HttpStatusCode.NotFound: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreQueryClassification.FromFailure(
                    new QueryFailureException(QueryFailureKind.NotFound, problem));
            }

            case (HttpStatusCode)429: {
                ProblemDetailsPayload problem = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
                return EventStoreQueryClassification.FromFailure(
                    new QueryFailureException(QueryFailureKind.RateLimited, problem, retryAfter));
            }

            default:
                return EventStoreQueryClassification.FromFailure(
                    new HttpRequestException(
                        $"EventStore query did not return 200 OK or 304 Not Modified (status {(int)response.StatusCode}).",
                        inner: null,
                        statusCode: response.StatusCode));
        }
    }

    [SuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "ProblemDetails decoding goes through System.Text.Json web defaults; the ProblemDetailsPayload is a single concrete type used by the Shell adapter.")]
    private async Task<ProblemDetailsPayload> ReadProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) {
        if (response.Content is null
            || (response.Content.Headers.ContentLength is { } length && length == 0)) {
            return ProblemDetailsPayload.Empty;
        }

        if (response.Content.Headers.ContentLength is { } contentLength
            && contentLength > MaxProblemDetailsBytes) {
            _logger.LogWarning(
                "EventStoreResponseClassifier: ProblemDetails body exceeded {MaxBytes} bytes (Content-Type={ContentType}); falling back to empty payload.",
                MaxProblemDetailsBytes,
                response.Content.Headers.ContentType?.MediaType);
            return ProblemDetailsPayload.Empty;
        }

        try {
            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using MemoryStream bounded = new();
            byte[] buffer = new byte[4096];
            while (true) {
                int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0) {
                    break;
                }

                if (bounded.Length + read > MaxProblemDetailsBytes) {
                    _logger.LogWarning(
                        "EventStoreResponseClassifier: ProblemDetails body exceeded {MaxBytes} bytes while reading (Content-Type={ContentType}); falling back to empty payload.",
                        MaxProblemDetailsBytes,
                        response.Content.Headers.ContentType?.MediaType);
                    return ProblemDetailsPayload.Empty;
                }

                bounded.Write(buffer, 0, read);
            }

            bounded.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync(bounded, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ParseProblemDetails(document.RootElement);
        }
        catch (JsonException ex) {
            // Body intentionally not echoed: JsonException.Message can leak server payload fragments.
            _logger.LogWarning(
                ex,
                "EventStoreResponseClassifier: ProblemDetails parse failed (Content-Type={ContentType}); falling back to empty payload.",
                response.Content?.Headers.ContentType?.MediaType);
            return ProblemDetailsPayload.Empty;
        }
    }

    private static ProblemDetailsPayload ParseProblemDetails(JsonElement root) {
        if (root.ValueKind != JsonValueKind.Object) {
            return ProblemDetailsPayload.Empty;
        }

        string? title = TryGetString(root, "title");
        string? detail = TryGetString(root, "detail");
        int? status = TryGetInt(root, "status");
        string? entityLabel = TryGetString(root, "entityLabel");

        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (root.TryGetProperty("errors", out JsonElement errorsElement)
            && errorsElement.ValueKind == JsonValueKind.Object) {
            foreach (JsonProperty entry in errorsElement.EnumerateObject()) {
                if (errors.Count >= MaxProblemDetailsFields) {
                    break;
                }

                List<string> messages = new();
                switch (entry.Value.ValueKind) {
                    case JsonValueKind.String:
                        if (TryLimitString(entry.Value.GetString()) is { Length: > 0 } single) {
                            messages.Add(single);
                        }

                        break;
                    case JsonValueKind.Array:
                        foreach (JsonElement message in entry.Value.EnumerateArray()) {
                            if (messages.Count >= MaxProblemDetailsMessagesPerField) {
                                break;
                            }

                            if (message.ValueKind == JsonValueKind.String
                                && TryLimitString(message.GetString()) is { Length: > 0 } text) {
                                messages.Add(text);
                            }
                        }

                        break;
                }

                if (messages.Count > 0) {
                    errors[entry.Name] = messages;
                }
            }
        }

        List<string> globals = new();
        if (root.TryGetProperty("globalErrors", out JsonElement globalsElement)
            && globalsElement.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement message in globalsElement.EnumerateArray()) {
                if (globals.Count >= MaxProblemDetailsGlobalMessages) {
                    break;
                }

                if (message.ValueKind == JsonValueKind.String
                    && TryLimitString(message.GetString()) is { Length: > 0 } text) {
                    globals.Add(text);
                }
            }
        }

        return new ProblemDetailsPayload(
            Title: title,
            Detail: detail,
            Status: status,
            EntityLabel: entityLabel,
            ValidationErrors: errors,
            GlobalErrors: globals);
    }

    private static string? TryGetString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            ? TryLimitString(value.GetString())
            : null;

    private static int? TryGetInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int result)
            ? result
            : null;

    private static async Task DrainBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        try {
            if (response.Content is null) {
                return;
            }

            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            byte[] discard = new byte[1024];
            while (await stream.ReadAsync(discard.AsMemory(), cancellationToken).ConfigureAwait(false) > 0) {
                // intentionally drain — keep the underlying socket reusable.
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception) {
            // Drain is best-effort; failure here is irrelevant to the caller.
        }
    }

    private static TimeSpan? ResolveRetryAfter(RetryConditionHeaderValue? header) {
        if (header is null) {
            return null;
        }

        if (header.Delta is { } delta) {
            return delta;
        }

        if (header.Date is { } date) {
            TimeSpan diff = date - DateTimeOffset.UtcNow;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }

        return null;
    }

    private static string? TryGetHeader(HttpResponseHeaders headers, string name)
        => headers.TryGetValues(name, out IEnumerable<string>? values) ? values.FirstOrDefault() : null;

    private static string? TryLimitString(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return value;
        }

        return value.Length <= MaxProblemDetailsStringLength
            ? value
            : value[..MaxProblemDetailsStringLength];
    }
}

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

/// <summary>Outcome category for <see cref="EventStoreQueryClassification"/>.</summary>
public enum QueryClassificationOutcome {
    /// <summary>HTTP 200 OK.</summary>
    Ok,

    /// <summary>HTTP 304 Not Modified.</summary>
    NotModified,

    /// <summary>Any non-200 / non-304 response — see <see cref="EventStoreQueryClassification.Failure"/>.</summary>
    Failure,
}
