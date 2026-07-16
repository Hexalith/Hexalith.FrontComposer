---
baseline_commit: 3515474f50cb2bede78c8a7c51ef517afeba511e
---

# Story 7.4: Opt-in drift detection vs. a baseline

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-05. -->

## Story

As an adopter developer,
I want to detect structural and metadata drift against a checked-in baseline,
so that unintended generated-surface changes are caught before release.

## Acceptance Criteria

1. Given `HfcDriftDetectionEnabled=true` or alias `FrontComposerDriftDetectionEnabled=true` and a trusted baseline `AdditionalText` named `frontcomposer.drift-baseline*.json` or `frontcomposer.generated-ui-baseline*.json`, when the generated projection or command surface changes structurally, then SourceTools reports HFC1065 at the configured drift severity and includes sanitized declaration, member, surface, path, shape-hash, schema, algorithm, and docs-link data. [Source: _bmad-output/planning-artifacts/epics.md#Story-7.4-Opt-in-drift-detection-vs-a-baseline; src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierProjectionPropertyTests.cs]
2. Given the same opt-in and trusted baseline, when display, role, icon, policy, badge, format, field-group, destructive, empty-state CTA, or other renderer/metadata-impacting surface changes, then SourceTools reports HFC1066 at the configured drift severity, caps duplicate reporting to the intended declaration/member/category granularity, and emits no drift diagnostics when current source matches the baseline exactly. [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs; _bmad-output/project-docs/api-contracts.md#1.4-MSBuild-analyzer-config-options-drift-detection]
3. Given drift detection is not opted in, when candidate baseline `AdditionalText` files are present, then no HFC1058-HFC1069 drift diagnostics are emitted and generated output remains byte-identical to the opt-in path. Drift detection must be diagnostics-only and must not rewrite generated code. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftByteStabilityRegressionTests.cs]
4. Given drift detection is opted in and the baseline is absent, configured to a non-matching `HfcDriftBaselinePath`, empty, malformed, unsupported, oversized, duplicated, invariant-violating, or redaction-unsafe, then SourceTools reports the cataloged HFC1058-HFC1064 or HFC1069 diagnostic, suppresses unsafe partial drift comparison, and never leaks absolute paths, secrets, tenant/user data, raw payloads, or untrusted baseline values. [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineMissingDiagnosticTests.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticRedactionTests.cs]
5. Given invalid drift analyzer-config options, diagnostic caps, multiple baselines, changed baseline content, or unrelated source edits, then SourceTools reports HFC1067/HFC1068 where applicable, preserves deterministic ordering, keeps `LoadDriftBaselines` isolated in the incremental graph, and the comparison pipeline remains independent of `CompilationProvider`. The separate HFC1070 trim/AOT advisory may use `CompilationProvider` but must stay isolated from drift comparison. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticOrderingAndTruncationTests.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Incremental/DriftIncrementalCacheTests.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs]
6. Given the drift diagnostics are part of the SourceTools catalog, when Story 7.4 is complete, then it produces `_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md` documenting the v1 baseline schema, MSBuild options, trusted input rules, HFC1058-HFC1069/HFC1070 dispositions, diagnostic payload contract, performance/cache constraints, and focused verification evidence. Historical `Story 9-1` labels in code/docs/tests are either reconciled where owned by this story or explicitly recorded as pre-existing labeling debt without changing behavior. [Source: docs/diagnostics/diagnostic-registry.json; docs/diagnostics/README.md; src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md; _bmad-output/implementation-artifacts/7-3-surface-the-hfc-diagnostic-catalog.md]

## Tasks / Subtasks

- [x] Audit the existing drift implementation before changing code (AC: 1, 2, 3, 4, 5, 6)
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` drift registrations and confirm comparison is gated by `HfcDriftDetectionEnabled` / `FrontComposerDriftDetectionEnabled`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs` end to end, including options binding, candidate baseline filtering, baseline load/trust checks, current snapshot generation, comparison, diagnostic facts, sanitization, truncation, and HFC1070 helpers.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`, and `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` for HFC1058-HFC1070.
  - [x] Read the complete `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**` suite and identify any stale Story 9-1 labels, unused red-phase constants, or unowned gaps before editing.
- [x] Confirm and pin structural and metadata drift behavior (AC: 1, 2)
  - [x] Reuse or extend `DriftClassifierProjectionPropertyTests`, `DriftClassifierTypeAndNullabilityTests`, `DriftClassifierBoundedContextTests`, `DriftClassifierRenameTests`, and `DriftClassifierMetadataTests`; do not create a parallel classifier.
  - [x] Ensure structural drift uses HFC1065 for declaration/property add/remove/rename, type-category changes, nullability changes, and bounded-context identity changes.
  - [x] Ensure metadata drift uses HFC1066 for Display/Description, ProjectionRole, Icon, RequiresPolicy, Destructive, ColumnPriority, ProjectionFieldGroup, RelativeTime/Currency, ProjectionBadge, and ProjectionEmptyStateCta changes.
  - [x] Ensure `HfcDriftSeverity=Warning|Error|Info|Information` affects only HFC1065/HFC1066 drift diagnostics, not trust-failure Error diagnostics.
- [x] Confirm and pin opt-in, baseline naming, and diagnostics-only behavior (AC: 3, 5)
  - [x] Verify no HFC1058-HFC1069 diagnostics fire when drift detection is disabled, even if candidate AdditionalTexts are present.
  - [x] Verify only `frontcomposer.drift-baseline*.json` and `frontcomposer.generated-ui-baseline*.json` files are considered candidate baselines.
  - [x] Verify generated output bytes and hint-name sets do not change with drift enabled, disabled, baseline file ordering, UTF-8 BOM, CRLF/LF baseline text, or repeated generator runs.
  - [x] Preserve `LoadDriftBaselines` as the tracked incremental step name unless every test/contract reference is intentionally updated.
- [x] Confirm and pin baseline trust failures and redaction (AC: 4)
  - [x] Keep HFC1058 distinct from HFC1059: missing/first-run baseline is Warning; configured path with no matching AdditionalText is Error.
  - [x] Keep HFC1060-HFC1064 fail-closed for empty/malformed JSON, unsupported schema, unsupported algorithm, byte/count bounds, duplicate identities, duplicate properties, and invariant violations.
  - [x] Keep HFC1069 fail-closed for redaction-unsafe values and suppress the would-have-leaked HFC1065/HFC1066 diagnostics for affected contracts.
  - [x] Preserve path normalization to repo-relative forward slashes or `<outside-project>`; no Windows drive roots, absolute host paths, URI paths, control characters, tenant/user/token/payload sentinels, or raw baseline text in messages/properties.
- [x] Confirm and pin diagnostic contract, ordering, truncation, and catalog governance (AC: 4, 5, 6)
  - [x] Verify every drift diagnostic exposes populated `BaselinePath`, `DeclarationPath`, `DeclarationName`, `MemberName`, `DriftKind`, `ExpectedShapeHash`, `ActualShapeHash`, `SchemaVersion`, `AlgorithmVersion`, and `BoundedContext` properties.
  - [x] Verify diagnostic messages retain the `What`, `Expected`, `Got`, `Fix`, and `DocsLink` shape and help links use the canonical diagnostics URL.
  - [x] Verify HFC1068 truncation stays inside `HfcDriftMaxDiagnostics`, has deterministic order, and records omitted count.
  - [x] Verify docs registry rows, docs stubs, release rows, descriptors, and `FcDiagnosticIds` constants remain aligned for HFC1058-HFC1070.
- [x] Preserve architecture boundaries and avoid scope creep (AC: 3, 5, 6)
  - [x] Do not change `CanonicalSchemaMaterial`, schema fingerprint algorithms, schema baseline provenance regex, MCP schema negotiation, generated output path contracts, package versions, or public API baselines unless a focused failing test proves Story 7.4 owns the change.
  - [x] Do not add a baseline generation/update CLI, code fix, public `Drift*` type, or `System.CommandLine`; Story 7.4 is build-time opt-in detection, not a baseline-authoring workflow.
  - [x] Keep trim/AOT HFC1070 as a separate `RegisterSourceOutput` path using compilation only for `IActionQueueProjectionCatalog` evidence. Do not let `CompilationProvider` flow into the HFC1058-HFC1069 comparison pipeline.
- [x] Produce the Story 7.4 contract artifact (AC: 6)
  - [x] Create `_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md`.
  - [x] Record baseline schema version `frontcomposer.generated-ui-baseline.v1`, algorithm `frontcomposer-structural-v1`, accepted file-name prefixes, MSBuild options, default bounds, diagnostic severity rules, and trust-failure precedence.
  - [x] Record historical Story 9-1 labels as brownfield provenance; update only owned references that would confuse Epic 7 docs/tests, and leave unrelated Story 9-x diagnostic-site ownership intact.
  - [x] Record verification commands and exact pass/failure counts.
- [x] Verify and record evidence (AC: 1, 2, 3, 4, 5, 6)
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run focused SourceTools drift tests via direct xUnit v3 in-process runner for `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**`.
  - [x] Run focused diagnostic registry tests covering HFC1058-HFC1070 parity.
  - [x] Run the broader SourceTools test assembly or direct xUnit v3 fallback and account for every failure by name.
  - [x] Create or update `_bmad-output/implementation-artifacts/tests/test-summary.md` with the Story 7.4 result.
  - [x] Reconcile the File List against `git status --short` before moving to review.

## Dev Notes

- Brownfield reality: the drift detector already exists and is wired into `FrontComposerGenerator`. The likely Story 7.4 implementation shape is confirm-and-pin plus contract/documentation reconciliation, with narrowly scoped fixes only where tests prove gaps. Do not rebuild the drift pipeline from scratch. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs; src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs]
- Current generator wiring collects projections, commands, candidate drift baseline AdditionalTexts, and analyzer-config drift options; reports option diagnostics; returns early unless drift is enabled; loads/trust-validates baselines; builds `DriftCurrentSnapshot`; compares with `DriftComparisonService`; then reports diagnostics. This comparison path deliberately does not combine `CompilationProvider`. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- HFC1070 is adjacent but separate: trim/AOT advisory runs from a distinct `RegisterSourceOutput` that combines `CompilationProvider` to detect adopter `IActionQueueProjectionCatalog` evidence. Preserve this separation; the P12 invariant applies to the HFC1058-HFC1069 drift comparison pipeline. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs]
- `DriftDetection.cs` currently defines schema version `frontcomposer.generated-ui-baseline.v1`, algorithm `frontcomposer-structural-v1`, default max diagnostics `50`, default max baseline bytes `256 KiB`, max declarations `512`, and max properties per declaration `256`. Accepted baselines are `frontcomposer.drift-baseline*.json` or `frontcomposer.generated-ui-baseline*.json`. [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs]
- Analyzer-config options are `HfcDriftDetectionEnabled`, alias `FrontComposerDriftDetectionEnabled`, `HfcDriftBaselinePath`, `HfcDriftMaxDiagnostics` `1..500`, `HfcDriftMaxBaselineBytes` `1..10 MiB`, and `HfcDriftSeverity` `Warning|Error|Info|Information`. Invalid numeric/severity options produce HFC1067 and fall back to documented safe defaults. [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs; _bmad-output/project-docs/api-contracts.md#1.4-MSBuild-analyzer-config-options-drift-detection]
- Baseline trust failures suppress comparison. Missing baseline is HFC1058 Warning; configured path mismatch is HFC1059 Error; empty/malformed JSON is HFC1060 Error; unsupported schema is HFC1061 Error; unsupported algorithm is HFC1062 Error; bounds are HFC1063 Error; duplicate/invariant failures are HFC1064 Error; redaction suppression is HFC1069 Error. [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs; _bmad-output/project-docs/api-contracts.md#1.5-Diagnostic-catalog-HFC1001-HFC1070]
- Drift diagnostics use HFC1065 for structural drift and HFC1066 for metadata drift. The emitted diagnostic property bag includes baseline/declaration paths, names, drift kind, expected/actual shape hashes, schema version, algorithm version, and bounded context; messages must keep the actionable `What/Expected/Got/Fix/DocsLink` shape. [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs; tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticContractTests.cs]
- The drift test suite is extensive and active. Many files still contain unused `SkipReason = "RED-PHASE..."` constants and Story 9-1 comments, but the attributes are normal `[Fact]` / `[Theory]` without `Skip`. Treat those labels as brownfield provenance unless the story intentionally reconciles comments/docs. [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**]
- Current docs registry and generated diagnostic stubs still list ownerStory `9-1-build-time-drift-detection` for HFC1058-HFC1070. Story 7.4 must not casually rewrite the whole diagnostics site; update only if the contract artifact or governance tests require Epic 7 ownership reconciliation. Story 7.3 already established that diagnostic catalog governance belongs to `docs/diagnostics/diagnostic-registry.json` and `DiagnosticRegistryTests`. [Source: docs/diagnostics/diagnostic-registry.json; docs/diagnostics/README.md; _bmad-output/implementation-artifacts/7-3-surface-the-hfc-diagnostic-catalog.md]
- Do not change `CanonicalSchemaMaterial`, `SchemaFingerprintAlgorithm`, `SchemaBaselineProvenance.SafeIdentifier`, `SchemaMigrationDeltaAnalyzer`, MCP schema negotiation, or stored fingerprint algorithms for this story. Those surfaces are related compatibility infrastructure, not the SourceTools generated-UI drift baseline pipeline. [Source: _bmad-output/project-context.md#Schema-Fingerprint-Integrity-Rules; src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs; src/Hexalith.FrontComposer.Contracts/Schema/SchemaBaselineContracts.cs; src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs]
- No external dependency research is needed. Relevant versions are pinned locally: .NET SDK `10.0.302`, Roslyn `5.3.0`, System.Text.Json `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`. Do not change package versions. [Source: _bmad-output/project-context.md#Technology-Stack-and-Versions; Directory.Packages.props]

### Project Structure Notes

- Expected production touch points:
  - `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` only for narrow pipeline fixes or comments if tests prove drift wiring gaps.
  - `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs` for narrow option/load/compare/diagnostic/sanitization fixes.
  - `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`, `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, `docs/diagnostics/diagnostic-registry.json`, and `docs/diagnostics/HFC1058.md` through `docs/diagnostics/HFC1070.md` only if governance drift is proven.
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Incremental/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` and `DiagnosticDescriptorTests.cs`
- Expected BMAD artifacts:
  - `_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md`
- Detected unrelated dirty file before Story 7.4 creation: `_bmad-output/story-automator/orchestration-1-20260604-140358.md`. Do not revert it or include it in the Story 7.4 File List unless the dev agent intentionally changes it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-7.4-Opt-in-drift-detection-vs-a-baseline]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad-output/project-docs/architecture.md#3-The-generation-pipeline-Layer-1-detail]
- [Source: _bmad-output/project-docs/api-contracts.md#1.4-MSBuild-analyzer-config-options-drift-detection]
- [Source: _bmad-output/project-docs/api-contracts.md#1.5-Diagnostic-catalog-HFC1001-HFC1070]
- [Source: _bmad-output/project-docs/data-models.md#5-Schema-fingerprinting-baselines-deltas-the-compatibility-model]
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs]
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs]
- [Source: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md]
- [Source: docs/diagnostics/README.md]
- [Source: docs/diagnostics/diagnostic-registry.json]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift]
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs]
- [Source: _bmad-output/implementation-artifacts/7-3-surface-the-hfc-diagnostic-catalog.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Resolved BMAD dev-story workflow customization: no activation prepend/append steps; persistent project-context files loaded.
- Confirmed `baseline_commit` already matched `HEAD` (`3515474f50cb2bede78c8a7c51ef517afeba511e`), so it was preserved.
- Audited `FrontComposerGenerator` drift wiring: `LoadDriftBaselines` is a tracked `AdditionalTextsProvider` step, HFC1058-HFC1069 comparison is gated by `HfcDriftDetectionEnabled` / `FrontComposerDriftDetectionEnabled`, and HFC1070 is isolated in a separate `CompilationProvider` output path.
- Audited `DriftDetection.cs`: options binding, candidate baseline filtering, trust validation, current snapshot generation, HFC1065/HFC1066 comparison, payload facts, sanitizer, path normalization, truncation, and HFC1070 helpers all match Story 7.4 constraints.
- Audited HFC1058-HFC1070 constants, descriptors, analyzer-release rows, registry/docs stubs, and the complete drift test suite. Historical Story 9-1 labels remain brownfield provenance and are recorded in the contract artifact.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --filter "FullyQualifiedName~Drift&Category!=Performance&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- QA Generate E2E Tests added `FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` to pin the documented drift opt-in alias when the primary `HfcDriftDetectionEnabled` option is absent.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after the QA alias pin.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline.DriftAnalyzerConfigOptionsTests.FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` passed 1/1.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class "*Drift*" -class- "*Benchmarks*"` passed 170/170 after the QA alias pin.
- Focused HFC1058-HFC1070 diagnostic parity lane passed 25/25 via direct xUnit v3 in-process runner.
- Broad SourceTools in-process lane with default exclusions ran 1023 tests: 1020 passed, 3 failed outside Story 7.4 (`deferred-work.md` missing, DataGrid FC-DOC required section gap, IDE parity path-normalization expectation).
- Configured solution-level `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 7.4 completed as confirm-and-pin with zero production source changes and one QA-generated alias opt-in test pin.
- Produced `_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md` documenting the v1 baseline schema, MSBuild options, accepted file-name prefixes, default bounds, HFC1058-HFC1069/HFC1070 dispositions, diagnostic payload contract, trust-failure precedence, redaction rules, performance/cache constraints, and verification evidence.
- Updated `_bmad-output/implementation-artifacts/tests/test-summary.md` with Story 7.4 focused results, broad SourceTools residual failures, and the local VSTest socket limitation.
- Acceptance criteria are satisfied by the existing drift implementation and focused test evidence: opt-in gating, diagnostics-only output stability, candidate baseline naming, structural HFC1065 drift, metadata HFC1066 drift, trust-failure/redaction diagnostics, deterministic ordering/truncation, catalog parity, and no `CompilationProvider` dependency in HFC1058-HFC1069 comparison.
- Historical Story 9-1 labels were intentionally retained as provenance; the Story 7.4 contract records them as pre-existing labeling debt without changing behavior.

### File List

- `_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/7-4-opt-in-drift-detection-vs-a-baseline.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs`

### Change Log

- 2026-06-05 - Completed Story 7.4 confirm-and-pin audit, produced the FC drift detection baseline contract, recorded verification evidence, and moved story/sprint tracking to review.
- 2026-06-05 - QA Generate E2E Tests added an alias opt-in pin for `FrontComposerDriftDetectionEnabled` and re-ran focused drift validation.
- 2026-06-05 - story-automator-review (adversarial). Independently re-verified all evidence: Release build 0/0; drift lane 170/170; diagnostic governance 117/118 (only the unrelated `Story112_LedgerRowsMapToOneOfThreeFinalStates` fails on missing `deferred-work.md`); broad SourceTools lane 1023 tests / 1020 passed / 3 pre-existing non-drift failures. Confirmed AC1 alias gating at `DriftDetection.cs:73-75` and the added alias pin passes 1/1. Auto-fixed 1 MEDIUM + 1 LOW evidence-accuracy issue: the contract artifact falsely stated "no test-code changes" and reported stale 169/169 + 1022/1019 counts; reconciled the contract, `test-summary.md`, and this Debug Log to the verified 170/170 + 1023/1020. No CRITICAL/HIGH findings; status moved review -> done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-05
**Outcome:** Approve (status -> done)

### Summary

Story 7.4 is a brownfield confirm-and-pin: the drift detector (HFC1058-HFC1070) already existed under historical Story 9-1 labels. The dev correctly avoided rebuilding the pipeline, added exactly one focused test pin for the documented `FrontComposerDriftDetectionEnabled` opt-in alias, and produced the FC-DRIFT-DETECTION-BASELINE v1 contract. All six acceptance criteria are satisfied and test-backed; every `[x]` task was independently confirmed done.

### Acceptance Criteria

- AC1 (structural HFC1065 + opt-in alias) — IMPLEMENTED. Alias fallback verified at `src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:73-75`; added pin `FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` passes 1/1; `DriftClassifier*` lanes green.
- AC2 (metadata HFC1066, dup capping, no-drift-when-identical) — IMPLEMENTED (drift suite 170/170).
- AC3 (diagnostics-only, byte-identical when not opted in) — IMPLEMENTED (`DriftByteStabilityRegressionTests`).
- AC4 (HFC1058-1064/1069 fail-closed, no leaks) — IMPLEMENTED (Baseline + redaction lanes).
- AC5 (HFC1067/1068, deterministic ordering, `LoadDriftBaselines` isolation, no `CompilationProvider` in comparison, separate HFC1070) — IMPLEMENTED (Incremental + Diagnostics + TrimAot lanes).
- AC6 (contract artifact + Story 9-1 labels recorded as debt) — IMPLEMENTED; contract present and complete.

### Findings and Resolutions

- MEDIUM (FIXED): Contract `fc-drift-detection-baseline-contract-2026-06-05.md` verification evidence was self-contradictory — claimed "did not require production source or test-code changes" while the File List/Change Log record an added test, and reported stale counts (169/169 drift; 1022/1019 broad). Reconciled to the verified 170/170 and 1023/1020, and to "no production source changes; one focused alias test pin added".
- LOW (FIXED): `test-summary.md` carried the same stale 1022/1019 broad-lane count; corrected to 1023/1020.
- LOW (noted, not changed): the dev-story `sprint-status.yaml` log comment says "zero production/test code changes" — a point-in-time entry that predates the QA alias-test addition; the story Completion Notes already state the accurate "one QA-generated alias opt-in test pin", so the historical log line is left intact.

### Verification (independently re-run)

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/...csproj -c Release` — 0 warnings / 0 errors.
- Drift lane `-class "*Drift*" -class- "*Benchmarks*"` — 170/170.
- Diagnostic governance (`DiagnosticRegistryTests`, `DiagnosticDescriptorTests`, `DriftDiagnosticCatalogTests`) — 117/118; the single failure is the unrelated `Story112_LedgerRowsMapToOneOfThreeFinalStates` (Story 11.2 ledger; missing `deferred-work.md`).
- Broad SourceTools lane (Performance/NightlyProperty/Quarantined/e2e-palette excluded) — 1023 total, 1020 passed, 3 failed; all 3 are pre-existing, non-drift failures inherited from the baseline and cannot have been caused by a single added test.

### File List — validated

Matches `git status` exactly. The dirty `_bmad-output/story-automator/orchestration-1-20260604-140358.md` is correctly excluded as pre-existing unrelated churn per Dev Notes.
