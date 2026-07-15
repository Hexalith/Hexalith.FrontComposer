using Microsoft.AspNetCore.Authentication;

namespace Hexalith.FrontComposer.Shell.Options;

public sealed class FrontComposerCustomBrokeredOptions {
    public bool Enabled { get; set; }
    public string ChallengeScheme { get; set; } = "Hexalith.FrontComposer.Custom";
    public string SignInScheme { get; set; } = "Hexalith.FrontComposer.Cookie";
    public Action<AuthenticationBuilder, string>? ConfigureAuthentication { get; set; }
}
