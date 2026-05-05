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
    /// <summary>
    /// Algorithm identifiers the negotiator accepts. The runtime trusts emitter-supplied
    /// fingerprints (it never recomputes them), so accepting both the runtime canonical-JSON
    /// algorithm and the SourceTools build-time text-blob algorithm is safe in v1. Per D23.
    /// </summary>
    private static readonly HashSet<string> SupportedAlgorithms = new(StringComparer.Ordinal) {
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1,
    };

    public static McpSchemaNegotiationResult Negotiate(McpSchemaNegotiationInput input) {
        ArgumentNullException.ThrowIfNull(input);

        // Precedence (per Negotiation Precedence section in story 8-6):
        // 1. Hidden/unknown equivalence (Story 8-2 hidden-resource oracle defense)
        // 2. Stale descriptor
        // 3. Schema integrity mismatch (P-42: integrity corruption is more actionable than
        //    "baseline missing", and integrity is a server-side authoritative check)
        // 4. Algorithm support (P-41: gate algorithm compatibility before we look at client
        //    versions, so an unsupported algorithm is never reported as "unknown client")
        // 5. Server fingerprint presence (P-13: missing server fingerprint is a distinct
        //    UnknownServerBaseline classification, not an algorithm error)
        // 6. Client fingerprint presence/value (P-14: both sides validated for null/whitespace)
        // 7. Trusted baseline available (with P-40 short-circuit on byte-identical hashes)
        // 8. Exact / compatible-additive / incompatible classification

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

        // P-42: integrity check before baseline-availability so a partial-manifest corruption
        // produces actionable "integrity-mismatch" remediation rather than "missing baseline".
        if (input.HasSchemaIntegrityMismatch) {
            return Result(
                McpSchemaNegotiationResultKind.SchemaIntegrityMismatch,
                FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
                "schema-unavailable",
                "schema.integrity-mismatch",
                "HFC-SCHEMA-INTEGRITY-MISMATCH",
                false);
        }

        // P-13: server fingerprint absence is distinct from unsupported algorithm; report it
        // as UnknownServerBaseline so operators are pointed at "ship a baseline" rather than
        // "fix algorithm".
        if (input.ServerFingerprint is null || string.IsNullOrWhiteSpace(input.ServerFingerprint.Value)) {
            return Result(
                McpSchemaNegotiationResultKind.UnknownServerBaseline,
                FrontComposerMcpFailureCategory.UnknownSchemaBaseline,
                "schema-unavailable",
                "schema.baseline.unknown",
                "HFC-SCHEMA-UNKNOWN-BASELINE",
                false);
        }

        // P-41: algorithm support gate before client-fingerprint-null check so a malformed
        // algorithm id against any request is classified deterministically.
        if (!SupportedAlgorithms.Contains(input.ServerFingerprint.AlgorithmId ?? string.Empty)) {
            return Result(
                McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
                FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm,
                "unsupported-schema-fingerprint",
                "schema.algorithm.unsupported",
                "HFC-SCHEMA-UNSUPPORTED-ALGORITHM",
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

        if (!SupportedAlgorithms.Contains(input.ClientFingerprint.AlgorithmId ?? string.Empty)) {
            return Result(
                McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
                FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm,
                "unsupported-schema-fingerprint",
                "schema.algorithm.unsupported",
                "HFC-SCHEMA-UNSUPPORTED-ALGORITHM",
                false);
        }

        // P-40: byte-identical hashes can short-circuit the baseline-trust check. A redeploy
        // of identical schema after baseline-store wipe should not lock callers out of side
        // effects — the structural truth is in the matching hash.
        bool hashesMatch = string.Equals(input.ClientFingerprint.Value, input.ServerFingerprint.Value, StringComparison.Ordinal);

        if (!input.HasTrustedBaseline && !hashesMatch) {
            return Result(
                McpSchemaNegotiationResultKind.UnknownServerBaseline,
                FrontComposerMcpFailureCategory.UnknownSchemaBaseline,
                "schema-unavailable",
                "schema.baseline.unknown",
                "HFC-SCHEMA-UNKNOWN-BASELINE",
                false);
        }

        if (hashesMatch) {
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
