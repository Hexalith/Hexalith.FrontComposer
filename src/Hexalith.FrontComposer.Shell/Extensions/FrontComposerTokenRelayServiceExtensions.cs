using System.Security.Claims;
using System.Security.Cryptography;

using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Registration helpers for circuit-safe, per-session bearer-token relay from a FrontComposer Blazor
/// Server host. Generic framework infrastructure: a domain module supplies only which provider and
/// which gateways to authorize, never the relay plumbing itself.
/// </summary>
public static class FrontComposerTokenRelayServiceExtensions {
    /// <summary>
    /// The default OIDC challenge scheme. Relay configuration follows the selected scheme in
    /// <see cref="FrontComposerOpenIdConnectOptions.ChallengeScheme"/>.
    /// </summary>
    public const string OidcChallengeScheme = "Hexalith.FrontComposer.Oidc";

    /// <summary>
    /// Registers the circuit-safe token relay services and captures the session's access token on each
    /// OIDC sign-in. Call after <c>AddHexalithFrontComposerAuthentication</c>. HTTPS-metadata
    /// enforcement is relaxed only in the Development environment (local Keycloak over http);
    /// every other environment keeps it enforced.
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerTokenRelay(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddHttpContextAccessor();
        _ = services.AddOptions<FrontComposerAuthenticationOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<FrontComposerUserTokenStore>();
        services.TryAddSingleton<CircuitServicesAccessor>();
        _ = services.AddScoped<CircuitHandler, FrontComposerCircuitServicesHandler>();
        _ = services.AddTransient<FrontComposerGatewayAuthorizationHandler>();

        // Keep tokens for relay and capture the access token into the per-session store when the
        // final authentication ticket is received. RequireHttpsMetadata is relaxed only for local-dev http
        // Keycloak (Development); production/staging keep metadata over https enforced.
        _ = services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>>(provider =>
        {
            FrontComposerUserTokenStore tokenStore = provider.GetRequiredService<FrontComposerUserTokenStore>();
            IHostEnvironment environment = provider.GetRequiredService<IHostEnvironment>();
            FrontComposerAuthenticationOptions authentication = provider.GetRequiredService<IOptions<FrontComposerAuthenticationOptions>>().Value;
            return new ConfigureNamedOptions<OpenIdConnectOptions>(authentication.OpenIdConnect.ChallengeScheme, options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveTokens = true;

                Func<TicketReceivedContext, Task>? previous = options.Events.OnTicketReceived;
                options.Events.OnTicketReceived = async context => {
                    if (previous is not null) {
                        await previous(context).ConfigureAwait(false);
                    }

                    // Never accept a provider-supplied local session identifier.
                    if (context.Principal is not { } principal || principal.Identity is not ClaimsIdentity identity
                        || !identity.IsAuthenticated || context.Result is not null)
                    {
                        return;
                    }

                    foreach (ClaimsIdentity claimsIdentity in principal.Identities)
                    {
                        foreach (Claim claim in claimsIdentity.FindAll(FrontComposerTokenSession.ClaimType).ToArray())
                        {
                            claimsIdentity.RemoveClaim(claim);
                        }
                    }

                    identity.AddClaim(new Claim(FrontComposerTokenSession.ClaimType, Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
                    FrontComposerTokenSession? session = FrontComposerTokenSession.Resolve(principal, authentication);
                    string? token = context.Properties?.GetTokenValue(authentication.TokenRelay.AccessTokenName);
                    DateTimeOffset? expiresAtUtc = ResolveTokenExpiry(context);
                    if (!string.IsNullOrWhiteSpace(token) && session is not null && expiresAtUtc.HasValue)
                    {
                        tokenStore.Set(session, token, expiresAtUtc.Value);
                    }
                };
            });
        });

        return services;
    }

    /// <summary>Adds the bearer-token relay handler to a gateway HTTP client.</summary>
    public static IHttpClientBuilder AddFrontComposerGatewayAuthorization(this IHttpClientBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddHttpMessageHandler<FrontComposerGatewayAuthorizationHandler>();
    }

    private static DateTimeOffset? ResolveTokenExpiry(TicketReceivedContext context)
    {
        string? expiresAt = context.Properties?.GetTokenValue("expires_at");
        return DateTimeOffset.TryParse(expiresAt, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset expiry) ? expiry : null;
    }
}
