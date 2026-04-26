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
| AC8 | CI runs realtime resilience tests | The default deterministic lane executes | At least 90 percent of FR24-FR29 reconnection/resilience behavior is traced in the FR24-FR29 matrix below and covered at unit/component level through the harness or story-local consumers; the denominator is realtime resilience behavior in Shell/component integration, not global solution coverage, and any remaining live-browser or Aspire smoke tests are explicitly tagged, non-default, and non-duplicative. |
| AC9 | Fault-path diagnostics and telemetry are captured | Harness scenarios trigger reconnect, rejoin failure, fallback polling, pending outcome resolution, and telemetry/log paths | Tests can assert structured logs/spans are redacted, no raw tenant/user/token/group/payload/problem detail leaks, and failures remain bounded categories. |
| AC10 | A developer adds a new supported SignalR fault test | The developer uses the harness API and README/XML-doc examples | The test can stage the fault, release/check the named checkpoint, and assert the observable client behavior without adding timers, sleeps, live servers, network dependencies, or a new fake implementation. |

### Party-Mode Hardening Addendum

The 2026-04-26 party-mode review tightened Story 5-7 around deterministic acceptance rather than scope expansion. These clarifications are binding for `bmad-dev-story`:

- **Harness boundary:** core harness types implement and expose only the Shell-owned `IProjectionHubConnection` seam plus test-support builders/checkpoints. Tests using the harness must not reference concrete `HubConnection`, SignalR transport primitives, Contracts-layer SignalR DTOs, or payload-bearing SignalR messages.
- **Checkpoint contract:** named checkpoints are one-shot observations unless explicitly documented as counted checkpoints. `BlockUntil` arms a checkpoint before the production call crosses it; `WaitFor` observes occurrence counts; `Release` continues a blocked operation; `FailNext` completes the next matching operation as faulted; `CancelNext` completes it as canceled using the supplied `CancellationToken`; `DropNext` suppresses a matching nudge; `DelayNext` queues it until release; `ReorderNext` holds a deterministic queue that is flushed in caller-specified order. Timeout helpers are diagnostics only and must never model ordering.
- **Execution contract:** checkpoint TCS instances use `TaskCreationOptions.RunContinuationsAsynchronously`; blocked checkpoints must be released, failed, or canceled in `finally`/async disposal; disposal suppresses later callbacks and reports outstanding checkpoints in the failure message.
- **Fault inventory:** implement the initial v0.1 fault selectors only for scenarios needed by Stories 5-1 through 5-6: start failure, start delay, disconnect, reconnecting, reconnected, closed-after-retry, join failure, leave failure, per-group drop, per-group duplicate, deterministic reorder, partial delivery, cancellation, and disposal races. Extension seams may be documented, but speculative selectors are out of scope.
- **State oracles:** tests must assert an observable owner outcome, not just "no crash": disconnected `Syncing` escalates immediately; reconnect alone does not confirm; terminal confirmed ignores later connection loss; failed rejoin marks only the affected group degraded; successful rejoin restores the group; fallback polling starts only for unresolved visible lanes and stops on reconnect/dispose.
- **FR24-FR29 traceability:** implementation must add or update a small trace table before closing AC8. Each FR24-FR29 behavior row records owner component/service, harness scenario, planned test class, default-lane status, and whether any non-default live smoke remains. AC8 passes when at least 90 percent of rows are default-lane unit/component rows.
- **Redaction contract:** AC9 forbids bearer/access tokens, raw tenant/user values, raw group strings, command/query/cache payloads, raw ProblemDetails bodies, raw SignalR exception messages, and connection IDs unless explicitly redacted or categorized. Allowed fields are bounded failure category, projection type when non-sensitive, redacted tenant marker, reconnect attempt, connection state, outcome, and approved correlation identifiers.
- **Package decision:** the harness remains internal to Shell.Tests for this story. Shared package naming, public API compatibility, and adopter-facing versioning are deferred until a future Testing-package extraction story validates the API shape.

---

## Tasks / Subtasks

- [ ] T1. Design the fault-harness API and v0.1 location (AC1, AC2, AC6, AC7)
  - [ ] Place the first implementation under `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/` or an equivalent test-support folder.
  - [ ] Name the primary fake `FaultInjectingProjectionHubConnection` or similar and have it implement `IProjectionHubConnection`.
  - [ ] Add a builder such as `ProjectionHubFaultScenarioBuilder` with readable scenario setup: start connected, start failed, reconnecting, reconnected, closed, join failure, leave failure, drop nudge, duplicate nudge, reorder nudges.
  - [ ] Keep fault controls separate from assertion helpers: the core fake drives connection behavior, checkpoints, and injected failures; owner-specific assertions stay in consumer test classes or thin helper adapters.
  - [ ] Keep extraction seams clear for a future `Hexalith.FrontComposer.Testing` package, but do not create the package in this story unless the repo already has a first-class packaging path for it.
  - [ ] Do not expose or require `Microsoft.AspNetCore.SignalR.Client.HubConnection` from tests that use the harness.

- [ ] T2. Add deterministic checkpoint primitives (AC2, AC6, AC8)
  - [ ] Provide named checkpoints for `StartAsync`, `JoinGroupAsync`, `LeaveGroupAsync`, connection-state publication, nudge publication, fallback trigger, and disposal.
  - [ ] Provide methods such as `BlockUntil(checkpoint)`, `Release(checkpoint)`, `FailNext(checkpoint, exception)`, `CancelNext(checkpoint)`, and `WaitFor(checkpoint, count)`.
  - [ ] Implement all coordination with `TaskCompletionSource` created with `RunContinuationsAsynchronously`.
  - [ ] Accept cancellation tokens and propagate them exactly as the production abstraction expects.
  - [ ] Document per-operation cancellation behavior for `StartAsync`, `JoinGroupAsync`, `LeaveGroupAsync`, nudge publication, fallback trigger, and disposal: whether the task faults with `OperationCanceledException`, completes canceled, suppresses callbacks, or leaves connection state unchanged.
  - [ ] Add timeout helpers only for test failure diagnostics; production behavior must not depend on real sleeps.
  - [ ] Add a fixture-level scenario registry or theory data source that enumerates every supported fault selector so AC1 cannot pass with undocumented one-off behavior.

- [ ] T3. Model connection states and retry outcomes (AC1, AC3, AC5, AC8)
  - [ ] Simulate `Connected`, `Reconnecting`, `Reconnected`, and `Closed` through `ProjectionHubConnectionStateChanged`.
  - [ ] Preserve Microsoft SignalR semantics used by the app: automatic reconnect is opt-in, default reconnect attempts are 0, 2, 10, and 30 seconds before stopping, `Reconnecting` fires before attempts, `Reconnected` fires after success with a new connection id, and failed automatic reconnect ends in `Closed`.
  - [ ] Keep initial start failure distinct from reconnect failure, matching Story 5-3's sticky `InitialStartFailed` behavior.
  - [ ] Support join/leave calls with per-group results so tests can mark one group `Degraded` while another remains `Active`.
  - [ ] Track active groups in the fake only as test observation. `ProjectionSubscriptionService` remains the production source of truth.

- [ ] T4. Model nudge delivery faults without data payloads (AC1, AC4, AC5, AC9)
  - [ ] Provide `PublishProjectionChanged(projectionType, tenantId)` helpers for normal nudges.
  - [ ] Provide drop, duplicate, partial delivery, delayed delivery, and reorder behaviors over nudge events.
  - [ ] Define nudge fault scope explicitly: each configured fault is per publication call and optionally narrowed by projection type, tenant marker, and joined group; reorder uses a deterministic queue flushed by test code, not scheduling races.
  - [ ] Validate or fail closed on blank/colon-containing projection and tenant segments when configuring scenario helpers.
  - [ ] Never include projection payloads, command payloads, raw tenant/user data, or ProblemDetails bodies in harness events or diagnostics.
  - [ ] Add assertions or helper guards proving the harness only publishes lightweight nudges.
  - [ ] Add negative tests proving payload-bearing SignalR messages are impossible through the harness API or fail closed at scenario setup.

- [ ] T5. Refactor existing story-local fakes to consume the harness (AC3, AC5, AC8)
  - [ ] Migrate the existing inline fake in `ProjectionSubscriptionServiceTests` first, before adding new coverage; the migration validates the API against known expectations and prevents over-general fake design.
  - [ ] Replace or adapt the inline `FakeProjectionHubConnection` in `ProjectionSubscriptionServiceTests` with the reusable harness.
  - [ ] Preserve existing tests: subscribe commit-after-join, nudge tenant route, rejoin exactly once, initial start failure, join failure, unsubscribe leave failure, tenant-aware notifier, subscriber isolation, degraded-group skip, and redacted rejoin logs.
  - [ ] Add race tests that were deferred from Story 5-3: duplicate subscribe/unsubscribe during reconnect, dispose during rejoin, failed rejoin stays degraded until next successful reconnect, and callback suppression after disposal.
  - [ ] Add SignalR wrapper coverage that was deferred from Story 5-3 only if testable without reflection/unsupported `HubConnection` mocking. Otherwise prove the wrapper boundary through the factory plus harness and document why direct private-wrapper tests remain out of scope.

- [ ] T6. Add lifecycle and form-preservation harness scenarios (AC3, AC4, AC8)
  - [ ] Extend `FcLifecycleWrapperDisconnectedTests` or adjacent bUnit tests to drive disconnect/reconnect through the harness-backed connection state.
  - [ ] Assert Syncing disconnect escalates immediately, reconnect alone does not confirm, and terminal confirmed ignores later connection loss.
  - [ ] Add generated command-form preservation tests if Story 5-5 did not already close them: edited field values and validation state survive disconnect/reconnect and degraded rejection.
  - [ ] Keep lifecycle wrapper tests separate from reconnect/reconciliation tests so failures identify the owning behavior rather than a broad realtime fixture.
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
  - [ ] State whether each telemetry test asserts structured logger records, `Activity` tags, or both, based on the helpers Story 5-6 actually provides.

- [ ] T9. CI and documentation integration (AC6, AC7, AC8)
  - [ ] Keep deterministic harness tests in the default unit/bUnit lane unless they require a separate trait for runtime.
  - [ ] Keep default CI offline-only. Live smoke tests, if added, must be explicitly enabled and must not gate deterministic CI unless existing infrastructure already supports them without new runtime dependencies.
  - [ ] If any live Aspire/browser smoke test is added, tag it separately (for example `Category=signalr-live-smoke`) and keep it small: one TCP/server-style reconnect sanity check, not a duplicate of unit coverage.
  - [ ] Reuse the `e2e-palette` trait pattern from Story 3-7 if a separate lane is needed.
  - [ ] Add a concise developer note or test-support README explaining how to stage common scenarios.
  - [ ] Add the FR24-FR29 traceability matrix required by AC8, mapping each resilience behavior to the harness scenario and default-lane test class.
  - [ ] If CI wiring for `aspire` is still unavailable, document that live-browser parity remains a follow-up and keep AC8 satisfied by deterministic unit/component coverage.

- [ ] T10. Tests and verification (AC1-AC9)
  - [ ] Harness unit tests: connection drop, delay, partial delivery, duplicate nudge, reorder, join/leave failure, cancellation, disposal, checkpoint timeout diagnostics, and handler isolation.
  - [ ] Consumer tests: `ProjectionSubscriptionService`, connection state, lifecycle wrapper, fallback scheduler/driver, reconnect reconciliation if present, pending command resolver if present, and telemetry/redaction.
  - [ ] Cleanup tests: prove harness disposal clears checkpoint queues, pending tasks, fake subscriptions, timer handles, and callbacks; test runs must be parallel-safe with unique synthetic group/session ids and no shared static fake state.
  - [ ] Determinism enforcement: harness code paths must not use `Task.Delay`, `Thread.Sleep`, real timers, unbounded waits, or wall-clock reads except through injected `TimeProvider` / `FakeTimeProvider` and named checkpoint advancement.
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
| D11 | `ProjectionSubscriptionServiceTests` fake migration is the first harness consumer. | Existing expectations should shape the fake before new race coverage is added. | Build a broad harness first and retrofit existing tests later. |
| D12 | AC8 is measured by requirement trace rows, not global coverage percentage. | The user value is resilience behavior coverage, not line/branch padding. | Treat any high code coverage metric as proof of FR24-FR29 coverage. |
| D13 | Harness code is parallel-safe by construction. | CI must tolerate default xUnit parallelism and repeated runs. | Disable test parallelism for the entire Shell suite; rely on static shared fake state. |
| D14 | Timeout and wait helpers are diagnostic-only. | Ordering must come from explicit checkpoints to avoid flakiness. | Use timeout duration as the synchronization mechanism. |
| D15 | Telemetry assertions target bounded categories and explicit forbidden data. | Fault paths are high-risk leak points, but broad telemetry policy belongs to Story 5-6 / governance. | Assert formatted log text only; create a new telemetry policy inside this story. |

### Library / Framework Requirements

- Target current repo package lines and TFMs: .NET 10, Blazor, Fluxor, Fluent UI Blazor, Microsoft.AspNetCore.SignalR.Client 10.0.6, xUnit v3, bUnit, Shouldly, NSubstitute, Verify.XunitV3, FsCheck, and Microsoft.Extensions.TimeProvider.Testing.
- Use `TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously` for checkpoints.
- Use `FakeTimeProvider` for timer-driven scenarios; inject `TimeProvider` rather than using `DateTimeOffset.UtcNow` or real delays.
- Do not use `Task.Delay`, `Thread.Sleep`, real timers, unbounded waits, or wall-clock reads in harness control paths. Tests advance time through `FakeTimeProvider` or named checkpoint release.
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
- Parallel safety means unique synthetic projection/group/session identifiers per test, no shared static fake state, and disposal that clears pending checkpoints, queued nudges, timers, subscriptions, and callbacks. If a specific fixture cannot be parallel-safe, document the narrow xUnit collection boundary in the test file and the README.
- Prefer service-level tests for sequencing and bUnit tests for visible lifecycle/connection UI. Use Playwright only for a deliberately separate live smoke.
- Every harness test that blocks a checkpoint must release or cancel it in `finally`/async disposal.
- All harness APIs should produce diagnostic failure messages naming the checkpoint and outstanding operations when a test times out.
- Redaction tests should inspect structured logger state and activity tags when available, not just formatted text.
- Test filters should keep default CI fast. Live smoke, if any, must have a category trait and a clear non-default invocation.
- Requirement traceability for AC8 lives in the story artifact or harness README until code comments can link to concrete test names. Each row should be updated as tests land rather than inferred after the fact.

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

## Party-Mode Review

| Field | Value |
| --- | --- |
| Date/time | 2026-04-26T08:03:57.7696732+02:00 |
| Selected story | 5-7-signalr-fault-injection-test-harness |
| Command / skill invocation | `/bmad-party-mode 5-7-signalr-fault-injection-test-harness; review;` |
| Participating BMAD agents | Winston (Architect), Amelia (Dev), Murat (Test Architect), John (PM) |
| Final recommendation | ready-for-dev |

### Findings Summary

- AC8 needed a measurable denominator and FR24-FR29 traceability instead of a vague "90 percent" target.
- The fake API needed an explicit checkpoint execution contract: ordering, release/fail/cancel semantics, disposal, diagnostics-only timeouts, and no wall-clock sleeps.
- The harness boundary needed sharper constraints against concrete SignalR client types, transport details, Contracts-layer SignalR DTOs, and payload-bearing events.
- Fault selectors needed a bounded v0.1 inventory tied to Stories 5-1 through 5-6 to avoid overbuilding.
- AC9 needed concrete allowed and forbidden telemetry fields.
- Existing inline fake migration needed to be the first consumer so the harness shape is proven by current tests.
- Live smoke, package extraction, and broad telemetry policy needed to remain deferred rather than expanding this story.

### Changes Applied

- Added AC10 for developer ergonomics: a new supported fault test must be authorable without sleeps, live infrastructure, or another fake.
- Rewrote AC8 to define the denominator as FR24-FR29 realtime resilience behavior in Shell/component integration and to require traceability rows.
- Added a party-mode hardening addendum covering the harness boundary, checkpoint contract, execution contract, fault inventory, state oracles, FR24-FR29 matrix, redaction contract, and package-decision deferral.
- Hardened T1-T10 with fake/assertion separation, cancellation documentation, scenario registry, nudge-fault scope, negative nudge-only tests, fake migration order, test-class ownership separation, telemetry assertion surface, offline-only CI, cleanup checks, and determinism enforcement.
- Added D11-D15 for fake migration order, AC8 measurement, parallel safety, diagnostic-only timeouts, and bounded telemetry assertions.
- Tightened testing standards for parallel safety, disposal, no wall-clock waits, and requirement traceability.

### Findings Deferred

- Shared `Hexalith.FrontComposer.Testing` package name, public API compatibility, versioning, and adopter documentation remain future Testing-package extraction work.
- Final live smoke suite decision remains optional and non-default; no live infrastructure is required by this story.
- Broader telemetry policy remains owned by Story 5-6 / governance; this story only asserts bounded fault-path categories and forbidden raw data.
- A project-wide test-pyramid decision for UI/component versus service coverage is deferred; Story 5-7 only proves deterministic harness-driven coverage.

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
