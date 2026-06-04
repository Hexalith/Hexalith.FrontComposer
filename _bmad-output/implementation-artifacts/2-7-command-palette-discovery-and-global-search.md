---
baseline_commit: ad6c78e7a23a43a08af2864f6fd452ad8a856360
---

# Story 2.7: Command palette discovery and global search

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Stories 2.1–2.6, the **command-palette + registry search surface already exists, builds, is mounted,
> and is heavily tested** at baseline `ad6c78e`. The shipped code carries **`Story 3-4` / `3-5` / `3-6` /
> `3-7` / `4-3` + `D##` / `P##`** docstring markers — the palette, shortcut framework, scorer, hydration
> lifecycle, badge integration, e2e-palette lane, and the grid global-search input were all authored **ahead
> of this epic's confirm-and-pin numbering** (exactly as Story 2.6's reconnect engine carried `Story 5-x`
> markers). **That is expected — do NOT "re-attribute", renumber, or churn it.**
>
> **What is already built, mounted, and pinned (do NOT rebuild, restyle, or re-wire):**
> - **`Ctrl+K` / `Cmd+K` → palette opens** — `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync`
>   registers `"ctrl+k"` **and** `"meta+k"` (D25 macOS parity) → `OpenPaletteAsync()` →
>   `IDialogService.ShowDialogAsync<FcCommandPalette>(...)`. The shell routes keydown via
>   `FrontComposerShell.HandleGlobalKeyDown` → `IShortcutService.TryInvokeAsync`. **The palette is opened
>   on-demand as a FluentUI dialog — it is intentionally NOT statically rendered in the shell template**
>   (`IDialogService` pattern). Pinned by `FrontComposerShellTests.CtrlKOpensPaletteDialogViaShortcutService`
>   + `CommandPaletteE2ETests.AC4_MetaKChord_DispatchesSamePaletteHandler_AsCtrlK`.
> - **ARIA combobox shell** — `FcCommandPalette.razor` renders the search input with `role="combobox"`,
>   `aria-haspopup="listbox"`, `aria-expanded` (true⇔results non-empty), `aria-controls="@ResultListId"`,
>   `aria-activedescendant="@ActiveDescendantId"`, `aria-autocomplete="list"`, plus a `role="status"
>   aria-live="polite" aria-atomic="true"` announce region. `FcPaletteResultList.razor` renders
>   `<ul role="listbox" aria-activedescendant>` → grouped `<li role="option" aria-selected aria-disabled>`.
>   **The trigger button `FcPaletteTriggerButton` IS mounted** in `FrontComposerShell.razor:77` (default
>   `HeaderEnd`). Partially pinned by `FcCommandPaletteTests` (7) + `FcPaletteResultListTests` (9).
> - **Keyboard navigation** — `FcCommandPalette.HandleKeyDownAsync` handles ArrowDown/ArrowUp
>   (`PaletteSelectionMovedAction(±1)`), Enter (`PaletteResultActivatedAction`), Escape
>   (`PaletteClosedAction`); browser-default suppression via the `fc-keyboard.js` JS module
>   (`registerPaletteKeyFilter`). Reducer clamp/selection logic pinned by `CommandPaletteReducerTests` (16).
> - **Live registry filtering (the search core)** — `CommandPaletteEffects.HandlePaletteQueryChanged` (150ms
>   debounce, per-dispatch CTS cancellation) iterates `IFrontComposerRegistry.GetManifests()` → scores
>   projections + commands via the pure `PaletteScorer` (three-band prefix/substring/fuzzy, rune-aware),
>   applies a +15 current-context bonus, filters unreachable commands (D21 `HasFullPageRoute`) + unauthorized
>   commands (`ICommandAuthorizationEvaluator`), ranks `OrderByDescending(Score).Take(50)`, and surfaces a
>   `shortcuts`-alias help path (`? / help / keys / kb / shortcut → "shortcuts"`). Pinned by
>   `CommandPaletteEffectsTests` (21) + `PaletteScorerTests` (5) + `PaletteScorerPropertyTests` (FsCheck).
> - **Recent-routes ring buffer + hydration + fail-closed scope** — `RecentRouteUrls` (cap 5, LRU,
>   per-`{tenantId}:{userId}:palette-recent`), `HydrationState` lifecycle (D19), tenant/user scope-clear
>   (DN2). Pinned by `CommandPaletteEffectsScopeTests` (2) + reducer/effect tests.
> - **e2e-palette CI lane** — `CommandPaletteE2ETests` (4) drives the full open→type→filter→activate→navigate
>   →persist flow through the **real** reducer+effect pipeline, **but carries `[Trait("Category",
>   "e2e-palette")]` and is EXCLUDED from the default test lane** (`Category!=…&Category!=e2e-palette&…`,
>   enforced by `CiGovernanceTests`). It runs in a dedicated lane (Story 3-7 D6).
> - **Grid global-search input** — `FcProjectionGlobalSearch` (Story 4-3 D6) is a **per-grid** 300ms-debounced
>   `role="combobox"` input that dispatches `GlobalSearchChangedAction(viewKey, payload)`; it is rendered by a
>   **generated view only when an `IProjectionSearchProvider<T>` is registered** — it is NOT the registry-wide
>   palette search and is NOT mounted in the shell chrome.
>
> **So this story's job is to (1) VERIFY both ACs hold end-to-end against `src/` at `ad6c78e`, (2) CLOSE the
> genuine durability gaps in the pins, and (3) resolve the AC2 `FcProjectionGlobalSearch` wording honestly**
> so 2.8 (FC-TBL confirm) builds on a pinned discovery baseline. **Default to ZERO `src/` change** — the
> palette is feature-complete; the deliverable is durable pins + an honest AC2 disposition. See the gaps below.

## Acceptance Criteria

> **AC source:** [_bmad-output/planning-artifacts/epics.md#Story 2.7]. ACs reproduced verbatim, then sharpened
> against `src/` reality at `ad6c78e`. **FR14 (nav/home/palette/badges), UX-DR4 (reusable interaction
> components — `FcCommandPalette` ARIA combobox).**

**AC1 — Keyboard-driven palette opens as an ARIA combobox with keyboard-navigable results. *(FR14, UX-DR4)***
**Given** the shell is focused (a circuit with the global keydown router wired —
`FrontComposerShell` root `@onkeydown="HandleGlobalKeyDown"`),
**When** the operator presses **`Ctrl+K`** (or `Cmd+K`/`Meta+K` on macOS — D25 parity),
**Then** `IShortcutService.TryInvokeAsync` resolves the `"ctrl+k"`/`"meta+k"` binding to
`FrontComposerShortcutRegistrar.OpenPaletteAsync`, which dispatches `PaletteOpenedAction` and opens
**`FcCommandPalette`** via `IDialogService.ShowDialogAsync<FcCommandPalette>` (idempotent — a second
`Ctrl+K` while open is a no-op, D12),
**And** the palette renders as an **ARIA combobox**: a focused search input with `role="combobox"`,
`aria-haspopup="listbox"`, `aria-expanded` reflecting result presence, `aria-controls` →
`FcPaletteResultList`, `aria-activedescendant` tracking the selected row, and `aria-autocomplete="list"`;
results render as `<ul role="listbox">` → `<li role="option" aria-selected>` grouped by category;
ArrowDown/ArrowUp move the selection (`PaletteSelectionMovedAction(±1)`, clamped, no wrap), Enter activates
(`PaletteResultActivatedAction`), Escape closes (`PaletteClosedAction`), and a `role="status"
aria-live="polite"` region announces the result count.

**AC2 — Live registry search surfaces matching projections (and commands). *(FR14)***
**Given** the palette is open and the registry holds one or more `DomainManifest`s,
**When** the operator types a query,
**Then** `CommandPaletteEffects.HandlePaletteQueryChanged` debounces (150ms, prior keystroke cancelled),
scores every registered **projection** and **command** label against the query via `PaletteScorer`
(prefix > substring > fuzzy, score > 0 retained), applies the +15 current-bounded-context bonus, filters
unreachable (`HasFullPageRoute`, D21) and unauthorized (`ICommandAuthorizationEvaluator`) commands, ranks the
top 50, and dispatches `PaletteResultsComputedAction` so the listbox surfaces the **matching projections**
(and commands/recent/shortcuts) live as the query changes — with the stale-result guard refusing late or
closed-palette results (D20).

> **AC2 disposition note — `FcProjectionGlobalSearch` (decide honestly, do NOT silently pass).** The epic's
> AC2 prose reads "…results filter live from the registry **and `FcProjectionGlobalSearch` surfaces matching
> projections**." At `ad6c78e` these are **two distinct surfaces**:
> - **Registry-wide projection/command search = the palette effect path** (`HandlePaletteQueryChanged` →
>   `PaletteResult(Category=Projection|Command)`). This is the genuine "global search across projections" and
>   is implemented + pinned. **This is what AC2 substantively asserts.**
> - **`FcProjectionGlobalSearch` = a per-grid, in-projection row search input** (Story 4-3 D6), rendered only
>   inside a generated DataGrid view when an `IProjectionSearchProvider<T>` is registered. It does **not**
>   "surface matching projections" across the registry; it filters rows **within one** projection grid.
>
> The dev agent must **verify this against `src/`** and pick ONE, recording the rationale in the Dev Agent
> Record (mirrors Story 2.6's AC1(b) honest-call pattern):
> 1. **Treat the palette's `Category=Projection` results as the AC2 "surfaces matching projections"
>    mechanism** (most likely correct — the palette *is* the cross-projection global search) and pin it
>    end-to-end; note that `FcProjectionGlobalSearch` is a separate Story-4-3 in-grid concern, confirmed +
>    pinned in isolation, not the AC2 registry surface.
> 2. **If a genuine, in-scope gap exists** where AC2 truly requires `FcProjectionGlobalSearch` to surface
>    registry-level projection matches → make the **minimal** honest `src/` change and pin it.
> 3. **If the wording is an epic/implementation mismatch** → record it explicitly and flag for PO/review so
>    AC2 is consciously accepted as satisfied-by-the-palette (do not claim the named component does something
>    it does not). **Prefer option 1** unless `src/` proves otherwise.

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before writing
> anything. Most subtasks resolve to "already true → add/strengthen the pin". Open a `src/` change only if a
> genuine AC gap is proven. Record what you found (true/false + evidence) in the Dev Agent Record so the
> review can audit it. **Expected `src/` delta for both ACs is ZERO** unless the AC2 option-2 path proves a
> real gap.

- [x] **Task 1 — Verify AC1: `Ctrl+K` → palette opens as an ARIA combobox (AC: #1)**
  - [x] **Shortcut → open path — confirm ALREADY WIRED + PINNED, no change.** Re-confirm
    `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync` registers `"ctrl+k"` **and** `"meta+k"` →
    `OpenPaletteAsync` (idempotency flag D12/D24), that `FrontComposerShell.HandleGlobalKeyDown` →
    `IShortcutService.TryInvokeAsync` is the router, and that `OpenPaletteAsync` dispatches
    `PaletteOpenedAction` + `IDialogService.ShowDialogAsync<FcCommandPalette>`. Re-confirm the existing pins:
    `FrontComposerShellTests.CtrlKOpensPaletteDialogViaShortcutService` (shell routes Ctrl+K → dialog open) +
    `CommandPaletteE2ETests.AC4_MetaKChord_DispatchesSamePaletteHandler_AsCtrlK` (meta+k parity — **note this
    one is in the excluded `e2e-palette` lane**).
  - [x] **ASSESS the ARIA-combobox render gap (the real AC1 deliverable).** The combobox attributes EXIST in
    `FcCommandPalette.razor` / `FcPaletteResultList.razor` but the **role pins are incomplete**: existing
    tests assert `aria-controls` / `aria-expanded` (false + true) / `aria-autocomplete` / `aria-disabled`, but
    **no default-lane test asserts `role="combobox"` on the input, `role="listbox"` on the results container,
    `role="option"` on rows, or that `aria-activedescendant` tracks `SelectedIndex`** (verified gap — see Dev
    Notes). **Add a durable bUnit pin** (in `FcCommandPaletteTests` / `FcPaletteResultListTests`) asserting
    the full combobox/listbox/option role set + `aria-activedescendant` ↔ selected `<li role="option" id>`.
    **Pin-only, no `src/` change** (attributes already render).
  - [x] **ASSESS the keyboard-navigation default-lane gap.** `HandleKeyDownAsync` implements ArrowDown/Up
    (`PaletteSelectionMovedAction(±1)`) / Enter / Escape, and `CommandPaletteReducerTests` pins the
    clamp/selection reducer logic — **but the end-to-end "press ArrowDown in the rendered palette → the
    selected `<li role="option" aria-selected>` + the input's `aria-activedescendant` advance"** path is only
    exercised in the **excluded** `e2e-palette` lane. Add a **default-lane** bUnit pin that fires
    `ArrowDown`/`ArrowUp`/`Escape` on the rendered `FcCommandPalette` and asserts the visible selection +
    `aria-activedescendant` change + Escape dispatches `PaletteClosedAction`. **Pin-only, no `src/` change.**
    Use `CultureScope` for any localized assertion; `JSInterop.Mode = Loose` for the keyboard module calls.

- [x] **Task 2 — Verify AC2: live registry search surfaces matching projections (AC: #2)**
  - [x] **Scoring + filter pipeline — confirm ALREADY PINNED, no change.** Re-confirm `PaletteScorer.Score`
    three-band ranking (`PaletteScorerTests` + `PaletteScorerPropertyTests` determinism/monotonicity) and the
    effect pipeline (`CommandPaletteEffectsTests`): debounce-cancels-earlier-keystroke,
    blank-query-restores-defaults, projection routes use the kebab navigation convention, negative scores
    filtered, whitespace trimmed, **unreachable commands filtered via `HasFullPageRoute` (D21)**, **denied
    protected commands filtered (`ICommandAuthorizationEvaluator`)**, contextual +15 bonus applied. Re-confirm
    the `D20` stale/closed-palette guard in `CommandPaletteReducerTests`
    (`OnPaletteResultsComputed_WhenPaletteClosed_NoOps`, `RejectsStaleQueryResults`).
  - [x] **RESOLVE the `FcProjectionGlobalSearch` AC2 wording (the honest call — see AC2 disposition note).**
    Grep `src/` to confirm: (a) the palette's `Category=Projection` results are the registry-wide
    "global search across projections" mechanism; (b) `FcProjectionGlobalSearch` is rendered **only** by a
    generated view gated on `IProjectionSearchProvider<T>` and dispatches `GlobalSearchChangedAction` for
    **in-grid** row search, not registry projection discovery. Pick option 1/2/3 from the disposition note and
    **record the evidence + decision** in the Dev Agent Record. **Prefer option 1 (palette = AC2 surface);
    expect ZERO `src/` change.**
  - [x] **Add the AC2 integration pin (the durable deliverable).** Add a **default-lane** test (NOT
    `e2e-palette`) that drives the live filtering end-to-end through the **real** scorer + effect: register a
    multi-manifest registry, open the palette, dispatch a query, await the debounce
    (`FakeTimeProvider`/`TimeProvider`), and assert the listbox surfaces exactly the matching projections
    (+ commands) ranked, and that a non-matching query yields the empty-state. This closes the
    "registry-live-filtering is only proven in the excluded e2e lane" durability gap. Reuse the
    `CommandPaletteEffectsTests` registry-fake harness; if a rendered-listbox assertion is wanted, mount
    `FcPaletteResultList` with the computed results (`GeneratedComponentTestBase`/`AddFrontComposerTestHost`).
  - [x] **Confirm `FcProjectionGlobalSearch` isolation pin (no AC2 dependency).** Re-confirm the existing
    `FcDataGridInputPersistenceTests.ProjectionGlobalSearch_KeepsPendingTypedValue_OnUnchangedParameterRerender`
    + `FilterReducerTests` coverage; if option 1 is chosen, this component needs no new AC2 pin (it is a
    Story-4-3 in-grid concern). Do **not** expand its scope in this story.

- [x] **Task 3 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE
    (use `-m:1 /nr:false` if node-reuse flakes, per Stories 2.4/2.5/2.6).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` —
    everything this story touches is green; new pins pass. **Host constraint (inherited from Stories
    2.3–2.6):** solution-level VSTest opens a local socket and fails with `SocketException (13): Permission
    denied` in this sandbox — if so, fall back to the **xUnit v3 in-process runner** per test assembly for
    local evidence, and record that the solution-level VSTest run is the CI gate. New pins land in
    **`Shell.Tests`** (component/effect/integration).
  - [x] **Run the `e2e-palette` lane too** (`--filter "Category=e2e-palette"`) and confirm the 4
    `CommandPaletteE2ETests` stay green — they are the existing full-flow coverage and must not regress, even
    though they are excluded from the default blocking lane. Do **not** move the new default-lane pins into
    `e2e-palette` (the whole point is to give AC1/AC2 *default-lane* coverage).
  - [x] **Re-prove the standing failure baseline.** Stories 2.5/2.6 recorded `Shell.Tests` at **8 failed**
    (documented pre-existing/environmental: `PendingStatusReopenGovernanceTests` ×4 deferred-work file-IO,
    `NavigationEffectsLastActiveRouteTests` ×1 hydration, `CounterStoryVerificationTests` ×2 Verify drift,
    `CommandRendererFullPageTests` ×1 query-fallback) and `SourceTools.Tests` at **3 failed**. Capture
    before→after counts for the assemblies you touch and confirm the **same** pre-existing failures remain
    (none new, none in the palette/search surface, none misattributed). Pure-pin work touches only
    `Shell.Tests`; `SourceTools.Tests` is untouched (no generator change).
  - [x] **`.verified.txt` discipline.** Confirm-and-pin → default **ZERO** snapshot edits. No palette pin
    should touch `CounterStoryVerificationTests` / `CounterProjectionApprovalTests` baselines; confirm them
    byte-for-byte unchanged.

- [x] **Task 4 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in the
    Dev Agent Record, reconciled against the actual git tree (the recurring Epic-1/2 review tax — pay it up
    front; include any QA test-summary artifact).
  - [x] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no stray
    `</content>` / `</invoke>` / `<invoke` / tool-call tags.
  - [x] **Record the AC2 disposition decision explicitly** (option 1/2/3 from Task 2) with the proven `src/`
    evidence, so the review can audit whether AC2 is satisfied-by-the-palette, minimally wired, or an
    explicitly-accepted epic/implementation wording mismatch.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`ad6c78e`) | This story |
|---|---|---|
| `Ctrl+K` / `Cmd+K` → `OpenPaletteAsync` → `ShowDialogAsync<FcCommandPalette>` | **Exists, mounted, pinned** — `FrontComposerShortcutRegistrar`, `FrontComposerShellTests.CtrlKOpens…`, E2E AC4 | Confirm; no change |
| ARIA combobox **attributes** (`role=combobox/listbox/option`, `aria-activedescendant`, `aria-expanded`) | **Render in `src/`**; **partially pinned** (controls/expanded/autocomplete yes; role=combobox/listbox/option + activedescendant **not** asserted) | **CLOSE THE PIN GAP** — bUnit role + activedescendant pin (no `src/` change) |
| Keyboard nav (Arrow/Enter/Escape) end-to-end in rendered palette | Handlers exist + reducer pinned; **end-to-end only in the EXCLUDED `e2e-palette` lane** | **ADD a default-lane** keyboard-nav bUnit pin (no `src/` change) |
| Live registry scoring/filter/rank (projections + commands) | **Exists & pinned** — `PaletteScorer*Tests`, `CommandPaletteEffectsTests` (21) | Confirm; add a **default-lane** AC2 integration pin |
| `D20` stale/closed-palette result guard | **Exists & pinned** — `CommandPaletteReducerTests` | Confirm; no change |
| Recent-routes ring buffer + hydration + fail-closed scope | **Exists & pinned** — reducer/effect/scope tests | Confirm; no change |
| **`FcProjectionGlobalSearch`** (per-grid search input, Story 4-3) | **Exists, generated-view-gated on `IProjectionSearchProvider<T>`**; isolation-pinned (`FcDataGridInputPersistenceTests`, `FilterReducerTests`) | **RESOLVE AC2 wording** (option 1/2/3) — likely a separate in-grid concern, not the AC2 registry surface |
| `CommandPaletteE2ETests` full flow | **Exists & green** — but `[Trait("Category","e2e-palette")]`, **excluded from default blocking lane** | Keep green; **mirror its AC1/AC2 essence into the default lane** |
| `PaletteScorerBench` (<200µs/candidate) | **Exists** — `Category=Performance` (advisory lane) | Confirm; no change |

> **Key judgment for the dev agent:** the palette is **feature-complete and mounted** — your job is *confirm +
> close the pin durability gaps + resolve the AC2 `FcProjectionGlobalSearch` wording honestly*, **ZERO `src/`
> change expected**. The two genuine deliverables are (1) **default-lane pins** for the ARIA combobox roles +
> `aria-activedescendant` + keyboard nav (today only proven in the *excluded* e2e lane or only at attribute
> subset / reducer level), and (2) an **honest AC2 disposition** for `FcProjectionGlobalSearch`. Do **not**
> assert AC1/AC2 pass on the strength of the excluded `e2e-palette` lane or attribute-subset tests alone —
> that is exactly the "source-only / presence-only / excluded-lane assertion = false confidence" lesson from
> Stories 2.3/2.5/2.6.

### Exact anchors (read these before touching anything)

> ⚠️ **Line numbers are guidance, not contracts.** They reflect `ad6c78e`; confirm by symbol/marker before
> relying on any single line. Cite the symbol, not the line, in new pins.

**AC1 — shortcut → open + ARIA combobox**
- **Shortcut framework** — `src/Hexalith.FrontComposer.Contracts/Shortcuts/{ShortcutBinding,ShortcutRegistration,IShortcutService}.cs`
  (Story 3-4 D1/D4; `ShortcutBinding.Normalize` canonicalizes `ctrl+shift+alt+meta+<key>` lowercase;
  `TryFromKeyboardEvent`). **Service + registrar** — `src/Hexalith.FrontComposer.Shell/Shortcuts/{ShortcutService,FrontComposerShortcutRegistrar}.cs`
  (`Register("ctrl+k", …, OpenPaletteAsync)` + `Register("meta+k", …)` ~L77–89; `OpenPaletteAsync` ~L149–202:
  D12 idempotency `_palettePending`, `PaletteOpenedAction`, `ShowDialogAsync<FcCommandPalette>` Modal/600px).
- **Shell keydown router** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
  (root `@onkeydown="HandleGlobalKeyDown"` ~L27; `<FcPaletteTriggerButton />` default `HeaderEnd` ~L77) +
  `FrontComposerShell.razor.cs` (`HandleGlobalKeyDown` → `Shortcuts.TryInvokeAsync(e)` ~L270;
  `RegisterKeyboardInteropAsync` loads `fc-keyboard.js` ~L468).
- **Palette dialog** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor`
  (root `role="dialog"` L20; **input `role="combobox" aria-haspopup="listbox" aria-expanded aria-controls
  aria-activedescendant aria-autocomplete="list"` L25–26**; live region `role="status" aria-live="polite"
  aria-atomic="true"` L35) + `FcCommandPalette.razor.cs` (`HandleKeyDownAsync` Arrow/Enter/Escape;
  `ActiveDescendantId` → `$"fc-palette-result-{SelectedIndex}"`; `OnAfterRenderAsync` two-tick live-region
  D15; `IAsyncDisposable` dismiss-path coherence D11).
- **Result listbox** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor`
  (`<ul role="listbox" aria-activedescendant> ` L25 → `<li role="none">` group `<ul role="group"
  aria-labelledby>` → `<li role="option" id aria-selected aria-disabled tabindex="-1">` L34–36;
  `ResultElementId(flatIndex)` → `fc-palette-result-{flatIndex}`; optional `IBadgeCountService` badge D16).
- **Trigger button** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor(.cs)`
  (`FluentButton` → `Registrar.OpenPaletteAsync()`; auto-populated default `HeaderEnd`, D18).
- **JS module** — `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js`
  (`registerShellKeyFilter` suppresses browser default for Ctrl/Cmd+K & Ctrl+,; `registerPaletteKeyFilter`
  suppresses Arrow/Enter/Escape/Tab inside the palette — the SoT for preventDefault, P2).

**AC2 — live registry search**
- **Effect (scoring loop)** — `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
  (`HandlePaletteQueryChanged`: `DebounceMilliseconds=150`, per-dispatch `_queryCts`; iterates
  `Registry.GetManifests()` → `PaletteScorer.Score` > 0; `ContextualBonus=15`; `HasFullPageRoute` D21;
  `CanSurfaceCommandAsync` via `ICommandAuthorizationEvaluator`; `OrderByDescending(Score).Take(TopResultCap=50)`;
  alias table `? help keys kb shortcut → "shortcuts"`; `KeyboardShortcutsSentinel="@shortcuts"` D23). Also
  `HandlePaletteResultActivated` (navigate / `@shortcuts` refill, open-redirect re-validation D10) +
  `HandleRecentRouteVisited` (`_persistGate` semaphore, `{tenant}:{user}:palette-recent`, HFC2105 fail-closed).
- **Scorer (pure)** — `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs`
  (`Score(query, candidate)`: prefix `100+2·len` > substring `50+len` > fuzzy `max(0,10+matched-gaps)`,
  rune-aware gap counting, weak-fuzzy collapses to 0; ADR-043). Contextual bonus lives in the **effect**, not
  the scorer (purity).
- **State/actions/reducers** — `src/Hexalith.FrontComposer.Shell/State/CommandPalette/{FrontComposerCommandPaletteState,CommandPaletteActions,CommandPaletteReducers,FrontComposerCommandPaletteFeature,PaletteResult,CommandPaletteHydrationState}.cs`
  (`PaletteResultCategory{Projection=0,Command=1,Recent=2,Shortcut=3}` ordinal-stable; `PaletteLoadState`;
  `ReducePaletteResultsComputed` D20 stale/closed guard + DN4 selection preservation; `ReducePaletteScopeChanged`
  fail-closed clear DN2).
- **Grid global-search (separate concern)** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor(.cs)`
  (Story 4-3 D6: `role="combobox" data-testid="fc-global-search"`, 300ms debounce, dispatches
  `GlobalSearchChangedAction(ViewKey, payload)`; rendered by generated view only when `IProjectionSearchProvider<T>`
  registered). **Not the registry-wide palette search — see AC2 disposition note.**
- **DI** — `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
  (`TryAddScoped<IShortcutService, ShortcutService>()` + `<FrontComposerShortcutRegistrar>()` ~L304–308;
  `TryAddScoped<CommandPaletteEffects>()` ~L313; Fluxor `ScanAssemblies` picks up the slice).

### Test anchors (where pins live / go)

- **Palette component (confirm + strengthen)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`
  (7: dialog/search render, live-region role/polite/atomic, `aria-controls`/`aria-expanded` false+true,
  `aria-autocomplete`, same/different-page focus) — **ADD `role="combobox"` + `aria-activedescendant`-tracks-
  selection pins + ArrowDown/Up/Escape keyboard-nav pins here.**
- **Result list (confirm + strengthen)** — `tests/…/Components/Layout/FcPaletteResultListTests.cs`
  (9: category headings, badge present/absent, `aria-disabled` informational, routable-shortcut no
  `aria-disabled`, empty-state) — **ADD `role="listbox"` + `role="option"` + `aria-selected`-tracks-
  `SelectedIndex` pins here.**
- **Effects (confirm)** — `tests/…/State/CommandPalette/CommandPaletteEffectsTests.cs` (21) +
  `CommandPaletteEffectsScopeTests.cs` (2: fail-closed persist).
- **Reducers (confirm)** — `tests/…/State/CommandPalette/CommandPaletteReducerTests.cs` (16: open/close/clamp/
  stale-guard/ring-buffer/scope-clear).
- **Scorer (confirm)** — `tests/…/State/CommandPalette/PaletteScorerTests.cs` (5) +
  `PaletteScorerPropertyTests.cs` (FsCheck: deterministic, prefix-monotonic).
- **Shortcut → open (confirm)** — `tests/…/Components/Layout/FrontComposerShellTests.cs`
  (`CtrlKOpensPaletteDialogViaShortcutService`, `CtrlCommaInvokesRegisteredShortcut`).
- **e2e-palette lane (confirm green, do NOT extend default coverage by relying on it)** —
  `tests/…/EndToEnd/CommandPaletteE2ETests.cs` (4, `[Trait("Category","e2e-palette")]`, EXCLUDED from default
  lane): AC1 open→type→activate→navigate→persist, AC2 empty-registry, AC3 shortcuts-query-5-bindings,
  AC4 meta+k parity. **Governance:** `tests/…/Governance/CiGovernanceTests.cs` asserts the default-lane filter
  string excludes `e2e-palette` — do not change that filter.
- **Grid global-search isolation (confirm)** — `tests/…/Components/DataGrid/FcDataGridInputPersistenceTests.cs`
  (`ProjectionGlobalSearch_KeepsPendingTypedValue_OnUnchangedParameterRerender`) +
  `tests/…/State/DataGridNavigation/FilterReducerTests.cs`.
- **AC1/AC2 default-lane pins (NEW — the deliverable)** — add in `FcCommandPaletteTests` /
  `FcPaletteResultListTests` (ARIA roles + activedescendant + keyboard nav) and a new
  `tests/…/State/CommandPalette/` or `…/Components/Layout/` integration test for live registry filtering
  (default lane, NOT `e2e-palette`). Reuse `GeneratedComponentTestBase`/`AddFrontComposerTestHost`,
  `JSInterop.Mode = Loose`, `FakeTimeProvider` for the 150ms debounce, `CultureScope` for localized copy.
- **Approval baseline (confirm untouched)** — `CounterProjectionApprovalTests` /
  `CounterStoryVerificationTests.*.verified.txt` byte-for-byte (no generator change in this story).

### Project-context guardrails that apply here (non-negotiable)

- **Fluxor single-writer (ADR-007) / scoped-lifetime (ADR-030):** each action type has one dispatch source;
  **effects own persistence + JS interop**, reducers stay pure (`CommandPaletteReducers` compute **no** scores
  — the effect pre-resolves `PaletteResultsComputedAction.Results`, ADR-039). `IShortcutService`,
  `FrontComposerShortcutRegistrar`, `CommandPaletteEffects` are **Scoped** — never capture in singletons.
  **NFR17 tripwire:** do not add a new `IStorageService.SetAsync` call site in `Shell/State/` (would require
  updating the tripwire whitelist + compliance matrix) — none is needed for pin-only work.
- **Generator rules:** this story should **not** touch the generator. If AC2 option-2 forces a generated-view
  change, **never hand-edit generated code** (`obj/**/generated/HexalithFrontComposer/`) — change
  `RazorEmitter.cs`; **no `ISymbol` escapes the parse stage**; `SourceTools` references **only** `Contracts`
  (netstandard2.0-clean). Prefer not going there.
- **Real-time / UI stack (pinned):** FluentUI v5 RC `5.0.0-rc.3-26138.1` (`IDialogService`, `FluentTextInput`,
  `FluentButton`, `FluentBadge` — note v5 RC2 has no `Accent` badge member; `BadgeAppearance.Tint` is the
  replacement, DN5); `Fluxor.Blazor.Web` 6.9.0; centralized versions in `Directory.Packages.props` — **never
  add `Version=` to a `.csproj`**; don't bump FluentUI/Fluxor in this story.
- **ULIDs not GUIDs:** `CorrelationId` for palette actions is generated via `IUlidFactory.NewUlid()` — never
  `Guid.NewGuid()`. (Already the case in `OpenPaletteAsync` / `FcCommandPalette`.)
- **Schema integrity:** do **not** touch `CanonicalSchemaMaterial` — nothing in this story should.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix,
  **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE); `ArgumentNullException.ThrowIfNull`
  at public boundaries; **no copyright/license headers** (this repo has none); **CRLF**, 4-space indent,
  final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for
  components (`JSInterop.Mode = Loose` — the palette calls `fc-keyboard.js`/`fc-focus.js`); **`FakeTimeProvider`**
  for the 150ms debounce (never wall-clock sleeps); **Verify.XunitV3** (NOT `Verify.Xunit`) for any snapshot,
  `.verified.txt` updated **intentionally**; plural `{Class}Tests.cs`; three-part
  `Subject_Scenario_Expectation`; **solution-level** `dotnet test` + trait filters (not per-project); run with
  **`DiffEngine_Disabled=true`** (else Verify hangs); `CultureScope` for culture-sensitive assertions.
  **New default-lane pins must NOT carry `[Trait("Category","e2e-palette")]`** — that defeats the purpose.
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress;
  built-in analyzers only (no Sonar/StyleCop/Roslynator).
- **Commits/branches:** This story is pure verification + pins (no `src/` change expected) → the work is
  **`test:`**-shaped (no release). If AC2 option-2 forces a genuine `src/` behaviour change, it is a
  **`feat:`** — use `feat:` then, not `test:`. **No direct commits to `main`** — cut a
  `test/story-2-7-*` (or `feat/story-2-7-*`) feature branch + PR. *(Stories 2.1–2.4 were committed straight to
  `main` by the story-automator, contradicting "no direct commits to main"; 2.5/2.6 used feature branches —
  prefer the branch, and if the automator forces `main`, record the deviation in the Change Log.)*

### Project Structure Notes

- **Alignment:** all touched surfaces sit in the established shell layout
  (`Shell/Shortcuts`, `Shell/State/CommandPalette`, `Shell/Components/Layout`, `Shell/Components/DataGrid`,
  `Shell/wwwroot/js`). New pins go in the matching `*.Tests` mirror
  (`Shell.Tests/Components/Layout`, `Shell.Tests/State/CommandPalette`). No new top-level folders or projects.
- **Dependency direction:** palette/shortcut/search surfaces live in `Shell` (→ Contracts). The shortcut
  contracts (`IShortcutService`, `ShortcutBinding`, `ShortcutRegistration`) live in `Contracts`. Do not pull
  net10/FluentUI deps into `SourceTools` or the netstandard2.0 face of `Contracts`.
- **No variances expected** (confirm-and-pin, zero `src/`). If AC2 option-2 carries a minimal deliberate
  `src/` delta, call it out in the Dev Agent Record with the proven gap and the option chosen.

### Epic dependencies & their state

| Story 2.7 needs | From | State at kickoff |
|---|---|---|
| Registry-driven nav + `DomainManifest`s (the search corpus) | Story 2.2 | ✅ done & pinned — palette iterates `IFrontComposerRegistry.GetManifests()` |
| Generated projection views + routes (palette navigates to them) | Story 2.1 | ✅ done & pinned — `CommandRouteBuilder`, kebab routes |
| DataGrid filtering + `GlobalSearchChangedAction` (grid in-search) | Story 2.3 / 4-3 | ✅ present — `FcProjectionGlobalSearch` (separate from AC2 registry search) |
| Shortcut framework + `FcCommandPalette` + scorer + hydration | Story 3-4 / 3-5 / 3-6 / 3-7 | ✅ present & pinned — authored ahead of this confirm-and-pin numbering (expected) |
| `IBadgeCountService` (optional palette projection badges) | Story 3-5 | ✅ present — optional; `FcPaletteResultList` degrades gracefully when unregistered |

> **Scope boundary:** this story is "**command palette discovery and global search**" (AC1 `Ctrl+K` → ARIA
> combobox + keyboard nav; AC2 live registry filtering surfaces matching projections). It is **not** the
> command-authoring/lifecycle palette features (Epic 3 — `@shortcuts` help, recent-route persistence internals,
> badge-count producer), nor the FC-TBL table-API confirmation (2.8). It does **not** reopen the shortcut
> framework, the scorer algorithm, the hydration lifecycle, or the EventStore stack. Stay inside AC1–AC2 and
> the durable-pin + AC2-disposition deliverables.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The command palette (FR14, UX-DR4) is shipped, mounted, and
heavily tested at `ad6c78e` (≈70 palette/search tests). **Done =** AC1 is proven true end-to-end against
`src/` (Ctrl+K → mounted ARIA combobox + keyboard nav) and the **ARIA-role + `aria-activedescendant` +
keyboard-nav durability gaps are closed with default-lane pins** (not relying on the excluded `e2e-palette`
lane or attribute-subset tests); AC2's live registry filtering is re-confirmed green and carries a
**default-lane integration pin**, and the **`FcProjectionGlobalSearch` AC2 wording is resolved honestly**
(palette = the registry search surface, the grid input is a separate Story-4-3 concern — or an explicit
minimal wiring / PO-accepted mismatch). The Release build is 0/0 under TWAE, the default test lane is green,
the `e2e-palette` lane stays green, the standing failure baseline (`Shell.Tests` 8 / `SourceTools.Tests` 3)
is re-proved pre-existing, the File List + counts are accurate, and the approval/`.verified.txt` baselines are
byte-for-byte unchanged (no generator change). **Default `src/` change = ZERO.**

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.7] — story statement, ACs (FR14, UX-DR4)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope; "the command palette" as a read-only discovery surface; FR14 (nav/home/palette/badges), UX-DR4 (reusable `FcCommandPalette` ARIA combobox)
- [Source: _bmad-output/planning-artifacts/epics.md#FR9] — global shortcut `Ctrl+K` (palette) active in the shell `FluentLayout`
- [Source: _bmad-output/project-context.md] — FluentUI v5 RC `5.0.0-rc.3-26138.1` / Fluxor 6.9.0 pins; Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), ADR-039 reducer purity, NFR17 tripwire, ULIDs-not-GUIDs, TWAE, test discipline (`DiffEngine_Disabled=true`, `FakeTimeProvider`, `CultureScope`, solution-level + trait filters), dependency-direction-to-Contracts, no copyright headers
- [Source: _bmad-output/implementation-artifacts/2-6-live-projection-updates-with-reconnect-reconciliation.md] — prior confirm-and-pin pattern; "source-only/presence-only/excluded-lane assertion = false confidence" lesson; honest AC-disposition (option 1/2/3) precedent; VSTest socket sandbox constraint + xUnit v3 in-process fallback; 8-failure Shell baseline / 3-failure SourceTools baseline; retro AI-1/AI-2 taxes; feature-branch (not `main`) deviation note
- [Source: _bmad-output/implementation-artifacts/2-5-column-prioritization-for-wide-projections.md] — confirm-and-pin "close the genuine durability gap, do not over-claim" precedent; render-specimen + `CultureScope` pin hardening
- [Source: _bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md] — `GlobalSearchChangedAction` / grid-filter surface (context for the `FcProjectionGlobalSearch` AC2 disposition)
- [Source: src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs] — `Register("ctrl+k"/"meta+k", OpenPaletteAsync)`, idempotency D12/D24, `ShowDialogAsync<FcCommandPalette>` (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs] + Contracts `Shortcuts/{ShortcutBinding,ShortcutRegistration,IShortcutService}.cs` — binding normalization + `TryInvokeAsync` router (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor(.cs)] — `@onkeydown="HandleGlobalKeyDown"`, `<FcPaletteTriggerButton/>` default `HeaderEnd` (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor(.cs)] — `role="combobox"`/`aria-*` input, `HandleKeyDownAsync` Arrow/Enter/Escape, `ActiveDescendantId`, live-region D15, dismiss D11 (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor] — `<ul role="listbox">`/`<li role="option" aria-selected aria-disabled>`, `ResultElementId`, badge D16 (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor.cs] — `Registrar.OpenPaletteAsync()` header trigger D18 (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js] — Ctrl/Cmd+K browser-default suppression + palette key filter (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs] — `HandlePaletteQueryChanged` debounce/scoring/filter/rank, `HasFullPageRoute` D21, auth filter, alias `@shortcuts` D23 (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs] — three-band rune-aware fuzzy scorer, ADR-043 (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/CommandPalette/{CommandPaletteReducers,CommandPaletteActions,FrontComposerCommandPaletteState,PaletteResult}.cs] — D20 stale/closed guard, `PaletteResultCategory`, ordinal-stable enums (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor.cs] — per-grid 300ms `GlobalSearchChangedAction` input, Story 4-3 D6 — **separate from AC2 registry search** (AC2 disposition)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs] — Scoped `IShortcutService`/registrar/`CommandPaletteEffects` registration (AC1/AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs] + `FcPaletteResultListTests.cs` — component pins (confirm + **add role/activedescendant/keyboard-nav pins**; AC1)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/{CommandPaletteEffectsTests,CommandPaletteReducerTests,PaletteScorerTests,PaletteScorerPropertyTests,CommandPaletteEffectsScopeTests}.cs] — scoring/effect/reducer pins (confirm; AC2 + **add default-lane registry-filter integration pin**)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs] — full-flow pins, `[Trait("Category","e2e-palette")]` EXCLUDED from default lane (confirm green; mirror essence into default lane)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs] — default-lane filter excludes `e2e-palette` (do not change)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs] — `CtrlKOpensPaletteDialogViaShortcutService` (confirm; AC1)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Resolved `bmad-dev-story` customization; no activation prepend/append steps. Loaded `_bmad-output/project-context.md`, story file, and sprint status. Story baseline commit preserved as `ad6c78e7a23a43a08af2864f6fd452ad8a856360`.
- 2026-06-04: Verified AC1 source path: `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync` registers `ctrl+k` and `meta+k`; shell root keydown routes to `IShortcutService.TryInvokeAsync`; `OpenPaletteAsync` dispatches `PaletteOpenedAction` and opens `FcCommandPalette` through `IDialogService`. Existing `FrontComposerShellTests.CtrlKOpensPaletteDialogViaShortcutService` and `CommandPaletteE2ETests.AC4_MetaKChord_DispatchesSamePaletteHandler_AsCtrlK` remain green.
- 2026-06-04: Verified AC1 render path: `FcCommandPalette.razor` renders `role="combobox"`, `aria-haspopup="listbox"`, `aria-expanded`, `aria-controls`, `aria-activedescendant`, and live `role="status"`; `FcPaletteResultList.razor` renders `role="listbox"` and `role="option"` rows. Default-lane pins are present in `FcCommandPaletteTests` and `FcPaletteResultListTests`.
- 2026-06-04: Verified AC2 source path: `CommandPaletteEffects.HandlePaletteQueryChanged` debounces on `TimeProvider`, iterates `IFrontComposerRegistry.GetManifests()`, scores projections and commands with `PaletteScorer`, applies contextual bonus, filters unreachable and unauthorized commands, ranks top 50, and dispatches `PaletteResultsComputedAction`. Reducer stale/closed-palette guards remain pinned.
- 2026-06-04: AC2 disposition decision = option 1. The palette's `PaletteResultCategory.Projection` results are the registry-wide projection discovery/search surface. `FcProjectionGlobalSearch` is a separate Story 4-3 in-grid row-search component because it dispatches `GlobalSearchChangedAction(ViewKey, query)` into DataGrid navigation state, not registry projection discovery. Current grep found no direct SourceTools emitter reference for `FcProjectionGlobalSearch`; this does not create an AC2 registry-search gap because AC2 is satisfied by the palette path and the grid component remains isolated by existing component/reducer pins.
- 2026-06-04: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-04: Validation: solution-level `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` failed before test execution with `System.Net.Sockets.SocketException (13): Permission denied` from MSBuild named-pipe setup. Used xUnit v3 in-process runner fallback.
- 2026-06-04: Validation: story-focused Shell default-lane classes passed 60/60. Full Shell default lane via in-process runner: 1760 total, 9 failed. Failures are outside the palette/search surface: PendingStatusReopenGovernanceTests x4, NavigationEffectsLastActiveRouteTests x1, CounterStoryVerificationTests x2, EventStorePactContractTests x1 mock-server socket, CommandRendererFullPageTests x1.
- 2026-06-04: Validation: `e2e-palette` in-process lane passed 4/4. `SourceTools.Tests` default lane reproduced 958 total, 3 failed, matching the known untouched SourceTools baseline. `git diff --name-only -- '*.verified.txt'` returned no changes.
- 2026-06-04: Sentinel scan: modified test files are clean. The only match in the story file is the pre-existing Task 4 checklist text naming the forbidden examples; no new Dev Agent Record, File List, Change Log, or test-summary content contains authoring sentinels.

### Completion Notes List

- Closed AC1 durability gaps with default-lane bUnit pins already present relative to story baseline: `SearchInput_RendersAsAriaCombobox`, `ArrowKeys_MoveSelection_AndTrackAriaActiveDescendant`, `Escape_DispatchesPaletteClosed_ClosingThePalette`, `ResultsContainer_RendersRoleListbox_WithRoleOptionRows`, and `AriaSelected_AndAriaActiveDescendant_TrackSelectedIndex`.
- Closed AC2 durability gap with default-lane effect pins already present relative to story baseline: `HandlePaletteQueryChanged_MultiManifest_SurfacesMatchingProjectionsRanked` and `HandlePaletteQueryChanged_NonMatchingQuery_SurfacesEmptyState`.
- No production `src/` changes were made. AC2 is consciously satisfied by the palette registry-search surface; `FcProjectionGlobalSearch` remains a separate in-grid search concern and was not expanded.
- Build is clean. Local solution-level VSTest remains sandbox-blocked by socket permissions, so local evidence uses the xUnit v3 in-process runner and CI remains the official solution-level VSTest gate.
- Approval snapshot discipline preserved: no `.verified.txt` files changed.

### File List

- `_bmad-output/implementation-artifacts/2-7-command-palette-discovery-and-global-search.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/2-7-test-summary.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Outcome:** Approved (auto-fix applied) · **Status:** review → done

**Scope reviewed:** the 3 changed source-relevant files vs baseline `ad6c78e`
(`FcCommandPaletteTests.cs`, `FcPaletteResultListTests.cs`, `CommandPaletteEffectsTests.cs`).
`_bmad/`, `_bmad-output/`, and `.codex/` were excluded per the review charter. ZERO `src/` production
change confirmed (`git diff ad6c78e..HEAD -- src/` empty) — confirm-and-pin held.

**Claims validated against reality (all true):**
- All 7 dev-added pins exist, contain real assertions (concrete expected values, not placeholders), and
  **pass 7/7** via the in-process runner. The AC2 effect pins drive the **real** `PaletteScorer` + effect
  across a multi-manifest registry and assert exact ranked `RouteUrl`s + empty-state.
- AC1 source attributes all render as asserted: input `role="combobox"`/`aria-haspopup="listbox"`,
  `<ul role="listbox">`, `<li role="option" id="fc-palette-result-{i}" aria-selected>`,
  `aria-activedescendant` tracking `SelectedIndex`. The rendered Arrow/Escape pins exercise the genuine
  keydown→reducer→render path (not reducer-only).
- Build `0 warnings / 0 errors` under TWAE. Full palette/search surface green; `e2e-palette` lane 4/4 green.
- **Standing baseline re-proved:** full Shell default lane = **1758 total / 8 failed**, and the 8 are exactly
  the documented pre-existing/environmental set (`PendingStatusReopenGovernanceTests` ×4,
  `NavigationEffectsLastActiveRouteTests` ×1, `CounterStoryVerificationTests` ×2,
  `CommandRendererFullPageTests` ×1) — **none in the palette/search surface, none new.** `.verified.txt`
  byte-for-byte unchanged. No authoring sentinels in the changed test files.
- **AC2 option-1 disposition is honest.** `FcProjectionGlobalSearch` dispatches the in-grid
  `GlobalSearchChangedAction(ViewKey, payload)` into DataGrid navigation state — not registry projection
  discovery — and is **not emitted by the SourceTools generator at baseline** (verified: zero emitter
  reference). The Dev Agent Record transparently recorded this; it is correctly out of AC2 scope (the palette
  `Category=Projection` path is the registry-wide search surface that AC2 substantively asserts).

**Findings & disposition (auto-fixed without prompting, per invocation):**
- 🟡 **MEDIUM — AC1 Enter-activation had no default-lane rendered pin.** AC1 names Enter activation, and the
  story's own charter forbids relying on the excluded `e2e-palette` lane — yet the new pins covered only
  Arrow/Escape. The rendered keydown→`PaletteResultActivatedAction`→`NavigationManager.NavigateTo(RouteUrl)`
  path was proven **only** in the excluded e2e lane. **Fixed:** added
  `Enter_ActivatesSelectedProjection_NavigatingToItsRoute` to `FcCommandPaletteTests` — fires `Enter` on the
  rendered palette and asserts navigation to the projection route through the **real** effect (bUnit
  `NavigationManager`). Passes; palette/search surface now **107 green**. Still ZERO `src/` change.
- 🟢 No HIGH/CRITICAL findings. No false `[x]` claims, no missing ACs, no security/perf issues in the changed
  test code, File List accurate for in-scope files.

**Issues fixed:** 1 (MEDIUM). **Action items created:** 0. **CRITICAL remaining:** 0 → status `done`.

## Change Log

- 2026-06-04: Confirmed and pinned Story 2.7 command palette discovery/global search. Added default-lane AC1/AC2 pins relative to baseline, recorded AC2 option-1 disposition, kept production `src/` unchanged, validated build/test lanes, and moved status to `review`.
- 2026-06-04: Executed `bmad-qa-generate-e2e-tests`; validated the existing Story 2.7 pins against the QA checklist, updated the story-specific and default test summaries with current run counts, and found no additional test gaps to apply.
- 2026-06-04: `story-automator-review` (adversarial) — re-ran build + all test lanes, validated every story claim against `src`/git reality, confirmed AC2 option-1 disposition honest, re-proved the 8-failure Shell baseline (none in palette surface). Auto-fixed one MEDIUM default-lane coverage gap (AC1 Enter activation proven only in the excluded e2e lane) by adding `Enter_ActivatesSelectedProjection_NavigatingToItsRoute`. ZERO `src/` change preserved; `.verified.txt` unchanged. Status `review` → `done`.
