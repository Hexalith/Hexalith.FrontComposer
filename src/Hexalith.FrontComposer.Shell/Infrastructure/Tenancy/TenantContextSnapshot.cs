namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Immutable validated tenant/user context captured before a tenant-scoped operation performs
/// side effects.
/// </summary>
/// <param name="TenantId">Authenticated tenant identifier, preserved verbatim.</param>
/// <param name="UserId">Authenticated user identifier, preserved verbatim.</param>
/// <param name="IsAuthenticated">Whether both tenant and user context were validated.</param>
/// <param name="CorrelationId">Non-PII handle used to correlate sanitized diagnostics.</param>
public sealed record TenantContextSnapshot {
    /// <summary>Initializes a new instance of the <see cref="TenantContextSnapshot"/> record.</summary>
    public TenantContextSnapshot(string TenantId, string UserId, bool IsAuthenticated, string CorrelationId) {
        // P7 — invariant: a snapshot represents a validated context. Hand-crafted snapshots with
        // empty/whitespace fields or with IsAuthenticated=false would be silently honored by
        // downstream EventStore command/query/subscription paths today. Fail closed at the type
        // boundary so misconstructed snapshots cannot reach side-effect code.
        if (string.IsNullOrWhiteSpace(TenantId)) {
            throw new ArgumentException("TenantId must not be empty.", nameof(TenantId));
        }

        if (string.IsNullOrWhiteSpace(UserId)) {
            throw new ArgumentException("UserId must not be empty.", nameof(UserId));
        }

        if (!IsAuthenticated) {
            throw new ArgumentException("Snapshots must represent authenticated contexts.", nameof(IsAuthenticated));
        }

        if (string.IsNullOrWhiteSpace(CorrelationId)) {
            throw new ArgumentException("CorrelationId must not be empty.", nameof(CorrelationId));
        }

        this.TenantId = TenantId;
        this.UserId = UserId;
        this.IsAuthenticated = IsAuthenticated;
        this.CorrelationId = CorrelationId;
    }

    /// <summary>Authenticated tenant identifier, preserved verbatim.</summary>
    public string TenantId { get; init; }

    /// <summary>Authenticated user identifier, preserved verbatim.</summary>
    public string UserId { get; init; }

    /// <summary>Whether both tenant and user context were validated. Always <see langword="true"/> for valid snapshots.</summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>Non-PII handle used to correlate sanitized diagnostics.</summary>
    public string CorrelationId { get; init; }
}
