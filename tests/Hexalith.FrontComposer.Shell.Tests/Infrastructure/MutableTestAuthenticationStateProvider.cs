using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure;

/// <summary>Publishes controlled principal changes through the actual framework event.</summary>
internal sealed class MutableTestAuthenticationStateProvider : AuthenticationStateProvider
{
    private Task<AuthenticationState> _state = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state;

    /// <summary>Sets the current authentication state and notifies the installed subscribers.</summary>
    /// <param name="state">The new state task.</param>
    public void Publish(Task<AuthenticationState> state)
    {
        _state = state;
        NotifyAuthenticationStateChanged(state);
    }
}
