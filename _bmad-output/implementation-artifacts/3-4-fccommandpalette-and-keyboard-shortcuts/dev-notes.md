# Dev Notes

## Executive Summary (Read First — Feynman-level, ~30 sec)

The palette is a `FluentDialog` that opens on `Ctrl+K`. When the user types, the effect waits 150 ms (debounce, cancels-earlier-keystroke), then runs a pure-function fuzzy scorer (`PaletteScorer.Score`) against every projection + command name in the registry. Results are grouped into **Projections / Commands / Recent / Shortcut** categories and rendered as a `role="listbox"`. Pressing Enter navigates to the route (or the generated command form); Escape closes. The recent-route list is 5 entries, persisted per-tenant/user under `{tenantId}:{userId}:palette-recent` with the same fail-closed guard as every other Epic 3 persistence path.

Keyboard shortcuts are owned by a new `IShortcutService` (Contracts-layer interface, Shell-layer impl, Scoped lifetime). Three shortcuts ship at shell level: `Ctrl+K` (palette), `Ctrl+,` (settings — migrated from Story 3-3's inline binding), `g h` (home chord). Duplicate registration logs `HFC2107` Information + last-writer-wins so adopters can deliberately override. Build-time conflict detection is deferred to Story 9-4.

The `IBadgeCountService` (Story 3-5) appears on projection rows as a `FluentBadge` — but only when the service is registered. 3-4 consumes it via nullable `[Inject]`, so "Story 3-5 not yet shipped" and "Story 3-5 shipped" differ only in whether the badge renders. No shim, no feature flag, no try-catch.

For the precision (why scoring lives in the effect, not the reducer; why the service is Scoped not Singleton; why badges use nullable DI) see **ADR-042 / 043 / 044** + **D1 / D7 / D8 / D16**. For what is NOT in 3-4 (build-time analyzer, in-palette command invocation, i18n scoring, virtual scrolling) see **Known Gaps G1-G15**.

## Service Binding Reference

All additions in 3-4 ride inside the existing `AddHexalithFrontComposer` registration — two new registrations + one new Fluxor feature discovered by the existing assembly scan.

- `IShortcutService` — **Scoped** (new, D2). Mirrors Story 3-1 `IStorageService` scoping. Backing `ConcurrentDictionary<string, ShortcutEntry>`. Register + GetRegistrations + TryInvokeAsync.
- `FrontComposerShortcutRegistrar` — **Scoped** (new). Consumed by `FrontComposerShell.OnAfterRenderAsync(firstRender)` once per circuit to register the three shell shortcuts.
- `IBadgeCountService` — **interface defined in 3-4, implementation in Story 3-5**. Nullable `[Inject]` in `FcPaletteResultList`.
- `IFrontComposerRegistry` — Singleton (Story 1-x). Consumed by `CommandPaletteEffects` for the search index.
- `IStorageService` — Scoped (Story 3-1 ADR-030). `CommandPaletteEffects` keeps its existing fail-closed pattern.
- `IUserContextAccessor` — Scoped (Story 2-2 D31). Reused via `TryResolveScope` guard.
- `IStringLocalizer<FcShellResources>` — Scoped via `AddLocalization()`. Resolves 12 new keys.
- `IDispatcher`, `IState<FrontComposerCommandPaletteState>`, `IState<FrontComposerNavigationState>` — Fluxor scoped-per-circuit.
- `IUlidFactory` — Singleton (Story 2-3). Correlation IDs for every dispatched action.
- `IDialogService` — Scoped (Fluent UI v5). `FcPaletteTriggerButton` + `FrontComposerShortcutRegistrar` call `ShowDialogAsync<FcCommandPalette>`.
- `NavigationManager` — Scoped (Blazor built-in). `HandlePaletteResultActivated` effect calls `NavigateTo`.
- `Fluxor` assembly scan — existing `ScanAssemblies(typeof(FrontComposerThemeState).Assembly)` auto-discovers the new `FrontComposerCommandPaletteFeature`.

## Palette State Machine

```
Event                                 │ Action dispatched              │ Effect handler path
────────────────────────────────────────────────────────────────────────────────────────────────────
App startup (hydrate)                 │ AppInitializedAction           │ HandleAppInitialized
                                      │  → PaletteHydratedAction       │   reads storage key, dispatches hydrate
Ctrl+K / header icon click            │ PaletteOpenedAction            │ (registrar) → ShowDialogAsync<FcCommandPalette>
                                      │                                 │   (idempotent — no-op if IsOpen)
                                      │                                 │ HandlePaletteOpened → PaletteResultsComputedAction (initial)
Keystroke in search field             │ PaletteQueryChangedAction      │ HandlePaletteQueryChanged (150 ms debounce)
                                      │  → PaletteResultsComputedAction │   scores registry + applies contextual bonus
ArrowUp/Down                          │ PaletteSelectionMovedAction    │ (reducer-only — clamps to [0, Len-1])
Enter                                 │ PaletteResultActivatedAction   │ HandlePaletteResultActivated
                                      │  → PaletteClosedAction          │   NavigationManager.NavigateTo
                                      │  → RecentRouteVisitedAction     │ HandleRecentRouteVisited → storage.SetAsync
Escape / dialog close                 │ PaletteClosedAction            │ (reducer-only — flips IsOpen=false)
────────────────────────────────────────────────────────────────────────────────────────────────────

Reducer (pure, no DI): state with {
    IsOpen = action.IsOpenValue (for Opened/Closed),
    Query = action.Query (for QueryChanged),
    Results = action.Results (for ResultsComputed),
    SelectedIndex = Math.Clamp(state.SelectedIndex + action.Delta, 0, Results.Length - 1) (for SelectionMoved),
    RecentRouteUrls = ringBufferPrepend(state.RecentRouteUrls, action.Url, 5) (for RecentRouteVisited)
}
```

## Scoring Algorithm

```
Input: query (user text), candidate (projection/command name)
Output: int Score (0 = no match; higher = better match)

Algorithm (PaletteScorer.Score, pure):
1. Short-circuit on empty inputs → 0.
2. q = query.ToLowerInvariant(); c = candidate.ToLowerInvariant().
3. If c.StartsWith(q) → 100 + (q.Length × 2).  // Exact prefix band.
4. Else if c.Contains(q) → 50 + q.Length.       // Contains substring band.
5. Else walk q through c:
     matched = 0, gaps = 0, ci = 0.
     For each qChar in q:
       found = c.IndexOf(qChar, ci).
       If found < 0 → return 0.
       gaps += found - ci.
       ci = found + 1.
       matched++.
   Return 10 + matched - gaps.

Contextual bonus (applied by EFFECT, not scorer):
  If candidate.BoundedContext == NavigationState.CurrentBoundedContext → Score += 15.
  If BoundedContext is null / empty → bonus=0 regardless.

Sort: OrderByDescending(r => r.Score).Take(50).
```

## PaletteState Shape & Persistence Schema

```
In-memory state:
  FrontComposerCommandPaletteState(
    IsOpen: bool,                              // arbitration with IDialogService
    Query: string,                              // "" initial; last query text
    Results: ImmutableArray<PaletteResult>,    // NEVER persisted
    RecentRouteUrls: ImmutableArray<string>,   // ring buffer, capped at 5
    SelectedIndex: int,                        // clamped [0, Results.Length - 1]
    LoadState: PaletteLoadState                // Idle / Searching / Ready
  )

Persistence:
  Storage key: {tenantId}:{userId}:palette-recent
  Stored value: string[] JSON (e.g., ["/counter", "/commerce/orders"])
  Schema invariants (locked by CommandPaletteEffectsTests + ScopeTests):
  - Array length 0-5; ordered most-recent-first.
  - NEVER persists Results, Query, IsOpen, SelectedIndex, LoadState.
  - Null/empty/whitespace tenant OR user → HFC2105 + no write.
  - Hydrate is read-only (ADR-038 mirror).
```

## Dialog Composition Diagram

```
┌─ FluentDialog (Width=600px, Modal=true, Title=@Localizer["CommandPaletteTitle"])
│
│  ┌─ [×] close icon (FluentDialog built-in, Escape also dismisses via dialog-local @onkeydown)
│
│  ┌─ Body: FcCommandPalette
│  │
│  │  ├─ <FluentSearch @ref="_searchRef" @bind-Value="Query" Placeholder="..." aria-controls="fc-palette-results" />
│  │  │     (auto-focused on OnAfterRenderAsync(firstRender))
│  │  │
│  │  ├─ <FcPaletteResultList
│  │  │     Id="fc-palette-results"
│  │  │     Results="@PaletteState.Results"
│  │  │     SelectedIndex="@PaletteState.SelectedIndex"
│  │  │     OnSelectionChanged="OnSelectionClicked" />
│  │  │
│  │  │   <ul role="listbox" aria-activedescendant="@ResultElementId(SelectedIndex)">
│  │  │     foreach group in {Projections, Commands, Recent, Shortcut}:
│  │  │       <li role="none">
│  │  │         <h4 id="@GroupHeadingId(category)">@CategoryLabel</h4>
│  │  │         <ul role="group" aria-labelledby="@GroupHeadingId(category)">
│  │  │           foreach (result, flatIndex) in group.Items:
│  │  │             <li role="option" id="@ResultElementId(flatIndex)"
│  │  │                 aria-selected="@(flatIndex == SelectedIndex)"
│  │  │                 class="@(selected ? 'fc-palette-option-selected' : 'fc-palette-option')">
│  │  │               @result.DisplayLabel
│  │  │               @if (IsInCurrentContext) → "(in current context)"
│  │  │               @if (BadgeCounts is not null && result.Category == Projection && Counts[type])
│  │  │                  <FluentBadge Intent="Info">@count</FluentBadge>
│  │  │               @if (Category == Shortcut) → <span>@Localizer[DescriptionKey]</span>
│  │  │             </li>
│  │  │         </ul>
│  │  │       </li>
│  │  │   </ul>
│  │  │
│  │  ├─ <p class="fc-palette-noresults">@Localizer["PaletteNoResultsText"]</p>  @* when Results.IsEmpty *@
│  │  │
│  │  └─ <div class="fc-sr-only" role="status" aria-live="polite" aria-atomic="true">
│  │        @string.Format(Localizer["PaletteResultCountTemplate"], Results.Length)
│  │     </div>
│  │
└─
```

## Fluent UI v5 Component Reference

Per MCP `get_component_details("FluentDialog")` (verified in Story 3-3 Task 0.2 — unchanged at Fluent UI v5 RC2):

- `IDialogService.ShowDialogAsync<TContent>(DialogParameters)` — instantiates `TContent` as dialog body. `TContent` implements `IDialogContentComponent`.
- `[CascadingParameter] IDialogInstance? Dialog` — available inside the content component; `Dialog.CloseAsync()` dismisses.

Per MCP `get_component_details("FluentSearch")` (verify in Task 0.2 — v5 RC2):

- `Value` (string) + `ValueChanged` (EventCallback<string>) for two-way bind.
- `Placeholder` parameter.
- `FocusAsync()` (expected — spike confirms).

Per MCP `get_component_details("FluentBadge")` (verify in Task 0.3):

- `Intent` (MessageIntent.Info/Success/Warning/Error) — `Info` used for ActionQueue badge counts.
- Child content renders the number.

## Files Touched Summary

**Created (18 files):**
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/IShortcutService.cs`
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutRegistration.cs`
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs`
- `src/Hexalith.FrontComposer.Contracts/Badges/IBadgeCountService.cs`
- `src/Hexalith.FrontComposer.Contracts/Badges/BadgeCountChangedArgs.cs`
- `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs`
- `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteState.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor` + `.razor.cs` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor` + `.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor` + `.razor.cs`

**Modified (6 files):**
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` — insert `<FcPaletteTriggerButton />` in `HeaderEnd` auto-populate ahead of `<FcSettingsButton />`; no other change.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` — delete Story 3-3 inline `Ctrl+,` branch, route `HandleGlobalKeyDown` through `IShortcutService.TryInvokeAsync`, register shell shortcuts on first render via `FrontComposerShortcutRegistrar.RegisterShellDefaultsAsync`.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs` — append `string? CurrentBoundedContext` field (nullable — derived from route) per Task 2.1a.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` + `.fr.resx` — 12 new keys.
- `src/Hexalith.FrontComposer.Shell/ServiceCollectionExtensions.cs` (or whichever file defines `AddHexalithFrontComposer`) — add `services.AddScoped<IShortcutService, ShortcutService>()` + `services.AddScoped<FrontComposerShortcutRegistrar>()`.
- `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` — add `HFC2107_ShortcutConflict` at Information severity.

**Created tests (11 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutBindingNormalizeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/HFC2107ShortcutConflictLogTest.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerPropertyTests.cs` (FsCheck)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerBench.cs` (BenchmarkDotNet, opt-in)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteReducerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteTriggerButtonTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs` (Playwright)

**Modified tests (2 files):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — append `PaletteTriggerAutoPopulatesAheadOfSettings` + `CtrlCommaInvokesRegisteredShortcut` + `CtrlKInvokesPaletteViaRegisteredShortcut` + `TextInputTargetGuardSkipsBareLetterChords`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` — append 6 lookups for 12 new keys.

**Deleted tests (1 file):**
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` — retired per Story 3-3 D16 migration contract (AC8).

No changes to `Contracts/Rendering/`, `SourceTools/`, `EventStore/`, or `Tenants/` submodules.

## Testing Standards

- xUnit v3, Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited.
- FsCheck.Xunit.v3 — already added to `Directory.Packages.props` (per Story 2-1 deferred note); 3-4 is the first real consumer of FsCheck after Story 2-4's property tests.
- BenchmarkDotNet for `PaletteScorerBench` — opt-in via `[Trait("Category", "Performance")]` so CI default run skips (matches Story 1-8 third-review perf-filter precedent).
- `TestContext.Current.CancellationToken` on async tests.
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI for Verify.
- `BunitJSInterop` strict mode — no JS interop in 3-4 beyond what `FluentSearch.FocusAsync()` does internally (spike confirms — no explicit `InvokeVoidAsync` calls from our components).
- `NSubstitute` for `IDialogService`, `IShortcutService` (in shell tests), `IBadgeCountService` (in palette tests).
- **Test count budget (L07):** ~34 new tests / 19 decisions ≈ 1.8 per decision — within Murat's 1.6-2.3 range. PR-review gate at Task 10.13 confirms or trims.

## Build & CI

- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story.
- `HFC2107_ShortcutConflict` is a new runtime diagnostic ID — AnalyzerReleases.Unshipped.md row required per Story 1-8 G2 discipline.
- Scoped CSS emits `{AssemblyName}.styles.css` automatically — `FcCommandPalette.razor.css`, `FcPaletteResultList.razor.css` ride the existing bundle (Story 3-1 ADR-034 filename contract unchanged).
- No new CI jobs — everything rides `dotnet build` + `dotnet test`.
- Playwright E2E in Task 10.12 runs via the Story 3-1 / 3-2 / 3-3 harness (Aspire MCP per `memory/feedback_no_manual_validation.md`).

## Previous Story Intelligence

**From Story 3-3 (immediate predecessor):**
- **Sharded story format** — `index.md` + per-section markdown files. 3-4 follows the identical structure.
- **L06 budget discipline** — 3-3 landed at 20 decisions (feature, ≤ 25). 3-4 lands at 19 — comfortably below cap; palette + shortcuts is two tightly-coupled surfaces that merit one story (architecture §524 "Phase 3: FcCommandPalette + …" treats them as one deliverable).
- **L07 test-to-decision ratio** — 3-3 at ~1.8 post-review. 3-4 at ~1.8 — consistent.
- **`IDialogService.ShowDialogAsync<TContent>` pattern** — 3-4 mirrors Story 3-3 `FcSettingsDialog` verbatim. Dialog owns lifetime; Fluxor `IsOpen` is used ONLY for arbitration (idempotent open).
- **`HeaderEnd` auto-populate** — 3-4 extends the Story 3-3 D12 auto-populate branch to include `<FcPaletteTriggerButton />` ahead of `<FcSettingsButton />`. Adopter-supplied non-null still wins.
- **Story 3-3 D16 `Ctrl+,` migration contract** — 3-4 D1 + D5 + AC8 fulfill the migration; `CtrlCommaSingleBindingTest` is deleted per the spec'd retirement (Task 8.4).
- **`FcDensityAnnouncer` `.fc-sr-only` utility class** (Story 3-3 D20) — 3-4's palette result-count announcement reuses the same class; no new CSS.
- **Cross-feature effect pattern** (Story 3-3 `DensityEffects.HandleViewportTierChanged` subscribing to `Navigation.ViewportTierChangedAction`) — 3-4 does not cross features, but `CommandPaletteEffects` reads `IState<FrontComposerNavigationState>` for contextual bonus (read-only cross-feature state consumption, also pre-D7 precedent).

**From Story 3-2:**
- **`ViewportTier` enum + `FrontComposerNavigationState.CurrentViewport`** — 3-4 does NOT subscribe to viewport (palette is size-agnostic; Fluent UI's dialog auto-adapts). 3-4 DOES add `CurrentBoundedContext` to `FrontComposerNavigationState` per Task 2.1a — appended field, no shape break.
- **`NavigationPersistenceBlob`** — 3-4 does NOT extend nav persistence; palette recent-routes persist under their own key (`"palette-recent"`) per D10.
- **`HFC2106_ThemeHydrationEmpty` cross-feature reuse** — 3-4 reuses for palette hydrate errors (same pattern as Story 3-3 D19).

**From Story 3-1:**
- **`IStorageService` ADR-030 Scoped lifetime** — `CommandPaletteEffects` picks up via constructor injection.
- **`IUserContextAccessor` ADR-029 fail-closed** — `TryResolveScope` pattern reused verbatim.
- **`FcShellOptions` (D14 / G1)** — 3-4 does NOT add any new options properties (no `ChordTimeoutMs` — see G15 deferral). Property count stays at 15 (post-3-3).
- **Single write path discipline** — `CommandPaletteReducers` stay pure static; scoring is effect-side.

**From Story 2-2:**
- **Generated `/domain/{CommandName}` FullPage route convention** — `HandlePaletteResultActivated` navigates to this route for `Category == Command`. The route is emitted by `CommandPageEmitter` per Story 2-2 D15.

**From Story 2-3:**
- **ULID correlation pattern** — reused via `IUlidFactory.NewUlid()` for every dispatched action.
- **Single-writer invariant (D19)** — 3-4's reducer set is the single write path into `FrontComposerCommandPaletteState`. No bypass.

**From Story 1-3:**
- **Per-concern Fluxor features** — 3-4 adds `CommandPalette` (the fourth feature after Theme, Density, Navigation). Follows the same `Shell/State/{Concern}/` layout.

## Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Cross-story contract table in Critical Decisions names producer and consumer for 9 seams (IShortcutService + ShortcutRegistration + ShortcutBinding, FrontComposerCommandPaletteState shape, PaletteResult + Category enum, PaletteScorer.Score, IBadgeCountService nullable injection, Story 3-3 Ctrl+, migration, HeaderEnd auto-populate extension, RecentRouteVisitedAction (Story 3-6 observation), HFC2107 diagnostic ID (Story 9-4 analyzer)).
- **L02 Fluxor feature producer+consumer scope** — `FrontComposerCommandPaletteFeature` PRODUCER stories: 3-4 (this story — new feature, reducers, effects, component subscription). CONSUMER stories: 3-4's own `FcCommandPalette` + `FcPaletteResultList`, Story 3-5 (badge count subscription — inert until 3-5 registers the service), Story 3-6 (may observe `RecentRouteVisitedAction` for session blob updates). Shipping ALL producers + effects in 3-4 (no deferred effects) avoids Story 2-2's "half a state machine" risk.
- **L03 Tenant/user isolation fail-closed** — D10 inherits Story 3-1 ADR-029 + Story 3-2 ADR-038 patterns verbatim via `TryResolveScope`. Memory feedback `feedback_tenant_isolation_fail_closed.md` honored.
- **L04 Generated name collision detection** — Not applicable; 3-4 does not extend the source generator.
- **L05 Hand-written service + emitted per-type wiring** — Not applicable; 3-4 infrastructure is hand-written only.
- **L06 Defense-in-depth budget** — 19 decisions, below the 25 feature cap. Room for a review round (party mode + advanced elicitation) to add up to 6 more without hitting the cap.
- **L07 Test count inflation** — ~34 tests / 19 decisions ≈ 1.8 — within Murat's range. Task 10.13 PR-review gate decides whether to add or trim.
- **L08 Party review vs. elicitation** — 3-4 has NOT yet been reviewed via party mode or advanced elicitation. Recommended flow before `dev-story`: `/bmad-party-mode` (Winston / Sally / Murat / Amelia) → apply findings → `/bmad-advanced-elicitation` (Pre-mortem / Red Team / Chaos) → `dev-story`. Key review areas: (a) D5 text-input guard simplification (single-letter-without-modifier skip) — Murat: does the simplification miss edge cases like "Ctrl+Shift+D in a textarea"? (b) D16 nullable `IBadgeCountService` injection — Winston: is Blazor's `[Inject]`-on-nullable actually tolerant of unregistered services in practice, or does it throw on the scope? (c) D11 idempotent open via state read before dispatch — Sally: does the state read + dispatch + dialog-show sequence have a race window if Ctrl+K fires twice within the same render tick? (d) D9 150 ms debounce value — Sally: the UX spec says 150 ms; does that hold for Russian/German users whose languages have longer words?
- **L09 ADR rejected-alternatives discipline** — ADR-042 cites 4 rejected, ADR-043 cites 6, ADR-044 cites 4. All ≥ 2 satisfied.
- **L10 Deferrals name a story** — All 15 Known Gaps cite specific owning stories (3-5, 6-1, 10-2, 9-4, v1.x, v2).
- **L11 Dev Agent Cheat Sheet** — Present. Feature story with 18 new source files + 11 new test files warrants the fast-path entry. Sized similar to 3-3's cheat sheet.

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-composition-shell-navigation-experience.md#Story 3.4 — AC source of truth, §150-197]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR18 Command palette]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR1 FcCommandPalette]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR43 Keyboard shortcuts via IShortcutService]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR67 IShortcutService with conflict detection]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR68 Command palette contextual commands]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#NFR5 Search response < 100 ms]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcCommandPalette, §72-114]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#IBadgeCountService, §42-70]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md#Navigation at scale, §273]
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern Fluxor Features, §527-536]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-029 IUserContextAccessor fail-closed]
- [Source: _bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/architecture-decision-records.md#ADR-030 IStorageService Scoped]
- [Source: _bmad-output/implementation-artifacts/3-2-sidebar-navigation-and-responsive-behavior/architecture-decision-records.md#ADR-038 Hydrate is read-only]
- [Source: _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/critical-decisions-read-first-do-not-revisit.md#D16 Ctrl+, inline binding migration contract to Story 3-4]
- [Source: _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/critical-decisions-read-first-do-not-revisit.md#D11 IDialogService direct invocation — no Fluxor action]
- [Source: _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/critical-decisions-read-first-do-not-revisit.md#D12 HeaderEnd auto-populate]
- [Source: _bmad-output/implementation-artifacts/3-3-display-density-and-user-settings/critical-decisions-read-first-do-not-revisit.md#D20 FcDensityAnnouncer .fc-sr-only utility class]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01-L11 — all lessons applied]
- [Source: memory/feedback_no_manual_validation.md — Aspire MCP + Playwright over manual validation for Task 11.3]
- [Source: memory/feedback_cross_story_contracts.md — cross-story contract table per ADR-016 canonical example]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D10 inherits Story 3-1 ADR-029]
- [Source: memory/feedback_defense_budget.md — 19 decisions, under ≤ 25 feature cap]
- [Source: src/Hexalith.FrontComposer.Contracts/Registration/IFrontComposerRegistry.cs — consumed read-only]
- [Source: src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs — consumed read-only]
- [Source: src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs — extended with CurrentBoundedContext (Task 2.1a)]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor — modified in Task 7.2 + 8.1 + 8.2 + 8.3]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsButton.razor — reused pattern template for FcPaletteTriggerButton]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor — reused pattern template for FcCommandPalette dialog content]
- [MCP: `get_component_details("FluentSearch")` — search input surface — Task 0.2 spike]
- [MCP: `get_component_details("FluentBadge")` — badge surface — Task 0.3 spike]
- [MCP: `get_component_details("FluentDialog")` — dialog lifecycle inherited from Story 3-3]

## Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §527-536):
  - `Shell/State/CommandPalette/` NEW subfolder — 4th Fluxor per-concern feature (after Theme, Density, Navigation). Matches the §529-536 table pattern.
  - `Contracts/Shortcuts/` NEW subfolder in Contracts — matches the `Rendering/` + `Registration/` + `Storage/` pattern of per-concern Contracts subfolders.
  - `Contracts/Badges/` NEW subfolder — defines `IBadgeCountService` for 3-4 consumption; Story 3-5 implements against the interface.
  - `Shell/Shortcuts/` NEW subfolder — internal implementation of the Contracts-layer interface. Mirrors `Shell/State/*/` per-concern naming.
  - `Shell/Components/Layout/` — 3 new files (`FcCommandPalette` + `FcPaletteResultList` + `FcPaletteTriggerButton`) land in the existing folder from Story 3-1 / 3-2 / 3-3.
  - No Contracts → Shell reverse references.
- **Fluent UI `Fc` prefix convention** honored — `FcCommandPalette`, `FcPaletteResultList`, `FcPaletteTriggerButton`.
- **`FcShellOptions` NOT extended** — G1 options-class split stays deferred to Story 9-2; property count stays at 15 (post-3-3).
- **`FrontComposerNavigationState` extension** (Task 2.1a) — append-only `string? CurrentBoundedContext` field. Story 3-2 ADR-037 "ViewportTier NEVER persisted" translates: `CurrentBoundedContext` is ALSO not persisted (derived from `NavigationManager.Uri` at render/route-change time).

---
