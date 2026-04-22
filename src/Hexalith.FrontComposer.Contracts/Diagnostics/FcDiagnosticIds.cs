namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Runtime-log diagnostic IDs (HFC2xxx). Reserved in code so logging call sites can reference
/// them symbolically; no analyzer release entry needed per architecture.md §648 precedent.
/// </summary>
/// <remarks>
/// Story 2-4 reserves HFC2100–HFC2102 for <c>FcLifecycleWrapper</c>. Extend this file as
/// additional runtime-log codes are allocated in subsequent stories.
/// </remarks>
public static class FcDiagnosticIds {
    /// <summary>
    /// Registry startup validation (<c>FrontComposerRegistry.ValidateManifests</c>) detected a
    /// command with no FullPage route (Story 3-4 D21). Thrown as an <see cref="InvalidOperationException"/>
    /// during service-collection build-up so the application never boots in a state where the palette
    /// can surface unreachable commands. Story 9-4 layers a compile-time analyzer that short-circuits
    /// this startup guard.
    /// </summary>
    public const string HFC1601_ManifestInvalid = "HFC1601";

    /// <summary>Wrapper received a transition for an unknown CorrelationId (subscribe-after-terminal-cleanup race).</summary>
    public const string HFC2100_UnknownCorrelationId = "HFC2100";

    /// <summary>Wrapper observed an idempotency-resolved transition (Story 2-5 will surface "already done" copy).</summary>
    public const string HFC2101_IdempotencyResolvedObserved = "HFC2101";

    /// <summary>Wrapper threshold timer fired outside the UI-thread context.</summary>
    public const string HFC2102_ThresholdTimerOffUiThread = "HFC2102";

    /// <summary>
    /// Form abandonment guard suppressed its warning bar because the lifecycle state
    /// is Submitting (or the wrapper flagged its own Start-over navigation). Story 2-5 D13.
    /// Information severity — this is expected suppression, not anomaly.
    /// </summary>
    public const string HFC2103_AbandonmentDuringSubmitting = "HFC2103";

    /// <summary>
    /// Wrapper rendered the idempotent Info bar (Story 2-5 D3 / AC2). Logged so adopters
    /// can measure frequency and calibrate copy; CorrelationId is redacted to its first 8
    /// characters + ellipsis (Story 2-4 RT-4 — not a cryptographic hash).
    /// </summary>
    public const string HFC2104_IdempotentInfoBarRendered = "HFC2104";

    /// <summary>
    /// Theme or density effect short-circuited persistence because <c>IUserContextAccessor</c>
    /// returned null / empty / whitespace for tenant or user (Story 3-1 D8 / ADR-029). Information
    /// severity — expected when <c>NullUserContextAccessor</c> is the registered default and no
    /// real accessor has been supplied yet. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2105_StoragePersistenceSkipped = "HFC2105";

    /// <summary>
    /// Theme effect hydrated with no stored value on app init (Story 3-1 D8 / AC3). Information
    /// severity — first-visit baseline, feature defaults apply. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2106_ThemeHydrationEmpty = "HFC2106";

    /// <summary>
    /// Navigation effect hydrated with no stored value on app init, or the stored blob was
    /// unreadable (Story 3-2 D15 amended). Information severity — feature defaults apply. The
    /// <c>Reason</c> payload distinguishes <c>Empty</c> (no blob) vs <c>Corrupt</c> (deserialization /
    /// shape failure). Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2107_NavigationHydrationEmpty = "HFC2107";

    /// <summary>
    /// Two <c>IShortcutService.Register</c> calls supplied the same normalised binding (Story 3-4 D3 / D19).
    /// Information severity — last-writer-wins is the documented adopter-override path. The structured
    /// payload carries <c>{Binding, PreviousDescriptionKey, NewDescriptionKey, PreviousCallSiteFile,
    /// PreviousCallSiteLine, NewCallSiteFile, NewCallSiteLine}</c> so an operator can identify both
    /// the overwritten registration and the replacing call site. Build-time
    /// enforcement is deferred to Story 9-4. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2108_ShortcutConflict = "HFC2108";

    /// <summary>
    /// A registered keyboard-shortcut handler threw an exception inside
    /// <c>IShortcutService.TryInvokeAsync</c> (Story 3-4 D1 handler-exception policy). Warning severity —
    /// the service caught the exception so it does not bubble to the Blazor error boundary; the
    /// shortcut is still treated as "fired" (returns <c>true</c>). Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2109_ShortcutHandlerFault = "HFC2109";

    /// <summary>
    /// <c>IFrontComposerRegistry.GetManifests()</c> threw inside the palette debounced scoring path
    /// (Story 3-4 ADR-043). Warning severity — palette renders "No matches found" instead of stalling
    /// in a "Searching…" state. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2110_PaletteScoringFault = "HFC2110";

    /// <summary>
    /// Palette recent-route hydrate found no stored value, an unreadable blob, or filtered tampered
    /// entries (Story 3-4 D10). Information severity — feature defaults apply. The <c>Reason</c>
    /// payload is one of <c>Empty</c> (no blob), <c>Corrupt</c> (deserialization / shape failure), or
    /// <c>Tampered</c> (entries failed the <c>IsInternalRoute</c> filter and were dropped). Runtime-only
    /// (no analyzer emission).
    /// </summary>
    public const string HFC2111_PaletteHydrationEmpty = "HFC2111";

    /// <summary>
    /// <c>BadgeCountService</c> encountered an exception during the initial parallel fetch, during
    /// a per-type re-fetch triggered by <c>IProjectionChangeNotifier</c>, or during seen-capability
    /// storage persistence (Story 3-5 D12, D13). Warning severity — the offending operation is
    /// excluded from the result / silently skipped, but the exception never crashes the shell or the
    /// Fluxor error boundary. The structured payload carries
    /// <c>{ProjectionTypeName|CapabilityId, ExceptionType, ExceptionMessage}</c> for operator
    /// correlation. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2112_BadgeInitialFetchFault = "HFC2112";

    /// <summary>
    /// <c>BadgeCountService</c> received a <c>IProjectionChangeNotifier.ProjectionChanged</c>
    /// payload whose string type-name failed <c>Type.GetType(..., throwOnError: false)</c>
    /// resolution (Story 3-5 D7). Information severity — most commonly an adopter mis-registration
    /// (assembly-qualified vs. short-name mismatch). De-duplicated per <c>BadgeCountService</c>
    /// instance (Scoped lifetime) so an operator sees one log per circuit per unresolvable string.
    /// The structured payload carries <c>{TypeNameString}</c>. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2113_ProjectionTypeUnresolvable = "HFC2113";
}
