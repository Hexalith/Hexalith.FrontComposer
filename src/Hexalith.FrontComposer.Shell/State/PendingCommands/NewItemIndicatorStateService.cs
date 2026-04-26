using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Transient indicator for a confirmed created entity that is relevant to a lane but outside current filters.</summary>
public sealed record NewItemIndicatorEntry(
    string ViewKey,
    string EntityKey,
    string MessageId,
    DateTimeOffset CreatedAt);

/// <summary>Circuit-local state for Story 5-5 new-item indicators.</summary>
public interface INewItemIndicatorStateService : IDisposable {
    void Add(NewItemIndicatorEntry entry);

    IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey);

    void DismissForFilterChange(string viewKey);

    void DismissMaterialized(string viewKey, string entityKey);

    void Clear(string reason);
}

/// <inheritdoc />
public sealed class NewItemIndicatorStateService : INewItemIndicatorStateService {
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Dictionary<(string ViewKey, string EntityKey), TrackedEntry> _entries = [];
    private readonly TimeProvider _time;
    private readonly IUserContextAccessor? _userContext;
    private readonly ILogger<NewItemIndicatorStateService> _logger;
    private (string? Tenant, string? User)? _scopeSnapshot;
    private long _generationCounter;
    private bool _disposed;

    public NewItemIndicatorStateService(TimeProvider? time = null)
        : this(time, userContext: null, logger: null) {
    }

    public NewItemIndicatorStateService(
        TimeProvider? time,
        IUserContextAccessor? userContext,
        ILogger<NewItemIndicatorStateService>? logger) {
        _time = time ?? TimeProvider.System;
        _userContext = userContext;
        _logger = logger ?? NullLogger<NewItemIndicatorStateService>.Instance;
    }

    /// <inheritdoc />
    public void Add(NewItemIndicatorEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        // P15 — fail-closed at the boundary; an empty key would silently cross-contaminate
        // DismissForFilterChange/Snapshot calls that filter on viewKey/entityKey.
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ViewKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.EntityKey);

        EnforceScopeBoundary();

        // P1 — generation token guards against a stale timer callback dismissing a freshly
        // re-added entry. The new tracked entry stamps a fresh generation; the timer callback
        // below ignores fires whose generation no longer matches the stored value.
        long generation = Interlocked.Increment(ref _generationCounter);

        // P2-P15 — create the timer first so the TrackedEntry is constructed in one shot,
        // avoiding the transient `Timer: null!` placeholder that the previous code held while a
        // concurrent `Snapshot()` call could observe the dictionary mid-write. The DefaultLifetime
        // dueTime ensures no callback fires before the entry is stored under the gate.
        ITimer timer = _time.CreateTimer(
            static state => {
                TimerState ctx = (TimerState)state!;
                ctx.Owner.OnTimerFired(ctx.ViewKey, ctx.EntityKey, ctx.Generation);
            },
            new TimerState(this, entry.ViewKey, entry.EntityKey, generation),
            DefaultLifetime,
            Timeout.InfiniteTimeSpan);

        ITimer? previous = null;
        bool installed = false;
        try {
            lock (_gate) {
                // P2-P17 — re-check _disposed after creating the timer so a Dispose() that ran
                // between EnforceScopeBoundary and lock acquisition does not leave the new timer
                // outliving the service. Disposed dictionary will not receive the new entry.
                if (_disposed) {
                    return;
                }

                (string ViewKey, string EntityKey) key = (entry.ViewKey, entry.EntityKey);
                if (_entries.Remove(key, out TrackedEntry? existing)) {
                    previous = existing.Timer;
                }

                _entries[key] = new TrackedEntry(entry, timer, generation);
                installed = true;
            }
        }
        finally {
            if (!installed) {
                // Service was disposed between timer creation and lock — clean up to prevent leak.
                timer.Dispose();
            }
        }

        previous?.Dispose();
    }

    /// <inheritdoc />
    public IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);

        lock (_gate) {
            if (_disposed) {
                return [];
            }

            return [.. _entries.Values
                .Select(static x => x.Entry)
                .Where(entry => string.Equals(entry.ViewKey, viewKey, StringComparison.Ordinal))
                .OrderBy(static entry => entry.CreatedAt)];
        }
    }

    /// <inheritdoc />
    public void DismissForFilterChange(string viewKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);

        List<ITimer> timers = [];
        lock (_gate) {
            if (_disposed) {
                return;
            }

            foreach (KeyValuePair<(string ViewKey, string EntityKey), TrackedEntry> item in _entries.ToArray()) {
                if (string.Equals(item.Key.ViewKey, viewKey, StringComparison.Ordinal) && _entries.Remove(item.Key)) {
                    timers.Add(item.Value.Timer);
                }
            }
        }

        foreach (ITimer timer in timers) {
            timer.Dispose();
        }
    }

    /// <inheritdoc />
    public void DismissMaterialized(string viewKey, string entityKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityKey);

        ITimer? timer = null;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            if (_entries.Remove((viewKey, entityKey), out TrackedEntry? existing)) {
                timer = existing.Timer;
            }
        }

        timer?.Dispose();
    }

    /// <inheritdoc />
    public void Clear(string reason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        List<ITimer> timers;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            timers = [.. _entries.Values.Select(static x => x.Timer)];
            _entries.Clear();
        }

        foreach (ITimer timer in timers) {
            timer.Dispose();
        }

        _logger.LogInformation("New-item indicator state cleared. Reason={Reason}", reason);
    }

    /// <inheritdoc />
    public void Dispose() {
        List<ITimer> timers;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            timers = [.. _entries.Values.Select(static x => x.Timer)];
            _entries.Clear();
        }

        foreach (ITimer timer in timers) {
            timer.Dispose();
        }
    }

    private void OnTimerFired(string viewKey, string entityKey, long generation) {
        ITimer? timer = null;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            if (!_entries.TryGetValue((viewKey, entityKey), out TrackedEntry? tracked)) {
                return;
            }

            if (tracked.Generation != generation) {
                // P1 — a newer Add() reused the same key; the new entry owns its own timer.
                return;
            }

            _entries.Remove((viewKey, entityKey));
            timer = tracked.Timer;
        }

        timer?.Dispose();
    }

    private void EnforceScopeBoundary() {
        if (_userContext is null) {
            return;
        }

        (string? Tenant, string? User) current = (_userContext.TenantId, _userContext.UserId);
        bool needsClear;
        lock (_gate) {
            if (_scopeSnapshot is null) {
                _scopeSnapshot = current;
                return;
            }

            needsClear = !string.Equals(_scopeSnapshot.Value.Tenant, current.Tenant, StringComparison.Ordinal)
                || !string.Equals(_scopeSnapshot.Value.User, current.User, StringComparison.Ordinal);
            if (needsClear) {
                _scopeSnapshot = current;
            }
        }

        if (needsClear) {
            _logger.LogWarning("New-item indicator tenant/user transition detected; flushing state.");
            Clear("TenantOrUserTransition");
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(NewItemIndicatorStateService));
        }
    }

    private sealed record TrackedEntry(NewItemIndicatorEntry Entry, ITimer Timer, long Generation);

    private sealed record TimerState(NewItemIndicatorStateService Owner, string ViewKey, string EntityKey, long Generation);
}
