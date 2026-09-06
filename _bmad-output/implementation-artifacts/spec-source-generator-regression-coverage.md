---
title: 'Strengthen source-generator regression coverage'
type: 'chore'
created: '2026-09-06'
status: 'done'
baseline_revision: 'e692c5d5ede480120e2dc98b568775d3af973a27'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred:
  - summary: >-
      The direct literal-sequence test gate recognizes only identifier postfix-increment arguments and can miss other runtime expressions.
    evidence: |-
      RenderTreeSequenceRewriterTests.RuntimeSequenceArgumentPattern matches an identifier followed by ++. Bare identifiers, prefix increments, decrements, arithmetic, and method calls can therefore pass this test-only gate. This limitation predates the bundle; production emission still runs AssignLiteralsOrFail, and every current badge/expand output passed the gate and Debug/Release parsing.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs:505
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Generator regression coverage does not independently prove path-truncation equivalence across delta kinds and surrogate cut positions. The masking helper and literal-sequence surfaces also need explicit, direct evidence that sequence literals are normalized without hiding runtime counters.

**Approach:** Extend focused SourceTools tests with an independent legacy-`Substring` oracle, production-derived bounds, multiple delta shapes, a surrogate-boundary matrix, and a mixed masking case; preserve and verify the direct badge and expand-row literal-sequence gates already present at the implementation baseline.

## Boundaries & Constraints

**Always:** Exercise `SchemaMigrationDeltaAnalyzer.Compare` through its public surface; derive the private production path bound without duplicating its numeric value; compare net10 span-concat behavior to an independent `Substring` oracle; cover valid UTF-16 pairs immediately before, across, and after the cut; keep `seq++` byte-for-byte visible while masking decimal literals; parse gated generator output in both Debug and Release configurations.

**Never:** Do not edit `.bmad-loop/**`, `_bmad-output/implementation-artifacts/deferred-work.md`, production generator/schema code, snapshots, public API baselines, package configuration, or generated output. Do not broaden the masking helper beyond decimal sequence forms actually emitted by the production rewriter.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Delta-path oracle | Long ASCII field name emitted as removed, added optional, or added required | Public comparison path equals the independent `Substring` oracle and carries the requested delta kind | No error expected |
| Surrogate before cut | Pair occupies the last two retained UTF-16 units | Pair is retained and output has no unpaired surrogate | No error expected |
| Surrogate across cut | High surrogate is the final candidate unit | Cut steps back, omits the pair, and output has no unpaired surrogate | No error expected |
| Surrogate after cut | Pair begins at the first omitted unit | Pair is omitted without shortening the retained ASCII prefix | No error expected |
| Mixed sequence text | Multiple literal calls surround `AddContent(seq++, ...)` | Every decimal literal becomes `#`; `seq++` and all other text remain unchanged | No error expected |
| Emitted badge/expand views | Mapped badge, Default expand row, and StatusOverview expand row | Direct literal-sequence gate finds no runtime sequence or ASP0006 control and parses Debug/Release | Assertion identifies the violating source site |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs` -- read-only production reference: private `_maxPathLength`; every delta kind flows through `Delta` and `TruncatePath`, whose net10 span concat must match the netstandard `Substring` branch.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaPathTruncationTests.cs` -- replace the duplicated numeric bound, generalize the public-pipeline helper to removed/added field kinds, add the oracle and surrogate-position matrix, and retain the unpaired-surrogate invariant.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeText.cs` -- read-only helper contract: masks decimal first arguments for the nine governed render-tree methods and deliberately leaves runtime expressions visible.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- current baseline directly covers all nine methods and runtime-counter variants; add a multi-call coexistence oracle including sequence zero and `seq++`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` -- read-only baseline evidence: mapped badge output already calls `ShouldUseLiteralRenderTreeSequences` before masking.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs` -- read-only baseline evidence: Default and StatusOverview outputs already call the direct gate; the Default case also pins literal `800`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs` -- reuse `ShouldUseLiteralRenderTreeSequences`, which rejects runtime counters/ASP0006 and parses both consumer configurations.

## Tasks & Acceptance

**Execution:**
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaPathTruncationTests.cs` -- derive the production bound, compare all requested field-delta paths with a suppressed legacy-`Substring` oracle, and cover pair positions at cut minus two, cut minus one, and cut.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- add one exact mixed-source test proving global literal replacement and runtime-counter preservation coexist.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs` -- retain and execute the existing direct literal-sequence gates for mapped badge, Default, and StatusOverview output; make no redundant edit when baseline evidence remains intact.

**Acceptance Criteria:**
- Given supported schema field changes, when `Compare` emits over-boundary delta paths, then every path matches the independent oracle for the production-derived bound and remains valid UTF-16 at each surrogate cut shape.
- Given mixed generated render-tree source, when sequence arguments are masked, then all governed decimal literals are normalized and the intervening runtime `seq++` expression remains observable.
- Given mapped badge and both expandable grid strategies, when their full emitted sources pass the direct sequence gate, then no runtime render-tree sequence or ASP0006 suppression survives and both Debug and Release syntax trees are valid.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 14 findings — high 0, medium 2, low 9, false 3, maybe-false 0
- findings:
  - `[low]` `[patch]` The exact-boundary test did not assert the exact unchanged path — patched with equality against `PathPrefix + name`, so same-length corruption now fails.
  - `[low]` `[patch]` The ASCII over-boundary case retained a fixed 400-character name after adopting a production-derived bound — patched to construct a path exactly one character beyond `_maxPathLength`.
  - `[false]` `[reject]` The delta matrix omits suffix-bearing field changes — the originating Story 11.21 defer explicitly requested an AddedField route in addition to RemovedField, not exhaustive coverage of every path producer; removed, added optional, and added required cover that cited gap.
  - `[low]` `[reject]` Raw regex masking can rewrite invocation-shaped text inside strings or comments — verified for crafted input, but the helper is test-only over controlled emitter output, the shape is unlikely in ordinary use, and syntax-aware masking is disproportionate to the risk.
  - `[low]` `[reject]` Raw regex masking can rewrite a same-named method on an unrelated receiver — verified in isolation, but current controlled emitter inputs use these method names for render-tree builders and a receiver-aware parser would add unjustified complexity.
  - `[medium]` `[defer]` `ShouldUseLiteralRenderTreeSequences` recognizes only identifier postfix increments and can miss other runtime first-argument expressions — verified as a pre-existing test-gate limitation; production `AssignLiteralsOrFail` remains the load-bearing fail-closed boundary.
  - `[low]` `[reject]` The recorded focused command omits `RenderTreeSequenceRewriterTests` positive detector controls — the fix would only edit this build's spec, and the matrix audit directly executed all changed and relied-upon output cases.
  - `[low]` `[reject]` The recorded command uses deprecated `-parallel none` — the exact command passed 44 tests and the only correction would edit this build's spec.
  - `[low]` `[patch]` Edge-case review independently found the fixed 400-character over-boundary fixture — patched by the same `_maxPathLength`-relative construction.
  - `[low]` `[reject]` Edge-case review combined string/comment and unrelated-receiver regex masking — verified as the same low-probability test-helper limitation whose syntax-aware fix is not worth the added complexity.
  - `[medium]` `[defer]` Edge-case review independently found that the direct gate can miss non-postfix runtime expressions — carried by the same pre-existing deferred gate limitation.
  - `[low]` `[reject]` Verification-gap review confirmed the deprecated xUnit switch — the command passed with only a deprecation warning, and its correction would edit this build's spec.
  - `[false]` `[reject]` A broad intent reading requires all type, enum, validation, and metadata path families — the verbatim ledger and its Story 11.21 source identify AddedField as the missing companion route, so the implemented three field-delta kinds satisfy the bounded request.
  - `[false]` `[reject]` A patch-local reading requires new DW-698 hunks — repository-state completion is a defensible reading, and baseline inspection plus passing tests prove the named badge and both expand-row outputs already call the direct gate.

## Design Notes

Reflection over the private constant keeps this a test-only change and prevents the test from silently drifting if the production bound changes. The oracle intentionally retains `Substring(0, cut) + "..."` with narrow analyzer suppressions because equivalence to that legacy implementation is the behavior under test, not production guidance.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.SchemaMigrationDeltaPathTruncationTests -class Hexalith.FrontComposer.SourceTools.Tests.GeneratedRenderTreeTextTests -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.RazorEmitterBadgeColumnTests -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.RazorEmitterExpandInRowTests` -- expected: all focused regression classes pass.

## Auto Run Result

Status: done

### Summary

Added public-pipeline regression coverage for production-derived schema path bounds, legacy `Substring` equivalence, removed/optional-added/required-added delta kinds, and valid surrogate pairs immediately before, across, and after the cut. Added a mixed render-tree masking oracle that preserves `seq++`, and verified the baseline's direct badge and expand-row literal-sequence gates without redundant edits.

### Files Changed

- `spec-source-generator-regression-coverage.md` -- records the implementation contract, review triage, verification, and residual risk.
- `../../../tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaPathTruncationTests.cs` -- derives the production bound and adds delta-kind, oracle, and surrogate-shape coverage.
- `../../../tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeTextTests.cs` -- adds mixed literal-masking and runtime-counter preservation coverage.

### Review Findings

- Patches applied: 2 low entries — exact-boundary equality and `_maxPathLength`-relative over-boundary input. No high or medium patches.
- Deferred: 1 pre-existing medium entry — the direct literal-sequence test gate recognizes postfix identifier increments but not every possible runtime expression; production `AssignLiteralsOrFail` remains the fail-closed boundary.
- Rejected: exhaustive suffix-bearing delta-family expansion (2 reports) because the originating defer specifically requested AddedField coverage; raw-regex string/comment masking, unrelated-receiver masking, and their combined duplicate because they are low-probability test-helper cases whose syntax-aware fix is disproportionate; omission of rewriter positive controls from the recorded command because its only fix edits this spec and separate controls passed; deprecated xUnit switch (2 reports) because the exact command passes and only this spec would change; and the patch-local DW-698 reading because existing direct gates were verified at the repository-state surface.

### Follow-up Review Recommendation

`false` — patched entries by verdict: high 0, medium 0, low 2. No patched high-severity risk or two-medium-patch threshold exists.

### Verification Performed

- Release SourceTools test-project build: succeeded with 0 warnings and 0 errors.
- Required four-class lane: 44 passed, 0 failed, 0 skipped.
- Explicit matrix-method lane: 16 passed, 0 failed, 0 skipped; every matrix row executed.
- `RenderTreeSequenceRewriterTests` positive-control class: 21 passed, 0 failed, 0 skipped.
- `git diff --check`: passed.

### Residual Risks

The test-only direct sequence gate's pre-existing regex does not recognize every theoretically possible nonliteral first-argument expression. Current controlled badge/expand emission passes, and the production rewriter still enforces `AssignLiteralsOrFail` before output is returned.
