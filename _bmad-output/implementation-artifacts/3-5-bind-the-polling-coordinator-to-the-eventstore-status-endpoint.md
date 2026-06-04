---
baseline_commit: 756e614ea2f58464b3477d56695cfcf62542cc56
---

# Story 3.5: Bind the polling coordinator to the EventStore status endpoint

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The polling coordinator is already implemented and tested.
> `PendingCommandPollingCoordinator` calls an `IPendingCommandStatusQuery` seam, but the only
> registered provider is `NullPendingCommandStatusQuery`. This story should replace that null
> provider in the EventStore path, confirm the existing EventStore endpoint contract, and pin the
> mapping from backend command statuses to pending-command terminal outcomes.
>
> Do not rebuild the command pipeline. Stories 3.1 through 3.4 already own generated command forms,
> density, FC-CMD identity/correlation, Acknowledged UI, typed rejection metadata, and lifecycle
> dispatch ordering. Story 3.6 still owns numeric confirming/degraded/polling budgets.

## Story

As an operator,
I want command confirmation driven by the real EventStore status query,
so that confirmed/rejected outcomes reflect backend truth.

## Acceptance Criteria

1. **EventStore-backed status query is bound to the coordinator.**  
   Given an acknowledged command registered in pending-command state,  
   When `PendingCommandPollingCoordinator.PollOnceAsync(...)` runs under an
   `AddHexalithEventStore(...)` host,  
   Then it uses an EventStore-backed `IPendingCommandStatusQuery` instead of
   `NullPendingCommandStatusQuery`,  
   And the query sends `GET /api/v1/commands/status/{id}` to EventStore,  
   And `{id}` is the pending `MessageId` because EventStore defaults its status `correlationId` to
   the submitted `messageId` when FrontComposer does not send an explicit EventStore
   `CorrelationId`.

2. **Backend status values map to the existing pending-command resolver.**  
   Given EventStore returns `CommandStatusResponse`,  
   When `Status` is `Received`, `Processing`, `EventsStored`, or `EventsPublished`,  
   Then the status query returns no terminal observation and the pending entry remains pending,  
   And any `Retry-After` hint is parsed only as metadata for later budget work.  
   When `Status` is `Completed`,  
   Then the query returns a `PendingCommandOutcomeObservation` with
   `Outcome = Confirmed` and `MessageId = pending.MessageId`.  
   When `Status` is `Rejected`,  
   Then the query returns `Outcome = Rejected` with bounded plain-text rejection title/detail from
   `RejectionEventType` and/or `FailureReason`.  
   When `Status` is `PublishFailed` or `TimedOut`,  
   Then the query returns `Outcome = NeedsReview` rather than pretending the command was normally
   rejected or confirmed. The pending summary may show `NeedsReview`; lifecycle dispatch still uses
   the existing terminal wrapper behavior from `PendingCommandStateService`.

3. **The EventStore status contract is recorded as confirm-stable or escalated.**  
   Given the EventStore submodule contract evidence,  
   When this story completes,  
   Then a contract artifact under `_bmad-output/contracts/` records the confirmed endpoint path,
   route parameter semantics, response fields, status enum mapping, auth/tenant behavior, and
   non-terminal `Retry-After` behavior,  
   Or any mismatch is escalated with an owner and the implementation does not claim the endpoint is
   stable.

4. **Identity, correlation, and lifecycle ownership do not regress.**  
   Given the FC-CMD v1 contract from Story 3.3,  
   When the EventStore status query is added,  
   Then generated form `CorrelationId` remains the form/lifecycle subscription key,  
   And pending-command `MessageId` remains the accepted command/status identity,  
   And no GUID generation, GUID parsing, raw command payload storage, tenant/user storage, or
   cross-circuit replay is introduced.

5. **Governance, registration, and focused tests prove the new binding.**  
   Given the current governance suite intentionally allows only `NullPendingCommandStatusQuery`,  
   When Story 3.5 is implemented,  
   Then that tripwire is updated or retired with an explicit Story 3.5 disposition,  
   And `AddHexalithFrontComposer()` alone still uses the null provider,  
   And `AddHexalithFrontComposer() -> AddHexalithEventStore(...)` resolves the EventStore provider,  
   And focused status-query, polling-coordinator, EventStore registration, and pending identity
   tests pass.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live polling and EventStore contract before editing (AC: #1-#5)**
  - [x] Read `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`
        completely; preserve oldest-first processing, budget cap, mid-tick re-check, cancellation,
        exception handling, and shared resolver use.
  - [x] Read `PendingCommandModels.cs`, `PendingCommandStateService.cs`, and
        `PendingCommandOutcomeResolver.cs`; preserve ULID canonicalization, duplicate terminal
        handling, `IdempotentConfirmed`, `NeedsReview`, tenant/user fail-closed clearing, and
        lifecycle dispatch behavior.
  - [x] Read `EventStoreCommandClient.cs`, `EventStoreResponseClassifier.cs`, `EventStoreOptions.cs`,
        and `EventStoreServiceExtensions.cs`; reuse the existing HTTP client, auth-token, response
        bounding, telemetry/logging, and DI patterns.
  - [x] Read `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs`
        before changing status-provider registration; this test is a known Story 3.5 tripwire.
  - [x] Read EventStore submodule contract evidence only; do not modify submodule files:
        `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`,
        `Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs`,
        `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatus.cs`, and
        matching `CommandStatusControllerTests`.

- [x] **Task 2 - Add the EventStore-backed `IPendingCommandStatusQuery` (AC: #1, #2, #4)**
  - [x] Add a Shell-side provider, expected name `EventStorePendingCommandStatusQuery`, under
        `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/` unless a better existing
        local pattern is found.
  - [x] Query `GET /api/v1/commands/status/{pending.MessageId}` against the EventStore base address.
        EventStore names the route parameter `correlationId`, but FrontComposer sends no explicit
        EventStore correlation ID, so EventStore defaults it to the submitted `messageId`.
  - [x] Use the existing EventStore command/query HTTP client registration pattern. Do not create a
        new unmanaged `HttpClient`, do not bypass `RequireAccessToken`, and do not log raw response
        bodies or raw exception messages.
  - [x] Parse a bounded response model matching EventStore `CommandStatusResponse`:
        `correlationId`, `status`, `statusCode`, `timestamp`, `aggregateId`, `eventCount`,
        `rejectionEventType`, `failureReason`, `timeoutDuration`.
  - [x] Treat unknown status names/status codes, malformed JSON, oversized bodies, 400/403/404/429,
        and 5xx as non-mutating failures. Let the coordinator catch/log provider exceptions or
        return null when the status is genuinely in-flight; do not mutate pending state on protocol
        drift.

- [x] **Task 3 - Map EventStore statuses into terminal observations (AC: #2, #4)**
  - [x] Map `Received`, `Processing`, `EventsStored`, and `EventsPublished` to `null`.
  - [x] Map `Completed` to `PendingCommandOutcomeObservation(
        Source = IdempotencyStatusQuery,
        Outcome = Confirmed,
        MessageId = pending.MessageId)`. Rename the source enum only if every existing resolver,
        test, and telemetry reference is intentionally updated.
  - [x] Map `Rejected` to `Outcome = Rejected`; bound rejection text to the same 512-character
        plain-text discipline used by `EventStoreResponseClassifier`.
  - [x] Map `PublishFailed` and `TimedOut` to `Outcome = NeedsReview` with bounded failure/timeout
        context. Do not collapse these infrastructure outcomes into normal domain rejection.
  - [x] Preserve existing idempotent/already-applied behavior. The current EventStore status
        response has no stable `alreadyApplied` field; do not invent one. Existing
        `IdempotentConfirmed` paths must keep passing.

- [x] **Task 4 - Bind DI and governance honestly (AC: #1, #3, #5)**
  - [x] Keep `AddHexalithFrontComposer()` defaulting to `NullPendingCommandStatusQuery` for hosts
        that do not opt into EventStore.
  - [x] In `AddHexalithEventStore(...)`, replace the default unkeyed scoped
        `IPendingCommandStatusQuery` with the EventStore-backed provider.
  - [x] Update `PendingStatusReopenGovernanceTests` and any deferred-work/release-note guard text so
        it no longer asserts the accepted null-provider constraint once Story 3.5 reopens it.
  - [x] Add registration tests proving the null provider remains for Quickstart-only hosts and the
        EventStore provider wins after `AddHexalithEventStore(...)`.
  - [x] Create `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md`
        or equivalent, with the confirm-stable endpoint evidence and any escalations.

- [x] **Task 5 - Add focused tests for status query and polling integration (AC: #1-#5)**
  - [x] Add status-query unit tests for all eight EventStore statuses:
        `Received`, `Processing`, `EventsStored`, `EventsPublished`, `Completed`, `Rejected`,
        `PublishFailed`, `TimedOut`.
  - [x] Add tests for `Retry-After` on non-terminal statuses, 404/no status found, 401/403/429,
        malformed/unknown statuses, malformed JSON, cancellation, and auth-token forwarding.
  - [x] Extend `PendingCommandPollingCoordinatorTests` with an EventStore-backed provider test that
        resolves a pending command through the real provider seam and shared resolver.
  - [x] Re-run Story 3.3 identity/pending focused tests and Story 3.4 lifecycle wrapper/rejection
        focused tests so pending resolution and lifecycle UI do not regress.
  - [x] If the response model or provider is public API, update public API baselines intentionally;
        otherwise keep new types internal where possible.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest/MSBuild sockets are blocked, use the established xUnit v3 in-process
        fallback and record CI as the authoritative solution-level gate.
  - [x] At minimum run focused lanes for `EventStoreRegistrationTests`,
        `PendingCommandPollingCoordinatorTests`, new `EventStorePendingCommandStatusQueryTests`,
        `PendingCommandStateServiceTests`, `PendingCommandOutcomeResolverTests`,
        `FcLifecycleWrapperTests`, `FcLifecycleWrapperRejectionTests`, and
        `PendingStatusReopenGovernanceTests`.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; this story should not normally require
        SourceTools Verify snapshot changes.
  - [x] Keep the File List complete, including this story file, sprint status, contract artifact,
        source, tests, governance updates, and any intentional snapshots.

## Dev Notes

### Current polling anchors

- `PendingCommandPollingCoordinator` already snapshots pending entries, orders oldest first, caps
  work by `FcShellOptions.MaxPendingCommandPollingPerTick`, re-checks the current entry before
  querying, calls `IPendingCommandStatusQuery`, and routes non-null observations through
  `PendingCommandOutcomeResolver`. Preserve this control flow. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs]
- `NullPendingCommandStatusQuery` deliberately returns `null`; it is correct for
  `AddHexalithFrontComposer()` without EventStore and incorrect for `AddHexalithEventStore(...)`
  after this story. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs]
- `PendingCommandOutcomeResolver` resolves by `MessageId` first, then uses framework-controlled
  `EntityKey`/projection/lane/status-slot matching only when exactly one pending candidate matches.
  The status endpoint should use `MessageId` and avoid the ambiguous fallback path. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs]
- `PendingCommandStateService.ResolveTerminal(...)` maps `Confirmed`, `Rejected`,
  `IdempotentConfirmed`, and `NeedsReview` to immutable pending entries and dispatches lifecycle
  terminal transitions keyed by the form `CorrelationId`. Do not dispatch lifecycle directly from
  the status-query provider. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]

### EventStore endpoint contract evidence

- EventStore exposes `[HttpGet("{correlationId}")]` under
  `api/v1/commands/status`; the controller is tenant-scoped and authorized. It returns 200 with
  `CommandStatusResponse`, 400 for invalid correlation ID, 403 when no tenant claim exists, 404 when
  no status exists for the authorized tenant(s), and 429 as documented. [Source: Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs]
- EventStore `CommandStatusResponse` fields are `CorrelationId`, `Status`, `StatusCode`,
  `Timestamp`, `AggregateId`, `EventCount`, `RejectionEventType`, `FailureReason`, and
  `TimeoutDuration`; timeout duration is ISO 8601 (`XmlConvert.ToString(TimeSpan)`, e.g. `PT30S`).
  [Source: Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs]
- EventStore `CommandStatus` has exactly eight explicit values: `Received`, `Processing`,
  `EventsStored`, `EventsPublished`, `Completed`, `Rejected`, `PublishFailed`, `TimedOut`.
  Values with integer value `>= Completed` are terminal in EventStore. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatus.cs]
- EventStore adds `Retry-After: 1` on non-terminal status responses only. Story 3.5 should not pick
  new polling budgets; capture the hint and leave scheduling/budget semantics to Story 3.6.
  [Source: Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Commands/CommandStatusControllerTests.cs]
- EventStore `CommandsController` defaults submitted `CorrelationId` to `MessageId` when the request
  omits it, then emits `Location: /api/v1/commands/status/{result.CorrelationId}`. FrontComposer's
  `EventStoreCommandClient` currently omits the request `CorrelationId`, so the status endpoint ID
  is the accepted `MessageId`. [Source: Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs]

### EventStore client anchors

- `EventStoreCommandClient` already creates the accepted command `MessageId` through
  `IUlidFactory.NewUlid()`, posts to `EventStoreOptions.CommandEndpointPath`, preserves
  `CommandResult.Location` and `RetryAfter`, and reads response `correlationId`. Do not change
  dispatch ordering or generated form pending registration for this story. [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]
- `EventStoreOptions` has `BaseAddress`, command/query/hub paths, `Timeout`, max request/response
  sizes, and `AccessTokenProvider` / `RequireAccessToken`. Prefer the fixed stable status path
  unless an implementation need justifies a new option; if a new option is added, update option
  validation and governance deliberately. [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs]
- `EventStoreResponseClassifier` is the local pattern for bounded ProblemDetails parsing, typed
  failures, `Retry-After`, and avoiding raw payload logging. Reuse its safety discipline rather
  than ad hoc string parsing or unbounded deserialization. [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs]
- `EventStoreServiceExtensions.AddHexalithEventStore(...)` is the correct place to replace the null
  status-query provider because that call already opts the host into EventStore-backed command/query
  and projection services. [Source: src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]

### Previous story intelligence

- Story 3.3 decided the FC-CMD v1 contract: `MessageId` is the accepted command and pending-command
  identity; generated form `CorrelationId` is the lifecycle subscription key; both are canonical
  26-character Crockford ULIDs; pending state is circuit-local and fail-closes on tenant/user scope
  changes. [Source: _bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md]
- Story 3.4 added visible `Acknowledged` UI and typed rejection metadata. The status endpoint's
  post-ack rejection path should populate pending summary/lifecycle terminal state; do not reopen
  immediate generated-form `CommandRejectedException` behavior. [Source: _bmad-output/implementation-artifacts/3-4-command-lifecycle-ui.md]
- Recent commits are story-scoped: `756e614` (Story 3.4 lifecycle UI), `cbe4d42` (Story 3.3 identity
  contract), `c6ff094` (Story 3.2 density), and `8cfcc80` (Story 3.1 command form generation).
  Expect this story to touch Shell infrastructure/state/tests and contract docs, not SourceTools
  emitters or generated snapshots.

### Architecture and constraints

- Follow Fluxor/shell single-writer discipline: the status query only observes backend state; the
  existing coordinator/resolver/state service performs the mutation and lifecycle transition.
  [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- Keep scoped-lifetime discipline. EventStore status query should be scoped like the EventStore
  command/query clients because it depends on per-circuit auth/tenant context. [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- Use repo-pinned dependencies only. No package upgrades are needed. Relevant pins include .NET SDK
  10.0.300, FluentUI Blazor `5.0.0-rc.3-26138.1`, Fluxor 6.9.0, NUlid 1.7.3, xUnit v3 3.2.2,
  Shouldly 4.3.0, bUnit 2.7.2, and Verify.XunitV3 31.19.0. [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- Tests use xUnit v3, Shouldly, NSubstitute, bUnit where needed, and solution-level test commands
  with `DiffEngine_Disabled=true`. Do not use raw `Assert.*` in new tests. [Source: _bmad-output/project-context.md#Testing Rules]
- Do not modify `Hexalith.EventStore` submodule files in this story. They are contract evidence for
  FrontComposer; any EventStore mismatch should be escalated in the contract artifact. [Source: _bmad-output/project-context.md#Development Workflow Rules]

### Out of scope

- Do not choose confirming-to-degraded thresholds, poll intervals, retry counts, or long-poll
  budgets. Story 3.6 owns numeric budgets.
- Do not add destructive confirmation, unsaved-form abandonment, one-at-a-time command execution,
  authorization policy gates, retries, or degraded retry handling. Epic 4 owns those.
- Do not add row-level `FcNewItemIndicator` producer wiring. The status endpoint response has no
  per-row identity needed to mark a specific projection row as fresh.
- Do not modify generated code under `obj/**/generated/**`, SourceTools emitters, command density,
  or generated lifecycle action shape unless the live source proves an unavoidable integration bug.
- Do not use `docs/` as scratch space; write story/contract evidence under `_bmad-output/`.

### Project Structure Notes

- Expected source touch points are under:
  `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/`,
  `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`,
  `src/Hexalith.FrontComposer.Shell/State/PendingCommands/`, and focused Shell tests under
  matching `tests/Hexalith.FrontComposer.Shell.Tests/` folders.
- Expected governance touch point:
  `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs`.
- Expected contract artifact:
  `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md`.
- Do not edit the EventStore submodule unless the user explicitly approves that separate work.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.5: Bind the polling coordinator to the EventStore status endpoint]
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md#Asks - ordered by what they unblock]
- [Source: _bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md]
- [Source: _bmad-output/implementation-artifacts/3-4-command-lifecycle-ui.md]
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs]
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Create-story context analysis loaded project context, sprint status, Epic 3, Story 3.4
  completion notes, FC-CMD contract, EventStore endpoint source/tests, and live Shell pending-command
  implementation.
- 2026-06-04: Discovery results: loaded `epics_content` from
  `_bmad-output/planning-artifacts/epics.md`; no authored PRD/UX/architecture file exists under
  `_bmad-output/planning-artifacts`, so architecture context came from
  `_bmad-output/project-context.md`, `_bmad-output/project-docs/architecture.md`,
  `_bmad-output/project-docs/api-contracts.md`, and live source files.
- 2026-06-04: Latest-technology research disposition: no package/API upgrade is needed. The story
  uses repo-pinned .NET/FluentUI/Fluxor/NUlid/test dependencies and local EventStore submodule
  contract evidence rather than external library changes.
- 2026-06-04: Implemented `EventStorePendingCommandStatusQuery`, bound it from
  `AddHexalithEventStore(...)`, retired the old null-provider-only governance tripwire, and created
  the confirm-stable EventStore status endpoint contract artifact.
- 2026-06-04: Validation evidence: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1
  /nr:false` passed with 0 warnings / 0 errors. Official filtered `dotnet test` was attempted with
  `DiffEngine_Disabled=true` and failed before execution on the local VSTest socket restriction
  (`SocketException (13): Permission denied`).
- 2026-06-04: xUnit v3 in-process fallback evidence: focused Shell lane for
  `EventStorePendingCommandStatusQueryTests`, `EventStoreRegistrationTests`,
  `PendingCommandPollingCoordinatorTests`, `PendingCommandStateServiceTests`,
  `PendingCommandOutcomeResolverTests`, `FcLifecycleWrapperTests`,
  `FcLifecycleWrapperRejectionTests`, and `PendingStatusReopenGovernanceTests` passed 80/80.
- 2026-06-04: Broader Shell default-lane in-process check ran 1777 tests with 5 known non-story
  failures: Pact mock-server socket, navigation last-active-route hydration, full-page query
  fallback, and two generated Verify snapshot/culture baselines. The old four
  `PendingStatusReopenGovernanceTests` failures are retired by this story.
- 2026-06-04: `git diff --name-only -- '*.verified.txt'` returned empty; no SourceTools Verify
  snapshot changes were needed.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-06-04.
- Story explicitly scopes implementation to binding the existing EventStore status endpoint through
  the existing `IPendingCommandStatusQuery` seam.
- Story pins the important identity distinction: EventStore route parameter is named
  `correlationId`, but FrontComposer can query by pending `MessageId` because EventStore defaults
  correlation to message ID when the submit request omits an explicit correlation ID.
- Story requires updating the null-provider governance tripwire instead of accidentally fighting it.
- Story leaves Story 3.6 numeric budgets and Epic 4 retry/degraded/concurrency policies out of
  scope.
- Added the EventStore-backed status query at the existing Shell/EventStore seam. It queries
  `/api/v1/commands/status/{pending.MessageId}`, uses the configured EventStore bearer-token path,
  reads bounded JSON, validates status name/code consistency, and returns only observation records;
  pending-state mutation remains owned by the polling coordinator, resolver, and state service.
- Mapped EventStore non-terminal statuses to `null`, `Completed` to `Confirmed`, `Rejected` to
  bounded plain-text rejection metadata, and `PublishFailed`/`TimedOut` to `NeedsReview`.
- Bound `AddHexalithFrontComposer()` to the null provider by default and `AddHexalithEventStore(...)`
  to the EventStore provider. The old deferred-work/null-only governance scanner was retired and
  replaced with direct Story 3.5 registration-contract governance.
- Created `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md` with a
  confirm-stable disposition and no escalations.

### File List

- _bmad-output/implementation-artifacts/3-5-bind-the-polling-coordinator-to-the-eventstore-status-endpoint.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md
- src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs
- src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs
- src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreHttp.cs (review fix — shared auth/bounded-read helper)
- src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs (review fix — reuse shared auth helper)
- src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs (review fix — reuse shared auth/bounded-read helper)
- tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStorePendingCommandStatusQueryTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreRegistrationTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs

### Change Log

- 2026-06-04: Bound pending-command fallback polling to EventStore's status endpoint for
  EventStore-enabled hosts while preserving the null provider for base FrontComposer hosts.
- 2026-06-04: Added status mapping, protocol-drift handling, registration/governance tests, polling
  integration coverage, and the confirm-stable status endpoint contract artifact.
- 2026-06-04 (review fix): Extracted shared `EventStoreHttp` helper for bearer-token authorization
  and bounded response-body reading; pointed `EventStoreCommandClient`, `EventStoreQueryClient`, and
  the new `EventStorePendingCommandStatusQuery` at it (removed three duplicate auth copies and one
  duplicate bounded-read copy). Behavior-preserving; build 0W/0E and focused EventStore/pending/
  lifecycle lanes green.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (autonomous story-automator review) — 2026-06-04
**Outcome:** Approved (auto-fix applied). Status → done (0 CRITICAL / 0 HIGH).

### Validation performed

- `dotnet build` (Release, `-m:1 /nr:false`) of `Hexalith.FrontComposer.Shell` and
  `Hexalith.FrontComposer.Shell.Tests`: **0 warnings / 0 errors**.
- Focused xUnit v3 in-process lanes (local `dotnet test` still blocked on the VSTest socket):
  `Infrastructure.EventStore` (incl. `EventStorePendingCommandStatusQueryTests`,
  `EventStoreRegistrationTests`, `EventStoreClientTests`, query-cache/cancellation/classifier),
  `State.PendingCommands` (coordinator/state-service/resolver), `Governance.PendingStatusReopen…`,
  and `FcLifecycleWrapper(Rejection)Tests`: **all pass**. The only red tests in the swept namespaces
  are `Story12_4_RedPhaseDefTests` — quarantined red-phase ATDD scaffolds for unstarted Story 12.4,
  excluded from the official gate (`Category!=Quarantined`) and unrelated to this story.
- Independent AC re-verification: EventStore `CommandStatus` enum integers (`Received=0`…`TimedOut=7`)
  confirmed against the submodule contract, so the `statusCode` consistency check is sound; route
  uses pending `MessageId`; status→outcome mapping matches AC #2; governance tripwire correctly
  reopened (AC #5); contract artifact matches the live controller/response evidence (AC #3).

### Findings

- **[MEDIUM — FIXED] Duplicated security/safety HTTP plumbing.** The new
  `EventStorePendingCommandStatusQuery` shipped a third byte-identical copy of `ApplyAuthorizationAsync`
  (bearer-token / `RequireAccessToken` logic) and a second copy of the byte-bounding
  `ReadBoundedResponseBodyAsync`, despite Dev Notes directing reuse of the existing client pattern.
  Drift risk on auth and DoS-bounding code. **Fix:** extracted `EventStoreHttp` and routed all three
  EventStore HTTP clients through it (behavior-preserving; exception messages still contain
  `MaxResponseBytes`; query-client charset-fallback logging preserved).
- **[MEDIUM — NOT auto-fixed; flagged] Undocumented submodule pointer drift.** The working tree
  advances four submodule gitlinks vs HEAD — `Hexalith.Builds`, `Hexalith.Commons`,
  `Hexalith.EventStore`, `Hexalith.Tenants` (Aspire 13.4.2 / ByteAether.Ulid 1.3.7 dependency bumps).
  None are in this story's File List, and the story explicitly scopes out EventStore submodule
  changes. Left untouched deliberately: these are shared dependency submodules outside the story's
  source scope, and reverting them risks the build. **Action for orchestrator:** stage/commit Story
  3.5 source separately from these submodule bumps, or revert the gitlinks before committing.
- **[LOW — accepted] Dead `Retry-After` parse.** `EventStorePendingCommandStatusQuery` parses the
  `Retry-After` header and discards it (`_ = ResolveRetryAfter(...)`). Intentional per AC #2 (Story
  3.6 owns polling budgets); kept as a documented seam.
- **[LOW — accepted] Public visibility.** The provider is `public` though only consumed via
  `IPendingCommandStatusQuery`; consistent with the sibling public `EventStoreCommandClient`/
  `EventStoreQueryClient` and not gated by the focused `PublicAPI.FcTbl` baseline.
