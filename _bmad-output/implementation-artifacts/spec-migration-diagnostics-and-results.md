---
title: 'Preserve diagnostic and outcome evidence in CLI migrations'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1633, DW-1634, DW-1635, DW-1637, DW-1640]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make `frontcomposer migrate` accurately report manual diagnostics and the outcome of planned edits. Report malformed sidecar payloads and inaccessible sidecar directories through the existing manual-only sentinel. Match valid source paths after project-relative normalization and keep display sanitization out of lookup. Account for all unwritten planned files after apply cancellation. When every discovered document was skipped, report the skips without claiming the project's source was checked and found unchanged.

</frozen-after-approval>

## Implementation Notes

The reader keeps source keys and unreadable-sidecar paths in separate collections. It reports malformed diagnostic entries and continues through readable entries and directories. Source discovery prunes excluded directories before recursing so an unreadable `obj` can be reported by the sidecar reader.

The planner keeps the existing unchanged result for a clean or empty plan. A plan that skipped every discovered document reports only its actual findings. Apply preserves successful writes and emits a failed entry for each unwritten planned file after cancellation. It finishes a completed apply report even if the write phase was interrupted.

## Review Triage Log

- Medium, fixed. Source discovery prunes excluded directories before opening them. The CLI test covers an unreadable `obj` child.
- Medium, fixed. Sidecar traversal handles failures per directory. The same test retains findings from a readable configuration.
- Medium, fixed. Invalid JSON string decoding produces a sentinel without dropping later entries.
- Medium, fixed. Missing and non-string diagnostic IDs produce a sentinel before unrelated diagnostic IDs are filtered.
- Medium, fixed. Valid source keys under `__sidecar__/` no longer share the sentinel lookup.
- Medium, deferred. A valid diagnostic for an absent eligible source still has no matching project document. The deferred-work ledger records the remaining case.
- Medium, fixed. Text reports distinguish an incomplete apply from a dry run.
- Low, coverage extended. Deterministic apply tests cover cancellation before the first write and after one completed write. They assert file contents and reported outcomes. A timed interruption through the full CLI JSON path was not exercised.
- Low, fixed. The CLI reference, package README and migration how-to describe the new operational behavior.

## Verification

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug -m:1` passed with zero warnings or errors using SDK 11.0.100.
- `dotnet test --project tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug --no-build` passed all 171 tests with zero skips.
- `python3 -m unittest tests/eng/test_validate_contract_artifacts.py` passed all four tests.
- `aspire run` could not start because the Aspire CLI is absent. The changed migration command was built and exercised independently of AppHost.
