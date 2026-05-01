using Microsoft.AspNetCore.Authentication;

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
        string userClaimType) {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(userClaimType);
        ResetProviderEnablement();
        SetSingleClaim(TenantClaimTypes, tenantClaimType);
        SetSingleClaim(UserClaimTypes, userClaimType);
        OpenIdConnect.Enabled = true;
        OpenIdConnect.ProviderName = "Keycloak";
        OpenIdConnect.Authority = authority;
        OpenIdConnect.ClientId = clientId;
        OpenIdConnect.ClientSecret = clientSecret;
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

internal enum FrontComposerAuthenticationProviderKind {
    None,
    OpenIdConnect,
    Saml2,
    GitHubOAuth,
    CustomBrokered,
}

public sealed class FrontComposerOpenIdConnectOptions {
    public bool Enabled { get; set; }
    public string ProviderName { get; set; } = "OpenIdConnect";
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.Oidc";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
    public Uri? Authority { get; set; }
    public Uri? MetadataAddress { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Audience { get; set; }

    /// <summary>P15 — explicit issuer validation (Story 7-1 Provider Strategy: "Validate issuer and audience explicitly").</summary>
    public string? ValidIssuer { get; set; }

    public string ResponseType { get; set; } = "code";
    public IList<string> Scopes { get; } = ["openid", "profile"];

    /// <summary>P9 — request the OIDC handler to retain access tokens server-side so the framework token relay can replay them per outbound operation. Default true.</summary>
    public bool SaveTokens { get; set; } = true;
}

public sealed class FrontComposerSaml2Options {
    public bool Enabled { get; set; }
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.Saml2";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public Uri? MetadataAddress { get; set; }
    public Action<AuthenticationBuilder, string>? ConfigureHandler { get; set; }
}

public sealed class FrontComposerGitHubOAuthOptions {
    public bool Enabled { get; set; }
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.GitHubOAuth";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public string CallbackPath { get; set; } = "/signin-github";
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public Uri AuthorizationEndpoint { get; set; } = new("https://github.com/login/oauth/authorize");
    public Uri TokenEndpoint { get; set; } = new("https://github.com/login/oauth/access_token");
    public Uri UserInformationEndpoint { get; set; } = new("https://api.github.com/user");
}

public sealed class FrontComposerCustomBrokeredOptions {
    public bool Enabled { get; set; }
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.Custom";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public Action<AuthenticationBuilder, string>? ConfigureAuthentication { get; set; }
}

public sealed class FrontComposerAuthRedirectOptions {
    public string DefaultChallengeScheme { get; set; } = "Hexalith.FrontComposer.Auth";

    /// <summary>DN4/P33 — cookie middleware redirects unauthenticated requests here. Default endpoint is mapped by `MapHexalithFrontComposerAuthenticationEndpoints` to issue a `ChallengeAsync` against the configured provider.</summary>
    public string LoginPath { get; set; } = "/authentication/challenge";

    /// <summary>DN4/P33 — cookie middleware redirects sign-out requests here. Default endpoint is mapped by `MapHexalithFrontComposerAuthenticationEndpoints` to issue a `SignOutAsync`.</summary>
    public string LogoutPath { get; set; } = "/authentication/sign-out";
}

/// <summary>P10 — cookie scheme security policy (Story 7-1 NFR20/NFR21 BFF cookie posture).</summary>
public sealed class FrontComposerAuthCookieOptions {
    /// <summary>When true (default), cookies are issued with `Secure=Always` outside Development; SameSite=Lax; HttpOnly=true; SlidingExpiration=false.</summary>
    public bool ApplySecureDefaults { get; set; } = true;
}

public sealed class FrontComposerTokenRelayOptions {
    public string AccessTokenName { get; set; } = "access_token";
    public string? AuthenticationScheme { get; set; }
    public bool AllowGitHubOAuthTokenRelay { get; set; }
    public Func<CancellationToken, ValueTask<string?>>? HostAccessTokenProvider { get; set; }
}
