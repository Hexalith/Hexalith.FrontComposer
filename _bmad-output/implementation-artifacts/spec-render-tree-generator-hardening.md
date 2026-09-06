---
title: 'Harden render-tree generator emit and rewrite boundaries'
type: 'bugfix'
created: '2026-09-06'
status: 'ready-for-dev'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - _bmad-output/planning-artifacts/architecture.md
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Render-tree generation has fail-safe gaps around comment trivia, invalid emitted method identifiers, non-positive truncation bounds, and repeated disposal, while several literal-sequence and masking contracts lack direct regression coverage.

**Approach:** Harden the generator boundaries with conservative detection, generation-time identifier validation, fail-soft bounded truncation, and atomic idempotent disposal; add focused tests while preserving generated behavior for valid inputs.

## Boundaries & Constraints

**Always:** Keep `SourceTools` netstandard2.0-compatible; preserve valid generated render behavior, literal numbering, first-disposal ordering, event-name string escaping, generated hint/artifact identities, and the Fluent UI v5 surface. Validate the emitted wrapper method identifier before mutating its output buffer. Treat Roslyn inspection as authoritative for surviving render-tree sequence arguments.

**Never:** Do not edit `.bmad-loop/**`, `_bmad-output/implementation-artifacts/deferred-work.md`, public API baselines, package/dependency configuration, unrelated format-hint paths, or hand-authored generated output. Do not reinterpret `eventName` as an identifier: it is an escaped `EventId` string and valid punctuation must remain accepted.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Rewrite residue | First sequence argument contains prefix/postfix increment after block or line-comment trivia | `AssignLiteralsOrFail` detects the surviving call | Throw `InvalidOperationException` naming the call and ASP0006 |
| Invalid emitted identifier | Nonblank invalid/reserved/escaped `methodName` | No generated text is appended | Throw `ArgumentException` for `methodName` |
| Event label | `eventName` contains spaces, punctuation, quotes, or backslashes | Escaped string literal remains valid | No error expected |
| Sequence mask | Supported call begins with a decimal literal versus `seq++`/`seq ++` | Literal becomes `#`; runtime expression remains unchanged | No error expected |
| Truncation bound | Non-empty value with bound `<= 0`, `1`, or positive interior bound | Empty, ellipsis-only, or bounded prefix-plus-ellipsis respectively | No incidental span exception |
| Repeated disposal | Generated grid or non-grid view is disposed more than once, including concurrent entry | Cleanup runs once; later callers return | No repeated dispatch, JS cleanup, unsubscribe, or child disposal |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` -- `FindRuntimeSequenceArgument` prefilter currently misses increment expressions separated from `(` by trivia; the existing Roslyn walk is definitive.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` -- `ValidateArguments` runs before buffer mutation; `methodName` becomes field/method syntax, while `eventName` is escaped literal text.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` -- grid-only `_disposed`, grid `DisposeAsync`, non-grid `Dispose`, and emitted `Truncate` helper.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeText.cs` -- test helper masks supported decimal sequence arguments and intentionally leaves runtime counters visible.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` and `GeneratedLogMethodEmitterTests.cs` -- direct fail-closed and validation seams.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`, `RazorEmitterExpandInRowTests.cs`, and `RazorEmitterVirtualizationTests.cs` -- focused badge, expand-row, literal-sequence, truncation, and disposal output contracts.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- live generated grid teardown test with disposal-count instrumentation.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/**/*.verified.txt` -- intentional RazorEmitter snapshots; update only files changed by the hardened emitted helper/guards.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` -- eliminate trivia-driven prefilter false negatives and cover block/line comments without changing clean literal output.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLogMethodEmitterTests.cs` -- reject invalid emitted method identifiers before append; retain valid and arbitrary escaped event names.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- directly cover every supported literal-call mask plus tight/spaced runtime-counter negative controls.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs` -- call `ShouldUseLiteralRenderTreeSequences` on mapped-badge and Default/StatusOverview expand-row output; keep the literal `800` pin.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`, and affected `tests/Hexalith.FrontComposer.SourceTools.Tests/**/*.verified.txt` snapshots -- emit and pin fail-soft non-positive truncation plus atomic once-only guards for grid/non-grid disposal.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- dispose the live generated grid repeatedly and assert subscription cleanup occurs exactly once.

**Acceptance Criteria:**
- Given valid generator models, when all hardened emitters run, then output remains syntactically valid in Debug and Release and existing observable rendering behavior is unchanged.
- Given mapped badge and both expand-row grid strategies, when their full emitted sources are inspected, then every governed render-tree sequence is a literal and no ASP0006 control is emitted.
- Given grid and non-grid generated views, when disposal is entered repeatedly, then only the first entry performs teardown and finalization suppression remains on that path.

## Spec Change Log

## Review Triage Log

## Design Notes

`eventName` is deliberately excluded from identifier validation because it is emitted only through `GeneratedLiteral.Escape` into an `EventId` string literal; rejecting it would break valid existing inputs without preventing invalid syntax. Use an atomic exchange for disposal so concurrent callers cannot both pass a read/write guard. For truncation, bounds `<= 0` fail soft to `string.Empty`; positive-bound output remains byte-for-byte equivalent.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none` -- expected: all SourceTools tests pass with approved snapshots.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` followed by the built test assembly filtered to `CounterProjectionView_Dispose_UnsubscribesIndicatorHandlerExactlyOnce` -- expected: repeat disposal passes with one subscription disposal.
