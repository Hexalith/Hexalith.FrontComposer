using System.Security.Claims;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

internal static class FrontComposerClaimExtractor {
    public static FrontComposerClaimExtractionResult Extract(
        ClaimsPrincipal? principal,
        IReadOnlyList<string> tenantAliases,
        IReadOnlyList<string> userAliases) {
        if (principal?.Identity?.IsAuthenticated != true) {
            return FrontComposerClaimExtractionResult.Fail("Unauthenticated", [], []);
        }

        SegmentResult tenant = ExtractSegment(principal, tenantAliases);
        SegmentResult user = ExtractSegment(principal, userAliases);
        if (tenant.Value is null || user.Value is null) {
            return FrontComposerClaimExtractionResult.Fail(tenant.Reason ?? user.Reason ?? "InvalidClaim", tenant.InvolvedAliases, user.InvolvedAliases);
        }

        return FrontComposerClaimExtractionResult.Success(tenant.Value, user.Value);
    }

    private static SegmentResult ExtractSegment(ClaimsPrincipal principal, IReadOnlyList<string> aliases) {
        List<(string Alias, string Value)> found = [];
        foreach (string alias in aliases) {
            Claim[] claims = principal.FindAll(alias).ToArray();
            if (claims.Length > 1) {
                return SegmentResult.Fail("MultiValuedClaim", [alias]);
            }

            if (claims.Length == 0) {
                continue;
            }

            string normalized = claims[0].Value.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) {
                return SegmentResult.Fail("EmptyClaim", [alias]);
            }

            if (normalized.Contains(':', StringComparison.Ordinal)) {
                return SegmentResult.Fail("ColonClaim", [alias]);
            }

            found.Add((alias, normalized));
        }

        if (found.Count == 0) {
            return SegmentResult.Fail("MissingClaim", aliases);
        }

        string first = found[0].Value;
        if (found.Any(item => !string.Equals(item.Value, first, StringComparison.Ordinal))) {
            return SegmentResult.Fail("ConflictingAliases", found.Select(item => item.Alias).ToArray());
        }

        return SegmentResult.Success(first);
    }

    private readonly record struct SegmentResult(string? Value, string? Reason, IReadOnlyList<string> InvolvedAliases) {
        public static SegmentResult Success(string value) => new(value, null, []);
        public static SegmentResult Fail(string reason, IReadOnlyList<string> aliases) => new(null, reason, aliases);
    }
}

internal sealed record FrontComposerClaimExtractionResult(
    bool Succeeded,
    string? TenantId,
    string? UserId,
    string Reason,
    IReadOnlyList<string> TenantAliases,
    IReadOnlyList<string> UserAliases) {
    public static FrontComposerClaimExtractionResult Success(string tenantId, string userId)
        => new(true, tenantId, userId, string.Empty, [], []);

    public static FrontComposerClaimExtractionResult Fail(
        string reason,
        IReadOnlyList<string> tenantAliases,
        IReadOnlyList<string> userAliases)
        => new(false, null, null, reason, tenantAliases, userAliases);
}
