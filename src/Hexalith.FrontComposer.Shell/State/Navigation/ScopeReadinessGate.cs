using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Default <see cref="IScopeReadinessGate"/> implementation (Story 3-6 D20 / ADR-049).
/// Centralises the scope-flip detection + exactly-once dispatch logic so the
/// <see cref="ScopeFlipObserverEffect"/>'s eight action handlers collapse to one line each.
/// </summary>
/// <remarks>
/// <para>
/// <b>Exactly-once dispatch (A3 concurrency guard):</b> Fluxor's effect handlers run concurrently
/// by default. Two observed actions dispatched nearly simultaneously will both invoke
/// <see cref="EvaluateAsync"/> before the reducer has flipped
/// <see cref="FrontComposerNavigationState.StorageReady"/>. The <c>_dispatched</c> field with
/// <see cref="Interlocked.CompareExchange(ref int, int, int)"/> tiebreaker guarantees exactly-once
/// dispatch under racing handlers, closing the gap the <see cref="IState{TState}.Value"/> read alone
/// does not.
/// </para>
/// </remarks>
public sealed class ScopeReadinessGate : IScopeReadinessGate {
    private readonly IState<FrontComposerNavigationState> _state;
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly IUlidFactory? _ulidFactory;
    private readonly ILogger<ScopeReadinessGate> _logger;
    private int _lastObservedScopeReady = -1;
    private int _dispatched;

    /// <summary>Initializes a new instance of the <see cref="ScopeReadinessGate"/> class.</summary>
    /// <param name="state">Navigation state carrying the transient <c>StorageReady</c> flag.</param>
    /// <param name="userContextAccessor">Tenant / user accessor used for the scope-flip check.</param>
    /// <param name="ulidFactory">Optional correlation-id factory; falls back to <see cref="Guid.NewGuid"/>.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ScopeReadinessGate(
        IState<FrontComposerNavigationState> state,
        IUserContextAccessor userContextAccessor,
        IUlidFactory? ulidFactory,
        ILogger<ScopeReadinessGate> logger) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);
        _state = state;
        _userContextAccessor = userContextAccessor;
        _ulidFactory = ulidFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task EvaluateAsync(IDispatcher dispatcher, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (cancellationToken.IsCancellationRequested) {
            return Task.CompletedTask;
        }

        // Cheap short-circuit — state-level flag already set, skip the Interlocked cost.
        if (_state.Value.StorageReady || Volatile.Read(ref _dispatched) != 0) {
            return Task.CompletedTask;
        }

        string? tenant = _userContextAccessor.TenantId;
        string? user = _userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(user)) {
            Interlocked.Exchange(ref _lastObservedScopeReady, 0);
            return Task.CompletedTask;
        }

        int previous = Interlocked.Exchange(ref _lastObservedScopeReady, 1);
        if (previous != 0) {
            return Task.CompletedTask;
        }

        // A3 tiebreaker — exactly-once dispatch even under concurrent EvaluateAsync invocations.
        if (Interlocked.CompareExchange(ref _dispatched, 1, 0) != 0) {
            return Task.CompletedTask;
        }

        string correlationId = NewCorrelationId();
        _logger.LogDebug("StorageReadyAction dispatched for first-time scope flip. CorrelationId={CorrelationId}.", correlationId);
        dispatcher.Dispatch(new StorageReadyAction(correlationId));
        return Task.CompletedTask;
    }

    private string NewCorrelationId()
        => _ulidFactory?.NewUlid() ?? Guid.NewGuid().ToString("N");
}
