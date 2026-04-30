namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Contract version for Level 3 field-slot overrides.
/// </summary>
/// <remarks>
/// The packed encoding is <c>major * 1,000,000 + minor * 1,000 + build</c>, which limits
/// minor and build to 0–999. <see cref="Major"/>, <see cref="Minor"/>, and <see cref="Build"/>
/// are the single source of truth; <see cref="Current"/> is derived from them so they cannot
/// drift independently. Bump <c>Major</c> only for breaking shape changes; bump
/// <c>Minor</c>/<c>Build</c> for additive or fix-only revisions, keeping each strictly
/// below 1000 so the packed encoding stays unambiguous.
/// </remarks>
public static class ProjectionSlotContractVersion {
    /// <summary>Current major version. Drives HFC1041 mismatch detection.</summary>
    public const int Major = 1;

    /// <summary>Current minor version. Must remain in <c>[0, 1000)</c>.</summary>
    public const int Minor = 0;

    /// <summary>Current build version. Must remain in <c>[0, 1000)</c>.</summary>
    public const int Build = 0;

    /// <summary>Current packed version: <c>Major * 1_000_000 + Minor * 1_000 + Build</c>.</summary>
    public const int Current = (Major * 1_000_000) + (Minor * 1_000) + Build;
}
