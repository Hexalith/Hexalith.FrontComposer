using Hexalith.FrontComposer.Mcp;

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

    /// <summary>
    /// Gets the Test-only credential that authenticates a caller the sample gates never admit. The browser harness
    /// uses it to probe the sanitized empty and failure shapes with an authenticated principal.
    /// </summary>
    internal const string NonAdmittedApiKey = "counter-e2e-mcp-non-admitted-key";

    /// <summary>Gets the tenant associated with the Test-only non-admitted credential.</summary>
    internal const string NonAdmittedTenantId = "probe-tenant";

    /// <summary>Gets the user associated with the Test-only non-admitted credential.</summary>
    internal const string NonAdmittedUserId = "probe-user";

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
