---
title: Refresh cached command defaults after metadata updates
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: a70639d586334b4960fc12913b1019b4fe96b895
context: []
---

<frozen-after-approval reason="DW-1140 from the deferred work ledger">

## Intent

Given a command whose defaults have been read, a .NET metadata update invalidates the cached reflection results and constructor defaults. Within a metadata generation, a renderer continues to use one constructor instance for related fields and reuses the cached reflection result for an explicit default.

</frozen-after-approval>

## Implementation Notes


Both providers look up a cache for the current metadata generation. A renderer keeps one constructor instance for related property defaults within a generation, and obsolete cache generations can be collected.

## Review Triage Log

- Serialized the two metadata-reset tests in one test class.
- Each lookup captures one generation. An update during a multi-property render may affect subsequent lookups, which the next render refreshes.
- The tests exercise the registered callback; they do not drive a live compiler delta.
- A miss uses the same generation-scoped cache as a hit, so a metadata update discards both.

## Verification

Shell default-provider and generated-form tests passed all 24 cases.
