# Acceptance Criteria

> Seven ACs distilled from Epic 3 §111-146 + UX spec §185-224 + UX-DR27 / UX-DR29 / UX-DR30. Each cites the binding decisions, the tasks that deliver it, and the tests that verify it.

---

## AC1: Four-tier density precedence resolves `EffectiveDensity` from user preference → deployment default → factory hybrid → per-component default

**Given** the density precedence function `DensityPrecedence.Resolve(DensityLevel? userPreference, DensityLevel? deploymentDefault, DensitySurface surface, ViewportTier tier)`
**When** all inputs are supplied
**Then** the tier-force override applies FIRST: `tier <= ViewportTier.Tablet` → `Comfortable`
**And** otherwise `userPreference` (when non-null) applies SECOND
**And** otherwise `deploymentDefault` (when non-null) applies THIRD
**And** otherwise the factory hybrid for the surface applies FOURTH (Default/DetailView/CommandForm/NavigationSidebar → Comfortable; DataGrid/DevModeOverlay → Compact)
**And** per-component tier 5 is documented in `DensityPrecedence` XML as "outside the resolver" — custom components hardcoding their density do not consult the resolver

**Given** the reducer path on `UserPreferenceChangedAction(newPref)`
**When** the reducer runs
**Then** the effect that dispatched the action has already computed `newEffective = DensityPrecedence.Resolve(newPref, options.DefaultDensity, DensitySurface.Default, currentTier)`
**And** the reducer assigns `state with { UserPreference = action.NewPreference, EffectiveDensity = action.NewEffective }` (the action carries BOTH fields)
**And** no DI is performed inside the reducer

**References:** D1, D3, D5, D6; ADR-039. **Tasks:** 1.2, 2.2. **Tests:** `DensityPrecedenceTests.Resolve_AllCombinations` (6 theory cases), `DensityReducerTests.UserPreferenceChanged_AssignsBothFields` (Task 10.1, 10.4).

---

## AC2: Settings FluentDialog renders density radio options, theme selector, live preview, and accessible announcements; changes take effect immediately

**Given** the user clicks the Settings button in `FrontComposerShell.HeaderEnd`
**When** `IDialogService.ShowDialogAsync<FcSettingsDialog>(...)` resolves
**Then** a modal `FluentDialog` opens at 480 px width
**And** the dialog body contains: (a) a `<h3>@Localizer["DensitySectionLabel"]</h3>` followed by a `FluentRadioGroup<DensityLevel>` with three options labeled `DensityCompactLabel` / `DensityComfortableLabel` / `DensityRoomyLabel`; (b) a `<h3>@Localizer["ThemeSectionLabel"]</h3>` followed by the existing `<FcThemeToggle />` component embedded verbatim; (c) a `<h3>@Localizer["DensityPreviewHeading"]</h3>` followed by `<FcDensityPreviewPanel Density="@SelectedDensity" ShowForcedViewportBadge="@(IsForcedByViewport && SelectedDensity != DensityLevel.Comfortable)" />`
**And** the dialog footer contains a `FluentButton` labeled `RestoreDefaultsLabel` ("Restore defaults" / "Rétablir les paramètres par défaut") accompanied by an inline helper text (`RestoreDefaultsHelperText` — "Clears density preference and sets theme to follow system.") beneath the button, and the button dispatches `UserPreferenceClearedAction` + `ThemeChangedAction(ThemeValue.System)` on click
**And** the dialog close `×` (built-in `FluentDialog` dismiss) dismisses the dialog without rolling back any changes already made (changes are live, not staged)

**Given** the user selects a different density radio
**When** the `FluentRadioGroup.ValueChanged` event fires
**Then** `FcSettingsDialog` dispatches `UserPreferenceChangedAction(correlationId, newDensity, newEffective)` where `newEffective = DensityPrecedence.Resolve(newDensity, options.DefaultDensity, DensitySurface.Default, currentTier)`
**And** the preview panel re-renders at the new density via its `[Parameter] Density` parameter update
**And** `<body>` `data-fc-density` attribute updates via `FcDensityApplier` (AC6)
**And** `FcDensityAnnouncer` (D20) updates its visually-hidden `aria-live="polite"` region text to `string.Format(DensityAnnouncementTemplate, LocalizedDensityLabel(newEffective))` so assistive tech announces the density consequence (not just the radio state)

**References:** D11, D13, D14, D15, D20; Epic AC §126. **Tasks:** 4.2b, 5.1, 6.1, 6.2, 7.1. **Tests:** `FcSettingsDialogTests.RendersDensityRadioThemeAndPreviewAndRestoreFooter`, `FcSettingsDialogTests.DensityRadioSelectionDispatchesAction`, `FcSettingsDialogTests.RestoreDefaultsDispatchesClearedAndThemeSystem`, `FcDensityPreviewPanelTests.RendersAtRequestedDensity`, `FcDensityPreviewPanelTests.ShowForcedViewportBadgeRendersBadge`, `FcDensityAnnouncerTests.AnnouncesOnEffectiveDensityChange` (Tasks 10.8, 10.9, 10.9b, 10.6b).

---

## AC3: User density preference persists per tenant/user via `IStorageService` and restores across sessions

**Given** a user has `IUserContextAccessor` returning non-null tenant + user AND the user selects a density via the settings dialog radio group
**When** `UserPreferenceChangedAction` dispatches through Fluxor
**Then** `DensityEffects.HandlePersistDensity` serialises `DensityLevel?` as JSON (non-null enum value)
**And** writes to `IStorageService` under key `StorageKeys.BuildKey(tenantId, userId, "density")`
**And** a page refresh triggers `DensityEffects.HandleAppInitialized` which loads the value and dispatches `DensityHydratedAction(storedValue)`
**And** the reducer assigns `state with { UserPreference = action.UserPreference, EffectiveDensity = resolvedEffective }` (resolved from the hydrated preference + current options + current tier)
**And** `DensityHydratedAction` does NOT trigger a subsequent `storage.SetAsync` write (hydrate is read-only per D8, mirrors Story 3-2 ADR-038)

**Given** the user selects "Reset to defaults"
**When** `UserPreferenceClearedAction` dispatches
**Then** `DensityEffects` serialises a literal `null` under the density key (clearing the user preference)
**And** the reducer assigns `state with { UserPreference = null, EffectiveDensity = resolvedEffective }` (fall-through to deployment default / factory hybrid)

**Given** `IUserContextAccessor.TenantId` or `UserId` is null / empty / whitespace
**When** any density persist or hydrate effect runs
**Then** the effect logs `HFC2105_StoragePersistenceSkipped` at Information severity
**And** `storage.SetAsync` / `storage.GetAsync` is NOT called (asserted separately from the log assertion — both must hold, per the Story 3-2 AC2 parity pattern)

**References:** D8, D18, D19; Story 3-1 ADR-029. **Tasks:** 3.1, 3.3. **Tests:** `DensityEffectsScopeTests.PersistsOnValidScope`, `DensityEffectsScopeTests.SkipsOnNullTenant`, `DensityEffectsScopeTests.SkipsOnNullUser`, `DensityEffectsScopeTests.SkipsOnWhitespaceScope`, `DensityEffectsScopeTests.HydrateDoesNotRePersist`, `DensityPersistenceSnapshotTests.BlobSchemaLocked` (Task 10.5).

---

## AC4: Viewport ≤ Tablet forces `Comfortable` regardless of user preference (44 px touch targets)

**Given** the current `ViewportTier` reported by `FcLayoutBreakpointWatcher` is `Tablet` or `Phone`
**When** `DensityPrecedence.Resolve(...)` is invoked with any combination of `userPreference` / `deploymentDefault`
**Then** the function returns `DensityLevel.Comfortable`
**And** `UserPreference` state is NOT cleared (the user's choice is preserved; only `EffectiveDensity` is overridden)

**Given** a viewport transition `Desktop → Tablet` via `ViewportTierChangedAction`
**When** `DensityEffects.HandleViewportTierChanged` runs
**Then** the effect computes `newEffective = DensityPrecedence.Resolve(state.UserPreference, options.DefaultDensity, DensitySurface.Default, Tablet)` = `Comfortable`
**And** dispatches `EffectiveDensityRecomputedAction(Comfortable)`
**And** the reducer assigns `state with { EffectiveDensity = Comfortable }` (UserPreference unchanged)
**And** `FcDensityApplier` propagates the change to `<body>` `data-fc-density="comfortable"` via `fc-density.js#setDensity`

**Given** a viewport transition `Tablet → Desktop` where the user previously selected `UserPreference = Compact`
**When** the viewport widens
**Then** `EffectiveDensity` re-resolves to `Compact` (user preference re-applies at Desktop)
**And** no user-visible re-selection is required

**Given** the settings dialog is open at `ViewportTier.Tablet` AND the user has selected a non-Comfortable density radio
**When** the dialog renders
**Then** `FcDensityPreviewPanel` receives `ShowForcedViewportBadge=true` and renders a visible overlay badge (`PreviewOnlyBadgeText` — "Preview only — Comfortable is active.") at the top-right of the specimen
**And** the inline forced-viewport note (`DensityForcedByViewportNote`) renders above the preview panel
**And** together the two surfaces eliminate the "three competing truths" cognitive dissonance (Freya review 2026-04-19)

**Given** a viewport transition that changes `EffectiveDensity` (e.g., `Desktop → Tablet` with `UserPreference = Compact`)
**When** `FcDensityAnnouncer` (D20) observes the change
**Then** its visually-hidden `aria-live="polite"` region text updates to `"Density set to Comfortable."` (EN) / `"Densité réglée sur Confortable."` (FR)
**And** screen readers announce the consequence of the viewport forcing — not just the radio state

**References:** D7, D1, D14, D20; ADR-040. **Tasks:** 1.2, 2.2, 3.2, 4.2b, 7.1. **Tests:** `DensityPrecedenceTests.Resolve_AtTablet_IgnoresUserPreference`, `DensityPrecedenceTests.Resolve_AtPhone_IgnoresDeploymentDefault`, `DensityReducerTests.ViewportTierChanged_AtTablet_ForcesComfortable_PreservesUserPreference`, `DensityReducerTests.ViewportTierChanged_BackToDesktop_RestoresUserPreference`, `DensityEffectsTests.HandleViewportTierChanged_DispatchesEffectiveDensityRecomputed`, `FcDensityPreviewPanelTests.ShowForcedViewportBadgeRendersBadge`, `FcDensityAnnouncerTests.AnnouncesOnEffectiveDensityChange` (Tasks 10.1, 10.4, 10.6b, 10.9b).

---

## AC5: Roomy density is a permanent first-class feature, equally verified alongside Compact and Comfortable

**Given** the `DensityLevel` enum
**When** its values are inspected
**Then** `Roomy` is present at integer position 2 (Compact=0, Comfortable=1, Roomy=2) — unchanged from Story 1-3 shipping values
**And** no feature flag, no preview gate, no conditional rendering limits Roomy availability

**Given** a user selects `Roomy` in the settings dialog at Desktop viewport
**When** `UserPreferenceChangedAction(_, Roomy)` dispatches
**Then** `EffectiveDensity = Roomy` (tier 2 wins since tier override is inactive at Desktop)
**And** `<body>` `data-fc-density="roomy"` applies
**And** `var(--fc-spacing-unit) = 6px` cascades through every scoped-CSS consumer
**And** `FcDensityPreviewPanel` renders its specimen at Roomy spacing

**Given** test coverage
**When** `DensityPrecedenceTests`, `DensityReducerTests`, and `FcDensityPreviewPanelTests` run
**Then** Roomy is exercised in at least one `[Theory]` case per test class (no Roomy-excluded test matrix)

**References:** D2, D5, D14, D17; UX spec §193-195. **Tasks:** 1.2, 2.2, 7.1. **Tests:** `DensityPrecedenceTests.Resolve_UserRoomy_Returns_Roomy`, `DensityReducerTests.RoomyIsAssignableAndPersistable`, `FcDensityPreviewPanelTests.RendersAtRoomy_AppliesSixPxSpacingUnit`, `DensityPersistenceSnapshotTests.BlobSchemaLocked` (includes Roomy round-trip) (Tasks 10.1, 10.4, 10.9).

---

## AC6: `--fc-density` CSS custom property applied to `<body>` is the single density source for all generated views

**Given** `FcDensityApplier.razor` mounts inside `FrontComposerShell.Content`
**When** `OnAfterRenderAsync(firstRender: true)` fires
**Then** the component imports `fc-density.js` and invokes `setDensity(state.EffectiveDensity)` with the hydrated initial value
**And** `document.body.dataset.fcDensity` equals `"compact"` / `"comfortable"` / `"roomy"` (lowercase enum name)

**Given** `EffectiveDensity` changes via any of the four triggering actions (UserPreferenceChanged, UserPreferenceCleared, DensityHydrated, EffectiveDensityRecomputed)
**When** the `IStateSelection<FrontComposerDensityState, DensityLevel>` subscription fires
**Then** `FcDensityApplier` invokes `setDensity(newLevel)` (fire-and-forget — no await blocks render)
**And** `<body>` `data-fc-density` updates to the new level

**Given** `FrontComposerShell.razor.css`
**When** scoped CSS rules are inspected
**Then** three rules bind `body[data-fc-density="compact"] { --fc-spacing-unit: 2px; }`, `body[data-fc-density="comfortable"] { --fc-spacing-unit: 4px; }`, `body[data-fc-density="roomy"] { --fc-spacing-unit: 6px; }`
**And** no other component declares its own `--fc-density` or per-level padding logic (the "zero per-component density logic" invariant is enforced by code review + the `DensityNoPerComponentLogicLintTest` that greps for `--fc-density` across `src/` and fails if it appears outside `FrontComposerShell.razor.css` + `fc-density.js`)

**References:** D9, D10; ADR-041. **Tasks:** 4.1, 4.2, 4.3, 7.2. **Tests:** `FcDensityApplierTests.InvokesSetDensityOnInitialRender`, `FcDensityApplierTests.InvokesSetDensityOnStateChange`, `FcDensityApplierTests.DisposeReleasesModule`, `DensityNoPerComponentLogicLintTest.SearchesSrcForRogueDensityVars` (Task 10.6).

---

## AC7: Settings button in `HeaderEnd` is visible (Story 3-1 D26 placeholder is now unhidden); Ctrl+, opens the same dialog

**Given** Counter.Web boots with the default `FrontComposerShell` configuration (adopter leaves `HeaderEnd` null)
**When** the shell renders
**Then** `<FcSettingsButton />` auto-populates the `HeaderEnd` slot (symmetric to Story 3-2 D8 `HeaderStart` → `FcHamburgerToggle`)
**And** the button is VISIBLE (no `@if (false)` guard — Story 3-1 D26 retirement per D12)
**And** clicking the button opens `FcSettingsDialog` via `IDialogService.ShowDialogAsync<FcSettingsDialog>(...)`
**And** the button's `aria-label` resolves from `FcShellResources.SettingsTriggerAriaLabel`

**Given** an adopter supplies a non-null `HeaderEnd` fragment
**When** the shell renders
**Then** the adopter's fragment wins (override escape hatch preserved)
**And** the adopter is responsible for rendering their own settings access if desired

**Given** keyboard focus is anywhere inside `.fc-shell-root`
**When** the user presses `Ctrl+,`
**Then** `FrontComposerShell.HandleGlobalKeyDown` detects `e.Key == "," && e.CtrlKey && !e.ShiftKey && !e.AltKey`
**And** the handler calls `DialogService.ShowDialogAsync<FcSettingsDialog>(...)` (same call path as the button)
**And** the dialog opens with focus on the first radio button (via `FluentDialog`'s focus-trap)

**Given** the shortcut is inline-bound (Story 3-4 migrates to `IShortcutService`)
**When** the shell renders in 3-3
**Then** NO `IShortcutService` interface is introduced
**And** the `HandleGlobalKeyDown` handler is the single `@onkeydown` binding on `.fc-shell-root`

**References:** D11, D12, D16. **Tasks:** 5.1, 5.2, 8.1, 8.2. **Tests:** `FcSettingsButtonTests.RendersInHeaderEndByDefault`, `FcSettingsButtonTests.ClickOpensDialog`, `FrontComposerShellTests.AutoRendersSettingsButtonWhenHeaderEndIsNull`, `FrontComposerShellTests.AdopterSuppliedHeaderEndWins`, `FrontComposerShellTests.CtrlCommaOpensSettingsDialog`, `FcSettingsDialogTests.InitialFocusOnFirstRadio` (Tasks 10.7, 10.8, 10.10).

---
