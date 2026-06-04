---
baseline_commit: db5e045f12217f4805d18164572a964dad6523d7
---

# Story 4.5: Retry and degraded-state handling

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. Epic 3 already decided the command lifecycle and polling
> budgets: `TimeoutActionThresholdMs = 10_000`, `PendingCommandPollingIntervalMs = 1_000`,
> `MaxPendingCommandPollingDurationMs = 120_000`, `MaxPendingCommandPollingPerTick = 25`, and
> automatic client command retry was explicitly `0` until Epic 4. Story 4.5 is the contract-producing
> story that must decide and pin the v1 retry/degraded behavior. Do not add a broad retry loop around
> generated forms: retry only pre-accept transient EventStore dispatch faults, reuse the same generated
> `MessageId` for every retry attempt, and never retry after `202 Accepted`, pending registration, a
> validation/auth/domain rejection, FC-CNC denial, or authorization denial.

## Story

As an operator,
I want failed or slow commands to retry within budget and surface a clear degraded state,
so that transient faults recover without manual resubmission.

## Acceptance Criteria

1. **Retry/degraded contract is recorded and testable.**  
   Given the v1 retry/degraded contract is created under `_bmad-output/contracts/`,  
   When reviewed,  
   Then it records the exact dispatch retry budget, retryable fault taxonomy, delay/cadence rules,
   degraded-state presentation, ownership, and non-goals.  
   And the contract states that accepted commands continue through existing status polling and
   `NeedsReview` expiry rather than being re-dispatched.

2. **Transient pre-accept EventStore dispatch faults retry within budget using one command identity.**  
   Given an EventStore command dispatch sees a retryable pre-accept transport fault or retryable HTTP
   response before any `202 Accepted`,  
   When the retry budget has remaining attempts,  
   Then the client retries the dispatch using the same `MessageId`, tenant/domain/aggregate identity,
   command type, and serialized payload.  
   And no generated `SubmittedAction`, pending registration, acknowledgement, lifecycle terminal
   transition, FC-CNC admission release leak, or duplicate command identity is introduced by retrying.

3. **Retry exhaustion surfaces a retryable degraded warning, not a false terminal result.**  
   Given retryable pre-accept faults continue until the retry budget is exhausted,  
   When the generated form receives the exhausted transient failure,  
   Then it resets the form lifecycle to `Idle`, preserves entered form values, shows accessible
   retry/degraded warning feedback with any bounded retry delay hint, and allows the operator to
   submit again manually.  
   And it must not register pending state, dispatch `AcknowledgedAction`, claim the command was queued,
   or mark the command as confirmed/rejected/needs-review because EventStore never accepted it.

4. **Slow accepted commands continue through degraded UI and bounded polling.**  
   Given a command was accepted and registered in `IPendingCommandStateService` as `Pending`,  
   When it remains unresolved past `TimeoutActionThresholdMs`,  
   Then `FcLifecycleWrapper` surfaces the existing degraded/action-prompt state while
   `PendingCommandPollingDriver` continues polling within `MaxPendingCommandPollingDurationMs`.  
   When the max polling duration expires, the existing resolver moves the entry to `NeedsReview` and
   first terminal outcome wins over later observations.

5. **Pending and rejected commands are announced in `FcPendingCommandSummary`.**  
   Given pending, rejected, confirmed, idempotent-confirmed, and needs-review command entries are
   present,  
   When `FcPendingCommandSummary` renders,  
   Then it lists pending and rejected commands in a bounded `aria-live="polite"` summary, keeps
   rejected entries in non-dismissible error message bars, keeps pending/needs-review language honest
   about whether EventStore accepted the command, and never exposes command payloads, tenant/user
   claims, access tokens, or raw exception text.

6. **Existing safety gates remain ordered and unchanged.**  
   Given this story completes,  
   When reviewing generated forms, Shell services, and EventStore paths,  
   Then destructive confirmation, abandonment guard, policy authorization, FC-CNC one-at-a-time
   admission, FC-CMD ULID identity, status-endpoint polling, and public EventStore pact behavior remain
   intact.  
   Retry must run inside the EventStore dispatch adapter after authorization/tenant resolution and
   before accepted-result pending registration; it must not run for Stub dispatch unless the contract
   explicitly records a test-only fallback.

## Tasks / Subtasks

- [x] **Task 1 - Record the FC-RETRY v1 retry/degraded contract (AC: #1-#6)**
  - [x] Create `_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md`.
  - [x] Record the v1 dispatch retry budget. Unless a newer Product/UX + EventStore decision exists
        in source docs, use a conservative default: one retry after the initial pre-accept attempt,
        fixed deterministic delay, and no jitter so `FakeTimeProvider` tests are stable.
  - [x] Define retryable pre-accept faults narrowly: transport `HttpRequestException` without a
        non-retryable status, HTTP `408`, `502`, `503`, and `504` before `202 Accepted`. Do not retry
        validation `400`, auth `401/403`, not-found `404`, domain rejection `409`, rate limit `429`,
        malformed responses, oversized payload/body guards, tenant failures, cancellation, FC-CNC
        denial, or authorization denial.
  - [x] Record that every retry attempt must reuse the same `MessageId`; retrying by re-entering
        `DispatchAsync` and generating a fresh ULID is forbidden.
  - [x] Record slow-command semantics: accepted pending commands do not re-dispatch; they show
        degraded/action-prompt UI after `TimeoutActionThresholdMs`, continue polling at
        `PendingCommandPollingIntervalMs`, and expire to `NeedsReview` after
        `MaxPendingCommandPollingDurationMs`.
  - [x] Record non-goals: no queue/batching, no MCP tool retry policy, no EventStore status endpoint
        shape change, no projection fallback changes, no third-party retry package, no Polly package,
        no retry for non-EventStore Stub dispatch.

- [x] **Task 2 - Add bounded retry options and validation if the contract introduces public knobs (AC: #1, #2)**
  - [x] If configurable retry is needed, add `FcShellOptions` properties rather than hardcoded magic
        numbers; keep defaults matching the contract and validate with data annotations.
  - [x] Update `FcShellOptionsThresholdValidator` so retry delay/attempt settings cannot exceed the
        broader pending-command polling budget or create impossible degraded timing.
  - [x] If a public enum value or option is added under Contracts, update any owned public API
        baselines and package-boundary tests intentionally.
  - [x] Do not add `Version=` to any project file and do not introduce a new package dependency.

- [x] **Task 3 - Implement pre-accept EventStore dispatch retry without duplicating command identity (AC: #2, #3, #6)**
  - [x] Update `EventStoreCommandClient` so it generates one `MessageId` per generated submit and
        reuses that ID across all retry attempts.
  - [x] Rebuild the `HttpRequestMessage` and content per attempt; do not reuse disposed request or
        content instances.
  - [x] Keep `EventStoreHttp.ApplyAuthorizationAsync`, tenant resolution, command type, domain,
        aggregate ID, and payload serialization behavior unchanged except where retry requires a
        fresh request object.
  - [x] Preserve `Retry-After` handling for `202 Accepted` and `429 RateLimited`; do not treat those
        as automatic dispatch-retry delays unless the contract says so explicitly.
  - [x] On retry exhaustion, throw a bounded warning/degraded outcome consumed by generated forms.
        Exception/log text must not include command payloads, tenant/user claims, tokens, or raw
        server body.
  - [x] Keep `OperationCanceledException` behavior unchanged: caller cancellation must stop retrying
        immediately and must not be swallowed.

- [x] **Task 4 - Surface retry exhaustion through generated command forms (AC: #3, #6)**
  - [x] Extend `CommandWarningKind` or the existing warning model only if needed to distinguish
        retryable degraded dispatch failure from `Forbidden`, `NotFound`, `RateLimited`, and
        authorization `Pending`.
  - [x] Update `CommandFormEmitter` warning rendering so exhausted transient failures reset lifecycle
        to `Idle`, preserve form values, announce accessible warning copy, and display a bounded retry
        hint when available.
  - [x] Ensure generated forms do not dispatch `AcknowledgedAction`, call
        `PendingCommandState.Register`, or mutate pending state for exhausted pre-accept failures.
  - [x] Ensure the FC-CNC admission lease is released from existing `finally` paths after retry
        exhaustion and after successful accepted registration.
  - [x] Keep Inline, CompactInline, and FullPage density behavior unchanged.

- [x] **Task 5 - Pin slow accepted command degraded behavior and expiry (AC: #4)**
  - [x] Re-run or extend `FcLifecycleWrapperThresholdTests` to prove `ActionPrompt` appears at
        `TimeoutActionThresholdMs` for accepted/syncing commands and is replaced by `Confirmed`,
        `Rejected`, or idempotent-confirmed terminal outcomes.
  - [x] Re-run or extend `PendingCommandPollingDriverTests` to prove command-status polling keeps
        ticking independently of projection fallback polling and respects interval changes/disabled
        state.
  - [x] Extend `PendingCommandPollingCoordinatorTests` only where needed to prove expiry to
        `NeedsReview`, oldest-first cap behavior, first-wins late observation handling, and no query
        call after expiry still hold after retry changes.
  - [x] Do not change EventStore status mapping: `Completed -> Confirmed`, `Rejected -> Rejected`,
        `PublishFailed/TimedOut -> NeedsReview`, non-terminal statuses -> continue polling.

- [x] **Task 6 - Make `FcPendingCommandSummary` satisfy pending/rejected announcement semantics (AC: #5)**
  - [x] Update `FcPendingCommandSummary` so active `Pending` entries are represented, not filtered out
        by `TerminalEntries`.
  - [x] Keep details bounded by `MaxDetails`, sorted predictably, and accessible through the existing
        `aria-live="polite"` section.
  - [x] Add or update localized resource strings for pending, retryable degraded, and overflow/count
        copy in both `FcShellResources.resx` and `FcShellResources.fr.resx`.
  - [x] Preserve rejected entries as non-dismissible error `FluentMessageBar` instances and render all
        server-provided rejection text as plain text.
  - [x] Add bUnit tests covering mixed pending/rejected/needs-review/confirmed summaries, zero or
        negative `MaxDetails`, injected `IPendingCommandStateService` snapshots, and redaction/no raw
        payload leakage.

- [x] **Task 7 - Add focused retry and regression tests (AC: #1-#6)**
  - [x] Add `EventStoreCommandClient` tests proving retryable pre-accept faults retry exactly within
        budget and reuse the same `MessageId` in every request body.
  - [x] Add tests proving non-retryable statuses (`400`, `401`, `403`, `404`, `409`, `429`) do not
        retry and preserve their existing typed exception behavior.
  - [x] Add tests proving cancellation stops retrying and no later retry attempt is scheduled.
  - [x] Add generated-runtime tests proving retry exhaustion renders warning feedback, preserves form
        input, resets lifecycle to `Idle`, and does not register pending state.
  - [x] Re-run Story 4.1 destructive confirmation, Story 4.2 abandonment, Story 4.3 FC-CNC, and Story
        4.4 authorization focused pins if any generated submit ordering changes.
  - [x] Update pact files only if the wire contract intentionally changes. A retry implementation that
        only repeats the same existing request should not require a provider contract change.

- [x] **Task 8 - Verify build/tests and record evidence honestly (AC: #1-#6)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run focused Shell tests for EventStore command retry, response classification, pending command
        polling/driver/state, lifecycle wrapper thresholds, pending summary, generated runtime
        command forms, FC-CNC, authorization, destructive confirmation, and abandonment guard.
  - [x] Run focused SourceTools tests for `CommandFormEmitter` output and any changed generated
        snapshots.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` when the environment permits. If local VSTest/MSBuild sockets are blocked, use the established in-process fallback and state CI remains authoritative.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; any snapshot change must be intentional and
        listed.
  - [x] Keep the File List complete, including the FC-RETRY contract, story file, sprint status,
        source, tests, resources, pact/public API baselines, specimen/e2e files, and intentional
        snapshots.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- No separate PRD or UX artifact was found under `_bmad-output/planning-artifacts/`.
- Loaded architecture context from `_bmad-output/project-context.md` and
  `_bmad-output/project-docs/architecture.md`.
- Loaded previous story context from
  `_bmad-output/implementation-artifacts/4-4-policy-gated-command-authorization.md`.
- Loaded supporting contracts:
  `_bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md`,
  `_bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md`, and
  `_bmad-output/contracts/fc-auth-policy-gated-command-authorization-2026-06-04.md`.
- `sprint-status.yaml` has `epic-4: in-progress`, Stories 4.1-4.4 `done`, and
  `4-5-retry-and-degraded-state-handling: backlog` before this create-story run.
- External web research was not required: this story uses repo-pinned .NET 10, ASP.NET Core HTTP,
  FluentUI, bUnit, xUnit v3, and existing EventStore/pending-command seams; no dependency upgrade,
  external API, or new package is in scope.

### Epic and Story Context

- Epic 4 layers safe command UX on top of Epic 3 command lifecycle: destructive confirmation,
  abandonment guard, one-at-a-time execution, policy authorization, and retry/degraded handling.
  [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Safe & Concurrent Command Execution]
- Story 4.5 requires transient dispatch faults to retry within an agreed budget and requires
  `FcPendingCommandSummary` to list pending/rejected commands in an `aria-live` region.
  [Source: _bmad-output/planning-artifacts/epics.md#Story 4.5: Retry and degraded-state handling]
- The command budget contract explicitly left automatic client command retry at `0` for Epic 3 and
  assigned explicit retry/degraded retry handling to Epic 4. Story 4.5 must close that open budget.
  [Source: _bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md#Retry Scope]

### Current Source State to Preserve

- `FcShellOptions` already owns lifecycle and polling budgets:
  `TimeoutActionThresholdMs = 10000`, `PendingCommandPollingIntervalMs = 1000`,
  `MaxPendingCommandPollingDurationMs = 120000`, `MaxPendingCommandPollingPerTick = 25`, and
  `MaxPendingCommandEntries = 100`. [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs]
- `FcShellOptionsThresholdValidator` already enforces
  `MaxPendingCommandPollingDurationMs > TimeoutActionThresholdMs` and
  `PendingCommandPollingIntervalMs <= MaxPendingCommandPollingDurationMs` when polling is enabled.
  [Source: src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs]
- `EventStoreCommandClient` generates the dispatch `MessageId` inside `DispatchAsync`, sends a POST
  to `EventStoreOptions.CommandEndpointPath`, classifies the response, returns `CommandResult` on
  `202 Accepted`, and invokes lifecycle `Syncing` only after acceptance. Any retry must keep one
  `MessageId` across attempts and must not invoke the lifecycle callback before acceptance.
  [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]
- `EventStoreResponseClassifier` currently maps command `400`, `401`, `403`, `404`, `409`, and
  `429` to typed validation/auth/warning/rejection exceptions. It treats other non-202 command
  statuses as `HttpRequestException`. Preserve the typed branches and bounded ProblemDetails
  parsing. [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs]
- `CommandWarningException` carries `RetryAfter`, currently used for `RateLimited`, and generated
  forms display `Retry after N seconds.` in a warning message bar. Preserve this display path and
  keep all warning copy plain text. [Source:
  src/Hexalith.FrontComposer.Contracts/Communication/CommandWarningException.cs] [Source:
  src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `CommandFormEmitter` already catches `CommandWarningException`, publishes feedback, resets the
  generated lifecycle to `Idle`, preserves form state, and releases FC-CNC admission in `finally`.
  Story 4.5 should extend this path instead of adding a second warning system. [Source:
  src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `PendingCommandPollingDriver` is `TimeProvider`-driven, independent of projection fallback polling,
  prevents overlapping ticks, and honors `PendingCommandPollingIntervalMs = 0` as disabled.
  [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingDriver.cs]
- `PendingCommandPollingCoordinator` polls pending entries oldest-first within
  `MaxPendingCommandPollingPerTick`, re-checks state before query, expires unresolved entries to
  `NeedsReview`, and logs status-query exceptions without exposing raw exception text.
  [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs]
- `EventStorePendingCommandStatusQuery` queries `/api/v1/commands/status/{MessageId}` and maps
  EventStore statuses: non-terminal statuses continue polling, `Completed -> Confirmed`,
  `Rejected -> Rejected`, `PublishFailed/TimedOut -> NeedsReview`. [Source:
  src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs]
- `PendingCommandStateService` validates canonical 26-character ULID `MessageId` and `CorrelationId`,
  keeps only framework metadata, resolves `NeedsReview` through the existing lifecycle mutation
  boundary, and treats late terminal observations as duplicates. [Source:
  src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- `FcLifecycleWrapper` already surfaces `ActionPrompt` at `TimeoutActionThresholdMs`, uses
  assertive live-region semantics for action prompt/rejected states, and replaces degraded warning
  with terminal confirmed/rejected/idempotent outcomes. [Source:
  src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor]
- `FcPendingCommandSummary` currently filters out active `Pending` entries and only displays
  terminal entries. That conflicts with this story's pending-summary acceptance criterion and is a
  likely implementation gap.
  [Source: src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs]

### Previous Story and Git Intelligence

- Story 4.4 completed FC-AUTH and established that authorization gates must run before Stub/EventStore
  side effects, lifecycle callbacks, HTTP sends, FC-CNC admission effects, or pending-state mutations.
  Do not move retry ahead of authorization. [Source:
  _bmad-output/implementation-artifacts/4-4-policy-gated-command-authorization.md#Acceptance Criteria]
- Story 4.4 also pinned generated protected-form authorization composition; if Story 4.5 changes
  generated submit order, rerun those protected-form runtime pins. [Source:
  _bmad-output/implementation-artifacts/4-4-policy-gated-command-authorization.md#Tasks / Subtasks]
- Story 4.3 established FC-CNC v1 as block-not-queue. Retry must not create a queue, hidden
  background dispatch, or client-side batching path. [Source:
  _bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md#Decision]
- Recent commits are `feat(story-4.4)`, `feat(story-4.3)`, `feat(story-4.2)`, `feat(story-4.1)`,
  and `docs: record epic 3 retrospective`; local pattern is contract artifact first, focused
  guardrail tests, Release build, and honest VSTest socket caveats.

### Project Structure Notes

- Contract artifact belongs under `_bmad-output/contracts/`.
- Public retry knobs and warning enum changes belong under:
  - `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`
  - `src/Hexalith.FrontComposer.Contracts/Communication/CommandWarningKind.cs`
- EventStore retry implementation belongs under:
  - `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`
  - possibly `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`
- Generated warning/rendering changes belong under:
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- Pending summary changes belong under:
  - `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor`
  - `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs`
  - `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
  - `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- Focused tests likely belong under:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/`

### Implementation Guardrails

- Use repo-pinned frameworks and test stack from `_bmad-output/project-context.md`; do not add Polly,
  FluentUI icons, third-party analyzers, or any package version in a `.csproj`.
- Keep all async awaits with `ConfigureAwait(false)` in non-Blazor library/service code.
- Preserve ULID-only `MessageId`/`CorrelationId`; never use `Guid.NewGuid()` or parse these IDs as
  GUIDs.
- Keep retry tests deterministic with `FakeTimeProvider`; avoid wall-clock sleeps.
- Do not log or render raw exception messages from transport/status-query failures because they can
  contain URLs, headers, tenant data, payload fragments, or tokens.
- Keep generated forms' existing order: validate, authorization, destructive `BeforeSubmit`, FC-CNC
  admission, generated `SubmittedAction`, EventStore dispatch, pending registration, then
  `AcknowledgedAction` only for accepted/registrable results.
- Do not change MCP command tool retry semantics; Epic 5 owns agent-facing command admission and
  lifecycle subscription behavior.

### Checklist Validation Notes

- Reinvention prevention: story points to existing EventStore classifier/client, generated warning
  path, pending polling coordinator/driver, lifecycle wrapper, and pending summary instead of asking
  for a new retry framework or UI system.
- Technical disaster prevention: story forbids new `MessageId` per retry, retries after acceptance,
  retries of non-idempotent domain failures, raw exception leakage, dependency additions, and ordering
  regressions around FC-AUTH/FC-CNC/pending registration.
- File-location prevention: story lists concrete source/test/resource paths and contract location.
- Regression prevention: story requires focused reruns for Stories 4.1-4.4 when submit ordering
  changes and requires pact/public API baseline updates only when intentionally affected.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Started dev-story workflow for Story 4.5; existing baseline commit preserved.
- 2026-06-05: Red phase confirmed missing retry options via focused Shell compile failure before implementation.
- 2026-06-05: Release build passed: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` -> 0 warnings / 0 errors.
- 2026-06-05: Focused in-process lanes passed: Shell retry/options/summary 86/86, Contracts options 33/33, SourceTools command-form emitter 30/30, Shell wrapper runtime 12/12, Shell safety/regression pins 177/177, SourceTools destructive/auth/parser pins 69/69.
- 2026-06-05: Required solution-level VSTest command was blocked before execution by `System.Net.Sockets.SocketException (13): Permission denied`; used xUnit v3 in-process fallback. Broad fallback: Contracts 177/177 green, SourceTools 1009 total / 3 known unrelated failures, Shell 1850 total / 5 known unrelated/environmental failures.
- 2026-06-05: Intentional `.verified.txt` changes limited to two generated command-form snapshots for retryable warning fallback rendering.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Recorded FC-RETRY v1 contract with one retry after the initial pre-accept attempt, deterministic 250 ms delay, retryable HTTP 408/502/503/504 plus transport `HttpRequestException`, and explicit non-goals.
- Added `FcShellOptions` retry knobs and validator coverage without new packages or project-file version changes.
- Implemented EventStore pre-accept retry inside `EventStoreCommandClient`, rebuilding request/content per attempt while reusing the same `MessageId` and preserving typed non-retryable exception behavior.
- Added `CommandWarningKind.RetryableDispatchFailed` and generated-form warning fallback so exhausted transient pre-accept failures reset lifecycle to `Idle`, preserve input, publish accessible warning feedback, and avoid pending registration/acknowledgement.
- Updated `FcPendingCommandSummary` to include active pending entries, bounded predictable ordering, honest pending/needs-review copy, EN/FR resources, and bUnit coverage for explicit entries plus service snapshots.
- Slow accepted command behavior, pending polling expiry, destructive confirmation, abandonment, FC-CNC, and authorization guardrails were re-run and remained green in focused lanes.

### File List

- `_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/4-5-retry-and-degraded-state-handling.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandWarningKind.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`
- `src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/FcShellOptionsVirtualizationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/FcPendingCommandSummaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/e2e/package.json`
- `tests/e2e/specs/retry-and-degraded-state-handling.spec.ts`

### Change Log

- 2026-06-05: Implemented FC-RETRY v1 pre-accept EventStore retry/degraded warning behavior and pending summary announcement semantics; status moved to review.
- 2026-06-05: Story-automator adversarial review. Build 0/0; focused lanes green independently. Auto-fixed 3 MEDIUM findings (File List completeness, FR localization of the retry-exhaustion warning, weak same-MessageId test). 0 CRITICAL/HIGH. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-05 · **Outcome:** Approved (with auto-fixes applied)

### Verification performed

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors**.
- Focused lanes (Release, `--no-build`, in-process VSTest): EventStore client **27/27**, pending-summary + options + generated retry-exhaustion **99/99**, SourceTools `CommandFormEmitter` **30/30**, Contracts options **33/33**, resource/FR-parity **83/83**.
- Read every File List source/test file. Re-derived retry classification against `EventStoreResponseClassifier`: the `default` branch stamps `HttpRequestException.StatusCode`, so only `408/502/503/504` + status-less transport faults retry; `400/401/403/404/409/429` map to typed exceptions and `500` is **not** retried — matches the FC-RETRY contract.
- Confirmed AC ordering: retry runs inside the EventStore dispatch adapter after tenant resolution and before pending registration; authorization is enforced by the upstream decorator; Stub dispatch is untouched. Exhaustion throws `CommandWarningException(RetryableDispatchFailed)`; the generated form resets to `Idle`, preserves input, publishes accessible feedback, and never registers pending state or dispatches `AcknowledgedAction`.
- Pre-existing/unrelated failures (NOT regressions): 4 Shell tests (`CounterStoryVerificationTests` ×2 projection-view snapshots, `NavigationEffectsLastActiveRouteTests`, `CommandRendererFullPageTests` query fallback) and 3 SourceTools tests (IDE-parity Linux path case, docs DataGrid contract, diagnostics ledger). All seven reproduced identically on a clean `git stash` baseline with my changes removed.

### Findings and resolutions

All ACs validated as implemented; every task marked `[x]` is genuinely done. No CRITICAL or HIGH findings.

- **MEDIUM — File List incomplete (Task 8).** `tests/e2e/specs/retry-and-degraded-state-handling.spec.ts` (new) and `tests/e2e/package.json` (modified) were changed in git but absent from the File List. **Fixed:** both added to the File List.
- **MEDIUM — FR operators saw hardcoded English on retry exhaustion.** `EventStoreCommandClient.CreateRetryExhaustedWarning` always populates an English `Title`/`Detail`, and the generated form preferred `_serverWarning.Detail`, so the dev's `RetryableDispatchFailedFallback` FR resource was unreachable dead code. **Fixed:** `CommandFormEmitter` now renders the localized title+body for `CommandWarningKind.RetryableDispatchFailed` (added `RetryableDispatchFailedTitle` to EN+FR resx; EN copy preserves existing wording so runtime assertions stay green). Regenerated the two intentional `CommandFormEmitter` `.verified.txt` snapshots; FR/EN resource parity test green.
- **MEDIUM — "same MessageId" retry test could not fail.** `CommandClient_RetryablePreAcceptStatus_RetriesOnceWithSameMessageId` used `FixedUlidFactory` (constant ULID), so it would pass even if the implementation regenerated the ID per attempt — defeating the AC #2 / Task 3 guarantee. **Fixed:** replaced with a `CountingUlidFactory` that issues distinct values per call; the test now asserts `NewUlid` is called exactly once and a single distinct `messageId` appears across both request bodies.
- **LOW (noted, not changed).** `EventStoreResponseClassifier`'s class-doc remark that 503 is "treated the same as any other 5xx … reconnect/polling UX belongs to Stories 5-3 through 5-5" predates this story's client-layer pre-accept retry; the classifier behavior itself is unchanged, so the comment remains accurate at that layer.
