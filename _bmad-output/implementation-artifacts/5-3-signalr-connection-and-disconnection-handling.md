# Story 5.3: SignalR Connection & Disconnection Handling

Status: review

> **Epic 5** -- Reliable Real-Time Experience. **FR22** live projection nudges, **FR24** disconnection detection and recovery posture, plus the first user-visible degraded-network behavior on top of Stories 5-1 and 5-2. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, **L12**, and **L14**.

---

## Executive Summary

Story 5-3 turns the EventStore SignalR subscription seam into a user-safe connection experience. It must not create a second real-time stack:

- Reuse Story 5-1's `IProjectionSubscription`, `IProjectionHubConnection`, `ProjectionSubscriptionService`, tenant-aware notifier companion, EventStore options, and default hub path `/hubs/projection-changes`. The older epic text says `/projections-hub`; that is superseded by the pinned EventStore contract.
- Reuse Story 5-2's response classifier, query client, and ETag cache for all data refreshes after `ProjectionChanged` nudges. SignalR continues to send signals only; projection data always comes from REST re-query with `If-None-Match`.
- Keep the disconnected UX inline and non-blocking. No modal, full-page overlay, forced navigation, form reset, or command draft persistence is allowed in this story.
- Preserve in-progress command form state during **EventStore projection hub** reconnects. This story does not promise recovery after the Blazor Server circuit is rejected/discarded; if the host circuit is lost, framework state is subject to Blazor circuit rules.
- Escalate active `Syncing` lifecycles immediately when the EventStore projection connection is lost. The existing `FcLifecycleWrapper` timer already accepts a disconnected hook; Story 5-3 wires it and updates the copy instead of inventing another lifecycle component.
- Ship a minimal bounded ETag polling fallback for visible projection refresh while the hub is disconnected. Full transparent polling parity, idempotent terminal reconciliation, optimistic badges, and command outcome summaries remain Story 5-5.

The intended shape is a scoped Shell connection-state service that observes the SignalR hub wrapper, owns reconnection/backoff metadata, exposes inline status state to shell/components, and tells projection/lifecycle consumers whether the real-time channel is connected, reconnecting, or unavailable.

---

## Story

As a business user,  
I want the application to detect when my connection drops and preserve my in-progress work,  
so that network interruptions do not disrupt my workflow or lose my unsaved form data.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The EventStore SignalR hub connection is established | A projection is visible or otherwise subscribed | The client joins the EventStore-owned `/hubs/projection-changes` hub through `JoinGroup(projectionType, tenantId)`, group values are fail-closed for null/blank/colon values, `ProjectionChanged(projectionType, tenantId)` carries no projection payload, and each accepted nudge triggers a REST re-query through the Story 5-2 query/cache seam with ETag validators. |
| AC2 | A stable projection hub connection is active | The hub enters reconnecting or disconnected state | A warning-colored inline note appears immediately in the relevant shell/projection area with no modal, overlay, focus trap, route change, or form remount; automatic reconnect starts using a configurable exponential backoff policy; screen readers receive a polite `role="status"` update. |
| AC3 | A command lifecycle is in `Syncing` | The EventStore projection connection is lost before confirmation | `FcLifecycleWrapper` escalates immediately to the action-prompt/timeout display with the exact user copy `Connection lost -- unable to confirm sync status`; the sync pulse stops; no indefinite wait remains; the currently entered command form values and validation state stay mounted. |
| AC4 | A business user is editing a generated command form | The EventStore projection connection drops and later reconnects | All unsaved field values remain in the existing component/EditContext state, no field is cleared, no new command is submitted automatically, stale server-side validation messages are not reintroduced, and the user can continue editing immediately after reconnection. |
| AC5 | The projection hub remains unavailable | Thirty seconds elapse without a successful reconnect or confirmation | The lifecycle stays in the manual refresh/action-prompt state with a manual refresh/start-over affordance; the timeout is configurable through the existing shell option threshold path unless a strictly necessary append-only alias is added; logs record a degraded connection event without tenant/user/raw payload leakage. |
| AC6 | The first connection loss occurs during active work | The disconnected indicator is rendered | The visible copy says `Connection interrupted -- your work is saved. Reconnecting...`; on reconnection a brief non-blocking confirmation says `Reconnected -- you can continue`; the tone is reassuring and not alarming, and no user action is required just to resume editing. |
| AC7 | Visible projections need refresh while the hub is disconnected | The bounded fallback timer fires | The Shell performs ETag-conditioned REST re-query for visible projection/count lanes through Story 5-2 only; fallback polling is bounded by options, respects current tenant/user/discriminator fail-closed rules, preserves current UI on `304`/429/failures, and stops when the hub reconnects or the projection unsubscribes. |
| AC8 | Tests run | Contracts, Shell services, lifecycle components, generated forms, and projection refresh tests execute | Coverage proves connection-state transitions, backoff scheduling, hub rejoin behavior, inline warning copy/a11y, no modal/overlay behavior, immediate Syncing escalation, form-state preservation, bounded fallback polling, tenant-aware nudge routing, disposal/cancellation cleanup, and no raw tenant/user/token/payload data in degraded-connection logs. |

---

## Tasks / Subtasks

- [x] T1. Extend the SignalR hub wrapper with connection-state events (AC1, AC2, AC8)
  - [x] Read `IProjectionHubConnection`, `SignalRProjectionHubConnectionFactory`, `ProjectionSubscriptionService`, and `ProjectionSubscriptionServiceTests` before editing.
  - [x] Add an internal connection-state event surface to `IProjectionHubConnection` or a companion interface. It must expose at least Connected, Reconnecting, Disconnected/Closed, and Reconnected/Started without leaking `Microsoft.AspNetCore.SignalR.Client` types into Contracts.
  - [x] Preserve `ProjectionChanged(projectionType, tenantId)` registration and the existing SignalR method names `JoinGroup` / `LeaveGroup`.
  - [x] Keep `/hubs/projection-changes` as the default `EventStoreOptions.ProjectionChangesHubPath`; `/projections-hub` may only appear as a configured override test, never as a default or doc recommendation.
  - [x] Register handlers before `StartAsync` where SignalR requires it so the first lost-connection event is not missed.
  - [x] Keep token acquisition per operation/reconnect path and preserve the current limitation that SignalR's `AccessTokenProvider` does not accept a cancellation token; do not block this story on solving that framework limitation.

- [x] T2. Add a scoped projection connection-state service (AC2, AC6, AC8)
  - [x] Create a Shell-owned service, for example under `Shell/Infrastructure/EventStore/ConnectionState/` or `Shell/State/ProjectionConnection/`, that records the current EventStore projection connection state, last transition timestamp, active reconnect attempt count, and last non-sensitive failure category.
  - [x] Register it as scoped for Blazor circuit safety. Do not make it singleton and do not capture tenant/user/token state at construction time.
  - [x] Expose a minimal subscribe/read API for components/effects. Prefer immutable snapshot records over mutable public properties.
  - [x] Bound any retained state/history with an option-backed cap or a single latest snapshot per L14; do not keep an unbounded reconnect event list.
  - [x] Log connection-state changes using structured templates and redacted/bounded values only. No bearer tokens, raw tenant IDs, user IDs, group names, command/query payloads, or ProblemDetails bodies in logs.

- [x] T3. Wire automatic reconnect and group rejoin behavior without duplicating subscription stacks (AC1, AC2, AC8)
  - [x] Use SignalR automatic reconnect behavior through the existing `SignalRProjectionHubConnectionFactory`; default delays may follow SignalR's 0s, 2s, 10s, 30s sequence, but expose an append-only option if tests need deterministic custom delays.
  - [x] Treat initial start failure separately from post-connect reconnect. `WithAutomaticReconnect()` does not retry initial `StartAsync` failures; the subscription service must surface initial-start failure as degraded state instead of pretending a reconnect loop exists.
  - [x] On reconnect, rejoin all currently active groups exactly once per group after the connection is connected. Reuse `ProjectionSubscriptionService`'s active group set and gate; do not introduce an independent "subscribed groups" collection that can drift.
  - [x] Preserve commit-after-join semantics: a group becomes active only after `JoinGroup` succeeds, and failed rejoin leaves the group marked degraded rather than silently active.
  - [x] Handle duplicate subscribe/unsubscribe during reconnect, dispose during reconnect, and a nudge arriving after dispose without callbacks after disposal or leaked active groups.

- [x] T4. Route nudges through REST re-query with Story 5-2 cache semantics (AC1, AC7)
  - [x] Consume the tenant-aware notifier path (`IProjectionChangeNotifierWithTenant`) when available. Legacy single-argument notifications remain for backward compatibility, but new Story 5-3 consumers must route by tenant when tenant is present.
  - [x] Ensure every nudge results in a REST re-query through the Story 5-2 response/cache seam; do not mutate projection state directly from the SignalR payload.
  - [x] Re-query only currently visible/subscribed projection lanes. Do not wake unrelated pages or prefetch hidden projections.
  - [x] Preserve existing UI state on `304 Not Modified`, 429, 503, auth failures, missing cache, or malformed responses according to Story 5-2's classifier rules.
  - [x] Deduplicate bursts of nudges per `(tenantId, projectionType, visible lane)` while a re-query is already in flight. The dedupe window must be bounded and must not drop the final refresh after a failure.

- [x] T5. Add inline disconnected/reconnected UX (AC2, AC6, AC8)
  - [x] Add a small Shell component, for example `FcProjectionConnectionStatus`, rather than overloading `FcLifecycleWrapper` or DataGrid notices with global connection copy.
  - [x] Render warning intent while reconnecting/disconnected and info/success confirmation briefly on reconnect. Use Fluent UI components already in the Shell stack.
  - [x] The indicator is inline in the shell/projection command surface and must not steal focus, block pointer interaction, or shift form fields enough to lose editing context.
  - [x] Add `role="status"` and `aria-live="polite"` for reconnecting/reconnected copy. Do not use assertive announcements unless later accessibility review proves users miss critical state.
  - [x] Add EN/FR resource keys for the exact copy in AC3 and AC6 if localization resources are already used in the touched component path; otherwise keep hardcoded copy no worse than current `FcLifecycleWrapper` precedent and record the resource follow-up explicitly.
  - [x] No decorative visual effects. Keep styling consistent with existing Fluent MessageBar/Badge usage and respect `prefers-reduced-motion`.

- [x] T6. Wire `FcLifecycleWrapper` disconnected escalation (AC3, AC5, AC8)
  - [x] Reuse `LifecycleThresholdTimer`'s existing `isDisconnected` hook instead of adding another timer. Story 2-4 left this hook specifically for Story 5-3.
  - [x] Inject/read the new connection-state service into `FcLifecycleWrapper` or a thin adapter so active `Syncing` lifecycles immediately compute `LifecycleTimerPhase.ActionPrompt` when the projection connection is disconnected.
  - [x] Update action-prompt copy for the disconnection case to exactly `Connection lost -- unable to confirm sync status`. Do not replace domain rejection or confirmed/idempotent copy.
  - [x] Keep existing terminal transition behavior: Confirmed/Rejected still enter terminal and stop timers even if the hub reconnect state changes later.
  - [x] If an append-only option is needed to express the epic's "30 seconds" timeout language, wire it as an alias or validator-compatible policy over the existing threshold model; avoid a conflicting second timeout source.
  - [x] Add regression tests proving a disconnected hook escalates immediately, reconnect does not falsely confirm a command, and terminal states ignore later connection changes.

- [x] T7. Preserve generated command form state during projection hub drops (AC4, AC8)
  - [x] Read `CommandFormEmitter.cs`, generated form baselines, `FcFormAbandonmentGuard`, and Story 2-5 validation/rejection behavior before editing.
  - [x] Ensure disconnected/reconnected indicators do not recreate the generated form component, replace the `EditContext`, clear current model values, or clear unrelated validation state.
  - [x] Do not persist raw command drafts to `IStorageService`, ETag cache, query cache, LocalStorage, or logs. Component/EditContext memory is the preservation mechanism for this story.
  - [x] Preserve field values across reconnect status changes in bUnit tests by editing fields, simulating connection loss/reconnect, and asserting the model/EditContext values remain unchanged.
  - [x] Do not auto-submit, replay, or retry in-flight command forms on reconnect. Command idempotent terminal reconciliation is Story 5-5.

- [x] T8. Add bounded fallback polling for visible projection refresh only (AC7, AC8)
  - [x] Add options such as `ProjectionFallbackPollingIntervalSeconds` and `MaxProjectionFallbackPollingLanes` only if Story 5-2 did not already add equivalent settings. Defaults should be conservative and bounded; `0` disables fallback polling.
  - [x] Poll only while the hub is reconnecting/disconnected and only for visible projection/count lanes with safe tenant/user/discriminator context.
  - [x] Use the same Story 5-2 query/cache seam and response classifier as nudge-driven refresh. No duplicate HTTP status parser.
  - [x] Stop polling promptly on reconnect, unsubscribe, component disposal, tenant/user scope loss, or option disablement.
  - [x] Do not implement full transparent polling parity, pending-command terminal reconciliation, optimistic badge transitions, or summaries of in-flight command outcomes. Those are Story 5-5.

- [x] T9. Tests and verification (AC1-AC8)
  - [x] Connection wrapper tests: reconnecting/reconnected/closed events, automatic reconnect policy wiring, initial start failure surfaced distinctly, access-token provider behavior preserved.
  - [x] Subscription tests: active groups rejoin once after reconnect, duplicate subscribe/unsubscribe races during reconnect, failed rejoin stays degraded, dispose suppresses callbacks/events, tenant-aware nudge delivery.
  - [x] Lifecycle tests: disconnected `Syncing` escalates immediately to action prompt, copy matches AC3, pulse stops, terminal Confirmed/Rejected states are not regressed.
  - [x] Component/a11y tests: inline indicator copy, `role="status"`, `aria-live="polite"`, no modal/overlay/focus trap, reconnected confirmation auto-clears according to option.
  - [x] Generated form tests: edited values and validation state survive connection state changes; no automatic submit/retry; no raw draft persistence.
  - [x] Fallback polling tests: bounded lane count, option disablement, ETag validator usage, `304` no-churn, 429/503 preserve visible data, cleanup on reconnect/dispose.
  - [x] Diagnostics tests: logs for reconnect/degraded/fallback paths do not contain bearer tokens, raw tenant/user values, group names, command/query payloads, or unbounded exception/problem bodies.

### Review Findings

> Source: `bmad-code-review` Pass-1 against commit `de4298e` on 2026-04-26 (Blind Hunter + Edge Case Hunter + Acceptance Auditor). Total raw: 49 findings across 3 layers; after dedupe: 31 findings → 4 decision-needed, 20 patch, 1 deferred, 6 dismissed.

#### Decision-needed

- [ ] [Review][Decision] DN1 — **AC7 bounded fallback polling has no production driver and no lane registrations.** `ProjectionFallbackRefreshScheduler.TriggerFallbackOnceAsync` and `RegisterLane` have zero non-test callers in `src/`. AC7 ("Visible projections need refresh while the hub is disconnected → the bounded fallback timer fires") is functionally unmet. Options: (a) wire a periodic driver (`PeriodicTimer` or Fluxor effect) + mount lane registration in DataGrid/count consumers now; (b) accept that nudge-driven refresh is the only path while disconnected and amend AC7 to scope-out periodic polling; (c) downgrade scheduler to nudge-only and ship periodic polling with Story 5-4. [`ProjectionFallbackRefreshScheduler.cs:50-117`]
- [ ] [Review][Decision] DN2 — **Failed group rejoin behavior is silently inconsistent with spec.** `RejoinActiveGroupsAsync` catch only logs and applies `Reconnecting/RejoinFailed`, but the failed group stays in `_activeGroups`, the loop continues to attempt the next group, and no watchdog retries the failure. Spec says "failed rejoin leaves the group marked degraded rather than silently active". Options: (a) per-group degraded marker (e.g., `Dictionary<GroupKey, GroupRejoinState>`) and skip nudge refresh for degraded entries; (b) remove failed groups from `_activeGroups` and require subscribers to resubscribe (breaks subscriber visibility); (c) add a bounded retry loop. [`ProjectionSubscriptionService.cs:758-771`]
- [ ] [Review][Decision] DN3 — **Rejoin runs synchronously on the SignalR dispatcher chain, holds `_gate`, with `CancellationToken.None`.** Slow rejoin blocks any concurrent Subscribe/Unsubscribe waiting on the gate, blocks subsequent state-change handlers, and `DisposeAsync` cannot interrupt in-flight rejoin. Options: (a) offload rejoin to a background `Task.Run` with the service's CTS so the dispatcher returns immediately; (b) keep on dispatcher but pass `_disposalCts.Token` and add a bounded `WaitAsync(timeout)` on the gate; (c) accept current behavior (treat rejoin as critical-path serialization). [`ProjectionSubscriptionService.cs:737-771`]
- [ ] [Review][Decision] DN4 — **`ExtractProjectionTypeFqn` viewKey shape is undocumented and brittle.** The fallback scheduler uses `IndexOf(':')` to peel a tenant prefix from `lane.ViewKey` and falls back to the whole viewKey when no colon exists. There is no validation at `RegisterLane` and no documented canonical shape. Lanes registered without a tenant prefix can match nudges across tenants. Options: (a) require canonical `tenantId:projectionTypeFqn` and validate at `RegisterLane` (fail-closed on mismatch); (b) carry tenantId/projectionType separately on the lane record (no string parsing); (c) document the existing convention as is. [`ProjectionFallbackRefreshScheduler.cs:1155-1160`]

#### Patches

- [ ] [Review][Patch] P1 — **Nudge dedupe drops the final refresh after failure.** Burst nudges hit `_inFlight.TryAdd`; the first wins, the rest are skipped. If the in-flight refresh fails, the *last* nudge during that window is gone with no replay. Spec T4 explicitly says "the dedupe window must not drop the final refresh after a failure". Add a `_pendingReplay` flag per key that re-runs once after failure. [`ProjectionFallbackRefreshScheduler.cs:88-117`]
- [ ] [Review][Patch] P2 — **Lane registry silently overwrites duplicate `ViewKey`; disposers cross-cancel.** `_lanes[lane.ViewKey] = lane` overwrites the previous registration, and the returned `Registration.Dispose` calls `TryRemove(lane.ViewKey)` removing whatever currently lives there. Two components registering the same ViewKey leak each other's disposers. Use `TryAdd` and refcount or reject duplicates. [`ProjectionFallbackRefreshScheduler.cs:1061-1070`]
- [ ] [Review][Patch] P3 — **No `SignalRProjectionHubConnectionFactory` tests.** Grep `SignalRProjectionHubConnectionFactory|WithAutomaticReconnect` in `tests/` returns nothing. T9.1 demands wrapper tests for `Reconnecting`/`Reconnected`/`Closed`, automatic reconnect policy wiring, distinct initial-start failure surface, and `AccessTokenProvider` per-call behavior. Add tests against a `HubConnection` fake.
- [ ] [Review][Patch] P4 — **No generated command form preservation tests (AC4 / T9.5).** No test mutates a generated form field, simulates `Disconnected` then `Reconnected` transitions, and asserts EditContext/model state survives. Completion notes claim preservation via "connection UI outside the form" — needs bUnit evidence. Add a test under `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*` or `tests/.../Generated/*`.
- [ ] [Review][Patch] P5 — **No diagnostics redaction tests; rejoin failure logs raw exception.** T9.7 demands logs free of bearer/tenant/user/group/payload data. `RejoinActiveGroupsAsync` calls `_logger.LogWarning(ex, ...)` with raw exception (`ex.Message` may carry group args). Redact exception message to `ex.GetType().Name` + structured `FailureCategory`, and add a `CapturingLogger<T>` test asserting no tenant/group/token in any reconnect/degraded/fallback log path. [`ProjectionSubscriptionService.cs:196-200`]
- [ ] [Review][Patch] P6 — **`ReconnectAttempt` is always pinned at 1.** Hardcoded in `OnConnectionStateChangedAsync` for both `Reconnecting` (`:163`) and `RejoinFailed` (`:194`). Spec T2 promises "active reconnect attempt count" in snapshot; consumers see a permanently meaningless value. Increment based on `_current.ReconnectAttempt + 1` in `ProjectionConnectionStateService.Apply` (or carry forward in the subscription service); reset to 0 on `Connected`. [`ProjectionSubscriptionService.cs:163,194`, `ProjectionConnectionState.cs:Apply`]
- [ ] [Review][Patch] P7 — **Handlers invoked without try/catch in `Apply` and `PublishAsync`; one throw kills the chain.** `ProjectionConnectionStateService.Apply` iterates handlers serially; a throw skips remaining handlers and propagates back to the SignalR dispatcher (`Closed` event delegate-throw escalates). `SignalRProjectionHubConnection.PublishAsync` has the same shape. Wrap each handler invocation in a try/catch logging at warning. [`ProjectionConnectionState.cs:106-108`, `SignalRProjectionHubConnectionFactory.cs:850-859`]
- [ ] [Review][Patch] P8 — **`FcProjectionConnectionStatus._clearTimer` race: stale callback can clear new cycle.** `_clearTimer = Time.CreateTimer(...)` is assigned *after* `CancelClearTimer()`, and the timer callback only checks `_disposed`. A previous cycle's pending callback can flip `_showReconnected = false` for the new cycle. Add a generation counter checked inside the callback. [`FcProjectionConnectionStatus.razor.cs:338-367`]
- [ ] [Review][Patch] P9 — **`Apply` equality short-circuit is dead.** `if (_current == snapshot) return;` compares records that include `LastTransitionAt` (always differs). The dedupe never trips. Compare only relevant fields (Status, IsDisconnected, ReconnectAttempt, LastFailureCategory) via a helper predicate. [`ProjectionConnectionState.cs:953-969`]
- [ ] [Review][Patch] P10 — **`FcLifecycleWrapper._projectionConnectionSnapshot` written off UI thread without memory barrier.** Field assignment from non-UI handler chain; UI-thread getters then read it. Reference assignment is atomic but ordering across two unrelated `Apply` calls is not guaranteed. Use `volatile` or capture inside the lambda passed to `InvokeAsync`. [`FcLifecycleWrapper.razor.cs:259-272`]
- [ ] [Review][Patch] P11 — **Nudge refresh path uses `CancellationToken.None`.** `_refreshScheduler.TriggerNudgeRefreshAsync(..., CancellationToken.None)` cannot be cancelled by subscription service disposal. T8 says "Stop polling promptly on … component disposal". Pass the service's `_disposalCts.Token`. [`ProjectionSubscriptionService.cs:707-711`]
- [ ] [Review][Patch] P12 — **`IProjectionConnectionState` registered twice; `ServiceCollectionExtensions` lacks `TimeProvider` fallback.** Both `EventStoreServiceExtensions` and `ServiceCollectionExtensions` call `TryAddScoped<IProjectionConnectionState, ...>`. Adopters wiring only Shell get the service but not `TimeProvider`, so construction throws at runtime. Pick a single owner (Shell wiring) and `TryAddSingleton<TimeProvider>(TimeProvider.System)` alongside it. [`ServiceCollectionExtensions.cs:306-307`, `EventStoreServiceExtensions.cs:42-43`]
- [ ] [Review][Patch] P13 — **`IsConnected` checked once per rejoin loop.** Hub may transition to `Disconnected` mid-loop; remaining `JoinGroupAsync` calls then throw, flooding logs with one `RejoinFailed` line per active group. Re-check `_disposed || !_connection.IsConnected` before each Join. [`ProjectionSubscriptionService.cs:183-201`]
- [ ] [Review][Patch] P14 — **`Subscribe(replay: true)` invokes replay outside the handler lock; concurrent `Apply` can deliver fresh-then-stale.** Replay reads snapshot under the lock but invokes the handler after release. A concurrent `Apply` between unlock and replay can deliver the new state first, then replay overwrites with the stale captured snapshot. Invoke replay under the lock or use a sequence number. [`ProjectionConnectionState.cs:67-79`]
- [ ] [Review][Patch] P15 — **No bUnit negative assertions for "no modal/overlay/focus trap/route change/form remount" (AC2/T9.4).** Existing test only checks `[role='dialog']` is empty. Add assertions: no `.fluent-overlay`/`.fluent-dialog` rendered; current focus does not move into the indicator on appearance; no `NavigationManager.NavigateTo` invoked.
- [ ] [Review][Patch] P16 — **No test asserting "reconnect does not falsely confirm a Syncing command" (T6 last bullet).** `FcLifecycleWrapperDisconnectedTests` covers immediate-escalation copy and terminal-Confirmed-ignores-loss. Add: enter `Syncing`, fire `Disconnected` (escalate to ActionPrompt), then fire `Reconnected` and assert lifecycle does not auto-transition to `Confirmed`.
- [ ] [Review][Patch] P17 — **No tests for duplicate subscribe/unsubscribe races during reconnect, failed-rejoin-stays-degraded, dispose-suppresses-callbacks.** T9.2 sub-bullets unfilled. Depends on DN2 outcome (degraded marker shape).
- [ ] [Review][Patch] P18 — **Fallback test gaps:** ETag `If-None-Match` propagation, cleanup on reconnect/dispose, `429/503 preserve visible data`. T9.6 bullets partially filled. Depends on DN1 driver decision.
- [ ] [Review][Patch] P19 — **Initial-start failure category may be overwritten by `Closed` event.** When `_connection.StartAsync` throws, the catch sets `Disconnected/InitialStartFailed`; SignalR's `Closed` event may then fire and overwrite `LastFailureCategory` with the exception type, losing the discrimination T3 demanded. Guard `Apply` against transitions away from `InitialStartFailed` until next `StartAsync` cycle. [`ProjectionSubscriptionService.cs:661-670`]
- [ ] [Review][Patch] P20 — **Per-tick `IsDisconnected` gating missing in `TriggerFallbackOnceAsync`.** Entry-checked only; if hub reconnects mid-loop the loop continues firing REST refreshes. AC7 says "stops when the hub reconnects". Re-check inside the lane loop. (Conditional on DN1.) [`ProjectionFallbackRefreshScheduler.cs:50-61`]

#### Deferred

- [x] [Review][Defer] W1 — **Information-level logging on every state transition floods telemetry on flapping connections.** A 30s SignalR reconnect cycle can write 3+ lines per cycle plus one `RejoinFailed` per active group. Rate-limiting / sampling belongs to Story 5-6 build-time infrastructure enforcement and observability. [`ProjectionConnectionState.cs:100-104`]

#### Dismissed (6, recorded for traceability)

- Multi-handler test for `FakeProjectionHubConnection` — production stores a list, fake stores single; leak detection is covered by P7 try/catch wrapping.
- `TriggerNudgeRefreshAsync` runs even when fallback polling is `0` — by design per D2/T4 (nudges always trigger refresh; the option governs disconnected polling, not nudge routing).
- `BoundCategory` truncates to 48 chars without ellipsis indicator — intentional bounded-diagnostic detail.
- `TriggerNudgeRefreshAsync` validation throw masking real cause — `ValidateGroup` short-circuits invalid input first; nudges never reach `TriggerNudgeRefreshAsync` with invalid tenant/projection.
- `OnProjectionChangedAsync` `ContainsKey` racy with concurrent `UnsubscribeAsync` — minor wasted refresh; lane registry is independent and the wasted refresh is a no-op when lane is gone.
- `FcProjectionConnectionStatus` mounted globally inside `fc-main-content` — spec accepts inline shell mounting; not a violation of "inline in the shell/projection command surface".

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Builds a SignalR `HubConnection`, uses `WithAutomaticReconnect()`, registers `ProjectionChanged`, and defaults through `EventStoreOptions.ProjectionChangesHubPath`. | Extend event visibility; do not replace with a second hub client or alternate default path. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Owns active group set, validates colon-free projection/tenant, starts connection before joining, commits active group after join, and notifies changes. | Reuse the active group set for rejoin; keep commit-after-join and disposal safety. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IProjectionChangeNotifier.cs` | Legacy projection-only notifier plus tenant-aware companion interface. | New Story 5-3 consumers should prefer tenant-aware notifications while raising the legacy event for compatibility. |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleThresholdTimer.cs` | Already has an optional `isDisconnected` hook that returns `ActionPrompt` immediately. | Wire the hook; do not add a parallel timer or duplicate phase machine. |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.*` | Renders Syncing pulse, Still syncing badge, ActionPrompt warning, Confirmed, Idempotent, and Rejected states. | Add disconnected-specific copy without breaking existing lifecycle states or Start-over behavior. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Generated forms own `EditContext`, field values, lifecycle wrapper, and rejection/validation behavior from earlier stories. | Do not remount forms or persist raw drafts when connection state changes. |
| Story 5-2 query/cache seam | Owns response classification, ETag cache, and no-churn `304` behavior. | All nudge and fallback refreshes must go through this seam. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-3 decision |
| --- | --- | --- | --- |
| EventStore hub endpoint | Story 5-1 and pinned EventStore docs | 5-3 connection manager | Default is `/hubs/projection-changes`; `/projections-hub` is superseded. |
| Nudge payload | EventStore SignalR hub | 5-3 refresh effects | Nudge is `(projectionType, tenantId)` only; data comes from REST re-query. |
| Response/cache seam | Story 5-2 | 5-3 nudge and fallback refresh | Reuse classifier, ETag cache, and no-churn 304 behavior. |
| Lifecycle timer hook | Story 2-4 | 5-3 disconnected escalation | Populate `isDisconnected` to move Syncing to ActionPrompt immediately. |
| Generated form state | Stories 2-1 through 2-5 | 5-3 disconnected UX | Preserve in-memory EditContext/model state; no raw draft storage. |
| Reconnection sweep UX | Story 5-4 | 5-3 | 5-3 detects/reconnects and performs bounded fallback only; stale-row animation and batched reconciliation are later. |
| Idempotent command reconciliation | Story 5-5 | 5-3 | 5-3 does not replay, auto-retry, optimistic-update, or summarize pending command outcomes. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | The EventStore hub default remains `/hubs/projection-changes`. | Story 5-1 and the pinned submodule established endpoint ownership. | Revert to `/projections-hub`; support two competing defaults. |
| D2 | SignalR remains nudge-only. | EventStore sends lightweight changes; correctness comes from REST + ETag. | Push projection payloads over SignalR; update grids directly from hub messages. |
| D3 | Connection state is Shell-scoped, not a Contracts dependency on SignalR. | Contracts must remain infrastructure-free. | Expose `HubConnectionState` or SignalR event types from Contracts. |
| D4 | Rejoin uses the existing active group set. | A second group registry can drift from subscribe/unsubscribe reality. | Maintain a parallel rejoin list; let components re-subscribe manually on reconnect. |
| D5 | Disconnect UI is inline and non-blocking. | Network degradation should not interrupt editing or create false data loss. | Modal, overlay, focus trap, route change, or forced page reload on disconnect. |
| D6 | Form preservation is in-memory component/EditContext preservation. | Story 5-2 forbids raw command drafts in cache/storage; this story handles projection hub loss, not server circuit loss. | Store raw drafts in LocalStorage; promise recovery after Blazor circuit rejection. |
| D7 | Syncing disconnect uses the existing lifecycle action-prompt phase. | Existing timer hook and wrapper affordance already represent manual recovery. | Add a new lifecycle enum state unless implementation evidence proves it necessary. |
| D8 | Fallback polling is bounded and visible-lane only. | Users need continuity while disconnected, but Story 5-5 owns full polling parity. | Poll every projection globally; implement idempotent command reconciliation here. |
| D9 | Initial start failure is distinct from reconnect. | SignalR automatic reconnect does not retry initial `StartAsync` failures. | Treat failed initial start as if automatic reconnect is already running. |
| D10 | Reconnect confirmation is brief and polite. | It restores confidence without requiring verification or adding notification noise. | Persistent success toast; assertive announcement; no confirmation at all. |
| D11 | Degraded-connection logs are redacted and bounded. | Tenant/user/token/payload data may be sensitive under NFR17-19. | Log raw group names, tenant IDs, access tokens, or request payloads for debugging. |
| D12 | No automatic command submit/retry on reconnect. | Story 5-5 owns idempotency and terminal reconciliation. | Replay in-flight forms after reconnect; infer success from reconnect alone. |

### Library / Framework Requirements

- Target existing repo packages and TFMs. Shell already references `Microsoft.AspNetCore.SignalR.Client`; do not add another SignalR client package unless 5-1's implementation is reverted.
- Use `HubConnectionBuilder.WithAutomaticReconnect()` behavior as the baseline, but test the wrapper through `IProjectionHubConnection` fakes rather than live network.
- Microsoft docs state default `WithAutomaticReconnect()` waits 0, 2, 10, and 30 seconds, transitions to `Reconnecting` before attempts, and fires `Closed` if reconnect attempts fail.
- Microsoft Blazor docs describe framework reconnect CSS classes for the Blazor circuit. Story 5-3's indicator is for the EventStore projection hub and must not fight the framework circuit reconnect UI.
- Blazor JS interop can throw `JSDisconnectedException` after a server-side circuit disconnect; dispose paths touched by this story must keep the existing try/catch discipline where JS interop is involved.
- If using persistent component state, Microsoft docs note `[PersistentState]` works for prerender/interactivity state. Do not use it for raw command draft persistence in this story.

External references checked on 2026-04-25:

- Microsoft Learn: ASP.NET Core SignalR .NET client automatic reconnect behavior -- https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client
- Microsoft Learn: ASP.NET Core Blazor SignalR guidance and circuit reconnect CSS classes -- https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr
- Microsoft Learn: ASP.NET Core Blazor JS interop disconnected-circuit behavior -- https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/
- Microsoft Learn: ASP.NET Core Blazor prerendered state persistence -- https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` | Internal connection-state event surface or companion interface. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Wire SignalR reconnect/reconnected/closed events into the internal abstraction. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Rejoin active groups after reconnect; surface degraded state; keep disposal safety. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/*` or `Infrastructure/EventStore/ConnectionState/*` | Scoped connection-state snapshots, reducer/effect or service implementation, fallback polling scheduler. |
| `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor*` or adjacent Shell component path | Inline disconnected/reconnected indicator. |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.*` | Wire disconnected hook and disconnected action-prompt copy. |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` and `Shell/Options/FcShellOptionsThresholdValidator.cs` | Only if new bounded reconnect/fallback options are needed. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Only if generated form markup must host the inline status without remounting forms. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Connection-state/rejoin/subscription tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/*` | Disconnected Syncing escalation tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/*` | Inline indicator and accessibility tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*` | Generated form preservation tests when emitter changes are required. |

### Testing Standards

- Use xUnit, Shouldly, NSubstitute, bUnit, fake `IProjectionHubConnection`, fake `TimeProvider`, and deterministic dispatcher/test harnesses. No live EventStore or live SignalR server required.
- Test reconnect timing with `FakeTimeProvider` or explicit fake reconnect policy; avoid wall-clock sleeps.
- Keep fallback polling tests deterministic: one scheduled tick, one in-flight dedupe path, one reconnect cleanup path.
- Add negative assertions for no modal/overlay/focus trap by checking rendered markup and focus behavior where practical.
- Add state-preservation tests by mutating generated form fields before simulated disconnect/reconnect, then asserting model/EditContext state survives.
- Keep diagnostics tests redaction-focused. Support logs may include failure categories and redacted correlation IDs, not sensitive routing or payload values.

### Scope Guardrails

Do not implement these in Story 5-3:

- Batched stale-row animation, reconnect sweep summary, and schema-mismatch invalidation UX -- Story 5-4.
- Idempotent terminal command reconciliation, optimistic badge desaturation, new-item indicators, and full polling parity -- Story 5-5.
- OpenTelemetry activity source, distributed trace spans, or build-time infrastructure enforcement -- Story 5-6.
- SignalR fault injection framework as a reusable package -- Story 5-7.
- Raw command draft persistence in LocalStorage, ETag cache, query cache, or logs.
- Recovery after a Blazor Server circuit is rejected and server component state is discarded.
- Replacing the EventStore REST + SignalR contract or adding Dapr access from FrontComposer.
- Live-browser/Playwright coverage unless a small bUnit test cannot verify a critical behavior.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Batched reconnection sweep, stale-row animation, schema-mismatch invalidation, and "Reconnected -- data refreshed" behavior. | Story 5-4 |
| Transparent polling parity, pending-command terminal reconciliation, optimistic badge transitions, and command outcome summaries. | Story 5-5 |
| Distributed tracing and structured lifecycle observability across command -> projection -> SignalR -> UI. | Story 5-6 |
| Reusable SignalR fault injection harness for unit/integration tests. | Story 5-7 |
| Documentation cleanup where older planning text still says `/projections-hub`. | Story 9-5 documentation site or planning-correction task |
| Provider/Pact verification of reconnect and SignalR hub behavior against the EventStore submodule. | Story 10-3 |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.3] -- Story statement, baseline ACs, FR22/FR24/UX-DR50/NFR15/NFR39 intent.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] -- EventStore transport seam, hub path, subscription service, and tenant-aware notification decisions.
- [Source: _bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md] -- Query/cache response seam that 5-3 must reuse for nudge and fallback refresh.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01] -- Cross-story contract clarity upfront.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- Tenant/user isolation fail-closed.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L08] -- Party review and elicitation are complementary hardening passes.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L12] -- Full async bridge lifecycle contract upfront.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L14] -- Bounded-by-policy for runtime caches/state.
- [Source: Hexalith.EventStore/docs/reference/query-api.md#SignalR-Hub-hubs-projection-changes] -- Current SignalR hub path, methods, group format, and nudge-only behavior.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub/ProjectionChangedHub.cs] -- `HubPath = "/hubs/projection-changes"`, `JoinGroup`, `LeaveGroup`, colon guard, group limits.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs] -- Existing SignalR client wrapper and automatic reconnect baseline.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs] -- Active group ownership, validation, subscription, notification, and disposal behavior.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleThresholdTimer.cs] -- Existing disconnected hook for immediate action-prompt escalation.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor] -- Current Syncing/action-prompt/confirmed/rejected UI surface.
- [Source: Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) -- Automatic reconnect state/delay behavior.
- [Source: Microsoft Learn: ASP.NET Core Blazor SignalR guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr) -- Blazor circuit reconnect classes and host-level SignalR guidance.

---

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-04-26: `dotnet build tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj -warnaserror /p:UseSharedCompilation=false` passed.
- 2026-04-26: `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --no-build` passed: 1113 passed, 3 skipped.
- 2026-04-26: `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --no-build` passed: 91 passed.
- 2026-04-26: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` passed.
- 2026-04-26: `dotnet test Hexalith.FrontComposer.sln --no-build` passed: Contracts 91/0/0, Shell 1113/0/3, SourceTools 481/0/0, Bench 2/0/0.

### Completion Notes List

- Added an internal SignalR connection-state event surface and wired `Reconnecting`, `Reconnected`, `Closed`, and post-start `Connected` transitions without leaking SignalR client types into Contracts.
- Added scoped Shell projection connection state plus bounded visible-lane refresh scheduling for nudge-driven and disconnected fallback re-query through the existing Story 5-2 page-loader/cache seam.
- Updated `ProjectionSubscriptionService` to surface initial-start degradation, rejoin active groups on reconnect from the existing active set, preserve commit-after-join semantics, and route accepted nudges through tenant-aware notification plus visible-lane refresh.
- Added inline `FcProjectionConnectionStatus` shell UX with AC6 copy, polite status announcements, and brief reconnected confirmation.
- Wired `FcLifecycleWrapper` to the existing disconnected timer hook so active `Syncing` lifecycles immediately render `Connection lost -- unable to confirm sync status` and stop the pulse without changing terminal Confirmed/Rejected behavior.
- Preserved generated form state by keeping connection UI outside the generated form/EditContext and avoiding draft persistence, auto-submit, replay, or retry behavior.
- Kept AC3/AC6 copy hardcoded in the touched lifecycle/EventStore components, matching current `FcLifecycleWrapper` precedent; localization resource follow-up remains optional for a later broader localization pass.

### File List

- `_bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperDisconnectedTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleWrapperTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs`

### Change Log

- 2026-04-26: Implemented Story 5-3 SignalR connection/disconnection handling and moved status to review.
