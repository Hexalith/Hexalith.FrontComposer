namespace Hexalith.FrontComposer.Contracts.Freshness;

/// <summary>The announcement urgency chosen by the caller's domain mapping.</summary>
public enum FcLiveRegionPoliteness
{
    /// <summary>No live region is rendered.</summary>
    None,
    /// <summary>A routine status is announced politely.</summary>
    Polite,
    /// <summary>A validation or failure message is announced assertively.</summary>
    Assertive,
}
