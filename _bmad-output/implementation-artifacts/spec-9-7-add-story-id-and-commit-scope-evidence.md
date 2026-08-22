---
title: 'Story 9.7: Add story-ID and commit-scope evidence'
type: 'chore'
created: '2026-08-22'
status: 'in-review'
baseline_commit: 'ceae00a4f9788222ed19153acfc05d68d0bc85d1'
story_id: '9.7'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-retro-2026-08-11.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Baseline-to-head reconciliation collapses all intervening commits into one path set, hiding story delivery among unrelated commits and dirty state. Epic 9 therefore could not attribute its implementation, review, and done transitions reliably.

**Approach:** Extend the existing validator with a deterministic baseline-to-candidate report: exact story-ID attribution, per-commit File List reconciliation, and explicit full-SHA shared/process exceptions.

## Boundaries & Constraints

**Always:** Canonicalize baseline/candidate SHAs, require ancestry, report every non-merge commit/path and list merges separately. Match `9.7`/`9-7` without matching `19.7`. Keep committed evidence separate from staged, unstaged, untracked, and documented-unrelated state. Exceptions require an in-range full SHA, `shared` or `process`, and a reason.

**Ask First:** Any new dependency, persisted report schema, additional disposition kind, or change to repository commit/status semantics beyond making the existing review-completion gate fail closed.

**Never:** Rewrite history, infer ownership from path changes alone, reuse path-level unrelated declarations as commit exceptions, hide Git failures, create a parallel validator, or change runtime, generated output, packages, submodules, or public APIs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Story commit | Matching subject; listed paths | Report SHA, match, and owned paths | Pass |
| Unmapped delivery | No/wrong ID touches a listed path | Report unmapped/interleaved commit | Fail until corrected or disposed |
| Shared/process | Full in-range SHA, valid kind/reason | Report but exclude from ownership | Reject malformed/stale declarations |
| Merge/dirty | Merge plus unrelated workspace edits | Report each separately | Invalid refs/ancestry fail |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py:213-342,469-597,689-1140,1313-1352` -- fail-closed story identity, candidate/commit/workspace evidence, NUL-safe path handling, and bounded File List reconciliation.
- `.agents/skills/bmad-retrospective/scripts/git_evidence.py:64-188,230-299` -- read-only Git/story/merge reference; no runtime dependency.
- `eng/tests/test_validate_story_artifacts.py:18-55,121-207,660-1143` -- temporary Git fixtures, identity boundaries, dispositions, unusual paths, and strict review-gate coverage.
- `.agents/skills/bmad-build/{spec-template.md:1-8,step-02-plan.md:10-12}` -- canonical numbered-story frontmatter and freeform omission rules.
- `.agents/skills/bmad-build/{step-04-review.md,step-05-present.md}` -- live review/done gate.
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` -- operator contract.
- `_bmad-output/implementation-artifacts/deferred-work.md:2579-2581` -- review-deferred legacy checked-task extraction mismatch.
- `.github/workflows/quality.yml:116-132` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2807-2832` -- authoritative blocking CI suite and anti-masking pin.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py` -- add story-ID extraction, `--candidate`, ancestry-safe commit/path reporting, strict exceptions, and separate workspace evidence -- single mechanical gate.
- [x] `eng/tests/test_validate_story_artifacts.py` -- cover matching, missing/wrong ID, shared/process, merge, interleaving, ID boundaries, invalid refs/ancestry, and dirty state -- load-bearing fixtures.
- [x] `.agents/skills/bmad-build/{step-04-review.md,step-05-present.md}` and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` -- block and document completion, including the final transition.
- [x] `.github/workflows/quality.yml` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- run and pin the suite as non-advisory.

**Acceptance Criteria:**
- Given baseline and candidate, when validation runs, then every non-merge commit, ID match, path/File List disposition, unrelated/interleaved commit, and merge is reported.
- Given an implementation/review/done commit is unmapped, when completion is attempted, then it fails until scope is corrected or validly disposed.
- Given dirty workspace state, when reporting runs, then it remains visible but separate from committed ownership.
- Given CI runs, when any mandatory fixture regresses, then the gate fails.

## Spec Change Log

## Design Notes

Use an optional `## Commit Scope Dispositions` section with one declaration per exception:

```text
- `<40-character-sha>` | `shared` | <non-empty reason>
- `<40-character-sha>` | `process` | <non-empty reason>
```

A matching commit is story-owned. A non-match touching listed paths is unmapped; one touching only unowned paths is unrelated. A match with unowned paths is interleaved. Exceptions explain commits without reclassifying paths.

## Verification

**Commands:**
- `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` -- modules compile.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- all runnable fixtures pass; commit coverage has no skip.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --filter "Category=Governance"` -- CI pin passes.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` -- actual report passes.
- `git diff --check` -- clean.

## Test Evidence

- Pre-patch baseline: `python3 -m unittest eng.tests.test_validate_story_artifacts` passed 63 tests with 2 optional ReviewVerifier skips.
- Post-patch: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed all 72 tests with the same 2 optional skips; every new commit-scope fixture ran.
- Focused Governance pin: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "FullyQualifiedName~CiGovernanceTests.PrepareManifest_UnsignedCandidate_HasNoAuthorSigningContract"` passed 1/1 after the blocking-job assertion update.
- Live report: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` passed against canonical baseline/candidate `ceae00a4f9788222ed19153acfc05d68d0bc85d1`, with the active unrelated spec reported separately.
- The exact post-review-patch broad Governance lane executed 225 tests: 223 passed and 2 pre-existing facts failed. One selects Fluent UI `4.13.2-rc.5` while the ledger expects `rc.4`; the CA1707 inventory expects 6994 but the unchanged baseline test sources produce 6995 and hash `639f5c4ac93714e6d7757c6cfec8ca337f459b8a3ee269cdee124dce94a349ff`. Removing the Story 9.7 C# diff and rerunning that exact fact reproduced the CA1707 failure, so it is not attributed to this patch.

## Documented Unrelated Workspace State

- `_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md` - pre-existing review-loop update owned by another active spec.

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
