---
title: 'Story 11.29: Fallback Refresh and View Registration Correctness'
type: 'bugfix'
created: '2026-09-19'
status: done
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

### Review Findings

Chunk 1 review (2026-09-20). Layers: Blind Hunter, Edge Case Hunter. Failed/empty: Acceptance Auditor, Verification Gap Reviewer.

- [x] [Review][Patch] Do not stamp a validator on present-page 304 when the scheduler has none [ProjectionFallbackRefreshScheduler.cs:328] — Keep `TryDispatchPageNotModified` (no flash). Record validator state only after a missing-page 304 success, or when a prior ETag/signature already exists. A successor 304 on leftover rows must not lock in the new owner so a later same-ETag 200 is still a first observation.
- [x] [Review][Defer] In-flight replay is not bounded to depth 1 [ProjectionFallbackRefreshScheduler.cs:259] — deferred: pre-existing P1/P24 retry orchestration; `_inFlight` is cleared before the recursive replay, so a nudge during replay can recurse again. Story 11.29's prior triage already carried this as EC-02/RBH-02.
- [x] [Review][Defer] HasReducerPage can go stale before dispatch [ProjectionFallbackRefreshScheduler.cs:320] — deferred: maybe-false; would be medium if a concurrent eviction between the unsynchronized sample and `TryDispatch*` were shown. Settled by a fixture that evicts `(ViewKey, Skip)` after `HasReducerPage` returns and before dispatch, or by re-reading presence under `DispatchGate`.
- [x] [Review][Defer] Fallback success completes in-flight grid TCS [ProjectionFallbackRefreshScheduler.cs:594] — deferred: pre-existing; `LoadPageSucceededAction` is dispatched with null `Completion`, so the reducer `TrySetResult`s any pending TCS for that key. Changing that requires the LoadedPage reducer contract, which this story must not alter.

#### Rejected

- BH spec Verification uses `dotnet <dll> -class` / `-notrait` and omits `Category=GovernanceBuild` — reject: the fix is to edit this spec; Hexalith fallback validation still documents assembly `-class` invocation.
- BH `GetActiveLane` is unused — false: the private helper is never invoked and produces no user- or developer-facing defect.
- BH four-argument constructor always sees an empty `StaticLoadedPageState` — false: production DI uses the primary constructor with Fluxor `IState<LoadedPageState>`; remaining 4-arg callers are tests that either use `RefreshAsync` or do not assert missing-page classification.
- BH reconciliation `BuildDedupeKey` omits `ViewKey` — false: P28 still coalesces identical query fingerprints on purpose; generated views use one `_viewKey` per type and refcount rather than dual-key the same query.
- BH intent-contract I/O matrix omits later triage patches — reject: the fix is to edit this spec.
- BH row-signature markers/`isReliable` lack direct assertions — false: depth-33 fails at `JsonDocument.Parse` (marker 0x03) before the walk fuse; the 11-step fixture already proves fail-safe `Changed` for bound and serialization failures.
- BH JSON numbers `1` and `1.0` produce different digests — false: same CLR DTOs serialize one number form; conservative mismatch is the specified fail-safe, not a missed material change.
- BH several facts bundle multiple contracts — false: xUnit still reports the failing assertion line, so a bundled fact does not hide which poll or registration rule failed.
- BH `review_loop_iteration` remains 0 and the Code Map still cites line 28 — reject: the fix is to edit this spec.
- BH custom `RefreshAsync` never enters `ClassifyRefreshResult` — false: generated views register without a callback; `MapCustomOutcome` is the adopter-owned bypass and must not double-dispatch loader classification.
- BH `NotSupportedException` and `IOException` share marker 0x04 — low: both paths are `isReliable=false` and already dispatch `Changed`; splitting markers adds branches with no user-visible miss.
- EC lane disposed after successful dispatch reports `Skipped` — false: the check uses the starting `LaneEntry`; after final disposal no live owner should be listed in `ChangedViewKeys`, and Fluxor already received the page.
- EC cancellation drops a pending retry token — low: teardown cancellation should not replay; the next poll or nudge covers a lost in-flight nudge, and persisting retry across cancel adds complexity.

## Spec Change Log

- 2026-09-19: Clarified that the two pre-existing default-Shell-lane failures are mandatory external prerequisites, not work authorized by Story 11.29 and not waivable baseline failures.
- 2026-09-20: Generalized the prerequisite rule to every out-of-scope default-Shell-lane failure so changing external failure names cannot stale or narrow the full-lane gate.

## Review Triage Log

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 | false | reject | The uploaded artifact is explicitly named and consumed as an unapproved capture candidate; later validation failure cannot turn it into an approved runtime identity. |
| BH-02 | false | reject | Successful writer activation assigns `eventstore_base` and immediately breaks the health loop, so the code does not post activation again after a successful response. |
| BH-03 | false | reject | Same-view accepted writes are serialized by the scheduler's in-flight key or rejected by the reducer's matching-completion guard; the review did not identify a reachable stale overwrite that survives those guards. |
| BH-04 | medium | patch | `IsNotModified` was evaluated before the negative-count protocol guard; classification now rejects a negative total before every validator branch and a regression covers malformed 304 input. |
| BH-05 | medium | patch | A runtime-null `Items` value could previously dispatch success and report `Changed`; classification now fails closed before dispatch and the malformed-result regression covers this path. |
| BH-06 | medium | patch | A successful 304 rebuild did not seed scheduler validator state, so the same subsequent 200 looked new; validator/signature state is now recorded after a successful 304 dispatch and the 304-to-200 regression proves de-duplication. |
| BH-07 | false | reject | Conservative `Changed` for content outside the explicit row/byte/depth bounds is the specified fail-safe behavior; treating a truncated digest as reliable would miss material changes beyond the bound. |
| BH-08 | medium | patch | Dispatch initially held the global registration gate; dispatch/disposal are now serialized by a per-entry gate while the global gate is held only for the active-owner check. |
| BH-09 | false | reject | `_bounded_read` deliberately fails closed when no no-follow primitive exists, and the evidence validator runs in the Ubuntu quality job; it does not perform an unsafe Windows read. |
| BH-10 | false | reject | Candidate assembly is a byte-copy operation whose documented caller validates live evidence immediately before assembly and validates the candidate afterward; the candidate is never treated as approval. |
| BH-11 | low | reject | A failed write can leave a runner-temp candidate directory, but each CI attempt gets a fresh runner and there is no in-process retry; atomic-directory construction is unrelated complexity for this fallback-refresh intent. |
| BH-12 | maybe-false | reject | The claim concerns a separate evidence-capture diagnostic; no reachable subprocess output containing Basic or opaque bearer authorization was demonstrated, and this intent explicitly excludes that release-governance surface. |
| BH-13 | maybe-false | reject | The claimed Story 11.25 tuple-authority mismatch belongs to the independent successor-identity history in the cumulative baseline range, not fallback refresh; this story must not amend that frozen record. |
| BH-14 | medium | reject | Story 11.25's spec/sprint completion mismatch is visible but belongs to independent release-evidence work in the cumulative baseline range; Story 11.29 explicitly forbids unrelated artifact/deferred-work changes. |
| BH-15 | false | reject | `sprint-status.yaml` is clean in the active working tree; the baseline-to-HEAD range includes an independently landed status commit, while this implementation did not modify the prohibited file. |
| BH-16 | medium | reject | The manual-dispatch input mismatch is in an independently landed quality-workflow change and is excluded by the fallback-refresh intent; no workflow edit or unrelated deferred-work mutation is authorized here. |
| BH-17 | low | reject | The stale `active v2 evidence` comment is cosmetic text in unrelated release governance and has no fallback-refresh runtime effect. |
| EC-01 | medium | patch | Same root cause as BH-06; 304 validator continuity is now recorded after successful dispatch and tested. |
| EC-02 | medium | reject | Replay recursion predates this story and requires sustained nudges during every awaited replay; changing retry orchestration is outside the approved correctness surfaces and preservation boundary. |
| EC-03 | high | reject | The parent-directory symlink race is in independent evidence tooling from the cumulative baseline range; the fallback-refresh intent and Never rules prohibit modifying or recording unrelated governance work here. |
| EC-04 | maybe-false | reject | Same independent diagnostic-redaction claim as BH-12; no reachable leaking subprocess output was demonstrated for this story. |
| EC-05 | false | reject | Same specified conservative-bound behavior as BH-07; repeated dispatch is intentional when equality cannot be proven within the bounded signature budget. |
| EC-06 | maybe-false | reject | The review did not establish Fluxor action ordering that evicts the page between the state sample and queued dispatch; deterministic absent-page cases are covered, and solving cross-queue linearizability would require an unauthorized action/reducer contract change. |
| VG-01 | medium | patch | No test combined a cached no-ETag signature with later page eviction; the existing no-ETag repeat fixture now evicts the reducer page and proves success dispatch plus `Changed`. |
| VG-02 | medium | patch | The 16 KiB row and depth-32 JSON bounds lacked execution proof; the bounded-signature fixture now exercises repeated oversized and over-depth rows and proves fail-safe dispatch without exception propagation. |
| RBH-01 | medium | patch | A refresh that dispatches `Changed` can be followed by a pending replay that returns `NotModified`; `StrongestOutcome` now preserves the material result, and the coordinated ETag fixture proves the reconciliation report remains changed. |
| RBH-02 | medium | reject | carried: sustained nudges can extend the recursive replay chain beyond the claimed depth-one bound, but EC-02 already records this pre-existing retry-orchestration issue as excluded by the approved intent. |
| RBH-03 | medium | patch | A pending retry was keyed only by `ViewKey`; retry state now carries its `LaneEntry` generation, rejects a stale owner's token, and retains a request made by the active replacement owner. |
| RBH-04 | medium | patch | ETag/signature state previously advanced before dispatcher success; dispatch helpers now commit it only after dispatch returns, and a throw-once regression proves the identical retry is not suppressed. |
| RBH-05 | medium | reject | `SetReconciliationGroupHealth` can expose a partial `ConcurrentDictionary` snapshot during `Clear` plus refill, but that behavior predates Story 11.29 and the intent excludes unrelated scheduler orchestration changes. |
| RBH-06 | false | reject | The quality workflow deliberately performs complementary AppHost-root and provider-root live-validation invocations; each validates the other packet semantically, and the gate reaches success only after both byte authorities have been recomputed. |
| RBH-07 | medium | reject | AppHost reevaluation can capture an unbounded MSBuild JSON stream, but this is cumulative release-evidence tooling outside the fallback-refresh intent and its explicit prohibition on dependency-governance changes. |
| RBH-08 | high | reject | The package-ledger CLI can prune an externally supplied non-fresh package root even though the smoke caller first enforces freshness; this destructive-tooling concern is real but belongs to cumulative release governance explicitly excluded from Story 11.29. |
| RBH-09 | medium | reject | Streaming package/runtime hashes do not revalidate descriptor identity and timestamps after reading, leaving a same-size mutation race; the affected evidence tooling is explicitly outside this story's intent. |
| RBH-10 | false | reject | carried: candidate assembly copies bounded snapshots, the documented caller validates the live evidence immediately beforehand, and the later contract-artifact gate validates the candidate inputs; BH-10 already records that the directory itself is never approval. |
| RBH-11 | low | reject | The workflow publishes a deliberately named unapproved successor candidate before the later approval-preparation gate; the artifact is never represented as approved, so moving upload would not correct a user-visible Story 11.29 defect. |
| RBH-12 | medium | reject | Writer-protocol invalid-bearer activation is exercised but not added to the durable authorization-control schema; that independent release-evidence enhancement is explicitly excluded from this fallback-refresh story. |
| RBH-13 | medium | reject | Package inventory traversal lacks incremental file-count and aggregate-work limits before the final ledger-size check, but the cumulative evidence implementation is outside the approved intent. |
| REC-01 | maybe-false | reject | carried: EC-06 already records that no reachable Fluxor ordering was established for eviction between the synchronous page-state sample and dispatch, and full cross-queue linearization would require an excluded action/reducer contract change. |
| REC-02 | medium | patch | Same root cause as RBH-03: pending retry state now binds and validates the requested `LaneEntry` generation before replay. |
| REC-03 | medium | patch | Same root cause as RBH-04: cached validators now advance only after dispatch completion. |
| REC-04 | low | reject | More than `Int32.MaxValue` simultaneous equivalent registrations could overflow the refcount, but that is not an everyday reachable component lifetime and adding an overflow guard is disproportionate branch complexity. |
| REC-05 | medium | reject | Deep evidence JSON can escape the current parser as `RecursionError`, but this finding is in independent governance validation explicitly excluded by the Story 11.29 intent. |
| REC-06 | low | reject | carried: BH-11 already records that a failed candidate write can leave a runner-temporary partial directory, but each CI attempt receives a fresh directory and atomic-directory construction is unrelated to this intent. |
| REC-07 | medium | reject | The smoke command wrapper bounds retained text only after `capture_output=True` has buffered the full process output; this release-evidence resource bound is outside the fallback-refresh intent. |
| REC-08 | medium | reject | AppHost capture writes compact evidence without rejecting a payload over the validator's 1 MiB bound, so later validation can fail after capture success; that independent evidence concern is excluded from this story. |
| RVG-01 | medium | patch | The no-ETag changed-row fixture now keeps visible rows identical while changing only `TotalCount` and proves success dispatch plus `Changed`. |
| RVG-02 | medium | patch | The contract-difference fixture now uses equal filter keys/counts with a different value and proves conflict rejection. |
| RVG-03 | medium | patch | The final-disposal fixture now reuses the same ETag under a replacement contract and proves it is treated as the new owner's first observation. |
| RVG-04 | medium | reject | The AppHost build-suppression regression is source-text based rather than executable metadata coverage, but that cumulative AppHost change is outside the fallback-refresh intent and must not expand Story 11.29 surfaces. |
| R2-BH-01 | medium | reject | carried: EC-02/RBH-02 already establish that sustained nudges can extend replay recursion; the behavior predates this story and retry-orchestration changes are outside the approved intent. |
| R2-BH-02 | maybe-false | reject | carried: EC-06/REC-01 already establish that no reachable Fluxor ordering has shown eviction between the synchronous presence sample and dispatch; proving it would require the deterministic interleaving fixture recorded there. |
| R2-BH-03 | false | reject | carried: BH-07/EC-05 already establish that repeated `Changed` results outside the explicit row, byte, and depth bounds are the specified fail-safe, not a missed material change. |
| R2-BH-04 | medium | reject | `graph --commit` does load its default policy from the worktree while `validate --commit` loads it from the selected commit, but that independent dependency-governance defect is already recorded outside this story and the intent forbids changing unrelated governance surfaces. |
| R2-BH-05 | false | reject | carried: RBH-06 already establishes that the quality workflow intentionally performs complementary AppHost-root and provider-root invocations; each validates both packets semantically and the pair recomputes both byte authorities. |
| R2-BH-06 | medium | reject | Predecessor validation can still invoke live provider and AppHost validation against current inputs when `require_current_match` is false, but that successor-governance behavior is independent committed history explicitly excluded by this fallback-refresh intent. |
| R2-BH-07 | medium | reject | Parsed JSON is traversed only after the raw serialized form matches a redaction pattern, so JSON escaping can avoid scalar/key scanning; this is real independent release-evidence work that the intent explicitly forbids adding to Story 11.29. |
| R2-BH-08 | maybe-false | reject | carried: BH-12/EC-04 already note that `_safe_process_diagnostic` omits raw-Authorization checks, but no reachable subprocess output containing Basic or opaque bearer authorization was demonstrated and the affected governance surface is excluded. |
| R2-BH-09 | false | reject | carried: BH-09 already establishes that `_bounded_read` deliberately fails closed without `O_NOFOLLOW` and the enforcing quality lane runs on Ubuntu; the code does not perform an unsafe Windows read. |
| R2-BH-10 | medium | reject | Child directory symlinks are skipped by the `is_dir()` branch before the later file-binding checks, but this runtime-evidence closure belongs to independent governance history that the Story 11.29 intent forbids modifying. |
| R2-BH-11 | medium | reject | carried: RBH-09 already establishes that streaming package/runtime hashes do not revalidate file identity and timestamps after reading; that independent evidence-tooling race is excluded from Story 11.29. |
| R2-BH-12 | low | reject | carried: RBH-11 already establishes that the published artifact is deliberately named an unapproved successor candidate; moving publication adds workflow complexity without correcting a Story 11.29 user-visible defect. |
| R2-BH-13 | medium | reject | carried: BH-16 already establishes that workflow-dispatch inputs and the final hard-coded validation target can diverge; the mismatch is an independently landed quality-workflow change excluded by this intent. |
| R2-EC-01 | medium | patch | A snapshotted prior owner could pass the initial active check, pause, then overwrite an already queued replacement owner's `_pendingRetry` token after ownership changed; the pending write is now guarded by `_laneGate` and rechecks that the exact entry remains active, with the focused and default Shell lanes green. |
| R2-EC-02 | medium | reject | carried: EC-02/RBH-02 already record the sustained-nudge replay recursion as pre-existing retry orchestration outside this story's approved correctness surfaces. |
| R2-EC-03 | maybe-false | reject | carried: EC-06/REC-01 already record the unproven eviction-between-sample-and-dispatch interleaving and what deterministic fixture would settle it. |
| R2-EC-04 | low | reject | carried: REC-04 already establishes that refcount overflow needs more than `Int32.MaxValue` simultaneous equivalent registrations, is not an everyday reachable lifetime, and does not justify another guard branch. |
| R2-EC-05 | medium | reject | A missing or unreadable active identity leaves `identity_bytes` null and the unconditional redaction scan calls `decode`, escaping as `AttributeError`; this independent evidence-validator defect is explicitly outside the fallback-refresh intent. |
| R2-EC-06 | false | reject | carried: BH-10/RBH-10 already establish that candidate assembly copies bounded byte snapshots, its documented caller validates live evidence immediately beforehand, and a later gate validates the candidate inputs; the directory itself is never approval. |
| R2-EC-07 | low | reject | carried: RBH-11 already establishes that the upload is explicitly an unapproved successor candidate and therefore does not represent final-gate approval. |
| R2-EC-08 | maybe-false | reject | carried: BH-12/EC-04 already record the missing raw-Authorization diagnostic check, the absence of a demonstrated reachable leaking subprocess output, and the explicit exclusion of release-governance work. |
| R2-EC-09 | low | reject | Transport status zero fails the workflow closed but can receive an imprecise authorization/cutover rejection reason code; that diagnostic-only issue is uncommon, independent governance behavior excluded by this intent, and adding retry-state branches is disproportionate here. |
| R2-EC-10 | maybe-false | reject | carried: EC-06/REC-01 already record the same missing-page interleaving claim and the deterministic evidence needed to establish it. |

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
