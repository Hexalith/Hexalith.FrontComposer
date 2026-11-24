---
title: 'DW-1940 Validate packaged analyzer sequences with Roslyn'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The packaged consumer check only matches simple postfix increments.

**Approach** Run the shared syntax-based assertion on every emitted package source and report the affected filename.

</frozen-after-approval>

## Implementation Notes

- The package consumer now checks emitted source under active and reachable conditional configurations.

## Review Triage Log

- Preserve the filename in failures and cover the combined NET11 and FEATURE branch.
