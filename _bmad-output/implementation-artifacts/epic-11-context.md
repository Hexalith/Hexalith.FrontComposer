# Epic 11 Context: Release Readiness Remediation Program (post-MVP quality hardening)

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the release-readiness risks identified by the architecture-quality review and subsequent acceptance evidence. It hardens runtime reliability, security, package and route contracts, testing support, maintainability enforcement, release identity, and artifact integrity before final v1.0 acceptance without adding product scope or reopening completed Epics 1–10.

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
- Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface
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
- Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity
- Story 11.25: Current EventStore Release Identity and Evidence
- Story 11.26: Analyzer Identifier Inventory Reconciliation
- Story 11.27: Generated Command Route Acceptance Locator
- Story 11.28: FC-NIP Semantic Fixture Alignment
- Story 11.29: Fallback Refresh and View Registration Correctness
- Story 11.30: Testing and MCP Boundary Hardening
- Story 11.31: Canonical Correlation Pseudonymization
- Story 11.32: Epic 11 Artifact Integrity Enforcement

## Requirements & Constraints

Remediation must preserve public API, schema, diagnostic, generated-output, CLI/MCP wire, and package compatibility unless a deliberate migration and baseline update is approved. Runtime changes must recover safely, prevent stale or cross-tenant state, maintain scoped-lifetime discipline, and never treat HTTP acceptance or a projection nudge as confirmed command success. Security, logs, telemetry, MCP responses, evidence, and snapshots must fail closed and exclude tokens, secrets, payloads, stack traces, unrestricted PII, and raw correlation identifiers.

Build policy remains .NET 10, `.slnx`, centralized dependencies, nullable analysis, `TreatWarningsAsErrors=true`, and `AnalysisMode=Recommended` with built-in analyzers only. Exceptions must be narrow, owned, and reviewable; broad suppressions are forbidden. Changed surfaces require focused tests plus applicable default, Governance, Contract, snapshot, PublicAPI, compatibility, generated-output, accessibility, and release-evidence gates. Every review defect class needs a durable regression guard and verifiable adopter- or operator-visible outcome.

## Technical Decisions

Generated commands use `/commands/{BoundedContext}/{CommandTypeName}`. Module tabs use `/{module}/{tab}` with one primary module entry and projection flyouts as secondary navigation. The UI-neutral `Contracts` kernel stays netstandard2.0-clean; net10-only `Contracts.UI` owns Blazor and Fluent rendering contracts, and SourceTools depends only on the kernel.

MCP lifecycle state must survive request scopes without captive scoped dependencies. EventStore authentication must handle circuit lifetime, expiry, sign-out, and eviction. Projection realtime uses bounded, observable fallback polling and automatic recovery. Fallback comparison must detect material content changes, while view registration must reject or safely replace conflicting scope ownership. Logging uses source-generated sites, sanitized structured values, and one deterministic correlation pseudonymization contract.

The active EventStore release identity is selected by a successor record that binds the exact source, package, Builds catalog, approvals, and live evidence; historical authorization remains immutable and is not projected onto the current target. Artifact validation fails closed on missing, stale, contradictory, or non-resolving evidence.

## UX & Interaction Patterns

Use FrontComposer and Fluent UI Blazor v5 with Fluent 2 tokens. Reconnecting, fallback, degraded, pending, rejected, and confirmed states must remain visible and accessible without relying on color, motion, hover, or noisy repeated announcements. Route activation and invalid-route fallback require deterministic focus and unambiguous route-level heading evidence. UI changes retain WCAG 2.2 AA keyboard, reflow, zoom, reduced-motion, and forced-colors behavior.

## Cross-Story Dependencies

The route decision and information-architecture gate precede route implementation; the Contracts split decision precedes its assembly, relocation, migration, and documentation work. Stories 11.17, 11.18, and 11.19 are nonimplementable parents; only their named children carry delivery state. Logging ownership proceeds security/fail-closed, then hot paths, then residual warning-or-higher sites.

The current remediation sequence is 11.25; then 11.26–11.28; an evidence-based acceptance checkpoint; 11.29–11.31; 11.32; then final acceptance. Stories inside each three-story group may proceed in parallel. Artifact integrity follows runtime and evidence hardening so it validates the final state.
