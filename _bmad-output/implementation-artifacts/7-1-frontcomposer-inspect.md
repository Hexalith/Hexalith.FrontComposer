# Story 7.1: frontcomposer inspect

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-05. -->
<!-- Senior Developer Review (AI) completed against .agents/skills/bmad-story-automator-review/checklist.md on 2026-06-05. -->

## Story

As an adopter developer,
I want to inspect generated output and diagnostics from the CLI,
so that I can verify what the generator produced without opening `obj/`.

## Acceptance Criteria

1. Given generated files plus `*.diagnostics.json` sidecars under `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`, when I run `frontcomposer inspect [--build] [--format json]`, then the command reports `generatedFiles`, `forms`, `grids`, `registrations`, `mcpManifestEntries`, `warnings`, and `errors` using JSON schema `frontcomposer.cli.inspect.v1`. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.1-frontcomposer-inspect]
2. Given `--fail-on-warning` or `--fail-on-error`, when matching diagnostics exist after any `--severity` / `--type` filtering, then the command returns `ExitCodes.ActionableFindings` (`1`); unavailable generated output returns `ExitCodes.GeneratedOutputUnavailable` (`3`). [Source: _bmad-output/planning-artifacts/epics.md#Story-7.1-frontcomposer-inspect]
3. Given `--build`, when the selected project builds successfully, then inspect reads the generated output from the public `GeneratedOutputPathContract` shape for the selected configuration/framework; if the build fails or generated output is still unavailable, it fails with sanitized guidance and exit code `3`. [Source: _bmad-output/project-docs/api-contracts.md#3.1-frontcomposer-inspect; _bmad-output/project-context.md#Source-Generator-Rules]
4. Given diagnostic sidecars, when sidecars are valid, malformed, missing optional fields, or contain hostile paths, then inspect reports only HFC diagnostics, emits a deterministic warning sentinel for unreadable/malformed sidecars, and never leaks absolute/out-of-project paths or raw control characters. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs; tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
5. Given the Epic 6 diagnostic-phase findings, when inspect reports diagnostics, then it does not claim cataloged Level 3/4 customization IDs are build-emitted unless the build output actually contains sidecar/build evidence. HFC1050-HFC1055 can be treated as build-time SourceTools warnings; HFC1038-HFC1045 must preserve the current call-site/startup/runtime or reserved distinction. [Source: _bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md#Next-Epic-Preparation-Epic-7]

## Tasks / Subtasks

- [x] Confirm and document the FC-CLI-INSPECT v1 contract (AC: 1, 2, 3, 4, 5)
  - [x] Create `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`.
  - [x] Record the JSON schema name, summary fields, generated-file family mapping, diagnostic-sidecar handling, exit codes, path-redaction rules, and diagnostic phase/disposition boundary.
  - [x] Cite live source and tests; do not invent behavior that is not implemented or pinned.
- [x] Audit the existing inspect implementation before changing it (AC: 1, 2, 3, 4)
  - [x] Read `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` completely.
  - [x] Read `src/Hexalith.FrontComposer.Cli/CliApplication.cs`, `CommandOptions.cs`, `ProjectSelection.cs`, `PathUtilities.cs`, `OutputSanitizer.cs`, `JsonOptions.cs`, and `ExitCodes.cs`.
  - [x] Read `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`, `CliFixture.cs`, `CliHelpTests.cs`, and `OutputSanitizerTests.cs`.
  - [x] Compare help text and README against `_bmad-output/project-docs/api-contracts.md` section 3.1. Current help advertises `--summary`, while the API contract does not; reconcile this by removing the unsupported promise or by explicitly implementing and documenting it if Product decides it is required.
- [x] Pin inspect JSON and text output behavior (AC: 1)
  - [x] Keep `schemaVersion = "frontcomposer.cli.inspect.v1"`.
  - [x] Ensure JSON and text use the same deterministic file ordering: related type, family, relative path.
  - [x] Confirm family classification for projection grids, command forms, command renderers, command pages, Fluxor artifacts, registrations, `FrontComposerMcpManifest.g.cs`, and `__FrontComposerProjectionTemplatesRegistration.g.cs`.
  - [x] Resolve the `mcpManifestEntries` semantic explicitly: if v1 means manifest-file count, record that in the FC-CLI-INSPECT contract; if v1 means command/resource descriptor count inside `FrontComposerMcpManifest.g.cs`, parse and test that count instead of reporting only the generated manifest file.
  - [x] Add focused tests for template manifest counting/reporting if not already present.
- [x] Pin diagnostic handling and fail flags (AC: 2, 4, 5)
  - [x] Add tests proving `--fail-on-warning` returns `1` for warning and error diagnostics, while `--fail-on-error` returns `1` only for errors.
  - [x] Add tests proving fail flags are evaluated after `--severity` and `--type` filtering.
  - [x] Add tests for missing optional sidecar fields, non-HFC diagnostics being ignored, malformed sidecars producing `HFCM0002`, and hostile sidecar paths being redacted or converted to safe project-relative output.
  - [x] Preserve `OutputSanitizer` on every user-visible value in text, JSON, and error output.
- [x] Pin `--build` behavior without adding a CLI framework or package dependency (AC: 3)
  - [x] Verify `RunBuildAsync` passes `-p:EmitCompilerGeneratedFiles=true`.
  - [x] Verify `CompilerGeneratedFilesOutputPath` resolves to `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/` for explicit and inferred frameworks.
  - [x] Add a narrow test seam only if needed to avoid brittle process tests; do not add System.CommandLine or broaden Roslyn package pins.
  - [x] Ensure failed build output is bounded/sanitized and returns exit code `3`, not `4`.
- [x] Preserve project selection and path safety (AC: 1, 3, 4)
  - [x] Keep `.csproj` as the required explicit project shape.
  - [x] Keep `.slnx` and `.fsproj` fail-closed for CLI v1.
  - [x] Keep ambiguous multi-TFM generated output requiring `--framework`.
  - [x] Keep `--absolute-paths` opt-in; default output must be project-relative and must not expose temporary fixture roots or host paths.
- [x] Update user-facing CLI docs only where behavior is verified (AC: 1, 2, 3, 4)
  - [x] Update `src/Hexalith.FrontComposer.Cli/README.md` if the inspect contract or help text changes.
  - [x] Do not use published `docs/` as scratch space; edit DocFX docs only when the story-owned contract requires it and run the docs validation lane if touched.
- [x] Verify and record evidence (AC: 1, 2, 3, 4, 5)
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run focused CLI tests, at minimum `tests/Hexalith.FrontComposer.Cli.Tests`.
  - [x] If exact solution-level VSTest is socket-blocked in this environment, record that honestly and provide focused in-process or project-lane evidence.
  - [x] Create or update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the Story 7.1 result.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: `frontcomposer inspect` is already implemented in `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`, dispatched by `CliApplication.RunAsync`, documented in `src/Hexalith.FrontComposer.Cli/README.md`, and covered by existing CLI tests. This story is primarily confirm-and-pin plus gap closure, not a greenfield CLI build. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
- The CLI has no third-party command framework by architecture decision. Continue using `CommandOptions`, `ProjectSelection`, `PathUtilities`, and `OutputSanitizer`; do not introduce System.CommandLine or another parser. [Source: _bmad-output/project-docs/architecture.md#7-Architecturally-significant-decisions-observed]
- The CLI project targets `net10.0`, is packaged as a dotnet tool, and references Roslyn Workspaces packages through central package management. Do not add `Version=` to the project file and do not change Roslyn pins for this story. [Source: src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj; Directory.Packages.props; _bmad-output/project-context.md#Technology-Stack-and-Versions]
- Generated output path is a public contract: `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`. Prefer the existing contract model and tests over ad hoc path literals where production code can reference it without creating an invalid dependency direction. [Source: src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs; _bmad-output/project-docs/data-models.md]
- `GeneratedOutputLoader` currently classifies top-level generated `.g.cs` / `.g.razor.cs` files, reads top-level `*.diagnostics.json`, and emits `HFCM0002` for malformed sidecars. Treat this as the surface to harden and pin. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs]
- The current `InspectSummary.McpManifestEntries` appears to count generated files classified as `McpManifest`, not command/resource descriptors inside the manifest. Do not let the story complete with this ambiguity: either document file-count semantics as the v1 contract or implement descriptor-count semantics with tests. [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; _bmad-output/project-docs/api-contracts.md#3.1-frontcomposer-inspect]
- Existing tests already pin JSON schema, generated-file counts, deterministic generated-file ordering, type filtering/suggestions, ambiguous frameworks, missing generated output, severity filtering, framework path traversal rejection, malformed sidecar sentinel, and project-directory rejection. Add tests around the unpinned gaps rather than duplicating these checks. [Source: tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]
- Epic 6 learning: diagnostic tooling must preserve phase/disposition. `inspect` must not flatten catalog IDs into "build diagnostics" unless emitted build/sidecar evidence exists. HFC1050-HFC1055 are build-time analyzer warnings; HFC1038-HFC1045 currently include call-site/startup/runtime or reserved behavior. [Source: _bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md]
- `frontcomposer migrate` has its own sidecar reader and migration-specific path rules. Reuse patterns if helpful, but do not rewrite migrate or change `frontcomposer.cli.migrate.v1` unless a direct inspect fix requires a shared helper. [Source: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; src/Hexalith.FrontComposer.Cli/README.md]
- No external dependency research is needed for this story: the relevant SDK, Roslyn, STJ, and test-library versions are already pinned in repository configuration. Do not upgrade packages as part of Story 7.1. [Source: global.json; Directory.Packages.props]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
  - `src/Hexalith.FrontComposer.Cli/CliApplication.cs` only if help text or dispatch behavior changes
  - `src/Hexalith.FrontComposer.Cli/README.md` only for verified CLI contract/doc alignment
  - `src/Hexalith.FrontComposer.Cli/PathUtilities.cs` / `OutputSanitizer.cs` only if shared path/sanitization hardening is required
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`
  - `tests/Hexalith.FrontComposer.Cli.Tests/CliFixture.cs` if a better fixture is required for `--build` or hostile sidecar paths
  - `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` if help text is reconciled
- Expected BMAD artifacts:
  - `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Detected conflict: `_bmad-output/story-automator/orchestration-1-20260604-140358.md` is currently modified and unrelated. Do not revert or include it in the Story 7.1 File List unless the dev agent intentionally changes it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-7.1-frontcomposer-inspect]
- [Source: _bmad-output/project-docs/api-contracts.md#3.1-frontcomposer-inspect]
- [Source: _bmad-output/project-docs/architecture.md#Runtime-composition-Shell]
- [Source: _bmad-output/project-docs/source-tree-analysis.md#Hexalith.FrontComposer.Cli]
- [Source: _bmad-output/project-docs/component-inventory.md#E-CLI-surface]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md#Next-Epic-Preparation-Epic-7]
- [Source: src/Hexalith.FrontComposer.Cli/InspectCommand.cs]
- [Source: tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Baseline commit captured: `d35f34534b8b43e1c7a9f7f7a8d7774a3a809922`.
- 2026-06-05: Audited `InspectCommand.cs`, CLI option/selection/path/sanitizer/JSON/exit-code support files, and existing CLI tests before implementation.
- 2026-06-05: Removed unsupported `inspect --summary` promise from global help; README had no `--summary` reference.
- 2026-06-05: Added FC-CLI-INSPECT v1 contract artifact and README inspect-output notes, documenting `mcpManifestEntries` as generated manifest file count.
- 2026-06-05: Hardened inspect sidecar path normalization for project-relative sidecar paths, URI/drive hostile paths, invalid paths, and unreadable sidecars.
- 2026-06-05: Added narrow `CreateBuildArguments` seam to pin `--build` MSBuild properties without brittle process tests.
- 2026-06-05: Validation: Release solution build passed 0 warnings / 0 errors.
- 2026-06-05: Validation: focused xUnit v3 in-process inspect/help lane passed 15/15.
- 2026-06-05: Validation caveat: exact project and solution VSTest lanes abort before execution with `System.Net.Sockets.SocketException (13): Permission denied`; CI remains authoritative.
- 2026-06-05: Full CLI in-process assembly ran 46 tests with 43 passing and 3 non-story local failures: read-only `$HOME/.dotnet/toolResolverCache` for tool smoke and two existing migration solution-selection failures.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- FC-CLI-INSPECT v1 contract created with live source/test citations and no invented behavior.
- `schemaVersion = "frontcomposer.cli.inspect.v1"` retained; JSON/text generated-file ordering remains shared through load-order tri-key sorting.
- `mcpManifestEntries` resolved as v1 generated manifest file count, not descriptor count inside `FrontComposerMcpManifest.g.cs`.
- Diagnostic sidecar behavior pinned for missing optional fields, non-HFC filtering, malformed `HFCM0002`, hostile path redaction, and fail flags after filtering.
- `--build` keeps `EmitCompilerGeneratedFiles=true` and generated-output path shape without adding a CLI framework, package dependency, or Roslyn version change.
- Published DocFX `docs/` were not touched.

### File List

- `_bmad-output/implementation-artifacts/7-1-frontcomposer-inspect.md`
- `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Cli/CliApplication.cs`
- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- `src/Hexalith.FrontComposer.Cli/README.md`
- `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

### Change Log

- 2026-06-05: Implemented Story 7.1 confirm-and-pin work for `frontcomposer inspect`; added FC-CLI-INSPECT v1 contract, focused inspect/help tests, sidecar path hardening, help/README alignment, and validation evidence. Status moved to review.
- 2026-06-05: Senior Developer Review (AI) completed. All 5 ACs verified against implementation; all tasks confirmed done; File List reconciled against git with no discrepancies. Closed one LOW finding (text/JSON summary parity: text summary line now reports `Warnings`/`Errors` totals) with a pinning test. Build 0/0; focused inspect/help lane 17/17 green; full CLI in-process assembly 46/48 with the only 2 failures being pre-existing migration solution-selection tests outside the inspect surface. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated adversarial review) on 2026-06-05
**Outcome:** Approve — Status → done (0 Critical findings)

### Scope reviewed

Story File List vs git reality (exact match, excluding the intentionally-excluded
`_bmad-output/story-automator/orchestration-1-20260604-140358.md`); all 5 Acceptance Criteria; all
Tasks/Subtasks completion claims; changed source and tests:
`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`, `CliApplication.cs`, `README.md`,
`tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`, `CliHelpTests.cs`, plus the
FC-CLI-INSPECT v1 contract artifact.

### Acceptance Criteria

- **AC1 (JSON schema + summary fields):** IMPLEMENTED. `InspectJson.From` emits
  `schemaVersion = "frontcomposer.cli.inspect.v1"` with `summary.{generatedFiles, forms, grids,
  registrations, mcpManifestEntries, warnings, errors}`. Verified by
  `InspectJson_ReportsGeneratedFilesWithDeterministicRelativePaths` and
  `InspectJson_ClassifiesGeneratedFamiliesAndCountsMcpManifestFiles`.
- **AC2 (fail flags + exit codes after filtering):** IMPLEMENTED. `--fail-on-warning`/`--fail-on-error`
  return `ExitCodes.ActionableFindings` (1); unavailable output returns `GeneratedOutputUnavailable`
  (3); evaluation occurs after `--severity`/`--type` filtering. Verified by
  `InspectFailFlags_UseExpectedSeverityThresholds`,
  `InspectFailOnError_DoesNotFailForWarningOnlyDiagnostics`, and
  `InspectFailFlags_AreEvaluatedAfterSeverityAndTypeFiltering`.
- **AC3 (`--build` generated-output path, exit 3 on failure):** IMPLEMENTED.
  `CreateBuildArguments` pins `-p:EmitCompilerGeneratedFiles=true` and
  `-p:CompilerGeneratedFilesOutputPath=obj/{Config}/{TFM}/generated/HexalithFrontComposer`; failed
  build returns 3 with bounded/sanitized guidance. Verified by
  `InspectBuildArguments_UsePublicGeneratedOutputPathContractShape`.
- **AC4 (sidecar handling + redaction):** IMPLEMENTED. Only `HFC*` IDs reported; `HFCM0002` sentinel
  for malformed/unreadable sidecars; drive-letter/URI/traversal/out-of-project paths redacted to
  `[redacted-path]`; control characters escaped. Verified by
  `InspectDiagnosticsSidecars_DefaultMissingOptionalFieldsIgnoreNonHfcAndRedactHostilePaths` and
  `Inspect_EmitsSentinelForMalformedDiagnosticsSidecars`.
- **AC5 (diagnostic phase honesty):** IMPLEMENTED. `inspect` reports only diagnostics actually present
  in sidecars; it never injects cataloged Level 3/4 IDs as build-emitted. The HFC1050-1055 vs
  HFC1038-1045 phase/disposition boundary is documented in the FC-CLI-INSPECT v1 contract.

### Findings

- **[LOW — fixed] Text/JSON summary parity.** Default format is `text`, but the text summary line
  omitted the `warnings`/`errors` totals that AC1 enumerates and that JSON reports. Fixed in
  `InspectCommand.RenderText` (added `Warnings`/`Errors` to the summary line) and pinned by new test
  `InspectText_SummaryLineReportsWarningAndErrorTotals`.
- **[LOW — accepted by design] Classifier suffix heuristic.** `GeneratedFileClassifier` could
  misclassify a projection razor whose related type name ends in `Page`/`Renderer`/`Feature`/etc.
  Documented as a v1 heuristic in the contract; generator naming makes collisions unlikely. No change.
- **[LOW — accepted by design] `grids` = all `ProjectionRazor` files.** Could overcount if a projection
  emits multiple razor files. Documented in the contract family table. No change.

### Verification evidence

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/...csproj -c Release`: 0 warnings / 0 errors.
- Focused in-process lane (`InspectCommandTests` + `CliHelpTests`): 17/17 passed.
- Full CLI in-process assembly: 48 total, 46 passed, 2 failed. Both failures
  (`MigrationCommandTests.ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically`,
  `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`) are pre-existing solution-selection
  tests last committed in Story 11.3 (commit `9530136`), are unchanged by this story, and are outside
  the inspect surface. Exact `dotnet test`/VSTest lanes remain socket-blocked locally
  (`SocketException (13): Permission denied`); CI is authoritative.
