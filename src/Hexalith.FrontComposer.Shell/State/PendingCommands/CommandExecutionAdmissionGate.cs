namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Circuit-local FC-CNC admission gate for generated command submissions.
/// </summary>
public sealed class CommandExecutionAdmissionGate(
    IPendingCommandStateService pendingCommandState,
    TimeProvider? timeProvider = null,
    IValidatedPendingScope? validatedScope = null) : ICommandExecutionAdmissionGate {
    private readonly object _sync = new();
    private readonly IPendingCommandStateService _pendingCommandState = pendingCommandState ?? throw new ArgumentNullException(nameof(pendingCommandState));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IValidatedPendingScope? _validatedScope = validatedScope;
    private CommandExecutionAdmissionMetadata? _currentAdmission;
    private long _nextAdmissionId;
    private long _scopeGeneration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutionAdmissionGate"/> class without a
    /// validated pending scope. Kept for binary compatibility with 4.4 callers; admission then requires
    /// the pending-command state service to report an available scope, so it still fails closed.
    /// </summary>
    /// <param name="pendingCommandState">The pending-command state service.</param>
    /// <param name="timeProvider">The time provider, or <see langword="null"/> for the system clock.</param>
    public CommandExecutionAdmissionGate(IPendingCommandStateService pendingCommandState, TimeProvider? timeProvider)
        : this(pendingCommandState, timeProvider, validatedScope: null) {
    }

    /// <inheritdoc />
    public CommandExecutionAdmission TryAcquire(CommandExecutionAdmissionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        long originatingGeneration = Volatile.Read(ref _scopeGeneration);
        (string TenantId, string UserId)? originatingScope = _validatedScope?.Current();
        bool scopeAvailable = _validatedScope is not null
            ? originatingScope is not null
            : _pendingCommandState is PendingCommandStateService concrete && concrete.IsScopeAvailable;
        if (!scopeAvailable) {
            return CommandExecutionAdmission.Denied(CommandExecutionAdmissionDenialReason.ScopeUnavailable, null, null);
        }

        lock (_sync) {
            if (originatingGeneration != _scopeGeneration
                || (_validatedScope is not null && _validatedScope.Current() != originatingScope)
                || (_validatedScope is null
                    && _pendingCommandState is PendingCommandStateService currentState
                    && !currentState.IsScopeAvailable)) {
                return CommandExecutionAdmission.Denied(CommandExecutionAdmissionDenialReason.ScopeUnavailable, null, null);
            }

            if (_currentAdmission is not null) {
                return CommandExecutionAdmission.Denied(
                    CommandExecutionAdmissionDenialReason.AdmissionAlreadyInProgress,
                    _currentAdmission.CommandTypeName,
                    messageId: null);
            }

            PendingCommandEntry? pending = _pendingCommandState
                .Snapshot()
                .FirstOrDefault(static e => e.Status == PendingCommandStatus.Pending);

            if (pending is not null) {
                return CommandExecutionAdmission.Denied(
                    CommandExecutionAdmissionDenialReason.PendingCommandAlreadyExists,
                    pending.CommandTypeName,
                    pending.MessageId);
            }

            long admissionId = ++_nextAdmissionId;
            _currentAdmission = new CommandExecutionAdmissionMetadata(
                admissionId,
                request.CommandTypeName,
                request.DisplayLabel,
                _timeProvider.GetUtcNow());

            return CommandExecutionAdmission.Admitted(admissionId, this);
        }
    }

    internal void ResetScope() {
        lock (_sync) {
            _scopeGeneration++;
            _currentAdmission = null;
        }
    }

    private void Release(long admissionId) {
        lock (_sync) {
            if (_currentAdmission?.AdmissionId == admissionId) {
                _currentAdmission = null;
            }
        }
    }

    private sealed record CommandExecutionAdmissionMetadata(
        long AdmissionId,
        string CommandTypeName,
        string? DisplayLabel,
        DateTimeOffset AdmittedAt);

    private sealed class AdmissionReleaser(CommandExecutionAdmissionGate owner, long admissionId) : ICommandExecutionAdmissionReleaser {
        private int _released;

        public void Dispose() {
            if (Interlocked.Exchange(ref _released, 1) == 0) {
                owner.Release(admissionId);
            }
        }
    }

    internal static ICommandExecutionAdmissionReleaser CreateReleaser(CommandExecutionAdmissionGate owner, long admissionId) =>
        new AdmissionReleaser(owner, admissionId);
}
