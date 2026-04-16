using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

/// <summary>
/// Story 2-2 Task 9.4 — demo IUserContextAccessor for the Counter sample so
/// <c>LastUsedValueProvider</c> doesn't fail closed in the absence of real auth.
/// Production adopters wire this from their OIDC provider (Story 7.1).
/// </summary>
internal sealed class DemoUserContextAccessor : IUserContextAccessor {
    public string? TenantId => "counter-demo";

    public string? UserId => "demo-user";
}
