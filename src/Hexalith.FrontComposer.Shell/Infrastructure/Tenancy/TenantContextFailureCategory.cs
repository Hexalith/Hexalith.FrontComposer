namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Sanitized tenant-context failure categories used for logs, telemetry, and exceptions.
/// </summary>
public enum TenantContextFailureCategory {
    /// <summary>No validation failure occurred.</summary>
    None = 0,

    /// <summary>The authenticated tenant was missing.</summary>
    TenantMissing,

    /// <summary>The authenticated user was missing.</summary>
    UserMissing,

    /// <summary>A tenant, user, projection, or group segment was malformed.</summary>
    MalformedSegment,

    /// <summary>A requested tenant differed from the authenticated tenant.</summary>
    TenantMismatch,

    /// <summary>A demo/synthetic tenant or user was used without explicit demo/test opt-in.</summary>
    SyntheticTenantRejected,

    /// <summary>The authenticated context changed after an operation snapshot was accepted.</summary>
    StaleTenantContext,
}
