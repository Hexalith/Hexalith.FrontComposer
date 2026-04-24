namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D2 — virtualization lane decision, latched per view key at grid mount and only
/// re-evaluated on explicit <see cref="InvalidateGridAction"/> (component disposal + remount,
/// explicit refresh button, adopter-dispatched reload signals).
/// </summary>
public enum VirtualizationLane {
    /// <summary>
    /// Client-side path: <c>FluentDataGrid.Items</c> bound to the full in-memory list.
    /// Selected when first-mount <c>state.Items.Count &lt; VirtualizationServerSideThreshold</c>.
    /// </summary>
    ClientSide = 0,

    /// <summary>
    /// Server-side path: <c>FluentDataGrid.ItemsProvider</c> bound to the generator-emitted
    /// <c>LoadPageAsync</c> callback backed by <see cref="Contracts.Rendering.LoadPageAction"/>.
    /// Selected when first-mount <c>state.Items.Count &gt;= VirtualizationServerSideThreshold</c>.
    /// </summary>
    ServerSide = 1,
}
