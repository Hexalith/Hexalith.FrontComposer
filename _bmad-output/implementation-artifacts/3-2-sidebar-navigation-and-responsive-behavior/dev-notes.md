# Dev Notes

## Executive Summary (Read First — Feynman-level, ~30 sec)

_Added 2026-04-19 per advanced elicitation Feynman finding — junior-dev onboarding at a glance._

When Counter.Web starts, the framework sidebar renders in "Desktop" mode by default because the browser hasn't been measured yet. Meanwhile, two things race: LocalStorage is asked for the user's saved sidebar preferences (corrupt or missing → feature defaults apply with a log entry), and a small JavaScript module measures the browser window and tells the app which size bucket it's in (Desktop / CompactDesktop / Tablet / Phone). Until the JS fires, the app never writes to LocalStorage — it only reads. Once the size bucket arrives, the sidebar snaps to the right layout. A user clicking the hamburger during that brief window is safe: their click writes preferences under the correct tenant + user identity, which was known before rendering started. If the JS never loads at all (CDN failure, etc.), the app logs a warning and stays at Desktop — the framework remains usable. That's the whole story; everything below is precision.

For the architectural precision (why the hydrate → persist → watcher ordering is safe by construction, why viewport is never persisted, why the Blazor Server SSR window cannot corrupt data), see **ADR-035 / 036 / 037 / 038** + **D14 / D23**. For the error-path precision (corrupt blob, storage quota, JS module import failure, circuit disposal mid-persist), see **D15 amended × 2**.

## Service Binding Reference

All additions in 3-2 ride inside the existing `AddHexalithFrontComposer` registration — no new DI extension methods.

- `IFrontComposerRegistry` — Singleton (Story 1-x). `FrontComposerNavigation` + `FcCollapsedNavRail` + the `FrontComposerShell` auto-populate check consume via `[Inject]`.
- `IStorageService` — Scoped (Story 3-1 ADR-030). `NavigationEffects` gets it via constructor injection; Fluxor effect DI matches the scoped lifetime.
- `IUserContextAccessor` — Scoped (Story 2-2 D31). Reused for the `TryResolveScope` guard in `NavigationEffects` exactly as `ThemeEffects` / `DensityEffects` do.
- `IStringLocalizer<FcShellResources>` — Scoped via `AddLocalization()` (adopter-owned per Story 3-1 D24). Resolves the 5 new aria labels.
- `IDispatcher`, `IState<FrontComposerNavigationState>`, `IStateSelection<>` — standard Fluxor scoped-per-circuit behavior.
- `IUlidFactory` — Singleton (Story 2-3 D2/D3). Used by `FcHamburgerToggle`, `FcCollapsedNavRail`, `FrontComposerNavigation` for correlation IDs on dispatched actions.
- `IJSRuntime` — Scoped. `FcLayoutBreakpointWatcher` imports the ES module lazily on first render.
- `Fluxor` assembly scan — the existing `ScanAssemblies(typeof(FrontComposerThemeState).Assembly)` in `AddHexalithFrontComposer` discovers `FrontComposerNavigationFeature`, `NavigationReducers`, and `NavigationEffects` automatically; no explicit feature registration.

## FrontComposerNavigation Composition Diagram

```
┌─ <FrontComposerShell>
│
│  ┌─ <FluentLayout MobileBreakdownWidth="768">
│  │
│  │  ┌─ <FluentLayoutItem Area=Header Height="48px">
│  │  │    ├─ @HeaderStart ?? <FcHamburgerToggle />   ← auto-populate D18 (HeaderStart)
│  │  │    ├─ <FluentText Typo=@Typography.AppTitle>@AppTitle</>
│  │  │    ├─ @HeaderCenter  ← NEW slot D10 (breadcrumbs — Story 3-5 fills in)
│  │  │    ├─ <FcThemeToggle />
│  │  │    └─ @HeaderEnd
│  │  │
│  │  ├─ <FluentLayoutItem Area=Navigation Width=@SidebarWidth
│  │  │                    Hidden=@(NavState.Value.CurrentViewport is Tablet or Phone)>
│  │  │    @Navigation ?? (Registry.GetManifests().Any() ? <FrontComposerNavigation /> : null)
│  │  │                                        ↓
│  │  │    ┌─ if (ShouldRenderCollapsedRail())  ← D7, D9
│  │  │    │    <FcCollapsedNavRail />         ← 48px, one icon per BC
│  │  │    │
│  │  │    └─ else
│  │  │         <FluentNav UseIcons=true UseSingleExpanded=false aria-label=@Localizer["NavMenuAriaLabel"]>
│  │  │             @foreach (manifest in Registry.GetManifests())
│  │  │                <FluentNavCategory Id=@BC Title=@Name
│  │  │                                   Expanded=@(!IsGroupCollapsed(BC))
│  │  │                                   ExpandedChanged=@(…)>
│  │  │                    @foreach (projection in manifest.Projections)
│  │  │                       <FluentNavItem Href=@BuildRoute(BC, projection)>
│  │  │                          @ProjectionLabel(projection)   ← Story 3-5 adds badge here
│  │  │                       </>
│  │  │                </>
│  │  │         </FluentNav>
│  │  │
│  │  ├─ <FluentLayoutItem Area=Content Padding=@Padding.All3>
│  │  │    <FcLayoutBreakpointWatcher />   ← NEW, zero-DOM subscribes to fc-layout-breakpoints.js
│  │  │    <FcSystemThemeWatcher />        ← from Story 3-1
│  │  │    @ChildContent
│  │  │
│  │  └─ <FluentLayoutItem Area=Footer>
│  │       @Footer ?? "Hexalith FrontComposer © @DateTime.Now.Year"
│  │
│  ├─ <FluentProviders />
│  └─ <Fluxor.Blazor.Web.StoreInitializer />
└─
```

`SidebarWidth` is derived from the viewport tier + collapsed flag:
- Desktop + NOT collapsed → `220px`
- Desktop + collapsed (user toggle) → `48px`
- CompactDesktop → `48px`
- Tablet / Phone → layout item Hidden (`FluentLayoutHamburger` drawer is the only entry)

## Viewport Tier State Machine

```
JS matchMedia compute tier:
  if (min-width: 1366px) matches        → 3 (Desktop)
  else if (min-width: 1024px) matches   → 2 (CompactDesktop)
  else if (min-width: 768px) matches    → 1 (Tablet)
  else                                  → 0 (Phone)

On subscribe: one invokeMethodAsync("OnViewportTierChangedAsync", currentTier)
On each matchMedia 'change' event:
  compute new tier
  if newTier == lastTier → no dispatch (dedupe D6)
  else invokeMethodAsync("OnViewportTierChangedAsync", newTier)

C# side (FcLayoutBreakpointWatcher):
  Dispatcher.Dispatch(new ViewportTierChangedAction((ViewportTier)tier))

Reducer:
  state with { CurrentViewport = NewTier }

Effects:
  HandlePersistNavigation is NOT wired to ViewportTierChangedAction (D14 / ADR-037)
```

## NavigationState Persistence Schema

```
Storage key: {tenantId}:{userId}:nav

Blob (JSON via System.Text.Json defaults):
{
  "SidebarCollapsed": true,
  "CollapsedGroups": {
    "Counter": true,
    "Orders": false
  }
}

Schema invariants (locked by NavigationPersistenceSnapshotTests.cs .verified.txt):
- NO "CurrentViewport" / "viewport" / "tier" property (ADR-037)
- "CollapsedGroups" is flat string→bool (no nesting)
- Sparse convention: only bounded contexts the user explicitly collapsed carry
  entries. Expanded is default; removal on expand rather than value=false (D11).
- System.Text.Json default camel-casing is NOT applied (properties are Pascal-cased
  to match the C# record). If a future bump enables camelCase, it's a schema break
  requiring migration (Story 9-2).
```

## Route Convention Reference

`BuildRoute(boundedContext, projectionFqn)`:

1. Extract type name: after last `.` in FQN (or the whole string if no `.`).
2. Lowercase the bounded context.
3. Convert type name PascalCase → kebab-case (`CounterView` → `counter-view`).
4. Compose: `/{boundedContextLower}/{typeNameKebab}`.

Examples:
- `Counter` + `Counter.Domain.Projections.CounterView` → `/counter/counter-view`
- `Orders` + `Orders.Projections.OpenOrdersProjection` → `/orders/open-orders-projection`
- `Inventory` + `StockLevel` (no namespace) → `/inventory/stock-level`

`ProjectionLabel(projectionFqn)`:

1. Extract type name (same as BuildRoute).
2. Insert spaces before each uppercase letter (except the first): `CounterView` → `Counter View`.
3. Return the resulting string.

Adopters wanting different routes or labels override `FrontComposerShell.Navigation` with a custom fragment (D18 override path).

## Fluent UI v5 FluentNav Reference

Per MCP `get_component_details("FluentNav")`:

- `FluentNav` container — supports `UseIcons` (bool, default true), `UseSingleExpanded` (bool, default false), `Density` (NavDensity?).
- `FluentNavCategory` — has `Id` (string, needed for programmatic expand/collapse), `Title`, `Expanded` (bool, two-way bindable), `ExpandedChanged` (EventCallback<bool>). NO `Icon` on sub-items (v5 renders hierarchy via indentation — UseIcons applies only to top-level categories/items).
- `FluentNavItem` — has `Href` OR `OnClick` (mutually exclusive per docs), `IconRest` / `IconActive`. When used inside `FluentNavCategory`, `IconRest` / `IconActive` are IGNORED (no icon rendered for sub-items).
- `FluentNav.CollapseCategoryAsync(id)` / `ExpandCategoryAsync(id)` — programmatic control via `@ref`.
- **No icon-only layout** — FluentNav v5 docs explicitly state "Nav doesn't support an icon-only layout." This is why 3-2 ships a separate `FcCollapsedNavRail` for the CompactDesktop + manual-collapse cases (D13).
- Keyboard — FluentNav uses roving tabindex internally. Arrow up/down move between items; Home/End jump to boundaries; Enter/Space activate. Tabbing away remembers the last focused item.

Per MCP `get_component_details("FluentLayoutHamburger")`:

- Default `Display = HamburgerDisplay.MobileOnly` (<768 px only).
- Setting `Visible = true` forces the hamburger icon to show regardless of viewport.
- `PanelPosition` (default `Start`), `PanelSize` (default `Medium`), `PanelHeader` (optional string) — all accept defaults for 3-2.
- `OnOpened` event callback fires when the drawer opens OR closes (check `args.Opened`).
- `ChildContent` overrides the drawer content; when null, FluentLayoutHamburger uses the `Navigation` `FluentLayoutItem`'s content — which is exactly `<FrontComposerNavigation />` after D18 auto-populate. No additional wiring needed.

Per MCP `get_component_details("FluentLayout")`:

- `MobileBreakdownWidth` (default 768). Below this, Navigation is automatically hidden and replaced by the hamburger drawer — Story 3-2 relies on the native behavior at the 768 px boundary (Tablet → Phone + Tablet hamburger-only already works).
- `OnBreakpointEnter` — single-breakpoint callback (at `MobileBreakdownWidth`). 3-2 does NOT use this callback; `FcLayoutBreakpointWatcher` via `matchMedia` provides the full 3-tier visibility that Story 3-3 + 3-4 + 3-5 need (ADR-036).

## Files Touched Summary

**Created (17 files):**
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

**Modified (5 files):**
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` + `.razor.cs` (append `HeaderCenter` param + auto-populate + watcher mount)
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` (5 new keys)
- `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` (delete adopter nav block)

**Created tests (7 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.cs` (+ `.verified.txt`)
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/SidebarResponsiveE2ETests.cs`

**Modified tests (3 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` (5 new key lookups)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs` (parameter count 8 → 9; verified.txt updated)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` (+ 2 tests for auto-populate)

No changes to `Contracts/`, `SourceTools/`, or `EventStore/` projects. No new `AnalyzerReleases.*.md` entries.

## Testing Standards

- xUnit v3, Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited.
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — reused if timing matters (not required for 3-2).
- `TestContext.Current.CancellationToken` on async tests.
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI for Verify.
- `BunitJSInterop` strict mode: explicit `.SetupVoid` / `.Setup<T>` for `fc-layout-breakpoints.js` interop calls.
- **Test count budget (L07):** ~33 new tests / 22 decisions ≈ 1.5 per decision — right at Murat's under-coverage floor. PR-review gate at Task 10.12 confirms or trims.

## Build & CI

- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story.
- No new `AnalyzerReleases.*.md` entries — HFC2105 + HFC2106 are runtime-only and already reserved by Story 3-1.
- Scoped CSS emits `{AssemblyName}.styles.css` automatically — `FcCollapsedNavRail.razor.css` + `FrontComposerNavigation.razor.css` ride the existing scoped-CSS bundle (Story 3-1 ADR-034 filename contract still holds).
- No new CI jobs — everything rides `dotnet build` + `dotnet test`.
- Playwright E2E in Task 10.11 runs via the Story 3-1 harness (Aspire MCP if available per `feedback_no_manual_validation.md`).

## Previous Story Intelligence

**From Story 3-1 (immediate predecessor):**
- **Sharded story format** — index.md + per-section markdown files. 3-2 follows the same structure.
- **L06 budget discipline** — 3-1 landed at 29 decisions (infrastructure, ≤ 40). 3-2 lands at 22 decisions — comfortably below the cap; navigation is a focused domain compared to 3-1's shell skeleton.
- **L07 test-to-decision ratio** — 3-1 at 1.48 post-elicitation. 3-2 at 1.5 — consistent; infrastructure-heavy stories in Epic 3 settle around the 1.5 floor.
- **`FcShellOptions` growth** — 3-1 bumped to 14 properties; 3-2 does NOT extend it (D22). G1 deferral to Story 9-2 still current.
- **ADR-029 `IUserContextAccessor` fail-closed** — reused verbatim for `NavigationEffects`. No new HFC2xxx code reserved.
- **ADR-030 `IStorageService` Scoped lifetime** — `NavigationEffects` picks up the Scoped storage via constructor injection; no new DI lifecycle consideration.
- **ADR-032 `@if (false)` hidden placeholder pattern** — 3-2 applies the same discipline to the `HeaderCenter` slot (empty by default, populated by 3-5) rather than rendering placeholder breadcrumb markup.
- **D25 Counter.Web adopter nav preservation** — explicit handoff executed in Task 9. The preservation was scoped as a sprint-gap measure; 3-2 retires it per the original handoff language.
- **D26 hidden placeholder buttons** — 3-2 does NOT unhide Ctrl+K or Settings. Those remain hidden until Stories 3-3 (Settings) and 3-4 (Command Palette) ship.

**From Story 2-4:**
- **`FcShellOptions` append-only discipline** — 3-2 honors by NOT extending the class (D22). Validates that discipline can include "don't extend" as a valid choice.
- **`IStringLocalizer<FcShellResources>`** — 3-2 consumes the infrastructure 3-1 introduced; adds 5 new keys per D19.

**From Story 2-3:**
- **Single-writer invariant (D19)** — 3-2's reducer set is the single write path into `FrontComposerNavigationState`. No bypass; `FcCollapsedNavRail`, `FcHamburgerToggle`, `FrontComposerNavigation` all dispatch through the action pipeline.
- **ULID correlation pattern** — reused via `IUlidFactory.CreateString()` for every dispatched action.

**From Story 2-2:**
- **`IUserContextAccessor` fail-closed (D31)** — directly inherited via `NavigationEffects.TryResolveScope`.
- **Fluxor feature producer-consumer discipline (L02)** — 3-2's new `FrontComposerNavigationFeature` is introduced alongside its producers (dispatched actions) and consumers (Stories 3-3 / 3-5 / 3-6) named in the cross-story contract table. No "half a state machine" risk.

**From Story 1-6:**
- **Counter.Web sample + `Counter.Domain` projections** — `DomainManifest.Projections` FQN shape confirmed; `BuildRoute` convention tested against real-world data.

**From Story 1-3:**
- **Per-concern Fluxor features** — 3-2 adds `Navigation` as the fourth feature (Theme / Density / DataGridNavigation already shipped). Follows the same `Shell/State/{Concern}/` layout.

## Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Cross-story contract table in Critical Decisions names both producer and consumer for every seam. ADR-035/036/037 document cross-story seams.
- **L02 Fluxor feature producer+consumer scope** — `FrontComposerNavigationFeature` PRODUCER stories: 3-2 (this story, dispatches all 5 actions). CONSUMER stories: 3-3 (density override via `ViewportTier`), 3-4 (command palette tier), 3-5 (badge counts + home), 3-6 (session persistence). Shipping the feature with ALL producers + effects in 3-2 avoids Story 2-2's "half a state machine" risk.
- **L03 Tenant/user isolation fail-closed** — D12 inherits Story 3-1 ADR-029 verbatim. Memory feedback `feedback_tenant_isolation_fail_closed.md` honored.
- **L04 Generated name collision detection** — Not applicable; 3-2 does not extend the source generator.
- **L05 Hand-written service + emitted per-type wiring** — Not applicable; 3-2 infrastructure is hand-written only. The registry is consumed read-only.
- **L06 Defense-in-depth budget** — 22 decisions, well below the 40 infrastructure cap. Room for a review round (party mode + advanced elicitation) to add up to 10 more without hitting the cap.
- **L07 Test count inflation** — 33 tests / 22 decisions ≈ 1.5 — at Murat's under-coverage floor. Task 10.12 PR-review gate decides whether to add or trim.
- **L08 Party review vs. elicitation** — 3-2 has NOT yet been reviewed via party mode or advanced elicitation. Recommended flow before `dev-story` execution: `/bmad-party-mode` with Winston / Sally / Murat / Amelia → apply findings → `/bmad-advanced-elicitation` (Pre-mortem / Red Team / Chaos / Hindsight) → `dev-story`. Key areas for review: (a) auto-populate D18 (Sally — is the "null fragment" check a hidden contract?); (b) ADR-036 matchMedia dedupe (Chaos — what if two events fire in the same tick?); (c) the `FrontComposerNavigation.ShouldRenderCollapsedRail()` path swap (Amelia — does the FluentNav unmount/remount cost perf during a tier transition?).
- **L09 ADR rejected-alternatives discipline** — ADR-035 cites 4 rejected, ADR-036 cites 4, ADR-037 cites 3. All ≥ 2 satisfied.
- **L10 Deferrals name a story** — All 17 Known Gaps cite specific owning stories (3-3, 3-4, 3-5, 3-6, 4-1, 9-4, 10-2, v1.x, v2). Epic-4 is used for G3/G4 because Epic 4's scope (projection rendering) is the natural home; a future Epic 4 story file will pick up the specific seam.
- **L11 Dev Agent Cheat Sheet** — Present. Infrastructure story with 8 new components + JS module + 5 resource keys warrants fast-path entry. 3-2's cheat sheet is shorter than 3-1's (~4 KB vs 12 KB) because the dependency graph is smaller and decisions ride the 3-1 infrastructure.

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-composition-shell-navigation-experience.md#Story 3.2 — AC source of truth, §60-108]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR17 Collapsible sidebar with bounded context groups]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR15 Sidebar navigation pattern]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR28 Responsive breakpoint matrix]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR29 Touch target guarantees at tablet/phone]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR30 Keyboard parity]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#NFR89 ≤ 2 clicks to any domain]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md#Breakpoint Behavior Matrix, §22-37]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/design-direction-decision.md#Sidebar behavior, §168-173]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/design-direction-decision.md#Navigation structure rule, §141]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/design-system-foundation.md#Component mapping — FluentNav, §30-31]
- [Source: _bmad-output/planning-artifacts/architecture.md#IFrontComposerRegistry + DomainManifest, §411-436]
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern Fluxor Features, §527-536]
- [Source: _bmad-output/planning-artifacts/architecture.md#Shell source tree — State/Navigation, §945]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/critical-decisions-read-first-do-not-revisit.md#D25 Counter.Web Navigation preservation]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/critical-decisions-read-first-do-not-revisit.md#D4 FrontComposerShell parameter surface append-only]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-029 IUserContextAccessor fail-closed]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-030 IStorageService Scoped]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/critical-decisions-read-first-do-not-revisit.md#D31 NullUserContextAccessor fail-closed]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01-L11 — all lessons applied]
- [Source: memory/feedback_no_manual_validation.md — Aspire MCP + Playwright over manual validation for Task 9.2 + 10.11]
- [Source: memory/feedback_cross_story_contracts.md — cross-story contract table per ADR-016 canonical example]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D12 inherits Story 3-1 ADR-029]
- [Source: memory/feedback_defense_budget.md — 22 decisions, under ≤ 40 infrastructure cap]
- [Source: src/Hexalith.FrontComposer.Contracts/Registration/IFrontComposerRegistry.cs — consumed read-only]
- [Source: src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs — projections list consumed verbatim]
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs — scope-guard pattern mirrored by NavigationEffects]
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs — pattern twin]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSystemThemeWatcher.razor.cs — FcLayoutBreakpointWatcher mirrors]
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-prefers-color-scheme.js — fc-layout-breakpoints.js mirrors]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor — modified in Task 8.3]
- [Source: samples/Counter/Counter.Web/Components/Layout/MainLayout.razor — rewired in Task 9.1]
- [MCP: `get_component_details("FluentNav")` — FluentNav v5 surface verified]
- [MCP: `get_component_details("FluentLayoutHamburger")` — Visible=true on ≥768px confirmed]
- [MCP: `get_component_details("FluentLayout")` — MobileBreakdownWidth default 768 px confirmed]

## Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §919-982, §945):
  - New `Shell/State/Navigation/` subfolder matches architecture §945 (`Navigation/` listed under Shell State features).
  - `Shell/Components/Layout/` subfolder already exists from Story 3-1 — `FrontComposerNavigation` + `FcCollapsedNavRail` + `FcHamburgerToggle` + `FcLayoutBreakpointWatcher` all land there.
  - `wwwroot/js/fc-layout-breakpoints.js` — matches the existing `wwwroot/js/` convention from Story 3-1 (`fc-beforeunload.js`, `fc-prefers-color-scheme.js`, `fc-expandinrow.js`).
  - No Contracts → Shell reverse references. `DomainManifest` is already in Contracts and consumed read-only.
  - `FrontComposerNavigation.razor.cs` references `Microsoft.FluentUI.AspNetCore.Components` — transitive via the shell's existing package reference; no `.csproj` edits.
- **Fluent UI `Fc` prefix convention** honored — `FcLayoutBreakpointWatcher`, `FcHamburgerToggle`, `FcCollapsedNavRail`.
- **`FrontComposer{Concern}` naming** for framework-owned non-adopter-facing layout components — `FrontComposerNavigation` mirrors `FrontComposerShell`.
- **`FcShellOptions` extension NOT honored** — 3-2 deliberately declines to extend (D22). The 3-1 G1 deferral stays current.

---
