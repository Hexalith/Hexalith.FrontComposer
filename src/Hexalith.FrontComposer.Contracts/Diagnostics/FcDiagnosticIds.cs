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

    /// <summary>
    /// Build-time Warning: <c>[ProjectionRole(..., WhenState = "...")]</c> references an
    /// enum member that does not exist on the projection's status-enum type (Story 4-1
    /// D3 / D17 / AC9). Unknown members flow through to the IR and silently never match
    /// at runtime. Emitted by the source generator at the attribute's call site.
    /// </summary>
    public const string HFC1022_ProjectionWhenStateMemberUnknown = "HFC1022";

    /// <summary>
    /// Build-time Information: <c>[ProjectionRole(ProjectionRole.Dashboard)]</c> falls back
    /// to Default DataGrid rendering in v1 (Story 4-1 D16 / D17 / AC10). Dashboard role is
    /// reserved — full rendering lands in Story 6-3. Emitted once per Dashboard-annotated
    /// projection per compilation by the <c>IIncrementalGenerator</c> per-input invocation
    /// model.
    /// </summary>
    public const string HFC1023_ProjectionRoleDashboardFallback = "HFC1023";

    /// <summary>
    /// Build-time Warning: a numeric <c>ProjectionRole</c> value outside the declared enum
    /// was carried by the <c>[ProjectionRole]</c> attribute — typically the result of an
    /// unsafe cast such as <c>(ProjectionRole)999</c> (Story 4-1 D15 / D17 / AC7). Renderer
    /// falls back to Default rendering; build remains green.
    /// </summary>
    public const string HFC1024_UnknownProjectionRoleValue = "HFC1024";

    /// <summary>
    /// Build-time Information: a projection enum column carries <c>[ProjectionBadge]</c> on
    /// some members but not all (Story 4-2 D6 / AC3). The generator emits semantic badges
    /// for annotated members and falls back to humanized text for unannotated members.
    /// Deduped per (projection, enum) pair so the same mixed enum is reported once per
    /// projection it appears on. Fix: annotate every enum member or remove all annotations
    /// for visual consistency.
    /// </summary>
    public const string HFC1025_BadgeSlotFallbackApplied = "HFC1025";

    /// <summary>
    /// Build-time Warning (RESERVED): a rendered badge is missing its visible text label
    /// (Story 4-2 D7). Unreachable from generated code because <c>FcStatusBadge.Label</c>
    /// is <c>EditorRequired</c> — the reservation is held for Story 10-2's specimen
    /// checker to flag adopter-authored custom badges that violate UX-DR30 commitment #1.
    /// No call sites in Story 4-2.
    /// </summary>
    public const string HFC1026_ColorOnlyBadgeDetected = "HFC1026";

    /// <summary>
    /// Build-time Information: a projection carries at least one Collection-typed column
    /// (<c>IReadOnlyList&lt;T&gt;</c> / <c>HashSet&lt;T&gt;</c> / <c>Dictionary&lt;,&gt;</c> / etc.)
    /// whose column header renders no filter affordance (Story 4-3 D14 / D20). Information
    /// severity — filter UI is omitted per D14; adopters needing collection-aware filters
    /// override via the Epic 6 Slot-level customization path. Per-projection deduped: one
    /// diagnostic per projection type regardless of how many Collection columns it carries.
    /// </summary>
    public const string HFC1027_CollectionColumnNotFilterable = "HFC1027";

    /// <summary>
    /// Build-time Information: two or more properties on a projection declare the same
    /// explicit <c>[ColumnPriority]</c> value (Story 4-4 D14 / D15). Deterministic fallback is
    /// declaration order within the tied priority. Fire once per colliding priority value per
    /// projection type. Fix: give each annotated property a distinct priority.
    /// </summary>
    public const string HFC1028_ColumnPriorityCollision = "HFC1028";

    /// <summary>
    /// Build-time Information: projection exceeds the 15-column auto-generation limit
    /// (UX-DR63 / Story 4-4 D15). <c>FcColumnPrioritizer</c> wraps the grid at runtime showing
    /// the first 10 columns by priority; remaining columns hide behind the "More columns" gear.
    /// Per-projection deduped. Fix: annotate columns with <c>[ColumnPriority]</c> to control
    /// the default-visible subset.
    /// </summary>
    public const string HFC1029_ColumnPrioritizerActivated = "HFC1029";

    /// <summary>
    /// Build-time Information: a <c>[ProjectionFieldGroup]</c> annotation declares a group
    /// name that case-insensitively collides with the reserved catch-all label
    /// "Additional details" (Story 4-5 D9 / D16). Fail-soft pass-through — the colliding
    /// group renders alongside the catch-all bucket. Per-projection deduped. Fix: rename
    /// the group.
    /// </summary>
    public const string HFC1030_FieldGroupNameCollidesWithCatchAll = "HFC1030";

    /// <summary>
    /// Build-time Information: a projection annotated <c>[ProjectionRole(Timeline)]</c>
    /// also carries one or more <c>[ProjectionFieldGroup]</c> annotations (Story 4-5 D17).
    /// Timeline has no detail body, so the grouping is silently unused. Per-projection
    /// deduped. Fix: remove the annotations or change the projection role.
    /// </summary>
    public const string HFC1031_FieldGroupIgnoredForNonDetailRole = "HFC1031";

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
    /// Navigation effect hydrated with no stored value on app init, the stored blob was unreadable,
    /// or the persisted <c>LastActiveRoute</c> was pruned (Story 3-2 D15 amended; Story 3-6 D21).
    /// Information severity — feature defaults apply. The <c>Reason</c> payload distinguishes
    /// <c>Empty</c> (no blob), <c>Corrupt</c> (deserialization / shape failure),
    /// <c>Invalid</c> (stored route failed internal-route / base-path validation → pruned),
    /// <c>OutOfScope</c> (stored route's bounded context is no longer registered → pruned),
    /// <c>RegistryFailure</c> (registry enumeration threw — route preserved). Runtime-only
    /// (no analyzer emission).
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

    /// <summary>
    /// <c>DataGridNavigationEffects</c> hydrate-side observation — either the storage key
    /// enumeration returned no blob at an expected key (race between <c>GetKeysAsync</c> and
    /// <c>GetAsync</c>), a blob failed deserialisation, the viewKey's bounded-context is no longer
    /// registered in <c>IFrontComposerRegistry.GetManifests()</c>, or the registry itself threw
    /// during the hydrate pass (Story 3-6 D11 / D14 / A9). Information severity — feature defaults
    /// apply per key, hydrate continues for remaining keys. The <c>Reason</c> payload is one of
    /// <c>Empty</c> (no blob at key), <c>Corrupt</c> (deserialisation / shape failure — per-key
    /// try/catch isolation), <c>OutOfScope</c> (viewKey's BC is no longer registered; key is
    /// pruned via <c>RemoveAsync</c>), or <c>RegistryFailure</c> (registry enumeration threw;
    /// pruning abandoned for that pass — data preserved). <c>RegistryFailure</c> is deduped
    /// once-per-hydrate-pass; <c>OutOfScope</c> is deduped once-per-distinct-viewKey via
    /// instance-scoped <c>ConcurrentDictionary</c> (mirrors Story 3-5 D7 dedup). The structured
    /// payload carries <c>{ViewKey}</c> when applicable. Runtime-only (no analyzer emission).
    /// </summary>
    public const string HFC2114_DataGridHydrationEmpty = "HFC2114";
}
