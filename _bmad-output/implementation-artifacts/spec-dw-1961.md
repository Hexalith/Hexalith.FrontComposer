---
title: 'DW-1961 Remove tautological route tenant assertions'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** Route tests assert values from a test fixture that never establishes a browser tenant.

**Approach** Keep the navigation assertions and remove the fixture assertion from this route suite.

</frozen-after-approval>

## Implementation Notes

- The removed fixture only returned strings. The Counter host supplies the demo user through its Test configuration.

## Review Triage Log

- All live route and focus assertions remain connected to the browser state.
