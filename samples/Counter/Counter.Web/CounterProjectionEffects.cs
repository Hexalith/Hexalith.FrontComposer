using Counter.Domain;

using Fluxor;

namespace Counter.Web;

/// <summary>
/// Sample-code effect that simulates SignalR projection catch-up. When a Counter command is
/// confirmed, it re-dispatches <see cref="CounterProjectionLoadRequestedAction"/> so the demo
/// visibly updates the Counter grid. In the real EventStore pipeline (Story 5.1+), this behaviour
/// is driven by incoming SignalR notifications rather than a client-local effect.
/// </summary>
public sealed class CounterProjectionEffects {
    private readonly IState<CounterProjectionState> _state;

    /// <summary>Initializes a new instance of the <see cref="CounterProjectionEffects"/> class.</summary>
    public CounterProjectionEffects(IState<CounterProjectionState> state) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>Handles <see cref="IncrementCommandActions.ConfirmedAction"/> by re-requesting the Counter projection.</summary>
    [EffectMethod]
    public Task OnCommandConfirmed(IncrementCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // Bump the in-memory counter so the grid actually changes for the demo.
        CounterProjection updated = new() {
            Id = "counter-1",
            Count = (_state.Value.Items?.FirstOrDefault()?.Count ?? 0) + 1,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        dispatcher.Dispatch(new CounterProjectionLoadRequestedAction(action.CorrelationId));
        dispatcher.Dispatch(new CounterProjectionLoadedAction(action.CorrelationId, [updated]));
        return Task.CompletedTask;
    }
}
