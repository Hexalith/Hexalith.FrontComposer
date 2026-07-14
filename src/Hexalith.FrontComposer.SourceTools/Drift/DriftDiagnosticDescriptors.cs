using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal static class DriftDiagnosticDescriptors {
    internal static DiagnosticDescriptor GetDescriptor(string id, DiagnosticSeverity severity)
        => id switch {
            DriftConstants.MissingBaselineId => DiagnosticDescriptors.GeneratedUiBaselineMissing,
            DriftConstants.InvalidBaselinePathId => DiagnosticDescriptors.GeneratedUiBaselinePathInvalid,
            DriftConstants.InvalidBaselineContentId => DiagnosticDescriptors.GeneratedUiBaselineContentInvalid,
            DriftConstants.UnsupportedSchemaId => DiagnosticDescriptors.GeneratedUiBaselineSchemaUnsupported,
            DriftConstants.UnsupportedAlgorithmId => DiagnosticDescriptors.GeneratedUiBaselineAlgorithmUnsupported,
            DriftConstants.BaselineBoundsExceededId => DiagnosticDescriptors.GeneratedUiBaselineBoundsExceeded,
            DriftConstants.DuplicateOrInvariantId => DiagnosticDescriptors.GeneratedUiBaselineIdentityInvalid,
            DriftConstants.StructuralDriftId => WithSeverity(DiagnosticDescriptors.GeneratedUiStructuralDrift, severity),
            DriftConstants.MetadataDriftId => WithSeverity(DiagnosticDescriptors.GeneratedUiMetadataDrift, severity),
            DriftConstants.InvalidOptionId => DiagnosticDescriptors.GeneratedUiDriftOptionInvalid,
            DriftConstants.TruncationId => DiagnosticDescriptors.GeneratedUiDriftTruncated,
            DriftConstants.RedactionSuppressedId => DiagnosticDescriptors.GeneratedUiDriftRedactionSuppressed,
            DriftConstants.TrimAotReflectionCatalogId => DiagnosticDescriptors.TrimAotReflectionCatalogWarning,
            _ => DiagnosticDescriptors.GeneratedUiStructuralDrift,
        };

    private static DiagnosticDescriptor WithSeverity(DiagnosticDescriptor descriptor, DiagnosticSeverity severity)
        => severity == descriptor.DefaultSeverity
            ? descriptor
            : new DiagnosticDescriptor(
                descriptor.Id,
                descriptor.Title,
                descriptor.MessageFormat,
                descriptor.Category,
                severity,
                descriptor.IsEnabledByDefault,
                descriptor.Description,
                descriptor.HelpLinkUri,
                descriptor.CustomTags.ToArray());
}
