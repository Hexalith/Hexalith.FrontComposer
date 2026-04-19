# Architecture Decision Records

## ADR-035: `FrontComposerNavigation` auto-populates `FrontComposerShell.Navigation` from `IFrontComposerRegistry`

**Status:** Accepted (Story 3-2)

**Context:** Story 3-1 D25 preserved the Counter.Web adopter-authored `FluentNav` block inside `FrontComposerShell.Navigation` so the sample wouldn't render an empty 220 px navigation column during the 3-1 → 3-2 sprint gap. That preservation was explicitly scoped as temporary ("Story 3-2 replaces with the framework-owned sidebar"). Story 3-2 now owns the framework sidebar. The choice is: how does an adopter opt in to the framework sidebar? Explicit boilerplate (`<FrontComposerShell><Navigation><FrontComposerNavigation /></Navigation>…`) or convention-based auto-population (`<FrontComposerShell>@Body</FrontComposerShell>` — Shell detects empty Navigation + non-empty registry and auto-renders the sidebar)?

**Decision:** When the adopter leaves the `Navigation` render fragment null AND `IFrontComposerRegistry.GetManifests()` returns ≥ 1 manifest, `FrontComposerShell` auto-renders `<FrontComposerNavigation />` inside the Navigation `FluentLayoutItem`. When the adopter supplies a `Navigation` fragment, the adopter wins. When the registry is empty, the Navigation layout area is OMITTED entirely (behaviour inherited from Story 3-1 AC1 Nav-hide-when-null addendum).

**Rejected alternatives:**
- **Always render the framework sidebar.** Breaks the Story 3-1 D25 escape-hatch discipline — adopters migrating from custom nav architectures need a quiet opt-out path that doesn't require explicit `Navigation="@null"` hacks. Also conflicts with the Override escape hatch ADR-027 cites.
- **Require adopters to pass `<FrontComposerNavigation />` explicitly.** One boilerplate line in every `MainLayout.razor` — defeats the three-line-layout pitch that Story 3-1 D3 established. Counter.Web onboarding becomes 4 lines, not 3.
- **Emit a build warning when `Navigation` is null and registry has manifests.** Roslyn analyzer cost (new HFC1xxx code, new test fixture, new documentation) exceeds the ergonomic benefit. The auto-populate IS the fix, not a warning.
- **Use a separate `UseFrameworkNavigation` (bool, default true) option.** Adds a parameter without adding information: "auto-populate with framework sidebar unless adopter provides a Navigation fragment" is already the semantic. An extra bool is redundant state.
- **Named `FcEmptyFragment` marker component as the canonical opt-out (e.g., `<Navigation><FcEmptyFragment /></Navigation>`).** Considered 2026-04-19 per round-table finding RT-5 — the `Navigation=@((RenderFragment)(_ => { }))` idiom is cryptic and ungoogleable for adopters. Rejected for v1: adds a new public component surface (violates D22's "no new adopter surfaces in 3-2"), and the opt-out path is niche (adopters with registered domains who want NO sidebar — rare enough that Epic 6 customisation may produce a better pattern). Mitigation: Task 8.4's class-level `<remarks>` XML documents the idiom with an `<example>` tag so IntelliSense surfaces it at authoring time. Reconsider in v1.x if adopter feedback indicates discoverability failure.

**Consequences:**
- Counter.Web `MainLayout.razor` collapses to its three-line form (D17).
- Adopters with custom navigation pass a `Navigation` fragment and see no behaviour change.
- Empty-registry bootstrap (Story 1-x scenarios, tests without a registered domain) renders no Navigation pane — no regression.
- `FrontComposerShell.razor.cs` gains an `[Inject] IFrontComposerRegistry Registry` reference; the auto-populate render block checks `Navigation is null && Registry.GetManifests().Count > 0` at render time.
- `FrontComposerShellTests` gains one test for the auto-render path and one for the override path (Task 10.10).
- **Opt-out escape hatch for adopters with registered domains who want NO sidebar** (clarified 2026-04-19 during advanced elicitation review): supply an empty render fragment — `Navigation=@((RenderFragment)(_ => { }))`. The parameter is non-null so the auto-populate branch is bypassed; the empty fragment renders nothing inside the Navigation `FluentLayoutItem` (which is still omitted entirely when the registry is empty). Null continues to trigger auto-populate. This closes the PM3 escape-hatch ambiguity flagged during advanced elicitation review.

**Verification:** `FrontComposerShellTests.AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty` + `FrontComposerShellTests.AdopterSuppliedNavigationFragmentWins` (Task 10.10).

---

## ADR-036: Three-tier responsive breakpoint via JS `matchMedia` (not pure CSS container queries)

**Status:** Accepted (Story 3-2)

**Context:** UX spec §22-37 defines four viewport tiers (Desktop ≥ 1366, CompactDesktop 1024–1365, Tablet 768–1023, Phone < 768). The framework must react to tier changes in at least three ways: (1) sidebar rendering (full nav / icon rail / drawer) — Story 3-2; (2) density forcing to `Comfortable` at ≤ Tablet for 44 px touch targets — Story 3-3; (3) command palette overlay layout — Story 3-4. Pure CSS (`@media` / `@container`) handles case (1) visually but cannot trigger the density forcing effect in (2), which must dispatch a Fluxor action to update `FrontComposerDensityState` so every density-aware component re-renders. That forces a JS → C# signal path.

**Decision:** `FcLayoutBreakpointWatcher.razor` imports `fc-layout-breakpoints.js`, which composes three `matchMedia` queries (`(min-width: 1366px)`, `(min-width: 1024px)`, `(min-width: 768px)`) into a single `ViewportTier` integer and invokes `OnViewportTierChangedAsync(int tier)` via `DotNetObjectReference`. The watcher dispatches `ViewportTierChangedAction((ViewportTier)tier)` into Fluxor. Stories 3-3 / 3-4 / 3-5 subscribe to `IState<FrontComposerNavigationState>.CurrentViewport` via `IStateSelection`.

**Rejected alternatives:**
- **Pure CSS `@media` / `@container` rules.** Handles visual changes but cannot drive Fluxor state. Story 3-3 density override needs a reactive state flip, not a style override — components that consume `DensityState` (DataGrids, forms, nav) must re-render when the forcing activates. Splitting some tier reactions into CSS and others into Fluxor creates two sources of truth and inevitable divergence.
- **`FluentLayout.OnBreakpointEnter` alone.** FluentLayout has a single breakpoint at `MobileBreakdownWidth` (default 768). Not enough granularity for the 3-tier matrix. Using it plus a second custom mechanism doubles the source-of-truth surface.
- **`ResizeObserver` on the shell element.** Same expressivity as `matchMedia` but returns raw pixel dimensions, losing the semantic "tier" abstraction. Would require every consumer to redo the 1366 / 1024 / 768 thresholding.
- **C#-side `InvokeAsync<int>("innerWidth")` polling on a timer.** Round-trip latency + battery / CPU cost per poll tick. `matchMedia` is event-driven at the browser layer — one event per crossing, not one event per 100 ms.

**Consequences:**
- Two JS modules now under `wwwroot/js/`: `fc-prefers-color-scheme.js` (Story 3-1) and `fc-layout-breakpoints.js` (Story 3-2). The shape is deliberately identical (subscribe / unsubscribe + one-shot emission on subscribe) so future watchers follow the pattern.
- Watcher runs per circuit. Blazor Server incurs one interop round-trip per tier crossing; WASM runs client-side entirely. Tier crossings are rare (user physically resizing a window) so the Server cost is negligible.
- Deduplication inside the JS module: if a `change` event fires on one of the three queries but the composed tier is unchanged, no C# call is made. The 1366 / 1024 / 768 boundaries only produce a tier change when crossed.

**Verification:** `FcLayoutBreakpointWatcherTests` (Task 10.4) asserts that the subscribe / unsubscribe / re-entrant-dispatch / dispose lifecycle matches `FcSystemThemeWatcherTests`.

---

## ADR-037: `ViewportTier` is derived-at-runtime, never persisted

**Status:** Accepted (Story 3-2)

**Context:** `FrontComposerNavigationState.CurrentViewport` is part of the reducer state, so a naive JSON round-trip would include it in the persisted blob. Should the framework persist the last-seen viewport tier? Other candidate behaviours: persist it and use it as a first-render hint; persist the user's "preferred" tier; persist nothing.

**Decision:** `ViewportTier` is an observed runtime value and is NEVER persisted to `LocalStorageService`. `NavigationPersistenceBlob` contains only `SidebarCollapsed` + `CollapsedGroups`. On app init, `NavigationEffects.HandleAppInitialized` hydrates the two persisted fields via `NavigationHydratedAction`. `CurrentViewport` stays at the feature default (`ViewportTier.Desktop`) until `FcLayoutBreakpointWatcher` dispatches the first `ViewportTierChangedAction` (which happens synchronously on the watcher's subscribe emission — one JS interop hop after first render).

**Rejected alternatives:**
- **Persist the viewport tier.** Bootstrap paradox — on page reload, the server renders the pre-interactive HTML before the JS module has a chance to measure the browser. If we hydrate from persistence ("you were at CompactDesktop last time"), the SSR pass renders the icon rail; when JS runs and the browser turns out to be 1920 px wide, the rail snaps to full nav — user sees a visible layout shift (FOIT/FOUC class). Persisting nothing and starting at `Desktop` default means the wrong render for tablet/phone users on the first frame but recovers without a component swap (FluentLayoutHamburger activates the right UI before the user notices).
- **Persist a user-preferred tier separately from observed tier.** Conflates preference with observation. `SidebarCollapsed` already captures the user's preference signal; the tier itself is an observation of the browser. Users don't think "I prefer CompactDesktop at 1920 px" — they think "I prefer my sidebar collapsed".
- **Use CSS `@media` for first-render and JS for subsequent changes.** Possible but doubles the source-of-truth surface and breaks Story 3-3's ability to drive density forcing from a single Fluxor state (see ADR-036). Inside the consequences of ADR-036, persisting viewport would resurrect the second source of truth.

**Consequences:**
- Hydration on app init delivers `SidebarCollapsed` + `CollapsedGroups` but leaves `CurrentViewport` at default `ViewportTier.Desktop`. The first viewport-tier dispatch from `FcLayoutBreakpointWatcher` (fired on the watcher's initial subscribe emission) arrives within a single render cycle on **WASM** (sub-frame latency); on **Blazor Server**, one SignalR interop round-trip (typical ~50-300 ms; longer on high-latency networks). Users on slow Blazor Server connections may interact with UI under the Desktop default tier during this window — this is safe by construction (no persist triggers can fire in this window per D23/ADR-038), producing only a visible layout swap when the watcher arrives. Latency framing clarified 2026-04-19 during advanced elicitation review (the original "~a frame in the WASM worst case" wording silently omitted the Blazor Server upper bound).
- Task 10.6 `NavigationEffectsScopeTests` includes a test that a blob round-trip (persist → hydrate) preserves `SidebarCollapsed` + `CollapsedGroups` but does NOT include a viewport field. The `NavigationPersistenceSnapshotTests.cs` verified.txt additionally asserts the absence of any `viewport` / `tier` property.
- Any future story that wants to restore tier-aware UI (e.g., "remember which tier the user last confirmed an 'expand all' choice on") must model it as a separate persisted field, not lean on `CurrentViewport`.
- Storage failure contract (added 2026-04-19): deserialization exceptions and storage I/O errors are caught at the effect boundary per D15 (amended 2026-04-19); the persisted blob is effectively single-writer-single-reader per `{tenantId}:{userId}:nav` key, so no concurrency contract is needed beyond `IStorageService`'s existing guarantees from Story 3-1.

**Verification:** `NavigationPersistenceSnapshotTests` verified.txt baseline (Task 10.7); `NavigationEffectsScopeTests.ViewportTierChangedDoesNotTriggerPersist` (Task 10.6).

---

## ADR-038: `NavigationHydratedAction` does not trigger re-persistence; SSR renders at `ViewportTier.Desktop` default

**Status:** Accepted (Story 3-2, added 2026-04-18 during party-mode review — amends D14, companion to D23)

**Context:** Blazor Server's pre-interactive render pass runs before `FcLayoutBreakpointWatcher` can measure the browser, so `FrontComposerNavigationState.CurrentViewport` starts at the feature default (`ViewportTier.Desktop`, ADR-037). The first interactive render triggers `FcLayoutBreakpointWatcher.OnAfterRenderAsync(firstRender: true)`, which synchronously dispatches `ViewportTierChangedAction(actualTier)` via the `matchMedia` subscribe emission. During this same first-render window, Fluxor's `HandleAppInitialized` loads the persisted blob from `IStorageService` and dispatches `NavigationHydratedAction`. As originally authored, D14 included `NavigationHydratedAction` in the persist-trigger set — producing a hydrate → persist round-trip that writes the same blob back to LocalStorage immediately after reading it. Party-mode review (Dr. Quinn + Winston + Sally) flagged this as an ordering surface: can a blob ever be persisted while `CurrentViewport` still holds the pre-hydration Desktop default, and could that state be observed by another effect mid-write?

**Decision:** (a) `NavigationHydratedAction` is REMOVED from `HandlePersistNavigation`'s trigger set. Hydrate is read-only from the perspective of `IStorageService`. (b) SSR / first render commits to `ViewportTier.Desktop` (feature default) as the rendering tier. (c) The layout swap that may occur when the first `ViewportTierChangedAction` resolves to a non-Desktop tier is accepted — see **ADR-037 Consequences** for the WASM vs Blazor Server latency breakdown (framing consolidated 2026-04-19 per Occam simplification; ADR-037 is the canonical home for the latency discussion). Task 10.11 captures observed first-tier-dispatch latency distribution on Blazor Server so the ADR-037 claims can be validated against real data. This is safe by construction: no persist triggers are reachable in the pre-watcher window, so the swap is a visible layout transition only, never data corruption.

**Rejected alternatives:**
- **Persist a viewport hint in a cookie so SSR can read the real tier.** Crosses the HTTP-cookie / Blazor-interop boundary, adds a new persistence surface, and still cannot cover first-ever-visit cold loads (no cookie exists yet). Disproportionate complexity for a single-frame visual concern.
- **Keep the hydrate-persist round-trip, add a transient `_hydrating` flag to suppress the write.** Introduces mutable transient state into a Fluxor reducer pattern that every other feature (Theme, Density) keeps immutable. Violates reducer discipline.
- **Gate `HandlePersistNavigation` on `ViewportTierChangedAction` having fired at least once.** Couples two unrelated actions through a runtime flag and does not close the race — a user-triggered `SidebarToggled` during the sub-frame pre-hydration window (theoretical only; user cannot physically click) would still write. Complexity without correctness gain.
- **Default `CurrentViewport` to an `Unknown` sentinel until the watcher fires.** Forces every downstream consumer (Story 3-3 density effect, Story 3-4 palette layout, Story 3-5 home grid) to handle three-state logic when the only two legitimate behaviors are "Desktop default" and "measured tier." Expands the contract surface for no adopter benefit.

**Consequences:**
- Task 3.1's `HandlePersistNavigation` effect list shrinks from four actions to three (`SidebarToggledAction`, `NavGroupToggledAction`, `SidebarExpandedAction`). `NavigationHydratedAction` remains the terminal reducer input for the hydrate path but does NOT fan out to a storage write.
- `NavigationEffectsScopeTests` gains `HydrateDoesNotRePersist` (Task 10.6).
- The "SSR paints tier X, user interacts, blob persists under tier X" scenario is structurally eliminated: viewport was never in the blob (ADR-037), hydrate no longer writes (ADR-038), and user-triggered persists always run after the watcher has reported the measured tier.
- Story 3-6 (session persistence & context restoration) reads the nav blob unchanged — the blob schema is untouched.
- No new diagnostic ID. HFC2105 / HFC2106 usage is unchanged.

**Verification:** `NavigationEffectsScopeTests.HydrateDoesNotRePersist` (Task 10.6). The existing `ViewportTierChangedDoesNotTriggerPersist` test continues to apply unchanged.

---
