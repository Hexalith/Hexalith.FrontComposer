namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Sanitized exception thrown when a tenant-scoped operation is blocked before side effects.
/// </summary>
public sealed class TenantContextException : InvalidOperationException {
    /// <summary>Initializes a new instance of the <see cref="TenantContextException"/> class.</summary>
    public TenantContextException(TenantContextFailureCategory failureCategory, string correlationId)
        : base($"Tenant context validation failed. FailureCategory={failureCategory} CorrelationId={correlationId}") {
        FailureCategory = failureCategory;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the sanitized failure category.</summary>
    public TenantContextFailureCategory FailureCategory { get; }

    /// <summary>Gets the non-PII diagnostic correlation handle.</summary>
    public string CorrelationId { get; }
}
