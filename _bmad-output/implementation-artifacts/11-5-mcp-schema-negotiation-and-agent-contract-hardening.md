# Story 11.5: MCP Schema Negotiation and Agent Contract Hardening

Status: ready-for-dev

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

---

## Tasks / Subtasks

- [ ] T1. Inventory and classify Story 11.5 deferred rows (AC1, AC17, AC18)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [ ] Capture MCP/schema/skill-corpus rows from Stories 8.1 through 8.6a, including DEF-D5, DEF-C3, DEF-CK4-*, DEF-2, Story 8.5 skill corpus rows, and Story 8.2/8.3 agent-surface rows that are not already owned by Story 10.6 or 11.7.
  - [ ] Classify each row as fix now, accept with evidence, split to Story 11.6/11.7/10.6, or leave blocked with a named product/architecture decision.
  - [ ] Preserve historical review text; append resolution markers rather than rewriting the ledger.

- [ ] T2. Implement compatible-additive revalidation and zero-side-effect coverage (AC2, AC9, AC16)
  - [ ] Unskip or replace the existing `CompatibleAdditive` projection/command admission tests that were left pending DEF-D5.
  - [ ] Thread a post-admission revalidation/defaulting/bounds step into `FrontComposerMcpProjectionReader` and `FrontComposerMcpCommandInvoker` before query/dispatch side effects.
  - [ ] Ensure `CompatibleWarning`/`EnumChanged` keeps the schema-drift signal instead of degrading to generic `ValidationFailed` when old enum values are rejected.
  - [ ] Add side-effect spies for command dispatch, query execution, lifecycle mutation, cache writes, renderer buffers, and token relay where feasible.
  - [ ] Keep hidden/unknown and tenant/policy checks ahead of schema details.

- [ ] T3. Harden fingerprint and aggregate semantics (AC3-AC6, AC8, AC10)
  - [ ] Compare `SchemaFingerprint.AlgorithmId` and `Value` together in `SchemaNegotiation.cs`.
  - [ ] Decide mixed-algorithm aggregate policy in `FrontComposerMcpRuntimeManifestAggregator`; reject or document same-algorithm-only input.
  - [ ] Close or document the no-claimed-fingerprint bypass in `FrontComposerMcpDescriptorRegistry`.
  - [ ] Route `ISkillCorpusFingerprintProvider` outputs into a production aggregate integrity path, or record the release constraint with tests proving the current seam.
  - [ ] Consolidate or production-test the `FrontComposerMcpDescriptorRegistry` constructor selected by `AddFrontComposerMcp`.
  - [ ] Add runtime lifecycle payload cross-checks for `McpLifecycleResult` so pinned state material cannot drift silently.

- [ ] T4. Tighten schema rejection, descriptor correlation, and agent categories (AC7, AC9, AC11, AC13, AC16)
  - [ ] Preserve descriptor correlation for schema rejection without exposing hidden names or tenant-specific data to agents.
  - [ ] Clarify `MessageKey`, `AgentCategory`, and `decisionKind` naming; change only if compatibility risk is acceptable and tests pin the public wire shape.
  - [ ] Document and test hidden/stale/schema precedence for production paths where stale descriptor state is upstream of `SchemaNegotiationRuntimeGate`.
  - [ ] Ensure schema-failure logs use bounded category/message/docs fields only.
  - [ ] Review `ProtocolUriCategory` and lifecycle categories; either specialize them with evidence or accept them as v1 decorative metadata.

- [ ] T5. Harden skill corpus and auth/header parser contracts (AC12-AC15)
  - [ ] Replace `SkillResourceReadResult` invalid-state construction with success/failure factories, or document the compatibility constraint.
  - [ ] Add tests for skill corpus diagnostics source redaction or no-log/no-agent-exposure contract.
  - [ ] Add `AuthContextAccessor` coverage for cached success, malformed sentinel rethrow, multi-valued headers, uppercase hex rejection, null/empty header no-op, and oversized lowercase values.
  - [ ] Decide whether enum display-label parity in MCP Markdown rendering is Story 11.5 scope; if not, record a handoff to Story 11.6 or v1.x because schema fingerprint material changes.

- [ ] T6. Update docs, ledger, and validation evidence (AC1, AC17, AC18)
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` with resolution/acceptance/split markers for every Story 11.5-owned row.
  - [ ] Update focused comments or release notes only where behavior changes need maintainer-facing explanation.
  - [ ] Record exact validation commands and outcomes in this story's Dev Agent Record.
  - [ ] Move Story 11.5 to `review` only after implementation and validation evidence are complete.

---

## Dev Notes

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

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-5-mcp-schema-negotiation-and-agent-contract-hardening` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Change Log

- 2026-05-11: Created Story 11.5 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md`
