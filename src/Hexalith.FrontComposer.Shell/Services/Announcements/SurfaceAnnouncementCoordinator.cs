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

    /// <summary>Restores an already delivered operation when it returns to a vacant surface.</summary>
    internal bool RestoreDelivered(string surface, string group, string identity, string message)
    {
        Action<string>[] handlers;
        Action<string, long>[] versionedHandlers;
        long revision;
        lock (_gate)
        {
            if (_disposed || !_surfaces.TryGetValue(surface, out Surface? owner)
                || !owner.Groups.TryGetValue(group, out Group? state)
                || !state.Delivered.Contains(identity))
            {
                return false;
            }

            state.Cancelled = false;
            state.LastMessage = message;
            state.LastIdentity = identity;
            state.Restorable = !state.Closed;
            if (owner.CurrentGroup is not null)
            {
                return true;
            }

            owner.Message = message;
            owner.CurrentGroup = group;
            owner.CurrentIdentity = identity;
            revision = ++owner.Revision;
            owner.SilentRevisions.Add(revision);
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
        }

        Deliver(surface, handlers, versionedHandlers, message, revision);
        return true;
    }

    internal bool IsSilentRevision(string surface, long revision)
    {
        lock (_gate)
        {
            return _surfaces.TryGetValue(surface, out Surface? owner)
                && (owner.SilentRevisions.Contains(revision) || owner.FreshSilentRevision == revision
                    || owner.PendingFreshSilentRevisions.Contains(revision));
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
            SurfaceAnnouncementDelivery.Run(surface, owner.Revision, IsSilentRevision(surface, owner.Revision),
                () => handler(owner.Message, owner.Revision),
                owner.CurrentGroup is { } current && owner.Groups.TryGetValue(current, out Group? currentState) && currentState.FreshRow);
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
        if (group.StartsWith("fresh-row:", StringComparison.Ordinal))
        {
            PublishFreshRow(surface, group, identity, message);
            return;
        }
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
            KeyValuePair<string, Group>? precedence = NonFreshOwner(owner);
            // An old request's immediate slow notice cannot displace a later
            // decisive outcome. The registration anchor allows a genuinely new
            // request to take the surface after that outcome.
            if (group.StartsWith("slow-query:", StringComparison.Ordinal)
                && precedence is { Key: var currentSlowBarrier, Value: var currentSlowOwner }
                && currentSlowOwner.Closed
                && !SlowRequestStartedAfter(group, currentSlowBarrier, currentSlowOwner.ClosedAt))
            {
                return;
            }
            if (group.StartsWith("stale:", StringComparison.Ordinal) && !state.HasStaleCandidate)
            {
                state.HasStaleCandidate = true;
                state.LastMessage = message;
                state.DeliveryOrder = ++owner.DeliveryOrder;
            }
            // A stale episode has a unique group. Once it spoke, a rerender of that
            // episode must never reclaim a shared surface from a later outcome.
            if (group.StartsWith("stale:", StringComparison.Ordinal) && state.Delivered.Contains(identity))
            {
                return;
            }
            // The current lifecycle state may rerender while a blocked attempt owns the
            // shared node. An already spoken state must remain available for silent
            // restoration without displacing the blocked attempt on that rerender.
            if (group.StartsWith("lifecycle:", StringComparison.Ordinal) && state.Delivered.Contains(identity))
            {
                return;
            }
            if (precedence is { Key: var decisiveGroup, Value: var decisive }
                && decisive.Closed
                && !string.Equals(decisiveGroup, group, StringComparison.Ordinal)
                && (group.StartsWith("limit:", StringComparison.Ordinal)
                    || (group.StartsWith("stale:", StringComparison.Ordinal)
                        && (decisive.LastIdentity?.StartsWith("failure", StringComparison.Ordinal) == true
                            || decisiveGroup.StartsWith("blocked:", StringComparison.Ordinal)))))
            {
                if (group.StartsWith("limit:", StringComparison.Ordinal)
                    && decisive.LastIdentity?.StartsWith("failure", StringComparison.Ordinal) == true)
                {
                    // The cap is still visible, but the page failure has speech priority.
                    // Keep the newest candidate so withdrawing the failure can reveal it.
                    foreach (KeyValuePair<string, Group> prior in owner.Groups)
                    {
                        if (!string.Equals(prior.Key, group, StringComparison.Ordinal)
                            && prior.Key.StartsWith("limit:", StringComparison.Ordinal))
                        {
                            prior.Value.Restorable = false;
                        }
                    }
                    state.LastMessage = message;
                    state.LastIdentity = identity;
                    state.Restorable = true;
                    state.DeliveryOrder = ++owner.DeliveryOrder;
                    state.Closed = true;
                    state.ClosedAt = time.GetUtcNow();
                }
                return;
            }
            // A later owner may queue work while a terminal owner is still mounted.
            // Flush checks that the terminal was withdrawn before publishing it.
            // A delivered A returning while B is queued cancels B without replaying A.
            if (state.Delivered.Contains(identity)
                && (TryParsePageGroup(group, out _, out _)
                    || owner.CurrentGroup is null
                    || (string.Equals(owner.CurrentGroup, group, StringComparison.Ordinal)
                        && string.Equals(owner.CurrentIdentity, identity, StringComparison.Ordinal))
                    || (owner.Groups.TryGetValue(owner.CurrentGroup, out Group? overlay) && overlay.FreshRow
                        && precedence is { Key: var underlying, Value: var underlyingState }
                        && string.Equals(underlying, group, StringComparison.Ordinal)
                        && string.Equals(underlyingState.LastIdentity, identity, StringComparison.Ordinal)
                        && (underlyingState.PendingIdentity is null
                            || string.Equals(underlyingState.PendingIdentity, identity, StringComparison.Ordinal)))))
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
            if (state.Seen.Contains(identity) && !state.Delivered.Contains(identity)
                && (state.PendingIdentity is null || string.Equals(state.PendingIdentity, identity, StringComparison.Ordinal)))
            {
                return;
            }
            if (state.PendingIdentity is { } superseded && !state.Delivered.Contains(superseded))
            {
                state.Seen.Remove(superseded);
            }
            state.Cancelled = false;
            _ = state.Seen.Add(identity);
            if (group.StartsWith("lifecycle:", StringComparison.Ordinal))
            {
                state.Restorable = false;
            }

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
                state.LastMessage = message;
                state.LastIdentity = identity;
                if (group.StartsWith("limit:", StringComparison.Ordinal))
                {
                    foreach (KeyValuePair<string, Group> prior in owner.Groups)
                    {
                        if (!string.Equals(prior.Key, group, StringComparison.Ordinal)
                            && prior.Key.StartsWith("limit:", StringComparison.Ordinal))
                        {
                            prior.Value.Restorable = false;
                        }
                    }
                }
                state.Restorable = group.StartsWith("limit:", StringComparison.Ordinal)
                    || group.StartsWith("lifecycle:", StringComparison.Ordinal) && !terminal;
                state.DeliveryOrder = ++owner.DeliveryOrder;
                state.Closed = terminal;
                state.ClosedAt = terminal ? time.GetUtcNow() : null;
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

        Deliver(surface, handlers, versionedHandlers, message, revision);
    }

    /// <summary>Immediately publishes a transient fresh-row message without changing other groups or timers.</summary>
    internal void PublishFreshRow(string surface, string group, string identity, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Action<string>[] handlers;
        Action<string, long>[] versionedHandlers;
        long revision;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            Surface owner = GetSurface(surface);
            if (owner.Groups.ContainsKey(group))
            {
                return;
            }
            string? underlyingGroup = owner.CurrentGroup is { } current
                && owner.Groups.TryGetValue(current, out Group? overlay) && overlay.FreshRow
                    ? overlay.UnderlyingGroup : NonFreshOwner(owner)?.Key;
            Group state = new()
            {
                FreshRow = true,
                PreviousGroup = owner.CurrentGroup,
                UnderlyingGroup = underlyingGroup,
                LastIdentity = identity,
                LastMessage = message,
                DeliveryOrder = ++owner.DeliveryOrder,
            };
            state.Delivered.Add(identity);
            owner.Groups.Add(group, state);
            owner.Message = message;
            owner.CurrentGroup = group;
            owner.CurrentIdentity = identity;
            revision = ++owner.Revision;
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
        }
        Deliver(surface, handlers, versionedHandlers, message, revision, freshRow: true);
    }

    /// <summary>Withdraws and retires only this fresh-row group, preserving unrelated work and fallback delivery rules.</summary>
    internal void WithdrawFreshRow(string surface, string group)
    {
        Action<string>[] handlers = [];
        Action<string, long>[] versionedHandlers = [];
        string message = string.Empty;
        long revision = 0;
        lock (_gate)
        {
            if (_disposed || !_surfaces.TryGetValue(surface, out Surface? owner)
                || !owner.Groups.TryGetValue(group, out Group? state) || !state.FreshRow)
            {
                return;
            }
            owner.Groups.Remove(group);
            if (!string.Equals(owner.CurrentGroup, group, StringComparison.Ordinal))
            {
                return;
            }
            KeyValuePair<string, Group>? previous = RestorableGroup(owner, state.PreviousGroup);
            previous ??= owner.Groups
                .Where(static pair => pair.Value.FreshRow)
                .OrderByDescending(static pair => pair.Value.DeliveryOrder)
                .Select(static pair => (KeyValuePair<string, Group>?)pair).FirstOrDefault();
            previous ??= RestorableGroup(owner, state.UnderlyingGroup);
            bool silent = true;
            if (previous is not null)
            {
                owner.Message = previous.Value.Value.LastMessage!;
                owner.CurrentGroup = previous.Value.Key;
                owner.CurrentIdentity = previous.Value.Value.LastIdentity ?? (previous.Value.Value.HasStaleCandidate ? "stale" : null);
            }
            else
            {
                silent = RestoreCancellationFallback(owner) || string.IsNullOrEmpty(owner.Message);
            }
            message = owner.Message;
            revision = ++owner.Revision;
            if (silent)
            {
                // Status subscribers capture silence synchronously at delivery. Only the current
                // replay marker is needed after a fresh group and its identity history retire.
                owner.FreshSilentRevision = revision;
                owner.PendingFreshSilentRevisions.Add(revision);
            }
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
        }
        Deliver(surface, handlers, versionedHandlers, message, revision);
    }

    /// <inheritdoc />
    public void Cancel(string surface, string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        if (group.StartsWith("fresh-row:", StringComparison.Ordinal))
        {
            WithdrawFreshRow(surface, group);
            return;
        }
        Action<string>[] handlers = [];
        Action<string, long>[] versionedHandlers = [];
        long revision = 0;
        string message = string.Empty;
        lock (_gate)
        {
            if (_surfaces.TryGetValue(surface, out Surface? owner)
                && owner.Groups.TryGetValue(group, out Group? state))
            {
                state.Cancelled = true;
                ++state.Version;
                state.Timer?.Dispose();
                state.Timer = null;
                if (state.PendingIdentity is { } cancelled && !state.Delivered.Contains(cancelled))
                {
                    state.Seen.Remove(cancelled);
                }
                state.PendingIdentity = null;
                state.Restorable = false;
                if (string.Equals(owner.CurrentGroup, group, StringComparison.Ordinal))
                {
                    bool silent = RestoreCancellationFallback(owner);
                    message = owner.Message;
                    handlers = [.. owner.Handlers];
                    versionedHandlers = [.. owner.VersionedHandlers];
                    revision = ++owner.Revision;
                    if (silent)
                    {
                        owner.SilentRevisions.Add(revision);
                    }
                }
            }
        }

        Deliver(surface, handlers, versionedHandlers, message, revision);
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
            owner.SilentRevisions.Clear();
            owner.FreshSilentRevision = null;
            owner.PendingFreshSilentRevisions.Clear();
            owner.Message = string.Empty;
            owner.CurrentGroup = null;
            owner.CurrentIdentity = null;
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
            revision = ++owner.Revision;
        }

        Deliver(surface, handlers, versionedHandlers, string.Empty, revision);
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

            if (NonFreshOwner(owner) is { Key: var currentGroup, Value: var currentState }
                && currentState.Closed
                && !string.Equals(currentGroup, group, StringComparison.Ordinal)
                && !group.StartsWith("load:", StringComparison.Ordinal)
                && !(currentGroup.StartsWith("blocked:", StringComparison.Ordinal)
                    && !group.StartsWith("stale:", StringComparison.Ordinal))
                && !(group.StartsWith("stale:", StringComparison.Ordinal)
                    && currentState.LastIdentity?.StartsWith("failure", StringComparison.Ordinal) != true
                    && !currentGroup.StartsWith("blocked:", StringComparison.Ordinal)
                    && !currentGroup.StartsWith("limit:", StringComparison.Ordinal))
                && !IsNewPageOperation(currentGroup, group))
            {
                state.Timer?.Dispose();
                state.Timer = null;
                state.PendingIdentity = null;
                state.Seen.Remove(identity);
                return;
            }

            state.Timer?.Dispose();
            state.Timer = null;
            state.PendingIdentity = null;
            state.Delivered.Add(identity);
            state.LastMessage = message;
            state.LastIdentity = identity;
            state.Restorable = group.StartsWith("lifecycle:", StringComparison.Ordinal);
            state.DeliveryOrder = ++owner.DeliveryOrder;
            owner.Message = message;
            owner.CurrentGroup = group;
            owner.CurrentIdentity = identity;
            handlers = [.. owner.Handlers];
            versionedHandlers = [.. owner.VersionedHandlers];
            revision = ++owner.Revision;
        }

        Deliver(surface, handlers, versionedHandlers, message, revision);
    }

    private void Deliver(string surface, Action<string>[] handlers, Action<string, long>[] versionedHandlers, string message, long revision, bool freshRow = false)
    {
        bool silent = IsSilentRevision(surface, revision);
        try
        {
            SurfaceAnnouncementDelivery.Run(surface, revision, silent, () =>
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
            }, freshRow);
        }
        finally
        {
            lock (_gate)
            {
                if (_surfaces.TryGetValue(surface, out Surface? owner))
                {
                    owner.PendingFreshSilentRevisions.Remove(revision);
                }
            }
        }
    }

    private static bool IsRestorable(KeyValuePair<string, Group> candidate)
    {
        Group state = candidate.Value;
        if (state.Cancelled || string.IsNullOrEmpty(state.LastMessage))
        {
            return false;
        }
        if (candidate.Key.StartsWith("limit:", StringComparison.Ordinal)
            || candidate.Key.StartsWith("lifecycle:", StringComparison.Ordinal))
        {
            return state.Restorable;
        }
        return state.FreshRow || state.HasStaleCandidate
            || state.LastIdentity is { } identity && state.Delivered.Contains(identity);
    }

    private static KeyValuePair<string, Group>? RestorableGroup(Surface owner, string? group)
        => group is not null && owner.Groups.TryGetValue(group, out Group? state)
            && IsRestorable(new KeyValuePair<string, Group>(group, state))
                ? new KeyValuePair<string, Group>(group, state) : null;

    private static (KeyValuePair<string, Group>? Lifecycle, KeyValuePair<string, Group>? Stale,
        KeyValuePair<string, Group>? Failure, KeyValuePair<string, Group>? Cap, bool RestoreLifecycle) RestorationCandidates(Surface owner)
    {
        KeyValuePair<string, Group>? lifecycle = owner.Groups
                .Where(static pair => pair.Key.StartsWith("lifecycle:", StringComparison.Ordinal)
                    && !pair.Value.Cancelled && pair.Value.Restorable
                    && !string.IsNullOrEmpty(pair.Value.LastMessage))
                .OrderByDescending(static pair => pair.Value.DeliveryOrder)
                .Select(static pair => (KeyValuePair<string, Group>?)pair)
                .FirstOrDefault();
        KeyValuePair<string, Group>? stale = owner.Groups
            .Where(static pair => pair.Key.StartsWith("stale:", StringComparison.Ordinal)
                && !pair.Value.Cancelled && pair.Value.HasStaleCandidate
                && !string.IsNullOrEmpty(pair.Value.LastMessage))
            .OrderByDescending(static pair => pair.Value.DeliveryOrder)
            .Select(static pair => (KeyValuePair<string, Group>?)pair)
            .FirstOrDefault();
        KeyValuePair<string, Group>? failure = owner.Groups
            .Where(static pair => !pair.Value.Cancelled && pair.Value.Closed
                && pair.Value.LastIdentity?.StartsWith("failure", StringComparison.Ordinal) == true
                && !string.IsNullOrEmpty(pair.Value.LastMessage))
            .OrderByDescending(static pair => pair.Value.DeliveryOrder)
            .Select(static pair => (KeyValuePair<string, Group>?)pair)
            .FirstOrDefault();
        KeyValuePair<string, Group>? cap = owner.Groups
                .Where(static pair => pair.Key.StartsWith("limit:", StringComparison.Ordinal)
                    && !pair.Value.Cancelled && pair.Value.Restorable
                    && !string.IsNullOrEmpty(pair.Value.LastMessage))
                .OrderByDescending(static pair => pair.Value.DeliveryOrder)
                .Select(static pair => (KeyValuePair<string, Group>?)pair)
                .FirstOrDefault();
        bool restoreLifecycle = lifecycle is not null && failure is null
            && (stale is null || lifecycle.Value.Value.DeliveryOrder >= stale.Value.Value.DeliveryOrder)
            && (cap is null || lifecycle.Value.Value.DeliveryOrder >= cap.Value.Value.DeliveryOrder);
        return (lifecycle, stale, failure, cap, restoreLifecycle);
    }

    private static bool RestoreCancellationFallback(Surface owner)
    {
        var (lifecycle, stale, failure, cap, restoreLifecycle) = RestorationCandidates(owner);
        owner.Message = restoreLifecycle ? lifecycle!.Value.Value.LastMessage!
            : failure?.Value.LastMessage ?? stale?.Value.LastMessage ?? cap?.Value.LastMessage ?? string.Empty;
        owner.CurrentGroup = restoreLifecycle ? lifecycle!.Value.Key : failure?.Key ?? stale?.Key ?? cap?.Key;
        owner.CurrentIdentity = restoreLifecycle ? lifecycle!.Value.Value.LastIdentity
            : failure?.Value.LastIdentity ?? (stale is not null ? "stale" : cap?.Value.LastIdentity);
        if (restoreLifecycle)
        {
            // The still-current lifecycle state already spoke before the
            // blocked attempt. Rebind the status text without speaking it again.
            return true;
        }
        if (failure is not null)
        {
            // The mounted page failure already spoke before the slow notice.
            return true;
        }
        if (cap is not null && stale is null)
        {
            // A cap first observed behind the failure speaks here. A cap that
            // already spoke returns as status text without a second utterance.
            return !cap.Value.Value.Delivered.Add(cap.Value.Value.LastIdentity!);
        }
        if (stale is not null)
        {
            Group staleState = stale.Value.Value;
            // Restoring the current candidate completes its pending delivery.
            // Its old trailing callback must neither repeat it nor outlive a
            // later terminal that takes this shared surface.
            ++staleState.Version;
            staleState.Timer?.Dispose();
            staleState.Timer = null;
            staleState.PendingIdentity = null;
            return !staleState.Delivered.Add("stale");
        }
        return false;
    }

    private static KeyValuePair<string, Group>? NonFreshOwner(Surface owner)
    {
        if (owner.CurrentGroup is { } current && owner.Groups.TryGetValue(current, out Group? state))
        {
            if (!state.FreshRow)
            {
                return new KeyValuePair<string, Group>(current, state);
            }
            if (state.UnderlyingGroup is { } underlying && owner.Groups.TryGetValue(underlying, out Group? prior)
                && IsRestorable(new KeyValuePair<string, Group>(underlying, prior)))
            {
                return new KeyValuePair<string, Group>(underlying, prior);
            }
        }
        if (owner.CurrentGroup is not { } fresh || !owner.Groups.TryGetValue(fresh, out Group? overlay) || !overlay.FreshRow)
        {
            return null;
        }
        return owner.Groups.Where(static pair => !pair.Value.FreshRow).Where(IsRestorable)
            .OrderByDescending(static pair => pair.Value.DeliveryOrder)
            .Select(static pair => (KeyValuePair<string, Group>?)pair).FirstOrDefault();
    }

    private static bool IsNewPageOperation(string currentGroup, string group)
        => TryParsePageGroup(currentGroup, out long currentScope, out long currentPage)
            && TryParsePageGroup(group, out long nextScope, out long nextPage)
            && (nextScope > currentScope || (nextScope == currentScope && nextPage > currentPage));

    private static bool SlowRequestStartedAfter(string group, string currentGroup, DateTimeOffset? terminalAt)
    {
        if (TryParsePageGroup(currentGroup, out long terminalScope, out long terminalPage)
            && TryParseSlowRegistration(group, out long requestScope, out long resultAtRegistration))
        {
            return requestScope > terminalScope
                || (requestScope == terminalScope && resultAtRegistration >= terminalPage);
        }

        // A cap closes the completed result. The next request can register in the
        // same clock tick, so compare its observed result sequence before UTC time.
        if (TryParseLimitGroup(currentGroup, out long capScope, out long capResult)
            && TryParseSlowRegistration(group, out long slowScope, out long priorResult))
        {
            return slowScope > capScope || slowScope == capScope && priorResult >= capResult;
        }

        // Standalone cap results can use a nonnumeric identity. Their shared
        // observation order still distinguishes requests registered within one clock tick.
        if (currentGroup.StartsWith("limit:", StringComparison.Ordinal)
            && TryParseObservedOrder(currentGroup, out long capOrder)
            && TryParseObservedOrder(group, out long requestOrder))
        {
            if (TryParseScope(currentGroup, out long currentScope)
                && TryParseSlowRegistration(group, out long pendingScope, out _)
                && pendingScope != currentScope)
            {
                return pendingScope > currentScope;
            }
            return requestOrder > capOrder;
        }

        const string marker = ":started:";
        int start = group.LastIndexOf(marker, StringComparison.Ordinal);
        if (start < 0 || terminalAt is null)
        {
            return false;
        }

        ReadOnlySpan<char> suffix = group.AsSpan(start + marker.Length);
        int end = suffix.IndexOf(':');
        return end > 0
            && long.TryParse(suffix[..end], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out long ticks)
            && ticks > terminalAt.Value.UtcTicks;
    }

    private static bool TryParseObservedOrder(string group, out long order)
    {
        const string marker = ":observed:";
        int start = group.LastIndexOf(marker, StringComparison.Ordinal);
        return long.TryParse(start < 0 ? ReadOnlySpan<char>.Empty : group.AsSpan(start + marker.Length),
            System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out order);
    }

    private static bool TryParseScope(string group, out long scope)
    {
        scope = 0;
        int marker = group.LastIndexOf(":scope:", StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }
        ReadOnlySpan<char> suffix = group.AsSpan(marker + 7);
        int end = suffix.IndexOf(':');
        return end > 0 && long.TryParse(suffix[..end], System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out scope);
    }

    private static bool TryParseSlowRegistration(string group, out long scope, out long resultAtRegistration)
    {
        scope = 0;
        resultAtRegistration = 0;
        int scopeMarker = group.LastIndexOf(":scope:", StringComparison.Ordinal);
        int startMarker = group.LastIndexOf(":started:", StringComparison.Ordinal);
        if (scopeMarker < 0 || startMarker <= scopeMarker + 7
            || !long.TryParse(group.AsSpan(scopeMarker + 7, startMarker - scopeMarker - 7),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out scope))
        {
            return false;
        }

        ReadOnlySpan<char> afterStart = group.AsSpan(startMarker + 9);
        int firstSeparator = afterStart.IndexOf(':');
        if (firstSeparator < 0)
        {
            return false;
        }

        ReadOnlySpan<char> afterTicks = afterStart[(firstSeparator + 1)..];
        int secondSeparator = afterTicks.IndexOf(':');
        return secondSeparator > 0
            && long.TryParse(afterTicks[..secondSeparator], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out resultAtRegistration);
    }

    private static bool TryParseLimitGroup(string group, out long scope, out long result)
    {
        scope = 0;
        result = 0;
        if (!group.StartsWith("limit:", StringComparison.Ordinal))
        {
            return false;
        }
        int scopeMarker = group.LastIndexOf(":scope:", StringComparison.Ordinal);
        if (scopeMarker < 0)
        {
            return false;
        }
        ReadOnlySpan<char> suffix = group.AsSpan(scopeMarker + 7);
        int separator = suffix.IndexOf(':');
        if (separator < 0 || !long.TryParse(suffix[..separator], System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out scope))
        {
            return false;
        }
        suffix = suffix[(separator + 1)..];
        separator = suffix.IndexOf(':');
        return separator > 0 && long.TryParse(suffix[..separator], System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParsePageGroup(string group, out long scope, out long page)
    {
        scope = 0;
        page = 0;
        if (group.StartsWith("page:", StringComparison.Ordinal))
        {
            return long.TryParse(group.AsSpan(5), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out page);
        }

        if (!group.StartsWith("scope:", StringComparison.Ordinal))
        {
            return false;
        }

        int marker = group.IndexOf(":page:", StringComparison.Ordinal);
        return marker > 6
            && long.TryParse(group.AsSpan(6, marker - 6), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out scope)
            && long.TryParse(group.AsSpan(marker + 6), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out page);
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
        public long DeliveryOrder { get; set; }
        public HashSet<long> SilentRevisions { get; } = [];
        public long? FreshSilentRevision { get; set; }
        public HashSet<long> PendingFreshSilentRevisions { get; } = [];
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
        public bool FreshRow { get; set; }
        public string? PreviousGroup { get; set; }
        public string? UnderlyingGroup { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public bool Cancelled { get; set; }
        public bool HasStaleCandidate { get; set; }
        public string? LastMessage { get; set; }
        public string? LastIdentity { get; set; }
        public bool Restorable { get; set; }
        public long DeliveryOrder { get; set; }
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
