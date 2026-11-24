---
title: 'DW-1119 Release detached popover handles'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The circuit registry holds the last open renderer even after Blazor removes it.

**Approach** Keep a weak registration so the circuit can still close a live popover without rooting a detached renderer.

</frozen-after-approval>

## Implementation Notes

- The registry tracks a weak renderer handle and holds each live previous renderer only during the close operation.

## Review Triage Log

- The collection regression complements the existing close ordering and stale release tests.
