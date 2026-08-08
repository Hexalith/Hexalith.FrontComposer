using System.Reflection;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Story 11.21 — seals the low-severity diagnostic family that replaced the 73-call direct-log
/// remainder Story 11.18 intentionally deferred.
/// </summary>
public sealed class FrontComposerDiagnosticLogTests
{
    private const string ViewKey = "acme:OrdersProjection";

    [Fact]
    public void AllLoggerMessages_PinAContiguousCollisionFreeBand()
    {
        LoggerMessageAttribute[] attributes = [.. typeof(FrontComposerDiagnosticLog)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(static method => method.GetCustomAttributes<LoggerMessageAttribute>())
            .OrderBy(static attribute => attribute.EventId)];

        attributes.Length.ShouldBe(73);
        attributes.Select(static attribute => attribute.EventId).ShouldBe(Enumerable.Range(6000, 73));
        attributes.Select(static attribute => attribute.EventName)
            .Distinct(StringComparer.Ordinal)
            .Count()
            .ShouldBe(73);
        attributes.ShouldAllBe(static attribute => !string.IsNullOrWhiteSpace(attribute.EventName));
        attributes.ShouldAllBe(static attribute =>
            attribute.Level == LogLevel.Trace
            || attribute.Level == LogLevel.Debug
            || attribute.Level == LogLevel.Information);
        attributes.Count(static attribute => attribute.Level == LogLevel.Information).ShouldBe(56);
        attributes.Count(static attribute => attribute.Level == LogLevel.Debug).ShouldBe(17);
    }

    [Fact]
    public void MigratedEvents_PreserveLevelTemplateAndPayloadOfTheDirectCallTheyReplaced()
    {
        CapturingLogger<FrontComposerDiagnosticLogTests> logger = new();
        InvalidOperationException exception = new("storage blew up");

        FrontComposerDiagnosticLog.DataGridPerKeyHydrateEmpty(
            logger,
            FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
            "Empty",
            ViewKey);
        FrontComposerDiagnosticLog.DataGridPersistFailed(
            logger,
            exception,
            FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
            "persist",
            ViewKey);
        FrontComposerDiagnosticLog.DataGridClearCancelled(logger);
        FrontComposerDiagnosticLog.PaletteActivationRejectedByRouteFilter(
            logger,
            FcDiagnosticIds.HFC2111_PaletteHydrationEmpty,
            PaletteResultCategory.Projection,
            "Tampered");

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe([6051, 6035, 6036, 6028]);
        logger.Entries.Select(static entry => entry.Level).ShouldBe([
            LogLevel.Information,
            LogLevel.Information,
            LogLevel.Debug,
            LogLevel.Information,
        ]);

        // Bounded values flow through verbatim: the migration must not change what an operator reads.
        logger.Entries[0].State["ViewKey"].ShouldBe(ViewKey);
        logger.Entries[0].State["Reason"].ShouldBe("Empty");
        logger.Entries[0].State["DiagnosticId"].ShouldBe(FcDiagnosticIds.HFC2114_DataGridHydrationEmpty);
        logger.Entries[0].Message.ShouldContain(ViewKey);

        // Exception attachment is preserved exactly where the direct call attached one.
        logger.Entries[1].Exception.ShouldBeSameAs(exception);
        logger.Entries[1].State["Direction"].ShouldBe("persist");
        logger.Entries[2].Exception.ShouldBeNull();
        logger.Entries[3].State["Category"].ShouldBe(PaletteResultCategory.Projection);
    }

    [Fact]
    public void NullValues_RenderExactlyAsTheDirectCallDid()
    {
        CapturingLogger<FrontComposerDiagnosticLogTests> logger = new();

        FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(logger, null);

        logger.Entries.ShouldHaveSingleItem().State["CommandTypeName"].ShouldBeNull();
        logger.Entries[0].Message.ShouldContain("(null)");
    }

    [Fact]
    public void OversizedAndControlCharacterValues_CollapseToABoundedDigest()
    {
        CapturingLogger<FrontComposerDiagnosticLogTests> logger = new();
        string oversized = new('k', 513);
        const string ControlCharacterPayload = "orders\r\ninjected-log-line";

        FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(logger, oversized);
        FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(logger, ControlCharacterPayload);

        logger.Entries.Count.ShouldBe(2);
        foreach (CapturedLogEntry entry in logger.Entries)
        {
            string rendered = entry.State["CommandTypeName"].ShouldBeOfType<string>();
            rendered.ShouldStartWith("sha256:");
            rendered.Length.ShouldBeLessThan(64);
        }

        logger.Entries[0].Message.ShouldNotContain(oversized);
        logger.Entries[1].Message.ShouldNotContain(ControlCharacterPayload);
        logger.Entries[1].Message.ShouldNotContain("injected-log-line");
    }

    [Theory]
    [InlineData("\u2028")] // LINE SEPARATOR — ends a line in JSON/JavaScript-based sinks.
    [InlineData("\u2029")] // PARAGRAPH SEPARATOR — same.
    [InlineData("\u202E")] // RIGHT-TO-LEFT OVERRIDE — reorders the rendered line.
    [InlineData("\u200E")] // LEFT-TO-RIGHT MARK — invisible.
    [InlineData("\u00AD")] // SOFT HYPHEN — invisible.
    public void LineForgingCharactersOutsideTheControlRange_AlsoCollapseToADigest(string injected)
    {
        // char.IsControl misses U+2028/U+2029 and every Unicode format character, so an
        // adopter-supplied value could otherwise still forge or hide a log line.
        CapturingLogger<FrontComposerDiagnosticLogTests> logger = new();
        string payload = "orders" + injected + "injected-log-line";

        FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(logger, payload);

        string rendered = logger.Entries.ShouldHaveSingleItem().State["CommandTypeName"].ShouldBeOfType<string>();
        rendered.ShouldStartWith("sha256:");
        logger.Entries[0].Message.ShouldNotContain("injected-log-line");
    }

    [Fact]
    public void BoundedValueAtTheLimit_IsStillLoggedVerbatim()
    {
        CapturingLogger<FrontComposerDiagnosticLogTests> logger = new();
        string atLimit = new('k', 512);

        FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(logger, atLimit);

        logger.Entries.ShouldHaveSingleItem().State["CommandTypeName"].ShouldBe(atLimit);
    }

    [Fact]
    public void NullLogger_IsAcceptedByEveryWrapper()
    {
        Should.NotThrow(() => FrontComposerDiagnosticLog.ClipboardCopyTimedOut(null));
        Should.NotThrow(() => FrontComposerDiagnosticLog.PaletteCommandTypeUnresolved(null, ViewKey));
    }

    [Fact]
    public void DisabledLogger_EmitsNothing()
    {
        DisabledLogger logger = new();

        // DisabledLogger.Log throws, so reaching the sink at all fails the test.
        Should.NotThrow(() => FrontComposerDiagnosticLog.ProjectionTemplateContractVersionDrift(
            logger,
            FcDiagnosticIds.HFC1036_ProjectionTemplateContractVersionDrift,
            "Acme.Orders",
            null,
            1,
            0,
            0,
            1,
            1,
            0));
        Should.NotThrow(() => FrontComposerDiagnosticLog.ClipboardCopyFailed(
            logger,
            new InvalidOperationException("never rendered")));
        Should.NotThrow(() => FrontComposerDiagnosticLog.DataGridPerKeyHydrateEmpty(
            logger,
            FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
            "Empty",
            ViewKey));
    }

    /// <summary>
    /// Advisory allocation budget for the disabled-path measurement below: 40,000 wrapper
    /// invocations (10,000 iterations x 4 wrappers). A single per-call allocation of the smallest
    /// possible reference object would already cost ~960 KB here, so the budget still proves the
    /// disabled path allocates nothing per call while tolerating the JIT/runtime bookkeeping that
    /// an exact-zero assertion charges to this thread on a shared CI machine.
    /// </summary>
    private const long DisabledPathAllocationBudgetBytes = 4096L;

    [Fact]
    [Trait("Category", "Performance")]
    public void DisabledLowSeverityEvents_AfterWarmup_StayWithinTheAllocationBudget()
    {
        // Category=Performance — this measurement is timing/host sensitive, so it belongs to the
        // advisory lane rather than the blocking default lane (quality.yml Gate 3c).
        DisabledLogger logger = new();
        InvalidOperationException exception = new("must remain unevaluated");
        string oversized = new('k', 4097);

        for (int index = 0; index < 100; index++)
        {
            InvokeDisabledWrappers(logger, exception, oversized);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            InvokeDisabledWrappers(logger, exception, oversized);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.ShouldBeLessThanOrEqualTo(
            DisabledPathAllocationBudgetBytes,
            $"The disabled low-severity path allocated {allocated} bytes over 40,000 wrapper calls; "
            + "the guard evaluates neither the bounded-value digest nor the message template when the "
            + "level is disabled, so per-call allocation must stay at zero.");
    }

    private static void InvokeDisabledWrappers(DisabledLogger logger, Exception exception, string oversized)
    {
        FrontComposerDiagnosticLog.DataGridPerKeyHydrateEmpty(
            logger,
            FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
            "Empty",
            oversized);
        FrontComposerDiagnosticLog.DataGridPersistFailed(
            logger,
            exception,
            FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
            "persist",
            oversized);
        FrontComposerDiagnosticLog.ClipboardCopyFailed(logger, exception);
        FrontComposerDiagnosticLog.ThemeHydrationCancelled(logger);
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

}
