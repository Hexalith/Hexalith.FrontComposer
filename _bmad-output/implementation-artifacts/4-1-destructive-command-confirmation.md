---
baseline_commit: a74d5322f0d44aeac110d2c491ced1018ff136e4
---

# Story 4.1: Destructive-command confirmation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. Destructive-command support is already partly implemented:
> `[Destructive]` exists in Contracts, `CommandParser` already captures `ConfirmationTitle` /
> `ConfirmationBody`, HFC1020/HFC1021 descriptors and parser tests exist, the shell has
> `FcDestructiveConfirmationDialog`, and `CommandRendererEmitter` already routes destructive generated
> renderers through `DestructiveBeforeSubmitAsync`. Treat this as a confirm-and-pin / close-gaps story.
> Do not rebuild command lifecycle, FC-CMD identity, EventStore status polling, retry policy,
> abandonment guard, or one-at-a-time execution.

## Story

As an operator,
I want destructive commands to require explicit confirmation,
so that I can't trigger irreversible actions by accident.

## Acceptance Criteria

1. **Destructive generated commands require confirmation before dispatch.**  
   Given a `[Destructive]` command with one or more non-derivable properties,  
   When the generated renderer/form is submitted,  
   Then `FcDestructiveConfirmationDialog` opens with the configured title/body, cancel is the safe
   default path, and command dispatch/lifecycle submission does not start until the operator confirms.

2. **Confirmation copy and safety behavior are deterministic.**  
   Given `[Destructive(ConfirmationTitle = "...", ConfirmationBody = "...")]`,  
   When the generated renderer opens the dialog,  
   Then the configured copy is passed as plain text and the destructive button label uses the command
   display label.  
   Given no override copy, the title falls back to `{DisplayLabel}?` and the body falls back to
   `This action cannot be undone.`.

3. **Cancel, validation, authorization, and rapid clicks cannot bypass the gate.**  
   Given the dialog is canceled or Escape is pressed,  
   When the submit path resumes,  
   Then no command dispatch occurs and lifecycle state returns/remains safe for another attempt.  
   Given validation fails, the dialog must not open.  
   Given `[RequiresPolicy]` is also present, authorization still runs before `BeforeSubmit` and again
   after the dialog returns.  
   Given rapid submit clicks, at most one destructive dialog/dispatch path may be active.

4. **Destructive diagnostics remain pinned.**  
   Given a destructive-verb-named command without `[Destructive]`,  
   When built,  
   Then HFC1020 (Info) advises adding `[Destructive]`.  
   Given a `[Destructive]` command with zero non-derivable properties,  
   When built,  
   Then HFC1021 (Error) is emitted and the command model/generation path fails closed.

5. **Scope stays inside destructive confirmation.**  
   Given this story completes,  
   When reviewing the diff,  
   Then there is no new retry budget, abandonment guard behavior, FC-CNC one-at-a-time policy,
   EventStore status endpoint change, or command identity/correlation change unless a live source gap
   proves it is strictly necessary and the story notes the reason.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit existing destructive command pipeline before editing (AC: #1-#5)**
  - [x] Read `src/Hexalith.FrontComposer.Contracts/Attributes/DestructiveAttribute.cs`; preserve the
        opt-in marker semantics and optional copy properties.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`; confirm
        `[Destructive]`, HFC1020 name heuristics, HFC1021 zero-field failure, derivable-field
        exclusion, and pure-IR invariants are intact.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` and
        `CommandRendererTransform.cs`; preserve threading of destructive metadata from parser to
        renderer model.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`; identify
        exactly where `IDialogService`, `_dialogOpen`, `DestructiveBeforeSubmitAsync`, and
        `BeforeSubmit` wiring are emitted.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`; preserve
        validation-before-`BeforeSubmit`, pre/post authorization checks, lifecycle bridge/last-used
        registration order, ULID correlation allocation, and rejected/canceled reset behavior.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.*`;
        preserve cancel autofocus, Escape cancel, plain-text copy, and danger-button CSS hook.

- [x] **Task 2 - Add or tighten generated-output pins for AC1-AC3 (AC: #1-#3)**
  - [x] Add focused SourceTools emitter/integration tests proving a `[Destructive]` command renderer
        emits `IDialogService`, `DestructiveBeforeSubmitAsync`, `FcDestructiveConfirmationDialog`
        parameters, `_dialogOpen`, and `BeforeSubmit = DestructiveBeforeSubmitAsync`.
  - [x] Pin configured confirmation title/body and fallback title/body in emitted source.
  - [x] Pin that non-destructive generated renderers continue to use
        `RefreshDerivedValuesBeforeSubmitAsync` directly and do not inject `IDialogService`.
  - [x] Pin that generated destructive renderer output remains valid C# and does not introduce
        `Guid.*`, user-supplied `MessageId`/`CorrelationId`, or lifecycle dispatch before the form
        submit path.

- [x] **Task 3 - Add runtime behavior pins only where coverage is missing (AC: #1-#3)**
  - [x] Extend `FcDestructiveConfirmationDialogTests` only if needed for missing plain-text,
        autofocus/Escape, callback, or class assertions.
  - [x] Add a generated renderer/form bUnit or integration test using the existing Shell test host
        patterns to prove cancel/Escape prevents `ICommandService.DispatchAsync` and confirm allows
        exactly one dispatch.
  - [x] If the existing test harness cannot drive the Fluent dialog portal directly, use the local
        `RecordingDialogService` pattern or a focused fake `IDialogService`, but keep the assertion on
        the generated renderer/form path rather than only the standalone dialog component.
  - [x] Add a rapid-submit/double-click pin if `_dialogOpen` is not already covered end-to-end.
  - [x] If `[RequiresPolicy]` + `[Destructive]` composition is not pinned, add a generated-source or
        runtime test proving the post-dialog authorization re-check remains present.

- [x] **Task 4 - Preserve and expand diagnostics pins for AC4 only if needed (AC: #4)**
  - [x] Keep HFC1020 severity as `Info` unless a separate story/ADR owns promotion.
  - [x] Keep HFC1021 severity as `Error` and generation fail-closed for zero non-derivable fields.
  - [x] Ensure destructive verb coverage includes the current parser regex behavior
        (`Delete`, `Remove`, `Purge`, `Wipe`, `Erase`, `Drop`, `Truncate` as implemented).
  - [x] Do not add a suppression attribute or new diagnostic ID in this story.

- [x] **Task 5 - Keep Epic 4 boundaries explicit (AC: #5)**
  - [x] Do not implement Story 4.2 `FcFormAbandonmentGuard` behavior except preserving existing
        generated renderer hooks.
  - [x] Do not implement Story 4.3 FC-CNC queue/block policy.
  - [x] Do not implement Story 4.4 `[RequiresPolicy]` changes except preserving existing composition
        if a destructive command also has a policy.
  - [x] Do not implement Story 4.5 retry/degraded retry behavior or change Epic 3 polling budgets.
  - [x] Do not change FC-CMD identity/correlation: `MessageId` and `CorrelationId` remain
        26-character Crockford ULIDs generated by infrastructure abstractions.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run focused SourceTools tests for destructive parsing, command renderer emitter/integration,
        and any affected generated snapshots.
  - [x] Run focused Shell tests for `FcDestructiveConfirmationDialog` and the generated
        confirm/cancel dispatch path.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` when the environment permits. If local VSTest/MSBuild sockets are blocked, use the established in-process fallback and state CI remains authoritative.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; any snapshot change must be intentional and
        listed.
  - [x] Keep the File List complete, including story file, sprint status, source, tests, and any
        intentional snapshots.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- No separate PRD or UX artifact was found under `_bmad-output/planning-artifacts/`; architecture
  fallback came from `_bmad-output/project-context.md` and `_bmad-output/project-docs/*`.
- Story 4.1 is the first Epic 4 story; this create-story run moved
  `_bmad-output/implementation-artifacts/sprint-status.yaml` `epic-4` from `backlog` to
  `in-progress` and `4-1-destructive-command-confirmation` from `backlog` to `ready-for-dev`.
- There is no previous Story 4.x file. Prior intelligence comes from Epic 3 completion and
  `_bmad-output/implementation-artifacts/epic-3-retro-2026-06-04.md`.
- External web research was not required: implementation uses repo-pinned .NET 10, FluentUI v5 RC,
  Fluxor, xUnit v3, bUnit, and local source contracts already recorded in project artifacts.

### Epic and Story Context

- Epic 4 adds safe command execution on top of Epic 3: destructive confirmation, abandonment guard,
  one-at-a-time execution, policy authorization, and retry/degraded handling. [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Safe & Concurrent Command Execution]
- Story 4.1 specifically requires `FcDestructiveConfirmationDialog` before dispatch for
  `[Destructive]` commands, plus HFC1020/HFC1021 diagnostics. [Source: _bmad-output/planning-artifacts/epics.md#Story 4.1: Destructive-command confirmation]
- FR12 names destructive-command confirmation as part of command lifecycle UX, while FR6 governs
  the HFC diagnostic catalog. [Source: _bmad-output/planning-artifacts/epics.md#Functional Requirements]
- AR10 explicitly keeps rich `<AuditTimeline>` / `<ConsequencePreview>` components out of v1; do not
  scope consequence-preview UI into this story. [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements]

### Current Source State to Preserve

- `DestructiveAttribute` is already public in Contracts with `ConfirmationTitle` and
  `ConfirmationBody` init properties; it is an opt-in marker and name heuristics are only advisory.
  [Source: src/Hexalith.FrontComposer.Contracts/Attributes/DestructiveAttribute.cs]
- `CommandParser` already:
  - Parses `[Destructive]` and optional copy into `CommandModel`.
  - Emits HFC1020 Info for destructive verb names without `[Destructive]`.
  - Emits HFC1021 Error and returns no model for `[Destructive]` commands with zero non-derivable
    properties.
  - Excludes derivable fields such as `MessageId`, `CommandId`, `CorrelationId`, `TenantId`,
    `UserId`, timestamps, and `[DerivedFrom]` from command form fields.  
  [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs]
- `CommandRendererEmitter` already emits `IDialogService` and `_dialogOpen` only for destructive
  commands, emits `DestructiveBeforeSubmitAsync`, passes title/body/label into
  `FcDestructiveConfirmationDialog`, and wires destructive renderers' `BeforeSubmit` to that method.
  [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- `CommandFormEmitter` runs the generated submit sequence: cancel previous CTS, pre-authorization,
  clear server validation, `BeforeSubmit`, post-authorization, `UlidFactory.NewUlid()` correlation,
  lifecycle bridge/last-used ensure, `SubmittedAction`, command dispatch, and terminal lifecycle
  actions. Do not move dispatch before `BeforeSubmit`. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `FcDestructiveConfirmationDialog` renders title/body as Blazor text, focuses Cancel, maps Escape to
  cancel, closes/cancels the `IDialogInstance` in `finally`, and uses
  `data-testid="fc-destructive-dialog"`, `fc-destructive-cancel`, and `fc-destructive-confirm`.
  [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor]
  [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs]
- Existing test coverage already includes parser-level destructive classification/HFC1020/HFC1021 and
  standalone dialog rendering/cancel/confirm/Escape/danger-class behavior. The likely missing coverage
  is generated renderer/form integration proving the dialog gate actually prevents/allows dispatch.
  [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserDestructiveTests.cs]
  [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcDestructiveConfirmationDialogTests.cs]

### Previous Story and Git Intelligence

- Epic 3 finalized command lifecycle, EventStore status binding, and command polling budgets. Epic 4
  must not reinterpret Epic 3 polling expiry as retry policy; automatic client retry remained `0`
  until Epic 4 explicitly owns retry. [Source: _bmad-output/implementation-artifacts/3-6-apply-confirming-degraded-and-polling-budgets.md]
- Epic 3 retrospective says to build destructive, abandonment, authorization, and retry surfaces on
  the existing lifecycle states, preserve `MessageId`-first resolution, and verify destructive
  confirmation composes with `FcAuthorizedCommandRegion` and generated forms. [Source: _bmad-output/implementation-artifacts/epic-3-retro-2026-06-04.md#Next Epic Preparation - Epic 4]
- Recent commits establish the expected pattern: record story context, make narrowly scoped source
  changes, add focused tests, preserve known baseline failures honestly, and keep File List
  reconciliation complete. Relevant recent commits: `feat(story-3.6)`, `feat(story-3.5)`,
  `feat(story-3.4)`.

### Project Structure Notes

- SourceTools changes belong under:
  - `src/Hexalith.FrontComposer.SourceTools/Parsing/`
  - `src/Hexalith.FrontComposer.SourceTools/Transforms/`
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/`
- Shell runtime/component changes belong under:
  - `src/Hexalith.FrontComposer.Shell/Components/Forms/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/`
- Do not edit generated output under `obj/**/generated/HexalithFrontComposer/`; change the generator
  or tests. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Do not write scratch docs into `docs/`; story notes and evidence belong under `_bmad-output/`.
  [Source: _bmad-output/project-context.md#Development Workflow Rules]

### Technical Guardrails

- Keep Contracts and SourceTools netstandard2.0-clean; SourceTools must reference only Contracts and
  must not depend on Shell. Generated code may reference Shell components. [Source: _bmad-output/project-docs/architecture.md#Layered structure]
- Keep IR pure and fully equatable; if destructive metadata changes, update equality/hash code on
  `CommandModel` / `CommandRendererModel` instead of threading ad hoc state. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Use existing FluentUI v5 RC components and existing `FcDestructiveConfirmationDialog`; do not add
  another modal/dialog library. [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- Blazor component event handlers may need to remain on the component sync context. Follow local
  component patterns and existing CA2007 suppressions where already justified; do not blindly add
  `ConfigureAwait(false)` inside UI event handlers. [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs]
- Continue using `IUlidFactory` for correlation allocation and never add GUID fallbacks.
  [Source: _bmad-output/project-context.md#Critical Implementation Rules]

### Testing Requirements

- Use xUnit v3, Shouldly, NSubstitute, bUnit, and Verify.XunitV3 where applicable; do not use raw
  `Assert.*`. [Source: _bmad-output/project-context.md#Testing Rules]
- Run tests with `DiffEngine_Disabled=true` for Verify snapshot safety. [Source: _bmad-output/project-docs/development-guide.md#Test]
- Preferred focused lanes:
  - `CommandParserDestructiveTests`
  - new/updated command renderer emitter or generator integration tests
  - `FcDestructiveConfirmationDialogTests`
  - new generated renderer/form dispatch-gating tests
  - existing Story 3.4 lifecycle wrapper/rejection tests if submit path code changes
- Any `.verified.txt` change must be intentional, reviewed, and listed in the Dev Agent Record.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.1: Destructive-command confirmation]
- [Source: _bmad-output/project-context.md#Critical Implementation Rules]
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition (Shell)]
- [Source: _bmad-output/project-docs/api-contracts.md#Source-generator contract (`FrontComposerGenerator`)]
- [Source: _bmad-output/implementation-artifacts/epic-3-retro-2026-06-04.md#Next Epic Preparation - Epic 4]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserDestructiveTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Audited destructive pipeline files named in Task 1 before editing. Existing parser,
  transform, form submit order, dialog component safety behavior, and Epic 4 scope boundaries were
  preserved.
- 2026-06-04: Found and fixed live generated-code gap: destructive generated renderers treated
  FluentUI v5 RC `ShowDialogAsync<TDialog>(Action<DialogOptions>)` as a dialog-reference-returning
  API. The generator now awaits the returned `DialogResult` directly, and emitted-source tests reject
  the stale `dialogRef.Result` pattern.
- 2026-06-04: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`
  passed with 0 warnings and 0 errors.
- 2026-06-04: Validation: SourceTools focused in-process lane passed 55/55
  (`CommandRendererEmitterTests`, `CommandFormEmitterTests`, `CommandParserDestructiveTests`).
- 2026-06-04: Validation: Shell focused in-process lane passed 9/9
  (`FcDestructiveConfirmationDialogTests`, `DestructiveCommandRendererIntegrationTests`).
- 2026-06-04: Validation: exact filtered solution `dotnet test --no-build` command failed before
  test execution with `System.Net.Sockets.SocketException (13): Permission denied` from MSBuild
  named-pipe setup; CI remains authoritative for the solution-level VSTest gate.
- 2026-06-04: Snapshot check: `git diff --name-only -- '*.verified.txt'` returned no files.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Destructive generated renderers now compile against the actual FluentUI v5 RC dialog API and await
  `DialogResult` directly before deciding whether to cancel.
- Added SourceTools pins for destructive renderer dialog injection/wiring, configured and fallback
  copy, non-destructive renderer behavior, no command dispatch/identity allocation in renderer
  output, post-dialog authorization re-check ordering, and the full destructive verb HFC1020 set.
- Added Shell generated renderer/form integration pins proving canceled destructive confirmation
  prevents command dispatch, confirmed confirmation allows one dispatch, and rapid submits open only
  one dialog and dispatch once.
- Existing standalone `FcDestructiveConfirmationDialogTests` already covered plain text rendering,
  cancel autofocus, Escape cancel, callbacks, and danger-button CSS; no component change was needed.
- Epic 4 boundaries were preserved: no abandonment guard behavior, FC-CNC policy, retry/degraded
  budget, EventStore status endpoint, or FC-CMD identity/correlation changes were introduced.
- Added specimen-only `PurgeSpecimenRecordCommand` plus a destructive section in
  `FrontComposerTypeSpecimen.razor`, and a Playwright e2e spec
  (`tests/e2e/specs/destructive-command-confirmation.spec.ts`) covering cancel/Escape, validation
  blocking the dialog, rapid-click single-dialog, and confirm→dispatch. e2e run is blocked in the
  sandbox (Chromium/loopback restrictions) and remains CI-authoritative; the .NET bUnit pins are the
  runnable local evidence. Details in `tests/4-1-test-summary.md`.

### File List

- `_bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/4-1-test-summary.md`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserDestructiveTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/DestructiveCommandRendererIntegrationTests.cs`
- `tests/e2e/specs/destructive-command-confirmation.spec.ts`
- `samples/Counter/Counter.Specimens.Domain/PurgeSpecimenRecordCommand.cs`
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor`

### Change Log

- 2026-06-04: Fixed destructive renderer dialog API usage for FluentUI v5 RC and added generated
  source/runtime pins for destructive confirmation gating, rapid-submit protection, auth composition,
  copy fallback, and HFC1020 verb coverage.
- 2026-06-04: Automated code review (story-automator). Verified all 5 ACs against implementation;
  Release build clean (0/0); SourceTools 55/55 and Shell destructive 9/9 passed. Auto-fixed File List
  (added e2e spec, specimen command, specimen razor, test summary) and recorded the e2e/specimen work
  in Completion Notes. Status review → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-04 · **Outcome:** Approve (status → done)

**Scope reviewed:** Story File List + git-discovered changes for Epic 4 Story 4.1.

**Verification performed:**
- Confirmed the core fix is correct: FluentUI v5 RC `IDialogService.ShowDialogAsync<TDialog>(Action<DialogOptions>)`
  returns `Task<DialogResult>` directly (checked via Fluent UI Blazor MCP docs); the prior
  `await dialogRef.Result` pattern was invalid and is removed.
- AC1–AC5 cross-checked against `CommandRendererEmitter` (`DestructiveBeforeSubmitAsync`, `_dialogOpen`
  guard, dialog parameter wiring, fallback copy), `CommandFormEmitter` (pre/post-`BeforeSubmit`
  authorization re-check ordering), `CommandParser` (HFC1020 Info verb set + HFC1021 Error fail-closed),
  and the Shell integration tests (cancel→no dispatch, confirm→one dispatch, rapid→one dialog).
- `dotnet build Hexalith.FrontComposer.slnx -c Release` → 0 warnings / 0 errors.
- `DiffEngine_Disabled=true dotnet test` (focused, per-project, `--no-build`): SourceTools 55/55,
  Shell destructive (`DestructiveCommandRendererIntegrationTests` + `FcDestructiveConfirmationDialogTests`)
  9/9.

**Findings:** 0 Critical, 0 High, 2 Medium (fixed), 1 Low (follow-up).
- [Medium][Fixed] File List omitted 4 story artifacts (e2e spec, specimen command/razor, test summary) — added.
- [Medium][Fixed] Completion Notes/Change Log did not record the e2e + specimen work — added.
- [Low][Follow-up] AC3 "validation failure must not open the dialog" has no in-process bUnit pin; it is
  covered by the (sandbox-blocked, CI-authoritative) e2e spec and by form-level `OnValidSubmit` gating.
  A dedicated required-field renderer fixture would be needed to pin it in bUnit without breaking the
  existing shared `DeleteWidgetCommand` integration tests.

### Review Follow-ups (AI)

- [ ] [AI-Review][Low] Add a dedicated bUnit pin proving a required-field validation failure does not
      open the destructive dialog, using a separate fixture command so the existing
      `DestructiveCommandRendererIntegrationTests` remain unaffected
      [tests/Hexalith.FrontComposer.Shell.Tests/Generated/DestructiveCommandRendererIntegrationTests.cs].
