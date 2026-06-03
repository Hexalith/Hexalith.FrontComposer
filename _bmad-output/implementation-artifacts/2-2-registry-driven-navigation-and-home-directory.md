---
baseline_commit: a0cbab2f136e35e4c43d593b44ad32f1b827b1a4
---

# Story 2.2: Registry-driven navigation and home directory

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Story 2.1, the entire registry-driven navigation + home-directory surface **already
> exists, renders, and is largely tested** at baseline `a0cbab2`. The shipped components are:
> `FrontComposerNavigation` (`FluentNav` tree grouped by bounded context, per-projection count +
> "New" badges, driven by `IFrontComposerRegistry.GetManifests()`), `FcHomeDirectory` / `FcHomeCard` /
> `FcHomeRouteView` (the `@page "/"` + `/home` landing page with four progressive states and
> urgency-sorted bounded-context cards), and the compact-viewport collapse path
> (`FcLayoutBreakpointWatcher` → `ViewportTierChangedAction` → `FcCollapsedNavRail` 48px / hamburger).
> A full Fluxor `Navigation` slice + `CapabilityDiscovery` slice (badge counts + seen-set) back them,
> with single-writer discipline, scope-guarded persistence, and a three-state hydration lifecycle.
>
> **So this story's job is to (1) VERIFY each of the three ACs holds end-to-end against `src/` at this
> commit, (2) CLOSE the durability gaps with focused regression pins, and (3) make the verification
> durable** so the rest of Epic 2 (grid/filter/expand) builds on a pinned discovery baseline.
> **Default to ZERO `src/` change.** AC3 (compact collapse) is already comprehensively pinned — expect
> to confirm-only. The real gaps are **AC1's count + projection-level "New" badge rendering** and
> **AC2's progressive / flat / accordion sort orderings**, which are currently only *implicit* inside
> broader render tests. If a real `src/` gap is found, fix the component (never hand-edit generated
> output) and update affected `.verified.txt` snapshots **intentionally**. If a behaviour is already
> correct, **pin it — do not "improve" or restyle** working output (that churns downstream snapshots for
> no AC reason; Epic-1 retro §5 flags "copy-a-pattern-without-the-difference" as the rising Epic-2 risk).

## Story

As an operator,
I want a navigation tree and a home landing page generated from the registered domain manifests,
so that I can find every bounded context and projection without a hand-built menu.

## Acceptance Criteria

**AC1 — `FrontComposerNavigation` renders a registry-driven `FluentNav` tree grouped by bounded context, with per-projection count + "New" badges. *(FR14, UX-DR2)***
**Given** registered `DomainManifest`s,
**When** the shell renders,
**Then** `FrontComposerNavigation` shows a `FluentNav` tree **grouped by bounded context** (one `FluentNavCategory` per manifest with projections, one `FluentNavItem` per projection), **with per-projection count badges** (rendered when `count > 0`) **and "New" badges** (rendered when the capability is unseen and `count > 0`), driven entirely by `IFrontComposerRegistry.GetManifests()` + the `CapabilityDiscovery` slice — no hand-built menu.

**AC2 — `FcHomeDirectory` at `/` and `/home` shows urgency-sorted bounded-context cards across its four progressive states. *(FR14)***
**Given** the home route (`/`, `/home`),
**When** loaded,
**Then** `FcHomeDirectory` renders the correct one of its **four progressive states** (Empty → Skeleton/Idle → Progressive/Seeding → Seeded), and in the seeded/progressive states shows **urgency-sorted bounded-context cards** (urgency = aggregate projection count, descending; ties broken by name ordinal), with zero-urgency contexts collapsed into the "Other areas" accordion when at least one context is actionable.

**AC3 — At a compact viewport, navigation collapses to the 48px `FcCollapsedNavRail` / hamburger per the breakpoint watcher. *(UX-DR3)***
**Given** a compact viewport,
**When** the shell renders,
**Then** navigation collapses to the **48px** `FcCollapsedNavRail` (at `CompactDesktop`, or `Desktop` + manually-collapsed sidebar) / **hamburger** (at `Tablet`/`Phone`), driven by `FcLayoutBreakpointWatcher` → `ViewportTierChangedAction` → `FrontComposerNavigationState.CurrentViewport` (never persisted, per ADR-037).

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before
> writing anything. Most subtasks should resolve to "already true → add/confirm the pin"; only open a
> `src/` change if a genuine AC gap is proven. Record what you found (true/false + the evidence) in the
> Dev Agent Record so the review can audit it.

- [x] **Task 1 — Verify AC1: registry-driven `FluentNav` tree + count + "New" badges (AC: #1)**
  - [x] **Tree + grouping + route + click-dispatch — confirm ALREADY PINNED, no change.** Re-confirm `FrontComposerNavigationTests` pins: one category per manifest, one item per projection, commands excluded, empty-projection categories hidden, D2 route convention, expand/collapse seen-set semantics (D13), tab-reachability (D16). And `FrontComposerNavigationCapabilityBadgeTests` pins the click→`CapabilityVisitedAction` synchrony (D13) and the `count == 0 → hide` / `unresolved-type → stay visible` visibility rules.
  - [x] **CLOSE the badge-rendering gap (durability pin).** The **count badge** (`count > 0` → `FluentBadge` `Filled`/`Brand`, `data-testid="fc-nav-badge-{bc}-{label}"`, text `@count`) and the **projection-level "New" badge** (`projShowsNew = unseen && count > 0` → `Tint`/`Informative`, `data-testid="fc-nav-new-{bc}-{label}"`) are currently **NOT** asserted in `FrontComposerNavigationTests`. Add named bUnit pins that seed `BadgeCountsSeededAction` + `SeenCapabilitiesHydratedAction` and assert: (a) count badge renders with the correct value when `count > 0` and is **absent** when `count == 0`; (b) projection-level "New" renders only when unseen **and** `count > 0`, and is suppressed once seen. (The **BC-level** "New" badge `bcShowsNew` may be pinned in the same pass for symmetry.)
  - [x] Confirm `AggregateBoundedContextCount` / `LookupCount` math is exercised by the new pin (aggregate over a multi-projection manifest). Do **not** add isolated unit tests for private helpers if the rendered-output pin already covers them.
  - [x] **Do not restyle badges.** `Filled`/`Brand` (count) and `Tint`/`Informative` ("New") are the shipped contract — pin the existing values, don't "improve" them.

- [x] **Task 2 — Verify AC2: home route + four states + urgency sort (AC: #2)**
  - [x] **Route + four states — confirm ALREADY PINNED.** Re-confirm `FcHomeRouteView.razor` maps `@page "/"` + `@page "/home"` → `<FcHomeDirectory />`, and `FcHomeDirectoryTests` pins all four states by testid: Empty (`fc-home-empty-no-microservices`), Skeleton/Idle (`fc-home-skeletons`), Progressive/Seeding (`fc-home-cards-progressive`), Seeded (`fc-home-cards-flat` / `fc-home-cards-urgent`), plus the urgent-variant urgency sort (`RendersUrgencySortedCards_WhenTotalActionableItemsExceedsZero`: Beta(9) → Gamma(5) → Alpha(2)) and the "Other areas" accordion presence/absence.
  - [x] **CLOSE the sort-ordering gaps (durability pins).** Three orderings are implemented but **NOT** explicitly pinned and could silently regress on a careless refactor:
    1. **Progressive state** ordering `OrderByDescending(IsReady).ThenByDescending(AggregateCount).ThenBy(Name, Ordinal)` — add a pin that seeds `Seeding` hydration with mixed ready/not-ready + different counts and asserts ready-first, then count-desc, then name-ordinal.
    2. **Seeded flat** (`totalActionable == 0`) ordering `OrderBy(Name, Ordinal)` — add a pin asserting alphabetical-ordinal order when all counts are zero.
    3. **Accordion zero-cards** ordering `OrderBy(Name, Ordinal)` inside `fc-home-other-areas` — add a pin asserting alphabetical-ordinal order of the collapsed zero-urgency cards.
  - [x] **Use `StringComparer.Ordinal`** in test expectations to match the component (not culture-aware ordering). Keep the `CultureInfo.CurrentUICulture = "en"` pinning the existing tests use for resource-key stability.

- [x] **Task 3 — Verify AC3: compact-viewport collapse to 48px rail / hamburger (AC: #3)**
  - [x] **Confirm ALREADY COMPREHENSIVELY PINNED — expect ZERO change.** Re-confirm the existing pins hold:
    - `FcLayoutBreakpointWatcherTests` — module import (D5), initial-tier emission, dedup of composed-tier no-ops, double-boundary skip, invalid-tier guard, clean dispose.
    - `FcCollapsedNavRailTests` — one button per manifest-with-projections, tooltip name, commands-only manifest renders no button, click → `SidebarExpandedAction` + `CapabilityVisitedAction`, badge/"New" rendering, and **`CompactDesktopClickRequestsHamburgerDrawer`** (rail click at CompactDesktop opens the hamburger drawer via the coordinator).
    - `FrontComposerShellTests.AutoRenderedNavigationUsesRailWidthAtCompactDesktop` — **48px** width at `CompactDesktop`; `NavigationPaneHiddenAtSubCompactDesktopViewports` — nav pane hidden at Tablet/Phone.
    - `FrontComposerNavigationTests.RendersRailAtCompactDesktop` / `RendersFullNavAtDesktop` — rail vs full `FluentNav` swap.
    - `FcHamburgerToggleTests` — hamburger visible at CompactDesktop/Tablet/Phone, hidden at Desktop (incl. Desktop+manual-collapse, D9).
  - [x] Confirm `fc-layout-breakpoints.js` breakpoints are unchanged: Desktop ≥1366px, CompactDesktop ≥1024px, Tablet ≥768px, Phone <768px; `ViewportTier` enum ordinals pinned (Phone=0, Tablet=1, CompactDesktop=2, Desktop=3) by `NavigationReducerTests.ViewportTierEnumValuesArePinned`. **Only add a pin if a genuine gap is found** — none is expected here.

- [x] **Task 4 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE.
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — everything this story touches is green; new pins pass.
  - [x] **Re-prove the standing 13-failure baseline** (8 Shell + 3 SourceTools + 2 Cli) is pre-existing/environmental, matching the Epic-1 retro record. This story's pins land in **`Shell.Tests`** — capture the Shell.Tests before→after count and confirm the **same 8 pre-existing Shell failures** remain (none new, none misattributed). `src/` is expected untouched, so the SourceTools/Cli clusters are unchanged.
  - [x] **If no render behaviour changed → ZERO `.verified.txt` snapshot edits.** (Confirm-and-pin: default zero `src/` change should hold.)

- [x] **Task 5 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in the Dev Agent Record below, reconciled against the actual git tree (this is the recurring Epic-1 review finding — pay it up front).
  - [x] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no stray `</content>` / `</invoke>` / tool-call tags.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`a0cbab2`) | This story |
|---|---|---|
| Registry-driven `FluentNav` tree, grouped by BC | **Exists** — `FrontComposerNavigation.razor` (loops `Registry.GetManifests()`, one `FluentNavCategory` per BC) | Confirm; no change |
| Per-projection **count** badge (`count>0`) | **Exists** — `FluentBadge` `Filled`/`Brand`, `fc-nav-badge-*` | **Add render pin** (currently unpinned) |
| Projection-level **"New"** badge (`unseen && count>0`) | **Exists** — `FluentBadge` `Tint`/`Informative`, `fc-nav-new-*` | **Add render pin** (currently unpinned) |
| BC-level "New" badge (`bcShowsNew`) | **Exists** — `fc-nav-bc-new-*` | Pin alongside (optional) |
| Route convention + click→`CapabilityVisitedAction` | **Exists & pinned** — D2 route, D13 dispatch synchrony | Confirm; no change |
| Navigation Fluxor slice (persist/hydrate/scope-guard) | **Exists & well-pinned** — `State/Navigation/*` | Confirm; no change |
| `CapabilityDiscovery` slice (counts + seen-set) | **Exists & pinned** — `State/CapabilityDiscovery/*` | Confirm; no change |
| Home route `/`, `/home` → `FcHomeDirectory` | **Exists** — `FcHomeRouteView.razor` (`@page "/"` + `/home`) | Confirm; no change |
| Home **four states** (Empty/Skeleton/Progressive/Seeded) | **Exists & pinned** by testid | Confirm; no change |
| Home **urgent-variant** urgency sort | **Exists & pinned** — `RendersUrgencySortedCards_…` | Confirm; no change |
| Home **progressive / flat / accordion** sort orderings | **Exists** — `OrderBy*` in `FcHomeDirectory.razor` | **Add 3 sort pins** (currently unpinned) |
| Compact collapse: watcher → 48px rail / hamburger | **Exists & comprehensively pinned** | Confirm; **ZERO change expected** |

> **Key judgment for the dev agent:** the deliverable is **confidence + durable pins on the two
> under-pinned surfaces (AC1 badges, AC2 sort orderings)**, not new features and not AC3 churn. If you
> find yourself editing `FrontComposerNavigation.razor`, `FcHomeDirectory.razor`, the Fluxor slices, or
> the breakpoint JS, **stop** — re-read the AC and confirm you've found a *genuine* gap, not a style
> preference. AC3 is already locked from five angles; resist re-pinning it.

### Exact anchors (read these before touching anything)

**AC1 — Navigation**
- **Tree + badges markup** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`: registry loop & `FluentNav` (`:34`); `FluentNavCategory` per BC (`:46`); per-projection `count = LookupCount(...)` (`:51`) and `projShowsNew` (`:55`); **count badge** `Filled`/`Brand` `fc-nav-badge-*` (`:60-64`); **projection "New"** `Tint`/`Informative` `fc-nav-new-*` (`:66-73`); **BC "New"** `fc-nav-bc-new-*` (`:76-83`).
- **Helpers** — `FrontComposerNavigation.razor.cs`: `BuildRoute` D2 convention (`:51-56`); `VisibleProjections` count-visibility filter (`:95-127`); `LookupCount` (`:129-133`); `AggregateBoundedContextCount` (`:135-149`); `HandleNavItemClicked` dual dispatch (`:188-191`); `ShouldRenderCollapsedRail` (`:169-173`).
- **Navigation slice** — `src/Hexalith.FrontComposer.Shell/State/Navigation/{FrontComposerNavigationState,NavigationActions,NavigationReducers,NavigationEffects,NavigationPersistenceBlob,NavigationHydrationState}.cs`. **Invariants:** `CurrentViewport`/`CurrentBoundedContext`/`StorageReady`/`HydrationState` **never persisted** (ADR-037/049, D19); persist only `SidebarCollapsed` + sparse `CollapsedGroups` + `LastActiveRoute`; hydrate does **not** re-persist (ADR-038); scope-guard fail-closed (HFC2105) when tenant/user null/whitespace.
- **CapabilityDiscovery slice** — `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/*` + `Badges/CapabilityIds.cs` (`bc:{ctx}` / `proj:{ctx}:{fqn}` id helpers). `Counts: ImmutableDictionary<Type,int>`, `SeenCapabilities: ImmutableHashSet<string>`, three-state `CapabilityDiscoveryHydrationState` (Idle→Seeding→Seeded).

**AC2 — Home**
- **Component** — `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor`: Empty (`:18-25`), Skeleton/Idle (`:27-36`), Progressive/Seeding sort `OrderByDescending(IsReady).ThenByDescending(AggregateCount).ThenBy(Name,Ordinal)` (`:76-103`), Seeded-flat `OrderBy(Name,Ordinal)` (`:104-116`), Seeded-urgent `OrderByDescending(AggregateCount).ThenBy(Name,Ordinal)` + zero-cards accordion `OrderBy(Name,Ordinal)` (`:117-155`). `aria-description="Sorted by urgency"` on each card list.
- **Code-behind** — `FcHomeDirectory.razor.cs`: `AggregateManifests` (`:101-127`) building `HomeCardModel(Manifest, AggregateCount, IsReady, ProjectionRows)` (`:145-149`); `IsReady = any projection resolved`, `AggregateCount = Σ resolved counts`.
- **Card** — `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor` (title, Brand count badge when `>0`, Informative "New" when `unseen && AggregateCount>0`, per-projection rows).
- **Route** — `src/Hexalith.FrontComposer.Shell/Components/Pages/FcHomeRouteView.razor` (`@page "/"` + `@page "/home"` → `<FcHomeDirectory />`).

**AC3 — Compact collapse (confirm-only)**
- **Watcher** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor(.cs)` (ES-module import `./_content/Hexalith.FrontComposer.Shell/js/fc-layout-breakpoints.js`; `[JSInvokable] OnViewportTierChangedAsync(int)` → `ViewportTierChangedAction`; degrades to Desktop on import failure).
- **JS** — `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js` (`matchMedia` queries: desktop `≥1366px`, compact `≥1024px`, tablet `≥768px`; `computeTier` → 3/2/1/0; emits initial tier; dedups composed-tier no-ops).
- **Rail** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor(.cs/.css)` (48px CSS rail; one button/manifest; `role="navigation"` + `NavMenuAriaLabel`; `OnRailClicked` → `SidebarExpandedAction` + at CompactDesktop `HamburgerCoordinator.ShowAsync()`).
- **Shell composition** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor(.cs)`: `IsSubCompactDesktopViewport` (Tablet/Phone → hide pane) (`:221-226`); `NavigationWidth` 48px vs 220px (`:232-234`); `ShouldUseCollapsedRailWidth` (CompactDesktop, or Desktop+collapsed) (`:555-559`).
- **Enum** — `ViewportTier.cs` (Phone=0, Tablet=1, CompactDesktop=2, Desktop=3) — ordinals are a pinned contract.

### Test anchors (where pins live / go)

- **Navigation component** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` (tree/grouping/route/expand-collapse/tab-reach/rail-vs-nav pins) **← AC1 count + projection-"New" badge pins go here**; `…/FrontComposerNavigationCapabilityBadgeTests.cs` (click-synchrony D13; count-visibility fallback/unresolved-type rules).
- **Navigation slice** — `tests/…/State/Navigation/NavigationReducerTests.cs` (toggle/sparse-group/viewport-isolation/hydrate-wholesale/enum-ordinals), `…/State/Navigation/NavigationEffectsScopeTests.cs` (persist scope-guards, ADR-037/038 reflection guards, hydrate-no-repersist).
- **CapabilityDiscovery slice** — `tests/…/State/CapabilityDiscovery/CapabilityDiscoveryReducersTests.cs` (seed/single-key/seen-set/race-no-lost-update).
- **Home component** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/Home/FcHomeDirectoryTests.cs` (four states, urgent sort `RendersUrgencySortedCards_…`, accordion present/absent, `NewBadge_Matrix`, keyboard activation) **← AC2 progressive / flat / accordion sort pins go here**; integration `tests/…/Components/CapabilityDiscovery/CapabilityDiscoveryConsumerIntegrationTests.cs` (real `BadgeCountService` seeds home badge); bootstrap `tests/…/Components/Layout/Story11BootstrapShellRenderTests.cs` (empty-registry home renders without throwing).
- **AC3 (confirm-only)** — `tests/…/Components/Layout/FcLayoutBreakpointWatcherTests.cs`, `…/FcCollapsedNavRailTests.cs` (incl. `CompactDesktopClickRequestsHamburgerDrawer`), `…/FcHamburgerToggleTests.cs`, `…/FrontComposerShellTests.cs` (`AutoRenderedNavigationUsesRailWidthAtCompactDesktop`, `NavigationPaneHiddenAtSubCompactDesktopViewports`).
- **Test scaffolds** — `LayoutComponentTestBase` (bUnit + Fluxor host, `JSInterop.Mode = Loose`); seed state by dispatching `BadgeCountsSeededAction` / `SeenCapabilitiesHydratedAction` / `ViewportTierChangedAction`; NSubstitute for `IFrontComposerRegistry` + `IUlidFactory`; `CultureInfo.CurrentUICulture = "en"` for resource-key stability.

### Project-context guardrails that apply here (non-negotiable)

- **Fluxor single-writer discipline (ADR-007):** one dispatch source per action type; **effects own persistence + JS interop**, reducers stay pure. Don't add a new persistence write without honoring the **NFR17 tripwire** (a new `IStorageService.SetAsync` call site in `Shell/State/` requires updating the tripwire whitelist + the story compliance matrix). This story is verify/pin — **no new `SetAsync` call sites expected**.
- **Viewport is runtime-derived, never persisted (ADR-037):** don't add an effect that reacts to `ViewportTierChangedAction`; `NavigationEffectsScopeTests` has a reflection guard that will fail the build if you do.
- **Scoped-lifetime discipline (ADR-030):** storage/effects/auth/tenant accessors are scoped — never captured in singletons.
- **Icons:** use the custom inline-SVG `FcFluentIcons` factory, **not** a FluentUI icons NuGet.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix, **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE), `ArgumentNullException.ThrowIfNull` at public boundaries, **no copyright/license headers** (this repo has none), **CRLF**, 4-space indent, final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for components (`LayoutComponentTestBase`); plural `{Class}Tests.cs`; three-part `Subject_Scenario_Expectation` method names; **solution-level** `dotnet test` + trait filters (not per-project); run with **`DiffEngine_Disabled=true`** (else Verify hangs); `.verified.txt` updated **intentionally** and committed.
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress; built-in analyzers only (no Sonar/StyleCop/Roslynator); centralized versions in `Directory.Packages.props` (never add `Version=` to a `.csproj`).
- **Commits/branches:** Conventional Commits; this work is **`test:`/`fix:`** shaped (verification + pins), **not `feat:`** unless a genuine new capability is added (a false `feat:` triggers a minor bump + NuGet publish). Already on a feature branch (`feat/story-1-2-fc-lyt-page-layout`); do **not** commit to `main`.

### Epic-1 dependencies & their state (from the Epic-1 retro §6)

| Epic-2 needs | From Epic 1 | State at kickoff |
|---|---|---|
| A11y ready-gate every story must pass | Story 1.3 (FC-A11Y) | ⚠️ escalated; primitive set shipped & pinned. For nav/home, the in-scope a11y pins are the `aria-label` (`NavMenuAriaLabel`), the `role="main"` + `aria-description="Sorted by urgency"` on home, badge accessible names, and tab-reachability — already pinned. **Layer-3 e2e axe is CI-only** (Playwright unsupported on this host + Node <24) → no e2e work here. |
| Localized chrome | Story 1.4 (FC-L10N) | ⚠️ escalated; parity tests green. Nav/home strings (`NewCapabilityBadgeText`, `Home*` keys) live in `FcShellResources.resx` (+`.fr.resx`). Keep tests on `en` for key stability. |
| Settings/preference persistence | Story 1.6 | ✅ shipped. The `Navigation` slice persists `SidebarCollapsed`/`CollapsedGroups`/`LastActiveRoute` via the scope-guarded storage path; **viewport is intentionally NOT persisted** (ADR-037). |
| Theme/density tokens | Story 1.6 | ✅ shipped; not on this story's critical path. |

> **Scope boundary:** this story is "**registry-driven navigation + home directory + compact collapse**".
> It is *not* the DataGrid filtering/status story (2.3), expand-in-row a11y (2.4), column prioritization
> (2.5), live updates (2.6), the command palette / global search (2.7 — `FcCommandPalette` is a separate
> surface), or FC-TBL confirmation (2.8). Stay inside AC1–AC3.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The nav/home/discovery surface (FR14, UX-DR2/DR3) is
shipped and substantially tested at `a0cbab2`. **Done = each of AC1/AC2/AC3 is proven true against
`src/` at this commit and carries a durable regression pin** — specifically the two under-pinned
surfaces are closed (AC1 count + projection-"New" badge rendering; AC2 progressive/flat/accordion sort
orderings), AC3 is re-confirmed green from its five existing angles, the Release build is 0/0 under
TWAE, the default test lane is green, the standing 13-failure baseline is re-proved pre-existing, and
the File List + counts are accurate. Genuine `src/` change is the exception, not the expectation — and
if made, it lands in the component (never generated output) with intentional snapshot updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.2] — story statement, ACs (FR14, UX-DR2, UX-DR3)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage; FR14 (nav/home/palette/badges) → Epic 2
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements] — UX-DR2 (semantic badge slots), UX-DR3 (responsive layout: breakpoint watcher, 48px rail, hamburger)
- [Source: _bmad-output/project-docs/component-inventory.md] — `FrontComposerNavigation`, `FcCollapsedNavRail`, `FcLayoutBreakpointWatcher`, `FcHomeDirectory`/`FcHomeCard`/`FcHomeRouteView` entries
- [Source: _bmad-output/project-context.md] — Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), NFR17 tripwire, `FcFluentIcons`, TWAE, test discipline, `DiffEngine_Disabled=true`
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] — FC-A11Y ready-gate (aria-label/role pins Layer-1; e2e Layer-3 CI-only)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] — FullWidth default for projection pages (nav/home chrome)
- [Source: _bmad-output/implementation-artifacts/2-1-render-a-projection-from-a-projection-type.md] — prior confirm-and-pin pattern; 13-failure baseline; retro AI-1/AI-2 taxes
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md#3,#6] — File-List/sentinel taxes (AI-1/AI-2), 13-failure baseline, Epic-2 dependency states
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor] — registry-driven `FluentNav` tree + count/"New" badges (AC1)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor] — four states + urgency sort (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCollapsedNavRail.razor] — 48px rail; [Source: src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js] — breakpoint watcher (AC3)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Pages/FcHomeRouteView.razor] — `@page "/"` + `/home` route
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs] — nav tree pins (AC1 badge pins land here)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Home/FcHomeDirectoryTests.cs] — four-states + urgent-sort pins (AC2 sort pins land here)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-dev-story workflow

### Debug Log References

- Release build: `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 Warning(s) / 0 Error(s)** under TWAE (baseline `a0cbab2`, src/ untouched).
- Default test lane (both runs): `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- Focused run of the two touched classes: `--filter "FullyQualifiedName~FrontComposerNavigationTests|FullyQualifiedName~FcHomeDirectoryTests"` → **41 passed / 0 failed**.

**Shell.Tests before→after the pins:**

| | Failed | Passed | Total |
|---|---|---|---|
| Before (baseline `a0cbab2`) | 8 | 1686 | 1694 |
| After dev-story (12 new pins added) | 8 | 1698 | 1706 |
| After QA gap-pins (`bmad-qa-generate-e2e-tests`, +5) | 8 | 1701 | 1709 |

Delta = **+15 passing tests, failures unchanged at 8.** dev-story added 12 cases (9 nav: 5 facts +
a 4-case theory; 3 home facts). A subsequent `bmad-qa-generate-e2e-tests` pass added 5 more cases
(3 nav appearance/aria pins + 2 route-shim pins in a new file — see File List). Net new tests all
green. *(Counts re-verified by the review pass on 2026-06-04: focused run 46/46 across the three pin
classes; Shell.Tests default lane 8 failed / 1701 passed / 1709 total.)*

**Standing 13-failure baseline re-proved pre-existing (none new, none mine):**

- **Shell.Tests — 8** (all environmental/generated, unrelated to AC1/AC2/AC3): `Generated.CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty`; `Generated.CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting`; `Generated.CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot`; `Governance.PendingStatusReopenGovernanceTests` (×4); `State.Navigation.NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute_DispatchesHydratedActions`.
- **SourceTools.Tests — 3** (unchanged; src/ untouched).
- **Cli.Tests — 2** (unchanged; src/ untouched).

### Completion Notes List

**Outcome: confirm-and-pin succeeded with ZERO `src/` change and ZERO `.verified.txt` edits.** All three ACs verified true end-to-end against `src/` at `a0cbab2`; the two under-pinned surfaces are now closed by durable regression pins; AC3 re-confirmed green from its existing angles.

- **AC1 (Task 1) — VERIFIED TRUE.** Tree/grouping/route/click/visibility already pinned (`FrontComposerNavigationTests` + `FrontComposerNavigationCapabilityBadgeTests`) — re-confirmed, no change. **Gap closed:** added 6 render pins to `FrontComposerNavigationTests.cs` covering the previously-unpinned badge markup:
  - `CountBadge_RendersBrandFilledBadgeWithValue_WhenProjectionCountPositive` — `fc-nav-badge-{bc}-{label}` renders with the count value.
  - `CountBadge_AbsentAndProjectionHidden_WhenResolvedCountIsZero` — resolved count 0 → projection filtered by `VisibleProjections`, badge + item both absent (the true count==0 contract).
  - `CountBadge_Absent_WhenProjectionVisibleButHasNoResolvedCount` — exercises the `@if (count > 0)` markup guard with the item still visible (unresolved FQN stays visible, `LookupCount`→0, no badge).
  - `ProjectionNewBadge_Matrix` (4-cell theory) — `projShowsNew = unseen && count>0`, keyed `fc-nav-new-{bc}-{label}`.
  - `MultiProjectionManifest_RendersPerProjectionCountBadges_AndAggregateBcNew` — per-item `LookupCount` + `AggregateBoundedContextCount` (Σ=5) drives the BC-level `fc-nav-bc-new-{bc}`.
  - `BcNewBadge_Absent_WhenBoundedContextAlreadySeen` — `bc:{BC}` in seen-set suppresses BC "New" while count badges remain.
  - `Filled`/`Brand` (count) and `Tint`/`Informative` ("New") appearances pinned as the shipped contract — not restyled.
- **AC2 (Task 2) — VERIFIED TRUE.** Route (`@page "/"` + `/home`), four states, and the urgent-variant sort already pinned — re-confirmed, no change. **Gap closed:** added 3 sort-ordering pins to `FcHomeDirectoryTests.cs`:
  - `RendersProgressiveCards_ReadyFirstThenCountDescThenNameOrdinal` — Seeding state via `BadgeCountChangedAction`; ready-first, count-desc, name-ordinal (Alpha(8) < Bravo(8) < Zulu(3) < not-ready Mike skeleton).
  - `RendersSeededFlatCards_InNameOrdinalOrder_WhenAllCountsZero` — `totalActionable==0` flat list alphabetical-ordinal (Alpha < Mike < Yankee).
  - `RendersOtherAreasAccordionZeros_InNameOrdinalOrder` — zero-urgency cards inside `fc-home-other-areas` alphabetical-ordinal (Alpha < Mike < Yankee). All expectations use `StringComparer.Ordinal`, culture pinned `en`.
- **AC3 (Task 3) — VERIFIED TRUE, CONFIRM-ONLY, ZERO change.** Re-confirmed the five existing angles hold (all green in the default lane): `FcLayoutBreakpointWatcherTests`, `FcCollapsedNavRailTests` (incl. `CompactDesktopClickRequestsHamburgerDrawer`), `FrontComposerShellTests.AutoRenderedNavigationUsesRailWidthAtCompactDesktop` / `NavigationPaneHiddenAtSubCompactDesktopViewports`, `FrontComposerNavigationTests.RendersRailAtCompactDesktop` / `RendersFullNavAtDesktop`, `FcHamburgerToggleTests`, and the `ViewportTier` ordinal pin `NavigationReducerTests.ViewportTierEnumValuesArePinned` (Phone=0…Desktop=3). `fc-layout-breakpoints.js` and `ViewportTier.cs` unchanged in the git tree. No genuine gap found → no new pin added (as expected).
- **Task 5 record-keeping:** File List below reconciled against `git status` (retro AI-1). Sentinel scan of both modified test files — no stray `</content>` / `</invoke>` / tool-call tags (retro AI-2).
- **QA gap-pin pass (`bmad-qa-generate-e2e-tests`, 2026-06-04, +5 pins):** after the dev-story pass, a QA-automation pass closed three additional unasserted contracts (see `_bmad-output/implementation-artifacts/tests/2-2-test-summary.md`):
  - **AC1 / FC-A11Y** — `FrontComposerNavigationTests.FullNav_RendersLocalizedNavMenuAriaLabel`: the full `FluentNav` exposes the localized `NavMenuAriaLabel` as its rendered `aria-label` (previously pinned only as a resource value, never on the element).
  - **AC1 / "do not restyle"** — `CountBadge_UsesFilledBrandAppearance_AsShippedContract` and `ProjectionNewBadge_UsesTintInformativeAppearance_AsShippedContract`: typed `FindComponents<FluentBadge>` assertions pin `Filled`/`Brand` (count) and `Tint`/`Informative` ("New") appearances, not just the testid + text. Adds private helper `FindBadgeByTestId`.
  - **AC2 route shim** — new file `FcHomeRouteViewTests.cs`: `DeclaresRootAndHomeRoutes` reflects the `[Route]` attributes (`/`, `/home`); `MountsFcHomeDirectory` proves the shim renders `<FcHomeDirectory />`. This closed the "re-confirm the route mapping" task item that had no test.
- **Review pass (`bmad-story-automator-review`, 2026-06-04):** adversarial verification re-proved all three ACs true end-to-end (build 0/0 under TWAE; focused run 46/46; Shell.Tests 8 failed / 1701 passed / 1709 total, the 8 being the documented pre-existing/environmental set, none new). Folded the QA gap-pins into this record and the File List (the original dev-story record predated them — recurring retro AI-1 tax paid).

### File List

Modified (test pins only — no `src/` or generated-output change):

- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` — AC1 count + projection/BC "New" badge render pins (dev-story: 9 test cases) **plus** the QA gap-pins `FullNav_RendersLocalizedNavMenuAriaLabel`, `CountBadge_UsesFilledBrandAppearance_AsShippedContract`, `ProjectionNewBadge_UsesTintInformativeAppearance_AsShippedContract` + helper `FindBadgeByTestId` (+3 cases).
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Home/FcHomeDirectoryTests.cs` — AC2 progressive / seeded-flat / accordion-zeros sort pins (3 new test cases).

Added (test pins only — no `src/` or generated-output change):

- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Pages/FcHomeRouteViewTests.cs` — **new file** (QA gap-pin pass). AC2 route-shim pins: `DeclaresRootAndHomeRoutes` (`@page "/"` + `/home`) and `MountsFcHomeDirectory` (+2 cases).
- `_bmad-output/implementation-artifacts/tests/2-2-test-summary.md` — test-automation summary from the `bmad-qa-generate-e2e-tests` pass (artifact, not source).

## Change Log

| Date | Version | Description | Author |
|---|---|---|---|
| 2026-06-04 | 0.1 | Story 2.2 confirm-and-pin complete: verified AC1/AC2/AC3 true against `src/` at `a0cbab2` with ZERO `src/` change; closed AC1 badge-render gap (6 pins) + AC2 sort-ordering gap (3 pins); re-confirmed AC3 (zero change); re-proved the standing 13-failure baseline pre-existing. Status → review. | Amelia (dev-story) |
| 2026-06-04 | 0.2 | QA gap-pin pass: +5 pins (3 nav appearance/aria + 2 route-shim in new `FcHomeRouteViewTests.cs`); test summary written to `tests/2-2-test-summary.md`. | bmad-qa-generate-e2e-tests |
| 2026-06-04 | 0.3 | Adversarial review: re-proved AC1/AC2/AC3 true (build 0/0; focused 46/46; Shell 8 failed / 1701 passed / 1709 total, baseline unchanged). Reconciled File List + Dev Agent Record + test counts against the git tree (folded in the 5 undocumented QA gap-pins + new file — retro AI-1). No `src/` defects found. Status remains → done. | bmad-story-automator-review |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Workflow:** `bmad-story-automator-review` (adversarial, auto-fix) · **Outcome:** ✅ **Approve**

### Verification of claims (git reality vs story)

- **`src/` untouched — TRUE.** `git status` shows only test files changed; no `src/` or generated-output edits. The confirm-and-pin default (zero `src/` change) held.
- **Build 0/0 under TWAE — TRUE.** `dotnet build … -c Release` → 0 Warning(s) / 0 Error(s).
- **Pins green — TRUE.** Focused run of `FrontComposerNavigationTests | FcHomeDirectoryTests | FcHomeRouteViewTests` → **46 passed / 0 failed**.
- **Standing baseline pre-existing — TRUE.** Shell.Tests default lane → **8 failed / 1701 passed / 1709 total**; the 8 are exactly the documented set (3 Generated snapshot/full-page, 4 `PendingStatusReopenGovernanceTests`, 1 `NavigationEffectsLastActiveRouteTests`). None from this story's pins.

### AC validation

- **AC1 (registry-driven `FluentNav` + count/"New" badges)** — IMPLEMENTED & PINNED. Count badge (`Filled`/`Brand`, `fc-nav-badge-*`), projection-"New" (`Tint`/`Informative`, `fc-nav-new-*`), and BC-"New" (`fc-nav-bc-new-*`) render exactly as `FrontComposerNavigation.razor:60-83`. Pins now also cover the rendered `aria-label` and the badge appearance contract.
- **AC2 (home `/` + `/home`, four states, urgency sort)** — IMPLEMENTED & PINNED. Route shim (`FcHomeRouteView.razor:2-3`), four progressive states, and progressive/flat/accordion-zeros sort orderings (`FcHomeDirectory.razor:80-153`) all carry pins; assertions use `StringComparer.Ordinal` matching the component.
- **AC3 (compact collapse → 48px rail / hamburger)** — IMPLEMENTED & PINNED. Re-confirmed green from its existing five angles; zero change, as expected.

### Findings (all record-keeping; no code defects)

| Sev | Finding | Resolution |
|---|---|---|
| MEDIUM | File List omitted new file `FcHomeRouteViewTests.cs` (retro AI-1) | Added to File List |
| MEDIUM | Dev Agent Record omitted 5 QA gap-pins (3 nav appearance/aria + 2 route-shim) | Documented in Completion Notes + File List |
| MEDIUM | Stale "After" test counts (8/1698/1706) | Reconciled to verified 8/1701/1709 |
| LOW | `2-2-test-summary.md` artifact unreferenced | Added to File List |

Test quality is genuine — real assertions against rendered markup and typed `FluentBadge` components, semantic locators, `WaitForAssertion` (no hardcoded waits), order-independent. No placeholders, no stray authoring sentinels (retro AI-2). **0 critical / 0 high findings → story remains `done`.**

_Reviewer: Jérôme Piquot on 2026-06-04_
