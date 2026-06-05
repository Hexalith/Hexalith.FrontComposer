---
baseline_commit: 395d747dca48485487ab41b56ae69672238cc50b
---

# Story 7.3: Surface the HFC diagnostic catalog

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-05. -->

## Story

As an adopter developer,
I want generator diagnostics surfaced consistently at build and via inspect,
so that I can act on annotation and usage problems.

## Acceptance Criteria

1. Given any active or reserved HFC1001-HFC1070 diagnostic row, when the diagnostic is emitted by SourceTools under `TreatWarningsAsErrors=true`, then the Roslyn descriptor severity, `AnalyzerReleases.Unshipped.md` severity, `docs/diagnostics/diagnostic-registry.json` `compilerSeverity`, and generated docs stub severity agree; `Error` diagnostics fail the build and `Warning` diagnostics are build warnings that TWAE escalates in normal builds. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.3-Surface-the-HFC-diagnostic-catalog; _bmad-output/project-docs/api-contracts.md#1.5-Diagnostic-catalog-HFC1001-HFC1070; docs/diagnostics/README.md#Runtime-only-vs-release-tracked-diagnostics]
2. Given `frontcomposer inspect --severity <hidden|info|warning|error>`, when generated diagnostic sidecars contain mixed severities, then inspect reports diagnostics at or above the requested level using threshold semantics: hidden includes all, info includes Info/Warning/Error, warning includes Warning/Error, and error includes Error only. Invalid severities still return `ExitCodes.InvalidArguments` (`2`). [Source: _bmad-output/planning-artifacts/epics.md#Story-7.3-Surface-the-HFC-diagnostic-catalog; _bmad-output/project-docs/api-contracts.md#3.1-frontcomposer-inspect; src/Hexalith.FrontComposer.Cli/InspectCommand.cs]
3. Given inspect sidecars from generated output, when diagnostics are read, then `HFC*` entries retain sanitized `id`, `severity`, `relatedType`, `path`, `what`, `expected`, `got`, `fix`, and `docsLink` fields, non-HFC compiler diagnostics are ignored, malformed sidecars emit deterministic warning sentinel `HFCM0002`, and no absolute host paths leak. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs; _bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md]
4. Given the catalog includes diagnostics whose current phase is reserved, runtime/startup, or not yet production-emitted, when Story 7.3 records the catalog, then it does not falsely claim build-time SourceTools emission for those diagnostics. At minimum preserve Epic 6 decisions for HFC1038-HFC1046 and Story 7.2's caveat that `HFCM9002` sidecar evidence is synthetic until production sidecar emission is owned. [Source: _bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md; _bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md; _bmad-output/implementation-artifacts/7-2-frontcomposer-migrate.md]
5. Given catalog governance exists under `docs/diagnostics`, when Story 7.3 is complete, then it produces `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md` documenting the v1 diagnostic-catalog contract, authoritative sources, severity rules, lifecycle/phase distinctions, inspect filtering semantics, and focused verification evidence. [Source: docs/diagnostics/README.md; docs/diagnostics/diagnostic-registry.json; tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs]

## Tasks / Subtasks

- [x] Audit and document the current HFC catalog authority chain (AC: 1, 4, 5)
  - [x] Read `docs/diagnostics/README.md` and `docs/diagnostics/diagnostic-registry.json`.
  - [x] Read `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`.
  - [x] Compare registry rows, descriptors, release rows, and docs stubs for HFC1001-HFC1070; do not hand-maintain a second catalog table in production code.
- [x] Produce the FC diagnostics catalog contract artifact (AC: 1, 4, 5)
  - [x] Create `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md`.
  - [x] Record `docs/diagnostics/diagnostic-registry.json` as the registry/governance source and `DiagnosticDescriptors` as the Roslyn emission source.
  - [x] Record lifecycle meanings (`active`, `reserved`, `deprecated`, `retired`, `removed-in-major`) and channel meanings (`compilerSeverity`, `runtimeLogLevel`, `panelSeverity`, `cliExitBehavior`).
  - [x] Record known phase caveats for HFC1038-HFC1046 and HFCM9002; do not upgrade docs prose to claim behavior not proven by source/tests.
- [x] Pin build-time severity consistency (AC: 1, 4)
  - [x] Extend `DiagnosticDescriptorTests` or `DiagnosticRegistryTests` so all SourceTools HFC1001-HFC1070 descriptors are covered, not just the current subset.
  - [x] Ensure each descriptor's `DefaultSeverity`, `Category`, `HelpLinkUri`, and title match the registry and analyzer release row.
  - [x] Ensure runtime-only or reserved/no-call-site diagnostics remain represented honestly; a descriptor may exist for a reserved ID, but the story must not invent a parser/emitter path.
  - [x] Add focused emission/build pins only for diagnostics with existing source paths or narrow Story 7.3-owned gaps such as HFC1056/HFC1057; avoid implementing drift-detection behavior reserved for Story 7.4.
- [x] Fix and pin `inspect --severity` threshold semantics (AC: 2, 3)
  - [x] Update `InspectCommand` so `--severity warning` includes Warning and Error, `--severity info` includes Info, Warning, and Error, `--severity hidden` includes Hidden, Info, Warning, and Error, and `--severity error` includes Error only.
  - [x] Preserve invalid-severity validation and `--fail-on-warning` / `--fail-on-error` evaluation after severity and type filtering.
  - [x] Add or update `InspectCommandTests` to cover mixed Hidden/Info/Warning/Error sidecars in JSON and text paths where useful.
  - [x] Keep sidecar sanitization and `HFCM0002` malformed-sidecar behavior unchanged.
- [x] Verify inspect output remains contract-compatible (AC: 2, 3)
  - [x] Confirm JSON schema name remains `frontcomposer.cli.inspect.v1`.
  - [x] Confirm text and JSON warning/error summary counts are calculated after severity/type filtering.
  - [x] Confirm absolute paths, URI paths, drive-qualified paths, and control characters remain sanitized.
  - [x] Confirm non-HFC diagnostics in sidecars are ignored.
- [x] Update documentation only where it is the owned source of truth (AC: 1, 4, 5)
  - [x] Update `src/Hexalith.FrontComposer.Cli/README.md` only if inspect severity semantics or sidecar fields are stale.
  - [x] Update `_bmad-output/project-docs/api-contracts.md` only if the story owns BMAD generated docs reconciliation; do not use `docs/` as scratch space.
  - [x] Do not edit generated `docs/diagnostics/HFC*.md` prose manually unless a registry/stub governance test requires a precise metadata correction.
- [x] Verify and record evidence (AC: 1, 2, 3, 4, 5)
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run focused SourceTools diagnostics governance tests.
  - [x] Run focused CLI inspect tests.
  - [x] Run the broader CLI test assembly or direct xUnit v3 in-process fallback and account for every failure by name.
  - [x] Create or update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the Story 7.3 result.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: most catalog infrastructure already exists. The authoritative governance surface is `docs/diagnostics/diagnostic-registry.json`, documented by `docs/diagnostics/README.md` and enforced by `DiagnosticRegistryTests`. Reuse that; do not create a second production diagnostic registry for inspect or SourceTools. [Source: docs/diagnostics/README.md; tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs]
- HFC symbolic constants live in `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`; SourceTools Roslyn descriptors live in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`; release tracking lives in `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`. New or corrected IDs must keep all three plus registry/docs stubs aligned. [Source: _bmad-output/project-context.md#Source-Generator-Rules; _bmad-output/project-docs/api-contracts.md#1.5-Diagnostic-catalog-HFC1001-HFC1070]
- Current `InspectCommand` exact-matches severity after normalization. That conflicts with Story 7.3's "at/above the level" wording. Treat this as an expected Story 7.3 fix and pin it with mixed severity sidecars. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
- `inspect --severity` is applied before `--type` today, and fail flags are evaluated after both filters. Preserve the effective "fail only on visible diagnostics" behavior, but the threshold change will alter what remains visible for `--severity warning` and `--severity info`. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
- `DiagnosticFileReader` currently reads top-level `*.diagnostics.json` sidecars from the generated-output directory, accepts either an array or `{ "diagnostics": [] }`, ignores non-HFC IDs, sanitizes every user-visible field, normalizes hostile paths to `[redacted-path]`, and emits `HFCM0002` when a sidecar is unreadable or malformed. Preserve these guarantees. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs]
- Do not claim all HFC1001-HFC1070 IDs have proven build-time emitters. Epic 6 established that HFC1038-HFC1041 are call-site/startup/runtime dispositions today; HFC1042/HFC1046 are reserved/catalog or adjacent analyzer scope; HFC1043-HFC1045 are registry/startup-runtime. Record this honestly in the contract artifact. [Source: _bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md; _bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md]
- Story 7.4 owns opt-in drift detection behavior vs baselines. Story 7.3 may verify descriptor/catalog consistency for HFC1058-HFC1070 and avoid regressions, but should not rebuild the drift pipeline or change `CanonicalSchemaMaterial`, baseline algorithms, or `CompilationProvider` isolation. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.4-Opt-in-drift-detection-vs-a-baseline; _bmad-output/project-context.md#Schema-Fingerprint-Integrity-Rules]
- HFC1056/HFC1057 already have parser tests under `CommandParserTests`, descriptors, constants, and registry/release rows. They are narrow candidates for Story 7.3 focused build/descriptor verification because the analyzer release rows label them Story 7-3. [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs; src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md]
- No external dependency research is needed for this story. Relevant versions are pinned locally: .NET SDK `10.0.300`, Roslyn `5.3.0`, System.Text.Json `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`. Do not change package versions or add a CLI framework. [Source: global.json; Directory.Packages.props; _bmad-output/project-context.md#Technology-Stack-and-Versions]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
  - `src/Hexalith.FrontComposer.Cli/README.md` only if inspect help/docs need verified wording updates
  - `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` only if descriptor/registry drift is found
  - `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` only if constants drift is found
  - `docs/diagnostics/diagnostic-registry.json` only if governance tests prove metadata drift
  - `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` only if release-row drift is found
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticDescriptorTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` only for narrow HFC1056/HFC1057 emission pins
- Expected BMAD artifacts:
  - `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Detected unrelated dirty file before Story 7.3 creation: `_bmad-output/story-automator/orchestration-1-20260604-140358.md`. Do not revert it or include it in the Story 7.3 File List unless the dev agent intentionally changes it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-7.3-Surface-the-HFC-diagnostic-catalog]
- [Source: _bmad-output/project-docs/api-contracts.md#1.5-Diagnostic-catalog-HFC1001-HFC1070]
- [Source: _bmad-output/project-docs/api-contracts.md#3.1-frontcomposer-inspect]
- [Source: _bmad-output/project-docs/architecture.md#3-The-generation-pipeline-Layer-1-detail]
- [Source: _bmad-output/project-context.md]
- [Source: docs/diagnostics/README.md]
- [Source: docs/diagnostics/diagnostic-registry.json]
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md]
- [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs]
- [Source: tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs]
- [Source: _bmad-output/implementation-artifacts/7-2-frontcomposer-migrate.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release -m:1 /nr:false --no-restore` - passed 0 warnings / 0 errors.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false --no-restore` - passed 0 warnings / 0 errors.
- `./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` - passed 18/18.
- `./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -method Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests.SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned` - passed 1/1.
- `./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticDescriptorTests -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticCatalogTests` - passed 24/24.
- Focused HFC1056/HFC1057 parser lane via direct xUnit v3 in-process runner - passed 7/7.
- `./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` with `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home` - passed 60/60.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true` - passed 0 warnings / 0 errors.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false --no-restore` - failed in CLI restore with `NU1301` because `api.nuget.org:443` is blocked in this sandbox.
- Full CLI in-process assembly without exclusions - 60/61 passed; `ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest` failed environmentally (read-only dotnet tool cache, then blocked NuGet after `DOTNET_CLI_HOME` moved to `/tmp`).
- Broad `DiagnosticRegistryTests` class - 114/115 passed; pre-existing failure `Story112_LedgerRowsMapToOneOfThreeFinalStates` because `deferred-work.md` is missing.
- Configured solution-level `dotnet test` - aborted locally because VSTest cannot create its TCP listener (`SocketException (13): Permission denied`).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented `frontcomposer inspect --severity` threshold filtering while preserving invalid argument handling and fail flags after severity/type filtering.
- Added mixed-severity JSON/text inspect tests, invalid severity coverage, and updated fail-flag expectations for threshold semantics.
- Added registry-derived HFC1001-HFC1070 severity parity governance across registry, Roslyn descriptors, AnalyzerReleases, and docs stubs.
- Created the FC diagnostics catalog contract artifact with authority-chain, lifecycle/channel, inspect-filtering, HFC1038-HFC1046, and HFCM9002 caveats.
- Updated the CLI README to document threshold severity semantics.
- Verification evidence recorded in `_bmad-output/implementation-artifacts/tests/test-summary.md`; local VSTest and packaging-smoke limitations are environmental and named above.
- Added Playwright E2E coverage `tests/e2e/specs/diagnostic-catalog-inspect.spec.ts` (severity-threshold filtering in JSON/text, invalid-severity rejection, malformed-sidecar `HFCM0002` without path leaks) and the `test:fc-diagnostics` script in `tests/e2e/package.json`.
- Review auto-fix: `--severity hidden` now includes non-canonical severities so it remains the true "include all" level (AC2), pinned by `InspectSeverity_Hidden_IncludesNonCanonicalSeverities`; CLI inspect lane is 19/19.

### File List

- `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/7-3-surface-the-hfc-diagnostic-catalog.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- `src/Hexalith.FrontComposer.Cli/README.md`
- `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/diagnostic-catalog-inspect.spec.ts`

### Change Log

- 2026-06-05 - Implemented Story 7.3 diagnostic catalog contract, inspect severity threshold semantics, governance/test pins, CLI docs update, and verification record.
- 2026-06-05 - Senior Developer Review (AI): auto-fixed 1 MEDIUM AC2 gap — `--severity hidden` now truly includes all diagnostics, including sidecar entries with non-canonical severities, pinned by `InspectSeverity_Hidden_IncludesNonCanonicalSeverities`. Reconciled File List with two previously undocumented e2e files. No CRITICAL/HIGH findings; status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-05 · **Outcome:** Approve (auto-fix applied)

### Verification performed

- Built `Hexalith.FrontComposer.SourceTools.Tests` and `Hexalith.FrontComposer.Cli.Tests` in Release: 0 warnings / 0 errors each.
- `DiagnosticRegistryTests.SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned` — 1/1 green (AC1/AC4/AC5 governance parity across registry, descriptors, AnalyzerReleases, docs stubs).
- `InspectCommandTests` full class — 19/19 green after fix (AC2/AC3 inspect filtering, sanitization, `HFCM0002`).
- Confirmed AC5 artifact `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md` exists; `test-summary.md` records the 7.3 result.
- Cross-referenced git working tree against the File List.

### Findings and disposition

- **MEDIUM (AC2 fidelity, fixed):** `InspectCommand.RunAsync` filtered with `SeverityRank(x.Severity) >= minimumSeverity`. For `--severity hidden` (rank 0) this dropped any diagnostic whose severity was non-canonical (rank `-1`, e.g. a malformed sidecar carrying `"severity":"Critical"`), so `hidden` was not the "include all" level AC2 mandates and was not a superset of the unfiltered output. Fixed: `hidden` now includes everything; info/warning/error keep strict threshold semantics. Pinned by new test `InspectSeverity_Hidden_IncludesNonCanonicalSeverities`.
- **MEDIUM (documentation, fixed):** File List omitted `tests/e2e/specs/diagnostic-catalog-inspect.spec.ts` (new Playwright spec) and `tests/e2e/package.json` (added `test:fc-diagnostics` script). Both added to the File List.
- **LOW (documentation, fixed):** Completion Notes / Change Log did not record the e2e regression spec; noted below.
- **No CRITICAL/HIGH:** every `[x]` task was verified against source/tests; all 5 ACs are implemented; no false build-time emission claims (HFC1038-HFC1046 / HFCM9002 caveats preserved in the contract).
