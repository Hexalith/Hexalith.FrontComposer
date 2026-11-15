---
title: 'Restore Quality Gate 2c and recapture the current EventStore tuple'
type: 'bugfix'
created: '2026-11-15'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '22f76488e8878e3f879c1dd1b6d7bf75534b2b1b'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current main fails Quality's catalog preparation because its Platform source projection is sealed to an older owner. Gate 2c is skipped, preventing a current Story 16.1 tuple capture and blocking Story 16.3.

**Approach:** Reseal the exact committed selection, prove the seal locally, and push it separately from a documentation activation commit. Qualify that successor through the existing hosted lanes, then append a current tuple record. Propose a bounded capture/approval freeze for G-3 and a historical D1 acceptance disposition; neither proposal supplies migration approval.

## Boundaries & Constraints

**Always:** run ListAgents before policy/submodule work; recheck concurrent changes before mutations. Derive seals from committed objects. Preserve source hashes, trust boundaries, configuration and execution pins. Validate commit messages with pinned commitlint; retain results. Check remote divergence before pushing; stage explicit task paths. Use `run-with-temp` for disposable verification.

**Never:** move gitlinks, edit submodule files, initialize nested submodules, change dependency versions, relax guards, fabricate passing CI, or modify the 2026-11-06 Tuple Record. Do not infer EventStore authority, sign-off, package runtime coverage, or G-3 closure from engineering acceptance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Exact reseal | Committed current root gitlinks | Seven policy values renewed from that selection | Stop if other reviewed inputs differ |
| Activation | Separate push whose event base is landed seal | Catalog preparation and required runtime steps succeed | Inspect exact failed step; no gate bypass |
| Concurrent drift | HEAD or selected inputs change | Re-derive selection before seal; recapture before approval | Preserve other sessions' edits; never force push |
| Capture | Same-attempt required steps all successful | Append dated record naming the tested full SHA | No blocked-attempt or stale-green record |

</frozen-after-approval>

## Code Map

- `eng/dependency-graph-policy.json`: `consumer_source_projections` and `profiles.frontcomposer-catalog-v1.selected_catalog_required_packages`; only runtime configuration change.
- `eng/dependency_graph.py`: `_consumer_projection`, `_owner_edges`, `canonical_digest`, `consumer_configuration`, `collect_graph`, `evaluate_semantics`; reuse unchanged.
- `tests/eng/test_dependency_graph.py`: existing projection and catalog-preparation coverage.
- `eng/dependency-governance.md`: separate seal/activation replay and active event-base policy rules.
- `.github/workflows/quality.yml`: retain catalog acquisition main-ref authorization and all gates/pins; isolate manual main qualification concurrency by run ID after observed repeated cancellation.
- `Directory.Packages.props`, `deps.nuget.props`, `Directory.Build.targets`: catalog import, modes and source-project version metadata.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md`: append immutable tuple evidence; existing record stays byte-identical.
- `_bmad-output/implementation-artifacts/deferred-work.md`, `sprint-status.yaml`: D1–D4 and Story 16.1 review state; append outcomes without changing historical entries.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md`: D1/D3 proposal requiring owner approval.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` — regenerate four projection values: Platform owner, raw inventory digest, root Builds pin, root-selector digest. At baseline owner is `f6f85032598865b9123f16364448bfd02c0ff5b0` and Builds is `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`.
- [x] `eng/dependency-graph-policy.json` — align selected catalog expectations: `Microsoft.CodeAnalysis.Workspaces.Common` 5.12.0, `Verify` and `Verify.XunitV3` 33.4.0; preserve others.
- [x] This spec — record focused checks, create seal commit C, run the two mandatory committed-object proofs, and only then push C. Preserve any pre-existing local review commit unchanged.
- [x] This spec — create non-seal activation A with parent C; replay under C, then push separately after C is remote-visible. Record SHAs/run URLs. C's own CI/Quality is red by design.
- [x] Story 16.1 spec — after A qualifies, append a dated record with exact identities, same-attempt jobs/steps, provider and interaction counts, commands, source/package lane identities, and per-consuming-project MSBuild property evaluation (D4). Recheck current selection before writing.
- [x] Proposal, `deferred-work.md` and `sprint-status.yaml` — seek approval of freeze/cadence/escalation and D1 exception; keep G-3 open. Close D2/D4 only with evidence.

**Acceptance Criteria:**
- Given committed seal C, when exact catalog acquisition with base=C/candidate=C and validation of C run before push, then both exit zero and report `ok: true`.
- Given later non-seal main commit A, when its same-attempt Quality jobs are inspected, then catalog preparation, all four Gate 2c steps and every Epic 9 live acceptance step conclude `success`.
- Given a qualifying current tuple, when recaptured, then full identities match committed objects and evaluated project contexts; the 2026-11-06 record remains byte-identical and no migration approval is claimed.
- Given the D1/D3 proposal, when a maintainer decides, then the historical scope violation, tested-candidate boundary, freeze deadline and escalation responsibility are explicit; no approval is silently transferred to a descendant commit.

## Implementation Notes

- **Qualification window started:** `2026-11-15T17:27:10Z` immediately before the seal push; deadline `2026-11-16T17:27:10Z`. Hold only this session's unrelated commits. No external automation is disabled and no external coordination messages are authorized or sent. Active agent parent was informed of the hold. Current EventStore/FrontComposer/Release approver identities are not established by this engineering approval. Missing authority, failed qualification, timeout, drift or a latest-main lineage conflict goes to the Release Owner with Architect involvement within four working hours; end the hold by the deadline and record one bounded disposition. G-3 remains open.

- **Approval, 2026-11-15:** The user approved both this spec and the course-correction proposal with “approve both and continue.” The original task explicitly authorizes the separate seal/activation pushes and hosted CI observation; that authorization takes precedence over generic Build restrictions on remote operations. No further permission is needed for these steps.
- **Approved proposal implementation:** Apply the proposal's concrete scheduling and candidate-lineage additions to the active PRD G-3/D-12, Epic 16, architecture and epic context, plus append a D1 exception receipt in Story 16.1 outside its immutable record. Record the requesting user's approval of this bounded historical exception without inventing role credentials. Do not claim AC 3 passed historically. Story 16.1 engineering acceptance follows verification; migration approval and G-3 remain separate. Preserve every byte of the 2026-11-06 record.
- **Qualification window:** Start and record a 24-hour window immediately before the seal push; capture within two hours after green required steps. Release Owner owns escalation, with Architect involvement, within four working hours of timeout, failed qualification, missing authority or latest-main lineage conflict. Only hold this session's unrelated commits; no external automation is disabled and no coordination message is sent without explicit authorization. Report observable concurrent drift.
- **Initial evidence:** 37 focused projection/catalog preparation tests passed. The existing refusal reproduces at the baseline. Seven proposed values pass in-memory semantics over 69 edges. Per-project Release and Debug/source evaluations currently select EventStore 5.1.0, but repeat against the hosted green candidate.

## Spec Change Log

- **2026-11-15, observed qualification cancellation:** Subsequent concurrent commits 984b6be79a09e5e5796f75c9c0eb35ab4ccc2707, a6ed4b94b74aacf833c76ba1391a51e383bd6478 and 2809eb366bf0548fe97f52c529892009c1b7ce35 kept superseding automatic Quality. The a6 commit passed all required steps and the formerly failing default lane before cancellation. A temporary-ref diagnostic was considered but rejected before branch creation: the existing catalog step explicitly requires refs/heads/main. Preserve that guard and isolate manual main Quality runs by run ID in their concurrency group; automatic push/PR runs keep their shared ref group and cancel-in-progress behavior. This reuses the existing workflow and changes no execution pin, policy, source, dependency or acceptance rule. KEEP: exact committed-object policy proof, all gates, both immutable records, current tuple/candidate lineage, no inferred approval. This bounded liveness correction implements the requested capture cadence without claiming global freeze coordination or a latest-main exemption.

## Review Triage Log

### Manual concurrency review

All three independent layers reviewed the isolated scheduling diff. Edge review reported no defect. QC-BH1 and QC-VG1 identify the same missing executable regression check: verdict **medium**, future harness integration deferred. Current verification is now supplied by evaluating the actual workflow expression with GitHub-maintained `@actions/expressions@0.3.61` in disposable TMPDIR: eight assertions cover automatic-ref group preservation, distinct manual IDs, manual/push isolation and distinct PR refs; two negative controls reproduce the dispatch-name typo and omitted-run-ID regressions. All pass. The existing main-ref acquisition guard and cancel-in-progress setting are asserted intact; three catalog-preparation workflow tests pass. No repository dependency or committed expression harness is added. Hosted manual qualification will additionally exercise the actual GitHub scheduler.


| Finding | Verdict | Verified evidence and route |
|---|---|---|
| BH1 | medium | The concurrent emitter removes the fixture marker; both current TRX framework cases fail before compilation. Defer to owning SourceTools work; no repair-owned source changed. |
| BH2 | medium | CommandRendererEmitter assigns derived values directly in BeforeSubmit without field notification; validation-revision tracking does not observe those writes. Pre-existing generated-form flow, outside the owner-pin repair; defer. |
| BH3 | medium | The new concurrent freshness check precedes StateHasChanged and target-resolution awaits, and dispatch still reads the shared model. Later mutation is not covered by that check; defer to generated-form ownership. |
| BH4 | false | The emitter assigns _editContext only in OnInitialized; no production path replaces it during authorization/BeforeSubmit. A reflection-only replacement test would exercise an unreachable current transition. |
| BH5 | medium | Root property queries do not evaluate referenced catalog views. The D4 completeness claim needs actual forwarded source contexts or an explicit remaining gap; patch an append-only review addendum and tracking. |
| BH6 | medium | CounterFixture unconditionally references EventStore.DomainService; Release package-mode query leaves EventStorePath empty and supplies no package reference. Patch the context interpretation outside immutable record bytes. |
| BH7 | low | Story 16.1 explicitly judges required steps; PRD G-3 still requires a passing workflow for release. Clarify those distinct gates without weakening the overall-Quality requirement. |
| BH8 | medium | Story frontmatter says done while sprint row and new capture say review. Patch frontmatter to in-review; acceptance remains pending overall Quality and review disposition. |
| BH9 | medium | Approved proposal requires repository coordination, while observed hold covered only this session. Document that coordination was not achieved; do not silently remove the prerequisite. |
| BH10 | medium | Four working hours has no supplied business calendar. Add an explicit conservative UTC due time for the observed incident, preserving the outer deadline and named-owner responsibility. |
| BH11 | low | Expected seal refusal is distinct from successor qualification failure. Clarify that the planned transition refusal alone does not trigger incident escalation. |
| EC1 | high | No upstream field null guard exists in emitted SubmitAsync; the new alias condition leaves nullable _editContext dereferenced in the net10 branch. Reviewer reproduced CS8602 with the real API; defer concurrent source regression. |
| EC2 | medium | Same removed marker and moved locals as BH1; both framework fixtures throw before compiler execution. Defer with BH1. |
| VG1 | medium | Verification reviewer independently confirmed the orphaned extraction marker and its current failed invocations. Same deferred root cause as BH1. |

## Verification

### Committed seal C

`50def1fe238787e9365c93991e3dc83af8c2e299` has parent `22f76488e8878e3f879c1dd1b6d7bf75534b2b1b`. The pre-existing review commit is preserved unchanged. Pinned `@commitlint/cli@21.2.4` validated the exact full seal message with zero problems and warnings; the commit hook and staged committed-files guard passed (two task paths). No gitlink changed.

Before push, both commands exited zero and returned `ok: true`:

- `run-with-temp python3 -B eng/dependency_graph.py --root . acquire-catalogs --event-base 50def1fe238787e9365c93991e3dc83af8c2e299 --candidate 50def1fe238787e9365c93991e3dc83af8c2e299`
- `run-with-temp python3 -B eng/dependency_graph.py --root . validate --commit 50def1fe238787e9365c93991e3dc83af8c2e299`

`git fetch origin main` reported no remote-only commits (`0 2` behind/ahead, including the pre-existing review commit). The separate `git push origin main` succeeded; `git ls-remote origin refs/heads/main` confirms C is remote-visible. Its push uses the old event-base policy, so C's own catalog preparation/graph qualification is expected to refuse and cannot supply tuple evidence.


### Activation A and local replay

`d654bb412bfc54635b356527a89988a99f91992e` has immediate parent C `50def1fe238787e9365c93991e3dc83af8c2e299`. Its nine changed paths are documentation under `_bmad-output/`; policy, configuration, workflows, dependency versions and gitlinks are unchanged. Pinned commitlint 21.2.4 validated its exact full message with zero problems/warnings; the committed-files guards passed. The actual 2026-11-06 record is 8,192 bytes and byte-identical; the anchored section comparison avoids its earlier inline heading reference.

The full `eng/dependency-governance.md` activation replay ran under `run-with-temp` with its fresh object stores beneath wrapper-provided `$TMPDIR`. All four commands exited zero and returned `ok: true`:

```bash
candidate=d654bb412bfc54635b356527a89988a99f91992e
seal=50def1fe238787e9365c93991e3dc83af8c2e299
python3 -B eng/dependency_graph.py acquire-catalogs --event-base "$seal" --candidate "$candidate"
python3 -B eng/dependency_graph.py validate --commit "$candidate"
python3 -B eng/dependency_graph.py acquire --event-base "$seal" --candidate "$candidate" --destination "$TMPDIR/objects"
python3 -B eng/dependency_graph.py --root "$TMPDIR/objects" diff --event push --event-base "$seal" --candidate "$candidate"
```

Both graphs contain 69 edges, `changes: []` and `affected_modules: []`; the active policy commit is C. The temporary stores were removed before the separate push. `git fetch origin main` confirmed C remained the remote tip (`0 1` behind/ahead); A's `git push origin main` succeeded without force or bypass-rule warnings. In the planning documents' generic candidate-C notation, A is the tested candidate; repair C denotes only the seal.

C's hosted runs are [Quality 43741183299](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43741183299) and [CI 43741183546](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43741183546). They are not qualifying evidence for a current tuple. A's required same-attempt steps succeeded, but a later source fix cancelled the remaining jobs; this is historical qualification evidence only, not the successor tuple.

### Concurrent source fix and fresh evidence observation

While this session held unrelated commits and made no follow-up push, remote main advanced to `923988d855cd6131b4287e3da763f12baf60ab22` (`fix(forms): refuse models changed after asynchronous validation`) at 2026-11-15 17:49:35 UTC. This is an unrelated concurrent commit, preserved unchanged and excluded from this repair's scope: `fc-expandinrow.js`, `CommandFormEmitter.cs`, `AsyncCommandFormValidationTests.cs`, `CiGovernanceTests.cs`, two CommandFormEmitter snapshots and `tests/ci-governance/stage_release_state.py`. It changes no gitlink, dependency policy, MSBuild configuration or seal input.

The original attempt ended on that concurrent fix. It triggered the approved drift/latest-main escalation condition; no named Release Owner/Architect rescheduling receipt is claimed and no coordination message was sent externally. The parent authorized following fresh current-main evidence within the original outer deadline `2026-11-16T17:27:10Z`, without extension. This is not migration approval or an owner disposition. The Release Owner remains responsible for a bounded disposition, with Architect involvement, within four working hours. Missing named migration authority and the later record-commit lineage issue remain open.

After rechecking the remote tip and exact selected inputs, `run-with-temp python3 -B eng/dependency_graph.py --root . acquire-catalogs --event-base d654bb412bfc54635b356527a89988a99f91992e --candidate 923988d855cd6131b4287e3da763f12baf60ab22` and `validate --commit 923988d855cd6131b4287e3da763f12baf60ab22` both exited zero with `ok: true`. Local main fast-forwarded to the concurrent source fix without changing the dirty repair notes. No new seal is needed: the existing seven values and all other reviewed inputs still match committed objects. Follow [Quality 43743271598, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43743271598/attempts/1) as the current candidate; append no cancelled/stale-current tuple.

A's historical attempt 1 passed catalog preparation and all four Gate 2c steps (last at 17:48:36 UTC), and all 27 Epic 9 steps. Provider tests: 80 passed, zero failed/errors/skipped/not run. Contract tests: three succeeded, zero failed/skipped. Artifact `14173581401` confirms 19 requested/reported/passing interactions, 19 states/setup/teardown events, clean lifecycle/completeness and no reason codes. Its expected/observed source is `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`, observed source assembly version `5.1.0+fdb59895975e86a1e234325a01237f259cdbd53b`. AppHost final JUnit counts are one test, zero failures/skips/errors; Playwright marks that test flaky (successful retry 2), so no first-attempt browser pass is claimed. All required source checks passed before cancellation, but no A tuple is appended as current after the concurrent fix.

### Second concurrent drift — 2026-11-15

At 18:03:43 UTC, unrelated concurrent commit `8ce46cfc1293925459c8b6af15ef7719cf0fc071` (`fix(ci): trust the exact mutation container checkout`) advanced remote main, changing exactly one line in `.github/workflows/mutation-property-nightly.yml`. This session did not create, amend or push that change. It changes no Quality workflow, gitlink, policy, dependency version or MSBuild configuration. The commit is outside this repair's scope and is preserved unchanged.

That push cancelled [Quality 43743271598, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43743271598/attempts/1) at the story-artifact tests. Although catalog preparation succeeded at 17:55:43 UTC, all four Gate 2c steps were skipped and Epic 9 invalid-bearer verification was cancelled. This attempt does not qualify and supplies no tuple record. [Quality 43744582233](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233) belongs to the new commit.

This is another observed drift/escalation event. No named Release Owner/Architect rescheduling receipt, external automation hold, additional approval or new 24-hour deadline is inferred. The original outer deadline remains `2026-11-16T17:27:10Z`. Local source changes and repair notes are preserved; no follow-up push occurred. Any fresh evidence observation must keep the exact-current-candidate and same-attempt gates.

The parent authorized one bounded read-only observation of the already-running current-main run `43744582233`, within the unchanged original deadline, while requesting a 20–30 minute main-push hold from the user. That request is coordination, not migration approval or a named Release Owner decision. Exact `acquire-catalogs --event-base 923988d855cd6131b4287e3da763f12baf60ab22 --candidate 8ce46cfc1293925459c8b6af15ef7719cf0fc071` and `validate --commit 8ce46cfc1293925459c8b6af15ef7719cf0fc071` both exited zero with `ok: true` under `run-with-temp`. Local main fast-forwarded safely with dirty repair notes preserved. No new seal or push was made. A further cancelling push ends this implementation observation and returns the preserved work to the parent; no private retry loop is authorized.

### Current capture and overall Quality failure — 2026-11-15

Current main `8ce46cfc1293925459c8b6af15ef7719cf0fc071` qualified under the individual-step rule in [Quality 43744582233 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/attempts/1): catalog preparation, all four Gate 2c steps and all 27 Epic 9 steps succeeded. The last required success was `2026-11-15T18:26:36Z`; the immutable capture was appended at `2026-11-15T18:44:11Z`, before the two-hour deadline `2026-11-15T20:26:36Z`. Immediately before writing, HEAD, origin/main and a fresh remote read were all `8ce46cfc1293925459c8b6af15ef7719cf0fc071`. The source selection remains EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` and catalog default `5.1.0`.

**The overall Quality run failed.** Later Gate 3a default unit/bUnit results were five failed, 6,680 succeeded, zero skipped. All seven other jobs succeeded. Shared [CI 43744582708](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582708) succeeded. No overall-green run, release readiness, Story 16.1 acceptance, migration approval or G-3 closure is asserted. The parent explicitly directed capture under the approved individual-step rule and will review acceptance separately; Story 16.1 stays at review.

Current same-attempt counts: Contract three succeeded, zero failed/skipped; provider tests 80 passed, zero errors/failed/skipped/not run; live provider 19 requested/reported/passed interactions, 19 states/setup/teardown events and all lifecycle flags true. Artifact `14174636740` expires `2026-11-29T18:26:36Z`. Epic artifact `14173977718` has one passed browser test, zero failures/skips/errors and zero flaky results; all four invalid-bearer surfaces reject with HTTP 401. These counts were read from the completed current job logs and existing artifacts, not inferred from cancelled attempts.

All six per-consuming-project MSBuild contexts were repeated after current required success: AppHost/UI/CounterFixture, Release package and Debug source, each net11.0, each EventStore `5.1.0`, all SDK 4.2 views false. Selected dependency configurations and checkout commits are exact and clean. Source lanes exercise the source gitlink; package property evaluation and shared CI establish no additional package-mode runtime compatibility. D2's catalog/Gate 2c blocker and D4's evaluation gap now have direct current evidence; their technical closure does not dispose of the later default-lane failures.

The complete new tuple record includes every required step, exact identities, commands, provider/interaction/browser counts, source/package lane scope and the five bounded residuals below. Earlier record bytes remain unchanged. The original window deadline stays `2026-11-16T17:27:10Z`; observed drift and missing named authority still require Release Owner/Architect disposition, and record descendants receive no transferred approval.

#### Bounded default-lane residuals (outside this repair)

| Failed test invocation | Observed cause / attribution |
|---|---|
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandTargetGeneratedFormTests.SynchronouslyBlockingProvider_DeadlineStillAllowsDispatch` | Observed `TimeoutException` waiting up to two seconds for `provider.Started`; causal origin is unproven. The test file predates and is unchanged by both concurrent commits. |
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 11)` | `ArgumentOutOfRangeException(startIndex)`: the fixture searches the old emitted `_editContext` validation marker, then calls `IndexOf` with -1. Concurrent `923988d855cd6131b4287e3da763f12baf60ab22` renamed that marker to `validatedEditContext`; the fixture was not updated. |
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 10)` | Same proven stale-marker failure introduced by the concurrent forms change; no framework compilation verdict was reached. |
| `Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle.UlidFactoryTests.ProductionCommandIdentityPaths_DoNotUseGuidIdentityApis` | Broad source-token assertion finds `Guid.TryParse(` in the emitter's GUID-valued field parsing. Both that occurrence and the test already exist at baseline `22f76488e8878e3f879c1dd1b6d7bf75534b2b1b`; no command-identity use is inferred from this scan. |
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererWrapperIntegrationTests.GeneratedForm_SubmitAccepted_RendersLifecyclePhasesAndKeepsFormPresent` | Observed bUnit assertion timeout: expected wrapper class `fc-lifecycle-pulse`, actual `fc-lifecycle-wrapper `. The test file is unchanged by both concurrent commits; causality and repeatability are unproven. |

The test-results artifact is `14175076344`; all five are failed TRX entries from the default lane of this same run. The two marker errors are proven consequences of concurrent `923988d855cd6131b4287e3da763f12baf60ab22`. The GUID parser occurrence and its broad test predate the baseline; both observed timeout test files also predate it, but their causal relationship to later generated-form changes is unproven. No failure is attributed to the seven policy changes: those change only committed selection seals/expectations and do not change the already-selected catalog or compiled source. No unrelated source/test fix or guard relaxation was made. Owning SourceTools, generated-form and lifecycle maintainers must reproduce/fix their bounded cases; neither timeout is labeled flaky without rerun evidence.

### Local reseal checks — 2026-11-15

- Derived from committed baseline `22f76488e8878e3f879c1dd1b6d7bf75534b2b1b`, preserving the pre-existing local review commit unchanged. The only policy changes are the four projection values and three catalog expectations authorized above.
- Platform owner `f6f85032598865b9123f16364448bfd02c0ff5b0`; raw inventory SHA256 `874d7ff434413ef9e11324fd4ecfae1bd9dda527f8cc405e89ffa5a0e5502fd0`; root Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`; root-selector SHA256 `5632f8e8b96f16d6274597b9c38bdec0aa14fbfa824b7457f99235ab1d7eaeaf`.
- Re-derived and compared the unchanged `.gitmodules` digest, seven inventory-only identities, canonical source hashes and consumer configuration digest before writing. In-memory `assert_policy_well_formed`, `collect_graph` and `evaluate_semantics` succeed over 69 edges and eight semantic selectors. This is preliminary evidence; committed-object proofs follow seal creation before push.
- `run-with-temp python3 -B -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests tests.eng.test_dependency_graph.QualityCatalogPreparationWorkflowTests tests.eng.test_dependency_graph.ApprovedProjectionExpectationTests` — exit 0; 37 tests passed in 11.685 seconds. Safe full command output is ignored local scratch, not a committed evidence artifact.
- The complete 2026-11-06 Tuple Record is snapshotted as original bytes; SHA256 `8f4755ff8a1d78951c3fc8da5104d0b371839f04cd0802cdcd2a23839330257d`. It will be compared after the append-only recapture.


- `run-with-temp python3 -B -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests tests.eng.test_dependency_graph.QualityCatalogPreparationWorkflowTests tests.eng.test_dependency_graph.ApprovedProjectionExpectationTests` — existing focused suites must pass; baseline: 37 tests pass across two invocations.
- `python3 eng/dependency_graph.py --root . acquire-catalogs --event-base <C> --candidate <C>` then `python3 eng/dependency_graph.py --root . validate --commit <C>` — mandatory exact committed seal proof before push.
- Replay activation as documented in `eng/dependency-governance.md`, with temporary object stores beneath wrapper-provided `$TMPDIR`; every command must succeed and dependency diff must be empty.
- Inspect hosted job/step metadata, logs and existing artifacts from one run attempt; retain only safe counts/commands/URLs in the spec.
- Compare the original Tuple Record's exact bytes before/after; inspect staged paths and gitlink diff; run `git diff --check` and the committed-files guard.

### Final local verification — 2026-11-15

- Current capture assertions compare the explicit committed gitlinks/catalog, exact attempt-1 `head_sha` in both jobs, all five build-side required successes and all 27 Epic 9 successes; captured within the two-hour deadline. Overall Quality failure and all five failed TRX invocations are explicit.
- Repeated six current MSBuild property queries after required success; each selects 5.1.0 at net11.0 in the documented source/package context. Selected dependency configurations and checkout identities remain clean/exact.
- Exact 2026-11-06 working-tree bytes are unchanged (8,192-byte body, SHA256 `8f4755ff8a1d78951c3fc8da5104d0b371839f04cd0802cdcd2a23839330257d`); the original committed-object record also compares equal after Git's CRLF normalization.
- `git diff --check` and `git diff --cached --check` — exit 0. All seven follow-up files retain CRLF and remain under `_bmad-output/`; no gitlink changes occur anywhere in this repair range.
- `bash eng/check-committed-files.sh --staged` — exit 0, seven files pass; `bash eng/check-committed-files.sh` — exit 0, 6,454 tracked files pass.
- Final follow-up documentation is staged and uncommitted for the parent review workflow. No post-activation push is made. Story 16.1 remains `review`; engineering acceptance, named migration decisions, G-3 closure and final commit/push are not claimed.

## File List

- `eng/dependency-graph-policy.json` — seven exact reseal/catalog-expectation values, committed in C.
- `_bmad-output/implementation-artifacts/spec-restore-quality-gate-2c-current-tuple.md` — approval, window, proofs, drift/scope dispositions, current qualification and residuals.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` — bounded historical exception and immutable current tuple; old record unchanged.
- `_bmad-output/implementation-artifacts/deferred-work.md` — append-only D1–D4 outcomes and bounded default-lane residuals.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — chronological evidence notes and current Story 16.1 engineering-done state; migration decisions remain pending.
- `_bmad-output/implementation-artifacts/epic-16-context.md`, `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md` — approved cadence/lineage planning rules.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md` — requesting-user approval and execution outcomes.

Unrelated concurrent commits `923988d855cd6131b4287e3da763f12baf60ab22` (seven source/test paths) and `8ce46cfc1293925459c8b6af15ef7719cf0fc071` (nightly mutation workflow) are excluded from repair scope and preserved unchanged. No final documentation commit or push is made before parent review.

## Review corrections — D4 scope, release gate and incident, 2026-11-15

Both Tuple Records remain byte-identical; Story 16.1 frontmatter is now `in-review` to match its review state. Its separate append-only Review Addendum withdraws the invalid CounterFixture Release/package interpretation and the earlier blanket D4 closure. Exact live-build globals and evaluated AppHost/UI edge metadata reproduce sixteen immediate source-reference contexts across fourteen projects, plus sixteen standalone controls. Parties SDK 4.2 views are true and select standalone 4.2.0, while inherited global EventStore 5.1.0 remains effective. CounterFixture's inherited Debug source path exists; its standalone Release path is empty/dangling. No version/configuration/source change was made.

D4 is resolved only for those evaluated immediate edge contexts and controls. Recursive/transitive restore/build properties, path-only resource launches and package-runtime coverage remain unproved; D4's completeness gap stays open. This addendum supersedes the earlier top-level-only completeness wording without rewriting either immutable record. D2's demonstrated catalog/Gate 2c repair is unchanged.

The current capture satisfies only Story 16.1's individual-step evidence rule. Its overall Quality failure leaves G-3's unchanged passing-workflow release condition unmet. The approved repository-wide coordination prerequisite was not achieved and remains an open Release Owner action with Architect involvement; no session-only exception removes it. No business calendar is assumed: first drift at `2026-11-15T17:49:35Z` has conservative disposition due time **`2026-11-15T21:49:35Z`**, four elapsed hours later. No named owner disposition receipt is claimed and the outer deadline remains `2026-11-16T17:27:10Z`. Expected old-policy refusal of the planned seal is not itself successor required-step qualification failure; the separate drift/authority/timeout triggers remain effective.

Only targeted MSBuild property/item evaluations, record/frozen-section byte checks and edited-document assertions were run for these review fixes; no broader test suite, build or remote mutation was run.

Targeted review verification passed: exact byte comparisons for both Tuple Records (SHA256 `f8dea0813fddef62a3bb645b56660208b104b87de886d743bf77b27736bbd8b7` and `fe3e007c951caebf763e9836dc3afb1a1f3ee250be9acb230f7a92c1667d0c73`), frozen-intent preservation, review frontmatter, nine edited-document CRLF/consistency assertions, and edited-path whitespace checks. The sixteen forwarded plus sixteen standalone MSBuild evaluations succeeded; query-command inspection confirms no synthetic TargetFramework global and empty evaluated SetTargetFramework/SetConfiguration/SetPlatform metadata. No build-target framework negotiation or recursive completeness is claimed. Pre-existing staged and unstaged parent notes remain intact; these review corrections are left in the working tree for parent staging/verification.

### Bounded main-only qualification liveness — 2026-11-15

The stable-branch option was rejected before creating or pushing a branch because the existing catalog-acquisition guard authorizes manual diagnostics only on `refs/heads/main`. The one-line concurrency expression instead preserves the exact old automatic group `quality-<github.ref>` and gives each workflow_dispatch run `quality-manual-<github.run_id>`. The main-only guard, event-base selection, permissions, all job/step criteria and execution pins stay byte-identical. This is an execution detail for the approved capture cadence, not an exemption for later main changes or a migration approval.

- `run-with-temp python3 -B -m unittest tests.eng.test_dependency_graph.QualityCatalogPreparationWorkflowTests` — 3 tests pass.
- Actual workflow-expression evaluation with temporary `@actions/expressions@0.3.61` — 8 positive assertions and 2 negative controls pass. Installation, script and npm cache were confined to wrapper TMPDIR and removed; only a safe result log remains ignored locally. The first local harness attempt used an outdated Dictionary constructor form, failed before expression evaluation, and was corrected to the current public add method before these successful checks.
- The concurrent source fixes 984b6be79a09e5e5796f75c9c0eb35ab4ccc2707 and a6ed4b94b74aacf833c76ba1391a51e383bd6478 are preserved. They address the earlier emitter/fixture/default-lane residuals; a6 passed default tests before a further mutation-workflow-only push 2809eb366bf0548fe97f52c529892009c1b7ce35 cancelled its remaining Quality checks. No cancelled run is labelled overall green.

### Green successor and engineering completion — 2026-11-15

The current main tuple is `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. Primary automatic main-push Quality 43755163930 attempt 1 completed **success in all eight jobs**; supplemental isolated main manual Quality 43755164106 also completed success at the same commit. Shared CI 43755164227 succeeded. The new immutable same-day Tuple Record is explicitly labeled the later green successor and uses only primary attempt-1 metadata/log/artifact counts: Contract 3 passed, provider tests 80 passed, live interactions 19/19 passed, default lane 6,685 passed/zero failed or skipped, all 27 Epic 9 steps successful, four invalid-bearer HTTP 401 rejections. Primary browser JUnit has one final passing test, while Playwright explicitly marks one flaky result (retry 0 failed, retry 1 passed); no first-attempt browser pass is claimed.

Fresh HEAD/origin/direct remote reads all matched the full tested SHA before capture. The source and catalog identities stay EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`, default 5.1.0, with no root preset. The 35 actual source consumer contexts across 27 projects were refreshed on e0: own net11.0, effective 5.1.0, source true, no current root/edge/global discrepancies. Eight consumer contexts have Parties SDK42 true; seven standalone project controls select 4.2.0 while hosted inherited selection stays 5.1.0. AppHost Release directly selects the Aspire package 5.1.0; UI Release supplies only a property value with no direct EventStore package; CounterFixture Release stays invalid/excluded. Four nonconsumer Contracts/Schema framework negotiation cases and four dependent edges, launch-time properties, restored package assets and package-runtime coverage remain explicit limits.

Source fixes `984b6be79a09e5e5796f75c9c0eb35ab4ccc2707` / `a6ed4b94b74aacf833c76ba1391a51e383bd6478` repair the recorded validation/fixture/test issues; primary TRX artifact 14179161540 reports all five former failed invocations Passed. Historical failed run 43744582233 and cancelled observations are unchanged. `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` implements protected manual concurrency while preserving the main-only acquisition guard, event-base policy, automatic group and all pins/gates. Standalone expression checks and hosted success do not install a durable CI expression harness; that future integration remains a scoped residual.

The separate engineering acceptance receipt records parent acceptance of Story 16.1 capture and the requesting-user-approved D1 historical exception. It creates no migration approval, role/authority transfer, named owner receipt or G-3 closure. Story 16.1 frontmatter/sprint state are done; pending Stories 16.2/16.3 are unchanged. The current green workflow condition is evidenced only for the exact tested commit. Named migration approvals and candidate/record-descendant lineage resolution still block G-3. Repository-wide coordination remains unachieved/open, with conservative incident due `2026-11-15T21:49:35Z` and unchanged outer deadline `2026-11-16T17:27:10Z`.

Remaining review residuals stay separately scoped: BH2 predates the owner-pin repair; BH3 concerns the concurrent freshness-check lineage from 923988d855cd6131b4287e3da763f12baf60ab22; QC-BH1/QC-VG1 concerns durable regression integration for the e0 scheduling expression. No fix of those scenarios is inferred from green CI. The primary browser retry and all D4 coverage boundaries remain disclosed. Only documentation paths are changed in this final pass; no source/workflow/submodule edits, gitlink moves, commit or push are made.

#### Completion checks for the later green successor

- Primary run identity/attempt/event and all eight successful jobs were checked against `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`; only its two primary job logs and existing primary artifacts supplied record counts. All five earlier failed invocations report Passed in primary TRX.
- Fresh HEAD, origin/main and direct remote reads agree at e0 before capture and after the final record assertions. Capture time `2026-11-15T20:32:40Z` precedes the primary required-step deadline `2026-11-15T22:12:06Z` and the original outer deadline.
- Both prior Tuple Records remain exact bytes (SHA256 `f8dea0813fddef62a3bb645b56660208b104b87de886d743bf77b27736bbd8b7`, `fe3e007c951caebf763e9836dc3afb1a1f3ee250be9acb230f7a92c1667d0c73`); the complete Review Addendum also remains exact bytes (SHA256 `089f94b3df9a0d5e20fd058e52a6c9462d65553f4196521d8505497622b39194`). Both frozen intent blocks are unchanged.
- Current consumer/standalone/Release property checks pass in the stated scope; no hidden framework, restore, launch or package-runtime completeness is inferred. Both done frontmatters and the done sprint row match the separate parent engineering acceptance; 16.2/16.3 stay backlog and G-3 stays open.
- `git diff --check` / `git diff --cached --check` pass. Only nine explicit `_bmad-output/` documentation paths are staged; CRLF is retained, source/workflows/configuration/gitlinks are unchanged by this documentation pass. `bash eng/check-committed-files.sh --staged` passes nine files; its full tracked check passes 6,454 files.
- No final commit or push has been made. The parent retains final diff inspection and the already-authorized commit/push action; that later commit cannot inherit the tested tuple's migration approval.

### Parent final attribution and completion audit — 2026-11-15

Repository-wide `run-with-temp python3 -B eng/validate-story-artifacts.py` and the current completion-scope story check with `--base e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` pass. Shared File List accounting now includes the explicitly approved companion planning documents used by activation A. Fifteen pre-existing/shared-ledger or companion-audit commits were individually inspected and declared with their real shared/process role; no unrelated source or gitlink ownership is claimed. The unchanged full-history strict check fails solely on historical dd575b67c82c377bd17c34fb3d14366277d2ac62, as disclosed in the approved D1 exception; no guard change or suppression is introduced.

The parent read the final tuple's primary-only fields, all requested step conclusions, per-project source/package-context limits and successful-retry disclosure. Both green run metadata and all immutable previous bytes were independently checked. Final repository/file guards pass (6,454 tracked files), and both final documentation messages passed pinned commitlint21.2.4 with zero problems/warnings. Companion planning and capture delivery are separate commits so the new Story16.1 capture commit changes only its spec and sprint-status.yaml; its exact new-commit attribution is checked after creation. Remaining reviewer deferrals and G-3 named approvals/lineage remain explicit.
