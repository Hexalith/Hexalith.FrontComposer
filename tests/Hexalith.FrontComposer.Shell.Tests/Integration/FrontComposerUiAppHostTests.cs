using System.Text.Json;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Integration;

public sealed class FrontComposerUiAppHostTests {
    private const string FrontComposerUiClientId = "hexalith-frontcomposer-ui";
    private const string FrontComposerUiClientSecret = "frontcomposer-ui-dev-secret";

    [Fact]
    public void AppHost_WiresFrontComposerUiOidcByDefaultWhenSecurityExists() {
        string root = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));

        program.ShouldContain("_ = frontComposerUI.WithJwtBearerSecurity(security);");
        program.ShouldContain("bool frontComposerOidcEnabled = !bool.TryParse(");
        program.ShouldContain("FrontComposerUi:OpenIdConnect:Enabled");
        program.ShouldContain("_ = frontComposerUI.WithOpenIdConnectSecurity(");
        program.ShouldContain(FrontComposerUiClientId);
        program.ShouldContain(FrontComposerUiClientSecret);
        program.IndexOf(
            "bool.TryParse(builder.Configuration[\"FrontComposerUi:OpenIdConnect:Enabled\"], out bool frontComposerOidcEnabled)",
            StringComparison.Ordinal)
            .ShouldBe(
                -1,
                "frontcomposer-ui OIDC must default on with Keycloak security present; the configuration key is an opt-out.");
    }

    [Fact]
    public void Realm_DeclaresFrontComposerUiConfidentialClientMatchingAppHostDefaults() {
        JsonElement client = FindRealmClient(FrontComposerUiClientId);

        client.GetProperty("publicClient").GetBoolean().ShouldBeFalse();
        client.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeTrue();
        client.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("secret").GetString().ShouldBe(FrontComposerUiClientSecret);

        client.GetProperty("redirectUris").EnumerateArray()
            .Select(static uri => uri.GetString())
            .ShouldContain("https://localhost:7273/signin-oidc");
        client.GetProperty("webOrigins").EnumerateArray()
            .Select(static uri => uri.GetString())
            .ShouldContain("https://localhost:7273");
    }

    [Fact]
    public void Realm_FrontComposerUiClientMapsEventStoreAudienceForAccessTokens() {
        JsonElement client = FindRealmClient(FrontComposerUiClientId);

        bool hasEventStoreAudience = client.GetProperty("protocolMappers").EnumerateArray()
            .Any(static mapper =>
                mapper.GetProperty("protocolMapper").GetString() == "oidc-audience-mapper"
                && mapper.GetProperty("config").TryGetProperty("included.client.audience", out JsonElement audience)
                && audience.GetString() == "hexalith-eventstore"
                && mapper.GetProperty("config").TryGetProperty("access.token.claim", out JsonElement accessTokenClaim)
                && accessTokenClaim.GetString() == "true");

        hasEventStoreAudience.ShouldBeTrue(
            "the FrontComposer UI client must stamp the EventStore audience into access tokens for downstream calls.");
    }

    private static JsonElement FindRealmClient(string clientId) {
        string root = FindRepoRoot();
        string realmPath = Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms", "hexalith-realm.json");
        File.Exists(realmPath).ShouldBeTrue($"Missing Keycloak realm import at {realmPath}.");

        using JsonDocument realm = JsonDocument.Parse(File.ReadAllText(realmPath));

        JsonElement? match = null;
        foreach (JsonElement client in realm.RootElement.GetProperty("clients").EnumerateArray()) {
            if (client.GetProperty("clientId").GetString() == clientId) {
                match = client.Clone();
                break;
            }
        }

        match.ShouldNotBeNull($"realm must declare the confidential '{clientId}' client.");
        return match.Value;
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
