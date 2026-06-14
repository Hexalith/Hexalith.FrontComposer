using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// EventStore-backed pending-command status provider used by the polling coordinator.
/// </summary>
public sealed class EventStorePendingCommandStatusQuery(
    IHttpClientFactory httpClientFactory,
    IOptions<EventStoreOptions> options,
    EventStoreResponseClassifier classifier,
    ILogger<EventStorePendingCommandStatusQuery> logger) : IPendingCommandStatusQuery {
    private const string StatusEndpointPrefix = "/api/v1/commands/status/";
    private const int MaxStatusTextLength = 512;

    /// <inheritdoc />
    public async ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(pendingCommand);

        EventStoreOptions current = options.Value;
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            StatusEndpointPrefix + Uri.EscapeDataString(pendingCommand.MessageId));
        await EventStoreHttp.ApplyAuthorizationAsync(request, current, cancellationToken).ConfigureAwait(false);

        HttpClient client = httpClientFactory.CreateClient(EventStoreQueryClient.HttpClientName);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = ResolveRetryAfter(response.Headers.RetryAfter);

        EventStoreQueryClassification classification = await classifier
            .ClassifyQueryAsync(response, cancellationToken)
            .ConfigureAwait(false);
        if (classification.Outcome == QueryClassificationOutcome.Failure) {
            throw classification.Failure!;
        }
        if (classification.Outcome != QueryClassificationOutcome.Ok) {
            throw ProtocolFailure(pendingCommand.MessageId, classification.Outcome.ToString());
        }

        EventStoreCommandStatusResponse status = await ReadStatusResponseAsync(
            response,
            current.MaxResponseBytes,
            pendingCommand.MessageId,
            cancellationToken).ConfigureAwait(false);

        EventStoreCommandStatus parsed = ParseStatus(status, pendingCommand.MessageId);
        return parsed switch {
            EventStoreCommandStatus.Received
                or EventStoreCommandStatus.Processing
                or EventStoreCommandStatus.EventsStored
                or EventStoreCommandStatus.EventsPublished => null,
            EventStoreCommandStatus.Completed => new PendingCommandOutcomeObservation(
                Source: PendingCommandOutcomeSource.IdempotencyStatusQuery,
                Outcome: PendingCommandTerminalOutcome.Confirmed,
                MessageId: pendingCommand.MessageId),
            EventStoreCommandStatus.Rejected => new PendingCommandOutcomeObservation(
                Source: PendingCommandOutcomeSource.IdempotencyStatusQuery,
                Outcome: PendingCommandTerminalOutcome.Rejected,
                MessageId: pendingCommand.MessageId,
                RejectionTitle: BoundText(status.RejectionEventType) ?? "Command rejected",
                RejectionDetail: BoundText(status.FailureReason)
                    ?? BoundText(status.RejectionEventType)
                    ?? "The command was rejected by EventStore."),
            EventStoreCommandStatus.PublishFailed => new PendingCommandOutcomeObservation(
                Source: PendingCommandOutcomeSource.IdempotencyStatusQuery,
                Outcome: PendingCommandTerminalOutcome.NeedsReview,
                MessageId: pendingCommand.MessageId,
                RejectionTitle: "Command publish failed",
                RejectionDetail: BoundText(status.FailureReason)
                    ?? "EventStore reported PublishFailed for the accepted command."),
            EventStoreCommandStatus.TimedOut => new PendingCommandOutcomeObservation(
                Source: PendingCommandOutcomeSource.IdempotencyStatusQuery,
                Outcome: PendingCommandTerminalOutcome.NeedsReview,
                MessageId: pendingCommand.MessageId,
                RejectionTitle: "Command timed out",
                RejectionDetail: BoundText(status.FailureReason)
                    ?? BoundText(status.TimeoutDuration)
                    ?? "EventStore reported TimedOut for the accepted command."),
            _ => throw ProtocolFailure(pendingCommand.MessageId, "UnknownStatus"),
        };
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "EventStore status response is a single concrete Shell-side DTO matching the confirmed EventStore contract.")]
    private async Task<EventStoreCommandStatusResponse> ReadStatusResponseAsync(
        HttpResponseMessage response,
        int maxResponseBytes,
        string messageId,
        CancellationToken cancellationToken) {
        string body = await EventStoreHttp
            .ReadBoundedResponseBodyAsync(response.Content, maxResponseBytes, logger, cancellationToken)
            .ConfigureAwait(false);

        try {
            EventStoreCommandStatusResponse? status = JsonSerializer.Deserialize<EventStoreCommandStatusResponse>(
                body,
                EventStoreRequestContent.JsonOptions);
            return status ?? throw ProtocolFailure(messageId, "EmptyBody");
        }
        catch (JsonException ex) {
            throw ProtocolFailure(messageId, ex.GetType().Name);
        }
    }

    private EventStoreCommandStatus ParseStatus(EventStoreCommandStatusResponse response, string messageId) {
        if (!Enum.TryParse(response.Status, ignoreCase: false, out EventStoreCommandStatus parsed)
            || !Enum.IsDefined(parsed)) {
            throw ProtocolFailure(messageId, "UnknownStatus");
        }

        if ((int)parsed != response.StatusCode) {
            throw ProtocolFailure(messageId, "StatusCodeMismatch");
        }

        return parsed;
    }

    private InvalidOperationException ProtocolFailure(string messageId, string failureCategory) {
        logger.LogWarning(
            "EventStore pending-command status response could not be used. FailureCategory={FailureCategory} MessageId={MessageId}",
            failureCategory,
            messageId);
        return new InvalidOperationException("EventStore command status response did not match the expected contract.");
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

    private static string? BoundText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        Span<char> buffer = stackalloc char[Math.Min(value.Length, MaxStatusTextLength)];
        int written = 0;
        for (int i = 0; i < value.Length && written < buffer.Length; i++) {
            char ch = value[i];
            buffer[written++] = char.IsControl(ch) ? ' ' : ch;
        }

        string bounded = new string(buffer[..written]).Trim();
        return bounded.Length == 0 ? null : bounded;
    }

    private sealed record EventStoreCommandStatusResponse(
        string CorrelationId,
        string Status,
        int StatusCode,
        DateTimeOffset Timestamp,
        string? AggregateId,
        int? EventCount,
        string? RejectionEventType,
        string? FailureReason,
        string? TimeoutDuration);

    private enum EventStoreCommandStatus {
        Received = 0,
        Processing = 1,
        EventsStored = 2,
        EventsPublished = 3,
        Completed = 4,
        Rejected = 5,
        PublishFailed = 6,
        TimedOut = 7,
    }
}
