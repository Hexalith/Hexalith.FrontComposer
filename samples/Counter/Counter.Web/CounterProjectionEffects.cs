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
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _pendingBatchAmounts = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _pendingIncrementAmounts = new();

    /// <summary>Initializes a new instance of the <see cref="CounterProjectionEffects"/> class.</summary>
    public CounterProjectionEffects(IState<CounterProjectionState> state) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>Captures the single-field increment amount until confirmation (mirrors batch behaviour).</summary>
    [EffectMethod]
    public Task OnIncrementSubmitted(IncrementCommandActions.SubmittedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _pendingIncrementAmounts[action.CorrelationId] = action.Command.Amount;
        return Task.CompletedTask;
    }

    /// <summary>Handles <see cref="IncrementCommandActions.ConfirmedAction"/> by re-requesting the Counter projection.</summary>
    [EffectMethod]
    public Task OnCommandConfirmed(IncrementCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        int delta = _pendingIncrementAmounts.TryRemove(action.CorrelationId, out int amount) ? amount : 1;
        return BumpAndDispatch(action.CorrelationId, dispatcher, delta);
    }

    /// <summary>Captures the submitted batch amount until the matching confirmation arrives.</summary>
    [EffectMethod]
    public Task OnBatchIncrementSubmitted(BatchIncrementCommandActions.SubmittedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _pendingBatchAmounts[action.CorrelationId] = action.Command.Amount;
        return Task.CompletedTask;
    }

    /// <summary>Story 2-2 Task 9.6 — handles <see cref="BatchIncrementCommandActions.ConfirmedAction"/>.</summary>
    [EffectMethod]
    public Task OnBatchIncrementConfirmed(BatchIncrementCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        int delta = _pendingBatchAmounts.TryRemove(action.CorrelationId, out int amount) ? amount : 1;
        return BumpAndDispatch(action.CorrelationId, dispatcher, delta);
    }

    /// <summary>Story 2-2 Task 9.6 — handles <see cref="ConfigureCounterCommandActions.ConfirmedAction"/>.</summary>
    [EffectMethod]
    public Task OnConfigureCounterConfirmed(ConfigureCounterCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return BumpAndDispatch(action.CorrelationId, dispatcher, delta: 0);
    }

    private Task BumpAndDispatch(string correlationId, IDispatcher dispatcher, int delta) {
        CounterProjection updated = new() {
            Id = "counter-1",
            Count = (_state.Value.Items?.FirstOrDefault()?.Count ?? 0) + delta,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        dispatcher.Dispatch(new CounterProjectionLoadRequestedAction(correlationId));
        dispatcher.Dispatch(new CounterProjectionLoadedAction(correlationId, [updated]));
        return Task.CompletedTask;
    }
}
