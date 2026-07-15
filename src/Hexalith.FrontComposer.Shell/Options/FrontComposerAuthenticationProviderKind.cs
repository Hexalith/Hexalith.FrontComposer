namespace Hexalith.FrontComposer.Shell.Options;

internal enum FrontComposerAuthenticationProviderKind {
    None,
    OpenIdConnect,
    Saml2,
    GitHubOAuth,
    CustomBrokered,
}
