using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Fail-closed authentication-state provider used when a host has not supplied a real Blazor
/// authentication integration.
/// </summary>
internal sealed class NullAuthenticationStateProvider : AuthenticationStateProvider {
    private static readonly Task<AuthenticationState> s_anonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => s_anonymousState;
}
