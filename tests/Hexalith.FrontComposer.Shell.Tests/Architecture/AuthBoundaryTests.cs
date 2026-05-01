using System.Text.RegularExpressions;

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
            // P18 — case-insensitive path comparison (Windows-developed, may run on Linux CI).
            if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || allowedPathFragments.Any(p => relative.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            // P18 — strip line and block comments before scanning so a doc-comment that legitimately
            // mentions "OpenIdConnect" does not trigger a boundary violation.
            string text = StripComments(File.ReadAllText(file));
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
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // P18 — exemptions are per-literal, not per-file. Only `access_token` and the
        // Authorization header literals are allowed in the two files where token-name plumbing
        // legitimately lives. Other token-sensitive literals remain banned even there.
        Dictionary<string, HashSet<string>> perFileLiteralExemptions = new(StringComparer.OrdinalIgnoreCase) {
            [Normalize("FrontComposerAuthenticationOptions.cs")] = new(StringComparer.Ordinal) { "access_token" },
            [Normalize("FrontComposerAccessTokenProvider.cs")] = new(StringComparer.Ordinal) { "access_token" },
            // Validator emits a teaching diagnostic that NAMES the token-name option default
            // ("default 'access_token'") so adopters know what to set; not a token value.
            [Normalize("FrontComposerAuthenticationOptionsValidator.cs")] = new(StringComparer.Ordinal) { "access_token" },
        };

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
                if (!text.Contains(tokenName, StringComparison.Ordinal)) {
                    continue;
                }

                bool exempt = perFileLiteralExemptions
                    .Any(kvp => relative.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase)
                        && kvp.Value.Contains(tokenName));
                if (!exempt) {
                    violations.Add(relative + " contains token-sensitive literal " + tokenName);
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    private static string StripComments(string source) {
        // Block comments first (greedy reluctant), then line comments.
        string noBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(noBlocks, @"//.*?$", string.Empty, RegexOptions.Multiline);
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
