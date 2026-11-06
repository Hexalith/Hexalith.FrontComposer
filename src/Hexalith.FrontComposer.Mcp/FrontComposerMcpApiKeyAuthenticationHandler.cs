using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Authenticates requests that carry a configured <see cref="FrontComposerMcpOptions.ApiKeys"/> entry in the
/// <see cref="FrontComposerMcpOptions.ApiKeyHeaderName"/> header, before ASP.NET Core authorization runs.
/// </summary>
/// <remarks>
/// A request without the header is not this scheme's concern (<see cref="AuthenticateResult.NoResult"/>). A
/// multi-valued, blank, or unmatched header fails with a fixed message that never contains the candidate key.
/// Keys are matched by <see cref="FrontComposerMcpApiKeyCredentialStore"/>, the same hashed fixed-time matcher the
/// in-handler agent context accessor uses. The accessor also rejects a request whose other authenticated claims
/// carry a tenant or user that conflicts with the matched key's identity.
/// </remarks>
/// <param name="options">The authentication scheme options monitor.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
/// <param name="mcpOptions">The FrontComposer MCP options that name the header and hold the configured keys.</param>
/// <param name="credentialStore">The hashed fixed-time API-key matcher.</param>
internal sealed class FrontComposerMcpApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<FrontComposerMcpOptions> mcpOptions,
    FrontComposerMcpApiKeyCredentialStore credentialStore) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    /// <summary>
    /// The fixed failure message for every rejected API-key header. It never contains the candidate key.
    /// </summary>
    internal const string FailureMessage = "The FrontComposer MCP API key was rejected.";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.TryGetValue(mcpOptions.Value.ApiKeyHeaderName, out StringValues values)) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? candidate = values.Count == 1 ? values[0] : null;
        if (string.IsNullOrWhiteSpace(candidate)) {
            return Task.FromResult(AuthenticateResult.Fail(FailureMessage));
        }

        FrontComposerMcpApiKeyIdentity? matched = credentialStore.Match(candidate);
        if (matched is null) {
            return Task.FromResult(AuthenticateResult.Fail(FailureMessage));
        }

        ClaimsIdentity identity = new(
            [
                new Claim("TenantId", matched.TenantId.Trim()),
                new Claim("UserId", matched.UserId.Trim()),
            ],
            Scheme.Name);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
