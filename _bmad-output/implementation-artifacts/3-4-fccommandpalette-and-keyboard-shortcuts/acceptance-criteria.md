# Acceptance Criteria

> Eight ACs distilled from Epic 3 §156-196 + UX-DR1 + UX-DR43 + UX-DR67 + UX-DR68 + NFR5 + Story 3-3 D16 migration contract. Each cites binding decisions, tasks, and tests.

---

## AC1: `IShortcutService` registers `Ctrl+K`, `Ctrl+,`, `g h` at shell level with duplicate-binding conflict detection at registration time (HFC2108 Information)

**Given** `AddHexalithFrontComposer` is called in `Program.cs`
**When** `FrontComposerShell.OnAfterRenderAsync(firstRender: true)` fires
**Then** `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync()` runs exactly once per circuit
**And** `IShortcutService.Register("ctrl+k", "PaletteShortcutDescription", OpenPaletteAsync)` registers the palette opener
**And** `IShortcutService.Register("ctrl+,", "SettingsShortcutDescription", OpenSettingsAsync)` registers the settings opener (replacing Story 3-3's inline binding — AC8)
**And** `IShortcutService.Register("g h", "HomeShortcutDescription", NavigateHomeAsync)` registers the go-home chord

**Given** two `Register` calls supply the same normalised binding
**When** the second call runs
**Then** `ILogger` emits `HFC2108_ShortcutConflict` at Information severity with structured fields `{Binding, PreviousDescriptionKey, NewDescriptionKey, CallSiteFile, CallSiteLine}`
**And** the second registration REPLACES the first (last-writer-wins per D3)
**And** `TryInvokeAsync` dispatches to the second handler for all subsequent invocations

**Given** an `IDisposable` returned from `Register` is disposed
**When** `TryInvokeAsync` is called with the same binding
**Then** the disposed handler does NOT fire
**And** if a replacement registration exists, the replacement fires; otherwise `TryInvokeAsync` returns `false`

**References:** D1, D2, D3, D19; ADR-042. **Tasks:** 1.1, 1.2, 1.3. **Tests:** `ShortcutServiceTests.RegisterThenInvoke_RunsHandler`, `ShortcutServiceTests.DuplicateRegister_LogsHFC2108_LastWriterWins`, `ShortcutServiceTests.DisposedRegistration_NoLongerFires` (Task 10.1, 10.1e).

---

## AC2: `Ctrl+K` or header palette icon opens `FcCommandPalette` with `FluentSearch` auto-focused, dialog ARIA pattern, focus trap, and `aria-activedescendant` tracking

**Given** keyboard focus is anywhere inside `.fc-shell-root`
**When** the user presses `Ctrl+K`
**Then** `IShortcutService.TryInvokeAsync(e)` normalises the event to `"ctrl+k"`, looks up the handler, and invokes `OpenPaletteAsync`
**And** `OpenPaletteAsync` checks `IState<FrontComposerCommandPaletteState>.Value.IsOpen` and returns early if `true` (idempotent open, D12)
**And** otherwise dispatches `PaletteOpenedAction(CorrelationId)` then calls `IDialogService.ShowDialogAsync<FcCommandPalette>(new DialogParameters { Modal = true, Width = "600px", Title = Localizer["CommandPaletteTitle"].Value })`

**Given** the palette dialog has mounted
**When** `OnAfterRenderAsync(firstRender: true)` fires on `FcCommandPalette`
**Then** the `FluentSearch` input is auto-focused via `FluentSearch.FocusAsync()` (Task 5.2)
**And** the dialog root carries `role="dialog"` + `aria-label="@Localizer[\"CommandPaletteTitle\"]"` (inherited from `FluentDialog` + DialogParameters.Title + explicit `aria-label` on the content root)
**And** focus is trapped within the dialog by Fluent UI's default dialog behaviour

**Given** the palette is open and the `FcPaletteResultList` has rendered results
**When** the user navigates with arrow keys
**Then** the `SelectedIndex` updates via `PaletteSelectionMovedAction(+1/-1)` dispatches
**And** the `<ul role="listbox">` has `aria-activedescendant="@ResultElementId(SelectedIndex)"` pointing at the current option
**And** the `<li role="option">` at that index has `aria-selected="true"` while siblings have `aria-selected="false"`

**Given** the user clicks the header palette trigger icon
**When** `FcPaletteTriggerButton.OpenAsync` fires
**Then** it takes the same `OpenPaletteAsync` path as the `Ctrl+K` shortcut (single entry point)

### Focus-management transitions (per Sally's party-mode review)

The palette MUST exhibit the following five focus transitions. Each is asserted in `FcCommandPaletteTests.FocusManagement_*` tests (Task 10.7d).

| # | Transition | Trigger | Focus lands on | Notes |
|---|---|---|---|---|
| F1 | **Open** | `Ctrl+K` or `FcPaletteTriggerButton` click | `FluentSearch` input (caret ready) | FluentDialog's default focus-restore on close → trigger element; `OnAfterRenderAsync(firstRender)` calls `FluentSearch.FocusAsync()`. |
| F2 | **Arrow navigation** | `ArrowDown` / `ArrowUp` | **Stays on `FluentSearch` input** — `aria-activedescendant` updates to the new `ResultElementId(SelectedIndex)`; real focus never leaves the input (caret stays, typing continues to work). No `tabindex="-1"` focus shift on `<li role="option">`. | This is the activedescendant pattern (NOT roving tabindex). Violating this breaks text-entry mid-arrow. |
| F3 | **Activate (navigation success)** | `Enter` on a Projection/Recent/Shortcut-with-route, or Command | After `NavigationManager.NavigateTo`, focus is owned by the target page's focus management. Dialog calls `Dialog.CloseAsync()` which restores focus to the ORIGINAL invoker (the element focused before palette open — typically the shell root or the trigger button). | Two-step: palette closes → invoker gains focus → target page's `OnAfterRenderAsync` may move focus again. 3-4 does NOT manage cross-page focus; pages own their landing target. |
| F4 | **Activate (same-page nav fallback)** | `Enter` on a result whose route matches the current URL | `NavigationManager.NavigateTo(currentUrl)` is a no-op for Blazor router. After `PaletteClosedAction` fires, focus returns to the original invoker via FluentDialog's `OnDismiss` path. If the invoker is no longer in the DOM (rare edge), focus falls back to `document.body` via a JavaScript interop helper `fcFocusBodyAsync()` wired into `FcCommandPalette.DisposeAsync`. | Defensive fallback — no orphaned focus. `document.body` is the browser default landing target for tabindex=0 roots. |
| F5 | **Close (Escape)** | `Escape` in dialog-scope `@onkeydown` handler | Original invoker (shell root or trigger button per F1). Same dismiss path as FluentDialog's X-button close. | Asserted via bUnit: focus the trigger, open palette, press Escape, assert `document.activeElement == triggerButton`. Not a browser-focus assertion (bUnit cannot — it is a presence-in-DOM + tabindex assertion). Real-focus verification in Playwright E2E. |

**References:** D1, D6, D11, D12, D18. **Tasks:** 5.1, 5.2, 5.3, 7.1. **Tests:** `FcCommandPaletteTests.RendersDialogWithRoleAndAriaLabel`, `FcCommandPaletteTests.AutoFocusesSearchInputOnFirstRender`, `FcCommandPaletteTests.ArrowNavigationDispatchesSelectionMoved`, `FcCommandPaletteTests.AriaActivedescendantTracksSelectedIndex`, `FcCommandPaletteTests.FocusManagement_ArrowsKeepFocusOnSearchInput`, `FcCommandPaletteTests.FocusManagement_EscapeRestoresFocusToInvoker`, `FcPaletteTriggerButtonTests.ClickOpensPalette`, `FrontComposerShellTests.PaletteTriggerAutoPopulatesAheadOfSettings` (Task 10.7, 10.7d, 10.8b, 10.10).

---

## AC3: Typing a query produces debounced (150 ms) fuzzy-matched results categorised Projections / Commands / Recent with sub-100 ms scoring time (NFR5)

**Given** the palette is open and the user types `"cou"`
**When** the `FluentSearch.ValueChanged` event fires
**Then** `FcCommandPalette.razor.cs` dispatches `PaletteQueryChangedAction(CorrelationId, "cou")`
**And** `CommandPaletteEffects.HandlePaletteQueryChanged` awaits `Task.Delay(150, _queryCts.Token)`

**Given** a second keystroke lands within 150 ms (user types `"u"` → query becomes `"cou"` → `"cou "` in 80 ms)
**When** the second `PaletteQueryChangedAction` dispatches
**Then** the effect cancels the previous `_queryCts` (causing the earlier `Task.Delay` to throw `OperationCanceledException` — swallowed silently)
**And** starts a new `Task.Delay(150, newCts.Token)` (via `TimeProvider` per D22 so tests use `FakeTimeProvider`)

**Given** the palette is closed (`PaletteClosedAction` dispatched) while a debounce timer is in flight (D20 stale-result guard)
**When** the in-flight `Task.Delay(150)` elapses AFTER the close
**Then** `CommandPaletteEffects.HandlePaletteClosed` has cancelled `_queryCts` (belt: upstream work aborts with `OperationCanceledException`)
**And** if cancellation races the dispatch, the reducer for `PaletteResultsComputedAction` no-ops when `state.IsOpen == false` (braces: downstream assignment refused)
**And** re-opening the palette shows the freshly-computed default results (Recent + top projections), never stale results from the previous session

**Given** 150 ms elapses without a new keystroke
**When** `Task.Delay` completes successfully
**Then** the effect enumerates `IFrontComposerRegistry.GetManifests()`, scores each projection + command via `PaletteScorer.Score(query, candidate)`, applies `+15` contextual bonus when `candidate.BoundedContext == NavigationState.CurrentBoundedContext` (D7)
**And** takes top-50 by score, interleaves Recent matches, dispatches `PaletteResultsComputedAction(query, results)`
**And** the reducer assigns `state with { Results = action.Results, SelectedIndex = 0, LoadState = Ready }`

**Given** the scorer is benchmarked against 1000 synthetic candidates
**When** `PaletteScorerBench.Score_1000Candidates` runs in `BenchmarkDotNet`
**Then** the per-candidate scoring cost is `< 100 μs`
**And** the total scoring pass is `< 100 ms` (NFR5 guardrail — well under the "< 100 ms per user keystroke" total-roundtrip budget when combined with the 150 ms debounce)

**Given** the palette results render
**When** `FcPaletteResultList` groups them
**Then** three `<section role="group" aria-labelledby="...">` regions render with headings `PaletteCategoryProjections` / `PaletteCategoryCommands` / `PaletteCategoryRecent`
**And** each group renders only its matching category (empty groups are omitted entirely — no "No projections" placeholder)

**References:** D6, D7, D8, D9, D14; ADR-043. **Tasks:** 3.1, 4.1, 4.2, 5.1, 6.1. **Tests:** `PaletteScorerTests.Score_ExactPrefix_Returns100Plus`, `PaletteScorerTests.Score_ContainsSubstring_Returns50Plus`, `PaletteScorerTests.Score_FuzzySubsequence_Returns10Plus`, `PaletteScorerTests.Score_NoMatch_ReturnsZero`, `PaletteScorerTests.Score_IsCaseInsensitive`, `PaletteScorerPropertyTests.ScoreIsDeterministic`, `PaletteScorerPropertyTests.ScoreIsMonotonicOnPrefixLength`, `CommandPaletteEffectsTests.DebounceCancelsEarlierKeystroke`, `CommandPaletteEffectsTests.ContextualBonusAppliesToMatchingBoundedContext`, `CommandPaletteEffectsTests.ResultsCategorisedIntoProjectionsCommandsRecent`, `PaletteScorerBench.Score_1000Candidates` (Tasks 10.3, 10.3a, 10.4b).

---

## AC4: Contextual mode — palette invoked from within a bounded context surfaces that context's commands first (+ in-context scoring bonus)

**Given** the user is on a route `/commerce/orders` where `NavigationState.CurrentBoundedContext == "Commerce"`
**When** the palette opens and the user types `"o"`
**Then** the effect's contextual-bonus pass adds `+15` to every result where `candidate.BoundedContext == "Commerce"`
**And** the top-50 sort-by-score-descending pushes in-context results to the top of the **Projections** and **Commands** categories
**And** `PaletteResult.IsInCurrentContext` is `true` on those results (for UI hinting — the result row renders a subtle "(in current context)" secondary label per UX spec §91)

**Given** the same user types `"increment"` on the same `/commerce/orders` route
**When** the scorer matches both `Counter.Domain.IncrementCommand` (boundedContext=`Counter`) and a hypothetical `Commerce.Domain.IncrementInventoryCommand` (boundedContext=`Commerce`)
**Then** both candidates receive the same `PaletteScorer.Score` (exact-prefix "increment" == "increment*") but the Commerce result receives the `+15` contextual bonus
**And** the Commerce candidate sorts ABOVE the Counter candidate

**Given** the user has no current bounded context (route `/` home)
**When** the palette opens and the user types a query
**Then** no contextual bonus is applied (all results score at their base `PaletteScorer.Score`)
**And** the sort order is pure-score-descending

**References:** D7, D8; Epic AC §170-172. **Tasks:** 3.1, 4.2. **Tests:** `CommandPaletteEffectsTests.ContextualBonusAppliesToMatchingBoundedContext`, `CommandPaletteEffectsTests.NoContextualBonus_WhenNavigationContextIsNull` (Task 10.4b).

---

## AC5: Keyboard navigation — Arrow keys move selection, Enter activates, Escape closes; results use `role="listbox"` / `role="option"`; screen reader announces result count

**Given** the palette is open and has ≥ 2 results
**When** the user presses `ArrowDown`
**Then** `FcCommandPalette.razor.cs`'s intra-dialog `@onkeydown` handler dispatches `PaletteSelectionMovedAction(+1)`
**And** the reducer clamps `SelectedIndex` to `[0, Results.Length − 1]` (no wrap in v1)
**And** `aria-activedescendant` updates to the new `ResultElementId(SelectedIndex)`

**Given** the user presses `Enter` with a selected result
**When** the dialog's `KeyDown` handler runs
**Then** it dispatches `PaletteResultActivatedAction(SelectedIndex)`
**And** `HandlePaletteResultActivated` effect reads `state.Results[action.SelectedIndex]`, calls `NavigationManager.NavigateTo(result.RouteUrl)` (Projection | Recent | Shortcut-with-route) OR navigates to the generated `/domain/{CommandName}` command form route (Command)
**And** dispatches `PaletteClosedAction` + `RecentRouteVisitedAction(newRoute)`
**And** `IDialogService`'s dialog close call runs (via the effect's `IDialogInstance` reference)

**Given** the user presses `Escape`
**When** the dialog's `KeyDown` handler runs
**Then** `PaletteClosedAction` dispatches + `Dialog.CloseAsync()` is invoked
**And** the Fluxor `IsOpen` flips to `false` so a subsequent `Ctrl+K` opens a fresh instance

**Given** the palette opens (any trigger)
**When** the palette dialog first renders
**Then** the visually-hidden `role="status" aria-live="polite" aria-atomic="true"` region renders with **empty text content** (`<div ...></div>`)
**And** then on the next render tick (scheduled via `await Task.Yield(); await InvokeAsync(StateHasChanged);` inside `FcCommandPalette.OnAfterRenderAsync(firstRender: true)` — immediately AFTER the `FluentSearch.FocusAsync()` call), the region is populated with the initial result count
**And** the empty-to-populated DOM mutation fires a genuine `aria-live` announce event that NVDA / JAWS / VoiceOver pick up as "3 results" (or the localised `PaletteResultCountTemplate` value)

**Given** `PaletteResultsComputedAction` fires (user types a query and debounce completes)
**When** the aria-live region's text content changes from one populated value to another
**Then** assistive tech announces the NEW text content (standard `aria-live="polite"` mutation polling)
**And** the announcement is localised per user culture (EN: "5 results" / FR: "5 résultats")

**Given** the palette renders with zero results (`PaletteNoResultsText`)
**When** the aria-live region populates
**Then** the announce text is the "No matches found" string — NOT "0 results" — so SR users get the same semantic distinction sighted users see (empty-state copy, not a literal zero count)

**References:** D15, D17; Epic AC §177-180. **Tasks:** 5.2, 5.5, 5.3, 5.4. **Tests:** `FcCommandPaletteTests.ArrowDownDispatchesSelectionMoved`, `FcCommandPaletteTests.EnterDispatchesActivation`, `FcCommandPaletteTests.EscapeClosesPalette`, `FcCommandPaletteTests.AriaLiveRegionRendersEmptyOnFirstRenderThenPopulatesOnNextTick`, `FcCommandPaletteTests.AriaLivePoliteAnnouncesResultCountOnQueryChange`, `FcCommandPaletteTests.AriaLiveAnnouncesNoMatchesForEmptyResults`, `FcPaletteResultListTests.RolesListboxAndOptionApplied`, `FcPaletteResultListTests.AriaSelectedFlipsOnSelectedIndexChange` (Tasks 10.7, 10.7b, 10.7c, 10.8).

> **Screen-reader smoke test (manual, Story 10-2 baseline)**: Open palette in Counter.Web with NVDA active. Verify "3 results" announces within 200 ms of dialog open. Type "cou" — verify "1 result" announces within 200 ms of the debounce completing. Clear the query — verify the pre-populated count re-announces. Failure of any of the three is a D15 regression. This smoke test is manual in 3-4 and automated via Playwright + axe-core in Story 10-2.

---

## AC6: Shortcut reference is discoverable via a default Commands entry AND via the `shortcuts` / `?` / `help` / `keys` / `kb` query aliases, rendering the complete shortcut reference from `IShortcutService.GetRegistrations()`

**Given** the palette is open with no query (initial state)
**When** `HandlePaletteOpened` populates the default Commands category
**Then** a synthetic entry appears in the Commands category with:
- `DisplayLabel` = `Localizer["KeyboardShortcutsCommandLabel"].Value` ("Keyboard Shortcuts" / "Raccourcis clavier")
- `CommandTypeName` = `"@shortcuts"` (sentinel — the `@` prefix is reserved for framework-synthetic entries per D23 cross-story contract)
- `RouteUrl` = `null`
- Description text = `Localizer["KeyboardShortcutsCommandDescription"].Value` ("View all keyboard shortcuts" / "Afficher tous les raccourcis clavier")

**And** activating the synthetic entry (Enter or click) dispatches `PaletteQueryChangedAction(correlationId, "shortcuts")` instead of navigating — the palette refills with the shortcut-reference view

**Given** the palette is open
**When** the user types any of `"shortcuts"`, `"?"`, `"help"`, `"keys"`, `"kb"`, or `"shortcut"` (case-insensitive)
**Then** `CommandPaletteEffects.ResolveShortcutAliasQuery(query)` returns the canonical `"shortcuts"` literal
**And** `HandlePaletteQueryChanged` detects the canonical match, BYPASSES `PaletteScorer`, enumerates `IShortcutService.GetRegistrations()` → maps each to `PaletteResult(Category: Shortcut, DisplayLabel: NormalisedLabel, BoundedContext: "", RouteUrl: null, CommandTypeName: null, Score: 0, IsInCurrentContext: false)` with `DescriptionKey` populated from the registration
**And** dispatches `PaletteResultsComputedAction(<original query>, registrations)` — action payload carries the user's actual keystrokes so the aria-live region announces on each alias type

**Given** the shortcuts category renders
**When** `FcPaletteResultList` renders the Shortcut category group
**Then** the heading reads `@Localizer["ShortcutsCategoryLabel"]` ("Keyboard shortcuts" / "Raccourcis clavier")
**And** each row shows the normalised label (e.g., "Ctrl+K") paired with the localised description resolved from `ShortcutRegistration.DescriptionKey` (via `Localizer[registration.DescriptionKey]`)
**And** shortcut rows are NOT activatable on Enter (they are informational — selecting does not dispatch navigation)
**And** shortcut rows carry `aria-disabled="true"` on the `<li role="option">` element — this makes the non-activatability discoverable to screen-reader users (VoiceOver/NVDA announce "dimmed" / "unavailable") and prevents the silent-failure UX where a sighted keyboard user presses Enter with no visible / audible feedback (post-elicitation gap-fill)
**And** the `aria-selected="true"` state still applies on arrow-key navigation (the rows ARE selectable for focus-tracking purposes via `aria-activedescendant`, just not activatable)

**References:** D1, D14, D23. **Tasks:** 3.2, 3.3, 3.3a, 6.1, 9.1, 9.2. **Tests:** `CommandPaletteEffectsTests.ShortcutsQueryBypassesScorer`, `CommandPaletteEffectsTests.ShortcutsQueryAliases_CanonicaliseTo_Shortcuts` (Theory: `?`, `help`, `keys`, `kb`, `shortcut`), `CommandPaletteEffectsTests.SyntheticKeyboardShortcutsCommandEntry_AppearsInDefaultOpen`, `FcPaletteResultListTests.RendersShortcutCategoryWithDescriptions`, `FcPaletteResultListTests.SyntheticShortcutsEntryActivatesShortcutsQuery`, `FcPaletteResultListTests.ShortcutRowsCarryAriaDisabledTrue` (Tasks 10.4b, 10.8).

---

## AC7: `IBadgeCountService` graceful degradation — palette renders with no errors and no empty-badge placeholders when the service is absent; Story 3-5 registration activates badge counts without palette changes

**Given** `IBadgeCountService` is NOT registered in DI (v1.0 pre-Story-3-5 state)
**When** the palette renders projection results
**Then** `FcPaletteResultList.OnInitialized` calls `ServiceProvider.GetService<IBadgeCountService>()` which returns `null`
**And** the badge render branch `@if (_badgeCounts is not null && ...)` short-circuits
**And** NO badge element renders in the row
**And** no empty placeholder, no "0" literal, no error log, no broken aria
**And** NO `InvalidOperationException` is thrown (unlike the `[Inject]` pattern which would have called `GetRequiredService` and thrown — see ADR-044 rejected alternative)

**Given** `IBadgeCountService` IS registered (Story 3-5 lands)
**When** the palette renders projection results
**Then** `_badgeCounts` resolves to the registered implementation via `GetService<IBadgeCountService>()`
**And** for each `PaletteResult` with `Category == Projection` and `ProjectionType is not null` and `_badgeCounts.Counts.TryGetValue(ProjectionType, out var count)`
**And** a `FluentBadge Intent="Info"` with `@count` renders at the end of the row
**And** subscribing to `_badgeCounts.CountChanged` in `OnInitialized` keeps the badge counts fresh (null-guarded; disposed in `DisposeAsync`)

**Given** `IBadgeCountService` is registered but the projection's `ProjectionType` is NOT present in `Counts` (projection has no ActionQueue hint, so it's not tracked)
**When** the row renders
**Then** no badge renders (same code path as "service absent" — absent data is indistinguishable from absent service at the row level)

**References:** D16; ADR-044. **Tasks:** 6.2. **Tests:** `FcPaletteResultListTests.RendersWithoutBadges_WhenBadgeServiceIsNull`, `FcPaletteResultListTests.RendersBadges_WhenBadgeServiceIsRegistered_AndProjectionMatches`, `FcPaletteResultListTests.NoBadgePlaceholder_WhenBadgeServiceResolvesButProjectionUnknown` (Task 10.8).

---

## AC8: Story 3-3 inline Ctrl+, binding is migrated to `IShortcutService.Register("ctrl+,", ...)` and the Story 3-3 `CtrlCommaSingleBindingTest` is deleted per the migration contract

**Given** the Story 3-3 `FrontComposerShell.HandleGlobalKeyDown` inline branch for `e.Key == "," && e.CtrlKey`
**When** Story 3-4 Task 8.1-8.3 runs
**Then** the inline branch is DELETED from `FrontComposerShell.razor.cs`
**And** the `.fc-shell-root`'s `@onkeydown="HandleGlobalKeyDown"` handler now calls `IShortcutService.TryInvokeAsync(e)` as its sole routing path (after the text-input-target guard per D5)
**And** `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync` calls `_shortcuts.Register("ctrl+,", "SettingsShortcutDescription", OpenSettingsAsync)` on first render per circuit
**And** `OpenSettingsAsync` invokes `DialogService.ShowDialogAsync<FcSettingsDialog>(...)` using the same `DialogOptions` as Story 3-3 (Modal=true, Width=480px, Title=`Localizer["SettingsDialogTitle"].Value`)

**Given** the Story 3-3 `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` file
**When** Story 3-4 Task 8.4 runs
**Then** the file is DELETED from the repo
**And** a replacement `FrontComposerShellTests.CtrlCommaInvokesRegisteredShortcut` is added that asserts: (a) the registrar registered `"ctrl+,"` on first render; (b) `TryInvokeAsync(new KeyboardEventArgs { Key = ",", CtrlKey = true })` invokes `FcSettingsDialog` via `IDialogService.ShowDialogAsync<FcSettingsDialog>` (asserted via `NSubstitute` mock)
**And** the user-visible behaviour of `Ctrl+,` is UNCHANGED (same dialog, same options, same focus trap)

**References:** Story 3-3 D16 migration contract; D1, D5. **Tasks:** 8.1, 8.2, 8.3, 8.4. **Tests:** `FrontComposerShellTests.CtrlCommaInvokesRegisteredShortcut`, `FrontComposerShellTests.CtrlKInvokesPaletteViaRegisteredShortcut`, `FrontComposerShellTests.TextInputTargetGuardSkipsBareLetterChords` (Task 10.10a).

---
