using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Accepts every <see cref="McpAuditLifecycleCommand"/> and echoes the handles the invoker injected, so the
/// lifecycle store records exactly the handles the agent receives.
/// </summary>
internal sealed class McpAuditCommandService : ICommandServiceWithLifecycle {
    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, null, cancellationToken);

    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        McpAuditLifecycleCommand dispatched = command as McpAuditLifecycleCommand
            ?? throw new InvalidOperationException("Unexpected audit command type.");
        return Task.FromResult(new CommandResult(dispatched.MessageId, CommandResultStatus.Accepted, dispatched.CorrelationId));
    }
}
