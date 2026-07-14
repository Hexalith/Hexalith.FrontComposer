namespace Hexalith.FrontComposer.SourceTools.Drift;

internal static class DriftConstants {
    internal const string SchemaVersion = "frontcomposer.generated-ui-baseline.v1";
    internal const string Algorithm = "frontcomposer-structural-v1";
    internal const int DefaultMaxDiagnostics = 50;
    internal const int DefaultMaxBaselineBytes = 256 * 1024;
    internal const int DefaultMaxDeclarations = 512;
    internal const int DefaultMaxPropertiesPerDeclaration = 256;

    internal const string MissingBaselineId = "HFC1058";
    internal const string InvalidBaselinePathId = "HFC1059";
    internal const string InvalidBaselineContentId = "HFC1060";
    internal const string UnsupportedSchemaId = "HFC1061";
    internal const string UnsupportedAlgorithmId = "HFC1062";
    internal const string BaselineBoundsExceededId = "HFC1063";
    internal const string DuplicateOrInvariantId = "HFC1064";
    internal const string StructuralDriftId = "HFC1065";
    internal const string MetadataDriftId = "HFC1066";
    internal const string InvalidOptionId = "HFC1067";
    internal const string TruncationId = "HFC1068";
    internal const string RedactionSuppressedId = "HFC1069";
    internal const string TrimAotReflectionCatalogId = "HFC1070";
}
