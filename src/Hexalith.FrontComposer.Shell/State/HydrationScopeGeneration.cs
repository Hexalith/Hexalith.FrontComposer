namespace Hexalith.FrontComposer.Shell.State;

/// <summary>Rejects hydration from before a scope transition while preserving legacy initial actions.</summary>
internal static class HydrationScopeGeneration
{
    internal static bool IsCurrent(long current, long? origin)
        => origin == current || (current == 0 && origin is null);
}
