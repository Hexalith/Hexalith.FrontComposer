---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.18c
parentStory: 11.18
owner: Developer + Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
implementationGate: post-correction-readiness-pass
baseline_commit: 3356ae7e9758fc95d86bcccf8b485d9a497ace91
---

# Story 11.18c: Hot-Path Log Sites

Status: review.

## Story

As a FrontComposer maintainer,
I want command-lifecycle, projection-refresh, and polling hot-path logs migrated to source-generated
logging,
so that frequent operator telemetry avoids runtime template parsing and allocation overhead without
changing lifecycle or realtime behavior.

## Acceptance Criteria

1. **The hot-path semantic ledger is frozen first.** Given the current post-11.18a baseline is 208
   direct calls across 49 files and the temporary guard assigns 91 Trace/Debug/Information calls to
   11.18c by severity, when implementation starts, then a Roslyn-backed member/line census identifies
   the actual command-lifecycle, pending-command, EventStore projection, fallback polling, refresh,
   reconciliation, and cache hot paths regardless of severity. Non-hot low-severity calls are recorded
   as exact intentional remainder; the scope is not equated blindly with all 91 calls.

2. **Ownership precedence is exclusive.** Given the sibling scopes, when a site is classified, then
   11.18a security/fail-closed ownership wins first, this story's semantic hot-path ownership wins
   second, and 11.18b owns only residual Warning/Error/Critical calls. The final governance ledger
   proves no duplicate or unowned in-threshold site.

3. **Owned hot paths use generated logging.** Given an owned call at any level, when migrated, then it
   invokes a `[LoggerMessage]` generated method or enabled-check wrapper with collision-free EventId and
   EventName. Level, diagnostic identity, structured fields, cancellation, retry cadence, state
   transition order, event cardinality, and error propagation remain unchanged.

4. **Hot-path work stays allocation-aware and safe.** Given logging is disabled, when an owned branch
   executes, then no interpolation, enumerable materialization, hash, `ToString`, or template parsing
   occurs for logging. Given logging is enabled, structured values remain bounded and sanitized under
   canonical NFR-6 and NFR-10.

5. **Behavior and performance evidence is focused.** Given migration completes, when lifecycle,
   pending-command polling, projection subscription/fallback, reconciliation, and ETag/cache tests run,
   then observable state and timing remain identical. The source guard rejects reintroduced direct
   calls in the frozen hot-path ledger and synthetic negatives prove it is non-vacuous.

6. **The remainder ledger is honest.** Given direct calls remain after this story, when review runs,
   then every remaining low-severity site has an exact member/line rationale outside the hot-path
   threshold and every remaining Warning+ site is assigned to 11.18b. Wildcard or folder-wide
   exceptions fail the criterion.

## Tasks / Subtasks

- [x] Freeze the implementation baseline and semantic hot-path inclusion rule before code edits.
- [x] Classify the exact 49-file ledger by member/line, including Warning+ hot-path sites.
- [x] Update temporary severity ownership tests to model security → hot path → residual Warning+.
- [x] Add/extend source-generated helpers and migrate every frozen hot-path call.
- [x] Add allocation/enabled-check, event-contract, behavior, and support-safety tests.
- [x] Publish the exact intentional low-severity remainder and 11.18b residual ledger.
- [x] Run Release, focused, broad, Governance, artifact, and file-integrity validation.

## Dev Notes

### Initial Hot-Path Candidate Families

Read every listed member before freezing scope. At minimum examine:

- `Infrastructure/EventStore/ProjectionSubscriptionService.cs` (18 direct calls),
  `EventStoreQueryClient.cs`, `EventStorePendingCommandStatusQuery.cs`, and
  `SignalRProjectionHubConnectionFactory.cs`.
- `Infrastructure/PendingCommands/PendingCommandPollingDriver.cs` and
  `Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs` /
  `ProjectionFallbackRefreshScheduler.cs`.
- `State/PendingCommands/PendingCommandPollingCoordinator.cs`, `PendingCommandStateService.cs`,
  `PendingCommandOutcomeResolver.cs`, and `NewItemIndicatorStateService.cs`.
- `State/ReconnectionReconciliation/*`, `State/ProjectionConnection/*`,
  `State/ETagCache/ETagCacheService.cs`, and lifecycle rendering/service paths.
- High-frequency palette/grid effects only if the implementation-start evidence proves they meet the
  approved hot-path threshold; do not expand from “frequent-looking” names alone.

The exact full path/count source is
`tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs`.

### Guardrails

- Story 11.18 is a parent only. Do not change 11.18a security events or begin 11.18b residual edits.
- Preserve FC-CMD budgets: Degraded at 10,000 ms, polling every 1,000 ms up to 120,000 ms, and one
  transient retry after 250 ms. Logging migration must not alter `TimeProvider` scheduling.
- Keep HTTP acceptance distinct from projection/status confirmation and preserve reconnection/fallback
  visibility.
- Do not enable analyzers, change packages, alter public API, regenerate SourceTools output, or modify
  UX/localization/submodules.

### Technical Reference

The official .NET guidance identifies source-generated logging as the preferred high-performance path
for .NET 6 and later: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging

### Validation Commands

```bash
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests -parallel none
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md
```

## References

- `_bmad-output/planning-artifacts/epics.md` — canonical 11.18 decomposition.
- `_bmad-output/implementation-artifacts/11-18-fail-closed-security-log-sites.md` — security owner.
- `_bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md` — residual owner.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — finding M15.
- https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Freeze the 208-call post-11.18a baseline as an exact Roslyn path/member/line ledger before production edits.
- Replace severity-only ownership with security-first, semantic-hot-path-second, residual-Warning+-last classification.
- Migrate the frozen hot-path ledger to explicit source-generated events with enabled-check sanitization wrappers where values require bounding or pseudonymization.
- Preserve lifecycle, polling, refresh, reconciliation, cache, cancellation, and timing behavior; prove contracts, safety, and the exact remainder through focused and broad validation.

### Debug Log References

- 2026-07-15: RED — `SemanticHotPathLedger_DirectCalls_AreFullyMigrated` reported all 81 implementation-start direct calls with exact path/member/line/severity evidence.
- 2026-07-15: GREEN — Shell Tests Release build passed with 0 warnings/errors; `SecurityLoggingGovernanceTests` passed 3/3 after the 81-site baseline was frozen.
- 2026-07-15: RED — the former severity-only partition assigned 117 sites to 11.18b instead of the semantic residual count of 54.
- 2026-07-15: GREEN — precedence-aware governance passed 4/4 with exact 81 hot-path, 54 residual Warning+, and 73 intentional low-severity ledgers.
- 2026-07-15: RED — the expanded disabled-path allocation probe detected a one-time 24-byte delegate allocation in shared wrapper plumbing.
- 2026-07-15: GREEN — dedicated enabled-check wrappers removed delegate creation; hot-path contract/governance tests passed 7/7 and 215 focused behavior tests passed.
- 2026-07-15: RED — broad Shell validation found the pre-story reconciliation owner test still expected an attached exception object.
- 2026-07-15: GREEN — the owner regression now asserts exception-type-only support-safe telemetry while preserving subscriber fan-out; Release solution build passed with 0 warnings/errors, broad Shell passed 2,324/2,324, and Governance passed 157/157.
- 2026-07-15: RED — final XML documentation placement between `LoggerMessage` attributes and declarations produced 41 CS1587 errors.
- 2026-07-15: GREEN — all 81 public helper methods are documented with comments placed before attributes; final Release solution build returned 0 warnings/errors and focused contracts remained 7/7 green.

### Completion Notes List

- Frozen baseline commit `3356ae7e9758fc95d86bcccf8b485d9a497ace91` and exact semantic hot-path census: 81 sites across 17 files, comprising 63 Warning/Error and 18 Debug/Information calls.
- Replaced severity-only ownership with exclusive security → semantic hot path → residual Warning+ precedence and exact path/member/line/level ledgers for every implementation-start direct call.
- Migrated all 81 frozen sites to source-generated EventIds 5700–5780 with explicit EventNames, level/template preservation, and enabled-check wrappers for hashing or category conversion.
- Replaced raw/prefix-redacted identifiers with stable SHA-256 pseudonyms and converted former exception-bearing cache/reconciliation events to bounded exception-type categories with no attached exception or stack trace.
- Pinned the exact post-migration remainder at 54 Warning+ sites for 11.18b and 73 intentional non-hot Trace/Debug/Information sites; no hot-path direct call remains.
- Final validation passed: Release solution build 0 warnings/errors; 215 focused behavior tests; 7 hot-path contract/governance tests; 2,324 broad non-Contract Shell tests; 157 Governance tests; CRLF/UTF-8/final-newline, received-artifact, submodule, exact-file-ledger, story-artifact, and `git diff --check` gates.

### File List

- `_bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md` (modified — baseline, implementation record, validation evidence, exact File List, and review transition)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — surgical Story 11.18c in-progress/review transitions)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs` (added — generated EventIds 5700–5780, enabled-check wrappers, bounded categories, and identifier digests)
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs` (modified — generated wrapper lifecycle diagnostics and removal of prefix-only redaction)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs` (modified — support-safe pending-status protocol diagnostic)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` (modified — generated 304 protocol-drift diagnostics)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` (modified — generated projection subscription/restart/rejoin/disposal diagnostics)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` (modified — bounded subscriber-fault diagnostic)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs` (modified — generated tick and disposal diagnostics)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs` (modified — generated fallback-loop and disposal diagnostics)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs` (modified — generated budget/lane/protocol diagnostics with digested lane identifiers)
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` (modified — generated lifecycle state-machine diagnostics with deferred identifier hashing)
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs` (modified — deferred key hashing and exception-type-only cache diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` (modified — generated state-clear and scope-transition diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs` (modified — generated bounded outcome-resolution diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` (modified — generated polling-result diagnostics with digested MessageIds)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs` (modified — generated registration/resolution/eviction/scope diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs` (modified — generated subscriber-fault diagnostic)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs` (modified — generated epoch-scoped orchestration diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs` (modified — exception-type-only subscriber-fault diagnostic)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs` (modified — exact semantic precedence, event-contract, hot-path, and remainder ledgers with synthetic negatives)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerHotPathLogTests.cs` (added — allocation, deferred-work, event identity, digest, and support-safety tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherOwnerLoggingTests.cs` (modified — reconciliation owner regression now pins exception-type-only telemetry and fan-out)

## Change Log

- 2026-07-15: Materialized approved 11.18c child with semantic hot-path precedence and exact remainder requirements.
- 2026-07-15: Implemented Story 11.18c, migrated 81 semantic hot-path sites to generated logging, froze the exact 127-call remainder, passed all gates, and moved the story to review.
