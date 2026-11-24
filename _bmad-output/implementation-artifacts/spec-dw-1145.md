---
title: 'DW-1145 Roll back partial LastUsed subscriptions'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

**Problem** If a generated subscriber fails to register both action handlers, DI cannot dispose the partially constructed instance.

**Approach** Undo subscriber registration and release its cancellation source when the constructor fails.

</frozen-after-approval>

## Implementation Notes

- The generated constructor rolls back both actions on failure. Runtime coverage exercises a failing second registration.

## Review Triage Log

- Retain the original exception if rollback fails and leave any retained submission callback inert.
