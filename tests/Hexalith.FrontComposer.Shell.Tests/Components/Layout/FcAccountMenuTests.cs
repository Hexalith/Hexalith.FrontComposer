using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Header account control. With the framework's fail-closed anonymous
/// <c>AuthenticationStateProvider</c> (the default when no host auth is wired), the avatar resolves to
/// the anonymous state and the menu offers Sign in — proving the control renders without a cascading
/// <c>Task&lt;AuthenticationState&gt;</c>.
/// </summary>
public sealed class FcAccountMenuTests : LayoutComponentTestBase
{
    public FcAccountMenuTests()
    {
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersAnonymousSignInAffordance()
    {
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("data-testid=\"fc-account-menu\"");
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-sign-out\"");
        });
    }
}
