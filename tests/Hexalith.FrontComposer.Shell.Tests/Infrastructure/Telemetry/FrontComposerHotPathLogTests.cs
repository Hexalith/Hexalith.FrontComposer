using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

public sealed class FrontComposerHotPathLogTests
{
    [Fact]
    public void RepresentativeEvents_UsePinnedContractsAndSupportSafePayloads()
    {
        const string SensitiveIdentifier = "tenant/user/jwt.payload.signature";
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
        logger.Entries[0].State["Cid"].ShouldBe("sha256:c5392b3771f73573");
        logger.Entries[2].State["FailureCategory"].ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void DisabledIdentifierEvent_AfterWarmup_AllocatesNothing()
    {
        DisabledLogger logger = new();
        const string SensitiveIdentifier = "tenant/user/jwt.payload.signature";
        InvalidOperationException exception = new("must remain unevaluated");

        for (int index = 0; index < 100; index++)
        {
            FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(logger, "HFC2100", SensitiveIdentifier);
            FrontComposerHotPathLog.ETagStorageReadFailed(logger, exception, SensitiveIdentifier);
            FrontComposerHotPathLog.PendingRegistrationMessageRejected(logger, SensitiveIdentifier);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(logger, "HFC2100", SensitiveIdentifier);
            FrontComposerHotPathLog.ETagStorageReadFailed(logger, exception, SensitiveIdentifier);
            FrontComposerHotPathLog.PendingRegistrationMessageRejected(logger, SensitiveIdentifier);
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
