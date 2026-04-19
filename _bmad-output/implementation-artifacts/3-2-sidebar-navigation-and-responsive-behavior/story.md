# Story

## Story Statement

**As a** business user,
**I want** a collapsible sidebar with bounded-context navigation groups that adapts to my screen size,
**So that** I can quickly navigate between domains and still have a usable experience on smaller screens.

## Story Goal (one sentence)

Ship the framework-owned `FrontComposerNavigation` sidebar component that auto-populates `FrontComposerShell.Navigation` from `IFrontComposerRegistry.GetManifests()` as collapsible `FluentNavCategory` groups (projection items only, commands excluded), persists sidebar-collapsed + per-group state per tenant/user via `LocalStorageService`, and adapts across four viewport tiers (Desktop ≥1366 expanded / CompactDesktop 1024–1365 icon-rail / Tablet 768–1023 drawer / Phone <768 drawer) driven by a JS `matchMedia`-backed `FcLayoutBreakpointWatcher` — all behind the Story 3-1 zero-override invariant and fail-closed tenant scoping (ADR-029).

## Scope Statement

Story 3-2 is the **Epic 3 navigation body** that lifts Counter.Web out of the adopter-authored `FluentNav` stopgap introduced as Story 3-1 Decision D25. It delivers the first responsive tier-awareness the framework owns (Desktop / CompactDesktop / Tablet / Phone) and establishes the `ViewportTier` enum + `NavigationState` Fluxor feature that Stories 3-3 (density override at <1024 px), 3-4 (command palette result layout), and 3-5 (sidebar badge counts) all consume.

**In scope:**

- `FrontComposerNavigation.razor` + `.razor.cs` + `.razor.css` in `Hexalith.FrontComposer.Shell/Components/Layout/`, a framework-owned sidebar that `[Inject]`s `IFrontComposerRegistry`, renders one `FluentNavCategory` per `DomainManifest` (grouping key = `BoundedContext`, display label = `Name`), and one `FluentNavItem` per projection type in the manifest. Commands are excluded (projection-view-only rule per UX spec §141). Nav item route is convention-based per D2.
- `FcCollapsedNavRail.razor` + `.razor.cs` — a 48 px vertical icon rail used at the **CompactDesktop** viewport tier only. One `FluentTooltip`-wrapped `FluentIcon` per bounded context; clicking forces the sidebar open as an overlay.
- `FcHamburgerToggle.razor` — a thin wrapper around `FluentLayoutHamburger` that lives in `FrontComposerShell.HeaderStart`. `Visible` binding is derived from `NavigationState.CurrentViewport != Desktop` (plus the user's manual toggle at Desktop) via Fluxor `IStateSelection`.
- `FcLayoutBreakpointWatcher.razor` — headless component that imports `fc-layout-breakpoints.js`, subscribes on `OnAfterRenderAsync(firstRender)`, and dispatches `ViewportTierChangedAction(ViewportTier)` every time the window crosses a boundary (1366 / 1024 / 768 px).
- `wwwroot/js/fc-layout-breakpoints.js` — ES module mirroring the shape of `fc-prefers-color-scheme.js` (subscribe / unsubscribe, one-shot emission at subscribe).
- `Shell/State/Navigation/` Fluxor feature: `FrontComposerNavigationState`, `NavigationActions`, `NavigationReducers`, `NavigationEffects`, `FrontComposerNavigationFeature`, `ViewportTier` enum.
- `FrontComposerShell` gains **one NEW append-only parameter**: `HeaderCenter` (RenderFragment?) for breadcrumbs. Story 3-5 populates the breadcrumb content; 3-2 only ships the slot. Parameter count 8 → 9 (D4 Story 3-1 append-only discipline honored).
- Conditional auto-population of `FrontComposerShell.Navigation` slot: when adopter leaves the slot null AND `IFrontComposerRegistry.GetManifests()` returns at least one manifest, shell renders `<FrontComposerNavigation />` automatically. Adopter-supplied `Navigation` fragment still wins (override escape hatch preserved).
- `NavigationEffects` mirrors `ThemeEffects` / `DensityEffects` fail-closed pattern — `IUserContextAccessor` null/empty/whitespace → log `HFC2105` Information + skip persistence (Story 3-1 D8 / ADR-029).
- Persistence blob under storage key `{tenantId}:{userId}:nav` — single JSON document with `SidebarCollapsed` + `CollapsedGroups` (per bounded-context map). `ViewportTier` is derived at runtime and NOT persisted (see ADR-037).
- 5 new resource keys in `FcShellResources.resx` + `.fr.resx`: `NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `NavGroupExpandAriaLabel`, `NavGroupCollapseAriaLabel`, `SkipToNavigationLabel`. Parity test already enforces matching FR translations.
- Counter.Web `MainLayout.razor` rewired: the adopter-authored `FluentNav` block (Story 3-1 D25 preservation) is DELETED; `@inject IFrontComposerRegistry` is removed; the layout collapses to its three-line form (`<FrontComposerShell>@Body</FrontComposerShell>`).
- Keyboard focus invariants (DOM tab order, `--colorStrokeFocus2` inheritance, skip-to-navigation link) verified via bUnit assertions. Playwright screenshot diff of the focus ring remains Story 10-2 scope.
- Playwright resize E2E smoke test (Task 10.11) — boot Counter.Web, resize browser to 1200 px → assert icon rail; resize to 800 px → assert hamburger-only; assert sidebar state persists across refresh at the same viewport.

**Out of scope (Known Gaps / downstream stories):**

- Badge counts on nav items (ActionQueue projections render count suffix) → **Story 3-5**. `FluentNavItem` content in 3-2 is forward-compatible — Story 3-5 wraps the label with a `<span>` + badge without parameter-surface change.
- Density override at <1024 px (forced comfortable for 44 px touch targets) → **Story 3-3** owns density state + its `ViewportTier` consumer. 3-2 ships the tier enum so 3-3 doesn't duplicate breakpoint logic.
- Command-palette contextual results at current bounded context → **Story 3-4**.
- Home directory + "new capability" indicators on nav items → **Story 3-5**.
- DataGrid / filter state restoration when navigating between projection views → **Story 3-6**.
- Projection role-hint metadata (ActionQueue / StatusOverview / Timeline) in `DomainManifest` — 3-2 does NOT modify `DomainManifest`; projection type name is rendered verbatim as the nav label. Role-hints + friendly display names land in **Epic 4** (projection rendering) which extends `DomainManifest`.
- Routing helpers / URL generation beyond the convention at D2 — adopters using non-conventional routes override `Navigation` slot.
- Drag-to-reorder nav items / favourites → **v2** (UX spec §37).
- Breadcrumb content (`HeaderCenter` slot) — 3-2 ships the empty slot; Story 3-5 (or a 3-5 follow-up) populates it.
- Dev-mode overlay at phone viewport — explicitly unsupported per UX spec responsive matrix; 3-2 codifies the non-support via an `@if (viewport != Phone)` guard when the overlay eventually lands (Story 6-5).
- Per-tenant accent in the sidebar → **v1.x** per architecture §598.
- RTL verification + specimen baselines → **v2** per UX spec §36.
- Windows High Contrast + `forced-colors` verification → **Story 10-2**.
- `axe-core` / screenshot diff automation → **Story 10-2**.

## Success Metric (observable)

- `dotnet test` passes with the six new test suites (`FrontComposerNavigationTests`, `FcCollapsedNavRailTests`, `FcHamburgerToggleTests`, `FcLayoutBreakpointWatcherTests`, navigation state + effects scope tests, Counter.Web integration test, Playwright resize smoke) — target **~42 new `[Fact]`/`[Theory]` declarations** on top of the Story 3-1 baseline (raised from ~33 on 2026-04-18 to absorb party-mode review additions: projections-empty category guard, JS dedup + skip-tier coverage, hydrate-does-not-re-persist, whitespace-scope fail-closed, and no-storage-call assertions alongside log assertions; raised further from ~38 to ~42 on 2026-04-19 to absorb advanced-elicitation review additions: watcher import-failure graceful-degrade, hydrate corrupt-blob logs-and-defaults, persist storage-failure logs-not-throws, and skip-to-navigation DOM-anchor assertion).
- Counter.Web boots with the framework-owned sidebar visible; the "Counter" category contains exactly one projection item; the adopter `MainLayout.razor` is back to three lines.
- Resize from 1920 px → 1200 px → 800 px → 600 px produces Desktop → CompactDesktop → Tablet → Phone transitions observable in Fluxor DevTools (dispatch trace), with the sidebar swapping between full FluentNav, icon rail, and drawer per the UX breakpoint matrix.
- Zero new build warnings. No regression in the Story 3-1 baseline (post-3-1 baseline rebaselined at Task 0.1).
- Bench: sidebar render + viewport-change dispatch latency ≤ 16 ms on Counter.Web (Blazor Server) using the Story 2-4 E2E harness methodology (added Playwright trace in Task 10.11).
