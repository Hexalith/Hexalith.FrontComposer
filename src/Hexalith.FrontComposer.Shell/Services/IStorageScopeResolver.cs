namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Single Scoped seam that resolves the current tenant/user storage scope with fail-closed
/// semantics (Story 11.15, M19 cluster 4). Consolidates the six formerly-duplicated effect-local
/// <c>TryResolveScope</c> helpers so tenant/user hardening is defined once and applied uniformly by
/// every persisted-feature effect (Theme, Density, Navigation, DataGridNavigation,
/// CapabilityDiscovery, CommandPalette).
/// </summary>
internal interface IStorageScopeResolver {
    /// <summary>
    /// Resolves the raw (un-escaped) tenant and user identities for a storage operation, failing
    /// closed when the scope is missing/blank/whitespace or the accessor getter throws non-fatally.
    /// </summary>
    /// <param name="tenantId">On success, the raw tenant identity; otherwise <see cref="string.Empty"/>.</param>
    /// <param name="userId">On success, the raw user identity; otherwise <see cref="string.Empty"/>.</param>
    /// <param name="direction">
    /// The operation direction (e.g. <c>hydrate</c> / <c>persist</c>) surfaced in the fail-closed
    /// diagnostic. Never a tenant/user value.
    /// </param>
    /// <returns><see langword="true"/> when both identities resolved; otherwise <see langword="false"/> (fail closed).</returns>
    bool TryResolveScope(out string tenantId, out string userId, string direction);
}
