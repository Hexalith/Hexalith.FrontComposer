using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default EventStore-backed command service. Story 5-2 routes every non-202 response
/// through <see cref="EventStoreResponseClassifier"/> so generated forms see a typed
/// exception (<see cref="CommandValidationException"/>, <see cref="CommandWarningException"/>,
/// <see cref="AuthRedirectRequiredException"/>, <see cref="CommandRejectedException"/>) instead
/// of a stringly-typed <see cref="HttpRequestException"/>.
/// </summary>
public sealed class EventStoreCommandClient(
    IHttpClientFactory httpClientFactory,
    IOptions<EventStoreOptions> options,
    IUlidFactory ulidFactory,
    IUserContextAccessor userContextAccessor,
    EventStoreResponseClassifier classifier,
    ILogger<EventStoreCommandClient> logger) : ICommandServiceWithLifecycle {
    internal const string HttpClientName = "Hexalith.FrontComposer.EventStore.Commands";

    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, null, cancellationToken);

    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);

        EventStoreOptions current = options.Value;
        string messageId = ulidFactory.NewUlid();
        (string tenant, _) = EventStoreIdentity.RequireUserContext(
            userContextAccessor,
            ReadStringProperty(command, "TenantId"));
        string domain = EventStoreIdentity.GetDomain(typeof(TCommand));
        string aggregateId = EventStoreIdentity.GetAggregateId(command);

        using HttpRequestMessage request = new(HttpMethod.Post, current.CommandEndpointPath);
        await ApplyAuthorizationAsync(request, current, cancellationToken).ConfigureAwait(false);

        JsonElement payload = SerializeCommandPayload(command);
        request.Content = EventStoreRequestContent.Create(
            new SubmitCommandRequest(
                messageId,
                tenant,
                domain,
                aggregateId,
                typeof(TCommand).FullName ?? typeof(TCommand).Name,
                payload),
            current.MaxRequestBytes);

        HttpClient client = httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        EventStoreCommandClassification classification = await classifier
            .ClassifyCommandAsync(response, cancellationToken)
            .ConfigureAwait(false);

        if (!classification.IsAccepted) {
            logger.LogWarning(
                "EventStore command dispatch returned unexpected HTTP status {StatusCode}. LocationPath={LocationPath}",
                (int)response.StatusCode,
                response.Headers.Location?.GetLeftPart(UriPartial.Path));
            throw classification.Failure!;
        }

        string? responseCorrelationId = classification.CorrelationId
            ?? await ReadCorrelationIdAsync(response, logger, cancellationToken).ConfigureAwait(false);

        CommandResult result = new(
            messageId,
            "Accepted",
            responseCorrelationId,
            classification.Location,
            classification.RetryAfter);

        onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, result.CorrelationId ?? result.MessageId);
        return result;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2090:DynamicallyAccessedMembers",
        Justification = "FrontComposer command DTOs are runtime adopter types; EventStore adapter reads optional TenantId by established reflection convention.")]
    private static string? ReadStringProperty<TCommand>(TCommand command, string propertyName) {
        object? value = typeof(TCommand).GetProperty(propertyName)?.GetValue(command);
        return value is null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore adapter serializes adopter command DTOs at runtime; AOT-specific contexts are deferred to Story 9-4.")]
    private static JsonElement SerializeCommandPayload<TCommand>(TCommand command)
        => JsonSerializer.SerializeToElement(command, EventStoreRequestContent.JsonOptions);

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

    private static async Task<string?> ReadCorrelationIdAsync(
        HttpResponseMessage response,
        ILogger logger,
        CancellationToken cancellationToken) {
        if (response.Content is null) {
            return null;
        }

        if (response.Content.Headers.ContentLength == 0) {
            return null;
        }

        try {
            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return document.RootElement.TryGetProperty("correlationId", out JsonElement value)
                ? value.GetString()
                : null;
        }
        catch (JsonException) {
            // Reason intentionally omitted — JsonException.Message can echo response body fragments.
            logger.LogWarning(
                "EventStore command response body could not be parsed as JSON; correlationId unavailable. ContentType={ContentType}",
                response.Content.Headers.ContentType?.MediaType);
            return null;
        }
    }

    private sealed record SubmitCommandRequest(
        string MessageId,
        string Tenant,
        string Domain,
        string AggregateId,
        string CommandType,
        JsonElement Payload,
        string? CorrelationId = null,
        Dictionary<string, string>? Extensions = null);

}
