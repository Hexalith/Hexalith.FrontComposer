using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>
/// Story 5-3 DN1 — periodic driver that fires bounded fallback polling while the EventStore
/// projection hub is disconnected. Subscribes to <see cref="IProjectionConnectionState"/> and
/// runs a <see cref="PeriodicTimer"/> only when the snapshot reports
/// <see cref="ProjectionConnectionSnapshot.IsDisconnected"/>. Stops promptly on reconnect,
/// disposal, or option disablement (interval &lt;= 0). Visible-lane refresh is delegated to the
/// scheduler, which already gates on the same disconnected-state and disabled-interval rules
/// (defense in depth — the driver does not retain unrelated polling state).
/// </summary>
public sealed class ProjectionFallbackPollingDriver : IAsyncDisposable {
    private readonly IProjectionConnectionState _connectionState;
    private readonly IProjectionFallbackRefreshScheduler _scheduler;
    private readonly IPendingCommandPollingCoordinator? _pendingCommandPolling;
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly ILogger<ProjectionFallbackPollingDriver> _logger;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly object _sync = new();
    private IDisposable? _subscription;
    private Task? _loopTask;
    private CancellationTokenSource? _loopCts;
    private int _disposed;

    public ProjectionFallbackPollingDriver(
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler scheduler,
        IOptionsMonitor<FcShellOptions> options,
        ILogger<ProjectionFallbackPollingDriver> logger,
        IPendingCommandPollingCoordinator? pendingCommandPolling = null) {
        ArgumentNullException.ThrowIfNull(connectionState);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionState = connectionState;
        _scheduler = scheduler;
        _options = options;
        _logger = logger;
        _pendingCommandPolling = pendingCommandPolling;
    }

    /// <summary>Starts subscribing to connection-state transitions. Idempotent.</summary>
    public void Start() {
        if (_disposed != 0) {
            return;
        }

        lock (_sync) {
            _subscription ??= _connectionState.Subscribe(OnConnectionChanged);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        try {
            _disposalCts.Cancel();
        }
        catch (ObjectDisposedException) {
        }

        IDisposable? sub;
        Task? loop;
        CancellationTokenSource? loopCts;
        lock (_sync) {
            sub = _subscription;
            _subscription = null;
            loop = _loopTask;
            loopCts = _loopCts;
            _loopTask = null;
            _loopCts = null;
        }

        sub?.Dispose();
        if (loopCts is not null) {
            try {
                loopCts.Cancel();
            }
            catch (ObjectDisposedException) {
            }
        }

        if (loop is not null) {
            try {
                await loop.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                // Loop already logs failures; swallow to keep disposal safe.
            }
        }

        loopCts?.Dispose();
        _disposalCts.Dispose();
    }

    private void OnConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        if (snapshot.IsDisconnected) {
            EnsureLoopRunning();
        }
        else {
            CancelLoop();
        }
    }

    private void EnsureLoopRunning() {
        lock (_sync) {
            if (_disposed != 0 || _loopTask is { IsCompleted: false }) {
                return;
            }

            _loopCts?.Dispose();
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(_disposalCts.Token);
            CancellationToken token = _loopCts.Token;
            _loopTask = Task.Run(() => RunAsync(token), token);
        }
    }

    private void CancelLoop() {
        CancellationTokenSource? toCancel;
        lock (_sync) {
            toCancel = _loopCts;
        }

        if (toCancel is null) {
            return;
        }

        try {
            toCancel.Cancel();
        }
        catch (ObjectDisposedException) {
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken) {
        try {
            while (!cancellationToken.IsCancellationRequested) {
                FcShellOptions current = _options.CurrentValue;
                int intervalSeconds = current.ProjectionFallbackPollingIntervalSeconds;
                if (intervalSeconds <= 0 || !_connectionState.Current.IsDisconnected) {
                    return;
                }

                try {
                    _ = await _scheduler.TriggerFallbackOnceAsync(cancellationToken).ConfigureAwait(false);
                    if (_pendingCommandPolling is not null) {
                        _ = await _pendingCommandPolling.PollOnceAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    FrontComposerLog.ProjectionFallbackPollingIterationFailed(_logger, ex.GetType().Name);
                }

                try {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Projection fallback polling loop terminated unexpectedly. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }
}
