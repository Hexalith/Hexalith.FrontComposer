namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Surface type routed through <c>DensityPrecedence.Resolve</c> to pick the UX-spec factory hybrid
/// default when neither the user preference nor the deployment default applies (Story 3-3 D5 / D6;
/// UX spec §208-214). Story 3-3 wires only <see cref="Default"/>; downstream stories (4-1 DataGrid,
/// 4-5 DetailView, 6-5 DevModeOverlay) plug their surface into the resolver when they land.
/// </summary>
/// <remarks>
/// Integer values are pinned so the enum can round-trip any future settings serialisation without
/// reordering risk. Additive evolution only — appending new surfaces at the tail is safe; reordering
/// or removing values is a v2 break.
/// </remarks>
public enum DensitySurface : byte {
    /// <summary>Shell-level default surface — falls back to <c>Comfortable</c>.</summary>
    Default = 0,

    /// <summary>DataGrid surface — factory default is <c>Compact</c> (UX spec §208).</summary>
    DataGrid = 1,

    /// <summary>Single-entity detail view — factory default is <c>Comfortable</c> (UX spec §210).</summary>
    DetailView = 2,

    /// <summary>Command form surface — factory default is <c>Comfortable</c> (UX spec §211).</summary>
    CommandForm = 3,

    /// <summary>Navigation sidebar surface — factory default is <c>Comfortable</c> (UX spec §212).</summary>
    NavigationSidebar = 4,

    /// <summary>Dev-mode overlay surface — factory default is <c>Compact</c> (UX spec §214).</summary>
    DevModeOverlay = 5,
}
