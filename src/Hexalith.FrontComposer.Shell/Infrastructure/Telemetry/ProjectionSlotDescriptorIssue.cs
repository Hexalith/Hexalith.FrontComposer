namespace Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

/// <summary>Fixed support-safe rejection categories for Level 3 slot descriptors.</summary>
internal enum ProjectionSlotDescriptorIssue
{
    /// <summary>Use the digested component compatibility reason.</summary>
    None,

    /// <summary>The projection type cannot be used as a closed generic argument.</summary>
    InvalidProjectionType,

    /// <summary>The descriptor supplies an undefined projection role.</summary>
    InvalidRole,
}
