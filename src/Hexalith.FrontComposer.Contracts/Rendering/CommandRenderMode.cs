namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Command-form rendering mode selected by the generated renderer — either implicitly from the
/// command's density classification or explicitly via the <c>RenderMode</c> parameter
/// (Story 2-2 AC5). Orthogonal to <see cref="FcRenderMode"/> which selects the Blazor transport.
/// </summary>
/// <remarks>
/// Numeric values are pinned because they may be persisted (telemetry, user preferences,
/// snapshot test fixtures); inserting a new mode in source order without an explicit value
/// would silently shift the persisted meaning of every existing value.
/// </remarks>
public enum CommandRenderMode {
    /// <summary>Button (0 fields) or button + single-field FluentPopover (1 field).</summary>
    Inline = 0,

    /// <summary>FluentCard with expand-in-row scroll stabilization; hides derivable fields (2–4 fields).</summary>
    CompactInline = 1,

    /// <summary>Routable Blazor page with breadcrumb + max-width container (5+ fields).</summary>
    FullPage = 2,
}
