---
title: Check visual baseline rationale for direct pushes
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: 0940e7dddfe0eb067579d3780558d26bc20d16d8
context: []
---

<frozen-after-approval reason="DW-1963 from the deferred work ledger">

## Intent

Given a GitHub push with committed snapshot changes anywhere in the push range, require the committed rationale change. Given missing or invalid push evidence in CI, fail the check with a diagnostic. Preserve pull-request and local working-tree checks.

</frozen-after-approval>

## Implementation Notes


The validator uses the push event’s exact before and after commits and reads the rationale from the candidate commit. Local and pull-request paths retain their existing working-tree and merge-base behavior.

## Review Triage Log

- Added coverage for invalid committed rationale masked by a dirty edit, a clean unrelated push, and the pull-request merge-base path.

## Verification

All seven Node regression tests passed against disposable Git repositories.
