namespace Hexalith.FrontComposer.Shell.Options;

public sealed class FrontComposerTokenRelayOptions {
    public string AccessTokenName { get; set; } = "access_token";
    public string? AuthenticationScheme { get; set; }
    public bool AllowGitHubOAuthTokenRelay { get; set; }
    public Func<CancellationToken, ValueTask<string?>>? HostAccessTokenProvider { get; set; }
    internal bool CircuitTokenSourceEnabled { get; set; }
}
