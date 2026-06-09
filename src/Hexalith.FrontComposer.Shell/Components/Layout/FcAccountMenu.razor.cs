using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Header account control rendering a <c>FluentAvatar</c> that opens a sign in / sign out
/// <c>FluentMenu</c>. The principal is resolved from
/// <see cref="AuthenticationStateProvider"/> — the same host-auth seam
/// <c>CommandAuthorizationEvaluator</c> uses — instead of <c>&lt;AuthorizeView&gt;</c>, so the
/// component renders without a cascading <c>Task&lt;AuthenticationState&gt;</c> (the framework
/// registers a fail-closed anonymous provider by default). Sign in / out delegate to the
/// framework auth endpoints (<see cref="FrontComposerAuthRedirectOptions.LoginPath"/> /
/// <see cref="FrontComposerAuthRedirectOptions.LogoutPath"/>) via a forced full-page navigation so
/// the OIDC challenge / sign-out completes server-side.
/// </summary>
public partial class FcAccountMenu : ComponentBase {
    // One account menu renders per page, so a constant id is a stable, collision-free anchor for the
    // FluentMenu Trigger (the documented avatar-as-trigger pattern).
    private const string AvatarId = "fc-account-avatar";

    private ClaimsPrincipal? _user;

    /// <summary>Injected host authentication-state seam (fail-closed anonymous provider by default).</summary>
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Injected navigation manager used to force a full-page redirect to the auth endpoints.</summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Injected authentication options carrying the framework login / logout endpoint paths.</summary>
    [Inject] private IOptions<FrontComposerAuthenticationOptions> AuthenticationOptions { get; set; } = default!;

    private bool IsAuthenticated => _user?.Identity?.IsAuthenticated == true;

    private string UserName => _user?.Identity?.Name ?? string.Empty;

    private string SignedInAsText => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        Localizer["SignedInAsLabel"].Value,
        UserName);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() {
        AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _user = state.User;
    }

    private void SignIn() => NavigateToAuthEndpoint(AuthenticationOptions.Value.Redirect.LoginPath);

    private void SignOut() => NavigateToAuthEndpoint(AuthenticationOptions.Value.Redirect.LogoutPath);

    private void NavigateToAuthEndpoint(string path) {
        // forceLoad guarantees the full server round-trip the cookie/OIDC endpoints require, matching
        // the retired Tenants auth-bar's data-enhance-nav="false" anchors.
        NavigationManager.NavigateTo($"{path}?returnUrl=/", forceLoad: true);
    }
}
