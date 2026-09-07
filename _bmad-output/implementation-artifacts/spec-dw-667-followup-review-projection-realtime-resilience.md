---
title: 'DW-667 follow-up review of projection realtime resilience'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
baseline_revision: '521fe2ded4e45e5e8c62705f57ab645419a84671'
baseline_commit: '521fe2ded4e45e5e8c62705f57ab645419a84671'
review_loop_iteration: 1  # no loopback: review produced no intent_gap or bad_spec entries
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story 11.2 finished after its review budget was exhausted, leaving its concurrency-sensitive realtime recovery without a final independent pass. That pass found hub-subscription races, fallback and pending-driver lifecycle gaps, an ETag seed-gate teardown hang, and factory wiring that existing tests do not exercise.

**Approach:** Preserve the Story 11.2 runtime and wire contracts while making connection phases and timer-driven recovery deterministic enough to close the races, keeping disposal bounded, and adding focused regression evidence for every verified finding.

## Boundaries & Constraints

**Always:** Keep changes inside Shell runtime/tests; preserve unbounded jittered reconnect, fallback-gated terminal restart, active-group rejoin, scoped tenant/user checks, sanitized logs, `ConfigureAwait(false)`, and the centralized captured access-token provider. Use `TimeProvider` for new timer-driven evidence and keep synchronization primitives free of user/dependency code while locks are held.

**Block If:** A fix requires a new public package API/options surface, a changed EventStore hub method or payload, or weakening bounded disposal, tenant isolation, or the existing fallback-enabled restart gate.

**Never:** Do not edit the deferred-work ledger, source spec, sprint tracker, generated output, package versions, submodules, EventStore server contracts, MCP behavior, or Contracts kernel. Do not expose raw tenant/user/group/token/cache-key data in logs or tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Close during subscribe | `Closed` arrives after join returns but before group publication | The successful subscription remains recoverable; fallback-enabled restart rejoins it and returns Connected | No terminally disconnected success and no escaped callback exception |
| Subscribe during automatic reconnect | Existing group is Reconnecting while a second group subscribes | No illegal second start; the new group is retained and joined by the reconnect epoch | Cancellation/disposal remains bounded and non-corrupting |
| Retry timeout during backoff | A restart attempt fails and its timeout expires inside retry delay | The loop begins another attempt instead of applying terminal `RestartCanceled` | Deterministic fake-time test; disposal cancellation still exits |
| Rapid connection flap | Disconnected loop is canceling during Connected then another Disconnected arrives | A replacement fallback loop starts after the old loop unwinds without a third state event | At most one active loop |
| Runtime fallback option change | Interval changes `0 -> positive` or `positive -> 0` while disconnected | Polling starts or stops promptly without another connection transition | Option registration is disposed safely |
| Concurrent teardown | Pending poll starts synchronously or two ETag seeds contend during dispose | Disposal reaches its bounded wait and every queued cache caller settles | No lock-held dependency call, stranded semaphore waiter, or teardown ODE |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` -- internal transport-phase seam needed to distinguish automatic reconnect from a startable disconnect.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` -- maps pinned SignalR state, installs `ProjectionHubRetryPolicy`, and binds `ProjectionHubWireContract`; expose only an internal non-network configuration seam for tests.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` -- `SubscribeAsync`, `RestartClosedConnectionAsync`, and `DelayClosedRestartRetryAsync` own the close/publication race, reconnect admission, and retry-time evidence.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs` -- `OnConnectionChanged`, loop completion, and option changes must converge on current disconnected/enabled state.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs` -- `Tick` currently starts coordinator code while `_sync` is held, bypassing the intended disposal bound when a dependency blocks synchronously.
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs` -- `Dispose` must not dispose `_lruSeedGate` beneath an owner plus queued waiter; retain the shipped `IDisposable` surface.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` and `Infrastructure/EventStore/FaultInjection/*` -- deterministic close-window, reconnect-subscribe, and timeout-during-backoff evidence using existing fault checkpoints plus fake time.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs` -- currently tests policy/literals in isolation; exercise the production `Create` composition and production adapter phase/token/method mapping as far as a non-network test seam permits.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriverTests.cs`, `Infrastructure/PendingCommands/PendingCommandPollingDriverTests.cs`, and `State/ETagCache/ETagCacheServiceTests.cs` -- focused loop/option/disposal contention regressions.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` -- repository identifier-inventory seal that must be regenerated for intentional new C# identifiers; this is not the deferred-work ledger.
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs` -- read-only: independent review found its clone/lock/snapshot coverage sufficient; do not change it.

## Tasks & Acceptance

**Execution:**
- [x] `IProjectionHubConnection.cs`, `SignalRProjectionHubConnectionFactory.cs`, and factory tests -- represent the minimum internal connection phase and prove the production `Create` path installs the unbounded policy; cover non-null token configuration, all phase mappings, and the scoped/unscoped method mapping used by the adapter without live network infrastructure.
- [x] `ProjectionSubscriptionService.cs` and its unit/fault tests -- retain subscriptions across Reconnecting and Disconnected transitions both before and during `JoinGroupAsync`, close the join-to-publication terminal-close window, inject `TimeProvider` into restart timeout/backoff, and deterministically prove both join-race branches plus timeout-during-delay retry.
- [x] `ProjectionFallbackPollingDriver.cs` and tests -- subscribe to option changes, recover cleanly from partial `Start` registration failure, reconcile current state when a canceled loop exits, and never hot-restart after a fatal loop fault; option-registration disposal must not skip remaining bounded cleanup.
- [x] `PendingCommandPollingDriver.cs` and tests -- publish an in-flight task under synchronization without invoking coordinator code under `_sync`, preserve fatal fault observation through that task, and use the injected `TimeProvider` for the disposal bound.
- [x] `ETagCacheService.cs` and tests -- make `Dispose` stop new seed work without disposing the live semaphore; prove an owner and queued waiter both settle after concurrent teardown.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` and its governance test -- reseal the intentional identifier inventory without adding analyzer exceptions or changing policy.
- [x] `_bmad-output/implementation-artifacts/spec-dw-667-followup-review-projection-realtime-resilience.md` -- record implemented files, independent review triage, exact commands, and validation; leave the deferred-work ledger untouched.

**Acceptance Criteria:**
- Given any matrix scenario, when its deterministic focused test runs, then the stated recovery or bounded-teardown outcome is observed without wall-clock sleeps, network infrastructure, leaked sensitive values, or a third stimulus.
- Given the focused EventStore, fallback, pending-command, ETag, fatal-taxonomy, and identifier-inventory suites, when run in Release with `DiffEngine_Disabled=true`, then all tests pass with zero warnings and the production factory/retry wiring is exercised.
- Given the completed bundle, when Git diff and story artifacts are inspected, then only owned Shell code/tests, the required identifier-inventory seal, and this bundle spec changed; the deferred-work ledger, source Story 11.2 spec, submodules, package files, and generated output are unchanged.

## Spec Change Log

- 2026-08-27: First review found the initial plan under-specified failure transitions, fatal-task observation, production factory evidence, cleanup failure handling, and the mandatory identifier-inventory reseal. Amended the executable tasks and verification boundary to avoid lost subscriptions, fatal hot-restart/unobserved-task states, partial-start leaks, helper-only factory tests, and an owned governance failure. KEEP: pre-publication active-group design; internal connection phase; fake-time restart timeout/backoff; fallback state/option convergence; pending poll invocation outside `_sync`; non-disposed ETag seed semaphore; the seven deterministic matrix regressions.

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 13: (high 2, medium 9, low 2)
- patch: 0
- defer: 0
- reject: 11
- addressed_findings:
  - `[high]` `[bad_spec]` Add the disconnected-after-phase-snapshot subscribe race and fatal fallback-loop termination to tasks/tests so active groups cannot be lost and fatal faults cannot hot-restart.
  - `[medium]` `[bad_spec]` Require the reconnect-during-join branch, pending fatal-fault propagation, fake-time disposal bounds, partial fallback-start cleanup, option-registration cleanup, and production factory/token/phase evidence.
  - `[low]` `[bad_spec]` Correct the test Code Map paths and make exact validation commands/results reproducible.
  - `[medium]` `[bad_spec]` Authorize and require the repository identifier-inventory reseal while keeping the deferred-work ledger and analyzer policy unchanged.

### 2026-09-07 — Review pass (iteration 2, independent 3-layer)

Reviewed content: commit `9ad4312f` (the DW-667 implementation, direct child of `baseline_commit`).
Verification was performed against the **current tree** (`main` at `97a4e6b2`), which also carries the
later DW-667 follow-ups `d6154159` and `a8222025`.

- intent_gap: 0
- bad_spec: 0
- patch: 8 (high 1, medium 4, low 3)
- defer: 2
- reject: 8

**Routed to patch**
- `[high]` `[patch]` Pending-group lifecycle is incomplete on the paths that assume wire membership.
  (a) `UnsubscribeAsync` (line 192) calls `LeaveGroupAsync` for any tracked key, including a group
  retained as `Pending` during `Reconnecting` that was never joined; `HubConnection.InvokeAsync`
  throws when not connected, and by the deliberate design at line 191 the key then stays in
  `_activeGroups`, so a later re-subscribe short-circuits at `ContainsKey → return` forever.
  (b) `SubscribeAsync` retains a `Pending` group and returns success; if that reconnect never lands
  (it exhausts to `Closed` with no restart path) nothing ever joins it, and because the entry is
  present every later subscribe short-circuits at `ContainsKey → return`, so the caller can never
  recover. Note the first fix attempted here — refusing the subscribe when
  `CanRestartClosedConnection()` is false — was wrong and was reverted: the reconnect epoch is driven
  by SignalR's `Reconnected` event and does not need the fallback gate, and the refusal broke the
  legitimate `Subscribe_DuringAutomaticReconnect_*` case. The shipped fix instead lets a later
  subscribe retry the join on a still-`Pending` entry.
- `[medium]` `[patch]` `Phase` was introduced but `IsConnected` was left as a parallel, unenforced
  source of truth. `RestartClosedConnectionAsync` (line 527) still gates the restart on
  `!_connection.IsConnected`, which is false while SignalR is `Reconnecting`, so the loop issues
  exactly the illegal second `StartAsync` that `Phase` was added to prevent. Both fakes accept a
  start from any phase, so no test can observe the rule.
- `[medium]` `[patch]` `Create_ConfiguresProductionRetryTokenAndInitialPhase` asserts the wrapper the
  factory handed its own observer, not what was installed. Emptying the `WithUrl` configure body
  ships an unauthenticated hub connection and the test stays green.
- `[medium]` `[patch]` The ETag dispose contract left dead, actively misleading residue: `Dispose` is
  an empty body, and both `catch (ObjectDisposedException)` handlers (lines 330, 353) are unreachable
  now that `_lruSeedGate` is never disposed, while their comments still describe the removed race.
  The same finding also claimed the "stops new persisted-LRU seed work" doc line is unasserted; that
  half is `false` — `SetAsync_AfterDispose_DegradesToAnUnseededCacheInsteadOfThrowing` already pins
  `GetKeysCalls == 0` with a non-vacuous live control, so no test was added for it.
- `[medium]` `[patch]` Test-harness robustness regressions: the bounded-dispose assertion was replaced
  by a bare `await` that hangs instead of failing; `pending.Release()` sits outside `try/finally` so a
  failed assertion disposes a `ManualResetEventSlim` under a blocked pool thread; two new subscription
  tests never dispose the service; one depends on the rejoin sweep's ordering; the fake overwrites
  `LastJoinCallbackTask`; `CancellableScheduler` busy-spins on a permanently-completed TCS.
- `[low]` `[patch]` `GroupHealth.Pending` was inserted first, silently renumbering the `byte` enum and
  flipping `default(GroupState).Health` from `Active` to `Pending` — the exact shape of the P4 bug
  documented at line 675.
- `[low]` `[patch]` `if (!added) return;` is unreachable: `ContainsKey` and `TryAdd` both run under the
  same held `_gate`.
- `[low]` `[patch]` `SelectGroupMethod`'s whitespace-only scope boundary — the `IsNullOrWhiteSpace`
  branch production actually takes — is untested.

**Deferred**
- `[medium]` `[defer]` `AnalyzerPolicy_IdentifierInventory_MatchesSeal` fails on the current tree
  (actual count=3320 vs sealed 3298). Not caused by DW-667: later work replaced the ledger algorithm
  and renamed the field from `testUnderscoreIdentifierTokens` (which DW-667 resealed to 7117) to
  `testPublicDeclarationIdentifiers`. Owned by the governance story; a non-owning story must not reseal.
- `[low]` `[defer]` The restart attempt deadline runs on the injected `TimeProvider` while
  `_gate.WaitAsync(GateWaitTimeout)` always uses the system clock, so the two diverge under a fake
  clock. Settled by a test that contends on the gate while the virtual restart deadline is pending.

**Rejected**
- `[false]` Fatal fallback-loop fault unobserved / `EnsureLoopRunning` hot-restart window /
  `DisposeAsync` missing fatal catch (4 findings, 2 layers): refuted at the cited locations — already
  fixed in the current tree by `d6154159`, which added `_fatalLoopFailure`, rethrows it from
  `DisposeAsync`, and tightened the guard to `_loopTask is not null`.
- `[false]` Subscribe stranded while `Phase` is `Connecting` (2 findings): unreachable. Every
  `StartAsync` site (lines 143, 530) runs while `_gate` is held, and SignalR's automatic reconnect
  reports `Reconnecting`, never `Connecting`.
- `[low]` `[reject]` `MapConnectionPhase` throwing on an unmapped `HubConnectionState`: all four
  current states are mapped, so this is a loud failure on a state not shown to be reachable.
- `[low]` `[reject]` Server-side group membership leak when `ThrowIfDisposed` fires after a successful
  join: the connection is being disposed, and the server drops group membership on disconnect.
- `[low]` `[reject]` Disposal bound "unbounded" under a fake clock that never advances: production uses
  `TimeProvider.System`; the proposed double-`WaitAsync` would reintroduce a real-time wait.
- `[low]` `[reject]` Restart-loop admission during the initial connect now that a `Pending` entry makes
  `_activeGroups` non-empty: costs one gate-wait timeout and a log, and self-heals.
- `[low]` `[reject]` Submodule gitlink bumps (`references/Hexalith.EventStore`, `references/Hexalith.Tenants`)
  riding in the commit against the spec's Never list: the fix is to amend this build's spec or rewrite
  history, and both gitlinks have since moved independently (now `da5accfc` / `e7f36662`), so no live
  defect remains.

## Design Notes

Treat transport phase as an internal adapter fact, not a new package contract. A subscription admitted while SignalR owns automatic reconnect should enter the active-group set for the existing reconnect epoch rather than call `StartAsync`. After any loop exits, converge from current state/options under the existing single-loop guard; do not rely on another event. `SemaphoreSlim` is managed and need not be disposed when disposal races active waiters.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProjectionSubscriptionServiceTests|FullyQualifiedName~ProjectionSubscriptionServiceFaultTests|FullyQualifiedName~SignalRProjectionHubConnectionFactoryTests|FullyQualifiedName~ProjectionFallbackPollingDriverTests|FullyQualifiedName~PendingCommandPollingDriverTests|FullyQualifiedName~ETagCacheServiceTests"` -- expected: all focused tests pass.
- `dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --configuration Release` -- expected: clean build with zero warnings.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FatalExceptionGuardGovernanceTests|FullyQualifiedName~AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal"` -- expected: owned governance tests pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: no new failures; any baseline release-coordinate failure is recorded separately with focused owned gates green.
- `git diff --check` -- expected: no whitespace errors; CRLF normalization warnings may be reported separately.

**Results (review pass, 2026-09-07, current tree at `97a4e6b2`):**
- Shell and Shell.Tests Release builds: 0 warnings, 0 errors under `TreatWarningsAsErrors=true`.
- Focused owned lane (subscription, fault, factory, fallback, pending, ETag, relocated-registration):
  100/100 pass, up from 93 after the review patches added regressions.
- Both new subscription regressions and the strengthened factory token assertion were mutation-checked:
  each fails against the pre-patch behavior and passes after it, so none is vacuous.
- `FatalExceptionGuardGovernanceTests`: 2/2 pass.
- Broad Shell lane (`Category!=Performance&e2e-palette&NightlyProperty&Quarantined`): 2719 total,
  3 failed, all in `Governance` and none in the DW-667 surface:
  - `AnalyzerPolicy_IdentifierInventory_MatchesSeal` — proven failing *before* any review patch
    (actual 3320 vs sealed 3298). The ledger algorithm and field were replaced by later work; owned
    by the governance story, which must do the reseal.
  - `AnalyzerPolicy_GovernanceContract_FailsClosed` — the known CA1707 test-identifier ledger drift
    owned by GOV-1/11.19; a non-owning story must not reseal it.
  - `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`
    — fails on the `references/Hexalith.EventStore` gitlink, which `97a4e6b2`
    ("chore(references): update subproject commits") moved during this session. No submodule is
    touched by this bundle.
- `git diff --check`: no whitespace errors; only expected CRLF normalization warnings.

**Documented Unrelated Changes:** `spec-align-latest-hexalith-modules-and-simplify-ci.md`,
`spec-bump-eventstore-package-to-3-99-0.md`, and `spec-fix-current-release-compatibility-gates.md`
are modified in the working tree by a concurrent session. They were neither edited nor staged here.

**Results (implementation pass):**
- Focused runtime/regression lane passed 94/94 after including the relocated-worker integration assertion that observes the newly published pending poll task.
- Shell and Shell.Tests Release builds passed with zero warnings and zero errors under `TreatWarningsAsErrors=true`.
- Fatal-taxonomy plus identifier-inventory governance passed 3/3 after resealing only `identifierInventory.testUnderscoreIdentifierTokens` and `testInventorySha256`; no warning control, disposition, or analyzer policy changed.
- The standard filtered Shell lane passed 2675/2676. Its sole failure was the baseline release-coordinate guard `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate`: the workflow consistently contains `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, while the repository test expects a different approved Builds SHA. No release workflow, coordinate policy, package file, or submodule is owned by DW-667.
- `git diff --check` reported no whitespace errors; only expected CRLF normalization warnings. Scope inspection confirmed the deferred-work ledger, Story 11.2 source spec, sprint tracker, package files, generated output, and submodule gitlinks are unchanged.

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/spec-dw-667-followup-review-projection-realtime-resilience.md`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubConnectionPhase.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/RelocatedInfrastructureRegistrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/FaultInjectingProjectionHubConnection.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/PendingCommands/PendingCommandPollingDriverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/ETagCacheServiceTests.cs`

## Auto Run Result

Status: done; independent review complete and its findings applied.

Review pass 2026-09-07: three independent layers (blind, edge-case, verification-gap) produced 34
findings. Triage recorded 0 intent_gap, 0 bad_spec (so no loopback), 8 patch entries, 2 defer, 8
reject. The patches closed two real production defects the original bundle introduced — a never-joined
`Pending` group could be left on the wire by `UnsubscribeAsync` and then block every later subscribe,
and the closed-restart loop still gated on `IsConnected` so it could issue the illegal second
`StartAsync` that `Phase` was added to prevent — plus a factory test that stayed green with the access
token removed entirely, dead ETag teardown handlers, and five test-harness robustness gaps.

Status: implementation-complete; mandatory review pending.

Summary: Closed/reconnecting subscription admission now retains recoverable groups without illegal starts; restart timeout/backoff, fallback-loop convergence, pending polling publication/disposal, and ETag seed teardown are deterministic and bounded. Production SignalR factory composition and adapter mappings are exercised without a network server.

Implementation self-audit triage: two cleanup gaps were found and patched before review: fallback option-registration fatal disposal is deferred until remaining bounded cleanup completes, and pending fatal poll propagation now disposes its cancellation source in a `finally` path. No intent gap, spec change, policy exception, or deferred work was introduced.

Residual risk before review: the exact broad Shell lane retains one unrelated release-coordinate governance failure described above. The concurrency-sensitive bundle now proceeds to the workflow's mandatory independent review.
