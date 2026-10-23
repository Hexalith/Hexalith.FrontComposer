---
title: 'Story 15.4: Correct the Fluent UI V5 Contingency Identity'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
story_id: '15.4'
story_key: '15-4-i-doc-c-correct-the-fluent-ui-v5-contingency-identity'
baseline_commit: '386ec94fe6de8c027edb7d108dcece0bf5b0f4e4'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The page describes rc.2 although the selected catalog supplies Components and Icons 5.0.1 GA. Its API/upgrade guidance is obsolete; validation and publication exclude it.

**Approach:** Publish accurate identity, API/token and contingency guidance, cite Story 13.9's dated decision, and extend the existing documentation gate to reject catalog/document drift.

## Boundaries & Constraints

**Always:** Read root HEAD's gitlink-selected catalog object. Document both package IDs/versions in one table. Report the 2026-10-21 Product Owner + Architect decision as **not approved**; keep OI-4, FLUENT-APP-1, D-13 and G-4 open. Preserve receipt lifecycle and evidence rules. Fail missing/ambiguous authority with deterministic relative diagnostics.

**Never:** Infer approval; change dependencies, gitlinks, runtime, evidence or unrelated statuses; implement canary/info-alias fixes; add registries, validators or evidence bundles. Correct the document without an exception.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Identity parity | Selected catalog and document table | Exactly one matching row per package | Missing, duplicate, unresolved or mismatched identities fail |
| Checkout divergence | Builds checkout differs from root-selected revision | Comparison still uses selected Git object | Missing gitlink/object fails; no checkout fallback |
| Approval posture | Selected GA version; dated negative decision | Explicit not-approved state, date and receipt citation | Selection never grants acceptance |
| Publication | Contingency page and normal docs command | Metadata, navigation, content and manifest include the page | Removed publication wiring is detected |

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` imports Builds; HEAD selects `4248b8c70cb35ba79fdd7e4dd63049d4c73211a0`. Its `Props/Directory.Packages.props` pins both packages to `5.0.1`.
- Story 13.9's `## Product Decision Receipt` supplies verified names and lifecycle rules.
- `eng/validate-docs.ps1`: reuse content enumeration, failures and manifest. Migration parity tests extract actual PowerShell functions through the AST.
- `DocsSiteValidationTests.ContentFiles()` omits the page; `MigrationCommandTests.CreatePublicationCopy()` lacks Git authority and needs isolated committed objects.

## Tasks & Acceptance

**Execution:**
- [x] `docs/fluent-ui-v5-contingency.md` — add metadata; replace stale identity, RC APIs, local pin/rollback commands, test counts and canary skeleton. Describe catalog-owned updates/rollback, Fluent 2 names, unsupported info token and FAST exclusion. Cite the dated receipt through a publication-safe repository link. Cover hosted Quality, separate human evidence, owner re-decision, patch/minor/major and breaking-change rules. State canary absence.
- [x] `docs/docfx.json`, `docs/toc.yml` — publish the page under How-to, retaining the four existing top-level sections.
- [x] `eng/validate-docs.ps1` — include the page in content/manifest inputs; invoke parity using selected catalog XML. Compare both table rows; reject absent/ambiguous authority and nonliteral versions. Derive expectations from the catalog; keep diagnostics bounded and relative.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — cover metadata, decision semantics and publication wiring. Exercise actual PowerShell with offline Git fixtures: match, either package missing/stale, duplicates, unresolved version, missing gitlink/object and advanced checkout. Prove integration with a full publication-command identity-drift case. Use PascalCase names.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` — adapt isolated publication fixtures to supply selected catalog objects; preserve migration-only failure assertions.
- [x] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — record checks, review and File List; reconcile 15.4, retaining Epic 15 in progress.

**Acceptance Criteria:**
- Given the selected catalog, when documentation validation runs, then identities match and negative fixtures reject drift.
- Given the receipt, when reading the published page, then API/token and upgrade guidance agrees, with explicit dated not-approved posture and unchanged approval gates.
- Given final changes, when publication/regression checks run, then the page is validated, reachable and published; migration probes retain their intended failures.

## Implementation Notes

No intent gaps or irreversible actions. Dispatch covers publication wiring, Git-object authority and isolated consumers. Preserve Story 15.3's gates, diagnostics and ownership conventions.

Approval recorded on 2026-10-23. Implementation starts at HEAD `8c61143ef4c6a983bfe1d25865857b9b4cd240c9`; the original baseline is preserved. Initial unrelated changes were captured in `/tmp/frontcomposer-15-4-initial-workspace.json` and `/tmp/frontcomposer-15-4-initial-workspace.diff`. Preserve the concurrent Story 13.6 change in sprint-status and all other existing edits.

Baseline check: `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` exited 2 before startup: MSB3030, nine missing Hexalith.Commons.UniqueIds DLL/PDB/XML copy inputs in referenced EventStore/Parties Debug outputs. Log: `/tmp/frontcomposer-15-4-aspire-baseline.log`. No AppHost resource assertions ran; do not repair unrelated runtime/dependency work for this documentation story.

## Spec Change Log

- 2026-10-23: Implemented the approved scope without changing frozen intent. The existing documentation validator now resolves root HEAD's Builds gitlink and reads its catalog Git object, rejects unresolved/ambiguous identities, checks the single two-package table and publication wiring, and includes the page in normal content/manifest inputs. Offline fixture commits are confined to temporary directories; the real repositories' index, dependencies and gitlinks are unchanged.

## Review Triage Log

- 2026-10-23: Parent performed an independent read-only audit of the owned code/documentation diff and found no runtime or catalog changes. Required workflow review and final closure remain with the parent; implementation is ready for review.

### Workflow review — 2026-10-23

All three layers ran at the parent model capability: Blind Hunter, Edge Case Hunter and Verification Gap. Each finding was graded before grouping. The broad baseline diff includes changes already present at implementation start; the user requested the Fluent contingency story, so unrelated runtime and accessibility work is outside that intent and is preserved.

| Finding | Verdict / disposition | Verified evidence |
|---|---|---|
| BH-1 | medium / patch, grouped with EC-2 | PowerShell `-like` misses DocFX's zero-directory `**/*.md` exclusion; the old gate accepted a page DocFX omitted. Reused the pinned DocFX matcher and added negative plus unrelated-exclusion positive cases. |
| BH-2 | medium / patch | The old TOC regex accepted an indented href inside a YAML description scalar. Require an items collection and named item; both description and item-name scalar spoofs now fail. |
| BH-3 | medium / patch | Fence/comment filtering left a table inside raw HTML pre content countable although it rendered as code. Track raw pre blocks outside fences and regress both hidden tables and unrelated pre examples. |
| BH-4 | medium / reject, outside requested intent | Contrast sampling starts outside-edge background resolution at the parent, losing target positioning. This helper predates 15.4 implementation and concerns concurrent Story 13.6 qualification, not the requested contingency guidance. No helper changes made. |
| BH-5 | medium / reject, outside requested intent | The helper resets out-of-flow state at the containing block before its fill coverage check. The reported escaped-fill case concerns concurrent Story 13.6 and is outside this story's documentation goal. |
| BH-6 | medium / reject, outside requested intent | Background coverage checks apply only to absolute/fixed escape, leaving relative offsets untreated. The existing contrast helper belongs to concurrent Story 13.6; it is preserved. |
| BH-7 | medium / reject, outside requested intent | The helper exempts body unconditionally even when opaque html prevents canvas propagation. This pre-existing helper behavior is outside the requested Fluent contingency correction. |
| BH-8 | medium / reject, outside requested intent | Bounding rectangles alone do not model rounded/background-clip fill footprints. This contrast qualification issue belongs to the concurrent accessibility work and is not changed here. |
| BH-9 | medium / reject, outside requested intent | A hidden host returns before pseudo-element paint sampling despite visible pseudos. This is a concurrent Story 13.6 helper issue; no runtime or browser helper change is part of this story. |
| BH-10 | low / reject, outside requested intent | Warning body comparison uses raw server Title while the rendered title can use the fallback, permitting duplicate fallback text. The emitter change was present before 15.4 implementation and is outside the contingency-document goal. |
| EC-1 | medium / patch | Exact package filtering ignored an expression target such as `Update="@(PackageVersion)"`, which can change both effective versions. Reject unresolved item targets; preserve unrelated literal targets and add negative/positive fixtures. |
| EC-2 | medium / patch, grouped with BH-1 | Independently reproduced the recursive exclusion false pass; the same pinned-matcher patch covers this finding. |
| EC-3 | medium / reject, outside requested intent | Independently reproduced BH-9's visible pseudo omission. It belongs to the concurrent accessibility helper, outside the requested documentation story. |
| EC-4 | low / reject, outside requested intent | Independently confirmed BH-10's effective-title mismatch. The pre-existing concurrent emitter work is preserved. |
| VG-1 | medium / reject, outside requested intent | Pre-verified: the AppHost timing test checks source strings, while Playwright starts Counter directly; commenting the call leaves the source assertion green. The reviewer identifies existing R-VG02 ownership under Story 13.6. This runtime-harness work is outside the requested Fluent contingency correction. |

Four owned root causes were patched through the original implementation agent. No owned finding remains unresolved, and no new deferred ledger entries were created. Ten review regressions cover six catalog/parser scenarios and four full publication cases. The first combined run exposed DocFX framework-copy selection in its positive exclusion case; the loader now chooses the highest compatible framework, and the final publication regression run passed 4/4.

### Matrix test audit

- Identity parity: `FluentCatalogParityUsesRootHeadSelectedObjectsAsync` plus review-regression catalog cases exercise actual PowerShell against isolated committed objects.
- Checkout divergence: AdvancedCheckout and DirtyCheckout cases pass using the selected object; missing gitlink/object/repository cases fail deterministically.
- Approval posture: `FluentContingencyPreservesDatedDecisionAndReceiptLifecycle` covers the date, negative decision, receipt, open gates and evidence/lifecycle wording.
- Publication: `FluentContingencyPublicationCommandChecksIdentityAndWiringAsync`, review publication cases and the metadata/navigation test cover real command integration, page input, DocFX exclusion, TOC and metadata.

The initial 43-case class passed without skips; the final parent run below covers all original and added cases on the patched tree.

## Verification

- Build affected test projects individually in Debug; run built xUnit v3 assemblies for docs and publication methods.
- `pwsh ./eng/validate-docs.ps1` — snippets/parity/DocFX pass; inspect HTML identity and decision.
- `python3 eng/validate-story-artifacts.py` plus per-story ownership checks — tracking/File List agree.
- `git diff --check` — clean. Record expanded commands and environmental blockers.

### Implementation verification — 2026-10-23

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0, zero warnings/errors. Log: `/tmp/story15_4_sourcetools_build.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — exit 0, 43 passed, zero failed/skipped/errors. Includes 25 offline Git scenarios, six full publication-command scenarios, metadata and dated decision/lifecycle checks. Actual PowerShell functions are extracted through the AST; repeat execution verifies bounded, relative, deterministic failures. Log: `/tmp/story15_4_doc_tests.log`.
- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0, zero warnings/errors. Log: `/tmp/story15_4_cli_build.log`.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` — exit 0, publication/metadata/parity manifest has zero blocking failures.
- `python3 eng/validate-story-artifacts.py` — exit 0, `Story artifact validation passed`. Log: `/tmp/story15_4_artifacts_global.log`.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/story15_4_fixture_commit.txt --verbose` — exit 0, zero problems/warnings for the exact full fixture message `test: seed catalog fixture`, before any fixture execution. Successful pinned-CLI evidence is preserved in `/tmp/story15_4_fixture_commitlint.log`. Commits exist only inside disposable offline fixture repositories.
- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` — exit 0, 127 passed, zero failed/skipped/errors. Both publication probes preserve their migration-only failures, and all migration safety/parity regressions pass. Log: `/tmp/story15_4_migration_tests.log`.
- `pwsh ./eng/validate-docs.ps1` — exit 0. All eight compile snippets pass; normal Release build, DocFX metadata and DocFX build complete; manifest has zero blocking failures. Log: `/tmp/story15_4_full_docs.log`.
- Published `docs/_site/fluent-ui-v5-contingency.html` was parsed with Python `HTMLParser`: both package IDs and exact 5.0.1 rows, the dated not-approved decision, all four open gates and the repository receipt link are present. `docs/_site/toc.html` links the page and the normal manifest includes its input path. Generated API inputs are byte-identical to their pre-command backup; no unrelated generated changes were retained.

- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md --base 8c61143ef4c6a983bfe1d25865857b9b4cd240c9` — exit 0, complete eight-path File List reconciles the actual implementation-start working changes and documented concurrent accessibility edits. Log: `/tmp/story15_4_artifacts_scoped.log`. The original baseline is retained; pre-existing Story 15.3-only paths since that older baseline are documented separately below.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md` — exit 0 against the preserved original baseline with the pre-existing Story 15.3 paths classified as unrelated. Log: `/tmp/story15_4_artifacts_baseline.log`.
- `git diff --check` — exit 0. The existing `CommandFormEmitter.cs` LF/CRLF notice remains unrelated; it is not a whitespace-check failure.

### Final parent verification after review patches — 2026-10-23

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0, zero warnings/errors; `/tmp/frontcomposer-15-4-parent-source-build.log`.
- `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` — exit 0, 53 passed, zero errors/failures/skips/unrun cases, including all ten review regressions; `/tmp/frontcomposer-15-4-parent-docs-tests.log`.
- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0, zero warnings/errors; `/tmp/frontcomposer-15-4-parent-cli-build.log`.
- `dotnet tests/Hexalith.FrontComposer.Cli.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests.dll -method '*MigrationPublicationCommand*'` — exit 0, both publication probes passed without errors/failures/skips/unrun cases; `/tmp/frontcomposer-15-4-parent-cli-publication.log`. The unchanged migration behaviors retain the earlier 127-case full-class pass.
- `pwsh ./eng/validate-docs.ps1` — exit 0 on the patched tree; eight compiled snippets, Release solution build and DocFX publication pass with zero blocking failures; `/tmp/frontcomposer-15-4-parent-docs-publication.log`.
- Parent HTMLParser inspection confirms exactly one selected-identity table with both package IDs and two 5.0.1 values, dated not-approved posture, open gates, receipt citation, generated navigation and manifest inclusion.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/frontcomposer-15-4-commit-message.txt --verbose` — exit 0, zero problems/warnings for the exact local commit candidate; successful evidence in `/tmp/frontcomposer-15-4-commitlint.log`.

- `python3 eng/validate-story-artifacts.py` — exit 0; `/tmp/frontcomposer-15-4-parent-artifacts-global.log`.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md` — exit 0 with the preserved baseline and complete eight-file ownership; `/tmp/frontcomposer-15-4-parent-artifacts-story.log`.
- `git diff --check` — exit 0; `/tmp/frontcomposer-15-4-parent-whitespace.log`.

All matrix cases ran and passed. No new work was deferred. The unrelated AppHost baseline failure is recorded above. The workflow marks this spec done while retaining the sprint row at review and Epic 15 in progress.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline, plus a concurrent Story 13.6 ledger append observed during verification; outside Story 15.4 ownership. This story did not edit the ledger.
- `_bmad-output/implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `docs/how-to/migration-guides.md` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `docs/migrations/9.1-to-9.2.md` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `docs/reference/cli.md` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `docs/validation/producer-fingerprints.json` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `src/Hexalith.FrontComposer.Cli/MigrationPlanner.cs` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `src/Hexalith.FrontComposer.Cli/PathUtilities.cs` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.
- `src/Hexalith.FrontComposer.Cli/ProjectDocumentLoader.cs` — pre-existing Story 15.3 hardening merged before implementation HEAD `8c61143e`, after the preserved original baseline; outside Story 15.4 ownership.

- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `src/Hexalith.FrontComposer.AppHost/Program.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcExpandedRowHiddenBannerTests.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/RecordingHiddenDetailFocusModule.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererWrapperIntegrationTests.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ZeroFieldFailureAccessibilityTests.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/helpers/responsive-accessibility.ts` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/scripts/a11y-state-evidence.mjs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/scripts/a11y-state-evidence.test.mjs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/scripts/validate-a11y-artifacts.mjs` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/specs/accessibility-state-matrix.spec.ts` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.
- `tests/e2e/specs/responsive-accessibility.spec.ts` — pre-existing concurrent accessibility work, outside Story 15.4 ownership.

## File List

- `docs/fluent-ui-v5-contingency.md`
- `docs/docfx.json`
- `docs/toc.yml`
- `eng/validate-docs.ps1`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`
- `_bmad-output/implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
