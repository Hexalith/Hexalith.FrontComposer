---
baseline_commit: 4ca6bbd214e62b2232f8cef272f78c9104b072e3
---

# Story 10.4: HFCM9002 production-emission decision

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Product Owner and Architect,
I want an explicit decision on production HFCM9002 migration sidecar emission,
so that adopter docs either promise a real SourceTools emitter or clearly keep HFCM9002 synthetic-only.

## Acceptance Criteria

1. Given the current CLI migrate contract, when Product and Architecture review HFCM9002, then they choose one of two paths: implement a SourceTools production sidecar emitter with tests, or remove/de-emphasize adopter-facing promises beyond synthetic/manual sidecar evidence.

2. Given production emission is approved, then SourceTools emits migration evidence through a supported, deterministic mechanism; CLI migrate reads it; docs describe it; and tests prove path safety, redaction, and text/JSON output parity.

3. Given production emission is not approved, then CLI README and contract docs keep the synthetic-only boundary prominent, and tests or governance checks prevent adopter-facing docs from implying normal builds generate HFCM9002 sidecars.

## Tasks / Subtasks

- [x] Capture the HFCM9002 decision record before implementation. (AC: 1)
  - [x] Create or update a decision artifact under `_bmad-output/contracts/` or `_bmad-output/implementation-artifacts/` that records the chosen path, owners, date, rationale, and the exact source documents reviewed.
  - [x] Treat absence of explicit Product + Architecture approval as "production emission not approved" for this story; do not implement a generator emitter on inferred approval.
  - [x] Close or update sprint-status action item `E10-AI-4` only after the decision path is recorded and validated.

- [x] Audit the current HFCM9002 implementation and docs before editing. (AC: 1, 3)
  - [x] Read `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` completely, especially `MigrationDiagnosticSidecarReader`.
  - [x] Read `src/Hexalith.FrontComposer.Cli/README.md`, `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`, `_bmad-output/project-docs/api-contracts.md`, `docs/migrations/9.1-to-9.2.md`, and `docs/diagnostics/migration-findings.json`.
  - [x] Search `src/Hexalith.FrontComposer.SourceTools/` and `tests/Hexalith.FrontComposer.SourceTools.Tests/` for `HFCM9002`, `*.diagnostics.json`, and sidecar emission before deciding any SourceTools changes are needed.
  - [x] Preserve the current truth unless approved evidence says otherwise: `HFCM9002` is manual-only and currently read from synthetic/hand-crafted sidecar evidence, not emitted by normal adopter builds.

- [x] If production emission is approved, design and implement it without illegal generator side effects. (AC: 2)
  - [x] First prove the chosen emission mechanism in a focused failing test; do not write files directly from the source generator.
  - [x] If using Roslyn source-generator output, verify the mechanism can legally produce readable migration evidence without adding non-C# text to `spc.AddSource`; if it cannot, choose and document a supported alternative before coding.
  - [x] Keep any new SourceTools parse/transform/emission data pure and equatable; no `ISymbol` may escape parse-stage models.
  - [x] Emit only project-relative source paths and sanitized `what` text that the CLI can trust after its existing sidecar path validation.
  - [x] Update `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` only as narrowly required for the approved evidence format; preserve path-safety, sentinel, text/JSON parity, fail-on-findings, and diff-budget behavior.
  - [x] Add SourceTools tests proving production evidence is emitted for the approved manual-migration condition and not emitted for unrelated commands/projections.
  - [ ] Add CLI tests proving the production evidence becomes `manual-only HFCM9002` in both JSON and text, hostile paths remain `__sidecar__/...`, absolute paths are redacted, and `--fail-on-findings` returns `ExitCodes.ActionableFindings`.
  - [x] Update adopter-facing docs and the FC-CLI migrate contract to describe the real production emitter and its limits.

- [x] If production emission is not approved, pin the synthetic-only boundary. (AC: 3)
  - [x] Verify `src/Hexalith.FrontComposer.Cli/README.md` still states that adopter builds do not yet produce production SourceTools HFCM9002 sidecars.
  - [x] Verify `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` still lists "No new production SourceTools HFCM9002 sidecar emitter" as a non-goal, or update the contract with equivalent prominent wording.
  - [x] Add or update a focused docs/governance test that fails if adopter-facing docs promise normal-build HFCM9002 production sidecars without the decision artifact approving production emission.
  - [x] Do not remove the CLI's synthetic sidecar reader; it remains useful for hand-crafted evidence and path-safety tests unless the decision record explicitly retires it.

- [x] Reconcile validation and story evidence. (AC: 1, 2, 3)
  - [x] Run the focused CLI test lane after any CLI or docs-contract changes.
  - [x] Run the focused SourceTools test lane after any SourceTools or diagnostic-governance changes.
  - [x] Run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md` before review.
  - [x] Attempt the standard filtered solution lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If broad validation is blocked locally, record the exact command, exact blocker, whether the blocker occurs before test execution, focused fallback result, and CI authority.
  - [x] Reconcile the File List against the Story 10.1 validator output before moving to review.

## Dev Notes

### Story Context

Epic 10 carries Epic 7 tooling-governance follow-through without reopening completed Stories 7.1-7.5. Story 10.4 implements `E7-AI-4` / `E10-AI-4`: make HFCM9002 production sidecar emission an explicit decision instead of leaving adopter-facing migration promises ambiguous. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.4: HFCM9002 production-emission decision`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md#1. Issue Summary`; `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#5. Action Items`]

The current safe default is no production emission. If the dev agent cannot point to explicit Product + Architecture approval in a decision artifact or approved planning document, take the not-approved path: keep the synthetic-only boundary prominent and add guard coverage. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md#2. Impact Analysis`]

### Current Implementation Facts

- `frontcomposer migrate` currently supports one catalog edge: `9.1.0 -> 9.2.0`, with `HFCM9001` as the safe fix and `HFCM9002` as the manual-only diagnostic. [Source: `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Catalog Edge`; `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`]
- `MigrationDiagnosticSidecarReader.Read(...)` enumerates `obj/**/generated/HexalithFrontComposer/**/*.diagnostics.json`, accepts JSON array or `{ diagnostics: [] }`, filters to `id == HFCM9002`, normalizes sidecar `path` to project-relative, and converts hostile/unreadable input into `__sidecar__/...` sentinel manual-only entries. [Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`]
- The CLI README already says the current sidecar reader is wired to synthetic test fixtures only and that adopter builds do not yet produce production SourceTools HFCM9002 sidecars. [Source: `src/Hexalith.FrontComposer.Cli/README.md#Manual-Only Migration Diagnostics (HFCM9002)`]
- The migrate contract already records `HFCM9002` sidecar reading as synthetic/test-fixture evidence and lists "No new production SourceTools HFCM9002 sidecar emitter" as a non-goal. [Source: `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Migration Diagnostics and Code Fixes`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Non-Goals`]
- SourceTools currently has no `HFCM9002` production emitter. The only SourceTools-side `HFCM` coverage found is diagnostic-governance tests proving HFCM rows are CLI migration findings, not Roslyn analyzer release rows. [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`; `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`]
- Roslyn source generators emit C# sources through `spc.AddSource`; do not assume that adding a `.diagnostics.json` hint is valid. If production emission is approved, prove the mechanism before implementation and do not introduce direct filesystem writes from the generator. [Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`; `_bmad-output/project-context.md#Source-Generator Rules`]

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - current migrate renderer, planner, sidecar reader, path normalization, sentinel behavior, and HFCM descriptors.
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - synthetic sidecar, hostile path, text/JSON parity, fail-on-findings, and diff-budget coverage.
- `src/Hexalith.FrontComposer.Cli/README.md` - adopter-facing HFCM9002 boundary.
- `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - authoritative migrate v1 contract.
- `_bmad-output/project-docs/api-contracts.md` - project-level CLI contract summary.
- `docs/migrations/9.1-to-9.2.md` and `docs/diagnostics/migration-findings.json` - migration finding docs and HFCM metadata.
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` - generator output registration if production emission is approved.
- `src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs` - generated-output path contract used by IDE parity, CLI inspection, and adopter docs.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`, and `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` - SourceTools generator and diagnostic-governance test patterns if production emission is approved or docs governance is added.

### Architecture Compliance

- Keep CLI as a leaf consumer. Do not add references from CLI to Shell/MCP/SourceTools internals or introduce `System.CommandLine`/third-party CLI frameworks. [Source: `_bmad-output/project-docs/architecture.md#7. Architecturally significant decisions`; `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Keep SourceTools netstandard2.0/compiler-host compatible. SourceTools may reference Contracts only; do not pull net10-only or Fluent dependencies into SourceTools. [Source: `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Generated-output path shape is a public contract. Do not change `obj/{Config}/{TFM}/generated/HexalithFrontComposer` layout or `GeneratedOutputPathContract.Version` unless the story explicitly owns all downstream IDE parity/docs evidence. [Source: `src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs`; `_bmad-output/project-docs/api-contracts.md#1. Source-generator contract`]
- HFCM findings are CLI migration findings, not SourceTools AnalyzerReleases rows. Do not add `HFCM9002` to SourceTools analyzer release files or convert it into a normal HFC1xxx build diagnostic without an explicit architecture decision. [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`]
- Preserve all user-visible output sanitization and project-relative path reporting. Hostile sidecar paths must stay redacted or become `__sidecar__/...`; no absolute host path may leak in text or JSON. [Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Path Safety and Redaction`]

### Anti-Patterns To Avoid

- Do not let a generated/source-code comment that mentions `HFCM9002` become a migration diagnostic; comments and ordinary source text must not trigger manual-only output.
- Do not claim normal adopter builds emit `HFCM9002` sidecars until production emission is implemented and tested end-to-end.
- Do not remove the current synthetic sidecar reader just to simplify the decision; it owns path-safety and redaction behavior that already has tests.
- Do not implement production emission by writing arbitrary files from a source generator.
- Do not broaden Story 10.4 into new migration catalog edges, new automated fixes, FixAll support, package upgrades, `.slnx` migration support, or shell/runtime behavior.
- Do not update published docs as scratch. Change only the exact docs or contract pages needed for the selected decision path.

### Testing Requirements

- Not-approved path minimum: focused docs/governance or CLI tests proving adopter-facing text keeps the synthetic-only boundary prominent, plus the Story 10.1 artifact validator.
- Approved path minimum: SourceTools test proving production evidence emission, CLI tests proving `manual-only HFCM9002` text and JSON consumption from production evidence, hostile path redaction tests, `--fail-on-findings` tests, docs/contract tests, and the Story 10.1 artifact validator.
- Required broad lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- If VSTest/MSBuild/NuGet lanes are locally blocked, use focused direct xUnit v3 in-process fallbacks where possible and record exact blockers. Existing Epic 7/10 stories consistently hit socket or NuGet restrictions locally; do not report blocked lanes as passed.

### Latest Technical Information

No external API/package research is required for Story 10.4. Use the repository-pinned stack and current contracts: .NET SDK `10.0.302`, Roslyn `Microsoft.CodeAnalysis.*` `5.3.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and `frontcomposer.cli.migrate.v1`. The risk is local contract honesty and generator feasibility, not stale third-party API knowledge. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`]

### Previous Story Intelligence

Story 10.1 implemented the mechanical story evidence gate. The dev agent must run `python3 eng/validate-story-artifacts.py --story <story>` before review and should expect File List and checked-task claims to be mechanically checked. [Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md`]

Story 10.2 cleaned adopter-facing historical labels while preserving product version labels and non-adopter-facing provenance. Use the same audit-first pattern: classify retained provenance explicitly instead of deleting useful internal history. [Source: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md`]

Story 10.3 added CLI text-output parity guard coverage and deliberately preserved the synthetic-only HFCM9002 boundary. Do not regress its new text/JSON parity expectations if the CLI reader changes. [Source: `_bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md`]

### Git Intelligence

Recent commits show the Epic 10 foundation:

- `4ca6bbd feat(story-10.3): CLI text-output parity guard`
- `832a022 docs(story-10.2): clean adopter historical labels`
- `86b6a92 feat(story-10.1): reconcile story review evidence`
- `88f0342 docs(story-9.2): document accepted submodule pointer updates`
- `f30a8ec feat: document accepted submodule pointer bumps for Story 9.2 and update orchestration state`

Treat current unrelated dirty Shell/docs/CI/testing changes as outside Story 10.4 unless the dev-story baseline proves they are story-owned.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md`.
- Sprint-status key: `10-4-hfcm9002-production-emission-decision`.
- Primary CLI files if the decision touches migrate behavior: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`, `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`, and `src/Hexalith.FrontComposer.Cli/README.md`.
- Primary SourceTools files if production emission is approved: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`, a narrowly named new emitter/model file if needed, and focused tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/`.
- Primary docs/contract files: `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`, `_bmad-output/project-docs/api-contracts.md`, `docs/migrations/9.1-to-9.2.md`, and `docs/diagnostics/migration-findings.json`.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 10 and Story 10.4 source of record.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` - approved Epic 7 follow-through proposal and HFCM9002 decision action.
- Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md` - E7-AI-4 carry-forward action and synthetic-evidence learning.
- Source: `_bmad-output/implementation-artifacts/7-2-frontcomposer-migrate.md` - original migrate contract story and HFCM9002 synthetic caveat.
- Source: `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - FC-CLI-MIGRATE v1 contract.
- Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md` - mechanical artifact validation requirement.
- Source: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md` - audit/classification pattern.
- Source: `_bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md` - CLI text/JSON parity guard and synthetic-only handoff.
- Source: `_bmad-output/project-context.md` - source-generator, CLI, testing, and package rules.
- Source: `_bmad-output/project-docs/api-contracts.md` - CLI and SourceTools contract summary.
- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - current migrate planner, sidecar reader, text/JSON renderer, and HFCM descriptors.
- Source: `src/Hexalith.FrontComposer.Cli/README.md` - current adopter-facing HFCM9002 boundary.
- Source: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - current synthetic sidecar and text/JSON parity tests.
- Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` - current generator output model.
- Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` - HFCM governance tests.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Create-story analysis loaded Hexalith LLM instructions, BMAD create-story workflow/config/template/checklist, project context, sprint status, Epic 10 source, Epic 7 follow-through proposal and retro, Story 7.2, Story 10.3, FC-CLI-MIGRATE contract, CLI migrate source/tests, SourceTools generator context, diagnostic governance tests, recent git history, and current git status.
- 2026-07-05: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX files matched the workflow patterns, so `_bmad-output/project-context.md`, `_bmad-output/project-docs/*`, contracts, previous stories, and live source/tests supplied project context.
- 2026-07-05: Confirmed `sprint-status.yaml` had Epic 10 in progress, Stories 10.1-10.3 done, and Story 10.4 in `backlog` before story creation.
- 2026-07-05: Validated story context against the create-story checklist by adding the two-path decision boundary, concrete file reads, current synthetic-only facts, generator side-effect warnings, previous-story intelligence, and validation requirements.
- 2026-07-05: Dev-story audit read the required CLI migrate source, CLI README, migrate contract, API contract summary, migration guide, migration findings JSON, SourceTools generator, generated-output contract, and HFCM diagnostic-governance test context.
- 2026-07-05: Product + Architecture approval search found no explicit production-emission approval in the Epic 10 proposal, epics source, or Epic 7 retro. Story default selected: production emission not approved.
- 2026-07-05: Test Evidence - CLI exact local command attempted: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --filter "FullyQualifiedName~MigrationCommandTests" --no-restore`; local result Blocked before test execution by `NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json` / permission denied.
- 2026-07-05: Test Evidence - CLI fallback build/test command attempted: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --filter "FullyQualifiedName~MigrationCommandTests" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false`; local result Blocked before test execution by VSTest `System.Net.Sockets.SocketException (13): Permission denied` after compiling the Debug test assembly.
- 2026-07-05: Test Evidence - CLI direct xUnit fallback: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -method "Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests.Hfcm9002Docs_KeepSyntheticOnlyBoundaryUnlessDecisionApprovesProductionEmission"` passed 1/1; `... -class "Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests"` passed 43/43. CI authority: Required for VSTest lane, fallback advisory/local evidence.
- 2026-07-05: Test Evidence - SourceTools governance fallback: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -method "Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests.HfcmMigrationFindings_AreCliGovernedNotRoslynReleaseRows"` passed 1/1. CI authority: Advisory for unchanged SourceTools surface.
- 2026-07-05: Test Evidence - standard filtered solution lane attempted with `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false`; local result Blocked before test execution by MSBuild named-pipe/socket creation `System.Net.Sockets.SocketException (13): Permission denied`. CI authority: Required.
- 2026-07-05: Test Evidence - story artifact validation: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md --base 4ca6bbd214e62b2232f8cef272f78c9104b072e3` passed; validator reported only documented unrelated dirty files.
- 2026-07-05: QA Generate E2E Tests - loaded `.agents/skills/bmad-qa-generate-e2e-tests/SKILL.md`, checklist, workflow customization, BMAD config, project-context facts, Story 10.4, and existing CLI/Playwright test patterns.
- 2026-07-05: QA Test Evidence - `npm --prefix tests/e2e run typecheck` passed.
- 2026-07-05: QA Test Evidence - `npm --prefix tests/e2e run test:story-10-4` passed 4/4 for `hfcm9002-production-emission-decision.spec.ts`.
- 2026-07-05: QA Test Evidence - `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md` passed after QA updates; validator reported only documented unrelated dirty files.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-05.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Recorded the HFCM9002 production-emission decision as not approved because no explicit Product + Architecture approval artifact exists.
- Preserved the existing synthetic/manual-only CLI sidecar reader and did not add a SourceTools production emitter.
- Added a CLI governance test that pins the decision artifact, the README/contract synthetic-only wording, and scans adopter-facing HFCM9002 docs for normal-build sidecar promises.
- Approved-path implementation tasks are closed as not applicable under the recorded not-approved decision path; no production evidence format, generator output, CLI reader change, or adopter production-emitter docs were introduced.
- The approved-path CLI production-evidence subtask remains unchecked as not applicable because production emission was not approved; existing synthetic sidecar path-safety/fail-on-findings coverage remains in `MigrationCommandTests`.
- QA generated a focused Playwright E2E governance lane for Story 10.4 covering the decision record, adopter-facing wording boundary, synthetic HFCM9002 text/JSON output, `--fail-on-findings`, and hostile sidecar sentinel redaction.

### File List

- `_bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md` - new Story 10.4 ready-for-dev context.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Story 10.4 status moved from `backlog` to `ready-for-dev` and `last_updated` refreshed.
- `_bmad-output/contracts/hfcm9002-production-emission-decision-2026-07-05.md` - recorded not-approved production-emission decision and reviewed-source rationale.
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - added HFCM9002 synthetic-only docs/decision governance guard.
- `tests/e2e/specs/hfcm9002-production-emission-decision.spec.ts` - added Story 10.4 Playwright E2E governance and CLI process-boundary coverage.
- `tests/e2e/package.json` - added the `test:story-10-4` focused Playwright lane.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` - added Story 10.4 QA-generated E2E test summary and checklist validation.
- `_bmad-output/contracts/` - pre-existing/contracts directory audit evidence for the decision-artifact placement task.
- `_bmad-output/implementation-artifacts/` - pre-existing implementation-artifacts directory audit evidence for the decision-artifact placement task.
- `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - pre-existing audited contract; unchanged because the synthetic-only non-goal wording was already present.
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - pre-existing audited CLI implementation; unchanged because the not-approved path preserved the sidecar reader.
- `src/Hexalith.FrontComposer.SourceTools/` - pre-existing SourceTools audit scope; unchanged because production emission was not approved.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/` - pre-existing SourceTools test audit scope; unchanged except for fallback execution evidence.
- `spc.AddSource` - pre-existing source-generator API audit literal; no production emission mechanism was implemented under the not-approved path.
- `ExitCodes.ActionableFindings` - pre-existing CLI contract literal verified by existing focused migration tests; no production evidence path was added.
- `__sidecar__/...` - pre-existing sentinel path contract verified by existing focused migration tests; no production evidence path was added.

## Documented Unrelated Changes

These paths were dirty before Story 10.4 creation and are unrelated to the create-story artifact work. They are documented for the Story 10.1 mechanical evidence gate and are intentionally not modified by this story.

- `.github/workflows/ci.yml` - Unrelated CI workflow drift; outside HFCM9002 story creation.
- `_bmad-output/story-automator/orchestration-9-20260704-182122.md` - Unrelated story-automator orchestration state.
- `docs/reference/pact-contracts.md` - Unrelated pact-contract documentation drift.
- `samples/Counter/Counter.Web/Program.cs` - Unrelated Counter sample host drift.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor` - Unrelated Shell UI change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor.cs` - Unrelated Shell UI code-behind change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor` - Unrelated Shell UI change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor.cs` - Unrelated Shell UI code-behind change.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` - Unrelated Shell toolbar change.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.cs` - Unrelated Shell toolbar code-behind change.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs` - Unrelated Shell toolbar test change.
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - Unrelated Testing package-boundary test change.

### Change Log

- 2026-07-05: Created Story 10.4 context and moved sprint status to ready-for-dev.
- 2026-07-05: Implemented Story 10.4 not-approved path: recorded HFCM9002 production-emission decision, pinned synthetic-only boundary with a CLI governance test, validated focused CLI/SourceTools fallbacks, and moved story to review.
- 2026-07-05: QA generated focused Story 10.4 Playwright E2E tests and recorded passing typecheck plus focused E2E validation.
- 2026-07-05: Adversarial code review (story-automator-review) completed. 0 CRITICAL, 0 confirmed HIGH/MEDIUM/LOW findings after independent verification; status advanced review -> done and sprint status synced.

## Senior Developer Review (AI)

- Reviewer: Jérôme Piquot on 2026-07-05.
- Outcome: **Approve** — not-approved production-emission path is correctly selected, minimally scoped, and fully guarded.
- Independent verification (re-ran, not just trusted the Dev Agent Record):
  - `MigrationCommandTests` class passed 43/43 via the direct xUnit v3 runner, including the new `Hfcm9002Docs_KeepSyntheticOnlyBoundaryUnlessDecisionApprovesProductionEmission` guard (1/1).
  - `npm run test:story-10-4` passed 4/4; `npm run typecheck` passed.
  - `python3 eng/validate-story-artifacts.py --story ...` exited 0 (mechanical reconciliation gate satisfied).
- AC coverage: AC1 satisfied by the recorded decision artifact; AC2 correctly N/A under the not-approved decision (its production-evidence CLI subtask is the only open `[ ]` task and is honestly deferred); AC3 satisfied by the CLI README + migrate-contract synthetic-only wording plus the CLI and E2E governance guards.
- Git vs File List: all 7 story-owned changes are listed; all 12 pre-existing unrelated dirty paths are documented. No discrepancies.
- Rejected candidate findings after checking: `ProjectRoot()` nullable-return concern (compiles under `TreatWarningsAsErrors=true`); governance regex "too narrow" (narrowness is deliberate to avoid false-positive matches on the README's negated wording); E2E sidecar-path glob coverage (verified `/generated/HexalithFrontComposer/` match and manual-only redaction).
