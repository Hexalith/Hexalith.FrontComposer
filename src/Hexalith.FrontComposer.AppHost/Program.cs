using Microsoft.Extensions.Hosting;

using Hexalith.EventStore.Aspire;
using Hexalith.FrontComposer.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control + resiliency configuration paths.
// Uses builder.AppHostDirectory to work under both `dotnet run` and Aspire testing.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.eventstore-admin.yaml");
string sampleAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.sample.yaml");
string resiliencyConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "resiliency.yaml");
string stateStoreComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "statestore.yaml");
string emptyDaprResourcesPath = ResolveEmptyDaprResourcesPath();
(string? daprPlacementHostAddress, string? daprSchedulerHostAddress) = AspireDaprLocalServiceEndpoints.Resolve(
    builder.Configuration[AspireDaprLocalServiceEndpoints.PlacementHostAddressKey],
    builder.Configuration[AspireDaprLocalServiceEndpoints.SchedulerHostAddressKey]);

// Keycloak-backed security resource for JWT/OIDC authentication.
// Enabled by default for local development with real OIDC token testing.
// Set EnableKeycloak=false in environment or appsettings to run without Keycloak
// (falls back to symmetric key auth via Authentication:JwtBearer:SigningKey).
bool localKeycloak = builder.ExecutionContext.IsRunMode
    && !string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase);
bool persistentKeycloak = bool.TryParse(builder.Configuration["KeycloakPersistent"], out bool persistent) && persistent;
bool disposableVerification = string.Equals(builder.Configuration[LocalReadinessConfiguration.Prefix + ":Enabled"], "true", StringComparison.OrdinalIgnoreCase);
if (disposableVerification && persistentKeycloak)
{
    throw new InvalidOperationException("Disposable verification refuses persistent Keycloak reuse.");
}
LocalReadinessConfiguration? verification = disposableVerification
    ? LocalReadinessConfiguration.Load(builder.Configuration[LocalReadinessConfiguration.Prefix + ":Directory"])
    : null;
bool maintenance = string.Equals(builder.Configuration[LocalReadinessConfiguration.Prefix + ":Maintenance"], "true", StringComparison.OrdinalIgnoreCase);
bool fullVerification = verification?.UseFullTopology(builder.Configuration[LocalReadinessConfiguration.Prefix + ":Topology"], maintenance) == true;
if (verification is not null)
{
    verification.ValidateIsolation(builder.AppHostDirectory);
    if (verification.FullTopology)
        LocalReadinessConfiguration.RequireDisabledMemories(Path.GetDirectoryName(new HexalithParties().ProjectPath)!);
    if (fullVerification) verification.RequireActivation();
}
// Existing persistent realms keep their operator-provisioned credentials and import path.
using LocalWorkloadRealm? workloadRealm = localKeycloak && !persistentKeycloak
    ? LocalWorkloadRealm.Create(
        Path.Combine(builder.AppHostDirectory, "KeycloakRealms"),
        persistentKeycloak,
        builder.Configuration["Parameters:" + LocalWorkloadRealm.SecretParameterName])
    : null;
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity(
    new HexalithEventStoreSecurityOptions
    {
        RealmImportPath = workloadRealm?.ImportDirectory ?? Path.Combine(builder.AppHostDirectory, "KeycloakRealms"),
    });

if (verification is not null && security is not null)
{
    _ = security.Keycloak
        .WithContainerName("fc-verify-" + verification.Owner + "-identity")
        .WithContainerRuntimeArgs("--label", "hexalith.frontcomposer.verification=" + verification.Owner);
}

// Explicit verification uses a disposable gateway; the normal development topology below is unchanged.
if (disposableVerification && !fullVerification)
{
    if (!builder.ExecutionContext.IsRunMode || security is null || workloadRealm is null)
    {
        throw new InvalidOperationException("Disposable verification requires local Keycloak run mode.");
    }
    IResourceBuilder<ProjectResource> verificationGateway = LocalReadinessTopology.Add(builder, verification!, security, workloadRealm, maintenance);
    if (!maintenance || !verification!.FullTopology)
    {
        _ = builder.AddProject<Projects.Counter_Web>("counter-web")
            .WaitFor(verificationGateway)
            .WithExternalHttpEndpoints();
    }
    await builder.Build().RunAsync().ConfigureAwait(false);
    return;
}

// Full verification flows through the ordinary composition below. Only its
// infrastructure inputs and sidecar paths change; modules and security stay ordinary.
if (verification is not null)
{
    if (!builder.ExecutionContext.IsRunMode || security is null || workloadRealm is null)
        throw new InvalidOperationException("Disposable verification requires local Keycloak run mode.");
    LocalReadinessTopology.AddInfrastructure(builder, verification);
    accessControlConfigPath = verification.FilePath("accesscontrol.yaml");
    adminServerAccessControlConfigPath = verification.FilePath("accesscontrol.eventstore-admin.yaml");
    sampleAccessControlConfigPath = verification.FilePath("accesscontrol.sample.yaml");
    stateStoreComponentPath = verification.FilePath("components/statestore.yaml");
    emptyDaprResourcesPath = verification.FilePath("empty");
    daprPlacementHostAddress = "127.0.0.1:" + verification.Port("placement");
    daprSchedulerHostAddress = "127.0.0.1:" + verification.Port("scheduler");
}

// Add EventStore (command gateway), Admin Server, and Admin UI projects.
// Project paths are resolved cross-repo via the IProjectMetadata classes in this AppHost.
IResourceBuilder<ProjectResource> eventStore = builder.AddProject<HexalithEventStore>("eventstore");
_ = eventStore
    .WithEnvironment("Authentication__DaprInternal__AllowedCallers__0", "tenants")
    .WithEnvironment("Authentication__DaprInternal__AllowedCallers__1", "parties")
    .WithEnvironment("EventStore__Publisher__TopicOverrides__global-administrators", "tenants.events")
    .WithEnvironment("EventStore__SignalR__Enabled", "true")
    .WithEnvironment("EventStore__DomainServices__Registrations__*|party|v1__AppId", "parties")
    .WithEnvironment("EventStore__DomainServices__Registrations__*|party|v1__MethodName", "process")
    .WithEnvironment("EventStore__DomainServices__Registrations__*|party|v1__TenantId", "*")
    .WithEnvironment("EventStore__DomainServices__Registrations__*|party|v1__Domain", "party")
    .WithEnvironment("EventStore__DomainServices__Registrations__*|party|v1__Version", "v1");
IResourceBuilder<ProjectResource> adminServer = builder.AddProject<HexalithEventStoreAdminServerHost>("eventstore-admin");
IResourceBuilder<ProjectResource> adminUI = builder.AddProject<HexalithEventStoreAdminUI>("eventstore-admin-ui");

// Wire the EventStore + Admin DAPR topology (shared state store + pub/sub, sidecars, resiliency)
// using the platform Aspire extension — the reusable boilerplate lives in the EventStore platform
// Aspire library rather than a per-domain re-implementation.
HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
    eventStore,
    adminServer,
    adminUI,
    eventStoreDaprConfigPath: accessControlConfigPath,
    adminServerDaprConfigPath: adminServerAccessControlConfigPath,
    resiliencyConfigPath: resiliencyConfigPath,
    stateStoreComponentPath: stateStoreComponentPath,
    daprPlacementHostAddress: daprPlacementHostAddress,
    daprSchedulerHostAddress: daprSchedulerHostAddress,
    eventStoreDaprHttpPort: verification?.Port("daprHttp") ?? 3501,
    pubSubComponentPath: verification?.FilePath("components/pubsub.yaml"));

// Add the Tenants domain service via the platform domain-module extension: its sidecar shares the
// EventStore state store + pub/sub.
IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants")
    .AddEventStoreDomainModule(
        eventStoreResources,
        "tenants",
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

IResourceBuilder<ProjectResource> parties = builder.AddProject<HexalithParties>("parties")
    // Hexalith.Parties currently carries the EventStore launchSettings ports (8080/7141).
    // Pin unique proxy ports here so DAPR invokes the Parties app instead of EventStore.
    .WithEndpoint("https", e => e.Port = 61450, createIfNotExists: false)
    .WithEndpoint("http", e => e.Port = 61449, createIfNotExists: false)
    .AddEventStoreDomainModule(
        eventStoreResources,
        "parties",
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress)
    .WithReference(tenants)
    .WaitFor(tenants)
    .WithEnvironment("Tenants__Enabled", "true")
    .WithEnvironment("Tenants__ServiceName", "tenants")
    .WithEnvironment("Tenants__CommandApiAppId", "eventstore")
    .WithEnvironment("Tenants__PubSubName", "pubsub")
    .WithEnvironment("Tenants__TopicName", "system.tenants.events");

// Tenants sample domain service. EventStore's appsettings register the sample/counter/greeting
// (and orders/inventory) domains against the "sample" app id; its sidecar shares the EventStore
// state store + pub/sub. Without it, the EventStore admin operational-index poll to "sample" fails
// at startup (500) and the whole index build is skipped.
_ = builder.AddProject<HexalithTenantsSample>("sample")
    .AddEventStoreDomainModule(
        eventStoreResources,
        "sample",
        sampleAccessControlConfigPath,
        isolatedDaprResourcesPath: emptyDaprResourcesPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

// Opt-in tenant-scope proof fixture. Keep the ordinary Tenants sample resource intact.
if (string.Equals(builder.Configuration["TenantScopeFixture:Enabled"], "true", StringComparison.OrdinalIgnoreCase)) {
    _ = eventStore
        .WithEnvironment("EventStore__DomainServices__Registrations__*|counter|v1__AppId", "counter-scope-fixture")
        .WithEnvironment("EventStore__DomainServices__Registrations__*|counter|v1__MethodName", "process")
        .WithEnvironment("EventStore__DomainServices__Registrations__*|counter|v1__TenantId", "*")
        .WithEnvironment("EventStore__DomainServices__Registrations__*|counter|v1__Domain", "counter")
        .WithEnvironment("EventStore__DomainServices__Registrations__*|counter|v1__Version", "v1");
    _ = builder.AddProject<Projects.Hexalith_FrontComposer_CounterFixture>("counter-scope-fixture")
        .AddEventStoreDomainModule(
            eventStoreResources,
            "counter-scope-fixture",
            sampleAccessControlConfigPath,
            isolatedDaprResourcesPath: emptyDaprResourcesPath,
            daprPlacementHostAddress: daprPlacementHostAddress,
            daprSchedulerHostAddress: daprSchedulerHostAddress);
}

// Wire Admin.UI to Admin.Server + EventStore SignalR (domain-agnostic composition kept in the AppHost).
EndpointReference adminServerHttps = adminServer.GetEndpoint("https");
EndpointReference eventStoreHttps = eventStore.GetEndpoint("https");
EndpointReference tenantsHttps = tenants.GetEndpoint("https");
_ = adminUI
    .WithReference(adminServer)
    .WaitFor(adminServer)
    .WithEnvironment("EventStore__SignalR__HubUrl", ReferenceExpression.Create($"{eventStoreHttps}/hubs/projection-changes"))
    .WithExternalHttpEndpoints();

// The Tenants UI is the primary front end — it renders the FrontComposer Shell/Contracts components
// and talks to the Tenants domain + EventStore gateways.
IResourceBuilder<ProjectResource> tenantsUI = builder.AddProject<HexalithTenantsUI>("tenants-ui")
    // Pin the host (proxy) ports to a low, fixed range. The project's launchSettings applicationUrl
    // uses 62445/62448, which can land inside a Windows WinNAT/Hyper-V reserved port range
    // (`netsh interface ipv4 show excludedportrange protocol=tcp`). When that happens the DCP proxy
    // cannot bind the port and the endpoint refuses connections (ERR_CONNECTION_REFUSED) even though
    // the app is healthy on its internal port. Low fixed ports avoid the high ephemeral reservation
    // blocks. Overridden here (not in the submodule's launchSettings) to keep the change repo-local.
    .WithEndpoint("https", e => e.Port = 7271, createIfNotExists: false)
    .WithEndpoint("http", e => e.Port = 7272, createIfNotExists: false)
    .WithReference(tenants)
    .WithReference(eventStore)
    .WaitFor(tenants)
    .WaitFor(eventStore)
    .WithEnvironment("Tenants__BaseAddress", tenantsHttps)
    .WithEnvironment("EventStore__BaseAddress", eventStoreHttps)
    .WithExternalHttpEndpoints();

IResourceBuilder<ProjectResource> frontComposerUI = builder.AddProject<Projects.Hexalith_FrontComposer_UI>("frontcomposer-ui")
    .WithEndpoint("https", e => e.Port = 7273, createIfNotExists: true)
    .WithEndpoint("http", e => e.Port = 7274, createIfNotExists: true)
    .WithReference(tenants)
    .WithReference(parties)
    .WithReference(eventStore)
    .WaitFor(tenants)
    .WaitFor(parties)
    .WaitFor(eventStore)
    .WithEnvironment("Tenants__BaseAddress", tenantsHttps)
    .WithEnvironment("EventStore__BaseAddress", eventStoreHttps)
    .WithEnvironment("EventStore__SignalR__HubUrl", ReferenceExpression.Create($"{eventStoreHttps}/hubs/projection-changes"))
    .WithEnvironment("Parties__BaseUrl", eventStoreHttps)
    .WithEnvironment("Parties__Tenant", builder.Configuration["FrontComposerUi:Parties:Tenant"] ?? "tenant-a")
    .WithExternalHttpEndpoints();

// Counter sample — the FrontComposer demo shell. Co-hosted in the single platform AppHost so the
// whole stack (EventStore, Tenants, Tenants UI, Counter) runs from ONE orchestrator. It stays a
// self-contained demo (fake auth + seeded specimen data) and is intentionally NOT wired to the
// EventStore/Tenants/Keycloak backend, to keep the a11y/visual specimen gate deterministic.
IResourceBuilder<ProjectResource> counterWeb = builder.AddProject<Projects.Counter_Web>("counter-web")
    .WithExternalHttpEndpoints();
if (builder.ExecutionContext.IsRunMode && builder.Environment.IsEnvironment("Test"))
{
    // Launch profiles keep the normal demo in Development. The explicit local Test run
    // must use the same gated specimen services and timings as the browser verification lane.
    _ = counterWeb
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Test")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Test")
        .WithEnvironment("Hexalith__FrontComposer__Specimens__Enabled", "true")
        .WithEnvironment("Hexalith__Shell__FormAbandonmentThresholdSeconds", "5")
        .WithEnvironment("Hexalith__Shell__TimeoutActionThresholdMs", "5000")
        .WithEnvironment("Hexalith__Shell__ProjectionReconnectedNoticeDurationMs", "30000")
        .WithEnvironment("Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs", "6500");
}

// Wire Keycloak auth to EventStore, Tenants, Admin.Server, Admin.UI, and Tenants.UI if enabled.
if (security is not null) {
    _ = eventStore.WithJwtBearerSecurity(security);
    if (workloadRealm is not null)
    {
        IResourceBuilder<ParameterResource> workloadSecret = builder.AddParameter(
            LocalWorkloadRealm.SecretParameterName,
            () => workloadRealm.ClientSecret,
            secret: true,
            publishValueAsDefault: false);
        _ = eventStore.WithEventStoreWorkloadClientCredentials(LocalWorkloadRealm.ClientId, workloadSecret);
    }

    _ = tenants.WithJwtBearerSecurity(security);

    _ = parties.WithJwtBearerSecurity(security);

    _ = adminServer.WithJwtBearerSecurity(security);

    // EventStore 3.103+ WithEventStoreClientCredentials(security) creates required
    // secret parameters with no values. Non-interactive aspire start then leaves
    // eventstore-admin-ui Waiting/ValueMissing. Bind the Keycloak realm local
    // identity already shipped in KeycloakRealms/hexalith-realm.json.
    IResourceBuilder<ParameterResource> adminUiUsername = builder.AddParameter(
        "eventstore-admin-ui-local-auth-username",
        static () => "admin-user",
        secret: true);
    IResourceBuilder<ParameterResource> adminUiPassword = builder.AddParameter(
        "eventstore-admin-ui-local-auth-password",
        static () => "admin-pass",
        secret: true);

    _ = adminUI
        .WithEventStoreClientCredentials(
            security,
            HexalithEventStoreSecurityOptions.DefaultEventStoreClientId,
            adminUiUsername,
            adminUiPassword)
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));

    // Interactive browser sign-in (authorization-code flow) for the Tenants UI. Uses a
    // confidential Keycloak client; the relayed access token carries the hexalith-eventstore
    // audience so EventStore gateway calls authorize per-user.
    _ = tenantsUI
        .WithJwtBearerSecurity(security)
        .WithOpenIdConnectSecurity(
            security,
            "hexalith-tenants-ui",
            "tenants-ui-dev-secret");

    _ = frontComposerUI.WithJwtBearerSecurity(security);
    bool frontComposerOidcEnabled = !bool.TryParse(
        builder.Configuration["FrontComposerUi:OpenIdConnect:Enabled"],
        out bool frontComposerOidcEnabledOverride)
        || frontComposerOidcEnabledOverride;
    if (frontComposerOidcEnabled) {
        _ = frontComposerUI.WithOpenIdConnectSecurity(
            security,
            builder.Configuration["FrontComposerUi:OpenIdConnect:ClientId"] ?? "hexalith-frontcomposer-ui",
            builder.Configuration["FrontComposerUi:OpenIdConnect:ClientSecret"] ?? "frontcomposer-ui-dev-secret");
    }
}
else {
    _ = adminUI.WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));
}

if (verification is not null)
{
    // Fresh owned state has no prior remote obligations. Keep the ordinary disabled
    // indexing feature, but remove Development's dormant shared Memories route.
    _ = parties.WithEnvironment("Parties__MemoriesSearch__Enabled", "false")
        .WithEnvironment("Parties__MemoriesSearch__Endpoint", string.Empty)
        .WithEnvironment("Parties__MemoriesSearch__ApiToken", string.Empty)
        .WithEnvironment("Parties__MemoriesSearch__TenantId", string.Empty)
        .WithEnvironment("Parties__MemoriesSearch__CaseId", string.Empty);
    foreach (ProjectResource application in builder.Resources.OfType<ProjectResource>()
        .Where(resource => resource.Name is "eventstore-admin" or "tenants" or "parties" or "sample"))
    {
        _ = builder.CreateResourceBuilder(application)
            .WithHttpHealthCheck("/health", endpointName: "http")
            .WithHttpHealthCheck(application.Name == "sample" ? "/alive" : "/ready", endpointName: "http");
    }
    LocalReadinessTopology.RequireAvailableApplicationPorts(builder);
    LocalReadinessTopology.WaitForInfrastructure(builder, eventStore);
    LocalReadinessTopology.AddConfigurationStore(builder, verification);
    LocalReadinessTopology.IsolateSidecars(builder, verification);
    _ = LocalReadinessTopology.WithGatewayHealth(builder, eventStore, maintenance: false);
}

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);

static string ResolveDaprConfigPath(string appHostDirectory, string fileName) {
    // Primary: resolve relative to AppHost project directory (works for dotnet run and Aspire testing)
    string configPath = Path.Combine(appHostDirectory, "DaprComponents", fileName);
    if (File.Exists(configPath)) {
        return configPath;
    }

    // Fallback: working directory (backwards compat for direct launch)
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", fileName);
    if (File.Exists(configPath)) {
        return configPath;
    }

    throw new FileNotFoundException(
        "DAPR access control configuration not found. "
        + $"Ensure {fileName} exists in the DaprComponents directory.",
        configPath);
}

static string ResolveEmptyDaprResourcesPath() {
    string path = Path.Combine(Path.GetTempPath(), "hexalith-frontcomposer-empty-dapr-resources");
    _ = Directory.CreateDirectory(path);
    return path;
}
