using System.Security.Cryptography;
using System.Text;

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
}
