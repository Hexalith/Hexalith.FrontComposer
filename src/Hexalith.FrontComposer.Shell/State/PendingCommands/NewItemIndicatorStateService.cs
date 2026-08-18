using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <inheritdoc />
public sealed class NewItemIndicatorStateService : INewItemIndicatorStateService
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Dictionary<(string ViewKey, string EntityKey), TrackedEntry> _entries = [];
    private readonly TimeProvider _time;
    private readonly IUserContextAccessor? _userContext;
    private readonly ILogger<NewItemIndicatorStateService> _logger;
    private readonly SnapshotPublisher<IReadOnlyList<string>> _publisher;
    private (string Tenant, string User)? _scopeSnapshot;
    private long _generationCounter;
    private bool _disposed;

    public NewItemIndicatorStateService(TimeProvider? time = null)
        : this(time, userContext: null, logger: null)
    {
    }

    public NewItemIndicatorStateService(
        TimeProvider? time,
        IUserContextAccessor? userContext,
        ILogger<NewItemIndicatorStateService>? logger)
    {
        _time = time ?? TimeProvider.System;
        _userContext = userContext;
        _logger = logger ?? NullLogger<NewItemIndicatorStateService>.Instance;
        _publisher = new SnapshotPublisher<IReadOnlyList<string>>(
            Array.Empty<string>(),
            static _ => { });
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string viewKey, Action handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            if (_disposed)
            {
                return System.Reactive.Disposables.Disposable.Empty;
            }

            return _publisher.Subscribe(
                affectedViewKeys =>
                {
                    for (int index = 0; index < affectedViewKeys.Count; index++)
                    {
                        if (string.Equals(affectedViewKeys[index], viewKey, StringComparison.Ordinal))
                        {
                            handler();
                            return;
                        }
                    }
                },
                replay: false);
        }
    }

    /// <inheritdoc />
    public void Add(NewItemIndicatorEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ViewKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.EntityKey);

        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        bool scopeBoundaryChanged = false;
        bool suppressed = false;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            bool scopeValid = TryReadScope(out (string Tenant, string User)? currentScope);
            if (!scopeValid)
            {
                _ = ApplyScopeBoundaryLocked(
                    currentScope,
                    scopeValid: false,
                    timers,
                    affectedViewKeys,
                    ref scopeBoundaryChanged);
            }
            else
            {
                long generation = Interlocked.Increment(ref _generationCounter);
                ITimer timer = _time.CreateTimer(
                    static state =>
                    {
                        var context = (TimerState)state!;
                        context.Owner.OnTimerFired(
                            context.ViewKey,
                            context.EntityKey,
                            context.Generation);
                    },
                    new TimerState(this, entry.ViewKey, entry.EntityKey, generation),
                    DefaultLifetime,
                    Timeout.InfiniteTimeSpan);

                bool installed = false;
                try
                {
                    _ = ApplyScopeBoundaryLocked(
                        currentScope,
                        scopeValid: true,
                        timers,
                        affectedViewKeys,
                        ref scopeBoundaryChanged);

                    (string ViewKey, string EntityKey) key = (entry.ViewKey, entry.EntityKey);

                    // First-wins decision; see INewItemIndicatorStateService.Add for the contract.
                    installed = _entries.TryAdd(key, new TrackedEntry(entry, timer, generation));
                    suppressed = !installed;
                    if (installed)
                    {
                        _ = affectedViewKeys.Add(entry.ViewKey);
                    }
                }
                finally
                {
                    if (!installed)
                    {
                        DisposeTimer(timer);
                    }
                }
            }
        }

        CompleteMutation(
            timers,
            affectedViewKeys,
            scopeBoundaryChanged,
            scopeBoundaryChanged ? "TenantOrUserTransition" : null);

        if (suppressed)
        {
            FrontComposerHotPathLog.NewItemIndicatorSuppressed(
                _logger,
                entry.MessageId,
                entry.ViewKey,
                entry.EntityKey);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);

        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        bool scopeBoundaryChanged = false;
        IReadOnlyList<NewItemIndicatorEntry> snapshot;
        lock (_gate)
        {
            if (_disposed)
            {
                return [];
            }

            bool scopeValid = TryReadScope(out (string Tenant, string User)? currentScope);
            if (!ApplyScopeBoundaryLocked(
                currentScope,
                scopeValid,
                timers,
                affectedViewKeys,
                ref scopeBoundaryChanged))
            {
                snapshot = [];
            }
            else
            {
                snapshot = [.. _entries.Values
                    .Select(static tracked => tracked.Entry)
                    .Where(entry => string.Equals(entry.ViewKey, viewKey, StringComparison.Ordinal))
                    .OrderBy(static entry => entry.CreatedAt)];
            }
        }

        CompleteMutation(
            timers,
            affectedViewKeys,
            scopeBoundaryChanged,
            scopeBoundaryChanged ? "TenantOrUserTransition" : null);
        return snapshot;
    }

    /// <inheritdoc />
    public void DismissForFilterChange(string viewKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);

        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            foreach (KeyValuePair<(string ViewKey, string EntityKey), TrackedEntry> item in _entries.ToArray())
            {
                if (string.Equals(item.Key.ViewKey, viewKey, StringComparison.Ordinal)
                    && _entries.Remove(item.Key))
                {
                    timers.Add(item.Value.Timer);
                    _ = affectedViewKeys.Add(viewKey);
                }
            }
        }

        CompleteMutation(timers, affectedViewKeys);
    }

    /// <inheritdoc />
    public void DismissMaterialized(string viewKey, string entityKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityKey);

        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (_entries.Remove((viewKey, entityKey), out TrackedEntry? existing))
            {
                timers.Add(existing.Timer);
                _ = affectedViewKeys.Add(viewKey);
            }
        }

        CompleteMutation(timers, affectedViewKeys);
    }

    /// <inheritdoc />
    public void Clear(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            ClearEntriesLocked(timers, affectedViewKeys);
        }

        CompleteMutation(timers, affectedViewKeys, clearReason: reason);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<ITimer> timers = [];
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            timers.AddRange(_entries.Values.Select(static tracked => tracked.Timer));
            _entries.Clear();
            _scopeSnapshot = null;
        }

        foreach (ITimer timer in timers)
        {
            DisposeTimer(timer);
        }
    }

    private bool ApplyScopeBoundaryLocked(
        (string Tenant, string User)? currentScope,
        bool scopeValid,
        List<ITimer> timers,
        HashSet<string> affectedViewKeys,
        ref bool scopeBoundaryChanged)
    {
        if (_userContext is null)
        {
            return true;
        }

        if (!scopeValid || currentScope is null)
        {
            scopeBoundaryChanged |= _scopeSnapshot is not null || _entries.Count > 0;
            _scopeSnapshot = null;
            ClearEntriesLocked(timers, affectedViewKeys);
            return false;
        }

        if (_scopeSnapshot is null)
        {
            _scopeSnapshot = currentScope;
            return true;
        }

        bool changed = !string.Equals(
                _scopeSnapshot.Value.Tenant,
                currentScope.Value.Tenant,
                StringComparison.Ordinal)
            || !string.Equals(
                _scopeSnapshot.Value.User,
                currentScope.Value.User,
                StringComparison.Ordinal);
        if (!changed)
        {
            return true;
        }

        _scopeSnapshot = currentScope;
        scopeBoundaryChanged = true;
        ClearEntriesLocked(timers, affectedViewKeys);
        return true;
    }

    private void ClearEntriesLocked(List<ITimer> timers, HashSet<string> affectedViewKeys)
    {
        foreach (TrackedEntry tracked in _entries.Values)
        {
            timers.Add(tracked.Timer);
            _ = affectedViewKeys.Add(tracked.Entry.ViewKey);
        }

        _entries.Clear();
    }

    private void CompleteMutation(
        List<ITimer> timers,
        HashSet<string> affectedViewKeys,
        bool scopeBoundaryChanged = false,
        string? clearReason = null)
    {
        foreach (ITimer timer in timers)
        {
            DisposeTimer(timer);
        }

        if (scopeBoundaryChanged)
        {
            FrontComposerHotPathLog.NewItemScopeTransition(_logger);
        }

        if (clearReason is not null)
        {
            FrontComposerHotPathLog.NewItemStateCleared(_logger, clearReason);
        }

        if (affectedViewKeys.Count > 0)
        {
            _publisher.Publish([.. affectedViewKeys]);
        }
    }

    private void OnTimerFired(string viewKey, string entityKey, long generation)
    {
        List<ITimer> timers = [];
        HashSet<string> affectedViewKeys = new(StringComparer.Ordinal);
        lock (_gate)
        {
            if (_disposed
                || !_entries.TryGetValue((viewKey, entityKey), out TrackedEntry? tracked)
                || tracked.Generation != generation)
            {
                return;
            }

            _ = _entries.Remove((viewKey, entityKey));
            timers.Add(tracked.Timer);
            _ = affectedViewKeys.Add(viewKey);
        }

        CompleteMutation(timers, affectedViewKeys);
    }

    private bool TryReadScope(out (string Tenant, string User)? scope)
    {
        scope = null;
        if (_userContext is null)
        {
            return true;
        }

        try
        {
            string? tenant = _userContext.TenantId;
            string? user = _userContext.UserId;
            if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(user))
            {
                return false;
            }

            scope = (tenant, user);
            return true;
        }
        catch (Exception exception) when (!ExceptionGuard.IsFatal(exception))
        {
            return false;
        }
    }

    private static void DisposeTimer(ITimer timer)
    {
        try
        {
            timer.Dispose();
        }
        catch (Exception exception) when (!ExceptionGuard.IsFatal(exception))
        {
            // State mutation has already committed; a nonfatal cleanup fault must not suppress
            // remaining timer disposal or the mutation's required logging and notification.
        }
    }

    private sealed record TrackedEntry(NewItemIndicatorEntry Entry, ITimer Timer, long Generation);

    private sealed record TimerState(
        NewItemIndicatorStateService Owner,
        string ViewKey,
        string EntityKey,
        long Generation);
}
