# Tasks / Subtasks

> 11 tasks (plus Task 0 prereq). Each task lists owning decision(s), AC(s), and exit criteria.

---

## Task 0 — Prereq verification + FluentSearch / FluentDialog interaction spike (≤ 30 min)

**Goal:** Confirm Fluent UI v5 `FluentSearch` surface, `FluentBadge` surface, `IDialogService.ShowDialogAsync` with content-level `@onkeydown` behavior, and `KeyboardEventArgs.MetaKey` availability. Rebaseline the pre-3-4 test count.

- [ ] **0.1** `rg -c '\[Fact\]\|\[Theory\]' tests/` captures the pre-3-4 test count. Record in `dev-agent-record.md` Completion Notes as `test_baseline_pre_3_4 = <N>` (expected ~649 post-3-3).
- [ ] **0.2** Verify `FluentSearch` v5 surface via `mcp__fluent-ui-blazor__get_component_details("FluentSearch")`. Confirm: (a) `Value` + `ValueChanged` bind semantics; (b) `Placeholder` parameter; (c) `FocusAsync()` method or equivalent programmatic focus API. If `FluentSearch` does not exist in v5 RC2, fall back to `FluentTextInput` with the same bind semantics and note in Completion Notes.
- [ ] **0.3** Verify `FluentBadge` v5 surface via `mcp__fluent-ui-blazor__get_component_details("FluentBadge")`. Confirm `Intent` parameter accepts `MessageIntent.Info`.
- [ ] **0.4** Verify content-component `@onkeydown` inside a `FluentDialog`-hosted `IDialogContentComponent` fires for `Escape`/`ArrowUp`/`ArrowDown`/`Enter` (dialog's default key handling does NOT swallow them before the content gets a shot). If it does swallow `Escape`, use Fluent's dialog-level `OnClosing` callback instead and adjust Task 5.3.
- [ ] **0.5** Confirm `KeyboardEventArgs.MetaKey` is populated on Blazor Server `@onkeydown` events. If only `Key`/`CtrlKey`/`ShiftKey`/`AltKey` are reliably populated, drop the `Meta` branch from `ShortcutBinding.Normalize` and note in D1 spec adjustment.
- [ ] **0.6** Confirm `Icons.Regular.Size20.Search` exists in the Fluent UI v5 icons package (or an equivalent — Search/Find/MagnifyingGlass). If the icon name differs, note + adjust Task 7.1.
- [ ] **0.7** `dotnet build` clean. Zero warnings baseline confirmed.
- [ ] **0.8** Spike output file — write findings to `_bmad-output/implementation-artifacts/3-4-fccommandpalette-and-keyboard-shortcuts/spike-notes.md` with sections: `FluentSearch surface verified`, `FluentBadge surface verified`, `IDialogService keyDown routing`, `MetaKey availability`, `Icon name confirmation`, `Baseline test count`. Cross-link this file from `dev-agent-record.md` Completion Notes. (Amelia party-mode review — spike output path explicit.)
- [ ] **0.9** Confirm `Microsoft.Extensions.Time.Testing.FakeTimeProvider` package is referenced by the Shell.Tests project (required by D22). If missing, add `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.*" />` to `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` and record in spike-notes.
- [ ] **0.10** Confirm `IFrontComposerRegistry.HasFullPageRoute(string commandTypeName) : bool` exists OR file a Story 2-2 amendment PR to add it (D21 cross-story contract). If the method is missing when 3-4 development begins, **block on the amendment PR** — do NOT implement a local polyfill. **Mechanical tripwire (post-elicitation hardening)**: run `rg -n "HasFullPageRoute" src/Hexalith.FrontComposer.Contracts` — if the grep returns ZERO matches, HALT immediately and file the Story 2-2 amendment PR before proceeding. The tripwire result (match count + file paths) MUST be recorded in `spike-notes.md` under a "D21 registry contract check" heading. A passing tripwire shows at least one match in `IFrontComposerRegistry.cs` or equivalent.

**Exit:** 0.1 value captured, 0.2-0.6 all match expectations, 0.8 spike-notes.md filled, 0.9 package reference confirmed, 0.10 registry contract tripwire passed + recorded OR amendment PR filed. **Blocks:** Every subsequent task.

---

## Task 1 — `IShortcutService` contract + `ShortcutService` implementation

**Decisions:** D1, D2, D3, D4, D12, D19. **ACs:** AC1, AC2, AC8.

- [ ] **1.1** Create `src/Hexalith.FrontComposer.Contracts/Shortcuts/IShortcutService.cs` with the three-method interface per D1. XML comment each method citing the binding grammar + dispose semantics.
- [ ] **1.1a** Create `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutRegistration.cs` — `public sealed record ShortcutRegistration(string Binding, string DescriptionKey, string NormalisedLabel)` with XML cite of D1 + D14.
- [ ] **1.1b** Create `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs` — `public static class` with `Normalize(string binding) : string` + `TryFromKeyboardEvent(KeyboardEventArgs e, out string binding) : bool`. `Normalize` implements: trim, lowercase, split on `+`, canonical modifier order (`ctrl`, `shift`, `alt`, `meta`), single-space-separated chord (`g h`), throws `ArgumentException` on empty strings, single bare letters (see D4 no-single-letter-keys rule), or unknown modifiers.
- [ ] **1.2** Create `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs` — internal `sealed class ShortcutService : IShortcutService`. Backing `ConcurrentDictionary<string, ShortcutEntry>` (entry = handler + descriptionKey + normalisedLabel + callSiteFile + callSiteLine). Constructor takes `(TimeProvider timeProvider, ILogger<ShortcutService> logger)` per D22 — `_timeProvider = timeProvider ?? TimeProvider.System`. `Register` returns a `Disposable` that removes the entry on dispose (comparison against the original `ShortcutEntry` so a replaced-then-disposed registration does NOT remove the replacement — guard via reference equality on the entry record).
- [ ] **1.2a** Implement `TryInvokeAsync(KeyboardEventArgs e)` per D4 FSM — calls `ShortcutBinding.TryFromKeyboardEvent(e, out var binding)`, short-circuits if false (modifier-less text input). Chord FSM:
    - Fast-path modifier-bearing binding (`ctrl+*`, `shift+ctrl+*`): if a handler exists, invoke it AND clear `_pendingFirstKey` + cancel chord timer (D4 sub-decision d).
    - Chord-prefix key (e.g., `g` is the first key of registered chord `g h`): store `_pendingFirstKey = binding`; schedule `_chordTimer = _timeProvider.CreateTimer(ClearPendingFirstKey, null, TimeSpan.FromMilliseconds(1500), Timeout.InfiniteTimeSpan)` (D22 + D4 sub-decision a); if `_pendingFirstKey` was already set, overwrite it (D4 sub-decision c: repeat-prefix during pending = new prefix replaces old) and dispose the old timer.
    - Matching-second-key: concatenate pending + incoming to form `"g h"`; look up + invoke; clear `_pendingFirstKey` + dispose timer.
    - Non-matching second key: clear `_pendingFirstKey` + dispose timer; re-evaluate the new key as a fresh single-key lookup.
    - Timer elapse (D4 sub-decision b): `ClearPendingFirstKey` callback sets `_pendingFirstKey = null` silently — NO log, NO diagnostic (expected flow, mirrors D9 `OperationCanceledException` swallow).
    - Concurrency (D4 sub-decision e): `_pendingFirstKey` + `_chordTimer` fields guarded by a `lock (_chordSync)` object; timer callback acquires the same lock before clearing.
    - Returns `true` iff a handler was invoked (chord completion or single-key fast-path).
- [ ] **1.2b** Confirm `TimeProvider` DI registration — the host's default registers `TimeProvider.System` automatically in ASP.NET Core 8+. Add no explicit registration. The test fixture (Task 10.1) injects `FakeTimeProvider` via test-project DI override.
- [ ] **1.3** Reserve `HFC2107_ShortcutConflict` in `src/Hexalith.FrontComposer.Shell/Diagnostics/DiagnosticDescriptors.cs` (or equivalent location matching the Story 3-1 / 3-2 precedent). Add row to `AnalyzerReleases.Unshipped.md` at Information severity with remediation text: "Adopter registered a keyboard shortcut that collides with an earlier registration. Latest registration wins. Rename the adopter's binding or deliberately accept the override."
- [ ] **1.4** Create `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs` — `public sealed class` with `Task RegisterShellDefaultsAsync()` method. Constructor injects `IShortcutService`, `IDispatcher`, `IState<FrontComposerCommandPaletteState>`, `IDialogService`, `NavigationManager`, `IStringLocalizer<FcShellResources>`, `IUlidFactory`. Implements the three shell shortcuts per D1 + D12 (idempotent palette open).
- [ ] **1.4a** Add **D24 idempotency guard** to `RegisterShellDefaultsAsync`: private `bool _registered` instance field; method body opens with `if (_registered) return; _registered = true;` BEFORE any `_shortcuts.Register(...)` call. The flag is NOT volatile — the Scoped-per-circuit lifetime means only one thread calls `RegisterShellDefaultsAsync` per circuit (Blazor's render loop is single-threaded per circuit). Tested via `FrontComposerShortcutRegistrarTests.RegisterShellDefaultsAsync_IsIdempotent_WithinSameInstance` (Task 10.1f).
- [ ] **1.5** Register in `AddHexalithFrontComposer`: `services.AddScoped<IShortcutService, ShortcutService>();` + `services.AddScoped<FrontComposerShortcutRegistrar>();`. Fluxor assembly scan automatically covers the feature (existing `typeof(FrontComposerThemeState).Assembly` scan picks up the new `FrontComposerCommandPaletteFeature`).
- [ ] **1.6** Reserve **`HFC2108_ShortcutHandlerFault`** in `DiagnosticDescriptors.cs` + add row to `AnalyzerReleases.Unshipped.md` at Warning severity with remediation text: "Registered keyboard-shortcut handler threw an exception. The handler fired and the shortcut service suppressed the exception to prevent Blazor error-boundary escalation. Inspect the handler implementation cited in `DescriptionKey`." Structured payload: `{Binding, DescriptionKey, ExceptionType, ExceptionMessage}`. Implement the catch-and-log inside `ShortcutService.TryInvokeAsync` per D1 handler-exception policy.

**Exit:** `dotnet build` clean. `ShortcutServiceTests` green.

---

## Task 2 — `FrontComposerCommandPaletteFeature` (Fluxor state + actions + reducers)

**Decisions:** D6, D8, D11, D12. **ACs:** AC2, AC3, AC5.

- [ ] **2.1** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteState.cs` with the record shape per D6.
- [ ] **2.2** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteFeature.cs` — `GetInitialState() => new(IsOpen: false, Query: "", Results: ImmutableArray<PaletteResult>.Empty, RecentRouteUrls: ImmutableArray<string>.Empty, SelectedIndex: 0, LoadState: PaletteLoadState.Idle)`.
- [ ] **2.3** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs` — 8 action records per story.md:
    ```csharp
    public sealed record PaletteOpenedAction(string CorrelationId);
    public sealed record PaletteClosedAction(string CorrelationId);
    public sealed record PaletteQueryChangedAction(string CorrelationId, string Query);
    public sealed record PaletteResultsComputedAction(string Query, ImmutableArray<PaletteResult> Results);
    public sealed record PaletteSelectionMovedAction(int Delta);
    public sealed record PaletteResultActivatedAction(int SelectedIndex);
    public sealed record RecentRouteVisitedAction(string Url);
    public sealed record PaletteHydratedAction(ImmutableArray<string> RecentRouteUrls);
    ```
- [ ] **2.4** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs` — `PaletteResult` record + `PaletteResultCategory` enum (`Projection=0, Command=1, Recent=2, Shortcut=3`) + `PaletteLoadState` enum (`Idle, Searching, Ready`).
- [ ] **2.5** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs` — 8 `[ReducerMethod]`s (one per action). Reducers stay pure static — no DI, no scoring. `PaletteSelectionMoved` clamps: `newIndex = Math.Clamp(currentIndex + action.Delta, 0, state.Results.Length - 1)` (with a guard for empty results → no-op). **`OnPaletteResultsComputed` applies D20 stale-result guard**: `public static FrontComposerCommandPaletteState OnPaletteResultsComputed(FrontComposerCommandPaletteState state, PaletteResultsComputedAction action) { if (!state.IsOpen) return state; return state with { Results = action.Results, SelectedIndex = 0, LoadState = PaletteLoadState.Ready }; }` — the `IsOpen` check refuses assignments that arrive after the palette closed. Not a purity violation (reads `state`, not external state).

**Exit:** Fluxor assembly scan discovers the feature. `dotnet build` clean.

---

## Task 3 — `CommandPaletteEffects` (debounce timer + recent-route persistence + stale-result guard)

**Decisions:** D7, D8, D9, D10, D13, D20, D21, D22, D23. **ACs:** AC3, AC4, AC5, AC6.

- [ ] **3.1** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` — `public sealed class` with constructor injection of `IFrontComposerRegistry`, `IShortcutService`, `IState<FrontComposerNavigationState>`, `IState<FrontComposerCommandPaletteState>`, `IStorageService`, `IUserContextAccessor`, `ILogger<CommandPaletteEffects>`, `NavigationManager`, `TimeProvider` (D22), `IStringLocalizer<FcShellResources>`. Private `_queryCts` field + `_paletteRecentFeatureSegment = "palette-recent"` constant + `_chordSync = new object()`.
- [ ] **3.2** Implement `HandlePaletteQueryChanged(PaletteQueryChangedAction action, IDispatcher dispatcher)`:
    - Cancel and replace `_queryCts`: `_queryCts?.Cancel(); _queryCts?.Dispose(); _queryCts = new CancellationTokenSource();`
    - Canonicalise query via `ResolveShortcutAliasQuery(action.Query)` (D23) — if the input is `?`, `help`, `keys`, `kb`, `shortcut` (exact match, case-insensitive, post-trim), return `"shortcuts"`; else return input unchanged.
    - Await `Task.Delay(TimeSpan.FromMilliseconds(150), _timeProvider, _queryCts.Token)` — catch `OperationCanceledException` and return silently.
    - If canonicalQuery.Equals("shortcuts", StringComparison.OrdinalIgnoreCase): map `_shortcuts.GetRegistrations()` to `PaletteResult` with `Category = Shortcut` + `DescriptionKey` copied from the registration; dispatch `PaletteResultsComputedAction(action.Query, results.ToImmutableArray())` (NB: payload carries the USER's original query, not the canonical — so the aria-live region announces `"?"` the user typed, not `"shortcuts"` they didn't).
    - Otherwise: enumerate `_registry.GetManifests()`, flatten to `(Projection | Command) × candidate` pairs, for Command candidates filter out any where `_registry.HasFullPageRoute(candidate.CommandTypeName) == false` (D21 — unreachable results never appear in the palette), score via `PaletteScorer.Score(canonicalQuery, candidate.Name)`, apply `+15` contextual bonus when `candidate.BoundedContext == _navState.Value.CurrentBoundedContext`.
    - Interleave Recent matches (`_state.RecentRouteUrls` where the URL's display label also scores > 0 on the query).
    - Take top-50 `OrderByDescending(r => r.Score)`, dispatch `PaletteResultsComputedAction(action.Query, results.ToImmutableArray())`.
- [ ] **3.2a** If `FrontComposerNavigationState` does NOT currently expose a `CurrentBoundedContext` string, ADD it as part of Task 2.1a — read-only derived property computed from `NavigationManager.Uri` via a lightweight parser at hydrate/route-change time. Task 2.1a: update `FrontComposerNavigationState` to append a `string? CurrentBoundedContext` field (nullable — null on home, non-null on any `/{bc}/...` route). Story 3-6 will persist routes; 3-4 just needs the current value.
- [ ] **3.2b** Implement `HandlePaletteClosed(PaletteClosedAction, IDispatcher)` per D20 stale-result guard — cancel + dispose the active `_queryCts`: `_queryCts?.Cancel(); _queryCts?.Dispose(); _queryCts = null;`. NO other side effect (Fluxor close transition already handled by the reducer; dialog-close is invoked by the component via `IDialogInstance.CloseAsync`). This prevents a stale `PaletteResultsComputedAction` from landing after the palette closes. The reducer for `PaletteResultsComputedAction` (Task 2.5) provides the second line of defence: `if (!state.IsOpen) return state;`.
- [ ] **3.3** Implement `HandlePaletteOpened(PaletteOpenedAction, IDispatcher)` — dispatches `PaletteResultsComputedAction("", initial)` where `initial` = **synthetic "Keyboard Shortcuts" Command entry (D23, first position)** + recent-route results + top-20 projections by alpha. The synthetic entry is `new PaletteResult(Category: Command, DisplayLabel: Localizer["KeyboardShortcutsCommandLabel"].Value, BoundedContext: "", RouteUrl: null, CommandTypeName: "@shortcuts", Score: 1000 /* always near top of initial view */, IsInCurrentContext: false, ProjectionType: null, DescriptionKey: "KeyboardShortcutsCommandDescription")`. This populates the palette so it's not blank on open AND provides a discoverable entry-point for the shortcut reference.
- [ ] **3.3a** Implement `ResolveShortcutAliasQuery(string query) : string` private helper — canonicalisation table per D23. Aliases (case-insensitive, trimmed): `?`, `help`, `keys`, `kb`, `shortcut`. Anything else returns the input unchanged. Pure function — testable standalone.
- [ ] **3.4** Implement `HandlePaletteResultActivated(PaletteResultActivatedAction action, IDispatcher dispatcher)`:
    - Read `state.Results[action.SelectedIndex]` via `IState<FrontComposerCommandPaletteState>`.
    - Special-case D23 sentinel: if `result.CommandTypeName == "@shortcuts"`, dispatch `PaletteQueryChangedAction(UlidFactory.NewUlid(), "shortcuts")` — refills palette with shortcut view. DO NOT dispatch `PaletteClosedAction` (palette stays open in the new mode). Return.
    - Switch on `result.Category`:
        - `Projection | Recent`: `_navigation.NavigateTo(result.RouteUrl)`.
        - `Shortcut`: if `result.RouteUrl is not null`, navigate; else no-op (shortcut reference rows are informational per AC6).
        - `Command`: `_navigation.NavigateTo(CommandRouteBuilder.BuildRoute(result.BoundedContext, result.CommandTypeName))` (D21 canonical URL contract).
    - Dispatch `PaletteClosedAction(UlidFactory.NewUlid())`.
    - Dispatch `RecentRouteVisitedAction(result.RouteUrl ?? CommandRouteBuilder.BuildRoute(result.BoundedContext, result.CommandTypeName))`.
- [ ] **3.4a** Create `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs` — `public static class` with two methods: `public static string KebabCase(string pascalCase)` (e.g., `"SubmitOrderCommand"` → `"submit-order-command"` via regex that inserts `-` before each uppercase letter that follows a lowercase letter or digit, then lowercases) AND `public static string BuildRoute(string boundedContext, string commandTypeName)` (returns `$"/domain/{KebabCase(boundedContext)}/{KebabCase(commandTypeName)}"`). Updated per D21 post-elicitation: helper lives under `Routing/` to avoid conflating keyboard-shortcut concerns with command-route concerns. `HandlePaletteResultActivated` in Task 3.4 uses `CommandRouteBuilder.BuildRoute(r.BoundedContext, r.CommandTypeName)` instead of inlining the kebab transform.
- [ ] **3.5** Implement `HandleRecentRouteVisited(RecentRouteVisitedAction action, IDispatcher dispatcher)`:
    - Build new ring buffer: `var updated = state.RecentRouteUrls.Remove(action.Url).Insert(0, action.Url); if (updated.Length > 5) updated = updated.RemoveRange(5, updated.Length - 5);`
    - Persist via `TryResolveScope` guard pattern (mirrors Story 3-1 / 3-2 / 3-3 verbatim) — if tenant+user resolve, `await _storage.SetAsync(StorageKeys.BuildKey(tenantId, userId, "palette-recent"), updated.ToArray())`; otherwise log `HFC2105_StoragePersistenceSkipped` + return.
    - Note: no `PaletteHydratedAction` dispatch here — this is a write path, not a rehydrate path.
- [ ] **3.6** Implement `HandleAppInitialized(AppInitializedAction, IDispatcher)` — mirrors Story 3-2 NavigationEffects hydrate verbatim: `TryResolveScope` → `await _storage.GetAsync<string[]>(key)` → on non-null, **filter through `IsInternalRoute(url)` per D10 post-Red-Team R7 hardening**, dispatch `PaletteHydratedAction(filteredStored.ToImmutableArray())`. `IsInternalRoute` rule (D10): must start with `/`, must NOT start with `//`, must NOT match `^[a-z]+:` scheme pattern (catches `http:`, `https:`, `javascript:`, `data:`, etc., case-insensitive), must NOT contain `\`. Filter implementation is a simple LINQ `.Where(IsInternalRoute).ToImmutableArray()` on the deserialised array. If any entries are filtered out, log the aggregate count at `HFC2106` Information severity with `Reason=Tampered` (new reason code — see Task 3.6b): `_logger.LogInformation("HFC2106: {RejectedCount} of {TotalCount} recent-route entries rejected — Reason=Tampered (non-internal URL format). Possible storage tampering or storage-schema drift.", rejectedCount, totalCount);`. Error handling mirrors Story 3-2 D15: `OperationCanceledException` → `LogDebug`; other → `HFC2106` Information with `Reason=Corrupt`; null/empty → `HFC2106` Information with `Reason=Empty`.
- [ ] **3.6a** Implement `private static bool IsInternalRoute(string url)` helper on `CommandPaletteEffects` (or co-located in `CommandRouteBuilder.cs`) — pure function; tested via `CommandPaletteEffectsTests.IsInternalRoute_Accepts_AllValidInternalRouteFormats` `[Theory]` covering `/`, `/counter`, `/domain/commerce/submit-order-command`, `/path?query=value`, `/path#hash` (all return true) AND `CommandPaletteEffectsTests.IsInternalRoute_Rejects_AllTamperedFormats` `[Theory]` covering `//evil.com`, `http://evil.com`, `javascript:alert(1)`, `data:text/html,...`, `\\backslash`, empty string, null (all return false).
- [ ] **3.6b** Extend the existing `HFC2106` reason-code enum (already reused from Story 3-1 / 3-2 with `Empty` / `Corrupt` reasons) to add a new `Reason=Tampered` value used by the D10 route-filter log entry. No new diagnostic ID needed — one ID per behavioural category, multiple reason codes per Story 3-2 D15 precedent. Update `AnalyzerReleases.Unshipped.md` only if the reason-code enum is a public surface (it is for structured-log consumers); otherwise document in `DiagnosticDescriptors.cs` XML comments. Log structure: `_logger.LogInformation("HFC2106: {RejectedCount} of {TotalCount} recent-route entries rejected — Reason=Tampered (non-internal URL format). Possible storage tampering or storage-schema drift.", rejectedCount, totalCount);`.
- [ ] **3.7** XML comment each effect handler citing the binding decision + diagnostic ID. Implement `IDisposable` on the effects class to dispose `_queryCts` (cancel + dispose on disposal — standard pattern).

**Exit:** Fluxor assembly scan discovers the effects. `dotnet build` clean with zero warnings.

---

## Task 4 — `PaletteScorer` pure-function fuzzy matcher

**Decisions:** D7; ADR-043. **ACs:** AC3, AC4.

- [ ] **4.1** Create `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs`:

    ```csharp
    public static class PaletteScorer {
      public static int Score(string query, string candidate) {
        if (string.IsNullOrEmpty(query)) return 0;
        if (string.IsNullOrEmpty(candidate)) return 0;
        var q = query.ToLowerInvariant();
        var c = candidate.ToLowerInvariant();

        // Exact prefix: 100 + matchLen × 2
        if (c.StartsWith(q, StringComparison.Ordinal)) return 100 + (q.Length * 2);

        // Contains: 50 + matchLen
        if (c.Contains(q, StringComparison.Ordinal)) return 50 + q.Length;

        // Fuzzy subsequence: walk q through c, track matched/gap
        int matched = 0, gaps = 0, ci = 0;
        foreach (var qc in q) {
          int found = c.IndexOf(qc, ci);
          if (found < 0) return 0;
          gaps += found - ci;
          ci = found + 1;
          matched++;
        }
        return 10 + matched - gaps;
      }
    }
    ```

- [ ] **4.2** XML comment on `Score` lists the three-band algorithm + explicit note that contextual bonus (`+15`) is applied by the EFFECT, not inside this pure function.

**Exit:** Scorer is pure static with no dependencies. `dotnet build` clean.

---

## Task 5 — `FcCommandPalette.razor` dialog content component

**Decisions:** D11, D15, D17. **ACs:** AC2, AC5, AC6.

- [ ] **5.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor`:

    ```razor
    @namespace Hexalith.FrontComposer.Shell.Components.Layout
    @using Microsoft.AspNetCore.Components.Web
    @using Microsoft.FluentUI.AspNetCore.Components
    @inherits Fluxor.Blazor.Web.Components.FluxorComponent
    @implements Microsoft.FluentUI.AspNetCore.Components.IDialogContentComponent
    @inject IStringLocalizer<FcShellResources> Localizer

    <div class="fc-palette-root" role="dialog" aria-label="@Localizer["CommandPaletteTitle"].Value" @onkeydown="HandleKeyDownAsync" @onkeydown:stopPropagation="true">
      <FluentSearch @ref="_searchRef"
                    Value="@_localQuery"
                    ValueChanged="OnQueryChangedAsync"
                    Placeholder="@Localizer["PaletteSearchPlaceholder"].Value"
                    aria-controls="fc-palette-results" />

      <FcPaletteResultList Id="fc-palette-results"
                          Results="@PaletteState.Value.Results"
                          SelectedIndex="@PaletteState.Value.SelectedIndex"
                          OnSelectionChanged="OnSelectionClicked" />

      @* D15 empty-then-populate live region — initial render carries empty text; OnAfterRenderAsync(firstRender) populates on next tick so the DOM mutation triggers SR announce. *@
      <div class="fc-sr-only" role="status" aria-live="polite" aria-atomic="true">@_liveRegionText</div>
    </div>
    ```

- [ ] **5.2** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs`:
    - `[CascadingParameter] IDialogInstance? Dialog`
    - `[Inject] IState<FrontComposerCommandPaletteState> PaletteState`
    - `[Inject] IDispatcher Dispatcher`
    - `[Inject] IUlidFactory UlidFactory`
    - `[Inject] IStringLocalizer<FcShellResources> Localizer`
    - Private `_localQuery` field + `_searchRef FluentSearch?` + `_liveRegionText` field (D15 — starts as `string.Empty`).
    - **`OnAfterRenderAsync(firstRender)`** (D15 empty-then-populate choreography + F1 focus):
        1. If `firstRender`: `await _searchRef.FocusAsync().ConfigureAwait(false)` (auto-focus — AC2 F1).
        2. If `firstRender`: `await Task.Yield();` then update `_liveRegionText = ComputeLiveRegionText(PaletteState.Value.Results);` then `StateHasChanged();` — forces the empty-to-populated mutation so SR engines pick up the announce (D15).
        3. On subsequent renders: update `_liveRegionText` from state subscription — mutation already flows through naturally because the element is mounted.
    - **D15 anti-regression inline comment (required)** — above the `_liveRegionText` field declaration AND immediately above the `await Task.Yield(); StateHasChanged();` line, paste the D15 anti-regression comment verbatim (see D15 rationale). This prevents a future "simplify OnAfterRenderAsync" refactor from collapsing the two ticks into one and silently breaking SR announces.
    - **`ComputeLiveRegionText`** helper: `PaletteState.Value.Results.IsEmpty ? Localizer["PaletteNoResultsText"].Value : string.Format(Localizer["PaletteResultCountTemplate"].Value, PaletteState.Value.Results.Length)` — AC5 empty-state semantic distinction ("No matches found" instead of "0 results").
    - **State subscription**: override `OnInitialized` to subscribe to `PaletteState.StateChanged` — on each tick, recompute `_liveRegionText` and call `InvokeAsync(StateHasChanged)`. Unsubscribe in `Dispose`.
    - `OnQueryChangedAsync(string newQuery)`: set `_localQuery`; `Dispatcher.Dispatch(new PaletteQueryChangedAction(UlidFactory.NewUlid(), newQuery))`.
    - `OnSelectionClicked(int index)`: `Dispatcher.Dispatch(new PaletteResultActivatedAction(index))`; close dialog after dispatch via `Dialog?.CloseAsync()`.
    - `HandleKeyDownAsync(KeyboardEventArgs e)`: on `Escape` → set `_explicitlyClosed = true`; dispatch `PaletteClosedAction`; call `Dialog?.CloseAsync()`; on `ArrowDown` → `PaletteSelectionMovedAction(+1)`; on `ArrowUp` → `PaletteSelectionMovedAction(-1)`; on `Enter` → set `_explicitlyClosed = true`; dispatch `PaletteResultActivatedAction(PaletteState.Value.SelectedIndex)`; call `Dialog?.CloseAsync()` (unless the activation sentinel `@shortcuts` D23 refills — in that case DO NOT set `_explicitlyClosed` and DO NOT close). ArrowDown / ArrowUp / Enter call `e.PreventDefault()` via `@onkeydown:preventDefault` attribute. **Dialog close-call ownership (D11)**: the component is the sole call-site for `Dialog.CloseAsync()`. The `HandlePaletteResultActivated` effect does NOT call `Dialog.CloseAsync` — it only dispatches `PaletteClosedAction` + navigation. The component's click / keydown handlers own the close call so there's exactly one `Dialog.CloseAsync` invocation per user-initiated close.
    - **Focus management (AC2 F2)**: arrow keys NEVER move real focus — the handler only dispatches `PaletteSelectionMovedAction` and updates `aria-activedescendant`. Real focus remains on `FluentSearch`. No `li.Focus()` calls anywhere in this component.
    - **Focus management (AC2 F4 fallback)**: on `DisposeAsync`, if `Dialog?.Result` indicates same-page-nav, invoke a `fcFocusBodyAsync()` JS helper (create `src/Hexalith.FrontComposer.Shell/wwwroot/fc-focus.js` with `export function focusBody() { document.body.focus({ preventScroll: true }); }`) via `IJSRuntime` — ensures no orphaned focus.
- [ ] **5.3** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.css`:
    ```css
    .fc-palette-root {
        display: flex;
        flex-direction: column;
        gap: 8px;
        padding: 12px;
        min-height: 400px;
    }
    .fc-palette-root fluent-search {
        width: 100%;
    }
    /* .fc-sr-only defined in FrontComposerShell.razor.css (Story 3-3 D20 shared utility). */
    ```
- [ ] **5.4** Implement `Dispose` / `DisposeAsync` — unsubscribe from `PaletteState.StateChanged`; dispose JS module reference if loaded (`fc-focus.js` from 5.2 F4 fallback). **D11 dismiss-path catch-all (post-elicitation Red Team R2 hardening)**: if `_explicitlyClosed == false`, dispatch `PaletteClosedAction(UlidFactory.NewUlid())` from `DisposeAsync` to cover X-button / backdrop / circuit-disconnect dismiss paths that did not route through the component's key handlers. The dispatch MUST be wrapped in `try { Dispatcher.Dispatch(new PaletteClosedAction(...)); } catch (ObjectDisposedException) { /* circuit disposed — state gone, nothing to update */ }` for dirty-disconnect robustness (the Dispatcher may throw on a torn-down circuit). Chain `base.DisposeAsync` per `FluxorComponent` semantics after the catch-all + JS disposal.
- [ ] **5.5** Create `src/Hexalith.FrontComposer.Shell/wwwroot/fc-focus.js` — single-function module for the AC2 F4 fallback. `export function focusBody() { document.body.focus({ preventScroll: true }); }`. Script-collocated with the Shell assembly per the existing wwwroot convention.

**Exit:** `IDialogService.ShowDialogAsync<FcCommandPalette>(...)` opens a dialog with search + result list + aria-live region that renders empty on first paint, populates on next tick, and updates on every query change.

---

## Task 6 — `FcPaletteResultList.razor` categorised listbox

**Decisions:** D14, D15, D16; ADR-044. **ACs:** AC3, AC5, AC6, AC7.

- [ ] **6.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor`:

    ```razor
    @namespace Hexalith.FrontComposer.Shell.Components.Layout
    @using Hexalith.FrontComposer.Contracts.Badges
    @using Hexalith.FrontComposer.Shell.State.CommandPalette
    @using Microsoft.FluentUI.AspNetCore.Components
    @implements IAsyncDisposable
    @inject IStringLocalizer<FcShellResources> Localizer

    <div id="@Id" class="fc-palette-results">
      <ul role="listbox" aria-activedescendant="@ResultElementId(SelectedIndex)">
        @foreach (var group in GroupResults(Results))
        {
          <li role="none">
            <h4 id="@GroupHeadingId(group.Category)">@CategoryLabel(group.Category)</h4>
            <ul role="group" aria-labelledby="@GroupHeadingId(group.Category)">
              @foreach (var (result, flatIndex) in group.Items)
              {
                @* AC6 post-elicitation: Shortcut-category rows render aria-disabled="true" so AT announces them as informational-only. *@
                <li role="option" id="@ResultElementId(flatIndex)"
                    aria-selected="@(flatIndex == SelectedIndex)"
                    aria-disabled="@(result.Category == PaletteResultCategory.Shortcut ? "true" : null)"
                    tabindex="-1"
                    class="@(flatIndex == SelectedIndex ? "fc-palette-option-selected" : "fc-palette-option")"
                    @onclick="() => OnSelectionChanged.InvokeAsync(flatIndex)">
                  <span class="fc-palette-option-label">@result.DisplayLabel</span>
                  @if (result.IsInCurrentContext)
                  {
                    <span class="fc-palette-option-context">(in current context)</span>
                  }
                  @if (_badgeCounts is not null && result.Category == PaletteResultCategory.Projection
                        && result.ProjectionType is not null
                        && _badgeCounts.Counts.TryGetValue(result.ProjectionType, out var count))
                  {
                    <FluentBadge Intent="MessageIntent.Info">@count</FluentBadge>
                  }
                  @if (result.Category == PaletteResultCategory.Shortcut)
                  {
                    <span class="fc-palette-option-hint">@Localizer[result.DescriptionKey ?? ""].Value</span>
                  }
                </li>
              }
            </ul>
          </li>
        }
      </ul>
      @if (Results.IsEmpty)
      {
        <p class="fc-palette-noresults">@Localizer["PaletteNoResultsText"].Value</p>
      }
    </div>

    @code {
      [Parameter, EditorRequired] public string Id { get; set; } = "fc-palette-results";
      [Parameter, EditorRequired] public ImmutableArray<PaletteResult> Results { get; set; }
      [Parameter, EditorRequired] public int SelectedIndex { get; set; }
      [Parameter, EditorRequired] public EventCallback<int> OnSelectionChanged { get; set; }
      [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;  // D16 / ADR-044 — optional service resolution.

      private IBadgeCountService? _badgeCounts;
      private IDisposable? _badgeSubscription;
      protected override void OnInitialized() {
        _badgeCounts = ServiceProvider.GetService<IBadgeCountService>();  // null when unregistered — Story 3-5 registers it.
        if (_badgeCounts is not null) {
          _badgeSubscription = _badgeCounts.CountChanged.Subscribe(_ => InvokeAsync(StateHasChanged));
        }
      }
      public ValueTask DisposeAsync() {
        _badgeSubscription?.Dispose();
        return ValueTask.CompletedTask;
      }
      private string ResultElementId(int i) => $"fc-palette-result-{i}";
      private string GroupHeadingId(PaletteResultCategory c) => $"fc-palette-group-{c.ToString().ToLowerInvariant()}";
      private string CategoryLabel(PaletteResultCategory c) => c switch {
        PaletteResultCategory.Projection => Localizer["PaletteCategoryProjections"].Value,
        PaletteResultCategory.Command => Localizer["PaletteCategoryCommands"].Value,
        PaletteResultCategory.Recent => Localizer["PaletteCategoryRecent"].Value,
        PaletteResultCategory.Shortcut => Localizer["ShortcutsCategoryLabel"].Value,
        _ => c.ToString(),
      };
      // GroupResults: yield (Category, IEnumerable<(PaletteResult, int flatIndex)>) tuples in Projection / Command / Recent / Shortcut order,
      // preserving the global flatIndex across groups so aria-activedescendant + SelectedIndex match.
    }
    ```

- [ ] **6.2** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor.css` — scoped CSS for the options list. Use `var(--fc-spacing-unit)` multipliers (ADR-041 compliance): `.fc-palette-results { padding: calc(var(--fc-spacing-unit) * 2); } .fc-palette-option-selected { background: var(--colorNeutralBackground2); }` etc.
- [ ] **6.3** Define `IBadgeCountService` + `BadgeCountChangedArgs` in `src/Hexalith.FrontComposer.Contracts/Badges/IBadgeCountService.cs` per ADR-044 contract-freeze. Story 3-5 implements against this interface.

**Exit:** `FcPaletteResultListTests` green — render-without-badges + render-with-badges scenarios pass.

---

## Task 7 — `FcPaletteTriggerButton.razor` header icon + `HeaderEnd` auto-populate update

**Decisions:** D18. **ACs:** AC2.

- [ ] **7.1** Create `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteTriggerButton.razor` — `<FluentButton Appearance="ButtonAppearance.Stealth" OnClick="OpenAsync" Title="@Localizer["PaletteTriggerAriaLabel"].Value"><FluentIcon Value="@(new Icons.Regular.Size20.Search())" /></FluentButton>`. Code-behind injects `IDispatcher` + `IUlidFactory` + `IDialogService` + `IStringLocalizer`; `OpenAsync` dispatches `PaletteOpenedAction` + calls `DialogService.ShowDialogAsync<FcCommandPalette>(new DialogParameters { Modal = true, Width = "600px", Title = Localizer["CommandPaletteTitle"].Value })`.
- [ ] **7.2** Update `FrontComposerShell.razor` `HeaderEnd` auto-populate branch (Story 3-3 D12): when `HeaderEnd is null`, render `<FcPaletteTriggerButton />` ahead of `<FcSettingsButton />` — both inside a `<FluentStack Horizontal="true">` at gap="4px".
- [ ] **7.3** Confirm `FrontComposerShellParameterSurfaceTests.verified.txt` UNCHANGED — parameter count stays at 9 (D18 auto-populate is a render-time behaviour, not a new parameter).

**Exit:** Counter.Web header shows the palette icon at top-right, to the LEFT of the settings gear.

---

## Task 8 — Story 3-3 `Ctrl+,` migration + `FrontComposerShell` global key router

**Decisions:** D1, D5; AC8 migration contract. **ACs:** AC1, AC2, AC8.

- [ ] **8.1** Update `FrontComposerShell.razor.cs` `HandleGlobalKeyDown` method:
    - DELETE the Story 3-3 inline `if (e.Key == "," && e.CtrlKey && ...)` branch.
    - Add text-input-target guard per D5: if `e.TargetElement`-equivalent is `<input>` / `<textarea>` / `[contenteditable]` AND no modifier key is present, return early.
        - Blazor Server caveat: `KeyboardEventArgs` does not carry the target element — the guard needs the event to bubble up to `.fc-shell-root` with `@onkeydown:stopPropagation="false"` at input sites so the shell receives the event AND can identify the target via `event.target` on the JS side. Implementation sketch: add a `[JSInvokable]` JS-side helper or accept the simpler "modifier-required for palette + chord, bare letters always go to inputs" rule — the simpler rule is good enough for v1 because the only bare-letter shortcut we ship is `g h` which is a documented power-user opt-in.
        - Simpler implementation: in `HandleGlobalKeyDown`, if `e.Key` is a single lowercase letter AND no `Ctrl` / `Shift` modifier, skip invocation. This misses the "user in a text field presses Ctrl+Shift+D" edge but matches the registered-shortcut profile.
    - Call `await _shortcuts.TryInvokeAsync(e)` — the return value is discarded (we don't propagate handled/not-handled up the DOM here; the shell is the last stop).
- [ ] **8.2** Update `FrontComposerShell.razor.cs`:
    - Add `[Inject] private IShortcutService ShortcutService { get; set; } = default!;`
    - Add `[Inject] private FrontComposerShortcutRegistrar Registrar { get; set; } = default!;`
    - In `OnAfterRenderAsync(firstRender: true)` (existing method), add `await Registrar.RegisterShellDefaultsAsync().ConfigureAwait(false)` call (before or after the existing `ApplyThemeAsync` call — order doesn't matter).
- [ ] **8.3** Remove from `FrontComposerShell.razor.cs`:
    - `[Inject] private IDialogService DialogService` — no longer directly used (moved to registrar).
    - `[Inject] private IStringLocalizer<FcShellResources> ShellLocalizer` — retained only if still used elsewhere; if not, remove.
    - The inline dialog-open call inside the removed branch.
- [ ] **8.4** DELETE `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/CtrlCommaSingleBindingTest.cs` — explicit retirement per Story 3-3 D16 migration contract. Commit message should cite Story 3-3 D16 + Story 3-4 D5 + AC8.
- [ ] **8.5** Add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — append 3 new tests: `PaletteTriggerAutoPopulatesAheadOfSettings`, `CtrlCommaInvokesRegisteredShortcut` (via mock `IShortcutService`), `CtrlKInvokesPaletteViaRegisteredShortcut`.

**Exit:** Counter.Web still opens settings on `Ctrl+,` + also opens palette on `Ctrl+K`. Story 3-3 `CtrlCommaSingleBindingTest` deleted without regression.

---

## Task 9 — Resource keys (EN + FR parity)

**Decisions:** D14. **ACs:** AC2, AC5, AC6.

- [ ] **9.1** Add 14 new keys (12 original + 2 D23 additions) to `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`:
    - `PaletteTriggerAriaLabel` = "Open command palette"
    - `CommandPaletteTitle` = "Command palette"
    - `PaletteSearchPlaceholder` = "Search projections, commands, recent… (type ? for shortcuts)"
    - `PaletteCategoryProjections` = "Projections"
    - `PaletteCategoryCommands` = "Commands"
    - `PaletteCategoryRecent` = "Recent"
    - `PaletteResultCountTemplate` = "{0} results"
    - `PaletteNoResultsText` = "No matches found"
    - `ShortcutsCategoryLabel` = "Keyboard shortcuts"
    - `PaletteShortcutDescription` = "Open command palette"
    - `SettingsShortcutDescription` = "Open settings"
    - `HomeShortcutDescription` = "Go to home"
    - `KeyboardShortcutsCommandLabel` = "Keyboard Shortcuts" (D23 — synthetic default Commands entry)
    - `KeyboardShortcutsCommandDescription` = "View all keyboard shortcuts" (D23 — entry description)
- [ ] **9.2** Add the same 14 keys to `FcShellResources.fr.resx`:
    - `PaletteTriggerAriaLabel` = "Ouvrir la palette de commandes"
    - `CommandPaletteTitle` = "Palette de commandes"
    - `PaletteSearchPlaceholder` = "Rechercher projections, commandes, récents… (tapez ? pour raccourcis)"
    - `PaletteCategoryProjections` = "Projections"
    - `PaletteCategoryCommands` = "Commandes"
    - `PaletteCategoryRecent` = "Récents"
    - `PaletteResultCountTemplate` = "{0} résultats"
    - `PaletteNoResultsText` = "Aucun résultat trouvé"
    - `ShortcutsCategoryLabel` = "Raccourcis clavier"
    - `PaletteShortcutDescription` = "Ouvrir la palette de commandes"
    - `SettingsShortcutDescription` = "Ouvrir les paramètres"
    - `HomeShortcutDescription` = "Aller à l'accueil"
    - `KeyboardShortcutsCommandLabel` = "Raccourcis clavier"
    - `KeyboardShortcutsCommandDescription` = "Voir tous les raccourcis clavier"

**Exit:** `CanonicalKeysHaveFrenchCounterparts` parity test passes. Both resx files build without warnings. Total: 14 new keys × 2 locales = 28 new strings (up from 24).

---

## Task 10 — Tests

**Decisions:** All. **ACs:** All.

- [ ] **10.1** Create `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` — 6 tests (baseline): register-then-invoke, duplicate-register-last-writer-wins + HFC2107 log, disposed-registration-no-longer-fires, chord-gh-invokes-within-window, chord-gh-nonmatching-second-falls-back, concurrent-register-is-thread-safe. **All chord-related tests MUST inject `FakeTimeProvider`** (D22) and advance virtual time via `fakeTime.Advance(TimeSpan.FromMilliseconds(N))`.
- [ ] **10.1c** Add to `ShortcutServiceTests.cs` — D4 chord boundary tests per Murat party-mode review:
    - `Chord_gh_FiresWhenSecondKeyArrivesAt1499ms` — advance 1499ms, press `h`, assert handler fires.
    - `Chord_gh_DoesNotFireWhenSecondKeyArrivesAt1501ms` — advance 1501ms, press `h`, assert handler does NOT fire (pending field cleared; `h` evaluated as fresh single-key).
    - `Chord_gh_ClearsPendingFieldAfterTimeoutEvenWithoutSecondKey` — press `g`, advance 1500ms, press `g` again, assert NEW 1500ms window starts (not a `g g` lookup); no log fires (D4 sub-decision b silent clear).
- [ ] **10.1d** Add to `ShortcutServiceTests.cs` — D4 sub-decision coverage:
    - `Chord_RepeatPrefixBeforeTimeout_OverwritesPendingField` (sub-decision c): press `g`, advance 800ms, press `g`, advance 800ms (1600ms total), press `h` — chord DOES fire because pending was overwritten at 800ms (fresh 1500ms window started).
    - `Chord_ModifierBindingDuringPending_FiresAndClears` (sub-decision d): press `g`, advance 300ms, press `Ctrl+K` — palette-open handler fires, `_pendingFirstKey` cleared; subsequent `h` at 600ms does NOT complete a chord.
- [ ] **10.2** Create `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutBindingNormalizeTests.cs` — 4 `[Theory]` cases: lowercase + modifier-reorder (`SHIFT+CTRL+K` → `ctrl+shift+k`), chord spacing (`g  h` → `g h`), invalid bare letter throws (`"g"` single key), invalid empty string throws. **Plus** `tests/Hexalith.FrontComposer.Shell.Tests/Routing/CommandRouteBuilderTests.cs` — 3 `[Theory]` cases for D21 kebab helper: `"SubmitOrderCommand"` → `"submit-order-command"`, `"IncrementCommand"` → `"increment-command"`, `"XMLParser"` → `"xml-parser"` (consecutive uppercase edge) + 1 `BuildRoute` test asserting `BuildRoute("Commerce", "SubmitOrderCommand")` → `"/domain/commerce/submit-order-command"`. (Location under `Routing/` mirrors the production file location per D21 post-elicitation.)
- [ ] **10.1e** Create `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/HFC2107ShortcutConflictLogTest.cs` — bUnit-less `ILogger` mock assertion on the structured payload fields `{Binding, PreviousDescriptionKey, NewDescriptionKey, CallSiteFile, CallSiteLine}`.
- [ ] **10.1f** Create `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/FrontComposerShortcutRegistrarTests.cs` — **D24 idempotency coverage**: `RegisterShellDefaultsAsync_IsIdempotent_WithinSameInstance` (two consecutive calls result in exactly 3 `IShortcutService.Register` invocations via a mock — not 6), `RegisterShellDefaultsAsync_RegistersThreeShellBindings` (asserts all three shortcuts are registered with their expected description keys).
- [ ] **10.1g** Add to `ShortcutServiceTests.cs` — **D1 handler-exception coverage + HFC2108**: `TryInvokeAsync_WhenHandlerThrows_LogsHFC2108AndReturnsTrue` (register a handler that throws, invoke it, assert log fired + return value is true + no exception propagates); `Register_ThrowsArgumentException_OnNullOrEmptyBinding` (`[Theory]` for `null`, `""`, `"  "`); `Register_ThrowsArgumentException_OnNullOrEmptyDescriptionKey` (same Theory); `Register_ThrowsArgumentException_OnNullHandler`.
- [ ] **10.3** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerTests.cs` — 5 `[Theory]` cases: exact-prefix (Score ≥ 100), contains (Score ≥ 50), subsequence (Score ≥ 10), no-match (Score == 0), case-insensitivity.
- [ ] **10.3a** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerPropertyTests.cs` — 2 FsCheck properties: `ScoreIsDeterministic` (same inputs → same Score across 100 random queries), `ScoreIsMonotonicOnExactPrefixLength` (longer exact-prefix query → higher score, within the prefix band).
- [ ] **10.3b** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/PaletteScorerBench.cs` — BenchmarkDotNet `[Benchmark]` `Score_1000Candidates` target < 100 ms total. Skip in CI via `#if !CI` or `[Trait("Category", "Performance")]` per Story 1-8 precedent (Story 1-8 deferred-work third-review notes — perf tests opt-in).
- [ ] **10.4** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteReducerTests.cs` — 6 tests: open, close, query-changed, results-computed (IsOpen=true → assigns), **`OnPaletteResultsComputed_WhenPaletteClosed_NoOps` (D20 stale-result reducer guard)**, selection-moved clamp, hydrate.
- [ ] **10.4b** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs` — tests (all use `FakeTimeProvider` per D22 + single-effects-instance reused across multi-dispatch per Murat review):
    - `DebounceCancelsEarlierKeystroke` — two `PaletteQueryChangedAction` dispatches; advance 100ms between them; advance 150ms more; only ONE `PaletteResultsComputedAction` dispatched (the second query's).
    - `ShortcutsQueryBypassesScorer` — query "shortcuts" → results come from `IShortcutService.GetRegistrations()`, scorer is NOT invoked (assert via mock `PaletteScorer` — actually the scorer is pure static, so assert indirectly: the result count equals registration count + no registry enumeration via `_registry` mock call count).
    - `ShortcutsQueryAliases_CanonicaliseTo_Shortcuts` `[Theory]` — aliases `?`, `help`, `keys`, `kb`, `shortcut` all produce the same shortcut-reference result set (D23).
    - `SyntheticKeyboardShortcutsCommandEntry_AppearsInDefaultOpen` — dispatch `PaletteOpenedAction`, assert results include synthetic entry with `CommandTypeName == "@shortcuts"` (D23).
    - `ContextualBonusAppliesToMatchingBoundedContext`.
    - `NoContextualBonus_WhenNavigationContextIsNull`.
    - `HydrateDoesNotRePersist` (ADR-038 mirror).
    - **`HandlePaletteClosed_CancelsInFlightQueryCts`** (D20 upstream guard) — dispatch query-changed, advance 50ms, dispatch palette-closed, advance 200ms total — assert NO `PaletteResultsComputedAction` fired (effect swallowed the `OperationCanceledException`).
    - **`HandleResultsComputedRace_ReducerNoOps_WhenPaletteClosedBetweenDispatchAndHandle`** (D20 downstream guard, integration) — simulate the cancellation race by dispatching `PaletteResultsComputedAction` directly against a state where `IsOpen = false`; assert resulting state is unchanged (the reducer refused the assignment).
- [ ] **10.4c** Add to `CommandPaletteEffectsTests.cs`:
    - `HandlePaletteResultActivated_NavigatesAndClosesAndUpdatesRecent` (Category=Projection).
    - `HandlePaletteResultActivated_NavigatesToKebabCommandRoute_ForCommandCategory` (D21 — assert URL is `/domain/{kebab-bc}/{kebab-cmd}`).
    - `HandlePaletteResultActivated_SyntheticShortcutsEntry_RefillsPaletteInsteadOfNavigating` (D23 — assert `PaletteQueryChangedAction("shortcuts")` dispatched AND palette stays open).
    - `HandlePaletteQueryChanged_FiltersUnreachableCommands_ViaHasFullPageRoute` (D21 — mock `IFrontComposerRegistry.HasFullPageRoute(...)` to return false for one candidate, assert it's excluded from results).
- [ ] **10.5** Create `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs` — 4 tests mirroring Story 3-3 `DensityEffectsScopeTests`: persist-on-valid-scope, skip + HFC2105 on null tenant, skip on null user, skip on whitespace scope.
- [ ] **10.7** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs` — 4 bUnit tests: renders dialog with role + aria-label, auto-focuses search on first render (assert `FluentSearch.FocusAsync` invoked via test-double), arrow navigation dispatches selection, Enter dispatches activation, Escape closes.
- [ ] **10.7b** Add to `FcCommandPaletteTests.cs` — `AriaLiveRegionRendersEmptyOnFirstRenderThenPopulatesOnNextTick` (D15 mutation timing — assert the rendered DOM at `firstRender==true` has empty text inside `role="status"`, then after a `WaitForState` yield the text equals the expected count template) + `AriaLivePoliteAnnouncesResultCountOnQueryChange` + `AriaLiveAnnouncesNoMatchesForEmptyResults`.
- [ ] **10.7c** Add to `FcCommandPaletteTests.cs` — `FocusManagement_ArrowsKeepFocusOnSearchInput` (AC2 F2 — dispatch ArrowDown, assert `document.activeElement` equivalent is still the FluentSearch via `IElement.IsFocused` — bUnit's DOM focus model) + `FocusManagement_EscapeRestoresFocusToInvoker` (AC2 F5) + `FocusManagement_ActivateSentinelDoesNotClosePalette` (AC2 + D23 — activating `@shortcuts` refills, does not close).
- [ ] **10.7d** Add to `FcCommandPaletteTests.cs` — **`PaletteDismissPaths_AllDispatchPaletteClosedAction`** (D11 + Winston party-mode push-back) `[Theory]` rows: X-button click, backdrop click (if FluentDialog supports), Escape key, programmatic `Dialog.CloseAsync()`. Each path asserts `PaletteClosedAction` dispatched AND `state.IsOpen == false` after the close. Cannot test adopter-requested close via FluentUI MVP — `IDialogInstance.CloseAsync` covers the programmatic case.
- [ ] **10.8** Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs` — 5 bUnit tests: renders 3 categories with headings, renders-without-badges-when-service-null (AC7), renders-badges-when-service-registered (AC7 positive), no-badge-placeholder-when-projection-unknown (AC7), renders-shortcut-category-with-descriptions (AC6). Plus `SyntheticShortcutsEntryActivatesShortcutsQuery` (D23 + AC6) and `ShortcutRowsCarryAriaDisabledTrue` (AC6 post-elicitation — assert Shortcut-category `<li role="option">` elements have `aria-disabled="true"` attribute).
- [ ] **10.9** ~~Create `FcPaletteTriggerButtonTests.cs`~~ **DELETED per Murat party-mode review** — the trigger-button-click-dispatches-action assertion is double-covered by `FrontComposerShellTests.PaletteTriggerAutoPopulatesAheadOfSettings` (10.10) which renders the button inside the shell and asserts click behaviour. Pick one: the shell integration test stays; the standalone trigger-button unit test is cut.
- [ ] **10.10** Modify `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` — append `PaletteTriggerAutoPopulatesAheadOfSettings` (exercises both the render integration AND the click-dispatches behaviour — subsumes deleted 10.9).
- [ ] **10.10a** Modify `FrontComposerShellTests.cs` — append `CtrlCommaInvokesRegisteredShortcut` (replaces deleted `CtrlCommaSingleBindingTest`), `CtrlKInvokesPaletteViaRegisteredShortcut`, `TextInputTargetGuardSkipsBareLetterChords`.
- [ ] **10.11** Modify `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs` — append 6 batched key-lookup methods covering the 12 new keys (Palette-dialog group {Title+Trigger+Placeholder}, Category group {Projections+Commands+Recent+Shortcuts}, Result-count + No-results group, Shortcut-descriptions group {Palette+Settings+Home}, FR parity spot-check).
- [ ] **10.12** Modify or add `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs` — Playwright test: boot Counter.Web, press Ctrl+K, type "cou", assert `PaletteCategoryProjections` heading visible + Counter row rendered, press Enter, assert navigation to `/counter`. HARDENED per Story 3-3 Task 10.12 precedent: `prefers-reduced-motion: reduce` in context, `expect.poll` with timeout, DOM-attribute-on-palette-root assertion. **Plus** `locator.waitFor({ state: 'visible' })` on the palette root BEFORE the attribute assertion (Murat party-mode: attribute-present-on-hidden-element is a false positive). Do NOT add `networkidle` waits — Blazor Server + SignalR keeps the connection warm, `networkidle` never fires or fires on unrelated traffic.
- [ ] **10.13** PR-review gate — confirm test count 36-42, bottom-quartile Occam / Matrix scoring applied (L07). Net delta vs original: +5 chord boundary / sub-decision tests (D4+D22), +2 discoverable-shortcuts tests (D23), +2 stale-result guard tests (D20), +1 kebab-case helper theory (D21), +2 focus-management tests (AC2 F2/F5), +1 sentinel-no-close test (D23), +2 live-region mutation-timing tests (D15), +1 dismiss-paths Theory (D11 resync), -1 deleted trigger-button standalone test (Murat dedup). Net ~+15 tests over original ~34 target = ~49 tests. Budget permits — the tests are risk-proportionate per Murat review; if PR review exceeds 42 materially, apply Occam/Matrix trim to duplicate-risk entries.

**Exit:** `dotnet test` green. Test count delta ≈ +49 (check against `test_baseline_pre_3_4` captured in Task 0.1).

---

## Task 11 — Zero-warning gate + regression baseline + Aspire MCP verification

**Decisions:** All. **ACs:** All.

- [ ] **11.1** `dotnet build --warnaserror` passes with zero warnings.
- [ ] **11.2** `dotnet test` green end-to-end (including Story 3-1 + 3-2 + 3-3 regression baselines, minus the deleted `CtrlCommaSingleBindingTest`).
- [ ] **11.3** Counter.Web automated verification via Aspire MCP + Claude-in-Chrome per `memory/feedback_no_manual_validation.md`:
    - (a) Press Ctrl+K → palette dialog opens with search focused.
    - (b) Type "cou" → Projection "Counter" appears under Projections heading.
    - (c) Press Enter → navigates to `/counter`.
    - (d) Press Ctrl+K again → Counter appears under Recent category (verifying the ring buffer persisted across the navigation).
    - (e) Type "shortcuts" → shortcuts list appears with Ctrl+K, Ctrl+,, g h rows + localised descriptions.
    - (f) Ctrl+, still opens settings dialog (Story 3-3 regression check).
    - (g) Focus an `<input>` element → press "g" → no navigation (D5 text-input guard).
    - (h) `g h` chord from focused shell root → navigates home.
- [ ] **11.4** Record the test-count delta, Aspire MCP observations, and the next-story handoff in `dev-agent-record.md`.

**Exit:** Story status transitions from `in-progress` → `review` via the dev-story workflow.

### Review Findings (2026-04-20 — bmad-code-review chunk 1)

- [x] `[Review][Patch]` `IFrontComposerRegistry.HasFullPageRoute` compatibility issue resolved by moving the fallback into a companion route-aware interface + extension method, keeping the original registry contract stable for pre-3-4 implementers.
- [x] `[Review][Patch]` `FrontComposerShortcutRegistrar.OpenPaletteAsync` now rolls back with `PaletteClosedAction` when dialog construction fails, so `Ctrl+K` does not leave the palette state stuck open.
- [x] `[Review][Patch]` `ShortcutService.TryInvokeBindingAsync` now suppresses/logs `OperationCanceledException` the same way as other handler faults, preventing shortcut exceptions from bubbling into the Blazor error path.
- [x] `[Review][Patch]` Duplicate-shortcut conflict telemetry now includes both the overwritten and replacing call sites in the structured payload.
- [x] `[Review][Patch]` `CommandRouteBuilder.IsInternalRoute` no longer rejects valid app-relative URLs merely because they contain `:` in the path/query/fragment.
- [x] `[Review][Patch]` The shortcut chord timer callback is now generation-guarded so stale timer callbacks cannot clear a freshly renewed pending chord window.
- [x] `[Review][Patch]` The shortcut logging tests now assert the diagnostic ID / log level / structured payload contract for both conflict and handler-fault telemetry.

### Review Findings (2026-04-20 — bmad-code-review chunk 2)

- [x] `[Review][Patch]` Shell-level bare-letter suppression blocks the `g h` home chord entirely [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:216-224`]
- [x] `[Review][Patch]` Projection results synthesize non-canonical routes instead of using the existing projection route builder [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:167-205`]
- [x] `[Review][Patch]` `CommandRouteBuilder` strips the `Command` suffix even though D21 requires the full kebab-cased command type name [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:20-35`]
- [x] `[Review][Patch]` Shortcut-reference rows remain activatable and currently dismiss the palette despite being informational-only [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:401-420`]
- [x] `[Review][Patch]` Shell and palette key handlers do not reliably suppress native browser/input default behavior for their shortcuts [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:23`; `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor:15-17`]
- [x] `[Review][Patch]` `CurrentBoundedContext` was added to navigation state but is never wired to real route changes [`src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextChangedAction.cs:1-8`]
- [x] `[Review][Patch]` Clearing the query wipes the pre-populated palette view instead of restoring the default suggestions [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:260-372`]
- [x] `[Review][Patch]` Negative fuzzy scores are treated as valid matches, so long-gap subsequence matches can leak into results [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs:47-61`]
- [x] `[Review][Patch]` The badge-count seam diverges from the story’s Type-based Story 3-5 contract by freezing projection keys as strings [`src/Hexalith.FrontComposer.Contracts/Badges/IBadgeCountService.cs:1-29`]
- [x] `[Review][Patch]` `FcCommandPalette` omits the specified search-to-results accessibility wiring, and the new tests do not cover the required focus/live-region behavior [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor:15-23`; `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs:1-36`]

### Review Findings (2026-04-20 — bmad-code-review chunk 2 rerun)

- [x] `[Review][Patch]` Synthetic “Keyboard Shortcuts” default entry never renders its required description text [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:44-47`]
- [x] `[Review][Patch]` `FcCommandPalette` root still omits the task-5.1 dialog semantics (`role="dialog"`, explicit `aria-label`, keydown stop-propagation) [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor:16-18`]
- [x] `[Review][Patch]` Same-page palette activation still lacks the AC2 F4 focus-body fallback described by the story (`fc-focus.js` + dispose-path hook) [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:102-125`]
- [x] `[Review][Patch]` `CommandRouteBuilder.IsInternalRoute` still accepts app-relative URLs that embed external schemes in the query or fragment [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:84-99`]
- [x] `[Review][Patch]` `HandlePaletteQueryChanged` does not trim non-alias queries before scoring, so copied queries with leading/trailing spaces can false-negative [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:232-235`]
- [x] `[Review][Patch]` `registerShellKeyFilter` suppresses shifted shell shortcuts (`Ctrl+Shift+K`, `Ctrl+Shift+,`) even though the shortcut service has no matching registration [`src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js:46-53`]

### Review Findings (2026-04-21 — bmad-code-review full re-review)

_Parallel layers: Blind Hunter (adversarial) + Edge Case Hunter (branch/boundary) + Acceptance Auditor (spec drift)._


**Resolution (2026-04-21 end of session):** 7 decisions resolved; 21 patches attempted — 19 applied clean, 2 reverted on test evidence (`BoundedContextRouteParser` segments[0] == "domain" anchor contradicted `FrontComposerShellTests.RouteChangesUpdateCurrentBoundedContextInNavigationState` which mandates the Counter sample's bare `/counter/...` BC convention; `IsInternalRoute` path-only scheme scan contradicted `CommandRouteBuilderTests.IsInternalRoute_RejectsTamperedOrAbsoluteUrls` which asserts D10 defence-in-depth must reject scheme tokens anywhere in the URL). `dotnet build --warnaserror` clean; all 613 tests still passing.

**Decision outcomes:** D24 ratified `Interlocked.Exchange` (critical-decisions amended); HFC diagnostic IDs renumbered 2107→2108 (ShortcutConflict) + 2108→2109 (ShortcutHandlerFault) across spec pack to match shipped code; D21 companion-interface (`IFrontComposerFullPageRouteRegistry` + extension) ratified in critical-decisions + cross-story table; G20 amended to document that `TryAddScoped` is the framework default AND adopter `AddScoped` override still works via DI last-wins; new G23 added for NFR5 benchmark deferral with explicit revisit triggers (Story 10-4 perf-gate pipeline OR adopter-reported palette lag); DN6 `ValidateManifests` accepted as inert placeholder with inline comment + exception-unreachable XML doc; AC1 heading severity corrected from "Warning" to "Information" (code already correct per D3 rationale).


**Decisions needed (must be resolved before patches)**

- [x] `[Review][Decision]` **D24 Interlocked.Exchange vs "plain bool" stance** — `FrontComposerShortcutRegistrar._registered` now uses `int` + `Interlocked.Exchange(ref _registered, 1)`, directly contradicting D24's documented rationale ("NOT `Interlocked.Exchange`-guarded — Blazor's render loop is single-dispatcher-thread; reviewers tempted to add thread-safety for defence-in-depth should resist"). Options: (a) roll back to plain `bool _registered; if (_registered) return; _registered = true;`; (b) amend D24 to ratify the hardening.
- [x] `[Review][Decision]` **HFC2107 → HFC2108 renumber unratified in spec** — `FcDiagnosticIds` emits `HFC2108_ShortcutConflict` but AC1 / D19 still cite `HFC2107_ShortcutConflict`. Spike-notes §"Diagnostic ID renumber" documents the rename; critical-decisions.md and acceptance-criteria.md do not. Options: (a) update AC1 + D19 + dev-notes to `HFC2108` (impl wins); (b) revert code to `HFC2107` (spec wins).
- [x] `[Review][Decision]` **D21 companion-interface vs direct-append** — `HasFullPageRoute` ships on new `IFrontComposerFullPageRouteRegistry` + extension method rather than appended to `IFrontComposerRegistry` per D21. DAR claims spike-notes §D21 ratifies this, but critical-decisions-read-first-do-not-revisit.md D21 row is unchanged. Options: (a) ratify in critical-decisions (amend D21 to reference companion interface); (b) move the method onto `IFrontComposerRegistry` as originally specified.
- [x] `[Review][Decision]` **`TryAddScoped<IShortcutService>` vs G20 trust-boundary rationale** — `ServiceCollectionExtensions.AddHexalithFrontComposer` uses `TryAddScoped`, but G20 explicitly calls out `TryAddScoped` as a sealing pattern that "would prevent deliberate adopter override via DI replacement, which breaks D3's last-writer-wins design intent". Adopter-override still works via `services.AddScoped<IShortcutService, Impl>()` after the framework call, but a `TryAddScoped`-style override silently no-ops. Options: (a) switch to `AddScoped` (last-writer-wins); (b) amend G20 to document why `TryAddScoped` is acceptable.
- [x] `[Review][Decision]` **NFR5 benchmark missing without Known-Gap row** — `PaletteScorerBench` is not in the diff; DAR says "well under NFR5 100 ms total-roundtrip budget by inspection" but AC3 success metric names `<100 μs per candidate` which requires measurement. Options: (a) add BenchmarkDotNet test this pass; (b) add a G-row in known-gaps-explicit-not-bugs.md with explicit "inspection-only rationale + revisit trigger" and defer.
- [x] `[Review][Decision]` **`FrontComposerRegistry.ValidateManifests` is tautological** — validator iterates each manifest's `Commands` and calls `HasFullPageRoute(command)`, but `HasFullPageRoute` returns `true` for any command that appears in any manifest. By construction the validator can never throw `HFC1601_ManifestInvalid`. Options: (a) accept as inert placeholder until Story 9-4 implements real routing-metadata enforcement (add inline rationale comment); (b) reshape `HasFullPageRoute` now to consult `IFrontComposerFullPageRouteRegistry` so the validator has real teeth; (c) drop the validator and the HFC1601 branch until 9-4.
- [x] `[Review][Decision]` **AC1 self-contradiction: `Warning` heading vs `Information` body severity** — acceptance-criteria.md AC1 heading says "HFC2107 Warning", body says "Information severity"; code uses `LogLevel.Information` matching D3's explicit rationale. Spec needs one-line fix. Options: (a) drop "Warning" from heading (impl-correct); (b) switch code to `LogLevel.Warning` (impl-incorrect).

**Patches (unambiguous fixes)**

- [x] `[Review][Dismiss-on-verify]` **`BoundedContextRouteParser` classifies any two-segment path as a bounded context** — reverted; `FrontComposerShellTests.RouteChangesUpdateCurrentBoundedContextInNavigationState` mandates the bare `/counter/...` convention — final `return segments.Length >= 2 ? segments[0] : null` converts `/help/topic`, `/admin/users`, `/login/oauth`, `/account/settings` into fake BCs that then feed `IsInCurrentContext` and silently award the `+15` contextual bonus to same-named manifests. Anchor on `segments[0] == "domain"` only. [`src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs:44-49`]
- [x] `[Review][Patch]` **`FcCommandPalette.DisposeAsync` uses `new` instead of `override`** — declared `public new async ValueTask DisposeAsync()`. Blazor/Fluxor tear-down through `ComponentBase`/`IAsyncDisposable` references will not call the hidden override, leaking `_keyboardModule`/`_focusModule` and skipping the D11 catch-all `PaletteClosedAction` on every framework-originated close. Same pattern in `FrontComposerShell.razor.cs`. Switch to `override`. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:~2332`; `FrontComposerShell.razor.cs` DisposeAsync]
- [x] `[Review][Patch]` **`ShortcutRegistrar.OpenPaletteAsync` silent-failure via HFC2109 absorption** — catch-block rethrows after compensating `PaletteClosedAction`; `ShortcutService.TryInvokeBindingAsync` catches all exceptions into an `HFC2109` Warning log and returns `true`. User sees Ctrl+K "work" (true returned) but no dialog appears and no UI feedback. Either propagate to an error-boundary-visible path or raise a user-visible toast on the compensating dispatch. [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:~85-132`; `ShortcutService.cs` TryInvokeBindingAsync]
- [x] `[Review][Dismiss-on-verify]` **`IsInternalRoute` scheme scan rejects benign URLs with scheme tokens in query/fragment** — reverted; D10 defence-in-depth intentionally rejects scheme tokens anywhere in the URL to prevent tampered-storage open-redirect via query/fragment payloads, asserted by `CommandRouteBuilderTests.IsInternalRoute_RejectsTamperedOrAbsoluteUrls` — raw `Contains("http:")`/`"https:"`/`"javascript:"`/`"data:"`/`"mailto:"` scans the whole URL including query/fragment. Legitimate routes like `/search?q=mailto:sales`, `/docs?topic=https:-intro`, `/notes#section:http:` are permanently refused on hydrate (`Reason=Tampered`) and drop out of the recents ring. Strip query+fragment before the scheme scan, or match scheme only at path-start. [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:~84-110`]
- [x] `[Review][Patch]` **Mac `Meta+K` not registered — palette unreachable on macOS** — `ShortcutBinding.TryFromKeyboardEvent` builds `"meta+k"` for Mac Cmd+K, but `FrontComposerShortcutRegistrar` only registers `"ctrl+k"`. `fc-keyboard.js:registerShellKeyFilter` preventDefault gate is `event.ctrlKey && !event.metaKey`, so Mac Chrome falls through to "focus address bar" default. Register `meta+k`/`meta+,` aliases when running on macOS (or always), and widen the JS preventDefault branch. [`src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`; `wwwroot/js/fc-keyboard.js:~46-53`]
- [x] `[Review][Patch]` **`ProjectionTypeResolver` permanently caches negative resolutions** — `_cache.GetOrAdd(typeName, …ResolveCore(name))` stores `null` and re-returns it forever. Plug-in / lazy-loaded / hot-reloaded adopter assemblies loaded after the first palette open stay invisible to `IBadgeCountService` until process restart. Either skip caching nulls, or invalidate on `AssemblyLoad`. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/ProjectionTypeResolver.cs:~38`]
- [x] `[Review][Patch]` **`HandlePaletteResultActivated` dispatches `PaletteClosedAction` AFTER `NavigateTo` with no guard** — `NavigationManager.NavigateTo` may synchronously unwind the current render tree in Blazor Server; subsequent `dispatcher.Dispatch(PaletteClosedAction)` can throw `ObjectDisposedException` against a torn-down dispatcher. Wrap in `try/catch (ObjectDisposedException)` mirroring D11's dismiss-path discipline, or dispatch close BEFORE `NavigateTo`. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~4060-4068`]
- [x] `[Review][Patch]` **`PaletteScorer.Score` can return negative values** — `10 + matched - gaps` goes negative when `gaps > 10 + matched`. Current callers filter `score <= 0`, but the negative return is a latent footgun for future consumers (e.g., ordered min-heap, telemetry aggregates). Clamp to `Math.Max(0, …)` inside the scorer. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs:~50-60`]
- [x] `[Review][Patch]` **`aria-selected` emitted on every option as `"true"`/`"false"` string** — `aria-selected="@((flatIndex == SelectedIndex).ToString().ToLowerInvariant())"` produces per-row `aria-selected="false"` announcements that NVDA reads as "unselected" on each arrow move. Emit `aria-selected` only on the active option (or omit entirely and rely on `aria-activedescendant`). [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:~2699`]
- [x] `[Review][Patch]` **`FrontComposerShell.HandleLocationChanged` only catches `ObjectDisposedException`** — if `BoundedContextRouteParser.Parse` or a disposed Fluxor store throws `InvalidOperationException("Already disposed")`, the navigation event pump propagates and takes the circuit down. Broaden to catch any exception in the detach path, or unsubscribe from `LocationChanged` in `DisposeAsync` before the store can be disposed. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:~1232`]
- [x] `[Review][Patch]` **`ShortcutBinding.TryFromKeyboardEvent` doesn't reject composition / lock keys** — only filters `Control`/`Shift`/`Alt`/`Meta`/`Unidentified`. `Dead` (dead-key composition), `Process` (IME), `AltGraph`, `CapsLock`, `NumLock`, `ScrollLock` flow through and can produce unreachable bindings like `ctrl+capslock`. Extend the rejection list. [`src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs:~83-99`]
- [x] `[Review][Patch]` **`ShortcutService.TryInvokeAsync` doesn't check `KeyboardEventArgs.Repeat`** — holding a chord-prefix letter like `g` auto-repeats, allocating a fresh chord timer per tick, bumping generation, and resetting pending state unpredictably. Return `false` early when `e.Repeat` is true. [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:~94-167`]
- [x] `[Review][Patch]` **`_disposed` check happens outside `_chordSync` lock** — line ~100 reads `_disposed` unlocked, line ~147 allocates chord timer under `_chordSync`. A circuit-teardown that sets `_disposed=true` between the two can leave a timer allocated on a cleared service. Move the `_disposed` check inside the same `lock (_chordSync)` that wraps timer allocation. [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:~94-167`]
- [x] `[Review][Patch]` **`ShortcutBinding.Normalize` accepts non-space whitespace in chord separator** — `binding.Contains(' ')` + `Split(' ', RemoveEmptyEntries)` silently collapses `"g\th"`, `"g\u00A0h"` (NBSP), `"g\rh"` into single-key bindings that then never match. Reject any whitespace ≠ `' '` with `ArgumentException`. [`src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutBinding.cs:~29-44`]
- [x] `[Review][Patch]` **`HandlePaletteQueryChanged` final dispatch lacks post-scoring cancellation check** — after `Task.Delay(150, ct)`, the synchronous scoring loop over all manifests is not cancellable; if the palette closes mid-scoring, `PaletteResultsComputedAction` still dispatches (reducer `!IsOpen` no-ops it) but `dispatcher.Dispatch` itself can throw `ObjectDisposedException` on a torn-down circuit. Add `if (cts.Token.IsCancellationRequested) return;` before the final dispatch. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~222-373`]
- [x] `[Review][Patch]` **`HandlePaletteScopeChanged` doesn't cancel in-flight query CTS** — tenant/user switch during an open palette with an active debounce: the in-flight scoring completes against the post-switch hydrate state, potentially mixing cross-scope results. Cancel the in-flight `_queryCts` before re-running hydration. [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~514-519`]
- [x] `[Review][Patch]` **`CommandRouteBuilder.BuildRoute` not guarded for empty `BoundedContext` on Command activation** — `HandlePaletteResultActivated` calls `BuildRoute(result.BoundedContext, result.CommandTypeName)` for `Category == Command`, but only null-checks `CommandTypeName`. A Command `PaletteResult` with empty `BoundedContext` triggers `ArgumentException` inside a Blazor event handler. Add symmetric null-check on `BoundedContext` or filter at scoring time. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:~383-407`]
- [x] `[Review][Patch]` **`CommandRouteBuilder.KebabCase` returns empty string for pathological input** — `"."`, `".."`, `".Foo"` produce empty / odd slugs, then `BuildRoute` yields `/domain//` or `/domain/foo/` which is passed verbatim to `NavigateTo`. Throw `ArgumentException` on empty result. [`src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs:~27-63`]
- [x] `[Review][Patch]` **`PaletteScorer` iterates `char`, breaking surrogate-pair queries** — `foreach (char qc in q)` splits emojis / supplementary-plane characters into independent lo/hi surrogate matches, producing nonsense scores for legitimate searches. Switch to `Rune.EnumerateRunes` (or `StringInfo.GetTextElementEnumerator`). [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteScorer.cs:~50-60`]
- [x] `[Review][Patch]` **`FcCommandPalette.DisposeAsync` catches `OCE` + `JSDisconnectedException` but not `JSException`** — `EnsureFocusModuleAsync` catches all three; disposal catches only two. A mid-tear-down `JSException` propagates to the Blazor error boundary. Align both sites on the same exception set. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:~132-140`]
- [x] `[Review][Patch]` **`FcPaletteResultList` badge-count observer doesn't null-check the disposed owner** — subscription is disposed in `DisposeAsync`, but `CountChanged` can fire after dispatch and before the subscription's unsubscribe completes (observable teardown race). `owner.InvokeAsync(owner.StateHasChanged)` on a disposed component throws `ObjectDisposedException`. Add a `_disposed` field and early-return in `OnNext`. [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:~162-170`]

**Deferred (pre-existing or out-of-scope)**

- [x] `[Review][Defer]` **`FrontComposerRegistry` constructor throw on `HFC1601` — latent, tied to Story 9-4** [`src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:~1325`] — deferred, tied to future HasFullPageRoute tightening in Story 9-4. Inert today (validator is tautological per decision DN6). Revisit when 9-4 implements real routing-metadata enforcement; consider moving validation out of ctor to `OnStart` so DI resolution can never cascade-fail a circuit.
- [x] `[Review][Defer]` **AC2/AC5/D11 bUnit matrix (arrow/enter/escape/focus-management/live-region no-match)** [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`] — deferred, pre-existing partial per DAR Review Findings. Reducer-level coverage is in place; component-level Theory for `PaletteDismissPaths_AllDispatchPaletteClosedAction` and `FocusManagement_*` tests tracked for a follow-up coverage pass.
- [x] `[Review][Defer]` **`ShortcutService.Dispose` race with pending chord timer callback** [`src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:~3410-3440`] — deferred, functionally harmless (generation guard absorbs the race). Revisit only if test harness uses real threaded `TimeProvider` (`FakeTimeProvider` never exercises this path).
- [x] `[Review][Defer]` **Prerender-only scope resolution retry** [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~112-155`] — deferred, Story 3-6 session-persistence / context-restoration will cover late-scope hydration with a proper `StorageReady` event. Today's fail-closed behaviour is the correct floor.
- [x] `[Review][Defer]` **`BoundedContextRouteParser` returns null for bare `/domain/{bc}` landing (2-segment domain path)** [`src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs:~44-49`] — deferred, by-design until bounded-context landing pages exist. Projection / command resolution needs the 3rd segment; contextual-bonus mis-attribution on a blank BC is a non-risk today.

_Noise/false-positives dismissed without write-through: 18 findings — including documented-deviation items (FluentSearch→FluentTextInput DN spike-notes §3; `BadgeAppearance.Tint` vs spec `Intent="Info"` DN5; `NET10_0_OR_GREATER` gating; file-end-newline style drift; nested `BadgeCountObserver` class vs lambda), defensive over-hardening that was correct (D4 chord-part `+` rejection; D20 stale-query guard), prose-only drift (D22 nullable vs non-nullable `TimeProvider`; D15 "state subscription" wording), and verified-safe behaviour (`@onkeydown:preventDefault="true"` does not break `FluentTextInput` typing per bubble/stopPropagation semantics and existing green tests; `D12` idempotency `_palettePending` dropping second Ctrl+K during `ShowDialogAsync` await is documented intent; percent-encoded scheme payloads are Story 7-x trust-boundary territory)._

