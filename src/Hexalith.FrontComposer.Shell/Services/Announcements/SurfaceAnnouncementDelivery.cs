namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Carries delivery silence through interface-forwarding coordinators and queued renderer callbacks.</summary>
internal static class SurfaceAnnouncementDelivery
{
    private static readonly AsyncLocal<(string Surface, long Revision, bool Silent, bool FreshRow)?> Current = new();

    /// <summary>Reads the silence captured for this delivered revision, independently of coordinator wrappers.</summary>
    internal static bool IsSilent(string surface, long revision) => Current.Value is { } current
        && current.Surface == surface && current.Revision == revision && current.Silent;

    /// <summary>Gets whether the delivered or replayed message belongs to the fresh-row family.</summary>
    internal static bool IsFreshRow(string surface, long revision) => Current.Value is { } current
        && current.Surface == surface && current.Revision == revision && current.FreshRow;

    /// <summary>Runs synchronous subscriber delivery with a context that flows into renderer continuations.</summary>
    internal static void Run(string surface, long revision, bool silent, Action deliver, bool freshRow = false)
    {
        var previous = Current.Value;
        Current.Value = (surface, revision, silent, freshRow);
        try
        {
            deliver();
        }
        finally
        {
            Current.Value = previous;
        }
    }
}
