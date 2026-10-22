---
title: 'Story 15.3: Align Migration and Classification Guidance'
type: 'bugfix'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
story_id: '15.3'
story_key: '15-3-i-doc-b-align-migration-and-classification-guidance'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Published guides do not explicitly distinguish executable migrations from manual upgrades. CLI guidance understates the existing safe fix and omits inspect semantics.

**Approach:** Classify the existing guides, extend existing parity/publication checks, and reconcile documentation with executable behavior.

## Boundaries & Constraints

**Always:** Preserve named inspect/migrate v1 JSON contracts, deterministic output, dry-run default, per-file atomic replacement, path boundaries, redacted default paths, and accepted history. HFCM9002 remains synthetic/manual-only; production emission is unapproved. Preserve unrelated retrospective edits and statuses.

**Never:** Add executable edges, public APIs, registries, replacement validators, or evidence bundles; reimplement CLI/generated output; change dependencies, submodules, Fluent approval, release baselines, or historical acceptance. Do not promise semantic secret scrubbing or whole-run rollback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Catalog/index parity | Executable edge or manual package/API guide | Exact versions, guide path and classification agree | Missing, duplicate, stale or false classification fails deterministically |
| Migration | Supported edge, default dry-run or explicit apply | Preview writes nothing; eligible edits use atomic per-file replacement | Unsafe targets refused; failures keep applied=false |
| Inspection | Generated families, sidecars, severity/type filters | Text/JSON counts and ordinal order agree; fail flags use filtered diagnostics | Unsafe paths redacted; malformed sidecars produce HFCM0002 |

</frozen-after-approval>

## Open Questions

- **Symlink safety versus documentation-only scope:** Story 15.3 requires symlink refusal but also limits changes to documents and missing assertions. Current apply replaces an in-project symlink, including an explicitly linked outside-target symlink, with a regular file. Choose **minimal safety correction** (extend existing write-safety policy and regression tests so the refusal requirement is true) or **documentation/checks only** (document current behavior, defer the safety correction, and explicitly leave that acceptance requirement unsatisfied).

## Code Map

- `src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs`: private Edges is authoritative; only 9.1.0 to 9.2.0 is executable. Use existing internals access/reflection in tests without exposing production API.
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`: existing dry-run/apply, boundaries, fix, sidecar and fail-flag tests; catalog/index parity is missing.
- `eng/validate-docs.ps1`: existing migration metadata/completeness and normalized producer-hash gates. Extend these; no independent registry.
- `src/Hexalith.FrontComposer.Cli/README.md` and `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`: reuse documented family/count, order, filter and path behavior. Public reference incorrectly promises fingerprints.
- `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs`, `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`, `src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs`: current safety seams; symlink decision above controls any runtime edits.

## Tasks & Acceptance

**Execution:**
- [ ] `docs/migrations/index.md`, `docs/migrations/9.1-to-9.2.md`, `docs/migrations/1.12-to-2.0.md`, `docs/migrations/3.1-to-4.0.md`, `docs/migrations/4.5-to-4.6.md` — classify guides and indexed edges using one frontmatter field, migrationMode: cli-supported or manual-only. Preserve exact 3.1.1 metadata. Explain the CLI-only HFCM9001 rename and synthetic HFCM9002 boundary.
- [ ] `docs/how-to/migration-guides.md`, `docs/reference/cli.md` — reconcile default dry-run, explicit versions, per-file atomic apply/partial failure, exclusions and supported schemas. Describe inspect families/counts, shared ordering, severity/type filtering before fail flags, sidecars, relative paths, explicit absolute-path opt-in and bounded output accurately.
- [ ] `eng/validate-docs.ps1`, `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` — extend existing gates to compare complete catalog tuples and classified index/guides bidirectionally. Exercise missing/unknown classification, false executable/manual claims, missing/duplicate/stale index links, version/path mismatch and missing catalog guidance. Keep diagnostics relative and deterministic; do not hardcode a replacement edge list. Source extraction must ignore comments and unrelated initializers.
- [ ] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — reuse documentation validation coverage and add only missing semantic assertions. Prefer PascalCase method names without identifier-seal churn.
- [ ] `docs/validation/producer-fingerprints.json` — refresh only the existing 9.1 guide's normalized hash after intentional edits.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md` — record exact checks and complete File List; preserve unrelated statuses and keep Epic 15 in progress.

**Acceptance Criteria:**
- Given the catalog and published guides, when parity runs, then every executable edge has matching guidance, manual-only changes are explicit, and negative fixtures reject drift.
- Given documented migration examples, when existing behavior checks run, then dry-run, atomic file replacement, unsafe-target handling and frontcomposer.cli.migrate.v1 agree with the approved safety-scope decision.
- Given inspect output, when documentation and existing behavior checks are compared, then forms/grids/registrations/manifests, ordering, severity/fail behavior, default redacted paths and frontcomposer.cli.inspect.v1 agree.
- Given final published edits, when documentation validation runs, then snippets and DocFX pass without introducing parallel inventories or changing CLI/generated contracts.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

No irreversible action is proposed. Footprint is existing documentation and validators/tests plus story tracking; runtime safety edits depend on the open decision. Atomicity is per file: earlier successful writes remain after a later failure.

Baseline CLI Debug build passed with zero warnings/errors. Three temporary symlink probes confirmed replacement for an internal link and an outside-target link with Compile Link metadata; an ordinary outside-target link was ignored. Evidence: /tmp/frontcomposer-15-3-cli-baseline-build.log and /tmp/frontcomposer-15-3-symlink-probe.json. Fixtures were outside the repository.

## Verification

- Build CLI.Tests and SourceTools.Tests individually in Debug; directly run the built CLI assembly's MigrationCommandTests and InspectCommandTests, and SourceTools DocsSiteValidationTests. Expect all passing, including negative classification fixtures.
- `pwsh ./eng/validate-docs.ps1` — expect structural checks, normalized producer hashes, compiled snippets and DocFX to pass. Probe the actual publication gate with negative classification fixtures.
- `python3 eng/validate-story-artifacts.py` and its per-story mode with explicit owned changed paths — expect consistent status/File List evidence independent of unrelated edits.
- `git diff --check` — expect no whitespace errors.

## File List

- `_bmad-output/implementation-artifacts/epic-15-context.md`
- `_bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md`
