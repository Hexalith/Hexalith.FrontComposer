---
baseline_commit: ea23187bba3d0f18006ea8db9df26842c19b1597
---

# Story 2.6: Live projection updates with reconnect & reconciliation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Like Stories 2.1–2.5, the **live-update / reconnect / reconciliation surface already exists, builds,
> and is heavily unit-tested** at baseline `ea23187`. The shipped code carries `Story 5-x` / `P##`
> docstring markers (the reconnect/reconciliation engine was authored ahead of this epic's confirm-and-pin
> numbering) — **that is expected; do not "re-attribute" or churn it.**
>
> **What is already built and pinned (do NOT rebuild or restyle):**
> - **SignalR subscription + reconnect plumbing** — `SignalRProjectionHubConnectionFactory`
>   (`WithAutomaticReconnect()`, `Reconnecting`/`Reconnected`/`Closed` events) →
>   `ProjectionSubscriptionService` (join/leave groups, nudge handling, rejoin-on-reconnect,
>   reconciliation trigger). Pinned by `ProjectionSubscriptionServiceTests` +
>   `ProjectionSubscriptionServiceFaultTests` (fault injection) + `FaultInjectingProjectionHubConnectionTests`.
> - **Live nudge → grid refresh** — the generated view registers a `ProjectionFallbackLane` via
>   `ProjectionFallbackRefreshScheduler.RegisterLane(...)` (emitted in `RazorEmitter.cs`); a SignalR nudge
>   drives `TriggerNudgeRefreshAsync(projectionType, tenantId)` → the lane's `RefreshAsync` re-queries
>   (ETag-gated). Pinned by `ProjectionFallbackRefreshSchedulerTests` / `ProjectionFallbackPollingDriverTests`.
> - **Connection-status UI (AC2)** — `FcProjectionConnectionStatus` is rendered in
>   **`FrontComposerShell.razor` (line ~108)**, subscribes to `IProjectionConnectionState` +
>   `IReconnectionReconciliationState`, surfaces "Reconnecting…" / "Refreshing data…" /
>   "Reconnected — data refreshed" with `role="status"` + `aria-live="polite"`, auto-clears via
>   `FakeTimeProvider`-driven timer. Pinned by `FcProjectionConnectionStatusTests` (7 tests incl. precedence,
>   stale-epoch filtering, once-per-epoch announce).
> - **Reconnect → reconcile engine (AC2)** — `ReconnectionReconciliationCoordinator.ReconcileAsync`
>   (epoch-scoped, supersession, sweep-marker dispatch, sweep cleanup TTL), `ReconciliationSweepState`
>   reducers (512-cap, LRU-by-expiry), `ProjectionConnectionState` (reconnect-attempt accumulation, bounded
>   failure-category dedup). Pinned by `ReconnectionReconciliationCoordinatorTests` /
>   `ReconciliationSweepReducersTests` / `ProjectionConnectionTelemetryTests`.
> - **`FcNewItemIndicator` component + `NewItemIndicatorStateService`** — the component renders
>   `role="status"` / `aria-live="polite"` "New item…" copy (adopter-overridable Text/AriaLabel); the
>   service tracks per-lane entries with 10s TTL auto-dismiss + dismiss-on-filter-change +
>   dismiss-on-materialization + tenant/user scope-boundary clearing. Pinned **in isolation** by
>   `FcNewItemIndicatorTests` (4 tests) + spike type-presence.
>
> **So this story's job is to (1) VERIFY both ACs hold end-to-end against `src/` at this commit, (2) CLOSE
> the genuine durability gap, and (3) make the verification durable** so 2.7 (palette/search) and 2.8
> (FC-TBL confirm) build on a pinned live-update baseline. **Default to ZERO `src/` change** — but see the
> AC1 gap below, which may be a *genuine* `src/` gap, not merely a test gap. Decide honestly; do not claim
> AC1 passes if it does not.

## Story

As an operator,
I want projection grids to update live and recover gracefully from connection loss,
so that I see current data and know when the stream is degraded.

## Acceptance Criteria

**AC1 — Live update on a SignalR change: the grid updates and a "new item" indicator marks fresh rows. *(FR13, FR14)***

> **AC1 disposition (PO-accepted 2026-06-04):**
> - **AC1(a) — live nudge → grid re-query/re-render (ETag-gated): ✅ IMPLEMENTED & durably pinned.**
> - **AC1(b) — "new-item indicator marks fresh rows": ⏸️ ACCEPTED-DEFERRED to Epic 3/5 — Story 5-5.**
>   The Product Owner has **formally accepted** carrying AC1(b) into the command-lifecycle producer story
>   (Story 5-5). Rationale (independently review-verified): the live-nudge seam
>   `IProjectionHubConnection.OnProjectionChanged(projectionType, tenantId)` carries **no per-row identity**
>   (`EntityKey`/`MessageId`), and the new-item indicator's producer belongs to the
>   command-lifecycle / pending-command-resolution path — out of scope for this projection-read-path story.
>   `FcNewItemIndicator` + `NewItemIndicatorStateService` ship as a **confirmed, unit- and integration-pinned
>   primitive**; their end-to-end producer→consumer wiring is **tracked as a Story 5-5 dependency**. This is a
>   conscious acceptance, not a silent pass.

**Given** an active projection subscription over SignalR (a registered, visible projection lane in a tenant
context),
**When** the backend emits a change for that projection type/tenant (a nudge),
**Then** the subscription pipeline (`ProjectionSubscriptionService.OnProjectionChangedAsync` →
`IProjectionFallbackRefreshScheduler.TriggerNudgeRefreshAsync`) triggers the registered lane's `RefreshAsync`,
the grid re-queries and re-renders with the new data (ETag-gated — a no-change wire result does not churn),
**And** a row that arrived during the live window is marked by **`FcNewItemIndicator`**
(`role="status"`, `aria-live="polite"`, localized "New item…" copy), driven by
`INewItemIndicatorStateService` (per-lane entry, 10s TTL auto-dismiss, dismiss-on-filter-change,
dismiss-on-materialization), **scoped/cleared on tenant or user transition**.

**AC2 — Reconnect surfaces status and the grid reconciles missed changes. *(UX-DR5, FR13)***
**Given** an active subscription whose SignalR connection drops,
**When** it reconnects (`ProjectionHubConnectionState.Reconnected`),
**Then** `ProjectionSubscriptionService` rejoins the active groups and runs a reconciliation pass
(`ReconnectionReconciliationCoordinator.ReconcileAsync`, epoch-scoped), the visible lanes re-query to catch
up missed changes, and changed lanes get a reconciliation **sweep marker**
(`MarkReconciliationSweepAction`, 512-cap / LRU / TTL-expiry),
**And** `FcProjectionConnectionStatus` (rendered in `FrontComposerShell`) surfaces the transition —
"Reconnecting…" while disconnected (connection-state precedence wins over a stale Refreshed snapshot),
then a brief "Reconnected — data refreshed" confirmation **only when the reconcile changed data**
(silent when unchanged), with `role="status"` + `aria-live="polite"`, auto-clearing after
`FcShellOptions.ProjectionReconnectedNoticeDurationMs`.

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming current behaviour against `src/` before
> writing anything. Most subtasks resolve to "already true → confirm the pin"; open a `src/` change only if
> a genuine AC gap is proven. Record what you found (true/false + evidence) in the Dev Agent Record so the
> review can audit it. **For AC2 the expected `src/` delta is ZERO. For AC1 see the new-item-indicator gap
> in Task 2 — it may require a genuine (minimal) `src/` wiring change or an explicit deferral decision.**

- [x] **Task 1 — Verify AC1 (a): SignalR nudge → grid live-refresh (AC: #1)**
  - [x] **Subscription → nudge path — confirm ALREADY PINNED, no change.** Re-confirm
    `ProjectionSubscriptionServiceTests` (subscribe idempotency, tenant-mismatch rejection, nudge handling,
    tenant-switch isolation) and `ProjectionSubscriptionServiceFaultTests` /
    `FaultInjectingProjectionHubConnectionTests` (Drop/Delay/PartialDelivery/Reorder/ReconnectNudge fault
    paths). Verify the emission site `ProjectionSubscriptionService.OnProjectionChangedAsync` →
    `IProjectionChangeNotifier`/`TriggerNudgeRefreshAsync`.
  - [x] **Scheduler refresh + ETag gate — confirm ALREADY PINNED, no change.** Re-confirm
    `ProjectionFallbackRefreshSchedulerTests` (`TriggerNudgeRefreshAsync` refreshes the matching
    projection-type/tenant lane; ETag-unchanged → no churn) + `ProjectionFallbackPollingDriverTests`
    (disconnected-mode bounded polling). Verify the generated view registers its lane —
    `RazorEmitter.cs` emits `ProjectionFallbackRefreshScheduler.RegisterLane(new ProjectionFallbackLane(...))`
    in the generated `*View` (confirm against a generated artifact, e.g. the Counter sample's generated view
    under `obj/**/generated/HexalithFrontComposer/`; **never hand-edit generated output**).
  - [x] **ASSESS the end-to-end "grid updates" gap.** The unit tests prove the scheduler/subscription in
    isolation. Confirm whether an end-to-end pin exists that *renders a generated grid, fires a nudge, and
    asserts the grid re-renders with new rows*. If absent and addable cheaply via the 2.3/2.4/2.5 render
    precedent (`GeneratedComponentTestBase`, a `Generated/` specimen), fold it into the AC1 integration pin
    in Task 2. **Pin-only; no `src/` change for this sub-clause** (the live-refresh wiring exists).

- [x] **Task 2 — Verify AC1 (b): "new item indicator marks fresh rows" — THIS IS THE REAL WORK / THE GAP**
  - [x] **Component + state service — confirm ALREADY PINNED in isolation, no change.** Re-confirm
    `FcNewItemIndicatorTests`: `RendersAccessiblePoliteIndicatorCopy` (`role="status"`, `aria-live="polite"`,
    localized text + aria-label), `AcceptsAdopterOverrideForVisibleTextAndAriaLabel`,
    `State_AutoDismissesAfterConfiguredDurationUsingTimeProvider` (10s TTL via `FakeTimeProvider`),
    `State_DismissesOnFilterChangeAndMaterialization`. Confirm `NewItemIndicatorStateService` scope-boundary
    clearing on tenant/user transition.
  - [x] **PROVE OR DISPROVE the end-to-end wiring (the genuine AC1 gap).** At baseline `ea23187`:
    `INewItemIndicatorStateService.Add(...)` / `Snapshot(...)` have **no caller in `src/`** (only the DI
    registration in `ServiceCollectionExtensions.cs`), and **`FcNewItemIndicator` is rendered nowhere**
    (no hand-written razor, no generated view emits it). So the building blocks exist and are unit-tested,
    but "a *fresh row* is *marked* in the *grid*" is **not wired end-to-end**. **Verify this against `src/`
    yourself** (grep `Add(`/`Snapshot(`/`<FcNewItemIndicator`), then choose ONE, and record the rationale:
    1. **If a wiring path genuinely exists** that you missed (e.g. the producer lives in the
       reconciliation/nudge flow and the component is rendered by a grid wrapper) → **pin it end-to-end**
       (nudge/reconcile produces a `NewItemIndicatorEntry`, grid renders `FcNewItemIndicator` for the lane).
       No `src/` change.
    2. **If it is genuinely unwired AND AC1 requires it for this story** → make the **minimal** `src/` change
       to connect producer→consumer: the nudge/reconcile path calls `INewItemIndicatorStateService.Add`
       for newly-materialized rows, and the generated grid (or a grid wrapper) renders `FcNewItemIndicator`
       from `Snapshot(viewKey)`. **Generator change goes in `RazorEmitter.cs`** (never hand-edit generated
       output); a shell-level render wrapper is also acceptable if it avoids regenerating every projection.
       Honour **Fluxor single-writer (ADR-007)** + **scoped-lifetime (ADR-030)**; the state service is
       already `Scoped`. **No new `IStorageService.SetAsync` call site** (NFR17 tripwire — none needed here).
    3. **If "new-item marking" is intentionally deferred** to a later story (e.g. the indicator ships as a
       confirmed-but-unwired primitive, mirroring how some Epic-5 surfaces predate their consumers) →
       **do NOT silently pass AC1.** Pin the component/service in isolation (already done), then **document
       the deferral explicitly** in the Dev Agent Record + Change Log and flag it for the review/PO so the AC
       is consciously accepted or carved out. Prefer option 1/2 if the wiring is small and low-risk.
  - [x] **Add the AC1 integration pin (the durable deliverable).** Following the 2.3/2.4/2.5 render
    precedent, add a `Generated/` (or `Components/DataGrid/`) test that exercises the *integrated* path you
    confirmed/wired: a registered lane + a nudge (or a direct `INewItemIndicatorStateService.Add`) →
    `FcNewItemIndicator` is present for the lane with the correct ARIA, and dismisses on TTL /
    materialization / filter-change. Pin `CultureInfo` via `CultureScope` for any localized assertion.

- [x] **Task 3 — Verify AC2: reconnect → status + reconcile missed changes (AC: #2) — expect ZERO `src/` change**
  - [x] **Connection-status component — confirm ALREADY PINNED, no change.** Re-confirm
    `FcProjectionConnectionStatusTests`: `Disconnected_RendersInlinePoliteStatusCopy`,
    `Reconciling_RendersRefreshingStatus`, `ReconciledWithChanges_RendersBriefConfirmation_ThenAutoClears`,
    `ReconciledWithoutChanges_ClearsSilently`, `DisconnectedWhileReconciling_PrecedenceWinsOverRefreshed`
    (connection-state precedence / stale-epoch filter), `ReconciliationCompletes_AnnouncesOnceForEpoch`.
    Confirm the component is rendered in `FrontComposerShell.razor` (~line 108) so it is actually mounted in
    the running shell (a component pinned but never mounted would not satisfy AC2 — assert it is mounted).
  - [x] **Reconnect → rejoin → reconcile engine — confirm ALREADY PINNED, no change.** Re-confirm
    `ReconnectionReconciliationCoordinatorTests` (epoch start/complete, no-change silent path, disposal
    cancellation, supersession P46, sweep-marker dispatch P20, sweep cleanup timer P22) +
    `ReconciliationSweepReducersTests` (512-cap, LRU-by-expiry P21, expired-skip, distinct-lane dedup) +
    `ProjectionConnectionTelemetryTests` (reconnect-attempt accumulation, bounded failure-category dedup).
    Verify `ProjectionSubscriptionService` rejoins groups on `Reconnected` and calls the coordinator.
  - [x] **ASSESS an end-to-end reconnect pin.** Confirm whether a test drives the *integrated* drop→reconnect
    →rejoin→reconcile→status path (the fault-injection harness `FaultInjectingProjectionHubConnection` +
    `ReconnectNudge` is the closest existing coverage). If a durable integration pin is missing and
    addable, add one asserting: on a simulated `Reconnected`, groups rejoin, `ReconcileAsync` runs once per
    epoch, changed lanes get a sweep marker, and `FcProjectionConnectionStatus` surfaces
    "Reconnecting…"→"Reconnected — data refreshed" (changed) / silent (unchanged). **Pin-only, no `src/`
    change** — AC2 is implemented at baseline.

- [x] **Task 4 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE
    (use `-m:1 /nr:false` if node-reuse causes flakiness, per Stories 2.4/2.5).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` —
    everything this story touches is green; new pins pass. **Host constraint (inherited from Stories
    2.3/2.4/2.5):** solution-level VSTest opens a local socket and fails with `SocketException (13):
    Permission denied` in this sandbox — if so, fall back to the **xUnit v3 in-process runner** per test
    assembly for local evidence, and record that the solution-level VSTest run is the CI gate. The new pins
    land in **`Shell.Tests`** (component/integration).
  - [x] **Re-prove the standing failure baseline.** Story 2.5's review recorded `Shell.Tests` at **8 failed**
    (documented pre-existing/environmental clusters: `PendingStatusReopenGovernanceTests` ×4 deferred-work
    file-IO, `NavigationEffectsLastActiveRouteTests` hydration, `CommandRendererFullPageTests` query-fallback,
    `CounterStoryVerificationTests` ×2 Verify snapshot drift) and `SourceTools.Tests` at **3 failed**. Capture
    before→after counts for the assemblies you touch and confirm the **same** pre-existing failures remain
    (none new, none misattributed). If your pins land only in `Shell.Tests`, say whether `SourceTools.Tests`
    is touched (it is only if you changed `RazorEmitter.cs` for the AC1 wiring).
  - [x] **`.verified.txt` discipline.** Confirm-and-pin → default **ZERO** snapshot edits. If a genuine AC1
    `src/` wiring change touches `RazorEmitter.cs` and changes generated markup, update the affected
    `CounterStoryVerificationTests.*RenderSnapshot.verified.txt` / emitter snapshots **intentionally**, and
    confirm the all-unannotated `CounterProjectionApprovalTests` baseline is byte-for-byte unchanged for any
    projection that does NOT exercise the new wiring.

- [x] **Task 5 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** record the complete File List + before→after test counts in the
    Dev Agent Record, reconciled against the actual git tree (the recurring Epic-1/2 review finding — pay it
    up front; include any QA test-summary artifact).
  - [x] **No authoring sentinels (retro AI-2):** scan new/modified test files + this story file — no stray
    `</content>` / `</invoke>` / `<invoke` / tool-call tags.
  - [x] **Record the AC1 gap decision explicitly** (option 1/2/3 from Task 2) with the proven evidence, so
    the review can audit whether AC1 is genuinely satisfied, minimally wired, or consciously deferred.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`ea23187`) | This story |
|---|---|---|
| SignalR hub + `WithAutomaticReconnect()` + state events | **Exists & pinned** — `SignalRProjectionHubConnectionFactory`, fault-injection tests | Confirm; no change |
| Subscription join/leave/nudge/rejoin-on-reconnect | **Exists & pinned** — `ProjectionSubscriptionService`, `ProjectionSubscriptionServiceTests` + fault tests | Confirm; no change |
| Live nudge → lane `RefreshAsync` (ETag-gated) | **Exists & pinned** — `ProjectionFallbackRefreshScheduler.TriggerNudgeRefreshAsync`, generated `RegisterLane` (`RazorEmitter.cs`) | Confirm; no change |
| **`FcNewItemIndicator` component + `NewItemIndicatorStateService`** | **Exists & pinned IN ISOLATION** — `FcNewItemIndicatorTests`; component rendered nowhere; `Add`/`Snapshot` **uncalled in `src/`** | **CLOSE THE GAP** — wire end-to-end (or deferral decision) + integration pin (AC1) |
| `FcProjectionConnectionStatus` (status copy, precedence, auto-clear) | **Exists, pinned & MOUNTED** — `FrontComposerShell.razor` ~L108, `FcProjectionConnectionStatusTests` (7) | Confirm; no change |
| Reconnect→reconcile coordinator + sweep markers + connection state | **Exists & pinned** — `ReconnectionReconciliationCoordinatorTests`, `ReconciliationSweepReducersTests`, `ProjectionConnectionTelemetryTests` | Confirm; assess integration pin |
| End-to-end drop→reconnect→reconcile→status (integration) | Closest = fault-injection harness (`ReconnectNudge`) | **Assess** a durable AC2 integration pin |

> **Key judgment for the dev agent:** AC2 is implemented and mounted — your job there is *confirm + (optional)
> integration pin*, ZERO `src/` change. **AC1 is the real work:** the live-*refresh* half is wired and
> pinned, but the "**new item indicator marks fresh rows**" half is an **isolated, unwired primitive** at
> this baseline. Verify that against `src/` (the producer `Add(...)` and the `<FcNewItemIndicator>` render
> site are both absent today), then make the *honest* call — wire it minimally (generator/`RazorEmitter.cs`
> or a grid wrapper), or document a conscious deferral. Do **not** assert AC1 passes on the strength of the
> isolation tests alone — that is exactly the "source-only / presence-only assertion gives false confidence"
> lesson from Stories 2.3/2.5.

### Exact anchors (read these before touching anything)

> ⚠️ **Line numbers are guidance, not contracts.** They reflect `ea23187`; confirm by symbol/marker before
> relying on any single line. Cite the symbol, not the line, in new pins.

**AC1 — live refresh + new-item indicator**
- **SignalR factory** — `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
  (`WithAutomaticReconnect()`; `Reconnecting`/`Reconnected`/`Closed` → `ProjectionHubConnectionStateChanged`).
  Interface `IProjectionHubConnection.cs` (`OnProjectionChanged`, `OnConnectionStateChanged`,
  `Join/LeaveGroupAsync`).
- **Subscription service** — `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
  (`OnProjectionChangedAsync` nudge handling; `OnConnectionStateChangedAsync` reconnect; `RejoinActiveGroupsAsync`;
  coordinator integration).
- **Scheduler** — `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`
  (`RegisterLane(ProjectionFallbackLane)`, `TriggerNudgeRefreshAsync(projectionType, tenantId)`,
  `TriggerReconciliationOnceAsync(epoch)`, ETag gate). Generated `RegisterLane` call emitted in
  `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` (~L1027; the `ProjectionFallbackRefreshScheduler`
  property injected ~L140). **Never hand-edit generated output** — change the emitter.
- **New-item component** — `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor(.cs)`
  (`role="status"`, `aria-live="polite"`, localized `NewItemIndicatorText`/`NewItemIndicatorAriaLabel`,
  adopter `Text`/`AriaLabelOverride` params).
- **New-item state service (the unwired producer)** — `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`
  (`INewItemIndicatorStateService.Add(NewItemIndicatorEntry)` / `Snapshot(viewKey)` /
  `DismissMaterialized` / `DismissForFilterChange`; 10s TTL; tenant/user scope-boundary clear; generation-token
  timer leak guard). DI: `ServiceCollectionExtensions.cs` ~L365 `TryAddScoped<INewItemIndicatorStateService, NewItemIndicatorStateService>()`.

**AC2 — reconnect status + reconcile**
- **Status component** — `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor(.cs)`
  (injects `IProjectionConnectionState` + `IReconnectionReconciliationState`; precedence; auto-clear via
  `FcShellOptions.ProjectionReconnectedNoticeDurationMs`). **Mounted in**
  `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` (~L108).
- **Connection state** — `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs`
  (`ProjectionConnectionTransition`; reconnect-attempt accumulation; bounded failure-category dedup; safe
  handler invocation).
- **Reconciliation** — `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/`:
  `ReconnectionReconciliationState.cs` (epoch dedup, `Start`/`Complete`/`Reset`),
  `ReconnectionReconciliationCoordinator.cs` (`ReconcileAsync`, supersession, sweep dispatch, cleanup timer),
  `ReconciliationSweepState.cs` (`MarkReconciliationSweepAction`, reducers: 512-cap, LRU, clear-expired).
- **Options** — `FcShellOptions.ProjectionReconnectedNoticeDurationMs` (notice auto-clear),
  fallback-poll budgets (`MaxProjectionFallbackPollingLanes`, etc.).

### Test anchors (where pins live / go)

- **Subscription (confirm)** — `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
  + `…/FaultInjection/ProjectionSubscriptionServiceFaultTests.cs`, `…/FaultInjection/FaultInjectingProjectionHubConnectionTests.cs`
  (harness: `FaultInjectingProjectionHubConnection(Factory).cs`).
- **Scheduler / polling (confirm)** — `tests/…/State/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs`,
  `…/ProjectionFallbackPollingDriverTests.cs`, `…/ProjectionConnectionTelemetryTests.cs`.
- **New-item (confirm isolation)** — `tests/…/Components/DataGrid/FcNewItemIndicatorTests.cs` (4) + spike
  type-presence `tests/…/Spike/Story10ShellIntegrationSpikeTests.cs` (~L130).
- **Connection-status (confirm)** — `tests/…/Components/EventStore/FcProjectionConnectionStatusTests.cs` (7).
- **Reconcile engine (confirm)** — `tests/…/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinatorTests.cs`,
  `…/ReconciliationSweepReducersTests.cs`.
- **AC1 integration pin (NEW — the gap)** — add under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
  (render specimen, 2.3/2.4/2.5 precedent) or `…/Components/DataGrid/`: prove nudge/Add → `FcNewItemIndicator`
  marks a fresh row on a lane + TTL/materialization/filter dismissal. Reuse `GeneratedComponentTestBase` /
  `AddFrontComposerTestHost`; `CultureScope` for localized copy.
- **AC2 integration pin (NEW — optional)** — add under `tests/…/Infrastructure/EventStore/` or
  `…/State/ReconnectionReconciliation/`: drive `Reconnected` via the fault harness, assert rejoin +
  one-reconcile-per-epoch + sweep marker + `FcProjectionConnectionStatus` copy transition.
- **Approval baseline (confirm untouched)** — `CounterProjectionApprovalTests` /
  `CounterStoryVerificationTests.*.verified.txt` byte-for-byte gate (only changes if you wire AC1 in the
  emitter for a projection that exercises it; baseline-only projections must stay byte-identical).

### Project-context guardrails that apply here (non-negotiable)

- **Fluxor single-writer (ADR-007) / scoped-lifetime (ADR-030):** each action type has one dispatch source;
  **effects own persistence + JS interop**, reducers stay pure (`ReconciliationSweepReducers` follow this).
  Storage/effects/auth/tenant accessors and `NewItemIndicatorStateService` are **scoped** — never capture in
  singletons. **NFR17 tripwire:** do not add a new `IStorageService.SetAsync` call site in `Shell/State/`
  (would require updating the tripwire whitelist + compliance matrix) — none is needed for AC1 wiring.
- **Generator rules (only if you wire AC1 in the emitter):** **never hand-edit generated code**
  (`obj/**/generated/HexalithFrontComposer/`) — change `RazorEmitter.cs` (or the annotated type). **No
  `ISymbol` escapes the parse stage**; IR stays pure & `EquatableArray`-based. **Diagnostics travel as
  `DiagnosticInfo`**, → Roslyn `Diagnostic` only inside `RegisterSourceOutput`. `SourceTools` references
  **only** `Contracts` (netstandard2.0-clean) — don't pull net10 deps in.
- **Real-time stack (pinned):** `Microsoft.AspNetCore.SignalR.Client` **10.0.8** (subscriptions),
  `System.Reactive` **6.1.0** (badge producer/consumer); FluentUI v5 RC `5.0.0-rc.3-26138.1`
  (`FluentMessageBar` for the status bar). Centralized versions in `Directory.Packages.props` — **never add
  `Version=` to a `.csproj`**; don't bump SignalR/FluentUI in this story.
- **Schema integrity:** do **not** touch `CanonicalSchemaMaterial` (encoder / sentinel /
  `StringComparer.Ordinal` / serialization) — it silently invalidates every fingerprint & baseline. Nothing
  in this story should.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix,
  **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE); `ArgumentNullException.ThrowIfNull`
  at public boundaries; **no copyright/license headers** (this repo has none); **CRLF**, 4-space indent,
  final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`); **bUnit** for
  components (`JSInterop.Mode = Loose`); Blazor component tests use `GeneratedComponentTestBase` /
  `AddFrontComposerTestHost`; **`FakeTimeProvider`** for every timer path (TTL auto-dismiss, notice
  auto-clear, sweep cleanup — all timer-driven here, so use it, never wall-clock sleeps); **Verify.XunitV3**
  (NOT `Verify.Xunit`) for any snapshot, `.verified.txt` updated **intentionally**; plural `{Class}Tests.cs`;
  three-part `Subject_Scenario_Expectation`; **solution-level** `dotnet test` + trait filters (not
  per-project); run with **`DiffEngine_Disabled=true`** (else Verify hangs); `CultureScope` for
  culture-sensitive assertions.
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress;
  built-in analyzers only (no Sonar/StyleCop/Roslynator).
- **Commits/branches:** If this story is pure verification + pins (AC2 + AC1 option 1/3), the work is
  **`test:`**-shaped (no release). If you make the AC1 wiring change (option 2), it is a genuine **`feat:`**
  (live new-item marking) — use `feat:` then, not `test:`. **No direct commits to `main`** — cut a
  `test/story-2-6-*` or `feat/story-2-6-*` feature branch + PR. *(History shows Stories 2.1–2.4 were
  committed straight to `main` by the story-automator, contradicting the "no direct commits to main" rule;
  2.5 used a feature branch — prefer the branch, and if the automator forces `main`, record the deviation in
  the Change Log.)*

### Project Structure Notes

- **Alignment:** all touched surfaces sit in the established shell layout (`Shell/Infrastructure/EventStore`,
  `Shell/State/ProjectionConnection`, `Shell/State/ReconnectionReconciliation`, `Shell/State/PendingCommands`,
  `Shell/Components/{DataGrid,EventStore,Layout}`) and — only if AC1 is wired in the generator —
  `SourceTools/Emitters/RazorEmitter.cs`. New pins go in the matching `*.Tests` mirror
  (`Shell.Tests/Generated`, `Shell.Tests/Components/DataGrid`, `Shell.Tests/Infrastructure/EventStore`,
  `Shell.Tests/State/ReconnectionReconciliation`). No new top-level folders or projects.
- **Dependency direction:** the reconnect/reconcile/new-item surfaces live in `Shell` (→ Contracts). The
  contracts (`IProjectionSubscription`, `IProjectionChangeNotifier`) live in `Contracts`. Do not pull
  net10/FluentUI/SignalR deps into `SourceTools` or the netstandard2.0 face of `Contracts`.
- **No variances expected for AC2** (confirm-and-pin, zero `src/`). **AC1** may carry a minimal, deliberate
  `src/` wiring delta (the new-item producer→consumer connection) — if made, call it out in the Dev Agent
  Record with the proven gap and the option chosen.

### Epic dependencies & their state

| Epic-2 needs | From | State at kickoff |
|---|---|---|
| Generated projection grid + Loading/Empty/Data dispatch (the grid that refreshes live) | Story 2.1 | ✅ pinned — `CounterStoryVerificationTests`, `GeneratedComponentTestBase` |
| DataGrid filtering + `GridViewSnapshot.Filters` (filter-change drives new-item dismissal) | Story 2.3 | ✅ done & pinned — `FcNewItemIndicator.DismissForFilterChange` ties to this |
| Expand-in-row detail (coexists in the same grid) | Story 2.4 | ✅ done & pinned |
| Column prioritization (coexists in wide grids) | Story 2.5 | ✅ done & pinned |
| EventStore SignalR+HTTP client swap (`AddHexalithEventStore`) | Story 1.x / Epic-5 plumbing | ✅ present — real `ProjectionSubscriptionService` + `SignalRProjectionHubConnectionFactory` swapped in over the stub |
| Command-lifecycle reconciliation / pending-command resolution on reconnect | Epic 3/5 | ⏳ the reconcile coordinator polls pending commands on reconnect (Story-5-5 DN1) — **not reopened here**; this story is the *projection* read path, not command lifecycle |

> **Scope boundary:** this story is "**live projection updates with reconnect & reconciliation**" (AC1 live
> nudge → grid refresh + new-item marking; AC2 reconnect → status surface + reconcile missed changes). It is
> *not* the command palette / global search (2.7 — `FcCommandPalette` / `FcProjectionGlobalSearch`), nor the
> FC-TBL table-API confirmation (2.8). It does **not** reopen command-lifecycle reconciliation
> (`alreadyApplied`, pending-command polling internals — Epic 3/5), the EventStore client swap itself, or
> Story-4-4 column-visibility persistence. Stay inside AC1–AC2.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability is
*already built*; the epics confirm + pin it. The live-update / reconnect / reconciliation engine (FR13 read
path, FR14, UX-DR5) is shipped and heavily unit-tested at `ea23187`. **Done =** AC2 is proven true end-to-end
against `src/` and carries a durable (re-confirmed + optionally integration-level) pin with **zero `src/`
change**; **AC1's live-refresh half** is re-confirmed green, and **AC1's "new-item indicator marks fresh
rows" half** is resolved honestly — either pinned end-to-end (if the wiring exists), minimally wired in the
generator/grid (if genuinely missing and in-scope), or explicitly deferred with PO/review sign-off (never
silently passed on isolation tests). The Release build is 0/0 under TWAE, the default test lane is green, the
standing failure baseline (`Shell.Tests` 8 / `SourceTools.Tests` 3) is re-proved pre-existing, the File List
+ counts are accurate, and any `.verified.txt` / approval-baseline change (only if AC1 was wired in the
emitter) is intentional with the non-exercising baseline confirmed byte-for-byte unchanged.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.6] — story statement, ACs (FR13, FR14, UX-DR5)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage; FR13 read path (SignalR projection subscriptions, reconnect/reconciliation status) → Epic 2
- [Source: _bmad-output/planning-artifacts/epics.md#Requirements Inventory] — FR13 (EventStore SignalR/HTTP clients, reconnect/reconciliation status), UX-DR5 (`FcProjectionConnectionStatus` status & loading UX)
- [Source: _bmad-output/project-context.md] — SignalR 10.0.8 / System.Reactive 6.1.0 / FluentUI v5 RC pins; Fluxor single-writer (ADR-007), scoped lifetime (ADR-030), NFR17 tripwire, generator rules (no `ISymbol` escape, `DiagnosticInfo` → `RegisterSourceOutput`), `CanonicalSchemaMaterial` immutability, TWAE, test discipline, `DiffEngine_Disabled=true`, dependency-direction-to-Contracts
- [Source: _bmad-output/project-docs/component-inventory.md#A] — `FcProjectionConnectionStatus`, `FcNewItemIndicator`, `FcPendingCommandSummary`; State slices (`ProjectionConnection`, `ReconnectionReconciliation`, `PendingCommands`); services (`ProjectionSubscriptionService`, `SignalRProjectionHubConnectionFactory`)
- [Source: _bmad-output/implementation-artifacts/2-5-column-prioritization-for-wide-projections.md] — prior confirm-and-pin pattern; "source-only/presence-only assertion = false confidence" lesson; VSTest socket sandbox constraint + xUnit v3 in-process fallback; 8-failure Shell baseline / 3-failure SourceTools baseline; retro AI-1/AI-2 taxes; feature-branch (not `main`) deviation note
- [Source: _bmad-output/implementation-artifacts/2-4-accessible-expand-in-row-detail.md] — `GeneratedComponentTestBase` end-to-end render precedent; `CultureScope` hardening
- [Source: _bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md] — `GridViewSnapshot.Filters` surface (filter-change → new-item dismissal); render-specimen precedent
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md] — File-List/sentinel taxes (AI-1/AI-2), failure baseline, "copy-a-pattern-without-the-difference" Epic-2 risk
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs] — `WithAutomaticReconnect()` + state events (AC1/AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs] — nudge handling, rejoin-on-reconnect, reconcile trigger (AC1/AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs] — `RegisterLane` / `TriggerNudgeRefreshAsync` / `TriggerReconciliationOnceAsync` (AC1/AC2)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs] — generated `RegisterLane` lane registration (~L1027); the AC1 new-item wiring change, if any, lands here
- [Source: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs] — new-item component (AC1) — rendered nowhere at baseline
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs] — `Add`/`Snapshot`/dismiss/scope-clear (AC1) — `Add`/`Snapshot` uncalled in `src/` at baseline
- [Source: src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor.cs] — status copy, precedence, auto-clear (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor] — `FcProjectionConnectionStatus` mounted (~L108) (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs] — `ReconcileAsync` epoch/supersession/sweep (AC2)
- [Source: src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs] — `MarkReconciliationSweepAction` + reducers (512-cap/LRU/clear-expired) (AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs] — new-item isolation pins (confirm; AC1) — **integration pin goes adjacent / under `Generated/`**
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcProjectionConnectionStatusTests.cs] — status pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs] + `…/FaultInjection/*` — subscription + reconnect-fault pins (confirm; AC1/AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinatorTests.cs] + `ReconciliationSweepReducersTests.cs` — reconcile-engine pins (confirm; AC2)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs] + `ProjectionFallbackPollingDriverTests.cs` + `ProjectionConnectionTelemetryTests.cs` — scheduler/polling/telemetry pins (confirm; AC1/AC2)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- Build: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **Build succeeded, 0 Warning(s), 0 Error(s)** (TWAE green).
- Test lane (host constraint per Stories 2.3/2.4/2.5): solution-level VSTest opens a local socket → `SocketException` in this sandbox, so evidence was captured via the **xUnit v3 in-process runner** on the built `Hexalith.FrontComposer.Shell.Tests` assembly with `DiffEngine_Disabled=true` and the story trait filter (`-notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined`). The solution-level VSTest run is the CI gate.
- AC1 gap verification (grep against `src/`, excluding `obj/`/`bin/`): `<FcNewItemIndicator` render sites in `src/` = **0** (only the component's own `FcNewItemIndicator.razor.cs` partial); `INewItemIndicatorStateService.Add(`/`Snapshot(` callers in `src/` = **0** (only the DI registration in `ServiceCollectionExtensions.cs:365` + the service's own definition). The only generated artifact referencing `FcNewItemIndicator` is the Razor compiler's own `FcNewItemIndicator_razor.g.cs` (the component compiling its own markup) — **no FrontComposer-generated projection view emits it**. Gap confirmed: the new-item indicator is an isolated, unwired primitive at `ea23187`.
- Nudge seam: `IProjectionHubConnection.OnProjectionChanged(Func<string,string,Task>)` carries only `(projectionType, tenantId)` — no per-row `EntityKey`/`MessageId`. `ProjectionSubscriptionService.OnProjectionChangedAsync` → `TriggerNudgeRefreshAsync` + pending-command `PollOnceAsync` (Story 5-5 DN1, the command-lifecycle path) — never calls `INewItemIndicatorStateService.Add`.

### Completion Notes List

**Scope:** Confirm-and-pin verification story. **Net `src/` change = ZERO** (both ACs). Work is `test:`-shaped: re-confirmed the existing live-update / reconnect / reconciliation surface and added two durable integration pins (7 new tests). Branch `test/story-2-6-live-projection-updates` (feature branch per Story 2.5 precedent / project-context "no direct commits to main").

**AC2 — VERIFIED end-to-end, ZERO `src/` change.**
- Re-confirmed the connection-status component is **mounted** in `FrontComposerShell.razor` (~L108) and pinned by `FcProjectionConnectionStatusTests` (6 tests — copy, precedence, stale-epoch, once-per-epoch, auto-clear). Re-confirmed reconnect→rejoin→reconcile engine (`ReconnectionReconciliationCoordinatorTests`, `ReconciliationSweepReducersTests`) and subscription/scheduler/telemetry suites.
- **New AC2 integration pin** (`ReconnectReconcileStatusIntegrationTests`, 2 tests) closes the one previously-isolated seam: wires the **real** `ReconnectionReconciliationCoordinator` → **real** `ReconnectionReconciliationStateService` → **real** `FcProjectionConnectionStatus`. Asserts a *changed* reconcile drives "Reconnected -- data refreshed" + emits a `MarkReconciliationSweepAction` for the changed lane + auto-clears after `ProjectionReconnectedNoticeDurationMs`; a *no-change* reconcile stays silent + emits no sweep marker.

**AC1(a) — live refresh half: VERIFIED, ZERO `src/` change.** Re-confirmed subscription→nudge→lane `RefreshAsync` (ETag-gated) via `ProjectionSubscriptionServiceTests` + fault suites + `ProjectionFallbackRefreshSchedulerTests`/`ProjectionFallbackPollingDriverTests`, and that the generated view registers its lane (generated `CounterProjection.g.razor.cs` emits `ProjectionFallbackRefreshScheduler.RegisterLane(...)`).

**AC1(b) — "new item indicator marks fresh rows": GAP RESOLVED VIA OPTION 3 (CONSCIOUS DEFERRAL — REQUIRES PO/REVIEW SIGN-OFF).**
- ⚠️ **AC1 is NOT fully satisfied end-to-end in `src/` by this story.** The `FcNewItemIndicator` component and `NewItemIndicatorStateService` exist and are unit-pinned **in isolation**, but at `ea23187` the indicator is **rendered by no `src/` view** and its producer (`Add`/`Snapshot`) has **no `src/` caller**.
- **Why deferred, not wired (option 2 rejected as dishonest/out-of-scope):** `NewItemIndicatorEntry` requires `EntityKey` + `MessageId`; its semantics ("confirmed created entity ... outside current filters") belong to the **command-lifecycle / pending-command resolution path (Story 5-5 DN1, Epic 3/5)**, which Story 2.6 explicitly does **not** reopen. The live nudge seam carries only `(projectionType, tenantId)` — no per-row identity — so producing the indicator from the projection read path would require either a SignalR wire-protocol change (out of scope — EventStore client swap not reopened) or fabricated identities + per-projection key-diffing + regenerating every projection's approval baseline. None of that is the "small and low-risk" wiring option 2 is meant for, and it cannot honour "marks the row that arrived during the live window outside current filters."
- **What was delivered instead (the durable, honest AC1(b) pin):** `FcNewItemIndicatorLaneIntegrationTests` (5 tests) proves the in-scope, testable half of the contract end-to-end: a `NewItemIndicatorEntry` pushed to `INewItemIndicatorStateService` for a lane surfaces a rendered `FcNewItemIndicator` (correct `role="status"`/`aria-live="polite"`/localized copy) **only for that lane**, and disappears on every dismissal trigger (10s TTL via `FakeTimeProvider`, materialization, filter-change). A minimal test-only `LaneHost` stands in for the eventual generated/shell-level consumer, reading `Snapshot(viewKey)` exactly as that consumer will.
- **Action for PO/review:** consciously **accept the AC1(b) deferral** (new-item marking lands with its command-lifecycle producer in Epic 3/5 / Story 5-5) **or** carve AC1(b) out of 2.6's acceptance. AC1(a) and AC2 are satisfied and durably pinned.

**Test counts (xUnit v3 in-process, filtered lane):**
- `Hexalith.FrontComposer.Shell.Tests`: **before 1740 total / 8 failed → after 1751 total / 8 failed** (+11 new pins across 4 files — 7 from dev-story, 4 from the subsequent `bmad-qa-generate-e2e-tests` pass — all green; **0 new failures**). _(Review-verified 2026-06-04: build 0/0 TWAE; the 4 new files' 11 tests pass; full filtered lane = 1751 total / 8 failed.)_
  - dev-story pins (7): `FcNewItemIndicatorLaneIntegrationTests` (5) + `ReconnectReconcileStatusIntegrationTests` (2).
  - QA-pass pins (4): `NudgeToSchedulerLaneRefreshIntegrationTests` (2) + `ReconnectReconcileSubscriptionIntegrationTests` (2).
- The 8 failures are the **documented pre-existing/environmental baseline**, unchanged: `PendingStatusReopenGovernanceTests` ×4 (deferred-work file-IO), `NavigationEffectsLastActiveRouteTests` ×1 (hydration), `CounterStoryVerificationTests` ×2 (Verify snapshot drift), `CommandRendererFullPageTests` ×1 (query-fallback). None are in any AC1/AC2 surface.
- AC1/AC2 confirm-target suites run in isolation: **100/100 passed**.
- `SourceTools.Tests`: **NOT touched** — no `RazorEmitter.cs` change was made (option 3, not option 2), so the documented 3-failure baseline is not re-run and the all-unannotated approval baseline (`CounterProjectionApprovalTests` / `CounterStoryVerificationTests.*.verified.txt`) is **byte-for-byte unchanged** (zero `.verified.txt` edits).

**Record-keeping (retro AI-1 / AI-2):** File List below reconciled against the git tree (2 new test files; no `src/` files). Sentinel scan of new test files for `</content>`/`</invoke>`/`<invoke`/tool tags → **clean**.

### File List

_New (tests only — zero `src/` change):_
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs` — AC1(b) producer-state→consumer-component integration pin (5 tests). _(dev-story)_
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconnectReconcileStatusIntegrationTests.cs` — AC2 coordinator→state→status integration pin (2 tests). _(dev-story)_
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/NudgeToSchedulerLaneRefreshIntegrationTests.cs` — AC1(a) nudge→real-scheduler→registered-lane refresh + routing-isolation pin (2 tests). _(QA pass `bmad-qa-generate-e2e-tests`)_
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/ReconnectReconcileSubscriptionIntegrationTests.cs` — AC2 full-chain pin: reconnect through the real `ProjectionSubscriptionService` → real coordinator → real scheduler → mounted `FcProjectionConnectionStatus` (2 tests). _(QA pass `bmad-qa-generate-e2e-tests`)_

_Tracking (not `src/`):_
- `_bmad-output/implementation-artifacts/2-6-live-projection-updates-with-reconnect-reconciliation.md` — checkboxes, Dev Agent Record, Change Log, Status.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story status → in-progress → review.
- `_bmad-output/implementation-artifacts/tests/2-6-test-summary.md` — QA test-automation summary (the 2 QA integration pins + coverage/validation evidence).

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated adversarial review — story-automator) · **Date:** 2026-06-04 · **Baseline:** `ea23187`
**Outcome:** **APPROVED → done.** Record corrected (retro AI-1) in the prior pass; the sole remaining gate — the AC1(b) deferral product decision — was **formally accepted by the Product Owner on 2026-06-04** (see PO Acceptance below). No CRITICAL/HIGH code blockers remain.

> **PO Acceptance (2026-06-04).** The Product Owner has **formally accepted** the **AC1(b)** ("new-item
> indicator marks fresh rows") **deferral to Epic 3/5 — Story 5-5**, because the live-nudge seam
> `OnProjectionChanged(projectionType, tenantId)` carries **no per-row identity** and the indicator's producer
> belongs to the command-lifecycle path. **AC1(a) and AC2 are implemented and durably pinned.** AC1(b) is
> recorded as **accepted-deferred** and **tracked as a Story 5-5 dependency**. With no other CRITICAL/HIGH
> blockers, Story 2.6 is **promoted to `done`**. The unwired `FcNewItemIndicator` / `NewItemIndicatorStateService`
> primitive remains confirmed and pinned in isolation + integration; its end-to-end wiring lands in Story 5-5.

### What was independently verified (not taken on the story's word)
- **Build:** `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/...csproj -c Release -m:1 /nr:false` → **0 Warning(s) / 0 Error(s)** (TWAE green).
- **New pins:** all **11** tests across the **4** new files pass (in-process xUnit v3 runner): `FcNewItemIndicatorLaneIntegrationTests` (5), `ReconnectReconcileStatusIntegrationTests` (2), `NudgeToSchedulerLaneRefreshIntegrationTests` (2), `ReconnectReconcileSubscriptionIntegrationTests` (2).
- **Full filtered lane:** `Hexalith.FrontComposer.Shell.Tests` = **1751 total / 8 failed**. The 8 failures are the **documented pre-existing/environmental baseline** — confirmed identity: `PendingStatusReopenGovernanceTests` ×4, `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute_DispatchesHydratedActions` ×1, `CounterStoryVerificationTests` ×2 (Verify drift), `CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` ×1. **None in any AC1/AC2 surface; zero new failures.**
- **AC2:** `FcProjectionConnectionStatus` is genuinely **mounted** at `FrontComposerShell.razor:108`. The new full-chain pin drives the **real** `ProjectionSubscriptionService` reconnect seam end-to-end. ✅ Implemented + durably pinned, ZERO `src/` change.
- **AC1(a):** live nudge → real scheduler → registered-lane refresh pinned end-to-end. ✅
- **AC1(b) gap is real and the deferral rationale is sound:** grep confirms `<FcNewItemIndicator` is rendered in **no** `src/` view and `INewItemIndicatorStateService.Add`/`Snapshot` have **no** `src/` caller (only the service definition + DI registration). The nudge seam `IProjectionHubConnection.OnProjectionChanged(Func<string,string,Task>)` carries only `(projectionType, tenantId)` — **no per-row identity** — so producing the indicator from the projection read path is not the "small, low-risk" wiring option 2 is for. Deferral (option 3) is the honest call.
- **Sentinels (retro AI-2):** new files clean. **House style:** no raw `Assert.*` (Shouldly throughout).

### Findings
- 🟡 **MEDIUM — File List / counts / Change Log were stale (retro AI-1, the exact recurring tax).** The dev-story record documented only its **own 2 pins** (7 tests, "1747/8") and omitted the **2 pins + test-summary added by the later `bmad-qa-generate-e2e-tests` pass** — leaving the File List, Completion-Notes counts, and Change Log out of sync with the actual git tree (4 new test files, 11 tests, 1751/8). **FIXED in this review:** File List now lists all 4 test files + the QA summary artifact; counts corrected to 1740 → 1751/8 (+11).
- ✅ **AC1(b) deferral — RESOLVED by PO acceptance (2026-06-04).** The gap is real (verified: `<FcNewItemIndicator` rendered in no `src/` view; `Add`/`Snapshot` have no `src/` caller; nudge seam carries no per-row identity), and per the story's mandate ("do **not** silently pass AC1") it was held for a conscious product decision rather than auto-fixed. The **Product Owner has formally accepted the deferral** to the command-lifecycle producer story (Epic 3/5 — Story 5-5); AC1(b) is recorded as **accepted-deferred** and **tracked as a Story 5-5 dependency**. No code change warranted. **Story promoted to `done`.**

### Action items
- [x] **[AI-Review][HIGH] PO decision — RESOLVED (2026-06-04):** PO **accepted** the AC1(b) "new-item indicator marks fresh rows" deferral to Epic 3/5 / Story 5-5 (tracked as a Story 5-5 dependency). AC1(a) + AC2 are done & pinned; Story 2.6 promoted to `done`. [ref: AC1 / Task 2 / Completion Notes / PO Acceptance]

## Change Log

| Date | Change | Author |
|---|---|---|
| 2026-06-04 | **PO acceptance → Story PROMOTED to `done`.** Product Owner **formally accepted** the **AC1(b)** ("new-item indicator marks fresh rows") **deferral to Epic 3/5 — Story 5-5** (live-nudge seam carries no per-row identity; indicator producer belongs to the command-lifecycle path). AC1(b) recorded as **accepted-deferred** + **tracked as a Story 5-5 dependency**; AC1 disposition note added to Acceptance Criteria; review Outcome updated to **APPROVED → done**, action item closed. Story-automator review re-verified no other CRITICAL/HIGH blockers (AC1(a) + AC2 implemented & durably pinned; 11 new pins green; sentinel-clean; ZERO `src/` change). **Status `review` → `done`; sprint-status synced.** | Jérôme Piquot (review / PO) |
| 2026-06-04 | **Senior Developer Review (AI).** Independently re-verified: build 0/0 TWAE; all 11 new tests (4 files) green; full filtered `Shell.Tests` lane = **1751 / 8** with the 8 failures confirmed as the documented pre-existing baseline (none in AC1/AC2 surface, zero new). AC2 + AC1(a) confirmed implemented & durably pinned end-to-end; AC1(b) gap + deferral rationale confirmed honest. **Corrected stale record (retro AI-1):** File List now includes the 2 QA-pass pins (`NudgeToSchedulerLaneRefreshIntegrationTests`, `ReconnectReconcileSubscriptionIntegrationTests`) + `tests/2-6-test-summary.md`; counts fixed to 1740 → 1751/8 (+11). **Status held at `review`** — promotion to `done` blocked on PO sign-off of the AC1(b) deferral. | Jérôme Piquot (review) |
| 2026-06-04 | Story 2.6 dev-story (confirm-and-pin). Verified AC1(a) live-refresh + AC2 reconnect/reconcile/status hold end-to-end against `src/` at `ea23187` with ZERO `src/` change. Added 2 durable integration pins (7 tests): `FcNewItemIndicatorLaneIntegrationTests` (AC1(b) producer-state→consumer-component contract) + `ReconnectReconcileStatusIntegrationTests` (AC2 real coordinator→state→status). **AC1(b) "new-item indicator marks fresh rows" consciously DEFERRED (option 3)** to the command-lifecycle producer story (Epic 3/5 / Story 5-5) — flagged for PO/review sign-off; the live nudge seam carries no per-row identity to produce the indicator. Build 0/0 TWAE; Shell.Tests 1740/8 → 1747/8 (+7 green, 0 new failures); approval baseline byte-for-byte unchanged. | Amelia (dev agent) |
