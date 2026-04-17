# Architecture Decision Records

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
