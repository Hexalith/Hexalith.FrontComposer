using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Integration;

public sealed class FrontComposerUiAppHostTests {
    [Fact]
    public void AppHost_WiresFrontComposerUiOidcByDefaultWhenSecurityExists() {
        string root = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));

        program.ShouldContain("_ = frontComposerUI.WithJwtBearerSecurity(security);");
        program.ShouldContain("bool frontComposerOidcEnabled = !bool.TryParse(");
        program.ShouldContain("FrontComposerUi:OpenIdConnect:Enabled");
        program.ShouldContain("_ = frontComposerUI.WithOpenIdConnectSecurity(");
        program.ShouldContain("hexalith-frontcomposer-ui");
        program.ShouldContain("frontcomposer-ui-dev-secret");
        program.IndexOf(
            "bool.TryParse(builder.Configuration[\"FrontComposerUi:OpenIdConnect:Enabled\"], out bool frontComposerOidcEnabled)",
            StringComparison.Ordinal)
            .ShouldBe(
                -1,
                "frontcomposer-ui OIDC must default on with Keycloak security present; the configuration key is an opt-out.");
    }

    private static string FindRepoRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (dir.GetFiles("Hexalith.FrontComposer.slnx").Length > 0) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.FrontComposer.slnx by walking up from " + AppContext.BaseDirectory);
    }
}
