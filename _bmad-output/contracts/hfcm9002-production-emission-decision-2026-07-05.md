# HFCM9002 Production Emission Decision

Date: 2026-07-05
Owners: Architect + Product Owner
Status: recorded
Decision: production emission not approved

## Rationale

No explicit Product + Architecture approval artifact was found that authorizes a production
SourceTools HFCM9002 migration sidecar emitter. Story 10.4 therefore follows its safe default:
keep HFCM9002 as synthetic/manual sidecar evidence only and guard adopter-facing docs against
implying normal builds generate HFCM9002 sidecars.

This decision does not retire the existing CLI sidecar reader. The reader remains useful for
hand-crafted evidence, synthetic fixtures, path-safety coverage, redaction coverage, and
text/JSON parity tests.

## Reviewed source documents

- `_bmad-output/planning-artifacts/epics.md` - Story 10.4 requires an explicit two-path decision.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` - E10-AI-4 records the decision action but does not approve production emission.
- `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md` - E7-AI-4 keeps production emission as a future design item and says HFCM9002 remains synthetic-only.
- `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - current migrate contract lists no production SourceTools HFCM9002 emitter as a non-goal.
- `src/Hexalith.FrontComposer.Cli/README.md` - CLI README says adopter builds do not yet produce production SourceTools HFCM9002 sidecars.
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - current sidecar reader consumes existing generated-output diagnostic sidecars and preserves path safety.
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` - current generator emits C# sources through `spc.AddSource` and has no HFCM9002 sidecar emitter.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` - HFCM rows are governed as CLI migration findings, not Roslyn analyzer release rows.

## Follow-up condition

Any future approval must update this record, implement a supported deterministic production evidence
mechanism without source-generator filesystem side effects, and add SourceTools plus CLI tests for
emission, path safety, redaction, fail-on-findings, and text/JSON parity before adopter docs may
describe normal-build HFCM9002 sidecar generation.
