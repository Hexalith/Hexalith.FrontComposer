using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Source-generated log helpers for MCP fail-closed branches.
/// </summary>
internal static partial class FrontComposerMcpLog {
    private static readonly Lazy<byte[]> RejectionDigestKey = new(() => RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Logs a sanitized tools/list fail-closed warning.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="category">Bounded MCP failure category.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void ToolsListFailedClosed(
        ILogger? logger,
        FrontComposerMcpFailureCategory category,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string categoryName = category.ToString();
        LogToolsListFailedClosed(logger, categoryName, exceptionType);
    }

    /// <summary>
    /// Logs a sanitized lifecycle route precheck warning.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="category">Bounded MCP failure category.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void LifecyclePrecheckFailedClosed(
        ILogger? logger,
        FrontComposerMcpFailureCategory category,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string categoryName = category.ToString();
        LogLifecyclePrecheckFailedClosed(logger, categoryName, exceptionType);
    }

    /// <summary>
    /// Logs a sanitized projection reader fail-closed warning.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="category">Bounded MCP failure category.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void ProjectionReaderFailedClosed(
        ILogger? logger,
        FrontComposerMcpFailureCategory category,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string categoryName = category.ToString();
        LogProjectionReaderFailedClosed(logger, categoryName, exceptionType);
    }

    /// <summary>
    /// Logs a sanitized tenant-gate denial caused by a gate exception.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="boundedContext">Manifest bounded context to hash before logging.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void TenantToolGateFailedClosed(
        ILogger? logger,
        string boundedContext,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string boundedContextDigest = SanitizeCategoryValue(boundedContext);
        LogTenantToolGateFailedClosed(logger, boundedContextDigest, exceptionType);
    }

    /// <summary>
    /// Logs a sanitized policy-gate denial caused by a gate exception.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="boundedContext">Manifest bounded context to hash before logging.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void PolicyGateFailedClosed(
        ILogger? logger,
        string boundedContext,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string boundedContextDigest = SanitizeCategoryValue(boundedContext);
        LogPolicyGateFailedClosed(logger, boundedContextDigest, exceptionType);
    }

    /// <summary>
    /// Logs a bounded schema-category command invocation failure.
    /// </summary>
    /// <param name="logger">Logger that receives the information event.</param>
    /// <param name="category">Bounded MCP failure category.</param>
    public static void CommandInvocationSchemaFailed(
        ILogger? logger,
        FrontComposerMcpFailureCategory category) {
        if (logger is null || !logger.IsEnabled(LogLevel.Information)) {
            return;
        }

        // CA1873: the bounded-category projection is already deferred behind the IsEnabled guard
        // above; binding it to a local keeps that laziness explicit at the call site rather than
        // relying on the analyzer to model the early-return guard. No behavioural change.
        string boundedCategory = BoundedCategory(category);
        LogCommandInvocationSchemaFailed(logger, boundedCategory);
    }

    /// <summary>
    /// Logs a bounded known MCP command invocation failure.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="category">Bounded MCP failure category.</param>
    public static void CommandInvocationKnownFailure(
        ILogger? logger,
        FrontComposerMcpFailureCategory category) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: same local-binding pattern as CommandInvocationSchemaFailed.
        string boundedCategory = BoundedCategory(category);
        LogCommandInvocationKnownFailure(logger, boundedCategory);
    }

    /// <summary>
    /// Logs a sanitized unexpected downstream MCP command invocation failure.
    /// </summary>
    /// <param name="logger">Logger that receives the warning.</param>
    /// <param name="exceptionType">Exception type name without message or stack trace.</param>
    public static void CommandInvocationUnexpectedFailure(
        ILogger? logger,
        string exceptionType) {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        // CA1873: the projection below is already deferred behind the IsEnabled guard above;
        // binding it to a local keeps that laziness explicit at the call site rather than relying
        // on the analyzer to model the early-return guard. No behavioural change.
        string categoryName = FrontComposerMcpFailureCategory.DownstreamFailed.ToString();
        string boundedExceptionType = ExceptionTypeOrDefault(exceptionType);
        LogCommandInvocationUnexpectedFailure(logger, categoryName, boundedExceptionType);
    }

    /// <summary>
    /// Logs a bounded non-exact schema negotiation decision.
    /// </summary>
    /// <param name="logger">Logger that receives the information event.</param>
    /// <param name="category">Bounded agent category.</param>
    /// <param name="messageKey">Bounded localization message key.</param>
    /// <param name="docsCode">Bounded diagnostic documentation code.</param>
    /// <param name="decisionKind">Bounded schema decision kind.</param>
    public static void SchemaNegotiationDecision(
        ILogger? logger,
        string category,
        string messageKey,
        string docsCode,
        McpSchemaNegotiationResultKind decisionKind) {
        if (logger is null || !logger.IsEnabled(LogLevel.Information)) {
            return;
        }

        // CA1873: see CommandInvocationSchemaFailed — the bounded-token projections are already
        // deferred behind the IsEnabled guard above and are bound to locals so the laziness is
        // explicit. No behavioural change.
        string boundedCategory = BoundedSchemaToken(category);
        string boundedMessageKey = BoundedSchemaToken(messageKey);
        string boundedDocsCode = BoundedSchemaToken(docsCode);
        string boundedDecisionKind = Enum.IsDefined(decisionKind) ? decisionKind.ToString() : "Unknown";
        LogSchemaNegotiationDecision(
            logger,
            boundedCategory,
            boundedMessageKey,
            boundedDocsCode,
            boundedDecisionKind);
    }

    /// <summary>Logs bounded outcomes for command invocations and lifecycle reads.</summary>
    /// <param name="logger">Logger receiving the opt-in debug event.</param>
    /// <param name="operation">Fixed MCP operation name.</param>
    /// <param name="successOutcome">Fixed outcome name when the operation succeeded.</param>
    /// <param name="result">Result from the invocation or lifecycle reader.</param>
    /// <param name="startedAt">Monotonic timestamp captured before the operation.</param>
    internal static void OperationCompleted(
        ILogger? logger,
        string operation,
        string successOutcome,
        FrontComposerMcpResult result,
        long startedAt) {
        try {
            if (logger is null || !logger.IsEnabled(LogLevel.Debug)) {
                return;
            }

            string operationName = operation;
            string descriptorKind = "command";
            string outcome = result.IsError ? BoundedCategory(result.Category) : OperationOutcome(result.StructuredContent, successOutcome);
            string durationBucket = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds switch {
                < 10 => "lt_10ms",
                < 100 => "10_to_99ms",
                < 1000 => "100_to_999ms",
                _ => "gte_1000ms",
            };

            string correlationIdDigest = DigestHandle(ReadString(result.StructuredContent, "correlationId"));
            string messageIdDigest = DigestHandle(ReadString(result.StructuredContent, "messageId"));
            LogOperationCompleted(logger, operationName, descriptorKind, outcome, durationBucket, correlationIdDigest, messageIdDigest);
        }
        catch (Exception ex) when (IsNonFatalLoggingFailure(ex)) {
            // A debug sink failure must not replace an acknowledgement after dispatch.
        }
    }

    private static string OperationOutcome(JsonObject? payload, string fallback)
        => payload?["outcome"] is JsonObject outcome && ReadString(outcome, "category") is
            "confirmed" or "rejected" or "idempotent_confirmed" or "timed_out" or "needs_review" or "warning" or "degraded_exhausted"
                ? ReadString(outcome, "category")!
                : fallback;

    private static string? ReadString(JsonObject? payload, string property)
        => payload?[property] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    private static bool IsNonFatalLoggingFailure(Exception exception) {
        while (exception is not (OutOfMemoryException or StackOverflowException or AccessViolationException)) {
            if (exception is AggregateException aggregate) {
                return aggregate.Flatten().InnerExceptions.All(IsNonFatalLoggingFailure);
            }

            if (exception.InnerException is not { } inner) {
                return true;
            }

            exception = inner;
        }

        return false;
    }

    /// <summary>Groups repeated domain rejections within one process without logging domain text.</summary>
    /// <param name="logger">Logger receiving the opt-in trace event.</param>
    /// <param name="rejection">Domain rejection whose stable code or legacy reason is fingerprinted.</param>
    /// <param name="correlationId">Server-issued handle for this invocation, if allocated.</param>
    internal static void CommandRejectionObserved(ILogger logger, CommandRejectedException rejection, string? correlationId) {
        try {
            if (!logger.IsEnabled(LogLevel.Trace)) {
                return;
            }

            string reason = rejection.ErrorCode == CommandRejectionDetails.UnknownErrorCode
                ? rejection.Message
                : rejection.ErrorCode;
            string rejectionDigest = DigestRejection(reason);
            string correlationIdDigest = DigestHandle(correlationId);
            LogCommandRejectionObserved(logger, rejectionDigest, correlationIdDigest);
        }
        catch (Exception ex) when (IsNonFatalLoggingFailure(ex)) {
            // A trace sink failure must not replace the domain rejection response.
        }
    }

    private static string DigestRejection(string reason) {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 256) {
            return "unknown";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(reason);
        Span<byte> digest = stackalloc byte[32];
        try {
            _ = HMACSHA256.HashData(RejectionDigestKey.Value, bytes, digest);
            return "hmac-sha256:" + Convert.ToHexStringLower(digest[..8]);
        }
        finally {
            CryptographicOperations.ZeroMemory(bytes);
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    private static string DigestHandle(string? handle)
        => handle is not null && FrontComposerMcpUlid.IsCanonical(handle) ? SanitizeCategoryValue(handle) : "unknown";

    private static string BoundedCategory(FrontComposerMcpFailureCategory category)
        => Enum.IsDefined(category) ? category.ToString() : "Unknown";

    private static string BoundedSchemaToken(string value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64) {
            return "unknown";
        }

        return value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')
            ? value
            : "unknown";
    }

    private static string ExceptionTypeOrDefault(string exceptionType)
        => string.IsNullOrWhiteSpace(exceptionType) ? "Exception" : exceptionType;

    private static string SanitizeCategoryValue(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "unknown";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value.Trim());
        try {
            byte[] hash = SHA256.HashData(bytes);
            string suffix = Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
            CryptographicOperations.ZeroMemory(hash);
            return "sha256:" + suffix;
        }
        finally {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    [LoggerMessage(EventId = 8310, Level = LogLevel.Warning,
        Message = "MCP tools/list failed closed. Category={Category}. ExceptionType={ExceptionType}.")]
    private static partial void LogToolsListFailedClosed(ILogger logger, string category, string exceptionType);

    [LoggerMessage(EventId = 8311, Level = LogLevel.Warning,
        Message = "MCP lifecycle precheck failed closed. Category={Category}. ExceptionType={ExceptionType}.")]
    private static partial void LogLifecyclePrecheckFailedClosed(ILogger logger, string category, string exceptionType);

    [LoggerMessage(EventId = 8312, Level = LogLevel.Warning,
        Message = "MCP projection reader failed closed. Category={Category}. ExceptionType={ExceptionType}.")]
    private static partial void LogProjectionReaderFailedClosed(ILogger logger, string category, string exceptionType);

    [LoggerMessage(EventId = 8313, Level = LogLevel.Warning,
        Message = "MCP tenant gate failed closed for bounded context {BoundedContext}. ExceptionType={ExceptionType}.")]
    private static partial void LogTenantToolGateFailedClosed(ILogger logger, string boundedContext, string exceptionType);

    [LoggerMessage(EventId = 8314, Level = LogLevel.Warning,
        Message = "MCP policy gate failed closed for bounded context {BoundedContext}. ExceptionType={ExceptionType}.")]
    private static partial void LogPolicyGateFailedClosed(ILogger logger, string boundedContext, string exceptionType);

    [LoggerMessage(EventId = 8315, EventName = "McpCommandSchemaFailed", Level = LogLevel.Information,
        Message = "MCP command invocation failed with schema category {Category}.")]
    private static partial void LogCommandInvocationSchemaFailed(ILogger logger, string category);

    [LoggerMessage(EventId = 8316, EventName = "McpCommandKnownFailure", Level = LogLevel.Warning,
        Message = "MCP command invocation failed with category {Category}.")]
    private static partial void LogCommandInvocationKnownFailure(ILogger logger, string category);

    [LoggerMessage(EventId = 8317, EventName = "McpCommandDownstreamFailed", Level = LogLevel.Warning,
        Message = "MCP command invocation failed with category {Category}. ExceptionType={ExceptionType}.")]
    private static partial void LogCommandInvocationUnexpectedFailure(
        ILogger logger,
        string category,
        string exceptionType);

    [LoggerMessage(EventId = 8319, EventName = "McpOperationCompleted", Level = LogLevel.Debug,
        Message = "MCP operation {Operation} completed for {DescriptorKind} with {Outcome} in {DurationBucket}. CorrelationIdDigest={CorrelationIdDigest}. MessageIdDigest={MessageIdDigest}.")]
    private static partial void LogOperationCompleted(
        ILogger logger, string operation, string descriptorKind, string outcome, string durationBucket, string correlationIdDigest, string messageIdDigest);

    [LoggerMessage(EventId = 8320, EventName = "McpCommandRejectionObserved", Level = LogLevel.Trace,
        Message = "MCP domain rejection observed. RejectionDigest={RejectionDigest}. CorrelationIdDigest={CorrelationIdDigest}.")]
    private static partial void LogCommandRejectionObserved(ILogger logger, string rejectionDigest, string correlationIdDigest);

    [LoggerMessage(EventId = 8318, EventName = "McpSchemaDecision", Level = LogLevel.Information,
        Message = "MCP schema negotiation decision {Category} {MessageKey} {DocsCode} {DecisionKind}.")]
    private static partial void LogSchemaNegotiationDecision(
        ILogger logger,
        string category,
        string messageKey,
        string docsCode,
        string decisionKind);
}
