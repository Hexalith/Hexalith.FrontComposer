---
baseline_commit: 832a022fc8a3a2e3d0c4be43ee4abd855a1a9440
---

# Story 10.3: CLI text-output parity guard

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Test Architect,
I want text output covered at the same behavioral boundary as JSON output for CLI commands,
so that summaries, filtering, and budgets cannot drift between machine and human output.

## Acceptance Criteria

1. Given a CLI command has JSON summary, filtering, fail-flag, or diff-budget behavior, when tests are added or changed, then text-output pins cover the same shared behavior unless the story explicitly documents why text does not expose that field.

2. Given a migration or inspect output budget changes, when JSON caps are updated, then text output caps and omitted-budget markers are updated and tested intentionally.

## Tasks / Subtasks

- [x] Audit existing CLI JSON/text coverage before changing tests. (AC: 1, 2)
  - [x] Read `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` and `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` completely before editing.
  - [x] Read `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`, `MigrationCommandTests.cs`, `CliFixture.cs`, `CliHelpTests.cs`, and `OutputSanitizerTests.cs` completely before editing.
  - [x] Build a concise parity matrix in the story completion notes or a small implementation artifact that lists each shared behavior: summaries, filtering, fail flags, deterministic ordering, sanitization/redaction, and diff budgets.
  - [x] For every JSON-only behavior in that matrix, either add a text-format pin or record a narrow reason why text intentionally does not expose the field.

- [x] Add inspect text-output parity pins. (AC: 1)
  - [x] Prove text and JSON summary counts stay aligned for generated files, forms, grids, registrations, MCP manifests, warnings, and errors.
  - [x] Prove `--severity` and `--type` filtering affect text output before fail flags in the same way current JSON tests prove the machine-readable path.
  - [x] Preserve existing inspect semantics: `schemaVersion = frontcomposer.cli.inspect.v1`, `mcpManifestEntries` means generated manifest file count, `hidden` includes non-canonical severities, fail flags evaluate after filters, and default text remains sanitized/project-relative.
  - [x] If text still omits a JSON field such as `relatedType` on generated-file rows, document that as intentional compact human output rather than silently treating it as parity.

- [x] Add migrate text-output parity pins. (AC: 1, 2)
  - [x] Prove text and JSON summary counts stay aligned for `changed`, `unchanged`, `skipped`, `failed`, `manualOnly`, and `conflicts`.
  - [x] Prove `--fail-on-findings` returns `ExitCodes.ActionableFindings` for text output on `safe-fix`, `manual-only`, or `conflict`, and returns success for unchanged-only output.
  - [x] Keep the existing `MigrationText_CapsPerEntryAndAggregateDiffs` pin and strengthen it only if the audit finds an untested omitted-budget or sanitization boundary.
  - [x] Preserve the synthetic-only `HFCM9002` boundary; Story 10.4 owns production-emission decisions.

- [x] Add reusable test structure without introducing a second CLI framework or broad parser. (AC: 1, 2)
  - [x] Prefer small test helpers inside the CLI test project that run the same fixture through `--format json` and default text, then compare the shared contract fields.
  - [x] Do not parse terminal text as a new public schema. Assert the stable human-output fragments that are already contractual: summary labels, diagnostic IDs/severities, entry kind/id/path labels, diff omitted marker, and exit codes.
  - [x] Keep tests under `tests/Hexalith.FrontComposer.Cli.Tests/` and do not add package references or third-party test libraries.

- [x] Update documentation or guidance only where implementation proves it is needed. (AC: 1, 2)
  - [x] If a reusable parity checklist is created, place it under `_bmad-output/implementation-artifacts/` or another existing BMAD artifact location, not published `docs/` scratch space.
  - [x] Update `src/Hexalith.FrontComposer.Cli/README.md`, `_bmad-output/project-docs/api-contracts.md`, or the FC-CLI contract artifacts only if tests reveal a current contract statement is stale.
  - [x] Do not hand-edit generated `obj/**` output or `docs/_site/**`.

- [x] Run focused and broad validation, then reconcile story evidence. (AC: 1, 2)
  - [x] Run the focused CLI test lane for `tests/Hexalith.FrontComposer.Cli.Tests`.
  - [x] Run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md` before review.
  - [x] Attempt the standard filtered solution lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If broad validation is blocked locally, record exact command, exact blocker, whether the blocker occurs before test execution, focused fallback result, and CI authority.
  - [x] Reconcile the File List against the Story 10.1 validator output before moving to review.

## Dev Notes

### Story Context

Epic 10 carries Epic 7 tooling-governance follow-through without reopening completed Stories 7.1-7.5. Story 10.3 implements `E7-AI-3`: treat CLI text output as contract coverage wherever CLI JSON output already pins shared summary, filtering, fail-flag, or budget behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.3: CLI text-output parity guard`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md#4.3 epics.md - Add Stories 10.1 Through 10.5`; `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#5. Action Items`]

This is a guardrail/test-hardening story. The expected outcome is durable evidence that human-readable CLI output cannot drift behind JSON output for behavior that adopters rely on in terminals. Do not rewrite `frontcomposer inspect` or `frontcomposer migrate` unless a focused parity test exposes a real defect. [Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#3. Key Learnings`]

### Existing Implementation Facts

- `frontcomposer inspect` supports `--format text|json`, default text, `--severity hidden|info|warning|error`, `--type`, `--fail-on-warning`, and `--fail-on-error`. Fail flags evaluate after severity/type filtering. [Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`]
- `InspectCommand.RenderText` currently prints project/configuration/framework, generated-file count, forms/grids/registrations/MCP manifest/warning/error summary counts, generated-file family/path rows, and diagnostic detail lines. [Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`]
- `InspectJson.From` emits `frontcomposer.cli.inspect.v1` with `project`, `summary`, `generatedFiles[]`, and `diagnostics[]`. JSON and text share the loaded file order; JSON explicitly preserves that load order instead of re-sorting. [Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`; `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md#JSON Schema`]
- Current inspect tests already pin JSON schema/counts/order, severity filtering in JSON, severity filtering in text summary, warning/error totals in text, fail flags after filters, malformed sidecar sentinels, path redaction, and generated-output error paths. Story 10.3 should close remaining text parity gaps without duplicating all JSON assertions verbatim. [Source: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`]
- `frontcomposer migrate` supports `--format text|json`, default dry-run, `--apply`, `--fail-on-findings`, and one catalog edge: `9.1.0 -> 9.2.0`. [Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Command Surface`]
- `MigrationCommand.RenderText` currently prints apply/dry-run status, summary counts, deterministic entry rows, sanitized `what` text, and diffs. It uses `MigrationJson.MaxAggregateDiffChars` and `MigrationJson.MaxPerEntryDiffChars`, so text and JSON share the 64,000 aggregate / 8,000 per-entry diff budgets. [Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`]
- Current migrate tests already pin JSON dry-run/apply, path safety, invalid input, fail-on-findings in JSON, JSON diff budgets, and text diff budgets. The obvious remaining gap is that several behavior tests run only `--format json`; Story 10.3 should add text-format pins for the shared behaviors rather than changing the migration contract. [Source: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`]

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` - inspect renderers, filters, summary calculation, sidecar handling, and JSON shape.
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - migrate renderers, summary calculation, fail-on-findings, diff-budget constants, sidecar reader, and migration result ordering.
- `src/Hexalith.FrontComposer.Cli/CliApplication.cs` - command dispatch/help if any output/help behavior needs narrow adjustment.
- `src/Hexalith.FrontComposer.Cli/README.md` - user-facing CLI contract only if a verified behavior/doc mismatch is found.
- `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - inspect JSON/text/fail-flag coverage.
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - migrate JSON/text/fail-flag/diff-budget coverage.
- `tests/Hexalith.FrontComposer.Cli.Tests/CliFixture.cs` - fixture generation helpers for both output formats.
- `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` - help surface if CLI help changes.
- `tests/Hexalith.FrontComposer.Cli.Tests/OutputSanitizerTests.cs` - sanitizer expectations if output rendering changes.
- `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md` and `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - contract wording for any documented text/JSON exceptions.
- `eng/validate-story-artifacts.py` - Story 10.1 evidence gate behavior if validation output needs interpretation.

### Architecture Compliance

- Keep the CLI a leaf consumer. Do not add references from CLI back into higher layers or introduce `System.CommandLine`/third-party CLI frameworks. [Source: `_bmad-output/project-docs/architecture.md#7. Architecturally significant decisions`; `_bmad-output/project-docs/source-tree-analysis.md#src - product code`]
- The CLI public surface is `CliApplication.RunAsync`, `ExitCodes`, and `OutputSanitizer`; all other command implementation types are internal. Keep tests focused through `CliApplication.RunAsync` and existing internal test access patterns. [Source: `_bmad-output/project-docs/component-inventory.md#E. CLI surface`]
- Use repository conventions: .NET SDK `10.0.302`, `.slnx` only, central package versions, xUnit v3, Shouldly, and no `Version=` in `.csproj`. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/project-context.md#Testing Rules`]
- Do not modify runtime shell/MCP/source-generator behavior, generated output, package references, public API baselines, pacts, or submodule files for this story.
- Preserve `OutputSanitizer` on all user-visible strings and keep default output project-relative unless the existing `--absolute-paths` inspect option is explicitly tested. [Source: `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md#Project Selection and Path Safety`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md#Path Safety and Redaction`]

### Anti-Patterns To Avoid

- Do not treat JSON as the only contract surface. Epic 7 review already found real text-only regressions after JSON coverage looked strong.
- Do not invent a rigid text schema. The guard should pin stable terminal facts, not make every whitespace detail public API.
- Do not add broad snapshot tests for whole terminal output if focused assertions can prove the parity boundary with less churn.
- Do not duplicate every JSON assertion. Cover shared summary/filter/fail/budget behavior and explicitly document intentionally text-omitted fields.
- Do not change HFCM9002 production behavior; Story 10.4 owns that decision.
- Do not edit published `docs/` as scratch. If a docs contract is stale, update only the exact owned page and run the docs validation command or record its blocker.
- Do not mark tasks complete without changed-file, test-evidence, completion-note, or documented-blocker backing; Story 10.1 validator should catch false claims.

### Testing Requirements

- Focused CLI lane: run the CLI test project after changes. Prefer a direct xUnit v3 in-process/focused command if VSTest is socket-blocked locally, and record exact command/result.
- Required artifact gate before review: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md`.
- Required broad lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- If broad solution/VSTest lanes are blocked, record the exact command, exact blocker text, blocker timing, focused fallback result, and CI authority.
- Any changed `docs/` producer input requires the docs validation/fingerprint discipline from Story 10.2; no docs validation is required for test-only CLI changes.

### Latest Technical Information

No external API/package research is required for Story 10.3. Use the repository-pinned stack and current CLI contracts: .NET SDK `10.0.302`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, `frontcomposer.cli.inspect.v1`, and `frontcomposer.cli.migrate.v1`. The risk is local contract drift between two render formats, not stale third-party API knowledge. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`; `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`]

### Previous Story Intelligence

Story 10.1 implemented the mechanical story evidence gate. The dev agent must run `python3 eng/validate-story-artifacts.py --story <story>` before review and should expect File List and checked-task claims to be mechanically checked. [Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md#Senior Developer Review (AI)`]

Story 10.2 shows the right pattern for this epic: audit first, classify intentional retained behavior explicitly, update only owned surfaces, and document local broad-lane blockers without pretending they passed. [Source: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md#Dev Notes`]

Current worktree at story creation has an unrelated modified `_bmad-output/story-automator/orchestration-9-20260704-182122.md`. Do not revert it and do not include it in Story 10.3 implementation unless the story intentionally edits it.

### Git Intelligence

Recent commits show the Epic 10 foundation just landed:

- `832a022 docs(story-10.2): clean adopter historical labels`
- `86b6a92 feat(story-10.1): reconcile story review evidence`
- `88f0342 docs(story-9.2): document accepted submodule pointer updates`
- `f30a8ec feat: document accepted submodule pointer bumps for Story 9.2 and update orchestration state`
- `914279b feat: update orchestration state for Story 9.2 and document submodule pointer bumps`

Treat `_bmad-output/story-automator/orchestration-9-20260704-182122.md` as unrelated dirty orchestration state unless this story deliberately updates story-automator records.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md`.
- Sprint-status key: `10-3-cli-text-output-parity-guard`.
- Primary production files, if a real parity defect is found: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` and `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`.
- Primary test files: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`, `MigrationCommandTests.cs`, and `CliFixture.cs`.
- Contract/reference files to update only if proven stale: `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`, `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`, `_bmad-output/project-docs/api-contracts.md`, and `src/Hexalith.FrontComposer.Cli/README.md`.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 10 and Story 10.3 source of record.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` - approved Epic 7 follow-through proposal and AR12 scope.
- Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md` - E7-AI-3 action item and text-output parity learning.
- Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md` - mechanical evidence gate and validator behavior.
- Source: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md` - previous story audit/classification pattern.
- Source: `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md` - inspect v1 contract.
- Source: `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` - migrate v1 contract.
- Source: `_bmad-output/project-docs/api-contracts.md` - current CLI command contract summary.
- Source: `_bmad-output/project-docs/architecture.md` - no third-party CLI framework architecture decision.
- Source: `_bmad-output/project-docs/source-tree-analysis.md` - CLI/test project layout.
- Source: `_bmad-output/project-docs/component-inventory.md` - public CLI surface.
- Source: `_bmad-output/project-context.md` - project stack, testing, docs, and submodule rules.
- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` - current inspect text/JSON render paths.
- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` - current migrate text/JSON render paths and diff budgets.
- Source: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - existing inspect coverage.
- Source: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - existing migrate coverage.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM instructions, sprint status, Epic 10 source, the Epic 7 follow-through proposal, Epic 7 retro, Story 10.1 and 10.2 artifacts, FC-CLI inspect/migrate contracts, project API/architecture/source-tree context, current CLI renderers/tests, recent git history, and current git status.
- 2026-07-04: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX files matched the workflow patterns, so `_bmad-output/project-context.md`, `_bmad-output/project-docs/*`, contracts, previous stories, and live CLI source/tests supplied project context.
- 2026-07-04: Confirmed `sprint-status.yaml` had Epic 10 in progress, Stories 10.1 and 10.2 done, and Story 10.3 in `backlog` before story creation.
- 2026-07-04: Validated story context against the create-story checklist by adding concrete CLI UPDATE files, existing parity facts, anti-patterns, previous-story intelligence, and validation requirements.
- 2026-07-04: Ran `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md`; result: `Story artifact validation passed.`
- 2026-07-04: Dev-story baseline captured at `832a022fc8a3a2e3d0c4be43ee4abd855a1a9440`; Story 10.3 moved to `in-progress` in story and sprint status.
- 2026-07-04: Read `InspectCommand.cs`, `MigrationCommand.cs`, `InspectCommandTests.cs`, `MigrationCommandTests.cs`, `CliFixture.cs`, `CliHelpTests.cs`, and `OutputSanitizerTests.cs` completely before editing.
- 2026-07-04: Added inspect text parity tests for JSON/text summary counts, generated family rows, sanitized project-relative text, severity/type filtering before fail flags, and text fail-flag outcomes.
- 2026-07-04: Added migrate text parity tests for JSON/text summary counts across all migration entry kinds and text-mode `--fail-on-findings` behavior for safe-fix, manual-only, and unchanged-only cases. Conflict summary rendering is pinned through the shared summary renderer; no current public CLI fixture produces a conflict entry without changing production migration behavior.
- 2026-07-04: No production CLI or documentation changes were needed; audit found existing text compact-output omissions are intentional human-output choices rather than stale docs.
- 2026-07-04: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Release --no-restore` built the CLI test project, then VSTest aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-07-04: Focused fallback passed: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests -class Hexalith.FrontComposer.Cli.Tests.CliHelpTests -class Hexalith.FrontComposer.Cli.Tests.OutputSanitizerTests` -> 65/65 passed.
- 2026-07-04: Full CLI in-process assembly without environment override ran 66 tests with one local environment failure: `ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest` could not write `/home/administrator/.dotnet/toolResolverCache/...` because home is read-only.
- 2026-07-04: Full CLI in-process assembly with `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home` reran 66 tests with one local environment failure: the same packaging smoke test reached restore, then failed before package execution because NuGet network access to `https://api.nuget.org/v3/index.json` is denied.
- 2026-07-04: Required broad command attempted: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`; blocked before test execution by MSBuild named-pipe/socket creation (`MSB1025`, `System.Net.Sockets.SocketException (13): Permission denied`). CI remains authoritative for this lane.
- 2026-07-04: Ran `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md --base 832a022fc8a3a2e3d0c4be43ee4abd855a1a9440`; result: `Story artifact validation passed.`
- 2026-07-04: QA generate E2E workflow loaded `bmad-qa-generate-e2e-tests`, project context, checklist, and Story 10.3; added Playwright CLI E2E coverage for inspect/migrate text-output parity gaps at the process boundary.
- 2026-07-04: `npm --prefix tests/e2e run typecheck` passed after adding `tests/e2e/specs/cli-text-output-parity.spec.ts`.
- 2026-07-04: `npm --prefix tests/e2e run test:story-10-3` passed 2/2. The first attempt exposed sandbox-blocked NuGet restore through `dotnet run`; the spec now prefers the existing Release CLI binary when present and falls back to `dotnet run --no-restore` for clean workspaces.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-04.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implementation was test-only. No `src/` CLI renderer changes, package references, generated output, docs, public API baselines, pacts, or submodule files were changed.
- Parity matrix:
  - Inspect summaries: JSON `summary.generatedFiles/forms/grids/registrations/mcpManifestEntries/warnings/errors` now has a default-text parity pin in `InspectText_SummaryCountsMatchJsonForSharedContractFields`.
  - Inspect filtering/fail flags: existing JSON/type/severity/fail-flag coverage remains, and text output now has `InspectText_FilteringAffectsRowsBeforeFailFlags` proving `--type` and `--severity` filter rows/diagnostics before `--fail-on-warning` and `--fail-on-error`.
  - Inspect deterministic ordering: existing JSON tri-key ordering test remains; the new text summary test pins representative family/path rows from the same fixture without making all whitespace a terminal schema.
  - Inspect sanitization/redaction: existing sanitizer/path-redaction tests remain; the new text summary test also verifies default text does not leak the fixture root.
  - Inspect intentional text omissions: text remains compact human output and intentionally does not emit JSON-only structural fields such as `schemaVersion`, `project.path`, `generatedFiles[].relatedType`, or diagnostic `relatedType`/`path` columns. Existing JSON tests continue to own those machine-readable fields.
  - Migrate summaries: `MigrationText_SummaryCountsMatchJsonForSharedContractFields` compares text summary labels with JSON summary values for `changed`, `unchanged`, `skipped`, `failed`, `manualOnly`, and `conflicts`.
  - Migrate fail flags: existing JSON `--fail-on-findings` coverage remains, and `MigrateText_FailOnFindingsMatchesActionableSummaryKinds` proves default text returns `ActionableFindings` for safe-fix/manual-only and success for unchanged-only. Conflict participation is covered by the shared summary/actionable formula and renderer matrix; no current public CLI path produces conflict entries without fabricating production behavior.
  - Migrate diff budgets: existing `MigrationJson_CapsPerEntryAndAggregateDiffs` and `MigrationText_CapsPerEntryAndAggregateDiffs` remain unchanged and already pin the shared 8,000 per-entry / 64,000 aggregate budgets plus the omitted-budget marker.
  - Migrate sanitization/redaction: existing sidecar hostile-path tests remain; the new text manual-only fail-on-findings pin also verifies the fixture root is not leaked.
- Documentation audit outcome: no current contract statement was stale, so no published docs or contract artifacts were edited.
- Test evidence:
  - Required command attempted: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Release --no-restore`.
  - Local result: Blocked after build and before test execution by VSTest socket creation, `System.Net.Sockets.SocketException (13): Permission denied`.
  - Fallback evidence: focused direct xUnit v3 in-process runner passed 65/65 for `InspectCommandTests`, `MigrationCommandTests`, `CliHelpTests`, and `OutputSanitizerTests`.
  - Full CLI assembly evidence: direct xUnit v3 in-process runner is locally blocked only in `ToolPackagingSmokeTests` by read-only home and then denied NuGet network restore when `DOTNET_CLI_HOME` is redirected to `/tmp`.
  - QA E2E evidence: `npm --prefix tests/e2e run typecheck` passed; `npm --prefix tests/e2e run test:story-10-3` passed 2/2.
  - CI authority: required for VSTest solution lane and packaging smoke; focused in-process CLI behavior lane is local advisory evidence.
- Broad validation evidence:
  - Required command attempted: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - Local result: Blocked before test execution by MSBuild named-pipe/socket creation, `MSB1025` with `System.Net.Sockets.SocketException (13): Permission denied`.
  - Fallback evidence: focused CLI behavior in-process lane passed 65/65.
  - CI authority: required.

### File List

- `_bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md` - baseline, task checkboxes, parity matrix, test evidence, File List, and story status updated for dev-story.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Story 10.3 status moved from `ready-for-dev` to `in-progress` and then `review`.
- `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - added inspect JSON/text summary and text filtering/fail-flag parity pins plus capture helper.
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - added migrate JSON/text summary and text fail-on-findings parity pins plus capture helper.
- `tests/e2e/specs/cli-text-output-parity.spec.ts` - added focused Playwright CLI E2E coverage for Story 10.3 inspect/migrate text-output parity.
- `tests/e2e/package.json` - added `test:story-10-3` focused Playwright lane.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` - added Story 10.3 QA automation summary, coverage metrics, checklist status, and validation evidence.
- `tests/Hexalith.FrontComposer.Cli.Tests/` - pre-existing focused CLI test project path used for validation evidence; story-owned file changes are listed separately.
- `tests/Hexalith.FrontComposer.Cli.Tests` - pre-existing focused CLI test project path spelling used by the task; story-owned file changes are listed separately.
- `tests/Hexalith.FrontComposer.Cli.Tests/CliFixture.cs` - pre-existing audit evidence only; read completely and not modified.
- `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` - pre-existing audit evidence only; read completely and not modified.
- `tests/Hexalith.FrontComposer.Cli.Tests/OutputSanitizerTests.cs` - pre-existing audit evidence only; read completely and not modified.
- `_bmad-output/implementation-artifacts/` - pre-existing artifact location classification; no reusable parity checklist file was created.
- `docs/` - pre-existing published docs location classification; not used as scratch space.
- `_bmad-output/project-docs/api-contracts.md` - pre-existing doc-scope audit classification; no stale contract statement found and not modified.
- `src/Hexalith.FrontComposer.Cli/README.md` - pre-existing doc-scope audit classification; no stale contract statement found and not modified.

### Change Log

- 2026-07-04: Added CLI inspect/migrate text-output parity guard tests, recorded compact text-output exceptions and local blockers, passed story artifact validation, and moved Story 10.3 to review.
- 2026-07-05: Story-automator adversarial review (auto-fix mode). Independently reproduced 65/65 focused CLI lane and the 4 new parity tests via the direct xUnit v3 runner; verified the built Release assembly matches current source. `python3 eng/validate-story-artifacts.py` exits 0 (mechanical gate passed). Both ACs confirmed implemented and tested; 0 CRITICAL/HIGH/MEDIUM findings. Story file status moved review -> done.
- 2026-07-05: Story-automator review cycle 2. Cycle 1 promoted this story file to `done` but the sprint-status source-of-truth sync did not land, so `sprint-status.yaml` key `10-3-cli-text-output-parity-guard` was still `review`. Re-verified independently (mechanical gate exits 0 for default and `--base 832a022`; local rebuild NuGet-blocked so confirmed the Release test assembly embeds the current test method names + assertion literals; direct xUnit v3 in-process runner: focused CLI lane 65/65 and the 4 new parity tests 4/4 green; AC1/AC2 confirmed against the live renderers). 0 CRITICAL/HIGH/MEDIUM. Completed the missing step by syncing sprint-status `10-3-cli-text-output-parity-guard` -> `done`.
- 2026-07-05: Story-automator review cycle 3. The orchestrator-helper `verify-code-review` gate failed because `eng/validate-story-artifacts.py` reported eight dirty paths missing from the File List. Inspected each diff against baseline `832a022`: all eight are unrelated concurrent drift with no `frontcomposer inspect`/`migrate` text-or-JSON output surface (pact provider-verification CI/docs, Counter sample specimen scanning, Fluent v5 `aria-label`→`AriaLabel` migration in three Shell components plus its Shell test, and a `Microsoft.NET.Test.Sdk` version bump in a package-boundary test). Documented them under "Documented Unrelated Changes" without modifying or staging any of the eight source files; story-owned changes remain the CLI parity tests, e2e spec, e2e `package.json`, test-summary, sprint-status, and this story file. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md` now exits 0. 0 CRITICAL/HIGH/MEDIUM; sprint-status stays `done` because the artifact gate passes.

## Documented Unrelated Changes

These paths are dirty in the working tree but are unrelated concurrent drift, outside Story 10.3's CLI text-output parity scope. They are documented here per the Story 10.1 mechanical evidence gate and are intentionally **not** modified or staged by this story. Each reason is backed by the diff against baseline `832a022`.

- `.github/workflows/ci.yml` — Unrelated pact/contract CI change: drops `-RequireProviderVerification` from Gate 2c because provider verification is EventStore-owned. No CLI text/JSON output surface; belongs to the concurrent pact provider-verification work.
- `docs/reference/pact-contracts.md` — Unrelated pact-contract documentation edit describing the FrontComposer/EventStore provider-verification split. Not a CLI (`inspect`/`migrate`) contract document.
- `samples/Counter/Counter.Web/Program.cs` — Unrelated Counter sample host wiring: adds `CounterSpecimensDomain`/`Program` assemblies to `ScanAssemblies` for specimen scanning. Sample quickstart config, not CLI parity.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor` — Unrelated Fluent v5 migration: raw `aria-label` attribute replaced by the strongly-typed `AriaLabel` parameter. Shell UI, no CLI surface.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor` — Unrelated Fluent v5 `aria-label`→`AriaLabel` migration. Shell UI, no CLI surface.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` — Unrelated Fluent v5 `aria-label`→`AriaLabel` migration. Shell UI, no CLI surface.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs` — Unrelated Shell test update asserting `search.Instance.AriaLabel` to match the `AriaLabel` parameter migration above. Not a CLI test.
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` — Unrelated `Microsoft.NET.Test.Sdk` version bump (18.3.0 → 18.7.0) inside the package-boundary test's embedded csproj expectation. Testing package hygiene, not CLI output.

## Senior Developer Review (AI)

**Reviewer:** Administrator (story-automator autonomous review) on 2026-07-05
**Outcome:** Approve — status set to `done`.

### Verification performed

- **Mechanical reconciliation gate:** `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md` exits 0 (also with `--base 832a022...`). Gate passed, so `done` is permitted.
- **Test execution (independently reproduced):** ran the four new tests through the direct xUnit v3 in-process runner (VSTest sockets remain blocked, matching the recorded blocker) — 4/4 passed. Re-ran the focused CLI lane (`InspectCommandTests`, `MigrationCommandTests`, `CliHelpTests`, `OutputSanitizerTests`) — 65/65 passed, matching the Dev Agent Record.
- **Assembly/source parity:** the prebuilt Release assembly is ~2 min older than the sources, so confirmed the built IL embeds the exact current assertion literals (UTF-16LE), proving the 65/65 evidence reflects the reviewed code, not a stale build.
- **AC1 (text pins cover shared JSON behavior):** `InspectText_SummaryCountsMatchJsonForSharedContractFields` and `InspectText_FilteringAffectsRowsBeforeFailFlags` prove text summary counts, generated-family rows, sanitized project-relative output, and `--type`/`--severity` filtering-before-fail-flag ordering against `InspectCommand.RenderText`. `MigrationText_SummaryCountsMatchJsonForSharedContractFields` and `MigrateText_FailOnFindingsMatchesActionableSummaryKinds` prove migrate summary parity and `--fail-on-findings` actionable-kind behavior against `MigrationCommand.RenderText`. Intentionally text-omitted JSON-only fields are documented in the parity matrix.
- **AC2 (budget parity):** no diff budget changed; the retained `MigrationText_CapsPerEntryAndAggregateDiffs` still pins the shared 8,000 per-entry / 64,000 aggregate caps and the `[diff omitted: aggregate diff budget exceeded]` marker for text.
- **Task audit:** every `[x]` task is backed by changed test code, completion notes, or a documented blocker; File List reconciles with `git status` (test-only change, no `src/` renderer edits).
- **Security/quality:** test-only change; `OutputSanitizer` and project-relative defaults are preserved, and both the unit and E2E tests assert the fixture root does not leak into output. No new runtime attack surface.

### Non-blocking observations (LOW — not auto-fixed)

- **Migrate row ordering not pinned across formats:** `MigrationJson.From` and `MigrationCommand.RenderText` each independently sort entries by `(Path, DiagnosticId)`; no test asserts the two orderings stay identical. AC1's enumerated shared behaviors are summary/filtering/fail-flag/budget (not ordering), so this is outside the story's contract and intentionally not pinned to avoid text-as-full-schema churn. Candidate for a future hardening pass.
- **E2E root-leak assertion is platform-sensitive:** `not.toContain(workspace.root)` can be trivially satisfied on macOS where `/var/folders` tmp paths are canonicalized to `/private/var`. Harmless on the Linux CI lane; the unit tests already assert root non-leakage via `fixture.Root`.
- **File List includes annotated not-modified entries** (audit-evidence and task-path "spelling" rows). These are deliberately retained because the Story 10.1 mechanical validator cross-references task-referenced paths; left as-is since the validator passes with them.
