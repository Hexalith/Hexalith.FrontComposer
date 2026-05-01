namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Default reusable guard for future tenant-scoped manifest and MCP tool enumeration.
/// </summary>
public sealed class TenantScopedManifestGate(IFrontComposerTenantContextAccessor tenantContextAccessor) : ITenantScopedManifestGate {
    /// <inheritdoc />
    public TenantContextResult TryAuthorizeEnumeration(string operationKind = "manifest-enumeration")
        => tenantContextAccessor.TryGetContext(requestedTenant: null, operationKind);
}
