using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;

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
    private static readonly TimeSpan DisposeWaitTimeout = TimeSpan.FromSeconds(2);

    private readonly IProjectionConnectionState _connectionState;
    private readonly IProjectionFallbackRefreshScheduler _scheduler;
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProjectionFallbackPollingDriver> _logger;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly object _sync = new();
    private IDisposable? _subscription;
    private IDisposable? _optionsChangeRegistration;
    private Task? _loopTask;
    private CancellationTokenSource? _loopCts;
    private bool _started;
    private bool _fatalLoopFault;
    private int _disposed;

    public ProjectionFallbackPollingDriver(
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler scheduler,
        IOptionsMonitor<FcShellOptions> options,
        ILogger<ProjectionFallbackPollingDriver> logger)
        : this(connectionState, scheduler, options, logger, TimeProvider.System) {
    }

    internal ProjectionFallbackPollingDriver(
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler scheduler,
        IOptionsMonitor<FcShellOptions> options,
        ILogger<ProjectionFallbackPollingDriver> logger,
        TimeProvider timeProvider) {
        ArgumentNullException.ThrowIfNull(connectionState);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _connectionState = connectionState;
        _scheduler = scheduler;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>Starts subscribing to connection-state transitions. Idempotent.</summary>
    public void Start() {
        lock (_sync) {
            if (_disposed != 0 || _started) {
                return;
            }

            _started = true;
        }

        IDisposable? subscription = null;
        IDisposable? optionsRegistration = null;
        try {
            subscription = _connectionState.Subscribe(OnConnectionChanged);
            optionsRegistration = _options.OnChange((_, _) => OnOptionsChanged());

            bool publish;
            lock (_sync) {
                publish = _disposed == 0 && _started;
                if (publish) {
                    _subscription = subscription;
                    _optionsChangeRegistration = optionsRegistration;
                }
            }

            if (!publish) {
                Exception? optionsDisposalFailure = DisposeRegistration(optionsRegistration);
                Exception? subscriptionDisposalFailure = DisposeRegistration(subscription);
                Exception? fatalDisposalFailure = optionsDisposalFailure ?? subscriptionDisposalFailure;
                if (fatalDisposalFailure is not null) {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fatalDisposalFailure).Throw();
                }

                return;
            }

            ReconcileCurrentState();
        }
        catch {
            Exception? optionsDisposalFailure = DisposeRegistration(optionsRegistration);
            Exception? subscriptionDisposalFailure = DisposeRegistration(subscription);
            lock (_sync) {
                _started = false;
                _subscription = null;
                _optionsChangeRegistration = null;
            }

            CancelLoop();
            Exception? fatalDisposalFailure = optionsDisposalFailure ?? subscriptionDisposalFailure;
            if (fatalDisposalFailure is not null) {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fatalDisposalFailure).Throw();
            }

            throw;
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
        IDisposable? optionsRegistration;
        Task? loop;
        lock (_sync) {
            _started = false;
            sub = _subscription;
            _subscription = null;
            optionsRegistration = _optionsChangeRegistration;
            _optionsChangeRegistration = null;
            loop = _loopTask;
        }

        Exception? optionsDisposalFailure = DisposeRegistration(optionsRegistration);
        Exception? subscriptionDisposalFailure = DisposeRegistration(sub);
        CancelLoop();

        bool loopCompleted = true;
        if (loop is not null) {
            try {
                await loop.WaitAsync(DisposeWaitTimeout, _timeProvider).ConfigureAwait(false);
            }
            catch (TimeoutException) {
                loopCompleted = false;
                FrontComposerHotPathLog.ProjectionFallbackPollingDisposeTimedOut(
                    _logger,
                    nameof(TimeoutException));
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                // Loop already logs failures; swallow to keep disposal safe.
                FrontComposerHotPathLog.ProjectionFallbackPollingDisposeFailed(
                    _logger,
                    ex.GetType().Name);
            }
        }

        if (loopCompleted) {
            _disposalCts.Dispose();
        }
        else {
            _ = loop!.ContinueWith(
                _ => {
                    _disposalCts.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        Exception? fatalDisposalFailure = optionsDisposalFailure ?? subscriptionDisposalFailure;
        if (fatalDisposalFailure is not null) {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fatalDisposalFailure).Throw();
        }
    }

    private void OnConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0 || !_started) {
            return;
        }

        if (snapshot.IsDisconnected && _options.CurrentValue.ProjectionFallbackPollingIntervalSeconds > 0) {
            EnsureLoopRunning();
        }
        else {
            CancelLoop();
        }
    }

    private void OnOptionsChanged()
        => ReconcileCurrentState();

    private void ReconcileCurrentState() {
        if (_disposed != 0 || !_started) {
            return;
        }

        OnConnectionChanged(_connectionState.Current);
    }

    private void EnsureLoopRunning() {
        lock (_sync) {
            if (_disposed != 0 || !_started || _fatalLoopFault || _loopTask is { IsCompleted: false }) {
                return;
            }

            CancellationTokenSource loopCts = CancellationTokenSource.CreateLinkedTokenSource(_disposalCts.Token);
            TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task loopTask = RunAsync(start.Task, loopCts.Token);
            _loopCts = loopCts;
            _loopTask = loopTask;
            _ = loopTask.ContinueWith(
                completed => OnLoopCompleted(completed, loopCts),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            _ = start.TrySetResult();
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

    private async Task RunAsync(Task start, CancellationToken cancellationToken) {
        await start.ConfigureAwait(false);
        try {
            while (!cancellationToken.IsCancellationRequested) {
                FcShellOptions current = _options.CurrentValue;
                int intervalSeconds = current.ProjectionFallbackPollingIntervalSeconds;
                if (intervalSeconds <= 0 || !_connectionState.Current.IsDisconnected) {
                    return;
                }

                try {
                    _ = await _scheduler.TriggerFallbackOnceAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    FrontComposerLog.ProjectionFallbackPollingIterationFailed(_logger, ex.GetType().Name);
                }

                try {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
            }
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            FrontComposerHotPathLog.ProjectionFallbackPollingTerminated(
                _logger,
                ex.GetType().Name);
        }
    }

    private void OnLoopCompleted(Task completed, CancellationTokenSource loopCts) {
        bool fatal = completed.IsFaulted
            && completed.Exception.Flatten().InnerExceptions.Any(ExceptionGuard.IsFatal);
        bool reconcile = false;
        lock (_sync) {
            if (ReferenceEquals(_loopTask, completed)) {
                _loopTask = null;
                _loopCts = null;
                _fatalLoopFault |= fatal;
                reconcile = _disposed == 0 && _started && !_fatalLoopFault;
            }
        }

        loopCts.Dispose();
        if (reconcile) {
            ReconcileCurrentState();
        }
    }

    private Exception? DisposeRegistration(IDisposable? registration) {
        if (registration is null) {
            return null;
        }

        try {
            registration.Dispose();
            return null;
        }
        catch (Exception ex) {
            if (ExceptionGuard.IsFatal(ex)) {
                return ex;
            }

            FrontComposerHotPathLog.ProjectionFallbackPollingDisposeFailed(
                _logger,
                ex.GetType().Name);
            return null;
        }
    }
}
