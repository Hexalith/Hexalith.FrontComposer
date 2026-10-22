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

### Review Findings

Post-commit code review of `bf40b927..02af2219` over the File List paths (2026-10-22). Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; none failed. Severity in brackets.

- [x] [Review][Patch] CR-1 [high] Bound the link-segment walk to the project root [src/Hexalith.FrontComposer.Cli/PathUtilities.cs:37] — `HasLinkedPathSegment` walks every ancestor to the filesystem root, so a project reached through any symlinked ancestor (macOS `/tmp` or `/var`, Fedora Atomic `/home`, a linked workspace folder) has every document skipped. Built-CLI probe: the real path gives `changed: 1`; the symlinked-ancestor path gives `changed: 0, skipped: 1`, and `--apply` reports `applied: true`, exit 0, source unchanged. The baseline migrated it. Stop the walk at the project directory, since only segments below it can redirect a write. Add a symlinked-ancestor regression, and scope the docs' "linked ancestor directories" wording to the project.
- [x] [Review][Patch] CR-2 [medium] Extend the unsupported-sidecar caveat to `migrate` [docs/migrations/9.1-to-9.2.md:110] — `MigrationDiagnosticSidecarReader` crashes the same way (probe: a scalar-root sidecar gives an unhandled `InvalidOperationException`, exit 134, and no report). The caveat appears only in inspect paragraphs, yet the new HFCM9002 text invites hand-crafted sidecars. Add the caveat to the migrate guidance in `docs/reference/cli.md` and `docs/how-to/migration-guides.md`. The runtime crash is CR-D2.
- [x] [Review][Patch] CR-3 [medium] Cover the "unsupported tuple syntax" fail-closed branch [eng/validate-docs.ps1:212] — no parity scenario leaves residue in the `Edges` body. A mutant that deletes the `$remaining` check passes all 27 scenarios while an executable `new MigrationEdge("4.5.0", ...)` edge escapes classification. Add an `unsupported-catalog-syntax` InlineData.
- [x] [Review][Patch] CR-4 [medium] Correct the handled-failure privacy claim [docs/reference/cli.md:64] — `CliApplication.cs:36` prints `IOException`/`UnauthorizedAccessException` messages through `OutputSanitizer.Sanitize`, which only escapes control characters. OS messages with absolute paths therefore reach stderr, contrary to "handled CLI failures avoid ... private absolute paths".
- [x] [Review][Patch] CR-5 [low] Name links in the skipped-document reason [src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs:63] — refused links, `.git` and outside-root targets report "outside generated, bin, obj, package cache, and submodule paths are eligible" with "Excluded path.". No test pins this text.
- [x] [Review][Patch] CR-6 [low] Drop the unchecked "only executable edge" claim [docs/reference/cli.md:45] — the parity gate never reads cli.md, so a new catalog edge leaves this sentence stale (AC4 parallel inventory). Point to the classified index instead.
- [x] [Review][Patch] CR-7 [low] Restrict the unclassified-link check to sibling guides [eng/validate-docs.ps1:297] — any `.md` link in index prose, such as `../reference/cli.md`, fails as an unclassified guide link.
- [x] [Review][Patch] CR-8 [low] Correct the null-related-type filter claim [docs/reference/cli.md:32] — sidecar diagnostics get `RelatedType = Sanitize(...)`, which is empty and never null (`DiagnosticFileReader.cs:29`). `--type` keeps only null (`TypeMatcher.cs:35`), so project-wide sidecar diagnostics are dropped. Only the HFCM0002 sentinel has null.
- [x] [Review][Patch] CR-9 [low] Assert the positive parity fixture injected its decoys [tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs:1018] — reformatting only the whitespace of the `Edges` declaration turns the `Replace` into a no-op, while the tokenized validator still passes, so the test proves nothing.
- [x] [Review][Patch] CR-10 [low] Stop pinning wrap positions in docs tests [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs:213] — `"do not\nsemantically scrub"` and split phrases (`"`applied=false` on"` plus `"emitted reports"`) break when the text is rewrapped. Normalize whitespace and assert whole phrases.
- [x] [Review][Patch] CR-11 [low] Add the 3.1 to 4.0 guide to site navigation [docs/toc.yml:14] — `toc.yml` lists three of the four guides (pre-existing; one-line fix).
- [x] [Review][Patch] CR-12 [low] Give the BH-7 deferral a Story 15.3 heading [_bmad-output/implementation-artifacts/deferred-work.md:10686] — it was appended under Story 15.1's heading.
- [x] [Review][Patch] CR-13 [low] Replace validator jargon in adopter-facing index prose [docs/migrations/index.md:20] — "govern parity".
- [x] [Review][Defer] CR-D1 Non-link reparse points inside the project may be refused as links [src/Hexalith.FrontComposer.Cli/PathUtilities.cs:39] — deferred: maybe-false, medium if true. The check tests `FileAttributes.ReparsePoint`, which also marks OneDrive placeholders, deduplicated files and mounted folders. To settle it, check `File.GetAttributes` and `LinkTarget` for a Files-On-Demand placeholder on Windows under default placeholder compatibility.
- [x] [Review][Defer] CR-D2 `migrate` crashes on scalar-root or non-object-entry sidecars [src/Hexalith.FrontComposer.Cli/MigrationDiagnosticSidecarReader.cs:41] — deferred: pre-existing (the reader is unchanged from the baseline). It is the same runtime gap as BH-7 and falls outside the approved symlink-only runtime exception.

Rejected:

- CR-R1 TOCTOU between apply revalidation and write — low: it needs concurrent local tampering during a developer-run command. Reordering would not close the temp-file/move window, and a real fix needs handle-based no-follow opens. The "revalidated immediately before write" wording predates this story.
- CR-R2 A link-probe IO error turns a failed read into a skipped entry — low: it needs an explicit Compile item under a non-traversable directory, and the fix adds a tri-state result.
- CR-R3 Catalog tokenizer gaps (char literals, `@$` verbatim strings, `__literalN` identifiers) — false: `MigrationCatalog.cs` has no char literals (its only `'` characters are inside strings at lines 27-31) and no such identifiers. Any misalignment fails loudly.
- CR-R4 An empty `BuildEdges([])` is not recognized — this is a loud failure on a state the catalog never reaches.
- CR-R5 A missing `MigrationCatalog.cs` terminates the validator — this is a loud failure on a state that cannot occur in a full checkout.
- CR-R6 `$matches` shadows the automatic `$Matches` — false: each `$Matches` read follows a successful `-match`, and no PSScriptAnalyzer runs in eng/ or CI.
- CR-R7 Guide links written as reference definitions, HTML anchors, `<dest>`, titled links or `.MD` bypass the index gate — low: this needs deliberately unusual markup, and the fix adds branches.
- CR-R8 The isolated publication test depends on a hand-kept input list and on pwsh — low: a missing input fails loudly; carried from the prior VG-2 trade-off.
- CR-R9 The docs index test duplicates the gate and hardcodes 3.1.1 — false: Tasks 1 and 5 require preserving the exact 3.1.1 and extending DocsSiteValidationTests, and a missing key still fails loudly.
- CR-R10 The rename docs omit declarations, generics and crefs that are not renamed, and keep the "Analyzer And Code Fix" heading — carried: the prior BH-5 accepted the "not semantic validation; review the diff" caveat.
- CR-R11 The dev-mode paragraph drops the type-registration and WebAssembly caveats — false: it requires an `IHostEnvironment` in DI, limits detection to instance and factory registrations, and links the canonical reference.
- CR-R12 The inspect contract omits item fields, exact family strings, severity case and the CLI-help `information` alias — low and pre-existing; AC3 does not require item fields to be enumerated.
- CR-R13 The packaged CLI README was not updated — false: it makes no write-exclusion claim that this change made stale.
- CR-R14 Windows junction and file-link coverage, and file-link failures without elevation — carried from the prior BH-8 (a disclosed Windows limitation).
- CR-R15 The how-to migration page has version frontmatter without `migrationMode` — false: no validator or test reads version metadata from how-to pages.
- CR-R16 BOM-less UTF-16/32 ASCII is decoded as UTF-8 instead of failing closed — low; both the runtime behaviour and the claim are pre-existing, and the fix needs heuristic detection.
- CR-R17 The index table is not in version order — false: it keeps the baseline list order, and no reader harm was identified.

## Implementation Notes

- 2026-10-22 final local commit preparation: the owning repository lacked node_modules/.bin/commitlint (exit 127). `npm ci` restored the existing lockfile tooling without changing dependency manifests or versions. Pinned commitlint 21.2.2 validated the exact candidate message with `node_modules/.bin/commitlint --edit /tmp/frontcomposer-15-3-resume-commit-message.txt --verbose`, zero problems/warnings; `/tmp/frontcomposer-15-3-resume-commitlint.log`. Completion bookkeeping was rechecked before the local commit; no push is authorized.

- 2026-10-22 final resumed completion: all three independent review layers completed; every finding has an individual verdict. Five accepted patch groups passed final parent verification, two pre-existing issues were added to deferred work, and the other low findings were rejected under the review rules. Spec is done and the sprint story is review; Epic 15 remains in progress. The AppHost environment check remains blocked by unchanged Memories.Client.Rest assets. Windows reparse behavior and unsupported sidecar shapes retain their existing deferrals. Unrelated Platform changes and the already-committed retrospective were preserved.

- 2026-10-22 resumed independent-review patches: R2-BH-1/R2-EC-1 now deduplicate normalized lexical source paths so refusing an alias preserves its ordinary target in either Compile order. R2-BH-2 resolves ancestor links through the existing Canonical/planned-target comparison, refusing project-root and workspace-ancestor retargeting with identical bytes while preserving normal linked-workspace operation and existing partial-write behavior. The earlier direct-root-link mismatch note is superseded by this ancestor-resolution correction. R2-BH-4 tokenizes character literals with asserted positive decoys; R2-BH-5 checks exact semantic versions in both guide fields; R2-BH-8 checks canonical classification-key spelling. Parent independently read the complete patch. The focused Debug build and 62 targeted tests passed with zero failures/skips; logs: `/tmp/frontcomposer-15-3-review2-cli-build-final.log`, `/tmp/frontcomposer-15-3-review2-focused-tests.log`. Full final verification follows on the parent.

- 2026-10-22 resumed parent acceptance audit: independently read the cumulative 1,678-line diff from the captured baseline and inspected the final build/test/publication logs and manifest. All seven execution tasks, all 13 accepted CR patches, and all four acceptance criteria are satisfied. Matrix coverage ran in the full 122-test migration/inspect lane with zero errors/failures/skips/unrun cases: complete catalog tuples and 28 deterministic negative parity fixtures plus the actual publication entry point; default dry-run, ordinary atomic apply, excluded/submodule/outside-root targets, internal/external links before reads and after planning, and linked workspace ancestors; generated-family counts, shared text/JSON ordering, severity/type filtering before fail flags, hostile-path redaction, and malformed-JSON HFCM0002. The 10 docs tests and final publication (eight snippets, five fingerprints, zero blocking failures) passed. Frozen intent is unchanged from resumed HEAD; original baseline is preserved. The unrelated retrospective is excluded. Audit diff: `/tmp/frontcomposer-15-3-resume-acceptance-92oldx5a.diff`.

- 2026-10-22 resumed post-commit review: implemented CR-1 through CR-13 without changing the frozen intent. The link-segment walk now stops before the selected project directory: file links and linked ancestor segments below that root remain refused, while linked workspace ancestors above it are allowed. Regressions exercise two ancestor depths in dry-run and apply and retain the existing internal/external link-preservation checks. Skipped-document reasons now name linked, outside-root, and `.git` paths.
- 2026-10-22 resumed post-commit review: added the unsupported-catalog-syntax negative fixture, asserted all positive catalog decoys were injected, and limited unclassified prose-link rejection to sibling migration guides; an unrelated CLI-reference prose link is now accepted. Migration-sidecar unsupported-shape caveats now accompany migrate guidance and the hand-crafted HFCM9002 example. Corrected filesystem-error privacy and empty-related-type filtering claims, replaced the unchecked executable-edge inventory with the classified index, normalized whitespace in prose assertions, added the 3.1 guide to navigation, gave the BH-7 deferral its Story 15.3 heading, and removed validator jargon from the index. Only the existing 9.1 guide producer hash was refreshed.
- 2026-10-22 resumed environment check: `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` exited 2 with NETSDK1004 for unchanged Memories.Client.Rest assets and MSB4166 child-node exits. The first patch batch preceded this resumed check; it is not a pristine pre-edit baseline. The historical pre-edit baseline below remains authoritative. Matching stop and describe commands exited 0 and confirmed no selected AppHost running. Logs: `/tmp/frontcomposer-15-3-resume-apphost-start.log`, `/tmp/frontcomposer-15-3-resume-apphost-stop.log`, and `/tmp/frontcomposer-15-3-resume-apphost-describe.log`.
- 2026-10-22 additional scope probe: selecting a project directory that is itself a direct directory link yields zero selected source edits in the unchanged `ProjectDocumentLoader`, because its canonical root differs from lexical child paths. The temporary extra probe passed the two above-root ancestor cases and failed the two direct-root-link cases (`changed: 0`); `/tmp/frontcomposer-15-3-resume-linked-root-probe.log`. This is an existing loader limitation, outside CR-1's linked-ancestor correction; the retained regression explicitly covers ancestors above the selected root, not a directly linked project directory. The loader and the Windows reparse-point/unsupported-sidecar runtime deferrals remain unchanged.

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

Resumed independent review (2026-10-22): Blind Hunter returned ten findings; Edge Case Hunter returned one; Verification Gap returned no gaps. Each finding is adjudicated below before grouping. The parent independently traced the runtime callers and reran isolated publication probes at `/tmp/frontcomposer-15-3-triage-reecqwcq`.

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| R2-BH-1 | medium | patch | `ProjectDocumentLoader.Load` groups by canonical target before the planner refuses links. An explicit linked alias preceding its ordinary target is retained alone and skipped, preventing the eligible ordinary file from migrating. Preserve distinct lexical documents until unsafe aliases are refused; add order-sensitive coverage. |
| R2-BH-2 | high | patch | `HasLinkedPathSegment` stops before the project directory; `Canonical(edit.FullPath)` resolves only a leaf/immediate parent link. Replacing a planned project root with a directory link carrying identical bytes therefore reaches `SourceFile.WriteAsync` on a different target. Preserve and revalidate the resolved target using the existing planned canonical-path seam, including changed ancestor links, while retaining pre-existing workspace links. |
| R2-BH-3 | medium | defer | Recursive `Directory.EnumerateFiles(..., AllDirectories)` traverses linked directory cycles before any planner safety check. This traversal is unchanged from the captured baseline, so the new refusal does not cause it. Record cycle/default-glob traversal as separate loader work. |
| R2-BH-4 | medium | patch | Parent added the valid unrelated declaration `private const char Quote = '"';` to an isolated catalog: the tokenizer lost the real initializer and emitted two failures. Recognize character literals with the existing literal-tokenization seam and extend the positive fixture. This new demonstrated fixture supersedes the earlier CR-R3 assumption that no relevant character literal exists. |
| R2-BH-5 | medium | patch | Parent changed a manual guide fromVersion and its matching index cell to `latest`; parity returned no failures. Relational agreement alone admits a non-version into exact upgrade guidance. Extend the existing metadata check to reject non-exact version values and exercise both fields. |
| R2-BH-6 | low | reject | Parent reproduced the reference-style duplicate-link bypass. This carries the earlier CR-R7 decision: ordinary checked-in migration links use inline Markdown; supporting unusual link syntax requires additional parsing branches for a rare authoring pattern. |
| R2-BH-7 | low | reject | Redirected stream completion has no separate deadline, but current publication invocations do not intentionally detach descendants and timeout cleanup kills the process tree. A surviving detached descendant or failed kill could hold a pipe; bounding both drains needs extra cancellation/cleanup branches for a rare test-infrastructure condition. |
| R2-BH-8 | low | patch | Parent renamed a manual guide's field to `MigrationMode`; parity still passed because PowerShell metadata lookup ignores key case, whereas the docs test uses a case-sensitive dictionary. Require the canonical spelling in the existing migrationMode check and add a negative fixture. |
| R2-BH-9 | medium | defer | The New Code snippet lacks `using Hexalith.FrontComposer.Shell.Extensions;`, where AddFrontComposerDevMode is declared. The complete snippet is unchanged from the captured baseline; fix its existing import as separate documentation follow-up rather than expanding this runtime safety patch. |
| R2-BH-10 | low | reject | The finding only requests editing the build spec's historical completion narrative. Workflow rules reject spec-only findings; the dated current acceptance record and final status transitions are maintained separately without deleting prior verification history. |
| R2-EC-1 | medium | patch | Independently reproduced the same linked-alias-before-target loss as R2-BH-1. The canonical GroupBy retains the refused link and drops the ordinary source before planning. |

Grouping: R2-BH-1 and R2-EC-1 share the loader's premature canonical deduplication. R2-BH-2, R2-BH-4, R2-BH-5, and R2-BH-8 have distinct demonstrated causes and each admits a direct correction in an existing seam without a public API. No intent-gap or bad-spec loopback is required. Pre-existing traversal and snippet-import issues are deferred individually; the other low findings are rejected under the workflow rules.

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

Final resumed parent verification after independent-review patches (2026-10-22):

- Both individual Debug test-project builds passed with zero warnings/errors.
- The complete direct-assembly MigrationCommandTests and InspectCommandTests lane passed **132/132**, with zero errors/failures/skips/unrun cases, 202.698 seconds. Log: `/tmp/frontcomposer-15-3-parent-resume-final-cli-tests.log`.
- The direct-assembly DocsSiteValidationTests lane passed **10/10**, with zero errors/failures/skips/unrun cases. Log: `/tmp/frontcomposer-15-3-parent-resume-final-docs-tests.log`.
- `pwsh ./eng/validate-docs.ps1` passed on the final runtime/validator tree: eight compiled snippets, five producer fingerprints, Release solution build, DocFX publication, and zero blocking failures. Log: `/tmp/frontcomposer-15-3-parent-resume-final-publication.log`; existing manifest: `artifacts/docs/validation-manifest.json`.
- `python3 eng/validate-story-artifacts.py` and its per-story invocation with all **19** File List paths passed. Both unstaged and staged `git diff --check` passed. Exact command arrays, exit codes, durations and logs for all nine final parent checks: `/tmp/frontcomposer-15-3-parent-resume-final-results.json`.
- Parent independently verified the updated cumulative diff, all five patch groups, and every matrix row's executed coverage. The catalog lane now includes 31 repeated negative fixtures. New alias-order and project-root/workspace-retarget cases verify final bytes and fail-closed behavior; all retained safety, partial-write and inspect cases passed. A direct built-CLI dry-run/apply probe also confirmed the directly linked project-directory case now works; `/tmp/frontcomposer-15-3-parent-direct-root-final.json`.
- Frozen intent matches resumed HEAD, the original full baseline is preserved, and the 19 File List paths exactly equal cumulative ownership after excluding the unrelated retrospective. Current audit diff: `/tmp/frontcomposer-15-3-resume-acceptance-92oldx5a.diff`.


Resumed implementation verification after CR-1 through CR-13 (2026-10-22); spec and sprint status remain `in-progress` for the parent's acceptance/matrix audit and independent review:

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Debug --no-restore` — passed, zero warnings/errors; `/tmp/frontcomposer-15-3-resume-cli-build-final.log`. Initial no-restore attempts lacked test assets; ordinary Debug builds restored the existing dependency graph without dependency-manifest changes. One nullable test assertion error was corrected before the final build.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore` — passed, zero warnings/errors; `/tmp/frontcomposer-15-3-resume-docs-build-final.log`.
- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` — passed 122/122, zero errors/failures/skips/unrun cases, 280.753 seconds; `/tmp/frontcomposer-15-3-resume-cli-tests.log`. Includes the 28 repeated negative parity fixtures, asserted decoy injection, unrelated-doc prose links, four linked-workspace-ancestor scenarios, all retained migration safety cases, and existing inspect behavior checks.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — passed 10/10, zero errors/failures/skips/unrun cases; `/tmp/frontcomposer-15-3-resume-docs-tests-final.log`.
- `pwsh ./eng/validate-docs.ps1` — passed on the final published wording, including eight compiled snippets, the solution Release build, all five producer fingerprints, and DocFX publication; zero blocking failures in `artifacts/docs/validation-manifest.json`; `/tmp/frontcomposer-15-3-resume-publication-final.log`.
- `python3 eng/validate-story-artifacts.py` — passed after final bookkeeping; `/tmp/frontcomposer-15-3-resume-artifact-global-final.log`. The exact per-story command below supplies the complete 18-path File List and passed after final bookkeeping; `/tmp/frontcomposer-15-3-resume-artifact-story-final.log`.
- Frozen-intent comparison against resumed HEAD passed. The 18 File List paths equal the cumulative owned paths from the captured baseline after excluding the already-committed unrelated retrospective. Only the existing 9.1 guide's normalized producer hash changed. `git diff --check` passed.

```shell
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md \
  --changed-file _bmad-output/implementation-artifacts/deferred-work.md \
  --changed-file _bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md \
  --changed-file _bmad-output/implementation-artifacts/sprint-status.yaml \
  --changed-file docs/how-to/migration-guides.md \
  --changed-file docs/migrations/index.md \
  --changed-file docs/migrations/9.1-to-9.2.md \
  --changed-file docs/migrations/1.12-to-2.0.md \
  --changed-file docs/migrations/3.1-to-4.0.md \
  --changed-file docs/migrations/4.5-to-4.6.md \
  --changed-file docs/reference/cli.md \
  --changed-file docs/toc.yml \
  --changed-file docs/validation/producer-fingerprints.json \
  --changed-file eng/validate-docs.ps1 \
  --changed-file src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs \
  --changed-file src/Hexalith.FrontComposer.Cli/PathUtilities.cs \
  --changed-file src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs \
  --changed-file tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs \
  --changed-file tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs
```

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

- `_bmad-output/implementation-artifacts/spec-epic-13-retrospective-follow-through.md` — concurrent retrospective follow-through work, first observed as untracked during the earlier review patch pass and already tracked at resumed HEAD. This story did not create, edit, stage or claim it.

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
- `docs/toc.yml`
- `docs/validation/producer-fingerprints.json`
- `eng/validate-docs.ps1`
- `src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs`
- `src/Hexalith.FrontComposer.Cli/PathUtilities.cs`
- `src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs`
- `src/Hexalith.FrontComposer.Cli/WriteSafetyPolicy.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`
