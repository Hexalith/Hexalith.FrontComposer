namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Page-layout measure for a FrontComposer page (FC-LYT contract, Story 1.2).
/// Selects how wide a page's content renders within the shell content area.
/// </summary>
/// <remarks>
/// <see cref="FullWidth"/> is the zero/default value and preserves the shell's edge-to-edge
/// behaviour; <see cref="Constrained"/> is the explicit opt-in for a readable max measure.
/// </remarks>
public enum FcPageLayoutMode {
    /// <summary>
    /// Content spans the full content area edge-to-edge (the default). Right for DataGrid-dense,
    /// read-only projection pages.
    /// </summary>
    FullWidth,

    /// <summary>
    /// Content is capped at a readable max measure and centred within the content area
    /// (<c>--fc-page-max-inline-size</c>). Opt-in for prose, forms, and detail pages.
    /// </summary>
    Constrained,
}
