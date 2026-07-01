# Story Review Reconciliation Checklist

Before moving a story from review to done:

1. Compare changed files against the story File List.
   - `git status --short`
   - `git diff --name-only <story-baseline>...HEAD` when a baseline commit is recorded
2. Compare completed task names against actual touched tests and implementation files.
3. Identify generated, QA, e2e, and documentation files separately.
4. Explicitly exclude unrelated dirty files with a short reason.
5. Record test-count deltas and any pre-existing failing lanes.
6. Do not promote the story until File List, task claims, and verification evidence agree.
