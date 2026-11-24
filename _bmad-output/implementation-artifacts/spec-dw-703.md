---
title: 'DW-703 Validate test data builder property selectors'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Accept direct writable properties, including boxed values. Reject nested and read-only selectors before building, and validate all BuildMany callbacks before invoking the first one.

</frozen-after-approval>

## Implementation Notes

- The builders accept direct writable properties and safe boxing. Review retained inherited property access and last-assignment behavior and validated a snapshot of batch callbacks before running them.
