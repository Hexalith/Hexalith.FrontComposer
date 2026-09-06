---
title: 'Harden render-tree generator emit and rewrite boundaries'
type: 'bugfix'
created: '2026-09-06'
status: 'done'
baseline_commit: 'e72125fb078a10868ea3511e3cc177ab5678b913'
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
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` -- eliminate trivia-driven prefilter false negatives and cover block/line comments without changing clean literal output.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLogMethodEmitterTests.cs` -- reject invalid emitted method identifiers before append; retain valid and arbitrary escaped event names.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- directly cover every supported literal-call mask plus tight/spaced runtime-counter negative controls.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs` -- call `ShouldUseLiteralRenderTreeSequences` on mapped-badge and Default/StatusOverview expand-row output; keep the literal `800` pin.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`, and affected `tests/Hexalith.FrontComposer.SourceTools.Tests/**/*.verified.txt` snapshots -- emit and pin fail-soft non-positive truncation plus atomic once-only guards for grid/non-grid disposal.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- dispose the live generated grid repeatedly and assert subscription cleanup occurs exactly once.

**Acceptance Criteria:**
- Given valid generator models, when all hardened emitters run, then output remains syntactically valid in Debug and Release and existing observable rendering behavior is unchanged.
- Given mapped badge and both expand-row grid strategies, when their full emitted sources are inspected, then every governed render-tree sequence is a literal and no ASP0006 control is emitted.
- Given grid and non-grid generated views, when disposal is entered repeatedly, then only the first entry performs teardown and finalization suppression remains on that path.

## Spec Change Log

- 2026-09-06: Made Roslyn inspection the authoritative literal-sequence test gate, covered remaining comment-trivia increment shapes, and pinned concurrent non-grid disposal. Production emit, truncation, and atomic disposal were already present at baseline; no Razor snapshots changed.

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 12 findings — high 0, medium 1, low 6, false 3, maybe-false 0
- findings:
  - `[low]` `[reject]` The Code Map still describes a trivia-missing prefilter — verified against the current map sentence; the only fix is editing this build's spec, which the workflow rejects.
  - `[low]` `[defer]` `PackagedAnalyzerConsumerTests` still scans packaged output with the postfix-only `RuntimeSequenceArgumentPattern` regex — verified at the generated-file loop and the `[GeneratedRegex]` that requires an identifier then `++` immediately after `(`. Comment-trivia and prefix leftovers would miss that net. Production `AssignLiteralsOrFail` already fails those shapes, and this copy predates the diff.
  - `[medium]` `[defer]` `FindRuntimeSequenceArgument` still flags only `++`/`--` inside argument 0, so `seq`, `n + 1`, or a method call leave `ShouldUseLiteralRenderTreeSequences` green — verified in the syntax-kind predicate. Emitters use postfix `seq++` and `AssignLiterals` already refuses non-increment references; the broader ASP0006-constant gap is pre-existing.
  - `[low]` `[defer]` `RuntimeSequenceArgumentPattern_MatchesSpacedAndTightIncrementArguments` still pins only postfix `seq++` / `seq ++` — verified; grouped with the packaged regex as the same second-gate limitation.
  - `[low]` `[patch]` The new trivia theories never assert that the regex is a non-match on those inputs — verified: they only throw `ShouldAssertException` and check `FindRuntimeSequenceArgument`. Without a regex-negative, the documented hole is not locked in.
  - `[low]` `[patch]` Comment trivia between operand and operator (`seq /* c */ ++`) is uncovered — verified: the new cases place comments between `(` and the increment expression, while `\s*` in the regex still cannot span that interior trivia. Production Roslyn already catches `PostIncrementExpression` there.
  - `[low]` `[reject]` The new helper theories do not assert the interpolated surviving-call-site message — a leftover increment already fails `ShouldBeNull`; extra message matching is unlikely in everyday use and is not needed to pin the gate.
  - `[false]` `[reject]` Two extra `AssignLiteralsOrFail` Facts should have been one theory — both new shapes throw `InvalidOperationException` and name ASP0006; copy-paste layout is not a bad outcome.
  - `[false]` `[reject]` Concurrent non-grid dispose counts `remove` on an empty `add` instead of a real multicast subscription — non-grid `Dispose` only unsubscribes `StateChanged`; the counting `remove` is that teardown effect, now made atomic so overlapping callers cannot under-count.
  - `[false]` `[reject]` There is no SourceTools compiled-fixture concurrent test for grid `DisposeAsync` — `CounterProjectionView_Dispose_UnsubscribesIndicatorHandlerExactlyOnce` already overlaps three `DisposeAsync` calls and asserts one subscription disposal, one JS `disposeViewKey`, and one of each cleanup dispatch.
  - `[low]` `[patch]` The postfix regex pin comment still calls the regex the packaged / `ShouldUseLiteralRenderTreeSequences` gate after the helper became Roslyn-first — verified at the comment on `RuntimeSequenceArgumentPattern_MatchesSpacedAndTightIncrementArguments`.
  - `[low]` `[reject]` Every execution task is marked `[x]` in a diff that does not retouch those other files — the changelog already records they were present at baseline; the only fix is editing this build's spec.

## Design Notes

`eventName` is deliberately excluded from identifier validation because it is emitted only through `GeneratedLiteral.Escape` into an `EventId` string literal; rejecting it would break valid existing inputs without preventing invalid syntax. Use an atomic exchange for disposal so concurrent callers cannot both pass a read/write guard. For truncation, bounds `<= 0` fail soft to `string.Empty`; positive-bound output remains byte-for-byte equivalent.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none` -- expected: all SourceTools tests pass with approved snapshots.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` followed by the built test assembly filtered to `CounterProjectionView_Dispose_UnsubscribesIndicatorHandlerExactlyOnce` -- expected: repeat disposal passes with one subscription disposal.
