namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Result returned by the canonical tenant-context accessor.
/// </summary>
public sealed record TenantContextResult(
    bool Succeeded,
    TenantContextSnapshot? Context,
    TenantContextFailureCategory FailureCategory,
    string CorrelationId) {
    /// <summary>Creates a successful context result.</summary>
    public static TenantContextResult Success(TenantContextSnapshot context) {
        ArgumentNullException.ThrowIfNull(context);
        return new(true, context, TenantContextFailureCategory.None, context.CorrelationId);
    }

    /// <summary>Creates a sanitized failure result.</summary>
    public static TenantContextResult Failure(TenantContextFailureCategory failureCategory, string correlationId)
        => new(false, null, failureCategory, correlationId);

    /// <summary>Returns the validated snapshot or throws a sanitized exception.</summary>
    public TenantContextSnapshot EnsureSuccess() {
        if (Succeeded && Context is not null) {
            return Context;
        }

        throw new TenantContextException(FailureCategory, CorrelationId);
    }
}
