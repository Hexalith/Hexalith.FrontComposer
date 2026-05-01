using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAuthenticationOptionsTests {
    [Fact]
    public void Validate_Fails_WhenNoProviderIsSelected() {
        ValidateOptionsResult result = Validate(new FrontComposerAuthenticationOptions());

        result.Failed.ShouldBeTrue();
        string joined = string.Join("\n", result.Failures);
        joined.ShouldContain(FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid);
        joined.ShouldContain("Configure exactly one authentication provider");
    }

    [Fact]
    public void Validate_Fails_WhenMultipleProvidersAreSelected() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.GitHubOAuth.Enabled = true;
        options.GitHubOAuth.ClientId = "github-client";
        options.GitHubOAuth.ClientSecret = "github-secret";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        string joined = string.Join("\n", result.Failures);
        joined.ShouldContain("exactly one");
        joined.ShouldContain("OpenIdConnect");
        joined.ShouldContain("GitHubOAuth");
    }

    [Fact]
    public void Validate_Fails_WhenTenantClaimNamesAreMissing() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.TenantClaimTypes.Clear();

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.Single().ShouldContain("TenantClaimTypes");
    }

    [Fact]
    public void Validate_Fails_WhenOidcAuthorityIsHttpOutsideDevelopment() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = new Uri("http://identity.test/realms/demo");

        ValidateOptionsResult result = Validate(options, isDevelopment: false);

        result.Failed.ShouldBeTrue();
        result.Failures.Single().ShouldContain("https");
    }

    [Fact]
    public void Validate_AcceptsHttpOidcAuthorityInDevelopment() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = new Uri("http://localhost:8080/realms/demo");

        ValidateOptionsResult result = Validate(options, isDevelopment: true);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenGitHubTokenRelayHasNoBroker() {
        FrontComposerAuthenticationOptions options = ValidGitHub();

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.Single().ShouldContain(FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
    }

    [Fact]
    public void Recipe_Keycloak_ConfiguresOidcAuthorizationCodeDefaults() {
        FrontComposerAuthenticationOptions options = new();

        options.UseKeycloak(
            authority: new Uri("https://keycloak.test/realms/frontcomposer"),
            clientId: "frontcomposer",
            clientSecret: "secret",
            tenantClaimType: "tenant",
            userClaimType: "sub");

        options.OpenIdConnect.Enabled.ShouldBeTrue();
        options.OpenIdConnect.ResponseType.ShouldBe("code");
        options.OpenIdConnect.Authority.ShouldBe(new Uri("https://keycloak.test/realms/frontcomposer"));
        options.TenantClaimTypes.ShouldBe(["tenant"]);
        options.UserClaimTypes.ShouldBe(["sub"]);
    }

    private static ValidateOptionsResult Validate(FrontComposerAuthenticationOptions options, bool isDevelopment = false)
        => new FrontComposerAuthenticationOptionsValidator(new TestHostEnvironment(isDevelopment)).Validate(null, options);

    private static FrontComposerAuthenticationOptions ValidOidc() {
        FrontComposerAuthenticationOptions options = new();
        options.OpenIdConnect.Enabled = true;
        options.OpenIdConnect.Authority = new Uri("https://identity.test/realms/demo");
        options.OpenIdConnect.ClientId = "frontcomposer";
        options.OpenIdConnect.ClientSecret = "secret";
        options.OpenIdConnect.Audience = "frontcomposer-api";
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("sub");
        return options;
    }

    private static FrontComposerAuthenticationOptions ValidGitHub() {
        FrontComposerAuthenticationOptions options = new();
        options.GitHubOAuth.Enabled = true;
        options.GitHubOAuth.ClientId = "github-client";
        options.GitHubOAuth.ClientSecret = "github-secret";
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("id");
        return options;
    }
}
