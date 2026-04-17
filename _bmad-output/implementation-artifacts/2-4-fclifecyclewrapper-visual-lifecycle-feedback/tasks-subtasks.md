# Tasks / Subtasks

> **Note (2026-04-17):** Implementation and code review for this story are complete (Status: done). The checkboxes below remain as a historical spec checklist; shipped work is recorded in the Dev Agent Record, File List, and Completion Notes—not by ticking every line post-delivery.
>
> Checkboxes are intentionally unchecked during authoring. Dev agent (Amelia) marks them `[x]` as each lands. Numbers align to the AC quick index.

### Task 0 — Prereq verification + mandatory package adds (≤ 20 min)

- [ ] 0.1: Verify Story 2-3 services are wired in `GeneratedComponentTestBase` at L47-52 — `ILifecycleBridgeRegistry`, `IUlidFactory`, `LifecycleOptions`, `ILifecycleStateService` all registered. If any missing, STOP and raise — Story 2-3 is not fully applied.
- [ ] 0.2: Verify `Microsoft.FluentUI.AspNetCore.Components` is pinned at `5.0.0-rc.2-26098.1` in `Directory.Packages.props`. Confirm `FluentSpinner`, `FluentMessageBar`, `FluentBadge`, `FluentButton` are exposed in v5 (use `mcp__fluent-ui-blazor__search_components` to confirm if unsure). If `FluentMessageBar` has a v5 rename, update D8 and adjust this spec before coding.
- [ ] 0.3: **Mandatory package add** (Amelia review 2026-04-16 — NOT conditional): add `<PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.0.0" />` (or latest compatible with .NET 10 SDK) to `Directory.Packages.props`, and `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />` to `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`. Tasks 5.2, 5.3b, 5.4, 5.4b hard-depend on `FakeTimeProvider`.
- [ ] 0.4: **Mandatory package add for Task 6.1** (Amelia review 2026-04-16 Blocker 3): add `<PackageVersion Include="Microsoft.Playwright" Version="1.49.0" />` (or latest) to `Directory.Packages.props` + `<PackageReference>` to `Hexalith.FrontComposer.Shell.Tests.csproj`. Install the browser binaries once via `pwsh tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/playwright.ps1 install chromium` (or equivalent on CI). Add a CI step that runs this install before the E2E test pass. If Jerome decides to descope AC9 instead, delete Tasks 6.1 + 6.2 here and move AC9 to a Known Gap with Story 5-7 ownership.
- [ ] 0.5: Run `dotnet build -c Release -p:TreatWarningsAsErrors=true` — baseline must be 0 warnings / 0 errors before changes start. If it's not clean, fix or surface first.
- [ ] 0.6: Run the full test suite baseline — must be 459/459 green (Story 2-3 dev record). Record baseline count; the target after this story lands is **~504**.

### Task 1 — IR extensions: none

- [ ] 1.1: Explicitly confirm no `CommandModel` / `FormModel` IR changes are needed. The wrapper is pure runtime; IR is untouched. (Sanity check — catches scope creep.)

### Task 2 — Create `FcLifecycleWrapper` component

- [ ] 2.1: Create `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor` + `.razor.cs`. Razor template renders: outer `<div class="fc-lifecycle-wrapper @_pulseClass">` containing `@ChildContent`, then a conditional `<div role="status" aria-live="@_ariaLiveLevel">@_announcement</div>`, then a conditional `<FluentBadge>Still syncing…</FluentBadge>` OR `<FluentMessageBar ...>` based on `_state.TimerPhase` and `_state.Current`. Code-behind implements `OnInitialized` (subscribe — D14), `OnParametersSet` (re-bind if CorrelationId changed — D15), `Dispose`/`DisposeAsync`. Parameters: `[Parameter, EditorRequired] public string CorrelationId`, `[Parameter] public RenderFragment? ChildContent`, `[Parameter] public string? RejectionMessage`.
- [ ] 2.2: Create `FcLifecycleWrapper.razor.css` — scoped CSS. Define `.fc-lifecycle-wrapper { position: relative; }`, `.fc-lifecycle-pulse { animation: fc-lifecycle-pulse 1.2s ease-in-out infinite; outline: 2px solid transparent; outline-offset: 2px; }`, `@keyframes fc-lifecycle-pulse { 0%, 100% { outline-color: transparent; } 50% { outline-color: var(--accent-fill-rest); } }`, and the reduced-motion media query per D11. No Fluent UI token overrides (UX §1690 zero-override).
- [ ] 2.3: Create `LifecycleUiState.cs` in the same folder. Immutable record (D6) with a pure `public static LifecycleUiState From(CommandLifecycleTransition transition, LifecycleTimerPhase phase)` factory method. Write the state transition table as a comment at the top (maps `(CurrentState, TimerPhase) → rendered elements`).
- [ ] 2.4: Create `LifecycleThresholdTimer.cs`. Class exposing `void Start()`, `void Reset(DateTimeOffset newAnchor)`, `void Stop()`, `event Action<LifecycleTimerPhase>? OnPhaseChanged`. Internal loop via `ITimer` obtained from `TimeProvider.CreateTimer(...)` ticking every 100 ms (D5). Constructor parameters: `TimeProvider time, IOptionsMonitor<FcShellOptions> options, Func<bool>? isDisconnected = null` (D23 isDisconnected seam; always `null` in v0.1 wrapper wiring). **Threshold snapshot caching (advanced-elicitation PM-C 2026-04-16):** do NOT read `options.CurrentValue` on every 100 ms tick — that allocates a fresh snapshot under config-watching containers and produces measurable GC pressure at 10 Hz × N wrappers. Instead cache the threshold values in private fields on construction, then subscribe to `options.OnChange(...)` with a callback that atomically swaps the fields (`Interlocked.Exchange` on each int). `Tick()` reads the cached fields. Dispose the `OnChange` subscription's `IDisposable` alongside the `ITimer` in `DisposeAsync`. Pure class — no Razor, no DI of `IJSRuntime`.
- [ ] 2.5: Wire `FcLifecycleWrapper.razor.cs` constructor / `OnInitialized` to `@inject ILifecycleStateService LifecycleService`, `@inject IOptionsMonitor<FcShellOptions> ShellOptions`, `@inject NavigationManager Nav`, `@inject ILogger<FcLifecycleWrapper> Logger`, `@inject TimeProvider Time` (D4/D5/ADR-021). On subscribe-callback: log HFC2100 if `_currentSubscriptionId != transition.CorrelationId` (race guard). Log HFC2101 Info if `transition.IdempotencyResolved`. On `Transition` → update `_state` → `InvokeAsync(StateHasChanged)` per Blazor idiom.
- [ ] 2.6: Emit wrapper component's event unsubscribe + timer dispose in `Dispose` / `DisposeAsync`. Idempotent — disposing twice is a no-op (use `Interlocked.Exchange` on a flag, same pattern as `LifecycleStateService`).
- [ ] 2.7: **~10-min investigation, Occam's Razor trim candidate (advanced-elicitation OC-2 2026-04-16):** check whether Fluent UI v5's `FluentMessageBar.Timeout` parameter honors `TimeProvider` injection (e.g., via an `[Inject] TimeProvider` pattern or an explicit `TimeProvider` parameter). If YES — drop D16 (our own auto-dismiss timer), delete the separate `_dismissAt` field, let `FluentMessageBar.Timeout` handle Confirmed auto-dismiss natively, remove Task 5.1b.3 (`Confirmed_auto_dismisses_after_ConfirmedToastDurationMs`) and replace with a simpler "FluentMessageBar rendered with Timeout=ConfirmedToastDurationMs" markup assertion. If NO — keep D16 as-is; mark this subtask `[~]` (investigated, not needed). Use `mcp__fluent-ui-blazor__get_component_details` on `FluentMessageBar` to inspect the v5 API. Investigation time-box: 10 minutes. If the v5 docs are ambiguous, default to keeping D16 (safer).

### Task 3 — Extend `FcShellOptions` with thresholds

- [ ] 3.1: In `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`, add four `int` properties per D12: `SyncPulseThresholdMs` (default 300), `StillSyncingThresholdMs` (default 2_000), `TimeoutActionThresholdMs` (default 10_000), `ConfirmedToastDurationMs` (default 5_000). Each with `[Range(min, max)]` per D12 ranges. XML doc each referencing NFR11-14 + UX-DR48.
- [ ] 3.2: Create `IValidateOptions<FcShellOptions>` implementation `FcShellOptionsThresholdValidator` in `src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs`. Validates ordering `Pulse < StillSyncing < TimeoutAction`. Returns `ValidateOptionsResult.Fail(...)` with a clear message on violation. **Register AFTER `ValidateDataAnnotations()`** (Amelia review 2026-04-16 Medium 2 — `IValidateOptions<T>` runs in registration order; `[Range]` failures must surface before ordering failures for clearer adopter error messages):
    ```csharp
    services
        .AddOptions<FcShellOptions>()
        .ValidateDataAnnotations()          // Step 1: [Range] first
        .ValidateOnStart();                 // Step 2: surface at startup
    services.AddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();  // Step 3: ordering validator runs after [Range]
    ```
  Confirm `Microsoft.Extensions.Options` is resolvable from Contracts (transitively via `Microsoft.Extensions.DependencyInjection` — should be, verify). Validator lives in Shell so Contracts stays dependency-free.
- [ ] 3.3: Reserve diagnostic IDs. If `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` exists, extend with `HFC2100`, `HFC2101`, `HFC2102`. Else create the file with const strings. (Runtime-only per architecture.md §648 — no `AnalyzerReleases.Unshipped.md` entry.)
- [ ] 3.4: In `ServiceCollectionExtensions.AddHexalithFrontComposer`, register `services.AddOptions<FcShellOptions>().ValidateDataAnnotations().ValidateOnStart()` if not already present. This triggers `[Range]` validation + the custom validator at startup — adopters with bad config fail fast, not at first-wrapper-render.

### Task 4 — Emitter wrap + using-directive emission + consumer Shell ProjectReference

- [ ] 4.1: Modify `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`. Two edits:
  - **4.1a:** Append `using Hexalith.FrontComposer.Shell.Components.Lifecycle;` to the emitted `using` block (currently at `CommandFormEmitter.cs:34-48`). This IS the namespace-resolution mechanism (D21) — NOT `_Imports.razor`, which doesn't apply to source-generated `.g.cs`.
  - **4.1b:** Wrap the emitted `EditForm` (currently at `CommandFormEmitter.cs:359` `OpenComponent<EditForm>`) in an outer `OpenComponent<FcLifecycleWrapper>(seq++)` + `AddAttribute(seq++, "CorrelationId", _submittedCorrelationId)` + `AddAttribute(seq++, "ChildContent", (RenderFragment)(...))` pattern — match the existing nested-component emission style used for `EditForm` + `DataAnnotationsValidator`. Close `FcLifecycleWrapper` after `EditForm` closes. Verify `_submittedCorrelationId` is in scope at L97 (confirmed by Amelia review 2026-04-16).
- [ ] 4.2: **Consumer-domain Shell ProjectReference** (Amelia review 2026-04-16 Blocker 1 / D20 resolution — this task REPLACES the original `_Imports.razor` edit which was a no-op). Add `<ProjectReference Include="..\..\..\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj" />` to `samples/Counter/Counter.Domain/Counter.Domain.csproj` (after line 9, next to the existing `Contracts` ProjectReference). Document the adopter requirement in a repo-level `CONTRIBUTING.md` bullet (or `docs/adopter-guide.md` if present — if not, skip the docs edit and surface as Known Gap G12 Documentation ownership): "Domain projects with `[Command]`-annotated types must reference `Hexalith.FrontComposer.Shell` alongside the `SourceTools` analyzer, same pattern as the existing `Microsoft.FluentUI.AspNetCore.Components` + `Fluxor.Blazor.Web` references."
- [ ] 4.3: Regenerate emitter snapshots. Run the SourceTools tests once to surface the diff, inspect the snapshot diffs in `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/Snapshots/` (files ending `.verified.txt`), confirm only the wrapper wrap + `using` additions appear, then `dotnet test ... --environment "DiffEngine_Disabled=true"` re-approves. **Scope (Amelia review 2026-04-16 Medium 4):** re-approve ALL `CommandFormEmitterTests.*.verified.txt` snapshots that contain `OpenComponent<EditForm>` — the wrap changes every emitted form, so expect **≥5 snapshot diffs** (list them after running the test-surface once so the count is exact). Known files likely to change: `CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly`, `CommandForm_ShowFieldsOnly_RendersOnlyNamedFields`, and every other `CommandForm_*` baseline.

### Task 5 — Tests (re-leveled per Murat review 2026-04-16; split across 7 files)

**Test-level rebalance:** Murat flagged 8 of 14 original bUnit tests as wanting to be pure-function unit tests on `LifecycleUiState.From(transition, phase)` — cheaper, faster, higher combinatorial coverage. Push those down; keep bUnit only for behaviours that exercise DI + navigation + re-render.

- [ ] 5.1a: Create `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleUiStateTests.cs` with **10 pure-function unit tests** on `LifecycleUiState.From(CommandLifecycleTransition, LifecycleTimerPhase)` (AC1, AC3, AC4, AC6, AC7 state-mapping — no bUnit, no renderer):
  - [ ] 5.1a.1: `Idle_phase_NoPulse_produces_no_announcement_no_pulse_no_message_bar`
  - [ ] 5.1a.2: `Submitting_phase_NoPulse_produces_submitting_announcement_polite_no_pulse`
  - [ ] 5.1a.3: `Acknowledged_phase_NoPulse_produces_no_announcement_no_pulse`
  - [ ] 5.1a.4: `Syncing_phase_Pulse_produces_pulse_class_and_NO_announcement` (Sally review 2026-04-16 — pulse is visual-only)
  - [ ] 5.1a.5: `Syncing_phase_StillSyncing_produces_still_syncing_badge_and_polite_announcement`
  - [ ] 5.1a.6: `Syncing_phase_ActionPrompt_produces_warning_message_bar_and_assertive_announcement_start_over_button`
  - [ ] 5.1a.7: `Confirmed_phase_Terminal_produces_success_message_bar_and_polite_announcement`
  - [ ] 5.1a.8: `Rejected_phase_Terminal_produces_danger_message_bar_and_assertive_announcement_no_auto_dismiss`
  - [ ] 5.1a.9: `Rejected_phase_uses_parameter_RejectionMessage_when_populated_else_localized_fallback`
  - [ ] 5.1a.10: `IdempotencyResolved_true_on_Confirmed_produces_same_output_as_fresh_Confirmed_in_v01` (G2 — v0.1 parity; Story 2-5 branches on this)
- [ ] 5.1b: Create `FcLifecycleWrapperTests.cs` with **6 bUnit tests** for non-pure behaviours that require DI + renderer:
  - [ ] 5.1b.1: `Idle_renders_only_ChildContent_no_wrapper_chrome` (integration sanity)
  - [ ] 5.1b.2: `ActionPrompt_Start_over_button_calls_NavigateTo_forceLoad_true` (NavigationManager mock, ADR-022)
  - [ ] 5.1b.3: `Confirmed_auto_dismisses_after_ConfirmedToastDurationMs_returns_to_Idle` (FakeTimeProvider + renderer)
  - [ ] 5.1b.4: `Rejected_message_bar_persists_until_user_dismiss_no_auto_dismiss_timer` (D17)
  - [ ] 5.1b.5: `CorrelationId_change_disposes_old_subscription_and_resubscribes_to_new_id` (D15 re-bind)
  - [ ] 5.1b.6: `Subscribe_replay_during_OnInitialized_does_not_NRE_on_half_initialized_state` (**Murat R-Reentrancy HIGH** — transition arrives synchronously during subscribe replay before `_state` + timer assigned)
  - [ ] 5.1b.7: `Dispose_during_auto_dismiss_timer_does_not_invoke_StateHasChanged_on_disposed_component` (**advanced-elicitation Pre-mortem PM-D 2026-04-16** — complements 5.3.5 which covers threshold-timer-dispose; this test covers Confirmed→auto-dismiss-timer-firing race when the wrapper is disposed mid-dismiss-delay, e.g., user navigates away at T+4s while ConfirmedToastDurationMs=5000)
- [ ] 5.2: Create `FcLifecycleWrapperThresholdTests.cs` with **7** `FakeTimeProvider` threshold-timing tests (AC2-AC5):
  - [ ] 5.2.1: `Confirmed_within_SyncPulseThresholdMs_never_applies_pulse_class_brand_signal_fusion` (AC2)
  - [ ] 5.2.2: `Exactly_at_SyncPulseThresholdMs_applies_pulse_class` (boundary)
  - [ ] 5.2.3: `Exactly_at_StillSyncingThresholdMs_renders_still_syncing_badge` (boundary)
  - [ ] 5.2.4: `Exactly_at_TimeoutActionThresholdMs_renders_action_prompt_message_bar` (boundary)
  - [ ] 5.2.5: `Timer_anchors_on_LastTransitionAt_not_subscribe_time` (D3 — renamed per Murat review 2026-04-16; honest about testing timer anchor, NOT full Blazor circuit reconnect which lives in Story 5-7)
  - [ ] 5.2.6: `Confirmed_while_in_ActionPrompt_phase_immediately_resolves_to_success_message_bar_no_dangling_pulse` (D19 single-timer reset)
  - [ ] 5.2.7: `Threshold_change_via_IOptionsMonitor_mid_Syncing_applies_on_next_tick` (**Murat R-Options-Hot-Reload MEDIUM** + ADR-023 — change `SyncPulseThresholdMs` from 300 → 100 while elapsed=150ms; next tick advances Phase to Pulse per ADR-023 retroactive-next-tick semantics)
- [ ] 5.2b: Create `LifecycleThresholdTimerPropertyTests.cs` with **3 FsCheck property tests** (**Murat highest-confidence recommendation** — 2-3 shipped 15, 2-4 was proposing 0; timer phase sequence is a textbook monotonic state machine). FsCheck.Xunit.v3 inherited from 2-3; 1000 CI iter / 10000 nightly per architecture.md §1419:
  - [ ] 5.2b.1: `Phase_monotonic_under_arbitrary_tick_schedule` — for any random FakeTimeProvider advance sequence, Phase is non-decreasing until `Reset`
  - [ ] 5.2b.2: `Reset_with_newer_anchor_is_idempotent_under_tick_ordering` — `Reset(A); tick; tick; Reset(B)` = `Reset(B)` regardless of intervening ticks
  - [ ] 5.2b.3: `Phase_computation_equals_pure_elapsed_bucket_function` — model-based: Phase always equals `BucketFor(UtcNow - Anchor, thresholds)` irrespective of tick order/frequency
- [ ] 5.3: Create `FcLifecycleWrapperA11yTests.cs` with **5** accessibility tests (AC1, AC4, AC5, AC8):
  - [ ] 5.3.1: `Live_region_role_is_status_when_Submitting_or_StillSyncing` (not during Pulse phase — Sally review 2026-04-16)
  - [ ] 5.3.2: `Live_region_role_is_alert_when_Rejected_or_ActionPrompt`
  - [ ] 5.3.3: `Focus_ring_preserved_on_descendant_focusable_during_pulse_phase` (markup-level; `.fc-lifecycle-pulse` only applies to outer wrapper, not inner button — pairs with Playwright visual test in Task 6 if available)
  - [ ] 5.3.4: `Reduced_motion_media_query_present_in_scoped_css_honoring_scope_attribute_selector` (**Murat R-Scoped-CSS MEDIUM** — Blazor scoped CSS rewrites `.fc-lifecycle-pulse` to `.fc-lifecycle-pulse[b-XXXXXX]`; assertion must match either form OR parse the scope attribute and assert the `@media (prefers-reduced-motion: reduce)` block contains `animation: none` AND `outline: 2px solid` under the scoped selector form) — D11
  - [ ] 5.3.5: `Dispose_during_inflight_transition_callback_does_not_invoke_StateHasChanged_on_disposed_component` (**Murat R-Circuit-Reconnect HIGH** — simulate dispose while a subscribe callback is mid-flight; assert no `StateHasChanged` fires and no `ObjectDisposedException` propagates)
  - [ ] 5.3.6: `Message_bar_render_on_Confirmed_does_not_steal_focus_from_sibling_focused_element` (**advanced-elicitation Pre-mortem PM-F 2026-04-16** — seat a focused `<input>` in the wrapper's `ChildContent`, trigger a Confirmed transition, assert via markup + bUnit `document.activeElement` polyfill that focus remains on the original input and was NOT moved to the newly-rendered `FluentMessageBar`. Catches the class of bug where `FluentMessageBar`'s internal autofocus behaviour steals focus from a user mid-typing in the next field)
- [ ] 5.4: Create `LifecycleThresholdTimerTests.cs` with **4** pure-class unit tests (D4, D5, ADR-021):
  - [ ] 5.4.1: `Phase_advances_NoPulse_Pulse_StillSyncing_ActionPrompt_as_fake_time_advances_past_thresholds`
  - [ ] 5.4.2: `Reset_with_new_anchor_rewinds_phase_to_NoPulse`
  - [ ] 5.4.3: `OnPhaseChanged_fires_exactly_once_per_phase_transition_no_duplicates`
  - [ ] 5.4.4: `Stop_then_Dispose_cancels_timer_and_no_further_events_fire`
- [ ] 5.5: Create `FcShellOptionsValidationTests.cs` with **3** options-validation tests (D12, Task 3):
  - [ ] 5.5.1: `Defaults_satisfy_ordered_thresholds_validator`
  - [ ] 5.5.2: `SyncPulse_gte_StillSyncing_fails_validation_with_clear_message`
  - [ ] 5.5.3: `Range_annotations_enforce_min_max_bounds_on_each_threshold_property`
- [ ] 5.6: Modify `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererCompactInlineTests.cs` + `CommandRendererInlineTests.cs` + `CommandRendererFullPageTests.cs` — add ONE assertion per file (3 total):
  - `markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive)` — confirms the emitter wrap lands in every density's rendered form.

**Revised total Task 5 test count: 10 + 7 + 7 + 3 + 6 + 4 + 3 + 3 = 43 new tests** (was 33 pre-review, 41 post-party-mode, +1 5.1b.7 auto-dismiss-dispose, +1 5.3.6 focus-preservation per advanced elicitation). All Murat-flagged coverage gaps closed; all eight Murat-flagged bUnit→unit re-levels applied; Pre-mortem PM-D + PM-F risks covered.

### Task 6 — Counter sample integration + Playwright E2E latency gate

- [ ] 6.1: Create `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CounterCommandLatencyE2ETests.cs`. Playwright package is added as mandatory in Task 0.4. Test scenarios (**sample sizes revised per Murat review 2026-04-16 — n=50 for P95 is statistical theatre**):
  - `CounterLatency_ColdActor_P95_Under_800ms` — measures **300 Increment clicks** (up from 50), discards first **10 clicks as warm-up** (JIT + first-circuit-cache noise), asserts P95 < 800 ms via `HdrHistogram` or simple array-sort-and-index on the remaining 290 samples. 300 gives ±~10% CI half-width at P95.
  - `CounterLatency_WarmActor_P50_Under_400ms` — measures **100 clicks** in the same session, asserts P50 < 400 ms (100 samples is adequate for P50 with reasonable CI).
  - Measurement point: `click → FluentMessageBar Intent=Success visible` (D2 binding contract — NOT an internal Fluxor subscription).
  - Trait with `[Trait("Category", "E2E")]` so CI can gate separately; do NOT run in normal `dotnet test` default pass.
  - If Playwright browser install fails on a specific CI agent, skip with `Skip.If(!IsPlaywrightAvailable())` + emit a CI warning annotation — do NOT silently pass.
- [ ] 6.2: In `samples/Counter/Counter.Web/Program.cs`, add `builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));` if not already present. Add `appsettings.Development.json` override section with `"Hexalith": { "Shell": { "SyncPulseThresholdMs": 300, "StillSyncingThresholdMs": 2000, "TimeoutActionThresholdMs": 10000 } }` — demonstrates the UX spec §1835 deployment-level calibration.
- [ ] 6.3: **OPTIONAL sanity pass, SKIP if 6.1 passes green** (advanced-elicitation OC-3 2026-04-16 — Occam trim). Only run if Task 6.1 Playwright E2E fails or is skipped. Run the Counter sample via Aspire (`dotnet run --project samples/Counter/Counter.AppHost`) and manually validate in a real browser that the lifecycle wrapper renders correctly on Increment dispatch. Use `mcp__aspire__list_apphosts` + `mcp__claude-in-chrome__*` if you want to automate. Record finding in Dev Notes. Explicit carve-out from memory/feedback_no_manual_validation.md — automated 6.1 is the primary gate; this is the fallback when automation is blocked.
- [ ] 6.4: **20-wrapper concurrency load test** (Winston review 2026-04-16 — ADR-020/D19 concurrency claim was thumb-suck; needs data). Create `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/WrapperConcurrencyLoadTests.cs` with one `[Trait("Category", "E2E")]` test: render a Blazor bUnit fixture with 20 concurrent `FcLifecycleWrapper` instances (one per fake CorrelationId), each independently transitioning `Submitting → Acknowledged → Syncing → Confirmed` at random offsets within a 2-second window. Measure: (a) CPU time spent in `ITimer` callbacks (must stay < 5% single-core at 10 Hz × 20 wrappers), (b) memory allocation per tick (must not leak — assert stable steady state via `GC.GetTotalAllocatedBytes(true)` across 1000 ticks), (c) no `ObjectDisposedException` or `InvalidOperationException` under concurrent dispose. If this fails, ADR-020's "200 Hz dismissed" hand-wave needs the shell-scoped tick multiplexer from Epic 5 promoted to Story 2-4 scope — raise before continuing.

### Task 7 — Regression + zero-warning gate

- [ ] 7.1: Run `dotnet build -c Release -p:TreatWarningsAsErrors=true` — must still be 0 warnings / 0 errors.
- [ ] 7.2: Run full `dotnet test` — must be **~492 / ~492 green** (459 baseline + ~33 new). If any Story 2-1/2-2/2-3 test went red, fix the wrapper side (don't change the prior story). Record the exact new count in Completion Notes.
- [ ] 7.3: Verify `dotnet format --verify-no-changes` passes (inherited repo convention).

---
