using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

internal sealed class CreateCounterTargetIdentityProvider : ICommandTargetIdentityProvider<CreateCounterCommand>
{
    public ValueTask<CommandTargetIdentity?> ResolveAsync(
        CreateCounterCommand command,
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
