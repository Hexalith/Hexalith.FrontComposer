using System.Text.Json.Nodes;

namespace Hexalith.FrontComposer.Mcp.Invocation;

internal static class FrontComposerMcpProjectionFailureMapper {
    public static FrontComposerMcpResult ToResult(FrontComposerMcpFailureCategory category) {
        ProjectionFailureContract contract = Map(category);
        JsonObject structured = new() {
            ["category"] = contract.TaxonomyCategory,
            ["message"] = contract.SafeText,
            ["docsCode"] = contract.DocsCode,
            ["retryable"] = contract.Retryable,
            ["refreshResources"] = contract.RefreshResources,
            ["isHiddenEquivalent"] = contract.IsHiddenEquivalent,
        };
        return FrontComposerMcpResult.Failure(category, contract.SafeText, structured);
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
            FrontComposerMcpFailureCategory.ResponseTooLarge => new(
                "response_too_large",
                "Projection output exceeded FrontComposer agent rendering limits.",
                "HFC-MCP-PROJECTION-RESPONSE-TOO-LARGE",
                Retryable: false,
                RefreshResources: false,
                IsHiddenEquivalent: false),
            FrontComposerMcpFailureCategory.UnsupportedRender or FrontComposerMcpFailureCategory.UnsupportedSchema => new(
                "unsupported_render",
                "Projection rendering strategy is not supported for the agent surface.",
                "HFC-MCP-PROJECTION-UNSUPPORTED-RENDER",
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
            FrontComposerMcpFailureCategory.DownstreamFailed or FrontComposerMcpFailureCategory.PolicyGateMissing => new(
                "downstream_failed",
                "Projection data is temporarily unavailable.",
                "HFC-MCP-PROJECTION-DOWNSTREAM-FAILED",
                Retryable: true,
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
