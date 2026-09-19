---
title: 'Prepare the EventStore Builds Runtime Compatibility Successor'
type: 'bugfix'
created: '2026-09-19'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'cec5258e5c92fd43f5cf9c376ba6013fe3b07ecc'
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-25-eventstore-3-106-evidence-reconciliation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current `origin/main` seals v3 at
`ba7ac196e60db8820525961791eccfacec24633f / 3.106.0 /
4f522a8caa62ad82584bdf56d54e16109b717b1c`, while the selected Builds successor is
`59862a00d72ef8c7b3e3be020fa967ebd89507a0`. Rewriting v3 would destroy evidence,
and Quality has no target-bound hosted recapture command.

**Approach:** Preserve v1-v3 byte-for-byte, prepare v4 predecessor validation and an
exact-target hosted capture, then materialize v4 later from its genuine six-file artifact.

## Boundaries & Constraints

**Always:** Bind the target tuple exactly; validate v4's predecessor contract and all
v3-bound evidence; stage only after provider and authenticated AppHost validation; keep
receipts empty and `migrationApprovalClaimed=false`; preserve
`cec5258e5c92fd43f5cf9c376ba6013fe3b07ecc`; leave Story 11.29 and
`sprint-status.yaml` unchanged.

**Never:** Rewrite historical contracts/evidence; invent live evidence, hashes, or
timestamps; label a candidate approved; change dependency content, versions, Pacts,
topology, or adapters; commit or push without the requested approval packet.

**Decision (2026-09-20):** Integrate `origin/main` and the capture preparation in one
merge commit so `cec5258e…` remains intact and only one additional commit requires
approval. Add `workflow_dispatch` with required FrontComposer revision, EventStore
source, package version, and Builds catalog inputs so hosted recapture is explicitly
target-bound and command-addressable.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Exact hosted recapture | Main revision and four tuple inputs match | Publish the manifest plus five evidence files | Any failed gate prevents upload |
| Dispatch drift | Revision, gitlink, or catalog version differs | Stop before live capture | Emit bounded tuple/preflight diagnostics; publish no evidence |
| Sealed v3 differs from checkout | v3 remains at Builds `4f522a8c…`, checkout selects `59862a00…` | Preserve v3 and keep active compatibility fail-closed pending v4 | Never mutate or relabel v3 |

</frozen-after-approval>

## Code Map

- `.github/workflows/quality.yml` -- add the chosen trigger and tuple preflight; retain candidate-before-active-validation ordering.
- `eng/eventstore_runtime_evidence.py`, `eng/validate-contract-artifacts.ps1` -- add target and future v4/predecessor validation while v3 remains active.
- `tests/eng/test_eventstore_runtime_evidence.py`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- cover drift, v3 tampering, open approval, and workflow gates.
- `.gitattributes`, `docs/reference/pact-contracts.md` -- reserve v4 byte boundaries and document the two-phase command.
- Identity v1-v3 and their evidence roots -- immutable inputs, not edit targets.

## Tasks & Acceptance

**Execution:**
- [x] Reconcile `origin/main` by the approved strategy while preserving the approved gitlink commit and current-main v3 bytes.
- [x] Add target validation and v4 predecessor scaffolding without creating v4 evidence.
- [x] Add the chosen hosted trigger, tuple preflight, six-file candidate contract, and governance/unit coverage.
- [x] Update byte-preservation rules and operator documentation; leave Story 11.29 and sprint status untouched.
- [x] Run focused evidence and governance checks, then stage only this successor-preparation scope and stop before commit/push.

**Acceptance Criteria:**
- Given v1-v3 history, when preparation is validated, then every historical byte is unchanged and v3 remains verifiable.
- Given any hosted input mismatch or failed provider/AppHost check, when the lane runs, then no candidate artifact is published.
- Given exact inputs and a successful lane, when staging runs, then the artifact has exactly six genuine target-tuple files and approval remains open.
- Given no hosted candidate yet, when this preparation change is reviewed, then no v4 live evidence, decision, subject, or approval claim exists.

## Implementation Notes

- Reconciled `origin/main` with `git merge --no-ff --no-commit origin/main`; the merge remains uncommitted and preserves `cec5258e5c92fd43f5cf9c376ba6013fe3b07ecc` as its first parent.
- Added an explicit successor-preparation validator that authenticates sealed identity v3 as the future-v4 predecessor, requires its approval to remain open, rejects any materialized v4 identity/evidence, and binds the current checkout to the exact successor tuple.
- Added required target inputs to `workflow_dispatch`. The exact target preflight runs before capture for every candidate-producing event: dispatch uses those four inputs, while a `push` to `main` uses `github.sha` plus the validator-owned EventStore/package/Builds tuple. Candidate staging/upload remains after independent provider/AppHost validation and before stale active-v3 contract validation.
- Reserved absent v4 identity/evidence paths as byte-exact and documented the two-phase hosted capture/import procedure. No v4 evidence, decision, subject, receipt, or approval claim was created.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| BH-01 | false | reject | Publication after the capture preflight and both live validators but before the deliberately stale active-v3 selector is the approved two-phase design; waiting for the active selector would make genuine v4 recapture impossible while v3 remains active. |
| BH-02 | false | reject | Push and dispatch artifacts are run-scoped candidates, not authoritative identities; either must still be explicitly downloaded, reviewed, and imported by a later phase-2 change, so multiple genuine candidates do not create an approval authority. |
| BH-03 | medium | patch | Push and dispatch share `quality-${{ github.ref }}` with cancellation enabled, so an operator dispatch can cancel or be cancelled by an unrelated main push before capture completes. Isolate dispatch concurrency by its requested revision while preserving normal branch-run cancellation. |
| BH-04 | false | reject | The final literals equal the validator-owned tuple, and the earlier preflight rejects every dispatch value that differs before capture; no mismatched supplied coordinate can reach the final validator in the current workflow. |
| BH-05 | medium | patch | `-PrepareRuntimeSuccessor` is evaluated only inside `if ($RequireProviderVerification)`, so using the exposed switch alone silently skips successor validation. Reject that combination explicitly. |
| BH-06 | medium | patch | Non-empty `-Successor*` values are ignored when `-PrepareRuntimeSuccessor` is absent. Reject orphaned successor coordinates so the PowerShell wrapper matches the Python CLI's fail-closed contract. |
| BH-07 | medium | patch | `_run()` performs manifest, ledger, or receipt writes before successor validation, and currently permits those modes with `--prepare-runtime-successor`. Make successor preparation mutually exclusive with all write modes before any mutation. |
| BH-08 | false | reject | Production validation already enforces the canonical v3 path through `_validate_active`, pins its byte hash with `IDENTITY_V3_SHA256`, requires open approval, and rejects materialized v4 state; the unused aggregate constant does not leave those behaviors unenforced. |
| BH-09 | false | reject | `_live_provenance` has real temporary-gitlink integration tests for exact pins and advanced EventStore/Builds checkouts, while successor validation requires the exact current FrontComposer HEAD rather than ancestry. The fixture's historical revision does not bypass production provenance behavior. |
| BH-10 | medium | patch | The workflow copy commands currently preserve source bytes, but only source-text assertions cover the assembled directory. Add an executed fixture-backed assembly path that proves exactly six regular files and byte identity with the manifest plus the intended live root. |
| BH-11 | false | reject | GitHub artifacts are intrinsically scoped to a workflow run and the pinned upload action records artifact ID, URL, and digest; phase 1 creates no repository approval authority, and phase 2 must independently review and hash the downloaded bytes. |
| BH-12 | low | reject | A malformed dispatch does run ordinary quality gates before the successor preflight, but it still stops before costly live capture or publication. Moving or duplicating the validator is disproportionate for an unlikely operator-input failure and would complicate gate setup. |
| VG-01 | medium | patch | Pre-verified: direct function tests do not prove `main()` forwards all four successor CLI coordinates. Add CLI-entry tests for the exact tuple and each independently drifted value. |
| VG-02 | medium | patch | Pre-verified: governance tests inspect staging source text but never execute candidate assembly. Share the workflow's assembly path with a fixture-backed test that asserts exact names, regular files, and byte equality. |
| VG-03 | false | reject | This repeats BH-01: the later active-v3 selector is intentionally expected to remain red until genuine v4 import, whereas every target/provider/AppHost capture gate already precedes publication. |
| EC-01 | false | reject | Immediately before staging, `Validate current live capture` recomputes AppHost and provider authority against the manifest/current checkout, and no intervening step mutates the checkout. A change after the earlier preflight is therefore caught before publication. |
| EC-02 | medium | patch | The v3 file is hashed and then opened again for JSON parsing; each read is stable individually, but a replacement between reads could make approval fields come from bytes other than the pinned predecessor. Hash and parse one bounded read. |
| EC-03 | false | reject | This repeats BH-01/VG-03 and conflicts with the approved candidate-before-active-validation ordering needed to retrieve genuine evidence while v3 correctly remains the active fail-closed selector. |

## Design Notes

The runtime manifest records the pushed preparation revision, and provider/AppHost completion
must precede the decision and subject. Phase 1 prepares capture; phase 2 imports downloaded
bytes and creates v4. Only governance, tests, and docs change; no product API or deployment does.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_eventstore_runtime_evidence.py tests/eng/test_pact_provider_apphost_smoke.py` -- expected: all evidence and smoke tests pass.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore --filter-method '*CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval*'` -- expected: the focused governance fact passes.
- `git diff --exit-code origin/main -- _bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json _bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v2.json _bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v3.json _bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24 _bmad-output/implementation-artifacts/evidence/eventstore-runtime-identity-v2 _bmad-output/implementation-artifacts/evidence/eventstore-runtime-identity-v3` -- expected: no historical contract/evidence changes after reconciliation.

**Results:**
- Focused Python evidence/smoke suites: 258 passed in 266.221 seconds.
- Requested `dotnet test --project` invocation produced no test output and remained idle without a child process for 5 minutes 21 seconds; cancellation also stalled, so the exact process was terminated and its empty wrapper result was not accepted as evidence.
- Fallback serialized Release build: succeeded with 0 warnings and 0 errors in 53.57 seconds.
- Direct xUnit v3 invocation of `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`: 1 passed, 0 failed in 0.473 seconds.
- Post-audit focused Release rebuild after pinning push/dispatch preflight behavior and successor documentation: succeeded with 0 warnings and 0 errors in 32.63 seconds.
- The first post-audit direct xUnit invocation failed 1/1 in 0.333 seconds because the new documentation assertion expected `Migration approval remains open` without the Markdown line break; the documentation was rewrapped without changing its meaning.
- Final post-audit direct xUnit invocation of `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`: 1 passed, 0 failed in 0.537 seconds.
- Historical v1-v3 contract/evidence diff against `origin/main`: empty.
- Review-patch successor-focused Python slice: 12 passed in 22.105 seconds.
- Review-patch focused Release build: succeeded with 0 warnings and 0 errors in 22.83 seconds.
- Review-patch direct governance invocation: 1 passed, 0 failed in 0.365 seconds.
- Full post-review Python evidence/smoke suites: 265 passed in 223.957 seconds.
- The post-review retry of the requested `dotnet test --project` invocation again produced no output; after 2 minutes 53 seconds, cancellation left the wrapper stalled, so only that exact process was terminated and its empty wrapper result was not accepted as evidence.
- Independent post-review direct xUnit v3 invocation of `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`: 1 passed, 0 failed in 0.336 seconds.
- Post-review staged and working-tree whitespace checks: passed.
- Post-review historical v1-v3 diff against `origin/main`: empty; identity v3 SHA-256 remains `6dc9aaa586cf35531de112bd68dd4d724a81d7ad76a11930684e8e9fe6c98892`.
- Post-review v4 identity/evidence absence, empty v3 receipts, `migrationApprovalClaimed=false`, and protected Story 11.29/sprint-status scope: confirmed.
