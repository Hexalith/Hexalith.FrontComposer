---
title: 'Story 9.7: Add story-ID and commit-scope evidence'
type: 'chore'
created: '2026-08-25'
status: 'done'
baseline_commit: 'ceae00a4f9788222ed19153acfc05d68d0bc85d1'
story_id: '9.7'
review_loop_iteration: 3
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-retro-2026-08-11.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 9.7's strict evidence gate cannot attribute its own published delivery commit because that self-enforcing commit predates the exact `9.7` subject rule and also contains visible unrelated paths. Rewriting published history is forbidden.

**Approach:** Add one hard-authorized `bootstrap-owned` disposition for the exact Story 9.7 baseline/delivery tuple. It counts only File List paths as story-owned, keeps every unlisted path visible, and leaves ordinary story-ID, `shared`, `process`, unmapped, and interleaving behavior unchanged.

## Boundaries & Constraints

**Always:** Authorize only story `9.7`, baseline `ceae00a4f9788222ed19153acfc05d68d0bc85d1`, and commit `fd04bdd97fbdd4976a0f213e46a316be199fd8a9`. Require the commit to be a non-merge whose sole parent is that baseline, not match `9.7`, and touch both listed guard paths `eng/validate-story-artifacts.py` and `eng/tests/test_validate_story_artifacts.py`. Accept at most one full-SHA `bootstrap-owned` declaration with a non-empty reason. Reconcile only listed paths; report all unlisted paths. Classify `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` as `shared` because its release-compatibility work later touched shared Story 9.7 paths.

**Ask First:** Any other bootstrap tuple, reusable authorization source, disposition kind, ownership rule, or commit/status semantic.

**Never:** Rewrite history, auto-detect bootstrap commits, accept a wildcard or movable ref, let story text authorize arbitrary bootstrap ownership, suppress unlisted paths, treat the whole bootstrap commit as story-owned, weaken ordinary unmapped/interleaved failures, or use path-level unrelated declarations as commit exceptions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Authorized bootstrap | Exact story/baseline/SHA/parent and both listed guard paths | Listed paths reconcile as `bootstrap-owned`; unlisted paths remain reported | Pass |
| Wrong authorization | Any story, baseline, SHA, parent, merge shape, or matching subject differs | No ownership is granted | Fail closed |
| Invalid declaration | Multiple bootstrap rows, stale/short SHA, empty reason, or missing guard path | No ownership is granted | Fail closed |
| Later shared commit | Exact `2dcc43fe...` declaration with `shared` and reason | Commit remains visible but contributes no ownership | Pass |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py:574-598,753-925,969-1020` -- disposition grammar, canonical range evidence, hard-bound authorization including the immutable bootstrap path set, listed-path reconciliation, and classification-aware path labels.
- `eng/tests/test_validate_story_artifacts.py:664-900` -- temporary Git fixtures and disposition fail-closed coverage; add pure authorization checks, canonical-artifact binding, CLI integration, and report-label cases.
- `.agents/skills/bmad-build/step-04-review.md:40-46` -- reviewer-facing strict-gate contract; document the one-time exception without making it routine guidance.
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md:22-24` -- operator contract for commit dispositions and anti-bypass behavior.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py`, `eng/tests/test_validate_story_artifacts.py`, workflow files, CI pin, and operator checklist -- preserve the delivered exact story-ID, canonical-ref, ancestry, per-commit path, merge, workspace, and File List evidence behavior.
- [x] `eng/validate-story-artifacts.py` -- add the hard-bound `bootstrap-owned` authorization, bind it to the canonical Story 9.7 artifact and immutable bootstrap-owned path set, reconcile only those authorized listed paths, and label listed paths as owned only for ownership-contributing classifications.
- [x] `eng/tests/test_validate_story_artifacts.py` -- prove the exact authorization succeeds and every artifact, tuple, topology, declaration, guard-path, and mutable-File-List deviation fails closed; prove shared/process/unmapped paths never receive an owned label and unlisted paths stay visible.
- [x] `.agents/skills/bmad-build/step-04-review.md` and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` -- document the one-time human-authorized recovery and prohibit routine substitution for correct commit attribution.
- [x] `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md` -- record both exact dispositions and refreshed verification evidence.

**Acceptance Criteria:**
- Given the exact authorized historical tuple and canonical Story 9.7 artifact, when strict validation runs to `HEAD`, then `fd04bdd9...` is reported as `story-id=no-match | disposition=bootstrap-owned`, only its listed paths are labeled `owned`, its unlisted paths remain visible as `unowned`, and `2dcc43fe...` is visible as `shared` with listed paths labeled `listed-unowned` rather than contributing ownership.
- Given any authorization or structural deviation, when validation runs, then it fails with actionable evidence and grants no bootstrap ownership.
- Given ordinary matching, unmapped, interleaved, `shared`, or `process` commits, when validation runs, then their existing classification and ownership semantics remain unchanged.

## Spec Change Log

- 2026-08-25, review loop 1 -- The strict gate exposed a self-bootstrap contradiction: the commit introducing exact Story 9.7 attribution could not satisfy a rule that did not yet exist, and published history cannot be rewritten. With explicit human approval, the frozen contract now permits one exact full-SHA `bootstrap-owned` tuple and the later release-compatibility commit is declared `shared`. This avoids the known-bad alternatives of history rewriting, generic exemptions, hidden interleaving, or whole-commit ownership. KEEP: canonical SHA/ancestry checks, exact ID boundaries, per-path visibility, separate workspace evidence, ordinary disposition semantics, existing CI enforcement, and the delivered validator behavior outside this exception.
- 2026-08-25, review loop 2 -- Adversarial review found the non-frozen Design Notes called every File List member `owned`, contradicting the approved rule that `shared` and `process` commits contribute no ownership. The report contract now labels a listed path `owned` only for `owned`, `interleaved`, or `bootstrap-owned` classifications and uses `listed-unowned` for listed paths in every non-owning classification. The authorization must also bind the canonical Story 9.7 artifact so a copied fixture cannot reuse it. This avoids misleading staging evidence and reusable recovery artifacts. KEEP: the exact code-bound story/baseline/SHA/sole-parent tuple, both listed and touched guard paths, fail-closed declaration handling, listed-only reconciliation, unlisted-path visibility, pure authorization tests, historical CLI integration, and operator anti-copy guidance.
- 2026-08-25, review loop 3 -- Adversarial review proved that binding `bootstrap-owned` to the canonical artifact still let later story text broaden historical ownership by adding either currently unowned bootstrap-commit path to the mutable File List. The non-frozen design now requires the exact twelve-path bootstrap-owned set to be code-bound and requires any changed intersection between the bootstrap commit and the File List to fail closed. This avoids converting the story artifact into an authorization source while still allowing unrelated future File List entries that the bootstrap commit never touched. KEEP: canonical artifact binding; the exact story/baseline/SHA/sole-parent/subject tuple; both guard paths; fail-closed malformed and multiple declarations; classification-aware `owned`, `listed-unowned`, and `unowned` labels; shared/process non-ownership; full per-path visibility; pure authorization tests; historical CLI integration; and operator anti-copy guidance.

## Commit Scope Dispositions

- `fd04bdd97fbdd4976a0f213e46a316be199fd8a9` | `bootstrap-owned` | self-enforcing Story 9.7 delivery predates its exact commit-ID gate; human-approved one-time recovery bound to the immutable baseline and guard paths
- `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` | `shared` | later release-compatibility work changed shared CI, governance, and deferred-work paths without belonging to Story 9.7

## Design Notes

`bootstrap-owned` is a code-authorized historical recovery, not a general third disposition. The story declaration and canonical artifact path must match the immutable authorization tuple; copying the text, editing frontmatter, or moving a baseline cannot create authority.

The bootstrap-owned set is also immutable and code-bound. The intersection of the bootstrap commit's touched paths and the canonical story File List must be exactly these twelve paths; adding either historically unowned path or removing an authorized path invalidates the declaration and grants no bootstrap ownership:

- `.agents/skills/bmad-build/spec-template.md`
- `.agents/skills/bmad-build/step-02-plan.md`
- `.agents/skills/bmad-build/step-04-review.md`
- `.agents/skills/bmad-build/step-05-present.md`
- `.github/workflows/quality.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `eng/tests/test_validate_story_artifacts.py`
- `eng/validate-story-artifacts.py`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

Future File List entries that the bootstrap commit did not touch do not change this intersection and therefore do not broaden or invalidate the historical authorization.

Path labels describe ownership, not mere File List membership. For `owned`, `interleaved`, and `bootstrap-owned` commits, listed paths are `owned` and other paths are `unowned`. For `shared`, `process`, `unmapped`, and `unrelated` commits, listed paths are `listed-unowned` and other paths are `unowned`. Reconciliation still admits only listed paths from ownership-contributing classifications.

## Verification

**Commands:**
- `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` -- both modules compile.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- all mandatory fixtures pass with no new skip.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` -- exact bootstrap/shared report passes and keeps unowned paths visible.
- `git diff --check` -- changed files are whitespace-clean.

## Test Evidence

- Pre-change baseline: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 72 tests with 2 existing optional `ReviewVerifierTests` skips.
- Iteration-1 known-bad evidence: the focused suite passed 76 tests with the same 2 optional skips, and the live strict report passed, but review proved its report still mislabeled listed paths in the `shared` commit as `owned` and allowed a copied fixture path to reuse the hard-coded tuple. Iteration 2 must replace this evidence rather than treating it as acceptance proof.
- Iteration-2 known-bad evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 76 tests with the same 2 optional `ReviewVerifierTests` skips, and the live strict report passed with truthful classification labels. Review nevertheless proved that mutating the canonical File List could broaden bootstrap ownership, so this evidence cannot satisfy iteration 3.
- Iteration-3 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 83 tests with the same 2 optional `ReviewVerifierTests` skips. The suite includes the exact historical CLI report, copied-artifact rejection, pure tuple/topology/declaration/guard checks, independent declared-versus-resolved baseline deviations that prove fail-closed `unmapped`/`listed-unowned` behavior with no ownership, a canonical-metadata regression that adds a historically unowned bootstrap path to the File List and likewise proves no ownership, and an end-to-end regression proving checked tasks under the exact `## Tasks & Acceptance` heading are extracted and evidence-validated. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` passed against canonical candidate `f35523436db525197dcc223ddbe8aa0db97bbdf3`; it reported the bootstrap commit's twelve authorized paths as `owned`, both other paths as `unowned`, and the shared commit's three listed paths as `listed-unowned` while preserving every other path as `unowned`.

## File List

- `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `eng/validate-story-artifacts.py`
- `eng/tests/test_validate_story_artifacts.py`
- `.agents/skills/bmad-build/spec-template.md`
- `.agents/skills/bmad-build/step-02-plan.md`
- `.agents/skills/bmad-build/step-04-review.md`
- `.agents/skills/bmad-build/step-05-present.md`
- `.github/workflows/quality.yml`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

## Suggested Review Order

**Exact bootstrap boundary**

- Start with the immutable twelve-path authorization surface.
  [`validate-story-artifacts.py:230`](../../eng/validate-story-artifacts.py#L230)

- Verify every declared and resolved tuple dimension fails closed.
  [`validate-story-artifacts.py:796`](../../eng/validate-story-artifacts.py#L796)

- Confirm canonical range evidence applies authorization before classification.
  [`validate-story-artifacts.py:878`](../../eng/validate-story-artifacts.py#L878)

**Ownership semantics**

- Reconciliation admits only ownership-contributing commit classifications.
  [`validate-story-artifacts.py:1141`](../../eng/validate-story-artifacts.py#L1141)

- Report labels distinguish ownership from mere File List membership.
  [`validate-story-artifacts.py:1167`](../../eng/validate-story-artifacts.py#L1167)

- Current spec headings now participate in checked-task evidence validation.
  [`validate-story-artifacts.py:1352`](../../eng/validate-story-artifacts.py#L1352)

**Regression proof and operator guidance**

- Historical, copied-artifact, baseline-ref, and mutable-File-List cases exercise the boundary.
  [`test_validate_story_artifacts.py:1224`](../../eng/tests/test_validate_story_artifacts.py#L1224)

- The current task heading has an end-to-end evidence regression.
  [`test_validate_story_artifacts.py:257`](../../eng/tests/test_validate_story_artifacts.py#L257)

- Reviewer guidance makes the historical recovery explicitly non-reusable.
  [`step-04-review.md:48`](../../.agents/skills/bmad-build/step-04-review.md#L48)

- The operator checklist prohibits copying or generalizing the exception.
  [`story-review-reconciliation-checklist.md:25`](story-review-reconciliation-checklist.md#L25)

**Deferred follow-ups**

- Merge-only path reconciliation remains isolated for future work.
  [`deferred-work.md:2592`](deferred-work.md#L2592)

- Package filename normalization remains isolated from Story 9.7.
  [`deferred-work.md:2596`](deferred-work.md#L2596)
