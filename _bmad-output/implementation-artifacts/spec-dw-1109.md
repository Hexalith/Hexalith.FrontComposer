---
title: 'DW-1109 Restore the Counter sample last-used increment amount'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

On interactive entry to the Counter overview, use the same tenant- and user-scoped LastUsed provider that records confirmed commands to initialize the editable Amount field. Keep the constructor default when storage has no usable amount. Cover submission, persistence and reopening after navigation in the live Counter sample.

</frozen-after-approval>

## Implementation Notes

- The Counter overview uses LastUsed to set the form initial value after interactive rendering. Review preserved the notice-only meaning of LastUsedDisabled and bounded the browser read. The live test checks amount persistence across navigation and fallback for unusable values.
- Full-suite validation exposed hosts without the LastUsed registration. The sample renders the constructor default in those hosts. The 31 Counter verification tests and the live browser suite passed after the repair.
