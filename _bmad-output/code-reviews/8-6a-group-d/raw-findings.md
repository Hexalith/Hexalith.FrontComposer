# Story 8-6a Group D — raw findings (pre-triage)

## Chunk A — Blind Hunter findings

- **Truncation logic over-trims when no migration-guide marker is present, dropping a real delta** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:160`
  When `migrationGuideMarker` is null, `markerSlot = 0`, code keeps `Math.Max(0, maxDeltaCount - 1 - 0) = maxDeltaCount - 1` ordered deltas, then appends `Truncated` marker for total `maxDeltaCount`. One slot smaller than necessary — callers asking for N deltas only get N-1 real findings plus the marker.

- **`maxDeltaCount = 1` produces a negative `Take` argument and an empty payload** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:160`
  With `maxDeltaCount = 1` and a migration-guide marker present, `Math.Max(0, 1 - 1 - 1) = 0`. All real deltas discarded, only marker + `Truncated` remain (2 > 1). With no marker, `Take(0)` drops every Breaking finding entirely. (Note: Group C deferred this very boundary — "harden floor to 2"; partial duplicate.)

- **`MissingMigrationGuide` semantics inverted: `RequiresMigrationGuide=true` triggers the missing-guide delta** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:134`
  Condition is `!baseline.Provenance.RequiresMigrationGuide`. Comment says delta should fire when no migration guide is declared, but property name reads as "this baseline requires a guide" not "has a guide". Schemas that *require* a guide skip the marker, schemas that don't always fail closed — likely wrong polarity. **NEEDS NAMING/SEMANTIC VERIFICATION.**

- **Aggregate decision recomputed AFTER migration-guide delta added, contradicting P-10 comment** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:143`
  Comment at line 118 says aggregate must reflect FULL pre-truncation set, but `aggregate` is computed only after appending synthetic `MissingMigrationGuide` row. Practical effect benign (synthetic delta is also Breaking), but invariant promised by comment is not what code does.

- **`DictionaryEqual` uses `KeyValuePair<string,string>.Equals` not Ordinal** — LOW — `SchemaMigrationDeltaAnalyzer.cs:255`
  `EqualityComparer<KeyValuePair<string,string>>.Default` falls back to KeyValuePair's default equality, not guaranteed Ordinal. Pairwise ordinal comparison safer.

- **Surrogate-pair guard is incomplete: low surrogate at `cut` left dangling** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:274`
  Check looks at `path[cut - 1]` (high surrogate immediately before cut). When `path[cut - 1]` is a *low* surrogate, that's a malformed UTF-16 input; `IsHighSurrogate` returns false, dangling low surrogate kept. (Note: Group C deferred this — "TruncatePath does not normalize lone low surrogates".)

- **Bounds-key allowlist uses a `HashSet` but treated as prefix-only** — LOW — `SchemaMigrationDeltaAnalyzer.cs:211`
  `BoundsKeyPrefixes` is a `HashSet<string>` with only `"bounds."`, iterated as prefix list. Single `StartsWith` wrapped in O(n) — and bare `"bounds."` would match. Cosmetic.

- **`CanonicalizerVersion`/`TestVectorId` mismatch returns `Unknown` with `truncated=false`** — LOW — `SchemaMigrationDeltaAnalyzer.cs:54`
  Naive consumers may assume empty field list is authoritative. Synthetic `CanonicalizerUnsupported` delta is the only signal.

- **`SequenceEqual` materializes both ordered enumerations every call** — LOW — `SchemaMigrationDeltaAnalyzer.cs:248`
  Per-field `OrderBy` of `EnumValues`. Cold path on build, fine. Already covered by Group B deferred ("perf scan over un-truncated input").

- **`Exact` short-circuit doesn't catch provenance drift (e.g. `RequiresMigrationGuide` flip)** — LOW — `SchemaMigrationDeltaAnalyzer.cs:59`
  If two snapshots share canonicalizer version + algorithm + fingerprint but differ in provenance, analyzer reports `Exact` and never surfaces. **Possibly intended.**

## Chunk A — Acceptance Auditor findings

**No fresh AC violations in chunk A; diff is faithful to spec.**

Verified:
- AC6: snapshot-based `Compare(SchemaBaselineSnapshot, SchemaBaselineSnapshot, …)` — no `HasCompatibleAdditiveDrift` bool path.
- AC12: `preliminaryAggregate` (line 123) computed from FULL pre-truncation set; truncation marker carries FULL aggregate (line 161).
- D9: `<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>` per Contracts pattern.
- D23: `SupportedAlgorithms` set holds exactly `Sha256CanonicalJsonV1` + `Sha256SourceToolsBlobV1`.
- P-43: `MissingMigrationGuide` appended BEFORE truncation; consumes reserved slot.
- P-44: empty-delta + hash-mismatch returns `Unknown` via `ComputeAggregate` (lines 174-181).
- P-45: surrogate-pair-safe `TruncatePath` at lines 264-279.
- P-46: `SchemaContractFamilyNames.Canonical` covers all 7 enum members; throws on undefined.

## Chunk B — Blind Hunter findings

- **`hashesMatch` ignores AlgorithmId, enabling cross-algorithm collision short-circuit** — HIGH — `SchemaNegotiation.cs:525`
  `string.Equals(input.ClientFingerprint.Value, input.ServerFingerprint.Value, …)` compares only encoded value. Two fingerprints from different algorithms sharing the same base64 string would byte-match and trip `Exact` on line 568. The accessor only guarantees client algorithm is in supported set, not that it matches server's. **VERIFY.**

- **Snapshot algorithm divergence silently accepted** — HIGH — `SchemaNegotiationRuntimeGate.cs:642-650`
  Comment acknowledges descriptor and server-snapshot fingerprints can use different algorithms. Combined with `hashesMatch` flaw above, hostile/buggy client computing same byte-string under weaker algorithm hits `Exact` short-circuit.

- **`HasTrustedBaseline` flips true when `descriptor.Fingerprint is not null`** — MEDIUM — `SchemaNegotiationRuntimeGate.cs:656,682`
  `HasTrustedBaseline: baseline is not null || descriptor.Fingerprint is not null` lets every descriptor with build-time fingerprint claim trusted baseline even when `ISchemaBaselineProvider` resolved nothing. **Note: D7 binding decision in prior context — descriptor.Fingerprint IS a trust signal in production. Likely false positive.**

- **`hashesMatch` Exact short-circuit fires when snapshot decision is `null` due to provider failure** — MEDIUM — `SchemaNegotiation.cs:568`
  Branch `if (hashesMatch && snapshotDecision is null)` reached when Baseline or Server is null — including `ObjectDisposedException` swallow (gate.cs:810-812). Disposed-scope failure becomes silent acceptance of hash match.

- **Aggregator throws `SchemaIntegrityMismatch` when only some descriptors carry fingerprints** — MEDIUM — `FrontComposerMcpRuntimeManifestAggregator.cs:312-315`
  `nestedFingerprints.Any(null) && nestedFingerprints.Any(not null)` fails closed if any single descriptor lacks fingerprint while siblings have one. Single legacy command/resource crashes registry construction at host startup. (Note: prior deferred — "Aggregator partial-fingerprint bypass when no manifest claims fingerprint" — different shape.)

- **Per-manifest validation loop ignores cross-manifest invariants** — MEDIUM — `FrontComposerMcpDescriptorRegistry.cs:88-104`
  `ValidateAggregateIntegrity` recomputes per manifest passing `[manifest]` plus empty corpus. No shared canonical contract between build-time and runtime aggregators is asserted — only hope they match byte-for-byte.

- **Ordering in aggregator excludes manifest-level identity** — MEDIUM — `FrontComposerMcpRuntimeManifestAggregator.cs:331-340`
  Fields ordered by `(AlgorithmId, Value)` only — no manifest name/version/position. Two distinct manifests with same nested set produce identical aggregate fingerprints, weakening AC8 "runtime aggregate" identity.

- **`SupportedAlgorithms` set drift between accessor and negotiator** — MEDIUM — `HttpFrontComposerMcpAgentContextAccessor.cs:181-184`
  Accessor pins its own copy with comment "must update both sides together." Runtime invariant enforced only by code review; no shared constant in diff.

- **`projection_unavailable` (snake_case) breaks kebab-case convention** — LOW — `SchemaNegotiation.cs:512`
  Other agentCategory strings: `"schema-mismatch"`, `"schema-unavailable"`, `"unsupported-schema-fingerprint"`, `"schema-exact"`. Inconsistent surface for client parsing.

- **No null-check on `descriptor.Fields` / `descriptor.Parameters`** — LOW — `SchemaNegotiationRuntimeGate.cs:824, 847`
  Will NRE on null collections, despite defensive treatment of `DerivablePropertyNames`. Inconsistent defensive policy on same descriptor source.

- **`MapSchemaFailureStrict` `ArgumentException` message embeds category enum value** — LOW — `SchemaNegotiationRuntimeGate.cs:715-723`
  `$"Category {category} is not a schema-failure category."` — if exception bubbles to generic logger, category enum name exposed in error logs.

- **`PublicationOnly` Lazy can produce duplicate snapshots** — LOW — `InMemorySchemaBaselineProvider.cs:403-405`
  `LazyThreadSafetyMode.PublicationOnly` allows multiple concurrent threads to invoke `BuildSnapshots`; deterministic so currently fine. (Already deferred-applied per Group A, false positive.)

- **`InMemorySchemaBaselineProvider` validates arbitrary `packageOwner` but only stores `"Hexalith.FrontComposer"`** — LOW — `InMemorySchemaBaselineProvider.cs:399-428`
  Wasted validation surface; subtle "you can pass anything but it'll just return false" trap.

- **`BuildRenderContracts` always forces `text/markdown`/`McpMarkdown`, ignoring `RenderStrategy`** — LOW — `FrontComposerMcpDescriptorRegistry.cs:113,128,131-134`
  Every resource gets `MarkdownRendererContract` regardless of strategy. (Note: prior Group A deferred "Render bounds wrong member / frozen at startup / empty Fields" — overlapping.)

- **`CompatibleWarning` produced but no consumer in diff inspects new kind** — LOW — `SchemaNegotiation.cs:490, 581-589`
  Surface inconsistency: consumers branching on `Exact || CompatibleAdditive` for "can proceed" may silently include or exclude warning case.

- **Header parser rejects algorithm IDs containing `:`** — LOW — `HttpFrontComposerMcpAgentContextAccessor.cs:233`
  Future algorithm name like `sha256:canonical-v2` would be unparseable. Cosmetic future-proofing.

- **`derivable.Contains(name, OrdinalIgnoreCase)` over collection is O(N×M)** — LOW — `SchemaNegotiationRuntimeGate.cs:838-849`
  Plus full-width Unicode equivalents not normalized. Cosmetic.

- **Cached parse memoizes only successful parses; malformed throws repeatedly** — LOW — `HttpFrontComposerMcpAgentContextAccessor.cs:201-211`
  Memo claim "avoids re-throwing" is partially false.

## Chunk C — Blind Hunter findings

- **Schema gate evaluated twice for tools with second evaluation racing the first** — HIGH — `FrontComposerMcpCommandInvoker.cs:56-60` + `FrontComposerMcpToolAdmissionService.cs:82-99`
  Between admission and invocation, agent context (tenant, schema header) could change in scoped DI; second call may reach different verdict for same descriptor. (Note: prior Group A "EvaluateCommand invoked twice per request" verified-defensive-only. Group D differentiator: framing as scope-mutation race, not duplicate work.)

- **`Accept` accepts schema result that already declared `!AllowsSideEffects` re-validation hazard** — HIGH — `FrontComposerMcpToolAdmissionService.cs:95-99`
  When `AllowsSideEffects=true && FailureCategory!=None` (compatible-warning), propagated unchecked; invoker `??` short-circuits, skipping `EvaluateCommand`. No re-validation on `CompatibleAdditive`/warning states. **Conflicts with AC5 / D20.**

- **Schema-failure category leaks through generic `UnknownTool` fallback** — HIGH — `FrontComposerMcpCommandInvoker.cs:42-49`
  Whitelist-style `Category is Schema*` check. Any future schema failure category not in list falls into `BuildUnknownToolStructuredContent`, exposing catalog-derived suggestions to a schema-denied caller.

- **`McpLifecycleResult` record fields PascalCase while ToJson contracts use lowercase** — HIGH — `McpLifecycleModels.cs:16-20` + `SchemaFingerprintTransform.cs:596-601`
  "Fix" changes fingerprint catalog from lowercase to uppercase. If record uses default `JsonSerializer`, wire output is PascalCase — silent wire-contract change. **MUST VERIFY against actual ToJson/serializer attrs.**

- **`SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs` deleted with no replacement in chunk** — HIGH — chunk-c lines 306-547
  File deleted entirely. **NOTE: replacement is in Schema project (chunk A) — Group B "Schema project extraction" handled this.** False positive given chunk-isolation.

- **`FrontComposerMcpException` schema branch swallows cancellation when filter throws** — MEDIUM — `FrontComposerMcpCommandInvoker.cs:139-149`
  Schema-error returned on a cancelled call breaks "cancellation always wins" contract.

- **`SchemaNegotiationRuntimeGate.EvaluateCommand` invoked synchronously with no `cancellationToken`** — MEDIUM — `FrontComposerMcpCommandInvoker.cs:59`, ToolAdmission:84, ProjectionReader:61
  Schema-gate work triggers fingerprint/canonical-JSON computation; cancelled tool call still pays full schema-evaluation cost.

- **`UnsupportedSchemaAlgorithm` mapped to wrong slug; collision with `SchemaIntegrityMismatch` and `UnknownSchemaBaseline`** — LOW — `FrontComposerMcpProjectionFailureMapper.cs:144-149,151,137-142`
  Two different categories with same client-visible slug make client-side branching ambiguous. **Conflicts with AC1 (sanitized agent categories).**

- **`Normalize(value)` swallows `null` to empty — silent fingerprint coercion** — MEDIUM — `SchemaFingerprintTransform.cs:615`
  Null vs empty produce identical fingerprint contribution. Two distinct contracts hash identically.

- **`FrontComposerMcpToolAdmissionService.ResolveAsync` exception filter narrow; non-schema misattribution** — MEDIUM — `FrontComposerMcpToolAdmissionService.cs:87-93`
  Non-schema `FrontComposerMcpException` from gate evaluation bubbles past admission catch, caught by invoker as "command invocation failed". Telemetry/correlation IDs point at wrong stage.

- **DI scope mismatch: `ISchemaBaselineProvider` Scoped vs `IFrontComposerMcpProjectionRenderer` Singleton** — LOW — `FrontComposerMcpServiceCollectionExtensions.cs:17,18`
  If renderer captures provider, scoping mismatch leaks first-request tenant baseline into singleton cache. **Should verify renderer doesn't capture provider.**

- **Projection reader runs schema gate THREE times for same descriptor on read path** — MEDIUM — `FrontComposerMcpProjectionReader.cs:61-65, 66, 200`
  Two synchronous calls on same descriptor + agent context within microseconds. With scoped provider state mutation, two evaluations CAN return different verdicts. **VERIFY count claim.**

## Chunk A — Edge Case Hunter findings

- **Null `Provenance`/`Fingerprint`/`Document` on `SchemaBaselineSnapshot` produces NRE** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:43-65`
  `baseline.Provenance.FingerprintAlgorithm`, `baseline.Fingerprint.Value`, `baseline.Document.ProtocolIdentifier` dereferenced after only checking `baseline is null` / `current is null`. Positional record with no per-property null guard. Trimmed/partial baseline file deserialised crashes negotiator instead of producing typed `UnsupportedAlgorithm`.

- **`Document.Fields` with duplicate `Name` throws `ArgumentException`, undermining fail-closed** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:68-69`
  `ToDictionary(f => f.Name, StringComparer.Ordinal)` throws on duplicate. `Compare` accepts arbitrary `SchemaBaselineSnapshot` instances and never re-runs `ValidateDocument`. Exception escapes to `Negotiate` which has no try/catch — converts known-malformed-baseline case into unhandled-exception 500 instead of `Unknown → Incompatible`.

- **`Document.Fields`/`Metadata`/`Collections` null on snapshot crashes the analyzer** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:68-116`
  `SchemaContractDocument` is positional record with `IReadOnlyList`/`IReadOnlyDictionary` properties but no init-time null check. Stripped baseline `{ "Fields": null }` deserialises with default JSON options into snapshot whose `Fields` is null.

- **Field-level `IsRequired` / `IsNullable` flips silently classified as non-breaking** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:90-103`
  When field exists in both, only `JsonType`, `TypeName`, `EnumValues`, `ValidationConstraints` compared. `IsRequired:false → true`, `IsNullable:true → false`, or `Title`/`Description`/per-field `Metadata` produces zero deltas. Result via canonicalizer hash diff falls through to `Unknown` (P-44) without actionable diagnostic. **Specifies that breaking changes lack discriminative deltas.**

- **`Document.Collections` changes produce no delta at all** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:64-117`
  Analyzer never iterates `baseline.Document.Collections` or `current.Document.Collections`. Renaming, removing, or changing `StableIdField` produces no delta. Hash difference detected → empty deltas → aggregate `Unknown` → consumers get generic "Unknown → Incompatible" instead of actionable "collection-removed" diagnostic.

- **`maxDeltaCount=1` with `MissingMigrationGuide` marker overflows budget** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:142-170`
  Reserves slot for marker via `markerSlot=1`, then adds `Truncated` marker unconditionally → final delta count = 2. P-11 documents invariant that truncation marker stays "WITHIN the maxDeltaCount budget." (Note: Group C deferred similar floor concern — but this specific overflow with marker is fresh.)

- **`netstandard2.0` target uses reflection-based `KeyValuePair<>` equality** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:250-255`
  `KeyValuePair<TKey,TValue>` does not override `Equals`/`GetHashCode` on `netstandard2.0`. Default comparer boxes both KVPs and uses `ValueType.Equals` — reflection-based, allocation-heavy, can produce reference-vs-value comparison surprises. Two semantically identical `ValidationConstraints` may compare unequal → spurious `ValidationConstraintChanged` delta.

- **`ProtocolIdentifier` null-vs-empty-vs-`<absent>` produces spurious `Breaking` delta** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:64-66`
  Compared raw without `CanonicalSchemaMaterial.NormalizeOptional`. Canonical pipeline normalises empty strings to `AbsentValueSentinel = "<absent>"`. Round-tripped baseline (`"<absent>"`) compared to fresh runtime snapshot (null) → `ProtocolIdentifierChanged` Breaking delta blocks deploy.

- **Field `Name` containing `.` collides with delta `Path` scheme** — LOW — `SchemaMigrationDeltaAnalyzer.cs:73,85,93,97,101`
  Paths built via concat `"$.Fields." + name`. Two contracts where one has `"a.b"` and other has `"a"` with sub-property `"b"` produce colliding paths. Downstream telemetry mis-aggregates.

- **Whitespace-only `FingerprintAlgorithm`/`CanonicalizerVersion` bypasses supported-set check** — LOW — `SchemaMigrationDeltaAnalyzer.cs:43-57`
  `SupportedAlgorithms.Contains(... ?? string.Empty)` handles null but not whitespace. `CanonicalizerVersion` whitespace-only on both sides yields equality, skipping `CanonicalizerUnsupported`. `Compare` accepts ungated snapshots.

- **`TruncatePath` exceeds `MaxPathLength` by 2 chars** — LOW — `SchemaMigrationDeltaAnalyzer.cs:264-279`
  When `cut-1` is high surrogate, `cut--`, then append `"..."` → final length `MaxPathLength + 2` (255 + 3 = 258), exceeds `MaxPathLength=256`. P-9 intent violated. (Already deferred in Group C as cosmetic; specific overflow value is fresh.)

- **`TruncatePath` lone low surrogate at boundary preserves unpaired low surrogate** — LOW — `SchemaMigrationDeltaAnalyzer.cs:273-278`
  Surrogate guard only covers high surrogate at `cut-1`. Lone low surrogate at exact `MaxPathLength - 1` (malformed input but possible — `SchemaFieldContract.Name` unvalidated) returned as-is. (Note: prior Group C deferred — duplicate.)

- **`Metadata` case-only key changes treated as separate keys** — LOW — `SchemaMigrationDeltaAnalyzer.cs:105-116`
  Renaming `"Capability"` → `"capability"` → both `OldRemoved` + `NewAdded` `MetadataChanged`. The `caseVariant` check in `ValidateObjectBody` rejects this in canonical JSON, but `Compare` does not. Bloats delta budget.

- **`CompareSnapshots` propagates `ArgumentException`/`ArgumentOutOfRangeException` to MCP request handlers** — MEDIUM — `SchemaNegotiation.cs:240-247`
  Calls `Compare` with no try/catch. Any thrown exception (duplicate field name, null `Document.Fields`) escapes `Negotiate` and surfaces as unhandled MCP request handler exception, contradicting the documented fail-closed precedence model.

## Chunk B — Acceptance Auditor findings

**Two HIGH-severity violations identified.**

- **AC8 violation: Corpus fingerprints collected at registration are silently discarded** — `FrontComposerMcpDescriptorRegistry.cs:43-47, 171, 184`
  `corpusFingerprints` collected from `ISkillCorpusFingerprintProvider`s, passed into `ValidateAggregateIntegrity(manifests, corpusFingerprints)` at line 47. Inside `ValidateAggregateIntegrity` at line 171: `_ = corpusFingerprints;` (explicitly discarded). Loop calls `Compute([manifest], [])` (line 184) — corpus forced empty. No production call site computes corpus-inclusive aggregate. AC8 explicitly mandates "the runtime aggregate manifest fingerprint includes corpus resource fingerprints, and the runtime aggregate is recomputed". **Fresh from prior reviews:** prior deferred entries flag aggregator dedup/cardinality and partial-fingerprint bypass *inside the function*; this finding is at the registry call site — corpus collection is dead by construction.

- **AC7 violation: Corpus-inclusive integrity check is silently scoped out by per-manifest loop** — `FrontComposerMcpDescriptorRegistry.cs:184`
  `Compute([manifest], [])` deliberately omits corpus from integrity check. Tampered/substituted corpus resource at runtime cannot trip `SchemaIntegrityMismatch` here — only per-manifest-nested tampering can. AC7 explicitly enumerates corpus as part of the disagreement set ("aggregate manifest fingerprint and its nested command/resource/renderer/**corpus** fingerprints disagree at runtime → fails closed with `SchemaIntegrityMismatch`"). **Fresh from prior reviews:** Group A's D6 framed this as "per-manifest scope ≠ cross-manifest scope" and resolved by pruning corpus from loop, but Group A's AC analysis stops at "AC8 covered by aggregator" without observing AC7's corpus dimension is uncovered.

**Positive observations:**
- AC4 trusted baseline resolver wiring is sound — `TryResolveBaseline` only passes compile-time constants `PackageOwner = "Hexalith.FrontComposer"` and `DefaultFixtureId = "baseline-known-v1"`; no client-supplied path/traversal/external identifier flows in.
- D2 fail-open semantics correctly implemented — both `EvaluateResource` and `EvaluateCommand` return `null` when fingerprint hint absent.
- AC15 structured log payload bounded to `(category, messageKey, docsCode, decisionKind)`; no fingerprint/hidden-name/path leak.
- AC1 failure-mapper completeness solid — `MapSchemaFailureStrict` rejects non-schema categories with `ArgumentException`.

## Chunk C — Acceptance Auditor findings

**One fresh AC violation identified:**

- **Asymmetric outer-catch telemetry between command invoker (patched) and projection reader (unpatched)** — Violates **AC1** (sanitized failure category) and D4/AC15 bounded-telemetry intent — `FrontComposerMcpProjectionReader.cs:122-124`
  Diff applies structured `LogWarning(ex, …)` capturing stack/type to `FrontComposerMcpCommandInvoker` (chunk-c lines 107-118), explicitly justifying the change ("previous logging emitted only the `DownstreamFailed` token, losing the underlying signal entirely"). The structurally identical bare `catch { … }` in projection reader was untouched in this diff. Schema-category exceptions still hit the explicit `catch (FrontComposerMcpException)` arm above, but any non-FrontComposerMcpException in the projection pipeline (e.g., a future renderer/bounds bug) is coerced to `DownstreamFailed` with **zero diagnostic capture** — the failure mode the chunk's commit comment calls out as broken on the command side. Asymmetric remediation introduced by this diff. **Fresh:** prior reviews never mention projection reader's outer catch.

Initial concerns reclassified as positive observations rather than violations:
- `OperationCanceledException` ordering correctly puts cancellation handler ahead of bare-catch `DownstreamFailed` mapping.
- Schema-rejection envelope omits `messageId`/`correlationId` — intentional fail-fast (no IDs to carry pre-admission).

**Positive observations:**
- AC14 faithful: `BuildRenderContracts` registers `FrontComposerRenderContract` per Markdown projection resource via `GetRenderContracts()` with bounds from live `FrontComposerMcpOptions` (not constants).
- AC13 / D23 preserved: both algorithms remain in allow-list; SourceTools-side analyzer deletion balanced by Schema project reference.
- AC1 sanitized mapping faithful: four new branches in `FrontComposerMcpProjectionFailureMapper:78-105` + `MapSchemaFailureStrict` enforcement.

## Chunk D — Blind Hunter findings

- **`Resolver_TypeExists` test asserts a tautology** — HIGH — `SchemaBaselineResolverTests.cs:988-1003`
  `TryFindResolverType()` does `return typeof(ISchemaBaselineProvider);` — a `typeof(...)` is never null. Compile-time guarantee, then asserts what compiler proved. Vacuous theatre.

- **Resolver fallback to hand-rolled `InMemorySchemaBaselineProvider` makes test self-fulfilling** — HIGH — `SchemaBaselineResolverTests.cs:1083-1100`
  When `resolver.IsInterface`, test creates `new InMemorySchemaBaselineProvider()` and validates *that* type's behavior, not production registration. **Mocking the SUT** — proves test double behaves correctly, proves nothing about SUT.

- **`Resolver_ReturnsTypedSnapshotForKnownIdentifiers` couples to test-double seed data** — MEDIUM — `SchemaBaselineResolverTests.cs:1041-1056`
  Resolver under test is `InMemorySchemaBaselineProvider` (per above); test asserts equality on values test infrastructure populated.

- **`CompatibleAdditive_OnCommand_AdmitsDispatch_AfterRevalidation` accepts both branches as success — proves nothing** — HIGH — `CommandInvokerSchemaGateTests.cs:157-182`
  Assertion: `if (result.IsError) { assert error categories } else { assert dispatched }`. Every outcome valid. Broken implementation returning random one is also valid. **No failing case.**

- **`RevalidationCountingQueryService.RevalidationCount` increments on `Take` bound test never sets** — HIGH — `ProjectionReaderSchemaGateTests.cs:457-473, 343`
  Counter only increments when `request.Take > 0 && request.Take <= 1024`. Test never sets `Take` — relies on production reader to pick a value in range. Comment: "Until T3 lands, RevalidationCount stays 0 and this scaffold will fail meaningfully when unskipped" — test was authored knowing it will fail incidentally. Side-channel guess at unrelated parameter.

- **`HiddenPrecedence_WinsOverSchemaMismatch` substring match on serialized JSON brittle and over-broad** — MEDIUM — `ProjectionReaderSchemaGateTests.cs:323-324`
  `ToJsonString().ShouldNotContain("schema-mismatch")`. A field with literal phrase "no schema-mismatch detected" or a docs URL would also fail. Conversely misses sub-string-mangled leakage like `"schemaMismatch"` (camelCase) or `"schema_mismatch"`.

- **`Map_NewSchemaCategories_ReturnDeterministicAgentTaxonomy` Theory degenerates** — MEDIUM — `ProjectionReaderSchemaTaxonomyTests.cs:533-555`
  All four rows pin `expectedRetryable=false`, `expectedRefreshResources=false`. If `UnknownSchemaBaseline` should be `retryable=true`, test silently locks wrong behavior. Columns prove nothing per-row.

- **`SanitizedPayload_DoesNotEcho_RawClientHashOrTenantId` checks unrelated literals never used** — MEDIUM — `ProjectionReaderSchemaTaxonomyTests.cs:591-598`
  Mapper invoked only with `FrontComposerMcpFailureCategory.SchemaMismatch`. Asserting output doesn't contain `"tenant-a"`, `"eyJabc"`, etc. proves nothing — values never plumbed in.

- **`SanitizedPayload_StructuredContent_ContainsOnlyBoundedFields` one-directional** — MEDIUM — `ProjectionReaderSchemaTaxonomyTests.cs:580-587`
  Loop checks every emitted key in allow-list. Mapper emitting `{}` would pass. No "must contain category, message, docsCode" check.

- **`DescriptorRegistry_LoadingTamperedAggregate_FailsClosed_WithIntegrityMismatch` accepts any exception with "integrity" substring** — MEDIUM — `AggregateManifestIntegrityTests.cs:866-871`
  Uses `Should.Throw<Exception>(...)` and lower-cases full message looking for "integrity". Any unrelated DI bootstrap exception that happens to contain "integrity" passes.

- **`RenderContractAdapterTests` use reflection for non-existent `GetRenderContracts()` method** — MEDIUM — `RenderContractAdapterTests.cs:798-808`
  Accessor doesn't exist (per current diff state); `accessor.ShouldNotBeNull(...)` always throws. Effectively `Skip = "..."` masquerading as `[Fact]`.

- **`SchemaFingerprintReflectionTests.LifecyclePayload_FingerprintIsStable_AcrossInvocations` `ShouldBeOneOf(...)` two algorithms gives free pick** — LOW — `SchemaFingerprintReflectionTests.cs:2186-2189`
  A fingerprint payload should have ONE deterministic algorithm. Same loose assertion in `SchemaBaselineResolverTests:1053-1055` and `RenderContractAdapterTests:758-760`.

- **`LifecycleCatalog_StateEnumValues_PinnedToCanonicalSet` self-anchored** — LOW — `SchemaFingerprintCrossPackageTests.cs:1142,1186-1199`
  `ExpectedStateLine` is hand-typed; "update ExpectedStateLine" instruction makes test its own contract. (Note: prior Group C deferred similar — partial duplicate.)

- **`LifecycleCatalog_FieldTypes_MatchRuntimePropertyTypes` hard-throw fragile fallback** — LOW — `SchemaFingerprintCrossPackageTests.cs:1211-1223`
  `MapClrTypeToCatalogType` throws for any non-string CLR type; future `DateTimeOffset` addition fails build with `InvalidOperationException` — not typed test failure with actionable cause.

- **`Negotiate_TrustingCallerBool_DoesNotOverrideAnalyzerDecision` reflection-built ctor may not flow bool** — MEDIUM — `SchemaNegotiationSnapshotInputTests.cs:1579-1596,1646-1655`
  Reflection ctor sets `additive` bool only if parameter name contains "additive". If T2 renames/removes parameter, bool silently never set; test passes regardless of whether "bool was ignored" or "bool was never plumbed". Vacuous when ctor parameters change.

- **`surface-metadata-only-renderer.json` internal contradiction** — MEDIUM — `Fixtures/surface-metadata-only-renderer.json`
  `"expectedDeltaCategory":"MetadataChanged"` while `"expectedNegotiationKind":"Exact"`. `Exact` means fingerprints byte-match → no delta to categorize. Structurally inconsistent. `SchemaFixtureCatalogTests` does not validate internal consistency.

- **`SchemaFixtureCatalogTests.Catalog_ShipsExactlyTheStoryT8Set` self-confirming list** — LOW — `SchemaFixtureCatalogTests.cs:1970-1989`
  `ExpectedFixtureIds` hand-typed string list inside test. Spec adds 10th fixture and author updates only test — no test catches drift.

- **Fixtures are metadata stubs, not runnable test vectors** — MEDIUM — `Fixtures/*.json`
  Each fixture is ~4-key descriptor (`fixtureId`, `title`, `contractFamily`, `expectedNegotiationKind`). No actual schema fields, no canonicalizable payload, no fingerprint value. `SchemaFixtureCatalogTests` does NOT verify fixtures *produce* claimed `expectedFingerprintAlgorithm` or `expectedNegotiationKind` — only that keys exist. **Fixtures are manifest-of-claims, not runnable test vectors. Conflicts with AC10.**

- **`ClientFingerprintHint_RejectsUnsupportedAlgorithm` payload over all-zero 32-byte buffer** — LOW — `AuthContextAccessorTests.cs:51-63`
  `byte[] hashBytes = new byte[32]` never populated — base64 is constant `"AAAA…"` (a known bound, all zeros). If parser has special case for "empty/zero hash", test exercises that path not "unsupported algorithm".

- **`SchemaNegotiationPrecedenceMatrixTests` magic-string leakage check** — LOW — `SchemaNegotiationPrecedenceMatrixTests.cs:1246-1252,1397-1402`
  Leakage guard `result.AgentCategory.ShouldNotContain(Server.Value)` checks for literal `"aaaa…"`. Real implementation leaking actual hex hash (different byte pattern) evades substring check.

- **`Negotiate_DerivesCompatibleAdditive_FromAnalyzerNotFromCallerBool` builds identical fingerprints** — MEDIUM — `SchemaNegotiationSnapshotInputTests.cs:1628-1659`
  Helper assigns `clientFingerprint=current.Fingerprint` and `serverFingerprint=current.Fingerprint` (identical) → negotiator likely returns `Exact`, not `CompatibleAdditive`. Test contract conflicts with sibling precedence-matrix test.

- **`SchemaFingerprintDeterminismTests.LifecyclePayload_FingerprintIdenticalAcrossCultures` non-isolated culture mutation** — MEDIUM — `SchemaFingerprintDeterminismTests.cs:2080-2106`
  Mutates `CultureInfo.CurrentCulture`/`CurrentUICulture`. If line 2089 throws, neither `try`/`finally` restore happens. State leak risk under failure. (Note: AsyncLocal in modern .NET, but xUnit parallel theory rows on shared thread pool can leak.)

## Chunk D — Acceptance Auditor findings

- **AC10 metadata-presence test misses spec-required `expectedFingerprintAlgorithm` and `expectedDeltaCategory` fields** — `SchemaFixtureCatalogTests.cs:45-50`
  `EachFixture_DocumentsExpectedFingerprintMaterial` only asserts `fixtureId`, `title`, `contractFamily`, `expectedNegotiationKind`, `expectedAgentCategory`, `notes`. The 9 fixture JSONs all carry `expectedFingerprintAlgorithm` and `expectedDeltaCategory` but a regression dropping either key would pass silently. **AC10 names "algorithm id" and "delta category" as explicit requirements.** Fresh — prior reviews patched fixture *content* but no prior triage covers `EachFixture_*` test's missing assertions on these two fields.

- **AC11 determinism matrix omits TZ and path-separator dimensions** — `SchemaFingerprintDeterminismTests.cs:17-21,50-55`
  Theory inputs vary culture (`en-US`, `tr-TR`, `de-DE`, `ja-JP`) and EOL (`\n`, `\r\n`, `\r`, ` `, ` `). **No `TimeZoneInfo` swap and no path-separator (`/` vs `\`) coverage.** Class-doc summary repeats AC11 full matrix verbatim, claiming coverage tests don't deliver.

- **AC5 `RevalidationCount` assertion vacuously satisfied** — `ProjectionReaderSchemaGateTests.cs:80,194-206`
  Asserts `query.RevalidationCount.ShouldBeGreaterThanOrEqualTo(1)`. Production reader at `FrontComposerMcpProjectionReader.cs:74-78` always sets `Take = Math.Max(1, Math.Min(DefaultResourceTake, MaxResourceTake))` for every code path → counter increments unconditionally. Test name `CompatibleAdditive_AdmitsDispatch_AfterRevalidation` claims AC5 verification but body verifies only "request was dispatched at all." **Distinct from prior D5 deferral**: D5 mandated `Skip = "AC5: revalidation pending follow-up"` marker; no `Skip` attribute is present anywhere in chunk D — deferral was not honored, test is live and falsely green.

**Positive observations:**
- All 9 spec-named fixtures exist on disk; `Catalog_ShipsExactlyTheStoryT8Set` correctly enforces the named set.
- AC12 `Compare_BreakingDeltaPastIndex25_StillProducesBreakingAggregate` verifies the contract against the analyzer.
- AC16 build verified clean (`TreatWarningsAsErrors=true` 0/0); no `Skip = "RED-PHASE: …"` markers remain.
- AC3 precedence matrix correctly stacks 9+ rows with three explicit leakage guards.

## Chunk C — Edge Case Hunter findings

- **Schema gate evaluated TWICE per resource read on happy path** — MEDIUM — `ProjectionReader.cs:61, 67, 93, 202`
  Three canonicalize-and-hash calls per successful read. Entry-level pass at line 61 fully redundant with first pass inside `ValidateSnapshotAsync` at line 67 (no descriptor mutation between).

- **Pre-render schema revalidation can mask transient baseline-store failure as `SchemaMismatch`** — MEDIUM — `ProjectionReader.cs:93` + `SchemaNegotiationRuntimeGate.cs:185-196`
  If `ISchemaBaselineProvider` is Scoped and request scope tears down between pre-query and post-query passes, `TryResolveBaseline` swallows `ObjectDisposedException` → returns null. `HasTrustedBaseline` differs between calls → in-flight read flips from accepted to rejected mid-way.

- **`MapSchemaFailureStrict` blows up on `UnsupportedSchema` (UnknownClientVersion)** — MEDIUM — `SchemaNegotiationRuntimeGate.cs:106-107` + `SchemaNegotiation.cs:127`
  Negotiator returns `FrontComposerMcpFailureCategory.UnsupportedSchema` for `UnknownClientVersion` arm. Strict mapper rejects this category with `ArgumentException`. CommandInvoker line 62 calls `ToStructuredFailure(schema.FailureCategory)` unconditionally → `ArgumentException` collapses to `DownstreamFailed` via outer catch. **Agent loses unknown-version signal.**

- **`UnsupportedSchema` (UnknownClientVersion) maps to render taxonomy in projection mapper** — MEDIUM — `FrontComposerMcpProjectionFailureMapper.cs:113-119`
  For `UnsupportedSchema`, projection mapper falls into `UnsupportedRender or UnsupportedSchema` arm and emits **render** taxonomy ("unsupported_render", `HFC-MCP-PROJECTION-UNSUPPORTED-RENDER`) — unrelated to actual root cause (missing/empty client schema fingerprint).

- **`ResolveAsync` admission throws `MalformedRequest` only when catalog has requested name** — HIGH — `ToolAdmissionService.cs:84-101`
  Schema gate wrapped in `try/catch` only fires on exact branch. When name not in catalog, suggestion path returns without consulting `ClientFingerprintHint`. Client with malformed `x-frontcomposer-schema-fingerprint` header gets `UnknownTool` + suggestion when probing misspelled name, but `MalformedRequest` when landing on real name. **Lets clients fingerprint catalog membership by header behavior.**

- **`InMemorySchemaBaselineProvider` is DEFAULT registration with placeholder baseline → all reads with fingerprint hint fail closed** — HIGH — `FrontComposerMcpServiceCollectionExtensions.cs:40` + `InMemorySchemaBaselineProvider.cs:64`
  `TryAddScoped<ISchemaBaselineProvider, InMemorySchemaBaselineProvider>()`. Provider's snapshot declares single field `("Number", "String", "string", true, false)`. When resolved against any real descriptor, `Compare` produces Breaking deltas (RemovedField "Number" + AddedRequiredField for every real field) → `SchemaMismatch`. **Any host adopting default registration without override has every MCP command/projection-with-fingerprint-hint fail closed.**

- **Cancellation NOT honored before entry-level schema gate or admission's schema gate** — MEDIUM — `ProjectionReader.cs:61`, `ToolAdmissionService.cs:87`, `CommandInvoker.cs:60`
  Patch added `ThrowIfCancellationRequested` at line 201 inside `ValidateSnapshotAsync` but not at first entry-level call. `EvaluateResource` performs synchronous canonical-JSON serialization + SHA-256 — explicitly the work the line-201 comment says should not run under cancel.

- **`CompatibleAdditive` / `CompatibleWarning` allow side effects but bypass argument-shape revalidation** — HIGH — `SchemaNegotiation.cs:211-219, 221-229` + `CommandInvoker.cs:59-65`
  Both kinds set `allowsSideEffects=true`. CommandInvoker short-circuits only on `!AllowsSideEffects`. `ValidateArguments` validates against current `descriptor.Parameters` set, not baseline. `CompatibleWarning` (e.g., enum value removed) treated identically to `Exact` for invocation; no warning surfaced in success envelope. **Conflicts with AC5 / AC6.**

- **`SchemaNegotiationRuntimeGate.EvaluateCommand` derivable filter not coupled to `SpoofedDerivableNames`** — MEDIUM — `SchemaNegotiationRuntimeGate.cs:222-233` + `CommandInvoker.cs:24-30`
  Filter sets aren't coupled. Descriptor whose derivable list omits TenantId but lists AggregateId will canonicalize TenantId into server fingerprint, while client (consulted DerivablePropertyNames at build time) excluded it. Divergent fingerprints for what should be identical contract.

- **Breaking decision with `RequiresMigrationGuide=false` collapses MissingMigrationGuide marker into plain Incompatible** — LOW — `SchemaNegotiation.Negotiate` + `SchemaMigrationDeltaAnalyzer.cs:128-134`
  Marker preserved in analyzer result but never surfaces in agent-visible payload. Operators get no deterministic signal that breakage is undocumented vs documented.

- **`ToolAdmissionService.ResolveAsync` doesn't revalidate schema between catalog build and acceptance — descriptor mutation race** — MEDIUM — `ToolAdmissionService.cs:75-101`
  Registry is Singleton; if registry exposes mutable descriptor objects, schema fingerprint computed on snapshot A, validation runs against snapshot B. Hot-reload scenarios produce stale gate decisions.

- **`Task.WhenAny` + reflection-rethrow pattern can wrap `OperationCanceledException` in `AggregateException`** — LOW — `CommandInvoker.cs:159-171, 191-193`
  Filter `ex is not OperationCanceledException` would not match `AggregateException` containing only `OperationCanceledException`. Could surface in reflection-invoked async paths as `DownstreamFailed` with stack trace, losing cancellation provenance.

- **Schema gate evaluates against `descriptor` (un-snapshotted) but post-query revalidation uses snapshot's descriptor** — MEDIUM — `ProjectionReader.cs:61` vs `:193, 202`
  Drift between paths if registry pointer mutates between calls.

- **Canonicalizer-version mismatch and structurally-Unknown both map to identical `SchemaMismatch`** — MEDIUM — `SchemaMigrationDeltaAnalyzer.cs:48-50, 175-181` + `SchemaNegotiation.cs:173-185`
  Canonicalizer mismatch is operator-actionable ("upgrade build pipeline"), distinct from structural-Unknown. Both surface as `HFC-SCHEMA-MISMATCH` — operators can't tell which to fix.

- **`LogAndReturn`'s `services.GetService<ILogger<...>>()` not guarded against `ObjectDisposedException`** — LOW — `SchemaNegotiationRuntimeGate.cs:160-174`
  Unlike `TryResolveBaseline` (line 187-196), no `try/catch ObjectDisposedException`. Disposed-scope race collapses negotiation to `DownstreamFailed`.

- **Lifecycle field positional drift not caught by name-set cross-check** — MEDIUM — `McpLifecycleModels.cs:16-20` + `SchemaFingerprintTransform.cs:595-601` + `SchemaFingerprintCrossPackageTests.cs:30-43`
  Positional record. Reordering positional parameters (e.g., swapping `Category` and `CorrelationId`) doesn't change the name set; cross-check is set-equality, not order-equality. Clients deserializing by position get wrong field assignments without fingerprint signal.

- **`Normalize` adds U+2028/U+2029 substitution but misses U+0085 (NEL)** — LOW — `SchemaFingerprintTransform.cs:614-620`
  Considered line terminator by `string.Split` and `TextReader.ReadLine`. Descriptor description containing U+0085 hashes differently after editor round-trip.

- **`Exact` short-circuit when `hashesMatch && snapshotDecision is null` regardless of `HasTrustedBaseline`** — MEDIUM — `SchemaNegotiation.cs:197-205`
  When `HasTrustedBaseline=true` (descriptor.Fingerprint not null) but `Baseline=null` (provider returned no baseline), `snapshotDecision=null`. With `hashesMatch=true` returns `Exact` — **no structural verification**, only byte-match between client claim and descriptor-emitter claim. Poisoned descriptor with tampered Fingerprint matching malicious client hint passes with zero structural validation.

- **`CompatibleWarning` enum-value mismatch surfaces as opaque `ValidationFailed` not as schema warning** — HIGH — `SchemaMigrationDeltaAnalyzer.cs:91-95` + `CommandInvoker.cs:266-284`
  EnumChanged / ValidationConstraintChanged produces `CompatibleWarning`. Gate allows side effects. `ValidatePrimitiveShape` checks `parameter.EnumValues.Contains(value.GetString(), StringComparer.Ordinal)` using **current** server values. Client baseline allowed "Old"; server's current schema removed "Old" from enum (CompatibleWarning) → validation rejects with `ValidationFailed`. Agent has no signal rejection is downstream of schema drift the gate already saw.

- **`McpLifecycleResult` PascalCase wire shape vs prior lowercase contract** — overlaps Chunk C Blind Hunter; verify wire serializer config.

- **Concurrent `MakeGenericMethod` allocation per call (cosmetic)** — LOW — `CommandInvoker.cs:184-194`
  Comment claims "cached delegate-style reflection" but only open generic is cached.

- **`Failure(category, structuredContent)` overload drops `Text`, two-arg vs three-arg surface inconsistency** — LOW — `FrontComposerMcpResult.cs:18-19, 21-25`
  Agents see different message-quality depending on which arm fires.

- **Admission-rejection arm doesn't log; exception-thrown rejection logs — observability drift** — LOW — `CommandInvoker.cs:45-50` (no log) vs `:142-151` (logs)
  Drift in observability between paths that surface identical agent payloads.

## Chunk D — Edge Case Hunter findings (test coverage gaps)

### HIGH

- **No test for `MissingMigrationGuide` interaction with truncation when marker lands as last delta** — `SchemaMigrationDeltaAnalyzer.cs:128-157`
  `markerSlot` reservation logic (P-43 / Group C) is non-trivial. Existing `Compare_BreakingDeltaPastIndex25_StillProducesBreakingAggregate` uses `requiresMigrationGuide: false` but only asserts `Decision == Breaking` — never asserts marker is present in truncated output, nor slot reserved while `Truncated` marker also fits. Regression dropping marker would pass.

- **No AC8 cross-package check that build-time aggregate differs from runtime aggregate when corpus is loaded** — `FrontComposerMcpRuntimeManifestAggregator.cs:11-58`
  `RuntimeAggregate_IncludesCorpusFingerprints_WhenSkillCorpusIsLoaded` only asserts `withCorpus.Value != withoutCorpus.Value`. AC8 contract requires build-time/runtime split; no test compares SourceTools `CreateAggregateManifestPayload` vs `FrontComposerMcpRuntimeManifestAggregator.Compute`. Future change accidentally fingerprinting corpus at build time goes undetected.

- **`FrontComposerMcpRuntimeManifestAggregator.Compute`'s "mixed null+non-null fingerprint" integrity throw has no test** — `FrontComposerMcpRuntimeManifestAggregator.cs:15-17`
  Security-critical fail-closed branch never exercised — all integrity tests use uniformly-fingerprinted or uniformly-null descriptors.

- **Cross-algorithm fall-through path not asserted** — `SchemaNegotiationRuntimeGate.cs:27-34`
  Comment declares "cross-algorithm clients fall through to structural snapshot comparator". Tests only cover shared algorithm. D23 boundary (client `Sha256SourceToolsBlobV1` vs server `Sha256CanonicalJsonV1`) unexercised.

- **`McpSchemaNegotiationResultKind.CompatibleWarning` branch has NO test coverage** — `SchemaNegotiation.cs:211-219`
  `agentCategory: "schema-compatible-warning"`, `messageKey`, `docsCode: HFC-SCHEMA-COMPATIBLE-WARNING`. Hit when analyzer returns `SchemaCompatibilityDecision.CompatibleWarning` (e.g., `EnumChanged` or `ValidationConstraintChanged`). No test constructs warning-only diff. **Entire CompatibleWarning decision path dead from test-coverage perspective.**

- **Algorithm-mismatch on build-time aggregate fingerprint (H7) has no test** — `FrontComposerMcpDescriptorRegistry.cs:180-182`
  Manifest stamping Fingerprint with `Sha256SourceToolsBlobV1` (instead of canonical) must throw `SchemaIntegrityMismatch`. No test in `AggregateManifestIntegrityTests` builds non-canonical-JSON algorithm id. H7 fail-closed branch uncovered.

### MEDIUM

- **Multi-valued `x-frontcomposer-schema-fingerprint` header not tested** — `HttpFrontComposerMcpAgentContextAccessor.cs:53-55`
  `if (values.Count != 1) throw ... MalformedRequest`. Accessor tests cover oversized/malformed/short/unsupported, never `new StringValues(["a", "b"])`. API-key path has `MultipleHeaderValues` test; schema-fingerprint header lacks parallel.

- **Cache-pollution / re-entrancy across requests not tested for `ClientFingerprintHint`** — `HttpFrontComposerMcpAgentContextAccessor.cs:44-60`
  No test for: per-`HttpContext` isolation, malformed-then-fixed re-read behavior, second-access returns same parsed instance.

- **Precedence matrix lacks "client null + algorithm-supported + integrity-OK + baseline-trusted-but-server-fingerprint-null" row** — `SchemaNegotiation.cs:102-110`
  `BuildInput` always supplies non-null `Server` fingerprint. "Server fingerprint missing despite trusted baseline" precedence ordering unverified.

- **Determinism tests don't cover OS / TZ / path-separator dimensions** — `SchemaFingerprintTransform.cs`
  AC11 docstring claims 5-dimension matrix; tests assert 2 (culture, EOL).

- **Truncation boundary at exactly `maxDeltaCount` and `maxDeltaCount + 1` not tested** — `SchemaMigrationDeltaAnalyzer.cs:136`
  `bool truncated = deltas.Count > maxDeltaCount`. Existing tests jump to 30; no test at exactly 25 (`IsTruncated == false`) and 26 (`IsTruncated == true`). Off-by-one undetectable.

- **Surrogate-pair path-truncation (P-45) not tested** — `SchemaMigrationDeltaAnalyzer.cs:258-273`
  No test constructs field name with surrogate pair near 256-char boundary. P-45 regression dropping `if (char.IsHighSurrogate) cut--` would pass.

- **`InMemorySchemaBaselineProvider.IsSafeIdentifier` accepts double-dot identifiers** — `InMemorySchemaBaselineProvider.cs:37-53`
  `"Hexalith..FrontComposer"` (or `"a..b"`) passes — path-traversal-shaped. Tests cover leading `"../"`, absolute, UNC, but never embedded-double-dot.

- **`Resolver_RejectsExternalPackageOwners` test misleadingly weak** — `SchemaBaselineResolverTests`
  Comment claims "must whitelist", but rejection happens because `"Contoso.NotShipped"` simply isn't a static-dictionary key. Future filesystem-based implementation would silently pass.

- **`ToolAdmissionSchemaGateTests` doesn't verify schema gate runs ONLY for exact matches** — `FrontComposerMcpToolAdmissionService.cs:84-105`
  Schema gate INSIDE `if (exact is not null)` branch. Misspelled tool takes suggestion path, never invokes gate. No oracle-attack test (agent learning "schema stale" only when name happens to match).

- **`SchemaIntegrityMismatch` aggregate-vs-nested integrity bypass not exhaustively probed** — `FrontComposerMcpDescriptorRegistry.cs:172-189`
  Tests cover (a) tampered nested resource + forged top-level, (b) tampered nested + valid top-level. Neither covers command-only manifest, nor mixed manifest with valid resource + tampered command in same aggregate.

### LOW

- **Fixture redundancy: `schema-same-different-order` and `schema-same-different-runtime-data` yield identical triples** — Fixtures
  Both: `Exact / schema-exact / Exact`. Differ only in `notes`. AC10 "minimal" claim weakened. Similar collapse for `schema-hidden-precedence` and `schema-unknown-precedence`.

- **No negative renderer fixture (e.g., `RendererCapabilityChanged` Breaking path)**
  `SchemaMigrationDeltaAnalyzer.cs:215-220` renderer-capability-change Breaking path uncovered by fixtures.

- **`SchemaNegotiationSnapshotInputTests` reflection scaffold regresses silently on parameter rename**
  Constructor parameter rename causes case match to fall through silently, producing meaningless inputs.

- **`RendererPayload_NormalizesEolInRendererId` doesn't cover lifecycle catalog under EOL variation**
  Future change flowing EOL-bearing strings through lifecycle payload regresses unnoticed.

- **`ZeroSideEffects_OnIncompatibleNegotiation_NoQueryNoRender` doesn't assert no logger emission of fingerprint-shaped tokens**
  AC15 D4 bounded-contract claim asserted only by source-comment inspection, not by runtime test.

- **`CompatibleAdditive_OnCommand_AdmitsDispatch_AfterRevalidation` structurally weak (duplicate Chunk D Blind)**
  No positive assertion revalidation actually ran. Production code skipping revalidation passes.

## Chunk B — Edge Case Hunter findings

### HIGH

- **`ValidateAggregateIntegrity` rejects every SourceTools-emitted manifest fingerprint (fail-closed startup)** — `FrontComposerMcpDescriptorRegistry.cs:180`
  `SchemaFingerprintTransform.cs:42` pins `AlgorithmId = Sha256SourceToolsBlobV1`. Integrity validator hard-requires `Sha256CanonicalJsonV1` per loop. Every host that consumes the source generator fails registry construction. The negotiator advertises both algorithms as supported (`SchemaNegotiation.cs:46-49`); contradiction local to integrity check. **Even if allow-list widened**, recomputed value uses canonical JSON and stamped value uses newline blob — byte streams structurally different, equality on `Value` cannot hold cross-algorithm. **VERIFY against actual McpManifestEmitter behavior.**

- **Client base64 fingerprint vs server hex fingerprint cannot byte-match** — `HttpFrontComposerMcpAgentContextAccessor.cs:147` vs `SchemaFingerprintContracts.cs:331-342`
  Parser stores raw base64. Server emits via `Sha256Hex` (lowercase 64-char hex). `string.Equals(client.Value, server.Value, Ordinal)` always false because base64 padding/+// alphabet never matches `[0-9a-f]{64}`. Clients can never reach byte-identical Exact path. **Defeats P-40 byte-identical short-circuit and recovery semantic.**

- **Unreachable Exact via byte-match means descriptor.Fingerprint-only deployments report Incompatible** — `SchemaNegotiationRuntimeGate.cs:34-46`
  When `baseline is null` and `descriptor.Fingerprint is not null`, gate sets `HasTrustedBaseline=true` and feeds `serverFingerprint = descriptor.Fingerprint` (hex) plus snapshot. `hashesMatch` always false (encoding mismatch); `CompareSnapshots` returns null because baseline is null. Falls through to `Incompatible/SchemaMismatch`. **Any host shipping only in-process generated manifests without baseline provider gets `SchemaMismatch` for every fingerprinted client request.**

### MEDIUM

- **`SchemaMaterialValidationException` from snapshot construction not categorized** — `SchemaNegotiationRuntimeGate.cs:199-256`
  `CanonicalSchemaMaterial.CreatePayload` throws `SchemaMaterialValidationException` (an `InvalidOperationException`) on duplicate field names, unknown family, etc. Gate makes no try/catch. Falls through to invoker's broad `catch (Exception)` → `DownstreamFailed` with `retryable=true`. **Indefinite agent retry loop on structural defect that never self-heals**; should have surfaced `SchemaIntegrityMismatch` with `retryable=false`.

- **`MalformedRequest` from `ClientFingerprintHint` getter bypasses schema-failure rewriter on tool resolution** — `FrontComposerMcpToolAdmissionService.cs:89-95`
  `ResolveAsync`'s catch filter only matches four schema categories; `MalformedRequest` falls through to outer `InvokeAsync` `catch (FrontComposerMcpException)` (line 141) → `FrontComposerMcpResult.Failure(ex.Category)` with generic text "Request failed." Agents lose ability to distinguish malformed schema header from any other malformed-request cause.

- **`HttpContext.Items` cache not populated when parsing throws — re-parses on every revalidation** — `HttpFrontComposerMcpAgentContextAccessor.cs:53-59`
  Cache write `http.Items[key] = parsed` unreachable when `ParseClientFingerprint` throws. Across `ProjectionReader.ReadAsync` fires up to four times per request, each producing duplicate structured-log entry and re-allocating byte arrays. Comment claim "avoids re-throwing" is partially false.

- **DI lifetime mismatch: singleton registry resolves `IEnumerable<ISkillCorpusFingerprintProvider>` from root scope** — `FrontComposerMcpDescriptorRegistry.cs:25-27` + `FrontComposerMcpServiceCollectionExtensions.cs:31`
  Registry is `TryAddSingleton`. With ASP.NET Core's `validateScopes=true` in Development, resolving Scoped/Transient corpus provider from singleton crashes at `BuildServiceProvider`. Contract (singleton-only) undocumented on interface.

- **Snapshot `requiresMigrationGuide: false` hard-coded — breaking deltas pass without migration guide marker** — `SchemaNegotiationRuntimeGate.cs:251` + `InMemorySchemaBaselineProvider.cs:77`
  Server snapshot built unconditionally. `SchemaMigrationDeltaAnalyzer.Compare` uses `baseline.Provenance.RequiresMigrationGuide` to decide whether to emit `MissingMigrationGuide`. With baseline always saying "no migration guide required", Breaking schema change produces `Breaking` aggregate but never the marker — defeating P-18 ("no migration guide for shipped breakage" signal).

- **Whitespace/control-character coverage of fingerprint header incomplete** — `HttpFrontComposerMcpAgentContextAccessor.cs:120-121`
  Parser only checks `Any(char.IsWhiteSpace)`. Allow-list rejects today, but if allow-list expands or if telemetry sink logs raw `algorithmId` before allow-list check (e.g., "rejected algorithm" log), control characters propagate. Tighten with `[a-z0-9.\-]+` regex.

### LOW

- **`ValidateAggregateIntegrity` skip on `manifest.Fingerprint == null` allows attacker-controlled assemblies to bypass integrity** — `FrontComposerMcpDescriptorRegistry.cs:173-175`
  Mixed null/non-null aggregates pass per-manifest integrity even though aggregator enforces all-or-none on per-manifest commands/resources. Document or enforce "manifests MUST stamp `Fingerprint`".

- **`InMemorySchemaBaselineProvider.IsSafeIdentifier` allows path-traversal segments** — `InMemorySchemaBaselineProvider.cs:37-53`
  `"a..b"` and `"a.."` pass. Today only in-memory dictionary consulted, so traversal moot. Class doc invokes `..` as motivation but regex never rejects. **Future on-disk loader composing `Path.Combine` would not fire rejection.** (Note: duplicate of Chunk D Edge Case finding.)

- **Cancellation not honored inside snapshot construction** — `SchemaNegotiationRuntimeGate.cs:199-256`
  `EvaluateResource`/`EvaluateCommand` accept no `CancellationToken`; canonical-JSON build inside `CanonicalSchemaMaterial.CreatePayload` cannot be interrupted. (Note: duplicate of Chunk C Edge Case finding.)

- **`MapSchemaFailureStrict` `ArgumentException` includes category enum name in message — caller leakage** — `SchemaNegotiationRuntimeGate.cs:106`
  Outer logger captures `ex.Message`; misuse-detection text leaks raw enum name into telemetry. (Note: duplicate of Chunk B Blind / Chunk C Edge.)

- **Empty-aggregate canonical hash collision across distinct empty corpora** — `FrontComposerMcpRuntimeManifestAggregator.cs:54-57`
  Two structurally distinct configurations ("no manifests, no corpus" vs "one manifest with no commands/resources, no corpus") produce same canonical document and fingerprint. Aggregator doesn't incorporate manifest count.

- **`Convert.FromBase64String` ambiguous padding accepted** — `HttpFrontComposerMcpAgentContextAccessor.cs:137-145`
  Padding length differences accepted; stored `encodedFingerprint` retains exact textual form. Two clients sending same 32-byte hash with different padding variants treated as different at byte level. Compounds encoding-mismatch unreliability.

- **Telemetry `result.Kind.ToString()` not bounded to allow-list** — `SchemaNegotiationRuntimeGate.cs:169-174`
  Future `McpSchemaNegotiationResultKind` enum value (e.g., `Quarantined`, `RateLimited`) propagates verbatim into structured logs.




