using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAuthenticationOptionsTests {
    [Fact]
    public void Validate_Fails_WhenNoProviderIsSelected() {
        ValidateOptionsResult result = Validate(new FrontComposerAuthenticationOptions());

        result.Failed.ShouldBeTrue();
        // P23 — predicate-based assertion replaces brittle `result.Failures.Single()` calls.
        result.Failures.ShouldContain(f => f.StartsWith(FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid, StringComparison.Ordinal));
        result.Failures.ShouldContain(f => f.Contains("exactly one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenMultipleProvidersAreSelected() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.GitHubOAuth.Enabled = true;
        options.GitHubOAuth.ClientId = "github-client";
        options.GitHubOAuth.ClientSecret = "github-secret";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("exactly one", StringComparison.Ordinal));
        result.Failures.ShouldContain(f => f.Contains("OpenIdConnect", StringComparison.Ordinal) && f.Contains("GitHubOAuth", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenTenantClaimNamesAreMissing() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.TenantClaimTypes.Clear();

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("TenantClaimTypes", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenOidcAuthorityIsHttpOutsideDevelopment() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = new Uri("http://identity.test/realms/demo");

        ValidateOptionsResult result = Validate(options, isDevelopment: false);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("https", StringComparison.Ordinal));
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
        result.Failures.ShouldContain(f => f.Contains(FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired, StringComparison.Ordinal));
    }

    // P30 — branch-coverage gap tests below.

    [Fact]
    public void Validate_Fails_WhenOidcMissingAuthorityAndMetadataAddress() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = null;
        options.OpenIdConnect.MetadataAddress = null;

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("Authority or MetadataAddress", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenOidcResponseTypeIsNotCode() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.ResponseType = "id_token";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("authorization code", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenOidcAudienceIsWhitespace() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Audience = "   ";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("Audience", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenOidcAuthorityContainsUserInfo() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = new Uri("https://user:pass@identity.test/realms/demo");

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("userinfo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenOidcScopesMissOpenId() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Scopes.Clear();
        options.OpenIdConnect.Scopes.Add("profile");

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("openid", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenOidcCallbackPathDoesNotStartWithSlash() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.CallbackPath = "signin-oidc";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("CallbackPath", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenOidcAuthorityAndMetadataDifferentHosts() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.Authority = new Uri("https://idp1.test");
        options.OpenIdConnect.MetadataAddress = new Uri("https://idp2.test/.well-known/openid-configuration");

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("same host", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenSignInAndChallengeSchemeCollide() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.OpenIdConnect.SignInScheme = "Hexalith.FrontComposer.Custom.Auth";
        options.OpenIdConnect.ChallengeScheme = "Hexalith.FrontComposer.Custom.Auth";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("must differ", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenSamlEnabledWithoutConfigureHandler() {
        FrontComposerAuthenticationOptions options = new();
        options.Saml2.Enabled = true;
        options.Saml2.MetadataAddress = new Uri("https://idp.test/saml/metadata");
        options.TenantClaimTypes.Add("tenant");
        options.UserClaimTypes.Add("nameid");

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("ConfigureHandler", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenSamlMetadataIsHttpOutsideDevelopment() {
        FrontComposerAuthenticationOptions options = new();
        options.Saml2.Enabled = true;
        options.Saml2.MetadataAddress = new Uri("http://idp.test/saml/metadata");
        options.Saml2.ConfigureHandler = (_, _) => { };
        options.TenantClaimTypes.Add("tenant");
        options.UserClaimTypes.Add("nameid");

        ValidateOptionsResult result = Validate(options, isDevelopment: false);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("https", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenAccessTokenNameIsWhitespace() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.TokenRelay.AccessTokenName = "   ";

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("AccessTokenName", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Fails_WhenTenantClaimsContainDuplicates() {
        FrontComposerAuthenticationOptions options = ValidOidc();
        options.TenantClaimTypes.Add("tenant_id"); // dup

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenGitHubMissingClientCredentials() {
        FrontComposerAuthenticationOptions options = new();
        options.GitHubOAuth.Enabled = true;
        options.GitHubOAuth.ClientId = "";
        options.GitHubOAuth.ClientSecret = "";
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("id");
        options.TokenRelay.AllowGitHubOAuthTokenRelay = true; // bypass broker rejection

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("ClientId", StringComparison.Ordinal) && f.Contains("ClientSecret", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_TeachingDiagnostic_IncludesGotSegment() {
        // P17 — teaching shape includes Got=<state>.
        FrontComposerAuthenticationOptions options = new();

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(f => f.Contains("Got=", StringComparison.Ordinal));
        result.Failures.ShouldContain(f => f.Contains("Expected=", StringComparison.Ordinal));
        result.Failures.ShouldContain(f => f.Contains("Docs=", StringComparison.Ordinal));
    }

    // P14 — recipe tests for every supported provider.

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
        options.OpenIdConnect.ProviderName.ShouldBe("Keycloak");
        options.SelectedProviderKind.ShouldBe(FrontComposerAuthenticationProviderKind.OpenIdConnect);
        options.TenantClaimTypes.ShouldBe(["tenant"]);
        options.UserClaimTypes.ShouldBe(["sub"]);
    }

    [Fact]
    public void Recipe_MicrosoftEntraId_ConfiguresOidcAuthorizationCodeDefaults() {
        FrontComposerAuthenticationOptions options = new();

        options.UseMicrosoftEntraId(
            authority: new Uri("https://login.microsoftonline.com/tenant-id/v2.0"),
            clientId: "frontcomposer",
            clientSecret: "secret");

        options.OpenIdConnect.Enabled.ShouldBeTrue();
        options.OpenIdConnect.ProviderName.ShouldBe("MicrosoftEntraId");
        options.OpenIdConnect.ResponseType.ShouldBe("code");
        options.TenantClaimTypes.ShouldBe(["tid"]);
        options.UserClaimTypes.ShouldBe(["sub"]);
        options.SelectedProviderKind.ShouldBe(FrontComposerAuthenticationProviderKind.OpenIdConnect);
    }

    [Fact]
    public void Recipe_Google_ConfiguresOidcAuthorizationCodeDefaults() {
        FrontComposerAuthenticationOptions options = new();

        options.UseGoogle(
            clientId: "frontcomposer",
            clientSecret: "secret",
            tenantClaimType: "hd"); // Google "hosted domain"

        options.OpenIdConnect.Enabled.ShouldBeTrue();
        options.OpenIdConnect.ProviderName.ShouldBe("Google");
        options.OpenIdConnect.Authority.ShouldBe(new Uri("https://accounts.google.com"));
        options.TenantClaimTypes.ShouldBe(["hd"]);
        options.UserClaimTypes.ShouldBe(["sub"]);
    }

    [Fact]
    public void Recipe_GitHubOAuth_ConfiguresGitHubProvider() {
        FrontComposerAuthenticationOptions options = new();

        options.UseGitHubOAuth(
            clientId: "frontcomposer",
            clientSecret: "secret",
            tenantClaimType: "tenant_id");

        options.GitHubOAuth.Enabled.ShouldBeTrue();
        options.OpenIdConnect.Enabled.ShouldBeFalse();
        options.GitHubOAuth.ClientId.ShouldBe("frontcomposer");
        options.UserClaimTypes.ShouldBe(["id"]);
    }

    [Fact]
    public void Recipe_CallingTwoRecipes_ReplacesPriorRecipe_NotStacks() {
        // P19 — recipes must be mutually exclusive.
        FrontComposerAuthenticationOptions options = new();
        options.UseKeycloak(new Uri("https://kc.test/"), "id1", "s1", "t1", "u1");
        options.UseGitHubOAuth("id2", "s2", "t2");

        options.OpenIdConnect.Enabled.ShouldBeFalse();
        options.GitHubOAuth.Enabled.ShouldBeTrue();
        options.SelectedProviderKind.ShouldBe(FrontComposerAuthenticationProviderKind.GitHubOAuth);
    }

    [Fact]
    public void Recipe_Keycloak_ThrowsOnNullArguments() {
        FrontComposerAuthenticationOptions options = new();

        Should.Throw<ArgumentException>(() => options.UseKeycloak(new Uri("https://kc.test/"), "", "secret", "tenant", "sub"));
        Should.Throw<ArgumentException>(() => options.UseKeycloak(new Uri("https://kc.test/"), "id", "", "tenant", "sub"));
        Should.Throw<ArgumentNullException>(() => options.UseKeycloak(null!, "id", "secret", "tenant", "sub"));
    }

    [Fact]
    public void Saml2_HandlerHook_IsInvokedWithBuilderAndScheme() {
        // P27 — SAML fake-handler bridging fixture. Verifies the ConfigureHandler hook contract
        // (the auth bridge's only path for SAML registration); we do not exercise the full
        // AddAuthentication pipeline here because that is integration territory.
        AuthenticationBuilder? observedBuilder = null;
        string? observedScheme = null;

        FrontComposerAuthenticationOptions options = new();
        options.Saml2.Enabled = true;
        options.Saml2.ChallengeScheme = "Hexalith.FrontComposer.Saml2";
        options.Saml2.ConfigureHandler = (b, scheme) => {
            observedBuilder = b;
            observedScheme = scheme;
        };

        Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();
        AuthenticationBuilder builder = Microsoft.Extensions.DependencyInjection.AuthenticationServiceCollectionExtensions
            .AddAuthentication(services);
        options.Saml2.ConfigureHandler.Invoke(builder, options.Saml2.ChallengeScheme);

        observedBuilder.ShouldNotBeNull();
        observedScheme.ShouldBe("Hexalith.FrontComposer.Saml2");
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
