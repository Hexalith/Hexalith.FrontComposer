---
baseline_commit: 8036c3c5ea1b44ddc2e8f6ccf8af97da4c74999d
---

# Story 2.3: DataGrid filtering, status, and empty/loading states

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Stories 2.1 and 2.2, the entire DataGrid filtering + status-badge + empty/loading surface
> **already exists, renders, and is partially tested** at baseline `8036c3c`. The shipped surface
> (much of it carries `Story 4-x` docstring markers because the brownfield code was authored ahead of
> this epic's confirm-and-pin numbering — **that is expected; do not "re-attribute" or churn it**):
>
> - **AC1 filtering** — `FcColumnFilterCell` (300 ms `TimeProvider`-driven debounce → `ColumnFilterChangedAction`),
>   `FcFilterSummary` (`role="status"`, "showing X of Y" + per-filter clauses), `FcFilterResetButton`
>   (`FiltersResetAction`, disabled when no active filters), `FcStatusFilterChips`, and the
>   `DataGridNavigation` Fluxor slice (`FilterActions` + `FilterEffects` + reducers, reserved-key
>   `__`-prefix guard).
> - **AC2 loading/empty** — `FcProjectionLoadingSkeleton` (`SkeletonLayout` = `DataGrid`/`Card`/`Timeline`,
>   `role="status"` + `aria-busy="true"`) and `FcProjectionEmptyPlaceholder` (`role="status"`, optional
>   auth-gated CTA), plus a distinct **filter-induced-empty** state `FcFilterEmptyState`
>   (`role="status"` + `aria-live="polite"`).
> - **AC3 status badges** — `FcStatusBadge` / `FcDesaturatedBadge` (mandatory `aria-label` composed from
>   `ColumnHeader` + `Label`), the `[ProjectionBadge]` → `BadgeSlot` mapping, and the `ColumnEmitter`
>   switch that emits the badge column (mapped arm → `FcStatusBadge`, unannotated arm → plain text,
>   default arm → localized `StatusBadgeUnknownStateFallback`).
>
> **So this story's job is to (1) VERIFY each of the three ACs holds end-to-end against `src/` at this
> commit, (2) CLOSE the durability gaps with focused regression pins, and (3) make the verification
> durable** so the rest of Epic 2 (expand-in-row 2.4, column prioritization 2.5, live updates 2.6)
> builds on a pinned grid baseline. **Default to ZERO `src/` change.** The real gap is that the three
> **filter UI components have NO dedicated bUnit test files** — `FcColumnFilterCell`, `FcFilterSummary`,
> `FcFilterResetButton`, `FcStatusFilterChips`, `FcFilterEmptyState` are exercised only indirectly. The
> filter **state/reducer** (`FilterReducerTests`) and the **badge/skeleton/empty components** are
> already pinned; confirm those, then close the filter-UI gap. If a real `src/` gap is found, fix the
> component (never hand-edit generated output) and update affected `.verified.txt` snapshots
> **intentionally**. If a behaviour is already correct, **pin it — do not "improve" or restyle** working
> output (Epic-1 retro §5 flags "copy-a-pattern-without-the-difference" as the rising Epic-2 risk).

## Story

As an operator,
I want to filter projection rows and see clear loading/empty/status feedback,
so that I can narrow large read-models and always know the grid's state.

## Acceptance Criteria

**AC1 — A debounced `ColumnFilterChangedAction` filters rows; a filter summary and reset button are shown. *(FR11)***
**Given** a projection grid,
**When** I type in a column filter,
**Then** the input debounces (300 ms, `TimeProvider`-driven) and dispatches a **`ColumnFilterChangedAction`** that updates the `DataGridNavigation` grid-view snapshot (writing a non-empty value, removing the key when cleared, and **refusing** reserved `__`-prefixed keys),
**And** an `FcFilterSummary` (`role="status"`) shows the active-filter description (status / column / search / sort clauses + "showing X of Y"),
**And** an `FcFilterResetButton` is shown that dispatches `FiltersResetAction` (→ `ClearGridStateAction`) and is **disabled when no filter is active**.

**AC2 — Loading and no-row states render the correct placeholder. *(FR11, UX-DR5)***
**Given** the projection query is **loading**,
**When** rendered,
**Then** `FcProjectionLoadingSkeleton` shows with the role-appropriate `SkeletonLayout` (`DataGrid` / `Card` / `Timeline`), carrying `role="status"` and `aria-busy="true"`,
**And given** the query **returns no rows**, `FcProjectionEmptyPlaceholder` shows (`role="status"`, with its optional auth-gated CTA),
**And** a filter that narrows the grid to **zero rows** surfaces the distinct `FcFilterEmptyState` (`role="status"` + `aria-live="polite"`) rather than the no-data placeholder.

**AC3 — `[ProjectionBadge]` status-enum columns render through `FcStatusBadge`/`FcDesaturatedBadge` with a mandatory `aria-label`. *(UX-DR2, NFR6)***
**Given** a status-enum column mapped via `[ProjectionBadge]`,
**When** the generated grid column renders,
**Then** the `ColumnEmitter` badge switch emits an `FcStatusBadge` arm per mapped enum member (with the `Slot` from `BadgeSlot` and the `ColumnHeader` supplied for aria-label context), a plain-text arm for declared-but-unannotated members, and a default arm using the localized `StatusBadgeUnknownStateFallback`,
**And** the rendered `FcStatusBadge`/`FcDesaturatedBadge` carries a **mandatory `aria-label`** composed as `"{ColumnHeader}: {Label}"` (falling back to `{Label}` when no column header), localized (FR-locale uses the non-breaking space before the colon).

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before
> writing anything. Most subtasks should resolve to "already true → add/confirm the pin"; only open a
> `src/` change if a genuine AC gap is proven. Record what you found (true/false + the evidence) in the
> Dev Agent Record so the review can audit it.

- [x] **Task 1 — Verify AC1: debounced filter action + filter summary + reset button (AC: #1)**
  - [x] **State/reducer layer — confirm ALREADY PINNED, no change.** Re-confirm `FilterReducerTests`
    pins: `ColumnFilterChanged_WritesKeyWhenNonEmpty`, `ColumnFilterChanged_NullRemovesKey`,
    `ColumnFilterChanged_RejectsReservedKey` (the `__`-prefix guard), `StatusFilterToggled_*`,
    `GlobalSearchChanged_WritesReservedSearchKey`, `SortChanged_*`,
    `FiltersReset_DispatchesClearGridStateAction`. Also confirm the record-level guard in
    `FilterActions.ColumnKey` rejects `__`-prefixed keys (`FilterActions.cs:48-50`).
  - [x] **CLOSE the filter-UI debounce gap (durability pin).** `FcColumnFilterCell` has **NO dedicated
    test file**. Add `FcColumnFilterCellTests` (bUnit + `FakeTimeProvider`) that: (a) typing a value and
    advancing the fake clock by **< 300 ms** dispatches **nothing**; (b) advancing **past 300 ms**
    dispatches exactly one `ColumnFilterChangedAction` with the typed value; (c) clearing the input
    dispatches `ColumnFilterChangedAction` with a null/empty `FilterValue`; (d) the cell hydrates its
    initial value from the grid-view snapshot (`OnParametersSet`); (e) dispose cancels a pending
    debounce without dispatching. Use `Microsoft.Extensions.TimeProvider.Testing` (already a dependency).
  - [x] **CLOSE the filter-summary gap.** `FcFilterSummary` has **NO dedicated test file**. Add
    `FcFilterSummaryTests` asserting: `role="status"` + `data-testid="fc-filter-summary"`; a clause per
    active status / column / search / sort filter; reserved `__`-prefixed keys excluded from the
    column-filter clauses (`FcFilterSummary.razor.cs:120-124`); culture pinned `en`.
  - [x] **CLOSE the reset-button gap.** `FcFilterResetButton` has **NO dedicated test file**. Add
    `FcFilterResetButtonTests`: dispatches `FiltersResetAction(ViewKey)` on click; **disabled when
    `HasActiveFilters` is false**; aria-label reflects the active-filter count. Optionally pin
    `FcStatusFilterChips` toggle → `StatusFilterToggledAction` in the same pass.
  - [x] **Do not restyle.** The `role`/`data-testid`/appearance values are the shipped contract — pin
    them, don't "improve" them. Do not add a new `IStorageService.SetAsync` call site (NFR17 tripwire).

- [x] **Task 2 — Verify AC2: loading skeleton + empty placeholder + filter-empty state (AC: #2)**
  - [x] **Skeleton + placeholder — confirm ALREADY PINNED.** Re-confirm `FcProjectionLoadingSkeletonTests`
    (`RendersDataGridLayoutByDefault_WithAriaBusyAndRoleStatus`, `RendersCardLayoutWhenLayoutIsCard`,
    `RendersTimelineLayoutWhenLayoutIsTimeline`, `RespectsRowCount`, `UsesEntityLabelInResolvedAriaLabel`)
    and `FcProjectionEmptyPlaceholderTests` (humanized plural + message, EN/FR aria-label, role-specific
    copy, CTA auth-gating incl. `CtaIsHiddenForAnonymousUsers_AC2_5`, parameter-surface freeze) hold.
    Confirm `SkeletonLayout` enum = `DataGrid`/`Card`/`Timeline` (`SkeletonLayout.cs:9-17`).
  - [x] **CLOSE the filter-induced-empty gap.** `FcFilterEmptyState` (`FcFilterEmptyState.razor:7` —
    `role="status"` + `aria-live="polite"` + `data-testid="fc-filter-empty-state"`) has **NO dedicated
    test file**. Add `FcFilterEmptyStateTests` asserting it renders with the live-region attributes and
    its reset affordance, and is the surface shown for **filtered-to-zero** (distinct from the no-data
    `FcProjectionEmptyPlaceholder`). **Only add a `src/` change if a genuine gap is proven** — expect a
    pin-only outcome.
  - [x] Confirm the projection view's Loading/Empty/Data dispatch is unchanged from Story 2.1 (the
    `ProjectionRole` strategy switch in the emitter). Do **not** re-pin 2.1's emitter switch here.

- [x] **Task 3 — Verify AC3: `[ProjectionBadge]` → `FcStatusBadge` column with mandatory aria-label (AC: #3)**
  - [x] **Component + emission — confirm ALREADY PINNED, expect ZERO change.** Re-confirm
    `FcStatusBadgeTests` (`ResolvesSlotToFluentColorAndAppearance` incl. `role="status"` +
    `data-fc-badge-slot`; `AriaLabelCombinesColumnHeaderAndLabelInEnglish` →
    `aria-label="Status: Pending"`; `AriaLabelUsesFrenchNonBreakingSpaceBeforeColon`; null/empty
    column-header fallbacks; `DoesNotExposeColorOrAppearanceParametersOnPublicApi`), `FcDesaturatedBadgeTests`,
    `SlotAppearanceTableTests`/`SlotContrastTests`, and the emission pins
    `RazorEmitterBadgeColumnTests` (`ZeroMappings_EmitsPlainTextPath_AndNoBadgeChildContent`,
    `PartialMappings_EmitsSwitchWithBadgeArmsPlusTextDefault`,
    `NullableEnum_WithBadgeMappings_EmitsNullCheckBeforeSwitch`,
    `BadgeLabelUsesHumanizeEnumLabel_NotResourceLookup`,
    `BadgeEmission_SuppliesColumnHeaderForAriaLabelContext`, `BadgeEmission_ParsesAsValidCSharp`).
  - [x] **Assess the rendered-grid badge gap.** The emission tests prove the *generated source*; the
    component tests prove the *badge in isolation*. If no test renders a **generated grid column** and
    asserts the badge's `aria-label="{ColumnHeader}: {Label}"` flows through end-to-end, add ONE focused
    bUnit pin (reuse the Story 2.1 `GeneratedComponentTestBase` / Counter specimen) — only if the
    end-to-end aria-label flow is not already covered by `CounterStoryVerificationTests` or the 2.1
    `ActionQueueProjection_RendersBadgeColumn_ThroughFcStatusBadge_WithAccessibleColumnHeader` pin.
    Do **not** duplicate an existing pin.
  - [x] **Do not restyle badges.** `BadgeSlot`→(Color, Appearance) via `SlotAppearanceTable` is the
    shipped contract; pin the values, don't change them.

- [x] **Task 4 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE.
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — everything this story touches is green; new pins pass.
  - [x] **Re-prove the standing 13-failure baseline** (8 Shell + 3 SourceTools + 2 Cli) is
    pre-existing/environmental, matching the Epic-1 retro record. This story's pins land in
    **`Shell.Tests`** — capture the Shell.Tests before→after count and confirm the **same 8 pre-existing
    Shell failures** remain (none new, none misattributed). `src/` is expected untouched, so the
    SourceTools/Cli clusters are unchanged.
  - [x] **If no render behaviour changed → ZERO `.verified.txt` snapshot edits.** (Confirm-and-pin:
    default zero `src/` change should hold.)

- [x] **Task 5 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in
    the Dev Agent Record below, reconciled against the actual git tree (this is the recurring Epic-1
    review finding — pay it up front).
  - [x] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no
    stray `</content>` / `</invoke>` / tool-call tags.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`8036c3c`) | This story |
|---|---|---|
| `ColumnFilterChangedAction` + reserved-key guard | **Exists & pinned (state)** — `FilterActions.cs:8-60`, `FilterReducerTests` | Confirm; **add filter-UI debounce pin** |
| 300 ms debounce in the filter cell | **Exists** — `FcColumnFilterCell.razor.cs:19` (`TimeProvider`) | **Add pin** (no test file today) |
| Filter summary ("showing X of Y" + clauses) | **Exists** — `FcFilterSummary.razor:6` (`role="status"`) | **Add pin** (no test file today) |
| Filter reset button (`FiltersResetAction`, disabled-when-empty) | **Exists** — `FcFilterResetButton.razor.cs:46-49` | **Add pin** (no test file today) |
| Status-filter chips | **Exists** — `FcStatusFilterChips.razor` | Optional pin alongside |
| Loading skeleton (DataGrid/Card/Timeline) | **Exists & pinned** — `FcProjectionLoadingSkeletonTests` | Confirm; no change |
| Empty placeholder (+ auth-gated CTA) | **Exists & pinned** — `FcProjectionEmptyPlaceholderTests` | Confirm; no change |
| Filter-induced empty state | **Exists** — `FcFilterEmptyState.razor:7` (`role="status"` + `aria-live`) | **Add pin** (no test file today) |
| `[ProjectionBadge]` → `FcStatusBadge` column emission | **Exists & pinned** — `RazorEmitterBadgeColumnTests` | Confirm; no change |
| `FcStatusBadge`/`FcDesaturatedBadge` aria-label | **Exists & pinned** — `FcStatusBadgeTests`/`FcDesaturatedBadgeTests` | Confirm; assess end-to-end grid pin |

> **Key judgment for the dev agent:** the deliverable is **confidence + durable pins on the
> under-pinned filter-UI components (AC1) and the filter-empty state (AC2)**, not new features and not
> badge/skeleton churn. If you find yourself editing `FcColumnFilterCell.razor`, `FcFilterSummary.razor`,
> `FcProjectionLoadingSkeleton.razor`, `FcStatusBadge.razor`, the `DataGridNavigation` slice, or
> `ColumnEmitter.cs`, **stop** — re-read the AC and confirm you've found a *genuine* gap, not a style
> preference. The badge + skeleton + empty-placeholder surfaces are already locked; resist re-pinning
> them.

### Exact anchors (read these before touching anything)

**AC1 — Filtering**
- **Filter actions (contract)** — `src/Hexalith.FrontComposer.Contracts/Rendering/FilterActions.cs`:
  `ColumnFilterChangedAction` (`:8-60`; `ColumnKey` `__`-prefix guard at `:41-56`, `FilterValue` nullable
  `:59`), `StatusFilterToggledAction` (`:62-103`), `GlobalSearchChangedAction` (`:109-135`),
  `SortChangedAction` (`:142-173`), `FiltersResetAction` (`:180-201`, chains `ClearGridStateAction`).
  Reserved keys in `src/Hexalith.FrontComposer.Contracts/Rendering/ReservedFilterKeys.cs` (`StatusKey`,
  `SearchKey`).
- **Filter effects/reducer** — `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/FilterEffects.cs`
  (`HandleColumnFilterChanged` `:54-78`, reserved-key invariant `:58`, normalize `:66`, capture `:76`;
  `HandleStatusFilterToggled` `:82-102`; `HandleGlobalSearchChanged` `:106-123`; `HandleSortChanged`
  `:127-139`; `HandleFiltersReset` `:144-150`; `GetOrEmptySnapshot` `:152-165`). Slice:
  `State/DataGridNavigation/{DataGridNavigationState,DataGridNavigationReducers,DataGridNavigationEffects,GridViewPersistenceBlob}.cs`.
- **Filter UI components** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/`:
  `FcColumnFilterCell.razor(.cs)` (debounce `DebounceInterval = 300 ms` `:19`; `OnValueChangedAsync`
  `Task.Delay(DebounceInterval, Time, token)` `:79`, dispatch on complete `:90`; `Dispose` cancels `:94-107`),
  `FcFilterSummary.razor(.cs)` (`role="status"` `:6`; `BuildClauses` `:73-95`; reserved-key exclusion
  `:120-124`; `ComposeSortClause` `:160-164`), `FcFilterResetButton.razor(.cs)` (disabled-when-empty,
  `OnResetClickedAsync` → `FiltersResetAction` `:46-49`), `FcStatusFilterChips.razor(.cs)`,
  `FcFilterEmptyState.razor` (`:7` live region).

**AC2 — Loading / empty**
- **Skeleton** — `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionLoadingSkeleton.razor`
  (`role="status"` + `aria-busy="true"` `:4-7`; Card `:8-19`, Timeline `:21-33`, DataGrid `:34-53`
  variants) + `.razor.cs` (`ColumnCount`/`RowCount`/`Layout`/`AriaLabel`/`EntityLabel` params;
  `ResolvedAriaLabel` `:57-60`; `CssClass` `:62-66`). Enum
  `src/Hexalith.FrontComposer.Shell/Components/Rendering/SkeletonLayout.cs` (`DataGrid`/`Card`/`Timeline` `:9-17`).
- **Empty placeholder** — `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor`
  (`role="status"` `:7-9`; auth-gated CTA `:12-37`) + `.razor.cs` (params + CTA resolution `:118-224`).
- **Filter-empty** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterEmptyState.razor(.cs)`
  (`role="status"` + `aria-live="polite"` + `data-testid="fc-filter-empty-state"` `:7`).

**AC3 — Status badges**
- **Components** — `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor(.cs)`
  (`Slot`/`Label`/`ColumnHeader` params; `OnParametersSet` resolves slot via `SlotAppearanceTable.Resolve`
  `:54` + composes aria-label `:57-59`; markup `role="status"` + `data-fc-badge-slot` +
  `data-testid="fc-status-badge"` `:11-18`), `FcDesaturatedBadge.razor(.cs)` (optimistic wrapper;
  `AriaLabel` `:73-85`). `BadgeSlot` enum at `src/Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs`;
  `SlotAppearanceTable` maps slot → (Color, Appearance).
- **Attribute + mapping** — `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionBadgeAttribute.cs`
  (`[AttributeUsage(Field)]`), parsed into `ColumnModel.BadgeMappings` (`BadgeMappingEntry`:
  `EnumMemberName`, `Slot`).
- **Badge emission** — `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`: enum column
  dispatch `:167-188` (badge path guarded by `col.BadgeMappings.Count > 0` `:181`),
  `EmitEnumBadgeChildContent` `:198-224`, shared `EmitBadgeSwitch` `:251-296` (mapped arm → `FcStatusBadge`
  `Slot` `:268-274`; unannotated arm → humanized text `:279-287`; default arm → localized
  `StatusBadgeUnknownStateFallback` `:291-295`; `ColumnHeader` always supplied for aria-label context).

> ⚠️ **Line numbers are guidance, not contracts.** They reflect `8036c3c`; confirm by symbol/marker
> before relying on any single line. Cite the symbol, not the line, in new pins.

### Test anchors (where pins live / go)

- **Filter state (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/FilterReducerTests.cs`
  (`ColumnFilterChanged_*`, `StatusFilterToggled_*`, `GlobalSearchChanged_*`, `SortChanged_*`,
  `FiltersReset_DispatchesClearGridStateAction`). Sibling slice tests:
  `DataGridNavigationReducerTests.cs`, `DataGridNavigationEffectsTests.cs`,
  `DataGridNavigationEffectsScopeTests.cs`, `LoadPage*Tests.cs`, `GridViewPersistenceBlobSchemaLockedTests.cs`.
- **Filter UI pins (NEW — AC1 gap)** — add under `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/`:
  `FcColumnFilterCellTests.cs`, `FcFilterSummaryTests.cs`, `FcFilterResetButtonTests.cs` (optional
  `FcStatusFilterChipsTests.cs`). Use a bUnit + Fluxor host base (see the existing
  `FcExpandInRowDetailTests`, `FcMaxItemsCapNoticeTests`, `FcSlowQueryNoticeTests`,
  `FcDataGridInputPersistenceTests` in that same folder for the established harness), `JSInterop.Mode = Loose`,
  `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) for the debounce, NSubstitute for
  `IFrontComposerRegistry`/`IUlidFactory`, and a recording dispatcher (`State/DataGridNavigation/RecordingDispatcher.cs`)
  to assert dispatched actions. Pin `CultureInfo.CurrentUICulture = "en"` for resource-key stability.
- **Skeleton / empty (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionLoadingSkeletonTests.cs`,
  `…/FcProjectionEmptyPlaceholderTests.cs`.
- **Filter-empty pin (NEW — AC2 gap)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFilterEmptyStateTests.cs`.
- **Badges (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusBadgeTests.cs`,
  `…/FcDesaturatedBadgeTests.cs`, `…/SlotAppearanceTableTests.cs`, `…/SlotContrastTests.cs`.
- **Badge emission (confirm)** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`.
- **End-to-end badge render (assess, AC3)** — `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
  and the Story 2.1 pin `RoleSpecificProjectionApprovalTests.ActionQueueProjection_RendersBadgeColumn_ThroughFcStatusBadge_WithAccessibleColumnHeader`.

### Project-context guardrails that apply here (non-negotiable)

- **Fluxor single-writer discipline (ADR-007):** one dispatch source per action type; **effects own
  persistence + JS interop**, reducers stay pure. This story is verify/pin — **no new
  `IStorageService.SetAsync` call site** is expected; adding one trips the **NFR17 tripwire** (requires
  updating the tripwire whitelist + the story compliance matrix).
- **Scoped-lifetime discipline (ADR-030):** storage/effects/auth/tenant accessors are scoped — never
  captured in singletons.
- **Generator rules (AC3 emission):** **never hand-edit generated code** (`obj/**/generated/HexalithFrontComposer/`)
  — fix the generator or the annotated type. **No `ISymbol` escapes the parse stage**; keep IR pure &
  `EquatableArray`-based. **Diagnostics travel as `DiagnosticInfo`**, converted to Roslyn `Diagnostic`
  only inside `RegisterSourceOutput`. `SourceTools` references **only** `Contracts` (netstandard2.0-clean).
- **Icons:** use the custom inline-SVG `FcFluentIcons` factory, **not** a FluentUI icons NuGet.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix,
  **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE),
  `ArgumentNullException.ThrowIfNull` at public boundaries, **no copyright/license headers** (this repo
  has none), **CRLF**, 4-space indent, final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for
  components; **Verify.XunitV3** (NOT `Verify.Xunit`) for any snapshot; plural `{Class}Tests.cs`;
  three-part `Subject_Scenario_Expectation` method names; **solution-level** `dotnet test` + trait
  filters (not per-project); run with **`DiffEngine_Disabled=true`** (else Verify hangs); `.verified.txt`
  updated **intentionally** and committed; **`FakeTimeProvider`** for the debounce (never `Thread.Sleep`/
  wall-clock waits — use `WaitForAssertion` for async render settling).
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
| Projection Loading/Empty/Data dispatch | Story 2.1 | ✅ pinned (`CounterStoryVerificationTests`, `FcProjectionLoadingSkeletonTests`, `FcProjectionEmptyPlaceholderTests`). This story confirms the grid-state UX *around* that dispatch; do not re-pin 2.1's emitter switch. |
| Badge `aria-label` (FC-A11Y in-scope pin) | Story 2.1 / 1.3 | ✅ pinned — `FcStatusBadgeTests` + the 2.1 `ActionQueueProjection_RendersBadgeColumn_…` emit pin. Layer-3 e2e axe is **CI-only** (Playwright unsupported on this host + Node <24) → no e2e work here. |
| Registry-driven nav/home around the grid | Story 2.2 | ✅ done; not on this story's path. |
| FC-LYT FullWidth for projection pages | Story 1.2 | ✅ FullWidth default (right for DataGrid-dense pages) → zero action. |
| FC-TBL table/column/filter API confirmation | Story 2.8 | ⏳ **Story 2.8 owns the formal FC-TBL confirm-stable.** This story *renders/filters into* the grid; it does **not** confirm the FC-TBL public surface or touch `PublicAPI.Shipped.txt`. There is **no FC-TBL contract doc** in `_bmad-output/contracts/` yet — that is 2.8's deliverable, **not** this story's. |

> **Scope boundary:** this story is "**DataGrid filtering + status badges + empty/loading states**". It
> is *not* expand-in-row a11y (2.4 — `FcExpandInRowDetail`/`FcExpandedRowHiddenBanner` already have
> tests; leave them), column prioritization (2.5 — `FcColumnPrioritizer`), live updates / reconnect
> (2.6 — `FcNewItemIndicator`/`FcSlowQueryNotice`/`FcMaxItemsCapNotice` belong to live-update UX),
> command palette / global search surface (2.7 — though `FcProjectionGlobalSearch` exists, the palette
> is 2.7's), or FC-TBL confirmation (2.8). Stay inside AC1–AC3.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The DataGrid filtering / status / empty-loading surface
(FR11, UX-DR2/DR5, NFR6) is shipped and partially tested at `8036c3c`. **Done = each of AC1/AC2/AC3 is
proven true against `src/` at this commit and carries a durable regression pin** — specifically the
under-pinned filter-UI components (AC1: `FcColumnFilterCell` debounce, `FcFilterSummary`,
`FcFilterResetButton`) and the filter-induced-empty state (AC2: `FcFilterEmptyState`) are closed with
new bUnit pins, the badge + skeleton + empty-placeholder surfaces (AC3 + AC2 no-data) are re-confirmed
green, the Release build is 0/0 under TWAE, the default test lane is green, the standing 13-failure
baseline is re-proved pre-existing, and the File List + counts are accurate. Genuine `src/` change is
the exception, not the expectation — and if made, it lands in the component (never generated output)
with intentional snapshot updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.3] — story statement, ACs (FR11, UX-DR5, UX-DR2, NFR6)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage; FR11 (DataGrid surface) → Epic 2
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements] — UX-DR2 (semantic badge slots), UX-DR5 (status & empty/loading UX)
- [Source: _bmad-output/project-docs/component-inventory.md] — `Fc*` grid/badge/rendering component entries
- [Source: _bmad-output/project-context.md] — Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), NFR17 tripwire, generator rules, `FcFluentIcons`, TWAE, test discipline, `DiffEngine_Disabled=true`
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] — FC-A11Y ready-gate (aria-label/role pins Layer-1; e2e Layer-3 CI-only)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] — FullWidth default for DataGrid-dense projection pages
- [Source: _bmad-output/implementation-artifacts/2-1-render-a-projection-from-a-projection-type.md] — prior confirm-and-pin pattern; badge/skeleton/empty pins; 13-failure baseline; retro AI-1/AI-2 taxes
- [Source: _bmad-output/implementation-artifacts/2-2-registry-driven-navigation-and-home-directory.md] — prior confirm-and-pin pattern; Shell.Tests baseline (8 failed / 1701 passed / 1709 total)
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md#3,#6] — File-List/sentinel taxes (AI-1/AI-2), 13-failure baseline, Epic-2 dependency states
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/FilterActions.cs] — `ColumnFilterChangedAction` + reserved-key guard + `FiltersResetAction` (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/FilterEffects.cs] — filter effects/reducer (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor.cs] — 300 ms `TimeProvider` debounce (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterSummary.razor] — filter summary `role="status"` (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterResetButton.razor.cs] — reset → `FiltersResetAction`, disabled-when-empty (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionLoadingSkeleton.razor] — `role="status"` + `aria-busy`; `SkeletonLayout` variants (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor] — no-data placeholder + auth-gated CTA (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterEmptyState.razor] — filter-induced-empty `role="status"` + `aria-live` (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor.cs] — mandatory aria-label from `ColumnHeader` + `Label` (AC3)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs] — `[ProjectionBadge]` → badge-switch emission (AC3)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/FilterReducerTests.cs] — filter state pins (confirm; AC1)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionLoadingSkeletonTests.cs] — skeleton pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionEmptyPlaceholderTests.cs] — empty-placeholder pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusBadgeTests.cs] — badge aria-label pins (confirm; AC3)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs] — badge-emission pins (confirm; AC3)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Story activation resolved via `.claude/skills/bmad-dev-story/SKILL.md`; no prepend/append steps; persistent project context loaded from `_bmad-output/project-context.md` plus submodule context files.
- Confirmed existing state/reducer pins: `FilterReducerTests` and `FilterActions.ColumnKey` reserved `__` guard are unchanged.
- Confirmed existing AC2/AC3 pins: `FcProjectionLoadingSkeletonTests`, `FcProjectionEmptyPlaceholderTests`, `FcStatusBadgeTests`, `FcDesaturatedBadgeTests`, `SlotAppearanceTableTests`, `SlotContrastTests`, and `RoleSpecificProjectionApprovalTests`.
- Added and ran new story pins with xUnit v3 in-process runner: `Shell.Tests` focused story classes → **20 passed / 0 failed**.
- Added and ran focused SourceTools badge-emitter pins: `RazorEmitterBadgeColumnTests` → **6 passed / 0 failed**.
- Existing confirmation classes: Shell state/render/badge classes → **75 passed / 0 failed**; SourceTools role-specific approvals → **16 passed / 0 failed** after intentional snapshot updates.
- Release build: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 -p:BuildInParallel=false -p:RestoreDisableParallel=true` → **0 Warning(s) / 0 Error(s)** before the sandboxed NuGet audit path began failing.
- Official `dotnet test` lane could not execute in this sandbox: VSTest fails before test execution with `System.Net.Sockets.SocketException (13): Permission denied` while opening its communication socket. Used xUnit v3 in-process executables as the no-socket fallback.
- Full in-process default-lane evidence: `Contracts.Tests` **159/159 passed**, `Mcp.Tests` **291/291 passed**, `SourceTools.Tests` **950 passed / 3 failed** (known pre-existing: IDE parity case sensitivity, deferred-work ledger missing, command form `_model` audit), `Cli.Tests` **38 passed / 3 failed** (two quoted-solution selection baseline failures + NuGet network restore in packaging smoke), `Testing.Tests` **9 passed / 2 failed** (pack/restore path blocked), `Shell.Tests` story pins **20/20 passed** and broader Shell lane reproduced the known environmental cluster; two Verify diffs are in-process runner formatting/culture artifacts and were not accepted.
- Sentinel scan of new/modified source/test files found no authoring tags. The only match is the story's own instruction text naming forbidden tags.

### Completion Notes List

- AC1 filter UI gaps closed with dedicated bUnit pins for `FcColumnFilterCell`, `FcFilterSummary`, `FcFilterResetButton`, and optional `FcStatusFilterChips`; no DataGridNavigation source changes and no new storage write sites.
- AC2 filter-induced-empty gap closed with dedicated `FcFilterEmptyStateTests`; loading skeleton and no-data placeholder remained unchanged and already pinned.
- AC3 rendered-grid badge gap exposed a genuine generator bug: `[ProjectionBadge]` enum columns emitted a `PropertyColumn` with `ChildContent`, but FluentUI `PropertyColumn` has no `ChildContent` parameter. Fixed `ColumnEmitter` to emit badge enum grid columns as `TemplateColumn<T>` while preserving title, badge switch arms, fallback labels, column-header aria context, and sort attributes.
- Added generated-grid runtime pin proving `FcStatusBadge` receives `ColumnHeader` and renders `aria-label="Status: Pending"` / `aria-label="Status: Approved"` end-to-end.
- Updated affected SourceTools snapshots intentionally because generated badge columns now use the renderable `TemplateColumn<T>` path.
- Added `CultureScope` helper so new English resource assertions do not leak culture into other Shell tests under the xUnit in-process runner.
- No component restyling, no generated output hand-editing, no new dependencies, and no `IStorageService.SetAsync` call sites.

### File List

- `_bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/CultureScope.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnFilterCellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFilterEmptyStateTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFilterResetButtonTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcFilterSummaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcStatusFilterChipsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionRenderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionSpecimen.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`

### Change Log

- 2026-06-04 — Implemented Story 2.3 confirm-and-pin coverage for DataGrid filtering, empty/loading/status surfaces; fixed generated badge columns to use `TemplateColumn<T>` for renderable `FcStatusBadge` child content; updated story and sprint status to review.
- 2026-06-04 — Senior Developer Review (AI): AC1/AC2/AC3 re-proved true against `src/`; the one `src/` change (badge `TemplateColumn` fix) independently verified as a genuine runtime bug fix; Release build 0/0; 13-failure baseline (8 Shell + 3 SourceTools + 2 Cli) re-proved pre-existing; new pins green; 0 critical/high — status → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Outcome:** ✅ Approve (status → done)

**Scope reviewed:** the full git working tree vs story File List, AC1–AC3 against `src/` at `8036c3c`, the one `src/` change (`ColumnEmitter.cs`), all eleven new/modified test + snapshot files, the Release build, and the default test lane. `_bmad-output/` artifacts excluded per review policy.

### Verification performed (independent, not taken on trust)

- **Release build** — `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 Warning(s) / 0 Error(s)** under TWAE. ✔
- **Test lane** — the dev recorded that the official `dotnet test` could not run in their sandbox (socket `Permission denied`). It **runs cleanly here**, so the lane was re-executed for real:
  - New story pins: Shell.Tests filter-UI + badge-render classes **24/24 passed**; SourceTools badge/role/snapshot classes **35/35 passed**.
  - Full **Shell.Tests** default lane: **8 failed / 1725 passed / 1733 total** → exactly the 2.2 baseline (8 failed / 1701 / 1709) **+24 new passing pins**, same 8 pre-existing failures, **none new**. ✔
  - Full **SourceTools.Tests** default lane: **3 failed / 950 passed** → the named pre-existing cluster (IDE-parity case-sensitivity, deferred-work ledger, command-form `_model` audit); the `ColumnEmitter` change introduced **no** new failures. ✔
- **File List vs git reality** — accurate for all source/test files. The two git changes not listed (`tests/2-3-test-summary.md`, `orchestration-*.md`) are both under `_bmad-output/` and out of review scope. ✔
- **Sentinel scan (retro AI-2)** — new/modified source & test files: **clean**, no stray tool-call tags. ✔

### AC validation

- **AC1 (debounced filter + summary + reset)** — IMPLEMENTED & now pinned. `FcColumnFilterCellTests` proves the 300 ms `FakeTimeProvider` debounce (sub-threshold = no dispatch, past-threshold = exactly one `ColumnFilterChangedAction`, clear → null value, rapid-typing coalesces, dispose cancels, snapshot hydration). `FcFilterSummaryTests` pins `role="status"`, per-filter clauses, reserved-key exclusion. `FcFilterResetButtonTests` pins `FiltersResetAction` dispatch, disabled-when-empty, aria-label count.
- **AC2 (loading / empty / filter-empty)** — IMPLEMENTED & pinned. `FcFilterEmptyStateTests` pins the distinct `role="status"` + `aria-live="polite"` live region and reset affordance; skeleton/placeholder pins re-confirmed green.
- **AC3 (`[ProjectionBadge]` → `FcStatusBadge` with mandatory aria-label)** — IMPLEMENTED **after a genuine fix** (see below); new end-to-end `BadgeProjectionRenderTests` proves `aria-label="Status: Pending"` / `"Status: Approved"` flow through the *rendered generated grid column*.

### Key finding — resolved by the dev, independently confirmed

🔴→✅ **The badge grid column was actually broken at runtime at baseline, not "exists & pinned".** `ColumnEmitter` emitted a FluentUI `PropertyColumn<T, string?>` with a `ChildContent` attribute — but `PropertyColumn` has **no** `ChildContent` parameter (confirmed against the FluentUI component API: only `TemplateColumn` exposes `ChildContent`), so rendering any `[ProjectionBadge]` column threw `does not have a property matching the name 'ChildContent'`. The pre-existing `RazorEmitterBadgeColumnTests` gave **false confidence** by asserting only the *generated source text*, never a render. The dev correctly switched the badge arm to `TemplateColumn<T>`, dropped the now-unused `Property` lambda, and updated three `.verified.txt` snapshots intentionally. Sort is preserved (the badge column's `SortBy = GridSort<T>.ByDescending(...)` is emitted independently of `Property`, only on the default sort column); app filtering is Fluxor/`FcColumnFilterCell`-driven and never relied on the column `Property`. The fix is correct and the new render pin guarantees it cannot silently regress again.

### Minor (LOW — no action required)

- `FcColumnFilterCellTests` drives the debounce via private reflection (`OnValueChangedAsync`, `_value`). Justified for deterministic `FakeTimeProvider` timing, but couples the pin to private member names. Acceptable; left as-is per the story's "don't churn working pins" guard.

_No CRITICAL or HIGH issues outstanding. Issues fixed during review: 0 (the sole defect was already fixed by the dev; review independently verified it). Action items created: 0._
