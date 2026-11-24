---
title: Reject duplicate headings in FC-NIP contract tables
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: e6ddd2410eb5dd8cd4cfc38f955035d469494edb
context: []
---

<frozen-after-approval reason="DW-1982 from the deferred work ledger">

## Intent

Given an FC-NIP contract document containing more than one trimmed line that exactly matches the requested heading, reject the ambiguous section before reading its table. Both contract readers keep accepting a single exact heading and ignore other headings with different text.

</frozen-after-approval>

## Implementation Notes


Both readers scan for a second exact heading before locating the selected table. They use the same space and tab handling for heading text.

## Review Triage Log

- Aligned heading whitespace behavior between .NET and JavaScript and used a conflicting second table in each regression.
- The readers still use line-based headings as required by the contract fixtures.

## Verification

All eight .NET FC-NIP contract tests and all four Playwright contract tests passed.
