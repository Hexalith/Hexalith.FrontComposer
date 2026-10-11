using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default EventStore-backed command service. Story 5-2 routes every non-202 response
/// through <see cref="EventStoreResponseClassifier"/> so generated forms see a typed
/// exception (<see cref="CommandValidationException"/>, <see cref="CommandWarningException"/>,
/// <see cref="AuthRedirectRequiredException"/>, <see cref="CommandRejectedException"/>) instead
/// of a stringly-typed <see cref="HttpRequestException"/>.
/// Story 7-3 Pass 4 DN-7-3-4-2: authorization is enforced via
/// <c>AuthorizingCommandServiceDecorator</c> at the DI seam; this concrete impl no longer takes a
/// gate parameter so test factories cannot silently bypass authorization by constructing the impl
/// without the gate.
/// </summary>
public sealed class EventStoreCommandClient(
    IHttpClientFactory httpClientFactory,
    IOptions<EventStoreOptions> options,
    IUlidFactory ulidFactory,
    IUserContextAccessor userContextAccessor,
    EventStoreResponseClassifier classifier,
    ILogger<EventStoreCommandClient> logger,
    IOptions<FcShellOptions>? shellOptions = null,
    TimeProvider? timeProvider = null) : ICommandServiceWithLifecycle, ICommandServiceWithLifecycleObservations {
    internal const string HttpClientName = "Hexalith.FrontComposer.EventStore.Commands";

    /// <summary>Initializes the EventStore command client using the system clock.</summary>
    public EventStoreCommandClient(
        IHttpClientFactory httpClientFactory,
        IOptions<EventStoreOptions> options,
        IUlidFactory ulidFactory,
        IUserContextAccessor userContextAccessor,
        EventStoreResponseClassifier classifier,
        ILogger<EventStoreCommandClient> logger,
        IOptions<FcShellOptions>? shellOptions)
        : this(httpClientFactory, options, ulidFactory, userContextAccessor, classifier, logger, shellOptions, null) {
    }

    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchWithObservationsAsync(command, null, cancellationToken);

    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        return await DispatchWithObservationsAsync(
            command,
            observation => onLifecycleChange?.Invoke(observation.State, observation.MessageId),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    Task<CommandResult> ICommandServiceWithLifecycleObservations.DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken)
        where TCommand : class =>
        DispatchWithObservationsAsync(command, onLifecycleObservation, cancellationToken);

    private async Task<CommandResult> DispatchWithObservationsAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EventStoreOptions current = options.Value;
        FcShellOptions currentShellOptions = shellOptions?.Value ?? new FcShellOptions();
        string messageId = ulidFactory.NewUlid();
        TenantContextSnapshot tenantContext = FrontComposerTenantContextAccessor
            .Resolve(
                userContextAccessor,
                currentShellOptions,
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
            JsonElement payload = SerializeCommandPayload(command);
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            // Before transport acceptance, retrying a POST could duplicate an operation. Send
            // once; only an acknowledged operation may use the same MessageId for one retry.
            using HttpRequestMessage request = new(HttpMethod.Post, current.CommandEndpointPath);
            await EventStoreHttp.ApplyAuthorizationAsync(request, current, cancellationToken).ConfigureAwait(false);
            request.Content = EventStoreRequestContent.Create(
                new SubmitCommandRequest(messageId, tenant, domain, aggregateId, commandTypeName, payload),
                current.MaxRequestBytes);
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested && response.StatusCode != HttpStatusCode.Accepted) {
                response.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
            FrontComposerTelemetry.SetHttpStatus(activity, (int)response.StatusCode);
            using (response) {
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
                    if (IsRetryablePreAcceptFailure(classification.Failure)) {
                        throw CreatePreAcceptWarning();
                    }

                    throw classification.Failure!;
                }

                // Acceptance is established by the headers. Keep this anchor even if the
                // optional response body stalls or cannot be read.
                DateTimeOffset? observedAt;
                Exception? lifecycleObservationFailure = null;
                try {
                    observedAt = (timeProvider ?? TimeProvider.System).GetUtcNow();
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    lifecycleObservationFailure = ex;
                    observedAt = null;
                }

                // Admission must be observable before optional correlation recovery starts.
                // Consumers use this original header anchor for the 10s/120s budgets.
                try {
                    onLifecycleObservation?.Invoke(new CommandLifecycleObservation(
                        CommandLifecycleState.Syncing,
                        messageId,
                        CommandMateriality.Unknown,
                        observedAt));
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    lifecycleObservationFailure = lifecycleObservationFailure is null
                        ? ex
                        : new AggregateException(lifecycleObservationFailure, ex);
                }

                if (lifecycleObservationFailure is not null) {
                    FrontComposerWarningLog.EventStoreLifecycleCallbackFailed(
                        logger,
                        messageId,
                        lifecycleObservationFailure);
                }

                string? responseCorrelationId = NormalizeCorrelation(classification.CorrelationId);
                if (responseCorrelationId is null) {
                    try {
                        responseCorrelationId = await ReadAcceptedCorrelationIdAsync(response, logger, commandTypeName, messageId, timeProvider ?? TimeProvider.System, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when ((ex is HttpRequestException or IOException or JsonException or TimeoutException or OperationCanceledException)
                        && !cancellationToken.IsCancellationRequested
                        && currentShellOptions.CommandDispatchRetryAttempts > 0) {
                        responseCorrelationId = await RetryAcceptedCorrelationAsync(
                            client, current, currentShellOptions, messageId, tenant, domain, aggregateId,
                            commandTypeName, payload, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TimeoutException or OperationCanceledException) {
                        // The headers prove acceptance; an unreadable optional body only leaves
                        // correlation unknown. Polling still starts with the original MessageId.
                        responseCorrelationId = null;
                    }
                }
                FrontComposerTelemetry.SetCorrelation(activity, responseCorrelationId);

                CommandResult result = new(
                    messageId,
                    CommandResultStatus.Accepted,
                    responseCorrelationId,
                    classification.Location,
                    classification.RetryAfter);

                FrontComposerTelemetry.SetOutcome(activity, "accepted");
                return result;
            }
        }
        catch (HttpRequestException ex) when (IsRetryablePreAcceptFailure(ex)) {
            FrontComposerTelemetry.SetOutcome(activity, "rejected");
            FrontComposerTelemetry.SetFailure(activity, nameof(CommandWarningException));
            throw CreatePreAcceptWarning();
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

    private async Task<string?> RetryAcceptedCorrelationAsync(
        HttpClient client,
        EventStoreOptions current,
        FcShellOptions retryOptions,
        string messageId,
        string tenant,
        string domain,
        string aggregateId,
        string commandTypeName,
        JsonElement payload,
        CancellationToken cancellationToken) {
        CancellationTokenSource recoveryCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<string?>? recovery = null;
        try {
            // Bound the complete optional recovery, even if a provider/transport ignores
            // cancellation. A normal HttpClient timeout must not extend accepted feedback.
            recovery = RetryAsync(recoveryCancellation.Token);
            return await recovery.WaitAsync(TimeSpan.FromSeconds(2), timeProvider ?? TimeProvider.System, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            // The first response already proved acceptance. A failed retry remains unconfirmed.
            return null;
        }
        finally {
            ObserveAcceptedCleanup(recoveryCancellation, recovery);
        }

        async Task<string?> RetryAsync(CancellationToken recoveryToken) {
            await DelayBeforeRetryAsync(retryOptions, timeProvider ?? TimeProvider.System, recoveryToken).ConfigureAwait(false);
            using HttpRequestMessage retry = new(HttpMethod.Post, current.CommandEndpointPath);
            await EventStoreHttp.ApplyAuthorizationAsync(retry, current, recoveryToken).ConfigureAwait(false);
            recoveryToken.ThrowIfCancellationRequested();
            retry.Content = EventStoreRequestContent.Create(
                new SubmitCommandRequest(messageId, tenant, domain, aggregateId, commandTypeName, payload),
                current.MaxRequestBytes);
            using HttpResponseMessage response = await client.SendAsync(retry, HttpCompletionOption.ResponseHeadersRead, recoveryToken).ConfigureAwait(false);
            recoveryToken.ThrowIfCancellationRequested();
            EventStoreCommandClassification repeated = await classifier.ClassifyCommandAsync(response, recoveryToken).ConfigureAwait(false);
            return repeated.IsAccepted
                ? NormalizeCorrelation(repeated.CorrelationId) ?? await ReadAcceptedCorrelationIdAsync(response, logger, commandTypeName, messageId, timeProvider ?? TimeProvider.System, recoveryToken).ConfigureAwait(false)
                : null;
        }
    }

    private static async Task DelayBeforeRetryAsync(FcShellOptions options, TimeProvider time, CancellationToken cancellationToken) {
        var delay = TimeSpan.FromMilliseconds(options.CommandDispatchRetryDelayMs);
        await Task.Delay(delay, time, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsRetryablePreAcceptFailure(Exception? exception) {
        if (exception is not HttpRequestException httpException) {
            return false;
        }

        return httpException.StatusCode is null
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static CommandWarningException CreatePreAcceptWarning()
        => new(
            CommandWarningKind.RetryableDispatchFailed,
            new ProblemDetailsPayload(
                "Command outcome unknown",
                "We could not confirm whether the command was accepted. Check its status before submitting again.",
                null,
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                Array.Empty<string>()));

    private static string? NormalizeCorrelation(string? correlationId)
        => string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;

    private static async Task<string?> ReadAcceptedCorrelationIdAsync(
        HttpResponseMessage response,
        ILogger logger,
        string commandType,
        string messageId,
        TimeProvider time,
        CancellationToken cancellationToken) {
        CancellationTokenSource bodyCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<string?>? reading = null;
        try {
            reading = ReadCorrelationIdAsync(response, logger, commandType, messageId, bodyCancellation.Token);
            return await reading.WaitAsync(TimeSpan.FromSeconds(2), time, cancellationToken).ConfigureAwait(false);
        }
        finally {
            ObserveAcceptedCleanup(bodyCancellation, reading);
        }
    }

    private static void ObserveAcceptedCleanup(CancellationTokenSource cancellation, Task? recovery)
    {
        // CancelAsync schedules provider callbacks without blocking accepted return. Keep the
        // source alive until callbacks and abandoned work settle, and observe every fault.
        Task cleanup = CompleteAcceptedCleanupAsync(cancellation, recovery);
        _ = cleanup.ContinueWith(
            static completed => { _ = completed.Exception; },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task CompleteAcceptedCleanupAsync(CancellationTokenSource cancellation, Task? recovery)
    {
        try
        {
            Task completion = Task.WhenAll(cancellation.CancelAsync(), recovery ?? Task.CompletedTask);
            try
            {
                await completion.ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)
                && completion.Exception?.Flatten().InnerExceptions.Any(ExceptionGuard.IsFatal) != true)
            {
                // Optional cleanup cannot turn proven acceptance into a dispatch failure.
            }
        }
        finally
        {
            cancellation.Dispose();
        }
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
            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                throw new JsonException("The accepted response body has no object envelope.");
            }

            if (!document.RootElement.TryGetProperty("correlationId", out JsonElement value)) {
                return null;
            }

            if (value.ValueKind is JsonValueKind.Null) {
                return null;
            }

            if (value.ValueKind != JsonValueKind.String) {
                throw new JsonException("The accepted response correlation has an invalid shape.");
            }

            string? correlationId = value.GetString();
            return NormalizeCorrelation(correlationId);
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
            throw;
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
