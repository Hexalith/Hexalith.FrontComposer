# Story Review Reconciliation Checklist

Before moving a story from review to done:

1. Run the mechanical validator from the repository root:
   - Numbered story + usable Git baseline: `python3 eng/validate-story-artifacts.py --story <story-file> --candidate HEAD`.
   - Freeform spec, missing/`NO_VCS`/unresolvable baseline, or no VCS: `python3 eng/validate-story-artifacts.py --story <story-file>` as the documented legacy/best-effort path.
   - Add `--base <commit>` only when intentionally overriding the story's `baseline_commit`.
   - An unresolvable `--base` fails; an unresolvable `baseline_commit` degrades to a reported bare-diff fallback.
   - Story identity is parsed in both modes. A malformed or self-contradicting `story_id`, `title`, H1, or filename fails a legacy run too, even though legacy mode never uses the ID.
   - Require canonical full baseline/candidate SHAs, every non-merge commit and path,
     exact story-ID/File List disposition, a separate merge list, and separate
     staged/unstaged/untracked/unresolved and documented-unrelated workspace evidence
     in the report.
   - Before final completion, rerun after the transition commit so implementation,
     review, and done-transition commits are all in the checked range.
2. Compare completed task names against actual touched tests, implementation files, Test Evidence, Completion Notes, or documented blockers.
3. Identify generated, QA, e2e, documentation, and accepted submodule-pointer evidence separately in the story File List.
4. Classify unrelated dirty files in a predictable story section:
   - `### Documented Unrelated Changes`
   - `- path/to/file - short reason`
   - CLI fallback: `--unrelated <path> --reason <text>`
5. Keep unrelated dirty files visible in validator output; do not add them to the story File List as story-owned changes.
   Path-level unrelated declarations do not excuse commits. A commit exception must be
   declared under `## Commit Scope Dispositions` as an in-range full 40-character SHA,
   `shared` or `process`, and a non-empty reason.
   - The sole `bootstrap-owned` exception is the human-authorized canonical Story 9.7
     recovery bound in validator code to one exact baseline, commit, parent topology,
     and immutable listed-path intersection. Never copy or broaden it, and never use it
     for routine attribution in place of a correct story-ID commit subject.
   - Stage and commit only reconciled story-owned File List paths. Never use a blanket
     add or absorb a documented-unrelated path into the story commit.
6. Record test-count deltas and any pre-existing failing lanes.
7. Enforce the standard Test Evidence language:
   - Use a lane table with `Lane`, `Required command`, `Local result`, `Blocker timing`, `Fallback evidence`, and `CI authority`.
   - `Local result` is `Passed`, `Failed`, or `Blocked`; never call a blocked exact lane passed because a fallback passed.
   - VSTest/MSBuild socket or named-pipe blockers must name the exact blocker text and whether it occurred before test execution.
   - Direct xUnit v3 in-process runs are local fallback evidence unless they are the required lane for that story.
   - NuGet/package/network blockers must name the blocked service or URI and any cached/no-restore fallback.
   - Playwright/Kestrel/browser blockers must name the CI browser/a11y/visual lane, owner, and expected artifact path when browser evidence remains required.
8. If the story or review fix changes a public component surface, route contract, CLI output, diagnostic metadata, generated-output shape, MCP descriptor, adopter-facing behavior, or any implementation behavior governed by a contract document, complete the doc-drift sweep checklist and record the result in the story evidence. Behavior-changing review fixes must explicitly name the contract docs checked and either update them or record a no-update rationale.
9. Do not promote the story until File List, task claims, documentation sweep, and verification evidence agree.
10. Treat `artifact_validation_failed` as a hard review-completion blocker. If the validator appears wrong, keep the story out of `done`, fix `eng/validate-story-artifacts.py` or its tests, rerun the validator, and record the fix evidence. Do not manually bypass the failure by editing story status, sprint status, or review policy.
