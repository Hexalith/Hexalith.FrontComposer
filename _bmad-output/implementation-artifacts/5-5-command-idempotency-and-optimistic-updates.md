# Story 5.5: Command Idempotency & Optimistic Updates

Status: review

> **Epic 5** -- Reliable Real-Time Experience. **FR28-FR29 / FR31** pending command outcome reconciliation, idempotent terminal handling, optimistic status badges, new-item visibility, and polling parity after Stories 5-1 through 5-4. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, **L12**, and **L14**.

---

## Executive Summary

Story 5-5 closes the command side of the degraded-network experience without replacing the transport, cache, or reconnect seams already shipped:

- Reuse Story 5-1's EventStore command/query contracts, ULID `MessageId`, `ICommandService`, `ICommandServiceWithLifecycle`, `ILifecycleStateService`, tenant/user fail-closed validation, and nudge-only SignalR contract.
- Reuse Story 5-2's response classifier, `CommandRejectedException`, command validation/warning/auth taxonomy, `IETagCache`, `IProjectionPageLoader`, `IActionQueueCountReader`, and `304` no-churn behavior. Do not add a second HTTP classifier or cache.
- Reuse Story 5-3's projection connection-state service and bounded visible-lane fallback scheduler. 5-5 upgrades fallback from "visible refresh while disconnected" to user-equivalent command outcome recovery, but it still runs through REST queries with ETag and bounded lane limits.
- Reuse Story 5-4's reconnect reconciliation and changed-lane sweep boundary. 5-5 does not duplicate the `Reconnected -- data refreshed` pass; it adds pending-command outcome resolution and optimistic/new-item UI on top of reconciled projection data.
- Keep idempotency anchored to ULID `MessageId`. Duplicate terminal outcomes for the same message must not produce duplicate success notifications, duplicate lifecycle transitions, replayed commands, or phantom row changes.
- Keep optimistic UI reversible and honest. `FcDesaturatedBadge` shows an expected status while the command is unresolved, then saturates on confirmed/idempotent-confirmed or reverts on rejected.
- Preserve command form input on degraded rejection. Reconnection can resolve a pending command as rejected, but it must not clear field values, resubmit forms, or hide the domain reason behind a generic network error.
- Add a clear pending-command summary after reconnect. Users should see which in-flight commands confirmed, rejected, or were already applied, without manually refreshing or verifying every row.

The intended implementation shape is a bounded Shell-scoped pending-command index keyed by framework-generated correlation ID and ULID `MessageId`. Generated command forms register pending command metadata after dispatch acceptance, projection/reconciliation/polling paths resolve matching outcomes, `ILifecycleStateService` receives exactly one terminal transition, and small Shell components render optimistic badges, new-item indicators, and a reconnection outcome summary.

---

## Story

As a business user,
I want commands that land during a disconnection to resolve correctly on reconnection, with optimistic badge updates that show me the expected state immediately,
so that I am never confused by duplicate outcomes, stale badges, or missing status changes after network recovery.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A command was accepted with a framework-generated ULID `MessageId` before or during EventStore projection-hub degradation | The command terminal outcome is observed through SignalR nudge refresh, reconnect reconciliation, fallback polling, or an EventStore idempotency/query response | The UI resolves the pending command to exactly one terminal user-visible outcome per `MessageId` (`Confirmed`, `Rejected`, or `IdempotentConfirmed`), the lifecycle receives at most one terminal transition, the pending entry is removed or marked terminal once, and no command is replayed automatically. |
| AC2 | A duplicate, stale, out-of-order, or repeated terminal outcome arrives for the same `MessageId` after live nudge, reconnect, or polling | The pending-command resolver processes it after a terminal outcome already won | The first terminal outcome wins; later observations are idempotent no-ops except for bounded already-applied summary metadata, `CommandLifecycleTransition.IdempotencyResolved` is preserved when applicable, and no second row insert, badge animation, toast, lifecycle terminal callback, or new-item indicator is emitted. |
| AC3 | A command predicts a status change for a visible row or badge lane | The command is acknowledged and enters `Syncing` or degraded pending state | `FcDesaturatedBadge` immediately renders a localizable visible state label such as `Confirming`, keeps the target status text visible, adds an equivalent localizable accessible label, uses `filter: saturate(0.5)`, remains understandable in grayscale and forced-colors mode, and stores the prior confirmed state as the rollback value. |
| AC4 | An optimistic status update confirms, rejects, or resolves idempotently | The terminal outcome is applied | Confirmed restores full saturation with a 200 ms CSS transition; Rejected reverts to the prior confirmed status, shows durable plain-text rejection context, and preserves form input; IdempotentConfirmed skips the revert animation and saturates directly; `prefers-reduced-motion: reduce` makes badge and indicator transitions instantaneous without relying on color flashes. |
| AC5 | A create command confirms a new entity that is visible in the current DataGrid lane but does not match the active filter/search/sort criteria | The projection page is reconciled or polled after the command outcome | `FcNewItemIndicator` renders the new row at the top of the grid lane with a full localizable string such as `New item. It may not match current filters yet.`, uses `aria-live="polite"` and `aria-describedby`, highlights subtly with Fluent info styling, remains meaningful without motion or color alone, and auto-dismisses after 10 seconds or immediately on the next filter/search/sort change. |
| AC6 | A domain-specific rejection is discovered during degraded network recovery | The rejection reaches the client after reconnect or fallback polling | The message format is `[What failed]: [Why]. [What happened to the data].`, renders as plain text in a Danger `FluentMessageBar`, does not auto-dismiss, preserves form model/EditContext values, and does not clear optimistic context until the rollback is visible. |
| AC7 | SignalR remains unavailable after the disconnected fallback threshold | The polling fallback activates | ETag-gated polling queries at configured bounded intervals feed the same pending-command outcome reducer used by SignalR nudge refresh and reconnect reconciliation; polling preserves terminal outcome parity for visible lanes and pending commands, processes bounded unresolved work fairly without starving older commands, keeps no-churn behavior on 304/429/503, and stops on reconnect, unsubscribe, disposal, tenant/user loss, or option disablement. |
| AC8 | A user returns from a disconnection with commands in flight | Reconnect reconciliation and pending-command resolution complete | A clear non-blocking summary covers only commands accepted during the degraded session, presents bounded counts first and accessible details second, lists each resolved command as confirmed, rejected, already applied, or unresolved, includes a clear next-action fallback for rejections/unresolved commands, requires no manual page refresh, and shows/logs no raw command payload, form value, tenant/user value, token, or unbounded server problem detail. |
| AC9 | Tests run | Contracts, Shell services, reducers/effects, generated command forms, badges, DataGrid indicators, and polling paths execute | Coverage proves exactly-once UI terminal resolution per `MessageId`, duplicate/out-of-order/idempotent handling, malformed or conflicting identifier rejection, ambiguous fallback-match unresolved behavior, live/reconnect/polling reducer parity, optimistic badge confirm/reject/idempotent paths, reduced-motion/forced-colors behavior, new-item indicator lifetime/filter dismissal/materialization cleanup, degraded rejection copy and form preservation, bounded polling parity, redacted diagnostics with sensitive-looking fixtures, deterministic fake-clock scheduling, and no raw draft persistence. |

---

## Tasks / Subtasks

- [x] T1. Add a bounded pending-command index (AC1, AC2, AC8, AC9)
  - [x] Read `ILifecycleStateService`, `LifecycleStateService`, `ICommandServiceWithLifecycle`, `CommandResult`, `CommandLifecycleTransition`, and Story 5-2 command taxonomy before editing.
  - [x] Create a Shell-scoped state/service under `Shell/State/PendingCommands/` or the adjacent lifecycle state folder that tracks pending commands by correlation ID and ULID `MessageId`.
  - [x] Store only framework metadata required for resolution: correlation ID, message ID, command type name, expected projection type/lane, optional aggregate/entity key, expected status slot, prior status slot, submit time, and bounded last outcome. Do not store raw command payloads or field values.
  - [x] Add an option-backed cap such as `MaxPendingCommandEntries` and FIFO/LRU eviction diagnostics per L14. Eviction must mark the command unresolved/degraded, not silently confirmed.
  - [x] Resolve terminal outcomes exactly once using atomic state transitions. Duplicate outcomes for a terminal entry must be no-ops except for optional idempotent summary metadata.
  - [x] Validate framework-generated `MessageId` values before registration and before accepting terminal observations. Empty, malformed, oversized, or caller-controlled identifiers must fail closed with redacted diagnostics.
  - [x] Define duplicate registration behavior explicitly: same `MessageId` merges metadata only when framework-controlled fields agree; conflicting metadata rejects the second registration and logs a redacted warning.
  - [x] Unknown terminal outcomes for untracked `MessageId` values must be ignored or summarized as external activity without mutating pending-command state.
  - [x] Dispose/clear per-circuit state on service disposal and tenant/user scope loss.

- [x] T2. Register pending commands from generated command forms without remounting forms (AC1, AC6, AC9)
  - [x] Extend generated command-form submit flow only after reading `CommandFormEmitter.cs`, Story 5-2 server-validation handling, and Story 5-3 form-preservation tests.
  - [x] Register the pending entry after `ICommandService.DispatchAsync` returns an accepted `CommandResult` with `MessageId`. If dispatch fails before acceptance, do not create a pending command.
  - [x] Capture correlation/message metadata without storing raw form models, user-entered fields, or validation messages in persistence.
  - [x] Keep existing `EditContext` and `ValidationMessageStore` instances mounted across connection-state changes and pending-outcome updates.
  - [x] Do not auto-retry, auto-submit, or replay the command on reconnect. Idempotency protects duplicated server observations, not client-side blind replay.

- [x] T3. Resolve pending outcomes from reconnect, nudge, and polling paths (AC1, AC2, AC7, AC8, AC9)
  - [x] Reuse Story 5-3 `ProjectionFallbackRefreshScheduler` and Story 5-4 reconciliation results as input signals. Do not add another visible-lane registry.
  - [x] Define a small resolver interface that accepts projection/query outcome metadata and matches it to pending commands by `MessageId`, correlation ID, or a framework-controlled entity/aggregate key when EventStore does not echo `MessageId` in projection rows.
  - [x] If the EventStore API exposes an idempotency/status query, call it through the existing `IQueryService`/`EventStoreQueryClient` and response classifier. If not, resolve only from projection state and record any unresolved ambiguity as a deferred follow-up rather than inventing a new provider contract.
  - [x] Route live nudge refresh, reconnect reconciliation, idempotency/status query results, and fallback polling through one reducer/service method so ordering, duplicate, and redaction rules cannot drift by delivery path.
  - [x] When matching by framework-controlled entity/aggregate key instead of `MessageId`, require an unambiguous single pending candidate for the current lane and expected transition. Multiple candidates must remain unresolved/degraded rather than guessing.
  - [x] Apply a first-terminal-wins rule for conflicting outcomes. If `Rejected` and `Confirmed` race for the same unresolved `MessageId`, the first reducer-accepted terminal state is authoritative and the later observation is recorded only as redacted diagnostic metadata.
  - [x] Treat lifecycle-transition dispatch failure as a redacted operational failure after the pending state is terminal. Do not reopen the command or start a retry loop that could duplicate the user-visible outcome.
  - [x] Confirmed outcomes transition `ILifecycleStateService` to `Confirmed`; rejected outcomes transition to `Rejected` with Story 5-2 rejection copy; idempotent confirmed outcomes preserve idempotency metadata so `FcLifecycleWrapper` can avoid a second celebration.
  - [x] Preserve current visible data on 304, 429, 503, auth failures, malformed responses, and schema mismatch according to Stories 5-2 through 5-4.
  - [x] Log bounded failure categories only. No raw tenant IDs, user IDs, group names, command/query payloads, tokens, or ProblemDetails bodies.

- [x] T4. Implement `FcDesaturatedBadge` and optimistic badge state (AC3, AC4, AC9)
  - [x] Reuse `FcStatusBadge` and `SlotAppearanceTable` rather than adding a second badge color system.
  - [x] Add a thin wrapper component, for example `Components/Badges/FcDesaturatedBadge.razor`, that accepts prior slot/label, optimistic slot/label, confirmation state, column header, and motion mode.
  - [x] Always render text. Color/desaturation must never be the only status signal.
  - [x] Model the visible optimistic states as localizable whole strings (`Confirming`, `Confirmed`, `Rejected`, `Already applied`, `Needs review` or equivalent). Do not concatenate localized visible text or aria suffix fragments.
  - [x] Add `filter: saturate(0.5)` while confirming and a 200 ms saturation transition on confirmed. Add `@media (prefers-reduced-motion: reduce)` to disable transition timing.
  - [x] Ensure rejected rollback restores the previous confirmed slot/label and does not leave stale `(confirming)` aria copy.
  - [x] Avoid row-wide styling here. Row/new-item highlighting belongs to T5.

- [x] T5. Add `FcNewItemIndicator` for confirmed created rows outside current filters (AC5, AC9)
  - [x] Read DataGrid generated view host and Story 4-3/4-4/4-5 DataGrid state before deciding where the row marker belongs.
  - [x] Add a bounded transient state slice keyed by view/lane and entity key/message ID. Use `TimeProvider` for the 10-second auto-dismiss; do not use wall-clock sleeps.
  - [x] Render the indicator row at the top of the current visible lane only when the confirmed entity is relevant to the lane but outside active filter/search/sort criteria.
  - [x] Dismiss on timer expiry, next filter/search/sort change, normal row materialization, lane disposal, tenant/user loss, or explicit row interaction if the implementation adds one.
  - [x] Use `aria-live="polite"` on the row and `aria-describedby` for the indicator text. The visible copy must be a full localizable string, not assembled punctuation fragments; suggested English text: `New item. It may not match current filters yet.`
  - [x] Keep highlight subtle and non-decorative. Use Fluent info background at low opacity and respect reduced motion for fade-out.

- [x] T6. Render degraded rejection and reconnect outcome summary (AC6, AC8, AC9)
  - [x] Reuse Story 5-2 `CommandRejectedException` and warning/feedback publisher seams. Do not branch on raw HTTP status in UI components.
  - [x] Add a small summary component under `Components/EventStore/` or `Components/Lifecycle/` that reads the bounded pending-command terminal outcomes for the current circuit.
  - [x] Limit the reconnect summary to commands accepted during the degraded session. It is not a global event history, audit log, or observability panel.
  - [x] Cap expanded summary details with an option-backed or component-local maximum. Overflow must show a redacted count such as additional resolved commands instead of rendering an unbounded list.
  - [x] Render a concise count summary first in a polite live region, then accessible details only when expanded or otherwise navigated to.
  - [x] Show confirmed, rejected, and already-applied rows with concise plain text. Do not show raw command JSON, payload values, stack traces, or raw ProblemDetails.
  - [x] Use Danger `FluentMessageBar` for degraded rejection with no auto-dismiss. Confirmed/already-applied summary should be Info/Success and non-blocking.
  - [x] Preserve form values and validation state after a rejection discovered during reconnect. The summary may reference the command display name, but must not clear or remount the form.

- [x] T7. Upgrade polling fallback to command-outcome parity (AC7, AC9)
  - [x] Extend the existing Story 5-3 fallback scheduler instead of creating a second polling loop.
  - [x] Add bounded pending-command polling only while SignalR is unavailable and only for accepted commands whose terminal outcome is unresolved.
  - [x] Use ETag-gated query paths and safe framework-controlled discriminators. If a pending command cannot be safely keyed, mark it degraded/unresolved rather than constructing a key from raw user input.
  - [x] Reuse the same resolver contract tests for live nudge, reconnect reconciliation, and polling. Polling parity means identical terminal user outcomes, not identical transport behavior.
  - [x] Process unresolved pending commands oldest-first within configured caps so a burst of new submissions cannot indefinitely postpone older degraded commands.
  - [x] Keep interval/lane/pending caps option-backed. `0` should disable polling if an option is added.
  - [x] Stop promptly on reconnect, terminal resolution, command eviction, route disposal, tenant/user loss, or cancellation.

- [x] T8. Tests and verification (AC1-AC9)
  - [x] Pending-command tests: accepted registration, no registration before acceptance, malformed/oversized `MessageId` fail-closed behavior, duplicate registration merge/reject behavior, unknown `MessageId` ignore/external summary behavior, cap/eviction behavior, disposal cleanup, tenant/user fail-closed, exactly-once terminal transition, duplicate terminal no-op, lifecycle-dispatch failure behavior, and idempotent metadata preservation.
  - [x] Generated form tests: accepted command registers metadata, dispatch failure does not register, field values and server-validation state survive reconnect/resolution/rejection, and no raw draft is persisted.
  - [x] Resolver tests: nudge refresh, reconnect reconciliation, fallback polling, idempotency/status query if available, projection-only matching, unresolved ambiguity for multiple entity-key candidates, duplicate/out-of-order terminals, reconnect-plus-polling race, rejection after optimistic success, success after rejection, cancellation, and stale/superseded reconnect epoch cleanup.
  - [x] Badge tests: desaturated confirming state, localizable state label remains visible, accessible label parity, forced-colors/grayscale readability, confirmed 200 ms transition, rejected rollback, idempotent-confirmed direct saturation, and reduced-motion CSS.
  - [x] New-item tests: indicator placement, exact copy, 10-second auto-dismiss via fake time, filter/search/sort dismissal, materialized-row cleanup, `aria-live`/`aria-describedby`, and no duplicate row after duplicate terminal outcome.
  - [x] Summary/rejection tests: confirmed/rejected/already-applied summary rows, Danger rejection copy format, no auto-dismiss for rejection, no raw payload/problem details, and no form remount.
  - [x] Polling tests: interval option, pending cap, oldest-first fairness, ETag validator usage, 304 no-churn, 429/503 preserve visible state, cleanup on reconnect/dispose, and no duplicate polling loop.
  - [x] Regression suite: run targeted Contracts/Shell/SourceTools generated-form tests plus `dotnet build -warnaserror` unless unrelated local dev work is failing.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs` | Six-state lifecycle enum ending in `Confirmed` or `Rejected`; no explicit pending/degraded enum state. | Prefer Shell pending-command state over expanding the public enum unless implementation proves append-only contract is required. |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleTransition.cs` | Carries `CorrelationId`, `MessageId`, timestamp anchors, and `IdempotencyResolved`. | Preserve idempotency signal and exactly-one terminal outcome behavior. |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` | Scoped correlation-keyed state service with bounded seen-MessageId cache and duplicate terminal suppression. | Reuse for terminal transitions; do not create a competing lifecycle service. |
| `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` | Accepted command result carries `MessageId`, status, optional server correlation/location/retry metadata. | Use `MessageId` as the idempotency anchor; do not invent a second client command ID. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs` | Bounded visible-lane nudge/fallback refresh scheduler using `IProjectionPageLoader`. | Extend or compose with this scheduler; do not add a parallel lane registry or polling timer. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` | Explicit `304` no-change path dispatches `LoadPageNotModifiedAction` with no state mutation. | Polling/reconciliation must keep no-churn semantics. |
| `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` | Suppresses duplicate badge count emissions and bridges projection nudges. | Do not emit badge animations on duplicate or 304 no-change outcomes. |
| `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.*` | Text-first semantic badge wrapper with localized contextual aria label. | `FcDesaturatedBadge` should wrap/reuse this behavior rather than fork slot mapping. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Generated forms own `EditContext`, lifecycle wrapper, validation store, warning publisher, and auth redirect handling. | Pending registration must not remount forms, clear values, or store raw drafts. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-5 decision |
| --- | --- | --- | --- |
| ULID command `MessageId` | Story 5-1 command client | Pending-command index and resolver | `MessageId` is the idempotency key and duplicate-outcome guard. |
| Command response taxonomy | Story 5-2 classifier | Rejection/summary UI | Reuse typed exceptions/outcomes; no UI raw-status switch. |
| Lifecycle idempotency signal | Story 2-3/5-1 lifecycle service | 5-5 summary and wrapper | Preserve `IdempotencyResolved` to avoid duplicate celebration. |
| Projection connection state | Story 5-3 | Polling and pending outcome recovery | Poll only while realtime unavailable and stop on reconnect. |
| Reconnect reconciliation | Story 5-4 | Pending resolver | Use reconciliation results as one outcome signal; do not duplicate sweep/summary. |
| Optimistic status badge | Story 4-2 badge system | 5-5 UI | Wrap `FcStatusBadge`; text remains mandatory. |
| New-item visibility | Story 4 DataGrid lanes | 5-5 indicator | Add transient row indicator without changing filter semantics. |
| Build-time observability | Story 5-6 | 5-5 | 5-5 logs redacted local diagnostics only; distributed traces remain 5-6. |

### Contract Assumptions and Outcome State Matrix

Party-mode review on 2026-04-26 tightened the implementation contract before development:

- `MessageId` is the single idempotency key for client-visible pending-command resolution. Correlation ID is supporting metadata, not a second dedupe axis.
- Terminal outcomes are immutable once accepted by the Shell reducer/service: first terminal outcome wins; duplicate, stale, and out-of-order observations become no-ops except for bounded redacted summary metadata.
- Live SignalR nudge refresh, reconnect reconciliation, fallback polling, and optional idempotency/status query results must enter the same pending-command outcome reducer/service method.
- Reconnect reconciliation and fallback polling must never replay commands. They only observe projection/status outcomes and reconcile visible state.
- A pending command with no terminal result remains pending until timeout/degraded policy, eviction, disposal, tenant/user loss, or explicit unresolved handling applies.
- Unknown `MessageId` outcomes are ignored or summarized as external activity; they must not mutate pending-command state.
- Fallback matching without a trustworthy `MessageId` is allowed only for unambiguous framework-controlled keys. Ambiguous candidates remain unresolved rather than risking a false confirmation.
- Reconnect summaries are circuit-scoped, degraded-session-scoped, and bounded. They explain outcome state; they are not an audit log or durable command history.

| State | Trigger | Visible badge/status | Summary behavior | Rollback / data behavior |
| --- | --- | --- | --- | --- |
| `Confirming` | Accepted command enters `Syncing` or degraded pending state | Localizable visible label plus target status text; desaturated; not color-only | No terminal summary yet | Prior confirmed value retained for rollback |
| `Confirmed` | First accepted terminal success for `MessageId` | Full saturation; target status becomes confirmed | Counted/listed once as confirmed | No replay; pending entry terminal/removed |
| `IdempotentConfirmed` / `AlreadyApplied` | Duplicate success or server says work already applied | Full saturation without second animation | Counted/listed once as already applied | No duplicate row, toast, lifecycle callback, or new-item indicator |
| `Rejected` | First accepted terminal rejection for `MessageId` | Prior confirmed status restored; rejection context shown | Counted/listed once as rejected with next-action fallback | Form values and validation state preserved |
| `NeedsReview` / unresolved | Polling/reconnect cannot safely match or command is evicted unresolved | Pending/degraded state remains understandable | Summarized as unresolved/degraded, redacted | No invented confirmation; no raw key construction from user input |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Pending commands are tracked in Shell-scoped bounded state keyed by correlation ID and ULID `MessageId`. | The framework needs exactly-once terminal resolution without storing raw drafts. | Persist command payloads to LocalStorage; rely only on component-local state. |
| D2 | `MessageId` is the idempotency anchor. | Story 5-1 already generates ULIDs for EventStore command idempotency. | Generate another optimistic-update ID; use row key alone. |
| D3 | Duplicate terminal outcomes are idempotent no-ops with optional summary metadata. | Prevents double success, duplicate rows, and repeated badge animations after reconnect. | Re-dispatch terminal lifecycle transitions; replay commands on reconnect. |
| D4 | Optimistic badge UI wraps `FcStatusBadge`. | Keeps color mapping, localization, and "text is always present" behavior consistent. | Add an unrelated badge component with new slot/color mapping. |
| D5 | Rejected optimistic updates roll back to the prior confirmed state. | Users must see that data did not change while preserving the rejection reason. | Leave the optimistic target visible after rejection; clear the badge entirely. |
| D6 | New-item indicator is transient and lane-scoped. | It explains why a newly created row appears despite current filters without changing filter rules. | Force filters to include the new row permanently; show a global toast only. |
| D7 | Polling fallback extends Story 5-3's scheduler. | A second polling loop would race visible-lane refresh and cache no-churn rules. | Add component-owned timers per command or per row. |
| D8 | Pending outcome matching can use framework-controlled entity/aggregate keys only when `MessageId` is absent from projection data. | Some projections may not echo command metadata, but raw form values are unsafe keys. | Match from raw command fields, free-text search, or serialized payloads. |
| D9 | Degraded rejection copy is plain text and non-auto-dismiss. | Users need a durable explanation after network recovery. | Render server HTML; auto-dismiss rejection after a fixed timeout. |
| D10 | Full distributed tracing remains out of scope. | Story 5-6 owns end-to-end observability and build-time enforcement. | Add OpenTelemetry span design piecemeal in 5-5. |
| D11 | No automatic command replay. | Idempotency resolves duplicate observations, not blind client retries after unknown network state. | Resubmit in-flight commands on reconnect. |
| D12 | Runtime pending state is bounded by policy. | Long-lived tabs and repeated offline work can otherwise leak memory. | Keep an unbounded history for convenience. |
| D13 | `MessageId` and fallback keys are validated before they influence state. | Pending-command state is a security boundary against malformed server echoes and accidental caller-controlled identifiers. | Accept any non-empty string; normalize from raw form values. |
| D14 | Ambiguous projection-only matches resolve to `NeedsReview` / unresolved. | False confirmations are worse than asking the user to review an unresolved command. | Pick the newest matching row; confirm all candidates. |
| D15 | Reconnect outcome summaries are bounded and circuit-scoped. | The summary must help the current user recover without becoming an audit log, memory leak, or PII surface. | Persist a durable history; render every historical terminal outcome. |

### Library / Framework Requirements

- Target current repo package lines and TFMs: .NET 10, Blazor, Fluxor, Fluent UI Blazor, xUnit, bUnit, Shouldly, NSubstitute, and existing `Microsoft.AspNetCore.SignalR.Client`.
- Use `System.Text.Json` web defaults already used by EventStore query/cache code. Do not introduce Newtonsoft.Json for status queries or synthetic test fixtures.
- Microsoft Learn documents that SignalR `.WithAutomaticReconnect()` is opt-in, uses default delays of 0, 2, 10, and 30 seconds, fires `Reconnecting`, then `Reconnected` on success or `Closed` after failed attempts. 5-5 should consume the existing 5-3 state rather than listening to raw SignalR events directly.
- MDN documents `filter: saturate()` as the CSS function for saturation/desaturation. Use `filter: saturate(0.5)` for the confirming badge, with reduced-motion CSS disabling transitions.
- Use component-scoped `.razor.css` for badge/indicator styling where possible. Avoid global CSS unless the generated DataGrid host requires it.
- Use `TimeProvider` or existing fake-time helpers for pending eviction, new-item auto-dismiss, polling intervals, and transition tests. Avoid real sleeps.

External references checked on 2026-04-26:

- Microsoft Learn: ASP.NET Core SignalR .NET client automatic reconnect behavior: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client
- Microsoft Learn: `HubConnectionBuilderExtensions.WithAutomaticReconnect` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.signalr.client.hubconnectionbuilderextensions.withautomaticreconnect
- Microsoft Learn: `JsonSerializerOptions.Web` API: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.web
- MDN: CSS `saturate()` filter function: https://developer.mozilla.org/en-US/docs/Web/CSS/filter-function/saturate

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/*` | Bounded pending-command index, outcome resolver, terminal summary state, reducers/effects or scoped service. |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` and `Shell/Options/FcShellOptionsThresholdValidator.cs` | Append-only pending-command/polling/new-item duration caps if needed. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Register pending command metadata after accepted dispatch; preserve form state. |
| `src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor*` | Optimistic status badge wrapper over `FcStatusBadge`. |
| `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor*` or generated host path | New row indicator and scoped styling. |
| `src/Hexalith.FrontComposer.Shell/Components/EventStore/*Pending*` or `Components/Lifecycle/*Pending*` | Reconnect outcome summary and degraded rejection UI. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs` | Extend existing fallback to pending command outcome parity if needed. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/*` | Pending registration, resolution, cap, eviction, polling, and redaction tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcDesaturatedBadgeTests.cs` | Optimistic badge rendering, accessibility, and reduced-motion tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs` | New-item indicator placement, dismissal, and accessibility tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/*Command*` | Generated command-form pending registration and form preservation tests. |

### Testing Standards

- Use xUnit, Shouldly, NSubstitute, bUnit, fake `TimeProvider`, fake `ICommandService`, fake `ILifecycleStateService` or real scoped service where useful, fake `IProjectionPageLoader`, and fake pending-command resolver.
- No live EventStore, live SignalR server, browser, Playwright, or network is required for Story 5-5.
- Keep tests deterministic: no real timers, no wall-clock animation waits, no random command order.
- Prefer state/service tests for exactly-once and duplicate handling, then bUnit tests for the visible optimistic badge/new-item/summary surfaces.
- Test accessibility by rendered markup and text: visible labels, `aria-label` confirming suffix, `aria-live="polite"`, `aria-describedby`, and non-auto-dismiss rejection behavior.
- Diagnostics tests must assert absence of bearer tokens, raw tenant/user IDs, raw command payloads, raw form values, and unbounded ProblemDetails details.

### Scope Guardrails

Do not implement these in Story 5-5:

- EventStore transport seams, endpoint defaults, or SignalR hub wrapper replacement -- Stories 5-1 and 5-3.
- HTTP response matrix, field validation mapping, auth redirect, or ETag cache core behavior -- Story 5-2.
- Basic disconnected banner, form-state preservation for hub drops, and first fallback visible-lane refresh -- Story 5-3.
- Rejoin sweep, `Reconnected -- data refreshed`, schema-mismatch invalidation UX, or changed-lane animation -- Story 5-4.
- Build-time infrastructure enforcement, OpenTelemetry spans, distributed tracing, or provider deployment parity -- Story 5-6.
- Reusable SignalR fault injection package -- Story 5-7.
- Raw command draft persistence in LocalStorage, ETag cache, query cache, logs, or pending-command state.
- Full recovery after Blazor Server circuit disposal/rejection. This story handles EventStore projection hub degradation inside the surviving circuit.
- Global event-envelope version negotiation, expected-revision conflict policy, or domain event migration policy.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Build-time infrastructure enforcement and full lifecycle observability spans. | Story 5-6 |
| Reusable SignalR fault-injection test harness. | Story 5-7 |
| Provider/Pact verification for idempotent command status and reconnect outcome queries. | Story 10-3 |
| Diagnostic ID catalog expansion for pending-command resolver failures if new HFC IDs are required. | Story 9-4 |
| Localization resource sweep for new optimistic/pending summary copy if this story keeps some copy inline by precedent. | Story 9-5 or localization hardening task |
| Full event-envelope idempotency governance beyond client `MessageId` observation. | Story 5-6 or Story 9-4 |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.5] -- story statement, baseline ACs, FR28/FR29/FR31, UX-DR6/UX-DR8/UX-DR46/UX-DR47, NFR44-NFR47.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] -- EventStore command `MessageId`, lifecycle contracts, tenant/user fail-closed rules, and SignalR nudge-only contract.
- [Source: _bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md] -- response classifier, command rejection taxonomy, ETag cache, 304 no-change semantics, and form validation preservation.
- [Source: _bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md] -- projection connection state, inline disconnected UX, fallback scheduler, and form-state guardrails.
- [Source: _bmad-output/implementation-artifacts/5-4-reconnection-reconciliation-and-batched-updates.md] -- reconnect reconciliation boundary, changed-lane sweep, schema mismatch behavior, and 5-5 exclusions.
- [Source: _bmad-output/planning-artifacts/architecture.md#EventStore-communication-contract] -- REST + SignalR + ULID idempotency architecture.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01] -- cross-story contract clarity upfront.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- tenant/user isolation fail-closed.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L08] -- party review and elicitation are complementary hardening passes.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L12] -- full async bridge lifecycle contract upfront.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L14] -- bounded runtime state/cache policy.
- [Source: src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs] -- current lifecycle exactly-once and duplicate MessageId behavior.
- [Source: src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs] -- existing bounded visible-lane fallback refresh seam.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor] -- existing text-first status badge wrapper.
- [Source: Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) -- automatic reconnect event behavior.
- [Source: MDN: CSS saturate()](https://developer.mozilla.org/en-US/docs/Web/CSS/filter-function/saturate) -- badge desaturation styling.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-04-26: Resolved workflow config, loaded sprint/story context, and moved `5-5-command-idempotency-and-optimistic-updates` to in-progress.
- 2026-04-26: Implemented pending command state/resolver, generated-form registration, optimistic/new-item/reconnect summary UI surfaces, and pending polling coordinator integration.
- 2026-04-26: Re-ran focused tests after each red/green step; accepted generated snapshot updates for command forms and current Fluent icon serialization.

### Completion Notes List

- Added bounded pending-command options and DI registrations for pending state, outcome resolution, new-item indicators, status query polling, and polling coordinator.
- Generated command forms now register accepted command metadata after `CommandResult` acceptance and before acknowledged lifecycle dispatch.
- Added reconnect outcome summary surface and pending-command fallback polling in the existing projection fallback polling loop.
- Verified full solution build and regression suite: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`; `dotnet test Hexalith.FrontComposer.sln --no-build` => Contracts 91/0/0, Shell 1175/0/3, SourceTools 486/0/0, Bench 2/0/0.

### File List

- `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.css`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcPendingCommandSummaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`

### Change Log

- 2026-04-26: Completed Story 5.5 implementation and moved status to review.

## Party-Mode Review

Date/time: 2026-04-26T06:05:58+02:00

Selected story key: `5-5-command-idempotency-and-optimistic-updates`

Command/skill invocation used: `/bmad-party-mode 5-5-command-idempotency-and-optimistic-updates; review;`

Participating BMAD agents:

- Winston (System Architect)
- John (Product Manager)
- Sally (UX Designer)
- Murat (Master Test Architect and Quality Advisor)

Findings summary:

- Cross-story contracts from Stories 5-1 through 5-4 were carrying too much implicit risk. The story needed explicit assumptions for `MessageId` lifetime, terminal outcome immutability, reconnect precedence, and shared resolver-path parity.
- "Exactly once" needed to be scoped to exactly-once UI terminal resolution per `MessageId`, not distributed exactly-once delivery.
- Optimistic badge, rollback, already-applied, unresolved, and reconnect-summary states needed visible/accessibility/localization contracts before implementation.
- Fallback polling needed to be defined as terminal outcome parity through the same resolver, not a separate transport behavior.
- Race and redaction tests needed to cover duplicate, stale, out-of-order, live/reconnect/polling overlap, sensitive-looking values, and fake-clock timing.

Changes applied:

- Tightened AC1-AC2 to define first-terminal-wins, duplicate/stale/out-of-order no-op behavior, and exactly-once UI terminal resolution per `MessageId`.
- Tightened AC3-AC5 and AC8-AC9 for localizable visible state labels, forced-colors/grayscale/reduced-motion behavior, accessible summaries, redaction, and deterministic fake-clock coverage.
- Added a Contract Assumptions and Outcome State Matrix that defines `Confirming`, `Confirmed`, `IdempotentConfirmed` / `AlreadyApplied`, `Rejected`, and `NeedsReview` / unresolved behavior.
- Added task guidance for duplicate registration, unknown `MessageId` handling, one shared resolver method across live/reconnect/polling, first-terminal-wins conflict handling, localizable whole strings, scoped reconnect summary limits, and shared resolver contract tests.

Findings deferred:

- Exact visual container for reconnect summary (inline panel vs toast vs expandable details) remains an implementation/design choice as long as the accessible behavior is met.
- Final microcopy polish remains deferred to localization hardening, but the state/copy intent and whole-string localization contract are now explicit.
- Durable pending-command persistence across browser refresh, automatic command replay, global idempotency infrastructure, rich reconnect diff UX, distributed tracing, and broad chaos/load harness work remain out of scope for Story 5-5.

Final recommendation: ready-for-dev

## Advanced Elicitation

Date/time: 2026-04-26T09:04:15+02:00

Selected story key: `5-5-command-idempotency-and-optimistic-updates`

Command/skill invocation used: `/bmad-advanced-elicitation 5-5-command-idempotency-and-optimistic-updates`

Batch 1 method names:

- Pre-mortem Analysis
- Failure Mode Analysis
- Red Team vs Blue Team
- First Principles Analysis
- Occam's Razor Application

Reshuffled Batch 2 method names:

- Chaos Monkey Scenarios
- Security Audit Personas
- Self-Consistency Validation
- Comparative Analysis Matrix
- Hindsight Reflection

Findings summary:

- Projection-only fallback matching could falsely confirm a pending command when multiple unresolved commands share the same framework-controlled entity or expected status.
- Reconnect outcome summaries needed explicit bounds so they do not become unbounded audit/history surfaces or leak sensitive command context.
- Pending-command identifiers needed fail-closed validation before entering state or accepting terminal observations.
- Polling parity needed a fairness rule so bursts of new pending commands do not indefinitely postpone older unresolved commands.
- Transient new-item indicators needed cleanup when the normal row materializes, not only on timer/filter changes.
- Lifecycle terminal dispatch failure needed a no-reopen/no-retry rule so operational failure does not create duplicate user-visible outcomes.

Changes applied:

- Tightened AC7-AC9 for oldest-first bounded polling fairness, bounded unresolved summary behavior, malformed identifier rejection, ambiguous fallback-match unresolved handling, and new-item materialization cleanup.
- Added task guidance for `MessageId` validation, ambiguous entity-key matching, lifecycle-dispatch failure handling, bounded summary overflow, normal-row indicator cleanup, and oldest-first polling.
- Added D13-D15 covering identifier validation, unresolved handling for ambiguous projection-only matches, and bounded circuit-scoped reconnect summaries.
- Expanded test guidance for malformed/oversized identifiers, lifecycle-dispatch failure, multiple-candidate ambiguity, materialized-row cleanup, and polling fairness.

Findings deferred:

- Durable cross-circuit pending-command recovery remains out of scope for Story 5-5.
- Automatic command replay remains rejected; reconnect and polling observe outcomes only.
- Distributed tracing, audit history, provider/Pact verification, and full chaos/load harness work remain with Stories 5-6, 5-7, 10-3, or later governance stories.

Final recommendation: ready-for-dev
