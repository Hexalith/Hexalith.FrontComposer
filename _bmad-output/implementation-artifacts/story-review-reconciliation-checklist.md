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
7. If the story or review fix changes a public component surface, route contract, CLI output, diagnostic metadata, or adopter-facing behavior, complete the doc-drift sweep checklist and record the result in the story evidence.
8. Do not promote the story until File List, task claims, documentation sweep, and verification evidence agree.
