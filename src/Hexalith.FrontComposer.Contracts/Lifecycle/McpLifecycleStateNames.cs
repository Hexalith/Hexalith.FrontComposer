namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Canonical MCP wire-protocol lifecycle state names emitted on the
/// <c>Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult.State</c> field. Distinct from
/// <see cref="CommandLifecycleState"/>, which is the client-facing UI lifecycle (Idle, Submitting,
/// Acknowledged, Syncing, Confirmed, Rejected); MCP protocol surfaces a smaller terminal-and-
/// in-flight set.
/// </summary>
/// <remarks>
/// 8-6a chunk-3 review (decision): the SourceTools lifecycle fingerprint catalog and any future
/// production emitter both source from <see cref="Canonical"/> so the AC9 cross-package check at
/// <c>SchemaFingerprintCrossPackageTests.LifecycleCatalog_StateEnumValues_PinnedToCanonicalSet</c>
/// pins to a real source of truth instead of self-pinning. Adding or removing a wire-state name
/// here regenerates the lifecycle fingerprint and surfaces as a build failure in the cross-check
/// test — adopters must regenerate their baseline.
/// </remarks>
public static class McpLifecycleStateNames {
    /// <summary>
    /// Canonical MCP lifecycle state set in catalog-emit order (alphabetical, matching the
    /// SourceTools blob canonicalizer's <c>StringComparer.Ordinal</c> sort downstream).
    /// </summary>
    public static readonly IReadOnlyList<string> Canonical = [
        "Accepted",
        "Confirmed",
        "Failed",
        "Rejected",
        "Running",
    ];
}
