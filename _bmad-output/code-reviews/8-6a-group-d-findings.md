# Story 8-6a — Code Review Group D Findings

**Date:** 2026-05-06
**Scope:** Fresh adversarial review of full story 8-6a diff (`1fa9cd5..HEAD`, excluding Story 9-1 drift scaffolds)
**Diff size:** 50 files / 3,011 lines (4 chunks)
**Reviewers:** 12 parallel subagents (Blind Hunter × 4, Edge Case Hunter × 4, Acceptance Auditor × 4)
**Prior-review dedup baseline:** Groups A, B, C applied/deferred/dismissed (`prior-review-context.md`)
**Raw findings:** `8-6a-group-d/raw-findings.md`

---

## Summary

- **5 Critical** — production-blocking issues; must fix before merge out of review
- **8 High** — correctness defects; fix in this story or explicitly defer with risk acknowledgment
- **9 Medium** — should-fix follow-ups; defer with owner if not addressed now
- **15 Low** — nice-to-have hardening; track or dismiss
- **12 Dismissed / duplicate of A/B/C** — already covered

---

## Critical (production-blocking — fix before merge)

### C1. Source-generated manifests **fail registry construction** at startup (H7 vs SourceTools algorithm mismatch)
**Severity:** Critical · **Fresh:** Yes · **Sources:** Chunk B Edge, Chunk D Edge

`FrontComposerMcpDescriptorRegistry.cs:180-182` (Group A H7 applied) hard-fails when `manifest.Fingerprint.AlgorithmId != Sha256CanonicalJsonV1`.

`SourceTools/Transforms/SchemaFingerprintTransform.cs:42` pins `AlgorithmId = Sha256SourceToolsBlobV1`. `Emitters/McpManifestEmitter.cs:48-53` calls `CreateAggregateManifestPayload(...)` whose `Payload(...)` (`SchemaFingerprintTransform.cs:255`) stamps the SourceTools blob algorithm, **not canonical JSON**.

**Effect:** Every host that consumes the source generator throws `SchemaIntegrityMismatch` on the first generated manifest discovered by `LoadGeneratedManifests`. **Production startup is blocked.** Tests do not catch this — no test in `Hexalith.FrontComposer.Mcp.Tests` references `FrontComposerMcpGeneratedManifest`, `[GeneratedManifest]`, or `ManifestAssemblies` (verified).

**Cross-decision conflict:** D23 explicitly preserves the two-algorithm v1 contract; H7 was applied as if the algorithm were already unified. Either H7 must accept `Sha256SourceToolsBlobV1`, or SourceTools must switch its aggregate manifest fingerprint to `Sha256CanonicalJsonV1` (different from per-command/resource).

**Recommended fix:** Choose one path:
- **(a)** Loosen H7 to accept either supported algorithm (preserves D23) **and** make `ValidateAggregateIntegrity` recompute via the same canonicalizer the manifest declared (requires invoking SourceTools' blob canonicalizer at runtime — currently unavailable at runtime).
- **(b)** Add a SourceTools `CreateAggregateManifestPayloadCanonical` that uses `CanonicalSchemaMaterial.CreatePayload` (canonical JSON) for the manifest's aggregate fingerprint only, while per-command/resource keep `Sha256SourceToolsBlobV1`. Then H7 is consistent.
- **(c)** Add an integration test that loads a real SourceTools-generated manifest into `ManifestAssemblies` to lock the contract.

---

### C2. AC8 corpus integrity contract is silently dead — `corpusFingerprints` discarded by per-manifest loop
**Severity:** Critical · **Fresh:** Yes · **Sources:** Chunk B Auditor

`FrontComposerMcpDescriptorRegistry.cs:43-47` collects `corpusFingerprints` from `ISkillCorpusFingerprintProvider`s and passes them into `ValidateAggregateIntegrity(manifests, corpusFingerprints)`. **At line 171** the parameter is explicitly discarded: `_ = corpusFingerprints;`. The loop at line 184 calls `Compute([manifest], [])` — corpus is forced empty.

No production caller invokes `FrontComposerMcpRuntimeManifestAggregator.Compute(manifests, corpusFingerprints)` with a non-empty corpus list (only test files do).

**AC8 mandates** "the runtime aggregate manifest fingerprint includes corpus resource fingerprints, and the runtime aggregate is recomputed." This invariant has no production implementation.

**AC7 mandates** "corpus fingerprints disagree at runtime → fails closed with `SchemaIntegrityMismatch`." A tampered/substituted corpus resource cannot trip integrity check; only per-manifest-nested tampering can.

**Recommended fix:** Add a runtime aggregate computation path that includes corpus and is persisted for cross-process verification, OR document that AC7's corpus dimension and AC8 are explicitly deferred and update spec to match.

---

### C3. Client-server fingerprint encoding mismatch makes byte-equality short-circuit unreachable; `descriptor.Fingerprint`-only deployments **always fail closed**
**Severity:** Critical · **Fresh:** Yes · **Sources:** Chunk B Edge, Chunk B Blind

- Server emits via `Sha256Hex` (`SchemaFingerprintContracts.cs:331-339`) — 64 lowercase hex chars
- Client header parser stores raw base64 (`HttpFrontComposerMcpAgentContextAccessor.cs:147`) — 44 chars including `+`/`/`/`=`
- `string.Equals(client.Value, server.Value, Ordinal)` (`SchemaNegotiation.cs:150`) cannot match

**Effects:**
1. P-40 byte-identical short-circuit (`SchemaNegotiation.cs:197`) is dead — happy path always falls through to structural snapshot compare.
2. **Critical regression:** when `baseline is null` (no provider registered or fixture not found) but `descriptor.Fingerprint is not null`, gate sets `HasTrustedBaseline=true`, `serverFingerprint=descriptor.Fingerprint` (hex), `Server=server` snapshot. `hashesMatch=false`, `CompareSnapshots` returns null because `input.Baseline is null`. Falls through to `Incompatible/SchemaMismatch`. **Any host shipping only in-process generated manifests without a baseline provider gets `SchemaMismatch` for every request carrying a fingerprint hint.**

**Recommended fix:** Pick one wire encoding and convert on the boundary. Either parse base64 → hex on accessor, or have `Sha256Hex` return base64. Add a round-trip test.

---

### C4. `MapSchemaFailureStrict` blows up on `UnsupportedSchema` (UnknownClientVersion path) → silent collapse to `DownstreamFailed`
**Severity:** Critical · **Fresh:** Yes · **Sources:** Chunk C Edge

`SchemaNegotiation.cs:127` returns `FrontComposerMcpFailureCategory.UnsupportedSchema` for the `UnknownClientVersion` arm. `SchemaNegotiationRuntimeGate.cs:106-107` (Strict) rejects this category with `ArgumentException` (only the four schema categories are accepted). `FrontComposerMcpCommandInvoker.cs:62` calls `ToStructuredFailure(schema.FailureCategory)` unconditionally → `ArgumentException` collapses to `DownstreamFailed` via outer `catch`.

**Effect:** Agents lose the "unknown-version" signal entirely; AC1 violated.

Mirror bug in `FrontComposerMcpProjectionFailureMapper.cs:113-119`: `UnsupportedSchema` falls into the `UnsupportedRender or UnsupportedSchema` arm and emits **render** taxonomy (`unsupported_render`, `HFC-MCP-PROJECTION-UNSUPPORTED-RENDER`), unrelated to the actual root cause.

**Recommended fix:** Either include `UnsupportedSchema` in the strict mapper's allow-list with a sanitized `unsupported-schema-version` agent category, or change `SchemaNegotiation.cs:127` to return `UnsupportedSchemaAlgorithm`. Either way, fix the projection mapper's render-vs-schema arm.

---

### C5. `InMemorySchemaBaselineProvider` is the **default registration** with a stub baseline (`SchemaFieldContract("Number", …)`) → all requests with fingerprint hint **fail closed**
**Severity:** Critical (production-impact, but explicitly deferred via D3) · **Fresh:** No (already deferred D3) · **Sources:** Chunk C Edge, Chunk B Edge

`FrontComposerMcpServiceCollectionExtensions.cs:40` registers `InMemorySchemaBaselineProvider` via `TryAddScoped`. The provider's snapshot at `InMemorySchemaBaselineProvider.cs:64` declares a single placeholder field `("Number", "String", "string", true, false)`. When resolved against any real descriptor, `Compare` produces Breaking deltas → `SchemaMismatch` for every request with a client fingerprint hint.

**Already deferred** to D3 ("real baseline-snapshot generation"). Surfacing here as **production-impact reminder**: any adopter of the default registration without overriding the baseline provider has every MCP command and projection-with-fingerprint-hint **fail closed** until D3 ships. Combined with C3, this is a guaranteed-deny path.

**Recommended fix:** Either disable the default registration (require explicit opt-in until D3), or have the placeholder provider return `false` from `TryResolve` so the gate falls back to descriptor.Fingerprint behavior. **Update `FrontComposerMcpServiceCollectionExtensions` defaults.**

---

## High (correctness — fix in this story)

### H1. `hashesMatch` ignores `AlgorithmId` — cross-algorithm collision short-circuit
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk B Blind

`SchemaNegotiation.cs:150`: `string.Equals(input.ClientFingerprint.Value, input.ServerFingerprint.Value, StringComparison.Ordinal)` compares values only, not algorithm IDs. Two fingerprints from different algorithms sharing the same encoded string trip `Exact` on line 197. Combined with C3, this is partially mitigated (the values can never match anyway), but the defensive design is wrong.

**Recommended fix:** Add `string.Equals(input.ClientFingerprint.AlgorithmId, input.ServerFingerprint.AlgorithmId, Ordinal)` to the `hashesMatch` predicate.

---

### H2. `CompatibleAdditive` / `CompatibleWarning` allow side effects but bypass argument-shape revalidation against baseline; AC5 contract weakens to "current schema only"
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk C Edge, Chunk D Auditor

- `SchemaNegotiation.cs:211-219, 221-229` set `allowsSideEffects=true` for both kinds.
- `FrontComposerMcpCommandInvoker.cs:59-65` short-circuits only on `!AllowsSideEffects`.
- `ValidateArguments` (line 226) validates against current `descriptor.Parameters` only — no re-validation against baseline.
- `CompatibleWarning` (e.g., enum-value removed) treated identically to `Exact`. No warning surfaced in success envelope.
- **Concrete edge:** `EnumChanged` produces `CompatibleWarning` (`SchemaMigrationDeltaAnalyzer.cs:91-95`). Gate allows side effects. `ValidatePrimitiveShape` checks against current `parameter.EnumValues`. Client whose baseline allowed "Old" sends "Old"; server's current schema removed "Old" → validation rejects with `ValidationFailed`. **Agent has no signal rejection is downstream of schema drift the gate already saw.**

**AC5** ("current server-side validation/defaulting/bounds re-runs before any side effect") needs to surface schema-drift context in the error response. **AC6** ("derive additive vs breaking via analyzer") is correct, but the consumer-side handling of `CompatibleWarning` is dropped on the floor.

**Recommended fix:** When `kind == CompatibleWarning`, attach `schema.warning` metadata to both success and validation-failure envelopes so agents can surface drift to operators.

---

### H3. AC5 `RevalidationCount` test assertion is vacuously satisfied; deferral marker missing
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk D Auditor, Chunk D Blind

`ProjectionReaderSchemaGateTests.cs:80,194-206` asserts `query.RevalidationCount.ShouldBeGreaterThanOrEqualTo(1)`. The fake's increment fires on `request.Take > 0 && request.Take <= 1024`. Production reader at `FrontComposerMcpProjectionReader.cs:74-78` always sets `Take = Math.Max(1, Math.Min(DefaultResourceTake, MaxResourceTake))` for every code path. Counter increments unconditionally → assertion passes regardless of whether revalidation actually ran.

Prior context **D5 chose to skip** this test under `Skip = "AC5: revalidation pending follow-up"`, but **no `Skip` attribute is present anywhere in chunk D** — deferral was not honored. Test is live, falsely green.

`CompatibleAdditive_OnCommand_AdmitsDispatch_AfterRevalidation` (`CommandInvokerSchemaGateTests.cs:157-182`) is structurally weaker still: `if (result.IsError) { … } else { … }` — every outcome is "valid".

**Recommended fix:** Either apply the `Skip` marker per D5, or anchor the assertion to a side-channel that ONLY fires when revalidation runs (e.g., a dedicated `Mock<IServerValidator>` with `Verifiable()` setup distinct from `Take`-bound increment).

---

### H4. `Document.Fields` duplicate-name throws `ArgumentException` from the analyzer; un-handled by `Negotiate` → unhandled-exception 500
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk A Edge

`SchemaMigrationDeltaAnalyzer.cs:68-69`: `ToDictionary(f => f.Name, StringComparer.Ordinal)` throws on duplicate field name. `Compare` accepts arbitrary `SchemaBaselineSnapshot` instances and never re-runs `ValidateDocument`. Exception escapes to `Negotiate` (`SchemaNegotiation.cs:240-247`) which has no `try/catch`. Surfaces to MCP request handlers as unhandled exception → contradicts documented fail-closed precedence.

**Recommended fix:** Wrap `Compare` in `Negotiate` with `try/catch (SchemaMaterialValidationException ex) { return Result.UnknownBaseline(...) }` or have `Compare` return `Unknown` on duplicate-key detection.

---

### H5. Field-level `IsRequired` / `IsNullable` flips not surfaced as deltas
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk A Edge

`SchemaMigrationDeltaAnalyzer.cs:90-103` only compares `JsonType`, `TypeName`, `EnumValues`, `ValidationConstraints` for fields present on both sides. Flipping `IsRequired:false → true` (clear breaking change) produces zero structured deltas. Hash diff falls through to `Unknown` aggregate → operators get generic "Unknown → Incompatible" rather than actionable diagnostic identifying the field whose required-flag changed.

`Document.Collections` is not iterated at all (line 64-117); collection rename / `StableIdField` change produces zero deltas → same `Unknown` collapse.

**Recommended fix:** Add `RequiredFlagChanged` and `NullabilityFlagChanged` delta kinds; iterate `Collections` and emit `CollectionRemoved` / `CollectionStableIdChanged`.

---

### H6. Schema-failure category leaks through generic `UnknownTool` fallback (catalog suggestions to schema-denied caller)
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk C Blind

`FrontComposerMcpCommandInvoker.cs:42-49` whitelist-style filter on `Category is SchemaMismatch or UnknownSchemaBaseline or UnsupportedSchemaAlgorithm or SchemaIntegrityMismatch`. Any future schema failure category not in list falls into `BuildUnknownToolStructuredContent`, exposing catalog-derived tool suggestions to a caller whose request was schema-denied (potential information leak).

**Recommended fix:** Branch on whether `Category` is "schema-related" via a marker enum or attribute, not by enum-name whitelist.

---

### H7. Oracle attack via fingerprint-header parsing: `MalformedRequest` only on catalog-hit
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk C Edge

`FrontComposerMcpToolAdmissionService.cs:84-101`: schema gate (which reads `ClientFingerprintHint`) wrapped in `try/catch` only fires on the exact-name-hit branch. When the requested name is not in the catalog, the suggestion path returns without consulting `ClientFingerprintHint`. A client with a malformed `x-frontcomposer-schema-fingerprint` header gets `UnknownTool` + suggestion when probing a misspelled name, but `MalformedRequest` when landing on a real name. **Lets clients fingerprint catalog membership by header behavior — oracle.**

**Recommended fix:** Read and validate `ClientFingerprintHint` at the very top of `ResolveAsync`, before catalog lookup, so malformed headers fail uniformly regardless of name match.

---

### H8. `McpLifecycleResult` PascalCase wire shape vs lowercase fingerprint catalog (potentially silent wire-contract change)
**Severity:** High · **Fresh:** Yes · **Sources:** Chunk C Blind

`McpLifecycleModels.cs:16-20` defines `record McpLifecycleResult(string Category, string CorrelationId, string MessageId, string State)` (PascalCase). The fingerprint catalog at `SchemaFingerprintTransform.cs:596-601` was updated to PascalCase. **If** `McpLifecycleResult` is serialized via default `JsonSerializer` (no `[JsonPropertyName("category")]` overrides), the wire output is PascalCase. Old clients consuming `lowerCamel` payloads would break.

**Action:** Verify the actual wire serializer config (look for `JsonNamingPolicy.CamelCase` or per-property `[JsonPropertyName]`). If serializer is unconfigured / uses `JsonSerializerDefaults.Web`, this is silent breaking change. **Verify before merge.**

---

## Medium (defer with owner; harden in v1.x)

### M1. Per-manifest validation loop ignores cross-manifest invariants (Chunk B Blind)
`FrontComposerMcpDescriptorRegistry.cs:88-104, 184` — recomputes per manifest with empty corpus; no shared canonical contract assertion between build-time and runtime aggregators. Defer with **owner = D6 follow-up**.

### M2. Aggregator throws `SchemaIntegrityMismatch` when only some descriptors carry fingerprints
`FrontComposerMcpRuntimeManifestAggregator.cs:15-17`. Single legacy command/resource crashes registry construction at host startup. **No migration path.** Defer with **owner = legacy-manifest-migration**.

### M3. Aggregator ordering excludes manifest-level identity (Chunk B Blind)
`FrontComposerMcpRuntimeManifestAggregator.cs:331-340` — fields ordered by `(AlgorithmId, Value)` only; two distinct manifests with same nested set produce identical aggregate fingerprints. Weakens AC8 "runtime aggregate" identity.

### M4. Multi-valued `x-frontcomposer-schema-fingerprint` header not tested (Chunk D Edge)
`HttpFrontComposerMcpAgentContextAccessor.cs:53-55` rejects multi-valued, but no parallel `MultipleHeaderValues` test like the API-key path. Add test.

### M5. Determinism matrix omits TZ and path-separator dimensions (Chunk D Auditor + Chunk D Edge)
`SchemaFingerprintDeterminismTests.cs:17-21` covers culture (4) and EOL (5). Class doc claims AC11 "OS / culture / TZ / EOL / path-separator" — **3 of 5 dimensions unverified.** Add `TimeZoneInfo` swap and path-separator coverage.

### M6. `ToolAdmissionService` doesn't revalidate schema between catalog build and acceptance — descriptor mutation race (Chunk C Edge)
Hot-reload scenarios produce stale gate decisions. Either deep-copy descriptors at admission or document that registry must be immutable.

### M7. Pre-render schema revalidation can mask transient baseline-store failure as `SchemaMismatch` (Chunk C Edge)
If baseline provider Scoped scope tears down between pre-query and post-query gate calls, `ObjectDisposedException` swallow → `HasTrustedBaseline` differs between calls → in-flight read flips. Document Scoped lifetime contract or extend `try/catch ObjectDisposedException` to all gate evaluations.

### M8. `SchemaMaterialValidationException` from snapshot construction not categorized (Chunk B Edge)
`SchemaNegotiationRuntimeGate.cs:199-256` wraps no try/catch around `Snapshot(...)`. `CanonicalSchemaMaterial.CreatePayload` throws on duplicate field names → falls through to `DownstreamFailed` with `retryable=true` instead of structural `SchemaIntegrityMismatch` with `retryable=false`. Indefinite retry loop on structural defect.

### M9. DI lifetime contract on `ISkillCorpusFingerprintProvider` undocumented
`FrontComposerMcpServiceCollectionExtensions.cs:31` makes `FrontComposerMcpDescriptorRegistry` singleton. With ASP.NET Core's `validateScopes=true`, resolving Scoped/Transient corpus provider crashes at `BuildServiceProvider`. Constraint enforced only by code review.

---

## Low (track or dismiss)

- **L1.** `hashesMatch` Exact short-circuit fires when `snapshotDecision` is null due to `ObjectDisposedException` swallow — combined with C3 unreachable in practice. (Chunk B Blind)
- **L2.** `SupportedAlgorithms` set drift between accessor and negotiator — invariant enforced only by code review. (Chunk B Blind)
- **L3.** `projection_unavailable` (snake_case) breaks kebab-case convention. (Chunk B Blind)
- **L4.** No null-check on `descriptor.Fields` / `descriptor.Parameters` — inconsistent with `DerivablePropertyNames` defensive policy. (Chunk B Blind)
- **L5.** `MapSchemaFailureStrict` `ArgumentException` message embeds category enum value — caller leakage if outer logger captures `ex.Message`. (Chunk B Blind, Chunk B Edge, Chunk C Edge)
- **L6.** `InMemorySchemaBaselineProvider.IsSafeIdentifier` accepts `..` segments — moot today (in-memory dictionary), live risk if filesystem path constructed from `packageOwner`. (Chunk B Edge, Chunk D Edge)
- **L7.** `BuildRenderContracts` always forces `text/markdown`/`McpMarkdown`, ignoring `RenderStrategy`. (Chunk B Blind — overlaps Group A deferred render-contract semantics)
- **L8.** Cancellation not honored before entry-level schema gate or admission's schema gate. (Chunk B Edge, Chunk C Edge)
- **L9.** `Normalize` adds U+2028/U+2029 substitution but misses U+0085 (NEL). (Chunk C Edge)
- **L10.** Telemetry `result.Kind.ToString()` not bounded to allow-list — future enum value propagates verbatim. (Chunk B Edge)
- **L11.** Empty-aggregate canonical hash collision across distinct empty configurations. (Chunk B Edge)
- **L12.** Test self-fulfillment / vacuity — `Resolver_TypeExists`, `LifecycleCatalog_StateEnumValues_PinnedToCanonicalSet` self-anchored, `Negotiate_TrustingCallerBool_DoesNotOverrideAnalyzerDecision` reflection ctor may not flow bool, `LifecyclePayload_FingerprintIsStable_AcrossInvocations` `ShouldBeOneOf` two algorithms gives free pick. (Chunk D Blind)
- **L13.** `surface-metadata-only-renderer.json` internal contradiction: `expectedDeltaCategory:"MetadataChanged"` + `expectedNegotiationKind:"Exact"` (Chunk D Blind). Verify intent and either drop one or document the meaning.
- **L14.** Fixtures are metadata stubs (4-key descriptors), not runnable test vectors — `SchemaFixtureCatalogTests` only verifies key presence, not that fixtures *produce* claimed `expectedFingerprintAlgorithm` / `expectedNegotiationKind`. **AC10 weak coverage.** (Chunk D Blind, Chunk D Auditor — partially overlapping)
- **L15.** `EachFixture_DocumentsExpectedFingerprintMaterial` misses `expectedFingerprintAlgorithm` and `expectedDeltaCategory` assertions — AC10 names "algorithm id" and "delta category" explicitly. (Chunk D Auditor)

---

## Dismissed / duplicate of A/B/C

- **`maxDeltaCount=1` with marker overflows budget** — Group C deferred ("harden floor to 2"); Chunk A Blind/Edge re-flagged with same shape.
- **`TruncatePath` exceeds `MaxPathLength` by 2 chars when surrogate-adjusted** — Group C dismissed as cosmetic.
- **`TruncatePath` lone low surrogate at boundary** — Group C deferred.
- **`HasTrustedBaseline` flips true when `descriptor.Fingerprint is not null`** — D7 binding decision (build-pipeline trust signal); Chunk B Blind concern is dismissed.
- **`PublicationOnly` Lazy concurrent execution** — Group A applied; Chunk B Blind concern is dismissed.
- **`EvaluateCommand` invoked twice per request** — Group A verified defensive null-coalesce; Chunk C Blind reframes as scope-mutation race (M6 keeps the fresh angle).
- **Schema gate fail-open when client omits header** — D2 explicit design.
- **HTTP separator off-by-one and surrogate-pair edges** — Group A dismissed.
- **`Reject(name, suggestion, catalog)` drops category** — Group A verified two distinct overloads.
- **`SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs` deleted with no replacement** (Chunk C Blind) — false positive due to chunk isolation; replacement is in Schema project (chunk A).
- **Aggregator partial-fingerprint bypass when no manifest claims fingerprint** — Group A deferred.
- **`McpLifecycleResult` model added but unused** — Group A noted; consumed by SourceTools T6 elsewhere.

---

## Verification status

- ✅ `hashesMatch` AlgorithmId omission verified (`SchemaNegotiation.cs:150`)
- ✅ Server hex emit verified (`SchemaFingerprintContracts.cs:165, 331-339`)
- ✅ Client base64 storage verified (`HttpFrontComposerMcpAgentContextAccessor.cs:147`)
- ✅ SourceTools `Sha256SourceToolsBlobV1` for aggregate manifest verified (`SchemaFingerprintTransform.cs:42, 196, 255` + `McpManifestEmitter.cs:48`)
- ✅ H7 hardcoded `Sha256CanonicalJsonV1` requirement verified (`FrontComposerMcpDescriptorRegistry.cs:180`)
- ✅ `corpusFingerprints` discard verified (`FrontComposerMcpDescriptorRegistry.cs:171, 184`)
- ✅ `InMemorySchemaBaselineProvider` placeholder content verified (`InMemorySchemaBaselineProvider.cs:64`)
- ✅ No test exercises SourceTools-generated manifest path (no references to `FrontComposerMcpGeneratedManifest`, `[GeneratedManifest]`, `ManifestAssemblies` in `Mcp.Tests`)
- ⚠️ `RequiresMigrationGuide` polarity reviewed: Chunk A Blind's "inverted" claim is **false positive** — the field reads as "is a guide required for this baseline?" and `!RequiresMigrationGuide` correctly fires when no guide is owned/declared.
- ⚠️ `McpLifecycleResult` JSON serializer config NOT verified — H8 needs spot-check before merge.

---

## Recommended actions

1. **Block merge** until C1, C2, C4 have a path forward (decide patch vs defer-with-spec-update).
2. C3 needs encoding alignment OR explicit acknowledgment that base64 wire never touches hex storage (rework comparison to byte-decode).
3. C5 — disable default `InMemorySchemaBaselineProvider` registration OR have it return `false` from `TryResolve` until D3.
4. H1, H2, H3, H4, H5, H6, H7 — apply patches in this story; small surface area.
5. H8 — verify wire serializer config; if PascalCase is silent breaking change, add `[JsonPropertyName]` overrides.
6. Medium and Low buckets — ticket and triage to v1.x; do not gate merge.
