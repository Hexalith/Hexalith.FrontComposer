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
            after.RootElement.GetProperty("attributes").GetProperty("CreateDefaultClientScopes").GetString().ShouldBe("true",
                "adding workload scopes must preserve Keycloak's built-in profile/email scopes for existing user clients");
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

    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public void LocalWorkloadRealmPreservesExplicitDefaultScopeConfiguration(string configuredValue)
    {
        string fixture = Directory.CreateTempSubdirectory("fc-workload-fixture-").FullName;
        try
        {
            string source = Path.Combine(fixture, "hexalith-realm.json");
            string json = "{\"clients\":[],\"attributes\":{\"CreateDefaultClientScopes\":\"" + configuredValue + "\",\"existing\":\"preserved\"}}";
            File.WriteAllText(source, json);
            using LocalWorkloadRealm realm = LocalWorkloadRealm.Create(fixture, false, null);
            using JsonDocument rendered = JsonDocument.Parse(File.ReadAllText(Path.Combine(realm.ImportDirectory, "hexalith-realm.json")));
            JsonElement attributes = rendered.RootElement.GetProperty("attributes");
            attributes.GetProperty("CreateDefaultClientScopes").GetString().ShouldBe(configuredValue);
            attributes.GetProperty("existing").GetString().ShouldBe("preserved");
            File.ReadAllText(source).ShouldBe(json);
        }
        finally
        {
            Directory.Delete(fixture, recursive: true);
        }
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
        program.ShouldContain("using LocalWorkloadRealm? workloadRealm = localKeycloak && !persistentKeycloak");
        program.ShouldContain("if (disposableVerification && persistentKeycloak)");
        program.IndexOf("if (disposableVerification && persistentKeycloak)", StringComparison.Ordinal)
            .ShouldBeLessThan(program.IndexOf("LocalWorkloadRealm.Create(", StringComparison.Ordinal));
        program.ShouldContain("if (workloadRealm is not null)");
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

    [Fact]
    public async Task DisposableReadinessDoesNotFollowHealthRedirectsAsync()
    {
        using System.Net.Sockets.TcpListener reservation = new(System.Net.IPAddress.Loopback, 0);
        reservation.Start();
        int port = ((System.Net.IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using System.Net.HttpListener listener = new();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        int requests = 0;
        Task server = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                System.Net.HttpListenerContext request;
                try { request = await listener.GetContextAsync().ConfigureAwait(true); }
                catch (System.Net.HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                Interlocked.Increment(ref requests);
                request.Response.StatusCode = 302;
                request.Response.RedirectLocation = $"http://localhost:{port}/redirected";
                request.Response.Close();
            }
        }, TestContext.Current.CancellationToken);
        try
        {
            LocalReadinessHealthCheck check = new(() => $"http://localhost:{port}", maintenance: false);
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult result = await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            result.Status.ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
            requests.ShouldBe(1);
        }
        finally
        {
            listener.Stop();
            await server.ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposableReadinessRequiresEveryPlatformComponentAsync(bool maintenance)
    {
        string[] names = ["self", "dapr-sidecar", "dapr-actor-placement", "dapr-statestore", "dapr-pubsub", "dapr-configstore",
            "dapr-app-channel-token", "projection-delivery-writer-protocol", "projection-delivery-unresolved-conflict"];
        Dictionary<string, object> entries = names.ToDictionary(name => name,
            name => (object)new { status = maintenance && name == "projection-delivery-writer-protocol" ? "Unhealthy" : "Healthy" });
        entries["additional-component"] = new { status = "Healthy" };
        int statusCode = maintenance ? 503 : 200;
        string status = maintenance ? "Unhealthy" : "Healthy";
        (await ObserveReadinessAsync(maintenance, statusCode, JsonSerializer.Serialize(new { status, results = entries })).ConfigureAwait(true))
            .ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        foreach (string missing in names)
        {
            Dictionary<string, object> incomplete = new(entries);
            _ = incomplete.Remove(missing);
            (await ObserveReadinessAsync(maintenance, statusCode, JsonSerializer.Serialize(new { status, results = incomplete })).ConfigureAwait(true))
                .ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, missing);
        }
        entries["additional-component"] = new { status = "Degraded" };
        (await ObserveReadinessAsync(maintenance, statusCode, JsonSerializer.Serialize(new { status, results = entries })).ConfigureAwait(true))
            .ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
    }

    [Theory]
    [InlineData(503, "{\"status\":\"Healthy\"}")]
    [InlineData(200, "{\"status\":\"Unhealthy\"}")]
    [InlineData(200, "[]")]
    [InlineData(200, "not-json")]
    public async Task DisposableReadinessRejectsInvalidReadyResponsesAsync(int readyStatus, string readyBody)
    {
        string[] names = ["self", "dapr-sidecar", "dapr-actor-placement", "dapr-statestore", "dapr-pubsub", "dapr-configstore",
            "dapr-app-channel-token", "projection-delivery-writer-protocol", "projection-delivery-unresolved-conflict"];
        string health = JsonSerializer.Serialize(new { status = "Healthy", results = names.ToDictionary(name => name, _ => new { status = "Healthy" }) });
        (await ObserveReadinessAsync(false, 200, health, readyStatus, readyBody).ConfigureAwait(true))
            .ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
    }

    private static async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus> ObserveReadinessAsync(
        bool maintenance, int healthStatus, string healthBody, int readyStatus = 200, string readyBody = "{\"status\":\"Healthy\"}")
    {
        using System.Net.Sockets.TcpListener reservation = new(System.Net.IPAddress.Loopback, 0);
        reservation.Start();
        int port = ((System.Net.IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using System.Net.HttpListener listener = new();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Task server = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                System.Net.HttpListenerContext request;
                try { request = await listener.GetContextAsync().ConfigureAwait(true); }
                catch (System.Net.HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                bool health = request.Request.Url?.AbsolutePath == "/health";
                request.Response.StatusCode = health ? healthStatus : readyStatus;
                byte[] body = System.Text.Encoding.UTF8.GetBytes(health ? healthBody : readyBody);
                await request.Response.OutputStream.WriteAsync(body, TestContext.Current.CancellationToken).ConfigureAwait(true);
                request.Response.Close();
            }
        }, TestContext.Current.CancellationToken);
        try
        {
            LocalReadinessHealthCheck check = new(() => $"http://localhost:{port}", maintenance);
            return (await check.CheckHealthAsync(new(), TestContext.Current.CancellationToken).ConfigureAwait(true)).Status;
        }
        finally
        {
            listener.Stop();
            await server.ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(6379, "1.18.7")]
    [InlineData(41001, "untrusted:latest")]
    public void DisposableReadinessRefusesSharedPortsOrUnknownRuntime(int redisPort, string version)
    {
        string directory = Directory.CreateTempSubdirectory("fc-readiness-test-").FullName;
        try
        {
            string path = Path.Combine(directory, "configuration.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                owner = new string('a', 32), daprVersion = version,
                ports = new { redis = redisPort, placement = 41002, scheduler = 41003, daprHttp = 41004 },
            }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.Load(directory));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("gateway", "gateway", false)]
    [InlineData("full", "full", true)]
    public void DisposableReadinessRequiresExplicitFullSelection(string? topology, string? selection, bool full)
    {
        string directory = CreateReadinessConfiguration(topology);
        try
        {
            LocalReadinessConfiguration configuration = LocalReadinessConfiguration.Load(directory);
            configuration.UseFullTopology(selection, maintenance: false).ShouldBe(full);
            configuration.UseFullTopology(selection, maintenance: true).ShouldBeFalse("maintenance must never compose full writers");
            Should.Throw<InvalidOperationException>(() => configuration.UseFullTopology(full ? null : "full", maintenance: false));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("other")]
    [InlineData("FULL")]
    [InlineData("")]
    public void DisposableReadinessRefusesUnknownTopology(string topology)
    {
        string directory = CreateReadinessConfiguration(topology);
        try { Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.Load(directory)); }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposableFullTopologyRequiresOwnedActivationReadback(bool wrongOwner)
    {
        string directory = CreateReadinessConfiguration("full");
        try
        {
            LocalReadinessConfiguration configuration = LocalReadinessConfiguration.Load(directory);
            Should.Throw<FileNotFoundException>(configuration.RequireActivation);
            string path = Path.Combine(directory, "activation.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                owner = new string(wrongOwner ? 'b' : 'a', 32), freshStoreVerified = true, schemaVersion = 1,
                writerProtocolVersion = 2, cutoverCommit = new string('c', 40), activatedAt = "2026-10-28T00:00:00Z",
            }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            if (wrongOwner) Should.Throw<InvalidOperationException>(configuration.RequireActivation);
            else configuration.RequireActivation();
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisposableFullTopologyRefusesEnabledMemoriesIndexing(bool enabled)
    {
        string directory = Directory.CreateTempSubdirectory("fc-memories-config-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{\"Parties\":{\"MemoriesSearch\":{\"Enabled\":false}}}");
            File.WriteAllText(Path.Combine(directory, "appsettings.Development.json"), JsonSerializer.Serialize(new { Parties = new { MemoriesSearch = new { Enabled = enabled } } }));
            if (enabled) Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.RequireDisabledMemories(directory));
            else LocalReadinessConfiguration.RequireDisabledMemories(directory);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void DisposableFullTopologyRefusesActivationWithoutFreshStateProof()
    {
        string directory = CreateReadinessConfiguration("full");
        try
        {
            string path = Path.Combine(directory, "activation.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new { owner = new string('a', 32), freshStoreVerified = false,
                schemaVersion = 1, writerProtocolVersion = 2, cutoverCommit = new string('c', 40), activatedAt = "2026-10-28T00:00:00Z" }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.Load(directory).RequireActivation());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void DisposableFullTopologyUsesTheOrdinaryGraphAfterItsActivationGuard()
    {
        string program = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));
        int activation = program.IndexOf("if (fullVerification) verification.RequireActivation();", StringComparison.Ordinal);
        int realm = program.IndexOf("LocalWorkloadRealm.Create(", StringComparison.Ordinal);
        int ordinary = program.IndexOf("IResourceBuilder<ProjectResource> eventStore =", StringComparison.Ordinal);
        activation.ShouldBeGreaterThan(0);
        activation.ShouldBeLessThan(realm, "activation and isolation must be checked before any realm/infrastructure mutation");
        string gatewayBranch = program[program.IndexOf("if (disposableVerification && !fullVerification)", StringComparison.Ordinal)..ordinary];
        gatewayBranch.ShouldContain("if (!maintenance || !verification!.FullTopology)");
        gatewayBranch.ShouldContain("return;");
        foreach (string application in new[] { "eventstore", "eventstore-admin", "eventstore-admin-ui", "tenants", "parties", "sample", "tenants-api", "tenants-ui", "frontcomposer-ui", "counter-web" })
        {
            program[ordinary..].ShouldContain("(\"" + application + "\")");
        }
        program[ordinary..].ShouldContain("LocalReadinessTopology.IsolateSidecars(builder, verification)");
        program[ordinary..].ShouldContain("pubSubComponentPath: verification?.FilePath(\"components/pubsub.yaml\")");
        program[ordinary..].ShouldContain("TenantScopeFixture:Enabled");
        program.ShouldContain("LocalReadinessConfiguration.RequireDisabledMemories(");
        program[ordinary..].ShouldContain(".WithEnvironment(\"Parties__MemoriesSearch__Endpoint\", string.Empty)");
        program[ordinary..].ShouldContain(".WithEnvironment(\"Parties__MemoriesSearch__Enabled\", \"false\")");
        program.ShouldContain(".WithContainerName(\"fc-verify-\" + verification.Owner + \"-identity\")");
        program.ShouldContain(".WithContainerRuntimeArgs(\"--label\", \"hexalith.frontcomposer.verification=\" + verification.Owner)");
        program[ordinary..].ShouldContain(".WithHttpHealthCheck(\"/health\", endpointName: \"http\")");
        program[ordinary..].ShouldContain("application.Name == \"sample\" ? \"/alive\" : \"/ready\"");
    }

    [Fact]
    public void TenantsApiAndBffsUseOwnedInvocationSidecarsAndValidatedUserBearer()
    {
        string directory = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost");
        string program = File.ReadAllText(Path.Combine(directory, "Program.cs"));
        foreach (string name in new[] { "tenants-api", "tenants-ui", "frontcomposer-ui" })
        {
            program.ShouldContain("AppId = \"" + name + "\"");
            program.ShouldContain("accesscontrol." + name + ".yaml");
        }
        program.ShouldContain("tenantsApi.WithEventStoreAuthenticationValidation(security)");
        program.ShouldContain(".WithEnvironment(\"EventStore__GatewayReadiness__BaseAddress\", eventStore.GetEndpoint(\"http\"))");
        program.ShouldContain(".WithEnvironment(\"Tenants__BaseAddress\", tenantsApiHttps)");
        string project = File.ReadAllText(Path.Combine(directory, "Hexalith.FrontComposer.AppHost.csproj"));
        project.ShouldContain("$(TenantsPath)/src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj");
        string metadata = File.ReadAllText(Path.Combine(directory, "HexalithTenantsApi.cs"));
        metadata.ShouldContain("public bool SuppressBuild => true");
        metadata.ShouldContain("Hexalith.Tenants.Api.csproj");
        foreach (string name in new[] { "tenants-ui", "frontcomposer-ui" })
        {
            string receiver = File.ReadAllText(Path.Combine(directory, "DaprComponents", "accesscontrol." + name + ".yaml"));
            receiver.ShouldContain("defaultAction: deny");
            receiver.ShouldNotContain("operations:");
        }
    }

    [Theory]
    [InlineData("statestore", "redisHost", "localhost:6379")]
    [InlineData("pubsub", "redisHost", "localhost:6379")]
    [InlineData("configstore", "redisHost", "localhost:6379")]
    [InlineData("statestore", "redisPassword", "shared")]
    public void DisposableIsolationRefusesEverySharedComponentPath(string component, string metadataName, string value)
    {
        string directory = CreateReadinessConfiguration("full");
        try
        {
            WriteReadinessIsolationFiles(directory);
            string appHost = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost");
            LocalReadinessConfiguration configuration = LocalReadinessConfiguration.Load(directory);
            configuration.ValidateIsolation(appHost);
            string path = Path.Combine(directory, "components", component + ".yaml");
            System.Text.Json.Nodes.JsonObject document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            System.Text.Json.Nodes.JsonNode entry = document["spec"]!["metadata"]!.AsArray().Single(item => item!["name"]!.GetValue<string>() == metadataName)!;
            entry["value"] = value;
            File.WriteAllText(path, document.ToJsonString());
            Should.Throw<InvalidOperationException>(() => configuration.ValidateIsolation(appHost));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("accesscontrol.yaml")]
    [InlineData("accesscontrol.eventstore-admin.yaml")]
    [InlineData("accesscontrol.sample.yaml")]
    [InlineData("accesscontrol.tenants-api.yaml")]
    [InlineData("accesscontrol.tenants-ui.yaml")]
    [InlineData("accesscontrol.frontcomposer-ui.yaml")]
    public void DisposableIsolationRefusesEverySharedDiscoveryPath(string file)
    {
        string directory = CreateReadinessConfiguration("full");
        try
        {
            WriteReadinessIsolationFiles(directory);
            string appHost = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost");
            File.Copy(Path.Combine(appHost, "DaprComponents", file), Path.Combine(directory, file), overwrite: true);
            Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.Load(directory).ValidateIsolation(appHost));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void DisposableIsolationRefusesRedisConfigurationEscapes()
    {
        string directory = CreateReadinessConfiguration("full");
        try
        {
            WriteReadinessIsolationFiles(directory);
            File.AppendAllText(Path.Combine(directory, "redis.conf"), "replicaof shared.invalid 6379\n");
            Should.Throw<InvalidOperationException>(() => LocalReadinessConfiguration.Load(directory)
                .ValidateIsolation(Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost")));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static string CreateReadinessConfiguration(string? topology)
    {
        string directory = Directory.CreateTempSubdirectory("fc-full-readiness-").FullName;
        string path = Path.Combine(directory, "configuration.json");
        Dictionary<string, object> configuration = new()
        {
            ["owner"] = new string('a', 32), ["daprVersion"] = "1.18.7", ["redisPassword"] = new string('d', 64),
            ["ports"] = new { redis = 41001, placement = 41002, scheduler = 41003, daprHttp = 41004 },
        };
        if (topology is not null) configuration["topology"] = topology;
        File.WriteAllText(path, JsonSerializer.Serialize(configuration));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return directory;
    }

    private static void WriteReadinessIsolationFiles(string directory)
    {
        string redisPath = Path.Combine(directory, "redis.conf");
        File.WriteAllText(redisPath, $"bind 0.0.0.0\nprotected-mode yes\nrequirepass {new string('d', 64)}\nsave \"\"\ndir /data\ndbfilename dump.rdb\n");
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(redisPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        foreach (string name in new[] { "components", "runtime", "empty" })
        {
            string path = Path.Combine(directory, name);
            Directory.CreateDirectory(path);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        foreach ((string name, string type, string[] scopes) in new (string, string, string[])[]
        {
            ("statestore", "state.redis", ["eventstore", "eventstore-admin", "tenants", "parties"]),
            ("pubsub", "pubsub.redis", ["eventstore", "tenants", "parties"]), ("configstore", "configuration.redis", ["eventstore"]),
        })
        {
            List<object> metadata = [new { name = "redisHost", value = "127.0.0.1:41001" }, new { name = "redisPassword", value = new string('d', 64) }];
            if (name == "statestore") { metadata.Add(new { name = "actorStateStore", value = "true" }); metadata.Add(new { name = "keyPrefix", value = "none" }); }
            string path = Path.Combine(directory, "components", name + ".yaml");
            File.WriteAllText(path, JsonSerializer.Serialize(new { apiVersion = "dapr.io/v1alpha1", kind = "Component", metadata = new { name }, spec = new { type, version = "v1", metadata }, scopes }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        foreach (string name in new[] { "accesscontrol.yaml", "accesscontrol.eventstore-admin.yaml", "accesscontrol.sample.yaml",
            "accesscontrol.tenants-api.yaml", "accesscontrol.tenants-ui.yaml", "accesscontrol.frontcomposer-ui.yaml" })
        {
            string source = Path.Combine(FindRepoRoot(), "src", "Hexalith.FrontComposer.AppHost", "DaprComponents", name);
            string path = Path.Combine(directory, name);
            File.WriteAllText(path, File.ReadAllText(source).Replace("connectionString: \"nr.db\"", "connectionString: " + JsonSerializer.Serialize(Path.Combine(directory, "runtime", "nr.db")), StringComparison.Ordinal));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
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
