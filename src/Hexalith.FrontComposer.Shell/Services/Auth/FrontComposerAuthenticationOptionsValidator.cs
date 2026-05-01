using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

internal sealed class FrontComposerAuthenticationOptionsValidator(IHostEnvironment? environment = null)
    : IValidateOptions<FrontComposerAuthenticationOptions> {
    private const string DocsBase = "https://hexalith.dev/frontcomposer/authentication";

    public ValidateOptionsResult Validate(string? name, FrontComposerAuthenticationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        List<string> selected = SelectedProviders(options);
        if (selected.Count != 1) {
            failures.Add(Fail(
                what: "Configure exactly one authentication provider for v1.",
                expected: "Select one of OpenIdConnect, Saml2, GitHubOAuth, or CustomBrokered.",
                got: "Selected providers: " + (selected.Count == 0 ? "none" : string.Join(", ", selected)),
                docsAnchor: "#single-provider"));
        }

        ValidateClaimAliases(options.TenantClaimTypes, nameof(options.TenantClaimTypes), failures);
        ValidateClaimAliases(options.UserClaimTypes, nameof(options.UserClaimTypes), failures);

        ValidateTokenRelay(options, failures);
        ValidateSchemeCollisions(options, failures);

        if (options.OpenIdConnect.Enabled) {
            ValidateOidc(options, failures);
        }

        if (options.Saml2.Enabled) {
            ValidateSaml2(options, failures);
        }

        if (options.GitHubOAuth.Enabled) {
            ValidateGitHub(options, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>P22 — exposed for eager pre-handler-registration validation in the DI extension.</summary>
    public static ValidateOptionsResult ValidateEagerly(FrontComposerAuthenticationOptions options, IHostEnvironment? environment)
        => new FrontComposerAuthenticationOptionsValidator(environment).Validate(name: null, options);

    private void ValidateOidc(FrontComposerAuthenticationOptions options, List<string> failures) {
        FrontComposerOpenIdConnectOptions oidc = options.OpenIdConnect;
        if (oidc.Authority is null && oidc.MetadataAddress is null) {
            failures.Add(Fail(
                "OIDC requires Authority or MetadataAddress.",
                "Configure the provider discovery endpoint.",
                "Both Authority and MetadataAddress are null.",
                "#oidc-authority"));
        }

        if (oidc.Authority is not null) {
            ValidateAbsoluteHttps(oidc.Authority, "OpenIdConnect.Authority", failures, "#oidc-authority");
            ValidateNoUserInfo(oidc.Authority, "OpenIdConnect.Authority", failures, "#oidc-authority");
        }

        if (oidc.MetadataAddress is not null) {
            ValidateAbsoluteHttps(oidc.MetadataAddress, "OpenIdConnect.MetadataAddress", failures, "#oidc-metadata");
            ValidateNoUserInfo(oidc.MetadataAddress, "OpenIdConnect.MetadataAddress", failures, "#oidc-metadata");
        }

        if (oidc.Authority is not null
            && oidc.MetadataAddress is not null
            && !string.Equals(oidc.Authority.Host, oidc.MetadataAddress.Host, StringComparison.OrdinalIgnoreCase)) {
            failures.Add(Fail(
                "OpenIdConnect.Authority and OpenIdConnect.MetadataAddress must point to the same host.",
                "Use a single discovery host or remove the conflicting one.",
                "Authority host=" + oidc.Authority.Host + ", MetadataAddress host=" + oidc.MetadataAddress.Host,
                "#oidc-authority"));
        }

        if (string.IsNullOrWhiteSpace(oidc.ClientId)) {
            failures.Add(Fail(
                "OIDC ClientId is required.",
                "Set OpenIdConnect.ClientId.",
                "ClientId is null or whitespace.",
                "#oidc-client"));
        }

        if (oidc.Audience is not null && string.IsNullOrWhiteSpace(oidc.Audience)) {
            failures.Add(Fail(
                "OIDC Audience must be non-whitespace when set.",
                "Set OpenIdConnect.Audience to a concrete audience or leave it null.",
                "Audience is whitespace.",
                "#oidc-audience"));
        }

        if (!string.Equals(oidc.ResponseType, "code", StringComparison.Ordinal)) {
            failures.Add(Fail(
                "OIDC server-side flows must use authorization code flow.",
                "Set OpenIdConnect.ResponseType to 'code'.",
                "ResponseType=" + oidc.ResponseType,
                "#oidc-response-type"));
        }

        ValidateOidcScopes(oidc, failures);
        ValidateCallbackPath(oidc.CallbackPath, "OpenIdConnect.CallbackPath", failures);
        ValidateCallbackPath(oidc.SignedOutCallbackPath, "OpenIdConnect.SignedOutCallbackPath", failures);
    }

    private static void ValidateOidcScopes(FrontComposerOpenIdConnectOptions oidc, List<string> failures) {
        IReadOnlyList<string> nonWhitespace = oidc.Scopes.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (nonWhitespace.Count == 0) {
            failures.Add(Fail(
                "OpenIdConnect.Scopes must contain at least one non-whitespace scope.",
                "Include 'openid' (and 'profile') in OpenIdConnect.Scopes.",
                "All scopes are null or whitespace.",
                "#oidc-scopes"));
            return;
        }

        if (!nonWhitespace.Contains("openid", StringComparer.Ordinal)) {
            failures.Add(Fail(
                "OpenIdConnect.Scopes must include the 'openid' scope.",
                "Add 'openid' to OpenIdConnect.Scopes.",
                "Scopes=" + string.Join(',', nonWhitespace),
                "#oidc-scopes"));
        }
    }

    private void ValidateSaml2(FrontComposerAuthenticationOptions options, List<string> failures) {
        if (options.Saml2.ConfigureHandler is null) {
            failures.Add(Fail(
                "SAML2 requires an adopter-supplied handler hook.",
                "Set Saml2.ConfigureHandler to register the SAML handler. MetadataAddress alone does not register a handler in FrontComposer.",
                options.Saml2.MetadataAddress is null ? "ConfigureHandler is null and MetadataAddress is null." : "ConfigureHandler is null (MetadataAddress alone is informational).",
                "#saml-handler"));
        }

        if (options.Saml2.MetadataAddress is not null) {
            ValidateAbsoluteHttps(options.Saml2.MetadataAddress, "Saml2.MetadataAddress", failures, "#saml-metadata");
        }
    }

    private static void ValidateGitHub(FrontComposerAuthenticationOptions options, List<string> failures) {
        if (string.IsNullOrWhiteSpace(options.GitHubOAuth.ClientId) || string.IsNullOrWhiteSpace(options.GitHubOAuth.ClientSecret)) {
            failures.Add(Fail(
                "GitHub OAuth requires ClientId and ClientSecret.",
                "Configure OAuth app credentials through host-owned secret storage.",
                "ClientId or ClientSecret is null/whitespace.",
                "#github-credentials"));
        }

        ValidateCallbackPath(options.GitHubOAuth.CallbackPath, "GitHubOAuth.CallbackPath", failures);

        if (options.TokenRelay.HostAccessTokenProvider is null && !options.TokenRelay.AllowGitHubOAuthTokenRelay) {
            failures.Add(FailWithId(
                FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired,
                "GitHub OAuth sign-in is not an EventStore bearer-token source.",
                "Provide a broker/custom HostAccessTokenProvider before enabling EventStore token relay.",
                "TokenRelay.HostAccessTokenProvider is null and TokenRelay.AllowGitHubOAuthTokenRelay is false.",
                "#github-broker"));
        }
    }

    private static void ValidateTokenRelay(FrontComposerAuthenticationOptions options, List<string> failures) {
        if (string.IsNullOrWhiteSpace(options.TokenRelay.AccessTokenName)) {
            failures.Add(Fail(
                "TokenRelay.AccessTokenName must be non-whitespace.",
                "Set TokenRelay.AccessTokenName to the token name (default 'access_token').",
                "AccessTokenName is null or whitespace.",
                "#token-relay"));
        }

        HashSet<string> tenantSet = new(StringComparer.Ordinal);
        foreach (string alias in options.TenantClaimTypes) {
            if (string.IsNullOrWhiteSpace(alias) || !tenantSet.Add(alias)) {
                continue;
            }
        }

        if (options.TenantClaimTypes.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.Ordinal).Count()
            != options.TenantClaimTypes.Count(a => !string.IsNullOrWhiteSpace(a))) {
            failures.Add(Fail(
                "TenantClaimTypes must not contain duplicate aliases.",
                "Remove duplicate alias entries.",
                "TenantClaimTypes=" + string.Join(',', options.TenantClaimTypes),
                "#claim-aliases"));
        }

        if (options.UserClaimTypes.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.Ordinal).Count()
            != options.UserClaimTypes.Count(a => !string.IsNullOrWhiteSpace(a))) {
            failures.Add(Fail(
                "UserClaimTypes must not contain duplicate aliases.",
                "Remove duplicate alias entries.",
                "UserClaimTypes=" + string.Join(',', options.UserClaimTypes),
                "#claim-aliases"));
        }
    }

    private static void ValidateSchemeCollisions(FrontComposerAuthenticationOptions options, List<string> failures) {
        if (options.OpenIdConnect.Enabled
            && string.Equals(options.OpenIdConnect.SignInScheme, options.OpenIdConnect.ChallengeScheme, StringComparison.Ordinal)) {
            failures.Add(Fail(
                "OpenIdConnect.SignInScheme and OpenIdConnect.ChallengeScheme must differ.",
                "Configure distinct cookie sign-in and OIDC challenge schemes.",
                "Both are '" + options.OpenIdConnect.SignInScheme + "'.",
                "#scheme-collision"));
        }

        if (options.Saml2.Enabled
            && string.Equals(options.Saml2.SignInScheme, options.Saml2.ChallengeScheme, StringComparison.Ordinal)) {
            failures.Add(Fail(
                "Saml2.SignInScheme and Saml2.ChallengeScheme must differ.",
                "Configure distinct cookie sign-in and SAML challenge schemes.",
                "Both are '" + options.Saml2.SignInScheme + "'.",
                "#scheme-collision"));
        }

        if (options.GitHubOAuth.Enabled
            && string.Equals(options.GitHubOAuth.SignInScheme, options.GitHubOAuth.ChallengeScheme, StringComparison.Ordinal)) {
            failures.Add(Fail(
                "GitHubOAuth.SignInScheme and GitHubOAuth.ChallengeScheme must differ.",
                "Configure distinct cookie sign-in and GitHub challenge schemes.",
                "Both are '" + options.GitHubOAuth.SignInScheme + "'.",
                "#scheme-collision"));
        }
    }

    private static void ValidateCallbackPath(string path, string name, List<string> failures) {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/')) {
            failures.Add(Fail(
                name + " must start with '/'.",
                "Set " + name + " to a path beginning with '/' (for example '/signin-oidc').",
                name + "=" + path,
                "#callback-path"));
        }
    }

    private void ValidateAbsoluteHttps(Uri uri, string name, List<string> failures, string docsAnchor) {
        if (!uri.IsAbsoluteUri) {
            failures.Add(Fail(name + " must be absolute.", "Use a full provider URI.", name + "=" + uri, docsAnchor));
            return;
        }

        bool development = environment is not null && environment.IsDevelopment();
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !development) {
            failures.Add(Fail(
                name + " must use https outside Development.",
                "Use https provider metadata in production.",
                name + " scheme=" + uri.Scheme,
                docsAnchor));
        }
    }

    private static void ValidateNoUserInfo(Uri uri, string name, List<string> failures, string docsAnchor) {
        if (uri.IsAbsoluteUri && !string.IsNullOrEmpty(uri.UserInfo)) {
            failures.Add(Fail(
                name + " must not embed userinfo (user:pass@host).",
                "Move credentials out of the URL into the provider client config.",
                name + " contains userinfo.",
                docsAnchor));
        }
    }

    private static void ValidateClaimAliases(IList<string> aliases, string name, List<string> failures) {
        if (aliases.Count == 0 || aliases.Any(string.IsNullOrWhiteSpace)) {
            failures.Add(Fail(
                name + " must contain at least one non-empty claim name.",
                "Configure explicit tenant and user claim aliases.",
                "Aliases empty or contain whitespace entries.",
                "#claim-aliases"));
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

    /// <summary>P17 — teaching diagnostic shape: What. Expected=... Got=... Docs=...</summary>
    private static string Fail(string what, string expected, string got, string docsAnchor)
        => FailWithId(FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid, what, expected, got, docsAnchor);

    private static string FailWithId(string diagnosticId, string what, string expected, string got, string docsAnchor)
        => $"{diagnosticId}: {what} Expected={expected} Got={got} Docs={DocsBase}{docsAnchor}";
}
