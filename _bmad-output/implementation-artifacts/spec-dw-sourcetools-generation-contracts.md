---
title: 'Harden SourceTools generation contracts'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1943, DW-1944, DW-1946, DW-1950, DW-1951]
---

<frozen-after-approval reason="human-owned intent">

## Intent

Reject invalid log levels and malformed or conflicting logging parameter declarations before the emitter appends generated code. Preserve nullable elements and generic arguments in assignment type names while omitting the nullable annotation on the outer reference type.

Extend the parser and renderer tests to verify that nullable assignment types compile with warnings as errors. Make exact assertions on the generated fallback arms portable across LF and CRLF. Close the five referenced ledger items after verification.

</frozen-after-approval>

## Implementation Notes

- Logging validation runs before the output buffer is touched. The emitter checks level membership, identifier collisions, and type syntax. Callers supply types that bind as logging generic arguments.
- The type formatter emits consecutive array ranks together and starts a new group at a nullable inner array. The renderer tests compile mixed-rank arrays and nullable scalar assignments with warnings as errors.
- Renderer case assertions normalize LF and CRLF. The test identifier inventory was resealed for six added declarations and one renamed declaration.

## Review Triage Log

- Medium, patched. A nullable inner array separating different ranks could change the assigned type. The mixed-rank parser and renderer fixtures now verify the correct shape.
- Medium, patched. The existing CA1707 seal required the refreshed test inventory. The Shell governance test validated the new count and hash.
- Low, patched. Duplicate tuple element names are rejected before append. The negative theory verifies that the existing buffer stays intact.
- Low, documented. Named type and ref-like binding depends on the generated compilation. The emitter's parameter contract assigns that check to its callers.
- Low, patched. The valid-level and nullable generic logging examples now compile against the real logging references.
- Low, patched. The verification results and deferred-work resolutions are recorded with the batch.

## Verification

- All 1,682 cases in the default SourceTools lane passed across the main run and the rerun of the four DocFX cases.
- The SourceTools test project built in Debug with zero warnings and errors. All 56 logging emitter cases passed.
- The nullable parser case, warnings-as-errors renderer case, and both line-ending cases passed. All four DocFX runbook cases passed after restoring the pinned local tool.
- The Shell test project built in Release with zero warnings and errors. Its identifier inventory test passed.
- Aspire CLI 17.0.0 could not start the AppHost because its first-run sentinel requires a writable user home. The generated component regressions were exercised through compiled fixtures.
