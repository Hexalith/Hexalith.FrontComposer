using System.Security.Cryptography;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Default tenant-context accessor backed by <see cref="IUserContextAccessor"/>.
/// </summary>
public sealed class FrontComposerTenantContextAccessor(
    IUserContextAccessor userContextAccessor,
    IOptions<FcShellOptions> options,
    ILogger<FrontComposerTenantContextAccessor> logger) : IFrontComposerTenantContextAccessor {
    private static readonly string[] SyntheticIdentifiers = [
        "default",
        "anonymous",
        "demo",
        "test",
        "counter",
        "counter-demo",
        "synthetic",
    ];

    /// <inheritdoc />
    public TenantContextResult TryGetContext(string? requestedTenant = null, string operationKind = "tenant-scoped")
        => Resolve(userContextAccessor, options.Value, logger, requestedTenant, operationKind);

    /// <inheritdoc />
    public TenantContextResult Revalidate(TenantContextSnapshot snapshot, string operationKind = "tenant-scoped") {
        ArgumentNullException.ThrowIfNull(snapshot);
        TenantContextResult current = TryGetContext(snapshot.TenantId, operationKind);
        if (!current.Succeeded || current.Context is null) {
            return current;
        }

        if (string.Equals(current.Context.TenantId, snapshot.TenantId, StringComparison.Ordinal)
            && string.Equals(current.Context.UserId, snapshot.UserId, StringComparison.Ordinal)) {
            return current;
        }

        return Block(
            logger,
            TenantContextFailureCategory.StaleTenantContext,
            operationKind,
            requestedTenantPresent: true,
            authenticatedTenantPresent: true,
            userPresent: true);
    }

    internal static TenantContextResult Resolve(
        IUserContextAccessor userContextAccessor,
        FcShellOptions options,
        ILogger logger,
        string? requestedTenant = null,
        string operationKind = "tenant-scoped") {
        ArgumentNullException.ThrowIfNull(userContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        string? tenant = userContextAccessor.TenantId;
        string? user = userContextAccessor.UserId;
        bool requestedPresent = HasValue(requestedTenant);
        bool tenantPresent = HasValue(tenant);
        bool userPresent = HasValue(user);
        if (!tenantPresent) {
            return Block(logger, TenantContextFailureCategory.TenantMissing, operationKind, requestedPresent, false, userPresent);
        }

        if (!userPresent) {
            return Block(logger, TenantContextFailureCategory.UserMissing, operationKind, requestedPresent, true, false);
        }

        // P9 — flags reflect *valid* presence: a malformed segment is "present but unusable",
        // so the diagnostic flag is set to false to surface which segment was rejected.
        bool tenantValid = IsValidSegment(tenant!);
        bool userValid = IsValidSegment(user!);
        bool requestedValid = !requestedPresent || IsValidSegment(requestedTenant!);
        if (!tenantValid || !userValid || !requestedValid) {
            return Block(
                logger,
                TenantContextFailureCategory.MalformedSegment,
                operationKind,
                requestedPresent && requestedValid,
                tenantValid,
                userValid);
        }

        if (requestedPresent
            && !string.Equals(requestedTenant, tenant, StringComparison.Ordinal)) {
            return Block(logger, TenantContextFailureCategory.TenantMismatch, operationKind, true, true, true);
        }

        if (!options.AllowDemoTenantContext
            && (IsSynthetic(tenant!) || IsSynthetic(user!))) {
            // P9 — set the affected flag to false so operators can tell which side was synthetic.
            return Block(
                logger,
                TenantContextFailureCategory.SyntheticTenantRejected,
                operationKind,
                requestedPresent && !IsSynthetic(requestedTenant!),
                !IsSynthetic(tenant!),
                !IsSynthetic(user!));
        }

        return TenantContextResult.Success(new TenantContextSnapshot(
            tenant!,
            user!,
            IsAuthenticated: true,
            CorrelationId: NewCorrelationId()));
    }

    internal static string RequireValidSegment(string? value, string paramName) {
        if (string.IsNullOrWhiteSpace(value) || !IsValidSegment(value)) {
            throw new ArgumentException("Tenant-scoped routing values must not be empty and must not contain ':' or control characters.", paramName);
        }

        return value!;
    }

    private static bool IsValidSegment(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        for (int i = 0; i < value.Length; i++) {
            char ch = value[i];
            if (ch == ':' || char.IsControl(ch)) {
                return false;
            }
        }

        return true;
    }

    private static bool IsSynthetic(string value)
        => SyntheticIdentifiers.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool HasValue(string? value)
        => !string.IsNullOrWhiteSpace(value);

    private static TenantContextResult Block(
        ILogger logger,
        TenantContextFailureCategory failureCategory,
        string operationKind,
        bool requestedTenantPresent,
        bool authenticatedTenantPresent,
        bool userPresent) {
        string correlationId = NewCorrelationId();
        FrontComposerLog.TenantContextBlocked(
            logger,
            DiagnosticId(failureCategory),
            operationKind,
            failureCategory.ToString(),
            requestedTenantPresent,
            authenticatedTenantPresent,
            userPresent,
            correlationId);
        return TenantContextResult.Failure(failureCategory, correlationId);
    }

    private static string DiagnosticId(TenantContextFailureCategory failureCategory)
        => failureCategory switch {
            TenantContextFailureCategory.TenantMissing or TenantContextFailureCategory.UserMissing => FcDiagnosticIds.HFC2015_TenantContextMissing,
            TenantContextFailureCategory.MalformedSegment => FcDiagnosticIds.HFC2016_TenantContextMalformedSegment,
            TenantContextFailureCategory.TenantMismatch => FcDiagnosticIds.HFC2017_TenantContextMismatch,
            TenantContextFailureCategory.SyntheticTenantRejected => FcDiagnosticIds.HFC2018_DemoTenantContextRejected,
            TenantContextFailureCategory.StaleTenantContext => FcDiagnosticIds.HFC2019_StaleTenantContext,
            _ => FcDiagnosticIds.HFC2015_TenantContextMissing,
        };

    private static string NewCorrelationId() {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
