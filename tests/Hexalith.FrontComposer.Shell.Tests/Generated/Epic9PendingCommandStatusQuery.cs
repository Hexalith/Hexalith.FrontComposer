using Hexalith.FrontComposer.Shell.State.PendingCommands;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Returns one deterministic terminal observation for the Epic 9 polling path.</summary>
internal sealed class Epic9PendingCommandStatusQuery(PendingCommandOutcomeObservation observation)
    : IPendingCommandStatusQuery
{
    /// <inheritdoc />
    public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pendingCommand);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<PendingCommandOutcomeObservation?>(observation);
    }
}
