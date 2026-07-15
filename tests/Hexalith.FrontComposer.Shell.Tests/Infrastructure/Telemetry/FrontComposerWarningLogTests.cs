using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

public sealed class FrontComposerWarningLogTests
{
    private const string DiagnosticId = "HFC_TEST";
    private const string Sensitive = "jwt.payload.signature /var/private/secret";

    [Fact]
    public void AllEvents_UsePinnedContractsAndSupportSafePayloads()
    {
        CapturingLogger<FrontComposerWarningLogTests> logger = new();
        InvalidOperationException exception = new(Sensitive);

        FrontComposerWarningLog.BadgeCatalogEnumerationFailed(logger, DiagnosticId, exception);
        FrontComposerWarningLog.BadgeReaderFailed(logger, DiagnosticId, Sensitive, exception);
        FrontComposerWarningLog.BadgeNegativeCount(logger, DiagnosticId, -1, Sensitive);
        FrontComposerWarningLog.BadgeNotifierFailed(logger, DiagnosticId, Sensitive, exception);
        FrontComposerWarningLog.LayoutUnknownViewportTier(logger, 999);
        FrontComposerWarningLog.LayoutSubscribeFailed(logger, "Desktop", exception);
        FrontComposerWarningLog.FieldSlotMissingParameters(logger, DiagnosticId, Sensitive, Sensitive, true, false, true);
        FrontComposerWarningLog.FieldSlotTypeMismatch(logger, DiagnosticId, Sensitive, Sensitive, Sensitive, Sensitive);
        FrontComposerWarningLog.FieldSlotRenderFailed(logger, DiagnosticId, Sensitive, Sensitive, Sensitive, Sensitive, exception);
        FrontComposerWarningLog.ProjectionSubtitleSubscribeFailed(logger, Sensitive, exception);
        FrontComposerWarningLog.ProjectionSubtitleDisposeFailed(logger, exception);
        FrontComposerWarningLog.ProjectionTemplateRenderFailed(logger, DiagnosticId, Sensitive, Sensitive, Sensitive, exception);
        FrontComposerWarningLog.ProjectionViewOverrideRenderFailed(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            Sensitive,
            "RenderFault",
            Sensitive,
            Sensitive,
            3,
            true);
        FrontComposerWarningLog.BootstrapValidationFailed(logger, exception);
        FrontComposerWarningLog.ProblemDetailsContentLengthExceeded(logger, 65_536, Sensitive);
        FrontComposerWarningLog.ProblemDetailsReadExceeded(logger, 65_536, Sensitive);
        FrontComposerWarningLog.ProblemDetailsParseFailed(logger, Sensitive, "JsonException");
        FrontComposerWarningLog.LocalStorageDeserializeFailed(logger, Sensitive, exception);
        FrontComposerWarningLog.LocalStorageDrainWriteFailed(logger, Sensitive, exception);
        FrontComposerWarningLog.RegistryRegistrationSkipped(logger, Sensitive, false, false);
        FrontComposerWarningLog.RegistryPolicyConflict(logger, Sensitive, Sensitive, Sensitive);
        FrontComposerWarningLog.RegistryPolicyOverwritten(logger, Sensitive, Sensitive, Sensitive);
        FrontComposerWarningLog.CustomizationValidationFailed(logger, DiagnosticId, 2, Sensitive);
        FrontComposerWarningLog.DiagnosticSinkPublished(logger, DiagnosticId, "Warning", Sensitive);
        FrontComposerWarningLog.ProjectionSlotInvalidContractVersion(logger, Sensitive, Sensitive, -1);
        FrontComposerWarningLog.ProjectionSlotIncompatibleContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            "MajorMismatch",
            1,
            2,
            3,
            4,
            5,
            6);
        FrontComposerWarningLog.ProjectionSlotInvalidComponent(
            logger,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.ProjectionSlotDuplicate(logger, Sensitive, Sensitive, Sensitive, Sensitive, Sensitive);
        FrontComposerWarningLog.ProjectionTemplateIncompatibleContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            "MajorMismatch",
            1,
            2,
            3,
            4,
            5,
            6);
        FrontComposerWarningLog.ProjectionTemplateDuplicate(logger, Sensitive, Sensitive, Sensitive, Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideNullSource(logger, 3);
        FrontComposerWarningLog.ProjectionViewOverrideInvalidContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            -1,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideIncompatibleContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            "MajorMismatch",
            1,
            2,
            3,
            4,
            5,
            6,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideInvalidComponent(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideDuplicate(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.StubLifecycleCallbackFailed(logger, Sensitive, exception);
        FrontComposerWarningLog.StubBackgroundTaskFaulted(logger, exception);
        FrontComposerWarningLog.ShortcutHandlerFailed(logger, DiagnosticId, Sensitive, Sensitive, exception);
        FrontComposerWarningLog.CapabilityPersistFailed(logger, DiagnosticId, Sensitive, exception);
        FrontComposerWarningLog.CapabilityHydrateFailed(logger, DiagnosticId, exception);
        FrontComposerWarningLog.BadgeSnapshotFailed(logger, DiagnosticId, exception);
        FrontComposerWarningLog.PaletteShortcutServiceMissing(logger, DiagnosticId);
        FrontComposerWarningLog.PaletteRegistryEnumerationFailed(logger, DiagnosticId, exception);
        FrontComposerWarningLog.PaletteManifestScoringFailed(logger, DiagnosticId, Sensitive, exception);
        FrontComposerWarningLog.PaletteNavigationServiceMissing(logger, DiagnosticId);
        FrontComposerWarningLog.PaletteNavigationRefused(logger, DiagnosticId, exception);
        FrontComposerWarningLog.PaletteOpenRegistryEnumerationFailed(logger, DiagnosticId, exception);
        FrontComposerWarningLog.PaletteOpenManifestFailed(logger, DiagnosticId, Sensitive, exception);
        FrontComposerWarningLog.PaletteAuthorizationEvaluatorMissing(logger);
        FrontComposerWarningLog.ProjectionLoadSchemaFailed(logger, Sensitive, "SchemaMismatchException");
        FrontComposerWarningLog.ProjectionLoadTerminalDispatchFailed(logger, Sensitive, 4, exception);
        FrontComposerWarningLog.LoadedPageNullItems(logger, Sensitive, 4);
        FrontComposerWarningLog.ThemeHydrationFailed(logger, exception);
        FrontComposerWarningLog.ThemePersistenceFailed(logger, exception);

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe(Enumerable.Range(5800, 54));
        logger.Entries.Select(static entry => entry.EventId.Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(54);
        logger.Entries.Count(static entry => entry.Level == LogLevel.Warning).ShouldBe(49);
        logger.Entries.Count(static entry => entry.Level == LogLevel.Error).ShouldBe(5);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(Sensitive, StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains("/var/private", StringComparison.Ordinal));
        logger.Entries[1].State["ProjectionTypeDigest"].ShouldBe("sha256:5d945b945ccfc83b");
        logger.Entries[13].State["MessageDigest"].ShouldBe("sha256:5d945b945ccfc83b");
    }

    [Fact]
    public void DisabledEvents_DoNotEvaluateSensitiveValues()
    {
        DisabledLogger logger = new();
        ThrowingValue value = new();
        InvalidOperationException exception = new(Sensitive);

        Should.NotThrow(() =>
        {
            FrontComposerWarningLog.BadgeReaderFailed(logger, DiagnosticId, value, exception);
            FrontComposerWarningLog.ProjectionViewOverrideRenderFailed(
                logger,
                DiagnosticId,
                value,
                value,
                value,
                value,
                value,
                value,
                1,
                false);
            FrontComposerWarningLog.DiagnosticSinkPublished(logger, value, value, value);
            FrontComposerWarningLog.ProjectionLoadTerminalDispatchFailed(logger, value, 1, exception);
        });
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

    private sealed class ThrowingValue
    {
        public override string ToString()
            => throw new InvalidOperationException("ToString must be deferred until logging is enabled.");
    }
}

