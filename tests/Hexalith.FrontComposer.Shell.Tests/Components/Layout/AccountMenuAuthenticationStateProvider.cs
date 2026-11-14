using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>Provides mutable circuit authentication for account-menu rendering tests.</summary>
/// <param name="user">The initial circuit principal.</param>
internal sealed class AccountMenuAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    private AuthenticationState _state = new(user);

    /// <summary>Gets or sets an initialization failure raised before a task is returned.</summary>
    public Exception? InitialFailure { get; set; }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => InitialFailure is { } failure ? throw failure : Task.FromResult(_state);

    /// <summary>Publishes a pending, failed, or completed authentication operation.</summary>
    /// <param name="state">The next authentication operation.</param>
    public void PublishState(Task<AuthenticationState> state) => NotifyAuthenticationStateChanged(state);

    /// <summary>Publishes the next principal to already-rendered components.</summary>
    /// <param name="user">The next circuit principal.</param>
    public void PublishUser(ClaimsPrincipal user)
    {
        _state = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }
}
