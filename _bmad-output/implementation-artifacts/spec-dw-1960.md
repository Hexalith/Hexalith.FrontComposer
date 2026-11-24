---
title: 'DW-1960 Assert rendered projection tab after flyout navigation'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The flyout acceptance proof checks the same Counter header on both routes.

**Approach** Assert that the projection tab is selected and its rendered projection grid is visible.

</frozen-after-approval>

## Implementation Notes

- The sample exposes a projection-only marker around the grid or its existing empty state.

## Review Triage Log

- Check actual destination content while allowing the sample to have no counter rows.
