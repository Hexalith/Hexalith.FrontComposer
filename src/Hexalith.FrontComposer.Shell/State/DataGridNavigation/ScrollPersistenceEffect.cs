using System.Collections.Concurrent;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D4 — coalesces <see cref="ScrollCapturedAction"/> dispatches via a 150 ms
/// <see cref="TimeProvider"/>-anchored debounce per view key, then dispatches
/// <see cref="CaptureGridStateAction"/> so Story 3-6's existing persistence effect takes over.
/// Reducers remain pure; this effect is the sole layer that chains to persistence.
/// </summary>
public sealed class ScrollPersistenceEffect : IDisposable {
    /// <summary>Story 4-4 D4 — 150 ms (Schroeder's "instant" midpoint).</summary>
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(150);

    private readonly IState<DataGridNavigationState> _state;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.Ordinal);
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="ScrollPersistenceEffect"/> class.</summary>
    /// <param name="state">Read-only navigation state — the persistence effect captures the REDUCER-APPLIED snapshot, so we only need to trigger capture.</param>
    /// <param name="timeProvider">Time source for deterministic debounce.</param>
    public ScrollPersistenceEffect(IState<DataGridNavigationState> state, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Debounces and chains <see cref="CaptureGridStateAction"/>.</summary>
    [EffectMethod]
    public async Task HandleScrollCapturedAsync(ScrollCapturedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (IsDisposed()) {
            return;
        }

        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = null;
        _pending.AddOrUpdate(
            action.ViewKey,
            cts,
            (_, existing) => {
                previous = existing;
                return cts;
            });

        if (previous is not null) {
            TryCancelAndDispose(previous);
        }

        try {
            await Task.Delay(DebounceInterval, _timeProvider, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return;
        }
        finally {
            _pending.TryRemove(new KeyValuePair<string, CancellationTokenSource>(action.ViewKey, cts));
            cts.Dispose();
        }

        if (IsDisposed()) {
            return;
        }

        // Pull the reducer-applied snapshot so Story 3-6's HandleCaptureGridState receives the
        // latest scroll position verbatim.
        if (!_state.Value.ViewStates.TryGetValue(action.ViewKey, out GridViewSnapshot? snapshot)) {
            return;
        }

        dispatcher.Dispatch(new CaptureGridStateAction(action.ViewKey, snapshot));
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) {
            return;
        }

        foreach (KeyValuePair<string, CancellationTokenSource> kvp in _pending) {
            TryCancelAndDispose(kvp.Value);
        }

        _pending.Clear();
    }

    private bool IsDisposed() => Volatile.Read(ref _disposed) == 1;

    private static void TryCancelAndDispose(CancellationTokenSource cts) {
        try {
            cts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed — nothing to cancel.
        }
        finally {
            cts.Dispose();
        }
    }
}
