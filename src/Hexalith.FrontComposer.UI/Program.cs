using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Parties.AdminPortal.Components;
using Hexalith.Parties.ConsumerPortal.Components;
using Hexalith.Parties.UI;
using Hexalith.Parties.UI.Authentication;
using Hexalith.Parties.UI.Extensions;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(
    typeof(TenantsFrontComposerDomain).Assembly,
    typeof(PartiesUiDomainMarker).Assembly));
builder.Services.AddFrontComposerDevMode(builder.Environment);
builder.Services.AddHexalithDomain<TenantsFrontComposerDomain>();
builder.Services.AddHexalithDomain<PartiesUiDomainMarker>();

bool authEnabled =
    Uri.TryCreate(builder.Configuration["Authentication:OpenIdConnect:Authority"], UriKind.Absolute, out Uri? oidcAuthority)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]);

if (authEnabled)
{
    _ = builder.Services.AddHexalithFrontComposerServerSecurity(o => o.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        tenantClaimType: "eventstore:current-tenant",
        userClaimType: "sub",
        roleClaimType: "roles"));
}

builder.Services.AddHexalithTenantsUiModule(builder.Configuration, authEnabled);
builder.Services.AddHexalithPartiesUiModule(builder.Configuration, authEnabled);

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();

if (authEnabled)
{
    _ = app.UseAuthentication();
    _ = app.UseAuthorization();
}

app.UseAntiforgery();

app.MapRazorComponents<Hexalith.FrontComposer.UI.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(TenantsFrontComposerDomain).Assembly,
        typeof(PartiesAdminPortal).Assembly,
        typeof(MyProfilePage).Assembly);

if (authEnabled)
{
    _ = app.MapHexalithFrontComposerAuthenticationEndpoints();
}

app.Run();
