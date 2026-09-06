---
title: 'DW-693 MCP lifecycle disposal coverage'
type: 'chore'
created: '2026-09-06'
status: 'done'
baseline_revision: 'ab2968695b232a757870697408134947876a5105'
baseline_commit: 'ab2968695b232a757870697408134947876a5105'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** `McpLifecycleStoreDisposalTests` pins `TryReadSnapshot` after disposal but leaves the lifecycle store's acknowledgement and observed-transition entry points unprotected against regression. All guarded store operations must continue to fail closed with `ObjectDisposedException` after disposal.

**Approach:** Extend the existing disposal regression suite with focused tests for the live acknowledgement operation, `TrackAcknowledged` (the current symbol corresponding to the bundle's stale `AcknowledgeAsync` name), and `TryRecordObservedTransition`.

## Boundaries & Constraints

**Always:** Invoke each operation only after disposing a fresh `FrontComposerMcpLifecycleStore`; assert the externally observable exception type; retain the existing pre-disposal and idempotent-disposal coverage; follow the repository's xUnit v3 and Shouldly conventions.

**Never:** Change production disposal behavior or API visibility, duplicate the shared `ThrowIfDisposed` implementation, broaden this bundle into lifecycle semantics beyond disposal, edit `_bmad-output/implementation-artifacts/deferred-work.md`, or edit any `.bmad-loop` orchestration ledger.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Acknowledgement after disposal | A disposed store; acknowledgement arguments are otherwise irrelevant because the disposal guard is first | No acknowledgement is recorded | `ObjectDisposedException` is thrown |
| Observed transition after disposal | A disposed store; transition argument is otherwise irrelevant because the disposal guard is first | No transition is recorded | `ObjectDisposedException` is thrown |
| Snapshot read after disposal | Existing covered disposed-store read | No snapshot is returned | Existing `ObjectDisposedException` assertion remains intact |

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs` -- Existing focused xUnit/Shouldly regression suite. Add the two missing post-disposal operation tests here without disturbing its pre-disposal and double-dispose tests.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:39` -- `TrackAcknowledged` is the current acknowledgement entry point and calls `ThrowIfDisposed` before validating or using its arguments.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:83` -- `TryRecordObservedTransition` also calls `ThrowIfDisposed` before validating or using its transition.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:104` -- `TryReadSnapshot` is the already-pinned comparison operation; production code is read-only for this bundle.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:294` -- Shared disposal guard whose contract the tests pin; do not modify it.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj` -- xUnit v3 and Shouldly test project with internals access supplied by the production project; no project change is required.
- `.bmad-loop/runs/20260906-162951-8225/bundles/mcp-lifecycle-disposal-coverage/intent.md` -- Authoritative bundle intent and verbatim DW-693 entry; read-only.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- Orchestrator-owned resolution ledger; explicitly read-only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs` -- Add synchronous Shouldly assertions that `TrackAcknowledged` and `TryRecordObservedTransition` each throw `ObjectDisposedException` when called after `Dispose` -- this directly closes both uncovered branches of the shared fail-closed contract.

**Acceptance Criteria:**
- Given a fresh lifecycle store has been disposed, when its current acknowledgement entry point `TrackAcknowledged` is invoked, then `ObjectDisposedException` is observed before any argument validation or lifecycle side effect.
- Given a fresh lifecycle store has been disposed, when `TryRecordObservedTransition` is invoked, then `ObjectDisposedException` is observed before any argument validation or transition side effect.
- Given the expanded disposal test class, when its focused tests run, then the new assertions and the existing `TryReadSnapshot`, pre-disposal, and idempotent-disposal assertions all pass.
- Given the completed change, when repository diffs are inspected, then neither the deferred-work ledger nor production code has changed.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 7 findings — high 0, medium 0, low 4, false 3, maybe-false 0
- findings:
  - `[low]` `[reject]` The `TrackAcknowledged` test does not construct fully valid acknowledgement collaborators — the null-forgiven arguments deliberately prove that the unconditional first-statement disposal guard wins over argument validation. Building descriptor, result, transition-list, and lifecycle fixtures would add complexity for an implausible null-specific branch without materially strengthening this direct guard regression.
  - `[low]` `[patch]` `TryRecordObservedTransition` was exercised only with `null!` — replaced it with a valid `CommandLifecycleTransition`, then confirmed the focused 5/5 and full MCP 381/381 test runs pass.
  - `[low]` `[patch]` `TrackAcknowledged` originally used `CancellationToken.None` — changed it to an already-cancelled token so the test pins disposal precedence over cancellation, then confirmed the focused 5/5 and full MCP 381/381 test runs pass.
  - `[false]` `[reject]` The matrix's no-side-effect outcomes are allegedly unasserted — each assertion requires `ObjectDisposedException` from the operation's first-statement disposal guard; without that guard, acknowledgement reaches later non-disposal validation and the valid unknown transition returns `false`, so neither test can pass after reaching a recording path.
  - `[false]` `[reject]` Completed verification results were allegedly absent from the in-review spec — `## Auto Run Result` is intentionally written during this workflow's Finalize phase, after review, so this is expected phase sequencing rather than missing evidence.
  - `[low]` `[reject]` The spec's literal `git diff` commands are index-blind — review used the required baseline-relative unified diff and also ran cached and baseline-relative whitespace/protected-path checks successfully. The proposed fix edits this build's spec and is therefore rejected by the review protocol.
  - `[false]` `[reject]` The diff allegedly diverges from a literal public `AcknowledgeAsync` or facade-level reading — no `AcknowledgeAsync` exists in owned source or tests, while the intent names the existing disposal test class and two internal store methods sharing `ThrowIfDisposed`; `TrackAcknowledged` is the unique live acknowledgement operation matching that evidence.

## Design Notes

The bundle says `AcknowledgeAsync`, but no such current symbol exists anywhere in the owned source or tests. `FrontComposerMcpLifecycleStore.TrackAcknowledged` is the sole acknowledgement operation, is present from the store's introduction, and shares the exact `ThrowIfDisposed` guard described by DW-693. Testing that live symbol resolves the stale-name mismatch without inventing a new API. Supplying null-forgiven arguments keeps setup minimal and deliberately proves that disposal wins over later argument validation.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Debug --filter-class "*McpLifecycleStoreDisposalTests"` -- expected: the complete disposal regression class passes.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Debug` -- expected: the full owning test project passes with no warnings.
- `git diff --check` -- expected: no whitespace errors.
- `git diff -- _bmad-output/implementation-artifacts/deferred-work.md .bmad-loop` -- expected: empty output.

## Auto Run Result

Status: done

Summary: Expanded `McpLifecycleStoreDisposalTests` so every live `FrontComposerMcpLifecycleStore` operation guarded by `ThrowIfDisposed` is pinned after disposal. The new acknowledgement assertion also uses an already-cancelled token, and the observed-transition assertion uses a valid transition.

Files changed:
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs` -- added post-disposal coverage for `TrackAcknowledged` and `TryRecordObservedTransition`, plus updated class documentation.
- `_bmad-output/implementation-artifacts/spec-dw-693-mcp-lifecycle-disposal-coverage.md` -- recorded the implementation contract, review triage, and verification evidence.

Review findings:
- Patches applied: 2 low — use a valid observed transition and an already-cancelled acknowledgement token.
- Items deferred: 0.
- Rejected: 5 — a fully valid acknowledgement fixture adds disproportionate setup around an unconditional first-statement guard; the exception assertions already prove recording paths are not reached; performed results belong to this Finalize record; baseline-relative and cached checks covered the index-blind literal diff commands; and the live-symbol evidence resolves the stale `AcknowledgeAsync`/public-surface reading to `TrackAcknowledged`.

Follow-up review recommendation: false. Patched entries were high 0, medium 0, low 2; neither follow-up threshold was met.

Verification performed:
- Isolated Aspire baseline: all 16 declared resources reached `Running`/`Healthy`, including `frontcomposer-ui`; the AppHost was then stopped cleanly before test builds.
- Focused disposal class: passed 5/5 with 0 failed and 0 skipped.
- Full `Hexalith.FrontComposer.Mcp.Tests` project: passed 381/381 with 0 failed and 0 skipped.
- `git diff --check`, cached whitespace checks, and baseline-relative whitespace checks: passed.
- Deferred-work ledger, `.bmad-loop`, and production lifecycle-store baseline-relative diffs: empty.

Residual risks: None identified. The bundle's `AcknowledgeAsync` label is historical; `TrackAcknowledged` is the sole current acknowledgement operation sharing the specified disposal guard.
