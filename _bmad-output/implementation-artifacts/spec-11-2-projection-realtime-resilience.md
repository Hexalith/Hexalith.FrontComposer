---
title: '11.2 Projection realtime resilience'
type: 'feature'
created: '2026-07-06T00:00:00+02:00'
status: 'done'
baseline_revision: 'd5fccfa7130e98b0db46b35473d80179bdcfcb27'
final_revision: 'df18fe838b5e72efda5e7d47f34ea5fc68992ed5'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** Projection SignalR currently relies on the finite default reconnect ladder and can settle into terminal `Closed`, leaving live grids dependent on slow fallback polling without an automatic realtime recovery path. Related in-flight disposal, registry live-list, and ETag cache seeding races can make reconnect/fallback behavior hang or miss persisted cache entries.

**Approach:** Add unbounded jittered SignalR retry plus service-owned restart-on-`Closed` gated by fallback availability, rejoin active groups after restart, bound disposal waits, align polling-driver disposal behavior, snapshot/lock registry reads, and serialize ETag cache LRU seeding with reset on failure.

## Boundaries & Constraints

**Always:** Preserve Story 11.1's centralized EventStore access-token provider and captured SignalR reconnect token path; keep changes inside Shell runtime/tests; keep logs sanitized to exception type or redacted keys; use existing Fluxor/EventStore/fallback seams; use `ConfigureAwait(false)` on awaited calls.

**Block If:** Restart-on-`Closed` cannot be implemented without a new public API/options surface or changing EventStore hub wire contracts; bounded disposal cannot avoid leaving a clearly corrupt active-group state; direct SignalR factory tests require live network/server infrastructure.

**Never:** Do not change generated command routes, Contracts kernel/package split, MCP behavior, SourceTools output, submodules, package versions, or EventStore server contracts. Do not log raw tenant/user/group payloads, tokens, stack traces, or cache keys.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Terminal hub close with fallback enabled | Active projection groups, hub emits `Closed` after default reconnect exhaustion, fallback polling interval is enabled | Service applies disconnected state, starts the hub again, rejoins active groups, and resumes realtime nudges without waiting for manual resubscribe | Startup/rejoin failures keep disconnected/degraded state and log sanitized failure category |
| Terminal hub close with fallback disabled | Active projection groups, hub emits `Closed`, fallback polling interval is disabled | Service applies disconnected state and does not spin an unbounded restart loop | No exception escapes the SignalR callback |
| Disposal during blocked work | Subscribe/rejoin/fallback/pending polling/cache seeding is in flight and ignores cancellation | Disposal completes within a bounded wait and unregisters callbacks/drivers best-effort | Sanitized warning only; no ObjectDisposedException escapes disposal |
| ETag seed failure then retry | First persisted-key enumeration fails or is canceled; later cache write/invalidation runs | Later caller retries seeding instead of treating the cache as permanently seeded | Failed seed logs redacted key-safe warning and resets the seeding gate |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` -- SignalR adapter, retry policy, hub method bindings, and direct factory test target.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` -- internal connection abstraction and connection-state contract.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` -- active group tracking, hub start/rejoin, `Closed` handling, fallback gate, and disposal gate.
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs` -- disconnected fallback driver whose lifecycle gates restart recovery.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingDriver.cs` -- second polling driver that must align disposal semantics.
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs` -- live manifest/nav/route/policy lists requiring snapshot or lock discipline.
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs` -- persisted LRU seeding path requiring serialized retryable seeding.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` -- service-level subscribe/reconnect/restart/disposal coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/ProjectionSubscriptionServiceFaultTests.cs` -- blocked-start/rejoin/disposal race coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs` -- new direct factory tests for retry policy and wire literals.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionFallbackPollingDriverTests.cs` -- fallback disposal and gate behavior coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingDriverTests.cs` -- pending-driver in-flight disposal coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/ETagCacheServiceTests.cs` -- seed coalescing/reset-on-failure coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs` -- registry snapshot/concurrent-read coverage.

## Tasks & Acceptance

**Execution:**
- [x] `SignalRProjectionHubConnectionFactory.cs` -- replace bare default reconnect with an internal unbounded jittered `IRetryPolicy`, centralize hub method-name constants, and expose testable seams without network dependency -- prevents terminal default-ladder exhaustion and pins wire literals.
- [x] `ProjectionSubscriptionService.cs` -- on terminal `Closed`, apply disconnected state and restart/rejoin active groups only when fallback polling is enabled/available; treat restart `Connected` like a rejoin epoch; bound gate waits during rejoin and disposal -- restores realtime without hanging disposal.
- [x] `ProjectionFallbackPollingDriver.cs` and `PendingCommandPollingDriver.cs` -- align disposal so both cancel, unregister, and bound/observe in-flight work consistently -- prevents background polling from outliving service disposal or blocking forever.
- [x] `FrontComposerRegistry.cs` -- add shared lock/snapshot discipline for manifest, nav, route, and policy lists, including `GetManifests`, `GetNavEntries`, `HasFullPageRoute`, `RegisterDomain`, and validation -- removes live-list enumeration races.
- [x] `ETagCacheService.cs` -- replace `_lruSeeded` one-way flag with serialized `Lazy<Task>` or semaphore-based seeding, coalesce concurrent seed callers, and reset on cancellation/failure -- fixes partial/permanent seed races.
- [x] `ProjectionSubscriptionServiceTests.cs`, `ProjectionSubscriptionServiceFaultTests.cs`, and `SignalRProjectionHubConnectionFactoryTests.cs` -- add closed-restart/rejoin, fallback-disabled no-restart, bounded-disposal, retry-policy, and wire-literal tests -- pins realtime recovery.
- [x] `ProjectionFallbackPollingDriverTests.cs`, `PendingCommandPollingDriverTests.cs`, `ETagCacheServiceTests.cs`, and `FrontComposerRegistryTests.cs` -- add focused race/disposal/cache/registry coverage -- pins non-H6 resilience findings.
- [x] `_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- update run result, completed tasks, validation evidence, and story status -- keeps BMAD artifacts consistent.

**Acceptance Criteria:**
- Given the projection hub connection drops beyond the default SignalR retry ladder, when fallback polling is enabled, then the client uses an unbounded jittered retry policy and a restart-on-`Closed` path that rejoins active groups and resumes realtime nudges.
- Given the projection hub connection drops beyond the default retry ladder, when fallback polling is disabled, then the service records disconnected state without creating an unbounded restart loop.
- Given `ProjectionSubscriptionService.DisposeAsync`, when startup, rejoin, fallback polling, pending polling, or cache seeding work is in flight, then disposal is bounded, callback registrations are released best-effort, both polling drivers follow aligned in-flight disposal semantics, registry reads use snapshots/locks, and failed cache seeding can retry.
- Given SignalR subscriptions are created and messages are handled, when factory and service tests run, then `ProjectionChanged`, `ProjectionChangedDetail`, `JoinGroup`, `JoinGroupScoped`, `LeaveGroup`, and `LeaveGroupScoped` are pinned without live network infrastructure.

## Spec Change Log

- 2026-07-06: Implemented Story 11.2 projection realtime resilience and moved story to in-review. Focused Shell validation passed; standard filtered solution lane is blocked by persistent unrelated SourceTools governance failure `DiagnosticRegistryTests.GovernanceEnumerations_AreDeterministicAcrossSurfaces`.
- 2026-07-06: Review pass patched closed-restart, disposal, cache-seeding, registry, and test-seam issues. No intent or spec rewrite was needed.
- 2026-07-06: Follow-up review pass applied 5 code patches (high 1, medium 1, low 3) to code inside `<intent-contract>` boundaries — closed-restart timeout now retries instead of aborting the unbounded loop; closed-restart reconcile runs off the gate; registry merge null-hardening; ETag seed skip-one-bad-key; disposed-race guard on the reconnect epoch — plus two new regression tests. One finding deferred (bounded-disposal connection/CTS leak on a wedged gate, a deliberate prior-pass tradeoff). No intent or spec rewrite was needed.

## Review Triage Log

### 2026-07-06 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 12: (high 5, medium 4, low 3)
- defer: 0
- reject: 5
- addressed_findings:
  - `[high]` `[patch]` Removed unrelated `references/Hexalith.EventStore` and `references/Hexalith.Memories` submodule movement from the story diff by checking root-declared submodules back to their pinned commits.
  - `[high]` `[patch]` Serialized closed-restart start/rejoin work through the subscription gate so it cannot double-start the SignalR connection against a concurrent subscribe.
  - `[high]` `[patch]` Added closed-restart retry behavior for transient restart and rejoin skips instead of leaving active groups detached after one failed attempt.
  - `[high]` `[patch]` Preserved sticky disconnected failure categories without suppressing later closed-restart recovery attempts.
  - `[high]` `[patch]` Kept closed recovery retrying after transient restart failure instead of stopping after the first failed attempt.
  - `[medium]` `[patch]` Changed bounded disposal so it does not clear active groups or stop/dispose the hub connection unless the operation gate is owned.
  - `[medium]` `[patch]` Added late-completion continuations for timed-out pending and fallback polling driver disposal so token sources are released when in-flight work eventually exits.
  - `[medium]` `[patch]` Made ETag persisted-LRU seeding retry when a per-key read fails, not only when key enumeration fails.
  - `[medium]` `[patch]` Added an `IOptionsMonitor<FcShellOptions>` path for closed-restart fallback gating while preserving existing `IOptions<FcShellOptions>` callers.
  - `[low]` `[patch]` Reduced registry hot-path allocation by using shallow locked snapshots for internal reads while keeping cloned snapshots for public `GetManifests`.
  - `[low]` `[patch]` Hardened registry manifest cloning against null runtime collections.
  - `[low]` `[patch]` Updated the projection subscription fake so restart tests exercise the production wrapper's `StartAsync` `Connected` publication and suppression path.

### 2026-07-06 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 5: (high 1, medium 1, low 3)
- defer: 1
- reject: 0
- addressed_findings:
  - `[high]` `[patch]` Closed-restart no longer permanently abandons the unbounded retry loop when one restart attempt hits the 10s per-attempt timeout: the timeout OCE is now distinguished from disposal and treated as a transient `RestartTimeout` that keeps retrying (skipping the redundant post-timeout backoff), so a slow-connecting server can no longer silently disable realtime recovery while a fast-failing one retried forever.
  - `[medium]` `[patch]` Closed-restart now runs the reconcile epoch (`SetReconciliationGroupHealth` / `Apply(Connected)` / `ReconcileAsync`) after releasing the operation gate rather than while holding it, so a recovery no longer blocks concurrent `SubscribeAsync`/`UnsubscribeAsync` for the reconcile duration — mirroring the `Reconnected` path.
  - `[low]` `[patch]` `FrontComposerRegistry.RegisterDomain` merge branch null-coalesces incoming `Projections`/`Commands`, matching the `Clone`/`ValidateManifests` null hardening this story already added, so a second registration of a bounded context with null collections no longer throws `ArgumentNullException` under the registry lock at startup (new regression test `RegisterDomain_MergeWithNullIncomingCollections_DoesNotThrow`).
  - `[low]` `[patch]` ETag persisted-LRU seeding skips a single failing/corrupt key instead of abandoning every key enumerated after it, while still returning not-seeded so a transient fault re-seeds next call; later keys stay inside eviction accounting (new regression test `SetAsync_WhenOnePersistedKeyReadFails_StillSeedsRemainingKeys`).
  - `[low]` `[patch]` `CompleteReconnectedEpochAsync` returns early once disposal has begun, so a gate-free rejoin→epoch handoff racing `DisposeAsync` no longer applies a `Connected` transition that leaves the shared connection state stuck reporting Connected during teardown.
  - Deferred (1): bounded `DisposeAsync` leaks the `HubConnection` (unbounded auto-reconnect) and `_disposalCts` when the operation gate cannot be acquired within the 2s bound — a deliberate prior-pass tradeoff backed by the `DisposeDuringBlockedSubscribe` bounded-wait test; tracked as a new entry in `deferred-work.md` for owner attention rather than reverting that decision.

### 2026-07-06 — Review pass (follow-up 2)
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 1, medium 0, low 2)
- defer: 2: (high 0, medium 1, low 1)
- reject: 5
- addressed_findings:
  - `[high]` `[patch]` Closed-restart could STILL permanently abandon the unbounded restart loop when the per-attempt `ClosedRestartTimeout` (10s) fired during the post-attempt backoff inside `DelayClosedRestartRetryAsync`'s `Task.Delay` — a call sited OUTSIDE the inner try/catch that H-F1 hardened. The resulting `OperationCanceledException` escaped to the outer generic `catch (OperationCanceledException)` → `RestartCanceled` → `return`, silently disabling realtime recovery on a slow-connecting server (the exact H-F1 failure class through the retry-delay path). Fixed by making `DelayClosedRestartRetryAsync` an instance method that swallows the per-attempt-timeout OCE (`when !_disposalCts.IsCancellationRequested`) so the loop re-evaluates and retries on the next iteration; this covers both the gate-unavailable and rejoin-skip callers. No deterministic timing test added — the 10s/1s waits use system-timer `CancelAfter`/`Task.Delay` with no injectable `TimeProvider` seam (the prior H-F1 patch likewise added none); verified by clean Release build + the existing closed-restart/fault/factory suite (73/73) for no happy-path regression.
  - `[low]` `[patch]` `RestartClosedConnectionAsync` now catches `ObjectDisposedException` in its outer handler. A concurrent disposal that disposed `_disposalCts` between the loop's stale `!_disposed` read and `CreateLinkedTokenSource(_disposalCts.Token)` previously threw an ODE that escaped the method (the "no exception escapes the callback" invariant was upheld only by the factory's outer per-handler guard, which logged a misleading "subscriber threw" warning during teardown). The method now upholds the invariant itself and logs a clean disposal cancellation.
  - `[low]` `[patch]` `RunBoundedDisposalOperationAsync` now catches `OperationCanceledException` (from the 2s bounded-disposal `timeout.Token` cancelling `StopAsync`) and logs it under the `TimeoutException` category rather than a generic "disposal operation failed", keeping disposal-timeout diagnostics accurate.
- deferred (2 NEW ledger entries only — existing entries untouched per orchestrator ownership):
  - `[medium]` Reconnected-epoch rejoin gives up with no retry on a 2s gate-timeout (asymmetric with the closed-restart retry loop) and can strand a live connection reporting `Disconnected` with active groups never rejoined; aggravated by last-writer-wins `Apply` with no epoch guard (partly pre-existing). Design fix (retry the reconnected rejoin and/or add an epoch guard) deferred.
  - `[low]` ETag LRU seeding re-scans all persisted keys on every write forever when a `:etag:` key is permanently unreadable (skip-one-bad-key keeps `_lruSeeded=0`, no cap/backoff); bounded by `MaxETagCacheEntries` and rare, so per-key-failure-accounting fix deferred.
- rejected (5): polling-driver `_disposalCts` retained only while a truly-hung task never completes (pathological premise, no OS-handle leak, mirrors the already-accepted deferred tradeoff); `SemaphoreSlim _lruSeedGate` not disposed (harmless — class is not `IDisposable`, no wait-handle allocated); `_restartConnectedStateSuppression` coupling to synchronous `Connected` publication (correct with the current factory; robustness observation); restart-on-`Closed` reachability/test-coverage observation (synthetic `Closed` is a valid unit approach, not a defect); duplicated `if (gateAcquired)` guard in the disposal `finally` (cosmetic, no behavioral impact).

## Design Notes

Keep restart recovery service-owned rather than server-owned: the EventStore hub wire contract stays unchanged, and the Shell decides whether fallback mode is active enough to attempt a restart. The restart path should reuse the existing active-group snapshot and access-token provider capture from Story 11.1 instead of reading ambient circuit context again.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProjectionSubscriptionServiceTests|FullyQualifiedName~ProjectionSubscriptionServiceFaultTests|FullyQualifiedName~FaultInjectingProjectionHubConnectionTests|FullyQualifiedName~ReconnectReconcileSubscriptionIntegrationTests|FullyQualifiedName~NudgeToSchedulerLaneRefreshIntegrationTests|FullyQualifiedName~SignalRProjectionHubConnectionFactoryTests"` -- passed: 77/77.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProjectionFallbackPollingDriverTests|FullyQualifiedName~ProjectionFallbackRefreshSchedulerTests|FullyQualifiedName~PendingCommandPollingDriverTests|FullyQualifiedName~PendingCommandPollingCoordinatorTests"` -- passed: 27/27.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~ETagCacheDiscriminatorTests|FullyQualifiedName~EventStoreQueryCacheIntegrationTests|FullyQualifiedName~FrontComposerRegistryTests"` -- passed: 70/70.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProjectionSubscriptionServiceTests|FullyQualifiedName~ProjectionSubscriptionServiceFaultTests|FullyQualifiedName~FaultInjectingProjectionHubConnectionTests|FullyQualifiedName~ReconnectReconcileSubscriptionIntegrationTests|FullyQualifiedName~NudgeToSchedulerLaneRefreshIntegrationTests|FullyQualifiedName~SignalRProjectionHubConnectionFactoryTests|FullyQualifiedName~ProjectionFallbackPollingDriverTests|FullyQualifiedName~ProjectionFallbackRefreshSchedulerTests|FullyQualifiedName~PendingCommandPollingDriverTests|FullyQualifiedName~PendingCommandPollingCoordinatorTests|FullyQualifiedName~ETagCacheServiceTests|FullyQualifiedName~ETagCacheDiscriminatorTests|FullyQualifiedName~EventStoreQueryCacheIntegrationTests|FullyQualifiedName~FrontComposerRegistryTests"` -- passed after review patches: 174/174.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- failed: unrelated SourceTools test `DiagnosticRegistryTests.GovernanceEnumerations_AreDeterministicAcrossSurfaces`; Shell passed 2080/2080, Contracts 177/177, CLI 67/67, MCP 358/358, Testing 30/30, SourceTools 1044/1045, Bench had no matching non-performance tests.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --filter "FullyQualifiedName~DiagnosticRegistryTests.GovernanceEnumerations_AreDeterministicAcrossSurfaces"` -- failed: persistent unrelated SourceTools `package-groups` shuffle assertion.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md` -- passed after submodule diff cleanup.
- `git diff --check` -- passed; only line-ending normalization warnings.

## File List

- `_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubRetryPolicy.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubWireContract.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/ProjectionSubscriptionServiceFaultTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/ETagCacheServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingDriverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionFallbackPollingDriverTests.cs`

## Auto Run Result

Status: done

Summary: Implemented projection realtime resilience for Story 11.2. SignalR projection hub connections now use an unbounded jittered retry policy and pinned wire-contract constants; terminal closed recovery is gated through fallback availability and rejoins active groups; subscription and polling disposal are bounded; registry reads use lock/snapshot discipline; ETag LRU seeding is serialized and retryable.

Review findings breakdown: 12 patch findings addressed (high 5, medium 4, low 3), 0 deferred, 5 rejected as duplicate/noise. Follow-up review is recommended because the review pass changed closed-restart and disposal behavior.

Verification: focused Story 11.2 Shell lane passed 174/174 after review patches; story artifact validation passed; `git diff --check` passed with line-ending normalization warnings only. Standard filtered solution lane still fails on unrelated persistent SourceTools governance test `DiagnosticRegistryTests.GovernanceEnumerations_AreDeterministicAcrossSurfaces`; Shell passed 2080/2080 in that broad run.

Residual risk: closed-restart retry remains intentionally scoped to the client subscription service and fallback-enabled active groups; the unrelated SourceTools governance failure remains outside this story.

### Follow-up review pass (2026-07-06)

Independent Blind Hunter + Edge Case Hunter review of the committed diff surfaced 6 unique findings (after dedup). Applied 5 patches and deferred 1 (no intent_gap, no bad_spec, no reject):
- `[high]` `[patch]` closed-restart per-attempt 10s timeout now retries instead of permanently abandoning the unbounded restart loop (a slow-connecting server previously killed realtime recovery while a fast-failing one retried forever).
- `[medium]` `[patch]` closed-restart reconcile epoch now runs after the operation gate is released, so recovery no longer blocks concurrent Subscribe/Unsubscribe (mirrors the Reconnected path).
- `[low]` `[patch]` registry merge branch null-coalesces incoming Projections/Commands; `[low]` `[patch]` ETag LRU seeding skips one failing key instead of stranding the rest; `[low]` `[patch]` reconnect epoch guards against a disposal race before applying Connected.
- Deferred (1): bounded `DisposeAsync` leaks the `HubConnection` (unbounded auto-reconnect) and `_disposalCts` when the gate can't be acquired within 2s — a deliberate prior-pass tradeoff backed by the `DisposeDuringBlockedSubscribe` bounded-wait test; recorded as a new entry in `deferred-work.md`.

Verification (follow-up): focused Story 11.2 Shell lane passed 176/176 (174 prior + 2 new regression tests: `RegisterDomain_MergeWithNullIncomingCollections_DoesNotThrow`, `SetAsync_WhenOnePersistedKeyReadFails_StillSeedsRemainingKeys`); full `Hexalith.FrontComposer.Shell.Tests` project passed 2080/2080 with the standard trait exclusions; Release build clean (0 warnings under `TreatWarningsAsErrors`). The unrelated SourceTools governance failure remains outside this story and was not re-run.

Follow-up review recommendation: true — the high-severity closed-restart control-flow change and the gate/reconcile restructuring are concurrency-sensitive and benefit from an independent follow-up review.

### Follow-up review pass 2 (2026-07-06)

Independent Blind Hunter + Edge Case Hunter review of the committed diff (baseline `d5fccfa` → HEAD) surfaced, after dedup, 4 unique defects plus 5 nits/observations. No intent_gap and no bad_spec — the intent contract and spec remained coherent, so no revert/loopback. Applied 3 patches (all in `ProjectionSubscriptionService.cs`), deferred 2, rejected 5:

- `[high]` `[patch]` Closed-restart still permanently abandoned the unbounded restart loop when the per-attempt 10s timeout fired during the post-attempt backoff delay (`DelayClosedRestartRetryAsync`'s `Task.Delay`, sited outside the inner try/catch H-F1 hardened): the OCE escaped to the outer generic OCE handler → `RestartCanceled` → return. `DelayClosedRestartRetryAsync` now swallows the per-attempt-timeout OCE (`when !_disposalCts.IsCancellationRequested`) so the loop retries — closing the H-F1 failure class through the retry-delay path.
- `[low]` `[patch]` `RestartClosedConnectionAsync` catches `ObjectDisposedException` from a disposal race on `_disposalCts.Token`, upholding the "no exception escapes the callback" invariant in the method rather than via the factory's outer guard.
- `[low]` `[patch]` bounded `RunBoundedDisposalOperationAsync` logs a `StopAsync` timeout-token cancellation as a `TimeoutException` category instead of a generic disposal failure.
- Deferred (2, NEW ledger entries only): (a) `[medium]` Reconnected-epoch rejoin has no retry on a 2s gate-timeout and can strand a live connection as `Disconnected` (aggravated by last-writer-wins `Apply`, partly pre-existing); (b) `[low]` ETag LRU seeding re-scans all persisted keys on every write when a `:etag:` key is permanently unreadable.
- Rejected (5): driver `_disposalCts` retention only under a truly-hung task; `SemaphoreSlim` not disposed (harmless, class not `IDisposable`); suppression-flag coupling to synchronous `Connected` publication; restart-on-`Closed` reachability/test-coverage observation; duplicated `if (gateAcquired)` disposal guard.

Verification (follow-up 2): Release build of `Hexalith.FrontComposer.Shell` clean (0 warnings under `TreatWarningsAsErrors`); focused lanes passed — `ProjectionSubscriptionServiceTests|ProjectionSubscriptionServiceFaultTests|FaultInjectingProjectionHubConnectionTests|SignalRProjectionHubConnectionFactoryTests` 73/73, `ReconnectReconcileSubscriptionIntegrationTests|NudgeToSchedulerLaneRefreshIntegrationTests|ProjectionFallbackPollingDriverTests|PendingCommandPollingDriverTests` 11/11 (84 total, 0 failures), all with `DiffEngine_Disabled=true`. No deterministic timing test was added for the high-severity fix: the 10s/1s restart waits use system-timer `CancelAfter`/`Task.Delay` with no injectable `TimeProvider` seam (the prior H-F1 patch added none for the same reason); the existing closed-restart suite pins the happy path against regression. The unrelated persistent SourceTools governance failure remains outside this story and was not re-run.

Follow-up review recommendation: true — the high-severity fix changes closed-restart loop control flow (the retry-delay OCE is now swallowed) in concurrency-sensitive code that could not be deterministically unit-tested, so an independent follow-up review pass is warranted.
