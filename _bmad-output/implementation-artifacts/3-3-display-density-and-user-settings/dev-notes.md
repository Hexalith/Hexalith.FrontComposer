# Dev Notes

## Executive Summary (Read First — Feynman-level, ~30 sec)

When the app starts, the density feature chooses a density for the user. It asks four questions in order: **(1) Is the browser tiny (Tablet or Phone)?** If yes, force Comfortable — touch-target law. **(2) Did the user explicitly pick one in the Settings dialog?** If yes, use that. **(3) Has the adopter configured a deployment-wide default in `appsettings.json`?** If yes, use that. **(4) Fallback to the UX spec's factory recommendation for this surface type** (DataGrid = Compact, everything else = Comfortable). The chosen value is called `EffectiveDensity`. It is written to `<body data-fc-density="...">` by a headless component called `FcDensityApplier`. All scoped CSS in the framework consumes a variable `--fc-spacing-unit` that the body attribute controls — so one attribute change cascades to every visible component at once. No component subscribes to density state individually. The user's explicit choice is separately preserved in `UserPreference` (null = "I haven't chosen"); when the browser resizes back to Desktop, the user's Compact choice re-applies automatically without them re-clicking.

The Settings dialog is a `FluentDialog` opened by either a button in the header (top-right, replacing the Story 3-1 D26 hidden placeholder) or `Ctrl+,`. It has three sections: density radio, the existing `FcThemeToggle` embedded verbatim, and a live preview specimen (a fake DataGrid row + form field + nav item) that shows how the app WILL look at each density. The preview wraps its specimen in its own `data-fc-density` scope so it can show a density different from the currently-applied one — useful when the browser is forcing Comfortable on Tablet but the user is hovering over the "Roomy" radio to see what Roomy would look like. There is no "Apply" button; changes are live. "Reset to defaults" clears the user preference and resets the theme to System.

For the precision (why the four-tier rule sits inside a pure function, not scattered across components) see **ADR-039 / 040 / 041** + **D1 / D3 / D9 / D10**. For the persistence shape (exactly one `DensityLevel?` serialised under `{tenantId}:{userId}:density`; hydrate is read-only) see **D8 / D18** + Story 3-2's ADR-038 (mirror). For what is NOT in 3-3 (IShortcutService full surface, command-palette integration, per-component overrides, cross-device sync) see **Known Gaps G1-G6**.

## Service Binding Reference

All additions in 3-3 ride inside the existing `AddHexalithFrontComposer` registration — no new DI extension methods.

- `IFrontComposerRegistry` — Singleton (Story 1-x). Not consumed by 3-3 directly; the settings dialog + density applier do not inspect manifests.
- `IStorageService` — Scoped (Story 3-1 ADR-030). `DensityEffects` keeps its existing constructor injection.
- `IUserContextAccessor` — Scoped (Story 2-2 D31). Reused via the `TryResolveScope` guard in `DensityEffects` (unchanged from Story 3-1).
- `IStringLocalizer<FcShellResources>` — Scoped via `AddLocalization()` (adopter-owned per Story 3-1 D24). Resolves the 11 new keys in Task 9.
- `IDispatcher`, `IState<FrontComposerDensityState>`, `IState<FrontComposerNavigationState>`, `IStateSelection<>` — standard Fluxor scoped-per-circuit behaviour. `DensityEffects` constructor-injects `IState<FrontComposerNavigationState>` for the viewport-tier read (Task 3.2).
- `IUlidFactory` — Singleton (Story 2-3 D2/D3). Used by `FcSettingsDialog` for correlation IDs on `UserPreferenceChangedAction` / `UserPreferenceClearedAction`.
- `IJSRuntime` — Scoped. `FcDensityApplier` imports the `fc-density.js` module lazily on first render.
- `IOptions<FcShellOptions>` — Singleton (bound from `IConfiguration`). 3-3 adds `DefaultDensity` property; `DensityEffects` constructor-injects for the deployment-default tier in the resolver.
- `IDialogService` — Scoped (Fluent UI v5 built-in). `FcSettingsButton` + `FrontComposerShell.HandleGlobalKeyDown` call `ShowDialogAsync<FcSettingsDialog>(...)`.
- `Fluxor` assembly scan — the existing `ScanAssemblies(typeof(FrontComposerThemeState).Assembly)` in `AddHexalithFrontComposer` discovers the new actions, reducers, and effect handlers automatically.

## Density Precedence State Machine

```
User action           │  Action dispatched              │  EffectiveDensity computed by
─────────────────────────────────────────────────────────────────────────────────────────
App startup (hydrate) │  DensityHydratedAction          │  DensityEffects.HandleAppInitialized
                      │    (stored UserPref, resolved)  │    reads options + nav.CurrentViewport,
                      │                                 │    resolves, dispatches
Selects radio in      │  UserPreferenceChangedAction    │  FcSettingsDialog code-behind
  settings dialog     │    (corrId, newPref, newEff)    │    reads options + nav.CurrentViewport,
                      │                                 │    calls DensityPrecedence.Resolve
Reset to defaults     │  UserPreferenceClearedAction    │  FcSettingsDialog code-behind
                      │    (corrId, newEff)             │    resolves with null userPreference
Viewport crosses      │  EffectiveDensityRecomputed     │  DensityEffects.HandleViewportTierChanged
  boundary            │    (newEff)                     │    on Navigation.ViewportTierChangedAction,
                      │                                 │    resolves with new tier
─────────────────────────────────────────────────────────────────────────────────────────

Reducer (pure, no DI): state with {
    UserPreference = action.UserPreference ?? state.UserPreference (when action has that field),
    EffectiveDensity = action.NewEffective (every action carries this)
}

FcDensityApplier subscribes via IStateSelection<FrontComposerDensityState, DensityLevel>
  (projects EffectiveDensity). On every Selected change → fc-density.js#setDensity(newLevel).
```

## DensityState Shape & Persistence Schema

```
In-memory state:
  FrontComposerDensityState(
    UserPreference: DensityLevel? (null means "I haven't chosen"),
    EffectiveDensity: DensityLevel (never null — always a resolved value)
  )

Storage key: {tenantId}:{userId}:density
Storage value (JSON via System.Text.Json defaults):
  - User selected Roomy        → "Roomy"  (enum written as string via JsonStringEnumConverter IF configured)
                               → 2         (enum written as int if default config applies)
  - User reset to defaults     → null
  - User never set a value     → (key absent from storage → feature defaults apply)

Schema invariants (locked by DensityPersistenceSnapshotTests.cs .verified.txt):
- Scalar DensityLevel? — not wrapped in a record
- Storage returns the scalar directly; hydrate effect passes it into DensityHydratedAction
- No "EffectiveDensity" or "ComputedDensity" field — EffectiveDensity is never persisted (ADR-041 "CSS cascade" + ADR-040 "tier forcing" both require EffectiveDensity to be recomputed on every boot from UserPreference + current context)
```

## Settings Dialog Composition Diagram

```
┌─ FluentDialog (Width=480px, Modal=true, Title=@Localizer["SettingsDialogTitle"])
│
│  ┌─ [×] close icon (FluentDialog built-in, Escape also dismisses)
│
│  ┌─ Body: <div class="fc-settings-body">
│  │
│  │  ├─ <h3 id="density-section">Display density</h3>
│  │  ├─ <FluentRadioGroup @bind-Value="SelectedDensity" aria-labelledby="density-section">
│  │  │     <FluentRadio Value=@DensityLevel.Compact>Compact</FluentRadio>
│  │  │     <FluentRadio Value=@DensityLevel.Comfortable>Comfortable</FluentRadio>
│  │  │     <FluentRadio Value=@DensityLevel.Roomy>Roomy</FluentRadio>
│  │  │  </FluentRadioGroup>
│  │  ├─ @if (IsForcedByViewport) { <FluentMessageBar Intent="Info">Your device size is forcing Comfortable density.</FluentMessageBar> }
│  │  │
│  │  ├─ <h3 id="theme-section">Theme</h3>
│  │  ├─ <FcThemeToggle />   ← Story 3-1 component embedded verbatim (D15)
│  │  │
│  │  ├─ <h3 id="preview-section">Preview</h3>
│  │  └─ <FcDensityPreviewPanel Density=@SelectedDensity
│  │                             ShowForcedViewportBadge=@(IsForcedByViewport && SelectedDensity != DensityLevel.Comfortable) />
│  │                                 ↑ D14 amendment — "Preview only — Comfortable is active" badge (Freya review)
│  │
│  └─ </div>
│
│  ┌─ Footer: <FluentDialogFooter><div class="fc-settings-footer-stack">
│  │     <FluentButton Appearance="Neutral" OnClick="RestoreDefaultsAsync">Restore defaults</FluentButton>
│  │     <span class="fc-settings-footer-helper">Clears density preference and sets theme to follow system.</span>
│  │                                ↑ D13 amendment — renamed Reset→Restore + helper text (Freya review)
│  └─ </div></FluentDialogFooter>
│
└─
```

## Live Preview Specimen

```
<div class="fc-density-preview" data-fc-density="@Density">   ← Local CSS cascade scope (D14)

  <section>
    <FluentDataGrid>                        ← Specimen row 1
      "ORD-001" | "ACME Inc."   | "Pending"
      "ORD-002" | "Contoso Ltd." | "Confirmed"
    </FluentDataGrid>
  </section>

  <section>
    <FluentTextField Label="Email"          ← Specimen row 2
                     Value="sample@acme.com"
                     ReadOnly="true" />
  </section>

  <section>
    <a class="fc-preview-navitem">Orders</a>  ← Specimen row 3 (standalone nav-item-like anchor,
                                                since FluentNavItem requires a FluentNav parent)
  </section>

</div>

Scoped CSS consumes var(--fc-spacing-unit) which resolves to:
  data-fc-density="compact"    → 2px
  data-fc-density="comfortable" → 4px
  data-fc-density="roomy"      → 6px
```

## CSS `--fc-density` Integration

```
body[data-fc-density="compact"]     { --fc-spacing-unit: 2px; }
body[data-fc-density="comfortable"] { --fc-spacing-unit: 4px; }
body[data-fc-density="roomy"]       { --fc-spacing-unit: 6px; }

Component scoped CSS (3-3 owns these files; downstream generators mirror):
  .fc-settings-body { gap: 16px; padding: 16px; }              ← Dialog itself NOT density-driven (consistent regardless)
  .fc-density-preview { padding: calc(var(--fc-spacing-unit) * 3); gap: calc(var(--fc-spacing-unit) * 2); }
  .fc-preview-navitem { padding: calc(var(--fc-spacing-unit) * 2); }

Future downstream files (Story 4-1 DataGrid renderer, Story 3-2 FrontComposerNavigation):
  All spacing values MUST use calc(var(--fc-spacing-unit) * N) multipliers — not hardcoded px.

DensityNoPerComponentLogicLintTest (Task 10.6a) enforces this convention in CI.
```

## Fluent UI v5 Component Reference

Per MCP `get_component_details("FluentDialog")`:

- `FluentDialog` container — supports `Modal` (bool), `Width` (string CSS), `PreventDismissOnOverlayClick` (bool), `Title` (string — renders in header).
- Built-in `×` close icon renders automatically; `Escape` key dismisses by default.
- Initial focus traps on the first focusable element inside the dialog body.
- `IDialogService.ShowDialogAsync<TContent>(DialogParameters)` — generic method that instantiates the component of type `TContent` as the dialog body. `TContent` must implement `IDialogContentComponent` (marker interface). Component receives the `DialogParameters` via `[CascadingParameter] FluentDialog Dialog { get; set; }`.
- `<FluentDialogFooter>` — render fragment for footer buttons. Renders inside the dialog chrome; `FluentDialog` handles alignment.

Per MCP `get_component_details("FluentRadioGroup")`:

- `FluentRadioGroup<TValue>` — generic component accepting `Value` (TValue), `ValueChanged` (EventCallback<TValue>), `Name` (string for the HTML form name attribute), and child `FluentRadio<TValue>` elements.
- `FluentRadio<TValue>` — accepts `Value` (TValue) and child content for the label text.
- Radio group handles keyboard navigation internally (arrow keys move between radios; Space toggles).
- Accessibility: use `aria-labelledby` pointing to a section heading id (pattern used in Task 6.1).

Per MCP `get_component_details("FluentMessageBar")`:

- `FluentMessageBar` — supports `Intent` (`MessageIntent.Info`/`Warning`/`Error`), `AllowDismiss` (bool, default true). Used in Task 6.1 for the viewport-forcing note.

## Files Touched Summary

**Created (13 files):**
- `src/Hexalith.FrontComposer.Contracts/Rendering/DensitySurface.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs`
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-density.js`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityApplier.razor` + `.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsButton.razor` + `.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor` + `.razor.cs` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor` + `.razor.css`

**Modified (8 files):**
- `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` — add `DefaultDensity` property (count 14 → 15)
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityState.cs` — shape change (`CurrentDensity` → `UserPreference` + `EffectiveDensity`)
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityFeature.cs` — initial state
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityActions.cs` — 4 new actions + existing `DensityChangedAction` retained
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs` — rewritten to consume pre-resolved effective density
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs` — adds viewport handler + cross-feature state read + user-pref-only persistence
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — inserts `<FcDensityApplier />` and auto-populates `HeaderEnd` with `<FcSettingsButton />`; adds `@onkeydown` + `tabindex="0"` on root
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — adds `HandleGlobalKeyDown` method + `[Inject] IDialogService`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` — adds three body[data-fc-density] rules
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` — 11 new keys

**Created tests (9 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityReducerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPersistenceSnapshotTests.cs` (+ `.verified.txt`)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityApplierTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/DensityNoPerComponentLogicLintTest.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsButtonTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcDensityPreviewPanelTests.cs`

**Modified tests (3 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsTests.cs` (or equivalent — add `DefaultDensity` coverage)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` (+ 3 tests for HeaderEnd auto-populate + Ctrl+, dialog open)
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` (+ 6 lookups for 11 new keys)
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/SidebarResponsiveE2ETests.cs` (+ density transition Playwright assertion)

No changes to `Contracts/Rendering/DensityLevel.cs` (enum stays as Story 1-3 shipped it).
No changes to `SourceTools/`, `EventStore/`, or `Tenants/` submodules.
No new `AnalyzerReleases.*.md` entries.

## Testing Standards

- xUnit v3, Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited.
- `TestContext.Current.CancellationToken` on async tests.
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI for Verify.
- `BunitJSInterop` strict mode: explicit `.SetupVoid("setDensity", ...)` for `fc-density.js` interop calls in `FcDensityApplierTests`.
- `NSubstitute` for `IDialogService` mocking in `FcSettingsButtonTests`.
- **Test count budget (L07):** ~31 new tests / 19 decisions ≈ 1.6 per decision — comfortable middle of Murat's range. PR-review gate at Task 10.13 confirms or trims.

## Build & CI

- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story.
- No new `AnalyzerReleases.*.md` entries — HFC2105 + HFC2106 are runtime-only and reused verbatim from Story 3-1 / 3-2.
- Scoped CSS emits `{AssemblyName}.styles.css` automatically — `FcSettingsDialog.razor.css`, `FcDensityPreviewPanel.razor.css`, and the updated `FrontComposerShell.razor.css` all ride the existing scoped-CSS bundle (Story 3-1 ADR-034 filename contract unchanged).
- No new CI jobs — everything rides `dotnet build` + `dotnet test`.
- Playwright E2E in Task 10.12 runs via the Story 3-1 / 3-2 harness (Aspire MCP if available per `memory/feedback_no_manual_validation.md`).

## Previous Story Intelligence

**From Story 3-2 (immediate predecessor):**
- **Sharded story format** — index.md + per-section markdown files. 3-3 follows the same structure.
- **L06 budget discipline** — 3-2 landed at 23 decisions (infrastructure, ≤ 40). 3-3 lands at 19 decisions (feature, ≤ 25) — comfortably below the cap; density/settings is a more focused surface than 3-2's navigation.
- **L07 test-to-decision ratio** — 3-2 at 1.5 post-elicitation. 3-3 at ~1.6 — consistent.
- **`ViewportTier` enum + `IState<FrontComposerNavigationState>.CurrentViewport`** — 3-3 consumes via `DensityEffects.HandleViewportTierChanged` cross-feature effect + `FcSettingsDialog.IsForcedByViewport` read. Story 3-2 G2 (forced-comfortable at Tablet) is CLOSED by Story 3-3 ADR-040.
- **ADR-029 `IUserContextAccessor` fail-closed** — reused verbatim for `DensityEffects`.
- **ADR-030 `IStorageService` Scoped lifetime** — `DensityEffects` picks up the Scoped storage via constructor injection.
- **ADR-037 `ViewportTier` not persisted** — 3-3 honors by never serialising `EffectiveDensity`; only `UserPreference` persists.
- **ADR-038 Hydrate is read-only** — 3-3 mirrors: `DensityHydratedAction` does NOT trigger re-persistence.
- **D10 `HeaderCenter` RenderFragment parameter** — 3-3 does NOT extend `FrontComposerShell` parameters; parameter count stays at 9.
- **D18 auto-populate pattern** — 3-3 applies the same pattern to `HeaderEnd` (symmetric to `HeaderStart` + `FcHamburgerToggle`).

**From Story 3-1:**
- **D26 hidden placeholder for Settings** — 3-3 retires the guard and ships the real button via D12.
- **`SettingsTriggerAriaLabel` resource key** (seeded in 3-1 resx) — reused verbatim; no rename.
- **`FcShellOptions` growth trigger (D14 / G1)** — 3-3 adds the 15th property; G1 options-class split stays deferred to Story 9-2. 14 → 15 does not cross a new threshold.
- **Single write path discipline** — `DensityReducers` stays pure static; action producers compute `NewEffective` before dispatching.
- **FcThemeToggle component** — embedded verbatim in FcSettingsDialog (D15); no duplication, no adapter wrapper.

**From Story 2-3:**
- **Single-writer invariant (D19)** — 3-3's reducer set is the single write path into `FrontComposerDensityState`. No bypass.
- **ULID correlation pattern** — reused via `IUlidFactory.NewUlid()` for every dispatched action.

**From Story 1-3:**
- **Per-concern Fluxor features** — 3-3 extends `Density` (the second feature after `Theme`). Follows the same `Shell/State/{Concern}/` layout.
- **`DensityLevel` enum** — unchanged from Story 1-3.

## Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Cross-story contract table in Critical Decisions names producer and consumer for every seam (DensityPrecedence.Resolve, DensitySurface enum, DensityState shape, FcShellOptions.DefaultDensity, FrontComposerShell.HeaderEnd auto-populate, Ctrl+, binding → Story 3-4, --fc-density CSS variable, IDialogService<FcSettingsDialog>, DensitySurface routing in custom components).
- **L02 Fluxor feature producer+consumer scope** — `FrontComposerDensityFeature` PRODUCER stories: 1-3 (feature skeleton), 3-1 (fail-closed wiring), 3-3 (this story — new actions, effects, viewport cross-feature subscription). CONSUMER stories: 3-3's own `FcDensityApplier`, 3-4 (potentially via `DensityChangedAction` from command palette), 4-1 (DataGrid surface via resolver), 4-5 (DetailView surface), 6-1 (annotation-level overrides), 6-5 (DevModeOverlay surface). Shipping ALL action producers + effects in 3-3 (no deferred effects) avoids Story 2-2's "half a state machine" risk.
- **L03 Tenant/user isolation fail-closed** — D8 inherits Story 3-1 ADR-029 verbatim via `TryResolveScope`. Memory feedback `feedback_tenant_isolation_fail_closed.md` honored.
- **L04 Generated name collision detection** — Not applicable; 3-3 does not extend the source generator.
- **L05 Hand-written service + emitted per-type wiring** — Not applicable; 3-3 infrastructure is hand-written only. `DensityPrecedence.Resolve` is a pure static function consumed by hand-written call sites.
- **L06 Defense-in-depth budget** — 19 decisions, well below the 25 feature cap. Room for a review round (party mode + advanced elicitation) to add up to 6 more without hitting the cap.
- **L07 Test count inflation** — 31 tests / 19 decisions ≈ 1.6 — comfortable middle of Murat's range. Task 10.13 PR-review gate decides whether to add or trim.
- **L08 Party review vs. elicitation** — 3-3 has NOT yet been reviewed via party mode or advanced elicitation. Recommended flow before `dev-story` execution: `/bmad-party-mode` with Winston / Sally / Murat / Amelia → apply findings → `/bmad-advanced-elicitation` (Pre-mortem / Red Team / Chaos / Hindsight) → `dev-story`. Key areas for review: (a) ADR-040 viewport forcing — Red Team: does preserving `UserPreference` across viewport changes surprise users who collapsed on a tablet and later wonder why their desktop shows their old choice? (b) D15 FcThemeToggle embed — Sally: does the `FluentMenuButton`-style toggle look out of place next to a `FluentRadioGroup`? Should theme be a radio group too for visual consistency? (c) D11 direct `IDialogService` call (no Fluxor action for dialog open) — Winston: is "not-Fluxor-tracked" the right call, or does it hurt observability?
- **L09 ADR rejected-alternatives discipline** — ADR-039 cites 4 rejected, ADR-040 cites 4, ADR-041 cites 5. All ≥ 2 satisfied.
- **L10 Deferrals name a story** — All 14 Known Gaps cite specific owning stories (3-4, 6-1, 4-1, 4-5, 6-5, 9-2, 10-2, v1.x, v2).
- **L11 Dev Agent Cheat Sheet** — Present. Feature story with 13 new source files + 9 new test files warrants a fast-path entry. 3-3's cheat sheet is sized similar to 3-2's.

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-composition-shell-navigation-experience.md#Story 3.3 — AC source of truth, §111-146]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR16 Display density selection]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR27 Three-level density precedence + Ctrl+, settings UI]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR29 Touch target guarantees at tablet/phone]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR30 Roomy as accessibility feature + 14 commitments]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Density Strategy, §185-224]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Settings UI Location, §222]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md#Touch Target Guarantees, §42-63]
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern Fluxor Features, §527-536]
- [Source: _bmad-output/planning-artifacts/architecture.md#Theme/density cascade Fluxor reducer chain, §405]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/critical-decisions-read-first-do-not-revisit.md#D26 Header placeholder hidden via compile-away guard]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-029 IUserContextAccessor fail-closed]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-030 IStorageService Scoped]
- [Source: _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/architecture-decision-records.md#ADR-037 ViewportTier never persisted]
- [Source: _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/architecture-decision-records.md#ADR-038 Hydrate is read-only]
- [Source: _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/critical-decisions-read-first-do-not-revisit.md#D8 HeaderStart auto-populate + D15 error handling]
- [Source: _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/known-gaps-explicit-not-bugs.md#G2 Density forcing deferred to Story 3-3]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01-L11 — all lessons applied]
- [Source: memory/feedback_no_manual_validation.md — Aspire MCP + Playwright over manual validation for Task 11.4]
- [Source: memory/feedback_cross_story_contracts.md — cross-story contract table per ADR-016 canonical example]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D8 inherits Story 3-1 ADR-029]
- [Source: memory/feedback_defense_budget.md — 19 decisions, under ≤ 25 feature cap]
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/DensityLevel.cs — consumed read-only]
- [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs — extended in Task 1.1]
- [Source: src/Hexalith.FrontComposer.Shell/State/Navigation/ViewportTier.cs — consumed read-only]
- [Source: src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs — CurrentViewport consumed via IState<>]
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs — scope-guard pattern mirrored by DensityEffects]
- [Source: src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs — error handling pattern mirrored]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSystemThemeWatcher.razor.cs — FcDensityApplier mirrors structure]
- [Source: src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-prefers-color-scheme.js — fc-density.js is the write-only analogue]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor — modified in Task 5.2 + 8.1 + Task 4.5]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcThemeToggle.razor — embedded in FcSettingsDialog verbatim]
- [MCP: `get_component_details("FluentDialog")` — dialog lifecycle verified]
- [MCP: `get_component_details("FluentRadioGroup")` — radio group binding semantics verified]
- [MCP: `get_component_details("FluentMessageBar")` — viewport-forcing note component verified]

## Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §919-982, §943):
  - `Shell/State/Density/` subfolder already exists from Story 1-3 + 3-1; 3-3 extends it with `DensityPrecedence.cs` and refactors existing files.
  - `Contracts/Rendering/DensitySurface.cs` lands alongside the existing `DensityLevel.cs` — matches the architecture §407 mapping.
  - `Shell/Components/Layout/` subfolder — 5 new files (`FcDensityApplier`, `FcSettingsButton`, `FcSettingsDialog`, `FcDensityPreviewPanel`, plus their `.cs` / `.css` siblings) land in the existing folder from Story 3-1 / 3-2.
  - `wwwroot/js/fc-density.js` — matches the existing `wwwroot/js/` convention (`fc-beforeunload.js`, `fc-prefers-color-scheme.js`, `fc-layout-breakpoints.js`).
  - No Contracts → Shell reverse references.
- **Fluent UI `Fc` prefix convention** honored — `FcDensityApplier`, `FcSettingsButton`, `FcSettingsDialog`, `FcDensityPreviewPanel`.
- **`FcShellOptions` extension** — Story 3-1 G1 deferral to Story 9-2 stays current; 15 properties is append-only.
- **`Contracts/Rendering/DensitySurface.cs`** — lives in `Contracts` (not `Shell`) because downstream stories (Epic 4, Epic 6) consume the enum from outside the `Shell` assembly.

---
