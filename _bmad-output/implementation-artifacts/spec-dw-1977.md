---
title: Exercise Escape closure on the live inline popover
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: e6ddd2410eb5dd8cd4cfc38f955035d469494edb
context: []
---

<frozen-after-approval reason="DW-1977 from the deferred work ledger">

## Intent

Given the Counter inline popover is open with focus on its Cancel button, pressing Escape closes it and returns focus to the opener. Keep the existing Cancel-by-Enter scenario in the same live run.

</frozen-after-approval>

## Implementation Notes


The existing product passed a live browser probe before this change. The Story 2.2 run now opens the popover, presses Escape on its Cancel control, verifies closure and restored focus, then reopens and verifies Enter-driven cancellation.

## Review Triage Log

- Scoped the opener to the generated trigger ID to exclude the inner submit button with the same name.

## Verification

The live Chromium scenario and all six runner unit cases passed.
