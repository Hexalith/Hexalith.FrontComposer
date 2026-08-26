using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Resolves the declared destination lane and statuses for the Epic 9 matrix.</summary>
internal sealed class Epic9StatusMoveTargetIdentityProvider
    : ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand>
{
    /// <inheritdoc />
    public ValueTask<CommandTargetIdentity?> ResolveAsync(
        StatusMoveProviderTargetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<CommandTargetIdentity?>(new(
            "Counter:Counter.Domain.CounterProjection",
            "counter-status-destination",
            PriorStatus: "Draft",
            ExpectedStatus: "Approved"));
    }
}
