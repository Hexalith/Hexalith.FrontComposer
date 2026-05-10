using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Mutable per-test user context accessor used by the default test host.
/// </summary>
public sealed class FrontComposerTestUserContextAccessor : IUserContextAccessor
{
    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public string? UserId { get; set; }
}
