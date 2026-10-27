---
title: 'Story 13.6: Adopt the Reviewed CI Verification Repair'
type: 'chore'
created: '2026-10-27'
status: 'done'
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
- Published Builds execution source `6952b59c3486ee50d32056c3fee07a07e8120bd5`, derived from approved `cad925260713718895282c69bcbdb8bc72e7b6a2`: `.github/workflows/domain-ci.yml` restores explicitly in Release, matching its existing Release build; `Tools/test-domain-workflow-test-platforms.ps1` executes both actual command bodies against a configuration-sensitive MSBuild fixture. Selected root gitlink remains externally owned. A test-only follow-up handles CRLF and preserves pinned SDK selection without changing the execution workflow.
- `eng/run-epic9-live-proof.sh`, `eng/verify-apphost-invalid-bearer.sh` and their existing Node regression files: release owned lifecycle locks after cleanup even when surviving descendants inherited the descriptor; preserve active contention and exact AppHost ownership checks.

- `docs/reference/components/command-lifecycle.md`, `docs/reference/components/projection-status.md` and `_bmad-output/project-docs/component-inventory.md`: document the existing dialog/feedback source surface so the actual configured CLI suite has a valid catalog baseline; no runtime/API or validator change.

## Tasks & Acceptance

- [x] Acquire exact sources outside the checkout and collect current/proposed closures.
- [x] Review execution changes and run closure/handoff/graph and Builds suites, including rejection controls.
- [x] `eng/dependency-graph-policy.json`: publish future authorization in a policy-only commit; both closures match once.
- [x] `.github/workflows/ci.yml`: commit the literal pin after remote policy activation; validate against earlier policy and publish.
- [x] Correct the demonstrated current-source documentation drift and rerun its two failing publication-isolation tests.
- [x] Inspect hosted CI/Quality, fresh results from all four projects, required Quality gates and four live rejection outcomes. Preserve failures/incompleteness.
- [x] Correct the demonstrated owned-lock handoff failure in the existing proof/rejection helpers and prove surviving-child release plus active-lock contention before repeating hosted verification.
- [x] Publish the exact 45-minute caller authorization in an earlier policy-only commit, then publish the caller budget and corrected proof/Quality configuration before repeated exact-candidate hosted checks.
- [x] Complete the test-only final review corrections for CRLF, pinned SDK selection and representative transport outcomes.
- [x] Preserve hidden live-proof files during upload and independently qualify the authenticated fresh downloaded bundle with unchanged checksums and final-mode validator.
- [x] Reconcile parent result/spec and this spec with revisions, commands/counts, CI links and independent gates; publish documentation separately.

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

### Release restore and transport-only recovery extension

The original frozen approval identifies the initial CAD adoption. Subsequent hosted evidence exposed two necessary execution repairs within the same approved verification intent. CI on external root `8d2b47bd206ffd5c5e77a5ac0461b9cfe52ea4fd` failed whole-solution default restore with NU1109 (EventStore 3.128.2 required versus centrally pinned 3.128.0), while explicit Release whole-solution restore succeeds. Direct UI-project Debug and Release restores both succeed; the failure is specific to the solution's default graph. Derived published Builds `6952b59c3486ee50d32056c3fee07a07e8120bd5` changes the blocking Restore command to `-p:Configuration=Release`, matching its existing Release build. A real executable configuration fixture with a spaced project path fails twice against the old command and passes in the complete 377-assertion suite against the corrected command. The runtime source is published on `fix/frontcomposer-13-6-release-restore`; Builds main and the root-selected gitlink were not changed by this work.

The same hosted Quality run passed the Epic 9 browser proof and its artifact/integrity gates after owned-lock repair. Its first command rejection probe then exhausted two curl 28 / HTTP 000 endpoints; later three paths returned real 401. The helper now permits at most two passes, and only when every endpoint in the first pass reports a recognized nonzero transport error plus HTTP000. Any HTTP outcome, partial response, unrecognized error or curl 0 / HTTP 000 fails immediately. Actual complete401/403 remains mandatory for each of the four paths; redirects remain forbidden. Focused tests pass36/36 and parent `npm run test:epic-9-evidence` from `tests/e2e` passes 180 Node plus 31 Python, zero failures/skips. This does not convert any HTTP000 into authentication evidence.

Concurrent external publication `a54b5c8dcb83f6471a0374c3bf04de856e979554` captured the prepared future authorization and helper/tests alongside its own EventStore package override and five gitlink advances. These external selections are preserved and are not attributed to this implementation. The policy was therefore already active before later caller-only root `3211b8f92f37a1ea10e2aa2b628ecba13ffd9fd4` adopted695. Caller Git-blob SHA-256 is `d31112aaddb3b5c1d8a7ecd4f63a3fc47bf4e902e5dbe3dff1f3915872499fc6`; complete 15-source authorization digest is `ea262ffb85a10749eb853dd40690b260acaaba352f8fe84f9e83c1e07aad3a0f`. Actual closure equals the draft-evaluator and matches exactly one authorization in the actual earlier active policy. Existing rows and all non-CI policy values were preserved by this work. Unlike the initial two policy adoptions, this 695 authorization was bundled by the external publication; it is not described as a separate policy-only commit.

Missing exact Memories-selected Builds `7f740a8264cf8ce444b70fe486473e5f97fd26ee` was located in an already-existing local repository, independently reviewed and published unchanged on `fix/frontcomposer-13-6-memories-builds-source`. Its existing full message passed owning pinned commitlint21.2.4. Fresh canonical acquisition and graph diff for 00b→8d then pass (41 graph changes, four affected modules); that graph result alone is not affected-module build proof. No nested submodule was initialized or updated.

### Final execution review

All three review lenses completed on the post-lock-review delta: five Blind Hunter findings, two Edge Case findings and no Verification Gap findings. The verifier independently reran 36 bearer tests and 377 Builds assertions successfully. Each finding is triaged separately below.

| Finding | Verdict / route | Evidence and disposition |
|---|---|---|
| R3-B1 — Disabled Aspire/performance restore defaults differ from Release build | medium / defer | Both optional lanes retain this mismatch in the already-approved CAD source;695 changes only the active blocking lane. Both lanes are disabled by the FrontComposer caller. Record the pre-existing upstream repair separately without enabling or claiming those lanes. |
| R3-B2 — Immediate connection failures can exhaust two passes quickly | maybe-false / reject | The helper first waits for healthy security/EventStore resources. The observed reachable cold failure was two ten-second timeouts; bounded recovery addresses that case. No healthy owned gateway with both immediately refused endpoints and a later successful readiness transition was demonstrated. Establish that actual lifecycle before adding a fixed delay, which by itself would not guarantee readiness. |
| R3-B3 — Fixture accepts a Build mutation removing --no-restore and setting unused UseNuGetDeps | false / reject | The fixture's explicit contract is effective Release configuration in the actual Restore and Build commands, and the mutations leave that contract satisfied. It does not claim to validate every dependency-graph property. The actual whole-solution default-restore failure and explicit-Release success separately establish the reason for this production correction. |
| R3-B4 — Temporary fixture bypasses pinned global.json | medium / patch | Real dotnet execution selects SDK from its temporary working directory. Copy the existing repository global.json there before execution so supported SDK selection is preserved. |
| R3-B5 — Only curl 28 cold recovery is covered | low / patch | Add representative connection 7, TLS 35 and incomplete-response 56 to the existing bounded cold-start success/HTTP200 rejection cases without production changes. |
| R3-E1 — CRLF inline command regex rejects valid workflow | medium / patch | Reproduced: the new regex anchors before an unconsumed carriage return. Accept optional CR and run the actual fixture with both LF and CRLF workflow inputs. |
| R3-E2 — Frozen initial source claim differs from derived execution pin | false / reject | The frozen entry records the initial approved CAD adoption and remains historical human-owned intent. This dated execution extension and the current code map explicitly record derived 695, its exact change, publication, authorization and actual caller. No execution claim is promoted to the initial source. |

Current hosted candidate is external merge `0fc5f4efca4b31ef700ac253f898ef051c9b17e4`. Relative to 3211 it changes only two UI behaviors and their tests; CI caller, policy, Quality workflow and all gitlinks are byte-identical. Semantic graph diff 3211→0fc passes with zero affected modules. CI 40566351567 and Quality 40566350657 are still running/queued; no final pass is claimed. Earlier 00b and 3211 runs were superseded by external main publications and retain their canceled outcomes.

Final review corrections are complete. Parent reruns pass **42/42 bearer tests**, **186 Node + 31 Python tests** via `npm run test:epic-9-evidence` from `tests/e2e`, and **383 Builds assertions**, with zero failures/skips. The PowerShell fixture now accepts CRLF outside its captured shell body, executes both LF and CRLF inputs in memory and copies the unchanged pinned global.json into its temporary working directory. Test-only Builds follow-up `f00c4ef7fb0d7b08068eaeeb4b062d5e5ee4babf` is published on `fix/frontcomposer-13-6-release-restore`; its workflow Git blob is identical to execution source695, which remains the caller's immutable pin. Runtime source is unchanged; no new policy or caller pin is necessary for these test-only corrections. The branch publication and both owning pinned commitlint results are retained in external scratch. No transient baseline or node_modules file was staged.

At 19:41 UTC, another external main publication advanced to `5b6b55cad59598e6bedf8690f27ef4e39d58321b` with dialog/keyboard runtime changes. Prior0fc proof cannot certify that UI revision. CI/policy/Quality/helper bytes and gitlinks remain identical; graph diff0fc→5b6 passes with zero changes/affected modules. GitHub Actions API quota is exhausted until19:56:48UTC; current run outcomes remain unreported until authenticated retrieval. This bounded spec stays in-review with hosted qualification and final publication tasks open; the parent remains in-progress.

### Real four-project execution and catalog correction

During the GitHub API quota pause, the parent built the four configured projects in Release with warnings as errors and executed the **actual immutable695 unit-test body** against real dotnet in fresh external scratch. All four execute despite the CLI failure: CLI155passed/2failed, Contracts233passed, Contracts.UI10passed and Testing185passed, zero skips. Its deferred failure counter is exactly1; the loop's exit0 is expected aggregation, not a passing final gate. The two CLI publication-isolation failures identify seven undocumented current-source dialog parameters and two unclassified Feedback components. The source validator directly reads copied `.razor`/`.cs` declarations and current Markdown; no Release package/reflection mismatch participates.

Extend the existing implementation plan with source-derived catalog documentation only: seven dialog parameter rows, seven Feedback parameter rows and two adopter classifications. The standalone Feedback components explicitly accept caller-owned copy/content and normalized record snapshots, supporting adopter composition rather than an invented infrastructure exemption. No validator, expectation, source API, behavior or threshold changes. Preserve all previous runtime/source/authorization fixes and all earlier failed reports. The existing implementation agent applies26 additive documentation lines across three files. `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` passes with zero blocking failures; the two exact formerly failing Release publication tests pass2/2 with no failures/skips/errors/not-run. Parent full four-project rerun and independent fresh-report qualification follow; these local results do not claim hosted qualification.

Parent local rerun of the actual695 unit body is green: **585/585**, comprising CLI157, Contracts233, Contracts.UI10 and Testing185, zero failures/skips. All four reports are fresh and independently qualify with exactly one summary/counter element, terminal counters, coherent counts and expected project identities. The actual step output is `failure-count=0`. Root remains5b6b55ca with the described dirty documentation correction; this is explicitly local patched-source evidence, not hosted/clean-commit proof. Raw reports, execution log, before/after root receipts and qualification JSON remain under `local-four-projects-final/` in external scratch.

Authenticated retrieval resumed after the19:56:48UTC quota reset. Hosted `5b6b55cad59598e6bedf8690f27ef4e39d58321b` [CI40569513928](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40569513928) executes all four configured projects with the same155/2,233,10,185 results, then correctly fails the final blocking gate for one failed project. [Quality40569513323](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40569513323) main fails13 SourceTools documentation scenarios on the same nine catalog findings. Its accessibility/visual job passes355 tests, Debug passes103, and live acceptance succeeds: one browser test plus artifact/integrity validation and all four actual first-endpoint401 outcomes. Browser matrix jobs remain in progress at19:59UTC. Predecessor0fc CI/Quality and3211 CI/Quality are now confirmed canceled, preserving those exact candidate outcomes. These current partial successes do not override the two overall failures or qualify a later corrected candidate.

### Catalog correction review

All three independent layers completed on the additive catalog correction: three Blind Hunter findings, no Edge Case findings and no Verification Gap findings.

| Finding | Verdict / route | Evidence and disposition |
|---|---|---|
| R4-B1 — Freshness announcement ownership is absent | medium / patch | The component always renders its own polite status region. Document that actual ownership and one announcement owner for the same freshness text, avoiding nested or duplicate announcers. |
| R4-B2 — Snapshot Status/AsOf contract is absent | low / patch | Document the source-defined input values and optional AsOf, and that this component reads Status while callers format timestamp copy. This is a direct description correction with no code/state changes. |
| R4-B3 — Newly adopter-classified components lack usage examples/imports | low / patch | Add minimal valid caller markup and its two source namespaces, preserving existing examples; the page currently demonstrates only the skeleton. |

All three patches route to the same implementation agent and change only projection-status.md. Parent targeted SourceTools documentation verification follows those edits; no source behavior or gate changes.

Catalog review corrections are complete and parent verification passes: SourceTools Release build has zero warnings/errors; both existing documentation classes pass155/155, zero failed/errors/skipped/not-run, including the13 hosted failures. The actual four-project loop's585 passing reports retain the earlier local patched-source attribution; the later explanation/examples do not change runtime or parameter contracts. Existing documentation validation passes again after those explanation edits.

Concurrent external `f02b490f746050186ad7fdf92bdc6877e360b22c` published the exact three reviewed catalog documentation files (47 additive lines). Our later test-only `dd84589d5fd5aa6aeca3fd8af0b551b6297d11a8` is remotely confirmed on main. No catalog commit under the separately prepared message was created by us. Its actual closure equals draft-evaluator and matches exactly one earlier active f02 authorization; semantic graph diff is green with zero changes/affected modules. Fresh [CI40575108068](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575108068) and [Quality40575109420](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575109420) are pending/running at20:09UTC. Story/result records remain unstaged until hosted qualification; no passing result is invented for dd.

The corrected dd hosted CI build-and-test job succeeds. Authenticated run metadata binds run40575108068/attempt1, push/main, exact candidate `dd84589d5fd5aa6aeca3fd8af0b551b6297d11a8` and actual referenced reusable695. Downloaded `blocking-test-results` artifact12768196099 has verified SHA-256 `577da512a21eeca73b8cd82dd95ef833118872e7badfa4430630443f5f37448b`. Independent qualification passes all four fresh reports: CLI157, Contracts233, Contracts.UI10, Testing185; **585 passed, zero failed/skipped**. Each report has one summary/counter element, terminal/coherent counts, expected identities and start/finish times within the successful job interval. Source/handoff governance and overall CI/Quality remain pending; this test-job success is not labelled an overall pass yet.

The dd Quality live job succeeds: **one browser test**, existing artifact/integrity validation and **four actual first-endpoint401 responses** for commands, command status, queries and SignalR negotiation. Debug succeeds **103/103**, zero failures/skips. Raw authenticated logs are `final-epic9.log` and `final-debug.log` in external scratch. Other jobs continue; all independent parent gates remain unchanged.

### Aggregate baseline review

The final combined diff was rebuilt from the preserved7ce37d baseline, including all current tracked/uncommitted changes and the complete CAD→f00 execution/test source delta. All three context-free review layers completed: ten Blind Hunter findings, six Edge Case findings and three Verification Gap findings. This full baseline also contains independently introduced feedback/download/typed-name feature work from external813b9c70, b633c07e and5b6b55ca commits. Git history and the actual source establish that these implementations were not added or changed by the CI-adoption repair. The repair's intent is truthful execution/provenance of the configured CI and Quality gates, not implementation of those separate adopter APIs. Their defects and missing integration proofs are therefore pre-existing feature follow-ups, not grounds for attributing their implementation or qualification to this bounded repair. They remain visible in the parent ledger and no passing CI is represented as proof of those untested contracts.

Each finding is triaged before grouping:

| Finding | Verdict / route | Evidence and disposition |
|---|---|---|
| R5-B1 — Early command confirmation lost | medium / defer | Source registers Subscribe only after awaited IssueCommand and performs no initial connected re-read. A valid confirmation during that await is lost; the operation can remain pending. External reconciler feature root cause; same group as R5-E1. |
| R5-B2 — Presentation rejection exception treated as command failure | medium / defer | Revert/Rejected execute inside the command try; a throwing Rejected enters its generic catch, repeats Revert and calls Failed with the presentation exception. External reconciler exception-boundary defect. |
| R5-B3 — Command fault after cancellation still produces feedback | medium / defer | The normal completion branch checks cancellation, but the generic catch calls Revert/Failed unconditionally. A late non-cancellation fault contradicts silent abandonment. External feature; same group as R5-E2. |
| R5-B4 — Fallback read cancellation argument ignored | medium / defer | TryReconcileAsync explicitly names its parameter ignored and passes only the operation token to the gate and Reconcile. A canceled per-attempt token cannot abandon that read. External feature callback contract. |
| R5-B5 — Reconciled announcement exception leaves completion pending | medium / defer | done becomes true before Announce; a throwing callback prevents TrySetResult. Subscription discards the Task, and later confirmations return because done is true. External feature; same group as R5-E3. |
| R5-B6 — Caller callback dispatcher affinity lost | maybe-false / defer | ConfigureAwait(false) demonstrably loses the captured context. No production consumer of this newly introduced reconciler is present to establish its dispatcher contract. Record unverified medium: a real Blazor caller using renderer-affine callbacks after asynchronous IssueCommand must establish the user-visible failure or an explicit dispatched-adapter contract. |
| R5-B7 — Download cleanup timeout skips module disposal | medium / defer | CleanupAsync catches JSException only. TaskCanceledException escapes before the following DisposeAsync and can replace the original transfer failure. External download service; same group as R5-E4. |
| R5-B8 — Late typed-name subtree is not observed | medium / defer | OnAfterRenderAsync returns on every later render, while RequiredName can change from null to a value and create the editor then. Its proxy observer is never registered. External typed-name addition; same group as R5-E6. |
| R5-B9 — ConfirmationHelp not associated with input | medium / defer | Help is a paragraph without an ID; the input's aria-describedby names only Body. Caller-provided typing guidance is absent from the native input's description. External typed-name feature. |
| R5-B10 — Parent result anchor renamed without updating link | low / patch | The parent points to absent approved-ci-adoption while the heading is now Initial approved CI adoption. Correct the current-result link to the existing current execution-repair heading. This is our record amendment, not the frozen current build spec. |
| R5-E1 — Confirmation during command await is missed | medium / defer | Independently reproduced against exact source; registration follows acceptance and a connected operation stays pending without another signal. Same root cause as R5-B1, preserved individually. |
| R5-E2 — Fault after caller cancellation invokes rollback/failure | medium / defer | Independently reproduced: generic fault path lacks the cancellation check present on normal completion. Same root cause as R5-B3, preserved individually. |
| R5-E3 — Throwing Reconciled announcement suppresses future completion | medium / defer | Independently reproduced: done=true, unobserved callback exception and incomplete Task. Same root cause as R5-B5, preserved individually. |
| R5-E4 — Cleanup interop timeout prevents disposal | medium / defer | Independently reproduced TaskCanceledException bypasses the later module disposal; same root cause as R5-B7, preserved individually. |
| R5-E5 — Explicit/native description can remain stale | medium / defer | The field observer filters attributes to data-fc-invalid and observes text only inside its root; the document observer sees child-list changes, not character-data changes in an external Body. Changing aria-describedby or external text can leave the proxy stale until another observed mutation. External projection feature. |
| R5-E6 — Dynamic RequiredName lacks accessibility initialization | medium / defer | Exact source firstRender guard skips observeFieldAccessibility for the later-created editor. Same root cause as R5-B8, preserved individually. |
| R5-V1 — Optimistic presentation application unobserved by tests | medium / defer | Pre-verified gap: every fixture ApplyOptimistic callback is a no-op, so removing the call leaves all checked assertions unchanged. Independent feature verification gap; test mutable presentation state and its command-time/rollback values. |
| R5-V2 — C# to JavaScript download operation identity unverified | medium / defer | Pre-verified gap: bUnit ignores the operation ID while browser tests call JavaScript directly with matching synthetic IDs. A real service-triggered browser download is absent; an ID mismatch would silently produce no file. Independent feature integration gap. |
| R5-V3 — Typed-name native description proxy has no browser proof | medium / defer | Pre-verified gap: typed-name component tests mock JS; existing browser cases enter old generated-field/modal paths. Removing the new native aria-describedby assignment preserves checked assertions. Independent feature browser-verification gap. |

After individual triage, eighteen deferred findings group into thirteen independent feature follow-ups; four earlier upstream follow-ups remain, for seventeen total. Five duplicate pairs retain their own rows above. The one owned patch corrects the sole stale parent Markdown link; an inverse-byte assertion proves all other parent content is unchanged, and story-artifact/scoped diff validation pass. None of these findings changes a configured gate, proof threshold, runtime implementation, manual fact or independent acceptance decision. The external feature implementations are preserved for their owning workstream.

### API-summary gate correction and stable candidate

Preceding exact root `dd84589d5fd5aa6aeca3fd8af0b551b6297d11a8` completes [CI40575108068](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575108068) successfully, including dependency governance. Its authenticated blocking artifact12768196099 has ZIP SHA-256 `577da512a21eeca73b8cd82dd95ef833118872e7badfa4430630443f5f37448b`; independent qualification verifies exactly four fresh reports with expected project identities, one summary/counter element each, coherent nonzero terminal counters and585 passes/zero failures/skips. Actual referenced workflow is immutable695. Authenticated handoff/source artifacts12768128004/12769395797 match their ZIP digests; local `verify-ci --live` and `verify-source --live` pass against exact rootdd/basef02 and the independently collected evaluator. The `--live` switch compares local Git/policy; authenticated GitHub retrieval separately establishes artifact provenance.

[Quality40575109420](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40575109420) succeeds in every required job except main. Main passes compilation, governance543 and live EventStore provider69, then full Docs Gate2d rejects only missing type summaries for FcFreshnessIndicator and FcLiveRegion. Its default Gate3 lane does not execute. Debug103, accessibility355 and live acceptance (one browser test plus artifact/integrity validation and four actual401 responses on endpoint1) succeed. Chromium530 passes/1 capability skip; Firefox508 passes/22 skips/one recovered flaky route-contract case; WebKit509 passes/22 skips/zero flaky cases. Existing retries are unchanged; this does not establish zero-flake parent acceptance or actual WebKit system-palette paint.

The implementation adds only XML summaries in the two components' `.razor.cs` partial declarations. No members, runtime behavior, API baselines, validators or gates change. Agent and parent both run `pwsh -NoProfile -File eng/validate-docs.ps1` without skip switches: exit0, eight of eight snippet builds pass, DocFX metadata/site succeeds and zero blocking failures. A preceding exit134/empty log occurred while the filesystem was full; it is infrastructure failure, not a gate result. Removing only three completed duplicate scratch acquisition stores restored space; their acquisition receipts/logs remain, and immutable sources remain canonically reacquirable. The manifest's deterministic generatedAt is not treated as execution freshness.

Root `4164ad0461dc33a0dfa856d9f9d48f037117977b` publishes only those two source documentation files after successful pinned21.2.2 commitlint and hooks. Graph diffdd→4164 has zero changes/affected modules. Independent complete15-source closure equals draft-evaluator and is authorized exactly once in the earlier activedd policy. [CI40582196522](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582196522) and [Quality40582194859](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582194859) are the fresh candidate runs. The user answered “no more pushall”; no `/pushall` is invoked and main is held stable during hosted verification.

Aggregate-review executable reproductions remain external: `dotnet run --project /tmp/fc-review-reconcile-sl70fu7_/repro.csproj --configuration Release --no-restore` exits0 and demonstrates lost early confirmation, duplicate rejection rollback, ignored canceled read token, incomplete reconciliation after callback failure and feedback after cancellation. Captured-context loss is observable, while its consumer-contract harm remains unverified. `dotnet run --project /tmp/fc-review-download-hticxexl/repro.csproj --configuration Release -p:NuGetAudit=false` exits0 and reports cleanup TaskCanceledException with zero module disposals. Other typed-name findings retain source-inspection/verification-gap attribution. These independently introduced feature follow-ups remain in the seventeen-group ledger.

### Concurrent source-documentation publication

Cursor-authored `d174bd822a60ac1929e506d367e0ee927f44821e` publishes three files: two duplicate type-summary partial declarations and the existing dialog CancelLabel's missing additive API-manifest entry. It cancels the4164 candidate's CI/Quality before qualification; those canceled runs establish no pass. The external changes are preserved, not attributed to this repair. Independent complete15-source closure and draft-evaluator match, earlier active4164 authorization matches exactly once and graph diff4164→d174 passes with zero changes/affected modules. The replacement exact-candidate runs are [CI40582483259](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582483259) and [Quality40582482647](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582482647). No `/pushall` was invoked by this continuation.

A local Release solution restore/build started on4164 and completed with zero warnings/errors before the external edits. Individual-project default-selection tests use the hosted trait exclusions and coverage, but read repository files while the external edits/publication occur. Their results must retain that changing-worktree attribution and cannot independently qualify exact4164 or d174. Hosted reports supply exact-candidate results.

### Exact successor CI qualification and local default selection

[CI40582483259](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40582483259), attempt1 on exact `d174bd822a60ac1929e506d367e0ee927f44821e`, completes successfully including dependency governance. Authenticated run metadata records push/main, `.github/workflows/ci.yml` and referenced immutable Builds695. Blocking artifact12769959099, ZIP SHA-256 `d95d09471da9a22411152270d5c3ccbd24f6683dda73fafde6d5b2bd3f2d6abb`, independently qualifies four fresh reports: CLI157, Contracts233, Contracts.UI10 and Testing185;585 passed, zero failed/skipped. Expected project identities, exactly one summary and counter element per report, coherent nonzero terminal counters and timestamps within the successful job interval all pass. Optional disabled Aspire/performance jobs remain skipped, not executed.

Authenticated handoff12769729950 has archive SHA-256 `49e37c43ba9b14a1005cc89b3bb3a5638cbd7796f0654c45eadef14241c650bf` and raw payload `303bb23259c429f68c44e8724755008cc95ce9d14251e4b073d1c7ff4e8e16e4`; source12770319002 has archive `3071fe26a96460b29b690dcb1d73643e6f8f0ad065fe59131272731698bcb896` and raw payload `eaa9e107aba00e2c5a56251671be8c9f1f9263314589093bd481e9634b096c3a`. Exact run/attempt/root/base4164 coordinates and archive digests match authenticated metadata. Handoff evaluator equals the independently collected closure. `python3 eng/dependency_handoff.py verify-ci --handoff <downloaded-handoff> --live` and corresponding `verify-source --proof <downloaded-source> --live` each return ok:true against local Git/policy. Receipts, reports, commands and results remain under external scratch `successor-summary-ci-*`.

The local individual-project default selection finishes6271 passed/zero failed/skipped: CLI157, Contracts233, Contracts.UI10, MCP435, Shell.Tests.Bench1, Shell3834, SourceTools1416 and Testing185. Each command uses `dotnet test --project <project> --configuration Release --no-build`, the same five hosted trait exclusions, TRX and Cobertura coverage. The existing `ci_governance.py validate-mtp-evidence` validates eight distinct modules, eight nonempty reports and eight coverage files, exit0. Release restore/build has zero warnings/errors. This retains the changing-working-tree attribution above rather than replacing exact hosted qualification. The generated Pact handoff's only local side effect was line endings; byte-normalized equality to HEAD is asserted before restoring repository CRLF. No consumer pact content changes.

### Successor live authentication and artifact qualification

Quality's exact d174 live job122465743893 succeeds: one real browser test passes in25.6 seconds; the existing artifact/integrity validator passes against the exact candidate. Raw rejection-helper log contains exactly the four expected completed401 outcomes. POST `/api/v1/commands` returns401 on endpoint2 after endpoint1's curl28 timeout; GET command status, POST queries and POST SignalR negotiate each return401 on endpoint1. That timeout is retained as transport evidence and is never counted as a rejection. No second-pass relaxation or authentication/gate change is introduced.

Authenticated exact-run/root live artifact download matches uploaded archive SHA-256 `d3dab1a6e8bace388e40ba5a81099e34981511b9dbea9d1903b9ce60fd5c59b1`. The existing offline validator is rerun locally with `--candidate d174bd822a60ac1929e506d367e0ee927f44821e`; its result and artifact receipt remain external scratch under `successor-summary-epic9*`. Debug job122465744681 succeeds with103 passed/zero failed/skipped. Main and remaining browser/accessibility jobs retain pending status until actual completion.

### Downloaded live-bundle completeness repair

Independent retrieval finds a packaging defect after the successful pre-upload validator: authenticated artifact12770429158 matches the logged digest, but lacks exactly `playwright-results/.last-run.json`, which checksums.sha256 lists. No unexpected or other missing files exist. The unchanged offline validator exits1: “checksums.sha256 must list every regular artifact exactly once and no unlisted artifact may exist.” This failure is retained; the downloaded bundle is not qualified and no missing evidence is synthesized.

Exact pinned actions/upload-artifact source defines `include-hidden-files` with defaultfalse. Extend the non-frozen implementation plan with `include-hidden-files: true` on only the existing Epic9 artifact upload. The generated proof root already passes complete sorted-checksum and redaction/semantic validation before upload. Preserve all file contents, hidden-file checksum requirements, validators, runtime/test/authentication thresholds, dependency selections and frozen parent acceptance. Publication must be followed by authenticated complete-artifact validation on its actual new root; d174's CI and other outcomes retain d174 attribution. Three independent focused reviewers assess the one-line packaging fix before publication.

### Focused artifact-upload review

All three independent review lenses complete. Blind Hunter and Edge Case return no findings. Verification Gap finds no blocking implementation defect and retains the planned hosted transport check: local validator fixtures have no dotfile and do not exercise GitHub artifact transport, so114 passing validator tests alone cannot establish this publication fix. This is a required pending verification task, not a deferred defect or a claimed pass. Exact pinned action sources parse the boolean and set excludeHiddenFiles to its inverse; the path glob remains restricted to the proof root. Independent inspection confirms39 manifest entries,38 downloaded files, exactly one missing hidden status file, no extras/symlinks and matching hashes for every present file. Credential-bearing raw logs remain outside the producer's initially empty proof root. No missing file is reconstructed for qualification.

Parent `node --test tests/e2e/scripts/validate-epic9-artifacts.test.mjs` passes114/114 without failures/skips. Before final qualification, authenticated fresh artifact metadata/download must bind the new full root/run/attempt, ZIP digest must match, every path in the fresh manifest, including the hidden status file, must be present with its exact bytes, and unchanged final-mode `validate-epic9-artifacts.mjs --candidate <new-full-root>` must pass with no `--allow-dirty`. The original downloaded d174 completeness failure remains recorded.

### Published complete-artifact candidate

Root `610917884f727d5d9686a5c0fb097f4ec9fac930` publishes only the one-line Epic9 upload correction. Exact full commit message passes pinned21.2.2 commitlint and hooks. CI caller bytes remain unchanged; independently collected15-source closure equals draft-evaluator and has exactly one authorization in the earlier active d174 policy. Graph diff d174→610 passes with zero changes/affected modules. Remote main is confirmed at the exact root before fresh [CI40584818881](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40584818881) and [Quality40584818259](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40584818259). The required artifact redownload/replay and remaining hosted gates are pending. The preceding d174 CI success and downloaded-live-bundle failure retain their original attribution; remaining Quality jobs are superseded/canceled by this necessary repair.

The sixth review round returns no blocking implementation findings and explicitly preserves hosted transport qualification as mandatory. Existing validator114 and CiGovernance86 pass without failures/skips. Seventeen existing deferred follow-up groups remain; this owned artifact completeness repair is implemented, not deferred. Five story records stay separate for final reconciliation and publication after actual runtime/workflow qualification. No `/pushall` is invoked.

### External c2 publication and independent follow-up reconciliation

Separate external commit `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4` advances main from610 and changes eight feedback/download/dialog source/test files. CI610's build/test job passes, then dependency governance is canceled; no overall610 CI qualification is claimed. Its remaining Quality work is superseded by c2. Preserve the implemented hidden-file upload repair unchanged. A read-only fetch/inspection and fast-forward preserve all five pending story records; no external feature changes are attributed to the CI repair. Independent15-source closure equals evaluator and matches exactly once in earlier active610 policy; graph diff610→c2 has zero changes/affected modules. New exact-candidate runs are [CI40586087034](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586087034) and [Quality40586086551](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586086551).

All three independent reconciliation lenses complete before grouping dispositions:

| Finding | Current disposition | Evidence |
|---|---|---|
| R5-B1/E1 | open | Subscription still follows awaited command acceptance; no initial connected read. Exact-c2 reproduction loses the early signal and stays pending until cancellation. |
| R5-B2 | resolved externally | Rejection callbacks move outside the command try. New tests and exact-source reproduction preserve original callback failure, one rollback and zero Failed callbacks. |
| R5-B3/E2 | resolved externally | Generic command fault now checks caller cancellation. New test and reproduction show silent abandonment with only Saving. |
| R5-B4 | open | Per-read argument remains explicitly ignored; a canceled read token still reaches Reconcile with an uncanceled operation token. |
| R5-B5/E3 | resolved externally | Callback fault now faults completion, terminates owner and disposes subscription once. New connected/one-shot fallback tests and reproduction cover that scope. |
| R5-B6 | unverified, open | Context loss remains, while no production consumer establishes a renderer-affine callback contract. |
| R5-B7/E4 | partially resolved | Nested finally guarantees module disposal. Exact-c2 transfer-fault plus cleanup-timeout reproduction still observes cleanup TaskCanceledException instead of original transfer failure, with disposal once. |
| R5-B8/E6 | open | First-render-only observer guard is unchanged for late RequiredName mounting. |
| R5-B9 | open | ConfirmationHelp still lacks native input description association. |
| R5-E5 | partially resolved | Registration-time external text mutations are observed. Explicit aria-describedby changes, newly referenced/replaced elements and observer refresh remain uncovered. |
| R5-V1 | open | ApplyOptimistic fixtures still do not observe mutable presentation state. |
| R5-V2 | open | No public C# service-triggered browser download/operation-ID bridge proof. |
| R5-V3 | open | No native typed-name description browser proof; c2's external-description text observation is also untested in a browser. |
| C2-V1 | medium / defer | Added callback-fault tests use a one-shot fallback ignoring its stop predicate. A realistic fallback consuming that predicate must prove termination after throwing ReadFailed/Degraded callbacks. This is source-inspected test coverage, not an executed mutant or introduced production defect. |

No concrete introduced accessibility regression is established by the independent reconciliation. Exact-source isolated reproduction copies six production files byte-for-byte; source hashes, harness and output remain under `/tmp/fc-c2-feedback-reconcile-35_56cn9/`. Both compile/run and later no-build execution exit0 while reporting the resolved and surviving behaviors above. Three of seventeen prior follow-up groups resolve; two partially resolve. Adding C2-V1 leaves eighteen historical/current groups and fifteen outstanding, including those two partial groups. Earlier dated findings remain historical evidence, not a claim the three corrected defects still exist.

Parent focused verification on c2: `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --disable-build-servers -m:1` exits0 with zero warnings/errors. Native MTP selection of FcOptimisticReconcilerTests, FcBrowserDownloadServiceTests and FcDestructiveConfirmationDialogTests passes44/44, zero failures/skips. Commands, logs and TRX remain external `c2-focused-*`. Shared source was not modified by the reconciliation agents. Full c2 hosted qualification remains pending.

### Current c2 CI qualification

[CI40586087034](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40586087034), attempt1, succeeds on exact `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4`, including dependency governance. Actual referenced reusable workflow is immutable695. Four independently qualified fresh reports contain CLI157, Contracts233, Contracts.UI10 and Testing185:585 passed, zero failed/skipped. Unique summaries/counters, coherent terminal counts, expected identities and report times within the successful job interval all pass. Authenticated metadata binds exact run/root; ZIP digest checks and local Git/policy source/handoff validation pass against earlier active610. Handoff evaluator equals the independently collected closure. Disabled optional Aspire/performance jobs remain skipped.

- blocking: artifact12772337422, archive SHA-256 `f9b9e24d2611a8ea882272e11dd97417941afcbb2ee472bad02d29f3a7383141`; authenticated identity/digest verification passes.
- handoff: artifact12771387479, archive SHA-256 `296256a7d6456ad7be28baeaf715a6bc5bac1a0423fd8049ce16d315c931639e`, raw payload `f8009602a3423dc2a46d38dcb8e93a479d1e7230a5553b00f2f713e600046fbc`; authenticated identity/digest verification passes.
- source: artifact12771133128, archive SHA-256 `d23b698aa3011a5a0162a66ff5233e26bbe5e8303129d85f0de59bd5bba14bae`, raw payload `51ce933a1a1199b8203b54b7749664d6faadd2afa9b9100299c83c35f5e6d2f4`; authenticated identity/digest verification passes.

Receipts, run metadata, reports and validation results remain external scratch `c2-ci-*`. Complete Quality and fresh downloaded live-bundle replay remain pending.

### Qualified complete live artifact on current c2

Exact c2 Quality live job122478396063, run40586086551/attempt1, succeeds. Authenticated artifact12771574151 has ZIP SHA-256 `1f3e2fb4c86e6d8efda15c57e8fa0c3525594c833a55438e0c0771a1df9dbd55` and unchanged manifest SHA-256 `55324c5d8c3d62f178c6fd8425d11b4f89c310982d74b30e6c96d412c75bad66`. The fresh download contains exactly all39 manifest-listed files, including `playwright-results/.last-run.json`; all file digests match, with no missing/unlisted paths. The unchanged validator runs in final mode against full candidate `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4`, without `--allow-dirty`, and exits0. Manifest bytes remain identical before/after replay. This independently establishes the610 upload repair on the current external-runtime successor; no missing evidence is synthesized.

One real browser test passes, existing semantic/integrity validation passes, and all four expected routes return completed401 on endpoint1. Raw logs/receipt show POST commands, GET command status, POST queries and POST hub negotiate; transport000 is never counted. Metadata/download digest and exact job/run/root/attempt bindings pass. Receipts, ZIP, unchanged extracted bundle and replay output remain external `c2-epic9*`. Remaining Quality gates retain pending status until actual completion.

### Current main Quality and default-artifact qualification

Quality main job succeeds on exact c2/attempt1. Required source compilation/isolation, CLI/governance, analyzer build proofs, tooling/semantic policy suites, contract/live provider, full documentation, default unit/bUnit, package compatibility and internal packaged-adopter smoke gates all pass. Raw logs record governance543, analyzer build proofs3, consumer contract3 and live EventStore provider69, with zero failures/skips in those selections. Full Docs Gate2d passes without skip switches.

Authenticated results artifact12773561137 ZIP SHA-256 `0458966e8adaa7b62a6293ea2b6e65d091e8da880e582a3d3561793707268c7b` and coverage artifact12773551061 ZIP SHA-256 `1abf902d8f3861eaa55e129b6e04739e57d8dd8bb3df5cbbafe97d90e88e93a4` bind exact Quality run40586086551/rootc2. Independent report qualification finds exactly eight distinct expected modules, unique summaries/counters, nonzero coherent terminal counts and report times within the successful main-job interval:6283 passed, zero failed/skipped. The unchanged MTP evidence validator independently accepts eight reports and eight coverage files.

| Module | Passed |
|---|---:|
| hexalith.frontcomposer.cli.tests.dll | 157 |
| hexalith.frontcomposer.contracts.tests.dll | 233 |
| hexalith.frontcomposer.contracts.ui.tests.dll | 10 |
| hexalith.frontcomposer.mcp.tests.dll | 435 |
| hexalith.frontcomposer.shell.tests.bench.dll | 1 |
| hexalith.frontcomposer.shell.tests.dll | 3846 |
| hexalith.frontcomposer.sourcetools.tests.dll | 1416 |
| hexalith.frontcomposer.testing.tests.dll | 185 |

Advisory palette selection passes4; performance selection has27 passed/one skipped; quarantine selects zero tests. Their existing advisory/ignore-empty semantics are unchanged, and no performance or quarantine qualification is inferred. Required main-job success is distinct from parent acceptance. Current Chromium530 passed/one capability skip, Firefox509 passed/22 skips, accessibility355 and browserless3+1 pass; no flaky tests are reported in those completed selections. WebKit remains pending. Raw main/browser logs and artifact receipts remain external `c2-*`.

### Final bounded completion

Exact root `c2b925bae1c9b3bfa8c3a4698ad0104de02ed5a4`, attempt1, completes CI40586087034 and Quality40586086551 successfully. All eight required Quality jobs succeed. Current WebKit has508 passed/22 skipped/one recovered flaky test: `specs/route-skip-links.spec.ts:196:3`, “opening a shell dialog preserves its own Tab flow”. No browser retry, skip, visual threshold, authentication rule or report validator is relaxed. Chromium has530 passed/one capability skip/no flaky; Firefox509 passed/22 skips/no flaky; accessibility355 and browserless3+1 pass. Main default6283, CI585, Debug103, governance/provider/docs/package/internal-adopter and complete-live-bundle qualifications have the exact evidence recorded above. Selections overlap and are not summed as unique tests.

The final hidden-artifact transport requirement is fulfilled by authenticated fresh download, complete39-file manifest set/digests and unchanged final-mode replay, including the previously omitted status file. Source/authorization and earlier active-base checks remain valid. Seven review/reconciliation rounds preserve every individual disposition; three external groups resolve, two partially resolve, and fifteen follow-up groups remain outstanding across eighteen historical/current groups. C2-V1 is a source-inspected verification gap, not an executed mutant or introduced production defect. The existing WebKit recovered flake is reported without inferring parent zero-flake acceptance.

Only the five story records are published in the final documentation descendant. Source, test code, workflows and dependency selections remain byte-identical to qualified c2; story-artifact validation and frozen/manual-content invariants cover these record changes. Runtime/binary/hosted evidence remains explicitly attributed to c2, with no claim that a future documentation commit's binaries were tested. Exact full assistant-created commit messages pass the owning pinned commitlint; scoped hooks and diff hygiene pass. No `/pushall` is invoked.

This completes only the approved CI-adoption continuation. Parent Story13.6 and sprint stay **in-progress** for actual WebKit system-palette paint, ordinary full topology and independent OI-16/G-4/FLUENT-APP-1/UX/Product/Architect decisions. All seven manual records remain completed Owner-reported Pass with supporting details waived; detailed observation inventory and original frozen intent/baselines are preserved. These independent acceptance requirements are not reopened or satisfied by inference from CI.

Separately observed operator-dispatched Release40588054005 targets c2. Its verify-source and plan jobs pass, prepare-candidate fails, verification handoff succeeds, and production release/publication jobs are skipped. This is outside the bounded CI-adoption qualification; no production publication success is inferred. The final record push was held while that exact-current-main release was active and resumes after its jobs finish. No release dispatch, cancellation, approval, pin change or production action is performed by this continuation.
