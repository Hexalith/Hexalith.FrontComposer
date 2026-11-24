---
title: 'DW-705 Decline invalid command names before querying registries'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Return false for null, empty or whitespace command type names before calling optional registry interfaces. The default and feature-aware paths should agree on the outcome for invalid names.

</frozen-after-approval>

## Implementation Notes

- The extension methods return false for invalid names on both registry paths. Review clarified nullable inputs and preserved the legacy fallback for valid command names.
