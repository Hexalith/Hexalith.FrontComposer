---
baseline_commit: 81eebf9192c65beb662c39427d3945cda98458b4
---

# Story 2.4: Accessible expand-in-row detail

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Stories 2.1, 2.2 and 2.3, the entire expand-in-row detail surface **already exists, renders, and
> is partially tested** at baseline `81eebf9`. The shipped surface carries `Story 4-5`/`Story 4-6`
> docstring markers because the brownfield code was authored ahead of this epic's confirm-and-pin
> numbering — **that is expected; do not "re-attribute" or churn it.**
>
> - **AC1 always-present region panel** — `FcExpandInRowDetail` (`role="region"` container **always
>   emitted**, `aria-label` from `ExpandInRowDetailPanelAriaLabelTemplate`, `@ChildContent` gated on
>   `HasExpanded`, `OnAfterRenderAsync` D8 scroll-stabilizer guard via `IExpandInRowJSModule`), the
>   generator-emitted expand-trigger `TemplateColumn` (`aria-expanded` / `aria-controls=_expandPanelId`,
>   `ChevronRight`, stop-propagation click → `HandleRowClickAsync`), and the `ExpandedRow` Fluxor slice
>   (`ExpandRowAction`/`CollapseRowAction` → PURE reducers, single-expand REPLACE invariant per UX-DR17,
>   ephemeral D22 view-key, `DisposeAsync` collapse).
> - **AC2 filter-hidden announcement** — `FcExpandedRowHiddenBanner` (`role="status"` + `aria-live="polite"`
>   breadcrumb above the grid, "Clear filter" → `FiltersResetAction`) **and** the embedded
>   visually-hidden live region inside `FcExpandInRowDetail` (`role="status"` + `aria-live="polite"` +
>   `aria-atomic="true"`, fed by `SuppressedAnnouncement`). The generated host view computes
>   `_expandedItemHiddenByFilter = _expandedItemKey is not null && _expandedItem is null` and drives both.
> - **AC3 e2e a11y** — the Playwright `specimen-accessibility.spec.ts` lane runs `expectNoBlockingAxeViolations`
>   against the specimen routes (the `type` specimen renders the grid + `fc-expanded-detail`), plus the
>   in-process bUnit `AxeCoreA11yTests` proxy.
>
> **So this story's job is to (1) VERIFY each of the three ACs holds end-to-end against `src/` at this
> commit, (2) CLOSE the durability gaps with focused regression pins, and (3) make the verification
> durable** so the rest of Epic 2 (column prioritization 2.5, live updates 2.6, palette 2.7, FC-TBL
> confirm 2.8) builds on a pinned expand-in-row baseline. **Default to ZERO `src/` change.** The real
> gap is **end-to-end behavioural coverage on a *generated grid*** — the component, reducer, and emitter
> are pinned in isolation, but the AC1 *expand-click → region populates* flow and the AC2 *filter hides
> an expanded row → banner + suppressed announcement* flow are exercised only indirectly (snapshot /
> component-isolation). If a real `src/` gap is found, fix the component (**never hand-edit generated
> output**) and update affected `.verified.txt` snapshots **intentionally**. If a behaviour is already
> correct, **pin it — do not "improve" or restyle** working output (Epic-1 retro §5 flags
> "copy-a-pattern-without-the-difference" as the rising Epic-2 risk).

## Story

As an operator using assistive technology,
I want row-detail panels that are always announced correctly,
so that expanded content and filter-hidden expansions are perceivable.

## Acceptance Criteria

**AC1 — Row detail renders in an always-present `role="region"` panel. *(FR11, NFR6, UX-DR6)***
**Given** a projection grid row that has detail,
**When** I expand it (click the row-action chevron, dispatching `ExpandRowAction`),
**Then** the detail body renders inside the `FcExpandInRowDetail` container, which **always** carries
`role="region"` and a localized `aria-label` (`ExpandInRowDetailPanelAriaLabelTemplate` → "Details for {0}")
**whether collapsed or expanded** — so the trigger button's `aria-controls` always resolves to a present
element (WCAG 4.1.2),
**And** the expand-trigger column renders `aria-expanded` reflecting the row's state and `aria-controls`
pointing at the panel's `_expandPanelId`, with collapse/expand aria-labels localized
(`ExpandRowButtonAriaLabelTemplate` / `CollapseRowButtonAriaLabelTemplate`),
**And** the single-expand invariant (UX-DR17) holds: expanding a second row REPLACES the first entry at
the reducer (no accumulation), and the panel collapses on `CollapseRowAction` / view `DisposeAsync`.

**AC2 — A filter that hides an expanded row announces the hidden expansion via a live region. *(WCAG 4.1.2)***
**Given** an expanded row whose item the current column-filter predicate then removes from `state.Items`,
**When** the filter applies and the host view computes `_expandedItemHiddenByFilter`
(`_expandedItemKey is not null && _expandedItem is null`),
**Then** `FcExpandedRowHiddenBanner` renders above the grid (`role="status"` + `aria-live="polite"`,
`data-testid="fc-expanded-row-hidden-banner"`) with the localized count copy
(`ExpandedRowHiddenByFilterBanner` → "1 expanded item hidden by current filter") and a "Clear filter"
affordance that dispatches `FiltersResetAction(ViewKey)`,
**And** the visually-hidden live region inside `FcExpandInRowDetail` (`role="status"` + `aria-live="polite"`
+ `aria-atomic="true"`) receives the `SuppressedAnnouncement`
(`ExpandInRowDetailSuppressedByFilterAnnouncement` → "Your expanded item is hidden by the current filter"),
polite so it queues after the current screen-reader utterance rather than interrupting.

**AC3 — The e2e a11y lane reports no critical violations against the grid.**
**Given** the e2e a11y lane (`tests/e2e` Playwright `specimen-accessibility.spec.ts`),
**When** run against the specimen routes that render the projection grid + expanded detail,
**Then** `expectNoBlockingAxeViolations` reports **no serious/critical axe violations**, and the
in-process bUnit `AxeCoreA11yTests` proxy is green for the rendered surfaces.
**Note (host constraint):** Playwright is **CI-only on this host** (Node <24 / Playwright unsupported —
inherited from Epic-1 retro & Story 2.3). Locally, verify the spec + axe-gate wiring and the bUnit axe
proxy; the Layer-3 Playwright run is the CI gate, not a local DoD step. **Do not** attempt to install or
run Playwright here.

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before
> writing anything. Most subtasks should resolve to "already true → add/confirm the pin"; only open a
> `src/` change if a genuine AC gap is proven. Record what you found (true/false + the evidence) in the
> Dev Agent Record so the review can audit it.

- [ ] **Task 1 — Verify AC1: always-present `role="region"` panel + trigger wiring + single-expand (AC: #1)**
  - [ ] **Component layer — confirm ALREADY PINNED, no change.** Re-confirm `FcExpandInRowDetailTests`
    pins: `Expanded_RendersChildContentInsideRegion`, `Collapsed_KeepsRegionButSuppressesChildContent`
    (the always-present `role="region"` is the load-bearing WCAG 4.1.2 contract — D19),
    `Region_UsesProvidedAriaLabel`, `Region_UsesProvidedPanelId`,
    `Expanding_InvokesScrollStabilizerWithNonDefaultElementReference`,
    `RerenderWhileExpanded_DoesNotInvokeScrollStabilizerAgain`,
    `SuppressedThenExpanded_InvokesScrollStabilizerWhenDetailReturns`.
  - [ ] **Reducer layer — confirm ALREADY PINNED, no change.** Re-confirm `ExpandedRowReducerTests`:
    `ExpandRowAction_AddsEntry_WhenNoneExistsForViewKey`, `ExpandRowAction_ReplacesEntry_OnSameViewKey`
    (the single-expand REPLACE invariant, UX-DR17 / D4), `ExpandRowAction_PreservesOtherViewKeys`,
    `CollapseRowAction_RemovesExistingEntry`, `CollapseRowAction_IsIdempotent_WhenNoEntryExists`,
    `GetEntry_ReturnsNull_WhenViewKeyAbsent`. Also re-confirm `ExpandedRowActionsTests` (Contracts) pins
    the `ViewKey`/`ItemKey` guard surface.
  - [ ] **Emitter layer — confirm ALREADY PINNED, no change.** Re-confirm `RazorEmitterExpandInRowTests`:
    `DefaultGridStrategy_EmitsExpandedRowStateWiring`, `DefaultGridStrategy_EmitsRowClassRowClickAndHiddenBanner`,
    `DefaultGridStrategy_EmitsExpandTriggerColumn` (`aria-expanded` / `aria-controls` / `_expandPanelId` /
    `ChevronRight` / stop-propagation), `DefaultGridStrategy_EmitsDetailWrapperAndFactoredDetailBody`,
    `DefaultGridStrategy_EmitsClickToggleAndDisposeCollapseDispatches`,
    `StatusOverviewStrategy_EmitsExpandTriggerAndDetailWrapper`,
    `NonGridDetailStrategies_DoNotEmitExpandInRowMachinery` (DetailRecord/Timeline emit none).
  - [ ] **ASSESS the end-to-end gap (likely the real AC1 work).** The emitter test proves *generated
    source text*; `FcExpandInRowDetailTests` proves the *component in isolation*;
    `CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot` captures the
    `fc-expand-panel-{viewKey}-{guid}` markup in a Verify **snapshot** (scrubbed at
    `NormalizeGridMarkup`). If **no test behaviourally renders a generated grid, clicks the expand
    trigger, and asserts** (a) the `role="region"` panel is present *when collapsed*, (b) on expand the
    `ChildContent` populates the region, (c) `aria-expanded` flips and `aria-controls` resolves to the
    present panel id — add ONE focused bUnit pin reusing `GeneratedComponentTestBase` /
    `CounterProjectionView` (mirror the Story 2.3 `BadgeProjectionRenderTests` precedent). Set up the
    `fc-expandinrow.js` JS module via `JSInterop.SetupModule(...).SetupVoid("initializeExpandInRow", …)`
    as `AxeCoreA11yTests` does. **Only add `src/` change if a genuine gap is proven** — expect a
    pin-only outcome.
  - [ ] **Do not restyle.** The `role`/`aria-label` template/`data-testid`/chevron values are the shipped
    contract — pin them, don't "improve" them. No new `IStorageService.SetAsync` call site (the slice is
    EPHEMERAL by design — NFR17 tripwire).

- [ ] **Task 2 — Verify AC2: filter-hidden banner + visually-hidden suppressed-announcement live region (AC: #2)**
  - [ ] **Banner — confirm ALREADY PINNED.** Re-confirm `FcExpandedRowHiddenBannerTests`:
    `RendersNothing_WhenNotHiddenByFilter`, `RendersBannerCopy_WhenHiddenByFilter` (`role="status"` +
    `aria-live="polite"` + copy), `ClearFilterLink_DispatchesFiltersResetAction`.
  - [ ] **Suppressed-announcement live region — confirm ALREADY PINNED.** Re-confirm
    `FcExpandInRowDetailTests.SuppressedAnnouncement_RendersPoliteLiveRegion` (the visually-hidden
    `role="status"` + `aria-live="polite"` + `aria-atomic="true"` region renders the announcement text).
  - [ ] **ASSESS the host-view `_expandedItemHiddenByFilter` end-to-end gap.** The banner + announcement
    are pinned in isolation, and the emitter pins prove `IsHiddenByFilter` / `SuppressedAnnouncement` are
    wired (`ProjectionRoleBodyEmitter.cs` — `_expandedItemHiddenByFilter` derivation +
    `AddAttribute(...,"IsHiddenByFilter",…)` + `AddAttribute(...,"SuppressedAnnouncement",…)`). If **no
    test exercises the runtime flow** — expand a row in a generated grid, then dispatch a filter that
    removes its item from `state.Items`, and assert the banner appears AND the detail's suppressed
    announcement is populated — add ONE focused bUnit pin (extend the Task-1 generated-grid harness).
    **Pin-only is the expected outcome unless a genuine runtime gap surfaces** (recall Story 2.3's badge
    `TemplateColumn` fix — a render-time bug hidden behind source-only assertions; confirm by *rendering*,
    not just asserting emitted text).
  - [ ] **Do not reopen Story 4-3.** Per the Path B contract, the host view computes `IsHiddenByFilter`
    and passes the bool — the banner stays type-agnostic. Don't add a `_filterPredicate` field or
    recompute the predicate in the banner.

- [ ] **Task 3 — Verify AC3: e2e a11y lane wiring + in-process axe proxy (AC: #3)**
  - [ ] **Confirm the bUnit axe proxy is green.** Re-run `AxeCoreA11yTests`
    (`AxeCore_InlineRenderer_*`, `AxeCore_CompactInlineRenderer_*` — which sets up the
    `fc-expandinrow.js` module —, `AxeCore_FullPageRenderer_*`): **no serious/critical** violations on
    the rendered surfaces. If a generated **projection grid with an expanded row** is not covered by an
    in-process axe assertion, add ONE bUnit axe pin over the Task-1 generated-grid render (reuse the
    existing axe harness) so the expand surface has a no-blocking-violations proxy that runs in the
    default lane. **Only if not already covered.**
  - [ ] **Confirm (do NOT run) the Playwright lane wiring.** Verify `tests/e2e/specs/specimen-accessibility.spec.ts`
    drives `expectNoBlockingAxeViolations` over `specimenManifest.routes`, and the `type` specimen
    renders the grid + `fc-expanded-detail` / `fc-expanded-detail-summary` surfaces. **Do not install or
    run Playwright** (CI-only on this host — Node <24). Record that AC3's Layer-3 gate is CI-owned and
    the local evidence is the spec wiring + the bUnit axe proxy.

- [ ] **Task 4 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE.
  - [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — everything this story touches is green; new pins pass.
  - [ ] **Re-prove the standing 13-failure baseline** (8 Shell + 3 SourceTools + 2 Cli) is
    pre-existing/environmental, matching the Epic-1 retro record and the Story 2.3 review (Shell.Tests
    landed at **8 failed / 1725 passed / 1733 total** after 2.3). This story's pins land in
    **`Shell.Tests`** — capture the Shell.Tests before→after count and confirm the **same 8 pre-existing
    Shell failures** remain (none new, none misattributed). `src/` is expected untouched, so the
    SourceTools/Cli clusters are unchanged.
  - [ ] **If no render behaviour changed → ZERO `.verified.txt` snapshot edits.** (Confirm-and-pin:
    default zero `src/` change should hold. If a genuine `src/` fix lands and changes generated markup,
    update `CounterStoryVerificationTests.*RenderSnapshot.verified.txt` and any emitter snapshot
    **intentionally**.)

- [ ] **Task 5 — Honest record-keeping (retro AI-1 / AI-2)**
  - [ ] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in
    the Dev Agent Record below, reconciled against the actual git tree (this is the recurring Epic-1
    review finding — pay it up front).
  - [ ] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no
    stray `</content>` / `</invoke>` / tool-call tags.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`81eebf9`) | This story |
|---|---|---|
| Always-present `role="region"` detail panel + aria-label | **Exists & pinned (component)** — `FcExpandInRowDetail.razor:10`, `FcExpandInRowDetailTests` | Confirm; **assess end-to-end generated-grid pin** |
| Expand-trigger column (`aria-expanded`/`aria-controls`/chevron) | **Exists & pinned (emitter source)** — `RazorEmitterExpandInRowTests` | Confirm; assess rendered-grid pin |
| Single-expand REPLACE invariant (UX-DR17) | **Exists & pinned** — `ExpandedRowReducers`, `ExpandedRowReducerTests` | Confirm; no change |
| `ExpandRowAction`/`CollapseRowAction` guard surface | **Exists & pinned** — `ExpandedRowActions.cs`, `ExpandedRowActionsTests` | Confirm; no change |
| Scroll-stabilizer JS interop (D8 guard) | **Exists & pinned** — `IExpandInRowJSModule`, `OnAfterRenderAsync` | Confirm; no change |
| Filter-hidden breadcrumb banner | **Exists & pinned (component)** — `FcExpandedRowHiddenBanner`, `FcExpandedRowHiddenBannerTests` | Confirm; **assess host-view runtime pin** |
| Visually-hidden suppressed-announcement live region | **Exists & pinned** — `FcExpandInRowDetail.razor:23-31`, `…SuppressedAnnouncement_RendersPoliteLiveRegion` | Confirm; assess runtime pin |
| Host-view `_expandedItemHiddenByFilter` derivation | **Exists** — `ProjectionRoleBodyEmitter.cs` (emitter source pinned) | **Assess runtime end-to-end pin** |
| e2e a11y axe gate (Playwright) | **Exists** — `specimen-accessibility.spec.ts` | Confirm wiring; **CI-only run** |
| in-process axe proxy | **Exists & pinned** — `AxeCoreA11yTests` | Confirm; assess expanded-grid coverage |

> **Key judgment for the dev agent:** the deliverable is **confidence + durable end-to-end pins on the
> generated-grid expand flow (AC1) and the filter-hidden runtime flow (AC2)**, not new features and not
> component/reducer/emitter churn. If you find yourself editing `FcExpandInRowDetail.razor`,
> `FcExpandedRowHiddenBanner.razor`, `ExpandedRowReducers.cs`, `ExpandedRowActions.cs`, or the expand
> branches of `ProjectionRoleBodyEmitter.cs`, **stop** — re-read the AC and confirm you've found a
> *genuine* gap, not a style preference. The component, reducer, and emitter surfaces are already locked;
> resist re-pinning them. Story 2.3's lesson: a source-only assertion can give **false confidence** —
> prove AC1/AC2 by *rendering a generated grid and interacting*, which is exactly where the residual gap
> is.

### Exact anchors (read these before touching anything)

**AC1 — always-present region + trigger + state**
- **Detail component** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandInRowDetail.razor`
  (always-rendered `<div role="region" aria-label=… class="fc-expand-in-row-detail" @ref>` `:10-18`,
  `@ChildContent` gated on `HasExpanded` `:15`; visually-hidden suppressed live region `:23-31`) +
  `.razor.cs` (`HasExpanded`/`ChildContent`/`DetailPanelAriaLabel`/`SuppressedAnnouncement`/`PanelId`
  params; `EffectivePanelId` `:74`; D8 scroll-stabilizer guard in `OnAfterRenderAsync` `:77-87`).
- **JS module** — `src/Hexalith.FrontComposer.Shell/Services/IExpandInRowJSModule.cs` (scoped cache,
  prerender-safe `InitializeAsync`); JS at `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js`.
- **State slice** — `src/Hexalith.FrontComposer.Shell/State/ExpandedRow/`:
  `ExpandedRowState.cs` (`ExpandedByViewKey` dict, EPHEMERAL — no `Persist`, `GetEntry` `:43`),
  `ExpandedRowReducers.cs` (`ReduceExpandRow` REPLACE `:20-29`, `ReduceCollapseRow` idempotent `:35-47`),
  `ExpandedRowEntry.cs`, `ExpandedRowFeature.cs` (empty initial state).
- **Actions (contract)** — `src/Hexalith.FrontComposer.Contracts/Rendering/ExpandedRowActions.cs`
  (`ExpandRowAction` `:19-50` — `ViewKey` non-empty guard `:34-43`, `ItemKey` non-null guard `:46-49`;
  `CollapseRowAction` `:59-80`).
- **Generator emission** — `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
  (`emitExpandableRows` block: `_expandedItemKey`/`_expandedItem`/`_expandedItemHiddenByFilter` derivation
  `~:426-450`; trigger column + `FcExpandInRowDetail` wrapper + `SuppressedAnnouncement` wiring
  `~:580-620`) and `Emitters/RazorEmitter.cs`. **Never hand-edit generated output** — change the emitter.

**AC2 — filter-hidden announcement**
- **Banner** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandedRowHiddenBanner.razor`
  (`@if (IsHiddenByFilter)` → `role="status"` + `aria-live="polite"` +
  `data-testid="fc-expanded-row-hidden-banner"` `:10-21`; "Clear filter" `FluentButton` `:16-20`) +
  `.razor.cs` (`ViewKey`/`IsHiddenByFilter` params; `_bannerMessage` from `ExpandedRowHiddenByFilterBanner`
  `:53`; `OnClearFilterClickedAsync` → `FiltersResetAction` `:56-59`).
- **Suppressed live region** — inside `FcExpandInRowDetail.razor:23-31` (visually-hidden `role="status"`,
  `aria-live="polite"`, `aria-atomic="true"`), fed by `SuppressedAnnouncement`.
- **Host-view derivation** — `ProjectionRoleBodyEmitter.cs`:
  `_expandedItemHiddenByFilter = _expandedItemKey is not null && _expandedItem is null` (`:130` status-overview
  variant, `:443` default-grid variant); `IsHiddenByFilter` attribute `:449`; `SuppressedAnnouncement`
  attribute `:593`/`:618`.

**AC3 — a11y lanes**
- **Playwright (CI-only)** — `tests/e2e/specs/specimen-accessibility.spec.ts`
  (`expectNoBlockingAxeViolations` per route `:51-61`; `type` specimen asserts `fc-expanded-detail`
  `:78`; keyboard flow reaches `fc-expanded-detail-summary` `:116`) + `tests/e2e/helpers/a11y.ts`.
- **in-process axe proxy** — `tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs`
  (sets up `fc-expandinrow.js` module `:34`).

> ⚠️ **Line numbers are guidance, not contracts.** They reflect `81eebf9`; confirm by symbol/marker
> before relying on any single line. Cite the symbol, not the line, in new pins.

### Localized strings (EN; FR sibling carries the non-breaking-space variants)

| Resource key | EN value | Use |
|---|---|---|
| `ExpandRowButtonAriaLabelTemplate` | `Expand details for {0}` | trigger aria-label (collapsed) |
| `CollapseRowButtonAriaLabelTemplate` | `Collapse details for {0}` | trigger aria-label (expanded) |
| `ExpandInRowDetailPanelAriaLabelTemplate` | `Details for {0}` | region landmark aria-label |
| `ExpandInRowDetailSuppressedByFilterAnnouncement` | `Your expanded item is hidden by the current filter` | visually-hidden live region |
| `ExpandedRowHiddenByFilterBanner` | `{0} expanded item hidden by current filter` | breadcrumb banner copy ({0}="1" in v1) |
| `ExpandedRowHiddenByFilterBannerClearLink` | `Clear filter` | banner action link |

> v1 is single-expand (UX-DR17) so the banner count is always "1"; ICU pluralization bundles with the
> multi-expand v2 commit per the Known Gaps ledger. Pin `CultureInfo.CurrentUICulture = "en"` (use the
> existing `CultureScope` helper) for resource-key stability in new pins.

### Test anchors (where pins live / go)

- **Detail component (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandInRowDetailTests.cs`.
- **Banner (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs`.
- **Reducer (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/State/ExpandedRow/ExpandedRowReducerTests.cs`.
- **Actions contract (confirm)** — `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ExpandedRowActionsTests.cs`.
- **Emitter shape (confirm)** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs`.
- **End-to-end generated-grid pins (NEW — AC1/AC2 gap)** — add under
  `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`, reusing `GeneratedComponentTestBase` /
  `CounterProjectionView` (and `JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js")`
  per `AxeCoreA11yTests`). Follow the Story 2.3 `BadgeProjectionRenderTests`/`BadgeProjectionSpecimen`
  precedent for a focused, named render pin. Use a recording dispatcher / real store
  (`InitializeStoreAsync`) to drive `ExpandRowAction` and a filter that empties `state.Items`.
- **Snapshot (confirm; touch only on intentional `src/` change)** —
  `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.*RenderSnapshot.verified.txt`.
- **in-process axe (confirm; assess expanded-grid coverage)** —
  `tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs`.

### Project-context guardrails that apply here (non-negotiable)

- **Fluxor single-writer discipline (ADR-007):** one dispatch source per action type; **effects own
  persistence + JS interop**, reducers stay pure (the `ExpandedRow` reducers NEVER chain dispatches).
  This story is verify/pin — the `ExpandedRow` slice is **EPHEMERAL by design** (no `Persist`, no
  LocalStorage hydration); **no new `IStorageService.SetAsync` call site** is expected — adding one trips
  the **NFR17 tripwire** (requires updating the tripwire whitelist + the story compliance matrix).
- **Scoped-lifetime discipline (ADR-030):** storage/effects/auth/tenant accessors AND `IExpandInRowJSModule`
  are scoped — never captured in singletons.
- **Generator rules (AC1/AC2 emission):** **never hand-edit generated code**
  (`obj/**/generated/HexalithFrontComposer/`) — fix the emitter or the annotated type. **No `ISymbol`
  escapes the parse stage**; keep IR pure & `EquatableArray`-based. **Diagnostics travel as
  `DiagnosticInfo`**, converted to Roslyn `Diagnostic` only inside `RegisterSourceOutput`. `SourceTools`
  references **only** `Contracts` (netstandard2.0-clean).
- **Icons:** use the custom inline-SVG `FcFluentIcons` factory (the chevron), **not** a FluentUI icons NuGet.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix,
  **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE) — note `OnAfterRenderAsync`
  in `FcExpandInRowDetail` deliberately uses `ConfigureAwait(true)` to stay on the renderer's sync
  context for the JS interop, which is correct; don't "fix" it. `ArgumentNullException.ThrowIfNull` at
  public boundaries, **no copyright/license headers** (this repo has none), **CRLF**, 4-space indent,
  final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for
  components (`JSInterop.Mode = JSRuntimeMode.Loose`, set up the `fc-expandinrow.js` module);
  **Verify.XunitV3** (NOT `Verify.Xunit`) for any snapshot; plural `{Class}Tests.cs`; three-part
  `Subject_Scenario_Expectation` method names; **solution-level** `dotnet test` + trait filters (not
  per-project); run with **`DiffEngine_Disabled=true`** (else Verify hangs); `.verified.txt` updated
  **intentionally** and committed; **`FakeTimeProvider`** for any timer-driven path (never `Thread.Sleep`/
  wall-clock — use `WaitForAssertionAsync` for async render settling).
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress;
  built-in analyzers only (no Sonar/StyleCop/Roslynator); centralized versions in
  `Directory.Packages.props` (never add `Version=` to a `.csproj`).
- **Commits/branches:** Conventional Commits; this work is **`test:`/`fix:`** shaped (verification +
  pins), **not `feat:`** unless a genuine new capability is added (a false `feat:` triggers a minor bump +
  NuGet publish). Already on a feature branch (`feat/story-1-2-fc-lyt-page-layout`); do **not** commit to
  `main`.

### Epic dependencies & their state

| Epic-2 needs | From | State at kickoff |
|---|---|---|
| DataGrid filtering (drives the AC2 "filter hides expanded row" path) | Story 2.3 | ✅ done & pinned — `FilterReducerTests`, `FcColumnFilterCell`, `FiltersResetAction`. The banner's "Clear filter" dispatches `FiltersResetAction` (Story 4-3/2.3 surface). |
| Projection Loading/Empty/Data dispatch + generated grid | Story 2.1 | ✅ pinned — `CounterStoryVerificationTests`, `GeneratedComponentTestBase`. The expand machinery only emits for grid strategies (Default / StatusOverview), not DetailRecord / Timeline. |
| Badge `aria-label` + status columns in the same grid | Story 2.3 | ✅ done — coexist with the expand trigger column; don't re-pin badges here. |
| Registry-driven nav/home around the grid | Story 2.2 | ✅ done; not on this story's path. |
| FC-A11Y accessibility primitives (role/aria-label ready-gate) | Story 1.3 | ✅ Layer-1 (aria-label/role) pinned in bUnit; Layer-3 e2e axe is **CI-only** (Playwright unsupported on this host) → AC3's Playwright gate is CI-owned, local evidence is the bUnit axe proxy + spec wiring. |
| FC-TBL table/column/expand API confirmation | Story 2.8 | ⏳ **Story 2.8 owns the formal FC-TBL confirm-stable.** This story *renders/interacts with* the expand surface; it does **not** confirm the FC-TBL public surface or touch `PublicAPI.Shipped.txt`. There is **no FC-TBL contract doc** in `_bmad-output/contracts/` yet — that is 2.8's deliverable. |

> **Scope boundary:** this story is "**accessible expand-in-row detail**" (AC1 always-present region,
> AC2 filter-hidden announcement, AC3 e2e a11y). It is *not* column prioritization (2.5 —
> `FcColumnPrioritizer`), live updates / reconnect (2.6 — `FcNewItemIndicator` /
> `FcProjectionConnectionStatus` / `FcSlowQueryNotice` / `FcMaxItemsCapNotice`), command palette /
> global search (2.7), or FC-TBL confirmation (2.8). Stay inside AC1–AC3. Do not re-pin Story 2.3's
> filtering surface (you *use* `FiltersResetAction`, you don't re-prove it).

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The expand-in-row surface (FR11, NFR6, UX-DR6, WCAG 4.1.2)
is shipped and partially tested at `81eebf9`. **Done = each of AC1/AC2/AC3 is proven true against `src/`
at this commit and carries a durable regression pin** — specifically the under-pinned **end-to-end
generated-grid flows** (AC1: expand-click → always-present `role="region"` populates, trigger
`aria-expanded`/`aria-controls`; AC2: filter hides an expanded row → `FcExpandedRowHiddenBanner` +
`SuppressedAnnouncement` live region) are closed with new bUnit pins; the component / reducer / emitter /
banner surfaces are re-confirmed green; the in-process axe proxy covers the expanded grid; the Playwright
AC3 gate is confirmed wired (CI-owned, not run locally); the Release build is 0/0 under TWAE; the default
test lane is green; the standing 13-failure baseline is re-proved pre-existing; and the File List +
counts are accurate. Genuine `src/` change is the exception, not the expectation — and if made, it lands
in the component or emitter (never generated output) with intentional snapshot updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.4] — story statement, ACs (FR11, NFR6, UX-DR6, WCAG 4.1.2)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage; FR11 (DataGrid surface) → Epic 2
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements] — UX-DR6 (expand-in-row detail), UX-DR17 (single-expand)
- [Source: _bmad-output/project-context.md] — Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), NFR17 tripwire, generator rules, `FcFluentIcons`, TWAE, test discipline, `DiffEngine_Disabled=true`
- [Source: _bmad-output/project-docs/component-inventory.md] — `FcExpandInRowDetail` / `FcExpandedRowHiddenBanner` entry; `ExpandedRow` state slice
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] — FC-A11Y ready-gate (aria-label/role Layer-1 pins; e2e axe Layer-3 CI-only)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] — FullWidth default for DataGrid-dense projection pages
- [Source: _bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md] — prior confirm-and-pin pattern; `BadgeProjectionRenderTests` end-to-end-render precedent; source-only-assertion false-confidence lesson; 13-failure baseline; Shell.Tests 8/1725/1733 baseline; retro AI-1/AI-2 taxes
- [Source: _bmad-output/implementation-artifacts/2-1-render-a-projection-from-a-projection-type.md] — generated-grid render harness (`GeneratedComponentTestBase`, `CounterStoryVerificationTests`)
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md#3,#6] — File-List/sentinel taxes (AI-1/AI-2), 13-failure baseline, Epic-2 dependency states, Playwright CI-only host constraint
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandInRowDetail.razor] — always-present `role="region"` panel + visually-hidden suppressed live region (AC1/AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandedRowHiddenBanner.razor] — filter-hidden breadcrumb `role="status"` + `aria-live` (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/ExpandedRow/ExpandedRowReducers.cs] — single-expand REPLACE invariant + idempotent collapse (AC1)
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/ExpandedRowActions.cs] — `ExpandRowAction`/`CollapseRowAction` guard surface (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Services/IExpandInRowJSModule.cs] — scoped, prerender-safe scroll-stabilizer interop (AC1)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs] — `_expandedItemHiddenByFilter` derivation + `IsHiddenByFilter`/`SuppressedAnnouncement` wiring (AC1/AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandInRowDetailTests.cs] — detail-component pins (confirm; AC1/AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs] — banner pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/ExpandedRow/ExpandedRowReducerTests.cs] — reducer pins (confirm; AC1)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs] — emitter-shape pins (confirm; AC1/AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs] — in-process axe proxy (confirm; AC3)
- [Source: tests/e2e/specs/specimen-accessibility.spec.ts] — Playwright axe gate over specimen grid + expanded detail (confirm wiring; AC3, CI-only)

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
