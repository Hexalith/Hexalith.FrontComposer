using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default EventStore-backed query service.
/// </summary>
public sealed class EventStoreQueryClient(
    IHttpClientFactory httpClientFactory,
    IOptions<EventStoreOptions> options,
    IUserContextAccessor userContextAccessor,
    ILogger<EventStoreQueryClient> logger) : IQueryService {
    internal const string HttpClientName = "Hexalith.FrontComposer.EventStore.Queries";

    public async Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        EventStoreOptions current = options.Value;
        IReadOnlyList<string> etags = GetETags(request);
        EventStoreValidation.ValidateETagCount(etags, current.MaxETagCount);

        (string tenant, _) = EventStoreIdentity.RequireUserContext(userContextAccessor, request.TenantId);
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

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, current.QueryEndpointPath);
        await ApplyAuthorizationAsync(httpRequest, current, cancellationToken).ConfigureAwait(false);
        if (etags.Count > 0) {
            for (int i = 0; i < etags.Count; i++) {
                if (ContainsHeaderInjectionChar(etags[i])) {
                    throw new ArgumentException(
                        "ETag validators must not contain control characters or CRLF sequences.",
                        nameof(request));
                }
            }

            httpRequest.Headers.TryAddWithoutValidation("If-None-Match", etags);
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
        string? responseETag = response.Headers.ETag?.Tag ?? TryGetHeader(response.Headers, "ETag");
        if (response.StatusCode == HttpStatusCode.NotModified) {
            return QueryResult<T>.NotModified(responseETag);
        }

        if (response.StatusCode != HttpStatusCode.OK) {
            logger.LogWarning(
                "EventStore query returned unexpected HTTP status {StatusCode}.",
                (int)response.StatusCode);
            throw new HttpRequestException("EventStore query did not return 200 OK or 304 Not Modified.", null, response.StatusCode);
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<T> items = ReadPayloadItems<T>(document.RootElement);
        return new QueryResult<T>(items, items.Count, responseETag);
    }

    private static IReadOnlyList<string> GetETags(QueryRequest request) {
        if (request.ETags is not null) {
            return request.ETags;
        }

        return string.IsNullOrWhiteSpace(request.ETag) ? [] : [request.ETag];
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

    [UnconditionalSuppressMessage(
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

    [UnconditionalSuppressMessage(
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

    private static string? TryGetHeader(HttpResponseHeaders headers, string name)
        => headers.TryGetValues(name, out IEnumerable<string>? values) ? values.FirstOrDefault() : null;

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
