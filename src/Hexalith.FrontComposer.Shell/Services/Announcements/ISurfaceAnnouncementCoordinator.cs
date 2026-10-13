namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Owns the single polite announcement stream for each visible surface.</summary>
public interface ISurfaceAnnouncementCoordinator
{
    /// <summary>Gets the last spoken message for a surface.</summary>
    string Current(string surface);

    /// <summary>Gets whether the current speech belongs to the specified event identity.</summary>
    bool IsCurrent(string surface, string group, string identity);

    /// <summary>Subscribes to spoken messages for a surface.</summary>
    IDisposable Subscribe(string surface, Action<string> handler);

    /// <summary>Atomically subscribes and replays the current revision to a status owner.</summary>
    IDisposable SubscribeWithReplay(string surface, Action<string, long> handler);

    /// <summary>Queues an intermediate message or publishes a terminal message immediately.</summary>
    /// <remarks>
    /// Groups prefixed with <c>fresh-row:</c> are reserved for fresh-row announcements. Such a group is published
    /// immediately, once, whatever <paramref name="terminal"/> and <paramref name="immediate"/> say; a repeated
    /// announcement for an existing group is ignored. It overlays the current status without changing other groups or
    /// their timers, and the underlying owner keeps its precedence, so messages that owner would have suppressed stay
    /// silent while the overlay is current.
    /// </remarks>
    void Announce(string surface, string group, string identity, string message, bool terminal = false, bool immediate = false);

    /// <summary>Cancels an unspoken intermediate message for a group.</summary>
    /// <remarks>
    /// Cancelling a <c>fresh-row:</c> group withdraws only that overlay. When it is current, the status it covered is
    /// restored without speech, or the status is cleared when nothing restorable remains.
    /// </remarks>
    void Cancel(string surface, string group);

    /// <summary>Clears every announcement and dedupe key after a scope boundary.</summary>
    void Clear(string surface);
}
