using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Source-generated log helpers for MCP fail-closed branches.
/// </summary>
internal static partial class FrontComposerMcpLog {
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

        LogToolsListFailedClosed(logger, category.ToString(), exceptionType);
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

        LogLifecyclePrecheckFailedClosed(logger, category.ToString(), exceptionType);
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

        LogProjectionReaderFailedClosed(logger, category.ToString(), exceptionType);
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

        LogTenantToolGateFailedClosed(logger, SanitizeCategoryValue(boundedContext), exceptionType);
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

        LogPolicyGateFailedClosed(logger, SanitizeCategoryValue(boundedContext), exceptionType);
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

        LogCommandInvocationSchemaFailed(logger, BoundedCategory(category));
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

        LogCommandInvocationKnownFailure(logger, BoundedCategory(category));
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

        LogCommandInvocationUnexpectedFailure(
            logger,
            FrontComposerMcpFailureCategory.DownstreamFailed.ToString(),
            ExceptionTypeOrDefault(exceptionType));
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

        LogSchemaNegotiationDecision(
            logger,
            BoundedSchemaToken(category),
            BoundedSchemaToken(messageKey),
            BoundedSchemaToken(docsCode),
            Enum.IsDefined(decisionKind) ? decisionKind.ToString() : "Unknown");
    }

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

    [LoggerMessage(EventId = 8318, EventName = "McpSchemaDecision", Level = LogLevel.Information,
        Message = "MCP schema negotiation decision {Category} {MessageKey} {DocsCode} {DecisionKind}.")]
    private static partial void LogSchemaNegotiationDecision(
        ILogger logger,
        string category,
        string messageKey,
        string docsCode,
        string decisionKind);
}
