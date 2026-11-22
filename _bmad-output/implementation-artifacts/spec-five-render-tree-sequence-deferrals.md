---
title: 'Close five render-tree sequence deferrals'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Close DW-1937, DW-1938, DW-1939, DW-1942, and DW-1953. Generated render-tree calls must carry integer sequence literals, including calls to `AddComponentParameter` and calls that name `sequence` out of order. The emitter must identify surviving runtime expressions, the direct generated-output guard must check Debug and Release, and the snapshot helper must mask only actual sequence literals so runtime expressions stay visible.

</frozen-after-approval>

## Implementation Notes

- SourceTools owns the Roslyn sequence-argument scan. The direct output guard and masking helper reuse its method catalog and argument selection.
- The isolated checkout uses the direct dependency revisions pinned by FrontComposer `main` at `0d411ae8957470270767443944c473457a3f6d19`.
- The emitter also inspects the Release parse when Debug cannot see every conditional region. The direct guard inspects the .NET 11 branch used by generated command validation.

## Review Triage Log

- Patched the snapshot masking regression in Release branches. The regression case also checks that a preprocessor-controlled arithmetic suffix stays visible.
- Patched the direct guard for the .NET 11 symbol and added separate failure cases for runtime sequences compiled in Debug and Release.
- Patched the emitter check for Release-only runtime expressions. DW-1936 remains open for arbitrary preprocessor symbols.
- Rejected a low-risk name collision with a same-named method on a different receiver. The scanner is scoped to controlled emitter output, and the emitters do not produce a matching non-render-tree call.
- Updated the five selected ledger records with their resolutions.

## Verification

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --no-restore -m:1 /nr:false` succeeded with zero warnings and errors.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net11.0/Hexalith.FrontComposer.SourceTools.Tests.dll -maxthreads 2` passed all 1,660 tests, including the packaged consumer checks and the drift benchmarks.
- `aspire run --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --isolated --nologo` exited 134 before launching the AppHost because the CLI could not create its home directory on the read-only filesystem.
