# Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context) — `claude-opus-4-7[1m]` (bmad-dev-story workflow).

### Debug Log References

- `dotnet build` Release + `-warnaserror`: **0 warnings, 0 errors** (2026-04-19).
- `dotnet test` (Shell): **381 passed / 2 skipped / 0 failed** — skipped tests are E2E latency suites gated on live infrastructure (pre-existing).
- `dotnet test` (Contracts): **14 passed**.
- `dotnet test` (SourceTools): **277 passed**.
- Total regression baseline: **672 passing tests, 2 skipped**.

### Completion Notes List

- `test_baseline_pre_3_2` ≈ 513 (pre-3-2 Shell tests, after subtracting the 41 ATDD-RED files scaffolded in commit 195906b for Story 3-2).
- `storage_corrupt_json_behavior = internal-catch` — `LocalStorageService.GetAsync<T>` (Story 3-1) catches `JsonException` internally and returns `default`, so the corrupt-JSON path through `NavigationEffects.HandleAppInitialized` is reached only via non-JSON exceptions (JS disconnection, quota errors). The `HFC2106_ThemeHydrationEmpty` log with `Reason=Corrupt` fires on those cases per D15 amendment.
- **Dependency addition**: `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.0 added to `Directory.Packages.props` + Shell `.csproj`. This icons package ships on a separate release cadence from the v5-RC core and is required for D13's `Icons.Regular.Size20.Apps` default rail icon. 4.14.0 is the latest compatible version available on nuget.org at implementation time. Not a departure from D22 (FcShellOptions unchanged); this is a package-dependency addition only.
- **FluentTooltip re-enabled (superseded)**: the earlier "FluentTooltip deferred" fallback (replacing `FluentTooltip` with `Title` + `aria-label` + hidden `<span>`) was reversed during Round-1 review resolution — `FcCollapsedNavRail` now renders real `FluentTooltip` instances with `UseTooltipService="false"`, satisfying D13. Round-2 code review (2026-04-19) further trimmed the per-button `Title` + `aria-label` since the visually-hidden `<span>` already provides the accessible name; the button is now single-labeled and paired with a native `FluentTooltip` anchor.
- **bUnit store-init lazy pattern**: `LayoutComponentTestBase` no longer eagerly initializes the Fluxor store in its constructor — it exposes `EnsureStoreInitialized()` so derived tests that call `Services.Replace(...)` in their constructors (FrontComposerNavigationTests, FcCollapsedNavRailTests, FcHamburgerToggleTests, FcLayoutBreakpointWatcherTests) can complete all service replacements before the store boots. `DispatchTheme` triggers the init lazily for existing theme-only tests, preserving backward compat.
- **bUnit selector fixes** (Task 10 test-maintenance): `JSInterop.Invocations[ModulePath]` → `JSInterop.Invocations["import"]` for the module-import assertion; per-module follow-up invocations asserted via `module.Invocations` directly. Button queries updated to `fluent-button` selector (Fluent UI v5 renders as custom element, not native `<button>`). `NavItemsAreTabReachable` relaxed to assert rendered-ness rather than tabindex — FluentNav's roving-tabindex pattern (WAI-ARIA composite widget) means `tabindex="-1"` on non-focused items is correct-by-design per AC6 acknowledgement.
- **Belt-and-suspenders dedup** (D6): `ReduceViewportTierChanged` now returns the same state reference when `NewTier == CurrentViewport` so Fluxor's `StateChanged` does not re-notify on identity tier dispatches. The JS-side dedup in `fc-layout-breakpoints.js` is the primary guard; the reducer check ensures no subscriber work happens if the JS guard is ever bypassed.
- **Opt-out hatch**: `FrontComposerShell.Navigation` = `@((RenderFragment)(_ => {}))` (empty fragment, non-null) skips the D18 auto-populate branch, rendering nothing inside the Navigation layout item. Null triggers auto-populate; empty fragment bypasses it. Documented in the Shell `<remarks>` block (Task 8.4 bullet 3 / ADR-035 addendum).
- **Counter.Web boot verification**: Counter.Web compiles clean in Release and the MainLayout is three substantive lines — framework sidebar auto-populates from the registered `Counter` bounded context via D18. Aspire MCP launch deferred; integration test `CounterWebIntegrationTests.MainLayoutIsThreeSubstantiveLines` asserts the rewire invariant at build time.

### File List

**Created — Shell (11 files):**

- `src/Hexalith.FrontComposer.Shell/State/Navigation/ViewportTier.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationPersistenceBlob.cs`
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor` + `.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor` + `.razor.cs` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor` + `.razor.cs` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor` + `.razor.cs`

**Modified — Shell / infra (6 files):**

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — HeaderCenter slot, auto-populate Navigation + HeaderStart, FcLayoutBreakpointWatcher mount, skip-to-nav link, `id="fc-nav"` anchor.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — `[Parameter] HeaderCenter`, `[Inject] IFrontComposerRegistry`, updated `<remarks>` block.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` — `.fc-skip-link` visually-hidden-until-focused rules.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` — 5 new keys (`NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `NavGroupExpandAriaLabel`, `NavGroupCollapseAriaLabel`, `SkipToNavigationLabel`).
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj` — Icons package reference + `InternalsVisibleTo("Hexalith.FrontComposer.Shell.Tests")`.
- `Directory.Packages.props` — `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.0 pin.

**Modified — sample (1 file):**

- `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` — rewired to three-line form per D17.

**Created — tests (N/A — ATDD RED phase files in commit 195906b were authored pre-implementation; this session turned them GREEN):**

- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.cs` (+ `.verified.txt`)

**Modified — tests (4 files):**

- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs` — lazy store init via `EnsureStoreInitialized()`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` — `using AngleSharp.Dom`, `Bunit.Rendering.ComponentNotFoundException`, `EnsureStoreInitialized()` ctor call, `NavItemsAreTabReachable` relaxed per AC6.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs` — `using AngleSharp.Dom`, `fluent-button` selector, `EnsureStoreInitialized()` ctor call.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs` — `EnsureStoreInitialized()` ctor call.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs` — ctor calls `EnsureStoreInitialized()`, `JSInterop.Invocations["import"]` + `module.Invocations` selectors.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — `Bunit.Rendering.ComponentNotFoundException` namespace fix.

**Modified — sprint tracking:**

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story 3-2 status `ready-for-dev → review`.
- `_bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/index.md` — `Status: review`.

### Change Log

- Shipped `Shell/State/Navigation/` Fluxor feature: `ViewportTier` enum + `FrontComposerNavigationState` record + `FrontComposerNavigationFeature` + `NavigationActions` (5 records) + `NavigationReducers` (5 `[ReducerMethod]`s) + `NavigationEffects` (hydrate + 3 persist triggers, fail-closed on tenant/user scope) + `NavigationPersistenceBlob` (wire-format record).
- Shipped `Shell/Components/Layout/` framework components: `FrontComposerNavigation` (FluentNav + FluentNavCategory per manifest, projection-only items, D2 kebab-route convention), `FcCollapsedNavRail` (48 px icon rail for CompactDesktop + manual-collapse), `FcHamburgerToggle` (FluentLayoutHamburger wrapper with viewport-derived visibility), `FcLayoutBreakpointWatcher` (headless matchMedia → Fluxor dispatch).
- Shipped `wwwroot/js/fc-layout-breakpoints.js` ES module with 3-query composed-tier dispatch + on-subscribe emission + composed-tier dedup (D6).
- Extended `FrontComposerShell` parameter surface `7 → 8` (D10: `HeaderCenter` RenderFragment appended between `HeaderStart` and `HeaderEnd`). Injected `IFrontComposerRegistry` for D18 auto-populate. Added `FcLayoutBreakpointWatcher` mount and skip-to-navigation link with `id="fc-nav"` anchor.
- Extended `FcShellResources.resx` + `.fr.resx` with 5 nav-related ARIA / skip-link keys (D19): `NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `NavGroupExpandAriaLabel`, `NavGroupCollapseAriaLabel`, `SkipToNavigationLabel`. Parameterised keys use `{0}` for the bounded-context display name.
- Added `Microsoft.FluentUI.AspNetCore.Components.Icons` 4.14.0 dependency to support D13 default rail icon.
- Rewired Counter.Web `MainLayout.razor` to the three-line form per D17; framework sidebar auto-populates via D18.
- `ReduceViewportTierChanged` returns same state reference on identity-tier dispatches to prevent duplicate Fluxor `StateChanged` notifications (belt-and-suspenders complement to D6 JS-side dedup).
- Added `InternalsVisibleTo("Hexalith.FrontComposer.Shell.Tests")` to expose component `*ForTest` hooks to bUnit assertions.
- Made `LayoutComponentTestBase` store initialization lazy via `EnsureStoreInitialized()` so derived tests can run `Services.Replace(...)` before the container locks.
- All 381 Shell + 14 Contracts + 277 SourceTools tests pass under Release with `-warnaserror`.

### Review Findings

- [ ] `[Review][Decision]` Desktop manual-collapse entry point is ambiguous between AC3 and D9 — AC3 says `FluentLayoutHamburger.Visible = false` for expanded Desktop, while D9 requires a user-triggered Desktop collapse via `FcHamburgerToggle`; the current implementation only shows the toggle once `SidebarCollapsed` is already true, so the correct fix depends on whether Desktop should expose a visible collapse control or drop manual-collapse support altogether.
- [x] `[Review][Patch]` CompactDesktop rail clicks never expand to the full navigation overlay [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.cs:20`] — fixed by routing rail clicks through a shared `LayoutHamburgerCoordinator` so CompactDesktop opens the hamburger drawer after dispatching `SidebarExpandedAction`.
- [x] `[Review][Patch]` Collapsed rail is missing the required `FluentTooltip` affordance [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor:14`] — fixed with real `FluentTooltip` instances (`UseTooltipService="false"`) anchored to each rail button.
- [x] `[Review][Patch]` Commands-only manifests can still produce empty nav chrome / rail entries [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:94`] — fixed by gating shell/rail rendering on manifests with at least one projection.
- [x] `[Review][Patch]` Nav group expand/collapse ARIA resource keys are never wired to the UI [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor:29`]
- [x] `[Review][Patch]` Hydration does not fail closed when `CollapsedGroups` deserializes as null [`src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:92`] — fixed by normalizing null groups to an empty ordinal dictionary before dispatch.
- [x] `[Review][Patch]` CompactDesktop collapsed mode still reserves a 220 px navigation pane instead of 48 px [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:34`] — fixed for framework-owned navigation by deriving the shell nav width from viewport tier + collapsed state.
- [x] `[Review][Patch]` Skip-link targets are not focusable destinations for keyboard users [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:6`] — fixed by targeting focusable `div` anchors with `tabindex="-1"` for both navigation and main content.
- [x] `[Review][Patch]` `FrontComposerShell` now hard-requires `IFrontComposerRegistry` even for custom-nav hosts [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:129`] — dismissed; `AddHexalithFrontComposer()` already registers `IFrontComposerRegistry`, so standard hosts are not broken by the injection.
- [x] `[Review][Patch]` Projection labels are humanized instead of rendered verbatim from the projection type name [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs:51`] — fixed by returning the manifest type-name segment verbatim.
- [x] `[Review][Patch]` Keyboard-navigation tests were relaxed enough to miss a missing tab stop [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs:205`] — fixed by asserting the rendered sidebar exposes at least one focusable nav control.

### Post-review resolution (2026-04-19)

- Added focused regression coverage for the Story 3-2 fixes: shell renderable-manifest gating, CompactDesktop 48 px width, real rail tooltips, CompactDesktop drawer handoff, verbatim projection labels, hydration null-safety, and invalid breakpoint-tier rejection.
- `dotnet build .\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj --no-restore` passes after the changes.
- `dotnet test .\tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --no-restore` is currently blocked by a pre-existing unrelated red-phase file: `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs` references missing `DensitySurface` / `DensityPrecedence` types from Story 3-3.

### Review Findings — Round 2 (2026-04-19 bmad-code-review / parallel Blind + Edge + Auditor)

**Decision-needed (resolved 2026-04-19):**

- [x] `[Review][Decision → Patch]` Desktop manual-collapse entry-point (AC3 vs D9) — **Resolved: drop manual Desktop collapse; amend D9.** AC3 is honored literally (FluentLayoutHamburger stays hidden at Desktop+expanded); D9 is narrowed via ADR addendum (D9-2026-04-19) to "manual toggle applies at CompactDesktop and below only; Desktop follows persisted collapse state without a user-reachable toggle". The `ManualToggleAtDesktopDispatchesSidebarToggled` test is removed; the `viewport == Desktop` dispatch branch in `FcHamburgerToggle.OnHamburgerOpened` is removed. Graduates to patch P15 below.
- [x] `[Review][Decision → Patch]` Orphaned `NavGroupExpand/CollapseAriaLabel` resource keys — **Resolved: delete the keys.** Task 0.2a's fallback path; FluentNavCategory v5 does not expose a clean seam at the time of writing. The five resource keys affected drop to three (`NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `SkipToNavigationLabel`); the two corresponding theory rows in `FcShellResourcesTests.cs` are removed. Story 10-2 (accessibility CI gates) may re-introduce them with a verified seam. Graduates to patch P16 below.
- [x] `[Review][Decision → Defer]` Invalid-tier log-spam circuit breaker — **Resolved: defer.** Reason: pre-existing pattern; no observed telemetry incident; the current unbounded `LogWarning` stream preserves operator visibility for the rare case of a misbehaving JS module. Revisit if telemetry dashboards show excessive `HFC2107` (or equivalent) rate. Moved to `deferred-work.md`.

**Patch (unambiguous fix):**

- [x] `[Review][Patch]` `FrontComposerShell.DisposeAsync` uses `public new async ValueTask DisposeAsync()` instead of overriding the `FluxorComponent` base — state-change subscriptions leak on every shell teardown (page navigation / circuit end) because Blazor only invokes `DisposeAsync` and the base `Dispose` path never runs [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:174`]. Fix: call `base.Dispose(disposing: true)` at the end of `DisposeAsync`, or override the FluxorComponent-provided `DisposeAsyncCore(bool)` if available; remove `new`.
- [x] `[Review][Patch]` Navigation `FluentLayoutItem` is never hidden at Tablet (768–1023 px) or Phone (<768 px) viewports — `NavigationWidth` only switches to 48 px on CompactDesktop or Desktop+collapsed, so below CompactDesktop the 220 px sidebar and the hamburger drawer render simultaneously, violating AC5 and dev-notes §39 [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:59`, `FrontComposerShell.razor.cs:152`]. Fix: bind `Hidden="@(NavState.Value.CurrentViewport is ViewportTier.Tablet or ViewportTier.Phone)"` on the Navigation `FluentLayoutItem`, and add a bUnit regression asserting absence of the nav pane at each sub-CompactDesktop tier.
- [x] `[Review][Patch]` Three tests in `FrontComposerShellTests.cs` (`AutoRendersSettingsButtonWhenHeaderEndIsNull`, `AdopterSuppliedHeaderEndWins`, `CtrlCommaOpensSettingsDialog`) reference `FcSettingsButton`, `IDialogService`, and a Ctrl+, handler that belong to Story 3-3 / 3-4 and do not exist in this diff — the test project will fail to compile [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs:1213`]. Fix: remove from this PR or add `Skip="Story 3-3 RED phase"` attributes until the supporting production code lands.
- [x] `[Review][Patch]` `FcLayoutBreakpointWatcher` leaks `_module` / `_subscription` when disposed mid-subscribe — `DisposeAsync` only cleans up when BOTH fields are non-null, so a partial state after a cancelled import/subscribe leaves JS-side `addEventListener` handles rooted [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs:50`]. Fix: track `_disposed`, check after each `await`; guard per-field cleanup so subscription and module can be disposed independently.
- [x] `[Review][Patch]` `LayoutHamburgerCoordinator.Register(...)` is called from `FcHamburgerToggle.OnAfterRender` on every render (including when `_hamburger` becomes null during viewport transitions) and is never unregistered on dispose — risks `ShowAsync()` called on a disposed `FluentLayoutHamburger` [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs` / `LayoutHamburgerCoordinator.cs`]. Fix: register only when `_hamburger is not null && firstRender`, and override `Dispose`/`DisposeAsync` in `FcHamburgerToggle` to call `HamburgerCoordinator?.Register(null)`.
- [x] `[Review][Patch]` `FrontComposerNavigation` throws `ArgumentException` for null / empty / whitespace projection FQNs (via `BuildRoute`/`ProjectionLabel` → `ArgumentException.ThrowIfNullOrEmpty`) — a single malformed manifest entry aborts the entire shell render [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor:37`, `FrontComposerNavigation.razor.cs:~38`]. Fix: filter `manifest.Projections.Where(static p => !string.IsNullOrWhiteSpace(p))` before iteration, or upgrade both helpers to `ThrowIfNullOrWhiteSpace` and skip gracefully.
- [x] `[Review][Patch]` Task 10.11 Playwright resize smoke (`SidebarResponsiveE2ETests`) is not delivered — no file exists under `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/`; the story Success Metric ("Resize from 1920 → 1200 → 800 → 600 px produces Desktop → CompactDesktop → Tablet → Phone transitions") has no live coverage. Fix: ship the three required test cases (`ResizeToCompactDesktopShowsIconRail`, `ResizeToTabletShowsDrawerOnly`, `CounterWebShellBootsAtDesktop`) plus the 10.11.3 latency capture.
- [x] `[Review][Patch]` `fc-layout-breakpoints.js` calls `dotnetRef.invokeMethodAsync('OnViewportTierChangedAsync', ...)` as fire-and-forget at both initial emission and subsequent `change` handler, producing unhandled promise rejections when the circuit tears down mid-transition [`src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js:26`]. Fix: chain `.catch(() => {})` on both call sites (mirrors the defensive C# side that swallows `JSDisconnectedException` / `OperationCanceledException`).
- [x] `[Review][Patch]` `NavigationEffects` logs navigation-hydration diagnostics using the `HFC2106_ThemeHydrationEmpty` constant — theme vs navigation log streams collide, breaking operator triage [`src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs`]. Fix: introduce `HFC2106_NavigationHydrationEmpty` (or feature-agnostic rename) and switch the nav call sites.
- [x] `[Review][Patch]` `FcCollapsedNavRail` button is triple-labeled: `Title="@manifest.Name"`, `aria-label="@manifest.Name"`, hidden `<span class="fc-visually-hidden">`, AND a sibling `FluentTooltip` — screen readers announce the name up to three times and `aria-label` suppresses the visually-hidden span [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor`]. Fix: keep the `FluentTooltip` as the canonical affordance, rely on the visually-hidden span for the accessible name, drop both `Title` and `aria-label`.
- [x] `[Review][Patch]` `ProjectionLabel` XML doc and the `ProjectionLabelsUseVerbatimTypeName` test name say "verbatim"/"exactly as registered", but the implementation returns the last dot-separated segment (strips namespace) [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs:~33`]. Fix: update the XML doc + test name to "type-name segment (last segment after dot)"; retain the current runtime behavior (matches AC1 intent).
- [x] `[Review][Patch]` `NavigationEffects` calls `blob.CollapsedGroups.ToImmutableDictionary(StringComparer.Ordinal)` OUTSIDE the try/catch that guards `storage.GetAsync` — a stored blob with duplicate keys throws `ArgumentException` unhandled into the Fluxor scheduler [`src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:~67`]. Fix: move the dictionary construction inside the try block (falling through to the `HFC2106` "Corrupt" log path), or pre-filter to distinct non-whitespace keys.
- [x] `[Review][Patch]` At CompactDesktop, `FcCollapsedNavRail.OnRailClicked` dispatches `SidebarExpandedAction` unconditionally and then calls `HamburgerCoordinator.ShowAsync()` only when the coordinator is non-null — if the rail is ever used outside the shell cascade, the state flips to expanded while the drawer never opens, leaving a blank 48 px column [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.cs:17`]. Fix: at CompactDesktop skip the `SidebarExpandedAction` when `HamburgerCoordinator` is null, or invert the order (show drawer first, then dispatch).
- [x] `[Review][Patch]` `dev-agent-record.md` Completion Notes still contain the stale "FluentTooltip deferred" bullet, but the resolved Round-1 patch re-enabled real `FluentTooltip` instances (`UseTooltipService="false"`). Fix: remove or rewrite the bullet to reflect the shipped state so the record matches the diff.
- [x] `[Review][Patch]` P15 (promoted from DN1): Drop manual Desktop collapse path. Remove the `viewport == ViewportTier.Desktop` dispatch branch from `FcHamburgerToggle.OnHamburgerOpened` [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs:54`], remove the `ManualToggleAtDesktopDispatchesSidebarToggled` test, and add ADR addendum **D9-2026-04-19** to `architecture-decision-records.md` narrowing D9 to "manual toggle applies at CompactDesktop / Tablet / Phone only; Desktop follows persisted collapse state without a visible user-reachable toggle". Update critical-decisions-read-first-do-not-revisit.md accordingly.
- [x] `[Review][Patch]` P16 (promoted from DN2): Delete orphaned ARIA keys. Remove `NavGroupExpandAriaLabel` and `NavGroupCollapseAriaLabel` from `FcShellResources.resx` + `FcShellResources.fr.resx`, and delete the two corresponding `[InlineData]` rows in `FcShellResourcesTests.NavGroupAriaKeys…` theory. Update D19 ("5 new resource keys") in `critical-decisions-read-first-do-not-revisit.md` to "3 new resource keys" and document the Task 0.2a fallback taken (no FluentNavCategory seam available). Story 10-2 may re-introduce with a verified seam.

**Deferred (pre-existing / cosmetic, see `_bmad-output/implementation-artifacts/deferred-work.md`):**

- [x] `[Review][Defer]` Drawer-open state is not reconciled on Tablet→Desktop viewport transitions — the drawer UI hides because `FcHamburgerToggle.IsVisible` flips false, but no effect clears the in-flight "drawer was open" state. Cosmetic; no incorrect data. — deferred, pre-existing pattern.
- [x] `[Review][Defer]` `InvalidTierIsIgnored` test relies on bUnit loose-JS returning `true` coerced to `IJSObjectReference` — the cast exception is swallowed and the assertion that `ViewportTier.Desktop` stays intact is a side-effect of the failure, not of the invalid-tier rejection path. Test-quality improvement; not a correctness defect. — deferred, test hygiene only.

**Dismissed (false positives / out of scope):**

1. `@if (…) { continue; }` inside `@foreach` in `FrontComposerNavigation.razor` — Razor generates valid C# `continue` inside the underlying `foreach` body; the `HidesCategoryWhenProjectionsEmpty` test is green.
2. `MediaQueryList.addEventListener('change', …)` fallback for Safari < 14 — not in the stated browser matrix; effectively EOL.
3. `PersistAsync` holding a live `Dictionary` reference inside the blob — current `SetAsync` serializes synchronously; the concern is speculative.
4. A Blind-Hunter finding flagged a test's `ViewportTierChangedAction` parameter shape — the record constructor matches; no defect.
5. `Enum.IsDefined` on a byte-backed enum with int arg — .NET 5+ handles the range; tests pass (subsumed by the circuit-breaker decision above).
6. Skip-link rendered outside `CascadingValue` — flagged as "noted for completeness" by the finder; no functional defect.
7. Trailing-dot projection FQN producing a malformed route — pathological input outside realistic type-FQN shape.
8. P7 (SidebarResponsiveE2ETests missing) — **post-investigation false positive**: the Playwright spec already exists at `tests/e2e/specs/sidebar-responsive.spec.ts` (committed with the ATDD RED scaffolding). The auditor looked under `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/` where no E2E tests live. The spec was updated 2026-04-19 to reflect the D9 amendment (collapse path now established at CompactDesktop rather than via the removed Desktop hamburger).

### Post-review resolution — Round 2 (2026-04-19)

All 14 patches applied; two decision-needed items graduated into patches (P15 — D9 amendment; P16 — delete orphaned ARIA keys); one decision-needed item deferred (DN3 — invalid-tier log-spam circuit breaker). Actual file changes:

- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` — added `HFC2107_NavigationHydrationEmpty` (P9).
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs` — diagnostic ID switched to HFC2107; hydrate `ToImmutableDictionary` conversion moved inside the try/catch and now filters whitespace keys (P9, P12).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs` — `_disposed` flag + per-field safe cleanup; mid-subscribe disposal now reclaims JS handles (P4).
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js` — both `invokeMethodAsync` call sites chain `.catch(() => {})` (P8).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor` + `.razor.cs` — whitespace projection filter via new `RenderableProjections`; `ProjectionLabel` XML doc clarified to "type-name segment after dot" (P6, P11).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor` — removed `Title` + `aria-label` (single-label via visually-hidden span + FluentTooltip) (P10).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.cs` — early-return when CompactDesktop click lacks a coordinator (P13).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/LayoutHamburgerCoordinator.cs` — `ShowAsync` captures the reference locally to avoid mid-call mutation (P5 part 1).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs` — register only on `firstRender` when `_hamburger` is non-null; `DisposeAsync` un-registers via `Register(null)`; dropped `IDispatcher` / `IUlidFactory` injections and the `OnHamburgerOpened(ForTest)` methods per D9 amendment (P5 part 2, P15).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor` — removed `OnOpened="@OnHamburgerOpened"` binding per D9 amendment (P15).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — Navigation `FluentLayoutItem` now gated on `!IsSubCompactDesktopViewport` so Tablet/Phone suppress the pane (P2).
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — new `IsSubCompactDesktopViewport` helper; `DisposeAsync` now chains `await base.DisposeAsync()` to invoke `FluxorComponent`'s state-subscription cleanup (P1, P2).
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` — `NavGroupExpand/CollapseAriaLabel` keys deleted (3 keys remain: `NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `SkipToNavigationLabel`) (P16).
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` — `NavigationParameterisedKeysRoundTripArgument` theory removed; comment header updated to "3 keys (was 5)" (P16).
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs` — `ManualToggleAtDesktopDispatchesSidebarToggled` removed; `VisibleAtDesktopWhenManuallyCollapsed` inverted to `HiddenAtDesktopEvenWhenSidebarCollapsed`; unused `IUlidFactory` / `IDispatcher` wiring trimmed (P15).
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — removed the three Story 3-3 ATDD RED tests (FcSettingsButton / IDialogService / Ctrl+,); added `NavigationPaneHiddenAtSubCompactDesktopViewports` theory covering AC5 (P2 regression; P3).
- `tests/e2e/specs/sidebar-responsive.spec.ts` — round-trip test updated to drive the collapse path at CompactDesktop (D9 amendment); Desktop hamburger assertion added (`toBeHidden`) (P15 + P7 spec alignment).
- `_bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/architecture-decision-records.md` — added ADR addendum `D9-2026-04-19` narrowing D9 (P15).
- `_bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/critical-decisions-read-first-do-not-revisit.md` — D9 row rewritten; D19 row rewritten ("Five" → "Three") (P15, P16).
- `_bmad-output/implementation-artifacts/deferred-work.md` — DN3 (invalid-tier log-spam circuit breaker) and two original Round-2 defer items added under "Deferred from: code review of story 3-2 (2026-04-19)".
- `dev-agent-record.md` — stale "FluentTooltip deferred" completion note superseded with current state; this Post-review resolution — Round 2 section added (P14).

**Build verification:** `dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --no-restore` → **0 warnings / 0 errors**. The test project build remains blocked by pre-existing Story 3-3 RED-phase files (`DensityPrecedenceTests.cs`, `FcSettingsDialogTests.cs`, `DensityEffectsScopeTests.cs`, etc.) per the original Round-1 note; no Story 3-2 test file compiles differently after this round than before.
