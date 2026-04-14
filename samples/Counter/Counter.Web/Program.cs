using Counter.Domain;

using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly, typeof(CounterDomain).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Counter.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
