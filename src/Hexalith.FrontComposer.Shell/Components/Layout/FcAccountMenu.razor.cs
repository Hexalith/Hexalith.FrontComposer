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

    // Right-align the dropdown to the avatar so it opens leftward and stays on-screen. The avatar is
    // ALWAYS the right-most header action (see FrontComposerShell), so Fluent's default menu popover
    // rule — inset-inline-start: anchor(start) with position-try-fallbacks: flip-block (vertical flip
    // only) — left-aligns the list to a viewport-edge trigger and lets it overflow the right edge,
    // clipping "Se connecter" / "Signed in as …". These two declarations mirror Fluent's own
    // :host([split]) ::slotted([popover]) rule (inset-inline-start: auto; inset-inline-end:
    // anchor(end)), the framework-sanctioned way to right-anchor a menu popover. Applied as an inline
    // Style because CSS isolation scope attributes never reach a Fluent web component's slotted
    // popover (a scoped .razor.css rule would be dead here).
    private const string MenuListStyle = "inset-inline-start: auto; inset-inline-end: anchor(end);";

    private ClaimsPrincipal? _user;

    /// <summary>Injected host authentication-state seam (fail-closed anonymous provider by default).</summary>
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    /// <summary>Injected navigation manager used to force a full-page redirect to the auth endpoints.</summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Injected authentication options carrying the framework login / logout endpoint paths.</summary>
    [Inject] private IOptions<FrontComposerAuthenticationOptions> AuthenticationOptions { get; set; } = default!;

    private bool IsAuthenticated => _user?.Identity?.IsAuthenticated == true;

    private string UserName => ResolveDisplayName();

    // Identity.Name only resolves when the token's NameClaimType matches what the IdP emits, which
    // the Keycloak `preferred_username` / `name` split (and per-IdP claim naming) routinely defeats —
    // leaving the avatar initials and menu header blank. Resolve a human-friendly display name from
    // the common claims in priority order, then fall back to given+family, then Identity.Name.
    private string ResolveDisplayName() {
        if (_user is null) {
            return string.Empty;
        }

        string? friendly = FirstNonEmptyClaim("name", ClaimTypes.Name, "preferred_username", "nickname", ClaimTypes.Email, "email");
        if (!string.IsNullOrWhiteSpace(friendly)) {
            return friendly;
        }

        string? given = FirstNonEmptyClaim(ClaimTypes.GivenName, "given_name");
        string? family = FirstNonEmptyClaim(ClaimTypes.Surname, "family_name");
        string composed = string.Join(' ', new[] { given, family }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(composed)
            ? _user.Identity?.Name ?? string.Empty
            : composed;
    }

    private string? FirstNonEmptyClaim(params string[] claimTypes) {
        foreach (string claimType in claimTypes) {
            string? value = _user!.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) {
                return value;
            }
        }

        return null;
    }

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

    private void NavigateToAuthEndpoint(string path) =>
        // forceLoad guarantees the full server round-trip the cookie/OIDC endpoints require, matching
        // the retired Tenants auth-bar's data-enhance-nav="false" anchors.
        NavigationManager.NavigateTo($"{path}?returnUrl=/", forceLoad: true);
}
