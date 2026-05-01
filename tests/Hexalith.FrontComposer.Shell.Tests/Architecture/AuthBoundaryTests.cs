using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class AuthBoundaryTests {
    [Fact]
    public void ProviderSpecificAuthenticationTypes_DoNotLeakOutsideShellAuthArea() {
        string root = FindRepositoryRoot();
        string[] bannedFragments = [
            "OpenIdConnect",
            "OAuthOptions",
            "OAuthCreatingTicketContext",
            "Saml",
            "GitHubOAuth",
        ];
        string[] allowedPathFragments = [
            Normalize("src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs"),
            Normalize("src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs"),
            Normalize("src/Hexalith.FrontComposer.Shell/Services/Auth/"),
            Normalize("tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/"),
            Normalize("tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs"),
            Normalize("tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs"),
            Normalize("_bmad-output/implementation-artifacts/7-1-oidc-saml-authentication-integration.md"),
        ];

        List<string> violations = [];
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)) {
            string relative = Normalize(Path.GetRelativePath(root, file));
            if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || allowedPathFragments.Any(relative.StartsWith)) {
                continue;
            }

            string text = File.ReadAllText(file);
            foreach (string fragment in bannedFragments) {
                if (text.Contains(fragment, StringComparison.Ordinal)) {
                    violations.Add(relative + " contains " + fragment);
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void AuthBridge_DoesNotWriteTokensToFrameworkStorage() {
        string root = FindRepositoryRoot();
        string[] authFiles = Directory.EnumerateFiles(
                Path.Combine(root, "src", "Hexalith.FrontComposer.Shell"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file => Normalize(Path.GetRelativePath(root, file)).Contains(
                Normalize("Services/Auth/"),
                StringComparison.Ordinal))
            .ToArray();

        List<string> violations = [];
        foreach (string file in authFiles) {
            string text = File.ReadAllText(file);
            string relative = Normalize(Path.GetRelativePath(root, file));
            if (text.Contains("IStorageService", StringComparison.Ordinal)
                || text.Contains("localStorage", StringComparison.Ordinal)
                || text.Contains(".SetAsync(", StringComparison.Ordinal)) {
                violations.Add(relative + " writes or references framework storage");
            }

            foreach (string tokenName in new[] { "access_token", "refresh_token", "id_token", "Headers.Authorization", "\"Authorization\"" }) {
                if (text.Contains(tokenName, StringComparison.Ordinal)
                    && !relative.EndsWith(Normalize("FrontComposerAuthenticationOptions.cs"), StringComparison.Ordinal)
                    && !relative.EndsWith(Normalize("FrontComposerAccessTokenProvider.cs"), StringComparison.Ordinal)) {
                    violations.Add(relative + " contains token-sensitive literal " + tokenName);
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.sln"))) {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Normalize(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
