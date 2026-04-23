namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T4.1 / D6 / G5 — layout variant for <see cref="FcProjectionLoadingSkeleton"/>.
/// Picked per projection role so the skeleton previews the shape of the body it replaces
/// (table vs card vs timeline). A 5-row table skeleton preceding a single
/// <c>FluentCard</c> render is a jarring "lie"; role-aware skeletons keep the promise.
/// </summary>
public enum SkeletonLayout {
    /// <summary>Default / ActionQueue / StatusOverview — N rows × M columns table shimmer.</summary>
    DataGrid,

    /// <summary>DetailRecord — single-card title bar + label/value row shimmers.</summary>
    Card,

    /// <summary>Timeline — vertical stack of timestamp pill + label shimmers.</summary>
    Timeline,
}
