---
title: 'Reconcile the superseded frozen release-evidence review'
type: 'refactor'
created: '2026-08-24'
status: 'done'
baseline_commit: '0bfb143e6d52cf83abcbd893e7f1c679f17d598b'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md'
  - '{project-root}/_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The July frozen-release evidence fix remains marked `in-review`, but its baseline now spans thousands of unrelated changes and its literal tag-resolution mechanism was removed by the later exact-source release redesign. Re-reviewing that baseline or reimplementing the obsolete query would create false attribution and unnecessary release-workflow churn.

**Approach:** Close the stale record with exact historical implementation and operational evidence, then identify the commits that superseded it and the current mechanism that replaced it. Make no workflow, source, test, release-setting, or submodule change.

## Boundaries & Constraints

**Always:** Preserve full commit SHAs and run URLs verbatim; distinguish historical implementation proof from current architecture; record the complete eight-path implementation scope; state that the green post-fix run did not exercise the old API-failure or orphan-release branches; keep the successor baseline and owned file list truthful.

**Ask First:** Any current release-workflow or test change, restoration of a side-effect probe, change to `HEXALITH_RELEASE_PUBLISH_ENABLED`, workflow dispatch, publication action, or modification of a submodule requires separate human authorization.

**Never:** Rewrite the stale baseline, add mass commit-scope exceptions, claim the old `target_commitish` mechanism still exists, claim the repository is currently frozen, treat expired evidence as retained proof, or reintroduce obsolete release logic merely to create a reviewable diff.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Historical implementation | Parent `b0254994...`; commit `550cb060...` | Record the exact one-commit range, all eight paths, and green run `29704283540` | Fail reconciliation if ancestry, scope, or run conclusion differs |
| Superseded mechanism | Current files lack the old query after `3ebbdce9...` | Record implemented-then-superseded; do not reimplement | Name the replacing architecture and evidence limitations |
| Current policy question | Live setting or current release behavior is requested | Keep it outside this documentation-only closure | Halt for separately authorized work |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md` -- only existing artifact to edit; set a truthful terminal status and append reconciliation evidence without altering its frozen intent.
- `_bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md` -- successor record; retain final verification evidence and the two-file owned File List.
- `.github/workflows/release-evidence.yml:144` -- read-only current non-publication disposition; lines 247–302 contain the current governed-release verification path.
- `eng/release_disposition.py:85` -- read-only current authenticated topology classifier.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1601` -- read-only current release-evidence governance surface.
- Git commits `550cb0602d506d9fd008a8c09f2cca6b328ec1e3`, `90c5dcb9af3ff4cf0c243c5af1a06295b09ca175`, `3ebbdce987b2d74340be66b26bc284aa59c9233e`, and `fd1f5b624d5dfee8f0d17da349ad6868553c68a1` -- read-only implementation, expansion, supersession, and follow-on attribution anchors.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md` -- change the status to `done`, add an `implemented-then-superseded` resolution, and append exact implementation scope, run proof, supersession chain, and proof limitations -- close the recurring stale review without changing frozen intent.
- [x] `_bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md` -- record completed verification and an exact File List containing only this successor and the stale spec -- keep the closure mechanically reviewable from the current baseline.

**Acceptance Criteria:**
- Given the stale spec and Git history, when reconciliation completes, then the record names the exact parent-to-implementation range and all eight paths without attributing later repository changes to the fix.
- Given post-fix run `29704283540`, when operational evidence is summarized, then frozen no-publication success is recorded while API-failure and orphan-branch proof limitations remain explicit.
- Given the exact-source redesign, when the stale item is closed, then `3ebbdce987b2d74340be66b26bc284aa59c9233e` is identified as superseding the old mechanism and no current code change is implied.
- Given the successor baseline, when the artifact validator runs, then only the two documentation artifacts are story-owned and no submodule change is present.

## Spec Change Log

## Design Notes

This closure distinguishes “the historical fix worked” from “the historical mechanism remains a current invariant.” The current release architecture must be evaluated through its own authenticated disposition and governed-attempt contracts; any suspected present-day policy gap belongs in separately approved work.

## Verification

**Commands:**
- `test "$(git rev-list --parents -n 1 550cb0602d506d9fd008a8c09f2cca6b328ec1e3)" = "550cb0602d506d9fd008a8c09f2cca6b328ec1e3 b0254994e279a21d0496d6b3286d6524eebb14b4"` -- passed: the implementation commit has exactly one parent, and it is the stale baseline.
- `test "$(git diff-tree --no-commit-id --name-status -r 550cb0602d506d9fd008a8c09f2cca6b328ec1e3 | wc -l)" -eq 8 && git diff-tree --no-commit-id --name-status -r 550cb0602d506d9fd008a8c09f2cca6b328ec1e3` -- passed: the output is the documented complete eight-path scope.
- `gh run view 29704283540 --attempt 1 --repo Hexalith/Hexalith.FrontComposer --json conclusion,headSha,jobs,url | jq -e '.conclusion == "success" and .headSha == "550cb0602d506d9fd008a8c09f2cca6b328ec1e3" and ([.jobs[] | select(.databaseId == 88238525952 and .name == "verify-published-release" and .conclusion == "success")] | length) == 1 and ([.jobs[] | select(.databaseId == 88238525952) | .steps[] | select(.name == "Resolve release tag" and .conclusion == "success")] | length) == 1 and ([.jobs[] | select(.databaseId == 88238525952) | .steps[] | select(.name == "Upload verification evidence artifact" and .conclusion == "success")] | length) == 1 and ([.jobs[] | select(.databaseId == 88238525952) | .steps[] | select((.name == "Download published GitHub Release assets and evidence" or .name == "Verify sealed manifest over downloaded bytes" or .name == "Download published NuGet bytes and compare hashes" or .name == "Verify signatures of published NuGet bytes" or .name == "Compose verification ledger record") and .conclusion == "skipped")] | length) == 5'` -- passed on 2026-08-24: attempt 1, job `88238525952`, and the expected historical branch completed green; this metadata does not independently prove absence of an external publication side effect.
- `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/29704283540/artifacts | jq -e '.total_count == 1 and (.artifacts | length) == 1 and .artifacts[0].id == 8447402914 and .artifacts[0].name == "release-verification-29704283540-1" and .artifacts[0].expired == true'` -- passed on 2026-08-24: the returned artifact metadata is exact and expired.
- `if log_output="$(gh run view 29704283540 --attempt 1 --repo Hexalith/Hexalith.FrontComposer --job 88238525952 --log 2>&1)"; then exit 1; else grep -F 'HTTP 410' <<<"$log_output"; fi` -- passed on 2026-08-24: archived Release Evidence logs return HTTP 410.
- `gh run view 29704185078 --attempt 1 --repo Hexalith/Hexalith.FrontComposer --json conclusion,headSha,jobs,url | jq -e '.conclusion == "success" and .headSha == "550cb0602d506d9fd008a8c09f2cca6b328ec1e3" and ([.jobs[] | select(.databaseId == 88238270531 and .name == "ci / build-and-test" and .conclusion == "success")] | length) == 1' && git show 550cb0602d506d9fd008a8c09f2cca6b328ec1e3:.github/workflows/ci.yml | rg -n -A 12 'Trait-clean Tier 1 projects only. Shell.Tests'` -- passed: CI was green but the exact workflow source says `Shell.Tests` ran in Quality, not this lane.
- `gh run view 29704184914 --attempt 1 --repo Hexalith/Hexalith.FrontComposer --json conclusion,headSha,jobs,url | jq -e '.conclusion == "failure" and .headSha == "550cb0602d506d9fd008a8c09f2cca6b328ec1e3" and ([.jobs[] | select(.databaseId == 88238269918 and .name == "build-and-test" and .conclusion == "failure") | .steps[] | select(.name == "Gate 2b: Infrastructure governance and telemetry contracts" and .conclusion == "failure")] | length) == 1 and ([.jobs[] | select(.databaseId == 88238269918) | .steps[] | select(.name == "Gate 3a: Unit + bUnit (default lane)" and .conclusion == "skipped")] | length) == 1'` -- passed: Quality failed before its default test lane could supply Governance execution evidence.
- `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/29704185078/artifacts | jq -e '.total_count == 1 and ([.artifacts[] | select(.id == 8447395908 and .expired == true)] | length) == 1' && gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/29704184914/artifacts | jq -e '.total_count == 4 and ([.artifacts[] | select(.expired == true)] | length) == 4 and ([.artifacts[].id] | sort) == ([8447430106, 8447397277, 8447396391, 8447396299] | sort)'` -- passed on 2026-08-24: all returned CI and Quality artifacts are expired.
- `for coordinate in '29704185078 88238270531' '29704184914 88238269918'; do read -r run_id job_id <<<"$coordinate"; if log_output="$(gh run view "$run_id" --attempt 1 --repo Hexalith/Hexalith.FrontComposer --job "$job_id" --log 2>&1)"; then exit 1; else grep -F 'HTTP 410' <<<"$log_output"; fi; done` -- passed on 2026-08-24: both archived build-job log requests return HTTP 410.
- `test "$(git show -s --format=%s 90c5dcb9af3ff4cf0c243c5af1a06295b09ca175)" = 'fix(ci): restore frozen release evidence runs' && test "$(git show -s --format=%s 3ebbdce987b2d74340be66b26bc284aa59c9233e)" = 'ci(release): align production publishing with exact-source dispatch' && test "$(git show -s --format=%s fd1f5b624d5dfee8f0d17da349ad6868553c68a1)" = 'fix(release): harden REL-3 unsigned FR24 fail-closed residuals' && git grep -q -E 'target_commitish|targetCommitish|gh release list' 90c5dcb9af3ff4cf0c243c5af1a06295b09ca175 -- .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs && ! git grep -q -E 'target_commitish|targetCommitish|gh release list' 3ebbdce987b2d74340be66b26bc284aa59c9233e -- .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs && ! git grep -q -E 'target_commitish|targetCommitish|gh release list' fd1f5b624d5dfee8f0d17da349ad6868553c68a1 -- .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs && git diff-tree --no-commit-id --name-status -r fd1f5b624d5dfee8f0d17da349ad6868553c68a1 | rg -q '^A\s+eng/release_disposition.py$'` -- passed: the three attribution anchors have the expected subjects and source transitions.
- `rg -n 'target_commitish|targetCommitish|gh release list' .github/workflows/release-evidence.yml tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- expected: no matches in the current superseding architecture.
- `for seam in 'release_disposition.py classify' 'Record explicit non-publication disposition' 'Require exact immutable GitHub Release'; do rg -Fq "$seam" .github/workflows/release-evidence.yml; done && rg -Fq 'def classify_release_run(' eng/release_disposition.py` -- passed: the current workflow and classifier expose the documented authenticated-disposition and governed-attempt seams.
- `python3 -m unittest tests/eng/test_release_disposition.py tests/eng/test_release_contract.py` -- passed: 29 tests, `OK`.
- `test "$( { git diff --name-only 0bfb143e6d52cf83abcbd893e7f1c679f17d598b; git ls-files --others --exclude-standard; } | LC_ALL=C sort -u)" = "$(printf '%s\n' '_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md' '_bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md' | LC_ALL=C sort)" && test -z "$(git diff --name-only 0bfb143e6d52cf83abcbd893e7f1c679f17d598b -- references)" && test -z "$(git status --short --untracked-files=no -- references)"` -- passed: exactly the two File List artifacts changed from the successor baseline and no gitlink/submodule change is present.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md` -- passed: canonical artifact validation accepted the exact two-file ownership record.
- `git diff --check` -- passed after CRLF normalization.

## Completion Notes

- Confirmed the exact parent relationship and the complete eight-path name-status scope for `550cb0602d506d9fd008a8c09f2cca6b328ec1e3`.
- Confirmed on 2026-08-24 that run `29704283540`, attempt 1, concluded successfully at that exact head. Job `88238525952`, tag resolution, and evidence upload succeeded while the five publication-verification steps were skipped. This proves the expected historical branch completed green, not independently that no external publication side effect existed.
- Confirmed artifact ID `8447402914` is expired and archived logs return HTTP 410. Retained proof is therefore limited to available run/job/step metadata.
- Confirmed surviving CI does not prove the historical Governance matrix passed: CI run `29704185078` excluded `Shell.Tests`; Quality run `29704184914` failed Gate 2b before Gate 3a; all returned artifacts and both build-job logs are expired/unavailable. The old API-failure and orphan branches were implemented historically but are not operationally proven by surviving evidence.
- Confirmed `90c5dcb9af3ff4cf0c243c5af1a06295b09ca175` retained and expanded the old query, `3ebbdce987b2d74340be66b26bc284aa59c9233e` removed it during the exact-source redesign, and `fd1f5b624d5dfee8f0d17da349ad6868553c68a1` hardened the replacing authenticated-disposition path.
- Confirmed the narrow current classifier/contract lane passes all 29 tests.
- Made no workflow, source, test, release-setting, publication, dispatch, or submodule change.

## File List

- `_bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md`
- `_bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md`

## Suggested Review Order

**Disposition and attribution**

- Start with the terminal disposition and successor link.
  [`spec-actions-29703578735-fix-release-evidence-noop.md:72`](spec-actions-29703578735-fix-release-evidence-noop.md#L72)

- Machine metadata exposes implementation and supersession to tooling.
  [`spec-actions-29703578735-fix-release-evidence-noop.md:8`](spec-actions-29703578735-fix-release-evidence-noop.md#L8)

- Exact commit scope separates functional work from coincident gitlinks.
  [`spec-actions-29703578735-fix-release-evidence-noop.md:78`](spec-actions-29703578735-fix-release-evidence-noop.md#L78)

**Evidence boundaries**

- Operational proof states what survived and what remains unproven.
  [`spec-actions-29703578735-fix-release-evidence-noop.md:101`](spec-actions-29703578735-fix-release-evidence-noop.md#L101)

- Supersession chain maps the obsolete resolver to current architecture.
  [`spec-actions-29703578735-fix-release-evidence-noop.md:135`](spec-actions-29703578735-fix-release-evidence-noop.md#L135)

- Verification pins mutable GitHub evidence and current contract behavior.
  [`spec-reconcile-frozen-release-evidence-review.md:67`](spec-reconcile-frozen-release-evidence-review.md#L67)

**Completion record**

- Completion notes summarize accepted proof limits and focused test results.
  [`spec-reconcile-frozen-release-evidence-review.md:87`](spec-reconcile-frozen-release-evidence-review.md#L87)

- File List constrains ownership to the two reconciliation artifacts.
  [`spec-reconcile-frozen-release-evidence-review.md:97`](spec-reconcile-frozen-release-evidence-review.md#L97)
