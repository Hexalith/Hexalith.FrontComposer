namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Canonical Shell-owned tenant-context accessor for tenant-scoped framework boundaries.
/// </summary>
public interface IFrontComposerTenantContextAccessor {
    /// <summary>
    /// Resolves a validated immutable context snapshot. When <paramref name="requestedTenant"/>
    /// is supplied, it must match the authenticated tenant exactly.
    /// </summary>
    TenantContextResult TryGetContext(string? requestedTenant = null, string operationKind = "tenant-scoped");

    /// <summary>
    /// Verifies that the currently authenticated context still matches a previously accepted
    /// snapshot before delayed side effects are applied.
    /// </summary>
    TenantContextResult Revalidate(TenantContextSnapshot snapshot, string operationKind = "tenant-scoped");
}
