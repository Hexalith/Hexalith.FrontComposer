using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Rendering;

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

    /// <summary>
    /// Threshold (seconds, NOT ms) at which <c>FcFormAbandonmentGuard</c> starts protecting
    /// full-page forms from accidental navigation loss. Timer anchors on the first
    /// <c>EditContext.OnFieldChanged</c> — mounting without editing never arms the guard.
    /// Story 2-5 Decision D6 / D10 / AC6.
    /// </summary>
    /// <remarks>
    /// Unit intentionally differs from the millisecond thresholds above: epics §1050 spec
    /// uses "30 seconds" verbatim, so the options surface exposes seconds to keep adopter
    /// configuration copy-paste-matching the spec.
    /// </remarks>
    [Range(5, 600, ErrorMessage = "FormAbandonmentThresholdSeconds must be between 5 and 600.")]
    public int FormAbandonmentThresholdSeconds { get; set; } = 30;

    /// <summary>
    /// Duration (ms) the idempotent Info <c>FluentMessageBar</c> remains visible before
    /// auto-dismiss (Story 2-5 Decision D6 / AC2). Default 5000 ms — Sally review #2 raised from
    /// 3000 so <c>aria-live="polite"</c> screen-reader queue delay does not cut users off mid-read.
    /// </summary>
    [Range(1_000, 30_000, ErrorMessage = "IdempotentInfoToastDurationMs must be between 1000 and 30000.")]
    public int IdempotentInfoToastDurationMs { get; set; } = 5_000;

    /// <summary>
    /// Accent color applied via <c>IThemeService.SetThemeAsync(ThemeSettings)</c> (Story 3-1 D1,
    /// AC2). Default <c>#0097A7</c>. Must be a 6-digit hex (case-insensitive) — named colors
    /// (<c>teal</c>), <c>rgb()</c>/<c>hsl()</c> syntax, and 8-hex alpha forms are rejected at
    /// startup so the value can be interpolated safely into a style block.
    /// </summary>
    [RegularExpression(
        @"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "AccentColor must be a 6-digit hex color like '#0097A7'.")]
    public string AccentColor { get; set; } = "#0097A7";

    /// <summary>
    /// LRU cap for <c>LocalStorageService</c> entries (Story 3-1 D15 / AC6). Default 500.
    /// Keep under 10 000 so the eviction scan (<c>OrderBy</c> over the last-access timestamps)
    /// stays O(n) with a small n.
    /// </summary>
    [Range(50, 10_000, ErrorMessage = "LocalStorageMaxEntries must be between 50 and 10000.")]
    public int LocalStorageMaxEntries { get; set; } = 500;

    /// <summary>
    /// Default UI culture (Story 3-1 D14 / AC7). Default <c>en</c>. BCP-47-lite format:
    /// two-letter language code optionally followed by <c>-CC</c> region code (e.g.
    /// <c>en</c>, <c>en-US</c>, <c>fr</c>, <c>fr-CA</c>).
    /// </summary>
    [RegularExpression(
        @"^[a-z]{2}(-[A-Z]{2})?$",
        ErrorMessage = "DefaultCulture must be a BCP-47 language tag like 'en' or 'en-US'.")]
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// Cultures <c>AddHexalithShellLocalization</c> advertises to ASP.NET Core's request
    /// localization pipeline (Story 3-1 D14 / AC7). Default <c>["en", "fr"]</c>. Must include
    /// <see cref="DefaultCulture"/>; the threshold validator enforces that cross-property invariant.
    /// </summary>
    public IReadOnlyList<string> SupportedCultures { get; set; } = ["en", "fr"];

    /// <summary>
    /// Deployment-wide default display density (Story 3-3 D4 / ADR-039 tier 2). <see langword="null"/>
    /// (default) means "no deployment default — fall through to the UX-spec factory hybrid for the
    /// surface (tier 3)." Non-null values are validated by <c>EnumDataType</c> through ASP.NET Core's
    /// built-in annotation pipeline.
    /// </summary>
    /// <remarks>
    /// Tier order in <c>DensityPrecedence.Resolve</c>: (1) viewport tier-force override at
    /// <c>ViewportTier ≤ Tablet</c> → <c>Comfortable</c> (ADR-040); (2) user preference when non-null;
    /// (3) <b>this property</b> when non-null; (4) factory hybrid by <c>DensitySurface</c>
    /// (UX spec §208-214). Adopters typically set this in <c>appsettings.json</c> — e.g. call-center
    /// deployments ship with <c>"DefaultDensity": "Compact"</c>.
    /// </remarks>
    [EnumDataType(typeof(DensityLevel), ErrorMessage = "DefaultDensity must be Compact, Comfortable, or Roomy.")]
    public DensityLevel? DefaultDensity { get; set; }

    /// <summary>
    /// Upper bound (inclusive) on the unfiltered row count returned by projection queries.
    /// Story 4-4 Decision D10. When <c>state.Items.Count</c> reaches this cap, the shell surfaces
    /// <c>FcMaxItemsCapNotice</c> inviting the user to apply filters. Servers are expected to
    /// honour this value via <c>QueryRequest.Take</c> clamping. Default: 10 000.
    /// </summary>
    [Range(100, 1_000_000, ErrorMessage = "MaxUnfilteredItems must be between 100 and 1000000.")]
    public int MaxUnfilteredItems { get; set; } = 10_000;

    /// <summary>
    /// Threshold (ms) at which <c>FcSlowQueryNotice</c> surfaces after a first-page fetch
    /// (Story 4-4 Decision D8 / D10 / AC3). Banner auto-dismisses after 5 s (matches
    /// <c>ConfirmedToastDurationMs</c> precedent). Default: 2 000 ms.
    /// </summary>
    [Range(500, 30_000, ErrorMessage = "SlowQueryThresholdMs must be between 500 and 30000.")]
    public int SlowQueryThresholdMs { get; set; } = 2_000;

    /// <summary>
    /// Lane decision boundary (Story 4-4 Decision D2 / D10). Projections whose first-mount
    /// <c>state.Items.Count</c> falls below this value bind the client-side
    /// <c>FluentDataGrid.Items</c> path; at or above, the generator emits a
    /// <c>GridItemsProvider&lt;T&gt;</c> server-side pagination lane. The validator
    /// enforces <c>VirtualizationServerSideThreshold &lt; MaxUnfilteredItems</c> so the server-side
    /// lane triggers before the cap. Default: 500.
    /// </summary>
    [Range(50, 10_000, ErrorMessage = "VirtualizationServerSideThreshold must be between 50 and 10000.")]
    public int VirtualizationServerSideThreshold { get; set; } = 500;

    /// <summary>
    /// Upper bound on <c>LoadedPageState.PagesByKey</c> entries across all view keys
    /// (Story 4-4 Decision D10). When a successful page write pushes the total above this value,
    /// the reducer evicts the oldest insertion-ordered entry and emits an Information-level log.
    /// Default: 200 pages — approximately 4 000 rows at 20 rows/page.
    /// </summary>
    [Range(10, 10_000, ErrorMessage = "MaxCachedPages must be between 10 and 10000.")]
    public int MaxCachedPages { get; set; } = 200;

    /// <summary>
    /// Story 5-2 D2 — global per-cache LRU entry cap for the opportunistic ETag cache.
    /// Default 200 entries. <c>0</c> disables the cache entirely (queries still execute, but
    /// <c>If-None-Match</c> is never sent and 200 responses are not cached). Bounded above by
    /// <see cref="LocalStorageMaxEntries"/> so a busy ETag cache cannot crowd out unrelated
    /// persisted preference state. <see cref="FcShellOptionsThresholdValidator"/> enforces the
    /// upper bound as a cross-property invariant.
    /// </summary>
    [Range(0, 10_000, ErrorMessage = "MaxETagCacheEntries must be between 0 and 10000.")]
    public int MaxETagCacheEntries { get; set; } = 200;

    /// <summary>
    /// Duration (ms) that the EventStore projection connection indicator keeps the non-blocking
    /// reconnected confirmation visible. Story 5-3 D10.
    /// </summary>
    [Range(1_000, 30_000, ErrorMessage = "ProjectionReconnectedNoticeDurationMs must be between 1000 and 30000.")]
    public int ProjectionReconnectedNoticeDurationMs { get; set; } = 3_000;

    /// <summary>
    /// Bounded fallback polling interval while the EventStore projection hub is reconnecting or
    /// disconnected. A value of <c>0</c> disables fallback polling. Story 5-3 D8.
    /// </summary>
    [Range(0, 300, ErrorMessage = "ProjectionFallbackPollingIntervalSeconds must be between 0 and 300.")]
    public int ProjectionFallbackPollingIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum number of visible projection lanes the fallback poller may refresh per tick.
    /// Story 5-3 D8 / L14 bounded runtime state.
    /// </summary>
    [Range(1, 100, ErrorMessage = "MaxProjectionFallbackPollingLanes must be between 1 and 100.")]
    public int MaxProjectionFallbackPollingLanes { get; set; } = 8;

    /// <summary>
    /// Maximum number of unresolved pending command entries retained in the circuit-local shell state.
    /// Story 5-5 D12 / L14. Eviction marks the oldest command as unresolved instead of confirming it.
    /// </summary>
    [Range(1, 1_000, ErrorMessage = "MaxPendingCommandEntries must be between 1 and 1000.")]
    public int MaxPendingCommandEntries { get; set; } = 100;
}
