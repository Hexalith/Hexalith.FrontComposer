---
baseline_commit: c6ff094e5f5badff5021d381439810d5d7938b67
---

# Story 3.3: Confirm the FC-CMD pending-identity and correlation contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The command identity surface already exists, but it is spread
> across generated forms, Shell lifecycle services, pending-command state, EventStore dispatch, and
> MCP lifecycle docs. The current live shape is:
>
> - Generated forms create a local `correlationId` with `IUlidFactory.NewUlid()` before dispatch and
>   use it to key the generated Fluxor lifecycle state and `FcLifecycleWrapper`.
> - `ICommandService` implementations create/return the accepted command `MessageId`; `PendingCommandStateService`
>   uses canonical 26-character Crockford ULID `MessageId` as the pending-command key.
> - Pending state is scoped per circuit/user, stores only framework metadata, and fail-closes on
>   tenant/user transitions when `IUserContextAccessor` is present.
> - `PendingCommandStatus.IdempotentConfirmed` maps to lifecycle `Confirmed` with
>   `idempotencyResolved=true`; duplicate terminal observations are ignored.
>
> Critical gap found during story creation: `UlidFactory.NewUlid()` catches
> `CryptographicException` and falls back to `Guid.NewGuid().ToString("N")`, which is 32 hex
> characters and violates the FC-CMD/NFR2 rule that `messageId` and `correlationId` are 26-character
> ASCII ULIDs, never GUIDs. Story 3.3 must remove or fail-close that fallback and pin the behavior
> with tests. Do not leave the fallback as an "unlikely" edge case.

## Story

As a FrontComposer maintainer,
I want the command-lifecycle identity contract pinned,
so that all command epics share one agreed pending-identity / correlation model.

## Acceptance Criteria

**AC1 - FC-CMD contract artifact records every identity decision or named escalation.**  
**Given** the FC-CMD contract draft,  
**When** this story completes,  
**Then** `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
exists and records the decided v1 contract for:
correlation-key shape, pending identity shape, uniqueness scope, lifecycle ownership,
`alreadyApplied` semantics, and reconciliation responsibility,  
**And** any item that cannot be decided is escalated with owner, due date, and the exact downstream
story it blocks.

**AC2 - `messageId` and `correlationId` are 26-character ASCII Crockford ULIDs, never GUIDs.**  
**Given** generated forms, `StubCommandService`, and `EventStoreCommandClient`,  
**When** a command is submitted and accepted,  
**Then** both the form `correlationId` and accepted `messageId` are produced through
`IUlidFactory.NewUlid()` and match `^[0-9A-HJKMNP-TV-Z]{26}$`,  
**And** no production command identity path uses `Guid.NewGuid()`, `Guid.Parse`, `Guid.TryParse`, or
32/36-character GUID formatting for `messageId` or `correlationId`.

**AC3 - Pending-command identity is keyed by framework-owned `MessageId` with bounded scope.**  
**Given** an accepted command result,  
**When** the generated form registers pending state,  
**Then** `PendingCommandStateService` stores the entry by canonical uppercase `MessageId`, rejects
malformed `MessageId`, rejects malformed `CorrelationId`, stores only framework metadata
(`CorrelationId`, `MessageId`, command type, optional projection/lane/entity/status-slot metadata),
and never stores raw command payload, form values, tenant ID, user ID, or validation text,  
**And** the contract states the v1 uniqueness scope as circuit-local pending state plus
tenant/user-transition fail-closed clearing, with backend uniqueness carried by the ULID `MessageId`.

**AC4 - Duplicate, idempotent, and conflicting observations have one visible outcome.**  
**Given** duplicate registration or terminal observations for the same `MessageId`,  
**When** the pending-command service processes them,  
**Then** matching duplicate registration merges, conflicting metadata is rejected,
terminal-after-terminal is counted and ignored, and only the first terminal outcome changes the
entry,  
**And** an idempotent/already-applied server outcome maps to
`PendingCommandStatus.IdempotentConfirmed` and lifecycle `Confirmed` with
`idempotencyResolved=true` so UI renders the "already confirmed" path instead of a new-success
celebration.

**AC5 - Reconciliation uses the shared resolver contract without broadening scope.**  
**Given** live nudge refresh, reconnect reconciliation, fallback polling, or future EventStore
status lookup input,  
**When** a terminal outcome arrives,  
**Then** `PendingCommandOutcomeResolver` resolves primarily by `MessageId`, may fall back to
framework-controlled entity/lane/status-slot metadata only when there is exactly one pending
candidate, leaves ambiguous matches unresolved, and routes terminal state through
`IPendingCommandStateService.ResolveTerminal`,  
**And** Story 3.5 remains responsible for binding the concrete `GET /api/v1/commands/status/{id}`
EventStore endpoint; Story 3.3 only pins the identity/reconciliation contract consumed by that
endpoint.

**AC6 - Existing command generation and density behavior does not regress.**  
**Given** Stories 3.1 and 3.2 pinned command artifacts, generated form lifecycle ordering, density,
derivable-field exclusion, and `IUlidFactory` correlation allocation,  
**When** this story completes,  
**Then** those tests and snapshots remain green,  
**And** any `.verified.txt` changes are intentional, minimal, and explained in the Dev Agent Record.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live FC-CMD identity path before editing (AC: #1-#6)**
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` around
        `OnValidSubmitAsync`, `correlationId`, pending registration, and acknowledged dispatch.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`,
        `Services/StubCommandService.cs`, and
        `Infrastructure/EventStore/EventStoreCommandClient.cs` before changing identity allocation.
  - [x] Read `src/Hexalith.FrontComposer.Shell/State/PendingCommands/` completely:
        `IPendingCommandStateService.cs`, `PendingCommandModels.cs`,
        `PendingCommandStateService.cs`, `PendingCommandOutcomeResolver.cs`, and
        `PendingCommandPollingCoordinator.cs`.
  - [x] Read lifecycle contracts and implementation:
        `IUlidFactory.cs`, `ILifecycleStateService.cs`, `CommandLifecycleTransition.cs`,
        `CommandLifecycleState.cs`, and `LifecycleStateService.cs`.
  - [x] Reconcile stale in-source comments that still mention older story numbers only where files are
        touched; do not perform broad comment-only churn.

- [x] **Task 2 - Produce the FC-CMD contract artifact (AC: #1, #3, #4, #5)**
  - [x] Create `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
        with explicit sections for `MessageId`, `CorrelationId`, scope, lifecycle ownership,
        `alreadyApplied`, reconciliation, and out-of-scope items.
  - [x] State the v1 model clearly: `CorrelationId` is the generated form/lifecycle subscription key;
        `MessageId` is the accepted command and pending-command identity key.
  - [x] State that pending state is circuit-local and fail-closes on tenant/user transitions; do not
        claim durable cross-tab replay until a later story actually provides it.
  - [x] State that Story 3.5 owns the concrete EventStore status endpoint binding and numeric
        polling budgets stay with Story 3.6.
  - [x] Link the contract artifact from this story's Dev Agent Record and, if project docs are
        adjusted, from `_bmad-output/project-docs/api-contracts.md`; do not use published `docs/` as
        scratch space.

- [x] **Task 3 - Remove the GUID fallback and pin ULID-only generation (AC: #2, #6)**
  - [x] Change `UlidFactory.NewUlid()` so it never returns a GUID. Prefer fail-closed behavior on
        cryptographic RNG failure over returning a non-ULID identifier.
  - [x] Update `UlidFactoryTests` to prove the normal path returns 26-character Crockford Base32,
        remains unique under parallel calls, and has no GUID fallback path.
  - [x] Add a source scan or focused test proving production command identity paths do not contain
        `Guid.NewGuid()` in generated command-form output, `StubCommandService`, or
        `EventStoreCommandClient`.
  - [x] Preserve `IUlidFactory` as the seam; do not introduce a second ID abstraction or package.
  - [x] Do not upgrade `NUlid`. The repo pin is `NUlid` 1.7.3 in `Directory.Packages.props`; official
        NuGet information confirms 1.7.3 and the 26-character canonical ULID shape.

- [x] **Task 4 - Validate both pending identity fields, not just `MessageId` (AC: #2, #3)**
  - [x] Extend `PendingCommandStateService` validation so `CorrelationId` also must be a
        26-character Crockford ULID; lowercase may be accepted only if canonicalized consistently.
  - [x] Add tests for invalid `CorrelationId` length, invalid characters, lowercase normalization
        behavior if supported, and no pending state mutation on rejection.
  - [x] Confirm `PendingCommandRegistration` still fails fast on null/whitespace
        `CorrelationId`, `MessageId`, and `CommandTypeName`.
  - [x] Keep `StringComparer.Ordinal` and explicit canonicalization; do not use culture-sensitive
        normalization or NFKC folding.

- [x] **Task 5 - Pin duplicate and already-applied semantics (AC: #4, #6)**
  - [x] Re-run or extend `PendingCommandStateServiceTests` for matching duplicate registration,
        conflicting duplicate registration, `MergedTerminal`, terminal-after-terminal
        `DuplicateIgnored`, and first-outcome-wins behavior.
  - [x] Re-run or extend `ResolveTerminal_IdempotentConfirmed_PreservesAlreadyAppliedOutcome` so
        `PendingCommandStatus.IdempotentConfirmed` dispatches lifecycle `Confirmed` with
        `idempotencyResolved=true`.
  - [x] Re-run or extend `FcLifecycleWrapperIdempotentTests` so the UI renders the idempotent Info
        path and does not emit the normal success celebration.
  - [x] Preserve the generated action ordering from Story 3.1:
        bridge/subscriber ensure, `SubmittedAction`, dispatch, pending registration, then
        `AcknowledgedAction` only when pending registration permits it.

- [x] **Task 6 - Pin reconciliation responsibility and ambiguity handling (AC: #5)**
  - [x] Re-run or extend `PendingCommandOutcomeResolverTests` for primary `MessageId` resolution,
        exactly-one framework metadata fallback, ambiguous fallback, absent identifiers, rejection
        metadata, and no form/storage mutation on reject.
  - [x] Re-run or extend `PendingCommandPollingCoordinatorTests` for oldest-first processing,
        zero-cap disablement, skip-already-resolved, thrown status-query continuation, and bounded
        `MaxPendingCommandPollingPerTick`.
  - [x] Keep `NullPendingCommandStatusQuery` as the default until Story 3.5 binds the real
        EventStore endpoint.
  - [x] Do not add row-level `FcNewItemIndicator` producer wiring in this story; command outcome
        row indicators belong to later lifecycle/status work once the producer payload is available.

- [x] **Task 7 - Verify build/tests and record evidence honestly (AC: #1-#6)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and require 0 warnings / 0 errors.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest is still socket-blocked with `SocketException (13): Permission denied`, use
        the established xUnit v3 in-process fallback and record CI as the authoritative
        solution-level VSTest gate.
  - [x] At minimum run focused lanes for `CommandFormEmitterTests`, `UlidFactoryTests`,
        `LifecycleStateServiceTests`, `FcLifecycleWrapperIdempotentTests`,
        `PendingCommandStateServiceTests`, `PendingCommandOutcomeResolverTests`, and
        `PendingCommandPollingCoordinatorTests`.
  - [x] Check `git diff --name-only -- '*.verified.txt'` and record whether snapshots changed.
  - [x] Keep the File List complete, including the FC-CMD contract artifact, this story file,
        sprint status, source changes, tests, and any generated snapshots.

## Dev Notes

### Current identity contract anchors

- `CommandFormEmitter` emits generated forms with injected `IUlidFactory` and creates
  `var correlationId = UlidFactory.NewUlid();` before `SubmittedAction`. The generated form stores
  `_submittedCorrelationId` and passes that value to `FcLifecycleWrapper`, so sibling forms ignore
  lifecycle events from another submission. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- Generated forms call `CommandService.DispatchAsync(...)`, then register pending state only after
  accepted command results. Pending registration currently includes `CorrelationId`, returned
  `MessageId`, and command type name; optional projection/lane/entity/status-slot metadata is not
  available to the source generator at form-emit time. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs]
- `EventStoreCommandClient` creates `messageId` with `IUlidFactory.NewUlid()`, sends it in
  `SubmitCommandRequest`, and returns it in `CommandResult`. It may read a response
  `correlationId`, but the generated form lifecycle continues to use its local form
  `correlationId`. Do not silently swap these identities without updating the contract and tests.
  [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs]
- `StubCommandService` creates accepted command `MessageId` with `IUlidFactory.NewUlid()` and
  invokes `Syncing`/`Confirmed` callbacks with that message ID for local demo/test hosts.
  [Source: src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs]
- `IUlidFactory.NewUlid()` promises a 26-character Crockford Base32 ULID string. `UlidFactory`
  currently violates that promise on `CryptographicException` by returning a GUID string; this is a
  story-owned gap. [Source: src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs]
  [Source: src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs]

### Pending-command state anchors

- `PendingCommandStateService` stores entries in a `Dictionary<string, PendingCommandEntry>` keyed
  by canonical uppercase `MessageId` with `StringComparer.Ordinal`. `TryNormalizeMessageId` accepts
  26 Crockford characters, uppercases lowercase input, and rejects invalid length/characters.
  [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- `PendingCommandRegistration` intentionally excludes raw command payloads, form values, tenant IDs,
  user IDs, and validation messages. Preserve that boundary. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs]
- Pending state is scoped through DI (`TryAddScoped<IPendingCommandStateService, PendingCommandStateService>()`)
  and clears pending entries on tenant/user transitions when a real `IUserContextAccessor` is
  present. [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs]
  [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- `FcShellOptions.MaxPendingCommandEntries` bounds pending entries and
  `MaxPendingCommandPollingPerTick` bounds fallback status polling. The threshold validator requires
  polling-per-tick to be less than or equal to the pending-entry cap. [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs]
  [Source: src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs]

### Lifecycle and reconciliation anchors

- `ILifecycleStateService` is keyed by `CorrelationId` and exposes transitions to subscribers.
  `CommandLifecycleTransition.IdempotencyResolved` is the already-applied signal consumed by UI.
  [Source: src/Hexalith.FrontComposer.Contracts/Lifecycle/ILifecycleStateService.cs]
  [Source: src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleTransition.cs]
- `PendingCommandStateService.ResolveTerminal(...)` maps `IdempotentConfirmed` to lifecycle
  `Confirmed` with `idempotencyResolved=true`. Duplicate terminal observations are counted and
  ignored, preserving one visible terminal outcome. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs]
- `PendingCommandOutcomeResolver` resolves by `MessageId` first. Without `MessageId`, it only uses
  framework-controlled `EntityKey` plus optional projection/lane/status-slot metadata, and it refuses
  ambiguous matches. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs]
- `PendingCommandPollingCoordinator` is transport-neutral and calls `IPendingCommandStatusQuery`.
  The default `NullPendingCommandStatusQuery` intentionally does nothing until Story 3.5 wires the
  EventStore status endpoint. [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs]

### Architecture and constraints

- SourceTools must remain a pure Roslyn incremental generator: parse -> pure IR -> transform -> emit.
  Do not let `ISymbol` escape parse-stage models, and do not add Shell or FluentUI references to
  SourceTools. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Generated code is not hand-edited. Change the generator/emitter or annotated test fixtures.
  Output under `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is a public contract.
  [Source: _bmad-output/project-context.md#Source-Generator Rules]
- All versions are centrally pinned. Do not add `Version=` to `.csproj` files and do not upgrade
  packages for this story. Relevant pins include `.NET SDK 10.0.300`, `NUlid` 1.7.3, Roslyn 5.3.0,
  FluentUI Blazor `5.0.0-rc.3-26138.1`, Fluxor 6.9.0, xUnit v3 3.2.2, and Verify.XunitV3 31.19.0.
  [Source: Directory.Packages.props] [Source: global.json]
- Official NuGet information checked on 2026-06-04 lists `NUlid` 1.7.3 and describes the canonical
  ULID string as 26 characters in Crockford Base32. Use the repo pin and fix contract compliance
  locally; do not chase alternative ULID packages. [Source: https://www.nuget.org/packages/NUlid]
- Testing rules are solution-level by default and require `DiffEngine_Disabled=true` for Verify
  snapshot tests. If the local VSTest socket restriction recurs, use the established xUnit v3
  in-process fallback and record the limitation. [Source: _bmad-output/project-context.md#Testing Rules]

### Previous story and retro intelligence

- Story 3.1 fixed generated form `correlationId` allocation from direct `Guid.NewGuid()` to
  `IUlidFactory.NewUlid()` and pinned command artifact, registration, diagnostic, placeholder,
  lifecycle, and pending-registration behavior. Preserve that fix. [Source: _bmad-output/implementation-artifacts/3-1-generate-a-command-form-from-a-command-type.md]
- Story 3.2 confirmed density and derivable-field exclusion with no production source changes.
  Story 3.3 must not reopen density thresholds, command page emission, FC-TBL, or projection grid
  behavior. [Source: _bmad-output/implementation-artifacts/3-2-apply-the-density-rule-to-command-forms.md]
- Epic 2 retro says current planning keys override stale comments that refer to command lifecycle as
  Story 5-5. New FC-CMD references should use Story 3.3 for identity/correlation, Story 3.5 for
  EventStore status endpoint binding, and Story 3.6 for budgets. [Source: _bmad-output/implementation-artifacts/epic-2-retro-2026-06-04.md]
- Story 2.6 accepted deferral of row-level `FcNewItemIndicator` producer wiring because the
  projection nudge lacks row identity. Do not add row identity to the projection nudge seam here.
  [Source: _bmad-output/implementation-artifacts/2-6-live-projection-updates-with-reconnect-reconciliation.md]

### Project Structure Notes

- Expected production files if source changes are needed:
  `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`,
  `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`,
  `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`,
  `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs`,
  `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, and possibly
  `src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs` if public contract wording must
  be tightened.
- Expected test files:
  `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/UlidFactoryTests.cs`,
  `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs`,
  `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs`,
  `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs`,
  `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperIdempotentTests.cs`,
  `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateServiceTests.cs`, and
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`.
- Documentation/contract updates belong under `_bmad-output/contracts/` or
  `_bmad-output/project-docs/`. Do not use published `docs/` as scratch space.
- Leave submodules alone. Do not modify `Hexalith.EventStore/`, `Hexalith.Tenants/`, or
  `Hexalith.Commons/`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3] - story statement and initial ACs.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3] - command authoring/lifecycle scope.
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md#Asks] - FC-CMD readiness ask.
- [Source: _bmad-output/project-context.md#Technology Stack & Versions] - pinned versions and package rules.
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules] - Fluxor/scope discipline.
- [Source: _bmad-output/project-context.md#Testing Rules] - solution-level test command and Verify discipline.
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition] - Shell lifecycle composition.
- [Source: _bmad-output/project-docs/api-contracts.md#Tools] - MCP command/lifecycle identity surface.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] - generated form correlation and pending registration.
- [Source: src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs] - ULID factory implementation and current GUID fallback gap.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs] - EventStore command `MessageId` allocation.
- [Source: src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs] - stub command `MessageId` allocation.
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs] - pending state keying, normalization, duplicate, idempotent, and scope behavior.
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs] - shared outcome resolution.
- [Source: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs] - status-query seam and polling budget.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs] - current pending-command coverage.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/UlidFactoryTests.cs] - current ULID tests.
- [Source: https://www.nuget.org/packages/NUlid] - NUlid package and canonical ULID shape.

## Senior Developer Review (AI)

Reviewer: Jérôme Piquot — 2026-06-04 (story-automator adversarial review, auto-fix mode)

Outcome: **Approve.** All six acceptance criteria are implemented and independently re-verified against
live source; build is clean (0/0) and every focused lane the story names is green.

Evidence gathered during review:
- Build: `dotnet build src/Hexalith.FrontComposer.Shell` Release → 0 warnings / 0 errors.
- Focused Shell lane (UlidFactory, LifecycleStateService, FcLifecycleWrapperIdempotent,
  PendingCommandState, PendingCommandOutcomeResolver, PendingCommandPollingCoordinator) → 55/55 pass.
- SourceTools `CommandFormEmitterTests` → 24/24 pass; the two changed `.verified.txt` snapshots are the
  intentional `Story 5-5 → Story 3.3` comment renumber plus the `InvalidCorrelationId` enum-match
  additions — minimal and expected (AC6).
- AC2 GUID scan: `StubCommandService` and `EventStoreCommandClient` allocate identity only via
  `IUlidFactory.NewUlid()`; no `Guid.*`/`ToString("N")` in the four named production paths. The
  `UlidFactory` GUID fallback is removed — `NewUlid()` now fails closed by propagating any RNG failure.
- AC3/AC4 test quality: `Register_MalformedCorrelationId_FailsClosedWithoutStateMutation` (rejects bad
  length, the invalid Crockford char `I`, and over-length input) asserts `Snapshot().ShouldBeEmpty()`;
  lowercase normalization, first-outcome-wins, idempotent-confirmed, and conflicting-metadata paths all
  have real assertions.
- Full-suite baseline (the solution-level VSTest the Dev Record reported as socket-blocked actually runs
  in this environment): Shell project = 8 failures / 1767, SourceTools = 3 failures / 985. **All are
  pre-existing and unrelated to Story 3.3** — the 4 governance tests fail on a `deferred-work.md` that
  was removed in commit `06e361d` (absent at baseline `c6ff094` too); the remaining failures are
  projection-snapshot, navigation-route-hydration, full-page query-fallback timeout (the documented 3.2
  baseline flake), datagrid-doc, IDE path-case, and diagnostic-ledger tests — none touch the
  ULID / pending-command / command-form surface this story changed.

Findings (no Critical, no High):
- **MEDIUM (auto-fixed)** — File List omitted `tests/e2e/specs/command-form-generation.spec.ts`, which
  this story modified (new `Story 3.3: FC-CMD pending identity and correlation contract` describe block
  + `expectFrameworkIdentityHidden` helper). Added to File List.
- **LOW (auto-fixed)** — File List omitted the modified
  `_bmad-output/implementation-artifacts/tests/test-summary.md`. Added to File List.
- **LOW (observation, not fixed)** — the new Story 3.3 e2e test asserts the transient `Submitting`
  text is visible before `fc-confirmed`; under the fast `StubCommandService` this could be timing-
  sensitive. e2e is out of the unit gate and was not executed here, so this is flagged for the
  Playwright lane rather than blocked.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Create-story workflow loaded project context, sprint status, epics, readiness request,
  brownfield architecture/source-tree docs, previous Stories 3.1 and 3.2, recent git history, and
  live command/lifecycle/pending source files.
- 2026-06-04: Discovered Story 3.3 is a contract-confirmation and guardrail story over existing
  command identity surfaces, not a rewrite of command generation, density, or status endpoint
  polling.
- 2026-06-04: Found critical contract gap: `UlidFactory.NewUlid()` can return a GUID string on
  cryptographic RNG failure. Story ACs/tasks require removing or fail-closing that fallback.
- 2026-06-04: Official NuGet/package check completed for `NUlid`; no package update is required or
  desired.
- 2026-06-04: Dev-story activation resolved no prepend/append steps. Preserved existing
  `baseline_commit: c6ff094e5f5badff5021d381439810d5d7938b67`, loaded project context, story, and
  sprint status, and moved Story 3.3 to in-progress.
- 2026-06-04: Re-audited `CommandFormEmitter`, `UlidFactory`, `StubCommandService`,
  `EventStoreCommandClient`, pending-command services, lifecycle contracts, and lifecycle
  implementation before editing.
- 2026-06-04: Implemented `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
  as the FC-CMD v1 identity/correlation contract.
- 2026-06-04: Removed the `UlidFactory` GUID fallback; `NewUlid()` now returns only the NUlid
  canonical ULID or fails closed by propagating generation failure.
- 2026-06-04: Extended pending registration validation/canonicalization to `CorrelationId`, added
  `InvalidCorrelationId`, and updated generated forms to skip `AcknowledgedAction` when pending
  registration rejects malformed correlation identity.
- 2026-06-04: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`
  passed with 0 warnings and 0 errors.
- 2026-06-04: Validation: exact solution-level
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
  failed before test execution with `System.Net.Sockets.SocketException (13): Permission denied`
  from MSBuild named-pipe setup; CI remains the authoritative VSTest gate.
- 2026-06-04: Validation fallback: xUnit v3 in-process Shell focused lane passed 55/55
  (`UlidFactoryTests`, `LifecycleStateServiceTests`, `FcLifecycleWrapperIdempotentTests`,
  `PendingCommandStateServiceTests`, `PendingCommandOutcomeResolverTests`, and
  `PendingCommandPollingCoordinatorTests`).
- 2026-06-04: Validation fallback: xUnit v3 in-process SourceTools focused lane passed 24/24
  (`CommandFormEmitterTests`) after accepting the two intentional generated-form snapshots.
- 2026-06-04: Snapshot check: `git diff --name-only -- '*.verified.txt'` reports only
  `CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
  and `CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`;
  both changes are intentional and limited to the generated pending-registration identity skip path.

### Completion Notes List

- FC-CMD contract artifact created and linked here:
  `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`.
- `MessageId` and `CorrelationId` are pinned to 26-character Crockford ULID identity paths:
  production allocation stays behind `IUlidFactory`, and command identity source scan rejects GUID
  generation/parsing/formatting APIs in the named production paths.
- Pending command state now validates and canonicalizes both `MessageId` and `CorrelationId`, rejects
  malformed correlation IDs without state mutation, keeps raw command/user/tenant/form data out of
  pending entries, and preserves `StringComparer.Ordinal` semantics.
- Duplicate registration, terminal first-outcome-wins, idempotent already-applied UI, resolver
  ambiguity handling, polling behavior, and generated submit ordering were re-verified.
- No `NUlid` package change was made, Story 3.5 EventStore status endpoint binding remains out of
  scope, Story 3.6 polling budgets remain out of scope, and row-level new-item producer wiring was
  not added.
- Two `.verified.txt` snapshots changed intentionally and minimally to reflect generated
  `InvalidCorrelationId` handling and Story 3.3 generated-code comments.

### File List

- `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
- `_bmad-output/implementation-artifacts/3-3-confirm-the-fc-cmd-pending-identity-and-correlation-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/UlidFactoryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/e2e/specs/command-form-generation.spec.ts`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

### Change Log

- 2026-06-04: Removed GUID fallback from ULID generation, added correlation ULID validation, created
  FC-CMD contract artifact, added/updated identity guardrail tests, accepted two intentional
  generated-form snapshots, and moved story to review.
- 2026-06-04: Story-automator adversarial review (auto-fix). Re-verified all 6 ACs against live source;
  build 0/0, Shell focused lane 55/55, CommandFormEmitterTests 24/24. Confirmed the full-suite Shell (8)
  and SourceTools (3) failures are pre-existing and unrelated to this story (governance tests depend on a
  `deferred-work.md` removed in 06e361d; remainder are projection-snapshot / navigation / full-page-
  timeout / docs / IDE-parity / ledger baselines). Auto-fixed two File List completeness gaps
  (`tests/e2e/specs/command-form-generation.spec.ts`, `tests/test-summary.md`). 0 Critical / 0 High;
  status moved review → done.
