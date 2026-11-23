---
title: 'Validate skill corpus registration and reads'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1650, DW-1661, DW-1665, DW-1666, DW-1667]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make skill resource discovery, reads and release validation agree on the corpus they consume. Reject duplicate skill IDs and collisions with the aggregate manifest URI during parsing. Freeze the provider's view at construction so later changes to caller-owned collections cannot change discovery or manifest metadata. Keep a missing release baseline visible as an advisory and propagate current parser diagnostics with or without a baseline. Reject negative read limits when options are constructed or copied, while allowing zero.

</frozen-after-approval>

## Implementation Notes

The parser checks each canonical URI and skill ID against the identities already registered, including the synthetic manifest. The provider builds discovery, lookup and both forms of the manifest from one copy of the resources and their reference and sample lists.

The release guard retains current parser diagnostics in both comparison paths. The existing missing-baseline diagnostic is informational. Other diagnostics remain blocking by default. The read-cap record validates both its positional parameter and its init accessor.

## Review Triage Log

- Medium, fixed. Startup now applies the same diagnostic validity rule as release validation. Regression coverage exercises advisory-only and mixed snapshots.
- Medium, fixed. A duplicate now reports the input file that declared the collision, even if its front matter names a different source document.
- Medium, deferred. A malformed supplied baseline can appear empty to the comparison. This predates the current-corpus change. The ledger records the case separately for release validation.

## Verification

Using SDK 11.0.100 and FrontComposer's committed submodule revisions, the Shell benchmark prerequisite and the MCP test project built in Debug with zero warnings or errors. The full MCP test project passed all 555 tests with zero skips.

```sh
dotnet build tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj -c Debug
dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug
dotnet test --project tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug --no-build
```
