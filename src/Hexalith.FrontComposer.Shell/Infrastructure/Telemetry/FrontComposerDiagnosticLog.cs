using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Shell.Services.DevMode;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>
/// Story 11.21 — source-generated logging for the low-severity (Trace/Debug/Information) Shell
/// diagnostic events that Story 11.18 intentionally left as direct <see cref="ILogger"/> calls.
/// </summary>
/// <remarks>
/// <para>
/// Every event keeps the exact level, message template, placeholder names, and cardinality of the
/// direct call it replaces; this family only moves the emission onto <c>LoggerMessage</c> delegates
/// (CA1848) and defers argument evaluation behind an <c>IsEnabled</c> check (CA1873).
/// </para>
/// <para>
/// String values flow through <see cref="Bounded(string?)"/>, the same support-safety posture as the
/// Story 11.18 families: a value that is already bounded and free of line-forging characters is
/// logged verbatim, and only an oversized value or one carrying a control, line/paragraph separator,
/// or Unicode format character collapses to a salt-free SHA-256 digest so a hostile payload cannot
/// flood the log stream or forge a log line.
/// </para>
/// <para>
/// The digest hashes at most <see cref="MaxDigestCharacters"/> characters of the value. That
/// pre-hash truncation is a deliberate, stated collision surface: two values that differ only beyond
/// that prefix produce the same digest when their character counts also match, and the appended
/// <c>len</c> suffix is the only remaining discriminator. The digest is a support correlation aid,
/// never an integrity or uniqueness proof, and no security decision may be taken on it.
/// </para>
/// <para>
/// EventIds occupy <c>6000-6072</c>. The occupied Shell bands below it are
/// <see cref="FrontComposerLog"/> at <c>5601-5650</c> (pre-11.18), and the Story 11.18 families
/// Security <c>5660-5691</c>, HotPath <c>5700-5780</c>, and Warning <c>5800-5853</c>;
/// SourceTools-generated output owns <c>5900+</c>.
/// </para>
/// </remarks>
internal static partial class FrontComposerDiagnosticLog
{
    private const int MaxLoggedCharacters = 512;

    private const int MaxDigestCharacters = 4096;

    /// <summary>Emits the <c>BadgeProjectionTypeUnresolved</c> low-severity diagnostic event.</summary>
    public static void BadgeProjectionTypeUnresolved(ILogger? logger, string? diagnosticId, string? typeNameString)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? typeNameStringText = Bounded(typeNameString);

        LogBadgeProjectionTypeUnresolved(logger, diagnosticIdText, typeNameStringText);
    }

    /// <summary>Emits the <c>ActionQueueCatalogPartialTypeLoad</c> low-severity diagnostic event.</summary>
    public static void ActionQueueCatalogPartialTypeLoad(ILogger? logger, Exception exception, string? assemblyName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? assemblyNameText = Bounded(assemblyName);

        LogActionQueueCatalogPartialTypeLoad(logger, exception, assemblyNameText);
    }

    /// <summary>Emits the <c>ActionQueueCatalogAssemblySkipped</c> low-severity diagnostic event.</summary>
    public static void ActionQueueCatalogAssemblySkipped(ILogger? logger, Exception exception, string? assemblyName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? assemblyNameText = Bounded(assemblyName);

        LogActionQueueCatalogAssemblySkipped(logger, exception, assemblyNameText);
    }

    /// <summary>Emits the <c>AbandonmentGuardWrapperNavigationYielded</c> low-severity diagnostic event.</summary>
    public static void AbandonmentGuardWrapperNavigationYielded(ILogger? logger, string? diag, string? target)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagText = Bounded(diag);
        string? targetText = Bounded(target);

        LogAbandonmentGuardWrapperNavigationYielded(logger, diagText, targetText);
    }

    /// <summary>Emits the <c>AbandonmentGuardSuppressedWhileSubmitting</c> low-severity diagnostic event.</summary>
    public static void AbandonmentGuardSuppressedWhileSubmitting(ILogger? logger, string? diag, string? cid)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagText = Bounded(diag);
        string? cidText = Bounded(cid);

        LogAbandonmentGuardSuppressedWhileSubmitting(logger, diagText, cidText);
    }

    /// <summary>Emits the <c>EmptyStateCtaCommandTypeUnresolved</c> low-severity diagnostic event.</summary>
    public static void EmptyStateCtaCommandTypeUnresolved(ILogger? logger, string? commandFqn)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? commandFqnText = Bounded(commandFqn);

        LogEmptyStateCtaCommandTypeUnresolved(logger, commandFqnText);
    }

    /// <summary>Emits the <c>DevModeOverlayRegistered</c> low-severity diagnostic event.</summary>
    public static void DevModeOverlayRegistered(
        ILogger? logger,
        string? environmentName,
        string? overlayVersion,
        string? gradientLevels)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? environmentNameText = Bounded(environmentName);
        string? overlayVersionText = Bounded(overlayVersion);
        string? gradientLevelsText = Bounded(gradientLevels);

        LogDevModeOverlayRegistered(logger, environmentNameText, overlayVersionText, gradientLevelsText);
    }

    /// <summary>Emits the <c>DevModeOverlaySkippedOutsideDevelopment</c> low-severity diagnostic event.</summary>
    public static void DevModeOverlaySkippedOutsideDevelopment(ILogger? logger, string? environmentName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? environmentNameText = Bounded(environmentName);

        LogDevModeOverlaySkippedOutsideDevelopment(logger, environmentNameText);
    }

    /// <summary>Emits the <c>RegistryCommandPolicyEntrySkipped</c> low-severity diagnostic event.</summary>
    public static void RegistryCommandPolicyEntrySkipped(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogRegistryCommandPolicyEntrySkipped(logger);
    }

    /// <summary>Emits the <c>ClipboardPayloadRejected</c> low-severity diagnostic event.</summary>
    public static void ClipboardPayloadRejected(ILogger? logger, int maxBytes)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogClipboardPayloadRejected(logger, maxBytes);
    }

    /// <summary>Emits the <c>ClipboardCopyCompleted</c> low-severity diagnostic event.</summary>
    public static void ClipboardCopyCompleted(ILogger? logger, ClipboardCopyResult outcome)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogClipboardCopyCompleted(logger, outcome);
    }

    /// <summary>Emits the <c>ClipboardCopyTimedOut</c> low-severity diagnostic event.</summary>
    public static void ClipboardCopyTimedOut(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        LogClipboardCopyTimedOut(logger);
    }

    /// <summary>Emits the <c>ClipboardCopyFailed</c> low-severity diagnostic event.</summary>
    public static void ClipboardCopyFailed(ILogger? logger, Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? exceptionTypeText = ExceptionTypeName(exception);

        LogClipboardCopyFailed(logger, exceptionTypeText);
    }

    /// <summary>Emits the <c>StarterEmissionLevelMismatch</c> low-severity diagnostic event.</summary>
    public static void StarterEmissionLevelMismatch(
        ILogger? logger,
        string? annotationKey,
        CustomizationLevel currentLevel,
        CustomizationLevel requestedLevel)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? annotationKeyText = Bounded(annotationKey);

        LogStarterEmissionLevelMismatch(logger, annotationKeyText, currentLevel, requestedLevel);
    }

    /// <summary>Emits the <c>StarterEmissionStaleMetadata</c> low-severity diagnostic event.</summary>
    public static void StarterEmissionStaleMetadata(
        ILogger? logger,
        string? annotationKey,
        ImmutableArray<ComponentTreeStaleReason> staleReasons)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? annotationKeyText = Bounded(annotationKey);
        string? reasonsText = Bounded(string.Join(",", staleReasons));

        LogStarterEmissionStaleMetadata(logger, annotationKeyText, reasonsText);
    }

    /// <summary>Emits the <c>StarterEmissionFailed</c> low-severity diagnostic event.</summary>
    public static void StarterEmissionFailed(
        ILogger? logger,
        Exception exception,
        string? annotationKey,
        CustomizationLevel level)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? annotationKeyText = Bounded(annotationKey);

        LogStarterEmissionFailed(logger, exception, annotationKeyText, level);
    }

    /// <summary>Emits the <c>ProjectionSlotContractVersionDrift</c> low-severity diagnostic event.</summary>
    public static void ProjectionSlotContractVersionDrift(
        ILogger? logger,
        string? diagnosticId,
        string? projection,
        string? field,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? projectionText = Bounded(projection);
        string? fieldText = Bounded(field);

        LogProjectionSlotContractVersionDrift(
            logger,
            diagnosticIdText,
            projectionText,
            fieldText,
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild);
    }

    /// <summary>Emits the <c>ProjectionTemplateContractVersionDrift</c> low-severity diagnostic event.</summary>
    public static void ProjectionTemplateContractVersionDrift(
        ILogger? logger,
        string? diagnosticId,
        string? projection,
        ProjectionRole? role,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? projectionText = Bounded(projection);
        string? roleText = Bounded(role?.ToString() ?? "<any>");

        LogProjectionTemplateContractVersionDrift(
            logger,
            diagnosticIdText,
            projectionText,
            roleText,
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild);
    }

    /// <summary>Emits the <c>ProjectionViewOverrideContractVersionDrift</c> low-severity diagnostic event.</summary>
    public static void ProjectionViewOverrideContractVersionDrift(
        ILogger? logger,
        string? diagnosticId,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild,
        string? source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? sourceText = Bounded(source);

        LogProjectionViewOverrideContractVersionDrift(
            logger,
            diagnosticIdText,
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild,
            sourceText);
    }

    /// <summary>Emits the <c>ProjectionViewOverrideBuildDrift</c> low-severity diagnostic event.</summary>
    public static void ProjectionViewOverrideBuildDrift(
        ILogger? logger,
        string? diagnosticId,
        string? projection,
        ProjectionRole? role,
        int current,
        int contractVersion,
        string? source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? projectionText = Bounded(projection);
        string? roleText = Bounded(role?.ToString() ?? "<any>");
        string? sourceText = Bounded(source);

        LogProjectionViewOverrideBuildDrift(
            logger,
            diagnosticIdText,
            projectionText,
            roleText,
            current,
            contractVersion,
            sourceText);
    }

    /// <summary>Emits the <c>ShortcutDuplicateRegistrationReplaced</c> low-severity diagnostic event.</summary>
    public static void ShortcutDuplicateRegistrationReplaced(
        ILogger? logger,
        string? diagnosticId,
        string? binding,
        string? previousDescriptionKey,
        string? newDescriptionKey,
        string? previousCallSiteFile,
        int previousCallSiteLine,
        string? newCallSiteFile,
        int newCallSiteLine)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? bindingText = Bounded(binding);
        string? previousDescriptionKeyText = Bounded(previousDescriptionKey);
        string? newDescriptionKeyText = Bounded(newDescriptionKey);
        string? previousCallSiteFileText = Bounded(previousCallSiteFile);
        string? newCallSiteFileText = Bounded(newCallSiteFile);

        LogShortcutDuplicateRegistrationReplaced(
            logger,
            diagnosticIdText,
            bindingText,
            previousDescriptionKeyText,
            newDescriptionKeyText,
            previousCallSiteFileText,
            previousCallSiteLine,
            newCallSiteFileText,
            newCallSiteLine);
    }

    /// <summary>Emits the <c>CapabilitySeenPersistCancelled</c> low-severity diagnostic event.</summary>
    public static void CapabilitySeenPersistCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogCapabilitySeenPersistCancelled(logger);
    }

    /// <summary>Emits the <c>CapabilitySeenHydrateCancelled</c> low-severity diagnostic event.</summary>
    public static void CapabilitySeenHydrateCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogCapabilitySeenHydrateCancelled(logger);
    }

    /// <summary>Emits the <c>PaletteHydrationCancelled</c> low-severity diagnostic event.</summary>
    public static void PaletteHydrationCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPaletteHydrationCancelled(logger);
    }

    /// <summary>Emits the <c>PaletteHydrationErrored</c> low-severity diagnostic event.</summary>
    public static void PaletteHydrationErrored(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogPaletteHydrationErrored(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>PaletteHydrationEmpty</c> low-severity diagnostic event.</summary>
    public static void PaletteHydrationEmpty(ILogger? logger, string? diagnosticId, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogPaletteHydrationEmpty(logger, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>PaletteRecentRouteEntriesRejected</c> low-severity diagnostic event.</summary>
    public static void PaletteRecentRouteEntriesRejected(
        ILogger? logger,
        string? diagnosticId,
        int rejectedCount,
        int totalCount,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogPaletteRecentRouteEntriesRejected(logger, diagnosticIdText, rejectedCount, totalCount, reasonText);
    }

    /// <summary>Emits the <c>PaletteCommandRouteMissing</c> low-severity diagnostic event.</summary>
    public static void PaletteCommandRouteMissing(ILogger? logger, string? diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogPaletteCommandRouteMissing(logger, diagnosticIdText);
    }

    /// <summary>Emits the <c>PaletteActivationRejectedByRouteFilter</c> low-severity diagnostic event.</summary>
    public static void PaletteActivationRejectedByRouteFilter(
        ILogger? logger,
        string? diagnosticId,
        PaletteResultCategory category,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogPaletteActivationRejectedByRouteFilter(logger, diagnosticIdText, category, reasonText);
    }

    /// <summary>Emits the <c>PaletteActivationWithoutTargetUrl</c> low-severity diagnostic event.</summary>
    public static void PaletteActivationWithoutTargetUrl(
        ILogger? logger,
        string? diagnosticId,
        PaletteResultCategory category,
        string? boundedContext,
        string? commandTypeName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? boundedContextText = Bounded(boundedContext);
        string? commandTypeNameText = Bounded(commandTypeName);

        LogPaletteActivationWithoutTargetUrl(
            logger,
            diagnosticIdText,
            category,
            boundedContextText,
            commandTypeNameText);
    }

    /// <summary>Emits the <c>PaletteRecentRoutePersistGateTimeout</c> low-severity diagnostic event.</summary>
    public static void PaletteRecentRoutePersistGateTimeout(ILogger? logger, string? diagnosticId, int timeoutMs)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogPaletteRecentRoutePersistGateTimeout(logger, diagnosticIdText, timeoutMs);
    }

    /// <summary>Emits the <c>PaletteRecentRoutePersistCancelled</c> low-severity diagnostic event.</summary>
    public static void PaletteRecentRoutePersistCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogPaletteRecentRoutePersistCancelled(logger);
    }

    /// <summary>Emits the <c>PaletteRecentRoutePersistFailed</c> low-severity diagnostic event.</summary>
    public static void PaletteRecentRoutePersistFailed(ILogger? logger, Exception exception, string? diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogPaletteRecentRoutePersistFailed(logger, exception, diagnosticIdText);
    }

    /// <summary>Emits the <c>PaletteCommandTypeUnresolved</c> low-severity diagnostic event.</summary>
    public static void PaletteCommandTypeUnresolved(ILogger? logger, string? commandTypeName)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? commandTypeNameText = Bounded(commandTypeName);

        LogPaletteCommandTypeUnresolved(logger, commandTypeNameText);
    }

    /// <summary>Emits the <c>DataGridPersistCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridPersistCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridPersistCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridPersistFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridPersistFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? direction,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? directionText = Bounded(direction);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridPersistFailed(logger, exception, diagnosticIdText, directionText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridClearCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridClearCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridClearCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridClearFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridClearFailed(ILogger? logger, Exception exception, string? diagnosticId, string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridClearFailed(logger, exception, diagnosticIdText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridOnDemandHydrateCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridOnDemandHydrateCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridOnDemandHydrateCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridOnDemandHydrateFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridOnDemandHydrateFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridOnDemandHydrateFailed(logger, exception, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridOnDemandHydrateEmpty</c> low-severity diagnostic event.</summary>
    public static void DataGridOnDemandHydrateEmpty(
        ILogger? logger,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridOnDemandHydrateEmpty(logger, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridHydrateCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridHydrateCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridHydrateCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridHydrateKeyEnumerationFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridHydrateKeyEnumerationFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? direction)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? directionText = Bounded(direction);

        LogDataGridHydrateKeyEnumerationFailed(logger, exception, diagnosticIdText, directionText);
    }

    /// <summary>Emits the <c>DataGridMalformedStorageKeyRejected</c> low-severity diagnostic event.</summary>
    public static void DataGridMalformedStorageKeyRejected(
        ILogger? logger,
        string? diagnosticId,
        string? reason,
        string? storageKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? storageKeyText = Bounded(storageKey);

        LogDataGridMalformedStorageKeyRejected(logger, diagnosticIdText, reasonText, storageKeyText);
    }

    /// <summary>Emits the <c>DataGridMalformedKeyPruneCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridMalformedKeyPruneCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridMalformedKeyPruneCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridMalformedKeyPruneFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridMalformedKeyPruneFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? storageKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? storageKeyText = Bounded(storageKey);

        LogDataGridMalformedKeyPruneFailed(logger, exception, diagnosticIdText, storageKeyText);
    }

    /// <summary>Emits the <c>DataGridOutOfScopeSnapshotPruned</c> low-severity diagnostic event.</summary>
    public static void DataGridOutOfScopeSnapshotPruned(
        ILogger? logger,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridOutOfScopeSnapshotPruned(logger, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridOutOfScopePruneCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridOutOfScopePruneCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridOutOfScopePruneCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridOutOfScopePruneFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridOutOfScopePruneFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridOutOfScopePruneFailed(logger, exception, diagnosticIdText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridPerKeyHydrateCancelled</c> low-severity diagnostic event.</summary>
    public static void DataGridPerKeyHydrateCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDataGridPerKeyHydrateCancelled(logger);
    }

    /// <summary>Emits the <c>DataGridPerKeyHydrateFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridPerKeyHydrateFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridPerKeyHydrateFailed(logger, exception, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridPerKeyHydrateEmpty</c> low-severity diagnostic event.</summary>
    public static void DataGridPerKeyHydrateEmpty(
        ILogger? logger,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridPerKeyHydrateEmpty(logger, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridPerKeySnapshotRejected</c> low-severity diagnostic event.</summary>
    public static void DataGridPerKeySnapshotRejected(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);
        string? viewKeyText = Bounded(viewKey);

        LogDataGridPerKeySnapshotRejected(logger, exception, diagnosticIdText, reasonText, viewKeyText);
    }

    /// <summary>Emits the <c>DataGridRegistryUnavailable</c> low-severity diagnostic event.</summary>
    public static void DataGridRegistryUnavailable(ILogger? logger, string? diagnosticId, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogDataGridRegistryUnavailable(logger, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>DataGridRegistryEnumerationFailed</c> low-severity diagnostic event.</summary>
    public static void DataGridRegistryEnumerationFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogDataGridRegistryEnumerationFailed(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>LoadedPageEvicted</c> low-severity diagnostic event.</summary>
    public static void LoadedPageEvicted(ILogger? logger, int cap, string? viewKey, int skip)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? viewKeyText = Bounded(viewKey);

        LogLoadedPageEvicted(logger, cap, viewKeyText, skip);
    }

    /// <summary>Emits the <c>DensityHydrationEmpty</c> low-severity diagnostic event.</summary>
    public static void DensityHydrationEmpty(ILogger? logger, string? diagnosticId, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogDensityHydrationEmpty(logger, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>DensityHydrationCancelled</c> low-severity diagnostic event.</summary>
    public static void DensityHydrationCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDensityHydrationCancelled(logger);
    }

    /// <summary>Emits the <c>DensityHydrationDeferred</c> low-severity diagnostic event.</summary>
    public static void DensityHydrationDeferred(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogDensityHydrationDeferred(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>DensityHydrationErrored</c> low-severity diagnostic event.</summary>
    public static void DensityHydrationErrored(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogDensityHydrationErrored(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>DensityPersistCancelled</c> low-severity diagnostic event.</summary>
    public static void DensityPersistCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogDensityPersistCancelled(logger);
    }

    /// <summary>Emits the <c>DensityPersistFailed</c> low-severity diagnostic event.</summary>
    public static void DensityPersistFailed(ILogger? logger, Exception exception, string? diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogDensityPersistFailed(logger, exception, diagnosticIdText);
    }

    /// <summary>Emits the <c>NavigationHydrationEmpty</c> low-severity diagnostic event.</summary>
    public static void NavigationHydrationEmpty(ILogger? logger, string? diagnosticId, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogNavigationHydrationEmpty(logger, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>NavigationHydrationCancelled</c> low-severity diagnostic event.</summary>
    public static void NavigationHydrationCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogNavigationHydrationCancelled(logger);
    }

    /// <summary>Emits the <c>NavigationHydrationErrored</c> low-severity diagnostic event.</summary>
    public static void NavigationHydrationErrored(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogNavigationHydrationErrored(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>NavigationRoutePrunedInvalid</c> low-severity diagnostic event.</summary>
    public static void NavigationRoutePrunedInvalid(ILogger? logger, string? diagnosticId, string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogNavigationRoutePrunedInvalid(logger, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>NavigationRoutePrunedOutOfScope</c> low-severity diagnostic event.</summary>
    public static void NavigationRoutePrunedOutOfScope(
        ILogger? logger,
        string? diagnosticId,
        string? boundedContext,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? boundedContextText = Bounded(boundedContext);
        string? reasonText = Bounded(reason);

        LogNavigationRoutePrunedOutOfScope(logger, diagnosticIdText, boundedContextText, reasonText);
    }

    /// <summary>Emits the <c>NavigationRegistryEnumerationFailed</c> low-severity diagnostic event.</summary>
    public static void NavigationRegistryEnumerationFailed(
        ILogger? logger,
        Exception exception,
        string? diagnosticId,
        string? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);
        string? reasonText = Bounded(reason);

        LogNavigationRegistryEnumerationFailed(logger, exception, diagnosticIdText, reasonText);
    }

    /// <summary>Emits the <c>NavigationPersistCancelled</c> low-severity diagnostic event.</summary>
    public static void NavigationPersistCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogNavigationPersistCancelled(logger);
    }

    /// <summary>Emits the <c>NavigationPersistFailed</c> low-severity diagnostic event.</summary>
    public static void NavigationPersistFailed(ILogger? logger, Exception exception, string? diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogNavigationPersistFailed(logger, exception, diagnosticIdText);
    }

    /// <summary>Emits the <c>ScopeReadinessStorageReadyDispatched</c> low-severity diagnostic event.</summary>
    public static void ScopeReadinessStorageReadyDispatched(ILogger? logger, string? correlationId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        string? correlationIdText = Bounded(correlationId);

        LogScopeReadinessStorageReadyDispatched(logger, correlationIdText);
    }

    /// <summary>Emits the <c>ThemeHydrationEmpty</c> low-severity diagnostic event.</summary>
    public static void ThemeHydrationEmpty(ILogger? logger, string? diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string? diagnosticIdText = Bounded(diagnosticId);

        LogThemeHydrationEmpty(logger, diagnosticIdText);
    }

    /// <summary>Emits the <c>ThemeHydrationCancelled</c> low-severity diagnostic event.</summary>
    public static void ThemeHydrationCancelled(ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        LogThemeHydrationCancelled(logger);
    }

    [LoggerMessage(EventId = 6000, EventName = "BadgeProjectionTypeUnresolved", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Projection type-name '{TypeNameString}' failed Type.GetType resolution — most likely an adopter mis-registration.")]
    private static partial void LogBadgeProjectionTypeUnresolved(
        ILogger logger,
        string? diagnosticId,
        string? typeNameString);

    [LoggerMessage(EventId = 6001, EventName = "ActionQueueCatalogPartialTypeLoad", Level = LogLevel.Information,
        Message = "ReflectionActionQueueProjectionCatalog: partial type-load for assembly '{AssemblyName}' — continuing with resolved types.")]
    private static partial void LogActionQueueCatalogPartialTypeLoad(
        ILogger logger,
        Exception exception,
        string? assemblyName);

    [LoggerMessage(EventId = 6002, EventName = "ActionQueueCatalogAssemblySkipped", Level = LogLevel.Information,
        Message = "ReflectionActionQueueProjectionCatalog: skipped assembly '{AssemblyName}' — GetTypes threw.")]
    private static partial void LogActionQueueCatalogAssemblySkipped(
        ILogger logger,
        Exception exception,
        string? assemblyName);

    [LoggerMessage(EventId = 6003, EventName = "AbandonmentGuardWrapperNavigationYielded", Level = LogLevel.Information,
        Message = "{Diag} Abandonment guard yielded for wrapper-initiated navigation. Target={Target}")]
    private static partial void LogAbandonmentGuardWrapperNavigationYielded(
        ILogger logger,
        string? diag,
        string? target);

    [LoggerMessage(EventId = 6004, EventName = "AbandonmentGuardSuppressedWhileSubmitting", Level = LogLevel.Information,
        Message = "{Diag} Abandonment guard suppressed while lifecycle state is Submitting. CorrelationId={Cid}")]
    private static partial void LogAbandonmentGuardSuppressedWhileSubmitting(ILogger logger, string? diag, string? cid);

    [LoggerMessage(EventId = 6005, EventName = "EmptyStateCtaCommandTypeUnresolved", Level = LogLevel.Information,
        Message = "Empty-state CTA suppressed: command type {CommandFqn} could not be resolved. The type may be trimmed, renamed, or live in a different assembly than the projection.")]
    private static partial void LogEmptyStateCtaCommandTypeUnresolved(ILogger logger, string? commandFqn);

    [LoggerMessage(EventId = 6006, EventName = "DevModeOverlayRegistered", Level = LogLevel.Information,
        Message = "FrontComposer dev-mode overlay registered. Environment={EnvironmentName} OverlayVersion={OverlayVersion} GradientLevels={GradientLevels}")]
    private static partial void LogDevModeOverlayRegistered(
        ILogger logger,
        string? environmentName,
        string? overlayVersion,
        string? gradientLevels);

    [LoggerMessage(EventId = 6007, EventName = "DevModeOverlaySkippedOutsideDevelopment", Level = LogLevel.Information,
        Message = "HFC2010: FrontComposer dev-mode AddFrontComposerDevMode invoked outside Development; overlay services were not registered. Environment={EnvironmentName}")]
    private static partial void LogDevModeOverlaySkippedOutsideDevelopment(ILogger logger, string? environmentName);

    [LoggerMessage(EventId = 6008, EventName = "RegistryCommandPolicyEntrySkipped", Level = LogLevel.Information,
        Message = "FrontComposer registry merge: skipping command policy entry with empty key or value during manifest merge.")]
    private static partial void LogRegistryCommandPolicyEntrySkipped(ILogger logger);

    [LoggerMessage(EventId = 6009, EventName = "ClipboardPayloadRejected", Level = LogLevel.Information,
        Message = "Dev-mode clipboard payload exceeded MaxClipboardPayloadBytes={MaxBytes}; copy rejected.")]
    private static partial void LogClipboardPayloadRejected(ILogger logger, int maxBytes);

    [LoggerMessage(EventId = 6010, EventName = "ClipboardCopyCompleted", Level = LogLevel.Information,
        Message = "Dev-mode clipboard copy completed. Outcome={Outcome}")]
    private static partial void LogClipboardCopyCompleted(ILogger logger, ClipboardCopyResult outcome);

    [LoggerMessage(EventId = 6011, EventName = "ClipboardCopyTimedOut", Level = LogLevel.Information,
        Message = "Dev-mode clipboard copy timed out.")]
    private static partial void LogClipboardCopyTimedOut(ILogger logger);

    [LoggerMessage(EventId = 6012, EventName = "ClipboardCopyFailed", Level = LogLevel.Information,
        Message = "Dev-mode clipboard copy failed unexpectedly. ExceptionType={ExceptionType}")]
    private static partial void LogClipboardCopyFailed(ILogger logger, string? exceptionType);

    [LoggerMessage(EventId = 6013, EventName = "StarterEmissionLevelMismatch", Level = LogLevel.Information,
        Message = "HFC1048: Unsupported customization level requested for starter emission. AnnotationKey={AnnotationKey} CurrentLevel={CurrentLevel} RequestedLevel={RequestedLevel}")]
    private static partial void LogStarterEmissionLevelMismatch(
        ILogger logger,
        string? annotationKey,
        CustomizationLevel currentLevel,
        CustomizationLevel requestedLevel);

    [LoggerMessage(EventId = 6014, EventName = "StarterEmissionStaleMetadata", Level = LogLevel.Information,
        Message = "HFC1049: Stale component-tree metadata suppressed starter emission. AnnotationKey={AnnotationKey} Reasons={Reasons}")]
    private static partial void LogStarterEmissionStaleMetadata(ILogger logger, string? annotationKey, string? reasons);

    [LoggerMessage(EventId = 6015, EventName = "StarterEmissionFailed", Level = LogLevel.Information,
        Message = "HFC1048: Starter emission failed. AnnotationKey={AnnotationKey} Level={Level}")]
    private static partial void LogStarterEmissionFailed(
        ILogger logger,
        Exception exception,
        string? annotationKey,
        CustomizationLevel level);

    [LoggerMessage(EventId = 6016, EventName = "ProjectionSlotContractVersionDrift", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Level 3 slot descriptor for projection {Projection} field {Field} targets contract minor {ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} but installed framework reports {ActualMajor}.{ActualMinor}.{ActualBuild}. Override accepted (source-compatible). Fix: rebuild the slot to silence this message. Docs: https://hexalith.github.io/FrontComposer/diagnostics/HFC1041")]
    private static partial void LogProjectionSlotContractVersionDrift(
        ILogger logger,
        string? diagnosticId,
        string? projection,
        string? field,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild);

    [LoggerMessage(EventId = 6017, EventName = "ProjectionTemplateContractVersionDrift", Level = LogLevel.Information,
        Message = "{DiagnosticId}: ProjectionTemplateDescriptor for projection {Projection} (role {Role}) targets contract minor {ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} but installed framework reports {ActualMajor}.{ActualMinor}.{ActualBuild}. Override accepted (source-compatible). Fix: rebuild the template to silence this message. Docs: https://hexalith.github.io/FrontComposer/diagnostics/HFC1036")]
    private static partial void LogProjectionTemplateContractVersionDrift(
        ILogger logger,
        string? diagnosticId,
        string? projection,
        string? role,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild);

    [LoggerMessage(EventId = 6018, EventName = "ProjectionViewOverrideContractVersionDrift", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Level 4 view override targets contract minor {ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} but installed framework reports {ActualMajor}.{ActualMinor}.{ActualBuild}. Override accepted (source-compatible). Source: {Source}. Fix: rebuild the replacement to silence this message. Docs: https://hexalith.github.io/FrontComposer/diagnostics/HFC1045")]
    private static partial void LogProjectionViewOverrideContractVersionDrift(
        ILogger logger,
        string? diagnosticId,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild,
        string? source);

    [LoggerMessage(EventId = 6019, EventName = "ProjectionViewOverrideBuildDrift", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Level 4 view override build drift for projection {Projection} role {Role}. Installed: {Current}. Got: {ContractVersion}. Source: {Source}. Selection proceeds.")]
    private static partial void LogProjectionViewOverrideBuildDrift(
        ILogger logger,
        string? diagnosticId,
        string? projection,
        string? role,
        int current,
        int contractVersion,
        string? source);

    [LoggerMessage(EventId = 6020, EventName = "ShortcutDuplicateRegistrationReplaced", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Duplicate shortcut registration replaced. Binding={Binding} PreviousDescriptionKey={PreviousDescriptionKey} NewDescriptionKey={NewDescriptionKey} PreviousCallSiteFile={PreviousCallSiteFile} PreviousCallSiteLine={PreviousCallSiteLine} NewCallSiteFile={NewCallSiteFile} NewCallSiteLine={NewCallSiteLine}")]
    private static partial void LogShortcutDuplicateRegistrationReplaced(
        ILogger logger,
        string? diagnosticId,
        string? binding,
        string? previousDescriptionKey,
        string? newDescriptionKey,
        string? previousCallSiteFile,
        int previousCallSiteLine,
        string? newCallSiteFile,
        int newCallSiteLine);

    [LoggerMessage(EventId = 6021, EventName = "CapabilitySeenPersistCancelled", Level = LogLevel.Debug,
        Message = "Capability-seen persist cancelled — circuit disposing.")]
    private static partial void LogCapabilitySeenPersistCancelled(ILogger logger);

    [LoggerMessage(EventId = 6022, EventName = "CapabilitySeenHydrateCancelled", Level = LogLevel.Debug,
        Message = "Capability-seen hydrate cancelled — circuit disposing.")]
    private static partial void LogCapabilitySeenHydrateCancelled(ILogger logger);

    [LoggerMessage(EventId = 6023, EventName = "PaletteHydrationCancelled", Level = LogLevel.Debug,
        Message = "Palette hydration cancelled — circuit disposing.")]
    private static partial void LogPaletteHydrationCancelled(ILogger logger);

    [LoggerMessage(EventId = 6024, EventName = "PaletteHydrationErrored", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Palette hydration errored. Reason={Reason}.")]
    private static partial void LogPaletteHydrationErrored(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6025, EventName = "PaletteHydrationEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Palette hydration found no stored value. Reason={Reason}.")]
    private static partial void LogPaletteHydrationEmpty(ILogger logger, string? diagnosticId, string? reason);

    [LoggerMessage(EventId = 6026, EventName = "PaletteRecentRouteEntriesRejected", Level = LogLevel.Information,
        Message = "{DiagnosticId}: {RejectedCount} of {TotalCount} palette recent-route entries rejected. Reason={Reason}.")]
    private static partial void LogPaletteRecentRouteEntriesRejected(
        ILogger logger,
        string? diagnosticId,
        int rejectedCount,
        int totalCount,
        string? reason);

    [LoggerMessage(EventId = 6027, EventName = "PaletteCommandRouteMissing", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Command activation rejected because no generated full-page route is registered.")]
    private static partial void LogPaletteCommandRouteMissing(ILogger logger, string? diagnosticId);

    [LoggerMessage(EventId = 6028, EventName = "PaletteActivationRejectedByRouteFilter", Level = LogLevel.Information,
        Message = "{DiagnosticId}: {Category} activation rejected by internal-route filter. Reason={Reason}.")]
    private static partial void LogPaletteActivationRejectedByRouteFilter(
        ILogger logger,
        string? diagnosticId,
        PaletteResultCategory category,
        string? reason);

    [LoggerMessage(EventId = 6029, EventName = "PaletteActivationWithoutTargetUrl", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Palette command activation produced no target URL — Category={Category}, BoundedContext='{BoundedContext}', CommandTypeName='{CommandTypeName}'. Closing palette without navigation.")]
    private static partial void LogPaletteActivationWithoutTargetUrl(
        ILogger logger,
        string? diagnosticId,
        PaletteResultCategory category,
        string? boundedContext,
        string? commandTypeName);

    [LoggerMessage(EventId = 6030, EventName = "PaletteRecentRoutePersistGateTimeout", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Palette recent-route persist gate timeout after {TimeoutMs}ms — dropping stale payload (a newer activation will retry).")]
    private static partial void LogPaletteRecentRoutePersistGateTimeout(
        ILogger logger,
        string? diagnosticId,
        int timeoutMs);

    [LoggerMessage(EventId = 6031, EventName = "PaletteRecentRoutePersistCancelled", Level = LogLevel.Debug,
        Message = "Palette recent-route persist cancelled — circuit disposing.")]
    private static partial void LogPaletteRecentRoutePersistCancelled(ILogger logger);

    [LoggerMessage(EventId = 6032, EventName = "PaletteRecentRoutePersistFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Palette recent-route persistence failed.")]
    private static partial void LogPaletteRecentRoutePersistFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId);

    [LoggerMessage(EventId = 6033, EventName = "PaletteCommandTypeUnresolved", Level = LogLevel.Information,
        Message = "Command palette filter dropped {CommandTypeName}: ProjectionTypeResolver could not resolve the type. Possible trim/AOT mismatch or assembly removal.")]
    private static partial void LogPaletteCommandTypeUnresolved(ILogger logger, string? commandTypeName);

    [LoggerMessage(EventId = 6034, EventName = "DataGridPersistCancelled", Level = LogLevel.Debug,
        Message = "DataGrid persist cancelled — circuit disposing.")]
    private static partial void LogDataGridPersistCancelled(ILogger logger);

    [LoggerMessage(EventId = 6035, EventName = "DataGridPersistFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid {Direction} failed — swallowed (next capture retries). ViewKey={ViewKey}.")]
    private static partial void LogDataGridPersistFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? direction,
        string? viewKey);

    [LoggerMessage(EventId = 6036, EventName = "DataGridClearCancelled", Level = LogLevel.Debug,
        Message = "DataGrid clear cancelled — circuit disposing.")]
    private static partial void LogDataGridClearCancelled(ILogger logger);

    [LoggerMessage(EventId = 6037, EventName = "DataGridClearFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid clear failed — swallowed. ViewKey={ViewKey}.")]
    private static partial void LogDataGridClearFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? viewKey);

    [LoggerMessage(EventId = 6038, EventName = "DataGridOnDemandHydrateCancelled", Level = LogLevel.Debug,
        Message = "DataGrid on-demand hydrate cancelled — circuit disposing.")]
    private static partial void LogDataGridOnDemandHydrateCancelled(ILogger logger);

    [LoggerMessage(EventId = 6039, EventName = "DataGridOnDemandHydrateFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid on-demand hydrate failed. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridOnDemandHydrateFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6040, EventName = "DataGridOnDemandHydrateEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid on-demand hydrate found no stored value. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridOnDemandHydrateEmpty(
        ILogger logger,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6041, EventName = "DataGridHydrateCancelled", Level = LogLevel.Debug,
        Message = "DataGrid hydrate cancelled — circuit disposing.")]
    private static partial void LogDataGridHydrateCancelled(ILogger logger);

    [LoggerMessage(EventId = 6042, EventName = "DataGridHydrateKeyEnumerationFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid hydrate key enumeration failed — hydrate abandoned. Direction={Direction}.")]
    private static partial void LogDataGridHydrateKeyEnumerationFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? direction);

    [LoggerMessage(EventId = 6043, EventName = "DataGridMalformedStorageKeyRejected", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid per-key hydrate rejected malformed storage key. Reason={Reason}. StorageKey={StorageKey}.")]
    private static partial void LogDataGridMalformedStorageKeyRejected(
        ILogger logger,
        string? diagnosticId,
        string? reason,
        string? storageKey);

    [LoggerMessage(EventId = 6044, EventName = "DataGridMalformedKeyPruneCancelled", Level = LogLevel.Debug,
        Message = "DataGrid malformed-key prune cancelled — circuit disposing.")]
    private static partial void LogDataGridMalformedKeyPruneCancelled(ILogger logger);

    [LoggerMessage(EventId = 6045, EventName = "DataGridMalformedKeyPruneFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid malformed-key prune failed. StorageKey={StorageKey}.")]
    private static partial void LogDataGridMalformedKeyPruneFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? storageKey);

    [LoggerMessage(EventId = 6046, EventName = "DataGridOutOfScopeSnapshotPruned", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Pruning stale DataGrid snapshot — bounded context is no longer registered. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridOutOfScopeSnapshotPruned(
        ILogger logger,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6047, EventName = "DataGridOutOfScopePruneCancelled", Level = LogLevel.Debug,
        Message = "DataGrid out-of-scope prune cancelled — circuit disposing.")]
    private static partial void LogDataGridOutOfScopePruneCancelled(ILogger logger);

    [LoggerMessage(EventId = 6048, EventName = "DataGridOutOfScopePruneFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid out-of-scope prune failed. ViewKey={ViewKey}.")]
    private static partial void LogDataGridOutOfScopePruneFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? viewKey);

    [LoggerMessage(EventId = 6049, EventName = "DataGridPerKeyHydrateCancelled", Level = LogLevel.Debug,
        Message = "DataGrid per-key hydrate cancelled — circuit disposing.")]
    private static partial void LogDataGridPerKeyHydrateCancelled(ILogger logger);

    [LoggerMessage(EventId = 6050, EventName = "DataGridPerKeyHydrateFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid per-key hydrate failed. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridPerKeyHydrateFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6051, EventName = "DataGridPerKeyHydrateEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid per-key hydrate found no blob at enumerated key. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridPerKeyHydrateEmpty(
        ILogger logger,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6052, EventName = "DataGridPerKeySnapshotRejected", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid per-key hydrate rejected by snapshot invariants. Reason={Reason}. ViewKey={ViewKey}.")]
    private static partial void LogDataGridPerKeySnapshotRejected(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason,
        string? viewKey);

    [LoggerMessage(EventId = 6053, EventName = "DataGridRegistryUnavailable", Level = LogLevel.Information,
        Message = "{DiagnosticId}: DataGrid hydrate — registry unavailable (null), out-of-scope pruning skipped. Reason={Reason}.")]
    private static partial void LogDataGridRegistryUnavailable(ILogger logger, string? diagnosticId, string? reason);

    [LoggerMessage(EventId = 6054, EventName = "DataGridRegistryEnumerationFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Registry enumeration failed during DataGrid hydrate — out-of-scope pruning abandoned for this pass. Reason={Reason}.")]
    private static partial void LogDataGridRegistryEnumerationFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6055, EventName = "LoadedPageEvicted", Level = LogLevel.Information,
        Message = "LoadedPageState eviction — MaxCachedPages={Cap} reached; evicted (viewKey={ViewKey}, skip={Skip})")]
    private static partial void LogLoadedPageEvicted(ILogger logger, int cap, string? viewKey, int skip);

    [LoggerMessage(EventId = 6056, EventName = "DensityHydrationEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Density hydration found no stored value — bootstrap defaults apply until the viewport watcher emits. Reason={Reason}.")]
    private static partial void LogDensityHydrationEmpty(ILogger logger, string? diagnosticId, string? reason);

    [LoggerMessage(EventId = 6057, EventName = "DensityHydrationCancelled", Level = LogLevel.Debug,
        Message = "Density hydration cancelled — circuit disposing.")]
    private static partial void LogDensityHydrationCancelled(ILogger logger);

    [LoggerMessage(EventId = 6058, EventName = "DensityHydrationDeferred", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Density hydration deferred until browser storage is available. Reason={Reason}.")]
    private static partial void LogDensityHydrationDeferred(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6059, EventName = "DensityHydrationErrored", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Density hydration errored — bootstrap defaults apply until the viewport watcher emits. Reason={Reason}.")]
    private static partial void LogDensityHydrationErrored(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6060, EventName = "DensityPersistCancelled", Level = LogLevel.Debug,
        Message = "Density persist cancelled — circuit disposing.")]
    private static partial void LogDensityPersistCancelled(ILogger logger);

    [LoggerMessage(EventId = 6061, EventName = "DensityPersistFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Density persistence failed — swallowed (next change retries).")]
    private static partial void LogDensityPersistFailed(ILogger logger, Exception exception, string? diagnosticId);

    [LoggerMessage(EventId = 6062, EventName = "NavigationHydrationEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Navigation hydration found no stored value — feature defaults apply. Reason={Reason}.")]
    private static partial void LogNavigationHydrationEmpty(ILogger logger, string? diagnosticId, string? reason);

    [LoggerMessage(EventId = 6063, EventName = "NavigationHydrationCancelled", Level = LogLevel.Debug,
        Message = "Navigation hydration cancelled — circuit disposing.")]
    private static partial void LogNavigationHydrationCancelled(ILogger logger);

    [LoggerMessage(EventId = 6064, EventName = "NavigationHydrationErrored", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Navigation hydration errored — feature defaults apply. Reason={Reason}.")]
    private static partial void LogNavigationHydrationErrored(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6065, EventName = "NavigationRoutePrunedInvalid", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Pruning stale LastActiveRoute — stored route rejected by internal-route/base-path validation. Reason={Reason}.")]
    private static partial void LogNavigationRoutePrunedInvalid(ILogger logger, string? diagnosticId, string? reason);

    [LoggerMessage(EventId = 6066, EventName = "NavigationRoutePrunedOutOfScope", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Pruning stale LastActiveRoute — bounded context '{BoundedContext}' is no longer registered. Reason={Reason}.")]
    private static partial void LogNavigationRoutePrunedOutOfScope(
        ILogger logger,
        string? diagnosticId,
        string? boundedContext,
        string? reason);

    [LoggerMessage(EventId = 6067, EventName = "NavigationRegistryEnumerationFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Registry enumeration failed during hydrate-side LastActiveRoute prune — preserving route. Reason={Reason}.")]
    private static partial void LogNavigationRegistryEnumerationFailed(
        ILogger logger,
        Exception exception,
        string? diagnosticId,
        string? reason);

    [LoggerMessage(EventId = 6068, EventName = "NavigationPersistCancelled", Level = LogLevel.Debug,
        Message = "Navigation persist cancelled — circuit disposing.")]
    private static partial void LogNavigationPersistCancelled(ILogger logger);

    [LoggerMessage(EventId = 6069, EventName = "NavigationPersistFailed", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Navigation persistence failed — swallowed (next toggle retries).")]
    private static partial void LogNavigationPersistFailed(ILogger logger, Exception exception, string? diagnosticId);

    [LoggerMessage(EventId = 6070, EventName = "ScopeReadinessStorageReadyDispatched", Level = LogLevel.Debug,
        Message = "StorageReadyAction dispatched for first-time scope flip. CorrelationId={CorrelationId}.")]
    private static partial void LogScopeReadinessStorageReadyDispatched(ILogger logger, string? correlationId);

    [LoggerMessage(EventId = 6071, EventName = "ThemeHydrationEmpty", Level = LogLevel.Information,
        Message = "{DiagnosticId}: Theme hydration found no stored value — feature defaults apply.")]
    private static partial void LogThemeHydrationEmpty(ILogger logger, string? diagnosticId);

    [LoggerMessage(EventId = 6072, EventName = "ThemeHydrationCancelled", Level = LogLevel.Debug,
        Message = "Theme hydration cancelled — circuit disposing.")]
    private static partial void LogThemeHydrationCancelled(ILogger logger);

    /// <summary>
    /// Returns <paramref name="value"/> unchanged when it is already bounded and free of characters
    /// that could forge a log line, and otherwise a stable SHA-256 digest. <see langword="null"/>
    /// flows through so a migrated event renders exactly as its direct-call predecessor did.
    /// </summary>
    private static string? Bounded(string? value)
        => value is null || (value.Length <= MaxLoggedCharacters && !ContainsLineForgingCharacter(value))
            ? value
            : Digest(value);

    /// <summary>
    /// Reports whether an adopter-supplied value carries a character that a log sink, terminal, or
    /// log viewer may treat as a line break or may render invisibly.
    /// </summary>
    /// <remarks>
    /// <c>char.IsControl</c> alone is not sufficient: U+2028 LINE SEPARATOR and U+2029 PARAGRAPH
    /// SEPARATOR terminate a line in JSON/JavaScript-based sinks and in several viewers, and Unicode
    /// format characters (category <see cref="UnicodeCategory.Format"/> — for example the bidi
    /// overrides U+202A-U+202E and isolates U+2066-U+2069, or U+200B ZERO WIDTH SPACE) render as
    /// nothing while reordering or hiding the text around them. All three classes are treated the
    /// same way as a control character: the value collapses to a digest instead of being logged
    /// verbatim. The category lookup takes the <see cref="string"/> overload so an astral format
    /// character expressed as a surrogate pair is classified from the whole code point.
    /// </remarks>
    private static bool ContainsLineForgingCharacter(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (char.IsControl(value[index]))
            {
                return true;
            }

            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            if (category is UnicodeCategory.Format
                or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator)
            {
                return true;
            }
        }

        return false;
    }

    private static string? ExceptionTypeName(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Type type = exception.GetType();
        return Bounded(type.FullName ?? type.Name);
    }

    private static string Digest(string value)
    {
        int originalCharacterCount = value.Length;
        bool truncated = originalCharacterCount > MaxDigestCharacters;
        ReadOnlySpan<char> characters = truncated
            ? value.AsSpan(0, MaxDigestCharacters)
            : value.AsSpan();
        byte[] bytes = GC.AllocateUninitializedArray<byte>(Encoding.UTF8.GetByteCount(characters));
        byte[]? hash = null;
        try
        {
            _ = Encoding.UTF8.GetBytes(characters, bytes);
            hash = SHA256.HashData(bytes);
            string digest = "sha256:" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
            return string.Create(CultureInfo.InvariantCulture, $"{digest}:len:{originalCharacterCount}");
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
}
