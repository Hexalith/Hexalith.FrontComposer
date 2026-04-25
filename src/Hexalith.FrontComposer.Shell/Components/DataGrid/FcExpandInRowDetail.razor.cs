using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-5 T1.1 / D7 / D8 / D19 / AC1 / AC3 — Shell component hosting the expand-in-row
/// detail panel. Owns the <see cref="ElementReference"/> for the panel container and invokes
/// <see cref="IExpandInRowJSModule.InitializeAsync"/> when <see cref="HasExpanded"/> transitions
/// false → true (or when the panel re-renders after AC8 silent suppression returns).
/// </summary>
/// <remarks>
/// <para>
/// <b>D8 extended interop guard.</b> Two transition variables track the prior state — the
/// previous <see cref="HasExpanded"/> value AND a "panel was actually rendered" bit. The interop
/// fires only when (a) <see cref="HasExpanded"/> went false → true, OR (b) panel was previously
/// suppressed but now re-rendered (AC8 return path). Without the second guard, the
/// suppression-and-return cycle would skip the scroll-stabilizer because <see cref="HasExpanded"/>
/// stayed true across the suppression.
/// </para>
/// <para>
/// <b>D19 always-rendered container.</b> The outer <c>&lt;div role="region"&gt;</c> is emitted
/// regardless of <see cref="HasExpanded"/> so the trigger button's <c>aria-controls</c> always
/// resolves to a present element (WCAG 4.1.2). When collapsed, the FluentCollapsibleRegion is
/// not rendered — only the empty container with its <c>role</c> + <c>aria-label</c> remains.
/// </para>
/// </remarks>
public partial class FcExpandInRowDetail : ComponentBase {
    private readonly string _defaultPanelId = $"fc-expand-panel-{Guid.NewGuid():N}";
    private ElementReference _detailRef;
    private bool _previousHasExpanded;
    private bool _previousWasSuppressed;

    [Inject]
    private IExpandInRowJSModule ExpandInRowJSModule { get; set; } = default!;

    /// <summary>Stable per-view key (D22 ephemeral form). Reserved for future telemetry; carried for parity with sibling components.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>True when the host view has resolved an expanded item for this view-key. Drives the panel render gate.</summary>
    [Parameter]
    [EditorRequired]
    public bool HasExpanded { get; set; }

    /// <summary>The detail body emitted by the generator (factored EmitDetailRecordBody output).</summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Localized aria-label for the outer region landmark (per-row item-name interpolated).</summary>
    [Parameter]
    [EditorRequired]
    public string DetailPanelAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// AC8 D19 — populated by the host view when an expansion is "logically active" (state has
    /// the entry) but suppressed (filter has hidden the row). Empty / null suppresses the
    /// live-region announcement. Polite assertiveness — read after current utterance.
    /// </summary>
    [Parameter]
    public string? SuppressedAnnouncement { get; set; }

    /// <summary>
    /// Gets or sets the deterministic element id consumed by the trigger button's
    /// <c>aria-controls</c> attribute. When omitted, the component falls back to an
    /// instance-local id for standalone use.
    /// </summary>
    [Parameter]
    public string? PanelId { get; set; }

    private string EffectivePanelId => string.IsNullOrWhiteSpace(PanelId) ? _defaultPanelId : PanelId!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        // D8 extended guard: fire the JS scroll-stabilizer ONLY on transitions into the rendered
        // state or when a previously suppressed logical expansion reappears after filters clear.
        bool wasSuppressed = !string.IsNullOrWhiteSpace(SuppressedAnnouncement);
        if (HasExpanded && (!_previousHasExpanded || _previousWasSuppressed)) {
            await ExpandInRowJSModule.InitializeAsync(_detailRef).ConfigureAwait(true);
        }

        _previousHasExpanded = HasExpanded;
        _previousWasSuppressed = wasSuppressed;
    }
}
