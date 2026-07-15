namespace Hexalith.FrontComposer.Shell.Options;

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
    public string? RoleClaimType { get; set; }

    /// <summary>P15 — explicit issuer validation (Story 7-1 Provider Strategy: "Validate issuer and audience explicitly").</summary>
    public string? ValidIssuer { get; set; }

    public string ResponseType { get; set; } = "code";
    public IList<string> Scopes { get; } = ["openid", "profile"];

    /// <summary>P9 — request the OIDC handler to retain access tokens server-side so the framework token relay can replay them per outbound operation. Default true.</summary>
    public bool SaveTokens { get; set; } = true;
}
