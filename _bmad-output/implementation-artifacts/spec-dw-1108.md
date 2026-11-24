---
title: 'DW-1108 Verify inline popover closure after confirmation'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Exercise the live Counter sample from opening the Increment popover through submission and confirmation. Require the form to remain open while the command is pending, close after confirmation, and return focus to its trigger.

</frozen-after-approval>

## Implementation Notes

- The live Chromium scenario now exercises the actual Increment form. Review tightened the assertion to observe Syncing while visible, require a terminal change in the anchored Fluent popover state, and verify returned focus.
