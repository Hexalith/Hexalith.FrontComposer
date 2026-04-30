namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Single source of truth for the Level 2 typed projection-template contract version
/// (Story 6-2 T7 / D6 / AC5). The constant gates compile-time validation between an
/// adopter's <c>[ProjectionTemplate(ExpectedContractVersion = ...)]</c> marker, the
/// generated template manifest schema, and the Shell-side runtime registry.
/// </summary>
/// <remarks>
/// <para>
/// Versioning policy:
/// <list type="bullet">
///   <item><description><b>Major</b> bumps (X+1.0.0) signal an incompatible context/descriptor/manifest
///     schema change — adopters must rebuild and migrate; runtime selection is blocked.</description></item>
///   <item><description><b>Minor</b> bumps (x.Y+1.0) signal additive, source-compatible changes —
///     SourceTools emits a <c>HFC1036</c> warning encouraging an upgrade but selection still
///     succeeds.</description></item>
///   <item><description><b>Build</b> bumps (x.y.Z+1) are non-functional and never emit drift
///     warnings.</description></item>
/// </list>
/// </para>
/// <para>
/// The version is intentionally exposed as integer triplet constants rather than a single
/// string so the SourceTools generator can compare numerically without parsing literals.
/// </para>
/// </remarks>
public static class ProjectionTemplateContractVersion {
    /// <summary>The Level 2 contract major version. Bumped only for incompatible changes.</summary>
    public const int Major = 1;

    /// <summary>The Level 2 contract minor version. Bumped for additive, source-compatible changes.</summary>
    public const int Minor = 0;

    /// <summary>The Level 2 contract build version. Bumped for non-functional revisions.</summary>
    public const int Build = 0;

    /// <summary>The packed contract version identifier (Major*1_000_000 + Minor*1_000 + Build).</summary>
    public const int Current = (Major * 1_000_000) + (Minor * 1_000) + Build;
}
