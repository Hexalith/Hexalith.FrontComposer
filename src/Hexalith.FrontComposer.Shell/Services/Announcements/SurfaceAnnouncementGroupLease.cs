namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Releases one mounted projection producer's announcement group claim.</summary>
internal sealed class SurfaceAnnouncementGroupLease(SurfaceAnnouncementGroupOwnership ownership, string surface, string group) : IDisposable
{
    private int _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            ownership.Release(surface, group);
        }
    }
}
