---
title: Verify generated form navigation after confirmed submission
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: 5747dcaadd75c5c4bc823fcb9d7b243a47a5ae60
context: []
---

<frozen-after-approval reason="DW-1973 from the deferred work ledger">

## Intent

Given a browser submits the generated Configure Counter form with an external ReturnPath, wait for confirmation-driven navigation to the application home. Given a valid in-app ReturnPath, verify confirmed submission returns to that URL. Keep the existing unsafe breadcrumb check.

</frozen-after-approval>

## Implementation Notes


The live browser submits both forms, observes a pending lifecycle on the command route, then waits for the exact destination and its heading. The external URL is intercepted and any navigation attempt there fails the scenario.

## Review Triage Log

- Added a deterministic confirmation delay and required the pending state on the original route before checking the final URL.
- Asserted the home and Counter headings and detected external navigation attempts.

## Verification

The Story 2.2 live browser run passed all eight executed scenarios, including S7. It reported five existing skips.
