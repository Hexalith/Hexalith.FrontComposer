# Tasks / Subtasks

> 11 tasks (plus Task 0 prereq). Each task lists the owning decision(s), AC(s), and the exit criterion. Post-review amendments (2026-04-19): Task 4 gains subtask 4.2b (`FcDensityAnnouncer` per D20); Task 10 gains subtasks 10.4a (reducer purity lint), 10.6b (announcer tests), 10.9b (preview forced-viewport badge), 10.10a (Ctrl+, single-binding lint); Task 10.6 and Task 10.12 are tightened per Murat review.

---

## Task 0 — Prereq verification + FluentDialog / FluentRadioGroup v5 API spike (30 min)

**Goal:** Confirm the Fluent UI v5 dialog + radio-group surfaces match documentation-at-spec-time. Rebaseline the pre-3-3 test count.

- [ ] **0.1** `rg -c '\[Fact\]\|\[Theory\]' tests/` captures the pre-3-3 `[Fact]` + `[Theory]` count. Record in `dev-agent-record.md` Completion Notes as `test_baseline_pre_3_3 = <N>` (expected ~613 post-3-2).
- [ ] **0.2** Verify `FluentDialog` + `IDialogService.ShowDialogAsync<T>` surface via `mcp__fluent-ui-blazor__get_component_details` for `FluentDialog`. Confirm: (a) `DialogParameters.Modal`, `Width`, `PreventDismissOnOverlayClick` accepted; (b) built-in close `×` icon renders by default; (c) `Escape` key closes by default; (d) initial focus auto-trapped on first focusable element.
- [ ] **0.3** Verify `FluentRadioGroup<TValue>` v5 surface via `mcp__fluent-ui-blazor__get_component_details` — confirm `Value` + `ValueChanged` bind semantics + `FluentRadio<TValue>` child pattern. If the component name differs (`FluentRadioGroup` vs `FluentRadioGroup<T>`), adjust Task 6.1 component-reference code accordingly.
- [ ] **0.4** Verify `Icons.Regular.Size20.Settings` exists in the Fluent UI v5 icons package. If the icon name is `Options` / `Gear` / another alias instead, note in `dev-agent-record.md` and adjust Task 5.1.
- [ ] **0.5** Confirm `<body>` element is directly manipulable via `IJSRuntime.InvokeVoidAsync("document.body.setAttribute", ...)` in Blazor Server (SignalR round-trip is safe; no Blazor circuit-root conflicts). Inspect Story 3-1 `fc-beforeunload.js` / Story 3-2 `fc-layout-breakpoints.js` for the established JS interop pattern — `fc-density.js` mirrors them.
- [ ] **0.6** Confirm `IState<FrontComposerNavigationState>` is resolvable from `DensityEffects` constructor (Fluxor DI: scoped effect class receives scoped state). If the cross-feature state read requires a different Fluxor API (`IStateSelection<,>`), adjust Task 3.2.
- [ ] **0.7** `dotnet build` clean. Zero warnings baseline confirmed.

**Exit:** 0.1 value captured, 0.2-0.6 all match expectations OR decisions escalated. **Blocks:** Every subsequent task.

---

## Task 1 — `FcShellOptions.DefaultDensity` + `DensityPrecedence` resolver

**Decisions:** D1, D4, D5, D6. **ACs:** AC1, AC5.

- [ ] **1.1** Add `FcShellOptions.DefaultDensity` property (nullable `DensityLevel?`, default `null`). XML doc cites UX spec §201-202 and the four-tier precedence role. No `[EnumDataType]` validation attribute needed — `DensityLevel?` binds native-correctly via `IConfiguration`. Parameter count 14 → 15.
- [ ] **1.2** Create `src/Hexalith.FrontComposer.Contracts/Rendering/DensitySurface.cs` — `public enum DensitySurface : byte { Default = 0, DataGrid = 1, DetailView = 2, CommandForm = 3, NavigationSidebar = 4, DevModeOverlay = 5 }` with XML comments citing UX spec §208-214.
- [ ] **1.3** Create `src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs` — `public static class DensityPrecedence { public static DensityLevel Resolve(DensityLevel? userPreference, DensityLevel? deploymentDefault, DensitySurface surface, ViewportTier tier) { ... } private static DensityLevel GetFactoryDefault(DensitySurface surface) { ... } }`. Body matches D1 precedence order. XML comment lists the four tiers + the per-component exit.
- [ ] **1.4** `dotnet build` clean.

**Exit:** Options property binds via `IConfiguration`; resolver is pure static.

---

## Task 2 — `FrontComposerDensityState` extension (UserPreference + EffectiveDensity)

**Decisions:** D2, D3. **ACs:** AC1, AC4.

- [ ] **2.1** Update `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityState.cs` — change from `(DensityLevel CurrentDensity)` to `(DensityLevel? UserPreference, DensityLevel EffectiveDensity)`. Any existing Story 3-1 reducer/test references to `CurrentDensity` update to `EffectiveDensity`.
- [ ] **2.2** Update `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityFeature.cs` — `GetInitialState() => new(UserPreference: null, EffectiveDensity: DensityLevel.Comfortable)`.
- [ ] **2.3** Create/update `src/Hexalith.FrontComposer.Shell/State/Density/DensityActions.cs` — add 4 new records:
  ```csharp
  public sealed record UserPreferenceChangedAction(string CorrelationId, DensityLevel NewPreference, DensityLevel NewEffective);
  public sealed record UserPreferenceClearedAction(string CorrelationId, DensityLevel NewEffective);
  public sealed record DensityHydratedAction(DensityLevel? UserPreference, DensityLevel NewEffective);
  public sealed record EffectiveDensityRecomputedAction(DensityLevel NewEffective);
  ```
  The existing `DensityChangedAction(string CorrelationId, DensityLevel NewDensity)` is KEPT (no deletion) — it remains the coarse "set density" entry point for a future Story 3-4 command-palette action. Its reducer (in Task 2.4) is retargeted to treat the payload as a `UserPreferenceChangedAction` equivalent — i.e., it resolves the effective density from the new preference + current context and assigns both fields.
- [ ] **2.4** Update `src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs` — 5 `[ReducerMethod]`s (one per action plus the retargeted `DensityChangedAction`). Each reducer assigns `EffectiveDensity` from the action's `NewEffective` payload — NO DI inside the reducer (D3). For `DensityChangedAction` (legacy 3-1 action), the reducer sets `state with { UserPreference = action.NewDensity, EffectiveDensity = action.NewDensity }` (the callsite dispatching this action is responsible for having resolved — since 3-3 does not ship a new caller for `DensityChangedAction`, this reducer is effectively dead code waiting for Story 3-4; it is retained so Story 1-3's action hierarchy stays intact and no callsite break occurs).
- [ ] **2.5** XML comment each action / reducer citing the binding decision.

**Exit:** Fluxor assembly scan discovers the feature + reducers. `dotnet build` clean.

---

## Task 3 — `DensityEffects` extension (viewport subscription + user-preference-only persistence)

**Decisions:** D7, D8, D18, D19. **ACs:** AC3, AC4.

- [ ] **3.1** Refactor `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs` to inject `IState<FrontComposerNavigationState> navigationState` + `IOptions<FcShellOptions> options` alongside the existing `IStorageService`, `IUserContextAccessor`, `ILogger<DensityEffects>`. Keep the `FeatureSegment = "density"` constant.
- [ ] **3.2** Add `[EffectMethod] public async Task HandleViewportTierChanged(Navigation.ViewportTierChangedAction action, IDispatcher dispatcher)` that computes `newEffective = DensityPrecedence.Resolve(state.UserPreference, options.Value.DefaultDensity, DensitySurface.Default, action.NewTier)` and dispatches `EffectiveDensityRecomputedAction(newEffective)` when the computed value differs from the current `state.EffectiveDensity`. No storage write on this path.
- [ ] **3.3** Replace the existing `HandleAppInitialized` body: load `DensityLevel?` from `StorageKeys.BuildKey(tenantId, userId, "density")`; compute `resolvedEffective = DensityPrecedence.Resolve(stored, options.Value.DefaultDensity, DensitySurface.Default, navigationState.Value.CurrentViewport)`; dispatch `DensityHydratedAction(stored, resolvedEffective)`. Error handling mirrors Story 3-2 D15 verbatim (OperationCanceledException → LogDebug; other → HFC2106 Information with `Reason=Corrupt`; null/empty → HFC2106 Information with `Reason=Empty`).
- [ ] **3.4** Add `[EffectMethod] HandleUserPreferenceChanged(UserPreferenceChangedAction, IDispatcher)` that writes `action.NewPreference` to storage via `PersistAsync(DensityLevel? value)`. Add `[EffectMethod] HandleUserPreferenceCleared(UserPreferenceClearedAction, IDispatcher)` that writes `null` to storage via the same helper. Add private `async Task PersistAsync(DensityLevel? value)` with `TryResolveScope` guard + OperationCanceledException handling + HFC2105 Information logging.
- [ ] **3.5** DELETE the legacy `HandleDensityChanged(DensityChangedAction, ...)` persist path — `DensityChangedAction` is retained as a reducer input only (Task 2.3 rationale); its effect-side persistence is absorbed by the new `HandleUserPreferenceChanged` path since Story 3-4 will dispatch `UserPreferenceChangedAction` when it lands.
- [ ] **3.6** XML comment each effect handler citing the binding decision + the diagnostic ID.

**Exit:** Fluxor assembly scan discovers the effects. `dotnet build` clean with zero warnings.

---

## Task 4 — `<body>` `--fc-density` attribute via JS interop

**Decisions:** D9, D10. **ACs:** AC6.

- [ ] **4.1** Create `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-density.js` — ES module exporting:
  ```javascript
  export function setDensity(level) {
    if (!level) return;
    document.body.dataset.fcDensity = String(level).toLowerCase();
  }
  ```
  Single write-only function; no subscribe/unsubscribe (unlike `fc-layout-breakpoints.js`).
- [ ] **4.2** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityApplier.razor` — headless component returning `@* empty *@`, inherits `Fluxor.Blazor.Web.Components.FluxorComponent`. `[Inject] IJSRuntime JS`, `[Inject] IStateSelection<FrontComposerDensityState, DensityLevel> EffectiveSelection`. In `OnInitialized()`, project `state => state.EffectiveDensity` into the selection. In `OnAfterRenderAsync(firstRender)`, if `firstRender`, import the module lazily + call `setDensity(EffectiveSelection.Value)`; in `OnParametersSetAsync` (or equivalent selection-subscribe hook), call `setDensity(EffectiveSelection.Value)` on every change. Fire-and-forget `Task` — no await in the render path.
- [ ] **4.3** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityApplier.razor.cs` — code-behind implementing `IAsyncDisposable` (release the JS module reference on dispose, mirroring `FcSystemThemeWatcher` exactly).
- [ ] **4.2b** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor` + `.razor.cs` (D20, post-review amendment) — headless component rendering a visually-hidden `<div role="status" aria-live="polite" aria-atomic="true" class="fc-sr-only">@AnnouncementText</div>` region. `[Inject] IStateSelection<FrontComposerDensityState, DensityLevel> EffectiveSelection`, `[Inject] IStringLocalizer<FcShellResources> Localizer`. Skip announcement on the first render (WCAG best practice — avoids "Density set to Comfortable" on every page load). `private string LocalizedDensityLabel(DensityLevel d) => d switch { DensityLevel.Compact => Localizer["DensityCompactLabel"].Value, DensityLevel.Comfortable => Localizer["DensityComfortableLabel"].Value, DensityLevel.Roomy => Localizer["DensityRoomyLabel"].Value, _ => d.ToString() };`. `AnnouncementText = string.Format(Localizer["DensityAnnouncementTemplate"].Value, LocalizedDensityLabel(EffectiveSelection.Value))`. No `IAsyncDisposable` needed (no JS interop).
- [ ] **4.4** Extend `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` — append three rules:
  ```css
  body[data-fc-density="compact"] { --fc-spacing-unit: 2px; }
  body[data-fc-density="comfortable"] { --fc-spacing-unit: 4px; }
  body[data-fc-density="roomy"] { --fc-spacing-unit: 6px; }
  [data-fc-density="compact"] { --fc-spacing-unit: 2px; }
  [data-fc-density="comfortable"] { --fc-spacing-unit: 4px; }
  [data-fc-density="roomy"] { --fc-spacing-unit: 6px; }
  .fc-sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; border: 0; }
  ```
  Document via an XML comment above each rule block: first three = body default (ADR-041 default source); next three = local override mirror (ADR-041 first-class local override — FcDensityPreviewPanel + future Story 4-1/4-5/6-5 consumers); `.fc-sr-only` is the visually-hidden utility class consumed by `FcDensityAnnouncer` (D20).
- [ ] **4.5** Extend `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — insert `<FcDensityApplier />` and `<FcDensityAnnouncer />` alongside `<FcSystemThemeWatcher />` and `<FcLayoutBreakpointWatcher />` inside the Content `FluentLayoutItem`.

**Exit:** Counter.Web boots, inspector shows `<body data-fc-density="comfortable">`, and `--fc-spacing-unit: 4px` resolves in the cascade.

---

## Task 5 — `FcSettingsButton.razor` (unhide Story 3-1 D26 placeholder)

**Decisions:** D11, D12. **ACs:** AC7.

- [ ] **5.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsButton.razor` — `<FluentButton Appearance="ButtonAppearance.Stealth" Title="@Localizer[\"SettingsTriggerAriaLabel\"].Value" OnClick="OpenDialogAsync"><FluentIcon Value="@(new Icons.Regular.Size20.Settings())" /></FluentButton>`. Code-behind injects `IDialogService` + `IStringLocalizer<FcShellResources>`. `OpenDialogAsync` calls `DialogService.ShowDialogAsync<FcSettingsDialog>(new DialogParameters { Modal = true, Width = "480px", Title = Localizer["SettingsDialogTitle"].Value })`.
- [ ] **5.2** Retire Story 3-1 D26's `@if (false)` compile-away guard for the Settings button — DELETE whatever placeholder Story 3-1 shipped (inspect `FrontComposerShell.razor` — if no placeholder exists because Story 3-1 never materialised one, this subtask is a no-op). Extend `FrontComposerShell.razor` to auto-populate `HeaderEnd` with `<FcSettingsButton />` when the parameter is `null`:
  ```razor
  @if (HeaderEnd is not null) { @HeaderEnd } else { <FcSettingsButton /> }
  ```
  Symmetric to Story 3-2 D8 `HeaderStart` → `FcHamburgerToggle`.
- [ ] **5.3** Confirm `FrontComposerShell.razor.cs` `HeaderEnd` parameter is unchanged (no rename, no retyping). Parameter count stays at 9 (no new parameter in 3-3).

**Exit:** Button visible in Counter.Web header top-right; clicking opens a mostly-empty `FluentDialog` (the dialog body lands in Task 6).

---

## Task 6 — `FcSettingsDialog.razor` (density radio + theme embed + live preview)

**Decisions:** D13, D14, D15, D17. **ACs:** AC2, AC5.

- [ ] **6.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor` — implements `IDialogContentComponent<EmptyDialogContent>` (or `IDialogContentComponent` per Fluent UI v5 documentation). Body layout:
  ```razor
  <div class="fc-settings-body">
    <h3 id="density-section">@Localizer["DensitySectionLabel"].Value</h3>
    <FluentRadioGroup @bind-Value="SelectedDensity" Name="fc-density-select"
                      aria-labelledby="density-section">
      <FluentRadio Value="@DensityLevel.Compact">@Localizer["DensityCompactLabel"].Value</FluentRadio>
      <FluentRadio Value="@DensityLevel.Comfortable">@Localizer["DensityComfortableLabel"].Value</FluentRadio>
      <FluentRadio Value="@DensityLevel.Roomy">@Localizer["DensityRoomyLabel"].Value</FluentRadio>
    </FluentRadioGroup>
    @if (IsForcedByViewport)
    {
      <FluentMessageBar Intent="MessageIntent.Info" AllowDismiss="false">
        @Localizer["DensityForcedByViewportNote"].Value
      </FluentMessageBar>
    }

    <h3 id="theme-section">@Localizer["ThemeSectionLabel"].Value</h3>
    <FcThemeToggle />

    <h3 id="preview-section">@Localizer["DensityPreviewHeading"].Value</h3>
    <FcDensityPreviewPanel Density="@SelectedDensity" ShowForcedViewportBadge="@(IsForcedByViewport && SelectedDensity != DensityLevel.Comfortable)" />
  </div>
  <FluentDialogFooter>
    <div class="fc-settings-footer-stack">
      <FluentButton Appearance="ButtonAppearance.Neutral" OnClick="RestoreDefaultsAsync">
        @Localizer["RestoreDefaultsLabel"].Value
      </FluentButton>
      <span class="fc-settings-footer-helper">@Localizer["RestoreDefaultsHelperText"].Value</span>
    </div>
  </FluentDialogFooter>
  ```
  Note: NO aria-live region inside the dialog — announcements live globally in `FcDensityAnnouncer` (D20) mounted on the shell so viewport-forced transitions announce even when the dialog is closed.
- [ ] **6.2** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs` — `[Inject] IState<FrontComposerDensityState> DensityState`, `[Inject] IState<FrontComposerNavigationState> NavState`, `[Inject] IOptions<FcShellOptions> Options`, `[Inject] IDispatcher Dispatcher`, `[Inject] IUlidFactory UlidFactory`. `SelectedDensity` property getter returns `DensityState.Value.UserPreference ?? DensityState.Value.EffectiveDensity`; setter dispatches `UserPreferenceChangedAction(UlidFactory.NewUlid(), value, DensityPrecedence.Resolve(value, Options.Value.DefaultDensity, DensitySurface.Default, NavState.Value.CurrentViewport))`. `IsForcedByViewport` computes `NavState.Value.CurrentViewport <= ViewportTier.Tablet && (DensityState.Value.UserPreference ?? Options.Value.DefaultDensity ?? DensityLevel.Comfortable) != DensityState.Value.EffectiveDensity`. `RestoreDefaultsAsync` (renamed from `ResetToDefaultsAsync` per D13 amendment) dispatches `UserPreferenceClearedAction(UlidFactory.NewUlid(), DensityPrecedence.Resolve(null, Options.Value.DefaultDensity, DensitySurface.Default, NavState.Value.CurrentViewport))` + `ThemeChangedAction(UlidFactory.NewUlid(), ThemeValue.System)`.
- [ ] **6.3** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.css` — scoped CSS: `.fc-settings-body { display: flex; flex-direction: column; gap: 16px; padding: 16px; } .fc-settings-body h3 { margin: 0 0 8px; font-size: var(--typeRampBase500FontSize); font-weight: var(--typeRampBase500FontWeight); } .fc-settings-footer-stack { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; } .fc-settings-footer-helper { font-size: var(--typeRampBase200FontSize); color: var(--colorNeutralForeground3); }`. Equal heading rhythm across Density / Theme / Preview sections (Freya review — theme section must not feel like afterthought). No overrides of `FluentDialog` / `FluentRadioGroup` internals (zero-override invariant).
- [ ] **6.4** Add `DensityForcedByViewportNote`, `RestoreDefaultsLabel`, `RestoreDefaultsHelperText`, `PreviewOnlyBadgeText`, `DensityAnnouncementTemplate` to the resx files (Task 9).

**Exit:** Settings dialog opens, all three sections render, radio selection updates the preview, Reset button dispatches both actions.

---

## Task 7 — `FcDensityPreviewPanel.razor` (DataGrid row + form field + nav item specimen)

**Decisions:** D14. **ACs:** AC2, AC5, AC6.

- [ ] **7.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor`:
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Layout
  @using Microsoft.FluentUI.AspNetCore.Components
  @inject IStringLocalizer<FcShellResources> Localizer

  <div class="fc-density-preview-wrapper">
    @if (ShowForcedViewportBadge)
    {
      <div class="fc-density-preview-badge" role="note">@Localizer["PreviewOnlyBadgeText"].Value</div>
    }
    <div class="fc-density-preview" data-fc-density="@Density.ToString().ToLowerInvariant()">
      <section>
        <FluentDataGrid Items="@_previewRows.AsQueryable()" GenerateHeader="GenerateHeaderOption.Default">
          <PropertyColumn Property="@(r => r.Order)" Title="Order" />
          <PropertyColumn Property="@(r => r.Customer)" Title="Customer" />
          <PropertyColumn Property="@(r => r.Status)" Title="Status" />
        </FluentDataGrid>
      </section>
      <section>
        <FluentTextField Label="Email" Placeholder="name@example.com" Value="sample@acme.com" ReadOnly="true" />
      </section>
      <section>
        <a class="fc-preview-navitem" href="#" tabindex="-1">Orders</a>
      </section>
    </div>
  </div>

  @code {
    [Parameter, EditorRequired] public DensityLevel Density { get; set; }
    [Parameter] public bool ShowForcedViewportBadge { get; set; }
    private readonly List<PreviewRow> _previewRows = new()
    {
      new PreviewRow("ORD-001", "ACME Inc.", "Pending"),
      new PreviewRow("ORD-002", "Contoso Ltd.", "Confirmed"),
    };
    private sealed record PreviewRow(string Order, string Customer, string Status);
  }
  ```
- [ ] **7.2** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor.css` — scoped CSS: `.fc-density-preview-wrapper { position: relative; } .fc-density-preview-badge { position: absolute; top: 4px; right: 4px; z-index: 1; padding: 2px 8px; background: var(--colorPaletteYellowBackground1); color: var(--colorPaletteYellowForeground1); border-radius: 4px; font-size: var(--typeRampBase200FontSize); font-weight: var(--fontWeightSemibold); } .fc-density-preview { display: flex; flex-direction: column; gap: calc(var(--fc-spacing-unit) * 2); padding: calc(var(--fc-spacing-unit) * 3); border: 1px solid var(--colorNeutralStroke1); border-radius: 4px; } .fc-preview-navitem { display: block; padding: calc(var(--fc-spacing-unit) * 2); color: var(--fc-color-accent); text-decoration: none; }`. All spacing consumes `var(--fc-spacing-unit)` (ADR-041). Badge styling uses Fluent UI design tokens (zero-override).
- [ ] **7.3** No persistence, no Fluxor state subscription — this is a pure-presentational parameter-driven component. The badge is driven entirely by the `ShowForcedViewportBadge` parameter — the panel doesn't know why it's showing (Freya review — parent composes the condition, panel renders it).

**Exit:** Preview panel renders at Compact / Comfortable / Roomy with visibly different spacing.

---

## Task 8 — Ctrl+, keyboard shortcut (inline binding; IShortcutService deferred to Story 3-4)

**Decisions:** D16. **ACs:** AC7.

- [ ] **8.1** Extend `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — add `@onkeydown="HandleGlobalKeyDown"` + `tabindex="0"` to the `.fc-shell-root` `<div>` (tabindex is required for the `<div>` to receive keyboard events).
- [ ] **8.2** Add `HandleGlobalKeyDown` method to `FrontComposerShell.razor.cs`:
  ```csharp
  private async Task HandleGlobalKeyDown(KeyboardEventArgs e)
  {
    if (e.Key == "," && e.CtrlKey && !e.ShiftKey && !e.AltKey && !e.MetaKey)
    {
      await DialogService.ShowDialogAsync<FcSettingsDialog>(new DialogParameters
      {
        Modal = true,
        Width = "480px",
        Title = Localizer["SettingsDialogTitle"].Value,
      });
    }
  }
  ```
  `[Inject] IDialogService DialogService` already required for Task 5.1 path; reuse. No change to parameter surface.
- [ ] **8.3** Document the 3-4 migration path in the `HandleGlobalKeyDown` XML comment: "Story 3-4 replaces this inline binding with `IShortcutService.Register(\"ctrl+,\", ...)`. The user-visible behaviour is unchanged by that migration — this method becomes a no-op when the service-based binding subsumes it."

**Exit:** Pressing Ctrl+, from any focus position in Counter.Web opens the settings dialog.

---

## Task 9 — Resource keys (EN + FR parity)

**Decisions:** D17. **ACs:** AC2, AC4, AC7.

- [ ] **9.1** Add 13 new keys to `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`:
  - `SettingsDialogTitle` = "Settings"
  - `SettingsDialogCloseAriaLabel` = "Close settings"
  - `DensitySectionLabel` = "Display density"
  - `DensityCompactLabel` = "Compact"
  - `DensityComfortableLabel` = "Comfortable"
  - `DensityRoomyLabel` = "Roomy"
  - `ThemeSectionLabel` = "Theme"
  - `DensityPreviewHeading` = "Preview"
  - `CtrlCommaShortcutHint` = "Ctrl+, to open settings"
  - `DensityForcedByViewportNote` = "Your device size is forcing Comfortable density. Your preference will re-apply at larger screen sizes."
  - `RestoreDefaultsLabel` = "Restore defaults"
  - `RestoreDefaultsHelperText` = "Clears density preference and sets theme to follow system."
  - `PreviewOnlyBadgeText` = "Preview only — Comfortable is active."
  - `DensityAnnouncementTemplate` = "Density set to {0}."
- [ ] **9.2** Add the same 13 keys to `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx` with French translations:
  - `SettingsDialogTitle` = "Paramètres"
  - `SettingsDialogCloseAriaLabel` = "Fermer les paramètres"
  - `DensitySectionLabel` = "Densité d'affichage"
  - `DensityCompactLabel` = "Compact"
  - `DensityComfortableLabel` = "Confortable"
  - `DensityRoomyLabel` = "Spacieux"
  - `ThemeSectionLabel` = "Thème"
  - `DensityPreviewHeading` = "Aperçu"
  - `CtrlCommaShortcutHint` = "Ctrl+, pour ouvrir les paramètres"
  - `DensityForcedByViewportNote` = "La taille de votre appareil impose la densité Confortable. Votre préférence s'appliquera de nouveau sur des écrans plus larges."
  - `RestoreDefaultsLabel` = "Rétablir les paramètres par défaut"
  - `RestoreDefaultsHelperText` = "Efface la préférence de densité et règle le thème sur le suivi système."
  - `PreviewOnlyBadgeText` = "Aperçu uniquement — Confortable est actif."
  - `DensityAnnouncementTemplate` = "Densité réglée sur {0}."

**Exit:** Parity test `CanonicalKeysHaveFrenchCounterparts` passes. Both resx files build without warnings.

---

## Task 10 — Tests

**Decisions:** All. **ACs:** All.

- [ ] **10.1** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs` — 6 `[Theory]` parameterised cases covering the precedence matrix (see AC1 test enumeration).
- [ ] **10.2** Extend `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsTests.cs` — add a test asserting `DefaultDensity` property exists, is nullable, accepts `null` and all three enum values.
- [ ] **10.3** Confirm `FrontComposerShellParameterSurfaceTests.verified.txt` is unchanged (parameter count stays at 9 — D12 auto-populate is a render-time behaviour, not a new parameter).
- [ ] **10.4** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityReducerTests.cs` — 4+ tests covering `UserPreferenceChangedAction` / `UserPreferenceClearedAction` / `DensityHydratedAction` / `EffectiveDensityRecomputedAction` reducers + the viewport-forcing scenarios (AC4 test enumeration).
- [ ] **10.4a** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityReducerPurityTest.cs` (Winston review — reducer-level lint enforcing ADR-039 purity invariant) — `[Fact]` that reads `src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs` as text and asserts the literal `DensityPrecedence.Resolve` does NOT appear anywhere in the file. If it does, the test fails with message: "ADR-039 purity violation — reducers must not invoke the resolver. Move the compute to the action producer and carry the pre-resolved value in the action payload." The test is paired with a comment in `DensityReducers.cs` citing this test so a future contributor who adds the call gets an immediate CI failure with context.
- [ ] **10.5** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs` — 5 tests mirroring `NavigationEffectsScopeTests`: persist on valid scope, skip + `HFC2105` on null tenant, skip on null user, skip on whitespace, hydrate does not re-persist.
- [ ] **10.5a** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPersistenceSnapshotTests.cs` — 2 `[Fact]`s: snapshot `JsonSerializer.Serialize((DensityLevel?)DensityLevel.Roomy)` and `JsonSerializer.Serialize<DensityLevel?>(null)`. Locks D18 schema via `.verified.txt`.
- [ ] **10.6** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityApplierTests.cs` — 3 bUnit tests: invokes `setDensity` on initial render with hydrated value, invokes on state change, disposes the module on dispose. Each test asserts `jsInterop.Invocations.Count == expected` (Murat review — invocation count catches subscription-leak regressions where a duplicate subscribe would fire `setDensity` twice per change).
- [ ] **10.6a** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/DensityNoPerComponentLogicLintTest.cs` — `[Fact]` that reads all `.razor.css` + `.cs` files under `src/Hexalith.FrontComposer.Shell/` (excluding the allow-listed approved files) and asserts none contain the literal string `--fc-density` (CSS custom property — must only appear in `FrontComposerShell.razor.css` + `fc-density.js`). `data-fc-density` is allow-listed in `FrontComposerShell.razor.css` (default + local-override rules), `fc-density.js`, `FcDensityApplier` (attribute writer), `FcDensityPreviewPanel` (canonical local override), and `FcDensityAnnouncer` (D20 — reads EffectiveDensity but doesn't write the attribute). **This is a temporary tripwire** — see Known Gap G15 for the Roslyn analyzer + PostCSS AST walker replacement (Story 9-x).
- [ ] **10.6b** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityAnnouncerTests.cs` (D20, 2 bUnit tests): (a) renders visually-hidden `<div role="status" aria-live="polite" aria-atomic="true" class="fc-sr-only">` — asserts both the attributes and the class; (b) on `EffectiveDensity` change from `Comfortable` → `Roomy`, the rendered text becomes `string.Format(DensityAnnouncementTemplate, DensityRoomyLabel)` — asserts the text content updated. First-render-skip is verified implicitly by not asserting any announcement on initial `OnInitialized`.
- [ ] **10.7** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsButtonTests.cs` — 2 bUnit tests: renders `FluentButton` with Settings icon + correct aria-label; click triggers `DialogService.ShowDialogAsync<FcSettingsDialog>(...)` (asserted via an `NSubstitute` mock `IDialogService`).
- [ ] **10.8** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs` — 5 bUnit tests: renders density radio group + theme section + preview + RestoreDefaults button + helper text (D13 amendment); radio selection dispatches `UserPreferenceChangedAction` with correct `NewEffective`; `RestoreDefaults` button dispatches `UserPreferenceClearedAction` + `ThemeChangedAction(System)`; `IsForcedByViewport` note renders at Tablet tier with a Compact user preference + `FcDensityPreviewPanel` receives `ShowForcedViewportBadge=true`; `IsForcedByViewport` note does NOT render at Desktop tier AND `ShowForcedViewportBadge=false` (no-op case).
- [ ] **10.9** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs` — 3 bUnit tests: renders specimen at each density (`[Theory]` over Compact / Comfortable / Roomy), `data-fc-density` attribute matches the parameter, no Fluxor state mutation.
- [ ] **10.9b** Extend `FcDensityPreviewPanelTests.cs` with 2 bUnit tests for the D14 badge amendment: (a) `ShowForcedViewportBadge=true` renders `.fc-density-preview-badge` with `PreviewOnlyBadgeText` content; (b) `ShowForcedViewportBadge=false` (default) does NOT render the badge element (asserted via `panel.FindAll(".fc-density-preview-badge").Count == 0`).
- [ ] **10.10** Extend `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` with 3 new tests: `AutoRendersSettingsButtonWhenHeaderEndIsNull`, `AdopterSuppliedHeaderEndWins`, `CtrlCommaOpensSettingsDialog` (simulated `KeyboardEventArgs` via bUnit `Trigger.KeyDownAsync`).
- [ ] **10.10a** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` (Winston review — pins the Story 3-4 `IShortcutService` migration contract) — `[Fact]` that reads `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` as text and asserts `@onkeydown="HandleGlobalKeyDown"` appears **exactly once** and only on the `.fc-shell-root` `<div>` declaration line. Fails with message: "Story 3-3 D16 invariant: exactly one inline `@onkeydown` binding on the shell root. Story 3-4 replaces this with `IShortcutService.Register` — do not add additional bindings in 3-3." This test DELIBERATELY becomes obsolete when Story 3-4 lands (Story 3-4 deletes this test as part of the migration; the `IShortcutService` infrastructure provides its own registration + conflict-detection invariant).
- [ ] **10.11** Extend `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` — 6 additional key-lookup test methods covering the 13 new keys (batched — Density group {Compact/Comfortable/Roomy/Section}, Theme group {Theme/Announcement template}, Settings-dialog group {Title/CloseAriaLabel/Shortcut}, Preview group {Heading/PreviewOnlyBadge}, Viewport-forced note, Restore group {Label/HelperText}).
- [ ] **10.12** Extend `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/SidebarResponsiveE2ETests.cs` from Story 3-2 with a `DensityTransitionOnResize` Playwright test — HARDENED per Murat review 2026-04-19 (flakiness risk: MEDIUM-HIGH if naïvely implemented): (a) the test harness enables `prefers-reduced-motion: reduce` via `context.emulateMedia({ reducedMotion: 'reduce' })` to eliminate CSS-transition races; (b) assertions poll with `expect.poll(() => page.evaluate(() => document.body.dataset.fcDensity), { timeout: 5000, intervals: [50, 100, 200] }).toBe('comfortable')` rather than single-shot snapshot; (c) assertions read `document.body.dataset.fcDensity` (DOM attribute) NOT `window.getComputedStyle(document.body).getPropertyValue('--fc-spacing-unit')` (computed style — bypasses the cascade-timing race that motivated the hardening). Flow: boot Counter.Web, set density to `Compact` via the settings dialog, resize to 800 px, poll-assert `fcDensity === "comfortable"`; resize to 1920 px, poll-assert `fcDensity === "compact"` (user preference restored). Test file XML comments link back to Murat's review for context.
- [ ] **10.13** PR-review gate — confirm test count 31-34, bottom-quartile Occam / Matrix scoring applied (L07).

**Exit:** `dotnet test` green. Test count delta ≈ +31 (check against `test_baseline_pre_3_3` captured in Task 0.1).

---

## Task 11 — Zero-warning gate + regression baseline

**Decisions:** All. **ACs:** All.

- [ ] **11.1** `dotnet build --warnaserror` passes with zero warnings.
- [ ] **11.2** `dotnet test` green end-to-end (including the Story 3-1 + 3-2 regression baselines).
- [ ] **11.3** Manual verification in Counter.Web: (a) Settings button visible top-right; (b) click opens dialog with all three sections; (c) choosing Roomy visibly increases preview spacing and persists across refresh; (d) resize to 800 px forces the DataGrid (visible on CounterPage) to Comfortable regardless of chosen preference; (e) resize back to 1920 px restores the chosen preference; (f) Ctrl+, opens the dialog from any focus position.
- [ ] **11.4** Aspire MCP automated verification per `memory/feedback_no_manual_validation.md` — use the Aspire harness to boot Counter.Web + Claude-in-Chrome to run the six manual checks above as scripted interactions.
- [ ] **11.5** Record the test-count delta, any manual observations, and the next-story handoff in `dev-agent-record.md`.

**Exit:** Story status transitions from `in-progress` → `review` via the dev-story workflow.

---
