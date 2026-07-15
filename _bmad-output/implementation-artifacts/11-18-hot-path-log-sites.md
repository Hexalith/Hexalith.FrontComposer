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
---

# Story 11.18c: Hot-Path Log Sites

Status: ready-for-dev.

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

- [ ] Freeze the implementation baseline and semantic hot-path inclusion rule before code edits.
- [ ] Classify the exact 49-file ledger by member/line, including Warning+ hot-path sites.
- [ ] Update temporary severity ownership tests to model security → hot path → residual Warning+.
- [ ] Add/extend source-generated helpers and migrate every frozen hot-path call.
- [ ] Add allocation/enabled-check, event-contract, behavior, and support-safety tests.
- [ ] Publish the exact intentional low-severity remainder and 11.18b residual ledger.
- [ ] Run Release, focused, broad, Governance, artifact, and file-integrity validation.

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

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-15: Materialized approved 11.18c child with semantic hot-path precedence and exact remainder requirements.
