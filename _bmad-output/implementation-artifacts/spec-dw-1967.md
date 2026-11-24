---
title: Expose one main landmark on the type specimen route
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: 8938be90c80ae918c799115928504d4d7297d02b
context: []
---

<frozen-after-approval reason="DW-1967 from the deferred work ledger">

## Intent

Given the type specimen is displayed inside the FrontComposer shell, expose the shell’s single main landmark and a named specimen region within it. Preserve the specimen heading and navigation targets.

</frozen-after-approval>

## Implementation Notes


The existing specimen heading now labels a section inside the shell’s main landmark.

## Review Triage Log

- The browser regression checks that the only main landmark is the shell’s own node and that the named region is the specimen root.

## Verification

The Counter Release build passed. Both Chromium checks passed, including the blocking Axe check.
