# Epic 11 Context: Release Readiness Remediation Program

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the remaining release-readiness risks without reopening completed product epics. It is
organized into runtime reliability/security, adopter testing/route integrity, contracts/package
boundary, and maintainability/enforcement workstreams. Stories 11.17, 11.18, and 11.19 are
nonimplementable decomposition parents; only their materialized children carry delivery state.

Epic 11 **does not reopen completed Epics 1–10**; it consumes completed Epic 10 governance evidence
where a story cites it.

> The workstream/current-state table in `_bmad-output/planning-artifacts/epics.md` is authoritative for
> what to work on next. **Do not infer an implementation candidate from file order, numeric sort, or a
> decomposition-parent heading**, and do not infer it from the flat list below, which is an index of
> story identity only and carries no delivery state. Parent Stories 11.17, 11.18, and 11.19 must
> **never** receive backlog, ready-for-dev, or review status.

## Stories

- Story 11.0: Command/projection route-contract decision gate
- Story 11.1: Token lifecycle and circuit-safe EventStore auth
- Story 11.2: Projection realtime resilience
- Story 11.3: MCP cross-request lifecycle and operability
- Story 11.4: Security-validation hardening
- Story 11.5: Dead-CSS remediation and visual-conformance guards
- Story 11.6: Testing harness failure modes
- Story 11.7: Command/projection route-contract implementation
- Story 11.8: Contracts kernel split decision and compatibility plan
- Story 11.9: Shell layering declaration and route/label relocation
- Story 11.11: Create Contracts.UI assembly and migrate the Blazor rendering surface
- Story 11.12: Relocate runtime and testing-owned types out of Contracts
- Story 11.13: Decompose `QueryRequest` through the HFC0001 migration path
- Story 11.14: Update architecture, project context, UX trace, and package compatibility docs
- Story 11.15: Storage scope and snapshot publisher consolidation
- Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation
- Story 11.17: Mechanical one-type-per-file split
- Story 11.17a: CLI package split
- Story 11.17b: SourceTools package split
- Story 11.17c: MCP/runtime split and benchmark-harness relocation
- Story 11.17d: Shell interface, implementation, and DTO bundle split
- Story 11.18: LoggerMessage migration for warnings and hot paths
- Story 11.18a: Fail-closed and security log sites
- Story 11.18b: Residual warning-and-above log sites
- Story 11.18c: Hot-path log sites
- Story 11.19: Enforcement and policy alignment
- Story 11.19a: Doc-comment enforcement realignment
- Story 11.19b: AppHost NuGet audit suppression
- Story 11.19c: Localization and identifier alignment
- Story 11.19d: Analyzer-elevation decision gate
- Story 11.20: Recommended analyzer policy and exception ledger
- Story 11.21: Recommended analyzer product and generator burn-down
- Story 11.22: Recommended analyzer test and sample burn-down
- Story 11.23: Recommended analyzer repository activation
- Story 11.24: Adopt the owner-approved EventStore runtime identity

## Requirements & Constraints

This epic remediates release risks rather than adding product scope. Implementation children must use
focused Given/When/Then acceptance criteria, preserve command/query/projection behavior, and add durable
evidence for each defect class. Security-sensitive paths fail closed and never expose raw tokens,
secrets, payloads, stack traces, unrestricted PII, or local paths.

Security-sensitive detail that implementation children still depend on: logs and telemetry default to
redacting configured tenant/user identifiers **including property and dictionary keys**, redact secrets
structurally, and bound path evidence. `TestFaultEvidenceRecorder` is evidence-only — it records
redacted fault observations and does **not** inject faults. Changed builders, assertions, fakes, and
evidence paths require direct tests and intentional Testing public-API baseline updates. Visual fixes
require guard-backed evidence for dead scoped CSS, unlinked stylesheets, legacy Fluent tokens, and
accessibility-sensitive behavior; the four blind-spot guard classes (unlinked stylesheets, dead scoped
CSS, parameter-splat surfaces, cross-request lifetimes) each gain a durable Governance test.

Public APIs, schema, CLI/MCP wire shapes, diagnostics, generated output, dependency identity, and package
inventory remain controlled contracts. Changes require intentional baseline, migration, compatibility,
or provenance evidence. Builds use .NET 10, `.slnx`, centralized dependencies, nullable code, warnings as
errors, and no broad warning or analyzer suppression.

Mechanical one-type-per-file work is package-scoped and behavior-preserving. Each materialized child
retains its approved narrow exceptions and proves organization, behavior, and public/package contract
stability through its package-specific validation lane.

## Technical Decisions

The canonical generated command route is `/commands/{BoundedContext}/{CommandTypeName}`. Module tabs use
`/{module}/{tab}` with projection flyouts secondary. The package boundary keeps the wire/attribute/schema
kernel UI-clean and places Blazor/Fluent rendering contracts in net10-only `Contracts.UI`; SourceTools
remains a packable netstandard2.0 analyzer.

`ProjectionQuery` owns canonical query criteria and is composed through `QueryRequest.Create`;
HFC0001/CS0618 preserves the v1.12 flattened source, deconstruction, and flat JSON shape throughout 2.x,
**with removal targeted for `3.0.0`** — do not remove that surface earlier. Shell boundaries place
telemetry cross-cutting, connection/polling workers in infrastructure, and route/label helpers outside
render components.

MCP cross-request state uses a singleton store behind a scoped facade and **must not capture scoped
admission services**. EventStore token acquisition must work safely in interactive circuits with expiry
and sign-out eviction, and projection realtime must recover beyond the default retry ladder. Logging ownership is exclusive:
security/fail-closed sites first, command-lifecycle/projection/polling hot paths second, and residual
Warning/Error/Critical sites last. Recommended analyzer adoption proceeds sequentially through Stories
11.20–11.23 with separate approvals; Story 11.23 gates v1.0 publication.

Shared-catalog compatibility is semantic, evaluated against every actual Builds selector in the bounded
depth-1/2 committed-object graph. Exact commits and catalog fingerprints are provenance, never
compatibility allowlists. The implemented local engine covers bounded graph collection and semantic
validation from FrontComposer's versioned policy. The approved GOV-1 architecture additionally requires
immutable base/before policy activation and release handoffs; those workflow/release portions remain
separately gated. No nested submodule initialization is permitted.

## UX & Interaction Patterns

Realtime and command surfaces expose reconnecting, fallback, degraded, pending, rejected, and confirmed
states without treating HTTP acceptance or a projection nudge as confirmed success. Visual remediation
uses Fluent 2 tokens, accessible names and keyboard behavior, stable selectors, reduced-motion and
forced-colors support, and the verification medium appropriate to the behavior.

## Cross-Story Dependencies

Package-boundary and release-readiness children depend on GOV-1 semantic catalog compatibility and
dependency provenance being green on the exact candidate revision. Legitimate Builds-pointer advances
require matching semantic policy and promotion evidence, not a historical SHA allowlist. GOV-1 does not
authorize editing the Builds submodule or changing dependency versions.

Story 11.0 and FC-IA-1 are resolved prerequisites for completed Story 11.7; Story 11.8 is the resolved
decision prerequisite for completed Stories 11.11–11.14. Story 11.19d is a decision record and may create
later implementation work only through a new, explicitly approved story.

Story 11.18c freezes hot-path ownership before 11.18b claims residual Warning+ sites. Stories 11.20–11.23
run sequentially after the approved 11.19d decision. Story 11.24 is separately gated by durable
EventStore Story 1.20 migration authority and does not authorize adapter, topology, rollback, or
deployment redesign.
