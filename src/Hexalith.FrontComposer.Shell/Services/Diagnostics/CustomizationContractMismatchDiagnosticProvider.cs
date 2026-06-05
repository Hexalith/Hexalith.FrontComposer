using System.Collections.ObjectModel;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Customization;

namespace Hexalith.FrontComposer.Shell.Services.Diagnostics;

internal sealed class CustomizationContractMismatchDiagnosticProvider(
    ICustomizationContractRejectionLog rejectionLog) : ICustomizationContractMismatchDiagnosticProvider {
    public IReadOnlyList<CustomizationDiagnostic> GetDiagnostics() {
        IReadOnlyList<CustomizationContractRejection> rejections = rejectionLog.Rejections;
        if (rejections.Count == 0) {
            return ReadOnlyCollection<CustomizationDiagnostic>.Empty;
        }

        List<CustomizationDiagnostic> diagnostics = new(capacity: rejections.Count);
        for (int i = 0; i < rejections.Count; i++) {
            diagnostics.Add(CreateDiagnostic(rejections[i]));
        }

        return diagnostics;
    }

    private static CustomizationDiagnostic CreateDiagnostic(CustomizationContractRejection rejection) {
        string expected = FormatVersion(rejection.Comparison.Expected);
        string actual = FormatVersion(rejection.Comparison.Actual);
        return CustomizationDiagnostic.Create(
            id: rejection.DiagnosticId,
            severity: CustomizationDiagnosticSeverity.Warning,
            phase: CustomizationDiagnosticPhase.Runtime,
            level: rejection.Level,
            projectionTypeName: rejection.ProjectionTypeName,
            componentTypeName: rejection.ComponentTypeName,
            role: rejection.Role,
            fieldName: rejection.FieldName,
            what: "A customization contract mismatch was rejected during startup hydration.",
            expected: $"The override component is rebuilt against installed contract version {expected}.",
            got: $"The registered override declared contract version {actual}; decision={rejection.Comparison.Decision}.",
            fix: "Rebuild the override package against the installed FrontComposer contracts or remove the incompatible registration.",
            fallback: "The descriptor is skipped, so the generated framework path remains available.",
            docsLink: $"https://hexalith.github.io/FrontComposer/diagnostics/{rejection.DiagnosticId}",
            properties: new Dictionary<string, string>(StringComparer.Ordinal) {
                ["decision"] = rejection.Comparison.Decision.ToString(),
                ["expectedVersion"] = expected,
                ["actualVersion"] = actual,
            });
    }

    private static string FormatVersion(CustomizationContractVersion version)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{version.Major}.{version.Minor}.{version.Build}");
}
