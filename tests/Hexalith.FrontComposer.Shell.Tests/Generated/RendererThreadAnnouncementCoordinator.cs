using Hexalith.FrontComposer.Shell.Services.Announcements;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Records the synchronization context of generated announcement calls.</summary>
internal sealed class RendererThreadAnnouncementCoordinator(ISurfaceAnnouncementCoordinator inner) : ISurfaceAnnouncementCoordinator, IDisposable
{
    public List<SynchronizationContext?> Contexts { get; } = [];

    public int AnnounceCount { get; private set; }

    public int CancelCount { get; private set; }

    public void Reset()
    {
        Contexts.Clear();
        AnnounceCount = 0;
        CancelCount = 0;
    }

    public string Current(string surface) => inner.Current(surface);

    public bool IsCurrent(string surface, string group, string identity) => inner.IsCurrent(surface, group, identity);

    public IDisposable Subscribe(string surface, Action<string> handler) => inner.Subscribe(surface, handler);

    public IDisposable SubscribeWithReplay(string surface, Action<string, long> handler) => inner.SubscribeWithReplay(surface, handler);

    public void Announce(string surface, string group, string identity, string message, bool terminal = false, bool immediate = false)
    {
        Contexts.Add(SynchronizationContext.Current);
        AnnounceCount++;
        inner.Announce(surface, group, identity, message, terminal, immediate);
    }

    public void Cancel(string surface, string group)
    {
        Contexts.Add(SynchronizationContext.Current);
        CancelCount++;
        inner.Cancel(surface, group);
    }

    public void Clear(string surface) => inner.Clear(surface);

    public void Dispose() => (inner as IDisposable)?.Dispose();
}
