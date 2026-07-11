---
created: 2026-07-09
epic: 11
story: 6
story_key: 11-6-testing-harness-failure-modes
source_epics: _bmad-output/planning-artifacts/epics.md
baseline_commit: a783c82bf9cdfbe1347c23ac6f8e699ae540eef4
baseline_revision: 3a4551bc1fe81d12999c6b70e86f8cce74e03d67
status: done
review_loop_iteration: 1
followup_review_recommended: true
final_revision: 8086851e47215d658489d9dd318d473063abbd7e
---

# Story 11.6: Testing Harness Failure Modes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer, starting with Hexalith.Tenants,
I want the Testing harness to model rejection, timeout, stall, authorization, and per-request query outcomes,
so adopters can genuinely test failure paths and paging, filtering, and sorting of generated components.

## Acceptance Criteria

1. Given `TestCommandService`, when adopter tests configure command and query outcomes, then it exposes configurable rejection, timeout, and stall-at-`Syncing` outcomes; `TestQueryService` and `TestProjectionPageLoader` accept per-request callbacks (`SucceedWith(Func<QueryRequest, QueryResult<T>>)` for queries and an equivalent page-loader callback carrying paging/filter/sort/search inputs); and `TestFaultInjectionProvider` either actually affects fake behavior or is renamed/reframed as an evidence recorder with public API and docs updated honestly. (M21)

2. Given the Counter sample's authorization-policy toggles, when those scenarios are promoted into the Testing harness, then the harness exposes equivalent configurable authorization-policy states for generated component tests, and constructor `GetAwaiter().GetResult()` usage is replaced with an async factory path.

3. Given the shipped Testing surface, when builders, assertions, or fakes are changed, then `Builders`, `Assertions`, and fakes get direct surface tests, not only host-level tests, and `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` is updated intentionally for each adopter-facing API change.

4. Given Story 10.5's Testing evidence privacy findings and the Testing host contract, when Story 11.6 changes fake services, per-request callbacks, builders, assertions, or fault/evidence paths that emit diagnostic or assertion evidence, then the default Testing lane preserves redaction for configured tenant/user identifiers in JSON values and property names, including dictionary keys; preserves structural redaction of token/secret/password keyed values; and proves raw external/local paths are absent or replaced with bounded repository-relative or redacted markers wherever the harness emits paths.

## Tasks / Subtasks

- [ ] Reconfirm brownfield state before editing. (AC: 1, 2, 3, 4)
  - [ ] Read this story, Epic 11 Story 11.6 in `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/project-context.md`, `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, and the Testing package host contract before changing code.
  - [ ] Read every file listed in "Current Files To Read Before Editing" completely before editing it.
  - [ ] Re-run `git status --short` before edits and preserve unrelated user or generated changes.
  - [ ] Treat existing behavior as partially useful but incomplete: current command/query/page fakes are deterministic and redacted, but the harness is still happy-path biased.

- [ ] Add command-service failure-mode configuration. (AC: 1, 4)
  - [ ] Preserve the default success behavior: deterministic message/correlation IDs, `Acknowledged -> Syncing -> Confirmed`, `CommandResultStatus.Accepted`, bounded redacted evidence, and cancellation before evidence.
  - [ ] Add adopter-facing configuration for rejection using the existing `CommandRejectedException` contract and `CommandLifecycleState.Rejected`; do not invent a parallel rejection vocabulary.
  - [ ] Add deterministic timeout and stall-at-`Syncing` modes that let generated component tests assert "still syncing", timeout action, and unresolved command UX without sleeping on wall-clock time.
  - [ ] Ensure lifecycle callbacks receive only the states implied by the configured outcome; stall-at-`Syncing` must not emit `Confirmed`, and rejection must not masquerade as accepted success.
  - [ ] Capture evidence for configured failure modes without leaking command payload identifiers, tokens, secrets, passwords, or raw paths.

- [ ] Add per-request query and projection-page callbacks. (AC: 1, 4)
  - [ ] Extend `TestQueryService` with `SucceedWith<T>(Func<QueryRequest, QueryResult<T>>)` while preserving existing list-based `SucceedWith<T>(IReadOnlyList<T>, string?)` and `NotModifiedWith<T>(...)` behavior.
  - [ ] Make callback evidence include the request projection type, skip/take, tenant, and mode while keeping sensitive fields redacted if any new diagnostic payloads are introduced.
  - [ ] Extend `TestProjectionPageLoader` with a per-request callback that exposes `projectionTypeFqn`, `skip`, `take`, `filters`, `sortColumn`, `sortDescending`, and `searchQuery`; introduce a small Testing-owned request record only if it reduces API ambiguity.
  - [ ] Prove paging, filtering, sorting, search, not-modified, empty, and cancellation paths directly in Testing tests.

- [ ] Resolve the fault-provider contract honestly. (AC: 1, 3, 4)
  - [ ] Decide whether `TestFaultInjectionProvider` will actually inject deterministic effects into the command/query/page fakes or be renamed/reframed as an evidence recorder.
  - [ ] If keeping "Injection" naming, prove at least one configured fault changes fake behavior in a deterministic test.
  - [ ] If reframing as an evidence recorder, update the type/member names or XML docs, README, how-to, host contract, public API baseline, and tests so adopters are not told it injects when it only records.
  - [ ] Keep the no live SignalR/EventStore/DAPR/browser-storage guarantee.

- [ ] Promote authorization-policy states into the Testing harness. (AC: 2, 3, 4)
  - [ ] Add a deterministic fake or options surface for `ICommandAuthorizationEvaluator` using existing Shell contracts: `CommandAuthorizationDecision.Allowed`, `Denied`, `Pending`, and `Blocked(...)` / failed-closed reasons.
  - [ ] Wire the fake through `AddFrontComposerTestHost(...)` so generated command renderers and `FcAuthorizedCommandRegion` can test allowed, denied, pending, missing-policy, unauthenticated, and handler-failed states without app-specific setup.
  - [ ] Use the Counter specimen policy names `Specimens.PolicyAllowed` and `Specimens.PolicyDenied` as behavioral precedent, not as hard-coded Testing defaults.
  - [ ] Add direct Testing tests that render or exercise policy-gated command behavior through the harness.

- [ ] Replace sync-over-async host setup with an async factory path. (AC: 2, 3)
  - [ ] Remove or bypass `GetAwaiter().GetResult()` in Testing package setup code.
  - [ ] Add an async composition path for `StoreInitializationMode.DuringHostSetup`, such as `AddFrontComposerTestHostAsync(...)`, that initializes Fluxor with `await ...ConfigureAwait(false)`.
  - [ ] For inheritance-based `FrontComposerTestBase`, keep constructor behavior safe and documented: constructors cannot be async, so default to explicit `InitializeStoreAsync()` or provide a documented async factory/test initialization pattern.
  - [ ] Update docs and tests so adopters know which setup path to use for during-host store initialization.

- [ ] Broaden direct Testing package surface tests. (AC: 3, 4)
  - [ ] Split or add focused tests for `TestCommandService`, `TestQueryService`, `TestProjectionPageLoader`, the fault provider/recorder, authorization fake, builders, assertions, redaction formatter, and async host factory.
  - [ ] Keep `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` as the public API gate and update `PublicAPI.Shipped.txt` only after reviewing each signature.
  - [ ] Update package README, `docs/how-to/test-generated-components.md`, and `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` for behavior/API drift.

- [ ] Preserve evidence privacy and package quality gates before review. (AC: 3, 4)
  - [ ] Keep Story 10.5 redaction matrix coverage for configured tenant/user values, property names, dictionary keys, and token/secret/password keyed values.
  - [ ] Add path redaction or bounded path tests only if Story 11.6 introduces path-bearing evidence or assertion messages.
  - [ ] Run the focused Testing package build/test lane, the direct xUnit runner fallback if VSTest sockets are blocked, package/public API tests, broad solution lane when feasible, Release build, `git diff --check`, and story artifact validation.

## Dev Notes

### Story Context

Epic 11 is architecture-review remediation for v1.0 release hardening. Story 11.6 closes M21: the Testing package currently lets adopters prove happy paths but not realistic failure, authorization, paging, filtering, or sorting flows. The PRD accepts this as v1.0 assumption A1 and maps success metrics through SM-5: adopter tests can simulate command success, rejection, timeout/stall, authorization denial, paging/filter/sort, and redacted evidence. [Source: `_bmad-output/planning-artifacts/epics.md`; `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`]

The package is adopter-facing and packable. Any public helper, fake, assertion, record, or setup method added or renamed in this story must be intentional, documented, tested directly, and reflected in `PublicAPI.Shipped.txt`. [Source: `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`; `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`]

Story 10.5 already tightened Testing evidence redaction. Story 11.6 expands the same fake/evidence surface, so it must preserve those privacy guarantees rather than adding a second sanitizer or bypassing the existing `RedactedEvidenceFormatter` without proof. [Source: `_bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e10r-ai-3-testing-privacy-into-11-6.md`]

### Brownfield Implementation Snapshot

Current inspection on 2026-07-09 found this live state. Verify against the working tree before relying on it:

- `TestCommandService` always succeeds today: it emits deterministic IDs, invokes `Acknowledged -> Syncing -> Confirmed`, records bounded redacted evidence, and returns `CommandResultStatus.Accepted`. It has no configured rejection, timeout, or stall mode.
- `ICommandService` already defines rejection as `CommandRejectedException`, and `CommandLifecycleState` already includes `Rejected`. Use those contracts.
- `TestQueryService` stores one configured `QueryResult<T>` per item type through `SucceedWith<T>(IReadOnlyList<T>, string?)` or `NotModifiedWith<T>(...)`. It records skip/take evidence but cannot vary results by `QueryRequest`.
- `QueryRequest` already carries skip, take, ETag, column filters, status filters, search query, sort column, sort direction, domain, aggregate, query type, entity, actor type, ETags, cache discriminator, and payload version.
- `TestProjectionPageLoader` stores one `ProjectionPageResult` per projection FQN. It receives filters, sort, and search from `LoadPageAsync(...)` but ignores them when selecting results.
- `TestFaultInjectionProvider` only records `Drop`, `Delay`, `PartialDelivery`, `Reorder`, and `ReconnectNudge` evidence. The 7.5 host contract explicitly calls it an evidence recorder, while Story 11.6 requires either real injection or honest rename/reframing.
- `FrontComposerTestBase` and `FrontComposerTestHostServiceCollectionExtensions.AddFrontComposerTestHost(...)` currently call `InitializeAsync().GetAwaiter().GetResult()` for during-host store initialization.
- The Counter specimen has `[RequiresPolicy("Specimens.PolicyAllowed")]` and `[RequiresPolicy("Specimens.PolicyDenied")]` commands. The sample registers one always-true and one always-false policy and renders both generated command renderers under "Policy-gated command authorization".
- Shell authorization contracts already expose `ICommandAuthorizationEvaluator`, `CommandAuthorizationRequest`, `CommandAuthorizationDecision`, `CommandAuthorizationDecisionKind.Allowed/Denied/Pending/FailedClosed`, `CommandAuthorizationReason`, and `CommandAuthorizationSurface`.
- The Testing test project currently has broad host-level coverage in `FrontComposerTestHostTests.cs` plus package boundary coverage in `PackageBoundaryTests.cs`. Story 11.6 must add or split direct surface tests for the fakes, builders, assertions, and new auth/async APIs.

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs`
- `src/Hexalith.FrontComposer.Testing/TestQueryService.cs`
- `src/Hexalith.FrontComposer.Testing/TestProjectionPageLoader.cs`
- `src/Hexalith.FrontComposer.Testing/TestFaultInjectionProvider.cs`
- `src/Hexalith.FrontComposer.Testing/Evidence.cs`
- `src/Hexalith.FrontComposer.Testing/Assertions.cs`
- `src/Hexalith.FrontComposer.Testing/Builders.cs`
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestBase.cs`
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestHostBuilder.cs`
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestOptions.cs`
- `src/Hexalith.FrontComposer.Testing/README.md`
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md`
- `docs/how-to/test-generated-components.md`
- `src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResultStatus.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs`
- `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/IProjectionPageLoader.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/ICommandAuthorizationEvaluator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/AuthorizingCommandServiceDecorator.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs`
- `samples/Counter/Counter.Specimens.Domain/PolicyAllowedSpecimenCommand.cs`
- `samples/Counter/Counter.Specimens.Domain/PolicyDeniedSpecimenCommand.cs`
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor`
- `samples/Counter/Counter.Web/Program.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcAuthorizedCommandRegionTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionEmptyPlaceholderTests.cs`

### Architecture Compliance

- Keep implementation inside the Testing package plus the minimum Shell/Contracts test fixture references needed for public contracts already consumed by Testing. Do not change EventStore, SignalR, MCP, SourceTools emitters, package versions, release artifacts, or sibling submodules unless a direct compile break proves it is required.
- Preserve the Testing package as a consumer-layer leaf for tests. Fakes must not open EventStore, SignalR, DAPR, browser storage, live HTTP, or a running app host.
- Use the repo's .NET 10 / C# latest rules, nullable-enabled code, `TreatWarningsAsErrors`, central package management, `.slnx` solution commands, and one top-level C# type per file for new public records/classes.
- Keep production awaits using `ConfigureAwait(false)`. The async factory path must not replace one sync-over-async deadlock risk with another hidden blocking call.
- Use existing Shell and Contracts types for command outcomes and authorization. Avoid duplicate enums unless they are Testing-specific configuration abstractions with clear mapping tests.
- Do not initialize nested submodules, use recursive submodule commands, or edit `references/Hexalith.*` paths for this story.

### Anti-Patterns To Avoid

- Do not implement timeout/stall by sleeping in tests or depending on wall-clock delays.
- Do not report a timeout or stall as `Confirmed`.
- Do not add per-request callbacks that ignore filters, sort, search, or requested page bounds.
- Do not keep "FaultInjection" naming if the type only records evidence and docs continue to claim injection.
- Do not update `PublicAPI.Shipped.txt` mechanically without reviewing the new public signatures.
- Do not weaken Story 10.5 redaction tests to make new evidence pass.
- Do not route Testing redaction through CLI `OutputSanitizer` unless a proven limitation in `RedactedEvidenceFormatter` requires it.

### Testing Requirements

Minimum focused lanes:

```bash
DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-restore -m:1 /nr:false
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false
```

If local VSTest socket creation is blocked, use the existing xUnit v3 in-process runner after building:

```bash
DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -noLogo
```

Public/package boundary gates:

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --filter "FullyQualifiedName~PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline|FullyQualifiedName~PackageBoundaryTests.Pack_TestingPackage_IncludesOnlyIntentionalAssets|FullyQualifiedName~PackageBoundaryTests.CleanConsumer_RestoresAndBuilds_FromLocalPackages" -m:1 /nr:false
```

Required story artifact gate before review:

```bash
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md
```

Required broad lane when feasible:

```bash
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
dotnet build Hexalith.FrontComposer.slnx -c Release
git diff --check
```

If local VSTest sockets, restore, package feed, browser, vulnerability feed, or environment permissions block validation, record the exact command, exact failure, and focused fallback evidence. Do not mark validation complete without evidence.

### Latest Technical Information

- Microsoft Learn's ASP.NET Core Blazor testing guidance distinguishes Razor component unit testing from E2E testing and states that unit tests mock external dependencies such as services and JS interop. This supports keeping the Testing package fake-driven and hostless. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0
- bUnit's `BunitContext` API exposes `Services` as the service collection/provider used when rendering components, and `AddAuthorization()` adds Blazor authentication/authorization test services. Source: https://bunit.dev/api/Bunit.BunitContext.html
- bUnit's JSInterop documentation confirms strict and loose modes for JS interop handling. The current Testing package default is loose mode through `FrontComposerTestOptions.JSInteropMode`; keep that behavior unless a test requires strictness explicitly. Source: https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html
- Microsoft Learn's `Task.ConfigureAwait` / CA2007 guidance calls out deadlock risk from captured contexts and recommends `ConfigureAwait(false)` for reusable library code where the continuation does not need the caller context. This supports replacing `GetAwaiter().GetResult()` with an async factory path and using `ConfigureAwait(false)` in Testing package awaits. Sources: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.configureawait?view=net-10.0 and https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007

### Previous Story Intelligence

Story 11.5 was a confirm-and-pin story: current code already satisfied much of the requirement, but the dev pass still verified focused lanes, browser/evidence lanes, Release build, broad solution tests, diff check, and story validation before moving to review. Apply the same evidence bar here, especially because Story 11.6 touches a publishable package surface.

Story 10.5 found that Testing evidence privacy must cover tenant/user values, property names, dictionary keys, token/secret/password keyed values, and bounded diagnostic payloads. If Story 11.6 adds new evidence shapes, extend that matrix instead of relying on the old command-payload-only tests.

Story 7.5 established the Testing host contract and public API baseline. It also documented the old fault provider as an evidence recorder, so this story must explicitly update that contract if behavior or naming changes.

### Git Intelligence

Current baseline at story creation:

- `a783c82bf9cdfbe1347c23ac6f8e699ae540eef4` (`a783c82 chore(deps): bump EventStore and Parties submodules`)
- Recent history includes Story 11.5 moved to review in `289bb09`.
- `git status --short` was clean immediately before this Story 11.6 artifact was created.

### Documented Unrelated Changes

No unrelated dirty worktree changes were present at story creation. If new unrelated changes appear during implementation, document and preserve them rather than reverting.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md`.
- Sprint-status key: `11-6-testing-harness-failure-modes`.
- Primary implementation area: `src/Hexalith.FrontComposer.Testing/`.
- Primary test area: `tests/Hexalith.FrontComposer.Testing.Tests/`.
- Supporting Shell/Contracts reference area: command communication contracts, lifecycle state, projection page loader, and Shell authorization contracts listed above.
- Avoid generated output, `obj/**`, package version files, release-gate artifacts, CI-alignment artifacts, deferred-work historical rows for older "11.6" contexts, and submodule paths unless the user explicitly redirects the work.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 11 source of record, Story 11.6 acceptance criteria, and authoritative implementation order.
- Source: `_bmad-output/planning-artifacts/prd.md` - UJ-6, SM-5, FR-22, A1, and release-hardening context.
- Source: `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md` - post-readiness remediation context.
- Source: `_bmad-output/planning-artifacts/architecture.md` - Testing harness failure-mode remediation listing.
- Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` - M21 finding and prioritized roadmap.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md` - original Story 11.6 framing.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e10r-ai-3-testing-privacy-into-11-6.md` - privacy acceptance criterion refinement.
- Source: `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` - current Testing host, fake, fault recorder, redaction, and public API contract.
- Source: `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md` - original Testing package story and public API lessons.
- Source: `_bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md` - redaction evidence lessons.
- Source: `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md` - previous story evidence bar and workflow lessons.
- Source: `_bmad-output/project-context.md` - project stack, coding, testing, docs, and submodule rules.
- Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` - repository-wide LLM instructions.
- Source: Microsoft Learn Blazor testing - https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0
- Source: bUnit `BunitContext` API - https://bunit.dev/api/Bunit.BunitContext.html
- Source: bUnit JSInterop test double docs - https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html
- Source: Microsoft Learn `Task.ConfigureAwait` - https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.configureawait?view=net-10.0
- Source: Microsoft Learn CA2007 - https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-09: Create-story analysis loaded root AGENTS instructions, Hexalith LLM instructions, BMAD create-story workflow/config/discovery/template/checklist, project context, root-declared submodule project-context files, sprint status, Epic 11 source, PRD/addendum, architecture, UX artifacts, architecture quality review, Testing host contract, prior Stories 7.5, 10.5, and 11.5, live Testing package source/tests/docs, Counter authorization specimen files, Shell authorization contracts, command/query/page contracts, current public API baseline, current git baseline, and current external Blazor/bUnit/ConfigureAwait docs.
- 2026-07-09: Confirmed no standard Story 11.6 artifact existed before creation.
- 2026-07-09: Confirmed `11-6-testing-harness-failure-modes` was the next authoritative backlog story in `sprint-status.yaml`; Epic 11 was already `in-progress`.
- 2026-07-09: Current code inspection found Story 11.6 requires real Testing package changes: command fake failure modes, query/page per-request callbacks, fault-provider contract resolution, auth fake states, async host factory, direct fake/builder/assertion tests, docs, public API baseline, and redaction preservation.

### Implementation Plan

- Expand the Testing fakes and host setup while preserving existing default happy-path behavior.
- Add direct tests first or alongside each public API change so the Testing package surface is pinned intentionally.
- Update docs, host contract, README/how-to, and public API baseline only for behavior that actually changes.
- Validate focused Testing lanes, package/public API gates, broad solution lane when feasible, Release build, diff check, and story artifact validation before review.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-09.
- Story status set to `ready-for-dev`.
- Sprint status updated so Story 11.6 is `ready-for-dev`.
- No source code was changed by story creation.

### Change Log

- 2026-07-09: Created Story 11.6 ready-for-dev artifact and moved sprint tracking from `backlog` to `ready-for-dev`.

## Spec Change Log

### 2026-07-11 — Review-driven re-derivation constraints

- Triggering findings: the first implementation covered fake-level happy and failure paths but did not satisfy the specified adopter/generated-component authorization surface, required direct/privacy/edge-path coverage, honest fault-recorder API contract, or robust callback/async-host failure semantics.
- Amendment: implementation must preserve the successful command reject/timeout/stall vocabulary, request callbacks, authorization DI seam, async initialization direction, public API baseline discipline, and focused green test lane while also satisfying every original task and acceptance criterion.
- Known-bad state to avoid: do not leave synchronous `AddFrontComposerTestHost(...)` silently ignoring `DuringHostSetup`; do not leak culture scope when async initialization fails or hold unsafe culture mutation across an await; do not allow stale callbacks to override later static/not-modified configuration; do not omit evidence for callback failures; do not derive command IDs from bounded evidence count or reread mutable outcomes during a dispatch; do not treat unknown/missing/unauthenticated authorization as allowed by default; do not retain injection-oriented public naming for a recorder-only provider; and do not claim completion with only direct fake tests.
- KEEP: deterministic command lifecycle outcomes without wall-clock sleeps; callback request objects carrying all query/page inputs; a host-exposed authorization fake registered as the exact DI instance; no sync-over-async; existing redaction formatter reuse; intentional public API and documentation updates; zero-warning Release build and focused Testing suite.

## Review Triage Log

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 12: (high 6, medium 6, low 0)
- patch: 0
- defer: 0
- reject: 9
- addressed_findings:
  - `[high]` `[bad_spec]` Re-derive authorization support through the host and a real policy-gated/generated consumer, including allowed, denied, pending, missing-policy, unauthenticated, and handler-failed states.
  - `[high]` `[bad_spec]` Preserve synchronous host semantics or make the compatibility behavior explicit while providing exception-safe, isolation-safe async initialization without sync-over-async.
  - `[high]` `[bad_spec]` Complete direct builders, assertions, fakes, fault-recorder, privacy, not-modified, empty, cancellation, paging, filtering, sorting, and search verification required by the story.
  - `[high]` `[bad_spec]` Align the fault evidence recorder's adopter-facing names, documentation, tests, and public API rather than changing prose alone.
  - `[high]` `[bad_spec]` Make callback configuration last-write-wins and capture bounded redacted request evidence even when configured callbacks fail.
  - `[high]` `[bad_spec]` Make command IDs monotonic per host and snapshot configured outcome/rejection data once per dispatch.
  - `[medium]` `[bad_spec]` Fail fast on invalid null callback results and pin consistent evidence status vocabulary.
  - `[medium]` `[bad_spec]` Update the authoritative host contract comprehensively for authorization wiring, async setup, recorder naming, and the complete public API delta.

### 2026-07-11 — Review pass 2
- intent_gap: 0
- bad_spec: 0
- patch: 15: (high 3, medium 10, low 2)
- defer: 0
- reject: 8
- addressed_findings:
  - `[high]` `[patch]` Redacted configured tenant/user identifiers in fault evidence while preserving plain correlation identifiers, with direct privacy assertions.
  - `[high]` `[patch]` Added rendered authorization coverage for denied, pending, missing-policy, unauthenticated, and handler-failed states and normalized callback exceptions fail-closed.
  - `[high]` `[patch]` Made query and page configuration atomic and truly last-write-wins under concurrent configuration.
  - `[medium]` `[patch]` Preserved cancellation semantics for callback-thrown cancellation and validated projection-page inputs.
  - `[medium]` `[patch]` Made command configuration snapshots thread-safe and retained evidence when lifecycle callbacks fail.
  - `[medium]` `[patch]` Added synchronous DuringHostSetup fail-fast verification, rejection-resolution verification, async cancellation, and consistent inheritance guidance.
  - `[medium]` `[patch]` Rejected contradictory blocked authorization reasons and avoided mutating registered options after composition.

### File List

- `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md`
- `docs/how-to/test-generated-components.md`
- `src/Hexalith.FrontComposer.Testing/Evidence.cs`
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestBase.cs`
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestHostBuilder.cs`
- `src/Hexalith.FrontComposer.Testing/ProjectionPageRequest.cs`
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`
- `src/Hexalith.FrontComposer.Testing/README.md`
- `src/Hexalith.FrontComposer.Testing/TestAuthorizationEvaluator.cs`
- `src/Hexalith.FrontComposer.Testing/TestCommandConfiguration.cs`
- `src/Hexalith.FrontComposer.Testing/TestCommandOutcome.cs`
- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs`
- `src/Hexalith.FrontComposer.Testing/TestFaultInjectionProvider.cs` (removed)
- `src/Hexalith.FrontComposer.Testing/TestFaultEvidenceRecorder.cs`
- `src/Hexalith.FrontComposer.Testing/TestProjectionPageConfiguration.cs`
- `src/Hexalith.FrontComposer.Testing/TestProjectionPageLoader.cs`
- `src/Hexalith.FrontComposer.Testing/TestQueryConfiguration.cs`
- `src/Hexalith.FrontComposer.Testing/TestQueryService.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs`

## Auto Run Result

Status: done

Summary: Hardened the adopter-facing Testing harness with deterministic command rejection, timeout, and stall outcomes; atomic request-sensitive query/page callbacks; fail-closed authorization states exercised through rendered policy regions; async host initialization; honestly named fault evidence recording; privacy-preserving evidence; direct surface tests; documentation; and intentional public API updates.

Review findings: first-pass specification re-derivation addressed 12 material alignment findings; second-pass review applied 15 localized patches; no deferred work remains; noise and non-actionable theoretical findings were rejected.

Follow-up review recommendation: true, because the review-driven patches span concurrency, cancellation, authorization, privacy, public API, and behavioral compatibility.

Verification: focused Testing Release build passed with zero warnings; focused Testing suite passed 46/46; xUnit executable fallback passed 46/46; public API/package/clean-consumer gates passed within the focused suite; broad filtered solution tests and full Release solution build passed in the implementation handoff; `git diff --check` passed. Story artifact validation remained sensitive to the story's historical baseline and unrelated post-baseline repository changes.

Residual risk: async initialization failure cleanup is implemented, but directly injecting an `IStore.InitializeAsync` failure remains constrained by bUnit service-provider locking. Deterministic sequences intentionally remain monotonic across Reset to avoid concurrent ID reuse.
