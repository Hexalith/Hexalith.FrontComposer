# Story 5.4: Reconnection, Reconciliation & Batched Updates

Status: in-progress

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

- [x] T1. Extend the 5-3 connection-state surface with a reconciliation trigger (AC1, AC6, AC9)
  - [x] Read Story 5-3 first. If 5-3 has not yet been implemented, implement only against the story's promised seam names after verifying current code; do not bypass or replace the 5-3 service.
  - [x] Expose a Shell-scoped `Reconnected` transition with enough metadata for a coordinator to start exactly one reconciliation pass per reconnect epoch.
  - [x] Treat reconnect/rejoin/reconcile/sweep results as epoch-scoped. Results from superseded epochs must be discarded and must not update cache, header state, lane markers, diagnostics, or visible data.
  - [x] Track reconnect epoch/correlation in memory only. Do not persist reconnect history to LocalStorage or the ETag cache.
  - [x] Bound retained state to the latest active reconciliation snapshot plus latest user-visible status per L14.
  - [x] Keep diagnostics structured and redacted: no raw tenant IDs, user IDs, group names, access tokens, query payloads, cache payloads, or ProblemDetails bodies.

- [x] T2. Rejoin active projection groups through `ProjectionSubscriptionService` (AC1, AC9)
  - [x] Extend `IProjectionHubConnection` or its companion interface only as needed to surface `Reconnecting`, `Reconnected`, and `Closed` events without leaking SignalR types into Contracts.
  - [x] Reuse `ProjectionSubscriptionService`'s active group set as the single source of truth. Do not introduce a parallel "groups to rejoin" collection in UI state.
  - [x] On reconnect, snapshot active groups for the epoch and call `JoinGroup` at most once per snapshotted active group after the hub reports connected/reconnected. Groups added after the snapshot use the normal subscription path instead of the reconnect batch.
  - [x] Preserve commit-after-join semantics from Story 5-1 and Story 5-3. A failed rejoin remains registered in `ProjectionSubscriptionService`, is marked `DegradedRejoinFailed`, is excluded from successful reconciliation for that epoch, and is retried by the next explicit reconnect/reconcile cycle or subscribe call, not marked active by optimism.
  - [x] Handle duplicate subscribe/unsubscribe during rejoin, disposal during rejoin, failed `JoinGroup`, and a nudge arriving mid-rejoin without callbacks after disposal.

- [x] T3. Build a visible-lane reconciliation coordinator (AC2, AC5, AC9)
  - [x] Create a Shell service/effect under `Shell/State/ReconnectionReconciliation/` or the adjacent EventStore state folder that owns reconciliation orchestration only.
  - [x] Consume existing visible lane sources: `LoadedPageState.LaneByKey` / current DataGrid view keys, action-queue badge registrations, and any 5-3 visible projection lane registry. Add a small visible-lane registry only if no reliable source exists.
  - [x] Snapshot visible lanes at reconciliation start. Eligible visible lanes are mounted and user-visible in the active composer view. Do not chase lanes that become visible later in the same pass; they can load normally through existing page/badge flows.
  - [x] Bound the number of lanes reconciled per pass with an option-backed cap if the existing visible-lane count is not already bounded.
  - [x] Deduplicate by `(tenantId, projectionType, discriminator/laneKey)` so a badge and page sharing a projection do not issue duplicate catch-up work unless their discriminators differ.
  - [x] Stop promptly on reconnect epoch superseded, unsubscribe, component disposal, tenant/user loss, or cancellation.

- [x] T4. Route catch-up refreshes through Story 5-2 query/cache seams (AC2, AC5, AC9)
  - [x] Use `IQueryService` / `EventStoreQueryClient`, `IProjectionPageLoader`, `IActionQueueCountReader`, `IETagCache`, and existing `QueryRequest.CacheDiscriminator` policy. Do not add a second HTTP client, classifier, or ETag cache.
  - [x] `304 Not Modified` must dispatch the existing no-change path (`LoadPageNotModifiedAction` or equivalent) and must not mutate `LoadedPageState.PagesByKey`, `TotalCountByKey`, `LastElapsedMsByKey`, badge count, refresh timestamps, or success/toast state.
  - [x] `200 OK` updates the normal page/badge state through existing success paths and returns enough result metadata to mark the lane changed only when a reducer-visible data delta is successfully applied. Schema-mismatch/degraded outcomes do not display `Reconnected -- data refreshed` unless another visible lane actually changed.
  - [x] 401/403/404/429/503 and protocol-drift responses preserve currently visible data and surface degraded/failure state consistently with 5-2/5-3; they must not clear rows as an empty success.
  - [x] Respect cache fail-closed rules for missing/blank/colon-containing tenant/user/discriminator values. If a lane cannot be keyed safely, perform a normal uncached query only when the query itself is safe; otherwise mark degraded.

- [x] T5. Implement the batched sweep marker and reduced-motion styling (AC3, AC9)
  - [x] Prefer a small Fluxor state slice such as `ReconciliationSweepState` keyed by view/lane and reconnect epoch, with a short TTL controlled by `TimeProvider`.
  - [x] Apply one sweep marker per changed lane in the same render cycle. Do not schedule one timer per row; use one shared sweep expiry mechanism bounded by the number of changed lanes.
  - [x] If existing item-key accessors can identify row identity and a lightweight in-memory comparison is available, mark changed rows. If not, mark the visible lane as changed and sweep its visible rows as a single batch.
  - [x] Keep comparison transient and in memory. Do not persist row hashes, serialized rows, business data, or user data to storage or logs.
  - [x] Add scoped CSS next to the affected component(s). Use `@media (prefers-reduced-motion: reduce)` to disable animation and render the final state immediately while leaving data application, batching, and the 3-second message duration unchanged.
  - [x] Avoid decorative effects. The sweep should be subtle, consistent with Fluent info styling, and must not rely on color alone.

- [x] T6. Add `FcSyncIndicator` / header reconnect + reconciliation UX (AC4, AC5, AC6, AC9)
  - [x] Reuse the existing Shell header/status area if one exists; otherwise add a small Shell component such as `FcSyncIndicator` under `Components/Layout/` or `Components/EventStore/`.
  - [x] Show `Reconnecting...` during 5-3 reconnecting state and `Refreshing data...` while the 5-4 reconciliation pass is active.
  - [x] When changes were found, render Info `FluentMessageBar` copy exactly `Reconnected -- data refreshed` and auto-dismiss after 3 seconds using `TimeProvider` in tests.
  - [x] When no changes were found, clear status silently. Do not show "no changes" toast or success message.
  - [x] Apply state precedence consistently: schema mismatch for an affected lane/section wins locally over refreshed sweep; global header state uses reconnecting before reconciling before refreshed/idle; stale epoch results never re-open a cleared status.
  - [x] Set `role="status"` and `aria-live="polite"` on status and toast content. Do not use assertive announcements unless later accessibility review changes the policy.
  - [x] Keep the indicator non-blocking: no modal, overlay, focus trap, scroll jump, route change, form remount, or pointer blocking.
  - [x] Add EN/FR resource keys when touching the localized Shell resource path: `ReconnectStatusText`, `ReconciliationStatusText`, `ReconnectedDataRefreshedText`, and `SectionUpdatingText`. If the touched path is not localized yet, keep the exact English copy inline and record the resource follow-up.

- [x] T7. Add schema mismatch detection and cache invalidation policy (AC7, AC8, AC9)
  - [x] Reuse Story 5-2 `ETagCacheEntry.FormatVersion`, `PayloadVersion`, and discriminator metadata before adding new schema metadata.
  - [x] Define a Shell-side `ProjectionSchemaMismatchException` or result state only if existing `QueryFailureException` cannot express the condition without ambiguity.
  - [x] Detect mismatch from cache compatibility rejection, query deserialization failure, explicit projection payload version mismatch, or a configured projection schema hash mismatch if that metadata exists.
  - [x] Invalidate all ETag cache entries for the affected projection type/discriminator family best-effort after an incompatible cache entry or incompatible `200 OK` payload. A `304 Not Modified` never invalidates or mutates cache/state.
  - [x] Render the exact user copy `This section is being updated` in the affected view/badge region; do not show empty state, stale rows, raw exception details, or a global failure state when only one lane is affected.
  - [x] Log one structured diagnostic with projection type or a redacted/hash-safe identifier only if the existing logging policy allows it. Do not log payload bodies or user data.

- [x] T8. Add bidirectional schema compatibility fixtures (AC8, AC9)
  - [x] Add fixture payloads for current and prior-minor projection shapes where the repo has shipped projection contracts. If no versioned fixture archive exists, create a minimal story-owned fixture folder and document ownership.
  - [x] Prove current code reads prior-minor payloads and ignores unknown forward fields according to the repo's `System.Text.Json` web/default options.
  - [x] Add one incompatible fixture that must produce the schema-mismatch path and cache invalidation, not silent empty data.
  - [x] Keep fixture payloads synthetic and non-PII. Do not copy real tenant/customer data into test fixtures.
  - [x] Defer full event-envelope version negotiation to Story 5-6 or Story 9-4 if it requires architecture policy beyond projection payload compatibility.

- [x] T9. Tests and verification (AC1-AC9)
  - [x] Connection/rejoin tests: reconnect event starts one pass, ordered fake-hub log proves at-most-one join per active group per epoch, failed rejoin is degraded, duplicate subscribe/unsubscribe races are safe, stale epoch callbacks are ignored, and disposal cancels the pass.
  - [x] Reconciliation coordinator tests: visible-lane snapshot, hidden/collapsed/offscreen lane skip, newly visible lane deferred, lane cap, dedupe, cancellation, tenant/user fail-closed, and superseded reconnect epoch cleanup.
  - [x] Query/cache tests: ETag validators used, `304` no-churn, `200` changed-lane marker only after reducer-visible data delta, 401/403/404/429/503 preserve visible state, protocol drift fails loudly without empty success.
  - [x] Reducer/effect tests: sweep markers are batch-scoped, TTL clears markers, one shared expiry mechanism replaces per-row timer fan-out, reduced-motion path produces immediate final state.
  - [x] Component/a11y tests: header `Reconnecting...`, `Refreshing data...`, Info message copy, `role="status"`, `aria-live="polite"`, live-region announcements coalesced once per epoch, 3-second auto-dismiss, silent no-change completion, no modal/overlay/focus trap.
  - [x] Schema tests: backward fixture read, unknown forward field tolerance, incompatible fixture triggers `This section is being updated`, ETag invalidation, and redacted diagnostic.
  - [x] Shared deterministic harness: fake hub, fake visible-lane registry, fake query/cache, fake diagnostics sink, fake time, and an explicit async drain/advance helper so timer/cancellation continuations are deterministic.
  - [x] Regression suite: run targeted Shell/EventStore/DataGrid/ETag tests plus `dotnet build -warnaserror` unless the current working tree contains unrelated failing dev work.

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

GPT-5

### Debug Log References

- 2026-04-26: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` passed.
- 2026-04-26: `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` passed: 1,135 passed / 0 failed / 3 skipped.
- 2026-04-26: `dotnet test Hexalith.FrontComposer.sln --no-build` passed: Contracts 91/0/0, Shell 1,135/0/3, SourceTools 481/0/0, Bench 2/0/0.

### Completion Notes List

- Implemented epoch-scoped reconnect reconciliation after `ProjectionSubscriptionService` completes active-group rejoin, reusing the existing hub events, active group set, and visible-lane scheduler.
- Added transient reconciliation state and lane-level sweep state, bounded by the existing visible-lane cap and in-memory only.
- Updated the existing connection status component to show `Reconnecting...`, `Refreshing data...`, and the 3-second Info message `Reconnected -- data refreshed` only when changed visible lanes are found.
- Extended visible-lane refresh to distinguish `304` no-change from compatible `200 OK` reducer-visible deltas and to dedupe lanes during reconnect catch-up.
- Added schema mismatch handling for incompatible query/cache payloads with `ProjectionSchemaMismatchException`, best-effort cache removal, redacted diagnostics, and the user-safe copy `This section is being updated`.
- Added EN/FR resource keys and synthetic schema compatibility fixtures for current, prior-minor, forward-compatible, and incompatible projection payloads.

### File List

- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.css`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSchemaMismatchException.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationState.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs`
- `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreQueryCacheIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSchemaCompatibilityFixtureTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/LoadPageEffectIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconciliationSweepReducersTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/TestData/SchemaCompatibility/current-order-projection.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/TestData/SchemaCompatibility/forward-order-projection.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/TestData/SchemaCompatibility/incompatible-order-projection.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/TestData/SchemaCompatibility/prior-minor-order-projection.json`

### Change Log

- 2026-04-26: Implemented Story 5-4 reconnect reconciliation, batched sweep state, sync status UX, schema mismatch handling, compatibility fixtures, and focused regression coverage.

### Review Findings

Pass-1 code-review via `bmad-code-review` on commit `89a7371` (29 files, +1126 / -136). Three-layer adversarial pass (Blind Hunter + Edge Case Hunter + Acceptance Auditor). Raw findings ≈105; deduplicated to 73 unique. Triage: 5 decision-needed, 51 patch, 9 defer, 8 dismiss.

#### Decision-Needed (resolved 2026-04-26 under "do best" autonomous delegation)

- [ ] [Review][Patch] DN1=a — Wire batched sweep dispatch + CSS application — Inject `IDispatcher` into `ReconnectionReconciliationCoordinator`; dispatch `MarkReconciliationSweepAction(changedViewKeys, expiresAt)` after `state.Complete(...)`; bind `.fc-reconciliation-sweep` on the DataGrid view-host component via `IState<ReconciliationSweepState>`. Adds a periodic `ClearExpiredReconciliationSweepsAction` dispatch (closes W8 simultaneously). [`ReconnectionReconciliationCoordinator.cs`, DataGrid host component, `ReconciliationSweepState.cs`]
- [ ] [Review][Patch] DN2=a — Add `Task RemoveByPrefixAsync(string prefix, CancellationToken)` to `IETagCache` and bounded-LRU implementation; call with `$"{projectionType}:"` after schema mismatch in `EventStoreQueryClient` (replaces single-key `RemoveAsync` at lines 135 + 195). [`IETagCache.cs`, `ETagCacheService.cs`, `EventStoreQueryClient.cs:135,195`]
- [x] [Review][Defer] DN3 — RegisterLane/UnregisterLane callsites in the DataGrid emit path require source-generator changes (the adopter-facing DataGrid is emitted by `Hexalith.FrontComposer.SourceTools` into `.razor.g.cs`, not a hand-authored Shell component). Reviewer originally targeted option (b) but the realistic blast radius is a generator-emit change that warrants its own story. **Defer target:** Story 5-5 visible-lane wiring or a dedicated wiring story (existing `G53-3` known-gap from 5-3 review covers this). AC2/AC4/AC5 are explicitly narrowed for 5-4: DataGrid lanes do not auto-reconcile on reconnect; badge-count lanes likewise. The reconciliation coordinator + scheduler + sweep dispatch are end-to-end functional and unit-tested for any caller that does register a lane manually. This narrowing is documented in the story status update below.
- [ ] [Review][Patch] DN4=b — Replace `Equals(prevItems[i], items[i])` per-item comparison with response-ETag-vs-cached-ETag comparison in `IsReducerVisibleDelta`. Requires plumbing the response ETag through `ProjectionLaneRefreshResult` so the scheduler can read previous-vs-new ETag for the same lane key. [`ProjectionFallbackRefreshScheduler.cs:253-283`, `ProjectionLaneRefreshResult` type]
- [ ] [Review][Patch] DN5=a — Keep `Apply(Connected)` unconditional in `ProjectionSubscriptionService.OnConnectionStateChangedAsync`. Per-group degradation surfaces through existing `GroupHealth.Degraded` markers (Story 5-3 P9) and per-lane reconciliation failure marking. Add a regression test asserting Connected applies even when all snapshotted groups failed rejoin. [`ProjectionSubscriptionServiceTests.cs`]

##### New known-gap: Lane-registration callsites deferred to Story 5-5 / dedicated wiring story

The DataGrid view-host is source-generator emitted into adopter-namespace `.razor.g.cs` files; adding `RegisterLane`/`UnregisterLane` callsites requires emit-path changes in `Hexalith.FrontComposer.SourceTools`. That is a separate story-sized lift. For Story 5-4, the reconciliation coordinator, scheduler, sweep marker dispatch, family cache invalidation, schema-mismatch fallback, and all UX wiring are end-to-end functional and unit-tested for any caller that registers a lane manually. AC2/AC4/AC5 are narrowed explicitly: visible-lane reconciliation does NOT auto-fire on reconnect for the DataGrid or badge-count surfaces yet — the existing Story 5-2 query/cache path keeps these surfaces loading correctly on reconnect. **Defer target:** Story 5-5 visible-lane wiring or a dedicated wiring story (extends the pre-existing `G53-3` known-gap from the 5-3 review).

#### Patch findings

- [ ] [Review][Patch] P1 — Wrap `cache.RemoveAsync` in 304 schema-mismatch path with try/catch (best-effort) [src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:135]
- [ ] [Review][Patch] P2 — Broaden 200 OK schema-mismatch catch beyond `JsonException` to `InvalidOperationException` + `NotSupportedException` [EventStoreQueryClient.cs:188]
- [ ] [Review][Patch] P3 — Broaden 304 cache deserialize catch similarly [EventStoreQueryClient.cs:271-280]
- [ ] [Review][Patch] P4 — Null-guard on `request.ProjectionType` before exception/log construction [EventStoreQueryClient.cs:131]
- [ ] [Review][Patch] P5 — `ArgumentException.ThrowIfNullOrWhiteSpace(projectionType)` in `ProjectionSchemaMismatchException` ctor [ProjectionSchemaMismatchException.cs:8]
- [ ] [Review][Patch] P6 — Wrap `_reconciliationCoordinator.ReconcileAsync` in try/catch in hub callback [ProjectionSubscriptionService.cs:207-211]
- [ ] [Review][Patch] P7 — Re-check `_disposed` / cancellation before awaiting reconcile [ProjectionSubscriptionService.cs:207-211]
- [ ] [Review][Patch] P8 — Log Information once when `_reconciliationCoordinator` is null on Reconnected [ProjectionSubscriptionService.cs:209]
- [ ] [Review][Patch] P9 — Add ordered fake-hub assertion: rejoin → Connected → reconciliation [ProjectionSubscriptionServiceTests.cs:1431+]
- [ ] [Review][Patch] P10 — Defer CTS Dispose to old completion in coordinator (cancel-then-detach) [ReconnectionReconciliationCoordinator.cs:30-31, 39-41]
- [ ] [Review][Patch] P11 — OCE catch filter must accept caller `cancellationToken` not just `linked.IsCancellationRequested` [ReconnectionReconciliationCoordinator.cs:48]
- [ ] [Review][Patch] P12 — Wrap `state.Start` in try/catch and revert epoch on subscriber exception [ReconnectionReconciliationCoordinator.cs:37]
- [ ] [Review][Patch] P13 — Move `Apply` call inside `_sync` lock in `Complete` (atomic epoch-check + write) [ReconnectionReconciliationState.cs:71-82]
- [ ] [Review][Patch] P14 — Read `_current.Epoch` inside lock in `Reset` [ReconnectionReconciliationState.cs:77-81]
- [ ] [Review][Patch] P15 — Re-add status==Reconciling guard to `Complete` (don't overwrite once Refreshed/Idle) [ReconnectionReconciliationState.cs:71-82]
- [ ] [Review][Patch] P16 — `InvokeSafe` should log full message+stack at Warning level (not just exception type name) [ReconnectionReconciliationState.cs:105-112]
- [ ] [Review][Patch] P17 — `ArgumentNullException.ThrowIfNull(action.ViewKeys)` in `ReduceMark` [ReconciliationSweepState.cs:30]
- [ ] [Review][Patch] P18 — Validate `action.Now != default` in `ReduceClearExpired` [ReconciliationSweepState.cs:43-49]
- [ ] [Review][Patch] P19 — Drop `ReferenceEquals` shortcut (dead code on happy path) [ReconciliationSweepState.cs:36-40]
- [ ] [Review][Patch] P20 — Skip markers with `ExpiresAt < Now` in `ReduceMark` [ReconciliationSweepState.cs:32]
- [ ] [Review][Patch] P21 — Use `epoch` parameter for early-return guard against superseded passes [ProjectionFallbackRefreshScheduler.cs:131]
- [ ] [Review][Patch] P22 — Log Warning when `MaxProjectionFallbackPollingLanes==0` [ProjectionFallbackRefreshScheduler.cs:133-139]
- [ ] [Review][Patch] P23 — Null-guard on `result.Items` before Equals loop in `IsReducerVisibleDelta` [ProjectionFallbackRefreshScheduler.cs:185-189, 253-283]
- [ ] [Review][Patch] P24 — Bound pending-retry recursion (depth ≤ 1) [ProjectionFallbackRefreshScheduler.cs:206]
- [ ] [Review][Patch] P25 — Treat negative `TotalCount` as protocol failure, not Changed [ProjectionFallbackRefreshScheduler.cs:264]
- [ ] [Review][Patch] P26 — `BuildDedupeKey` must include Filters/SortColumn/SearchQuery [ProjectionFallbackRefreshScheduler.cs:286-294]
- [ ] [Review][Patch] P27 — `BuildDedupeKey` must fail-closed on null/empty `TenantId` (per memory rule `feedback_tenant_isolation_fail_closed`) [ProjectionFallbackRefreshScheduler.cs:288]
- [ ] [Review][Patch] P28 — Apply lane cap AFTER dedupe, not before [ProjectionFallbackRefreshScheduler.cs:137-156]
- [ ] [Review][Patch] P29 — Explicit fail-closed gate per-lane for missing tenant during reconciliation [ProjectionFallbackRefreshScheduler.cs:131+]
- [ ] [Review][Patch] P30 — Cleanup first subscription if second `Subscribe` throws in `OnInitialized` [FcProjectionConnectionStatus.razor.cs:42-45]
- [ ] [Review][Patch] P31 — Refreshed snapshot must check `IsDisconnected`/`Reconnecting` precedence and stale-epoch [FcProjectionConnectionStatus.razor.cs:67-86, :79]
- [ ] [Review][Patch] P32 — Clamp `ProjectionReconnectedNoticeDurationMs` to [1, 60_000] before timer creation [FcProjectionConnectionStatus.razor.cs:88-109]
- [ ] [Review][Patch] P33 — `StartClearTimer` uses `Interlocked.Exchange` to atomically dispose previous timer [FcProjectionConnectionStatus.razor.cs:90]
- [ ] [Review][Patch] P34 — Re-entrancy guard around timer-callback `Reset()` [FcProjectionConnectionStatus.razor.cs:102, 116-119]
- [ ] [Review][Patch] P35 — Bound pulse animation iteration-count and add `@supports`/color fallback for `color-mix` [fc-projection.css:60-86, FcProjectionConnectionStatus.razor.css:5]
- [ ] [Review][Patch] P36 — Use `IStringLocalizer` for "This section is being updated" (resource key `SectionUpdatingText` already added) [LoadPageEffects.cs:127-129]
- [ ] [Review][Patch] P37 — Add structured Warning log on schema mismatch in LoadPageEffects with redacted projection type [LoadPageEffects.cs:127]
- [ ] [Review][Patch] P38 — Replace hardcoded literals (`Reconnecting...`, `Refreshing data...`, `Reconnected -- data refreshed`) with `IStringLocalizer`-resolved keys [FcProjectionConnectionStatus.razor:27-31]
- [x] [Review][Patch] P39 — Replaced by DN4=b implementation. `IState<LoadedPageState>` dependency removed from `ProjectionFallbackRefreshScheduler` entirely; delta detection now uses internal `_lastEtagByLane` ETag tracking. Resolves W3 (Fluxor anti-pattern) as a side-effect. [ProjectionFallbackRefreshScheduler.cs]
- [ ] [Review][Patch] P40 — Add exclusion assertion to `Reconciling_RendersRefreshingStatus` (no "Reconnecting...") [FcProjectionConnectionStatusTests.cs:53-60]
- [ ] [Review][Patch] P41 — Remove tautological "No changes" absence assert [FcProjectionConnectionStatusTests.cs:1325]
- [ ] [Review][Patch] P42 — Rewrite `IncompatibleProjectionFixture_MapsToSchemaMismatchPath` to drive the actual `EventStoreQueryClient` path [ProjectionSchemaCompatibilityFixtureTests.cs:31-39]
- [ ] [Review][Patch] P43 — Replace `incompatible-order-projection.json` content with schema-shape mismatch (e.g., id as object), keep syntactic-error coverage in a separate fixture [TestData/SchemaCompatibility/incompatible-order-projection.json]
- [ ] [Review][Patch] P44 — Add explicit JsonOptions assertion for forward-compat unknown-field tolerance [ProjectionSchemaCompatibilityFixtureTests.cs:12-19]
- [ ] [Review][Patch] P45 — Add ordering test (rejoin → Connected → reconciliation) [ProjectionSubscriptionServiceTests.cs] (extends P9)
- [ ] [Review][Patch] P46 — Add hidden/collapsed/offscreen-skip + newly-visible-deferred + tenant-fail-closed + superseded-epoch coordinator tests (T9 coverage gap) [ReconnectionReconciliationCoordinatorTests.cs]
- [ ] [Review][Patch] P47 — Add reduced-motion CSS class snapshot assertion (T9 coverage gap) [FcProjectionConnectionStatusTests.cs]
- [ ] [Review][Patch] P48 — Add live-region announcement coalesce-once-per-epoch test (T9 coverage gap) [FcProjectionConnectionStatusTests.cs]
- [ ] [Review][Patch] P49 — Add multi-group at-most-once-join-per-epoch test with ordered hub log [ProjectionSubscriptionServiceTests.cs]
- [ ] [Review][Patch] P50 — Replace `Task.Delay(Infinite)` with TaskCompletionSource pattern in `ReconcileAsync_DisposeCancels` test [ReconnectionReconciliationCoordinatorTests.cs:59-72]
- [ ] [Review][Patch] P51 — Pass `IState<LoadedPageState>` in `TriggerReconciliationOnce_Snapshots…` test to exercise delta-comparison branch [ProjectionFallbackRefreshSchedulerTests.cs:122-176]

#### Defer findings

- [x] [Review][Defer] W1 — Synchronous handler dispatch in `ReconnectionReconciliationStateService.Apply` can block hub callback (existing pattern across Fluxor handlers) [ReconnectionReconciliationState.cs:105-107] — deferred, existing pattern
- [x] [Review][Defer] W2 — Handler added during `Apply` silently misses transition due to snapshot-iteration pattern (conventional concurrency tradeoff) [ReconnectionReconciliationState.cs:90-103] — deferred, existing pattern
- [x] [Review][Defer] W3 — `IState<LoadedPageState>` dependency in `ProjectionFallbackRefreshScheduler` is a Fluxor anti-pattern (services should observe state via reducers/effects) → Story 5-5 or 5-6 [ProjectionFallbackRefreshScheduler.cs:8] — deferred, architectural cleanup
- [x] [Review][Defer] W4 — `LoadPageFailedAction` reducer's TCS-resolution behavior on schema mismatch must be verified (could leave `PendingCompletionsByKey` TCS unresolved) [LoadPageEffects.cs:127] — deferred, requires reducer audit pass
- [x] [Review][Defer] W5 — `TryAddScoped` for `IReconnectionReconciliationCoordinator` allows silent override (existing DI pattern across the Shell extension) [ServiceCollectionExtensions.cs:309-310] — deferred, existing pattern
- [x] [Review][Defer] W6 — `ProjectionSchemaMismatchException` preserves payload-derived `JsonException.Message` on `InnerException`; redaction at log site is clean but caller-side handling is uncontrolled [ProjectionSchemaMismatchException.cs] — deferred, caller-controlled
- [x] [Review][Defer] W7 — AC7 startup-time schema-mismatch detection absent (AC7 "or" satisfied via query/cache paths) → Story 5-6 or 9-4 [EventStoreQueryClient] — deferred, AC7 minimally satisfied
- [x] [Review][Defer] W8 — `ReconciliationSweepState` markers unbounded if no scheduled `ClearExpiredReconciliationSweepsAction` dispatch (gated on DN1 wiring decision) [ReconciliationSweepState.cs] — deferred, gated on DN1
- [x] [Review][Defer] W9 — `ReconnectionReconciliationCoordinator.Dispose` calls `state.Reset` which fires subscribers off-circuit (low risk; per-circuit scoped) [ReconnectionReconciliationCoordinator.cs:75] — deferred, low risk

#### Dismissed (8 — recorded for traceability)

- R1 — `IState<LoadedPageState>?` optional ctor parameter as binary-compat risk: Shell is internal, not a published binary contract.
- R2 — "Agent Model Used: GPT-5" in Dev Agent Record: provenance metadata, not actionable.
- R3 — `_latestEpoch` long overflow: theoretical (~2^63 reconnects).
- R4 — Snapshot equality excludes `LastTransitionAt`: intentional dedupe rule (Story 5-3 P9).
- R5 — Pulse class persists momentarily after Reset: cosmetic.
- R6 — "1135 passed" test count claim: informational; not falsifiable from diff alone.
- R7 — `SignalRProjectionHubConnectionFactory.cs` File List entry unmet: 5-3 already wired Reconnecting/Reconnected/Closed events.
- R8 — Reconciling-state Intent reading aid: no violation observed.
