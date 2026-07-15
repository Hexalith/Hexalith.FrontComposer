namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Result of an FC-CNC admission attempt.</summary>
public sealed class CommandExecutionAdmission : IDisposable {
    private readonly ICommandExecutionAdmissionReleaser? _releaser;

    private CommandExecutionAdmission(
        bool isAdmitted,
        CommandExecutionAdmissionDenialReason denialReason,
        string? blockingCommandTypeName,
        string? blockingMessageId,
        ICommandExecutionAdmissionReleaser? releaser) {
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
