---
baseline_commit: 437d3e97cf07abc1310f6363781a62b7e52fc008
---

# Story 4.3: One-at-a-time execution policy (FC-CNC)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. Generated command forms already suppress repeated submits
> for their own lifecycle state, and pending-command state already tracks accepted commands by
> `MessageId`. That does not yet provide a circuit-wide one-at-a-time policy: two different command
> forms, two renderers, or a rapid submit before pending registration can still race. Story 4.3 owns
> the FC-CNC v1 contract and its runtime admission gate. The v1 fallback is **block, do not queue**:
> if another command is admitted or pending, the next submit is rejected locally with clear feedback
> and no `SubmittedAction`, no HTTP dispatch, and no background client queue. Batching and queued
> execution are fast-follow and must not be built here.

## Story

As an operator,
I want commands to execute one at a time in v1,
so that rapid sequences stay predictable while batching is deferred.

## Acceptance Criteria

1. **FC-CNC contract is recorded and unambiguous.**  
   Given the FC-CNC contract artifact is created under `_bmad-output/contracts/`,  
   When reviewed,  
   Then it states that v1 command execution is one-at-a-time per circuit/user scope, the approved
   fallback is **block/reject the later local submit**, no client-side queue is created, and batching
   is recorded as fast-follow.  
   And it identifies the lock lifetime: pre-dispatch admission is held until the command is accepted
   and registered as pending, then `PendingCommandStateService` keeps the command in-flight until a
   terminal pending status (`Confirmed`, `Rejected`, `IdempotentConfirmed`, or `NeedsReview`).

2. **Rapid second submits are blocked before any side effect.**  
   Given a command submit has been admitted but has not yet completed its initial dispatch round trip,  
   When any generated command form in the same scoped shell circuit submits another command,  
   Then the second submit is blocked before `SubmittedAction`, `ICommandService.DispatchAsync`,
   pending registration, EventStore HTTP send, or lifecycle mutation.  
   And the original in-flight command continues normally.

3. **Accepted pending commands block later submits until terminal resolution.**  
   Given a command has returned `Accepted` and is registered in `IPendingCommandStateService` with
   `PendingCommandStatus.Pending`,  
   When another generated command form submits before that pending entry resolves,  
   Then the later submit is blocked locally with operator-visible feedback and no dispatch.  
   When the pending entry resolves to a terminal status through the existing resolver/polling path,  
   Then a later submit can be admitted.

4. **Operator feedback is accessible and preserves form input.**  
   Given a submit is blocked by FC-CNC,  
   When the generated form re-renders,  
   Then the operator sees a non-modal warning that another command is already in progress, the form
   input remains intact and correctable, focus is not stolen, and the message is announced through
   the existing warning/live-region pattern.  
   The copy must not claim the command was queued, retried, or submitted.

5. **Existing Epic 3 and Epic 4 behavior is preserved.**  
   Given this story completes,  
   When reviewing the diff,  
   Then FC-CMD identity/correlation remains ULID-only, pending registration/resolution remains the
   shared `MessageId` mutation boundary, EventStore status polling and numeric budgets remain as
   decided in Story 3.6, destructive confirmation still validates and confirms before dispatch,
   abandonment guard behavior is unchanged, `[RequiresPolicy]` authorization behavior remains Story
   4.4 scope, and retry/degraded retry remains Story 4.5 scope.

## Tasks / Subtasks

- [x] **Task 1 - Confirm and record the FC-CNC contract (AC: #1)**
  - [x] Create `_bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md`.
  - [x] Record v1 scope as circuit/user-scoped one-at-a-time command admission.
  - [x] Record fallback as block/reject later local submits; explicitly state no queue, no batching,
        no automatic retry, and no hidden background dispatch.
  - [x] Record lock ownership: a short pre-accept admission lock closes the dispatch race; accepted
        commands are considered in-flight while `IPendingCommandStateService.Snapshot()` contains a
        `Pending` entry.
  - [x] Link the contract to AR7 FC-CNC and note batching as fast-follow.

- [x] **Task 2 - Add a scoped command execution admission gate (AC: #2, #3)**
  - [x] Add a small Shell service under `src/Hexalith.FrontComposer.Shell/Services/` or
        `src/Hexalith.FrontComposer.Shell/State/PendingCommands/` following local naming and
        namespace conventions.
  - [x] Register it as `Scoped` in `AddHexalithFrontComposerQuickstart()` so both Stub and EventStore
        hosts inherit the policy; do not use static/process-wide state.
  - [x] The gate must hold an atomic admitted-but-not-yet-pending flag and must also deny admission
        when `IPendingCommandStateService.Snapshot()` has any `Status == Pending`.
  - [x] The gate must store only framework metadata needed for diagnostics; do not store command
        payloads, form values, tenant/user raw claims, validation messages, or server responses.
  - [x] Denial reasons should distinguish at least "admission already in progress" and "pending
        command already exists" for tests/logging, but user copy can be a single localized warning.
  - [x] Releasing the pre-accept admission must happen in `finally` paths for validation/auth/dispatch
        exceptions and after successful pending registration so an exception cannot permanently lock
        the circuit.

- [x] **Task 3 - Wire generated command forms through the gate (AC: #2, #3, #4, #5)**
  - [x] Update `CommandFormEmitter` to inject the gate into generated forms.
  - [x] Call the gate after client validation, authorization checks, and `BeforeSubmit` have completed,
        but before allocating/submitting lifecycle side effects that represent an actual command
        attempt. Preserve existing destructive dialog validation/confirmation ordering.
  - [x] If admission is denied, set the generated form warning state and publish through the existing
        command feedback/message-bar pattern, leave the form input intact, and return before `SubmittedAction`,
        `CommandService.DispatchAsync`, and `PendingCommandState.Register`.
  - [x] If admission succeeds, keep the existing order: ensure bridge/subscriber, dispatch
        `SubmittedAction`, call `CommandService.DispatchAsync`, register pending on `Accepted`, and
        dispatch `AcknowledgedAction` only when pending registration permits it.
  - [x] Preserve generated form behavior for `CommandValidationException`, `CommandWarningException`,
        `AuthRedirectRequiredException`, `CommandRejectedException`, and `OperationCanceledException`.
  - [x] Do not change generated command density, full-page/inline render modes, FC-CMD ULID
        generation, EventStore command payload shape, or pending resolver semantics.

- [x] **Task 4 - Keep service-level/direct-call behavior honest (AC: #2, #3, #5)**
  - [x] Audit `AuthorizingCommandServiceDecorator`, `StubCommandService`, and
        `EventStoreCommandClient` before deciding whether an additional service-decorator guard is
        needed for non-generated callers.
  - [x] If a decorator is added, it must compose with the existing authorization decorator without
        bypassing authorization or changing HTTP request payloads.
  - [x] Do not use a decorator as the only generated-form race fix unless tests prove there is no
        gap between `DispatchAsync` returning `Accepted` and generated pending registration.
  - [x] Leave MCP command tools out of scope unless a direct compile break forces a shared interface
        change; Epic 5 owns MCP-facing policy.

- [x] **Task 5 - Add focused tests that prove the race is closed (AC: #1-#5)**
  - [x] Add Shell unit tests for the admission gate: first admission succeeds, concurrent/second
        admission fails, pending snapshot blocks admission, terminal pending entries do not block,
        and failed/exceptional paths release the pre-accept flag.
  - [x] Add SourceTools emitter tests proving generated forms inject the gate and call it before
        `SubmittedAction` / `CommandService.DispatchAsync` / `PendingCommandState.Register`.
  - [x] Add generated renderer/form runtime tests with a controlled `ICommandServiceWithLifecycle`
        proving two rapid submits result in exactly one dispatch while the first round trip is held.
  - [x] Add a runtime test proving an accepted pending command blocks a later submit until
        `PendingCommandStateService.ResolveTerminal(...)` resolves it.
  - [x] Add a test proving blocked submits render/publish warning feedback, keep form values, and do
        not mutate lifecycle or pending state.
  - [x] Re-run affected Story 4.1 destructive and Story 4.2 abandonment pins if submit ordering or
        `BeforeSubmit` placement changes.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run focused Shell tests for the new admission gate, pending-command state interactions, and
        generated renderer/form runtime behavior.
  - [x] Run focused SourceTools tests for `CommandFormEmitter` output and any affected snapshots.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` when the environment permits. If local VSTest/MSBuild sockets are blocked, use the established in-process fallback and state CI remains authoritative.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; any snapshot change must be intentional and
        listed.
  - [x] Keep the File List complete, including the FC-CNC contract, story file, sprint status,
        source, tests, specimen/e2e files, and intentional snapshots.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- Loaded readiness input from `_bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md`.
- No separate PRD, architecture, or UX artifact was found under `_bmad-output/planning-artifacts/`;
  architecture fallback came from `_bmad-output/project-context.md` and `_bmad-output/project-docs/*`.
- Loaded previous story context from
  `_bmad-output/implementation-artifacts/4-2-unsaved-form-abandonment-guard.md`.
- `sprint-status.yaml` has `epic-4: in-progress`, Stories 4.1 and 4.2 `done`, and
  `4-3-one-at-a-time-execution-policy-fc-cnc: backlog` before this create-story run.
- External web research was not required: implementation depends on repo-pinned .NET 10, Fluxor,
  FluentUI v5 RC, bUnit, xUnit v3, and existing local command/pending contracts.

### Epic and Story Context

- Epic 4 layers safe command UX on top of Epic 3 command lifecycle: destructive confirmation,
  abandonment guard, one-at-a-time execution, policy authorization, and retry/degraded handling.
  [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Safe & Concurrent Command Execution]
- Story 4.3 is the FC-CNC story. The epics require one-at-a-time execution when another command is
  in flight and require the v1 contract to confirm one-at-a-time while recording batching as
  fast-follow. [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3: One-at-a-time execution policy (FC-CNC)]
- The readiness request asks FrontComposer + Product/UX to own FC-CNC, confirms one-at-a-time as
  the v1 contract, and states batching is fast-follow. [Source:
  _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md#Asks - ordered by what they unblock]
- AR7 FC-CNC is separate from AR8 numeric budgets. Story 3.6 already decided command-status polling
  and degraded thresholds; do not reopen them here. [Source:
  _bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md#Decision]

### Current Source State to Preserve

- `CommandFormEmitter` generated forms already prevent repeated submits for their own generated
  lifecycle state: only `Idle`, `Rejected`, or `Confirmed` may submit. This is not global across
  command forms or command types. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- Generated submit order today is: validate and clear server errors, run optional authorization,
  run `BeforeSubmit`, allocate `CorrelationId` via `IUlidFactory`, ensure lifecycle bridge and
  last-used subscriber, dispatch `SubmittedAction`, call `ICommandService.DispatchAsync`, register
  accepted pending state, then dispatch `AcknowledgedAction` when pending registration allows it.
  [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `IPendingCommandStateService` is scoped/circuit-local and exposes `Snapshot()`. Its entries are
  keyed by canonical 26-character Crockford ULID `MessageId`; only entries with
  `PendingCommandStatus.Pending` should block FC-CNC admission. Terminal entries remain visible but
  must not block new commands. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- `PendingCommandStateService` registers only after the command service returns `Accepted`. That
  means FC-CNC needs a pre-dispatch admission flag/lease in addition to checking pending snapshots,
  otherwise two rapid submits can pass before either one becomes pending. [Source:
  src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `FcShellOptions` already has pending-command caps and polling budgets:
  `PendingCommandPollingIntervalMs = 1000`, `MaxPendingCommandPollingDurationMs = 120000`,
  `MaxPendingCommandEntries = 100`, and `MaxPendingCommandPollingPerTick = 25`. Do not add retry or
  polling semantics for FC-CNC. [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs]
- `AuthorizingCommandServiceDecorator` wraps both Stub and EventStore `ICommandServiceWithLifecycle`
  registrations before dispatch. If a service-level FC-CNC decorator is added, preserve that
  authorization-before-side-effect invariant. [Source:
  src/Hexalith.FrontComposer.Shell/Services/Authorization/AuthorizingCommandServiceDecorator.cs]
- `StubCommandService` and `EventStoreCommandClient` both generate accepted `MessageId` values with
  `IUlidFactory`. Do not add GUID fallbacks or change EventStore command request shape. [Source:
  src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs] [Source:
  src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]

### Previous Story and Git Intelligence

- Story 4.2 completed with no production source changes and added generated full-page
  renderer/form abandonment pins. Its follow-ups are test-helper cleanup and a pre-existing
  full-page return-path fallback failure; neither is FC-CNC scope. [Source:
  _bmad-output/implementation-artifacts/4-2-unsaved-form-abandonment-guard.md#Review Follow-ups (AI)]
- Story 4.2 explicitly did not implement FC-CNC queue/block policy; Story 4.3 is the first story
  that should touch that surface. [Source:
  _bmad-output/implementation-artifacts/4-2-unsaved-form-abandonment-guard.md#Tasks / Subtasks]
- Story 4.1 established the destructive confirmation pattern: generated-runtime proof is required,
  not only standalone component tests. Keep that testing standard here. [Source:
  _bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md#Completion Notes List]
- Recent commits are `feat(story-4.2)`, `feat(story-4.1)`, `docs: record epic 3 retrospective`,
  and `feat(story-3.6)`. The local pattern is narrow scoped changes, focused tests, and explicit
  sandbox/CI caveats.

### Project Structure Notes

- FC-CNC Shell service code should stay under:
  - `src/Hexalith.FrontComposer.Shell/Services/`
  - or `src/Hexalith.FrontComposer.Shell/State/PendingCommands/` if the implementation is tightly
    coupled to pending-command state.
- Generated form changes belong under:
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- Generated runtime pins belong under:
  - `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
- Shell unit tests for admission/pending behavior belong under:
  - `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/`
  - or `tests/Hexalith.FrontComposer.Shell.Tests/Services/`
- Do not edit generated output under `obj/**/generated/HexalithFrontComposer/`; change the
  generator or tests. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Do not write scratch docs into `docs/`; contract/story/evidence artifacts belong under
  `_bmad-output/`. [Source: _bmad-output/project-context.md#Development Workflow Rules]

### Technical Guardrails

- Keep FC-CNC state scoped per circuit/user. Never use static, singleton, or process-wide command
  locks; that would block unrelated users and violate the scoped-lifetime rule.
- Do not store command payloads or form field values in the admission gate. Use only command type,
  display label, timestamps, and pending `MessageId`/`CorrelationId` metadata when needed.
- Use existing `CommandFeedbackWarning` / `ICommandFeedbackPublisher` and generated form warning
  rendering patterns for blocked-submit feedback; do not add another toast/dialog library.
- Gate before `SubmittedAction` and `ICommandService.DispatchAsync`. A blocked submit must not look
  like a submitted command in lifecycle, pending state, telemetry, or EventStore.
- Preserve `BeforeSubmit` semantics. For destructive commands, validation and confirmation must
  still happen before dispatch; if another command starts while the dialog is open, the post-dialog
  FC-CNC admission attempt should block cleanly and keep the form editable.
- Keep `PendingCommandStateService` as the accepted-command source of truth. Do not create a second
  pending-command registry or duplicate command-status polling path.
- No automatic client retry, retry budget, degraded retry UI, or queue draining in this story.
- Continue using `IUlidFactory` for `MessageId` and `CorrelationId`; never introduce GUID parsing or
  formatting for command identity.

### Testing Requirements

- Use xUnit v3, Shouldly, NSubstitute, bUnit, and Verify.XunitV3 where applicable; do not use raw
  `Assert.*`. [Source: _bmad-output/project-context.md#Testing Rules]
- Run tests with `DiffEngine_Disabled=true` for Verify snapshot safety.
- Preferred focused lanes:
  - new FC-CNC admission gate tests
  - `PendingCommandStateServiceTests`
  - `CommandFormEmitterTests`
  - generated renderer/form runtime tests under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
  - affected `DestructiveCommandRendererIntegrationTests` and
    `CommandRendererFullPageTests` if submit/`BeforeSubmit` ordering changes
- Generated runtime tests should use a controlled `ICommandServiceWithLifecycle` with
  `TaskCompletionSource` or equivalent to hold the first dispatch and prove the second submit does
  not dispatch.
- Any `.verified.txt` change must be intentional, reviewed, and listed in the Dev Agent Record.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3: One-at-a-time execution policy (FC-CNC)]
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md#Asks - ordered by what they unblock]
- [Source: _bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md#Lifecycle Ownership]
- [Source: _bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md#Retry Scope]
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Services/Authorization/AuthorizingCommandServiceDecorator.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Implemented scoped `CommandExecutionAdmissionGate` under pending-command state and registered it through the Shell service graph.
- 2026-06-04: Updated `CommandFormEmitter` so generated forms attempt FC-CNC admission after validation/authorization/`BeforeSubmit` and before correlation allocation, lifecycle submission, command dispatch, and pending registration.
- 2026-06-04: Audited `AuthorizingCommandServiceDecorator`, `StubCommandService`, and `EventStoreCommandClient`; no service decorator was added because Stub/EventStore return `Accepted` before generated pending registration, so a decorator cannot close the generated-form race alone.
- 2026-06-04: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-04: Validation: focused Shell in-process lane passed 107/107 for `FcShellResourcesTests`, `CommandExecutionAdmissionGateTests`, `PendingCommandStateServiceTests`, and `CommandRendererWrapperIntegrationTests`.
- 2026-06-04: Validation: focused SourceTools in-process `CommandFormEmitterTests` passed 29/29 after accepting two intentional generated-form snapshots.
- 2026-06-04: Validation: affected destructive pins passed 3/3; full-page pins passed 12/12 when excluding the known pre-existing `Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` failure from Story 4.2. The combined affected run reproduced only that known failure.
- 2026-06-04: Validation: required solution-level VSTest command with `DiffEngine_Disabled=true`, `--no-build`, default trait exclusions, `-m:1`, and `/nr:false` was attempted and remained locally blocked before execution by `System.Net.Sockets.SocketException (13): Permission denied`; CI remains authoritative for that lane.
- 2026-06-04: Validation: broader SourceTools default in-process fallback with configured trait exclusions ran 1006 tests with 3 known pre-existing failures: DataGrid docs FC-DOC section, missing `deferred-work.md` ledger, and IDE parity case-sensitive path normalization.
- 2026-06-04: Validation: broader Shell default in-process fallback with configured trait exclusions ran 1815 tests with 5 known pre-existing/environmental failures: Pact mock server socket, navigation last-active-route hydration, two CounterStoryVerification snapshot/culture baselines, and the known full-page query-fallback baseline. Resource parity is green after adding FC-CNC keys to both canonical and French resources.
- 2026-06-04: Validation: `git diff --name-only -- '*.verified.txt'` lists only the two intentional `CommandFormEmitterTests` snapshots.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Created the FC-CNC v1 contract artifact recording circuit/user-scoped one-at-a-time admission, block-not-queue fallback, lock lifetime, and batching as fast-follow.
- Added a scoped admission gate that stores framework metadata only, distinguishes pre-dispatch and pending-command denials, and releases admitted leases idempotently.
- Wired generated command forms through the gate before command side effects. Blocked submits publish/render the existing warning message-bar feedback, preserve form values, and return before lifecycle, dispatch, pending registration, or EventStore send.
- Kept direct service dispatch behavior unchanged after audit; authorization decorator composition and Stub/EventStore payloads remain untouched.
- Added focused unit, emitter, and generated-runtime tests proving the race is closed and pending terminal resolution re-opens admission.

### File List

- `_bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md`
- `_bmad-output/implementation-artifacts/4-3-one-at-a-time-execution-policy-fc-cnc.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionGate.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/ICommandExecutionAdmissionGate.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/CommandExecutionAdmissionGateTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/e2e/package.json`
- `tests/e2e/specs/one-at-a-time-execution-policy.spec.ts`
- `_bmad-output/implementation-artifacts/tests/4-3-test-summary.md`

### Change Log

- 2026-06-04: Added FC-CNC contract, scoped admission gate, generated-form gate wiring, blocked-submit warning feedback, focused Shell/SourceTools coverage, and validation evidence. Status set to review.
- 2026-06-04: Senior Developer Review (AI) — adversarial review passed. Re-verified Release build (0/0) and re-ran focused Shell + SourceTools lanes (15/15). Fixed File List completeness (added e2e spec, e2e `package.json`, and 4-3 test summary). No CRITICAL/HIGH issues. Status set to done; sprint status synced.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Outcome:** Approve (auto-fix applied)

### Verification performed

- **Build:** `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors** (confirmed, not just claimed).
- **Tests (re-run, in-process VSTest, `DiffEngine_Disabled=true`, `--no-build`):**
  - `CommandExecutionAdmissionGateTests` (admit-first, deny-second, pending-blocks, 4× terminal-does-not-block, exceptional-release) — pass.
  - `GeneratedForms_RapidSecondSubmit_BlocksBeforeDispatchLifecycleAndPendingMutation` — pass (proves second submit blocks while first dispatch held: `DispatchCount == 1`, empty pending snapshot, form values preserved, one warning, no "queued" copy).
  - `GeneratedForms_PendingCommandBlocksUntilTerminalResolution` — pass (accepted pending blocks later submit; `ResolveTerminal(Confirmed)` re-opens admission → `DispatchCount == 2`).
  - SourceTools `Emit_InjectsCommandExecutionAdmissionGate`, `Emit_CommandExecutionAdmissionRunsAfterBeforeSubmitBeforeSideEffects`, `Emit_CommandExecutionAdmissionReleasesInFinally`, plus the two affected `.verified.txt` snapshots — pass. **Total: 15/15.**

### Acceptance Criteria

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC1 — contract recorded/unambiguous | IMPLEMENTED | `_bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md` states one-at-a-time per circuit/user, block/reject fallback, no queue/batch/retry/hidden dispatch, lock lifetime, terminal statuses, AR7 link, batching fast-follow. |
| AC2 — rapid second submit blocked before side effects | IMPLEMENTED | Gate `TryAcquire` runs after `BeforeSubmit`, before `SubmittedAction`/`DispatchAsync`/`Register`; `_currentAdmission` flag closes the pre-pending race. Runtime + emitter-ordering tests confirm. |
| AC3 — accepted pending blocks until terminal | IMPLEMENTED | Gate denies when `Snapshot()` has any `Status == Pending`; terminal entries do not block. Runtime test confirms re-open after `ResolveTerminal`. |
| AC4 — accessible feedback, input preserved | IMPLEMENTED | Localized warning (canonical + `fr`) via existing `CommandFeedbackPublisher`/`_serverWarning`; copy avoids queued/retried/submitted; form values retained (e2e + runtime markup assertions). |
| AC5 — Epic 3/4 behavior preserved | IMPLEMENTED | ULID-only identity, shared `MessageId` boundary, polling/budgets, destructive `BeforeSubmit` ordering, abandonment guard all untouched; only two intentional snapshots changed. |

### Findings

- **MEDIUM — File List incomplete (fixed).** `tests/e2e/specs/one-at-a-time-execution-policy.spec.ts`, `tests/e2e/package.json`, and `_bmad-output/implementation-artifacts/tests/4-3-test-summary.md` were present in git but missing from the File List, despite Task 6 requiring e2e files be listed. Added.
- **LOW — accepted (no change).** Generated blocked path guards `if (_serverWarning is not null)` immediately after `SetCommandInProgressWarning` always assigns it (defensive, harmless). The `typeof(...).FullName ?? nameof(...)` fallback for the command type name is unreachable for concrete types but matches the existing pending-registration convention. Neither is a defect; fixing would churn the two verified snapshots for no behavioral benefit.

No CRITICAL or HIGH issues; no false `[x]` task claims; no fabricated File List entries (every listed file has corresponding git changes).
