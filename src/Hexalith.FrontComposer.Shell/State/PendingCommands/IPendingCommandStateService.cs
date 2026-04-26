namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Circuit-local, bounded pending-command index keyed by framework-generated ULID MessageId.
/// </summary>
public interface IPendingCommandStateService : IDisposable {
    PendingCommandRegistrationResult Register(PendingCommandRegistration registration);

    PendingCommandResolutionResult ResolveTerminal(PendingCommandTerminalObservation observation);

    PendingCommandEntry? GetByMessageId(string messageId);

    IReadOnlyList<PendingCommandEntry> Snapshot();

    void Clear(string reason);
}
