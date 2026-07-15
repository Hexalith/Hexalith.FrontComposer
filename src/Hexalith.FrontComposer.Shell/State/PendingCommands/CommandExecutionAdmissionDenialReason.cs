namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Reason a command admission was denied.</summary>
public enum CommandExecutionAdmissionDenialReason {
    None,
    AdmissionAlreadyInProgress,
    PendingCommandAlreadyExists,
}
