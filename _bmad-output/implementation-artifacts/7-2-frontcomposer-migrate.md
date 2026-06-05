---
baseline_commit: 3ce0e2983a41c2448e8ffcd4ad96e2a3141bcbfb
---

# Story 7.2: frontcomposer migrate

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-05. -->

## Story

As an adopter developer,
I want to apply allowlisted code-fix migrations across version edges,
so that I can upgrade safely with a dry-run preview.

## Acceptance Criteria

1. Given `--from` / `--to` matching a `MigrationCatalog` edge, when I run `frontcomposer migrate` without `--apply`, then dry-run is the default, no source files are written, and JSON output uses schema `frontcomposer.cli.migrate.v1` with `applied=false`, deterministic `summary`, and `entries[]` fields. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.2-frontcomposer-migrate; _bmad-output/project-docs/api-contracts.md#3.2-frontcomposer-migrate]
2. Given an allowlisted safe fix for the current `9.1.0 -> 9.2.0` edge, when `--apply` runs, then only immediately planned eligible source files are written through the same-directory temp-file + replace path, `applied=true` is emitted only after every planned write succeeds, and rerunning migrate is idempotent with `unchanged` output. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; src/Hexalith.FrontComposer.Cli/README.md#Migration-Output-Notes]
3. Given a target inside `bin`, `obj`, `.git`, `packages`, `.nuget`, `nupkgs`, any `/generated/` segment, or a submodule root, when planning or apply encounters it, then the target is skipped/refused and is not written. Out-of-project paths and hostile sidecar paths must be redacted or reported as `__sidecar__/...`, never leaked as absolute host paths. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.2-frontcomposer-migrate; src/Hexalith.FrontComposer.Cli/PathUtilities.cs; src/Hexalith.FrontComposer.Cli/MigrationCommand.cs]
4. Given unsupported version edges, incompatible `--dry-run` / `--apply` usage, unsupported project/solution shapes, malformed solution project entries, or `.slnx` / `.fsproj` input, when migrate starts, then it fails closed with sanitized guidance, returns `ExitCodes.InvalidArguments` (`2`), and does not plan or write source changes. [Source: _bmad-output/project-docs/api-contracts.md#3.2-frontcomposer-migrate; src/Hexalith.FrontComposer.Cli/ProjectSelection.cs; src/Hexalith.FrontComposer.Cli/ExitCodes.cs]
5. Given migration diagnostics, when `HFCM9001` is found in source, then migrate applies only the FrontComposer-owned `AddFrontComposerDebugOverlay` -> `AddFrontComposerDevMode` safe fix; when `HFCM9002` is found through generated diagnostic sidecars, then migrate reports `manual-only` without writing. `HFCM9002` text in comments or ordinary source text must not be treated as a migration diagnostic. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; src/Hexalith.FrontComposer.Cli/README.md#Manual-Only-Migration-Diagnostics-HFCM9002]
6. Given migration output in text or JSON, when entries contain diffs, control characters, long hunks, or many changed files, then all user-visible fields pass through `OutputSanitizer`, per-entry diffs are capped at 8,000 chars, aggregate diffs at 64,000 chars, and `--fail-on-findings` returns `ExitCodes.ActionableFindings` (`1`) only for changed/manual-only/conflict findings. [Source: _bmad-output/project-docs/api-contracts.md#3.2-frontcomposer-migrate; src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs]
7. Given the previous Story 7.1 review evidence that full CLI in-process tests still had migration solution-selection failures, when Story 7.2 is complete, then the migration-focused CLI lane either passes those tests or records a verified, non-story environmental cause with exact failing test names. [Source: _bmad-output/implementation-artifacts/7-1-frontcomposer-inspect.md#Senior-Developer-Review-AI; tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs]

## Tasks / Subtasks

- [x] Confirm and document the FC-CLI-MIGRATE v1 contract (AC: 1, 2, 3, 4, 5, 6)
  - [x] Create `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`.
  - [x] Record the supported edge list, JSON schema name, entry kinds, exit-code behavior, fail-on-findings behavior, diff budgets, encoding limits, path-safety policy, sidecar trust rules, and atomic apply semantics.
  - [x] Cite live source and tests; do not invent a production SourceTools `HFCM9002` emitter.
- [x] Audit the existing migrate implementation before changing it (AC: 1, 2, 3, 4, 5, 6)
  - [x] Read `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` completely.
  - [x] Read `src/Hexalith.FrontComposer.Cli/CliApplication.cs`, `CommandOptions.cs`, `ProjectSelection.cs`, `PathUtilities.cs`, `OutputSanitizer.cs`, `JsonOptions.cs`, and `ExitCodes.cs`.
  - [x] Read `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`, `CliFixture.cs`, `CliHelpTests.cs`, `OutputSanitizerTests.cs`, and `ToolPackagingSmokeTests.cs`.
  - [x] Compare `src/Hexalith.FrontComposer.Cli/README.md` and `_bmad-output/project-docs/api-contracts.md` section 3.2 against implemented behavior; update only verified CLI docs.
- [x] Pin catalog edge and dry-run/apply semantics (AC: 1, 2)
  - [x] Keep `MigrationCatalog` edge `9.1.0 -> 9.2.0` with docs link `docs/migrations/9.1-to-9.2.md`.
  - [x] Prove `--dry-run` is the default and never writes source.
  - [x] Prove `--apply` writes only `PlannedFileEdit` sources and sets `applied=true` only after a clean run.
  - [x] Prove apply is idempotent and subsequent runs report `unchanged`.
- [x] Pin path-safety and redaction behavior (AC: 3)
  - [x] Prove `WriteSafetyPolicy` refuses `bin`, `obj`, `.git`, package/cache directories, `/generated/`, and submodule roots.
  - [x] Prove project-relative reporting for eligible files and `[redacted-path]` / `__sidecar__/...` reporting for out-of-root or hostile paths.
  - [x] Add focused tests for any unpinned excluded segment, symlink/canonical path, or submodule boundary gap found during audit.
- [x] Pin migration diagnostic and code-fix boundaries (AC: 5)
  - [x] Prove `HFCM9001` only replaces identifier usages of `AddFrontComposerDebugOverlay`; `nameof(...)` and comments must not produce false positives.
  - [x] Prove unsupported or non-allowlisted `CodeActionOperation`s become `manual-only` and discard unsafe file edits.
  - [x] Prove `FrontComposerMigrationCodeFixProvider.GetFixAllProvider()` remains `null`.
  - [x] Preserve the `HFCM9002` caveat: sidecar reading is currently synthetic/test-fixture evidence until Story 7.3/7.4 diagnostic governance emits production sidecars.
- [x] Pin project selection and known migration-test failures (AC: 4, 7)
  - [x] Re-run or reproduce `ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically`.
  - [x] Re-run or reproduce `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`.
  - [x] If either still fails, fix within `ProjectSelection` without broadening v1 support to `.slnx`, `.fsproj`, multi-project solutions, or unsupported project types.
  - [x] Keep explicit `--project` precedence over `--solution` and cwd discovery.
- [x] Pin output, encoding, and failure behavior (AC: 4, 6)
  - [x] Prove invalid `--format`, unsupported edges, and mutually exclusive `--dry-run` / `--apply` return `2` before writing.
  - [x] Prove source files over 16 MiB and unknown encodings fail closed.
  - [x] Prove text and JSON output sanitize control characters and truncate long diffs within documented budgets.
  - [x] Prove `--fail-on-findings` returns `1` for changed/manual-only/conflict findings, not for purely unchanged output.
- [x] Verify and record evidence (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run focused CLI migrate tests, at minimum the `MigrationCommandTests` lane.
  - [x] Run the broader CLI test assembly or in-process fallback and account for every failure by name.
  - [x] Create or update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the Story 7.2 result.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: `frontcomposer migrate` is already implemented in `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`, dispatched by `CliApplication.RunAsync`, documented in `src/Hexalith.FrontComposer.Cli/README.md`, and covered by `MigrationCommandTests`. This story is primarily confirm-and-pin plus gap closure, not a greenfield migration CLI build. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs]
- The public CLI entry is `CliApplication.RunAsync`; `MigrationCommand`, `MigrationPlanner`, `MigrationApplier`, `MigrationCatalog`, `MigrationDiagnosticSidecarReader`, and the code-fix provider are internal implementation details. Keep the package surface narrow. [Source: _bmad-output/project-docs/component-inventory.md#E-CLI-surface]
- The CLI has no third-party command framework by architecture decision. Continue using `CommandOptions`, `ProjectSelection`, `PathUtilities`, and `OutputSanitizer`; do not introduce System.CommandLine or another parser. [Source: _bmad-output/project-docs/architecture.md#7-Architecturally-significant-decisions-observed; _bmad-output/project-context.md#Code-Quality-Style-Rules]
- The CLI project targets `net10.0`, is packaged as a dotnet tool, and references Roslyn Workspaces packages through central package management. Do not add `Version=` to `.csproj` files and do not change Roslyn pins for this story. [Source: src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj; Directory.Packages.props; _bmad-output/project-context.md#Technology-Stack-and-Versions]
- The current migration catalog has one edge: `9.1.0 -> 9.2.0`, docs link `docs/migrations/9.1-to-9.2.md`, safe diagnostic `HFCM9001`, and manual-only diagnostic `HFCM9002`. Treat additional edges or extra automated fixes as out of scope unless Product supplies a new catalog contract. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs]
- Current safe fix scope is intentionally narrow: scan identifier names for `AddFrontComposerDebugOverlay`, skip `nameof`, and replace with `AddFrontComposerDevMode`. Do not make broad semantic rewrites, add FixAll, or accept code actions that add/remove documents, references, analyzer config, or non-approved file paths. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs]
- `HFCM9002` sidecar reading is currently a synthetic fixture contract. The README explicitly says there is no production SourceTools emitter yet; do not claim adopter builds produce those sidecars until the diagnostic catalog/drift stories own that emitter. [Source: src/Hexalith.FrontComposer.Cli/README.md#Manual-Only-Migration-Diagnostics-HFCM9002; src/Hexalith.FrontComposer.Cli/MigrationCommand.cs]
- Apply mode already uses a same-directory temp file and source-hash recheck before writing. Preserve the "clean apply only" meaning of `applied=true`; partial write, cancellation, stale hash, unreadable file, or unsafe target must produce `failed` and keep `applied=false`. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs]
- Path safety is not just a reporting concern. Planning skips unsafe compile items; apply rechecks canonical path and submodule boundaries before writing. Do not bypass `WriteSafetyPolicy` or `PathUtilities.ToProjectRelative`. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; src/Hexalith.FrontComposer.Cli/PathUtilities.cs]
- Previous Story 7.1 review recorded two migration solution-selection failures in the full CLI in-process assembly: `MigrationCommandTests.ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically` and `MigrationCommandTests.ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`. Story 7.2 owns verifying and closing or honestly reclassifying them. [Source: _bmad-output/implementation-artifacts/7-1-frontcomposer-inspect.md#Senior-Developer-Review-AI]
- No external dependency research is needed for this story: the relevant SDK, Roslyn Workspaces, STJ, xUnit v3, and Shouldly versions are pinned in repository configuration. Do not upgrade packages as part of Story 7.2. [Source: global.json; Directory.Packages.props]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
  - `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs` if the known migration solution-selection failures still reproduce
  - `src/Hexalith.FrontComposer.Cli/CliApplication.cs` only if help or dispatch text is wrong
  - `src/Hexalith.FrontComposer.Cli/README.md` only for verified CLI contract/doc alignment
  - `src/Hexalith.FrontComposer.Cli/PathUtilities.cs` / `OutputSanitizer.cs` only if shared path/sanitization hardening is required
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
  - `tests/Hexalith.FrontComposer.Cli.Tests/CliFixture.cs` if a safer fixture is required for path, sidecar, or encoding cases
  - `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` if help text changes
  - `tests/Hexalith.FrontComposer.Cli.Tests/OutputSanitizerTests.cs` if sanitizer behavior changes
- Expected BMAD artifacts:
  - `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Detected unrelated dirty file: `_bmad-output/story-automator/orchestration-1-20260604-140358.md` is modified before Story 7.2 creation. Do not revert it or include it in the Story 7.2 File List unless the dev agent intentionally changes it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-7.2-frontcomposer-migrate]
- [Source: _bmad-output/project-docs/api-contracts.md#3.2-frontcomposer-migrate]
- [Source: _bmad-output/project-docs/architecture.md#CLI]
- [Source: _bmad-output/project-docs/source-tree-analysis.md#Hexalith.FrontComposer.Cli]
- [Source: _bmad-output/project-docs/component-inventory.md#E-CLI-surface]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/7-1-frontcomposer-inspect.md#Senior-Developer-Review-AI]
- [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs]
- [Source: tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Reproduced the two Story 7.1 migration solution-selection failures in the CLI in-process lane: `ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically` and `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`.
- 2026-06-05: Fixed `ProjectSelection` by normalizing `.sln` project-entry path separators before canonicalization; reran `MigrationCommandTests` green.
- 2026-06-05: Exact VSTest focused lane remained locally socket-blocked with `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-05: Exact solution build command attempted and blocked by NuGet vulnerability data access to `api.nuget.org:443`; network-disabled build variant passed with 0 warnings / 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Confirmed and documented FC-CLI-MIGRATE v1 in `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md` with live source/test citations and the synthetic-only `HFCM9002` sidecar caveat.
- Fixed the `.sln` project-selection regression for Windows-style project paths on non-Windows hosts without adding `.slnx`, `.fsproj`, multi-project, or unsupported project-type support.
- Added migration pins for clean `applied=true`, invalid argument fail-closed behavior, `--fail-on-findings`, `nameof(...)` false-positive prevention, excluded path segments, and JSON diff budgets.
- Validation: focused `MigrationCommandTests` passed 39/39; broader CLI in-process fallback excluding packaging smoke passed 57/57; packaging smoke remains environment-blocked by NuGet/tool-cache access; VSTest remains socket-blocked.

### File List

- `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/7-2-frontcomposer-migrate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

### Change Log

- 2026-06-05: Captured Story 7.2 baseline and moved story to in-progress.
- 2026-06-05: Fixed migration `.sln` project path normalization and added focused migrate contract pins.
- 2026-06-05: Created FC-CLI-MIGRATE v1 contract and updated Story 7.2 test-summary evidence.
- 2026-06-05: Completed DoD validation and moved story to review.
- 2026-06-05: Senior Developer Review (AI) — verified build (0/0) and migration lane via the direct xUnit v3 in-process runner (no VSTest socket dependency); MigrationCommandTests 39/39 and full CLI assembly 58/58 green. Fixed AC6 text/JSON diff-budget parity (text `RenderText` now honors the 64,000-char aggregate cap), added `MigrationText_CapsPerEntryAndAggregateDiffs`, and removed a dead `dryRun` local. Full assembly now 59/59. Moved story to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-05 · **Outcome:** Approved (auto-fix mode)

### Scope & method

Adversarial review of Story 7.2 against the seven ACs. Git changes reconciled against the File List (no discrepancies; `_bmad-output/story-automator/orchestration-1-20260604-140358.md` is the documented pre-existing unrelated dirty file). The one production change this story introduces is `ProjectSelection.NormalizeSolutionProjectPath` (the `.sln` Windows-separator fix); the rest is test pins against the pre-existing migrate implementation in `MigrationCommand.cs`.

### AC verification

- **AC1 (dry-run default, schema, fields):** PASS — `MigrationCommand.RunAsync` defaults to dry-run; `Migrate_DefaultsToDryRunAndDoesNotWriteSource` pins `frontcomposer.cli.migrate.v1`, `applied=false`, deterministic summary, and full entry fields, and asserts no source write.
- **AC2 (apply writes planned files, atomic, idempotent):** PASS — `MigrateApply_WritesOnlyImmediatelyPlannedSourceFilesAndIsIdempotent`; same-directory temp-file + `File.Move` and source-hash recheck in `SourceFile.WriteAsync`/`MigrationApplier`; `applied=true` only when no `failed` entry and not cancelled.
- **AC3 (path safety + redaction):** PASS — `WriteSafetyPolicy`/`PathUtilities.HasExcludedSegment` exclude bin/obj/.git/packages/.nuget/nupkgs//generated/ and submodule roots; `WriteSafetyPolicy_RefusesExcludedSegments`, submodule and hostile-sidecar `__sidecar__/...` tests confirm redaction; no absolute host paths leaked.
- **AC4 (fail-closed on invalid input → exit 2):** PASS — invalid `--format`, conflicting `--dry-run/--apply`, unsupported edges, `.slnx`/`.fsproj`, malformed/out-of-root solution entries all return `InvalidArguments` before any write.
- **AC5 (HFCM9001 safe fix / HFCM9002 manual-only / no false positives):** PASS — identifier-only scan skips `nameof` and comments (`Migrate_DoesNotTreatNameofObsoleteApiAsSafeFix`, `...InsideCommentAsManualOnly`); `GetFixAllProvider()` returns null; sidecar HFCM9002 → manual-only with the synthetic-fixture caveat preserved.
- **AC6 (text/JSON sanitization + diff budgets + fail-on-findings):** PASS **after fix** — JSON path already enforced per-entry 8,000 and aggregate 64,000; the **text** path (`RenderText`) enforced only the per-entry cap. Fixed so text honors the aggregate cap too, and added `MigrationText_CapsPerEntryAndAggregateDiffs`. `--fail-on-findings` returns 1 only for changed/manual-only/conflict (verified by `Migrate_FailOnFindingsReturnsOneOnlyForActionableFindings`).
- **AC7 (close or honestly reclassify the two 7.1 migration failures):** PASS — both `ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically` and `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot` now pass. The dev recorded these as VSTest-socket-blocked; this review ran them directly via the xUnit v3 in-process executable (`bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests`), bypassing the socket transport, and confirmed them green — so AC7 is satisfied by the *tests pass* branch, not an environmental excuse.

### Findings (all auto-fixed)

1. **[MEDIUM] AC6 text/JSON diff-budget parity** — `MigrationCommand.RenderText` lacked the 64,000-char aggregate diff cap that `MigrationJson` enforces; with many changed files, text output could exceed the contracted aggregate budget. Fixed by mirroring the JSON budget (shared `MigrationJson.MaxAggregateDiffChars`/`MaxPerEntryDiffChars` constants).
2. **[MEDIUM] Test coverage gap** — the text-format render path had no test coverage; every migration test used `--format json`. Added `MigrationText_CapsPerEntryAndAggregateDiffs`.
3. **[LOW] Dead local** — `bool dryRun = ...` in `MigrationCommand.RunAsync` was computed but never read. Removed.

### Evidence

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/...csproj -c Debug -m:1 /nr:false` → 0 warnings / 0 errors (before and after fixes).
- `./bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests -class ...MigrationCommandTests` → 40/40 (39 prior + new text-cap pin).
- Full CLI in-process assembly → **59/59**, 0 skipped (includes `ToolPackagingSmokeTests`, which passed in this environment).

**Critical issues remaining: 0** → status set to **done**.
