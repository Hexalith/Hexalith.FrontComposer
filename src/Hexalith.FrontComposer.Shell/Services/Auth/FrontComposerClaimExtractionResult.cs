namespace Hexalith.FrontComposer.Shell.Services.Auth;

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
