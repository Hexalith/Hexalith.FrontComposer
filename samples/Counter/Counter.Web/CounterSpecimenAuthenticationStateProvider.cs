using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Counter.Web;

/// <summary>
/// Test-only authenticated principal for specimen E2E policy gates.
/// </summary>
internal sealed class CounterSpecimenAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> s_state = Task.FromResult(
        new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "demo-user"),
                        new Claim(ClaimTypes.Name, "FrontComposer specimen user"),
                    ],
                    authenticationType: "CounterSpecimenTest"))));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => s_state;
}
