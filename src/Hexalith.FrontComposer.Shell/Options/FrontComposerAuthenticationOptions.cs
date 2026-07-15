namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Options for the FrontComposer authentication bridge. Exactly one provider recipe is supported
/// per deployment in v1; provider-specific details stay in Shell registration code.
/// </summary>
public sealed class FrontComposerAuthenticationOptions {
    /// <summary>Gets the OIDC provider recipe options.</summary>
    public FrontComposerOpenIdConnectOptions OpenIdConnect { get; } = new();

    /// <summary>Gets the SAML2 handler bridge options.</summary>
    public FrontComposerSaml2Options Saml2 { get; } = new();

    /// <summary>Gets the GitHub OAuth sign-in recipe options.</summary>
    public FrontComposerGitHubOAuthOptions GitHubOAuth { get; } = new();

    /// <summary>Gets the custom or brokered provider bridge options.</summary>
    public FrontComposerCustomBrokeredOptions CustomBrokered { get; } = new();

    /// <summary>Gets tenant claim aliases in deterministic precedence order.</summary>
    public IList<string> TenantClaimTypes { get; } = [];

    /// <summary>Gets user claim aliases in deterministic precedence order.</summary>
    public IList<string> UserClaimTypes { get; } = [];

    /// <summary>Gets redirect safety options.</summary>
    public FrontComposerAuthRedirectOptions Redirect { get; } = new();

    /// <summary>Gets per-operation access token relay options.</summary>
    public FrontComposerTokenRelayOptions TokenRelay { get; } = new();

    /// <summary>Gets cookie scheme security options (Story 7-1 P10).</summary>
    public FrontComposerAuthCookieOptions Cookie { get; } = new();

    /// <summary>Configures the Keycloak OIDC authorization-code recipe.</summary>
    public void UseKeycloak(
        Uri authority,
        string clientId,
        string clientSecret,
        string tenantClaimType,
        string userClaimType,
        string? roleClaimType = null) {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(userClaimType);
        if (roleClaimType is not null) {
            ArgumentException.ThrowIfNullOrWhiteSpace(roleClaimType);
        }

        ResetProviderEnablement();
        SetSingleClaim(TenantClaimTypes, tenantClaimType);
        SetSingleClaim(UserClaimTypes, userClaimType);
        OpenIdConnect.Enabled = true;
        OpenIdConnect.ProviderName = "Keycloak";
        OpenIdConnect.Authority = authority;
        OpenIdConnect.ClientId = clientId;
        OpenIdConnect.ClientSecret = clientSecret;
        OpenIdConnect.RoleClaimType = roleClaimType;
        OpenIdConnect.ResponseType = "code";
    }

    /// <summary>Configures the Microsoft Entra ID OIDC authorization-code recipe.</summary>
    public void UseMicrosoftEntraId(
        Uri authority,
        string clientId,
        string clientSecret,
        string tenantClaimType = "tid",
        string userClaimType = "sub") {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(userClaimType);
        ResetProviderEnablement();
        SetSingleClaim(TenantClaimTypes, tenantClaimType);
        SetSingleClaim(UserClaimTypes, userClaimType);
        OpenIdConnect.Enabled = true;
        OpenIdConnect.ProviderName = "MicrosoftEntraId";
        OpenIdConnect.Authority = authority;
        OpenIdConnect.ClientId = clientId;
        OpenIdConnect.ClientSecret = clientSecret;
        OpenIdConnect.ResponseType = "code";
    }

    /// <summary>Configures the Google OIDC authorization-code recipe.</summary>
    public void UseGoogle(
        string clientId,
        string clientSecret,
        string tenantClaimType,
        string userClaimType = "sub") {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(userClaimType);
        ResetProviderEnablement();
        SetSingleClaim(TenantClaimTypes, tenantClaimType);
        SetSingleClaim(UserClaimTypes, userClaimType);
        OpenIdConnect.Enabled = true;
        OpenIdConnect.ProviderName = "Google";
        OpenIdConnect.Authority = new Uri("https://accounts.google.com");
        OpenIdConnect.ClientId = clientId;
        OpenIdConnect.ClientSecret = clientSecret;
        OpenIdConnect.ResponseType = "code";
    }

    /// <summary>Configures GitHub OAuth sign-in. EventStore token relay still requires a broker.</summary>
    public void UseGitHubOAuth(string clientId, string clientSecret, string tenantClaimType, string userClaimType = "id") {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(userClaimType);
        ResetProviderEnablement();
        SetSingleClaim(TenantClaimTypes, tenantClaimType);
        SetSingleClaim(UserClaimTypes, userClaimType);
        GitHubOAuth.Enabled = true;
        GitHubOAuth.ClientId = clientId;
        GitHubOAuth.ClientSecret = clientSecret;
    }

    private void ResetProviderEnablement() {
        // P19 — recipes are mutually exclusive. Calling a second recipe must replace, not stack.
        OpenIdConnect.Enabled = false;
        Saml2.Enabled = false;
        GitHubOAuth.Enabled = false;
        CustomBrokered.Enabled = false;
    }

    internal FrontComposerAuthenticationProviderKind SelectedProviderKind {
        get {
            if (OpenIdConnect.Enabled) {
                return FrontComposerAuthenticationProviderKind.OpenIdConnect;
            }

            if (Saml2.Enabled) {
                return FrontComposerAuthenticationProviderKind.Saml2;
            }

            if (GitHubOAuth.Enabled) {
                return FrontComposerAuthenticationProviderKind.GitHubOAuth;
            }

            return CustomBrokered.Enabled
                ? FrontComposerAuthenticationProviderKind.CustomBrokered
                : FrontComposerAuthenticationProviderKind.None;
        }
    }

    internal string SelectedChallengeScheme()
        => SelectedProviderKind switch {
            FrontComposerAuthenticationProviderKind.OpenIdConnect => OpenIdConnect.ChallengeScheme,
            FrontComposerAuthenticationProviderKind.Saml2 => Saml2.ChallengeScheme,
            FrontComposerAuthenticationProviderKind.GitHubOAuth => GitHubOAuth.ChallengeScheme,
            FrontComposerAuthenticationProviderKind.CustomBrokered => CustomBrokered.ChallengeScheme,
            _ => Redirect.DefaultChallengeScheme,
        };

    private static void SetSingleClaim(IList<string> target, string value) {
        target.Clear();
        target.Add(value);
    }
}
