using System.Security.Cryptography;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

/// <summary>
/// Default tenant-context accessor backed by <see cref="IUserContextAccessor"/>.
/// Accepts tenant identifiers up to 64 UTF-16 code units and user identifiers up to 1024.
/// </summary>
public sealed class FrontComposerTenantContextAccessor : IFrontComposerTenantContextAccessor {
    // Match the EventStore command and query request limit for tenant identifiers.
    private const int MaxTenantLength = 64;
    private const int MaxUserLength = 1024;
    private static readonly TimeSpan ProjectionNudgeWarningInterval = TimeSpan.FromSeconds(30);

    private readonly IUserContextAccessor _userContextAccessor;
    private readonly Func<FcShellOptions> _getOptions;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Func<bool> _shouldLogProjectionNudgeFailure;
    private readonly Lock _projectionNudgeWarningLock = new();
    private long? _lastProjectionNudgeWarning;

    /// <summary>Initializes the accessor using Shell options.</summary>
    public FrontComposerTenantContextAccessor(
        IUserContextAccessor userContextAccessor,
        IOptions<FcShellOptions> options,
        ILogger<FrontComposerTenantContextAccessor> logger)
        : this(userContextAccessor, options, logger, optionsMonitor: null) {
    }

    /// <summary>Initializes the accessor using the current monitored Shell options when available.</summary>
    public FrontComposerTenantContextAccessor(
        IUserContextAccessor userContextAccessor,
        IOptions<FcShellOptions> options,
        ILogger<FrontComposerTenantContextAccessor> logger,
        IOptionsMonitor<FcShellOptions>? optionsMonitor)
        : this(userContextAccessor, () => optionsMonitor?.CurrentValue ?? options.Value, logger) {
    }

    /// <summary>Adapts direct EventStore client construction to the same tenant resolver.</summary>
    internal FrontComposerTenantContextAccessor(
        IUserContextAccessor userContextAccessor,
        Func<FcShellOptions> getOptions,
        ILogger logger,
        TimeProvider? timeProvider = null) {
        _userContextAccessor = userContextAccessor;
        _getOptions = getOptions;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _shouldLogProjectionNudgeFailure = ShouldLogProjectionNudgeFailure;
    }
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
        => Resolve(_userContextAccessor, _getOptions(), _logger, requestedTenant, operationKind);

    /// <inheritdoc />
    public TenantContextResult Revalidate(TenantContextSnapshot snapshot, string operationKind = "tenant-scoped") {
        ArgumentNullException.ThrowIfNull(snapshot);
        Func<bool>? shouldLogFailure = operationKind == "projection-nudge"
            ? _shouldLogProjectionNudgeFailure
            : null;
        TenantContextResult current = ResolveCore(
            _userContextAccessor, _getOptions(), _logger, null, operationKind, shouldLogFailure);
        if (!current.Succeeded || current.Context is null) {
            return current;
        }

        if (string.Equals(current.Context.TenantId, snapshot.TenantId, StringComparison.Ordinal)
            && string.Equals(current.Context.UserId, snapshot.UserId, StringComparison.Ordinal)) {
            return current;
        }

        return Block(
            _logger,
            TenantContextFailureCategory.StaleTenantContext,
            operationKind,
            requestedTenantPresent: true,
            authenticatedTenantPresent: true,
            userPresent: true,
            shouldLogFailure);
    }

    internal static TenantContextResult Resolve(
        IUserContextAccessor userContextAccessor,
        FcShellOptions options,
        ILogger logger,
        string? requestedTenant = null,
        string operationKind = "tenant-scoped")
        => ResolveCore(userContextAccessor, options, logger, requestedTenant, operationKind, null);

    private static TenantContextResult ResolveCore(
        IUserContextAccessor userContextAccessor,
        FcShellOptions options,
        ILogger logger,
        string? requestedTenant,
        string operationKind,
        Func<bool>? shouldLogFailure) {
        ArgumentNullException.ThrowIfNull(userContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        string? tenant = null;
        string? user = null;
        try {
            tenant = userContextAccessor.TenantId;
            user = userContextAccessor.UserId;
        }
        catch (Exception exception) when (exception is not OperationCanceledException && !ExceptionGuard.IsFatal(exception)) {
            // Leave the unavailable identity segment unset. The missing-context path logs only presence flags.
        }
        bool requestedPresent = HasValue(requestedTenant, MaxTenantLength);
        bool tenantPresent = HasValue(tenant, MaxTenantLength);
        bool userPresent = HasValue(user, MaxUserLength);
        if (!tenantPresent) {
            return Failure(TenantContextFailureCategory.TenantMissing, requestedPresent, false, userPresent);
        }

        if (!userPresent) {
            return Failure(TenantContextFailureCategory.UserMissing, requestedPresent, true, false);
        }

        // P9 — flags reflect *valid* presence: a malformed segment is "present but unusable",
        // so the diagnostic flag is set to false to surface which segment was rejected.
        bool tenantValid = IsValidSegment(tenant!, MaxTenantLength);
        bool userValid = IsValidSegment(user!, MaxUserLength);
        bool requestedValid = !requestedPresent || IsValidSegment(requestedTenant!, MaxTenantLength);
        if (!tenantValid || !userValid || !requestedValid) {
            return Failure(
                TenantContextFailureCategory.MalformedSegment,
                requestedPresent && requestedValid,
                tenantValid,
                userValid);
        }

        if (requestedPresent
            && !string.Equals(requestedTenant, tenant, StringComparison.Ordinal)) {
            return Failure(TenantContextFailureCategory.TenantMismatch, true, true, true);
        }

        if (!options.AllowDemoTenantContext
            && (IsSynthetic(tenant!) || IsSynthetic(user!))) {
            // P9 — set the affected flag to false so operators can tell which side was synthetic.
            return Failure(
                TenantContextFailureCategory.SyntheticTenantRejected,
                requestedPresent && !IsSynthetic(requestedTenant!),
                !IsSynthetic(tenant!),
                !IsSynthetic(user!));
        }

        return TenantContextResult.Success(new TenantContextSnapshot(
            tenant!,
            user!,
            IsAuthenticated: true,
            CorrelationId: NewCorrelationId()));

        TenantContextResult Failure(TenantContextFailureCategory category, bool requested, bool authenticated, bool hasUser)
            => Block(logger, category, operationKind, requested, authenticated, hasUser, shouldLogFailure);
    }

    private static bool IsValidSegment(string value, int maxLength) {
        if (value.Length > maxLength) {
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

    private static bool HasValue(string? value, int maxLength)
        // An oversized value is malformed, even when it consists entirely of whitespace.
        => value is not null && (value.Length > maxLength || !string.IsNullOrWhiteSpace(value));

    private static TenantContextResult Block(
        ILogger logger,
        TenantContextFailureCategory failureCategory,
        string operationKind,
        bool requestedTenantPresent,
        bool authenticatedTenantPresent,
        bool userPresent,
        Func<bool>? shouldLogFailure) {
        string correlationId = NewCorrelationId();
        if (shouldLogFailure?.Invoke() != false) {
            FrontComposerLog.TenantContextBlocked(
                logger,
                DiagnosticId(failureCategory),
                operationKind,
                failureCategory.ToString(),
                requestedTenantPresent,
                authenticatedTenantPresent,
                userPresent,
                correlationId);
        }

        return TenantContextResult.Failure(failureCategory, correlationId);
    }

    private bool ShouldLogProjectionNudgeFailure() {
        lock (_projectionNudgeWarningLock) {
            long now = _timeProvider.GetTimestamp();
            if (_lastProjectionNudgeWarning is long previous
                && _timeProvider.GetElapsedTime(previous, now) < ProjectionNudgeWarningInterval) {
                return false;
            }

            _lastProjectionNudgeWarning = now;
            return true;
        }
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
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
