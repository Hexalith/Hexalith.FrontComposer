---
title: 'Cover five MCP schema negotiation regressions'
type: 'chore'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-806, DW-808, DW-809, DW-810, DW-811]
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Protect the existing MCP schema contract with regression coverage for deferred items DW-806, DW-808, DW-809, DW-810 and DW-811. Exercise precedence when either fingerprint uses an unsupported algorithm, validate the identifier length and character boundaries, pin the message key for additive schema changes, and isolate the per-manifest integrity check from skill corpus registration.

</frozen-after-approval>

## Implementation Notes

- The precedence matrix now supplies the two fingerprints directly. It verifies result kind, agent category, message key and permission for side effects for each case.
- The default baseline store is empty. The existing identifier predicate is internal so the test assembly can distinguish validation failures from ordinary lookup misses. Resolver tests also check both argument positions and the empty output on rejection.
- The registry accepts the original aggregate hash and raises `SchemaIntegrityMismatch` after a single nested fingerprint changes.

## Review Triage Log

- Low, patched. The backslash fixture now starts with a valid ASCII prefix so the validator reaches the separator.
- Medium, patched. The additive fixture now takes the baseline fingerprint from the client and the current fingerprint from the server.
- Medium, patched. Every matrix row checks permission for side effects, including both unsupported-algorithm paths.
- Low, patched. The integrity test describes the manifest operation as hashing and uses a valid hexadecimal replacement digest.

## Verification

The MCP test project and its Shell benchmark prerequisite built in Debug with zero warnings and zero errors using SDK 11.0.100 and the recorded Builds revision. The full MCP test project passed all 547 tests, with zero skipped.

```sh
dotnet build tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj -c Debug
dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug
dotnet test --project tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug --no-build
```
