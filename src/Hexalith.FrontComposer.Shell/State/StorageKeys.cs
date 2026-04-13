namespace Hexalith.FrontComposer.Shell.State;

/// <summary>
/// Builds storage keys for persisted Fluxor state features.
/// Keys follow the pattern documented in <see cref="Contracts.Storage.IStorageService"/>.
/// </summary>
public static class StorageKeys
{
    // TODO: Replace with ITenantContext/IUserContext when authentication is implemented (Epic 7)

    /// <summary>Placeholder tenant identifier used before multi-tenancy is enabled.</summary>
    public const string DefaultTenantId = "default";

    /// <summary>Placeholder user identifier used before authentication is enabled.</summary>
    public const string DefaultUserId = "anonymous";

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
