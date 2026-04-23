namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Transform-stage strategy dispatched per projection (Story 4-1 D4 / D5 / ADR-052).
/// Mirrors <see cref="Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole"/> one-to-one
/// plus the synthetic <see cref="Default"/> sentinel used when no <c>[ProjectionRole]</c>
/// attribute is present — the public enum has no way to express "default" because absence
/// IS default. Kept internal to SourceTools so the emit-stage switch can be exhaustively
/// checked at compile time.
/// </summary>
public enum ProjectionRenderStrategy {
    /// <summary>Sentinel: no <c>[ProjectionRole]</c> attribute present. Renders the standard compact DataGrid.</summary>
    Default,

    /// <summary>Inline-action DataGrid filtered by <c>WhenState</c> (Story 4-1 AC1).</summary>
    ActionQueue,

    /// <summary>Aggregate-count DataGrid with click-through navigation (Story 4-1 AC2).</summary>
    StatusOverview,

    /// <summary><see cref="Microsoft.FluentUI.AspNetCore.Components.FluentCard"/> single-entity body (Story 4-1 AC3).</summary>
    DetailRecord,

    /// <summary>Vertical chronological list ordered by first DateTime property (Story 4-1 AC4).</summary>
    Timeline,

    /// <summary>Reserved — delegates to <see cref="Default"/> + HFC1023 Information diagnostic (Story 4-1 AC10 / D16).</summary>
    Dashboard,
}
