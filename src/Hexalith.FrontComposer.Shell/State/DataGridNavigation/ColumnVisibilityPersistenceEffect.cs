using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D7 — listens for <see cref="ColumnVisibilityChangedAction"/> and
/// <see cref="ResetColumnVisibilityAction"/>, then dispatches <see cref="CaptureGridStateAction"/>
/// IMMEDIATELY (no debounce — checkbox clicks are low-frequency) so Story 3-6's persistence
/// effect takes over. Keeps the reducer pure.
/// </summary>
public sealed class ColumnVisibilityPersistenceEffect {
    private readonly IState<DataGridNavigationState> _state;

    /// <summary>Initializes a new instance of the <see cref="ColumnVisibilityPersistenceEffect"/> class.</summary>
    /// <param name="state">Read-only navigation state.</param>
    public ColumnVisibilityPersistenceEffect(IState<DataGridNavigationState> state) {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <summary>Dispatches <see cref="CaptureGridStateAction"/> after a visibility toggle.</summary>
    [EffectMethod]
    public Task HandleColumnVisibilityChangedAsync(
        ColumnVisibilityChangedAction action,
        IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return DispatchCaptureAsync(action.ViewKey, dispatcher);
    }

    /// <summary>Dispatches <see cref="CaptureGridStateAction"/> after a visibility reset.</summary>
    [EffectMethod]
    public Task HandleResetColumnVisibilityAsync(
        ResetColumnVisibilityAction action,
        IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return DispatchCaptureAsync(action.ViewKey, dispatcher);
    }

    private Task DispatchCaptureAsync(string viewKey, IDispatcher dispatcher) {
        if (!_state.Value.ViewStates.TryGetValue(viewKey, out GridViewSnapshot? snapshot)) {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new CaptureGridStateAction(viewKey, snapshot));
        return Task.CompletedTask;
    }
}
