using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

internal sealed class UpdateCounterTargetIdentityProvider : ICommandTargetIdentityProvider<UpdateCounterCommand>
{
    public ValueTask<CommandTargetIdentity?> ResolveAsync(
        UpdateCounterCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(command.CounterId))
        {
            return ValueTask.FromResult<CommandTargetIdentity?>(null);
        }

        return ValueTask.FromResult<CommandTargetIdentity?>(new(
            "Counter:Counter.Domain.CounterProjection",
            command.CounterId));
    }
}
