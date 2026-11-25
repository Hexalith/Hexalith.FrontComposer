---
title: 'Deferred customization override follow-ups'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete six actionable deferred-work entries in ledger order after DW-933. Verify and record each result, commit each separately, and validate the combined build before pushing to main.

</frozen-after-approval>

## Implementation Notes

- DW-937 uses the existing Counter sample and contract mismatch fixture. Add an opt-in Level 4 fault and a compile-only accessibility fixture.
- DW-941 collects current SDK evidence for a Level 4 Razor body edit and records which edits require rebuilding and restarting.
- DW-942 diagnoses statically incompatible typed registrations at build time. Startup validation continues to cover descriptors supplied at runtime.
- DW-943 documents the render-scoped lifetime of the Level 4 context and its delegates. Verify that repeated rendering supplies the updated context.
- DW-944 exposes rejection details through an optional registry interface. Duplicate registration remains a startup failure.
- DW-945 shares packing factors and uses the existing compatibility policy across customization levels.

## Review Triage Log

- Medium, fixed. The owned Counter lane asserts HFC1050 in the opt-in build. The existing externally hosted mismatch scenarios retain their configuration. A separate host proves Level 2 fallback and a second command after the fault.
- Medium, fixed. SourceTools compares CLR-equivalent type shapes, rejects incompatible direct-parameter attributes, and checks typed method references and provable component shapes in generic helpers. Runtime validation recognizes the same parameter flags.
- Low, fixed. HFC1043 documents the analyzer reference needed in the project containing the registration. The customization reference explains discovery of the optional registry diagnostics contract.
- Medium, deferred to DW-1989. The existing SourceTools Level 2 parser duplicates the version comparison and has source-bound mutation evidence. The new parity test detects version drift.
- Medium, already tracked by DW-948. Invalid CLR types supplied through raw descriptors can fail generic construction. Typed registration cannot supply those types.
- False as a scope concern. DW-944 asks for rejection enumeration. The contract-mismatch panel keeps its version-specific purpose and custom tooling can consume the new optional interface.
- False as a coverage concern. The existing Level 4 browser test verifies the normal replacement and Level 3 slot. The diagnostic suite focuses on the stale and fault cases. The owned hosts also exercise the warning build. External hosts supply prebuilt fixtures.
- False as a scope concern. Archived Story 6.4 T8 requires live observation for a Razor body edit and documentation of the rebuild categories. The guide identifies which evidence supports each.
