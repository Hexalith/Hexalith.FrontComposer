using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Resolves the explicit cross-row destination carried by the typed test command.</summary>
internal sealed class Epic9CrossRowTargetIdentityProvider
    : ICommandTargetIdentityProvider<CrossRowProviderTargetCommand>
{
    /// <inheritdoc />
    public ValueTask<CommandTargetIdentity?> ResolveAsync(
        CrossRowProviderTargetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<CommandTargetIdentity?>(new(
            "Counter:Counter.Domain.CounterProjection",
            command.DestinationId));
    }
}
