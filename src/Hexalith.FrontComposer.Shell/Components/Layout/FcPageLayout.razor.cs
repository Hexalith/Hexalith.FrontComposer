using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Declares a page's FC-LYT layout measure (Story 1.2). A page drops this into its content to opt
/// into a non-default mode: <c>&lt;FcPageLayout Mode="FcPageLayoutMode.Constrained"&gt;…&lt;/FcPageLayout&gt;</c>.
/// The component carries no markup of its own — it renders its <see cref="ChildContent"/> verbatim.
/// </summary>
/// <remarks>
/// Follows <see cref="FcHamburgerToggle"/>'s register-on-render / clear-on-dispose lifecycle: it sets
/// the cascaded <see cref="FcPageLayoutCoordinator"/> mode on render (first render and any later
/// re-render, so a rebound <see cref="Mode"/> takes effect) and resets it to
/// <see cref="FcPageLayoutMode.FullWidth"/> on dispose, so leaving the page (or omitting this
/// component) restores the shell's edge-to-edge default. Satisfies the readiness-request
/// <c>&lt;PageLayout&gt;</c> naming intent while keeping full-width the zero-config default.
/// </remarks>
public sealed partial class FcPageLayout : ComponentBase, IDisposable {
    private bool _registered;

    /// <summary>
    /// The layout mode this page declares. Defaults to <see cref="FcPageLayoutMode.FullWidth"/>, so a
    /// bare <c>&lt;FcPageLayout&gt;</c> is a no-op; pass <see cref="FcPageLayoutMode.Constrained"/> to
    /// opt into the readable max-measure.
    /// </summary>
    [Parameter] public FcPageLayoutMode Mode { get; set; } = FcPageLayoutMode.FullWidth;

    /// <summary>The page content rendered within the declared measure.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The cascaded coordinator owned by <see cref="FrontComposerShell"/>. Null when the component is
    /// used outside a shell, in which case the declaration is silently inert.
    /// </summary>
    [CascadingParameter] private FcPageLayoutCoordinator? PageLayoutCoordinator { get; set; }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) {
        base.OnAfterRender(firstRender);
        if (PageLayoutCoordinator is not null) {
            // Push the declared mode on first render AND on every subsequent render. Unlike
            // FcHamburgerToggle (which registers a stable @ref once), Mode is a value parameter a page
            // may rebind, so we re-apply it whenever this component re-renders. SetMode no-ops on an
            // unchanged mode, so this cannot loop the render cycle. Records registration so dispose resets.
            _registered = true;
            PageLayoutCoordinator.SetMode(Mode);
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_registered) {
            PageLayoutCoordinator?.Reset();
        }
    }
}
