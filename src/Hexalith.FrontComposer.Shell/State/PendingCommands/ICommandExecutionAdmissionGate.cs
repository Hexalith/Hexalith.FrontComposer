namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Circuit-local admission gate for one-at-a-time command execution.
/// </summary>
public interface ICommandExecutionAdmissionGate
{
    /// <summary>
    /// Attempts to admit a command before generated-form lifecycle or dispatch side effects run.
    /// </summary>
    /// <param name="request">Framework metadata for the attempted command.</param>
    /// <returns>An admission result. Dispose admitted results when the pre-dispatch window closes.</returns>
    CommandExecutionAdmission TryAcquire(CommandExecutionAdmissionRequest request);
}

/// <summary>
/// Framework metadata for FC-CNC admission. It intentionally excludes command payloads and form values.
/// </summary>
public sealed record CommandExecutionAdmissionRequest
{
    public CommandExecutionAdmissionRequest(string commandTypeName, string? displayLabel = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandTypeName);
        CommandTypeName = commandTypeName;
        DisplayLabel = displayLabel;
    }

    public string CommandTypeName { get; }

    public string? DisplayLabel { get; }
}

/// <summary>Reason a command admission was denied.</summary>
public enum CommandExecutionAdmissionDenialReason
{
    None,
    AdmissionAlreadyInProgress,
    PendingCommandAlreadyExists,
}

/// <summary>Result of an FC-CNC admission attempt.</summary>
public sealed class CommandExecutionAdmission : IDisposable
{
    private readonly ICommandExecutionAdmissionReleaser? _releaser;

    private CommandExecutionAdmission(
        bool isAdmitted,
        CommandExecutionAdmissionDenialReason denialReason,
        string? blockingCommandTypeName,
        string? blockingMessageId,
        ICommandExecutionAdmissionReleaser? releaser)
    {
        IsAdmitted = isAdmitted;
        DenialReason = denialReason;
        BlockingCommandTypeName = blockingCommandTypeName;
        BlockingMessageId = blockingMessageId;
        _releaser = releaser;
    }

    public bool IsAdmitted { get; }

    public CommandExecutionAdmissionDenialReason DenialReason { get; }

    public string? BlockingCommandTypeName { get; }

    public string? BlockingMessageId { get; }

    public void Dispose() => _releaser?.Dispose();

    internal static CommandExecutionAdmission Admitted(long admissionId, CommandExecutionAdmissionGate owner) =>
        new(
            isAdmitted: true,
            CommandExecutionAdmissionDenialReason.None,
            blockingCommandTypeName: null,
            blockingMessageId: null,
            CommandExecutionAdmissionGate.CreateReleaser(owner, admissionId));

    internal static CommandExecutionAdmission Denied(
        CommandExecutionAdmissionDenialReason denialReason,
        string? blockingCommandTypeName,
        string? messageId) =>
        new(
            isAdmitted: false,
            denialReason,
            blockingCommandTypeName,
            messageId,
            releaser: null);
}

internal interface ICommandExecutionAdmissionReleaser : IDisposable
{
}

