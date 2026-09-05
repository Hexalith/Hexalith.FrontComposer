---
title: 'Dispose test-owned service providers'
type: 'chore'
created: '2026-09-05'
status: 'blocked'
baseline_revision: 'cb7633264032cdcea562b3e76d30734310cb4ddc'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** `BadgeCountServiceTests` and `NavigationEffectsLastActiveRouteTests` build concrete `ServiceProvider` instances but pass most of them inline or retain them through a non-disposable abstraction, so test-owned singleton and scoped resources do not have deterministic teardown.

**Approach:** Give every provider returned by the affected test helpers an explicit concrete `using ServiceProvider` local and pass that local to the existing system under test, preserving all behavior and assertions.

## Boundaries & Constraints

**Always:** Cover every `EmptyProvider`, `WithNotifier`, and local `BuildServiceProvider` call in the two named test classes. Declare the provider before a disposable system under test so reverse declaration-order teardown disposes the system under test first. Preserve existing test names, setup, actions, and assertions.

**Never:** Change production code, service registrations, product behavior, unrelated provider call sites, or the deferred-work ledger. Do not make a provider helper own disposal or make the production services dispose a caller-owned provider.

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs:48` -- `EmptyProvider` and `WithNotifier` return concrete providers; all helper results used throughout the class need test-scope ownership, including tests that manually call `BadgeCountService.DisposeAsync`.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs:47` -- the concrete provider helper has three inline consumers around lines 109, 188, and 216 that need explicit lifetime locals.
- `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:81` -- read-only ownership evidence: the constructor resolves optional services from `IServiceProvider`; `Dispose` releases its own subscriptions and resources, not the supplied provider.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs:51` -- read-only ownership evidence: the provider is a late-bound lookup input and the effect has no provider-disposal responsibility.
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj:1` -- owning xUnit v3/Microsoft.Testing.Platform test project for focused verification.
- `tests/README.md:20` -- repository test-runner contract and focused Shell project lane; VSTest filter syntax is not permitted.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs` -- materialize every helper-created provider as a concrete `using ServiceProvider` local before constructing `BadgeCountService`, including constructor-guard and manual-disposal tests, so every test deterministically releases provider-owned resources after the service is torn down.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs` -- materialize and dispose the provider in each of the three tests that supplies `NavigationManager`, then pass the local into `NavigationEffects` without changing the scenarios or assertions.

**Acceptance Criteria:**
- Given any test in either affected class that calls a provider-building helper, when the test scope exits normally or through an exception, then the concrete `ServiceProvider` is deterministically disposed by a `using` declaration.
- Given a `BadgeCountService` test with both a provider and a disposable service instance, when scope teardown occurs, then the service is disposed before its provider because the provider local is declared first.
- Given the two affected test classes, when they execute through the repository's xUnit v3 test runner, then all existing assertions pass unchanged.
- Given the completed diff, when its paths are inspected, then it contains only the two affected test files and this workflow specification; production code and `_bmad-output/implementation-artifacts/deferred-work.md` are unchanged.

## Spec Change Log

## Review Triage Log

### 2026-09-05 — Review pass
- verdicts: 22 findings — high 0, medium 8, low 1, false 10, maybe-false 3
- findings:
  - `[false]` `[reject]` Blind hunter: the spec's three-path change envelope conflicts with additional baseline-diff paths — `git diff --cached --name-only` contains only this spec and the two intended test files; the other paths arrived through concurrent commits or remain unrelated and unstaged, so this task did not violate its envelope.
  - `[medium]` `[patch]` Blind hunter: the two manual `DisposeAsync` tests lacked exception-safe service teardown before provider teardown — both `sut` locals now use idempotent `using BadgeCountService` fallbacks while retaining the explicit asynchronous disposal calls and assertions.
  - `[false]` `[reject]` Blind hunter: provider teardown requires a disposable-sentinel regression assertion — the ledger identifies missing provider disposal at call sites, and C# `using` provides the required exception-safe `Dispose` call without retesting language/runtime semantics or changing existing assertions.
  - `[false]` `[reject]` Blind hunter: the Verification section omits observed results and unrelated path checks — this workflow records observed results under Auto Run Result after review; both declared commands passed, and the extra baseline paths are outside the staged task delta.
  - `[medium]` `[reject]` Blind hunter: the baseline transition reopens DW-1041 and DW-1400 — the transition is real but belongs to concurrent orchestrator commits; the invocation explicitly forbids this task from editing the ledger, and neither the staged nor unstaged task delta touches it.
  - `[medium]` `[reject]` Blind hunter: the baseline transition removes six accepted orchestration decisions — the transition is real but belongs to concurrent orchestrator work in `.bmad-loop/decisions.json`, which is outside the explicitly test-only intent and absent from the task delta.
  - `[false]` `[reject]` Blind hunter: the Builds pointer is an unexplained harmful downgrade — commit `f93b4b627ca9fe282e76ca7bc9de6135ac2ad0e8` explicitly records a restore-breaking bump rollback, and the pointer change is committed concurrent work rather than this task.
  - `[false]` `[reject]` Blind hunter: the Memories pointer discards fixes without an approved reason — the same explicit restore-break rollback commit owns this concurrent pointer change; it is not part of the staged task delta.
  - `[maybe-false]` `[reject]` Blind hunter: literal `.nuget/packages/` matching may miss Windows or custom package roots — settling this requires evaluating the evolving UI asset graph on Windows and with a custom `NUGET_PACKAGES`; the invocation expressly limits this work to tests, and the UI file is unrelated concurrent work.
  - `[maybe-false]` `[reject]` Blind hunter: published MCP/SourceTools compile assets may be removed without equivalent source references — settling this requires tracing the evolving nested UI project graph and building consumers that use those APIs; the production UI work is explicitly outside this test-only task.
  - `[medium]` `[reject]` Blind hunter: the reviewed smoke evidence has a failed final verdict — the failure is real evidence for separate, actively changing production UI work, but this task changes no AppHost/UI surface and requires only the owning Shell test lane.
  - `[low]` `[reject]` Blind hunter: release-policy bookkeeping in diagnostics documentation can become stale — the committed documentation change is unrelated concurrent work expressly excluded by the test-only intent, so this task does not edit it.
  - `[medium]` `[patch]` Edge-case hunter: exceptions before explicit `DisposeAsync` could leave `BadgeCountService` live while its provider tears down — the same two manual-disposal sites now have `using BadgeCountService` fallbacks, preserving service-before-provider teardown on exceptional exits.
  - `[false]` `[reject]` Edge-case hunter: nested UI projects lack a propagated duplicate-assembly guard — the current concurrent UI work forwards `CustomAfterMicrosoftCommonTargets` and imports `DropPublishedFrontComposerAssemblies.targets`; the cited missing-guard state no longer exists and is not part of this task.
  - `[maybe-false]` `[reject]` Edge-case hunter: UI NuGet asset matching may be path-separator/package-root dependent — Windows plus custom-package-root MSBuild evidence would settle the claim; the cited production file is unrelated, concurrently changing work excluded by the intent.
  - `[medium]` `[reject]` Edge-case hunter: previously closed ledger entries appear open in the baseline transition — this duplicates the real but concurrent ledger observation; the task obeys the explicit no-ledger-edit boundary.
  - `[medium]` `[reject]` Edge-case hunter: accepted decisions disappear from the baseline transition — this duplicates the real but concurrent decisions-file observation; orchestration state is outside the test-only intent and task delta.
  - `[false]` `[reject]` Edge-case hunter: the task conceals production/governance changes behind a three-path claim — staged-path inspection proves those paths are concurrent commits or unrelated working changes, not reviewed files produced by this task.
  - `[false]` `[reject]` Intent auditor: the selected lifetime reading needs assertions that observe provider-owned disposable dependencies — the verbatim ledger makes the call-site disposal reading specific, `using ServiceProvider` supplies that lifetime guarantee, and the instruction explicitly preserves the existing assertions.
  - `[false]` `[reject]` Intent auditor: the task change envelope includes eleven paths and edits the forbidden ledger — only three paths are staged for this task; the baseline grew because the branch advanced concurrently, and the ledger is untouched by this run.
  - `[false]` `[reject]` Verification reviewer: the reviewed task contradicts its test-only scope contract — the finding conflates intervening commits and unrelated unstaged edits with the three-file staged task delta.
  - `[medium]` `[reject]` Verification reviewer: production project-graph work lacks passing live evidence — that separate evidence remains failed and is not claimed as verification for this test-only cleanup; no production project-graph file is part of the task delta.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` -- expected: the owning test project builds with recommended analyzers and warnings-as-errors.
- `dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter-class "*BadgeCountServiceTests" --filter-class "*NavigationEffectsLastActiveRouteTests"` -- expected: the affected xUnit v3 classes pass through Microsoft.Testing.Platform.

## Auto Run Result

Status: blocked
Blocking condition: finalization left repository dirty

### Summary

All providers created by the three affected test helpers now have concrete, exception-safe `using ServiceProvider` ownership. `BadgeCountService` instances are declared after their providers, including idempotent `using` fallbacks in the two tests that explicitly exercise `DisposeAsync`, so service teardown precedes provider teardown while every existing assertion remains intact.

### Files Changed

- `../../tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs` -- owns all 17 helper-created providers and guarantees service-first teardown.
- `../../tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs` -- owns the three navigation providers previously passed inline.
- `spec-test-provider-lifetime-cleanup.md` -- records intent, implementation map, review triage, and verification evidence.

### Review Findings

- Patches applied: 1 medium entry (reported twice), adding `using BadgeCountService` fallbacks to both manual-disposal tests.
- Items deferred: 0.
- Rejected findings:
  - Blind scope-envelope mismatch -- false because the staged task delta contains only the two tests and this spec; other baseline paths are concurrent commits or unrelated unstaged work.
  - Blind request for a disposable sentinel -- false because the ledger specifically requires call-site disposal, which C# `using` guarantees without changing existing assertions.
  - Blind missing observed verification -- false because observations are recorded in this result after the mandated review phase.
  - Blind ledger reopening -- rejected because it is real concurrent orchestrator work and the intent explicitly forbids ledger edits by this task.
  - Blind decisions removal -- rejected because it is concurrent orchestration state outside the test-only intent.
  - Blind Builds rollback -- false because commit `f93b4b627ca9fe282e76ca7bc9de6135ac2ad0e8` documents the restore-breaking bump rollback.
  - Blind Memories rollback -- false for the same documented concurrent rollback.
  - Blind NuGet path portability -- unverified and rejected because Windows/custom-package-root evidence belongs to unrelated, actively changing production UI work.
  - Blind MCP/SourceTools asset removal -- unverified and rejected because it requires tracing the unrelated nested UI graph.
  - Blind failed AppHost smoke -- rejected because it is real evidence for separate production work, not this Shell test cleanup.
  - Blind diagnostics-document staleness -- rejected as low-impact concurrent documentation work outside the explicit intent.
  - Edge nested-project guard -- false because the concurrent UI work now propagates `CustomAfterMicrosoftCommonTargets` and imports its drop target.
  - Edge NuGet path portability -- unverified duplicate rejected because it concerns excluded production UI work.
  - Edge ledger reopening -- rejected duplicate because the task leaves the orchestrator-owned ledger untouched.
  - Edge decisions removal -- rejected duplicate because the task does not own orchestration state.
  - Edge scope-envelope mismatch -- false duplicate because staged-path inspection isolates the three task files.
  - Intent request for disposal-observing assertions -- false because the verbatim ledger selects call-site disposal and requires preserving existing assertions.
  - Intent eleven-path envelope divergence -- false because it conflates concurrent branch advancement with this task's staged delta.
  - Verification scope contradiction -- false for the same staged-versus-concurrent distinction.
  - Verification failed live evidence -- rejected because production smoke evidence is not a verification surface for this test-only task.

### Follow-up Review Recommendation

`false` -- patched entries by verdict: high 0, medium 1, low 0. The single medium entry was directly corrected and covered by the rerun.

### Verification Performed

- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` -- passed with 0 warnings and 0 errors.
- `dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter-class "*BadgeCountServiceTests" --filter-class "*NavigationEffectsLastActiveRouteTests"` -- passed: 25 total, 25 succeeded, 0 failed, 0 skipped.
- `git diff --cached --check` -- passed before review; final staged validation is repeated during commit finalization.

### Residual Risks

No known residual risk remains in the provider-lifetime cleanup. Concurrent production UI/AppHost work is intentionally excluded and preserved.
