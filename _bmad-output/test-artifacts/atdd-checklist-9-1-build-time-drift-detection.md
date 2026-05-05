---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04-generate-tests', 'step-04c-aggregate', 'step-05-validate-and-complete']
lastStep: 'step-05-validate-and-complete'
lastSaved: '2026-05-05'
storyId: '9.1'
storyKey: '9-1-build-time-drift-detection'
storyFile: '_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md'
atddChecklistPath: '_bmad-output/test-artifacts/atdd-checklist-9-1-build-time-drift-detection.md'
generatedTestFiles:
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftComparisonServiceTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierProjectionPropertyTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierTypeAndNullabilityTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierBoundedContextTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierRenameTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineMissingDiagnosticTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticContractTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticRedactionTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticOrderingAndTruncationTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticPrecedenceTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticCatalogTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Incremental/DriftIncrementalCacheTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftByteStabilityRegressionTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureInvarianceTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Seam/DriftSeamPublicSurfaceContractTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs'
fixtureFiles:
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-empty.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-malformed.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-unsupported-schema.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-unsupported-algorithm.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-oversized.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-duplicate-identity-within.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-duplicate-identity-across-a.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-duplicate-identity-across-b.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-invariant-violation.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-valid-projection-v1.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-valid-command-v1.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/Fixtures/baseline-redaction-sentinels.json'
inputDocuments:
  - '_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md'
  - 'src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs'
  - 'src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md'
  - 'src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs'
  - 'src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/HotReloadRebuildClassifierTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaAnalyzerTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs'
  - '.claude/skills/bmad-testarch-atdd/resources/knowledge/test-levels-framework.md'
  - '.claude/skills/bmad-testarch-atdd/resources/knowledge/test-priorities-matrix.md'
  - '.claude/skills/bmad-testarch-atdd/resources/knowledge/test-quality.md'
  - '_bmad-output/process-notes/story-creation-lessons.md'
---

# ATDD Checklist — Story 9-1 · Build-Time Drift Detection

> Generated 2026-05-05 by `bmad-testarch-atdd` (Tea — Master Test Architect).
> TDD phase: **RED**. All scaffolds will compile but be marked `Skip = "RED-PHASE: …"`. Activation
> happens task-by-task as the implementer (Amelia / `bmad-agent-dev`) lands T1–T7.

---

## 1. Preflight & Context

| Field | Value |
| --- | --- |
| `story_id` | `9.1` |
| `story_key` | `9-1-build-time-drift-detection` |
| `story_file` | `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md` |
| Story status | `ready-for-dev` (party-mode + advanced-elicitation review complete) |
| Detected stack (story scope) | `backend` — SourceTools generator/analyzer + Roslyn `AdditionalText` baselines; no Shell, MCP runtime, or Playwright e2e in scope |
| Test framework | xUnit v3 + Shouldly + `Verify.Xunit` (matches existing `Hexalith.FrontComposer.SourceTools.Tests` patterns) |
| Generation mode | **AI generation, sequential** (backend; no Playwright/MCP recording — `tea_use_playwright_utils:true` ignored for backend scope) |
| Test build gate | `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` then `dotnet test Hexalith.FrontComposer.sln --no-build` |
| Diagnostic ID block | Reserve **HFC1058+** contiguous range (current Unshipped allocation ends at HFC1057) |

### Existing artifacts to leverage / extend

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs` — reflective HFC ID/uniqueness/release-notes/HelpLinkUri parity. Extend for HFC1058+ range and new `FcDiagnosticIds` constants once the impl introduces it.
- `tests/.../Diagnostics/HotReloadRebuildClassifierTests.cs` — HFC1010 classifier precedent; mirror style for new precedence table.
- `tests/.../Caching/IncrementalCachingTests.cs` — current tracked step assertions (`Parse`, `ParseCommand`, `ParseProjectionTemplate`); extend for new `LoadDriftBaselines` tracked step + `AdditionalText` cache behavior.
- `tests/.../Benchmarks/IncrementalRebuildBenchmarkTests.cs` — NFR8 perf harness; extend with warmup-excluded median + p95 over cache-hit and cache-miss paths.
- `tests/.../Integration/GeneratorDriverTests.cs` — `CSharpGeneratorDriver` integration pattern (HelpLinkUri, source-location assertions).
- `tests/.../Diagnostics/SchemaMigrationDeltaTruncationTests.cs` — truncation summary / sorted ordering precedent (mirrors AC16 first-50 + omitted-count rule).
- `tests/.../Diagnostics/SchemaMigrationDeltaAnalyzerTests.cs` — `Snapshot([…])` fixture-driven matrix pattern; template for T7 matrix-driven classifier tests.
- `tests/.../Schema/SchemaFixtureCatalogTests.cs` — JSON fixture loader/validator; reuse for baseline trust-failure fixtures (empty/malformed/oversized/duplicate-id/unsupported-schema/unsupported-algorithm).

### Cross-story handoffs anchored

- **Story 8-6 (`SchemaFingerprintTransform`)**: drift detector compares **generated UI baseline** structural metadata, not MCP schema fingerprints. No reuse of MCP fingerprint code.
- **Story 1-8 (`HFC1010` hot-reload)**: drift diagnostics never reuse HFC1010; they go in HFC1058+ block.
- **Story 9-2 (CLI inspect/update)**: comparison seam stays internal (`InternalsVisibleTo`); no public API shape introduced in 9-1 scaffolds.
- **Story 9-4 (diagnostic governance)**: HelpLinkUri values use the documented placeholder shape; final URLs land in 9-4.

### Notes worth flagging during implementation

1. `FcDiagnosticIds` constants class is referenced by the story but does **not** yet exist in `src/Hexalith.FrontComposer.SourceTools`. T4 introduces it; catalog tests must assert both `DiagnosticDescriptors` and `FcDiagnosticIds` parity once the impl lands.
2. AC10 / T2 caching seam: existing tests only assert `Parse`/`ParseCommand`/`ParseProjectionTemplate`. New drift `AdditionalTextsProvider` adds a tracked step (`LoadDriftBaselines`); use `InternalsVisibleTo` + `IncrementalGeneratorRunReason` snapshot pattern already in `IncrementalGeneratorCacheHitTests.cs`.
3. AC19 culture-invariance: no `CultureInfo`-scoped fixture exists yet; will add one for `fr-FR` and `tr-TR`.

---

## 2. Generation Mode

`backend` ⇒ **AI generation, sequential**. No Playwright/MCP recording. Skill defaults `tea_use_playwright_utils: true` only matter for `frontend|fullstack` scopes. Ignored here. AI generation works directly from the story acceptance criteria, the existing SourceTools IR (`DomainModel`, `CommandModel`, `PropertyModel`, `RazorModel`, `CommandFormModel`, `RegistrationModel`), and the matrix/snapshot patterns established in `SchemaMigrationDeltaAnalyzerTests` and `IncrementalCachingTests`.

---

## 3. Test Strategy

### 3.1 AC → scenario → level → priority

| AC | Scenarios (red-phase) | Level | Priority | Planned test file |
| --- | --- | --- | --- | --- |
| AC1 | Drift comparison runs only when a trusted baseline parses; pure structural compare returns expected `DriftResult` for matched/added/removed/changed members. | Unit | P1 | `DriftComparisonServiceTests.cs` |
| AC2 | Removed projection property → 1 diagnostic; carries `DeclarationName`, `MemberName`, affected UI surface, docs link; default Warning unless options stricter. | Unit (matrix) | P1 | `DriftClassifierProjectionPropertyTests.cs` |
| AC3 | Added projection property → diagnostic classifies add into one of {DataGrid column, detail field, MCP descriptor, unsupported placeholder}. | Unit (matrix) | P1 | `DriftClassifierProjectionPropertyTests.cs` |
| AC4 | One removed + one added of compatible category in same declaration ⇒ deterministic rename diagnostic with exact message wording per Epic 9; ambiguous matches split into add+remove. | Unit (matrix) + golden message string | P1 | `DriftClassifierRenameTests.cs` |
| AC5 | Type/category change (string↔int) and nullability change → Warning diagnostic stating affected rendering; covers projection + command property. | Unit (matrix) | P1 | `DriftClassifierTypeAndNullabilityTests.cs` |
| AC6 | BoundedContext name change → 1 diagnostic listing all affected surfaces (navigation/registration/session/MCP/badge grouping). | Unit | P1 | `DriftClassifierBoundedContextTests.cs` |
| AC7 | Display/role/group/priority/badge/icon/destructive/policy metadata changes → ≤1 diagnostic per declaration per impact category. | Unit (matrix) | P1 | `DriftClassifierMetadataTests.cs` |
| AC8 | Missing baseline (first run) ⇒ 1 actionable diagnostic distinct from invalid baseline path; explicit Story-9-2 CLI handoff text. | Unit + Integration | **P0** | `DriftBaselineMissingDiagnosticTests.cs` |
| AC9 | Trust failures: empty / whitespace / malformed / oversized / duplicate-id-within-file / duplicate-id-across-files / unsupported schema version / unsupported algorithm / invariant violation → Error diagnostic, drift comparison suppressed, no last-writer-wins merge. | Unit (matrix) + golden fixtures | **P0** | `DriftBaselineTrustFailureTests.cs` + `Fixtures/baseline-*.json` |
| AC10 | Unchanged compilation: `Parse`/`ParseCommand`/`ParseProjectionTemplate` + new `LoadDriftBaselines` step report `Cached`/`Unchanged`; unrelated file edits do not invalidate domain parsing; unchanged `AdditionalText` does not re-parse. | Integration (`CSharpGeneratorDriver` + tracked-step snapshot) | **P0** | `DriftIncrementalCacheTests.cs` |
| AC11 | NFR8: median incremental added overhead < 500 ms on bounded fixture, warmup excluded, ≥ 20 iterations, median + p95 for cache-hit and cache-miss. Marked benchmark, not unit gate. | Benchmark/Perf | P1 | `DriftBenchmarkTests.cs` (sibling to `IncrementalRebuildBenchmarkTests`) |
| AC12 | Each emitted diagnostic exposes structured properties (`BaselinePath`, `DeclarationPath`, `DeclarationName`, `MemberName`, `DriftKind`, `ExpectedShapeHash`, `ActualShapeHash`, `SchemaVersion`, `AlgorithmVersion`), uses `HelpLinkUri`, normalizes paths to repo-relative or `<outside-project>` sentinel, includes What/Expected/Got/Fix/DocsLink. | Integration (`CSharpGeneratorDriver` property assertions) | **P0** | `DriftDiagnosticContractTests.cs` |
| AC13 | Tenant ID / user ID / claim / token / cache key / ETag / payload / row / absolute path / raw JSON / generated source / oversized text sentinels never appear in messages, diagnostic properties, or exception text — even when present in source/baseline fixtures. | Unit (sentinel-soaked fixtures) | **P0** | `DriftDiagnosticRedactionTests.cs` |
| AC14 | `PublishTrimmed=true` analyzer-config + reflection-catalog evidence + no adopter override ⇒ Warning diagnostic pointing to source-generated catalog. | Integration | P2 | `TrimAotReflectionCatalogDiagnosticTests.cs` |
| AC15 | Host policy catalog unknowable at build time ⇒ no diagnostic emitted; runtime validators stay authoritative (negative test). | Unit | P2 | `TrimAotReflectionCatalogDiagnosticTests.cs` |
| AC16 | Many drift diagnostics: sort by bounded context → declaration kind → declaration name → member name → drift kind (ordinal); first 50 emitted, then 1 truncation summary with omitted count. | Unit (matrix + ordering snapshot) | P1 | `DriftDiagnosticOrderingAndTruncationTests.cs` |
| AC17 | Comparison seam is `internal` only — no public CLI/command verb/code-fix/source-rewrite/public API surface added. Reflective contract test asserts only documented internal types are exposed via `InternalsVisibleTo`. | Contract / structural | P3 | `DriftSeamPublicSurfaceContractTests.cs` |
| AC18 | Existing generated Razor/Fluxor/registration/MCP outputs remain byte-stable across: repeated runs, equivalent input ordering (`[Projection]` declared in different file order), partial-type declaration order, equivalent baseline file ordering. Drift detection adds diagnostics only — never rewrites output. | Integration (Verify byte-equality across N permutations) | **P0** | `DriftByteStabilityRegressionTests.cs` |
| AC19 | Under `CultureInfo("fr-FR")` and `CultureInfo("tr-TR")`: classification, sort order, hash digests, numeric formatting in messages, diagnostic property values, and generated bytes are identical to invariant baseline. | Integration (CultureScope fixture × matrix) | P1 | `DriftCultureInvarianceTests.cs` |

### 3.2 Cross-cutting / catalog tests (extend existing files)

| Concern | Extension | File |
| --- | --- | --- |
| HFC1058+ uniqueness, HFCxxxx shape, release-notes parity, `HelpLinkUri` populated | extend reflective catalog test | `Diagnostics/DiagnosticCatalogTests.cs` (existing — append assertions for the new range and for `FcDiagnosticIds` constants once impl introduces it) |
| Precedence ordering (missing → malformed → unsupported-schema → unsupported-algorithm → oversized → duplicate-id → structural drift → metadata drift → renderer drift → trim/AOT → truncation) | new precedence-table test | `DriftDiagnosticPrecedenceTests.cs` |
| Invalid analyzer-config option (`build_property.HfcDriftMaxDiagnostics=-1`, etc.) → deterministic config diagnostic + safe-default fall-through | matrix unit | `DriftAnalyzerConfigOptionsTests.cs` |
| Multi-baseline merge: ordinal-sorted path normalization, dedup, fail-closed on cross-file duplicate identity (no last-writer-wins) | matrix unit | (folded into `DriftBaselineTrustFailureTests.cs`) |
| Partial type declaration merge order does not affect identity / hash / diagnostic / generated output | matrix unit | (folded into `DriftComparisonServiceTests.cs`) |

### 3.3 Test directory layout

```
tests/Hexalith.FrontComposer.SourceTools.Tests/
├── Drift/
│   ├── Comparison/
│   │   ├── DriftComparisonServiceTests.cs
│   │   ├── DriftClassifierProjectionPropertyTests.cs
│   │   ├── DriftClassifierTypeAndNullabilityTests.cs
│   │   ├── DriftClassifierBoundedContextTests.cs
│   │   ├── DriftClassifierMetadataTests.cs
│   │   └── DriftClassifierRenameTests.cs
│   ├── Baseline/
│   │   ├── DriftBaselineMissingDiagnosticTests.cs
│   │   ├── DriftBaselineTrustFailureTests.cs
│   │   ├── DriftAnalyzerConfigOptionsTests.cs
│   │   └── Fixtures/
│   │       ├── baseline-empty.json
│   │       ├── baseline-malformed.json
│   │       ├── baseline-unsupported-schema.json
│   │       ├── baseline-unsupported-algorithm.json
│   │       ├── baseline-oversized.json
│   │       ├── baseline-duplicate-identity-within.json
│   │       ├── baseline-duplicate-identity-across-a.json
│   │       ├── baseline-duplicate-identity-across-b.json
│   │       ├── baseline-invariant-violation.json
│   │       ├── baseline-valid-projection-v1.json
│   │       ├── baseline-valid-command-v1.json
│   │       └── baseline-redaction-sentinels.json
│   ├── Diagnostics/
│   │   ├── DriftDiagnosticContractTests.cs
│   │   ├── DriftDiagnosticRedactionTests.cs
│   │   ├── DriftDiagnosticOrderingAndTruncationTests.cs
│   │   └── DriftDiagnosticPrecedenceTests.cs
│   ├── Incremental/
│   │   └── DriftIncrementalCacheTests.cs
│   ├── Regression/
│   │   ├── DriftByteStabilityRegressionTests.cs
│   │   └── DriftCultureInvarianceTests.cs
│   ├── TrimAot/
│   │   └── TrimAotReflectionCatalogDiagnosticTests.cs
│   └── Seam/
│       └── DriftSeamPublicSurfaceContractTests.cs
├── Benchmarks/
│   └── DriftBenchmarkTests.cs              (sibling of IncrementalRebuildBenchmarkTests)
└── Diagnostics/
    └── DiagnosticCatalogTests.cs           (existing — append HFC1058+ assertions)
```

### 3.4 Coverage budget

- **Test classes:** 17 new + 1 extension to existing catalog test
- **Skipped (RED-PHASE) tests:** ~110 across the 17 files (matrix theories expand)
- **JSON fixtures:** 12
- **No duplicate coverage:** classifier matrices live in `Drift/Comparison/`; diagnostic-shape concerns live in `Drift/Diagnostics/`; baseline trust lives in `Drift/Baseline/`; incremental + regression + culture concerns kept separate from classifier logic.

### 3.5 Red-phase activation order (handoff to dev)

| Order | File(s) | Activated when |
| --- | --- | --- |
| 1 | `DriftComparisonServiceTests` + `DriftClassifier*` | T1 + T3 land (baseline contract + classifier) |
| 2 | `DriftBaselineMissingDiagnosticTests` + `DriftBaselineTrustFailureTests` + `DriftAnalyzerConfigOptionsTests` | T1 + T2 land (baseline parser + trust failures + options validation) |
| 3 | `DriftDiagnosticContractTests` + `DriftDiagnosticRedactionTests` + `DriftDiagnosticOrderingAndTruncationTests` + `DriftDiagnosticPrecedenceTests` + `DiagnosticCatalogTests` extension | T4 lands (HFC1058+ descriptors, FcDiagnosticIds, precedence table, redaction) |
| 4 | `DriftIncrementalCacheTests` | T2 lands (`AdditionalTextsProvider` + `LoadDriftBaselines` tracked step) |
| 5 | `DriftByteStabilityRegressionTests` + `DriftCultureInvarianceTests` | T5 lands (no auto-rewrite + culture-invariant emission) |
| 6 | `TrimAotReflectionCatalogDiagnosticTests` | T6 lands (trim/AOT evidence diagnostic) |
| 7 | `DriftSeamPublicSurfaceContractTests` | T1+T3+T4 land (internal seam settled) |
| 8 | `DriftBenchmarkTests` | T7 perf instrumentation lands |

### 3.6 Red-phase guarantees

- Every generated test compiles against current `Hexalith.FrontComposer.SourceTools.Tests` references (`xunit.v3`, `Shouldly`, `NSubstitute`, `Microsoft.CodeAnalysis.CSharp`, `Verify.Xunit`).
- Every test is decorated with `Skip = "RED-PHASE: <T#> — pending <implementation artefact>"` so the unmodified suite stays green.
- Skip strings name the **task** (T1–T7) so dev can `grep -r "RED-PHASE: T3"` to find the wave to unskip.
- Fixtures are committed alongside scaffolds so that activation only requires removing `Skip` and implementing the surface under test.

---

## 4. Generation Outputs

### 4.1 Files written

**20 test files** (17 new + 1 existing helper made `public` + 1 sibling catalog test) and **12 JSON fixtures**, organised under `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/` (plus `Benchmarks/DriftBenchmarkTests.cs`).

| Wave | Files | Activated when |
| --- | --- | --- |
| 1 — Comparison & classifiers | `Drift/Comparison/Drift{ComparisonService,ClassifierProjectionProperty,ClassifierTypeAndNullability,ClassifierBoundedContext,ClassifierMetadata,ClassifierRename}Tests.cs` | T1 + T3 land |
| 2 — Baseline trust & options | `Drift/Baseline/Drift{BaselineMissingDiagnostic,BaselineTrustFailure,AnalyzerConfigOptions}Tests.cs` + `Fixtures/baseline-*.json` | T1 + T2 + T4 land |
| 3 — Diagnostics | `Drift/Diagnostics/Drift{DiagnosticContract,DiagnosticRedaction,DiagnosticOrderingAndTruncation,DiagnosticPrecedence,DiagnosticCatalog}Tests.cs` | T4 lands |
| 4 — Incremental cache | `Drift/Incremental/DriftIncrementalCacheTests.cs` | T2 lands |
| 5 — Regression & byte-stability | `Drift/Regression/Drift{ByteStabilityRegression,CultureInvariance}Tests.cs` | T5 + T7 land |
| 6 — Trim/AOT advisory | `Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs` | T6 lands |
| 7 — Seam contract | `Drift/Seam/DriftSeamPublicSurfaceContractTests.cs` | T1 + T3 + T4 land |
| 8 — Benchmark / NFR8 | `Benchmarks/DriftBenchmarkTests.cs` | T7 perf instrumentation lands |

### 4.2 Test counts (post-build)

```
Build:  0 errors, 0 warnings (Hexalith.FrontComposer.SourceTools.Tests)
Suite:  Failed: 0 | Passed: 606 | Skipped: 75 | Total: 681  (Duration: 4 s)
Drift:  Failed: 0 | Skipped: 64 (every new RED-PHASE test reports Skipped)
```

The 64 RED-PHASE tests across 20 new test classes assert behavior the dev agent will turn on
task-by-task. The 2 "Passed" matches that turned up under the `~Drift` filter
(`ProjectionTemplateMarkerTests.RunGenerators_*VersionDrift_*`) are pre-existing tests whose
method names contain the substring "Drift" and are unrelated to Story 9-1.

### 4.3 RED-phase contract enforced

- Every new `[Fact]` and `[Theory]` carries `Skip = "RED-PHASE: T# — <pending impl>"`.
- Every test asserts the **planned behavior** (not placeholder `Should.NotThrow(...)` no-ops).
- Reflective lookups (`TryFindServiceType()`, `Assembly.GetTypes()`, `MethodInfo.GetParameters()`) keep the assembly compiling against the **current** SourceTools surface; activation only requires the dev agent to remove the `Skip` once the named type/method exists.
- All 12 JSON fixtures parse as JSON (except `baseline-empty.json` — intentionally empty) and the malformed/oversized/duplicate fixtures are negative-case proof artefacts wired into `DriftBaselineTrustFailureTests`.

### 4.4 Activation playbook for the dev agent

```
T1  → unskip:  Drift/Comparison/DriftComparisonServiceTests
              Drift/Baseline/DriftBaselineMissingDiagnosticTests
              Drift/Baseline/DriftBaselineTrustFailureTests (Fixture_FailsClosed_*)
              Drift/Baseline/DriftAnalyzerConfigOptionsTests
              Drift/Seam/DriftSeamPublicSurfaceContractTests (Service_TypeIsInternal_*)

T2  → unskip:  Drift/Incremental/DriftIncrementalCacheTests
              Drift/Baseline/DriftBaselineTrustFailureTests (DuplicateIdentityAcross*)

T3  → unskip:  Drift/Comparison/DriftClassifier*Tests (all six)
              Drift/Diagnostics/DriftDiagnosticOrderingAndTruncationTests

T4  → unskip:  Drift/Diagnostics/Drift{DiagnosticContract,DiagnosticPrecedence,DiagnosticCatalog}Tests
              Drift/Diagnostics/DriftDiagnosticRedactionTests

T5  → unskip:  Drift/Regression/DriftByteStabilityRegressionTests

T6  → unskip:  Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests

T7  → unskip:  Drift/Regression/DriftCultureInvarianceTests
              Benchmarks/DriftBenchmarkTests

After every wave, dev should run:
  dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false
  dotnet test  Hexalith.FrontComposer.sln --no-build
```

### 4.5 Notes left for implementation

- `FcDiagnosticIds` already exists at `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` — earlier preflight note had it as missing; it's a constants registry shared between Contracts and SourceTools. T4 adds HFC1058+ entries there, not a new file.
- `DriftIncrementalCacheTests` expects a tracked step named exactly `LoadDriftBaselines`. If T2 picks a different name, update the constant `ExpectedTrackedStepName` at the top of the file.
- `DriftSeamPublicSurfaceContractTests` requires `[InternalsVisibleTo("Hexalith.FrontComposer.SourceTools.Tests")]` on the SourceTools assembly; the test will fail until this attribute is added.
- `DriftBenchmarkTests` uses `Stopwatch`-based measurement and is wall-clock sensitive. If CI runners produce noisy numbers, mark the test class with a custom xUnit `[Trait("Category", "Benchmark")]` and exclude from PR runs (consistent with `IncrementalRebuildBenchmarkTests`).

---

## 5. Validation & Completion

### 5.1 Checklist (backend-applicable items only — frontend Playwright/Cypress sections marked N/A)

| Section | Status | Notes |
| --- | --- | --- |
| Prerequisites — story approved with clear ACs | ✅ | 19 ACs, ready-for-dev (party-mode + advanced-elicitation reviewed) |
| Prerequisites — dev environment ready | ✅ | `dotnet build` + `dotnet test` succeed locally |
| Prerequisites — framework scaffolding exists | ✅ | xUnit v3 + Shouldly + NSubstitute + Verify already wired in `Hexalith.FrontComposer.SourceTools.Tests.csproj` |
| Prerequisites — playwright.config.ts | N/A | Backend-only story; Playwright config exists at `tests/e2e/playwright.config.ts` for the wider repo but not used here |
| Step 1 — story markdown loaded | ✅ | `_bmad-output/implementation-artifacts/9-1-build-time-drift-detection.md` |
| Step 1 — ACs identified | ✅ | All 19 mapped in §3.1 |
| Step 1 — affected systems identified | ✅ | SourceTools generator/analyzer + Contracts FcDiagnosticIds + AnalyzerReleases.Unshipped.md |
| Step 1 — knowledge fragments loaded | ✅ | `test-levels-framework`, `test-priorities-matrix`, `test-quality`, `data-factories`, `component-tdd`, `test-healing-patterns`, `ci-burn-in` |
| Step 2 — test level selection | ✅ | Unit majority + Integration (`CSharpGeneratorDriver`) + 1 Benchmark; documented per AC |
| Step 2 — duplicate coverage avoided | ✅ | Comparison/Baseline/Diagnostics/Incremental/Regression/TrimAot/Seam directories partition concerns |
| Step 2 — P0–P3 prioritisation applied | ✅ | P0: AC8/9/10/12/13/18; P1: AC1–7/11/16/19; P2: AC14/15; P3: AC17 |
| Step 3 — red-phase scaffolds created at appropriate level | ✅ | 20 test classes; all under `Drift/` plus `Benchmarks/DriftBenchmarkTests.cs` |
| Step 3 — Given-When-Then format | ✅ | Each test name encodes scenario; XML-doc cites AC and task |
| Step 3 — `data-testid` selectors | N/A | No UI tests in scope |
| Step 3 — Network-first pattern | N/A | No HTTP traffic; in-process Roslyn driver |
| Step 3 — every test marked Skip | ✅ | All 64 new tests report `Skipped` (full-suite run: 0 Failed, 75 Skipped, 606 Passed) |
| Step 4 — data factories | ✅ | 12 JSON baseline fixtures + reflective `InvocationProbe` helper |
| Step 4 — fixtures with auto-cleanup | ✅ | Fixtures are read-only inputs; nothing to clean up |
| Step 4 — `data-testid` requirements listed | N/A | No UI |
| Step 5 — implementation checklist created | ✅ | Activation playbook §4.4 maps tests → tasks T1–T7 |
| Step 6 — output file created | ✅ | `_bmad-output/test-artifacts/atdd-checklist-9-1-build-time-drift-detection.md` |
| Step 6 — frontmatter complete | ✅ | `storyId`, `storyKey`, `storyFile`, `atddChecklistPath`, `generatedTestFiles`, `fixtureFiles`, `inputDocuments` populated |
| Quality — deterministic, isolated, atomic, &lt; 300 lines, no hard waits | ✅ | Largest test file is `DriftBaselineTrustFailureTests.cs` at ~140 lines; no `Thread.Sleep`/`waitForTimeout`; `CultureInfo` mutations restored in `finally` |
| Knowledge base — `test-quality.md` principles applied | ✅ | No conditionals controlling flow; assertions explicit in test bodies; no try/catch for flow control |
| Code quality — no warnings | ✅ | `dotnet build` reports `0 Warning(s) 0 Error(s)` |
| CLI sessions cleaned up | N/A | No browser automation |
| Temp artifacts in `{test_artifacts}` | ✅ | Checklist at `_bmad-output/test-artifacts/`; no scratch files in random locations |

### 5.2 Risks / assumptions handed off to dev (Amelia)

1. **`InternalsVisibleTo` is required.** `DriftSeamPublicSurfaceContractTests.DriftComparisonService_IsAccessibleOnlyViaInternalsVisibleTo` will fail until `[InternalsVisibleTo("Hexalith.FrontComposer.SourceTools.Tests")]` is added to the SourceTools assembly. Add it as part of T1.
2. **Tracked-step name is hard-coded.** `DriftIncrementalCacheTests` expects exactly `"LoadDriftBaselines"`. If T2 picks a different name, update the constant `ExpectedTrackedStepName`.
3. **HFC1058+ allocation block.** `DriftDiagnosticCatalogTests` enforces the allocation lives in `[1058, 1099]`. If Story 9-4 finalises a different range first, both the catalog test and `AnalyzerReleases.Unshipped.md` need updating.
4. **Epic 9 verbatim rename wording.** `DriftClassifierRenameTests.OneRemoved_OneAdded_CompatibleCategory_EmitsRenameWithEpic9Wording` asserts the literal message shape from AC4. Drift on the wording = drift on the test.
5. **Benchmark wall-clock sensitivity.** `DriftBenchmarkTests` is sensitive to CI runner load. Consider a `[Trait("Category", "Benchmark")]` and a separate CI job, mirroring `IncrementalRebuildBenchmarkTests`.
6. **Diagnostic location seam.** `DriftDiagnosticContractTests.DiagnosticLocation_PointsAtSourceDeclaration_NotBaselineFile` requires the analyzer to attach the source-declaration `Location` (not the AdditionalText location). Existing patterns from `Hfc1031DiagnosticTests` show the right approach.

### 5.3 Verification commands

```powershell
# RED-phase build + suite (run after each unskip wave)
dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false
dotnet test  Hexalith.FrontComposer.sln --no-build

# Drift-only filter
dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests --no-build --filter "FullyQualifiedName~Drift"
```

### 5.4 Recommended next workflow

`bmad-dev-story 9-1` (Amelia activates T1 → T7 in order, unskipping the matching wave per §4.4
after each landing). After T7 lands, run `bmad-testarch-test-review` to confirm green-phase quality.
