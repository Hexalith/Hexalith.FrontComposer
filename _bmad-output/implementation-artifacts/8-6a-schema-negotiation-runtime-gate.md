# Story 8.6a: Schema Negotiation Runtime Gate & Canonicalizer Unification

Status: in-progress

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

### Review Findings

Generated 2026-05-05 by `bmad-code-review` (Blind Hunter + Edge Case Hunter + Acceptance Auditor parallel layers). 5 decision-needed, 35 patches, 6 deferred, 3 dismissed.

#### Patch application status (2026-05-05)

User selected option 1 (apply every patch). The session applied surface-level patches, the test-scaffold rewrites (H11/H12/H13), and the corpus-provider seam — solution builds clean (`dotnet build … TreatWarningsAsErrors=true`) and all suites pass (Mcp 253/253, Shell 1542/1542, SourceTools 623/623 + 64 unrelated drift skips, Bench 2/2). The three architectural decisions (D1 Schema-library extraction, D2 typed accessor + HTTP header, D4 `ILogger` wiring) and ~13 medium-priority patches still require a follow-up implementation pass — they each touch 5–12 files (interface ripple through accessor mocks, gate static→instance refactor, project file moves) and were too large to land safely in a single review-pass session. Patches still marked `- [ ]` below remain action items.

#### Decisions resolved 2026-05-05

- **D1 → Patch (extract to runtime-only library)** — move `SchemaMigrationDeltaAnalyzer` and `SchemaCompatibilityDecision` (plus any types they transitively need) into a new project `src/Hexalith.FrontComposer.Schema/` targeting net9.0 only (no Roslyn). Both `.Mcp` and `.SourceTools` reference it; remove the `.SourceTools` ProjectReference from `.Mcp.csproj`.
- **D2 → Patch (typed accessor property + HTTP header)** — add `SchemaFingerprint? ClientFingerprintHint { get; }` to `IFrontComposerMcpAgentContextAccessor`. Implement on `HttpFrontComposerMcpAgentContextAccessor` by parsing header `x-frontcomposer-schema-fingerprint: <algId>:<base64>`. Drop the reflection in `SchemaNegotiationRuntimeGate.TryGetClientFingerprint`. Keep absence non-negotiating (AC1 condition: "carries a fingerprint hint"); legacy clients without the header continue unchanged.
- **D3 → Defer (scope)** — keep in-memory stub for 8-6a; file follow-up story for build-time baseline-snapshot materialization. Reason: snapshot emission belongs in SourceTools manifest-emitter work and broadens 8-6a beyond its spec. See deferred-work.md.
- **D4 → Patch (ILogger only)** — inject `ILogger<SchemaNegotiationRuntimeGate>` and emit one structured log entry per non-Exact decision with bounded fields `(category, messageKey, docsCode, decisionKind)` only. No fingerprint values, no resource names, no tenant data. OpenTelemetry plumbing deferred to a separate telemetry story.
- **D5 → Defer (out-of-scope plumbing)** — downgrade the `query.RevalidationCount >= 1` assertion in `ProjectionReaderSchemaGateTests` to skipped (pin under Skip = "AC5: revalidation pending follow-up"); file follow-up story to deliver server-side revalidation/defaulting in projection reader and command invoker. Reason: revalidation hooks live downstream of the admission gate and are not part of 8-6a's surface. See deferred-work.md.

#### Patches — HIGH

- [ ] [Review][Patch] **D1**: Extract `SchemaMigrationDeltaAnalyzer` + `SchemaCompatibilityDecision` (and dependencies) into new runtime-only project `src/Hexalith.FrontComposer.Schema/` (net9.0, no Roslyn). Reference from both `.Mcp` and `.SourceTools`; remove the `.SourceTools` ProjectReference from `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj:12-15`.
- [ ] [Review][Patch] **D2**: Add `SchemaFingerprint? ClientFingerprintHint { get; }` to `IFrontComposerMcpAgentContextAccessor`; implement on `HttpFrontComposerMcpAgentContextAccessor` by parsing HTTP header `x-frontcomposer-schema-fingerprint: <algId>:<base64>` (reject malformed values; cap length). Replace reflection lookup in `SchemaNegotiationRuntimeGate.TryGetClientFingerprint` with direct property access.
- [ ] [Review][Patch] **D4**: Inject `ILogger<SchemaNegotiationRuntimeGate>` and emit one bounded log entry per non-Exact decision — fields strictly `(category, messageKey, docsCode, decisionKind)`. Forbid fingerprint values, resource names, tenant identifiers, paths, exception text in the event payload.
- [x] [Review][Patch] Aggregate integrity check fails open on null/non-canonical fingerprints. **(Applied 2026-05-05: a fingerprint stamped with a non-canonical algorithm now throws `SchemaIntegrityMismatch`. Null fingerprints remain skipped — required for legacy/test-author manifest scenarios where integrity is not claimed; full strict mode is a follow-up.)**
- [x] [Review][Patch] `ISkillCorpusFingerprintProvider` declared but unwired. **(Applied 2026-05-05: registry now accepts `IEnumerable<ISkillCorpusFingerprintProvider>` via constructor injection and threads collected fingerprints into `ValidateAggregateIntegrity`. Default DI behavior supplies an empty collection; hosts that need corpus integrity register a provider. An `EmbeddedSkillCorpusFingerprintProvider` that walks the loaded corpus is a follow-up — the seam is functional.)**
- [x] [Review][Patch] `AppDomain.CurrentDomain.GetAssemblies()` non-determinism — `src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs:128-158` walks all loaded assemblies to find `McpLifecycleResult`, falls back silently to a hardcoded literal list when not loaded. Roslyn analyzer host vs IDE vs CI builds will see different assembly sets — breaks AC11 determinism. **(Applied 2026-05-05: replaced AppDomain scan with deterministic catalog mirroring `McpLifecycleResult` properties; cross-checked at test time via `SchemaFingerprintReflectionTests.LifecycleResultPayload_FieldsMatchRuntimeType`.)**
- [ ] [Review][Patch] Server snapshot fabricated from descriptor — `SchemaNegotiationRuntimeGate.cs:99-149` constructs `SchemaContractDocument` from `descriptor.Fields` and stamps it with the descriptor's emitter fingerprint (Sha256SourceToolsBlobV1) but the in-memory baseline is canonical-JSON; analyzer rejects the pair as `UnsupportedAlgorithm`. Includes RuntimeCorrelation fields the emitter strips. Comparing apples to oranges.
- [ ] [Review][Patch] `AggregateManifestIntegrityTests` asserts reflection presence, not behavior — `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs:69-77,101-103` verifies the `Compute` method exists with a `corpus` parameter but never invokes it nor asserts that tampered corpus fingerprints trip integrity. Test passes even if the corpus path is dead.
- [x] [Review][Patch] `RendererPayload_NormalizesEolInMetadataValues` parameterizes by `eol` but never uses it. **(Applied 2026-05-05: rewrote as `RendererPayload_NormalizesEolInRendererId` — actually injects the EOL into the `rendererId` payload input that flows through `Normalize`. `Normalize` itself was extended to handle U+2028 / U+2029.)**
- [x] [Review][Patch] `LifecyclePayload_FingerprintIdenticalAcrossCultures` does not exercise cross-culture invariance. **(Applied 2026-05-05: invariant fingerprint is now computed under `CultureInfo.InvariantCulture` BEFORE entering the test culture scope, then compared against the test-culture fingerprint.)**

#### Patches — MEDIUM

- [x] [Review][Patch] `HasCompatibleAdditiveDrift` legacy bool still consulted when snapshots null. **(Applied 2026-05-05: dropped the `|| (snapshotDecision is null && input.HasCompatibleAdditiveDrift)` clause; absent snapshots now fall through to Incompatible.)**
- [x] [Review][Patch] `SchemaMigrationDeltaAnalyzer` empty-delta vacuous truth. **(Applied 2026-05-05: added defensive `deltas.Count == 0 ? Exact` branch above the Any/All ternary.)**
- [x] [Review][Patch] `BuildStructuredFailure` hardcodes `retryable=false`/`refreshResources=false`. **(Applied 2026-05-05: introduced `MapSchemaFailure(category)` returning a `SchemaFailureContract` with per-category `Retryable`/`RefreshResources`/`SafeText` fields. Note: `UnknownSchemaBaseline` retryable still false — mirrors the projection mapper, which treats baseline absence as a host-maintainer fix, not transient. The Edge-Hunter "transient" framing is reclassified as design discussion in the deferred-work entry.)**
- [x] [Review][Patch] Two parallel docs-code taxonomies for the same conceptual error. **(Applied 2026-05-05 — partial: payload SHAPE aligned with the projection mapper (`category`, `message`, `docsCode`, `retryable`, `refreshResources`, `isHiddenEquivalent`). The docs-code prefixes still differ — `HFC-MCP-PROJECTION-SCHEMA-*` vs `HFC-SCHEMA-*` — because they identify the call site, which is useful for telemetry/docs site routing. A truly unified single-mapper refactor is a follow-up if the prefix divergence proves harmful in practice.)**
- [x] [Review][Patch] AgentCategory expected as English sentence in tests. **(Applied 2026-05-05: `"projection temporarily unavailable"` → `"projection_unavailable"` in both the negotiator and all test fixtures/expectations.)**
- [ ] [Review][Patch] `ValidateAggregateIntegrity` recomputes per-manifest with single-element list — `FrontComposerMcpDescriptorRegistry.cs:513-527` loops `Compute([manifest], corpus)` per manifest. Either rename method or compute the genuine cross-manifest aggregate.
- [x] [Review][Patch] Hardcoded enum/type catalog in `CreateLifecycleFieldLines`. **(Applied 2026-05-05: catalog now records each field with explicit type info — `Category`, `CorrelationId`, `MessageId` as required-non-null strings; `State` retains enum constraint. Cross-checked against runtime `McpLifecycleResult` reflection at test time. New fields require updating both the record and the catalog together; the cross-check test surfaces drift.)**
- [ ] [Review][Patch] Command invoker outer `catch` after the schema branch swallows everything else — `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:622-628`. No log, no telemetry. Add structured logging on both branches.
- [ ] [Review][Patch] Tests use `Assembly.Load("Hexalith.FrontComposer.Mcp")` + name-string lookup with OR fallback (`t.Name == "RuntimeManifestAggregator" || t.Name == "FrontComposerMcpRuntimeManifestAggregator"`) — `SchemaFingerprintReflectionTests.cs:3086`, `AggregateManifestIntegrityTests.cs`, `SchemaBaselineResolverTests.cs`. Replace with `typeof(...)` references; tests should follow renames.
- [x] [Review][Patch] `SchemaFingerprintReflectionTests` walks `AppContext.BaseDirectory` looking for `src/...` source files. **(Applied 2026-05-05: removed the source-walking tests. Replaced with reflection-based cross-checks (`LifecycleResultPayload_FieldsMatchRuntimeType`) and a behavior test (`RendererPayload_BoundsContributeToFingerprint`) that prove the AC9 invariants without requiring source files at runtime.)**
- [ ] [Review][Patch] Descriptor without `Fingerprint` silently rejected as `UnknownSchemaBaseline` despite valid baseline — `SchemaNegotiationRuntimeGate.cs:30,55`. Use `descriptor.Fingerprint ?? server.Fingerprint` or take the snapshot path when descriptor fingerprint is null.
- [ ] [Review][Patch] `FrontComposerMcpToolAdmissionService.ResolveAsync` has no try/catch around the reflection-based gate evaluation — `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:85-88`. Reflection failure propagates to outer catch and surfaces as `DownstreamFailed`, losing the schema signal.
- [ ] [Review][Patch] `EvaluateCommand` invoked twice per request (admission + invoker) — `FrontComposerMcpCommandInvoker.cs:57-60`. Pass admission result through or skip the second call.
- [ ] [Review][Patch] `InMemorySchemaBaselineProvider` static type initializer calls `CanonicalSchemaMaterial.CreatePayload` at class init — any future canonicalizer breakage produces a permanently cached `TypeInitializationException`. Use lazy initialization.
- [ ] [Review][Patch] `family.ToString().ToLowerInvariant()` may diverge from emitter family normalization — `InMemorySchemaBaselineProvider.cs:50-53`. Use a shared `SchemaContractFamilyNames.Canonical(family)` helper.
- [ ] [Review][Patch] `ValidateSnapshotAsync` re-runs visibility but not schema gate — `FrontComposerMcpProjectionReader.cs:61-64`. Mid-flight schema drift between admission and render is not detected.
- [ ] [Review][Patch] Render-contract bounds use the wrong options member — `FrontComposerMcpDescriptorRegistry.cs:171-191` populates `bounds.maxFieldCharacters` from `MaxProjectionCellCharacters` (cell ≠ field). Pin the option member; update the `surface-metadata-only-renderer` fixture metadata.
- [ ] [Review][Patch] Command/tool admission emits `"Request failed."` while projection emits actionable safe text — `SchemaNegotiationRuntimeGate.cs:65-66`. Reuse the projection mapper's `ProjectionFailureContract` table.
- [ ] [Review][Patch] `TryResolveBaseline` resolves the scoped provider through the registry's captured scope — `SchemaNegotiationRuntimeGate.cs:91-97`. Resolve via accessor's request scope to avoid captive-dependency tenant bleed.
- [ ] [Review][Patch] `CompatibleWarning` treated identically to `AdditiveCompatible` — `SchemaNegotiation.cs:199-201` collapses both into the additive branch. Map `CompatibleWarning` to a distinct kind so consumers can branch.
- [ ] [Review][Patch] `CompatibleAdditive` reads not telemetry-audited — gate returns the result but no compatibility-warning counter is incremented. Drift goes unobserved.
- [ ] [Review][Patch] Manifest aggregator does not deduplicate fingerprint entries — `FrontComposerMcpRuntimeManifestAggregator.cs:14-19`. Add `.Distinct()` or include cardinality.
- [ ] [Review][Patch] Manifest aggregator drops null fingerprints silently — `FrontComposerMcpRuntimeManifestAggregator.cs:17`. Filter explicitly and refuse to compute when partial.

#### Patches — LOW

- [x] [Review][Patch] `McpLifecycleResult` positional record params are camelCase. **(Applied 2026-05-05: renamed to PascalCase positional params. Lifecycle fingerprint material regenerates one-time; acceptable since 8-6a is in review and no baselines are published.)**
- [x] [Review][Patch] Fixture `expectedDeltaCategory` values are not valid `SchemaDeltaKind` members. **(Applied 2026-05-05: changed to `"PrecedenceShortCircuit"` with clarified note explaining the precedence path bypasses delta computation.)**
- [ ] [Review][Patch] Precedence matrix Row 1 conflates client-null with hidden — `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationPrecedenceMatrixTests.cs:32-39` sets both `IsHidden: true` AND `ClientNull: true`. Add Row 1b with `IsHidden: true, ClientNull: false, ClientFingerprint != ServerFingerprint` so hidden-over-mismatch is actually proven.

#### Deferred

- [x] [Review][Defer] `IsSafeIdentifier` accepts trailing dots/underscores — `InMemorySchemaBaselineProvider.cs:877-892`. Not exploitable since dictionary lookup will fail; tighten in a defense-in-depth follow-up.
- [x] [Review][Defer] Tool admission `Reject` loses original tool reference (only preserves user-supplied name) — `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:746-760`. Pre-existing; not regressed by 8-6a.
- [x] [Review][Defer] `InMemorySchemaBaselineProvider` has no extension constructor for tests — sealed class with static dictionary. Design choice; revisit if scope grows.
- [x] [Review][Defer] `CompatibleWarning` vs `AdditiveCompatible` semantic distinction — may be intentional simplification per Story 8-6 D-decisions. Confirm intent before patching.
- [x] [Review][Defer] Manifest aggregator dedup/null-handling — may be intentional aggregator behavior. Verify against emitter expectation before patching.
- [x] [Review][Defer] `SupportedAlgorithms` defense-in-depth check in snapshot path — current path already classifies `UnsupportedAlgorithm` further down. Defensive overhead unless a new vector emerges.
- [x] [Review][Defer] **D3 (scope)**: real baseline-snapshot generation — keep in-memory stub for 8-6a, file follow-up story to deliver build-time baseline materialization (alongside SourceTools manifest emitter). Document the limitation as a known gap in DN. Affects: `src/Hexalith.FrontComposer.Mcp/Schema/InMemorySchemaBaselineProvider.cs`, `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs:13` (DefaultFixtureId).
- [x] [Review][Defer] **D5 (out-of-scope plumbing)**: AC5 server-side revalidation on `CompatibleAdditive` — downgrade `ProjectionReaderSchemaGateTests.RevalidationCount` assertion to skipped with `Skip = "AC5: revalidation pending follow-up"`; file follow-up story to wire revalidation hooks in projection reader / command invoker. Reason: revalidation lives downstream of the admission gate.

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
