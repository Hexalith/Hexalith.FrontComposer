using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

internal sealed class FrontComposerAuthenticationOptionsValidator(IHostEnvironment? environment = null)
    : IValidateOptions<FrontComposerAuthenticationOptions> {
    public ValidateOptionsResult Validate(string? name, FrontComposerAuthenticationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        List<string> selected = SelectedProviders(options);
        if (selected.Count != 1) {
            failures.Add(Fail(
                "Configure exactly one authentication provider for v1.",
                "Select one of OpenIdConnect, Saml2, GitHubOAuth, or CustomBrokered.",
                "Selected providers: " + (selected.Count == 0 ? "none" : string.Join(", ", selected))));
        }

        ValidateClaimAliases(options.TenantClaimTypes, nameof(options.TenantClaimTypes), failures);
        ValidateClaimAliases(options.UserClaimTypes, nameof(options.UserClaimTypes), failures);

        if (options.OpenIdConnect.Enabled) {
            ValidateOidc(options, failures);
        }

        if (options.Saml2.Enabled) {
            if (options.Saml2.MetadataAddress is null && options.Saml2.ConfigureHandler is null) {
                failures.Add(Fail(
                    "SAML2 configuration needs handler metadata or an adopter-supplied handler hook.",
                    "Set Saml2.MetadataAddress or Saml2.ConfigureHandler.",
                    "Do not parse SAML assertions in FrontComposer."));
            }
        }

        if (options.GitHubOAuth.Enabled) {
            if (string.IsNullOrWhiteSpace(options.GitHubOAuth.ClientId) || string.IsNullOrWhiteSpace(options.GitHubOAuth.ClientSecret)) {
                failures.Add(Fail(
                    "GitHub OAuth requires ClientId and ClientSecret.",
                    "Configure OAuth app credentials through host-owned secret storage.",
                    "FrontComposer stores no GitHub token bodies."));
            }

            if (options.TokenRelay.HostAccessTokenProvider is null && !options.TokenRelay.AllowGitHubOAuthTokenRelay) {
                failures.Add(Fail(
                    "GitHub OAuth sign-in is not an EventStore bearer-token source.",
                    "Provide a broker/custom HostAccessTokenProvider before enabling EventStore token relay.",
                    FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired));
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateOidc(FrontComposerAuthenticationOptions options, List<string> failures) {
        FrontComposerOpenIdConnectOptions oidc = options.OpenIdConnect;
        if (oidc.Authority is null && oidc.MetadataAddress is null) {
            failures.Add(Fail(
                "OIDC requires Authority or MetadataAddress.",
                "Configure the provider discovery endpoint.",
                "Keycloak, Entra, and Google recipes use OIDC discovery with authorization-code flow."));
        }

        if (oidc.Authority is not null) {
            ValidateAbsoluteHttps(oidc.Authority, "OpenIdConnect.Authority", failures);
        }

        if (oidc.MetadataAddress is not null) {
            ValidateAbsoluteHttps(oidc.MetadataAddress, "OpenIdConnect.MetadataAddress", failures);
        }

        if (string.IsNullOrWhiteSpace(oidc.ClientId)) {
            failures.Add(Fail("OIDC ClientId is required.", "Set OpenIdConnect.ClientId.", "Client secrets remain host-owned."));
        }

        if (!string.Equals(oidc.ResponseType, "code", StringComparison.Ordinal)) {
            failures.Add(Fail(
                "OIDC server-side flows must use authorization code flow.",
                "Set OpenIdConnect.ResponseType to 'code'.",
                "Implicit and hybrid flows are not part of FrontComposer v1."));
        }
    }

    private void ValidateAbsoluteHttps(Uri uri, string name, List<string> failures) {
        if (!uri.IsAbsoluteUri) {
            failures.Add(Fail(name + " must be absolute.", "Use a full provider URI.", "Relative provider metadata is not accepted."));
            return;
        }

        bool development = environment is not null && environment.IsDevelopment();
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !development) {
            failures.Add(Fail(name + " must use https outside Development.", "Use https provider metadata in production.", "HTTP metadata is allowed only for local development fixtures."));
        }
    }

    private static void ValidateClaimAliases(IList<string> aliases, string name, List<string> failures) {
        if (aliases.Count == 0 || aliases.Any(string.IsNullOrWhiteSpace)) {
            failures.Add(Fail(name + " must contain at least one non-empty claim name.", "Configure explicit tenant and user claim aliases.", "Missing claim configuration fails closed."));
        }
    }

    private static List<string> SelectedProviders(FrontComposerAuthenticationOptions options) {
        List<string> selected = [];
        if (options.OpenIdConnect.Enabled) {
            selected.Add(nameof(options.OpenIdConnect));
        }

        if (options.Saml2.Enabled) {
            selected.Add(nameof(options.Saml2));
        }

        if (options.GitHubOAuth.Enabled) {
            selected.Add(nameof(options.GitHubOAuth));
        }

        if (options.CustomBrokered.Enabled) {
            selected.Add(nameof(options.CustomBrokered));
        }

        return selected;
    }

    private static string Fail(string whatHappened, string fix, string context)
        => $"{FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid}: {whatHappened} Expected={fix} Context={context} Docs=https://hexalith.dev/frontcomposer/authentication";
}
