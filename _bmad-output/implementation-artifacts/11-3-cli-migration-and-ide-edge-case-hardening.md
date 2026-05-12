# Story 11.3: CLI, Migration, and IDE Edge-Case Hardening

Status: review

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
| AC21 | A deferred row is closed as an accepted constraint rather than fixed | The ledger and docs are updated | The row names the documented user-visible behavior, targeted regression evidence, release-safety rationale, and follow-up owner/story when the constraint is not permanent. |
| AC22 | CLI migration, inspect, sidecar, or IDE evidence code decides whether a path is trusted or writable | The decision runs | The same workspace path policy is used before filesystem mutation, resolving symlinks/junctions where supported, case/path variants, relative segments, UNC/drive-relative inputs, excluded folders, and submodule boundaries with shared regression coverage. |
| AC23 | Project or solution selection has no explicit path or has multiple candidates | Selection runs from a repository or subfolder | Precedence is deterministic: explicit `--project`/`--solution` wins, a single unambiguous current-root candidate is allowed, and all ambiguous or unsupported discovery paths fail with a sanitized actionable error; no "first found" fallback is permitted. |
| AC24 | Migration, sidecar, or IDE `$OutPath` writes fail, are cancelled, or receive hostile paths | The command exits | Temp files are created only inside the already-validated target directory, replacement happens only after validation succeeds, final files are unchanged on failure, cleanup is best-effort and bounded, and text/JSON errors are sanitized. |
| AC25 | Story 11.3 closes CLI/IDE deferred rows | Implementation proposes new diagnostics, HFCM release-row governance, SourceTools drift behavior, docs-site polish, or CI/release orchestration | The change is rejected or split to Stories 11.2, 11.4, 11.6, or 11.7 unless it is a narrow compatibility handoff needed to keep CLI/IDE evidence coherent. |
| AC26 | Tests create symlinks, junctions, case-variant paths, submodule-like paths, package-cache paths, or hostile sidecar/evidence manifests | Fixtures run locally or in CI | Fixtures are isolated under per-test temporary roots, never point to HOME/TEMP globals or live repo content, skip only for documented platform limitations, and never initialize or update nested submodules. |
| AC27 | `Debugger.Launch()` source-generator guidance is evaluated | Story 11.3 completes | The story chooses one binary rule before review: production paths forbid unconditional debugger launch with grep/analyzer evidence, or an explicit guarded/debug-only allowance is documented with the exact validation evidence. |
| AC28 | A release maintainer reviews Story 11.3 evidence | The story moves to review | The maintainer can read a single deferred-row matrix with `Deferred ID`, `Status`, `Evidence`, `User-visible behavior`, and `Follow-up story`; every status is fixed, accepted, superseded, or split. |
| AC29 | Story 11.3 starts and finishes deferred-row closure | The implementer inventories and then updates `deferred-work.md` | The starting inventory and final matrix reconcile exactly: every captured Story 11.3 row has one current outcome, no duplicate aliases remain unresolved, and any newly discovered row is either resolved in-scope or explicitly split before review. |
| AC30 | CLI migration, sidecar, IDE evidence, or `$OutPath` logic validates a path before writing or trusting it | The filesystem state can change after initial planning | The target is revalidated immediately before mutation or evidence trust, including resolved root, submodule/excluded-folder status, link/junction target, and same-directory temp-file placement; a changed target fails closed with sanitized output. |
| AC31 | Story 11.3 records validation commands, JSON/text snippets, deferred-row evidence, or release-maintainer notes | Evidence is written to docs, story notes, CI artifacts, or ledger rows | Evidence is sanitized for absolute local paths, user names, temp directory names, tokens, raw source payloads, and terminal control characters while preserving enough project-relative context to reproduce the result. |
| AC32 | Hostile-path, encoding, sidecar, manifest, and write-failure fixtures run on Windows, Linux, or CI agents | A platform capability is missing or cleanup fails | Tests skip only for named platform limitations, mark cleanup failures as visible test output or failures, and never retarget fixtures to live repository, HOME, shared TEMP, package-cache, or submodule content. |
| AC33 | A deferred edge case is proposed as an accepted constraint | The implementer chooses not to fix it in Story 11.3 | The acceptance includes a lightweight likelihood/impact rationale; high-impact security, data-loss, or release-integrity risks cannot be accepted silently and must be fixed, blocked, or split to a named owner. |
| AC34 | CLI and IDE hardening changes affect exit codes, counts, warnings, failures, manual-only entries, skipped files, or JSON `applied` semantics | Help, README, reference docs, text output, and JSON output are reviewed | The machine-readable and human-readable contracts agree on the same precedence, counts, and non-success behavior; mismatches block review. |

---

## Tasks / Subtasks

- [x] T1. Inventory and classify the Story 11.3 deferred rows (AC1, AC19, AC20)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [x] Capture every unresolved `DEF-9-2-*` and `DEF-9-3-*` row plus Story 11.3-routed rows such as CLI path/help/sidecar/manifest/debug findings.
  - [x] Save a starting inventory snapshot before changing code or docs, then reconcile the final matrix against that snapshot so each Story 11.3 row has exactly one current outcome.
  - [x] Classify each row as fix now, document accepted constraint, split to Story 11.2, or split to another Epic 11 story.
  - [x] Produce the release-maintainer matrix with columns `Deferred ID`, `Status`, `Evidence`, `User-visible behavior`, and `Follow-up story`; accepted constraints require docs/help behavior, targeted evidence, release-safety rationale, and future owner when not permanent.
  - [x] Add a lightweight likelihood/impact note for accepted constraints and escalate high-impact security, data-loss, or release-integrity rows to fixed, blocked, or split.
  - [x] Preserve historical review text; add resolution markers rather than deleting rows.

- [x] T2. Harden CLI project selection and path boundaries (AC2-AC4)
  - [x] Review `ProjectSelection.cs` for `--project`, `--solution`, `.sln`, `.slnx`, `.csproj`, and `.fsproj` behavior.
  - [x] Decide whether `.slnx` and `.fsproj` are supported in v1; if not, add explicit user-facing errors and docs.
  - [x] Add tests for quoted solution paths, escaped quotes where feasible, ambiguous matches, unsupported formats, and symlink/junction canonicalization.
  - [x] Ensure canonicalization uses `PathUtilities` consistently before trust decisions and output normalization.
  - [x] Define a single workspace path policy used by CLI migration, inspect-side evidence, sidecar reads, and IDE `$OutPath` decisions before any mutation; include expected outcomes for relative segments, trailing separators, UNC/drive-relative inputs, missing paths, symlink/junction escapes, case variants, and submodule/package-cache boundaries.
  - [x] Revalidate resolved targets immediately before filesystem mutation or evidence trust so link/junction swaps, root drift, and excluded-folder changes fail closed.
  - [x] Define project/solution precedence as explicit argument over single unambiguous current-root candidate over deterministic failure; add tests proving no "first found" fallback remains.
  - [x] Confirm migration write policy still excludes generated output, `bin`, `obj`, package caches, root-level submodules, nested submodule paths, outside-root linked files, and unrelated repositories.

- [x] T3. Harden migration catalog, workspace setup, apply result, and file IO behavior (AC5-AC7, AC10, AC12-AC14)
  - [x] Replace latent `SingleOrDefault` duplicate-edge behavior with explicit catalog uniqueness validation for `(fromVersion,toVersion)`.
  - [x] Surface `MefHostServices` or workspace composition failures as bounded CLI errors.
  - [x] Confirm `MigrationResult.Applied` or equivalent success field is false when any file write fails.
  - [x] Verify atomic temp-and-replace writes remain same-directory and preserve encoding/line endings where supported.
  - [x] Prove write-failure and cancellation behavior with injected failures: final file unchanged, temp artifact bounded or cleaned best-effort, JSON/text result non-success, and stderr/stdout redacted.
  - [x] Reconcile text and JSON counts for changed, unchanged, skipped, failed, manual-only, warning, and error outcomes; mismatches block review.
  - [x] Document and test strict UTF-8 / BOM handling, drive-root canonicalization, and excessive-file-size behavior as fixed or accepted.
  - [x] Revisit Ctrl+C double-press and SIGTERM handling; implement only if it stays low-risk and testable.
  - [x] Keep tool packaging smoke tests bounded with skip/timeout behavior and documented live-tree build isolation constraints.

- [x] T4. Harden sidecar, diff, sanitizer, and JSON/help semantics (AC8-AC11, AC18)
  - [x] Add README/reference/help coverage for `--fail-on-warning` versus `--fail-on-error` precedence and JSON `applied`/fail behavior.
  - [x] Add or update JSON schema notes for inspect and migrate payloads: path relativity, redaction, exit code mapping, warning/manual-only/failure fields, and known limitations.
  - [x] Decide whether unified diffs are terminal-safe only or patch-applicable; document the exact contract and keep tests aligned.
  - [x] Add sidecar tests for drive-relative paths (`C:foo.cs`), traversal, duplicate/case-variant paths, unreadable files, malformed JSON, and unsafe source path reporting.
  - [x] Distinguish missing sidecar, malformed/unreadable sidecar, and untrusted sidecar path as controlled fail-closed outcomes; do not silently fall back to permissive behavior.
  - [x] Ensure `OutputSanitizer` and diff rendering bound user-controlled fields without leaking raw omitted content.
  - [x] Confirm command output, JSON snippets, diff snippets, and docs examples redact absolute paths, user names, temp directory names, tokens, raw source payloads, and terminal control characters.

- [x] T5. Harden IDE parity manifest parsing and evidence generation (AC15-AC18)
  - [x] Add strict duplicate-key and unknown-field detection for `docs/ide-parity-matrix.json` and evidence manifests.
  - [x] Preserve strict trailing-comma rejection but add a friendly failure category or test name so maintainers understand the failure.
  - [x] Revisit `IdeParityReportSanitizer` ESC replacement only if downstream JSON decoding consumes its output; otherwise mark accepted with evidence.
  - [x] Evaluate `IdeParityRepositoryRoot` ancestor walk through symlinked `AppContext.BaseDirectory`; fix or document the CI assumption.
  - [x] Confirm `jobs/ide-parity-version-revalidation.ps1` writes `$OutPath` atomically and cannot escape expected artifact roots in release usage.
  - [x] Choose one binary `Debugger.Launch()` policy before review: either forbid unconditional production-path launches with grep/analyzer-style validation, or document the exact guarded/debug-only allowance and evidence.
  - [x] Keep hostile manifest and `$OutPath` fixtures inside per-test roots, and make missing platform capabilities or cleanup failures visible instead of silently relaxing fixture safety.

- [x] T6. Update public docs and ledger evidence (AC1, AC8, AC9, AC13, AC18-AC20)
  - [x] Update `src/Hexalith.FrontComposer.Cli/README.md` and `docs/reference/cli.md` so package README and public docs agree.
  - [x] Update `docs/reference/ide-parity.md` or `docs/ide-parity-matrix.md` only for story-owned manifest/evidence behavior.
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` with resolution/acceptance/split markers for all Story 11.3 rows.
  - [x] Record exact validation commands and outcomes in this story's Dev Agent Record.
  - [x] Record quality-gate evidence for path policy, atomic failure behavior, strict JSON parsing, hostile sidecars, CLI/IDE JSON/help/exit-code contracts, package smoke skip/run rationale, and root-level-only submodule handling.
  - [x] Record the starting inventory snapshot, final reconciliation result, accepted-constraint risk rationale, and evidence-sanitization checks.
  - [x] Move Story 11.3 to `review` only after implementation and validation evidence are complete.

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
| D8 | All write/trust decisions must flow through one workspace path policy before filesystem mutation. | Prevents CLI migration, inspect, sidecar, and IDE evidence code from drifting on symlink, junction, submodule, package-cache, and outside-root decisions. |
| D9 | Accepted constraints are release decisions, not unresolved leftovers. | Every accepted row must name user-visible behavior, targeted evidence, release-safety rationale, and a follow-up owner when it is not permanent. |
| D10 | Atomic writes use temp files only inside the already-validated target directory and replace only after validation completes. | Same-directory temp-and-replace keeps same-volume semantics and makes failure/cancellation state observable and testable. |
| D11 | Project selection never falls back to an arbitrary first candidate. | Deterministic release tooling is more valuable than guessing when multiple solutions/projects or unsupported formats are present. |
| D12 | Story 11.3 does not create, rename, or govern diagnostics/HFCM release rows. | Diagnostic governance belongs to Story 11.2; this story may only keep CLI/IDE evidence compatible with that contract. |
| D13 | Hostile-path and symlink/junction fixtures must be temporary and platform-aware. | Tests should prove boundaries without touching live repo content, HOME/TEMP globals, or nested submodule initialization. |
| D14 | `Debugger.Launch()` handling must be binary before review: forbidden by validation or explicitly guarded and documented. | A documentation-only maybe state is too ambiguous for release-readiness evidence. |
| D15 | Deferred-row closure is snapshot-driven: capture the starting Story 11.3 inventory, then reconcile final outcomes against it. | Prevents aliases, duplicate rows, and newly discovered rows from disappearing during a broad hardening pass. |
| D16 | Trust and write decisions are revalidated at the last responsible moment. | Initial canonicalization alone does not defend against root drift, link/junction swaps, or excluded-folder changes between planning and mutation. |
| D17 | Evidence must be reproducible without exposing environment-specific or sensitive data. | Release artifacts should help maintainers reproduce behavior without leaking local absolute paths, user names, temp folders, raw source payloads, or control characters. |
| D18 | Accepted constraints require an explicit likelihood/impact rationale. | Low-probability issues may be documented for v1, but high-impact security, data-loss, or release-integrity risks need a fix, block, or named split owner. |
| D19 | Fixture safety is fail-closed. | Tests must not silently relax hostile-path coverage by redirecting to live repo content, HOME, shared TEMP, package caches, or submodules when platform capabilities are missing. |
| D20 | CLI/IDE result semantics are a public contract. | Exit-code precedence, JSON `applied`, counts, warnings, failures, skipped files, and manual-only entries must agree across help, docs, text output, and machine output. |

### Decision Rules for Deferred Rows

| Status | Required evidence |
| --- | --- |
| Fixed | Test or validation command, touched behavior, and docs/help update when user-visible. |
| Accepted | Documented user-visible behavior, targeted regression evidence, release-safety rationale, lightweight likelihood/impact rationale, and follow-up owner/story when not permanent. |
| Superseded | Replacement behavior or owner, evidence that the old row no longer applies, and a pointer to the superseding row/story. |
| Split | Destination story, reason the work is out of Story 11.3 scope, and any compatibility handoff required here. |

Rows involving diagnostic registry/HFCM governance split to Story 11.2. SourceTools drift/generator coverage splits to Story 11.4. Docs-site UX polish splits to Story 11.6. CI/release orchestration splits to Story 11.7. Story 11.3 may not add new CLI/IDE behavior outside existing deferred rows unless the row inventory exposes a release-blocking bug in the owned CLI/IDE conformance surface.

### Workspace Path Policy Examples

| Input shape | Expected Story 11.3 decision |
| --- | --- |
| Explicit `--project` or `--solution` inside selected root | Allow only after canonicalization and supported-format validation. |
| Multiple discovered solutions/projects with no explicit path | Fail deterministically with a sanitized actionable error. |
| Relative path, `..`, trailing separator, case variant, or missing segment | Normalize before trust decisions; fail closed when the canonical target is absent, ambiguous, or outside the selected root. |
| Symlink or junction escaping the selected root | Refuse as outside-root even when the lexical path appears local. |
| Symlink, junction, or excluded-folder state changes after initial planning | Revalidate immediately before mutation or evidence trust; fail closed if the resolved target no longer matches the approved policy. |
| UNC or drive-relative sidecar/output path | Refuse or emit deterministic sentinel/manual-only behavior per CLI contract; never silently reinterpret as repo-local. |
| `bin`, `obj`, generated output, package cache, root-level submodule, nested submodule, or unrelated repository path | Treat as read-only/excluded for migration and output writes; do not initialize or update nested submodules. |

### Quality Gate Evidence Required Before Review

| Evidence area | Minimum proof |
| --- | --- |
| Path policy | Focused tests for inside-root allow, symlink/junction escape refusal, case/drive-relative behavior where platform-feasible, excluded folders, and submodule boundaries. |
| Atomic writes | Injected write/cancel failure showing final file unchanged, non-success result, bounded temp cleanup, and sanitized text/JSON output. |
| CLI/IDE contracts | README/reference/help examples for fail flags, exit codes, JSON fields, path relativity, diff limits, and known unsupported cases. |
| Strict parsing | Duplicate-key, unknown-field, trailing-comma/BOM, traversal, unsupported URI, and malformed JSON tests with friendly categories. |
| Sidecar hostility | Missing, malformed, unreadable, traversal, duplicate/case-variant, too-large, and drive-relative sidecars produce controlled fail-closed outcomes. |
| Debugger launch | Grep/analyzer validation forbids unconditional production-path launch, or a documented guarded/debug-only allowance with evidence. |
| Release-maintainer matrix | Every Story 11.3 deferred row has status, evidence, user-visible behavior, and follow-up story when applicable. |
| Inventory reconciliation | Starting deferred-row snapshot and final release-maintainer matrix match exactly, including duplicate aliases and newly discovered Story 11.3 rows. |
| Accepted constraints | Each accepted row includes likelihood/impact rationale; high-impact security, data-loss, or release-integrity rows are fixed, blocked, or split. |
| Evidence redaction | Story notes, docs examples, JSON/text snippets, and validation output avoid absolute local paths, user names, temp folders, tokens, raw source payloads, and terminal control characters. |
| Result contract reconciliation | Help, README, reference docs, text output, and JSON output agree on exit-code precedence, counts, warning/error behavior, `applied`, skipped, failed, and manual-only semantics. |
| Submodule guardrail | Evidence confirms root-level submodules are excluded/write-protected and no recursive nested submodule initialization/update is run. |

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

- 2026-05-12: `dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Release --no-restore` — passed, 39 tests.
- 2026-05-12: `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~IdeParity" --no-restore` — passed, 35 tests.
- 2026-05-12: `pwsh ./jobs/ide-parity-version-revalidation.ps1 -NoGithub -OutPath artifacts/ide-parity/revalidation-dry-run.story-11-3.md` — passed, no configured version drift and no artifact written.
- 2026-05-12: `pwsh ./eng/validate-docs.ps1` — passed; evidence manifest `artifacts/docs/validation-manifest.json`.
- 2026-05-12: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" --no-restore` — passed; main lane reported no failures.

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-3-cli-migration-and-ide-edge-case-hardening` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-11: Party-mode review applied via `/bmad-party-mode 11-3-cli-migration-and-ide-edge-case-hardening; review;`. Added guardrails for release-maintainer deferred-row decisions, single workspace path policy, deterministic project selection, atomic write failure evidence, hostile fixture handling, `Debugger.Launch()` policy, and cross-story scope boundaries.
- 2026-05-11: Advanced elicitation applied via `/bmad-advanced-elicitation 11-3-cli-migration-and-ide-edge-case-hardening`. Added guardrails for inventory reconciliation, last-moment path revalidation, evidence redaction, accepted-constraint risk rationale, fixture fail-closed behavior, and CLI/IDE result contract consistency.
- 2026-05-12: Starting inventory snapshot captured 29 Story 11.3-owned rows: DW-0011 through DW-0040/DW-0044 plus DW-0660. Final reconciliation leaves no active Story 11.3 owner markers in `deferred-work.md`.
- 2026-05-12: Hardened CLI project/solution selection: explicit paths are canonicalized, `.sln` quoted project paths are parsed deterministically, `.slnx` and `.fsproj` fail closed, and solution/current-directory ambiguity requires explicit `--project`.
- 2026-05-12: Hardened migration behavior: workspace composition failures emit bounded guidance, source reads reject invalid UTF-8 and files over 16 MiB, hostile sidecar paths produce sentinel manual-only entries, and quoted `.gitmodules` submodule paths remain excluded from writes.
- 2026-05-12: Hardened IDE parity evidence: strict duplicate-key/unknown-field/trailing-comma tests cover matrix and evidence JSON, repository-root discovery resolves symlinked base directories, `$OutPath` writes are repository-bounded/atomic, and production `Debugger.Launch()` is forbidden by test.
- 2026-05-12: Public README/reference docs now agree on exit codes, fail flag precedence, JSON fields, path policy, diff limitations, encoding limits, Ctrl+C/SIGTERM scope, IDE manifest strictness, and HFCM `introducedIn` semantics.

### Release Maintainer Matrix

| Deferred ID | Status | Evidence | User-visible behavior | Follow-up story |
| --- | --- | --- | --- | --- |
| DW-0011 / DEF-9-3-1 | Accepted | `docs/reference/ide-parity.md`; IDE parity tests | ESC remains literal `\u001B` text for report safety. | Story 11.4 only if downstream JSON decoding consumes sanitized text |
| DW-0012 / DEF-9-3-2 | Fixed | `CONTRIBUTING.md`; `ProductionSource_ForbidsUnconditionalDebuggerLaunch` | Production source forbids `Debugger.Launch()` prompts. | None |
| DW-0013 / DEF-9-3-3 | Fixed | `jobs/ide-parity-version-revalidation.ps1`; `docs/reference/ide-parity.md` | `$OutPath` is repository-bounded and atomically written. | None |
| DW-0014 / DEF-9-3-4 | Fixed | `IdeParityRepositoryRoot` link resolution | Symlinked test base directories anchor to the real checkout. | None |
| DW-0015 / DEF-9-3-5 | Fixed | `StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas` | Matrix/evidence JSON rejects duplicate keys and unknown fields. | None |
| DW-0016 / DEF-9-3-6 | Fixed | Strict JSON test and `docs/reference/ide-parity.md` | Trailing commas remain fail-closed with named test evidence. | None |
| DW-0017 / DEF-9-2-18 | Fixed | CLI README and reference docs | JSON fields, exit codes, and path semantics are listed. | None |
| DW-0018 / DEF-9-2-19 | Accepted | CLI README and reference docs | Diffs are terminal-safe informational output, not patch-applicable. | None unless product requires patch output |
| DW-0019 / DEF-9-2-20 | Accepted | `InspectCommand.cs` comment | Closest-type matching is a bounded hint. | None |
| DW-0020 / DEF-9-2-21 | Accepted | CLI README and reference docs | Widely separated edits may produce coarse informational diffs. | None unless patch output becomes required |
| DW-0021 / DEF-9-2-22 | Fixed | `SourceFile_ReadAsyncRejectsInvalidUtf8`; docs | Unsupported encodings fail closed. | None |
| DW-0022 / DEF-9-2-23 | Accepted | CLI encoding docs | Standard BOM precedence applies to the extreme UTF-32/UTF-16 edge. | None |
| DW-0023 / DEF-9-2-24 | Fixed | `MigrationCatalog.BuildEdges` | Duplicate migration catalog edges fail with named edge. | None |
| DW-0025 / DEF-9-2-26 | Fixed | `PathUtilities.Canonical`; selection tests | Drive roots/non-project roots are refused. | None |
| DW-0026 / DEF-9-2-27 | Fixed | `SourceFile_ReadAsyncRejectsExcessiveFileSizeBeforeDecoding` | Files over 16 MiB fail closed before decoding. | None |
| DW-0027 / DEF-9-2-28 | Fixed | `ToolPackagingSmokeTests`; main lane | PATHEXT casing is preserved in package smoke lookup. | None |
| DW-0028 / DEF-9-2-29 | Fixed | `Migrate_SidecarHostilePathsSurfaceManualOnlySentinel` | Hostile sidecar paths produce sentinel manual-only entries. | None |
| DW-0029 / DEF-9-2-7 | Fixed | `ProjectSelection.cs`; CLI tests | Explicit selected paths are canonicalized. | None |
| DW-0030 / DEF-9-2-8 | Fixed | `.sln` quoted path test | Quoted solution paths parse deterministically. | None |
| DW-0031 / DEF-9-2-9 | Fixed | unsupported `.slnx`/`.fsproj` tests and docs | Unsupported formats fail closed with guidance. | None |
| DW-0032 / DEF-9-2-10 | Accepted | CLI README/reference docs | `formattingApplied` is reserved and currently false. | Future formatter-backed migration edge |
| DW-0033 / DEF-9-2-11 | Accepted | `Program.cs`; CLI reference | Ctrl+C is supported; SIGTERM-specific handling is outside v1 local workflow. | Story 11.7 if release automation needs SIGTERM semantics |
| DW-0035 / DEF-9-2-13 | Fixed | `MigrationPlanner` workspace failure catch | Workspace assembly failures emit bounded guidance. | None |
| DW-0037 / DEF-9-2-15 | Fixed | `.gitmodules` quoted path test | Hand-written quoted submodule paths remain write-excluded. | None |
| DW-0038 / DEF-9-2-16 | Fixed | `PathUtilities.Canonical` | Odd path shapes fail closed instead of crashing. | None |
| DW-0039 / DEF-9-2-17 | Fixed | `Program.cs`; CLI reference | First Ctrl+C cancels; second uses OS default termination. | None |
| DW-0040 / DEF-9-2-1 | Fixed | CLI help, README, reference docs | `--fail-on-warning` precedence is documented. | None |
| DW-0044 / DEF-9-2-5 | Fixed | `MigrationCatalog.BuildEdges` | Duplicate `(from,to)` edges are explicitly rejected. | None |
| DW-0660 | Fixed | `docs/diagnostics/README.md` | HFCM `introducedIn` means CLI/tooling release. | None |

### Change Log

- 2026-05-11: Created Story 11.3 and marked ready-for-dev.
- 2026-05-11: Party-mode review hardening applied; added AC21-AC28, Decisions D8-D14, deferred-row decision rules, workspace path examples, quality-gate evidence, and task updates.
- 2026-05-11: Advanced elicitation hardening applied; added AC29-AC34, Decisions D15-D20, deferred-row acceptance rationale, last-moment path revalidation, evidence redaction, fixture safety, and result contract reconciliation requirements.
- 2026-05-12: Implemented CLI/IDE edge-case hardening, reconciled Story 11.3 deferred rows, updated docs/evidence, and moved story to review.

### File List

- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/11-3-cli-migration-and-ide-edge-case-hardening.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/diagnostics/README.md`
- `docs/reference/cli.md`
- `docs/reference/ide-parity.md`
- `jobs/ide-parity-version-revalidation.ps1`
- `src/Hexalith.FrontComposer.Cli/CliApplication.cs`
- `src/Hexalith.FrontComposer.Cli/InspectCommand.cs`
- `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`
- `src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`
- `src/Hexalith.FrontComposer.Cli/README.md`
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityConformanceUtilityTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`

## Party-Mode Review

- ISO date and time: 2026-05-11T08:04:51+02:00
- Selected story key: `11-3-cli-migration-and-ide-edge-case-hardening`
- Command/skill invocation used: `/bmad-party-mode 11-3-cli-migration-and-ide-edge-case-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: The review agreed Story 11.3 is valuable but too easy to implement as a broad edge-case bucket. Key risks were implicit path-policy divergence across CLI/IDE surfaces, ambiguous project/solution selection fallback, underspecified atomic write failure semantics, accepted constraints without release-maintainer evidence, accidental spillover into Stories 11.2/11.4/11.6/11.7, unsafe hostile-path fixtures, sidecar fail-open ambiguity, and unclear `Debugger.Launch()` acceptance.
- Changes applied: Added AC21-AC28; added Decisions D8-D14; added `Decision Rules for Deferred Rows`; added `Workspace Path Policy Examples`; added `Quality Gate Evidence Required Before Review`; tightened tasks for deferred-row matrices, path policy, deterministic selection, atomic failure evidence, sidecar fail-closed cases, `Debugger.Launch()` policy, and submodule guardrails.
- Findings deferred: John's recommendation to collapse the existing twenty ACs into six to eight outcome-driven ACs was deferred to avoid high-churn story rewriting during this pre-dev hardening pass; the new AC21-AC28 instead bound the existing detailed ACs with release-maintainer evidence and scope rules. No product-scope, architecture-policy, diagnostic-governance, SourceTools drift, docs-site UX, or CI/release orchestration changes were applied.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- ISO date and time: 2026-05-11T08:17:58+02:00
- Selected story key: `11-3-cli-migration-and-ide-edge-case-hardening`
- Command/skill invocation used: `/bmad-advanced-elicitation 11-3-cli-migration-and-ide-edge-case-hardening`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- Findings summary: The elicitation found that Story 11.3 was already strong on scope boundaries, but still had failure modes around deferred-row aliases disappearing during implementation, path trust decisions becoming stale between planning and mutation, release evidence leaking local paths or raw snippets, accepted constraints lacking a risk threshold, hostile fixtures silently relaxing safety when platform support is missing, and human-readable and machine-readable CLI result semantics drifting apart.
- Changes applied: Added AC29-AC34; added Decisions D15-D20; tightened T1-T6 for starting inventory snapshots, final reconciliation, accepted-constraint likelihood/impact rationale, last-moment path revalidation, evidence sanitization, result-count reconciliation, and fail-closed fixture behavior; extended deferred-row decision rules, workspace path policy examples, and quality-gate evidence requirements.
- Findings deferred: No product-scope, architecture-policy, diagnostic/HFCM governance, SourceTools drift, docs-site UX, or CI/release orchestration changes were applied. Broader AC consolidation remained deferred for the same reason captured in the party-mode review: it would create high story churn without materially improving pre-dev readiness.
- Final recommendation: ready-for-dev
