using System.Reflection;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

public sealed class FrontComposerHotPathLogTests
{
    private const string SensitiveIdentifier = "tenant/user/jwt.payload.signature";
    private const string AllowlistedMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string AllowlistedViewKey = "acme:OrdersProjection";
    private const string SensitiveDigest = "sha256:c5392b3771f73573";
    private const string MessageIdDigest = "sha256:8eac53b3f14d71fe";
    private const string ViewKeyDigest = "sha256:99218267c1520f0b";

    private static readonly string[] ExpectedEventNames =
    [
        "LifecycleUnexpectedCorrelation",
        "LifecycleIdempotencyResolved",
        "LifecycleIdempotentInfoBarRendered",
        "LifecycleTimerPhaseMarshaled",
        "PendingStatusProtocolFailure",
        "QueryNotModifiedWithoutCacheRepeated",
        "QueryNotModifiedWithoutCacheRetry",
        "ProjectionSubscriptionGateTimeout",
        "ProjectionSubscriptionDisposalFailed",
        "ProjectionChangeSubscriberFailed",
        "ProjectionChangeDetailSubscriberFailed",
        "ReconciliationCoordinatorMissing",
        "ReconnectionReconciliationCallbackFailed",
        "HubClosedRestartGateUnavailable",
        "HubClosedRestartTimedOut",
        "HubClosedRestartFailed",
        "HubClosedRestartCanceledDuringDisposal",
        "HubClosedRestartTimedOutOrCanceled",
        "HubClosedRestartDisposedSource",
        "ProjectionReconnectRejoinGateUnavailable",
        "ProjectionDisposalOperationWaitTimedOut",
        "ProjectionDisposalOperationCanceledByTimeout",
        "ProjectionDisposalOperationFailed",
        "ProjectionDisposeBoundedTimedOut",
        "ProjectionDisposeBoundedFailed",
        "ProjectionHubStateSubscriberFailed",
        "PendingPollingTickFailed",
        "PendingPollingDisposeTimedOut",
        "PendingPollingDisposeFailed",
        "ProjectionFallbackPollingDisposeTimedOut",
        "ProjectionFallbackPollingDisposeFailed",
        "ProjectionFallbackPollingTerminated",
        "ReconciliationBudgetZero",
        "ReconciliationLaneMissingTenant",
        "ReconciliationLaneDegraded",
        "ProjectionRefreshNegativeCount",
        "LifecycleReplayCallbackFaulted",
        "LifecycleCrossCorrelationDuplicate",
        "LifecycleInvalidTransition",
        "LifecycleMissingSubmitted",
        "LifecycleSubscriberFaulted",
        "LifecycleMessageCacheEvicted",
        "ETagStorageReadFailed",
        "ETagIncompatibleEntry",
        "ETagRemoveIncompatibleFailed",
        "ETagStorageWriteFailed",
        "ETagStorageRemoveFailed",
        "ETagFamilyInvalidationSegmentInvalid",
        "ETagFamilyInvalidationCanonicalizationInvalid",
        "ETagFamilyInvalidationReadFailed",
        "ETagLruEnumerationFailed",
        "ETagLruTimestampSeedFailed",
        "ETagLruEvictionRemoveFailed",
        "NewItemStateCleared",
        "NewItemScopeTransition",
        "PendingOutcomeMissingIdentity",
        "PendingOutcomeFallbackIdentityIncomplete",
        "PendingOutcomeNoMatch",
        "PendingOutcomeAmbiguous",
        "NewItemMetadataIncomplete",
        "PendingPollDuplicateTerminal",
        "PendingPollNonResolved",
        "PendingRegistrationMessageRejected",
        "PendingRegistrationCorrelationRejected",
        "PendingRegistrationMetadataConflict",
        "PendingTerminalRejected",
        "PendingTerminalUnknown",
        "PendingStateCleared",
        "PendingEvictedUnresolved",
        "PendingEvictedDispatchSkipped",
        "PendingLifecycleDisposed",
        "PendingLifecycleDispatchFailed",
        "PendingScopeTransition",
        "ProjectionStateSubscriberFailed",
        "ReconciliationStateStartFailed",
        "ReconciliationFailed",
        "ReconciliationSweepMarkerFailed",
        "ReconciliationPendingResolutionFailed",
        "ReconciliationStateResetFailed",
        "ReconciliationSweepCleanupFailed",
        "ReconciliationStateSubscriberFailed",
        "PendingOutcomeBufferOverflow",
        "PendingOutcomeTimestampRejected",
        "PendingOutcomePublicationFailed",
    ];

    private static readonly LogLevel[] ExpectedLevels =
    [
        LogLevel.Warning, // 5700
        LogLevel.Information, // 5701
        LogLevel.Information, // 5702
        LogLevel.Debug, // 5703
        LogLevel.Warning, // 5704
        LogLevel.Warning, // 5705
        LogLevel.Information, // 5706
        LogLevel.Warning, // 5707
        LogLevel.Warning, // 5708
        LogLevel.Warning, // 5709
        LogLevel.Warning, // 5710
        LogLevel.Information, // 5711
        LogLevel.Warning, // 5712
        LogLevel.Warning, // 5713
        LogLevel.Warning, // 5714
        LogLevel.Warning, // 5715
        LogLevel.Warning, // 5716
        LogLevel.Warning, // 5717
        LogLevel.Warning, // 5718
        LogLevel.Warning, // 5719
        LogLevel.Warning, // 5720
        LogLevel.Warning, // 5721
        LogLevel.Warning, // 5722
        LogLevel.Warning, // 5723
        LogLevel.Warning, // 5724
        LogLevel.Warning, // 5725
        LogLevel.Warning, // 5726
        LogLevel.Warning, // 5727
        LogLevel.Warning, // 5728
        LogLevel.Warning, // 5729
        LogLevel.Warning, // 5730
        LogLevel.Warning, // 5731
        LogLevel.Warning, // 5732
        LogLevel.Information, // 5733
        LogLevel.Information, // 5734
        LogLevel.Warning, // 5735
        LogLevel.Error, // 5736
        LogLevel.Warning, // 5737
        LogLevel.Error, // 5738
        LogLevel.Warning, // 5739
        LogLevel.Error, // 5740
        LogLevel.Debug, // 5741
        LogLevel.Warning, // 5742
        LogLevel.Information, // 5743
        LogLevel.Warning, // 5744
        LogLevel.Warning, // 5745
        LogLevel.Warning, // 5746
        LogLevel.Warning, // 5747
        LogLevel.Warning, // 5748
        LogLevel.Warning, // 5749
        LogLevel.Warning, // 5750
        LogLevel.Warning, // 5751
        LogLevel.Warning, // 5752
        LogLevel.Information, // 5753
        LogLevel.Warning, // 5754
        LogLevel.Warning, // 5755
        LogLevel.Debug, // 5756
        LogLevel.Debug, // 5757
        LogLevel.Warning, // 5758
        LogLevel.Debug, // 5759
        LogLevel.Debug, // 5760
        LogLevel.Warning, // 5761
        LogLevel.Warning, // 5762
        LogLevel.Warning, // 5763
        LogLevel.Warning, // 5764
        LogLevel.Warning, // 5765
        LogLevel.Debug, // 5766
        LogLevel.Information, // 5767
        LogLevel.Warning, // 5768
        LogLevel.Debug, // 5769
        LogLevel.Debug, // 5770
        LogLevel.Warning, // 5771
        LogLevel.Warning, // 5772
        LogLevel.Warning, // 5773
        LogLevel.Warning, // 5774
        LogLevel.Warning, // 5775
        LogLevel.Warning, // 5776
        LogLevel.Warning, // 5777
        LogLevel.Warning, // 5778
        LogLevel.Warning, // 5779
        LogLevel.Warning, // 5780
        LogLevel.Warning, // 5781
        LogLevel.Warning, // 5782
        LogLevel.Warning, // 5783
    ];

    [Fact]
    public void AllLoggerMessages_PinEventIdsNamesAndLevels()
    {
        LoggerMessageAttribute[] attributes = [.. typeof(FrontComposerHotPathLog)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(static method => method.GetCustomAttributes<LoggerMessageAttribute>())
            .OrderBy(static attribute => attribute.EventId)];

        attributes.Length.ShouldBe(84);
        attributes.Select(static attribute => attribute.EventId).ShouldBe(Enumerable.Range(5700, 84));
        attributes.Select(static attribute => attribute.EventName).ShouldBe(ExpectedEventNames);
        attributes.Select(static attribute => attribute.Level).ShouldBe(ExpectedLevels);
    }

    [Fact]
    public void RepresentativeEvents_UsePinnedContractsAndSupportSafePayloads()
    {
        CapturingLogger<FrontComposerHotPathLogTests> logger = new();

        FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(logger, "HFC2100", SensitiveIdentifier);
        FrontComposerHotPathLog.LifecycleInvalidTransition(
            logger,
            SensitiveIdentifier,
            "Acknowledged",
            "Idle",
            SensitiveIdentifier);
        FrontComposerHotPathLog.ETagStorageReadFailed(
            logger,
            new InvalidOperationException("secret path /var/private/cache"),
            SensitiveIdentifier);
        FrontComposerHotPathLog.PendingOutcomeMissingIdentity(logger, "ProjectionPush", "Confirmed");
        FrontComposerHotPathLog.ReconciliationStateSubscriberFailed(logger, nameof(InvalidOperationException));

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe([5700, 5738, 5742, 5755, 5780]);
        logger.Entries.Select(static entry => entry.EventId.Name).ShouldBe([
            "LifecycleUnexpectedCorrelation",
            "LifecycleInvalidTransition",
            "ETagStorageReadFailed",
            "PendingOutcomeMissingIdentity",
            "ReconciliationStateSubscriberFailed",
        ]);
        logger.Entries.Select(static entry => entry.Level).ShouldBe([
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Warning,
            LogLevel.Warning,
        ]);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(SensitiveIdentifier, StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains("/var/private/cache", StringComparison.Ordinal));
        logger.Entries[0].State["Cid"].ShouldBe(SensitiveDigest);
        logger.Entries[2].State["FailureCategory"].ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void AllowlistedIdentifiers_AreAlwaysDigested()
    {
        CapturingLogger<FrontComposerHotPathLogTests> logger = new();

        FrontComposerHotPathLog.LifecycleMessageCacheEvicted(logger, AllowlistedMessageId);
        FrontComposerHotPathLog.PendingEvictedUnresolved(logger, AllowlistedMessageId);
        FrontComposerHotPathLog.ReconciliationLaneMissingTenant(logger, AllowlistedViewKey, "OrdersProjection");
        FrontComposerHotPathLog.PendingStatusProtocolFailure(logger, "ProtocolDrift", AllowlistedMessageId);

        FrontComposerHotPathLog.DigestIdentifier(AllowlistedMessageId).ShouldBe(MessageIdDigest);
        FrontComposerHotPathLog.DigestIdentifier(AllowlistedViewKey).ShouldBe(ViewKeyDigest);
        FrontComposerHotPathLog.DigestIdentifier(null).ShouldBe("absent");
        FrontComposerHotPathLog.DigestIdentifier("   ").ShouldBe("absent");

        logger.Entries[0].State["Evicted"].ShouldBe(MessageIdDigest);
        logger.Entries[1].State["MessageId"].ShouldBe(MessageIdDigest);
        logger.Entries[2].State["ViewKey"].ShouldBe(ViewKeyDigest);
        logger.Entries[2].State["ProjectionType"].ShouldBe(
            FrontComposerHotPathLog.DigestIdentifier("OrdersProjection"));
        logger.Entries[3].State["MessageId"].ShouldBe(MessageIdDigest);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(AllowlistedMessageId, StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(AllowlistedViewKey, StringComparison.Ordinal));
    }

    [Fact]
    public void NestedExceptionTypeNames_RemainReadableFailureCategories()
    {
        CapturingLogger<FrontComposerHotPathLogTests> logger = new();
        const string NestedTypeName = "Outer+NestedFaultException";

        FrontComposerHotPathLog.ETagLruTimestampSeedFailed(logger, AllowlistedMessageId, NestedTypeName);

        logger.Entries.ShouldHaveSingleItem().State["FailureCategory"].ShouldBe(NestedTypeName);
        logger.Entries[0].State["KeyHash"].ShouldBe(MessageIdDigest);
    }

    [Fact]
    public void DistinctDisposalEvents_UseDistinctMessageText()
    {
        CapturingLogger<FrontComposerHotPathLogTests> logger = new();

        FrontComposerHotPathLog.HubClosedRestartCanceledDuringDisposal(logger, "OperationCanceledException");
        FrontComposerHotPathLog.HubClosedRestartDisposedSource(logger, "ObjectDisposedException");
        FrontComposerHotPathLog.ProjectionDisposalOperationWaitTimedOut(logger, "Wait", "TimeoutException");
        FrontComposerHotPathLog.ProjectionDisposalOperationCanceledByTimeout(logger, "Wait", "OperationCanceledException");

        logger.Entries[0].Message.ShouldContain("canceled during disposal");
        logger.Entries[1].Message.ShouldContain("source was disposed");
        logger.Entries[1].Message.ShouldNotContain("canceled during disposal");
        logger.Entries[2].Message.ShouldContain("timed out");
        logger.Entries[3].Message.ShouldContain("canceled by timeout");
    }

    [Fact]
    public void DisabledIdentifierEvent_AfterWarmup_AllocatesNothing()
    {
        DisabledLogger logger = new();
        InvalidOperationException exception = new("must remain unevaluated");

        for (int index = 0; index < 100; index++)
        {
            InvokeDisabledDigestWrappers(logger, exception);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            InvokeDisabledDigestWrappers(logger, exception);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.ShouldBe(0L);
    }

    [Fact]
    public void DisabledCategoryEvent_DoesNotEvaluateToString()
    {
        DisabledLogger logger = new();
        ThrowingCategory category = new();

        Should.NotThrow(() => FrontComposerHotPathLog.PendingOutcomeMissingIdentity(logger, category, category));
    }

    private static void InvokeDisabledDigestWrappers(DisabledLogger logger, Exception exception)
    {
        FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(logger, "HFC2100", SensitiveIdentifier);
        FrontComposerHotPathLog.ETagStorageReadFailed(logger, exception, SensitiveIdentifier);
        FrontComposerHotPathLog.PendingRegistrationMessageRejected(logger, SensitiveIdentifier);
        FrontComposerHotPathLog.ReconciliationLaneMissingTenant(logger, AllowlistedViewKey, "OrdersProjection");
        FrontComposerHotPathLog.LifecycleMessageCacheEvicted(logger, AllowlistedMessageId);
        FrontComposerHotPathLog.PendingEvictedUnresolved(logger, AllowlistedMessageId);
        FrontComposerHotPathLog.PendingStatusProtocolFailure(logger, "ProtocolDrift", AllowlistedMessageId);
    }

    private sealed class DisabledLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => throw new InvalidOperationException("A disabled logger must not receive a log entry.");
    }

    private sealed class ThrowingCategory
    {
        public override string ToString()
            => throw new InvalidOperationException("ToString must be deferred until logging is enabled.");
    }
}
