---
created: 2026-07-15
updated: 2026-07-16
epic: 11
childStory: 11.18b
parentStory: 11.18
owner: Developer + Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: review
implementationGate: post-correction-readiness-pass
baseline_commit: 615605e3a358a542dcbb49b5b82601e60db7eb28
---

# Story 11.18b: Residual Warning-And-Above Log Sites

Status: review.

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

- [x] Freeze the implementation baseline and the exact 11.18a/11.18c exclusion ledgers.
- [x] Re-run the Roslyn direct-call census and record the residual Warning+ path/member/line inventory.
- [x] Add or extend eponymous source-generated logging helpers with collision-free EventIds.
- [x] Migrate every residual Warning/Error/Critical site while preserving behavior and severity.
- [x] Add focused event-contract, cardinality, exception, and adversarial-redaction tests.
- [x] Replace the temporary severity-only ownership guard with the final exclusive child ledger.
- [x] Run the required validation and reconcile the story File List from tracked plus untracked files.

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

GPT-5 Codex

### Implementation Plan

- Freeze the 54-site post-11.18a/post-11.18c residual Warning+ ledger before production edits.
- Migrate the ledger to one eponymous generated event family with explicit identities, preserved severity/cardinality, and enabled-check support-safety wrappers.
- Replace the temporary ownership partition with exact implementation-start and post-migration ledgers, synthetic negative guards, and focused event/redaction tests.
- Reconcile affected behavior tests to the generated contract, then run focused, broad, Governance, build, authoring, and artifact gates.

### Debug Log References

- 2026-07-15: Aspire runtime baseline could not start because the referenced `Hexalith.Parties` project already fails HFC0001 obsolete-API enforcement; `aspire stop` confirmed no AppHost remained running. Focused Release builds remained clean.
- 2026-07-15: RED — `ResidualWarningAndAboveLedger_DirectCalls_AreFullyMigrated` reported the exact 54 implementation-start sites: 49 Warning and 5 Error calls across 22 files.
- 2026-07-15: GREEN — all 54 sites migrated to EventIds 5800–5853; focused event contracts passed 2/2 and the exact ownership/governance class passed 5/5.
- 2026-07-15: RED — the broad Shell lane found 14 behavior assertions coupled to raw free-form text, attached exceptions, or `LoggerExtensions` call shapes.
- 2026-07-15: GREEN — owner tests now pin EventId/EventName, null exception attachment, and digested payloads; the Release solution build passed with 0 warnings/errors, broad non-Contract Shell passed 2,332/2,332, and Governance passed 158/158.

### Completion Notes List

- Frozen baseline commit `615605e3a358a542dcbb49b5b82601e60db7eb28` and the exact precedence-aware 54-site residual Warning+ census: 49 Warning and 5 Error calls across 22 files after the 11.18a security and 11.18c semantic hot-path owners were excluded.
- Migrated every owned site to `FrontComposerWarningLog` EventIds 5800–5853 with explicit EventNames and levels; no direct Shell `LogWarning`, `LogError`, or `LogCritical` call remains.
- Preserved branch cardinality, cancellation handling, control flow, and severity. Documented support-safety replacements convert raw exception messages to SHA-256 digests, omit attached exceptions/stacks, and digest projection/component/field/registration/policy/source/key/view/message/tenant/user/capability/bounded-context identifiers behind `ILogger.IsEnabled`.
- Replaced the temporary severity owner with an exclusive implementation-start ledger, an empty residual Warning+ owner, and the exact remaining 73 low-severity direct calls across 20 files. Governance now rejects duplicate EventIds, placeholder/signature drift, residual Warning+ calls, and unowned calls with synthetic negative coverage.
- Added exhaustive 54-event identity/severity/redaction coverage and disabled-path deferred-evaluation coverage; updated affected owner tests to assert generated contracts and support-safe payloads.
- Final validation passed: Release solution and Shell Tests builds with 0 warnings/errors; 2 focused helper tests; 5 exact ownership/governance tests; 2,332 broad non-Contract Shell tests; 158 Governance tests; exact File List, CRLF/UTF-8/final-newline, submodule, story-artifact, and `git diff --check` gates.

### File List

- `_bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md` (modified — baseline, implementation record, validation evidence, exact File List, and review transition)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — surgical Story 11.18b in-progress/review transitions)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerWarningLog.cs` (added — generated EventIds 5800–5853, enabled-check wrappers, bounded categories, and SHA-256 digests)
- `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` (modified — generated badge catalog, reader, negative-count, and notifier diagnostics)
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcLayoutBreakpointWatcher.razor.cs` (modified — generated viewport-tier and subscription diagnostics)
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs` (modified — generated missing-parameter, type-mismatch, and render-fault diagnostics)
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs` (modified — generated subscription and disposal diagnostics)
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs` (modified — generated template render-fault diagnostic)
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs` (modified — deferred tenant/user hashing and generated replacement render-fault diagnostic)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidationGate.cs` (modified — generated support-safe bootstrap validation error)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs` (modified — generated bounded ProblemDetails diagnostics)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs` (modified — generated digested-key storage diagnostics)
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs` (modified — generated registration and policy-conflict diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs` (modified — generated support-safe customization validation error)
- `src/Hexalith.FrontComposer.Shell/Services/InMemoryDiagnosticSink.cs` (modified — generated message-digest diagnostic)
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs` (modified — generated contract, component, and duplicate slot diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs` (modified — generated contract and duplicate template diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs` (modified — generated construction, contract, component, and duplicate override diagnostics)
- `src/Hexalith.FrontComposer.Shell/Services/StubCommandService.cs` (modified — generated callback and background-task errors)
- `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs` (modified — generated digested shortcut-handler diagnostic)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs` (modified — generated persistence, hydration, and badge-snapshot diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` (modified — generated palette dependency, enumeration, manifest, navigation, and authorization diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` (modified — generated schema and terminal-dispatch diagnostics)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs` (modified — generated null-items diagnostic)
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs` (modified — generated theme hydration and persistence diagnostics)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs` (modified — exact exclusive ledgers, 54-event contracts, placeholder drift guard, and synthetic negatives)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerWarningLogTests.cs` (added — exhaustive event identity, severity, exception, redaction, digest, and deferred-evaluation tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs` (modified — enabled generated-warning capture)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcFieldSlotHostTests.cs` (modified — generated event and support-safe payload assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs` (modified — generated event, digest, and null-exception assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerBootstrapGuardTests.cs` (modified — generated bootstrap error and message-digest assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` (modified — generated slot/view registration event and digest assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs` (modified — generated registration event and digest assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs` (modified — generated invalid/duplicate event and digest assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` (modified — generated shortcut event, deferred logging, digest, and null-exception assertions)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsTests.cs` (modified — generated persistence event and null-exception assertions)

## Change Log

- 2026-07-15: Materialized approved 11.18b child with live post-11.18a census and exclusive ownership gate.
- 2026-07-15: Implemented Story 11.18b, migrated the exact 54 residual Warning+ sites to generated support-safe logging, finalized the exclusive remainder ledger, and passed Release, broad Shell, and Governance gates.
