# Epic 11 Context: Architecture Review Remediation

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the architecture-review remediation needed before v1.0 without reopening completed product epics. It hardens runtime blind spots, security validation, realtime and MCP reliability, visual governance, Testing harness failure modes, route activation, package boundaries, shell layering, logging, and policy enforcement so the existing FrontComposer surface is release-ready, supportable, and safe for adopter modules such as Hexalith.Tenants.

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
- Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface
- Story 11.12: Relocate runtime and testing-owned types out of Contracts
- Story 11.13: Decompose `QueryRequest` through the HFC0001 migration path
- Story 11.14: Update architecture, project context, UX trace, and package compatibility docs
- Story 11.9: Shell layering declaration and route/label relocation
- Story 11.15: Storage scope and snapshot publisher consolidation
- Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation
- Story 11.17: Mechanical one-type-per-file split
- Story 11.18: LoggerMessage migration for warnings and hot paths
- Story 11.19: Enforcement and policy alignment

## Requirements & Constraints

Epic 11 implementation stories must remediate release-readiness risks from the architecture review, not introduce new user-facing product scope. Runtime work must preserve EventStore command/query/projection behavior while improving token expiry, sign-out eviction, circuit-safe token acquisition, projection reconnect behavior, bounded disposal, cache seeding, and hub wire-contract pins.

Security-sensitive work must fail closed and be directly tested. Return-path validation, storage-key canonicalization, SignalR/HTTP wire DTO serialization, MCP hidden/denied paths, lifecycle authorization, API-key handling, logs, evidence, and Testing outputs must avoid raw tokens, secrets, unrestricted identifiers, raw paths, stack traces, and raw payloads.

Visual and accessibility remediation must be evidence-based. Dead scoped CSS, unlinked stylesheets, legacy Fluent/FAST tokens, and accessibility-sensitive regressions require durable Governance guards plus rendered-DOM, computed-style, bUnit, e2e, or equivalent evidence. UI work must preserve WCAG-relevant accessible names, roles, focus behavior, keyboard access, live regions, reduced-motion behavior, forced-colors behavior, stable test selectors, and support-safe messaging.

Testing-package changes must make failure paths realistic for adopters: command rejection, timeout, stall-at-syncing, authorization-policy states, paging/filter/sort callbacks, redacted evidence, and directly tested builders/assertions/fakes. Public API baselines must be updated intentionally when shipped Testing surface changes.

All implementation remains under the repository's strict build posture: .NET 10, nullable, centralized package versions, built-in analyzers only, `TreatWarningsAsErrors=true`, source-generated logging for warning/security/hot paths where required, and no global warning or analyzer disable to mask remediation work.

## Technical Decisions

The canonical generated command route family is `/commands/{BoundedContext}/{CommandTypeName}`. Command palette entries, projection empty-state CTAs, and generated command pages must converge on that route family, and route activation must be pinned by e2e coverage. Module-page/tab routes are governed separately by the FC-IA-1 decision; Story 11.7 stays blocked until that IA gate is signed off.

The approved package-boundary target is a netstandard2.0-clean `Contracts` kernel plus a net10-only `Contracts.UI` assembly for Blazor/Fluent rendering contracts. The kernel contains attributes, communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts, and diagnostic IDs. Blazor/Fluent rendering contracts such as typography tokens, render-fragment contexts, keyboard-event members, and projection slot/template/view rendering contracts move out of the kernel. SourceTools must continue referencing only the kernel.

No new Blazor, Fluent, runtime service, or testing implementation types should be added to `Contracts`. Runtime-owned types move to runtime packages, Testing-owned fakes move to Testing, shell options and Fluxor action records move to Shell where approved, and `QueryRequest` must be decomposed through the existing HFC0001 migration/deprecation path rather than a silent breaking change.

Schema fingerprints, generated output paths, CLI JSON schemas, MCP schemas, HFC diagnostics, Testing public API, and package inventory are public contracts. Any public-surface or package-boundary change requires intentional baseline updates, compatibility notes, migration/deprecation guidance, release inventory updates, and package-consumer validation before v1.0.

Shell layering must be made explicit and enforceable: telemetry is cross-cutting, connection and polling workers belong in infrastructure, render components should not own route/label helper logic, duplicated scope-resolution and snapshot-publisher behavior should be consolidated, and helper consolidation must preserve behavior through focused tests.

## UX & Interaction Patterns

Epic 11 UI work is mostly corrective. Realtime and lifecycle UI must expose reconnecting, fallback, degraded, and pending states without converting HTTP acceptance or projection nudges into confirmed success. Command activation must land on real generated command pages from palette and CTA flows.

Visual fixes must follow existing FrontComposer/Fluent UI v5 patterns, Fluent 2 tokens, and the established shell UX contracts. Status, empty/loading, connection, settings, density-preview, and command-palette surfaces must preserve accessible names, keyboard reachability, focus-visible behavior, reduced-motion and forced-colors fallbacks, and support-safe copy. Stories with visual or layout decisions not already covered by planning artifacts need story-local design notes.

## Cross-Story Dependencies

Story creation follows the Epic 11 implementation-order table, not heading order or numeric sort. Story 11.0 is complete and unblocks general implementation; Story 11.8 is complete but its package-boundary implementation stories, 11.11 through 11.14, remain deliberately last.

The preferred order is: 11.1, 11.2, 11.4, 11.3, 11.5, 11.6, 11.7, then the lower-risk consolidation group 11.9/11.15/11.16, then split child stories for 11.17, 11.18, and 11.19, and finally 11.11 through 11.14.

Stories 11.17, 11.18, and 11.19 are decomposition parents and must be split into independently reviewable child stories with named validation lanes before development. Story 11.7 depends on the completed route decision and the FC-IA-1 module-tab/projection-flyout IA sign-off. Stories 11.11 through 11.14 depend on the approved Contracts split plan and must carry package compatibility, public API, documentation, migration, and release evidence together.
