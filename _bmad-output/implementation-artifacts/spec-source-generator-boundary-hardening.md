---
title: 'Harden source-generator boundaries'
type: 'bugfix'
created: '2026-09-06'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
baseline_revision: '106f0392aef69b8fa85622a59e5479e10320f7bb'
baseline_commit: '106f0392aef69b8fa85622a59e5479e10320f7bb'
context:
  - _bmad-output/planning-artifacts/architecture.md
warnings: []
deferred:
  - summary: >-
      AssignLiteralsOrFail can miss runtime sequence increments that exist only in a disabled conditional branch.
    evidence: |-
      AssignLiterals deliberately returns conditional documents unchanged, but FindRuntimeSequenceArgument parses only with DEBUG defined; a counter solely in an inactive #else or non-DEBUG branch remains DisabledTextTrivia. This predates the comment-trivia correction.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:182
    severity: medium
  - summary: >-
      The fail-closed render-tree check accepts nonliteral sequence expressions that contain no increment or decrement.
    evidence: |-
      The text gate and syntax walk detect ++/-- nodes only, so seq, seq + 1, or GetSequence() can pass. The deferred bundle was limited to comment trivia around increment expressions, and this broader gap predates it.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:182
    severity: medium
  - summary: >-
      The governed render-tree method catalog omits AddComponentParameter.
    evidence: |-
      The pinned framework exposes RenderTreeBuilder.AddComponentParameter(int sequence, string name, object? value), while SequenceMethodNames and the masking helper omit it. No SourceTools emitter currently calls it, so this is a pre-existing future-coverage gap.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:52
    severity: low
  - summary: >-
      GeneratedRenderTreeText can mask the numeric prefix of a runtime arithmetic expression.
    evidence: |-
      SequenceArgumentPattern has no delimiter after its digit run, so AddContent(12 + seq, ...) becomes AddContent(# + seq, ...). This pre-existing test-helper behavior is outside the four ledger boundaries.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeText.cs:25
    severity: medium
  - summary: >-
      Literal-sequence test helpers use a weaker postfix-increment regex than the production Roslyn check.
    evidence: |-
      ShouldUseLiteralRenderTreeSequences and the packaged-consumer gate do not recognize prefix increments, decrements, arithmetic, method calls, or comment-separated expressions. Production AssignLiteralsOrFail still catches the increment/decrement forms addressed here; the duplicate test regex predates this bundle.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs:505
    severity: low
  - summary: >-
      The broader increment/decrement prefilter may add a second Roslyn parse for generated documents with ordinary loop increments.
    evidence: |-
      Rewritten output can retain unrelated i++ loops, which now pass the cheap text gate even without a surviving sequence counter. A representative generator/IDE benchmark is needed to establish whether the extra parse has material latency.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:182
    severity: 'medium (unverified)'
  - summary: >-
      Named render-tree sequence arguments can evade the positional first-argument scan.
    evidence: |-
      C# permits named arguments to be reordered, while FindRuntimeSequenceArgument inspects syntax argument zero rather than the parameter named sequence. Current emitters use positional calls, making this a pre-existing low-risk gap.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:199
    severity: low
  - summary: >-
      GeneratedLogMethodEmitter does not validate the emitted LogLevel member name.
    evidence: |-
      level is appended directly as LogLevel.<value>, so a malformed internal caller value would emit invalid semantic C# after other validation. All current call sites use controlled literals; this was not part of DW-688.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs:111
    severity: low
  - summary: >-
      Generated logging parameter names and type strings remain unvalidated before output mutation.
    evidence: |-
      Keywords, duplicate names, logger/exception collisions, or malformed type strings can still produce uncompilable generated members. Current call sites are internal controlled literals, and this broader pre-existing input contract was not part of DW-688.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs:126
    severity: low
  - summary: >-
      Concurrent metadata hardening does not cover every compiler-unreferenceable property type.
    evidence: |-
      The separately landed metadata fixture covers invalid identifiers/namespaces and non-SZ arrays, but not inaccessible referenced types or unresolved/error types. That work is outside this bundle and was already committed independently during the run.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/MetadataAssignmentFixture.cs:13
    severity: medium
  - summary: >-
      Concurrent SourceTypeNameFormatter changes retain top-level array nullability despite the documented non-nullable assignment-type contract.
    evidence: |-
      Format passes includeNullableAnnotation:false, but the array branch ignores the flag and records the outer array annotation. DomainModel documents SourceTypeName as non-nullable; the formatter work was committed independently during this run.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Parsing/SourceTypeNameFormatter.cs:35
    severity: medium
  - summary: >-
      Concurrent command-renderer changes suppress obsolete warnings across the entire generated file.
    evidence: |-
      The new CS0612/CS0618 pragma begins after #nullable and restores at end of file, so unrelated obsolete API use in generated code can be hidden. This separately committed parser/renderer work is outside the four-entry bundle.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:30
    severity: medium
  - summary: >-
      Generated OnStateChanged handlers have no disposed guard, so a state notification racing
      teardown can still reach InvokeAsync(StateHasChanged).
    evidence: |-
      Both the grid and non-grid OnStateChanged bodies call InvokeAsync unconditionally, and Dispose
      unsubscribes only after claiming the guard. The window predates this bundle: the unsubscribe
      order is unchanged and the discarded Task makes any ObjectDisposedException unobserved. Only
      OnNewItemIndicatorsChanged carries a Volatile.Read guard.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1060
    severity: low
  - summary: >-
      The fail-closed render-tree gate's decrement arm is exercised by no test.
    evidence: |-
      No SourceTools test feeds a decrement sequence argument through AssignLiteralsOrFail, so the
      '--' half of both the text gate and the Post/PreDecrementExpression walk is unpinned. This is
      pre-existing: the retired HasIncrementInFirstArgumentPosition prefilter also scanned '--' and
      was equally uncovered. One theory row on the existing AssignLiteralsOrFail tests would close it.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:188
    severity: low
  - summary: >-
      Concurrent soft-fail arm assertions hardcode a bare LF and fail on a CRLF test host.
    evidence: |-
      CommandRendererStaticAssignmentTests compares SliceCase(...).Trim() against a literal
      'case "X":\n                return false;', while CommandRendererEmitter builds that arm with
      StringBuilder.AppendLine (Environment.NewLine) and SliceCase does no normalization. Green on
      Linux, red on Windows. The assertions belong to the independently committed prefill refactor.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CommandRendererStaticAssignmentTests.cs:178
    severity: medium
  - summary: >-
      Concurrent top-level nullable-annotation suppression in SourceTypeNameFormatter is pinned by no test.
    evidence: |-
      Format now passes includeNullableAnnotation:false, but every fixture asserting an annotated
      SourceTypeName is an array or inner-generic shape that routes around the flag, and no fixture
      declares a top-level annotated reference type. Restoring the previous default leaves the whole
      suite green. A 'public string? Note' property asserting SourceTypeName == "global::System.String"
      would settle it. Committed independently during this run.
    location: >-
      src/Hexalith.FrontComposer.SourceTools/Parsing/SourceTypeNameFormatter.cs:35
    severity: medium
  - summary: >-
      The concurrent HFC1016 documentation shows one of three emitted message variants.
    evidence: |-
      CommandParser emits "has no public setter", "is declared with an 'init' accessor", and "has a
      non-public setter", but the corrected example documents only the first, and the page does not
      describe where the diagnostic points for a member that comes from metadata rather than source.
      The doc edit belongs to the independently committed prefill refactor.
    location: >-
      docs/diagnostics/HFC1016.md
    severity: low
---

<intent-contract>

## Intent

**Problem:** Generated-code boundaries still allow comment trivia to hide runtime render-tree counters, invalid wrapper identifiers to reach emitted C#, non-positive truncation bounds to throw, and repeated disposal to repeat teardown.

**Approach:** Make Roslyn inspection authoritative for surviving sequence arguments, validate emitted identifiers before buffer mutation, make truncation fail soft at non-positive bounds, and gate generated disposal atomically; add focused regressions for each corrected boundary.

## Boundaries & Constraints

**Always:** Keep `SourceTools` netstandard2.0-compatible; preserve valid generated render behavior, literal sequence numbering, first-disposal teardown order, escaped event-name text, generated hint/artifact identities, and Fluent UI v5 output. Validate `methodName` before appending any output. Preserve the repository's existing CRLF/LF rules, including LF approval snapshots.

**Never:** Do not edit `.bmad-loop/**`, `_bmad-output/implementation-artifacts/deferred-work.md`, public API baselines, package/dependency configuration, unrelated format-hint paths, or hand-authored generated output. Do not treat `eventName` as a C# identifier: it is escaped `EventId` string data and valid punctuation must remain accepted.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Rewrite residue | First sequence argument contains prefix/postfix increment after block or line-comment trivia | `AssignLiteralsOrFail` detects the surviving governed call | Throw `InvalidOperationException` naming the call and ASP0006 |
| Invalid emitted identifier | Nonblank invalid, reserved, or escaped `methodName` | No generated text is appended | Throw `ArgumentException` for `methodName` |
| Event label | `eventName` contains spaces, punctuation, quotes, or backslashes | Escaped string literal remains valid | No error expected |
| Sequence mask | Supported call begins with a decimal literal versus tight/spaced runtime increment | Literal becomes `#`; runtime expression remains unchanged | No error expected |
| Truncation bound | Non-empty value with bound `<= 0`, `1`, or a positive interior bound | Empty, ellipsis-only, or bounded prefix-plus-ellipsis respectively | No incidental span exception |
| Repeated disposal | Generated grid or non-grid view is disposed repeatedly or concurrently | Cleanup runs once; later callers return | No repeated dispatch, JS cleanup, unsubscribe, or child disposal |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` -- `FindRuntimeSequenceArgument` performs the definitive Roslyn walk, but `HasIncrementInFirstArgumentPosition` / `StartsArgumentList` can suppress that walk when trivia separates `(` from an increment expression.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` -- `ValidateArguments` runs before field-name construction and buffer mutation; `methodName` forms field/method syntax, while `eventName` flows only through `GeneratedLiteral.Escape` into a string literal.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` -- grid output already emits `_disposed` for notification checks, but grid `DisposeAsync` only writes it and non-grid `Dispose` has no guard; `EmitFormatters` emits the unsafe `Truncate` expression.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` and `GeneratedLogMethodEmitterTests.cs` -- direct fail-closed and pre-mutation validation seams.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeText.cs` and new `GeneratedRenderTreeTextTests.cs` -- masking helper and direct coverage for all supported calls plus runtime-counter negative controls.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`, `RazorEmitterExpandInRowTests.cs`, and `RazorEmitterVirtualizationTests.cs` -- badge/expand literal enforcement and emitted truncation/disposal contracts.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- live grid subscription instrumentation exposes repeated teardown.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/**/*.verified.txt` -- approval outputs affected by the shared Razor helper and disposal guard; update only snapshots changed by these emitter corrections.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` -- remove trivia-driven prefilter false negatives and cover block/line comments without changing clean literal rewriting.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLogMethodEmitterTests.cs` -- reject invalid wrapper identifiers before append while retaining arbitrary escaped event labels.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- directly cover every supported literal-call mask plus tight/spaced runtime-counter negative controls.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs` -- apply the full literal-sequence assertion to mapped-badge and Default/StatusOverview expand-row output while retaining the literal `800` pin.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`, and affected approval snapshots -- emit and pin fail-soft non-positive truncation and atomic once-only guards for grid/non-grid disposal.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- dispose the live generated grid repeatedly and assert subscription cleanup occurs exactly once.

**Acceptance Criteria:**
- Given valid generator models, when the hardened emitters run in Debug and Release, then generated output remains syntactically valid and observable rendering behavior is unchanged.
- Given mapped badge and both expand-row grid strategies, when their full emitted sources are inspected, then every governed render-tree sequence is a literal and no ASP0006 control is emitted.
- Given grid and non-grid generated views, when disposal is entered repeatedly or concurrently, then only the first entry performs teardown and finalization suppression remains on that path.

## Spec Change Log

- 2026-09-06: Implemented all boundary-hardening tasks and updated the affected Razor approval snapshots.

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 32 findings — high 0, medium 14, low 9, false 8, maybe-false 1
- findings:
  - `[medium]` `[defer]` Runtime sequence increments solely in disabled conditional branches remain invisible to the DEBUG-only Roslyn walk — verified against conditional fail-safe tests and grouped into the disabled-branch deferred item.
  - `[medium]` `[defer]` Nonliteral sequence expressions without `++`/`--` still pass the fail-closed check — verified in the text gate and syntax-node predicate; this broader pre-existing boundary is deferred.
  - `[low]` `[defer]` `AddComponentParameter` is absent from the governed method catalogs — local API inspection confirmed the sequence-bearing framework method, while repository search found no SourceTools emitter use; deferred as future coverage.
  - `[medium]` `[defer]` The masking regex can replace only the numeric prefix of `12 + seq` — verified from the missing post-digit delimiter; deferred because the helper behavior predates and lies outside the four ledger entries.
  - `[low]` `[defer]` `ShouldUseLiteralRenderTreeSequences` and the packaged gate use a narrower regex than production — verified, but production Roslyn checks retain the story-owned increment/decrement protection; deferred.
  - `[maybe-false]` `[defer]` The broader operator-presence gate may impose material generator latency through extra parses — ordinary loop increments can trigger the parse, but representative IDE/generator benchmarking is required to establish impact; recorded medium if verified.
  - `[false]` `[reject]` A concurrent second disposer returning before the first completes is not a contract violation — the captured matrix explicitly says later callers return and requires exactly-once cleanup, not task joining.
  - `[low]` `[reject]` A teardown exception can leave later cleanup skipped after the guard is set — this is a generic failure-path possibility, but expected dispatch/JS teardown exceptions are already caught, retry-after-failed-dispose was not promised, and restructuring cleanup would add disproportionate complexity.
  - `[medium]` `[patch]` The original grid test did not deterministically overlap disposal calls — patched by blocking the first JS cleanup invocation before starting two later disposals and asserting their immediate completion.
  - `[medium]` `[patch]` The original grid test observed only indicator subscription disposal — patched to assert one pending-page clear dispatch, one expanded-row collapse dispatch, one `disposeViewKey` call, and one subscription disposal.
  - `[medium]` `[patch]` Non-grid disposal had source-shape coverage only — patched with a runtime-compiled emitted `Dispose` test whose counting event source proves exactly one unsubscribe across two calls.
  - `[low]` `[defer]` The generated `LogLevel` member remains unchecked — verified but limited to controlled internal callers and outside DW-688; deferred.
  - `[low]` `[defer]` Generated logging parameter names and type strings remain unchecked — verified as a broader controlled-caller contract predating DW-688; deferred.
  - `[medium]` `[defer]` The concurrent metadata fixture omits some compiler-unreferenceable type cases — verified in the independently committed parser work and deferred outside this bundle.
  - `[medium]` `[defer]` The concurrent formatter preserves top-level array nullability despite the non-nullable assignment-type contract — verified in `FormatCore`/`FormatArrayType` and deferred outside this bundle.
  - `[medium]` `[defer]` The concurrent command-renderer pragma suppresses obsolete warnings file-wide — verified in independently committed renderer work and deferred outside this bundle.
  - `[false]` `[reject]` The bundle spec omits parser/metadata/HFC1016 changes — those files were changed and committed concurrently by a separate workflow after this run's clean baseline, so absorbing them would misstate this bundle.
  - `[low]` `[reject]` The spec lists no Debug command — an explicit Debug SourceTools build ran successfully at 0 warnings/errors during review, and the workflow rejects fixes whose only action is editing this build's spec.
  - `[medium]` `[defer]` Disabled conditional branches evade the active-tree runtime-sequence scan — verified duplicate of the first blind finding and carried by the same deferred item.
  - `[medium]` `[defer]` Runtime sequence expressions without increment/decrement evade the scan — verified duplicate of the second blind finding and carried by the same deferred item.
  - `[low]` `[defer]` A reordered named `sequence` argument is not necessarily syntax argument zero — verified C# shape, but current emitters are positional; deferred as a pre-existing gap.
  - `[false]` `[reject]` A same-named method on an unrelated receiver would cause an erroneous generator failure — generated sources are emitter-controlled, no such call exists, and failing loudly would remain safer than shipping unchecked output.
  - `[low]` `[reject]` Setting `_disposed` before an unexpected teardown exception prevents a later retry — same low-probability retry-after-failed-dispose concern already rejected because expected failure modes are caught and the proposed reset conflicts with exactly-once effects.
  - `[false]` `[reject]` Roslyn is not authoritative because non-increment expressions remain unchecked — the original ledger boundary is specifically trivia-hidden increment residue; Roslyn is authoritative for that corrected boundary.
  - `[low]` `[reject]` First-disposal completion is not guaranteed after an unexpected teardown exception — same rejected failure-path concern; no uncaught throwing production path was demonstrated and successful-path completion is fully verified.
  - `[medium]` `[patch]` Grid once-only teardown verification would pass the former `Volatile.Write` behavior — patched with deterministic overlap plus exact dispatch, JS, and subscription counts.
  - `[medium]` `[patch]` Non-grid once-only disposal lacked behavioral verification — patched by executing the emitted method twice against a counting remove accessor.
  - `[false]` `[reject]` DW-687 diverges from the diff — the auditor found the Roslyn-backed block/line-comment tests aligned with the production boundary; additional assertions do not change that outcome.
  - `[false]` `[reject]` DW-688 should validate `eventName` as an identifier — `eventName` is escaped string data, while `methodName` alone forms member syntax; the semantic reading prevents invalid C# without rejecting valid event labels.
  - `[false]` `[reject]` DW-700 requires a full generated-view runtime path — the test compiles and invokes the exact emitted helper body across every specified bound, which is the observable boundary in question.
  - `[medium]` `[patch]` DW-701 runtime coverage originally reached only one grid cleanup effect and no non-grid execution — both gaps were patched with deterministic grid side-effect counts and an executing non-grid unsubscribe test.
  - `[false]` `[reject]` Unrelated parser/metadata files show intent drift — they belong to independently committed concurrent work; no deferred-work ledger file was edited and the bundle-owned diff remains aligned.

### 2026-09-06 — Follow-up review pass
- verdicts: 33 findings — high 0, medium 7, low 18, false 8, maybe-false 0
- findings:
  - `[false]` `[reject]` `FormatArrayType` reverses rank against nullability and mis-renders mixed-rank jagged arrays — refuted: ranks are printed outer-first (the leftmost group is the outermost array) while a `?` printed after group *j* annotates layer `Count-1-j`, because the outermost array's suffix comes last. Both mappings are independent and correct; `string[]?[,]` and `string?[,][]?` each round-trip, the latter matching the existing `AttributeParserTests` assertion.
  - `[medium]` `[defer]` `FormatCore`'s array arm drops `includeNullableAnnotation` — carried: same location and claim as the logged concurrent-formatter row, and the code still reads as that row describes.
  - `[false]` `[reject]` The change edits `deferred-work.md`, which the Never list forbids — carried and re-refuted against the file list: commit `6dcdc9ce` touches no ledger file. The ledger appears in the diff only as the orchestrator's uncommitted sweep, which this run must not modify, revert, or commit.
  - `[false]` `[reject]` The reviewed change set spans two workstreams with no declaration — carried: `git log 106f0392..HEAD --name-only` attributes every formatter, parser, HFC1016 and `CommandRendererEmitterTests` file to `0173ef69`, a separately committed prefill refactor, not to this bundle.
  - `[low]` `[reject]` Spec frontmatter disagrees with its body — the `in-review` status was this pass's own transient state, and the workflow rejects findings whose only fix is editing this build's spec.
  - `[low]` `[reject]` The Verification section covers none of the newly touched surfaces — those surfaces belong to the concurrent commit, and the fix would edit this build's spec.
  - `[low]` `[reject]` New ledger entries are not uniform (missing `severity`, no `source_spec` back-pointer) — the fix edits `deferred-work.md`, which the intent's Never list excludes and the orchestrator owns.
  - `[low]` `[patch]` The replacement text gate carries no rationale, unlike the two XML-documented helpers it retired — patched with a remark on `FindRuntimeSequenceArgument` recording why the gate is deliberately broad and why it tests both operators while `AssignLiterals` tests only `++`, so the visible asymmetry is not "simplified" away.
  - `[low]` `[patch]` `GeneratedRenderTreeTextTests` negative controls cover only postfix `seq++`/`seq ++` — patched with prefix-increment, spaced-decrement, and comment-separated rows so a masking regression that swallowed a runtime counter would fail.
  - `[low]` `[reject]` `Emit_ProjectionTeardownUsesAtomicOnceOnlyGuards…` would pass a leftover `Volatile.Write` or a duplicated `_disposed` field — true of that test in isolation, but the 16 regenerated approval snapshots pin the full emitted text and a duplicate field is CS0102, so neither shape can ship; extra assertions would duplicate the approvals.
  - `[medium]` `[defer]` Concurrent soft-fail arm assertions hardcode `\n` while the emitter uses `AppendLine` — verified: `SliceCase(...).Trim()` does not normalize interior newlines, so the assertion is Linux-only. It belongs to `0173ef69`, not this bundle; deferred.
  - `[low]` `[reject]` Compiled fixtures accumulate in the non-collectible default `AssemblyLoadContext` — real but negligible at two assemblies per run, and a collectible-context rewrite is more complexity than the harm justifies.
  - `[low]` `[reject]` The grid disposal test outgrew its name and leaks an `ActionDispatched` handler — the method name is unchanged, so the spec's verification filter still resolves, and the handler is attached to a test-local dispatcher that does not outlive the test.
  - `[low]` `[patch]` The `@`-prefix check is redundant and the emitter's documented contract omits the new identifier rule — the redundancy is refuted as harmful (`SyntaxFacts.IsValidIdentifier` already rejects `@`, and the escaped-name theory still proves the boundary, so it stays as belt-and-braces per Design Notes); the documentation half is patched — `Emit`'s `<exception cref="ArgumentException">` now states the unescaped non-keyword identifier rule and that `eventName` is escaped string data.
  - `[low]` `[defer]` The concurrent HFC1016 page documents one of three emitted message variants — verified against `CommandParser`; the doc edit belongs to `0173ef69` and is deferred.
  - `[false]` `[reject]` `FormatArrayType` mis-renders mixed-rank jagged arrays — duplicate of the first finding and refuted on the same round-trip derivation.
  - `[medium]` `[defer]` The array branch keeps a top-level `?` despite the non-nullable assignment-type contract — carried.
  - `[low]` `[reject]` Teardown throwing after `Interlocked.Exchange` permanently skips the remaining cleanup — carried: rejected twice in the prior pass because expected dispatch and JS failures are already caught, retry-after-failed-dispose was never promised, and a reset conflicts with exactly-once effects.
  - `[false]` `[reject]` `Truncate(null, n)` throws a `NullReferenceException` inside generated code — refuted: the only emitted call sites are `Truncate(HumanizeEnumLabel(...), 30)` in `ColumnEmitter`, whose argument is the non-nullable `string` returned by the emitted `HumanizeEnumLabel`. No caller can pass null, and the parameter is declared non-nullable.
  - `[low]` `[defer]` Non-grid `OnStateChanged` has no disposed guard — verified, and the grid handler has none either; but the unsubscribe ordering is unchanged by this bundle, so the post-dispose notification window is pre-existing. Deferred.
  - `[low]` `[defer]` The emitted `LogLevel` member name is unvalidated — carried; all call sites remain controlled literals.
  - `[false]` `[reject]` The diff edits `deferred-work.md` despite the Never list — duplicate; refuted on the same commit-attribution evidence.
  - `[medium]` `[defer]` Top-level nullable-annotation suppression in `SourceTypeNameFormatter.Format` is pinned by no test — accepted as filed by the verification-gap layer; routed to defer rather than patch because the formatter change is `0173ef69`'s, not this bundle's.
  - `[low]` `[defer]` The fail-closed gate's decrement arm is exercised by no test — accepted as filed; verified pre-existing, since the retired `HasIncrementInFirstArgumentPosition` prefilter also scanned `--` with no coverage. Deferred with the one-line closure noted.
  - `[medium]` `[defer]` `Format` and its array arm read as contradictory — carried.
  - `[low]` `[reject]` `CompileFixture` uses the default `AssemblyLoadContext` — duplicate; rejected on the same negligible-harm reasoning.
  - `[medium]` `[defer]` Roslyn is authoritative only downstream of a text gate, so non-increment runtime arguments never reach the walk — carried: recorded in the prior pass as the broader nonliteral-expression and disabled-branch gaps.
  - `[low]` `[defer]` The badge and expand-row assertions call the weaker `ShouldUseLiteralRenderTreeSequences` regex — carried: production `AssignLiteralsOrFail` retains the story-owned protection.
  - `[medium]` `[defer]` The Sequence-mask row has no production surface and the `12 + seq` prefix wart is uncovered — carried; this pass's added negative controls narrow but do not close it.
  - `[false]` `[reject]` Truncation is proved on lifted text rather than a view or caller — carried: the test compiles and invokes the byte-identical emitted method body across every matrix bound, and the caller's format-hint origin is on the Never list.
  - `[low]` `[reject]` Non-grid once-only disposal is proved on a re-hosted fixture, not a generated component — carried: the prior pass patched this to execute the exact emitted `Dispose` body twice against a counting remove accessor, which is the observable boundary.
  - `[false]` `[reject]` The diff carries a second workstream and a Never-list ledger edit — carried; refuted on commit attribution.
  - `[low]` `[patch]` `Emit`'s `<exception cref="ArgumentException">` documents only the null/empty/whitespace and template rules, trailing the enforced contract — grouped with the identifier finding above and patched in the same edit.

## Design Notes

The render-tree text check should use only a cheap presence test for increment/decrement operators before the Roslyn walk; reconstructing trivia in string scanning duplicates the parser and invites another bypass. Identifier validation must reject keywords and `@`-escaped names because the emitter derives a backing field from the first character. Use `Interlocked.Exchange` for disposal so concurrent callers cannot both enter cleanup. For truncation, bounds `<= 0` return `string.Empty`; positive-bound output stays byte-for-byte equivalent.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none` -- expected: all SourceTools tests pass with approved snapshots.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` followed by the built test assembly filtered to `CounterProjectionView_Dispose_UnsubscribesIndicatorHandlerExactlyOnce` -- expected: repeat disposal passes with one subscription disposal.


## Auto Run Result

Status: done

### Summary

Follow-up review pass over the boundary-hardening bundle. All four review layers ran and 33 findings were triaged. No high or medium defect attributable to this bundle survived verification. Three low findings were patched: the replacement render-tree text gate now carries the rationale its retired helpers documented, the masking helper's runtime-counter negative controls cover prefix, decrement and comment-separated forms, and the generated-logging emitter's documented exception contract states the identifier rule it already enforces. No production behavior changed in this pass.

### Files Changed

- `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs` -- comment only: records why the cheap gate over-matches and why it keeps a decrement arm that `AssignLiterals` does not need.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs` -- documentation only: `<exception>` now covers the unescaped non-keyword identifier rule and the escaped-string status of `eventName`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- three additional runtime-counter negative controls (`++seq`, `-- seq`, comment-separated `++seq`).
- `_bmad-output/implementation-artifacts/spec-source-generator-boundary-hardening.md` -- follow-up triage log, five new deferred entries, and this result.

### Review Findings

- Patches applied: 3 entries, all `low` (high 0, medium 0, low 3) -- gate rationale comment, widened mask negative controls, corrected emitter exception documentation.
- Deferred: 5 new entries (17 total). Two are this repository's pre-existing gaps -- the missing disposed guard on generated `OnStateChanged` handlers and the untested decrement arm of the fail-closed gate. Three belong to the independently committed prefill refactor `0173ef69` -- LF-hardcoded soft-fail arm assertions that fail on a CRLF host, the unpinned top-level nullable-annotation suppression in `SourceTypeNameFormatter`, and the partial HFC1016 message documentation.
- Rejected: the two jagged-array rank-reversal claims, refuted by deriving the round-trip (ranks outer-first, annotations reversed) against the existing `string?[,][]?` assertion; the `Truncate(null, n)` claim, refuted because the only emitted call sites pass a non-nullable `HumanizeEnumLabel` result with a literal bound; three ledger and Never-list claims, refuted because commit `6dcdc9ce` touches no ledger file and the ledger diff is the orchestrator's uncommitted sweep; the two-workstream claim on the same commit attribution; three findings whose only fix would edit this build's spec or the orchestrator-owned ledger; the guard-test weakness, because the 16 approval snapshots already pin the emitted text; the `AssemblyLoadContext` accumulation and the test-local handler, both negligible; the `@`-prefix redundancy, which produces no bad outcome and is retained per Design Notes; and four carried rejections from the first pass (teardown-exception retry, lifted-text truncation, re-hosted non-grid fixture, and the scope claims).
- Follow-up review recommendation: `false`. This is a follow-up pass and it patched no `high` entry (high 0, medium 0, low 3), so the work has converged.

### Verification Performed

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/... --configuration Release`: 0 warnings, 0 errors.
- `DiffEngine_Disabled=true .../Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none`: 1,256 passed, 0 failed, 0 skipped (up from 1,253 by the three added theory rows).
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/... --configuration Release`: 0 warnings, 0 errors.
- `CounterProjectionView_Dispose_UnsubscribesIndicatorHandlerExactlyOnce`: 1 passed, 0 failed, 0 skipped.

### Residual Risks

Seventeen deferred items remain outside this bundle. The two graded medium in this pass -- the CRLF-host assertion hazard and the unpinned nullable-annotation contract -- belong to the concurrently committed prefill refactor and should be routed to that work rather than re-opened here.

### Documented Unrelated Changes

`_bmad-output/implementation-artifacts/deferred-work.md` is modified in the working tree by the orchestrator's sweep. It is orchestrator-owned and on this spec's Never list, so this run neither edited, reverted, nor committed it; the working copy therefore stays dirty on that one file after finalization.
