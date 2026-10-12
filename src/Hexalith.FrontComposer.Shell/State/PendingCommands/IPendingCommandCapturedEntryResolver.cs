namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Resolves polling evidence only while the originally observed pending entry still owns its identity.</summary>
/// <remarks>Custom outcome resolvers used with polling must implement this capability atomically with their terminal mutation.</remarks>
public interface IPendingCommandCapturedEntryResolver : IPendingCommandOutcomeResolver
{
    /// <summary>Resolves an observation for the exact pending entry captured before the status query.</summary>
    PendingCommandOutcomeResolutionResult ResolveForEntry(
        PendingCommandOutcomeObservation observation,
        PendingCommandEntry expectedEntry);
}
