namespace Hexalith.FrontComposer.Contracts.Freshness;

/// <summary>UI-normalized record freshness; transport wire values remain caller-owned.</summary>
public enum FcFreshnessStatus
{
    /// <summary>The record reflects the current projection.</summary>
    Current,
    /// <summary>The record is awaiting refresh.</summary>
    Stale,
    /// <summary>Only the last known record is available.</summary>
    Degraded,
}
