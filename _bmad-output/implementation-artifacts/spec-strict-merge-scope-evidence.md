---
title: 'Report merge and workspace evidence for strict story review'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1872, DW-1875, DW-1887, DW-1888, DW-1889]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Give reviewers and automation the same strict story-scope verdict across ordinary commits, merges, and the current workspace. Account for merge paths that differ from every parent under the existing ownership rules. Give shared and process declarations an explicit outcome on merges. Fail strict review while the index contains unresolved conflicts. Identify Git installations that lack the revision-resolution option the validator requires. Offer a JSON report with classifications, path labels, workspace state and failures while preserving the default text interface.

</frozen-after-approval>

## Implementation Notes

The validator reads Git's NUL-delimited combined diff for each merge and classifies its resolution paths with the same rules as ordinary commits. A nonmatching merge may carry a shared or process declaration for those paths. A pathless merge rejects either declaration. Strict review fails on unmerged index entries until the resolution is staged.

Git 2.30 introduced `rev-parse --end-of-options`. Older versions can silently ignore the option and resolve an ordinary ref, so the validator checks the installed version before resolving revisions. The text and JSON renderers share the same path labels. The operator reference documents the JSON fields and exit status.

## Review Triage Log

- Medium, fixed. A path outside the project previously terminated validation before JSON could be written. The CLI now returns the path error in the same JSON envelope as other validation failures.
- Medium, fixed. Matching commits with interleaved changes now report the ignored non-owning declaration in both formats and retain their original validation failure.
- Medium, fixed. JSON encodes paths and subjects containing undecodable Git bytes reversibly. Tests cover different invalid bytes and valid names beginning with the reserved encoding prefix.
- Low, fixed. Sorting JSON keys makes declared workspace reasons reproducible across Python hash seeds.
- Medium, fixed. A real two-parent conflict fixture verifies both a third resolution and selection of either parent verbatim.

## Verification

- `python -m unittest eng.tests.test_validate_story_artifacts` ran 245 tests. 243 passed and two optional review-verifier tests skipped because the story automator is absent.
- `python eng/validate-story-artifacts.py` passed repository integrity validation.
- The pinned commitlint CLI (21.2.4) validated the commit message with zero problems and zero warnings.
