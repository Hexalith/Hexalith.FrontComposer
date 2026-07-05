using System.Globalization;

using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Registration helpers for circuit-safe, per-user bearer-token relay from a FrontComposer Blazor
/// Server host. Generic framework infrastructure: a domain module supplies only which provider and
/// which gateways to authorize, never the relay plumbing itself.
/// </summary>
public static class FrontComposerTokenRelayServiceExtensions {
    /// <summary>
    /// The OIDC challenge scheme the relay hooks to capture access tokens. Matches the default
    /// <see cref="FrontComposerOpenIdConnectOptions.ChallengeScheme"/>.
    /// </summary>
    public const string OidcChallengeScheme = "Hexalith.FrontComposer.Oidc";

    /// <summary>
    /// Registers the circuit-safe token relay services and captures the user's access token on each
    /// OIDC sign-in. Call after <c>AddHexalithFrontComposerAuthentication</c>. HTTPS-metadata
    /// enforcement is relaxed only in the Development environment (local Keycloak over http);
    /// every other environment keeps it enforced.
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerTokenRelay(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<FrontComposerUserTokenStore>();
        services.TryAddSingleton<CircuitServicesAccessor>();
        _ = services.AddScoped<CircuitHandler, FrontComposerCircuitServicesHandler>();
        _ = services.AddTransient<FrontComposerGatewayAuthorizationHandler>();

        // Keep tokens for relay and capture the access token into the per-user store when the
        // authorization code is validated. RequireHttpsMetadata is relaxed only for local-dev http
        // Keycloak (Development); production/staging keep metadata over https enforced.
        _ = services.AddOptions<OpenIdConnectOptions>(OidcChallengeScheme)
            .Configure<FrontComposerUserTokenStore, TimeProvider, IHostEnvironment>((options, tokenStore, timeProvider, environment) => {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveTokens = true;

                Func<TokenValidatedContext, Task>? previous = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context => {
                    if (previous is not null) {
                        await previous(context).ConfigureAwait(false);
                    }

                    string? token = context.TokenEndpointResponse?.AccessToken;
                    string? userId = context.Principal?.FindFirst("sub")?.Value
                        ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    DateTimeOffset? expiresAtUtc = ResolveTokenExpiry(context, timeProvider);
                    if (!string.IsNullOrEmpty(token) && userId is not null && expiresAtUtc.HasValue) {
                        tokenStore.Set(userId, token, expiresAtUtc.Value);
                    }
                };
            });

        return services;
    }

    /// <summary>Adds the bearer-token relay handler to a gateway HTTP client.</summary>
    public static IHttpClientBuilder AddFrontComposerGatewayAuthorization(this IHttpClientBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddHttpMessageHandler<FrontComposerGatewayAuthorizationHandler>();
    }

    private static DateTimeOffset? ResolveTokenExpiry(TokenValidatedContext context, TimeProvider timeProvider) {
        string? expiresAt = context.Properties?.GetTokenValue("expires_at");
        if (!string.IsNullOrWhiteSpace(expiresAt)
            && DateTimeOffset.TryParse(
                expiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset parsed)) {
            return parsed.ToUniversalTime();
        }

        string? expiresIn = context.TokenEndpointResponse?.ExpiresIn;
        if (!string.IsNullOrWhiteSpace(expiresIn)
            && int.TryParse(expiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            && seconds > 0) {
            return timeProvider.GetUtcNow().AddSeconds(seconds);
        }

        return null;
    }
}
