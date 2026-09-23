using Hexalith.FrontComposer.Shell.State.PendingCommands;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>Adapts the canonical tenant accessor for the pending-command state layer.</summary>
public sealed class ValidatedPendingScope(IFrontComposerTenantContextAccessor tenantContextAccessor) : IValidatedPendingScope {
    /// <inheritdoc />
    public (string TenantId, string UserId)? Current() {
        TenantContextSnapshot? context = tenantContextAccessor.TryGetContext(operationKind: "pending-state").Context;
        return context is null ? null : (context.TenantId, context.UserId);
    }
}
