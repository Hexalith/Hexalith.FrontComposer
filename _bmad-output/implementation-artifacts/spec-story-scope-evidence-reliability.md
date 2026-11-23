---
title: 'Make story scope evidence reliable in CI'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1882, DW-1883, DW-1884, DW-1885, DW-1894]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the story artifact validator's evidence reliable when CI captures its output. Report the most specific matching workspace classification, flush the evidence before printing failures, and reject a commit range if either endpoint changes during collection. Name the existing pipe-delimiter quoting rule where it is enforced and exercise all four documented reasons for accepting an unchanged File List entry.

</frozen-after-approval>

## Implementation Notes

The validator now checks both original Git refs after it reads the workspace snapshots. Each moved ref produces a diagnostic with both commit IDs. Workspace rows and dirty-file notices use the same most-specific classification. The shared quoting predicate explicitly covers the pipe that separates report fields.

The CLI regressions use a buffered child process with a combined output stream and a repository containing tracked, unchanged File List entries. The ref-movement regression moves real Git refs during evidence collection.

## Review Triage Log

- Medium, patched. The ref-movement fixture now updates refs during the second workspace read, so an early endpoint check fails the test.
- Medium, patched. Separate baseline-only, candidate-only and simultaneous cases verify each diagnostic and its original range.
- Low, patched. The operator reference documents the single-stream redirection that preserves write order.
- Low, patched. A CLI fixture verifies that both the dirty-file notice and the workspace evidence row display the closest classification.
- Low, patched. The operator reference describes the before-and-after commit comparison and the failure when a ref disappears.

## Verification

- `python3 -m unittest -v eng.tests.test_validate_story_artifacts` passed 224 tests. Two existing review-verifier tests were skipped because the optional story automator is absent.
- `python3 eng/validate-story-artifacts.py` passed the repository integrity check.
- In a scratch copy, forcing the accepted-reason predicate to return false made the new CLI test fail for all four File List entries.
