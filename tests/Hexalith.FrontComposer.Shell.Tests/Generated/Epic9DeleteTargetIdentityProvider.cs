using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Resolves the delete target used to prove the no-indicator disposition.</summary>
internal sealed class Epic9DeleteTargetIdentityProvider
    : ICommandTargetIdentityProvider<DeleteProviderTargetCommand>
{
    /// <inheritdoc />
    public ValueTask<CommandTargetIdentity?> ResolveAsync(
        DeleteProviderTargetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<CommandTargetIdentity?>(new(
            "Counter:Counter.Domain.CounterProjection",
            "counter-deleted"));
    }
}
