using System.Globalization;
using System.Text.Json;

using Hexalith.FrontComposer.AppHost;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Integration;

public sealed class FrontComposerUiAppHostTests {
    private const string FrontComposerUiClientId = "hexalith-frontcomposer-ui";
    private const string FrontComposerUiClientSecret = "frontcomposer-ui-dev-secret";

    [Fact]
    public void AppHostCounterAccessibilityOverridesAreConfinedToExplicitLocalTestRuns()
    {
        string program = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));
        int gate = program.IndexOf("if (builder.ExecutionContext.IsRunMode && builder.Environment.IsEnvironment(\"Test\"))", StringComparison.Ordinal);
        gate.ShouldBeGreaterThan(0);
        int end = program.IndexOf('}', gate);
        string branch = program[gate..end];
        branch.ShouldContain("counterWeb");
        branch.ShouldContain(".WithEnvironment(\"ASPNETCORE_ENVIRONMENT\", \"Test\")");
        branch.ShouldContain(".WithEnvironment(\"Hexalith__FrontComposer__Specimens__Enabled\", \"true\")");
        program.Remove(gate, end - gate).ShouldNotContain(".WithEnvironment(\"ASPNETCORE_ENVIRONMENT\", \"Test\")");

        // The explicit Test run claims the browser lane's timings, which state evidence records as fixture options.
        string playwright = File.ReadAllText(Path.Combine(FindRepoRoot(), "tests", "e2e", "playwright.config.ts"));
        (string Key, string Value, string Lane)[] timings = [
            ("Hexalith__Shell__FormAbandonmentThresholdSeconds", "5", "Hexalith__Shell__FormAbandonmentThresholdSeconds: '5'"),
            ("Hexalith__Shell__TimeoutActionThresholdMs", "5000", "Hexalith__Shell__TimeoutActionThresholdMs: '5000'"),
            ("Hexalith__Shell__ProjectionReconnectedNoticeDurationMs", "30000", "const PROJECTION_RECONNECTED_NOTICE_DURATION_MS = '30000';"),
            ("Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs", "6500", "process.env.FC_E2E_STORY_3_6_CONFIRM_DELAY_MS ?? '6500'"),
        ];
        foreach ((string key, string value, string lane) in timings)
        {
            branch.ShouldContain($".WithEnvironment(\"{key}\", \"{value}\")");
            playwright.ShouldContain(lane);
        }
        playwright.ShouldContain("Hexalith__Shell__ProjectionReconnectedNoticeDurationMs: PROJECTION_RECONNECTED_NOTICE_DURATION_MS");
    }

    [Fact]
    public void UiAppDocumentLanguage_Source_UsesSingleCsharpAuthorityForPrerenderAndInteractive() {
        string root = FindRepoRoot();
        string app = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.UI", "Components", "App.razor"));

        // Prerender and interactive share this C# expression; absence of a JS lang writer is the
        // agreement contract (see UiAppDocumentLanguageRenderTests for rendered en/fr observation).
        app.ShouldContain("<html lang=\"@System.Globalization.CultureInfo.CurrentUICulture.Name\">");
        app.ShouldNotContain("<html lang=\"en\">");
        app.ShouldNotContain("document.documentElement.lang");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void EffectiveUiCulture_SupportedLanguage_ProducesBcp47Tag(string cultureName) {
        using CultureScope _ = new(cultureName);

        string tag = CultureInfo.CurrentUICulture.Name;

        tag.ShouldBe(cultureName);
        CultureInfo.GetCultureInfo(tag).Name.ShouldBe(tag);
    }

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
    public void AppHost_WiresPartiesWithUniqueDaprReachablePortsAndEventStoreRegistration() {
        string root = FindRepoRoot();
        string program = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));

        program.ShouldContain(".WithEndpoint(\"https\", e => e.Port = 61450, createIfNotExists: false)");
        program.ShouldContain(".WithEndpoint(\"http\", e => e.Port = 61449, createIfNotExists: false)");
        program.ShouldContain("EventStore__DomainServices__Registrations__*|party|v1__AppId");
        program.ShouldContain("Authentication__DaprInternal__AllowedCallers__1\", \"parties\"");
        program.ShouldNotContain("EventStore__DomainServices__Registrations__wildcard_party_v1");
    }

    [Fact]
    public void AppHost_StateStoreScopesPartiesForProjectionState() {
        string root = FindRepoRoot();
        string stateStore = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.FrontComposer.AppHost",
            "DaprComponents",
            "statestore.yaml"));

        stateStore.ShouldContain("  - eventstore");
        stateStore.ShouldContain("  - tenants");
        stateStore.ShouldContain("  - parties");
        stateStore.ShouldContain("  - eventstore-admin");
    }

    [Fact]
    public void AppHostTenantsSampleSuppressesRuntimeBuildAfterTheSealedPrebuild() {
        string root = FindRepoRoot();
        string metadata = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.FrontComposer.AppHost",
            "HexalithTenantsSample.cs"));

        metadata.ShouldContain("public bool SuppressBuild => true;");
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

    [Fact]
    public void LocalWorkloadRealmPreservesUserRealmAndGeneratesPrivateMatchingCredentials()
    {
        string sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms");
        string source = Path.Combine(sourceDirectory, "hexalith-realm.json");
        byte[] original = File.ReadAllBytes(source);
        string importDirectory;
        using (LocalWorkloadRealm realm = LocalWorkloadRealm.Create(sourceDirectory, false, null))
        {
            importDirectory = realm.ImportDirectory;
            string renderedPath = Path.Combine(importDirectory, "hexalith-realm.json");
            using JsonDocument before = JsonDocument.Parse(original);
            using JsonDocument after = JsonDocument.Parse(File.ReadAllText(renderedPath));
            JsonElement[] originalClients = before.RootElement.GetProperty("clients").EnumerateArray().ToArray();
            JsonElement[] renderedClients = after.RootElement.GetProperty("clients").EnumerateArray().ToArray();
            renderedClients.Length.ShouldBe(originalClients.Length + 1);
            foreach (JsonElement client in originalClients)
            {
                JsonElement preserved = renderedClients.Single(candidate => candidate.GetProperty("clientId").GetString() == client.GetProperty("clientId").GetString());
                JsonElement.DeepEquals(client, preserved).ShouldBeTrue("existing user clients must retain their exact contracts");
            }
            foreach (JsonProperty property in before.RootElement.EnumerateObject().Where(property => property.Name is not "clients" and not "clientScopes"))
            {
                JsonElement.DeepEquals(property.Value, after.RootElement.GetProperty(property.Name)).ShouldBeTrue(property.Name);
            }
            JsonElement workload = renderedClients.Single(client => client.GetProperty("clientId").GetString() == LocalWorkloadRealm.ClientId);
            string.Equals(workload.GetProperty("secret").GetString(), realm.ClientSecret, StringComparison.Ordinal).ShouldBeTrue("the rendered secret must match the parameter without printing credentials");
            realm.ClientSecret.Length.ShouldBe(64);
            Convert.FromHexString(realm.ClientSecret).Length.ShouldBe(32);
            workload.GetProperty("publicClient").GetBoolean().ShouldBeFalse();
            workload.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeTrue();
            workload.GetProperty("fullScopeAllowed").GetBoolean().ShouldBeFalse();
            workload.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse();
            workload.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeFalse();
            workload.GetProperty("implicitFlowEnabled").GetBoolean().ShouldBeFalse();
            workload.GetProperty("defaultClientScopes").GetArrayLength().ShouldBe(0);
            workload.GetProperty("attributes").GetProperty("access.token.lifespan").GetString().ShouldBe("300");
            if (!OperatingSystem.IsWindows())
            {
                File.GetUnixFileMode(importDirectory).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.GetUnixFileMode(renderedPath).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            File.ReadAllBytes(source).ShouldBe(original);
        }
        Directory.Exists(importDirectory).ShouldBeFalse();
        File.ReadAllBytes(source).ShouldBe(original);
    }

    [Fact]
    public void LocalWorkloadRealmUsesDistinctCredentialsForIndependentRuns()
    {
        string sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms");
        using LocalWorkloadRealm first = LocalWorkloadRealm.Create(sourceDirectory, false, null);
        using LocalWorkloadRealm second = LocalWorkloadRealm.Create(sourceDirectory, false, null);
        string.Equals(first.ClientSecret, second.ClientSecret, StringComparison.Ordinal).ShouldBeFalse("independent local runs must not share credentials");
        first.ImportDirectory.ShouldNotBe(second.ImportDirectory);
    }

    [Fact]
    public void LocalWorkloadRealmGrantsOnlyExplicitPlatformAudienceAndOperationScopes()
    {
        string sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms");
        using LocalWorkloadRealm realm = LocalWorkloadRealm.Create(sourceDirectory, false, null);
        using JsonDocument rendered = JsonDocument.Parse(File.ReadAllText(Path.Combine(realm.ImportDirectory, "hexalith-realm.json")));
        JsonElement[] scopes = rendered.RootElement.GetProperty("clientScopes").EnumerateArray().ToArray();
        JsonElement workload = rendered.RootElement.GetProperty("clients").EnumerateArray().Single(client => client.GetProperty("clientId").GetString() == LocalWorkloadRealm.ClientId);
        string[] optional = workload.GetProperty("optionalClientScopes").EnumerateArray().Select(scope => scope.GetString()!).ToArray();
        optional.Length.ShouldBe(9);
        scopes.Select(scope => scope.GetProperty("name").GetString()).Order().ShouldBe(optional.Order());
        foreach (string audience in new[] { "sample", "tenants", "parties", "counter-scope-fixture" })
        {
            JsonElement mapper = scopes.Single(scope => scope.GetProperty("name").GetString() == "eventstore-audience." + audience).GetProperty("protocolMappers")[0];
            mapper.GetProperty("protocolMapper").GetString().ShouldBe("oidc-audience-mapper");
            mapper.GetProperty("config").GetProperty("included.custom.audience").GetString().ShouldBe(audience);
        }
        foreach (string operation in new[] { "process", "replay-state", "query", "project", "metadata" })
        {
            JsonElement mapper = scopes.Single(scope => scope.GetProperty("name").GetString() == "eventstore-operation.domain-service." + operation).GetProperty("protocolMappers")[0];
            mapper.GetProperty("protocolMapper").GetString().ShouldBe("oidc-hardcoded-claim-mapper");
            mapper.GetProperty("config").GetProperty("claim.name").GetString().ShouldBe("eventstore:operation");
            mapper.GetProperty("config").GetProperty("claim.value").GetString().ShouldBe("domain-service:" + operation);
        }
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "configured-private-value")]
    [InlineData(false, "")]
    public void LocalWorkloadRealmRefusesPersistentReuseAndSecretOverrides(bool persistent, string? configuredSecret)
    {
        string sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms");
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => LocalWorkloadRealm.Create(sourceDirectory, persistent, configuredSecret));
        exception.Message.ShouldNotContain("configured-private-value");
        exception.Message.ShouldNotContain(sourceDirectory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LocalWorkloadRealmRefusesToReplaceExistingClientOrScope(bool clientConflict)
    {
        string fixture = Directory.CreateTempSubdirectory("fc-workload-fixture-").FullName;
        try
        {
            string source = Path.Combine(fixture, "hexalith-realm.json");
            string json = clientConflict
                ? "{\"clients\":[{\"clientId\":\"eventstore\",\"secret\":\"existing-private-value\"}]}"
                : "{\"clients\":[],\"clientScopes\":[{\"name\":\"eventstore-audience.sample\"}]}";
            File.WriteAllText(source, json);
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => LocalWorkloadRealm.Create(fixture, false, null));
            exception.Message.ShouldNotContain("existing-private-value");
            File.ReadAllText(source).ShouldBe(json);
        }
        finally
        {
            Directory.Delete(fixture, recursive: true);
        }
    }

    [Fact]
    public void LocalWorkloadRealmCleanupPreservesUnownedFiles()
    {
        string sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "KeycloakRealms");
        LocalWorkloadRealm realm = LocalWorkloadRealm.Create(sourceDirectory, false, null);
        string sentinel = Path.Combine(realm.ImportDirectory, "user-owned.txt");
        try
        {
            File.WriteAllText(sentinel, "preserve");
            Should.Throw<IOException>(realm.Dispose);
            File.ReadAllText(sentinel).ShouldBe("preserve");
            File.Exists(Path.Combine(realm.ImportDirectory, "hexalith-realm.json")).ShouldBeTrue();
        }
        finally
        {
            File.Delete(sentinel);
            realm.Dispose();
        }
        Directory.Exists(realm.ImportDirectory).ShouldBeFalse();
    }

    [Fact]
    public void AppHostWorkloadWiringIsLocalAndUsesTheRenderedSecretParameter()
    {
        string program = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));
        program.ShouldContain("bool localKeycloak = builder.ExecutionContext.IsRunMode");
        program.ShouldContain("builder.Configuration[\"EnableKeycloak\"]");
        program.ShouldContain("using LocalWorkloadRealm? workloadRealm = localKeycloak");
        program.ShouldContain("builder.Configuration[\"KeycloakPersistent\"]");
        program.ShouldContain("builder.Configuration[\"Parameters:\" + LocalWorkloadRealm.SecretParameterName]");
        program.ShouldContain("RealmImportPath = workloadRealm?.ImportDirectory");
        int binding = program.IndexOf("IResourceBuilder<ParameterResource> workloadSecret", StringComparison.Ordinal);
        int end = program.IndexOf("_ = tenants.WithJwtBearerSecurity", binding, StringComparison.Ordinal);
        string wiring = program[binding..end];
        wiring.ShouldContain("() => workloadRealm.ClientSecret");
        wiring.ShouldContain("secret: true");
        wiring.ShouldContain("publishValueAsDefault: false");
        wiring.ShouldContain("eventStore.WithEventStoreWorkloadClientCredentials(LocalWorkloadRealm.ClientId, workloadSecret)");
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
