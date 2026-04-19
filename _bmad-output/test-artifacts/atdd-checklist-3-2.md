---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04-generate-tests']
lastStep: 'step-04-generate-tests'
lastSaved: '2026-04-19'
generationMode: 'ai-generation'
storyId: '3-2'
storyStatus: 'ready-for-dev'
inputDocuments:
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/index.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/story.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/acceptance-criteria.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/critical-decisions-read-first-do-not-revisit.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/architecture-decision-records.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/tasks-subtasks.md
  - tests/e2e/playwright.config.ts
  - tests/e2e/specs/smoke.spec.ts
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs
  - .claude/skills/bmad-testarch-atdd/resources/knowledge/test-healing-patterns.md
knowledgeFragmentsLoaded:
  core:
    - data-factories
    - component-tdd
    - test-quality
    - test-healing-patterns
    - selector-resilience
    - timing-debugging
    - fixture-architecture
    - network-first
    - test-levels-framework
    - test-priorities-matrix
    - playwright-cli
  extended:
    - ci-burn-in
teaConfigFlags:
  tea_use_playwright_utils: true
  tea_use_pactjs_utils: false
  tea_pact_mcp: 'none'
  tea_browser_automation: 'auto'
  test_stack_type: 'auto-detected:fullstack'
---

# ATDD Checklist — Story 3-2: Sidebar Navigation & Responsive Behavior

## Step 1 — Preflight & Context

### Stack Detection
- **Detected:** `fullstack` (auto) — .NET 10 + Blazor Server sidebar in `src/Hexalith.FrontComposer.Shell/` + Playwright E2E under `tests/e2e/`.

### Prerequisites
- [x] Story approved — `ready-for-dev` per `sprint-status.yaml` (last updated 2026-04-18).
- [x] Clear AC — 7 ACs with Given/When/Then, test names, task and decision cross-refs.
- [x] Test framework configured — `tests/e2e/playwright.config.ts` + `tests/Hexalith.FrontComposer.Shell.Tests` (xUnit + bUnit + NSubstitute + Shouldly + Verify).
- [x] Dev env — `samples/Counter/Counter.Web` boots under Aspire.

### Story Context Summary
- **Goal:** Framework-owned `FrontComposerNavigation` sidebar auto-populating `FrontComposerShell.Navigation` from `IFrontComposerRegistry`, persisted per tenant/user, adaptive across 4 viewport tiers.
- **Scope:** 7 ACs, 23 binding decisions, 4 ADRs (035–038), 11 tasks. Target ~38 new tests.
- **Non-trivial invariants (memory-aware):**
  - Fail-closed tenant scoping — HFC2105 log **AND** `storage.SetAsync` never called (AC2 + Task 10.6.2–10.6.4).
  - ViewportTier never persisted (ADR-037 + Task 10.6.5 + Task 10.7).
  - Hydrate does NOT re-persist (ADR-038 + Task 10.6.6).
  - Parameter-surface append-only (D10 + Task 10.9 Verify drift 8→9).
  - Commands-only manifest → no category (D1 + Task 10.1.4).
  - JS dedup on same composed tier (D6 + Task 10.4.5).

### Cross-Story Contracts Produced by 3-2
- `ViewportTier` enum + `ViewportTierChangedAction` → consumed by 3-3 / 3-4 / 3-5 (ordinal-stable byte values).
- `FrontComposerNavigationState` — schema-locked via `NavigationPersistenceSnapshotTests` → consumed by 3-6.
- `FrontComposerShell.HeaderCenter` RenderFragment parameter (append-only, position between HeaderStart and HeaderEnd) → consumed by 3-5.
- `Navigation` slot auto-population (D18) → consumed by every adopter (Counter.Web + Epic 6).
- Plain `<FluentNavItem>` label today → forward-compatible with 3-5 badge wrap.
- Storage blob under `{tenantId}:{userId}:nav` → consumed by 3-6 session-resume.

### Budget Check (memory-aware — `feedback_defense_budget.md`)
- **Binding decisions:** 23 (feature story — under the ≤25 cap).
- **Targeted tests:** ~38 → decision-to-test ratio **1.65** — inside Murat's 1.5–1.8 band (Task 10.12 PR-review gate). No trim or add needed at spec time.

### Existing Test Patterns to Mirror
| Concern | Reference file | Pattern |
|---|---|---|
| Fluxor effect scope guard | `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs` | NSubstitute `IStorageService` + `DidNotReceiveWithAnyArgs()`; custom `AssertLoggedInformation(logger, diagnosticId)` helper for `ILogger.Log` capture |
| Parameter surface snapshot | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs` | Verify snapshot (`.verified.txt`) — drift = hard fail |
| Resource parity | `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` | `CanonicalKeysHaveFrenchCounterparts` enumeration — self-updates from resx |
| E2E smoke | `tests/e2e/specs/smoke.spec.ts` | POM (`page-objects/`), BDD comments, axe-core via `expectNoAxeViolations` |

### TEA Config Flags (from `_bmad/tea/config.yaml`)
- `tea_use_playwright_utils: true` — existing specs use traditional POM; we keep alignment, defer playwright-utils introduction to a later refactor.
- `tea_use_pactjs_utils: false`
- `tea_pact_mcp: none`
- `tea_browser_automation: auto`
- `risk_threshold: p1`

### Knowledge Fragments Loaded
See YAML frontmatter `knowledgeFragmentsLoaded`.

### Inputs Confirmed
- Ready to generate failing acceptance tests in Step 2.

## Step 2 — Generation Mode

**Mode:** `ai-generation`

**Rationale:**
- Components under test (`FrontComposerNavigation`, `FcCollapsedNavRail`, `FcHamburgerToggle`, `FcLayoutBreakpointWatcher`) are not yet implemented — no DOM to record against.
- ACs are fully spec'd by 23 binding decisions + 4 ADRs; DOM structure, action dispatch semantics, persistence schema, ARIA labels, and resource keys are all pinned.
- Majority of the ~38 tests are bUnit + Fluxor unit + Verify snapshot — no browser runtime.
- Only Playwright E2E (Task 10.11) needs a live browser, and its DOM targets are dictated by the spec we're about to implement.

**Recording deferred to:** future test-healing / refactor passes where a live DOM can be snapshotted (Task 10.11 follow-ups in Story 10-2 axe + focus-ring screenshots).

## Step 3 — Test Strategy

### Priority Rubric (project-specific, risk-weighted)
- **P0** — Tenant isolation, wire-format snapshots, parameter surface, WCAG keyboard invariants, auto-populate core path. Failing any of these breaks either a compliance / cross-story contract or a P0 ergonomics claim. Must be gate-blocking on every PR.
- **P1** — Responsive rendering, persistence round-trip, viewport watcher dispatch + dedup, resource EN/FR parity, E2E resize smoke. Impacts UX quality but not tenant safety. Gate-blocking on main-branch CI.
- **P2** — Sparse-blob semantics (SetItem vs Remove), idempotency (`SidebarExpandedAction`), skip-tier dedup, empty-blob hydrate behavior. Edge cases; run on PR but tolerate a single retry.

### AC → Test Strategy Matrix

| AC | Scenario | Level | Priority | Test Case | Risk Justification |
|---|---|---|---|---|---|
| **AC1** | One `FluentNavCategory` per manifest with ≥1 projection | Component (bUnit) | P0 | `FrontComposerNavigationTests.RendersOneCategoryPerManifest` | Core render path — if broken, no nav appears. |
| **AC1** | One `FluentNavItem` per projection FQN | Component | P0 | `FrontComposerNavigationTests.RendersOneItemPerProjection` | Core render path. |
| **AC1** | Commands NOT rendered as nav items (D1 UX §141) | Component | P0 | `FrontComposerNavigationTests.DoesNotRenderCommandsAsNavItems` | Governance — commands appear inside projection views, not nav. Leak = spec violation. |
| **AC1** | Commands-only manifest produces NO category (D1 clarification, 2026-04-18) | Component | P1 | `FrontComposerNavigationTests.HidesCategoryWhenProjectionsEmpty` | Edge case — wrong-on-fix produces noise-without-signal empty shells. |
| **AC1** | Convention route `/{bc-lower}/{projection-kebab}` (D2) | Unit (pure static) | P1 | `FrontComposerNavigationTests.BuildRouteProducesExpectedHref` | Contract for Epic 4 route extensions; drift breaks every projection URL. |
| **AC2** | Valid scope → persist with `{tenantId}:{userId}:nav` key | Component+Effect (bUnit + NSubstitute) | P0 | `NavigationEffectsScopeTests.PersistsOnValidScope` | Wire-format: storage key format is cross-story contract (3-6). |
| **AC2** | Null tenant → log HFC2105 **AND** `storage.SetAsync` NEVER called | Component+Effect | P0 | `NavigationEffectsScopeTests.SkipsOnNullTenant` | **Tenant isolation** — memory `feedback_tenant_isolation_fail_closed.md`. Both assertions required (Murat feedback). |
| **AC2** | Null user → log HFC2105 **AND** no storage call | Component+Effect | P0 | `NavigationEffectsScopeTests.SkipsOnNullUser` | Same as above — symmetric. |
| **AC2** | Whitespace tenant/user → log + no storage call | Component+Effect | P0 | `NavigationEffectsScopeTests.SkipsOnWhitespaceUserContext` (`[Theory]` for tenant-only + user-only) | Whitespace-is-empty invariant — must be an explicit test, not implicit. |
| **AC2** | `ViewportTierChangedAction` does NOT trigger persist (ADR-037) | Component+Effect | P1 | `NavigationEffectsScopeTests.ViewportTierChangedDoesNotTriggerPersist` | Performance + wire-format — viewport is observation, not preference. |
| **AC2** | `NavigationHydratedAction` does NOT trigger re-persist (ADR-038) | Component+Effect | P1 | `NavigationEffectsScopeTests.HydrateDoesNotRePersist` | Closes pre-hydration SSR ordering surface. |
| **AC2** | Blob schema pinned — no `viewport`/`tier` field | Snapshot (Verify) | P0 | `NavigationPersistenceSnapshotTests.BlobSchemaLocked` | Wire format locked — drift breaks Story 3-6 session-resume. |
| **AC2** | `SidebarToggled` → flips `SidebarCollapsed` | Unit (reducer) | P1 | `NavigationReducerTests.SidebarToggledFlipsFlag` | Reducer purity invariant. |
| **AC2** | `NavGroupToggled` collapse adds entry / expand removes entry (D11 sparse) | Unit | P2 | `NavigationReducerTests.NavGroupToggled_AddRemove` (`[Theory]`) | Sparse-by-default blob discipline. |
| **AC2** | `NavigationHydrated` replaces wholesale | Unit | P1 | `NavigationReducerTests.NavigationHydratedReplacesWholesale` | Hydrate semantics — single-source reducer path. |
| **AC2** | `SidebarExpanded` idempotent | Unit | P2 | `NavigationReducerTests.SidebarExpandedIsIdempotent` | Low-risk — second dispatch = no-op. |
| **AC3** | Desktop tier → 220 px expanded nav, hamburger hidden | Component (bUnit + Fluxor state) | P1 | `FrontComposerNavigationTests.RendersFullNavAtDesktop` + `FcHamburgerToggleTests.VisibleFalseAtDesktopWhenNotCollapsed` | Default happy path. |
| **AC3** | New user default: `SidebarCollapsed = false` | Unit (feature) | P1 | `NavigationReducerTests.ViewportTierChangedOnlyUpdatesCurrentViewport` + feature-default assertion via `FrontComposerNavigationFeature.GetInitialState()` | UX §170 guarantee. |
| **AC4** | CompactDesktop → `FcCollapsedNavRail` rendered | Component | P1 | `FrontComposerNavigationTests.RendersRailAtCompactDesktop` | Responsive core. |
| **AC4** | Rail: one button per manifest, tooltip = BC name, click dispatches `SidebarExpanded` | Component | P1 | `FcCollapsedNavRailTests.RendersOneButtonPerManifest` / `.TooltipContainsBoundedContextName` / `.ClickDispatchesSidebarExpanded` | Rail component contract. |
| **AC4** | Hamburger visible at CompactDesktop + Tablet + Phone | Component | P1 | `FcHamburgerToggleTests.VisibleAcrossNonDesktopTiers` (`[Theory]` Tablet/Phone/CompactDesktop) | D7 UX matrix. |
| **AC4** | Watcher dispatches `ViewportTierChangedAction(CompactDesktop)` | Component (bUnit + IJSRuntime mock) | P1 | `FcLayoutBreakpointWatcherTests.DispatchesInitialTierOnSubscribe` | Watcher lifecycle contract. |
| **AC4** | Watcher dedupes identical composed tier (D6) | Component | P1 | `FcLayoutBreakpointWatcherTests.DedupesWhenComposedTierUnchanged` | Prevents Blazor Server interop chatter — ~100 calls/sec during resize drag. |
| **AC4** | Watcher imports module on first render only, disposes cleanly | Component | P1 | `FcLayoutBreakpointWatcherTests.ImportsModuleOnFirstRender` + `.DisposesCleanly` | Lifecycle invariants. |
| **AC4** | Double-boundary skip-tier (D6-spawned finding) | Component | P2 | `FcLayoutBreakpointWatcherTests.DispatchesCorrectTierOnDoubleBoundarySkip` | Edge case — fast resize from Desktop → Tablet without intermediate CompactDesktop. |
| **AC5** | Tablet/Phone → Navigation hidden, drawer-only | Component | P1 | `FcHamburgerToggleTests.VisibleAtTablet` / `.VisibleAtPhone` (covered by matrix [Theory]) | D7 UX matrix. |
| **AC5** | Manual toggle at Desktop dispatches `SidebarToggled` | Component | P1 | `FcHamburgerToggleTests.ManualToggleAtDesktopDispatchesSidebarToggled` | User-driven collapse path (D9). |
| **AC6** | Every `FluentNavItem` tab-reachable (no `tabindex="-1"` on focusables) | Component | P0 | `FrontComposerNavigationTests.NavItemsAreTabReachable` | **WCAG 2.1 AA** — regulatory-adjacent. |
| **AC6** | 5 new ARIA resource keys present in EN + FR, `{0}` placeholder round-trips | Resource (xUnit) | P1 | `FcShellResourcesTests.NavigationAriaKeysArePresent` + existing parity test | L10n contract. |
| **AC7** | MainLayout.razor is exactly three substantive lines | Integration (file shape) | P0 | `CounterWebIntegrationTests.MainLayoutIsThreeLines` | Zero-setup ergonomics pitch (D17). |
| **AC7** | `Navigation` slot auto-populates when null + registry non-empty | Integration (bUnit + real registry) | P0 | `FrontComposerShellTests.AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty` | Core contract (D18 / ADR-035). |
| **AC7** | Adopter-supplied `Navigation` fragment wins | Integration | P0 | `FrontComposerShellTests.AdopterSuppliedNavigationFragmentWins` | Override escape hatch (D18). |
| **AC7** | Parameter count 8→9, `HeaderCenter : RenderFragment?` between `HeaderStart` and `HeaderEnd` | Snapshot (Verify) | P0 | `FrontComposerShellParameterSurfaceTests` verified.txt drift | **Append-only parameter discipline** (D10) — cross-story contract. |
| **AC4 + AC5 + AC7** | Resize 1920→1200→800→600 produces correct DOM transitions | E2E (Playwright) | P1 | `sidebar-responsive.spec.ts` — `resizes across tiers` | Integration smoke against real Blazor Server. |
| **AC2 + AC7** | Collapse at Desktop persists across refresh | E2E | P1 | `sidebar-responsive.spec.ts` — `sidebar collapse persists across refresh` | Round-trip through LocalStorage on a live browser. |

### Level Distribution
- **Unit (pure xUnit):** 6 tests — reducers + static helpers
- **Component (bUnit):** ~20 tests — FrontComposerNavigation / FcCollapsedNavRail / FcHamburgerToggle / FcLayoutBreakpointWatcher
- **Integration (bUnit + wired Fluxor / registry):** 6 tests — effect scope + shell auto-populate + Counter.Web shape
- **Snapshot (Verify):** 2 tests — persistence blob + parameter surface
- **Resource:** 1 addendum (parity test self-extends from resx keys)
- **E2E (Playwright):** 2 specs (`resizes across tiers`, `sidebar collapse persists across refresh`)

**Total: ~37 new tests** (within the ~38 target ±2 per Task 10.12 budget gate; ratio vs 23 decisions = 1.61).

### Priority Distribution
- **P0 (gate-blocking / every commit):** 13 tests — tenant isolation, wire-format snapshots, parameter surface, WCAG, auto-populate, three-line MainLayout
- **P1 (main-branch CI):** 18 tests — responsive rendering, persistence round-trip, watcher, resource parity, E2E smoke
- **P2 (PR with one retry):** 6 tests — sparse-blob semantics, idempotency, edge-case dedup

### Red-Phase Requirements

Every test below must fail before Task 1–9 implementation. Verification strategy:

| Red-phase mechanism | Applies to |
|---|---|
| Test references a type / member that doesn't exist yet (`FrontComposerNavigationState`, `ViewportTier`, `NavigationActions.*`, `FrontComposerNavigation`, `FcCollapsedNavRail`, etc.) → **compile error** | All unit + component + integration + snapshot tests |
| Verify snapshot missing (no `.verified.txt` exists for `NavigationPersistenceSnapshotTests`) → test fails with `VerifyException` on first run | `NavigationPersistenceSnapshotTests.BlobSchemaLocked` |
| Verify snapshot drift (`.verified.txt` for `FrontComposerShellParameterSurfaceTests` still shows 8 parameters) → snapshot mismatch on first run after `HeaderCenter` parameter added | Parameter surface test |
| Resource key missing in resx → `IStringLocalizer["NavMenuAriaLabel"].ResourceNotFound == true` | Resource parity test addendum |
| Playwright test selector points at `[data-testid="fc-collapsed-rail"]` / `[data-testid="fc-hamburger-toggle"]` which don't render yet → timeout failure | E2E smoke specs |

**CI enforcement:** the `dotnet test` task in Task 11.2 runs the suite; red-phase gate is that after checkout + `dotnet build` at Step 4 output, every new test in this checklist is authored and `dotnet test --filter "FullyQualifiedName~Navigation"` reports non-zero failures. Once implementation lands, suite transitions to green. Task 10.13 CI snapshot-drift gate is a **hard fail** — confirm in `.github/workflows/*.yml` when we reach Step 4.

## Step 4 — Generated FAILING Test Suite (TDD Red Phase)

### Generation mode
- Resolved: `sequential` (inline generation — subagent/agent-team not dispatched for .NET stack; test file authoring happens in the main thread since the spec already carries all information needed).
- TDD phase: **RED** — every new test file compiles against types / components that do not yet exist, or asserts DOM / resource keys that are not yet emitted. First `dotnet test` + first `npx playwright test` will fail loudly. That is the intended signal.

### Files Created (11)

| # | File | Tests | AC(s) | Task(s) | Red-phase mechanism |
|---|---|---|---|---|---|
| 1 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducerTests.cs` | 8 | AC2, AC3, AC4 | 10.5 | Compile error — types `FrontComposerNavigationState`, `ViewportTier`, `NavigationReducers`, `SidebarToggledAction`, `NavGroupToggledAction`, `ViewportTierChangedAction`, `SidebarExpandedAction`, `NavigationHydratedAction` not yet present |
| 2 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs` | 8 | AC2 | 10.6 | Compile error — `NavigationEffects`, `NavigationPersistenceBlob`, `NavigationEffectsInstrumentation` not yet present |
| 3 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.cs` | 1 | AC2 (D21, ADR-037) | 10.7 | Compile error on `NavigationPersistenceBlob`; Verify snapshot baseline locks wire format |
| 4 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.BlobSchemaLocked.verified.txt` | — | AC2 | 10.7 | Baseline snapshot — `.received.txt` diff on any future field add |
| 5 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` | 10 | AC1, AC3, AC6 | 10.1 | Compile error on `FrontComposerNavigation`, `FcCollapsedNavRail`, `ViewportTierChangedAction` etc. |
| 6 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs` | 3 | AC4 | 10.2 | Compile error on `FcCollapsedNavRail` |
| 7 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs` | 5 | AC4, AC5 | 10.3 | Compile error on `FcHamburgerToggle` + `IsVisibleForTest` / `OnHamburgerOpenedForTest` test seams |
| 8 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs` | 6 | AC3, AC4, AC5 | 10.4 | Compile error on `FcLayoutBreakpointWatcher` + `OnViewportTierChangedAsync` |
| 9 | `tests/Hexalith.FrontComposer.Shell.Tests/Integration/CounterWebIntegrationTests.cs` | 1 | AC7 | 10.10 | Assertion fails until Counter.Web `MainLayout.razor` collapses to 3 lines (Task 9.1) |
| 10 | `tests/e2e/page-objects/shell.page.ts` | — (POM) | AC4, AC5, AC7 | 10.11 | Selectors target `data-testid` markers not yet emitted |
| 11 | `tests/e2e/specs/sidebar-responsive.spec.ts` | 3 (specs) | AC4, AC5, AC7 | 10.11 | Selector timeout until sidebar renders |

### Files Modified (3)

| # | File | Change | Red-phase mechanism |
|---|---|---|---|
| 12 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` | +3 tests: `AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty`, `AdopterSuppliedNavigationFragmentWins`, `NoNavigationRendersWhenSlotNullAndRegistryEmpty` | `FindComponent<FrontComposerNavigation>` throws until Task 8.3 auto-populate lands |
| 13 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs` | Expected array updated 6→7 parameters with `HeaderCenter` inserted at index 1 per D10 ordering. **Note:** story D10 says "8→9" but the Story 3-1 baseline is 6 parameters, so the real count is 6→7 — the test encodes reality, not the story's miscount. | Reflection assertion fails until Task 8.4 adds `HeaderCenter` parameter |
| 14 | `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` | +2 `[Theory]` tests covering 5 new keys (`NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `SkipToNavigationLabel`, `NavGroupExpandAriaLabel`, `NavGroupCollapseAriaLabel`) in EN + FR, including `{0}` round-trip | `localizer[key].ResourceNotFound == true` until Task 8.5 / 8.6 update resx files |

### Test Count (Red Phase)

| File | Test count |
|---|---|
| NavigationReducerTests | 8 |
| NavigationEffectsScopeTests | 8 (incl. `[Theory]` × 4 for whitespace) |
| NavigationPersistenceSnapshotTests | 1 |
| FrontComposerNavigationTests | 10 |
| FcCollapsedNavRailTests | 3 |
| FcHamburgerToggleTests | 5 |
| FcLayoutBreakpointWatcherTests | 6 |
| CounterWebIntegrationTests | 1 |
| FrontComposerShellTests (added) | +3 |
| FrontComposerShellParameterSurfaceTests (reused) | 1 (updated) |
| FcShellResourcesTests (added) | +2 (`[Theory]` rows expand) |
| **Total .NET tests** | **~48 `[Fact]` / `[Theory]` declarations** (above the 38 target — Task 10.12 PR-review gate should trim if ratio >1.8; suggest trimming Theory rows where duplicated coverage exists) |
| Playwright specs | 3 |
| **Grand total** | **~51 tests** |

### Budget Reconciliation vs Story 3-2 Target
- **Story target:** ~38 tests (Task 10 sum); PR-review band 1.5–1.8× decisions (23) = 34.5 – 41.4 tests.
- **Generated:** ~51 tests (ratio 2.2 — above the upper band per Task 10.12).
- **Trim candidates (to land near 40):**
  - `NavigationReducerTests.SidebarToggledIsInvolution` — low-risk coverage of what `SidebarToggledFlipsFlag` already proves; pick one. (P2, drop)
  - `NavigationReducerTests.ViewportTierEnumValuesArePinned` — covered implicitly by JS→C# wire format + snapshot test; could collapse into a single snapshot assertion. (P2, drop if redundant)
  - `FcHamburgerToggleTests.ViewportDrivenVisibilityDoesNotDispatchToggle` — negative test; keep. Don't drop.
  - `FcLayoutBreakpointWatcherTests.DispatchesOnSubsequentChange` — overlaps with `DispatchesInitialTierOnSubscribe` assertion surface. (P2, merge)
  - `NavigationEffectsScopeTests.HandleAppInitialized_ValidContextEmptyStorage_DoesNotDispatch` — symmetric with ThemeEffects existing coverage; optional. (P2, drop)
- **Recommended:** drop 3 P2 tests to land at **48 tests** (ratio 2.08, still above 1.8 band — escalate at PR review or tighten further) OR drop 6 P2 tests to land at **45** (ratio 1.96). Decision deferred to PR reviewer per Task 10.12.

### Red-Phase Compile Gate

Run locally to confirm red phase:

```bash
# .NET — should fail with compile errors referencing the new Navigation types
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj

# Playwright — should fail at selector timeout for the new data-testid markers
cd tests/e2e && npx playwright test specs/sidebar-responsive.spec.ts
```

After Task 1–9 implementation lands, both commands transition to green.

### CI Snapshot-Drift Gate (Task 10.13)

- `NavigationPersistenceSnapshotTests.BlobSchemaLocked.verified.txt` — baseline committed (line 1: `{"SidebarCollapsed":true,"CollapsedGroups":{"Counter":true,"Orders":false}}`). Any field addition/removal/rename produces a `.received.txt` diff → CI must hard-fail on unreviewed drift.
- `FrontComposerShellParameterSurfaceTests.Parameter_surface_matches_story_3_2_contract` — hardcoded array assertion; parameter surface drift produces an immediate xUnit failure (no snapshot file involved; drift is impossible to merge without editing this file).
- **Action for dev:** confirm `.github/workflows/*.yml` does NOT mark `Verify` tests as `continue-on-error: true`.

### Handoff Notes to Amelia / Dev Agent

1. **Red-phase state:** `dotnet build` will fail on the Shell.Tests project until Task 1 (enum + state record) lands. This is intentional. Do not weaken the tests to compile prematurely.
2. **Test seams on components:** tests reference `FcHamburgerToggle.IsVisibleForTest` / `OnHamburgerOpenedForTest` / `FrontComposerNavigation.OnGroupExpandedChangedForTest` — expose these as `internal` methods with `[InternalsVisibleTo("Hexalith.FrontComposer.Shell.Tests")]` in the Shell project (or collapse into `[Parameter]` callbacks if the existing patterns prefer that — `FcSystemThemeWatcher` uses `[JSInvokable]` public methods which is acceptable too).
3. **`NavigationEffectsInstrumentation` helper:** lives inside the test file; no source change needed. If the persist-trigger method names diverge from the conventions (`HandleSidebarToggled` / `HandleNavGroupToggled` / `HandleSidebarExpanded` / `HandlePersistNavigation`), update the reflection filter or add [Fact]s for each missing pattern.
4. **Verify snapshot baseline is authored as the EXPECTED final state.** On first run after Task 3.2 lands, Verify will accept the baseline because the `.verified.txt` was pre-committed. If the dev implements a different JSON property order, CI hard-fail signals the divergence — do NOT auto-accept; escalate to Murat.
5. **Playwright fixtures:** `sidebar-responsive.spec.ts` imports from `../fixtures/index.js` (uses `tenantTest + lifecycleTest` merge). The tenant fixture runs through the existing auth flow — no additional fixture changes needed.
6. **D10 parameter-count documentation bug:** the story says "parameter count 8 → 9" but the Story 3-1 baseline is 6 parameters (verified in git). Post-3-2 count = 7. The test encodes reality (7 entries). Flag this in the critical-decisions review if appearing in downstream stories.

### Definition-of-Done for This ATDD Artifact

- [x] Checklist frontmatter tracks all 4 steps
- [x] 11 new test files + 3 modifications written
- [x] Verify snapshot baseline committed
- [x] Red-phase mechanism documented per file
- [x] Budget reconciliation + trim candidates identified
- [x] Cross-story contracts referenced
- [x] Memory-aware assertions applied (fail-closed tenant + append-only parameter surface + viewport-not-persisted)
- [x] Handoff notes ready for Dev agent

### Red-Phase Verification (run at 2026-04-19)

```
$ dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj
    0 Warning(s)
    16 Error(s)
```

All 16 errors are compile errors referencing missing-by-design types:
- `namespace Hexalith.FrontComposer.Shell.State.Navigation` (to be created by Task 1)
- `FrontComposerNavigationState` / `ViewportTier` (Task 1)
- `NavigationEffects` (Task 3)
- `ViewportTier.Desktop` / `.Tablet` etc. in theory-attribute arguments (Task 1)

No errors in pre-existing test files. No warnings. Red phase is clean — ready for dev handoff.

