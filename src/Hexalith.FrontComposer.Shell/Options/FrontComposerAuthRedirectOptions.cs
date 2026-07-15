namespace Hexalith.FrontComposer.Shell.Options;

public sealed class FrontComposerAuthRedirectOptions {
    public string DefaultChallengeScheme { get; set; } = "Hexalith.FrontComposer.Auth";

    /// <summary>DN4/P33 — cookie middleware redirects unauthenticated requests here. Default endpoint is mapped by `MapHexalithFrontComposerAuthenticationEndpoints` to issue a `ChallengeAsync` against the configured provider.</summary>
    public string LoginPath { get; set; } = "/authentication/challenge";

    /// <summary>DN4/P33 — cookie middleware redirects sign-out requests here. Default endpoint is mapped by `MapHexalithFrontComposerAuthenticationEndpoints` to issue a `SignOutAsync`.</summary>
    public string LogoutPath { get; set; } = "/authentication/sign-out";
}
