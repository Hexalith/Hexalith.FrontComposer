# Story 2.4: FcLifecycleWrapper — Visual Lifecycle Feedback

Status: ready-for-dev

---

## Dev Agent Cheat Sheet (Read First — 2 pages)

> Amelia-facing terse summary. Authoritative spec is the full document below. Every line links to a section for detail.

**Goal:** Ship `FcLifecycleWrapper` — a hand-written Blazor component that **subscribes to `ILifecycleStateService.Subscribe(correlationId, ...)`** (Story 2-3 ADR-017 D19 binding consumer contract) and renders progressive visual feedback for every command submission: in-button spinner (existing), Acknowledged→Syncing sync pulse (≥300 ms), "Still syncing..." inline text (≥2 s), action prompt with manual refresh (≥10 s), Confirmed success `FluentMessageBar` (auto-dismiss 5 s), Rejected `FluentMessageBar` (danger, no-dismiss), with full `prefers-reduced-motion` + `aria-live` support and zero focus-ring suppression (UX-DR49).

**Scope boundary:** Epic 2 happy path (stable connection). **No Disconnected state** (UX-DR2 "Disconnected" mention) — Story 5-3 owns SignalR connection awareness; Story 2-3 ADR-017 **D11** explicitly cut `ConnectionState` from `ILifecycleStateService` in v0.1. Domain-specific rejection-message formatting, destructive confirmation dialog, and form abandonment protection are **Story 2-5**. Visual regression baselines (per-theme × per-density × per-motion-preference) land with Story 10-2.

**Binding contract with Story 2-3 (D19 single-writer invariant / ADR-017 consumer):**
- `FcLifecycleWrapper` MUST consume `ILifecycleStateService.Subscribe(correlationId, Action<CommandLifecycleTransition>)` and treat its callback as the single source of truth for lifecycle state. It **MUST NOT** `@inject IState<{Command}LifecycleFeatureState>` or read any per-command Fluxor feature state directly — bypassing the service loses the HFC2007 divergence detection + FR30 `OutcomeNotifications` enforcement. Reference Story 2-3 Decision D19.
- The progressive-visibility timers (300 ms / 2 s / 10 s) MUST anchor on `CommandLifecycleTransition.LastTransitionAt` (the monotonic anchor surfaced per Story 2-3 D15 / Sally Story C), **not** wall-clock `DateTimeOffset.UtcNow` or `DateTime.Now` from mount. This guarantees thresholds measure real command elapsed time across reconnect / replay, not clock-from-subscribe.
- `CommandLifecycleTransition.IdempotencyResolved == true` means the Confirmed/Rejected arose from duplicate-MessageId detection (Story 2-3 D10). In v0.1, render the same Confirmed message-bar (no "already done" differentiation copy — deferred to Story 2-5 per **Known Gap G2**). The flag is not ignored; it logs `HFC2101` (runtime-warning) so 2-5's localized copy lands without a second API change.

**Binding contract with Stories 2-1 / 2-2:**
- `CommandFormEmitter`'s existing `FluentSpinner`-in-submit-button (emitted during `LifecycleState.Value.State == CommandLifecycleState.Submitting`) stays **unchanged**. The wrapper adds the **surrounding** live region + post-submit feedback, it does NOT replace the in-button spinner. Developers get both layers for free: the button spinner tells the user "this button is busy"; the wrapper tells the user "the system is processing / confirmed / still waiting / rejected".
- `CommandFormEmitter` now wraps its emitted `<EditForm>` markup in `<FcLifecycleWrapper CorrelationId="@_submittedCorrelationId">` (ADR-020). `_submittedCorrelationId` is already in scope (introduced Story 2-3 Task 3.3 for `ResetToIdleAction(string CorrelationId)`). Snapshots re-approved.
- `CommandRendererEmitter` (Story 2-2) is **not modified** in v0.1. Pure 0-field inline button dispatch (no form) does NOT get the wrapper overlay in Epic 2 — documented as **Known Gap G1** with explicit deferral to Story 4-5 (expand-in-row detail renderer where the inline button + progressive reveal lives).
- `ADR-016` renderer/form split maintained: the wrapper wraps the **form**, not the renderer's outer chrome. The renderer's breadcrumb / compact-popover structure is untouched.

**ADR-020 one-liner:** FcLifecycleWrapper is a hand-written Shell component; emitter wraps generated forms with it. No new per-command emitted artifact.

**Files to create / extend:**

| Path | Action |
|---|---|
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor` | Create (Task 2.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs` | Create (Task 2.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.css` | Create — sync-pulse keyframes + reduced-motion media query (Task 2.2) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs` | Create — local derived-state record + transition-to-ui-state mapper (Task 2.3) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleThresholdTimer.cs` | Create — `TimeProvider`-injectable threshold timer (pulse/text/action) (Task 2.4) |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` | Modify — add `SyncPulseThresholdMs`, `StillSyncingThresholdMs`, `TimeoutActionThresholdMs`, `ConfirmedToastDurationMs` + ordered-threshold IValidateOptions (Task 3.1) |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` *(new if absent; else extend)* | Reserve `HFC2100`–`HFC2102` runtime-log codes (Task 3.2) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Modify — wrap emitted `<EditForm>` in `<FcLifecycleWrapper CorrelationId="@_submittedCorrelationId">` (Task 4.1) |
| `src/Hexalith.FrontComposer.Shell/_Imports.razor` | Modify — add `@using Hexalith.FrontComposer.Shell.Components.Lifecycle` (Task 4.2) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperTests.cs` | Create — 14 bUnit state-render tests (Task 5.1) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperThresholdTests.cs` | Create — 6 fake-time threshold-timing tests (Task 5.2) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperA11yTests.cs` | Create — 4 aria-live + role + reduced-motion markup tests (Task 5.3) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleThresholdTimerTests.cs` | Create — 4 timer-unit tests (Task 5.4) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs` | Create — 3 ordered-threshold validation tests (Task 5.5) |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitter*.verified.txt` | Re-approve — 2 existing snapshots now include wrapper markup (Task 4.3) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererCompactInlineTests.cs` *(modify)* | Re-assert wrapper presence in generated renderer output (1 added assertion per density) (Task 5.6) |
| `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CounterCommandLatencyE2ETests.cs` | Create — Playwright/Aspire-backed P95 cold + P50 warm latency gate (Task 6.1) |
| `samples/Counter/Counter.Web/Program.cs` | Modify — bind `FcShellOptions.SyncPulseThresholdMs` / `StillSyncingThresholdMs` / `TimeoutActionThresholdMs` from config section (demo-only) (Task 6.2) |

**AC quick index (details in Acceptance Criteria section below):**

| AC | One-liner | Task(s) |
|---|---|---|
| AC1 | Submitting → in-button `FluentSpinner` (existing) **plus** wrapper surfaces `aria-live="polite"` "Submitting…" announcement; submit button disabled; focus ring preserved (UX-DR49) | 2, 4 |
| AC2 | Acknowledged → Confirmed within **SyncPulseThresholdMs** (default 300 ms) → pulse **never fires**, wrapper resolves invisibly to idle (NFR11, UX-DR48 brand-signal fusion) | 2, 5.2 |
| AC3 | 300 ms–2 s in Syncing → `.fc-lifecycle-pulse` accent-color CSS animation on wrapper frame; threshold configurable via `FcShellOptions.SyncPulseThresholdMs` | 2, 3, 5.2 |
| AC4 | 2 s–10 s in Syncing → "Still syncing…" `FluentBadge` (Accent) inline below the form; threshold configurable via `FcShellOptions.StillSyncingThresholdMs` (NFR13) | 2, 3, 5.2 |
| AC5 | >10 s in Syncing → action prompt `FluentMessageBar` (Warning) with "Refresh" button that reloads the page (minimal v0.1 recovery per ADR-022); threshold configurable via `FcShellOptions.TimeoutActionThresholdMs` (NFR14) | 2, 3, 5.2 |
| AC6 | Confirmed → `FluentMessageBar` (Success) auto-dismisses after `ConfirmedToastDurationMs` (default 5 s); `aria-live="polite"` announces "Submission confirmed" | 2, 5.1 |
| AC7 | Rejected → `FluentMessageBar` (Danger) **no auto-dismiss**; `aria-live="assertive"` announces "Submission rejected"; domain-specific message payload surfaces via `RejectionMessage` parameter (populated by Story 2-5; v0.1 falls back to generic "Command rejected") | 2, 5.1 |
| AC8 | `prefers-reduced-motion: reduce` → CSS media query replaces pulse keyframe with static `outline: 2px solid var(--accent-fill-rest)`; focus ring never dimmed or suppressed (UX-DR49) | 2.2, 5.3 |
| AC9 | P95 click-to-confirmed **< 800 ms cold actor** and P50 **< 400 ms warm actor** measured via Playwright task timer on localhost Aspire topology with Counter sample; CI job fails if either percentile is breached (NFR1, NFR2) | 6 |

**Scope guardrails (do NOT implement — see Known Gaps):**
- **Disconnected state / SignalR `HubConnectionState` observation** → **Story 5-3** (the `ConnectionState` seam on `ILifecycleStateService` was cut in 2-3 per D11; 2-4 cannot implement Disconnected state without the contract). Wrapper does NOT escalate on SignalR drop in v0.1 — it keeps running the timer, eventually showing the 10 s action prompt. That **is** the degraded-path behaviour in Epic 2 happy-path scope.
- **Domain-specific rejection message formatting** ("Approval failed: insufficient inventory. The order has been returned to Pending.") → **Story 2-5** — v0.1 takes a `RejectionMessage` string parameter but defaults to a generic localized fallback.
- **Destructive action confirmation dialog (FluentDialog)** → **Story 2-5**
- **Form abandonment protection on navigation** → **Story 2-5**
- **"Already done (by another user)" IdempotentConfirmed UX differentiation** → **Story 2-5** — v0.1 logs `HFC2101` when `IdempotencyResolved==true` so 2-5 can grep history for the production copy-cut.
- **Visual regression baseline screenshots (per-theme × per-density × per-motion-preference)** → **Story 10-2** (accessibility CI gates + visual specimen verification)
- **SignalR fault-injection test harness** (simulate drops during Syncing) → **Story 5-7**
- **Pure 0-field inline-button lifecycle overlay** (no form) → **Story 4-5** (expand-in-row detail); Counter sample's `IncrementCommandRenderer` (1-field) *does* get the wrapper via `CommandFormEmitter` because it still emits an `<EditForm>` in the popover
- **Manual refresh action prompt alternatives** (retry button, contact support link) → **Story 5-3** recovery UX (ADR-022 only ships page-reload)

**3 new runtime-log diagnostic codes reserved (runtime-only, NOT analyzer-emitted — no `AnalyzerReleases.Unshipped.md` entry per architecture.md §648 precedent used in 2-3 HFC2004-2007):**
- **HFC2100 Warning (runtime log)** — `FcLifecycleWrapper` received a transition for an unknown `CorrelationId` (wrapper subscribed but entry already evicted — indicates subscribe-after-terminal-cleanup race). Logged + swallowed; UI stays in `Idle`.
- **HFC2101 Info (runtime log)** — `FcLifecycleWrapper` observed `IdempotencyResolved==true` transition; deferred-copy Story 2-5 consumer will populate "already done" wording. Logged at Information level (not Warning — it is expected behaviour under Blazor circuit reconnect, not a bug).
- **HFC2102 Warning (runtime log)** — `FcLifecycleWrapper` threshold timer fired outside UI-thread context (shouldn't happen given `InvokeAsync(StateHasChanged)`, but guarded with a diagnostic for field debugging of Blazor-component teardown races).

**Test expectation: ~47 new tests, cumulative ~506** (Story 2-3 ends at 459 per its dev record). Breakdown at Task 5 / 6. Per L06 defense budget (≤25 decisions for feature story, we land at **23** after advanced-elicitation added D22 XSS + D23 isDisconnected seam), tests scale with decisions at ~2.0/decision — still under 2-3's 2.3/decision ratio.

**Start here:** Task 0 (prereqs — verify Story 2-3 services wired in test base; confirm Microsoft.FluentUI v5 exposes `FluentSpinner`, `FluentMessageBar`, `FluentBadge`, `FluentButton`) → Task 1 (no IR changes — wrapper is pure runtime) → Task 2 (create `FcLifecycleWrapper` + CSS + timer) → Task 3 (extend `FcShellOptions` with thresholds + ordered-validator) → Task 4 (wire emitter wrap + snapshots) → Task 5 (bUnit tests: state render + threshold timing + a11y + options validation) → Task 6 (Counter sample Program.cs config binding + Playwright E2E latency gate).

**The 23 Decisions and 4 ADRs below are BINDING. Do not revisit without raising first.** (D20/D21 + ADR-023 added 2026-04-16 post-party-mode review; D22 XSS + D23 isDisconnected-seam added 2026-04-16 post-advanced-elicitation pass; former ADR-023 demoted to Dev Note and former ADR-024 renumbered to ADR-023 in the same pass — see Change Log.)

---

## Story

As a business user,
I want to see clear, progressive visual feedback during command submission so I know the system is working and never wonder "did it work?",
so that I have 100% confidence in every command outcome without needing to manually refresh or double-submit.

---

## Critical Decisions (READ FIRST — Do NOT Revisit)

These decisions are BINDING. Tasks reference them by number. If implementation uncovers a reason to change one, raise it before coding, not after.

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Wrapper is a hand-written Shell component, not emitted per-command.** Zero new generator artifacts. The wrapper takes a single `[Parameter] public string CorrelationId` (Story 2-3 D1 — string, not Guid) and a single `[Parameter] public RenderFragment? ChildContent`, plus an optional `[Parameter] public string? RejectionMessage`. **Public-parameter-surface stability contract (Winston review 2026-04-16):** the parameter surface is **append-only through v1** — Story 2-5 and beyond may ADD optional parameters (e.g., `OnDismiss`), may NOT remove or rename or change types of the three parameters listed here, may NOT introduce new `EditorRequired` parameters without a semver-major revision in Epic 9. Breaking this contract would cascade through every re-emitted form from `CommandFormEmitter` and break Story 2-1's snapshot regression gate. | L05 hand-written service + (optional) emitted wiring pattern. The wrapper is generic across command types — no per-command typed state to emit. Emitting it per-command would multiply circuit memory and defeat the cross-command correlation-index design (Story 2-3 ADR-017). Append-only stability prevents 2-5 silently breaking 2-1 — Winston's primary concern. **URL-parameter safety (advanced-elicitation Red Team RT-2 2026-04-16):** any future parameter that accepts a URL (e.g., hypothetical `ReturnUrl`, `RedirectOnDismiss`) MUST be validated against an adopter-provided allowlist before being passed to `NavigationManager.NavigateTo`. Open-redirect is a surface we close by default: ADR-022 uses `NavigationManager.Uri` (current URL echo) which is safe; any deviation requires explicit allowlist validation in the same PR. |
| D2 | **Wrapper subscribes exclusively via `ILifecycleStateService.Subscribe(correlationId, onTransition)`** and uses the callback shape (Story 2-3 ADR-018 bespoke-callback contract). It does **NOT** consume `IObservable<T>` (would invite `System.Reactive`), does NOT consume `IState<{Command}LifecycleFeatureState>` (Story 2-3 D19 single-writer invariant — bypassing the service loses FR30 `OutcomeNotifications` enforcement and HFC2007 divergence detection), and does NOT poll `GetState` on a timer. | Matches the 50-LoC subscribe-and-dispose pattern documented as canonical in 2-3 ADR-018. |
| D3 | **Threshold anchor is `CommandLifecycleTransition.LastTransitionAt`** (Story 2-3 D15 monotonic anchor), NOT `DateTime.UtcNow` from mount or the wrapper's own subscribe-timestamp. The pulse timer, "Still syncing…" timer, and action-prompt timer all measure `TimeProvider.GetUtcNow() - LastTransitionAt` on each tick. Under circuit reconnect / Story 5-4 replay, this anchors the user-visible thresholds to **real command elapsed time**, not wall-clock-from-remount. | Sally's Story C (reconnect staleness): without this, "Still syncing…" would lie after a mid-flight reconnect — the user sees 2 s from page restore, not 2 s from their actual click. Story 2-3 already pays the cost of carrying the anchor; 2-4 must consume it. |
| D4 | **`LifecycleThresholdTimer` is a standalone `IDisposable` class, not inline `System.Threading.Timer` inside the component.** Takes `TimeProvider time`, a current `LastTransitionAt` anchor, and the three threshold values; exposes a single `Action<LifecycleTimerPhase>? OnPhaseChanged` event. Phases are `NoPulse → Pulse → StillSyncing → ActionPrompt → Terminal`. The component subscribes to `OnPhaseChanged` and calls `InvokeAsync(StateHasChanged)` on each tick. Fakeable via `TimeProvider.System` in prod / `FakeTimeProvider` in tests. | Pulls timing logic out of the Razor component so it's unit-testable in isolation (Task 5.4 — 4 fake-time unit tests without bUnit). Matches Story 2-3 D6 `LifecycleEntry` mutable-class-with-thread-safe-fields pattern — threshold state is held in a mutable timer class with a single mutating method (`Tick`). |
| D5 | **Timer uses `ITimer` obtained from `TimeProvider.CreateTimer(...)` with 100 ms callback period** (NOT `PeriodicTimer`). `PeriodicTimer.WaitForNextTickAsync` is not `TimeProvider`-aware in .NET 8/9/10 BCL and uses its own clock — that would break `FakeTimeProvider.Advance` determinism in Task 5.2 threshold tests (Murat review 2026-04-16 — the single hardest-to-catch flakiness source). `ITimer` via `TimeProvider.CreateTimer` **does** honor `FakeTimeProvider.Advance`. 100 ms interval is coarse enough to be cheap (10 Hz per active wrapper × typically ≤5 concurrent wrappers on a page = 50 Hz total, well under the 30+ Hz polling-wasteful threshold the UX spec flags in §1655). Tick interval is NOT configurable — `SyncPulseThresholdMs` must be ≥ `100` per ordered-validator. Timer disposed on `IAsyncDisposable.DisposeAsync` via `ITimer.DisposeAsync`. **D5 text is the single source of truth on the timer primitive; ADR-021 agrees.** | Cascading `Task.Delay(300) → Task.Delay(1700) → Task.Delay(8000)` would require three restart paths under state changes (transition arrives mid-delay) and leaks cancellation tokens on circuit teardown. A single tick loop is simpler and matches the scope-lifetime-teardown pattern from 2-3 ADR-019. Choosing `ITimer`-via-`TimeProvider` over `PeriodicTimer` is explicitly about test determinism — the difference is invisible in production, decisive under `FakeTimeProvider`. |
| D6 | **Wrapper's local state is a single `LifecycleUiState` record** — `(CommandLifecycleState Current, LifecycleTimerPhase TimerPhase, string? MessageId, bool IdempotencyResolved, DateTimeOffset LastTransitionAt, string? RejectionMessage)`. Mapped from incoming `CommandLifecycleTransition` + `LifecycleThresholdTimer.Phase` in a single pure function `LifecycleUiState.From(...)`. The component re-renders by overwriting `_state` and calling `StateHasChanged`. | Immutable record + pure mapper = easy snapshot tests. No scattered `bool _isSubmitting`, `bool _isPulsing`, `bool _showToast` fields — avoids state-drift bugs of the sort Story 2-3's `LifecycleEntry` concurrency discussion warned against. |
| D7 | **Existing `FluentSpinner`-in-submit-button stays; wrapper does NOT overlay a second spinner.** Story 2-1/2-3's `CommandFormEmitter.EmitSubmitMethod` already emits `FluentSpinner` inside the submit `FluentButton` when `LifecycleState.Value.State == CommandLifecycleState.Submitting` (see emitter L390). Wrapper adds **only** the surrounding `aria-live` region ("Submitting…") + post-submit feedback. Two overlapping spinners would confuse screen readers and look like a bug. | Minimum-surface-area change. Keeps `CommandFormEmitter` Story 2-1 snapshot diff to ONE change (emitter wrap tag in Task 4.1) instead of TWO (remove in-button spinner + emit wrapper replacement). Also correctly scopes "button busy" (in-button spinner) vs. "system processing" (wrapper live region). |
| D8 | **UX spec's "FluentProgressRing" references are read as `FluentSpinner`** per the Fluent UI v5 rename (FluentProgressRing is deprecated in v5 in favor of FluentSpinner — confirmed via `check_project_version` MCP on pinned 5.0.0-rc.2-26098.1). No visual change (same component); naming in this story consistently uses **`FluentSpinner`**. Documented in Dev Notes so future-2-4 review / 9-5 docs use v5-current names. | UX spec was drafted pre-Fluent-v5-GA. The emitter already uses `FluentSpinner` (verified L390 of `CommandFormEmitter.cs`). Consistent naming prevents "does the code match the spec?" drift confusion. |
| D9 | **CSS is scoped to `FcLifecycleWrapper.razor.css`**, not global. Produces `_content/Hexalith.FrontComposer.Shell/FcLifecycleWrapper.razor.css` at build. Zero-override contract (UX spec §1690) honored — no Fluent UI design token hacking, no shadow-DOM penetration. Only the wrapper's own layout-structural rules (flex container + absolute-positioned live region) + sync-pulse keyframes + reduced-motion media query. | Scoped CSS prevents accent-color pulse leaking to adopter code; matches `FcFieldPlaceholder.razor.css` precedent (Story 1-8 placeholder component ships scoped). |
| D10 | **Sync pulse keyframe uses `animation: fc-lifecycle-pulse 1.2s ease-in-out infinite`** targeting `outline-color` only — NOT `opacity`, NOT `background-color`, NOT `box-shadow`. `outline-color` animation leaves the focus ring (also implemented via `outline`) visually coexisting because Fluent UI focus ring uses `outline` as its primary keyboard-focus indicator and the pulse is applied to `outline-color` on the wrapper's own `<div>` — the focus ring lives on the internal form's focusable children, which are different elements. Explicit test asserts both coexist (Task 5.3). | UX-DR49 "focus ring never dimmed or suppressed" is the critical invariant. Opacity / box-shadow pulses would dim the focus ring by overlaying. `outline-color` on the parent wrapper is the narrowest pulse target that preserves descendant focus. |
| D11 | **`prefers-reduced-motion: reduce` replaces pulse keyframe with static `outline: 2px solid var(--accent-fill-rest)`** via a `@media (prefers-reduced-motion: reduce) { .fc-lifecycle-pulse { animation: none; outline: 2px solid var(--accent-fill-rest); } }` CSS block. No JS detection, no `@inject IJSRuntime` polling — CSS handles it natively, component logic is motion-agnostic. The timer phase still fires (still-syncing text + action prompt still land on time); only the pulse animation is replaced. | UX spec §958-978 mandates "reduced motion preference respected via prefers-reduced-motion: reduce; motion replaced with instantaneous state changes". CSS-only implementation means: zero JS test surface for reduced-motion (only a markup-has-media-query assertion in Task 5.3), zero SSR-vs-CSR divergence, honours OS-level + per-page user control. |
| D12 | **Four configurable thresholds live on `FcShellOptions`** (extend existing — do NOT create `FcLifecycleOptions` nor bind a second options type): `SyncPulseThresholdMs` (default 300, range [100, 10_000]), `StillSyncingThresholdMs` (default 2_000, range [300, 30_000]), `TimeoutActionThresholdMs` (default 10_000, range [2_000, 300_000]), `ConfirmedToastDurationMs` (default 5_000, range [1_000, 60_000]). Ordering: `SyncPulseThresholdMs < StillSyncingThresholdMs < TimeoutActionThresholdMs` enforced by custom `IValidateOptions<FcShellOptions>` registered via `AddOptions<FcShellOptions>().Validate(...)`. Wrapper takes threshold values via `IOptionsMonitor<FcShellOptions>` to pick up config reload; reading `CurrentValue` on each tick is constant-time and supports Counter sample's hot-reload scenario. | Single source of truth for shell config (already used by Story 2-2 `FullPageFormMaxWidth`, `DataGridNavCap`, `LastUsedDisabled`, `EmbeddedBreadcrumb`). Adding a 2nd options type would fragment the shell-config surface. `IOptionsMonitor` over `IOptions` lets dev-mode hot-reload tune thresholds without circuit restart (UX spec §1682 "Calibrate thresholds per deployment"). |
| D13 | **Tenant/user isolation (L03) is INHERITED, not enforced at this component.** `FcLifecycleWrapper` has no persisted state, no `IStorageService` writes, no user-keyed dictionary. It renders circuit-scoped `ILifecycleStateService` state directly. Same rationale as Story 2-3 D13 — the service is already per-circuit (2-3 D12 scoped lifetime); the wrapper is a read-only view over it. No `IUserContextAccessor` / `ITenantContextAccessor` dependency. | L03 applies to services that *persist* per-user data. Lifecycle state is in-memory + ephemeral; the wrapper is a renderer not a persister. |
| D14 | **Subscription happens in `OnInitialized` (sync), not `OnInitializedAsync`.** `Subscribe()` is synchronous per 2-3 ADR-018 interface contract, and doing it sync in `OnInitialized` guarantees the replay-on-subscribe callback fires before `OnAfterRenderAsync` — so first render already reflects current state, not "loading". `IDisposable` captured in a private field and released in `Dispose`. Matches Blazor component idiom; no `await` = no re-entrant `StateHasChanged` from the subscribe callback itself. | If subscribe were async, the replay callback could land during the `await`, before the component's render tree existed, causing `StateHasChanged` to no-op silently. Sync-subscribe-in-OnInitialized is the Blazor-idiomatic pattern documented in 2-3 ADR-018. |
| D15 | **`CorrelationId` parameter change semantics: if the wrapper's `CorrelationId` changes between renders, it disposes the old subscription, constructs a new `LifecycleThresholdTimer`, and subscribes to the new CorrelationId in `OnParametersSet`.** Empty/null CorrelationId renders an empty fragment (no subscription, no timer). | Component re-use across DataGrid rows + expand-in-row popover open/close — each row can dispatch its own command with a fresh CorrelationId, and the wrapper must re-bind without unmount+remount. Empty-id passthrough matches the generated form's `@_submittedCorrelationId` which is `""` before first submit. |
| D16 | **Message bar auto-dismiss is implemented via `LifecycleThresholdTimer` not `FluentMessageBar.Timeout`**. Rationale: `FluentMessageBar.Timeout` (v5) internally sets up its own timer, which is not `TimeProvider`-fakeable and races with our timer on circuit teardown. Instead, the wrapper holds its own "dismiss at" timestamp and emits `LifecycleTimerPhase.Terminal` → resets `_state` to `Idle` → removes the message bar. Gives `FakeTimeProvider` full control in Task 5.2 threshold tests. | Enables deterministic bUnit tests that advance time by `(5_001)ms` and assert the message bar is gone. Avoids two-timer race on disposal. |
| D17 | **Rejected state's `FluentMessageBar` does NOT auto-dismiss in v0.1.** No time-based removal, only user dismissal via the bar's default `X` close button. Matches epics.md §1034 AC: "FluentMessageBar (Danger) renders with no auto-dismiss" — v0.1 honours the AC even though Story 2-5 will add the domain-specific message body. Confirmed auto-dismisses (5 s); Rejected sticks until user dismisses or next submit clears the wrapper. | FR30 "exactly one user-visible outcome" + NFR47 "zero silent failures". An auto-dismissing error is a silent failure for the user who looked away. Sticky-until-dismissed is the safe default. |
| D18 | **10 s action-prompt button reloads the current page via `NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true)`**, NOT a silent refetch + `ResetToIdle` dispatch nor a retry of the original command. Rationale: v0.1 degraded-path UX is "something's wrong, start over safely" — retry logic would require Story 5-5 command-idempotency guarantees that aren't yet landed. Page reload is the safe universal recovery that preserves form input in browser history (back button) if the user wants to retry manually. ADR-022 locks this choice in. | Minimum-viable recovery. Smarter recovery (retry, contact-support, auto-reconnect) lands with Story 5-3 reconciliation. Do not ship an incomplete retry path in 2-4 that Story 5-5 will re-architect. |
| D19 | **Wrapper runs ONE `LifecycleThresholdTimer` per instance, reset on every Acknowledged-or-later transition.** The timer is created in `OnParametersSet` when CorrelationId first binds, and `.Reset(transition.LastTransitionAt)` is called on every transition received from the service. The timer's Phase starts at `NoPulse` and advances only when `UtcNow - LastTransitionAt` crosses each threshold. On Confirmed/Rejected, the timer stops (Phase = `Terminal`) and the wrapper pivots to the message-bar-with-auto-dismiss path. | Single-timer design prevents Phase overlap (e.g., pulse animation still running while message bar is visible). Reset-on-LastTransitionAt honours D3 monotonic anchor — if a Syncing transition arrives after a reconnect, the timer resets to the original transition time, not now. |
| D20 | **Consumer domain projects MUST add `<ProjectReference Include="...\Hexalith.FrontComposer.Shell\..." />` alongside the existing `SourceTools` analyzer reference.** Amelia review 2026-04-16 Blocker 1 resolution. The emitter-wrap strategy (ADR-020) requires `FcLifecycleWrapper` to be resolvable in the consumer's compilation unit where `.g.cs` is generated. Counter.Domain already references `Microsoft.FluentUI.AspNetCore.Components` and `Fluxor.Blazor.Web` directly (verified `samples/Counter/Counter.Domain/Counter.Domain.csproj:14-15`) — adding `Hexalith.FrontComposer.Shell` as another required reference is a consistent extension of the existing "consumer domain directly references any runtime library emitted in its generated code" pattern. **Task 4.2 adds `<ProjectReference>` to `samples/Counter/Counter.Domain/Counter.Domain.csproj`** and documents the adopter requirement in Dev Notes. Architectural layering is preserved: Shell→Contracts is untouched; domain→Shell is the reverse direction (no cycle); it mirrors how domain→FluentUI already works. The mid-term cleaner split (lean `Hexalith.FrontComposer.Components` package sibling to Shell, containing only emitted-into-domain components) is **Story 9-x scope** — flagged in Known Gap G12. | Preserves the build + the architectural principle that emitted code's type references must be resolvable in the consumer project. Alternative (move wrapper to Contracts) would force Contracts to pull FluentUI + AspNetCoreFramework, violating §1144 dependency-free invariant. Alternative (new Components package now) is out-of-scope project-structure work. Shell reference is the minimum consistent extension of the existing pattern. |
| D21 | **`CommandFormEmitter` emits `using Hexalith.FrontComposer.Shell.Components.Lifecycle;` in every generated form's `.g.cs` using-block** (Amelia review 2026-04-16 Blocker 2 resolution). Generated `.g.cs` compiles as plain C# via `RenderTreeBuilder.OpenComponent<T>(seq++)` — it does NOT consult `_Imports.razor`. Therefore a using-directive must be written into the emitted source itself. The emitter's using-directive block at `CommandFormEmitter.cs:34-48` (current implementation) is the insertion point. Task 4.1 carries this edit; Task 4.2 (originally proposed to edit `Shell/_Imports.razor`) is REPURPOSED to the D20 csproj edit — the original `_Imports.razor` edit is a no-op and is DROPPED. Fully-qualified type names in the emitter output were considered (`global::Hexalith.FrontComposer.Shell.Components.Lifecycle.FcLifecycleWrapper`) but rejected because the rest of the emitted code uses short names + usings; mixing styles harms snapshot readability. | `_Imports.razor` only scopes the namespace set for `.razor` files inside the same project — it has no effect on source-generated `.g.cs`. Story 2-1's existing emit uses short names `FluentSpinner`, `EditForm`, etc. via a `using` block (verified `CommandFormEmitter.cs:40-48`). Append one line — `using Hexalith.FrontComposer.Shell.Components.Lifecycle;` — for consistency. |
| D22 | **`RejectionMessage` parameter MUST render as plain text via `@RejectionMessage`, NEVER via `@((MarkupString)RejectionMessage)` or `MarkupString.Create(...)`** (advanced-elicitation Red Team RT-1 2026-04-16 — **CRITICAL XSS prevention**). Blazor's default `@expression` rendering HTML-encodes strings, which is the required behaviour. A dev agent (or a well-intentioned Story 2-5 contributor) implementing "rich rejection messages" via `MarkupString` would open an XSS vector the moment any adversary-controlled string reaches `RejectionMessage` (a realistic surface under Epic 7 multi-tenancy if a tenant's domain-rejection event payload ever round-trips through `RejectionMessage`). This decision is tested by `LifecycleUiStateTests.RejectionMessage_with_script_tag_renders_HTML_encoded` — assert that `RejectionMessage = "<script>alert(1)</script>"` produces markup containing `&lt;script&gt;` (escaped), NOT executable `<script>`. Story 2-5's domain-specific-message work may add a SEPARATE opt-in "allow-rich-formatting" parameter with a sanitizer (e.g., HtmlSanitizer library) — but that is a Story 2-5 decision; v0.1 is strict plain-text. | XSS in a lifecycle-status surface is indefensible — the wrapper renders on every command rejection, so a single injection compromises every user who sees that rejection. Plain-text default + explicit opt-in-with-sanitizer path is the OWASP-correct pattern. Tax: zero — Blazor's default is already HTML-encoding; we just contract it explicitly so no one "improves" it later. |
| D23 | **`LifecycleThresholdTimer` accepts an optional `Func<bool>? isDisconnected` constructor parameter** (advanced-elicitation Hindsight H-1 2026-04-16). Default `null` in v0.1. When non-null, the timer's `Tick()` evaluates the delegate first; if `true`, Phase immediately advances to `ActionPrompt` regardless of elapsed time (Story 5-3's Disconnected state escalation). **In v0.1 this parameter is ALWAYS null** — the wrapper's constructor passes `null` and v0.1 behaviour is unchanged. Story 5-3 will populate the delegate with a `HubConnectionState` check once 2-3's D11-cut `ConnectionState` contract lands. **Why add the seam now:** adding a constructor parameter to a standalone class is append-safe (D1 append-only stability contract permits constructor arg additions as long as existing callers compile); retrofitting the disconnect hook in 5-3 would require rewriting the timer class structurally. Cost today: 3 LOC in the constructor + 2 LOC in `Tick()`. Cost deferred: 5-3 refactor of a class tested by 3 FsCheck properties + 4 unit tests. | L08 party-mode-then-elicitation discipline applied: Hindsight method specifically asked "what will 5-3 regret that 2-4 didn't do?" — the answer was the missing hook. Append-only parameter addition honours D1; v0.1 behaviour is identical; 5-3 plug-in is mechanical. Zero v0.1 risk, substantial 5-3 simplification. |

---

## Architecture Decision Records

### ADR-020: Emitter-Driven Wrap (CommandFormEmitter inserts `<FcLifecycleWrapper>`), Not Runtime Cascade

- **Status:** Accepted
- **Context:** Two ways to ensure every command form renders inside `FcLifecycleWrapper`:
  1. **Runtime cascade** — `FrontComposerShell.razor` wraps the `<Router>` outlet in a `<CascadingValue Name="LifecycleWrapperHost">`, and every generated renderer calls into it via a service-locator pattern to report its CorrelationId. Wrapper is rendered *once* at the shell level, pulls data from the cascade.
  2. **Emitter wrap** — `CommandFormEmitter` emits `<FcLifecycleWrapper CorrelationId="@_submittedCorrelationId">` as the outer element of the form. Wrapper instance exists *per-form*, scoped to the DOM subtree of the form it wraps.
- **Decision:** Take option 2 (emitter wrap). Each command form gets its own `FcLifecycleWrapper` instance with its own CorrelationId binding. Wrapper instance lifetime matches the form's lifetime.
- **Consequences:** (+) No service-locator hack. (+) CorrelationId binds via standard Blazor parameter flow, no cascade plumbing. (+) Multiple concurrent commands (Counter page today has three renderers side-by-side) each get independent visual feedback. (+) Wrapper's local timer/subscription state is naturally co-located with its form — no shared-wrapper mutex. (-) N wrapper instances for N concurrent forms means N active `LifecycleThresholdTimer`s at 10 Hz each = 10N Hz total. For Counter sample (3 wrappers) = 30 Hz, well under the polling-wasteful threshold. At 20 wrappers per page, 200 Hz — still acceptable for Blazor Server circuit load; if it becomes a problem, introduce a shared shell-scoped tick multiplexer in Epic 5 (not now). (-) Snapshot tests for `CommandFormEmitter` re-approve (Task 4.3).
- **Rejected alternatives:**
  - **Runtime cascade at shell layer** — tighter coupling between shell + renderer, service-locator anti-pattern, shared timer becomes a bottleneck under high form count.
  - **Per-command emitted wrapper subclass** (`{Command}LifecycleWrapper.g.razor`) — defeats ADR-017 cross-command aggregator design. Wrapper is generic; typing it per-command buys nothing.
  - **Wrap at `CommandRendererEmitter`** (Story 2-2 output) — the renderer's popover/fullpage chrome is above the form; wrapping at renderer level would apply pulse/messages to the chrome, not the form. Bad UX (breadcrumb would pulse). Form-level wrap is the right layer.
  - **Wrap inside `<EditForm>`** — can't wrap non-direct-child components inside an `<EditForm>` children region without breaking the form submit binding. Wrap outside the EditForm.

### ADR-021: `LifecycleThresholdTimer` as Testable Standalone Class (not Inline `Task.Delay` Chain)

- **Status:** Accepted
- **Context:** The wrapper needs three time-based phases (pulse at 300 ms, text at 2 s, prompt at 10 s). Three approaches considered:
  1. **Inline `async Task.Delay` chain** — `await Task.Delay(300, ct); Phase = Pulse; await Task.Delay(1700, ct); Phase = StillSyncing; await Task.Delay(8000, ct); Phase = ActionPrompt;`
  2. **Three separate `System.Threading.Timer`s** — one per threshold, each scheduled at a different absolute delay.
  3. **Single tick loop (`PeriodicTimer`) with anchor+phase recalculation** — timer fires every 100 ms, recomputes `Phase` from `UtcNow - LastTransitionAt`, fires `OnPhaseChanged` only when Phase advances.
- **Decision:** Take option 3 (single tick loop). Encapsulated in `LifecycleThresholdTimer` (D4) with a single `TimeProvider.CreateTimer` tick source (`ITimer`, NOT `PeriodicTimer` — see D5 for the `FakeTimeProvider` determinism rationale). Phase computation is pure: `(elapsed < pulseMs) ? NoPulse : (elapsed < stillSyncingMs) ? Pulse : (elapsed < timeoutMs) ? StillSyncing : ActionPrompt`. Fakeable via `FakeTimeProvider`.
- **Primary rationale (Winston review 2026-04-16):** the decisive argument is **anchor recomputation under circuit reconnect / Story 5-4 durable-replay**. With three scheduled timers at absolute offsets (`+300ms`, `+2000ms`, `+10000ms`), when a Syncing transition arrives after a reconnect with a `LastTransitionAt` that is 1500 ms in the past, you must cancel all three and reschedule with adjusted offsets (`-1200ms`, `+500ms`, `+8500ms`) — the `-1200ms` is not a real timer, so you synchronously fire `Phase=Pulse` + schedule the remaining two. That is bespoke orchestration per-reconnect. A single tick loop recomputes Phase from `UtcNow - LastTransitionAt` on every tick; reconnect is literally a no-op (next tick observes the new anchor and Phase lands correctly).
- **Consequences:** (+) Reconnect semantics are trivial — no special orchestration. (+) Single cancellation path (dispose the `ITimer`). (+) Testable with `FakeTimeProvider.Advance(TimeSpan)` without bUnit (Task 5.4 — 4 pure unit tests + 3 FsCheck properties per Murat review). (-) 100 ms tick is coarser than "exact 300 ms" — phase transition lands between 300-399 ms after anchor, visible latency 0-99 ms. For a user-perceived pulse threshold, this sub-100ms jitter is below JND (just-noticeable difference for animation onset, typically ~150 ms for abstract UI feedback). (-) Slight CPU cost vs. scheduled timers — 10 Hz per wrapper, see ADR-020 consequences for total load analysis.
- **Rejected alternatives:**
  - **Three separate timers** — the reconnect-anchor recomputation problem above is the primary reason this fails. Secondary reasons: three cancellation paths, race on Confirmed-before-Pulse-threshold (need to stop all three), increased allocation churn under fast-path (≤300 ms confirm fires and immediately cancels all three timers).
  - **`Task.Delay` chain** — same reconnect-anchor problem as three-timers, plus cancellation token leaks on dispose mid-delay; restart on anchor change requires `TaskCompletionSource` orchestration; testing requires `Task.Delay` interception which `FakeTimeProvider` doesn't do cleanly.

### ADR-022: v0.1 10 s Action Prompt = Full Page Reload, Not Silent Retry

- **Status:** Accepted
- **Context:** At 10 s in Syncing, the UX spec mandates "action prompt with refresh option" (NFR14, epics AC §999). Three prompt behaviours considered:
  1. **Full page reload** (`NavigationManager.NavigateTo(Uri, forceLoad: true)`)
  2. **Silent retry** — re-dispatch the original command + reset wrapper state
  3. **Manual re-submit prompt** — button that says "Retry" which does nothing but let the user click submit again (no auto-dispatch)
- **Decision:** Take option 1 (page reload). Button label: **"Start over"** (Winston review 2026-04-16 — "Refresh" implies the system will sync; "Start over" admits we don't know). On click: `NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true)`.
- **Consequences:** (+) No dependency on Story 5-5 command-idempotency guarantees — a page reload is idempotent at the browser level (the back button still has the form input). (+) Minimum-surface-area UX. (+) User still retains agency — they can choose not to reload if they know the command completed server-side and want to keep the current session. (-) Loses in-flight form state that wasn't submitted elsewhere. Mitigation: Story 2-5 adds form abandonment protection on navigation which warns before page-reload. (-) Doesn't resolve the underlying slow-confirm problem — only gives the user an escape hatch. That's fine; Story 5-3/5-4 deliver real reconciliation, 2-4 just has to not hang.
- **Rejected alternatives:**
  - **Silent retry** — requires command idempotency (Story 5-5) to avoid double-effects. Not available in v0.1. Would create FR30 "exactly one outcome" violations.
  - **Manual re-submit prompt** — ambiguous to user ("did the first one go through? if I click retry, will I double-buy?"). Worst-of-both.
  - **Contact-support link** — not applicable to localhost Counter sample; would require adopter to wire a support URL, so deferrable to adopter configuration in Epic 6.

### ADR-023: `IOptionsMonitor.CurrentValue` Hot-Reload — Next-Tick Take-Effect, Never Retroactive Phase-Shorten

- **Status:** Accepted (added 2026-04-16 post-party-mode review — Winston concern on G9)
- **Context:** Task 2.4's `LifecycleThresholdTimer` reads `IOptionsMonitor<FcShellOptions>.CurrentValue` on every 100 ms tick (D12). In production with adopter-driven config hot-reload (e.g., `appsettings.json` file-watcher, Azure App Config push), threshold values can change **mid-submission**. Question: what are the semantics when `SyncPulseThresholdMs` drops from 300 → 100 while a command is in `Syncing` with `elapsed = 150ms`?
  - **Option A (retroactive):** instantly transition Phase from `NoPulse` to `Pulse` on the next tick because `150 > 100`.
  - **Option B (monotonic no-shorten):** refuse to retroactively advance Phase; only apply the new threshold from the NEXT transition (or after Phase=`Terminal` reset).
  - **Option C (current-submission-frozen):** snapshot thresholds on Acknowledged receipt; hot-reload only affects the NEXT submission.
- **Decision:** Take **Option A** — retroactive next-tick evaluation. Phase is always a pure function of `(UtcNow - LastTransitionAt, currentThresholds)` computed on each tick. If thresholds change mid-submission, the next tick observes the new thresholds and Phase may advance (or theoretically regress, though Phase is designed monotonic — see D19 single-timer-reset invariant — so regression is impossible unless `Reset` is called). **Explicit invariant:** `Phase` never regresses without `Reset`; threshold changes can only advance Phase on the next tick boundary, never roll it back. Document this with a **rate-limited runtime-log line** (Information level, no new diagnostic ID — **max 1 log entry per minute per circuit** to prevent log-flood under misconfigured adopter polling per advanced-elicitation Chaos Monkey CM-3 2026-04-16) when `OnChange` callbacks fire during an active submission, for adopter observability.
- **Consequences:** (+) Simple mental model — Phase is derived state, not stored state. (+) Aligns with D4/D6 "pure function" design philosophy. (+) Adopter config reload has predictable effect (applies within 100 ms). (-) An adopter who drops `SyncPulseThresholdMs` from 300 → 100 during a live submission may see the pulse suddenly appear on a 150 ms-old command that would not have pulsed under the old config. Acceptable: hot-reload is a deliberate adopter action; if they need finer control they can use IOptions (static-at-startup) instead of IOptionsMonitor. G9 remains an untested edge case with cheap bUnit coverage added in Task 5.3b per Murat review.
- **Rejected alternatives:**
  - **Option B (monotonic no-shorten)** — requires storing per-submission "highest-observed Phase"; adds state to `LifecycleThresholdTimer` that D4 deliberately avoids.
  - **Option C (snapshot-on-Acknowledged)** — adopter config changes would have UNOBSERVABLE effect mid-submission, making hot-reload feel broken. Surprise principle violated.

---

## Acceptance Criteria

### AC1: Submitting → live-region announcement + disabled submit + preserved focus ring

**Given** a command form rendered by `CommandFormEmitter` (therefore wrapped in `<FcLifecycleWrapper CorrelationId="@_submittedCorrelationId">`)
**When** the user submits and the lifecycle transitions `Idle → Submitting`
**Then** the wrapper renders a `<div role="status" aria-live="polite">` containing the localized "Submitting…" announcement
**And** the in-button `FluentSpinner` (emitted by Story 2-1 `CommandFormEmitter`) renders inside the submit button
**And** the submit button is disabled (via existing emitter logic at L382-384 of `CommandFormEmitter.cs`)
**And** the focus ring on any focusable element *within the wrapped form* remains visible and unchanged (UX-DR49)
**And** no sync-pulse CSS class is applied yet
**And** **rapid re-submit (double-click < 150ms) is blocked by the already-disabled submit button from D7 + existing `CommandFormEmitter.cs:382-384` logic** — the second click is swallowed by the native `disabled` attribute; no duplicate dispatch, no wrapper state glitch (Sally review 2026-04-16)

References: FR23, UX-DR2, UX-DR49, NFR11, Decision **D2, D6, D7, D10**, ADR-020

### AC2: Confirmed within `SyncPulseThresholdMs` → pulse never fires (brand-signal fusion)

**Given** the wrapper observed an `Acknowledged` transition at time `T0`
**When** a `Confirmed` transition arrives within `FcShellOptions.SyncPulseThresholdMs` (default 300 ms) of `T0` (i.e., `LastTransitionAt(Confirmed) - LastTransitionAt(Acknowledged) < SyncPulseThresholdMs`)
**Then** the sync-pulse CSS class is never applied to the wrapper root (pulse never fires)
**And** the "Still syncing…" badge is never rendered
**And** the action prompt is never rendered
**And** the wrapper transitions directly to the Confirmed message-bar state
**And** the lifecycle resolves invisibly (only the Confirmed `FluentMessageBar` is rendered)

References: FR23, UX-DR48, NFR11, Decision **D4, D5, D12, D19**, ADR-021

### AC3: 300 ms–2 s in Syncing → sync pulse

**Given** the wrapper is in `Syncing` state
**When** `UtcNow - LastTransitionAt` is in the range `[SyncPulseThresholdMs, StillSyncingThresholdMs)` (defaults: `[300, 2_000)`)
**Then** the wrapper root receives the `.fc-lifecycle-pulse` CSS class
**And** the `outline-color` animation fires on the wrapper's outer `<div>` using `var(--accent-fill-rest)`
**And** the focus ring on any descendant focusable element remains untouched
**And** **NO aria-live announcement fires during the Pulse phase** — the pulse is visual-only, intentionally silent for screen-reader users (Sally review 2026-04-16: two `aria-live="polite"` utterances 2s apart on the same region risk a stale "Syncing…" announcement queuing ahead of the Confirmed announcement; silence is the right default when things are working). The first SR announcement in the post-Acknowledged lifecycle lands at AC4 "Still syncing…" — 2s in — honoring the user-visible escalation threshold NFR13.

References: FR23, UX-DR2, UX-DR48, UX-DR49, NFR12, Decision **D10, D11, D12**, ADR-021

### AC4: 2 s–10 s in Syncing → "Still syncing…" inline text

**Given** the wrapper is in `Syncing` state
**When** `UtcNow - LastTransitionAt` is in the range `[StillSyncingThresholdMs, TimeoutActionThresholdMs)` (defaults: `[2_000, 10_000)`)
**Then** a `FluentBadge` (Appearance=Accent) is rendered below the wrapped form with localized text "Still syncing…"
**And** the pulse animation continues on the wrapper root
**And** the live region re-announces "Still syncing…" via `aria-live="polite"` (screen reader gets the escalation)

References: FR23, UX-DR2, NFR13, Decision **D12, D19**, ADR-021

### AC5: >10 s in Syncing → action prompt with manual refresh

**Given** the wrapper is in `Syncing` state
**When** `UtcNow - LastTransitionAt` >= `TimeoutActionThresholdMs` (default 10_000 ms)
**Then** a `FluentMessageBar` (Intent=Warning) is rendered below the wrapped form with:
  - Title: localized **"Action needed: the system hasn't confirmed your submission"** (active-voice per Sally review 2026-04-16 — direct verb at the start is what SR users hearing `aria-live="assertive"` interruption deserve)
  - Body: localized "You can wait, or start over from a fresh page."
  - Action button: `FluentButton` (Appearance=Accent) labeled **"Start over"** (Winston review 2026-04-16 — "Refresh" implies sync; "Start over" admits we don't know)
**And** the `FluentBadge` "Still syncing…" is replaced by the message bar
**And** clicking "Start over" calls `NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true)` (ADR-022)
**And** the live region announces the escalation via `aria-live="assertive"` (escalation increases politeness level)

References: FR23, UX-DR2, NFR14, Decision **D5, D12, D18**, ADR-021, ADR-022

### AC6: Confirmed → success message bar with auto-dismiss

**Given** the wrapper observes a `Confirmed` transition
**When** the transition arrives at any phase (`NoPulse`, `Pulse`, `StillSyncing`, or `ActionPrompt`)
**Then** a `FluentMessageBar` (Intent=Success) is rendered with localized title "Submission confirmed"
**And** the `LifecycleThresholdTimer` stops (Phase=`Terminal`), no pulse, no badge, no action prompt rendering
**And** the live region announces "Submission confirmed" via `aria-live="polite"`
**And** after `FcShellOptions.ConfirmedToastDurationMs` (default 5_000 ms) from the Confirmed `LastTransitionAt`, the message bar is removed and the wrapper returns to `Idle` (rendering only the `ChildContent` form)
**And** the submit button re-enables (existing `CommandFormEmitter` logic at L380-384 treats Confirmed as re-submittable)

References: FR23, FR30, UX-DR2, Decision **D12, D16, D19**

### AC7: Rejected → danger message bar, no auto-dismiss

**Given** the wrapper observes a `Rejected` transition
**When** the transition's `RejectionMessage` parameter is populated by Story 2-5 (future) or null in v0.1
**Then** a `FluentMessageBar` (Intent=Error) is rendered with:
  - Title: localized "Submission rejected"
  - Body: `RejectionMessage` if set, else localized generic "The command was rejected. Please review your input and try again."
**And** the message bar has NO auto-dismiss (D17); user must click the built-in close button to dismiss
**And** the live region announces "Submission rejected" via `aria-live="assertive"`
**And** the submit button re-enables (existing emitter logic treats Rejected as retryable)
**And** any in-flight pulse animation is stopped (Phase=`Terminal`)

References: FR30, UX-DR2, NFR46, NFR47, Decision **D12, D17, D19**

### AC8: `prefers-reduced-motion: reduce` → instant state indicator, focus ring preserved

**Given** the user has `prefers-reduced-motion: reduce` set (OS-level or browser-level)
**When** the wrapper enters `Syncing` state and would normally show the pulse animation
**Then** CSS media query `@media (prefers-reduced-motion: reduce)` replaces `animation: fc-lifecycle-pulse 1.2s ...` with `animation: none; outline: 2px solid var(--accent-fill-rest);`
**And** the outline is static (no animation keyframe)
**And** the focus ring on any descendant focusable element remains fully visible and unchanged
**And** the phase timer still fires normally (still-syncing text still lands at 2 s, action prompt still lands at 10 s — only the *animation* is replaced, not the state logic)

References: UX-DR2, UX-DR49, Decision **D9, D10, D11**

### AC9: End-to-end latency gates — P95 < 800 ms cold / P50 < 400 ms warm

**Given** the Counter sample Aspire topology running on localhost
**When** the Playwright E2E test fires 50 Increment commands against a cold actor (first after fresh `dotnet run`) and 100 commands against a warm actor (same session)
**Then** the measured `click → Confirmed state visible in UI` latency satisfies:
  - **P95 cold actor < 800 ms** (NFR1)
  - **P50 warm actor < 400 ms** (NFR2)
**And** the CI job `command-latency-gate` fails the build if either percentile is breached
**And** the test uses `FcLifecycleWrapper`'s DOM signal (`FluentMessageBar` Intent=Success visible) as the "Confirmed in UI" end-point, NOT an internal Fluxor state subscription — because NFR1/NFR2 measure the USER-VISIBLE confirmation, not the backend lifecycle state

References: NFR1, NFR2, NFR88, Decision **D2, D6**, memory/feedback_no_manual_validation.md

---

## Tasks / Subtasks

> Checkboxes are intentionally unchecked. Dev agent (Amelia) marks them `[x]` as each lands. Numbers align to the AC quick index.

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

## Known Gaps (Explicit, Not Bugs)

Per lesson **L10** (deferrals name a story, not an epic), every gap below has an owning story number.

| # | Gap | Why deferred | Owning story |
|---|---|---|---|
| G1 | **Pure 0-field inline-button lifecycle overlay** — wrapper is emitted only around `<EditForm>` (Task 4.1). Pure dispatch buttons (0-field commands that bypass the form path) do NOT get the surrounding `aria-live` region in v0.1. Counter sample's `IncrementCommandRenderer` *does* get the wrapper because it still emits an EditForm in the popover. | Scope containment (L06 ≤25 decisions budget). Pure-button overlay requires a different markup strategy (position:absolute overlay) and is a distinct UX problem from form-surround. | **Story 4-5** (Expand-in-row detail and progressive disclosure) |
| G2 | **`IdempotencyResolved==true` "already done" UX differentiation** — wrapper logs HFC2101 Info when the flag is observed but renders the same Confirmed `FluentMessageBar` copy. Story 2-5's domain-specific-message work will branch on this flag to render "This order was already approved (by another user). No action needed." **Semantic-overloading warning for Story 2-5 + Story 5-4** (advanced-elicitation Hindsight H-2 2026-04-16): `IdempotencyResolved==true` means two different things depending on arrival path — **2-5 context:** another user / tenant produced the same Confirmed before this user's submission (the "someone else did it" UX); **5-4 context:** this user's OWN prior Confirmed is being replayed after a circuit reconnect (a pure-UX no-op, NOT "someone else did it"). Rendering 2-5's "already done by another user" copy under 5-4 would be WRONG and user-hostile. **Required reconciliation:** Story 5-4 must either extend the transition record with a second flag distinguishing cross-user from self-reconnect OR Story 2-5's UX copy must be written to be safe under both interpretations ("This was already confirmed — no action needed" works for both). 2-4 surfaces the signal; 2-5/5-4 teams must coordinate the copy. | Story 2-5 owns all rejection + idempotent-outcome UX copy. v0.1 wrapper does not ship the branched copy; it just surfaces the signal for 2-5. | **Story 2-5** (Command rejection, confirmation & form protection) + **Story 5-4** (reconnection reconciliation) |
| G3 | **Disconnected state immediate escalation** — UX-DR2 mentions "Disconnected state"; Story 2-3 D11 cut `ConnectionState` from `ILifecycleStateService`. The wrapper in v0.1 does NOT observe SignalR state; on disconnect during Syncing it falls through to the 10 s action prompt via normal timing. **Adopter release-note hint (Sally review 2026-04-16):** surface in `CHANGELOG.md` / release notes a plain-language bullet "v0.1 FcLifecycleWrapper does not detect SignalR disconnection; a dropped connection during submission surfaces as the normal 10-second 'Start over' action prompt rather than an immediate disconnect message. Full disconnect UX lands in v0.5 (Epic 5)." So adopters set correct user expectations. | Story 5-3 designs the real `ConnectionState` contract (with `LastConnectedAt`, `ReconnectAttempt`, `Reason` fields that Story 2-3 D11 identified as needed). Shipping a stub here would be the cargo-culting the Occam review already flagged. | **Story 5-3** (SignalR connection and disconnection handling) |
| G4 | **Domain-specific rejection message formatting** — v0.1 wrapper renders `RejectionMessage` parameter if set, else a generic localized fallback. The "[What failed]: [Why]. [What happened to the data]." format from epics §1031 is Story 2-5's work. | Separation of concerns — wrapper is the UI container; 2-5 is the content/formatting layer. | **Story 2-5** |
| G5 | **Destructive-action confirmation dialog + form abandonment protection** | Belongs in the form-protection story; the wrapper is about *post-submit* visual feedback. | **Story 2-5** |
| G6 | **Visual-regression baselines** (per-theme × per-density × per-motion-preference screenshots) | A11y + visual-regression CI infrastructure lands in Story 10-2. Adding one wrapper's visual regression independently would fragment the gate setup. | **Story 10-2** (Accessibility CI gates & visual specimen verification) |
| G7 | **SignalR fault-injection test harness** (simulate mid-Syncing disconnection) — would let us test that the 10 s action prompt is the correct behaviour under *real* disconnect (not just stalled-Syncing). | Requires a test harness that doesn't exist yet; Story 5-7 stands it up. | **Story 5-7** (SignalR fault-injection test harness) |
| G8 | **Smarter `>10s` recovery** (retry button, auto-reconnect attempt, contact-support link) | ADR-022 locks v0.1 to page-reload; real reconciliation needs command-idempotency (Story 5-5) + connection recovery (Story 5-3). | **Story 5-3** / **Story 5-5** |
| G9 | **Threshold hot-reload guarantees** (`IOptionsMonitor` change-token propagation to running wrappers in mid-submit) — v0.1 reads `CurrentValue` on each tick, so config reload during a live submission changes thresholds on the fly. This is almost certainly OK but isn't tested. | L07 cost-benefit: test of a niche hot-reload-during-submit edge case doesn't pay for itself. Surface the behaviour in Dev Notes; re-test if production surfaces a bug. | **Story 9-3** (IDE parity and developer experience — covers config hot-reload UX generally) |
| G10 | **Specimen / developer-mode visual showcase** (`FcLifecycleWrapperSpecimen.razor`) that renders all 5 states with fake transitions | Dev-tooling concern, Epic 9 scope. Useful for Story 10-2 visual regression setup. | **Story 9-3** or **Story 6-5** (FcDevModeOverlay and starter template generator) |
| G11 | **ADR-022 page-reload recovery loses in-flight form input; G5 form-abandonment protection lives in Story 2-5** (Sally review 2026-04-16). There is a release window between 2-4 ship (page-reload available) and 2-5 ship (form-abandonment warning) during which users clicking "Start over" will silently lose typed data. The browser back button still has the input, but relying on that is a UX antipattern. **Mitigation options for release sequencing:** (a) sequence 2-5 to ship in the same release as 2-4 (preferred — Sally's recommendation), (b) add a one-line interim warning to the "Start over" button tooltip for v0.1 ("Your typed data will be lost"), (c) ship 2-4 with the current behaviour and accept the window. Jerome to decide at PR time; spec honors (a) by flagging the sequencing coupling explicitly. **Also:** Story 5-5 command idempotency is a downstream prerequisite for any "silent retry" alternative to ADR-022 — until 5-5 lands, reload is the only safe recovery. | Cross-story UX coupling — not a bug in 2-4 per se, but a surface the release manager must track. | **Story 2-5** (form-abandonment warning on navigation) + **Story 5-5** (idempotency unlock for silent-retry alternatives) |
| G12 | **Consumer-domain Shell ProjectReference is architectural debt** — D20 accepts adding `Hexalith.FrontComposer.Shell` as a required reference from consumer domain projects, which extends but doesn't break the existing "domain references FluentUI + Fluxor directly" pattern. The cleaner solution — a lean `Hexalith.FrontComposer.Components` package sibling to Shell, containing only the emitted-into-domain components (`FcLifecycleWrapper`, future `FcFieldPlaceholder` if/when its emission lands, future `FcDevModeOverlay`) — would mean consumers reference Components, not Shell. | Scope containment; splitting the package now adds a new csproj + moves files + updates all references, and the immediate D20 ProjectReference extension is a one-line csproj edit per consumer. Mid-term split is a mechanical refactor once the Components set stabilizes. | **Story 9-x** (likely 9-2 CLI migration or a new 9-4 package split story) |
| G13 | **Multi-circuit server-wide CPU cost risk** (advanced-elicitation Pre-mortem PM-A 2026-04-16). ADR-020's "200 Hz per page dismissed" assumes *single-circuit* measurement (validated by Task 6.4's 20-wrapper load test). Under production load — e.g., 500 concurrent Blazor Server users × 8 avg wrappers each × 10 Hz × `InvokeAsync(StateHasChanged)` producing SignalR diff frames — aggregate server CPU/bandwidth cost could be substantial. Task 6.4 does NOT catch this because it tests one circuit with 20 wrappers, not 500 circuits with 20 wrappers each. **Mitigation roadmap:** Story 5-x should promote the shell-scoped tick multiplexer from "nice-to-have future work" (ADR-020 escape hatch) to **P0 scalability gate**. Add a dedicated multi-circuit load test to Story 10-2 or a new 5-8 "server-side lifecycle scalability" story. | Pre-mortem specifically asked what we'd regret in 6 months; this scenario reads as the most likely operational failure. Not a 2-4 blocker — v0.1 adopters are unlikely to have 500 concurrent users immediately — but the roadmap owner must not lose track. | **Story 5-x** (new scalability story OR 10-2 CI gates OR a new 5-8) |
| G14 | **`LastTransitionAt` client-server clock-skew risk** (advanced-elicitation Chaos Monkey CM-2 2026-04-16). The wrapper computes `UtcNow - LastTransitionAt` using the client's clock (browser-side `TimeProvider.GetUtcNow()`), while `LastTransitionAt` is set from the server's clock in Story 2-3's `Transition(...)`. Under Story 5-4 durable replay, a reconnected circuit delivers a `LastTransitionAt` from original server time — if the client clock is skewed (e.g., Windows laptop with drifted clock or deliberately-wrong timezone override), elapsed time could appear negative or instantly-elapsed. `ActionPrompt` could render immediately after reconnect with no visible reason. Not a v0.1 bug (5-4 is the reconnect story), but a contract concern the 5-4 author must honour: either anchor on server-delivered "estimated elapsed ms" instead of raw `LastTransitionAt`, or document adopter guidance for tolerated clock skew (typically ≤ 5 s for NTP-synchronized clients). | Real-world clock-skew scenarios exist (Windows laptops with stale time service, vpn tunnels across regions). 5-4 is the right place to add the fix; 2-4 flags the expectation. | **Story 5-4** (Reconnection reconciliation and batched updates) |

---

## Dev Notes

### Service Binding Reference

No new DI registrations in `AddHexalithFrontComposer` beyond what Story 2-3 already landed **except** the options validator from Task 3.2:

```csharp
// Story 2-4 Task 3.2 — ordered-threshold validator for FcShellOptions.
services.AddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();

// Story 2-4 Task 3.4 — fail-fast on startup if shell options misconfigured.
services
    .AddOptions<FcShellOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`FcLifecycleWrapper` injects `ILifecycleStateService` (Story 2-3 D12 Scoped), `IOptionsMonitor<FcShellOptions>` (built-in), `NavigationManager` (built-in), `ILogger<FcLifecycleWrapper>` (built-in), `TimeProvider` (built-in on .NET 10). No wrapper-specific registrations — the wrapper is a Razor component + scoped CSS + standalone timer class, all resolved through normal Blazor DI.

### `FcShellOptions` Growth Risk (demoted from former ADR-023 during advanced-elicitation 2026-04-16)

Story 2-2 added three lifecycle-adjacent properties to `FcShellOptions` (`FullPageFormMaxWidth`, `DataGridNavCap`, `EmbeddedBreadcrumb`, `LastUsedDisabled`). Story 2-4 D12 adds four MORE threshold properties (`SyncPulseThresholdMs`, `StillSyncingThresholdMs`, `TimeoutActionThresholdMs`, `ConfirmedToastDurationMs`). The options class now mixes three concerns: form layout, data grid navigation, lifecycle timing.

**v0.1 decision: keep all eight properties on `FcShellOptions`**, accepting the concern mixing. Named trigger for splitting into `FcFormOptions` + `FcLifecycleOptions` (+ potentially `FcGridOptions`): when `FcShellOptions` reaches **≥12 properties** OR when a second cross-concern appears (e.g., an MCP-related option). The split lands in **Story 9-2** alongside the CLI inspection/migration tooling that rewrites adopter `appsettings.json` sections (`"Hexalith:Shell"` → `"Hexalith:Shell:Form"`/`"Hexalith:Shell:Lifecycle"`).

Was originally drafted as ADR-023 during party-mode review; demoted to this Dev Note during advanced-elicitation Occam pass because the content documents a *future refactor* and binds no v0.1 behavior — the ADR overhead (Status / Context / Decision / Consequences / Rejected alternatives sections) wasn't pulling its weight.

### Runtime-Log CorrelationId Sanitization (advanced-elicitation Red Team RT-4 + Pre-mortem PM-E 2026-04-16)

HFC2100/2101/2102 runtime-log messages MUST NOT emit raw CorrelationIds — hash each to its first 8 characters plus ellipsis (e.g., `"a1b2c3d4…"`). Rationale: CorrelationIds leak into third-party log aggregators (Datadog, Splunk, New Relic) when adopters use default `ILogger` sinks. In multi-tenant deployments this enables cross-tenant traffic-pattern correlation ("Tenant A users colliding on order approvals"). Hashing to first 8 chars preserves enough for debug-time matching within a single log context while breaking cross-context correlation.

Implementation pattern:
```csharp
private static string HashForLog(string correlationId)
    => correlationId.Length <= 8 ? correlationId : $"{correlationId[..8]}…";

_logger.LogWarning("HFC2100 …for correlationId={CorrelationIdFirst8}", HashForLog(transition.CorrelationId));
```

Unit-test assertion in `FcLifecycleWrapperTests`: log output for HFC2100/2101/2102 does NOT contain the full CorrelationId string.

### `ILifecycleStateService` is Internal-Surface-Only (advanced-elicitation Red Team RT-3 2026-04-16)

`ILifecycleStateService.Transition(...)` is the lifecycle state bus's ONLY write surface. Story 2-3 D19 guarantees the bridge is the single legitimate writer, but that's a convention, not enforcement. **Adopters MUST NOT expose `ILifecycleStateService` to JavaScript interop in production builds** — doing so would let a malicious page script forge `Confirmed` transitions for arbitrary CorrelationIds, causing every wrapper subscribed to that id to celebrate a submission that never happened.

If an adopter needs JS-visible lifecycle observation (e.g., a dev-mode overlay), expose a **read-only projection** (e.g., `IReadOnlyLifecycleObserver` with only `GetState`/`Subscribe` — no `Transition`), NOT the full service interface. This is a documentation-level warning in v0.1; analyzer enforcement can land in Story 9-1 build-time drift detection if adopters need the safety net.

### Fluent UI v5 Naming Note (D8)

UX specification wording frequently uses **"FluentProgressRing"**. In Fluent UI Blazor **v5.0.0-rc.2-26098.1** (the pinned version in `Directory.Packages.props`), `FluentProgressRing` is **deprecated/renamed to `FluentSpinner`** (confirmed via `mcp__fluent-ui-blazor__search_components` — see Task 0.2). **Use `FluentSpinner` in all new code**; it's also what `CommandFormEmitter` already emits at L390. Do not re-introduce `FluentProgressRing`.

Per UX spec §472-474, lifecycle feedback uses:
- `FluentSpinner` (in the submit button during Submitting — existing emitter)
- `FluentBadge` (Accent appearance, "Still syncing…" text at 2-10 s)
- `FluentMessageBar` (Success / Danger / Warning for Confirmed / Rejected / ActionPrompt)

`IToastService` is **removed** in Fluent UI v5 — UX spec §492 explicitly confirms this. Do NOT inject or reference `IToastService`. Use `FluentMessageBar` inline per spec §1145.

### Files Touched Summary

**Shell/Components/Lifecycle/** (new):
- `FcLifecycleWrapper.razor`
- `FcLifecycleWrapper.razor.cs`
- `FcLifecycleWrapper.razor.css`
- `LifecycleUiState.cs`
- `LifecycleThresholdTimer.cs`

**Shell/Options/** (new folder if absent):
- `FcShellOptionsThresholdValidator.cs`

**Shell/Extensions/** (modified):
- `ServiceCollectionExtensions.cs` — register validator (Task 3.2), ensure `AddOptions<FcShellOptions>().ValidateDataAnnotations().ValidateOnStart()` present (Task 3.4)

**Shell/_Imports.razor** (modified):
- Add `@using Hexalith.FrontComposer.Shell.Components.Lifecycle` (Task 4.2)

**Contracts/** (modified):
- `FcShellOptions.cs` — 4 new threshold properties with `[Range]` (Task 3.1)
- *(optional)* `Diagnostics/FcDiagnosticIds.cs` — extend with HFC2100-2102 (Task 3.3)

**SourceTools/Emitters/** (modified):
- `CommandFormEmitter.cs` — wrap emitted `<EditForm>` in `<FcLifecycleWrapper>` (Task 4.1)

**samples/Counter/Counter.Web/** (modified):
- `Program.cs` — bind `FcShellOptions` from `Hexalith:Shell` config section (Task 6.2)
- `appsettings.Development.json` — override thresholds (Task 6.2)

**tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/** (new):
- `LifecycleUiStateTests.cs` (10 unit tests on pure `From(...)` function — Task 5.1a; includes D22 XSS encoding test `RejectionMessage_with_script_tag_renders_HTML_encoded`)
- `FcLifecycleWrapperTests.cs` (7 bUnit behavioural tests — Task 5.1b, includes R-Reentrancy + auto-dismiss-timer-dispose)
- `FcLifecycleWrapperThresholdTests.cs` (7 FakeTimeProvider threshold tests — Task 5.2, includes R-Options-Hot-Reload)
- `LifecycleThresholdTimerPropertyTests.cs` (3 FsCheck property tests — Task 5.2b)
- `FcLifecycleWrapperA11yTests.cs` (6 accessibility tests — Task 5.3, includes R-Circuit-Reconnect dispose-during-inflight + focus-preservation-on-message-bar-insert)
- `LifecycleThresholdTimerTests.cs` (4 timer-unit tests — Task 5.4)

**tests/Hexalith.FrontComposer.Shell.Tests/Options/** (new folder):
- `FcShellOptionsValidationTests.cs` (3 tests — Task 5.5)

**tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/** (new folder):
- `CounterCommandLatencyE2ETests.cs` (2 tests — Task 6.1, revised to n=300 cold + 10-click warm-up discard per Murat review)
- `WrapperConcurrencyLoadTests.cs` (1 test — Task 6.4 — 20-wrapper concurrency load test per Winston review)

**tests/Hexalith.FrontComposer.Shell.Tests/Generated/** (modified):
- `CommandRendererInlineTests.cs` — add `fc-lifecycle-wrapper` presence assertion
- `CommandRendererCompactInlineTests.cs` — same
- `CommandRendererFullPageTests.cs` — same

**tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/Snapshots/** (re-approved):
- `CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- (any other `CommandFormEmitterTests.*.verified.txt` snapshots exercising the EditForm emission — inspect and re-approve all that change)

### Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Wrapper component | `FcLifecycleWrapper` | — |
| Scoped CSS file | `FcLifecycleWrapper.razor.css` | generated as `_content/Hexalith.FrontComposer.Shell/FcLifecycleWrapper.razor.css` |
| Sync-pulse CSS class | `.fc-lifecycle-pulse` | — |
| CSS keyframes | `@keyframes fc-lifecycle-pulse` | — |
| Timer class | `LifecycleThresholdTimer` | — |
| UI state record | `LifecycleUiState` | — |
| Timer phase enum | `LifecycleTimerPhase { NoPulse, Pulse, StillSyncing, ActionPrompt, Terminal }` | — |
| Options validator | `FcShellOptionsThresholdValidator` | — |
| Runtime diagnostic codes | `HFC2100`, `HFC2101`, `HFC2102` | — |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 (inherited from 2-1/2-2/2-3)
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — verify it's already referenced in `Hexalith.FrontComposer.Shell.Tests.csproj` per `dotnet list package`; add if missing (Task 0.1 catches this)
- Microsoft.Playwright for Task 6.1 E2E — verify reference, install browsers on CI agent via `playwright install chromium` before the E2E job (reuse any existing Playwright wiring from `Hexalith.EventStore`'s Playwright gate if present)
- `TestContext.Current.CancellationToken` on async test helpers (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- **Test count budget (L07 applied):** **~47 new tests** (revised up from 33 per Murat review, further +2 per advanced-elicitation Pre-mortem PM-D/PM-F — 10 unit + 7 bUnit + 7 threshold + 3 FsCheck properties + 4 timer-unit + 6 a11y + 3 validation + 3 renderer-assertion + 2 E2E latency + 1 E2E concurrency + 1 Counter sample manual-validation sanity check). Cumulative target **~506**. L07 cost-benefit applied: 8 bUnit tests re-leveled to pure unit tests (net +2 tests, 10× speed per test, broader combinatorial coverage). 3 new FsCheck properties reuse 2-3's infrastructure (1000 CI / 10000 nightly). R-Reentrancy + R-Circuit-Reconnect + R-Options-Hot-Reload coverage closed; Pre-mortem PM-D (auto-dismiss-timer dispose) + PM-F (focus-preservation) + Red Team RT-1 (XSS encoding) coverage added.

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- No `AnalyzerReleases.Unshipped.md` update — HFC2100-2102 are runtime-logged only (architecture.md §648 precedent honored by 2-3 HFC2004-2007)
- Roslyn 4.12.0 pinned (inherited)
- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story
- E2E test `[Trait("Category", "E2E")]` — new CI job `command-latency-gate` filters `--filter Category=E2E` and runs the Counter Aspire topology via `dotnet run --project samples/Counter/Counter.AppHost &` before running the Playwright tests

### Previous Story Intelligence

**From Story 2-3 (immediate predecessor — review status):**

- **Binding consumer contract (D19):** `FcLifecycleWrapper` MUST consume `ILifecycleStateService.Subscribe` and NEVER read `{Command}LifecycleFeature` state directly. This is the single hardest gotcha — easy to slip into `@inject IState<IncrementCommandLifecycleFeatureState>` because that's the obvious Blazor-Fluxor idiom. The story above pins it in the cheat sheet, Decision D2, AC1, and AC9 all converge on the same invariant.
- **Monotonic anchor (D15):** Use `CommandLifecycleTransition.LastTransitionAt`, not `DateTime.UtcNow`. Story 2-3 already pays the implementation cost; skipping it here would regress Sally's Story C reconnect-staleness fix.
- **IdempotencyResolved flag (D10):** Logs HFC2101 in v0.1, full UX copy lands in Story 2-5. Do not drop the flag or the log — Story 2-5 will grep history for HFC2101 production hits to size the localization work.
- **Story 2-3 used `FakeTimeProvider` in `Microsoft.Extensions.TimeProvider.Testing`** for property-based state machine tests; reuse the same package for threshold timer tests here. 2-3 cut timed eviction (ADR-019) so the package may have been pulled from the Shell.Tests csproj — check Task 0.1.
- **ServiceCollectionExtensions precedent (Story 2-3):** AddHexalithDomain<T> now scans for types ending in `LifecycleBridge` AND `LastUsedSubscriber`. No change needed here — `FcLifecycleWrapper` is a Razor component, not a discovered domain type.
- **Options validator pattern precedent:** Story 2-3 did NOT add a validator; Story 2-4 is the first shell-options validator. Use `IValidateOptions<T>` rather than `ValidateDataAnnotations()` for cross-property validation. Register once.

**From Story 2-2:**

- `CommandFormEmitter` L380-396 already handles the in-button spinner + disabled-while-submitting behaviour — D7 says DO NOT alter this. Story 2-1 Decision D13 owns the in-button visual; don't overreach.
- Snapshot re-approval pattern: re-approve only the files that changed, inspect diffs first to confirm only the intended wrap addition (not accidental trailing whitespace, not unrelated format changes).

**From Story 2-1:**

- `_submittedCorrelationId` is the form's CorrelationId field name in emitted Razor (introduced in the emitter for Fluxor action CorrelationId plumbing). Verify the field name at Task 4.1 before wrapping; if it's renamed in a later patch, use the current name.

### Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity:** The cheat sheet's "Binding contract with Story 2-3" and "Binding contract with Stories 2-1 / 2-2" sections + ADR-020/021/022/023 lock the cross-story seams explicitly. Reference: Story 2-3 D19 cited 4× in this spec so the invariant can't be lost.
- **L04 Generated name collision detection:** No new emitted artifact added (D1), so no collision risk. The emitter wrap uses the existing `<FcLifecycleWrapper>` naked component name — collides with no other generated file name. Scoped CSS file (`FcLifecycleWrapper.razor.css`) collides with no existing file in `Shell/Components/`.
- **L05 Hand-written service + emitted per-type wiring:** The wrapper is hand-written; wiring into generated forms is done by modifying `CommandFormEmitter` (not emitting per-command subscribers). Matches the Story 2-3 bridge-pattern split.
- **L06 Defense-in-depth budget:** 23 Decisions after party-mode review + advanced-elicitation (D20 + D21 + D22 + D23 added) — under the ≤25 feature-story cap. Occam trim applied (ADR-023 options-consolidation demoted to Dev Note). No further trim needed.
- **L07 Test cost-benefit:** 47 new tests / 23 decisions = ~2.0/decision, tighter than 2-3's 2.3/decision and 2-2's 3.1/decision. All threshold-boundary tests (5.2) use `FakeTimeProvider` for determinism — avoids the Story 2-2 TestGuidFactory / TestUlidFactory proliferation. 8 bUnit tests re-leveled to pure unit tests per Murat — cheaper + faster + broader combinatorial coverage (net +2 tests for substantially higher signal). Advanced elicitation added 2 tests covering specific production-regret failure modes (auto-dismiss-timer dispose, focus-preservation), at high value per test.
- **L09 ADR rejected-alternatives discipline:** ADR-020 cites 4, ADR-021 cites 2, ADR-022 cites 3, ADR-023 cites 2 (the former ADR-024 renumbered after advanced-elicitation demoted former-ADR-023 options-consolidation to a Dev Note). Minimum 2 per ADR satisfied on all four. ADR-020's 4 surfaced the "wrap at renderer chrome" alternative that nearly won; ADR-021 lead-rationale rewritten to reconnect-anchor-recomputation per Winston review; ADR-023 pins hot-reload semantics per Winston review + rate-limit per Chaos Monkey CM-3.
- **L10 Deferrals name a story:** All 10 Known Gaps cite a specific owning story (no "Epic N" or "future" vagueness).
- **L11 Dev Agent Cheat Sheet:** Present despite the story being under 30 decisions — feature story with cross-story bindings benefits from the fast-path entry.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.4 — AC source of truth, §970-1017]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR2 — FcLifecycleWrapper requirements, §315]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR48 — sync pulse frequency rule, §361]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR49 — sync pulse + focus ring coexistence, §362]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR1-2 — P95/P50 command latency SLOs, §1344-1345]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR11-14 — progressive visibility thresholds]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR88 — zero "did it work?" hesitations]
- [Source: _bmad-output/planning-artifacts/prd.md#FR23 — five-state lifecycle]
- [Source: _bmad-output/planning-artifacts/prd.md#FR30 — exactly-one user-visible outcome]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#FcLifecycleWrapper — component anatomy, §1803-1842]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Threshold configuration — SyncPulseThresholdMs deployment override, §1835]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Accessibility — aria-live politeness + reduced-motion contract, §1838-1842]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#ILifecycleStateService publisher/subscriber architecture, §1698-1723]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Brand-signal fusion frequency rule, §750]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Zero-override commitment for custom components, §1690]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Tier 1 testing strategy for FcLifecycleWrapper, §2162-2166]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Fluent UI v5 IToastService removal, §492]
- [Source: _bmad-output/planning-artifacts/architecture.md#397 — D2 Fluxor lifecycle + wrapper (v0.1 → v1)]
- [Source: _bmad-output/planning-artifacts/architecture.md#536 — CommandLifecycleState ephemeral; not persisted]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges; 2xxx runtime-logged]
- [Source: _bmad-output/planning-artifacts/architecture.md#920-935 — Shell/Components/Lifecycle folder structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts must not reference other packages (dependency-free — FcShellOptions extension stays in Contracts)]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Decision D19 — single-writer invariant + binding consumer contract for Story 2-4]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Decision D15 — CommandLifecycleTransition.LastTransitionAt monotonic anchor]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Decision D10 — IdempotencyResolved detection-only, no terminal synthesis]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Decision D11 — ConnectionState deferred to Story 5-3]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#ADR-017 — service as cross-command correlation index]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#ADR-018 — bespoke callback subscription contract]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#ADR-016 — renderer/form split]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#L380-396 — in-button FluentSpinner during Submitting]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — running list of known deferrals]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01 — cross-story contract clarity]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L05 — hand-written service + emitted wiring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L06 — ≤25 decisions budget for feature story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L07 — test count cost-benefit]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L09 — ADR rejected-alternatives discipline]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L10 — deferrals name a story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L11 — cheat sheet for cross-story-binding stories]
- [Source: memory/feedback_no_manual_validation.md — automated E2E preference; Task 6 honors with Playwright + Aspire]
- [Source: memory/feedback_cross_story_contracts.md — explicit cross-story contracts per ADR-016 canonical example; ADR-020/021/022 here mirror the pattern]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D13 inherits Story 2-3 D13 rationale (ephemeral, no persisted data)]
- [Source: memory/feedback_defense_budget.md — 19 decisions, under the ≤25 feature-story cap]

### Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §920-935):
  - New `Shell/Components/Lifecycle/` folder — architecture.md §929-931 designates this folder for `FrontComposerLifecycleWrapper.razor` + `.razor.cs`. Our filename is `FcLifecycleWrapper` (per Fc-prefix convention UX spec §1692 vs. the architecture's "FrontComposer*" prefix), resolving the naming conflict as follows: the UX spec's `Fc` prefix is canonical for custom components per §1692 "Naming convention: All custom components use the `Fc` prefix"; the architecture doc's `FrontComposer*` names in the folder tree are pre-UX-spec-finalization and represent an earlier draft. **Decision to use `Fc` prefix is bound here** — pull the rest of the architecture blueprint filenames into the `Fc` prefix pattern as those components land in Epic 3+ stories; do NOT rename existing `FrontComposer*` service classes (that's Story 9-x).
  - New `Shell/Components/Lifecycle/` is a new subfolder under `Shell/Components/`, consistent with existing `Shell/Components/Rendering/` (which holds `FcFieldPlaceholder.razor` — v0.1 precedent for `Fc` prefix).
  - `FcShellOptions` extension stays in Contracts — preserves the dependency-free invariant (architecture.md §1144). The four new threshold `int` properties are pure POCO; no new namespace pulled in.
  - New `Shell/Options/` folder for `FcShellOptionsThresholdValidator.cs` — consistent with the existing `Shell/Services/` and `Shell/Infrastructure/` folder-per-concern organization.
  - No new test project — existing `Hexalith.FrontComposer.Shell.Tests` absorbs all new tests.

- **Detected conflicts or variances:**
  - **`FrontComposerLifecycleWrapper` vs. `FcLifecycleWrapper` naming** — as above, UX spec `Fc` prefix wins; architecture blueprint §929-930 references `FrontComposerLifecycleWrapper` and is stale. Treat blueprint names as advisory, UX spec names as canonical.
  - **`FcShellOptions` now holds both form-shell options (FullPageFormMaxWidth, DataGridNavCap) and lifecycle options (SyncPulseThresholdMs et al.)** — this is intentional per D12. Future split into `FcFormOptions` + `FcLifecycleOptions` is a refactor for Story 9-2 if `FcShellOptions` grows beyond ~10 properties; not in scope for 2-4.
  - **Playwright package not yet referenced in Shell.Tests csproj** — Task 0.2 + Task 6.1 detect and add if absent. Not a blocker, just a one-line csproj edit.

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

*(Dev agent fills in during implementation. Template:)*

- `<YYYY-MM-DD>` — initial test baseline before story: <N>/<N> green
- `<YYYY-MM-DD>` — after Task 2: <elapsed>, notable issues
- `<YYYY-MM-DD>` — emitter snapshot re-approval: <count> files
- `<YYYY-MM-DD>` — final full-suite run: <M>/<M> green (<delta> new)
- `<YYYY-MM-DD>` — `dotnet build -c Release -p:TreatWarningsAsErrors=true`: <result>
- `<YYYY-MM-DD>` — Playwright E2E latency gate: P95 cold <ms>, P50 warm <ms>

### Completion Notes List

*(Dev agent fills in at story completion. Template:)*

- AC1: ...
- AC2: ...
- AC3: ...
- AC4: ...
- AC5: ...
- AC6: ...
- AC7: ...
- AC8: ...
- AC9: ...
- Test tally: +<N> new tests = cumulative <M> across the test projects.
- Zero-warning build: ...
- Known deviations from story spec: ...

### File List

*(Dev agent fills in at story completion. Groups: Created / Modified.)*

### Change Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-04-16 | Story created via `/bmad-create-story 2-4` | Epic 2 continuation; Story 2-3 moves to review, 2-4 becomes ready-for-dev |
| 2026-04-16 | Party-mode review applied (Sally + Winston + Amelia + Murat, parallel subagents). Changes: D1 parameter-stability clause; D5 clarifies `ITimer` via `TimeProvider.CreateTimer` over `PeriodicTimer` (FakeTimeProvider determinism); added **D20** (consumer-domain Shell ProjectReference) resolving Amelia Blocker 1; added **D21** (emitter emits `using` directive) resolving Amelia Blocker 2 + supersedes original Task 4.2 `_Imports.razor` edit; added **ADR-023** (FcShellOptions consolidation / split deferred to 9-2); added **ADR-024** (IOptionsMonitor hot-reload next-tick-take-effect semantics); tightened ADR-021 lead argument (reconnect-anchor-recomputation over cancellation); tightened ADR-022 copy ("Start over" over "Refresh page"); AC1 rapid-resubmit sub-bullet; AC3 drops polite announcement during Pulse (pulse visual-only per Sally); AC5 title active-voice + "Start over" button; Task 0.3/0.4 mandatory package adds (`FakeTimeProvider` + `Playwright`); Task 3.2 validator registration order explicit; Task 4.1 split into 4.1a emit-using + 4.1b wrap-EditForm; Task 4.2 repurposed to consumer-domain Shell ProjectReference + adopter docs; Task 4.3 snapshot scope widened (≥5 diffs); Task 5 re-leveled — 8 bUnit tests pushed to pure-unit `LifecycleUiStateTests` (+10 tests); added 3 FsCheck property tests (Task 5.2b); added R-Reentrancy bUnit test (5.1b.6); added R-Circuit-Reconnect dispose test (5.3.5); added R-Options-Hot-Reload bUnit test (5.2.7); tightened 5.3.4 scoped-CSS assertion for Blazor attribute-selector rewrite; Task 6.1 bumped to n=300 cold + 10-click warm-up discard; added Task 6.4 20-wrapper concurrency load test; added **G11** (ADR-022 form-state-loss window cross-ref to Story 2-5/5-5 sequencing); added **G12** (consumer Shell ProjectReference as architectural debt — clean split deferred to 9-x); G3 release-note hint; test total 33→45; decision count 19→21; cumulative test target 492→504. | Review feedback from four independent subagents converged on: (a) architectural blocker in emitter-to-consumer-domain layering (Amelia, must-fix), (b) Sally's a11y-chatter concern on AC3 polite announcement, (c) Murat's statistical-invalidity of n=50 for P95 + missing FsCheck properties + bUnit→unit re-level, (d) Winston's append-only-parameter contract + options-consolidation ADR + hot-reload semantics ADR. All concerns either applied, deferred to a named story with G-entry, or surfaced as explicit sequencing decisions for release-manager attention. |
| 2026-04-16 | Advanced-elicitation pass applied (Pre-mortem + Red Team vs Blue Team + Occam's Razor + Chaos Monkey + Hindsight Reflection — 5 methods sequenced per `/bmad-advanced-elicitation`). Changes: added **D22** (CRITICAL XSS — `RejectionMessage` renders plain text via `@RejectionMessage`, never `MarkupString`) resolving Red Team RT-1; added **D23** (`LifecycleThresholdTimer` accepts optional `Func<bool>? isDisconnected` — Story 5-3 seam, v0.1 null-default) resolving Hindsight H-1; demoted former ADR-023 (`FcShellOptions` consolidation) to a Dev Note (Occam OC-1 — documents a future refactor, binds no v0.1 behavior, ADR overhead not earning its keep); renumbered former ADR-024 (hot-reload) → ADR-023; added rate-limit clause to ADR-023 (`OnChange` runtime-log max 1/minute/circuit) per Chaos Monkey CM-3; D1 addendum for URL-accepting-parameter allowlist per Red Team RT-2; Task 2.4 rewritten — subscribe to `IOptionsMonitor.OnChange` and cache thresholds in fields via `Interlocked.Exchange`, avoid per-tick `CurrentValue` allocation per Pre-mortem PM-C; added Task 2.7 (10-min `FluentMessageBar.Timeout` TimeProvider investigation) per Occam OC-2; added Task 5.1b.7 (auto-dismiss-timer Dispose race) per Pre-mortem PM-D; added Task 5.3.6 (focus-preservation on MessageBar insert) per Pre-mortem PM-F; Task 6.3 marked optional sanity pass per Occam OC-3; extended G2 with `IdempotencyResolved` semantics-reconciliation warning for 2-5/5-4 teams per Hindsight H-2; added **G13** (multi-circuit server-wide CPU risk — shell-scoped tick multiplexer promotion to Story 5-x P0) per Pre-mortem PM-A; added **G14** (`LastTransitionAt` client-server clock-skew concern for Story 5-4 contract) per Chaos Monkey CM-2; added 3 Dev Notes sections (demoted options-growth-risk; runtime-log CorrelationId sanitization; `ILifecycleStateService` internal-surface-only JS-interop warning); test total 45→47; decision count 21→23; ADR count 5→4; cumulative test target 504→506. | Per L08 discipline (party-mode catches architecture/coupling; advanced-elicitation catches security/edge-cases/robustness). Red Team surfaced the single genuine CRITICAL — the `MarkupString` XSS trap — which would have shipped un-caught by any prior review round. Pre-mortem + Hindsight surfaced production-regret scenarios (server-wide CPU, focus stealing, auto-dismiss dispose race) that the 33-test budget didn't cover. Occam trim demoted an over-spec'd ADR to a right-sized Dev Note, keeping the budget within L06 cap. All 15 synthesis items applied; none deferred. |
