using Counter.Specimens;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

namespace Counter.Web;

/// <summary>Fixed Test-only admission scope seam; the shell's real tenant scope remains independent.</summary>
internal sealed class AccessibilitySpecimenPendingScope(
    IFrontComposerTenantContextAccessor tenantContext,
    AccessibilitySpecimenState scenarios) : IValidatedPendingScope
{
    /// <inheritdoc />
    public (string TenantId, string UserId)? Current()
        => scenarios.Outcome != "admission" && tenantContext.TryGetContext(operationKind: "accessibility-admission").Context is { } scope
            ? (scope.TenantId, scope.UserId) : null;
}
