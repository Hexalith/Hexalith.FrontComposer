namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Owns the single polite announcement stream for each visible surface.</summary>
public interface ISurfaceAnnouncementCoordinator
{
    /// <summary>Gets the last spoken message for a surface.</summary>
    string Current(string surface);

    /// <summary>Subscribes to spoken messages for a surface.</summary>
    IDisposable Subscribe(string surface, Action<string> handler);

    /// <summary>Atomically subscribes and replays the current revision to a status owner.</summary>
    IDisposable SubscribeWithReplay(string surface, Action<string, long> handler);

    /// <summary>Queues an intermediate message or publishes a terminal message immediately.</summary>
    /// <remarks>
    /// Groups prefixed with <c>fresh-row:</c> are reserved for framework fresh-row announcements. The built-in
    /// <see cref="SurfaceAnnouncementCoordinator"/> publishes these groups immediately regardless of
    /// <paramref name="terminal"/> and <paramref name="immediate"/>, ignoring repeats while the group exists.
    /// Publication overlays the current status without changing other groups or their timers, and the underlying
    /// owner keeps its precedence. Adopter-supplied coordinators retain their ordinary announcement behavior unless
    /// they implement this reserved family.
    /// </remarks>
    void Announce(string surface, string group, string identity, string message, bool terminal = false, bool immediate = false);

    /// <summary>Cancels an unspoken intermediate message for a group.</summary>
    /// <remarks>
    /// The built-in <see cref="SurfaceAnnouncementCoordinator"/> withdraws and retires a <c>fresh-row:</c> group.
    /// When it is current, a retained eligible status or another fresh-row overlay returns without speech. Otherwise,
    /// the ordinary cancellation fallback may deliver a newly eligible candidate or clear the status when none remains.
    /// Adopter-supplied coordinators retain their ordinary cancellation behavior unless they implement this reserved family.
    /// </remarks>
    void Cancel(string surface, string group);

    /// <summary>Clears every announcement and dedupe key after a scope boundary.</summary>
    void Clear(string surface);
}
