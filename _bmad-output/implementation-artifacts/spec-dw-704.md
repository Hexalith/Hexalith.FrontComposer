---
title: 'DW-704 Reject null commands before dispatching'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Validate the command argument in the compatibility extensions before choosing the dispatch implementation, across both Contracts target frameworks.

</frozen-after-approval>

## Implementation Notes

- Both compatibility dispatch extensions validate the command on each framework target. Review expanded the regression to cover basic and lifecycle-aware services with callbacks.
