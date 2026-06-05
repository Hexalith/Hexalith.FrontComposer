# Test Automation Summary

## Story 7.1 - frontcomposer inspect

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - added focused inspect pins for generated-file family mapping, v1 MCP manifest file-count semantics, build arguments, fail flags after filtering, warning-only `--fail-on-error` behavior, sidecar optional fields, non-HFC filtering, malformed sidecar sentinels, and hostile path redaction.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` - added a help-text pin proving unsupported `--summary` is no longer advertised.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - (Senior Developer Review, AI) added `InspectText_SummaryLineReportsWarningAndErrorTotals` pinning text/JSON summary parity for `Warnings`/`Errors` totals in default text output.

### API Tests
- [x] Not applicable - Story 7.1 has no HTTP API endpoint surface.

### E2E Tests
- [x] CLI E2E-style coverage uses the existing in-process `CliApplication.RunAsync` pattern for the `frontcomposer inspect` user workflow.
- [x] Browser E2E tests are not applicable - Story 7.1 is a CLI inspection feature with no browser-visible UI.

### Coverage
- CLI inspect JSON schema and summary: covered.
- Generated-file family mapping: projection grids, command forms, command renderers, command pages, Fluxor artifacts, registrations, `FrontComposerMcpManifest.g.cs`, and `__FrontComposerProjectionTemplatesRegistration.g.cs` covered.
- Diagnostic handling: severity/type filtering, fail flags, warning-only threshold behavior, missing optional sidecar fields, non-HFC filtering, malformed sidecar sentinel, and hostile sidecar path redaction covered.
- Build behavior: `EmitCompilerGeneratedFiles=true` and `CompilerGeneratedFilesOutputPath=obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer` covered by a narrow build-argument seam; process-level `--build` remains validated by build output and CI.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -v:minimal` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -parallel none -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests -class Hexalith.FrontComposer.Cli.Tests.CliHelpTests` passed 17/17 (16 dev + 1 Senior Developer Review parity pin).
- [x] (Senior Developer Review, AI) Full CLI in-process assembly ran 48 tests: 46 passed, 2 failed. The only failures are pre-existing `MigrationCommandTests` solution-selection tests (last committed in Story 11.3, commit `9530136`), unchanged by Story 7.1 and outside the inspect surface.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-build -v:minimal` aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the VSTest socket transport.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through in-process command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] All generated tests run successfully in the focused in-process lane.
- [x] Tests use proper locators: N/A for CLI; assertions target semantic CLI JSON/text fields and exit codes.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Next Steps
- Run the normal VSTest lane in CI or a local environment that permits socket creation.

## Story 7.2 - frontcomposer migrate

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - added focused migration pins for clean `applied=true`, invalid `--format`, mutually exclusive `--dry-run`/`--apply`, `--fail-on-findings`, `nameof(...)` false-positive prevention, excluded path segments, JSON diff budgets, and the two prior solution-selection failures.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - (QA Generate E2E Tests, AI) added public CLI output pins for `schemaVersion: frontcomposer.cli.migrate.v1`, safe-fix entry fields, migration docs link, sanitized diff payload, and manual-only `--fail-on-findings` exit behavior.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - (Senior Developer Review, AI) added `MigrationText_CapsPerEntryAndAggregateDiffs` pinning AC6 text-mode per-entry (8,000) and aggregate (64,000) diff-budget parity with the JSON path, closing the previously untested text render path.

### API Tests
- [x] Not applicable - Story 7.2 has no HTTP API endpoint surface.

### E2E Tests
- [x] CLI E2E-style coverage uses the existing in-process `CliApplication.RunAsync` pattern for the `frontcomposer migrate` user workflow.
- [x] Browser E2E tests are not applicable - Story 7.2 is a CLI migration feature with no browser-visible UI.

### Coverage
- Migration catalog: `9.1.0 -> 9.2.0` edge pinned.
- Dry-run/apply: dry-run default/no write, clean apply `applied=true`, idempotent rerun `unchanged`, source-hash drift failure, same-directory temp-file semantics covered by source audit.
- Path safety: `bin`, `obj`, `.git`, `packages`, `.nuget`, `nupkgs`, `/generated/`, submodule root, out-of-project redaction, and hostile sidecar sentinel paths covered.
- Diagnostics: `HFCM9001` safe fix, comments/`nameof` negative controls, unsupported code actions as manual-only, no FixAll, and synthetic `HFCM9002` sidecar manual-only behavior covered.
- Output/failure behavior: JSON schema, entry kinds, summary counts, docs link, diff payload, diff budgets, sanitizer behavior, invalid arguments, unsupported edges, source size/encoding fail-closed behavior, and `--fail-on-findings` for changed/manual-only/unchanged outcomes covered.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed 39/39.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 57/57.
- [x] (QA Generate E2E Tests, AI) `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after adding the QA pins.
- [x] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed 39/39 after adding the QA pins.
- [x] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 57/57 after adding the QA pins.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false` passed with 0 warnings / 0 errors.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` failed during restore because NuGet vulnerability data access to `api.nuget.org:443` is blocked in this sandbox.
- [ ] Full CLI in-process assembly without exclusions ran 58 tests: 57 passed, 1 environmental packaging smoke failure (`ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest`) due NuGet network/tool-cache access.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~MigrationCommandTests"` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [ ] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~MigrationCommandTests" -v:minimal` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [x] (Senior Developer Review, AI) `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug -m:1 /nr:false` passed with 0 warnings / 0 errors after the review fixes (text aggregate diff-budget parity + dead-local removal).
- [x] (Senior Developer Review, AI) Ran the tests via the **direct xUnit v3 in-process executable** (`./bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests`), which does not use the VSTest socket transport: `-class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed **40/40** (39 dev + 1 review pin), and the full CLI in-process assembly passed **59/59** with 0 skipped (incl. `ToolPackagingSmokeTests`, which passed in this environment).
- [x] (Senior Developer Review, AI) AC7 resolution: the two prior solution-selection failures (`ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically`, `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`) are confirmed **passing**, so AC7 is met by the tests-pass branch rather than an environmental reclassification.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through in-process command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] All generated tests run successfully in the focused in-process lane.
- [x] Tests use proper locators: N/A for CLI; assertions target semantic CLI JSON/text fields and exit codes.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 7.3 - surface the HFC diagnostic catalog

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - added mixed Hidden/Info/Warning/Error threshold pins for JSON and text inspect output, invalid `--severity` validation, and updated fail-flag expectations after threshold filtering.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` - added `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`, deriving active/reserved SourceTools rows from the registry and pinning descriptor, release-row, registry, and docs-stub severity parity.
- [x] `tests/e2e/specs/diagnostic-catalog-inspect.spec.ts` - added Playwright process-level CLI E2E coverage for `frontcomposer inspect` severity threshold filtering, invalid severity exit handling, malformed-sidecar `HFCM0002`, non-HFC sidecar filtering, JSON schema, text summary counts, and absolute path redaction.

### API Tests
- [x] Not applicable - Story 7.3 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser UI E2E tests are not applicable - Story 7.3 changes CLI inspect filtering and diagnostic catalog governance.
- [x] CLI E2E coverage now runs through Playwright by shelling out to the `frontcomposer` CLI project against disposable generated-output sidecars.
- [x] `tests/e2e/package.json` - added `test:fc-diagnostics` for the focused Story 7.3 CLI E2E lane.

### Coverage
- Inspect severity threshold semantics: hidden includes Hidden/Info/Warning/Error; info includes Info/Warning/Error; warning includes Warning/Error; error includes Error only.
- Inspect invalid severity remains `ExitCodes.InvalidArguments` (`2`).
- Inspect warning/error summary counts are calculated after severity filtering in text and JSON output.
- `--fail-on-warning` / `--fail-on-error` remain evaluated after severity and type filtering.
- Sidecar HFC filtering, optional fields, malformed-sidecar `HFCM0002`, and path sanitization remain covered by existing inspect pins.
- HFC1001-HFC1070 SourceTools catalog parity is covered through registry-derived descriptor/release-row/docs-stub checks.
- HFC1056/HFC1057 parser emission remains covered by existing focused parser pins.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` passed 18/18.
- [x] (Senior Developer Review, AI) `./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` passed 19/19 after adding `InspectSeverity_Hidden_IncludesNonCanonicalSeverities`, which pins that `--severity hidden` includes non-canonical-severity sidecar entries (the AC2 include-all level) while `error` still excludes them.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -method Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests.SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned` passed 1/1.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticDescriptorTests -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticCatalogTests` passed 24/24.
- [x] Focused HFC1056/HFC1057 parser lane passed 7/7 via direct xUnit v3 in-process runner.
- [x] `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 60/60.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true` passed with 0 warnings / 0 errors.
- [x] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run typecheck` passed.
- [x] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run test:fc-diagnostics` passed 3/3 in Chromium with `PLAYWRIGHT_SKIP_WEBSERVER=1`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false --no-restore` failed in `src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj` with `NU1301` because this sandbox blocks `api.nuget.org:443`.
- [ ] Full CLI in-process assembly without exclusions ran 61 tests: 60 passed, 1 environmental packaging smoke failure (`ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest`). First run failed on read-only `/home/administrator/.dotnet/toolResolverCache`; rerun with `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home` failed on blocked NuGet access.
- [ ] Broad `DiagnosticRegistryTests` class ran 115 tests: 114 passed, 1 pre-existing governance failure `Story112_LedgerRowsMapToOneOfThreeFinalStates` because `deferred-work.md` is missing. The Story 7.3 catalog parity method passed separately.
- [ ] Configured solution-level `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`), even with `-m:1 /nr:false`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through Playwright process-level command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned generated tests run successfully in focused in-process and Playwright CLI E2E lanes.
- [x] Tests use proper locators: N/A for CLI/catalog governance; assertions target semantic CLI JSON/text fields, exit codes, and catalog metadata.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 7.4 - opt-in drift detection vs. a baseline

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs` - added a focused alias opt-in pin proving `FrontComposerDriftDetectionEnabled=true` enables drift comparison when `HfcDriftDetectionEnabled` is absent.
- [x] Existing drift tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**` cover opt-in gating, candidate baseline naming, trust failures, structural drift, metadata drift, diagnostic payloads, ordering/truncation, byte stability, incremental caching, redaction, and HFC1070 trim/AOT isolation.
- [x] Existing diagnostic governance tests cover HFC1058-HFC1070 descriptor, registry, docs-stub, `FcDiagnosticIds`, and `AnalyzerReleases.Unshipped.md` parity.

### API Tests
- [x] Not applicable - Story 7.4 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser E2E tests are not applicable - Story 7.4 is a build-time SourceTools diagnostic feature.
- [x] Generator-driver style SourceTools tests cover the adopter build-time flow with analyzer-config options and `AdditionalText` baselines.

### Coverage
- Opt-in behavior: `HfcDriftDetectionEnabled` and `FrontComposerDriftDetectionEnabled` enable comparison; disabled drift ignores candidate baselines and leaves generated output stable.
- Baseline trust: HFC1058-HFC1064 and HFC1069 fail closed for missing, configured-path mismatch, empty/malformed/unsupported/oversized/duplicated/invariant-violating, and redaction-unsafe baselines.
- Drift classification: HFC1065 covers structural declaration/property/type/nullability/bounded-context drift; HFC1066 covers renderer and metadata-impacting changes.
- Diagnostic contract: payload property bag, help links, message shape, path normalization, redaction, deterministic ordering, and truncation are covered.
- Incremental/output stability: `LoadDriftBaselines` tracked step name, no `CompilationProvider` dependency for HFC1058-HFC1069 comparison, and diagnostics-only byte stability are covered.
- HFC1070: trim/AOT advisory remains separately registered and compilation-backed only for action-queue catalog evidence.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after the QA alias pin.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline.DriftAnalyzerConfigOptionsTests.FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` passed 1/1.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class "*Drift*" -class- "*Benchmarks*"` passed 170/170 after the QA alias pin.
- [x] Focused diagnostic parity lane passed 25/25 via direct xUnit v3 in-process runner: `DriftDiagnosticCatalogTests`, `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`, descriptor/release-row parity methods.
- [x] Broad SourceTools in-process lane with default exclusions ran 1023 tests: 1020 passed, 3 failed. Failures are outside Story 7.4: `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates` (`deferred-work.md` missing), `FcDocComponentDocumentationContractTests.EveryComponentPageContainsAllRequiredSections(datagrid.md)`, and `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --filter "FullyQualifiedName~Drift&Category!=Performance&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`).

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; generator-driver SourceTools tests cover the build-time flow.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use proper locators: N/A for build-time SourceTools diagnostics; assertions target Roslyn diagnostics, generated outputs, and catalog metadata.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
