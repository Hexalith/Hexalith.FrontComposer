using Hexalith.FrontComposer.Contracts.Rendering;

namespace AdopterProofKit.Web;

/// <summary>
/// Stands in for the adopter's sign-in with one fixed, public, non-synthetic tenant and user so the
/// Shell scope boundary admits generated surfaces. A real host uses its authentication bridge instead.
/// </summary>
public sealed class ProofUserContextAccessor : IUserContextAccessor
{
    /// <inheritdoc />
    public string? TenantId => "adopter-proof";

    /// <inheritdoc />
    public string? UserId => "adopter-proof-operator";
}
