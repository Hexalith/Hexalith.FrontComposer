using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>
/// Source-generated, support-safe log helpers for Shell fail-closed security branches.
/// </summary>
internal static partial class FrontComposerSecurityLog
{
    /// <summary>Logs a component authorization evaluation failure without raw identifiers.</summary>
    public static void ComponentAuthorizationEvaluationFailed(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogComponentAuthorizationEvaluationFailed(
            logger,
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId),
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs replacement of an adopter-provided EventStore token provider.</summary>
    public static void AuthenticationBridgeTokenProviderReplaced(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogAuthenticationBridgeTokenProviderReplaced(
            logger,
            FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
    }

    /// <summary>Logs request-principal claim extraction failure using bounded reason and counts.</summary>
    public static void RequestClaimExtractionFailed(
        ILogger? logger,
        string reason,
        int tenantAliasCount,
        int userAliasCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogRequestClaimExtractionFailed(
            logger,
            FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed,
            BoundedClaimReason(reason),
            tenantAliasCount,
            userAliasCount);
    }

    /// <summary>Logs circuit-principal claim extraction failure using bounded reason and counts.</summary>
    public static void CircuitClaimExtractionFailed(
        ILogger? logger,
        string reason,
        int tenantAliasCount,
        int userAliasCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogCircuitClaimExtractionFailed(
            logger,
            FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed,
            BoundedClaimReason(reason),
            tenantAliasCount,
            userAliasCount);
    }

    /// <summary>Logs the fail-closed GitHub token-exchange requirement.</summary>
    public static void GitHubTokenExchangeRequired(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogGitHubTokenExchangeRequired(logger, FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
    }

    /// <summary>Logs a missing access token with an allowlisted provider category.</summary>
    public static void AccessTokenMissing(
        ILogger? logger,
        string providerKind)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAccessTokenMissing(
            logger,
            FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
            BoundedProviderKind(providerKind));
    }

    /// <summary>Logs access-token acquisition failure by exception type only.</summary>
    public static void AccessTokenAcquisitionFailed(ILogger? logger, string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAccessTokenAcquisitionFailed(
            logger,
            FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs authentication-state resolution failure without raw authorization identifiers.</summary>
    public static void AuthorizationAuthenticationStateFailed(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
        => LogAuthorizationFailure(
            logger,
            LogLevel.Warning,
            LogAuthorizationAuthenticationStateFailed,
            commandType,
            policy,
            correlationId,
            exceptionType);

    /// <summary>Logs tenant-context resolution failure without raw authorization identifiers.</summary>
    public static void AuthorizationTenantContextFailed(
        ILogger? logger,
        CommandAuthorizationReason reason,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAuthorizationTenantContextFailed(
            logger,
            BoundedReason(reason),
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId),
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs a null authorization result without raw authorization identifiers.</summary>
    public static void AuthorizationNullResult(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAuthorizationNullResult(
            logger,
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId));
    }

    /// <summary>Logs missing-policy exception failure without raw authorization identifiers.</summary>
    public static void AuthorizationMissingPolicyFailed(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
        => LogAuthorizationFailure(
            logger,
            LogLevel.Warning,
            LogAuthorizationMissingPolicyFailed,
            commandType,
            policy,
            correlationId,
            exceptionType);

    /// <summary>Logs authorization handler failure without raw authorization identifiers.</summary>
    public static void AuthorizationHandlerFailed(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
        => LogAuthorizationFailure(
            logger,
            LogLevel.Warning,
            LogAuthorizationHandlerFailed,
            commandType,
            policy,
            correlationId,
            exceptionType);

    /// <summary>Logs a bounded authorization-block reason without raw authorization identifiers.</summary>
    public static void AuthorizationBlocked(
        ILogger? logger,
        CommandAuthorizationReason reason,
        string commandType,
        string policy,
        string correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAuthorizationBlocked(
            logger,
            BoundedReason(reason),
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId));
    }

    /// <summary>Logs denial of a command whose declared and runtime types are unnamed.</summary>
    public static void DispatchDeclaredAndRuntimeCommandUnnamed(ILogger? logger, string commandShortName)
        => LogSingleIdentifier(
            logger,
            LogLevel.Warning,
            LogDispatchDeclaredAndRuntimeCommandUnnamed,
            commandShortName);

    /// <summary>Logs denial of a resolved command whose type is unnamed.</summary>
    public static void DispatchResolvedCommandUnnamed(ILogger? logger, string commandShortName)
        => LogSingleIdentifier(
            logger,
            LogLevel.Warning,
            LogDispatchResolvedCommandUnnamed,
            commandShortName);

    /// <summary>Logs authorization-service resolution failure by exception type only.</summary>
    public static void DispatchServiceResolutionFailed(
        ILogger? logger,
        string commandType,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogDispatchServiceResolutionFailed(
            logger,
            SanitizeIdentifier(commandType),
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs a missing command authorization evaluator.</summary>
    public static void DispatchEvaluatorMissing(ILogger? logger, string commandType)
        => LogSingleIdentifier(logger, LogLevel.Warning, LogDispatchEvaluatorMissing, commandType);

    /// <summary>Logs dispatch-side authorization evaluation failure by exception type only.</summary>
    public static void DispatchEvaluationFailed(
        ILogger? logger,
        string commandType,
        string policy,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogDispatchEvaluationFailed(
            logger,
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs a null dispatch-side authorization decision.</summary>
    public static void DispatchNullDecision(ILogger? logger, string commandType, string policy)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogDispatchNullDecision(logger, SanitizeIdentifier(commandType), SanitizeIdentifier(policy));
    }

    /// <summary>Logs a pending dispatch-side authorization decision.</summary>
    public static void DispatchPending(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId)
        => LogDispatchDecision(
            logger,
            LogLevel.Information,
            LogDispatchPending,
            commandType,
            policy,
            correlationId);

    /// <summary>Logs a canceled dispatch-side authorization decision.</summary>
    public static void DispatchCanceled(
        ILogger? logger,
        string commandType,
        string policy,
        string correlationId)
        => LogDispatchDecision(
            logger,
            LogLevel.Debug,
            LogDispatchCanceled,
            commandType,
            policy,
            correlationId);

    /// <summary>Logs a blocked dispatch-side authorization decision.</summary>
    public static void DispatchBlocked(
        ILogger? logger,
        CommandAuthorizationReason reason,
        string commandType,
        string policy,
        string correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogDispatchBlocked(
            logger,
            BoundedReason(reason),
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId));
    }

    /// <summary>Logs an empty authorization-policy catalog using only a count.</summary>
    public static void AuthorizationPolicyCatalogEmpty(ILogger? logger, int declaredPolicyCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAuthorizationPolicyCatalogEmpty(logger, declaredPolicyCount);
    }

    /// <summary>Logs missing authorization-policy catalog entries using only a count.</summary>
    public static void AuthorizationPolicyCatalogMissing(ILogger? logger, int missingPolicyCount)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogAuthorizationPolicyCatalogMissing(logger, missingPolicyCount);
    }

    /// <summary>Logs the one-time LastUsed fail-closed diagnostic.</summary>
    public static void LastUsedScopeMissing(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLastUsedScopeMissing(logger, "D31");
    }

    /// <summary>Logs a registry failure by exception type only.</summary>
    public static void EmptyStateRegistryFailed(ILogger? logger, string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogEmptyStateRegistryFailed(logger, ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs an explicit CTA miss using pseudonymized identifiers.</summary>
    public static void EmptyStateExplicitCommandMissing(
        ILogger? logger,
        string projectionType,
        string commandName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogEmptyStateExplicitCommandMissing(
            logger,
            SanitizeIdentifier(projectionType),
            SanitizeIdentifier(commandName));
    }

    /// <summary>Logs an ambiguous explicit CTA match using counts and pseudonymized identifiers.</summary>
    public static void EmptyStateExplicitCommandAmbiguous(
        ILogger? logger,
        string projectionType,
        string commandName,
        int matchCount,
        string selectedBoundedContext,
        string selectedCommand)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogEmptyStateExplicitCommandAmbiguous(
            logger,
            SanitizeIdentifier(projectionType),
            SanitizeIdentifier(commandName),
            matchCount,
            SanitizeIdentifier(selectedBoundedContext),
            SanitizeIdentifier(selectedCommand));
    }

    /// <summary>Logs an unmatched CTA bounded context using pseudonymized identifiers.</summary>
    public static void EmptyStateBoundedContextMissing(
        ILogger? logger,
        string projectionType,
        string boundedContext)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogEmptyStateBoundedContextMissing(
            logger,
            SanitizeIdentifier(projectionType),
            SanitizeIdentifier(boundedContext));
    }

    /// <summary>Logs CTA suppression for an unsafe internal route using pseudonymized identifiers.</summary>
    public static void EmptyStateUnsafeRoute(ILogger? logger, string route, string commandType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogEmptyStateUnsafeRoute(
            logger,
            SanitizeIdentifier(route),
            SanitizeIdentifier(commandType));
    }

    /// <summary>Logs storage-scope accessor failure by exception type and pseudonymized feature.</summary>
    public static void StorageAccessorFailed(
        ILogger? logger,
        string feature,
        string direction,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogStorageAccessorFailed(
            logger,
            FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
            SanitizeIdentifier(feature),
            BoundedDirection(direction),
            ExceptionTypeOrDefault(exceptionType));
    }

    /// <summary>Logs missing storage scope with pseudonymized feature attribution.</summary>
    public static void StorageScopeMissing(ILogger? logger, string feature, string direction)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogStorageScopeMissing(
            logger,
            FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
            SanitizeIdentifier(feature),
            BoundedDirection(direction));
    }

    private static void LogAuthorizationFailure(
        ILogger? logger,
        LogLevel level,
        Action<ILogger, string, string, string, string> write,
        string commandType,
        string policy,
        string correlationId,
        string exceptionType)
    {
        if (logger is null || !logger.IsEnabled(level))
        {
            return;
        }

        write(
            logger,
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId),
            ExceptionTypeOrDefault(exceptionType));
    }

    private static void LogDispatchDecision(
        ILogger? logger,
        LogLevel level,
        Action<ILogger, string, string, string> write,
        string commandType,
        string policy,
        string correlationId)
    {
        if (logger is null || !logger.IsEnabled(level))
        {
            return;
        }

        write(
            logger,
            SanitizeIdentifier(commandType),
            SanitizeIdentifier(policy),
            SanitizeIdentifier(correlationId));
    }

    private static void LogSingleIdentifier(
        ILogger? logger,
        LogLevel level,
        Action<ILogger, string> write,
        string identifier)
    {
        if (logger is null || !logger.IsEnabled(level))
        {
            return;
        }

        write(logger, SanitizeIdentifier(identifier));
    }

    private static string SanitizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value.Trim());
        byte[]? hash = null;
        try
        {
            hash = SHA256.HashData(bytes);
            return "sha256:" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        }
        finally
        {
            if (hash is not null)
            {
                CryptographicOperations.ZeroMemory(hash);
            }

            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static string BoundedClaimReason(string reason)
        => reason switch
        {
            "Unauthenticated" => "Unauthenticated",
            "MultiValuedClaim" => "MultiValuedClaim",
            "EmptyClaim" => "EmptyClaim",
            "ColonClaim" => "ColonClaim",
            "MissingClaim" => "MissingClaim",
            "ConflictingAliases" => "ConflictingAliases",
            "InvalidClaim" => "InvalidClaim",
            _ => "Unknown",
        };

    private static string BoundedProviderKind(string providerKind)
        => !string.IsNullOrWhiteSpace(providerKind)
            && providerKind.Length <= 32
            && providerKind.All(static character => char.IsAsciiLetterOrDigit(character))
                ? providerKind
                : "Unknown";

    private static string BoundedDirection(string direction)
        => direction switch
        {
            "hydrate" => "hydrate",
            "persist" => "persist",
            _ => "unknown",
        };

    private static string BoundedReason(CommandAuthorizationReason reason)
        => Enum.IsDefined(reason) ? reason.ToString() : "Unknown";

    private static string ExceptionTypeOrDefault(string? exceptionType)
        => string.IsNullOrWhiteSpace(exceptionType) ? "Exception" : exceptionType;

    [LoggerMessage(EventId = 5660, EventName = "ComponentAuthorizationEvaluationFailed", Level = LogLevel.Warning,
        Message = "Command region authorization evaluation failed closed. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogComponentAuthorizationEvaluationFailed(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5661, EventName = "AuthenticationBridgeTokenProviderReplaced", Level = LogLevel.Information,
        Message = "{DiagnosticId}: FrontComposer authentication bridge replaces a previously configured EventStore access-token provider.")]
    private static partial void LogAuthenticationBridgeTokenProviderReplaced(ILogger logger, string diagnosticId);

    [LoggerMessage(EventId = 5662, EventName = "RequestClaimExtractionFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Request auth claim extraction failed. Reason={Reason} TenantAliasCount={TenantAliasCount} UserAliasCount={UserAliasCount}.")]
    private static partial void LogRequestClaimExtractionFailed(
        ILogger logger,
        string diagnosticId,
        string reason,
        int tenantAliasCount,
        int userAliasCount);

    [LoggerMessage(EventId = 5663, EventName = "CircuitClaimExtractionFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Circuit auth claim extraction failed. Reason={Reason} TenantAliasCount={TenantAliasCount} UserAliasCount={UserAliasCount}.")]
    private static partial void LogCircuitClaimExtractionFailed(
        ILogger logger,
        string diagnosticId,
        string reason,
        int tenantAliasCount,
        int userAliasCount);

    [LoggerMessage(EventId = 5664, EventName = "GitHubTokenExchangeRequired", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: GitHub OAuth sign-in cannot be relayed as an EventStore bearer token without a broker.")]
    private static partial void LogGitHubTokenExchangeRequired(ILogger logger, string diagnosticId);

    [LoggerMessage(EventId = 5665, EventName = "AccessTokenMissing", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Access-token acquisition returned no token. ProviderKind={ProviderKind}.")]
    private static partial void LogAccessTokenMissing(ILogger logger, string diagnosticId, string providerKind);

    [LoggerMessage(EventId = 5666, EventName = "AccessTokenAcquisitionFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Access-token acquisition failed. ExceptionType={ExceptionType}.")]
    private static partial void LogAccessTokenAcquisitionFailed(
        ILogger logger,
        string diagnosticId,
        string exceptionType);

    [LoggerMessage(EventId = 5667, EventName = "AuthorizationAuthenticationStateFailed", Level = LogLevel.Warning,
        Message = "Command authorization failed closed resolving authentication state. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogAuthorizationAuthenticationStateFailed(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5668, EventName = "AuthorizationTenantContextFailed", Level = LogLevel.Warning,
        Message = "Command authorization failed closed resolving tenant context. Reason={Reason} CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogAuthorizationTenantContextFailed(
        ILogger logger,
        string reason,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5669, EventName = "AuthorizationNullResult", Level = LogLevel.Warning,
        Message = "Command authorization failed closed because the authorization service returned no result. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest}.")]
    private static partial void LogAuthorizationNullResult(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest);

    [LoggerMessage(EventId = 5670, EventName = "AuthorizationMissingPolicyFailed", Level = LogLevel.Warning,
        Message = "Command authorization failed closed. Reason=MissingPolicy CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogAuthorizationMissingPolicyFailed(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5671, EventName = "AuthorizationHandlerFailed", Level = LogLevel.Warning,
        Message = "Command authorization failed closed. Reason=HandlerFailed CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogAuthorizationHandlerFailed(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5672, EventName = "AuthorizationBlocked", Level = LogLevel.Warning,
        Message = "Command authorization blocked. Reason={Reason} CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest}.")]
    private static partial void LogAuthorizationBlocked(
        ILogger logger,
        string reason,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest);

    [LoggerMessage(EventId = 5673, EventName = "DispatchDeclaredAndRuntimeCommandUnnamed", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because declared and runtime command types have no fully qualified name. CommandTypeDigest={CommandTypeDigest}.")]
    private static partial void LogDispatchDeclaredAndRuntimeCommandUnnamed(ILogger logger, string commandTypeDigest);

    [LoggerMessage(EventId = 5674, EventName = "DispatchResolvedCommandUnnamed", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because the resolved command type has no fully qualified name. CommandTypeDigest={CommandTypeDigest}.")]
    private static partial void LogDispatchResolvedCommandUnnamed(ILogger logger, string commandTypeDigest);

    [LoggerMessage(EventId = 5675, EventName = "DispatchServiceResolutionFailed", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because authorization services could not be resolved. CommandTypeDigest={CommandTypeDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogDispatchServiceResolutionFailed(
        ILogger logger,
        string commandTypeDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5676, EventName = "DispatchEvaluatorMissing", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because the authorization evaluator is not registered. CommandTypeDigest={CommandTypeDigest}.")]
    private static partial void LogDispatchEvaluatorMissing(ILogger logger, string commandTypeDigest);

    [LoggerMessage(EventId = 5677, EventName = "DispatchEvaluationFailed", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because authorization evaluation threw. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogDispatchEvaluationFailed(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string exceptionType);

    [LoggerMessage(EventId = 5678, EventName = "DispatchNullDecision", Level = LogLevel.Warning,
        Message = "Direct command dispatch failed closed because the evaluator returned no decision. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest}.")]
    private static partial void LogDispatchNullDecision(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest);

    [LoggerMessage(EventId = 5679, EventName = "DispatchPending", Level = LogLevel.Information,
        Message = "Direct command dispatch deferred because authorization is pending. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest}.")]
    private static partial void LogDispatchPending(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest);

    [LoggerMessage(EventId = 5680, EventName = "DispatchCanceled", Level = LogLevel.Debug,
        Message = "Direct command dispatch canceled before authorization completed. CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest}.")]
    private static partial void LogDispatchCanceled(
        ILogger logger,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest);

    [LoggerMessage(EventId = 5681, EventName = "DispatchBlocked", Level = LogLevel.Warning,
        Message = "Direct command dispatch blocked by authorization. Reason={Reason} CommandTypeDigest={CommandTypeDigest} PolicyDigest={PolicyDigest} CorrelationDigest={CorrelationDigest}.")]
    private static partial void LogDispatchBlocked(
        ILogger logger,
        string reason,
        string commandTypeDigest,
        string policyDigest,
        string correlationDigest);

    [LoggerMessage(EventId = 5682, EventName = "AuthorizationPolicyCatalogEmpty", Level = LogLevel.Warning,
        Message = "FrontComposer command authorization policy catalog is empty while commands declare policies. DeclaredPolicyCount={DeclaredPolicyCount}.")]
    private static partial void LogAuthorizationPolicyCatalogEmpty(ILogger logger, int declaredPolicyCount);

    [LoggerMessage(EventId = 5683, EventName = "AuthorizationPolicyCatalogMissing", Level = LogLevel.Warning,
        Message = "FrontComposer command authorization policy catalog is missing generated command policies. MissingPolicyCount={MissingPolicyCount}.")]
    private static partial void LogAuthorizationPolicyCatalogMissing(ILogger logger, int missingPolicyCount);

    [LoggerMessage(EventId = 5684, EventName = "LastUsedScopeMissing", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: LastUsed persistence disabled because tenant or user context is missing.")]
    private static partial void LogLastUsedScopeMissing(ILogger logger, string diagnosticId);

    [LoggerMessage(EventId = 5685, EventName = "EmptyStateRegistryFailed", Level = LogLevel.Warning,
        Message = "Empty-state CTA resolution failed because the FrontComposer registry threw. ExceptionType={ExceptionType}.")]
    private static partial void LogEmptyStateRegistryFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5686, EventName = "EmptyStateExplicitCommandMissing", Level = LogLevel.Warning,
        Message = "Projection requested an empty-state CTA command, but no reachable writable command matched. ProjectionTypeDigest={ProjectionTypeDigest} CommandDigest={CommandDigest}.")]
    private static partial void LogEmptyStateExplicitCommandMissing(
        ILogger logger,
        string projectionTypeDigest,
        string commandDigest);

    [LoggerMessage(EventId = 5687, EventName = "EmptyStateExplicitCommandAmbiguous", Level = LogLevel.Warning,
        Message = "Projection empty-state CTA command matched multiple registered commands; the deterministic first match was selected. ProjectionTypeDigest={ProjectionTypeDigest} CommandDigest={CommandDigest} MatchCount={MatchCount} SelectedBoundedContextDigest={SelectedBoundedContextDigest} SelectedCommandDigest={SelectedCommandDigest}.")]
    private static partial void LogEmptyStateExplicitCommandAmbiguous(
        ILogger logger,
        string projectionTypeDigest,
        string commandDigest,
        int matchCount,
        string selectedBoundedContextDigest,
        string selectedCommandDigest);

    [LoggerMessage(EventId = 5688, EventName = "EmptyStateBoundedContextMissing", Level = LogLevel.Warning,
        Message = "Projection declares a bounded context with no matching manifest; CTA resolution falls back to projection ownership. ProjectionTypeDigest={ProjectionTypeDigest} BoundedContextDigest={BoundedContextDigest}.")]
    private static partial void LogEmptyStateBoundedContextMissing(
        ILogger logger,
        string projectionTypeDigest,
        string boundedContextDigest);

    [LoggerMessage(EventId = 5689, EventName = "EmptyStateUnsafeRoute", Level = LogLevel.Warning,
        Message = "Empty-state CTA route failed internal-route validation and was suppressed. RouteDigest={RouteDigest} CommandDigest={CommandDigest}.")]
    private static partial void LogEmptyStateUnsafeRoute(
        ILogger logger,
        string routeDigest,
        string commandDigest);

    [LoggerMessage(EventId = 5690, EventName = "StorageAccessorFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Storage scope resolution skipped because the user-context accessor threw. FeatureDigest={FeatureDigest} Direction={Direction} Reason=AccessorThrew ExceptionType={ExceptionType}.")]
    private static partial void LogStorageAccessorFailed(
        ILogger logger,
        string diagnosticId,
        string featureDigest,
        string direction,
        string exceptionType);

    [LoggerMessage(EventId = 5691, EventName = "StorageScopeMissing", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Storage scope resolution skipped because tenant or user context is unavailable. FeatureDigest={FeatureDigest} Direction={Direction}.")]
    private static partial void LogStorageScopeMissing(
        ILogger logger,
        string diagnosticId,
        string featureDigest,
        string direction);
}
