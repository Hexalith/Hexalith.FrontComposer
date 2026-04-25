# Story 5.5: Command Idempotency & Optimistic Updates

Status: ready-for-dev

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
| AC1 | A command was accepted with a framework-generated ULID `MessageId` before or during EventStore projection-hub degradation | The command terminal outcome is observed through SignalR nudge refresh, reconnect reconciliation, fallback polling, or an EventStore idempotency/query response | The matching lifecycle resolves to exactly one terminal state (`Confirmed`, `Rejected`, or idempotent confirmed), no duplicate success/rejection notification is shown, the pending entry is removed or marked terminal once, and no command is replayed automatically. |
| AC2 | A duplicate terminal outcome arrives for the same `MessageId` or the same command is re-observed after reconnection | The pending-command resolver processes it | The outcome is treated as idempotent, user-visible messaging says the work was already applied only once, `CommandLifecycleTransition.IdempotencyResolved` is preserved when applicable, and no second row insert, badge animation, toast, or lifecycle terminal callback is emitted. |
| AC3 | A command predicts a status change for a visible row or badge lane | The command is acknowledged and enters `Syncing` or degraded pending state | `FcDesaturatedBadge` immediately renders the target state with `filter: saturate(0.5)`, keeps the text label visible, adds an aria label suffix like `(confirming)`, and uses the prior confirmed state as the rollback value. |
| AC4 | An optimistic status update confirms, rejects, or resolves idempotently | The terminal outcome is applied | Confirmed restores full saturation with a 200 ms CSS transition; Rejected reverts to the prior confirmed status without losing form input; IdempotentConfirmed skips the revert animation and saturates directly; `prefers-reduced-motion: reduce` makes every transition instantaneous. |
| AC5 | A create command confirms a new entity that is visible in the current DataGrid lane but does not match the active filter/search/sort criteria | The projection page is reconciled or polled after the command outcome | `FcNewItemIndicator` renders the new row at the top of the grid lane with text `New -- may not match current filters`, uses `aria-live="polite"` and `aria-describedby`, highlights subtly with Fluent info styling, and auto-dismisses after 10 seconds or immediately on the next filter/search/sort change. |
| AC6 | A domain-specific rejection is discovered during degraded network recovery | The rejection reaches the client after reconnect or fallback polling | The message format is `[What failed]: [Why]. [What happened to the data].`, renders as plain text in a Danger `FluentMessageBar`, does not auto-dismiss, preserves form model/EditContext values, and does not clear optimistic context until the rollback is visible. |
| AC7 | SignalR remains unavailable after the disconnected fallback threshold | The polling fallback activates | ETag-gated polling queries at configured bounded intervals maintain projection and command-outcome correctness for visible lanes and pending commands; polling preserves the same visible behavior as SignalR-confirmed outcomes; polling stops on reconnect, unsubscribe, disposal, tenant/user loss, or option disablement. |
| AC8 | A user returns from a disconnection with commands in flight | Reconnect reconciliation and pending-command resolution complete | A clear non-blocking summary lists each pending command as confirmed, rejected, or already applied; no manual page refresh is needed; no raw command payload, tenant/user value, token, or unbounded server problem detail is shown or logged. |
| AC9 | Tests run | Contracts, Shell services, reducers/effects, generated command forms, badges, DataGrid indicators, and polling paths execute | Coverage proves exactly-once terminal resolution, duplicate/idempotent handling, optimistic badge confirm/reject/idempotent paths, reduced-motion CSS, new-item indicator lifetime/filter dismissal, degraded rejection copy and form preservation, bounded polling parity, redacted diagnostics, and no raw draft persistence. |

---

## Tasks / Subtasks

- [ ] T1. Add a bounded pending-command index (AC1, AC2, AC8, AC9)
  - [ ] Read `ILifecycleStateService`, `LifecycleStateService`, `ICommandServiceWithLifecycle`, `CommandResult`, `CommandLifecycleTransition`, and Story 5-2 command taxonomy before editing.
  - [ ] Create a Shell-scoped state/service under `Shell/State/PendingCommands/` or the adjacent lifecycle state folder that tracks pending commands by correlation ID and ULID `MessageId`.
  - [ ] Store only framework metadata required for resolution: correlation ID, message ID, command type name, expected projection type/lane, optional aggregate/entity key, expected status slot, prior status slot, submit time, and bounded last outcome. Do not store raw command payloads or field values.
  - [ ] Add an option-backed cap such as `MaxPendingCommandEntries` and FIFO/LRU eviction diagnostics per L14. Eviction must mark the command unresolved/degraded, not silently confirmed.
  - [ ] Resolve terminal outcomes exactly once using atomic state transitions. Duplicate outcomes for a terminal entry must be no-ops except for optional idempotent summary metadata.
  - [ ] Dispose/clear per-circuit state on service disposal and tenant/user scope loss.

- [ ] T2. Register pending commands from generated command forms without remounting forms (AC1, AC6, AC9)
  - [ ] Extend generated command-form submit flow only after reading `CommandFormEmitter.cs`, Story 5-2 server-validation handling, and Story 5-3 form-preservation tests.
  - [ ] Register the pending entry after `ICommandService.DispatchAsync` returns an accepted `CommandResult` with `MessageId`. If dispatch fails before acceptance, do not create a pending command.
  - [ ] Capture correlation/message metadata without storing raw form models, user-entered fields, or validation messages in persistence.
  - [ ] Keep existing `EditContext` and `ValidationMessageStore` instances mounted across connection-state changes and pending-outcome updates.
  - [ ] Do not auto-retry, auto-submit, or replay the command on reconnect. Idempotency protects duplicated server observations, not client-side blind replay.

- [ ] T3. Resolve pending outcomes from reconnect, nudge, and polling paths (AC1, AC2, AC7, AC8, AC9)
  - [ ] Reuse Story 5-3 `ProjectionFallbackRefreshScheduler` and Story 5-4 reconciliation results as input signals. Do not add another visible-lane registry.
  - [ ] Define a small resolver interface that accepts projection/query outcome metadata and matches it to pending commands by `MessageId`, correlation ID, or a framework-controlled entity/aggregate key when EventStore does not echo `MessageId` in projection rows.
  - [ ] If the EventStore API exposes an idempotency/status query, call it through the existing `IQueryService`/`EventStoreQueryClient` and response classifier. If not, resolve only from projection state and record any unresolved ambiguity as a deferred follow-up rather than inventing a new provider contract.
  - [ ] Confirmed outcomes transition `ILifecycleStateService` to `Confirmed`; rejected outcomes transition to `Rejected` with Story 5-2 rejection copy; idempotent confirmed outcomes preserve idempotency metadata so `FcLifecycleWrapper` can avoid a second celebration.
  - [ ] Preserve current visible data on 304, 429, 503, auth failures, malformed responses, and schema mismatch according to Stories 5-2 through 5-4.
  - [ ] Log bounded failure categories only. No raw tenant IDs, user IDs, group names, command/query payloads, tokens, or ProblemDetails bodies.

- [ ] T4. Implement `FcDesaturatedBadge` and optimistic badge state (AC3, AC4, AC9)
  - [ ] Reuse `FcStatusBadge` and `SlotAppearanceTable` rather than adding a second badge color system.
  - [ ] Add a thin wrapper component, for example `Components/Badges/FcDesaturatedBadge.razor`, that accepts prior slot/label, optimistic slot/label, confirmation state, column header, and motion mode.
  - [ ] Always render text. Color/desaturation must never be the only status signal.
  - [ ] Add `filter: saturate(0.5)` while confirming and a 200 ms saturation transition on confirmed. Add `@media (prefers-reduced-motion: reduce)` to disable transition timing.
  - [ ] Ensure rejected rollback restores the previous confirmed slot/label and does not leave stale `(confirming)` aria copy.
  - [ ] Avoid row-wide styling here. Row/new-item highlighting belongs to T5.

- [ ] T5. Add `FcNewItemIndicator` for confirmed created rows outside current filters (AC5, AC9)
  - [ ] Read DataGrid generated view host and Story 4-3/4-4/4-5 DataGrid state before deciding where the row marker belongs.
  - [ ] Add a bounded transient state slice keyed by view/lane and entity key/message ID. Use `TimeProvider` for the 10-second auto-dismiss; do not use wall-clock sleeps.
  - [ ] Render the indicator row at the top of the current visible lane only when the confirmed entity is relevant to the lane but outside active filter/search/sort criteria.
  - [ ] Dismiss on timer expiry, next filter/search/sort change, lane disposal, tenant/user loss, or explicit row interaction if the implementation adds one.
  - [ ] Use `aria-live="polite"` on the row and `aria-describedby` for the indicator text. The visible copy must be exactly `New -- may not match current filters`.
  - [ ] Keep highlight subtle and non-decorative. Use Fluent info background at low opacity and respect reduced motion for fade-out.

- [ ] T6. Render degraded rejection and reconnect outcome summary (AC6, AC8, AC9)
  - [ ] Reuse Story 5-2 `CommandRejectedException` and warning/feedback publisher seams. Do not branch on raw HTTP status in UI components.
  - [ ] Add a small summary component under `Components/EventStore/` or `Components/Lifecycle/` that reads the bounded pending-command terminal outcomes for the current circuit.
  - [ ] Show confirmed, rejected, and already-applied rows with concise plain text. Do not show raw command JSON, payload values, stack traces, or raw ProblemDetails.
  - [ ] Use Danger `FluentMessageBar` for degraded rejection with no auto-dismiss. Confirmed/already-applied summary should be Info/Success and non-blocking.
  - [ ] Preserve form values and validation state after a rejection discovered during reconnect. The summary may reference the command display name, but must not clear or remount the form.

- [ ] T7. Upgrade polling fallback to command-outcome parity (AC7, AC9)
  - [ ] Extend the existing Story 5-3 fallback scheduler instead of creating a second polling loop.
  - [ ] Add bounded pending-command polling only while SignalR is unavailable and only for accepted commands whose terminal outcome is unresolved.
  - [ ] Use ETag-gated query paths and safe framework-controlled discriminators. If a pending command cannot be safely keyed, mark it degraded/unresolved rather than constructing a key from raw user input.
  - [ ] Keep interval/lane/pending caps option-backed. `0` should disable polling if an option is added.
  - [ ] Stop promptly on reconnect, terminal resolution, command eviction, route disposal, tenant/user loss, or cancellation.

- [ ] T8. Tests and verification (AC1-AC9)
  - [ ] Pending-command tests: accepted registration, no registration before acceptance, cap/eviction behavior, disposal cleanup, tenant/user fail-closed, exactly-once terminal transition, duplicate terminal no-op, and idempotent metadata preservation.
  - [ ] Generated form tests: accepted command registers metadata, dispatch failure does not register, field values and server-validation state survive reconnect/resolution/rejection, and no raw draft is persisted.
  - [ ] Resolver tests: nudge refresh, reconnect reconciliation, fallback polling, idempotency/status query if available, projection-only matching, unresolved ambiguity, cancellation, and stale/superseded reconnect epoch cleanup.
  - [ ] Badge tests: desaturated confirming state, label remains visible, aria `(confirming)` suffix, confirmed 200 ms transition, rejected rollback, idempotent-confirmed direct saturation, and reduced-motion CSS.
  - [ ] New-item tests: indicator placement, exact copy, 10-second auto-dismiss via fake time, filter/search/sort dismissal, `aria-live`/`aria-describedby`, and no duplicate row after duplicate terminal outcome.
  - [ ] Summary/rejection tests: confirmed/rejected/already-applied summary rows, Danger rejection copy format, no auto-dismiss for rejection, no raw payload/problem details, and no form remount.
  - [ ] Polling tests: interval option, pending cap, ETag validator usage, 304 no-churn, 429/503 preserve visible state, cleanup on reconnect/dispose, and no duplicate polling loop.
  - [ ] Regression suite: run targeted Contracts/Shell/SourceTools generated-form tests plus `dotnet build -warnaserror` unless unrelated local dev work is failing.

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

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)

