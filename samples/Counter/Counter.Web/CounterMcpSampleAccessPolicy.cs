using System.Net;

using Hexalith.FrontComposer.Mcp;

using Microsoft.AspNetCore.Authorization;

namespace Counter.Web;

/// <summary>Defines the explicit identity admitted by the local Counter MCP sample.</summary>
internal static class CounterMcpSampleAccessPolicy
{
    /// <summary>Gets the local sample credential used by the browser harness.</summary>
    internal const string ApiKey = "counter-e2e-mcp-key";

    /// <summary>Gets the tenant associated with the local sample credential.</summary>
    internal const string TenantId = "demo-tenant";

    /// <summary>Gets the user associated with the local sample credential.</summary>
    internal const string UserId = "demo-user";

    /// <summary>Admits local Test transport envelopes; MCP gates independently require the sample credential.</summary>
    /// <returns>The explicit loopback transport policy for the Test sample host.</returns>
    internal static AuthorizationPolicy CreateTestTransportPolicy() => new AuthorizationPolicyBuilder()
        .RequireAssertion(context => context.Resource is HttpContext http
            && http.Connection.RemoteIpAddress is IPAddress address
            && IPAddress.IsLoopback(address))
        .Build();

    /// <summary>Checks that authenticated context and principal claims match the sample identity.</summary>
    /// <param name="context">The authenticated request context.</param>
    /// <returns>Whether the request belongs to the explicit local sample identity.</returns>
    internal static bool IsAdmitted(FrontComposerMcpAgentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Principal.Identity?.IsAuthenticated == true
            && string.Equals(context.TenantId, TenantId, StringComparison.Ordinal)
            && string.Equals(context.UserId, UserId, StringComparison.Ordinal)
            && string.Equals(context.Principal.FindFirst("TenantId")?.Value, TenantId, StringComparison.Ordinal)
            && string.Equals(context.Principal.FindFirst("UserId")?.Value, UserId, StringComparison.Ordinal);
    }
}
