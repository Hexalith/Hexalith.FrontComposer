using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Schema;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class HttpFrontComposerMcpAgentContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerMcpOptions> options,
    FrontComposerMcpApiKeyCredentialStore? apiKeyCredentialStore = null) : IFrontComposerMcpAgentContextAccessor {
    private const string SchemaFingerprintHeaderName = "x-frontcomposer-schema-fingerprint";
    private const int MaxSchemaFingerprintHeaderLength = 256;
    private const int MaxSchemaFingerprintAlgorithmLength = 128;
    private const int Sha256HexLength = 64;
    private const string ClientFingerprintCacheKey = "Hexalith.FrontComposer.Mcp.SchemaFingerprintHint";

    // C3 (Group D / chunk-2 re-review): sentinel cached on HttpContext.Items when the header
    // parse failed, so subsequent property accesses on the same request return the cached
    // failure instead of re-parsing and re-throwing on every access (admission, invoker, pre-
    // query, pre-render). The exception is re-thrown each access to preserve fail-closed
    // semantics, but parsing happens exactly once.
    private static readonly object MalformedFingerprintSentinel = new();

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
    private readonly FrontComposerMcpApiKeyCredentialStore _apiKeyCredentialStore =
        apiKeyCredentialStore ?? new FrontComposerMcpApiKeyCredentialStore(options);

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
            // and validate the header exactly once. The malformed-request sentinel covers the
            // failure path so a malformed header still triggers fail-closed on every access
            // (preserving the security posture) without re-running the parser.
            if (http.Items.TryGetValue(ClientFingerprintCacheKey, out object? cached)) {
                return cached is SchemaFingerprint hint ? hint
                    : ReferenceEquals(cached, MalformedFingerprintSentinel)
                        ? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest)
                        : null;
            }

            try {
                if (values.Count != 1) {
                    throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
                }

                SchemaFingerprint? parsed = ParseClientFingerprint(values[0]);
                http.Items[ClientFingerprintCacheKey] = parsed;
                return parsed;
            }
            catch (FrontComposerMcpException ex) when (ex.Category == FrontComposerMcpFailureCategory.MalformedRequest) {
                http.Items[ClientFingerprintCacheKey] = MalformedFingerprintSentinel;
                throw;
            }
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

            FrontComposerMcpApiKeyIdentity? matched = _apiKeyCredentialStore.Match(candidate) ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
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
            || encodedFingerprint.Length != Sha256HexLength
            || algorithmId.Any(char.IsWhiteSpace)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        // 8-6a re-review: reject unsupported algorithms at the trust boundary instead of letting
        // the client smuggle arbitrary algorithm strings into the negotiator and structured logs.
        if (!SupportedAlgorithms.Contains(algorithmId)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        // C3 (Group D / chunk-2 re-review): server-side fingerprints emit lowercase hex via
        // CanonicalSchemaMaterial.Sha256Hex (Contracts/Schema/SchemaFingerprintContracts.cs); the
        // client must use the SAME wire encoding so byte-equality short-circuits in McpSchemaNegotiator
        // can match identical structural schemas. Reject anything that is not exactly 64 lowercase
        // hex characters (the SHA-256 hex form). Previously the parser accepted base64, which
        // never matched the server's hex value — making the byte-match path unreachable and
        // forcing every fingerprint-bearing request to fall through to structural snapshot compare.
        if (!IsLowercaseSha256Hex(encodedFingerprint)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        return new SchemaFingerprint(algorithmId, encodedFingerprint);
    }

    private static bool IsLowercaseSha256Hex(string value) {
        foreach (char c in value) {
            bool isDigit = c is >= '0' and <= '9';
            bool isLowerHex = c is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex) {
                return false;
            }
        }

        return true;
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
