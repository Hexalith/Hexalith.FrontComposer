# FC-CLI-INSPECT v1 Contract

Date: 2026-06-05
Status: confirmed-and-pinned
Scope: `frontcomposer inspect` generated-output and diagnostics reporting.
Owner: FrontComposer Epic 7, Story 7.1.

## Command Surface

`frontcomposer inspect` reads generated files and diagnostic sidecars from the public generated-output
directory:

`obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`

The public source of that layout is `GeneratedOutputPathContract.Directory` and
`GeneratedOutputPathContract.Template`. The CLI keeps the same project-relative directory shape and
passes `-p:EmitCompilerGeneratedFiles=true` plus
`-p:CompilerGeneratedFilesOutputPath=obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer`
when `--build` is supplied. If `--framework` is omitted for the build invocation, the CLI passes the
MSBuild placeholder `$(TargetFramework)` so each target framework resolves under its own output folder.

Supported inspect options are:

- `--project <path>`
- `--solution <path>`
- `--configuration <name>` with default `Debug`
- `--framework <tfm>`
- `--build`
- `--type <metadata-name>`
- `--severity hidden|info|warning|error`
- `--fail-on-warning`
- `--fail-on-error`
- `--format text|json` with default `text`
- `--absolute-paths`

`--summary` is not part of v1. Story 7.1 removed it from global help rather than adding unsupported
behavior.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/CliApplication.cs`
- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- Source: `src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

## JSON Schema

`inspect --format json` emits `schemaVersion = "frontcomposer.cli.inspect.v1"`.

Top-level fields:

- `schemaVersion`
- `project`: `name`, selected-project-relative `path`, `configuration`, `framework`
- `summary`: `generatedFiles`, `forms`, `grids`, `registrations`, `mcpManifestEntries`, `warnings`,
  `errors`
- `generatedFiles[]`: `path`, `family`, `relatedType`
- `diagnostics[]`: `id`, `severity`, `relatedType`, `path`, `what`, `expected`, `got`, `fix`,
  `docsLink`

All user-visible text and JSON string values pass through `OutputSanitizer` or project-relative path
normalization before output.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/JsonOptions.cs`
- Source: `src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/OutputSanitizerTests.cs`

## Generated-File Families

Generated files are enumerated top-level only from the generated-output directory and include `*.g.cs`
and `*.g.razor.cs`. Ordering is deterministic and shared by JSON and text output:

1. `RelatedType` with null treated as the empty string
2. `Family`
3. `RelativePath`

Family mapping:

| File pattern | Family | Summary field |
|---|---|---|
| `*.g.razor.cs` projection output | `ProjectionRazor` | `grids` |
| `*Feature.g.cs` or `*LifecycleFeature.g.cs` | `FluxorFeature` | `generatedFiles` only |
| `*Actions.g.cs` | `FluxorActions` | `generatedFiles` only |
| `*Reducers.g.cs` | `FluxorReducers` | `generatedFiles` only |
| `*Registration.g.cs` | `Registration` | `registrations` |
| `*.CommandForm.g.razor.cs` or `*CommandForm.g.razor.cs` | `CommandForm` | `forms` |
| `*.CommandRenderer.g.razor.cs` or `*Renderer.g.razor.cs` | `CommandRenderer` | `generatedFiles` only |
| `*LifecycleBridge.g.cs` | `CommandLifecycleBridge` | `generatedFiles` only |
| `*LastUsedSubscriber.g.cs` | `CommandLastUsedSubscriber` | `generatedFiles` only |
| `*Page.g.razor.cs` | `CommandPage` | `generatedFiles` only |
| `FrontComposerMcpManifest.g.cs` | `McpManifest` | `mcpManifestEntries` |
| `__FrontComposerProjectionTemplatesRegistration.g.cs` | `TemplateManifest` | `generatedFiles` only |
| Unmatched `*.g.cs` / `*.g.razor.cs` | `Unknown` | `generatedFiles` only |

`mcpManifestEntries` in v1 means the count of generated manifest files classified as `McpManifest`.
It does not parse command or resource descriptors inside `FrontComposerMcpManifest.g.cs`.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

## Diagnostic Sidecars

`inspect` reads top-level `*.diagnostics.json` sidecars from the generated-output directory. A sidecar
may be either a JSON array or an object with a `diagnostics` array.

Rules:

- Only diagnostic IDs starting with `HFC` are reported.
- Missing optional fields are emitted as empty strings.
- Malformed JSON emits one deterministic sentinel diagnostic per sidecar:
  `HFCM0002`, `Warning`.
- Unreadable sidecars also emit `HFCM0002`, `Warning`.
- Sidecar `path` values are normalized relative to the selected project directory when possible.
- Drive-relative paths, URI-shaped paths, paths outside the selected project, traversal, invalid
  paths, and host paths are redacted to `[redacted-path]`.
- Control characters are escaped before text or JSON output.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`
- Source: `src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

## Exit Codes

| Code | Name | Inspect meaning |
|---|---|---|
| `0` | `Success` | Inspect completed and no requested fail flag matched filtered diagnostics. |
| `1` | `ActionableFindings` | `--fail-on-warning` matched a warning or error, or `--fail-on-error` matched an error, after `--severity` and `--type` filtering. |
| `2` | `InvalidArguments` | Invalid input, unsupported project/solution shape, ambiguous generated output, invalid severity, invalid framework, or unmatched/ambiguous type filter. |
| `3` | `GeneratedOutputUnavailable` | Build failed, generated-output directory is missing, selected framework output is missing, or the generated-output directory contains no generated files. |
| `4` | `ApplyWriteFailure` | Shared CLI filesystem/cancellation failure path; not the expected inspect generated-output failure code. |

`--fail-on-warning` is stricter than `--fail-on-error`. When both are supplied, warning-or-error
matching produces code `1`.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/ExitCodes.cs`
- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

## Project Selection and Path Safety

Project selection remains fail-closed:

- `--project` must resolve to an existing `.csproj` file.
- `.fsproj` is unsupported in v1.
- `--solution` must resolve to `.sln`; `.slnx` is unsupported in v1.
- A `.sln` may select exactly one `.csproj`; multiple projects require `--project`.
- Generated output across multiple target frameworks is ambiguous unless `--framework` is supplied.
- Default output is project-relative. `--absolute-paths` is opt-in for generated-file paths only.
- Error output redacts absolute paths through `PathUtilities.RedactAbsolute` / project-relative
  conversion before display.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`
- Source: `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Diagnostic Phase Boundary

`inspect` reports diagnostics that are actually present in sidecar/build output. It does not claim
cataloged FC-CUST Level 3/4 customization IDs are build-emitted unless sidecar/build evidence exists.

Current disposition boundary from Epic 6:

- `HFC1050`-`HFC1055` are build-time SourceTools accessibility analyzer warnings for statically
  inspectable Level 2/3/4 customization surfaces.
- `HFC1038`-`HFC1041` are currently Level 3 call-site/startup/runtime or reserved/catalog behavior,
  not proven SourceTools sidecar/build output.
- `HFC1042` and `HFC1046` are reserved/catalog-adjacent.
- `HFC1043`-`HFC1045` are current Level 4 registry/startup/runtime diagnostics.

Evidence:

- Source: `_bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md`
- Source: `_bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md`
- Source: `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md`
- Source: `_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md`
- Source: `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`

## Non-Goals

- No third-party CLI framework.
- No `System.CommandLine` dependency.
- No Roslyn package pin changes.
- No generated-code hand edits.
- No migration command schema or behavior changes.
- No published `docs/` changes in this story.
- No parsing of descriptor entries inside `FrontComposerMcpManifest.g.cs` for v1.

## Changed-File Reconciliation

Story-owned changed files at completion:

- `_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/7-1-frontcomposer-inspect.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Cli/CliApplication.cs`
- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- `src/Hexalith.FrontComposer.Cli/README.md`
- `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`

Pre-existing unrelated workspace change observed and not owned by this story:

- `_bmad-output/story-automator/orchestration-1-20260604-140358.md`

Validation summary:

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings and
  0 errors.
- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Focused xUnit v3 in-process lane passed: `InspectCommandTests` + `CliHelpTests`, 15/15.
- Exact `dotnet test` / solution VSTest lanes are locally blocked before test execution by
  `System.Net.Sockets.SocketException (13): Permission denied`; CI remains authoritative.
- Full CLI in-process assembly ran 46 tests with 43 passing and 3 non-story local failures: local
  tool package smoke cannot write to read-only `$HOME/.dotnet/toolResolverCache`, and two existing
  migration solution-selection tests fail outside the inspect surface.
