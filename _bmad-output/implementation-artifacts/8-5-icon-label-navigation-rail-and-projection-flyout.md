---
baseline_commit: 7fe2d5a
---

# Story 8.5: Icon+label navigation rail + projection flyout

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-25. -->
<!-- Senior Developer Review (AI) completed 2026-06-25; auto-fix applied. See review section below. -->

## Story

As an operator,
I want an icon+label navigation rail with an outline-to-filled active state and a projection flyout,
so that navigation is compact and scannable like the Aspire app-bar while keeping the registry hierarchy.

## Acceptance Criteria

1. Given Desktop, when the shell renders navigation, then the primary nav is one rail rendered at 72px labeled when `SidebarCollapsed` is false or 48px icon-only when `SidebarCollapsed` is true; the always-visible desktop hamburger keeps toggling this through the existing `SidebarToggledAction` reducer/effect path, and Mobile/Compact viewports keep using the drawer behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.5-Icon-label-navigation-rail-projection-flyout; src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs; src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs]
2. Given each bounded-context tile, when it renders in either rail width, then it uses a Fluent component affordance with a Fluent icon, localized accessible name, aggregate count badge, and "New" badge parity with the current navigation; the active context uses the filled icon variant, an accent left-bar thread, and `aria-current` without using accent as a background surface. [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor; src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs]
3. Given a bounded-context tile is activated by click, Enter, or Space, then a projection flyout opens from that tile and lists the context's visible projections plus explicit nav entries with count and "New" badges; the existing longest segment-prefix rule still lights exactly one current projection/nav entry, and activation marks the bounded context/projection capability as seen before navigation. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationCapabilityBadgeTests.cs]
4. Given the projection flyout is open, when the operator uses keyboard navigation, then the interaction is fully reachable and recoverable: menu has `role="menu"` semantics, items are keyboard reachable, Enter/Space activates, arrow navigation works or is explicitly delegated to the Fluent component, Esc closes, focus returns to the invoking tile, and disabled/policy-gated entries preserve their current behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story-8.5-Icon-label-navigation-rail-projection-flyout; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#FluentMenu; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#FluentPopover]
5. Given the pinned Fluent UI Blazor package `5.0.0-rc.3-26138.1`, when the chosen `FluentMenu` or `FluentPopover` implementation is rendered in bUnit and, where feasible, Playwright, then anchoring, keyboard behavior, `data-testid`, `role`, and `aria-*` attribute splatting are confirmed before the story is moved to review. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; _bmad-output/project-docs/architecture.md#4.3-Layout-component-policy-project-wide-guideline; tests/e2e]

## Tasks / Subtasks

- [x] Audit current navigation behavior before editing (AC: 1, 2, 3)
  - [x] Read `FrontComposerNavigation.razor`, `.razor.cs`, `.razor.css`, `FcCollapsedNavRail.razor`, `.razor.cs`, `.razor.css`, `FrontComposerShell.razor`, `FrontComposerShell.razor.cs`, `FcHamburgerToggle.razor(.cs)`, and navigation state/effects tests.
  - [x] Confirm the current full-nav and collapsed-rail feature parity: manifests, explicit nav entries, policy gating, disabled reasons, localized labels, count badges, "New" badges, visible-projection filtering, and longest-prefix active route.
  - [x] Do not change navigation persistence schema unless a failing test proves it is unavoidable; `SidebarCollapsed`, `CollapsedGroups`, and `LastActiveRoute` are already persisted.
- [x] Replace the split full-nav/tree plus collapsed rail with one rail behavior (AC: 1, 2)
  - [x] Implement a single rail rendering path in `FrontComposerNavigation` or a narrow child component that handles both 72px labeled and 48px icon-only modes from `FrontComposerNavigationState`.
  - [x] Keep `FrontComposerShell.NavigationWidth` aligned with the rendered rail width: Desktop expanded = 72px, Desktop collapsed/CompactDesktop rail = 48px; sub-CompactDesktop still omits the navigation layout item and uses the drawer.
  - [x] Preserve the default `Navigation` slot override: adopters supplying `FrontComposerShell.Navigation` still replace framework navigation completely.
  - [x] Preserve `FcHamburgerToggle`: Desktop dispatches `SidebarToggledAction`; non-Desktop renders `FluentLayoutHamburger`; CompactDesktop drawer coordination must not regress.
- [x] Render bounded-context tiles with active icon and accent thread (AC: 2)
  - [x] Add or resolve icon pairs through `FcFluentIcons` without adding a Fluent icons NuGet package; keep the string icon contract (`Regular.Size20.*`) for domain manifests.
  - [x] Render each context tile with a Fluent component (`FluentButton` or equivalent) and Fluent layout/text primitives; icon-only mode must have a non-duplicated accessible name.
  - [x] Active context uses filled icon variant plus a left accent indicator; accent may be used as the thread (`--fc-color-accent` / `--fc-accent-base-color`) but must not appear in `background` or `background-color`.
  - [x] Keep aggregate count and BC-level "New" badges keyed by stable `data-testid`s or deliberately migrate tests with equivalent stable selectors.
- [x] Implement projection flyout (AC: 3, 4, 5)
  - [x] Choose `FluentMenu` when its `Trigger`, `OpenMenuAsync`, `CloseMenuAsync`, and `FluentMenuItem` role behavior satisfy anchoring/focus requirements; otherwise use `FluentPopover AnchorId` with an explicit menu/list inside.
  - [x] Flyout contents must include the same visible projections returned by `VisibleProjections(...)` and explicit `FrontComposerNavEntry` items from `EntriesForContext(...)`; orphan nav-entry contexts must remain reachable.
  - [x] Projection/nav-entry activation must preserve current capability behavior: dispatch `CapabilityVisitedAction(bc:...)` and projection capability visited before route navigation.
  - [x] Disabled nav entries render non-navigable affordance plus localized reason; policy-gated entries stay hidden through `AuthorizeView`.
  - [x] Esc closes and returns focus to the invoking tile; Enter/Space opens or activates; arrow behavior is pinned by tests or by a documented Fluent-owned roving-focus assertion.
- [x] Preserve active-route and hierarchy semantics (AC: 2, 3)
  - [x] Keep `NormalizeHref`, `LongestNavPrefix`, `BuildRoute`, `ProjectionLabel`, `VisibleProjections`, `LookupCount`, and `AggregateBoundedContextCount` semantics unless tests prove a narrow helper split is required.
  - [x] Ensure detail pages still light the section ancestor and unrelated routes leave nothing active.
  - [x] Ensure query strings/fragments are stripped before matching and that only one item is active.
- [x] Update localized strings and resources only where required (AC: 2, 4)
  - [x] Reuse `NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, and `NewCapabilityBadgeText`.
  - [x] Add EN and FR `FcShellResources` entries for any new flyout labels, disabled/menu aria labels, or test-visible text.
- [x] Add focused tests (AC: 1, 2, 3, 4, 5)
  - [x] Update `FrontComposerNavigationTests` for 72px labeled Desktop rail, 48px icon-only collapsed rail, CompactDesktop rail/drawer behavior, and absence of the old full `FluentNav` tree if removed.
  - [x] Update `FcCollapsedNavRailTests` or replace them with rail-mode tests if the component is deleted; do not leave stale tests asserting the old expand-on-click behavior if click now opens the flyout.
  - [x] Cover aggregate count/"New" badge parity in both rail modes and projection-level badges inside the flyout. (Review 2026-06-25 correction: this coverage was added in `FrontComposerNavigationTests` — `CountBadge_*`, `ProjectionNewBadge_Matrix`, `MultiProjectionManifest_RendersPerProjectionCountBadges_AndAggregateBcNew`, `BcNewBadge_Absent_WhenBoundedContextAlreadySeen` — not in `FrontComposerNavigationCapabilityBadgeTests`, which was left unchanged and still passes against the rail via `HandleNavItemClickedForTest`.)
  - [x] Extend `FrontComposerNavigationNavEntryTests` for explicit entries inside the flyout, policy gating, disabled reasons, orphan contexts, and longest-prefix single-active behavior.
  - [x] Add bUnit interaction tests for flyout open/close, focus return, role/aria/testid splatting, and Enter/Space/Esc behavior where bUnit can model it.
  - [x] Keep `FcHamburgerToggleTests`, navigation reducer/effect tests, and shell navigation-width tests green.
- [x] Add or update browser evidence where feasible (AC: 4, 5)
  - [x] Add/extend Playwright shell-navigation coverage for keyboard opening, menu item navigation, Esc focus return, and light/dark visual/a11y checks.
  - [x] If local Kestrel/socket permissions block Playwright, record exact commands and blockers in `_bmad-output/implementation-artifacts/tests/test-summary.md` and keep bUnit coverage strong.
- [x] Preserve Epic 8 and repo boundaries (AC: 1-5)
  - [x] Preserve Story 8.1 neutral header/footer, Story 8.2 accent-as-background guard, Story 8.3 optional header logo, and Story 8.4 compact/grid CSS behavior.
  - [x] Do not implement Story 8.6 `FcPageToolbar` or Story 8.7 status icon generator work.
  - [x] Do not edit `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`, package versions, `.slnx`, PublicAPI baselines, pacts, MCP, CLI, schema fingerprint code, or generated `obj/` output.
  - [x] Leave the unrelated modified `_bmad-output/story-automator/orchestration-8-20260625-123921.md` alone unless the dev-story workflow explicitly owns a new change to it.
- [x] Verify and record evidence (AC: 1-5)
  - [x] Run `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false`.
  - [x] Run focused Shell lanes: `FrontComposerNavigationTests`, `FrontComposerNavigationCapabilityBadgeTests`, `FrontComposerNavigationNavEntryTests`, `FcCollapsedNavRailTests` or replacement rail tests, `FcHamburgerToggleTests`, `FrontComposerShellTests`, and `FluentConformanceTests`.
  - [x] Run the solution default lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`.
  - [x] Run relevant Playwright a11y/visual/navigation coverage when feasible.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with commands, counts, and blockers.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: `FrontComposerNavigation` currently switches between a full `FluentNav` tree and `FcCollapsedNavRail`. Story 8.5 is not a brand-new nav system; it is a refactor that must preserve the current registry-driven data paths while changing the visual interaction model to an Aspire-style rail plus flyout. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor]
- Current desktop expanded navigation renders `FluentNav` with `FluentNavCategory` per manifest/orphan context. Projection links are generated by `BuildRoute`, explicit entries by `FrontComposerNavEntry`, disabled entries render without href plus a reason, and gated entries use `AuthorizeView`. The new flyout must keep all of those behaviors. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs]
- Current collapsed rail is 48px and uses one `FluentButton` per bounded context with a `FluentTooltip`, aggregate count, BC-level "New" badge, and click behavior that expands the sidebar. Story 8.5 changes click/Enter semantics to open the projection flyout, so update old tests intentionally rather than preserving the obsolete expand-on-click contract. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs]
- `FrontComposerShell.ShouldUseCollapsedRailWidth()` currently returns true for CompactDesktop or Desktop + `SidebarCollapsed`, and `FrontComposerNavigation.ShouldRenderCollapsedRail()` mirrors that. Keep the shell width calculation and component rendering in lockstep; a width/render mismatch leaves blank chrome or clipped rail content. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs; src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs]
- `FcHamburgerToggle` is already the single Desktop writer for `SidebarToggledAction`; do not introduce a second Fluxor writer for the same user action. Keep non-Desktop `FluentLayoutHamburger` drawer behavior and CompactDesktop `LayoutHamburgerCoordinator` coordination. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor; src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs]
- Active navigation is intentionally computed by `NormalizeHref` + `LongestNavPrefix`, then expressed today by giving only one `FluentNavItem` `NavLinkMatch.Prefix`. The rail/flyout cannot rely on `FluentNavItem` doing this automatically if those components are removed; explicitly compute active context/item and pin exactly-one-active behavior. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs]
- Count and "New" badge semantics are already shared with capability discovery: `VisibleProjections` hides resolved zero-count projections after counts seed, keeps unresolved or uncounted projections visible, and `HandleNavItemClicked` marks both bounded context and projection seen. Use these helpers instead of duplicating filtering logic in the flyout. [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationCapabilityBadgeTests.cs]
- `FcFluentIcons` currently creates regular inline SVG icons and `TryCreate` maps only a small string-key set; icon variants in the Fluent package support `Regular` and `Filled`, but this repo uses a custom inline SVG factory rather than a Fluent icons package. Implement active/rest icon pairs inside this factory or a narrow helper without adding dependencies. [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#IconVariant]
- Pinned Fluent v5 RC local docs show `FluentMenu` exposes `Trigger`, `ChildContent`, `OpenMenuAsync`, `CloseMenuAsync`, and `FluentMenuItem.Role`; `FluentPopover` exposes `AnchorId`, `Opened`, and `OpenedChanged`. Verify the chosen component's generated DOM and keyboard behavior in this repo before relying on it for UX-DR6. [Source: /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#FluentMenu; /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml#FluentPopover]
- Use Fluent v5 components and Fluent 2 tokens only. Raw interactive controls remain banned; raw `<a>` links are allowed for navigation. Custom CSS may handle layout/positioning and the accent left-bar, but not theme redefinition. Do not use `--design-unit`, `--neutral-*`, `--accent-*`, `--type-ramp-*`, or accent variables as backgrounds. [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md; _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- Previous Story 8.4 review caught a real defect where `::part()` selectors did not match FluentDataGrid v5's light-DOM table and tests pinned broken CSS. For nav CSS, inspect rendered DOM and pin selectors that actually match; do not add governance tests that only assert a token appears in dead CSS. [Source: _bmad-output/implementation-artifacts/8-4-compact-default-density-and-grid-polish.md#Senior-Developer-Review-AI]
- No external web research is needed for package choice. Relevant versions are pinned locally: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, and Playwright `1.61.0`. Do not change package versions or add dependencies. [Source: _bmad-output/project-context.md#Technology-Stack-Versions; Directory.Packages.props; global.json]

### Project Structure Notes

- Expected Shell production touch points:
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.css`
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor`, `.razor.cs`, `.razor.css` only if retained as the icon-only rail implementation; otherwise delete/update tests cleanly.
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FcNavContextFlyout.razor` and `.razor.cs` if the flyout is split out.
  - `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` for navigation width constants/calculation.
  - `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs` for active/rest icon variants.
  - `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` and `.fr.resx` only for new localized strings.
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationCapabilityBadgeTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs` or replacement rail tests.
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcHamburgerToggleTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
  - relevant `tests/e2e/specs/*` navigation/a11y/visual coverage if feasible.
- Expected BMAD artifacts:
  - `_bmad-output/implementation-artifacts/8-5-icon-label-navigation-rail-and-projection-flyout.md`
  - `_bmad-output/implementation-artifacts/sprint-status.yaml`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Avoid touching:
  - `Hexalith.Tenants/**`, `Hexalith.EventStore/**`, `Hexalith.Commons/**`
  - `src/Hexalith.FrontComposer.SourceTools/**` except no known need for this story
  - `src/Hexalith.FrontComposer.Mcp/**`
  - `src/Hexalith.FrontComposer.Cli/**`
  - `src/Hexalith.FrontComposer.Schema/**`
  - `Directory.Packages.props`, `.slnx` structure, PublicAPI baselines, pacts, generated `obj/` output, Story 8.6 toolbar files, and Story 8.7 status badge/icon emitter files.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-8.5-Icon-label-navigation-rail-projection-flyout]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md#Story-8.5-VR-3-Icon-label-nav-rail-projection-flyout]
- [Source: _bmad-output/project-docs/architecture.md#4-Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/architecture.md#4.1-UI-component-policy-project-wide-governance]
- [Source: _bmad-output/project-docs/architecture.md#4.3-Layout-component-policy-project-wide-guideline]
- [Source: _bmad-output/project-context.md]
- [Source: Hexalith.AI.Tools/hexalith-ux-instructions.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationCapabilityBadgeTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs]
- [Source: /home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM and UX rules, sprint status, Epic 8 source, the 2026-06-25 Aspire visual refresh proposal, architecture/project context, Story 8.4, current navigation/shell/hamburger/icon files, navigation tests, pinned Fluent UI package XML, recent git history, and current git status.
- 2026-06-25: Confirmed Story 8.5 status was `backlog` in `sprint-status.yaml`; Epic 8 is already `in-progress`; previous story 8.4 is `done`.
- 2026-06-25: Confirmed current nav split: Desktop full `FluentNav` tree, CompactDesktop/Desktop-collapsed `FcCollapsedNavRail`, and shell width logic coupled through `ShouldUseCollapsedRailWidth`.
- 2026-06-25: Confirmed no external web research was required because the relevant package/API versions are pinned and local Fluent v5 XML docs are available.
- 2026-06-25: Dev-story RED phase added Story 8.5 rail/flyout bUnit assertions; old implementation failed with missing `fc-navigation-rail` and flyout menu.
- 2026-06-25: Replaced the split full `FluentNav` / `FcCollapsedNavRail` path with a single `FrontComposerNavigation` rail using `FluentButton` tiles and `FluentMenu` flyouts.
- 2026-06-25: Deleted obsolete `FcCollapsedNavRail` component/test files after replacement rail-mode tests passed.
- 2026-06-25: Local VSTest and Playwright browser lanes are socket-blocked; direct xUnit v3 and e2e typecheck evidence recorded in `tests/test-summary.md`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 8.5 created as a Shell navigation refactor story with explicit guardrails around preserving registry-driven manifests, explicit nav entries, active-route matching, count/"New" badge semantics, hamburger single-writer state, Fluent v5 interaction verification, and Epic 8 chrome/density boundaries.
- Validation pass completed against the create-story checklist; guardrails added for no raw controls, no legacy tokens, no accent background fills, no package changes, no submodule edits, and no Story 8.6/8.7 scope creep.
- Implemented one framework-owned rail path: Desktop expanded renders a 72px labeled rail; Desktop collapsed and CompactDesktop render a 48px icon-only rail; Tablet/Phone continue to omit the navigation layout item.
- Added `FluentMenu` projection flyouts per bounded context, preserving visible projection filtering, explicit nav entries, policy gating, disabled reasons, count/"New" badges, orphan contexts, and capability visited dispatch before navigation.
- Active route matching still uses `NormalizeHref` + `LongestNavPrefix`; flyout menu items and context tiles expose `aria-current` for the single active route/context.
- `FcFluentIcons` now supports requested `IconVariant` values while preserving the existing string-key icon contract and existing call sites.
- Playwright sidebar coverage was updated and TypeScript typecheck passed; browser execution remains locally blocked by Kestrel socket permissions.

### File List

- `_bmad-output/implementation-artifacts/8-5-icon-label-navigation-rail-and-projection-flyout.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor` (deleted)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor.css` (deleted)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCollapsedNavRailTests.cs` (deleted)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs`
- `tests/e2e/page-objects/shell.page.ts`
- `tests/e2e/specs/sidebar-responsive.spec.ts`

### Change Log

- 2026-06-25: Implemented Story 8.5 icon+label navigation rail and projection flyout; moved story to review. Focused Shell lane 113/113 passed, broad Shell non-Contract lane 1983/1983 passed, e2e typecheck passed. Solution VSTest and Playwright browser execution are socket-blocked locally and recorded in test summary.
- 2026-06-25: Senior Developer Review (AI) auto-fix pass. Implemented the missing active/rest icon pair on the default `Apps20` rail glyph (Filled now renders a distinct denser app-grid; Regular/logo unchanged), removed dead `MatchFor`/`IsGroupCollapsed` plus the unreachable `OnGroupExpandedChanged` handler/test-hook and its now-orphaned `IUlidFactory` injection, deleted two stale UI-coupled nav-group tests (reducer behavior still pinned by `NavigationReducerTests`), renamed the stale `FullNav_RendersLocalizedNavMenuAriaLabel` test, and corrected the inaccurate `FrontComposerNavigationCapabilityBadgeTests` task claim. Re-verified: Release build 0W/0E, focused Shell lane 111/111, broad Shell non-Contract lane 1984/1984. Story → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-25 · **Mode:** autonomous auto-fix · **Outcome:** Approved after fixes (0 CRITICAL remaining)

**Evidence reproduced independently:** Release build of `Hexalith.FrontComposer.Shell.Tests` = 0 warnings / 0 errors. Direct in-process xUnit v3 runner (VSTest/Playwright sockets blocked locally, as the dev recorded): focused rail/nav/shell/hamburger/governance/localization lane 111/111; full Shell assembly minus Performance/e2e-palette/NightlyProperty/Quarantined 1984/1984 (was 1986 before two stale tests were removed). All ACs confirmed implemented; longest-prefix single-active routing, count/"New" badge parity, policy/disabled/orphan handling, and the FluentMenu flyout all verified at the bUnit seam. Story-8.2 accent-as-background guard preserved (nav CSS uses accent only on `border-inline-start`, never `background`).

### Findings & resolutions (all auto-fixed)

| # | Sev | Finding | Resolution |
|---|-----|---------|------------|
| 1 | HIGH | AC1/AC2 + task "Add or resolve icon pairs" require an **outline-to-filled active state**, but `FcFluentIcons.Apps20(Filled)` and `Apps20(Regular)` returned the *identical* SVG path — `IconVariant.Filled` was metadata-only with **zero rendered effect**, so active and rest tiles looked the same. | Added a distinct `AppsFilledPath` (denser app-grid) returned only for the Filled variant. `Apps20(Regular)` is untouched so the Story-8.3 header-logo cell and `FrontComposerShellTests` solid-path pins stay green. Active tiles now render a genuinely different glyph (on top of the already-present accent thread + semibold + `aria-current`). |
| 2 | MEDIUM | Dead production code left from the removed `FluentNav` tree: `MatchFor` and `IsGroupCollapsed` had no callers. | Deleted both. |
| 3 | MEDIUM | `OnGroupExpandedChanged` (+ `OnGroupExpandedChangedForTest` hook) and two tests drove a nav-group expand/collapse handler with **no UI trigger** — the flyout model has no collapsible categories. Comments still claimed "the FluentNavCategory callback wires to this method" (Dev Notes explicitly warned against stale tests). `IUlidFactory` was injected solely for this dead path. | Removed the handler, the test hook, the `IUlidFactory` injection + its `using`, and the two stale tests. Persisted `CollapsedGroups`/`NavGroupToggledAction` schema and its D13 reducer/seen-set semantics remain covered by `NavigationReducerTests`; `CollapsedGroupsState_DoesNotHideUnifiedRailTile` still pins rail behavior. |
| 4 | MEDIUM | Task "Extend `FrontComposerNavigationCapabilityBadgeTests` … projection-level badges" was marked `[x]` but that file has **zero git changes** — the coverage actually landed in `FrontComposerNavigationTests`. | Corrected the task wording to point at the real coverage; that file was confirmed unchanged and still passing. |
| 5 | LOW | Stale test `FullNav_RendersLocalizedNavMenuAriaLabel` name/comment referenced a `FluentNav` that no longer exists (it asserts the rail's aria-label). | Renamed to `Rail_RendersLocalizedNavMenuAriaLabel` and fixed the comment. |

### Noted, not changed (out of story scope)
- `SidebarExpandedAction` now has no production producer (the deleted `FcCollapsedNavRail` was its only dispatcher); its reducer/effect/reducer-test are intentionally retained because Task 1 mandates preserving the persisted navigation schema. Left as-is.
- `People20(Filled)` (and any custom domain icon supplied as a single string glyph) remains metadata-only — only the default `Apps20` tile has a true active/rest pair. People only renders when an adopter opts in via an icon key; its active state still carries the accent thread + semibold + `aria-current`. Tracked as a minor future polish, not a blocker.
- Working tree carries submodule-pointer moves (`Hexalith.EventStore`, `Hexalith.Tenants`) and a modified `_bmad-output/story-automator/orchestration-*.md` that are **not** part of this story and that Task boundaries forbid editing; left untouched (pre-existing environment/orchestration state).
