---
title: 'Story 15.3: Align Migration and Classification Guidance'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
story_id: '15.3'
story_key: '15-3-i-doc-b-align-migration-and-classification-guidance'
baseline_commit: 'bf40b9276f440646e753a0636854595cc7d0affc'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Published guides do not explicitly distinguish executable migrations from manual upgrades. CLI guidance understates the existing safe fix and omits inspect semantics.

**Approach:** Classify the existing guides, extend existing parity/publication checks, and reconcile documentation with executable behavior.

**Decision:** Include the minimal write-safety correction and regression coverage needed to refuse symlinked migration targets.

## Boundaries & Constraints

**Always:** Preserve named inspect/migrate v1 JSON contracts, deterministic output, dry-run default, per-file atomic replacement, path boundaries, redacted default paths, and accepted history. HFCM9002 remains synthetic/manual-only; production emission is unapproved. Preserve unrelated retrospective edits and statuses.

**Never:** Add executable edges, public APIs, registries, replacement validators, or evidence bundles; reimplement CLI/generated output; change dependencies, submodules, Fluent approval, release baselines, or historical acceptance. Do not promise semantic secret scrubbing or whole-run rollback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Catalog/index parity | Executable edge or manual package/API guide | Exact versions, guide path and classification agree | Missing, duplicate, stale or false classification fails deterministically |
| Migration | Supported edge, default dry-run or explicit apply | Preview writes nothing; eligible edits use atomic per-file replacement | Generated, submodule, symlinked, outside-root, bin, obj and .git targets refused |
| Inspection | Generated families, sidecars, severity/type filters | Text/JSON counts and ordinal order agree; fail flags use filtered diagnostics | Unsafe paths redacted; malformed sidecars produce HFCM0002 |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs`: private Edges is authoritative; only 9.1.0 to 9.2.0 is executable.
- `eng/validate-docs.ps1`: existing migration metadata/completeness and normalized producer-hash gates. Extend these; no independent registry.
- `src/Hexalith.FrontComposer.Cli/README.md` and `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs`: reuse inspect behavior; public reference incorrectly promises fingerprints.
- `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs`, `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`: existing safety seams called by planner and applier; currently permit some symlink targets.

## Tasks & Acceptance

**Execution:**
- [x] `docs/migrations/index.md`, `docs/migrations/9.1-to-9.2.md`, `docs/migrations/1.12-to-2.0.md`, `docs/migrations/3.1-to-4.0.md`, `docs/migrations/4.5-to-4.6.md` — classify guides and indexed edges using one frontmatter field, migrationMode: cli-supported or manual-only. Preserve exact 3.1.1 metadata. Explain the CLI-only HFCM9001 rename and synthetic HFCM9002 boundary.
- [x] `docs/how-to/migration-guides.md`, `docs/reference/cli.md` — reconcile default dry-run, explicit versions, per-file atomic apply/partial failure, exclusions and supported schemas. Describe inspect families/counts, shared ordering, severity/type filtering before fail flags, sidecars, relative paths, explicit absolute-path opt-in and bounded output accurately.
- [x] `eng/validate-docs.ps1`, `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` — extend existing gates to compare complete catalog tuples and classified index/guides bidirectionally. Exercise missing/unknown classification, false executable/manual claims, missing/duplicate/stale index links, version/path mismatch and missing catalog guidance. Keep diagnostics relative and deterministic; do not hardcode a replacement edge list. Source extraction must ignore comments and unrelated initializers.
- [x] `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs`, `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`, `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` — refuse file links and linked ancestor directories/junctions before source reads and at apply revalidation. Cover internal/external targets, explicit Compile Link metadata and plan-to-apply link replacement; assert links and target bytes remain unchanged. Retain ordinary-file migration and existing exclusions.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — extend existing documentation coverage; use PascalCase test names.
- [x] `docs/validation/producer-fingerprints.json` — refresh only the existing 9.1 guide's normalized hash after intentional edits.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md` — record exact checks and complete File List; preserve unrelated statuses and keep Epic 15 in progress.

**Acceptance Criteria:**
- Given the catalog and published guides, when parity runs, then every executable edge has matching guidance, manual-only changes are explicit, and negative fixtures reject drift.
- Given migration targets, when dry-run or apply runs, then unsafe targets are refused without changing link/target bytes, ordinary eligible edits retain atomic file replacement, failures keep applied=false, and frontcomposer.cli.migrate.v1 is preserved.
- Given inspect output, when documentation and existing behavior checks are compared, then forms/grids/registrations/manifests, ordering, severity/fail behavior, default redacted paths and frontcomposer.cli.inspect.v1 agree.
- Given final published edits, when documentation validation runs, then snippets and DocFX pass without introducing parallel inventories or changing CLI/generated contracts.

## Implementation Notes

- 2026-10-22 completion: all three independent review layers returned findings (the blind layer completed after a service-filtered first attempt). Every finding has an individual verdict; all accepted direct patches passed final parent verification. One pre-existing unsupported-sidecar runtime issue is deferred; Windows file-link/junction execution remains unverified on this Linux host. Spec is done, the sprint story is review for maintainer handoff, and Epic 15 remains in progress.
- Local commit preparation: `node_modules/.bin/commitlint --version` initially failed with exit 127 because the workspace tooling was absent. `npm ci` restored the existing lockfile's tooling without changing dependency manifests/versions. Pinned commitlint 21.2.2 then validated the exact full candidate with `node_modules/.bin/commitlint --edit /tmp/frontcomposer-15-3-commit-message.txt --verbose`: zero problems/warnings, exit 0. Successful evidence: /tmp/frontcomposer-15-3-commitlint.log. Only the 16 owned paths will be included; no push is authorized by this workflow.

- 2026-10-22 parent acceptance audit: read all 1,056 lines of the cumulative diff from the captured baseline, including the approved planning delta, and independently inspected final logs. All seven tasks and four acceptance criteria are satisfied. Matrix coverage executed with zero errors/failures/skips/unrun cases: catalog/index parity through positive and 18 negative fixtures plus actual publication probes; migration through existing dry-run/atomic/exclusion cases and 13 new link/outside-root cases; inspect through its complete existing command class. Final focused evidence is 96 CLI plus 10 documentation tests. Full publication passed with eight compiled snippets and five producer fingerprints. The exact 15 changed paths equal the File List. The frozen block matches the approved scope; the recorded AppHost baseline and unexecuted Windows junction branch remain validation limits. Audit diff: /tmp/frontcomposer-15-3-acceptance-0l_wqemq.diff.

- 2026-10-22: User approved the complete spec and continuation. Captured baseline before implementation; the frozen intent is locked.
- Required baseline command `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited 2 before launch: NETSDK1004 missing project.assets.json in unchanged root-declared Commons.UniqueIds and Memories projects, with an MSB3026 copy-contention warning. Log: /tmp/frontcomposer-15-3-apphost-baseline.log. `aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` and the matching `aspire describe --format Json` both exited 0; no running resources remain. Logs: /tmp/frontcomposer-15-3-apphost-stop.log and /tmp/frontcomposer-15-3-apphost-describe.log. Keep this baseline limitation separate from focused story validation.

- 2026-10-22 implementation: classified the four migration guides and their index rows with `migrationMode`, retaining the exact `3.1.1` source version. Reconciled public migration/inspect guidance with the existing v1 contracts, CLI-only `HFCM9001` fix, and synthetic/manual-only `HFCM9002` boundary. Refreshed only the existing 9.1 guide producer hash.
- 2026-10-22 implementation: extended the existing publication gate to compare complete private-catalog tuples, guide metadata, and classified index links bidirectionally using ordinal paths and deterministic relative diagnostics. Token extraction ignores comments, string contents, and unrelated initializers. Regression fixtures exercise 18 negative drift cases and compare repeated diagnostics.
- 2026-10-22 implementation: the shared write-safety policy now refuses file links and every linked ancestor/reparse point before source reads and during existing apply revalidation. Regressions preserve internal/external target bytes and link identity in dry-run/apply, cover explicit Compile Link metadata and post-plan replacements, and retain successful earlier writes after later failure. Directory fixtures use symlinks on Linux and junctions on Windows; Windows execution remains outside this Linux run.

- 2026-10-22 review fixes: manual-only classification now checks executable version-pair membership independently of the guide path. Catalog extraction recognizes raw string literals and rejects conditional syntax with a relative diagnostic. Published index parity ignores HTML comments and fenced blocks, requires complete header/separator rows, and catches unclassified fragment/query links. Added matching drift fixtures plus nested linked-grandparent cases.
- 2026-10-22 review fixes: retained an automated isolated real-publication command test with heavy stages skipped and a catalog-only mismatch; fixture setup copies inputs without symlinks. Process timeouts terminate the process tree and drain output. Added deterministic pre-cancelled text/JSON migration cases and same-named non-API identifier coverage without changing the scanner or DiagnosticFileReader.
- 2026-10-22 review fixes: documented the syntactic rename, nameof exclusion, required diff review, customization prerequisites, emitted-report cancellation limits, and unsupported sidecar-shape limitation. Refreshed only the existing migration-guide producer hash. The earlier complete verification below covers the initial implementation; the parent owns full verification after these focused review fixes.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1 | medium | patch | Complete-tuple matching includes the filename, so an added manual guide can contradict an executable version pair. Parent reran the isolated manual-alias fixture and observed no failures. Compare pair membership independently from guidance-path matching. |
| BH-2 | low | patch | Parent reran the raw-string fixture: unrelated valid C# text breaks extraction because the tokenizer recognizes ordinary/verbatim strings only. Directly extend literal recognition and the existing positive extraction fixture. |
| BH-3 | medium | patch | Parent reran an inactive BuildEdges initializer plus active empty Edges: parity returns no failures although runtime has no edge. Reject unsupported conditional catalog syntax rather than interpreting inactive declarations as executable. |
| BH-4 | medium | patch | Parent reran commented and fenced migration tables; both pass despite disappearing as published navigation. Exclude those regions before classifying visible index rows. |
| BH-5 | medium | patch | MigrationDiagnosticScanner scans IdentifierNameSyntax, excluding nameof, without invocation/semantic validation. New invocation-only prose overstates this unchanged scanner. Document the syntactic limitation and review requirement; do not broaden runtime changes. |
| BH-6 | medium | patch | CliApplication catches cancellation outside report rendering and returns 4; planning/output cancellation can leave stdout empty. New blanket applied=false wording must distinguish emitted reports from absent reports. |
| BH-7 | medium | defer | Primitive JSON roots/non-object entries reach TryGetProperty on a non-object and throw InvalidOperationException. DiagnosticFileReader and its catch list are identical in the captured baseline; unsupported-shape handling is a pre-existing runtime issue, beyond the approved symlink-only safety exception. Record it separately; correct new blanket prose under P-2. |
| BH-8 | low | reject | Real Windows file-link fixtures require OS privilege or Developer Mode; no failing configured Windows lane was established and this run claims Linux coverage only. Keep the environmental requirement explicit in validation limits; silently skipping or adding capability branches would not provide the required file-link evidence. |
| BH-9 | low | patch | WaitForExitAsync cancellation currently leaves the PowerShell child alive. Reuse the existing ToolPackagingSmokeTests process-tree cleanup pattern and drain redirected streams. |
| EC-1 | medium | patch | HTML-comment table acceptance is independently reproduced; same root cause as BH-4, retained as its own finding before grouping. |
| EC-2 | medium | patch | The current prefix-only header/separator skip ignores an appended malformed duplicate row beginning fromVersion or ---. Match complete header/separator rows instead. |
| EC-3 | medium | patch | The non-row regex ends at .md), so an unclassified duplicate fragment link is invisible. Include fragment/query suffixes when recognizing guide links outside classified rows. |
| VG-1 | medium | patch | Pre-verified regression gap: both current directory-link theories place the link directly above the source. Add nested-directory variants so checking only the immediate parent fails the tests. |
| VG-2 | medium | patch | Pre-verified regression gap: committed negative tests extract and invoke functions, so deleting the publication entry-point call leaves them green. Retain an actual-script negative fixture with catalog-only drift. |
| P-1 | medium | patch | Existing Story 15.2 round-4 deferral explicitly assigns migration guidance to this story. The renamed dev-mode registration requires a DEBUG Shell assembly and explicit Development environment; published Release packages do not enable it. Link these prerequisites from the corrected migration guidance. |
| P-2 | medium | patch | New generic failure-output assurance exceeds CliApplication's handled-exception paths. Qualify it and document unsupported sidecar shapes without promising a new runtime contract; the existing runtime issue remains BH-7's deferral. |

Grouping: BH-4 + EC-1 share hidden Markdown acceptance. All other survivors have distinct demonstrated causes. Each patch is a direct correction within existing documents, validator branches or test helpers; no public surface or unproven runtime state is added. No intent-gap or bad-spec loopback is required.

Review patch resolution: BH-1 now checks executable version pairs independently of guide paths; BH-2 recognizes raw literals; BH-3 rejects conditional catalogs. BH-4/EC-1 ignore commented/fenced rows; EC-2 checks complete header/separator rows; EC-3 recognizes unclassified fragment/query links. VG-1 now covers linked grandparents beneath ordinary nested directories; VG-2 runs the actual publication entry point with a catalog-only mismatch in an isolated regular-file copy. BH-9 terminates and drains timed-out PowerShell children. BH-5 documents the existing syntactic rename, BH-6 documents missing/partial cancellation reports, P-1 links DEBUG/Development registration prerequisites, and P-2 qualifies handled-failure/unsupported-shape assurances. All patch paths were independently read by the parent. BH-7 remains the recorded pre-existing runtime deferral; BH-8 remains a disclosed Windows capability limitation rather than skipped evidence. The final parent CLI run passed 117/117 and documentation class passed 10/10, all with zero errors/failures/skips/unrun cases. Frozen-block equality was verified against the baseline plus the two explicitly approved scope edits.

## Design Notes

No irreversible action is proposed. Earlier successful writes remain after later failure. Baseline CLI Debug build passed; temporary probes confirmed the symlink gap.

## Verification

Final parent verification after all review patches (2026-10-22):

- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` — passed 117/117, zero errors/failures/skips/unrun cases, 308.107 seconds; `/tmp/frontcomposer-15-3-parent-cli-final.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — passed 10/10, zero errors/failures/skips/unrun cases; `/tmp/frontcomposer-15-3-parent-docs-tests-final.log`.
- `pwsh ./eng/validate-docs.ps1` — passed on the final patched tree, including snippet compilation, the solution Release build and DocFX publication; zero blocking failures in `artifacts/docs/validation-manifest.json`; `/tmp/frontcomposer-15-3-parent-publication-final.log`.
- Global artifact validation and per-story validation with all 16 File List paths supplied explicitly passed. The 16 owned tracked changes equal the File List; the documented untracked concurrent retrospective spec is excluded from ownership. `git diff --check` passed.

Review-fix focused checks (2026-10-22); complete verification remains with the parent after these edits:

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Debug --no-restore` — passed, zero warnings/errors; `/tmp/frontcomposer-15-3-review-cli-build.log`.
- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -method '*MigrationPublication*' -method '*MigrationRefusesFileAndAncestorLinksBeforeReadingSource' -method '*MigrationApplyRefusesLinksIntroducedAfterPlanningAndKeepsEarlierWrites' -method '*MigrationPreCancelledBeforePlanningReturnsFourWithoutAReport' -method '*MigrationSyntacticRenameIncludesSameNamedNonApiIdentifiersAndExcludesNameof'` — passed 52/52, zero failures/skips; `/tmp/frontcomposer-15-3-review-cli-focused.log`. Includes 27 repeated negative parity cases, raw strings, actual isolated publication wiring with a catalog-only mismatch, nested linked ancestors, pre-cancelled output, and syntactic non-API rename behavior.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore` — passed, zero warnings/errors; `/tmp/frontcomposer-15-3-review-source-tools-build.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -method '*MigrationGuidanceDescribesCliSafeFixAndSyntheticManualBoundary' -method '*CliReferencePinsInspectCountsOrderingFilteringAndPathOptIn'` — passed 2/2, zero failures/skips; `/tmp/frontcomposer-15-3-review-docs-focused.log`.
- `git diff --check` — passed after review-fix bookkeeping. Only the existing migration-guide normalized producer hash changed; no new File List entries were needed for the review fixes.

Initial implementation checks, before the review fixes:

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Debug --no-restore` — passed with zero warnings/errors; final log `/tmp/frontcomposer-15-3-cli-build-final.log`. The initial no-restore attempt lacked test assets; the ordinary focused Debug build restored them. Intermediate owned helper errors were corrected before this final build.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug` — passed with zero warnings/errors; log `/tmp/frontcomposer-15-3-source-tools-build.log`.
- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` — passed 96/96, zero failures/skips; final log `/tmp/frontcomposer-15-3-cli-tests-final.log`. The initial helper-only JSON wrapping failure was fixed; this final run uses a separate log.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — passed 10/10, zero failures/skips; log `/tmp/frontcomposer-15-3-docs-tests.log`.
- `pwsh ./eng/validate-docs.ps1` — passed: zero blocking failures, eight compiled snippets passed, all five producer fingerprints checked, and DocFX `2.78.5` publication completed. Final log `/tmp/frontcomposer-15-3-validate-docs-final.log`; existing manifest `artifacts/docs/validation-manifest.json`. The earlier attempt reached the internal Release solution build and exposed an owned junction-test helper return-type error; that error was corrected before the final focused build. This is separate from the unchanged AppHost baseline limitation above.
- `python3 /tmp/story-15-3-publication-probes.py` — isolated copies invoke the actual `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` publication gate: valid baseline passed; missing classification, false manual classification, and duplicate index links failed as intended. Final log `/tmp/frontcomposer-15-3-publication-probes-final.log`.
- `python3 eng/validate-story-artifacts.py` — passed.
- Per-story owned-path command below — passed. Removed the unchanged epic context from the File List; its frontmatter context reference remains intact.

```shell
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md \
  --changed-file _bmad-output/implementation-artifacts/deferred-work.md \
  --changed-file docs/how-to/migration-guides.md \
  --changed-file docs/migrations/index.md \
  --changed-file docs/migrations/9.1-to-9.2.md \
  --changed-file docs/migrations/1.12-to-2.0.md \
  --changed-file docs/migrations/3.1-to-4.0.md \
  --changed-file docs/migrations/4.5-to-4.6.md \
  --changed-file docs/reference/cli.md \
  --changed-file docs/validation/producer-fingerprints.json \
  --changed-file eng/validate-docs.ps1 \
  --changed-file src/Hexalith.FrontComposer.Cli/PathUtilities.cs \
  --changed-file src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs \
  --changed-file tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs \
  --changed-file tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs \
  --changed-file _bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md \
  --changed-file _bmad-output/implementation-artifacts/sprint-status.yaml
```

- `git diff --check` — passed after final story bookkeeping.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/spec-epic-13-retrospective-follow-through.md` — untracked concurrent retrospective follow-through work, first observed during the review patch pass. This story did not create, edit, stage or claim it.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/how-to/migration-guides.md`
- `docs/migrations/index.md`
- `docs/migrations/9.1-to-9.2.md`
- `docs/migrations/1.12-to-2.0.md`
- `docs/migrations/3.1-to-4.0.md`
- `docs/migrations/4.5-to-4.6.md`
- `docs/reference/cli.md`
- `docs/validation/producer-fingerprints.json`
- `eng/validate-docs.ps1`
- `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`
- `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`
