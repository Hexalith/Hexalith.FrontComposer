# Test Automation Summary — Story 2.5 (Column prioritization for wide projections)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Role:** QA automation engineer (test generation only)
**Date:** 2026-06-04 · **Baseline commit:** `d417501` · **Branch:** `test/story-2-5-column-prioritization`
**Framework:** xUnit v3 + Shouldly + bUnit (auto-detected; matched existing project patterns)

## Feature under test

Story 2.5 — wide projections (**>15 columns**, strict per D6) activate `FcColumnPrioritizer` (AC1) and
columns order by `[ColumnPriority(n)]` → `(Priority ?? int.MaxValue, declarationOrder)` stable sort
with HFC1028 collision diagnostic (AC2).

## Gap analysis

The isolated layers were already pinned at baseline:

| Layer | Existing pin | Asserts |
|---|---|---|
| Transform sort (AC2 order) | `RazorModelColumnPriorityOrderTests` (added by dev-story) | `ColumnModel` order, transform level |
| Emitter wrap (AC1) | `RazorEmitterColumnPrioritizerTests` | emitted source — **presence, not order** |
| Component (AC1) | `FcColumnPrioritizerTests` | gear/popover/ARIA on **hand-built** columns |
| Diagnostics | `Hfc1028DiagnosticTests` / `Hfc1029DiagnosticTests` | build-time Info diagnostics |

**Discovered gap (auto-applied):** no test renders an **actual generated wide grid** and proves AC1
(prioritizer wraps it) + AC2 (priority ORDER) *together at render time* — the gap the dev-story
explicitly assessed and deferred. The Story 2.3 (`BadgeProjectionRenderTests`) and Story 2.4
(`ExpandInRowGeneratedGridTests`) precedent is to render the generated view through bUnit. This
workflow closes it.

## Generated Tests

### E2E / generated-grid render tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/WidePriorityProjectionRenderTests.cs` — 2 tests
  - `WideGrid_WrapsGeneratedGridInColumnPrioritizer_WithDefaultHiddenCount` (**AC1**) — the generated
    `WidePriorityProjectionView` is wrapped by `div.fc-column-prioritizer`; the gear renders with
    `aria-haspopup="dialog"`, `aria-expanded="false"`, and `aria-label` reflecting **8** default-hidden
    columns (18 − `MaxVisibleColumns` 10).
  - `WideGrid_OrdersColumnsByPriorityThenDeclaration_AtRenderTime` (**AC2**) — opening the prioritizer
    popover renders one checkbox per column (`data-testid="fc-column-prioritizer-checkbox-{key}"`) in the
    generator-emitted `_allColumnsDescriptor` order; asserts the full 18-key sequence is
    priority-then-declaration (`Gamma,Delta,Alpha,Theta,Zeta, Id,Beta,Epsilon,Eta,Iota,Kappa,Lambda,Mu,Nu,Xi,Omicron,Pi,Rho`)
    — deterministically **different** from declaration order. Asserted on the plain-markup checkbox list,
    not the FluentUI-v5 data-grid shadow DOM, per the story's brittleness caveat.

### Specimen
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/WidePriorityProjectionSpecimen.cs` — first
  wide (>15-col) `[Projection]` test specimen: `WidePriorityProjection` with 18 properties, 5 carrying
  scrambled `[ColumnPriority]` values + a `WidePriorityDomain` bounded context. Follows the
  `BadgeProjectionSpecimen` / `StatusProjection` precedent.

### API tests
- N/A — FrontComposer ships no deployed HTTP service; the surface exercised here is the in-process
  generated-grid render path, covered above.

## Coverage

- AC1 (>15-col → HFC1029 + prioritizer wrap): emitter + component (pre-existing) **+ end-to-end render (new)** ✅
- AC2 (`[ColumnPriority]` ordering + HFC1028): transform order + diagnostic (pre-existing) **+ end-to-end render order (new)** ✅
- Generated wide-grid render path: **0 → 1 specimen covered**

## Test execution (xUnit v3 in-process runner, `DiffEngine_Disabled=true`)

> Solution-level VSTest opens a local socket → `SocketException (13): Permission denied` in this
> sandbox (inherited Stories 2.3/2.4 constraint); per-assembly in-process runner used for local
> evidence — the solution-level VSTest run is the CI gate.

- **New tests:** `WidePriorityProjectionRenderTests` — **2 total / 2 passed / 0 failed**.
- **Release build:** `dotnet build … -c Release -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)** (TWAE clean).
- **Shell.Tests default lane** (`Category!=Performance&!=e2e-palette&!=NightlyProperty&!=Quarantined`):
  **1740 total / 8 failed** — the +2 new tests pass; failure count unchanged from the recorded baseline.

### Standing failure baseline (re-proved pre-existing — none new, none misattributed)

The same **8** failures recorded for Story 2.4 remain, all environmental/pre-existing and unrelated to
column prioritization (none touch the new specimen):

- `CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting`, `…StatusProjectionView_NullAndBooleanValues_RenderSnapshot` — Verify snapshot-drift cluster
- `PendingStatusReopenGovernanceTests.*` (4) — `deferred-work.md` file-IO governance cluster
- `CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` — query-fallback cluster
- `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute_DispatchesHydratedActions` — navigation-hydration cluster

## Notes

- **Zero `src/` change**, **zero `.verified.txt`** edits — the new specimen carries no Verify snapshot;
  the AC1/AC2 assertions are assertion-based. The all-unannotated `CounterProjectionApprovalTests`
  byte-for-byte baseline is unaffected (no shared generated output).
- Sentinel scan (retro AI-2): new files clean — no stray tool/authoring tags.

## Next Steps

- Run the solution-level VSTest lane in CI (the blocking gate) to confirm the in-process evidence.
- Optional future coverage: a render-level HFC1028 collision specimen (currently pinned at the parse
  stage only) if an end-to-end collision assertion is later wanted.
