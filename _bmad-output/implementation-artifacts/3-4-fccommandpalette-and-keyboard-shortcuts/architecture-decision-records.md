# Architecture Decision Records

## ADR-042: `IShortcutService` is the single registration + conflict-detection surface for keyboard shortcuts; native ARIA/DOM behaviours are NOT routed through it

**Status:** Accepted (Story 3-4)

**Context:** UX-DR43 requires framework shortcuts (Ctrl+K, Ctrl+Shift+D, Ctrl+,, g h, `/`) plus conflict prevention with build-time warnings when adopter custom components register conflicting bindings. Story 3-3 shipped `Ctrl+,` as an inline `@onkeydown` handler on `.fc-shell-root` with a `CtrlCommaSingleBindingTest` pinning exactly-one-binding invariant — that was a deliberate v1.0 placeholder awaiting Story 3-4's service infrastructure. The choice: continue with inline bindings scattered across shell components (one `@onkeydown` per shortcut — N shortcuts = N handlers), a Fluxor-action-based pub/sub (every KeyDown dispatches `GlobalKeyPressedAction`, reducers + effects filter), a central `IShortcutService` with a `Register(binding, handler)` API that owns the dictionary, or a JS-side module (`fc-shortcuts.js` intercepts keydown events and invokes a `DotNetObjectReference` callback per binding).

**Decision:** A `public interface IShortcutService` in `Contracts/Shortcuts/` with three methods — `IDisposable Register(string binding, string descriptionKey, Func<Task> handler)`, `IReadOnlyList<ShortcutRegistration> GetRegistrations()`, `Task<bool> TryInvokeAsync(KeyboardEventArgs e)` — is the single registration + dispatch surface. Bindings are normalised via `ShortcutBinding.Normalize(string)` (lowercase, modifier-canonical-order `ctrl+shift+alt+meta+key`, chord `a b` with one space). The service is Scoped (per-circuit). `FrontComposerShell.razor`'s `.fc-shell-root` `@onkeydown` calls `TryInvokeAsync(e)` as its sole routing path — all global shortcuts flow through one dispatcher. **Native ARIA/DOM behaviours are NOT routed through `IShortcutService`** — `Escape` closing a `FluentDialog`, `ArrowUp`/`ArrowDown` on a `role="listbox"`, `Tab` traversal — those remain owned by Fluent UI + browser defaults. Framework shortcuts register via `FrontComposerShortcutRegistrar` (runs once per circuit from `FrontComposerShell.OnAfterRenderAsync`); adopter shortcuts register from adopter's own DI module or `OnInitializedAsync` hooks.

**Rejected alternatives:**

- **Inline `@onkeydown` handlers scattered across shell components (Story 3-3's placeholder approach).** One handler per shortcut means N files to audit for conflicts + N places where the text-input guard must be replicated. Scales poorly past ~3 shortcuts. UX-DR43 explicitly calls for a service.
- **Fluxor-action-based pub/sub (`GlobalKeyPressedAction` + filter reducers/effects).** Every KeyDown → dispatch → enumerate reducers → enumerate effects: overkill for a dispatcher concern. Adds Fluxor action log noise for what is fundamentally a dictionary lookup.
- **JS-side shortcut module (`fc-shortcuts.js` + `DotNetObjectReference`).** Adds a JS↔.NET round-trip per keystroke which inflates latency past NFR5's 100 ms budget. Also bypasses Blazor's `KeyboardEventArgs` event handling — harder to interact with Blazor lifecycles.
- **Routing native ARIA keys (Escape, arrows) through `IShortcutService`.** Would let adopters accidentally override built-in dialog behaviour by registering those keys. Dialog-local keys belong in the dialog's own event handler — separation of concerns.

**Consequences:**

- Story 3-3 D16 migration contract is FULFILLED. `FrontComposerShell.HandleGlobalKeyDown`'s inline `Ctrl+,` branch is deleted. The Story 3-3 `CtrlCommaSingleBindingTest` is deleted as part of the migration (Task 8.4).
- Adopter shortcut registration is one DI extension call: `services.AddScoped<IMyAdopterShortcuts>()` → inside the service's `OnInitialized`, `_shortcuts.Register("ctrl+shift+d", "AdopterDevOverlayDescription", OpenMyOverlayAsync)`.
- Duplicate binding registration (D3) logs `HFC2108_ShortcutConflict` + last-writer-wins — adopter-deliberate-override is supported. Registration order is real-time-imperative-call order, NOT DI-configuration order (see D3 clarification).
- Handler-exception policy (D1 Warning-level `HFC2109_ShortcutHandlerFault`): handler exceptions are caught inside `TryInvokeAsync`, logged, and the method returns `true` — preventing Blazor error-boundary bubble-up. A separate, new diagnostic ID distinct from `HFC2108`.
- Build-time conflict detection across a whole solution (would catch "adopter ships a package that secretly binds Ctrl+K, conflicting with the shell") is deferred to **Story 9-4** Roslyn analyzer.
- The text-input guard lives in ONE place (`.fc-shell-root` `@onkeydown` + `TryInvokeAsync`'s target-element check — D5) instead of being replicated per-handler. Adopter-embedded contenteditable editors (Monaco, CodeMirror) that define their own Ctrl+K semantic must register AFTER the shell (D3) to override — tracked as Known Gap G18.
- The `@` prefix on `PaletteResult.CommandTypeName` is **RESERVED for framework-synthetic palette entries** (e.g., D23's `"@shortcuts"` sentinel). Adopters building palette entries via future extensibility hooks (v1.x) MUST NOT use `@`-prefixed CommandTypeName values; the framework reserves the prefix to unambiguously distinguish synthetic routing from generated command routes.
- **Trust boundary (v1 security model)**: all code running inside `AddHexalithFrontComposer`'s host — including adopter DI extensions, referenced NuGet packages, and Razor components — is assumed to be in the same trust boundary as the framework. The `IShortcutService` interface lives in `Contracts/` and any package referencing Contracts can implement it; DI last-registration-wins means an adopter-supplied implementation REPLACES the framework one without warning. Framework-level defence against hostile adopters (sealed registration via `TryAddScoped` + fail-closed on duplicate) is deliberately NOT shipped because it would break D3's last-writer-wins extensibility for adopter shortcut customisation. Deployers responsible for production trust decisions should audit their dependency tree. Tracked as Known Gap **G20**.

**Verification:** `ShortcutServiceTests.RegisterThenInvoke_RunsHandler`, `ShortcutServiceTests.DuplicateRegister_LogsHFC2108_LastWriterWins`, `ShortcutServiceTests.DisposedRegistration_NoLongerFires`, `ShortcutServiceTests.Chord_gh_InvokesHandler_WithinWindow`, `ShortcutServiceTests.Chord_gh_NonMatchingSecond_FallsBackToFreshLookup` (Task 10.1). Shell-level `FrontComposerShellTests.CtrlCommaInvokesRegisteredShortcut` asserts the Story 3-3 migration (Task 10.10a).

---

## ADR-043: Fuzzy-match scoring runs pure + synchronous in `PaletteScorer.cs`; the 150 ms debounce is an effect-side timer that dispatches `PaletteResultsComputedAction` with the scored set

**Status:** Accepted (Story 3-4)

**Context:** Epic AC §165 requires "results appear after 150 ms debounce with fuzzy matching against bounded context names, projection names, and command names." NFR5 requires search response time < 100 ms. UX spec §107 specifies the 150 ms debounce window. The choice: where does scoring live (reducer, effect, component code-behind, background service), and how is debounce implemented (`Task.Delay` + CancellationToken, `System.Reactive.Linq.Throttle`, Blazor `Timer`, JS-side `setTimeout`).

**Decision:** Scoring is a `public static int Score(string query, string candidate)` function in `PaletteScorer.cs` — zero dependencies, `ToLowerInvariant` + ordinal comparison, three-tier score band (prefix 100+, contains 50+, subsequence 10+, no-match 0). The pure function is called by `CommandPaletteEffects.HandlePaletteQueryChanged` AFTER a 150 ms `Task.Delay(150, ct)` where `ct` is a per-dispatch `CancellationToken` from a rolling `_queryCts` field on the effect class. Contextual bonus (`+15` for in-context results) is applied by the effect after scoring, NOT inside the pure scorer. The effect takes top-50 by score and dispatches `PaletteResultsComputedAction(query, results)`. The reducer for that action assigns `state with { Results = action.Results, SelectedIndex = 0, LoadState = Ready }` — no scoring in the reducer.

**Rejected alternatives:**

- **Scoring inside the reducer.** Reducers are pure static; scoring over `IFrontComposerRegistry.GetManifests()` needs DI. Injecting registry into the reducer violates Fluxor's static discipline (same rationale as Story 3-3 ADR-039 density precedence resolver being effect-side).
- **Scoring inside the component code-behind (`FcCommandPalette.razor.cs`).** Coupling the fuzzy match to the render component makes the scorer untestable in isolation and re-runs on every component instance. Extracting to a pure function lets `PaletteScorerTests` + `PaletteScorerPropertyTests` (FsCheck) run without Blazor.
- **`System.Reactive.Linq.Throttle` for debounce.** Adds a `System.Reactive` dependency for one debounce call site. `Task.Delay + CancellationToken` is the standard .NET async debouncer and already part of the BCL.
- **Blazor `Timer` debounce.** `Timer.Elapsed` fires on a threadpool thread, requires `InvokeAsync(StateHasChanged)` marshalling — same round-trips as `Task.Delay` but with more state to manage (start / stop / dispose per query).
- **JS-side `setTimeout` debounce.** Requires a JS↔.NET trip per keystroke for the cancel signal. Adds complexity without changing user-visible behaviour.
- **Contextual bonus inside the pure scorer.** The scorer would need a `CultureInfo` + current-context parameter, coupling pure logic to state. Keeping the bonus in the effect means `PaletteScorerTests` don't mock navigation state.

**Consequences:**

- `PaletteScorer` is trivially testable with `[Theory]` + FsCheck property tests (monotonic on longer exact prefix, deterministic on same inputs).
- The effect's `_queryCts` field is scoped-per-circuit because `CommandPaletteEffects` is Scoped; no cross-circuit cancellation leakage.
- `OperationCanceledException` from the cancelled `Task.Delay` is expected flow — the effect swallows it without logging to avoid noise on every keystroke.
- **Registry-throw graceful degradation**: if `IFrontComposerRegistry.GetManifests()` throws (e.g., during hot-reload while the registry is rebuilding, or if a corrupt manifest raises an exception during enumeration), the effect catches the exception, logs it at Warning severity (`HFC2109_PaletteScoringFault` — new diagnostic ID), and dispatches `PaletteResultsComputedAction(query, ImmutableArray<PaletteResult>.Empty)` so the palette renders an empty-results UI instead of leaving the user with stuck "Searching…" feedback. The Fluxor error boundary never sees this exception. User-visible outcome: the user sees "No matches found" (same semantics as an empty query result), and operators see a Warning-level telemetry event for post-mortem.
- Top-50 cap means the dispatched action's `Results` array is bounded — no Fluxor memory balloon on a 10000-projection registry.
- `"shortcuts"` special query bypasses `PaletteScorer` entirely — the effect enumerates `IShortcutService.GetRegistrations()` and wraps them as `PaletteResult` with `Category = Shortcut`.

**Verification:** `PaletteScorerTests.*` (5 tests: prefix, contains, subsequence, no-match, case-insensitivity), `PaletteScorerPropertyTests.*` (2 FsCheck properties: determinism, monotonic-on-prefix-length), `CommandPaletteEffectsTests.DebounceCancelsEarlierKeystroke`, `CommandPaletteEffectsTests.ShortcutsQueryBypassesScorer`, `CommandPaletteEffectsTests.ContextualBonusAppliesToMatchingBoundedContext`, `PaletteScorerBench.cs` BenchmarkDotNet micro-bench asserting `< 100 μs / candidate` (Task 10.3, 10.3a, 10.4b).

---

## ADR-044: `IBadgeCountService` is consumed through a nullable constructor injection — absent service means "no badges"; Story 3-5 registers the service and badges appear automatically without palette changes

**Status:** Accepted (Story 3-4)

**Context:** Epic AC §167 requires badge counts from `IBadgeCountService` to appear on ActionQueue-hinted projection results in the command palette. Epic AC §192-194 explicitly says "when the command palette renders before Story 3.5 is implemented, badge counts gracefully degrade to not shown (no errors, no empty badges), and once IBadgeCountService is registered (Story 3.5), counts appear automatically without palette changes." The choice: (a) 3-4 ships a no-op shim `IBadgeCountService` so the palette can assume the service always resolves; (b) 3-4 consumes it via `IServiceProvider.GetService(typeof(IBadgeCountService))` with a null check; (c) 3-4 consumes it via nullable constructor injection (`IBadgeCountService? BadgeCounts`); (d) 3-4 defines a partial `BadgeCountChannelRegistry` that is optionally filled by downstream stories.

**Decision:** 3-4's `FcPaletteResultList.razor.cs` consumes `IBadgeCountService` via **`IServiceProvider.GetService<IBadgeCountService>()` in `OnInitialized`** — NOT via `[Inject]` directly, because Blazor's `[Inject]` uses `GetRequiredService` and throws when the service is unregistered. Pattern: `[Inject] private IServiceProvider ServiceProvider { get; set; } = default!;` → `protected override void OnInitialized() { _badgeCounts = ServiceProvider.GetService<IBadgeCountService>(); if (_badgeCounts is not null) { _badgeSubscription = _badgeCounts.CountChanged.Subscribe(...); } }`. The render path gates on `@if (_badgeCounts is not null && result.Category == PaletteResultCategory.Projection && _badgeCounts.Counts.TryGetValue(result.ProjectionType, out var count))` — when any of those fail, the badge element is not rendered (no placeholder, no empty `<span>`, no error). Story 3-5 registers `services.AddScoped<IBadgeCountService, BadgeCountService>()` and badges appear on the next circuit — no 3-4 code change required. The service interface lives in `Contracts/Badges/IBadgeCountService.cs` (3-4 defines the interface + the `PaletteResult.ProjectionType` shape); Story 3-5 ships the concrete implementation.

**Rejected alternatives:**

- **`[Inject] IBadgeCountService? BadgeCounts` nullable parameter injection.** Blazor's `[Inject]` calls `GetRequiredService` which throws `InvalidOperationException` when the service is unregistered — the nullable annotation does NOT switch Blazor's resolution to `GetService`. This was the first-draft approach, rejected after verifying Blazor DI semantics; `IServiceProvider.GetService` in `OnInitialized` is the correct pattern.
- **Shim `IBadgeCountService` no-op registration in 3-4.** Two competing registrations (shim + real) is a DI footgun — Blazor takes the last registration, which depends on DI-configuration ordering. Adopters unaware of the shim would see palette badges work in 3-4 → break when 3-5 ships → re-work after tracing the double-registration. No-shim is simpler.
- **Partial `BadgeCountChannelRegistry` pattern.** Over-engineered — introduces a new abstraction just to defer a single service. `IServiceProvider.GetService` achieves the same deferral with zero extra types.
- **Register a throwing `IBadgeCountService` in 3-4 + wrap calls in try-catch.** Throw-then-catch-on-every-render has a non-trivial perf cost, and exception-driven feature gates are a smell.

**Consequences:**

- `IBadgeCountService` interface is defined in **`Contracts/Badges/`** in 3-4 (so the palette can reference it) — not in the Shell assembly (where Story 3-5's implementation will live). The interface surface (`IReadOnlyDictionary<Type, int> Counts`, `IObservable<BadgeCountChangedArgs> CountChanged`, `int TotalActionableItems`) is frozen by 3-4; Story 3-5 implements against the frozen shape.
- **Lifetime requirement (XML-doc-level contract)**: implementations of `IBadgeCountService` MUST register as **Scoped** (per-circuit / per-user). The palette's `FcPaletteResultList.OnInitialized` subscribes to `CountChanged` via `Subscribe(_ => InvokeAsync(StateHasChanged))` and disposes in `DisposeAsync` — a Singleton implementation would leak subscriptions across circuits, cause cross-user state bleed, and likely produce `InvalidOperationException` from `StateHasChanged` calls into disposed render trees. The interface file carries the `<remarks>Implementations MUST register as Scoped to preserve per-circuit subscription semantics.</remarks>` XML doc at the interface declaration. Story 3-5's implementation registers via `services.AddScoped<IBadgeCountService, BadgeCountService>()` — verified at Story 3-5 merge time.
- `PaletteResult.ProjectionType : Type?` (nullable) is new — 3-4's effect populates it from the `PaletteResult`'s source manifest when `Category == Projection`; other categories leave it null.
- No shim, no feature-flag, no try-catch: the `IServiceProvider.GetService<T>()` call IS the switch.
- Story 3-5 can register + unregister `IBadgeCountService` in tests without a teardown-ordering worry — palette tests that want no badges simply don't register.
- Real-time `CountChanged` subscription (Epic AC §193) is implemented in `FcPaletteResultList.OnInitialized`: `var svc = ServiceProvider.GetService<IBadgeCountService>(); if (svc is not null) { _subscription = svc.CountChanged.Subscribe(_ => InvokeAsync(StateHasChanged)); }` — null-guarded, disposed in `IAsyncDisposable.DisposeAsync`.

**Verification:** `FcPaletteResultListTests.RendersWithoutBadges_WhenBadgeServiceIsNull`, `FcPaletteResultListTests.RendersBadges_WhenBadgeServiceIsRegistered_AndProjectionMatches`, `FcPaletteResultListTests.NoBadgePlaceholder_WhenBadgeServiceResolvesButProjectionUnknown` (Task 10.8). The "Story 3-5 lands → palette updates" scenario is covered structurally by the first two tests (one with a mock `IBadgeCountService`, one without) — no integration test needed because the contract is unchanged.

---
