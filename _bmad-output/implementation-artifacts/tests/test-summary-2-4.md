# Test Automation Summary — Story 2.4 (Accessible expand-in-row detail)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-04
**Story:** `2-4-accessible-expand-in-row-detail.md`
**Feature under test:** Accessible expand-in-row detail — AC1 always-present `role="region"` panel +
trigger wiring + single-expand; AC2 filter-hidden announcement; AC3 e2e a11y lane.

## Framework Detected

- **Component / runtime tests:** xUnit **v3** + **Shouldly** + **bUnit** (`JSRuntimeMode.Loose`),
  driven through `GeneratedComponentTestBase` against the generated `CounterProjectionView`.
- **E2E a11y / visual:** **Playwright** workspace at `tests/e2e` (`npm run test:a11y`).
  **CI-only on this host** (Node < 24 — Playwright unsupported, per the story constraint and the
  Epic-1 retro). Not installed or run locally; wiring confirmed by inspection only.
- **Run command:** solution-level `dotnet test … --filter "Category!=…"` with `DiffEngine_Disabled=true`.
  Local execution used the per-assembly xUnit v3 in-process runner because VSTest cannot open its
  local socket in this sandbox (`SocketException (13): Permission denied`).

## Gap Analysis

Component / reducer / emitter / banner layers were already pinned in isolation, and prior Story 2.4
work added two generated-grid runtime pins. This QA pass found **three runnable behaviours still
unpinned at the rendered-grid level**, each flagged by the story itself:

| Gap | AC | Why it mattered |
|---|---|---|
| Collapse-via-trigger runtime path | AC1 | Only the *expand* branch of `HandleRowClickAsync` was exercised end-to-end; the toggle→`CollapseRowAction` branch (region empties, `aria-expanded`→`false`) was unproven at render. |
| Single-expand REPLACE precondition at runtime (UX-DR17) | AC1 | REPLACE itself is pinned at the *reducer*; the runtime-unproven half was that both triggers in one grid emit the **same** ephemeral view key (the precondition REPLACE keys off) — the rendered second-row→first-row revert was source-only, the "false-confidence" risk the story calls out. |
| In-process axe proxy over the *expanded* grid | AC3 | `AxeCoreA11yTests` covers only the command renderers; Task 3 asks for a no-blocking-violations ARIA-contract proxy over the expanded generated grid in the default lane. |

## Generated Tests

### Runtime / E2E (bUnit generated-grid) — `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ExpandInRowGeneratedGridTests.cs`

Pre-existing (confirmed green):
- [x] `CounterProjectionView_ExpandTrigger_PopulatesAlwaysPresentRegion` — AC1 expand→region populates.
- [x] `CounterProjectionView_FilterHidesExpandedRow_RendersBannerAndSuppressedAnnouncement` — AC2 banner + suppressed live region.

New (this QA pass):
- [x] `CounterProjectionView_CollapseTrigger_EmptiesRegionAndResetsAria` — AC1 collapse: region empties, `aria-expanded`→`false`, `aria-controls` still resolves.
- [x] `CounterProjectionView_ExpandingSecondRow_ReplacesFirstExpansion` — AC1/UX-DR17: both triggers emit the same ephemeral view key at runtime (the REPLACE precondition; reducer REPLACE itself pinned in `ExpandedRowReducerTests`), restored single-key slice shows one region, first row reverts to collapsed.
- [x] `CounterProjectionView_ExpandedRow_HasNoBlockingAxeContractViolations` — AC3 in-process axe proxy over the expanded grid (`region`/`button-name`/`aria-valid-attr-value`/`aria-expanded`; suppression live region silent while visible).

### API Tests
- N/A — Story 2.4 is a UI/accessibility surface; no HTTP endpoint or service contract is in scope.

### E2E (Playwright, CI-only — wiring confirmed, not run)
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` drives `expectNoBlockingAxeViolations` over every
  `specimenManifest.routes` entry; the `type` specimen asserts `fc-expanded-detail` and the keyboard
  flow reaches `fc-expanded-detail-summary`. **AC3's Layer-3 gate is CI-owned.**

## Coverage

- **AC1:** expand ✓, collapse ✓, REPLACE precondition (shared view key) at runtime ✓ + reducer REPLACE pinned in unit, `aria-expanded`/`aria-controls` ✓ — closed end-to-end.
- **AC2:** filter-hidden banner + suppressed announcement ✓.
- **AC3:** in-process axe proxy now covers the **expanded** generated grid ✓; Playwright Layer-3 gate confirmed wired (CI-only).

## Verification

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/…csproj -c Release -m:1 /nr:false` → **0 warnings / 0 errors** (TWAE).
- `ExpandInRowGeneratedGridTests` → **5/5 passed** (2 pre-existing + 3 new).
- Related confirm pins (`FcExpandInRowDetailTests`, `FcExpandedRowHiddenBannerTests`, `ExpandedRowReducerTests`, `AxeCoreA11yTests`) → **20/20 passed**, no regression.
- No `src/` change; no generated output or `.verified.txt` snapshots touched.
- Sentinel scan (`</content>`/`<invoke>`/tool tags) on the test file → clean.

## Next Steps

- Run the full default lane + Playwright a11y lane in CI (the Layer-3 axe gate is CI-owned on this host).
- No further expand-in-row coverage gaps identified for AC1–AC3.
