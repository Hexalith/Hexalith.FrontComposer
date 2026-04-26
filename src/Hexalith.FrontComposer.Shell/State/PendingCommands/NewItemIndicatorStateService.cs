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
}

/// <inheritdoc />
public sealed class NewItemIndicatorStateService : INewItemIndicatorStateService {
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Dictionary<(string ViewKey, string EntityKey), (NewItemIndicatorEntry Entry, ITimer Timer)> _entries = [];
    private readonly TimeProvider _time;
    private bool _disposed;

    public NewItemIndicatorStateService(TimeProvider? time = null) {
        _time = time ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public void Add(NewItemIndicatorEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        ITimer? previous = null;
        lock (_gate) {
            ThrowIfDisposed();
            (string ViewKey, string EntityKey) key = (entry.ViewKey, entry.EntityKey);
            if (_entries.Remove(key, out (NewItemIndicatorEntry Entry, ITimer Timer) existing)) {
                previous = existing.Timer;
            }

            ITimer timer = _time.CreateTimer(
                static state => {
                    (NewItemIndicatorStateService Owner, string ViewKey, string EntityKey) ctx =
                        ((NewItemIndicatorStateService, string, string))state!;
                    ctx.Owner.DismissMaterialized(ctx.ViewKey, ctx.EntityKey);
                },
                (this, entry.ViewKey, entry.EntityKey),
                DefaultLifetime,
                Timeout.InfiniteTimeSpan);

            _entries[key] = (entry, timer);
        }

        previous?.Dispose();
    }

    /// <inheritdoc />
    public IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey) {
        lock (_gate) {
            return [.. _entries.Values
                .Select(static x => x.Entry)
                .Where(entry => string.Equals(entry.ViewKey, viewKey, StringComparison.Ordinal))
                .OrderBy(static entry => entry.CreatedAt)];
        }
    }

    /// <inheritdoc />
    public void DismissForFilterChange(string viewKey) {
        List<ITimer> timers = [];
        lock (_gate) {
            foreach (KeyValuePair<(string ViewKey, string EntityKey), (NewItemIndicatorEntry Entry, ITimer Timer)> item in _entries.ToArray()) {
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
        ITimer? timer = null;
        lock (_gate) {
            if (_entries.Remove((viewKey, entityKey), out (NewItemIndicatorEntry Entry, ITimer Timer) existing)) {
                timer = existing.Timer;
            }
        }

        timer?.Dispose();
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

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(NewItemIndicatorStateService));
        }
    }
}
