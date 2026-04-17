# Acceptance Criteria

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
