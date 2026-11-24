---
title: 'DW-799 Verify configured Markdown render bounds'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Use distinct non-default row, field, document and cell limits and assert the runtime render contract publishes the configured values.

</frozen-after-approval>

## Implementation Notes

- The runtime test asserts four distinct non-default option values through the public descriptor registry. Review replaced the obsolete reflection helper with the published accessor.
