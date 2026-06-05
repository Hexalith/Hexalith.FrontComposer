# FC-CLI-MIGRATE v1 Contract

Date: 2026-06-05
Status: confirmed-and-pinned
Scope: `frontcomposer migrate` dry-run/apply behavior for allowlisted migration edges.
Owner: FrontComposer Epic 7, Story 7.2.

## Command Surface

`frontcomposer migrate` plans or applies allowlisted Roslyn code-fix migrations across declared
FrontComposer version edges.

Supported options:

- `--from <version>` and `--to <version>`; both must match a catalog edge.
- `--dry-run`; default when `--apply` is absent.
- `--apply`; writes source only for planned eligible edits.
- `--project <path>` or `--solution <path>`.
- `--format text|json`; default `text`.
- `--fail-on-findings`.

`--dry-run` and `--apply` are mutually exclusive. Invalid `--format`, unsupported edges, unsupported
project/solution shapes, malformed solution project entries, `.slnx`, and `.fsproj` fail closed with
`ExitCodes.InvalidArguments` (`2`) before source changes are planned or written.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/CliApplication.cs`
- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/CommandOptions.cs`
- Source: `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`
- Source: `src/Hexalith.FrontComposer.Cli/ExitCodes.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs`

## Catalog Edge

The v1 catalog contains exactly one supported edge:

| From | To | Docs link | Safe diagnostic | Manual-only diagnostic |
|---|---|---|---|---|
| `9.1.0` | `9.2.0` | `docs/migrations/9.1-to-9.2.md` | `HFCM9001` | `HFCM9002` |

Unsupported version order, missing edges, or unknown versions return `2` and report the supported
edge list. This story does not add new edges.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## JSON Schema

`migrate --format json` emits `schemaVersion = "frontcomposer.cli.migrate.v1"`.

Top-level fields:

- `schemaVersion`
- `applied`
- `summary`: `changed`, `unchanged`, `skipped`, `failed`, `manualOnly`, `conflicts`
- `entries[]`: `diagnosticId`, `kind`, `path`, `what`, `expected`, `got`, `fix`, `docsLink`,
  `diff`, `formattingApplied`

Entry `kind` values:

- `safe-fix`
- `unchanged`
- `skipped`
- `failed`
- `manual-only`
- `conflict`

Entries are ordered deterministically by path and diagnostic id for JSON and text output. All
user-visible fields are sanitized before output. Per-entry diffs are capped at 8,000 characters and
aggregate JSON diffs at 64,000 characters; further diffs emit an omitted-budget placeholder.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/JsonOptions.cs`
- Source: `src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/OutputSanitizerTests.cs`

## Exit Codes and Fail-on-Findings

| Code | Name | Migrate meaning |
|---|---|---|
| `0` | `Success` | Planning/apply completed without requested failure promotion. |
| `1` | `ActionableFindings` | `--fail-on-findings` saw at least one `safe-fix`, `manual-only`, or `conflict` entry. |
| `2` | `InvalidArguments` | Invalid input or unsupported catalog/project/solution shape. |
| `3` | `GeneratedOutputUnavailable` | Not used by migrate v1. |
| `4` | `ApplyWriteFailure` | Apply/write, cancellation, workspace, or filesystem failure. |

`--fail-on-findings` does not promote purely `unchanged` output.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/ExitCodes.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Path Safety and Redaction

Planning and apply refuse writes outside the selected project and into these segments:

- `bin`
- `obj`
- `.git`
- `.hg`
- `.svn`
- `packages`
- `.nuget`
- `nupkgs`
- any `/generated/` segment
- submodule roots discovered from repository `.gitmodules`

Out-of-project paths are reported as `[redacted-path]`; untrusted sidecar paths are reported under
`__sidecar__/...` rather than leaking absolute host paths. Apply rechecks canonical target path,
submodule boundaries, and source hash before writing.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Source: `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Migration Diagnostics and Code Fixes

`HFCM9001` is the only automated safe-fix diagnostic for this edge. The scanner finds identifier
usages of `AddFrontComposerDebugOverlay`, skips `nameof(...)`, and does not infer diagnostics from
comments or ordinary source text. The code fix replaces the identifier with
`AddFrontComposerDevMode`.

The code-fix provider has no FixAll provider. Any missing, non-deterministic, or non-allowlisted
`CodeActionOperation` is treated as `manual-only`; unsafe document additions/removals, reference
changes, analyzer config changes, and writes outside the approved document are discarded.

`HFCM9002` is manual-only. Today it is read only from synthetic/test-fixture generated diagnostic
sidecars under `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`. There is no
production SourceTools `HFCM9002` sidecar emitter in this story. Story 7.3/7.4 diagnostic governance
and drift work owns production emission/ownership decisions.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- Source: `src/Hexalith.FrontComposer.Cli/README.md`

## Apply Semantics

Dry-run is the default and never writes source. Apply mode writes only the `PlannedFileEdit` source
files produced by the current plan. Each write uses a same-directory temporary file, then replaces the
original. `applied=true` is emitted only after `--apply` ran to completion and every planned write
succeeded. Partial write, cancellation, stale source hash, unreadable source, unsafe target drift, or
write failure emits `failed` and keeps `applied=false`.

Rerunning apply after a clean migration is idempotent and reports `unchanged`.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Encoding and Size Limits

Migration source reads support UTF-8, UTF-8 BOM, UTF-16 LE/BE, and UTF-32 LE/BE. Invalid UTF-8 and
unknown encodings fail closed. Source files larger than 16 MiB are refused before decoding.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Project Selection

Selection precedence is:

1. Explicit `--project`
2. Explicit `--solution`
3. Current directory discovery

`--solution` supports `.sln` only, requires exactly one `.csproj`, rejects unsupported project types,
rejects malformed `Project(...)` entries, and rejects project paths outside the solution directory.
Story 7.2 fixed Windows-style `.sln` project paths on non-Windows hosts by normalizing solution entry
separators before canonicalization.

Evidence:

- Source: `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`
- Test: `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`

## Non-Goals

- No third-party CLI framework.
- No new migration catalog edge.
- No new production SourceTools `HFCM9002` sidecar emitter.
- No broad semantic rewrite of adopter source.
- No FixAll support.
- No package or Roslyn version changes.
- No `.slnx`, `.fsproj`, multi-project solution, or unsupported project-type support.

## Validation Summary

- Exact release build command `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`
  was attempted and failed during restore with blocked NuGet vulnerability data access.
- Network-disabled release build evidence passed:
  `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false`.
- Focused in-process CLI migration lane: `MigrationCommandTests` passed 39/39.
- Broader in-process CLI assembly excluding network/tool-cache packaging smoke: passed 57/57.
- Full in-process CLI assembly: 57/58, with only
  `ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest` blocked by local
  NuGet/network or read-only home tool-cache access.
- VSTest command attempted locally and blocked before test execution by
  `System.Net.Sockets.SocketException (13): Permission denied`; CI remains authoritative for VSTest.
