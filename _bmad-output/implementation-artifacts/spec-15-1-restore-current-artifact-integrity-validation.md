---
title: 'Story 15.1: Restore Current Artifact-Integrity Validation'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'dispatch'
story_id: '15.1'
baseline_commit: '6a6f06fd02f2acb72d00eb4971a639b0e366ee1e'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
  - '{project-root}/docs/reference/story-artifact-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 11.32 lacks a File List; its unbounded legacy command includes later deliveries. Global validation passes but misses epic/child completion contradictions.

**Approach:** Restore the delivered inventory, validate its bounded scope with existing CLI options, and extend existing status and File List fixtures.

## Boundaries & Constraints

**Always:** Preserve completed Epics 9–11, Story 11.32's status, frozen intent, baseline, and acceptance. Keep residual approvals open. Reuse the standard-library validator and blocking CI command. Diagnostics use escaped relative paths and stable ordering.

**Never:** Add a validator, schema, report, ownership exception, or lane. Change strict/legacy discovery, misclassify historical delivery, rewrite immutable evidence, modify submodules/product behavior, or manufacture approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Historical scope | Current Story 11.32 artifact and actual baseline-to-delivery paths | Complete 21-path File List reconciles through existing explicit-path mode | Missing or extra entries fail |
| Incomplete list | Missing, empty, or nonempty incomplete File List | Stable contradiction diagnostic naming the artifact | Nonzero exit; no unrelated-path dump for absent lists |
| Status drift | Done epic with unfinished story, backlog epic with started story, or stale story/action status | Existing global command identifies the exact contradiction | Fail closed |
| Isolation | Same defect in different checkout roots with unrelated dirty files | Same support-safe diagnostic | No machine path or dirty-file dependency |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py`: reuse sprint parsing, repository story/action checks, `check_file_list`, and escaping helpers.
- `eng/tests/test_validate_story_artifacts.py`: reuse temporary Git fixtures, `RepositoryIntegrityTests`, and live-corpus/unsafe-path assertions. Baseline: 212 tests pass, two skipped.
- `docs/reference/story-artifact-validation.md`: authoritative scope contract.
- `.github/workflows/quality.yml`: preserve its exact blocking Python module command.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py` -- validate declared epic lifecycle against numeric child rows, excluding retrospective rows; make missing/empty File List diagnostics independent of discovered changes while preserving actionable incomplete-list diagnostics.
- [x] `eng/tests/test_validate_story_artifacts.py` -- cover epic/story/action drift, absent/empty/incomplete lists, root/dirty-state independence, and preserved escaping. Add a live Story 11.32 inventory/CLI regression against its immutable delivery diff.
- [x] `_bmad-output/implementation-artifacts/spec-11-32-epic-11-artifact-integrity-enforcement.md` -- add the exact delivered File List and dated repair/proof notes without changing historical acceptance.
- [x] `docs/reference/story-artifact-validation.md` -- document epic agreement and the exact bounded historical command; explain why an old baseline-to-current-workspace command includes later deliveries.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` and this spec -- update `15-1-i-plan-int-2-restore-current-artifact-integrity-validation`, Epic 15 progress, and verification; preserve historical states.

**Acceptance Criteria:**
- Given the current corpus and delivered Story 11.32 scope, when global and bounded per-story validation run, then both pass and the historical File List equals the actual delivery path set.
- Given any matrix defect, when its focused fixture invokes validation, then it fails deterministically with the relevant contradiction and no machine-specific information.
- Given closed Epics 9–11 and FR27/FR28/FR29.5/FR29.6 outcomes, when regressions run, then history stays closed with existing focused coverage; residual approvals retain their owners.
- Given the existing CI lane, when verification completes, then its exact command is unchanged and focused results are recorded here.

## Implementation Notes

- Review patches applied: uppercase child suffixes participate, unsupported epic statuses and empty done epics fail, numeric matching avoids int conversion, and scrambled-row tests protect complete diagnostic ordering. Six focused lifecycle tests passed; root final verification follows.

- Review pass 1: ten findings triaged independently. Five bounded patch groups cover epic status validation, case-insensitive children, empty done epics, oversized numeric identifiers, and ordering verification. Three verified pre-existing limitations were recorded in the deferred-work ledger.

- Root audit (2026-10-21): read the complete tracked/untracked diff against the preserved baseline, confirmed every execution task and acceptance criterion, and mapped all four matrix rows to passing tests in the unfiltered 218-test run: historical inventory/CLI; absent, empty, incomplete and unsafe-path File Lists; epic/story/action drift; and cross-root/dirty-state isolation. The Story 15.1 per-story validator passes. Git confirms Story 11.32 is append-only with unchanged frozen intent and the CI workflow has no diff.

- Approved by Administrator on 2026-10-21. AppHost baseline: `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited 2: NU1010 in the unchanged Parties.UI submodule (missing central PackageVersion declarations), followed by a NuGet null-reference failure. The Python validation lane remains independently runnable.

- Implemented on 2026-10-21: declared epic lifecycle agreement now reuses parsed numeric sprint rows; retrospective and approval-action rows are excluded. Missing/empty File Lists report one escaped relative artifact path without baseline or discovered-path content. Nonempty incomplete lists retain actionable escaped missing/extra paths. Strict/legacy discovery and the blocking CI workflow remain unchanged.
- Restored Story 11.32's exact 21-path Git delivery inventory and dated maintenance proof. Added fixtures for lifecycle, story/action drift, absent, empty and incomplete lists, root/dirty-state independence, escaping, and live historical inventory equality plus explicit-path CLI validation. Epics 9–11 and Story 11.32 remain done; residual approvals and their owners are unchanged.
- Implementation verified; spec/queue remain in-progress for the root workflow's independent review. Epic 15 remains in-progress with Stories 15.2–15.5 in backlog.


- Final review resolution (2026-10-21): BH-03/BH-04/BH-05/EC-01/EC-02/VG-01 patches were independently inspected and verified. The final full suite ran 221 tests in 108.497 seconds with two existing skips and no failures; the full docs gate, current global/per-story checks, bounded historical command, and whitespace checks passed. Removing both ordering calls from a temporary copy is rejected by both exact lifecycle-order assertions.
- Current workflow state: this spec is done and its sprint row remains review as required by Build's completion step; Epic 15 stays in-progress. Earlier implementation-state notes above record the prior handoff. Three verified pre-existing limitations remain in the deferred-work ledger; none reopens accepted Epic 9–11 delivery.
- Local commit preparation: branch `fix/story-15-1-artifact-integrity` follows the contribution guide's prohibition on direct main commits. The exact message `fix(15.1): restore current artifact integrity validation` passed pinned `@commitlint/cli` 21.2.2 with `node_modules/.bin/commitlint --edit /tmp/frontcomposer-story-15-1-commit-message.txt --verbose`: 0 problems, 0 warnings. Successful output is preserved in `/tmp/frontcomposer-story-15-1-commitlint.log` and this record.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Evidence and route |
|---|---|---|
| BH-01 | medium | Reproduced the dirty-basename task-evidence dependency in both HEAD's validator and the candidate. The File List diagnostic itself is identical; unchanged checked-task shorthand matching produces the additional varying error. Defer this pre-existing evidence-attribution limitation. |
| BH-02 | medium | Reproduced raw U+2028 in the unchanged checked-task diagnostic in both HEAD and the candidate; File List paths are escaped. Defer the pre-existing checked-task formatting issue. |
| BH-03 | medium | The ledger documents only backlog/in-progress/done for epics, but a parsed epic-99=review is ignored by the new check. Patch epic-specific status validation. |
| BH-04 | medium | A parsed 99.2B unfinished child is ignored by the new lowercase-only suffix regex. Patch case-insensitive child matching; shared cause with EC-01. |
| BH-05 | medium | A declared done epic with no numeric child rows currently passes, allowing complete child deletion to erase completion evidence. Patch the demonstrated empty-done-epic state without adding a registry. |
| BH-06 | medium | Reproduced a Story 15.1 artifact/queue mismatch passing global validation. Artifact/queue reconciliation remains restricted to Epic 11 in unchanged code. Defer extension of that pre-existing corpus boundary. |
| BH-07 | low | The implementation handoff note records its prior in-progress state; current frontmatter and queue correctly show review. Reject this spec-only editing request per the Build review rules; append current completion state during final presentation. |
| EC-01 | medium | Reproduced the same uppercase-suffix bypass as BH-04 using 99-1A. Patch together with BH-04; retain this independent finding row. |
| EC-02 | low | Reproduced ValueError from converting a 4301-digit numeric component with int(). Patch by comparing zero-normalized digit strings, a direct replacement with no new public surface. |
| VG-01 | medium | Pre-verified mutation removes both sorting calls while the lifecycle tests still pass. Patch the existing isolation fixture with scrambled epic/child insertion and an exact ordered diagnostic assertion. |

## Design Notes

Generate the 21 paths from baseline `00d4da4788692ee405a081884e78482a9556b0c3` through delivery `9aad029f837b8ecbd9f58e6f89ffdd76bf403bcf`, obtained from Git. Supply them through existing `--changed-file` options. Assert exact inventory equality so accepted-extra prose cannot hide drift. This checks inventory rather than strict ownership. Run global integrity separately.

For declared epic rows, done requires every numeric child done; backlog forbids started/completed children. In-progress may await closure. Exclude approval actions and retrospective rows.

## Verification

Actual results (2026-10-21):

- `python3 -m unittest eng.tests.test_validate_story_artifacts` — passed: 218 tests, two existing skips. Includes new matrix/isolation fixtures and the live exact 21-path inventory/CLI regression.
- `python3 -m unittest eng.tests.test_validate_story_artifacts.StoryArtifactValidatorTests.test_absent_and_empty_file_lists_ignore_checkout_roots_and_dirty_paths eng.tests.test_validate_story_artifacts.StoryArtifactValidatorTests.test_incomplete_file_list_explicit_scope_is_stable_and_escaped eng.tests.test_validate_story_artifacts.StoryArtifactValidatorTests.test_baseline_commit_from_frontmatter_detects_missing_file_list_entry eng.tests.test_validate_story_artifacts.CommitScopeEvidenceTests.test_file_list_failure_paths_are_escaped eng.tests.test_validate_story_artifacts.RepositoryIntegrityTests` — passed: 25 focused tests.
- `python3 -m unittest eng.tests.test_validate_story_artifacts.CommitScopeEvidenceTests.test_file_list_failure_paths_are_escaped eng.tests.test_validate_story_artifacts.StoryArtifactValidatorTests.test_submodule_pointer_path_is_not_silently_ignored` — passed: two tests, including the final expanded unsafe-artifact/missing/extra escaping assertions.
- `python3 eng/validate-story-artifacts.py` — passed: global sentinel and repository integrity checks.
- Bounded invocation below — passed: historical Story 11.32 inventory reconciles through existing explicit-path mode.
- `pwsh ./eng/validate-docs.ps1` — passed: full documentation validation, including build, snippets and DocFX; evidence manifest at `artifacts/docs/validation-manifest.json`.
- `git diff --check` — passed: no whitespace errors.

The exact blocking `python3 -m unittest eng.tests.test_validate_story_artifacts` command in `.github/workflows/quality.yml` is unchanged. The workflow contents match `HEAD` after checkout line-ending normalization. Story 11.32's frozen block matches `HEAD`; its existing dated acceptance remains intact. The AppHost baseline blocker is recorded under Implementation Notes and did not block these independent Python/documentation checks.

Final verification after review patches (2026-10-21):

- `python3 -m unittest eng.tests.test_validate_story_artifacts` — OK: 221 tests in 108.497 seconds, two existing skips.
- `pwsh ./eng/validate-docs.ps1` — exit 0; Docs validation passed, including build/snippets/DocFX. Root rerun output: `/tmp/frontcomposer-15-1-final-docs.log`.
- `python3 eng/validate-story-artifacts.py` and the Story 15.1 per-story invocation — passed.
- The exact published bounded historical invocation below — passed.
- `git diff --check` — passed.

Reproducible bounded historical invocation from the repository root:

```bash
python3 - <<'PY'
import subprocess

baseline = "00d4da4788692ee405a081884e78482a9556b0c3"
delivery = "9aad029f837b8ecbd9f58e6f89ffdd76bf403bcf"
paths = subprocess.check_output(
    ["git", "diff", "--name-only", "-z", baseline, delivery], text=True
).split("\0")[:-1]
assert len(paths) == 21
command = [
    "python3", "eng/validate-story-artifacts.py", "--story",
    "_bmad-output/implementation-artifacts/spec-11-32-epic-11-artifact-integrity-enforcement.md",
]
for path in paths:
    command.extend(["--changed-file", path])
raise SystemExit(subprocess.run(command, check=False).returncode)
PY
```

## File List

- `_bmad-output/implementation-artifacts/epic-15-context.md`
- `_bmad-output/implementation-artifacts/spec-15-1-restore-current-artifact-integrity-validation.md`
- `_bmad-output/implementation-artifacts/spec-11-32-epic-11-artifact-integrity-enforcement.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `docs/reference/story-artifact-validation.md`
- `eng/tests/test_validate_story_artifacts.py`
- `eng/validate-story-artifacts.py`
