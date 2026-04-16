using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #1 in the chain (Story 2-2 Decision D24): supplies infrastructure-owned values
/// for well-known property names. Reads <c>TenantId</c> / <c>UserId</c> from <see cref="IUserContextAccessor"/>
/// when available; otherwise publishes a <c>D31</c> dev diagnostic and declines so subsequent providers
/// can attempt resolution.
/// </summary>
/// <remarks>
/// Values are returned in the declared CLR type of the command property (Story 2-2 Decision D15 — Group D
/// code review): GUID-typed properties receive <see cref="Guid"/> values, <see cref="DateTime"/>-typed
/// properties receive <see cref="DateTime"/> values, and so on. String-typed properties continue to
/// receive the hex ("N") / invariant ISO forms for serialization stability.
/// </remarks>
public sealed class SystemValueProvider : IDerivedValueProvider {
    private static readonly ConcurrentDictionary<(Type, string), Type?> PropertyTypeCache = new();

    private readonly IUserContextAccessor _userContext;
    private readonly IUlidFactory _ulidFactory;
    private readonly IDiagnosticSink? _diagnostics;

    public SystemValueProvider(
        IUserContextAccessor userContext,
        IUlidFactory ulidFactory,
        IDiagnosticSink? diagnostics = null) {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _ulidFactory = ulidFactory ?? throw new ArgumentNullException(nameof(ulidFactory));
        _diagnostics = diagnostics;
    }

    /// <inheritdoc/>
    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ct.ThrowIfCancellationRequested();

        Type? propertyType = ResolvePropertyType(commandType, propertyName);

        DerivedValueResult result = propertyName switch {
            "MessageId" => new DerivedValueResult(true, NewUlid(propertyType)),
            "CommandId" or "CorrelationId" => new DerivedValueResult(true, NewId(propertyType)),
            "Timestamp" or "CreatedAt" or "ModifiedAt" => new DerivedValueResult(true, NowFor(propertyType)),
            "TenantId" => FromContext(_userContext.TenantId, "TenantId"),
            "UserId" => FromContext(_userContext.UserId, "UserId"),
            _ => DerivedValueResult.None,
        };

        return Task.FromResult(result);
    }

    /// <summary>Returns the CLR type of a command property, cached per <c>(Type, name)</c>.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is delegated to the renderer.")]
    [UnconditionalSuppressMessage("Trimming", "IL2080:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is delegated to the renderer.")]
    private static Type? ResolvePropertyType(Type commandType, string propertyName)
        => PropertyTypeCache.GetOrAdd(
            (commandType, propertyName),
            static k => LookupPropertyType(k.Item1, k.Item2));

    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "commandType flows from generated code; trim preservation is delegated to the renderer.")]
    private static Type? LookupPropertyType(Type commandType, string propertyName) {
        try {
            return commandType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.PropertyType;
        }
        catch (AmbiguousMatchException) {
            return null;
        }
    }

    /// <summary>
    /// Generates a fresh identifier in the declared property type — <see cref="Guid"/> for
    /// <c>Guid</c> properties, hex ("N") <see cref="string"/> for string-typed properties (default).
    /// </summary>
    private static object NewId(Type? propertyType) {
        Type effective = Nullable.GetUnderlyingType(propertyType ?? typeof(string)) ?? propertyType ?? typeof(string);
        if (effective == typeof(Guid)) {
            return Guid.NewGuid();
        }

        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Generates a fresh ULID MessageId via <see cref="IUlidFactory"/> (Story 2-3 Decision D2 — MessageId
    /// is ULID, not Guid). For Guid-typed properties the provider still returns a Guid (dev convenience);
    /// a subsequent story will normalise MessageId properties to <c>string</c>.
    /// </summary>
    private object NewUlid(Type? propertyType) {
        Type effective = Nullable.GetUnderlyingType(propertyType ?? typeof(string)) ?? propertyType ?? typeof(string);
        if (effective == typeof(Guid)) {
            return Guid.NewGuid();
        }

        return _ulidFactory.NewUlid();
    }

    /// <summary>
    /// Returns the current timestamp coerced to the declared property type.
    /// <see cref="DateTime"/> → <see cref="DateTime.UtcNow"/>; otherwise <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    private static object NowFor(Type? propertyType) {
        Type effective = Nullable.GetUnderlyingType(propertyType ?? typeof(DateTimeOffset)) ?? propertyType ?? typeof(DateTimeOffset);
        if (effective == typeof(DateTime)) {
            return DateTime.UtcNow;
        }

        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Resolves the user-context segment and, when absent, publishes the D31 fail-closed diagnostic
    /// (parity with <c>LastUsedValueProvider</c>). Returns <see cref="DerivedValueResult.None"/> so
    /// the provider chain continues.
    /// </summary>
    private DerivedValueResult FromContext(string? value, string segmentName) {
        if (!string.IsNullOrWhiteSpace(value)) {
            return new DerivedValueResult(true, value);
        }

        _diagnostics?.Publish(new DevDiagnosticEvent(
            Code: "D31",
            Category: "FailClosed",
            Message: $"SystemValueProvider: IUserContextAccessor.{segmentName} is null/whitespace; command will submit without a pre-filled {segmentName}.",
            CapturedAt: DateTimeOffset.UtcNow));
        return DerivedValueResult.None;
    }
}
