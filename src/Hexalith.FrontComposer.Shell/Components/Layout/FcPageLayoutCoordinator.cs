using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Coordinates the active <see cref="FcPageLayoutMode"/> between a page's <see cref="FcPageLayout"/>
/// declaration (which lives inside <c>@ChildContent</c>, below the shell) and the shell's
/// <c>#fc-main-content</c> container (FC-LYT contract, Story 1.2).
/// </summary>
/// <remarks>
/// Instance-per-shell field cascaded as a fixed value, mirroring <see cref="LayoutHamburgerCoordinator"/>
/// (the repo's established child→shell layout-signalling pattern). <see cref="FcPageLayout"/> sets its
/// mode on first render and resets to <see cref="FcPageLayoutMode.FullWidth"/> on dispose; the shell
/// subscribes to <see cref="Changed"/> to re-render the container's <c>data-fc-page-layout</c> attribute
/// and constrained-class toggle. <see cref="FcPageLayoutMode.FullWidth"/> is the default so a page that
/// declares nothing keeps today's edge-to-edge behaviour (zero regression). Single-writer, not a DI
/// singleton — honours ADR-030 scoped lifetime exactly like <see cref="LayoutHamburgerCoordinator"/>.
/// </remarks>
/// <remarks>
/// <b>Single-writer, last-writer-wins.</b> One <see cref="FcPageLayout"/> per page is the supported
/// shape. If two are live at once, the last to set its mode wins and disposing either resets the
/// coordinator to <see cref="FcPageLayoutMode.FullWidth"/> regardless of the other — declaring two
/// measures on one page is unsupported (out of MVP scope).
/// </remarks>
internal sealed class FcPageLayoutCoordinator {
    /// <summary>
    /// Raised when <see cref="Mode"/> changes so the shell can re-render the content container. The
    /// shell subscribes on init and unsubscribes on dispose.
    /// </summary>
    internal event Action? Changed;

    /// <summary>The active page-layout mode. Defaults to <see cref="FcPageLayoutMode.FullWidth"/>.</summary>
    internal FcPageLayoutMode Mode { get; private set; } = FcPageLayoutMode.FullWidth;

    /// <summary>
    /// Sets the active mode. Called by <see cref="FcPageLayout"/> on first render. No-ops (no event)
    /// when the mode is unchanged, so a shell re-render cannot re-enter via the child's render loop.
    /// </summary>
    /// <param name="mode">The mode the page declares.</param>
    internal void SetMode(FcPageLayoutMode mode) {
        if (Mode == mode) {
            return;
        }

        Mode = mode;
        Changed?.Invoke();
    }

    /// <summary>
    /// Resets to the default <see cref="FcPageLayoutMode.FullWidth"/>. Called on
    /// <see cref="FcPageLayout"/> dispose so leaving a constrained page restores the edge-to-edge default.
    /// </summary>
    internal void Reset() => SetMode(FcPageLayoutMode.FullWidth);
}
