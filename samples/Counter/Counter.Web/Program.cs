using Counter.Domain;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();

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
    .AddInteractiveServerRenderMode();

app.Run();
