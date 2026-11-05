using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

internal sealed class MutableCircuitAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
{
    public ClaimsPrincipal Principal { get; set; } = principal;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(Principal));
}
