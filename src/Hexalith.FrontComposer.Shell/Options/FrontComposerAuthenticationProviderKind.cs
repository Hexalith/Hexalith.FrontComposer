namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>Identifies the authentication provider mode configured for FrontComposer.</summary>
public enum FrontComposerAuthenticationProviderKind {
    /// <summary>No provider is enabled.</summary>
    None,

    /// <summary>OpenID Connect.</summary>
    OpenIdConnect,

    /// <summary>An adopter-supplied SAML2 handler.</summary>
    Saml2,

    /// <summary>GitHub OAuth.</summary>
    GitHubOAuth,

    /// <summary>A host-owned brokered authentication handler.</summary>
    CustomBrokered,
}
