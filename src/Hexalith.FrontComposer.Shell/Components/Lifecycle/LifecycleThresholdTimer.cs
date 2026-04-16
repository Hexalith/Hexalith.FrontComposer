namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// Story 2-4 Decision D4 / D5 / ADR-021 — single tick-loop threshold timer for
/// <see cref="FcLifecycleWrapper"/>. Recomputes <see cref="CurrentPhase"/> on every
/// <see cref="TimeProvider.CreateTimer"/> callback (100 ms by default) and raises
/// <see cref="OnPhaseChanged"/> when the phase advances.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="TimeProvider.CreateTimer"/> (returns <see cref="ITimer"/>) — NOT
/// <see cref="PeriodicTimer"/> — so <c>FakeTimeProvider.Advance(TimeSpan)</c> in tests
/// deterministically drives the Tick callback. See D5.
/// </para>
/// <para>
/// Phase computation is pure: <c>(elapsed &lt; pulseMs) ? NoPulse : (elapsed &lt; stillSyncingMs)
/// ? Pulse : (elapsed &lt; timeoutMs) ? StillSyncing : ActionPrompt</c>. Under reconnect / replay
/// the anchor <see cref="Reset"/> is called with the transition's <c>LastTransitionAt</c> and the
/// next Tick recomputes Phase correctly (Story 2-3 D15 monotonic anchor / Sally Story C).
/// </para>
/// <para>
/// Thread-safety: mutating state (<see cref="_anchor"/>, <see cref="_currentPhase"/>, the
/// cached thresholds) is assigned atomically in the Tick / Reset path; callers are expected
/// to serialize Start / Reset / Stop calls through the Blazor dispatcher
/// (<see cref="Microsoft.AspNetCore.Components.ComponentBase.InvokeAsync(System.Action)"/>).
/// </para>
/// </remarks>
public sealed class LifecycleThresholdTimer : IDisposable {
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

    private readonly TimeProvider _time;
    private readonly Func<bool>? _isDisconnected;
    private readonly object _sync = new();

    private int _pulseThresholdMs;
    private int _stillSyncingThresholdMs;
    private int _timeoutActionThresholdMs;

    private ITimer? _tickTimer;
    private DateTimeOffset _anchor;
    private LifecycleTimerPhase _currentPhase = LifecycleTimerPhase.NoPulse;
    private bool _running;
    private bool _disposed;

    /// <summary>Initializes a new <see cref="LifecycleThresholdTimer"/>.</summary>
    /// <param name="time">The <see cref="TimeProvider"/> — <see cref="TimeProvider.System"/> in prod, fake in tests.</param>
    /// <param name="pulseThresholdMs">Elapsed ms before entering <see cref="LifecycleTimerPhase.Pulse"/>.</param>
    /// <param name="stillSyncingThresholdMs">Elapsed ms before entering <see cref="LifecycleTimerPhase.StillSyncing"/>.</param>
    /// <param name="timeoutActionThresholdMs">Elapsed ms before entering <see cref="LifecycleTimerPhase.ActionPrompt"/>.</param>
    /// <param name="isDisconnected">Optional hook (Story 2-4 D23). Story 5-3 will populate with a SignalR-state check; null in v0.1.</param>
    public LifecycleThresholdTimer(
        TimeProvider time,
        int pulseThresholdMs,
        int stillSyncingThresholdMs,
        int timeoutActionThresholdMs,
        Func<bool>? isDisconnected = null) {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
        _pulseThresholdMs = pulseThresholdMs;
        _stillSyncingThresholdMs = stillSyncingThresholdMs;
        _timeoutActionThresholdMs = timeoutActionThresholdMs;
        _isDisconnected = isDisconnected;
        _anchor = time.GetUtcNow();
    }

    /// <summary>Raised whenever <see cref="CurrentPhase"/> advances to a new phase.</summary>
    public event Action<LifecycleTimerPhase>? OnPhaseChanged;

    /// <summary>Gets the current phase computed from elapsed time since <see cref="_anchor"/>.</summary>
    public LifecycleTimerPhase CurrentPhase {
        get {
            // Recompute lazily so callers that read without driving the ITimer
            // (e.g., synchronous unit tests advancing a fake clock and then reading) still see
            // the correct phase. This is safe: computing and surfacing Phase without firing
            // OnPhaseChanged is allowed (events are the authoritative advance signal).
            TryAdvance();
            return _currentPhase;
        }
    }

    /// <summary>
    /// Atomically updates the threshold values — called by <see cref="FcLifecycleWrapper"/>
    /// on <c>IOptionsMonitor&lt;FcShellOptions&gt;.OnChange</c>. ADR-023 retroactive next-tick
    /// semantics — the next Tick observes the new thresholds and may advance Phase.
    /// </summary>
    /// <param name="pulseMs">New pulse threshold.</param>
    /// <param name="stillSyncingMs">New still-syncing threshold.</param>
    /// <param name="timeoutMs">New timeout-action threshold.</param>
    public void UpdateThresholds(int pulseMs, int stillSyncingMs, int timeoutMs) {
        Interlocked.Exchange(ref _pulseThresholdMs, pulseMs);
        Interlocked.Exchange(ref _stillSyncingThresholdMs, stillSyncingMs);
        Interlocked.Exchange(ref _timeoutActionThresholdMs, timeoutMs);
    }

    /// <summary>
    /// Starts the internal tick loop. Idempotent: repeated calls are noops.
    /// Emits <see cref="OnPhaseChanged"/>(<see cref="LifecycleTimerPhase.NoPulse"/>) on first start
    /// so consumers can seed UI state without a custom init path.
    /// </summary>
    public void Start() {
        lock (_sync) {
            if (_disposed || _running) {
                return;
            }
            _running = true;
            _currentPhase = LifecycleTimerPhase.NoPulse;
            _tickTimer = _time.CreateTimer(TickCallback, state: null, dueTime: TickInterval, period: TickInterval);
        }

        OnPhaseChanged?.Invoke(LifecycleTimerPhase.NoPulse);
    }

    /// <summary>
    /// Rewinds the anchor to <paramref name="newAnchor"/>. Phase is recomputed from elapsed time
    /// since the new anchor; typically rewinds Phase to <see cref="LifecycleTimerPhase.NoPulse"/>.
    /// </summary>
    /// <param name="newAnchor">The new monotonic anchor (Story 2-3 D15 <c>LastTransitionAt</c>).</param>
    public void Reset(DateTimeOffset newAnchor) {
        LifecycleTimerPhase previous;
        LifecycleTimerPhase next;
        lock (_sync) {
            if (_disposed) {
                return;
            }
            _anchor = newAnchor;
            previous = _currentPhase;
            next = ComputePhase();
            _currentPhase = next;
        }

        if (previous != next) {
            OnPhaseChanged?.Invoke(next);
        }
    }

    /// <summary>
    /// Stops the tick loop without disposing. Idempotent. <see cref="Start"/> may be called again
    /// to resume; typical lifecycle is Stop on terminal transitions and Dispose on component teardown.
    /// </summary>
    public void Stop() {
        ITimer? toDispose = null;
        lock (_sync) {
            if (!_running) {
                return;
            }
            _running = false;
            toDispose = _tickTimer;
            _tickTimer = null;
        }

        toDispose?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() {
        ITimer? toDispose = null;
        lock (_sync) {
            if (_disposed) {
                return;
            }
            _disposed = true;
            _running = false;
            toDispose = _tickTimer;
            _tickTimer = null;
            OnPhaseChanged = null;
        }

        toDispose?.Dispose();
    }

    private void TickCallback(object? _) => TryAdvance();

    private void TryAdvance() {
        LifecycleTimerPhase previous;
        LifecycleTimerPhase next;
        lock (_sync) {
            if (_disposed) {
                return;
            }
            previous = _currentPhase;
            next = ComputePhase();
            if (next == previous) {
                return;
            }
            _currentPhase = next;
        }

        OnPhaseChanged?.Invoke(next);
    }

    private LifecycleTimerPhase ComputePhase() {
        // Terminal short-circuit. Once Terminal, it does not regress without Reset (D19).
        if (_currentPhase == LifecycleTimerPhase.Terminal) {
            return LifecycleTimerPhase.Terminal;
        }

        // D23 — if the optional disconnected hook fires, escalate straight to ActionPrompt.
        if (_isDisconnected is not null && _isDisconnected()) {
            return LifecycleTimerPhase.ActionPrompt;
        }

        TimeSpan elapsed = _time.GetUtcNow() - _anchor;
        double elapsedMs = elapsed.TotalMilliseconds;
        if (elapsedMs < _pulseThresholdMs) {
            return LifecycleTimerPhase.NoPulse;
        }
        if (elapsedMs < _stillSyncingThresholdMs) {
            return LifecycleTimerPhase.Pulse;
        }
        if (elapsedMs < _timeoutActionThresholdMs) {
            return LifecycleTimerPhase.StillSyncing;
        }
        return LifecycleTimerPhase.ActionPrompt;
    }

    /// <summary>
    /// Story 2-4 D19 — explicit terminal pivot for Confirmed/Rejected transitions. Stops the tick
    /// loop and emits <see cref="LifecycleTimerPhase.Terminal"/> so the wrapper can swap UI to the
    /// message-bar state without further phase advances.
    /// </summary>
    public void EnterTerminal() {
        ITimer? toDispose = null;
        bool emit = false;
        lock (_sync) {
            if (_disposed) {
                return;
            }
            if (_currentPhase != LifecycleTimerPhase.Terminal) {
                _currentPhase = LifecycleTimerPhase.Terminal;
                emit = true;
            }
            _running = false;
            toDispose = _tickTimer;
            _tickTimer = null;
        }

        toDispose?.Dispose();
        if (emit) {
            OnPhaseChanged?.Invoke(LifecycleTimerPhase.Terminal);
        }
    }
}
