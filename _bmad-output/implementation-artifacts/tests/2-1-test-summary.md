# Test Automation Summary — Story 2.1: Render a projection from a `[Projection]` type

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-04 · **QA engineer:** Administrator
**Baseline commit:** `76fc6da` · **Branch:** `feat/story-1-2-fc-lyt-page-layout`
**Mode:** Confirm-and-pin (brownfield) — auto-applied discovered coverage gaps as tests. **Zero `src/` change, zero `.verified.txt` snapshot edits.**

## Test framework detected

- **Unit / integration / render (runnable here):** xUnit **v3** + Shouldly + Verify.XunitV3 + bUnit (.NET 10, `Hexalith.FrontComposer.slnx`). Generator tests drive `FrontComposerGenerator` through `CompilationHelper` / `CSharpGeneratorDriver`.
- **Browser E2E (CI-only on this host):** Playwright workspace at `tests/e2e` (`@playwright/test` + `@axe-core/playwright`). `engines.node >= 24`; host Node is **v22.22.1**, so the browser/axe lane cannot execute locally — consistent with the story's FC-A11Y Layer-3 "CI-only" note. No browser E2E generated; the runnable generator-driver + render layer is the E2E surface for this generator-centric feature.

## Coverage map (AC1 / AC2 / AC3)

| AC | Behaviour | Status before this run | Action |
|---|---|---|---|
| AC1 | 5-file emission, public output path (Debug+Release), Loading/Empty/Data dispatch | Fully pinned (`GeneratorDriverTests`, `IdeParityConformanceUtilityTests`, `Fc*` render tests) | No gap |
| AC2 | **ActionQueue** WhenState filter + badge `aria-label` | Pinned by dev (named emit asserts) | No gap |
| AC2 | **Timeline** chronological ordering | Only *implicit* in approval snapshot | **Gap closed — pin added** |
| AC2 | **StatusOverview** aggregation (group + count) | Only *implicit* in approval snapshot | **Gap closed — pin added** |
| AC2 | **DetailRecord** single-record (card, not grid) | Only *implicit* in approval snapshot | **Gap closed — pin added** |
| AC2 | `[RelativeTime]` / `[Currency]` render, empty-state CTA | Fully pinned (`CounterStoryVerificationTests`, `FcProjectionEmptyPlaceholderTests`) | No gap |
| AC3 | non-`partial` → HFC1003 → TWAE build break | Fully pinned by dev (`Hfc1003TreatWarningsAsErrorsTests`) | No gap |

### Gap rationale

AC2 states the role strategy "governs layout (e.g. ActionQueue filters by `WhenState`; Timeline orders chronologically; StatusOverview aggregates; DetailRecord renders a single record)." The dev added explicit named emit-pins for **ActionQueue** only; the other three role behaviours lived solely inside the 500–1100-line `*_Approval.verified.txt` snapshots, where a careless snapshot re-accept could silently drop them. Three named emit-assertion tests now pin each role's distinctive layout behaviour independently of the snapshot — mirroring the dev's own AC2 pattern.

## Generated Tests

### E2E / generator-render pins (xUnit v3)

- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs`
  - [x] `TimelineProjection_EmitsChronologicalOrdering_OnTimestampProperty` — asserts `state.Items.OrderByDescending(x => x.OccurredAt)` + `"fc-timeline-row"`
  - [x] `StatusOverviewProjection_EmitsAggregation_GroupedByStatusWithCount` — asserts `.GroupBy(x => x.Status)` + `.OrderByDescending(g => g.Count)`
  - [x] `DetailRecordProjection_EmitsSingleRecordLayout_NotGrid` — asserts `state.Items[0]` + `OpenComponent<FluentCard>`

All three drive the full Parse → Transform → Emit pipeline via `FrontComposerGenerator`, use semantic emit markers (no hardcoded paths/waits), have three-part names, and are order-independent.

### API tests

- Not applicable. This feature is a Roslyn source generator (compile-time), not an HTTP/service API. The "API" under test is the attribute → generated-output contract, exercised by the generator-driver tests above and the existing `GeneratorDriverTests` / `IdeParityConformanceUtilityTests`.

## Coverage metrics

- **ProjectionRole layout behaviours explicitly pinned:** 4 / 6 named roles (ActionQueue, Timeline, StatusOverview, DetailRecord). The remaining two — **Default** (plain DataGrid) and **Dashboard** (HFC1023 → Default fallback, deferred to Story 6-3) — are covered by approval snapshots + the `DashboardWrongShapeProjection_EmitsHfc1023_ForDashboardFallback` diagnostic pin; no distinct layout to assert.
- **Acceptance criteria with durable named pins:** AC1 ✅ · AC2 ✅ (now 4/4 named role behaviours explicit) · AC3 ✅.
- **`SourceTools.Tests` assembly:** 953 total · **950 passed** · 3 failed — `+3` from this run (947 → 950 passing).

## Validation run

```
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/... \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -c Debug
→ Failed: 3, Passed: 950, Total: 953
```

Targeted run of the touched file: `RoleSpecificProjectionApprovalTests` → **16 passed / 0 failed** (includes the 3 new pins).

The **3 assembly failures are the pre-existing/environmental standing baseline**, unchanged by this run:
- `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates` — missing `deferred-work.md` fixture
- `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux` — Linux case-sensitivity
- `CommandFormEmitterTests.Emit_DoesNotLogModelInstance`

None new, none attributable to the added pins.

## Files changed

- **Modified** `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs` — +3 named AC2 role-layout emit pins (Timeline / StatusOverview / DetailRecord).
- **Added** `_bmad-output/implementation-artifacts/tests/2-1-test-summary.md` — this summary.

_No `src/` changes. No generated-output edits. No `.verified.txt` snapshot edits._

## Next steps

- New pins run in the default trait lane (no special category) — they ride CI alongside the existing suite.
- The browser/axe E2E lane (`tests/e2e`, `npm run test:a11y`) remains CI-only here (Node < 24); validate AC2 badge a11y end-to-end there when the runner has Node ≥ 24.
- Add edge cases (multi-member WhenState ordering, Timeline tie-breaking) if Epic-2 sibling stories (2.3 filter / 2.4 expand) surface new behaviours.
