using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

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
    ILogger<EventStoreCommandClient> logger,
    IOptions<FcShellOptions>? shellOptions = null) : ICommandServiceWithLifecycle {
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
        TenantContextSnapshot tenantContext = FrontComposerTenantContextAccessor
            .Resolve(
                userContextAccessor,
                shellOptions?.Value ?? new FcShellOptions(),
                logger,
                ReadStringProperty(command, "TenantId"),
                "command-dispatch")
            .EnsureSuccess();
        (string tenant, _) = EventStoreIdentity.RequireUserContext(tenantContext);
        string domain = EventStoreIdentity.GetDomain(typeof(TCommand));
        string aggregateId = EventStoreIdentity.GetAggregateId(command);
        string commandTypeName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        long startedAt = Stopwatch.GetTimestamp();
        using Activity? activity = FrontComposerTelemetry.StartCommandDispatch(
            commandTypeName,
            messageId,
            FrontComposerTelemetry.TenantMarker(tenant));

        try {
            using HttpRequestMessage request = new(HttpMethod.Post, current.CommandEndpointPath);
            await ApplyAuthorizationAsync(request, current, cancellationToken).ConfigureAwait(false);

            JsonElement payload = SerializeCommandPayload(command);
            request.Content = EventStoreRequestContent.Create(
                new SubmitCommandRequest(
                    messageId,
                    tenant,
                    domain,
                    aggregateId,
                    commandTypeName,
                    payload),
                current.MaxRequestBytes);

            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            FrontComposerTelemetry.SetHttpStatus(activity, (int)response.StatusCode);
            EventStoreCommandClassification classification = await classifier
                .ClassifyCommandAsync(response, cancellationToken)
                .ConfigureAwait(false);

            if (!classification.IsAccepted) {
                string failureCategory = classification.Failure?.GetType().Name ?? "UnexpectedStatus";
                FrontComposerTelemetry.SetOutcome(activity, "rejected");
                FrontComposerTelemetry.SetFailure(activity, failureCategory);
                // F13 — emit only LocationPresent boolean; the Location header path can carry
                // raw aggregate IDs / route values derived from tenant/user input which AC5
                // forbids. Operators have CommandType + MessageId + FailureCategory to find
                // the command end-to-end.
                // F09 — sanitize messageId at the log boundary so trace tags and log fields
                // share the same bounded format.
                FrontComposerLog.CommandUnexpectedStatus(
                    logger,
                    (int)response.StatusCode,
                    commandTypeName,
                    FrontComposerTelemetry.SafeIdentifierOrAbsent(messageId),
                    failureCategory,
                    response.Headers.Location is not null,
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
                throw classification.Failure!;
            }

            string? responseCorrelationId = classification.CorrelationId
                ?? await ReadCorrelationIdAsync(response, logger, commandTypeName, messageId, cancellationToken).ConfigureAwait(false);
            FrontComposerTelemetry.SetCorrelation(activity, responseCorrelationId);

            CommandResult result = new(
                messageId,
                "Accepted",
                responseCorrelationId,
                classification.Location,
                classification.RetryAfter);

            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, result.CorrelationId ?? result.MessageId);
            FrontComposerTelemetry.SetOutcome(activity, "accepted");
            return result;
        }
        catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested
            || oce.CancellationToken.IsCancellationRequested) {
            // F18 — broaden the canceled filter so a linked-CTS leaf cancellation (e.g., the
            // request linked-token from an internal HttpClient timeout-as-cancellation) classifies
            // as canceled rather than failure. Caller tokens still take precedence; if the leaf
            // token is anonymous, we fall back to the original behavior via the second clause.
            FrontComposerTelemetry.SetOutcome(activity, "canceled");
            throw;
        }
        catch (Exception ex) {
            // F29 — explicitly tag outcome=failed in the catch-all so dashboards see a paired
            // (outcome, failure_category) on every error path, matching the explicit branches.
            // F23 — only set failure category if it is not already set (preserves the explicit
            // `rejected` branch's failureCategory which mirrors classification.Failure but might
            // not match the wrapping/inner exception type bubbled here).
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
        string commandType,
        string messageId,
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
            // F09 — sanitize messageId at the log boundary so trace tags and log fields share
            // the same bounded format.
            FrontComposerLog.CommandCorrelationBodyParseFailed(
                logger,
                response.Content.Headers.ContentType?.MediaType,
                commandType,
                FrontComposerTelemetry.SafeIdentifierOrAbsent(messageId));
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
