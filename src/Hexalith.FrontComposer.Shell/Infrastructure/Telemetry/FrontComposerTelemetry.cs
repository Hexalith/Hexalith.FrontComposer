using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.Telemetry;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>Shell-owned telemetry primitives for the FrontComposer framework.</summary>
internal static class FrontComposerTelemetry {
    public static readonly ActivitySource Source = new(
        FrontComposerActivitySource.Name,
        FrontComposerActivitySource.Version);

    public const string CommandDispatchOperation = "frontcomposer.command.dispatch";
    public const string QueryExecuteOperation = "frontcomposer.query.execute";
    public const string ProjectionNudgeOperation = "frontcomposer.projection.nudge";
    public const string ProjectionFallbackPollOperation = "frontcomposer.projection.fallback_poll";
    public const string ProjectionConnectionTransitionOperation = "frontcomposer.projection.connection_transition";
    public const string ProjectionRejoinOperation = "frontcomposer.projection.rejoin";
    public const string LifecycleTransitionOperation = "frontcomposer.lifecycle.transition";
    public const string PendingCommandOutcomeOperation = "frontcomposer.pending_command.outcome";

    public const string CommandTypeTag = "frontcomposer.command.type";
    public const string ProjectionTypeTag = "frontcomposer.projection.type";
    public const string QueryTypeTag = "frontcomposer.query.type";
    public const string MessageIdTag = "frontcomposer.message_id";
    public const string CorrelationIdTag = "frontcomposer.correlation_id";
    public const string TenantMarkerTag = "frontcomposer.tenant";
    public const string OutcomeTag = "frontcomposer.outcome";
    public const string FailureCategoryTag = "frontcomposer.failure_category";
    public const string TransportTag = "frontcomposer.transport";
    public const string HttpStatusCodeTag = "http.response.status_code";
    public const string CacheOutcomeTag = "frontcomposer.cache.outcome";
    public const string ElapsedMsTag = "frontcomposer.elapsed_ms";
    public const string LifecycleStateTag = "frontcomposer.lifecycle.state";
    public const string IdempotencyResolvedTag = "frontcomposer.idempotency_resolved";
    public const string SuppressedCountTag = "frontcomposer.log.suppressed_count";

    public static Activity? StartCommandDispatch(string commandType, string messageId, string tenantMarker)
        => Start(
            CommandDispatchOperation,
            ActivityKind.Client,
            (CommandTypeTag, SafeTypeName(commandType)),
            (MessageIdTag, SafeIdentifier(messageId)),
            (TenantMarkerTag, tenantMarker),
            (TransportTag, "http"));

    public static Activity? StartQueryExecute(string projectionType, string queryType, string cacheOutcome, string tenantMarker)
        => Start(
            QueryExecuteOperation,
            ActivityKind.Client,
            (ProjectionTypeTag, SafeTypeName(projectionType)),
            (QueryTypeTag, SafeTypeName(queryType)),
            (CacheOutcomeTag, cacheOutcome),
            (TenantMarkerTag, tenantMarker),
            (TransportTag, "http"));

    public static Activity? StartProjectionNudge(string projectionType, string tenantMarker)
        => Start(
            ProjectionNudgeOperation,
            ActivityKind.Internal,
            (ProjectionTypeTag, SafeTypeName(projectionType)),
            (TenantMarkerTag, tenantMarker),
            (TransportTag, "signalr"));

    public static Activity? StartProjectionFallbackPoll()
        => Start(ProjectionFallbackPollOperation, ActivityKind.Internal);

    public static Activity? StartProjectionConnectionTransition(string status, string? failureCategory, int attempt, int suppressedCount)
        => Start(
            ProjectionConnectionTransitionOperation,
            ActivityKind.Internal,
            (OutcomeTag, SafeTypeName(status)),
            (FailureCategoryTag, BoundCategory(failureCategory) ?? "none"),
            ("frontcomposer.reconnect_attempt", attempt),
            (SuppressedCountTag, suppressedCount));

    public static Activity? StartProjectionRejoin(string projectionType, string tenantMarker)
        => Start(
            ProjectionRejoinOperation,
            ActivityKind.Internal,
            (ProjectionTypeTag, SafeTypeName(projectionType)),
            (TenantMarkerTag, tenantMarker),
            (TransportTag, "signalr"));

    public static Activity? StartLifecycleTransition(
        string state,
        string correlationId,
        string? messageId,
        bool idempotencyResolved)
        => Start(
            LifecycleTransitionOperation,
            ActivityKind.Internal,
            (LifecycleStateTag, SafeTypeName(state)),
            (CorrelationIdTag, SafeIdentifier(correlationId)),
            (MessageIdTag, SafeIdentifier(messageId)),
            (IdempotencyResolvedTag, idempotencyResolved));

    public static Activity? StartPendingCommandOutcome(string outcome, string? commandType, string messageId, string correlationId)
        => Start(
            PendingCommandOutcomeOperation,
            ActivityKind.Internal,
            (OutcomeTag, SafeTypeName(outcome)),
            (CommandTypeTag, SafeTypeName(commandType)),
            (MessageIdTag, SafeIdentifier(messageId)),
            (CorrelationIdTag, SafeIdentifier(correlationId)));

    public static void SetOutcome(Activity? activity, string? outcome)
        => SetTag(activity, OutcomeTag, BoundCategory(outcome));

    public static void SetFailure(Activity? activity, string? failureCategory) {
        string? bounded = BoundCategory(failureCategory);
        if (bounded is null) {
            return;
        }

        SetTag(activity, FailureCategoryTag, bounded);
        activity?.SetStatus(ActivityStatusCode.Error, bounded);
    }

    public static void SetHttpStatus(Activity? activity, int statusCode)
        => activity?.SetTag(HttpStatusCodeTag, statusCode);

    public static void SetElapsed(Activity? activity, TimeSpan elapsed)
        => activity?.SetTag(ElapsedMsTag, elapsed.TotalMilliseconds);

    public static void SetCorrelation(Activity? activity, string? correlationId)
        => SetTag(activity, CorrelationIdTag, SafeIdentifier(correlationId));

    public static string TenantMarker(string? tenant)
        => string.IsNullOrWhiteSpace(tenant) ? "absent" : "present";

    public static string? BoundCategory(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();
        Span<char> buffer = stackalloc char[Math.Min(span.Length, 64)];
        int written = 0;
        foreach (char ch in span) {
            if (written >= buffer.Length) {
                break;
            }

            buffer[written++] = char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '_';
        }

        return written == 0 ? null : new string(buffer[..written]);
    }

    private static Activity? Start(string name, ActivityKind kind, params (string Key, object? Value)[] tags) {
        Activity? activity = Source.StartActivity(name, kind);
        if (activity is null) {
            return null;
        }

        foreach ((string key, object? value) in tags) {
            if (value is null) {
                continue;
            }

            if (value is string text) {
                SetTag(activity, key, text);
            }
            else {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }

    private static void SetTag(Activity? activity, string key, string? value) {
        if (activity is null || string.IsNullOrWhiteSpace(value)) {
            return;
        }

        activity.SetTag(key, value);
    }

    private static string? SafeIdentifier(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : BoundCategory(value);

    private static string? SafeTypeName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : SanitizeBounded(value, 256);

    private static string? SanitizeBounded(string value, int maxLength) {
        ReadOnlySpan<char> span = value.AsSpan().Trim();
        Span<char> buffer = stackalloc char[Math.Min(span.Length, maxLength)];
        int written = 0;
        foreach (char ch in span) {
            if (written >= buffer.Length) {
                break;
            }

            buffer[written++] = char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or '+' ? ch : '_';
        }

        return written == 0 ? null : new string(buffer[..written]);
    }
}
