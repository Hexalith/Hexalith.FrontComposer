using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class AccountMenuAuthenticationChangeTests : LayoutComponentTestBase
{
    [Fact]
    public async Task AccountMenuTracksPrincipalChangesAndFailsClosedOnAnUnavailableState()
    {
        MutableTestAuthenticationStateProvider provider = new();
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
        AuthenticationState authenticated = new(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Current operator")], "Test")));

        await cut.InvokeAsync(() => provider.Publish(Task.FromResult(authenticated))).ConfigureAwait(true);

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-out\"");
            cut.Markup.ShouldContain("Current operator");
        });
        await cut.InvokeAsync(() => provider.Publish(Task.FromException<AuthenticationState>(new InvalidOperationException("Unavailable")))).ConfigureAwait(true);
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-sign-out\"");
        });
    }
}
