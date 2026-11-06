using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Records every dispatched <see cref="McpAuditLifecycleCommand"/> with the values the invoker injected, and
/// acknowledges it with those injected handles.
/// </summary>
internal sealed class McpRecordingAuditCommandService : ICommandServiceWithLifecycle {
    /// <summary>Gets the commands dispatched so far, in dispatch order.</summary>
    internal ConcurrentQueue<McpAuditLifecycleCommand> Dispatched { get; } = new();

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
        Dispatched.Enqueue(dispatched);
        return Task.FromResult(new CommandResult(dispatched.MessageId, CommandResultStatus.Accepted, dispatched.CorrelationId));
    }
}
