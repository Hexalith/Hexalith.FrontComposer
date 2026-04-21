# Dev Agent Record

## Agent Model Used

claude-opus-4-7 (1M context) executing the bmad-dev-story workflow on 2026-04-20.

## Debug Log References

- Initial regression sweep after registering `CommandPaletteEffects` exposed 26 test failures rooted in unit-test fixtures that bypass `AddHexalithFrontComposer` (no `IFrontComposerRegistry` / `IUlidFactory` / `TimeProvider` / `IStringLocalizer` / `NavigationManager` / `IUserContextAccessor`). Resolved by lazy-resolving every non-Fluxor-core dependency on `CommandPaletteEffects` through `IServiceProvider`. The constructor now takes only Fluxor `IState<>`s + `ILogger` + `IServiceProvider`. Production behaviour is unchanged because every production registration still satisfies the lookups; isolated unit-test fixtures are no longer forced to register palette deps they don't exercise.
- Three deviations from the spec are documented in `spike-notes.md`: HFC2107 collision (renamed to HFC2108–HFC2111), `IFrontComposerRegistry.HasFullPageRoute` extension added inline (Story 2-2 amendment PR not blocked on), and `FluentSearch` swap to `FluentTextInput` with `TextInputType.Search` (Fluent UI v5 RC2 ships `FluentTextInput`, not `FluentSearch`).

## Completion Notes List

- `test_baseline_pre_3_4 = 594` `[Fact]` / `[Theory]` declarations across 100 files (Task 0.1 — Grep snapshot 2026-04-20 on a clean main checkout).
- Task 0.2 — FluentSearch is NOT in v5 RC2; fell back to `FluentTextInput` with `TextInputType="Search"` per the spike-notes contingency. Two-way bind via `Value` + `ValueChanged`; focus via `_searchRef.Element?.FocusAsync()`.
- Task 0.3 — `FluentBadge.Appearance` (not `Intent`) maps to `BadgeAppearance.Tint` for the projection-row badge.
- Task 0.4 — Dialog content `@onkeydown` fires for Escape / ArrowUp / ArrowDown / Enter without being swallowed by the underlying `FluentDialog`.
- Task 0.5 — `KeyboardEventArgs.MetaKey` is available on Blazor Server `@onkeydown` events; `meta` is part of `ShortcutBinding.Normalize`.
- Task 0.6 — `Icons.Regular.Size20.Search` exists in v5 RC2.
- Task 0.7 — `dotnet build Hexalith.FrontComposer.sln --nologo` reports `0 Warning(s) / 0 Error(s)` (clean baseline).
- Task 0.9 — `Microsoft.Extensions.TimeProvider.Testing` already referenced by `Hexalith.FrontComposer.Shell.Tests.csproj`; no addition required.
- Task 0.10 — `HasFullPageRoute` did NOT exist on `IFrontComposerRegistry`. Per the L01 cross-story-contracts memory, added the method inline in this story (Contracts-side append + concrete implementation in `FrontComposerRegistry`). Surfaces every registered command as reachable; Story 9-4 will layer build-time validation. Tripwire grep recorded in `spike-notes.md`.
- ✅ AC1 — `IShortcutService.Register("ctrl+k", ...)`, `Register("ctrl+,", ...)`, `Register("g h", ...)` all wired through `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync()` with D24 idempotency guard. Duplicate registration logs `HFC2108_ShortcutConflict` Information + last-writer-wins.
- ✅ AC2 — `Ctrl+K` and the header palette icon both open `FcCommandPalette` via `IDialogService.ShowDialogAsync<FcCommandPalette>`. Search input auto-focuses on first render. `aria-activedescendant` updates with arrow navigation.
- ✅ AC3 — 150 ms `Task.Delay(_, _, _timeProvider, ct)` debounce (`TimeProvider`-driven so tests use `FakeTimeProvider`); `PaletteScorer` runs three-band fuzzy match; results categorised as Projections / Commands / Recent.
- ✅ AC4 — `+15` contextual bonus applied by the effect (NOT the pure scorer) when `manifest.BoundedContext == NavigationState.CurrentBoundedContext`.
- ✅ AC5 — Dialog handles Escape / ArrowUp / ArrowDown / Enter via `@onkeydown`. `aria-live="polite"` region renders empty on first tick and populates on the next via `await Task.Yield(); StateHasChanged();` per D15 anti-regression.
- ✅ AC6 — Typing `shortcuts` (or aliases `?` / `help` / `keys` / `kb` / `shortcut`) bypasses the scorer and renders `IShortcutService.GetRegistrations()` as a Shortcut-category result list with `aria-disabled="true"`. Default open also surfaces a synthetic "Keyboard Shortcuts" entry per D23.
- ✅ AC7 — `FcPaletteResultList` consumes `IBadgeCountService` via `IServiceProvider.GetService<T>()` (NOT `[Inject]`) per ADR-044. Absent service → no badge, no placeholder, no exception. Bunit tests cover both registered + absent.
- ✅ AC8 — Story 3-3 inline `Ctrl+,` branch deleted from `FrontComposerShell.razor.cs`; the registrar's `Register("ctrl+,", ...)` now owns the dispatch. `tests/.../CtrlCommaSingleBindingTest.cs` deleted; replacement coverage in `FrontComposerShellTests.CtrlCommaOpensSettingsDialogFromShellRoot` (existing test still green) + the new `CtrlKOpensPaletteDialogViaShortcutService` palette test.
- Test delta: pre-3-4 baseline 594 `[Fact]/[Theory]` declarations → post-3-4 727 declarations (+133 net new). Shell.Tests run reports 572 passing / 0 failing / 2 skipped (skipped pair are pre-existing latency E2E tests under `[Trait("Category", "Performance")]`). `dotnet build Hexalith.FrontComposer.sln --nologo --warnaserror` reports `0 Warning(s) / 0 Error(s)`.
- Diagnostic IDs allocated: `HFC2108_ShortcutConflict` (Information), `HFC2109_ShortcutHandlerFault` (Warning), `HFC2110_PaletteScoringFault` (Warning), `HFC2111_PaletteHydrationEmpty` (Information with reasons `Empty` / `Corrupt` / `Tampered`). All documented in `Contracts/Diagnostics/FcDiagnosticIds.cs`.
- Story 2-2 contract extension: `IFrontComposerRegistry.HasFullPageRoute(string commandTypeName) : bool` added; default implementation in `FrontComposerRegistry` returns `true` for every command in any registered manifest. Story 9-4 layers a build-time analyzer that flags missing FullPage routes at compile time.
- Story 3-4 D7 extension: `FrontComposerNavigationState.CurrentBoundedContext` (nullable) appended via primary-constructor default parameter so the Story 3-3 / 3-2 test fixtures continue to construct the state with the existing 3-arg form. The new `BoundedContextChangedAction` + `ReduceBoundedContextChanged` reducer keep the route-derived value in sync; route-watching wiring is a Story 3-6 concern (palette consumes whatever `CurrentBoundedContext` is set to).

## File List

**Created (Contracts):**
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/IShortcutService.cs`
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutRegistration.cs`
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs`
- `src/Hexalith.FrontComposer.Contracts/Badges/IBadgeCountService.cs`
- `src/Hexalith.FrontComposer.Contracts/Badges/BadgeCountChangedArgs.cs`

**Created (Shell):**
- `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs`
- `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`
- `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteState.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextChangedAction.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor` + `.razor.cs` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor` + `.razor.cs`

**Modified:**
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` — added HFC2108–HFC2111.
- `src/Hexalith.FrontComposer.Contracts/Registration/IFrontComposerRegistry.cs` — added `HasFullPageRoute`.
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs` — implemented `HasFullPageRoute`.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — palette trigger now renders ahead of settings in HeaderEnd auto-populate.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — deleted Story 3-3 inline `Ctrl+,` branch; routes `HandleGlobalKeyDown` through `IShortcutService.TryInvokeAsync` with the D5 text-input guard; injects `IShortcutService` + `FrontComposerShortcutRegistrar`; calls `Registrar.RegisterShellDefaultsAsync` from `OnAfterRenderAsync(firstRender: true)`.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` — appended nullable `CurrentBoundedContext` (default `null`).
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs` — added `ReduceBoundedContextChanged`.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` — 14 new keys × 2 locales = 28 new strings.
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` — Scoped registrations for `IShortcutService`, `FrontComposerShortcutRegistrar`, `CommandPaletteEffects`.

**Tests created:**
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/IShortcutServiceTests.cs` (replaced by the tests below in the Shortcuts/ folder)
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutBindingNormalizeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/FrontComposerShortcutRegistrarTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Routing/CommandRouteBuilderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerPropertyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteReducerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`

**Tests modified:**
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — appended `PaletteTriggerAutoPopulatesAheadOfSettings` + `CtrlKOpensPaletteDialogViaShortcutService` + `TextInputBareLetterChord_DoesNotTriggerNavigation`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` — appended `PaletteAndShortcutKeysResolveInBothLocales` `[Theory]` covering 14 new keys.

**Tests deleted:**
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` — Story 3-3 D16 migration contract retirement (the inline `Ctrl+,` binding is gone; `IShortcutService` brings its own conflict-detection invariant).

**Out-of-scope (Known Gaps deferred):**
- Build-time Roslyn analyzer for `IShortcutService.Register` uniqueness → Story 9-4.
- `IBadgeCountService` concrete implementation + SignalR hub subscription → Story 3-5.
- `IPaletteAliasProvider` adopter extensibility for D23 alias overrides → v1.x.
- Playwright palette E2E (`tests/.../EndToEnd/CommandPaletteE2ETests.cs`) → deferred to the Aspire MCP verification path described in Task 11.3 (manual on first ship; Story 10-2 pipeline runs the Playwright matrix).
- BenchmarkDotNet `PaletteScorerBench.Score_1000Candidates` micro-bench → opt-in via `[Trait("Category", "Performance")]` (Story 1-8 precedent); the algorithm's allocation profile is bounded by the top-50 cap and a 1000-candidate registry stays well under the NFR5 100 ms total-roundtrip budget by inspection.

## Change Log

- 2026-04-20 — Implemented Story 3-4 in a single bmad-dev-story session. Net delta: +18 created files, ~6 modified files (Shell), 1 modified test file, 1 deleted test file, +12 new test files, +14 new resource keys × 2 locales. `dotnet test` reports 572 / 0 / 2 (passed / failed / skipped) on Shell.Tests; full solution `dotnet test` passes. `dotnet build --warnaserror` clean.

## Review Findings

*Generated by bmad-code-review on 2026-04-20 (three parallel reviewers: Blind Hunter, Edge Case Hunter, Acceptance Auditor). Raw counts: 43 — dedup → 37 unique — triage → 6 decision-needed, 26 patch, 1 defer, 4 dismissed. Dismissed: chord-lock ref-equality nit, brittle-timer hypothetical, adopter-localizer contract violation, `IShortcutService` netstandard2.0 TFM gate (intentional per `KeyboardEventArgs` dependency).*

### Decision-needed (6 resolved on 2026-04-21)

- [x] [Review][Decision] **D21 registry contract fractured** — Resolved: **ratify deviation** (companion interface + extension kept). Follow-up patch below updates spike-notes §D21 + `IFrontComposerRegistry` XML docs to flag the companion-opt-in contract.
- [x] [Review][Decision] **Per-user state bleed on user-switch** — Resolved: **PATCH** (option 1). See patch below.
- [x] [Review][Decision] **Text-input guard JS round-trip per keystroke** — Resolved: **PATCH** (option 1). See patch below.
- [x] [Review][Decision] **`SelectedIndex` hard-reset to 0** — Resolved: **PATCH** (option 1). See patch below.
- [x] [Review][Decision] **`FluentBadge.Appearance` `Tint` vs `Accent`** — Resolved: **PATCH** (option 1). See patch below.
- [x] [Review][Decision] **`HFC1601_ManifestInvalid` validator missing** — Resolved: **PATCH** (option 1). See patch below.

### Patch (26 original + 6 from decision resolution = 32 → 31 applied + 1 withdrawn + 1 partial)

*Applied and verified on 2026-04-21 with `dotnet build --warnaserror` clean and `dotnet test` passing (613 passed / 0 failed / 2 skipped for Shell.Tests; 277 passed for SourceTools.Tests).*

- [x] [Review][Patch, from DN1] Spike-notes §D21 amended to document the companion-interface ratification; `IFrontComposerRegistry` XML doc now points at `IFrontComposerFullPageRouteRegistry` as the opt-in contract.
- [x] [Review][Patch, from DN2] `PaletteScopeChangedAction` + reducer + `HandlePaletteScopeChanged` effect added — adopter wires it by dispatching on `IUserContextAccessor` change. Reducer clears `RecentRouteUrls`; effect re-runs hydrate for the new scope.
- [x] [Review][Patch, from DN3] Editable-element filter moved into `fc-keyboard.js:registerShellKeyFilter` — the C#-side `IsEditableElementActiveAsync` round-trip deleted. Shell now calls `Shortcuts.TryInvokeAsync` directly.
- [x] [Review][Patch, from DN4] `ReducePaletteResultsComputed` preserves `SelectedIndex` clamped into `[0, Results.Length)`.
- [~] [Review][Patch, from DN5] WITHDRAWN after verification — `BadgeAppearance.Accent` does not exist in v5 RC2 (enum members are `Filled` / `Ghost` / `Outline` / `Tint`). Ratified `Tint` as the Info-equivalent; spike-notes §0.3 corrected to match the real enum.
- [x] [Review][Patch, from DN6] `HFC1601_ManifestInvalid` diagnostic added to `FcDiagnosticIds`; `FrontComposerRegistry.ValidateManifests()` throws `InvalidOperationException("HFC1601: ...")` at startup when a registered command has no FullPage route.
- [~] [Review][Patch] `IsInternalRoute` scheme check — WITHDRAWN after verification. The Blind-Hunter "false positive on `/docs/about-http-1.1`" claim is wrong — that URL contains `http-` (hyphen), not `http:` (colon). The original substring check correctly rejects open-redirect URLs (e.g., `/redirect?next=https://evil.com`) that an anchored-only check would let through. Reverted.
- [x] [Review][Patch] `HandlePaletteResultActivated` null `CommandTypeName` — guarded via `when` clause; returns null targetUrl instead of calling `BuildRoute(..., string.Empty)`.
- [x] [Review][Patch] Hydrate cap — `HandleAppInitialized` caps `filtered.Length` at `RingBufferCap`; reducer also caps on hydrate.
- [x] [Review][Patch] Hydrate-vs-first-visit race — `ReducePaletteHydrated` guards with `if (!state.RecentRouteUrls.IsEmpty) return state;`.
- [x] [Review][Patch] CSS selector — `::deep fluent-search` renamed to `::deep fluent-text-input`.
- [x] [Review][Patch] Registrar `_registered` idempotency — converted to `int` + `Interlocked.Exchange`.
- [x] [Review][Patch] Chord-pending focus-transition guard — the DN3 JS-side filter now suppresses bare-letter keys targeting editable elements before they reach Blazor. Chord state no longer leaks across focus changes via the old C# guard-and-let-through pattern.
- [x] [Review][Patch] `LocationChanged` handler — wrapped in try/catch `ObjectDisposedException`; handler detaches itself defensively if the circuit is already disposed.
- [x] [Review][Patch] `BoundedContextRouteParser` case normalisation — extracted segments now lowercased via `ToLowerInvariant` so PascalCase / kebab-case URLs for the same BC don't churn the reducer.
- [x] [Review][Patch] `ShortcutService.TryInvokeAsync` — `_disposed` guard added at entry.
- [x] [Review][Patch] Debounce stale-query guard — `ReducePaletteResultsComputed` checks `state.Query == action.Query` (Ordinal) before assigning.
- [x] [Review][Patch] `OpenPaletteAsync` catch — narrowed to `Exception when ex is not OperationCanceledException`; compensating dispatch wrapped in `try { … } catch (ObjectDisposedException) { }`.
- [x] [Review][Patch] `OpenPaletteAsync` IsOpen race — added `_palettePending` flag with `Interlocked.Exchange` serialising read-and-dispatch; cleared in `finally`.
- [x] [Review][Patch] `OnSelectionClickedAsync` + Enter handler — body-focus verdict computed BEFORE dispatch (pre-dispatch `NavigationManager.Uri` snapshot) so the navigation effect cannot racily advance the comparison.
- [x] [Review][Patch] `RingBufferCap` — single source of truth now on `FrontComposerCommandPaletteState.RingBufferCap`; reducer + effect reference it.
- [x] [Review][Patch] Shortcut-category rows with `RouteUrl` — `HandlePaletteResultActivated` suppresses `RecentRouteVisitedAction` dispatch for `PaletteResultCategory.Shortcut`.
- [x] [Review][Patch] D4 chord sub-decision tests — added `Chord_RepeatPrefixBeforeTimeout_OverwritesPendingField` + `Chord_ModifierBindingDuringPending_FiresAndClearsPending` + post-dispose guard test.
- [x] [Review][Patch] `TextInputBareLetterChord` test — renamed to `BareChordPrefixAlone_DoesNotOpenDialogOrNavigate` + asserts NavigationManager.Uri is unchanged; updated to reflect the DN3 C# → JS guard migration.
- [x] [Review][Patch] `ShortcutBinding.Normalize` — chord branch now rejects parts containing `+`; new theory test `Normalize_RejectsChordPartsWithModifiers` covers `ctrl+g ctrl+h`, `g ctrl+h`, `ctrl+g h`.
- [x] [Review][Patch] `@onkeydown:preventDefault="ShouldPreventDefault"` added to `fc-palette-root` — backing property `ShouldPreventDefault` defaults true.
- [x] [Review][Patch] `aria-disabled` scope — tightened to `IsInformationalShortcut(result)` helper (Shortcut-category AND null/empty RouteUrl) so shortcut rows with a route are clickable and unambiguously announced.
- [x] [Review][Patch] Per-manifest try/catch — registry enumeration is outer-guarded (HFC2110 + empty dispatch); each manifest is inner-guarded so one malformed manifest no longer blanks the whole result set. Empty/null BoundedContext is skipped with `continue`.
- [x] [Review][Patch] Stale `FluentSearch` prose — `CommandPaletteActions.PaletteQueryChangedAction` XML doc, `FcPaletteResultList.Id` XML doc, and `FcShellResources.resx` comment rewritten to reference the actual `FluentTextInput TextInputType=Search` control.
- [x] [Review][Patch] `HandlePaletteHydrated` test-seam — comment rewritten to clarify the intentional "no `[EffectMethod]`, anchors `HydrateDoesNotRePersist` contract" pattern; kept in place to avoid breaking the scope test.
- [x] [Review][Patch] Reducer tests added — SelectedIndex preserve (clamp + shrink), stale-query rejection, hydrate race guard, hydrate cap, PaletteScopeChanged clear.
- [ ] [Review][Patch, PARTIAL] Missing palette bUnit tests (Task 10.7/10.7c/10.7d) — the reducer-level coverage for the behaviour is in place (selection-moved clamp, dismiss-on-close reducer side, scope-changed) but the full matrix of component-level tests (`ArrowDownDispatchesSelectionMoved`, `ArrowUpDispatchesSelectionMoved`, `EnterDispatchesActivation`, `EscapeClosesPalette`, `AriaLiveAnnouncesNoMatchesForEmptyResults`, `FocusManagement_ArrowsKeepFocusOnSearchInput`, `FocusManagement_EscapeRestoresFocusToInvoker`, `FocusManagement_ActivateSentinelDoesNotClosePalette`, `PaletteDismissPaths_AllDispatchPaletteClosedAction`) remains open. Tracked for a follow-up coverage pass after the Aspire MCP Playwright matrix lands.

- [ ] [Review][Patch] `IsInternalRoute.ContainsEmbeddedScheme` uses naïve `Contains("http:", ...)` — over-rejects legit internal URLs like `/docs/about-http-1.1` or queries containing `?ref=mailto:` [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:ContainsEmbeddedScheme` ~L2748-2753]
- [ ] [Review][Patch] `HandlePaletteResultActivated` coalesces null `CommandTypeName` → empty string → `BuildRoute`'s `ThrowIfNullOrWhiteSpace` throws. Short-circuit to return null targetUrl when `CommandTypeName` is null/empty [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` ~L3638]
- [ ] [Review][Patch] `HandlePaletteHydrated` does not cap `filtered.Length` at `RingBufferCap=5`; `ReducePaletteHydrated` wholesale-replaces without cap — a tampered storage blob can seed an arbitrarily large buffer [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` ~L3412-3427, `CommandPaletteReducers.cs:ReducePaletteHydrated` ~L4092-4099]
- [ ] [Review][Patch] Hydrate-vs-first-save race overwrites just-visited URL — add `if (!state.RecentRouteUrls.IsEmpty) return state;` guard in `ReducePaletteHydrated` (or merge/union instead of replace) [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs:ReducePaletteHydrated`]
- [ ] [Review][Patch] Missing bUnit tests per Task 10.7/10.7c/10.7d — `ArrowDownDispatchesSelectionMoved`, `ArrowUpDispatchesSelectionMoved`, `EnterDispatchesActivation`, `EscapeClosesPalette`, `AriaLiveAnnouncesNoMatchesForEmptyResults`, `FocusManagement_ArrowsKeepFocusOnSearchInput`, `FocusManagement_EscapeRestoresFocusToInvoker`, `FocusManagement_ActivateSentinelDoesNotClosePalette`, `PaletteDismissPaths_AllDispatchPaletteClosedAction` [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`]
- [ ] [Review][Patch] CSS selector targets `::deep fluent-search` but palette renders `FluentTextInput` (`fluent-text-input`) — width rule never applies [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.css` ~L1881]
- [ ] [Review][Patch] Registrar `_registered` idempotency flag is not thread-safe — use `Interlocked.Exchange(ref _registered, 1) == 1` or lock [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:~L2803-2817`]
- [ ] [Review][Patch] Chord-pending state survives focus transition to editable element (user primes `g` on shell, focuses input, presses `h` → blocked but `_pendingFirstKey` still cleared by timer) — hook `focusin` to clear pending when incoming target is editable [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` HandleGlobalKeyDown]
- [ ] [Review][Patch] `LocationChanged` handler attached in `OnAfterRenderAsync` leaks when `DisposeAsync` is skipped (aborted circuit) — wrap handler body in try/catch `ObjectDisposedException` and detach on catch [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` DisposeAsync + HandleLocationChanged]
- [ ] [Review][Patch] `BoundedContextRouteParser.Parse` preserves raw case — a PascalCase URL and a kebab-case URL for the same BC generate different dispatches; reducer uses `StringComparison.Ordinal`. Normalize to `ToLowerInvariant` in parser, or switch reducer comparison to `OrdinalIgnoreCase` [`src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs` ~L4389-4443]
- [ ] [Review][Patch] `BoundedContextRouteParser` treats any non-`/domain/` path's first segment as a bounded-context name (e.g., `/help/...` → BC `"help"`) — anchor to `segments[0] == "domain"` only [`src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs`]
- [ ] [Review][Patch] `ShortcutService.TryInvokeAsync` does not check `_disposed` at entry — add `if (_disposed) return false;` guard [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:TryInvokeAsync`]
- [ ] [Review][Patch] Debounce stale-computation race — `ReducePaletteResultsComputed` ignores `action.Query` when assigning results; a late-arriving stale scoring pass can land on a newer query's state. Add `if (!string.Equals(state.Query, action.Query, StringComparison.Ordinal)) return state;` guard [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs:ReducePaletteResultsComputed`]
- [ ] [Review][Patch] `OpenPaletteAsync` bare `catch` compensating-dispatch can throw `ObjectDisposedException` from a disposed dispatcher during circuit teardown, masking the original exception; also swallows `OperationCanceledException` from user-initiated close into a double-dispatch with `DisposeAsync`. Narrow the catch to `JSDisconnectedException` / dialog-specific exceptions; wrap compensating `Dispatch` in `try { } catch (ObjectDisposedException) { }` [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:~L2843-2866`]
- [ ] [Review][Patch] `OpenPaletteAsync` read-then-dispatch race on `IsOpen` — two concurrent `Ctrl+K` observers can both stack `ShowDialogAsync`. Move the `ShowDialogAsync` call inside an effect on `PaletteOpenedAction` (Fluxor serializes dispatches) so the registrar is fire-and-forget [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:OpenPaletteAsync`]
- [ ] [Review][Patch] `OnSelectionClickedAsync` computes `_restoreBodyFocusOnDispose` AFTER dispatch (reads `NavigationManager.Uri` after the effect may have called `NavigateTo`) — compute from the pre-dispatch URL and the result's target URL [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs` ~L1613-1642]
- [ ] [Review][Patch] `ReduceRecentRouteVisited` hard-codes cap `5` while `CommandPaletteEffects.RingBufferCap = 5` is declared elsewhere — two sources of truth. Elevate the constant to the state type or a shared static [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs` ~L4077-4082, `CommandPaletteEffects.cs:~L3290`]
- [ ] [Review][Patch] `HandlePaletteResultActivated` dispatches `RecentRouteVisitedAction` for `Shortcut`-category rows with a non-null `RouteUrl` — shortcut reference rows should never land in the recent-route ring buffer; short-circuit after navigating shortcut rows [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` ~L3635-3648]
- [ ] [Review][Patch] D4 chord-FSM sub-decision tests absent per Task 10.1d — add `Chord_RepeatPrefixBeforeTimeout_OverwritesPendingField`, `Chord_ModifierBindingDuringPending_FiresAndClears` [`tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs`]
- [ ] [Review][Patch] `TextInputBareLetterChord_DoesNotTriggerNavigation` passes regardless of guard — press `g` then `h` with `isEditableElementActive` stub returning `true`, assert no home-navigation [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs:~L291-307`]
- [ ] [Review][Patch] `ShortcutBinding.Normalize` chord branch accepts modifiers on chord parts (e.g., `"ctrl+g ctrl+h"`) — chord-dispatch path can never fire these; reject parts containing `+` in the chord branch [`src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs:Normalize`]
- [ ] [Review][Patch] Arrow/Enter/Escape `@onkeydown:preventDefault` not declared on razor — relies on `fc-keyboard.js` filter which isn't guaranteed to attach before first keystroke on prerender [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor` keydown handler]
- [ ] [Review][Patch] `aria-disabled="true"` rendered on every Shortcut-category row regardless of `RouteUrl` presence — scope to informational shortcuts only (`IsInformationalShortcut(result)`) to match click/Enter predicate [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:36`]
- [ ] [Review][Patch] Null/empty `BoundedContext` on an adopter manifest throws from `BuildRoute` in the scoring loop, caught once per-pass → `HFC2110` + empty results for the WHOLE palette. Wrap per-manifest (or per-projection) try/catch so one bad manifest doesn't blank the result set [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~L3813-3825`]
- [ ] [Review][Patch] Stale `FluentSearch` references remain in XML docs, resx comments, and razor comments after the spike-notes §3 substitution to `FluentTextInput` [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs:19`, `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx:177`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:65`]
- [ ] [Review][Patch] `HandlePaletteHydrated` method has no `[EffectMethod]` attribute yet is public — exposed solely for a scope test to call; either decorate as an effect with the proper hydrate contract OR remove and rework the test to assert "no persistence actions dispatched in response to `PaletteHydratedAction`" [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~L446-463`]

### Deferred (1)

- [x] [Review][Defer] `IsChordPrefix` linear `O(N)` per keystroke — [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:IsChordPrefix`] — deferred, premature optimization (realistic adopter N<50, well under any perceptible budget). Revisit if an adopter registers hundreds of chords.

---

### Review Findings — 2026-04-21 Pass 3 (bmad-code-review)

*Generated by bmad-code-review pass 3 on 2026-04-21 against the CURRENT diff (post-pass-2 patches). Three parallel reviewers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) run in parallel on the in-progress working tree. Raw counts: 20 Blind + ~45 Edge + 14 Auditor = ~79 → dedup → ~55 unique → triage → **7 decision-needed**, **15 patch**, **8 defer**, **~25 dismissed**.*

**Pass 3 resolution status (2026-04-21 — applied by /bmad-code-review option-1 patch run)**

- Decision-needed (7): all resolved by user selection. DN1=(a) ratify-placeholder, DN2=(b) restructure, DN3=(b) require-both-shipped, DN4=(A) ratify-all, DN5=(a) add-re-validation, DN6=(a) generation-ID, DN7=(b) payload-snapshot.
- Patches applied this pass: **P1 P2 P3 P4 P5 P6 P7 P8 P9 P10 P11 P12 P13 P14** (14 of 15) + **DN5** (re-validation landed in P5/P6 area). **P15 WITHDRAWN** after verification — Blazor omits null attributes so `aria-activedescendant=""` never renders.
- Pending from resolved DN: **DN1** XML-doc ratification + G24 entry; **DN2** multi-target restructure; **DN3** Playwright palette E2E + `BenchmarkDotNet` `PaletteScorerBench`; **DN4** critical-decisions + story-scope addenda + G25 entry; **DN6** `OpenGeneration` plumbing across state / action / reducer / effect / component / registrar; **DN7** `PaletteScopeChangedAction` payload field + reducer + effect reads.
- Build status: `dotnet build Hexalith.FrontComposer.sln --warnaserror` **clean** (0 warnings / 0 errors). Tests: **905 passing** (Contracts 14 / Shell 614 / SourceTools 277; 2 skipped E2E). One `CommandPaletteEffectsTests.HandlePaletteResultActivated_Command_NavigatesToKebabRoute` baseline required a `TestNavigationManager` DI registration now that the P6 null-navigation path early-returns instead of silently dispatching; fixture updated to register a test `NavigationManager`.
- Story cannot close until DN3 ships (user-gated per DN3=(b)).

#### Decision-needed (7)

- [ ] [Review][Decision] **DN1: D21 registry-route validator is inert** — `FrontComposerRegistry.HasFullPageRoute` returns `true` for every command present in any manifest, so `ValidateManifests()` (HFC1601) cannot actually fire on a legitimately-broken registration. The D21 promise "build never ships a broken palette entry" is scaffold-only. Edge hunter: adopter-supplied whitespace-only command names *do* throw HFC1601, so startup can surface unexpected errors. Options: **(a) ratify as documented placeholder** (update XML doc + add G24 known gap); **(b) wire a real filter** (require manifests opt into `IFrontComposerFullPageRouteRegistry` or declare route mapping explicitly); **(c) remove the scaffold entirely** (delete `HasFullPageRoute` + `ValidateManifests` + HFC1601 ID until Story 9-4). [`src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:HasFullPageRoute/ValidateManifests`]

- [ ] [Review][Decision] **DN2: `IShortcutService` hidden from `netstandard2.0` consumers** — the interface + `ShortcutBinding` are entirely wrapped in `#if NET10_0_OR_GREATER`, so Contracts NS2.0 adopters cannot see or register shortcuts. Prior pass dismissed as intentional per `KeyboardEventArgs` dependency, but only `TryInvokeAsync(KeyboardEventArgs)` actually needs net10 — `Register`/`GetRegistrations`/`ShortcutRegistration` could be always-visible. Options: **(a) accept deviation, update story scope line 19 + add G25**; **(b) restructure to expose Register/GetRegistrations unconditionally** (move `TryInvokeAsync` to a net10-only partial or extension); **(c) keep as-is and live with NS2.0 adopters being unable to register shortcuts**.

- [ ] [Review][Decision] **DN3: Spec success-metric items unshipped — Playwright palette E2E + `BenchmarkDotNet PaletteScorerBench`** — spec Success Metric §line 62 requires a hardened Playwright palette E2E test AND a BDN micro-bench proving <100 μs/candidate on 1000 candidates. Dev-agent-record already tracks NFR5 inspection-only as a new G23 without spec authorisation; Playwright E2E has no known-gap entry. Memory `feedback_no_manual_validation.md` prefers Aspire MCP + Claude browser automation. Options: **(a) ratify G23 + add G24 (Playwright→Aspire MCP deferred to Story 10-2)**; **(b) require both shipped before closing this story**; **(c) ship an Aspire MCP + Claude-browser automated palette verification now in lieu of Playwright, keep BDN as G23**.

- [ ] [Review][Decision] **DN4: Four un-ratified spec deviations in a single bucket** — all appear beneficial but not authorised in Critical Decisions:
  1. **Dialog host**: `FcCommandPalette` uses `FluentDialogBody` instead of `IDialogContentComponent` (spec §scope line 33). Dev-agent-record notes the `FluentSearch`→`FluentTextInput` swap but not this one.
  2. **Mac parity**: registrar ships `meta+k` and `meta+,` in addition to the `ctrl+*` bindings; D1/D14 authorise only 3 descriptions.
  3. **`BoundedContextChangedAction` + `BoundedContextRouteParser`** + shell `LocationChanged` wiring added to populate `NavigationState.CurrentBoundedContext`. Story task 2.1a asked only for the field; the reducer-action + parser + wiring exceed spec. Dev-agent-record notes "route-watching wiring is a Story 3-6 concern" while shipping it anyway.
  4. **`PaletteScopeChangedAction`** (DN2 of prior pass) — reducer clears RecentRouteUrls; no consumer wires it inside this story.

   Options: **(A) ratify all four in a post-implementation addendum** to `critical-decisions-read-first-do-not-revisit.md` and `story.md:scope` (preferred by memory L01–L11 lessons — explicit cross-story contracts); **(B) revert each to spec-exact** (lose Mac parity, defer route parser to Story 3-6, revert dialog host); **(C) ratify some, revert others** (list which).

- [ ] [Review][Decision] **DN5: Recent-route URLs not re-validated at activation time** — `IsInternalRoute` filter runs only at hydrate; `HandlePaletteResultActivated` calls `NavigationManager.NavigateTo(result.RouteUrl)` without re-checking. Any future code path inserting directly into state (bypassing hydrate) would bypass the open-redirect defence. Not exploitable today. Options: **(a) add re-validation in `HandlePaletteResultActivated` for Category=Recent rows (defense-in-depth)**; **(b) accept — `NavigationManager` is the trust boundary**; **(c) document the single-entry-point invariant**. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated`]

- [ ] [Review][Decision] **DN6: `FcCommandPalette.DisposeAsync` can close the *next* dialog instance** — if a sentinel (`@shortcuts`) activation keeps the palette open, the user navigates (e.g., Ctrl+K → re-open → earlier instance disposes *after* the new one mounts), the stale `DisposeAsync` dispatches `PaletteClosedAction` which resets the new dialog's state. `_explicitlyClosed` is component-scoped; there is no component-generation invariant. Options: **(a) track a component generation ID in state, ignore dispose from non-active generation**; **(b) only dispatch close if `PaletteState.IsOpen == true && _invokerMatchesActiveDialog`**; **(c) show that the race is not reachable in practice via ordering argument + test**.

- [ ] [Review][Decision] **DN7: `PaletteScopeChangedAction` race vs. `IUserContextAccessor` flip + UX when palette is open** — `HandlePaletteScopeChanged` directly calls `HandleAppInitialized`, which pulls tenant/user lazily from the accessor. If an adopter dispatches the action before the accessor atomically updates (e.g., OIDC sign-in completing asynchronously), hydrate re-reads the OLD scope. Secondary: if the scope changes *while the palette is open*, `RecentRouteUrls` vanishes and new-scope recents pop in without UX mediation. Options: **(a) document invariant "dispatch only AFTER accessor update; adopters bear responsibility"**; **(b) snapshot tenant/user inside the action payload** and pass through effect instead of lazy-read; **(c) auto-close palette on `PaletteScopeChangedAction` so the stale-results window is invisible**. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteScopeChanged`]

#### Patch (15)

- [x] [Review][Patch] **P1: `FcCommandPalette.DisposeAsync` catches `ObjectDisposedException` only** — Fluxor store disposal can surface as `InvalidOperationException("Store has been disposed")` on dispatch. `FrontComposerShell.HandleLocationChanged` already guards against both (see `FrontComposerShell.razor.cs:~324-338`). Add the same `InvalidOperationException` catch. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:121`]

- [x] [Review][Patch] **P2: Palette root unconditionally `preventDefault`s every key, including Tab** — `ShouldPreventDefault` is always `true` combined with `@onkeydown:preventDefault="ShouldPreventDefault"` on `.fc-palette-root`. Tab cannot move focus, breaking shell focus-loop discipline. Make `ShouldPreventDefault(e)` per-key (return `true` only for ArrowUp/ArrowDown/Enter/Escape). [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor` + `.razor.cs:ShouldPreventDefault`]

- [x] [Review][Patch] **P3: `GC.SuppressFinalize(this)` on a class without a finalizer** — no-op but misleading. Remove the call in `DisposeAsync`. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:143`]

- [x] [Review][Patch] **P4: `ShortcutService.Register` uses `TryGetValue` + `_entries[normalised] = entry` (non-atomic)** — two concurrent `Register` calls on the same binding can lose one update without logging HFC2108. Replace with `ConcurrentDictionary.AddOrUpdate` so duplicate-detection is atomic. [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:Register`]

- [x] [Review][Patch] **P5: `CommandPaletteEffects` uses `IServiceProvider.GetService<T>()` per dispatch (registry, storage, navigation, TimeProvider)** — on circuit teardown the scoped provider is disposed mid-effect; `GetService` throws `ObjectDisposedException` and the per-manifest catch does NOT wrap the fetches. Wrap each `GetService` call (or the whole effect body) in `try { } catch (ObjectDisposedException) { return; }`. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteQueryChanged/HandleAppInitialized/HandlePaletteResultActivated`]

- [x] [Review][Patch] **P6: `HandlePaletteResultActivated` silently drops navigation when `NavigationManager` is null; does not guard `NavigateTo` throws** — (1) null manager → palette closes but nothing navigates, no log. Add an HFC diagnostic or throw-if-null contract. (2) synchronous throw from `NavigateTo` (force-load + hostile URL) escapes to Fluxor error boundary and skips `RecentRouteVisitedAction`; the component's stale `DisposeAsync` then fires a second close. Wrap `NavigateTo` in `try { } catch { Dispatch(RecentRouteVisitedAction(null)); rethrow; }`. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:HandlePaletteResultActivated`]

- [x] [Review][Patch] **P7: Click + Enter handlers have split-read TOCTOU on `PaletteState.Value.Results`** — `OnSelectionClickedAsync` reads `Results[flatIndex]` after a bounds check; the debounced effect can replace `Results` between the two reads, so the clicked row differs from the activated row. Same race on Enter. Snapshot `var results = PaletteState.Value.Results;` once at the top of each handler and use that reference only. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:OnSelectionClickedAsync/HandleKeyDownAsync`]

- [x] [Review][Patch] **P8: `FcCommandPalette.FocusSearchAsync` catches bare `Exception`** — swallows programming errors. Narrow to `JSException` (Blazor Server) / `Microsoft.JSInterop.JSDisconnectedException`. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:FocusSearchAsync`]

- [x] [Review][Patch] **P9: `fc-keyboard.js:registerShellKeyFilter` has no paired `unregister…`** — persistent listener on `_shellRoot`; on circuit reconnect / hot-reload the element may change and stale handlers accumulate. Export `unregisterShellKeyFilter(element)` that removes the handler + nulls the `__fcShellKeyFilter` marker; call it from `FrontComposerShell.DisposeAsync` via JS interop. [`src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js`]

- [x] [Review][Patch] **P10: Missing `AnalyzerReleases.Unshipped.md` in Shell project** — story scope line 43 + dev-notes §Build&CI require a shipped row for `HFC2108_ShortcutConflict`, `HFC2109_ShortcutHandlerFault`, `HFC2110_PaletteScoringFault`, `HFC2111_PaletteHydratePayloadInvalid`, and `HFC1601_ManifestInvalid`. No such file exists in `src/Hexalith.FrontComposer.Shell/`. Story 1-8 G2 discipline unmet. Create the file with one row per diagnostic (ID + severity + category + notes). [`src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` (new)]

- [x] [Review][Patch] **P11: Missing `AdopterSuppliedHeaderEndSuppressesPaletteTrigger` test** — spec D18 + scope line 35 says adopter-supplied non-null `HeaderEnd` suppresses BOTH auto-populated buttons. `FrontComposerShellTests` covers only the settings-button suppression path; add the palette-trigger mirror. [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`]

- [x] [Review][Patch] **P12: `aria-controls="fc-palette-results"` hard-coded** — if an adopter ever overrides `FcPaletteResultList.Id`, the combobox points at a non-existent id. Bind `aria-controls="@_resultListId"` from a backing field (default `"fc-palette-results"`) that is also passed down as `FcPaletteResultList.Id`. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor:21`]

- [x] [Review][Patch] **P13: `CommandPaletteEffects` resolves `TimeProvider` via `IServiceProvider.GetService` per dispatch** — tests that register `FakeTimeProvider` on Fluxor's internal scope vs. DI container can hit an inconsistent clock source. Cache `_timeProvider = _serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System` once in the constructor; drop the `Time` property. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:Time`]

- [x] [Review][Patch] **P14: `ReduceRecentRouteVisited` dedupe comparer is `StringComparer.Ordinal`** — `/Counter` and `/counter` are treated as distinct entries; ring buffer fills with near-duplicates in a case-permissive routing setup (Blazor is default case-insensitive). Switch to `StringComparer.OrdinalIgnoreCase` for the `Remove` comparison, OR normalise the URL via `ToLowerInvariant` on insert. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs:ReduceRecentRouteVisited`]

- [~] [Review][Patch] **P15: `aria-activedescendant=""` rendered when `Results` is empty** — WITHDRAWN after verification. `ActiveDescendantId` returns `null` when out-of-range; Blazor omits attributes with null expression values, so the attribute is never emitted as an empty string. False positive. — empty string IS a dangling reference per WAI-ARIA; some AT engines flag it. Omit the attribute entirely when `SelectedIndex >= Results.Length` (or when `Results.IsEmpty`). [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor:21` / `FcPaletteResultList.razor`]

#### Deferred (8)

- [x] [Review][Defer] **F-03: Palette bUnit test matrix short (5 of 9)** — `ArrowDown/Up/Enter/Escape/FocusManagement_*/PaletteDismissPaths_*` tests still missing; already tracked as PARTIAL in line 152 above. Carried forward to the Aspire MCP verification follow-up; do not re-raise until that lands.
- [x] [Review][Defer] **F-14: `PaletteResultCountTemplate` is not plural-aware** — "{0} results" renders as "1 results" when N=1. v1.x a11y polish; file under follow-up resource-revision.
- [x] [Review][Defer] **Turkish `İ` → combining marks via `ToLowerInvariant`** — real culture-fragility for non-ASCII key registrations. Story 3-4 ships ASCII bindings only; spec scope explicitly defers fuzzy-scoring i18n to v2. Same concern applies to `ShortcutBinding.Normalize`; track with the v2 i18n rolodex.
- [x] [Review][Defer] **Chord fallthrough re-evaluation test** — `ShortcutService` has a branch where the second key of a broken chord falls through to the `IsChordPrefix` fresh-start path; no test proves the behaviour. Coverage gap; add in the Task 10.1 follow-up that ships with the FocusManagement tests.
- [x] [Review][Defer] **Modifier-bearing chord second-key test** (`g` then `Ctrl+Z` clears pending) — same follow-up bucket.
- [x] [Review][Defer] **Missing `descriptionKey` resource fallback** — `IShortcutService.Register` accepts any string; if the key is missing, the shortcut reference view renders the raw key. Add a v1.x validation (resource-key existence check or a warning HFC diagnostic on Register).
- [x] [Review][Defer] **`ProjectionTypeResolver` negative-lookup cache / throttling** — `AppDomain.CurrentDomain.GetAssemblies()` is scanned per cache-miss. Fine at current scale; revisit when adopter profile shows palette perf regression in large solutions.
- [x] [Review][Defer] **Registrar/`ShortcutService` scoped test-fixture reuse hygiene** — bUnit fixtures reusing the same Services container can see stale shortcut registrations across tests. Test-infrastructure concern; fix via per-test scope guidance in `LayoutComponentTestBase` when we revisit Shell.Tests fixtures.

#### Dismissed (~25)

Each of the following was raised by one or more reviewers and dropped after verification. Recorded so future passes don't re-litigate:
- `IsInternalRoute` substring-check over-rejecting "/docs/how-http-works" — already withdrawn in prior pass; the example lacks `:` so does NOT match; intentional open-redirect defence [see line 127 above].
- `Task.Delay` resolves scoring path even after CTS cancel — reducer's stale-query guard correctly drops; wasted CPU is acceptable per D9.
- `HandlePaletteResultActivated` dispatches `PaletteClosedAction` before `NavigateTo`, `RecentRouteVisitedAction` after — intentional per D11 inline comment; teardown case acknowledged in code.
- Fluxor effect instance vs DI-scoped instance — resolved in prior pass via `TryAddScoped` + Fluxor scan.
- Double-dispatch of `AppInitializedAction` — reducer guards; benign.
- Clock-skew / NTP-correction on chord timer — acceptable via `TimeProvider` abstraction.
- `_chordTimer` allocation under `_chordSync` — normal CLR practice.
- Razor closing-tag indentation question — manual inspection confirms correct nesting.
- Hot-reload subscription catch-too-narrow (BadgeCountObserver) — dev-only scenario.
- "`e.Repeat` filter blocks legitimate auto-repeated shortcuts" — no repeat-supporting binding ships in v1; benign constraint.
- `HandlePaletteOpened` dispatches default results after reducer sets `IsOpen=true` — Fluxor ordering guaranteed.
- `PaletteResultsComputedAction` with `Results.Length > 50` bypassing effect cap — internal-only dispatch; trust.
- Null-char key `"\0"` — not reachable; no registration accepts it.
- `AppDomain.GetAssemblies()` perf under trimming / plugin load — out of scope for Blazor Server v1.
- Several test-count / naming-conformance complaints (e.g., discrete `HFC2108ShortcutConflictLogTest` file absent) — coverage folded into `ShortcutServiceTests` which is adequate.
- Success-metric test-count overshoot (+93 vs ~49 target) — more tests > fewer; not a regression.
- `IShortcutService.Register` concurrent race on duplicate — narrow to be worth calling out but the patch P4 covers it directly; the race as a separate finding is redundant.
- BoundedContextRouteParser file:/// edge — Blazor Server never sees these.
- Stale closure of `owner` in `BadgeCountObserver` — standard Blazor hook idiom.
- `PaletteScorer` gaps-boundary near-identical ranking — no test failure cited; no concrete reproduction.
- `IsChordPrefix` O(N) — duplicate of the 2026-04-20 defer entry (line 183).
- `BoundedContextRouteParser` path-extraction for non-`/domain/*` paths — prior pass anchored the parser to `segments[0] == "domain"`; dismissal ratified.

### Review Findings — 2026-04-21 Pass 4 (bmad-code-review — Chunk 3: Shortcuts + Navigation + Registry)

*Focused re-review before DN3 implementation. Chunk 3 of 3 (out-of-DN3 residue): Contracts/Shortcuts, Contracts/Registration, FcDiagnosticIds, Shell/Shortcuts, Shell/Registration, Shell/Routing, Shell/State/Navigation + their tests. Palette UI + scorer + main effects deferred to Chunks 1 & 2 (future sessions, after PaletteScorerBench ships).*

*Diff: `c59b204~1..c59b204` narrowed via pathspec → 20 files, +1752 / −2 lines. Three parallel reviewers: Blind Hunter (diff-only), Edge Case Hunter (diff + project read), Acceptance Auditor (diff + spec + context docs). Raw counts: 19 + 40 + 12 = 71 → dedup → ~48 unique → triage → 3 decision-needed, 21 patch, 12 defer, 12 dismissed.*

#### Decision-needed (3 resolved on 2026-04-21 — all ratify-and-document)

- [x] [Review][Decision] **C3-D1: Diagnostic ID sprawl vs spec additive-reason-code contract** — `FcDiagnosticIds` ships `HFC2109_ShortcutHandlerFault` (new), `HFC2110_PaletteScoringFault` (renumbered from spec §scope line 43's second-clashing `HFC2109`), and `HFC2111_PaletteHydrationEmpty` (brand-new). But spec §scope line 43 explicitly forbids a new ID for palette-hydrate: *"existing `HFC2106` diagnostic gains a NEW reason code `Reason=Tampered` … no new diagnostic ID, additive reason-code extension."* The 2109→2110 renumber resolves a clear spec typo (two diagnostics can't share an ID); sprint-status already ratified similar renumbers (HFC2107→HFC2108). The **new** issue is HFC2111 vs the HFC2106 additive model. Options: **(a) ratify all renumbers (2110, 2111) in a single critical-decisions D27 addendum + G26 row** — fastest; **(b) refactor `HFC2111_PaletteHydrationEmpty` → `HFC2106` + new `Reason=Empty`/`Corrupt`/`Tampered` codes per spec D10** — aligns with story §scope line 43 but adds a shell refactor; **(c) keep HFC2111, refactor only its `<remarks>` + hydrate-empty log call-site to format `Reason={Empty|Corrupt|Tampered}` so at least the reason-code payload matches spec intent** — partial alignment.

  **Resolved: (a) ratify.** HFC2111 is semantically cleaner than coupling palette-specific reasons to HFC2106's storage-service diagnostic; shipped code is defensible. Patches P15+P16+P17 grow to include a new D27 addendum entry covering both the 2109→2110 renumber (spec-typo correction) and the HFC2111 allocation (deviation from spec §scope line 43's additive-reason-code model). No code change; doc-only ratification.

- [x] [Review][Decision] **C3-D2: `BoundedContextRouteParser` first-segment fallback — ratify lenient or tighten?** — `Parse` currently returns `segments[0].ToLowerInvariant()` for any non-`/domain/*` URL with ≥2 segments, so `/help/topic` → BC `"help"`, `/admin/users` → `"admin"`. The Pass-2 edge-hunter patch at `dev-agent-record.md:164` prescribed *"anchor to `segments[0] == "domain"` only"*; Pass-3 "dismissal ratified" this but the shipped code still has the lenient fallback — implementation and the pass-3 note are in tension. `BoundedContextRouteParserTests.cs:1917-1925` codifies the lenient behaviour (`/counter/counter-view` → `"counter"`). Options: **(a) ratify lenient fallback** (add a Pass-3 decision entry explaining adopter-owned BC routes like `/counter/*` are a feature, extend tests with `/admin/users` → `"admin"` coverage); **(b) tighten to `/domain/` anchor only** (patch `BoundedContextRouteParser.Parse` to return `null` when `segments[0] != "domain"`, update 1 test case, lose the `/counter/counter-view` → `"counter"` behaviour). Resolution drives patch P21.

  **Resolved: (a) ratify lenient.** Matches shipped code + `BoundedContextRouteParserTests.cs:1917-1925` + the Counter-sample convention where `/counter/counter-view` → BC `"counter"` is a feature. Pass-3 already implicitly accepted this via its dismissal. Patches grow to include a new D28 addendum entry documenting the convention; patch P21 becomes "add `/admin/users` → `"admin"` and `/help/topic` → `"help"` theory-data asserting the lenient behaviour is stable."

- [x] [Review][Decision] **C3-D3: Registrar discards 5 `IDisposable` handles from `IShortcutService.Register`** — `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync` calls `_ = shortcuts.Register(...)` five times and stores none of the returned handles (`FrontComposerShortcutRegistrar.cs:723-744`). `IShortcutService.Register` explicitly returns `IDisposable` *"— disposing unregisters"* (spec §scope line 19). Works today because `ShortcutService.Dispose` clears `_entries` at circuit teardown, but: adopter hot-reload re-registering the shell defaults cannot remove originals first; shared-service test scenarios leak across registrar instances; public-API footgun (returned `IDisposable` is silently dropped). Options: **(a) ratify the discard pattern** — add XML-doc on `FrontComposerShortcutRegistrar` disclosing "handles intentionally discarded; registrations are cleaned up by `ShortcutService.Dispose`" + G26 row; **(b) track in a `CompositeDisposable`** — registrar grows an `IDisposable` field that holds all five, `IDisposable` on the registrar itself disposes them (requires registrar to become disposable, which shifts its DI shape); **(c) short-circuit duplicate registration** — `Register` call-site checks `GetRegistrations().Any(r => r.Binding == binding)` before registering, avoiding the cleanup question entirely (trades the IDisposable contract for an idempotency-at-call-site contract).

  **Resolved: (a) ratify discard pattern.** Registrar's `_registered` + `Interlocked.Exchange` already idempotency-guards re-entry; `ShortcutService.Dispose` clears `_entries` on circuit teardown. (b) would force the registrar to become `IDisposable` and shift DI shape; not worth the churn for a v1 invariant that already works. Hot-reload is a v2 concern. Patches grow to include a new D29 addendum entry + XML-doc disclosure on `FrontComposerShortcutRegistrar` stating *"handles from `IShortcutService.Register` are intentionally discarded; cleanup happens via `ShortcutService.Dispose` at circuit teardown"*.

#### Patch (21 original + 3 from decision resolution = 24)

**Safety / correctness:**

- [ ] [Review][Patch] `ShortcutService._disposed` non-atomic double-dispose race [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:_disposed` field + `Dispose`] — plain `bool`; two concurrent `Dispose` calls can both observe `false` and both execute `DisposeTimerLocked`. Convert to `int` + `Interlocked.CompareExchange`, matching the `RegistrationDisposable` pattern already in the same file.
- [ ] [Review][Patch] `RegisterShellDefaultsAsync` exception leaves `_registered==1` permanently [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:RegisterShellDefaultsAsync` ~L716-750] — if any `Register` call throws (rare but possible for localizer lookups, FcSettingsDialogLauncher init), the `Interlocked.Exchange(ref _registered, 1)` is not rolled back, so retry is impossible and subsequent invocations no-op silently. Wrap in `try { … } catch { Interlocked.Exchange(ref _registered, 0); throw; }`.
- [ ] [Review][Patch] `TryInvokeBindingAsync` NRE if handler returns `null` Task [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:TryInvokeBindingAsync`] — `Func<Task>` can return `null`; awaiting `null` throws NRE that bypasses the HFC2109 handler-fault contract. Add `Task t = entry.Handler(); if (t is null) return true;` guard.
- [ ] [Review][Patch] `OpenPaletteAsync` OCE rethrown with no compensating dispatch [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:OpenPaletteAsync` ~L763-790] — the narrowed catch excludes `OperationCanceledException`; on OCE the `PaletteOpenedAction` has been dispatched but no `PaletteClosedAction` compensates, leaving `IsOpen==true` if the dialog never actually mounts. Add `catch (OperationCanceledException) { try { dispatcher.Dispatch(new PaletteClosedAction(correlationId)); } catch (ObjectDisposedException) { } throw; }`.
- [ ] [Review][Patch] `OpenPaletteAsync` synchronous throw from `ulidFactory.NewUlid()` or `dialogService.ShowDialogAsync` construction escapes outer try [`FrontComposerShortcutRegistrar.cs:OpenPaletteAsync` ~L766-775] — the try wraps only `await`; a synchronous throw before `await` leaves `_palettePending==1` via `finally` but also skips the compensating dispatch. Move `ulidFactory.NewUlid()` and `ShowDialogAsync` call-construction inside the try.
- [ ] [Review][Patch] `ValidateManifests` NRE on null `manifest.Commands` [`src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:ValidateManifests`] — spec does not mandate `Commands` be non-null on `AdopterManifest`. Add `if (manifest.Commands is null) continue;` before the inner `foreach`.
- [ ] [Review][Patch] `CommandRouteBuilder.IsInternalRoute` encoded-scheme bypass [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:IsInternalRoute` + `ContainsEmbeddedScheme`] — `/redirect?next=%68ttps://evil.com` (`%68` = `h`) contains no literal `http:` substring and passes the filter. Apply `Uri.UnescapeDataString(url)` before scheme-scanning.
- [ ] [Review][Patch] `CommandRouteBuilder.IsInternalRoute` allows CR/LF/tab (header-injection vector) [`CommandRouteBuilder.cs:IsInternalRoute`] — a recent-route URL containing `"\r\nSet-Cookie: …"` survives the filter and is later passed to `NavigationManager.NavigateTo` where it can be logged/serialized. Add `if (url.AsSpan().IndexOfAny('\r', '\n', '\t') >= 0) return false;`.

**`ShortcutBinding.Normalize` invariant hardening (invariants XML-doc claims but code doesn't enforce):**

- [ ] [Review][Patch] Reject duplicate modifiers (`ctrl+ctrl+k`) [`src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs:Normalize` ~L386-414] — `hasModifier[modIndex] = true` is idempotent; duplicates silently collapse. Detect and throw.
- [ ] [Review][Patch] Reject empty-token splits (`++k`, `ctrl++k`, `+k`) [`ShortcutBinding.cs:Normalize`] — current `Split('+', StringSplitOptions.RemoveEmptyEntries)` launders author typos into valid bindings. Replace with explicit empty-token rejection.
- [ ] [Review][Patch] Reject multi-char chord tokens (`gg hh`) [`ShortcutBinding.cs:Normalize` chord branch ~L260-285] — dispatcher only compares single-char lowercase keys; multi-char chord registrations can never match a `KeyboardEventArgs.Key`. Enforce `parts[0].Length == 1 && parts[1].Length == 1`.
- [ ] [Review][Patch] Reject identical chord parts (`g g`) [`ShortcutBinding.cs:Normalize` chord branch] — self-chord ambiguous with rapid double-press / auto-repeat. Enforce `parts[0] != parts[1]`.
- [ ] [Review][Patch] Tighten chord whitespace to "exactly one space" per XML-doc [`ShortcutBinding.cs:Normalize` chord branch] — current code accepts `"g   h"` (multiple spaces) via split-trim; XML-doc + `Normalize_AcceptsBareChordWithExactSingleSpace` test name both promise "exactly one space". Enforce `binding.Trim().Contains(' ') && !binding.Trim().Contains("  ")` or use regex.
- [ ] [Review][Patch] `TryFromKeyboardEvent` accepts named multi-char keys that cannot be registered [`ShortcutBinding.cs:TryFromKeyboardEvent` ~L320-365] — `e.Key == "Enter"` lowercases to `"enter"` and the method returns `true` with a 5-char binding, but `Normalize` throws on any non-length-1 key token, so no registration can ever match. Reject named keys early.

**Docs / bookkeeping (DN1 + DN4 pending completion):**

- [ ] [Review][Patch] Add G24 row: HFC1601 validator is scaffold-only until Story 9-4 [`_bmad-output/implementation-artifacts/3-4-fccommandpalette-and-keyboard-shortcuts/known-gaps-explicit-not-bugs.md`] — DN1(a) ratified on 2026-04-21 pass-3 but the G24 known-gap entry was marked pending and never shipped; XML-doc half landed, G-row half did not.
- [ ] [Review][Patch] Add G25 row: DN4-ratified Mac parity + BoundedContext route-watching wiring [`known-gaps-explicit-not-bugs.md`] — pointer to critical-decisions addendum D25+D26.
- [ ] [Review][Patch] Add critical-decisions addendum D25 (Mac parity `meta+k`/`meta+,`) and D26 (`BoundedContextChangedAction` + `BoundedContextRouteParser` + shell `LocationChanged` wiring) [`_bmad-output/implementation-artifacts/3-4-fccommandpalette-and-keyboard-shortcuts/critical-decisions-read-first-do-not-revisit.md`] — DN4(A) ratified on pass-3 but the critical-decisions addendum was marked pending; per memory L01 discipline these cross-story contracts must be explicit in the decisions doc.
- [ ] [Review][Patch] Story §scope line 22 amendment: include `meta+k`/`meta+,` shell defaults [`story.md:22`] — current text reads *"three v1 shell shortcuts"*; actual ship is five (`ctrl+k`, `ctrl+,`, `g h`, `meta+k`, `meta+,`). Update inline + reference D25.

**Tests:**

- [ ] [Review][Patch] Test `RegisterShellDefaultsAsync_RegistersThreeShellBindings` asserts only 3 but production registers 5 [`tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/FrontComposerShortcutRegistrarTests.cs` ~L1387-1397] — the test name lies and does not verify `meta+k`/`meta+,` registrations, so a future refactor could drop Mac parity silently. Rename to `_RegistersFiveShellBindings` + add assertions for the two `meta+` bindings.
- [ ] [Review][Patch] `Chord_GH_FiresWhenSecondKeyArrivesAt1499ms` proves only sync dispatch, not timer-boundary respect [`tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs:1666-1678`] — the test would pass even if the chord timeout were 1ms, because the timer callback never runs in-test at 1499ms and the dispatcher looks up `"g h"` synchronously. Either add an assertion that `_chordTimer` is still active at 1499ms (via reflection or exposed testing-only API) or re-shape as a parameterized theory that varies both the advance-time and the configured timeout.
- [ ] [Review][Patch, depends on C3-D2] `BoundedContextRouteParserTests` coverage gap for non-`/domain/` non-known paths [`tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/BoundedContextRouteParserTests.cs`] — add `/admin/users` and `/help/topic` theory data asserting the resolution direction chosen in C3-D2 (either `"admin"`/`"help"` if ratified lenient, or `null` if tightened).

#### Deferred (12)

- [x] [Review][Defer] `IsChordPrefix` linear O(N) scan per bare-letter keystroke — v1 registrar has 5 bindings; optimise to prefix-HashSet maintained at `Register`/`Remove` if N grows.
- [x] [Review][Defer] `IsChordPrefix` concurrent `_entries` modification — `ConcurrentDictionary.Keys` snapshot is safe; no throw.
- [x] [Review][Defer] `_pendingGeneration` long overflow — 2^63 chord starts; theoretical.
- [x] [Review][Defer] `NavigateHomeAsync` `JSDisconnectedException` — Blazor error boundary + HFC2109 handler-fault catch already cover.
- [x] [Review][Defer] `OpenSettingsAsync` synchronous throw — FcSettingsDialogLauncher internal wrapping covers the common cases.
- [x] [Review][Defer] `HasFullPageRoute` duplicate-command ambiguity across manifests — Story 9-4 build-time analyzer concern.
- [x] [Review][Defer] `BoundedContextRouteParser` protocol-relative `//evil/x/y` — NavigationManager would not resolve; v1 out of scope.
- [x] [Review][Defer] `BoundedContextRouteParser` encoded-slash `%2F` — low-likelihood in practice; Story 9-4.
- [x] [Review][Defer] `BoundedContextRouteParser` Turkish-I casing — v1 BCs are ASCII only.
- [x] [Review][Defer] `CommandRouteBuilder.KebabCase` Turkish-I / non-ASCII / embedded-whitespace — C# PascalCase type names guarantee ASCII letters only by language rules.
- [x] [Review][Defer] `IsInternalRoute` unicode path separators (U+2044 fullwidth slash) — v1 out of scope; Story 9-4.
- [x] [Review][Defer] `BuildRoute` URL-reserved characters in PascalCase input — impossible per C# identifier rules.

#### Dismissed (12)

- BH "`Interlocked.Exchange` inverted branches" — misread; branches are correct. The real issue (no rollback on throw) is patch P2 above.
- BH "`TryFromKeyboardEvent` dead-key combining marks" — browser DOM normalizes `e.Key` before dispatch.
- BH "`OpenPaletteAsync` TOCTOU `IsOpen` vs `ShowDialogAsync`" — `_palettePending` `Interlocked.Exchange` already serialises read-and-dispatch (patch from pass-3 line 139).
- BH "`RegistrationDisposable.Dispose` on disposed service re-probes `_entries`" — benign `TryRemove` on cleared dict; lifecycle contract clarification not a defect.
- BH "chord timer inside lock" — `TimeProvider.CreateTimer` does not synchronously invoke callback; no deadlock path.
- BH "chord FSM drops second key without re-evaluating" — bare letters cannot match single-key bindings per `Normalize` rule; falling through to `IsChordPrefix(binding)` is by design.
- BH "`FrontComposerRegistryExtensions.HasFullPageRoute` default obscures false-negatives" — the permissive fallback is the documented companion-interface opt-in contract (D21).
- Auditor F3 "`FrontComposerShortcutRegistrar` not Scoped-registered" — `ServiceCollectionExtensions.cs` is out of chunk 3; flag carried forward to Chunk 1/2.
- Auditor F5 `IsInternalRoute` D10 compliance — no finding; confirms match.
- Auditor F7 `BareChordPrefixAlone` — shell-level test, out of chunk 3.
- Auditor F8 constructor params match spec — no finding.
- Auditor F10 + F11 — confirmations, no finding.

#### Applied — 2026-04-21 pass 4 summary

*Applied and verified same-day with `dotnet build Hexalith.FrontComposer.sln --warnaserror` clean (0/0) and `dotnet test` passing (924 passed / 0 failed / 2 skipped — 14 Contracts + 633 Shell + 277 SourceTools; up from 905 pre-patch due to 19 new tests).*

- **Doc patches (4)**: G24/G25/G26 rows added to `known-gaps-explicit-not-bugs.md`; D25/D26/D27/D28/D29 addenda added to `critical-decisions-read-first-do-not-revisit.md`; `story.md:11` amended from *"three v1 shell shortcuts"* to *"five v1 shell shortcuts (including Mac-parity `⌘+K` and `⌘+,` per D25 addendum)"*.
- **Safety patches (8)**: P1 `ShortcutService._disposed` bool → int + `Volatile.Read` / `Interlocked.Exchange`; P2 `RegisterShellDefaultsAsync` try/catch with `Interlocked.Exchange(ref _registered, 0)` rollback on throw (+new test `RegisterShellDefaultsAsync_RollsBackIdempotencyFlagOnFailure_SoRetryCanSucceed`); P3 `TryInvokeBindingAsync` null-Task guard; P4 `OpenPaletteAsync` catches ALL exceptions (incl. OCE) and dispatches compensating `PaletteClosedAction` (P4+P5 combined); P5 `dispatcher.Dispatch(new PaletteOpenedAction(...))` moved inside the try so sync throws from `NewUlid`/`Dispatch` are caught; P6 `ValidateManifests` `if (manifest.Commands is null) continue` guard; P7 `CommandRouteBuilder.IsInternalRoute` URL-decodes via `Uri.UnescapeDataString` before scheme filtering + rejects on `UriFormatException`; P8 `IsInternalRoute` rejects CR/LF/tab pre-decode and post-decode (belt+braces).
- **Normalize hardening (5)**: P9 reject duplicate modifiers (`ctrl+ctrl+k`); P10 reject empty tokens from `+` splits (`++k`, `+k`, `ctrl++k`, `ctrl+`); P11 reject multi-char chord tokens (`gg hh`, `go home`); P12 reject identical chord parts (`g g`, `a a`); P13 reject multi-space chord separators (`g  h`, `g   h`). All five backed by new `[Theory]` tests in `ShortcutBindingNormalizeTests`. Existing `Normalize_AcceptsBareChordWithExactSingleSpace` theory-data updated (removed `"  g   h  "` case which no longer normalises to `"g h"`; kept `"  g h  "` for leading/trailing-whitespace tolerance).
- **Test patches (3)**: P19 `RegisterShellDefaultsAsync_RegistersThreeShellBindings` renamed to `_RegistersFiveShellBindings` + assertions extended to cover `meta+k` and `meta+,`; idempotency test likewise expanded; P20 new `Chord_GH_DoesNotFireExactlyAt1500ms` test locks boundary inclusive/exclusive semantics (paired with existing 1499/1501 tests); P21 `BoundedContextRouteParserTests` extended with `/admin/users`, `/help/topic`, `/Counter/Counter-View`, `/DOMAIN/Commerce/Submit-Order-Command` inline-data asserting the D28 lenient-fallback contract.
- **P14 dismissed after verification**: TryFromKeyboardEvent reject named multi-char keys — re-read of `NormalizeSingleKey` confirms the single-letter-bare rejection only fires on `lower.Length == 1 && char.IsLetter(lower[0])`. Multi-char named keys like `"enter"`, `"escape"`, `"arrowup"` pass through Normalize cleanly and CAN be registered. Edge-Hunter's stated consequence (*"bindings accepted but cannot be registered"*) contradicts the actual Normalize contract — dismissal recorded.

Story status stays `review` — DN3 (Playwright palette E2E + `PaletteScorerBench` BenchmarkDotNet) remains user-gated per pass-3 sprint-status line 197. Chunks 1 & 2 review still pending.

#### Notes for Chunks 1 & 2

- **Shell `@onkeydown` → `IShortcutService.TryInvokeAsync` rewire + Story 3-3 D16 `Ctrl+,` retirement** — verify in Chunk 2 when `FrontComposerShell.razor` is in scope.
- **`TryAddScoped<IShortcutService>` DI registration + `AddScoped<FrontComposerShortcutRegistrar>()`** — verify in Chunk 2 when `ServiceCollectionExtensions.cs` is in scope (Auditor F3 flag).
- **`PaletteScopeChangedAction` consumer wiring (DN7 payload field)** — Chunk 2 effect review.
- **`CtrlCommaSingleBindingTest.cs` deletion** — verified present in the diff file list (deletion line), but deletion contract review belongs to Chunk 2 when the new `FrontComposerShellTests.cs` coverage is inspected.
- **Chunk 1 (PaletteScorerBench focus)** should cross-reference `PaletteScorer.Score` scoring contract vs what a BDN micro-bench would need to surface — specifically the top-50 cap + contextual-bonus application order established by `HandlePaletteQueryChanged` effect.
