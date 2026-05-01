namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Guard contract for future tenant-scoped manifest or MCP tool enumeration.
/// </summary>
public interface ITenantScopedManifestGate {
    /// <summary>
    /// Returns a validated tenant context for enumeration, or a sanitized failure result when
    /// the current context is missing or invalid.
    /// </summary>
    TenantContextResult TryAuthorizeEnumeration(string operationKind = "manifest-enumeration");
}
