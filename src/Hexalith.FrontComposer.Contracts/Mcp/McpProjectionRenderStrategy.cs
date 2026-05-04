namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// SDK-neutral render strategy for an agent-readable projection resource.
/// </summary>
public enum McpProjectionRenderStrategy {
    /// <summary>Standard tabular projection rendering.</summary>
    Default,

    /// <summary>Pending-action tabular projection rendering.</summary>
    ActionQueue,

    /// <summary>Grouped status summary rendering.</summary>
    StatusOverview,

    /// <summary>Single-record detail rendering.</summary>
    DetailRecord,

    /// <summary>Chronological timeline rendering.</summary>
    Timeline,

    /// <summary>Reserved dashboard rendering strategy.</summary>
    Dashboard,
}
