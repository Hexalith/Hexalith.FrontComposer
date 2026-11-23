---
title: 'Verify v1 benchmark gate evidence'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
closes_deferred: [DW-1653, DW-1654, DW-1657, DW-1658, DW-1659]
---

<frozen-after-approval reason="human-owned intent">

## Intent

A v1 benchmark decision requires one consistent, redacted result for each of the 20 distinct prompts. Comparing a run to an approved baseline requires matching corpus hash, scoring version, validator version, redaction policy version and provider configuration hash. A legitimate miss contributes to the denominator. Pin the published v1 prompt IDs and prove the cache can reuse an independently computed equivalent key.

</frozen-after-approval>

## Implementation Notes

The C# benchmark harness owns the gate and embedded prompt loader. Use the existing candidate-only status for complete runs without an approved baseline and the existing invalid-evidence status for rejected inputs. Preserve the 80% target and stronger approved thresholds.

The prompt loader and gate both check uniqueness because callers can construct a prompt set directly. The gate shares row validation with the artifact writer. Each rejected row is counted once even if it fails multiple checks. Approved comparisons hash the supplied v1 prompt set using the same field order and escaping as the Python benchmark runner.

## Review Triage Log

- Medium, fixed. Rows with incompatible baseline or provider metadata previously inflated the pass count. The mixed-run test now checks both reported counts.
- Medium, fixed. A row that asserted redaction success could still contain diagnostics rejected by the artifact writer. The gate and writer now use the same persistence check.
- Medium, fixed. The legacy-metadata regression now clears each missing field independently across every row and the baseline.
- Medium, fixed. The run-side hash comes from the prompt contents. Tests pin the independent Python digest and cover edited prompts, ordered arrays and escaped Unicode.

## Verification

Using SDK 11.0.100 and FrontComposer's committed submodule revisions, both Debug projects built with zero warnings and errors. The benchmark project passed all 24 tests and the MCP project passed all 555 tests, with zero skips. The MCP suite exposed an extra cross-assembly call in the first implementation. Computing SHA-256 with the .NET API kept corpus hashing within the benchmark project and the suite passed on rerun.

```sh
dotnet build tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj -c Debug
dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug
dotnet test --project tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Debug --no-build
dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj -c Debug --no-build
```

With all ten direct dependency checkouts at their fetched main revisions, the benchmark project also built cleanly and both benchmark harness test classes passed all 22 tests. The Python prompt-set validator returned 20 valid prompts and the same v1 corpus hash. The AppHost baseline command `aspire run --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --isolated --nologo` exited 127 because the Aspire CLI was unavailable.
