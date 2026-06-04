---
baseline_commit: cbe4d42d8334c46beb806ed53a0a39b0534a0e3d
---

# Story 3.4: Command lifecycle UI

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The lifecycle UI is not a blank slate. `FcLifecycleWrapper`,
> `LifecycleUiState`, generated command Fluxor actions/reducers, generated command forms, lifecycle
> bridges, `ILifecycleStateService`, pending-command state, and pending-command summary UI already
> exist and are heavily tested from Stories 2.4, 3.1, 3.2, and 3.3.
>
> Story 3.4 should confirm and close the operator-facing lifecycle UI contract, not rebuild the
> command pipeline. Two real remaining gaps are visible from the current source:
>
> - `Acknowledged` is currently silent in `FcLifecycleWrapper`; the epic requires it to be surfaced.
> - Typed rejection metadata named by the epic (`errorCode`, `reasonCategory`, `suggestedAction`,
>   `docsCode`) is not yet carried through the Shell UI.
>
> Today generated forms catch `CommandRejectedException`, dispatch
> `RejectedAction(correlationId, ex.Message, ex.Resolution)`, and `FcLifecycleWrapper` renders a
> plain-text title/body. Preserve that UX and form retry behavior, but promote rejection data to a
> typed, testable contract.

## Story

As an operator,
I want to see a command progress through its lifecycle,
so that I know whether it was acknowledged, confirmed, or rejected.

## Acceptance Criteria

1. **Lifecycle wrapper surfaces every command phase.**  
   Given a submitted generated command form,  
   When it progresses through the normal local lifecycle,  
   Then `FcLifecycleWrapper` surfaces `Submitting -> Acknowledged -> Syncing -> Confirmed/Rejected`
   through the existing live region, badge, and message-bar surfaces,  
   And the wrapped form remains present while lifecycle feedback renders.

2. **Typed rejection metadata is displayed and remains safe.**  
   Given a command rejection carrying `errorCode`, `reasonCategory`, `suggestedAction`, and
   `docsCode`,  
   When the generated form receives the rejection,  
   Then the rejection message bar exposes those fields in plain text,  
   And the form stays editable/correctable with current field values intact,  
   And no rejection field is rendered as markup or logged with raw unbounded server payload.

3. **Generated lifecycle state preserves correlation isolation.**  
   Given two command form instances of the same command type,  
   When one instance submits, progresses, or rejects,  
   Then only the matching `CorrelationId` updates its visible lifecycle state,  
   And stale callbacks from a previous submit do not overwrite a newer submit.

4. **Each lifecycle transition has one dispatch source.**  
   Given the generated command Fluxor slice and lifecycle bridge,  
   When state transitions occur,  
   Then `Submitted`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, and `ResetToIdle` each have
   one framework-owned dispatch path,  
   And reducers remain pure while effects/services own async work and interop.

5. **Prior command contracts do not regress.**  
   Given Stories 3.1 through 3.3,  
   When this story completes,  
   Then generated artifact inventory, density rules, derivable-field exclusion, ULID-only
   `CorrelationId`/`MessageId`, pending registration, idempotent confirmed behavior, and the FC-CMD
   identity/correlation contract remain green,  
   And Story 3.5 still owns binding `GET /api/v1/commands/status/{id}` while Story 3.6 still owns
   confirming/degraded/polling budgets.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live lifecycle UI path before editing (AC: #1-#5)**
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` around
        `OnValidSubmitAsync`, the `CommandRejectedException` catch, `BuildRenderTree`, and
        rejection-copy helpers.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs`,
        `CommandFluxorFeatureEmitter.cs`, and `CommandLifecycleBridgeEmitter.cs` before changing
        generated action/state shape.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor`,
        `FcLifecycleWrapper.razor.cs`, and `LifecycleUiState.cs` before changing rendered UI.
  - [x] Read `src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs` and
        `ProblemDetailsPayload.cs` before introducing typed rejection fields.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`
        to understand how HTTP 409 currently becomes `CommandRejectedException`.
  - [x] Read the Story 3.3 FC-CMD contract artifact and preserve its `MessageId`/`CorrelationId`
        ownership model.

- [x] **Task 2 - Define the typed rejection contract without breaking existing callers (AC: #2, #5)**
  - [x] Add a small public Contracts-side shape for command rejection details, or extend
        `CommandRejectedException` with optional typed properties while keeping the existing
        `(string reason, string resolution)` constructor working.
  - [x] Required fields are `ErrorCode`, `ReasonCategory`, `SuggestedAction`, and `DocsCode`.
        Treat unknown or absent values as safe fallback text, not as null-reference failures.
  - [x] Keep all rejection strings plain text. Do not introduce `MarkupString`, raw HTML,
        unbounded ProblemDetails echoing, or culture-sensitive normalization.
  - [x] Update `EventStoreResponseClassifier` so HTTP 409 can populate the typed rejection shape
        from bounded ProblemDetails extension members when present, while preserving current
        title/detail fallback behavior.
  - [x] Update `StubCommandServiceOptions` / `StubCommandService` only as needed for deterministic
        rejection tests; do not add backend status polling or retry logic here.
  - [x] If public API baselines exist for the touched package, update them intentionally and record
        the reason in the Dev Agent Record.

- [x] **Task 3 - Carry typed rejection through generated Fluxor and lifecycle UI (AC: #1-#4)**
  - [x] Add the minimal `Acknowledged` UI treatment required by AC1. Current behavior starts the
        threshold timer but renders no visible acknowledged badge/message; update tests that
        currently expect silence.
  - [x] Update generated `RejectedAction` and lifecycle state so typed rejection details survive
        from generated form catch block to reducer state.
  - [x] Preserve `Reason`/`Resolution` compatibility or provide an equivalent migration path so
        existing tests and generated snapshots fail only for intentional shape changes.
  - [x] Update generated form helpers so `FcLifecycleWrapper` receives a typed rejection detail
        parameter in addition to the existing title/body copy, or an equivalent bounded UI model.
  - [x] Update `FcLifecycleWrapper` / `LifecycleUiState` to render the typed fields in the rejected
        branch without auto-dismiss and without hiding the wrapped form.
  - [x] Preserve existing `data-fc-phase` values, ARIA role/live behavior, idempotent Info behavior,
        and Start-over wrapper-initiated navigation behavior unless a test proves a story-owned fix
        is required.

- [x] **Task 4 - Preserve lifecycle ordering and single-writer ownership (AC: #1, #3, #4, #5)**
  - [x] Preserve generated form submit order: ensure bridge/subscriber, `SubmittedAction`, command
        dispatch, pending registration, then `AcknowledgedAction` only when registration permits it.
  - [x] Keep service callbacks as the only generated path for `Syncing` / `Confirmed` and the
        `CommandRejectedException` catch as the only generated path for immediate domain rejection.
  - [x] Keep pending-command terminal resolution as the only framework path that maps accepted
        backend outcomes by `MessageId` back into lifecycle transitions.
  - [x] Do not bind the real EventStore status endpoint, choose numeric budgets, wire retry policy,
        add destructive confirmation, or add one-at-a-time command execution in this story.

- [x] **Task 5 - Add focused source-generator and Shell tests (AC: #1-#5)**
  - [x] Extend `CommandFluxorEmitterTests` for the typed `RejectedAction` shape, state fields, and
        reducer behavior, including correlation guard preservation.
  - [x] Extend `LifecycleBridgeEmitterTests` so `RejectedAction` still forwards exactly one
        `CommandLifecycleState.Rejected` transition to `ILifecycleStateService`.
  - [x] Extend `CommandFormEmitterTests` and `.verified.txt` snapshots for typed rejection catch,
        wrapper parameter forwarding, and existing retry-enabled form behavior after rejection.
  - [x] Extend `FcLifecycleWrapperRejectionTests` for typed field rendering, plain-text escaping,
        no auto-dismiss, and fallback behavior when typed fields are missing.
  - [x] Add or extend a generated-form integration test proving
        `Submitting -> Acknowledged -> Syncing -> Confirmed` and `Submitting -> Rejected` render
        through the wrapper while the form remains present/correctable.
  - [x] Re-run the Story 3.3 identity/pending focused tests so ULID-only IDs, pending registration,
        duplicate/idempotent behavior, and resolver semantics do not regress.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and require 0 warnings / 0 errors.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest is still socket-blocked with `SocketException (13): Permission denied`, use
        the established xUnit v3 in-process fallback and record CI as the authoritative
        solution-level VSTest gate.
  - [x] At minimum run focused lanes for `CommandFluxorEmitterTests`, `LifecycleBridgeEmitterTests`,
        `CommandFormEmitterTests`, `FcLifecycleWrapperTests`, `FcLifecycleWrapperRejectionTests`,
        `CounterPageLifecycleE2ETests`, `PendingCommandStateServiceTests`,
        `PendingCommandOutcomeResolverTests`, and `PendingCommandPollingCoordinatorTests`.
  - [x] Check `git diff --name-only -- '*.verified.txt'` and record whether snapshots changed.
  - [x] Keep the File List complete, including this story file, sprint status, source changes,
        tests, and generated snapshots.

## Dev Notes

### Current lifecycle UI anchors

- `FcLifecycleWrapper` already wraps generated command forms and renders child content plus lifecycle
  feedback. It currently handles `Submitting` live-region text, `Syncing` pulse/still-syncing/action
  prompt, non-idempotent `Confirmed` success, idempotent `Confirmed` Info, and `Rejected` error
  message bars. It currently does **not** render visible `Acknowledged` feedback, so AC1 requires a
  deliberate wrapper/test update. [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor]
- `FcLifecycleWrapper` subscribes to `ILifecycleStateService` by `CorrelationId`, rejects unexpected
  correlation transitions with HFC2100 logging, starts the threshold timer on `Acknowledged` /
  `Syncing`, and stops timers at terminal states. Preserve this subscription model. [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs]
- `LifecycleUiState.From(...)` is the pure mapper from `CommandLifecycleTransition` to wrapper UI
  state. Add typed rejection data there only if it remains immutable and side-effect free. [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs]
- Existing wrapper tests already pin generic phases, ARIA role/live behavior, rejected no-dismiss,
  plain-text rejection body, and correlation-id resubscribe. `Acknowledged_state_transitions_timer_phase_to_NoPulse`
  currently expects no visible acknowledged marker; update it intentionally for this story's AC1.
  Extend these tests instead of replacing them.
  [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperTests.cs]
  [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperRejectionTests.cs]

### Generated command pipeline anchors

- `CommandFormEmitter` injects `IUlidFactory`, creates `_submittedCorrelationId`, dispatches
  `SubmittedAction`, calls `CommandService.DispatchAsync(...)`, registers pending state after an
  accepted `MessageId`, and only then dispatches `AcknowledgedAction` when registration permits it.
  Do not change that order. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- The current rejection catch dispatches
  `RejectedAction(correlationId, ex.Message, ex.Resolution)`, not typed metadata. Story 3.4 owns
  carrying `errorCode`, `reasonCategory`, `suggestedAction`, and `docsCode` through this path.
  [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `CommandFluxorActionsEmitter` emits six generated actions:
  `Submitted`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, and `ResetToIdle`. `RejectedAction`
  currently has `Reason` and `Resolution` only. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs]
- `CommandFluxorFeatureEmitter` emits a state record with `State`, `CorrelationId`, `MessageId`,
  `RejectionReason`, and `RejectionResolution`; every reducer except `Submitted` is correlation
  guarded. Preserve this guard when adding typed fields. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorFeatureEmitter.cs]
- `CommandLifecycleBridgeEmitter` forwards generated actions to `ILifecycleStateService`. It should
  continue to send exactly one `Rejected` transition for a rejected action; typed rejection UI data
  belongs in generated Fluxor state/wrapper parameters, not in `ILifecycleStateService` unless the
  contract is deliberately expanded and all consumers are updated. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs]

### Rejection and pending-command anchors

- `CommandRejectedException` is the public Contracts type used by `ICommandService` implementations
  for domain rejection. It currently exposes `Exception.Message` plus `Resolution` only. Extend
  carefully because this is public API. [Source: src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs]
- `ProblemDetailsPayload` is bounded plain-text data from RFC 7807 responses. It currently contains
  `Title`, `Detail`, `Status`, `EntityLabel`, `ValidationErrors`, and `GlobalErrors`, but no
  extension-member bag. If typed rejection fields are read from ProblemDetails extensions, keep the
  same byte/count/string bounds used by `EventStoreResponseClassifier`. [Source: src/Hexalith.FrontComposer.Contracts/Communication/ProblemDetailsPayload.cs]
- `EventStoreResponseClassifier` maps HTTP 409 Conflict to `CommandRejectedException` using
  ProblemDetails title/detail. Story 3.4 may extend this mapping for typed rejection details, but
  must not change 400 validation, 401 redirect, 403/404/429 warning, or default HTTP failure
  classification. [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs]
- Pending-command terminal observations already carry `RejectionTitle`, `RejectionDetail`, and
  `RejectionDataImpact`; `FcPendingCommandSummary` formats those for after-the-fact summaries.
  This is not the same as the generated form's immediate typed rejection UI. Reuse naming only if it
  truly fits; do not conflate summary data with immediate form rejection metadata. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs]
  [Source: src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs]

### Previous story intelligence

- Story 3.3 decided the FC-CMD v1 contract: `CorrelationId` is the generated form/lifecycle
  subscription key; `MessageId` is the accepted command and pending-command identity key; both are
  canonical 26-character Crockford ULIDs via `IUlidFactory`; pending state is circuit-local and
  fail-closes on tenant/user transition. [Source: _bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md]
- Story 3.3 removed the `UlidFactory` GUID fallback, validated both pending `MessageId` and
  `CorrelationId`, and preserved idempotent/already-applied semantics. Do not reintroduce GUIDs,
  raw payload storage, tenant/user storage, or ambiguous reconciliation. [Source: _bmad-output/implementation-artifacts/3-3-confirm-the-fc-cmd-pending-identity-and-correlation-contract.md]
- Recent commits are story-scoped: `cbe4d42` (Story 3.3 identity contract), `c6ff094` (Story 3.2
  density rule), and `8cfcc80` (Story 3.1 command form generation). Expect snapshot churn in
  generated command form/Fluxor outputs if the typed rejection shape changes; explain every
  `.verified.txt` change.

### Architecture and constraints

- SourceTools remains a pure Roslyn incremental generator: parse -> pure IR -> transform -> emit.
  Do not let `ISymbol` escape parse-stage models, and do not add Shell-only dependencies beyond the
  already-established generated reference surface. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Generated code is not hand-edited. Change emitters/transforms/tests, then regenerate or update
  Verify snapshots intentionally. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Blazor/Fluxor code must follow ADR-007 single-writer discipline: each action type has one dispatch
  source; effects/services own async work; reducers stay pure. [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- Use repo-pinned dependencies only. Relevant pins include .NET SDK 10.0.300, FluentUI Blazor
  `5.0.0-rc.3-26138.1`, Fluxor 6.9.0, NUlid 1.7.3, xUnit v3 3.2.2, Shouldly 4.3.0, bUnit 2.7.2,
  and Verify.XunitV3 31.19.0. No package upgrades are needed for this story. [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- Public API changes in `Contracts` must respect package boundaries and owned baseline tests.
  Update baselines deliberately when adding public rejection detail types/properties. [Source: _bmad-output/project-context.md#Testing Rules]
- Keep localization and accessibility behavior honest. Existing lifecycle strings are partly inline
  and partly resource-backed; if this story adds visible strings, add/update resources where the
  surrounding component already uses `FcShellResources`, and test English fallback behavior. [Source: src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx]

### Out of scope

- Do not bind `GET /api/v1/commands/status/{id}`; Story 3.5 owns that.
- Do not choose confirming/degraded/polling timing budgets; Story 3.6 owns those.
- Do not add destructive confirmation, unsaved-form abandonment policy, one-at-a-time execution,
  authorization policy gates, retries, or degraded retry handling; Epic 4 owns those.
- Do not add row-level `FcNewItemIndicator` producer wiring; the Story 2.6 PO-accepted deferral
  keeps that work tied to later command outcome producer identity.
- Do not modify submodules or published `docs/` as scratch space.

### Project Structure Notes

- Expected source touch points are under:
  `src/Hexalith.FrontComposer.Contracts/Communication/`,
  `src/Hexalith.FrontComposer.SourceTools/Emitters/`,
  `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/`,
  `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/`, and focused Shell/SourceTools
  tests under matching `tests/` folders.
- If public API baselines are updated, include the baseline files in the File List and state which
  public members were intentionally added.
- Keep generated output snapshots in `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*.verified.txt`.
  Do not edit `obj/**/generated/**`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4: Command lifecycle UI]
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- [Source: _bmad-output/project-context.md#Source-Generator Rules]
- [Source: _bmad-output/project-docs/architecture.md#4. Runtime composition (Shell)]
- [Source: _bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Re-audited the live lifecycle UI path before editing: generated form submit/catch/render helpers, generated Fluxor actions/state/reducers, lifecycle bridge, wrapper/UI mapper, Contracts rejection payload types, EventStore 409 classifier, and Story 3.3 FC-CMD identity/correlation contract.
- 2026-06-04: Exact `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` is locally blocked by MSBuild/VSTest named-pipe socket creation (`SocketException (13): Permission denied`). Used xUnit v3 in-process runner fallback for local evidence; CI remains the authoritative solution-level VSTest gate.
- 2026-06-04: Exact `dotnet build Hexalith.FrontComposer.slnx -c Release` failed once without diagnostic detail in this sandbox; `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed 0 warnings / 0 errors.
- 2026-06-04: Public API baseline note: no owned `Contracts` public API baseline exists in this repo. The public API change is additive (`CommandRejectionDetails`, `CommandRejectedException.Details/ErrorCode/ReasonCategory/SuggestedAction/DocsCode`, `ProblemDetailsPayload.RejectionDetails`) and is pinned by Contracts tests.
- 2026-06-04: Intentional Verify snapshot changes: two `CommandFormEmitterTests.*.verified.txt` files now show typed rejection action dispatch, `FcLifecycleWrapper.RejectionDetails` forwarding, and generated `BuildFcLifecycleRejectionDetails()`.
- 2026-06-04: Broad in-process default lanes reproduced existing non-story baselines: SourceTools 989 total / 4 failed (IDE path normalization, datagrid docs section, deferred-work ledger missing, deterministic shuffle fluke); Shell 1774 total / 9 failed (deferred-work governance x4, Pact mock server socket, navigation hydration, two projection snapshot locale/format diffs, full-page query fallback). Focused story lanes are green.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-06-04.
- Added typed command rejection metadata via `CommandRejectionDetails` and additive `CommandRejectedException`/`ProblemDetailsPayload` members while preserving the existing `(reason, resolution)` constructor and title/detail fallback behavior.
- Bounded HTTP 409 ProblemDetails extension parsing for `errorCode`, `reasonCategory`, `suggestedAction`, and `docsCode`; missing values fall back to safe plain-text strings.
- Carried typed rejection details through generated `RejectedAction`, generated lifecycle state, generated form wrapper parameters, and `FcLifecycleWrapper` rejected UI without expanding `ILifecycleStateService`.
- Added visible `Acknowledged` lifecycle UI through live-region announcement, badge, and non-dismissible info message bar while preserving existing rejected/confirmed/idempotent/start-over behavior.
- Preserved lifecycle ordering and single-writer ownership: bridge/subscriber activation precedes `SubmittedAction`; pending registration still gates `AcknowledgedAction`; callbacks remain the generated `Syncing`/`Confirmed` path; `CommandRejectedException` remains the immediate rejection path.
- Added focused Contracts, SourceTools, Shell wrapper/classifier/stub, generated-form integration, and pending-command regression tests. Story 3.3 pending/identity semantics remain green in the focused lane.
- Generated forms now emit a stable `CommandId` wrapper attribute derived from the command type name via `CommandFormEmitter.BuildLifecycleCommandId` (e.g. `BatchIncrementCommand` → `batch-increment`, culture-invariant), so `FcLifecycleWrapper` renders `data-testid="fc-lifecycle-<command-id>"`. This enables the rewritten Playwright lifecycle spec and the in-process integration test selectors; the kebab conversion is deterministic at generation time.

### File List

- _bmad-output/implementation-artifacts/3-4-command-lifecycle-ui.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs
- src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectionDetails.cs
- src/Hexalith.FrontComposer.Contracts/Communication/ProblemDetailsPayload.cs
- src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor
- src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs
- src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs
- src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs
- src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs
- src/Hexalith.FrontComposer.Shell/Services/StubCommandServiceOptions.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorActionsEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorFeatureEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs
- tests/Hexalith.FrontComposer.Contracts.Tests/Communication/Story52ResponseSurfaceTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperRejectionTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleUiStateTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleWrapperTestBase.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterPageLifecycleE2ETests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreResponseClassifierTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Services/StubCommandServiceTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFluxorEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/LifecycleBridgeEmitterTests.cs
- tests/e2e/fixtures/lifecycle.fixture.ts
- tests/e2e/specs/lifecycle.spec.ts

### Change Log

- 2026-06-04: Implemented typed rejection metadata and acknowledged lifecycle UI for Story 3.4; added focused generator, Shell, Contracts, and pending-regression coverage; updated two intentional generated-form snapshots.
- 2026-06-04: Story-automator adversarial review — re-verified all 5 ACs against live source; build 0/0 and focused lanes green (Contracts 10/10, SourceTools 46/46, Shell story/pending 98/98, full SourceTools 985/989 with only the 4 disclosed pre-existing baseline failures). 0 Critical / 0 High; 1 Medium + 1 Low documentation gap auto-fixed (e2e files added to File List; generated CommandId behavior documented). Status → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-04 · **Outcome:** Approve

**Scope verified:** `git status`/`git diff` cross-referenced against the File List; every changed source/test file read; Release build run; focused + full SourceTools test lanes executed; generated-output snapshots and e2e selectors traced to real artifacts.

**Acceptance Criteria — all met:**
- AC1 (every phase surfaced + form stays present): `FcLifecycleWrapper` now renders Acknowledged via live region + badge + non-dismissible Info bar; `CommandRendererWrapperIntegrationTests.GeneratedForm_SubmitAccepted_*` proves Submitting → Acknowledged → Syncing(pulse) → Confirmed with the form present throughout. ✔
- AC2 (typed rejection displayed + safe): fields rendered via Razor auto-encoding (`Rejection_typed_fields_render_as_plain_text` asserts `&lt;E409&gt;` and no `<E409>`); classifier bounds every extension string to 512 chars (`Command_409Conflict_BoundsTypedRejectionDetailStrings`); `GeneratedForm_SubmitRejected_*` proves field values (`existing name`, `value="7"`) survive the rejection. No `MarkupString`, no raw-payload logging. ✔
- AC3 (correlation isolation): reducers keep the `state.CorrelationId != action.CorrelationId` guard; new typed fields are only written inside the guarded branch and reset on `Submitted`/`ResetToIdle`. ✔
- AC4 (one dispatch source per transition): `RejectedAction` dispatched only from the generated catch block; the lifecycle bridge still forwards exactly one `Rejected` transition (`Emit_RejectedAction_ForwardsExactlyOneRejectedTransitionToLifecycleService`). Reducers remain pure. ✔
- AC5 (no 3.1–3.3 regression): pending/identity focused tests green; ULID-only and FC-CMD contract untouched; the only Verify-snapshot changes are the two intentional generated-form snapshots; full SourceTools failures are exactly the four disclosed pre-existing/environmental baselines (IDE path normalization, datagrid docs section, deferred-work ledger missing, Fisher-Yates shuffle fluke). ✔

**Findings (auto-fixed per non-interactive review):**
- [MEDIUM] File List omitted `tests/e2e/fixtures/lifecycle.fixture.ts` and `tests/e2e/specs/lifecycle.spec.ts`, both modified for this story (6-state lifecycle set; `fc-acknowledged`/`fc-confirmed` assertions against the `batch-increment` form). → Added to File List.
- [LOW] The new generated `CommandId` wrapper attribute / `data-testid="fc-lifecycle-<id>"` behavior was not described in the Dev Agent Record. → Documented in Completion Notes.

**Observation (no change, defensible as-is):** the Acknowledged screen-reader announcement is the single word "Acknowledged" (matches the badge) while terminal states use fuller phrasing ("Submission confirmed/rejected"); left intact as a deliberate badge-aligned choice.

**Verification commands:** `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` (0/0); `DiffEngine_Disabled=true dotnet test … --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` per project (focused lanes 100% green; full SourceTools 985/989 = 4 disclosed baselines).
