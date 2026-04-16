using Counter.Domain;
using Counter.Web;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// Adopter owns AddLocalization (Shell no longer FrameworkReferences Microsoft.AspNetCore.App).
builder.Services.AddLocalization();

builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();

// Story 2-2 Task 9.4 — demo user context so LastUsed pre-fill works end-to-end without real auth.
// Production adopters replace this with a real accessor (OIDC claims via Story 7.1).
builder.Services.Replace(new ServiceDescriptor(typeof(IUserContextAccessor), typeof(DemoUserContextAccessor), ServiceLifetime.Scoped));

// Slightly higher stub latencies so the 5-state lifecycle is observable in the Counter sample.
builder.Services.Configure<StubCommandServiceOptions>(o => {
    o.AcknowledgeDelayMs = 150;
    o.SyncingDelayMs = 150;
    o.ConfirmDelayMs = 200;
});

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Counter.Web.Components.App>()
    .AddAdditionalAssemblies(typeof(IncrementCommand).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
