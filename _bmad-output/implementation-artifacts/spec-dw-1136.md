---
title: 'DW-1136 Redact rejected command return paths'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** The generated renderer writes rejected return paths to a structured log event.

**Approach** Keep fallback navigation and the correlation ID while excluding the route input from the rejection event.

</frozen-after-approval>

## Implementation Notes

- The generated full-page renderer logs the unchanged event ID and navigates home after confirmation.

## Review Triage Log

- The test supplies a known lifecycle correlation ID and asserts the event state and fallback URI.
