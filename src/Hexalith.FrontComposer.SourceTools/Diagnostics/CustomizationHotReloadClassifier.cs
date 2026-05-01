namespace Hexalith.FrontComposer.SourceTools.Diagnostics;

/// <summary>
/// Known customization edit categories for hot-reload/rebuild diagnostics.
/// </summary>
public enum CustomizationHotReloadChangeKind {
    RazorBodyEdit = 0,
    CssOnlyEdit = 1,
    MarkerMetadataChanged = 2,
    ExpectedContractVersionChanged = 3,
    GenericContextTypeChanged = 4,
    DescriptorSchemaChanged = 5,
    RegistrationAddedOrRemoved = 6,
    DuplicateRegistrationIntroduced = 7,
    GeneratedManifestVersionMismatch = 8,
}

/// <summary>
/// Result of classifying a customization edit for hot reload.
/// </summary>
public sealed class HotReloadRebuildClassification {
    public HotReloadRebuildClassification(bool requiresRebuild, string? diagnosticId, string message) {
        RequiresRebuild = requiresRebuild;
        DiagnosticId = diagnosticId;
        Message = message;
    }

    public bool RequiresRebuild { get; }

    public string? DiagnosticId { get; }

    public string Message { get; }
}

/// <summary>
/// Classifies customization edits that Blazor hot reload can or cannot safely reflect.
/// </summary>
public static class CustomizationHotReloadClassifier {
    public static HotReloadRebuildClassification Classify(CustomizationHotReloadChangeKind changeKind) {
        if (changeKind == CustomizationHotReloadChangeKind.RazorBodyEdit
            || changeKind == CustomizationHotReloadChangeKind.CssOnlyEdit) {
            return new HotReloadRebuildClassification(
                requiresRebuild: false,
                diagnosticId: null,
                message: "Hot reload may apply this customization body/style edit when Blazor supports it.");
        }

        string got = ChangeKindLabel(changeKind);
        string message =
            "What: A customization edit changed metadata that the running app cannot safely refresh through hot reload.\n"
            + "Expected: Razor body and CSS-only edits may hot reload; marker metadata, contracts, descriptors, and registrations require rebuild validation.\n"
            + $"Got: {got}.\n"
            + "Fix: Stop the running app, rebuild the project, and restart so SourceTools and Shell registries agree on the descriptor set.\n"
            + "Fallback: Existing generated rendering remains the safe path until the rebuild completes.\n"
            + "DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1010";

        return new HotReloadRebuildClassification(
            requiresRebuild: true,
            diagnosticId: "HFC1010",
            message: message);
    }

    private static string ChangeKindLabel(CustomizationHotReloadChangeKind changeKind)
        => changeKind switch {
            CustomizationHotReloadChangeKind.MarkerMetadataChanged => "marker metadata changed",
            CustomizationHotReloadChangeKind.ExpectedContractVersionChanged => "expected contract version changed",
            CustomizationHotReloadChangeKind.GenericContextTypeChanged => "generic context type changed",
            CustomizationHotReloadChangeKind.DescriptorSchemaChanged => "descriptor schema changed",
            CustomizationHotReloadChangeKind.RegistrationAddedOrRemoved => "slot or view registration was added or removed",
            CustomizationHotReloadChangeKind.DuplicateRegistrationIntroduced => "duplicate registration was introduced",
            CustomizationHotReloadChangeKind.GeneratedManifestVersionMismatch => "generated manifest version mismatches the loaded framework contract",
            _ => changeKind.ToString(),
        };
}
