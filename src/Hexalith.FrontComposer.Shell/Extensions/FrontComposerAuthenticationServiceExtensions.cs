using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

public static class FrontComposerAuthenticationServiceExtensions {
    public static IServiceCollection AddHexalithFrontComposerAuthentication(
        this IServiceCollection services,
        Action<FrontComposerAuthenticationOptions> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        _ = services.AddOptions<FrontComposerAuthenticationOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<FrontComposerAuthenticationOptions>, FrontComposerAuthenticationOptionsValidator>());
        services.AddHttpContextAccessor();

        FrontComposerAuthenticationOptions setup = new();
        configure(setup);
        AddAuthenticationHandlers(services, setup);

        services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ClaimsPrincipalUserContextAccessor>());
        services.Replace(ServiceDescriptor.Scoped<IAuthRedirector, FrontComposerAuthRedirector>());
        services.TryAddSingleton<FrontComposerAccessTokenProvider>();
        _ = services.AddOptions<EventStoreOptions>()
            .Configure<FrontComposerAccessTokenProvider>((eventStore, tokenProvider) => {
                eventStore.AccessTokenProvider ??= tokenProvider.GetAccessTokenAsync;
            });

        return services;
    }

    private static void AddAuthenticationHandlers(IServiceCollection services, FrontComposerAuthenticationOptions options) {
        string signInScheme = GetSignInScheme(options);
        string challengeScheme = options.SelectedChallengeScheme();
        AuthenticationBuilder builder = services.AddAuthentication(authentication => {
            authentication.DefaultScheme = signInScheme;
            authentication.DefaultChallengeScheme = challengeScheme;
        });

        _ = builder.AddCookie(signInScheme, cookie => {
            cookie.LoginPath = "/authentication/challenge";
            cookie.LogoutPath = "/authentication/sign-out";
        });

        if (options.OpenIdConnect.Enabled) {
            _ = builder.AddOpenIdConnect(options.OpenIdConnect.ChallengeScheme, oidc => {
                oidc.SignInScheme = options.OpenIdConnect.SignInScheme;
                oidc.Authority = options.OpenIdConnect.Authority?.ToString();
                oidc.MetadataAddress = options.OpenIdConnect.MetadataAddress?.ToString();
                oidc.ClientId = options.OpenIdConnect.ClientId;
                oidc.ClientSecret = options.OpenIdConnect.ClientSecret;
                oidc.ResponseType = options.OpenIdConnect.ResponseType;
                oidc.CallbackPath = options.OpenIdConnect.CallbackPath;
                oidc.SignedOutCallbackPath = options.OpenIdConnect.SignedOutCallbackPath;
                if (!string.IsNullOrWhiteSpace(options.OpenIdConnect.Audience)) {
                    oidc.TokenValidationParameters.ValidAudience = options.OpenIdConnect.Audience;
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
