using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.FrontComposer.Mcp.Extensions;

/// <summary>
/// Registers the FrontComposer MCP API-key authentication scheme.
/// </summary>
public static class FrontComposerMcpAuthenticationBuilderExtensions {
    /// <summary>
    /// Adds an authentication scheme that authenticates the keys configured in
    /// <see cref="FrontComposerMcpOptions.ApiKeys"/>, read from the <see cref="FrontComposerMcpOptions.ApiKeyHeaderName"/>
    /// header, so they are honored before the authorization that <see cref="FrontComposerMcpEndpointRouteBuilderExtensions.MapFrontComposerMcp"/>
    /// requires.
    /// </summary>
    /// <remarks>
    /// <para>A matched key produces a <see cref="System.Security.Claims.ClaimsIdentity"/> whose authentication type is
    /// <paramref name="authenticationScheme"/> and whose <c>TenantId</c> and <c>UserId</c> claims come from the matched
    /// <see cref="FrontComposerMcpApiKeyIdentity"/>. A request without the header yields no result, so another scheme
    /// can authenticate it. A multi-valued, blank, or unmatched header fails with a fixed message; neither the
    /// response nor the log contains the key.</para>
    /// <para>ASP.NET Core authenticates only the default scheme and the schemes named by the authorization policy.
    /// Make this scheme the default, for example <c>AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme)</c>,
    /// or add it to <see cref="Microsoft.AspNetCore.Authorization.AuthorizationOptions.DefaultPolicy"/>'s authentication schemes.</para>
    /// </remarks>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The scheme name; defaults to <see cref="FrontComposerMcpAuthenticationDefaults.ApiKeyScheme"/>.</param>
    /// <returns>The same authentication builder, for chaining.</returns>
    public static AuthenticationBuilder AddFrontComposerMcpApiKey(
        this AuthenticationBuilder builder,
        string authenticationScheme = FrontComposerMcpAuthenticationDefaults.ApiKeyScheme) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        // AddFrontComposerMcp registers the same singleton; registering it here keeps the scheme resolvable when the
        // host calls AddAuthentication first. Without AddFrontComposerMcp no key is configured, so every key fails.
        builder.Services.TryAddSingleton<FrontComposerMcpApiKeyCredentialStore>();
        return builder.AddScheme<AuthenticationSchemeOptions, FrontComposerMcpApiKeyAuthenticationHandler>(
            authenticationScheme,
            configureOptions: null);
    }
}
