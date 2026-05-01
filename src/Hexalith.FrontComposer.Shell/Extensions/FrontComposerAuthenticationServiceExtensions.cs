using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// FrontComposer authentication bridge wiring (Story 7-1). Composes ASP.NET Core authentication
/// handlers with framework seams (`IUserContextAccessor`, `IAuthRedirector`,
/// `EventStoreOptions.AccessTokenProvider`) without owning a login UI.
/// </summary>
public static class FrontComposerAuthenticationServiceExtensions {
    /// <summary>
    /// Registers the FrontComposer authentication bridge. Exactly one provider recipe must be
    /// configured (OIDC, SAML, GitHub OAuth, or CustomBrokered).
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerAuthentication(
        this IServiceCollection services,
        Action<FrontComposerAuthenticationOptions> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // P1 — build options once. Capture once, hand the same instance to both DI options
        // pipeline and the synchronous handler-registration path. Re-running `configure` on a
        // second instance triggers double-side-effects (secret fetches, registrations).
        FrontComposerAuthenticationOptions setup = new();
        configure(setup);

        // P22 — validate eagerly before registering authentication handlers. Prevents partial
        // handler registrations leaking into DI when ValidateOnStart later rejects the options.
        ValidateOptionsResult eager = FrontComposerAuthenticationOptionsValidator.ValidateEagerly(
            setup,
            ResolveHostEnvironment(services));
        if (eager.Failed) {
            throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid,
                $"{FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid}: {string.Join(" | ", eager.Failures)}");
        }

        _ = services.AddOptions<FrontComposerAuthenticationOptions>()
            .Configure(o => CopyConfiguration(setup, o))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<FrontComposerAuthenticationOptions>, FrontComposerAuthenticationOptionsValidator>());
        services.AddHttpContextAccessor();

        AddAuthenticationHandlers(services, setup);

        // P2 — only swap framework seams when a provider is actually configured. The eager
        // validator already rejected the no-provider case, but defensive guarding keeps
        // future code paths from silently replacing seams when validation is bypassed.
        if (setup.SelectedProviderKind != FrontComposerAuthenticationProviderKind.None) {
            services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ClaimsPrincipalUserContextAccessor>());
            services.Replace(ServiceDescriptor.Scoped<IAuthRedirector, FrontComposerAuthRedirector>());
            services.TryAddSingleton<FrontComposerAccessTokenProvider>();

            // P32 (DN2) — always replace `EventStoreOptions.AccessTokenProvider` and log when an
            // adopter pre-set it. Silent `??=` skip lets a stale adopter delegate bypass the
            // GitHub broker check (HFC2014) and the access-token diagnostic path.
            // P34 (DN5) — also force RequireAccessToken=true: the auth bridge implies bearer
            // tokens are required for EventStore traffic. Adopters opt out by setting
            // `RequireAccessToken = false` AFTER calling AddHexalithFrontComposerAuthentication.
            _ = services.AddOptions<EventStoreOptions>()
                .Configure<FrontComposerAccessTokenProvider, ILoggerFactory>((eventStore, tokenProvider, loggerFactory) => {
                    if (eventStore.AccessTokenProvider is not null) {
                        loggerFactory
                            .CreateLogger("Hexalith.FrontComposer.Shell.Authentication")
                            .LogInformation(
                                "{DiagnosticId}: FrontComposer authentication bridge replaces a previously configured EventStoreOptions.AccessTokenProvider.",
                                FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
                    }

                    eventStore.AccessTokenProvider = tokenProvider.GetAccessTokenAsync;
                    eventStore.RequireAccessToken = true;
                });
        }

        return services;
    }

    /// <summary>
    /// DN4/P33 — Maps the FrontComposer authentication challenge and sign-out endpoints. Cookie
    /// middleware redirects unauthenticated requests to `LoginPath` (default
    /// `/authentication/challenge`); this mapper turns those redirects into a
    /// `ChallengeAsync` against the configured provider, avoiding the 404 trap when the
    /// adopter has not provided a login UI.
    /// </summary>
    public static IEndpointRouteBuilder MapHexalithFrontComposerAuthenticationEndpoints(this IEndpointRouteBuilder endpoints) {
        ArgumentNullException.ThrowIfNull(endpoints);
        FrontComposerAuthenticationOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<FrontComposerAuthenticationOptions>>()
            .Value;

        // Use RequestDelegate explicitly so the typed `MapGet(IEndpointRouteBuilder, string,
        // RequestDelegate)` overload is selected (avoids the trimming-unfriendly minimal-API
        // route handler reflection path).
        RequestDelegate challenge = async context => {
            string? returnUrl = context.Request.Query["returnUrl"];
            string sanitized = FrontComposerReturnUrl.Sanitize(returnUrl);
            await context.ChallengeAsync(
                options.SelectedChallengeScheme(),
                new AuthenticationProperties { RedirectUri = sanitized })
                .ConfigureAwait(false);
        };

        RequestDelegate signOut = async context => {
            string? returnUrl = context.Request.Query["returnUrl"];
            string sanitized = FrontComposerReturnUrl.Sanitize(returnUrl);
            await context.SignOutAsync(
                GetSignInScheme(options),
                new AuthenticationProperties { RedirectUri = sanitized })
                .ConfigureAwait(false);
        };

        _ = endpoints.MapGet(options.Redirect.LoginPath, challenge);
        _ = endpoints.MapGet(options.Redirect.LogoutPath, signOut);
        return endpoints;
    }

    private static IHostEnvironment? ResolveHostEnvironment(IServiceCollection services) {
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHostEnvironment));
        return descriptor?.ImplementationInstance as IHostEnvironment;
    }

    private static void CopyConfiguration(FrontComposerAuthenticationOptions source, FrontComposerAuthenticationOptions target) {
        // The OptionsBuilder.Configure callback fires per-resolution. Copy from the captured
        // setup snapshot rather than re-running adopter-supplied configuration (which may have
        // observable side-effects such as secret fetches).
        CopyOidc(source.OpenIdConnect, target.OpenIdConnect);
        CopySaml2(source.Saml2, target.Saml2);
        CopyGitHub(source.GitHubOAuth, target.GitHubOAuth);
        CopyCustomBrokered(source.CustomBrokered, target.CustomBrokered);

        target.TenantClaimTypes.Clear();
        foreach (string alias in source.TenantClaimTypes) {
            target.TenantClaimTypes.Add(alias);
        }

        target.UserClaimTypes.Clear();
        foreach (string alias in source.UserClaimTypes) {
            target.UserClaimTypes.Add(alias);
        }

        target.Redirect.DefaultChallengeScheme = source.Redirect.DefaultChallengeScheme;
        target.Redirect.LoginPath = source.Redirect.LoginPath;
        target.Redirect.LogoutPath = source.Redirect.LogoutPath;

        target.TokenRelay.AccessTokenName = source.TokenRelay.AccessTokenName;
        target.TokenRelay.AuthenticationScheme = source.TokenRelay.AuthenticationScheme;
        target.TokenRelay.AllowGitHubOAuthTokenRelay = source.TokenRelay.AllowGitHubOAuthTokenRelay;
        target.TokenRelay.HostAccessTokenProvider = source.TokenRelay.HostAccessTokenProvider;

        target.Cookie.ApplySecureDefaults = source.Cookie.ApplySecureDefaults;
    }

    private static void CopyOidc(FrontComposerOpenIdConnectOptions source, FrontComposerOpenIdConnectOptions target) {
        target.Enabled = source.Enabled;
        target.ProviderName = source.ProviderName;
        target.ChallengeScheme = source.ChallengeScheme;
        target.SignInScheme = source.SignInScheme;
        target.CallbackPath = source.CallbackPath;
        target.SignedOutCallbackPath = source.SignedOutCallbackPath;
        target.Authority = source.Authority;
        target.MetadataAddress = source.MetadataAddress;
        target.ClientId = source.ClientId;
        target.ClientSecret = source.ClientSecret;
        target.Audience = source.Audience;
        target.ValidIssuer = source.ValidIssuer;
        target.ResponseType = source.ResponseType;
        target.SaveTokens = source.SaveTokens;
        target.Scopes.Clear();
        foreach (string scope in source.Scopes) {
            target.Scopes.Add(scope);
        }
    }

    private static void CopySaml2(FrontComposerSaml2Options source, FrontComposerSaml2Options target) {
        target.Enabled = source.Enabled;
        target.ChallengeScheme = source.ChallengeScheme;
        target.SignInScheme = source.SignInScheme;
        target.MetadataAddress = source.MetadataAddress;
        target.ConfigureHandler = source.ConfigureHandler;
    }

    private static void CopyGitHub(FrontComposerGitHubOAuthOptions source, FrontComposerGitHubOAuthOptions target) {
        target.Enabled = source.Enabled;
        target.ChallengeScheme = source.ChallengeScheme;
        target.SignInScheme = source.SignInScheme;
        target.CallbackPath = source.CallbackPath;
        target.ClientId = source.ClientId;
        target.ClientSecret = source.ClientSecret;
        target.AuthorizationEndpoint = source.AuthorizationEndpoint;
        target.TokenEndpoint = source.TokenEndpoint;
        target.UserInformationEndpoint = source.UserInformationEndpoint;
    }

    private static void CopyCustomBrokered(FrontComposerCustomBrokeredOptions source, FrontComposerCustomBrokeredOptions target) {
        target.Enabled = source.Enabled;
        target.ChallengeScheme = source.ChallengeScheme;
        target.SignInScheme = source.SignInScheme;
        target.ConfigureAuthentication = source.ConfigureAuthentication;
    }

    private static void AddAuthenticationHandlers(IServiceCollection services, FrontComposerAuthenticationOptions options) {
        if (options.SelectedProviderKind == FrontComposerAuthenticationProviderKind.None) {
            return;
        }

        string signInScheme = GetSignInScheme(options);
        string challengeScheme = options.SelectedChallengeScheme();
        AuthenticationBuilder builder = services.AddAuthentication(authentication => {
            authentication.DefaultScheme = signInScheme;
            authentication.DefaultChallengeScheme = challengeScheme;
        });

        // P10 — cookie scheme security defaults. Adopter can opt out via Cookie.ApplySecureDefaults=false
        // for special-case test hosts (HTTP-only loopback, etc.).
        bool secureDefaults = options.Cookie.ApplySecureDefaults;
        _ = builder.AddCookie(signInScheme, cookie => {
            cookie.LoginPath = options.Redirect.LoginPath;
            cookie.LogoutPath = options.Redirect.LogoutPath;
            if (secureDefaults) {
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.HttpOnly = true;
                cookie.SlidingExpiration = false;
            }
        });

        if (options.OpenIdConnect.Enabled) {
            _ = builder.AddOpenIdConnect(options.OpenIdConnect.ChallengeScheme, oidc => {
                oidc.SignInScheme = options.OpenIdConnect.SignInScheme;
                if (options.OpenIdConnect.Authority is not null) {
                    oidc.Authority = options.OpenIdConnect.Authority.ToString();
                }

                if (options.OpenIdConnect.MetadataAddress is not null) {
                    oidc.MetadataAddress = options.OpenIdConnect.MetadataAddress.ToString();
                }

                oidc.ClientId = options.OpenIdConnect.ClientId;
                oidc.ClientSecret = options.OpenIdConnect.ClientSecret;
                oidc.ResponseType = options.OpenIdConnect.ResponseType;
                oidc.CallbackPath = options.OpenIdConnect.CallbackPath;
                oidc.SignedOutCallbackPath = options.OpenIdConnect.SignedOutCallbackPath;
                // P9 — request access tokens be saved server-side so the framework token relay
                // can read them via `HttpContext.GetTokenAsync` per outbound operation.
                oidc.SaveTokens = options.OpenIdConnect.SaveTokens;

                if (!string.IsNullOrWhiteSpace(options.OpenIdConnect.Audience)) {
                    oidc.TokenValidationParameters.ValidAudience = options.OpenIdConnect.Audience;
                }

                // P15 — explicit issuer validation when supplied.
                if (!string.IsNullOrWhiteSpace(options.OpenIdConnect.ValidIssuer)) {
                    oidc.TokenValidationParameters.ValidIssuer = options.OpenIdConnect.ValidIssuer;
                }

                oidc.Scope.Clear();
                foreach (string scope in options.OpenIdConnect.Scopes.Where(s => !string.IsNullOrWhiteSpace(s))) {
                    oidc.Scope.Add(scope);
                }
            });
        }

        if (options.Saml2.Enabled) {
            options.Saml2.ConfigureHandler?.Invoke(builder, options.Saml2.ChallengeScheme);
        }

        if (options.GitHubOAuth.Enabled) {
            _ = builder.AddOAuth(options.GitHubOAuth.ChallengeScheme, oauth => {
                oauth.SignInScheme = options.GitHubOAuth.SignInScheme;
                oauth.ClientId = options.GitHubOAuth.ClientId ?? string.Empty;
                oauth.ClientSecret = options.GitHubOAuth.ClientSecret ?? string.Empty;
                oauth.CallbackPath = options.GitHubOAuth.CallbackPath;
                oauth.AuthorizationEndpoint = options.GitHubOAuth.AuthorizationEndpoint.ToString();
                oauth.TokenEndpoint = options.GitHubOAuth.TokenEndpoint.ToString();
                oauth.UserInformationEndpoint = options.GitHubOAuth.UserInformationEndpoint.ToString();
                oauth.SaveTokens = false;
            });
        }

        if (options.CustomBrokered.Enabled) {
            options.CustomBrokered.ConfigureAuthentication?.Invoke(builder, options.CustomBrokered.ChallengeScheme);
        }
    }

    private static string GetSignInScheme(FrontComposerAuthenticationOptions options)
        => options.SelectedProviderKind switch {
            FrontComposerAuthenticationProviderKind.OpenIdConnect => options.OpenIdConnect.SignInScheme,
            FrontComposerAuthenticationProviderKind.Saml2 => options.Saml2.SignInScheme,
            FrontComposerAuthenticationProviderKind.GitHubOAuth => options.GitHubOAuth.SignInScheme,
            FrontComposerAuthenticationProviderKind.CustomBrokered => options.CustomBrokered.SignInScheme,
            _ => "Hexalith.FrontComposer.Cookie",
        };
}
