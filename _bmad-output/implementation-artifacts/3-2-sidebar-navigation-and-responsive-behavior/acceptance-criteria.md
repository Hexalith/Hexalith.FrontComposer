# Acceptance Criteria

> Seven ACs distilled from Epic 3 §60-108 + UX spec §22-37 + §168-173. Each cites the binding decisions, the tasks that deliver it, and the tests that verify it.

---

## AC1: `FrontComposerNavigation` renders one `FluentNavCategory` per bounded context with projection items only

**Given** at least one `DomainManifest` is registered in `IFrontComposerRegistry`
**When** `<FrontComposerNavigation />` renders inside `FrontComposerShell.Navigation`
**Then** the output contains one `<FluentNavCategory Id="@boundedContext" Title="@name">` for each manifest
**And** each category contains one `<FluentNavItem Href="/{boundedContext-lowercase}/{projectionTypeName-kebab-case}">@projectionLabel</FluentNavItem>` for each FQN in `DomainManifest.Projections`
**And** no `FluentNavItem` is rendered for entries in `DomainManifest.Commands` (commands are NOT nav items)
**And** when a manifest's `Projections` collection is empty, its `FluentNavCategory` is NOT rendered at all (commands-only manifests produce no sidebar entry per D1)
**And** `FluentNav.UseIcons = true` so category chevrons render per UX spec §30

**References:** D1, D2. **Tasks:** 6.1, 6.2. **Tests:** `FrontComposerNavigationTests.RendersOneCategoryPerManifest`, `FrontComposerNavigationTests.RendersOneItemPerProjection`, `FrontComposerNavigationTests.DoesNotRenderCommandsAsNavItems`, `FrontComposerNavigationTests.HidesCategoryWhenProjectionsEmpty`, `FrontComposerNavigationTests.BuildRouteProducesExpectedHref` (Task 10.1).

---

## AC2: Sidebar collapse preference and per-group expanded state persist per tenant/user

**Given** a user has `IUserContextAccessor` returning non-null tenant + user AND toggles the sidebar collapsed / expands a nav group / collapses a nav group
**When** `SidebarToggledAction` / `NavGroupToggledAction` / `SidebarExpandedAction` dispatches through Fluxor
**Then** `NavigationEffects.HandlePersistNavigation` serialises `NavigationPersistenceBlob(SidebarCollapsed, CollapsedGroups)` as JSON
**And** writes to `IStorageService` under key `StorageKeys.BuildKey(tenantId, userId, "nav")`
**And** a page refresh triggers `NavigationEffects.HandleAppInitialized` which loads the blob and dispatches `NavigationHydratedAction`
**And** the reducer replaces `SidebarCollapsed` + `CollapsedGroups` wholesale
**And** `NavigationHydratedAction` does NOT trigger a subsequent `storage.SetAsync` write (hydrate is read-only per D14 amendment + ADR-038)
**And** `ViewportTier` is NOT present in the serialised blob (ADR-037)

**Given** `IUserContextAccessor.TenantId` or `UserId` is null / empty / whitespace
**When** any persist or hydrate effect runs
**Then** the effect logs `HFC2105_StoragePersistenceSkipped` at Information severity
**And** `storage.SetAsync` / `storage.GetAsync` is NOT called (asserted separately from the log assertion — both must hold, per Murat's party-mode feedback)

**References:** D12, D14, D15, D21, D23. **Tasks:** 3.1, 3.2. **Tests:** `NavigationEffectsScopeTests.PersistsOnValidScope`, `NavigationEffectsScopeTests.SkipsOnNullTenant`, `NavigationEffectsScopeTests.SkipsOnNullUser`, `NavigationEffectsScopeTests.SkipsOnWhitespaceUserContext`, `NavigationEffectsScopeTests.HydrateDoesNotRePersist`, `NavigationPersistenceSnapshotTests.BlobSchemaLocked` (Tasks 10.6, 10.7).

---

## AC3: Desktop (≥1366px) renders expanded 220 px sidebar; new users start expanded

**Given** the browser viewport width is ≥ 1366 px
**When** `FcLayoutBreakpointWatcher` subscribes on first render
**Then** the initial emission dispatches `ViewportTierChangedAction(ViewportTier.Desktop)`
**And** `FrontComposerShell.Navigation` area renders at 220 px width with `<FrontComposerNavigation />` in full FluentNav form (labels + chevrons visible)
**And** for a new user with no persisted `SidebarCollapsed`, the feature default is `false` (expanded) per UX spec §170
**And** `FluentLayoutHamburger.Visible = false`

**References:** D3, D4, D7. **Tasks:** 1.1, 1.2, 1.3, 6.1. **Tests:** `NavigationReducerTests.ViewportTierChangedUpdatesCurrentViewport`, `FrontComposerNavigationTests.RendersOneCategoryPerManifest` (under Desktop default) (Tasks 10.1, 10.5).

---

## AC4: Compact desktop (1024–1365px) auto-collapses to `FcCollapsedNavRail` icon rail

**Given** the browser viewport width is between 1024 and 1365 px inclusive
**When** `fc-layout-breakpoints.js` reports `CompactDesktop` (tier int 2)
**Then** `ViewportTierChangedAction(ViewportTier.CompactDesktop)` dispatches
**And** the Navigation area renders `<FcCollapsedNavRail />` in a 48 px column
**And** each rail button shows a Fluent icon (default `Icons.Regular.Size20.Apps` per D13) with a `FluentTooltip` anchored to the rail showing the bounded-context `Name`
**And** clicking a rail button dispatches `SidebarExpandedAction(correlationId)` which flips `SidebarCollapsed = false` (expanding the sidebar as an overlay via `FluentLayoutHamburger`)
**And** `FluentLayoutHamburger.Visible = true` so the user can also expand the drawer manually
**And** breadcrumbs (when populated by Story 3-5's `HeaderCenter` content) may truncate with ellipsis (UX spec responsive matrix)

**References:** D7, D13. **Tasks:** 5.1, 7.1, 7.2, 8.1. **Tests:** `FcCollapsedNavRailTests.RendersOneButtonPerManifest`, `FcCollapsedNavRailTests.TooltipContainsBoundedContextName`, `FcCollapsedNavRailTests.ClickDispatchesSidebarExpanded`, `FcHamburgerToggleTests.VisibleAtCompactDesktop`, `SidebarResponsiveE2ETests.ResizeToCompactDesktopShowsIconRail` (Tasks 10.2, 10.3, 10.11).

---

## AC5: Tablet (768–1023px) and Phone (<768px) render drawer navigation via `FluentLayoutHamburger`

**Given** the browser viewport width is between 768 and 1023 px (Tablet) or below 768 px (Phone)
**When** `fc-layout-breakpoints.js` reports the tier
**Then** `ViewportTierChangedAction` dispatches with `Tablet` or `Phone`
**And** the Navigation `FluentLayoutItem` is hidden
**And** `FluentLayoutHamburger.Visible = true` and clicking it opens `<FrontComposerNavigation />` inside the hamburger drawer panel
**And** drawer nav items render at ≥ 48 px height per UX spec responsive matrix (inherited from `FluentLayoutHamburger.PanelSize` defaults — no custom override)

**Given** Phone tier
**When** the shell renders
**Then** the layout is single-column (Navigation area hidden, Content fills the viewport)
**And** dev-mode overlay is not supported (UX spec — confirmed by compile-away guard in future Story 6-5)

**References:** D7, D8. **Tasks:** 5.1, 8.1. **Tests:** `FcHamburgerToggleTests.VisibleAtTablet`, `FcHamburgerToggleTests.VisibleAtPhone`, `FcLayoutBreakpointWatcherTests.DispatchesTierOnChange`, `SidebarResponsiveE2ETests.ResizeToTabletShowsDrawerOnly` (Tasks 10.3, 10.4, 10.11).

---

## AC6: Keyboard navigation visits every nav item in DOM order with `--colorStrokeFocus2` focus ring

**Given** keyboard focus starts outside the shell
**When** the user presses Tab repeatedly
**Then** focus moves through the DOM order: `skip-to-content link` → `skip-to-navigation link` → `hamburger button` (when visible) → `HeaderCenter fragment` → `app-title region` → `theme toggle` → `HeaderEnd fragment` → `first FluentNavCategory header` → `first FluentNavItem` → ... → `main content`
**And** the focus ring uses Fluent UI's `--colorStrokeFocus2` CSS custom property (no scoped-CSS override)
**And** the skip-to-navigation link (`SkipToNavigationLabel` from `FcShellResources`) is visually hidden until focused, at which point it becomes a click-to-skip affordance
**And** the skip-to-navigation link's `href="#fc-nav"` points to the Navigation `FluentLayoutItem` element which carries `id="fc-nav"` — the anchor resolves whether the Navigation slot is auto-populated OR adopter-supplied (Task 8.3a)
**And** every `FluentNavItem` is reachable (no items with `tabindex="-1"` outside the roving tabindex pattern FluentNav manages internally)

**References:** D16, D19. **Tasks:** 6.1, 8.3a (added 2026-04-19), 8.4. **Tests:** `FrontComposerNavigationTests.NavItemsAreTabReachable` (bUnit finds all `<a>` / `<button>` inside the nav and asserts no `tabindex="-1"` on focusable items), `FrontComposerShellTests.RendersSkipToNavigationLinkWithCorrectAnchor` (Task 10.10 test 3, added 2026-04-19), resource parity test covers `SkipToNavigationLabel` (Tasks 10.1, 10.8, 10.10). **Deferred to Story 10-2:** Playwright screenshot diff of the focus ring across themes (automated visual regression).

---

## AC7: Counter.Web `MainLayout.razor` delegates navigation entirely to the framework-owned `FrontComposerNavigation`

**Given** Counter.Web is rebuilt after Task 9
**When** `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` is inspected
**Then** the file contains `@inherits LayoutComponentBase` + `@using Hexalith.FrontComposer.Shell.Components.Layout` + `<FrontComposerShell>@Body</FrontComposerShell>` and NOTHING else (three substantive lines)
**And** the `@inject IFrontComposerRegistry` directive is removed
**And** the inline `<FluentNav>` + `@foreach (var manifest in Registry.GetManifests())` block is removed
**And** when Counter.Web boots, the sidebar renders one `FluentNavCategory` for the `Counter` bounded context containing one `FluentNavItem` per projection in `Counter.Domain` — auto-populated via D18

**References:** D17, D18. **Tasks:** 9.1. **Tests:** `CounterWebIntegrationTests.MainLayoutIsThreeLines` (regex or AST-based file-shape assertion), `FrontComposerShellTests.AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty`, `FrontComposerShellTests.AdopterSuppliedNavigationFragmentWins`, `SidebarResponsiveE2ETests.CounterWebShellBootsAtDesktop` (Tasks 10.10, 10.11).

---
