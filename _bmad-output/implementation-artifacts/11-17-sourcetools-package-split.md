---
baseline_commit: 9a870d07ddf8c919e33f272cb3e17eb63dc040b8
---

# Story 11.17b: SourceTools package split

Status: review

<!-- Note: This executable child specializes the non-implementable Story 11.17 parent. The sprint key intentionally remains 11-17-sourcetools-package-split. -->

## Story

As a FrontComposer maintainer,
I want the SourceTools drift implementation split mechanically into one type per file,
so that the package follows the documented source-organization convention without changing analyzer behavior, diagnostics, or generated output.

## Acceptance Criteria

1. Given `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs` currently contains 17 direct top-level declarations and no `DriftDetection` type, when the split is complete, then that aggregate file is deleted and the declarations live in exactly these same-named files under the same `Hexalith.FrontComposer.SourceTools.Drift` namespace: `DriftConstants.cs`, `DriftOptionsResult.cs`, `DriftOptions.cs`, `DriftBaselineInput.cs`, `DriftBaselineLoadResult.cs`, `DriftBaselineSet.cs`, `DriftBaselineContract.cs`, `DriftBaselineProperty.cs`, `DriftBaselineLoader.cs`, `DriftCurrentContract.cs`, `DriftCurrentProperty.cs`, `DriftCurrentSnapshot.cs`, `DriftComparisonResult.cs`, `DriftComparisonService.cs`, `DriftDiagnosticFact.cs`, `DriftSanitizer.cs`, and `DriftDiagnosticDescriptors.cs`. Each file contains exactly one direct top-level declaration whose name matches the file, and no empty compatibility/aggregator file is retained.
2. The split is mechanically identity-preserving: all 17 types retain their namespace, assembly, metadata name, internal top-level accessibility, type kind, modifiers, primary-constructor/generic/interface shape, attributes, constants, member order and bodies, comments/XML documentation, nullable context, directives, and observable exception/stack semantics except for the intentional source-document path change. The resulting inventory remains exactly 13 `internal sealed class` types and 4 `internal static class` types; `DriftBaselineInput` still implements `IEquatable<DriftBaselineInput>` and `DriftComparisonService` remains a sealed instance class. No SourceTools public type is added and no PublicAPI baseline is created.
3. Drift behavior remains unchanged end to end: analyzer-config option names/defaults/ranges/severity, baseline discovery and candidate ordering, BOM/JSON parsing, trust and bounds checks, duplicate handling, fail-closed behavior, sanitization/redaction/path normalization, current snapshot construction, structural-versus-metadata classification, ordinal ordering and truncation, hashes/fingerprints, diagnostic locations/property bags/message arguments, and the complete HFC1058-HFC1070 descriptor catalog and AnalyzerReleases parity all remain identical.
4. The incremental-generator seam remains unchanged. The drift lane still consumes projections, commands, AdditionalText baselines, and options without combining `CompilationProvider`; only the already-isolated trim/AOT advisory may consume compilation. `LoadDriftBaselines` tracking/cache behavior, gate-off behavior, diagnostic ordering, generated source set/order, and every generated-source byte remain stable. `FrontComposerGenerator.cs` and existing verified snapshots are not changed unless a failing preservation test proves the story cannot be completed mechanically and the discrepancy is escalated.
5. A durable, non-vacuous Governance guard enforces one direct top-level type/delegate per production file within `src/Hexalith.FrontComposer.SourceTools/Drift`, excludes only build/generated paths, requires declaration/file-name parity, catches a synthetic multi-kind two-declaration sample, and reflection-pins the exact 17 internal top-level type identities. The guard has no allowlist. Its Drift-only scope is deliberate because unrelated approved SourceTools multi-type debt belongs outside Story 11.17b.
6. The SourceTools drift, HFC catalog, generated-byte, P12 incremental-cache, packaged-analyzer consumer, default, and Governance lanes pass with warnings treated as errors. The final evidence includes a declaration-body comparison against the implementation baseline after newline normalization, an exact changed/untracked path ledger, CRLF/UTF-8/final-newline checks for new C# files, received/generated-artifact and submodule audits, and `git diff --check`. Finding M14 is recorded as addressed by this child, with no new `NoWarn`, pragma suppression, dependency/version, project, solution, package-policy, or API-baseline change.

## Tasks / Subtasks

- [x] Task 1: Capture the implementation baseline and prove the mechanical inventory (AC: 1, 2, 6)
  - [x] Record the implementation-start commit and the complete pre-existing dirty-worktree ledger before editing; preserve the active Story 11.17a CLI review changes, deferred-work history, sprint history, and root gitlink advances without absorbing, reverting, or relabeling them.
  - [x] Parse `DriftDetection.cs` with Roslyn and pin the current direct declaration inventory: 17 internal top-level types, 13 sealed classes plus 4 static classes, with the exact fully qualified names from AC1.
  - [x] Capture each declaration block from the baseline so review can compare extracted declaration bodies after newline normalization; do not use a working-tree copy as the reference after edits begin.

- [x] Task 2: Perform the SourceTools Drift split mechanically (AC: 1, 2)
  - [x] Move each declaration into the same-named file listed in AC1, preserve file-scoped namespace semantics and declaration text, and keep any declaration-owned nested types with their owner.
  - [x] Give each file only the imports it actually requires; validate the likely per-file import map under warnings-as-errors instead of copying the aggregate using block or introducing global usings.
  - [x] Delete `DriftDetection.cs` after all 17 declarations are accounted for. Do not add a `DriftDetection` facade or edit the project file: SDK default compile items already include the new `.cs` files.
  - [x] Preserve CRLF, UTF-8, final newlines, K&R formatting, nullable behavior, directives, comments, and declaration/member order; do not run a package-wide formatter or opportunistic cleanup.

- [x] Task 3: Add a durable SourceTools organization guard (AC: 2, 5)
  - [x] Add `tests/Hexalith.FrontComposer.SourceTools.Tests/Architecture/SourceToolsTypeOrganizationGovernanceTests.cs` using the existing Roslyn test dependency.
  - [x] Tag the guard with `[Trait("Category", "Governance")]` so the repository Governance lane executes it; keep the direct class invocation below as the focused proof.
  - [x] Scope the production-source scan to the `SourceTools/Drift` directory; count direct compilation-unit/file-scoped/block-namespace type and delegate declarations, while leaving nested declaration identity untouched and excluding only `bin`, `obj`, and generated output.
  - [x] Require at most one direct declaration per file and exact file-name/declaration-name parity, with no allowlist and no dependency on the deleted aggregate filename.
  - [x] Add a synthetic source containing two different direct declaration kinds so the guard proves it fails non-vacuously when the repository locator or syntax walk regresses.
  - [x] Reflection-pin the exact 17 `Hexalith.FrontComposer.SourceTools.Drift.*` types as direct, non-public types and retain the existing `DriftSeamPublicSurfaceContractTests` assertions.

- [x] Task 4: Preserve Drift/HFC/generated-output contracts (AC: 3, 4, 6)
  - [x] Run the complete `Hexalith.FrontComposer.SourceTools.Tests.Drift*` namespace, including baseline, comparison, diagnostics, regression/byte-stability, incremental cache, seam, culture, and trim/AOT tests.
  - [x] Run both `DriftDiagnosticCatalogTests` and the repository-level `DiagnosticCatalogTests`; prove HFC1058-HFC1070 IDs, categories, severities, messages, properties, help links, order, and release-file parity are unchanged.
  - [x] Prove generated source hints, ordering, and bytes are unchanged with the existing byte-stability/snapshot tests; no `.verified.*`, `AnalyzerReleases.*`, or generated file update is expected.
  - [x] Preserve the P12 pipeline boundary in `FrontComposerGenerator.cs`: the drift comparison lane must remain free of `CompilationProvider`, and the existing isolated trim/AOT lane must remain the only permitted use.
  - [x] Run `PackagedAnalyzerConsumerTests` to confirm the analyzer package still presents the same SourceTools/Contracts assets and clean-consumer behavior. Do not require `.nupkg`, PDB, or stack-trace byte identity because intentional source-document paths change.

- [x] Task 5: Reconcile current-tree documentation without rewriting history (AC: 1, 3, 6)
  - [x] Retarget the current-code comment in `DriftByteStabilityRegressionTests.cs` from the deleted aggregate to the owning `DriftBaselineLoader.cs`; do not change the test logic or expected bytes.
  - [x] Update the maintained SourceTools topology in `_bmad-output/project-docs/source-tree-analysis.md` from the aggregate `DriftDetection.cs` entry to the decomposed Drift files.
  - [x] Leave planning artifacts, completed Story 7.4, `_bmad-output/implementation-artifacts/deferred-work.md`, historical sprint comments, and review provenance untouched: their old path references are audit evidence, not current-tree instructions.

- [x] Task 6: Execute the release-aligned gates and reconcile evidence (AC: 3, 4, 5, 6)
  - [x] Restore/build the Release solution and SourceTools test project serialized with warnings as errors, then run the focused direct xUnit v3 commands and the solution default/Governance lanes below.
  - [x] Compare all 17 extracted declaration bodies with the captured baseline after newline normalization and document any import-only differences; any member-body, signature, modifier, attribute, directive, or diagnostic change blocks completion.
  - [x] Reconcile the exact union of tracked changes and untracked files against the story File List. Audit submodule gitlinks, received/generated artifacts, CRLF/final newlines, and `git diff --check`; do not include unrelated workspace changes in the story ledger.
  - [x] Record finding `M14` in the Change Log as closed for the approved SourceTools `DriftDetection.cs` slice only; do not claim closure for other Story 11.17 children or other known multi-type files.

## Dev Notes

### Scope and Current-State Decisions

- This file is **Story 11.17b**, the executable SourceTools child. The `11-17-sourcetools-package-split` sprint/file key omits the letter by repository convention; `11-17-cli-package-split` is the separate Story 11.17a sibling already in progress. The Story 11.17 parent is a decomposition container and must not be implemented directly.
- “Package split” here means a source-file slice inside the existing `Hexalith.FrontComposer.SourceTools` analyzer project. It does **not** create or split a NuGet package, assembly, namespace, solution project, or dependency boundary.
- The approved production delta is one deletion plus 17 additions under `src/Hexalith.FrontComposer.SourceTools/Drift`. No production UPDATE file is expected. The aggregate contains no eponymous type, so retaining an empty `DriftDetection.cs` would create misleading debt rather than compatibility.
- `DriftDetection.cs` currently owns option parsing, baseline inputs/loading, baseline and current models, comparison/classification, diagnostic facts, sanitization, and descriptors. `FrontComposerGenerator.cs` consumes those types but needs no wiring change.
- The likely import split is:
  - no explicit import beyond current implicit usings for `DriftConstants`, `DriftBaselineProperty`, `DriftCurrentProperty`, and `DriftSanitizer`;
  - `System.Collections.Immutable` for the small immutable option/result/baseline/current/comparison models;
  - Roslyn diagnostics/options imports for `DriftOptions`, Roslyn text imports for `DriftBaselineInput`, JSON/text/globalization imports for `DriftBaselineLoader`, Parsing for `DriftCurrentSnapshot`, Roslyn/globalization for `DriftComparisonService`, cryptography/text/Roslyn imports for `DriftDiagnosticFact`, and SourceTools diagnostics/Roslyn imports for `DriftDiagnosticDescriptors`.
  Treat this as a review map, not license to alter implementation; compile with TWAE and remove stale imports per file.
- The new guard is intentionally Drift-scoped. Package-wide enforcement would fail unrelated existing bundles such as `Parsing/DomainModel.cs`, `ProjectionTemplateMarkerInfo.cs`, `CustomizationHotReloadClassifier.cs`, `Transforms/FormFieldModel.cs`, `McpManifestTransform.cs`, and `SchemaFingerprintTransform.cs`, silently expanding this child beyond approved scope.

### Architecture Compliance and Never-List

- Preserve SourceTools as a `netstandard2.0` Roslyn component referencing only Contracts. Do not edit `Hexalith.FrontComposer.SourceTools.csproj`, the test project, `Hexalith.FrontComposer.slnx`, `Directory.Packages.props`, `global.json`, package inventory/validation policy, analyzer package layout, or project references.
- Preserve value-equatable incremental models, no `ISymbol`/syntax leakage after parse, immutable/equatable collection semantics, and diagnostics-as-data until the registered output seam. This mechanical split must not address the separate M22 equality/performance concern.
- Preserve baseline trust, canonical schema material, ordinal comparisons, hashes, caps, sanitization, redaction, and fail-closed behavior. Do not change HFC rules, AnalyzerReleases metadata, snapshots, generator hints, documentation URLs, or trim/AOT behavior.
- Do not create a SourceTools PublicAPI baseline: all 17 moved types are internal, and existing seam/reflection/package-consumer tests are the truthful compatibility evidence.
- Do not touch other Story 11.17 children (CLI, MCP/runtime/benchmarks, Shell), Story 11.18 LoggerMessage work, Story 11.19 policy enforcement, Contracts.UI/package-boundary work, unrelated SourceTools multi-type files, M22, M23, or deferred Drift behavior/performance findings.
- Do not rewrite `_bmad-output/implementation-artifacts/deferred-work.md` or historical artifacts merely because they record the former discovery path. Do not edit generated files, submodules, package versions, target frameworks, compatibility suppressions, release baselines, or public APIs.
- UX impact is **N/A**: no Razor, CSS, text, layout, accessibility, responsive, command-line, or adopter interaction change is intended.

### Library and Framework Requirements

- Use the repository-pinned toolchain as authoritative: .NET SDK `10.0.301`, C# latest/14, Roslyn `5.6.0`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. No new package or version update is required.
- The .NET SDK implicitly includes `.cs` compile items by default, so the split does not require explicit project entries. [MSBuild properties for Microsoft.NET.Sdk](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#default-item-inclusion-properties)
- Keep one file-scoped namespace per new file; Microsoft documents that the declaration applies to the entire file and is the normal single-namespace form. [Namespaces and using directives](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces#file-scoped-namespaces)
- Roslyn's incremental-generator guidance treats value equality and removal of symbols/syntax from pipeline models as critical to cache reuse; the split must preserve those model/equality semantics. [Incremental generators cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#pipeline-model-design)
- `RegisterSourceOutput` runs when its incremental input changes and can add sources or report diagnostics. Keep the existing providers and output registration intact. [RegisterSourceOutput API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.incrementalgeneratorinitializationcontext.registersourceoutput?view=roslyn-dotnet-5.0.0)

### Testing Requirements

Use a Release-aligned restore because Release builds exercise package boundaries and exclude the Debug-only AppHost. Build serially and run focused xUnit v3 filters through the built executable; wildcards are supported by the repository-pinned runner. FrontComposer's broad gates remain solution-level trait-filtered runs.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests \
  -class Hexalith.FrontComposer.SourceTools.Tests.Architecture.SourceToolsTypeOrganizationGovernanceTests \
  -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests \
  -namespace 'Hexalith.FrontComposer.SourceTools.Tests.Drift*' \
  -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests \
  -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticCatalogTests \
  -class Hexalith.FrontComposer.SourceTools.Tests.Integration.PackagedAnalyzerConsumerTests \
  -parallel none

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --filter "Category=Governance"

BASELINE=<implementation-start-commit>
git diff --name-status "$BASELINE"
git ls-files --others --exclude-standard
git diff --submodule=short "$BASELINE" -- references
rg --files -g '*.received.*' -g '!references/**' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

If solution-level test transport is locally blocked, record the exact blocker separately, retain direct-executable SourceTools evidence, and identify the authoritative CI lane. Do not convert an environmental blocker into an implementation failure or omit the required lane silently. Test totals are comparison evidence, not hard-coded ACs; the new guard legitimately raises totals.

### Previous-Story Intelligence

- Story 11.16 proved that a mechanical SourceTools refactor still needs generated parse/compile/runtime and byte-stability evidence. Its review caught a lost null/empty short-circuit, so declaration-body comparison is mandatory rather than relying on a green build.
- Its non-vacuous guard pattern combines a repository/source scan with a synthetic forbidden example. A repository scan alone can pass if path discovery is broken.
- Its review also found an incomplete changed-file ledger when root gitlinks moved. This child must reconcile tracked, untracked, and submodule paths separately and keep unrelated gitlinks out of its File List.
- The direct Story 11.17a precedent uses Roslyn to count direct declarations, preserves nested types, reflection-pins exact internal identities, and checks declaration bodies after newline normalization. Its later review found a redundant per-file using, reinforcing the smallest-correct-import requirement here.

### Git Intelligence

- At story creation, `main` and `origin/main` are `9a870d07ddf8c919e33f272cb3e17eb63dc040b8` (`chore(release): 3.1.0 [skip ci]`). Recalculate the baseline and inventory when implementation begins.
- `7f53cf3f` is the strongest direct precedent: it implemented the Story 11.17a CLI mechanical split plus a non-vacuous organization guard. `db9ba9ee` implemented Story 11.16; `23a59ad9` review-hardened its behavior preservation; `ff166ac2` is the earlier package/type-ownership split precedent.
- The creation workspace contains active unrelated changes in the CLI 11.17a story/review, deferred-work and sprint artifacts, `MigrationApplier.cs`, and three root gitlinks. Preserve them. SourceTools production/test paths were clean at creation time.
- Follow repository Git instructions: root-declared submodules only, never initialize nested submodules recursively, keep root gitlinks explicit, and use Conventional Commits for implementation commits.

### Project Structure Notes

- Expected production DELETE: `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs`.
- Expected production ADD: the 17 same-named Drift files enumerated in AC1. Ordinary SDK compile globs include them; no `.csproj` or `.slnx` edit is expected.
- Expected test ADD: `tests/Hexalith.FrontComposer.SourceTools.Tests/Architecture/SourceToolsTypeOrganizationGovernanceTests.cs`.
- Expected narrow UPDATE files: `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftByteStabilityRegressionTests.cs` (path comment only) and `_bmad-output/project-docs/source-tree-analysis.md` (maintained topology only).
- Existing `DriftSeamPublicSurfaceContractTests`, `DriftByteStabilityRegressionTests`, `DriftIncrementalCacheTests`, `DriftDiagnosticCatalogTests`, `DiagnosticCatalogTests`, and `PackagedAnalyzerConsumerTests` are preservation gates. Do not rewrite them merely to make the split pass.
- Creation-time changes are limited to this story file and the surgical sprint-status transition. Implementation must replace the File List below with the exact final story-owned ledger before review.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1117-mechanical-one-type-per-file-split]
- [Source: _bmad-output/planning-artifacts/prd.md#Functional-Requirements] (canonical FR-6, FR-25, FR-29; Epic 7's older “FR7” label is legacy traceability)
- [Source: _bmad-output/planning-artifacts/architecture.md#Core-Architectural-Decisions]
- [Source: _bmad-output/project-docs/architecture.md#SourceTools-Incremental-Generator]
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#Moderate-Findings] (M14)
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-readiness-major-issues.md#Proposal-G]
- [Source: _bmad-output/project-context.md#Source-Generator-Rules]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md#Coding-Instructions]
- [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs]
- [Source: _bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md]
- [Source: _bmad-output/implementation-artifacts/11-17-cli-package-split.md]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Implementation Plan

- Capture the immutable implementation baseline and unrelated dirty-worktree ledger before edits.
- Split the 17 Roslyn declarations mechanically, preserving normalized declaration bodies while minimizing imports per file.
- Add a Drift-scoped non-vacuous organization and exact internal-identity Governance guard.
- Retarget only current-tree documentation, then execute focused, default, Governance, packaging, artifact, and mechanical integrity gates.

### Debug Log References

- 2026-07-14 Task 1: baseline `9a870d07ddf8c919e33f272cb3e17eb63dc040b8`; branch `main` matched `origin/main` at capture.
- 2026-07-14 Task 1 unrelated pre-existing ledger: modified `11-17-cli-package-split.md`, `deferred-work.md`, `sprint-status.yaml`, `MigrationApplier.cs`; advanced root gitlinks `references/Hexalith.Builds`, `references/Hexalith.Memories`, `references/Hexalith.Tenants`; untracked `11-17-sourcetools-package-split.md`.
- 2026-07-14 Task 1 Roslyn 5.6 baseline proof: desired one-declaration condition failed as expected; inventory then passed at exactly 17 direct internal classes (13 sealed, 4 static). Normalized declaration SHA-256 values were captured for all AC1 identities from `git show 9a870d07:.../DriftDetection.cs` for final comparison.
- 2026-07-14 Task 2: split all 17 declarations mechanically, deleted the aggregate, and minimized imports. Release SourceTools build passed with 0 warnings/0 errors; Release SourceTools.Tests build passed with 0 warnings/0 errors; complete Drift namespace passed 170/170.
- 2026-07-14 Task 2 body gate: Roslyn comparison reported `MATCH` for all 17 normalized declaration bodies and exactly 17 working files with no aggregate. Differences are import-only plus the intentional source-document path changes.
- 2026-07-14 Task 3 red/green: the focused placeholder failed 1/1 as intended; the implemented Governance guard then passed 4/4 and the complete SourceTools test assembly passed 1094/1094.
- 2026-07-14 Task 4: complete Drift namespace passed 170/170; repository diagnostic catalog plus packaged-analyzer consumer tests passed 5/5. No verified snapshot, AnalyzerReleases, generated file, or `FrontComposerGenerator.cs` change exists; the only `CompilationProvider` use remains the isolated trim/AOT advisory.
- 2026-07-14 Task 5: retargeted the BOM regression comment and expanded maintained SourceTools topology only. Byte-stability class passed 11/11; docs validation passed and produced `artifacts/docs/validation-manifest.json` as ignored build evidence.
- 2026-07-14 Task 6 Release gates: solution and SourceTools.Tests builds passed with 0 warnings/0 errors; focused guard 4/4, Drift 170/170, catalog/package 5/5; solution default lane passed 4109/4109 across seven test assemblies; Governance passed 286/286 across CLI, SourceTools, and Shell.
- 2026-07-14 Task 6 mechanical gates: all 17 Roslyn-normalized declaration bodies matched baseline with import-only differences; File List matched all 23 story-owned paths exactly; all 18 new C# files passed UTF-8/no-BOM/CRLF/final-newline checks; no received, generated, verified, AnalyzerReleases, project, solution, package-policy, public-API, `NoWarn`, pragma, or `Debugger.Launch` change; `git diff --check` and story-artifact validation passed. The three baseline-captured unrelated gitlink advances remained untouched; a fourth clean concurrent advance of `references/Hexalith.PolymorphicSerializations` to `96f53f30` appeared after the gates and was audited and excluded from this story ledger.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Task 1 complete: captured the immutable baseline, exact unrelated workspace ledger, Roslyn declaration identities/modifiers, and normalized declaration-body hashes before production edits.
- Task 2 complete: decomposed `DriftDetection.cs` into the exact 17 same-named internal type files with smallest verified imports and no declaration-body, modifier, directive, comment, or member-order changes.
- Task 3 complete: added a Drift-scoped Governance guard with repository scanning, exact file/declaration parity, a synthetic class-plus-delegate negative, exact internal identity/modifier pins, and a no-public-Drift-surface assertion.
- Task 4 complete: preserved all Drift/HFC1058-HFC1070, generated-byte, incremental-cache/P12, seam, trim/AOT, catalog, AnalyzerReleases, and packaged-analyzer consumer contracts.
- Task 5 complete: reconciled only maintained current-tree references; historical story, planning, sprint, deferred-work, and review provenance remain untouched.
- Task 6 complete: all release-aligned, focused, default, Governance, declaration-identity, file-ledger, encoding, artifact, submodule, policy, and whitespace gates passed.
- Story 11.17b complete and ready for review: 17 internal Drift declarations now live in exact same-named files, the aggregate is removed, a four-test Governance/identity guard prevents regression, maintained topology is current, and all acceptance/preservation gates are green.

### File List

- `_bmad-output/implementation-artifacts/11-17-sourcetools-package-split.md` (ADD — implementation record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATE — 11.17b status and history)
- `_bmad-output/project-docs/source-tree-analysis.md` (UPDATE — decomposed SourceTools Drift topology)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineContract.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineInput.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineLoader.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineLoadResult.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineProperty.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftBaselineSet.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftComparisonResult.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftComparisonService.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftConstants.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftCurrentContract.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftCurrentProperty.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftCurrentSnapshot.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs` (DELETE — former 17-declaration aggregate)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDiagnosticDescriptors.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDiagnosticFact.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftOptions.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftOptionsResult.cs` (ADD — mechanically split declaration)
- `src/Hexalith.FrontComposer.SourceTools/Drift/DriftSanitizer.cs` (ADD — mechanically split declaration)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Architecture/SourceToolsTypeOrganizationGovernanceTests.cs` (ADD — Drift organization and identity Governance guard)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftByteStabilityRegressionTests.cs` (UPDATE — current owner path comment only)

## Change Log

- 2026-07-14: Created executable Story 11.17b and marked ready-for-dev. Scoped the mechanical SourceTools `DriftDetection.cs` split, durable Drift organization guard, drift/HFC/generated-byte/P12 preservation lanes, and M14 closure evidence.
- 2026-07-14: Implemented and validated the SourceTools Drift split. Closed architecture-quality finding M14 for the approved `DriftDetection.cs` slice only; other Story 11.17 children and known unrelated multi-type files remain outside this closure.
