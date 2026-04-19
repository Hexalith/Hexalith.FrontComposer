# Tasks / Subtasks

> 11 tasks (plus Task 0 prereq). Each task lists the owning decision(s), AC(s), and the exit criterion.

---

## Task 0 — Prereq verification + FluentNav v5 API spike (45 min)

**Goal:** Confirm the FluentNav v5 surface used by Story 3-2 matches the documentation-at-spec-time. Rebaseline the pre-3-2 test count.

- [ ] **0.1** `rg -c '\[Fact\]\|\[Theory\]' tests/` captures the pre-3-2 `[Fact]` + `[Theory]` count. Record it in `dev-agent-record.md` Completion Notes as `test_baseline_pre_3_2 = <N>` (expected ~580 post-3-1).
- [ ] **0.2** Verify FluentNav v5 `FluentNavCategory.Expanded` + `ExpandedChanged` surface via `mcp__fluent-ui-blazor__get_component_details` for `FluentNavCategory`. Confirm the event fires with a `bool` payload on chevron click.
- [ ] **0.2a** Verify whether `FluentNavCategory` exposes a parameter to override the expand/collapse button's aria-label via the same `mcp__fluent-ui-blazor__get_component_details` call. **If yes:** wire `NavGroupExpandAriaLabel` / `NavGroupCollapseAriaLabel` in Task 6.1 using parameterised localizer lookups with `manifest.Name` as `{0}` (toggle label based on current `Expanded` state). **If no (Fluent UI handles `aria-expanded` internally with no override seam):** delete `NavGroupExpandAriaLabel` + `NavGroupCollapseAriaLabel` from D19, Task 8.5, Task 8.6, and the Task 10.8 parity enumeration — fall back to Fluent UI's built-in `aria-expanded` + `Title`-derived announcement. Added 2026-04-19 — closes SC3 orphaned-resx-key finding from advanced elicitation review.
- [ ] **0.3** Verify `FluentLayoutHamburger.Visible = true` coerces the hamburger to show on viewports ≥ 768 px (normally hidden). If the MCP docs say otherwise, escalate before Task 8 — the entire CompactDesktop tier strategy depends on this.
- [ ] **0.4** Verify `FluentTooltip.Anchor` accepts an element `id` (string). If it requires a component reference instead, adjust `FcCollapsedNavRail` to use `@ref` anchoring (minor internal change — no decision impact).
- [ ] **0.4a** Verify `FluentLayoutItem` passes arbitrary HTML attributes (specifically `id="fc-nav"`) through to its rendered DOM root via `AdditionalAttributes` / unmatched-parameter splatting. **If yes:** Task 8.3a applies `id="fc-nav"` directly on the `FluentLayoutItem`. **If no:** Task 8.3a wraps the Navigation slot content in `<div id="fc-nav">...</div>` inside the `FluentLayoutItem`. Either way, the `#fc-nav` anchor target resolves for the Task 8.3a skip-link. Added 2026-04-19 per round-table finding RT-2.
- [ ] **0.5** Confirm `DomainManifest.Projections` contains dotted FQNs (e.g., `Counter.Domain.Projections.CounterView`) by inspecting `samples/Counter/Counter.Domain/**/*Registration.g.cs`. If the generator emits short names, adjust D2 `BuildRoute` / `ProjectionLabel` impl to handle both shapes.
- [ ] **0.6** Verify `IUserContextAccessor` registration in `AddHexalithFrontComposer` still resolves `NullUserContextAccessor` for test hosts (no Story 3-x changes expected — sanity check).
- [ ] **0.6a** Verify Story 3-1 `IStorageService.GetAsync<T>(key)` behavior on corrupt JSON: does the implementation catch `JsonException` internally and return `null`, OR does it rethrow to the caller? Inspect `Hexalith.FrontComposer.Shell/Services/LocalStorageService.cs` (Story 3-1). **Record the result in `dev-agent-record.md` Completion Notes as `storage_corrupt_json_behavior = rethrow | internal-catch`.** If `internal-catch`, Task 10.6.7's mock-throws-JsonException path is unreachable in production — adjust the test to assert the null-return path instead. If `rethrow`, Task 10.6.7 as-written is correct. Added 2026-04-19 per rubber-duck finding RD-1 (contract ambiguity).
- [ ] **0.7** `dotnet build` clean. Zero warnings baseline confirmed.

**Exit:** 0.1 value captured, 0.2-0.6 all match expectations OR decisions escalated. **Blocks:** Every subsequent task.

---

## Task 1 — `ViewportTier` enum + `NavigationState` feature

**Decisions:** D3, D4. **ACs:** AC3.

- [ ] **1.1** Create `src/Hexalith.FrontComposer.Shell/State/Navigation/ViewportTier.cs` — `public enum ViewportTier : byte { Phone = 0, Tablet = 1, CompactDesktop = 2, Desktop = 3 }` with XML doc comments citing UX spec §22-37 boundaries.
- [ ] **1.2** Create `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` — `public record FrontComposerNavigationState(bool SidebarCollapsed, ImmutableDictionary<string, bool> CollapsedGroups, ViewportTier CurrentViewport)`. `using System.Collections.Immutable;`.
- [ ] **1.3** Create `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationFeature.cs` — `sealed class FrontComposerNavigationFeature : Feature<FrontComposerNavigationState>` with `GetName() => nameof(FrontComposerNavigationState).Replace("State", "")` and `GetInitialState() => new(false, ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal), ViewportTier.Desktop)`.

**Exit:** `dotnet build` clean. Feature type is discoverable by Fluxor's assembly scan.

---

## Task 2 — `NavigationActions` + `NavigationReducers`

**Decisions:** D11, D13, D14, D15. **ACs:** AC2, AC3, AC4.

- [ ] **2.1** Create `NavigationActions.cs` with 5 records:
  ```csharp
  public sealed record SidebarToggledAction(string CorrelationId);
  public sealed record NavGroupToggledAction(string CorrelationId, string BoundedContext, bool Collapsed);
  public sealed record ViewportTierChangedAction(ViewportTier NewTier);
  public sealed record SidebarExpandedAction(string CorrelationId);
  public sealed record NavigationHydratedAction(bool SidebarCollapsed, ImmutableDictionary<string, bool> CollapsedGroups);
  ```
- [ ] **2.2** Create `NavigationReducers.cs` with 5 `[ReducerMethod]`s:
  - `SidebarToggledAction` → `state with { SidebarCollapsed = !state.SidebarCollapsed }`
  - `NavGroupToggledAction` → if `Collapsed`, `state.CollapsedGroups.SetItem(BoundedContext, true)`; else `state.CollapsedGroups.Remove(BoundedContext)` (D11 sparse-by-default)
  - `ViewportTierChangedAction` → `state with { CurrentViewport = NewTier }` (no persistence — D14)
  - `SidebarExpandedAction` → `state with { SidebarCollapsed = false }` (idempotent)
  - `NavigationHydratedAction` → `state with { SidebarCollapsed = action.SidebarCollapsed, CollapsedGroups = action.CollapsedGroups }` (replace, not merge)
- [ ] **2.3** Add XML doc comments citing the binding decisions.

**Exit:** `dotnet build` clean. Reducer methods discoverable by Fluxor.

---

## Task 3 — `NavigationEffects` (hydrate + persist, fail-closed)

**Decisions:** D12, D14, D15, D21. **ACs:** AC2.

- [ ] **3.1** Create `NavigationEffects.cs` following `ThemeEffects.cs` pattern exactly:
  - Constructor: `IStorageService storage, IUserContextAccessor userContextAccessor, ILogger<NavigationEffects> logger, IState<FrontComposerNavigationState> state`.
  - Private `TryResolveScope(out string tenantId, out string userId)` — null/empty/whitespace → log `HFC2105_StoragePersistenceSkipped` + return false.
  - Private const `FeatureSegment = "nav"`.
  - `[EffectMethod] HandleAppInitialized(AppInitializedAction, IDispatcher)` — load blob via `storage.GetAsync<NavigationPersistenceBlob?>(key)`; if non-null, dispatch `NavigationHydratedAction(blob.SidebarCollapsed, blob.CollapsedGroups.ToImmutableDictionary(StringComparer.Ordinal))`; if null, log `HFC2106_ThemeHydrationEmpty` (reused — D15).
  - `[EffectMethod] HandlePersistNavigation(SidebarToggledAction | NavGroupToggledAction | SidebarExpandedAction, IDispatcher)` — serialise `new NavigationPersistenceBlob(state.Value.SidebarCollapsed, state.Value.CollapsedGroups.ToDictionary())` and `storage.SetAsync(key, blob)`. **`NavigationHydratedAction` is intentionally EXCLUDED from the persist-trigger set** (D14 amended 2026-04-18 + ADR-038 — hydrate is read-only; re-persisting the just-loaded blob is wasted I/O and closes the SSR pre-hydration ordering surface).
  - NOTE: Fluxor's `[EffectMethod]` does not union-type inputs; write ONE effect per action type, all three calling the same private `PersistAsync()` helper.
  - **Try/catch placement (added 2026-04-19 per rubber-duck finding RD-2 / RD-4):** the try/catch lives inside the private `PersistAsync()` helper (single site), NOT duplicated across each `[EffectMethod]` handler. Catch differentiates: `OperationCanceledException` → `Logger.LogDebug(ex, "Persist cancelled — circuit disposing")` (silent / low-noise for expected tab-close case); any other `Exception` → `Logger.LogInformation(ex, "HFC2105_StoragePersistenceSkipped ...")` + swallow (no rethrow). `HandleAppInitialized` applies the same pattern: `OperationCanceledException` → Debug; any other `Exception` around `storage.GetAsync<T>` → log `HFC2106_ThemeHydrationEmpty` with structured field `Reason=Corrupt` (vs `Reason=Empty` for the null-blob case — see D15 amendment) and skip the `NavigationHydratedAction` dispatch.
- [ ] **3.2** Create `NavigationPersistenceBlob.cs` (internal sealed record) — `(bool SidebarCollapsed, Dictionary<string, bool> CollapsedGroups)`. `Dictionary<string, bool>` (not ImmutableDictionary) for `System.Text.Json` compatibility without custom converters.
- [ ] **3.3** Register `NavigationEffects` via Fluxor's assembly scan (already active via `AddHexalithFrontComposer` → `ScanAssemblies(typeof(FrontComposerThemeState).Assembly)`). No new DI line.

**Exit:** `dotnet build` clean. NavigationEffects discoverable by Fluxor. No new `FcDiagnosticIds.cs` entry.

---

## Task 4 — `fc-layout-breakpoints.js` JS module

**Decisions:** D5, D6. **ACs:** AC3, AC4, AC5.

- [ ] **4.1** Create `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js`:
  ```javascript
  const QUERIES = {
      desktop: '(min-width: 1366px)',
      compact: '(min-width: 1024px)',
      tablet:  '(min-width: 768px)',
  };

  function computeTier(desktop, compact, tablet) {
      if (desktop) return 3; // Desktop
      if (compact) return 2; // CompactDesktop
      if (tablet)  return 1; // Tablet
      return 0;              // Phone
  }

  export function subscribe(dotnetRef) {
      if (!dotnetRef) {
          throw new Error('fc-layout-breakpoints.subscribe: dotnetRef is required.');
      }
      const mqDesktop = window.matchMedia(QUERIES.desktop);
      const mqCompact = window.matchMedia(QUERIES.compact);
      const mqTablet  = window.matchMedia(QUERIES.tablet);
      let lastTier = computeTier(mqDesktop.matches, mqCompact.matches, mqTablet.matches);
      dotnetRef.invokeMethodAsync('OnViewportTierChangedAsync', lastTier);

      const handler = () => {
          const tier = computeTier(mqDesktop.matches, mqCompact.matches, mqTablet.matches);
          if (tier === lastTier) return; // dedupe — D6
          lastTier = tier;
          dotnetRef.invokeMethodAsync('OnViewportTierChangedAsync', tier);
      };

      mqDesktop.addEventListener('change', handler);
      mqCompact.addEventListener('change', handler);
      mqTablet.addEventListener('change', handler);

      return {
          unsubscribe() {
              mqDesktop.removeEventListener('change', handler);
              mqCompact.removeEventListener('change', handler);
              mqTablet.removeEventListener('change', handler);
          },
      };
  }

  export function unsubscribe(subscription) {
      subscription?.unsubscribe();
  }
  ```
- [ ] **4.2** Confirm `wwwroot/js/` is packaged into the RCL static assets bundle (verified in Story 3-1 for the other two JS modules — no new `.csproj` edits expected).

**Exit:** `dotnet build` emits the module under `_content/Hexalith.FrontComposer.Shell/js/fc-layout-breakpoints.js`.

---

## Task 5 — `FcLayoutBreakpointWatcher.razor`

**Decisions:** D5. **ACs:** AC3, AC4, AC5.

- [ ] **5.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor`:
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Layout
  @implements IAsyncDisposable
  @* Story 3-2 D5 — headless component; subscribes to fc-layout-breakpoints.js on first render. *@
  ```
- [ ] **5.2** Create `FcLayoutBreakpointWatcher.razor.cs` mirroring `FcSystemThemeWatcher.razor.cs`:
  - Constants: `BreakpointModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-layout-breakpoints.js"`.
  - Fields: `_module`, `_subscription`, `_selfRef`.
  - `[Inject] IJSRuntime JS`, `[Inject] IDispatcher Dispatcher`.
  - `OnAfterRenderAsync(bool firstRender)` imports module, creates `DotNetObjectReference`, calls `subscribe`. Idempotent on non-first render.
  - `[JSInvokable] public Task OnViewportTierChangedAsync(int tier)` → `Dispatcher.Dispatch(new ViewportTierChangedAction((ViewportTier)tier)); return Task.CompletedTask;`
  - `DisposeAsync` unsubscribes + disposes the module + self-ref (mirroring the Story 3-1 watcher pattern).
  - **Added 2026-04-19 per D6 amendment + rubber-duck RD-6:** resource-safe try/catch/finally discipline. Sequence inside `OnAfterRenderAsync(firstRender: true)`:
    1. `_selfRef = DotNetObjectReference.Create(this);` — assign BEFORE the import/subscribe call so a mid-subscribe throw still leaves `_selfRef` non-null for `DisposeAsync` cleanup.
    2. `try { _module = await JS.InvokeAsync<IJSObjectReference>("import", BreakpointModulePath); _subscription = await _module.InvokeAsync<IJSObjectReference>("subscribe", _selfRef); }`
    3. `catch (OperationCanceledException) { /* circuit disposing during first-render — swallow silently */ }`
    4. `catch (Exception ex) { Logger.LogWarning(ex, "FcLayoutBreakpointWatcher subscribe failed; viewport stays at {Default}", ViewportTier.Desktop); }`
    5. `DisposeAsync` null-guards every field: `_subscription?.InvokeVoidAsync("unsubscribe")`, `_module?.DisposeAsync()`, `_selfRef?.Dispose()`. Partial-initialization (e.g., `_module` loaded but `_subscription` throw) disposes cleanly without NullRefs.
  - No new `HFC2xxx` diagnostic ID added (D12 discipline preserved). The component stays alive (empty fragment); `CurrentViewport` stays at `ViewportTier.Desktop` feature default and the framework remains usable at Desktop rendering.

**Exit:** Component renders empty fragment. Subscribes + dispatches on first render. Disposes cleanly. Module-load failure is caught, logged as Warning, and does not crash the circuit.

---

## Task 6 — `FrontComposerNavigation.razor` framework sidebar

**Decisions:** D1, D2, D9, D11, D16. **ACs:** AC1, AC3, AC6.

- [ ] **6.1** Create `FrontComposerNavigation.razor` in `Shell/Components/Layout/`:
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Layout
  @using Microsoft.FluentUI.AspNetCore.Components
  @inject IFrontComposerRegistry Registry
  @inject IStringLocalizer<FcShellResources> Localizer

  @if (ShouldRenderCollapsedRail())
  {
      <FcCollapsedNavRail />
  }
  else
  {
      <FluentNav UseIcons="true" UseSingleExpanded="false" Padding="@Padding.All2" Style="height: 100%;"
                 aria-label="@Localizer[\"NavMenuAriaLabel\"].Value">
          @foreach (DomainManifest manifest in Registry.GetManifests())
          {
              <FluentNavCategory Id="@manifest.BoundedContext"
                                 Title="@manifest.Name"
                                 Expanded="@(!IsGroupCollapsed(manifest.BoundedContext))"
                                 ExpandedChanged="@(value => OnGroupExpandedChanged(manifest.BoundedContext, value))">
                  @foreach (string projectionFqn in manifest.Projections)
                  {
                      <FluentNavItem Href="@BuildRoute(manifest.BoundedContext, projectionFqn)">
                          @ProjectionLabel(projectionFqn)
                      </FluentNavItem>
                  }
              </FluentNavCategory>
          }
      </FluentNav>
  }
  ```
- [ ] **6.2** Create `FrontComposerNavigation.razor.cs`:
  - `[Inject] IDispatcher Dispatcher`, `[Inject] IState<FrontComposerNavigationState> NavState`, `[Inject] IUlidFactory UlidFactory`.
  - `private bool ShouldRenderCollapsedRail() => NavState.Value.CurrentViewport == ViewportTier.CompactDesktop || (NavState.Value.CurrentViewport == ViewportTier.Desktop && NavState.Value.SidebarCollapsed);` (D9)
  - `private bool IsGroupCollapsed(string boundedContext) => NavState.Value.CollapsedGroups.TryGetValue(boundedContext, out bool collapsed) && collapsed;`
  - `private void OnGroupExpandedChanged(string boundedContext, bool expanded) => Dispatcher.Dispatch(new NavGroupToggledAction(UlidFactory.CreateString(), boundedContext, Collapsed: !expanded));`
  - `internal static string BuildRoute(string boundedContext, string projectionFqn) { string typeName = projectionFqn.Contains('.') ? projectionFqn[(projectionFqn.LastIndexOf('.') + 1)..] : projectionFqn; return $"/{boundedContext.ToLowerInvariant()}/{ToKebab(typeName)}"; }` — `ToKebab` converts PascalCase → kebab-case.
  - `internal static string ProjectionLabel(string projectionFqn) { string typeName = projectionFqn.Contains('.') ? projectionFqn[(projectionFqn.LastIndexOf('.') + 1)..] : projectionFqn; return Humanize(typeName); }` — `Humanize` splits PascalCase into space-separated words.
- [ ] **6.3** Create `FrontComposerNavigation.razor.css` — minimal scoped CSS (D13 indicates NO FluentNav label hiding; the collapsed rail is a separate component). Only add: `:host { display: block; height: 100%; }`.
- [ ] **6.4** Subscribe `NavState` via `OnInitialized` using `IStateSelection<FrontComposerNavigationState, (ViewportTier, bool, ImmutableDictionary<string, bool>)>` for minimal rerender on viewport / collapsed / groups changes only.

**Exit:** Component compiles, renders FluentNav structure under Desktop tier, renders `FcCollapsedNavRail` under CompactDesktop tier OR manual collapse.

---

## Task 7 — `FcCollapsedNavRail.razor` icon rail (CompactDesktop tier)

**Decisions:** D13. **ACs:** AC4.

- [ ] **7.1** Create `FcCollapsedNavRail.razor`:
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Layout
  @using Microsoft.FluentUI.AspNetCore.Components
  @inject IFrontComposerRegistry Registry
  @inject IStringLocalizer<FcShellResources> Localizer

  <div class="fc-collapsed-rail" role="navigation" aria-label="@Localizer[\"NavMenuAriaLabel\"].Value">
      @foreach (DomainManifest manifest in Registry.GetManifests())
      {
          string anchorId = $"fc-rail-{manifest.BoundedContext}";
          <FluentButton id="@anchorId"
                        Appearance="ButtonAppearance.Stealth"
                        Width="48px"
                        Title="@manifest.Name"
                        OnClick="@(() => OnRailClicked())">
              <FluentIcon Value="@(new Icons.Regular.Size20.Apps())" />
          </FluentButton>
          <FluentTooltip Anchor="@anchorId" Position="TooltipPosition.End">
              @manifest.Name
          </FluentTooltip>
      }
  </div>
  ```
- [ ] **7.2** Create `FcCollapsedNavRail.razor.cs`:
  - `[Inject] IDispatcher Dispatcher`, `[Inject] IUlidFactory UlidFactory`.
  - `private void OnRailClicked() => Dispatcher.Dispatch(new SidebarExpandedAction(UlidFactory.CreateString()));`
- [ ] **7.3** Create `FcCollapsedNavRail.razor.css`:
  ```css
  .fc-collapsed-rail {
      display: flex;
      flex-direction: column;
      width: 48px;
      padding: 4px 0;
      gap: 4px;
      align-items: center;
  }
  ```

**Exit:** Rail renders one 48 px button per manifest with a tooltip. Click dispatches `SidebarExpandedAction`.

---

## Task 8 — `FcHamburgerToggle.razor` + `FrontComposerShell.HeaderCenter` slot

**Decisions:** D7, D8, D9, D10, D18, D19. **ACs:** AC4, AC5, AC7.

- [ ] **8.1** Create `FcHamburgerToggle.razor`:
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Layout
  @using Microsoft.FluentUI.AspNetCore.Components
  @inject IStringLocalizer<FcShellResources> Localizer
  @inject IDispatcher Dispatcher
  @inject IUlidFactory UlidFactory
  @inject IState<FrontComposerNavigationState> NavState

  <FluentLayoutHamburger Visible="@IsVisible"
                         IconTitle="@Localizer[\"HamburgerToggleAriaLabel\"].Value"
                         OnOpened="@OnHamburgerOpened" />
  ```
- [ ] **8.2** Create `FcHamburgerToggle.razor.cs`:
  - `private bool IsVisible => NavState.Value.CurrentViewport != ViewportTier.Desktop || NavState.Value.SidebarCollapsed;`
  - `private void OnHamburgerOpened(LayoutHamburgerEventArgs args) { /* Dispatch SidebarToggledAction only when user explicitly toggles at Desktop — viewport-driven visibility is automatic */ if (args.Opened && NavState.Value.CurrentViewport == ViewportTier.Desktop) { Dispatcher.Dispatch(new SidebarToggledAction(UlidFactory.CreateString())); } }`
  - Use `IStateSelection<FrontComposerNavigationState, (ViewportTier, bool)>` for minimal rerender.
- [ ] **8.3** Modify `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`:
  - Insert `<FcLayoutBreakpointWatcher />` just above `<FcSystemThemeWatcher />` in the Content area.
  - Inside `HeaderStart` default: if `HeaderStart is null` render `<FcHamburgerToggle />` (symmetric to the `Navigation` auto-populate in D18 — adopters who supply their own `HeaderStart` retain full override).
  - Insert a `HeaderCenter` rendering block between the app title stack and the right-side stack: `<FluentStack Horizontal="true">@HeaderCenter</FluentStack>` — omitted when null.
  - In the `Navigation` layout item: if `Navigation is null && Registry.GetManifests().Count > 0`, render `<FrontComposerNavigation />`. Otherwise render `@Navigation` as today. Add `id="fc-nav"` to the Navigation `FluentLayoutItem` (target anchor for the skip-to-navigation link added in Task 8.3a).
- [ ] **8.3a** Insert the skip-to-navigation anchor into `FrontComposerShell.razor` immediately after the existing Story 3-1 `SkipToContent` link: `<a class="fc-skip-link" href="#fc-nav">@Localizer["SkipToNavigationLabel"].Value</a>`. Scoped CSS `.fc-skip-link` inherits from Story 3-1's shared visually-hidden-until-focused pattern (no new CSS — reuse). Gate rendering on `Registry.GetManifests().Count > 0 || Navigation is not null` so the skip-link does not appear when the Navigation pane is omitted entirely. Added 2026-04-19 — closes SC3 orphaned-resx-key finding and wires AC6's `SkipToNavigationLabel` reference + D16's "3-2 adds a `SkipToNavigationLabel` link below [SkipToContent]" statement.
- [ ] **8.4** Modify `FrontComposerShell.razor.cs` — append `[Parameter] public RenderFragment? HeaderCenter { get; set; }`. Add `[Inject] private IFrontComposerRegistry Registry { get; set; } = default!;`. Update XML docs. **Add a class-level XML doc `<remarks>` block (expanded 2026-04-19 per round-table RT-8 — originally scoped to just D10 ordering, now covers all five 3-2 concerns adopters will encounter):**
  1. **Parameter ordering rule (D10):** insertion order = L→R visual header layout (HeaderStart, HeaderCenter, HeaderEnd, Navigation, Footer, ChildContent, AppTitle), NOT alphabetical.
  2. **Navigation auto-populate (D18 / ADR-035):** when `Navigation` is null AND `IFrontComposerRegistry.GetManifests()` returns ≥1 manifest, shell renders `<FrontComposerNavigation />` automatically.
  3. **Opt-out escape hatch for adopters who want NO sidebar despite having registered domains (ADR-035 addendum):** supply `Navigation="@((RenderFragment)(_ => { }))"` (empty render fragment — non-null, bypasses auto-populate, renders nothing). Example usage in `<example>` XML doc tag so IntelliSense surfaces it.
  4. **Skip-to-navigation anchor contract (Task 8.3a):** the Navigation `FluentLayoutItem` carries `id="fc-nav"` (or wraps content in `<div id="fc-nav">` per Task 0.4a spike outcome); `SkipToNavigationLabel` resource string renders an `<a class="fc-skip-link" href="#fc-nav">` link immediately after Story 3-1's SkipToContent link. Both are visually-hidden-until-focused.
  5. **`IFrontComposerRegistry` injection rationale:** required by the auto-populate check in (2); a scoped Fluxor `[Inject]` would not suffice because the registry is queried at render time, not reducer time.
- [ ] **8.5** Modify `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` — add 5 keys: `NavMenuAriaLabel`, `HamburgerToggleAriaLabel`, `NavGroupExpandAriaLabel`, `NavGroupCollapseAriaLabel`, `SkipToNavigationLabel`.
- [ ] **8.6** Modify `FcShellResources.fr.resx` — add the same 5 keys with French translations per D19.

**Exit:** Shell renders with hamburger in HeaderStart (default), optional HeaderCenter slot, auto-populated Navigation. Resource parity test passes.

---

## Task 9 — Counter.Web `MainLayout.razor` rewire (remove adopter nav block)

**Decisions:** D17, D18. **ACs:** AC7.

- [ ] **9.1** Modify `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` — delete the `<Navigation>…</Navigation>` render fragment AND the `@inject IFrontComposerRegistry Registry` line. Final contents:
  ```razor
  @inherits LayoutComponentBase
  @using Hexalith.FrontComposer.Shell.Components.Layout

  <FrontComposerShell>@Body</FrontComposerShell>
  ```
- [ ] **9.2** Run `dotnet run --project samples/Counter/Counter.Web` locally (or via Aspire MCP per `feedback_no_manual_validation.md`). Confirm the Counter bounded-context sidebar renders automatically. Screenshot via Aspire MCP browser / GIF into `dev-agent-record.md` if available.

**Exit:** Counter.Web MainLayout is three substantive lines. Framework sidebar visible at Desktop.

---

## Task 10 — Tests

**Decisions:** ALL. **ACs:** ALL.

- [ ] **10.1** `FrontComposerNavigationTests.cs` — 8 tests (D1, D2, D11, D16, D18):
  1. `RendersOneCategoryPerManifest` — registry with 2 manifests (each with ≥1 projection) → 2 `FluentNavCategory` elements.
  2. `RendersOneItemPerProjection` — manifest with 3 projections → 3 `FluentNavItem` elements inside its category.
  3. `DoesNotRenderCommandsAsNavItems` — manifest with 2 projections + 3 commands → still 2 items.
  4. `HidesCategoryWhenProjectionsEmpty` — registry with one manifest that has 0 projections + ≥1 command → ZERO `FluentNavCategory` rendered (D1 clarification: no empty-category shell). Added 2026-04-18.
  5. `BuildRouteProducesExpectedHref` — **`[Theory]` over projection FQN shapes** (added 2026-04-19 per round-table RT-1; acronyms and single-letter prefixes were unexercised):
     - `Counter.Domain.Projections.CounterView` in `Counter` → `/counter/counter-view` (baseline happy path).
     - `Orders.Domain.Projections.HTTPRequestView` in `Orders` → `/orders/http-request-view` (ALL-CAPS acronym at start — boundary: not `/orders/h-t-t-p-request-view`).
     - `Experiments.Domain.Projections.ABTestSetup` in `Experiments` → `/experiments/ab-test-setup` (2-letter acronym prefix).
     - `Telemetry.Domain.Projections.IPv6Config` in `Telemetry` → `/telemetry/i-pv6-config` OR `/telemetry/ipv6-config` (acronym + digit + word — **test asserts the chosen convention** to pin it down; dev decides, then updates test if mapping differs from the most-natural-English expectation).
     - `Reports.Domain.Projections.Report2024` in `Reports` → `/reports/report2024` (CamelCase + digits without separator).
     - Short FQN (no `.` separator): `Counter.Domain.Projections.View` in `Counter` → `/counter/view` (last-dot fallback).
     - `ProjectionLabel` is tested in parallel on the same FQN inputs → "Counter View", "HTTP Request View", "AB Test Setup", etc., so the Humanize + Kebab helpers share the `[Theory]` data source.
  6. `ExpandedStateBindsToCollapsedGroups` — state with `CollapsedGroups["Counter"] = true` → category's `Expanded = false`.
  7. `NavGroupToggledDispatchesOnExpandedChange` — simulate `ExpandedChanged(false)` → dispatcher receives `NavGroupToggledAction(_, "Counter", true)`.
  8. `NavItemsAreTabReachable` — bUnit finds all `<a>` / `<button>` in the rendered output; asserts no `tabindex="-1"` on non-decorative elements.
- [ ] **10.2** `FcCollapsedNavRailTests.cs` — 3 tests (D13):
  1. `RendersOneButtonPerManifest`.
  2. `TooltipContainsBoundedContextName`.
  3. `ClickDispatchesSidebarExpanded` with non-empty ULID correlation.
- [ ] **10.3** `FcHamburgerToggleTests.cs` — 3 tests (D8, D9):
  1. `VisibleFalseAtDesktopWhenNotCollapsed`.
  2. `VisibleTrueAtCompactDesktop` (also covers Tablet / Phone via `[Theory]` with all three tiers).
  3. `ManualToggleAtDesktopDispatchesSidebarToggled`.
- [ ] **10.4** `FcLayoutBreakpointWatcherTests.cs` — 6 tests (D5, D6):
  1. `ImportsModuleOnFirstRender`.
  2. `DispatchesInitialTierOnSubscribe`.
  3. `DispatchesOnSubsequentOnViewportTierChangedAsyncCall`.
  4. `DisposesCleanly` — asserts unsubscribe + DotNetObjectReference disposal.
  5. `DedupesWhenComposedTierUnchanged` — simulate two rapid-succession `OnViewportTierChangedAsync` calls with the SAME int value (e.g. two matchMedia change events crossing different queries that compose back to the same tier) → dispatcher receives EXACTLY ONE `ViewportTierChangedAction` (D6 dedup). Added 2026-04-18 per Murat's contract-test gap finding.
  6. `DispatchesCorrectTierOnDoubleBoundarySkip` — simulate a viewport jump 1367px → 1023px via one `OnViewportTierChangedAsync(1)` call (Tablet) — asserts the watcher dispatches `ViewportTierChangedAction(ViewportTier.Tablet)` with no intermediate `CompactDesktop` action (composed-int ladder collapses both crossings). Added 2026-04-18 per Dr. Quinn's skip-tier finding.
  7. `ImportFailureDoesNotThrowAndLogsWarning` — **`[Theory]` over exception types** (added 2026-04-19 per rubber-duck RD-5; production surfaces more than just `JSException`):
     - `JSException` — generic JS interop failure → asserts Warning log.
     - `HubException` — SignalR transport failure → asserts Warning log.
     - `OperationCanceledException` — circuit disposal mid-subscribe → asserts **Debug** log (not Warning — expected cancellation path per RD-4), swallowed silently.
     - Generic `Exception` — catch-all → asserts Warning log.
     For all four cases: (a) no exception propagates out of `OnAfterRenderAsync` (Fluxor circuit stays alive), (b) no `ViewportTierChangedAction` dispatched (viewport stays at `ViewportTier.Desktop` feature default), (c) `DisposeAsync` null-guard path is exercised cleanly (no NullRefs) when partial `_module` / `_subscription` state exists. Added 2026-04-19 per D6 amendment + FMEA-F1 + RD-5/RD-6 findings.
- [ ] **10.5** `NavigationReducerTests.cs` — 5 tests (D11, D13, D14, D15):
  1. `SidebarToggledFlipsFlag`.
  2. `NavGroupToggledCollapseAddsEntry` / `NavGroupToggledExpandRemovesEntry` (`[Theory]` with two cases).
  3. `ViewportTierChangedOnlyUpdatesCurrentViewport` — asserts `SidebarCollapsed` and `CollapsedGroups` unchanged.
  4. `SidebarExpandedIsIdempotent`.
  5. `NavigationHydratedReplacesWholesale`.
- [ ] **10.6** `NavigationEffectsScopeTests.cs` — 6 bUnit tests (D12, D14, D23, ADR-038):
  1. `PersistsOnValidScope` — dispatch `SidebarToggledAction` → `storage.SetAsync` called exactly once with `{tenantId}:{userId}:nav` key.
  2. `SkipsOnNullTenant` — logger receives `HFC2105_StoragePersistenceSkipped` Information; **AND separately**: `storage.SetAsync` is NEVER called. Both assertions required (Murat's fail-closed tightening).
  3. `SkipsOnNullUser` — symmetric; both log + no-call assertions.
  4. `SkipsOnWhitespaceUserContext` — tenant or user is a whitespace-only string → both log + no-call assertions (previously implicit in D12; now explicit test per Murat's feedback).
  5. `ViewportTierChangedDoesNotTriggerPersist` — dispatch `ViewportTierChangedAction` → `storage.SetAsync` is NEVER called (D14, ADR-037).
  6. `HydrateDoesNotRePersist` — dispatch `NavigationHydratedAction` → `storage.SetAsync` is NEVER called (D14 amendment, ADR-038). Added 2026-04-18.
  7. `HydrateCorruptBlobLogsAndDefaults` — `IStorageService` mock configured so `GetAsync<NavigationPersistenceBlob?>` throws `JsonException` (simulating corrupt JSON / schema drift / manual tampering). Dispatch `AppInitializedAction`. Assert: (a) effect does not throw (Fluxor dispatcher completes), (b) `HFC2106_ThemeHydrationEmpty` logged at Information with exception payload, (c) no `NavigationHydratedAction` dispatched (feature defaults apply — `SidebarCollapsed=false`, `CollapsedGroups=empty`). Added 2026-04-19 per D15 amendment + PM1/FMEA-F2 finding.
  8. `PersistStorageFailureLogsNotThrows` — **`[Theory]` over exception types** (added 2026-04-19 per rubber-duck RD-3/RD-5; wording tightened to clarify state-read timing):
     - `InvalidOperationException` — transient I/O fault → asserts Information-level `HFC2105_StoragePersistenceSkipped` log.
     - `JSException` with quota-like message — LocalStorage quota exhaustion via JS interop surface → asserts Information-level `HFC2105_StoragePersistenceSkipped` log.
     - `OperationCanceledException` — circuit disposal mid-persist → asserts **Debug-level** log (not Information — per RD-4, expected cancellation should not generate routine Information entries).
     For all three cases: (a) no exception propagates out of `HandlePersistNavigation` (Fluxor dispatcher completes cleanly), (b) **after the dispatch resolves** (Fluxor ordering: reducer → effect), the bUnit state read `state.Value.SidebarCollapsed` reflects the toggled value — correctness of in-memory state is independent of persistence success. Added 2026-04-19 per D15 amendment + PM5/FMEA-F3/CH2/RD-3/RD-5 findings.
- [ ] **10.7** `NavigationPersistenceSnapshotTests.cs` — 1 Verify snapshot (D21):
  ```csharp
  await Verify(JsonSerializer.Serialize(new NavigationPersistenceBlob(true, new() { ["Counter"] = true, ["Orders"] = false })));
  ```
  The `.verified.txt` locks the exact JSON shape: `{"SidebarCollapsed":true,"CollapsedGroups":{"Counter":true,"Orders":false}}`. No `CurrentViewport` / `viewport` / `tier` property present (ADR-037).
- [ ] **10.8** Modify `FcShellResourcesTests.cs` — extend the existing parity enumeration to cover the 5 new keys (existing `CanonicalKeysHaveFrenchCounterparts` test self-updates from the resx file; add explicit lookups for the new keys to assert non-empty EN + FR values). `NavGroupExpandAriaLabel` / `NavGroupCollapseAriaLabel` round-trip `{0}` placeholder ("Expand Counter" / "Développer Counter").
- [ ] **10.9** Modify `FrontComposerShellParameterSurfaceTests.cs` verified.txt — parameter count 8 → 9; new entry `HeaderCenter : RenderFragment?` inserted between `HeaderStart` and `HeaderEnd`.
- [ ] **10.10** Modify `FrontComposerShellTests.cs` — 3 new tests (D17, D18, Task 8.3a):
  1. `AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty`.
  2. `AdopterSuppliedNavigationFragmentWins`.
  3. `RendersSkipToNavigationLinkWithCorrectAnchor` — render shell with a non-empty registry; bUnit asserts an `<a class="fc-skip-link" href="#fc-nav">` element exists in DOM AND the target element with `id="fc-nav"` exists on the Navigation `FluentLayoutItem`. Also assert the link is visually-hidden-until-focused (class applied, not inline style). Added 2026-04-19 per AC6 amendment.
- [ ] **10.11** `SidebarResponsiveE2ETests.cs` — 2 Playwright tests (AC4, AC5, AC7):
  1. `ResizeAcrossTiers` — boot Counter.Web, resize 1920 → 1200 → 800 → 600, assert DOM transitions (full nav → rail → drawer-closed → drawer-closed).
  2. `SidebarCollapsePersistsAcrossRefresh` — at 1920 px, toggle hamburger (manual collapse), refresh, assert sidebar still collapsed.
  3. **Latency capture (added 2026-04-19 per round-table RT-6; turns ADR-037/038 "~50-300 ms typical" speculation into data):** during both tests above, measure the wall-clock delta between (a) first render complete marker (`FrontComposerShell` rendered sentinel) and (b) first `ViewportTierChangedAction` dispatched (Fluxor DevTools trace or a test-only `IDispatcher` decorator). Capture `min / p50 / p99` across at least 10 repeated runs and append to `dev-agent-record.md` Completion Notes under `blazor_server_first_tier_dispatch_latency_ms`. If observed p99 > 1000 ms, escalate as a spec-change proposal (ADR-037/038 claims need revision). Not a pass/fail gate in 3-2; observational only.
- [ ] **10.12** PR-review gate (reviewer-applied, not dev-applied) — reviewer inspects the 10.1-10.11 suite and decides whether the ~42-test budget justifies the 23 decisions. If ratio drops below 1.5 (Murat's under-coverage floor), reviewer identifies trim candidates; if above 1.8, reviewer identifies add candidates. **Note 2026-04-19:** post-elicitation ratio is 1.83 — marginally over the 1.8 ceiling but each added test targets a specific amended-decision fail-closed surface per `feedback_tenant_isolation_fail_closed.md`. Preserve rather than trim.
- [ ] **10.13** CI snapshot-drift gate — confirm `FrontComposerShellParameterSurfaceTests.verified.txt` drift (8 → 9 parameters, Task 10.9) and `NavigationPersistenceSnapshotTests.verified.txt` (new baseline, Task 10.7) are both behind a **hard-fail** CI check on unreviewed drift, not an advisory warning (Murat's gate-theater check). If the project's Verify gate is currently advisory, flag as a sprint-local infra fix rather than silently accepting unreviewed changes.

**Exit:** `dotnet test` green. New test count matches `test_baseline_pre_3_2 + 42` within ±2 after the PR-review gate (raised from +38 to +42 on 2026-04-19 during advanced elicitation review: +1 watcher import-failure, +2 hydrate/persist storage-error paths, +1 skip-link DOM assertion). Snapshot-drift CI gate confirmed as hard-fail.

---

## Task 11 — Zero-warning gate + regression baseline

- [ ] **11.1** `dotnet build --configuration Release -warnaserror` clean. Zero new warnings. If a new FluentNav v5 obsolete warning surfaces, escalate rather than suppressing.
- [ ] **11.2** Run the full Story 3-1 test suite under `tests/` to confirm no regression. Record the cumulative `[Fact]` + `[Theory]` count post-3-2 in `dev-agent-record.md`.
- [ ] **11.3** Update `_bmad-output/implementation-artifacts/sprint-status.yaml` → `3-2-sidebar-navigation-and-responsive-behavior: in-progress → ready-for-review` (this transition is handled by the dev-story workflow, NOT by Story 3-2 creation).

**Exit:** All tests green + zero warnings + Counter.Web sample boots with the framework sidebar.

---
