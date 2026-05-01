using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

/// <summary>
/// Sample-only fake authenticated accessor for Story 7-1 smoke tests. It is opt-in through
/// configuration and must not be used as a production auth bridge.
/// </summary>
internal sealed class CounterFakeAuthUserContextAccessor : IUserContextAccessor {
    public string? TenantId => "counter-fake-auth";

    public string? UserId => "fake-auth-user";
}
