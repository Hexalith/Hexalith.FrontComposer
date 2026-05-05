---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-generation-mode', 'step-03-test-strategy', 'step-04c-aggregate', 'step-05-validate-and-complete']
lastStep: 'step-05-validate-and-complete'
lastSaved: '2026-05-05'
storyId: '8.6a'
storyKey: '8-6a-schema-negotiation-runtime-gate'
storyFile: '_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md'
atddChecklistPath: '_bmad-output/test-artifacts/atdd-checklist-8-6a-schema-negotiation-runtime-gate.md'
generatedTestFiles:
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationPrecedenceMatrixTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaBaselineResolverTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaTaxonomyTests.cs'
  - 'tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/RenderContractAdapterTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintReflectionTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintDeterminismTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs'
fixtureFiles:
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v1.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v2-compatible.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v2-structural-delta.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-unknown.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-same-different-order.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-same-different-runtime-data.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-hidden-precedence.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-unknown-precedence.json'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/surface-metadata-only-renderer.json'
inputDocuments:
  - '_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md'
  - '_bmad-output/implementation-artifacts/8-6-schema-versioning-and-multi-surface-abstraction.md'
  - '_bmad-output/implementation-artifacts/8-4a-projection-rendering-sanitized-taxonomy-and-snapshot.md'
  - '_bmad-output/implementation-artifacts/8-5-skill-corpus-and-build-time-agent-support.md'
  - '_bmad-output/process-notes/story-creation-lessons.md'
  - 'src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs'
  - 'src/Hexalith.FrontComposer.Contracts/Schema/SchemaBaselineContracts.cs'
  - 'src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs'
  - 'src/Hexalith.FrontComposer.Contracts/Rendering/FrontComposerRenderContract.cs'
  - 'src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs'
  - 'src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs'
  - 'src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs'
  - 'src/Hexalith.FrontComposer.SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs'
---

# ATDD Checklist — Story 8-6a · Schema Negotiation Runtime Gate & Canonicalizer Unification

> Generated 2026-05-05 by `bmad-testarch-atdd` (Tea — Master Test Architect).
> TDD phase: **RED**. All scaffolds compile but are marked `Skip = "RED-PHASE: …"`. Activation
> happens task-by-task as the implementer (Amelia / `bmad-agent-dev`) lands T1–T9.

---

## 1. Preflight & Context

| Field | Value |
| --- | --- |
| `story_id` | `8.6a` |
| `story_key` | `8-6a-schema-negotiation-runtime-gate` |
| `story_file` | `_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md` |
| Story status | `ready-for-dev` |
| Detected stack | `backend` (story scope is .NET MCP / Contracts / SourceTools / Shell) |
| Test framework | xUnit v3 + Shouldly + NSubstitute (matches existing `Hexalith.FrontComposer.Mcp.Tests` patterns) |
| Generation mode | **AI generation, sequential** (backend; no browser recording per skill default) |
| Test build gate (AC16) | `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` then `dotnet test Hexalith.FrontComposer.sln --no-build` (Contracts / Mcp / Shell / SourceTools / Bench suites) |

### Existing artifacts leveraged

- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs` — current 8-6 unit invariants; will be supplanted by snapshot-input variants under T2.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs` — sanitized taxonomy harness (CountingQueryService / SequenceEpochProvider / StaticAccessor) reused by the schema-gate scaffolds.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaAnalyzerTests.cs` — `Snapshot([…])` helper pattern reused across the new SourceTools scaffolds.

---

## 2. Generation Mode

`backend` ⇒ **AI generation, sequential**. No Playwright/MCP recording. Skill defaults `tea_use_playwright_utils: true` only matter for `frontend|fullstack` scopes. Ignored here.

---

## 3. Test Strategy — AC × Level × File × Priority

| AC | Test level | File | Priority | Linked tasks | Skip reason key |
| --- | --- | --- | --- | --- | --- |
| AC1 — negotiator runs on every projection / command / tool admission | Integration | `Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs`, `…/CommandInvokerSchemaGateTests.cs`, `…/ToolAdmissionSchemaGateTests.cs` | P0 | T3 | `RED-PHASE: T3 wiring` |
| AC1 — failure mapper surfaces sanitized schema branches | Integration | `Mcp.Tests/Invocation/ProjectionReaderSchemaTaxonomyTests.cs` | P0 | T4 | `RED-PHASE: T4 mapper` |
| AC2 — Story 8-2 hidden/unknown precedence over schema mismatch | Integration | `…/ProjectionReaderSchemaGateTests.cs::HiddenPrecedence_*` | P0 | T3 | `RED-PHASE: T3 wiring` |
| AC3 — table-driven precedence matrix with leakage assertions | Unit | `Mcp.Tests/Schema/SchemaNegotiationPrecedenceMatrixTests.cs` | P0 | T2, T3, T8 | `RED-PHASE: T2 snapshot input + T8 matrix` |
| AC4 — package-owned baseline resolver rejects untrusted paths | Unit | `Mcp.Tests/Schema/SchemaBaselineResolverTests.cs` | P0 | T1 | `RED-PHASE: T1 resolver` |
| AC5 — `CompatibleAdditive` re-runs current validation/defaulting before dispatch | Integration | `…/ProjectionReaderSchemaGateTests.cs::CompatibleAdditive_*`, `…/CommandInvokerSchemaGateTests.cs::CompatibleAdditive_*` | P0 | T3 | `RED-PHASE: T3 wiring` |
| AC6 — `HasCompatibleAdditiveDrift` replaced by `BaselineSnapshot` / `ServerSnapshot` derived inside negotiator | Unit | `Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs` | P0 | T2 | `RED-PHASE: T2 input swap` |
| AC7 — aggregate-vs-nested integrity fails closed | Unit | `Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs::AggregateMismatch_*` | P1 | T5 | `RED-PHASE: T5 integrity` |
| AC8 — runtime aggregate includes corpus fingerprints | Unit | `Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs::CorpusFingerprints_*` | P1 | T5 | `RED-PHASE: T5 corpus` |
| AC9 — lifecycle / renderer fingerprint material derived from real types/options | Unit | `SourceTools.Tests/Transforms/SchemaFingerprintReflectionTests.cs` | P1 | T6 | `RED-PHASE: T6 reflection` |
| AC10 — 9-fixture suite is discoverable and self-describing | Unit | `SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs` + `Schema/Fixtures/*.json` | P0 | T8 | `RED-PHASE: T8 fixtures` |
| AC11 — two clean generations byte-identical across OS / culture / TZ / EOL / path-separator | Unit | `SourceTools.Tests/Transforms/SchemaFingerprintDeterminismTests.cs` | P0 | T8 | `RED-PHASE: T8 determinism` |
| AC12 — truncation past 25 deltas still yields `Breaking` aggregate | Unit | `SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs` | P1 | T8 | `RED-PHASE: T8 truncation` |
| AC13 — canonicalizer unification investigation | Process | (no automated test — captured as TODO in T9) | P3 | T9 | n/a |
| AC14 — `.Mcp` adapter produces a `FrontComposerRenderContract` | Unit/Integration | `Mcp.Tests/Rendering/RenderContractAdapterTests.cs` | P1 | T7 | `RED-PHASE: T7 adapter` |
| AC15 — telemetry / logs / structured payloads only carry bounded category fields | Unit + Integration | `…/SchemaNegotiationPrecedenceMatrixTests.cs::LeakageGuards_*`, `…/ProjectionReaderSchemaTaxonomyTests.cs::SanitizedPayload_*` | P0 | T3, T4, T8 | `RED-PHASE: T4 mapper / T3 telemetry` |
| AC16 — full build + targeted test suites pass | Process | covered by CI gate (no test scaffold) | P0 | All tasks | n/a |

### Coverage map (counts)

| Test file | Skipped scaffolds | ACs | Tasks |
| --- | --- | --- | --- |
| `Schema/SchemaNegotiationPrecedenceMatrixTests.cs` | 4 | AC3, AC15 | T2, T8 |
| `Schema/SchemaNegotiationSnapshotInputTests.cs` | 4 | AC6 | T2 |
| `Schema/SchemaBaselineResolverTests.cs` | 6 | AC4 | T1 |
| `Schema/AggregateManifestIntegrityTests.cs` | 3 | AC7, AC8 | T5 |
| `Invocation/ProjectionReaderSchemaGateTests.cs` | 6 | AC1, AC2, AC5 | T3 |
| `Invocation/CommandInvokerSchemaGateTests.cs` | 3 | AC1, AC5 | T3 |
| `Invocation/ToolAdmissionSchemaGateTests.cs` | 2 | AC1 | T3 |
| `Invocation/ProjectionReaderSchemaTaxonomyTests.cs` | 4 | AC1, AC15 | T4 |
| `Rendering/RenderContractAdapterTests.cs` | 3 | AC14 | T7 |
| `Transforms/SchemaFingerprintReflectionTests.cs` | 4 | AC9 | T6 |
| `Transforms/SchemaFingerprintDeterminismTests.cs` | 2 | AC11 | T8 |
| `Diagnostics/SchemaMigrationDeltaTruncationTests.cs` | 2 | AC12 | T8 |
| `Schema/SchemaFixtureCatalogTests.cs` | 3 | AC10 | T8 |

Total: **46 skipped scaffolds** across **13 test files** + **9 fixture manifests**.

---

## 4. Red-phase status

✅ Red-phase scaffolds generated under `tests/Hexalith.FrontComposer.{Mcp,SourceTools}.Tests/`.

- All scaffolds use `[Fact(Skip = "RED-PHASE: …")]` or `[Theory(Skip = "RED-PHASE: …")]` so CI stays green until the dev unskips them task by task.
- Bodies assert **expected** behavior (not placeholders). Where a type/member from T1/T2/T5/T6/T7 does not yet exist, the body resolves the member via reflection and fails with a precise "type not yet implemented" message when unskipped — this gives meaningful red-phase failures on activation, not generic compile errors.
- No scaffold leaks raw exceptions, hidden resource names, tenant identifiers, or schema-detail payload across the sanitized boundary; the leakage guards in `SchemaNegotiationPrecedenceMatrixTests` and `ProjectionReaderSchemaTaxonomyTests` make this assertion explicit.

---

## 5. Task-by-task activation plan (handoff to `bmad-agent-dev`)

When implementing each story task, unskip the scoped scaffold(s) **before** writing production code:

1. **T1 — `ISchemaBaselineProvider` resolver**
   - Unskip `SchemaBaselineResolverTests.cs` (6 scaffolds).
   - Run `dotnet test --no-build --filter FullyQualifiedName~SchemaBaselineResolverTests`.
   - Expected red → green after T1 lands.

2. **T2 — Snapshot-based negotiator input (AC6)**
   - Unskip `SchemaNegotiationSnapshotInputTests.cs` (4 scaffolds).
   - When updating `McpSchemaNegotiationInput`, mark the legacy `HasCompatibleAdditiveDrift` `[Obsolete]`; the existing `SchemaNegotiationTests.cs` continues to assert the obsolete shape until full retirement.
   - Run `dotnet test --filter FullyQualifiedName~SchemaNegotiationSnapshotInputTests`.

3. **T3 — Production wiring (AC1, AC2, AC5, AC15)**
   - Unskip `ProjectionReaderSchemaGateTests.cs`, `CommandInvokerSchemaGateTests.cs`, `ToolAdmissionSchemaGateTests.cs`, and the `LeakageGuards_*` rows of `SchemaNegotiationPrecedenceMatrixTests.cs` (1 row).
   - Confirm the projection reader's `ValidateSnapshotAsync` is re-run on `CompatibleAdditive`.

4. **T4 — Failure mapper extension (AC1)**
   - Unskip `ProjectionReaderSchemaTaxonomyTests.cs` (4 scaffolds).
   - Add the four new branches in `FrontComposerMcpProjectionFailureMapper.Map` plus the equivalent command/tool adapters.

5. **T5 — Aggregate integrity check (AC7, AC8)**
   - Unskip `AggregateManifestIntegrityTests.cs` (3 scaffolds).

6. **T6 — Lifecycle / renderer reflection (AC9)**
   - Unskip `SchemaFingerprintReflectionTests.cs` (4 scaffolds).
   - Replace literal field lists in `SchemaFingerprintTransform.CreateLifecycleResultPayload` and the renderer payload bounds.

7. **T7 — Render-contract `.Mcp` adapter (AC14)**
   - Unskip `RenderContractAdapterTests.cs` (3 scaffolds).

8. **T8 — Fixture suite + determinism + truncation + matrix (AC3, AC10, AC11, AC12, AC15)**
   - Unskip `SchemaFixtureCatalogTests.cs`, `SchemaFingerprintDeterminismTests.cs`, `SchemaMigrationDeltaTruncationTests.cs`, and the remaining `SchemaNegotiationPrecedenceMatrixTests.cs` rows.
   - The 9 fixture JSONs already ship in `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/`; the catalog tests verify they're discoverable and self-describing.

9. **T9 — Optional canonicalizer unification (AC13)**
   - No skipped scaffold. If the Roslyn analyzer host can load `System.Text.Json` source-gen, replace `Sha256SourceToolsBlobV1` usage with `CanonicalSchemaMaterial.CreatePayload`. Otherwise document the constraint inline in `SchemaFingerprintTransform.cs` and keep D23.

10. **AC16 build gate**
    - After all activations, run `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false && dotnet test Hexalith.FrontComposer.sln --no-build` and verify the targeted suites named in AC16 are green.

---

## 6. Manual handoff

If the story file `_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md` doesn't already link these artifacts, append under `## Dev Notes`:

```markdown
### ATDD Artifacts

- Checklist: `_bmad-output/test-artifacts/atdd-checklist-8-6a-schema-negotiation-runtime-gate.md`
- API/Unit tests: see `generatedTestFiles` in checklist frontmatter
- Fixtures: see `fixtureFiles` in checklist frontmatter
```

The story already references the fixture suite under T8 — only the link to this checklist needs to be added when the dev agent picks up the work.

---

## 7. Validation summary

| Check | Result |
| --- | --- |
| All scaffolds use `Skip = "RED-PHASE: …"` (xUnit equivalent of `test.skip()`) | ✅ |
| All scaffolds assert expected behavior (no `Assert.True(true)` placeholders) | ✅ |
| All scaffolds compile against current Story 8-6 production state | ✅ — verified by `dotnet build` smoke during step 5 |
| Each AC mapped to ≥ 1 scaffold (AC13/AC16 are process gates, documented above) | ✅ |
| Coverage avoids duplication across levels (precedence-matrix unit ≠ admission integration) | ✅ |
| Sanitization / leakage guards explicit (AC15) | ✅ — see `LeakageGuards_*` rows |

— Tea
