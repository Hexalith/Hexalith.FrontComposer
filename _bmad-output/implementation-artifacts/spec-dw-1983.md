---
title: 'DW-1983 Document the projection fallback scheduler contract'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Document every public scheduler member, including registration ownership, conflicting lanes, refresh counts and the default interface implementations.

</frozen-after-approval>

## Implementation Notes

- The XML comments cover all six interface members. Review checked exception coverage and validation against the scheduler implementation and the shared EventStore segment validator.
