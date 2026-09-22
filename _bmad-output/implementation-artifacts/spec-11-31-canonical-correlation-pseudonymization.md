---
title: 'Story 11.31: Canonical Correlation Pseudonymization'
type: 'bugfix'
created: '2026-09-21'
status: 'done'
baseline_commit: '7830f110c02d0360efa5680aebc7db1b0bb24077'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Readiness diagnostics can emit a raw correlation identifier while lifecycle and hot-path logs use a different digest path, preventing safe cross-family joins. The hash implementations also disagree on token shape and oversized input.

**Approach:** Add one internal log pseudonymizer and route readiness, diagnostic-correlation, lifecycle, and hot-path values through it. Preserve established hot-path tokens and unrelated diagnostic bounding behavior.

## Boundaries & Constraints

**Always:** Trim input; map null/empty/whitespace to `absent`; emit non-empty input as `sha256:` plus the lowercase first eight SHA-256 bytes of the complete trimmed UTF-8 value. Process oversized input with bounded temporary storage, preserve deterministic .NET UTF-8 behavior, clear sensitive buffers, and hash only after the log level is enabled. Preserve logging metadata, runtime identifiers, and public APIs.

**Never:** Log a targeted raw identifier; truncate before hashing; append a length suffix to correlation tokens; change Activity/generated-adopter contracts or security-log sentinel policy; or alter generic `FrontComposerDiagnosticLog.Bounded` output beyond sharing its hash core.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Joinable | Same ID across readiness, diagnostic, lifecycle, and hot-path logs | Identical `sha256:<16 lowercase hex>`; no raw ID | Distinct fixtures differ |
| Normalize | `"  correlation-α  "` / `"correlation-α"` | Same full trimmed UTF-8 hash | No other Unicode normalization |
| Absent | Null/empty/whitespace | `absent` | No hash |
| Oversized | Long values differ only near the end | Tokens differ; temporary memory stays bounded | No truncation/raw fallback |
| Disabled | Target level disabled | No hash or entry | Control flow unchanged |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLogPseudonymizer.cs` -- new internal owner of normalized, bounded-memory hashing.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/{FrontComposerHotPathLog,FrontComposerDiagnosticLog}.cs` -- remove duplicate hashing; canonicalize readiness correlation while retaining enabled guards and generic diagnostic `:len:` output.
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:275-296` -- consume the canonical join token without changing state or Activity inputs.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/*Log*Tests.cs`, `Services/Lifecycle/LifecycleStateServiceTests.cs`, and `State/Navigation/ScopeReadinessGateTests.cs` -- prove edge cases, cross-family equality, no raw leakage, disabled-path behavior, and unchanged runtime IDs.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` and `_bmad-output/implementation-artifacts/deferred-work.md` -- reseal the exact test-name delta; close DW-1769/DW-1770 only after passing evidence.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/{FrontComposerLogPseudonymizer,FrontComposerHotPathLog,FrontComposerDiagnosticLog}.cs` and `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` -- add the shared helper and migrate consumers without metadata or runtime-identity drift.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/*Log*Tests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateServiceTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs` -- cover every matrix row, exact joins, enabled guards, and preserved generic diagnostic output.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` and `_bmad-output/implementation-artifacts/deferred-work.md` -- reseal only intentional test declarations, then close DW-1769/DW-1770 with evidence.

**Acceptance Criteria:**
- Given one normalized ID reaches readiness, lifecycle-observation, and hot-path events, when structured state is captured, then tokens are byte-identical, match `^sha256:[0-9a-f]{16}$`, and contain no raw ID.
- Given non-correlation diagnostics and lifecycle consumers, when the change runs, then diagnostic shape, event contracts, and in-process IDs remain unchanged.
- Given focused/default Shell, governance, and Release gates, when verified, then all pass with zero warnings and DW-1769/DW-1770 close with evidence.

## Implementation Notes

- Added a stateful UTF-8 streaming hash over a fixed 1,024-byte stack buffer; digest and UTF-8 buffers are cleared after use.
- Routed diagnostic correlation events 6004/6070, lifecycle observation, and all hot-path identifier digests through the canonical helper after their existing level guards.
- Kept generic diagnostic safe-value passthrough, 4,096-character prefix policy, original-character-count suffix, logging metadata, Activity inputs, and runtime identifiers unchanged.
- Resealed the six intentional public underscore-named test declarations: 3358 -> 3364, digest `20a0de26ad91d5a13a369b83bca69b2123ea1fd43e6005eb02be2bd12ada89af` -> `800bc077abcc2bf2de15f4cab6d732ca481dcff8d5a6919746b17ed5e1762776`.
- Following explicit human authorization to repair the prerequisite default-lane blocker, reconciled only the unsealed current/successor runtime tuple to EventStore `66cb4edaa2b474090f2b4e375d481a4bbcd70a08`, package `3.106.0`, and Builds `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab` across the validator, Quality workflow, governance assertion, and operator guide. Sealed identities v1-v3, historical approval/evidence, open approval state, and v4 absence remain unchanged.

## Spec Change Log

- 2026-09-22: Implemented the canonical pseudonymizer, matrix/runtime-identity regressions, analyzer-ledger reseal, and DW-1769/DW-1770 closure evidence.
- 2026-09-22: Applied the explicitly authorized prerequisite repair for the drifted unsealed EventStore current/successor source revision and re-ran the blocked acceptance gates.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1: event 6004 hashes a caller-redacted prefix | medium | patch | Verified at `FcFormAbandonmentGuard.razor.cs:144-147`: the sole production caller passes `RedactForLog(CorrelationId)`, so equal full identifiers cannot join other families and shared eight-character prefixes collide. |
| BH-2: direct wrapper coverage bypasses the form caller | medium | patch | Verified: `LogFamilies_SameIdentifier_EmitJoinablePseudonym` calls the wrapper directly, while the component-path test asserts navigation only and never captures event 6004. |
| BH-3: the Code Map omits the form consumer | low | rejected | The omission is real, but its proposed correction edits this build's spec, which review rules reject; the production caller and test still proceed under BH-1/BH-2. |
| BH-4: DW-1769 closure currently overclaims production-path proof | medium | patch | Verified: closure cites direct-wrapper equality, but the production event 6004 path still hashes `RedactForLog(CorrelationId)`; fixing that path and adding component evidence makes the existing closure accurate. |
| BH-5: no structural governance assertion pins centralization/guard placement | low | rejected | Verified that `SecurityLoggingGovernanceTests` has no pseudonymizer-specific structural rule, but current functional and allocation tests enforce behavior; future re-duplication is unlikely in everyday use and a new source-governance rule adds non-trivial complexity. |
| BH-6: generic `Bounded` compatibility lacks malformed/multibyte boundary vectors | medium | patch | Verified: existing generic-diagnostic vectors are ASCII/control focused; no test compares one-shot UTF-8 behavior for unpaired surrogates or the 4,096-character truncation boundary after switching hash cores. |
| BH-7: lifecycle disabled logging lacks a regression | medium | patch | Verified: the guard is correctly placed in `LifecycleStateService`, but its tests only use enabled/no-op loggers and do not prove oversized identifiers avoid hashing when Information is disabled. |
| BH-8: lifecycle message-id output lacks exact cross-family equality coverage | low | patch | Verified: the lifecycle test hard-codes one message token but never compares the same message ID with a hot-path output; a direct assertion can pin the shared-owner invariant. |
| BH-9: diagnostic-log class remarks describe all strings as `Bounded` passthrough | low | patch | Verified: events 6004 and 6070 now intentionally bypass `Bounded` and emit canonical pseudonyms, so the remarks are inaccurate; a direct documentation correction is sufficient. |
| BH-10: pseudonymizer documentation overstates correlation-only/support-safe semantics | low | patch | Verified: the helper also processes message IDs, view keys, projection types, and generic diagnostic material; comments should identify it as a join aid rather than an anonymity, uniqueness, integrity, or authorization primitive. |
| BH-11: recorded verification commands omit two reported invocations | low | rejected | The 45-test affected-family run and seven successor-validator run are reported without their exact commands, but the proposed correction edits this build's spec and is rejected by review rules. |
| VG-1: form-abandonment logging never receives the complete identifier | medium | patch | Pre-verified verification-gap evidence demonstrates `corr-guard-001` becomes `corr-gua…`, producing a non-joinable token while current component assertions still pass. |
| VG-2: disabled correlation hashing is covered only by an advisory Performance test | medium | patch | Pre-verified verification-gap evidence shows both changed diagnostic wrappers are excluded from blocking disabled-path coverage because the only allocation test is `Category=Performance` and the untraited disabled test does not call them. |

## Design Notes

Story 11.18c is the compatibility anchor: trim, hash the complete UTF-8 identifier, and emit the first eight digest bytes in lowercase. Stream large input through a bounded buffer. `FrontComposerDiagnosticLog.Bounded` keeps safe-value passthrough and `:len:<original-character-count>` by composing the raw hash primitive rather than the normalized correlation wrapper.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry.FrontComposerLogPseudonymizerTests -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests` -- expected: focused behavior/governance pass.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none -notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined` -- expected: default Shell lane passes.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-09-22):**
- Shell test-project Release build passed with 0 warnings and 0 errors.
- Focused behavior/security/governance lane passed 21/21.
- Diagnostic/hot-path/lifecycle/readiness regression lane passed 45/45.
- Solution Release build passed with 0 warnings and 0 errors.
- `git diff --check` passed.
- Explicitly authorized prerequisite repair verification passed: seven focused successor-validator tests and the exact formerly failing `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` fact (1/1).
- Post-review default Shell lane passed 2,746/2,746 with zero errors, failures, skips, or tests not run.
- Immutable EventStore runtime identity v1-v3 contracts and historical evidence retain zero diff from HEAD; the v4 contract and evidence tree remain absent.

### Review Findings

- [x] [Review][Patch] Disabled lifecycle allocation test pins an exact 568-byte baseline [tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateServiceTests.cs:307]
- [x] [Review][Patch] Disabled correlation allocation test requires exact zero on the default lane [tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs:319]
- [x] [Review][Patch] Canonical-pseudonym remarks still read as a 4096-character digest [src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerDiagnosticLog.cs:33]

#### Rejected

- Spec code map omits the form-abandonment caller — rejected, because the correction edits this spec.
- Story frontmatter says `done` while sprint status says `review` — rejected, because the correction edits this spec.
- Verification commands omit the 45-test lane and the successor-validator invocation — rejected, because the correction edits this spec.
- Analyzer reseal ignores new tests without underscores — `false`: the ledger hashes only underscore-containing public identifiers, and these tests contain none.
- Oversized-hash ceiling of 32,768 bytes is too loose — `false`: two million-character inputs allocate megabytes when buffered whole, so the ceiling still rejects unbounded temporary storage.
- SHA-256 length failure skips lifecycle subscribers and readiness dispatch — `false`: a non-32-byte SHA-256 digest is not a reachable input, and the throw is the correct loud failure.
- Readiness gate has no disabled-logger dispatch test — `false`: `Dispatch` runs after the log wrapper returns, and the disabled return stays inside `ScopeReadinessStorageReadyDispatched`.
