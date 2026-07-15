---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.18b
parentStory: 11.18
owner: Developer + Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
implementationGate: post-correction-readiness-pass
---

# Story 11.18b: Residual Warning-And-Above Log Sites

Status: ready-for-dev.

## Story

As a FrontComposer maintainer,
I want every residual Shell Warning, Error, and Critical direct log call migrated to source-generated
logging after security and hot-path ownership is frozen,
so that operator-relevant failures follow one performant, structured, support-safe convention.

## Acceptance Criteria

1. **Ownership is frozen without overlap.** Given Story 11.18a is the security/fail-closed owner and
   Story 11.18c owns command-lifecycle, projection-refresh, and polling hot paths regardless of
   severity, when this story starts, then the implementation baseline records the exact residual
   Warning/Error/Critical ledger after those two ownership sets are removed. A site belongs to exactly
   one child; the denominator is never silently reduced.

2. **The live census is reconciled.** Given the post-11.18a governance baseline contains exactly 208
   direct calls across 49 Shell files, including 117 Warning/Error/Critical calls before hot-path
   precedence is applied, when the census is refreshed, then
   `SecurityLoggingGovernanceTests.ExpectedDirectCallCounts` is the exact path/count source of truth
   and every delta is explained by a commit or added to scope.

3. **Residual Warning+ sites use generated events.** Given an owned call currently invokes
   `LogWarning`, `LogError`, or `LogCritical`, when migrated, then it calls a `[LoggerMessage]` partial
   method or a thin enabled-check wrapper. Severity, exactly-once branch cardinality, diagnostic ID,
   exception behavior, structured field meaning, and control flow remain unchanged unless an explicit
   support-safety replacement is documented.

4. **Logs remain support-safe.** Given adversarial tokens, secrets, JWT or EventStore material,
   payloads, stack traces, absolute paths, tenant/user values, or unrestricted identifiers, when every
   owned event is captured, then none appears raw. Expensive formatting or pseudonymization occurs
   only behind `ILogger.IsEnabled`; exceptions are passed only when the existing non-security event
   intentionally requires them and redaction tests prove the result is safe.

5. **Governance is non-vacuous.** Given the migration is complete, when Governance runs, then it scans
   a non-empty exact production inventory, rejects residual direct Warning+ calls outside the 11.18c
   hot-path owner set, rejects duplicate EventIds and placeholder/signature drift, and reports
   repository-relative offenders. Synthetic negatives prove each guard can fail.

6. **Validation is green.** Given the owned ledger is empty, when the Release build, focused Shell
   tests, broad Shell non-Contract lane, Governance lane, story-artifact validator, and diff/line-ending
   checks run, then they pass under warnings-as-errors with no package, public API, schema, generated
   output, localization, UX, analyzer-policy, or submodule change.

## Tasks / Subtasks

- [ ] Freeze the implementation baseline and the exact 11.18a/11.18c exclusion ledgers.
- [ ] Re-run the Roslyn direct-call census and record the residual Warning+ path/member/line inventory.
- [ ] Add or extend eponymous source-generated logging helpers with collision-free EventIds.
- [ ] Migrate every residual Warning/Error/Critical site while preserving behavior and severity.
- [ ] Add focused event-contract, cardinality, exception, and adversarial-redaction tests.
- [ ] Replace the temporary severity-only ownership guard with the final exclusive child ledger.
- [ ] Run the required validation and reconcile the story File List from tracked plus untracked files.

## Dev Notes

### Prerequisites And Scope Rule

- Story 11.18 is a nonimplementable parent. This file is the executable 11.18b contract.
- Do not begin production edits until Story 11.18c has frozen its semantic hot-path ledger. A
  Warning+ call in a command-lifecycle, projection-refresh, or polling hot path belongs to 11.18c.
- Story 11.18a is already in review and owns all security/fail-closed sites. Never remigrate or
  renumber its MCP 8315–8318 or Shell 5660–5691 event family.
- The current 117 count is a pre-precedence ceiling, not permission to claim all Warning+ calls. The
  final denominator is `117 - Warning+ sites assigned to 11.18c`, reconciled at implementation start.

### Current-State Evidence To Read

- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs` — exact
  49-file/208-call path ledger and current 117/91 severity split.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLog.cs` — existing
  source-generated EventIds 5601–5650.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs` — 11.18a
  security EventIds 5660–5691 and strict support-safety pattern.
- `_bmad-output/implementation-artifacts/11-18-fail-closed-security-log-sites.md` and
  `_bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md` — sibling boundaries.

### Architecture And Anti-Patterns

- Keep logging helpers internal and eponymous; do not add Contracts or package surface.
- Use static partial `[LoggerMessage]` methods with explicit EventId, EventName, level, and structured
  PascalCase placeholders. Do not use interpolation or pre-format values before enabled checks.
- Do not enable CA1848/CA1873 or change `AnalysisMode`; Story 11.19d owns analyzer policy.
- Do not weaken the census with wildcard allowlists, path-prefix exemptions, or “below threshold”
  comments that lack an exact member/line ledger.

### Technical Reference

Microsoft recommends `LoggerMessageAttribute` source generation for high-performance logging because
templates are parsed at compile time and boxing/temporary allocations are avoided:
https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation

### Validation Commands

```bash
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests -parallel none
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md
```

## References

- `_bmad-output/planning-artifacts/epics.md` — canonical 11.18 decomposition and FR-29 trace.
- `_bmad-output/implementation-artifacts/epic-11-context.md` — workstream and ownership precedence.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — finding M15.
- https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-15: Materialized approved 11.18b child with live post-11.18a census and exclusive ownership gate.
