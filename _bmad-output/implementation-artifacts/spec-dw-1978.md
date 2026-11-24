---
title: 'DW-1978 Cover shell navigation with full-page Axe scans'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The legacy command scan does not cover shell chrome, where nested interactive controls were reported.

**Approach** Scan the live shell in the desktop module flyout and mobile drawer states for nested interactive controls.

</frozen-after-approval>

## Implementation Notes

- Both live shell states were clean on the current Fluent markup. Chromium exercises each opened state.

## Review Triage Log

- Require an applicable rule pass so an empty scan cannot satisfy the check.
