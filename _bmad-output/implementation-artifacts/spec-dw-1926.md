---
title: 'DW-1926 Test settings storage-key parity'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The browserless helper checks Unicode email casing while several encoding and normalization paths lack vectors.

**Approach** Extend its parity assertions to tenant normalization, case-preserving non-email users, and URI escaping.

</frozen-after-approval>

## Implementation Notes

- The selected helper test retains its existing CI entry point and checks additional parity vectors.

## Review Triage Log

- The vectors distinguish .NET trimming behavior from JavaScript trim on NEL and BOM.
