using Hexalith.Commons.Aspire;
using Hexalith.EventStore.Aspire;
using Hexalith.FrontComposer.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control + resiliency configuration paths.
// Uses builder.AppHostDirectory to work under both `dotnet run` and Aspire testing.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.eventstore-admin.yaml");
string resiliencyConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "resiliency.yaml");
string stateStoreComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "statestore.yaml");
(string? daprPlacementHostAddress, string? daprSchedulerHostAddress) = AspireDaprLocalServiceEndpoints.Resolve(
    builder.Configuration[AspireDaprLocalServiceEndpoints.PlacementHostAddressKey],
    builder.Configuration[AspireDaprLocalServiceEndpoints.SchedulerHostAddressKey]);

// Keycloak identity provider for JWT authentication.
// Enabled by default for local development with real OIDC token testing.
// Set EnableKeycloak=false in environment or appsettings to run without Keycloak
// (falls back to symmetric key auth via Authentication:JwtBearer:SigningKey).
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase)) {
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}

// Add EventStore (command gateway), Admin Server, and Admin UI projects.
// Project paths are resolved cross-repo via the IProjectMetadata classes in this AppHost.
IResourceBuilder<ProjectResource> eventStore = builder.AddProject<HexalithEventStore>("eventstore");
_ = eventStore.WithEnvironment("EventStore__Publisher__TopicOverrides__global-administrators", "tenants.events");
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
    daprSchedulerHostAddress: daprSchedulerHostAddress);

// Add the Tenants domain service via the platform domain-module extension: its sidecar shares the
// EventStore state store + pub/sub.
IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants")
    .AddEventStoreDomainModule(
        eventStoreResources,
        "tenants",
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

// Tenants sample domain service. EventStore's appsettings register the sample/counter/greeting
// (and orders/inventory) domains against the "sample" app id; its sidecar shares the EventStore
// state store + pub/sub. Without it, the EventStore admin operational-index poll to "sample" fails
// at startup (500) and the whole index build is skipped.
_ = builder.AddProject<HexalithTenantsSample>("sample")
    .AddEventStoreDomainModule(
        eventStoreResources,
        "sample",
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

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

// Counter sample — the FrontComposer demo shell. Co-hosted in the single platform AppHost so the
// whole stack (EventStore, Tenants, Tenants UI, Counter) runs from ONE orchestrator. It stays a
// self-contained demo (fake auth + seeded specimen data) and is intentionally NOT wired to the
// EventStore/Tenants/Keycloak backend, to keep the a11y/visual specimen gate deterministic.
_ = builder.AddProject<Projects.Counter_Web>("counter-web")
    .WithExternalHttpEndpoints();

// Wire Keycloak auth to EventStore, Tenants, Admin.Server, and Admin.UI if enabled.
if (keycloak is not null && realmUrl is not null) {
    _ = eventStore
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");

    _ = tenants
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");

    _ = adminServer
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");

    _ = adminUI
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"))
        .WithEnvironment("EventStore__Authentication__Authority", realmUrl)
        .WithEnvironment("EventStore__Authentication__ClientId", "hexalith-eventstore")
        .WithEnvironment("EventStore__Authentication__Username", "admin-user")
        .WithEnvironment("EventStore__Authentication__Password", "admin-pass");

    _ = tenantsUI
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "")
        // Interactive browser sign-in (authorization-code flow) for the Tenants UI. Uses a
        // confidential Keycloak client; the relayed access token carries the hexalith-eventstore
        // audience so EventStore gateway calls authorize per-user.
        .WithEnvironment("Authentication__OpenIdConnect__Authority", realmUrl)
        .WithEnvironment("Authentication__OpenIdConnect__ClientId", "hexalith-tenants-ui")
        .WithEnvironment("Authentication__OpenIdConnect__ClientSecret", "tenants-ui-dev-secret")
        .WithEnvironment("Authentication__OpenIdConnect__Audience", "hexalith-eventstore");
}
else {
    _ = adminUI.WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));
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
