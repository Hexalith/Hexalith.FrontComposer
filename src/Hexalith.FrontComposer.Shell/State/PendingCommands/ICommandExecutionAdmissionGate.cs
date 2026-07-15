namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Circuit-local admission gate for one-at-a-time command execution.
/// </summary>
public interface ICommandExecutionAdmissionGate {
    /// <summary>
    /// Attempts to admit a command before generated-form lifecycle or dispatch side effects run.
    /// </summary>
    /// <param name="request">Framework metadata for the attempted command.</param>
    /// <returns>An admission result. Dispose admitted results when the pre-dispatch window closes.</returns>
    CommandExecutionAdmission TryAcquire(CommandExecutionAdmissionRequest request);
}
