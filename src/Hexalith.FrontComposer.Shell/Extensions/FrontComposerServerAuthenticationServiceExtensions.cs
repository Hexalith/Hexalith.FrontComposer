using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Blazor Server security wiring for FrontComposer hosts. These helpers are an explicit, Server-scoped
/// opt-in (they depend on <see cref="ServerAuthenticationStateProvider"/> and the circuit), kept out
/// of the render-mode-agnostic <c>AddHexalithFrontComposerAuthentication</c> so non-Server adopters
/// are unaffected.
/// </summary>
public static class FrontComposerServerAuthenticationServiceExtensions {
    /// <summary>
    /// Flows the cookie-authenticated principal into interactive Server components, replacing the
    /// fail-closed <c>NullAuthenticationStateProvider</c> the FrontComposer Quickstart registers.
    /// Call from a Blazor Server host once interactive OIDC sign-in is wired; without it the header
    /// account menu and <c>&lt;AuthorizeView&gt;</c> always see an anonymous user after sign-in.
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerServerAuthenticationState(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddCascadingAuthenticationState();
        services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>());
        return services;
    }

    /// <summary>
    /// One-call convenience for a Blazor Server host: composes the authentication bridge
    /// (<c>AddHexalithFrontComposerAuthentication</c>), the server authentication-state provider
    /// (<see cref="AddHexalithFrontComposerServerAuthenticationState"/>), and the per-user token relay
    /// (<see cref="FrontComposerTokenRelayServiceExtensions.AddHexalithFrontComposerTokenRelay"/>) in
    /// the correct order. Domain modules supply only the provider configuration.
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerServerSecurity(
        this IServiceCollection services,
        Action<Options.FrontComposerAuthenticationOptions> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        _ = services.AddHexalithFrontComposerAuthentication(configure);
        _ = services.AddHexalithFrontComposerServerAuthenticationState();
        _ = services.AddHexalithFrontComposerTokenRelay();

        // Replace the HttpContext-only IUserContextAccessor (registered by
        // AddHexalithFrontComposerAuthentication) with the circuit-aware variant so interactive Server
        // components resolve the signed-in user when HttpContext is null. Ordered after the token relay
        // so its CircuitServicesAccessor dependency is registered.
        _ = services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ServerCircuitUserContextAccessor>());
        return services;
    }
}
