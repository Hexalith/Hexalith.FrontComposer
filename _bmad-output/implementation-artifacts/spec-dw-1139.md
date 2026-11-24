---
title: 'DW-1139 Index command form field filters'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** Generated forms scan the supplied field filter for each field and validation descriptor.

**Approach** Build an ordinal membership set when the host supplies parameters and share it across field rendering and validation summaries.

</frozen-after-approval>

## Implementation Notes

- Parameter updates rebuild the index for both field rendering and validation summaries.

## Review Triage Log

- Null, empty, and singleton filters keep the inexpensive existing path. The test reuses a mutated multi-entry array.
