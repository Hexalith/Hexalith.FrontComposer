---
title: 'DW-1925 Isolate browserless Playwright reports'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The contract and settings helper commands can leave reports under the same paths as hosted accessibility runs.

**Approach** Give each browserless command its own default reports and retain the explicit output overrides.

</frozen-after-approval>

## Implementation Notes

- Separate scoped HTML, JSON, and JUnit paths let hosted validation use its own report filenames.

## Review Triage Log

- A scratch run confirmed browserless reporting preserves hosted files. The existing script guard verifies the scoped commands.
