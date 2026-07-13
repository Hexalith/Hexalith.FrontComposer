using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Default <see cref="IStorageScopeResolver"/> — the single Scoped storage scope resolver
/// (Story 11.15, M19 cluster 4). The body is adopted from the former <c>CommandPaletteEffects</c>
/// helper (the strongest of the six copies): the <see cref="IUserContextAccessor"/> property getters
/// are read inside a non-fatal try/catch so a throwing accessor (disposed claims principal / adopter
/// JWT-parse fault) fails closed with <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/>
/// (<c>Reason=AccessorThrew</c>) instead of bubbling an unhandled exception into Fluxor's effect
/// pipeline — fixing the latent throw-propagation defect the other five copies shared.
/// </summary>
/// <remarks>
/// Fail-closed contract (unchanged from the six originals): missing/blank/whitespace tenant OR user →
/// both <c>out</c> parameters set to <see cref="string.Empty"/>, returns <see langword="false"/>, logs
/// HFC2105 at <see cref="LogLevel.Information"/> with the direction placeholder; on success assigns the
/// RAW (un-escaped) identities and returns <see langword="true"/> so identity canonicalization stays
/// centralized in <c>StorageKeys</c> / <c>FrontComposerStorageKey</c>. Tenant/user values are NEVER
/// logged (PII). Depends only on <see cref="IUserContextAccessor"/> and <see cref="ILogger"/>
/// (dependency-inward; no <c>State</c> / <c>Components</c> edges), so it stays a Scoped leaf service.
/// </remarks>
internal sealed class StorageScopeResolver : IStorageScopeResolver {
    private readonly IUserContextAccessor? _accessor;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageScopeResolver"/> class.
    /// </summary>
    /// <param name="accessor">
    /// The tenant/user context accessor. Tolerated <see langword="null"/> so the resolver survives
    /// unit-test fixtures that bypass accessor registration (it then fails closed).
    /// </param>
    /// <param name="logger">Logger for the HFC2105 fail-closed diagnostic.</param>
    public StorageScopeResolver(IUserContextAccessor? accessor, ILogger logger) {
        ArgumentNullException.ThrowIfNull(logger);
        _accessor = accessor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool TryResolveScope(out string tenantId, out string userId, string feature, string direction) {
        // The accessor property getters may throw (claims principal disposed, JWT parse error in an
        // adopter implementation). Wrap the reads so the fail-closed branch fires consistently with
        // HFC2105 instead of bubbling an unhandled effect exception into Fluxor's pipeline.
        string? rawTenant;
        string? rawUser;
        try {
            rawTenant = _accessor?.TenantId;
            rawUser = _accessor?.UserId;
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            _logger.LogInformation(
                "{DiagnosticId}: {Feature} {Direction} skipped — IUserContextAccessor threw on TenantId/UserId access. Reason=AccessorThrew. FailureCategory={FailureCategory}.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                feature,
                direction,
                ex.GetType().Name);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            _logger.LogInformation(
                "{DiagnosticId}: {Feature} {Direction} skipped — null/empty/whitespace tenant or user context.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                feature,
                direction);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        tenantId = rawTenant;
        userId = rawUser;
        return true;
    }
}
