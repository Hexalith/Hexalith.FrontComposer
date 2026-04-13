namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Provisional contract for tracking command lifecycle state transitions.
/// Story 1.3 may extend lifecycle handling through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface ICommandLifecycleTracker
{
    /// <summary>
    /// Returns the IDs of all commands currently in a non-Idle state.
    /// </summary>
    /// <returns>A read-only list of active command IDs.</returns>
    IReadOnlyList<string> GetActiveCommandIds();

    /// <summary>
    /// Returns the current lifecycle state for the given command.
    /// Returns <see cref="CommandLifecycleState.Idle"/> for unrecognized command IDs.
    /// </summary>
    /// <param name="commandId">The ULID message ID from <see cref="Communication.CommandResult"/>.</param>
    /// <returns>The current lifecycle state.</returns>
    CommandLifecycleState GetState(string commandId);

    /// <summary>
    /// Transitions a command to a new lifecycle state.
    /// </summary>
    /// <param name="commandId">The ULID message ID from <see cref="Communication.CommandResult"/>.</param>
    /// <param name="newState">The target lifecycle state.</param>
    void Transition(string commandId, CommandLifecycleState newState);
}
