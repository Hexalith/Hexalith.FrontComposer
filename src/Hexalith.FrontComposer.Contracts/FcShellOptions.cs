using System.ComponentModel.DataAnnotations;

namespace Hexalith.FrontComposer.Contracts;

/// <summary>
/// Adopter-facing shell options for FrontComposer — themeable defaults consumed by
/// generated renderers and shell components. Bound via
/// <c>services.Configure&lt;FcShellOptions&gt;(...)</c>.
/// </summary>
/// <remarks>
/// Bind validation via
/// <c>services.AddOptions&lt;FcShellOptions&gt;().BindConfiguration("...").ValidateDataAnnotations().ValidateOnStart();</c>
/// to surface misconfiguration at startup rather than at first use.
/// </remarks>
public sealed class FcShellOptions {
    /// <summary>
    /// Gets or sets the <c>max-width</c> CSS value for FullPage command renderers' containers
    /// (Story 2-2 Decision D26). Default: <c>"720px"</c>.
    /// </summary>
    /// <remarks>
    /// Restricted to <c>&lt;number&gt;&lt;unit&gt;</c> form (units: <c>px</c>, <c>em</c>, <c>rem</c>,
    /// <c>%</c>, <c>vw</c>, <c>vh</c>, <c>ch</c>) because the value is interpolated into a Razor
    /// <c>style="max-width: {value}; ..."</c> attribute by the generated renderer. The format guard
    /// blocks CSS injection from misconfigured / hostile sources.
    /// </remarks>
    [RegularExpression(
        @"^(?!0+(\.0+)?(px|em|rem|%|vw|vh|ch)$)\d+(\.\d+)?(px|em|rem|%|vw|vh|ch)$",
        ErrorMessage = "FullPageFormMaxWidth must match '<non-zero-number><unit>' where unit is one of: px, em, rem, %, vw, vh, ch.")]
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
    /// <remarks>
    /// Must be at least 1. A value of 0 or negative would cause the reducer to evict every
    /// snapshot on every Capture, silently destroying all state.
    /// </remarks>
    [Range(1, int.MaxValue, ErrorMessage = "DataGridNavCap must be at least 1.")]
    public int DataGridNavCap { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether to suppress the dev-mode <c>FluentMessageBar</c> warning rendered by
    /// <c>LastUsedValueProvider</c> when tenant or user context is missing (Story 2-2 Task 3.5).
    /// </summary>
    /// <remarks>
    /// This option ONLY controls the dev-mode notice. It does NOT disable LastUsed itself —
    /// per Decision D31 the provider always fails-closed when tenant/user are missing,
    /// regardless of this flag. Production builds skip the notice entirely.
    /// </remarks>
    public bool LastUsedDisabled { get; set; }

    /// <summary>
    /// Threshold (ms) before <c>FcLifecycleWrapper</c> applies the <c>.fc-lifecycle-pulse</c>
    /// animation. A Confirmed transition arriving within this window never fires the pulse
    /// (UX-DR48 brand-signal fusion). Story 2-4 Decision D12 / NFR11.
    /// </summary>
    [Range(50, 2_000, ErrorMessage = "SyncPulseThresholdMs must be between 50 and 2000.")]
    public int SyncPulseThresholdMs { get; set; } = 300;

    /// <summary>
    /// Threshold (ms) at which <c>FcLifecycleWrapper</c> renders the localized "Still syncing…"
    /// <c>FluentBadge</c> beneath the wrapped form. Story 2-4 Decision D12 / NFR13.
    /// </summary>
    [Range(500, 10_000, ErrorMessage = "StillSyncingThresholdMs must be between 500 and 10000.")]
    public int StillSyncingThresholdMs { get; set; } = 2_000;

    /// <summary>
    /// Threshold (ms) at which <c>FcLifecycleWrapper</c> escalates to the action-prompt
    /// <c>FluentMessageBar</c> offering manual refresh recovery. Story 2-4 Decision D12 / NFR14.
    /// </summary>
    [Range(5_000, 60_000, ErrorMessage = "TimeoutActionThresholdMs must be between 5000 and 60000.")]
    public int TimeoutActionThresholdMs { get; set; } = 10_000;

    /// <summary>
    /// Duration (ms) the Confirmed <c>FluentMessageBar</c> remains visible before auto-dismiss.
    /// Story 2-4 Decision D12 / AC6.
    /// </summary>
    [Range(1_000, 30_000, ErrorMessage = "ConfirmedToastDurationMs must be between 1000 and 30000.")]
    public int ConfirmedToastDurationMs { get; set; } = 5_000;
}
