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
    /// <returns>A storage key in the format <c>{tenantId}:{userId}:{feature}</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="userId"/> contains a colon — prevents cross-tenant collision on keys that re-use <c>:</c> as segment separator.</exception>
    public static string BuildKey(string tenantId, string userId, string feature)
    {
        GuardIdentitySegments(tenantId, userId);
        return $"{tenantId}:{userId}:{feature}";
    }

    /// <summary>4-segment key for discriminated features (DataGrid, ETagCache). Matches IStorageService doc pattern.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="feature">The feature name.</param>
    /// <param name="discriminator">The discriminator (e.g., projection type).</param>
    /// <returns>A storage key in the format <c>{tenantId}:{userId}:{feature}:{discriminator}</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="userId"/> contains a colon.</exception>
    public static string BuildKey(string tenantId, string userId, string feature, string discriminator)
    {
        GuardIdentitySegments(tenantId, userId);
        return $"{tenantId}:{userId}:{feature}:{discriminator}";
    }

    private static void GuardIdentitySegments(string tenantId, string userId)
    {
        // Two tuples ("a:b","c") and ("a","b:c") would otherwise yield identical prefixes; enumerate
        // / prune paths could then cross-read or cross-delete a foreign tenant's keys. Reject colons.
        if (tenantId is not null && tenantId.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("tenantId must not contain ':'.", nameof(tenantId));
        }

        if (userId is not null && userId.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("userId must not contain ':'.", nameof(userId));
        }
    }
}
