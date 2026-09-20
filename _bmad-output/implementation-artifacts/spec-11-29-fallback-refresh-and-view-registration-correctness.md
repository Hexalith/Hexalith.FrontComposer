---
title: 'Story 11.29: Fallback Refresh and View Registration Correctness'
type: 'bugfix'
created: '2026-09-19'
status: in-progress
baseline_commit: '257215600ee2cf3c921014d27c07b625fee7c59d'
baseline_revision: 5213552913b421a26b3f4822f02f0d9b98504ebb
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
warnings:
  - oversized
deferred: []
---

<intent-contract>

## Intent

**Problem:** Fallback reconciliation can miss material row changes when no ETag is returned, can leave an evicted visible page absent when a validator is reused, and can silently retain the first tenant/query contract registered under a reused `ViewKey`.

**Approach:** Give no-ETag responses a bounded canonical content signature, make missing reducer-page state independently material, and permit refcounted duplicate registrations only when their complete scope/query contracts are equivalent.

## Boundaries & Constraints

**Always:** Preserve the public `ProjectionFallbackLane` and scheduler interfaces, scoped circuit lifetime, deterministic lane ordering/budgets, identical-registration refcounting, disposal-safe cache cleanup, tenant isolation, and generated views' existing dispose-before-reregister behavior. Canonicalize object properties ordinally, preserve array order, bound signature work/material, and retain changed-versus-unchanged behavior without logging row or tenant data. Treat every applicable default-Shell-lane failure outside Story 11.29's authorized surfaces as an external prerequisite, regardless of which test names an earlier run recorded: every such failure must be repaired outside Story 11.29 before this story is re-armed or resumed, and the applicable default Shell lane must then pass in full.

**Never:** Treat row count or reference identity as content equality; let an equal/304 validator suppress rebuilding a required missing `(ViewKey, Skip)` page; mutate an incumbent lane after rejecting a conflicting registration; silently replace a live owner; expose scope/query values in conflict errors; change generator output, public contracts, `sprint-status.yaml`, or unrelated deferred work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| No-ETag material change | Equal totals/counts, different row values | Canonical digest changes; success action is dispatched and the lane reports changed | Serialization is bounded and failure remains fail-safe without leaking payloads |
| No-ETag logical repeat | Distinct objects with equivalent canonical values | Digest is stable; no duplicate success action | Property order does not affect the digest |
| Reused validator, missing page | Equal ETag or `IsNotModified`, required reducer page absent | Returned/cached page is dispatched, including an empty page | Rebuild is reported as changed |
| Equivalent duplicate registration | Same `ViewKey` and complete lane contract | One lane is refreshed; ownership is refcounted until the last disposal | Earlier disposal cannot remove the remaining owner |
| Conflicting duplicate registration | Same `ViewKey`, different tenant or query/callback contract | Registration is rejected and incumbent remains active | Throw a bounded, payload-free `InvalidOperationException`; allow the new contract after final prior disposal |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs:28` -- lane ownership, registration/refcount disposal, validator/signature state, refresh classification, reducer-page detection, and dispatch policy.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRowSignature.cs` -- new internal helper for fixed-size deterministic digests over bounded, ordinally canonicalized row JSON using the Shell JSON profile.
- `src/Hexalith.FrontComposer.Shell/Services/FcJson.cs:10` -- existing canonical immutable JSON options; reuse rather than creating another `JsonSerializerOptions` holder.
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackLane.cs:11` -- read-only complete registration contract: view, projection, tenant, page window, filters, sort, search, and optional callback.
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageState.cs:32` -- reducer-page presence source keyed by `(ViewKey, Skip)`.
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs:156` -- read-only evidence that `LoadPageNotModifiedAction` cannot reconstruct an absent page.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1484` -- read-only evidence that generated views already dispose an old lane before registering a changed local query contract.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:98` -- governed CA1707 test-identifier seal and source-suppression inventory that must track the new regression methods and narrow row-serialization annotations.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs:220` -- focused ETag, no-ETag, reconciliation, registration, and concurrency regression surface.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRowSignature.cs` -- implement a fixed-size digest over totals/counts and bounded canonical row content, with ordinal object-property ordering, preserved array order, and deterministic safe handling of limits/serialization failures.
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs` -- use value signatures for no-ETag classification; dispatch page success whenever visible reducer state is missing, including equal/304 validators and empty pages; atomically refcount only equivalent lane contracts and reject live conflicts without mutating the incumbent.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs` -- add deterministic changed/unchanged row, missing-page validator, equivalent-refcount, tenant/query conflict, incumbent-retention, disposal, and no-duplicate-work fixtures covering the matrix.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseal the approved test-identifier inventory for the new public test methods and register the narrowly justified trimming/AOT suppressions without weakening analyzer policy.

**Acceptance Criteria:**
- Given deterministic fixtures for all matrix cases, when the focused scheduler class and applicable default Shell lane run, then equal-count content changes and missing reducer pages dispatch exactly when material, equivalent content and registrations do no duplicate work, and conflicting live scope/query registrations cannot leak across tenants.
- Given the completed implementation, when the Release solution build runs, then public/API, analyzer, nullable, warnings-as-errors, and generated-output contracts remain green without generator or snapshot drift.
- Given any applicable default-Shell-lane failure outside Story 11.29's authorized surfaces, when Story 11.29 is re-armed and verification resumes, then every such failure has already been repaired outside this story and the applicable default Shell lane passes in full; no historical test-name list limits the prerequisite set, and neither a baseline waiver nor a Story 11.29 change to dependency-governance or other unrelated surfaces satisfies this criterion.

## Spec Change Log

- 2026-09-19: Clarified that the two pre-existing default-Shell-lane failures are mandatory external prerequisites, not work authorized by Story 11.29 and not waivable baseline failures.
- 2026-09-20: Generalized the prerequisite rule to every out-of-scope default-Shell-lane failure so changing external failure names cannot stale or narrow the full-lane gate.

## Review Triage Log

## Design Notes

Registration comparison covers every `ProjectionFallbackLane` field: ordinal `ViewKey`, projection, tenant, sort, and search; numeric page window; sort direction; ordinal filter key/value equality independent of enumeration order; and callback identity. Serialize registration/decrement mutations under a narrow gate so comparison/refcount/removal are atomic and dictionary update factories cannot repeat side effects. Final disposal removes only its owned entry and associated ETag/content-signature state.

The row helper returns a digest rather than retaining canonical payload. Its explicit row/byte/depth limits and deterministic limit/error markers keep allocation and stored material bounded; it never logs or exposes row content.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: focused test assembly builds with zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.ProjectionConnection.ProjectionFallbackRefreshSchedulerTests` -- expected: all focused fixtures pass without skips or duplicate refresh work.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -noLogo -noColor -parallel none -notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined` -- expected: applicable default Shell lane passes.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: solution build passes with zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors.
