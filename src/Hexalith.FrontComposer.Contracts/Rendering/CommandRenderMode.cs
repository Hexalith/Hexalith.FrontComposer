namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Command-form rendering mode selected by the generated renderer — either implicitly from the
/// command's density classification or explicitly via the <c>RenderMode</c> parameter
/// (Story 2-2 AC5). Orthogonal to <see cref="FcRenderMode"/> which selects the Blazor transport.
/// </summary>
public enum CommandRenderMode {
    /// <summary>Button (0 fields) or button + single-field FluentPopover (1 field).</summary>
    Inline,

    /// <summary>FluentCard with expand-in-row scroll stabilization; hides derivable fields (2–4 fields).</summary>
    CompactInline,

    /// <summary>Routable Blazor page with breadcrumb + max-width container (5+ fields).</summary>
    FullPage,
}
