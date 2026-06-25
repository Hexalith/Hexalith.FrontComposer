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

## Story 7.5 - Testing library bUnit host and deterministic fakes

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` - added host wiring pins for JSInterop override, service replacement, TimeProvider registration, domain assembly de-duplication, direct-composition `DuringHostSetup` store initialization, command cancellation/context/lifecycle evidence, query not-modified/empty evidence, projection page not-modified evidence, query/page cancellation, all five deterministic fault modes, redaction/truncation, and generated Counter command dispatch through public Testing APIs.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - added package README inclusion pin, hardened Release pack commands with `-m:1 /nr:false`, and made the clean temporary consumer restore/build locally from packed packages without repo-relative project references.

### API Tests
- [x] Public Testing package API baseline remains enforced by `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline`.
- [x] Intentional public API addition: `TestQueryService.NotModifiedWith<T>(IReadOnlyList<T>, string?)`, recorded in `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`.

### E2E Tests
- [x] Browser E2E tests are not applicable - Story 7.5 is a bUnit Testing package story.
- [x] Generated Counter projection and command flows are exercised through bUnit using adopter-facing Testing APIs only.
- [x] Clean temporary consumer package restore/build validates package consumption without repo-relative project references.

### Coverage
- Host wiring: localization, FluentUI components, Shell defaults, in-memory storage, user context, command/query/page-loader fakes, TimeProvider, fault provider, Loose JSInterop default, JSInterop override, culture restoration, direct-composition replacement-before-initialization, and option-driven store initialization covered.
- Fakes: command lifecycle order and deterministic IDs, command context/evidence/redaction, query success/not-modified/empty paths, projection page success/not-modified/empty paths, cancellation before evidence capture, bounded evidence, and parallel context isolation covered.
- Faults: Drop, Delay, PartialDelivery, Reorder, and ReconnectNudge covered with deterministic timestamp/tenant/user/correlation evidence and bounded retention.
- Package/public API: public API baseline, README/package baseline file inclusion, dependency exclusions, Release pack, and clean temporary consumer restore/build covered.

### Validation
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors in 33.96s after the QA projection not-modified evidence pin.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests` passed 23/23 via direct xUnit v3 in-process runner (22/22 before the Senior Developer Review redaction regression pin).
- [x] Senior Developer Review (auto-fix) added `RedactedEvidenceFormatter_Format_RedactsSecretValuesContainingCommas`, which failed before the JSON-string-aware redaction fix in `Evidence.cs` and passes after it; re-verified Release solution build at 0 warnings / 0 errors.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -m:1 /nr:false` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-build -c Release` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`) across test assemblies before executing tests.

### Checklist
- [x] API tests generated if applicable: public API baseline and package boundary pins updated.
- [x] E2E tests generated if UI exists: N/A for browser UI; bUnit generated Counter paths and clean consumer package flow cover adopter-facing flows.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned focused lane runs successfully through the direct xUnit v3 in-process runner.
- [x] Tests use proper locators: bUnit assertions target semantic component markup and service evidence, not brittle external services.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 8.1 - Neutral header chrome and footer framing

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` - added focused shell chrome pins for the neutral header background/divider, default footer neutral frame with `FluentText`, and adopter-supplied footer content inside the framed footer chrome.
- [x] `tests/e2e/specs/shell-chrome.spec.ts` - added focused Playwright coverage that drives the live shell through the existing settings theme control, then asserts header/footer computed styles resolve to `--colorNeutralBackground2`/`--colorNeutralStroke2`, the header does not use the accent surface fill, and title/action contrast remains WCAG AA in light and dark themes.
- [x] `tests/e2e/package.json` - added `test:fc-shell-chrome` for the focused Story 8.1 browser lane.

### API Tests
- [x] Not applicable - Story 8.1 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser a11y/visual evidence remains owned by `tests/e2e/specs/specimen-accessibility.spec.ts`.
- [x] Story 8.1 browser chrome assertions are now generated in `tests/e2e/specs/shell-chrome.spec.ts`.
- [ ] Local Playwright execution was blocked before browser launch because Kestrel could not create a listening socket in this sandbox; CI remains the browser/a11y/visual gate.

### Coverage
- Header chrome: top-level shell header `FluentStack` keeps `height: 48px`, `padding: 0 12px`, and `HorizontalAlignment.SpaceBetween`, and now uses `--colorNeutralBackground2` plus `--colorNeutralStroke2`.
- Footer chrome: default and adopter-supplied footer content render inside the same neutral `FluentStack` frame with `min-height: 36px`.
- Browser chrome: live shell header/footer computed styles are pinned against Fluent neutral tokens in light and dark themes, with accent-surface regression and contrast checks.
- Fluent governance: no legacy Fluent v4/FAST token was introduced; the focused Shell legacy-token governance method passes.

### Validation
- [x] RED phase: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.HeaderChrome_UsesNeutralSurfaceAndDivider -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.DefaultFooterChrome_UsesNeutralFrameAndFluentText -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.AdopterSuppliedFooter_RendersInsideNeutralFrame` failed 3/3 before the Razor change, proving the new assertions covered the missing behavior.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] Focused Story 8.1 direct xUnit v3 lane passed 3/3 after implementation.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests` passed 27/27.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.Shell.Tests.Governance.FluentConformanceTests.Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog` passed 1/1.
- [x] (QA Generate E2E Tests, AI) `./tests/e2e/node_modules/.bin/tsc --noEmit --ignoreDeprecations 5.0 -p tests/e2e/tsconfig.json` passed for the generated Playwright spec under the installed TypeScript 5.9.3 compiler.
- [ ] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run typecheck` failed before type-checking because local `node_modules` contains stale `typescript@5.9.3` while `package.json`/`package-lock.json` require `typescript@6.0.3`; TS 5.9 rejects `tsconfig.json` `ignoreDeprecations: "6.0"`.
- [ ] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions executed because Kestrel could not create a listening socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` failed during restore with `NU1900` because this sandbox cannot access NuGet vulnerability data at `api.nuget.org:443`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:NuGetAudit=false` built Story 8.1-owned Shell projects, then failed in `Hexalith.Tenants.UI` because nested `Hexalith.Memories` submodule projects are intentionally not initialized under the repository submodule rules.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` failed during restore with the same `NU1900` NuGet vulnerability-data network blocker.
- [ ] `npm --prefix tests/e2e run test:a11y` failed before tests executed because the specimen web server could not bind a Kestrel socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `npm --prefix tests/e2e run test:visual:update` failed with the same Kestrel socket permission blocker before refreshing light/dark visual baselines.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: `tests/e2e/specs/shell-chrome.spec.ts` now covers Story 8.1 browser chrome in light/dark themes; local execution blocked by sandbox socket restrictions.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover custom/default footer paths and token-governed header/footer chrome.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use existing user-visible controls, visible shell text, and computed Fluent token assertions without hardcoded waits.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics and local blockers.

## Story 8.2 - Accent-as-thread policy and regression guard

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - added `Shell_chrome_styles_never_use_accent_as_surface_background`, a Shell `.css`/`.razor` static-scan guard that fails when `background` or `background-color` declarations reference `var(--fc-color-accent)` or `var(--fc-accent-base-color)`.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - added matcher-level QA pins for forbidden `background`/`background-color` declarations, including CSS `var(..., fallback)` syntax, and allowed non-background accent thread uses.

### API Tests
- [x] Not applicable - Story 8.2 has no HTTP API endpoint surface.

### E2E Tests
- [x] No new browser E2E was required; Story 8.2 reuses Story 8.1 shell-chrome browser coverage for rendered neutral header/footer behavior.
- [ ] Local `test:fc-shell-chrome` execution remains blocked before browser assertions because Kestrel cannot create a listening socket in this sandbox.

### Coverage
- Architecture rule: `_bmad-output/project-docs/architecture.md` §4.1 already states that the accent is a thread, not a chrome fill, so no architecture edit was required.
- Forbidden uses: `background:` and `background-color:` declarations in Shell `.css`/`.razor` source now fail if their value references the Shell accent bridge variables, including fallback-valued CSS variable calls.
- Allowed uses: custom property definitions, foreground color, border/outline/focus, active navigation, links, primary affordances, badges, selected-state accent thread uses, and future accent-left-bar-style shadows are not flagged.
- Allowlist discipline: the accent-surface allowlist is empty and includes stale-entry detection.
- Story 8.1 preservation: neutral header/footer bUnit pins pass unchanged.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] (QA Generate E2E Tests, AI) `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors after adding matcher pins.
- [x] RED phase: temporarily adding `background: var(--fc-color-accent);` to `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` made `Shell_chrome_styles_never_use_accent_as_surface_background` fail 1/1 with the expected repository-relative offender path; the temporary violation was removed before final validation.
- [x] Focused new guard direct xUnit v3 lane passed 1/1 after removing the temporary violation.
- [x] Full `FluentConformanceTests` governance class passed 6/6, including the existing legacy-token guard and the new accent-as-background guard.
- [x] (QA Generate E2E Tests, AI) Full `FluentConformanceTests` governance class passed 17/17, including the new matcher-level forbidden/allowed declaration pins.
- [x] Story 8.1 shell chrome direct xUnit v3 lane passed 3/3: `HeaderChrome_UsesNeutralSurfaceAndDivider`, `DefaultFooterChrome_UsesNeutralFrameAndFluentText`, and `AdopterSuppliedFooter_RendersInsideNeutralFrame`.
- [x] (QA Generate E2E Tests, AI) Focused Story 8.1 preservation lane passed 28/28 via direct xUnit v3: `FrontComposerShellTests` plus `SlotMappingRegressionTests`.
- [x] (QA Generate E2E Tests, AI) Matcher-focused direct xUnit v3 lane passed 11/11: `Accent_surface_guard_flags_background_declarations` and `Accent_surface_guard_allows_thread_declarations`.
- [x] Shell in-process assembly excluding Contract tests passed 1964/1964 via the direct xUnit v3 runner.
- [ ] Full Shell in-process assembly ran 1967 tests: 1966 passed, 1 Pact Contract test failed because PactNet could not start a local mock server in this sandbox.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` restored/built until test execution, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [ ] `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: no new browser test required; Story 8.1 shell-chrome E2E remains the rendered-browser coverage and is locally socket-blocked.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover the critical regression path with a RED-phase temporary violation.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use source scanning rather than brittle rendered markup for this governance policy.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics and local blockers.
