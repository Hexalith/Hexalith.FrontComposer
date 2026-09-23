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

    /// <inheritdoc />
    public CommandExecutionAdmission TryAcquire(CommandExecutionAdmissionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        bool scopeAvailable = _validatedScope is not null
            ? _validatedScope.Current() is not null
            : _pendingCommandState is PendingCommandStateService concrete && concrete.IsScopeAvailable;
        if (!scopeAvailable) {
            return CommandExecutionAdmission.Denied(CommandExecutionAdmissionDenialReason.ScopeUnavailable, null, null);
        }

        lock (_sync) {
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
