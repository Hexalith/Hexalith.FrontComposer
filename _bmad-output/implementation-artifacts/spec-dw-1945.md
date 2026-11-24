---
title: Decline static assignment for property types generated code cannot access
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: 7fcd90a054ef249960005e0df7b772ac590638d5
context: []
---

<frozen-after-approval reason="DW-1945 from the deferred work ledger">

## Intent

Given metadata-only derivable properties with inaccessible referenced types or unresolved types, preserve their provider fall-through without emitting a typed assignment that fails the consumer build. Keep typed assignment for accessible, source-nameable types. Prove both the parser flags and the output compilation with warnings as errors.

</frozen-after-approval>

## Implementation Notes


The parser checks whether the renderer can name the property type recursively. An error type, inaccessible type, or compiler-blocked type uses the existing provider fall-through path.

## Review Triage Log

- Added an inaccessible array member and a compiler-accessible member to the same metadata fixture.
- Moved the non-SZ array guard into the recursive type check and applied the compiler-blocking attribute checks to named types.
- The existing alias-only compilation and new accessible metadata assertion verify that generated assignments remain typed when the source can name them.

## Verification

All 59 parser tests passed.

The larger Python suite detected stale mutation evidence. The parser uses a separate accessibility check so its 96 reviewed literal mutation spans remain exact. The same verification found two stale fingerprints from the preceding generator batch. The repair rebinds those 59 identities to unchanged source text at their current locations. The retained observations, allowed statuses, and control execution evidence are preserved. All 27 finite mutation-validation tests and all 59 parser tests passed.

The full suite also exposed a spelling mismatch in the positive generated-code assertion. The emitter escapes every member name with @. The fixture now checks the escaped typed assignment. All five focused generator integration cases passed, including compilation with warnings as errors.
