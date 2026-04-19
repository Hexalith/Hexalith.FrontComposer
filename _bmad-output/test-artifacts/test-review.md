---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-review-quality', 'step-04-fixes-applied']
lastStep: 'step-04-fixes-applied'
lastSaved: '2026-04-19'
reviewer: 'Murat (TEA)'
scope: 'Full Shell.Tests suite + e2e sidebar (focused on Story 3-2 ATDD red-phase cluster)'
storyId: '3-2'
storyStatus: 'ready-for-dev (ATDD red-phase)'
stackType: 'fullstack (xUnit + bUnit + Playwright)'
inputDocuments:
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/acceptance-criteria.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/critical-decisions-read-first-do-not-revisit.md
  - _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/dev-agent-record.md
  - _bmad-output/test-artifacts/atdd-checklist-3-2.md
  - tests/e2e/playwright.config.ts
  - tests/e2e/specs/sidebar-responsive.spec.ts
  - tests/e2e/page-objects/shell.page.ts
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducerTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Integration/CounterWebIntegrationTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/selector-resilience.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/timing-debugging.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-healing-patterns.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/selective-testing.md
---

# Test Quality Review — Story 3-2 Cluster + Shell.Tests Suite

**Reviewer:** Murat (TEA)
**Date:** 2026-04-19
**Review mode:** Create (full)
**Context:** Story 3-2 is `ready-for-dev`; the test files under review are **ATDD red-phase** — designed to fail at compile/assertion until Tasks 1–9 land. Quality criteria are the same, but "test doesn't currently pass" is **not** a finding.

---

## Executive Summary

**Verdict:** Strong ATDD scaffolding with sharp AC→test→decision traceability, but five issues need correction before the red phase can be trusted as a gate. Two are **critical** (hard wait, invented contract), three are **high** (broken negative assertion, reflection hack with dead assertion, hidden test-id contract). The suite otherwise shows mature patterns: typed component finds, fail-closed tenant scoping, parameter-surface locks, snapshot wire-format anchoring, and bilingual resource parity.

**Risk signal:** The tests claim coverage they don't actually deliver for three scenarios (rail mode, sidebar dedup, ADR-037/038 guard). If the dev agent makes these pass without fixing the tests, the story ships with false confidence on AC4 and the persistence ordering invariant.

**Counts in scope (new / modified for Story 3-2):**
- C# new: 8 files, ~51 test methods (across Theory expansions ~62 executions)
- C# modified: 3 files, +6 test methods for 3-2
- E2E new: 1 spec (3 tests) + 1 page object
- Red-phase tests (will fail until dev): 100% of new; targeted subset of modified

**Go/No-Go recommendation for entering dev (green phase):** **Go, with mandatory pre-dev fixes F1 + F2** (see "Must-fix before dev" below). F3–F5 can be fixed during dev without blocking.

---

## Findings — Severity-Ordered

### 🚨 CRITICAL

#### F1. Hard wait in `shell.page.ts` violates deterministic-wait DoD
**File:** `tests/e2e/page-objects/shell.page.ts:42`
**Violates:** `test-quality.md` DoD — "No Hard Waits"; `timing-debugging.md` anti-pattern #1.

```ts
async resizeTo(width: number, height = 900): Promise<void> {
  await this.page.setViewportSize({ width, height });
  // One extra frame so matchMedia → fc-layout-breakpoints.js → C# dispatch all settle.
  await this.page.waitForTimeout(50);  // ❌ hard wait
}
```

**Why it matters:** `waitForTimeout(50)` is a classic compounding-flake surface. CI under load takes more than 50 ms to finish the `matchMedia → JS module → SignalR → C# dispatch → Fluxor notify → Blazor rerender` chain. The comment acknowledges it's a crutch, which is the tell. With six `resizeTo` calls in the main spec, one slow CI frame blows the whole test.

**Fix:**
```ts
async resizeTo(width: number, height = 900): Promise<void> {
  await this.page.setViewportSize({ width, height });
  // Wait for the viewport tier dispatch to land by observing a tier-specific anchor.
  // The caller's subsequent expect().toBeVisible() already does this — so drop the wait entirely
  // and let Playwright's assertion retries handle the propagation window.
}
```
The `expect(shell.fullNav).toBeVisible()` immediately after already polls. Delete the timeout.

**If a synchronization anchor is needed**, expose a DOM attribute that reflects the observed tier (e.g., `<body data-fc-viewport="desktop">` on `ViewportTierChangedAction`) and assert `await expect(page.locator('body')).toHaveAttribute('data-fc-viewport', 'desktop')`. That's deterministic and readable.

---

#### F2. `FcHamburgerToggleTests.ViewportDrivenVisibilityDoesNotDispatchToggle` invents a contract not in the ACs/decisions
**File:** `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs:99-116`
**Violates:** `test-quality.md` Example 3 — assertions must reflect the spec, not a hypothesis.

```csharp
[Fact]
public void ViewportDrivenVisibilityDoesNotDispatchToggle()
{
    // Negative test — at CompactDesktop, visibility comes from the tier selector, not from a
    // SidebarToggled dispatch. Opening the hamburger at CompactDesktop must NOT fire
    // SidebarToggledAction (which would flip SidebarCollapsed and cause a rail/overlay mismatch).
    // ...
    cut.Instance.OnHamburgerOpenedForTest(opened: true);
    cut.WaitForAssertion(() =>
        state.Value.SidebarCollapsed.ShouldBe(before, ...));
}
```

**Why it matters:** D9 reads *"User manual toggle at Desktop = SidebarToggledAction…"* — the word "Desktop" is explicit but no guard contract is defined for non-Desktop tiers. D7 at CompactDesktop says *"clicking [the rail] dispatches SidebarExpandedAction"* — that's the **rail button**, not the hamburger. What the hamburger should dispatch at CompactDesktop is **spec-ambiguous**.

If the dev agent implements the guard the test asserts, fine — but the ACs don't state it, the critical decisions don't state it, and no ADR covers it. This is the test driving the implementation into a behavior the PM/architect never signed off on.

**Resolution options:**
1. **Preferred:** Pause dev, raise a **spec-change proposal** to amend D9 with the guard explicitly (e.g., "D9-addendum: `FcHamburgerToggle.OnHamburgerOpened` dispatches `SidebarToggledAction` **only when `CurrentViewport == Desktop`**"). Then the test is correct.
2. If the intent is that `FluentLayoutHamburger` at CompactDesktop/Tablet/Phone invokes its own internal drawer-open (not our dispatch), delete this test entirely — it's testing that we didn't wire a callback we wouldn't have wired.

**Until resolved, this test encodes a ghost decision.** That's worse than no test.

---

### ⚠️ HIGH

#### F3. `FrontComposerNavigationTests.RendersRailAtCompactDesktop` — the key negative assertion is a tautology
**File:** `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs:225-229`

```csharp
cut.WaitForAssertion(() =>
{
    _ = cut.FindComponent<FcCollapsedNavRail>();
    cut.Markup.ShouldNotContain("<FluentNav"); // ❌ FluentNav not rendered in rail mode
});
```

**Why it matters:** bUnit renders Razor components to **HTML output**, not Razor markup. `<FluentNav>` as a literal string never appears in the DOM — FluentNav produces `<div class="fluent-nav">` or similar. This assertion **always passes** whether FluentNav renders or not. False confidence on AC4's primary invariant.

**Fix:**
```csharp
cut.WaitForAssertion(() =>
{
    _ = cut.FindComponent<FcCollapsedNavRail>();
    Should.Throw<Bunit.ComponentNotFoundException>(
        () => cut.FindComponent<Microsoft.FluentUI.AspNetCore.Components.FluentNav>(),
        "At CompactDesktop the full FluentNav must NOT render — only the FcCollapsedNavRail.");
});
```

The `Desktop`-tier variant (`RendersFullNavAtDesktop`, line 247) already uses the correct pattern — mirror it.

---

#### F4. `NavigationEffectsScopeTests.ViewportTierChangedDoesNotTriggerPersist` — reflection helper + dead assertion
**File:** `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs:109-133, 275-293`

Two issues in one test:

**(a) Reflection hack with name-pattern filter (lines 275-293):**
```csharp
internal static class NavigationEffectsInstrumentation
{
    public static Type[] GetPersistTriggerInputs(Type effectsType) =>
        effectsType.GetMethods(...)
            .Where(m => m.Name.Contains("SidebarToggled", ...) ||
                        m.Name.Contains("NavGroupToggled", ...) ||
                        m.Name.Contains("SidebarExpanded", ...) ||
                        m.Name.Contains("PersistNavigation", ...)) // ⚠️ name-based
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType))...
}
```
If a dev adds a new persist effect called `HandleNavStateChanged(NavStateChangedAction)`, the filter misses it and the ADR-037/038 invariant silently erodes.

**(b) Dead final assertion (line 132):**
```csharp
await Task.CompletedTask;
_ = storage.DidNotReceiveWithAnyArgs().SetAsync(...); // ❌ no action was ever dispatched
```
No action was dispatched to the effects, so of course SetAsync wasn't called. This assertion reports "pass" regardless of implementation correctness.

**Why it matters:** ADR-037/038 are load-bearing (pre-hydration ordering surface per D23). This test is the **only** automated check that viewport changes never write LocalStorage. If it passes for the wrong reason, you discover the regression in production when a rapid resize thrashes LocalStorage.

**Fix (behavioral, not reflective):**
```csharp
[Fact]
public async Task ViewportTierChangedDoesNotTriggerPersist()
{
    var storage = Substitute.For<IStorageService>();
    // ... setup sut with valid scope ...

    // If NavigationEffects has a [EffectMethod] for ViewportTierChangedAction, it must be
    // HandleViewportTierChanged(action, dispatcher) — call it directly and assert no write.
    var method = typeof(NavigationEffects).GetMethods()
        .FirstOrDefault(m =>
            m.GetCustomAttributes(typeof(EffectMethodAttribute), false).Any() &&
            m.GetParameters().FirstOrDefault()?.ParameterType == typeof(ViewportTierChangedAction));

    method.ShouldBeNull("ADR-037: no [EffectMethod] may accept ViewportTierChangedAction.");
    // No call to storage should have occurred because no method exists to route it.
    await storage.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default);
}
```
Now the test fails the moment **any** effect method (by any name) accepts `ViewportTierChangedAction`. The check is intent-accurate, not name-accurate.

Apply the same principle to `HydrateDoesNotRePersist` — it currently calls `HandleNavigationHydrated` directly (good), but the parallel reflection-style invariant should exist for `NavigationHydratedAction` too.

---

#### F5. E2E contract drift — `data-testid` attributes referenced in e2e but never asserted in bUnit
**Files:**
- `tests/e2e/page-objects/shell.page.ts:22-28` expects `fc-shell-navigation`, `fc-navigation-full`, `fc-collapsed-rail`, `fc-hamburger-toggle`, `fc-nav-category-Counter`
- No bUnit test asserts any of these test-ids are emitted.

**Why it matters:** Two-sided contract with no unit-level lock. The dev agent could ship the components rendering perfectly **without** the `data-testid` attributes and all bUnit tests pass. E2E tests then time out in CI, and the failure mode looks like a UI bug rather than a missing selector hook.

**Fix:** Add lightweight bUnit assertions in each component test:
```csharp
// In FrontComposerNavigationTests.RendersOneCategoryPerManifest:
cut.Markup.ShouldContain("data-testid=\"fc-navigation-full\"");
cut.Markup.ShouldContain("data-testid=\"fc-nav-category-Counter\"");

// In FcCollapsedNavRailTests.RendersOneButtonPerManifest:
cut.Markup.ShouldContain("data-testid=\"fc-collapsed-rail\"");

// In FcHamburgerToggleTests.VisibleFalseAtDesktopWhenNotCollapsed:
cut.Markup.ShouldContain("data-testid=\"fc-hamburger-toggle\"");

// In FrontComposerShellTests.Renders_navigation_slot_when_provided:
cut.Markup.ShouldContain("data-testid=\"fc-shell-navigation\"");
```
Now bUnit is the contract gate for the e2e selector surface. Selector drift fails fast at the unit level instead of slow in CI.

---

### 📝 MEDIUM

#### F6. `FrontComposerNavigationTests.ExpandedStateBindsToCollapsedGroups` — regex markup assertion (line 164)
```csharp
cut.Markup.ShouldMatch(".*id=\"Counter\".*Expanded=\"?false\"?.*", "…");
```
FluentNavCategory may omit the `Expanded` attribute when the value is default, or render as `expanded` (HTML5 style) or skip entirely when falsy. The regex is over-specified.

**Better:**
```csharp
var category = cut.FindComponents<FluentNavCategory>()
    .Single(c => c.Instance.Id == "Counter");
category.Instance.Expanded.ShouldBeFalse();
```
Assert the component property, not the rendered markup string.

---

#### F7. `FcCollapsedNavRailTests.ClickDispatchesSidebarExpanded` — `GetAwaiter().GetResult()` + ULID call count
**File:** `FcCollapsedNavRailTests.cs:94, 98`

```csharp
cut.InvokeAsync(() => firstButton.Click()).GetAwaiter().GetResult();  // ⚠️ sync-over-async
// ...
_ = _ulidFactory.Received(2).NewUlid(); // setup toggle + click expansion
```

- **`.GetAwaiter().GetResult()`** — deadlock-prone on sync-context-sensitive test runners. Make the test `async Task` and `await`. bUnit supports it.
- **`_ulidFactory.Received(2)`** — call count is brittle. If the reducer/effect pipeline adds a correlation-id generation anywhere (e.g., a telemetry hook), count changes. Assert end state (`SidebarCollapsed.ShouldBeFalse()`), which already exists on line 96. Delete the `Received(2)` line.

---

#### F8. `FcLayoutBreakpointWatcherTests.DisposesCleanly` — global invocations search captures sibling modules (line 137-140)
```csharp
JSInterop.Invocations
    .SelectMany(kv => kv.Value)
    .Any(inv => inv.Identifier.Equals("unsubscribe", ...))
```
`LayoutComponentTestBase` sets up `fc-beforeunload.js` and `fc-prefers-color-scheme.js` modules, **both of which have `unsubscribe` identifiers**. If the watcher under test skips `unsubscribe` but sibling modules invoke theirs during disposal, this test passes.

**Fix:**
```csharp
JSInterop.Invocations[ModulePath]
    .Any(inv => inv.Identifier.Equals("unsubscribe", StringComparison.Ordinal))
    .ShouldBeTrue("DisposeAsync MUST invoke unsubscribe on fc-layout-breakpoints.js (D5)");
```
Scope to the module under test.

---

#### F9. `FcLayoutBreakpointWatcherTests.DedupesWhenComposedTierUnchanged` — name/assertion drift (line 73-97)
The test name promises dedup per D6 (JS-side: "no interop call fires"), but the assertion is on Fluxor's value-equality short-circuit (reducer runs, state unchanged, no StateChanged event). Developer who eagerly dispatches on every `OnViewportTierChangedAsync` call passes this test **and** violates D6.

**Two fixes:**
1. **Keep the value-equality check** under a more honest name: `DuplicateTierDoesNotRenotifyState`.
2. **Add a real dedup test** that asserts the JS module call count, not the reducer side-effect:
   ```csharp
   // Invoke the watcher's C# handler twice with the same tier and verify the dispatcher
   // was called once (or the dispatch pipeline detected the no-op). Even better: verify
   // the JS module is NOT invoked twice under the same-tier condition. The actual dedup
   // lives in the JS module, so this may need a JS unit test.
   ```

The JS-side dedup (per D6) is **not** covered by any C# test. Either add a JS unit test for `fc-layout-breakpoints.js`, or add a matching E2E assertion that rapid `resizeTo` same-tier calls produce a single Fluxor dispatch (observable via a counter fixture).

---

#### F10. `FrontComposerNavigationTests` dispatches before render (lines 156-165, 219-222, 239-243)
```csharp
IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
dispatcher.Dispatch(new NavGroupToggledAction("c-setup", "Counter", Collapsed: true));
IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();
```
Fluxor's dispatch is effectively synchronous, but the component is built against the state snapshot at render time. If the dispatcher/reducer chain has any async step (e.g., effect decoration for logging), the reducer may not have applied before `Render<>()` captures state.

**Fix (safer):**
```csharp
await cut.InvokeAsync(() =>
    Services.GetRequiredService<IDispatcher>()
        .Dispatch(new NavGroupToggledAction("c-setup", "Counter", Collapsed: true)));
cut.Render();  // or WaitForState(...)
```
Or await a state-changed signal:
```csharp
var state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
Services.GetRequiredService<IDispatcher>().Dispatch(new NavGroupToggledAction(...));
await Task.Run(() => SpinWait.SpinUntil(() => state.Value.CollapsedGroups.ContainsKey("Counter"), 500));
var cut = Render<FrontComposerNavigation>();
```

---

#### F11. Hardcoded viewport widths in `sidebar-responsive.spec.ts` miss the boundary edges
Values used: `1920`, `1200`, `900`, `600`. These are deep inside each tier band — Desktop starts at exactly 1366, CompactDesktop at 1024, Tablet at 768 (per D4). **Boundary tests are missing**:
- Width 1366 (Desktop lower boundary, inclusive)
- Width 1365 (CompactDesktop upper boundary)
- Width 1024 (CompactDesktop lower)
- Width 1023 (Tablet upper)
- Width 768 (Tablet lower, inclusive)
- Width 767 (Phone upper)

**Why it matters:** Off-by-one in `matchMedia` queries is common (`min-width: 1366px` vs `min-width: 1367px`). The interior widths wouldn't catch it. Add a `@boundary` theory variant that iterates all six boundary pixels.

Also: extract the boundary constants to `shell.page.ts` as named exports so any breakpoint change updates the test source of truth alongside the JS module.

---

### 🔹 LOW

#### F12. `NavigationPersistenceSnapshotTests` — only the happy path is locked (D15 amended coverage is missing)
D15 (amended 2026-04-19 × 2) specifies HFC2106 `Reason=Empty` vs `Reason=Corrupt` structured field semantics. No test exercises the corrupt-JSON path. This matters less for a wire-format snapshot test, but the hydrate error branches should be covered in `NavigationEffectsScopeTests` with a corrupt-blob setup:
```csharp
[Fact]
public async Task HandleAppInitialized_CorruptBlob_LogsWithReasonCorrupt() { ... }

[Fact]
public async Task HandleAppInitialized_OperationCanceled_LogsDebugNotInformation() { ... }
```

#### F13. No `@smoke` / `@p0` tags on sidebar E2E specs
`sidebar-responsive.spec.ts` has no tag annotations. Per `selective-testing.md`, the responsive matrix (AC4/AC5) is P0 user-experience — should be tagged `@p0 @smoke` so pre-commit/PR jobs can filter it in. Currently it falls into the full regression bucket only.

#### F14. `FrontComposerShellTests.Applies_theme_once_on_first_render` — hardcoded default values (line 69)
```csharp
Arg.Is<ThemeSettings>(settings => settings.Mode == ThemeMode.Light && settings.Color == "#0097A7")
```
Magic values. If Story 3-1's default color changes (Epic 6 customization gradient), this test breaks unrelated to its own invariant. Extract to a `DefaultThemeSettings` constant or read from `FcShellOptions`.

#### F15. `CounterWebIntegrationTests` regex is greedy (line 40)
```csharp
substantiveLines[2].ShouldMatch(@"<FrontComposerShell>.*@Body.*</FrontComposerShell>", ...)
```
Matches `<FrontComposerShell>junk@BodyMore</FrontComposerShell>`. Low risk in practice but tighten to `^<FrontComposerShell>\s*@Body\s*</FrontComposerShell>$`.

#### F16. Unused `CancellationToken ct` in `HandleAppInitialized_ValidContextEmptyStorage_DoesNotDispatch`
File: `NavigationEffectsScopeTests.cs:167` — declared but never passed to `storage.GetAsync`. Minor; either use it or drop.

#### F17. E2E persistence test lacks tenant fixture
`sidebar-responsive.spec.ts:36-51` ("sidebar collapse persists across refresh") destructures only `{ page }`, not the project's `tenant` fixture. Per AC2, persistence fail-closes on missing `IUserContextAccessor.TenantId/UserId`. Without the tenant fixture wiring auth, the reload path is a no-op and the test's `collapsedRail` visibility could pass for the wrong reason (tier-driven, not persistence-driven).
**Fix:** use `test({ page, tenant })` and assert the auth is active before the collapse sequence.

---

## Positive Patterns (keep doing these)

1. **Typed component finds** — `cut.FindComponent<T>()` with `Should.Throw<ComponentNotFoundException>` for negative cases is the right pattern throughout. Far more resilient than markup string searches.

2. **Fail-closed scoping tests** — `NavigationEffectsScopeTests` covers tenant null, user null, whitespace × 4 InlineData combinations, logs assertion, storage-call-absence assertion. This matches the memory feedback `feedback_tenant_isolation_fail_closed.md` exactly. Template for all future per-user persistence work.

3. **Parameter-surface lock** — `FrontComposerShellParameterSurfaceTests` encodes the append-only contract (Story 3-1 D4 discipline) with `MetadataToken` ordering. This is the right way to prevent accidental parameter reorder across stories.

4. **Snapshot + verified.txt for wire format** — `NavigationPersistenceSnapshotTests` pins the persistence JSON shape. Cross-story contract with Story 3-6 (session resume) has a concrete anchor.

5. **Bilingual resource parity** — `FcShellResourcesTests.CanonicalKeysHaveFrenchCounterparts` prevents localization drift. Good that Story 3-2 extended it to parameterized keys (`{0}` placeholders).

6. **AC → test → decision traceability in test comments** — Every test file declares which AC and D it covers. Makes review and future triage trivial.

7. **Reusable `LayoutComponentTestBase`** — centralizes JS module mocks, storage/accessor substitutes, store init. Reduces per-test boilerplate and ensures consistent fixture setup.

8. **bUnit `JSInterop` modules mocked before render** — `FcLayoutBreakpointWatcherTests` follows the network-first pattern's C# analogue: module setup happens before `Render<>()` so the `OnAfterRenderAsync(firstRender: true)` import succeeds.

---

## Must-fix Before Dev (Blocks Green-Phase Start)

1. **F1** — Remove `waitForTimeout(50)` from `shell.page.ts`. It will bleed flake into the e2e suite.
2. **F2** — Resolve the `ViewportDrivenVisibilityDoesNotDispatchToggle` ghost contract. Either amend D9 with a spec-change proposal or delete the test.

Everything else can be fixed during dev or as a follow-up PR without blocking. F3–F5 are strong candidates for the dev agent's first code-review pass.

---

## Not Covered (Out of Scope)

- **Coverage mapping / gate decision** — routed to `trace` workflow per `test-review` step-01 directive.
- **Full Shell.Tests suite audit (Lifecycle / Theme / Density / Generated)** — inventoried but not deep-reviewed (all pre-existed 3-2; no modifications in scope beyond the files listed in the git status).
- **JS unit tests for `fc-layout-breakpoints.js`** — no test framework configured for the `.js` modules. D6's dedup contract is untested at the unit level. Recommend Vitest or similar; file under a follow-up.
- **Focus-ring visual regression (AC6)** — explicitly deferred to Story 10-2 per D16.
- **Badge count rendering (D20)** — deferred to Story 3-5 per design.
- **Playwright `@p0/@p1/@smoke` tag strategy** — `selective-testing.md` recommends a tag taxonomy; none currently applied. Low priority for this review, worth a follow-up.

---

## Recommended Next Steps

1. Dev agent applies **F1 + F2** fixes, commits, then starts Task 1.
2. Dev agent picks up **F3, F4, F5** during Task 10 wrap-up (these are test-correction tasks, not feature tasks).
3. On completion of Task 10, re-run this review in **Validate mode** to check that fixes landed and no new quality regressions crept in.
4. Route **coverage + gate decision** through `trace` workflow after green-phase completion.

---

_Review completed 2026-04-19. Murat signing off — the scaffolding is solid; the two critical fixes are cheap; the rest is polish._

---

## Fixes Applied (2026-04-19 same day)

| Finding | File(s) touched | Status |
|---|---|---|
| **F1** Hard wait | `tests/e2e/page-objects/shell.page.ts` | ✅ Removed `waitForTimeout(50)`; rely on `expect()` polling. |
| **F2** Ghost contract | `FcHamburgerToggleTests.cs` | ✅ Deleted `ViewportDrivenVisibilityDoesNotDispatchToggle`. In-file comment flags the spec-change path if the guard is intended. |
| **F3** Tautology | `FrontComposerNavigationTests.cs` | ✅ Replaced `ShouldNotContain("<FluentNav")` with typed `ComponentNotFoundException` check on `FluentNav`. |
| **F4** Reflection + dead assertion | `NavigationEffectsScopeTests.cs` | ✅ Replaced `ViewportTierChangedDoesNotTriggerPersist` with two intent-accurate reflection tests (`NoEffectMethodAcceptsViewportTierChangedAction`, `NoEffectMethodAcceptsNavigationHydratedAction`). Removed orphan `NavigationEffectsInstrumentation` helper class. |
| **F5** Selector contract | 4 bUnit files | ✅ Added `data-testid` assertions for `fc-navigation-full`, `fc-nav-category-*`, `fc-collapsed-rail`, `fc-hamburger-toggle`, `fc-shell-navigation`. |
| **F6** Markup regex | `FrontComposerNavigationTests.ExpandedStateBindsToCollapsedGroups` | ✅ Now asserts `category.Instance.Expanded` via typed component find. |
| **F7** Sync-over-async + ULID count | `FcCollapsedNavRailTests.ClickDispatchesSidebarExpanded` | ✅ `async Task` + `await cut.InvokeAsync(...)`. Dropped brittle `Received(2).NewUlid()` — observable state change is the contract. |
| **F8** Global JS invocation scan | `FcLayoutBreakpointWatcherTests.DisposesCleanly` | ✅ Scoped to `JSInterop.Invocations[ModulePath]`. |
| **F9** Dedup test naming | `FcLayoutBreakpointWatcherTests` | ✅ Renamed to `DuplicateTierDoesNotRenotifyState`. Note added that the true D6 JS-side dedup needs a `fc-layout-breakpoints.js` unit test (no JS test framework configured — follow-up). |
| **F11** Boundary widths | `sidebar-responsive.spec.ts` + `shell.page.ts` | ✅ Added `ViewportBreakpoints` constants; 6 new `@p1` parameterized tests at each tier boundary pixel. |
| **F13** Tag taxonomy | `sidebar-responsive.spec.ts` | ✅ Added `@p0 @smoke` on AC3/AC4/AC5 + AC1/AC7 specs, `@p1` on boundary tests. |
| **F15** Greedy regex | `CounterWebIntegrationTests.cs` | ✅ Anchored pattern: `^<FrontComposerShell>\s*@Body\s*</FrontComposerShell>$`. |
| **F16** Unused `ct` | `NavigationEffectsScopeTests.HandleAppInitialized_ValidContextEmptyStorage_DoesNotDispatch` | ✅ Removed unused declaration. |
| **F17** Missing tenant fixture | `sidebar-responsive.spec.ts` | ✅ All 4 tests now destructure `{ page, tenant }` and assert `tenant.tenantId` is truthy before exercising AC2 flows. |

### Deliberately NOT fixed

- **F10** (dispatch-before-render race) — re-analysis: Fluxor's default dispatcher runs reducers synchronously; no actual race. Downgraded from Medium to non-issue.
- **F12** (D15 corrupt-blob / OperationCanceled coverage) — requires designing new tests that risk inventing contracts (cf. F2). Dev agent should add these during the green phase with D15 in hand.
- **F14** (hardcoded theme default `#0097A7`) — acceptable pin. Documenting intent is cheaper than extracting a constant.
- **JS-side D6 dedup** — no JS test framework configured. Tracked in F9's in-code comment as a follow-up.

### Outstanding flag for user decision

**F2 follow-up:** If the dev/architect confirms that non-Desktop `OnHamburgerOpened` SHOULD be guarded (not dispatch `SidebarToggledAction`), amend D9 with an explicit clause and reinstate the deleted test. Until that amendment lands, the behavior is spec-ambiguous by design.

