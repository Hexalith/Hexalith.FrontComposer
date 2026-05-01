namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Version marker for the Level 4 full projection-view replacement contract.
/// </summary>
public static class ProjectionViewOverrideContractVersion {
    /// <summary>Current major contract version.</summary>
    public const int Major = 1;

    /// <summary>Current minor contract version.</summary>
    public const int Minor = 0;

    /// <summary>Current build contract version.</summary>
    public const int Build = 0;

    /// <summary>
    /// Packed contract version. The major value drives compatibility; minor/build drift remains
    /// diagnosable without changing selection semantics.
    /// </summary>
    public const int Current = (Major * 1_000_000) + (Minor * 1_000) + Build;
}
