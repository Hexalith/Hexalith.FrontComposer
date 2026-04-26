# Story 5.4: Reconnection, Reconciliation & Batched Updates

Status: ready-for-dev

> **Epic 5** -- Reliable Real-Time Experience. **FR25-FR27** reconnect catch-up, stale-data reconciliation, batched update feedback, and schema-mismatch safety after Stories 5-1 through 5-3. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, **L12**, and **L14**.

---

## Executive Summary

Story 5-4 turns a successful EventStore projection-hub reconnect into one calm, deterministic catch-up pass:

- Reuse Story 5-3's projection connection-state service and active subscription ownership. 5-4 does not create a second SignalR client, second subscribed-group registry, or second disconnected banner.
- Reuse Story 5-2's `EventStoreQueryClient`, response classifier, `IETagCache`, `LoadPageNotModifiedAction`, and badge count reader for every catch-up query. Reconciliation is ETag-conditioned REST, never projection data pushed over SignalR.
- On `Reconnected`, create one monotonic reconnect epoch, snapshot active groups and currently visible projection/count lanes, rejoin each snapshotted group at most once for that epoch, then reconcile only eligible visible lanes. Hidden pages, unrelated projections, and background prefetch remain out of scope.
- If a lane returns `304 Not Modified`, clear reconnect state silently. If a lane returns `200 OK`, update that lane and trigger one batched visual sweep for the changed visible rows/lane, not individual per-row flashing.
- Keep the UX quiet: show `Reconnected -- data refreshed` for 3 seconds only when changes were found. If no changes were found, clear the header status without toast or message.
- Treat schema mismatch as a user-safe degraded state. Log a clear diagnostic, invalidate affected ETag cache entries, and render `This section is being updated` instead of empty/stale data.
- Preserve 5-5 boundaries. This story does not resolve pending command terminal outcomes, replay commands, optimistic-update badges, show command outcome summaries, or implement full polling parity.

The intended implementation shape is a Shell-scoped reconciliation coordinator that reacts to the 5-3 reconnect event, snapshots currently visible lanes, rejoins active groups through `ProjectionSubscriptionService`, issues bounded ETag-conditioned query refreshes through existing 5-2 seams, records a short-lived "changed lane" marker, and lets UI components render a single sweep + polite status message.

---

## Story

As a business user,
I want the application to seamlessly recover after a network interruption by rejoining subscriptions, catching up on missed changes, and showing me what changed in one smooth sweep,
so that I trust the data I see is current without needing to manually refresh.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The EventStore projection hub reconnects after a previously connected session | The 5-3 connection state transitions to `Reconnected` | The client starts one monotonic reconnect epoch, snapshots active projection groups from `ProjectionSubscriptionService`, issues at most one rejoin request per snapshotted `(projectionType, tenantId)` for that epoch, ignores/cancels superseded epoch results, does not duplicate group state outside `ProjectionSubscriptionService`, treats failed rejoins as degraded instead of silently active, and only starts reconciliation after rejoin attempts complete or are explicitly classified as failed. |
| AC2 | Visible projection pages, visible action-queue badge/count lanes, or other subscribed visible lanes exist | Reconnection reconciliation starts | Each eligible visible lane issues an ETag-conditioned REST query through Story 5-2 seams, using framework-controlled tenant/user/discriminator values; "visible" means currently mounted and user-visible in the active composer view at epoch snapshot time; hidden, collapsed, background-tab, offscreen-virtualized, newly visible after snapshot, and unsubscribed lanes are skipped/deferred to normal loading; `304 Not Modified` leaves visible state unchanged; `200 OK` updates from the response and marks the lane for one batched sweep only when a reducer-visible data delta is successfully applied. |
| AC3 | Catch-up queries return changed projection data | Stale rows or a changed visible lane are identified | All changed visible rows receive a single coordinated CSS sweep in the same render window; no row-by-row flash cascade occurs; if row-level identity/diff is unavailable, the visible lane receives a single lane-level sweep; the sweep is a non-flashing highlight transition of at most 700 ms, causes no layout shift, does not repeat for the same epoch, and `prefers-reduced-motion: reduce` makes the update instantaneous without sweep animation. |
| AC4 | Reconciliation completes and at least one visible lane changed | `FcSyncIndicator` or the equivalent Shell header status processes the result | The header reconnecting state clears and a `FluentMessageBar` with Info intent displays `Reconnected -- data refreshed`, has `role="status"` and `aria-live="polite"`, coalesces announcements once per reconnect epoch instead of per row/lane, and auto-dismisses after 3 seconds without stealing focus. |
| AC5 | Reconciliation completes and no visible lane changed | The result is processed | The header reconnecting state clears silently, no toast/message is shown, no row sweep marker is added, no badge animation fires, and no synthetic "success" timestamp or loading churn is produced. |
| AC6 | The Shell header is reconnecting or reconciling | The user is navigating or editing during degraded network recovery | Header text shows `Reconnecting...` while reconnecting and `Refreshing data...` while reconciliation is running; both use subtle pulse styling, `aria-live="polite"`, and no modal, overlay, focus trap, pointer blocking, route change, or form remount. |
| AC7 | A schema evolution mismatch is detected at startup, during query deserialization, cache compatibility validation, or reconciliation | Projection type or payload shape cannot be safely consumed | A clear structured diagnostic is logged without raw tenant/user/token/payload data; after a classified incompatible cache entry or incompatible `200 OK` payload, all ETag cache entries for the affected projection type/discriminator family are invalidated best-effort; a `304 Not Modified` never triggers invalidation or state mutation; the affected lane/section renders `This section is being updated`; unaffected lanes, badges, subscriptions, and cached data continue normally; stale data is not shown as current and empty state is not used as a false success. |
| AC8 | Schema compatibility tests run | Event/projection payload contracts evolve within the same major version | New code can deserialize prior-minor fixtures for supported projection payloads; old-code tolerance for unknown fields is represented by forward-compatibility fixture tests or documented serializer options; incompatible payloads produce the 5-4 schema-mismatch path instead of silent stale UI. |
| AC9 | Tests run | Shell services, reducers/effects, components, and cache/schema seams execute | Coverage proves reconnect epoch supersession, ordered at-most-once active group rejoin, visible-lane-only reconciliation, observable 304 no-churn, changed-lane sweep, reduced-motion behavior, 3-second auto-dismiss, silent no-change completion, header accessibility, schema mismatch invalidation, disposal/cancellation cleanup, and redacted diagnostics. |

---

## Tasks / Subtasks

- [ ] T1. Extend the 5-3 connection-state surface with a reconciliation trigger (AC1, AC6, AC9)
  - [ ] Read Story 5-3 first. If 5-3 has not yet been implemented, implement only against the story's promised seam names after verifying current code; do not bypass or replace the 5-3 service.
  - [ ] Expose a Shell-scoped `Reconnected` transition with enough metadata for a coordinator to start exactly one reconciliation pass per reconnect epoch.
  - [ ] Treat reconnect/rejoin/reconcile/sweep results as epoch-scoped. Results from superseded epochs must be discarded and must not update cache, header state, lane markers, diagnostics, or visible data.
  - [ ] Track reconnect epoch/correlation in memory only. Do not persist reconnect history to LocalStorage or the ETag cache.
  - [ ] Bound retained state to the latest active reconciliation snapshot plus latest user-visible status per L14.
  - [ ] Keep diagnostics structured and redacted: no raw tenant IDs, user IDs, group names, access tokens, query payloads, cache payloads, or ProblemDetails bodies.

- [ ] T2. Rejoin active projection groups through `ProjectionSubscriptionService` (AC1, AC9)
  - [ ] Extend `IProjectionHubConnection` or its companion interface only as needed to surface `Reconnecting`, `Reconnected`, and `Closed` events without leaking SignalR types into Contracts.
  - [ ] Reuse `ProjectionSubscriptionService`'s active group set as the single source of truth. Do not introduce a parallel "groups to rejoin" collection in UI state.
  - [ ] On reconnect, snapshot active groups for the epoch and call `JoinGroup` at most once per snapshotted active group after the hub reports connected/reconnected. Groups added after the snapshot use the normal subscription path instead of the reconnect batch.
  - [ ] Preserve commit-after-join semantics from Story 5-1 and Story 5-3. A failed rejoin remains registered in `ProjectionSubscriptionService`, is marked `DegradedRejoinFailed`, is excluded from successful reconciliation for that epoch, and is retried by the next explicit reconnect/reconcile cycle or subscribe call, not marked active by optimism.
  - [ ] Handle duplicate subscribe/unsubscribe during rejoin, disposal during rejoin, failed `JoinGroup`, and a nudge arriving mid-rejoin without callbacks after disposal.

- [ ] T3. Build a visible-lane reconciliation coordinator (AC2, AC5, AC9)
  - [ ] Create a Shell service/effect under `Shell/State/ReconnectionReconciliation/` or the adjacent EventStore state folder that owns reconciliation orchestration only.
  - [ ] Consume existing visible lane sources: `LoadedPageState.LaneByKey` / current DataGrid view keys, action-queue badge registrations, and any 5-3 visible projection lane registry. Add a small visible-lane registry only if no reliable source exists.
  - [ ] Snapshot visible lanes at reconciliation start. Eligible visible lanes are mounted and user-visible in the active composer view. Do not chase lanes that become visible later in the same pass; they can load normally through existing page/badge flows.
  - [ ] Bound the number of lanes reconciled per pass with an option-backed cap if the existing visible-lane count is not already bounded.
  - [ ] Deduplicate by `(tenantId, projectionType, discriminator/laneKey)` so a badge and page sharing a projection do not issue duplicate catch-up work unless their discriminators differ.
  - [ ] Stop promptly on reconnect epoch superseded, unsubscribe, component disposal, tenant/user loss, or cancellation.

- [ ] T4. Route catch-up refreshes through Story 5-2 query/cache seams (AC2, AC5, AC9)
  - [ ] Use `IQueryService` / `EventStoreQueryClient`, `IProjectionPageLoader`, `IActionQueueCountReader`, `IETagCache`, and existing `QueryRequest.CacheDiscriminator` policy. Do not add a second HTTP client, classifier, or ETag cache.
  - [ ] `304 Not Modified` must dispatch the existing no-change path (`LoadPageNotModifiedAction` or equivalent) and must not mutate `LoadedPageState.PagesByKey`, `TotalCountByKey`, `LastElapsedMsByKey`, badge count, refresh timestamps, or success/toast state.
  - [ ] `200 OK` updates the normal page/badge state through existing success paths and returns enough result metadata to mark the lane changed only when a reducer-visible data delta is successfully applied. Schema-mismatch/degraded outcomes do not display `Reconnected -- data refreshed` unless another visible lane actually changed.
  - [ ] 401/403/404/429/503 and protocol-drift responses preserve currently visible data and surface degraded/failure state consistently with 5-2/5-3; they must not clear rows as an empty success.
  - [ ] Respect cache fail-closed rules for missing/blank/colon-containing tenant/user/discriminator values. If a lane cannot be keyed safely, perform a normal uncached query only when the query itself is safe; otherwise mark degraded.

- [ ] T5. Implement the batched sweep marker and reduced-motion styling (AC3, AC9)
  - [ ] Prefer a small Fluxor state slice such as `ReconciliationSweepState` keyed by view/lane and reconnect epoch, with a short TTL controlled by `TimeProvider`.
  - [ ] Apply one sweep marker per changed lane in the same render cycle. Do not schedule one timer per row; use one shared sweep expiry mechanism bounded by the number of changed lanes.
  - [ ] If existing item-key accessors can identify row identity and a lightweight in-memory comparison is available, mark changed rows. If not, mark the visible lane as changed and sweep its visible rows as a single batch.
  - [ ] Keep comparison transient and in memory. Do not persist row hashes, serialized rows, business data, or user data to storage or logs.
  - [ ] Add scoped CSS next to the affected component(s). Use `@media (prefers-reduced-motion: reduce)` to disable animation and render the final state immediately while leaving data application, batching, and the 3-second message duration unchanged.
  - [ ] Avoid decorative effects. The sweep should be subtle, consistent with Fluent info styling, and must not rely on color alone.

- [ ] T6. Add `FcSyncIndicator` / header reconnect + reconciliation UX (AC4, AC5, AC6, AC9)
  - [ ] Reuse the existing Shell header/status area if one exists; otherwise add a small Shell component such as `FcSyncIndicator` under `Components/Layout/` or `Components/EventStore/`.
  - [ ] Show `Reconnecting...` during 5-3 reconnecting state and `Refreshing data...` while the 5-4 reconciliation pass is active.
  - [ ] When changes were found, render Info `FluentMessageBar` copy exactly `Reconnected -- data refreshed` and auto-dismiss after 3 seconds using `TimeProvider` in tests.
  - [ ] When no changes were found, clear status silently. Do not show "no changes" toast or success message.
  - [ ] Apply state precedence consistently: schema mismatch for an affected lane/section wins locally over refreshed sweep; global header state uses reconnecting before reconciling before refreshed/idle; stale epoch results never re-open a cleared status.
  - [ ] Set `role="status"` and `aria-live="polite"` on status and toast content. Do not use assertive announcements unless later accessibility review changes the policy.
  - [ ] Keep the indicator non-blocking: no modal, overlay, focus trap, scroll jump, route change, form remount, or pointer blocking.
  - [ ] Add EN/FR resource keys when touching the localized Shell resource path: `ReconnectStatusText`, `ReconciliationStatusText`, `ReconnectedDataRefreshedText`, and `SectionUpdatingText`. If the touched path is not localized yet, keep the exact English copy inline and record the resource follow-up.

- [ ] T7. Add schema mismatch detection and cache invalidation policy (AC7, AC8, AC9)
  - [ ] Reuse Story 5-2 `ETagCacheEntry.FormatVersion`, `PayloadVersion`, and discriminator metadata before adding new schema metadata.
  - [ ] Define a Shell-side `ProjectionSchemaMismatchException` or result state only if existing `QueryFailureException` cannot express the condition without ambiguity.
  - [ ] Detect mismatch from cache compatibility rejection, query deserialization failure, explicit projection payload version mismatch, or a configured projection schema hash mismatch if that metadata exists.
  - [ ] Invalidate all ETag cache entries for the affected projection type/discriminator family best-effort after an incompatible cache entry or incompatible `200 OK` payload. A `304 Not Modified` never invalidates or mutates cache/state.
  - [ ] Render the exact user copy `This section is being updated` in the affected view/badge region; do not show empty state, stale rows, raw exception details, or a global failure state when only one lane is affected.
  - [ ] Log one structured diagnostic with projection type or a redacted/hash-safe identifier only if the existing logging policy allows it. Do not log payload bodies or user data.

- [ ] T8. Add bidirectional schema compatibility fixtures (AC8, AC9)
  - [ ] Add fixture payloads for current and prior-minor projection shapes where the repo has shipped projection contracts. If no versioned fixture archive exists, create a minimal story-owned fixture folder and document ownership.
  - [ ] Prove current code reads prior-minor payloads and ignores unknown forward fields according to the repo's `System.Text.Json` web/default options.
  - [ ] Add one incompatible fixture that must produce the schema-mismatch path and cache invalidation, not silent empty data.
  - [ ] Keep fixture payloads synthetic and non-PII. Do not copy real tenant/customer data into test fixtures.
  - [ ] Defer full event-envelope version negotiation to Story 5-6 or Story 9-4 if it requires architecture policy beyond projection payload compatibility.

- [ ] T9. Tests and verification (AC1-AC9)
  - [ ] Connection/rejoin tests: reconnect event starts one pass, ordered fake-hub log proves at-most-one join per active group per epoch, failed rejoin is degraded, duplicate subscribe/unsubscribe races are safe, stale epoch callbacks are ignored, and disposal cancels the pass.
  - [ ] Reconciliation coordinator tests: visible-lane snapshot, hidden/collapsed/offscreen lane skip, newly visible lane deferred, lane cap, dedupe, cancellation, tenant/user fail-closed, and superseded reconnect epoch cleanup.
  - [ ] Query/cache tests: ETag validators used, `304` no-churn, `200` changed-lane marker only after reducer-visible data delta, 401/403/404/429/503 preserve visible state, protocol drift fails loudly without empty success.
  - [ ] Reducer/effect tests: sweep markers are batch-scoped, TTL clears markers, one shared expiry mechanism replaces per-row timer fan-out, reduced-motion path produces immediate final state.
  - [ ] Component/a11y tests: header `Reconnecting...`, `Refreshing data...`, Info message copy, `role="status"`, `aria-live="polite"`, live-region announcements coalesced once per epoch, 3-second auto-dismiss, silent no-change completion, no modal/overlay/focus trap.
  - [ ] Schema tests: backward fixture read, unknown forward field tolerance, incompatible fixture triggers `This section is being updated`, ETag invalidation, and redacted diagnostic.
  - [ ] Shared deterministic harness: fake hub, fake visible-lane registry, fake query/cache, fake diagnostics sink, fake time, and an explicit async drain/advance helper so timer/cancellation continuations are deterministic.
  - [ ] Regression suite: run targeted Shell/EventStore/DataGrid/ETag tests plus `dotnet build -warnaserror` unless the current working tree contains unrelated failing dev work.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` | Internal SignalR abstraction with `IsConnected`, `OnProjectionChanged`, `StartAsync`, `JoinGroupAsync`, `LeaveGroupAsync`, `StopAsync`, and disposal. | Extend for reconnect events if needed; do not expose SignalR types through Contracts. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Builds `HubConnection` with `WithAutomaticReconnect()` and maps `ProjectionChanged`, `JoinGroup`, and `LeaveGroup`. | Keep `/hubs/projection-changes` and automatic reconnect baseline; initial start failure is still distinct from reconnect. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Owns active group set, commit-after-join, tenant-aware notifier, disposal, and group validation. | Reuse active group set for rejoin; do not add a second group registry. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Central query path with response classifier, ETag cache integration, `304` from cache, and protocol-drift retry. | Reconciliation refreshes must go through this path. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` | Dispatches `LoadPageNotModifiedAction` for Story 5-2 `304` no-change. | Preserve no-churn semantics on reconnect. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs` | Resolves 304 TCS without mutating page/total/elapsed state. | Do not convert 304 into a synthetic success for reconnect UX. |
| `src/Hexalith.FrontComposer.Shell/State/ETagCache/*` | Bounded ETag cache with format/payload compatibility metadata. | Reuse for schema mismatch detection and invalidation; do not add parallel cache storage. |
| `src/Hexalith.FrontComposer.Shell/Badges/EventStoreActionQueueCountReader.cs` and `BadgeCountService.cs` | Badge count refresh uses EventStore query/cache and no-change semantics. | Include visible badge/count lanes in reconciliation without forcing badge animation on 304. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-4 decision |
| --- | --- | --- | --- |
| EventStore hub reconnect state | Story 5-3 | 5-4 coordinator | Reconnect event triggers one reconciliation pass per reconnect epoch. |
| Active SignalR groups | Story 5-1/5-3 `ProjectionSubscriptionService` | 5-4 rejoin | Existing active group set is the only source of truth. |
| ETag query/cache seam | Story 5-2 | 5-4 reconciliation | All catch-up refreshes use existing `IQueryService`, cache discriminator, and response classifier. |
| DataGrid no-change path | Story 5-2 | 5-4 UX | `304` remains no-churn; no synthetic success/toast/animation. |
| Disconnected inline UX | Story 5-3 | 5-4 header state | 5-4 adds reconciling/refreshed states, not a second disconnect banner. |
| Command idempotency/outcomes | Story 5-5 | 5-4 | 5-4 does not reconcile pending command terminal outcomes or replay commands. |
| Schema evolution policy | Architecture + Story 5-2 cache metadata | 5-4 schema mismatch UX | 5-4 handles projection payload compatibility at UI/query boundary; broad event-version policy remains later architecture work if needed. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Reconnection reconciliation starts from the 5-3 reconnect event. | Keeps connection detection and user-facing disconnect posture in one place. | Poll connection state from every component; add another hub watcher. |
| D2 | Active group rejoin reuses `ProjectionSubscriptionService` state. | A second group list can drift under subscribe/unsubscribe races. | Let components manually re-subscribe; maintain UI-owned group registry. |
| D3 | Reconciliation is visible-lane-only. | Users need current visible data without hidden-page query bursts. | Requery every known projection or prefetch hidden pages. |
| D4 | `304 Not Modified` is silent success with no UI churn. | The best reconnect outcome is invisible when data did not change. | Show a "Reconnected" toast for every reconnect; dispatch a synthetic success state. |
| D5 | Changed data gets one batched sweep per lane/epoch. | Calm resolution and avoids noisy per-row flashes. | Animate each row as its query returns; use long decorative animations. |
| D6 | Reduced motion disables sweep animation. | Accessibility and user preference override decoration. | Always animate; hide changes entirely. |
| D7 | Schema mismatch renders a degraded section message and invalidates affected ETags. | Empty/stale data would falsely imply correctness. | Show empty state; keep stale rows; clear the whole cache. |
| D8 | Schema compatibility is tested with synthetic fixtures. | Prevents silent projection drift without real customer data. | Depend only on live EventStore tests; copy production payloads into tests. |
| D9 | Reconnect info message appears only when changes were found. | Avoids notification fatigue and aligns UX-DR39/UX-DR51. | Always show a toast; never confirm changed-data refresh. |
| D10 | Reconciliation state is bounded and transient. | Reconnect history and row comparisons can leak memory if retained. | Store unlimited reconciliation events or row snapshots. |
| D11 | No pending-command reconciliation in 5-4. | Story 5-5 owns idempotent terminal outcomes and optimistic UI. | Infer command success from projection changes; replay commands on reconnect. |
| D12 | Diagnostics are redacted and bounded. | Reconnect paths touch tenant, user, route, cache, and projection payload data. | Log raw groups, tenant/user IDs, query payloads, or cache bodies for debugging. |
| D13 | Reconnect/rejoin/reconcile/sweep work is scoped to a monotonic reconnect epoch. | Prevents stale async work from a superseded reconnect from mutating cache, header state, lane markers, diagnostics, or visible data. | Let late callbacks update state if their individual lane result looks valid. |
| D14 | Active group rejoin is at-most-once per snapshotted group per epoch, not an absolute distributed exactly-once guarantee. | The client can prove command issuance; server-side group membership remains idempotent SignalR behavior. | Promise global exactly-once semantics across server reconnect state. |
| D15 | Visible lanes are mounted and user-visible in the active composer view at epoch snapshot time. | Makes visible-lane-only reconciliation testable and avoids background fetch bursts. | Treat previously loaded, collapsed, background-tab, or newly mounted lanes as part of the same pass. |
| D16 | Changed means a compatible `200 OK` successfully applies a reducer-visible data delta. | Keeps `304`, semantic no-op responses, schema mismatch, and degraded failures from producing false success UI. | Treat any `200 OK` or cache refresh as user-visible changed data. |
| D17 | Schema mismatch is lane/section-scoped and never triggered by `304 Not Modified`. | Preserves no-churn guarantees and avoids one incompatible lane becoming a global failure. | Invalidate cache or replace the whole page whenever any mismatch is suspected. |

### Library / Framework Requirements

- Target existing repo package lines: .NET 10, Blazor, Fluxor, Fluent UI Blazor, xUnit, bUnit, Shouldly, NSubstitute, and the current `Microsoft.AspNetCore.SignalR.Client` reference already in Shell.
- Microsoft Learn states `WithAutomaticReconnect()` configures default reconnect delays of 0, 2, 10, and 30 seconds, raises `Reconnecting` before attempts, raises `Reconnected` after success, and raises `Closed` if reconnect attempts fail. Initial `StartAsync` failures are not retried by automatic reconnect.
- Use component-scoped `.razor.css` for new indicator/sweep styling where possible. CSS isolation scopes component CSS at build time and avoids global style collisions.
- Use `TimeProvider` or existing fake-time test helpers for 3-second auto-dismiss and sweep TTL tests. Avoid wall-clock sleeps.
- Use `System.Text.Json` web/default options already used by EventStore query/cache code. Do not introduce Newtonsoft.Json for schema fixtures or compatibility checks.

External references checked on 2026-04-26:

- Microsoft Learn: ASP.NET Core SignalR .NET client -- automatic reconnect behavior and event order: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client
- Microsoft Learn: `HubConnectionBuilderExtensions.WithAutomaticReconnect` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.signalr.client.hubconnectionbuilderextensions.withautomaticreconnect
- Microsoft Learn: ASP.NET Core Blazor CSS isolation: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` | Reconnect event surface or companion interface for Shell-only SignalR state. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Wire SignalR `Reconnecting`, `Reconnected`, and `Closed` events into the abstraction. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Rejoin active groups exactly once after reconnect and surface degraded rejoin failures. |
| `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/*` | Reconciliation coordinator, status snapshots, visible-lane model, sweep state, reducers/effects. |
| `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcSyncIndicator.razor*` or existing layout component path | Header reconnect/reconciling/refreshed status UI. |
| `src/Hexalith.FrontComposer.Shell/Components/DataGrid/*` or adjacent generated-view host files | Batched sweep marker rendering and scoped CSS if the existing grid surface owns row markup. |
| `src/Hexalith.FrontComposer.Shell/State/ETagCache/*` | Bounded projection-family invalidation only if current cache seam cannot invalidate safely. |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` and `Shell/Options/FcShellOptionsThresholdValidator.cs` | Only if new caps/TTL/durations are needed; keep append-only and validated. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Reconnect event and rejoin behavior tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/*` | Coordinator, visible-lane, dedupe, sweep, schema mismatch tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/*` | `FcSyncIndicator` copy, auto-dismiss, a11y, no-blocking behavior. |
| `tests/Hexalith.FrontComposer.Shell.Tests/TestData/SchemaCompatibility/*` | Synthetic prior/current/forward/incompatible payload fixtures. |

### Testing Standards

- Use xUnit, Shouldly, NSubstitute, bUnit, fake `IProjectionHubConnection`, fake visible-lane registry, fake `IQueryService`, fake `IETagCache`, and `FakeTimeProvider` or repo-equivalent fake time.
- No live EventStore, live SignalR server, browser, Playwright, or network is required for 5-4.
- Keep tests deterministic: no real delays, no wall-clock animation timing, no random row order.
- Test accessibility by rendered markup and focus behavior where practical: `role="status"`, `aria-live="polite"`, no modal/overlay/focus trap.
- Test reduced motion through CSS presence/snapshot or class behavior; do not depend on browser media-query execution in bUnit.
- Keep schema fixtures small, synthetic, and redacted.

### Scope Guardrails

Do not implement these in Story 5-4:

- SignalR initial disconnected banner, form-state preservation, or Syncing timeout escalation -- Story 5-3.
- Pending-command terminal reconciliation, duplicate command/idempotent outcomes, optimistic badge desaturation, new-item indicators, or command outcome summaries -- Story 5-5.
- Full transparent polling parity when SignalR is unavailable -- Story 5-5.
- Build-time infrastructure enforcement, distributed tracing, and OpenTelemetry spans -- Story 5-6.
- Reusable SignalR fault injection harness package -- Story 5-7.
- Raw command draft persistence, LocalStorage row snapshots, persisted row hashes, or cache of user-entered form/search/filter values.
- Global EventStore event-version negotiation or domain event migration policy unless an existing metadata seam already supports it. Keep 5-4 at projection payload/query boundary.
- Replacing the EventStore REST + SignalR contract, adding Dapr access from FrontComposer, or pushing full projection payloads over SignalR.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Pending-command terminal reconciliation, optimistic badges, new item indicators, transparent polling parity, and command outcome summaries. | Story 5-5 |
| Build-time infrastructure enforcement and full observability traces across command -> projection -> SignalR -> UI. | Story 5-6 |
| Reusable SignalR fault-injection test harness beyond story-local fakes. | Story 5-7 |
| Full event-envelope schema governance, diagnostic ID policy, and AOT-friendly schema hash enforcement if projection fixtures are not enough. | Story 9-4 or Story 5-6 |
| Provider/Pact verification of reconnect/rejoin and query/ETag behavior against EventStore. | Story 10-3 |
| Documentation cleanup where older planning text still says `/projections-hub`. | Story 9-5 documentation site or planning-correction task |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.4] -- story statement, baseline ACs, FR25-FR27, UX-DR5/UX-DR39/UX-DR51, NFR40-NFR42/NFR48-NFR50.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] -- EventStore endpoint ownership, active group semantics, SignalR nudge-only contract.
- [Source: _bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md] -- response classifier, ETag cache, 304 no-change behavior, cache metadata compatibility.
- [Source: _bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md] -- expected connection-state service, reconnect UX, fallback boundaries, form-preservation boundaries.
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns] -- schema evolution and reliability testability expectations.
- [Source: _bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md#Reconnection-Reconciliation] -- ETag-conditioned visible projection sweep and silent no-change reconnect behavior.
- [Source: _bmad-output/planning-artifacts/ux-design-specification/desired-emotional-response.md#Reconnection-Reconciliation-UX] -- single subtle animation sweep and 3-second info toast.
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Global-content-area-top] -- MessageBar placement and Info auto-dismiss convention.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01] -- cross-story contract clarity upfront.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- tenant/user isolation fail-closed.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L08] -- party review and elicitation are complementary hardening passes.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L14] -- runtime cache/state must be bounded by policy.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs] -- current active-group and nudge ownership.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs] -- current query/cache/classifier implementation.
- [Source: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs] -- current 304 not-modified effect path.
- [Source: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs] -- current 304 reducer no-churn behavior.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub/ProjectionChangedHub.cs] -- `JoinGroup`, `LeaveGroup`, and nudge-only SignalR hub behavior.
- [Source: Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) -- automatic reconnect event behavior.
- [Source: Microsoft Learn: ASP.NET Core Blazor CSS isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation) -- component-scoped CSS guidance.

---

## Dev Agent Record

### Party-Mode Review

- Date/time: 2026-04-26T05:06:45.7049555+02:00
- Selected story key: `5-4-reconnection-reconciliation-and-batched-updates`
- Command/skill invocation used: `/bmad-party-mode 5-4-reconnection-reconciliation-and-batched-updates; review;`
- Participating BMAD agents: Winston (System Architect), John (Product Manager), Sally (UX Designer), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - P1 reconnect concurrency needed an explicit monotonic epoch contract and stale-result discard rule.
  - P1 active group rejoin needed an at-most-once-per-epoch client contract instead of vague global exactly-once language.
  - P1/P2 schema mismatch needed lane-scoped behavior, recovery boundaries, and no conflict with `304` no-churn semantics.
  - P2 visible-lane-only reconciliation needed a precise eligibility definition and snapshot timing.
  - P2 changed-data UI needed a deterministic predicate before showing `Reconnected -- data refreshed`.
  - P2 batched sweep, reduced-motion, localization, live-region, and test-harness requirements needed measurable implementation constraints.
- Changes applied:
  - Clarified AC1/AC9 and T1/T2/T9 around reconnect epochs, stale callback discard, and ordered fake-hub proof of at-most-once joins.
  - Clarified AC2/T3 around mounted, user-visible active-view lanes and deferring hidden/collapsed/background/offscreen/newly visible lanes to normal loading.
  - Clarified AC2/T4/D16 so "changed" requires a compatible `200 OK` with a reducer-visible data delta.
  - Clarified AC3/T5 around one non-flashing sweep, max 700 ms, no layout shift, no per-row timer fan-out, and reduced-motion behavior.
  - Clarified AC4/T6 around coalesced polite live-region announcements and localization resource keys.
  - Clarified AC7/T7/D17 around lane-scoped schema mismatch handling, affected-family ETag invalidation, and `304` never invalidating or mutating.
  - Added binding decisions D13-D17 for epoch ownership, at-most-once client rejoin, visible-lane definition, changed-data predicate, and schema-mismatch scoping.
- Findings deferred:
  - Product/UX may later add a support/debug affordance for schema mismatch; 5-4 keeps only the safe user copy plus structured diagnostics.
  - Architecture may later move broad event-envelope version governance to Story 9-4 or Story 5-6; 5-4 remains projection payload/query-boundary compatibility.
  - Numeric defaults for any new option-backed lane cap or concurrency cap remain implementation choices constrained by the existing options validator pattern.
- Final recommendation: ready-for-dev

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
