---
title: 'DW-667 follow-up review of projection realtime resilience'
type: 'bugfix'
created: '2026-08-27'
status: 'in-review'
baseline_revision: '521fe2ded4e45e5e8c62705f57ab645419a84671'
baseline_commit: '521fe2ded4e45e5e8c62705f57ab645419a84671'
review_loop_iteration: 1
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

## Design Notes

Treat transport phase as an internal adapter fact, not a new package contract. A subscription admitted while SignalR owns automatic reconnect should enter the active-group set for the existing reconnect epoch rather than call `StartAsync`. After any loop exits, converge from current state/options under the existing single-loop guard; do not rely on another event. `SemaphoreSlim` is managed and need not be disposed when disposal races active waiters.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ProjectionSubscriptionServiceTests|FullyQualifiedName~ProjectionSubscriptionServiceFaultTests|FullyQualifiedName~SignalRProjectionHubConnectionFactoryTests|FullyQualifiedName~ProjectionFallbackPollingDriverTests|FullyQualifiedName~PendingCommandPollingDriverTests|FullyQualifiedName~ETagCacheServiceTests"` -- expected: all focused tests pass.
- `dotnet build src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --configuration Release` -- expected: clean build with zero warnings.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FatalExceptionGuardGovernanceTests|FullyQualifiedName~AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal"` -- expected: owned governance tests pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: no new failures; any baseline release-coordinate failure is recorded separately with focused owned gates green.
- `git diff --check` -- expected: no whitespace errors; CRLF normalization warnings may be reported separately.

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

Status: implementation-complete; mandatory review pending.

Summary: Closed/reconnecting subscription admission now retains recoverable groups without illegal starts; restart timeout/backoff, fallback-loop convergence, pending polling publication/disposal, and ETag seed teardown are deterministic and bounded. Production SignalR factory composition and adapter mappings are exercised without a network server.

Implementation self-audit triage: two cleanup gaps were found and patched before review: fallback option-registration fatal disposal is deferred until remaining bounded cleanup completes, and pending fatal poll propagation now disposes its cancellation source in a `finally` path. No intent gap, spec change, policy exception, or deferred work was introduced.

Residual risk before review: the exact broad Shell lane retains one unrelated release-coordinate governance failure described above. The concurrency-sensitive bundle now proceeds to the workflow's mandatory independent review.
