---
title: Drain packaged analyzer process output concurrently
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: a70639d586334b4960fc12913b1019b4fe96b895
context: []
---

<frozen-after-approval reason="DW-1907 from the deferred work ledger">

## Intent

Given a packaged analyzer test child that fills stderr before closing stdout, collect both streams and return its true exit code. Given caller cancellation, stop the child and its process tree, observe process exit, and propagate cancellation to the test.

</frozen-after-approval>

## Implementation Notes


The packaged-consumer runner keeps the original exit code and output order. Cancellation kills the running process tree and waits for the root process before rethrowing.

## Review Triage Log

- Added an atomic PID handoff and assertions for both the parent and descendant. The test waits for cancellation cleanup even on an assertion failure.
- Process-tree cleanup applies while the launched process owns its descendants. A detached process whose parent has exited is outside the runner’s ownership. Redirected reads still observe caller cancellation.

## Verification

Both process regressions passed. One filled stderr before closing stdout and the other verified process-tree cancellation.
