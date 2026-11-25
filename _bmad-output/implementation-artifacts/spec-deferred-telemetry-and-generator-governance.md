---
title: 'Deferred telemetry and generator governance'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete six actionable entries in ledger order after DW-958. Preserve skip reasons in the ledger, verify each change, commit each item separately, and validate the combined changes before pushing main.

</frozen-after-approval>

## Implementation Notes

- DW-960 adds a host-owned OpenTelemetry tracing recipe using the existing FrontComposer activity source.
- DW-961 verifies the typed connection log payload during recovery, window rollover, and disposal.
- DW-964 verifies telemetry redaction across the five failure paths named in the ledger.
- DW-966 preserves supported correlation identifiers that exceed the previous logging bound and signals overflow explicitly.
- DW-968 uses the same sanitized command type for accepted-response diagnostics and activity tags.
- DW-969 checks real generated C# for forbidden infrastructure references.

## Review Triage Log

- Medium, fixed. The generated-code scanner reparses output with all combinations of the emitter's current debug and target-framework symbols. A probe exercises a forbidden reference in a conditional branch.
- Medium, fixed. The generator fixture verifies that the populated Level 2 manifest was emitted and scans the full output.
- Medium, fixed. Semantic binding detects provider references inherited through global and alias imports. Matched syntax covers unresolved provider names in a consumer without the provider package.
- Low, fixed. Semantic binding distinguishes a provider-qualified call from an ordinary member access on a local with the same name.
- Medium, deferred. The existing accepted-response warning includes the provider-supplied media type. This is a separate field from the DW-968 command type.
- Low, rejected. DW-966 verifies both identifier helpers at the length boundaries. The dispatch test additionally exercises the maximum supported wire correlation ID through the actual activity pipeline.
- Low, rejected. The five failure cases verify the structured events and span tags emitted by the current paths. The reviewed paths do not emit additional log scopes or activity events for these failures.
- Low, rejected. The fallback polling span and its structured log are covered. The additional nudge rerun continues to pin the existing budget and verifies its structured log.
- Medium, deferred. The HTTP 304 corrupt-cache path retries after its wrapper log. The verified existing outer-catch fix covers the fresh HTTP 200 schema parser.
- Medium, deferred. The existing HTTP 200 response sets the success outcome before parsing. The ledger tracks this alongside the cache-path cause.
