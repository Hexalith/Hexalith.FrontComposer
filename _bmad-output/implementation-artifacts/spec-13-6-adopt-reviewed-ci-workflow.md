---
title: 'Story 13.6: Adopt the Reviewed CI Verification Repair'
type: 'chore'
created: '2026-10-27'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '7ce37d1cfd62a7b0efc80ea7f7304a61d1308c8f'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned approved CI-adoption proposal">

## Intent

**Problem:** Shared CI consumes a Builds workflow predating the reviewed child-stdin and fresh/nonzero/passing-TRX repairs. Historical green CI does not establish execution of every configured project.

**Approach:** Adopt already selected Builds `cad925260713718895282c69bcbdb8bc72e7b6a2` through existing exact-source policy and staged publication, then verify CI and Quality on the actual candidate.

The user approved source review, policy-first CI adoption, commits/pushes and hosted verification on 2026-10-27: “yes I approve”. This supersedes earlier no-remote/no-commit restrictions for this bounded continuation. The proposal is summarized in the parent result's prepared CI adoption handoff.

## Boundaries & Constraints

**Always:** Acquire exact sources in isolated stores. Review complete caller/reusable/action closure with the existing collector. Keep the current closure authorized. Publish future authorization in a policy-only commit before a later caller-only pin change; validate against the actual active base. Validate exact commit messages with pinned commitlint and retain evidence. Preserve test, accessibility, artifact and authentication thresholds.

**Never:** Change gitlinks, dependencies, release pins, protected baselines or authentication enforcement. Initialize nested submodules. Represent the scratch caller as published. Infer independent accessibility/owner acceptance from CI. Parent Story 13.6 remains in-progress for its separate criteria.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior | Error Handling |
|---|---|---|---|
| Source closure | Exact published Builds/action commits | Collector binds sources, hashes and literal edges | Missing/mutable/ambiguous sources fail closed |
| Authorization | Current and future caller bytes | Current row preserved; future authorization active before adoption | Zero/multiple matches reject handoff |
| Project execution | Four configured CI projects | Fresh nonzero passing TRX per invocation | Consumed stdin, absent/stale/zero/malformed/failed reports reject |
| Hosted candidate | Actual root and active-base policy | CI/Quality and four live 401/403 outcomes attributed exactly | Failures/skips retained without blanket qualification |

</frozen-after-approval>

## Code Map

- `eng/workflow_source_closure.py`: immutable collector and policy projection.
- `eng/dependency_handoff.py`, `eng/dependency_graph.py`: authorization and active-base graph/handoff validation.
- `eng/dependency-graph-policy.json`: one sorted exact CI authorization; preserve all existing rows.
- `.github/workflows/ci.yml`: shared CI literal and explicit 45-minute outer budget change only after matching policy activation; preserve the four projects and other inputs.
- `.github/workflows/ci.yml` and `.github/workflows/quality.yml`: observed checkout/setup overruns require 45-minute outer budgets for shared CI, accessibility, browser matrix and live proof; preserve test/performance assertions and all four projects. The existing browser-job budget guard in `CiGovernanceTests.cs` follows that intentional envelope change.
- Selected Builds `.github/workflows/domain-ci.yml` and `Tools/test-domain-workflow-test-platforms.ps1`: immutable execution and regression sources; no edits.
- `eng/run-epic9-live-proof.sh`, `eng/verify-apphost-invalid-bearer.sh` and their existing Node regression files: release owned lifecycle locks after cleanup even when surviving descendants inherited the descriptor; preserve active contention and exact AppHost ownership checks.

## Tasks & Acceptance

- [x] Acquire exact sources outside the checkout and collect current/proposed closures.
- [x] Review execution changes and run closure/handoff/graph and Builds suites, including rejection controls.
- [x] `eng/dependency-graph-policy.json`: publish future authorization in a policy-only commit; both closures match once.
- [x] `.github/workflows/ci.yml`: commit the literal pin after remote policy activation; validate against earlier policy and publish.
- [ ] Inspect hosted CI/Quality, fresh results from all four projects, required Quality gates and four live rejection outcomes. Preserve failures/incompleteness.
- [ ] Correct the demonstrated owned-lock handoff failure in the existing proof/rejection helpers and prove surviving-child release plus active-lock contention before repeating hosted verification.
- [ ] Publish the exact 45-minute caller authorization in an earlier policy-only commit, then publish the caller budget and corrected proof/Quality configuration before repeated exact-candidate hosted checks.
- [ ] Reconcile parent result/spec and this spec with revisions, commands/counts, CI links and independent gates; publish documentation separately.

**Acceptance Criteria:**

- Given approved immutable sources, when the later caller is evaluated, then exactly one future authorization exists in its already active base and the old closure remains authorized.
- Given hosted execution, when qualification is assessed, then all four CI projects have fresh nonzero passing results and required Quality gates and live rejection probes are assessed on that candidate; missing evidence never becomes Pass.
- Given independent accessibility/platform/owner requirements, when this adoption ends, then their qualification changes only with actual satisfying evidence.

## Implementation Notes

Preserve the three known parent reconciliation edits from the preceding turn for the separate documentation commit. Policy/caller commits exclude them. Scratch caller `337db965e62029163bbab7fda6b690b30aeb25c9` is an isolated source-collection candidate; policy binds its blob rather than commit by design.

All three independent reviews completed: ten Blind Hunter findings, one Edge Case finding, and no Verification Gap findings. The consumed-to-proposed workflow/action changes were included, not only the TRX repair. The active FrontComposer caller has coverage, governed-CI and Aspire tiers disabled; the complete static closure nevertheless includes their declared literal action sources.

Fresh exact-source acquisition verifies published Builds `cad925260713718895282c69bcbdb8bc72e7b6a2` and all external metadata commits. Current closure has 14 sources and authorization digest `b38d68b1556cb765eac9129398f8a5613deac2379e59b95aee15b6b1f8f05934`; proposed closure has 15 sources and digest `c9a3729f8b3aeccf8bc56f64792387b4686c11ea62209e33418fc51e5be3e259`. Current policy matches only the current closure; the proposed policy matches each exactly once and preserves every existing row and non-CI value.

Exact local checks: `python3 -m unittest discover -s tests/eng -p test_workflow_source_closure.py` — 11 passed; equivalent `test_dependency_handoff.py` — 11 passed; `test_dependency_graph.py` — 102 passed. From selected Builds, `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` — 371 assertions passed. Raw outputs, exact source objects, closure projections and additional actual-workflow probes remain external scratch at `/tmp/story13-6-ci-adoption-approved.KFDful/`.

Three existing upstream limitations are deferred: optional Bash hook option-like filenames, duplicate TRX summary acceptance, and unfinished counters in otherwise passing TRX. None changes an active FrontComposer hook or an observed generated report. Hosted qualification will independently require one summary/counter element, terminal counters and every expected project identity/count, so these malformed-report limitations cannot establish this candidate's pass. No upstream source, selected revision or gate is weakened.

## Spec Change Log

The hosted CI job exceeded its 15-minute limit in source initialization without executing tests; accessibility exceeded its 25-minute outer limit. Chromium completed its full selection successfully after substantial setup. Extend the agent-owned plan to use the existing 45-minute main Quality envelope for the affected outer jobs. No test assertions, performance measurements, authentication or dependency selections change. Keep source acquisition, existing authorizations, all earlier source/graph successes and their exact failed-attempt attribution; add a future caller authorization before changing its bytes again. The surviving-child regression proves the lock-inheritance defect, while the hosted lock holder's PID was not captured, so that specific owner attribution remains unproven.

Hosted execution exposed a plan omission: the live proof passed its browser test and integrity/artifact gates, then its sequential rejection helper failed immediately because the lifecycle lock remained held. The existing scripts acquire an inheritable descriptor and close it only through process exit; surviving descendants can retain that open-file-description lock. Extend the non-frozen code map/tasks with explicit owned unlock after cleanup and focused real-process regression coverage. Preserve authentication, all four HTTP probes, cleanup ownership, frozen acceptance, every successful source/policy check and previous failed evidence. No dependency or selected Builds change is needed.

## Review Triage Log

| Finding | Verdict / route | Evidence and disposition |
|---|---|---|
| B1 — Verification section lacks commands/counts | false / reject | Those commands are the planned checks, not completed claims. The implementation notes now record exact executions; the requested fix is this spec, which the review workflow excludes from patch findings. |
| B2 — Parent still describes partial/unapplied proposal | false / reject | That is the preceding dated preparation, before this approval. Final parent reconciliation is an explicit pending task; it will retain that history and record actual publication. |
| B3 — Commit durable source manifest | false / reject | Repository evidence policy is tests plus CI and sourced story records. Committing new evidence manifests would contradict that policy; exact hashes/revisions and review dispositions are recorded here, with immutable sources reacquirable by SHA. |
| B4 — Summary lacks per-project counts | false / reject | The shared step already rejects zero execution and uploads per-project TRX. Actual counts and exact identities will be extracted during the planned hosted qualification; no hosted pass is yet claimed. |
| B5 — Duplicate TRX summaries accepted | medium / defer | Actual extracted verifier accepts a passing first summary followed by a failed second summary. Existing selected-Builds limitation; independently reject such artifacts for this candidate, and file upstream cardinality repair. |
| B6 — Pending/in-progress counters ignored | medium / defer | Actual extracted verifier accepts nonzero pending/inProgress alongside otherwise passing outcomes. Existing selected-Builds limitation; candidate qualification will independently require terminal counters. |
| B7 — No real MTP exit-2/failed-TRX fixture | false / reject | A fresh probe runs the actual MTP body with coherent failed TRX and exit 2, proves all four calls execute, records failure-count=1, and proves the actual final gate exits 1. Existing tests also cover generic nonzero aggregation and failed-TRX rejection separately. |
| B8 — Mock invocation string loses argument boundaries | maybe-false / reject | The string log is lossy, but no active quoting defect is demonstrated: production arguments are quoted. An unquoted-argument mutant surviving the existing special-path fixture would establish the proposed test weakness; adding a new serialization mechanism for that hypothetical gap is unwarranted. |
| B9 — Harness WaitForExit has no deadline | maybe-false / reject | Known fixture children complete and the complete suite passes. No reachable hung child is demonstrated; a reproducing fixture would be needed before adding timeout/process-tree behavior to this upstream test helper. |
| B10 — Four verifier copies may diverge later | low / reject | All four copies currently implement the same checks and all four paths are exercised by the existing suite. No actual divergent behavior is identified; consolidation adds machinery for a hypothetical maintenance issue. |
| E1 — Hook filename can be Bash option | medium / defer | A root hook named -e passes the path/file guards; bash -e exits 0 without running it, while bash -- -e runs it. Aspire job is disabled for this FrontComposer caller, so this upstream optional-hook defect is not reached by the adopted path. |

## Verification

- Existing `tests/eng/test_workflow_source_closure.py`, `test_dependency_handoff.py`, `test_dependency_graph.py` and selected Builds `Tools/test-domain-workflow-test-platforms.ps1` pass.
- Existing graph diff and draft-evaluator/create-ci validate the actual pin candidate against its earlier policy commit.
- Story-artifact validation, diff/staged-file hygiene and pinned commitlint pass.
- Hosted results record actual candidate/run/attempt, project counts, required Quality outcomes and live HTTP rejection results. Blockers never weaken acceptance.

Policy-only root `d11d85a3c61f651cc3645782f99eca0e7fd3a189` was published and remotely confirmed before pin-only root `cdb83ad634168a418c04b7d0c2326bf8fada68f1`. The actual caller's collected closure equals the existing `draft-evaluator` result and projects exactly one authorization in that actual earlier policy. `python3 eng/dependency_graph.py diff --event push --event-base d11d85a3c61f651cc3645782f99eca0e7fd3a189 --candidate cdb83ad634168a418c04b7d0c2326bf8fada68f1` exits 0. Local `create-ci` also exits 0 using actual run `40551439815` / attempt `1`; it is contract validation while hosted execution is pending, not a fabricated CI pass.

Hosted runs automatically started on exact pin candidate: [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439815), [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40551439079). Parent source/result reconciliation remains separate from those execution inputs.

### Hosted repair review

| Finding | Verdict / route | Evidence and disposition |
|---|---|---|
| R2-B1 — No held-during-cleanup assertion | medium / patch | Existing fixtures check only final availability. Extend actual discovery/shutdown fakes with separate real-lock attempts throughout cleanup, then retain final availability. |
| R2-B2 — No failing surviving-child proof | medium / patch | Existing failure fixtures do not leave an inherited descriptor alive. Combine the demonstrated inheritance condition with a failed execution and preserve the failing result. |
| R2-B3 — Inherited-descriptor signal cleanup uncovered | low / patch | SIGINT/SIGTERM invoke the shared EXIT cleanup, but the inherited-descriptor case lacks direct cancellation evidence. Add deterministic owned-harness signal coverage. |
| R2-B4 — Unlock rejection not covered | medium / patch | Both new release-failure branches deliberately fail an otherwise successful proof; exercise controlled real-wrapper rejection and the fixed diagnostic. |
| R2-B5 — Holder readiness/teardown unbounded | low / patch | Test helpers await process events without deadlines. Bound their own readiness/teardown so a fixture failure cannot leave the test process waiting indefinitely. |
| R2-V1 — Early-unlock mutants pass all 53 tests | medium / patch | Verification reviewer moved both actual unlocks before discovery/shutdown; all existing tests still passed. The held-through-cleanup regression must reject those mutants. Same correction group as R2-B1; retained as an individual finding. |

Edge Case review returned no findings. All six filed findings are routed to the existing implementation agent for the smallest test correction; no source authorization or production-lock ordering defect was found. The correction preserves the explicit owned unlock after cleanup and all existing exact ownership/authentication guards.

All six hosted-repair review findings are patched. The existing implementation agent changed only the two regression files for this review iteration. Real independent lock probes now require contention during owned cleanup discovery/shutdown/postflight; failure, SIGINT/SIGTERM and unlock-rejection cases preserve their exact results, and helper readiness/teardown are bounded. Focused suites pass 61/61. Parent copied-source controls pass all six ordering cases; moving the actual unlocks before cleanup makes all six fail. The full parent tooling command `npm run test:epic-9-evidence` passes 175 Node plus 31 Python tests, without skips or failures. Shell syntax and story/diff validation pass. Debug governance build has zero warnings/errors; the 86-test CI governance class passed with the intentional 45-minute browser-job guard.

The original CI attempt 1 and attempt 2 each exceeded their actual 15-minute source-initialization budget without executing tests. Initial Quality's main job succeeds (6,241 default tests, zero failed/skipped), as do Debug (103) and Chromium (526 passed / one capability skip). Accessibility, Firefox and WebKit exceed their original outer budgets; the live proof passes one browser test and its artifact/integrity checks before the sequential invalid-bearer helper rejects lock contention. These are exact `cdb83ad634168a418c04b7d0c2326bf8fada68f1` results, preserved separately from the repair.

The 45-minute caller authorization was published first at policy-only root `8a77d92458c6e9659a626ad092e4b8eade4942bd`. Local caller-only root `5d006f3a179eb417ed59147503b475be9664c70e` binds caller blob `8f8aadabbe65ee65a0b477e90a4062d7d70c7ddc78d582ce0a8129b68ed96358` and authorization digest `4d23bedb5baccb4219b8811ef935095af9408040d059a1fe7af33e13ef263fda` against that earlier active policy, preserving all previous rows. The helper/budget repair and later published candidate still need hosted qualification.
