using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Names the shell's single content <c>main</c> landmark (<c>#fc-main-content</c>) from a page
/// (handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>, Requested
/// outcome 1/2). A page drops this into its content to name the landmark by its visible route
/// heading <b>without</b> an orphaned page-level <c>aria-labelledby</c> on a non-landmark wrapper:
/// <c>&lt;FcContentLabel LabelledBy="@HeadingId" /&gt;</c>. The component carries no markup of its own
/// — it renders its optional <see cref="ChildContent"/> verbatim.
/// </summary>
/// <remarks>
/// Follows <see cref="FcPageLayout"/>'s register-on-render / clear-on-dispose lifecycle: it sets the
/// cascaded <see cref="FcContentLabelCoordinator"/> name on render (first render and any later
/// re-render, so a rebound <see cref="LabelledBy"/> / <see cref="Label"/> takes effect) and resets it
/// on dispose, so leaving the page (or omitting this component) restores the shell's unlabelled
/// default. Used outside a shell the declaration is silently inert.
/// <para>
/// <b>Single-writer (last-writer-wins).</b> One declaration per page is the supported shape. Two
/// <see cref="FcContentLabel"/> markers on one page share the single cascaded
/// <see cref="FcContentLabelCoordinator"/>: disposing either one calls
/// <see cref="FcContentLabelCoordinator.Reset"/> and clears a still-live sibling's name from
/// <c>#fc-main-content</c> until the survivor next re-renders. This faithfully mirrors the accepted
/// <see cref="FcPageLayoutCoordinator"/> last-writer-wins limitation; a writer-identity guard (applied
/// to both coordinators together) would be required before multi-writer support is safe.
/// </para>
/// <para>
/// <b>First-paint (InteractiveServer).</b> Registration runs from <c>OnAfterRender</c>, so on a
/// static-SSR / prerender pass <c>#fc-main-content</c> carries no page-declared
/// <c>aria-label</c> / <c>aria-labelledby</c> until interactive hydration completes. The shell-parameter
/// path (<c>FrontComposerShell.ContentLabel</c> / <c>ContentLabelledBy</c>) is correct on first paint;
/// prefer it when a name must be present before hydration.
/// </para>
/// </remarks>
public sealed partial class FcContentLabel : ComponentBase, IDisposable {
    private bool _registered;

    /// <summary>
    /// Id reference that names the content landmark (emitted as <c>aria-labelledby</c>). Prefer this
    /// over <see cref="Label"/>: pass the <c>HeadingId</c> of the route's <see cref="FcPageHeader"/>
    /// so the landmark is named by the visible page heading. Takes precedence over <see cref="Label"/>.
    /// </summary>
    [Parameter] public string? LabelledBy { get; set; }

    /// <summary>
    /// Literal accessible name for the content landmark (emitted as <c>aria-label</c>). Use only when
    /// no visible heading exists to reference. Ignored when <see cref="LabelledBy"/> is set.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Optional content rendered verbatim within the declaration.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The cascaded coordinator owned by <see cref="FrontComposerShell"/>. Null when the component is
    /// used outside a shell, in which case the declaration is silently inert.
    /// </summary>
    [CascadingParameter] private FcContentLabelCoordinator? ContentLabelCoordinator { get; set; }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) {
        base.OnAfterRender(firstRender);
        if (ContentLabelCoordinator is not null) {
            // Push the declared name on first render AND on every subsequent render. LabelledBy/Label
            // are value parameters a page may rebind, so we re-apply whenever this component
            // re-renders. Set no-ops on unchanged values, so this cannot loop the render cycle.
            // Records registration so dispose resets.
            _registered = true;
            ContentLabelCoordinator.Set(Label, LabelledBy);
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_registered) {
            ContentLabelCoordinator?.Reset();
        }
    }
}
