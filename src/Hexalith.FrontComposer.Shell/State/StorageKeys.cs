using Hexalith.FrontComposer.Shell.Services;

namespace Hexalith.FrontComposer.Shell.State;

/// <summary>
/// Builds storage keys for persisted Fluxor state features.
/// Keys follow the pattern documented in <see cref="Contracts.Storage.IStorageService"/>.
/// </summary>
/// <remarks>
/// Story 3-1 D8 / ADR-029 removed the <c>DefaultTenantId</c> / <c>DefaultUserId</c>
/// placeholder constants. Callers MUST resolve non-null, non-whitespace tenant and user
/// identifiers from <c>IUserContextAccessor</c> and short-circuit (logging <c>HFC2105</c>)
/// when the accessor returns null / empty / whitespace. Passing a static "default"/"anonymous"
/// string is a cross-tenant bleed risk and is forbidden framework-wide.
/// </remarks>
public static class StorageKeys {
    /// <summary>3-segment key for simple features (theme, density, nav).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="feature">The feature name.</param>
    /// <returns>A storage key in the format <c>{tenant}:{user}:{feature}</c>, where tenant and user are canonicalized key segments.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/>, <paramref name="userId"/>, or <paramref name="feature"/> is null, empty, or whitespace; when <paramref name="feature"/> contains a separator; or when <paramref name="tenantId"/> or <paramref name="userId"/> is not valid Unicode (e.g. contains an unpaired surrogate) and so fails NFC normalization during canonicalization.</exception>
    public static string BuildKey(string tenantId, string userId, string feature) {
        (string tenant, string user) = CanonicalizeIdentitySegments(tenantId, userId);
        GuardFeatureSegment(feature);
        return $"{tenant}:{user}:{feature}";
    }

    /// <summary>4-segment key for discriminated features (DataGrid, ETagCache). Matches IStorageService doc pattern.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="feature">The feature name.</param>
    /// <param name="discriminator">The discriminator (e.g., projection type).</param>
    /// <returns>A storage key in the format <c>{tenant}:{user}:{feature}:{discriminator}</c>, where tenant and user are canonicalized key segments.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/>, <paramref name="userId"/>, <paramref name="feature"/>, or <paramref name="discriminator"/> is null, empty, or whitespace; when <paramref name="feature"/> contains a separator; or when <paramref name="tenantId"/> or <paramref name="userId"/> is not valid Unicode (e.g. contains an unpaired surrogate) and so fails NFC normalization during canonicalization.</exception>
    public static string BuildKey(string tenantId, string userId, string feature, string discriminator) {
        (string tenant, string user) = CanonicalizeIdentitySegments(tenantId, userId);
        GuardFeatureSegment(feature);
        ArgumentException.ThrowIfNullOrWhiteSpace(discriminator);
        return $"{tenant}:{user}:{feature}:{discriminator}";
    }

    private static void GuardFeatureSegment(string feature) {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);
        if (feature.Contains(':', StringComparison.Ordinal)) {
            throw new ArgumentException("Feature segment must not contain ':'.", nameof(feature));
        }
    }

    private static (string TenantId, string UserId) CanonicalizeIdentitySegments(string tenantId, string userId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return (
            FrontComposerStorageKey.CanonicalizeTenant(tenantId),
            FrontComposerStorageKey.CanonicalizeUser(userId));
    }
}
