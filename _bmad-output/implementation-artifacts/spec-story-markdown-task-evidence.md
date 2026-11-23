---
title: 'Preserve source locations and task evidence when parsing story Markdown'
type: 'refactor'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1729, DW-1878, DW-1879, DW-1881, DW-1886]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Read story sections, checked tasks, and the first document heading from the same semantic Markdown. Merge repeated section bodies without allocating lines for intervening sections or examples. Report malformed declarations and heading levels at their original source lines. Keep examples out of blocker evidence and keep task evidence checks consistent for every supported past-tense verb.

</frozen-after-approval>

## Implementation Notes

The validator indexes semantic lines once per story metadata read. It stores original line numbers for disposition declarations and passes only headings and checked items to the task collector. The verb tests enumerate expected forms independently of the validator. The operator reference explains where blocker text counts as evidence.

## Review Triage Log

- Medium, fixed. An H1 now closes the previous evidence section. An appendix heading and its prose can no longer satisfy a checked task in the preceding section.
- Medium, fixed. The disposition heading check reports level one with its source line, as the operator reference requires.
- Low, fixed. The reference distinguishes general blocker evidence from evidence for an explicitly claimed output. A regression covers a task that names a blocked path.
- Medium, fixed. Tasks beginning with a supported output verb now reconcile named outputs even when they contain no general evidence keyword. Coverage exercises six verb forms with missing and accounted-for paths, plus advisory and negated references.
- Medium, deferred. HTML authoring comments already counted as general evidence. The documented decision for DW-1886 covers fenced and indented Markdown examples. The ledger records the separate comment-handling issue.

## Verification

- The validator suite ran 231 tests. 229 passed and the two optional story-automator integration tests skipped because the skill was absent.
- `python3 eng/validate-story-artifacts.py` passed repository integrity validation.
- Comparing parsed metadata across 328 implementation-artifact Markdown files produced identical values after omitting blank evidence lines.
- Clearing the irregular-verb mapping made the vocabulary test fail for both `split` and `write`.
