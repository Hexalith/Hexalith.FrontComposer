# Dev Agent Cheat Sheet (Read First — 2 pages)

> Amelia-facing terse summary. Authoritative spec is the full document. Every line links to a section for detail.

**Goal:** Land the framework-owned **command-palette + keyboard-shortcut infrastructure** that completes Epic 3's "power-user navigation" deliverable: (1) a `public interface IShortcutService` in Contracts/Shortcuts/ with `Register(binding, descriptionKey, handler) : IDisposable` + `GetRegistrations()` + `TryInvokeAsync(KeyboardEventArgs)` — normalised-binding grammar `ctrl+shift+alt+meta+key` and chord `a b`; Scoped lifetime; `ConcurrentDictionary` backing store; last-writer-wins duplicate handling with `HFC2107_ShortcutConflict` Information log (D1 / D2 / D3 / ADR-042); (2) `FrontComposerShortcutRegistrar` runs on `FrontComposerShell.OnAfterRenderAsync(firstRender)` registering `ctrl+k` (palette), `ctrl+,` (settings — MIGRATES Story 3-3 D16 inline binding — AC8), `g h` (home chord) per D4 / D5 / AC1; (3) `FrontComposerShell.razor`'s `.fc-shell-root` `@onkeydown` DELETES Story 3-3's inline Ctrl+, branch and now calls `IShortcutService.TryInvokeAsync(e)` with a text-input-target guard per D5; (4) new `FrontComposerCommandPaletteFeature` Fluxor feature — state `(bool IsOpen, string Query, ImmutableArray<PaletteResult> Results, ImmutableArray<string> RecentRouteUrls, int SelectedIndex, PaletteLoadState LoadState)`; 8 actions (`PaletteOpenedAction`, `PaletteClosedAction`, `PaletteQueryChangedAction`, `PaletteResultsComputedAction`, `PaletteSelectionMovedAction`, `PaletteResultActivatedAction`, `RecentRouteVisitedAction`, `PaletteHydratedAction`); 8 pure-static reducers (no DI, no scoring) per D6 / D8; (5) `CommandPaletteEffects` with a 150 ms `Task.Delay(150, ct)` debounce + per-dispatch `_queryCts` cancellation (D9); enumerates `IFrontComposerRegistry.GetManifests()`, scores each candidate via `PaletteScorer.Score(query, candidate.Name)`, applies `+15` contextual bonus when `candidate.BoundedContext == navigationState.CurrentBoundedContext` (D7), takes top-50, dispatches `PaletteResultsComputedAction` (D8 / ADR-043); (6) "shortcuts" special query bypasses the scorer and returns `IShortcutService.GetRegistrations()` mapped to `PaletteResult` rows (AC6); (7) recent-route ring buffer size 5 persisted under `{tenantId}:{userId}:palette-recent` with `TryResolveScope` fail-closed guard (D10 — mirrors Story 3-1/3-2/3-3 ADR-029 + ADR-038); hydrate is read-only (D10); (8) `FcCommandPalette.razor` dialog content component (`IDialogContentComponent`) hosting `FluentSearch` + `FcPaletteResultList` + visually-hidden `role="status" aria-live="polite"` result-count region (D11 / D15 — reuses Story 3-3 D20 `.fc-sr-only` utility class); `Escape` / `ArrowUp` / `ArrowDown` / `Enter` handled via dialog-local `@onkeydown` — NOT routed through `IShortcutService` (D17); (9) `FcPaletteResultList.razor` renders `<ul role="listbox" aria-activedescendant>` with `<li role="option">` children grouped into Projection / Command / Recent / Shortcut categories (D14); (10) `IBadgeCountService` defined in `Contracts/Badges/` + consumed via nullable `[Inject]` — absent → no badge; Story 3-5 registers the impl and badges appear automatically (D16 / ADR-044); `CountChanged` subscription in `OnInitialized` null-guarded, disposed in `DisposeAsync`; (11) `FcPaletteTriggerButton.razor` header icon auto-populates `HeaderEnd` BEFORE `FcSettingsButton` (D18 — extends Story 3-3 D12 pattern); (12) 12 new resource keys EN + FR with `CanonicalKeysHaveFrenchCounterparts` parity auto-covered (D14); (13) Story 3-3 `CtrlCommaSingleBindingTest` DELETED per Story 3-3 D16 migration contract + replaced with `FrontComposerShellTests.CtrlCommaInvokesRegisteredShortcut` (AC8 / Task 8.4).

**Scope boundary:** 3-4 is the **Epic 3 power-user navigation body** that completes the shell keyboard-shortcut infrastructure and ships the FcCommandPalette. Build-time Roslyn analyzer for `IShortcutService.Register` unique-binding enforcement → **Story 9-4**. Concrete `IBadgeCountService` implementation + SignalR hub subscription → **Story 3-5** (3-4 defines the interface in Contracts + consumes via nullable injection so 3-5 activates seamlessly). In-palette command invocation (type "increment" → Enter → inline form inside palette) → **v1.x**. Per-adopter custom palette categories → **v1.x**. Fuzzy scoring I18n (Turkish-I, NFKC) → **v2**. Virtual scrolling past 50 results → **v2**. `forced-colors` + RTL verification → **Story 10-2** / **v2** (mirrors Story 3-3 G8 / G9). Density parity specimen + Playwright screenshot diff across themes × densities → **Story 10-2**. Custom chord timeout (ChordTimeoutMs option) → **v1.x** (fixed 1500 ms in v1). 3-4 inherits — does not re-litigate — Story 3-1's zero-override + `LocalStorageService` scoping (ADR-030) + `IUserContextAccessor` fail-closed (ADR-029) + scoped CSS filename contract (ADR-034); Story 3-2's `ViewportTier` enum (unused but read-only available) + `ADR-038` hydrate-is-read-only; Story 3-3's `IDialogService` direct-invocation pattern (D11) + `HeaderEnd` auto-populate pattern (D12) + `.fc-sr-only` CSS utility class (D20).

**Binding contracts with existing Epic 1 + 2 + 3-1 + 3-2 + 3-3 deliverables:**

- **Story 3-3 D16 `Ctrl+,` migration contract** — FULFILLED. Delete `FrontComposerShell.HandleGlobalKeyDown`'s inline `Ctrl+,` branch; route all keys through `IShortcutService.TryInvokeAsync`. Delete `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs`.
- **Story 3-3 D12 `HeaderEnd` auto-populate** — EXTENDED. When `HeaderEnd is null`, render `<FcPaletteTriggerButton />` ahead of `<FcSettingsButton />` in a `FluentStack Horizontal gap="4px"`.
- **Story 3-3 D20 `.fc-sr-only` utility class** — REUSED verbatim for the palette's `aria-live` result-count region. No new CSS.
- **Story 3-3 D11 `IDialogService.ShowDialogAsync` direct invocation (no Fluxor action for dialog open)** — PATTERN REUSED for `FcCommandPalette`. BUT: unlike 3-3, 3-4's palette state has `IsOpen` field for shortcut arbitration (idempotent Ctrl+K — D12). The `IsOpen` field shadows `IDialogService`'s internal state only for the arbitration path.
- **Story 3-3 D17 13 resource keys** — 3-4 appends 12 more (palette + shortcut descriptions). Parity test `CanonicalKeysHaveFrenchCounterparts` auto-covers.
- **Story 3-2 `FrontComposerNavigationState`** — EXTENDED. Append `string? CurrentBoundedContext` nullable field (Task 2.1a). Not persisted (ADR-037 viewport-precedent generalises).
- **Story 3-1 `LocalStorageService` + ADR-030 Scoped lifetime** — `CommandPaletteEffects` keeps its constructor injection; no change.
- **Story 3-1 ADR-029 `IUserContextAccessor` fail-closed** — `CommandPaletteEffects.TryResolveScope` stays the same pattern. Null/empty/whitespace tenant OR user → log `HFC2105` Information + return early.
- **Story 3-1 `StorageKeys.BuildKey(tenantId, userId, feature)`** — 3-4 uses the 3-segment form with `feature = "palette-recent"`.
- **Story 3-1 `FcShellResources` localization infrastructure** — 3-4 adds 12 keys to BOTH resx files.
- **Story 3-1 `FcShellOptions`** — 3-4 does NOT extend. Property count stays at 15 (post-3-3). G1 options-class split stays deferred to Story 9-2.
- **Story 2-2 generated `/domain/{CommandName}` FullPage route** — `HandlePaletteResultActivated` navigates here for `Category == Command`. No generator change required.
- **Story 2-3 single-writer invariant (D19)** — 3-4's reducer set is the single write path into `FrontComposerCommandPaletteState`. No bypass. ULID correlation via `IUlidFactory.NewUlid()` for every action.
- **Story 1-3 per-concern Fluxor feature pattern** — 3-4 adds `CommandPalette` (4th feature after Theme/Density/Navigation). Follows `Shell/State/{Concern}/` layout.

**ADR-042 one-liner:** `IShortcutService` is the SINGLE global-shortcut registration + dispatch surface. `Register(binding, descriptionKey, handler) : IDisposable` (grammar `ctrl+shift+alt+meta+key` normalised lowercase; chord `a b` with single space; throws on bare single letters + empty). Scoped lifetime; `ConcurrentDictionary` backing store; duplicate = `HFC2107` Information + last-writer-wins. `FrontComposerShortcutRegistrar` registers 3 shell defaults (`ctrl+k` palette, `ctrl+,` settings, `g h` home). Native ARIA/DOM keys (Escape/arrows/Tab in dialogs + listboxes) stay owned by Fluent UI + browser — NOT routed through the service. Rejected: inline per-handler (N-file audit), Fluxor-action pub/sub (overkill), JS-side module (latency), routing ARIA keys through the service (widens purview).

**ADR-043 one-liner:** `PaletteScorer.Score(query, candidate) : int` is a pure static 3-band fuzzy matcher — prefix 100+, contains 50+, subsequence 10+, no-match 0. Called by `CommandPaletteEffects` AFTER a 150 ms `Task.Delay(150, ct)` debounce with per-dispatch `CancellationTokenSource`. Contextual `+15` bonus applied by effect (not scorer) when `candidate.BoundedContext == currentBoundedContext`. Top-50 by score. Reducer stays pure static (receives pre-computed Results). Rejected: scoring in reducer (needs DI), scoring in component code-behind (untestable + re-runs per instance), System.Reactive.Linq.Throttle (new dep for one call site), Blazor Timer (more state to manage), JS-side setTimeout (round-trip), contextual bonus in pure scorer (couples logic to state).

**ADR-044 one-liner:** `IBadgeCountService` is defined in `Contracts/Badges/IBadgeCountService.cs` (interface surface frozen by 3-4: `IReadOnlyDictionary<Type,int> Counts`, `IObservable<BadgeCountChangedArgs> CountChanged`, `int TotalActionableItems`). Story 3-5 ships the implementation. `FcPaletteResultList` consumes via `IServiceProvider.GetService<IBadgeCountService>()` in `OnInitialized` (NOT `[Inject] IBadgeCountService?` — Blazor's `[Inject]` uses `GetRequiredService` and throws on unregistered services regardless of nullable annotation). Absent service → badge element NOT rendered (no placeholder). Story 3-5 registration → badges appear with no 3-4 code change. Rejected: `[Inject] IBadgeCountService?` nullable parameter (Blazor throws on unregistered), shim no-op registration (double-registration footgun), partial registry pattern (over-engineered), throwing shim + try-catch (exception-driven feature gate).

**Files to create / extend:**

| Path | Action |
|---|---|
| `src/Hexalith.FrontComposer.Contracts/Shortcuts/IShortcutService.cs` | Create — interface (Task 1.1, D1). |
| `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutRegistration.cs` | Create — record (Task 1.1a, D1, D14). |
| `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs` | Create — `public static class` normaliser (Task 1.1b, D1, D4). |
| `src/Hexalith.FrontComposer.Contracts/Badges/IBadgeCountService.cs` | Create — interface frozen by 3-4 (Task 6.3, D16 / ADR-044). |
| `src/Hexalith.FrontComposer.Contracts/Badges/BadgeCountChangedArgs.cs` | Create — record (Task 6.3). |
| `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs` | Create — internal impl (Task 1.2, D2). |
| `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs` | Create — registers 3 shell shortcuts on first render (Task 1.4). |
| `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteFeature.cs` + State.cs + Actions.cs + Reducers.cs | Create — Fluxor feature (Task 2.1-2.5, D6, D8). |
| `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` | Create — debounce + scoring + persistence (Task 3.1-3.7, D7, D8, D9, D10, D13). |
| `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs` | Create — record + category enum (Task 2.4, D14). |
| `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs` | Create — pure static fuzzy matcher (Task 4.1, D7, ADR-043). |
| `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` | Modify — append `string? CurrentBoundedContext` (Task 2.1a). |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor` + `.razor.cs` + `.razor.css` | Create — dialog content (Task 5.1-5.4, D11, D15, D17). |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor` + `.razor.css` | Create — listbox component (Task 6.1-6.2, D14, D16, D18). |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor` + `.razor.cs` | Create — header icon (Task 7.1, D18). |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` | Modify — insert `<FcPaletteTriggerButton />` in `HeaderEnd` auto-populate ahead of `<FcSettingsButton />` (Task 7.2). |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` | Modify — delete Story 3-3 inline `Ctrl+,` branch; route `HandleGlobalKeyDown` through `IShortcutService.TryInvokeAsync` (Task 8.1-8.3, D5 + AC8). |
| `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` | Modify — 12 new keys (Task 9.1-9.2, D14). |
| `src/Hexalith.FrontComposer.Shell/ServiceCollectionExtensions.cs` (or equivalent) | Modify — register `IShortcutService` + `FrontComposerShortcutRegistrar` Scoped (Task 1.5). |
| `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` | Modify — add `HFC2107_ShortcutConflict` Information row (Task 1.3). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` | Create — 6 tests (Task 10.1, D1-D4, AC1). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutBindingNormalizeTests.cs` | Create — 4 theory cases (Task 10.2). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/HFC2107ShortcutConflictLogTest.cs` | Create — structured log assertion (Task 10.1e, D19). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerTests.cs` | Create — 5 theory cases (Task 10.3, D7). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerPropertyTests.cs` | Create — 2 FsCheck properties (Task 10.3a). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerBench.cs` | Create — BenchmarkDotNet opt-in (Task 10.3b, NFR5). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteReducerTests.cs` | Create — 6 reducer tests (Task 10.4). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs` | Create — 5+ tests incl debounce, shortcuts-query, contextual bonus, activation, hydrate-read-only (Task 10.4b, 10.4c). |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs` | Create — 4 fail-closed-scope tests (Task 10.5, AC3 + Story 3-1/3-2/3-3 parity). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs` | Create — 5 bUnit tests (render, auto-focus, arrow, Enter, Escape, aria-live) (Task 10.7, 10.7b). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs` | Create — 5 bUnit tests incl badge nullable-DI scenarios (Task 10.8, D16 / ADR-044). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteTriggerButtonTests.cs` | Create — 1 bUnit test (Task 10.9, D18). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` | Modify — 4 new tests (Task 10.10, 10.10a). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` | Modify — 6 lookups for 12 new keys (Task 10.11). |
| `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs` | Create — Playwright hardened per Story 3-3 precedent (Task 10.12). |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` | **DELETE** — explicit retirement per Story 3-3 D16 migration contract (Task 8.4 + AC8). |

**AC quick index:**

| AC | One-liner | Task(s) |
|---|---|---|
| AC1 | `IShortcutService` registers 3 shell defaults with HFC2107 duplicate-detection; `Disposable.Dispose()` removes entries (D1-D3, D19). | 1.1, 1.2, 1.3, 1.4, 10.1, 10.1e |
| AC2 | Ctrl+K / header icon opens palette; dialog with role + aria-label + auto-focused search + aria-activedescendant (D6, D11, D12, D18). | 5.1, 5.2, 7.1, 7.2, 10.7, 10.10 |
| AC3 | 150 ms debounce + pure-function PaletteScorer + 3 categories rendered; sub-100 ms scoring benchmark (D7, D8, D9, ADR-043). | 3.1, 4.1, 5.1, 6.1, 10.3, 10.3a, 10.3b, 10.4b |
| AC4 | Contextual +15 bonus for current-bounded-context results (D7, D8). | 3.1, 10.4b |
| AC5 | Arrow/Enter/Escape native dialog handling; aria-live result count (D15, D17). | 5.2, 5.3, 5.4, 10.7, 10.7b |
| AC6 | "shortcuts" query bypasses scorer, returns `IShortcutService.GetRegistrations()` (D1, D14). | 3.2, 6.1, 10.4b, 10.8 |
| AC7 | `IBadgeCountService` nullable `[Inject]` graceful degradation (D16, ADR-044). | 6.1, 6.2, 10.8 |
| AC8 | Story 3-3 Ctrl+, inline binding migrated to `IShortcutService.Register`; `CtrlCommaSingleBindingTest` DELETED (D1, D5, Story 3-3 D16 migration). | 8.1, 8.2, 8.3, 8.4, 10.10a |

**Scope guardrails (do NOT implement — see Known Gaps):**

- Build-time Roslyn analyzer for `IShortcutService.Register` uniqueness → **Story 9-4**.
- `IBadgeCountService` concrete implementation → **Story 3-5** (3-4 defines interface only).
- In-palette command invocation (inline form) → **v1.x**.
- Per-adopter custom palette categories → **v1.x** (Epic 6 gradient concern).
- Fuzzy scoring I18n → **v2**.
- Virtual scrolling past 50 results → **v2**.
- `forced-colors` + RTL verification → **Story 10-2** / **v2**.
- Density parity specimen screenshot diff → **Story 10-2**.
- Configurable chord timeout → **v1.x** (fixed 1500 ms in v1).
- Adopter-overridable palette copy — already supported via existing `IStringLocalizer<FcShellResources>` replacement pattern (Story 3-1 D21). No new framework surface.

**One new diagnostic code reserved:** `HFC2107_ShortcutConflict` at Information severity — matches Story 3-1/3-2/3-3 "one diagnostic per behavioural category" precedent. No existing diagnostic covers "adopter deliberately overrode a framework binding" — this is a new category.

**Test expectation: ~34 new tests** (L07 budget: 34 / 19 decisions = 1.8 — within Murat's 1.6-2.3 range). Breakdown: 6 shortcut-service + 4 binding-normalize + 1 HFC2107-log + 5 scorer + 2 scorer-property (FsCheck) + 1 scorer-bench (opt-in, not counted in regular pass) + 6 reducer + 5+2 effects + 4 effects-scope + 4 palette-component + 5 result-list + 1 trigger-button + 4 shell-extend + 6 resource-lookups + 1 Playwright palette E2E = **~56**; trimmed ~22 via Occam (redundant HFC2107 payload-field assertions merged into one test; overlapping scorer theory cases; exhaustive shortcut-registration scenarios merged; excess resource-lookup per-key) → **~34**. PR-review gate at Task 10.13 confirms or trims further. Flakiness mitigation for the single Playwright E2E: `prefers-reduced-motion` harness override + `expect.poll` + DOM-attribute-on-palette-root assertion.

**Start here:** Task 0 (FluentSearch / FluentBadge spike + `KeyboardEventArgs.MetaKey` check + test baseline) → Task 1 (IShortcutService + ShortcutService + Registrar + HFC2107) → Task 2 (Fluxor feature state + actions + reducers) → Task 3 (effects with debounce + scoring + persistence) → Task 4 (PaletteScorer pure function) → Task 5 (FcCommandPalette dialog content) → Task 6 (FcPaletteResultList + IBadgeCountService Contracts interface) → Task 7 (FcPaletteTriggerButton + HeaderEnd auto-populate update) → Task 8 (Story 3-3 Ctrl+, migration + CtrlCommaSingleBindingTest deletion) → Task 9 (resource keys) → Task 10 (tests) → Task 11 (zero-warning gate + Aspire MCP verification).

**The 19 Decisions and 3 ADRs are BINDING. Do not revisit without raising first.**

---
