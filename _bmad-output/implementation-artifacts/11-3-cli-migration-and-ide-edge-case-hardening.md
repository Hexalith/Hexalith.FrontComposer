# Story 11.3: CLI, Migration, and IDE Edge-Case Hardening

Status: ready-for-dev

> **Epic 11** - Deferred Hardening & Release Readiness. Closes CLI migration, inspect/help, IDE manifest, path normalization, sidecar, write-safety, and generator-debug follow-ups routed from Stories 9.2 and 9.3. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11-3 is the release-readiness hardening pass for the `frontcomposer` CLI and IDE parity evidence surfaces.

Story 9-2 delivered the CLI inspect/migrate tool, source-safe migration planning, project-relative JSON output, redaction, and package smoke coverage. Story 9-3 delivered the IDE parity matrix, evidence manifests, generated-output path contract checks, version revalidation, and contributor debugging guidance. Later reviews deferred a bounded set of edge cases around path canonicalization, solution parsing, strict manifest parsing, sidecar normalization, write/error behavior, help/reference clarity, and whether some low-probability findings should remain explicit accepted constraints.

This story implements or consciously rejects those follow-ups with evidence. The intended outcome is that release tooling behaves predictably outside the happy path, and remaining limitations are named in help/reference material instead of being hidden in old review notes.

---

## Story

As a developer,
I want the CLI migration and IDE parity surfaces hardened against documented edge cases,
so that release tooling behaves predictably outside the happy path.

### Release-Readiness Job To Preserve

A maintainer preparing a release candidate should be able to run the focused CLI and IDE conformance tests, review the docs/help output, and know which deferred CLI/IDE edge cases are fixed, accepted as constraints, or split to another owner with explicit evidence.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary CLI files | Harden `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`, `MigrationCommand.cs`, `InspectCommand.cs`, `PathUtilities.cs`, `Program.cs`, `OutputSanitizer.cs`, and CLI README/reference docs only where needed. |
| Primary IDE files | Harden `docs/ide-parity-matrix.json`, `docs/ide-parity-matrix.md`, `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/*`, `jobs/ide-parity-version-revalidation.ps1`, and `CONTRIBUTING.md` only where 9.3 deferred rows require it. |
| Deferred ledger | Close or explicitly accept `DEF-9-2-*`, `DEF-9-3-*`, and any Story 11.3-routed migration/IDE rows in `_bmad-output/implementation-artifacts/deferred-work.md`. |
| CLI behavior | Prefer fixed fail-closed behavior for path safety, parser strictness, sidecar normalization, and user-facing error clarity. |
| Accepted constraints | Low-probability edge cases may remain accepted only when README/help/reference docs and tests prove the behavior is intentional and bounded. |
| HFCM boundary | Do not solve the HFCM registry/release-row governance owned by Story 11.2, except to keep CLI-side behavior compatible with that owner. |
| Scope guardrail | Do not reopen SourceTools drift detection, diagnostic registry governance, MCP/schema negotiation, shell UX, EventStore reliability, or release-pipeline credential work. |
| Validation | Focus on `Hexalith.FrontComposer.Cli.Tests`, IDE parity contract tests, docs/reference checks touched by this story, and `deferred-work.md` evidence. |

Start here: T1 inventory and classify deferred rows -> T2 fix CLI path/project/write/error edge cases -> T3 tighten sidecar and JSON/help contracts -> T4 harden IDE manifest parsing and generator-debug enforcement -> T5 update docs/ledger/evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Deferred rows `DEF-9-2-*`, `DEF-9-3-*`, and Story 11.3-routed migration/IDE rows exist in `deferred-work.md` | Story 11.3 completes | Each row is marked resolved, superseded, split, or accepted with evidence, owner, and date; no row is silently dropped. |
| AC2 | A developer passes `--project`, `--solution`, generated-output paths, linked files, symlinked paths, junctions, case variants, or drive-relative sidecar paths | Inspect or migrate plans work | Paths are canonicalized before trust decisions, normalized for output, refused when they escape the selected root or excluded folders, and tested on the current platform where feasible. |
| AC3 | The CLI parses `.sln`, `.slnx`, `.csproj`, or `.fsproj` inputs | Project selection runs | Supported formats are handled with deterministic errors for malformed quoting, unsupported project types, ambiguous matches, and unsupported solution shapes; unsupported formats are documented fail-closed behavior if not implemented. |
| AC4 | Migration apply reads project documents or generated sidecar paths | The operation plans or writes files | Generated output, `bin`, `obj`, package caches, root-level submodules, nested submodule paths, outside-root linked files, unrelated repositories, and path drift are refused before writing. |
| AC5 | Migration apply writes source | A write, cancellation, or IO failure occurs | Atomic temp-and-replace behavior remains intact, partial writes are not hidden, result JSON/text reports changed/unchanged/skipped/failed/manual-only counts, and failures produce a non-success outcome. |
| AC6 | CLI migration loads Roslyn workspaces or MEF services | Composition or workspace setup fails | The command surfaces a bounded, sanitized, user-actionable error such as "Workspaces assemblies failed to load" instead of raw exception dumps or generic IO failure. |
| AC7 | Migration catalog edges are registered | The catalog initializes or resolves an edge | Duplicate `(fromVersion,toVersion)` edges fail with a startup/catalog validation error that names the edge, not a latent `SingleOrDefault` or static-initializer surprise. |
| AC8 | `frontcomposer inspect` uses `--fail-on-warning`, `--fail-on-error`, or JSON output | Help, README, text output, and JSON output are rendered | Flag precedence and applied fail behavior are documented and machine-readable; no consumer must infer it from source. |
| AC9 | `frontcomposer migrate --format json` emits paths, diff bodies, diagnostics, or manual-only entries | Output is rendered | JSON fields remain schema-stable, bounded, redacted, project-relative where possible, and explicit about known diff applicability limitations. |
| AC10 | The CLI renders unified diffs containing control characters, ANSI escapes, multiline strings, or large edits | Text or JSON diff output is produced | Output remains safe for terminals/logs and clearly states whether the diff is informational or patch-applicable; accepted non-applicable limits are documented. |
| AC11 | `MigrationDiagnosticSidecarReader` reads malformed, unreadable, drive-relative, traversal, case-variant, or duplicate sidecar paths | Migration planning runs | Sidecar failures produce deterministic sentinel/manual-only entries or fail-closed skips without raw path leaks, silent drops, or duplicate casing surprises. |
| AC12 | Source files use uncommon encodings such as BOM variants, strict UTF-8 failures, or very large files | Migration reads files | Supported encodings are documented and tested; unsupported encodings and excessive size conditions fail closed with sanitized guidance instead of corrupting output. |
| AC13 | CLI receives Ctrl+C, repeated Ctrl+C, or platform-specific termination during migration | The process handles cancellation | Cancellation behavior is deterministic, does not throw from disposed handlers, preserves already-written evidence, and documents whether SIGTERM or second-press force-exit is in scope. |
| AC14 | CLI tool packaging smoke tests run | `dotnet` is absent, hangs, or pack/install mutates output | Tests skip or fail deterministically, use bounded timeouts, and avoid hiding live-tree mutation risks. |
| AC15 | IDE parity matrix or evidence manifest JSON contains unknown properties, duplicate keys, BOM/trailing-comma variants, traversal paths, or unsupported URI schemes | Validation runs | Parsing is strict enough to reject duplicate keys and unsupported fields with friendly category messages, while preserving fail-closed behavior for malformed JSON. |
| AC16 | IDE parity dry-run issue generation writes `$OutPath` | Two runs or hostile paths are attempted | Writes are atomic, repository-bounded when invoked from release scripts, and documented as serial release-gate behavior if parallel execution remains out of scope. |
| AC17 | `CONTRIBUTING.md` documents `Debugger.Launch()` source-generator debugging | Validation runs | The project either enforces the guidance through a grep/analyzer-style check or explicitly records why documentation-only guidance is accepted for v1. |
| AC18 | CLI/IDE hardening touches docs or public reference pages | Docs are updated | `src/Hexalith.FrontComposer.Cli/README.md`, `docs/reference/cli.md`, `docs/reference/ide-parity.md`, and related help text agree on exit codes, JSON fields, path semantics, diff limitations, and known unsupported cases. |
| AC19 | Story 11.2 owns diagnostic/HFCM governance | Story 11.3 changes CLI-side migration behavior | HFCM registry/release-row relocation remains outside Story 11.3 unless the change is only a compatibility handoff; cross-story handoff is recorded explicitly. |
| AC20 | Validation completes | Story 11.3 moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved accepted constraints, and evidence paths. |

---

## Tasks / Subtasks

- [ ] T1. Inventory and classify the Story 11.3 deferred rows (AC1, AC19, AC20)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [ ] Capture every unresolved `DEF-9-2-*` and `DEF-9-3-*` row plus Story 11.3-routed rows such as CLI path/help/sidecar/manifest/debug findings.
  - [ ] Classify each row as fix now, document accepted constraint, split to Story 11.2, or split to another Epic 11 story.
  - [ ] Preserve historical review text; add resolution markers rather than deleting rows.

- [ ] T2. Harden CLI project selection and path boundaries (AC2-AC4)
  - [ ] Review `ProjectSelection.cs` for `--project`, `--solution`, `.sln`, `.slnx`, `.csproj`, and `.fsproj` behavior.
  - [ ] Decide whether `.slnx` and `.fsproj` are supported in v1; if not, add explicit user-facing errors and docs.
  - [ ] Add tests for quoted solution paths, escaped quotes where feasible, ambiguous matches, unsupported formats, and symlink/junction canonicalization.
  - [ ] Ensure canonicalization uses `PathUtilities` consistently before trust decisions and output normalization.
  - [ ] Confirm migration write policy still excludes generated output, `bin`, `obj`, package caches, root-level submodules, nested submodule paths, outside-root linked files, and unrelated repositories.

- [ ] T3. Harden migration catalog, workspace setup, apply result, and file IO behavior (AC5-AC7, AC10, AC12-AC14)
  - [ ] Replace latent `SingleOrDefault` duplicate-edge behavior with explicit catalog uniqueness validation for `(fromVersion,toVersion)`.
  - [ ] Surface `MefHostServices` or workspace composition failures as bounded CLI errors.
  - [ ] Confirm `MigrationResult.Applied` or equivalent success field is false when any file write fails.
  - [ ] Verify atomic temp-and-replace writes remain same-directory and preserve encoding/line endings where supported.
  - [ ] Document and test strict UTF-8 / BOM handling, drive-root canonicalization, and excessive-file-size behavior as fixed or accepted.
  - [ ] Revisit Ctrl+C double-press and SIGTERM handling; implement only if it stays low-risk and testable.
  - [ ] Keep tool packaging smoke tests bounded with skip/timeout behavior and documented live-tree build isolation constraints.

- [ ] T4. Harden sidecar, diff, sanitizer, and JSON/help semantics (AC8-AC11, AC18)
  - [ ] Add README/reference/help coverage for `--fail-on-warning` versus `--fail-on-error` precedence and JSON `applied`/fail behavior.
  - [ ] Add or update JSON schema notes for inspect and migrate payloads: path relativity, redaction, exit code mapping, warning/manual-only/failure fields, and known limitations.
  - [ ] Decide whether unified diffs are terminal-safe only or patch-applicable; document the exact contract and keep tests aligned.
  - [ ] Add sidecar tests for drive-relative paths (`C:foo.cs`), traversal, duplicate/case-variant paths, unreadable files, malformed JSON, and unsafe source path reporting.
  - [ ] Ensure `OutputSanitizer` and diff rendering bound user-controlled fields without leaking raw omitted content.

- [ ] T5. Harden IDE parity manifest parsing and evidence generation (AC15-AC18)
  - [ ] Add strict duplicate-key and unknown-field detection for `docs/ide-parity-matrix.json` and evidence manifests.
  - [ ] Preserve strict trailing-comma rejection but add a friendly failure category or test name so maintainers understand the failure.
  - [ ] Revisit `IdeParityReportSanitizer` ESC replacement only if downstream JSON decoding consumes its output; otherwise mark accepted with evidence.
  - [ ] Evaluate `IdeParityRepositoryRoot` ancestor walk through symlinked `AppContext.BaseDirectory`; fix or document the CI assumption.
  - [ ] Confirm `jobs/ide-parity-version-revalidation.ps1` writes `$OutPath` atomically and cannot escape expected artifact roots in release usage.
  - [ ] Add a validation check for unconditional `Debugger.Launch()` in source-generator paths or record documentation-only acceptance with a grep command.

- [ ] T6. Update public docs and ledger evidence (AC1, AC8, AC9, AC13, AC18-AC20)
  - [ ] Update `src/Hexalith.FrontComposer.Cli/README.md` and `docs/reference/cli.md` so package README and public docs agree.
  - [ ] Update `docs/reference/ide-parity.md` or `docs/ide-parity-matrix.md` only for story-owned manifest/evidence behavior.
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` with resolution/acceptance/split markers for all Story 11.3 rows.
  - [ ] Record exact validation commands and outcomes in this story's Dev Agent Record.
  - [ ] Move Story 11.3 to `review` only after implementation and validation evidence are complete.

---

## Dev Notes

### Current State

- Epic 11 routes deferred release-readiness work into seven backlog stories. Story 11.3 owns CLI migration, IDE parity, manifest parsing, path normalization, sidecar behavior, and help/README edge cases.
- The CLI project is `src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj`, targets `net10.0`, is packaged as a .NET tool named `frontcomposer`, and keeps Roslyn Workspaces dependencies out of `SourceTools`.
- CLI tests live in `tests/Hexalith.FrontComposer.Cli.Tests` and currently cover inspect, migrate, sanitizer, help, fixtures, and package smoke behavior.
- IDE parity tests live in `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity`, with matrix data in `docs/ide-parity-matrix.json` and public narrative in `docs/ide-parity-matrix.md`.
- The public CLI reference page exists at `docs/reference/cli.md`, but its exit-code table currently says only `0`, `1`, and `2`; Story 9.2 acceptance criteria and CLI code use a richer exit-code model.
- The package README documents project-relative migration JSON paths, conservative imported `Compile` handling, atomic apply writes, and synthetic-only HFCM9002 sidecar behavior.
- Story 11.2 owns diagnostic registry, HFCM release-row strategy, RS2002 suppression, diagnostic docs slug/schema/sample validation, and compatibility suppression governance. Story 11.3 must not silently take that scope.

### Deferred Rows To Close Or Accept

| Deferred ID | Required Story 11.3 treatment |
| --- | --- |
| DEF-9-2-1 | Document and test `--fail-on-warning` vs `--fail-on-error` precedence in help/README/JSON. |
| DEF-9-2-5 | Replace latent duplicate migration edge behavior with explicit catalog validation. |
| DEF-9-2-7 | Canonicalize `--project`/selection through symlink/junction paths before downstream trust, or document why downstream write policy is sufficient. |
| DEF-9-2-8 | Fix or explicitly reject `.sln` escaped-quote parsing gaps with tests. |
| DEF-9-2-9 | Decide support for `.slnx` and `.fsproj`; Story 11.3 is the current owner despite the old Story 9.3 label. |
| DEF-9-2-10 | Keep `formattingApplied=false` only if README/JSON says no formatter runs yet, or implement formatting for real fixes. |
| DEF-9-2-11 | Decide whether SIGTERM handling is needed for v1 developer workflows. |
| DEF-9-2-13 | Surface MEF/workspace composition failures cleanly. |
| DEF-9-2-15 | Fix or accept `.gitmodules` escaped path limitations. |
| DEF-9-2-16 | Broaden `PathUtilities.Canonical` exception handling for long/invalid/unsupported paths. |
| DEF-9-2-17 | Decide Ctrl+C double-press force-exit behavior. |
| DEF-9-2-18 | Correct README/reference framing around JSON path/schema semantics. |
| DEF-9-2-19 | Document diff applicability vs terminal/log safety and test the chosen behavior. |
| DEF-9-2-20 | Add caller-side comment/docs for the 256-character Levenshtein cap or accept with evidence. |
| DEF-9-2-21 | Document bounded small-diff contract or improve `UnifiedDiff.DiffOps` for wider-separated edits. |
| DEF-9-2-22 | Document strict UTF-8 failure behavior and add focused tests if not already present. |
| DEF-9-2-23 | Accept or fix UTF-32 LE BOM collision behavior with evidence. |
| DEF-9-2-24 | Move duplicate-edge failure out of static initializer surprise into explicit validation. |
| DEF-9-2-26 | Accept drive-root canonical edge only if tests/docs prove drive roots are impossible candidates. |
| DEF-9-2-27 | Accept huge source-file OOM risk only with size guard or documented realistic limit. |
| DEF-9-2-28 | Accept PATHEXT casing only with platform-specific rationale or fix cheaply. |
| DEF-9-2-29 | Fix drive-relative sidecar path handling or emit deterministic sentinel behavior. |
| DEF-9-3-1 | Keep ESC sanitizer behavior only if downstream consumers are not JSON-decoding the escaped value; otherwise fix. |
| DEF-9-3-2 | Enforce or explicitly accept documentation-only `Debugger.Launch()` guidance. |
| DEF-9-3-3 | Confirm `OutPath` atomic write and serial release-gate assumptions. |
| DEF-9-3-4 | Fix or document symlinked `AppContext.BaseDirectory` repository-root walk behavior. |
| DEF-9-3-5 | Add duplicate-key and unknown-field rejection for matrix/evidence JSON. |
| DEF-9-3-6 | Preserve strict trailing-comma rejection but improve the failure category/message. |

### Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.3 owns behavior hardening and explicit acceptance of CLI/IDE edge cases, not broad feature expansion. | Keeps Epic 11 release readiness bounded and applies L06/L07. |
| D2 | A deferred row can close as an accepted constraint only when docs/tests name the behavior and prove it is safe enough for v1. | Avoids false closure while not forcing low-value edge fixes. |
| D3 | CLI path decisions must happen before write or trust boundaries, not only when formatting output. | Path normalization after planning is too late for safety. |
| D4 | Public help/reference docs are part of the contract for accepted limitations. | If a developer can hit it, the CLI must not require reading old story reviews. |
| D5 | IDE manifest validation should be strict and category-rich. | Silent duplicate-key or unknown-field tolerance defeats release evidence integrity. |
| D6 | HFCM governance remains a Story 11.2 contract. | Story 11.3 can keep CLI sidecar behavior compatible, but registry/release-row ownership stays with diagnostics governance. |
| D7 | Root-level submodules may be read as boundaries only; nested submodules must not be initialized or updated. | Preserves repository guardrails and avoids changing submodule state during tooling tests. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs` | Update likely | Project/solution parsing, format support, symlink/junction canonicalization. |
| `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs` | Update likely | Catalog validation, workspace errors, sidecar normalization, source IO, diff behavior, apply results, submodule parsing. |
| `src/Hexalith.FrontComposer.Cli/PathUtilities.cs` | Update likely | Broader canonicalization exception handling and edge-case path rules. |
| `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` | Update possible | Fail flag docs/JSON, Levenshtein cap comment, inspect output behavior. |
| `src/Hexalith.FrontComposer.Cli/Program.cs` | Update possible | Ctrl+C/SIGTERM behavior if selected for implementation. |
| `src/Hexalith.FrontComposer.Cli/OutputSanitizer.cs` | Update possible | Diff/control-character behavior if contract changes. |
| `src/Hexalith.FrontComposer.Cli/README.md` | Update likely | CLI help, JSON schema/path semantics, exit codes, limitations. |
| `docs/reference/cli.md` | Update likely | Public reference must match package README/help semantics. |
| `docs/reference/ide-parity.md` | Update possible | Public reference for strict manifest/evidence behavior. |
| `docs/ide-parity-matrix.json` / `docs/ide-parity-matrix.md` | Update possible | Matrix schema/manifest contract if strict parser fields change. |
| `jobs/ide-parity-version-revalidation.ps1` | Update possible | `$OutPath` bounds/atomic behavior and release-gate evidence. |
| `CONTRIBUTING.md` | Update possible | Generator-debug guidance enforcement/accepted constraint. |
| `tests/Hexalith.FrontComposer.Cli.Tests/*` | Update likely | CLI regression coverage. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/*` | Update likely | Manifest/evidence strict parsing and sanitizer tests. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.3-owned rows resolved/accepted/split after implementation. |
| `_bmad-output/implementation-artifacts/11-3-cli-migration-and-ide-edge-case-hardening.md` | Update | Dev Agent Record, validation, file list, completion notes. |

### Project Structure Notes

- Keep CLI-only Roslyn Workspaces dependencies inside `Hexalith.FrontComposer.Cli`; do not move them into `Hexalith.FrontComposer.SourceTools`.
- `Hexalith.FrontComposer.SourceTools` remains a Roslyn analyzer/source-generator project targeting `netstandard2.0`; CLI and IDE parity hardening must not add runtime-only APIs there.
- Package versions are centrally managed in `Directory.Packages.props`. Do not add inline `Version=` metadata.
- CLI tests belong in `tests/Hexalith.FrontComposer.Cli.Tests`; IDE parity governance tests remain in `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity`.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`. Do not initialize or update nested submodules; tests may read `.gitmodules` only to exclude boundaries.
- Avoid regenerating `docs/_site` unless docs generation is explicitly part of the implementation evidence.

### Testing Strategy

- Run focused CLI tests first:
  - `dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Release`
- Run focused IDE parity tests if manifest/evidence behavior changes:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~IdeParity"`
- Run docs validation if public docs/reference pages change:
  - `pwsh ./eng/validate-docs.ps1`
- Run package smoke tests when CLI packaging behavior changes; keep timeouts bounded and avoid recursive submodule operations.
- For final release-confidence, run the main lane if time allows:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 9.2 CLI inspect/migrate | Story 11.3 | CLI behavior, JSON schemas, path safety, migration sidecars, help text, and package smoke tests are hardened or explicitly accepted. |
| Story 9.3 IDE parity | Story 11.3 | Matrix/evidence parsing, repository-root discovery, sanitizer behavior, version revalidation, and generator-debug guidance are hardened or explicitly accepted. |
| Story 11.1 ledger reconciliation | Story 11.3 | Deferred rows routed to Story 11.3 must be closed with evidence or accepted constraints. |
| Story 11.2 diagnostic governance | Story 11.3 | HFCM registry/release-row strategy is not owned here; CLI sidecar behavior must remain compatible with the governance path. |
| Story 11.4 drift/generator hardening | Story 11.3 | SourceTools generator behavior and drift diagnostics remain separate unless the change is only CLI/IDE consumer validation. |
| Story 11.7 CI/release governance | Story 11.3 | Release automation may consume CLI/IDE checks later; this story supplies deterministic local gates and evidence. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| HFCM migration ID registry/release-row relocation and RS2002 suppression strategy. | Story 11.2 |
| SourceTools drift detection, PublishAot diagnostic behavior, and generated-output coverage gaps. | Story 11.4 |
| Public docs site UX polish outside CLI/IDE reference corrections. | Story 11.6 |
| CI release orchestration that consumes CLI/IDE release gates. | Story 11.7 |
| Custom IDE extensions or vendor-specific automation beyond matrix/evidence validation. | Product/architecture decision after Story 11.3 evidence |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.3`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-2-cli-inspection-and-migration-tools`] - CLI deferred rows from Story 9.2 reviews.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-9-3-ide-parity-and-developer-experience`] - IDE parity deferred rows.
- [Source: `_bmad-output/implementation-artifacts/9-2-cli-inspection-and-migration-tools.md`] - original CLI inspect/migrate story, review patches, and known gaps.
- [Source: `_bmad-output/implementation-artifacts/9-3-ide-parity-and-developer-experience.md`] - original IDE parity story, matrix/evidence contracts, and hardening traces.
- [Source: `_bmad-output/implementation-artifacts/11-1-deferred-work-ledger-reconciliation-and-ownership.md`] - Epic 11 routing and ledger reconciliation contract.
- [Source: `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md`] - diagnostic/HFCM governance boundary to preserve.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-10.md`] - Correct Course rationale for Epic 11.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR63-FR65`] - CLI inspect, CLI migration, and IDE parity product requirements.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md`] - developer tooling, migration guide, IDE matrix, and generated-output path context.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - SourceTools, IDE performance, diagnostic, and submodule constraints.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for CLI output, diagnostics, docs, source generators, tests, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-3-cli-migration-and-ide-edge-case-hardening` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Change Log

- 2026-05-11: Created Story 11.3 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/11-3-cli-migration-and-ide-edge-case-hardening.md`
