# Story 5.7: SignalR Fault Injection Test Harness

Status: ready-for-dev

> **Epic 5** - Reliable Real-Time Experience. **FR82 / NFR53 / NFR59** reusable deterministic fault injection for the SignalR and degraded-network seams built by Stories 5-1 through 5-6. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, **L12**, and **L14**.

---

## Executive Summary

Story 5-7 turns the ad hoc fakes created during Stories 5-3 through 5-6 into a reusable, deterministic test harness for realtime failure modes:

- Reuse the Shell-owned `IProjectionHubConnection` abstraction. The harness must not mock `HubConnection` directly in production tests and must not expose SignalR types through Contracts.
- Simulate connection drop, connection delay, partial nudge delivery, duplicate nudges, and message reorder at precise awaitable checkpoints.
- Let tests stage failures around command lifecycle states, projection nudges, reconnect/rejoin, fallback polling, pending command resolution, and telemetry.
- Keep the harness in the existing test infrastructure for v0.1. Prepare an extraction seam for the future `Hexalith.FrontComposer.Testing` package, but do not create or publish a new package unless the current repo already has the packaging lane.
- Preserve the EventStore contract: SignalR carries lightweight `ProjectionChanged(projectionType, tenantId)` nudges only; REST + ETag remains the data path.
- Make timing deterministic through `TimeProvider` / `FakeTimeProvider` and explicit checkpoint tasks. No real sleeps, live SignalR server, live EventStore, Docker, Dapr sidecar, browser, or network should be required for the unit/component harness.
- Close known gaps from earlier stories: Story 2-4 disconnected lifecycle coverage, Story 3-7 Aspire/CI harness handoff, Story 5-3 SignalR wrapper and race tests, Story 5-4 reconnect/reconciliation faults, Story 5-5 out-of-order command outcomes, and Story 5-6 telemetry failure-path evidence.

The intended implementation shape is a small test-support namespace with a scriptable `FaultInjectingProjectionHubConnection`, builder APIs for common scenarios, checkpoint primitives (`BlockUntil`, `Release`, `DropNext`, `DelayNext`, `ReorderNext`), and focused tests proving existing realtime components can consume it.

---

## Story

As a developer,
I want a test harness that simulates SignalR connection faults without requiring a live server,
so that I can write reliable unit and integration tests for all reconnection and resilience behaviors.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A test configures the SignalR fault harness | The test stages a connection fault | The harness supports connection drop, connection delay, partial message delivery, duplicate nudge delivery, message reorder, join/leave failure, start failure, closed-after-retry, and cancellation/disposal races. |
| AC2 | A test needs a fault at a precise lifecycle point | The test awaits a named checkpoint such as before `StartAsync`, before `JoinGroupAsync`, before `ProjectionChanged`, during `Syncing`, or during reconnect rejoin | The harness blocks deterministically until the test releases or faults the checkpoint; no wall-clock sleep is required to coordinate the scenario. |
| AC3 | A connection drop is simulated during a command's `Syncing` state | `FcLifecycleWrapper`, projection connection state, and fallback polling observe the event | Tests can assert immediate timeout escalation, form state preservation, no auto-confirm on reconnect, fallback query activation, and prompt stop on reconnect/disposal. |
| AC4 | A nudge or command outcome is reordered | `ProjectionChanged` arrives before command acknowledgment, after reconnect but before rejoin, or after a duplicate terminal outcome | Tests can assert lifecycle and pending-command state machines reject invalid transitions, deduplicate terminal outcomes, and never replay commands automatically. |
| AC5 | Partial delivery is simulated | Some active groups receive a nudge, some are dropped, and some rejoin attempts fail | Tests can assert degraded-group behavior, visible-lane-only reconciliation, fallback polling for unresolved lanes, and no cross-tenant/cross-projection leakage. |
| AC6 | The harness is used with current test infrastructure | Tests run under xUnit, bUnit, Shouldly, NSubstitute, `FakeTimeProvider`, Fluxor harnesses, and existing Shell test projects | No live SignalR server, live EventStore, live network, Dapr sidecar, Docker, browser, Playwright install, or Aspire process is required for the deterministic lane. |
| AC7 | The harness is prepared for adopter use | A future `Hexalith.FrontComposer.Testing` extraction occurs | Public/test-support APIs already follow a builder pattern, avoid internal production types where possible, keep synthetic test data non-PII, and document which APIs are stable versus v0.1 internal. |
| AC8 | CI runs realtime resilience tests | The default deterministic lane executes | At least 90 percent of FR24-FR29 reconnection/resilience behavior is testable at unit/component level through the harness or story-local consumers, with any remaining live-browser or Aspire smoke tests explicitly tagged and non-duplicative. |
| AC9 | Fault-path diagnostics and telemetry are captured | Harness scenarios trigger reconnect, rejoin failure, fallback polling, pending outcome resolution, and telemetry/log paths | Tests can assert structured logs/spans are redacted, no raw tenant/user/token/group/payload/problem detail leaks, and failures remain bounded categories. |

---

## Tasks / Subtasks

- [ ] T1. Design the fault-harness API and v0.1 location (AC1, AC2, AC6, AC7)
  - [ ] Place the first implementation under `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/` or an equivalent test-support folder.
  - [ ] Name the primary fake `FaultInjectingProjectionHubConnection` or similar and have it implement `IProjectionHubConnection`.
  - [ ] Add a builder such as `ProjectionHubFaultScenarioBuilder` with readable scenario setup: start connected, start failed, reconnecting, reconnected, closed, join failure, leave failure, drop nudge, duplicate nudge, reorder nudges.
  - [ ] Keep extraction seams clear for a future `Hexalith.FrontComposer.Testing` package, but do not create the package in this story unless the repo already has a first-class packaging path for it.
  - [ ] Do not expose or require `Microsoft.AspNetCore.SignalR.Client.HubConnection` from tests that use the harness.

- [ ] T2. Add deterministic checkpoint primitives (AC2, AC6, AC8)
  - [ ] Provide named checkpoints for `StartAsync`, `JoinGroupAsync`, `LeaveGroupAsync`, connection-state publication, nudge publication, fallback trigger, and disposal.
  - [ ] Provide methods such as `BlockUntil(checkpoint)`, `Release(checkpoint)`, `FailNext(checkpoint, exception)`, `CancelNext(checkpoint)`, and `WaitFor(checkpoint, count)`.
  - [ ] Implement all coordination with `TaskCompletionSource` created with `RunContinuationsAsynchronously`.
  - [ ] Accept cancellation tokens and propagate them exactly as the production abstraction expects.
  - [ ] Add timeout helpers only for test failure diagnostics; production behavior must not depend on real sleeps.

- [ ] T3. Model connection states and retry outcomes (AC1, AC3, AC5, AC8)
  - [ ] Simulate `Connected`, `Reconnecting`, `Reconnected`, and `Closed` through `ProjectionHubConnectionStateChanged`.
  - [ ] Preserve Microsoft SignalR semantics used by the app: automatic reconnect is opt-in, default reconnect attempts are 0, 2, 10, and 30 seconds before stopping, `Reconnecting` fires before attempts, `Reconnected` fires after success with a new connection id, and failed automatic reconnect ends in `Closed`.
  - [ ] Keep initial start failure distinct from reconnect failure, matching Story 5-3's sticky `InitialStartFailed` behavior.
  - [ ] Support join/leave calls with per-group results so tests can mark one group `Degraded` while another remains `Active`.
  - [ ] Track active groups in the fake only as test observation. `ProjectionSubscriptionService` remains the production source of truth.

- [ ] T4. Model nudge delivery faults without data payloads (AC1, AC4, AC5, AC9)
  - [ ] Provide `PublishProjectionChanged(projectionType, tenantId)` helpers for normal nudges.
  - [ ] Provide drop, duplicate, partial delivery, delayed delivery, and reorder behaviors over nudge events.
  - [ ] Validate or fail closed on blank/colon-containing projection and tenant segments when configuring scenario helpers.
  - [ ] Never include projection payloads, command payloads, raw tenant/user data, or ProblemDetails bodies in harness events or diagnostics.
  - [ ] Add assertions or helper guards proving the harness only publishes lightweight nudges.

- [ ] T5. Refactor existing story-local fakes to consume the harness (AC3, AC5, AC8)
  - [ ] Replace or adapt the inline `FakeProjectionHubConnection` in `ProjectionSubscriptionServiceTests` with the reusable harness.
  - [ ] Preserve existing tests: subscribe commit-after-join, nudge tenant route, rejoin exactly once, initial start failure, join failure, unsubscribe leave failure, tenant-aware notifier, subscriber isolation, degraded-group skip, and redacted rejoin logs.
  - [ ] Add race tests that were deferred from Story 5-3: duplicate subscribe/unsubscribe during reconnect, dispose during rejoin, failed rejoin stays degraded until next successful reconnect, and callback suppression after disposal.
  - [ ] Add SignalR wrapper coverage that was deferred from Story 5-3 only if testable without reflection/unsupported `HubConnection` mocking. Otherwise prove the wrapper boundary through the factory plus harness and document why direct private-wrapper tests remain out of scope.

- [ ] T6. Add lifecycle and form-preservation harness scenarios (AC3, AC4, AC8)
  - [ ] Extend `FcLifecycleWrapperDisconnectedTests` or adjacent bUnit tests to drive disconnect/reconnect through the harness-backed connection state.
  - [ ] Assert Syncing disconnect escalates immediately, reconnect alone does not confirm, and terminal confirmed ignores later connection loss.
  - [ ] Add generated command-form preservation tests if Story 5-5 did not already close them: edited field values and validation state survive disconnect/reconnect and degraded rejection.
  - [ ] Keep form-state tests in-process and deterministic; do not use Playwright for this unit/component lane.

- [ ] T7. Add reconnect, reconciliation, polling, and pending-outcome scenarios (AC4, AC5, AC8, AC9)
  - [ ] Provide tests for 5-4 visible-lane-only reconciliation under reorder and partial nudge delivery.
  - [ ] Provide tests for 5-5 pending command outcome ordering: projection nudge before ack, duplicate terminal outcome, rejected during degraded network, and unresolved ambiguity.
  - [ ] Provide tests for fallback polling: ETag validator usage, cleanup on reconnect/dispose, 304 no-churn, and 429/503 preserving visible data.
  - [ ] Use the existing `ProjectionFallbackRefreshScheduler`, `ProjectionFallbackPollingDriver`, `IProjectionPageLoader`, and EventStore response classifier seams. Do not add a second polling loop or HTTP classifier.

- [ ] T8. Add telemetry and redaction fault-path tests (AC9)
  - [ ] Reuse Story 5-6 `ActivitySource`/logger capture helpers when available.
  - [ ] Trigger start failure, reconnect failure, rejoin failure, nudge handler exception, fallback polling failure, and pending outcome failure through the harness.
  - [ ] Assert logs/spans contain bounded fields such as projection type, redacted tenant marker, connection state, failure category, reconnect attempt, and outcome where policy allows.
  - [ ] Assert absence of bearer tokens, raw access tokens, raw tenant/user values, raw group strings, command/query/cache payloads, raw exception messages, and raw ProblemDetails bodies.

- [ ] T9. CI and documentation integration (AC6, AC7, AC8)
  - [ ] Keep deterministic harness tests in the default unit/bUnit lane unless they require a separate trait for runtime.
  - [ ] If any live Aspire/browser smoke test is added, tag it separately (for example `Category=signalr-live-smoke`) and keep it small: one TCP/server-style reconnect sanity check, not a duplicate of unit coverage.
  - [ ] Reuse the `e2e-palette` trait pattern from Story 3-7 if a separate lane is needed.
  - [ ] Add a concise developer note or test-support README explaining how to stage common scenarios.
  - [ ] If CI wiring for `aspire` is still unavailable, document that live-browser parity remains a follow-up and keep AC8 satisfied by deterministic unit/component coverage.

- [ ] T10. Tests and verification (AC1-AC9)
  - [ ] Harness unit tests: connection drop, delay, partial delivery, duplicate nudge, reorder, join/leave failure, cancellation, disposal, checkpoint timeout diagnostics, and handler isolation.
  - [ ] Consumer tests: `ProjectionSubscriptionService`, connection state, lifecycle wrapper, fallback scheduler/driver, reconnect reconciliation if present, pending command resolver if present, and telemetry/redaction.
  - [ ] Accessibility-adjacent assertions: disconnected and reconnecting UI remains non-modal, no overlay/focus trap, status messages retain `role="status"` / `aria-live="polite"` where existing components own those attributes.
  - [ ] Regression suite: run `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` and targeted Shell tests for EventStore/projection/lifecycle/fallback paths. Run full solution tests if unrelated local work is not already dirty/failing.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` | Internal abstraction over SignalR with projection nudge and connection-state callbacks. | Harness implements this seam; do not leak SignalR client types into Contracts. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Builds real `HubConnection`, wires `WithAutomaticReconnect`, publishes `Connected/Reconnecting/Reconnected/Closed`, isolates handler exceptions. | Do not replace production wrapper. Harness verifies consumers through the abstraction. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Owns active groups, commit-after-join, degraded rejoin health, nudge routing, and fallback driver startup. | Production group ownership remains here; fake group tracking is observation only. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs` | Scoped connection-state service with logical dedupe and handler isolation. | Tests may drive transitions, but production dedupe and subscriber semantics stay unchanged. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs` | Runs one bounded polling loop while disconnected. | Harness can trigger disconnected/reconnected states; do not create another loop. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` | Contains inline `FakeProjectionHubConnection` and already covers several Story 5-3 fixes. | Migrate fake behavior into reusable harness without losing assertions. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperDisconnectedTests.cs` | Covers immediate Syncing disconnect escalation and no auto-confirm on reconnect. | Extend with harness-driven scenarios; keep exact user copy assertions. |
| `.github/workflows/ci.yml` | Default, `e2e-palette`, and Performance lanes exist; build-and-test may be hardened by Story 5-6. | Do not make live SignalR/Aspire/browser tests part of the default lane unless they are deterministic and cheap. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-7 decision |
| --- | --- | --- | --- |
| SignalR hub abstraction | Story 5-1 / 5-3 | Fault harness | Implement `IProjectionHubConnection`; never mock real `HubConnection` in business tests. |
| Projection connection states | Story 5-3 | Lifecycle, fallback, reconnect tests | Harness publishes state changes using existing record types. |
| Fallback polling loop | Story 5-3 | Fault scenarios | Drive through connection state; do not create another scheduler. |
| Reconnect reconciliation | Story 5-4 | Partial/reorder scenarios | Test visible-lane and no-churn rules through existing query/cache seams. |
| Pending command resolution | Story 5-5 | Out-of-order/idempotent scenarios | Harness stages nudge/outcome ordering; no command replay. |
| Observability/redaction | Story 5-6 | Fault-path telemetry tests | Reuse logger/span capture helpers and bounded failure categories. |
| Testing package future | Architecture v0.3 | Adopter test host | Prepare API shape now; package extraction remains future work unless already supported. |
| CI lane pattern | Story 3-7 | 5-7 test categorization | Reuse deterministic default lane first; separate live smoke only if necessary. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | The primary harness implements `IProjectionHubConnection`. | This is the production testability seam and avoids unsupported `HubConnection` mocking. | Mock private `HubConnection`; expose SignalR types through Contracts. |
| D2 | Harness scenarios are deterministic checkpoint scripts. | Reconnection races must be reproducible in CI. | Use `Task.Delay` sleeps; rely on live network timing. |
| D3 | Fault events remain nudge-only. | FrontComposer data correctness comes from REST + ETag after nudges. | Push projection payloads through the fake hub. |
| D4 | Test support starts inside Shell.Tests in v0.1. | Architecture says the Testing package is a v0.3 extraction seam; forcing a package now adds process overhead. | Create/publish `Hexalith.FrontComposer.Testing` immediately; leave every fake inline. |
| D5 | The fake may track groups for assertions, but production group truth stays in `ProjectionSubscriptionService`. | Prevents tests from encoding a second source of truth. | Add production group registry to the harness; let components own subscriptions. |
| D6 | Partial/reorder/drop behavior is per-event and per-group. | Story 5-4 and 5-5 failures happen in specific lanes and command epochs. | Global "hub is flaky" flags that make scenarios ambiguous. |
| D7 | `FakeTimeProvider` and `TimeProvider` are mandatory for timers and delays. | Microsoft guidance recommends injected time for deterministic, leak-free time tests. | Real sleeps and wall-clock timers. |
| D8 | Live Aspire/browser/TCP smoke is optional and small. | NFR59 asks for 90 percent unit-level coverage; live tests should not duplicate deterministic coverage. | Move all reconnect validation to Playwright/toxiproxy. |
| D9 | Redaction assertions are first-class harness consumers. | Fault paths are where raw exception and group data leak. | Treat diagnostics as manual inspection. |
| D10 | Builder APIs must be extraction-friendly. | FR71/Epic 10 expects adopter-facing test host utilities later. | Use anonymous delegates and repo-private magic strings everywhere. |

### Library / Framework Requirements

- Target current repo package lines and TFMs: .NET 10, Blazor, Fluxor, Fluent UI Blazor, Microsoft.AspNetCore.SignalR.Client 10.0.6, xUnit v3, bUnit, Shouldly, NSubstitute, Verify.XunitV3, FsCheck, and Microsoft.Extensions.TimeProvider.Testing.
- Use `TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously` for checkpoints.
- Use `FakeTimeProvider` for timer-driven scenarios; inject `TimeProvider` rather than using `DateTimeOffset.UtcNow` or real delays.
- Keep all harness data synthetic and non-PII. Use projection names like `orders` and tenant markers like `tenant-a` only when testing redaction explicitly.
- Do not add Playwright or browser binaries for the deterministic harness. If a live smoke is added, isolate it in a separate category.

External references checked on 2026-04-26:

- Microsoft Learn: ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client
- Microsoft Learn: Testing with FakeTimeProvider: https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/*` | Fault harness, scenario builder, checkpoints, and harness unit tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` | Replace inline fake and add race/degraded/disposal scenarios. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/*` | Harness-driven connection-state/fallback polling scenarios. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/*Disconnected*Tests.cs` | Syncing disconnect/reconnect/form preservation scenarios. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/*` | If 5-4 state exists, partial/reorder reconnect tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/*` | If 5-5 state exists, out-of-order/idempotent outcome tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/*` | If 5-6 helpers exist, fault-path log/span redaction tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/README.md` or inline XML docs | Usage examples for common scenarios and extraction notes. |
| `.github/workflows/ci.yml` | Only if a separate live smoke lane is added or deterministic harness category needs a named gate. |

### Testing Standards

- Unit/component tests must be deterministic, parallel-safe, and not depend on a live server, browser, local port, Docker, Dapr, Aspire, EventStore, or real timers.
- Prefer service-level tests for sequencing and bUnit tests for visible lifecycle/connection UI. Use Playwright only for a deliberately separate live smoke.
- Every harness test that blocks a checkpoint must release or cancel it in `finally`/async disposal.
- All harness APIs should produce diagnostic failure messages naming the checkpoint and outstanding operations when a test times out.
- Redaction tests should inspect structured logger state and activity tags when available, not just formatted text.
- Test filters should keep default CI fast. Live smoke, if any, must have a category trait and a clear non-default invocation.

### Scope Guardrails

Do not implement these in Story 5-7:

- Replacing the production SignalR wrapper or EventStore REST/SignalR contract.
- Rewriting command lifecycle, reconnect reconciliation, pending command resolver, fallback polling, ETag cache, or telemetry behavior except where tests expose a true defect.
- Publishing a new NuGet package or introducing a new solution project unless the repo already has package extraction infrastructure ready.
- Adding Dapr, Docker, EventStore, toxiproxy, Playwright, browser binaries, or Aspire as required dependencies for the deterministic harness.
- Sending full projection payloads over SignalR or adding query/command payloads to harness events.
- Persisting harness state to LocalStorage, ETag cache, or any production storage.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Formal `Hexalith.FrontComposer.Testing` package extraction and adopter docs. | Epic 10 / v0.3 packaging story |
| Live browser + Aspire reconnect smoke, if still needed after deterministic coverage. | Story 10-2 or dedicated signalr-live-smoke follow-up |
| Provider/Pact verification against real EventStore SignalR hub behavior. | Story 10-3 |
| Full flaky-test quarantine and reintroduction policy for live realtime tests. | Story 10-5 |
| Public API compatibility promises for testing helpers. | Story 9-4 / Epic 10 |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.7] - story statement, baseline ACs, FR82, NFR53, and NFR59.
- [Source: _bmad-output/planning-artifacts/architecture.md#Test-infrastructure-architectural-prerequisites] - SignalR testable abstraction behind a wrapper for fault injection.
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md] - fault injection wrapper simulating drop, delay, partial delivery, and reorder without live server.
- [Source: _bmad-output/planning-artifacts/prd/user-journeys.md] - degraded-network journey and deterministic SignalR fault injection as primary test surface.
- [Source: _bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md] - future `Hexalith.FrontComposer.Testing` package and adopter test utilities.
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/known-gaps-explicit-not-bugs.md] - disconnected lifecycle known gap assigned to Story 5-7.
- [Source: _bmad-output/implementation-artifacts/3-7-command-palette-e2e-and-scorer-bench.md] - CI trait pattern and Aspire/browser validation handoff to Story 5-7.
- [Source: _bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md] - current SignalR abstraction, degraded rejoin, fallback polling, and deferred test gaps.
- [Source: _bmad-output/implementation-artifacts/5-4-reconnection-reconciliation-and-batched-updates.md] - reconnect/reconciliation fault scenarios and 5-7 ownership.
- [Source: _bmad-output/implementation-artifacts/5-5-command-idempotency-and-optimistic-updates.md] - out-of-order/idempotent pending command scenarios and 5-7 ownership.
- [Source: _bmad-output/implementation-artifacts/5-6-build-time-infrastructure-enforcement-and-observability.md] - telemetry/redaction fault-path expectations and 5-7 ownership.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L08] - party review and elicitation are complementary hardening passes.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs] - production abstraction to implement in the harness.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs] - active group and rejoin ownership.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs] - current inline fake to replace or adapt.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperDisconnectedTests.cs] - current disconnected lifecycle assertions.
- [Source: Microsoft Learn: ASP.NET Core SignalR .NET client](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client) - reconnect event order, default automatic reconnect delays, and initial-start caveat.
- [Source: Microsoft Learn: Testing with FakeTimeProvider](https://learn.microsoft.com/en-us/dotnet/core/extensions/timeprovider-testing) - deterministic time testing and timer cleanup guidance.

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
