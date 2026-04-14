namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Defines the UI role a projection plays, determining the default rendering strategy.
/// Capped at five roles to enforce focused domain modeling.
/// </summary>
public enum ProjectionRole {
    /// <summary>A queue of pending actions requiring user intervention.</summary>
    ActionQueue,

    /// <summary>A high-level status overview dashboard tile.</summary>
    StatusOverview,

    /// <summary>A detailed single-record view.</summary>
    DetailRecord,

    /// <summary>A chronological event timeline.</summary>
    Timeline,

    /// <summary>An aggregated metrics dashboard.</summary>
    Dashboard,
}
