using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Authenticated rendering of the header account control. The connected user's display name must
/// surface (as the header label, avatar initials, and menu title) even when the token does not carry
/// a claim that maps to <see cref="System.Security.Principal.IIdentity.Name"/> — the common Keycloak
/// case where only <c>preferred_username</c> / <c>given_name</c>+<c>family_name</c> are present.
/// </summary>
public sealed class FcAccountMenuAuthenticatedTests : LayoutComponentTestBase {
    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider {
        private readonly AuthenticationState _state = new(user);

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    private void UseAuthenticatedUser(params Claim[] claims) {
        // authenticationType non-null => IsAuthenticated == true; with no ClaimTypes.Name claim,
        // Identity.Name stays null so the test exercises the claim-fallback resolution.
        ClaimsPrincipal user = new(new ClaimsIdentity(claims, authenticationType: "oidc"));
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => new StubAuthenticationStateProvider(user)));
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersDisplayNameFromPreferredUsernameWhenIdentityNameMissing() {
        UseAuthenticatedUser(new Claim("preferred_username", "jdupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-account-name\"");
            cut.Markup.ShouldContain("jdupont");
            // The disabled header item must carry the name (regression: it previously rendered the long
            // "Signed in as {name}" sentence, which was clipped at the viewport edge).
            cut.Find("[data-testid='fc-account-user']").TextContent.ShouldContain("jdupont");
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-out\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-sign-in\"");
        });
    }

    [Fact]
    public void RendersDisplayNameFromGivenAndFamilyNameWhenNoFriendlyClaim() {
        UseAuthenticatedUser(
            new Claim("given_name", "Jean"),
            new Claim("family_name", "Dupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jean Dupont"));
    }

    [Fact]
    public void PrefersFriendlyNameClaimOverPreferredUsername() {
        UseAuthenticatedUser(
            new Claim("name", "Jean Dupont"),
            new Claim("preferred_username", "jdupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jean Dupont"));
    }
}
