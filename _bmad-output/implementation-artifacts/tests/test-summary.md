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
