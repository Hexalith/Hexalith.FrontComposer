---
title: 'DW-892 Pin the tenant-context diagnostic ids per failure category'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Add tests that prove each tenant-context failure category logs its mapped diagnostic id: TenantMissing/UserMissing to HFC2015, MalformedSegment to HFC2016, TenantMismatch to HFC2017, SyntheticTenantRejected to HFC2018 and StaleTenantContext to HFC2019.

</frozen-after-approval>

## Implementation Notes

- Added a five-case theory (`TryGetContextLogsTheDiagnosticIdOfEachFailureCategory`) driven by a case table, a completeness check that the table covers every category `TryGetContext` can report, `RevalidateLogsTheStaleContextDiagnosticId`, and a no-log success theory. Each failure asserts exactly one Warning with EventId 5650 and the structured `DiagnosticId` and `FailureCategory` values. It also checks the case-sensitive message prefix and the returned correlation id, and that the raw sentinel tenant and user values are absent. The local capturing logger now records level, event id and structured state.
- Names avoid underscores, like the other tests added today, so the analyzer identifier inventory seal is unchanged. Production mapping in `FrontComposerTenantContextAccessor.DiagnosticId` is unchanged.
- Validation: `TenantContextValidationMatrixTests` 20/20.

## Review Triage Log

- low, patched — the prefix and category assertions are now case-sensitive.
- medium, patched — a completeness fact compares the case table with `Enum.GetValues<TenantContextFailureCategory>()` minus `None` and the Revalidate-only `StaleTenantContext`.
- low, patched — the test summaries now describe exactly what each test covers.
- low, patched — the tests assert Warning level and EventId 5650.
- low, patched — the tests assert the structured `DiagnosticId` and `FailureCategory` state.
- low, patched — the tests assert that the log carries the returned correlation id and that Revalidate does not reuse the snapshot's id.
- medium, deferred — when the tenant changes, `Revalidate` reports TenantMismatch (HFC2017) rather than HFC2019. That is existing behavior and needs an owner decision, so this change does not pin it.
- low, patched — success rows, including an allowed demo tenant, assert that nothing is logged.
- low, patched — every failure row checks, case-sensitively, that distinctive raw sentinel values are absent from the log.
- low, rejected — the underscore-free names match the other tests added today and avoid an analyzer inventory reseal for a test-only change.
- low, patched — the long snapshot construction is wrapped.
