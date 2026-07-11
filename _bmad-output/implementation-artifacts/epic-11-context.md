# Epic 11 Context: Architecture Review Remediation

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the remaining architecture-review risks without reopening completed product epics. It hardens runtime reliability, security, realtime and MCP behavior, visual governance, adopter testing, route activation, package boundaries, shell layering, logging, and enforcement so FrontComposer is safe to consume and release.

## Stories

- Story 11.0: Command/projection route-contract decision gate
- Story 11.1: Token lifecycle and circuit-safe EventStore auth
- Story 11.2: Projection realtime resilience
- Story 11.4: Security-validation hardening
- Story 11.3: MCP cross-request lifecycle and operability
- Story 11.5: Dead-CSS remediation and visual-conformance guards
- Story 11.6: Testing harness failure modes
- Story 11.7: Command/projection route-contract implementation
- Story 11.9: Shell layering declaration and route/label relocation
- Story 11.15: Storage scope and snapshot publisher consolidation
- Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation
- Story 11.17: Mechanical one-type-per-file split
- Story 11.18: LoggerMessage migration for warnings and hot paths
- Story 11.19: Enforcement and policy alignment
- Story 11.8: Contracts kernel split decision and compatibility plan
- Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface
- Story 11.12: Relocate runtime and testing-owned types out of Contracts
- Story 11.13: Decompose `QueryRequest` through the HFC0001 migration path
- Story 11.14: Update architecture, project context, UX trace, and package compatibility docs

## Requirements & Constraints

This epic is release-risk remediation, not new product scope. Each implementation story must use focused Given/When/Then acceptance criteria, preserve existing command/query/projection behavior, and add durable evidence for the defect class it closes.

Security and support-safety apply throughout. Auth, redirects, storage scopes, MCP denial paths, lifecycle tracking, logs, telemetry, snapshots, and Testing evidence must fail closed where appropriate and must not expose raw tokens, secrets, payloads, stack traces, unrestricted PII, or raw local paths.

Testing must let adopters simulate command rejection, timeout, stall-at-`Syncing`, authorization-policy states, and per-request paging/filter/sort outcomes. Changed builders, assertions, fakes, and fault/evidence paths require direct tests, default redaction of configured tenant/user identifiers (including property and dictionary keys), structural secret redaction, bounded path evidence, and intentional Testing public-API baseline updates.

Visual fixes require guard-backed evidence for dead scoped CSS, unlinked stylesheets, legacy Fluent tokens, and accessibility-sensitive behavior. Public API, schema, CLI/MCP wire shapes, diagnostics, generated output, and package inventory remain controlled contracts; changes require intentional baselines and migration or compatibility evidence. Build policy remains .NET 10, `.slnx`, centralized dependencies, nullable code, and warnings as errors, with no global warning/analyzer suppression used to hide remediation.

## Technical Decisions

The canonical generated command route is `/commands/{BoundedContext}/{CommandTypeName}`. Palette entries, empty-state CTAs, and generated command pages must converge on it. Module tabs use `/{module}/{tab}`, with projection flyouts secondary to the module workspace.

The implemented package target keeps both `Contracts` TFMs UI-clean and places Blazor/Fluent rendering contracts in packable net10-only `Contracts.UI`. `SourceTools` remains a packable netstandard2.0 analyzer referencing only the kernel. Shell owns runtime options, registries, and Fluxor actions; Testing owns `InMemoryStorageService`. Public moves require package-consumer, public-API, documentation, release-inventory, and deprecation evidence.

MCP cross-request state uses a singleton state store behind a scoped facade; it must not capture scoped admission services. EventStore token acquisition must work safely in interactive circuits with expiry and sign-out eviction. Projection realtime must recover beyond the default retry ladder, restart after closed connections, and align disposal/cache synchronization. Fail-closed and hot-path logging uses sanitized source-generated events.

`ProjectionQuery` now owns canonical query criteria and is composed through `QueryRequest.Create`; HFC0001/CS0618 preserves the v1.12 flattened source/deconstruction surface and flat JSON through the `2.0.0` transition. Shell boundaries place telemetry cross-cutting, connection/polling workers in infrastructure, and route/label helpers outside render components; duplicated scope, snapshot, fatal-exception, hydration, JSON, and literal-escaping behavior should be consolidated with focused equivalence tests.

## UX & Interaction Patterns

Realtime and command surfaces must expose reconnecting, fallback, degraded, pending, rejected, and confirmed states without treating HTTP acceptance or a projection nudge as confirmed success. UI remediation uses FrontComposer/Fluent UI Blazor v5, Fluent 2 tokens, accessible names and keyboard behavior, stable selectors, reduced-motion and forced-colors support, plus rendered-DOM, computed-style, bUnit, e2e, or Governance evidence appropriate to the change.

## Cross-Story Dependencies

Story creation follows the Epic 11 implementation-order table, not heading or numeric order: 11.1, 11.2, 11.4, 11.3, 11.5, 11.6, 11.7; then 11.9/11.15/11.16; then the split children of 11.17, 11.18, and 11.19; finally 11.11-11.14.

Story 11.0 and the module-tab IA gate are resolved prerequisites for 11.7. Story 11.8 approved the split; 11.11 implemented Contracts.UI but remains status-blocked on Story 11.14 evidence, 11.12 is done, and 11.13 is in review. Story 11.14 owns the explicit inventory, migration/docs trace, `v1.12.0` compatibility baseline, and approved `2.0.0` posture; it does not recreate the assembly changes. Stories 11.17, 11.18, and 11.19 remain decomposition parents requiring independently reviewable children.
