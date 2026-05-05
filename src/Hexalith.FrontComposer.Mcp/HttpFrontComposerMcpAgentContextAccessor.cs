using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Schema;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class HttpFrontComposerMcpAgentContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerMcpOptions> options) : IFrontComposerMcpAgentContextAccessor {
    private const string SchemaFingerprintHeaderName = "x-frontcomposer-schema-fingerprint";
    private const int MaxSchemaFingerprintHeaderLength = 512;
    private const int MaxSchemaFingerprintAlgorithmLength = 128;
    private const int MaxSchemaFingerprintValueLength = 256;
    private const int Sha256ByteLength = 32;
    private const string ClientFingerprintCacheKey = "Hexalith.FrontComposer.Mcp.SchemaFingerprintHint";

    /// <summary>
    /// 8-6a re-review: pin the algorithms accepted at the trust boundary so a hostile client
    /// cannot smuggle arbitrary algorithm strings into structured logs and the negotiator. This
    /// matches <see cref="McpSchemaNegotiator"/>'s <c>SupportedAlgorithms</c> set; any expansion
    /// must update both sides together.
    /// </summary>
    private static readonly HashSet<string> SupportedAlgorithms = new(StringComparer.Ordinal) {
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1,
    };

    public IServiceProvider? RequestServices => httpContextAccessor.HttpContext?.RequestServices;

    public SchemaFingerprint? ClientFingerprintHint {
        get {
            HttpContext? http = httpContextAccessor.HttpContext;
            if (http is null
                || !http.Request.Headers.TryGetValue(SchemaFingerprintHeaderName, out StringValues values)) {
                return null;
            }

            // 8-6a re-review: memoize the parsed fingerprint on HttpContext.Items so repeated
            // accesses (admission + invoker, projection reader pre-query + pre-render) parse
            // and validate the header exactly once. Also avoids re-throwing the malformed-request
            // exception multiple times per request. The key includes the assembly identifier so
            // hosts that compose multiple FrontComposer pipelines do not collide.
            if (http.Items.TryGetValue(ClientFingerprintCacheKey, out object? cached)) {
                return cached as SchemaFingerprint;
            }

            if (values.Count != 1) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
            }

            SchemaFingerprint? parsed = ParseClientFingerprint(values[0]);
            http.Items[ClientFingerprintCacheKey] = parsed;
            return parsed;
        }
    }

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

    private static SchemaFingerprint? ParseClientFingerprint(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > MaxSchemaFingerprintHeaderLength) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        int separator = trimmed.IndexOf(':');
        if (separator <= 0 || separator == trimmed.Length - 1 || trimmed.IndexOf(':', separator + 1) >= 0) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        string algorithmId = trimmed[..separator];
        string encodedFingerprint = trimmed[(separator + 1)..];
        if (algorithmId.Length > MaxSchemaFingerprintAlgorithmLength
            || encodedFingerprint.Length > MaxSchemaFingerprintValueLength
            || algorithmId.Any(char.IsWhiteSpace)
            || encodedFingerprint.Any(char.IsWhiteSpace)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        // 8-6a re-review: reject unsupported algorithms at the trust boundary instead of letting
        // the client smuggle arbitrary algorithm strings into the negotiator and structured logs.
        if (!SupportedAlgorithms.Contains(algorithmId)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        // 8-6a re-review: validate decoded byte length matches the expected SHA-256 hash size.
        // A 1-byte base64 value previously slipped through and was forwarded to the negotiator
        // as a "fingerprint". Drop the FormatException as inner-exception so its text cannot
        // leak through telemetry sinks that log ex.InnerException.Message (AC15).
        byte[] decoded;
        try {
            decoded = Convert.FromBase64String(encodedFingerprint);
        }
        catch (FormatException) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        if (decoded.Length != Sha256ByteLength) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        return new SchemaFingerprint(algorithmId, encodedFingerprint);
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
