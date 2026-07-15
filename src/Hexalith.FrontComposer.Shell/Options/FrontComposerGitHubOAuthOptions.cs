namespace Hexalith.FrontComposer.Shell.Options;

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
