using System.Runtime.CompilerServices;

namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Shares one standalone status node among notices using the same scoped surface.</summary>
internal sealed class SurfaceStatusOwnership
{
    private static readonly ConditionalWeakTable<ISurfaceAnnouncementCoordinator, SurfaceStatusOwnership> Owners = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, List<SurfaceStatusLease>> _claims = new(StringComparer.Ordinal);

    /// <summary>Gets the status ownership registry belonging to a scoped coordinator.</summary>
    internal static SurfaceStatusOwnership For(ISurfaceAnnouncementCoordinator coordinator)
        => Owners.GetValue(coordinator, static _ => new SurfaceStatusOwnership());

    /// <summary>Claims a node, promoting the next mounted notice when the owner leaves.</summary>
    internal SurfaceStatusLease Claim(string surface, Action<bool> changed)
    {
        lock (_gate)
        {
            if (!_claims.TryGetValue(surface, out List<SurfaceStatusLease>? claims))
            {
                claims = [];
                _claims.Add(surface, claims);
            }
            SurfaceStatusLease lease = new(this, surface, changed) { IsOwner = claims.Count == 0 };
            claims.Add(lease);
            return lease;
        }
    }

    /// <summary>Releases a mounted notice's claim without changing its announcement stream.</summary>
    internal void Release(string surface, SurfaceStatusLease lease)
    {
        lock (_gate)
        {
            if (!_claims.TryGetValue(surface, out List<SurfaceStatusLease>? claims)
                || !claims.Remove(lease))
            {
                return;
            }
            if (claims.Count == 0)
            {
                _claims.Remove(surface);
            }
            else if (lease.IsOwner)
            {
                claims[0].IsOwner = true;
                claims[0].Changed(true);
            }
        }
    }
}
