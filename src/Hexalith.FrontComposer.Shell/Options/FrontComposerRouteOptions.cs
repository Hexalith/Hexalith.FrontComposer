namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>Declares top-level host routes which cannot be registered as Module aliases.</summary>
public sealed class FrontComposerRouteOptions
{
    /// <summary>Host-owned top-level segments, compared without regard to case.</summary>
    public ISet<string> ReservedSegments { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
