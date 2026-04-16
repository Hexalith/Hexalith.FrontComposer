using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Default accessor returning <see langword="null"/> for both segments. Triggers Decision D31
/// fail-closed behavior in <c>LastUsedValueProvider</c> until the adopter registers a real
/// implementation backed by their auth stack.
/// </summary>
public sealed class NullUserContextAccessor : IUserContextAccessor {
    /// <inheritdoc/>
    public string? TenantId => null;

    /// <inheritdoc/>
    public string? UserId => null;
}
