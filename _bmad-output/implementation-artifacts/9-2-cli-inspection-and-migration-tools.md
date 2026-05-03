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
| AC19 | A developer or CI script consumes CLI output | Commands complete | Exit codes are stable: `0` success, `1` actionable HFC findings when the caller enables fail-on-findings behavior, `2` invalid arguments or ambiguous project/type/configuration, `3` missing/stale/unsupported generated output, and `4` apply/write/interruption failure. Plain HFC warnings do not make `inspect` fail unless `--fail-on-warning` or an equivalent explicit option is provided. |
| AC20 | A developer requests machine-readable output with `--format json` | Inspect or migrate renders output | JSON includes `schemaVersion`, deterministic sorted arrays, normalized project-relative paths, stable enum/string values, and redacted placeholders; it excludes timestamps, durations, ANSI color, localized prose, raw exceptions, and absolute paths unless an explicit diagnostic option is used. |
| AC21 | A command enumerates projects, TFMs, configurations, generated files, diagnostics, migration entries, or code-fix results | Output is rendered or fixes are planned | Ordering is deterministic by stable ordinal keys, path separators are normalized in machine output, and text output preserves the same logical order as JSON. |
| AC22 | The developer runs `frontcomposer migrate` without `--apply` | Migration planning executes | Dry-run is the default and writes no files; `--apply` is the only source-writing mode, and apply may write only files listed in the immediately computed operation plan. |
| AC23 | Migration apply encounters generated output, `bin/`, `obj/`, package caches, root-level submodules, nested submodule paths if present, linked files outside the project root, or unrelated repositories | Migration plans or applies fixes | The tool refuses those targets before writing, reports sanitized skipped/failed counts, does not initialize or update submodules, and never recursively scans nested submodule metadata. |
| AC24 | Migration has multiple safe fixes, manual-only entries, conflicts, or write failures | Migration completes or fails | Safe fixes for one file are composed through a single Roslyn solution/document operation where possible; manual-only entries are reported and never applied; conflicts are skipped with deterministic diagnostics; failures report changed/unchanged/skipped/failed counts and leave no partially corrupted target. |
| AC25 | CLI integration tests exercise inspect and migrate | The suite runs in CI | Tests use synthetic temporary workspaces from a shared fixture builder, not the repository's real generated output; fixtures cover single project, multi-TFM, Debug/Release, stale output, missing output, HFC diagnostics, root-level submodule exclusion, generated/bin/obj exclusion, outside-project paths, dry-run no-write, apply idempotency, conflict handling, manual-only migrations, write failures, and packaging/tool smoke commands. |
| AC26 | Migration apply resolves candidate write paths through symlinks, junctions, linked files, case variants, or paths that change between planning and write | The operation plan is validated | The tool canonicalizes paths before planning and immediately before writing, refuses targets outside the selected project/solution root or inside excluded directories/submodules after resolution, detects plan-vs-write path/hash drift, and aborts affected files without following attacker-controlled links. |
| AC27 | Generated type names, diagnostics, source snippets, file names, or migration guidance contain control characters, ANSI escape sequences, excessive length, JSON-looking text, or log-line delimiters | Text or JSON output is rendered | Output encodes or replaces unsafe characters, bounds every user-controlled field, preserves valid JSON shape, prevents terminal/control-sequence injection, and reports truncation deterministically without leaking the omitted raw value. |
| AC28 | A Roslyn code fix returns operations beyond ordinary solution/document edits, tries to add files outside the selected project, or exposes a Fix All action whose edits are not deterministic | Migration plans or applies the fix | The CLI inspects every `CodeActionOperation`, allows only FrontComposer-owned solution/document changes within the approved write set, rejects custom/external-process/unsupported operations, and treats unsafe Fix All providers as manual-only entries. |
| AC29 | A requested migration uses an unknown version, unsupported version order, ambiguous package train, or a multi-hop upgrade with no explicit migration edge | Migration runs | The tool fails closed before planning edits, reports the supported migration edges and docs link, emits no source changes, and uses manual guidance rather than guessing across versions. |

---

## Tasks / Subtasks

- [ ] T1. Define CLI project and package shape (AC1, AC4, AC7, AC16, AC17)
  - [ ] Add a new console project under `src/Hexalith.FrontComposer.Cli/` unless an existing tooling host is introduced before implementation.
  - [ ] Package it as a .NET tool with `<PackAsTool>true</PackAsTool>` and a stable command name, recommended `frontcomposer`.
  - [ ] Target the current shipping runtime TFM used by the repo; do not force SourceTools off `netstandard2.0`.
  - [ ] Keep CLI-only dependencies, MSBuild workspace dependencies, and console formatting dependencies out of `Hexalith.FrontComposer.SourceTools`.
  - [ ] Add package metadata so global and local tool installation share the lockstep FrontComposer version.
  - [ ] CI must pack the tool, install it from the local package output into a temporary local tool manifest, and smoke-test `frontcomposer --help`, `frontcomposer inspect --format json`, `frontcomposer migrate --dry-run`, and `frontcomposer migrate --apply` against fixtures.
  - [ ] Keep the .NET 10 `dnx` path optional and non-blocking unless the CI image already provides the required SDK.

- [ ] T2. Establish generated-output path contract (AC1, AC2, AC5, AC6, AC7)
  - [ ] Use the public path contract `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs` and sibling `.g.cs` files.
  - [ ] Wire the path through package-owned MSBuild props/targets or a documented generator-output option; avoid ad hoc path guesses in the CLI.
  - [ ] `inspect` must read the generated-output location from the canonical SourceTools/generator contract first; file-name parsing is a documented fallback for legacy output only.
  - [ ] Preserve existing generator hint-name semantics: namespace-qualified projection hints and `.Command` command hint prefixes.
  - [ ] Add a contract test proving generated output lands under the documented path for Debug/Release and at least one multi-targeted fixture.
  - [ ] When output is absent, distinguish "project has no FrontComposer types", "build not run", "generation failed", and "unsupported target framework".
  - [ ] Report resolved generated-output paths as normalized project-relative paths when inside the project and as redacted diagnostic-safe text when unavailable or unsafe.

- [ ] T3. Implement inspect model loading (AC1-AC8, AC15)
  - [ ] Build a small SDK-neutral inspect model: project identity, configuration, TFM, generated files, source family, related domain type, MCP entries, and diagnostics.
  - [ ] Classify generated files by suffix: `.g.razor.cs`, `Feature.g.cs`, `Actions.g.cs`, `Reducers.g.cs`, `Registration.g.cs`, `CommandForm.g.razor.cs`, `CommandRenderer.g.razor.cs`, `CommandLifecycleBridge.g.cs`, `FrontComposerMcpManifest.g.cs`, and template manifest files.
  - [ ] Prefer generated metadata produced by SourceTools when available; fall back to deterministic file-name parsing only when metadata is absent.
  - [ ] Sort output by bounded context, type FQN, source family, then path using ordinal comparison.
  - [ ] Redact machine-local path prefixes in default output; provide absolute paths only behind an explicit `--absolute-paths` diagnostic option.
  - [ ] Limit displayed diagnostics to FrontComposer-generated output and HFC-relevant project context; do not turn `inspect` into a general analyzer report.

- [ ] T4. Implement inspect command UX (AC1-AC8)
  - [ ] Support `frontcomposer inspect`, `frontcomposer inspect --summary`, `frontcomposer inspect --type <metadata-name>`, `--project`, `--solution`, `--configuration`, `--framework`, `--build`, `--format text|json`, `--severity`, and explicit fail-on-diagnostic options.
  - [ ] Keep text output concise and operator-readable; JSON output must be deterministic for CI snapshots.
  - [ ] JSON output must include `schemaVersion`, stable field names, sorted arrays, normalized project-relative paths, and redacted placeholders; do not emit timestamps, durations, colors, localized prose, raw exceptions, or machine-specific absolute paths by default.
  - [ ] Type matching accepts full metadata name first, then unambiguous simple type name; ambiguous simple names require the full name.
  - [ ] Include HFC diagnostic docs links when `DiagnosticDescriptor.HelpLinkUri` is available.
  - [ ] Non-zero exit codes follow AC19 and distinguish invalid arguments, ambiguous target/type, missing or stale generated output, build/generation failure, requested type not found, and explicit fail-on-findings behavior.
  - [ ] Sanitize text output and JSON string fields for control characters, ANSI escapes, line-delimiter injection, and overlong generated names/diagnostics; deterministic truncation must be visible without printing the raw omitted value.

- [ ] T5. Introduce migration/code-fix architecture (AC9-AC12)
  - [ ] Add a migration abstraction that maps `(fromVersion, toVersion, diagnosticId)` to a Roslyn code-fix provider or a manual migration note.
  - [ ] Keep analyzer/code-fix providers in a separate assembly if Workspaces packages are required; do not add Workspaces dependencies to the generator assembly unless party review explicitly approves it.
  - [ ] SourceTools may expose SDK-neutral inspection/migration primitives, but CLI orchestration, file walking, tool packaging, console UX, MSBuild Workspace usage, and `CodeFixProvider` execution stay in CLI or CLI-owned projects.
  - [ ] Execute only allowlisted FrontComposer-owned migration code-fix providers pinned to the repo's Roslyn `4.12.0` package family unless a documented build failure forces a narrow exception.
  - [ ] Use Roslyn `CodeFixProvider` patterns: declare `FixableDiagnosticIds`, register fixes through `RegisterCodeFixesAsync`, and provide Fix All only where edits are deterministic and conflict-free.
  - [ ] Inspect returned `CodeActionOperation` instances and allow only FrontComposer-owned solution/document edit operations within the computed write set; reject custom operations, process launches, unsupported file operations, and non-deterministic Fix All actions.
  - [ ] Support manual-only migration entries for changes that require product or architecture judgment.
  - [ ] Reserve migration-specific HFC IDs only after checking `AnalyzerReleases.Unshipped.md`; Story 9-4 owns final public diagnostic governance.
  - [ ] Model migration edges explicitly by package/version train; unknown, reversed, skipped, or ambiguous version requests fail closed before any edit planning.

- [ ] T6. Implement migration dry-run and apply modes (AC9-AC13, AC15)
  - [ ] Make `--dry-run` the default and documented migration mode; writing source files requires explicit `--apply`.
  - [ ] In dry-run, compute proposed changes and render unified diff or structured JSON without writing files.
  - [ ] In apply mode, compute the same operation plan immediately before writing; only files in that plan may be modified.
  - [ ] Compose Roslyn document changes first, detect overlapping edits, then write files in a deterministic order while preserving encoding and line endings where practical.
  - [ ] Never modify generated files, `obj/`, `bin/`, package caches, root-level submodules, nested submodule paths if present, vendored repositories, linked files outside the project root, or files outside the selected project/solution.
  - [ ] Do not initialize, update, or recurse into submodules; read root-level submodule boundaries only to exclude them from scan/write targets.
  - [ ] Canonicalize candidate paths before planning and immediately before write, including symlinks, junctions, case variants, and linked documents; refuse if the resolved target moves outside the approved write set or enters an excluded path.
  - [ ] Capture a pre-write content hash or equivalent stable snapshot for each planned source file and revalidate it immediately before writing; plan-vs-write drift aborts that file with exit code 4 and no partial overwrite.
  - [ ] Exit non-zero when any target file cannot be safely fixed, and report changed, unchanged, skipped, failed, manual-only, and conflict counts without hiding successfully planned fixes.

- [ ] T7. Add migration guide handoff (AC8, AC9, AC11, AC16)
  - [ ] Link each migration diagnostic to a future diagnostic/migration page path owned by Story 9-4 or Story 9-5.
  - [ ] Emit message fields shaped as What, Expected, Got, Fix, and DocsLink.
  - [ ] Do not publish the full DocFX documentation site in this story; provide docs stubs or links that future docs stories can fill.
  - [ ] Keep deprecation-window policy references aligned with NFR77: minimum one minor version before removal.

- [ ] T8. Tests and verification (AC1-AC25)
  - [ ] Unit tests for inspect model classification, type matching, ambiguity, sorting, redaction, exit-code mapping, and JSON stability.
  - [ ] CLI integration tests must use synthetic temporary workspaces from a shared fixture builder, not the repository's real generated output.
  - [ ] Fixtures normalize path separators, sort output deterministically, and assert repo-relative or redacted paths only.
  - [ ] Integration tests with temporary projects proving generated files appear at the documented `obj/{Config}/{TFM}/generated/HexalithFrontComposer` path for single-project, multi-TFM, Debug, Release, stale-output, missing-output, and HFC-diagnostic cases.
  - [ ] CLI tests for `inspect`, `inspect --type`, missing output, ambiguous TFM, build failure, warning/error filtering, and JSON output.
  - [ ] Code-fix tests using Microsoft.CodeAnalysis.Testing-style analyzer/code-fix verification for every automated migration.
  - [ ] Migration dry-run tests proving exit code, diagnostics, proposed file changes, no filesystem mutation, deterministic diffs, stable ordering, redacted paths, manual-only reporting, and non-zero failure behavior.
  - [ ] Migration apply tests proving exact file diffs, source edits compose correctly, second-run idempotency, conflict handling, manual-only cases, write failure behavior, and no writes to generated/bin/obj/package-cache/submodule/outside-project paths.
  - [ ] Add output-injection tests for generated names, diagnostics, snippets, and migration guidance containing ANSI escapes, control characters, JSON-like payloads, very long strings, and line delimiters.
  - [ ] Add code-action safety tests where providers return custom operations, unsupported file operations, outside-project additions, and unsafe Fix All results; all must be rejected or reported manual-only without file writes.
  - [ ] Add migration catalog tests for unknown versions, reversed versions, ambiguous package trains, missing edges, and explicit multi-hop refusal.
  - [ ] Add TOCTOU path tests that change a symlink/junction/linked-file target or file content between planning and write; apply must detect drift and abort the affected file.
  - [ ] Add negative write-protection fixtures for root-level submodules, nested submodule paths if present, linked outside-project files where supported by the OS, generated output, `bin/`, and `obj/`; refusal messages must not leak absolute user paths.
  - [ ] Add a bounded large-fixture or benchmark-style integration test proving inspect/migrate avoid unnecessary repeated full-tree work and complete within an agreed CI threshold.
  - [ ] Tool packaging test: `dotnet pack`, local tool install or `dotnet tool run`, and optional .NET 10 `dnx` smoke path when available in CI image.
  - [ ] Full regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Capture exact verification commands in the Dev Agent Record: test command(s), packaging smoke, CLI smoke commands, and full regression build.

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
- `Hexalith.FrontComposer.SourceTools` must remain free of CLI entry points, MSBuild Workspace dependencies, console rendering, file-system scanning, dotnet tool packaging, and source-writing migration orchestration.
- Migration execution must load only allowlisted FrontComposer-owned code-fix providers; do not dynamically execute arbitrary analyzer/code-fix assemblies from the target solution.

### CLI Contract

Required command forms for this story:

```text
frontcomposer inspect
frontcomposer inspect --summary
frontcomposer inspect --type <fully-qualified-type-name>
frontcomposer migrate --from <version> --to <version> --dry-run
frontcomposer migrate --from <version> --to <version> --apply
```

Type matching resolves exact metadata names first, then unambiguous simple names. Ambiguous simple names exit non-zero and list bounded project-relative candidates. `migrate` defaults to dry-run when neither `--dry-run` nor `--apply` is supplied.

Exit codes:

| Code | Meaning |
| --- | --- |
| 0 | Success; no requested failure condition. |
| 1 | Actionable HFC findings when the caller explicitly enables fail-on-findings behavior. |
| 2 | Invalid arguments, ambiguous target, ambiguous type, or ambiguous configuration/framework. |
| 3 | Missing, stale, unsupported, or failed generated-output discovery. |
| 4 | Apply/write/interruption failure. |

Machine-readable JSON is a versioned contract with `schemaVersion`, stable field names, sorted arrays, normalized project-relative paths, and redacted placeholders. JSON must not contain ANSI color, timestamps, durations, localized prose, raw exception text, or machine-specific absolute paths by default.

### Generated Output Path Contract

Story 9-2 makes this path visible to humans and scripts:

```text
obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs
```

The implementation may use MSBuild properties such as `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath`, or package-owned targets, but the resulting path must be tested as a public contract. Do not rely on current compiler temp paths or IDE-specific generated-source virtual paths.

The CLI should consume the canonical SourceTools/generator output metadata or package-owned path property first. Deterministic file-name parsing is a fallback only when metadata is absent, and fallback behavior must be documented in test names and user-facing diagnostics.

### Migration Boundaries

- Automated migrations must be narrow, diagnostic-ID-driven, and reversible by normal source control review.
- Do not run broad source formatting, namespace cleanup, nullable sweeps, or semantic refactors unrelated to the selected migration diagnostics.
- Do not auto-edit API changes where adopter intent matters, such as policy naming, tenant model choices, custom renderer semantics, or cross-story architecture contracts. Emit manual guidance instead.
- Story 9-4 owns final diagnostic ID system and deprecation documentation policy. Story 9-2 can add the execution machinery and provisional migration entries needed for tests.
- `--apply` may write only files listed in the immediately computed operation plan. It must refuse generated output, `bin/`, `obj/`, package caches, root-level submodules, nested submodule paths if encountered, linked files resolving outside the project root, and unrelated repository paths.
- Submodule handling is exclusion-only. The CLI must not initialize, update, or recursively inspect submodule metadata; it may read root-level submodule boundaries only to keep scan/write targets outside them.
- Manual-only entries are never applied. Conflicting safe fixes are skipped with deterministic diagnostics rather than applied partially.

### Advanced Elicitation Hardening

These hardening points were applied by `/bmad-advanced-elicitation 9-2-cli-inspection-and-migration-tools` on 2026-05-03 and refine the party-mode contract without expanding product scope.

| Area | Hardening |
| --- | --- |
| Path safety | Migration apply validates canonical resolved paths during planning and again immediately before writing, including symlinks, junctions, linked files, case variants, and excluded submodule/bin/obj/generated locations. |
| TOCTOU protection | Apply captures a stable source snapshot or hash for every planned file and aborts that file if content or resolved target changes before write. |
| Output injection | Text and JSON renderers sanitize control characters, ANSI escapes, line delimiters, JSON-looking payloads, and overlong generated names/diagnostics before display or persistence. |
| Code action execution | Migration inspects Roslyn `CodeActionOperation` outputs and allows only approved solution/document edits from FrontComposer-owned providers inside the computed write set. |
| Version catalog | Migration edges are explicit and fail closed for unknown, reversed, ambiguous, skipped, or unsupported version paths. |

### Party-Mode Review Clarifications

These clarifications were applied by `/bmad-party-mode 9-2-cli-inspection-and-migration-tools; review;` on 2026-05-03 and are part of the pre-dev contract.

| Area | Clarification |
| --- | --- |
| CLI/SourceTools boundary | `SourceTools` can expose SDK-neutral generated-output facts, but CLI orchestration, MSBuild Workspace usage, console rendering, packaging, file walking, and source-writing migration execution stay outside the generator assembly. |
| CLI contract | `inspect`, `inspect --summary`, `inspect --type`, `migrate --dry-run`, and `migrate --apply` need stable exit codes, deterministic ordering, and explicit type/configuration ambiguity behavior before implementation. |
| JSON contract | Machine output is versioned with `schemaVersion`, sorted arrays, normalized project-relative paths, and redacted placeholders; no timestamps, durations, colors, localized prose, raw exceptions, or absolute user paths by default. |
| Migration safety | Dry-run is the default; apply requires `--apply`, recomputes the operation plan immediately before writing, modifies only planned source files, composes Roslyn document changes, and reports changed/unchanged/skipped/failed/manual/conflict counts. |
| Write exclusions | Migration refuses generated output, `bin/`, `obj/`, package caches, root-level submodules, nested submodule paths if present, linked outside-project files, and unrelated repositories without initializing or updating submodules. |
| Fixture strategy | CLI tests use synthetic temporary workspaces from a shared fixture builder, not current repo artifacts, with normalized paths, filesystem mutation oracles, apply idempotency, and packaging smoke coverage. |

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
- Dynamic execution of arbitrary target-solution analyzer/code-fix assemblies.
- Broad source formatting, nullable sweeps, namespace cleanup, or semantic refactors outside allowlisted migration diagnostics.

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
- 2026-05-03: Party-mode review completed via `/bmad-party-mode 9-2-cli-inspection-and-migration-tools; review;`. Applied CLI/SourceTools boundary, exit-code, JSON schema, deterministic ordering, dry-run/apply, submodule exclusion, write-protection, fixture-oracle, packaging smoke, and scope-guardrail hardening. Ready for advanced elicitation on a later run.
- 2026-05-03: Advanced elicitation completed via `/bmad-advanced-elicitation 9-2-cli-inspection-and-migration-tools`. Applied path canonicalization and TOCTOU write checks, output-injection sanitization, Roslyn code-action operation allowlisting, explicit migration-edge validation, and matching test coverage. Ready for development.

### Party-Mode Review

- **Date/time:** 2026-05-03T09:27:12+02:00
- **Selected story key:** `9-2-cli-inspection-and-migration-tools`
- **Command/skill invocation used:** `/bmad-party-mode 9-2-cli-inspection-and-migration-tools; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found the adopter workflow valuable and correctly scoped, but implementation needed sharper contracts before development: CLI/SourceTools dependency direction, stable exit codes, machine-readable JSON schema, deterministic ordering, default dry-run semantics, safe apply planning, submodule/write exclusion, allowlisted code-fix execution, synthetic fixture architecture, and explicit CI evidence.
- **Changes applied:** Added AC19-AC25; hardened T1-T8; added CLI Contract and Party-Mode Review Clarifications; tightened Package and Dependency Boundaries, Generated Output Path Contract, Migration Boundaries, Scope Guardrails, completion notes, and review trace.
- **Findings deferred:** Advanced security/robustness edge-case probing remains for a later advanced elicitation run; drift comparison remains Story 9-1; IDE conformance remains Story 9-3; diagnostic governance and public docs remain Stories 9-4/9-5; visual/specimen validation remains Story 10-2; broad mutation testing remains Story 10-4; optional .NET 10 `dnx` is non-blocking unless CI already has the SDK.
- **Final recommendation:** ready-for-dev

### Advanced Elicitation

- **Date/time:** 2026-05-03T09:42:13+02:00
- **Selected story key:** `9-2-cli-inspection-and-migration-tools`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 9-2-cli-inspection-and-migration-tools`
- **Batch 1 method names:** Pre-mortem Analysis; Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Occam's Razor Application.
- **Reshuffled Batch 2 method names:** First Principles Analysis; Comparative Analysis Matrix; Chaos Monkey Scenarios; Architecture Decision Records; Hindsight Reflection.
- **Findings summary:** The elicitation found the party-mode version strong on scope and boundaries, but still under-specified several failure-prone implementation details: path canonicalization across symlinks/junctions/linked files, plan-vs-write drift, terminal/control-sequence injection in CLI output, unsafe Roslyn `CodeActionOperation` execution, and ambiguous version-edge planning.
- **Changes applied:** Added AC26-AC29; hardened T4-T8; added Advanced Elicitation Hardening; updated completion notes and trace. The accepted changes stay within the existing inspect/migrate scope and clarify safety oracles for development.
- **Findings deferred:** No product-scope or architecture-policy changes were applied. Broader IDE parity remains Story 9-3; final diagnostic governance and public migration docs remain Stories 9-4/9-5; visual/specimen validation remains Story 10-2; broad mutation testing remains Story 10-4.
- **Final recommendation:** ready-for-dev

### File List

(to be filled in by dev agent)
