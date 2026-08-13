using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

public sealed class FrontComposerWarningLogTests
{
    private const string DiagnosticId = "HFC2115";
    private const string Sensitive = "jwt.payload.signature /var/private/secret";
    private static readonly string[] ExpectedEventNames =
    [
        "BadgeCatalogEnumerationFailed",
        "BadgeReaderFailed",
        "BadgeNegativeCount",
        "BadgeNotifierFailed",
        "LayoutUnknownViewportTier",
        "LayoutSubscribeFailed",
        "FieldSlotMissingParameters",
        "FieldSlotTypeMismatch",
        "FieldSlotRenderFailed",
        "ProjectionSubtitleSubscribeFailed",
        "ProjectionSubtitleDisposeFailed",
        "ProjectionTemplateRenderFailed",
        "ProjectionViewOverrideRenderFailed",
        "BootstrapValidationFailed",
        "ProblemDetailsContentLengthExceeded",
        "ProblemDetailsReadExceeded",
        "ProblemDetailsParseFailed",
        "LocalStorageDeserializeFailed",
        "LocalStorageDrainWriteFailed",
        "RegistryRegistrationSkipped",
        "RegistryPolicyConflict",
        "RegistryPolicyOverwritten",
        "CustomizationValidationFailed",
        "DiagnosticSinkPublished",
        "ProjectionSlotInvalidContractVersion",
        "ProjectionSlotIncompatibleContractVersion",
        "ProjectionSlotInvalidComponent",
        "ProjectionSlotDuplicate",
        "ProjectionTemplateIncompatibleContractVersion",
        "ProjectionTemplateDuplicate",
        "ProjectionViewOverrideNullSource",
        "ProjectionViewOverrideInvalidContractVersion",
        "ProjectionViewOverrideIncompatibleContractVersion",
        "ProjectionViewOverrideInvalidComponent",
        "ProjectionViewOverrideDuplicate",
        "StubLifecycleCallbackFailed",
        "StubBackgroundTaskFaulted",
        "ShortcutHandlerFailed",
        "CapabilityPersistFailed",
        "CapabilityHydrateFailed",
        "BadgeSnapshotFailed",
        "PaletteShortcutServiceMissing",
        "PaletteRegistryEnumerationFailed",
        "PaletteManifestScoringFailed",
        "PaletteNavigationServiceMissing",
        "PaletteNavigationRefused",
        "PaletteOpenRegistryEnumerationFailed",
        "PaletteOpenManifestFailed",
        "PaletteAuthorizationEvaluatorMissing",
        "ProjectionLoadSchemaFailed",
        "ProjectionLoadTerminalDispatchFailed",
        "LoadedPageNullItems",
        "ThemeHydrationFailed",
        "ThemePersistenceFailed",
        "EventStoreLifecycleCallbackFailed",
    ];

    [Fact]
    public void AllEvents_WhenEmitted_UsePinnedContractsAndSupportSafePayloads()
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
        FrontComposerWarningLog.FieldSlotTypeMismatch(
            logger,
            DiagnosticId,
            "projection-secret",
            "field-secret",
            "descriptor-secret",
            "host-secret");
        FrontComposerWarningLog.FieldSlotRenderFailed(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            ProjectionRole.Dashboard,
            Sensitive,
            exception);
        FrontComposerWarningLog.ProjectionSubtitleSubscribeFailed(logger, Sensitive, exception);
        FrontComposerWarningLog.ProjectionSubtitleDisposeFailed(logger, exception);
        FrontComposerWarningLog.ProjectionTemplateRenderFailed(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            ProjectionRole.Dashboard,
            exception);
        FrontComposerWarningLog.ProjectionViewOverrideRenderFailed(
            logger,
            DiagnosticId,
            Sensitive,
            Sensitive,
            ProjectionRole.Dashboard,
            nameof(InvalidOperationException),
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
            CustomizationContractVersionDecision.MajorMismatch,
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
        FrontComposerWarningLog.ProjectionSlotDuplicate(
            logger,
            Sensitive,
            ProjectionRole.Dashboard,
            Sensitive,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.ProjectionTemplateIncompatibleContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            ProjectionRole.Dashboard,
            CustomizationContractVersionDecision.MajorMismatch,
            1,
            2,
            3,
            4,
            5,
            6);
        FrontComposerWarningLog.ProjectionTemplateDuplicate(
            logger,
            Sensitive,
            ProjectionRole.Dashboard,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideNullSource(logger, 3);
        FrontComposerWarningLog.ProjectionViewOverrideInvalidContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            ProjectionRole.Dashboard,
            -1,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideIncompatibleContractVersion(
            logger,
            DiagnosticId,
            Sensitive,
            ProjectionRole.Dashboard,
            CustomizationContractVersionDecision.MajorMismatch,
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
            ProjectionRole.Dashboard,
            Sensitive,
            Sensitive,
            Sensitive);
        FrontComposerWarningLog.ProjectionViewOverrideDuplicate(
            logger,
            DiagnosticId,
            Sensitive,
            ProjectionRole.Dashboard,
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
        FrontComposerWarningLog.EventStoreLifecycleCallbackFailed(logger, Sensitive, exception);

        logger.Entries.Select(static entry => entry.EventId.Id).ShouldBe(Enumerable.Range(5800, 55));
        logger.Entries.Select(static entry => entry.EventId.Name).ShouldBe(ExpectedEventNames);
        logger.Entries.Count(static entry => entry.Level == LogLevel.Warning).ShouldBe(49);
        logger.Entries.Count(static entry => entry.Level == LogLevel.Error).ShouldBe(6);
        logger.Entries.ShouldAllBe(static entry => entry.Exception == null);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(Sensitive, StringComparison.Ordinal));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains("/var/private", StringComparison.Ordinal));
        logger.Entries[1].State["ProjectionTypeDigest"].ShouldBe("sha256:5d945b945ccfc83b");
        logger.Entries[13].State["MessageDigest"].ShouldBe("sha256:5d945b945ccfc83b");
        logger.Entries[0].State["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
        logger.Entries[7].State["ProjectionTypeDigest"].ShouldBe("sha256:73e4df605cab0ae6");
        logger.Entries[7].State["FieldDigest"].ShouldBe("sha256:961a8528b6efb484");
        logger.Entries[7].State["DescriptorFieldTypeDigest"].ShouldBe("sha256:10bb37e980be0d75");
        logger.Entries[7].State["HostFieldTypeDigest"].ShouldBe("sha256:736340e6842adca3");
        foreach (int eventId in new[] { 5806, 5807, 5825, 5826, 5827, 5828, 5830, 5831, 5832, 5833, 5834, 5848 })
        {
            logger.Entries.Single(entry => entry.EventId.Id == eventId).Message.ShouldContain(
                eventId == 5848 ? "AddHexalithFrontComposer" : "Fix");
        }

        logger.Entries.Single(static entry => entry.EventId.Id == 5824).Message.ShouldContain("Expected");
    }

    [Fact]
    public void DisabledEvents_WhenInvoked_DoNotEvaluateSensitiveValues()
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

    [Fact]
    public void DiagnosticSinkPublished_WhenShortSafeShapedSecretsAreSupplied_DigestsUnknownCodeAndCategory()
    {
        CapturingLogger<FrontComposerWarningLogTests> logger = new();

        FrontComposerWarningLog.DiagnosticSinkPublished(logger, "SecretToken123", "Tenant42", Sensitive);

        CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        ((string)entry.State["Code"]!).ShouldStartWith("sha256:");
        ((string)entry.State["Category"]!).ShouldStartWith("sha256:");
        entry.Message.ShouldNotContain("SecretToken123");
        entry.Message.ShouldNotContain("Tenant42");
    }

    [Fact]
    public void DiagnosticSinkPublished_WhenMessageExceedsDigestBound_EmitsBoundedStableDigest()
    {
        CapturingLogger<FrontComposerWarningLogTests> logger = new();
        string oversized = new('x', 100_000);

        FrontComposerWarningLog.DiagnosticSinkPublished(logger, "D31", "LastUsed", oversized);
        FrontComposerWarningLog.DiagnosticSinkPublished(logger, "D31", "LastUsed", oversized);

        string first = (string)logger.Entries[0].State["MessageDigest"]!;
        string second = (string)logger.Entries[1].State["MessageDigest"]!;
        first.ShouldBe(second);
        first.ShouldEndWith(":len:100000");
        first.Length.ShouldBeLessThan(64);
        logger.Entries.ShouldAllBe(static entry => entry.Message.Length < 512);
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

