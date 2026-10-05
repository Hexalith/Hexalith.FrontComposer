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
    void Announce(string surface, string group, string identity, string message, bool terminal = false, bool immediate = false);

    /// <summary>Cancels an unspoken intermediate message for a group.</summary>
    void Cancel(string surface, string group);

    /// <summary>Clears every announcement and dedupe key after a scope boundary.</summary>
    void Clear(string surface);
}
