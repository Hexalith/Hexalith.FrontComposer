# Epic 11 Context: Architecture Review Remediation

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes v1.0 architecture-review remediation without reopening completed product epics. It hardens runtime reliability, security validation, realtime and MCP behavior, visual governance, Testing harness failure modes, route activation, package boundaries, shell layering, logging, and enforcement policy so FrontComposer is release-ready and safe for adopter modules such as Hexalith.Tenants.

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

Epic 11 implementation is release-risk remediation, not new product scope. Stories must address runtime blind spots and architecture boundaries with focused Given/When/Then acceptance criteria, preserve existing command/query/projection behavior, and avoid reopening Epics 1-8 or depending on Epics 9-10.

Security, privacy, and support-safety are mandatory across the epic. Auth, return-path validation, storage keys, MCP hidden/denied paths, lifecycle authorization, API-key handling, logs, telemetry, evidence, snapshots, and Testing outputs must fail closed where appropriate and must not expose raw tokens, secrets, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, unrestricted PII, or raw local paths.

Visual and accessibility remediation must be guard-backed. Dead scoped CSS, unlinked stylesheets, legacy Fluent/FAST tokens, and accessibility-sensitive regressions require durable Governance checks plus rendered-DOM, computed-style, bUnit, e2e, or equivalent evidence. UI work must preserve accessible names, roles, keyboard access, focus behavior, live-region behavior, reduced-motion and forced-colors behavior, stable test selectors, and support-safe copy.

Testing package work must make adopter failure-path tests realistic: rejection, timeout, stall-at-syncing, authorization-policy states, per-request paging/filter/sort callbacks, redacted evidence, and direct tests for changed builders, assertions, and fakes. Public API baselines must be updated intentionally whenever shipped Testing surface changes.

All work remains under the repository's strict build posture: .NET 10, `.slnx` only, nullable enabled, centralized package versions, built-in analyzers, `TreatWarningsAsErrors=true`, source-generated logging for warning/security/hot paths where required, and no global warning or analyzer disable to mask remediation.

## Technical Decisions

The canonical generated command route family is `/commands/{BoundedContext}/{CommandTypeName}`. Command palette entries, projection empty-state CTAs, and generated command pages must converge on that route family and be pinned by e2e route-activation coverage.

The FC-IA-1 route/navigation decision is signed off. Module workspaces use one primary shell entry per module, required default tabs, path-segment tab routes shaped as `/{module}/{tab}`, and projection flyouts remain strictly secondary routes into the module workspace rather than a second primary information architecture.

The approved package-boundary target is a netstandard2.0-clean `Contracts` kernel plus a net10-only `Contracts.UI` assembly. The kernel owns attributes, communication contracts, registration abstractions, MCP descriptors, schema fingerprint contracts, and diagnostic IDs. Blazor/Fluent rendering contracts such as typography tokens, render-fragment contexts, keyboard-event members, and projection slot/template/view contracts move out of the kernel. `SourceTools` must keep referencing only the kernel.

No new Blazor, Fluent, runtime-service, or testing-implementation types should be added to `Contracts`. Runtime-owned types move to runtime packages, Testing-owned fakes move to Testing, shell options and Fluxor action records move to Shell where approved, and `QueryRequest` changes must use the existing HFC0001 migration/deprecation path rather than a silent breaking change.

Schema fingerprints, generated output paths, CLI JSON schemas, MCP schemas, HFC diagnostics, Testing public API, and package inventory are public contracts. Public-surface or package-boundary changes require intentional baseline updates, compatibility notes, migration/deprecation guidance, release inventory updates, and package-consumer validation before v1.0.

Shell layering must be explicit and enforceable: telemetry is cross-cutting, connection and polling workers belong in infrastructure, render components should not own route/label helpers, duplicated scope-resolution and snapshot-publisher behavior should be consolidated, and helper consolidation must preserve behavior through focused tests.

## UX & Interaction Patterns

Epic 11 UI work is corrective. Realtime and lifecycle surfaces must expose reconnecting, fallback polling, degraded, pending, rejected, and confirmed states without treating HTTP acceptance or projection nudges as confirmed success. Palette and CTA command activation must land on real generated command pages.

UI changes must use FrontComposer and Fluent UI Blazor v5 patterns, Fluent 2 tokens, and existing shell interaction components. Status indicators use icon plus tooltip plus `aria-label`; command and projection state changes use live regions only when useful and non-noisy; hover-only affordances are insufficient; modal stacks should stay shallow.

Stories with visual or layout decisions not already covered by planning artifacts need story-local design notes.

## Cross-Story Dependencies

Story creation follows the Epic 11 implementation-order table, not heading order or numeric sort. Story 11.0 is complete and unlocks general implementation. Story 11.8 is complete, but its package-boundary implementation stories, 11.11 through 11.14, remain deliberately last.

The implementation order is: 11.1, 11.2, 11.4, 11.3, 11.5, 11.6, 11.7, then 11.9/11.15/11.16, then split child stories for 11.17, 11.18, and 11.19, and finally 11.11 through 11.14.

Stories 11.17, 11.18, and 11.19 are decomposition parents and must be split into independently reviewable child stories with named validation lanes before development. Stories 11.11 through 11.14 depend on the approved Contracts split plan and must carry package compatibility, public API, documentation, migration, and release evidence together.
