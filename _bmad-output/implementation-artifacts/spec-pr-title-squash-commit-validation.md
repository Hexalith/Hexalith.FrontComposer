---
title: 'Prevent invalid squash commit subjects'
type: 'bugfix'
created: '2026-07-10'
status: 'done'
review_loop_iteration: 2
baseline_commit: 'fdc86a2934e5bd651561ee7e6f8260aacd50596d'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Git instructions validate commits before a pull request but do not require validating the pull-request title or final squash subject. A branch-derived title can therefore become an invalid commit on `main`, as happened with `Fix/actions 29112530341 verify baselines (#55)`.

**Approach:** Extend the shared Git guidance and its self-contained LLM summary so agents explicitly author and validate Conventional Commit pull-request titles, verify the title after creation, supply the squash subject, and wait for checks before merging.

## Boundaries & Constraints

**Always:** Preserve Conventional Commit syntax and the existing repository-pinned commitlint workflow. State that pull-request titles are prospective commit subjects for squash merges, require an explicit title instead of a branch-derived default, and make successful checks a precondition for merging.

**Ask First:** Any change to GitHub Actions workflows, repository rulesets, branch protection, commit history, submodule commits, parent pointer commits, or remote state.

**Never:** Edit `AGENTS.md`, `CLAUDE.md`, or `.github/copilot-instructions.md`; weaken commitlint; use `--admin` to bypass checks; initialize nested submodules; modify unrelated working-tree content; commit, merge, or push as part of this documentation-only fix.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Valid explicit title | `test: refresh verify baselines` | Validate before PR creation and use it explicitly as the PR title | Stop if commitlint rejects it |
| Branch-derived title | `Fix/actions 29112530341 verify baselines` | Reject and replace it with an explicit Conventional Commit title | Never create or merge with the generated title |
| Squash merge | PR contains one or more valid commits | Revalidate the current PR title, wait for checks, and set the squash subject explicitly | Do not merge while checks are pending or failing |
| Title changed remotely | Valid local title differs from the live PR title | Read and validate the live title before merge | Correct the title and rerun checks before merging |

</frozen-after-approval>

## Code Map

- `references/Hexalith.AI.Tools/hexalith-git-instructions.md` -- authoritative detailed workflow for branches, commits, pull requests, and pushes.
- `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` -- self-contained summary that every Hexalith agent must read.
- `.github/workflows/ci.yml` -- existing evidence that PR titles and the latest `main` commit are checked; inspection only.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.AI.Tools/hexalith-git-instructions.md` -- add a fail-closed workflow that explicitly binds repository, head, `main`, and PR URL; validates non-empty state; rechecks title/base/head after checks; locks the merge head; and describes merge-queue behavior accurately.
- [x] `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` -- add matching PR-title, explicit repository/head/base/URL, post-check revalidation, checked-head, squash-subject, and no-bypass invariants.

**Acceptance Criteria:**
- Given an agent is about to create a pull request, when it follows the instructions, then it validates an explicit Conventional Commit title and does not accept a branch-derived default.
- Given a pull request will be squash-merged, when the agent follows the instructions, then it validates the live title, waits for checks to pass, and explicitly supplies the validated squash subject.
- Given checks are pending or failing, when merge guidance is applied, then the instructions forbid direct or administrative bypass.
- Given the two instruction documents are read independently, when their Git guidance is compared, then both preserve the same title and squash-subject invariant without modifying the assistant entrypoints.
- Given title validation, PR lookup, or checks fail, when the documented command sequence runs, then no later create or merge command executes.
- Given the current branch, configured merge base, or PR head changes, when the workflow runs, then explicit base, PR URL, and head-SHA matching prevent creation or merge against unintended state.
- Given the PR title, base, or head changes while checks run, when the pre-merge revalidation executes, then the mismatch stops the merge request.
- Given a repository uses a merge queue, when the instructions describe completion, then they distinguish completed PR-head checks from later queue-managed integration checks.

## Spec Change Log

- **Iteration 1:** Blind Hunter found that the first command examples did not short-circuit on failure, selected a PR implicitly, omitted the intended base and head lock, overstated GitHub's default squash-title behavior, and promised a fresh check that the current workflow does not trigger on title edits. Amended the tasks, design, acceptance, and verification guidance to require fail-closed chaining, `--base main`, an explicit PR URL, `--match-head-commit`, repository-setting-neutral wording, and a supported validation retrigger. This avoids creating or merging after failed validation, targeting the wrong PR/base, racing a changed head, or relying on a stale title check. **KEEP:** explicit title validation before and after PR creation, live-title revalidation, successful checks before merge, an explicit squash subject, no administrator bypass, and matching invariants in both instruction documents.
- **Iteration 2:** Both reviewers found that PR creation still inferred the repository/head, `PR_URL` could be empty, and title/base were not rechecked after the check wait; they also identified wording that conflated PR-head checks with later merge-queue checks. Amended the task, acceptance, design, and verification guidance to require explicit repository/head/base/URL binding, non-empty guards, post-check title/base/head comparison, and truthful merge-queue language. This avoids creating from the wrong branch, silently falling back to branch-selected PR operations, merging after retargeting or title edits, and overstating what the local sequence can guarantee. **KEEP:** all Iteration 1 safeguards, fail-closed chaining, checked-head matching, repository-setting-neutral squash-title wording, and the title-edit stop/ask recovery path.

## Design Notes

Use fail-closed shell examples based on supported GitHub CLI options. Chain dependent operations with `&&`; derive and validate explicit `REPO` and `HEAD_BRANCH` values from the inspected working repository; create with `--repo`, `--head`, and `--base main`; capture and validate the PR URL; and pass the URL to every later command. Before checks, capture title, base, and `headRefOid`; after checks, read all three again, require exact matches and `main`, revalidate the title, and pass the checked head to `gh pr merge --match-head-commit`. Describe the PR title as a possible squash subject depending on repository settings and commit count. State that merge queues may run later integration checks. If a title is edited and no validation retrigger is documented, stop and ask the user. Keep detailed commands in the topical Git file and concise matching invariants in the self-contained LLM summary.

## Verification

**Commands:**
- `git -C references/Hexalith.AI.Tools -c core.whitespace=cr-at-eol diff --check` -- expected: no whitespace errors while honoring the files' CRLF endings.
- `git -C references/Hexalith.AI.Tools diff -- hexalith-git-instructions.md hexalith-llm-instructions.md` -- expected: only the scoped guidance changes.
- `rg -n "pull-request title|squash subject|branch-derived|checks|--base main|--match-head-commit|PR_URL|HEAD_BRANCH|baseRefName" references/Hexalith.AI.Tools/hexalith-{git,llm}-instructions.md` -- expected: both documents carry the new invariants and the detailed workflow carries its explicit repository/head/base/URL safeguards.
- Exercise the documented snippets with stubbed commands that fail title validation, PR lookup, checks, and post-check state comparison -- expected: PR creation and merge are not invoked after any failure.

**Recorded focused evidence:**
- Valid title `test: refresh verify baselines` passed the repository-pinned commitlint; branch-derived title `Fix/actions 29112530341 verify baselines` failed with `type-empty` and `subject-empty`.
- Stubbed creation exited with the validation sentinel (`42`) rather than the create sentinel (`99`). Stubbed merge flows exited with the check (`43`) and lookup (`44`) sentinels rather than the merge sentinel (`99`).
- Commitlint accepted ignored subject `Merge branch main`, confirming that explicit Message Rules remain mandatory after a successful lint. Empty-URL and post-check retarget stubs exited with guard failure (`1`) rather than the merge sentinel (`99`).

## Suggested Review Order

**Policy and rationale**

- Defines PR titles as prospective commit subjects and rejects generated defaults.
  [`hexalith-git-instructions.md:111`](../../references/Hexalith.AI.Tools/hexalith-git-instructions.md#L111)

- Explains why commitlint success cannot replace the explicit message rules.
  [`hexalith-git-instructions.md:119`](../../references/Hexalith.AI.Tools/hexalith-git-instructions.md#L119)

**Fail-closed workflow**

- Binds creation to repository, pushed head, main, and a non-empty PR URL.
  [`hexalith-git-instructions.md:124`](../../references/Hexalith.AI.Tools/hexalith-git-instructions.md#L124)

- Revalidates title, base, and head after checks before requesting merge.
  [`hexalith-git-instructions.md:158`](../../references/Hexalith.AI.Tools/hexalith-git-instructions.md#L158)

- Preserves branch-protection and merge-queue responsibilities without bypass flags.
  [`hexalith-git-instructions.md:183`](../../references/Hexalith.AI.Tools/hexalith-git-instructions.md#L183)

**Self-contained agent rule**

- Carries the essential title, targeting, revalidation, and queue invariants.
  [`hexalith-llm-instructions.md:293`](../../references/Hexalith.AI.Tools/hexalith-llm-instructions.md#L293)
