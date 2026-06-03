# Test Automation Summary — Story 2.2 (Registry-driven navigation & home directory)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Role:** QA automation engineer (test generation only — no code review / story validation)
**Date:** 2026-06-04 · **Baseline commit:** `a0cbab2` · **Branch:** `feat/story-1-2-fc-lyt-page-layout`
**Mode:** auto-apply all discovered gaps.

## Framework detected

- **bUnit 2.7.2** + **xUnit v3** + **Shouldly** + **NSubstitute** (the runnable component / "E2E-at-the-seam" layer).
  New tests follow the existing `LayoutComponentTestBase` (Fluxor host, `JSInterop.Mode = Loose`,
  registry/ulid substitutes, seed via dispatched `BadgeCountsSeededAction` / `SeenCapabilitiesHydratedAction`).
- **Playwright e2e workspace** exists at `tests/e2e` but is **CI-only / unrunnable on this host**
  (`engines.node >= 24`; host is **Node v22.22.1**) — matches the story's FC-A11Y note that Layer-3
  e2e axe is CI-only. No Playwright specs were generated (they could not be executed/verified here),
  so coverage was added at the runnable bUnit layer where Step 4 ("run tests to verify they pass") holds.

## Coverage analysis (what was already automated)

All three ACs were already substantially covered (tree/grouping/route/click/visibility, the four home
states + urgent/progressive/flat/accordion sort orderings, and AC3 compact-collapse from five angles).
Three **genuine gaps** remained — each a contract the source renders but **no test asserted**:

| # | Gap | AC | Risk if unpinned |
|---|-----|----|------------------|
| 1 | Full `FluentNav` accessible name — `NavMenuAriaLabel` was tested only as a *resource value*, never as the rendered `aria-label` on the nav element | AC1 / FC-A11Y | Nav could lose/blank its accessible name; every existing pin still passes |
| 2 | `FcHomeRouteView` `@page "/"` + `@page "/home"` → `<FcHomeDirectory/>` — **zero** tests referenced the route shim | AC2 | Deep-link route contract or the directory mount could silently break |
| 3 | Count badge `Filled`/`Brand` + projection-"New" badge `Tint`/`Informative` appearance — tests pinned only the `testid` + text, not the styling contract Task 1 says "do not restyle" | AC1 | Badges could be restyled with all pins still green |

## Generated Tests

### E2E / component (bUnit)

- [x] `tests/…/Components/Layout/FrontComposerNavigationTests.cs` — **+3** pins (gaps #1, #3):
  - `FullNav_RendersLocalizedNavMenuAriaLabel` — nav exposes the localized `NavMenuAriaLabel` as `aria-label`.
  - `CountBadge_UsesFilledBrandAppearance_AsShippedContract` — count `FluentBadge` is `Filled`/`Brand` (typed-component assertion via `FindComponents<FluentBadge>` + `data-testid`).
  - `ProjectionNewBadge_UsesTintInformativeAppearance_AsShippedContract` — projection-"New" `FluentBadge` is `Tint`/`Informative`.
- [x] `tests/…/Components/Pages/FcHomeRouteViewTests.cs` — **+2** pins, new file (gap #2):
  - `DeclaresRootAndHomeRoutes` — reflects `[Route]` attributes; asserts `"/"` and `"/home"`.
  - `MountsFcHomeDirectory` — renders the shim and finds the `FcHomeDirectory` component / `fc-home-directory` testid.

### API tests

- n/a — Story 2.2 is a Blazor shell UI surface (registry-driven nav + home). No HTTP/API endpoints in scope.

## Test execution (verified)

- **Build:** `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under `TreatWarningsAsErrors`.
- **Focused** (`FrontComposerNavigationTests|FcHomeRouteViewTests|FcHomeDirectoryTests`): **46 passed / 0 failed** (was 41 → **+5** new pins, all green).
- **Default lane** (`Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined`):
  - Contracts 159✓ · Mcp 291✓ · Testing 11✓
  - **Shell.Tests: 1701 passed / 8 failed / 1709 total** — the 8 are the documented pre-existing/environmental set (3 Generated + 4 Governance `PendingStatusReopen` + 1 `NavigationEffectsLastActiveRoute`); **none mine**.
  - Cli 2 failed · SourceTools 3 failed — unchanged (`src/` untouched this pass).
  - **Standing 13-failure baseline (8 Shell + 2 Cli + 3 SourceTools) re-proved pre-existing.**
  - Note: `EventStoreTelemetryTests.QueryNotModifiedTwiceWithoutCache_TagsProtocolDriftRetry` is **flaky under full-parallel load** (passes 3/3 in isolation) — not part of the stable 8, not introduced here.

## Coverage (Story 2.2 ACs)

- **AC1** (nav tree + count/"New" badges): full tree/grouping/route/click/visibility + badge testid/text/**appearance** + **aria-label** → covered.
- **AC2** (home four states + urgency sort + **route**): four states + all four sort orderings + **`/` & `/home` route shim** → covered.
- **AC3** (compact collapse): pre-pinned from five angles; confirm-only, **no change**.

## Next Steps

- Run the **Playwright Layer-3 e2e / axe** suite in CI (Node ≥24) for browser-level AC1–AC3 confirmation — out of scope on this host.
- Consider quarantining/stabilizing the flaky `EventStoreTelemetryTests` protocol-drift timing test (separate from Story 2.2).

---

**Checklist** (`bmad-qa-generate-e2e-tests/checklist.md`): all items ✅. Tests use semantic locators
(testids, roles, typed components), clear `Subject_Scenario_Expectation` names, no hardcoded waits
(`WaitForAssertion`), and are order-independent. `src/` and `.verified.txt` snapshots untouched.
