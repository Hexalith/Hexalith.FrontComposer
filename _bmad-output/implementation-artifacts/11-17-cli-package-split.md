---
baseline_commit: 6188288a0ccdf3394389019b732d630f25726925
---
# Story 11.17a: CLI package split

Status: in-progress

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: mechanical refactor; first child of the non-implementable Story 11.17 decomposition parent. -->

## Story

As a FrontComposer maintainer,
I want the CLI production source split one top-level type per file,
so that the CLI follows the repository convention without changing its behavior or shipped tool contracts.

## Acceptance Criteria

This story refines FR25, FR28, and FR29 and closes only the CLI slice of architecture finding **M14**. Scope is bounded by **Dev Notes > Scope Decisions and Never-list**.

1. **(mechanical CLI split)** Given `MigrationCommand.cs` contains 23 top-level types, `InspectCommand.cs` contains 14, and the otherwise-compliant CLI package has one residual two-type `CommandOptions.cs` bundle at the baseline, when the CLI package split is complete, then each named declaration is in a same-named `.cs` file under `src/Hexalith.FrontComposer.Cli/`; `MigrationCommand`, `InspectCommand`, and `CommandOptions` remain in their existing files; `CommandLineException` moves to `CommandLineException.cs`; every CLI production file contains at most one top-level type; and the production delta is exactly 3 modified plus 36 new source files with no deletion.

2. **(type and assembly identity)** Given all 39 declarations in the three baseline offender files are currently top-level and `internal`, when 36 are moved and the three eponymous declarations remain, then every declaration preserves its exact namespace, metadata name, containing-type identity, accessibility, type kind, modifiers, base type/interfaces, generic shape, attributes, constants, member order, member bodies, XML/comments, nullable context, and conditional directives. The CLI test friend-assembly relationship remains unchanged; a focused reflection/shape pin proves the 39 target types keep their full names and non-public visibility; and the existing exported authored type set (`CliApplication`, `ExitCodes`, `OutputSanitizer`) remains exact.

3. **(CLI behavior and wire contracts)** Given `frontcomposer inspect` and `frontcomposer migrate` are shipped dotnet-tool contracts, when the existing in-process CLI suite and packaging smoke run, then command parsing, help, exit codes, text output, ordering, path redaction, write safety, migration planning/apply behavior, and cancellation behavior remain unchanged; the JSON schemas still report exactly `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1`; and no snapshot, README, migration document, generated output, or package configuration changes solely because declarations moved files.

4. **(PublicAPI disposition)** Given the epic says the CLI `PublicAPI.Shipped.txt` remains unchanged, when repository evidence is checked, then the implementation records that no CLI PublicAPI baseline exists and none is created. Instead, the existing JSON/help/tool-package contracts plus the moved-type reflection pin demonstrate compatibility. All authored moved types remain internal, no public type/member is introduced, and the existing tracked baselines for Contracts.UI, Shell FcTbl, and Testing remain untouched.

5. **(durable guard and evidence)** Given a mechanical split can regress through later bundling, when validation runs, then a CLI-focused Roslyn Governance guard scans production `.cs` sources (excluding `bin`, `obj`, and generated outputs), reports every file with more than one direct namespace/compilation-unit type or delegate, has no broad allowlist, and proves non-vacuity with a synthetic multi-type sample. The focused CLI lane, Release solution build under warnings as errors, default and Governance gates, packaging smoke, artifact audit, CRLF check for changed C# files, `git diff --check`, and exact file-list audit pass.

## Tasks / Subtasks

- [x] **Task 1 — Split `MigrationCommand.cs` into 23 same-named files (AC: #1, #2)**
  - [x] Keep only `MigrationCommand` in `MigrationCommand.cs`; move the remaining declarations verbatim to the flat CLI project folder using the exact mapping below. Do not introduce subfolders or change the namespace.
  - [x] Preserve each declaration's current kind/modifiers. In particular, keep the record parameter order/defaults, `MigrationJson` constants/budgets, `MigrationPlanner` Roslyn workspace behavior, `SourceFile` encoding/write semantics, `WriteSafetyPolicy`, `SubmoduleBoundaryReader`, `UnifiedDiff`, and `FrontComposerMigrationCodeFixProvider : CodeFixProvider` unchanged.
  - [x] Compute the smallest correct using set for each physical file. Removing a demonstrably unused using is the only permitted cleanup; do not reformat or rewrite declaration/member text while moving it.

  | Existing declaration | Destination |
  |---|---|
  | `MigrationCommand` | `MigrationCommand.cs` (retain) |
  | `MigrationEdge` | `MigrationEdge.cs` |
  | `MigrationCatalog` | `MigrationCatalog.cs` |
  | `MigrationPlan` | `MigrationPlan.cs` |
  | `PlannedFileEdit` | `PlannedFileEdit.cs` |
  | `SourceFileContent` | `SourceFileContent.cs` |
  | `MigrationEntry` | `MigrationEntry.cs` |
  | `MigrationResult` | `MigrationResult.cs` |
  | `MigrationSummary` | `MigrationSummary.cs` |
  | `MigrationPlanner` | `MigrationPlanner.cs` |
  | `ProjectDocumentSet` | `ProjectDocumentSet.cs` |
  | `ProjectDocument` | `ProjectDocument.cs` |
  | `ProjectDocumentLoader` | `ProjectDocumentLoader.cs` |
  | `SourceFile` | `SourceFile.cs` |
  | `MigrationDiagnostics` | `MigrationDiagnostics.cs` |
  | `MigrationDiagnosticScanner` | `MigrationDiagnosticScanner.cs` |
  | `MigrationDiagnosticSidecarReader` | `MigrationDiagnosticSidecarReader.cs` |
  | `FrontComposerMigrationCodeFixProvider` | `FrontComposerMigrationCodeFixProvider.cs` |
  | `MigrationApplier` | `MigrationApplier.cs` |
  | `WriteSafetyPolicy` | `WriteSafetyPolicy.cs` |
  | `SubmoduleBoundaryReader` | `SubmoduleBoundaryReader.cs` |
  | `UnifiedDiff` | `UnifiedDiff.cs` |
  | `MigrationJson` | `MigrationJson.cs` |

- [x] **Task 2 — Split `InspectCommand.cs` into 14 same-named files (AC: #1, #2)**
  - [x] Keep only `InspectCommand` in `InspectCommand.cs`; move the remaining declarations verbatim to the flat CLI project folder using the exact mapping below.
  - [x] Preserve `GeneratedSourceFamily` member names/order, record parameter order/defaults, generated-family classification, framework selection, diagnostic sidecar behavior, type matching, severity filtering, ordering, sanitization, and `InspectJson` shape exactly.
  - [x] Compute per-file usings without opportunistic style or behavior changes.

  | Existing declaration | Destination |
  |---|---|
  | `InspectCommand` | `InspectCommand.cs` (retain) |
  | `InspectReport` | `InspectReport.cs` |
  | `InspectSummary` | `InspectSummary.cs` |
  | `GeneratedFileInfo` | `GeneratedFileInfo.cs` |
  | `GeneratedSourceFamily` | `GeneratedSourceFamily.cs` |
  | `InspectDiagnostic` | `InspectDiagnostic.cs` |
  | `InspectLoadResult` | `InspectLoadResult.cs` |
  | `GeneratedOutputLoader` | `GeneratedOutputLoader.cs` |
  | `FrameworkSelection` | `FrameworkSelection.cs` |
  | `GeneratedFileClassifier` | `GeneratedFileClassifier.cs` |
  | `TypeMatcher` | `TypeMatcher.cs` |
  | `TypeMatchResult` | `TypeMatchResult.cs` |
  | `DiagnosticFileReader` | `DiagnosticFileReader.cs` |
  | `InspectJson` | `InspectJson.cs` |

- [x] **Task 3 — Close the residual `CommandOptions.cs` package offender (AC: #1, #2)**
  - [x] Keep `CommandOptions` in `CommandOptions.cs` and move the one-line `internal sealed class CommandLineException(string message) : Exception(message);` declaration verbatim to `CommandLineException.cs` in the same folder/namespace.
  - [x] Do not change parser validation, exception type/base/primary-constructor behavior, option messages, short/long option rules, or `CliApplication` handling. This tiny move is included because a truthful package-wide guard would otherwise fail immediately on known debt outside the two god files.

- [x] **Task 4 — Add non-vacuous organization and identity guards (AC: #2, #4, #5)**
  - [x] Add one dedicated `tests/Hexalith.FrontComposer.Cli.Tests/Architecture/CliTypeOrganizationGovernanceTests.cs` test type. Use the already available Roslyn dependency; do not add a package or project reference.
  - [x] Parse production CLI `.cs` files and count only types/delegates directly owned by the compilation unit or its namespace. Nested types retain their containing-type identity and must not be promoted merely to satisfy the guard. Top-level-statement files may contain zero declared types.
  - [x] Exclude only generated/build output (`bin`, `obj`, generated sources). Do not exempt `MigrationCommand.cs`, `InspectCommand.cs`, whole folders, records, enums, interfaces, delegates, partial types, or future files through a broad allowlist.
  - [x] Add a synthetic source containing at least two different top-level declaration kinds and assert the analyzer reports the path and both names. This proves the repository scan cannot pass because of a broken locator or empty enumeration.
  - [x] Pin the 39 target `Type.FullName` values, top-level/non-nested identity, and non-public visibility through reflection. Also pin the existing exported authored type set to `CliApplication`, `ExitCodes`, and `OutputSanitizer`. This is the truthful substitute for the nonexistent CLI PublicAPI file; do not freeze unrelated compiler-generated/private nested helper details.

- [x] **Task 5 — Prove the shipped CLI contracts did not move (AC: #3, #4)**
  - [x] Run the entire in-process `Hexalith.FrontComposer.Cli.Tests` suite, including `InspectCommandTests`, `MigrationCommandTests`, and `CliHelpTests`; do not replace it with tests that merely instantiate the relocated records.
  - [x] Keep the existing schema assertions for `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1` green and verify their current field names, ordering, summary behavior, redaction, diff budgets, and exit codes remain unchanged.
  - [x] Run `ToolPackagingSmokeTests` so the packed dotnet tool still installs and executes from a local manifest. Inspect package contents only to confirm the existing tool contract; source file layout is not a package-content requirement.
  - [x] Search the repository for public API baselines and record the CLI disposition in the Dev Agent Record: no CLI baseline existed before or after; no baseline was fabricated or edited; the relevant existing baselines remain byte-identical.

- [x] **Task 6 — Reconcile mechanical evidence and broad gates (AC: #1-#5)**
  - [x] Recalculate the source inventory immediately before implementation if HEAD differs from `baseline_commit`. Record the two named god files as `2 bundled files / 37 declarations` to `37 same-named files`, and the complete CLI offender inventory as `3 files / 39 declarations / 36 excess` to zero multi-type production files, with 3 modified and 36 new production files.
  - [x] Inspect the final diff against the baseline. Every production change must be attributable to declaration movement or per-file using cleanup; reject behavioral edits, formatting churn, generated files, package/project changes, documentation drift, or gitlink movement.
  - [x] Build the final path ledger from both `git diff --name-status <baseline>` and `git ls-files --others --exclude-standard`; `git diff` alone does not see the 36 new unstaged files. Over that union, verify every changed/new `.cs` file has CRLF with no lone LF, no trailing-whitespace defect, and no unexpected generated/`*.received.*` artifact. Audit root gitlinks separately with `git diff --submodule=short <baseline> -- references`.
  - [x] Run configuration-aligned Release restore/build, the focused CLI executable lane, solution default lane, Governance lane, packaging smoke, received/generated artifact audit, changed-C# CRLF audit, and `git diff --check`.
- [x] Update this story's Dev Agent Record, exact File List, PublicAPI disposition, validation commands/results, and Change Log before moving the story to review. Test totals are evidence, not hard-coded expected counts.

### Review Findings

- [x] [Review][Patch] Remove the redundant `System.Text` import introduced by the split [src/Hexalith.FrontComposer.Cli/MigrationApplier.cs:1]
- [x] [Review][Defer] Semantically bind obsolete API migration before replacing matching identifiers [src/Hexalith.FrontComposer.Cli/MigrationDiagnosticScanner.cs:17] — deferred, pre-existing
- [x] [Review][Defer] Preserve SDK default compile items when a project also declares explicit linked sources [src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs:30] — deferred, pre-existing
- [x] [Review][Defer] Honor wildcard `Compile Exclude` patterns beyond exact paths and trailing `/**` [src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs:137] — deferred, pre-existing
- [x] [Review][Defer] Surface syntactically valid migration sidecars whose root has the wrong shape [src/Hexalith.FrontComposer.Cli/MigrationDiagnosticSidecarReader.cs:39] — deferred, pre-existing
- [x] [Review][Defer] Fail closed when `.gitmodules` exists but cannot be read [src/Hexalith.FrontComposer.Cli/SubmoduleBoundaryReader.cs:24] — deferred, pre-existing
- [x] [Review][Defer] Parse `.gitmodules` section and key names case-insensitively [src/Hexalith.FrontComposer.Cli/SubmoduleBoundaryReader.cs:38] — deferred, pre-existing
- [x] [Review][Defer] Include source encoding and BOM state in apply-time drift detection [src/Hexalith.FrontComposer.Cli/MigrationApplier.cs:30] — deferred, pre-existing
- [x] [Review][Defer] Preserve source-file metadata when replacing a migrated file [src/Hexalith.FrontComposer.Cli/SourceFile.cs:20] — deferred, pre-existing
- [x] [Review][Defer] Recheck the 16 MiB limit after reading concurrently changing source files [src/Hexalith.FrontComposer.Cli/SourceFile.cs:8] — deferred, pre-existing
- [x] [Review][Defer] Represent terminal-newline state accurately in informational unified diffs [src/Hexalith.FrontComposer.Cli/UnifiedDiff.cs:205] — deferred, pre-existing
- [x] [Review][Defer] Retain the configured trailing context at end-of-file in unified-diff hunks [src/Hexalith.FrontComposer.Cli/UnifiedDiff.cs:81] — deferred, pre-existing
- [x] [Review][Defer] Preserve specific manual-only sidecar findings when a code action is unsupported [src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs:174] — deferred, pre-existing

## Dev Notes

### Scope Decisions

1. **This is Story 11.17a, not the Story 11.17 parent.** The parent explicitly forbids promotion before decomposition. Sprint tracking therefore uses four executable children: CLI (this story), SourceTools, MCP/runtime plus benchmark relocation, and Shell bundles. Only this child becomes `ready-for-dev` now.
2. **One top-level type per file is the invariant.** Nested declarations are not present in the two target bundles, but the guard must not force future nested types to top level because that changes metadata identity and `private` accessibility. The Fluxor action-group exception belongs to 11.17d and is not a CLI exception.
3. **Mechanical means declaration-preserving.** Move syntax; do not modernize null checks, validation, cancellation, constructors, modifiers, collection expressions, serializer projections, exception handling, Roslyn APIs, or comments. Per-file import reduction is allowed only as required for clean compilation under warnings as errors.
4. **The CLI contract is a tool contract, not a C# library baseline.** There is no `src/Hexalith.FrontComposer.Cli/PublicAPI.Shipped.txt`. All target types are internal and the project grants `InternalsVisibleTo` to the CLI tests. Do not invent a PublicAPI baseline to satisfy stale epic wording; preserve type identity by reflection and actual user contracts through JSON/help/package tests.
5. **Flat placement is intentional.** The CLI project currently uses a flat folder and its architecture/source-tree documentation describes that shape. Same-named files in the existing folder are the least disruptive organization; this child does not introduce a new layering scheme.

### Current-State Inventory

- `MigrationCommand.cs`: 1,463 lines and 23 internal top-level declarations—1 class derived from `CodeFixProvider`, 13 static classes, and 9 records.
- `InspectCommand.cs`: 685 lines and 14 internal top-level declarations—6 static classes, 7 records, and 1 enum.
- `CommandOptions.cs`: 2 internal sealed classes; retain `CommandOptions` and move `CommandLineException`.
- Complete offender total: 3 files, 39 internal top-level declarations, 36 excess declarations. None is file-local or nested. All other CLI production files already satisfy the guard.
- `Hexalith.FrontComposer.Cli.csproj` targets `net10.0`, is a packable dotnet tool, uses SDK default compile items, references Roslyn Workspaces, and declares `InternalsVisibleTo` for the test assembly. The split requires no `.csproj` edit.
- `Hexalith.FrontComposer.Cli.Tests.csproj` already reaches the required Roslyn assemblies through the CLI project graph and uses xUnit v3 + Shouldly. Do not add a parser library or test framework.

### Behavior That Must Stay Frozen

- Inspect JSON schema: `frontcomposer.cli.inspect.v1`; migrate JSON schema: `frontcomposer.cli.migrate.v1`.
- Keep all current command options, exit codes, deterministic ordering, relative/absolute-path policy, sanitizer/redaction behavior, diagnostic severity thresholds, type suggestions, generated-family classification, migration catalog and safe-fix behavior, atomic write and drift checks, submodule exclusions, diff budgets, and text/JSON parity.
- Accepted constraints are not refactor opportunities: `TypeMatcher.Distance` retains its 256-character bound; `UnifiedDiff` retains its 32-line lookahead; unsupported/ambiguous encoding behavior remains fail-closed; `formattingApplied` remains truthful and currently false; top-level MSBuild import warnings remain; informational diffs need not become patch-applicable.
- Also leave the review's unrelated CLI observations untouched: cancellation exit-code semantics, identifier-only migration scanning, missing-dotnet wording, generated-artifact naming duplication, and duplicate sidecar/path helpers belong to other decisions or follow-ups.

### Architecture Compliance

- CLI remains a Layer 2 consumer with no FrontComposer project reference and no third-party CLI framework.
- Preserve the namespace `Hexalith.FrontComposer.Cli`, file-scoped namespace syntax, internal visibility, and the current project/assembly boundary.
- New C# files must use CRLF. Preserve existing K&R declaration/member formatting during the mechanical move; do not run a bulk style conversion over the CLI.
- Warnings are errors, nullable and implicit usings stay enabled, versions remain centrally managed, and ordinary SDK compile items are implicit.
- UX impact is **N/A**: no visible text, command syntax, output, help, docs, or interaction is intended to change.

### Library and Framework Requirements

- Stay on the repository-pinned toolchain: .NET SDK `10.0.301`, .NET 10, C# 14/latest, Roslyn Workspaces `5.6.0`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`.
- No package, package version, target-framework, solution, project, tool-manifest, CLI framework, analyzer, or code-generation change is required.
- Microsoft documents `EnableDefaultCompileItems` as `true` by default, so new `.cs` files are compiled implicitly. [MSBuild properties for Microsoft.NET.Sdk](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#default-item-inclusion-properties)
- File-scoped namespaces lower to the equivalent traditional namespace scope; keep using placement semantically equivalent when declarations move. [C# `namespace` reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/namespace)
- `InternalsVisibleTo` grants the named test assembly access to internal types at the assembly boundary; file relocation does not require changing the friend relationship. [Friend assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/friend)

### Testing Requirements

Use a Release-aligned restore because Release builds consume package boundaries and exclude the Debug-only AppHost. Build the CLI test project, then use its xUnit v3 executable for focused classes if VSTest transport is unavailable. FrontComposer broad gates remain solution-level trait-filtered runs.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -parallel none

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --filter "Category=Governance"

BASELINE=6188288a0ccdf3394389019b732d630f25726925
git diff --name-status "$BASELINE"
git ls-files --others --exclude-standard
git diff --submodule=short "$BASELINE" -- references
rg --files -g '*.received.*' -g '!references/**' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

If implementation deliberately rebases the story, replace `BASELINE` with the recalculated commit and update the frontmatter first. The name-status/untracked union is the input to the exact File List and changed-C# byte audit; expected untracked planning artifacts listed under Git Intelligence must be classified but not absorbed.

The new repository guard must carry `[Trait("Category", "Governance")]`. The reflection/JSON contract pin may additionally carry `Contract`. Microsoft documents `Category` as the xUnit filter property, matching the repository lane. [dotnet test filter syntax](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest#filter-option-details)

### Previous-Story Intelligence

- Story 11.16 demonstrated that a nominally behavior-preserving move can drop a null/empty short-circuit. For 11.17a, preserve every declaration/member body verbatim; do not combine movement with cleanup.
- Its review also found an incomplete File List and unrelated gitlink movement. Compare the final diff against the captured baseline, list every path, and independently reject submodule changes.
- Reuse the established non-vacuous governance pattern: source/syntax analysis plus a synthetic forbidden sample. A repository scan alone can pass falsely when its root locator is broken.
- Earlier commit `ff166ac2134b13e839e6e1c9bbab35472ad09019` is the strongest one-type-per-file precedent. It preserved declarations but later exposed stale copied usings; use per-file imports and let the warnings-as-errors build prove them.
- Previous totals (Release 0 warnings/errors, default 4,100/4,100, Governance 277/277) are historical comparison evidence only. New tests legitimately increase totals.

### Git Intelligence

- Baseline is `main`/`origin/main` at `6188288a0ccdf3394389019b732d630f25726925`. The final build topology is Debug/source references with AppHost versus Release/package dependencies without AppHost; use configuration-aligned restore.
- Root-declared submodules were clean at baseline. Preserve their current pins and never initialize nested submodules recursively.
- Shared-workspace rechecks during story creation showed unrelated work already present outside this story/sprint delta: `Directory.Build.targets`; both Story 11.16 artifacts; `docs/diagnostics/{README.md,compatibility-suppressions.json}`; `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`; the Contracts and Shell `CompatibilitySuppressions.xml` files; `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`; `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`; `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`; `tests/eng/test_pack_release_packages.py`; and untracked `_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md`. Do not edit, delete, restore, stage, or include them in this story. Re-run status before implementation and classify any additional drift.
- Follow the repository Git instructions and Conventional Commits if implementation commits are requested. Treat code/diffs as authoritative over commit subjects.

### Scope Never-list

- No SourceTools `DriftDetection.cs` split (11.17b), MCP `SkillCorpus.cs`/benchmark relocation (11.17c), or Shell bundle/Fluxor exception work (11.17d).
- No LoggerMessage migration (11.18), XML-doc/analyzer/NuGet-audit policy work (11.19), package-boundary/Contracts migration (11.11-11.14), or route/layer/helper consolidation.
- No public API baseline fabrication, public visibility change, type rename, namespace/subfolder redesign, partial-type conversion, nested-to-top-level promotion, or new architecture abstraction.
- No behavioral cleanup of inspect/migrate, sidecar reader consolidation, path helper consolidation, generated-artifact naming extraction, new migration edge, serializer/schema change, performance tuning, or accepted-constraint remediation.
- No `.csproj`, `.slnx`, package, README/docs, generated/snapshot, PublicAPI, deferred-work, UX, CI, submodule, or unrelated artifact change is expected.

### Project Structure Notes

Expected production delta: modify `MigrationCommand.cs`, `InspectCommand.cs`, and `CommandOptions.cs`; add the 35 same-named files listed in Tasks 1 and 2 plus `CommandLineException.cs`. Expected test delta: add one architecture/governance test file (or, if implementation evidence proves a second file is necessary to preserve one test type per file, document it explicitly). Expected artifact delta: this story and `sprint-status.yaml` only until implementation updates the Dev Agent Record. No production file deletion is expected.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.17] — decomposition parent, child boundaries, required validation lanes, and mechanical/public-contract criteria.
- [Source: _bmad-output/planning-artifacts/prd.md#Functional-Requirements] — FR25 intentional contract evolution, FR28 authoritative order, and FR29 focused remediation/decomposition.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md#Proposal-G] — one-type-per-file program extraction and independently reviewable story requirement.
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#M14] — original multi-type-file finding and CLI counts.
- [Source: _bmad-output/planning-artifacts/architecture.md] — architecture invariants and canonical project-doc pointer.
- [Source: _bmad-output/project-docs/architecture.md#Layered-structure] — CLI Layer 2 placement, no project references, and no third-party CLI framework.
- [Source: _bmad-output/project-docs/api-contracts.md#CLI-commands] — CLI options, schemas, exit codes, redaction, and write-safety contracts.
- [Source: _bmad-output/project-docs/development-guide.md#Test] — solution-level trait lanes, xUnit v3/Shouldly stack, and actual PublicAPI baseline inventory.
- [Source: _bmad-output/project-docs/source-tree-analysis.md#Hexalith.FrontComposer.Cli] — current flat CLI source layout and test entry points.
- [Source: _bmad-output/project-context.md] — .NET/C#/style/TWAE/test/generated-output/submodule rules.
- [Source: _bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md] — prior implementation/review lessons and validation precedent.
- Code anchors: `src/Hexalith.FrontComposer.Cli/{MigrationCommand,InspectCommand}.cs`; `src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj`; `tests/Hexalith.FrontComposer.Cli.Tests/{MigrationCommandTests,InspectCommandTests,CliHelpTests,ToolPackagingSmokeTests}.cs`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Execute Tasks 1-3 as declaration-preserving file moves, with a focused Roslyn red/green organization pin for each baseline offender.
- Complete Task 4 by generalizing the focused pins into the package-wide non-vacuous Governance guard and adding reflection/exported-surface identity evidence.
- Prove Tasks 5-6 through the complete CLI executable lane, package smoke, Release/default/Governance gates, baseline-aware path/CRLF/artifact audits, and exact story reconciliation.

### Debug Log References

- 2026-07-14 Task 1 RED: `CliTypeOrganizationGovernanceTests.MigrationCommandSource_ContainsOnlyMigrationCommand` failed with the expected 23-name declaration list.
- 2026-07-14 Task 1 GREEN: Release CLI/test build passed with 0 warnings and 0 errors; direct xUnit v3 CLI lane passed 68/68.
- 2026-07-14 Task 1 integrity: all 23 `MigrationCommand.cs` baseline declaration bodies matched their same-named destination files exactly after newline normalization; each destination contains one direct top-level declaration.
- 2026-07-14 Task 2 RED: `CliTypeOrganizationGovernanceTests.InspectCommandSource_ContainsOnlyInspectCommand` failed with the expected 14-name declaration list.
- 2026-07-14 Task 2 GREEN: Release CLI/test build passed with 0 warnings and 0 errors; direct xUnit v3 CLI lane passed 69/69.
- 2026-07-14 Task 2 integrity: all 14 `InspectCommand.cs` baseline declaration bodies matched their same-named destination files exactly after newline normalization; each destination contains one direct top-level declaration.
- 2026-07-14 Task 3 RED: `CliTypeOrganizationGovernanceTests.CommandOptionsSource_ContainsOnlyCommandOptions` failed with `CommandLineException` plus `CommandOptions` in the offender file.
- 2026-07-14 Task 3 GREEN: Release CLI/test build passed with 0 warnings and 0 errors; direct xUnit v3 CLI lane passed 70/70.
- 2026-07-14 Task 3 integrity: both `CommandOptions.cs` baseline declaration bodies matched their same-named destination files exactly after newline normalization.
- 2026-07-14 Task 4 RED: the synthetic class-plus-delegate source produced zero violations against the pre-implementation empty scanner, proving the non-vacuity test failed as intended.
- 2026-07-14 Task 4 GREEN: `CliTypeOrganizationGovernanceTests` passed 4/4 and the complete direct xUnit v3 CLI lane passed 71/71; Release build remained 0 warnings and 0 errors.
- 2026-07-14 Task 5 contracts: direct xUnit v3 CLI lane passed 71/71, including inspect/migrate/help behavior; `ToolPackagingSmokeTests` passed 1/1 and both exact `frontcomposer.cli.*.v1` schema assertions remain live.
- 2026-07-14 Task 5 PublicAPI disposition: no CLI PublicAPI baseline exists before or after this story and none was created; Contracts.UI, Shell FcTbl, and Testing are the only tracked baselines and their baseline diff is empty.
- 2026-07-14 Task 6 artifact-audit hardening: the validator initially misclassified checked-task `.cs` and `Hexalith.FrontComposer.Cli.Tests` tokens as paths. Added a focused RED/GREEN regression and narrowed bare-token path recognition to known file suffixes/dotfiles without weakening slash-qualified path checks; active validator tests passed 10/10.
- 2026-07-14 Task 6 pre-existing Python integration blocker: the complete `eng.tests.test_validate_story_artifacts` module still errors in 2 `ReviewVerifierTests` because current HEAD no longer contains `.agents/skills/bmad-story-automator/src`; the active `StoryArtifactValidatorTests` class and the real story audit are authoritative for this validator change.
- 2026-07-14 Task 6 gates: Release restore and solution build passed with 0 warnings/0 errors; CLI executable 71/71; default solution 4,105/4,105; Governance 282/282; packaging smoke 1/1; active artifact-validator tests 10/10; real story artifact validation passed.
- 2026-07-14 Task 6 ledger: 44 story-owned paths, including exactly 3 modified + 36 new CLI production files and 1 new C# governance test; all 39 target declaration bodies match baseline after newline normalization; all 40 changed/new C# files are CRLF-only; received/generated artifact scan and `git diff --check` are clean.
- 2026-07-14 Task 6 baseline classification: HEAD advanced from `6188288a` to `91873c0c` before implementation. The baseline diff's release/configuration/docs/PublicAPI-adjacent paths and EventStore/Memories/Tenants gitlinks are committed post-baseline drift; the story-owned diff against starting HEAD has no package/project/docs/PublicAPI/generated/snapshot/gitlink change.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Task 1 split the 23-declaration migration bundle into 23 flat same-named files with minimal required imports and no declaration-body change.
- Task 2 split the 14-declaration inspect bundle into 14 flat same-named files with minimal required imports and no declaration-body change.
- Task 3 moved `CommandLineException` verbatim to its same-named file and preserved all parser/exception behavior through the complete CLI suite.
- Task 4 added a no-allowlist Roslyn organization guard, synthetic negative, 39-type internal/top-level identity pin, and exact three-type exported authored-surface pin without new dependencies.
- Task 5 proved inspect/migrate/help/package behavior unchanged and recorded the truthful no-CLI-PublicAPI disposition; existing PublicAPI baselines remain byte-identical.
- Task 6 required a narrow artifact-validator false-positive correction so checked task prose can name a source extension or test assembly without fabricating File List paths; slash-qualified and real filename evidence remains fail-closed.
- Task 6 reconciled exact file/declaration counts and passed every required Release, focused, default, Governance, packaging, artifact, CRLF, submodule, and diff-integrity gate.

### File List

- `_bmad-output/implementation-artifacts/11-17-cli-package-split.md` (new story context)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (11.17a development status tracking; siblings remain backlog)
- `eng/tests/test_validate_story_artifacts.py` (modified, regression for extension/assembly-name task tokens)
- `eng/validate-story-artifacts.py` (modified, narrow task-path false-positive fix required by the story audit)
- `src/Hexalith.FrontComposer.Cli/CommandLineException.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/CommandOptions.cs` (modified, retained eponymous declaration)
- `src/Hexalith.FrontComposer.Cli/FrontComposerMigrationCodeFixProvider.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/DiagnosticFileReader.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/FrameworkSelection.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/GeneratedFileClassifier.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/GeneratedFileInfo.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/GeneratedOutputLoader.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/GeneratedSourceFamily.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` (modified, retained eponymous declaration)
- `src/Hexalith.FrontComposer.Cli/InspectDiagnostic.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/InspectJson.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/InspectLoadResult.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/InspectReport.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/InspectSummary.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationApplier.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` (modified, retained eponymous declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationDiagnosticScanner.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationDiagnosticSidecarReader.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationDiagnostics.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationEdge.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationEntry.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationJson.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationPlan.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationResult.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/MigrationSummary.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/PlannedFileEdit.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/ProjectDocument.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/ProjectDocumentSet.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/SourceFile.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/SourceFileContent.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/SubmoduleBoundaryReader.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/TypeMatchResult.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/TypeMatcher.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/UnifiedDiff.cs` (new, moved declaration)
- `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs` (new, moved declaration)
- `tests/Hexalith.FrontComposer.Cli.Tests/Architecture/CliTypeOrganizationGovernanceTests.cs` (new, Task 1 red/green organization pin; expanded in Task 4)

## Change Log

- 2026-07-14: Created Story 11.17a from the mandatory 11.17 package/defect-class decomposition; resolved the nonexistent CLI PublicAPI baseline wording with internal type-shape plus shipped tool-contract evidence.
- 2026-07-14: Split all 39 CLI offender declarations into same-named files (3 modified + 36 new production files), added non-vacuous organization/identity governance, preserved shipped CLI contracts, and passed Release/default/Governance/package/artifact integrity gates.
- 2026-07-14: Completed the chunked migration-split code review; removed one redundant split-time import, deferred twelve pre-existing migration behaviors, dismissed seven non-findings, and revalidated the CLI test project Release build with 0 warnings and 0 errors.
