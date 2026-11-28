---
title: 'Exercise fresh SourceTools mutation findings'
type: 'chore'
created: '2026-11-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="the user authorized a separate targeted test change">

## Intent

Add focused tests for the 69 fresh native mutant IDs identified by the exact `3297cbdc` replay. Reuse existing tests where their assertions already distinguish the mutation and make them eligible for the affected cohort. Test supported behavior through parser, transform, and generated-code entry points. Use exact bounded literal replays to record which tests distinguish a mutant. Present any evidence-backed deferrals for explicit owner approval. Preserve existing receipt-bearing tests, mutation allowances, thresholds, and release gates. Treat focused literal evidence separately from any future authenticated native run.

</frozen-after-approval>

## Implementation Notes

Added three SourceTools test classes under `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/` for canonical payloads, nested projections, and parser identity. Marked the existing split-file HFC1012 test as a mutation error regression so both cohorts select it. The tests exercise parser and transform behavior, compile and inspect generated artifacts, and check emitted registration manifests.

The isolated, unmutated Debug build passed with zero warnings and errors. All 19 selected baseline tests passed. The isolated literal replays covered all 69 exact source replacements from the recorded native identity crosswalk. Sixty replacements caused an assertion failure in the selected test, eight hash-arithmetic replacements passed, and one replacement exceeded the 25-second test bound. The two literal builds for `12397` and `14174` demoted CS8602 in their disposable workspaces so the tests could exercise the replacement behavior. Both produced the expected test failure.

The [focused replay evidence](../../artifacts/mutation/fresh-controls-1df797a8-20261128T155433Z/controls.json) records the exact source replacements, test hashes, and individual outcomes. Its SHA-256 is `ec7d77933c3a9db75e25f7236c382aa9ad68772472b6afa8d6194bf151cf1853`. The controls used source base `1df797a868fc6c5fe29c6610ae91d1e1f6cd234d` and the recorded identities from native source `3297cbdc559f28a3250c97b963a820e06cd1700a`. The changed tests still need an authenticated native cohort run before their results can support a gate decision. Story 16.3 and G-3 remain open.

## Owner-approved deferrals

Both owner decisions were recorded on 2026-11-28 at 15:57 UTC.

- **Bounded hash arithmetic.** The owner approved deferring `13760`, `13761`, `15088`, `15089`, `15413`, `15414`, `15735`, and `15736` on the tested contract. All eight preserved equal-value hashing and the observed effect of nested identities on the hash in the selected fixtures. All eight had survived both historical native cohorts. Hash-distribution performance stays open.
- **Namespace collision nonprogress.** The owner approved deferring `14194`. This replacement kept selecting the same occupied namespace root and exceeded the 25-second isolated replay bound. The historical native cohort recorded `Timeout` in Happy Path and `NoCoverage` in Error Handling. Any future native allowance requires separate review.

## Review Triage Log

- Reserved the generated namespace root in the new nested fixtures so the focused control for `14191` reaches a finite assertion. The replay for `14194` exercised the occupied-root loop under the process time bound.
- Scoped the CS8602 demotion to the two disposable literal builds and verified that both tests failed at runtime.
- Reworked the dynamic assignment fixture to exercise a derivable type and assert both conversion and assignment in rendered code.
- Exercised the explicit override through the renderer with compiled imported metadata and checked the positive and negative output.
- Asserted the expected generated files, compiled them, and resolved the expected CLR names in a collectible assembly load context.
- Checked rejected sources for CLR nested-name separators and encoded artifact hint names.
- Kept the hash observer focused on collection behavior and nested-identity participation. The proposed incremental-driver cache test would exercise a separate behavior.
- Added assertions against the emitted registration manifest.
