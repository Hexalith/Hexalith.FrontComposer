---
title: 'Dispose test-owned service providers'
type: 'chore'
created: '2026-09-05'
status: ready-for-dev
baseline_revision: 092240002f55f7fbacaef017b91d752d8ca10fe3
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

**Always:** Cover every `EmptyProvider`, `WithNotifier`, and local `BuildServiceProvider` call in the two named test classes. Declare the provider before a disposable system under test so reverse declaration-order teardown disposes the system under test first. Preserve existing test names, setup, actions, and assertions. For finalization, evaluate scope against the story-owned staged or committed delta rather than repository-wide cleanliness. Repository-wide dirtiness alone is never a failure: classify every changed path, finalize when the story-owned changes are staged or committed as intended, and preserve and ignore ambient changes only when they are path-disjoint from the three owned files and their outside-story provenance is clear. Block finalization only when a changed path overlaps an owned file outside the intended story delta or ownership cannot be separated.

**Never:** Change production code, service registrations, product behavior, unrelated provider call sites, or the deferred-work ledger. Do not make a provider helper own disposal or make the production services dispose a caller-owned provider. Do not clean, stage, commit, or absorb ambient changes into this story merely to satisfy finalization.

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
- Given the completed story-owned delta, when its paths are inspected, then it contains only the two affected test files and this workflow specification.
- Given a dirty working tree at finalization, when every changed path is classified against the story-owned delta, then repository-wide dirtiness alone does not block finalization; path-disjoint ambient changes with clearly separate provenance remain untouched, while changes that overlap an owned file outside the intended story delta or cannot be partitioned by ownership block finalization.

## Spec Change Log

- 2026-09-05: Clarified that finalization is story-scoped and repository-wide dirtiness alone is not a blocking condition.

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

