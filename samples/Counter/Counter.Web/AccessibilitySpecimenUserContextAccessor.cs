using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

/// <summary>Mutable fixed identities for opt-in Test-only scope boundary regressions.</summary>
internal sealed class AccessibilitySpecimenUserContextAccessor : IUserContextAccessor
{
    /// <inheritdoc />
    public string? TenantId { get; set; } = "counter-demo";

    /// <inheritdoc />
    public string? UserId { get; set; } = "demo-user";
}
