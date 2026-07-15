using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

public sealed class FrontComposerSecurityLogTests
{
    [Fact]
    public void AllSecurityEvents_UsePinnedUniqueContractsAndNeverCaptureExceptions()
    {
        const string IdentifierSentinel = "jwt.payload.signature";
        CapturingLogger<FrontComposerSecurityLogTests> logger = new();

        FrontComposerSecurityLog.ComponentAuthorizationEvaluationFailed(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthenticationBridgeTokenProviderReplaced(logger);
        FrontComposerSecurityLog.RequestClaimExtractionFailed(logger, "ConflictingAliases", 2, 1);
        FrontComposerSecurityLog.CircuitClaimExtractionFailed(logger, "MissingClaim", 1, 1);
        FrontComposerSecurityLog.GitHubTokenExchangeRequired(logger);
        FrontComposerSecurityLog.AccessTokenMissing(logger, "CustomBrokered");
        FrontComposerSecurityLog.AccessTokenAcquisitionFailed(logger, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationAuthenticationStateFailed(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationTenantContextFailed(logger, CommandAuthorizationReason.StaleTenantContext, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationNullResult(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.AuthorizationMissingPolicyFailed(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationHandlerFailed(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationBlocked(logger, CommandAuthorizationReason.Denied, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchDeclaredAndRuntimeCommandUnnamed(logger, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchResolvedCommandUnnamed(logger, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchServiceResolutionFailed(logger, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.DispatchEvaluatorMissing(logger, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchEvaluationFailed(logger, IdentifierSentinel, IdentifierSentinel, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.DispatchNullDecision(logger, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchPending(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchCanceled(logger, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.DispatchBlocked(logger, CommandAuthorizationReason.Denied, IdentifierSentinel, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.AuthorizationPolicyCatalogEmpty(logger, 2);
        FrontComposerSecurityLog.AuthorizationPolicyCatalogMissing(logger, 1);
        FrontComposerSecurityLog.LastUsedScopeMissing(logger);
        FrontComposerSecurityLog.EmptyStateRegistryFailed(logger, typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.EmptyStateExplicitCommandMissing(logger, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.EmptyStateExplicitCommandAmbiguous(logger, IdentifierSentinel, IdentifierSentinel, 2, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.EmptyStateBoundedContextMissing(logger, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.EmptyStateUnsafeRoute(logger, IdentifierSentinel, IdentifierSentinel);
        FrontComposerSecurityLog.StorageAccessorFailed(logger, IdentifierSentinel, "persist", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.StorageScopeMissing(logger, IdentifierSentinel, "hydrate");

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe(Enumerable.Range(5660, 32));
        logger.Entries.Select(static entry => entry.EventId.Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(32);
        logger.Entries.Select(static entry => entry.Level).ShouldBe([
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Debug,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Information,
            LogLevel.Information,
        ]);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(IdentifierSentinel, StringComparison.Ordinal));
    }

    [Fact]
    public void SecurityEvents_AdversarialIdentifiers_EmitStablePseudonymsAndNoException()
    {
        const string Command = " Acme.Secret.RotateTokenCommand ";
        const string Policy = "billing.secret.policy";
        const string Correlation = "jwt.payload.signature";
        CapturingLogger<FrontComposerSecurityLogTests> logger = new();

        FrontComposerSecurityLog.ComponentAuthorizationEvaluationFailed(
            logger,
            Command,
            Policy,
            Correlation,
            typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AccessTokenAcquisitionFailed(
            logger,
            typeof(AuthenticationFailureException).FullName!);
        FrontComposerSecurityLog.StorageScopeMissing(logger, "Density", "persist");

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe([5660, 5666, 5691]);
        logger.Entries.Select(static entry => entry.EventId.Name).ShouldBe([
            "ComponentAuthorizationEvaluationFailed",
            "AccessTokenAcquisitionFailed",
            "StorageScopeMissing",
        ]);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(Command.Trim(), StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(Policy, StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(Correlation, StringComparison.Ordinal));
        logger.Entries[0].State["CommandTypeDigest"].ShouldBe("sha256:145c2762f0cda437");
        logger.Entries[0].State["PolicyDigest"].ShouldBe("sha256:a29d55f4de76a301");
        logger.Entries[0].State["CorrelationDigest"].ShouldBe("sha256:3967ac9c7115778e");
        logger.Entries[2].State["FeatureDigest"].ShouldBe("sha256:77a283d69258c2ae");
        logger.Entries[2].State["Direction"].ShouldBe("persist");
    }

    [Fact]
    public void AccessTokenMissing_ProviderKind_IsBounded()
    {
        CapturingLogger<FrontComposerSecurityLogTests> logger = new();

        FrontComposerSecurityLog.AccessTokenMissing(logger, "jwt.payload.signature");

        CapturedLogEntry entry = logger.Entries.Single();
        entry.EventId.Id.ShouldBe(5665);
        entry.State["ProviderKind"].ShouldBe("Unknown");
    }
}
