using Counter.Domain;

using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Counter.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
