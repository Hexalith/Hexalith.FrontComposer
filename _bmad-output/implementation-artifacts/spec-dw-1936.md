---
title: Validate render-tree sequence arguments in conditional branches
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: 7fcd90a054ef249960005e0df7b772ac590638d5
context: []
---

<frozen-after-approval reason="DW-1936 from the deferred work ledger">

## Intent

Given generated code whose render-tree call is reachable only with a different set of preprocessor symbols, reject a nonliteral sequence argument before it reaches the consumer. Preserve literal-only output and fast-path unconditional source. Fail closed when a document exceeds the supported conditional symbol count.

</frozen-after-approval>

## Implementation Notes


The rewriter still leaves documents it cannot safely edit alone. The final validation checks complete parses for each combination of up to four conditional symbols. Larger documents fail closed so the generator never silently emits an unchecked branch.

## Review Triage Log

- Added a symbol present only in an elif, reachable and impossible branches at the symbol limit, and XML documentation for the limit.

## Verification

All 44 render-tree sequence rewriter tests passed.
