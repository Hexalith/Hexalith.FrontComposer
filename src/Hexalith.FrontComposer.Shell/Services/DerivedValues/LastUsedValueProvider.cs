using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.DerivedValues;

/// <summary>
/// Derived-value provider #4 in the chain (Story 2-2 Decision D24). Reads the last-confirmed value
/// for a given (tenant, user, commandType, property) tuple from <see cref="IStorageService"/>.
/// FAILS CLOSED on missing tenant/user (Decision D31 + D39): returns <c>HasValue=false</c> for reads
/// and no-ops for writes, and publishes a single dev-mode <see cref="DevDiagnosticEvent"/> per circuit.
/// </summary>
public sealed class LastUsedValueProvider : IDerivedValueProvider, ILastUsedRecorder {
    private readonly IStorageService _storage;
    private readonly IUserContextAccessor? _userContext;
    private readonly IDiagnosticSink? _sink;
    private readonly ILogger<LastUsedValueProvider>? _logger;
    private readonly ConcurrentDictionary<(Type, string), PropertyInfo> _propertyCache = new();

    /// <summary>Gets a per-circuit flag set when a tenant/user guard refusal occurred.</summary>
    public bool TenantGuardTripped { get; private set; }

    public LastUsedValueProvider(
        IStorageService storage,
        IUserContextAccessor? userContext = null,
        IDiagnosticSink? sink = null,
        ILogger<LastUsedValueProvider>? logger = null) {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _userContext = userContext;
        _sink = sink;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (!TryResolveTenantAndUser(out string? tenantId, out string? userId)) {
            return DerivedValueResult.None;
        }

        string key;
        try {
            key = FrontComposerStorageKey.Build(tenantId, userId, commandType.FullName ?? commandType.Name, propertyName);
        }
        catch (InvalidOperationException) {
            return DerivedValueResult.None;
        }

        object? value = await _storage.GetAsync<object?>(key, ct).ConfigureAwait(false);
        return value is null ? DerivedValueResult.None : new DerivedValueResult(true, value);
    }

    /// <summary>
    /// Persists every non-system property of <paramref name="command"/> to storage. Called by the
    /// per-command emitted <c>{Command}LastUsedSubscriber</c> after a <c>Confirmed</c> lifecycle
    /// transition (Story 2-2 Decision D28).
    /// </summary>
    /// <inheritdoc cref="ILastUsedRecorder.RecordAsync{TCommand}"/>
    [UnconditionalSuppressMessage("Trimming", "IL2091:UnrecognizedReflectionPattern", Justification = "Call site is generated subscriber code with concrete TCommand type; trim preservation is the renderer's responsibility.")]
    public Task RecordAsync<TCommand>(TCommand command) where TCommand : class => Record(command);

    public async Task Record<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(TCommand command) where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);

        if (!TryResolveTenantAndUser(out string? tenantId, out string? userId)) {
            return;
        }

        Type commandType = typeof(TCommand);
        string commandFqn = commandType.FullName ?? commandType.Name;

        foreach (PropertyInfo prop in commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanRead || IsSystemOwnedProperty(prop.Name)) {
                continue;
            }

            string key;
            try {
                key = FrontComposerStorageKey.Build(tenantId, userId, commandFqn, prop.Name);
            }
            catch (InvalidOperationException) {
                return;
            }

            object? value = prop.GetValue(command);
            if (value is null) {
                await _storage.RemoveAsync(key).ConfigureAwait(false);
                continue;
            }

            await _storage.SetAsync(key, value).ConfigureAwait(false);
        }
    }

    private static bool IsSystemOwnedProperty(string name) => name switch {
        "MessageId" or "CommandId" or "CorrelationId" or "TenantId" or "UserId"
            or "Timestamp" or "CreatedAt" or "ModifiedAt" => true,
        _ => false,
    };

    private bool TryResolveTenantAndUser(out string? tenantId, out string? userId) {
        tenantId = _userContext?.TenantId;
        userId = _userContext?.UserId;

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId)) {
            if (!TenantGuardTripped) {
                TenantGuardTripped = true;
                _sink?.Publish(new DevDiagnosticEvent(
                    "D31",
                    "LastUsed",
                    "LastUsed persistence disabled: tenant/user context missing. Register an IUserContextAccessor bound to your auth stack, or set FcShellOptions.LastUsedDisabled=true to silence.",
                    DateTimeOffset.UtcNow));
                _logger?.LogWarning(
                    "LastUsed persistence disabled: tenantId or userId is null/empty — D31 fail-closed. Register an IUserContextAccessor in the host.");
            }

            tenantId = null;
            userId = null;
            return false;
        }

        return true;
    }
}
