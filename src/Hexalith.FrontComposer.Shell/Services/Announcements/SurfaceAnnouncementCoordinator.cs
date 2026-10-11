using Hexalith.FrontComposer.Shell.Services;

namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Scoped, fake-time capable coalescing and deduplication for polite speech.</summary>
public sealed class SurfaceAnnouncementCoordinator(TimeProvider time) : ISurfaceAnnouncementCoordinator, IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(250);
    private readonly object _gate = new();
    private readonly Dictionary<string, Surface> _surfaces = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <inheritdoc />
    public string Current(string surface)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        lock (_gate)
        {
            return _surfaces.TryGetValue(surface, out Surface? owner) ? owner.Message : string.Empty;
        }
    }

    /// <inheritdoc />
    public bool IsCurrent(string surface, string group, string identity)
    {
        lock (_gate)
        {
            return _surfaces.TryGetValue(surface, out Surface? owner)
                && string.Equals(owner.CurrentGroup, group, StringComparison.Ordinal)
                && string.Equals(owner.CurrentIdentity, identity, StringComparison.Ordinal);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string surface, Action<string> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Surface owner = GetSurface(surface);
            owner.Handlers.Add(handler);
            return new Subscription(this, surface, handler);
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeWithReplay(string surface, Action<string, long> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Surface owner = GetSurface(surface);
            owner.VersionedHandlers.Add(handler);
            handler(owner.Message, owner.Revision);
            return new VersionedSubscription(this, surface, handler);
        }
    }

    /// <inheritdoc />
    public void Announce(string surface, string group, string identity, string message, bool terminal = false, bool immediate = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Action<string>[] handlers = [];
        Action<string, long>[] versionedHandlers = [];
        long revision = 0;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            Surface owner = GetSurface(surface);
            Group state = owner.Groups.TryGetValue(group, out Group? existing)
                ? existing
                : owner.Groups[group] = new Group();
            if (state.Closed)
            {
                return;
            }
            // A delivered A returning while B is queued cancels B without replaying A.
            if (state.Delivered.Contains(identity))
            {
                ++state.Version;
                state.Timer?.Dispose();
                state.Timer = null;
                if (state.PendingIdentity is { } cancelled && !state.Delivered.Contains(cancelled))
                {
                    state.Seen.Remove(cancelled);
                }
                state.PendingIdentity = null;
                return;
            }
            // A repeated render is silent, but an A -> B -> A burst must end in A.
            // The earlier A was superseded before it was delivered.
            if (state.Seen.Contains(identity)
                && (state.PendingIdentity is null || string.Equals(state.PendingIdentity, identity, StringComparison.Ordinal)))
            {
                return;
            }
            if (state.PendingIdentity is { } superseded && !state.Delivered.Contains(superseded))
            {
                state.Seen.Remove(superseded);
            }
            _ = state.Seen.Add(identity);

            state.Timer?.Dispose();
            state.Timer = null;
            state.PendingIdentity = terminal || immediate ? null : identity;
            long version = ++state.Version;
            if (terminal || immediate)
            {
                // A decisive outcome owns the surface. Pending messages from other groups
                // must not replace it when their trailing windows expire.
                foreach (Group pending in owner.Groups.Values)
                {
                    if (!ReferenceEquals(pending, state))
                    {
                        ++pending.Version;
                        pending.Timer?.Dispose();
                        pending.Timer = null;
                        if (pending.PendingIdentity is { } cancelled && !pending.Delivered.Contains(cancelled))
                        {
                            pending.Seen.Remove(cancelled);
                        }
                        pending.PendingIdentity = null;
                    }
                }
                state.Delivered.Add(identity);
                state.Closed = terminal;
                owner.Message = message;
                owner.CurrentGroup = group;
                owner.CurrentIdentity = identity;
                handlers = [.. owner.Handlers];
                versionedHandlers = [.. owner.VersionedHandlers];
                revision = ++owner.Revision;
            }
            else
            {
                state.Timer = time.CreateTimer(
                    _ => Flush(surface, group, state, identity, version, message),
                    null,
                    Window,
                    Timeout.InfiniteTimeSpan);
            }
        }

        Deliver(handlers, versionedHandlers, message, revision);
    }

    /// <inheritdoc />
    public void Cancel(string surface, string group)
    {
        Action<string>[] handlers = [];
        Action<string, long>[] versionedHandlers = [];
        long revision = 0;
        lock (_gate)
        {
            if (_surfaces.TryGetValue(surface, out Surface? owner)
                && owner.Groups.TryGetValue(group, out Group? state))
            {
                ++state.Version;
                state.Timer?.Dispose();
                state.Timer = null;
                if (state.PendingIdentity is { } cancelled && !state.Delivered.Contains(cancelled))
                {
                    state.Seen.Remove(cancelled);
                }
                state.PendingIdentity = null;
                if (string.Equals(owner.CurrentGroup, group, StringComparison.Ordinal))
                {
                    owner.Message = string.Empty;
                    owner.CurrentGroup = null;
                    owner.CurrentIdentity = null;
                    handlers = [.. owner.Handlers];
                    versionedHandlers = [.. owner.VersionedHandlers];
                    revision = ++owner.Revision;
                }
            }
        }

        Deliver(handlers, versionedHandlers, string.Empty, revision);
    }

    /// <inheritdoc />
    public void Clear(string surface)
    {
        Action<string>[] handlers;
        Action<string, long>[] versionedHandlers;
        long revision;
        lock (_gate)
        {
            if (!_surfaces.TryGetValue(surface, out Surface? owner))
            {
                return;
            }

            foreach (Group group in owner.Groups.Values)
            {
                group.Timer?.Dispose();
                ++group.Version;
            }

            owner.Groups.Clear();
            owner.Message = string.Empty;
            owner.CurrentGroup = null;
            owner.CurrentIdentity = null;
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
            revision = ++owner.Revision;
        }

        Deliver(handlers, versionedHandlers, string.Empty, revision);
    }

    private void Flush(string surface, string group, Group captured, string identity, long version, string message)
    {
        Action<string>[] handlers;
        Action<string, long>[] versionedHandlers;
        long revision;
        lock (_gate)
        {
            if (_disposed || !_surfaces.TryGetValue(surface, out Surface? owner)
                || !owner.Groups.TryGetValue(group, out Group? state) || !ReferenceEquals(state, captured) || state.Version != version)
            {
                return;
            }

            state.Timer?.Dispose();
            state.Timer = null;
            state.PendingIdentity = null;
            state.Delivered.Add(identity);
            owner.Message = message;
            owner.CurrentGroup = group;
            owner.CurrentIdentity = identity;
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
            revision = ++owner.Revision;
        }

        Deliver(handlers, versionedHandlers, message, revision);
    }

    private static void Deliver(Action<string>[] handlers, Action<string, long>[] versionedHandlers, string message, long revision)
    {
        foreach (Action<string> handler in handlers)
        {
            try
            {
                handler(message);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
            {
                // One removed or failing component cannot suppress the other listeners.
            }
        }
        foreach (Action<string, long> handler in versionedHandlers)
        {
            try
            {
                handler(message, revision);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
            {
                // A disposed subscriber cannot suppress other listeners.
            }
        }
    }

    private Surface GetSurface(string surface)
        => _surfaces.TryGetValue(surface, out Surface? existing)
            ? existing
            : _surfaces[surface] = new Surface();

    private void Unsubscribe(string surface, Action<string> handler)
    {
        lock (_gate)
        {
            if (_surfaces.TryGetValue(surface, out Surface? owner))
            {
                _ = owner.Handlers.Remove(handler);
            }
        }
    }

    private void UnsubscribeVersioned(string surface, Action<string, long> handler)
    {
        lock (_gate)
        {
            if (_surfaces.TryGetValue(surface, out Surface? owner))
            {
                _ = owner.VersionedHandlers.Remove(handler);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (Surface surface in _surfaces.Values)
            {
                foreach (Group group in surface.Groups.Values)
                {
                    group.Timer?.Dispose();
                }
            }

            _surfaces.Clear();
        }
    }

    private sealed class Surface
    {
        public string Message { get; set; } = string.Empty;
        public string? CurrentGroup { get; set; }
        public string? CurrentIdentity { get; set; }
        public List<Action<string>> Handlers { get; } = [];
        public List<Action<string, long>> VersionedHandlers { get; } = [];
        public long Revision { get; set; }
        public Dictionary<string, Group> Groups { get; } = new(StringComparer.Ordinal);
    }

    private sealed class Group
    {
        public HashSet<string> Seen { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Delivered { get; } = new(StringComparer.Ordinal);
        public string? PendingIdentity { get; set; }
        public ITimer? Timer { get; set; }
        public long Version { get; set; }
        public bool Closed { get; set; }
    }

    private sealed class Subscription(SurfaceAnnouncementCoordinator owner, string surface, Action<string> handler) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Unsubscribe(surface, handler);
            }
        }
    }

    private sealed class VersionedSubscription(SurfaceAnnouncementCoordinator owner, string surface, Action<string, long> handler) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.UnsubscribeVersioned(surface, handler);
            }
        }
    }
}
