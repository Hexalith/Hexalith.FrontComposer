namespace Hexalith.FrontComposer.Contracts;

/// <summary>
/// Adopter-facing shell options for FrontComposer — themeable defaults consumed by
/// generated renderers and shell components. Bound via
/// <c>services.Configure&lt;FcShellOptions&gt;(...)</c>.
/// </summary>
public sealed class FcShellOptions {
    /// <summary>
    /// Gets or sets the <c>max-width</c> CSS value for FullPage command renderers' containers
    /// (Story 2-2 Decision D26). Default: <c>"720px"</c>.
    /// </summary>
    public string FullPageFormMaxWidth { get; set; } = "720px";

    /// <summary>
    /// Gets or sets whether FullPage command renderers embed an inline <c>FluentBreadcrumb</c>
    /// (Story 2-2 Decision D15). Shell-level breadcrumb rendering lands in Story 3.1;
    /// until then the embedded fallback is on by default. Default: <see langword="true"/>.
    /// </summary>
    public bool EmbeddedBreadcrumb { get; set; } = true;

    /// <summary>
    /// Gets or sets the LRU cap for per-view <c>DataGridNavigationState</c> snapshots
    /// (Story 2-2 Decision D33). Reducer evicts the entry with the oldest <c>CapturedAt</c>
    /// once <c>ViewStates.Count</c> exceeds this cap. Default: 50.
    /// </summary>
    public int DataGridNavCap { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether <c>LastUsedValueProvider</c> suppresses its dev-mode
    /// "LastUsed persistence disabled" <c>FluentMessageBar</c> surface
    /// (Story 2-2 Task 3.5). Production builds ignore this and never render the message.
    /// </summary>
    public bool LastUsedDisabled { get; set; }
}
