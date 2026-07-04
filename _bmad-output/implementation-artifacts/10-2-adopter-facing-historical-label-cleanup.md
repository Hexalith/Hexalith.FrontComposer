---
baseline_commit: 86b6a92ddea32c67de589da710133f9e1f77f76e
---

# Story 10.2: Adopter-facing historical-label cleanup

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a technical writer,
I want adopter-facing CLI, diagnostics, and Testing docs free of stale historical story ownership labels,
so that adopters are not sent to obsolete Story 9 provenance when Epic 7 owns the current contract.

## Acceptance Criteria

1. Given CLI, migration, diagnostics, Testing README, and published how-to docs, when they describe current Epic 7 behavior, then adopter-facing text names the current contract or feature, not stale historical story ownership.

2. Given source comments or generated diagnostic registry metadata retain old Story 9 labels as provenance, when they are not adopter-facing and do not misstate current ownership, then they may remain documented as brownfield provenance.

## Tasks / Subtasks

- [x] Audit all Story 9 ownership labels before editing. (AC: 1, 2)
  - [x] Run a repository scan for `Story 9`, `story 9`, `9-1-build-time-drift-detection`, `9-2-cli-inspection-and-migration-tools`, `9-4-diagnostic-id-system-and-deprecation-policy`, and `9-5-diataxis-documentation-site` across `docs/`, `src/Hexalith.FrontComposer.Cli/`, `src/Hexalith.FrontComposer.Testing/`, and relevant test/docs validation files.
  - [x] Classify each hit as adopter-facing rendered prose, package README prose, published front matter/metadata, internal source comment, generated API metadata, or retained provenance.
  - [x] Record any intentionally retained labels in the story completion notes with a reason tied to AC2.

- [x] Reconcile adopter-facing CLI and migration copy. (AC: 1)
  - [x] Update `docs/how-to/migration-guides.md` so the "Why it changed" text names the CLI migration contract / developer-mode migration behavior instead of "Story 9-2" and "Story 9-4".
  - [x] Update `docs/migrations/9.1-to-9.2.md` so the description and "Why This Changed" section describe the versioned migration contract and `AddFrontComposerDevMode` behavior, not Story 9 ownership.
  - [x] Update `docs/reference/cli.md` so the final link text names migration stubs/guides by feature or version, not "Story 9-2 migration stubs".
  - [x] Preserve real version identifiers such as `9.1.0`, `9.2.0`, and path slugs like `docs/migrations/9.1-to-9.2.md`; those are product version labels, not stale story ownership labels.

- [x] Reconcile adopter-facing diagnostics documentation without erasing valid provenance. (AC: 1, 2)
  - [x] Update `docs/reference/diagnostics/index.md` so the description/body names the diagnostic registry and published diagnostic reference contract, not Story 9-4/9-5 ownership.
  - [x] Audit `docs/diagnostics/HFC*.md` front matter and body for Story 9 labels that DocFX exposes to adopters; update visible ownership copy only where it misstates current contract ownership.
  - [x] Treat `docs/diagnostics/diagnostic-registry.json`, `docs/diagnostics/migration-findings.json`, source comments, and generated API YAML as provenance/metadata unless they are rendered as adopter-facing ownership copy or validated public metadata.
  - [x] Do not hand-edit `docs/_site/**`; if generated output needs updating, regenerate it through the docs pipeline and document the command.

- [x] Reconcile Testing package docs if the audit finds stale labels. (AC: 1)
  - [x] Read `src/Hexalith.FrontComposer.Testing/README.md` and `docs/how-to/test-generated-components.md` before deciding whether any change is needed.
  - [x] Keep Testing copy focused on the package contract: bUnit host, deterministic fakes, evidence redaction, and public API baseline.
  - [x] Do not rename public Testing APIs, change `PublicAPI.Shipped.txt`, or alter redaction behavior for this story; Story 10.5 owns redaction default-lane guards.

- [x] Update docs validation evidence and fingerprints only when owned inputs change. (AC: 1, 2)
  - [x] If editing a producer input listed by `eng/validate-docs.ps1`, update `docs/validation/producer-fingerprints.json` intentionally.
  - [x] If changing diagnostic registry/stub metadata, run the diagnostic registry/governance lane and update any generated baselines only when required by that lane.
  - [x] Keep all copy support-safe: no private paths, tenant/user identifiers, secrets, terminal control characters, or raw exception text.

- [x] Run focused validation and record exact results. (AC: 1, 2)
  - [x] Run `pwsh ./eng/validate-docs.ps1 -SkipDocFx` after docs edits.
  - [x] Run the relevant focused governance tests if diagnostics or skill-corpus metadata changes, starting with `DiagnosticRegistryTests` for diagnostic registry/stub changes.
  - [x] Run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md` before review.
  - [x] If broad docs/DocFX/solution validation is blocked locally, record the exact command, exact blocker, blocker timing, and CI authority.

## Dev Notes

### Story Context

Epic 10 carries Epic 7 tooling-governance follow-through without reopening completed Stories 7.1-7.5. Story 10.2 implements the Epic 7 retrospective action to remove stale historical story ownership from adopter-facing CLI, diagnostics, and Testing docs while allowing true brownfield provenance to remain where it is not adopter-facing. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 10: Tooling Governance Follow-Through`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md#4.3 epics.md - Add Stories 10.1 Through 10.5`; `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#Action Items`]

The target behavior is documentation clarity, not ownership archaeology. Adopter-facing prose should say "CLI migrate contract", "diagnostic registry", "published diagnostics reference", "Testing package", "versioned migration guide", or the concrete feature/contract. It should not tell adopters that current behavior belongs to old Story 9 planning labels. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.2: Adopter-facing historical-label cleanup`]

### Existing Implementation Facts

- `docs/how-to/migration-guides.md` is a published adopter how-to. It currently says: "Story 9-2 moved CLI migration reports..." and "Story 9-4 reserved..." in the visible body.
- `docs/migrations/9.1-to-9.2.md` is a published migration guide. Its description and "Why This Changed" section name Story 9-2, while the underlying product fact is the 9.1 to 9.2 migration from `AddFrontComposerDebugOverlay` to `AddFrontComposerDevMode`.
- `docs/reference/cli.md` is a published adopter reference. It currently links to "Story 9-2 migration stubs" instead of naming the versioned migration stubs/guides.
- `docs/reference/diagnostics/index.md` is a published adopter reference. It currently describes diagnostics as "generated from the Story 9-4 registry" and says "Story 9-5 publishes those Story 9-4 registry entries".
- `src/Hexalith.FrontComposer.Cli/README.md` already uses current contract language for `inspect`, `migrate`, and the synthetic-only HFCM9002 boundary; do not rewrite it unless the audit finds a stale label.
- `src/Hexalith.FrontComposer.Testing/README.md` already uses current package-contract language and has no visible Story 9 label in the initial scan.
- `docs/diagnostics/diagnostic-registry.json`, `docs/diagnostics/migration-findings.json`, many diagnostic stubs, and source comments include Story 9 labels as registry, migration, or authoring provenance. AC2 permits retaining these only when they are not rendered as current adopter-facing ownership claims.

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `docs/how-to/migration-guides.md` - current adopter migration how-to and stale visible Story 9 body copy.
- `docs/migrations/9.1-to-9.2.md` - versioned migration guide, product version labels, and migration explanation copy.
- `docs/reference/cli.md` - CLI reference page and stale final link text.
- `docs/reference/diagnostics/index.md` - diagnostics reference summary and stale Story 9 wording.
- `docs/how-to/test-generated-components.md` - Testing how-to; likely no Story 9 copy but it has historical owner metadata that must be classified.
- `src/Hexalith.FrontComposer.Testing/README.md` - package README; likely no Story 9 copy but in explicit Story 10.2 scope.
- `src/Hexalith.FrontComposer.Cli/README.md` - package README; current HFCM9002 synthetic-only wording must be preserved.
- `docs/diagnostics/README.md` - registry authoring contract; classify as internal/provenance unless rendered to adopters.
- `docs/diagnostics/diagnostic-registry.json` and `docs/diagnostics/migration-findings.json` - authoritative metadata; update only if validation or DocFX rendering proves adopter-facing ownership drift.
- `eng/validate-docs.ps1` and `docs/validation/producer-fingerprints.json` - docs validation and producer fingerprint behavior if edited docs are producer inputs.

### Architecture Compliance

- Keep this as docs/tooling governance. Do not change product runtime behavior, source generators, CLI command behavior, Testing package behavior, schema fingerprints, package references, public API baselines, pacts, or generated source output.
- `docs/` is a published DocFX site, not scratch space. Use existing front matter and Diataxis structure; do not add ad hoc files outside the current docs taxonomy.
- Use ASCII copy unless an edited file already requires non-ASCII. Preserve existing line endings and final newline.
- Do not hand-edit generated `docs/_site/**` output. Regenerate through `eng/validate-docs.ps1`/DocFX only if the repo expects generated output to change.
- Do not modify `references/Hexalith.*` submodules and do not run recursive or remote submodule commands.

### Anti-Patterns To Avoid

- Do not replace product version labels (`9.1.0`, `9.2.0`, `9.1-to-9.2`) with Epic 7 labels. Version numbers are user-facing product facts.
- Do not do a blind global replacement of `Story 9`. Some labels are provenance in registry metadata, source comments, generated API docs, or validation examples.
- Do not make HFCM9002 sound production-emitted. Story 10.4 owns the production-emission decision; this story must preserve the current synthetic/manual-only boundary.
- Do not reopen Epic 7 implementation, add CLI text parity tests, or add Testing redaction cases here; Stories 10.3 and 10.5 own those.
- Do not claim docs validation passed if DocFX, NuGet, MSBuild, or socket restrictions block it locally.

### Testing Requirements

- Required focused docs validation: `pwsh ./eng/validate-docs.ps1 -SkipDocFx`.
- If diagnostic registry/stub metadata changes: run the focused SourceTools diagnostic registry governance lane, preferably the direct xUnit v3 in-process runner if VSTest is socket-blocked locally.
- If only Markdown prose changes and docs validation passes, no runtime C# test is required.
- Required story artifact validation before review: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md`.
- Broad validation remains the standard filtered lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.

### Latest Technical Information

No external API/package research is required for Story 10.2. Use the repository-pinned stack and docs tooling in `_bmad-output/project-context.md`: .NET SDK `10.0.301`, DocFX validation through `eng/validate-docs.ps1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and the existing FrontComposer docs/diagnostic registry conventions. The risk is copy/metadata accuracy, not stale third-party API knowledge.

### Previous Story Intelligence

Story 10.1 implemented the mechanical story evidence gate. The dev agent must expect `python3 eng/validate-story-artifacts.py --story <story>` to check the File List and checked-task evidence before review promotion. Do not mark tasks complete unless changed files, docs validation output, test evidence, completion notes, or a documented blocker backs the claim. [Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md#Senior Developer Review (AI)`]

Story 10.1 also fixed dotfile path parsing in File List reconciliation and wired `artifact_validation_failed` into review completion. Include any `.agents`, `.github`, or dotdir path literally in File List if touched, and document unrelated dirty files instead of claiming them. [Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md#Findings and resolutions`]

Current worktree at story creation has an unrelated modified `_bmad-output/story-automator/orchestration-9-20260704-182122.md`. Do not revert it and do not include it in Story 10.2 implementation unless the story intentionally edits it.

### Git Intelligence

Recent commits show Story 10.1 just landed the evidence gate and earlier Story 9 work is still present in history:

- `86b6a92 feat(story-10.1): reconcile story review evidence`
- `88f0342 docs(story-9.2): document accepted submodule pointer updates`
- `f30a8ec feat: document accepted submodule pointer bumps for Story 9.2 and update orchestration state`
- `914279b feat: update orchestration state for Story 9.2 and document submodule pointer bumps`
- `c23a1890 feat(story-9.2): add tests and documentation for FcNewItemIndicator producer and generated-grid consumer`

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md`.
- Sprint-status key: `10-2-adopter-facing-historical-label-cleanup`.
- Primary docs validation script: `eng/validate-docs.ps1`.
- Published docs roots in scope: `docs/how-to/`, `docs/migrations/`, `docs/reference/`, and visible diagnostic pages under `docs/diagnostics/`.
- Package README roots in scope: `src/Hexalith.FrontComposer.Cli/README.md` and `src/Hexalith.FrontComposer.Testing/README.md`.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 10 and Story 10.2 source of record.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` - approved Epic 7 follow-through proposal and AR12 scope.
- Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md` - stale historical label action item and HFCM9002 caution.
- Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md` - previous story evidence-gate behavior and review learnings.
- Source: `docs/how-to/migration-guides.md` - stale visible Story 9 migration copy.
- Source: `docs/migrations/9.1-to-9.2.md` - stale visible Story 9 migration-guide copy and versioned migration semantics.
- Source: `docs/reference/cli.md` - stale visible Story 9 link text.
- Source: `docs/reference/diagnostics/index.md` - stale visible Story 9 diagnostics-reference copy.
- Source: `src/Hexalith.FrontComposer.Cli/README.md` - current CLI README and HFCM9002 synthetic-only boundary.
- Source: `src/Hexalith.FrontComposer.Testing/README.md` - current Testing package README.
- Source: `eng/validate-docs.ps1` - docs validation and producer fingerprint rules.
- Source: `_bmad-output/project-context.md` - project stack, docs, testing, and submodule rules.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM instructions, sprint status, Epic 10 source, the Epic 7 follow-through proposal, the Story 10.1 artifact and review findings, relevant adopter docs/README files, docs validation behavior, recent git history, and current git status.
- 2026-07-04: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX files matched the workflow patterns, so `_bmad-output/project-context.md`, docs validation files, and live docs/README files supplied project context.
- 2026-07-04: Confirmed `sprint-status.yaml` had Epic 10 in progress, Story 10.1 done, and Story 10.2 in `backlog` before story creation.
- 2026-07-04: Validated story context against the create-story checklist by adding concrete adopter-facing audit targets, likely UPDATE files, provenance guardrails, previous-story intelligence, and validation requirements.
- 2026-07-04: Dev-story loaded BMAD workflow/config/project-context and Story 10.2, captured baseline `86b6a92ddea32c67de589da710133f9e1f77f76e`, and moved sprint status to `in-progress`.
- 2026-07-04: RED scan found stale adopter-facing Story 9 copy in `docs/how-to/migration-guides.md`, `docs/migrations/9.1-to-9.2.md`, `docs/reference/cli.md`, and `docs/reference/diagnostics/index.md`, plus retained provenance in diagnostic registry/stub metadata, source comments, validation inputs, and generated API metadata.
- 2026-07-04: GREEN scan confirmed the stale adopter-facing phrases no longer appear in the four edited pages.
- 2026-07-04: `docs/migrations/9.1-to-9.2.md` is a docs producer input; recalculated SHA-256 `eedbe85ecad96fb896b54051e4d4cb00d45360b9ebbdc2ae8c85d209311fde85` and updated `docs/validation/producer-fingerprints.json`.
- 2026-07-04: QA generate-e2e-tests workflow loaded the BMAD QA skill/checklist, project config, persistent project context, Story 10.2, existing Playwright contract-docs patterns, and Story 10.2 docs/provenance surfaces; added the focused `adopter-historical-label-cleanup` Playwright spec and npm lane.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-04.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Replaced stale visible Story 9 ownership labels with current contract/feature wording in the migration how-to, 9.1-to-9.2 migration guide, CLI reference, and diagnostics reference index.
- Preserved product version labels (`9.1.0`, `9.2.0`, `9.1-to-9.2`) and the HFCM9002 synthetic/manual-only boundary; no runtime code, public APIs, generated source, diagnostic registry rows, or submodules were changed.
- Testing package docs were read and left unchanged because they already describe the package contract with bUnit host, deterministic fakes, redacted evidence, and public API baseline wording.
- Intentionally retained Story 9 labels under AC2 where they are provenance/metadata rather than current adopter-facing ownership claims: `ownerStory`/`storyOwner` front matter in docs pages and HFC stubs, `docs/diagnostics/diagnostic-registry.json`, `docs/diagnostics/migration-findings.json`, `docs/diagnostics/README.md` authoring-contract history, source comments in `src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`, validation producer story IDs in `eng/validate-docs.ps1` and `docs/validation/producer-fingerprints.json`, tests that pin historical governance behavior, and generated API YAML copied from source XML comments.
- Review follow-up (2026-07-04): the retained-provenance audit record above was completed for two hits the original scan missed. `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` `migrationOwner: Story 9-5` is agent-audience front-matter metadata and is retained under AC2. `docs/ide-parity-matrix.md` `story-9-3` version pin and the "Story 9-3" scope-decision notes record historical scoping rationale (not current-feature ownership) and are retained under AC2. Both surfaces are non-adopter-rendered-ownership provenance.
- Did not hand-edit `docs/_site/**`; DocFX output was not regenerated because the local full DocFX/build lanes are blocked and generated site output is excluded from source edits.
- QA coverage now includes 3 focused Playwright documentation-governance tests that pin current contract wording in adopter docs, preservation of 9.1/9.2 version/API facts, and AC2 retention of Story 9 provenance only in metadata/internal registry surfaces.

### Documented Unrelated Changes

- `_bmad-output/story-automator/orchestration-9-20260704-182122.md` - pre-existing unrelated dirty orchestration state recorded by story creation; do not modify or list for Story 10.2 unless implementation intentionally edits it.

### Documented Blockers

- `pwsh ./eng/validate-docs.ps1 -SkipDocFx` - blocked inside compile-snippet builds before story-specific docs assertions could complete. Exact local result: Failed with 6 existing snippet build failures (`docs/how-to/customization-gradient-cookbook.md` snippets 1-4, `docs/how-to/test-generated-components.md` snippet 5, `docs/tutorials/getting-started.md` snippet 6). Direct reproduction with `dotnet build artifacts/docs/snippets/snippet-005/Snippet.csproj --nologo -v:normal -p:UseSharedCompilation=false` failed in the Restore target with `Build FAILED. 0 Warning(s) 0 Error(s)`; `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` also failed aggregate multi-target build with `0 Warning(s) 0 Error(s)`, while `-f net10.0` and `-f netstandard2.0` target-specific builds both passed. Blocker timing: before snippet compilation/test body, during MSBuild restore/project-reference graph evaluation. CI authority: Required for the full lane; local fallback evidence recorded below.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` - blocked before test execution by MSBuild node socket creation: `MSBUILD : error MSB1025` with `System.Net.Sockets.SocketException (13): Permission denied`. Fallback `-m:1 /nr:false` avoided the socket error but restore was then blocked by restricted network NuGet audit lookups (`NU1900` unable to load `https://api.nuget.org/v3/index.json`) in referenced UI/Tenants/Parties projects. CI authority: Required for broad regression; local fallback evidence recorded below.

### Test Evidence

- Required command attempted: `rg -n "Story 9-2 moved|Story 9-4 reserved|Migration guidance for Story 9-2|Story 9-2 aligned|Story 9-2 migration stubs|Story 9-4 registry|Story 9-5 publishes" docs/how-to/migration-guides.md docs/migrations/9.1-to-9.2.md docs/reference/cli.md docs/reference/diagnostics/index.md`
  Local result: Passed; exit 1 with no matches after edits. CI authority: Advisory.
- Required command attempted: `pwsh ./eng/validate-docs.ps1 -SkipDocFx`
  Local result: Blocked; exact blocker recorded in Documented Blockers. Fallback evidence: `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` passed with `Docs validation passed. Evidence manifest: artifacts/docs/validation-manifest.json`; `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --nologo -v:minimal -f net10.0 -p:UseSharedCompilation=false` passed; `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --nologo -v:minimal -f netstandard2.0 -p:UseSharedCompilation=false` passed. CI authority: Required for full snippet lane.
- Required command attempted: `DiagnosticRegistryTests`
  Local result: Not applicable; diagnostic registry/stub metadata was not changed, so the focused governance lane was not required by the story. CI authority: Not applicable.
- Required command attempted: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
  Local result: Blocked; exact blocker and single-node fallback blocker recorded in Documented Blockers. CI authority: Required for broad regression.
- Required command attempted: `npm run test:story-10-2` from `tests/e2e`
  Local result: Passed; 3/3 Chromium Playwright tests. CI authority: Advisory focused E2E/docs-governance coverage.
- Required command attempted: `npm run typecheck` from `tests/e2e`
  Local result: Passed; TypeScript E2E workspace compiled with `tsc --noEmit`. CI authority: Advisory focused E2E workspace check.
- Required command attempted: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md`
  Local result: Passed; `Story artifact validation passed.` CI authority: Required local story evidence gate.

### File List

- `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md` - story record, task checkboxes, test evidence, file list, and review status.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Story 10.2 status moved through in-progress/review with baseline/test summary.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` - QA test automation summary updated with Story 10.2 generated tests, coverage, validation, and checklist evidence.
- `docs/how-to/migration-guides.md` - replaced stale Story 9 migration/governance ownership prose with contract wording.
- `docs/migrations/9.1-to-9.2.md` - replaced stale Story 9 description and "Why This Changed" prose with versioned migration contract wording.
- `docs/reference/cli.md` - replaced stale Story 9 migration-stub link text with versioned migration-stub wording.
- `docs/reference/diagnostics/index.md` - replaced stale Story 9 registry/publishing wording with diagnostic registry and published reference contract wording.
- `docs/concepts/source-generation-and-mcp-split.md` - review follow-up: replaced a stale visible `Story 9-5` ownership label describing the current single-source docs/MCP-split behavior with feature wording ("The single-source docs model ..."); missed by the original audit.
- `docs/validation/producer-fingerprints.json` - updated SHA-256 for the edited migration-guide producer input.
- `tests/e2e/package.json` - added the focused Story 10.2 Playwright script.
- `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` - added focused adopter-facing docs/provenance cleanup coverage.
- `docs/` - pre-existing audited documentation root used for Story 9 ownership-label scan evidence; not modified as a single path.
- `src/Hexalith.FrontComposer.Cli/` - pre-existing audited CLI source root used for Story 9 ownership-label scan evidence; package README/source comments retained where provenance-only.
- `src/Hexalith.FrontComposer.Testing/` - pre-existing audited Testing package root used for Story 9 ownership-label scan evidence; package docs/API baseline unchanged.
- `docs/diagnostics/diagnostic-registry.json` - pre-existing provenance metadata audited and intentionally unchanged under AC2.
- `docs/diagnostics/migration-findings.json` - pre-existing migration provenance metadata audited and intentionally unchanged under AC2.
- `docs/how-to/test-generated-components.md` - pre-existing Testing how-to audited and intentionally unchanged because current package-contract wording was already correct.
- `src/Hexalith.FrontComposer.Testing/README.md` - pre-existing package README audited and intentionally unchanged because current package-contract wording was already correct.
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` - pre-existing Testing public API baseline audited by scope guard and intentionally unchanged.
- `eng/validate-docs.ps1` - pre-existing validation producer list audited to identify fingerprint-owned inputs; not modified.

### Change Log

- 2026-07-04: Story 10.2 implementation completed; adopter-facing CLI, migration, and diagnostics pages no longer present current behavior as Story 9-owned, while provenance metadata remains intact under AC2.
- 2026-07-04: QA Generate E2E Tests added focused Playwright documentation-governance coverage for Story 10.2 and refreshed the test automation summary/story evidence.
- 2026-07-04: Senior Developer Review (AI) auto-fixed one AC1 miss — a stale `Story 9-5` ownership label in the adopter-facing published `docs/concepts/source-generation-and-mcp-split.md` describing current single-source docs/MCP behavior — reworded to feature language, extended the Playwright spec to guard that page, and completed the retained-provenance audit record.

## Senior Developer Review (AI)

**Reviewer:** Administrator | **Date:** 2026-07-04 | **Outcome:** Approve (after auto-fix)

Adversarial review validated every task/AC claim against git reality and the actual implementation. The four originally-edited adopter pages, the producer fingerprint update (SHA-256 verified matching), and the retained provenance surfaces all hold, and the focused Playwright spec passes 3/3.

Findings:

- **[MEDIUM — fixed] AC1 coverage gap in an adopter-facing concept page.** `docs/concepts/source-generation-and-mcp-split.md` (`audience: adopter`, `status: published`) still stated "Story 9-5 keeps both needs in one Markdown source ... during docs validation" — a stale historical story-ownership label on current single-source docs/MCP behavior. Task 1 required a scan across all of `docs/`, but this hit was neither reworded nor recorded as retained provenance. Auto-fixed to "The single-source docs model ..." (ASCII, CRLF preserved; not a fingerprinted producer input, so no fingerprint change). Added the page to the `adopter-historical-label-cleanup` Playwright spec to guard against regression.
- **[LOW — fixed] Incomplete retained-provenance audit record (Task 1, subtask 3).** The completion notes omitted `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` (`migrationOwner: Story 9-5`, agent-audience front-matter metadata) and `docs/ide-parity-matrix.md` (`story-9-3` version pin plus "Story 9-3" scope-decision notes). Both are AC2-permitted provenance (metadata / historical scope rationale, not adopter-facing current-ownership claims); the audit record was completed rather than editing them, respecting the "no blind Story 9 replacement" anti-pattern.
- **[Verified — no change] Provenance retention is correct under AC2.** `docs/diagnostics/README.md`, `diagnostic-registry.json`, `migration-findings.json`, front-matter `ownerStory` fields, and generated API metadata legitimately retain Story 9 labels as non-adopter provenance.

Gate: `python3 eng/validate-story-artifacts.py` exits 0 after the File List update; 0 CRITICAL issues remain.
