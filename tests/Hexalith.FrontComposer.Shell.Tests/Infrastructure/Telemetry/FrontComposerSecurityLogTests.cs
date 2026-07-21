using System.Security.Cryptography;
using System.Text;

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

    [Fact]
    public void MultiFieldSecurityEvents_DigestEachIdentifierIntoItsOwnField()
    {
        CapturingLogger<FrontComposerSecurityLogTests> logger = new();

        FrontComposerSecurityLog.AuthorizationAuthenticationStateFailed(
            logger, "cmd.AuthState", "policy.AuthState", "corr.AuthState", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationTenantContextFailed(
            logger, CommandAuthorizationReason.StaleTenantContext, "cmd.TenantCtx", "policy.TenantCtx", "corr.TenantCtx", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationNullResult(logger, "cmd.NullResult", "policy.NullResult", "corr.NullResult");
        FrontComposerSecurityLog.AuthorizationMissingPolicyFailed(
            logger, "cmd.MissingPolicy", "policy.MissingPolicy", "corr.MissingPolicy", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationHandlerFailed(
            logger, "cmd.HandlerFailed", "policy.HandlerFailed", "corr.HandlerFailed", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.AuthorizationBlocked(
            logger, CommandAuthorizationReason.Denied, "cmd.Blocked", "policy.Blocked", "corr.Blocked");
        FrontComposerSecurityLog.DispatchEvaluationFailed(
            logger, "cmd.EvalFailed", "policy.EvalFailed", typeof(InvalidOperationException).FullName!);
        FrontComposerSecurityLog.DispatchNullDecision(logger, "cmd.NullDecision", "policy.NullDecision");
        FrontComposerSecurityLog.DispatchPending(logger, "cmd.Pending", "policy.Pending", "corr.Pending");
        FrontComposerSecurityLog.DispatchCanceled(logger, "cmd.Canceled", "policy.Canceled", "corr.Canceled");
        FrontComposerSecurityLog.DispatchBlocked(
            logger, CommandAuthorizationReason.Denied, "cmd.DispatchBlocked", "policy.DispatchBlocked", "corr.DispatchBlocked");
        FrontComposerSecurityLog.EmptyStateExplicitCommandMissing(logger, "proj.CommandMissing", "cmd.CommandMissing");
        FrontComposerSecurityLog.EmptyStateExplicitCommandAmbiguous(
            logger, "proj.Ambiguous", "cmd.Ambiguous", 3, "bc.Ambiguous", "selcmd.Ambiguous");
        FrontComposerSecurityLog.EmptyStateBoundedContextMissing(logger, "proj.BcMissing", "bc.BcMissing");
        FrontComposerSecurityLog.EmptyStateUnsafeRoute(logger, "route.Unsafe", "cmd.Unsafe");
        FrontComposerSecurityLog.StorageAccessorFailed(
            logger, "feature.AccessorFailed", "hydrate", typeof(InvalidOperationException).FullName!);

        CapturedLogEntry[] entries = [.. logger.Entries];
        entries.Length.ShouldBe(16);

        entries[0].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.AuthState"));
        entries[0].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.AuthState"));
        entries[0].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.AuthState"));

        entries[1].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.TenantCtx"));
        entries[1].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.TenantCtx"));
        entries[1].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.TenantCtx"));

        entries[2].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.NullResult"));
        entries[2].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.NullResult"));
        entries[2].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.NullResult"));

        entries[3].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.MissingPolicy"));
        entries[3].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.MissingPolicy"));
        entries[3].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.MissingPolicy"));

        entries[4].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.HandlerFailed"));
        entries[4].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.HandlerFailed"));
        entries[4].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.HandlerFailed"));

        entries[5].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.Blocked"));
        entries[5].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.Blocked"));
        entries[5].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.Blocked"));

        entries[6].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.EvalFailed"));
        entries[6].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.EvalFailed"));

        entries[7].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.NullDecision"));
        entries[7].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.NullDecision"));

        entries[8].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.Pending"));
        entries[8].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.Pending"));
        entries[8].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.Pending"));

        entries[9].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.Canceled"));
        entries[9].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.Canceled"));
        entries[9].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.Canceled"));

        entries[10].State["CommandTypeDigest"].ShouldBe(ComputeExpectedDigest("cmd.DispatchBlocked"));
        entries[10].State["PolicyDigest"].ShouldBe(ComputeExpectedDigest("policy.DispatchBlocked"));
        entries[10].State["CorrelationDigest"].ShouldBe(ComputeExpectedDigest("corr.DispatchBlocked"));

        entries[11].State["ProjectionTypeDigest"].ShouldBe(ComputeExpectedDigest("proj.CommandMissing"));
        entries[11].State["CommandDigest"].ShouldBe(ComputeExpectedDigest("cmd.CommandMissing"));

        entries[12].State["ProjectionTypeDigest"].ShouldBe(ComputeExpectedDigest("proj.Ambiguous"));
        entries[12].State["CommandDigest"].ShouldBe(ComputeExpectedDigest("cmd.Ambiguous"));
        entries[12].State["MatchCount"].ShouldBe(3);
        entries[12].State["SelectedBoundedContextDigest"].ShouldBe(ComputeExpectedDigest("bc.Ambiguous"));
        entries[12].State["SelectedCommandDigest"].ShouldBe(ComputeExpectedDigest("selcmd.Ambiguous"));

        entries[13].State["ProjectionTypeDigest"].ShouldBe(ComputeExpectedDigest("proj.BcMissing"));
        entries[13].State["BoundedContextDigest"].ShouldBe(ComputeExpectedDigest("bc.BcMissing"));

        entries[14].State["RouteDigest"].ShouldBe(ComputeExpectedDigest("route.Unsafe"));
        entries[14].State["CommandDigest"].ShouldBe(ComputeExpectedDigest("cmd.Unsafe"));

        entries[15].State["FeatureDigest"].ShouldBe(ComputeExpectedDigest("feature.AccessorFailed"));
        entries[15].State["Direction"].ShouldBe("hydrate");

        entries.ShouldAllBe(static entry => entry.Exception == null);
    }

    private static string ComputeExpectedDigest(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return "sha256:" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
