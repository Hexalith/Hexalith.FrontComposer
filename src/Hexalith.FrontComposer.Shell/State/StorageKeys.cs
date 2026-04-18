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
    public static string BuildKey(string tenantId, string userId, string feature)
        => $"{tenantId}:{userId}:{feature}";

    /// <summary>4-segment key for discriminated features (DataGrid, ETagCache). Matches IStorageService doc pattern.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="feature">The feature name.</param>
    /// <param name="discriminator">The discriminator (e.g., projection type).</param>
    /// <returns>A storage key in the format <c>{tenantId}:{userId}:{feature}:{discriminator}</c>.</returns>
    public static string BuildKey(string tenantId, string userId, string feature, string discriminator)
        => $"{tenantId}:{userId}:{feature}:{discriminator}";
}
