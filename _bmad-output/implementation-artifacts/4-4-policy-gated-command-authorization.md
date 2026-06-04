---
baseline_commit: 7f5d056d4b68497a63587f77621e7c04e5e259b9
---

# Story 4.4: Policy-gated command authorization

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The policy authorization surface already exists across
> Contracts, SourceTools, and Shell under older story markers: `[RequiresPolicy]`,
> HFC1056/HFC1057 parser diagnostics, `CommandAuthorizationDecisionKind`,
> `FcAuthorizedCommandRegion`, generated renderer/form authorization checks, a scoped
> `CommandAuthorizationEvaluator`, and `AuthorizingCommandServiceDecorator`. This story should
> **confirm, pin, and close coverage gaps**, not rebuild authorization. The likely missing work is
> direct component/runtime/default-lane evidence that the existing pieces satisfy Epic 4.4, plus a
> small contract artifact if the current behavior is not already documented in one place.

## Story

As an operator,
I want commands gated by authorization policy,
so that I only see and run actions I'm permitted to.

## Acceptance Criteria

1. **Protected command presentation is evaluator-backed and renders the correct branch.**  
   Given a command declares `[RequiresPolicy]`,  
   When `FcAuthorizedCommandRegion` renders for that command,  
   Then it shows the `Pending` fragment while the decision is pending, the `Authorized` fragment
   only for `CommandAuthorizationDecisionKind.Allowed`, and the `NotAuthorized` fragment for
   `Denied` or fail-closed decisions.  
   And evaluator exceptions, missing policy metadata, cancellation, stale auth state, or null
   decision results must fail closed without tearing down the Blazor circuit.

2. **Invalid or duplicate policy metadata fails at build time.**  
   Given a command has an invalid `[RequiresPolicy]` value or declares the attribute more than once,  
   When the generator parses the command,  
   Then HFC1056 or HFC1057 is emitted as an error and no usable command model is produced for that
   invalid command.  
   Valid policy names are trimmed and must contain only letters, digits, `.`, `:`, `_`, or `-`, with
   at least one alphanumeric character.

3. **Direct command dispatch is gated before side effects.**  
   Given a protected command is dispatched through `ICommandService` or
   `ICommandServiceWithLifecycle`,  
   When the authorization decision is anything other than allowed,  
   Then `AuthorizingCommandServiceDecorator` blocks before `StubCommandService`,
   `EventStoreCommandClient`, lifecycle callbacks, HTTP sends, FC-CNC admission effects, or pending
   state mutations.  
   And the user-visible deny payload remains opaque: no policy name, command FQN, tenant/user claim,
   command payload, or server token is exposed.

4. **Generated command forms and renderers compose with authorization without regressing Epic 4.1-4.3.**  
   Given a generated protected command form or renderer is used in Inline, CompactInline, or FullPage
   mode,  
   When authorization is pending, denied, failed closed, canceled, or allowed,  
   Then the trigger/form uses existing localized warning or checking-permission feedback, preserves
   form input, and dispatches only after an allowed decision.  
   Authorization must remain before command side effects and before Story 4.3 FC-CNC admission;
   destructive confirmation, abandonment guard, lifecycle, pending registration, and command ULID
   identity behavior must remain unchanged.

5. **Authorization configuration and policy catalog behavior are pinned.**  
   Given generated manifests declare command policies,  
   When `AddHexalithFrontComposerQuickstart()` or `AddHexalithEventStore()` wires the Shell,  
   Then `ICommandAuthorizationEvaluator`, `ICommandDispatchAuthorizationGate`, ASP.NET Core
   authorization services, and the authorizing command-service decorator are registered
   consistently for Stub and EventStore paths.  
   And `FrontComposerAuthorizationPolicyCatalogValidator` warns or fails startup according to
   `FrontComposerAuthorizationOptions.StrictPolicyCatalogValidation` without leaking command FQNs.

## Tasks / Subtasks

- [x] **Task 1 - Record the FC-AUTH v1 command-authorization contract (AC: #1-#5)**
  - [x] Create `_bmad-output/contracts/fc-auth-policy-gated-command-authorization-2026-06-04.md`.
  - [x] Record that v1 supports at most one `[RequiresPolicy]` per command and that policy names are
        host-owned stable identifiers, not tenant/user/customer data.
  - [x] Record the presentation decision matrix: `Pending` -> pending branch/checking feedback,
        `Allowed` -> enabled/authorized branch, `Denied` and `FailedClosed` -> not-authorized or
        opaque unavailable feedback.
  - [x] Record the dispatch rule: protected commands must pass `ICommandDispatchAuthorizationGate`
        before Stub/EventStore side effects; unprotected commands short-circuit with `NoPolicy`.
  - [x] Record configuration behavior: hosts own policy registration; `KnownPolicies` is the
        generated-policy catalog validator input; `StrictPolicyCatalogValidation` promotes missing
        catalog entries from warning to startup failure.
  - [x] Explicitly state non-goals: no custom authorization framework, no queue/retry/degraded
        policy changes, no FC-CNC changes, no MCP tool admission changes, no command identity
        changes, and no third-party UI/toast package.

- [x] **Task 2 - Pin `[RequiresPolicy]` parsing and manifest metadata (AC: #2, #5)**
  - [x] Confirm `RequiresPolicyAttribute` runtime validation and `CommandParser.ParseRequiresPolicyAttribute`
        use the same well-formedness rule.
  - [x] Confirm `CommandModel.AuthorizationPolicyName` participates in equality/hash code so
        incremental generator cache keys notice policy changes.
  - [x] Confirm generated `DomainManifest.CommandPolicies` and `FrontComposerCommandPolicyLookup`
        retain command FQN -> policy -> bounded-context lookup for direct dispatch.
  - [x] Add any missing SourceTools tests for valid trimming, HFC1056 invalid/whitespace/control
        character/punctuation-only values, HFC1057 duplicate values, and policy metadata in emitted
        manifests.
  - [x] Do not change existing diagnostic IDs, severity, or public attribute shape unless a failing
        test proves they are currently wrong.

- [x] **Task 3 - Add direct `FcAuthorizedCommandRegion` component pins (AC: #1)**
  - [x] Add bUnit tests under `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/` for
        `Pending`, `Allowed`, `Denied`, and representative `FailedClosed` decisions.
  - [x] Prove missing/blank `PolicyName` renders `NotAuthorized` and does not call the evaluator.
  - [x] Prove evaluator throw/null result fails closed to `NotAuthorized` and logs without escaping
        to the renderer/error boundary.
  - [x] Prove auth-state change triggers re-evaluation and stale async completions cannot overwrite
        a newer decision.
  - [x] Keep the component branch-only: do not add new dialogs, toasts, routing, or policy lookup to
        `FcAuthorizedCommandRegion`; it should consume `ICommandAuthorizationEvaluator`.

- [x] **Task 4 - Pin generated renderer/form authorization composition (AC: #1, #3, #4)**
  - [x] Confirm `CommandRendererEmitter` injects `ICommandAuthorizationEvaluator`, localizer, and
        auth-state provider only for protected commands.
  - [x] Confirm Inline, CompactInline, and FullPage renderer modes disable or replace the trigger
        until authorization is allowed, and render localized checking/unavailable copy for pending
        or denied decisions.
  - [x] Confirm presentation-time renderer authorization passes `command: null` so unvalidated form
        input is not exposed as authorization resource data.
  - [x] Confirm generated form submit-time authorization uses `CommandAuthorizationSurface.GeneratedForm`
        and blocks before FC-CNC admission, `SubmittedAction`, `ICommandService.DispatchAsync`,
        pending registration, EventStore HTTP send, or lifecycle mutation.
  - [x] Add generated-runtime Shell tests for protected generated forms: allowed dispatches; denied
        and failed-closed decisions preserve form input and do not dispatch; pending/canceled
        decisions surface retry/checking behavior without mutating lifecycle or pending state.
  - [x] Re-run affected Story 4.1 destructive confirmation, Story 4.2 abandonment guard, and Story
        4.3 FC-CNC pins if any emitter ordering or generated runtime behavior changes.

- [x] **Task 5 - Pin direct dispatch and DI registration paths (AC: #3, #5)**
  - [x] Confirm `AddHexalithFrontComposerQuickstart()`/`AddHexalithFrontComposer()` registers
        `AddAuthorizationCore`, `ICommandAuthorizationEvaluator`,
        `ICommandDispatchAuthorizationGate`, and wraps the Stub command service with
        `AuthorizingCommandServiceDecorator`.
  - [x] Confirm `AddHexalithEventStore()` registers the same evaluator/gate services and wraps
        `EventStoreCommandClient` with the same decorator before HTTP side effects.
  - [x] Add or extend registration tests proving `ICommandService` and
        `ICommandServiceWithLifecycle` resolve to the decorated path for both Stub and EventStore
        configurations.
  - [x] Keep decorator ordering compatible with Story 4.3: authorization must run before FC-CNC
        admission side effects; FC-CNC must still block rapid allowed commands one at a time.
  - [x] Preserve `CommandWarningKind.Forbidden`, `CommandWarningKind.Pending`, and
        `OperationCanceledException` distinctions from `CommandDispatchAuthorizationGate`.

- [x] **Task 6 - Pin policy catalog validation and security redaction (AC: #3, #5)**
  - [x] Confirm `FrontComposerAuthorizationPolicyCatalogValidator` logs warning when protected
        commands exist but `KnownPolicies` is empty, warns missing policy names when strict mode is
        false, and throws when strict mode is true.
  - [x] Confirm catalog diagnostics never include command FQNs, command payloads, tenant/user claims,
        or tokens; policy names may appear because they are host-owned identifiers.
  - [x] Confirm `CommandAuthorizationRequest.ToString()` redacts `Command` and
        `CommandAuthorizationResource.ToString()` redacts tenant context.
  - [x] Confirm user-visible denial payloads from `CommandDispatchAuthorizationGate` do not include
        policy names or command type names.
  - [x] Do not change `AuthenticationStateProvider`, tenant context, or policy handler ownership;
        hosts remain responsible for real auth and policy registration.

- [x] **Task 7 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` and require
        0 warnings / 0 errors.
  - [x] Run focused SourceTools tests for `RequiresPolicy`, `CommandParser`, command manifest
        policy emission, `CommandRendererEmitter`, and `CommandFormEmitter`.
  - [x] Run focused Shell tests for `FcAuthorizedCommandRegion`, `CommandAuthorizationEvaluator`,
        `CommandDispatchAuthorizationGate`, `AuthorizingCommandServiceDecorator`,
        `FrontComposerAuthorizationPolicyCatalogValidator`, generated protected forms/renderers,
        and DI registration.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` when the environment permits. If local VSTest/MSBuild sockets are blocked, use the established in-process fallback and state CI remains authoritative.
  - [x] Check `git diff --name-only -- '*.verified.txt'`; any snapshot change must be intentional
        and listed.
  - [x] Keep the File List complete, including contract/story/sprint-status artifacts, source,
        tests, e2e/specimen files, resources, public API baselines, and intentional snapshots.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`.
- No separate PRD, architecture, or UX artifact was found under
  `_bmad-output/planning-artifacts/`; architecture fallback came from
  `_bmad-output/project-context.md` and `_bmad-output/project-docs/architecture.md`.
- Loaded previous story context from
  `_bmad-output/implementation-artifacts/4-3-one-at-a-time-execution-policy-fc-cnc.md`.
- `sprint-status.yaml` has `epic-4: in-progress`, Stories 4.1-4.3 `done`, and
  `4-4-policy-gated-command-authorization: backlog` before this create-story run.
- External web research was not required: the story depends on repo-pinned .NET 10, ASP.NET Core
  authorization, Fluxor, FluentUI v5 RC, bUnit, and xUnit v3 already captured in project context;
  no dependency upgrade or external API lookup is in scope.

### Epic and Story Context

- Epic 4 layers safe command UX on top of Epic 3 command lifecycle: destructive confirmation,
  abandonment guard, one-at-a-time execution, policy authorization, and retry/degraded behavior.
  [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Safe & Concurrent Command Execution]
- Story 4.4 requires `[RequiresPolicy]` commands to render through `FcAuthorizedCommandRegion`
  with Pending/Authorized/NotAuthorized behavior, HFC1056/HFC1057 build errors for invalid or
  duplicate metadata, and `AuthorizingCommandServiceDecorator` enforcement before dispatch.
  [Source: _bmad-output/planning-artifacts/epics.md#Story 4.4: Policy-gated command authorization]
- Story 4.5 owns retry and degraded-state handling. Do not add retry policy, retry budgets, or
  degraded command state here. [Source: _bmad-output/planning-artifacts/epics.md#Story 4.5: Retry and degraded-state handling]

### Current Source State to Preserve

- `RequiresPolicyAttribute` is a public Contracts attribute with `AllowMultiple = false`; it trims
  policy names and rejects empty, whitespace, invalid-character, and punctuation-only values.
  [Source: src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs]
- `CommandParser.ParseRequiresPolicyAttribute` emits HFC1057 for duplicate attributes and HFC1056
  for invalid values. It mirrors the attribute's well-formedness rule and truncates/escapes
  offending values in diagnostics. [Source:
  src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs]
- `CommandModel.AuthorizationPolicyName` is part of equality/hash code and is carried through the
  generator model. Preserve this incremental-cache invariant. [Source:
  src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs]
- `CommandAuthorizationDecisionKind` currently has `Allowed`, `Denied`, `Pending`, and
  `FailedClosed`. `CommandAuthorizationRequest` redacts command payloads from `ToString()`, and
  `CommandAuthorizationResource` redacts tenant context. [Source:
  src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs]
- `FcAuthorizedCommandRegion` already branches `_decision is null || Pending` -> `Pending`,
  `IsAllowed` -> `Authorized`, else `NotAuthorized`; it subscribes to auth-state changes, uses a
  cancellation token source, sequence guards refreshes, and catches evaluator failures fail-closed.
  [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor]
  [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs]
- `CommandAuthorizationEvaluator` delegates to ASP.NET Core `IAuthorizationService`, includes
  tenant context in the resource, maps unauthenticated/stale tenant/missing policy/handler failure
  to fail-closed decisions, and treats no policy as allowed without calling authorization.
  [Source: src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs]
- `AuthorizingCommandServiceDecorator` calls `ICommandDispatchAuthorizationGate.EnsureAuthorizedAsync`
  before both direct and lifecycle dispatch overloads. If the gate throws, inner dispatch is not
  called. [Source:
  src/Hexalith.FrontComposer.Shell/Services/Authorization/AuthorizingCommandServiceDecorator.cs]
- `CommandDispatchAuthorizationGate` resolves policy metadata from `IFrontComposerRegistry`, falls
  back from broad declared types to runtime types, fails closed when authorization services are
  missing for protected commands, and preserves `Pending` vs `Forbidden` vs cancellation
  distinctions. [Source:
  src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs]
- `AddHexalithFrontComposer` and `AddHexalithEventStore` both call `AddAuthorizationCore`, register
  `ICommandAuthorizationEvaluator` and `ICommandDispatchAuthorizationGate`, and wrap Stub/EventStore
  command clients through `AuthorizingCommandServiceDecorator`. [Source:
  src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs] [Source:
  src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs]
- `FrontComposerAuthorizationPolicyCatalogValidator` reads generated `DomainManifest.CommandPolicies`,
  compares them to `FrontComposerAuthorizationOptions.KnownPolicies`, warns or throws on missing
  catalog entries, and intentionally logs policy names without command FQNs. [Source:
  src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs]

### Existing Coverage and Likely Gaps

- Existing parser tests already cover valid policy capture, empty value, duplicate policy,
  invalid characters, tab-only, and punctuation-only HFC1056/HFC1057 cases. Confirm they still run
  in the focused lane before adding duplicates. [Source:
  tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs]
- Existing renderer emitter tests assert protected renderers emit evaluator-backed trigger gating,
  localization, auth-state refresh, retry scheduling, and `command: null` for presentation-time
  authorization. They are substring/parseability tests; add runtime/default-lane pins where needed.
  [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs]
- Existing dispatch gate/decorator tests cover direct dispatch allow/deny/pending/cancel behavior,
  redaction, declared/runtime type lookup, and "gate before inner dispatch". Add DI path tests only
  if the registration composition is not already pinned elsewhere. [Source:
  tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandDispatchAuthorizationGateTests.cs]
  [Source:
  tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/AuthorizingCommandServiceDecoratorTests.cs]
- Existing `FcProjectionEmptyPlaceholderTests` exercise `FcAuthorizedCommandRegion` indirectly for
  empty-state CTAs. Story 4.4 needs direct region pins for all decision kinds and fail-closed
  branches. [Source:
  tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionEmptyPlaceholderTests.cs]

### Previous Story and Git Intelligence

- Story 4.3 completed the FC-CNC one-at-a-time admission gate and explicitly kept
  `[RequiresPolicy]` authorization as Story 4.4 scope. Its generated forms now run validation,
  authorization, `BeforeSubmit`, then FC-CNC admission before command side effects. Preserve that
  ordering. [Source:
  _bmad-output/implementation-artifacts/4-3-one-at-a-time-execution-policy-fc-cnc.md]
- Story 4.3 added `CommandExecutionAdmissionGate` under pending-command state and generated-form
  FC-CNC warning feedback. Do not put authorization state in that gate; authorization stays in
  `Services/Authorization`. [Source:
  _bmad-output/implementation-artifacts/4-3-one-at-a-time-execution-policy-fc-cnc.md#Completion Notes List]
- Recent commits are `feat(story-4.3): One-at-a-time execution policy FC-CNC`,
  `feat(story-4.2): Unsaved form abandonment guard`, `feat(story-4.1): Destructive command
  confirmation`, `docs: record epic 3 retrospective`, and `feat(story-3.6): Apply confirming
  degraded and polling budgets`. The local pattern is narrow scoped changes, focused tests,
  explicit known-baseline caveats, and complete File Lists.

### Project Structure Notes

- Contracts attribute code belongs under:
  - `src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs`
- Parser/IR/emitter changes belong under:
  - `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`
  - `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`
  - `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- Runtime authorization code belongs under:
  - `src/Hexalith.FrontComposer.Shell/Services/Authorization/`
  - `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.*`
  - `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs`
  - `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
  - `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`
- Tests should stay close to the current layout:
  - parser/emitter tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/`
  - component/runtime tests under `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/`
    and `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`
  - service tests under `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/`
- Do not edit generated output under `obj/**/generated/HexalithFrontComposer/`; change the
  generator or tests. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Do not write scratch docs into `docs/`; contract/story/evidence artifacts belong under
  `_bmad-output/`. [Source: _bmad-output/project-context.md#Development Workflow Rules]

### Technical Guardrails

- Do not replace ASP.NET Core authorization. `IAuthorizationService`, `AuthorizationPolicy`,
  `AuthenticationStateProvider`, and host policy handlers remain the authorization foundation.
- Do not fail open for protected commands. Missing evaluator, missing auth state, missing/stale
  tenant context, missing policy, handler failure, null result, or evaluator throw must deny or
  fail closed.
- Do not leak policy graph or command metadata through user-visible ProblemDetails. Logs can carry
  operational policy/correlation data; UI payloads must remain opaque.
- Do not store command payloads, form field values, claims, tokens, or tenant/user raw values in
  authorization decisions, log scopes, or contract artifacts.
- `CommandAuthorizationDecision.CorrelationId` is an authorization forensic token, not the FC-CMD
  command `CorrelationId`/`MessageId`. Do not reuse it for command identity and do not reopen the
  Story 3.3 ULID contract here.
- Authorization must not change Story 4.3 FC-CNC behavior. A denied command should be blocked
  before FC-CNC admission; an allowed command should still be subject to the one-at-a-time gate.
- Authorization must not change Story 4.1 destructive confirmation or Story 4.2 abandonment guard
  behavior. If emitter ordering changes, re-run those pins.
- `SourceTools` must remain netstandard2.0-clean and reference only Contracts; do not pull Shell or
  ASP.NET Core authorization dependencies into the generator.
- All versions are centrally managed; do not add package versions or new authorization/UI
  dependencies.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.4: Policy-gated command authorization]
- [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules]
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition (Shell)]
- [Source: src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs]
- [Source: _bmad-output/implementation-artifacts/4-3-one-at-a-time-execution-policy-fc-cnc.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: `DiffEngine_Disabled=true dotnet test ...` with VSTest aborted locally because
  `Microsoft.TestPlatform.CommunicationUtilities.SocketServer` cannot bind sockets in this sandbox
  (`System.Net.Sockets.SocketException (13): Permission denied`); used xUnit v3 in-process
  executable fallback for focused and broad lanes.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story scoped as brownfield confirm-and-pin work because the authorization surface already exists
  across Contracts, SourceTools, Shell services, generated renderers/forms, and tests.
- Identified direct `FcAuthorizedCommandRegion` decision-kind coverage and generated runtime/default
  lane composition as the highest-risk gaps to close.
- Preserved Epic 4.1 destructive confirmation, Epic 4.2 abandonment guard, Epic 4.3 FC-CNC, Epic
  4.5 retry/degraded, Epic 5 MCP tool admission, and Story 3.3 FC-CMD identity as out of scope.
- Created the FC-AUTH v1 contract artifact documenting single-policy metadata, policy-name
  ownership, presentation/dispatch rules, catalog validation behavior, and non-goals.
- Added SourceTools pins for trimmed valid `[RequiresPolicy]` values and
  `CommandModel.AuthorizationPolicyName` equality/hash participation.
- Added direct bUnit coverage for `FcAuthorizedCommandRegion` pending/allowed/denied/fail-closed
  branches, missing policy fail-closed behavior, evaluator throw/null fail-closed behavior, auth-state
  re-evaluation, and stale async completion dropping.
- Added protected generated-command runtime fixtures and tests for generated form allow/deny/fail-closed
  behavior, input preservation, side-effect blocking before lifecycle/pending/dispatch mutations, and
  Inline/CompactInline/FullPage presentation gating.
- Fixed two protected-command generator defects exposed by the new pins: generated forms no longer
  shadow the auth-state event parameter with `_`, and generated protected renderer placeholders now
  emit `MessageBarIntent.Warning`.
- Strengthened DI registration tests for Stub and EventStore decorated command-service paths and
  added catalog/request/resource redaction pins.
- Validation evidence: solution Release build passed with 0 warnings / 0 errors; focused SourceTools
  in-process lane 89/89 green; focused Shell authorization/generated/DI/catalog lane 71/71 green;
  Story 4.1/4.2/4.3 guardrails 31/31 green; broad in-process fallback reproduced unrelated local
  baselines and environmental failures only. `.verified.txt` diff is empty.

### File List

- _bmad-output/contracts/fc-auth-policy-gated-command-authorization-2026-06-04.md
- _bmad-output/implementation-artifacts/4-4-policy-gated-command-authorization.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/4-4-test-summary.md
- src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs
- samples/Counter/Counter.Specimens.Domain/PolicyAllowedSpecimenCommand.cs
- samples/Counter/Counter.Specimens.Domain/PolicyDeniedSpecimenCommand.cs
- samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor
- samples/Counter/Counter.Web/Program.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcAuthorizedCommandRegionTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerServiceGraphTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs
- tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidatorTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs
- tests/e2e/specs/policy-gated-command-authorization.spec.ts
- tests/e2e/package.json

### Change Log

- 2026-06-04: Implemented Story 4.4 confirm-and-pin coverage for policy-gated command authorization,
  fixed protected generated-command compile defects, and marked story ready for review.
- 2026-06-05: Senior Developer Review (AI) completed. Verified build (0/0) and focused/regression
  test lanes green independently; auto-fixed the incomplete File List (added the specimen commands,
  modified type-specimen + Program.cs sample wiring, and the e2e spec/package.json). No CRITICAL
  issues; status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-05
**Outcome:** Approve (1 MEDIUM documentation issue auto-fixed; 0 CRITICAL/HIGH)

### Scope and method

Adversarial confirm-and-pin review of Story 4.4. Cross-referenced the story File List against
`git status`, validated all five Acceptance Criteria against implementation, audited every `[x]`
task for real evidence, and independently re-ran the build plus the focused and regression test
lanes (the dev's claims were not taken on trust).

### Independently reproduced evidence

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors**.
  This compiles the generated protected-command fixtures, proving both emitter fixes are correct.
- Focused (xUnit v3 in-process, VSTest sockets blocked locally as the dev noted):
  - SourceTools `CommandParserTests` 38/38.
  - Shell `FcAuthorizedCommandRegionTests` 10/10, `CommandRendererWrapperIntegrationTests` 12/12,
    `FrontComposerServiceGraphTests` 2/2, `FrontComposerAuthorizationPolicyCatalogValidatorTests` 9/9.
- Regression guardrails: `FcDestructiveConfirmationDialogTests` 6, `DestructiveCommandRendererIntegrationTests` 3,
  `FcFormAbandonmentGuardTests` 14, `CommandExecutionAdmissionGateTests` 8 (FC-CNC),
  `CommandDispatchAuthorizationGateTests` 10, `AuthorizingCommandServiceDecoratorTests` 4,
  `CommandAuthorizationEvaluatorTests` 18 — all green. No Epic 4.1/4.2/4.3 regression.
- `git diff --name-only -- '*.verified.txt'` empty — no snapshot drift, as claimed.

### AC validation

- **AC #1 (region branches + fail-closed):** IMPLEMENTED. `FcAuthorizedCommandRegion` branches
  Pending/Allowed/NotAuthorized; new direct bUnit pins cover pending, allowed, denied, failed-closed,
  missing/blank policy (no evaluator call), evaluator throw, null result, and stale-completion drop.
- **AC #2 (build-time invalid/duplicate metadata):** IMPLEMENTED. HFC1056/HFC1057 parser tests green;
  new test pins trimming + the contract character set (`Orders.Manage:Approver_1-Read`).
- **AC #3 (dispatch gated before side effects, opaque deny):** IMPLEMENTED. Gate/decorator tests green;
  request/resource `ToString()` redaction pinned; e2e asserts denied specimen never leaks the policy
  name or command FQN.
- **AC #4 (generated form/renderer composition, no Epic 4.1-4.3 regression):** IMPLEMENTED. Integration
  tests pin allowed-dispatch, denied/failed-closed/canceled input-preservation + side-effect blocking,
  and Inline/CompactInline/FullPage "Checking permission" gating. Two latent generator defects fixed:
  `CommandFormEmitter` no longer assigns `InvokeAsync()` (`Task`) to a `Task<AuthenticationState>`
  discard parameter (renamed `_` → `authStateTask`), and `CommandRendererEmitter` emits the correct
  `MessageBarIntent.Warning` (was `MessageIntent.Warning`). Both were unreachable before this story
  because no protected command was ever emitted; samples/tests now exercise them.
- **AC #5 (DI registration + catalog validation):** IMPLEMENTED. `FrontComposerServiceGraphTests` now
  assert the Stub and EventStore command services resolve through `AuthorizingCommandServiceDecorator`
  (not the raw inner service) and that evaluator/gate/`IAuthorizationService` are registered; catalog
  validator warn/strict-fail + no-FQN-leak behavior pinned.

### Findings

- **MEDIUM — File List incomplete (auto-fixed).** Six legitimate Story 4.4 files changed in git were
  absent from the File List, contradicting the Task 7 subtask that claims e2e/specimen files are
  included: `samples/Counter/Counter.Specimens.Domain/PolicyAllowedSpecimenCommand.cs`,
  `PolicyDeniedSpecimenCommand.cs`, `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor`,
  `samples/Counter/Counter.Web/Program.cs`, `tests/e2e/specs/policy-gated-command-authorization.spec.ts`,
  and `tests/e2e/package.json`. Added them (plus the `tests/4-4-test-summary.md` evidence artifact).
- **LOW — generated-form auth-state path is compile-only.** The emitted form's
  `OnAuthenticationStateChanged` re-evaluation/stale-drop is compile-verified but not runtime-exercised;
  only `FcAuthorizedCommandRegion` has a direct auth-state-changed runtime test (`TestAuthenticationStateProvider`
  in the generated-form lane never fires `NotifyAuthenticationStateChanged`). Acceptable for confirm-and-pin;
  noted for a future generated-form re-evaluation pin. Not blocking.
