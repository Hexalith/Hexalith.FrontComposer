using Counter.Domain;
using Counter.Web;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Story 3-1 ADR-030 — ValidateScopes = true so Singleton-captures of IStorageService (now Scoped)
// fail at boot instead of leaking writes across tenants. Has to sit on the host builder BEFORE
// any service resolution.
builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// Story 3-1 D28 — Quickstart chains AddLocalization + AddHexalithShellLocalization +
// AddHexalithFrontComposer into a single call. Granular 3-call path remains available for
// advanced adopters who want per-call control.
builder.Services.AddHexalithFrontComposerQuickstart(
    o => o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();

// Story 2-4 Task 6.2 — bind FcShellOptions from configuration so adopters can tune
// lifecycle thresholds AND (Story 3-1) the new AccentColor / LocalStorageMaxEntries / DefaultCulture
// / SupportedCultures values from appsettings.
builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));

// Story 2-2 Task 9.4 — demo user context so LastUsed pre-fill works end-to-end without real auth.
// Production adopters replace this with a real accessor (OIDC claims via Story 7.1).
builder.Services.Replace(new ServiceDescriptor(typeof(IUserContextAccessor), typeof(DemoUserContextAccessor), ServiceLifetime.Scoped));

// Slightly higher stub latencies so the 5-state lifecycle is observable in the Counter sample.
builder.Services.Configure<StubCommandServiceOptions>(o =>
{
    o.AcknowledgeDelayMs = 150;
    o.SyncingDelayMs = 150;
    o.ConfirmDelayMs = 200;
});

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapRazorComponents<Counter.Web.Components.App>()
    .AddAdditionalAssemblies(typeof(IncrementCommand).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
