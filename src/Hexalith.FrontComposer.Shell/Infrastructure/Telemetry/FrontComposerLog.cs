using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

internal static partial class FrontComposerLog {
    [LoggerMessage(
        EventId = 5601,
        Level = LogLevel.Warning,
        Message = "EventStore command dispatch returned unexpected HTTP status {StatusCode}. CommandType={CommandType} MessageId={MessageId} FailureCategory={FailureCategory} LocationPresent={LocationPresent} ElapsedMs={ElapsedMs}")]
    public static partial void CommandUnexpectedStatus(
        ILogger logger,
        int statusCode,
        string commandType,
        string messageId,
        string failureCategory,
        bool locationPresent,
        double elapsedMs);

    [LoggerMessage(
        EventId = 5602,
        Level = LogLevel.Warning,
        Message = "EventStore command response body could not be parsed as JSON; correlationId unavailable. ContentType={ContentType} CommandType={CommandType} MessageId={MessageId}")]
    public static partial void CommandCorrelationBodyParseFailed(
        ILogger logger,
        string? contentType,
        string commandType,
        string messageId);

    [LoggerMessage(
        EventId = 5610,
        Level = LogLevel.Warning,
        Message = "EventStore query returned unexpected HTTP status {StatusCode}. ProjectionType={ProjectionType} QueryType={QueryType} FailureCategory={FailureCategory} ElapsedMs={ElapsedMs}")]
    public static partial void QueryUnexpectedStatus(
        ILogger logger,
        int statusCode,
        string projectionType,
        string queryType,
        string failureCategory,
        double elapsedMs);

    [LoggerMessage(
        EventId = 5611,
        Level = LogLevel.Warning,
        Message = "Projection response schema mismatch. ProjectionType={ProjectionType}, FailureCategory={FailureCategory}")]
    public static partial void QuerySchemaMismatch(
        ILogger logger,
        string projectionType,
        string failureCategory);

    [LoggerMessage(
        EventId = 5612,
        Level = LogLevel.Warning,
        Message = "Best-effort projection cache invalidation failed during schema-mismatch handling. FailureCategory={FailureCategory}")]
    public static partial void QuerySchemaMismatchInvalidationFailed(
        ILogger logger,
        string failureCategory);

    [LoggerMessage(
        EventId = 5613,
        Level = LogLevel.Warning,
        Message = "EventStore query cache write failed after successful 200 OK response. FailureCategory={FailureCategory}")]
    public static partial void QueryCacheWriteFailed(ILogger logger, string failureCategory);

    [LoggerMessage(
        EventId = 5620,
        Level = LogLevel.Information,
        Message = "EventStore projection connection state changed. Status={Status}, Attempt={Attempt}, FailureCategory={FailureCategory}, SuppressedCount={SuppressedCount}")]
    public static partial void ProjectionConnectionChanged(
        ILogger logger,
        string status,
        int attempt,
        string failureCategory,
        int suppressedCount);

    [LoggerMessage(
        EventId = 5621,
        Level = LogLevel.Warning,
        Message = "Projection refresh failed. ProjectionType={ProjectionType} FailureCategory={FailureCategory}")]
    public static partial void ProjectionRefreshFailed(ILogger logger, string projectionType, string failureCategory);

    [LoggerMessage(
        EventId = 5622,
        Level = LogLevel.Warning,
        Message = "Projection fallback polling iteration failed. FailureCategory={FailureCategory}")]
    public static partial void ProjectionFallbackPollingIterationFailed(ILogger logger, string failureCategory);

    [LoggerMessage(
        EventId = 5623,
        Level = LogLevel.Warning,
        Message = "EventStore projection group rejoin failed. ProjectionType={ProjectionType} FailureCategory={FailureCategory}")]
    public static partial void ProjectionRejoinFailed(ILogger logger, string projectionType, string failureCategory);

    [LoggerMessage(
        EventId = 5630,
        Level = LogLevel.Warning,
        Message = "Pending command polling failed. FailureCategory={FailureCategory} MessageId={MessageId}")]
    public static partial void PendingCommandPollingFailed(ILogger logger, string failureCategory, string messageId);

    [LoggerMessage(
        EventId = 5631,
        Level = LogLevel.Warning,
        Message = "Pending command lifecycle terminal dispatch failed. MessageId={MessageId} Outcome={Outcome} FailureCategory={FailureCategory}")]
    public static partial void PendingCommandLifecycleTerminalDispatchFailed(
        ILogger logger,
        string messageId,
        string outcome,
        string failureCategory);

    [LoggerMessage(
        EventId = 5640,
        Level = LogLevel.Information,
        Message = "Lifecycle transition observed. CorrelationId={CorrelationId} MessageId={MessageId} State={State} IdempotencyResolved={IdempotencyResolved}")]
    public static partial void LifecycleTransitionObserved(
        ILogger logger,
        string correlationId,
        string? messageId,
        string state,
        bool idempotencyResolved);
}
