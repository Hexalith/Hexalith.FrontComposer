---
baseline_commit: d4175017bd0dc8dc5acf40836861f680cf86d6eb
---

# Story 2.5: Column prioritization for wide projections

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Stories 2.1–2.4, the **entire column-prioritization surface already exists, builds, renders, and
> is partially tested** at baseline `d417501`. The shipped code carries `Story 4-4` docstring markers
> (UX-DR63 / decisions D6/D15/D17/D22) because the brownfield code was authored ahead of this epic's
> confirm-and-pin numbering — **that is expected; do not "re-attribute" or churn it.**
>
> What is already built and pinned:
> - **`[ColumnPriority(int)]` attribute** — `Contracts/Attributes/ColumnPriorityAttribute.cs`
>   (`AttributeTargets.Property`, `Inherited=false`, `AllowMultiple=false`); pinned by
>   `ColumnPriorityAttributeTests` + `Level1FormatAttributeTests`.
> - **HFC1029 (Info) — ">15 columns → `FcColumnPrioritizer` activates"** — emitted in
>   `RazorModelTransform.cs` (transform stage, **strict `> 15`** per D6; exactly 15 does NOT trigger),
>   pinned by `Hfc1029DiagnosticTests` (20-col fires, 15-col boundary, per-projection dedupe).
> - **HFC1028 (Info) — "[ColumnPriority] collision"** — emitted in `AttributeParser.cs`
>   (`EmitColumnPriorityCollisionDiagnostic`, parse stage, once per colliding priority value per
>   projection, declaration-order tiebreaker), pinned by `Hfc1028DiagnosticTests`.
> - **Emitter wrap** — `RazorEmitter.cs` wraps the `FluentDataGrid` in `<FcColumnPrioritizer>` and emits
>   `_allColumnsDescriptor` / `_defaultHiddenColumns` only when `Columns.Count > 15`; pinned by
>   `RazorEmitterColumnPrioritizerTests` (15-col no-wrap baseline, 16-col wraps, all-keys present).
> - **`FcColumnPrioritizer` component** — gear + popover, `ColumnVisibilityChangedAction` /
>   `ResetColumnVisibilityAction`, `MaxVisibleColumns` default **10**, aria-label/`role="dialog"`; pinned
>   by `FcColumnPrioritizerTests` + the Story 1.0 spike's `MaxVisibleColumns_DefaultIsTen`.
>
> **So this story's job is to (1) VERIFY both ACs hold end-to-end against `src/` at this commit, (2) CLOSE
> the one genuine durability gap, and (3) make the verification durable** so the rest of Epic 2 (live
> updates 2.6, palette 2.7, FC-TBL confirm 2.8) builds on a pinned column-prioritization baseline.
> **Default to ZERO `src/` change.**
>
> **The genuine gap is AC2's "columns order by priority".** Every *isolated* layer is pinned, but the
> **actual ordering behaviour** — the transform stable-sort `(Priority ?? int.MaxValue, declarationOrder)`
> in `RazorModelTransform.cs` — is **NOT directly pinned**: `RazorModelTransformTests` has no priority
> test, and `RazorEmitterColumnPrioritizerTests.DescriptorListContainsAllColumnKeys` asserts every key is
> **present**, *not* that the descriptor list is **in priority order**. So a regression that broke the
> sort (e.g. dropped the index-stable tiebreaker, or stopped gating on `anyPriority`) would pass every
> current test. That is exactly the "source-only assertion gives false confidence" lesson from Story 2.3.
> If you find a real `src/` gap, fix the **generator** (never hand-edit generated output) and update any
> affected `.verified.txt` snapshot **intentionally**. If behaviour is already correct, **pin it — do not
> "improve" or restyle** working output (Epic-1 retro §5 flags "copy-a-pattern-without-the-difference" as
> the rising Epic-2 risk).

## Story

As an operator,
I want wide projections to prioritize the most important columns,
so that >15-column grids stay usable without horizontal overload.

## Acceptance Criteria

**AC1 — A projection with more than 15 columns activates `FcColumnPrioritizer`, and HFC1029 is reported as info at build. *(FR11, FR6)***
**Given** a `[Projection]` whose generated grid has **more than 15** columns (strict `> 15` per D6 — exactly 15 does not trigger),
**When** the project builds,
**Then** the transform stage emits **HFC1029** (`DiagnosticSeverity.Info`) — `"Projection {0} has {1} columns (>15); FcColumnPrioritizer activates — {2} columns hidden by default: [{3}]"` — once per projection (deduped via the incremental per-input model), naming the columns hidden by default (indices 10..Count-1),
**And** the generated grid is wrapped in `<FcColumnPrioritizer>` carrying `ViewKey`, `AllColumns` (`_allColumnsDescriptor`), `HiddenColumns`, and `MaxVisibleColumns="10"`,
**And** at runtime the prioritizer renders its gear affordance (`aria-label` reflecting the hidden-column count, `role="dialog"` popover) with the first **10** columns visible and the remainder hidden by default (UX-DR7).

**AC2 — `[ColumnPriority(n)]` orders columns by priority; a priority collision reports HFC1028 (info). *(FR11, FR6)***
**Given** a projection whose properties carry `[ColumnPriority(n)]` annotations,
**When** the project builds and the grid renders,
**Then** columns are ordered by **`(Priority ?? int.MaxValue, declarationOrder)`** — annotated columns sort ascending by priority, unannotated columns fall to the back in declaration order, and the sort is **stable** (equal priorities keep declaration order via the index tiebreaker),
**And** the sort is a **NO-OP when no column declares a priority** (declaration-order baseline preserved byte-for-byte — the `CounterProjectionApprovalTests` gate),
**And** when two or more properties share the **same explicit** priority value, **HFC1028** (`DiagnosticSeverity.Info`) — `"[ColumnPriority] collision on {0} — properties [{1}] share priority {2}. Deterministic tiebreaker is declaration order"` — fires once per colliding priority value per projection (unannotated columns sharing the `int.MaxValue` sentinel do **not** collide).

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before
> writing anything. Most subtasks should resolve to "already true → confirm the pin"; only open a `src/`
> change if a genuine AC gap is proven. Record what you found (true/false + the evidence) in the Dev Agent
> Record so the review can audit it. **The expected `src/` delta for this story is ZERO.**

- [x] **Task 1 — Verify AC1: >15-column threshold → HFC1029 + `FcColumnPrioritizer` wrap (AC: #1)**
  - [x] **Diagnostic layer — confirm ALREADY PINNED, no change.** Re-confirm `Hfc1029DiagnosticTests`:
    `FiresOnce_WhenProjectionHasMoreThan15Columns_PayloadNamesHiddenDefaults` (20-col, payload lists
    hidden defaults), `DoesNotFire_AtExactly15Columns_StrictInequalityBoundary` (the strict `> 15`
    boundary, D6), `DedupesPerProjection_OneDiagnosticRegardlessOfColumnCount` (per-projection dedupe,
    D22). Verify the emission site is `RazorModelTransform.cs` transform stage (`if (columns.Count > 15)`)
    and severity is `Info`.
  - [x] **Emitter layer — confirm ALREADY PINNED, no change.** Re-confirm `RazorEmitterColumnPrioritizerTests`:
    `NoWrap_AtFifteenColumns_PreservesBaseline` (15-col → no wrapper), `Wraps_WhenSixteenColumns_EmitsPrioritizerAndDescriptor`
    (16-col → `FcColumnPrioritizer` + `_allColumnsDescriptor` + `_defaultHiddenColumns` + `MaxVisibleColumns`),
    `DescriptorListContainsAllColumnKeys` (20-col → all keys present + `_defaultHiddenColumns` set).
  - [x] **Component layer — confirm ALREADY PINNED, no change.** Re-confirm `FcColumnPrioritizerTests`:
    `Gear_RendersTopRightWithHiddenCountAriaLabel`, `Gear_SwapsAriaLabel_WhenNoColumnsHidden`,
    `ClickingGear_OpensPopover_WithCheckboxForEachColumn`, `Popover_CarriesDialogRole_AndLabelledByHeader`,
    `RootDiv_CarriesPrioritizerClass_AndViewKeyAttribute`, plus the spike pin
    `FcColumnPrioritizer_MaxVisibleColumns_DefaultIsTen` (UX-DR7 default-10).
  - [x] **ASSESS the end-to-end gap.** The diagnostic/emitter pins prove *generated source text + a build
    diagnostic*; `FcColumnPrioritizerTests` proves the *component in isolation with hand-built columns*.
    There is **no test that renders an actual generated wide (>15-col) grid and asserts the prioritizer
    wraps it**. This is a lower-priority gap than AC2 (the boundary + wrap source are tightly pinned), but
    if you add the AC2 end-to-end render pin (Task 2) for a >15-col specimen, fold the AC1 wrap assertion
    (prioritizer present, gear rendered, default-10 visible) into the **same** render so one specimen
    covers both ACs. **Only add `src/` change if a genuine gap is proven** — expect a pin-only outcome.

- [x] **Task 2 — Verify AC2: priority ordering + HFC1028 collision (AC: #2) — THIS IS THE REAL WORK**
  - [x] **Collision diagnostic — confirm ALREADY PINNED, no change.** Re-confirm `Hfc1028DiagnosticTests`:
    `FiresOnce_WhenTwoColumnsSharePriority_PayloadIncludesPropertyNamesAndPriority`,
    `FiresOncePerCollidingPriorityValue_WhenMultipleDistinctPrioritiesCollide`,
    `DoesNotFire_WhenPrioritiesAreDistinctOrUnannotated` (the `int.MaxValue`-sentinel non-collision).
    Verify the emission site is `AttributeParser.EmitColumnPriorityCollisionDiagnostic` (parse stage),
    severity `Info`.
  - [x] **Parse → IR — confirm ALREADY PINNED, no change.** `AttributeParser.ParseColumnPriority` reads the
    first ctor arg into `PropertyModel.ColumnPriority` (`int?`, null when absent); carried through the
    equatable IR (`DomainModel.cs` / `ColumnModel.Priority`). Confirm the attribute pins
    (`ColumnPriorityAttributeTests` — any signed int accepted, property-only, non-repeatable).
  - [x] **CLOSE THE ORDERING GAP (the genuine AC2 deliverable).** The stable sort in
    `RazorModelTransform.cs` — `Array.Sort` over an index array with comparator
    `(buffer[l].Priority ?? int.MaxValue).CompareTo(buffer[r].Priority ?? int.MaxValue)`, tiebreak on the
    original index — is what makes "columns order by priority" true, and it is **gated on `anyPriority`**
    (NO-OP when no column has a priority). **No existing test asserts the resulting column ORDER.**
    `RazorModelTransformTests` has no priority test; `RazorEmitterColumnPrioritizerTests.DescriptorListContainsAllColumnKeys`
    asserts every key is **present**, not **ordered**. Add a focused pin (prefer the **transform level** so
    it tests the sort directly, in `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs`
    or a new sibling test class) that asserts the **resulting `ColumnModel` order**, covering:
    1. **mixed priorities + unannotated** → annotated ascend by priority, unannotated trail in declaration
       order (e.g. declare `[ColumnPriority(2)] B`, `A` (none), `[ColumnPriority(1)] C` → order `C, B, A`);
    2. **NO-OP baseline** → all-unannotated columns keep **declaration order** byte-for-byte (this is the
       `CounterProjectionApprovalTests` invariant — assert it explicitly so a regression that always-sorts
       is caught);
    3. **stability on collision** → two columns sharing priority `n` keep declaration order (the index
       tiebreaker), and HFC1028 fires (cross-check with the parse-stage collision pin);
    4. *(optional)* `int.MinValue` "pin-to-front" / `int.MaxValue` edge values sort sanely.
    If a transform-level harness is awkward, an **emitter-level** order pin (assert the `_allColumnsDescriptor`
    list emits keys in priority order, not just contains them) is an acceptable alternative — but the
    transform-level test is preferred because it isolates the sort from the >15-col wrap. **Expect this to
    be pin-only** (the sort is correct at `d417501`); if rendering/asserting reveals a real ordering bug,
    fix `RazorModelTransform.cs` and update affected snapshots **intentionally**.
  - [x] **ASSESS an end-to-end generated-grid render pin (recommended, mirrors 2.3/2.4 precedent).** To
    prove AC2 the way Story 2.3 (`BadgeProjectionRenderTests`) and Story 2.4 (`ExpandInRowGeneratedGridTests`)
    proved their ACs — by *rendering a generated grid*, not just asserting emitted text — add ONE wide
    (>15-col) **specimen projection** with `[ColumnPriority]` annotations under
    `tests/Hexalith.FrontComposer.Shell.Tests/Generated/` (follow the `BadgeProjectionSpecimen` /
    `StatusProjection` precedent) and a render pin reusing `GeneratedComponentTestBase` that asserts:
    (a) the generated grid is wrapped by `FcColumnPrioritizer` (AC1), (b) the rendered/declared column
    order reflects priority (AC2). **Only if it can be done without churning the existing specimen set** —
    if the bUnit/FluentUI-v5 render makes column-order assertion brittle, the transform-level order pin
    (above) is the load-bearing AC2 pin and this render pin is the AC1-wrap bonus. Pin `CultureInfo` via
    the existing `CultureScope` helper for any localized assertion (as Story 2.4 hardened).
  - [x] **Do not restyle / do not re-pin solved layers.** The attribute shape, HFC1028/HFC1029 IDs,
    severities, message formats, `MaxVisibleColumns=10`, gear/popover ARIA, and the descriptor field names
    are the shipped contract — pin them, don't "improve" them. Don't add a second sort, don't change the
    `int.MaxValue` sentinel, don't touch `CanonicalSchemaMaterial`.

- [x] **Task 3 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE
    (use `-m:1 /nr:false` if node-reuse causes flakiness, per Story 2.4's recorded constraint).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` —
    everything this story touches is green; new pins pass. **Note (host constraint, inherited from Stories
    2.3/2.4):** solution-level VSTest opens a local socket and fails with `SocketException (13): Permission
    denied` in this sandbox — if so, fall back to the **xUnit v3 in-process runner** emitted per test
    assembly for local evidence, and record that the VSTest run is the CI gate. New AC2-ordering pins land
    in **`SourceTools.Tests`** (transform); any end-to-end render pin lands in **`Shell.Tests`**.
  - [x] **Re-prove the standing failure baseline.** Story 2.4's review recorded the Shell.Tests default
    lane at **8 failed** (documented pre-existing/environmental clusters: Verify snapshot drift, missing
    `deferred-work.md` file IO, navigation-hydration, telemetry timing, query-fallback; plus the
    SourceTools/Cli clusters). Capture the before→after counts for the assemblies you touch and confirm
    the **same pre-existing failures** remain (none new, none misattributed). If your pins land **only** in
    `SourceTools.Tests`, the Shell baseline is untouched — say so explicitly.
  - [x] **`.verified.txt` discipline.** Confirm-and-pin → default **ZERO** snapshot edits. The AC2 ordering
    pins are assertion-based (not Verify snapshots), so no `.verified.txt` change is expected. If a genuine
    `src/` fix lands and changes generated markup, update
    `CounterStoryVerificationTests.*RenderSnapshot.verified.txt` and any emitter snapshot **intentionally**
    — and confirm the all-unannotated `CounterProjectionApprovalTests` baseline is byte-for-byte unchanged.

- [x] **Task 4 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in
    the Dev Agent Record, reconciled against the actual git tree (this is the recurring Epic-1/2 review
    finding — pay it up front; include any QA test-summary artifact).
  - [x] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no stray
    `</content>` / `</invoke>` / `<invoke` / tool-call tags.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`d417501`) | This story |
|---|---|---|
| `[ColumnPriority(int)]` attribute (property-only, non-repeatable, any signed int) | **Exists & pinned** — `Contracts/Attributes/ColumnPriorityAttribute.cs`, `ColumnPriorityAttributeTests` | Confirm; no change |
| Parse → `PropertyModel.ColumnPriority` (`int?`) | **Exists & pinned (indirect)** — `AttributeParser.ParseColumnPriority`, equatable IR | Confirm; no change |
| **HFC1029 (Info) — >15 cols → prioritizer activates** (strict `> 15`, per-projection dedupe) | **Exists & pinned** — `RazorModelTransform.cs`, `Hfc1029DiagnosticTests` | Confirm; no change |
| **HFC1028 (Info) — [ColumnPriority] collision** (per colliding value, decl-order tiebreak) | **Exists & pinned** — `AttributeParser.EmitColumnPriorityCollisionDiagnostic`, `Hfc1028DiagnosticTests` | Confirm; no change |
| Emitter wrap → `<FcColumnPrioritizer>` + `_allColumnsDescriptor`/`_defaultHiddenColumns` (>15) | **Exists & pinned (source)** — `RazorEmitter.cs`, `RazorEmitterColumnPrioritizerTests` | Confirm; no change |
| `FcColumnPrioritizer` component (gear + popover, dispatch, ARIA, default-10) | **Exists & pinned** — `FcColumnPrioritizer.razor(.cs)`, `FcColumnPrioritizerTests`, spike `MaxVisibleColumns_DefaultIsTen` | Confirm; no change |
| **Column ORDER = `(Priority ?? int.MaxValue, declarationOrder)` stable sort** (gated on `anyPriority`; NO-OP baseline) | **Exists; emitter "all-keys-present" pin only — ORDER UNPINNED** | **CLOSE THE GAP** — add a transform-level order pin (AC2) |
| End-to-end render of a generated wide grid (>15 cols) with priorities | **Not covered** — component tested in isolation only | **Assess** an end-to-end render pin (AC1 wrap + AC2 order), 2.3/2.4 precedent |

> **Key judgment for the dev agent:** the deliverable is **a durable pin on the priority-ORDER behaviour
> (AC2)** — every other layer is already locked. If you find yourself editing
> `ColumnPriorityAttribute.cs`, `RazorModelTransform.cs`, `AttributeParser.cs`, `RazorEmitter.cs`, or
> `FcColumnPrioritizer.razor`, **stop** — re-read the AC and confirm you've found a *genuine* gap, not a
> style preference. Story 2.3's lesson: a source-only / presence-only assertion can give **false
> confidence**; prove AC2 by asserting the **resulting column order**, which is exactly where the residual
> gap is.

### Exact anchors (read these before touching anything)

> ⚠️ **Line numbers are guidance, not contracts.** They reflect `d417501`; confirm by symbol/marker before
> relying on any single line. Cite the symbol, not the line, in new pins.

**AC1 — >15-column threshold + prioritizer wrap**
- **Diagnostic (transform stage)** — `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs`
  (`if (columns.Count > 15)` → builds `hiddenByDefault` from indices `10..Count-1`, emits HFC1029 `Info`
  via `CreateTransformDiagnostic`). **Strict `> 15`** (D6) — exactly 15 does not trigger.
- **Emitter wrap** — `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` (the
  `model.Columns.Count > 15` branches: `_allColumnsDescriptor` / `_defaultHiddenColumns` static fields,
  `ResolveHiddenColumns(...)`, `<FcColumnPrioritizer ViewKey/AllColumns/HiddenColumns/MaxVisibleColumns="10">`
  open/close). **Never hand-edit generated output** — change the emitter.
- **Component** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor(.cs)`
  (`ViewKey`/`AllColumns`/`HiddenColumns`/`MaxVisibleColumns` default **10**/`ChildContent` params; gear
  `Settings20` icon + `FluentPopover` `role="dialog"`; `aria-label` swaps on hidden count; dispatches
  `ColumnVisibilityChangedAction` / `ResetColumnVisibilityAction`).
- **Diagnostic ID** — `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
  (`HFC1029_ColumnPrioritizerActivated`); descriptor in
  `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` (`DiagnosticSeverity.Info`,
  help link `…/diagnostics/HFC1029`); doc `docs/diagnostics/HFC1029.md`.

**AC2 — priority ordering + collision**
- **Attribute** — `src/Hexalith.FrontComposer.Contracts/Attributes/ColumnPriorityAttribute.cs`
  (`ColumnPriorityAttribute(int priority)`, `int Priority { get; }`; `AttributeTargets.Property`,
  `Inherited=false`, `AllowMultiple=false`).
- **Parse** — `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`:
  `ParseColumnPriority(IPropertySymbol)` → `int?`; `EmitColumnPriorityCollisionDiagnostic(...)` (fires
  HFC1028 `Info` once per colliding priority value per projection; unannotated `int.MaxValue` sentinel
  does **not** collide). `PropertyModel.ColumnPriority` carried in `Parsing/DomainModel.cs`.
- **Sort (the AC2 behaviour to pin)** — `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs`:
  `bool anyPriority` gate; `Array.Sort(declarationOrder, (l,r) => { int lp = buffer[l].Priority ?? int.MaxValue; … return cmp != 0 ? cmp : l.CompareTo(r); })`
  then rebuild `columnsArray` in sorted order. **NO-OP when `!anyPriority`** (declaration-order baseline,
  `CounterProjectionApprovalTests` gate).
- **Diagnostic ID** — `FcDiagnosticIds.HFC1028_ColumnPriorityCollision`; descriptor in
  `DiagnosticDescriptors.cs` (`DiagnosticSeverity.Info`, message
  `"[ColumnPriority] collision on {0} — properties [{1}] share priority {2}. Deterministic tiebreaker is declaration order."`,
  help link `…/diagnostics/HFC1028`); doc `docs/diagnostics/HFC1028.md`.

### Test anchors (where pins live / go)

- **Attribute (confirm)** — `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/ColumnPriorityAttributeTests.cs`
  + `…/Attributes/Level1FormatAttributeTests.cs` (`ColumnPriorityAttribute_AcceptsAnySignedIntPriority`,
  `…_IsPropertyOnlyAndNotRepeatable`).
- **HFC1029 (confirm)** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1029DiagnosticTests.cs`.
- **HFC1028 (confirm)** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1028DiagnosticTests.cs`.
- **Emitter wrap (confirm)** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterColumnPrioritizerTests.cs`
  (note: `DescriptorListContainsAllColumnKeys` checks **presence, not order**).
- **Component (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs`
  + spike `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs`
  (`FcColumnPrioritizer_MaxVisibleColumns_DefaultIsTen`).
- **AC2 ORDER pin (NEW — the gap)** — add to
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs` (or a new
  sibling `RazorModelColumnPriorityOrderTests.cs`): assert the resulting `ColumnModel` order for
  mixed/NO-OP/collision cases. Use the existing transform-test scaffolding (`CompilationHelper` /
  parse-then-transform helpers already used in that file).
- **End-to-end render pin (NEW — optional AC1 wrap + AC2 order)** — add a wide (>15-col) specimen under
  `tests/Hexalith.FrontComposer.Shell.Tests/Generated/` (follow `BadgeProjectionSpecimen` / `StatusProjection`)
  + a render pin reusing `GeneratedComponentTestBase`, mirroring `BadgeProjectionRenderTests` (2.3) /
  `ExpandInRowGeneratedGridTests` (2.4).
- **Approval baseline (confirm untouched)** — the `CounterProjectionApprovalTests` /
  `CounterStoryVerificationTests.*.verified.txt` byte-for-byte gate for the all-unannotated NO-OP path.

### Project-context guardrails that apply here (non-negotiable)

- **Generator rules (this story's core):** **never hand-edit generated code**
  (`obj/**/generated/HexalithFrontComposer/`) — fix the transform/emitter or the annotated type. **No
  `ISymbol` escapes the parse stage** — `ColumnPriority` is carried as a pure `int?` in the equatable IR
  (`EquatableArray<T>` for collections; a missing field silently breaks incremental caching). **Diagnostics
  travel as `DiagnosticInfo`**, converted to Roslyn `Diagnostic` only inside `RegisterSourceOutput` —
  HFC1028 (parse) and HFC1029 (transform) follow this. Bands: **`HFC1xxx` = build-time** (these are Info).
  `SourceTools` references **only** `Contracts` (netstandard2.0-clean) — don't pull net10 deps in.
- **Schema integrity:** do **not** touch `CanonicalSchemaMaterial` (encoder / sentinel / `StringComparer.Ordinal`
  / serialization) — it silently invalidates every fingerprint & baseline. Column reordering must not
  change the canonical schema material contract.
- **Fluxor single-writer (ADR-007) / scoped-lifetime (ADR-030):** the prioritizer's `ColumnVisibilityChangedAction`
  / `ResetColumnVisibilityAction` flow and `ColumnVisibilityPersistenceEffect` are Story-4-4 surface — this
  story does **not** reopen visibility persistence. **NFR17 tripwire:** do not add a new
  `IStorageService.SetAsync` call site in `Shell/State/` (would require updating the tripwire whitelist +
  compliance matrix) — none is expected here.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix,
  **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE); `ArgumentNullException.ThrowIfNull`
  at public boundaries; **no copyright/license headers** (this repo has none); **CRLF**, 4-space indent,
  final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for
  components (`JSInterop.Mode = Loose`); generator tests go through `CompilationHelper.CreateCompilation()`;
  Blazor component tests use `GeneratedComponentTestBase` / `AddFrontComposerTestHost`; **Verify.XunitV3**
  (NOT `Verify.Xunit`) for any snapshot, `.verified.txt` updated **intentionally**; plural `{Class}Tests.cs`;
  three-part `Subject_Scenario_Expectation` method names; **solution-level** `dotnet test` + trait filters
  (not per-project); run with **`DiffEngine_Disabled=true`** (else Verify hangs); `CultureScope` for
  culture-sensitive assertions; `FakeTimeProvider` for any timer path (none here).
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress;
  built-in analyzers only (no Sonar/StyleCop/Roslynator); centralized versions in `Directory.Packages.props`
  (never add `Version=` to a `.csproj`).
- **Commits/branches:** Conventional Commits — this work is **`test:`** shaped (verification + pins), **not
  `feat:`** (a false `feat:` triggers a minor bump + NuGet publish). **No direct commits to `main`** — cut a
  `test/story-2-5-*` (or `feat/story-2-5-*`) feature branch + PR. *(Note: the repo's recent history shows
  Stories 2.1–2.4 were committed straight to `main` by the story-automator; that contradicts the
  project-context "no direct commits to main" rule — prefer a feature branch, and if the automator pipeline
  forces `main`, record that deviation in the Change Log.)*

### Project Structure Notes

- **Alignment:** all touched surfaces sit in the established generator pipeline
  (`Contracts/Attributes` → `SourceTools/Parsing` → `SourceTools/Transforms` → `SourceTools/Emitters`) and
  the Shell DataGrid components (`Shell/Components/DataGrid`). New pins go in the matching `*.Tests`
  project mirror (`SourceTools.Tests/Transforms`, `SourceTools.Tests/Diagnostics`, `Shell.Tests/Generated`,
  `Shell.Tests/Components/DataGrid`). No new top-level folders or projects.
- **Dependency direction:** the AC2 sort lives in `SourceTools` (references only `Contracts`); the
  `FcColumnPrioritizer` component lives in `Shell` (→ Contracts). No new cross-references; do not pull
  net10/FluentUI deps into `SourceTools` or the netstandard2.0 face of `Contracts`.
- **No variances expected:** confirm-and-pin with zero `src/` change is the target; any deviation
  (a genuine `src/` fix) must be called out in the Dev Agent Record with the proven gap.

### Epic dependencies & their state

| Epic-2 needs | From | State at kickoff |
|---|---|---|
| Generated projection grid + Loading/Empty/Data dispatch (the grid the prioritizer wraps) | Story 2.1 | ✅ pinned — `CounterStoryVerificationTests`, `GeneratedComponentTestBase` |
| Registry-driven nav/home around the grid | Story 2.2 | ✅ done; not on this story's path |
| DataGrid filtering + `GridViewSnapshot.Filters` (the `__hidden` CSV the prioritizer reads/writes) | Story 2.3 | ✅ done & pinned — column-visibility persistence is Story-4-4 surface, not reopened here |
| Expand-in-row detail (coexists in the same grid) | Story 2.4 | ✅ done & pinned; the expand trigger column counts toward the grid's column total — keep that in mind if authoring a >15-col specimen |
| FC-TBL table/column API confirmation | Story 2.8 | ⏳ **2.8 owns the formal FC-TBL confirm-stable + `PublicAPI.Shipped.txt`.** This story *exercises* the column-priority surface; it does **not** confirm the public surface |

> **Scope boundary:** this story is "**column prioritization for wide projections**" (AC1 >15-col
> threshold → HFC1029 + `FcColumnPrioritizer` wrap; AC2 `[ColumnPriority]` ordering + HFC1028 collision).
> It is *not* live updates / reconnect (2.6 — `FcNewItemIndicator` / `FcProjectionConnectionStatus`),
> command palette / global search (2.7), or FC-TBL confirmation (2.8). It does **not** reopen Story-4-4's
> column-visibility persistence (`ColumnVisibilityChangedAction` / `ResetColumnVisibilityAction` /
> `ColumnVisibilityPersistenceEffect`) — you *render/observe* the prioritizer, you don't re-prove its
> persistence wiring. Stay inside AC1–AC2.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The column-prioritization surface (FR11, FR6; UX-DR7 default-10)
is shipped and partially tested at `d417501`. **Done = both AC1 and AC2 are proven true against `src/` at
this commit and carry a durable regression pin** — specifically the under-pinned **AC2 priority-ORDER
behaviour** (the transform stable-sort) is closed with a new transform-level order pin (mixed / NO-OP
baseline / collision-stability), the HFC1028/HFC1029/emitter/component/attribute layers are re-confirmed
green, optionally an end-to-end generated wide-grid render pins the AC1 wrap + AC2 order together, the
Release build is 0/0 under TWAE, the default test lane is green, the standing failure baseline is re-proved
pre-existing, and the File List + counts are accurate. Genuine `src/` change is the exception, not the
expectation — and if made, it lands in the transform/emitter (never generated output) with intentional
snapshot updates and the all-unannotated approval baseline confirmed byte-for-byte unchanged.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.5] — story statement, ACs (FR11, FR6)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage; FR11 (DataGrid surface incl. column prioritization for >15-col projections) → Epic 2
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements] — UX-DR7 (page layout / FullWidth for DataGrid-dense pages); default-10 visible (UX-DR63 in shipped markers)
- [Source: _bmad-output/project-context.md] — generator rules (no `ISymbol` escape, `DiagnosticInfo` → `RegisterSourceOutput`, `HFC1xxx` build-time band), `CanonicalSchemaMaterial` immutability, Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), NFR17 tripwire, TWAE, test discipline, `DiffEngine_Disabled=true`, dependency-direction-to-Contracts
- [Source: _bmad-output/implementation-artifacts/2-4-accessible-expand-in-row-detail.md] — prior confirm-and-pin pattern; `ExpandInRowGeneratedGridTests` end-to-end-render precedent; source-only-assertion false-confidence lesson; VSTest socket sandbox constraint + xUnit v3 in-process fallback; 8-failure Shell baseline; retro AI-1/AI-2 taxes
- [Source: _bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md] — `BadgeProjectionRenderTests`/`BadgeProjectionSpecimen` end-to-end-render precedent; `GridViewSnapshot.Filters` surface (the `__hidden` CSV the prioritizer consumes)
- [Source: _bmad-output/implementation-artifacts/2-1-render-a-projection-from-a-projection-type.md] — generated-grid render harness (`GeneratedComponentTestBase`, `CounterStoryVerificationTests`, approval baseline)
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md] — File-List/sentinel taxes (AI-1/AI-2), failure baseline, Epic-2 dependency states, "copy-a-pattern-without-the-difference" Epic-2 risk
- [Source: src/Hexalith.FrontComposer.Contracts/Attributes/ColumnPriorityAttribute.cs] — `[ColumnPriority(int)]` shape (AC2)
- [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs] — `(Priority ?? int.MaxValue, declarationOrder)` stable sort gated on `anyPriority` (AC2 order) + HFC1029 emit (AC1)
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs] — `ParseColumnPriority` + `EmitColumnPriorityCollisionDiagnostic` HFC1028 (AC2 collision)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs] — `>15`-col `<FcColumnPrioritizer>` wrap + `_allColumnsDescriptor`/`_defaultHiddenColumns` (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor] — gear + popover, `MaxVisibleColumns=10`, ARIA (AC1)
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs] — `HFC1028_ColumnPriorityCollision`, `HFC1029_ColumnPrioritizerActivated`
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs] — HFC1028/HFC1029 descriptors (Info severity, message formats, help links)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs] — transform-test scaffolding; **AC2 order pin goes here** (currently no priority test)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1028DiagnosticTests.cs] — HFC1028 collision pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1029DiagnosticTests.cs] — HFC1029 threshold/dedupe pins (confirm; AC1)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterColumnPrioritizerTests.cs] — emitter wrap pins (confirm; AC1) — note: presence-not-order
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs] — component render/dispatch/ARIA pins (confirm; AC1)
- [Source: tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/ColumnPriorityAttributeTests.cs] — attribute pins (confirm; AC2)
- [Source: docs/diagnostics/HFC1028.md] / [Source: docs/diagnostics/HFC1029.md] — diagnostic reference docs

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- Build: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)** (TWAE clean).
- VSTest host constraint inherited from Stories 2.3/2.4 (solution-level `dotnet test` opens a local socket → `SocketException (13): Permission denied` in this sandbox). Per-assembly evidence captured via the **xUnit v3 in-process runner** (`DiffEngine_Disabled=true`); the solution-level VSTest run is the CI gate.

### Completion Notes List

**Outcome: confirm-and-pin with ZERO `src/` change (as expected).** Every column-prioritization layer was verified to hold against `src/` at `d417501`; the one genuine durability gap — the AC2 column-**ORDER** behaviour — is now closed with a transform-level regression pin.

**Verification findings (true/false + evidence):**

- **AC1 — >15-col → HFC1029 + `FcColumnPrioritizer` wrap: TRUE, already pinned.**
  - Diagnostic layer (transform stage): `RazorModelTransform.cs` emits HFC1029 `Info` under strict `if (columns.Count > 15)` (hidden defaults = indices `10..Count-1`). `Hfc1029DiagnosticTests` re-confirmed green.
  - Emitter layer: `RazorEmitterColumnPrioritizerTests` re-confirmed green (15-col no-wrap / 16-col wraps / all-keys-present).
  - Component layer: `FcColumnPrioritizerTests` (7) + spike `FcColumnPrioritizer_MaxVisibleColumns_DefaultIsTen` (1) re-confirmed green (default-10, ARIA, gear/popover `role="dialog"`).
- **AC2 — `[ColumnPriority(n)]` ordering + HFC1028 collision: TRUE; ORDER was the unpinned gap, now CLOSED.**
  - Collision diagnostic: `Hfc1028DiagnosticTests` re-confirmed green; emission site `AttributeParser.EmitColumnPriorityCollisionDiagnostic` (parse stage, `Info`), `int.MaxValue` sentinel non-collision verified.
  - Parse→IR + attribute: `ColumnPriorityAttributeTests` re-confirmed green (any signed int, property-only, non-repeatable); `PropertyModel.ColumnPriority` (`int?`) carried through the equatable IR.
  - **ORDER gap CLOSED:** The stable sort in `RazorModelTransform.cs` — `Array.Sort` over an index array, comparator `(buffer[l].Priority ?? int.MaxValue).CompareTo(...)` with `l.CompareTo(r)` index tiebreaker, gated on `anyPriority` — had **no** test asserting the resulting `ColumnModel` order (`RazorModelTransformTests` had no priority test; `DescriptorListContainsAllColumnKeys` asserts presence, not order). Added **`RazorModelColumnPriorityOrderTests`** (5 transform-level pins, all green): (1) mixed priorities + unannotated → `C,B,A`; (2) multiple unannotated trail in declaration order; (3) **NO-OP baseline** → all-unannotated preserve declaration order + null priorities (the `CounterProjectionApprovalTests` invariant, asserted explicitly so an always-sorts regression is caught); (4) collision stability via index tiebreaker; (5) signed edge values (`int.MinValue` pins front, explicit `int.MaxValue` interleaves with the unannotated sentinel by declaration order).
- **End-to-end render pin (Task 1 ASSESS / Task 2 ASSESS): ADDED and green.** Following the 2.3/2.4 render precedent, a wide (18-col > 15, D6) `[ColumnPriority]` specimen (`WidePriorityProjectionSpecimen.cs`: `WidePriorityProjection` + `WidePriorityDomain`) and a render pin (`WidePriorityProjectionRenderTests.cs`, 2 tests) were added under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`, reusing `GeneratedComponentTestBase` + `CultureScope`. `WideGrid_WrapsGeneratedGridInColumnPrioritizer_WithDefaultHiddenCount` asserts AC1 (the generated grid is wrapped by `FcColumnPrioritizer`; gear `aria-label` reflects 8 hidden = 18 − MaxVisibleColumns 10; `aria-haspopup="dialog"`). `WideGrid_OrdersColumnsByPriorityThenDeclaration_AtRenderTime` asserts AC2 by reading the prioritizer popover's checkbox sequence (which renders the generator-emitted `_allColumnsDescriptor` verbatim) — order `Gamma, Delta, Alpha, Theta, Zeta` (annotated, ascending) then the unannotated columns trailing in declaration order — keeping the order assertion off the brittle FluentUI-v5 header DOM. Both pass (2/2). The **transform-level order pin remains the load-bearing AC2 pin**; this render pin is the AC1-wrap + AC2-order bonus.
- **No restyle / no re-pin of solved layers:** zero edits to `ColumnPriorityAttribute.cs`, `RazorModelTransform.cs`, `AttributeParser.cs`, `RazorEmitter.cs`, `FcColumnPrioritizer.razor`, `CanonicalSchemaMaterial`, or any `.verified.txt` snapshot. The all-unannotated `CounterProjectionApprovalTests` baseline is byte-for-byte unchanged (re-run green).

**Test counts (before → after), `DiffEngine_Disabled=true` xUnit v3 in-process runner, default trait filter `Category!=Performance&!=e2e-palette&!=NightlyProperty&!=Quarantined`:**

| Assembly | Before | After | Delta | Notes |
|---|---|---|---|---|
| `SourceTools.Tests` | 953 total / 3 failed | **958 total / 3 failed** | **+5 pass, 0 new failures** | Proven by temporarily removing the new file: identical 3 failures both runs. |
| `Shell.Tests` | 1738 total / 8 failed | **1740 total / 8 failed** | **+2 pass, 0 new failures** | The 2 `WidePriorityProjectionRenderTests` pins pass; the new `WidePriorityProjection` specimen (a generated `[Projection]`) compiles into the assembly without disturbing the standing 8-failure baseline. |

The **3 `SourceTools.Tests` failures are pre-existing / environmental**, unrelated to column prioritization, and the same set appears with and without the new pins: `CommandFormEmitterTests.Emit_DoesNotLogModelInstance`, `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`, `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates` (the documented `deferred-work.md` file-IO cluster). Confirm-layer spot-runs all green: HFC1028+HFC1029+emitter (9/9), `ColumnPriorityAttributeTests` (2/2), `FcColumnPrioritizerTests` (7/7) + spike (1/1), `CounterProjectionApprovalTests` (1/1).

**Shell.Tests baseline:** the AC2 transform order pins landed in `SourceTools.Tests`; the AC1-wrap + AC2-order **end-to-end render pins + specimen** landed in `Shell.Tests/Generated/` (`WidePriorityProjectionRenderTests` + `WidePriorityProjectionSpecimen`). The full Shell.Tests default lane runs **1740 total / 8 failed** — the **same** 8 pre-existing/environmental failures Story 2.4 recorded (`PendingStatusReopenGovernanceTests` ×4 deferred-work file-IO, `NavigationEffectsLastActiveRouteTests` hydration, `CommandRendererFullPageTests` query-fallback, `CounterStoryVerificationTests` ×2 Verify snapshot drift) — **none new, none related to column prioritization**. The 2 new render pins pass (2/2); the new generated `WidePriorityProjection` specimen does not perturb the baseline.

**Sentinel scan (retro AI-2):** all **three** new test files (`RazorModelColumnPriorityOrderTests.cs`, `WidePriorityProjectionRenderTests.cs`, `WidePriorityProjectionSpecimen.cs`) are clean; the only `<invoke>`/`</content>` matches are inside this story file's Task 4 instruction text itself.

**Branch note (project-context deviation):** work done on `test/story-2-5-column-prioritization` (test-shaped, not `feat:`) per the "no direct commits to main" guardrail — divergent from Stories 2.1–2.4 which the story-automator committed straight to `main`.

### File List

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelColumnPriorityOrderTests.cs` — **added.** 5 transform-level AC2 column-ORDER regression pins (mixed / multi-unannotated / NO-OP baseline / collision-stability / signed-edge).
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/WidePriorityProjectionSpecimen.cs` — **added.** Wide (18-col > 15) `[ColumnPriority]` specimen projection (`WidePriorityProjection`) + `WidePriorityDomain` bounded context, whose priority order is provably different from declaration order.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/WidePriorityProjectionRenderTests.cs` — **added.** 2 end-to-end render pins: AC1 (`FcColumnPrioritizer` wraps the generated wide grid, gear aria-label = 8 hidden) + AC2 (popover checkbox order reflects priority-then-declaration).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — modified (workflow tracking: 2-5 ready-for-dev → in-progress → review → done).
- `_bmad-output/implementation-artifacts/2-5-column-prioritization-for-wide-projections.md` — modified (this story file: task checkboxes, Dev Agent Record, Senior Developer Review, Change Log, Status).

**No `src/` files changed.** No `.verified.txt` snapshots changed.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Outcome:** Approve (auto-fixed record-keeping)

**Independently verified against `src/` at `d417501` + working tree:**

- **Build:** `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors** (TWAE). Both `Shell.Tests` and `SourceTools.Tests` compile.
- **AC1 — >15-col → HFC1029 + `FcColumnPrioritizer` wrap:** IMPLEMENTED. Transform emits HFC1029 `Info` under strict `if (columns.Count > 15)`; emitter wrap + component DOM (`data-fc-column-prioritizer`, gear `data-testid`, `aria-haspopup="dialog"`, per-column checkbox `data-testid`) all present. `WidePriorityProjectionRenderTests` proves the wrap on a real generated wide grid (2/2 green).
- **AC2 — `[ColumnPriority(n)]` order + HFC1028 collision:** IMPLEMENTED. `RazorModelTransform` stable sort `(Priority ?? int.MaxValue, declarationOrder)` gated on `anyPriority`. `RazorModelColumnPriorityOrderTests` (5/5 green) + the render order pin pin the resulting `ColumnModel`/checkbox order; HFC1028 + attribute confirm pins green (9/9 + 2/2).
- **Test lanes (independently re-run, `DiffEngine_Disabled=true`, in-process xUnit v3, default trait filter):** `SourceTools.Tests` **958 / 3 failed** (3 pre-existing/environmental); `Shell.Tests` **1740 / 8 failed** (same 8 pre-existing clusters, none new, none column-prioritization). No regression introduced.

**Findings (all auto-fixed in this Dev Agent Record — no code/test change required, implementation was correct):**

| # | Sev | Finding | Resolution |
|---|---|---|---|
| 1 | HIGH | Completion Notes claimed the end-to-end render pin was *"assessed, NOT added"* — false; `WidePriorityProjectionRenderTests` + `WidePriorityProjectionSpecimen` exist and pass. | Completion Note rewritten to reflect the pins were added and are green. |
| 2 | HIGH | File List omitted the two added, git-tracked, passing Shell.Tests files (retro AI-1). | Both files added to the File List. |
| 3 | MED | Claimed *"pins landed only in SourceTools.Tests"* / *"Shell.Tests baseline untouched"*; the test-count table omitted the Shell.Tests row. | Shell.Tests row added (1738→1740/8); baseline paragraph corrected (8 same pre-existing failures, 0 new). |
| 4 | LOW | Sentinel-scan note said *"new test file"* (singular) for 3 new files. | Corrected to name all three; all verified clean. |

No CRITICAL issues: every `[x]` task is genuinely done (the implementation is in fact *more* complete than the original record claimed), both ACs hold end-to-end, build is 0/0, and no new test failures were introduced.

## Change Log

| Date | Change |
|---|---|
| 2026-06-04 | Story 2.5 dev-story (confirm-and-pin). Closed AC2 column-ORDER gap with `RazorModelColumnPriorityOrderTests` (5 transform-level pins). Re-confirmed AC1 (HFC1029 / emitter wrap / `FcColumnPrioritizer` component+default-10) and AC2 (HFC1028 collision / `[ColumnPriority]` attribute / parse→IR) layers green. Zero `src/` change, zero snapshot edits; all-unannotated approval baseline byte-for-byte unchanged. Release build 0/0 under TWAE. SourceTools.Tests 953→958 (+5 pass, 0 new failures; 3 pre-existing/environmental failures reproduced). Work on `test/story-2-5-column-prioritization` (test-shaped). Status → review. |
| 2026-06-04 | Story-automator review (adversarial, auto-fix). Independently re-verified build 0/0, AC1/AC2 in `src/`, SourceTools.Tests 958/3 + Shell.Tests 1740/8 (8 pre-existing, 0 new). Fixed 2 HIGH + 1 MED + 1 LOW **record-keeping** defects: the render pin (`WidePriorityProjectionRenderTests` + `WidePriorityProjectionSpecimen`) was actually added and green but was documented as "NOT added" and omitted from the File List, and the Shell.Tests touch/count was misreported. Corrected File List, Completion Notes, test-count table, and sentinel-scan note; added Senior Developer Review. 0 critical. Status → done. |
