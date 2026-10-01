using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Counter.Web;

/// <summary>Authenticates only the explicit local Counter sample API key.</summary>
internal sealed class CounterMcpSampleAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "CounterMcpSample";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-FrontComposer-Mcp-Key", out Microsoft.Extensions.Primitives.StringValues values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (values.Count != 1 || !string.Equals(values[0], CounterMcpSampleAccessPolicy.ApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid sample credential."));
        }

        ClaimsIdentity identity = new([
            new Claim("TenantId", CounterMcpSampleAccessPolicy.TenantId),
            new Claim("UserId", CounterMcpSampleAccessPolicy.UserId),
        ], Scheme.Name);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
