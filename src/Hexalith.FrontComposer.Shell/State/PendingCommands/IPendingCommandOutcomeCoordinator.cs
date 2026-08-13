namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Coordinates accepted command registrations with terminal observations that may arrive before acknowledgement.
/// </summary>
public interface IPendingCommandOutcomeCoordinator : IPendingCommandOutcomeResolver {
    /// <summary>
    /// Buffers the first terminal observation produced before an accepted dispatch can be associated.
    /// </summary>
    /// <param name="ownerId">The correlation identifier that owns the in-flight dispatch.</param>
    /// <param name="observation">The terminal observation to buffer.</param>
    /// <returns>The buffering disposition.</returns>
    PendingCommandOutcomeResolutionResult BufferBeforeAccepted(
        string ownerId,
        PendingCommandOutcomeObservation observation);

    /// <summary>Associates an accepted MessageId with its pre-dispatch target and replays an early terminal observation.</summary>
    PendingCommandRegistrationResult AssociateAccepted(PendingCommandRegistration registration);

    /// <summary>Discards an unaccepted early observation owned by a canceled producer.</summary>
    void DiscardBuffered(string? messageId);

    /// <summary>Discards every unaccepted early observation owned by one canceled producer.</summary>
    /// <param name="ownerId">The correlation identifier that owns the in-flight dispatch.</param>
    void DiscardBufferedByOwner(string ownerId);
}
