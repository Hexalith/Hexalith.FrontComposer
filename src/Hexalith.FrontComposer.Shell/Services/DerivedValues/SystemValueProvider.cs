using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #1 in the chain (Story 2-2 Decision D24): supplies infrastructure-owned values
/// for well-known property names. Reads <c>TenantId</c> / <c>UserId</c> from <see cref="IUserContextAccessor"/>
/// when available; otherwise falls through so subsequent providers can attempt resolution.
/// </summary>
public sealed class SystemValueProvider : IDerivedValueProvider {
    private readonly IUserContextAccessor? _userContext;

    public SystemValueProvider(IUserContextAccessor? userContext = null) {
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        DerivedValueResult result = propertyName switch {
            "MessageId" => new DerivedValueResult(true, Guid.NewGuid().ToString("N")),
            "CommandId" => new DerivedValueResult(true, Guid.NewGuid().ToString("N")),
            "CorrelationId" => new DerivedValueResult(true, Guid.NewGuid().ToString("N")),
            "Timestamp" or "CreatedAt" or "ModifiedAt" => new DerivedValueResult(true, DateTimeOffset.UtcNow),
            "TenantId" => FromContext(_userContext?.TenantId),
            "UserId" => FromContext(_userContext?.UserId),
            _ => DerivedValueResult.None,
        };

        return Task.FromResult(result);
    }

    private static DerivedValueResult FromContext(string? value)
        => string.IsNullOrWhiteSpace(value) ? DerivedValueResult.None : new DerivedValueResult(true, value);
}
