---
title: 'DW-702 Validate malformed shortcut chords before formatting'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Reject empty or extra chord parts at the public label formatter boundary with an argument error. Preserve labels for valid normalized bindings.

</frozen-after-approval>

## Implementation Notes

- The formatter accepts supported labels and rejects blank or excess chord parts. Review kept the split bounded while retaining the malformed-input regression cases.
