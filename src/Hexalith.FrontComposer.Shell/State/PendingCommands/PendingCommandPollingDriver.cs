using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// TimeProvider-driven command-status polling driver. It schedules ticks only; pending-state
/// mutation remains owned by <see cref="IPendingCommandPollingCoordinator"/>.
/// </summary>
public sealed class PendingCommandPollingDriver : IAsyncDisposable {
    private readonly IPendingCommandPollingCoordinator _coordinator;
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PendingCommandPollingDriver> _logger;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly object _sync = new();
    private IDisposable? _optionsChangeRegistration;
    private ITimer? _timer;
    private int _pollInFlight;
    private int _disposed;

    public PendingCommandPollingDriver(
        IPendingCommandPollingCoordinator coordinator,
        IOptionsMonitor<FcShellOptions> options,
        TimeProvider timeProvider,
        ILogger<PendingCommandPollingDriver> logger) {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Starts command-status polling. Idempotent.</summary>
    public void Start() {
        if (_disposed != 0) {
            return;
        }

        lock (_sync) {
            if (_disposed != 0 || _timer is not null) {
                return;
            }

            _timer = _timeProvider.CreateTimer(
                Tick,
                state: null,
                dueTime: Timeout.InfiniteTimeSpan,
                period: Timeout.InfiniteTimeSpan);
            _optionsChangeRegistration = _options.OnChange((_, _) => ApplyInterval());
            ApplyIntervalLocked(_options.CurrentValue.PendingCommandPollingIntervalMs);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return ValueTask.CompletedTask;
        }

        try {
            _disposalCts.Cancel();
        }
        catch (ObjectDisposedException) {
        }

        ITimer? timer;
        IDisposable? optionsRegistration;
        lock (_sync) {
            timer = _timer;
            _timer = null;
            optionsRegistration = _optionsChangeRegistration;
            _optionsChangeRegistration = null;
        }

        optionsRegistration?.Dispose();
        timer?.Dispose();

        _disposalCts.Dispose();
        return ValueTask.CompletedTask;
    }

    private void ApplyInterval() {
        lock (_sync) {
            if (_disposed != 0) {
                return;
            }

            ApplyIntervalLocked(_options.CurrentValue.PendingCommandPollingIntervalMs);
        }
    }

    private void ApplyIntervalLocked(int intervalMs) {
        if (_timer is null) {
            return;
        }

        if (intervalMs <= 0) {
            _ = _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        TimeSpan interval = TimeSpan.FromMilliseconds(intervalMs);
        _ = _timer.Change(interval, interval);
    }

    private void Tick(object? _) {
        if (_disposed != 0 || _options.CurrentValue.PendingCommandPollingIntervalMs <= 0) {
            return;
        }

        if (Interlocked.CompareExchange(ref _pollInFlight, 1, 0) != 0) {
            return;
        }

        _ = PollOnceSafelyAsync();
    }

    private async Task PollOnceSafelyAsync() {
        try {
            _ = await _coordinator.PollOnceAsync(_disposalCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Pending command polling driver tick failed. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
        finally {
            _ = Interlocked.Exchange(ref _pollInFlight, 0);
        }
    }
}
