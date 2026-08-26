using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Supplies deterministic callback and polling dispatch plans for the Epic 9 composed lane.
/// </summary>
internal sealed class Epic9ScriptedCommandService(TimeProvider timeProvider)
    : ICommandServiceWithLifecycleObservations
{
    private readonly object _gate = new();
    private readonly Dictionary<Type, Queue<(string MessageId, bool EmitTerminal, CommandMateriality Materiality)>> _plans = [];

    /// <summary>Gets the typed commands that crossed the dispatch boundary.</summary>
    public List<object> DispatchedCommands { get; } = [];

    /// <summary>Adds the next deterministic result for a command type.</summary>
    public void Enqueue<TCommand>(
        string messageId,
        bool emitTerminal = true,
        CommandMateriality materiality = CommandMateriality.Material)
        where TCommand : class
    {
        lock (_gate)
        {
            if (!_plans.TryGetValue(typeof(TCommand), out Queue<(string, bool, CommandMateriality)>? plans))
            {
                plans = [];
                _plans.Add(typeof(TCommand), plans);
            }

            plans.Enqueue((messageId, emitTerminal, materiality));
        }
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class => DispatchAsync(command, null, cancellationToken);

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        (string MessageId, bool EmitTerminal, CommandMateriality Materiality) plan;
        lock (_gate)
        {
            DispatchedCommands.Add(command);
            if (!_plans.TryGetValue(typeof(TCommand), out Queue<(string, bool, CommandMateriality)>? plans)
                || !plans.TryDequeue(out plan))
            {
                throw new InvalidOperationException($"No Epic 9 dispatch plan exists for {typeof(TCommand).FullName}.");
            }
        }

        if (plan.EmitTerminal)
        {
            onLifecycleObservation?.Invoke(new CommandLifecycleObservation(
                CommandLifecycleState.Confirmed,
                plan.MessageId,
                plan.Materiality,
                timeProvider.GetUtcNow()));
        }

        return Task.FromResult(new CommandResult(plan.MessageId, CommandResultStatus.Accepted));
    }
}
