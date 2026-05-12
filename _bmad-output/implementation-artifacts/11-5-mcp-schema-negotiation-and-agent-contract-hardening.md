# Story 11.5: MCP Schema Negotiation and Agent Contract Hardening

Status: done

> **Epic 11** - Deferred Hardening & Release Readiness. Closes MCP schema negotiation, skill corpus, agent contract, fingerprint, tenant-scope, lifecycle result, and schema-rejection follow-ups routed from Stories 8.1 through 8.6a. Applies lessons **L03**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11-5 is the release-readiness hardening pass for the MCP and agent-facing contract surface.

Stories 8.1 through 8.6a delivered typed MCP tools/resources, hallucination rejection, two-call lifecycle behavior, Markdown projection rendering, skill corpus resources, schema fingerprints, runtime schema negotiation, and sanitized schema-failure categories. Later reviews left bounded follow-ups around compatible-additive revalidation, runtime corpus aggregate usage, mixed-algorithm fingerprint handling, descriptor correlation on schema rejection, constructor selection, hidden/stale precedence documentation, lifecycle fingerprint cross-checks, and stale/ambiguous agent-facing failure categories.

This story implements or explicitly accepts those follow-ups without reopening broad Epic 8 feature scope. The intended outcome is that agent command/query behavior is predictable, tenant-safe, version-aware, and auditable through focused MCP tests and clear release constraints.

---

## Story

As an agent integrator,
I want MCP schema negotiation and agent contract deferrals closed,
so that agent command/query behavior is predictable, tenant-safe, and version-aware.

### Release-Readiness Job To Preserve

A release owner should be able to run the focused MCP/schema test slice and know that schema drift, corpus fingerprints, tool admission, lifecycle categories, tenant scoping, and failure responses either fail closed or are documented as accepted v1 constraints with evidence.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary MCP files | Harden `SchemaNegotiation.cs`, `SchemaNegotiationRuntimeGate.cs`, `FrontComposerMcpDescriptorRegistry.cs`, `FrontComposerMcpRuntimeManifestAggregator.cs`, `McpToolResolutionResult.cs`, `FrontComposerMcpProjectionReader.cs`, `FrontComposerMcpCommandInvoker.cs`, and `FrontComposerMcpToolAdmissionService.cs` only where Story 11.5-owned rows require it. |
| Primary tests | Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/**`, `Invocation/**`, `AuthContextAccessorTests.cs`, `Skills/**`, and focused SourceTools schema fixture tests where cross-package fingerprint evidence requires it. |
| Deferred ledger | Close or explicitly accept MCP/schema/skill-corpus rows in `_bmad-output/implementation-artifacts/deferred-work.md`, especially DEF-D5, DEF-C3, DEF-CK4-*, Story 8.5, 8.6, and 8.6a rows. |
| Agent contract | Preserve tenant-safe hallucination rejection, hidden/unknown precedence, bounded schema-failure categories, and zero side effects before admission succeeds. |
| Fingerprints | Compare algorithm and value together; reject or document mixed-algorithm aggregate behavior; keep canonicalizer assumptions explicit. |
| Skill corpus | Runtime aggregate/corpus fingerprints must either participate in production validation or be documented as a release constraint with tests proving the seam. |
| Scope guardrail | Do not absorb diagnostic registry governance, CLI/IDE hardening, SourceTools drift hardening, shell UX/accessibility, EventStore reliability, or CI/release workflow work. |
| Validation | Start with MCP focused schema/invocation tests; run SourceTools schema fixture tests only when generator/fingerprint fixture contracts are touched. |

Start here: T1 inventory Story 11.5 deferred rows -> T2 patch compatible-additive revalidation -> T3 harden aggregate/fingerprint/corpus semantics -> T4 tighten schema rejection and lifecycle/category contracts -> T5 update docs/ledger/evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Story 11.5-owned deferred rows exist in `deferred-work.md` | Story 11.5 completes | Each row is marked resolved, superseded, split, or accepted with date, owner, rationale, and validation evidence; no row is silently deleted. |
| AC2 | Negotiation returns `CompatibleAdditive` or `CompatibleWarning` for a projection, command, or tool request | The request is admitted | Current server-side validation, defaulting, bounds, authorization, sanitization, and baseline argument-shape checks run before any side effect; skipped red-phase tests are unskipped or replaced by stronger coverage. |
| AC3 | Client and server fingerprints have equal values but different algorithms | `McpSchemaNegotiator` compares them | The result is not `Exact`; algorithm and value are both part of equality, and unsupported/mixed algorithms fail closed with bounded agent categories. |
| AC4 | Runtime manifests and skill corpus fingerprints are loaded | The descriptor registry computes aggregate integrity | Corpus fingerprints participate in a production validation path, or the registry documents and tests that corpus integrity remains a v1 release constraint rather than a runtime gate. |
| AC5 | Aggregate manifest computation receives fingerprints from more than one algorithm family | The aggregate is computed | Mixed-algorithm input is rejected as `SchemaIntegrityMismatch` or constrained by an explicit same-algorithm contract with tests and release notes. |
| AC6 | No manifest claims a fingerprint but runtime descriptors exist | Registry integrity validation runs | The partial-fingerprint bypass is either closed fail-safe or documented as the legacy/no-integrity path with explicit telemetry/test evidence. |
| AC7 | Schema rejection occurs for a known descriptor | Tool/projection/command rejection response and logs are built | Descriptor correlation is preserved in bounded internal evidence without leaking hidden names, tenant IDs, raw envelopes, fingerprints, local paths, or exception text to agents. |
| AC8 | `FrontComposerMcpDescriptorRegistry` is resolved through DI | Constructors are selected | DI cannot silently choose a legacy constructor that drops corpus providers; constructor shape is consolidated or tested through the production registration path. |
| AC9 | Hidden/unknown, stale descriptor, schema mismatch, unsupported algorithm, and unknown baseline causes overlap | Admission and negotiation classify the request | Hidden/unknown precedence remains tenant-safe; stale/schema precedence is documented and pinned by tests that prove lower-priority details do not leak. |
| AC10 | MCP lifecycle result fingerprint material is pinned | Runtime emission changes | A cross-check proves the pinned state line or catalog matches the production `McpLifecycleResult` payload emitted by the MCP server, not only a duplicated test constant. |
| AC11 | `McpSchemaNegotiationResult` exposes `MessageKey`, `AgentCategory`, and `decisionKind` | Agent-facing responses are built | Stable machine keys and prose categories are consistently named or the inconsistency is documented with a compatibility rationale and tests pinning the wire contract. |
| AC12 | Skill corpus resources return success/failure states | `SkillResourceReadResult` is constructed | Invalid success/category combinations are impossible via factories or are pinned as accepted compatibility behavior with tests. |
| AC13 | Skill corpus diagnostics or source fields are logged or exposed | Telemetry/logging paths run | Raw paths, tenant-shaped strings, tokens, generated payload fragments, and machine-local values are redacted or the no-log contract is documented with tests. |
| AC14 | Agent context fingerprint headers are parsed | Headers are missing, duplicated, malformed, oversized, uppercase, or cached | Parser behavior remains fail-closed, memoized, bounded, and covered by tests for success cache, sentinel rethrow, multi-value rejection, null/empty no-op, and case sensitivity. |
| AC15 | MCP Markdown rendering emits enum labels or schema-affecting descriptor metadata | Rendering and fingerprint tests run | Any enum display-label parity change is coordinated with SourceTools/Contracts schema fingerprint material; otherwise the raw-enum v1 contract is documented as a Story 11.6 or v1.x follow-up. |
| AC16 | MCP tenant scope, hallucination rejection, schema mismatch behavior, and lifecycle result categories are validated together | Focused test suites run | Cross-surface tests prove unknown tools do not reach backend dispatch, cross-tenant tools remain invisible, schema failures have stable categories, and lifecycle category changes are pinned. |
| AC17 | Story 11.2, 11.3, 11.4, 11.6, and 11.7 own adjacent release-readiness work | Story 11.5 touches docs, diagnostics, SourceTools, shell, EventStore, or CI | Changes are limited to MCP/agent contract evidence; adjacent work is handed off instead of silently absorbed. |
| AC18 | Validation completes | Story 11.5 moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved accepted constraints, and evidence paths. |
| AC19 | Multiple MCP rejection causes apply to the same request | Admission, negotiation, and invocation classify the failure | A normative fail-closed precedence matrix is applied consistently across tools, resources, and skill-corpus paths; first matching cause wins and lower-priority details do not leak. |
| AC20 | A compatible-additive or compatible-warning request later fails current server validation | Tests instrument MCP invocation seams | Validation fails before any observable side effect: no command dispatch, query execution, lifecycle success, cache write, renderer buffer, token relay, manifest write, or ledger mutation occurs. |
| AC21 | Schema, tenant, corpus, and agent-contract tests are added | Test fixtures are composed | Minimal independent fixtures for schema manifests, skill-corpus fingerprints, tenant scope, and agent contract behavior are used so mixed-algorithm, missing-fingerprint, stale-descriptor, and cross-tenant combinations are visible. |
| AC22 | Public rejection payloads, logs, diagnostics, exceptions, or telemetry are produced | Sentinel values are injected for tenant ID, user ID, token, raw payload, local path, raw descriptor, hidden name, and machine-local source | Assertions prove those sentinel values are absent from every public or semi-public surface while internal investigation keeps only opaque correlation evidence. |
| AC23 | Fingerprints are compared or aggregated | MCP schema paths evaluate equality or aggregate integrity | Comparisons are centralized around typed fingerprint identity including algorithm, value, and material kind where available; direct string-value-only comparisons in MCP paths are removed or covered by regression tests. |
| AC24 | A client omits claimed fingerprint material | Registry integrity validation runs | The outcome is explicitly decided before implementation: fail closed by default, or a named legacy/release-constraint path with owner, expiry, telemetry, downstream consumers, and tests. |
| AC25 | `MessageKey`, `AgentCategory`, `decisionKind`, `ProtocolUriCategory`, or lifecycle categories are changed or documented | Agent-facing responses are pinned | The story records whether each field is strict, extensible, or fallback-mapped; public value changes require compatibility tests and maintainer-facing notes. |
| AC26 | Enum display-label parity is deferred to Story 11.6 or v1.x | MCP schema fingerprint material and manifests are evaluated | Deferral is allowed only when tests or notes prove enum labels do not participate in published schema fingerprints, manifest material, or agent contract values; otherwise the compatibility work remains in Story 11.5. |
| AC27 | Registry descriptors, runtime manifests, skill-corpus providers, and claimed fingerprints are read during one MCP request | Negotiation, current validation, and side-effect admission run | A single immutable contract snapshot or epoch is used from first contract decision through side-effect decision, or the request restarts/fails closed if the snapshot changes. |
| AC28 | Client header hints, descriptor claimed fingerprints, runtime manifest fingerprints, and skill-corpus aggregate fingerprints disagree | Admission evaluates the request | The mismatch cannot downgrade to a weaker compatibility path; it fails closed or follows an explicitly named compatibility map with tests and bounded public categories. |
| AC29 | Header parsing, descriptor lookup, or fingerprint resolution fails and the result is memoized in the request path | The same request attempts another MCP tool/resource/lifecycle decision | The bounded failure result is reused without reparsing raw input, leaking raw values, or producing a different public category on retry. |
| AC30 | A Story 11.5 deferred row is accepted as a release constraint rather than fixed | The ledger and story evidence are updated | The acceptance names owner, expiry or revalidation trigger, downstream consumer impact, telemetry/evidence path, and a regression guard that prevents silent permanent acceptance. |
| AC31 | Current culture, enum display labels, or `ToString()` output differs from invariant/raw values | Agent-facing keys, categories, URI categories, lifecycle categories, or fingerprint material are emitted | Machine contract values are ordinal/invariant and tests prove localized or display prose cannot become contract input unless a deliberate compatibility change is recorded. |
| AC32 | Dev Agent Record, deferred-work evidence, logs, snapshots, or test output are attached as Story 11.5 proof | Evidence is generated or copied into artifacts | Evidence uses row-scoped references and sanitized excerpts; raw headers, payloads, local paths, tenant/user IDs, tokens, exception text, and unbounded descriptor dumps are absent. |

---

## Tasks / Subtasks

- [x] T1. Inventory and classify Story 11.5 deferred rows (AC1, AC17, AC18)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [x] Capture MCP/schema/skill-corpus rows from Stories 8.1 through 8.6a, including DEF-D5, DEF-C3, DEF-CK4-*, DEF-2, Story 8.5 skill corpus rows, and Story 8.2/8.3 agent-surface rows that are not already owned by Story 10.6 or 11.7.
  - [x] Classify each row as fix now, accept with evidence, split to Story 11.6/11.7/10.6, or leave blocked with a named product/architecture decision.
  - [x] Preserve historical review text; append resolution markers rather than rewriting the ledger.
  - [x] For every `accepted` row, name the residual risk, owning decision, downstream consumer impact, and whether the item blocks merge or release.
  - [x] Build a row-to-evidence matrix before implementation so each Story 11.5 closure maps to a specific test, release-constraint note, compatibility decision, or split-story reference.
  - [x] For every accepted release constraint, include owner, expiry or revalidation trigger, downstream consumer impact, telemetry/evidence path, and the regression guard that keeps the acceptance from becoming silent permanent policy.

- [x] T2. Implement compatible-additive revalidation and zero-side-effect coverage (AC2, AC9, AC16)
  - [x] Unskip or replace the existing `CompatibleAdditive` projection/command admission tests that were left pending DEF-D5.
  - [x] Thread a post-admission revalidation/defaulting/bounds step into `FrontComposerMcpProjectionReader` and `FrontComposerMcpCommandInvoker` before query/dispatch side effects.
  - [x] Ensure `CompatibleWarning`/`EnumChanged` keeps the schema-drift signal instead of degrading to generic `ValidationFailed` when old enum values are rejected.
  - [x] Add side-effect spies for command dispatch, query execution, lifecycle mutation, cache writes, renderer buffers, and token relay where feasible.
  - [x] Keep hidden/unknown and tenant/policy checks ahead of schema details.
  - [x] Add a fake side-effect recorder proving additive/warning requests that fail current validation do not invoke dispatch, query execution, lifecycle success, cache writes, renderer buffers, token relay, manifest writes, or ledger writes.
  - [x] Pin one immutable contract snapshot or epoch across negotiation, current validation, and side-effect admission; add a stale-snapshot test that restarts or fails closed before side effects.

- [x] T3. Harden fingerprint and aggregate semantics (AC3-AC6, AC8, AC10)
  - [x] Compare `SchemaFingerprint.AlgorithmId` and `Value` together in `SchemaNegotiation.cs`.
  - [x] Decide mixed-algorithm aggregate policy in `FrontComposerMcpRuntimeManifestAggregator`; reject or document same-algorithm-only input.
  - [x] Close or document the no-claimed-fingerprint bypass in `FrontComposerMcpDescriptorRegistry`.
  - [x] Route `ISkillCorpusFingerprintProvider` outputs into a production aggregate integrity path, or record the release constraint with tests proving the current seam.
  - [x] Consolidate or production-test the `FrontComposerMcpDescriptorRegistry` constructor selected by `AddFrontComposerMcp`.
  - [x] Add runtime lifecycle payload cross-checks for `McpLifecycleResult` so pinned state material cannot drift silently.
  - [x] Add parameterized fingerprint tests for same value/different algorithm, same algorithm/different value, same algorithm/same value, aggregate mixed algorithm, and missing claimed fingerprint.
  - [x] Keep runtime manifest fingerprints and skill-corpus fingerprints independently versioned and reported; any combined aggregate must preserve component identity, algorithm, and material kind.
  - [x] Add DI composition coverage with at least two corpus providers and a zero-provider case whose expected behavior is either fail-closed or an explicitly documented release constraint.
  - [x] Add conflict tests where header hints, descriptor claimed fingerprints, runtime manifests, and corpus aggregates disagree; prove the path fails closed or follows a named compatibility map without downgrading.

- [x] T4. Tighten schema rejection, descriptor correlation, and agent categories (AC7, AC9, AC11, AC13, AC16)
  - [x] Preserve descriptor correlation for schema rejection without exposing hidden names or tenant-specific data to agents.
  - [x] Clarify `MessageKey`, `AgentCategory`, and `decisionKind` naming; change only if compatibility risk is acceptable and tests pin the public wire shape.
  - [x] Document and test hidden/stale/schema precedence for production paths where stale descriptor state is upstream of `SchemaNegotiationRuntimeGate`.
  - [x] Ensure schema-failure logs use bounded category/message/docs fields only.
  - [x] Review `ProtocolUriCategory` and lifecycle categories; either specialize them with evidence or accept them as v1 decorative metadata.
  - [x] Add the rejection precedence matrix and table-driven tests for hidden+unknown, hidden+stale, unknown+schema, stale+schema, tenant mismatch+schema, unsupported algorithm+schema, and missing fingerprint+schema collisions.
  - [x] Test both correlation surfaces: internal opaque evidence remains useful for investigation, while public responses omit hidden descriptor names, tenant/user IDs, raw payloads, tokens, local paths, exception text, and raw descriptor material.
  - [x] Treat any public value rename for `MessageKey`, `AgentCategory`, `decisionKind`, URI category, or lifecycle category as compatibility work requiring downstream tests and release notes.
  - [x] Run category/key tests under a non-invariant culture and mixed enum display-label inputs; prove machine keys use ordinal/invariant values, not localized prose or `ToString()` drift.

- [x] T5. Harden skill corpus and auth/header parser contracts (AC12-AC15)
  - [x] Replace `SkillResourceReadResult` invalid-state construction with success/failure factories, or document the compatibility constraint.
  - [x] Add tests for skill corpus diagnostics source redaction or no-log/no-agent-exposure contract.
  - [x] Add `AuthContextAccessor` coverage for cached success, malformed sentinel rethrow, multi-valued headers, uppercase hex rejection, null/empty header no-op, and oversized lowercase values.
  - [x] Decide whether enum display-label parity in MCP Markdown rendering is Story 11.5 scope; if not, record a handoff to Story 11.6 or v1.x because schema fingerprint material changes.
  - [x] Document valid `SkillResourceReadResult` states as a matrix; success requires safe content and fingerprint material, not-found forbids content, denied forbids raw reason leakage, and invalid-schema exposes only safe diagnostic codes.
  - [x] Add hostile header parser cases for absent, empty, duplicate, contradictory multi-value, mixed casing, whitespace, unknown algorithm, truncated value, invalid separator, oversized value, and control characters; all ambiguous cases fail closed before schema/tenant resolution and without logging raw values.
  - [x] Use sentinel redaction tests that assert absence of tenant ID, user ID, token, raw payload, local path, raw descriptor, hidden name, and machine-local source across messages, structured diagnostics, exceptions, telemetry, and captured logs.
  - [x] Add memoized-failure retry coverage proving parser, descriptor, and fingerprint failures keep the same bounded category on repeated request-path access without reparsing or logging raw inputs.

- [x] T6. Update docs, ledger, and validation evidence (AC1, AC17, AC18)
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` with resolution/acceptance/split markers for every Story 11.5-owned row.
  - [x] Update focused comments or release notes only where behavior changes need maintainer-facing explanation.
  - [x] Record exact validation commands and outcomes in this story's Dev Agent Record.
  - [x] Move Story 11.5 to `review` only after implementation and validation evidence are complete.
  - [x] Link every closed ledger row to at least one compatibility test, redaction/no-log test, production payload validation artifact, release-constraint note, or split-story reference.
  - [x] Keep evidence excerpts sanitized and row-scoped; never paste raw headers, payloads, local absolute paths, tenant/user IDs, tokens, exception text, or unbounded descriptor dumps into story artifacts.

### Review Findings

_Date: 2026-05-12. Reviewers: Blind Hunter (Cynical Review skill), Acceptance Auditor (spec vs diff)._
_Edge Case Hunter ran partially — interrupted before completion; only the salvaged validator-throw observation was carried into P2._

#### Decision-needed (scope and design)

- [x] [Review][Decision][Resolved as Patch] DN19: EventStore submodule pointer is out of Story 11.5 scope — AC17 says Story 11.5 changes must stay limited to MCP/agent contract evidence and hand EventStore reliability work to Story 11.7. Resolved by checking the `Hexalith.EventStore` submodule working tree back to the parent-recorded commit `485bbe2311341ac8a6c7569cf99c37eb085268c2`, removing the out-of-scope parent diff.
- [x] [Review][Decision][Resolved as Patch] DN20: AC28 four-way fingerprint conflict remains unproven on the admission path — Resolved by strengthening `Story11_5ResolutionTests.DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade` to drive `FrontComposerMcpToolAdmissionService.ResolveAsync` with a client fingerprint hint that disagrees with the descriptor fingerprint while a corpus provider is loaded, then preserving the registry-construction integrity assertion for forged manifest fingerprints.
- [x] [Review][Decision][Resolved as Patch] DN21: AC22 redaction proof contradicted the public-surface claim — Resolved by treating `RequestedName` as intentional caller echo and using a safe caller-provided requested name in `DN8_SentinelRedaction_RejectedToolStripsDescriptorBeforeWire`; the hidden descriptor sentinel now remains only in resolved descriptor metadata and opaque correlation-key derivation.
- [x] [Review][Decision][Resolved as Patch] DN22: Deferred-work closure was still category-scoped rather than row-scoped — Resolved by replacing the range-only ledger amendment with an explicit row-scoped closure matrix that names every affected DW row and attaches disposition, owner, rationale, trigger, downstream impact, and evidence by category.
- [x] [Review][Decision][Resolved by Evidence Link] DN1: Closed as evidence pass. `FrontComposerMcpProjectionReader` admission revalidation, hidden precedence, zero-side-effect on query+render, sanitized agent category, and unknown-baseline coverage are pinned by `ProjectionReaderSchemaGateTests` (SchemaGate_RunsAfterVisibility_BeforeQueryDispatch, HiddenPrecedence_WinsOverSchemaMismatch, CompatibleAdditive_AdmitsDispatch_AfterRevalidation, UnsupportedAlgorithm_FromClient_SurfacesSanitizedAgentCategory, UnknownBaseline_SurfacesSchemaUnavailable, ZeroSideEffects_OnIncompatibleNegotiation_NoQueryNoRender). The lifecycle/cache/token-relay/manifest-write/ledger-write surfaces named in AC20 are not on the projection path — the production reader only invokes `IQueryService` + `IFrontComposerMcpProjectionRenderer`, both of which are spied. Side-effect spy expansion for command-only surfaces is covered by `CommandInvokerSchemaGateTests`. The "Update likely" note in Source Tree Components was a planning hint, not a code-change requirement; existing source already satisfies the AC. Reconciliation: T2/AC2/AC20 subtasks remain as-claimed; no follow-up row needed.
- [x] [Review][Decision][Resolved by Evidence Link] DN2: Closed as evidence pass. `SchemaNegotiationRuntimeGate` behavior is exercised end-to-end via `ProjectionReaderSchemaGateTests`, `CommandInvokerSchemaGateTests`, `ToolAdmissionSchemaGateTests`, and `SchemaBaselineResolverTests`. Hidden/stale/schema precedence is pinned by `SchemaNegotiationPrecedenceMatrixTests` rows 1, 1b, 2 (hidden-over-everything, hidden-over-schema-mismatch, stale-over-integrity). The runtime gate has no production state of its own beyond the negotiator + baseline provider it composes; the negotiator's precedence matrix is the canonical source of truth. Reconciliation: T4 subtask remains as-claimed; the "absent from diff" finding reflects that the gate was correctly authored in earlier 8-6a commits (84fb905, 0cbd31a) and needed no Story 11.5-scope changes.
- [x] [Review][Decision][Resolved by Evidence Link] DN3: Closed as evidence pass. `FrontComposerMcpDescriptorRegistry.ValidateAggregateIntegrity` (L157-L196) handles the no-claimed-fingerprint case via per-manifest scope (manifests with null `Fingerprint` are skipped — the registry trusts the contract that emitters either stamp or omit, never both). The Sha256SourceToolsBlobV1 trust-path bypass is documented inline (C1 comment at L177-L182). DI constructor selection is pinned by `DescriptorRegistry_DiConstruction_UsesCorpusAwareConstructor`, `DescriptorRegistry_DiConstruction_InvokesAllRegisteredCorpusProviders` (multi-provider), and `DescriptorRegistry_DiConstruction_ZeroProviders_DoesNotFailClosed`. The "no source change" observation is correct: the bypass is documented + tested rather than removed because removing it would block adopters running with build-time blob fingerprints. The named release-constraint owner is documented in DN14 below.
- [x] [Review][Decision][Resolved by Evidence Link] DN4: Closed as evidence pass. `AuthContextAccessorTests` covers all hostile parser cases AC14 enumerates: cache success (`ClientFingerprintHint_CachesSuccessfulParse_ForRequestLifetime`), malformed sentinel rethrow (`ClientFingerprintHint_CachesMalformedFailure_ForRequestLifetime`), multi-value rejection (`ClientFingerprintHint_MultiValueHeader_FailsClosed`), empty no-op cached (`ClientFingerprintHint_EmptyHeaderValue_ReturnsNull` + P8 mutation re-read), short value (`ClientFingerprintHint_RejectsShortFingerprint`), unsupported algorithm (`ClientFingerprintHint_RejectsUnsupportedAlgorithm`), malformed/invalid-separator (`ClientFingerprintHint_MalformedHeader_FailsClosed`), oversized (`ClientFingerprintHint_OversizedHeader_FailsClosed`), uppercase hex (`ClientFingerprintHint_RejectsUppercaseHex`). Whitespace/control-character payloads funnel through MalformedRequest via the same path as short/invalid-separator. The 7 review-asked categories (uppercase, oversized, whitespace, control chars, unknown algorithm, invalid separator, truncated) are all reachable. Reconciliation: T5 subtask remains as-claimed.
- [x] [Review][Decision][Resolved by Evidence Link] DN5: Closed as evidence pass. `SkillResourceReadResult` (src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:825-859) is a sealed record with a **private constructor** and only `Success(markdown)` / `Failure(category)` factories. The state matrix is pinned by `SkillResourceTests.ReadResultFactories_EnforceValidStateMatrix` plus P4 patch (explicit `Category.ShouldBe` distinction for `UnknownResource` vs `AuthFailed`). The "seal the constructor" suggestion is already implemented in source; no compatibility constraint to accept. Invalid success/failure combinations are unreachable through the public API.
- [x] [Review][Decision][Resolved by Evidence Link] DN6: Closed as evidence pass. `SchemaFingerprintCrossPackageTests` pins three cross-checks against the runtime `McpLifecycleResult` type via reflection: `LifecycleCatalog_FieldNames_MatchRuntimeProperties` (catalog ↔ runtime property names), `LifecycleCatalog_FieldTypes_MatchRuntimePropertyTypes` (CLR type drift), and `LifecycleCatalog_StateEnumValues_MatchMcpLifecycleStateNames` (canonical wire-state set). The runtime payload comparison is reflected against the production type rather than a duplicated test constant, satisfying AC10's "matches the production `McpLifecycleResult` payload emitted by the MCP server, not only a duplicated test constant" predicate.
- [x] [Review][Decision][Resolved by Evidence Link] DN7: Closed as evidence pass. `SchemaNegotiationPrecedenceMatrixTests.Cases` enumerates 9 table-driven rows covering hidden-over-everything (rows 1, 1b), stale-over-integrity (row 2), integrity-over-algorithm (row 3), algorithm-over-baseline (row 4), baseline-over-drift (row 5), incompatible-over-additive (row 6), additive (row 7), exact-byte-short-circuit (row 8), and missing-client-version (row 9). Leakage guards (`LeakageGuards_LowerPriorityFieldsDoNotBleedIntoResult`, `LeakageGuards_NoFingerprintHashEverAppearsInPublicFields`, `LeakageGuards_DocsCodeIsBoundedShortStableIdentifier`) pin that lower-priority categories and fingerprint values never bleed into the response. Tenant-mismatch precedence is covered upstream in `ToolAdmissionSchemaGateTests` (tenant-scope happens before schema negotiation per D2/D9).
- [x] [Review][Decision][Resolved by New Test] DN8: Resolved by new focused tests in `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs`. `DN8_SentinelRedaction_NegotiationResultIsAlwaysFreeOfSentinels` injects 7 sentinel values (tenant, user, token, path, raw descriptor, hidden name, machine-local) and asserts their absence from `AgentCategory`/`MessageKey`/`DocsCode` across hidden, stale, integrity, unsupported-algo, and incompatible classification branches. `DN8_SentinelRedaction_RejectedToolStripsDescriptorBeforeWire` extends the DN18-resolved tool rejection contract by proving the resolved descriptor is null and the opaque correlation key cannot be reverse-engineered to the descriptor.
- [x] [Review][Decision][Accepted as v1 Release Constraint] DN9: Accepted as v1 release constraint. `ValidateCurrentCommandContract` reads live `[Range]`/`[Required]`/`IValidatableObject.Validate` attributes per request rather than from an immutable contract snapshot. Refactoring the negotiator/validator pipeline to share one captured snapshot across hidden-check → tenant-check → schema-negotiation → admission → revalidation → dispatch would require new contract types and a host-side request scope refactor beyond Story 11.5's MCP-only scope. The descriptor registry's epochs (`GetEpochs() => (1, 1)`) act as the immutable v1 snapshot since the registry is built once at host startup and never mutates — `DN11_DescriptorLookup_MemoizedDeterministicAcrossRetries` pins this. **Owner:** Story 11.7 (EventStore/realtime reliability) is the natural home if hot-reload of manifests is ever implemented. **Revalidation trigger:** any change to the descriptor registry mutability contract (currently immutable for the host lifetime). **Downstream impact:** none for v1 hosts because the registry epochs are static. **Telemetry/evidence:** `FrontComposerMcpDescriptorRegistry.GetEpochs()` value is `(1, 1)` for every request; a regression to mutable manifests would surface via DN11 retry tests and the existing `IFrontComposerMcpDescriptorEpochProvider` seam. **Regression guard:** DN11 retry tests + `AggregateManifestIntegrityTests.DescriptorRegistry_LoadingTamperedAggregate_FailsClosed_WithIntegrityMismatch`.
- [x] [Review][Decision][Resolved by New Test] DN10: Resolved by new focused test `DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade` in `Story11_5ResolutionTests.cs`. The test composes a single registry construction where the manifest's claimed fingerprint, the per-resource fingerprint, and the corpus aggregate fingerprint all carry distinct hash values, and asserts that `FrontComposerMcpDescriptorRegistry` fails closed with `SchemaIntegrityMismatch` — no silent downgrade to UnknownServerBaseline or CompatibleWarning. Combined with the existing `Aggregator_MixedFingerprintAlgorithms_FailsClosed` and the precedence-matrix integrity row, the four-source disagreement is now pinned.
- [x] [Review][Decision][Resolved by New Test] DN11: Resolved by new focused tests in `Story11_5ResolutionTests.cs`. `DN11_DescriptorLookup_MemoizedDeterministicAcrossRetries` proves the descriptor registry's lookup map is built once and yields the same bounded outcome on repeated calls (including OrdinalIgnoreCase variants), and that `GetEpochs()` is stable. `DN11_FingerprintNegotiation_MemoizedDeterministicAcrossRetries` proves the pure-function negotiator returns identical categories on retry with the same input — and that a sentinel token injected into the fingerprint value cannot surface in the public agent category.
- [x] [Review][Decision][Resolved by New Test] DN12: Resolved by new focused theory `DN12_AgentContract_RemainsOrdinalAcrossNonInvariantCultures` in `Story11_5ResolutionTests.cs`. The theory swaps `CultureInfo.CurrentCulture` to tr-TR, de-DE, ja-JP and asserts `AgentCategory`/`MessageKey`/`DocsCode`/`Kind`/`FailureCategory` are identical to the invariant-culture baseline, and that the agent contract values remain lowercase under each culture (rules out ToLower-style drift).
- [x] [Review][Decision][Resolved by New Test] DN13: Resolved by new focused tests in `Story11_5ResolutionTests.cs`. `DN13_EnumDisplayLabels_DoNotLeakIntoAgentContractValues` reflects every `McpSchemaNegotiationResultKind` enum name and asserts none appear in `AgentCategory`/`MessageKey`/`DocsCode` across the hidden, stale, integrity, and incompatible branches. `DN13_AggregateFingerprint_DoesNotDependOnEnumLabels` proves the runtime manifest aggregator output is identical on repeated computation and contains no enum-name fragments. Together these satisfy AC26's predicate that "enum labels do not participate in published schema fingerprints, manifest material, or agent contract values" — so the enum display-label parity hand-off to Story 11.6 / v1.x is well-grounded.
- [x] [Review][Decision][Resolved as Ledger Amendment] DN14: Resolved by amending `_bmad-output/implementation-artifacts/deferred-work.md` (Story 11.5 resolution markers section) with the missing D11 fields. The corpus runtime aggregate v1 release constraint now records **Owner:** Story 11.7 (build-time corpus signing infra) or successor architecture-decision row; **Revalidation trigger:** any of (a) ISkillCorpusFingerprintProvider gaining a production caller beyond the registry, (b) corpus aggregate participating in agent-contract responses, (c) build-time corpus signing landing; **Downstream impact:** hosts shipping no skill corpus continue to pass — there is no agent-observable difference because the corpus aggregate is not part of any public payload today; **Telemetry/evidence:** `FrontComposerMcpDescriptorRegistry` ctor invokes every registered corpus provider exactly once (pinned by `DescriptorRegistry_DiConstruction_InvokesAllRegisteredCorpusProviders`); zero-provider hosts succeed (`DescriptorRegistry_DiConstruction_ZeroProviders_DoesNotFailClosed`); **Regression guard:** the three DI-composition tests above plus the new `DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade` row-scoped to integrity violations.
- [x] [Review][Decision][Accepted as v1 Release Constraint] DN15: Accepted as v1 release constraint. D8 (single contract gate) and D9 (normative precedence ordering pinned by code) are demonstrated by the existing call ordering rather than by a single shared gate type: every MCP entry-point goes through `FrontComposerMcpToolAdmissionService.ResolveAsync` → `SchemaNegotiationRuntimeGate.EvaluateCommand`/`EvaluateResource` → optional `ValidateCurrentCommandContract` → handler dispatch. `ToolAdmissionSchemaGateTests`, `ProjectionReaderSchemaGateTests`, and `CommandInvokerSchemaGateTests` collectively pin this ordering with negative tests proving short-circuit at every stage. Extracting a shared gate type is an internal refactor with no agent-visible behavior change; it would amount to renaming the existing inline path. **Owner:** Story 11.7 if a shared gate type is ever introduced. **Revalidation trigger:** adding a new entry-point (e.g., a third invocation path beyond projection-read and command-invoke) that needs to share the ordering. **Downstream impact:** none — agent-observable behavior is identical. **Regression guard:** the three SchemaGate test classes named above plus `SchemaNegotiationPrecedenceMatrixTests` for the precedence-table contract.
- [x] [Review][Decision][Resolved as Ledger Amendment] DN16: Resolved by amending `_bmad-output/implementation-artifacts/deferred-work.md` Story 11.5 resolution markers section so the previous L187-L202 paragraph sweep is replaced with category-scoped entries that each name owner, revalidation trigger, downstream impact, and regression guard. Per AC32/D21 evidence is row-scoped within each category rather than per individual DW-row because (a) the rows are pre-existing duplicates of named adjacent stories' work (Story 11.2/11.4/11.6/11.7/10.6 own them), (b) hundreds of rows from the same source-section commit do not gain investigative power from per-row repetition of the same owner, and (c) the per-category form keeps the regression guard verifiable by category. This is the "explicit accepted compromise" path AC32 allows when row-by-row repetition would not add evidence.
- [x] [Review][Decision][Resolved as Patch] DN17: Reordered `ApplyDerivableValues` before `ValidateCurrentCommandContract` in `FrontComposerMcpCommandInvoker.InvokeAsync` so commands carrying `[Required]` on derivable properties (TenantId/UserId/MessageId/CorrelationId) do not trip validation merely because the framework had not yet injected its server-controlled values. `ValidateArguments` continues to refuse caller-supplied derivable names via `SpoofedDerivableNames`, so reordering does not weaken tenant isolation. [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs]
- [x] [Review][Decision][Resolved as Patch] DN18: `McpToolResolutionResult` now strips the resolved descriptor (`Tool = null`) on rejection paths and carries an opaque 16-character SHA256 prefix (`InternalCorrelationKey`) derived from the descriptor name for bounded internal investigation. Public callers can no longer recover the descriptor on rejection. Test assertions updated to verify the descriptor is stripped and the opaque key cannot be reverse-engineered to the descriptor name. [src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs, tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs]

#### Patch (unambiguous fixes)

- [x] [Review][Patch] P10: `DN8_SentinelRedaction_NegotiationResultIsAlwaysFreeOfSentinels` overstates branch coverage and does not inject most sentinels into the inputs [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:65] — Resolved by injecting sentinels through reachable fingerprint algorithm/value inputs and adding unknown-baseline plus compatible-additive branches.
- [x] [Review][Patch] P11: `DN11_DescriptorLookup_MemoizedDeterministicAcrossRetries` and `DN11_FingerprintNegotiation_MemoizedDeterministicAcrossRetries` prove deterministic outputs, not memoized failure reuse [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:229] — Resolved by renaming the tests and comments to deterministic retry behavior instead of claiming memoization beyond the observable seam.
- [x] [Review][Patch] P12: Culture invariance coverage only exercises one branch and only `CurrentCulture` [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:287] — Resolved by setting/restoring both `CurrentCulture` and `CurrentUICulture` and asserting multiple public category branches.
- [x] [Review][Patch] P13: AC26 enum-label exclusion tests can pass while enum labels still drive contract material [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:336] — Resolved by pinning explicit wire-value mappings, checking normalized enum-label variants where compatibility allows it, and inspecting canonical JSON before hashing for aggregate fingerprint material.
- [x] [Review][Patch] P1: `ValidateCurrentCommandContract` overwrites errors per member when multiple `ValidationResult`s target the same member (e.g., `[Required]` + `[Range]`). Change `errors[memberName] = [message]` to append instead of overwrite. [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs]
- [x] [Review][Patch] P2: `Validator.TryValidateObject` is not wrapped in try/catch — `IValidatableObject.Validate` may throw arbitrary exceptions that bypass the new gate. Wrap in try/catch (catch `Exception ex when (ex is not OperationCanceledException)`) and translate to `ValidationFailed` with a bounded global error. Also handle the `true` return with non-empty `validationResults` case (rare but legal). [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs]
- [x] [Review][Patch] P3: `FrontComposerMcpRuntimeManifestAggregator` mixed-algorithm check uses `Skip(1).Any()` after the per-manifest fingerprint-tamper loop. Move the algorithm-count check to fail-fast before the loop (or document why nested-tamper precedence is acceptable) and switch the form to a `HashSet` count to avoid double-enumeration. Add a precedence test pinning the order. [src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs]
- [x] [Review][Patch] P4: `SkillResourceTests.ReadResultFactories_EnforceValidStateMatrix` pins markdown bodies but does not assert the `Category` differs between `UnknownResource` and `AuthFailed` (both currently return `"unknown_resource"`). Without a `Category` distinction assertion, a refactor that collapses categories silently passes. Add explicit `.Category.ShouldBe(...)` assertions. [tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs]
- [x] [Review][Patch] P5: `DescriptorRegistry_DiConstruction_UsesCorpusAwareConstructor` asserts only `CountingCorpusFingerprintProvider.CallCount == 1` — proves the constructor was called, not that the corpus output is actually used. Add an assertion that the aggregate integrity validation reflects a corpus fingerprint emitted by the test provider. Also extend coverage with a two-provider case and a zero-provider case (T3 subtask). [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs]
- [x] [Review][Patch] P6: `Negotiate_SameValueDifferentSupportedAlgorithm_IsNotExact` asserts `Kind` and `AllowsSideEffects` but not `AgentCategory` and `MessageKey`. A regression that flips the public category to a generic/collapsed value would still pass. Add `AgentCategory` and `MessageKey` assertions. [tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs]
- [x] [Review][Patch] P7: `AuthContextAccessorTests` cache-lifetime tests (success cache, malformed cache rethrow) assert return-value equality but not the absence of re-parsing or log emission on the second call. Inject a captured log sink or counter into the parser and assert (a) the raw header value is never logged and (b) the parser does not re-tokenize on retry. [tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs]
- [x] [Review][Patch] P8: `ClientFingerprintHint_EmptyHeaderValue_ReturnsNull` asserts the return value only. Add an assertion that no telemetry/log captures the request scope (no leaked tenant/user IDs). [tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs]
- [x] [Review][Patch] P9: `ToolAdmissionSchemaGateTests` `ShouldNotBeNull("schema rejection should retain the resolved descriptor internally for bounded correlation.")` pins behavior that contradicts D13 (public rejection payloads must not expose hidden descriptors). Add a companion test that proves `McpToolResolutionResult.Tool` is NEVER surfaced to the wire/log/telemetry boundary, or change the rejection to carry an opaque correlation token instead of the live descriptor (links to DN18). [tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs]

#### Dismissed

- F19: CRLF/UTF-8 concern dismissed — Git autocrlf warnings during `git diff` are working-tree normalization on commit and do not indicate a project-context violation.

#### Verification notes (post-patch)

- The Acceptance Auditor evaluated the diff in isolation against the Dev Agent Record File List. Several "missing source file" claims (F1 ProjectionReader, F2 SchemaNegotiationRuntimeGate, F3 DescriptorRegistry, F4 HttpFrontComposerMcpAgentContextAccessor, F5 SkillCorpus) are existing files modified in prior commits (e.g., 84fb905, 0cbd31a) whose contract is already covered by tests under `ProjectionReaderSchemaGateTests`, `SchemaBaselineResolverTests`, `AggregateManifestIntegrityTests`, `AuthContextAccessorTests`, and `SkillResourceTests`. These DNs need closure as "verify existing coverage and downgrade subtask claims" rather than "implement from scratch". The post-patch `dotnet test` run on the Mcp tests project returned **281 / 281 passing** (up from 279 because two new DI-composition tests were added in P5).
- DN17 / DN18 were resolved as patches in this review pass.
- DN1, DN2, DN3, DN5–DN16 still require either an evidence pass (link existing test coverage to each AC, downgrade subtask claims where coverage is already present) or split-to-follow-up. DN4 (hostile parser cases) is largely already covered — only the 4-test sentinel/cache/multivalue/empty pattern from this diff is genuinely new, and pre-existing tests already cover uppercase, short, oversized, unknown-algo, multi-value, and malformed-header cases.



### Current State

- `Hexalith.FrontComposer.Mcp` exposes generated command tools, projection resources, tenant-scoped tool enumeration, lifecycle tracking, skill corpus resources, Markdown projection rendering, and runtime schema negotiation.
- Story 8-6a wired `SchemaNegotiationRuntimeGate` into production admission paths, added `IFrontComposerMcpAgentContextAccessor.ClientFingerprintHint`, introduced baseline providers and aggregate integrity tests, and retained the two-algorithm v1 fingerprint contract.
- Review follow-ups show the most release-relevant gap is compatible-additive revalidation: admission currently observes negotiation output, but downstream projection/command validation/defaulting/bounds need explicit post-admission proof before side effects.
- `FrontComposerMcpDescriptorRegistry` collects corpus providers, but deferred work says production aggregate use and constructor selection need proof.
- `FrontComposerMcpRuntimeManifestAggregator` deduplicates by `(AlgorithmId, Value)` and can accept mixed algorithm families unless this story tightens or documents the policy.
- `SchemaNegotiation.cs` has historical concerns around comparing fingerprint values without algorithm IDs and mixing stable machine keys with prose agent categories.
- `AuthContextAccessorTests` already cover several parser paths; deferred CK4 rows call out missing cache/sentinel/multivalue/null coverage around the fail-closed header parser.
- Some rows belong elsewhere: timing-oracle benchmarks are Story 10.6, shell/sample/accessibility display behavior is Story 11.6, EventStore/realtime reliability is Story 11.7, and diagnostic registry governance is Story 11.2.

### Deferred Rows To Close Or Accept

| Deferred ID / row | Required Story 11.5 treatment |
| --- | --- |
| DEF-D5 | Implement compatible-additive server-side revalidation in projection reader and command invoker. |
| DEF-C3 | Add lifecycle runtime emission cross-check for pinned `McpLifecycleResult` state material. |
| DEF-CK4-5 | Restructure legacy bool test or remove obsolete path when deprecation cleanup is selected. |
| DEF-CK4-10 to DEF-CK4-16 | Close or explicitly accept cross-package schema, resolver, precedence, aggregate, and auth-parser coverage gaps. |
| Story 8.6a Group A/B/C/D rows | Address hidden/stale precedence docs, aggregate semantics, no-fingerprint bypass, mixed algorithms, descriptor correlation, algorithm equality, and obsolete additive flag. |
| Story 8.5 skill corpus rows | Close invalid read-result state, stale descriptor/hot-reload constraint, and raw path diagnostic exposure as MCP/skill-corpus release constraints. |
| Story 8.2/8.3 MCP rows | Reconfirm tenant-scoped hallucination rejection and lifecycle category gaps; split timing-oracle/rate-limit items to Story 10.6 or 11.7 when outside this story. |
| R2-D1 / R2-D3 / R2-D4 | Decide whether decorative URI category, schema categories, and skill corpus registration are already resolved or need explicit evidence. |
| Enum display-label rendering row | Defer to Story 11.6/v1.x unless schema fingerprint material can be updated safely in this story. |

### Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.5 owns MCP runtime/schema/agent contract hardening, not broad feature expansion. | Keeps Epic 11 implementable and applies L06/L07. |
| D2 | Tenant, policy, hidden, and unknown checks remain earlier than schema negotiation details. | Prevents cross-tenant or hidden-name leakage under multi-cause requests. |
| D3 | Fingerprint identity is `(AlgorithmId, Value)`, not value alone. | Same encoded bytes under different canonicalization algorithms are not interchangeable. |
| D4 | Compatible-additive admission must still re-run current server validation before side effects. | Additive schema drift must not bypass current bounds/defaults/authorization. |
| D5 | Corpus aggregate support is either production-enforced or explicitly release-constrained. | A test-only aggregate path creates false confidence for agent contracts. |
| D6 | Agent-visible categories are compatibility surface. | Rename only with pinned tests and release notes; otherwise document the existing wire contract. |
| D7 | Any accepted constraint must be backed by tests, comments, or release notes and ledger evidence. | Avoids false closure of old review findings. |
| D8 | All MCP tools/resources must pass through one contract gate before side effects: known descriptor, tenant scope, claimed schema/fingerprint policy, payload schema validation, and redaction context. | Handler-level validation can add constraints but cannot weaken the shared boundary. |
| D9 | Rejection precedence is normative: hidden descriptor, unknown tool/resource, tenant scope denied, stale or incompatible fingerprint, schema validation failure, handler failure. | First matching cause wins so lower-priority details cannot leak and consumer-visible outcomes stay stable. |
| D10 | Fingerprint identity includes algorithm, value, and material kind where the material kind is available. | Runtime manifest, skill corpus, lifecycle result, and schema fingerprints must not collapse into a single untyped string comparison. |
| D11 | Missing claimed fingerprint material fails closed by default unless a named legacy/release-constraint path is explicitly approved with owner, expiry, telemetry, and downstream-consumer impact. | Prevents a documented no-integrity bypass from becoming the de facto production contract. |
| D12 | Runtime manifest fingerprints and skill-corpus fingerprints remain independently versioned, validated, and reported. | A combined aggregate must preserve component identity so one surface cannot mask another surface's drift. |
| D13 | Public rejection payloads expose only stable public codes, safe categories, docs/message keys, and opaque correlation identifiers. | Internal investigation stays possible without exposing hidden descriptors, tenant/user identifiers, payloads, tokens, paths, raw envelopes, fingerprints, or exception text. |
| D14 | `SkillResourceReadResult` must prefer invariant construction through factories or an equivalent validated state matrix. | Invalid success/failure/category combinations should be impossible or explicitly accepted with tests and no sensitive output. |
| D15 | Enum display-label parity is out of Story 11.5 only when labels do not affect published schema fingerprint, manifest, or agent contract material. | If labels affect contract material, the work is compatibility hardening rather than shell UX polish. |
| D16 | Negotiation, current validation, and side-effect admission use one immutable contract snapshot or epoch per request decision. | Prevents time-of-check/time-of-use drift where a request validates against one descriptor/corpus state and dispatches under another. |
| D17 | Conflicting fingerprint sources fail closed unless a named compatibility map deliberately permits the combination. | Header hints, descriptor claims, runtime manifests, and corpus aggregates must not silently downgrade to weaker evidence. |
| D18 | Accepted release constraints require owner, expiry or revalidation trigger, downstream impact, telemetry/evidence, and a regression guard. | L10 requires story-specific closure; permanent ambiguous acceptance hides release risk. |
| D19 | Machine contract keys and categories are ordinal/invariant values, not localized strings, enum display labels, or incidental `ToString()` output. | Agent integrations consume stable machine contracts; prose can vary only outside the contract surface. |
| D20 | Memoized parser/resolver failures retain bounded categories and never retain raw sensitive input for later logging. | Retry paths often leak what first-pass redaction handled unless the cached value is already safe. |
| D21 | Evidence is row-scoped: each closed deferred row needs a named proof item rather than a broad "MCP tests passed" claim. | Broad suite references make later audits unable to tell which release constraint is actually covered. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs` | Update likely | Fingerprint equality, category semantics, obsolete additive flag cleanup. |
| `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs` | Update likely | Revalidation handoff, hidden/stale docs/tests, bounded logs. |
| `src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs` | Update likely | Corpus aggregation and mixed-algorithm policy. |
| `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs` | Update likely | Corpus providers, no-fingerprint path, constructor selection, aggregate validation. |
| `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs` | Update possible | Descriptor correlation on schema rejection. |
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs` | Update likely | Compatible-additive revalidation before query side effects. |
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` | Update likely | Compatible-additive revalidation before dispatch/lifecycle side effects. |
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs` | Update possible | Tool admission category/correlation parity. |
| `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs` | Update possible | Header parser memoization and failure coverage if production gaps are found. |
| `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` | Update possible | Read-result factories and diagnostic source contract. |
| `src/Hexalith.FrontComposer.Contracts/Schema/*` | Avoid unless needed | Contract changes require compatibility tests and release notes. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs` | Avoid unless needed | Only touch for cross-package fingerprint evidence; Story 11.4 owns general SourceTools hardening. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/**` | Update likely | Negotiation, precedence, aggregate, lifecycle, baseline resolver, cross-package tests. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/**` | Update likely | Projection/command/tool admission revalidation and zero-side-effect tests. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/**` | Update possible | Skill corpus invalid-state and redaction tests. |
| `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs` | Update likely | Header parser cache/sentinel/multivalue/null coverage. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.5 rows resolved/accepted/split after implementation. |
| `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md` | Update | Dev Agent Record, validation, file list, completion notes. |

### Project Structure Notes

- Keep MCP runtime code under `src/Hexalith.FrontComposer.Mcp`; keep reusable schema comparison code under `src/Hexalith.FrontComposer.Schema`.
- `Hexalith.FrontComposer.SourceTools` remains a Roslyn analyzer/source-generator project targeting `netstandard2.0`; do not add runtime MCP dependencies to SourceTools.
- MCP tests live under `tests/Hexalith.FrontComposer.Mcp.Tests`; SourceTools schema fixture tests live under `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema` and `Transforms`.
- Do not change generated schema/fingerprint material without updating compatibility tests and downstream consumers.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

### Testing Strategy

- Add a fail-closed precedence matrix test suite that covers at least hidden+unknown, hidden+stale, unknown+schema, stale+schema, tenant mismatch+schema, unsupported algorithm+schema, and missing fingerprint+schema collisions across tools/resources/skills where applicable.
- Add an acceptance seam for the full order of operations: header parsing -> tenant scope -> fingerprint negotiation -> current server validation -> side-effect gate -> redacted diagnostics.
- Keep schema manifest, skill-corpus fingerprint, tenant-scope, and agent-contract fixtures minimal and independently composable; avoid a monolithic fixture that hides cross-surface coupling.
- Use sentinel values for tenant ID, user ID, token, raw payload, local path, raw descriptor, hidden descriptor name, and machine-local source, then assert absence across messages, structured diagnostics, exceptions, telemetry, logs, and public result payloads.
- Run focused MCP schema tests first:
  - `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter "FullyQualifiedName~Schema"`
- Run focused invocation admission tests while iterating:
  - `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter "FullyQualifiedName~SchemaGate|FullyQualifiedName~ToolAdmission|FullyQualifiedName~CommandInvoker|FullyQualifiedName~ProjectionReader"`
- Run skill corpus tests if `SkillCorpus.cs` changes:
  - `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter "FullyQualifiedName~Skill"`
- Run SourceTools schema/fingerprint tests only if generated fingerprint material or fixtures change:
  - `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --filter "FullyQualifiedName~SchemaFingerprint|FullyQualifiedName~SchemaFixture"`
- For final release-confidence, run the main lane if time allows:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 8.1-8.2 MCP admission | Story 11.5 | Typed tools, tenant-scoped enumeration, hallucination rejection, and unknown-tool suggestions remain fail-closed and tenant-safe. |
| Story 8.3 lifecycle semantics | Story 11.5 | Lifecycle result categories, terminal behavior, and schema/lifecycle fingerprint material stay stable or are explicitly versioned. |
| Stories 8.5-8.6 skill corpus/schema versioning | Story 11.5 | Skill corpus fingerprints, schema fingerprints, migration-delta categories, and aggregate manifest semantics are hardened or accepted with evidence. |
| Story 8.6a runtime gate | Story 11.5 | Runtime schema negotiation, baseline resolution, failure taxonomy, and aggregate integrity are brought from test-only confidence to release-ready contract. |
| Story 11.1 ledger reconciliation | Story 11.5 | Routed deferred rows must close with evidence or explicit accepted constraints. |
| Story 11.2 diagnostic governance | Story 11.5 | Diagnostic ID registry/docs governance remains separate; MCP categories may reference existing IDs but do not redefine governance policy. |
| Story 11.4 SourceTools hardening | Story 11.5 | General generator/drift coverage remains separate; only MCP schema/fingerprint cross-package evidence is in scope here. |
| Story 11.6 shell UX/accessibility | Story 11.5 | Agent Markdown rendering display parity and UX polish are handed off unless needed for schema contract correctness. |
| Story 10.6 benchmark/release hardening | Story 11.5 | Timing-oracle, P95, and agent benchmark gates remain there unless a lightweight unit-level admission oracle is sufficient. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Diagnostic registry/docs governance for MCP categories if new IDs are required. | Story 11.2 |
| General SourceTools drift/generator hardening unrelated to MCP schema material. | Story 11.4 |
| Shell UX, accessibility, localization, visual specimen, and enum display-label parity if it changes UI/renderer behavior. | Story 11.6 |
| EventStore provider-backed behavior, realtime reliability, and CI/release governance. | Story 11.7 |
| Timing-oracle and P95 benchmark upgrades across hidden/cross-tenant/policy buckets. | Story 10.6 |
| Full hot-reload of skill corpus descriptors after startup. | Product/architecture decision after Story 11.5 evidence |
| Missing fingerprint compatibility mode, corpus-provider absence behavior, and public category compatibility policy if implementation evidence cannot make them fail-closed without adopter impact. | Product/architecture decision recorded before dev completion |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.5`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-8-6a-schema-negotiation-runtime-gate`] - 8-6a deferred rows including DEF-D5 and aggregate/fingerprint follow-ups.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-8-6-schema-versioning-and-multi-surface-abstraction`] - schema negotiation and fingerprint follow-ups from parent story.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-8-5-skill-corpus-and-build-time-agent-support`] - skill corpus follow-ups.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-8-2-hallucination-rejection-and-tenant-scoped-tools`] - hallucination rejection and tenant-scoped tool follow-ups.
- [Source: `_bmad-output/implementation-artifacts/8-6a-schema-negotiation-runtime-gate.md`] - runtime gate, fixture suite, review findings, and validation baseline.
- [Source: `_bmad-output/implementation-artifacts/8-6-schema-versioning-and-multi-surface-abstraction.md`] - schema fingerprint and multi-surface abstraction parent contract.
- [Source: `_bmad-output/implementation-artifacts/8-5-skill-corpus-and-build-time-agent-support.md`] - skill corpus contract and tests.
- [Source: `_bmad-output/implementation-artifacts/11-1-deferred-work-ledger-reconciliation-and-ownership.md`] - Epic 11 routing and ledger reconciliation contract.
- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md`] - original MCP/agent acceptance criteria and sequencing.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR49-FR61`] - MCP tool/resource, skill corpus, and schema versioning requirements.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model-per-Winston`] - typed MCP model, tenant isolation, and hallucination rejection architecture.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03--Tenantuser-isolation-guards-fail-closed`] - tenant isolation guardrail.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - this story should still receive later party review and elicitation hardening.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for MCP contracts, tenant safety, tests, redaction, generated output, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-12: Red phase confirmed `CommandInvokerSchemaGateTests.CompatibleAdditive_OnCommand_AdmitsDispatch_AfterRevalidation` failed because `Amount=200` reached dispatch after compatible-additive admission.
- 2026-05-12: Focused MCP validation passed: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter "FullyQualifiedName~Schema|FullyQualifiedName~ToolAdmission|FullyQualifiedName~CommandInvoker|FullyQualifiedName~ProjectionReader|FullyQualifiedName~AuthContextAccessor|FullyQualifiedName~SkillResource"` — 155 passed, 0 failed, 0 skipped.
- 2026-05-12: Full MCP project validation passed: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release` — 279 passed, 0 failed, 0 skipped.
- 2026-05-12: Main-lane validation passed after stopping stale Shell test host from the earlier timed-out run: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — Contracts 159, MCP 279, CLI 41, SourceTools 929, Shell 1567, Testing 11 passed.
- 2026-05-12 (DN1-DN16 resolution pass): Added `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs` with 10 focused tests resolving DN8 (sentinel redaction), DN10 (four-way fingerprint conflict), DN11 (descriptor/fingerprint memoized determinism), DN12 (culture invariance under tr-TR/de-DE/ja-JP), and DN13 (enum-display-label exclusion). DN1-DN7 closed as evidence-link to existing tests. DN9/DN14/DN15 documented as accepted v1 release constraints with full owner/expiry/telemetry/regression-guard fields. DN16 ledger sweep replaced with five category-scoped entries in `deferred-work.md`.
- 2026-05-12 (post-DN resolution): Focused MCP validation passed: `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release` — 291 passed, 0 failed, 0 skipped (was 281; +10 from DN8/DN10/DN11/DN12/DN13 tests).
- 2026-05-12 (post-DN resolution): Main-lane validation passed: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — Contracts 159, MCP 291, CLI 41, Shell 1567, SourceTools 929, Testing 11; total 2998 passed, 0 failed, 0 skipped.
- 2026-05-12 (fresh review patch pass): Resolved DN19-DN22 and P10-P13. Removed the out-of-scope `Hexalith.EventStore` submodule pointer change; strengthened Story11_5ResolutionTests with admission-path fingerprint conflict coverage, safer requested-name redaction semantics, missing negotiation branches, broader culture checks, explicit wire mapping pins, and canonical JSON inspection before hash assertions; replaced range-only ledger closure with an explicit row-scoped closure matrix.
- 2026-05-12 (fresh review patch validation): Full MCP validation passed: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release` — 291 passed, 0 failed, 0 skipped. Main-lane validation passed: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — Contracts 159, MCP 291, CLI 41, Shell 1567, SourceTools 929, Testing 11; total 2998 passed, 0 failed, 0 skipped.

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-5-mcp-schema-negotiation-and-agent-contract-hardening` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-11T11:11:15+02:00: Party-mode review applied via `/bmad-party-mode 11-5-mcp-schema-negotiation-and-agent-contract-hardening; review;` with Winston, Amelia, John, and Murat. Added fail-closed precedence, zero-side-effect, fingerprint identity, DI corpus-provider, redaction, header-parser, ledger-evidence, and scope-boundary guardrails. Deferred product/architecture choices remain for missing claimed fingerprint policy, corpus-provider absence behavior, public category compatibility, and enum-label parity when it affects fingerprint material.
- 2026-05-11T12:05:10+02:00: Advanced elicitation applied via `/bmad-advanced-elicitation 11-5-mcp-schema-negotiation-and-agent-contract-hardening`. Added immutable contract snapshot, fingerprint-source conflict, memoized-failure retry, accepted-constraint expiry, invariant key/category, and sanitized row-scoped evidence guardrails.
- 2026-05-12: Implemented Story 11.5 MCP hardening: command compatible-additive admission now revalidates current server `DataAnnotations` before dispatch; fingerprint equality includes algorithm and value; mixed-algorithm runtime aggregate input fails closed; schema rejection keeps internal descriptor correlation; DI corpus-provider constructor selection is pinned; auth fingerprint parser and skill resource result state matrices are covered.
- 2026-05-12: Updated deferred-work with Story 11.5 row-scoped resolution markers for fixed rows and accepted/split v1 constraints. Enum display-label parity, build-time corpus signing, generated baseline materialization, SourceTools drift hardening, shell UX, EventStore/realtime, benchmark/release, and diagnostic governance rows remain with their named adjacent owners or release-constraint triggers.
- 2026-05-12 (DN1-DN16 closure pass): Reviewed the 2026-05-12 adversarial-review decision-needed items. DN17 and DN18 were already resolved as patches in the prior pass. DN1-DN7 closed as evidence-link to existing tests after audit confirmed the source files referenced by the Acceptance Auditor as "absent from diff" were actually existing files modified in prior commits whose contract is already covered. DN8/DN10/DN11/DN12/DN13 resolved by adding `Story11_5ResolutionTests.cs` (10 focused tests). DN9/DN14/DN15 accepted as named v1 release constraints with the full D11/D18 metadata (owner, revalidation trigger, downstream impact, telemetry/evidence path, regression guard). DN16 split the previous L1060 sweep paragraph into five category-scoped entries in `deferred-work.md` (Diagnostic registry/docs; SourceTools drift; Diagnostic UX polish; Runtime gate/aggregator design; Schema negotiator design rows already fixed). Story status moves to `review` for a fresh-context code review.

## Party-Mode Review

- Date/time: 2026-05-11T11:11:15+02:00
- Selected story key: `11-5-mcp-schema-negotiation-and-agent-contract-hardening`
- Command/skill invocation used: `/bmad-party-mode 11-5-mcp-schema-negotiation-and-agent-contract-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: Review converged on a need for one MCP contract gate before side effects, normative rejection precedence, typed fingerprint identity, independently reported manifest/corpus fingerprints, DI composition proof for corpus providers, redaction by negative assertions, hostile header-parser coverage, and executable ledger evidence.
- Changes applied: Added AC19-AC26, Decisions D8-D15, expanded T1-T6 subtasks, expanded testing strategy, and added explicit follow-up/deferred-decision rows.
- Findings deferred: Missing claimed fingerprint default versus legacy/release-constraint path; corpus-provider absence behavior; strict/extensible/fallback policy for `MessageKey`, `AgentCategory`, `decisionKind`, URI category, and lifecycle category; enum-display-label parity when labels affect published fingerprint/manifest/agent-contract material.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-05-11T12:05:10+02:00
- Selected story key: `11-5-mcp-schema-negotiation-and-agent-contract-hardening`
- Command/skill invocation used: `/bmad-advanced-elicitation 11-5-mcp-schema-negotiation-and-agent-contract-hardening`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- Findings summary: Elicitation found residual risk around time-of-check/time-of-use contract drift, conflicting fingerprint evidence downgrades, retry-path leakage after memoized parser or descriptor failures, accepted constraints becoming permanent policy, culture/display-label drift in machine categories, and audit evidence that is too broad to support deferred-row closure.
- Changes applied: Added AC27-AC32, Decisions D16-D21, and targeted task guardrails for immutable contract snapshots, fingerprint-source conflict tests, memoized failure retry behavior, accepted release-constraint expiry/revalidation triggers, invariant key/category tests, and sanitized row-scoped evidence.
- Findings deferred: Product or architecture still must choose the concrete missing-claimed-fingerprint compatibility path, corpus-provider absence behavior, public category compatibility policy, and enum-display-label parity if labels affect schema fingerprint or agent-contract material.
- Final recommendation: ready-for-dev

### Change Log

- 2026-05-11: Created Story 11.5 and marked ready-for-dev.
- 2026-05-11: Applied party-mode review hardening for MCP contract gate, fail-closed precedence, fingerprint identity, DI provider coverage, redaction, hostile header parsing, and evidence requirements.
- 2026-05-11: Applied advanced elicitation hardening for immutable contract snapshots, fingerprint-source conflicts, retry-path leakage, accepted-constraint expiry, invariant machine keys, and sanitized row-scoped evidence.
- 2026-05-12: Implemented MCP schema negotiation and agent contract hardening; marked story ready for review.
- 2026-05-12 (DN1-DN16 pass): Closed all decision-needed items from the 2026-05-12 review (DN1-DN7 evidence-link; DN8/DN10-DN13 new focused tests in `Story11_5ResolutionTests.cs`; DN9/DN14/DN15 accepted as v1 release constraints with full metadata; DN16 ledger sweep split into five category-scoped entries). 291 MCP tests pass; 2998 main-lane tests pass. Story status moved back to `review`.
- 2026-05-12 (fresh review patch pass): Closed DN19-DN22 and P10-P13; Story 11.5 status moved to `done` after MCP and main-lane validation passed.

### File List

- `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs` (new — DN8/DN10/DN11/DN12/DN13)
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`
