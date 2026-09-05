---
title: 'DW-683 harden CommandFormEmitter admission-disposal token anchoring'
type: 'bugfix'
created: '2026-09-05'
status: 'done'
baseline_revision: '1a7edded603cd557a97dda1277e5ae3101fbec4d'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** `Emit_CommandExecutionAdmissionReleasesInFinally` locates `try` and `finally` with substring searches after the submitted-log call. An intervening identifier or other larger token containing either substring can satisfy the ordering assertions against the wrong text and conceal a generated disposal-order regression.

**Approach:** Make the test oracle resolve exact C# keyword tokens and add a focused collision case containing misleading `try`/`finally` substrings. Continue to prove that the admission lease is disposed only after the actual `finally` keyword, without changing emitted product behavior.

## Boundaries & Constraints

**Always:** Preserve the submitted-log call-site anchor, parse the inspected source as C#, distinguish exact `TryKeyword` and `FinallyKeyword` tokens from larger identifiers, and assert the full submitted-log → `try` → `finally` → `admission.Dispose()` order. Keep the regression deterministic and within the existing xUnit v3/Shouldly test suite.

**Never:** Edit `CommandFormEmitter`, generated snapshots, analyzer policy, the deferred-work ledger, or unrelated `IndexOf` assertions. Do not weaken a missing-anchor failure or rely on whitespace/formatting to identify keywords.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Generated admission cleanup | Current emitted command-form source | Exact `try` and `finally` keyword tokens occur after the submitted-log call, and `admission.Dispose()` follows the exact `finally` token | Missing or misordered syntax fails the assertion |
| Misleading larger tokens | Valid inspected source includes identifiers containing `try` and `finally` before the corresponding keywords | Identifiers are ignored; the oracle selects only the keyword tokens and retains the correct disposal-order proof | A substring-based implementation fails the regression |

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` -- `Emit_CommandExecutionAdmissionReleasesInFinally` is the sole owned test; its exact submitted-log/dispose anchors are sound, while its `IndexOf("try")` and `IndexOf("finally")` anchors are the defect. The file already references Roslyn C# APIs and parses emitted source elsewhere.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` -- read-only evidence: emission around `LogCommandSubmitted(Logger, correlationId)` writes the lifecycle locals, an actual `try` statement, a later `finally`, and `admission.Dispose()`; no product correction is required.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj` -- focused net10.0 xUnit v3 test project with Shouldly and `Microsoft.CodeAnalysis.CSharp`; no dependency change is needed.
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md` -- read-only continuity evidence records why the submitted-log call site became the stable predecessor anchor and identifies DW-683 as deferred pre-existing test-oracle debt.
- `.bmad-loop/runs/20260905-075920-30ce/bundles/dw-683-deferred-from-code-review-of-spec/intent.md` -- read-only bundle authority. The deferred-work ledger is explicitly outside the writable surface.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` -- harden `Emit_CommandExecutionAdmissionReleasesInFinally` to use exact parsed keyword tokens and exercise larger-token substring collisions while retaining the existing call-site and disposal ordering checks.

**Acceptance Criteria:**
- Given emitted command-form source and a larger identifier containing `try` after the submitted-log call, when admission cleanup ordering is inspected, then the assertion anchors on the subsequent exact C# `try` keyword.
- Given a larger identifier containing `finally` before the cleanup clause, when admission cleanup ordering is inspected, then the assertion anchors on the exact C# `finally` keyword and proves `admission.Dispose()` occurs afterward.
- Given the completed change, when the focused `CommandFormEmitterTests` lane runs, then all tests pass without production, snapshot, dependency, analyzer-policy, or deferred-work-ledger changes.

## Spec Change Log

## Review Triage Log

### 2026-09-05 — Review pass
- verdicts: 14 findings — high 0, medium 6, low 0, false 8, maybe-false 0
- findings:
  - `[medium]` `[patch]` The first post-log `finally` token belonged to an inner cleanup rather than the admission-disposal clause — the patch now resolves `admission.Dispose()` as an invocation, its containing `FinallyClauseSyntax`, and that clause's parent `TryStatementSyntax`.
  - `[medium]` `[patch]` The first post-log `try` token was not structurally tied to the admission lease — the patch now identifies the admission `try` from the disposal clause and separately identifies the direct inner lifecycle-cleanup `try` for the ledger's post-log ordering proof.
  - `[medium]` `[patch]` A later raw `admission.Dispose();` substring could have matched a comment, string, or unrelated member — the patch now selects the exact invocation expression and asserts that its syntax span is contained by the admission `finally` block.
  - `[false]` `[reject]` The EventStore gitlink would silently ship with DW-683 — it appeared as concurrent unstaged workspace state after the clean entry check and was never staged as part of the owned change.
  - `[false]` `[reject]` The Memories gitlink would silently ship with DW-683 — it appeared as concurrent unstaged workspace state after the clean entry check and was never staged as part of the owned change.
  - `[false]` `[reject]` Missing exact keywords would throw before a Shouldly ordering assertion — a loud test failure is the required fail-closed outcome for missing syntax, so a different failure message adds no behavioral coverage.
  - `[medium]` `[patch]` Disposal could move outside its `finally` while remaining textually later — the patch asserts that the exact invocation is contained by the exact disposal `FinallyClauseSyntax`.
  - `[false]` `[reject]` The two gitlinks contradict the test-only claim — both are unstaged concurrent workspace changes, not reviewed DW-683 files, and the task commit uses explicit owned paths.
  - `[medium]` `[patch]` No verification proved that disposal belongs to the exception-safe admission cleanup clause — the syntax-tree containment and parent-clause assertions now provide that proof, and the complete affected class passed.
  - `[false]` `[reject]` The reviewed diff showed unrelated gitlink advances — workspace inspection confirmed they are unstaged concurrent state and no dependency update is included in the owned change.
  - `[medium]` `[patch]` The diff implemented lexical collision resistance but not the defensible structural reading of admission cleanup — the final test now proves both the exact post-log inner keyword order and structural containment in the outer admission `finally`.
  - `[false]` `[reject]` The specification is planning material rather than regression behavior — that separation is intentional; executable evidence lives in `CommandFormEmitterTests` and the spec records traceability only.
  - `[false]` `[reject]` Submodule-pointer surfaces diverge from the bundle intent — they are concurrent unstaged state outside the task's explicitly staged paths, not a DW-683 change.
  - `[false]` `[reject]` No runtime generated-form disposal test was added — the bundle identifies a static test-oracle defect, and the syntax-tree regression directly observes the requested test surface without changing or claiming runtime behavior.

### 2026-09-05 — Review pass (bmad-build step-04)

- `[false]` `[carried]` Blind hunter: mixed DW-683 and bump-spec workstreams in one working-tree diff — carried: EventStore/Memories gitlinks and the bump spec are concurrent unstaged workspace state; HEAD commit `8b38d7bb` contains only the owned test and this spec.
- `[false]` `[carried]` Blind hunter: EventStore gitlink landed option B with no recorded choice — carried: the pointer is not part of the owned change and remains unstaged.
- `[false]` `[carried]` Blind hunter: gitlink edits omit `currentCompatibility` / `CiGovernanceTests` updates — carried: those pointers are not in the owned commit, so identity tests were not asked to accept a new EventStore SHA.
- `[medium]` `[carried]` Blind hunter: first post-log `try`/`finally` tokens bound the inner dispatch pair — carried: `Emit_CommandExecutionAdmissionReleasesInFinally` now identifies `admission.Dispose()` as an invocation, walks to its `FinallyClauseSyntax`, and treats that parent `TryStatementSyntax` as the admission lease.
- `[medium]` `[carried]` Blind hunter: `admission.Dispose()` still located by substring `IndexOf` — carried: the test now selects the exact `admission.Dispose` invocation and asserts its span is inside the admission `finally` block.
- `[false]` `[carried]` Blind hunter: `DefaultIfEmpty(-1)` weakens missing-keyword failure — carried: a missing invocation or clause now fails via `.Single()` / Shouldly type assertions; a loud failure is the required fail-closed outcome.
- `[false]` Collision text is spliced after the submitted-log call inside the logger `if` — inserting `retryKeywordCollision` / `finallyKeywordCollision` after the complete call statement remains valid C# in the current braced emission; `FindToken` must be `IdentifierToken`, so a format change that breaks the splice fails parse rather than passing on the wrong token.
- `[false]` Other `IndexOf("finally")` oracles remain in sibling facts — intent forbids editing unrelated `IndexOf` assertions and the deferred-work ledger, so leftover substring debt in those tests is outside this story.
- `[false]` Empty Spec Change Log / verification-output fields — the requested fix is to edit this build's spec; executable evidence lives in the test class.
- `[false]` Bump-spec verification commands are not executable as written — that file is concurrent untracked work excluded by this story's test-oracle intent.
- `[medium]` `[carried]` Edge-case hunter: first post-log try/finally pair is inner lifecycle — carried: same structural admission-try / inner-cleanup-try split as the patched oracle.
- `[medium]` `[carried]` Edge-case hunter: later non-finally `admission.Dispose` still passes `IndexOf` — carried: containment is now `disposalFinally.Block.Span.Contains(disposeInvocation.Span)`.
- `[false]` `[carried]` Edge-case hunter: EventStore gitlink diverges from tagged `v3.102.0` — carried: unstaged concurrent pointer, not in `8b38d7bb`.
- `[false]` `[carried]` Edge-case hunter: DW-683 would ship unrelated submodule identities — carried: the owned commit does not include gitlink hunks.
- `[medium]` `[carried]` Verification-gap: hardened fact still binds inner dispatch `try`/`finally` — carried: pre-verified gap; current test asserts the disposal invocation's enclosing `finally` is the admission `try`'s `finally`, that the admission `try` contains the submitted-log call, and that the inner lifecycle `finally` precedes the admission `finally` keyword.
- `[false]` `[carried]` Verification-gap other: EventStore/Memories gitlinks vs `CiGovernanceTests` — carried: unstaged concurrent state outside the owned paths.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: focused test assembly builds with zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormEmitterTests` -- expected: the entire affected test class passes.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Hardened the command-form admission-disposal test oracle against larger identifiers containing `try` or `finally`. The final assertion uses Roslyn syntax structure to prove both the exact post-log lifecycle-cleanup keyword order and that the exact `admission.Dispose()` invocation belongs to the outer admission `finally` clause.

Files changed:
- `../../tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` — adds collision identifiers, exact syntax-token checks, and structural disposal-clause containment assertions.
- `spec-dw-683-command-form-emitter-test-token-anchoring.md` — records the implementation contract, verification, review triage, and result.

Review findings breakdown:
- Patches applied: 1 grouped medium entry (6 duplicate reports), covering structural `try`/`finally` association and exact disposal-in-finally containment. Patched counts: high 0, medium 1, low 0.
- Items deferred: 0.
- Rejected: missing syntax already fails closed; changing the failure message adds no behavioral evidence.
- Rejected: EventStore gitlink shipping claim; the pointer is concurrent unstaged workspace state outside the explicit task paths.
- Rejected: Memories gitlink shipping claim; the pointer is concurrent unstaged workspace state outside the explicit task paths.
- Rejected: combined gitlink/spec-boundary claim; neither pointer is staged into the task change.
- Rejected: verification review's combined gitlink claim; no dependency update is part of the owned change.
- Rejected: intent audit's submodule divergence; the unrelated pointers remain outside the task commit.
- Rejected: specification-is-not-runtime-evidence observation; the spec is traceability and the executable regression lives in the test.
- Rejected: missing runtime generated-form test; the bundle requests repair of a static test oracle, which the syntax-tree regression directly exercises.

Follow-up review recommendation: false. One medium grouped entry was patched; the first-pass threshold requires at least two medium entries or one high entry.

Verification performed:
- Release build of `Hexalith.FrontComposer.SourceTools.Tests`: passed with 0 warnings and 0 errors.
- Full `CommandFormEmitterTests` class: 49 passed, 0 failed, 0 skipped, 0 not run.
- Matrix audit: both current generated cleanup and misleading larger-token rows executed in `Emit_CommandExecutionAdmissionReleasesInFinally` and passed.
- `git diff --check`: passed.

Residual risks: none in the owned test change. Concurrent unstaged submodule pointers and a separate untracked spec remain outside this bundle and may prevent a clean-worktree finalization check.
