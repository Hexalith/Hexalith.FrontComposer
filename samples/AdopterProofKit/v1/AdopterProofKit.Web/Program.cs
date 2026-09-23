using AdopterProofKit.Domain;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

string mode = Environment.GetEnvironmentVariable("ADOPTER_PROOF_BOOTSTRAP") ?? "three-call";
string? endpoint = Environment.GetEnvironmentVariable("ADOPTER_PROOF_EVENTSTORE_ENDPOINT");
if (mode != "empty" && !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
{
    throw new InvalidOperationException("ADOPTER_PROOF_EVENTSTORE_ENDPOINT must be an absolute URI.");
}

void AddEventStore() => builder.Services.AddHexalithEventStore(options =>
{
    options.BaseAddress = new Uri(endpoint!, UriKind.Absolute);
});

switch (mode)
{
    case "three-call":
        builder.Services.AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(typeof(ProofDomain).Assembly));
        builder.Services.AddHexalithDomain<ProofDomain>();
        AddEventStore();
        break;
    case "missing-quickstart":
        builder.Services.AddHexalithDomain<ProofDomain>();
        AddEventStore();
        break;
    case "misordered":
        AddEventStore();
        builder.Services.AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(typeof(ProofDomain).Assembly));
        builder.Services.AddHexalithDomain<ProofDomain>();
        break;
    case "empty":
        builder.Services.AddHexalithFrontComposerQuickstart();
        break;
    default:
        throw new InvalidOperationException("ADOPTER_PROOF_BOOTSTRAP must be three-call, missing-quickstart, misordered, or empty.");
}

if (mode == "three-call"
    && (!builder.Services.Any(descriptor => descriptor.ServiceType == typeof(EventStoreCommandClient))
        || !builder.Services.Any(descriptor => descriptor.ServiceType == typeof(EventStoreQueryClient))
        || builder.Services.Any(descriptor => descriptor.ServiceType == typeof(StubCommandService))))
{
    throw new InvalidOperationException(
        "Adopter proof requires AddHexalithEventStore(...) to register real command and query services before host build.");
}

builder.Services.Configure<FcShellOptions>(options => options.AllowDemoTenantContext = false);

WebApplication app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<AdopterProofKit.Web.Components.App>()
    .AddAdditionalAssemblies(typeof(ProofDomain).Assembly)
    .AddInteractiveServerRenderMode();
app.Run();
