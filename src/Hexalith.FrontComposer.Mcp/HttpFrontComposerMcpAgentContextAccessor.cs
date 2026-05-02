using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class HttpFrontComposerMcpAgentContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerMcpOptions> options) : IFrontComposerMcpAgentContextAccessor {
    public FrontComposerMcpAgentContext GetContext() {
        HttpContext? http = httpContextAccessor.HttpContext;

        // API-key path: only honored when exactly one non-whitespace header value matches a configured key.
        // Multi-valued headers and empty/whitespace values fail-closed instead of falling through to claims.
        if (http is not null
            && http.Request.Headers.TryGetValue(options.Value.ApiKeyHeaderName, out Microsoft.Extensions.Primitives.StringValues headerValues)) {
            if (headerValues.Count != 1) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
            }

            string? candidate = headerValues[0];
            if (string.IsNullOrWhiteSpace(candidate)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
            }

            FrontComposerMcpApiKeyIdentity? matched = MatchApiKey(candidate, options.Value.ApiKeys);
            if (matched is null) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
            }

            return Create(matched.TenantId, matched.UserId, claimsSource: null);
        }

        ClaimsPrincipal user = http?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        if (user.Identity?.IsAuthenticated != true) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
        }

        string? tenant = FirstClaim(user, options.Value.TenantClaimTypes);
        string? userId = FirstClaim(user, options.Value.UserClaimTypes);
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(userId)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.TenantMissing);
        }

        return Create(tenant, userId, claimsSource: user);
    }

    private static FrontComposerMcpApiKeyIdentity? MatchApiKey(
        string candidate,
        IDictionary<string, FrontComposerMcpApiKeyIdentity> apiKeys) {
        // Constant-time scan: walk every registered key so timing does not reveal valid prefixes.
        byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);
        FrontComposerMcpApiKeyIdentity? matched = null;
        foreach (KeyValuePair<string, FrontComposerMcpApiKeyIdentity> entry in apiKeys) {
            if (string.IsNullOrWhiteSpace(entry.Key)) {
                continue;
            }

            byte[] storedBytes = Encoding.UTF8.GetBytes(entry.Key);
            if (candidateBytes.Length == storedBytes.Length
                && CryptographicOperations.FixedTimeEquals(candidateBytes, storedBytes)) {
                matched = entry.Value;
            }
        }

        return matched;
    }

    private static string? FirstClaim(ClaimsPrincipal principal, IEnumerable<string> claimTypes) {
        foreach (string claimType in claimTypes) {
            string? value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) {
                return value.Trim();
            }
        }

        return null;
    }

    private static FrontComposerMcpAgentContext Create(string tenantId, string userId, ClaimsPrincipal? claimsSource) {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.TenantMissing);
        }

        // Preserve original IdP claims (roles/groups) so future policy enforcement (Story 8-2) can inspect them.
        // For API-key paths, claimsSource is null; identity carries only TenantId/UserId.
        List<Claim> claims = [
            new Claim("TenantId", tenantId.Trim()),
            new Claim("UserId", userId.Trim()),
        ];
        if (claimsSource?.Identity is ClaimsIdentity sourceIdentity) {
            foreach (Claim original in sourceIdentity.Claims) {
                if (original.Type is "TenantId" or "UserId") {
                    continue;
                }

                claims.Add(new Claim(original.Type, original.Value, original.ValueType, original.Issuer, original.OriginalIssuer));
            }
        }

        ClaimsIdentity identity = new(claims, authenticationType: "FrontComposerMcp");
        return new FrontComposerMcpAgentContext(tenantId.Trim(), userId.Trim(), new ClaimsPrincipal(identity));
    }
}
