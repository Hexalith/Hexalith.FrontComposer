using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Extensions;

/// <summary>
/// Maps the FrontComposer MCP endpoints behind the framework's fail-closed host checks.
/// </summary>
public static class FrontComposerMcpEndpointRouteBuilderExtensions {
    /// <summary>
    /// Maps the FrontComposer MCP Streamable HTTP endpoints and requires an authenticated caller on every one of them.
    /// </summary>
    /// <remarks>
    /// <para>Every mapped endpoint carries one authorization policy object, built when this method runs: the host's
    /// default policy, read from <see cref="IAuthorizationPolicyProvider.GetDefaultPolicyAsync"/>, combined with
    /// <see cref="AuthorizationPolicyBuilder.RequireAuthenticatedUser"/>. ASP.NET Core requires every policy attached
    /// to an endpoint, so a policy the host appends with <c>RequireAuthorization(...)</c> on the returned builder adds
    /// requirements; it cannot remove the authenticated-user requirement or the default policy.</para>
    /// <para><c>AllowAnonymous()</c> on the returned builder skips authorization for the endpoints. That is an
    /// explicit, host-owned bypass of this requirement; the framework does not detect or support it.</para>
    /// <para>Outside Development, as reported by <see cref="HostEnvironmentEnvExtensions.IsDevelopment"/>, mapping
    /// rejects <see cref="AllowAllMcpTenantToolGate"/> and <see cref="AllowAllResourceVisibilityGate"/>. The gates are
    /// resolved once, from a temporary asynchronous service scope that is disposed before this method returns, so a gate
    /// whose constructor needs an active HTTP request cannot be resolved here. Only those exact types are rejected; a
    /// decorator that wraps an allow-all gate is the host's responsibility.</para>
    /// <para>These checks apply only to this mapping. The SDK's own <c>MapMcp</c>, the stdio transport and
    /// <c>McpServer.Create</c> bypass them, and the host owns any server it composes that way.</para>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern; defaults to <see cref="FrontComposerMcpOptions.EndpointPattern"/>.</param>
    /// <returns>The convention builder for the mapped MCP endpoints.</returns>
    /// <exception cref="InvalidOperationException">
    /// FrontComposer MCP, ASP.NET Core authentication, or ASP.NET Core authorization services are not registered, or an
    /// allow-all gate is registered outside Development.
    /// </exception>
    public static IEndpointConventionBuilder MapFrontComposerMcp(this IEndpointRouteBuilder endpoints, string? pattern = null) {
        ArgumentNullException.ThrowIfNull(endpoints);

        IOptions<FrontComposerMcpOptions>? options = endpoints.ServiceProvider.GetService<IOptions<FrontComposerMcpOptions>>() ?? throw new InvalidOperationException(
                "FrontComposer MCP services are not registered. Call IServiceCollection.AddFrontComposerMcp(...) before MapFrontComposerMcp(...).");
        string route = pattern ?? options.Value.EndpointPattern;
        _ = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value.ResourceCollection;
        AuthorizationPolicy policy = ValidateHostComposition(endpoints.ServiceProvider);

        return endpoints.MapMcp(route).RequireAuthorization(policy);
    }

    private static AuthorizationPolicy ValidateHostComposition(IServiceProvider services) {
        // Gate dependencies may implement only IAsyncDisposable, which a synchronous scope Dispose() rejects.
        AsyncServiceScope scope = services.CreateAsyncScope();
        try {
            IServiceProvider scoped = scope.ServiceProvider;
            if (scoped.GetService<IHostEnvironment>()?.IsDevelopment() != true) {
                RejectAllowAllGates(scoped);
            }

            return BuildAuthenticatedUserPolicy(scoped);
        }
        finally {
            scope.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static void RejectAllowAllGates(IServiceProvider services) {
        if (services.GetRequiredService<IFrontComposerMcpTenantToolGate>() is AllowAllMcpTenantToolGate) {
            throw new InvalidOperationException("MapFrontComposerMcp requires a restrictive IFrontComposerMcpTenantToolGate outside Development; AllowAllMcpTenantToolGate is sample/dev only.");
        }

        if (services.GetRequiredService<IFrontComposerMcpResourceVisibilityGate>() is AllowAllResourceVisibilityGate) {
            throw new InvalidOperationException("MapFrontComposerMcp requires a restrictive IFrontComposerMcpResourceVisibilityGate outside Development; AllowAllResourceVisibilityGate is sample/dev only.");
        }
    }

    private static AuthorizationPolicy BuildAuthenticatedUserPolicy(IServiceProvider services) {
        bool hasAuthentication = services.GetService<IAuthenticationSchemeProvider>() is not null;
        IAuthorizationPolicyProvider? policyProvider = services.GetService<IAuthorizationPolicyProvider>();
        if (!hasAuthentication || policyProvider is null) {
            List<string> missingCalls = [];
            if (!hasAuthentication) {
                missingCalls.Add("IServiceCollection.AddAuthentication(...)");
            }

            if (policyProvider is null) {
                missingCalls.Add("IServiceCollection.AddAuthorization(...)");
            }

            throw new InvalidOperationException(
                "MapFrontComposerMcp requires ASP.NET Core authentication and authorization services so anonymous MCP requests are rejected before dispatch. Call "
                + string.Join(" and ", missingCalls)
                + " before MapFrontComposerMcp(...).");
        }

        // A policy object replaces ASP.NET Core's implicit default policy for an empty [Authorize], so the host's
        // default policy is folded in here and the authenticated-user requirement cannot be substituted away.
        AuthorizationPolicy defaultPolicy = policyProvider.GetDefaultPolicyAsync().GetAwaiter().GetResult();
        return new AuthorizationPolicyBuilder()
            .Combine(defaultPolicy)
            .RequireAuthenticatedUser()
            .Build();
    }
}
