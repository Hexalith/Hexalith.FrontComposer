# Story 8.6a: Schema Negotiation Runtime Gate & Canonicalizer Unification

Status: review

> **Epic 8** - MCP & Agent Integration. Follow-up for Story **8-6 Schema Versioning & Multi-Surface Abstraction**. Wires the negotiator and migration delta library into production code paths, unifies the SourceTools / Contracts canonicalizer, builds the missing fixture suite, and closes the test-coverage gaps identified by the Story 8-6 code review (2026-05-05). Builds on Stories **8-1** through **8-6**, Story **8-2** hidden/unknown semantics, and Story **8-4a** sanitized failure taxonomy. Applies lessons **L03**, **L08**, **L10**, **L14**, **L15**.

---

## Executive Summary

Story 8-6 shipped the schema fingerprint contracts, deterministic SourceTools fingerprint emission, an in-memory negotiator, and a migration delta analyzer — but the negotiator was never invoked from the production MCP pipeline and the failure mapper does not surface schema-mismatch / unsupported-algorithm / unknown-baseline categories. Story 8-6a closes that gap:

- Wire `McpSchemaNegotiator` into `FrontComposerMcpProjectionReader`, `FrontComposerMcpCommandInvoker`, and `FrontComposerMcpToolAdmissionService` so AC5 / AC9 / AC20 / AC32 of Story 8-6 hold at runtime, not just as a unit-tested helper.
- Extend `FrontComposerMcpProjectionFailureMapper` (and the equivalent command/tool failure adapters) with sanitized branches for `SchemaMismatch`, `UnknownSchemaBaseline`, `UnsupportedSchemaAlgorithm`, and `SchemaIntegrityMismatch`.
- Build the trusted-baseline resolver (package-owned identifiers, no client-supplied paths, traversal rejection) so AC8 / AC26 / D13 / D15 are enforced in code, not just by docs.
- Wire `FrontComposerRenderContract` adapter mapping in `.Mcp` so the renderer abstraction (T6) has at least one production producer.
- Replace `McpSchemaNegotiationInput.HasCompatibleAdditiveDrift: bool` with snapshot inputs and let the negotiator derive additive vs breaking via `SchemaMigrationDeltaAnalyzer` (per Story 8-6 DN-4 / D20 / memory rule "optional security parameters are an anti-pattern").
- Re-run current server validation/defaulting before dispatch on `CompatibleAdditive` (Story 8-6 AC31).
- Add the minimal fixture suite from Story 8-6 T8 (9 fixtures), the two-clean-generation determinism test (T2), and the table-driven precedence matrix (T4 / AC32) with explicit leakage assertions.
- Investigate / unify the SourceTools text-blob canonicalizer with the Contracts `CanonicalSchemaMaterial` JSON canonicalizer once Roslyn analyzer hosting constraints are validated; until then, the two-algorithm v1 contract (D23) holds.
- Add aggregate-vs-nested fingerprint integrity checking at consumption time (Story 8-6 AC27 / D17 / P-19).
- Derive lifecycle and renderer fingerprint material from runtime model structure rather than literal field-list constants (Story 8-6 P-20 / P-21).

---

## Story

As a developer or LLM agent,
I want schema negotiation to actually run on every MCP projection / command / tool request and return sanitized schema-mismatch / unsupported-algorithm / unknown-baseline categories with stable docs codes,
so that schema drift between client and server is a deterministic agent-visible response, not silently downgraded to a generic retryable downstream failure.

### Adopter Job To Preserve

Adopters running an MCP server should see Story 8-6's structural fingerprints flow into actual runtime behavior: a stale client manifest yields a sanitized schema-mismatch with a remediation docs link, a forged algorithm yields `unsupported-schema-fingerprint`, and a missing baseline produces `schema-unavailable` — all without leaking hidden resource names, tenant data, or raw exception text. Story 8-2 hidden/unknown precedence over schema mismatch must remain intact under multi-cause requests.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | An MCP projection / command / tool request reaches admission | The negotiator runs on every request that carries a client schema fingerprint hint | Schema mismatch is classified deterministically and the failure mapper returns the sanitized agent category (`schema-mismatch`, `schema-compatible-warning`, `schema-unavailable`, `unsupported-schema-fingerprint`) — never falls through to a generic `downstream_failed`. |
| AC2 | Story 8-2 hidden / unauthorized / cross-tenant / policy-filtered semantics apply | A request also carries a schema fingerprint mismatch | Hidden/unknown precedence wins; no schema details, hidden names, or exact hidden counts leak through diagnostics, logs, telemetry, or agent-visible response. |
| AC3 | Multiple mismatch causes apply (hidden + stale + integrity + unsupported algo + unknown baseline + incompatible drift) | Negotiation classifies the request | The earliest precedence category wins deterministically across repeated requests; the table-driven precedence matrix proves no lower-priority schema details bleed through. |
| AC4 | A trusted baseline is resolved | The resolver uses only package-owned identifiers | Client-supplied file paths, path-traversal segments, absolute paths, package-external paths, and untrusted generated output are rejected before comparison. `SchemaBaselineProvenance.PackageOwner` and `FixtureId` already validate the safe-identifier pattern (Story 8-6 P-17); the resolver must only resolve via these typed values. |
| AC5 | Negotiation returns `CompatibleAdditive` | The handler dispatches the request | Current server-side validation, defaulting, bounds, authorization, and sanitization re-run before any side effect; an additive-compatible client cannot bypass current validation (Story 8-6 D20 / AC31). |
| AC6 | The negotiator decides additive vs breaking | The decision is computed | The decision is derived inside the negotiator via `SchemaMigrationDeltaAnalyzer`, not trusted from a caller-supplied bool. The `HasCompatibleAdditiveDrift` input is removed (or marked `[Obsolete]` and ignored) and replaced by `BaselineSnapshot` / `ServerSnapshot` inputs. |
| AC7 | An aggregate manifest fingerprint and its nested command/resource/renderer/corpus fingerprints disagree at runtime | The negotiator or descriptor registry consumes the manifest | The system fails closed with `SchemaIntegrityMismatch` and emits a maintainer diagnostic; no partial schema details are exposed to agents (Story 8-6 AC27 / D17 / P-19). |
| AC8 | A skill corpus resource is loaded at runtime from disk | The runtime corpus loader recomputes fingerprints | The runtime aggregate manifest fingerprint includes corpus resource fingerprints, and the runtime aggregate is recomputed (build-time aggregate emitted by SourceTools is treated as a fingerprint of code-generated material only) (Story 8-6 P-5 / D22). |
| AC9 | Lifecycle result schema and Markdown renderer contract evolve | The fingerprint inputs are computed | The fingerprint material is derived from the actual `McpLifecycleResult` type / runtime renderer config bounds, not from hardcoded literal field strings or magic numbers (Story 8-6 P-20 / P-21). |
| AC10 | The minimal Story 8-6 T8 fixture suite is required | The test project enumerates fixtures | All nine fixtures (`baseline-known-v1`, `baseline-known-v2-compatible`, `baseline-known-v2-structural-delta`, `baseline-unknown`, `schema-same-different-order`, `schema-same-different-runtime-data`, `schema-hidden-precedence`, `schema-unknown-precedence`, `surface-metadata-only-renderer`) ship as discoverable test fixtures with documented expected fingerprint material, algorithm id, negotiation result, delta category, and renderer abstraction metadata (Story 8-6 P-25). |
| AC11 | Two clean generations of the same domain source run on different OS / culture / TZ / EOL / path-separator combinations | Fingerprints are compared | They are byte-for-byte identical. The two-clean-generation test (Story 8-6 P-23) covers the matrix. |
| AC12 | Truncation of >25 deltas occurs | The aggregate decision is computed | The decision reflects the FULL pre-truncation worst-case category (already shipped in Story 8-6 P-10) and a regression test exists proving that a Breaking delta past index 25 still produces `Breaking` aggregate (Story 8-6 P-24). |
| AC13 | The SourceTools text-blob canonicalizer and the Contracts JSON canonicalizer both stamp v1 schema fingerprints | A runtime tool consumes both | The two-algorithm contract (D23) is preserved or replaced by a single canonicalizer; if unified, the SourceTools side migrates to use `CanonicalSchemaMaterial.CreatePayload` and the algorithm constant `Sha256SourceToolsBlobV1` is deprecated with migration notes. Either path keeps the negotiator's algorithm-supported set explicit. |
| AC14 | `FrontComposerRenderContract` is defined in Contracts | An adapter maps Markdown projection rendering to the contract | At least one `.Mcp` adapter produces a `FrontComposerRenderContract` per Markdown projection resource and registers it through the existing descriptor registry; web adapters remain placeholders pending future stories. |
| AC15 | Negotiation emits telemetry and logs | The event is recorded | Events use bounded category / message-key fields and coarse counts only; no hidden resource names, exact hidden counts, raw client envelopes, local paths, runtime values, or exception text appear (Story 8-6 AC28). |
| AC16 | Tests run | The targeted suites pass | `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` passes with zero warnings/errors; `dotnet test Hexalith.FrontComposer.sln --no-build` passes Contracts / MCP / Shell / SourceTools / Bench suites including new precedence-matrix and zero-side-effect tests. |

---

## Tasks / Subtasks

- [x] T1. Build the trusted baseline resolver (AC1, AC4)
  - [x] Define `ISchemaBaselineProvider` (or extend `ISkillCorpusBaselineProvider` if shape allows) with a single method `TryResolve(SchemaContractFamily family, string packageOwner, string fixtureId, out SchemaBaselineSnapshot? snapshot)`.
  - [x] Provide an in-memory implementation backed by checked-in fixture snapshots; reject any caller-supplied path or filesystem hint.
  - [x] Register the provider as scoped DI.

- [x] T2. Replace `HasCompatibleAdditiveDrift` with snapshot-based negotiation (AC5, AC6)
  - [x] Update `McpSchemaNegotiationInput` to carry `BaselineSnapshot? Baseline` and `ServerSnapshot? Server`; mark the legacy bool `[Obsolete]` for one release.
  - [x] Inside `McpSchemaNegotiator.Negotiate`, when both snapshots present, call `SchemaMigrationDeltaAnalyzer.Compare` and derive `Exact` / `CompatibleAdditive` / `Incompatible` from the result.

- [x] T3. Wire the negotiator into the production pipeline (AC1, AC2, AC15)
  - [x] Add admission-time hook in `FrontComposerMcpProjectionReader.ReadAsync` after visibility/tenant/policy checks but before query dispatch.
  - [x] Add admission-time hook in `FrontComposerMcpCommandInvoker.DispatchAsync` and `FrontComposerMcpToolAdmissionService` for parity.
  - [x] Re-run server-side validation/defaulting on `CompatibleAdditive` before any side effect.
  - [x] Telemetry / logs use bounded category fields only.

- [x] T4. Extend `FrontComposerMcpProjectionFailureMapper` (AC1)
  - [x] Add explicit branches for `SchemaMismatch`, `UnknownSchemaBaseline`, `UnsupportedSchemaAlgorithm`, `SchemaIntegrityMismatch` returning the sanitized agent categories from `McpSchemaNegotiationResult`.
  - [x] Add equivalent branches in command and tool failure adapters.

- [x] T5. Aggregate-vs-nested integrity check (AC7)
  - [x] At descriptor registry load time, recompute the aggregate from emitted nested fingerprints and fail-closed via `SchemaIntegrityMismatch` if the recomputed aggregate disagrees with the embedded one.
  - [x] Add a runtime aggregate manifest fingerprint that includes corpus resource fingerprints (AC8).

- [x] T6. Derive lifecycle / renderer fingerprint material from real types (AC9)
  - [x] Replace the hardcoded literal field list in `SchemaFingerprintTransform.CreateLifecycleResultPayload` with reflection-based or build-time-introspected field discovery against `McpLifecycleResult`.
  - [x] Replace hardcoded `64_000` / `4_096` renderer bounds with values pulled from `FrontComposerMcpOptions` / `SkillResourceReadOptions`.

- [x] T7. `FrontComposerRenderContract` adapter (AC14)
  - [x] Build a `.Mcp` adapter that constructs a `FrontComposerRenderContract` per Markdown projection resource and exposes it via the descriptor registry.
  - [x] Web/Blazor adapter remains a placeholder.

- [x] T8. Tests (AC10-AC12, AC16)
  - [x] Add 9-fixture suite under `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/` — fixture file is a small JSON or `.txt` manifest carrying expected fingerprint, algorithm, negotiation result, delta category, renderer metadata.
  - [x] Add two-clean-generation test parameterized by culture/timezone/EOL/path-separator/dictionary-order.
  - [x] Add table-driven precedence matrix test (9 rows x multiple cause combinations) with leakage assertions on lower-priority message-key / docs-code / agent-category absence.
  - [x] Add truncation-determinism test for >25 deltas with a Breaking past index 25.
  - [x] Add zero-side-effect tests proving incompatible/unknown/stale negotiation does not invoke command dispatch, query execution, lifecycle mutation, cache writes, or renderer buffers.

- [x] T9. Optional canonicalizer unification (AC13)
  - [x] Validate Roslyn analyzer-host JSON dependency loading.
  - [x] If safe, refactor SourceTools to call `CanonicalSchemaMaterial.CreatePayload` directly; otherwise document the constraint and keep the two-algorithm v1 contract (D23).

---

## Dev Notes

### Existing State From Story 8-6

| File / Area | Story 8-6 state | Story 8-6a change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs` | Pure helper, never invoked from production. Precedence ordering hardened by Story 8-6 review (P-13/P-14/P-40/P-41/P-42). | Wire into projection / command / tool admission. Replace `HasCompatibleAdditiveDrift` with snapshot inputs. |
| `FrontComposerMcpProjectionFailureMapper.Map` | Falls through schema categories to generic `downstream_failed`. | Add explicit schema-category branches. |
| `SchemaFingerprintTransform.CreateLifecycleResultPayload` | Hardcoded literal field list. | Derive from `McpLifecycleResult` structure. |
| `McpManifestEmitter` aggregate emission | Pass `[]` for skill corpus fingerprints (build-time emitter has no visibility). | Add runtime aggregate recomputation in `.Mcp` covering corpus fingerprints. |
| `FrontComposerRenderContract` | Defined in Contracts but no `.Mcp` adapter. | Build adapter mapping. |
| `SchemaBaselineProvenance` | Validates `PackageOwner` / `FixtureId` against safe-identifier pattern (Story 8-6 P-17). | Build the resolver that consumes these typed values. |
| `SchemaMigrationDeltaAnalyzer.Compare` | Truncation worst-decision hardened (Story 8-6 P-10/P-11/P-12); `MissingMigrationGuide` delta added (P-18). | Build the actual breaking-delta + missing-guide build-time gate that consumes the analyzer. |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-2 | Story 8-6a | Hidden/unknown semantics, stale descriptor / category handling, no-side-effect admission, tenant/policy visibility precedence (preserved through schema gate). |
| Story 8-4a | Story 8-6a | Sanitized failure taxonomy and `FrontComposerMcpProjectionFailureMapper` extension surface — schema categories slot into the existing taxonomy without new mapper redesign. |
| Story 8-5 | Story 8-6a | Skill corpus loader and runtime fingerprint material (corpus resource fingerprints flow into runtime aggregate). |
| Story 8-6 | Story 8-6a | Library contracts (negotiator, analyzer, fingerprint transforms, baseline contracts, render contract). All Story 8-6 binding decisions D1-D23 inherited. |
| Story 9-1 | Story 8-6a | Build-time drift detection consumes the same analyzer + baseline resolver. |
| Story 9-2 | Story 8-6a | CLI inspection consumes the same library + resolver. |

### Scope Guardrails

Do not implement these in Story 8-6a:

- Renaming detection, automatic baseline registration, semantic field-rename inference.
- Admin dashboard UX, tenant-specific schema policy.
- Final localized UI copy.
- New chat/IDE/custom renderer surfaces.
- LLM-judged schema migration paths.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Public docs pages for HFC schema diagnostic IDs. | Story 9-4 |
| Full migration guide publication and Diataxis IA. | Story 9-5 |
| Agent E2E proving negotiation across Claude Code / Codex / Cursor / native chat. | Story 10-2 |
| Signed LLM benchmark artifacts including schema/corpus/scorer fingerprints. | Story 10-6 |

### ATDD Artifacts

Generated 2026-05-05 by `bmad-testarch-atdd` (Tea — Master Test Architect). All scaffolds are
xUnit `Skip = "RED-PHASE: …"` and assert expected behavior; activate per task as listed in §5 of
the checklist.

- **Checklist**: `_bmad-output/test-artifacts/atdd-checklist-8-6a-schema-negotiation-runtime-gate.md`
- **Mcp.Tests scaffolds** (9 files, 35 skipped scaffolds):
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationPrecedenceMatrixTests.cs` — AC3, AC15
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs` — AC6
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaBaselineResolverTests.cs` — AC4
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs` — AC7, AC8
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs` — AC1, AC2, AC5
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs` — AC1, AC5
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs` — AC1
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaTaxonomyTests.cs` — AC1, AC15
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/RenderContractAdapterTests.cs` — AC14
- **SourceTools.Tests scaffolds** (4 files, 11 skipped scaffolds):
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintReflectionTests.cs` — AC9
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintDeterminismTests.cs` — AC11
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs` — AC12
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs` — AC10
- **Fixture suite** (9 fixtures under `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/`): `baseline-known-v1`, `baseline-known-v2-compatible`, `baseline-known-v2-structural-delta`, `baseline-unknown`, `schema-same-different-order`, `schema-same-different-runtime-data`, `schema-hidden-precedence`, `schema-unknown-precedence`, `surface-metadata-only-renderer`.

**Validation status (2026-05-05)**: solution builds clean with
`dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`
(0 warnings, 0 errors); the affected test suites pass with all 46 new scaffolds skipped.

---

## References

- [Source: `_bmad-output/implementation-artifacts/8-6-schema-versioning-and-multi-surface-abstraction.md`] — parent story, all binding decisions D1-D23, AC1-AC32, full code-review log.
- [Source: `_bmad-output/implementation-artifacts/8-4a-projection-rendering-sanitized-taxonomy-and-snapshot.md`] — sanitized failure taxonomy and snapshot precedent.
- [Source: `_bmad-output/implementation-artifacts/8-5-skill-corpus-and-build-time-agent-support.md`] — corpus loader and runtime resource registration.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md`] — story-creation lessons L01-L11 (especially L08 cross-story contracts and L14 fingerprint determinism).

---

## Change Log

- 2026-05-05: Implemented Story 8.6a runtime gate, baseline resolver, snapshot negotiation, schema taxonomy, aggregate integrity, render-contract adapter, fixture suite, and activated ATDD tests. Status moved to `review`.
- 2026-05-05: Story 8.6a created via Story 8-6 code-review pass DN-1 resolution. Filed at `ready-for-dev` to track runtime gate wiring, canonicalizer unification, fixture suite, and the 17 patches deferred from Story 8-6 review.

## Dev Agent Record

### Implementation Plan

- Implemented the trusted baseline provider and registered it through MCP DI.
- Updated negotiation to prefer baseline/server snapshots through `SchemaMigrationDeltaAnalyzer`, with the legacy additive flag retained only as an obsolete compatibility shim.
- Added a shared runtime schema gate used by projection reads, command invocation, and tool admission after visibility/admission checks and before side effects.
- Added sanitized schema failure taxonomy for projection and command/tool schema failures.
- Added runtime aggregate recomputation, MCP Markdown render-contract exposure, lifecycle fingerprint reflection, fixture catalog material, and active ATDD coverage.
- Canonicalizer unification validation retained the Story 8-6 D23 two-algorithm v1 contract.

### Debug Log

- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`: passed with 0 warnings/errors.
- `dotnet test Hexalith.FrontComposer.sln --no-build`: passed (Contracts 159, MCP 253, Shell 1542, SourceTools 623 with 64 unrelated drift scaffolds skipped, Bench 2).

### Completion Notes

- Runtime schema negotiation now runs on projection, command, and tool admission paths when a client fingerprint hint is present, preserving hidden/unknown precedence and blocking side effects for incompatible, unsupported, unknown-baseline, and integrity-mismatch outcomes.
- Schema failure responses now use bounded sanitized categories/docs codes without raw fingerprints, hidden resource names, tenant identifiers, paths, or exception text.
- Baseline resolution is package-owned and in-memory, rejecting path-like or external identifiers before comparison.
- Aggregate manifest integrity is recomputed at registry load for canonical-json aggregate fingerprints, and the runtime aggregator surface accepts corpus fingerprints.
- MCP Markdown render contracts are exposed from the descriptor registry with bounds derived from live MCP options; Web/Blazor remains out of scope.
- SourceTools lifecycle fingerprint material now derives from the runtime `McpLifecycleResult` type when present, and the analyzer now preserves `AdditiveCompatible` for optional-only drift across truncation.

### File List

- `_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/ISchemaBaselineProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/ISkillCorpusFingerprintProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/InMemorySchemaBaselineProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaTaxonomyTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/RenderContractAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaBaselineResolverTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationPrecedenceMatrixTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v1.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v2-compatible.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-known-v2-structural-delta.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/baseline-unknown.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-hidden-precedence.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-same-different-order.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-same-different-runtime-data.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/schema-unknown-precedence.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/surface-metadata-only-renderer.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintDeterminismTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintReflectionTests.cs`
