using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default EventStore-backed query service. Story 5-2 routes every response through
/// <see cref="EventStoreResponseClassifier"/> so consumers see typed outcomes
/// (<see cref="QueryResult{T}.NotModified(string?)"/>,
/// <see cref="QueryResult{T}.NotModifiedFromCache"/>,
/// <see cref="QueryFailureException"/>, <see cref="AuthRedirectRequiredException"/>) rather
/// than stringly-typed <see cref="HttpRequestException"/>s. When
/// <see cref="QueryRequest.CacheDiscriminator"/> is supplied and accepted by the framework
/// allowlist, the client integrates with <see cref="IETagCache"/>: <c>If-None-Match</c> is
/// emitted from the cached entry, 200 OK responses are written through the cache
/// fire-and-forget, and 304 Not Modified responses reuse cached payload while preserving
/// the no-change signal in <see cref="QueryResult{T}.IsNotModified"/>.
/// </summary>
public sealed class EventStoreQueryClient(
    IHttpClientFactory httpClientFactory,
    IOptions<EventStoreOptions> options,
    IUserContextAccessor userContextAccessor,
    EventStoreResponseClassifier classifier,
    IETagCache cache,
    IAuthRedirector authRedirector,
    ILogger<EventStoreQueryClient> logger) : IQueryService {
    internal const string HttpClientName = "Hexalith.FrontComposer.EventStore.Queries";

    public async Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        EventStoreOptions current = options.Value;
        (string tenant, string userId) = EventStoreIdentity.RequireUserContext(userContextAccessor, request.TenantId);
        string? cacheKey = ResolveCacheKey(request, tenant, userId);
        ETagCacheEntry? cachedEntry = cacheKey is null
            ? null
            : await cache.TryGetAsync(cacheKey, request.CachePayloadVersion, cancellationToken).ConfigureAwait(false);

        QueryResult<T> result = await ExecuteAsync<T>(
            request,
            current,
            tenant,
            cacheKey,
            cachedEntry,
            allowProtocolDriftRetry: true,
            cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<QueryResult<T>> ExecuteAsync<T>(
        QueryRequest request,
        EventStoreOptions current,
        string tenant,
        string? cacheKey,
        ETagCacheEntry? cachedEntry,
        bool allowProtocolDriftRetry,
        CancellationToken cancellationToken) {
        IReadOnlyList<string> requestEtags = GetRequestEtags(request);
        EventStoreValidation.ValidateETagCount(requestEtags, current.MaxETagCount);

        string domain = EventStoreValidation.RequireNonColonSegment(
            EventStoreIdentity.NormalizeRouteSegment(request.Domain),
            nameof(request.Domain));
        string aggregateId = EventStoreValidation.RequireNonColonSegment(request.AggregateId, nameof(request.AggregateId));
        string queryType = EventStoreValidation.RequireNonColonSegment(request.QueryType ?? request.ProjectionType, nameof(request.QueryType));
        string projectionType = EventStoreValidation.RequireNonColonSegment(request.ProjectionType, nameof(request.ProjectionType));
        if (!string.IsNullOrWhiteSpace(request.EntityId)) {
            _ = EventStoreValidation.RequireNonColonSegment(request.EntityId, nameof(request.EntityId));
        }

        if (!string.IsNullOrWhiteSpace(request.ProjectionActorType)) {
            _ = EventStoreValidation.RequireNonColonSegment(request.ProjectionActorType, nameof(request.ProjectionActorType));
        }

        string initialCacheOutcome = cachedEntry is not null ? "hit" : cacheKey is null ? "disabled" : "miss";
        long startedAt = Stopwatch.GetTimestamp();
        using Activity? activity = FrontComposerTelemetry.StartQueryExecute(
            projectionType,
            queryType,
            initialCacheOutcome,
            FrontComposerTelemetry.TenantMarker(tenant));

        try {
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, current.QueryEndpointPath);
            await ApplyAuthorizationAsync(httpRequest, current, cancellationToken).ConfigureAwait(false);

            List<string> ifNoneMatch = new(requestEtags.Count + 1);
            for (int i = 0; i < requestEtags.Count; i++) {
                if (ContainsHeaderInjectionChar(requestEtags[i])) {
                    throw new ArgumentException(
                        "ETag validators must not contain control characters or CRLF sequences.",
                        nameof(request));
                }

                ifNoneMatch.Add(requestEtags[i]);
            }

            if (cachedEntry is not null
                && !ContainsHeaderInjectionChar(cachedEntry.ETag)
                && !ifNoneMatch.Contains(cachedEntry.ETag, StringComparer.Ordinal)) {
                ifNoneMatch.Add(cachedEntry.ETag);
            }

            if (ifNoneMatch.Count > 0) {
                EventStoreValidation.ValidateETagCount(ifNoneMatch, current.MaxETagCount);
                httpRequest.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
            }

            JsonElement payload = SerializeQueryPayload(request);
            httpRequest.Content = EventStoreRequestContent.Create(
                new SubmitQueryRequest(
                    tenant,
                    domain,
                    aggregateId,
                    queryType,
                    projectionType,
                    payload,
                    request.EntityId,
                    request.ProjectionActorType),
                current.MaxRequestBytes);

            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            FrontComposerTelemetry.SetHttpStatus(activity, (int)response.StatusCode);
            EventStoreQueryClassification classification = await classifier
                .ClassifyQueryAsync(response, cancellationToken)
                .ConfigureAwait(false);

            switch (classification.Outcome) {
                case QueryClassificationOutcome.NotModified: {
                    // F28 — distinguish "served cached payload after 304" (from_cache) from
                    // "no-change signal returned to caller" (not_modified). Default to
                    // not_modified; the cached branch overrides to from_cache.
                    FrontComposerTelemetry.SetOutcome(activity, "not_modified");
                    if (cachedEntry is not null) {
                        FrontComposerTelemetry.SetOutcome(activity, "from_cache");
                        try {
                            return DeserializeNotModifiedFromCache<T>(cachedEntry, request.ProjectionType);
                        }
                        catch (ProjectionSchemaMismatchException ex) {
                            FrontComposerLog.QuerySchemaMismatch(
                                logger,
                                ex.ProjectionType,
                                ex.GetType().Name);
                            throw;
                        }
                    }

                    // Caller did not opt into framework cache integration (no CacheDiscriminator) —
                    // they own their own cache lifecycle. Surface the explicit no-change signal so
                    // their existing handler can take its own path.
                    if (cacheKey is null) {
                        return QueryResult<T>.NotModified(classification.ETag);
                    }

                    // From here on cacheKey is non-null: caller asked for framework cache integration
                    // but no compatible entry was readable, so EventStore is asserting a state the
                    // client doesn't have. AC4 / D10: retry once uncached, fail loudly otherwise.
                    if (!allowProtocolDriftRetry) {
                        FrontComposerTelemetry.SetFailure(activity, "ProtocolDriftNoCache");
                        logger.LogWarning(
                            "EventStore returned 304 Not Modified twice without a matching cache entry — failing loudly to preserve visible UI state.");
                        throw new HttpRequestException(
                            "EventStore returned 304 Not Modified but no compatible cached payload exists.",
                            inner: null,
                            statusCode: System.Net.HttpStatusCode.NotModified);
                    }

                    FrontComposerTelemetry.SetOutcome(activity, "protocol_drift_retry");
                    logger.LogInformation(
                        "EventStore returned 304 Not Modified without a matching cache entry — retrying once uncached (Story 5-2 D10).");
                    return await ExecuteAsync<T>(
                        request: request with { ETag = null, ETags = null },
                        current,
                        tenant,
                        cacheKey: cacheKey,
                        cachedEntry: null,
                        allowProtocolDriftRetry: false,
                        cancellationToken).ConfigureAwait(false);
                }

                case QueryClassificationOutcome.Ok: {
                    FrontComposerTelemetry.SetOutcome(activity, "ok");
                    string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    IReadOnlyList<T> items;
                    int totalCount;
                    try {
                        using JsonDocument document = JsonDocument.Parse(body);
                        items = ReadPayloadItems<T>(document.RootElement);
                        totalCount = ReadTotalCount(document.RootElement, items.Count);
                    }
                    catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException or ArgumentException) {
                        // P2 — broaden beyond JsonException so type-shape mismatches surfaced by
                        // ReadPayloadItems<T>.Deserialize (InvalidOperationException, NotSupportedException,
                        // ArgumentException) flow through the schema-mismatch path instead of bubbling raw.
                        // P4 — null-guard the diagnostic field; request.ProjectionType is normalized non-null
                        // earlier but logging the literal "null" is still preferable to a NullReferenceException.
                        string diagnosticProjectionType = projectionType ?? string.Empty;
                        // DN2 — invalidate the projection family, not just this single cache key.
                        await TryInvalidateSchemaMismatchAsync(cacheKey, diagnosticProjectionType, cancellationToken).ConfigureAwait(false);

                        FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
                        FrontComposerLog.QuerySchemaMismatch(
                            logger,
                            diagnosticProjectionType,
                            ex.GetType().Name);
                        throw new ProjectionSchemaMismatchException(diagnosticProjectionType, ex);
                    }

                    string? eTag = classification.ETag;
                    if (cacheKey is not null && eTag is { Length: > 0 } && !ContainsHeaderInjectionChar(eTag)) {
                        long now = DateTimeOffset.UtcNow.UtcTicks;
                        ETagCacheEntry entry = new(
                            ETag: eTag,
                            Payload: body,
                            CachedAtUtcTicks: now,
                            LastAccessedUtcTicks: now,
                            FormatVersion: ETagCacheEntry.CurrentFormatVersion,
                            PayloadVersion: request.CachePayloadVersion,
                            Discriminator: request.CacheDiscriminator ?? string.Empty);
                        _ = PersistCacheEntryAsync(cacheKey, entry);
                    }

                    return new QueryResult<T>(items, totalCount, eTag);
                }

                case QueryClassificationOutcome.Failure:
                default: {
                    string failureCategory = classification.Failure?.GetType().Name ?? "UnexpectedStatus";
                    FrontComposerTelemetry.SetOutcome(activity, "failure");
                    FrontComposerTelemetry.SetFailure(activity, failureCategory);
                    if (classification.Failure is AuthRedirectRequiredException authRequired) {
                        await authRedirector.RedirectAsync(returnUrl: null, cancellationToken).ConfigureAwait(false);
                        throw authRequired;
                    }

                    FrontComposerLog.QueryUnexpectedStatus(
                        logger,
                        (int)response.StatusCode,
                        projectionType,
                        queryType,
                        failureCategory,
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
                    throw classification.Failure!;
                }
            }
        }
        catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested
            || oce.CancellationToken.IsCancellationRequested) {
            // F18 — broaden the canceled filter so a linked-CTS leaf cancellation classifies
            // as canceled rather than failure.
            FrontComposerTelemetry.SetOutcome(activity, "canceled");
            throw;
        }
        catch (Exception ex) {
            // F19/F23/F30 — only set outcome/failure if the explicit branch above did not
            // already tag them. This preserves "protocol_drift_retry" / "rejected" /
            // "schema_mismatch" outcomes set before the recursive call or throw bubbles here.
            if (activity?.GetTagItem(FrontComposerTelemetry.OutcomeTag) is null) {
                FrontComposerTelemetry.SetOutcome(activity, "failed");
            }

            if (activity?.GetTagItem(FrontComposerTelemetry.FailureCategoryTag) is null) {
                FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
            }

            throw;
        }
        finally {
            FrontComposerTelemetry.SetElapsed(activity, Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private string? ResolveCacheKey(QueryRequest request, string tenant, string userId)
        => request.CachePayloadVersion >= 1
            && !HasCacheUnsafeQueryShape(request)
            && request.CacheDiscriminator is { Length: > 0 } discriminator
            && cache.TryBuildKey(tenant, userId, discriminator, out string key)
                ? key
                : null;

    private async Task PersistCacheEntryAsync(string cacheKey, ETagCacheEntry entry) {
        try {
            await cache.SetAsync(cacheKey, entry, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            FrontComposerLog.QueryCacheWriteFailed(logger, ex.GetType().Name);
        }
    }

    private static bool HasCacheUnsafeQueryShape(QueryRequest request) {
#pragma warning disable CS0618 // Legacy Filter remains part of cache-safety gating until v1.0-rc2.
        return !string.IsNullOrWhiteSpace(request.Filter)
            || (request.ColumnFilters is { Count: > 0 })
            || (request.StatusFilters is { Count: > 0 })
            || !string.IsNullOrWhiteSpace(request.SearchQuery)
            || !string.IsNullOrWhiteSpace(request.SortColumn);
#pragma warning restore CS0618
    }

    private static IReadOnlyList<string> GetRequestEtags(QueryRequest request) {
        if (request.ETags is not null) {
            return request.ETags;
        }

        return string.IsNullOrWhiteSpace(request.ETag) ? [] : [request.ETag];
    }

    [SuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore adapter deserializes adopter projection DTOs at runtime; AOT-specific contexts are deferred to Story 9-4.")]
    private static QueryResult<T> DeserializeNotModifiedFromCache<T>(ETagCacheEntry entry, string projectionType) {
        try {
            using JsonDocument document = JsonDocument.Parse(entry.Payload);
            IReadOnlyList<T> items = ReadPayloadItems<T>(document.RootElement);
            int totalCount = ReadTotalCount(document.RootElement, items.Count);
            return QueryResult<T>.NotModifiedFromCache(items, totalCount, entry.ETag);
        }
        // P3 — broaden parity with the 200 OK path so type-shape mismatches in cached payload
        // also map to schema-mismatch instead of propagating raw.
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException or ArgumentException) {
            throw new ProjectionSchemaMismatchException(projectionType ?? string.Empty, ex);
        }
    }

    /// <summary>
    /// DN2 — best-effort family-level cache invalidation on schema mismatch. AC7 specifies
    /// "all entries for the affected projection type/discriminator family". Wrapped in try/catch
    /// per P1 — a cache-removal failure must never replace the schema-mismatch we are surfacing.
    /// </summary>
    private async Task TryInvalidateSchemaMismatchAsync(string? cacheKey, string projectionType, CancellationToken cancellationToken) {
        try {
            if (!string.IsNullOrEmpty(projectionType)) {
                await cache.RemoveByProjectionTypeAsync(projectionType, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(cacheKey)) {
                await cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            FrontComposerLog.QuerySchemaMismatchInvalidationFailed(logger, ex.GetType().Name);
        }
    }

    private static bool ContainsHeaderInjectionChar(string value) {
        if (string.IsNullOrEmpty(value)) {
            return false;
        }

        for (int i = 0; i < value.Length; i++) {
            char ch = value[i];
            if (char.IsControl(ch)) {
                return true;
            }
        }

        return false;
    }

    [SuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore adapter deserializes adopter projection DTOs at runtime; AOT-specific contexts are deferred to Story 9-4.")]
    private static IReadOnlyList<T> ReadPayloadItems<T>(JsonElement root) {
        if (!root.TryGetProperty("payload", out JsonElement payload)
            || payload.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            return [];
        }

        if (payload.ValueKind == JsonValueKind.Array) {
            return payload.Deserialize<List<T>>(EventStoreRequestContent.JsonOptions) ?? [];
        }

        T? item = payload.Deserialize<T>(EventStoreRequestContent.JsonOptions);
        return item is null ? [] : [item];
    }

    private static int ReadTotalCount(JsonElement root, int defaultCount) {
        if (root.TryGetProperty("totalCount", out JsonElement totalCount)
            && totalCount.ValueKind == JsonValueKind.Number
            && totalCount.TryGetInt32(out int parsed)) {
            return parsed;
        }

        return defaultCount;
    }

    [SuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore adapter serializes query payload metadata through System.Text.Json web defaults.")]
    private static JsonElement SerializeQueryPayload(QueryRequest request) {
#pragma warning disable CS0618 // Filter remains part of the compatibility payload until v1.0-rc2.
        return JsonSerializer.SerializeToElement(
            new QueryPayload(
                request.Filter,
                request.Skip,
                request.Take,
                request.ColumnFilters,
                request.StatusFilters,
                request.SearchQuery,
                request.SortColumn,
                request.SortDescending),
            EventStoreRequestContent.JsonOptions);
#pragma warning restore CS0618
    }

    private static async Task ApplyAuthorizationAsync(
        HttpRequestMessage request,
        EventStoreOptions options,
        CancellationToken cancellationToken) {
        if (options.AccessTokenProvider is null) {
            if (options.RequireAccessToken) {
                throw new InvalidOperationException("EventStore access token provider is required.");
            }

            return;
        }

        string? token = await options.AccessTokenProvider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) {
            if (options.RequireAccessToken) {
                throw new InvalidOperationException("EventStore access token provider returned an empty token.");
            }

            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record SubmitQueryRequest(
        string Tenant,
        string Domain,
        string AggregateId,
        string QueryType,
        string? ProjectionType,
        JsonElement? Payload,
        string? EntityId,
        string? ProjectionActorType);

    private sealed record QueryPayload(
        string? Filter,
        int? Skip,
        int? Take,
        IReadOnlyDictionary<string, string>? ColumnFilters,
        IReadOnlyList<string>? StatusFilters,
        string? SearchQuery,
        string? SortColumn,
        bool SortDescending);

}
