using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

internal static partial class FrontComposerWarningLog
{
    /// <summary>Emits the <c>BadgeCatalogEnumerationFailed</c> residual warning-and-above event.</summary>
    public static void BadgeCatalogEnumerationFailed(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogBadgeCatalogEnumerationFailed(
            logger,
            Category(diagnosticId),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>BadgeReaderFailed</c> residual warning-and-above event.</summary>
    public static void BadgeReaderFailed(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogBadgeReaderFailed(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>BadgeNegativeCount</c> residual warning-and-above event.</summary>
    public static void BadgeNegativeCount(
        ILogger? logger,
        string diagnosticId,
        int newCount,
        object? projectionType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogBadgeNegativeCount(
            logger,
            Category(diagnosticId),
            newCount,
            Digest(projectionType));
    }

    /// <summary>Emits the <c>BadgeNotifierFailed</c> residual warning-and-above event.</summary>
    public static void BadgeNotifierFailed(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogBadgeNotifierFailed(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>LayoutUnknownViewportTier</c> residual warning-and-above event.</summary>
    public static void LayoutUnknownViewportTier(
        ILogger? logger,
        int tier)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLayoutUnknownViewportTier(
            logger,
            tier);
    }

    /// <summary>Emits the <c>LayoutSubscribeFailed</c> residual warning-and-above event.</summary>
    public static void LayoutSubscribeFailed(
        ILogger? logger,
        object? defaultTier,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLayoutSubscribeFailed(
            logger,
            Category(defaultTier),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>FieldSlotMissingParameters</c> residual warning-and-above event.</summary>
    public static void FieldSlotMissingParameters(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? fieldType,
        bool parentNull,
        bool fieldNull,
        bool renderContextNull)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogFieldSlotMissingParameters(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(fieldType),
            parentNull,
            fieldNull,
            renderContextNull);
    }

    /// <summary>Emits the <c>FieldSlotTypeMismatch</c> residual warning-and-above event.</summary>
    public static void FieldSlotTypeMismatch(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? field,
        object? descriptorFieldType,
        object? hostFieldType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogFieldSlotTypeMismatch(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(field),
            Digest(descriptorFieldType),
            Digest(hostFieldType));
    }

    /// <summary>Emits the <c>FieldSlotRenderFailed</c> residual warning-and-above event.</summary>
    public static void FieldSlotRenderFailed(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? componentType,
        object? role,
        object? field,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogFieldSlotRenderFailed(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(componentType),
            Category(role),
            Digest(field),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ProjectionSubtitleSubscribeFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionSubtitleSubscribeFailed(
        ILogger? logger,
        object? projectionType,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSubtitleSubscribeFailed(
            logger,
            Digest(projectionType),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ProjectionSubtitleDisposeFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionSubtitleDisposeFailed(
        ILogger? logger,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSubtitleDisposeFailed(
            logger,
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ProjectionTemplateRenderFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionTemplateRenderFailed(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? componentType,
        object? role,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionTemplateRenderFailed(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(componentType),
            Category(role),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ProjectionViewOverrideRenderFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideRenderFailed(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? componentType,
        object? role,
        object? exceptionCategory,
        object? tenantId,
        object? userId,
        int consecutiveFailures,
        bool circuitOpen)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionViewOverrideRenderFailed(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(componentType),
            Category(role),
            Category(exceptionCategory),
            Digest(tenantId),
            Digest(userId),
            consecutiveFailures,
            circuitOpen);
    }

    /// <summary>Emits the <c>BootstrapValidationFailed</c> residual warning-and-above event.</summary>
    public static void BootstrapValidationFailed(
        ILogger? logger,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogBootstrapValidationFailed(
            logger,
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>ProblemDetailsContentLengthExceeded</c> residual warning-and-above event.</summary>
    public static void ProblemDetailsContentLengthExceeded(
        ILogger? logger,
        int maxBytes,
        object? contentType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProblemDetailsContentLengthExceeded(
            logger,
            maxBytes,
            Category(contentType));
    }

    /// <summary>Emits the <c>ProblemDetailsReadExceeded</c> residual warning-and-above event.</summary>
    public static void ProblemDetailsReadExceeded(
        ILogger? logger,
        int maxBytes,
        object? contentType)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProblemDetailsReadExceeded(
            logger,
            maxBytes,
            Category(contentType));
    }

    /// <summary>Emits the <c>ProblemDetailsParseFailed</c> residual warning-and-above event.</summary>
    public static void ProblemDetailsParseFailed(
        ILogger? logger,
        object? contentType,
        object? failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProblemDetailsParseFailed(
            logger,
            Category(contentType),
            Category(failureCategory));
    }

    /// <summary>Emits the <c>LocalStorageDeserializeFailed</c> residual warning-and-above event.</summary>
    public static void LocalStorageDeserializeFailed(
        ILogger? logger,
        object? key,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLocalStorageDeserializeFailed(
            logger,
            Digest(key),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>LocalStorageDrainWriteFailed</c> residual warning-and-above event.</summary>
    public static void LocalStorageDrainWriteFailed(
        ILogger? logger,
        object? key,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLocalStorageDrainWriteFailed(
            logger,
            Digest(key),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>RegistryRegistrationSkipped</c> residual warning-and-above event.</summary>
    public static void RegistryRegistrationSkipped(
        ILogger? logger,
        object? registrationType,
        bool hasManifest,
        bool hasRegisterMethod)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogRegistryRegistrationSkipped(
            logger,
            Digest(registrationType),
            hasManifest,
            hasRegisterMethod);
    }

    /// <summary>Emits the <c>RegistryPolicyConflict</c> residual warning-and-above event.</summary>
    public static void RegistryPolicyConflict(
        ILogger? logger,
        object? commandType,
        object? priorPolicy,
        object? incomingPolicy)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogRegistryPolicyConflict(
            logger,
            Digest(commandType),
            Digest(priorPolicy),
            Digest(incomingPolicy));
    }

    /// <summary>Emits the <c>RegistryPolicyOverwritten</c> residual warning-and-above event.</summary>
    public static void RegistryPolicyOverwritten(
        ILogger? logger,
        object? commandType,
        object? priorPolicy,
        object? incomingPolicy)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogRegistryPolicyOverwritten(
            logger,
            Digest(commandType),
            Digest(priorPolicy),
            Digest(incomingPolicy));
    }

    /// <summary>Emits the <c>CustomizationValidationFailed</c> residual warning-and-above event.</summary>
    public static void CustomizationValidationFailed(
        ILogger? logger,
        string diagnosticId,
        int rejectionCount,
        object? message)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogCustomizationValidationFailed(
            logger,
            Category(diagnosticId),
            rejectionCount,
            Digest(message));
    }

    /// <summary>Emits the <c>DiagnosticSinkPublished</c> residual warning-and-above event.</summary>
    public static void DiagnosticSinkPublished(
        ILogger? logger,
        object? code,
        object? category,
        object? message)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogDiagnosticSinkPublished(
            logger,
            Category(code),
            Category(category),
            Digest(message));
    }

    /// <summary>Emits the <c>ProjectionSlotInvalidContractVersion</c> residual warning-and-above event.</summary>
    public static void ProjectionSlotInvalidContractVersion(
        ILogger? logger,
        object? projectionType,
        object? field,
        int contractVersion)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSlotInvalidContractVersion(
            logger,
            Digest(projectionType),
            Digest(field),
            contractVersion);
    }

    /// <summary>Emits the <c>ProjectionSlotIncompatibleContractVersion</c> residual warning-and-above event.</summary>
    public static void ProjectionSlotIncompatibleContractVersion(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? field,
        object? decision,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSlotIncompatibleContractVersion(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Digest(field),
            Category(decision),
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild);
    }

    /// <summary>Emits the <c>ProjectionSlotInvalidComponent</c> residual warning-and-above event.</summary>
    public static void ProjectionSlotInvalidComponent(
        ILogger? logger,
        object? projectionType,
        object? field,
        object? expectedProjectionType,
        object? expectedFieldType,
        object? componentType,
        object? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSlotInvalidComponent(
            logger,
            Digest(projectionType),
            Digest(field),
            Digest(expectedProjectionType),
            Digest(expectedFieldType),
            Digest(componentType),
            Category(reason));
    }

    /// <summary>Emits the <c>ProjectionSlotDuplicate</c> residual warning-and-above event.</summary>
    public static void ProjectionSlotDuplicate(
        ILogger? logger,
        object? projectionType,
        object? role,
        object? field,
        object? existingComponent,
        object? newComponent)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionSlotDuplicate(
            logger,
            Digest(projectionType),
            Category(role),
            Digest(field),
            Digest(existingComponent),
            Digest(newComponent));
    }

    /// <summary>Emits the <c>ProjectionTemplateIncompatibleContractVersion</c> residual warning-and-above event.</summary>
    public static void ProjectionTemplateIncompatibleContractVersion(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? role,
        object? decision,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionTemplateIncompatibleContractVersion(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Category(role),
            Category(decision),
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild);
    }

    /// <summary>Emits the <c>ProjectionTemplateDuplicate</c> residual warning-and-above event.</summary>
    public static void ProjectionTemplateDuplicate(
        ILogger? logger,
        object? projectionType,
        object? role,
        object? existingTemplate,
        object? newTemplate)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionTemplateDuplicate(
            logger,
            Digest(projectionType),
            Category(role),
            Digest(existingTemplate),
            Digest(newTemplate));
    }

    /// <summary>Emits the <c>ProjectionViewOverrideNullSource</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideNullSource(
        ILogger? logger,
        int index)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionViewOverrideNullSource(
            logger,
            index);
    }

    /// <summary>Emits the <c>ProjectionViewOverrideInvalidContractVersion</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideInvalidContractVersion(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? role,
        int contractVersion,
        object? source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionViewOverrideInvalidContractVersion(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Category(role),
            contractVersion,
            Digest(source));
    }

    /// <summary>Emits the <c>ProjectionViewOverrideIncompatibleContractVersion</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideIncompatibleContractVersion(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? role,
        object? decision,
        int expectedMajor,
        int expectedMinor,
        int expectedBuild,
        int actualMajor,
        int actualMinor,
        int actualBuild,
        object? source)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionViewOverrideIncompatibleContractVersion(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Category(role),
            Category(decision),
            expectedMajor,
            expectedMinor,
            expectedBuild,
            actualMajor,
            actualMinor,
            actualBuild,
            Digest(source));
    }

    /// <summary>Emits the <c>ProjectionViewOverrideInvalidComponent</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideInvalidComponent(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? role,
        object? componentType,
        object? source,
        object? reason)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionViewOverrideInvalidComponent(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Category(role),
            Digest(componentType),
            Digest(source),
            Category(reason));
    }

    /// <summary>Emits the <c>ProjectionViewOverrideDuplicate</c> residual warning-and-above event.</summary>
    public static void ProjectionViewOverrideDuplicate(
        ILogger? logger,
        string diagnosticId,
        object? projectionType,
        object? role,
        object? componentA,
        object? sourceA,
        object? componentB,
        object? sourceB)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogProjectionViewOverrideDuplicate(
            logger,
            Category(diagnosticId),
            Digest(projectionType),
            Category(role),
            Digest(componentA),
            Digest(sourceA),
            Digest(componentB),
            Digest(sourceB));
    }

    /// <summary>Emits the <c>StubLifecycleCallbackFailed</c> residual warning-and-above event.</summary>
    public static void StubLifecycleCallbackFailed(
        ILogger? logger,
        object? messageId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogStubLifecycleCallbackFailed(
            logger,
            Digest(messageId),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>StubBackgroundTaskFaulted</c> residual warning-and-above event.</summary>
    public static void StubBackgroundTaskFaulted(
        ILogger? logger,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Error))
        {
            return;
        }

        LogStubBackgroundTaskFaulted(
            logger,
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ShortcutHandlerFailed</c> residual warning-and-above event.</summary>
    public static void ShortcutHandlerFailed(
        ILogger? logger,
        string diagnosticId,
        object? binding,
        object? descriptionKey,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogShortcutHandlerFailed(
            logger,
            Category(diagnosticId),
            Digest(binding),
            Digest(descriptionKey),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>CapabilityPersistFailed</c> residual warning-and-above event.</summary>
    public static void CapabilityPersistFailed(
        ILogger? logger,
        string diagnosticId,
        object? capability,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogCapabilityPersistFailed(
            logger,
            Category(diagnosticId),
            Digest(capability),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>CapabilityHydrateFailed</c> residual warning-and-above event.</summary>
    public static void CapabilityHydrateFailed(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogCapabilityHydrateFailed(
            logger,
            Category(diagnosticId),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>BadgeSnapshotFailed</c> residual warning-and-above event.</summary>
    public static void BadgeSnapshotFailed(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogBadgeSnapshotFailed(
            logger,
            Category(diagnosticId),
            ExceptionType(exception),
            Digest(exception.Message));
    }

    /// <summary>Emits the <c>PaletteShortcutServiceMissing</c> residual warning-and-above event.</summary>
    public static void PaletteShortcutServiceMissing(
        ILogger? logger,
        string diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteShortcutServiceMissing(
            logger,
            Category(diagnosticId));
    }

    /// <summary>Emits the <c>PaletteRegistryEnumerationFailed</c> residual warning-and-above event.</summary>
    public static void PaletteRegistryEnumerationFailed(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteRegistryEnumerationFailed(
            logger,
            Category(diagnosticId),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>PaletteManifestScoringFailed</c> residual warning-and-above event.</summary>
    public static void PaletteManifestScoringFailed(
        ILogger? logger,
        string diagnosticId,
        object? boundedContext,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteManifestScoringFailed(
            logger,
            Category(diagnosticId),
            Digest(boundedContext),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>PaletteNavigationServiceMissing</c> residual warning-and-above event.</summary>
    public static void PaletteNavigationServiceMissing(
        ILogger? logger,
        string diagnosticId)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteNavigationServiceMissing(
            logger,
            Category(diagnosticId));
    }

    /// <summary>Emits the <c>PaletteNavigationRefused</c> residual warning-and-above event.</summary>
    public static void PaletteNavigationRefused(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteNavigationRefused(
            logger,
            Category(diagnosticId),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>PaletteOpenRegistryEnumerationFailed</c> residual warning-and-above event.</summary>
    public static void PaletteOpenRegistryEnumerationFailed(
        ILogger? logger,
        string diagnosticId,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteOpenRegistryEnumerationFailed(
            logger,
            Category(diagnosticId),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>PaletteOpenManifestFailed</c> residual warning-and-above event.</summary>
    public static void PaletteOpenManifestFailed(
        ILogger? logger,
        string diagnosticId,
        object? boundedContext,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteOpenManifestFailed(
            logger,
            Category(diagnosticId),
            Digest(boundedContext),
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>PaletteAuthorizationEvaluatorMissing</c> residual warning-and-above event.</summary>
    public static void PaletteAuthorizationEvaluatorMissing(
        ILogger? logger)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogPaletteAuthorizationEvaluatorMissing(
            logger);
    }

    /// <summary>Emits the <c>ProjectionLoadSchemaFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionLoadSchemaFailed(
        ILogger? logger,
        object? projectionType,
        object? failureCategory)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionLoadSchemaFailed(
            logger,
            Digest(projectionType),
            Category(failureCategory));
    }

    /// <summary>Emits the <c>ProjectionLoadTerminalDispatchFailed</c> residual warning-and-above event.</summary>
    public static void ProjectionLoadTerminalDispatchFailed(
        ILogger? logger,
        object? viewKey,
        int skip,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogProjectionLoadTerminalDispatchFailed(
            logger,
            Digest(viewKey),
            skip,
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>LoadedPageNullItems</c> residual warning-and-above event.</summary>
    public static void LoadedPageNullItems(
        ILogger? logger,
        object? viewKey,
        int skip)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogLoadedPageNullItems(
            logger,
            Digest(viewKey),
            skip);
    }

    /// <summary>Emits the <c>ThemeHydrationFailed</c> residual warning-and-above event.</summary>
    public static void ThemeHydrationFailed(
        ILogger? logger,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogThemeHydrationFailed(
            logger,
            ExceptionType(exception));
    }

    /// <summary>Emits the <c>ThemePersistenceFailed</c> residual warning-and-above event.</summary>
    public static void ThemePersistenceFailed(
        ILogger? logger,
        Exception exception)
    {
        if (logger is null || !logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        LogThemePersistenceFailed(
            logger,
            ExceptionType(exception));
    }

    private static string Category(object? value)
    {
        string? text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Absent";
        }

        text = text.Trim();
        if (text.Length > 64 || text.Any(static character => !char.IsLetterOrDigit(character)
            && character is not '.' and not '_' and not '-'))
        {
            return Digest(text);
        }

        return text;
    }

    private static string Digest(object? value)
    {
        string? text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "absent";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text.Trim());
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

    private static string ExceptionType(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.GetType().Name;
    }

    [LoggerMessage(EventId = 5800, EventName = "BadgeCatalogEnumerationFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Badge catalog enumeration threw; initial fetch was skipped. ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogBadgeCatalogEnumerationFailed(ILogger logger, string diagnosticId, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5801, EventName = "BadgeReaderFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Badge reader threw. ProjectionTypeDigest={ProjectionTypeDigest} ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogBadgeReaderFailed(ILogger logger, string diagnosticId, string projectionTypeDigest, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5802, EventName = "BadgeNegativeCount", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Badge reader returned negative count {NewCount}; emission was dropped. ProjectionTypeDigest={ProjectionTypeDigest}.")]
    private static partial void LogBadgeNegativeCount(ILogger logger, string diagnosticId, int newCount, string projectionTypeDigest);

    [LoggerMessage(EventId = 5803, EventName = "BadgeNotifierFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Badge notifier handler threw. ProjectionTypeDigest={ProjectionTypeDigest} ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogBadgeNotifierFailed(ILogger logger, string diagnosticId, string projectionTypeDigest, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5804, EventName = "LayoutUnknownViewportTier", Level = LogLevel.Warning,
        Message = "FcLayoutBreakpointWatcher received unknown viewport tier {Tier}; ignoring.")]
    private static partial void LogLayoutUnknownViewportTier(ILogger logger, int tier);

    [LoggerMessage(EventId = 5805, EventName = "LayoutSubscribeFailed", Level = LogLevel.Warning,
        Message = "FcLayoutBreakpointWatcher subscribe failed; viewport stays at {DefaultTier}. ExceptionType={ExceptionType}.")]
    private static partial void LogLayoutSubscribeFailed(ILogger logger, string defaultTier, string exceptionType);

    [LoggerMessage(EventId = 5806, EventName = "FieldSlotMissingParameters", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: FcFieldSlotHost received null required parameters. ProjectionTypeDigest={ProjectionTypeDigest} FieldTypeDigest={FieldTypeDigest} ParentNull={ParentNull} FieldNull={FieldNull} RenderContextNull={RenderContextNull}. Field renders nothing.")]
    private static partial void LogFieldSlotMissingParameters(ILogger logger, string diagnosticId, string projectionTypeDigest, string fieldTypeDigest, bool parentNull, bool fieldNull, bool renderContextNull);

    [LoggerMessage(EventId = 5807, EventName = "FieldSlotTypeMismatch", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Level 3 slot descriptor field type does not match the host field type. ProjectionTypeDigest={ProjectionTypeDigest} FieldDigest={FieldDigest} DescriptorFieldTypeDigest={DescriptorFieldTypeDigest} HostFieldTypeDigest={HostFieldTypeDigest}. Descriptor ignored; default rendering used.")]
    private static partial void LogFieldSlotTypeMismatch(ILogger logger, string diagnosticId, string projectionTypeDigest, string fieldDigest, string descriptorFieldTypeDigest, string hostFieldTypeDigest);

    [LoggerMessage(EventId = 5808, EventName = "FieldSlotRenderFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Level 3 slot render fault isolated. ProjectionTypeDigest={ProjectionTypeDigest} ComponentTypeDigest={ComponentTypeDigest} RoleCategory={RoleCategory} FieldDigest={FieldDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogFieldSlotRenderFailed(ILogger logger, string diagnosticId, string projectionTypeDigest, string componentTypeDigest, string roleCategory, string fieldDigest, string exceptionType);

    [LoggerMessage(EventId = 5809, EventName = "ProjectionSubtitleSubscribeFailed", Level = LogLevel.Warning,
        Message = "FcProjectionSubtitle failed to subscribe to badge count changes; falling back to cascading count. ProjectionTypeDigest={ProjectionTypeDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogProjectionSubtitleSubscribeFailed(ILogger logger, string projectionTypeDigest, string exceptionType);

    [LoggerMessage(EventId = 5810, EventName = "ProjectionSubtitleDisposeFailed", Level = LogLevel.Warning,
        Message = "FcProjectionSubtitle disposal threw while unsubscribing badge count changes. ExceptionType={ExceptionType}.")]
    private static partial void LogProjectionSubtitleDisposeFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5811, EventName = "ProjectionTemplateRenderFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Level 2 template render fault isolated. ProjectionTypeDigest={ProjectionTypeDigest} ComponentTypeDigest={ComponentTypeDigest} RoleCategory={RoleCategory} ExceptionType={ExceptionType}.")]
    private static partial void LogProjectionTemplateRenderFailed(ILogger logger, string diagnosticId, string projectionTypeDigest, string componentTypeDigest, string roleCategory, string exceptionType);

    [LoggerMessage(EventId = 5812, EventName = "ProjectionViewOverrideRenderFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Level 4 view replacement render fault isolated. ProjectionTypeDigest={ProjectionTypeDigest} ComponentTypeDigest={ComponentTypeDigest} RoleCategory={RoleCategory} ExceptionCategory={ExceptionCategory} TenantDigest={TenantDigest} UserDigest={UserDigest} ConsecutiveFailures={ConsecutiveFailures} CircuitOpen={CircuitOpen}.")]
    private static partial void LogProjectionViewOverrideRenderFailed(ILogger logger, string diagnosticId, string projectionTypeDigest, string componentTypeDigest, string roleCategory, string exceptionCategory, string tenantDigest, string userDigest, int consecutiveFailures, bool circuitOpen);

    [LoggerMessage(EventId = 5813, EventName = "BootstrapValidationFailed", Level = LogLevel.Error,
        Message = "FrontComposer bootstrap validation failed. ExceptionType={ExceptionType} MessageDigest={MessageDigest}.")]
    private static partial void LogBootstrapValidationFailed(ILogger logger, string exceptionType, string messageDigest);

    [LoggerMessage(EventId = 5814, EventName = "ProblemDetailsContentLengthExceeded", Level = LogLevel.Warning,
        Message = "EventStore ProblemDetails body exceeded {MaxBytes} bytes by Content-Length; falling back to empty payload. ContentTypeCategory={ContentTypeCategory}.")]
    private static partial void LogProblemDetailsContentLengthExceeded(ILogger logger, int maxBytes, string contentTypeCategory);

    [LoggerMessage(EventId = 5815, EventName = "ProblemDetailsReadExceeded", Level = LogLevel.Warning,
        Message = "EventStore ProblemDetails body exceeded {MaxBytes} bytes while reading; falling back to empty payload. ContentTypeCategory={ContentTypeCategory}.")]
    private static partial void LogProblemDetailsReadExceeded(ILogger logger, int maxBytes, string contentTypeCategory);

    [LoggerMessage(EventId = 5816, EventName = "ProblemDetailsParseFailed", Level = LogLevel.Warning,
        Message = "EventStore ProblemDetails parse failed; falling back to empty payload. ContentTypeCategory={ContentTypeCategory} FailureCategory={FailureCategory}.")]
    private static partial void LogProblemDetailsParseFailed(ILogger logger, string contentTypeCategory, string failureCategory);

    [LoggerMessage(EventId = 5817, EventName = "LocalStorageDeserializeFailed", Level = LogLevel.Warning,
        Message = "LocalStorageService failed to deserialize a value. KeyDigest={KeyDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogLocalStorageDeserializeFailed(ILogger logger, string keyDigest, string exceptionType);

    [LoggerMessage(EventId = 5818, EventName = "LocalStorageDrainWriteFailed", Level = LogLevel.Warning,
        Message = "LocalStorageService drain write failed. KeyDigest={KeyDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogLocalStorageDrainWriteFailed(ILogger logger, string keyDigest, string exceptionType);

    [LoggerMessage(EventId = 5819, EventName = "RegistryRegistrationSkipped", Level = LogLevel.Warning,
        Message = "Skipping a registration type because required manifest members were absent. RegistrationTypeDigest={RegistrationTypeDigest} HasManifest={HasManifest} HasRegisterMethod={HasRegisterMethod}.")]
    private static partial void LogRegistryRegistrationSkipped(ILogger logger, string registrationTypeDigest, bool hasManifest, bool hasRegisterMethod);

    [LoggerMessage(EventId = 5820, EventName = "RegistryPolicyConflict", Level = LogLevel.Warning,
        Message = "FrontComposer registry policy lookup resolved multiple policies. CommandTypeDigest={CommandTypeDigest} PriorPolicyDigest={PriorPolicyDigest} IncomingPolicyDigest={IncomingPolicyDigest}. Last-write-wins remains active.")]
    private static partial void LogRegistryPolicyConflict(ILogger logger, string commandTypeDigest, string priorPolicyDigest, string incomingPolicyDigest);

    [LoggerMessage(EventId = 5821, EventName = "RegistryPolicyOverwritten", Level = LogLevel.Warning,
        Message = "FrontComposer registry merge overwrote a command policy. CommandTypeDigest={CommandTypeDigest} PriorPolicyDigest={PriorPolicyDigest} IncomingPolicyDigest={IncomingPolicyDigest}. Last-write-wins remains active.")]
    private static partial void LogRegistryPolicyOverwritten(ILogger logger, string commandTypeDigest, string priorPolicyDigest, string incomingPolicyDigest);

    [LoggerMessage(EventId = 5822, EventName = "CustomizationValidationFailed", Level = LogLevel.Error,
        Message = "{DiagnosticId}: Customization contract validation failed. RejectionCount={RejectionCount} MessageDigest={MessageDigest}.")]
    private static partial void LogCustomizationValidationFailed(ILogger logger, string diagnosticId, int rejectionCount, string messageDigest);

    [LoggerMessage(EventId = 5823, EventName = "DiagnosticSinkPublished", Level = LogLevel.Warning,
        Message = "A diagnostic was published. Code={Code} Category={Category} MessageDigest={MessageDigest}.")]
    private static partial void LogDiagnosticSinkPublished(ILogger logger, string code, string category, string messageDigest);

    [LoggerMessage(EventId = 5824, EventName = "ProjectionSlotInvalidContractVersion", Level = LogLevel.Warning,
        Message = "HFC1041: Level 3 slot descriptor declares an invalid contract version. ProjectionTypeDigest={ProjectionTypeDigest} FieldDigest={FieldDigest} ContractVersion={ContractVersion}. Descriptor ignored.")]
    private static partial void LogProjectionSlotInvalidContractVersion(ILogger logger, string projectionTypeDigest, string fieldDigest, int contractVersion);

    [LoggerMessage(EventId = 5825, EventName = "ProjectionSlotIncompatibleContractVersion", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Level 3 slot descriptor has an incompatible contract version. ProjectionTypeDigest={ProjectionTypeDigest} FieldDigest={FieldDigest} Decision={Decision} Expected={ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} Actual={ActualMajor}.{ActualMinor}.{ActualBuild}. Descriptor ignored.")]
    private static partial void LogProjectionSlotIncompatibleContractVersion(ILogger logger, string diagnosticId, string projectionTypeDigest, string fieldDigest, string decision, int expectedMajor, int expectedMinor, int expectedBuild, int actualMajor, int actualMinor, int actualBuild);

    [LoggerMessage(EventId = 5826, EventName = "ProjectionSlotInvalidComponent", Level = LogLevel.Warning,
        Message = "HFC1039: Invalid Level 3 slot component. ProjectionTypeDigest={ProjectionTypeDigest} FieldDigest={FieldDigest} ExpectedProjectionTypeDigest={ExpectedProjectionTypeDigest} ExpectedFieldTypeDigest={ExpectedFieldTypeDigest} ComponentTypeDigest={ComponentTypeDigest} ReasonCategory={ReasonCategory}.")]
    private static partial void LogProjectionSlotInvalidComponent(ILogger logger, string projectionTypeDigest, string fieldDigest, string expectedProjectionTypeDigest, string expectedFieldTypeDigest, string componentTypeDigest, string reasonCategory);

    [LoggerMessage(EventId = 5827, EventName = "ProjectionSlotDuplicate", Level = LogLevel.Warning,
        Message = "HFC1040: Duplicate Level 3 slot overrides registered. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} FieldDigest={FieldDigest} ExistingComponentDigest={ExistingComponentDigest} NewComponentDigest={NewComponentDigest}.")]
    private static partial void LogProjectionSlotDuplicate(ILogger logger, string projectionTypeDigest, string roleCategory, string fieldDigest, string existingComponentDigest, string newComponentDigest);

    [LoggerMessage(EventId = 5828, EventName = "ProjectionTemplateIncompatibleContractVersion", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Projection template has an incompatible contract version. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} Decision={Decision} Expected={ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} Actual={ActualMajor}.{ActualMinor}.{ActualBuild}. Descriptor ignored.")]
    private static partial void LogProjectionTemplateIncompatibleContractVersion(ILogger logger, string diagnosticId, string projectionTypeDigest, string roleCategory, string decision, int expectedMajor, int expectedMinor, int expectedBuild, int actualMajor, int actualMinor, int actualBuild);

    [LoggerMessage(EventId = 5829, EventName = "ProjectionTemplateDuplicate", Level = LogLevel.Warning,
        Message = "Duplicate projection templates were registered; both were ignored. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} ExistingTemplateDigest={ExistingTemplateDigest} NewTemplateDigest={NewTemplateDigest}.")]
    private static partial void LogProjectionTemplateDuplicate(ILogger logger, string projectionTypeDigest, string roleCategory, string existingTemplateDigest, string newTemplateDigest);

    [LoggerMessage(EventId = 5830, EventName = "ProjectionViewOverrideNullSource", Level = LogLevel.Warning,
        Message = "Null ProjectionViewOverrideDescriptorSource at index {Index} was skipped during registry construction.")]
    private static partial void LogProjectionViewOverrideNullSource(ILogger logger, int index);

    [LoggerMessage(EventId = 5831, EventName = "ProjectionViewOverrideInvalidContractVersion", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Invalid Level 4 view override contract version. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} ContractVersion={ContractVersion} SourceDigest={SourceDigest}.")]
    private static partial void LogProjectionViewOverrideInvalidContractVersion(ILogger logger, string diagnosticId, string projectionTypeDigest, string roleCategory, int contractVersion, string sourceDigest);

    [LoggerMessage(EventId = 5832, EventName = "ProjectionViewOverrideIncompatibleContractVersion", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Incompatible Level 4 view override contract version. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} Decision={Decision} Expected={ExpectedMajor}.{ExpectedMinor}.{ExpectedBuild} Actual={ActualMajor}.{ActualMinor}.{ActualBuild} SourceDigest={SourceDigest}.")]
    private static partial void LogProjectionViewOverrideIncompatibleContractVersion(ILogger logger, string diagnosticId, string projectionTypeDigest, string roleCategory, string decision, int expectedMajor, int expectedMinor, int expectedBuild, int actualMajor, int actualMinor, int actualBuild, string sourceDigest);

    [LoggerMessage(EventId = 5833, EventName = "ProjectionViewOverrideInvalidComponent", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Invalid Level 4 view override component. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} ComponentTypeDigest={ComponentTypeDigest} SourceDigest={SourceDigest} ReasonCategory={ReasonCategory}.")]
    private static partial void LogProjectionViewOverrideInvalidComponent(ILogger logger, string diagnosticId, string projectionTypeDigest, string roleCategory, string componentTypeDigest, string sourceDigest, string reasonCategory);

    [LoggerMessage(EventId = 5834, EventName = "ProjectionViewOverrideDuplicate", Level = LogLevel.Error,
        Message = "{DiagnosticId}: Duplicate Level 4 view overrides registered. ProjectionTypeDigest={ProjectionTypeDigest} RoleCategory={RoleCategory} ComponentADigest={ComponentADigest} SourceADigest={SourceADigest} ComponentBDigest={ComponentBDigest} SourceBDigest={SourceBDigest}.")]
    private static partial void LogProjectionViewOverrideDuplicate(ILogger logger, string diagnosticId, string projectionTypeDigest, string roleCategory, string componentADigest, string sourceADigest, string componentBDigest, string sourceBDigest);

    [LoggerMessage(EventId = 5835, EventName = "StubLifecycleCallbackFailed", Level = LogLevel.Error,
        Message = "Stub command lifecycle callback threw; Syncing and Confirmed notifications were skipped. MessageIdDigest={MessageIdDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogStubLifecycleCallbackFailed(ILogger logger, string messageIdDigest, string exceptionType);

    [LoggerMessage(EventId = 5836, EventName = "StubBackgroundTaskFaulted", Level = LogLevel.Error,
        Message = "StubCommandService background task faulted. ExceptionType={ExceptionType}.")]
    private static partial void LogStubBackgroundTaskFaulted(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5837, EventName = "ShortcutHandlerFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Registered shortcut handler threw. BindingDigest={BindingDigest} DescriptionKeyDigest={DescriptionKeyDigest} ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogShortcutHandlerFailed(ILogger logger, string diagnosticId, string bindingDigest, string descriptionKeyDigest, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5838, EventName = "CapabilityPersistFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Capability-seen persistence failed. CapabilityDigest={CapabilityDigest} ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogCapabilityPersistFailed(ILogger logger, string diagnosticId, string capabilityDigest, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5839, EventName = "CapabilityHydrateFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Capability-seen hydrate failed; defaulting to empty set. ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogCapabilityHydrateFailed(ILogger logger, string diagnosticId, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5840, EventName = "BadgeSnapshotFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Badge counts snapshot threw during seed; dispatching an empty dictionary. ExceptionType={ExceptionType} ExceptionMessageDigest={ExceptionMessageDigest}.")]
    private static partial void LogBadgeSnapshotFailed(ILogger logger, string diagnosticId, string exceptionType, string exceptionMessageDigest);

    [LoggerMessage(EventId = 5841, EventName = "PaletteShortcutServiceMissing", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Palette shortcuts query bypassed scoring but the shortcut service is unregistered; dispatching an empty result set.")]
    private static partial void LogPaletteShortcutServiceMissing(ILogger logger, string diagnosticId);

    [LoggerMessage(EventId = 5842, EventName = "PaletteRegistryEnumerationFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Registry enumeration threw during palette scoring; dispatching an empty result set. ExceptionType={ExceptionType}.")]
    private static partial void LogPaletteRegistryEnumerationFailed(ILogger logger, string diagnosticId, string exceptionType);

    [LoggerMessage(EventId = 5843, EventName = "PaletteManifestScoringFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: A manifest threw during palette scoring; skipping it and keeping other results. BoundedContextDigest={BoundedContextDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogPaletteManifestScoringFailed(ILogger logger, string diagnosticId, string boundedContextDigest, string exceptionType);

    [LoggerMessage(EventId = 5844, EventName = "PaletteNavigationServiceMissing", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Palette activation resolved no NavigationManager; navigation was dropped.")]
    private static partial void LogPaletteNavigationServiceMissing(ILogger logger, string diagnosticId);

    [LoggerMessage(EventId = 5845, EventName = "PaletteNavigationRefused", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: NavigationManager.NavigateTo refused the target URL. ExceptionType={ExceptionType}.")]
    private static partial void LogPaletteNavigationRefused(ILogger logger, string diagnosticId, string exceptionType);

    [LoggerMessage(EventId = 5846, EventName = "PaletteOpenRegistryEnumerationFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: Registry enumeration failed during palette open; falling back to an empty projection preview. ExceptionType={ExceptionType}.")]
    private static partial void LogPaletteOpenRegistryEnumerationFailed(ILogger logger, string diagnosticId, string exceptionType);

    [LoggerMessage(EventId = 5847, EventName = "PaletteOpenManifestFailed", Level = LogLevel.Warning,
        Message = "{DiagnosticId}: A manifest threw during palette open preview; skipping it and keeping other rows. BoundedContextDigest={BoundedContextDigest} ExceptionType={ExceptionType}.")]
    private static partial void LogPaletteOpenManifestFailed(ILogger logger, string diagnosticId, string boundedContextDigest, string exceptionType);

    [LoggerMessage(EventId = 5848, EventName = "PaletteAuthorizationEvaluatorMissing", Level = LogLevel.Warning,
        Message = "Command palette filter could not resolve ICommandAuthorizationEvaluator; protected commands will be hidden.")]
    private static partial void LogPaletteAuthorizationEvaluatorMissing(ILogger logger);

    [LoggerMessage(EventId = 5849, EventName = "ProjectionLoadSchemaFailed", Level = LogLevel.Warning,
        Message = "Projection load failed schema check. ProjectionTypeDigest={ProjectionTypeDigest} FailureCategory={FailureCategory}.")]
    private static partial void LogProjectionLoadSchemaFailed(ILogger logger, string projectionTypeDigest, string failureCategory);

    [LoggerMessage(EventId = 5850, EventName = "ProjectionLoadTerminalDispatchFailed", Level = LogLevel.Warning,
        Message = "Defensive terminal dispatch failed during finally; completion may orphan. ViewKeyDigest={ViewKeyDigest} Skip={Skip} ExceptionType={ExceptionType}.")]
    private static partial void LogProjectionLoadTerminalDispatchFailed(ILogger logger, string viewKeyDigest, int skip, string exceptionType);

    [LoggerMessage(EventId = 5851, EventName = "LoadedPageNullItems", Level = LogLevel.Warning,
        Message = "LoadPageSucceededAction received a null Items payload. ViewKeyDigest={ViewKeyDigest} Skip={Skip}.")]
    private static partial void LogLoadedPageNullItems(ILogger logger, string viewKeyDigest, int skip);

    [LoggerMessage(EventId = 5852, EventName = "ThemeHydrationFailed", Level = LogLevel.Warning,
        Message = "Failed to hydrate theme state from storage. ExceptionType={ExceptionType}.")]
    private static partial void LogThemeHydrationFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5853, EventName = "ThemePersistenceFailed", Level = LogLevel.Warning,
        Message = "Failed to persist theme state. ExceptionType={ExceptionType}.")]
    private static partial void LogThemePersistenceFailed(ILogger logger, string exceptionType);

}

