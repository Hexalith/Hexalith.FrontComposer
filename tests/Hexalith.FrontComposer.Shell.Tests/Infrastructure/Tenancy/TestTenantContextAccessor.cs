using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

internal sealed class TestTenantContextAccessor : IFrontComposerTenantContextAccessor {
    public string? TenantId { get; set; } = "tenant-a";

    public string? UserId { get; set; } = "user-a";

    public TenantContextResult TryGetContext(string? requestedTenant = null, string operationKind = "tenant-scoped") {
        if (string.IsNullOrWhiteSpace(TenantId)) {
            return TenantContextResult.Failure(TenantContextFailureCategory.TenantMissing, "test-correlation");
        }

        if (string.IsNullOrWhiteSpace(UserId)) {
            return TenantContextResult.Failure(TenantContextFailureCategory.UserMissing, "test-correlation");
        }

        if (requestedTenant is not null && !string.Equals(requestedTenant, TenantId, StringComparison.Ordinal)) {
            return TenantContextResult.Failure(TenantContextFailureCategory.TenantMismatch, "test-correlation");
        }

        return TenantContextResult.Success(new TenantContextSnapshot(TenantId, UserId, true, "test-correlation"));
    }

    public TenantContextResult Revalidate(TenantContextSnapshot snapshot, string operationKind = "tenant-scoped") {
        TenantContextResult current = TryGetContext(snapshot.TenantId, operationKind);
        return current.Succeeded && current.Context?.UserId == snapshot.UserId
            ? current
            : TenantContextResult.Failure(TenantContextFailureCategory.StaleTenantContext, "test-correlation");
    }
}
