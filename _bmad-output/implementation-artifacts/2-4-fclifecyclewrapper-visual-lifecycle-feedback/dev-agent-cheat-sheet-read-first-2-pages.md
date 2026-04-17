# Dev Agent Cheat Sheet (Read First — 2 pages)

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
