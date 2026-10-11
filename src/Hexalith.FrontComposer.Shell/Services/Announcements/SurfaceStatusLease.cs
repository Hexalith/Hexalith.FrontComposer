namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Releases one standalone status node claim.</summary>
internal sealed class SurfaceStatusLease(SurfaceStatusOwnership ownership, string surface, Action<bool> changed) : IDisposable
{
    private int _disposed;

    /// <summary>Gets or sets whether this mounted notice currently owns the node.</summary>
    internal bool IsOwner { get; set; }

    /// <summary>Gets the renderer notification for an ownership change.</summary>
    internal Action<bool> Changed { get; } = changed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            ownership.Release(surface, this);
        }
    }
}
