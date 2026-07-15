using Microsoft.AspNetCore.Authentication;

namespace Hexalith.FrontComposer.Shell.Options;

public sealed class FrontComposerSaml2Options {
    public bool Enabled { get; set; }
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.Saml2";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public Uri? MetadataAddress { get; set; }
    public Action<AuthenticationBuilder, string>? ConfigureHandler { get; set; }
}
