---
title: 'Reconcile checked tasks with visible evidence and declared outputs'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

A reviewer should be able to tell which output supports a checked task. Require a changed or accounted-for submodule pointer when the task names it, and require a listed or explicitly classified file when the task uses basename shorthand. Keep ordinary directory references, suffix literals, and assembly names usable in prose. Only visible content should supply general task evidence. If a task fails, print its artifact path and source line unambiguously.

</frozen-after-approval>

## Implementation Notes

The validator compares exact paths with Git changes and resolves basename shorthand from the File List and bounded workspace classifications. The index supplies mode `160000` for a submodule. Checked-task diagnostics use the same path formatter as the File List. The evidence reader filters hidden comments before extracting general prose or blocker declarations. Regressions cover these cases and exercise the existing suffix rules with tracked files.

## Review Triage Log

- Medium, fixed. Exact classification now accounts for a root-level submodule directory. The Git index distinguishes its pointer from a broad directory classification.
- Medium, fixed. A File List entry such as `elsewhere/Vendor` cannot satisfy a task naming the root-level `Vendor` pointer.
- Medium, fixed. A bullet containing only a hidden comment supplies no general evidence. Regressions cover ordered and unordered markers.
- Medium, fixed. The task scanner removes hidden comments before it looks for a blocker in the task text. A visible blocker still counts.
- Medium, fixed. A fence inside a hidden comment leaves subsequent headings and evidence visible. An unterminated comment reports its opening line.
- Medium, fixed. Backtick spans preserve literal comment delimiters across lines in the same paragraph. An unmatched backtick does not hide a later comment.
- Medium, fixed in follow-up review. Removing a leading comment cannot promote trailing text into a heading or a File List bullet. The regression checks both single-line and multiline comments.

## Verification

- `python -m unittest -v eng.tests.test_validate_story_artifacts` ran 237 tests. 235 passed and two optional story-automator integration tests skipped because that component is absent.
- `python eng/validate-story-artifacts.py` passed repository integrity validation. Parsed metadata matched the previous validator for all 328 previously tracked top-level Markdown artifacts.
- An end-to-end CLI run rejected a missing root-level pointer, escaped the U+2028 in the story filename, and accepted the declared pointer change. Each of three isolated mutations to the filename-suffix checks caused the strengthened fixture to fail.
