# Story 8-6a — Prior Review Context (Groups A, B, C — applied or deferred)

This document lists findings already triaged in Groups A, B, C of the 8-6a code review. **Group D reviewers must NOT re-flag these as fresh findings.** A finding that *cosmetically* looks like one of these but raises a genuinely distinct concern is allowed; identify the differentiator clearly.

## Group A — applied patches (initial implementation review)

- D6 — per-manifest integrity loop in `FrontComposerMcpDescriptorRegistry`
- Header parser hardening: SHA-256 length check + algorithm allow-list (`HttpFrontComposerMcpAgentContextAccessor`)
- Accessor memoization on `HttpContext.Items` (parse fingerprint once per request)
- `ObjectDisposedException` resilience in `TryResolveBaseline` → null baseline (not `DownstreamFailed`)
- `OrdinalIgnoreCase` comparer alignment for derivable-property filtering
- `SchemaCompatibilityDecision.Unknown` → `Incompatible` (when both snapshots present)
- HashSet tuple-key `(AlgorithmId, Value)` to avoid `:` collision in dedup
- `LazyThreadSafetyMode.PublicationOnly` on `InMemorySchemaBaselineProvider` snapshots
- `decisionKind`/`category` log fields use `.ToString()` (deterministic structured-log)
- `MapSchemaFailureStrict` throws `ArgumentException` on non-schema category (outer/inner contract)
- `cancellationToken.ThrowIfCancellationRequested()` before sync gate work in `ValidateSnapshotAsync`
- Inner-exception drop on `Convert.FromBase64String` (no chaining `FormatException`)
- `corpusProviders.SelectMany` null guard
- `descriptor.DerivablePropertyNames` null guard
- Command invoker exception logging with stack capture

## Group A — deferred (acknowledged, not reviewed-as-bugs)

- D3: Real baseline-snapshot generation — runtime gate is currently a no-op for clients sending fingerprint hint until D3 ships. `DefaultFixtureId = "baseline-known-v1"` placeholder is intentional.
- `IsHiddenOrUnknown` / `IsStaleDescriptor` hardcoded `false` at gate — visibility/policy/tenant filtering happens upstream. AC2/AC3 hold by flow ordering.
- `CompatibleAdditive` and `CompatibleWarning` collapse to same agent category (`schema-compatible-warning`).
- Aggregator dedup hides cardinality + `corpusFingerprintCount` pre-dedup mismatch.
- Aggregator partial-fingerprint bypass when no manifest claims fingerprint.
- `BuildStructuredFailure` enumeration oracle (`isHiddenEquivalent: false`).
- `McpLifecycleResult` model added but unused in Group A scope (used elsewhere in story).
- `EvaluateCommand` invoked twice — verified defensive null-coalesce only.
- Render bounds wrong member / frozen at startup / empty Fields — defer to render-contract semantics follow-up.
- `HasTrustedBaseline` set on `descriptor.Fingerprint` — D7 partial-revert; descriptor's emitter-stamped fingerprint IS a trust signal.

## Group A — dismissed (not bugs)

- Schema gate fail-open when client omits header — explicit D2 design.
- `ToolAdmissionService` exception filter — intentionally narrow.
- `Reject(name, suggestion, catalog)` drops category — false positive (two distinct overloads).
- HTTP separator off-by-one and surrogate-pair edges — correctly handled / cosmetic.
- Undefined `SchemaContractFamily` enum value falls through dictionary — correct fail-closed.

## Group B — applied patches (Schema project extraction & SourceTools transforms)

- D9: Schema.csproj multi-target `net10.0;netstandard2.0` (mirrors Contracts pattern).
- D10: Lifecycle cross-check relocated from SourceTools.Tests → Mcp.Tests; dropped `InternalsVisibleTo "Hexalith.FrontComposer.SourceTools.Tests"` from Mcp.csproj.
- P-43: `MissingMigrationGuide` delta hoisted ABOVE truncation (respects `maxDeltaCount` budget).
- P-44: Empty-delta + hash mismatch → `Unknown` (not vacuous-true `Exact`).
- P-45: Surrogate-pair-safe path truncation (`TruncatePath` helper).
- P-46: `SchemaContractFamilyNamesTests` build-time exhaustiveness (kebab-case + distinct).
- P-47: Cross-package check now validates field NAMES, TYPES, and pins State enum-values cell.

## Group B — deferred

- Lost invariant comment on truncation marker (doc-only).
- `.All(...)` perf scan — fine at MaxDeltaCount=25.
- Ellipsis length budget +2 (downstream telemetry-cap audit).
- AdditiveCompatible vs CompatibleWarning aggregate decision regression — verify before declaring complete.
- Attribute-based exhaustiveness on switch — redundant given P-46.

## Group B — dismissed

- Lifecycle camelCase → PascalCase fingerprint regen — accepted by spec.
- Two prompt-artifact false positives on `Replace(' ', '\n')`.
- Mcp net10.0 → Schema netstandard2.0 reference — runtime loads natively.
- `CreateLifecycleFieldLines` per-call allocation — cold path (build-time generator).
- Schema project rename without `[TypeForwardedTo]` — internal namespace, no consumers.
- netstandard2.0 + collection-expression compile risk — verified clean.

## Group C — applied patches (SchemaContractFamilyNames & migration delta diagnostics)

- **Reserved slot for `MissingMigrationGuide` marker** in truncation: extracted before `Take(...)`, re-appended.
- Removed unused `using System.Text.RegularExpressions` from `SchemaFingerprintCrossPackageTests.cs`.
- **Tightened kebab-case regex** in `SchemaContractFamilyNamesTests`: `^[a-z][a-z0-9]*(-[a-z0-9]+)*$` (no trailing/consecutive dashes).

## Group C — deferred

- `TruncatePath` does not normalize lone low surrogates (defense-in-depth).
- `maxDeltaCount = 1` boundary loses all real findings — harden floor to 2.
- `ExpectedStateLine` pins alphabetical canonicalizer order; runtime emission cross-check is missing.
- Group B doc entry mis-describes path-truncation marker length (doc-only).

## Group C — dismissed

- Empty-delta = `Unknown` breaking equal-schemas contract — false positive (byte-equality short-circuit precedes).
- netstandard2.0 collection-expression compile risk — verified clean both facets.
- Removing `InternalsVisibleTo "Hexalith.FrontComposer.SourceTools.Tests"` breaking unrelated tests — verified.
- `preliminaryAggregate` vs final `aggregate` recompute fragility — currently idempotent.
- `TruncatePath` off-by-one length after surrogate adjustment — cosmetic.
- `LifecycleCatalog_FieldTypes_MatchRuntimePropertyTypes` doesn't assert catalog has runtime fields — covered by sibling test.
- `MapClrTypeToCatalogType` masks future drift as `InvalidOperationException` — fail-closed informative.

## Story-level Critical Decisions D1..D23 (inherited from Story 8-6)

These are binding decisions; do **not** propose reversing them:

- D2: schema gate fail-open when client omits header (explicit design).
- D7: descriptor.Fingerprint participates in `HasTrustedBaseline` (build-pipeline trust signal).
- D20: re-run server-side validation on `CompatibleAdditive` before any side effect.
- D23: two-algorithm v1 contract (`Sha256CanonicalJsonV1` + `Sha256SourceToolsBlobV1`) preserved.
- AC11: byte-for-byte fingerprint determinism across OS/culture/TZ/EOL/path-separator.
