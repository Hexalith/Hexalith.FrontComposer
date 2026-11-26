using Counter.Domain;

using Fluxor;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Counter.Web;

/// <summary>
/// Sample-code effect that simulates SignalR projection catch-up. When a Counter command is
/// confirmed, it sends a delta to the projection reducer so the demo visibly updates the Counter grid.
/// In the real EventStore pipeline (Story 5.1+), incoming SignalR notifications drive the update.
/// </summary>
public sealed class CounterProjectionEffects {
    private readonly IState<CounterProjectionState> _state;
    private readonly IFrontComposerTenantContextAccessor _tenantContext;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Amount, TenantContextSnapshot Scope, long ScopeGeneration)> _pendingBatchAmounts = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Amount, TenantContextSnapshot Scope, long ScopeGeneration)> _pendingIncrementAmounts = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (TenantContextSnapshot Scope, long ScopeGeneration)> _pendingConfigureScopes = new();

    /// <summary>Initializes a new instance of the <see cref="CounterProjectionEffects"/> class.</summary>
    public CounterProjectionEffects(IState<CounterProjectionState> state, IFrontComposerTenantContextAccessor tenantContext) {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>Captures the single-field increment amount until confirmation (mirrors batch behaviour).</summary>
    [EffectMethod]
    public Task OnIncrementSubmitted(IncrementCommandActions.SubmittedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (_tenantContext.TryGetContext(operationKind: "counter-increment-submit").Context is { } scope) {
            _pendingIncrementAmounts[action.CorrelationId] = (action.Command.Amount, scope, _state.Value.ScopeGeneration);
        }
        return Task.CompletedTask;
    }

    /// <summary>Handles <see cref="IncrementCommandActions.ConfirmedAction"/> by re-requesting the Counter projection.</summary>
    [EffectMethod]
    public Task OnCommandConfirmed(IncrementCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return _pendingIncrementAmounts.TryRemove(action.CorrelationId, out var pending)
            ? BumpAndDispatch(action.CorrelationId, dispatcher, pending.Amount, pending.Scope, pending.ScopeGeneration)
            : Task.CompletedTask;
    }

    /// <summary>Captures the submitted batch amount until the matching confirmation arrives.</summary>
    [EffectMethod]
    public Task OnBatchIncrementSubmitted(BatchIncrementCommandActions.SubmittedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (_tenantContext.TryGetContext(operationKind: "counter-batch-submit").Context is { } scope) {
            _pendingBatchAmounts[action.CorrelationId] = (action.Command.Amount, scope, _state.Value.ScopeGeneration);
        }
        return Task.CompletedTask;
    }

    /// <summary>Story 2-2 Task 9.6 — handles <see cref="BatchIncrementCommandActions.ConfirmedAction"/>.</summary>
    [EffectMethod]
    public Task OnBatchIncrementConfirmed(BatchIncrementCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return _pendingBatchAmounts.TryRemove(action.CorrelationId, out var pending)
            ? BumpAndDispatch(action.CorrelationId, dispatcher, pending.Amount, pending.Scope, pending.ScopeGeneration)
            : Task.CompletedTask;
    }

    /// <summary>Captures the configure submission scope for its later confirmation.</summary>
    [EffectMethod]
    public Task OnConfigureCounterSubmitted(ConfigureCounterCommandActions.SubmittedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (_tenantContext.TryGetContext(operationKind: "counter-configure-submit").Context is { } scope) {
            _pendingConfigureScopes[action.CorrelationId] = (scope, _state.Value.ScopeGeneration);
        }

        return Task.CompletedTask;
    }

    /// <summary>Story 2-2 Task 9.6 — handles <see cref="ConfigureCounterCommandActions.ConfirmedAction"/>.</summary>
    [EffectMethod]
    public Task OnConfigureCounterConfirmed(ConfigureCounterCommandActions.ConfirmedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        return _pendingConfigureScopes.TryRemove(action.CorrelationId, out var pending)
            ? BumpAndDispatch(action.CorrelationId, dispatcher, delta: 0, pending.Scope, pending.ScopeGeneration)
            : Task.CompletedTask;
    }

    private Task BumpAndDispatch(string correlationId, IDispatcher dispatcher, int delta, TenantContextSnapshot scope, long scopeGeneration) {
        if (_state.Value.ScopeGeneration != scopeGeneration
            || !_tenantContext.Revalidate(scope, operationKind: "counter-projection-confirm").Succeeded) {
            return Task.CompletedTask;
        }

        if (_tenantContext.Revalidate(scope, operationKind: "counter-projection-dispatch").Succeeded) {
            dispatcher.Dispatch(new CounterProjectionDeltaAction(correlationId, delta, scope.TenantId, scope.UserId, scopeGeneration, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    /// <summary>Applies a confirmed delta to the state being reduced, including prior queued deltas.</summary>
    [ReducerMethod]
    public static CounterProjectionState ApplyConfirmedDelta(CounterProjectionState state, CounterProjectionDeltaAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (state.ScopeGeneration != action.ScopeGeneration)
        {
            return state;
        }

        CounterProjectionState requested = CounterProjectionReducers.OnCounterProjectionLoadRequested(state,
            new CounterProjectionLoadRequestedAction(action.CorrelationId)
            {
                ScopeTenantId = action.TenantId,
                ScopeUserId = action.UserId,
                OriginScopeGeneration = action.ScopeGeneration,
            });
        CounterProjection? current = state.Items is { Count: > 0 } items ? items[0] : null;
        CounterProjection updated = new()
        {
            Id = "counter-1",
            Count = (current?.Count ?? 0) + action.Delta,
            LastUpdated = current is not null && current.LastUpdated > action.Timestamp ? current.LastUpdated : action.Timestamp,
        };
        return CounterProjectionReducers.OnCounterProjectionLoaded(requested,
            new CounterProjectionLoadedAction(action.CorrelationId, [updated])
            {
                OriginScopeGeneration = action.ScopeGeneration,
            });
    }
}
