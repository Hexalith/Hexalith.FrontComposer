using Counter.Domain;
using Counter.Specimens;
using Counter.Specimens.Domain;
using Counter.Web;
using Counter.Web.Components.Replacements;
using Counter.Web.Components.Slots;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;
using Hexalith.FrontComposer.Shell.Components.Specimens;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsEnvironment("Test")) {
    builder.WebHost.UseStaticWebAssets();
}

bool specimensEnabled = FrontComposerSpecimenRoutes.IsEnabled(builder.Configuration, builder.Environment);
bool mcpEnabled = builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Test");

// Story 3-1 ADR-030 — ValidateScopes = true so Singleton-captures of IStorageService (now Scoped)
// fail at boot instead of leaking writes across tenants. Has to sit on the host builder BEFORE
// any service resolution.
builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

// Story 3-1 D28 — Quickstart chains AddLocalization + AddHexalithShellLocalization +
// AddHexalithFrontComposer into a single call. Granular 3-call path remains available for
// advanced adopters who want per-call control.
builder.Services.AddHexalithFrontComposerQuickstart(
    o => {
        if (specimensEnabled) {
            _ = o.ScanTypes(
                typeof(CounterProjectionEffects),
                typeof(CounterProjectionSeedFeature),
                typeof(SpecimenStatusProjectionSeedFeature),
                typeof(SpecimenFormattingProjectionSeedFeature),
                typeof(IncrementCommandLifecycleFeature),
                typeof(IncrementCommandReducers),
                typeof(BatchIncrementCommandLifecycleFeature),
                typeof(BatchIncrementCommandReducers),
                typeof(ConfigureCounterCommandLifecycleFeature),
                typeof(ConfigureCounterCommandReducers),
                typeof(PurgeSpecimenRecordCommandLifecycleFeature),
                typeof(PurgeSpecimenRecordCommandReducers),
                typeof(PolicyAllowedSpecimenCommandLifecycleFeature),
                typeof(PolicyAllowedSpecimenCommandReducers),
                typeof(PolicyDeniedSpecimenCommandLifecycleFeature),
                typeof(PolicyDeniedSpecimenCommandReducers));
        }
        else {
            _ = o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly);
        }
    });
builder.Services.AddFrontComposerDevMode(builder.Environment);
builder.Services.AddHexalithDomain<CounterDomain>();
if (specimensEnabled) {
    _ = builder.Services.AddHexalithDomain<CounterSpecimensDomain>();
    _ = builder.Services.AddAuthorizationCore(o => {
        o.AddPolicy("Specimens.PolicyAllowed", policy => policy.RequireAssertion(_ => true));
        o.AddPolicy("Specimens.PolicyDenied", policy => policy.RequireAssertion(_ => false));
    });
    _ = builder.Services.Configure<Hexalith.FrontComposer.Shell.Options.FrontComposerAuthorizationOptions>(o => {
        o.KnownPolicies.Add("Specimens.PolicyAllowed");
        o.KnownPolicies.Add("Specimens.PolicyDenied");
    });
    if (builder.Environment.IsEnvironment("Test")) {
        builder.Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, CounterSpecimenAuthenticationStateProvider>());
    }
}

if (specimensEnabled) {
    _ = builder.Services.AddHexalithProjectionTemplates<FrontComposerTypeSpecimen>();
}
else {
    // Story 6-2 T4 / T9 / AC3 — register the SourceTools-emitted Level 2 projection-template
    // manifest through a direct generated descriptor reference so startup stays trim/AOT friendly.
    _ = builder.Services.AddHexalithProjectionTemplates(__FrontComposerProjectionTemplatesRegistration.Descriptors);

    // Story 6-3 T9 / AC15 — reference Level 3 slot override. Only Count is custom-rendered;
    // Id and Last changed continue through generated FrontComposer field rendering. The typed
    // 3-generic overload (GB-P10) catches component-type mismatches at compile time, which is
    // what adopters should reach for; the Type-taking overload exists for codegen scenarios.
    _ = builder.Services.AddSlotOverride<CounterProjection, int, CounterCountSlot>(
        field: x => x.Count);

    if (builder.Configuration.GetValue<bool>("Hexalith:FrontComposer:E2E:SeedContractMismatch")) {
        _ = builder.Services.AddSingleton(new ProjectionSlotDescriptorSource([
            new ProjectionSlotDescriptor(
                ProjectionType: typeof(CounterProjection),
                FieldName: nameof(CounterProjection.Count),
                FieldType: typeof(int),
                Role: null,
                ComponentType: typeof(CounterCountSlot),
                ContractVersion: (ProjectionSlotContractVersion.Major + 1) * 1_000_000),
        ]));
    }

    // Story 6-4 T9 / AC12 — Level 4 full view replacement reference. The replacement owns the
    // projection body only; generated shell, loading/empty state, lifecycle, grid envelope, and
    // render-context plumbing remain framework-owned around it. The registration is role-agnostic
    // because CounterProjection has only one role (Default ≡ no [ProjectionRole] attribute);
    // fallback-to-generated evidence lives in the SourceTools test fixtures and in the
    // `CounterProjectionView_LoadedState_RendersColumnsAndFormatting` Shell test which renders
    // the same view without `AddViewOverride`.
    _ = builder.Services.AddViewOverride<CounterProjection, CounterFullViewReplacement>();
}

// Story 2-4 Task 6.2 — bind FcShellOptions from configuration so adopters can tune
// lifecycle thresholds AND (Story 3-1) the new AccentColor / LocalStorageMaxEntries / DefaultCulture
// / SupportedCultures values from appsettings.
builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));
builder.Services.Configure<FcShellOptions>(o =>
    // Story 7-2 — the Counter sample's DemoUserContextAccessor is intentionally visible and
    // local-only. Production hosts must supply Story 7-1 real auth claims instead.
    o.AllowDemoTenantContext = builder.Environment.IsDevelopment()
        || builder.Environment.IsEnvironment("Test"));

// Story 2-2 Task 9.4 — demo user context so LastUsed pre-fill works end-to-end without real auth.
// Story 7-1 keeps this default credential-free; fake auth is opt-in and visibly sample-only.
// P11 — fake auth is permitted ONLY in Development. In any other environment the configuration
// flag is rejected at startup so a stray appsettings entry cannot silently disable real auth.
bool fakeAuthRequested = builder.Configuration.GetValue<bool>("Hexalith:FrontComposer:FakeAuth:Enabled");
if (fakeAuthRequested && !builder.Environment.IsDevelopment()) {
    throw new InvalidOperationException(
        "Hexalith:FrontComposer:FakeAuth:Enabled is only permitted when ASPNETCORE_ENVIRONMENT=Development. "
        + "Remove the configuration entry or run with the Development environment for sample smoke tests.");
}

if (fakeAuthRequested) {
    LoggerFactory.Create(b => b.AddConsole())
        .CreateLogger("Counter.FakeAuth")
        .LogCritical(
            "Counter sample is running with FAKE authentication (Hexalith:FrontComposer:FakeAuth:Enabled=true). All requests share a single shared identity. Do not deploy with this flag set.");
}

Type userContextAccessorType = fakeAuthRequested
    ? typeof(CounterFakeAuthUserContextAccessor)
    : typeof(DemoUserContextAccessor);
builder.Services.Replace(new ServiceDescriptor(typeof(IUserContextAccessor), userContextAccessorType, ServiceLifetime.Scoped));

// Slightly higher stub latencies so the 5-state lifecycle is observable in the Counter sample.
builder.Services.Configure<StubCommandServiceOptions>(o => {
    o.AcknowledgeDelayMs = 150;
    o.SyncingDelayMs = 150;
    o.ConfirmDelayMs = 200;
});
builder.Services.Configure<StubCommandServiceOptions>(
    builder.Configuration.GetSection("Hexalith:FrontComposer:StubCommandService"));

if (mcpEnabled) {
    builder.Services.TryAddScoped<IQueryService, CounterMcpSampleQueryService>();
    _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
    _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
    _ = builder.Services.AddFrontComposerMcp(o => {
        o.ManifestAssemblies.Add(typeof(CounterDomain).Assembly);
        if (specimensEnabled) {
            o.ManifestAssemblies.Add(typeof(CounterSpecimensDomain).Assembly);
        }

        o.ApiKeys["counter-e2e-mcp-key"] = new FrontComposerMcpApiKeyIdentity(
            "demo-tenant",
            "demo-user");
    });
}

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

RazorComponentsEndpointConventionBuilder razorComponents = app.MapRazorComponents<Counter.Web.Components.App>();
if (specimensEnabled) {
    _ = razorComponents.AddAdditionalAssemblies(typeof(IncrementCommand).Assembly, typeof(FrontComposerTypeSpecimen).Assembly);
}
else {
    _ = razorComponents.AddAdditionalAssemblies(typeof(IncrementCommand).Assembly);
}

razorComponents
    .AddInteractiveServerRenderMode();

if (mcpEnabled) {
    _ = app.MapFrontComposerMcp();
}

app.Run();
