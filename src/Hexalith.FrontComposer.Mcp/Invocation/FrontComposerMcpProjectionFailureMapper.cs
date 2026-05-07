using System.Text.Json.Nodes;

namespace Hexalith.FrontComposer.Mcp.Invocation;

internal static class FrontComposerMcpProjectionFailureMapper {
    // DN-2: hidden-equivalent categories share the public payload of UnknownResource so an
    // adversary cannot branch on category/docsCode/message to learn whether a resource exists,
    // is policy-blocked, or is auth/tenant-blocked. The internal `Category` enum is preserved
    // for telemetry and host-side decisions; only the agent-visible structured content collapses.
    private static readonly ProjectionFailureContract HiddenEquivalentPublic = new(
        "unknown_resource",
        "Projection resource is not available.",
        "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE",
        Retryable: false,
        RefreshResources: true,
        IsHiddenEquivalent: true);

    public static FrontComposerMcpResult ToResult(FrontComposerMcpFailureCategory category) {
        ProjectionFailureContract internalContract = Map(category);
        ProjectionFailureContract publicContract = internalContract.IsHiddenEquivalent
            ? HiddenEquivalentPublic
            : internalContract;
        JsonObject structured = new() {
            ["category"] = publicContract.TaxonomyCategory,
            ["message"] = publicContract.SafeText,
            ["docsCode"] = publicContract.DocsCode,
            ["retryable"] = publicContract.Retryable,
            ["refreshResources"] = publicContract.RefreshResources,
            ["isHiddenEquivalent"] = publicContract.IsHiddenEquivalent,
        };
        return FrontComposerMcpResult.Failure(category, publicContract.SafeText, structured);
    }

    private static ProjectionFailureContract Map(FrontComposerMcpFailureCategory category)
        => category switch {
            FrontComposerMcpFailureCategory.UnknownResource => new(
                "unknown_resource",
                "Projection resource is not available.",
                "HFC-MCP-PROJECTION-UNKNOWN-RESOURCE",
                Retryable: false,
                RefreshResources: true,
                IsHiddenEquivalent: true),
            FrontComposerMcpFailureCategory.MalformedRequest or FrontComposerMcpFailureCategory.ValidationFailed => new(
                "malformed_resource",
                "Projection resource request is invalid.",
                "HFC-MCP-PROJECTION-MALFORMED-RESOURCE",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.AuthFailed => new(
                "auth_failed",
                "Projection resource is not available for the current agent context.",
                "HFC-MCP-PROJECTION-AUTH-FAILED",
                Retryable: false,
                RefreshResources: true,
                IsHiddenEquivalent: true),
            FrontComposerMcpFailureCategory.TenantMissing => new(
                "tenant_missing",
                "Projection resource is not available for the current tenant context.",
                "HFC-MCP-PROJECTION-TENANT-MISSING",
                Retryable: false,
                RefreshResources: true,
                IsHiddenEquivalent: true),
            FrontComposerMcpFailureCategory.PolicyFiltered => new(
                "policy_filtered",
                "Projection resource is not available for the current agent context.",
                "HFC-MCP-PROJECTION-POLICY-FILTERED",
                Retryable: false,
                RefreshResources: true,
                IsHiddenEquivalent: true),
            FrontComposerMcpFailureCategory.StaleDescriptor => new(
                "stale_descriptor",
                "Projection descriptor is stale. Refresh available resources and retry.",
                "HFC-MCP-PROJECTION-STALE-DESCRIPTOR",
                Retryable: true,
                RefreshResources: true,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.SchemaMismatch => new(
                "schema-mismatch",
                "Projection schema is not compatible with the client manifest. Refresh schema metadata and retry.",
                "HFC-MCP-PROJECTION-SCHEMA-MISMATCH",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.UnknownSchemaBaseline => new(
                "schema-unavailable",
                "Projection schema baseline is unavailable. Refresh schema metadata or contact the host maintainer.",
                "HFC-MCP-PROJECTION-SCHEMA-UNAVAILABLE",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm => new(
                "unsupported-schema-fingerprint",
                "Projection schema fingerprint algorithm is not supported by this server.",
                "HFC-MCP-PROJECTION-UNSUPPORTED-SCHEMA-ALGORITHM",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch => new(
                "schema-unavailable",
                "Projection schema metadata failed an integrity check.",
                "HFC-MCP-PROJECTION-SCHEMA-INTEGRITY-MISMATCH",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.ResponseTooLarge => new(
                "response_too_large",
                "Projection output exceeded FrontComposer agent rendering limits.",
                "HFC-MCP-PROJECTION-RESPONSE-TOO-LARGE",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.UnsupportedRender => new(
                "unsupported_render",
                "Projection rendering strategy is not supported for the agent surface.",
                "HFC-MCP-PROJECTION-UNSUPPORTED-RENDER",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            // C4 (Group D / chunk-2 re-review): UnsupportedSchema (UnknownClientVersion path) was
            // previously folded into the UnsupportedRender arm, emitting the wrong taxonomy
            // (`unsupported_render`) for a schema-version mismatch. Distinct branch so agents
            // can branch between rendering capability and schema version.
            FrontComposerMcpFailureCategory.UnsupportedSchema => new(
                "unsupported-schema-version",
                "Projection schema version declared by the client is not supported by this server.",
                "HFC-MCP-PROJECTION-UNSUPPORTED-SCHEMA-VERSION",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.QueryRejected => new(
                "query_failed",
                "Projection data is temporarily unavailable.",
                "HFC-MCP-PROJECTION-QUERY-FAILED",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.Timeout => new(
                "timeout",
                "Projection read timed out before a safe response was produced.",
                "HFC-MCP-PROJECTION-TIMEOUT",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.Canceled => new(
                "canceled",
                "Projection read was canceled before a safe response was produced.",
                "HFC-MCP-PROJECTION-CANCELED",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.DegradedResult => new(
                "degraded_result",
                "Projection data is available with degraded freshness.",
                "HFC-MCP-PROJECTION-DEGRADED-RESULT",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.DownstreamFailed => new(
                "downstream_failed",
                "Projection data is temporarily unavailable.",
                "HFC-MCP-PROJECTION-DOWNSTREAM-FAILED",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            // R2-P4: PolicyGateMissing is a host configuration error (no security gate
            // registered), not a transient downstream failure. Telling agents to retry
            // indefinitely against a misconfigured host is wasteful and noisy. Surface as
            // non-retryable so the agent stops and the operator notices.
            FrontComposerMcpFailureCategory.PolicyGateMissing => new(
                "downstream_failed",
                "Projection data is temporarily unavailable.",
                "HFC-MCP-PROJECTION-DOWNSTREAM-FAILED",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            _ => new(
                "downstream_failed",
                "Projection data is temporarily unavailable.",
                "HFC-MCP-PROJECTION-DOWNSTREAM-FAILED",
                Retryable: true,
                RefreshResources: false,
                IsHiddenEquivalent: false),
        };

    private sealed record ProjectionFailureContract(
        string TaxonomyCategory,
        string SafeText,
        string DocsCode,
        bool Retryable,
        bool RefreshResources,
        bool IsHiddenEquivalent);
}
