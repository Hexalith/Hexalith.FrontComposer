---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04-generate-tests']
lastStep: 'step-04-generate-tests'
lastSaved: '2026-04-19'
generationMode: 'ai-generation'
storyId: '3-3'
storyStatus: 'ready-for-dev'
inputDocuments:
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/index.md
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/story.md
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/acceptance-criteria.md
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/critical-decisions-read-first-do-not-revisit.md
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/architecture-decision-records.md
  - _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/tasks-subtasks.md
  - tests/e2e/playwright.config.ts
  - tests/e2e/specs/sidebar-responsive.spec.ts
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs
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

# ATDD Checklist — Story 3-3: Display Density & User Settings

## Step 1 — Preflight & Context

### Stack Detection
- **Detected:** `fullstack` (auto) — .NET 10 + Blazor Server density/settings under `src/Hexalith.FrontComposer.Shell/` + Playwright E2E under `tests/e2e/`.

### Prerequisites
- [x] Story approved — `ready-for-dev` per `sprint-status.yaml` (last updated 2026-04-19).
- [x] Clear AC — 7 ACs with Given/When/Then, test names, decision/task/ADR cross-refs.
- [x] Test framework configured — `tests/e2e/playwright.config.ts` + `tests/Hexalith.FrontComposer.Shell.Tests` (xUnit + bUnit + NSubstitute + Shouldly + Verify).
- [x] Dev env — `samples/Counter/Counter.Web` boots under Aspire.

### Story Context Summary
- **Goal:** Framework-owned settings surface that exposes the four-tier density precedence (user → deployment default → factory hybrid → per-component) via `FcSettingsDialog`, accessible through `FcSettingsButton` in `HeaderEnd` and the `Ctrl+,` shortcut, with `ViewportTier ≤ Tablet` forcing `Comfortable` (44 px touch targets).
- **Scope:** 7 ACs, 19 binding decisions, 3 ADRs (039–041), 11 tasks. Target ~31 new tests.
- **Non-trivial invariants (memory-aware):**
  - Fail-closed tenant scoping — HFC2105 log **AND** `storage.SetAsync` never called for null/empty/whitespace tenant or user (`feedback_tenant_isolation_fail_closed.md`).
  - Hydrate does NOT re-persist (D8, ADR-038 mirror).
  - `DensityHydratedAction` MUST NOT appear in any `[EffectMethod]` whose name starts with `HandlePersist` (reflective ADR-038 invariant — borrow from Story 3-2 NavigationEffectsScopeTests F4).
  - Reducers stay pure — resolver runs at action producer (D3); reducer assigns only.
  - `EffectiveDensity` is the single shell-level density value; `UserPreference` is preserved across viewport-forced overrides (ADR-040).
  - `--fc-density` / `data-fc-density` lives only in `FrontComposerShell.razor.css`, `fc-density.js`, `FcDensityApplier`, `FcDensityPreviewPanel` (ADR-041).
  - Resource parity EN ↔ FR auto-extends via `CanonicalKeysHaveFrenchCounterparts` (Story 3-1 pattern).
  - Parameter surface stays at 7 (D12 — auto-populate is render-time, NOT a new parameter).
  - Cross-feature read pattern: `DensityEffects` injects `IState<FrontComposerNavigationState>` (Story 3-2 contract).

### Cross-Story Contracts Produced by 3-3
- `DensityPrecedence.Resolve(...)` pure function → consumed by Stories 4-1 (DataGrid surface), 4-5 (DetailView), 6-5 (DevModeOverlay), 6-1 (annotation overrides).
- `DensitySurface` enum (ordinal-stable, all 6 values shipped now) → consumed by every downstream surface story.
- `FrontComposerDensityState(UserPreference, EffectiveDensity)` schema-locked via `DensityPersistenceSnapshotTests` → consumed by Story 3-6 session-resume.
- `FcShellOptions.DefaultDensity` (15th property) → consumed by adopters configuring deployment defaults.
- `HeaderEnd` auto-populate with `FcSettingsButton` (symmetric to Story 3-2 D8 `HeaderStart` auto-populate) → consumed by Counter.Web today + Story 3-4.
- `Ctrl+,` inline binding on `.fc-shell-root` → migrates to `IShortcutService` in Story 3-4 (same user-visible behaviour).
- `--fc-density` CSS custom property + `body[data-fc-density]` → consumed by every scoped CSS the framework emits.
- `IDialogService.ShowDialogAsync<FcSettingsDialog>` invocation contract → consumed by Story 3-4 command palette.

### Cross-Story Contracts Consumed by 3-3
- `ViewportTier` enum + `Navigation.ViewportTierChangedAction` (Story 3-2 ADR-036).
- `FrontComposerNavigationState.CurrentViewport` field via `IState<FrontComposerNavigationState>` (Story 3-2 ADR-037).
- `FcThemeToggle` component embedded verbatim in dialog (Story 3-1 D15 — DRY, no reimplementation).
- `IUlidFactory` (Story 2-3) for correlation IDs.
- `IUserContextAccessor` + `StorageKeys.BuildKey(...)` (Story 3-1 D8 / ADR-029).
- `HFC2105_StoragePersistenceSkipped` + `HFC2106_ThemeHydrationEmpty` (cross-feature reuse per Story 3-2 D12 / D19).

### Budget Check (memory-aware — `feedback_defense_budget.md`)
- **Binding decisions:** 19 (feature story — under the ≤25 cap).
- **Targeted tests:** ~36 (after generation; story spec budgets ~31). Decision-to-test ratio **1.89** — slightly above Murat's 1.5–1.8 band.
- **Disposition:** 4 of the extra tests are mandatory mirror-pattern tests for Story 3-2 ADR-038 reflective invariants (no `HandlePersist*` accepts `DensityHydratedAction`) and `FrontComposerShellParameterSurfaceTests` re-run guard. The remaining excess is two `[Theory]` rows for the precedence resolver's matrix; PR-review may trim them if the invariant feels redundant. Memory L08 trim path documented at PR-review gate (Task 10.13).

### Existing Test Patterns to Mirror
| Concern | Reference file | Pattern |
|---|---|---|
| Fluxor effect scope guard | `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs` | NSubstitute `IStorageService.DidNotReceiveWithAnyArgs().SetAsync(...)` + `AssertLoggedInformation(logger, diagnosticId)` helper for `ILogger.Log` |
| Cross-feature `IState<T>` injection in effect | `NavigationEffects` consumes `IState<FrontComposerNavigationState>` for viewport read | `FakeState(value)` substitute helper |
| `[EffectMethod]` reflective invariant | `NavigationEffectsScopeTests.NoEffectMethodAcceptsViewportTierChangedAction` | `Reflection over GetMethods(Public)` filtering by `EffectMethodAttribute` + first-parameter type |
| Verify snapshot wire-format lock | `NavigationPersistenceSnapshotTests` + `.verified.txt` | `VerifyXunit.Verifier.Verify(JsonSerializer.Serialize(blob))` baseline |
| bUnit JS module mock | `LayoutComponentTestBase.JSInterop.SetupModule(modulePath)` + `SetupVoid(name).SetVoidResult()` | Loose mode + per-test module setup |
| bUnit Fluxor state read | `EnsureStoreInitialized()` + `Services.GetRequiredService<IState<TState>>()` | Lazy store init after `Services.Replace` calls |
| Parameter surface snapshot | `FrontComposerShellParameterSurfaceTests` | Reflection enumerates `[Parameter]` props ordered by `MetadataToken` |
| Resource parity / round-trip | `FcShellResourcesTests.NavigationStaticKeysResolveInBothLocales` | `[Theory]` over `(key, en, fr)` triples; round-trip check `localizer[key].ResourceNotFound.ShouldBeFalse()` |
| E2E sidebar smoke | `tests/e2e/specs/sidebar-responsive.spec.ts` | Tenant fixture + `ShellPage` POM + `resizeTo(width)` |

### TEA Config Flags (from `_bmad/tea/config.yaml`)
- `tea_use_playwright_utils: true` — existing E2E uses traditional POM; we keep alignment, defer playwright-utils introduction to a later refactor.
- `tea_use_pactjs_utils: false`
- `tea_pact_mcp: none`
- `tea_browser_automation: auto`
- `risk_threshold: p1`

### Knowledge Fragments Loaded
See YAML frontmatter `knowledgeFragmentsLoaded`.

### Inputs Confirmed
- Ready to generate failing acceptance tests in Step 2.

---

## Step 2 — Generation Mode

**Mode:** `ai-generation`

**Rationale:**
- Components under test (`DensityPrecedence`, `FcDensityApplier`, `FcSettingsButton`, `FcSettingsDialog`, `FcDensityPreviewPanel`, expanded `DensityEffects`, expanded `FrontComposerDensityState`) are not yet implemented — no DOM to record against.
- ACs are fully spec'd by 19 binding decisions + 3 ADRs; precedence resolver, action shapes, persistence schema, ARIA labels, dialog layout, and resource keys are all pinned in `critical-decisions-read-first-do-not-revisit.md` + `architecture-decision-records.md`.
- Majority of the ~36 tests are pure xUnit (precedence resolver) + bUnit (component) + Verify (snapshot) — no browser runtime.
- Only Playwright E2E (Task 10.12) needs a live browser, and its DOM targets (`document.body.dataset.fcDensity` + the `FcSettingsButton` selector) are dictated by the spec we're about to implement.

**Recording deferred to:** future test-healing / refactor passes where a live DOM can be snapshotted (Story 10-2 axe + screenshot diff for density specimens).

---

## Step 3 — Test Strategy

### Priority Rubric (project-specific, risk-weighted)
- **P0** — Tenant isolation, wire-format snapshots, parameter surface, accessibility-floor invariants (viewport-forced Comfortable), single-source `--fc-density` invariant, `DensityHydratedAction` reflective check. Failing any of these breaks tenant safety, a cross-story contract, or a regulatory-adjacent UX claim. Gate-blocking on every PR.
- **P1** — Precedence resolver matrix, reducer round-trip per action, viewport-handler effect dispatch, resource EN/FR parity, settings dialog DOM contract, button auto-populate, E2E density transition. Impacts UX quality but not tenant safety. Gate-blocking on main-branch CI.
- **P2** — Roomy-specific spacing assertions (covered implicitly by precedence theory rows), reset-to-defaults dual-dispatch ordering, idempotency edge cases. Edge cases; run on PR but tolerate a single retry.

### AC → Test Strategy Matrix

| AC | Scenario | Level | Priority | Test Case | Risk Justification |
|---|---|---|---|---|---|
| **AC1** | Precedence: `(null, null, Default, Desktop)` → `Comfortable` | Unit (theory) | P1 | `DensityPrecedenceTests.Resolve_AllCombinations` row 1 | Factory hybrid baseline. |
| **AC1** | Precedence: `(Compact, null, Default, Desktop)` → `Compact` | Unit (theory) | P1 | row 2 | User preference wins over factory. |
| **AC1** | Precedence: `(null, Roomy, Default, Desktop)` → `Roomy` | Unit (theory) | P1 | row 3 | Deployment default wins over factory. |
| **AC1** | Precedence: `(null, null, DataGrid, Desktop)` → `Compact` | Unit (theory) | P1 | row 4 | Surface-specific factory hybrid (ADR-039 verification). |
| **AC1** | Precedence: `(null, Compact, NavigationSidebar, Desktop)` → `Compact` | Unit (theory) | P1 | row 5 | Deployment default beats factory hybrid for nav surface. |
| **AC1** | Precedence: `(Compact, null, Default, Tablet)` → `Comfortable` | Unit (theory) | P0 | row 6 | **Tier-force override** at tier 1 — accessibility floor. |
| **AC1** | Reducer assigns BOTH `UserPreference` and `EffectiveDensity` from action payload | Unit | P1 | `DensityReducersTests.UserPreferenceChanged_AssignsBothFields` | Validates D3 — reducer is pure assign. |
| **AC2** | Settings dialog opens at 480px modal width | Component (bUnit) | P1 | `FcSettingsDialogTests.RendersDensityRadioThemeAndPreview` | Dialog DOM contract. |
| **AC2** | Dialog body contains density radio + embedded `FcThemeToggle` + preview panel | Component | P1 | same | Layout integrity (D13 + D15). |
| **AC2** | Density radio change dispatches `UserPreferenceChangedAction` with computed `NewEffective` | Component | P1 | `FcSettingsDialogTests.DensityRadioSelectionDispatchesAction` | Action payload correctness; resolver runs at producer (D3). |
| **AC2** | Reset-to-defaults dispatches `UserPreferenceClearedAction` + `ThemeChangedAction(System)` | Component | P2 | `FcSettingsDialogTests.ResetToDefaultsDispatchesClearedAndThemeSystem` | Two-action footer button contract. |
| **AC2 + AC4** | Forced-by-viewport note renders at Tablet when user prefers Compact | Component | P0 | `FcSettingsDialogTests.RendersForcedDensityNoteAtTabletWhenUserPrefersCompact` | UX spec — user must see why their choice is overridden (ADR-040). |
| **AC2 + AC4** | Forced-by-viewport note absent at Desktop | Component | P1 | `FcSettingsDialogTests.NoForcedNoteAtDesktop` | Negative case — no false positive. |
| **AC3** | Persists on valid scope under `{tenantId}:{userId}:density` | Effect | P0 | `DensityEffectsScopeTests.PersistsOnValidScope_UserPreferenceChanged` | Wire-format: storage key format is cross-story contract (3-6). |
| **AC3** | Null tenant → log HFC2105 **AND** `storage.SetAsync` NEVER called (UserPreferenceChanged) | Effect | P0 | `DensityEffectsScopeTests.SkipsOnNullTenant_UserPreferenceChanged` | **Tenant isolation** memory `feedback_tenant_isolation_fail_closed.md`. |
| **AC3** | Null user → log + no storage call (UserPreferenceChanged) | Effect | P0 | `DensityEffectsScopeTests.SkipsOnNullUser_UserPreferenceChanged` | Symmetric. |
| **AC3** | Whitespace tenant/user → log + no storage call (UserPreferenceChanged) | Effect | P0 | `DensityEffectsScopeTests.SkipsOnWhitespaceUserContext_UserPreferenceChanged` | Whitespace-is-empty invariant explicit. |
| **AC3** | UserPreferenceCleared writes literal `null` JSON | Effect | P1 | `DensityEffectsScopeTests.PersistsNullOnUserPreferenceCleared` | Reset path persists clearing. |
| **AC3** | `DensityHydratedAction` does NOT trigger re-persist (ADR-038 mirror) | Effect | P0 | `DensityEffectsScopeTests.HydrateDoesNotRePersist` | Story 3-2 ADR-038 cross-feature parity. |
| **AC3** | No `HandlePersist*` `[EffectMethod]` accepts `DensityHydratedAction` | Reflection | P0 | `DensityEffectsScopeTests.NoHandlePersistEffectMethodAcceptsDensityHydratedAction` | Reflective ADR-038 invariant — name-pattern resilient. |
| **AC3** | Hydrate dispatches `DensityHydratedAction(stored, resolved)` | Effect | P1 | `DensityEffectsTests.HandleAppInitialized_StorageContainsValue_DispatchesDensityHydrated` | Hydrate signal contract. |
| **AC3 + AC5** | Persistence blob: `DensityLevel?` non-null serialises as enum | Snapshot | P0 | `DensityPersistenceSnapshotTests.SerialisedNonNull_LocksSchema` | Wire format pinned — Story 3-6 contract. |
| **AC3 + AC5** | Persistence blob: `DensityLevel?` null serialises as JSON `null` | Snapshot | P0 | `DensityPersistenceSnapshotTests.SerialisedNull_LocksSchema` | Reset-to-defaults wire format. |
| **AC4** | `Resolve(Compact, _, _, Tablet)` → `Comfortable` (already in AC1 matrix) | Unit | P0 | `DensityPrecedenceTests.Resolve_AllCombinations` row 6 | (cross-listed) |
| **AC4** | Reducer: `EffectiveDensityRecomputedAction` updates only `EffectiveDensity` | Unit | P1 | `DensityReducersTests.EffectiveDensityRecomputed_PreservesUserPreference` | UserPreference preserved across viewport changes. |
| **AC4** | `DensityEffects.HandleViewportTierChanged` re-resolves and dispatches `EffectiveDensityRecomputedAction` when changed | Effect | P1 | `DensityEffectsTests.HandleViewportTierChanged_DispatchesEffectiveDensityRecomputed` | Cross-feature handler contract (D7). |
| **AC4** | `HandleViewportTierChanged` does NOT call storage | Effect | P0 | `DensityEffectsScopeTests.ViewportTierChangedDoesNotPersist` | Performance + viewport-not-persisted invariant (D8). |
| **AC4** | No `[EffectMethod]` named `HandlePersist*` accepts `ViewportTierChangedAction` | Reflection | P0 | `DensityEffectsScopeTests.NoHandlePersistEffectMethodAcceptsViewportTierChangedAction` | ADR-037 mirror — viewport never persisted. |
| **AC5** | Roomy round-trips Reducer + Persistence + Preview | Snapshot+Component | P1 | `DensityPersistenceSnapshotTests.SerialisedNonNull_LocksSchema` (Roomy) + `FcDensityPreviewPanelTests.RendersAtRequestedDensity` (Roomy theory row) | Roomy-equality verification (UX-DR27 / D2). |
| **AC6** | `FcDensityApplier` invokes `setDensity` on first render | Component | P1 | `FcDensityApplierTests.InvokesSetDensityOnInitialRender` | First-paint correctness. |
| **AC6** | `FcDensityApplier` invokes `setDensity` on subsequent state changes | Component | P1 | `FcDensityApplierTests.InvokesSetDensityOnStateChange` | Subscription contract. |
| **AC6** | `FcDensityApplier` releases JS module on dispose | Component | P2 | `FcDensityApplierTests.DisposesCleanly` | Lifecycle hygiene. |
| **AC6** | `--fc-density` / `data-fc-density` lint — single-source invariant | Lint (xUnit + file scan) | P0 | `DensityNoPerComponentLogicLintTest.SearchesSrcForRogueDensityVars` | ADR-041 — no per-component density logic. |
| **AC7** | Settings button auto-populates `HeaderEnd` when null | Component | P0 | `FrontComposerShellTests.AutoRendersSettingsButtonWhenHeaderEndIsNull` | Auto-populate contract (D12). |
| **AC7** | Adopter `HeaderEnd` fragment wins | Component | P0 | `FrontComposerShellTests.AdopterSuppliedHeaderEndWins` | Override escape hatch. |
| **AC7** | Click on settings button opens `FcSettingsDialog` via `IDialogService` | Component | P1 | `FcSettingsButtonTests.ClickOpensSettingsDialog` | Dialog invocation contract (D11). |
| **AC7** | Settings button renders Fluent Settings icon + aria-label | Component | P1 | `FcSettingsButtonTests.RendersFluentButtonWithSettingsIconAndAriaLabel` | Visual + accessibility contract. |
| **AC7** | Ctrl+, opens settings dialog | Component | P1 | `FrontComposerShellTests.CtrlCommaOpensSettingsDialog` | D16 — inline shortcut contract. |
| **AC7** | `FrontComposerShell` parameter surface stays at 7 | Snapshot | P0 | `FrontComposerShellParameterSurfaceTests.Parameter_surface_matches_story_3_2_contract` | D12 — auto-populate is render-time, NOT a new parameter. |
| **AC2/AC4/AC7** | 11 new EN+FR resource keys round-trip | Resource | P1 | `FcShellResourcesTests.SettingsDialogStaticKeysResolveInBothLocales` (`[Theory]`) | L10n contract (D17). |
| **AC1+AC4+AC6+AC7** | Counter.Web density transition on resize: Compact → 800 px → Comfortable; → 1920 px → Compact | E2E (Playwright) | P1 | `density-transition.spec.ts` — `density auto-switches at Tablet boundary preserving user preference` | Live integration smoke. |
| **AC2+AC7** | Counter.Web settings button visible + dialog opens via click | E2E | P1 | `density-transition.spec.ts` — `settings button opens dialog at desktop` | DOM smoke against real Blazor Server. |
| **AC1** | `DensityPrecedence` documents per-component tier 5 in XML — **NOT** an automated test (manual code-review check) | (manual) | n/a | n/a | Documentation invariant per D1; reviewer reads `.cs` XML. |

### Level Distribution
- **Unit (pure xUnit):** 11 tests — precedence resolver theory (6 rows), reducers (5 facts) + DensityFeatureTests update (1) — **12 total**.
- **Component (bUnit):** 16 tests — `FcDensityApplier` (3), `FcSettingsButton` (2), `FcSettingsDialog` (5), `FcDensityPreviewPanel` (3 theory rows), `FrontComposerShell` adds (3).
- **Effect / Integration:** 11 tests — `DensityEffectsScopeTests` (8: persist-on-valid, null tenant, null user, whitespace [Theory] x2, persist null on cleared, hydrate-no-repersist, viewport-no-persist) + `DensityEffectsTests` (3: viewport handler dispatches, hydrate dispatches, options-driven resolver respects DefaultDensity).
- **Reflection invariants:** 2 tests — no `HandlePersist*` accepts `DensityHydratedAction` / `ViewportTierChangedAction` (ADR-038 / ADR-037 mirror).
- **Lint:** 1 test — `--fc-density` / `data-fc-density` outside approved files.
- **Snapshot (Verify):** 2 tests + 2 baselines — `DensityPersistenceSnapshotTests`.
- **Resource:** 1 added test method (`[Theory]` over 11 keys in EN+FR — counted as 1 test method that exercises 22 lookups).
- **Options:** 1 added test — `FcShellOptionsValidationTests.DefaultDensity_NullablePropertyExists`.
- **E2E (Playwright):** 2 specs in 1 file (`density-transition.spec.ts`).

**Total: ~36 tests** (after counting reducers + DensityEffectsTests rewrites; 19 decisions × 1.89 ratio — slightly above Murat's 1.5–1.8 band, see Budget Check above).

### Priority Distribution
- **P0 (gate-blocking / every commit):** 12 tests — tenant isolation, wire-format snapshots, parameter surface, viewport-forced Comfortable, lint single-source, ADR-038/037 reflection invariants, auto-populate, adopter override.
- **P1 (main-branch CI):** 18 tests — precedence resolver matrix, reducer assignments, viewport-handler dispatch, dialog layout, button click, ctrl+comma, resource parity, applier interop, E2E.
- **P2 (PR with one retry):** 6 tests — Roomy-specific renders, applier dispose, reset-button dual dispatch, idempotency rows in precedence theory.

### Red-Phase Requirements

Every test below must fail before Tasks 1–9 implementation. Verification strategy:

| Red-phase mechanism | Applies to |
|---|---|
| Test references type / member that doesn't exist yet (`DensityPrecedence`, `DensitySurface`, `UserPreferenceChangedAction`, `UserPreferenceClearedAction`, `DensityHydratedAction`, `EffectiveDensityRecomputedAction`, `FcDensityApplier`, `FcSettingsButton`, `FcSettingsDialog`, `FcDensityPreviewPanel`, `FcShellOptions.DefaultDensity`, `FrontComposerDensityState.UserPreference / .EffectiveDensity`) → **compile error** | All unit + component + integration + snapshot tests |
| Existing tests asserting `state.CurrentDensity` updated to `state.EffectiveDensity` → compile error until Task 2.1 renames the property | `DensityReducersTests`, `DensityFeatureTests`, `DensityEffectsTests` (rewritten) |
| `Verify` snapshot baselines pre-committed (`SerialisedNonNull.verified.txt` + `SerialisedNull.verified.txt`) → snapshot mismatch on first run if dev introduces a different JSON shape | `DensityPersistenceSnapshotTests` |
| Resource key missing in resx → `localizer["SettingsDialogTitle"].ResourceNotFound == true` | `FcShellResourcesTests.SettingsDialogStaticKeysResolveInBothLocales` |
| Lint scan finds `--fc-density` / `data-fc-density` in approved files only → fails today (zero approved files exist), passes after Task 4 lands them | `DensityNoPerComponentLogicLintTest` |
| Playwright spec selector `[data-testid="fc-settings-button"]` and assertion `document.body.dataset.fcDensity` → timeout / null until Tasks 4 + 5 land the markers | `density-transition.spec.ts` |
| Reflection check `[EffectMethod]` first-parameter type filter — fails today with no `DensityEffects` exposing the new action types (compile error) | `DensityEffectsScopeTests.NoHandlePersist*` |

**CI enforcement:** the `dotnet test` task in Task 11.2 runs the suite; red-phase gate is that after checkout + `dotnet build` at Step 4 output, every new test in this checklist is authored and `dotnet test --filter "FullyQualifiedName~Density"` reports non-zero failures. Once implementation lands, suite transitions to green. Task 10.13 PR-review gate confirms test-count delta ≈ +31 (`test_baseline_pre_3_3` captured in Task 0.1).

---

## Step 4 — Generated FAILING Test Suite (TDD Red Phase)

### Generation mode
- Resolved: `sequential` (inline generation — subagent/agent-team not dispatched for .NET stack; test file authoring happens in the main thread since the spec already carries all information needed).
- TDD phase: **RED** — every new test file compiles against types / components / actions / parameters that do not yet exist, asserts DOM / resource keys / persistence-blob shapes that are not yet emitted, or executes a reflection scan that has nothing to find. First `dotnet test` + first `npx playwright test` will fail loudly. That is the intended signal.

### Files Created (10)

| # | File | Tests | AC(s) | Task(s) | Red-phase mechanism |
|---|---|---|---|---|---|
| 1 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs` | 6 (theory) | AC1, AC4, AC5 | 10.1 | Compile error — types `DensityPrecedence`, `DensitySurface` not yet present; `ViewportTier` already exists |
| 2 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPersistenceSnapshotTests.cs` | 2 | AC3, AC5 | 10.5a | Compile error on `JsonSerializer.Serialize<DensityLevel?>(...)` is fine (works today), but Verify baseline lock pins schema for downstream stories |
| 3 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPersistenceSnapshotTests.SerialisedNonNull_LocksSchema.verified.txt` | — | AC3, AC5 | 10.5a | Baseline snapshot — `.received.txt` diff on any future field add |
| 4 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPersistenceSnapshotTests.SerialisedNull_LocksSchema.verified.txt` | — | AC3 | 10.5a | Baseline snapshot — locks `null` JSON literal for clear-path |
| 5 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityNoPerComponentLogicLintTest.cs` | 1 | AC6 | 10.6a | Fails today (no approved files exist); passes once Task 4 + Task 7 land approved files |
| 6 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityApplierTests.cs` | 3 | AC6 | 10.6 | Compile error on `FcDensityApplier` |
| 7 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsButtonTests.cs` | 2 | AC7 | 10.7 | Compile error on `FcSettingsButton` |
| 8 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs` | 5 | AC2, AC4, AC5 | 10.8 | Compile error on `FcSettingsDialog`, `UserPreferenceChangedAction`, `UserPreferenceClearedAction` |
| 9 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs` | 3 (theory) | AC2, AC5, AC6 | 10.9 | Compile error on `FcDensityPreviewPanel` |
| 10 | `tests/e2e/specs/density-transition.spec.ts` | 2 | AC1, AC2, AC4, AC6, AC7 | 10.12 | Selector + DOM-attribute assertions miss until Tasks 4+5 land the body attribute and the settings button |

### Files Modified (7)

| # | File | Change | Red-phase mechanism |
|---|---|---|---|
| 11 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityReducersTests.cs` | Replaced single `ReduceDensityChanged_AllDensityLevels_UpdatesState` with 5 focused tests covering the new actions. Asserts `state.EffectiveDensity` (Story 3-1's `CurrentDensity` is renamed per D2) and `state.UserPreference`. | Compile errors: `state.EffectiveDensity` / `state.UserPreference` properties don't exist; new action types don't exist |
| 12 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityFeatureTests.cs` | Updated to assert new initial state shape `(UserPreference: null, EffectiveDensity: Comfortable)`. | Compile error on positional record fields |
| 13 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs` | Replaced legacy `DensityChangedAction` persist tests with `UserPreferenceChangedAction` persist + `HandleViewportTierChanged` dispatch + `HandleAppInitialized` dispatches `DensityHydratedAction` rather than `DensityChangedAction`. | Compile errors on new action types + `DensityEffects` constructor signature change (adds `IState<FrontComposerNavigationState>` + `IOptions<FcShellOptions>`) |
| 14 | `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs` | Adds 5 tests + 2 reflective tests: persist-on-valid (`UserPreferenceChanged`), null tenant skips (`UserPreferenceChanged`), null user skips, whitespace skips, persist null on `UserPreferenceCleared`, hydrate does not re-persist, viewport handler does not persist + 2 reflection invariants (no `HandlePersist*` accepts `DensityHydratedAction` / `ViewportTierChangedAction`). | Compile errors on new action + new constructor signature; reflection scans pass after impl |
| 15 | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` | +3 tests: `AutoRendersSettingsButtonWhenHeaderEndIsNull`, `AdopterSuppliedHeaderEndWins`, `CtrlCommaOpensSettingsDialog` (uses bUnit `Trigger.KeyDownAsync` on `.fc-shell-root`). | `FindComponent<FcSettingsButton>` throws until Task 5 lands the auto-populate; Ctrl+, handler missing until Task 8 |
| 16 | `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs` | +1 `[Theory]` test asserting `DefaultDensity` property exists, accepts `null` and all three `DensityLevel` values. | Compile error on `FcShellOptions.DefaultDensity` until Task 1.1 |
| 17 | `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` | +1 `[Theory]` test method covering 11 new keys × EN/FR (22 lookups). | `localizer[key].ResourceNotFound == true` until Task 9.1 / 9.2 add keys |

### Test Count (Red Phase)

| File | Test count |
|---|---|
| DensityPrecedenceTests | 6 |
| DensityPersistenceSnapshotTests | 2 |
| DensityNoPerComponentLogicLintTest | 1 |
| FcDensityApplierTests | 3 |
| FcSettingsButtonTests | 2 |
| FcSettingsDialogTests | 5 |
| FcDensityPreviewPanelTests | 3 |
| DensityReducersTests (modified) | 5 |
| DensityFeatureTests (modified) | 1 (unchanged count, updated assertions) |
| DensityEffectsTests (modified) | 3 |
| DensityEffectsScopeTests (modified, +5 new + 2 reflection) | 9 |
| FrontComposerShellTests (added) | +3 |
| FcShellOptionsValidationTests (added) | +1 |
| FcShellResourcesTests (added) | +1 (`[Theory]` with 22 row lookups counted as 1 method) |
| **Total .NET tests** | **45 `[Fact]` / `[Theory]` declarations** |
| Playwright specs | 2 |
| **Grand total** | **~47 tests** |

### Budget Reconciliation vs Story 3-3 Target
- **Story target:** ~31 tests (Task 10 sum); PR-review band 1.5–1.8× decisions (19) = 28.5 – 34.2 tests.
- **Generated:** ~47 tests (ratio 2.47 — above the upper band per Task 10.13).
- **Trim candidates (to land near 35):**
  - `DensityPrecedenceTests` row 5 (deployment-default beats factory hybrid for `NavigationSidebar`) — overlaps with row 3 since both encode "deployment default beats factory hybrid". (P1, drop one)
  - `FcDensityApplierTests.DisposesCleanly` — covered implicitly by the `IAsyncDisposable` contract test in `LayoutComponentTestBase` shared setup. (P2, drop)
  - `DensityEffectsScopeTests.NoHandlePersistEffectMethodAcceptsViewportTierChangedAction` — complements `ViewportTierChangedDoesNotPersist` (behavioural). The reflective check is more resilient but more expensive; pick one. (P0, keep — ADR-037 reflective parity with Story 3-2)
  - `FcSettingsDialogTests.NoForcedNoteAtDesktop` — covered indirectly by other Desktop-tier tests. (P2, drop)
- **Recommended:** drop the 2 P2 tests above to land at **45 tests** (ratio 2.37, still above 1.8 band). Decision deferred to PR reviewer per Task 10.13. The 4 reflection-style tests (lint, ADR-038, ADR-037, parameter surface) are non-negotiable per the memory invariants list — they are the cross-story contract guards.

### Red-Phase Compile Gate

Run locally to confirm red phase:

```bash
# .NET — should fail with compile errors referencing the new Density types
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj

# Playwright — should fail at selector timeout / DOM-attribute assertion miss
cd tests/e2e && npx playwright test specs/density-transition.spec.ts
```

After Tasks 1–9 implementation lands, both commands transition to green.

### CI Snapshot-Drift Gate (Task 10.13 mirror)

- `DensityPersistenceSnapshotTests.SerialisedNonNull_LocksSchema.verified.txt` — baseline committed (line 1: `"Roomy"` — `JsonSerializer.Serialize<DensityLevel?>(DensityLevel.Roomy)` returns the enum name as a JSON string under default `JsonSerializerOptions`; if dev introduces an enum-as-int converter, snapshot drifts and CI hard-fails).
- `DensityPersistenceSnapshotTests.SerialisedNull_LocksSchema.verified.txt` — baseline committed (line 1: `null`).
- `FrontComposerShellParameterSurfaceTests` — confirmed already-locked at 7 parameters (D12 — auto-populate is render-time, NOT a new parameter); no edit required to that test file.
- **Action for dev:** confirm `.github/workflows/*.yml` does NOT mark `Verify` tests as `continue-on-error: true`.

### Handoff Notes to Amelia / Dev Agent

1. **Red-phase state:** `dotnet build` will fail on the Shell.Tests project until Task 1 (enum + resolver) and Task 2 (state + actions) land. This is intentional. Do not weaken the tests to compile prematurely.
2. **State-shape rename:** Story 3-3 D2 renames `FrontComposerDensityState.CurrentDensity` → `EffectiveDensity` and adds `UserPreference`. The pre-existing tests (`DensityReducersTests`, `DensityFeatureTests`, `DensityEffectsTests`) are rewritten to match — they will compile-fail until Task 2.1 lands the state record change. Task 2.5's "any existing references update to EffectiveDensity" includes these tests.
3. **`DensityEffects` constructor signature change:** Task 3.1 adds `IState<FrontComposerNavigationState>` + `IOptions<FcShellOptions>` to the constructor. The rewritten `DensityEffectsScopeTests` instantiate with the new signature (using NSubstitute's `FakeState` helper borrowed from `NavigationEffectsScopeTests`).
4. **Test seam on `FcSettingsButton`:** the test injects an NSubstitute `IDialogService` and asserts `dialogService.Received(1).ShowDialogAsync<FcSettingsDialog>(Arg.Any<DialogParameters>())`. This requires `FcSettingsButton` to take `IDialogService` via `[Inject]` — not via constructor injection.
5. **bUnit JS module mock for `FcDensityApplier`:** the test sets up `JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-density.js")` and `module.SetupVoid("setDensity", _ => true).SetVoidResult()`. Mirror of `FcLayoutBreakpointWatcherTests.ImportsModuleOnFirstRender`.
6. **Lint test scope:** `DensityNoPerComponentLogicLintTest` greps `src/Hexalith.FrontComposer.Shell/` for `--fc-density` and `data-fc-density`. Approved files (allowed to contain those strings): `Components/Layout/FrontComposerShell.razor.css`, `Components/Layout/FcDensityApplier.razor` + `.cs`, `Components/Layout/FcDensityPreviewPanel.razor` + `.cs`, `wwwroot/js/fc-density.js`. Any other file containing the literals → test fails. The dev agent should confirm scoped CSS sources do NOT inline `data-fc-density="..."` selectors elsewhere.
7. **Verify snapshot baseline is authored as the EXPECTED final state.** On first run after Task 3 lands, Verify will accept the baseline because the `.verified.txt` was pre-committed. If the dev implements a different JSON serialisation (e.g., enum-as-int), CI hard-fail signals the divergence — do NOT auto-accept; escalate to Murat.
8. **E2E spec uses tenant fixture:** `density-transition.spec.ts` imports from `../fixtures/index.js` (uses `tenantTest + lifecycleTest` merge). The tenant fixture runs through the existing auth flow — without it the density persist effect fail-closes (AC3 scope guard) and the test would pass for the wrong reason.
9. **Cross-feature reducer concern:** Story 3-3 D7 routes `Navigation.ViewportTierChangedAction` into a `DensityEffects.HandleViewportTierChanged` cross-feature handler that re-resolves and dispatches an intra-feature `EffectiveDensityRecomputedAction`. The Density REDUCER never reads `Navigation` state — purity preserved (D3).
10. **`FcSettingsDialogTests` resolver call site:** the dialog calls `DensityPrecedence.Resolve(...)` from its setter to compute `NewEffective` BEFORE dispatching. The test asserts the dispatched action carries `NewEffective = DensityLevel.Comfortable` for a Tablet-viewport scenario where the user picked Compact (tier-force activates).

### Definition-of-Done for This ATDD Artifact

- [x] Checklist frontmatter tracks all 4 steps
- [x] 10 new test files + 7 modifications written
- [x] 2 Verify snapshot baselines committed
- [x] Red-phase mechanism documented per file
- [x] Budget reconciliation + trim candidates identified
- [x] Cross-story contracts (produced + consumed) referenced
- [x] Memory-aware assertions applied (fail-closed tenant + ADR-038/037 reflection invariants + single-source CSS lint + parameter-surface lock)
- [x] Handoff notes ready for Dev agent

### Red-Phase Verification (run at 2026-04-19)

Observed build state on a clean rebuild after committing this artifact's test files:

```
$ dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-incremental
    0 Warning(s)
    7 Error(s)  ← Roslyn surfaces only DensityPrecedenceTests.cs errors first; see note
```

**Roslyn first-pass error budget note:** when the `[InlineData(..., DensitySurface.Default, ...)]` attribute arguments in `DensityPrecedenceTests.cs` reference a type that does not exist, Roslyn aborts the semantic pass early and reports only the 7 `CS0103` / `CS0246` errors against that file. Removing `DensityPrecedenceTests.cs` from the project temporarily exposes the full red-phase error wave: **176 compile errors** across the other test files, all referencing missing-by-design types — exactly the intended signal:

- `DensityPrecedence` / `DensitySurface` (Tasks 1.2, 1.3)
- `UserPreferenceChangedAction`, `UserPreferenceClearedAction`, `DensityHydratedAction`, `EffectiveDensityRecomputedAction` (Task 2.3)
- `FrontComposerDensityState.UserPreference`, `.EffectiveDensity` (Task 2.1)
- `FcShellOptions.DefaultDensity` (Task 1.1)
- `FcDensityApplier`, `FcSettingsButton`, `FcSettingsDialog`, `FcDensityPreviewPanel` (Tasks 4.2, 5.1, 6.1, 7.1)
- `DensityEffects` constructor expanded with `IState<FrontComposerNavigationState>` + `IOptions<FcShellOptions>` (Task 3.1)
- `DensityReducers.ReduceUserPreferenceChanged` / `.ReduceUserPreferenceCleared` / `.ReduceDensityHydrated` / `.ReduceEffectiveDensityRecomputed` (Task 2.4)
- `DensityEffects.HandleUserPreferenceChanged` / `.HandleUserPreferenceCleared` / `.HandleViewportTierChanged` (Tasks 3.2, 3.4)

No errors expected in pre-existing test files unrelated to Density. No warnings introduced. Red phase is clean — ready for dev handoff once the implementation tasks begin.

**Verification commands for the dev agent:**

```bash
# After Task 1 lands DensitySurface, the first error wave clears and the next 169 errors surface.
# After Task 2 lands the new actions + state shape, ~120 errors clear.
# After Tasks 3 / 4 / 5 / 6 / 7 land the effects + components, the suite turns green.
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-incremental
dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~Density"
```
