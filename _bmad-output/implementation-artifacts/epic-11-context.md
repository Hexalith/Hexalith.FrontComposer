# Epic 11 Context: Release Readiness Remediation Program

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 11 closes the highest release-readiness blind spots left by the architecture-quality review so adopters and operators receive reliable runtime behavior, secure and diagnosable failure handling, stable package and route contracts, useful testing support, and enforceable maintainability standards before v1.0, without adding new user-facing scope or reopening completed Epics 1–10.

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

## Requirements & Constraints

Work is organized into runtime reliability/security, adopter testing/route integrity, contracts/package boundary, and maintainability/enforcement. Changes must preserve existing command, query, projection, public API, schema, generated-output, diagnostic, CLI/MCP wire, and package compatibility contracts unless an intentional migration and baseline update is approved. Release validation uses .NET 10, the `.slnx` solution, centralized dependencies, nullable analysis, and `TreatWarningsAsErrors=true`; changed surfaces must pass their focused tests plus applicable Governance, Contract, snapshot, compatibility, PublicAPI, generated-output, and Release gates.

Security and support evidence must fail closed and exclude raw tokens, secrets, payloads, stack traces, unrestricted PII, and unbounded local paths. Runtime blind spots require durable regression coverage, including cross-request lifetimes, unlinked stylesheets, dead scoped CSS, and parameter-splat surfaces. Analyzer remediation must use built-in analyzers only, keep warnings-as-errors unchanged, avoid broad category or repository-wide suppressions, and retain only narrow, owned, reviewable exceptions. Mechanical cleanup must preserve behavior and public API shape.

## Technical Decisions

Generated commands use `/commands/{BoundedContext}/{CommandTypeName}`; module tabs use `/{module}/{tab}`, with projection flyouts remaining secondary navigation. The `Contracts` kernel stays netstandard2.0-clean and UI-neutral, while net10-only `Contracts.UI` owns Blazor/Fluent rendering contracts; SourceTools continues to depend only on the kernel. Shell routing owns pure route and label derivation, Infrastructure owns connection and polling workers, and telemetry remains cross-cutting.

MCP lifecycle state spans requests through a singleton store behind a scoped facade without captive scoped dependencies. EventStore authentication supports interactive circuits, token expiry, and sign-out eviction; projection realtime must recover after the default reconnect ladder and dispose concurrent work safely. Logging ownership is exclusive: security/fail-closed sites first, command-lifecycle/projection/polling hot paths second, and residual warning-or-higher sites last. Recommended analyzer adoption is staged through policy/exception classification, product/generator cleanup, test/sample cleanup, and repository-wide activation.

## UX & Interaction Patterns

Use Fluent UI Blazor v5 and Fluent 2 tokens, with WCAG 2.2 AA keyboard, focus, naming, live-region, reduced-motion, and forced-colors behavior. Realtime and command experiences must expose reconnecting, fallback, degraded, pending, rejected, and confirmed states without treating HTTP acceptance or a projection nudge as confirmed success. Visual fixes require rendered evidence and Governance guards that prevent dead styles, unlinked CSS, and legacy tokens from returning.

## Cross-Story Dependencies

Story 11.0 and the signed-off information-architecture gate precede Story 11.7. Story 11.8 precedes Stories 11.11–11.14. Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents; only their named children carry queue status. Logging children follow the security → hot-path → residual ordering. The analyzer program is strictly sequential—11.20 → 11.21 → 11.22 → 11.23—with separate Architecture/Product approval at each phase; 11.23 gates v1.0 publication. Story 11.24 remains blocked until EventStore records explicit runtime-migration authority and an approved source/package identity.
