---
baseline_commit: 3efea6d12e5774a572bc8329301949553ed2faae
---

# Story 4.2: Unsaved-form abandonment guard

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. Unsaved-form abandonment support is already partly
> implemented: `FcFormAbandonmentGuard` exists in Shell, uses `NavigationLock` and
> `FluentMessageBar`, `FcShellOptions.FormAbandonmentThresholdSeconds` and the threshold validator
> exist, generated command forms expose `OnEditContextReady`, and `CommandRendererEmitter` already
> wraps full-page command forms in `FcFormAbandonmentGuard`. Story 4.2 v1 scope is generated
> `CommandRenderMode.FullPage` command forms, because the guard is page-navigation protection and the
> current generator only mounts it on full-page renderers. Treat this as a confirm-and-pin /
> close-gaps story. Do not rebuild command lifecycle, destructive confirmation, FC-CNC one-at-a-time
> execution, policy authorization, retry/degraded handling, EventStore status polling, FC-CMD
> identity/correlation, or Inline/CompactInline abandonment UX.

## Story

As an operator,
I want to be warned before navigating away from an unsaved command form,
so that I don't lose in-progress input.

## Acceptance Criteria

1. **Dirty generated full-page command forms are protected before internal navigation.** *(UX-DR4)*  
   Given a generated `CommandRenderMode.FullPage` command form has received at least one user edit,  
   When the operator attempts internal navigation after the configured abandonment threshold,  
   Then `FcFormAbandonmentGuard` intercepts the `NavigationLock` callback, prevents navigation, and
   renders a warning `FluentMessageBar` with clear stay/leave actions.

2. **Clean full-page forms do not interrupt navigation.**  
   Given a generated full-page command form has not received a field edit, has no `EditContext`, or
   is still below `FcShellOptions.FormAbandonmentThresholdSeconds`,  
   When the operator navigates away,  
   Then no warning is shown and the guard does not block navigation.

3. **Stay, leave, Escape, and lifecycle suppression are deterministic.**  
   Given the warning is visible,  
   When the operator chooses "Stay on form" or presses Escape on the warning action area,  
   Then navigation remains canceled, in-progress input remains present, and the warning clears.  
   Given the operator chooses "Leave anyway",  
   When navigation is retried,  
   Then the guard allows that single pending navigation and clears its bypass flag so later unrelated
   navigation attempts are not accidentally allowed.  
   Given `FcLifecycleWrapper` initiated navigation or the command lifecycle is `Submitting`,  
   When navigation is attempted,  
   Then the guard yields without prompting; `Syncing`, `Rejected`, `NeedsReview`, and ordinary dirty
   states must not be blanket-suppressed.

4. **Generated renderer integration is pinned, not only component internals.**  
   Given a generated full-page command renderer,  
   When built or rendered in tests,  
   Then the renderer passes the generated form `EditContext` to `FcFormAbandonmentGuard`, passes the
   current lifecycle correlation id, keeps `BeforeSubmit` ordering intact, and proves dirty vs clean
   navigation through the generated renderer/form path.  
   Inline and CompactInline command renderers remain out of scope for Story 4.2; do not add a new
   popover/card abandonment UX in this story.

5. **Scope stays inside abandonment protection.**  
   Given this story completes,  
   When reviewing the diff,  
   Then there is no new destructive-confirmation behavior, FC-CNC queue/block policy,
   `[RequiresPolicy]` authorization behavior, retry budget/degraded retry behavior, EventStore
   status endpoint change, or command identity/correlation change unless a live source gap proves it
   is strictly necessary and the story notes the reason.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the existing abandonment pipeline before editing (AC: #1-#5)**
  - [x] Read `src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor` and
        `.razor.cs`; preserve `NavigationLock`, warning `FluentMessageBar`, first-edit anchoring,
        threshold check, lifecycle suppression, leave/stay behavior, and disposal cleanup.
  - [x] Read `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` and
        `src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs`; preserve the
        seconds-based `FormAbandonmentThresholdSeconds` public option and the invariant that it
        exceeds `StillSyncingThresholdMs`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`; preserve
        `EditContext` construction, `OnEditContextReady`, `IsDirty`, field-change handling,
        validation-before-`BeforeSubmit`, and submit/lifecycle ordering.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`; identify
        exactly where `_formEditContext`, `OnFormEditContextReady`, full-page
        `FcFormAbandonmentGuard`, `CorrelationId`, `EditContext`, `BeforeSubmit`, and
        `OnEditContextReady` are emitted.
  - [x] Read Story 4.1's follow-up before touching destructive paths; only address the validation
        dialog follow-up if this story changes destructive generated form submit behavior.

- [x] **Task 2 - Close component-level behavior gaps in `FcFormAbandonmentGuard` only where needed (AC: #1-#3)**
  - [x] Add or tighten bUnit tests proving dirty + threshold-crossed navigation calls
        `PreventNavigation()` and renders `data-testid="fc-form-abandonment-warning"`.
  - [x] Add or tighten tests proving clean/no-edit, null `EditContext`, and below-threshold navigation
        do not show the warning or prevent navigation.
  - [x] Pin "Stay on form", Escape, and "Leave anyway" behavior, including that the leave bypass is
        consumed by the next navigation event and cleared after use.
  - [x] Pin lifecycle suppression precisely: `Submitting` and wrapper-initiated navigation yield;
        `Syncing` and other non-submitting states still protect dirty forms.
  - [x] If the current reflection-based `LocationChangingContext` helper is retained, keep it local
        to tests and explain why it is necessary under .NET 10.

- [x] **Task 3 - Add generated renderer/form integration pins (AC: #1-#4)**
  - [x] Add SourceTools emitted-source tests proving full-page renderers emit
        `builder.OpenComponent<FcFormAbandonmentGuard>`, pass `CorrelationId`, pass
        `_formEditContext`, and pass `OnEditContextReady` from the generated form.
  - [x] Add a Shell generated-renderer integration test using the existing
        `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs` pattern
        or a new fixture command with five non-derivable fields so density is `FullPage`.
  - [x] In that integration test, drive an actual field edit through the generated form, attempt
        internal navigation through the test `NavigationManager`/`LocationChangingContext` seam, and
        prove the guard appears and blocks only the dirty path.
  - [x] Prove a clean generated full-page form does not show the guard and does not block navigation.
  - [x] Prove the generated renderer continues to refresh derivable values via `BeforeSubmit` and
        does not move command dispatch, ULID allocation, or lifecycle actions into the renderer.

- [x] **Task 4 - Preserve render-mode scope deliberately (AC: #4)**
  - [x] Keep Story 4.2 scoped to generated `CommandRenderMode.FullPage` command forms.
  - [x] Record in Completion Notes that Inline and CompactInline command renderers are intentionally
        out of scope because the existing guard is page-navigation protection and warning placement
        inside popovers/cards needs separate Product/UX approval.
  - [x] Do not wrap Inline or CompactInline forms in `FcFormAbandonmentGuard` in this story.

- [x] **Task 5 - Keep Epic 4 boundaries explicit (AC: #5)**
  - [x] Do not change `FcDestructiveConfirmationDialog` or destructive dialog API usage except for a
        directly proven regression in shared submit plumbing.
  - [x] Do not implement Story 4.3 FC-CNC queue/block policy.
  - [x] Do not implement Story 4.4 `[RequiresPolicy]` changes.
  - [x] Do not implement Story 4.5 retry/degraded retry behavior or change Epic 3 command-status
        polling budgets.
  - [x] Do not change FC-CMD identity/correlation: `MessageId` and `CorrelationId` remain
        26-character Crockford ULIDs generated by infrastructure abstractions.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run focused SourceTools tests for command renderer emitter output and any affected generated
        snapshots.
  - [x] Run focused Shell tests for `FcFormAbandonmentGuard` and generated full-page command renderer
        dirty/clean navigation behavior.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` when the environment permits. If local VSTest/MSBuild sockets are blocked, use the established in-process fallback and state CI remains authoritative.
  - [x] If specimen/e2e coverage is added, run the relevant `tests/e2e` lane when possible; otherwise
        state the sandbox limitation and rely on runnable bUnit/source-generator evidence.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; any snapshot change must be intentional and
        listed.
  - [x] Keep the File List complete, including story file, sprint status, source, tests, specimen/e2e
        files, and any intentional snapshots.

### Review Follow-ups (AI)

- [ ] [AI-Review][Medium][Out-of-scope] Pre-existing test
      `Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` fails in-process: the generated
      full-page breadcrumb falls back to `href="/"` instead of resolving `returnPath=%2Fcounter`
      from the query string when `PageContext.ReturnPath` is empty. This is Story 2-2 D32 breadcrumb
      behavior, predates Story 4.2 (committed before this story, unchanged by it), and is outside the
      abandonment-guard scope, so it was deliberately NOT fixed here to honor AC5 / the confirm-and-pin
      no-production-source-changes contract. Triage in its own fix. [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs:126]
- [ ] [AI-Review][Low] De-duplicate the test-local reflection seams
      (`BuildLocationChangingContext`, `InvokeNavigationChangingAsync`, `DidPreventNavigation`) now
      copied into both `FcFormAbandonmentGuardTests.cs` and `CommandRendererFullPageTests.cs` (same
      assembly) into one shared internal test helper. [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs:228]
- [ ] [AI-Review][Low] Remove the dead `|| pars[i].ParameterType == typeof(object)` branch in
      `FcFormAbandonmentGuardTests.BuildLocationChangingContext` — the leading `typeof(string)` check
      already short-circuits the string case. [tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcFormAbandonmentGuardTests.cs:350]

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- No separate PRD or UX artifact was found under `_bmad-output/planning-artifacts/`; architecture
  fallback came from `_bmad-output/project-context.md` and `_bmad-output/project-docs/*`.
- Loaded previous story context from
  `_bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md`.
- `sprint-status.yaml` has `epic-4: in-progress`, Story 4.1 `done`, and
  `4-2-unsaved-form-abandonment-guard: backlog` before this create-story run.
- External web research was not required: implementation depends on repo-pinned .NET 10,
  FluentUI v5 RC, bUnit, Fluxor, and existing local source contracts already recorded in project
  artifacts.

### Epic and Story Context

- Epic 4 layers safe command UX on top of Epic 3 command lifecycle: destructive confirmation,
  abandonment guard, one-at-a-time execution, policy authorization, and retry/degraded handling.
  [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Safe & Concurrent Command Execution]
- Story 4.2 specifically requires dirty command-form navigation attempts to be intercepted by
  `FcFormAbandonmentGuard`/`NavigationLock`, and clean forms to navigate without interruption. This
  story narrows the v1 implementation surface to generated full-page command forms, matching the
  existing component and generator mounting point.
  [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2: Unsaved-form abandonment guard]
- FR12 owns command lifecycle UI including form-abandonment guard and destructive-command
  confirmation; Epic 4 is the destructive/abandonment/authorization slice, not the core lifecycle
  identity contract. [Source: _bmad-output/planning-artifacts/epics.md#Functional Requirements]
- AR7 FC-CNC and AR8 retry/degraded policy are separate Story 4.3 and 4.5 concerns; do not solve
  them inside abandonment guard work. [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements]

### Current Source State to Preserve

- `FcFormAbandonmentGuard` already renders a `NavigationLock` and warning `FluentMessageBar` with
  `data-testid="fc-form-abandonment-warning"`, `fc-form-abandonment-stay`, and
  `fc-form-abandonment-leave`. The warning title is `You have unsaved input.` and the body is
  `Leaving now discards what you've entered.` [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor]
- The guard subscribes to `EditContext.OnFieldChanged`, captures only the first edit timestamp,
  detaches after anchoring, and is inert when `EditContext` is null. [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs]
- Navigation is blocked only after `FcShellOptions.FormAbandonmentThresholdSeconds` has elapsed.
  `FormAbandonmentThresholdSeconds` is seconds-based, defaults to 30, and is range-limited to
  5-600. [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs]
- `FcShellOptionsThresholdValidator` enforces
  `FormAbandonmentThresholdSeconds * 1000 > StillSyncingThresholdMs`, so the abandonment prompt
  cannot fire before the "Still syncing" lifecycle cue has had a chance to show. [Source: src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs]
- The guard yields for wrapper-initiated navigation and for `CommandLifecycleState.Submitting`.
  Comments explicitly state `Syncing` should still fire the guard. [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs]
- `OnLeaveClickedAsync` sets a one-shot `_isLeaving` bypass before `NavigateTo`, consumes it in the
  next navigation callback, and clears it on `NavigateTo` exceptions. Preserve this failure-safety
  behavior. [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs]
- `CommandFormEmitter` already exposes `OnEditContextReady`, creates an `EditContext`, subscribes to
  `OnFieldChanged`, sets `IsDirty = true`, and invokes `OnEditContextReady` after construction.
  [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `CommandRendererEmitter` already declares `_formEditContext`, captures it in
  `OnFormEditContextReady`, and wraps full-page forms in `FcFormAbandonmentGuard` with
  `CorrelationId`, `EditContext`, and child generated form attributes. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- Existing test coverage includes component-level first-edit anchoring, second-edit stability,
  stay/leave behavior, inert null-`EditContext`, and disposal unsubscribe tests. Coverage inventory
  did not show a dedicated generated full-page renderer dirty/clean navigation integration pin.
  [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcFormAbandonmentGuardTests.cs]

### Previous Story and Git Intelligence

- Story 4.1 fixed destructive renderer dialog API usage for FluentUI v5 RC and added generated
  renderer/form integration pins for destructive cancel/confirm/rapid-submit behavior. Preserve that
  pattern: generated-runtime proof matters more than standalone component proof. [Source: _bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md#Completion Notes List]
- Story 4.1 explicitly kept abandonment guard out of scope, so Story 4.2 is the first Epic 4 story
  that should own these guard proof points. [Source: _bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md#Tasks / Subtasks]
- Story 4.1 review left one low follow-up: add a dedicated bUnit pin proving required-field
  validation failure does not open the destructive dialog. Only pick this up if Story 4.2 changes
  shared destructive/form submit behavior; otherwise leave it for the destructive surface. [Source: _bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md#Review Follow-ups (AI)]
- Recent commits establish the expected implementation pattern: `feat(story-4.1)`, `feat(story-3.6)`,
  and `feat(story-3.5)` made narrow source changes, added focused tests, and recorded sandbox/CI
  limitations honestly.

### Project Structure Notes

- Shell component changes belong under:
  - `src/Hexalith.FrontComposer.Shell/Components/Forms/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/`
- Generated command renderer/form changes belong under:
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
- Specimen/e2e additions, if needed, belong under:
  - `samples/Counter/Counter.Specimens.Domain/`
  - `samples/Counter/Counter.Specimens/`
  - `tests/e2e/specs/`
- Do not edit generated output under `obj/**/generated/HexalithFrontComposer/`; change the generator
  or tests. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Do not write scratch docs into `docs/`; story notes and evidence belong under `_bmad-output/`.
  [Source: _bmad-output/project-context.md#Development Workflow Rules]

### Technical Guardrails

- Keep Contracts and SourceTools netstandard2.0-clean; SourceTools must reference only Contracts and
  must not depend on Shell. Generated code may reference Shell components. [Source: _bmad-output/project-docs/architecture.md#Layered structure]
- Keep generator IR pure and fully equatable. If abandonment metadata is added to renderer/form
  models, update equality/hash code instead of threading ad hoc mutable state. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Use existing `FcFormAbandonmentGuard`, `NavigationLock`, and FluentUI v5 RC components; do not add
  another modal/dialog/message-bar library. [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- Blazor component event handlers may need to remain on the component synchronization context. Follow
  the local `FcFormAbandonmentGuard` CA2007 suppression rationale instead of blindly adding
  `ConfigureAwait(false)` inside UI event handlers. [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs]
- Preserve generated command submit ordering: validation happens before `BeforeSubmit`; `BeforeSubmit`
  happens before ULID allocation, lifecycle `SubmittedAction`, and `ICommandService.DispatchAsync`.
  [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- Continue using `IUlidFactory` for correlation allocation and never add GUID fallbacks.
  [Source: _bmad-output/project-context.md#Critical Implementation Rules]

### Testing Requirements

- Use xUnit v3, Shouldly, NSubstitute, bUnit, and Verify.XunitV3 where applicable; do not use raw
  `Assert.*`. [Source: _bmad-output/project-context.md#Testing Rules]
- Run tests with `DiffEngine_Disabled=true` for Verify snapshot safety. [Source: _bmad-output/project-docs/development-guide.md#Test]
- Preferred focused lanes:
  - `FcFormAbandonmentGuardTests`
  - `FcShellOptionsValidationTests`
  - `CommandRendererEmitterTests`
  - new/updated generated full-page command renderer integration tests
  - any affected Story 4.1 destructive integration tests if shared submit or renderer behavior changes
- If e2e coverage is added, follow the existing `tests/e2e/specs/destructive-command-confirmation.spec.ts`
  pattern: specimen route, stable `data-testid`, and lifecycle assertions where applicable.
- Any `.verified.txt` change must be intentional, reviewed, and listed in the Dev Agent Record.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2: Unsaved-form abandonment guard]
- [Source: _bmad-output/project-context.md#Critical Implementation Rules]
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition (Shell)]
- [Source: _bmad-output/project-docs/api-contracts.md#Source-generator contract (`FrontComposerGenerator`)]
- [Source: _bmad-output/project-docs/component-inventory.md#Forms, dialogs & lifecycle]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcFormAbandonmentGuardTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Activation customization resolved with no prepend/append steps; loaded persistent project-context facts and config.
- 2026-06-04: Story 4.2 started from `ready-for-dev`; existing `baseline_commit` preserved and sprint status moved to `in-progress`.
- 2026-06-04: Task 1 audit completed before editing: verified guard component, shell options/validator, command form emitter, command renderer emitter, and Story 4.1 destructive follow-up.
- 2026-06-04: Added component-level navigation callback tests for dirty/clean/null/below-threshold behavior, Escape, one-shot leave bypass, Submitting suppression, Syncing protection, and wrapper-initiated navigation.
- 2026-06-04: Added SourceTools emitter pins for full-page `FcFormAbandonmentGuard` wiring and Inline/CompactInline non-wrapping scope.
- 2026-06-04: Added generated full-page renderer runtime pins proving dirty generated-form navigation blocks, clean navigation does not block, and `BeforeSubmit` still refreshes derivable `MessageId` before dispatch.
- 2026-06-04: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- 2026-06-04: Validation: SourceTools in-process `CommandRendererEmitterTests` passed 18/18; Shell in-process `FcFormAbandonmentGuardTests` passed 14/14; Shell in-process new generated full-page pins passed 3/3.
- 2026-06-04: Validation: required solution-level VSTest command was attempted with `DiffEngine_Disabled=true` and `--no-build`, but local MSBuild/VSTest named-pipe socket creation is sandbox-blocked (`SocketException (13): Permission denied`) before test execution. CI remains authoritative for that lane.
- 2026-06-04: Snapshot check `git diff --name-only -- '*.verified.txt'` returned no changes.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Completed Story 4.2 as a confirm-and-pin change with no production source changes. Existing guard/options/generator behavior was preserved.
- Added bUnit coverage for actual guard navigation callback behavior, including dirty threshold blocking, clean/null/below-threshold yielding, Stay/Escape clearing, one-shot Leave bypass consumption, precise Submitting suppression, Syncing protection, and wrapper-initiated navigation yielding.
- Added generated renderer/form integration pins proving full-page renderers pass `_formEditContext`, `CorrelationId`, `BeforeSubmit`, and `OnEditContextReady` through the generated form path.
- Inline and CompactInline command renderers remain intentionally out of scope because the existing guard is page-navigation protection; warning placement inside popovers/cards needs separate Product/UX approval.
- No destructive confirmation, FC-CNC queue/block policy, `[RequiresPolicy]`, retry/degraded, EventStore status polling, or FC-CMD identity/correlation changes were made.

### File List

- `_bmad-output/implementation-artifacts/4-2-unsaved-form-abandonment-guard.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcFormAbandonmentGuardTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`
- `tests/e2e/specs/form-abandonment-guard.spec.ts` (added by QA-automation step)
- `tests/e2e/package.json` (added `test:abandonment` focused lane)
- `tests/e2e/playwright.config.ts` (sets `Hexalith__Shell__FormAbandonmentThresholdSeconds=5` for the hosted Counter specimen)
- `_bmad-output/implementation-artifacts/tests/4-2-test-summary.md` (QA-automation test summary)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-04 (story-automator autonomous adversarial review, auto-fix mode)
**Outcome:** Approve (status → done). All five Acceptance Criteria are implemented and pinned; build is
clean; the new abandonment pins pass. Two MEDIUM and three LOW findings were raised; the in-scope
documentation finding was auto-fixed, the rest are tracked as Review Follow-ups (AI).

### Verification evidence (re-run during review)

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors**.
- `CommandRendererEmitterTests` (in-process xUnit v3) → **18/18 pass**, including the two new pins
  (`Renderer_FullPage_EmitsAbandonmentGuardWithCorrelationAndEditContextWiring`,
  `Renderer_InlineAndCompactInline_DoNotWrapFormsInAbandonmentGuard`).
- `FcFormAbandonmentGuardTests` → **14/14 pass**, including the six new navigation-callback pins
  (dirty/clean/null/below-threshold, Escape, one-shot Leave bypass non-leak, Submitting-suppressed /
  Syncing-still-protects, wrapper-initiated yield).
- `CommandRendererFullPageTests` → **13/14**: the three new Story 4.2 pins
  (`...DirtyGeneratedForm_ShowsAbandonmentGuardAndPreventsNavigation`,
  `...CleanGeneratedForm_DoesNotShowAbandonmentGuardOrPreventNavigation`,
  `...Submit_RefreshesDerivableValuesBeforeDispatch`) all pass. The single failure is the
  pre-existing, out-of-scope `Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty`
  (see Review Follow-ups M1).
- Solution-level VSTest lane remains sandbox-blocked (named-pipe socket `Permission denied`); the
  in-process xUnit v3 executables were run directly instead. CI remains authoritative for the e2e lane.

### AC coverage

| AC | Status | Evidence |
| -- | ------ | -------- |
| 1 Dirty full-page protected | IMPLEMENTED | `FcFormAbandonmentGuard` `NavigationLock` + warning bar; pinned by component dirty-threshold test and generated-renderer dirty test. |
| 2 Clean forms uninterrupted | IMPLEMENTED | clean / null-EditContext / below-threshold pins (component + generated renderer). |
| 3 Stay/Leave/Escape/lifecycle | IMPLEMENTED | Escape-as-Stay pin, one-shot `_isLeaving` non-leak pin, Submitting-suppressed-but-Syncing-protects pin, wrapper-initiated yield pin. |
| 4 Generated renderer integration pinned | IMPLEMENTED | emitter order pins (guard → form → `BeforeSubmit` → `OnEditContextReady`) + runtime `BeforeSubmit` derived-value refresh pin; Inline/CompactInline non-wrap pin. |
| 5 Scope held | IMPLEMENTED | `git status` shows no `src/**` or generated changes; only tests + e2e config. |

### Findings

- **M1 (Medium, auto-fixed):** The dev File List omitted the QA-automation artifacts
  (`tests/e2e/specs/form-abandonment-guard.spec.ts`, `tests/e2e/package.json`,
  `tests/e2e/playwright.config.ts`, `_bmad-output/.../tests/4-2-test-summary.md`). Root cause is the
  story-automator sequencing (QA-automation runs after dev records the File List). **Fixed** by adding
  the four files to the File List above.
- **M2 (Medium, tracked — Review Follow-ups):** Pre-existing failing test
  `Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty` (breadcrumb returnPath query fallback).
  Unchanged by and unrelated to Story 4.2; not auto-fixed to honor AC5 / no-production-source-changes.
- **L1–L3 (Low, tracked — Review Follow-ups):** test-helper duplication across the two test files;
  dead `typeof(object)` branch in one `BuildLocationChangingContext`; e2e spec authored but not yet
  executed (sandbox Kestrel socket block, honestly recorded in the QA test summary).

### Validated risk that turned out fine

- The e2e `Hexalith__Shell__FormAbandonmentThresholdSeconds=5` override does NOT break startup
  validation: `FcShellOptionsThresholdValidator` requires `5s*1000 > StillSyncingThresholdMs(2000)`
  and `StillSyncing(2000) < TimeoutAction(5000)` — both hold with the playwright env overrides.
- The e2e locators resolve against real Counter elements: `/commands/Counter/ConfigureCounterCommand`
  route, `.fc-command-form[aria-label="Configure Counter command form"]`, the `Configure Counter`
  link, and a single breadcrumb link "Counter" (the trailing crumb is plain text, so the
  `getByRole('link', {name: /counter/i})` locator is unambiguous).

### Change Log

- 2026-06-04: Added component and generated renderer/form abandonment guard pins; preserved full-page-only scope and Epic 4 boundaries; recorded build/test evidence and sandbox limitation for solution-level VSTest.
- 2026-06-04: Senior Developer Review (AI) — adversarial review with auto-fix. Re-ran build (0/0) and focused in-process tests (emitter 18/18, guard 14/14, full-page 13/14). Reconciled File List with QA-automation e2e artifacts (auto-fixed). Logged one pre-existing out-of-scope full-page breadcrumb-query test failure plus two low test-maintainability items as Review Follow-ups (AI). Status review → done.
