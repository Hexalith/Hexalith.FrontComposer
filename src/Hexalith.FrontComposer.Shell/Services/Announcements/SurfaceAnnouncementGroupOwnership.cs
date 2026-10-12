using System.Runtime.CompilerServices;

namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Shares a projection announcement group among mounted producers of one surface.</summary>
public sealed class SurfaceAnnouncementGroupOwnership
{
    private static readonly ConditionalWeakTable<ISurfaceAnnouncementCoordinator, SurfaceAnnouncementGroupOwnership> Owners = new();
    private readonly ISurfaceAnnouncementCoordinator _announcements;
    private readonly object _gate = new();
    private readonly Dictionary<(string Surface, string Group), int> _claims = [];
    private readonly Dictionary<(string Surface, string Key), string> _activeEpisodes = [];
    private readonly Dictionary<(string Surface, string Group), string> _episodeKeysByGroup = [];
    private readonly Dictionary<string, long> _nextEpisodeBySurface = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Surface, string Family), (string Key, string Group, bool Ended)> _transitions = [];
    private readonly Dictionary<(string Surface, object Request), long> _requestOrders = [];
    private readonly Queue<(string Surface, object Request)> _requestOrderQueue = [];
    private readonly Dictionary<string, long> _nextOrderBySurface = new(StringComparer.Ordinal);

    private SurfaceAnnouncementGroupOwnership(ISurfaceAnnouncementCoordinator announcements)
    {
        _announcements = announcements;
    }

    /// <summary>Gets the ownership registry associated with a scoped announcement coordinator.</summary>
    public static SurfaceAnnouncementGroupOwnership For(ISurfaceAnnouncementCoordinator announcements)
        => Owners.GetValue(announcements, static coordinator => new SurfaceAnnouncementGroupOwnership(coordinator));

    /// <summary>Claims a group until the returned lease is disposed.</summary>
    public IDisposable Claim(string surface, string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        lock (_gate)
        {
            (string Surface, string Group) key = (surface, group);
            _claims[key] = _claims.GetValueOrDefault(key) + 1;
        }
        return new SurfaceAnnouncementGroupLease(this, surface, group);
    }

    /// <summary>Shares an active event among mounted owners and gives a later event a fresh group.</summary>
    internal IDisposable ClaimEpisode(string surface, string key, out string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surface);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            (string Surface, string Key) episodeKey = (surface, key);
            if (!_activeEpisodes.TryGetValue(episodeKey, out group!))
            {
                long sequence = _nextEpisodeBySurface.GetValueOrDefault(surface) + 1;
                _nextEpisodeBySurface[surface] = sequence;
                group = key + ":episode:" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _activeEpisodes[episodeKey] = group;
                _episodeKeysByGroup[(surface, group)] = key;
            }
            (string Surface, string Group) claimKey = (surface, group);
            _claims[claimKey] = _claims.GetValueOrDefault(claimKey) + 1;
        }
        return new SurfaceAnnouncementGroupLease(this, surface, group);
    }

    /// <summary>Shares a visible transition among mounted owners while recognizing a later return to the same value.</summary>
    internal IDisposable ClaimTransitionEpisode(string surface, string family, string key, out string group)
    {
        lock (_gate)
        {
            (string Surface, string Family) transitionKey = (surface, family);
            if (!_transitions.TryGetValue(transitionKey, out var current)
                || current.Ended || !string.Equals(current.Key, key, StringComparison.Ordinal))
            {
                long sequence = _nextEpisodeBySurface.GetValueOrDefault(surface) + 1;
                _nextEpisodeBySurface[surface] = sequence;
                long order = _nextOrderBySurface.GetValueOrDefault(surface) + 1;
                _nextOrderBySurface[surface] = order;
                current = (key, key + ":episode:" + sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ":observed:" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), false);
                _transitions[transitionKey] = current;
            }

            group = current.Group;
            (string Surface, string Group) claimKey = (surface, group);
            _claims[claimKey] = _claims.GetValueOrDefault(claimKey) + 1;
        }

        return new SurfaceAnnouncementGroupLease(this, surface, group);
    }

    /// <summary>Ends a visible episode only after its final mounted claimant hides it.</summary>
    internal void EndTransitionEpisode(string surface, string family, string group)
    {
        lock (_gate)
        {
            (string Surface, string Family) key = (surface, family);
            if (_transitions.TryGetValue(key, out var current)
                && string.Equals(current.Group, group, StringComparison.Ordinal)
                && !_claims.ContainsKey((surface, group)))
            {
                _transitions[key] = (current.Key, current.Group, true);
            }
        }
    }

    /// <summary>Records the order in which an active request became visible to the shared surface.</summary>
    internal long ObserveRequest(string surface, object request)
    {
        lock (_gate)
        {
            (string Surface, object Request) key = (surface, request);
            if (!_requestOrders.TryGetValue(key, out long order))
            {
                order = _nextOrderBySurface.GetValueOrDefault(surface) + 1;
                _nextOrderBySurface[surface] = order;
                _requestOrders[key] = order;
                _requestOrderQueue.Enqueue(key);
                // Only settled requests may leave this order index. A pending request
                // can outlive any number of later requests and still precede a cap.
                if (_requestOrders.Count > 512)
                {
                    int count = _requestOrderQueue.Count;
                    for (int index = 0; index < count; index++)
                    {
                        (string Surface, object Request) oldest = _requestOrderQueue.Dequeue();
                        if (oldest.Request is TaskCompletionSource<object> completion && completion.Task.IsCompleted)
                        {
                            _requestOrders.Remove(oldest);
                        }
                        else
                        {
                            _requestOrderQueue.Enqueue(oldest);
                        }
                    }
                }
            }
            return order;
        }
    }

    internal void Release(string surface, string group)
    {
        lock (_gate)
        {
            (string Surface, string Group) key = (surface, group);
            if (!_claims.TryGetValue(key, out int count))
            {
                return;
            }
            if (count == 1)
            {
                _claims.Remove(key);
                if (_episodeKeysByGroup.Remove(key, out string? episodeKey))
                {
                    _activeEpisodes.Remove((surface, episodeKey));
                }
                _announcements.Cancel(surface, group);
            }
            else
            {
                _claims[key] = count - 1;
            }
        }
    }
}
