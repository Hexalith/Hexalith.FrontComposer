using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public enum McpSchemaNegotiationResultKind {
    Exact,
    CompatibleAdditive,
    Incompatible,
    UnknownClientVersion,
    UnknownServerBaseline,
    HiddenOrUnknown,
    StaleDescriptor,
    UnsupportedAlgorithm,
    Unavailable,
    SchemaIntegrityMismatch,
}

public sealed record McpSchemaNegotiationInput(
    bool IsHiddenOrUnknown,
    bool IsStaleDescriptor,
    SchemaFingerprint? ClientFingerprint,
    SchemaFingerprint? ServerFingerprint,
    bool HasTrustedBaseline,
    bool HasCompatibleAdditiveDrift,
    bool HasSchemaIntegrityMismatch);

public sealed record McpSchemaNegotiationResult(
    McpSchemaNegotiationResultKind Kind,
    FrontComposerMcpFailureCategory FailureCategory,
    string AgentCategory,
    string MessageKey,
    string DocsCode,
    bool AllowsSideEffects);

public static class McpSchemaNegotiator {
    public static McpSchemaNegotiationResult Negotiate(McpSchemaNegotiationInput input) {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsHiddenOrUnknown) {
            return Result(
                McpSchemaNegotiationResultKind.HiddenOrUnknown,
                FrontComposerMcpFailureCategory.UnknownResource,
                "unknown_resource",
                "schema.hidden-or-unknown",
                "HFC-MCP-UNKNOWN-RESOURCE",
                false);
        }

        if (input.IsStaleDescriptor) {
            return Result(
                McpSchemaNegotiationResultKind.StaleDescriptor,
                FrontComposerMcpFailureCategory.StaleDescriptor,
                "projection temporarily unavailable",
                "schema.stale-descriptor",
                "HFC-MCP-STALE-DESCRIPTOR",
                false);
        }

        if (input.ClientFingerprint is null || string.IsNullOrWhiteSpace(input.ClientFingerprint.Value)) {
            return Result(
                McpSchemaNegotiationResultKind.UnknownClientVersion,
                FrontComposerMcpFailureCategory.UnsupportedSchema,
                "unknown-version",
                "schema.client-version.unknown",
                "HFC-SCHEMA-UNKNOWN-CLIENT",
                false);
        }

        if (!string.Equals(input.ClientFingerprint.AlgorithmId, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)
            || !string.Equals(input.ServerFingerprint?.AlgorithmId, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)) {
            return Result(
                McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
                FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm,
                "unsupported-schema-fingerprint",
                "schema.algorithm.unsupported",
                "HFC-SCHEMA-UNSUPPORTED-ALGORITHM",
                false);
        }

        if (!input.HasTrustedBaseline) {
            return Result(
                McpSchemaNegotiationResultKind.UnknownServerBaseline,
                FrontComposerMcpFailureCategory.UnknownSchemaBaseline,
                "schema-unavailable",
                "schema.baseline.unknown",
                "HFC-SCHEMA-UNKNOWN-BASELINE",
                false);
        }

        if (input.HasSchemaIntegrityMismatch) {
            return Result(
                McpSchemaNegotiationResultKind.SchemaIntegrityMismatch,
                FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
                "schema-unavailable",
                "schema.integrity-mismatch",
                "HFC-SCHEMA-INTEGRITY-MISMATCH",
                false);
        }

        if (string.Equals(input.ClientFingerprint.Value, input.ServerFingerprint?.Value, StringComparison.Ordinal)) {
            return Result(
                McpSchemaNegotiationResultKind.Exact,
                FrontComposerMcpFailureCategory.None,
                "schema-exact",
                "schema.exact",
                "HFC-SCHEMA-EXACT",
                true);
        }

        if (input.HasCompatibleAdditiveDrift) {
            return Result(
                McpSchemaNegotiationResultKind.CompatibleAdditive,
                FrontComposerMcpFailureCategory.None,
                "schema-compatible-warning",
                "schema.compatible-additive",
                "HFC-SCHEMA-COMPATIBLE-ADDITIVE",
                true);
        }

        return Result(
            McpSchemaNegotiationResultKind.Incompatible,
            FrontComposerMcpFailureCategory.SchemaMismatch,
            "schema-mismatch",
            "schema.incompatible",
            "HFC-SCHEMA-MISMATCH",
            false);
    }

    private static McpSchemaNegotiationResult Result(
        McpSchemaNegotiationResultKind kind,
        FrontComposerMcpFailureCategory category,
        string agentCategory,
        string messageKey,
        string docsCode,
        bool allowsSideEffects)
        => new(kind, category, agentCategory, messageKey, docsCode, allowsSideEffects);
}
