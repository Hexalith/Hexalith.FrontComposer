using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>
/// Source-generated, allocation-aware, support-safe log helpers for Shell command-lifecycle,
/// projection-refresh, polling, reconciliation, and cache hot paths.
/// </summary>
internal static partial class FrontComposerHotPathLog
{
    /// <summary>Emits the <c>LifecycleUnexpectedCorrelation</c> hot-path event.</summary>
    public static void LifecycleUnexpectedCorrelation(
        ILogger? logger,
        string diagnosticId,
        string? correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLifecycleUnexpectedCorrelation(logger, diagnosticId, Digest(correlationId));
    }

    /// <summary>Emits the <c>LifecycleIdempotencyResolved</c> hot-path event.</summary>
    public static void LifecycleIdempotencyResolved(
        ILogger? logger,
        string diagnosticId,
        string? correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogLifecycleIdempotencyResolved(logger, diagnosticId, Digest(correlationId));
    }

    /// <summary>Emits the <c>LifecycleIdempotentInfoBarRendered</c> hot-path event.</summary>
    public static void LifecycleIdempotentInfoBarRendered(
        ILogger? logger,
        string diagnosticId,
        string? correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogLifecycleIdempotentInfoBarRendered(logger, diagnosticId, Digest(correlationId));
    }

    /// <summary>Emits the <c>PendingStatusProtocolFailure</c> hot-path event.</summary>
    public static void PendingStatusProtocolFailure(
        ILogger? logger,
        string failureCategory,
        string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingStatusProtocolFailure(logger, Category(failureCategory), Digest(messageId));
    }

    /// <summary>Emits the <c>ProjectionHubStateSubscriberFailed</c> hot-path event.</summary>
    public static void ProjectionHubStateSubscriberFailed<TState>(
        ILogger? logger,
        TState state,
        string failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionHubStateSubscriberFailed(logger, Category(state), Category(failureCategory));
    }

    /// <summary>Emits the <c>ReconciliationLaneMissingTenant</c> hot-path event.</summary>
    public static void ReconciliationLaneMissingTenant(
        ILogger? logger,
        string? viewKey,
        string? projectionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogReconciliationLaneMissingTenant(logger, Digest(viewKey), Digest(projectionType));
    }

    /// <summary>Emits the <c>ReconciliationLaneDegraded</c> hot-path event.</summary>
    public static void ReconciliationLaneDegraded(
        ILogger? logger,
        string? viewKey,
        string? projectionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogReconciliationLaneDegraded(logger, Digest(viewKey), Digest(projectionType));
    }

    /// <summary>Emits the <c>ProjectionRefreshNegativeCount</c> hot-path event.</summary>
    public static void ProjectionRefreshNegativeCount(ILogger? logger, string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionRefreshNegativeCount(logger, Digest(viewKey));
    }

    /// <summary>Emits the <c>LifecycleReplayCallbackFaulted</c> hot-path event.</summary>
    public static void LifecycleReplayCallbackFaulted(
        ILogger? logger,
        string? correlationId,
        string failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogLifecycleReplayCallbackFaulted(logger, Digest(correlationId), Category(failureCategory));
    }

    /// <summary>Emits the <c>LifecycleCrossCorrelationDuplicate</c> hot-path event.</summary>
    public static void LifecycleCrossCorrelationDuplicate(
        ILogger? logger,
        string? correlationId,
        string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLifecycleCrossCorrelationDuplicate(logger, Digest(correlationId), Digest(messageId));
    }

    /// <summary>Emits the <c>LifecycleInvalidTransition</c> hot-path event.</summary>
    public static void LifecycleInvalidTransition<TState>(
        ILogger? logger,
        string? correlationId,
        TState from,
        TState to,
        string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogLifecycleInvalidTransition(
            logger,
            Digest(correlationId),
            Category(from),
            Category(to),
            Digest(messageId));
    }

    /// <summary>Emits the <c>LifecycleMissingSubmitted</c> hot-path event.</summary>
    public static void LifecycleMissingSubmitted<TState>(
        ILogger? logger,
        string? correlationId,
        TState state)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLifecycleMissingSubmitted(logger, Digest(correlationId), Category(state));
    }

    /// <summary>Emits the <c>LifecycleSubscriberFaulted</c> hot-path event.</summary>
    public static void LifecycleSubscriberFaulted<TState>(
        ILogger? logger,
        string? correlationId,
        TState state,
        string failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogLifecycleSubscriberFaulted(
            logger,
            Digest(correlationId),
            Category(state),
            Category(failureCategory));
    }

    /// <summary>Emits the <c>LifecycleMessageCacheEvicted</c> hot-path event.</summary>
    public static void LifecycleMessageCacheEvicted(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogLifecycleMessageCacheEvicted(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>ETagStorageReadFailed</c> hot-path event.</summary>
    public static void ETagStorageReadFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagStorageReadFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>ETagIncompatibleEntry</c> hot-path event.</summary>
    public static void ETagIncompatibleEntry(
        ILogger? logger,
        string? key,
        int formatVersion,
        int payloadVersion,
        int expectedPayloadVersion)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogETagIncompatibleEntry(
            logger,
            Digest(key),
            formatVersion,
            payloadVersion,
            expectedPayloadVersion);
    }

    /// <summary>Emits the <c>ETagRemoveIncompatibleFailed</c> hot-path event.</summary>
    public static void ETagRemoveIncompatibleFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagRemoveIncompatibleFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>ETagStorageWriteFailed</c> hot-path event.</summary>
    public static void ETagStorageWriteFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagStorageWriteFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>ETagStorageRemoveFailed</c> hot-path event.</summary>
    public static void ETagStorageRemoveFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagStorageRemoveFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>ETagFamilyInvalidationReadFailed</c> hot-path event.</summary>
    public static void ETagFamilyInvalidationReadFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagFamilyInvalidationReadFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>ETagLruTimestampSeedFailed</c> hot-path event.</summary>
    public static void ETagLruTimestampSeedFailed(
        ILogger? logger,
        string? key,
        string failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagLruTimestampSeedFailed(logger, Digest(key), Category(failureCategory));
    }

    /// <summary>Emits the <c>ETagLruEvictionRemoveFailed</c> hot-path event.</summary>
    public static void ETagLruEvictionRemoveFailed(ILogger? logger, Exception exception, string? key)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogETagLruEvictionRemoveFailed(logger, Digest(key), Category(exception.GetType().Name));
    }

    /// <summary>Emits the <c>NewItemStateCleared</c> hot-path event.</summary>
    public static void NewItemStateCleared(ILogger? logger, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogNewItemStateCleared(logger, Category(reason));
    }

    /// <summary>Emits the <c>PendingOutcomeMissingIdentity</c> hot-path event.</summary>
    public static void PendingOutcomeMissingIdentity<TSource, TOutcome>(
        ILogger? logger,
        TSource source,
        TOutcome outcome)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingOutcomeMissingIdentity(logger, Category(source), Category(outcome));
    }

    /// <summary>Emits the <c>PendingOutcomeFallbackIdentityIncomplete</c> hot-path event.</summary>
    public static void PendingOutcomeFallbackIdentityIncomplete<TSource>(ILogger? logger, TSource source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingOutcomeFallbackIdentityIncomplete(logger, Category(source));
    }

    /// <summary>Emits the <c>PendingOutcomeNoMatch</c> hot-path event.</summary>
    public static void PendingOutcomeNoMatch<TSource>(ILogger? logger, TSource source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingOutcomeNoMatch(logger, Category(source));
    }

    /// <summary>Emits the <c>PendingOutcomeAmbiguous</c> hot-path event.</summary>
    public static void PendingOutcomeAmbiguous<TSource>(ILogger? logger, TSource source, int candidateCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingOutcomeAmbiguous(logger, Category(source), candidateCount);
    }

    /// <summary>Emits the <c>NewItemMetadataIncomplete</c> hot-path event.</summary>
    public static void NewItemMetadataIncomplete(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogNewItemMetadataIncomplete(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>PendingPollDuplicateTerminal</c> hot-path event.</summary>
    public static void PendingPollDuplicateTerminal(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingPollDuplicateTerminal(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>PendingPollNonResolved</c> hot-path event.</summary>
    public static void PendingPollNonResolved<TStatus>(
        ILogger? logger,
        TStatus status,
        string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingPollNonResolved(logger, Category(status), Digest(messageId));
    }

    /// <summary>Emits the <c>PendingRegistrationMessageRejected</c> hot-path event.</summary>
    public static void PendingRegistrationMessageRejected(ILogger? logger, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingRegistrationMessageRejected(logger, Category(reason));
    }

    /// <summary>Emits the <c>PendingRegistrationCorrelationRejected</c> hot-path event.</summary>
    public static void PendingRegistrationCorrelationRejected(ILogger? logger, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingRegistrationCorrelationRejected(logger, Category(reason));
    }

    /// <summary>Emits the <c>PendingRegistrationMetadataConflict</c> hot-path event.</summary>
    public static void PendingRegistrationMetadataConflict(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingRegistrationMetadataConflict(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>PendingTerminalRejected</c> hot-path event.</summary>
    public static void PendingTerminalRejected(ILogger? logger, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingTerminalRejected(logger, Category(reason));
    }

    /// <summary>Emits the <c>PendingTerminalUnknown</c> hot-path event.</summary>
    public static void PendingTerminalUnknown(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingTerminalUnknown(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>PendingStateCleared</c> hot-path event.</summary>
    public static void PendingStateCleared(ILogger? logger, string? reason, int outstandingPendingCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogPendingStateCleared(logger, Category(reason), outstandingPendingCount);
    }

    /// <summary>Emits the <c>PendingEvictedUnresolved</c> hot-path event.</summary>
    public static void PendingEvictedUnresolved(ILogger? logger, string? messageId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingEvictedUnresolved(logger, Digest(messageId));
    }

    /// <summary>Emits the <c>PendingEvictedDispatchSkipped</c> hot-path event.</summary>
    public static void PendingEvictedDispatchSkipped<TStatus>(
        ILogger? logger,
        string? messageId,
        TStatus currentStatus)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingEvictedDispatchSkipped(logger, Digest(messageId), Category(currentStatus));
    }

    /// <summary>Emits the <c>PendingLifecycleDisposed</c> hot-path event.</summary>
    public static void PendingLifecycleDisposed(ILogger? logger, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPendingLifecycleDisposed(logger, Category(reason));
    }

    /// <summary>Emits the <c>PendingLifecycleDispatchFailed</c> hot-path event.</summary>
    public static void PendingLifecycleDispatchFailed(
        ILogger? logger,
        string? reason,
        string? messageId,
        string failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPendingLifecycleDispatchFailed(
            logger,
            Category(reason),
            Digest(messageId),
            Category(failureCategory));
    }

    private static string Category<T>(T value)
    {
        string? text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Absent";
        }

        text = text.Trim();
        if (text.Length > 64 || text.Any(static character => !char.IsLetterOrDigit(character)
            && character is not '.' and not '_' and not '-'))
        {
            return Digest(text);
        }

        return text;
    }

    private static string Digest(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "absent";
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return $"sha256:{Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant()}";
    }

    [LoggerMessage(EventId = 5700, EventName = "LifecycleUnexpectedCorrelation", Level = LogLevel.Warning,
        Message = "{Diag} — FcLifecycleWrapper received transition for unexpected CorrelationId={Cid}")]
    private static partial void LogLifecycleUnexpectedCorrelation(ILogger logger, string diag, string cid);

    [LoggerMessage(EventId = 5701, EventName = "LifecycleIdempotencyResolved", Level = LogLevel.Information,
        Message = "{Diag} — idempotency-resolved transition observed for CorrelationId={Cid}")]
    private static partial void LogLifecycleIdempotencyResolved(ILogger logger, string diag, string cid);

    [LoggerMessage(EventId = 5702, EventName = "LifecycleIdempotentInfoBarRendered", Level = LogLevel.Information,
        Message = "{Diag} FcLifecycleWrapper rendered idempotent Info bar. CorrelationId={Cid}")]
    private static partial void LogLifecycleIdempotentInfoBarRendered(ILogger logger, string diag, string cid);

    /// <summary>Emits the <c>LifecycleTimerPhaseMarshaled</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5703, EventName = "LifecycleTimerPhaseMarshaled", Level = LogLevel.Debug,
        Message = "{Diag} Threshold timer phase update received off the UI thread; marshaling render work via InvokeAsync.")]
    public static partial void LifecycleTimerPhaseMarshaled(ILogger logger, string diag);

    [LoggerMessage(EventId = 5704, EventName = "PendingStatusProtocolFailure", Level = LogLevel.Warning,
        Message = "EventStore pending-command status response could not be used. FailureCategory={FailureCategory} MessageId={MessageId}")]
    private static partial void LogPendingStatusProtocolFailure(ILogger logger, string failureCategory, string messageId);

    /// <summary>Emits the <c>QueryNotModifiedWithoutCacheRepeated</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5705, EventName = "QueryNotModifiedWithoutCacheRepeated", Level = LogLevel.Warning,
        Message = "EventStore returned 304 Not Modified twice without a matching cache entry — failing loudly to preserve visible UI state.")]
    public static partial void QueryNotModifiedWithoutCacheRepeated(ILogger logger);

    /// <summary>Emits the <c>QueryNotModifiedWithoutCacheRetry</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5706, EventName = "QueryNotModifiedWithoutCacheRetry", Level = LogLevel.Information,
        Message = "EventStore returned 304 Not Modified without a matching cache entry — retrying once uncached (Story 5-2 D10).")]
    public static partial void QueryNotModifiedWithoutCacheRetry(ILogger logger);

    /// <summary>Emits the <c>ProjectionSubscriptionGateTimeout</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5707, EventName = "ProjectionSubscriptionGateTimeout", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal timed out waiting for the operation gate. FailureCategory={FailureCategory}")]
    public static partial void ProjectionSubscriptionGateTimeout(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionSubscriptionDisposalFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5708, EventName = "ProjectionSubscriptionDisposalFailed", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal failed. FailureCategory={FailureCategory}")]
    public static partial void ProjectionSubscriptionDisposalFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionChangeSubscriberFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5709, EventName = "ProjectionChangeSubscriberFailed", Level = LogLevel.Warning,
        Message = "Projection change subscriber threw while handling nudge. FailureCategory={FailureCategory}")]
    public static partial void ProjectionChangeSubscriberFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionChangeDetailSubscriberFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5710, EventName = "ProjectionChangeDetailSubscriberFailed", Level = LogLevel.Warning,
        Message = "Projection change detail subscriber threw while handling nudge. FailureCategory={FailureCategory}")]
    public static partial void ProjectionChangeDetailSubscriberFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ReconciliationCoordinatorMissing</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5711, EventName = "ReconciliationCoordinatorMissing", Level = LogLevel.Information,
        Message = "Projection reconciliation coordinator is not registered. Reconnect catch-up will not run.")]
    public static partial void ReconciliationCoordinatorMissing(ILogger logger);

    /// <summary>Emits the <c>ReconnectionReconciliationCallbackFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5712, EventName = "ReconnectionReconciliationCallbackFailed", Level = LogLevel.Warning,
        Message = "Reconnection reconciliation threw out of the hub callback. FailureCategory={FailureCategory}")]
    public static partial void ReconnectionReconciliationCallbackFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartGateUnavailable</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5713, EventName = "HubClosedRestartGateUnavailable", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart skipped because the operation gate was unavailable. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartGateUnavailable(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartTimedOut</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5714, EventName = "HubClosedRestartTimedOut", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart attempt timed out. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartTimedOut(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5715, EventName = "HubClosedRestartFailed", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart failed. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartCanceledDuringDisposal</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5716, EventName = "HubClosedRestartCanceledDuringDisposal", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart canceled during disposal. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartCanceledDuringDisposal(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartTimedOutOrCanceled</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5717, EventName = "HubClosedRestartTimedOutOrCanceled", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart timed out or was canceled. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartTimedOutOrCanceled(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>HubClosedRestartDisposedSource</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5718, EventName = "HubClosedRestartDisposedSource", Level = LogLevel.Warning,
        Message = "EventStore projection hub closed-restart canceled during disposal. FailureCategory={FailureCategory}")]
    public static partial void HubClosedRestartDisposedSource(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionReconnectRejoinGateUnavailable</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5719, EventName = "ProjectionReconnectRejoinGateUnavailable", Level = LogLevel.Warning,
        Message = "Projection reconnect rejoin skipped because the operation gate was unavailable. FailureCategory={FailureCategory}")]
    public static partial void ProjectionReconnectRejoinGateUnavailable(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionDisposalOperationWaitTimedOut</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5720, EventName = "ProjectionDisposalOperationWaitTimedOut", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}")]
    public static partial void ProjectionDisposalOperationWaitTimedOut(ILogger logger, string operation, string failureCategory);

    /// <summary>Emits the <c>ProjectionDisposalOperationCanceledByTimeout</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5721, EventName = "ProjectionDisposalOperationCanceledByTimeout", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}")]
    public static partial void ProjectionDisposalOperationCanceledByTimeout(ILogger logger, string operation, string failureCategory);

    /// <summary>Emits the <c>ProjectionDisposalOperationFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5722, EventName = "ProjectionDisposalOperationFailed", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal operation failed. Operation={Operation}, FailureCategory={FailureCategory}")]
    public static partial void ProjectionDisposalOperationFailed(ILogger logger, string operation, string failureCategory);

    /// <summary>Emits the <c>ProjectionDisposeBoundedTimedOut</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5723, EventName = "ProjectionDisposeBoundedTimedOut", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}")]
    public static partial void ProjectionDisposeBoundedTimedOut(ILogger logger, string operation, string failureCategory);

    /// <summary>Emits the <c>ProjectionDisposeBoundedFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5724, EventName = "ProjectionDisposeBoundedFailed", Level = LogLevel.Warning,
        Message = "EventStore projection subscription disposal operation failed. Operation={Operation}, FailureCategory={FailureCategory}")]
    public static partial void ProjectionDisposeBoundedFailed(ILogger logger, string operation, string failureCategory);

    [LoggerMessage(EventId = 5725, EventName = "ProjectionHubStateSubscriberFailed", Level = LogLevel.Warning,
        Message = "EventStore projection hub state subscriber threw. State={State}, FailureCategory={FailureCategory}")]
    private static partial void LogProjectionHubStateSubscriberFailed(ILogger logger, string state, string failureCategory);

    /// <summary>Emits the <c>PendingPollingTickFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5726, EventName = "PendingPollingTickFailed", Level = LogLevel.Warning,
        Message = "Pending command polling driver tick failed. FailureCategory={FailureCategory}")]
    public static partial void PendingPollingTickFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>PendingPollingDisposeTimedOut</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5727, EventName = "PendingPollingDisposeTimedOut", Level = LogLevel.Warning,
        Message = "Pending command polling driver disposal timed out waiting for the in-flight poll. FailureCategory={FailureCategory}")]
    public static partial void PendingPollingDisposeTimedOut(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>PendingPollingDisposeFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5728, EventName = "PendingPollingDisposeFailed", Level = LogLevel.Warning,
        Message = "Pending command polling driver disposal observed an in-flight poll failure. FailureCategory={FailureCategory}")]
    public static partial void PendingPollingDisposeFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionFallbackPollingDisposeTimedOut</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5729, EventName = "ProjectionFallbackPollingDisposeTimedOut", Level = LogLevel.Warning,
        Message = "Projection fallback polling driver disposal timed out waiting for the in-flight loop. FailureCategory={FailureCategory}")]
    public static partial void ProjectionFallbackPollingDisposeTimedOut(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionFallbackPollingDisposeFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5730, EventName = "ProjectionFallbackPollingDisposeFailed", Level = LogLevel.Warning,
        Message = "Projection fallback polling driver disposal observed an in-flight loop failure. FailureCategory={FailureCategory}")]
    public static partial void ProjectionFallbackPollingDisposeFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ProjectionFallbackPollingTerminated</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5731, EventName = "ProjectionFallbackPollingTerminated", Level = LogLevel.Warning,
        Message = "Projection fallback polling loop terminated unexpectedly. FailureCategory={FailureCategory}")]
    public static partial void ProjectionFallbackPollingTerminated(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ReconciliationBudgetZero</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5732, EventName = "ReconciliationBudgetZero", Level = LogLevel.Warning,
        Message = "Reconciliation budget is zero (MaxProjectionFallbackPollingLanes={Budget}); reconcile pass skipped.")]
    public static partial void ReconciliationBudgetZero(ILogger logger, int budget);

    [LoggerMessage(EventId = 5733, EventName = "ReconciliationLaneMissingTenant", Level = LogLevel.Information,
        Message = "Reconciliation skipped lane without tenant context. ViewKey={ViewKey}, ProjectionType={ProjectionType}")]
    private static partial void LogReconciliationLaneMissingTenant(ILogger logger, string viewKey, string projectionType);

    [LoggerMessage(EventId = 5734, EventName = "ReconciliationLaneDegraded", Level = LogLevel.Information,
        Message = "Reconciliation skipped lane for degraded projection group. ViewKey={ViewKey}, ProjectionType={ProjectionType}")]
    private static partial void LogReconciliationLaneDegraded(ILogger logger, string viewKey, string projectionType);

    [LoggerMessage(EventId = 5735, EventName = "ProjectionRefreshNegativeCount", Level = LogLevel.Warning,
        Message = "Projection refresh returned negative TotalCount; treating as protocol failure. ViewKey={ViewKey}")]
    private static partial void LogProjectionRefreshNegativeCount(ILogger logger, string viewKey);

    [LoggerMessage(EventId = 5736, EventName = "LifecycleReplayCallbackFaulted", Level = LogLevel.Error,
        Message = "Lifecycle subscribe replay callback faulted. CorrelationId={CorrelationId} FailureCategory={FailureCategory}")]
    private static partial void LogLifecycleReplayCallbackFaulted(ILogger logger, string correlationId, string failureCategory);

    [LoggerMessage(EventId = 5737, EventName = "LifecycleCrossCorrelationDuplicate", Level = LogLevel.Warning,
        Message = "HFC2005: duplicate MessageId detected across CorrelationIds (treated as fresh submission). CorrelationId={CorrelationId} MessageId={MessageId}")]
    private static partial void LogLifecycleCrossCorrelationDuplicate(ILogger logger, string correlationId, string messageId);

    [LoggerMessage(EventId = 5738, EventName = "LifecycleInvalidTransition", Level = LogLevel.Error,
        Message = "HFC2004: invalid lifecycle transition dropped. CorrelationId={CorrelationId} From={From} To={To} MessageId={MessageId}")]
    private static partial void LogLifecycleInvalidTransition(ILogger logger, string correlationId, string from, string to, string messageId);

    [LoggerMessage(EventId = 5739, EventName = "LifecycleMissingSubmitted", Level = LogLevel.Warning,
        Message = "HFC2007: transition arrived for a CorrelationId without a prior Submitted observation. CorrelationId={CorrelationId} State={State}")]
    private static partial void LogLifecycleMissingSubmitted(ILogger logger, string correlationId, string state);

    [LoggerMessage(EventId = 5740, EventName = "LifecycleSubscriberFaulted", Level = LogLevel.Error,
        Message = "Lifecycle subscriber callback faulted. CorrelationId={CorrelationId} NewState={NewState} FailureCategory={FailureCategory}")]
    private static partial void LogLifecycleSubscriberFaulted(ILogger logger, string correlationId, string newState, string failureCategory);

    [LoggerMessage(EventId = 5741, EventName = "LifecycleMessageCacheEvicted", Level = LogLevel.Debug,
        Message = "Lifecycle MessageId cache evicted oldest. Evicted={Evicted}")]
    private static partial void LogLifecycleMessageCacheEvicted(ILogger logger, string evicted);

    [LoggerMessage(EventId = 5742, EventName = "ETagStorageReadFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: storage read failed for redacted key {KeyHash} — degrading to cache miss. FailureCategory={FailureCategory}")]
    private static partial void LogETagStorageReadFailed(ILogger logger, string keyHash, string failureCategory);

    [LoggerMessage(EventId = 5743, EventName = "ETagIncompatibleEntry", Level = LogLevel.Information,
        Message = "ETagCacheService: incompatible cache entry for redacted key {KeyHash} (FormatVersion={FormatVersion}, PayloadVersion={PayloadVersion}, ExpectedPayloadVersion={ExpectedPayloadVersion}) — diagnostic miss.")]
    private static partial void LogETagIncompatibleEntry(ILogger logger, string keyHash, int formatVersion, int payloadVersion, int expectedPayloadVersion);

    [LoggerMessage(EventId = 5744, EventName = "ETagRemoveIncompatibleFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: failed to remove incompatible entry for redacted key {KeyHash} — best-effort cleanup. FailureCategory={FailureCategory}")]
    private static partial void LogETagRemoveIncompatibleFailed(ILogger logger, string keyHash, string failureCategory);

    [LoggerMessage(EventId = 5745, EventName = "ETagStorageWriteFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: storage write failed for redacted key {KeyHash} — entry not persisted. FailureCategory={FailureCategory}")]
    private static partial void LogETagStorageWriteFailed(ILogger logger, string keyHash, string failureCategory);

    [LoggerMessage(EventId = 5746, EventName = "ETagStorageRemoveFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: storage remove failed for redacted key {KeyHash} — best-effort cleanup. FailureCategory={FailureCategory}")]
    private static partial void LogETagStorageRemoveFailed(ILogger logger, string keyHash, string failureCategory);

    /// <summary>Emits the <c>ETagFamilyInvalidationSegmentInvalid</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5747, EventName = "ETagFamilyInvalidationSegmentInvalid", Level = LogLevel.Warning,
        Message = "ETagCacheService: tenant-scoped family invalidation skipped — tenant or user identifier failed segment validation.")]
    public static partial void ETagFamilyInvalidationSegmentInvalid(ILogger logger);

    /// <summary>Emits the <c>ETagFamilyInvalidationCanonicalizationInvalid</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5748, EventName = "ETagFamilyInvalidationCanonicalizationInvalid", Level = LogLevel.Warning,
        Message = "ETagCacheService: tenant-scoped family invalidation skipped — tenant or user identifier failed canonicalization.")]
    public static partial void ETagFamilyInvalidationCanonicalizationInvalid(ILogger logger);

    [LoggerMessage(EventId = 5749, EventName = "ETagFamilyInvalidationReadFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: family invalidation read failed for redacted key {KeyHash} — skipping. FailureCategory={FailureCategory}")]
    private static partial void LogETagFamilyInvalidationReadFailed(ILogger logger, string keyHash, string failureCategory);

    /// <summary>Emits the <c>ETagLruEnumerationFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5750, EventName = "ETagLruEnumerationFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: failed to enumerate persisted keys for cache LRU seeding. FailureCategory={FailureCategory}")]
    public static partial void ETagLruEnumerationFailed(ILogger logger, string failureCategory);

    [LoggerMessage(EventId = 5751, EventName = "ETagLruTimestampSeedFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: failed to seed LRU timestamp for redacted key {KeyHash}. FailureCategory={FailureCategory}")]
    private static partial void LogETagLruTimestampSeedFailed(ILogger logger, string keyHash, string failureCategory);

    [LoggerMessage(EventId = 5752, EventName = "ETagLruEvictionRemoveFailed", Level = LogLevel.Warning,
        Message = "ETagCacheService: LRU eviction storage remove failed for redacted key {KeyHash}. FailureCategory={FailureCategory}")]
    private static partial void LogETagLruEvictionRemoveFailed(ILogger logger, string keyHash, string failureCategory);

    [LoggerMessage(EventId = 5753, EventName = "NewItemStateCleared", Level = LogLevel.Information,
        Message = "New-item indicator state cleared. Reason={Reason}")]
    private static partial void LogNewItemStateCleared(ILogger logger, string reason);

    /// <summary>Emits the <c>NewItemScopeTransition</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5754, EventName = "NewItemScopeTransition", Level = LogLevel.Warning,
        Message = "New-item indicator tenant/user transition detected; flushing state.")]
    public static partial void NewItemScopeTransition(ILogger logger);

    [LoggerMessage(EventId = 5755, EventName = "PendingOutcomeMissingIdentity", Level = LogLevel.Warning,
        Message = "Pending command outcome dropped because both MessageId and EntityKey were absent. Source={Source} Outcome={Outcome}")]
    private static partial void LogPendingOutcomeMissingIdentity(ILogger logger, string source, string outcome);

    [LoggerMessage(EventId = 5756, EventName = "PendingOutcomeFallbackIdentityIncomplete", Level = LogLevel.Debug,
        Message = "Pending command outcome ignored because fallback row identity metadata was incomplete. Source={Source}")]
    private static partial void LogPendingOutcomeFallbackIdentityIncomplete(ILogger logger, string source);

    [LoggerMessage(EventId = 5757, EventName = "PendingOutcomeNoMatch", Level = LogLevel.Debug,
        Message = "Pending command outcome ignored because no framework-controlled match was found. Source={Source}")]
    private static partial void LogPendingOutcomeNoMatch(ILogger logger, string source);

    [LoggerMessage(EventId = 5758, EventName = "PendingOutcomeAmbiguous", Level = LogLevel.Warning,
        Message = "Pending command outcome left unresolved because framework-controlled matching was ambiguous. Source={Source} CandidateCount={CandidateCount}")]
    private static partial void LogPendingOutcomeAmbiguous(ILogger logger, string source, int candidateCount);

    [LoggerMessage(EventId = 5759, EventName = "NewItemMetadataIncomplete", Level = LogLevel.Debug,
        Message = "New-item indicator skipped because pending command row metadata is incomplete. MessageId={MessageId}")]
    private static partial void LogNewItemMetadataIncomplete(ILogger logger, string messageId);

    [LoggerMessage(EventId = 5760, EventName = "PendingPollDuplicateTerminal", Level = LogLevel.Debug,
        Message = "Pending command polling observed duplicate terminal. MessageId={MessageId}")]
    private static partial void LogPendingPollDuplicateTerminal(ILogger logger, string messageId);

    [LoggerMessage(EventId = 5761, EventName = "PendingPollNonResolved", Level = LogLevel.Warning,
        Message = "Pending command polling produced non-resolved status. Status={Status} MessageId={MessageId}")]
    private static partial void LogPendingPollNonResolved(ILogger logger, string status, string messageId);

    [LoggerMessage(EventId = 5762, EventName = "PendingRegistrationMessageRejected", Level = LogLevel.Warning,
        Message = "Pending command registration rejected. Reason={Reason}")]
    private static partial void LogPendingRegistrationMessageRejected(ILogger logger, string reason);

    [LoggerMessage(EventId = 5763, EventName = "PendingRegistrationCorrelationRejected", Level = LogLevel.Warning,
        Message = "Pending command registration rejected. Reason={Reason}")]
    private static partial void LogPendingRegistrationCorrelationRejected(ILogger logger, string reason);

    [LoggerMessage(EventId = 5764, EventName = "PendingRegistrationMetadataConflict", Level = LogLevel.Warning,
        Message = "Pending command duplicate registration rejected because framework metadata conflicts. MessageId={MessageId}")]
    private static partial void LogPendingRegistrationMetadataConflict(ILogger logger, string messageId);

    [LoggerMessage(EventId = 5765, EventName = "PendingTerminalRejected", Level = LogLevel.Warning,
        Message = "Pending command terminal observation rejected. Reason={Reason}")]
    private static partial void LogPendingTerminalRejected(ILogger logger, string reason);

    [LoggerMessage(EventId = 5766, EventName = "PendingTerminalUnknown", Level = LogLevel.Debug,
        Message = "Pending command terminal observation ignored for unknown MessageId. MessageId={MessageId}")]
    private static partial void LogPendingTerminalUnknown(ILogger logger, string messageId);

    [LoggerMessage(EventId = 5767, EventName = "PendingStateCleared", Level = LogLevel.Information,
        Message = "Pending command state cleared. Reason={Reason} OutstandingPendingCount={Count}")]
    private static partial void LogPendingStateCleared(ILogger logger, string reason, int count);

    [LoggerMessage(EventId = 5768, EventName = "PendingEvictedUnresolved", Level = LogLevel.Warning,
        Message = "Pending command evicted unresolved because MaxPendingCommandEntries was exceeded. MessageId={MessageId}")]
    private static partial void LogPendingEvictedUnresolved(ILogger logger, string messageId);

    [LoggerMessage(EventId = 5769, EventName = "PendingEvictedDispatchSkipped", Level = LogLevel.Debug,
        Message = "Skipping evicted lifecycle dispatch because the entry is no longer in NeedsReview state. MessageId={MessageId} CurrentStatus={CurrentStatus}")]
    private static partial void LogPendingEvictedDispatchSkipped(ILogger logger, string messageId, string currentStatus);

    [LoggerMessage(EventId = 5770, EventName = "PendingLifecycleDisposed", Level = LogLevel.Debug,
        Message = "Pending command lifecycle dispatch skipped because LifecycleStateService is disposed. Reason={Reason}")]
    private static partial void LogPendingLifecycleDisposed(ILogger logger, string reason);

    [LoggerMessage(EventId = 5771, EventName = "PendingLifecycleDispatchFailed", Level = LogLevel.Warning,
        Message = "Pending command lifecycle dispatch failed during {Reason}. MessageId={MessageId} FailureCategory={FailureCategory}")]
    private static partial void LogPendingLifecycleDispatchFailed(ILogger logger, string reason, string messageId, string failureCategory);

    /// <summary>Emits the <c>PendingScopeTransition</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5772, EventName = "PendingScopeTransition", Level = LogLevel.Warning,
        Message = "Pending command tenant/user transition detected; flushing pending state.")]
    public static partial void PendingScopeTransition(ILogger logger);

    /// <summary>Emits the <c>ProjectionStateSubscriberFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5773, EventName = "ProjectionStateSubscriberFailed", Level = LogLevel.Warning,
        Message = "Projection connection state subscriber threw. FailureCategory={FailureCategory}")]
    public static partial void ProjectionStateSubscriberFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ReconciliationStateStartFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5774, EventName = "ReconciliationStateStartFailed", Level = LogLevel.Warning,
        Message = "Reconciliation state.Start threw. Epoch={Epoch}, FailureCategory={FailureCategory}")]
    public static partial void ReconciliationStateStartFailed(ILogger logger, long epoch, string failureCategory);

    /// <summary>Emits the <c>ReconciliationFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5775, EventName = "ReconciliationFailed", Level = LogLevel.Warning,
        Message = "Reconnection reconciliation failed. Epoch={Epoch}, FailureCategory={FailureCategory}")]
    public static partial void ReconciliationFailed(ILogger logger, long epoch, string failureCategory);

    /// <summary>Emits the <c>ReconciliationSweepMarkerFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5776, EventName = "ReconciliationSweepMarkerFailed", Level = LogLevel.Warning,
        Message = "Sweep marker dispatch failed. Epoch={Epoch}, FailureCategory={FailureCategory}")]
    public static partial void ReconciliationSweepMarkerFailed(ILogger logger, long epoch, string failureCategory);

    /// <summary>Emits the <c>ReconciliationPendingResolutionFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5777, EventName = "ReconciliationPendingResolutionFailed", Level = LogLevel.Warning,
        Message = "Pending command resolution from reconnect failed. Epoch={Epoch} FailureCategory={FailureCategory}")]
    public static partial void ReconciliationPendingResolutionFailed(ILogger logger, long epoch, string failureCategory);

    /// <summary>Emits the <c>ReconciliationStateResetFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5778, EventName = "ReconciliationStateResetFailed", Level = LogLevel.Warning,
        Message = "Reconciliation state.Reset threw during dispose. FailureCategory={FailureCategory}")]
    public static partial void ReconciliationStateResetFailed(ILogger logger, string failureCategory);

    /// <summary>Emits the <c>ReconciliationSweepCleanupFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5779, EventName = "ReconciliationSweepCleanupFailed", Level = LogLevel.Warning,
        Message = "Sweep cleanup dispatch failed. Epoch={Epoch}, FailureCategory={FailureCategory}")]
    public static partial void ReconciliationSweepCleanupFailed(ILogger logger, long epoch, string failureCategory);

    /// <summary>Emits the <c>ReconciliationStateSubscriberFailed</c> hot-path event.</summary>
    [LoggerMessage(EventId = 5780, EventName = "ReconciliationStateSubscriberFailed", Level = LogLevel.Warning,
        Message = "Reconnection reconciliation state subscriber threw. FailureCategory={FailureCategory}")]
    public static partial void ReconciliationStateSubscriberFailed(ILogger logger, string failureCategory);
}
