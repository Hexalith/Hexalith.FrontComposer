# Story Review Reconciliation Checklist

Before moving a story from review to done:

1. Run the mechanical validator from the repository root:
   - `python3 eng/validate-story-artifacts.py --story <story-file>`
   - Add `--base <commit>` only when intentionally overriding the story's `baseline_commit`.
2. Compare completed task names against actual touched tests, implementation files, Test Evidence, Completion Notes, or documented blockers.
3. Identify generated, QA, e2e, documentation, and accepted submodule-pointer evidence separately in the story File List.
4. Classify unrelated dirty files in a predictable story section:
   - `### Documented Unrelated Changes`
   - `- path/to/file - short reason`
   - CLI fallback: `--unrelated <path> --reason <text>`
5. Keep unrelated dirty files visible in validator output; do not add them to the story File List as story-owned changes.
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
