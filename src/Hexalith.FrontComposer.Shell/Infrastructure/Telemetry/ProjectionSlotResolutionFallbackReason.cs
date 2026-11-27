namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>Support-safe reasons why a Level 3 slot lookup fell back to generated rendering.</summary>
[Flags]
internal enum ProjectionSlotResolutionFallbackReason
{
    /// <summary>The lookup found no eligible registration.</summary>
    NoMatch = 1,

    /// <summary>Conflicting registrations suppressed the matching slot.</summary>
    Ambiguous = 2,

    /// <summary>An interface or abstract projection type had no matching registration.</summary>
    UnsupportedProjectionType = 4,
}
