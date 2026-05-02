# Story 9.2: CLI Inspection & Migration Tools

Status: ready-for-dev

> **Epic 9** - Developer Tooling & Documentation. Covers **FR63**, **FR64**, and **NFR77**. Builds on Story **9-1** generated-output and drift-baseline contracts, existing SourceTools incremental generator outputs, the HFC diagnostic catalog, and the Story 8 MCP manifest surface. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, and **L15**.

---

## Executive Summary

Story 9-2 adds developer-facing tooling for inspecting generated FrontComposer output and applying safe framework migration fixes:

- Provide a `dotnet` tool entry point for `inspect` and `migrate` commands.
- Inspect generated Razor, Fluxor, registration, MCP manifest, and diagnostic output from deterministic `obj/{Config}/{TFM}/generated/HexalithFrontComposer/...` paths.
- Expose summary and filtered views without requiring developers to browse `obj/` by hand.
- Apply known migration fixes through Roslyn code-fix providers with dry-run, reviewable diffs, and explicit diagnostic IDs.
- Keep SourceTools generator/analyzer load-context discipline intact. Do not move CLI-only dependencies into the generator assembly.

---

## Story

As a developer,
I want CLI tools to inspect what the source generator produced and to apply automated code fixes when upgrading framework versions,
so that I can debug generation issues and upgrade confidently without manual code changes.

### Adopter Job To Preserve

An adopter should be able to run a local or global `frontcomposer` tool in a solution, ask "what did the generator produce for this domain type?", see the exact generated files and HFC diagnostics, then run a migration preview that reports every proposed edit before writing source files. The tool must make generator behavior inspectable without hiding build failures, mutating source unexpectedly, or requiring a specific IDE.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A developer runs `frontcomposer inspect --type Acme.Shipping.ShipmentProjection` from a project or solution directory | The command executes | The tool resolves the target project, builds or loads generated output, and displays the deterministic generated files for that type. |
| AC2 | A type-specific inspection finds generated output | Output is rendered | The report includes generated Razor component, Fluxor state/actions/reducers, registration, MCP descriptor contribution where present, and all HFC diagnostics associated with the type. |
| AC3 | A type-specific inspection cannot find the type | The command completes | It exits non-zero with a bounded message listing closest known generated type names and does not print raw compiler exception text or absolute path dumps beyond the relevant project-relative path. |
| AC4 | The developer runs `frontcomposer inspect` without a type filter | The command executes | A summary is displayed with counts of generated forms, grids, registrations, MCP manifest entries, warnings, and errors. |
| AC5 | Generated files exist under `obj/{Config}/{TFM}/generated/HexalithFrontComposer` | The summary report is rendered | Each generated file is listed with a deterministic project-relative path and source family classification. |
| AC6 | The generated output directory is missing or stale | Inspect runs | The tool explains whether a build is required, optionally runs the build when `--build` is provided, and never silently reports an empty generation set as success. |
| AC7 | The target project has multiple target frameworks or configurations | Inspect runs | `--framework` and `--configuration` select the output; ambiguous targets produce a clear error with valid choices. |
| AC8 | The generator emits diagnostics during inspection | The CLI displays them | Diagnostics use HFC ID, severity, location, What/Expected/Got/Fix/DocsLink fields where available, and `DiagnosticDescriptor.HelpLinkUri` when present. |
| AC9 | A framework version upgrade has known breaking API changes | The developer runs `frontcomposer migrate --from <version> --to <version> --dry-run` | Roslyn code fixes for known migration diagnostics are discovered, planned, and reported without writing files. |
| AC10 | The developer runs migration without `--dry-run` | Fixes are applied | Only fixable HFC migration diagnostics are changed; each edit is reported with what changed, why, diagnostic ID, file path, and whether formatting was applied. |
| AC11 | A migration diagnostic has no safe automated fix | Migration runs | The tool reports a manual action with what needs to change, where, and a migration guide link. |
| AC12 | Multiple code fixes can touch the same file | Migration applies | Edits are composed through Roslyn `Solution`/`Document` changes and conflict-detected before writing; overlapping edits abort the file rather than producing partial corruption. |
| AC13 | A migration is interrupted or a write fails | The process exits | Already-written files are reported, unwritten files remain untouched, and the tool exits non-zero with a retry-safe summary. |
| AC14 | The project contains generated files, `bin/`, `obj/`, submodules, or unrelated repositories | Migration scans | It only scans explicit solution/project documents, never recursively edits generated output or nested submodules. |
| AC15 | CLI output may include tenant/user/policy names, command payload examples, local paths, or diagnostics | Reports are rendered | Sensitive values are redacted or project-relative; no tenant IDs, user IDs, tokens, command payload values, ETags, raw exception text, or machine-specific absolute paths are persisted in logs. |
| AC16 | The CLI package is distributed | A developer installs it | It is available as a `dotnet` global/local tool with a stable command name and version aligned to the FrontComposer package train. |
| AC17 | A developer wants to run the tool without permanent installation on .NET 10 SDK | The package is available from a feed | The story documents the `dnx` path as optional convenience while keeping `dotnet tool install` and local tool manifests first-class. |
| AC18 | CLI tests run in CI | The suite executes | It validates inspect, migration dry-run, migration apply, diagnostics, redaction, path stability, and package/tool invocation without relying on a developer IDE. |

---

## Tasks / Subtasks

- [ ] T1. Define CLI project and package shape (AC1, AC4, AC7, AC16, AC17)
  - [ ] Add a new console project under `src/Hexalith.FrontComposer.Cli/` unless an existing tooling host is introduced before implementation.
  - [ ] Package it as a .NET tool with `<PackAsTool>true</PackAsTool>` and a stable command name, recommended `frontcomposer`.
  - [ ] Target the current shipping runtime TFM used by the repo; do not force SourceTools off `netstandard2.0`.
  - [ ] Keep CLI-only dependencies, MSBuild workspace dependencies, and console formatting dependencies out of `Hexalith.FrontComposer.SourceTools`.
  - [ ] Add package metadata so global and local tool installation share the lockstep FrontComposer version.

- [ ] T2. Establish generated-output path contract (AC1, AC2, AC5, AC6, AC7)
  - [ ] Use the public path contract `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs` and sibling `.g.cs` files.
  - [ ] Wire the path through package-owned MSBuild props/targets or a documented generator-output option; avoid ad hoc path guesses in the CLI.
  - [ ] Preserve existing generator hint-name semantics: namespace-qualified projection hints and `.Command` command hint prefixes.
  - [ ] Add a contract test proving generated output lands under the documented path for Debug/Release and at least one multi-targeted fixture.
  - [ ] When output is absent, distinguish "project has no FrontComposer types", "build not run", "generation failed", and "unsupported target framework".

- [ ] T3. Implement inspect model loading (AC1-AC8, AC15)
  - [ ] Build a small SDK-neutral inspect model: project identity, configuration, TFM, generated files, source family, related domain type, MCP entries, and diagnostics.
  - [ ] Classify generated files by suffix: `.g.razor.cs`, `Feature.g.cs`, `Actions.g.cs`, `Reducers.g.cs`, `Registration.g.cs`, `CommandForm.g.razor.cs`, `CommandRenderer.g.razor.cs`, `CommandLifecycleBridge.g.cs`, `FrontComposerMcpManifest.g.cs`, and template manifest files.
  - [ ] Prefer generated metadata produced by SourceTools when available; fall back to deterministic file-name parsing only when metadata is absent.
  - [ ] Sort output by bounded context, type FQN, source family, then path using ordinal comparison.
  - [ ] Redact machine-local path prefixes in default output; provide absolute paths only behind an explicit `--absolute-paths` diagnostic option.

- [ ] T4. Implement inspect command UX (AC1-AC8)
  - [ ] Support `frontcomposer inspect`, `frontcomposer inspect --type <metadata-name>`, `--project`, `--solution`, `--configuration`, `--framework`, `--build`, `--format text|json`, and `--severity`.
  - [ ] Keep text output concise and operator-readable; JSON output must be deterministic for CI snapshots.
  - [ ] Type matching accepts full metadata name first, then unambiguous simple type name; ambiguous simple names require the full name.
  - [ ] Include HFC diagnostic docs links when `DiagnosticDescriptor.HelpLinkUri` is available.
  - [ ] Non-zero exit codes: invalid arguments, ambiguous target, build failure, generation failure, or requested type not found.

- [ ] T5. Introduce migration/code-fix architecture (AC9-AC12)
  - [ ] Add a migration abstraction that maps `(fromVersion, toVersion, diagnosticId)` to a Roslyn code-fix provider or a manual migration note.
  - [ ] Keep analyzer/code-fix providers in a separate assembly if Workspaces packages are required; do not add Workspaces dependencies to the generator assembly unless party review explicitly approves it.
  - [ ] Use Roslyn `CodeFixProvider` patterns: declare `FixableDiagnosticIds`, register fixes through `RegisterCodeFixesAsync`, and provide Fix All only where edits are deterministic and conflict-free.
  - [ ] Support manual-only migration entries for changes that require product or architecture judgment.
  - [ ] Reserve migration-specific HFC IDs only after checking `AnalyzerReleases.Unshipped.md`; Story 9-4 owns final public diagnostic governance.

- [ ] T6. Implement migration dry-run and apply modes (AC9-AC13, AC15)
  - [ ] Make `--dry-run` available and documented; consider it the recommended command in docs and CI examples.
  - [ ] In dry-run, compute proposed changes and render unified diff or structured JSON without writing files.
  - [ ] In apply mode, compose Roslyn document changes first, detect overlapping edits, then write files in a deterministic order.
  - [ ] Never modify generated files, `obj/`, `bin/`, vendored submodules, or files outside the selected project/solution.
  - [ ] Exit non-zero when any target file cannot be safely fixed, and report the manual follow-up without hiding successfully planned fixes.

- [ ] T7. Add migration guide handoff (AC8, AC9, AC11, AC16)
  - [ ] Link each migration diagnostic to a future diagnostic/migration page path owned by Story 9-4 or Story 9-5.
  - [ ] Emit message fields shaped as What, Expected, Got, Fix, and DocsLink.
  - [ ] Do not publish the full DocFX documentation site in this story; provide docs stubs or links that future docs stories can fill.
  - [ ] Keep deprecation-window policy references aligned with NFR77: minimum one minor version before removal.

- [ ] T8. Tests and verification (AC1-AC18)
  - [ ] Unit tests for inspect model classification, type matching, ambiguity, sorting, redaction, and JSON stability.
  - [ ] Integration tests with temporary projects proving generated files appear at the documented `obj/{Config}/{TFM}/generated/HexalithFrontComposer` path.
  - [ ] CLI tests for `inspect`, `inspect --type`, missing output, ambiguous TFM, build failure, warning/error filtering, and JSON output.
  - [ ] Code-fix tests using Microsoft.CodeAnalysis.Testing-style analyzer/code-fix verification for every automated migration.
  - [ ] Migration dry-run tests proving no file writes, deterministic diffs, manual-only reporting, and non-zero failure behavior.
  - [ ] Migration apply tests proving source edits compose correctly and generated/bin/obj/submodule paths are ignored.
  - [ ] Tool packaging test: `dotnet pack`, local tool install or `dotnet tool run`, and optional .NET 10 `dnx` smoke path when available in CI image.
  - [ ] Full regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.

---

## Dev Notes

### Existing SourceTools State

- `FrontComposerGenerator` is an `IIncrementalGenerator` in `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`.
- Current tracked discovery steps are `Parse`, `ParseCommand`, and `ParseProjectionTemplate`; inspect tooling must not disrupt these incremental cache tests.
- Projection outputs currently include Razor, Fluxor feature, actions, reducers, registration, shared template manifest, and shared MCP manifest.
- Command outputs currently include form, actions, lifecycle feature, registration, renderer, last-used subscriber, lifecycle bridge, optional full-page component, shared template manifest, and shared MCP manifest.
- Existing hint-name rules are namespace-qualified for projections and add `.Command` for command outputs. CLI classification should reuse this instead of inventing a parallel naming transform.
- `McpManifestEmitter.GeneratedHintName` is `FrontComposerMcpManifest.g.cs`; Story 9-2 should read or classify it for inspect summaries but must not change MCP runtime behavior.

### Package and Dependency Boundaries

- `Hexalith.FrontComposer.SourceTools` currently targets `netstandard2.0`, has `IsRoslynComponent=true`, and references `Microsoft.CodeAnalysis.CSharp` with `PrivateAssets="all"`.
- `Directory.Packages.props` pins `Microsoft.CodeAnalysis.CSharp` to `4.12.0` because higher versions can break IDE analyzer load context. Do not upgrade Roslyn broadly inside this story.
- If CLI or code-fix implementation needs `Microsoft.CodeAnalysis.Workspaces` or `Microsoft.CodeAnalysis.CSharp.Workspaces`, add them only to CLI/code-fix projects and pin them to the same Roslyn minor as SourceTools unless a documented build failure forces otherwise.
- Keep `System.CommandLine` or other CLI parser dependencies out unless they are stable and pinned. A small internal parser is acceptable for the initial two-command surface if it reduces dependency risk.

### Generated Output Path Contract

Story 9-2 makes this path visible to humans and scripts:

```text
obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs
```

The implementation may use MSBuild properties such as `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath`, or package-owned targets, but the resulting path must be tested as a public contract. Do not rely on current compiler temp paths or IDE-specific generated-source virtual paths.

### Migration Boundaries

- Automated migrations must be narrow, diagnostic-ID-driven, and reversible by normal source control review.
- Do not run broad source formatting, namespace cleanup, nullable sweeps, or semantic refactors unrelated to the selected migration diagnostics.
- Do not auto-edit API changes where adopter intent matters, such as policy naming, tenant model choices, custom renderer semantics, or cross-story architecture contracts. Emit manual guidance instead.
- Story 9-4 owns final diagnostic ID system and deprecation documentation policy. Story 9-2 can add the execution machinery and provisional migration entries needed for tests.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 9-1 | Story 9-2 | Generated-output path, baseline/diff library handoff, and drift diagnostics consumed by inspect/migrate. |
| Story 1-8 | Story 9-2 | Hot-reload/full rebuild messaging and NFR8 generator performance expectations. |
| Stories 1-4 through 1-5 | Story 9-2 | Parse/Transform/Emit IR split and deterministic hint-name conventions. |
| Stories 2-2 through 2-5 | Story 9-2 | Command output families, density, lifecycle bridge, destructive metadata, and form migration targets. |
| Stories 4-1 through 4-6 | Story 9-2 | Projection roles, DataGrid output, unsupported placeholders, empty-state CTA, badges, groups, and format annotations. |
| Stories 6-2 through 6-6 | Story 9-2 | Template/slot/view contract-version diagnostics and customization migration candidates. |
| Story 7-3 | Story 9-2 | Authorization policy diagnostics and manual migration boundaries. |
| Stories 8-1 through 8-6 | Story 9-2 | MCP manifest output, schema/version compatibility notes, and agent-facing descriptor inspection. |
| Story 9-4 | Story 9-2 | Final diagnostic docs pages, HFC ID governance, and deprecation policy links. |
| Story 9-5 | Story 9-2 | Public migration guides and DocFX publication. |

### Scope Guardrails

Do not implement these in Story 9-2:

- Build-time drift comparison itself. Owner: Story 9-1.
- Final public diagnostic documentation site. Owner: Story 9-4 / Story 9-5.
- IDE conformance matrix or IDE-specific test harness. Owner: Story 9-3.
- Broad PublicApiAnalyzers governance. Owner: Story 9-4.
- DocFX site generation. Owner: Story 9-5.
- MCP schema negotiation or lifecycle behavior changes. Owner: Stories 8-3 / 8-6.
- Recursive repository or nested submodule migration scanning.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Final diagnostic ID documentation pages for all migration diagnostics. | Story 9-4 |
| Public migration guide publication and DocFX navigation. | Story 9-5 |
| IDE integration and conformance claims for code-fix availability. | Story 9-3 |
| Visual/specimen migration validation for generated UI screenshots. | Story 10-2 |
| Broad mutation testing for migration fix providers. | Story 10-4 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-9-developer-tooling-documentation.md#Story-9.2`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md`] - generated-output, diagnostic, and baseline handoff constraints.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review vs. elicitation sequencing and hardening roles.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - source generator as infrastructure and diagnostic-first constraints.
- [Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`] - current incremental generator pipeline and hint-name outputs.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] - SourceTools target framework and Roslyn component packaging constraints.
- [Source: `Directory.Packages.props`] - Roslyn, MCP, test, and runtime package pins.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] - current HFC descriptor and HelpLinkUri pattern.
- [Source: `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`] - current HFC SourceTools allocation through HFC1057.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`] - MCP manifest generated hint and descriptor emission.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`] - generated tree count and hint-name regression tests.
- [Source: Microsoft Learn `Create a .NET tool`](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create) - `PackAsTool`, `ToolCommandName`, and packaging shape.
- [Source: Microsoft Learn `Install and use a .NET global tool`](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use) - global tool, local source, and .NET 10 `dnx` usage.
- [Source: Microsoft Learn `CodeFixProvider.RegisterCodeFixesAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider.registercodefixesasync) - Roslyn code-fix registration contract.
- [Source: Microsoft Learn `Write your first analyzer and code fix`](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix) - analyzer/code-fix project and test patterns.
- [Source: Microsoft Learn `Roslyn analyzers overview`](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview) - analyzers in IDE and CI workflows.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-02: Story created via `/bmad-create-story 9-2-cli-inspection-and-migration-tools` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
