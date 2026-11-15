---
title: 'Restore Quality Gate 2c and recapture the current EventStore tuple'
type: 'bugfix'
created: '2026-11-15'
status: 'in-progress'
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
- `.github/workflows/quality.yml`: catalog preparation, four Gate 2c steps, and every `epic9-live-acceptance` step; inspect without editing.
- `Directory.Packages.props`, `deps.nuget.props`, `Directory.Build.targets`: catalog import, modes and source-project version metadata.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md`: append immutable tuple evidence; existing record stays byte-identical.
- `_bmad-output/implementation-artifacts/deferred-work.md`, `sprint-status.yaml`: D1–D4 and Story 16.1 review state; append outcomes without changing historical entries.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md`: D1/D3 proposal requiring owner approval.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` — regenerate four projection values: Platform owner, raw inventory digest, root Builds pin, root-selector digest. At baseline owner is `f6f85032598865b9123f16364448bfd02c0ff5b0` and Builds is `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`.
- [x] `eng/dependency-graph-policy.json` — align selected catalog expectations: `Microsoft.CodeAnalysis.Workspaces.Common` 5.12.0, `Verify` and `Verify.XunitV3` 33.4.0; preserve others.
- [x] This spec — record focused checks, create seal commit C, run the two mandatory committed-object proofs, and only then push C. Preserve any pre-existing local review commit unchanged.
- [ ] This spec — create non-seal activation A with parent C; replay under C, then push separately after C is remote-visible. Record SHAs/run URLs. C's own CI/Quality is red by design.
- [ ] Story 16.1 spec — after A qualifies, append a dated record with exact identities, same-attempt jobs/steps, provider and interaction counts, commands, source/package lane identities, and per-consuming-project MSBuild property evaluation (D4). Recheck current selection before writing.
- [ ] Proposal, `deferred-work.md` and `sprint-status.yaml` — seek approval of freeze/cadence/escalation and D1 exception; keep G-3 open. Close D2/D4 only with evidence.

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

## Review Triage Log

## Verification

### Committed seal C

`50def1fe238787e9365c93991e3dc83af8c2e299` has parent `22f76488e8878e3f879c1dd1b6d7bf75534b2b1b`. The pre-existing review commit is preserved unchanged. Pinned `@commitlint/cli@21.2.4` validated the exact full seal message with zero problems and warnings; the commit hook and staged committed-files guard passed (two task paths). No gitlink changed.

Before push, both commands exited zero and returned `ok: true`:

- `run-with-temp python3 -B eng/dependency_graph.py --root . acquire-catalogs --event-base 50def1fe238787e9365c93991e3dc83af8c2e299 --candidate 50def1fe238787e9365c93991e3dc83af8c2e299`
- `run-with-temp python3 -B eng/dependency_graph.py --root . validate --commit 50def1fe238787e9365c93991e3dc83af8c2e299`

`git fetch origin main` reported no remote-only commits (`0 2` behind/ahead, including the pre-existing review commit). The separate `git push origin main` succeeded; `git ls-remote origin refs/heads/main` confirms C is remote-visible. Its push uses the old event-base policy, so C's own catalog preparation/graph qualification is expected to refuse and cannot supply tuple evidence.


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
