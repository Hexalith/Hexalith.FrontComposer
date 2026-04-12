---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-02-elicitation-and-party, step-03-create-stories, step-04-final-validation]
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
---

# Hexalith.FrontComposer - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.FrontComposer, decomposing the requirements from the PRD, UX Design, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Developer can mark a command record with [Command] and have the framework generate a corresponding form component at compile time.
FR2: Developer can mark a projection type with [Projection] and have the framework generate a DataGrid view at compile time.
FR3: Developer can declare bounded contexts with [BoundedContext(name)] and have the framework group them as navigation sections with optional domain-language display labels.
FR4: Developer can tag a projection with [ProjectionRole(role)] to signal its rendering role hint (ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard -- capped at 5).
FR5: Developer can tag a status enum value with [ProjectionBadge(slot)] and have the framework render a semantic status badge from a configurable palette.
FR6: Framework can infer field rendering from .NET types (primitives, enums, DateTimeOffset, collections) and select the appropriate input component without developer intervention.
FR7: Framework can detect drift between backend domain declarations and generated UI at build time, surfacing mismatches as compile-time diagnostics rather than runtime silent behavior.
FR8: Framework can select command rendering density -- inline button (0-1 non-derivable fields), compact inline form (2-4 fields), or full-page form (5+ fields) -- based on field count.
FR9: Framework can produce an explicit placeholder for unsupported field types with a build-time warning and documentation link, so that unsupported types never render silently.
FR10: Framework can surface developer-provided field descriptions as contextual help (tooltips, inline labels) in generated views, so business users understand column and field meanings without developer intervention.
FR11: Framework can render meaningful empty states with domain-language guidance and contextual calls-to-action for projections with no data.
FR12: Business user can filter, sort, and text-search within auto-generated projection DataGrid views, with filter/sort state persisted across sessions.
FR13: Developer can install the framework's composition shell with a single NuGet meta-package reference and register a domain with a minimal registration ceremony.
FR14: Developer can configure the shell's theme with a customizable accent color overridable at deployment.
FR15: Business user can toggle between Light, Dark, and System themes and have the preference persist across sessions.
FR16: Business user can select display density (Compact, Comfortable, Roomy) and have the preference apply across all generated views.
FR17: Business user can navigate between bounded contexts via a collapsible sidebar with nav groups up to two levels of hierarchy depth.
FR18: Business user can invoke a command palette via keyboard shortcut (Ctrl+K) to fuzzy-search commands, projections, and recently visited views across all bounded contexts.
FR19: Business user can resume a prior session with last-visited navigation section, applied filters, sort order, and expanded rows restored.
FR20: Business user can expand an entity row in place for detail view without navigating away from the projection context.
FR21: Framework can surface newly available bounded contexts or commands with a "New" badge on first appearance after a framework or domain update.
FR22: Framework can preserve in-progress command form state across connection interruptions, restoring unsaved field values after reconnection.
FR23: Framework can render a five-state command lifecycle (idle -> submitting -> acknowledged -> syncing -> confirmed) with progressive visibility thresholds (thresholds defined in NFRs).
FR24: Framework can detect SignalR connection loss and display a warning-colored connection-lost inline note without disrupting the user's in-flight workflow.
FR25: Framework can reconnect to SignalR after network restoration, rejoin subscribed projection groups, and fire an ETag-gated catch-up query to reconcile stale projection state.
FR26: Framework can batch stale projection updates from reconnection into a single animation sweep, rather than per-row flashes.
FR27: Framework can display a short auto-dismissing reconnect notification after successful reconciliation.
FR28: Framework can surface domain-specific command rejection messages that name the conflicting entity and propose a concrete resolution, rather than generic error messages.
FR29: Framework can handle idempotent command outcomes: a command landing during a disconnect produces the correct terminal state on reconciliation without user-visible duplication.
FR30: For each command submission, the framework shall emit exactly one user-visible outcome (success, rejection, or error notification) -- never silently fail, never produce duplicate user-visible effects.
FR31: Framework can fall back to ETag-gated polling when SignalR is unavailable, preserving correctness of the projection view under degraded network conditions.
FR32: Framework can abstract all event-store communication behind swappable service contracts supporting command dispatch, query execution, and real-time subscription, without direct coupling to infrastructure providers.
FR33: Framework can cache query results per tenant and user in client-side storage with ETag validation and bounded eviction, as an opportunistic cache where correctness comes from server reconciliation.
FR34: Framework can handle the full HTTP response matrix (200, 202, 304, 400, 401, 403, 404, 409, 429) with progressive user-facing UX tailored to each response class.
FR35: Framework can propagate tenant context from JWT bearer tokens through all command and query operations, enforcing tenant isolation at the framework layer.
FR36: Framework can generate unique message identifiers for command idempotency, ensuring replay-safe handling with deterministic duplicate detection.
FR37: Framework can integrate with Keycloak, Microsoft Entra ID, GitHub, and Google identity providers via standard OIDC/SAML flows without shipping a custom authentication UI.
FR38: Developer can register a domain with the Aspire hosting builder via a typed extension method (e.g., .WithDomain<T>()), completing the registration ceremony alongside Program.cs registration.
FR39: Developer can override field rendering via declarative attributes without writing custom components (annotation level).
FR40: Developer can override component rendering via typed Razor templates bound to domain model contracts (template level).
FR41: Developer can override a specific projection field via a typed slot declaration using refactor-safe lambda expressions (slot level).
FR42: Developer can replace a generated component entirely with a custom implementation while preserving the framework's lifecycle wrapper, accessibility contract, and custom-component accessibility checks (full-replacement level).
FR43: Framework can validate customization overrides against the framework version at build time, warning when an override's expected contract doesn't match the installed framework version.
FR44: Framework can provide hot reload support for all four customization gradient levels without application restart.
FR45: Framework can ensure customization and generation errors include sufficient context for developer self-service resolution: what was expected, what was found, how to fix it, and a diagnostic ID linking to a documentation page.
FR46: Developer can apply authorization policies to commands via declarative attributes that integrate with ASP.NET Core authorization middleware.
FR47: Framework can isolate rendering failures in customization gradient overrides to the affected component via error boundaries, preventing one faulty override from crashing the composition shell, with a diagnostic ID in the fallback UI.
FR48: Framework can enforce at build time that no generated or framework code references infrastructure provider types directly, routing all communication through framework abstractions.
FR49: Framework can expose a domain model as typed agent tools via an in-process Model Context Protocol server hosted alongside the composition shell.
FR50: Framework can emit typed MCP tool parameters with validation constraints derived from domain validation rules, preventing schema divergence between web and agent surfaces.
FR51: Framework can reject MCP tool calls referencing unknown tool names at the contract boundary, returning a suggestion response with the correct tool name and the full tenant-scoped tool list -- stopping hallucinations at the fence.
FR52: Framework can expose commands as MCP tools using a two-call lifecycle pattern where the command invocation returns an acknowledgment with a subscription URI, and state transitions are exposed via a separate lifecycle tool with guaranteed terminal states.
FR53: Framework can render projections as Markdown tables, status cards, and timelines consumable by LLM agents through chat surfaces.
FR54: Framework can enumerate MCP tools scoped to the agent's active tenant, so agents see only the tools available for their authorization scope.
FR55: Framework can publish a versioned skill corpus containing attribute references, domain-modeling conventions, and code generation patterns as both a NuGet package and MCP-discoverable resources at runtime, consumable by LLM agents and human developers.
FR56: Framework can produce typed NuGet contracts shared between backend, web surface, and MCP surface from a single source, with auto-generated MCP tool descriptions derived from the same source as web form labels -- preventing schema drift across modalities.
FR57: LLM agent (runtime) can issue commands and read projections against a FrontComposer-registered domain with the same lifecycle semantics and rollback messages as the web surface.
FR58: LLM agent (build-time) can produce compilable, structurally-valid microservice code from a fixed prompt corpus, with framework-emitted typed partial types guiding the agent into a compiler-checked shape. Tested against a pinned model version with a structural validator.
FR59: Framework can emit schema hash fingerprints per projection and MCP tool manifest, enabling graceful client/server version negotiation when framework versions diverge across deployments.
FR60: Framework can produce a migration delta or breaking-change diagnostic when a schema hash fingerprint differs from the previously deployed version, providing a remediation path rather than detection alone.
FR61: Framework can define a rendering abstraction contract that decouples composition logic from surface-specific renderers, enabling multi-surface rendering from a single domain source even when v1 ships only one surface through it.
FR62: Developer can scaffold a new FrontComposer project via a project template that provides a ready-to-run local development topology with all framework integrations pre-wired.
FR63: Developer can use a CLI tool to inspect source-generator output for a specific domain type at a deterministic file path.
FR64: Developer can run a CLI migration tool to apply Roslyn analyzer code fixes for cross-version framework upgrades.
FR65: Developer can use Visual Studio, JetBrains Rider, or VS Code with C# Dev Kit and receive equivalent IntelliSense, hover documentation, go-to-definition, and source generator debugging experience.
FR66: Framework can reserve a dedicated diagnostic ID range per package, so any framework diagnostic resolves to a consistent, lookup-addressable documentation page.
FR67: Framework can deprecate an API with a minimum one-minor-version window and publish deprecation messages that link to a migration path via diagnostic ID.
FR68: Framework can publish documentation in four Diataxis genres -- tutorials, how-to, reference, and explanation/concepts -- as a generated documentation site.
FR69: Framework can require a migration guide for any change that would break a shipped skill corpus example, regardless of semantic version bucket.
FR70: Developer can incrementally rebuild source-generator output on domain attribute change via hot reload without full application restart.
FR71: Framework can provide a test host and utilities for generated components, enabling adopters to write component tests for their customization gradient overrides and auto-generated views.
FR72: Framework can emit structured logging events from the lifecycle wrapper and runtime services following OpenTelemetry semantic conventions, enabling end-to-end distributed tracing from user click to backend to projection update to SignalR to UI update.
FR73: Framework can run a nightly LLM code-generation benchmark as a quality gate with pinned model versions and a rolling-median threshold.
FR74: Framework can produce semantic version releases automatically from conventional commits.
FR75: Framework can generate a CycloneDX SBOM per release and publish signed NuGet packages with symbol packages for IDE debugging.
FR76: Framework can verify accessibility conformance via automated CI checks, blocking merge on serious or critical WCAG violations.
FR77: Framework can verify visual specimens per release across theme, density, and language-direction dimensions, failing merge on unexplained drift.
FR78: Framework can verify generated UI components consume EventStore API contracts correctly via consumer-driven contract tests at the REST-to-generated-UI seam.
FR79: Framework can verify source generator correctness via mutation testing, ensuring that code mutations in the generator produce detectable failures in generated output.
FR80: Framework can automatically detect, isolate, and quarantine flaky tests into a separate CI lane with a reintroduction gate.
FR81: Framework can verify command idempotency via property-based testing with randomly generated command sequences, ensuring replay-safety across reconnect scenarios.
FR82: Framework can expose a test harness for simulating SignalR connection faults (drop, delay, partial delivery, reorder) without requiring a live server.

### NonFunctional Requirements

NFR1 (Performance - Web Latency): Command click-to-confirmed state P95 cold actor < 800ms (v1), < 500ms (v1.x). Measured via Playwright task timer on localhost Aspire topology.
NFR2 (Performance - Web Latency): Command click-to-confirmed state P50 warm actor < 400ms (v1), < 300ms (v1.x). Measured same as NFR1.
NFR3 (Performance - Web Latency): First interactive render < 300ms (v1), < 200ms (v1.x). Measured via custom Performance.mark('hfc-shell-interactive') in OnAfterRender, validated via Playwright.
NFR4 (Performance - Web Latency): DataGrid render with 500 virtualized rows P95 < 300ms (v1), < 200ms (v1.x). Measured via bUnit render benchmark.
NFR5 (Performance - Web Latency): Command palette search response < 100ms (v1), < 50ms (v1.x). Measured via synthetic keystroke-to-results timer.
NFR6 (Performance - Agent Latency): Agent command-to-projection read-your-writes P95 < 1500ms (v1), < 800ms (v1.x). Measured via MCP tool-call round-trip benchmark on localhost.
NFR7 (Performance - Agent Latency): MCP hallucination-rejection response time P95 < 100ms (v1), < 50ms (v1.x). Measured via unit test timer on rejection path.
NFR8 (Performance - Generator): Incremental rebuild per domain assembly < 500ms. CI-gated via IIncrementalGenerator diagnostics.
NFR9 (Performance - Generator): Full solution rebuild for 50-aggregate reference domain < 4s. CI benchmark; gate on incremental only, full-rebuild is advisory.
NFR10 (Performance - Generator): Hot reload latency for domain attribute change < 2s. Manual verification against dev-loop SLO.
NFR11 (Performance - Lifecycle Thresholds): Happy path lifecycle invisible to user when < 300ms.
NFR12 (Performance - Lifecycle Thresholds): Subtle sync pulse animation displayed for delays between 300ms and 2s.
NFR13 (Performance - Lifecycle Thresholds): Explicit "Still syncing..." inline text displayed for delays between 2s and 10s.
NFR14 (Performance - Lifecycle Thresholds): Action prompt with manual refresh option displayed for delays > 10s.
NFR15 (Performance - Lifecycle Thresholds): Warning-colored inline note displayed immediately on HubConnectionState.Disconnected, with ETag polling fallback.
NFR16 (Performance - Lifecycle Thresholds): Batched animation sweep + 3s auto-dismissing toast on HubConnectionState.Reconnected.
NFR17 (Security - Data Posture): Framework persists ONLY UI preference state (theme, density, nav, filters, sort) in client-side storage. Zero PII, zero business data at the framework layer.
NFR18 (Security - Data Posture): All business data lives in adopter microservices and Hexalith.EventStore. Framework never reads, writes, or caches business data beyond ETag-validated query results.
NFR19 (Security - Data Posture): ETag cache entries contain projection snapshots scoped to {tenantId}:{userId} with bounded eviction. Cache is opportunistic; correctness comes from server queries.
NFR20 (Security - Auth): OIDC/SAML integration with Keycloak, Microsoft Entra ID, GitHub, Google. No custom auth UI.
NFR21 (Security - Auth): JWT bearer tokens propagated through all command and query operations.
NFR22 (Security - Auth): Tenant isolation enforced at framework layer via TenantId from JWT claims.
NFR23 (Security - Auth): [RequiresPolicy] attributes integrate with ASP.NET Core authorization middleware. Missing policies produce build-time warnings.
NFR24 (Security - Supply Chain): Stable NuGet packages signed with OSS-signing certificate.
NFR25 (Security - Supply Chain): CycloneDX SBOM generated per release.
NFR26 (Security - Supply Chain): Symbols (.snupkg) published for IDE debugging.
NFR27 (Security - MCP): Typed-contract hallucination rejection: unknown tool names rejected with suggestion + tenant-scoped tool list. Command never reaches backend.
NFR28 (Security - MCP): Cross-tenant tool visibility is a security bug (must never occur).
NFR29 (Accessibility): WCAG 2.1 AA conformance on all auto-generated output.
NFR30 (Accessibility): All generated forms must have associated <label> elements, enforced via axe-core CI gate.
NFR31 (Accessibility): All interactive elements must be keyboard-navigable, verified via manual screen-reader verification per release.
NFR32 (Accessibility): Color contrast >= 4.5:1 for text, >= 3:1 for UI components, enforced via axe-core CI gate.
NFR33 (Accessibility): Focus management on navigation transitions, verified via Playwright focus-tracking assertions.
NFR34 (Accessibility): ARIA landmarks, roles, and live regions enforced via axe-core CI gate + manual audit.
NFR35 (Accessibility): Screen reader compatibility with NVDA, JAWS, VoiceOver, verified via manual checklist logged in release notes.
NFR36 (Accessibility): Custom overrides checked for a11y contract compliance via build-time Roslyn analyzer with WCAG citation + user scenario.
NFR37 (Accessibility - CI): axe-core via Playwright fails build on "serious" or "critical" violations.
NFR38 (Accessibility - CI): Visual specimen baseline verified across Light/Dark x Compact/Comfortable/Roomy. Unexplained drift fails merge. RTL deferred to v2.
NFR39 (Reliability - SignalR): Auto-reconnect with exponential backoff.
NFR40 (Reliability - SignalR): Automatic group rejoin + ETag-gated catch-up query on reconnection.
NFR41 (Reliability - SignalR): Batched reconciliation: N stale rows as one sweep, not N individual flashes.
NFR42 (Reliability - SignalR): Auto-dismissing "Reconnected -- data refreshed" toast (3s).
NFR43 (Reliability - SignalR): In-progress form state preserved across connection interruptions.
NFR44 (Reliability - Commands): Every submission produces exactly one user-visible outcome: success, rejection, or error notification.
NFR45 (Reliability - Commands): Idempotent handling via ULID message IDs with deterministic duplicate detection.
NFR46 (Reliability - Commands): Domain-specific rejection messages name conflicting entity and propose resolution.
NFR47 (Reliability - Commands): Zero silent failures across all surfaces.
NFR48 (Reliability - Schema): All persisted event schemas and MCP tool schemas must be bidirectionally compatible within a major version.
NFR49 (Reliability - Schema): Schema evolution tests required: bidirectional deserialization matrix covering v1.0 event x v1.N code and v1.N event x v1.0 code.
NFR50 (Reliability - Schema): Migration delta or breaking-change diagnostic emitted when schema hash diverges from prior deployed version.
NFR51 (Testability): Unit test coverage >= 80% line coverage on core framework code (generator core, command pipeline, SignalR reconnection logic). Tooling: xUnit + Shouldly.
NFR52 (Testability): Component test coverage >= 15% line coverage on auto-generated Razor components. Tooling: bUnit.
NFR53 (Testability): Integration tests: minimum 3 tests per API boundary. Tooling: xUnit + SignalR fault injection.
NFR54 (Testability): E2E tests: one suite per reference microservice covering happy path + disconnect/reconnect + rejection rollback. Tooling: Playwright.
NFR55 (Testability - Pact): Consumer-driven Pact contract tests between REST surface and generated UI, with provider verification per release.
NFR56 (Testability - Mutation): Stryker.NET mutation testing on source generator with >= 80% kill score on happy-path pipeline, >= 60% on error-handling paths.
NFR57 (Testability - Flaky): Flaky-test quarantine lane with automatic detection, isolation, separate CI lane, and reintroduction gate.
NFR58 (Testability - Property): FsCheck property-based testing for command idempotency: replay(commands) == original_outcomes for 1000 generated sequences.
NFR59 (Testability - Fault Injection): SignalR fault-injection test wrapper simulating drop, delay, partial delivery, reorder without live server.
NFR60 (Testability - LLM Benchmark): Nightly LLM benchmark on main, pinned model versions, temperature 0 with fixed seed.
NFR61 (Testability - LLM Benchmark): 28-day ratchet rule: gate = max(initial gate, trailing 28-day median minus 3pp).
NFR62 (Testability - LLM Benchmark): Model transition rule: ratchet pauses during model transitions.
NFR63 (Testability - LLM Benchmark): Prompt corpus of 20 prompts at v1, cached prompt-response pairs, 4/20 legitimate misses allowed.
NFR64 (CI - Pipeline Time): Inner loop (unit + component) < 5 minutes.
NFR65 (CI - Pipeline Time): Full CI (excluding nightly) < 12 minutes.
NFR66 (CI - Pipeline Time): Nightly CI < 45 minutes.
NFR67 (CI - Enforcement): If full CI exceeds 15 minutes for 3 consecutive days, mandatory "CI diet" task auto-created.
NFR68 (CI - Build): Trim warnings block CI (IsTrimmable="true" on all framework assemblies).
NFR69 (CI - Build): PublicApiAnalyzers fail CI on accidental breaking changes within a minor version.
NFR70 (CI - Build): Conventional commit-msg hook shipped with project template; CI lint validates.
NFR71 (CI - Build): CS1591 missing XML doc becomes error after v1.0-rc1 (API freeze milestone).
NFR72 (Trim - Week 2 Evaluation): FluentValidation, DAPR SDK, and Fluent UI Blazor v5 must each pass trim compatibility evaluation.
NFR73 (Deployment): Framework runs identically on-premise, sovereign cloud, Azure Container Apps, AWS ECS/EKS, GCP Cloud Run.
NFR74 (Deployment): Zero direct infrastructure coupling -- no direct references to Redis, Kafka, Postgres, CosmosDB, or DAPR SDK types from framework assemblies.
NFR75 (Maintainability - Versioning): Lockstep versioning across all packages for v1. Cross-package mismatch = build error.
NFR76 (Maintainability - Versioning): Binary compatibility within minor versions enforced by PublicApiAnalyzers.
NFR77 (Maintainability - Deprecation): One minor version minimum deprecation window.
NFR78 (Maintainability - Deprecation): Migration guide required for any change breaking a shipped skill corpus example.
NFR79 (Maintainability - Logging): OpenTelemetry semantic conventions for structured logging. End-to-end tracing.
NFR80 (Maintainability - Diagnostics): Diagnostic ID scheme with reserved ranges per package: HFC0001-0999 through HFC5000-5999.
NFR81 (Maintainability - Sustainability): Every quality gate must survive the solo-maintainer sustainability filter.
NFR82 (Usability - Onboarding): Time-to-first-render <= 5 minutes from dotnet new to running app.
NFR83 (Usability - Code Ceremony): Non-domain code per microservice <= 10 lines.
NFR84 (Usability - Customization): Customization time <= 5 minutes to override one field's rendering.
NFR85 (Usability - LLM Generation): LLM one-shot generation rate >= 80%.
NFR86 (Usability - Customization Cliff): Zero customization-cliff events in first 6 months.
NFR87 (Usability - Business User): First-task completion < 30 seconds with zero training.
NFR88 (Usability - Business User): Command lifecycle confidence 100% -- zero double-submits, zero "did it work?" hesitations.
NFR89 (Usability - Business User): Context-switch budget for top-10 actions <= 2 clicks to begin.
NFR90 (Usability - Business User): Session resumption: user lands on last navigation section with last state restored.
NFR91 (Usability - Agent): Tool-call correctness >= 95%.
NFR92 (Usability - IDE): Visual Studio 2026 reference, JetBrains Rider parity, VS Code with C# Dev Kit supported.
NFR93 (Portability): .NET 10 only for v1. No back-porting.
NFR94 (Portability): Blazor Server (dev) + Blazor Auto (production) primary. WASM standalone supported. Hybrid out of scope.
NFR95 (Documentation): DocFX-generated site with four Diataxis genres shipped at v1.
NFR96 (Documentation): Single source, two renderings (MCP + DocFX) with narrative vs reference markers.
NFR97 (Documentation): Teaching errors enforced at compile time via error message template.
NFR98 (Documentation): Day-1 highest-leverage doc is the customization gradient cookbook.
NFR99 (Release): Semantic-release from conventional commits with NuGet prerelease suffix.
NFR100 (Release): Package count collapse trigger if CI exceeds 90 minutes or wall-clock exceeds 2 hours.
NFR101 (Horizontal Framework): No vertical-specific features in core framework.
NFR102 (Horizontal Framework): No PII at framework layer, no vertical-specific audit/consent/DLP.
NFR103 (Tone & Language): Technical, precise, concise, confident, domain-language-consistent messages.

### Additional Requirements

**STARTER TEMPLATE:** No existing template fits. This is a greenfield .NET 10 monorepo framework. Epic 1 Story 1 must scaffold the solution structure manually per the phase-tagged directory structure (starting with W1-tagged elements only). First priority: MSBuild spine -> Contracts -> SourceTools -> Counter sample -> CI.

**Infrastructure & Build System:**
- Directory.Build.props with walk-up isolation guards (FrontComposerRoot property guard, ImportDirectoryBuildProps=false in submodules)
- Directory.Packages.props with central package management: Microsoft.CodeAnalysis.CSharp >=4.12.0, Fluxor.Blazor.Web 6.9.0, Fluent UI Blazor v5 (exact RC pin), xUnit 2.9.3 (NOT v3), bUnit 2.7.2, FluentAssertions, coverlet.collector
- xUnit pinned to v2 (not v3) due to bUnit 2.7.2 compatibility
- deps.local.props / deps.nuget.props two-file import pattern (ADR-002) with UseNuGetDeps boolean
- global.json pinning .NET SDK 10.0.5
- nuget.config, .editorconfig for code style
- Solution folders: src/, samples/, tests/
- W1: exactly 6 .csproj files (3 source + 2 sample + 1 test)

**Package Dependency Graph:**
- Contracts: dependency-free, multi-targets net10.0;netstandard2.0, change-controlled
- SourceTools: netstandard2.0 only, IsRoslynComponent=true, references Contracts + Microsoft.CodeAnalysis.CSharp
- Shell references Contracts (normal) + SourceTools (analyzer-only ref)
- Shell and EventStore are PEERS, both depend on Contracts
- SourceTools must NEVER reference Shell, Fluxor, or Fluent UI
- NuGet packaging prep for SourceTools with analyzer DLL placement

**Source Generator Architecture:**
- Three-stage pipeline: Parse (INamedTypeSymbol -> DomainModel IR), Transform (DomainModel -> output models), Emit (output models -> source code)
- IR extraction pattern mandatory for testability (70% unit test ratio, Stryker mutation testing)
- ForAttributeWithMetadataName for incremental pipeline (500ms budget)
- Generator emits Fluxor types as strings (fully-qualified names) -- no Fluxor dependency in SourceTools
- All generated files must end in .g.cs or .g.razor.cs, live in obj/ not src/
- Diagnostic catalog: HFC1000-HFC1999, 4-field message format

**Blazor Auto Render Mode:**
- Blazor Auto as first-class architectural constraint
- [PersistentState] attribute (.NET 10) mandatory for cross-render-mode state
- DI scope divergence documentation (Server = scoped-per-circuit, WASM = scoped-per-app)
- IStorageService adapter pattern for localStorage (WASM-only access)
- Runtime render-mode checks (OperatingSystem.IsBrowser(), IComponentRenderMode)

**State Management (Fluxor):**
- One Feature per domain type (ADR-008)
- Explicit IState<T> subscribe/dispose -- NEVER FluxorComponent base class
- Immutable record actions, past-tense naming, always include CorrelationId
- Per-concern features: ThemeState, DensityState, NavigationState, DataGridState, ETagCacheState, CommandLifecycleState
- CommandLifecycleState ephemeral (not persisted), all others persisted via IStorageService

**EventStore Communication:**
- Two channels: REST (POST commands -> 202, POST queries -> 200 + ETag) + SignalR hub (ProjectionChanged nudges)
- Client re-queries via REST with ETag after SignalR nudge
- ULID message IDs, max 10 If-None-Match per request, 1MB max body
- No colons in ProjectionType/TenantId/domain names (DAPR actor ID separator)
- camelCase JSON wire format
- SignalR groups: {projectionType}:{tenantId}, lightweight nudges only
- Command lifecycle timeout: 30 seconds (configurable)

**IStorageService Contract:**
- 5-method interface: GetAsync<T>, SetAsync<T>, RemoveAsync, GetKeysAsync, FlushAsync
- Two implementations: LocalStorageService (WASM), InMemoryStorageService (Server + bUnit)
- SetAsync fire-and-forget, FlushAsync via beforeunload JS interop
- LRU eviction, cache key pattern: {tenantId}:{userId}:{featureName}:{discriminator}

**MCP / Agent Integration:**
- Commands as typed MCP tools with FluentValidation
- Projections as MCP resources returning Markdown tables
- Lifecycle subscription via separate lifecycle/subscribe tool
- Hallucination rejection via schema validation against source-generator-emitted tool manifest
- MCP tool enumeration tenant-scoped

**Multi-Tenancy:**
- v0.1 single-tenant only; multi-tenant at v1
- JWT TenantId claim propagated through all operations
- Cross-tenant data visibility is a security bug

**DAPR Infrastructure:**
- DAPR IS the abstraction layer -- no custom wrapper
- All infrastructure through DAPR component bindings
- Pin to DAPR 1.17.4

**Fluent UI v5:**
- Still RC/preview -- ADR-003 accepts this risk
- Pin exact version, no abstraction layer over Fluent UI
- Budget 1-2 weeks migration if v5 GA breaks things
- Weekly canary build against latest RC drop

**CI/CD Pipeline:**
- W1: ci.yml with gates 1-3 (Contracts build, full solution build, SourceTools.Tests)
- W2: expand to gates 1-5 (+ API surface comparison, multi-TFM tests, banned-reference scan)
- W2: canary-fluentui.yml (weekly), nightly.yml (Stryker, FsCheck, LLM benchmark, Pact, flaky quarantine)
- Budget: <5min inner loop, <12min full CI, <45min nightly

**Testing Infrastructure:**
- IR pattern: 90% unit tests on Transform+Emit, integration for Parse
- Pact contracts file-based (not Pact Broker)
- FsCheck: bounded vocabulary, deterministic seed in CI, random in nightly
- Flaky quarantine via xUnit Trait, 5 consecutive passes to reintroduce
- Snapshot tests: golden files with semantic DOM comparison (AngleSharp-based)
- bUnit: FrontComposerTestBase pre-configures Fluxor + InMemoryStorageService
- Test naming: {Method}_{Scenario}_{Expected} OR Should_{Behavior}_When_{Condition}
- Field type coverage matrix: 29 tests

**Phase-Critical Sequencing:**
- W1 Day 1: MSBuild spine -> dotnet restore -> verify green
- W1 Day 1-2: Contracts (attributes + IRenderer) -> SourceTools (generator stub) -> dotnet build
- W1 Day 2-4: Counter.Domain + Counter.Web -> SourceTools.Tests -> ci.yml (gates 1-3)
- W1 deliverables: "Hello world" generator, working Aspire topology, build time baselines

### UX Design Requirements

UX-DR1: Implement FcCommandPalette -- universal search/navigation overlay (Ctrl+K or header icon). FluentSearch with auto-focus, categorized results (Projections, Commands, Recent), fuzzy matching, badge counts from IBadgeCountService, keyboard navigation (arrow keys, Enter, Escape, 150ms debounce), full ARIA dialog pattern with aria-activedescendant. Contextual mode: current bounded context commands first.
UX-DR2: Implement FcLifecycleWrapper -- five-state command lifecycle wrapper (Idle, Submitting, Acknowledged, Syncing, Confirmed/Rejected) with timeout escalation (2-10s text, >10s action prompt) and Disconnected state. FluentProgressRing during Submitting, configurable sync pulse threshold (default 300ms), domain-specific rollback on Rejection, aria-live polite/assertive, prefers-reduced-motion support.
UX-DR3: Implement FcFieldPlaceholder -- visible placeholder for unsupported field types. FluentCard with dashed border, warning icon, field name, type annotation, docs link. Dev-mode highlight with type details. Build-time warning emitted. Accessible role="status".
UX-DR4: Implement FcEmptyState -- domain-specific empty state with "[No {entities}] yet." message and contextual CTA "Send your first [Command Name]". No CTA for read-only projections. Accessible role="status".
UX-DR5: Implement FcSyncIndicator -- singleton shell header component for reconnection. "Reconnecting..." with pulse, ETag-conditioned catch-up, batch sweep animation for stale rows, "Reconnected -- data refreshed" auto-dismiss 3s toast if changes found. prefers-reduced-motion respected.
UX-DR6: Implement FcDesaturatedBadge -- extends FluentBadge with desaturation during Syncing (filter: saturate(0.5)), 200ms CSS transition on Confirmed, revert on Rejected, direct saturate on IdempotentConfirmed. aria-label includes state.
UX-DR7: Implement FcColumnPrioritizer -- DataGrid wrapper for >15 fields. First 8-10 columns by priority, "More columns ([N] hidden)" toggle, column visibility persisted in LocalStorage. Transparent for <=15 fields.
UX-DR8: Implement FcNewItemIndicator -- new entity highlight in DataGrid when outside current filters. Subtle highlight, "New -- may not match current filters" text, auto-dismiss 10s or on filter change.
UX-DR9: Implement FcDevModeOverlay (#if DEBUG) -- diagnostic layer toggled via Ctrl+Shift+D. Dotted outlines with convention names, click-open FluentDrawer detail panel with convention info, customization level, starter template copy. Unsupported fields highlighted.
UX-DR10: Implement FcHomeDirectory -- urgency-sorted bounded-context directory as v1 home. "Welcome back, [user name]. You have [N] items needing attention across [M] areas." Cards sorted by badge count descending. Zero-urgency in collapsed "Other areas".
UX-DR11: Implement FcStarterTemplateGenerator -- dev-time code generation from FcDevModeOverlay. Walks component tree via IRazorEmitter, emits Razor source for customization Levels 2-4 with typed Context. Clipboard copy via JS interop. Dev mode only.
UX-DR12: Implement ILifecycleStateService -- scoped service tracking per-command lifecycle states. API: Observe(correlationId), GetState, Transition, ConnectionState. Supports IdempotentConfirmed state. Property-based testing for state machine validity.
UX-DR13: Implement IBadgeCountService -- singleton providing ActionQueue badge counts. API: Counts dictionary, CountChanged observable, TotalActionableItems. Fetches initial counts, subscribes to SignalR for updates.
UX-DR14: Implement application shell layout -- FluentLayout with Header (48px: title, breadcrumbs, Ctrl+K trigger, theme toggle, settings), Navigation sidebar (~220px/~48px collapsed), Content area. Forms max 720px; DataGrids full-width.
UX-DR15: Implement sidebar navigation -- collapsible via FluentLayoutHamburger, preference persisted. Nav items are projection views only. Badge counts on ActionQueue items. Responsive: <1366px auto-collapse to icon-only.
UX-DR16: Implement action density rules -- 0-1 non-derivable fields: inline buttons; 2-4 fields: compact inline form (slide open below row); 5+ fields: full-page form (720px centered). Derivable = resolvable from context/system/default.
UX-DR17: Implement expand-in-row scroll stabilization -- pin expanded row top edge, push content below, scrollIntoView block:'nearest' + requestAnimationFrame. One row expanded at a time in v1.
UX-DR18: Implement expand-in-row progressive disclosure for >12 fields -- primary fields (first 6-8) immediate, secondary fields in FluentAccordion sections.
UX-DR19: Implement DataGrid state preservation across full-page form navigation -- scroll position, filters, sort, expanded row, selected row in per-view memory object.
UX-DR20: Implement session persistence in LocalStorage -- last nav section, filters, sort, expanded row per DataGrid. Graceful degradation if LocalStorage unavailable.
UX-DR21: Implement label resolution chain -- (1) [Display(Name)], (2) IStringLocalizer resource file, (3) humanized CamelCase, (4) raw field name. Build-time warning for unhumanized names.
UX-DR22: Implement form field type inference -- string->FluentTextField, bool->FluentCheckbox, DateTime->FluentDatePicker, enum->FluentSelect, int->FluentNumberField. FluentValidation integration, required fields marked, accessible labels.
UX-DR23: Implement color system with 6 semantic slots -- Accent (#0097A7), Neutral, Success, Warning, Danger, Info. Command lifecycle color mapping defined.
UX-DR24: Implement projection status badge system -- 6-slot palette: Neutral, Info, Success, Warning, Danger, Accent. [ProjectionBadge(BadgeSlot)] annotation. Unknown falls back to Neutral with warning.
UX-DR25: Implement Light/Dark/System theme support with instant switching via <fluent-design-theme>. Persisted in LocalStorage.
UX-DR26: Implement Typography API via Hexalith.FrontComposer.Typography static class with 9 constants mapping to Fluent UI type ramp. Living table with strict versioning.
UX-DR27: Implement three-level density (Compact/Comfortable/Roomy) with 4-tier precedence. Settings UI via Ctrl+, with live preview. --fc-density CSS custom property.
UX-DR28: Implement four responsive breakpoints -- Desktop (>=1366px), Compact desktop (1024-1365px), Tablet (768-1023px), Phone (<768px) with detailed behavior per tier.
UX-DR29: Implement touch targets -- 44x44px minimum for <1024px viewports enforced via density auto-switch. Specific targets per component type.
UX-DR30: Implement WCAG 2.1 AA with 14 specific commitments -- color never sole signal, focus visibility, keyboard parity, screen reader announcements, prefers-reduced-motion, CamelCase expansion, labels, contrast ratios, build-time analyzers, dev-mode overlay a11y, Roomy density, 400% zoom reflow, non-text contrast, forced-colors mode.
UX-DR31: Implement custom component accessibility contract (6 requirements) -- accessible name, keyboard reachability, focus visibility, state announcements, reduced-motion, forced-colors. Enforced via Roslyn analyzers.
UX-DR32: Implement automated accessibility testing in CI -- axe-core via Playwright (serious/critical block merge), contrast verification, keyboard tests, focus screenshots, density parity, forced-colors emulation, zoom/reflow. Manual screen reader verification per release.
UX-DR33: Implement type specimen verification view -- renders every type ramp slot, color token, theme, density. Screenshots compared against baselines per theme x density x direction. Drift fails CI.
UX-DR34: Implement data formatting specimen view -- DataGrid with all data types exercising formatting rules. Per-theme x per-density baseline comparison.
UX-DR35: Implement data formatting rules -- locale-formatted numbers, absolute/relative timestamps, truncated IDs (8-char monospace copy-on-click), null em dashes, collection counts, currency, boolean Yes/No, truncated enums (30-char), timezone via JS Intl.DateTimeFormat.
UX-DR36: Implement button hierarchy -- Primary (1 per context), Secondary, Outline, Danger (always confirm). Domain-language labels. Icons on Primary and row actions.
UX-DR37: Implement destructive action confirmation dialog -- FluentDialog with action name, description, "This action cannot be undone.", Cancel (auto-focused) and Danger button.
UX-DR38: Implement form abandonment protection for full-page forms -- FluentMessageBar warning after >30s on form when navigating away. Configurable threshold.
UX-DR39: Implement notification patterns using FluentMessageBar -- inline per-row + global content area. Success 5s auto-dismiss, Info 3s, Warning no dismiss, Error no dismiss. Max 3 visible, error aggregation >2 in 5s.
UX-DR40: Implement DataGrid filtering -- column filters (FluentSearch, 300ms debounce, server-side), status filters (FluentBadge toggle chips), global search (IProjectionSearchProvider hook), command palette for cross-context.
UX-DR41: Implement filter persistence -- per-DataGrid in LocalStorage keyed by bounded-context:projection-type. "Reset filters" button. Filter visibility summary. Empty filtered state distinct from empty total state.
UX-DR42: Implement virtual scrolling as default -- Fluent <Virtualize>, client-side <500 items, server-side ItemsProvider 500+. FluentSkeleton at scroll boundary. MaxUnfilteredItems safety rail (10,000). Row count display.
UX-DR43: Implement keyboard shortcuts via IShortcutService -- Ctrl+K (palette), Ctrl+Shift+D (dev overlay), Ctrl+, (settings), g h (home), / (first filter). Conflict prevention with build-time warnings.
UX-DR44: Implement contextual view subtitles -- ActionQueue: "[N] awaiting [action]"; StatusOverview: "[N] total across [M] statuses"; Default: "[N] [entities]".
UX-DR45: Implement projection role hints -- ActionQueue, StatusOverview, DetailRecord, Timeline, Default. Capped at 5-7. [ProjectionRoleHint] annotation with explicit states.
UX-DR46: Implement domain-specific error messages -- "[What failed]: [Why]. [What happened to the data]." No generic messages. Idempotent outcomes acknowledged as success. Form input preserved on rejection.
UX-DR47: Implement optimistic update pattern -- badge transitions immediately with desaturation, saturates on confirmation, reverts on rejection, skips revert on idempotent.
UX-DR48: Implement sync pulse frequency rule -- pulse timer starts at Acknowledged; if Confirmed within 300ms, pulse never fires. Calibration guidance for deployments >70% pulse rate.
UX-DR49: Implement sync pulse and focus ring coexistence -- both must remain distinguishable during syncing. Focus ring never dimmed.
UX-DR50: Implement SignalR-down lifecycle escalation -- immediate timeout message instead of indefinite pulse when disconnected during syncing window.
UX-DR51: Implement schema evolution resilience -- detect mismatches at startup, show "This section is being updated" message, invalidate cached ETags.
UX-DR52: Implement new capability arrival gating -- nav entries appear only when projection has data. Subtle "New" badge, removed after first visit.
UX-DR53: Implement user context restoration flow -- returning users see last state; first-visit users see home directory by badge count; graceful degradation if LocalStorage cleared.
UX-DR54: Implement four-level customization gradient with dev-mode overlay discovery -- Level 1 Annotation, Level 2 Template, Level 3 Slot, Level 4 Full Replacement. Each inherits above. Overlay recommends level and offers starter templates.
UX-DR55: Implement auto-generation boundary protocol -- never silently omit unsupported types. Always render FcFieldPlaceholder + build warning + dev-mode highlight.
UX-DR56: Implement Fc component naming convention -- "Fc" prefix for all custom framework components to distinguish from Fluent UI ("Fluent") and adopter components.
UX-DR57: Implement zero-override strategy -- no custom CSS on Fluent UI components, no custom tokens beyond accent color, no custom typography/icons/spacing/elevation. Custom components use only structural CSS.
UX-DR58: Implement confirmation pattern rules -- non-destructive: no confirmation; destructive: always FluentDialog; full-page form nav: conditional after 30s; bulk (v2): confirm with count.
UX-DR59: Implement Blazor Server + Blazor Auto platform strategy -- Server for dev inner loop, Auto for production. Document mobile latency and hot reload limitations.
UX-DR60: Implement localization -- IFluentLocalizer for English + French resource files. Label resolution chain on all generated strings.
UX-DR61: Implement Fluent UI v5 migration requirements -- FluentNavMenu->FluentNav, IToastService removed, SelectedOptions->SelectedItems, FluentDesignTheme->CSS custom properties, FluentDesignSystemProvider->FluentProviders.
UX-DR62: Implement phone-tier DataGrid pattern (<768px) -- hide inline actions, tap row to expand, actions inside expansion.
UX-DR63: Implement auto-generation scale limit -- >15 fields triggers FcColumnPrioritizer. Dev-mode overlay shows hidden columns.
UX-DR64: Implement build-time accent contrast check -- Roslyn analyzer checking contrast ratio against Light/Dark backgrounds. Blocks build via TreatWarningsAsErrors.
UX-DR65: Implement null handling -- em dash for null values everywhere. Never "null", "N/A", or empty cells.
UX-DR66: Implement Task Tracker sample domain (not counter) -- list with status badges, inline actions, command form, lifecycle loop, empty states, 3-5 seeded tasks. Counter in docs only.
UX-DR67: Implement IShortcutService with conflict detection -- framework shortcuts at shell level, build-time warning on adopter conflicts.
UX-DR68: Implement command palette contextual commands -- current bounded context first, cross-context follows. Badge counts in results. "shortcuts" query for reference.
UX-DR69: Implement home directory urgency ranking -- badge count descending. Global subtitle orientation.
UX-DR70: Implement real-time badge count updates -- sidebar and palette via IBadgeCountService subscribing to SignalR hub for ActionQueue projection events.
UX-DR71: Implement Fluent UI dependency management protocol -- zero-override, upstream bug reports, narrow critical-bug shim only. No forks, no CSS overrides, no private patches.

### FR Coverage Map

FR1: Epic 2 - Command form auto-generation from [Command] attribute
FR2: Epic 1 - DataGrid auto-generation from [Projection] attribute
FR3: Epic 1 - Bounded context grouping via [BoundedContext] attribute
FR4: Epic 4 - Projection role hints via [ProjectionRole]
FR5: Epic 4 - Semantic status badges via [ProjectionBadge]
FR6: Epic 1 - Field type inference for input components
FR7: Epic 9 - Build-time drift detection between backend and UI
FR8: Epic 2 - Action density rules (inline/compact/full-page)
FR9: Epic 4 - Explicit placeholder for unsupported field types
FR10: Epic 4 - Field descriptions as contextual help
FR11: Epic 4 - Meaningful empty states with CTAs
FR12: Epic 4 - Filter, sort, and text-search in DataGrids
FR13: Epic 1 - Single NuGet meta-package installation
FR14: Epic 3 - Customizable accent color theme
FR15: Epic 3 - Light/Dark/System theme toggle
FR16: Epic 3 - Display density selection (Compact/Comfortable/Roomy)
FR17: Epic 3 - Collapsible sidebar with bounded context nav groups
FR18: Epic 3 - Command palette (Ctrl+K) fuzzy search
FR19: Epic 3 - Session resumption with last state restored
FR20: Epic 4 - Expand entity row in-place for detail
FR21: Epic 3 - "New" badge on newly available capabilities
FR22: Epic 5 - Form state preservation across connection interruptions
FR23: Epic 2 - Five-state command lifecycle rendering
FR24: Epic 5 - SignalR connection loss detection and inline note
FR25: Epic 5 - SignalR reconnection with group rejoin and ETag catch-up
FR26: Epic 5 - Batched stale projection updates as single sweep
FR27: Epic 5 - Auto-dismissing reconnect notification
FR28: Epic 5 - Domain-specific command rejection messages
FR29: Epic 5 - Idempotent command outcome handling
FR30: Epic 2 - Exactly-one user-visible outcome per command
FR31: Epic 5 - ETag-gated polling fallback when SignalR unavailable
FR32: Epic 5 - Swappable service contracts for EventStore communication
FR33: Epic 5 - Client-side ETag cache with bounded eviction
FR34: Epic 5 - Full HTTP response matrix handling
FR35: Epic 7 - Tenant context propagation from JWT
FR36: Epic 2 - ULID message identifiers for idempotency
FR37: Epic 7 - OIDC/SAML integration (Keycloak, Entra ID, GitHub, Google)
FR38: Epic 1 - Aspire hosting builder registration via .WithDomain<T>()
FR39: Epic 6 - Annotation-level field rendering override
FR40: Epic 6 - Template-level component rendering override
FR41: Epic 6 - Slot-level field replacement via lambda expressions
FR42: Epic 6 - Full component replacement with lifecycle wrapper preserved
FR43: Epic 6 - Build-time customization contract validation
FR44: Epic 6 - Hot reload for all four customization levels
FR45: Epic 6 - Actionable error messages with diagnostic IDs
FR46: Epic 7 - Authorization policies via declarative attributes
FR47: Epic 6 - Error boundary isolation for faulty overrides
FR48: Epic 5 - Build-time infrastructure coupling enforcement
FR49: Epic 8 - Domain model as typed MCP tools
FR50: Epic 8 - Typed MCP tool parameters from validation rules
FR51: Epic 8 - Hallucination rejection with suggestion response
FR52: Epic 8 - Two-call lifecycle pattern for MCP commands
FR53: Epic 8 - Projections as Markdown tables/cards/timelines for agents
FR54: Epic 8 - Tenant-scoped MCP tool enumeration
FR55: Epic 8 - Versioned skill corpus as NuGet + MCP resources
FR56: Epic 8 - Typed NuGet contracts shared across surfaces
FR57: Epic 8 - Agent runtime command/query with same lifecycle semantics
FR58: Epic 8 - Agent build-time code generation from prompt corpus
FR59: Epic 8 - Schema hash fingerprints for version negotiation
FR60: Epic 8 - Migration delta diagnostic on schema hash divergence
FR61: Epic 8 - Rendering abstraction contract for multi-surface delivery
FR62: Epic 1 - Project template with ready-to-run Aspire topology
FR63: Epic 9 - CLI tool to inspect source-generator output
FR64: Epic 9 - CLI migration tool for cross-version upgrades
FR65: Epic 9 - IDE parity (VS, Rider, VS Code with C# Dev Kit)
FR66: Epic 9 - Dedicated diagnostic ID range per package
FR67: Epic 9 - API deprecation with migration path via diagnostic ID
FR68: Epic 9 - Diataxis-genre documentation site
FR69: Epic 9 - Migration guide for skill-corpus-breaking changes
FR70: Epic 1 - Incremental hot reload on domain attribute change (pulled forward from Epic 9)
FR71: Epic 10 - Test host and utilities for adopter component tests
FR72: Epic 5 - OpenTelemetry structured logging with distributed tracing (pulled forward from Epic 10)
FR73: Epic 10 - Nightly LLM code-generation benchmark
FR74: Epic 1 - Semantic version releases from conventional commits (pulled forward from Epic 10)
FR75: Epic 10 - CycloneDX SBOM and signed NuGet packages
FR76: Epic 10 - Automated accessibility CI checks blocking on violations
FR77: Epic 10 - Visual specimen verification per release
FR78: Epic 10 - Consumer-driven Pact contract tests
FR79: Epic 10 - Mutation testing on source generator
FR80: Epic 10 - Flaky test quarantine with reintroduction gate
FR81: Epic 10 - Property-based testing for command idempotency
FR82: Epic 5 - SignalR fault injection test harness (pulled forward from Epic 10)

## Epic List

**NOTE: Story creation (Step 3) must map relevant UX-DRs and NFRs to each epic. UX-DRs and NFRs are not tracked in the FR coverage map but must be woven into story acceptance criteria. NFRs are cross-cutting quality attributes (e.g., NFR29 WCAG AA applies to all UI epics, NFR82 onboarding time applies to Epic 1). UX-DRs define specific component and interaction implementations that must be assigned to the epic where their parent FR lives.**

**EPIC ORDERING NOTE: Epics 2, 3, and 4 are parallel-eligible after Epic 1 -- numbering indicates priority, not mandatory sequence. A solo maintainer will build sequentially, but the epics are designed so any of 2/3/4 could come first after Epic 1.**

**STORY SIZING GUIDANCE:** Target 1-3 day implementation for a single dev agent. Stories with "Implementation note" annotations may expand to 2-5 days and can be split into sub-stories during implementation. Stories with 3-4 ACs are typically 1-day efforts; stories with 6-8 ACs are typically 2-3 day efforts.

**VELOCITY BASELINE:** Before committing to the v0.3 timeline, measure actual implementation time on the first 3 stories of Epic 1. Use this as a solo-maintainer velocity baseline to calibrate sprint capacity for subsequent epics. Adjust story count per sprint based on measured throughput, not estimates.

**AGENT VALIDATION GATE:** The v0.3 milestone (Epics 1-5) focuses on developer and business user value. Before declaring v0.3 complete, validate that the foundations for LLM agent integration (typed contracts in Contracts package, lifecycle state machine, EventStore abstractions) are agent-ready -- meaning Epic 8 stories can build on them without retrofit. This is a design validation, not a feature gate.

**MILESTONE MAP:**
- **v0.1 (demo-able):** Epics 1 + 2 -- developer can show annotated domain types producing DataGrid + command forms with lifecycle feedback
- **v0.3 (beta-usable):** Epics 1-5 -- business user has a complete, reliable composed application
- **v1-rc (feature-complete):** Epics 1-8 -- all surfaces (web, agent, customization) functional with auth
- **v1 (ship):** Epics 1-10 -- full tooling, documentation, and quality gates

### Epic 1: Project Scaffolding & First Auto-Generated View
Developer can scaffold a FrontComposer project, register a domain with minimal ceremony, and see an auto-generated DataGrid from [Projection]-annotated types running in an Aspire topology. Includes the W1 architecture phase: MSBuild spine (Directory.Build.props, Directory.Packages.props, deps.local.props), Contracts package with attributes, SourceTools generator stub, Counter sample domain, CI gates 1-3, semantic-release pipeline, and hot reload for domain attribute changes. Includes a Fluent UI v5 migration contingency story to handle potential breaking changes if v5 goes GA during development.
**Story ordering:** (1) MSBuild spine + submodule isolation -> "project builds and restores" validation gate, (2) Contracts package with attributes + IRenderer skeleton + IStorageService contract + InMemoryStorageService, (3) Fluxor setup and base state infrastructure, (4) SourceTools generator stub with ForAttributeWithMetadataName, (5) Counter sample domain + Aspire topology, (6) CI gates 1-3 + semantic-release pipeline, (7) hot reload validation.
**FRs covered:** FR2, FR3, FR6, FR13, FR38, FR62, FR70, FR74

### Epic 2: Command Submission & Lifecycle Feedback
Business user can submit commands through auto-generated forms (inline, compact, or full-page based on field count) and see a five-state lifecycle with guaranteed exactly-one-outcome semantics. **Scope boundary: Epic 2 covers the happy path (stable connection). Epic 5 extends for degraded/disconnected conditions.** Story acceptance criteria must explicitly state "assumes stable connection."
**FRs covered:** FR1, FR8, FR23, FR30, FR36

### Epic 3: Composition Shell & Navigation Experience
Business user can navigate bounded contexts via a collapsible sidebar, toggle Light/Dark/System themes, set display density, invoke a command palette (Ctrl+K), resume prior sessions, and discover new capabilities via "New" badges.
**FRs covered:** FR14, FR15, FR16, FR17, FR18, FR19, FR21

### Epic 4: Rich DataGrid & Projection Interaction
Business user can filter, sort, and search DataGrid views; expand entity rows in-place for detail; see status badges with semantic palette, role-based rendering hints, empty states with CTAs, field descriptions as contextual help, and explicit placeholders for unsupported types.
**FRs covered:** FR4, FR5, FR9, FR10, FR11, FR12, FR20

### Epic 5: Reliable Real-Time Experience
Business user gets instant feedback on command outcomes, sees live data updates via SignalR, and experiences graceful recovery from network interruptions -- with batched reconnection sweeps, preserved form state, ETag-based caching, idempotent handling, domain-specific rejection messages, polling fallback, structured logging with distributed tracing, and a SignalR fault injection test harness. **Scope boundary: Epic 5 extends Epic 2's happy path for degraded/disconnected conditions.** Story acceptance criteria must explicitly state which degraded condition is being handled. **Story ordering is critical:** stories should layer basic EventStore communication first (FR32 abstraction, FR34 HTTP handling, FR33 caching), then SignalR connectivity (FR24 detection, FR25 reconnection), then resilience behaviors (FR26 batching, FR27 notifications, FR28-29 idempotency), then cross-cutting concerns (FR48 enforcement, FR72 logging, FR82 fault harness).
**FRs covered:** FR22, FR24, FR25, FR26, FR27, FR28, FR29, FR31, FR32, FR33, FR34, FR48, FR72, FR82

### Epic 6: Developer Customization Gradient
Developer can customize generated UI at four levels -- annotation overrides, typed Razor templates, slot-level field replacement, and full component replacement -- with hot reload, build-time contract validation, error boundaries, and actionable error messages with diagnostic IDs. **Depends on Epics 1-4** (not just Epic 1) -- customization targets the generated views, command forms, DataGrid features, and shell components built in those epics.
**FRs covered:** FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR47

### Epic 7: Authentication, Authorization & Multi-Tenancy
Users authenticate via OIDC/SAML (Keycloak, Entra ID, GitHub, Google), commands are authorized via declarative policy attributes, and tenant context from JWT is propagated and enforced across all command, query, and subscription operations.
**FRs covered:** FR35, FR37, FR46

### Epic 8: MCP & Agent Integration
LLM agents can issue commands and read projections via typed MCP tools with hallucination rejection, tenant-scoped enumeration, two-call lifecycle pattern, Markdown-rendered projections, and shared typed NuGet contracts. **v1.x-deferrable within this epic:** versioned skill corpus (FR55), build-time LLM code generation (FR58), schema hash fingerprints (FR59), migration delta diagnostics (FR60), and rendering abstraction contract (FR61).
**FRs covered:** FR49, FR50, FR51, FR52, FR53, FR54, FR55*, FR56, FR57, FR58*, FR59*, FR60*, FR61* (*v1.x-deferrable)

### Epic 9: Developer Tooling & Documentation
Developer has CLI tools (inspect generator output, migration), build-time drift detection, IDE parity (VS/Rider/VS Code), diagnostic ID ranges with doc links, deprecation with migration paths, and Diataxis-genre documentation site. Built incrementally alongside earlier epics.
**FRs covered:** FR7, FR63, FR64, FR65, FR66, FR67, FR68, FR69

### Epic 10: Framework Quality & Adopter Confidence
Framework provides test host/utilities for adopters, automated CI gates (accessibility checks, visual specimens, Pact contracts, mutation testing, property-based idempotency, flaky quarantine), LLM code-generation benchmark, and signed releases with SBOM. Built incrementally alongside earlier epics -- quality gates are woven into each phase, not deferred to the end. **Note:** FR79 (mutation testing) aligns with Epic 1's generator; FR78 (Pact contracts) and FR81 (property-based testing) align with Epic 5's resilience code. Story creation should consider implementation timing alongside these epics even though they are organized here for tracking.
**FRs covered:** FR71, FR73, FR75, FR76, FR77, FR78, FR79, FR80, FR81

---

## Epic 1: Project Scaffolding & First Auto-Generated View

Developer can scaffold a FrontComposer project, register a domain with minimal ceremony, and see an auto-generated DataGrid from [Projection]-annotated types running in an Aspire topology.

### Story 1.1: Solution Structure & Build Infrastructure

As a developer,
I want a correctly structured .NET 10 solution with MSBuild spine, central package management, and submodule isolation,
So that all subsequent framework packages build cleanly from a single `dotnet restore && dotnet build`.

**Acceptance Criteria:**

**Given** the repository is cloned with submodules initialized
**When** `dotnet restore` is run from the repository root
**Then** restore completes successfully with zero warnings
**And** Directory.Build.props contains FrontComposerRoot property guard preventing walk-up import from EventStore/Tenants submodules
**And** submodules have ImportDirectoryBuildProps=false set

**Given** the solution file exists
**When** the solution structure is inspected
**Then** solution folders src/, samples/, tests/ exist
**And** Directory.Packages.props pins: Microsoft.CodeAnalysis.CSharp >=4.12.0, Fluxor.Blazor.Web 6.9.0, Fluent UI Blazor v5 (exact RC pin), xUnit 2.9.3, bUnit 2.7.2, FluentAssertions, coverlet.collector
**And** global.json pins .NET SDK 10.0.5
**And** .editorconfig and nuget.config are present

**Given** the deps.local.props and deps.nuget.props files exist
**When** UseNuGetDeps is set to false (default) in Directory.Build.props
**Then** deps.local.props is imported and EventStorePath resolves to the root Hexalith.EventStore submodule
**When** UseNuGetDeps is set to true
**Then** deps.nuget.props is imported for NuGet package references

**Given** the Hexalith.EventStore submodule is present
**When** `dotnet build` targets the submodule projects in isolation
**Then** the submodule compiles without errors, confirming integration stability

**References:** FR13, FR62 (partial), NFR93 (.NET 10 only), Architecture W1 Day 1

---

### Story 1.2: Contracts Package with Core Abstractions

As a developer,
I want a Contracts package containing domain model attributes, renderer abstractions, and storage service contracts,
So that all other framework packages and adopter code can depend on stable, change-controlled contracts.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer.Contracts project exists
**When** the project is built
**Then** it multi-targets net10.0 and netstandard2.0
**And** it has zero package dependencies (dependency-free)
**And** it compiles successfully under both target frameworks

**Given** the Contracts package is referenced
**When** a developer inspects available attributes
**Then** the following attributes are defined: [BoundedContext(name)], [Projection], [Command], [ProjectionRole(role)], [ProjectionBadge(slot)], [Display(Name)]
**And** ProjectionRole supports ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard (capped at 5)
**And** BadgeSlot supports Neutral, Info, Success, Warning, Danger, Accent (6 slots)

**Given** the Contracts package is referenced
**When** a developer inspects renderer abstractions
**Then** IRenderer<TModel, TOutput> interface is defined with RenderField, RenderDataGrid, RenderDetail methods
**And** IProjectionRenderer<TProjection> extends IRenderer
**And** RenderContext record is defined with TenantId, UserId, Mode, DensityLevel, IsReadOnly properties

**Given** the Contracts package is referenced
**When** a developer inspects storage abstractions
**Then** IStorageService is defined with 5 methods: GetAsync<T>, SetAsync<T>, RemoveAsync, GetKeysAsync, FlushAsync
**And** InMemoryStorageService implementation exists for Server-side and bUnit testing
**And** cache key pattern follows {tenantId}:{userId}:{featureName}:{discriminator}

**Given** the IRenderer and IStorageService contracts
**When** they are designed in this story
**Then** they are provisional -- designed with awareness of Fluxor state patterns (Story 1.3) but may be hardened after Fluxor setup validates the state shape
**And** any contract changes in Story 1.3 are applied as non-breaking additions (new methods/properties), not redesigns

**References:** FR3, FR6, FR13 (partial), UX-DR56 (Fc naming convention), Architecture ADR-001

---

### Story 1.3: Fluxor State Management Foundation

As a developer,
I want Fluxor state management configured with base feature infrastructure and explicit subscription patterns,
So that all generated and custom components use a consistent, AOT-friendly state management approach.

**Acceptance Criteria:**

**Given** the Shell project references Fluxor.Blazor.Web 6.9.0
**When** Fluxor is registered in the DI container
**Then** registration completes without errors
**And** Fluxor is configured for Blazor Server and Blazor Auto render modes

**Given** a component needs to subscribe to state
**When** the component uses the framework's subscription pattern
**Then** it uses explicit IState<T> inject with subscribe/dispose
**And** FluxorComponent base class is NEVER used (AOT-friendly requirement)
**And** subscription cleanup occurs on component dispose

**Given** the base state features are initialized
**When** the application starts
**Then** ThemeState feature exists with Light/Dark/System values (default: Light)
**And** DensityState feature exists with Compact/Comfortable/Roomy values (default: Comfortable for forms, Compact for DataGrids)
**And** all actions are immutable records with past-tense naming (e.g., ThemeChanged, DensityChanged)
**And** all actions include a CorrelationId property

**Given** ThemeState or DensityState changes
**When** the state is persisted
**Then** persistence uses IStorageService (InMemoryStorageService in dev/test)
**And** CommandLifecycleState is excluded from persistence (ephemeral only)

**References:** Architecture ADR-008, UX-DR23 (color system baseline), UX-DR27 (density baseline)

---

### Story 1.4: Source Generator - Parse Stage

As a developer,
I want a Roslyn incremental source generator that parses domain model attributes into a typed intermediate representation,
So that the framework can reason about domain types at compile time with a testable, pure-function core.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer.SourceTools project exists
**When** the project is built
**Then** it targets netstandard2.0 only
**And** IsRoslynComponent=true and EnforceExtendedAnalyzerRules=true are set
**And** it references Contracts (for attribute types) and Microsoft.CodeAnalysis.CSharp (PrivateAssets="all")
**And** it does NOT reference Shell, Fluxor, or Fluent UI

**Given** a C# type is annotated with [Projection]
**When** the Parse stage runs via ForAttributeWithMetadataName
**Then** the INamedTypeSymbol is extracted into a DomainModel IR record
**And** the IR captures: type name, namespace, properties (name, type, nullability), applied attributes ([BoundedContext], [ProjectionRole], [ProjectionBadge], [Display])
**And** the Parse function is pure (no side effects, no Compilation references in output)

**Given** a test project with known [Projection]-annotated types
**When** Parse stage snapshot tests run
**Then** golden file output (.approved.cs) matches expected DomainModel IR for each test type
**And** the field type coverage matrix includes: string, int, long, decimal, bool, DateTime, DateTimeOffset, DateOnly, enum, Guid, nullable variants, and collections (List<T>, IEnumerable<T>)

**Given** the generator encounters an unsupported field type
**When** the Parse stage processes it
**Then** a diagnostic HFC1001 is emitted (What/Expected/Got/Fix/DocsLink format)
**And** the field is included in the IR with an IsUnsupported flag

**References:** FR2 (partial), FR6 (partial), FR9 (partial), NFR8 (<500ms incremental), Architecture 3-stage pipeline

---

### Story 1.5: Source Generator - Transform & Emit Stages

As a developer,
I want the source generator to transform parsed domain models into Blazor DataGrid components with field type inference, label resolution, and data formatting,
So that annotating a type with [Projection] produces a fully rendered, correctly formatted DataGrid at compile time.

**Acceptance Criteria:**

**Given** a DomainModel IR from the Parse stage
**When** the Transform stage runs
**Then** it produces output models for: a Razor DataGrid component, Fluxor feature/actions/reducers, and a BoundedContext domain registration
**And** Fluxor types are emitted as fully-qualified name strings (no Fluxor dependency in SourceTools)

**Given** output models from Transform
**When** the Emit stage runs
**Then** generated files are named: {TypeName}.g.razor.cs, {TypeName}Feature.g.cs, {TypeName}Actions.g.cs, {BoundedContext}DomainRegistration.g.cs
**And** all generated files go to obj/{Config}/{TFM}/generated/HexalithFrontComposer/
**And** namespaces match folder paths exactly

**Given** a [Projection]-annotated type with various .NET property types
**When** the generated DataGrid renders
**Then** string fields render as text columns
**And** int/long/decimal fields render as right-aligned locale-formatted columns
**And** bool fields render as "Yes"/"No" text
**And** DateTime/DateTimeOffset fields render as short date per CultureInfo
**And** enum fields render as humanized labels (max 30 chars with ellipsis)
**And** null values render as em dash (--) in all columns
**And** column headers use the label resolution chain: [Display(Name)] > humanized CamelCase > raw field name

**Given** a type annotated with [BoundedContext("Orders")]
**When** the generator runs
**Then** it produces a domain registration grouping all projections under the "Orders" navigation section
**And** bounded context display labels support optional domain-language overrides

**Given** the Emit stage output
**When** snapshot tests run
**Then** golden HTML output (.approved.html) matches expected rendered structure using AngleSharp semantic DOM comparison

**References:** FR2, FR3, FR6, UX-DR21 (label resolution), UX-DR35 (data formatting), UX-DR65 (null handling)

---

### Story 1.6: Counter Sample Domain & Aspire Topology

As a developer,
I want a working Counter sample domain running in an Aspire topology that demonstrates end-to-end auto-generation from domain attributes to rendered DataGrid,
So that I can validate the framework works and use it as a reference for building my own domains.

**Acceptance Criteria:**

**Given** the Counter.Domain project exists with [BoundedContext("Counter")], [Projection]-annotated types, and [Command]-annotated records
**When** `dotnet build` runs
**Then** the source generator produces DataGrid components, Fluxor state, and domain registration without errors
**And** non-domain code in Counter.Domain is <= 10 lines (NuGet reference, registration)

**Given** the Counter.Web project references Shell + SourceTools (as analyzer)
**When** the project is built
**Then** SourceTools is referenced via OutputItemType="Analyzer", ReferenceOutputAssembly="false", SetTargetFramework="netstandard2.0"
**And** Contracts is referenced as a normal project reference

**Given** the Counter.AppHost Aspire project exists
**When** `dotnet run` is executed on the AppHost
**Then** the Aspire topology starts with all services registered
**And** the developer can register the domain via .WithDomain<T>() typed extension method
**And** navigating to the web application shows a basic shell layout with the Counter bounded context in navigation

**Given** the application is running
**When** the developer navigates to the Counter projection view
**Then** a DataGrid renders with auto-generated columns from the projection type
**And** the shell uses Fluent UI Blazor v5 with accent color #0097A7
**And** the zero-override strategy is followed (no custom CSS on Fluent UI components)
**And** the basic shell layout follows UX-DR14 (header, navigation sidebar, content area)

**Given** a cold machine with .NET 10 SDK installed
**When** the developer clones the repo and runs the Counter sample
**Then** time-to-first-render is <= 5 minutes (NFR82)

**Given** the project template strategy (UX-DR66)
**When** the shipped project template is finalized (post-W1)
**Then** the Counter sample is replaced with a Task Tracker sample domain demonstrating: list with status badges (To Do amber, Done green), inline action buttons (Complete), a command form (Create Task), lifecycle loop, and meaningful empty states with 3-5 seeded sample tasks
**And** the Counter remains as a minimal example in documentation only

**References:** FR13, FR38, FR62, NFR82, NFR83, UX-DR14 (basic), UX-DR23 (accent color), UX-DR57, UX-DR59, UX-DR66

---

### Story 1.7: CI Pipeline & Semantic Release

As a developer,
I want a CI pipeline with build gates and semantic versioning from conventional commits,
So that every merge is validated and releases are automated with lockstep package versioning.

**Acceptance Criteria:**

**Given** a pull request is opened
**When** the ci.yml workflow runs
**Then** Gate 1 passes: Contracts builds successfully targeting netstandard2.0 in isolation
**And** Gate 2 passes: full solution builds successfully (all projects)
**And** Gate 3 passes: SourceTools.Tests run and pass

**Given** the CI pipeline runs
**When** the inner loop (unit + component tests) completes
**Then** total execution time is < 5 minutes (NFR64)
**And** trim warnings fail the build (IsTrimmable="true" on all framework assemblies, NFR68)

**Given** CI gates are configured
**When** they are first introduced (Epic 1 scope)
**Then** gates run in advisory mode (report but do not block merges)
**And** a note documents that gates will be hardened to blocking in Epic 2

**Given** a conventional commit is merged to main
**When** the semantic-release pipeline runs
**Then** a version number is computed from commit messages (feat/fix/breaking)
**And** all framework packages receive the same version number (lockstep versioning, NFR75)
**And** the conventional commit-msg hook validates commit message format

**Given** the inner loop development experience
**When** a developer runs local unit + component tests
**Then** total execution time is < 5 minutes and is treated as a non-negotiable quality gate from day one (NFR64)
**And** if the inner loop exceeds 5 minutes at any point during Epic 1, it is treated as a blocking issue before new feature work
**And** test infrastructure (fixtures, harnesses, base classes) must be frictionless enough that skipping tests feels harder than running them

**References:** FR74, NFR64, NFR68, NFR70, NFR75, NFR99, Architecture W1 CI gates 1-3

---

### Story 1.8: Hot Reload & Fluent UI Contingency

As a developer,
I want domain attribute changes to trigger incremental source generator rebuilds with hot reload support, and a documented contingency plan for Fluent UI v5 GA migration,
So that my development inner loop is fast and I'm protected against upstream Fluent UI breaking changes.

**Acceptance Criteria:**

**Given** a running application with hot reload enabled
**When** a developer adds or modifies a [Projection] attribute on a domain type
**Then** the source generator incrementally rebuilds affected output
**And** the updated DataGrid reflects the change without full application restart
**And** hot reload latency is < 2 seconds (NFR10)

**Given** a domain attribute change triggers the incremental generator
**When** the rebuild completes
**Then** only the affected domain assembly is regenerated (not the full solution)
**And** rebuild time per domain assembly is < 500ms (NFR8)

**Given** Fluent UI Blazor v5 is pinned at an exact RC version
**When** a new RC or GA release is published upstream
**Then** the documented contingency plan covers: version pin update procedure, load-bearing APIs to validate (FluentLayout, DefaultValues, FluentDataGrid, FluentProviders), expected migration effort (1-2 weeks budget), and rollback procedure
**And** the canary build preparation (canary-fluentui.yml) is documented for implementation in Epic 3 (W2 scope)

**Given** a developer inspects generator output
**When** hot reload limitations apply (e.g., generic type changes, new attribute additions requiring full restart)
**Then** the limitation is documented and a build-time message indicates "Full restart required for this change type"

**References:** FR70, NFR8, NFR10, UX-DR61, Architecture hot reload limitations

---

**Epic 1 Summary:**
- 8 stories covering all 8 FRs (FR2, FR3, FR6, FR13, FR38, FR62, FR70, FR74)
- Relevant NFRs woven into acceptance criteria (NFR8, NFR10, NFR64, NFR68, NFR75, NFR82, NFR83, NFR93, NFR99)
- Relevant UX-DRs addressed (UX-DR14, UX-DR21, UX-DR23, UX-DR35, UX-DR56, UX-DR57, UX-DR59, UX-DR61, UX-DR65)
- Stories are sequentially completable: 1.1 (build infra) -> 1.2 (contracts) -> 1.3 (Fluxor) -> 1.4 (parse) -> 1.5 (transform/emit) -> 1.6 (sample) -> 1.7 (CI) -> 1.8 (hot reload)

---

## Epic 2: Command Submission & Lifecycle Feedback

Business user can submit commands through auto-generated forms (inline, compact, or full-page based on field count) and see a five-state lifecycle with guaranteed exactly-one-outcome semantics. **Scope: happy path (stable connection). Epic 5 extends for degraded/disconnected conditions.**

### Story 2.1: Command Form Generation & Field Type Inference

As a developer,
I want the source generator to produce form components from [Command]-annotated records with automatic field type inference and validation,
So that business users get correctly typed, validated input forms without manual component authoring.

**Acceptance Criteria:**

**Given** a record type annotated with [Command]
**When** the source generator runs
**Then** a Razor form component is emitted with input fields for each non-derivable property
**And** the generated file follows naming convention {CommandName}Form.g.razor.cs

**Given** a generated command form with various property types
**When** the form renders
**Then** string properties render as FluentTextField
**And** bool properties render as FluentCheckbox (toggle style)
**And** DateTime/DateOnly properties render as FluentDatePicker
**And** enum properties render as FluentSelect with humanized option labels
**And** int/long properties render as FluentNumberField
**And** unsupported types render as FcFieldPlaceholder with build-time warning HFC1002

**Given** a generated command form
**When** field labels render
**Then** the label resolution chain applies: [Display(Name)] > IStringLocalizer > humanized CamelCase > raw field name
**And** every field has an associated <label for=""> element
**And** required fields are visually marked
**And** validation messages use aria-describedby for accessibility

**Given** FluentValidation rules exist for the command type
**When** the form is submitted with invalid input
**Then** validation messages appear inline via FluentValidationMessage
**And** the form does not submit until validation passes
**And** EditContext is wired to FluentValidation rules

**Given** the v0.1 milestone scope (Epics 1-2 only, EventStore abstractions not yet available)
**When** command forms submit
**Then** a stub ICommandDispatcher is used that simulates the command lifecycle (Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed) with configurable delays
**And** the stub is replaceable with the real EventStore dispatcher (Story 5.1) without code changes
**And** the Counter/Task Tracker sample demonstrates the full lifecycle against the stub

**References:** FR1, UX-DR22, UX-DR21 (label resolution), UX-DR3 (field placeholder), NFR30 (accessibility labels)

---

### Story 2.2: Action Density Rules & Rendering Modes

As a business user,
I want commands to render at the appropriate density -- inline buttons for simple actions, compact forms for moderate actions, and full-page forms for complex actions,
So that I can take action quickly on simple commands without navigating away, while complex commands get the space they need.

**Acceptance Criteria:**

**Given** a [Command] with 0-1 non-derivable fields
**When** the command appears on a DataGrid row
**Then** it renders as an inline button with Secondary appearance and a leading action icon
**And** clicking the button submits the command immediately (0 fields) or shows a single inline input (1 field)

**Given** a [Command] with 2-4 non-derivable fields
**When** the business user clicks the command action
**Then** a compact inline form slides open below the DataGrid row within the expand-in-row space
**And** derivable fields are pre-filled from: current projection context, last-used value (session-persisted), or command definition default
**And** the expand-in-row scroll stabilization pins the expanded row's top edge to the current viewport position (scrollIntoView block:'nearest' + requestAnimationFrame)
**And** only one row is expanded at a time (v1 constraint)

**Given** a [Command] with 5+ non-derivable fields
**When** the business user clicks the command action
**Then** a full-page form renders at max 720px width, centered
**And** breadcrumb shows the navigation path back to the DataGrid
**And** DataGrid state (scroll position, filters, sort, expanded row) is preserved in a per-view memory object for restoration on return

**Given** the action density determination
**When** the generator analyzes a command's fields
**Then** a field is classified as "derivable" if resolvable from: current projection context, system values (timestamp, user ID), or command definition defaults
**And** only non-derivable fields count toward the density threshold

**References:** FR8, UX-DR16, UX-DR17, UX-DR19 (DataGrid state preservation), UX-DR36 (button hierarchy)

---

### Story 2.3: Command Lifecycle State Management

As a developer,
I want a lifecycle state service that tracks each command through five states with ULID-based idempotency and guarantees exactly one user-visible outcome,
So that every command submission is traceable, replay-safe, and never produces silent failures or duplicate effects.

**Acceptance Criteria:**

**Given** ILifecycleStateService is registered in DI
**When** the service is inspected
**Then** it exposes: Observe(Guid correlationId) returning IObservable<LifecycleState>, GetState(Guid), Transition(Guid, LifecycleState), and ConnectionState property
**And** the service scope is per-circuit in Blazor Server, per-user in Blazor WebAssembly

**Given** a command submission begins
**When** the lifecycle is initialized
**Then** a ULID message identifier is generated for the command
**And** the lifecycle transitions: Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed (or Rejected)
**And** each transition is observable via Observe(correlationId)

**Given** the lifecycle state machine
**When** any command reaches a terminal state (Confirmed or Rejected)
**Then** exactly one user-visible outcome is produced (success notification, rejection message, or error notification)
**And** the CommandLifecycleState is ephemeral (evicted on terminal state, not persisted to IStorageService)
**And** no silent failures occur -- every submission path produces a visible outcome

**Given** a command with a ULID message ID
**When** a duplicate submission with the same ULID arrives
**Then** deterministic duplicate detection identifies it
**And** the duplicate does not produce a second user-visible effect

**Given** the lifecycle state machine under property-based testing
**When** random lifecycle event sequences are generated
**Then** the state machine never enters an invalid state (e.g., Confirmed after Rejected, or Submitting after Confirmed)

**References:** FR23, FR30, FR36, UX-DR12, NFR44, NFR45, NFR47

---

### Story 2.4: FcLifecycleWrapper - Visual Lifecycle Feedback

As a business user,
I want to see clear, progressive visual feedback during command submission so I know the system is working and never wonder "did it work?",
So that I have 100% confidence in every command outcome without needing to manually refresh or double-submit.

**Acceptance Criteria:**

**Given** a command is submitted
**When** the lifecycle enters Submitting state
**Then** FcLifecycleWrapper displays FluentProgressRing
**And** the submit button is disabled to prevent double-submission
**And** aria-live="polite" announces "Submitting..."

**Given** the lifecycle enters Acknowledged -> Syncing
**When** the Confirmed state arrives within 300ms of Acknowledged
**Then** the sync pulse animation never fires (brand-signal fusion frequency rule)
**And** the lifecycle resolves invisibly to the user (NFR11)

**Given** the lifecycle enters Syncing
**When** 300ms-2s elapses without Confirmed
**Then** a subtle sync pulse animation displays on the affected element (accent color)
**And** the sync pulse threshold is configurable via FrontComposerOptions.SyncPulseThresholdMs (default 300)

**Given** the lifecycle remains in Syncing
**When** 2s-10s elapses without Confirmed
**Then** explicit "Still syncing..." inline text is displayed (NFR13)

**Given** the lifecycle remains in Syncing
**When** >10s elapses without Confirmed
**Then** an action prompt with manual refresh option is displayed (NFR14)

**Given** the lifecycle reaches Confirmed
**When** the success notification renders
**Then** FluentMessageBar (Success) auto-dismisses after 5 seconds
**And** aria-live="polite" announces the confirmation

**Given** the user has prefers-reduced-motion enabled
**When** the sync pulse would normally animate
**Then** the pulse is replaced with an instant state indicator (no animation)
**And** focus ring is never dimmed or suppressed during any lifecycle state

**Given** end-to-end command performance under stable connection
**When** latency is measured via Playwright task timer on localhost Aspire topology
**Then** command click-to-confirmed state P95 cold actor < 800ms (NFR1)
**And** command click-to-confirmed state P50 warm actor < 400ms (NFR2)

**References:** FR23, FR30, UX-DR2, UX-DR48 (sync pulse rule), UX-DR49 (focus ring coexistence), NFR1-2, NFR11-14, NFR88 (zero "did it work?" hesitations)

---

### Story 2.5: Command Rejection, Confirmation & Form Protection

As a business user,
I want domain-specific rejection messages that tell me what went wrong, destructive action confirmation dialogs that prevent accidents, and form abandonment protection that saves my work,
So that I never lose data to accidental navigation, never misunderstand an error, and never accidentally destroy something.

**Acceptance Criteria:**

**Given** a command is rejected by the backend
**When** the rejection message renders
**Then** it follows the format: "[What failed]: [Why]. [What happened to the data]." (e.g., "Approval failed: insufficient inventory. The order has been returned to Pending.")
**And** no generic "Action failed" messages are used
**And** FluentMessageBar (Danger) renders with no auto-dismiss
**And** aria-live="assertive" announces the rejection
**And** form input is preserved on rejection (never clear form on error)

**Given** a command produces an idempotent outcome (rejected but intent fulfilled)
**When** the outcome renders
**Then** the message acknowledges success, not failure (e.g., "This order was already approved (by another user). No action needed.")
**And** FluentMessageBar (Info) renders with 3-second auto-dismiss

**Given** a [Command] is annotated or identified as destructive (Delete, Remove, Purge)
**When** the business user clicks the action
**Then** a FluentDialog confirmation appears with: action name as title, description of what will be destroyed, "This action cannot be undone." text
**And** Cancel button has Secondary appearance and is auto-focused (prevents accidental Enter confirmation)
**And** destructive action button has Danger appearance with domain-language label
**And** Escape closes the dialog
**And** destructive actions never appear as inline buttons on DataGrid rows

**Given** a business user is on a full-page form for >30 seconds
**When** the user attempts navigation (breadcrumb, sidebar, command palette)
**Then** FluentMessageBar (Warning) appears at the form top: "You have unsaved input. [Stay on form] [Leave anyway]"
**And** "Stay on form" is Primary and auto-focused
**And** "Leave anyway" is Secondary
**And** the threshold is configurable via FrontComposerOptions.FormAbandonmentThresholdSeconds (default 30)

**Given** non-destructive commands (Approve, Create, Update)
**When** the user submits
**Then** no confirmation dialog is shown (lifecycle wrapper provides feedback)

**Given** the button hierarchy across all command rendering modes
**When** buttons render
**Then** Primary appearance is used for the main action (one per visual context)
**And** Secondary for supporting actions
**And** Outline for tertiary/filter toggles
**And** Danger only for destructive actions (always with confirmation)
**And** all buttons use domain-language labels (e.g., "Send Create Order" not "Submit")
**And** Primary and DataGrid row action buttons include leading icons

**Given** a business user experiences their first command rejection or error
**When** the error state resolves
**Then** the user can clearly understand: what happened, what state their data is in, and what action to take next (retry, modify input, or abandon)
**And** the recovery path requires zero external documentation -- the UI itself guides recovery
**And** after recovery, the user's confidence is restored (no lingering "did it work?" uncertainty)

**References:** FR30, UX-DR36, UX-DR37, UX-DR38, UX-DR39, UX-DR46, UX-DR58, NFR46, NFR47, NFR103

---

**Epic 2 Summary:**
- 5 stories covering all 5 FRs (FR1, FR8, FR23, FR30, FR36)
- Relevant NFRs woven into acceptance criteria (NFR1-2, NFR11-14, NFR44-45, NFR47, NFR88)
- Relevant UX-DRs addressed (UX-DR2, UX-DR3, UX-DR12, UX-DR16, UX-DR17, UX-DR19, UX-DR21, UX-DR22, UX-DR36-39, UX-DR46, UX-DR48-49, UX-DR58)
- Scope boundary: all stories assume stable connection; degraded-path handling is Epic 5
- Stories are sequentially completable: 2.1 (form generation) -> 2.2 (density rules) -> 2.3 (lifecycle state) -> 2.4 (visual feedback) -> 2.5 (rejection/confirmation)

---

## Epic 3: Composition Shell & Navigation Experience

Business user can navigate bounded contexts via a collapsible sidebar, toggle Light/Dark/System themes, set display density, invoke a command palette (Ctrl+K), resume prior sessions, and discover new capabilities via "New" badges.

### Story 3.1: Shell Layout, Theme & Typography

As a business user,
I want a well-structured application shell with a configurable theme and consistent typography,
So that the composed application feels professional and I can switch between light and dark modes based on my preference.

**Acceptance Criteria:**

**Given** the application shell renders
**When** the layout is inspected
**Then** FluentLayout + FluentLayoutItem compose three areas: Header (48px), Navigation sidebar (~220px expanded, ~48px collapsed), Content area
**And** the Header contains: app title, breadcrumbs, Ctrl+K command palette trigger icon, theme toggle, settings icon
**And** forms in the Content area constrain to max 720px width
**And** DataGrids render at full content area width

**Given** the shell's accent color
**When** the developer inspects the configuration
**Then** the default accent color is #0097A7 (--accent-base-color)
**And** the accent color is overridable at deployment via configuration
**And** no custom CSS overrides are applied on Fluent UI components (zero-override strategy)

**Given** the theme toggle in the header
**When** the business user selects Light, Dark, or System
**Then** the theme switches instantly via Fluent UI <fluent-design-theme> at the shell layer
**And** System mode follows OS preference via prefers-color-scheme media query
**And** the selected theme is persisted in LocalStorage
**And** the theme is restored on return visits

**Given** the color system
**When** semantic tokens are inspected
**Then** six semantic color slots are defined: Accent (#0097A7), Neutral (shell chrome/borders), Success (confirmed/approved), Warning (pending/stale), Danger (rejected/destructive), Info (informational/"New" badges)
**And** command lifecycle states map to these slots: Idle=Neutral, Submitting=Accent, Acknowledged=Neutral, Syncing=Accent, Confirmed=Success, Rejected=Danger

**Given** the Typography API
**When** a component references Hexalith.FrontComposer.Typography constants
**Then** the following mappings are available: AppTitle=Title1, BoundedContextHeading=Subtitle1, ViewTitle=Title3, SectionHeading=Subtitle2, FieldLabel=Body1Strong, Body=Body1, Secondary=Body2, Caption=Caption1, Code=Body1+monospace
**And** no mapping changes occur in patch versions; minor version changes are documented with before/after screenshots

**Given** the IStorageService abstraction for real persistence
**When** the application runs in Blazor WebAssembly mode
**Then** LocalStorageService is available as the IStorageService implementation
**And** it supports LRU eviction with configurable max entries
**And** SetAsync is fire-and-forget (does not block render)
**And** FlushAsync is called via beforeunload JS interop hook in App.razor

**Given** framework-generated UI strings
**When** localization is configured
**Then** English and French resource files are provided via IFluentLocalizer (UX-DR60)
**And** the label resolution chain applies to all typographic elements
**And** adopters can provide additional language translations via IStringLocalizer

**References:** FR14, FR15, UX-DR14, UX-DR23, UX-DR25, UX-DR26, UX-DR57, UX-DR60

---

### Story 3.2: Sidebar Navigation & Responsive Behavior

As a business user,
I want a collapsible sidebar with bounded context navigation groups that adapts to my screen size,
So that I can quickly navigate between domains and still have a usable experience on smaller screens.

**Acceptance Criteria:**

**Given** bounded contexts are registered with the framework
**When** the sidebar renders
**Then** each bounded context appears as a collapsible FluentNav group
**And** nav groups support up to two levels of hierarchy depth
**And** nav items are projection views only (commands are not nav items)
**And** collapsed-group state is persisted in LocalStorage

**Given** the sidebar toggle
**When** the business user clicks the FluentLayoutHamburger toggle
**Then** the sidebar collapses to icon-only (~48px) or expands (~220px)
**And** the preference is persisted in LocalStorage
**And** all new users start with the sidebar expanded

**Given** a desktop viewport (>=1366px)
**When** the shell renders
**Then** the sidebar is expanded by default with full labels and group hierarchy

**Given** a compact desktop viewport (1024-1365px)
**When** the shell renders
**Then** the sidebar auto-collapses to icon-only (~48px)
**And** the hamburger toggle expands it as an overlay
**And** breadcrumbs may truncate

**Given** a tablet viewport (768-1023px)
**When** the shell renders
**Then** the sidebar renders as a drawer navigation
**And** density is forced to comfortable for 44px touch targets
**And** drawer nav items are at least 48px tall

**Given** a phone viewport (<768px)
**When** the shell renders
**Then** the layout is single-column with drawer navigation
**And** dev-mode overlay is not supported at this viewport

**Given** keyboard navigation
**When** the user tabs through sidebar items
**Then** focus is visible with Fluent --colorStrokeFocus2
**And** all nav items are reachable via keyboard in DOM order

**References:** FR17, UX-DR15, UX-DR28, UX-DR29, UX-DR30 (keyboard parity), NFR89 (<=2 clicks)

---

### Story 3.3: Display Density & User Settings

As a business user,
I want to choose my preferred display density and access settings easily,
So that the application matches my work style -- compact for scanning, roomy for detailed work.

**Acceptance Criteria:**

**Given** the density system
**When** the user has not set a preference
**Then** the 4-tier precedence applies: (1) user preference in LocalStorage, (2) deployment-wide default via config, (3) factory hybrid defaults (compact for DataGrids/dev-mode, comfortable for detail views/forms/nav sidebar), (4) per-component default
**And** the density is applied via --fc-density CSS custom property on <body>

**Given** the settings icon in the header (Ctrl+, shortcut)
**When** the user opens the settings panel
**Then** a FluentDialog renders with: density radio options (Compact, Comfortable, Roomy), theme selector, and a live preview showing one DataGrid row + one form field + one nav item
**And** changes take effect immediately in the preview

**Given** the user selects a density preference
**When** the preference is applied
**Then** all generated views update to the selected density
**And** the preference is stored in LocalStorage
**And** the preference persists across sessions

**Given** a viewport < 1024px
**When** the responsive density override applies
**Then** density is forced to comfortable regardless of user preference
**And** this ensures 44x44px minimum touch targets
**And** components not inheriting density (command palette results, filter badges) apply responsive CSS padding

**Given** the Roomy density level
**When** it is selected
**Then** it is a permanent first-class feature (never removed)
**And** it is designed to support accessibility scenarios requiring larger touch targets and more whitespace

**References:** FR16, UX-DR27, UX-DR29, UX-DR30 (Roomy as accessibility feature)

---

### Story 3.4: FcCommandPalette & Keyboard Shortcuts

As a business user,
I want a command palette that lets me fuzzy-search across all projections, commands, and recent views from anywhere in the application,
So that I can navigate and take action in 2 keystrokes instead of clicking through menus.

**Acceptance Criteria:**

**Given** the user is anywhere in the application
**When** Ctrl+K is pressed or the header palette icon is clicked
**Then** the FcCommandPalette overlay opens with FluentSearch auto-focused
**And** the overlay has role="dialog" with aria-label, focus trap, and aria-activedescendant tracking

**Given** the command palette is open
**When** the user types a search query
**Then** results appear after 150ms debounce with fuzzy matching against bounded context names, projection names, and command names
**And** results are categorized: Projections, Commands, Recent
**And** badge counts from IBadgeCountService appear on ActionQueue-hinted projection results
**And** search response time is < 100ms (NFR5)

**Given** the command palette is invoked from within a bounded context
**When** results render
**Then** commands for the current bounded context appear first (contextual mode)
**And** cross-context results follow

**Given** the command palette results
**When** the user navigates with keyboard
**Then** arrow keys move between results, Enter selects, Escape closes
**And** results use role="listbox" / role="option"
**And** screen reader announces result count per keystroke

**Given** the user types "shortcuts" in the command palette
**When** results render
**Then** a complete shortcut reference is displayed

**Given** IShortcutService is registered
**When** framework shortcuts are inspected
**Then** Ctrl+K (open command palette), Ctrl+, (open settings), g h (go to home) are registered at shell level
**And** adopter custom components that register conflicting shortcuts produce a build-time warning
**And** native keyboard behaviors (Escape for dialogs, arrows for lists) rely on ARIA roles and DOM focus, not IShortcutService

**Given** IBadgeCountService may not yet be available (Story 3.5)
**When** the command palette renders before Story 3.5 is implemented
**Then** badge counts gracefully degrade to not shown (no errors, no empty badges)
**And** once IBadgeCountService is registered (Story 3.5), counts appear automatically without palette changes

**References:** FR18, UX-DR1, UX-DR43, UX-DR67, UX-DR68, NFR5, NFR89

---

### Story 3.5: Home Directory, Badge Counts & New Capability Discovery

As a business user,
I want a home page that shows me what needs attention across all domains, with live badge counts and subtle indicators for newly available capabilities,
So that I can prioritize my work and notice new features without being disrupted by announcements.

**Acceptance Criteria:**

**Given** IBadgeCountService is registered
**When** the application starts
**Then** the service fetches initial ActionQueue badge counts via parallel lightweight queries
**And** subscribes to SignalR hub for projection update events filtered to ActionQueue-hinted types
**And** Counts (IReadOnlyDictionary<ProjectionType, int>), CountChanged (IObservable<BadgeCountChangedArgs>), and TotalActionableItems (int) are available
**And** scope is per-circuit in Blazor Server, singleton in Blazor WebAssembly

**Given** the FcHomeDirectory renders as the v1 home page
**When** there are items needing attention
**Then** a global subtitle displays: "Welcome back, [user name]. You have [N] items needing attention across [M] areas."
**And** bounded context cards are sorted by badge count descending (urgency ranking)
**And** each card shows: group name, projection entries with badge counts, click-through arrow
**And** zero-urgency contexts are listed in a collapsed "Other areas" section

**Given** the home directory
**When** no items need attention across any context
**Then** "All caught up" message displays

**Given** the home directory
**When** no bounded contexts are registered
**Then** an empty state displays with a getting-started guide link

**Given** the home directory
**When** data is loading
**Then** FluentSkeleton cards render as loading placeholders

**Given** the home directory has role="main" landmark
**When** keyboard navigation is used
**Then** cards are navigable with role="link"
**And** badge counts are included in aria-label
**And** sort order is communicated via aria-description="Sorted by urgency"

**Given** a new bounded context or projection becomes available
**When** it appears in navigation for the first time
**Then** a subtle "New" badge (Info color slot) is shown
**And** the badge is removed after the user's first visit
**And** the nav entry appears only when at least one projection contains data (no empty nav entries)

**Given** badge counts in the sidebar nav
**When** the SignalR hub emits projection update events
**Then** sidebar badge counts update in real-time via IBadgeCountService
**And** command palette results also reflect updated counts

**References:** FR21, UX-DR10, UX-DR13, UX-DR52, UX-DR69, UX-DR70, NFR87

---

### Story 3.6: Session Persistence & Context Restoration

As a business user,
I want to return to exactly where I left off -- same navigation section, same filters, same scroll position,
So that context switches (lunch, meetings, browser restarts) don't cost me time re-establishing my workspace.

**Acceptance Criteria:**

**Given** a returning user with prior session state in LocalStorage
**When** the application loads
**Then** the user lands on their last active navigation section
**And** last applied filters per DataGrid are restored
**And** last sort order is restored
**And** last expanded row is restored
**And** the experience matches NFR90 (session resumption)

**Given** a first-visit user with no session state
**When** the application loads
**Then** the user sees the FcHomeDirectory sorted by badge count descending
**And** no error or empty state is shown

**Given** LocalStorage is unavailable (IT policy, full, private browsing)
**When** the application loads
**Then** the user starts from the home directory without error
**And** no error messages, warnings, or degraded UI indicators are shown
**And** the application functions normally without persistence

**Given** session state is being persisted
**When** state changes occur (navigation, filter change, sort change, row expansion)
**Then** state is written to LocalStorage with compact JSON schema
**And** keys follow the pattern bounded-context:projection-type for per-DataGrid state
**And** only UI preference state is stored -- zero PII, zero business data (NFR17)

**Given** the user navigates to a DataGrid, applies filters, sorts, and expands a row
**When** the user navigates away and returns
**Then** all DataGrid state (scroll position, filters, sort, expanded row, selected row highlight) is restored from the per-view memory object (within-session)
**And** cross-session state (filters, sort, expanded row) is restored from LocalStorage

**Given** filter values persisted in LocalStorage
**When** the persistence mechanism stores filter state
**Then** only filter metadata is stored (column name, operator, filter text) -- not full business entity data
**And** if filter text contains business data (e.g., customer name used as filter), this is acknowledged as user-initiated browser-local storage with the same trust model as browser history
**And** no server-side business data is proactively written to LocalStorage by the framework

**References:** FR19, UX-DR20, UX-DR53, NFR17, NFR90

---

**Epic 3 Summary:**
- 6 stories covering all 7 FRs (FR14, FR15, FR16, FR17, FR18, FR19, FR21)
- Relevant NFRs woven into acceptance criteria (NFR5, NFR17, NFR29-34, NFR87, NFR89, NFR90)
- Relevant UX-DRs addressed (UX-DR1, UX-DR10, UX-DR13-15, UX-DR20, UX-DR23, UX-DR25-30, UX-DR43, UX-DR52-53, UX-DR57, UX-DR67-70)
- Stories are sequentially completable: 3.1 (shell/theme) -> 3.2 (sidebar) -> 3.3 (density/settings) -> 3.4 (command palette) -> 3.5 (home/badges) -> 3.6 (session persistence)

---

## Epic 4: Rich DataGrid & Projection Interaction

Business user can filter, sort, and search DataGrid views; expand entity rows in-place for detail; see status badges with semantic palette, role-based rendering hints, empty states with CTAs, field descriptions as contextual help, and explicit placeholders for unsupported types.

### Story 4.1: Projection Role Hints & View Rendering

As a developer,
I want to annotate projections with role hints that change how the framework renders them, with contextual subtitles that orient business users,
So that each projection view is optimized for its purpose without writing custom rendering code.

**Acceptance Criteria:**

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.ActionQueue, WhenState = "Pending,Submitted")]
**When** the view renders
**Then** items are sorted by priority with inline action buttons
**And** only items matching the specified explicit states appear
**And** the view drives home badge counts via IBadgeCountService

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.StatusOverview)]
**When** the view renders
**Then** aggregate counts per badge slot are displayed
**And** clicking a status group navigates to the DataGrid filtered by that status

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.DetailRecord)]
**When** the view renders
**Then** a single-entity detail view renders using FluentCard + FluentAccordion
**And** it appears inside the expand-in-row context

**Given** a projection annotated with [ProjectionRoleHint(RoleHint.Timeline)]
**When** the view renders
**Then** a chronological event list renders with timestamps and status badges in vertical timeline layout

**Given** a projection with no role hint annotation
**When** the view renders
**Then** it uses the Default rendering: standard compact DataGrid, sortable, with inline actions per density rules

**Given** any projection view
**When** the view title renders
**Then** a contextual subtitle appears below the title:
**And** ActionQueue: "[N] [entities] awaiting your [action]"
**And** StatusOverview: "[N] total across [M] statuses"
**And** Default: "[N] [entities]"

**Given** the role hint system
**When** the total hint count is evaluated
**Then** the set is permanently capped at 5-7 (ActionQueue, StatusOverview, DetailRecord, Timeline, Default)
**And** future hints use template overrides via the customization gradient

**Given** any auto-generated projection view
**When** data is being fetched
**Then** a Loading state renders with FluentSkeleton per-component placeholders matching the expected layout
**And** every generated view handles 3 states: Loading (FluentSkeleton), Empty (FcEmptyState from Story 4.6), and Data (normal rendering)

**References:** FR4, UX-DR44, UX-DR45

---

### Story 4.2: Status Badge System

As a business user,
I want to see color-coded status badges on projection items that communicate state at a glance,
So that I can scan a DataGrid and instantly identify which items need attention.

**Acceptance Criteria:**

**Given** an enum value annotated with [ProjectionBadge(BadgeSlot.Warning)]
**When** the badge renders in a DataGrid or detail view
**Then** it uses the Warning semantic color slot (--palette-yellow-*/amber)
**And** the badge includes both color AND text label (color is never the sole signal)

**Given** the 6-slot badge palette
**When** badges render
**Then** the following mappings apply:
**And** Neutral (Draft, Created, Unknown) -- neutral color
**And** Info (Submitted, InReview, Queued) -- blue
**And** Success (Approved, Confirmed, Completed, Shipped) -- green
**And** Warning (Pending, Delayed, Partial, NeedsAttention) -- amber
**And** Danger (Rejected, Cancelled, Failed, Expired) -- red
**And** Accent (Active, Running, Highlighted) -- teal

**Given** an enum value with no [ProjectionBadge] annotation or an unknown slot
**When** the badge renders
**Then** it falls back to Neutral appearance
**And** a build-time warning is emitted

**Given** a developer needs more than 6 badge states
**When** the escape path is used
**Then** a custom badge component can be provided via the customization gradient (Epic 6)
**And** the custom component must honor the accessibility contract (color + text/icon)

**Given** any badge in the application
**When** accessibility is evaluated
**Then** the badge text label is always present and readable
**And** color contrast meets WCAG AA (4.5:1 for normal text, 3:1 for UI components)

**References:** FR5, UX-DR24, UX-DR30 (color never sole signal), NFR32

---

### Story 4.3: DataGrid Filtering, Sorting & Search

As a business user,
I want to filter, sort, and search within DataGrid views with my preferences remembered across sessions,
So that I can quickly find the items I need without re-applying my filters every time.

**Acceptance Criteria:**

**Given** a DataGrid with column headers
**When** the user interacts with column filters
**Then** FluentSearch inputs appear in column headers
**And** filtering is debounced at 300ms
**And** filtering is server-side via ETag-cached query parameters
**And** keyboard shortcut "/" focuses the first column filter

**Given** a DataGrid with status badge columns
**When** status filter chips render above the DataGrid
**Then** FluentBadge toggle chips appear for each badge slot with items
**And** clicking toggles the filter: filled appearance = active, outline = inactive
**And** multiple status filters can be active simultaneously

**Given** a DataGrid with active filters
**When** the filter state is inspected
**Then** a filter visibility summary appears below the DataGrid header: "Filtered: Status = Pending, Approved | 12 of 47 orders"
**And** a "Reset filters" Outline button is available that clears all filters and persisted state

**Given** the IProjectionSearchProvider interface
**When** an adopter registers an implementation
**Then** a global search UI appears in the DataGrid header
**When** no implementation is registered
**Then** the search UI is hidden (no empty search box)

**Given** DataGrid filter state (column filters + status filters + sort order + search query)
**When** the user navigates away and returns
**Then** filter state is restored from LocalStorage keyed by bounded-context:projection-type
**And** sort order and column filter values persist across sessions

**Given** active filters produce zero results
**When** the empty filtered state renders
**Then** the message shows: "No orders match the current filters. [Reset filters] to see all 47 orders."
**And** this is visually distinct from the FcEmptyState for zero-total-items

**References:** FR12, UX-DR40, UX-DR41, NFR87

---

### Story 4.4: Virtual Scrolling & Column Prioritization

As a business user,
I want DataGrids to handle large datasets smoothly and auto-manage column visibility when projections have many fields,
So that performance stays fast and the view doesn't become unusable with wide data.

**Acceptance Criteria:**

**Given** any DataGrid
**When** virtual scrolling is configured
**Then** Fluent UI <Virtualize> is used as the default (no "load more" buttons, no page numbers)
**And** client-side virtualization is used for < 500 items
**And** server-side virtualization via ItemsProvider is used for 500+ items with ETag-cached queries
**And** a FluentSkeleton row renders at the scroll boundary during server fetch (same height as real row)

**Given** a DataGrid with 500 virtualized rows
**When** rendering performance is measured
**Then** P95 render time is < 300ms (NFR4)

**Given** an initial data fetch that takes > 2000ms
**When** the performance prompt evaluates
**Then** a FluentMessageBar (Info) suggests: "Loading is slow. Add filters to narrow results."

**Given** the MaxUnfilteredItems safety rail (default 10,000)
**When** a projection has more items than the cap
**Then** the message shows: "Showing first 10,000 items. Use filters to find specific records."
**And** a row count displays in the DataGrid header: "47 orders" or "12 of 47 orders"

**Given** a projection with > 15 fields
**When** the FcColumnPrioritizer activates
**Then** the first 8-10 columns are shown by priority ([ColumnPriority] annotation or declaration order)
**And** a "More columns ([N] hidden)" FluentButton (Outline) appears in the column header row
**And** clicking opens a panel with checkboxes to toggle column visibility
**And** column visibility selections are persisted in LocalStorage per projection type

**Given** a projection with <= 15 fields
**When** the DataGrid renders
**Then** FcColumnPrioritizer is transparent (all columns shown, no toggle)

**Given** the column toggle panel
**When** keyboard navigation is used
**Then** the toggle is keyboard-accessible
**And** the panel uses role="dialog" with a checkbox list
**And** screen reader announces "[N] columns hidden. Activate to show more."

**Given** scroll position within a DataGrid
**When** the user navigates away and returns (within-session)
**Then** scroll position is restored from the per-view memory object

**References:** FR12 (partial), UX-DR7, UX-DR42, UX-DR63, NFR4

---

### Story 4.5: Expand-in-Row Detail & Progressive Disclosure

As a business user,
I want to expand an entity row in place to see full details without losing my DataGrid context,
So that I can inspect and act on items without navigating away and losing my scroll position and filters.

**Acceptance Criteria:**

**Given** a DataGrid row
**When** the business user clicks to expand
**Then** the detail view opens in-place below the row
**And** only one row is expanded at a time (v1 constraint)
**And** the previously expanded row collapses

**Given** a projection with <= 12 fields in detail view
**When** the expanded detail renders
**Then** all fields are visible immediately in a 3-column grid within the expanded area

**Given** a projection with > 12 fields in detail view
**When** the expanded detail renders
**Then** primary fields (first 6-8) are visible immediately
**And** secondary fields are grouped into collapsible FluentAccordion sections
**And** sections are labeled by field group (from domain model property grouping annotations, or alphabetical chunking fallback)

**Given** a row expansion on desktop
**When** the expand animation runs
**Then** the expanded row's top edge is pinned to the current viewport position
**And** content below pushes down
**And** scrollIntoView with block:'nearest' plus requestAnimationFrame stabilizes the DOM reflow
**And** animation uses Fluent UI standard expand/collapse transition
**And** prefers-reduced-motion makes the transition instant

**Given** a row collapse
**When** the collapse completes
**Then** scroll position adjusts to keep the next row at natural attention position

**Given** a phone viewport (< 768px)
**When** inline action buttons would normally render on DataGrid rows
**Then** inline actions are hidden via CSS media query
**And** tapping a row expands it (expand-in-row pattern)
**And** action buttons appear inside the expanded detail view
**And** each row is at comfortable density (one row per item for scannability)

**References:** FR20, UX-DR17, UX-DR18, UX-DR62, UX-DR30 (prefers-reduced-motion)

---

### Story 4.6: Empty States, Field Descriptions & Unsupported Types

As a business user,
I want helpful empty states that guide me toward action, field descriptions that explain what columns mean, and clear indicators for any fields the framework can't auto-render,
So that I'm never confused by blank screens, cryptic column names, or missing data.

**Acceptance Criteria:**

**Given** a projection with zero total items
**When** the FcEmptyState renders
**Then** it displays: a large muted FluentIcon, primary message "[No {entity plural}] yet." with entity name humanized from projection type
**And** if the user has available commands, a CTA button "Send your first [Command Name]" appears
**And** if no commands are available (read-only projection), the message appears without CTA
**And** optional secondary text from resource files is shown if available
**And** accessibility: role="status", aria-label="No [entities] found", CTA keyboard-focusable

**Given** the empty filtered state (filters active, zero matches, but total items > 0)
**When** the state renders
**Then** it is visually distinct from FcEmptyState
**And** shows: "No orders match the current filters. [Reset filters] to see all 47 orders."

**Given** a developer adds [Description("...")] or [Display(Description="...")] to a projection property
**When** the DataGrid column header renders
**Then** the description surfaces as contextual help via tooltip on hover
**And** in detail/form views, the description appears as an inline label below the field

**Given** a projection with an unsupported field type (e.g., Dictionary<string, List<T>>)
**When** the auto-generated view renders
**Then** FcFieldPlaceholder renders: FluentCard with dashed border, FluentIcon (Warning), field name, type annotation, message "This field requires a custom renderer", and FluentAnchor link to customization gradient docs
**And** a build-time warning is emitted for each unsupported field
**And** the field is never silently omitted (zero silent omissions)
**And** accessibility: role="status", aria-label="[Field name] requires custom renderer", focusable in tab order

**Given** dev-mode overlay is active (Ctrl+Shift+D, Epic 6 scope)
**When** an unsupported field is present
**Then** it is highlighted with a red-dashed border with exact unsupported type name and recommended override level

**References:** FR9, FR10, FR11, UX-DR3, UX-DR4, UX-DR55 (auto-generation boundary protocol)

---

**Epic 4 Summary:**
- 6 stories covering all 7 FRs (FR4, FR5, FR9, FR10, FR11, FR12, FR20)
- Relevant NFRs woven into acceptance criteria (NFR4, NFR29-34, NFR87)
- Relevant UX-DRs addressed (UX-DR3-4, UX-DR7, UX-DR17-18, UX-DR24, UX-DR35, UX-DR40-42, UX-DR44-45, UX-DR55, UX-DR62-63)
- Stories are sequentially completable: 4.1 (role hints) -> 4.2 (badges) -> 4.3 (filtering) -> 4.4 (virtual scrolling) -> 4.5 (expand-in-row) -> 4.6 (empty states/descriptions)

---

## Epic 5: Reliable Real-Time Experience

Business user gets instant feedback on command outcomes, sees live data updates via SignalR, and experiences graceful recovery from network interruptions -- with batched reconnection sweeps, preserved form state, ETag-based caching, idempotent handling, domain-specific rejection messages, polling fallback, structured logging with distributed tracing, and a SignalR fault injection test harness. **Extends Epic 2's happy path for degraded/disconnected conditions.**

### Story 5.1: EventStore Service Abstractions

As a developer,
I want all event-store communication abstracted behind swappable service contracts,
So that the framework is decoupled from infrastructure providers and I can swap implementations without changing application code.

**Acceptance Criteria:**

**Given** the framework's EventStore communication layer
**When** the service contracts are inspected
**Then** ICommandDispatcher is defined with a method to dispatch commands returning a correlation ID
**And** IQueryExecutor is defined with methods to execute queries with ETag support (If-None-Match headers)
**And** IProjectionSubscription is defined with methods to subscribe/unsubscribe to projection groups and receive change nudges
**And** all three contracts are in the Contracts package (no infrastructure dependencies)

**Given** the service contracts
**When** the default implementations are inspected
**Then** ICommandDispatcher sends POST /api/v1/commands and expects 202 Accepted
**And** IQueryExecutor sends POST /api/v1/queries and expects 200 + ETag
**And** IProjectionSubscription connects to /projections-hub via SignalR
**And** all communication uses camelCase JSON wire format

**Given** a consumer project references the framework
**When** it registers EventStore services
**Then** services are registered via DI with swappable implementations
**And** the consumer can replace any contract implementation without modifying framework code

**Given** the DAPR infrastructure strategy
**When** infrastructure is accessed
**Then** DAPR is the abstraction layer (no custom wrapper on top of DAPR)
**And** all infrastructure (state, pubsub, secrets) goes through DAPR component bindings
**And** DAPR itself is a permitted direct dependency

**Given** wire format constraints
**When** messages are exchanged
**Then** no colons appear in ProjectionType, TenantId, or domain names (DAPR actor ID separator)
**And** max 10 If-None-Match headers per request
**And** max 1MB request body
**And** ULID message IDs are used for command idempotency

**References:** FR32, NFR74 (zero direct infrastructure coupling), Architecture EventStore Communication

---

### Story 5.2: HTTP Response Handling & ETag Caching

As a business user,
I want the framework to handle all server responses gracefully and cache data intelligently,
So that I see appropriate feedback for every situation and the application feels fast even with repeated queries.

**Acceptance Criteria:**

**Given** the framework receives an HTTP response
**When** the response status is evaluated
**Then** 200 OK: data renders normally with ETag stored
**And** 202 Accepted: command acknowledged, lifecycle transitions to Acknowledged
**And** 304 Not Modified: cached data is current, no re-render
**And** 400 Bad Request: validation errors surface inline on form fields
**And** 401 Unauthorized: redirect to authentication flow
**And** 403 Forbidden: "You don't have permission to [action]" FluentMessageBar (Warning)
**And** 404 Not Found: "This [entity] no longer exists" FluentMessageBar (Warning)
**And** 409 Conflict: domain-specific rejection with entity name and resolution
**And** 429 Too Many Requests: "Please wait before retrying" with retry-after indication

**Given** a successful query response with an ETag
**When** the result is cached
**Then** the cache entry is scoped to {tenantId}:{userId} with the projection snapshot
**And** cache entries are stored via IStorageService (LocalStorageService in WASM, InMemoryStorageService in Server)
**And** SetAsync is fire-and-forget (does not block render)
**And** LRU eviction applies when max entries is reached (configurable)
**And** cache key follows pattern: {tenantId}:{userId}:{featureName}:{discriminator}

**Given** the ETag cache
**When** a subsequent query is made for the same projection
**Then** the cached ETag is sent via If-None-Match header
**And** if 304 is returned, the cached data is used without network data transfer
**And** correctness comes from server queries (cache is opportunistic)

**Given** the application is closing
**When** the beforeunload event fires
**Then** FlushAsync is called via JS interop in App.razor
**And** pending cache writes are flushed to storage

**Given** zero PII requirements (NFR17)
**When** cache contents are inspected
**Then** only projection snapshots (business data from server, not user-entered data) are cached
**And** no PII is stored at the framework layer

**Given** ETag cache key security
**When** cache keys are constructed
**Then** all cache key components (tenantId, userId, featureName, discriminator) are framework-controlled values derived from JWT claims and compile-time projection type names
**And** no user-supplied input is used in cache key construction
**And** cache key manipulation by client-side code cannot access another user's cached data within the same tenant

**References:** FR33, FR34, NFR17-19, Architecture IStorageService Contract

---

### Story 5.3: SignalR Connection & Disconnection Handling

As a business user,
I want the application to detect when my connection drops and preserve my in-progress work,
So that network interruptions don't disrupt my workflow or lose my unsaved form data.

**Acceptance Criteria:**

**Given** the SignalR hub connection to /projections-hub
**When** the connection is established
**Then** the client subscribes to projection groups using pattern {projectionType}:{tenantId}
**And** the connection receives lightweight ProjectionChanged nudges (never full data payloads)
**And** on receiving a nudge, the client re-queries via REST with ETag for actual data

**Given** a stable SignalR connection
**When** HubConnectionState transitions to Disconnected
**Then** a warning-colored inline note displays immediately (NFR15)
**And** the note does not disrupt the user's in-flight workflow (no modal, no overlay)
**And** auto-reconnect begins with exponential backoff (NFR39)

**Given** a command is in the Syncing state (300ms-2s window)
**When** SignalR disconnects during this window
**Then** FcLifecycleWrapper escalates immediately to timeout message: "Connection lost -- unable to confirm sync status"
**And** the sync pulse does NOT continue indefinitely waiting for confirmation (UX-DR50)
**And** ETag polling fallback is activated

**Given** a business user has an in-progress command form
**When** the SignalR connection drops
**Then** all unsaved field values are preserved in component state
**And** on reconnection, the form state is restored without data loss
**And** the user can continue editing immediately after reconnection

**Given** the command lifecycle timeout
**When** 30 seconds elapse without confirmation (configurable via FrontComposerOptions.CommandTimeoutSeconds)
**Then** the lifecycle transitions to timeout state with a manual refresh option

**Given** a business user experiences their first connection loss during active work
**When** the disconnection indicator appears
**Then** the user understands: their in-progress work is safe (form state preserved), the system is attempting to reconnect, and no action is required from them
**And** the messaging tone is reassuring, not alarming ("Connection interrupted -- your work is saved. Reconnecting...")
**And** on reconnection, a brief confirmation restores confidence without requiring the user to verify data manually

**Implementation note:** This story covers 4 distinct features (hub connection, loss detection, lifecycle escalation, form preservation). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR22, FR24, UX-DR2 (Disconnected state), UX-DR50, NFR15, NFR39, NFR43

---

### Story 5.4: Reconnection, Reconciliation & Batched Updates

As a business user,
I want the application to seamlessly recover after a network interruption -- rejoining subscriptions, catching up on missed changes, and showing me what changed in one smooth sweep,
So that I trust the data I see is current without needing to manually refresh.

**Acceptance Criteria:**

**Given** SignalR reconnection succeeds
**When** HubConnectionState transitions to Reconnected
**Then** the client automatically rejoins all previously subscribed projection groups (NFR40)
**And** for each visible projection, an ETag-conditioned GET is issued to reconcile stale state

**Given** ETag-conditioned catch-up queries return changed projections
**When** stale rows are identified
**Then** all stale rows receive a single batched CSS animation sweep simultaneously (not per-row flashes, NFR41)
**And** the animation respects prefers-reduced-motion (instant update if enabled)

**Given** the batched reconciliation completes with changes found
**When** FcSyncIndicator processes the results
**Then** a "Reconnected -- data refreshed" FluentMessageBar (Info) auto-dismisses after 3 seconds (NFR42)
**And** the header reconnection indicator clears

**Given** the batched reconciliation completes with NO changes found
**When** FcSyncIndicator processes the results
**Then** the header indicator silently clears
**And** no toast or notification is shown

**Given** FcSyncIndicator in the shell header during disconnection
**When** the connection is lost
**Then** "Reconnecting..." text with subtle pulse displays in the header
**And** aria-live="polite" announces the header status
**And** role="status" is set on any toast notifications

**Given** a schema evolution mismatch is detected at startup or after reconnection
**When** projection types don't match expected schemas
**Then** a clear diagnostic is logged
**And** "This section is being updated" message shows to business users instead of empty/stale data
**And** all cached ETags for the affected projection type are invalidated

**Given** schema bidirectional compatibility requirements (NFR48-50)
**When** event schemas or projection types evolve within a major version
**Then** new code can deserialize events from prior minor versions (backward-compatible reads)
**And** old code tolerates unknown fields from newer versions (forward-compatible serialization)
**And** schema evolution tests cover bidirectional deserialization matrix for shipped minor versions

**Implementation note:** This story covers 5 distinct features (group rejoin, ETag catch-up, batched animation, toast notification, schema evolution). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR25, FR26, FR27, UX-DR5, UX-DR39 (Info toast 3s), UX-DR51, NFR40-42, NFR48-50

---

### Story 5.5: Command Idempotency & Optimistic Updates

As a business user,
I want commands that land during a disconnection to resolve correctly on reconnection, with optimistic badge updates that show me the expected state immediately,
So that I'm never confused by duplicate outcomes, stale badges, or missing status changes after network recovery.

**Acceptance Criteria:**

**Given** a command was submitted before disconnection
**When** the command lands on the backend during the disconnect
**Then** on reconnection, the correct terminal state (Confirmed or Rejected) is produced
**And** no user-visible duplication occurs (no double success notifications, no phantom state changes)
**And** deterministic duplicate detection via ULID message ID prevents replay issues

**Given** a command is submitted (stable or reconnected)
**When** the optimistic update pattern activates
**Then** FcDesaturatedBadge transitions the status badge immediately to the target state with desaturated color (filter: saturate(0.5))
**And** on SignalR Confirmed: 200ms CSS transition restores full saturation
**And** on Rejected: badge reverts to the pre-optimistic confirmed state
**And** on IdempotentConfirmed: badge skips revert animation and saturates directly
**And** aria-label includes state during Syncing (e.g., "[Status] (confirming)")
**And** text label is always present (color never sole signal)
**And** prefers-reduced-motion makes the transition instantaneous

**Given** a new entity is created via command and does not match current filter criteria
**When** the creation is confirmed
**Then** FcNewItemIndicator renders the new row at the top of the DataGrid with subtle highlight (Fluent info-background at 10% opacity)
**And** text "New -- may not match current filters" displays
**And** auto-dismisses after 10 seconds or on next filter change with 300ms fade-out
**And** aria-live="polite" on the row, aria-describedby for indicator text

**Given** a domain-specific command rejection during degraded network conditions
**When** the rejection reaches the client on reconnection
**Then** the rejection message format applies: "[What failed]: [Why]. [What happened to the data]."
**And** form input is preserved (never cleared on error)
**And** FluentMessageBar (Danger) with no auto-dismiss

**Given** SignalR is completely unavailable
**When** the polling fallback activates
**Then** ETag-gated polling queries at configured intervals maintain projection correctness
**And** the polling preserves the same user-visible behavior as SignalR (updates appear, lifecycle resolves)
**And** the polling fallback is transparent to the business user

**Given** a business user returns from a disconnection where commands were in flight
**When** the reconciliation completes
**Then** the user can see a clear summary of what happened to each pending command (confirmed, rejected, or idempotent)
**And** no manual verification or page refresh is needed to confirm the final state
**And** the experience feels like "the system handled it" rather than "something went wrong"

**Implementation note:** This story covers 5 distinct features (idempotent outcomes, optimistic updates, new item indicator, polling fallback, rejection during disconnect). It may be split into sub-stories during implementation if scope proves too large for a single dev agent session.

**References:** FR28, FR29, FR31, UX-DR6, UX-DR8, UX-DR46, UX-DR47, NFR44-47

---

### Story 5.6: Build-Time Infrastructure Enforcement & Observability

As a developer,
I want build-time guarantees that no framework code directly couples to infrastructure providers, and structured logging across the full lifecycle,
So that the framework remains portable across deployment targets and I can trace any operation end-to-end.

**Acceptance Criteria:**

**Given** the framework assemblies
**When** a CI build runs
**Then** an automated check asserts zero direct references to Redis, Kafka, Postgres, CosmosDB, or DAPR SDK types from framework assemblies
**And** all infrastructure access routes through DAPR component bindings
**And** violations fail the build with a descriptive error message

**Given** the framework's runtime services
**When** structured logging is inspected
**Then** OpenTelemetry semantic conventions are followed
**And** every log entry includes: CommandType or ProjectionType, TenantId, CorrelationId
**And** message template + parameters are used (never string interpolation)
**And** log levels follow: Debug (dev), Information (flow), Warning (degraded), Error (intervention)

**Given** the framework's distributed tracing
**When** FrontComposerActivitySource is used
**Then** it is a shared static ActivitySource name across all framework packages
**And** traces span: user click -> backend command -> projection update -> SignalR nudge -> UI update
**And** tracing is compatible with Grafana, Jaeger, and Application Insights

**Given** the framework runs on different deployment targets
**When** it is deployed to on-premise (bare Aspire), sovereign cloud (Kubernetes), Azure Container Apps, AWS ECS/EKS, or GCP Cloud Run
**Then** behavior is identical across all targets (NFR73)
**And** CI validates Aspire, local Kubernetes, and Azure Container Apps configurations

**References:** FR48, FR72, NFR73, NFR74, NFR79, NFR80

---

### Story 5.7: SignalR Fault Injection Test Harness

As a developer,
I want a test harness that simulates SignalR connection faults without requiring a live server,
So that I can write reliable unit and integration tests for all reconnection and resilience behaviors.

**Acceptance Criteria:**

**Given** the SignalR fault injection test harness
**When** a test configures a fault scenario
**Then** the following fault types are supported: connection drop, connection delay, partial message delivery, and message reorder
**And** faults can be triggered programmatically at precise points in the command lifecycle

**Given** a test using the fault harness
**When** a connection drop is simulated during a command's Syncing state
**Then** the test can assert: FcLifecycleWrapper escalates to timeout, form state is preserved, and reconnection triggers catch-up query

**Given** a test using the fault harness
**When** a message reorder is simulated (ProjectionChanged arrives before command acknowledgment)
**Then** the test can assert: the lifecycle state machine handles the out-of-order events correctly without invalid state transitions

**Given** the fault harness
**When** used alongside the existing test infrastructure
**Then** it integrates with xUnit + bUnit test patterns
**And** it does not require a live SignalR server, live EventStore, or live network
**And** 90% of reconnection behaviors (FR24-29) are testable at unit level via the harness (NFR59)

**Given** the fault harness as a test utility
**When** an adopter references it
**Then** it is available as part of the framework's test host package (FR71, Epic 10)
**And** test data follows the Builder pattern for domain models

**References:** FR82, NFR53, NFR59, Architecture testing infrastructure

---

**Epic 5 Summary:**
- 7 stories covering all 14 FRs (FR22, FR24-29, FR31-34, FR48, FR72, FR82)
- Relevant NFRs woven into acceptance criteria (NFR15-19, NFR39-47, NFR53, NFR59, NFR73-74, NFR79-80)
- Relevant UX-DRs addressed (UX-DR2, UX-DR5-6, UX-DR8, UX-DR39, UX-DR46-47, UX-DR50-51)
- Layered story ordering: 5.1-5.2 (basic communication) -> 5.3 (connectivity) -> 5.4 (reconciliation) -> 5.5 (resilience) -> 5.6 (enforcement/observability) -> 5.7 (test harness)
- Each story explicitly states which degraded condition it handles

---

## Epic 6: Developer Customization Gradient

Developer can customize generated UI at four levels -- annotation overrides, typed Razor templates, slot-level field replacement, and full component replacement -- with hot reload, build-time contract validation, error boundaries, and actionable error messages with diagnostic IDs. **Depends on Epics 1-4** (customization targets the generated views, command forms, DataGrid features, and shell components built in those epics).

### Story 6.1: Level 1 - Annotation Overrides

As a developer,
I want to override field rendering via declarative attributes without writing custom components,
So that I can adjust labels, formatting, column priority, and display hints with a one-line attribute change and immediate hot reload feedback.

**Acceptance Criteria:**

**Given** a projection property with [Display(Name = "Order Date")]
**When** the generated DataGrid renders
**Then** the column header uses "Order Date" instead of the humanized property name
**And** [Display(Name)] takes precedence over ALL auto-formatting rules including enum humanization

**Given** a projection property with [ColumnPriority(1)]
**When** FcColumnPrioritizer activates for projections with >15 fields
**Then** fields with lower priority numbers appear first in the visible column set

**Given** a DateTime property with [RelativeTime]
**When** the DataGrid cell renders
**Then** the value shows relative time (e.g., "3 hours ago") using fixed-width abbreviations
**And** switches to absolute after 7 days
**And** the default (without annotation) remains absolute date format

**Given** a decimal property with [Currency]
**When** the DataGrid cell renders
**Then** the value is locale-formatted with currency symbol, right-aligned

**Given** an annotation override is applied
**When** the developer saves the file with hot reload active
**Then** the change reflects in the running application without restart
**And** customization time is <= 5 minutes from reading docs to seeing the override (NFR84)

**Given** the annotation-level customization
**When** it is evaluated as a customization gradient level
**Then** it is compile-time only (attributes processed by the source generator)
**And** no runtime registration or custom component code is required

**Given** the sample domain (Counter or Task Tracker)
**When** Level 1 annotation overrides are demonstrated
**Then** at least one override is applied to the sample domain as a reference implementation (e.g., [Display(Name="...")] on a projection property)

**References:** FR39, UX-DR54 (Level 1), NFR84

---

### Story 6.2: Level 2 - Typed Razor Template Overrides

As a developer,
I want to override component rendering via typed Razor templates bound to domain model contracts,
So that I can rearrange section layouts and field groupings without replacing the entire component.

**Acceptance Criteria:**

**Given** a developer creates a Razor template for a projection view
**When** the template is bound to the domain model
**Then** a typed Context parameter provides access to all projection fields and metadata
**And** the template controls section-level layout (field grouping, ordering, visual hierarchy)
**And** individual field rendering still uses the framework's auto-generation within the template sections

**Given** a Level 2 template override
**When** it is registered
**Then** it is a compile-time artifact (processed by the source generator)
**And** the framework detects the template and uses it instead of the default layout

**Given** a Level 2 template
**When** the framework version changes
**Then** build-time contract validation checks the template's expected contract against the installed framework version (FR43)
**And** a warning is emitted if the contract doesn't match: "Template expects FrontComposer v{expected}, installed v{actual}. See HFC{id}."

**Given** FcStarterTemplateGenerator (Story 6.5)
**When** the developer requests a Level 2 starter template from the dev-mode overlay
**Then** the generated Razor source includes: typed Context parameter, exact Fluent UI components/parameters used by the default layout, and comments indicating the contract type
**And** the developer can paste and modify the template as a starting point

**Given** a Level 2 template override with hot reload active
**When** the developer modifies the template
**Then** the change reflects without application restart (FR44)

**Given** the sample domain
**When** Level 2 template overrides are demonstrated
**Then** at least one template override is applied to the sample domain rearranging section layout as a reference implementation

**References:** FR40, FR43 (partial), FR44 (partial), UX-DR54 (Level 2)

---

### Story 6.3: Level 3 - Slot-Level Field Replacement

As a developer,
I want to replace a single field's renderer with a custom component while all other fields remain auto-generated,
So that I can customize one problematic field without rewriting the entire view.

**Acceptance Criteria:**

**Given** a developer wants to override a specific field
**When** they register a slot override
**Then** IOverrideRegistry.AddSlotOverride is called with a refactor-safe lambda expression identifying the field (e.g., `registry.AddSlotOverride<OrderProjection>(o => o.Priority, typeof(CustomPriorityRenderer))`)
**And** the lambda expression ensures rename-safe field identification (compile error on field rename)

**Given** a slot override is registered for a field
**When** the generated view renders
**Then** the custom component renders for the overridden field
**And** all other fields render via the framework's auto-generation
**And** the custom component receives a typed FieldSlotContext<T> with: field value, field metadata, parent entity reference, and render context (density, theme, read-only state)

**Given** a slot-level override registration
**When** it executes
**Then** it is a runtime registration (not compile-time)
**And** DI never registers per-type renderers; single ProjectionRenderer<T> resolves via IOverrideRegistry for customs

**Given** a Level 3 slot override with hot reload active
**When** the developer modifies the custom component
**Then** the change reflects without application restart (FR44)

**Given** FcStarterTemplateGenerator
**When** the developer requests a Level 3 starter template
**Then** the generated source includes: typed FieldSlotContext<T> parameter, the current field's Fluent UI component and parameters, and the exact contract type

**Given** the sample domain
**When** Level 3 slot overrides are demonstrated
**Then** at least one slot override replaces a field renderer in the sample domain as a reference implementation

**References:** FR41, FR44 (partial), UX-DR54 (Level 3)

---

### Story 6.4: Level 4 - Full Component Replacement

As a developer,
I want to replace a generated component entirely with a custom implementation while preserving the framework's lifecycle wrapper, accessibility contract, and shell integration,
So that I have complete control over rendering for complex views without losing framework benefits.

**Acceptance Criteria:**

**Given** a developer wants to fully replace a generated view
**When** they register a view override
**Then** IOverrideRegistry.AddViewOverride is called with the projection type and custom component type
**And** the registration is runtime (not compile-time)

**Given** a full replacement component
**When** it renders
**Then** the framework's lifecycle wrapper (FcLifecycleWrapper) still wraps the component
**And** shell integration (navigation, breadcrumbs, density, theme) is preserved
**And** the custom component receives the full domain model context, render context, and lifecycle state

**Given** a full replacement component
**When** the custom component accessibility contract is evaluated
**Then** the 6 requirements are enforced:
**And** (1) expose accessible name via aria-label or visible text (build-time warning if missing)
**And** (2) preserve keyboard reachability in DOM order
**And** (3) preserve focus visibility (no overriding --colorStrokeFocus2)
**And** (4) announce state changes using same aria-live politeness categories
**And** (5) respect prefers-reduced-motion
**And** (6) support forced-colors mode with system color keywords

**Given** a Level 4 override with hot reload active
**When** the developer modifies the custom component
**Then** the change reflects without application restart (FR44)

**Given** FcStarterTemplateGenerator
**When** the developer requests a Level 4 starter template
**Then** the generated source includes: complete view structure with lifecycle wrapper integration, accessibility contract hooks, typed parameters, and comments for all Fluent UI components used in the default view

**Given** the customization gradient hierarchy
**When** all four levels are inspected
**Then** each level inherits capabilities from the level above (Level 2 includes Level 1 attributes; Level 3 includes Levels 1-2; Level 4 includes Levels 1-3)

**Given** the sample domain
**When** Level 4 full replacement is demonstrated
**Then** at least one full view replacement is applied to the sample domain as a reference implementation preserving lifecycle wrapper and accessibility contract

**References:** FR42, FR44 (partial), UX-DR31, UX-DR54 (Level 4)

---

### Story 6.5: FcDevModeOverlay & Starter Template Generator

As a developer,
I want an interactive diagnostic overlay that shows me what conventions are applied to each generated element and generates starter code for customization,
So that I can discover how to customize any part of the UI without reading documentation first.

**Acceptance Criteria:**

**Given** the application is running in debug mode (#if DEBUG)
**When** the developer presses Ctrl+Shift+D or clicks the dev-mode header icon
**Then** the FcDevModeOverlay activates showing dotted outlines around each auto-generated element
**And** info badges display the convention name on each element

**Given** the dev-mode overlay is active
**When** the developer clicks on an annotated element
**Then** a 360px FluentDrawer detail panel opens showing:
**And** convention name and description
**And** contract type (full type name)
**And** current customization level (default or overridden)
**And** recommended override level for common customization goals
**And** "Copy starter template" button (for Levels 2-4)
**And** before/after toggle for active overrides

**Given** the developer clicks "Copy starter template" for a Level 2 override
**When** FcStarterTemplateGenerator processes the request
**Then** it walks the auto-generation engine's in-memory component tree via IRazorEmitter service
**And** emits Razor source with: typed Context parameter, exact Fluent UI components, typed parameters, and contract type comments
**And** the source is copied to clipboard via JS interop

**Given** unsupported fields in the current view
**When** the dev-mode overlay is active
**Then** unsupported fields are highlighted with a red-dashed border
**And** the detail panel shows the exact unsupported type name and recommended override level

**Given** the dev-mode overlay
**When** keyboard navigation is used
**Then** annotations are keyboard-navigable (tab order)
**And** the detail panel has role="complementary"
**And** Escape closes the panel
**And** screen reader announces the convention name on focus

**Given** production builds
**When** the application compiles without DEBUG
**Then** FcDevModeOverlay is completely excluded (zero production footprint)
**And** FcStarterTemplateGenerator is registered only in development mode

**References:** FR39-42 (discovery path), UX-DR9, UX-DR11, UX-DR54

---

### Story 6.6: Build-Time Validation, Error Boundaries & Diagnostics

As a developer,
I want the framework to catch customization errors at build time, isolate rendering failures at runtime, and give me actionable error messages with documentation links,
So that a broken override never crashes the shell and I can fix problems without asking for help.

**Acceptance Criteria:**

**Given** a customization override (any level)
**When** the framework version changes between minor versions
**Then** build-time validation checks the override's expected contract against the installed framework version (FR43)
**And** a warning is emitted: "Override expects FrontComposer v{expected}, installed v{actual}. See HFC{id}."

**Given** a custom override component with accessibility issues
**When** the build runs
**Then** Roslyn analyzers check the custom component against the 6-requirement accessibility contract (UX-DR31)
**And** missing accessible names produce build-time warnings with WCAG citation + user scenario
**And** with TreatWarningsAsErrors=true for accessibility warnings, builds with inaccessible overrides are blocked

**Given** the build-time accent contrast check
**When** a custom accent color is configured
**Then** a Roslyn analyzer computes contrast ratio against both Light and Dark neutral backgrounds
**And** if either ratio fails WCAG AA (4.5:1 normal text, 3:1 large text/UI components), a build warning is emitted
**And** with TreatWarningsAsErrors=true, inaccessible accent colors block the build

**Given** a customization override throws a rendering exception at runtime
**When** the error boundary activates
**Then** the failure is isolated to the affected component only (FR47)
**And** a diagnostic panel renders in place of the faulty component with: error description, diagnostic ID (HFC2001), and a link to the documentation page
**And** the rest of the composition shell continues to function normally
**And** the error is logged with full context (component type, override level, exception details)

**Given** any customization or generation error
**When** the error message is produced
**Then** it includes: what was expected, what was found, how to fix it, and a diagnostic ID linking to a documentation page (FR45)
**And** the diagnostic ID follows the reserved range for the package (HFC0001-0999 Contracts, HFC1000-1999 SourceTools, HFC2000-2999 Shell)

**Given** hot reload is active for all four customization levels
**When** any override is modified and saved
**Then** the change reflects in the running application without restart (FR44)
**And** hot reload limitations (generic type changes, new attribute additions) trigger a build-time message: "Full restart required for this change type"

**References:** FR43, FR44, FR45, FR47, UX-DR31, UX-DR64, NFR36, NFR80, NFR86

---

**Epic 6 Summary:**
- 6 stories covering all 8 FRs (FR39-45, FR47)
- Relevant NFRs woven into acceptance criteria (NFR36, NFR80, NFR84, NFR86)
- Relevant UX-DRs addressed (UX-DR9, UX-DR11, UX-DR31, UX-DR54, UX-DR64)
- Stories are sequentially completable: 6.1 (annotation) -> 6.2 (template) -> 6.3 (slot) -> 6.4 (full replacement) -> 6.5 (dev-mode overlay) -> 6.6 (validation/errors)
- Each level inherits capabilities from previous levels

---

## Epic 7: Authentication, Authorization & Multi-Tenancy

Users authenticate via OIDC/SAML (Keycloak, Entra ID, GitHub, Google), commands are authorized via declarative policy attributes, and tenant context from JWT is propagated and enforced across all command, query, and subscription operations.

### Story 7.1: OIDC/SAML Authentication Integration

As a developer,
I want to integrate standard identity providers via OIDC/SAML without building a custom authentication UI,
So that users can authenticate with their existing corporate or social identity and I don't maintain auth UI code.

**Acceptance Criteria:**

**Given** the framework's authentication configuration
**When** an identity provider is configured
**Then** standard OIDC/SAML flows are used for authentication
**And** the following identity providers are supported: Keycloak, Microsoft Entra ID, GitHub, and Google
**And** no custom authentication UI is shipped by the framework (all auth UI comes from the identity provider)

**Given** a user navigates to the application unauthenticated
**When** the authentication flow triggers
**Then** the user is redirected to the configured identity provider's login page
**And** on successful authentication, the user is redirected back with a JWT bearer token
**And** the JWT is stored and propagated through all subsequent requests

**Given** a JWT bearer token
**When** the token is inspected by the framework
**Then** TenantId and UserId claims are extracted
**And** the token is validated (signature, expiry, audience)
**And** failed validation redirects to re-authentication

**Given** v1 scope
**When** authentication is configured
**Then** a single identity provider is configured per deployment
**And** multi-IdP support (simultaneous Keycloak + Entra ID) is documented as a v1.x enhancement

**Given** zero PII requirements (NFR102)
**When** the framework processes JWT tokens
**Then** only TenantId and UserId claims are extracted for framework operations
**And** no PII from the token is stored, logged, or cached at the framework layer

**Given** JWT token storage requirements
**When** the authentication state is managed
**Then** the standard ASP.NET Core authentication state provider pattern is used (not raw LocalStorage)
**And** Blazor Server uses server-side circuit state (no client-side token storage)
**And** Blazor WebAssembly uses the framework's secure token handling with HttpOnly cookie preference where possible
**And** raw JWT tokens are never stored in LocalStorage (XSS mitigation)

**References:** FR37, NFR20, NFR21, NFR102

---

### Story 7.2: Tenant Context Propagation & Isolation

As a business user,
I want my data to be completely isolated from other tenants with no possibility of cross-tenant data leakage,
So that I can trust the application with my organization's data.

**Acceptance Criteria:**

**Given** an authenticated user with a JWT containing TenantId claim
**When** the user performs any operation
**Then** TenantId is propagated through all command dispatch operations (included in command envelope)
**And** TenantId is propagated through all query execution operations (included in query parameters)
**And** TenantId is included in SignalR group subscriptions ({projectionType}:{tenantId})
**And** TenantId scopes all ETag cache keys ({tenantId}:{userId}:{featureName}:{discriminator})
**And** TenantId scopes MCP tool enumeration (agents see only tools for their tenant)

**Given** the tenant isolation enforcement
**When** a request attempts to access data from a different tenant
**Then** the framework layer blocks the request before it reaches the backend
**And** cross-tenant data visibility is treated as a security bug (NFR28)
**And** the violation is logged at Error level with TenantId, attempted TenantId, and CorrelationId

**Given** DAPR actor key patterns
**When** actor IDs are constructed
**Then** the pattern {projectionType}:{tenantId} is used
**And** no colons appear in ProjectionType or TenantId values (DAPR actor ID separator constraint)

**Given** the IStorageService cache
**When** cache entries are scoped
**Then** all entries include {tenantId}:{userId} prefix
**And** cache lookup never returns entries from a different tenant, even if UserId matches

**Given** v0.1 scope (single-tenant)
**When** multi-tenancy is not yet configured
**Then** a stub TenantProvider returns a fixed default tenant ID
**And** all framework operations function correctly with the stub
**And** the stub is replaceable with the real JWT-based provider without code changes (only configuration)

**References:** FR35, NFR21, NFR22, NFR28, Architecture Multi-Tenancy

---

### Story 7.3: Command Authorization Policies

As a developer,
I want to apply authorization policies to commands via declarative attributes that integrate with ASP.NET Core,
So that I can enforce role-based and policy-based access control on domain operations using the standard .NET authorization model.

**Acceptance Criteria:**

**Given** a command annotated with [RequiresPolicy("OrderApprover")]
**When** a user without the "OrderApprover" policy submits the command
**Then** the framework rejects the command before dispatch to the backend
**And** a 403 Forbidden response is returned
**And** the UX renders: "You don't have permission to [command action]" via FluentMessageBar (Warning)

**Given** a command annotated with [RequiresPolicy]
**When** the authorization check executes
**Then** it integrates with ASP.NET Core authorization middleware
**And** standard IAuthorizationService is used for policy evaluation
**And** claims from the JWT bearer token are available for policy evaluation

**Given** a command with a [RequiresPolicy] attribute referencing a policy name
**When** the referenced policy is not registered in the authorization configuration
**Then** a build-time warning is emitted: "Policy '{policyName}' referenced by [RequiresPolicy] on {CommandType} is not registered. See HFC{id}."
**And** the warning includes a documentation link for policy registration

**Given** commands without [RequiresPolicy] attributes
**When** they are submitted
**Then** no authorization check is performed beyond basic authentication (user must be authenticated)
**And** this is the default behavior for commands that don't need role/policy restrictions

**Given** the authorization layer
**When** inline action buttons render on DataGrid rows for policy-protected commands
**Then** buttons for commands the user is not authorized to execute are hidden or disabled
**And** the authorization check uses the same policy evaluation as the dispatch check (no divergence between UI and backend)

**References:** FR46, NFR23, Architecture Security

---

**Epic 7 Summary:**
- 3 stories covering all 3 FRs (FR35, FR37, FR46)
- Relevant NFRs woven into acceptance criteria (NFR20-23, NFR28, NFR102)
- v0.1 single-tenant stub included for backward compatibility
- Stories are sequentially completable: 7.1 (authentication) -> 7.2 (tenant propagation) -> 7.3 (authorization policies)

---

## Epic 8: MCP & Agent Integration

LLM agents can issue commands and read projections via typed MCP tools with hallucination rejection, tenant-scoped enumeration, two-call lifecycle pattern, Markdown-rendered projections, and shared typed NuGet contracts. **v1.x-deferrable within this epic:** versioned skill corpus (FR55), build-time LLM code generation (FR58), schema hash fingerprints (FR59), migration delta diagnostics (FR60), and rendering abstraction contract (FR61).

### Story 8.1: MCP Server & Typed Tool Exposure

As a developer,
I want my domain model automatically exposed as typed MCP tools via an in-process server alongside the composition shell,
So that LLM agents can discover and call my commands and queries with the same type safety as the web surface.

**Acceptance Criteria:**

**Given** a FrontComposer-registered domain with [Command] and [Projection] annotated types
**When** the MCP server starts alongside the composition shell
**Then** an in-process Model Context Protocol server is hosted and accessible to LLM agents
**And** each [Command] is exposed as a typed MCP tool
**And** each [Projection] is exposed as a typed MCP resource

**Given** a [Command] exposed as an MCP tool
**When** the tool schema is inspected
**Then** typed parameters are emitted with validation constraints derived from FluentValidation rules (FR50)
**And** parameter types, required/optional status, and constraints match the domain model exactly
**And** schema divergence between web form and MCP tool is prevented by single-source generation

**Given** MCP tool descriptions
**When** they are generated
**Then** tool descriptions are auto-generated from the same source as web form labels (FR56)
**And** the label resolution chain applies: [Display(Name)] > humanized CamelCase > raw name
**And** typed NuGet contracts are shared between backend, web surface, and MCP surface from a single source

**Given** the MCP server configuration
**When** the developer inspects the setup
**Then** the MCP server is registered via DI with the domain registration
**And** no additional configuration is required beyond the existing domain registration ceremony

**Given** the MCP server is running with a registered domain
**When** integration tests execute
**Then** at least 3 commands can be invoked via MCP tool calls and produce expected lifecycle outcomes (Acknowledged -> Confirmed)
**And** at least 2 projections can be read via MCP resources and return correctly formatted Markdown
**And** an unknown tool call is rejected with a suggestion response

**Given** an LLM agent connecting to the MCP server
**When** the agent authenticates
**Then** client credentials or API key authentication is supported for machine-to-machine MCP access
**And** the authenticated agent receives a JWT with TenantId claim scoped to its authorized tenant
**And** the authentication flow is distinct from the web OIDC redirect flow (no browser required)

**References:** FR49, FR50, FR56, NFR91 (tool-call correctness >= 95%)

---

### Story 8.2: Hallucination Rejection & Tenant-Scoped Tools

As an LLM agent,
I want unknown or malformed tool calls rejected immediately with a helpful suggestion,
So that hallucinated tool names never reach the backend and I can self-correct with the correct tool list.

**Acceptance Criteria:**

**Given** an MCP tool call with an unknown tool name
**When** the call reaches the contract boundary
**Then** it is rejected before reaching the backend
**And** the response includes: a suggestion with the closest matching correct tool name, and the full tenant-scoped tool list
**And** the rejection response time is P95 < 100ms (NFR7)

**Given** an MCP tool call with invalid parameters (type mismatch, missing required field, constraint violation)
**When** schema validation runs against the source-generator-emitted tool manifest
**Then** the call is rejected with a specific validation error describing which parameter failed and why
**And** the command never reaches the backend

**Given** an authenticated agent with a JWT containing TenantId
**When** the agent enumerates available MCP tools
**Then** only tools scoped to the agent's active tenant are visible (FR54)
**And** tools from other tenants are completely invisible (not rejected, not listed)
**And** cross-tenant tool visibility is treated as a security bug (NFR28)

**Given** tenant-scoped tool enumeration
**When** the agent's authorization scope is evaluated
**Then** [RequiresPolicy]-annotated commands are only listed if the agent's JWT satisfies the policy
**And** unauthorized tools are excluded from the enumeration (not shown as "forbidden")

**Given** a rejected MCP tool call with a suggestion response
**When** the suggestion includes the tenant-scoped tool list
**Then** tool names in the suggestion are domain-generic (e.g., "CreateOrder" not "AcmeCorp_CreateOrder")
**And** no tenant-identifiable information is leaked through error responses or tool listings
**And** tool descriptions do not contain tenant-specific data

**References:** FR51, FR54, NFR7, NFR27, NFR28

---

### Story 8.3: Two-Call Lifecycle & Agent Command Semantics

As an LLM agent,
I want to issue commands with the same lifecycle semantics as the web surface, using a two-call pattern that gives me an acknowledgment and a way to track state transitions,
So that I can reliably submit commands and wait for confirmed outcomes without polling blindly.

**Acceptance Criteria:**

**Given** an agent issues a command via MCP tool call
**When** the command is dispatched
**Then** the first call returns an acknowledgment with: correlation ID (ULID), and a subscription URI for lifecycle tracking
**And** the acknowledgment corresponds to the Acknowledged lifecycle state

**Given** the agent has a subscription URI from the first call
**When** the agent calls the lifecycle/subscribe tool with the correlation ID
**Then** state transitions are exposed: Syncing -> Confirmed (or Rejected)
**And** the lifecycle tool guarantees a terminal state is always reached (Confirmed or Rejected)
**And** no silent failures: every command produces exactly one terminal outcome

**Given** a command is rejected
**When** the agent reads the rejection
**Then** the rejection message follows the same format as the web surface: "[What failed]: [Why]. [What happened to the data]."
**And** the agent can parse the structured rejection to decide on retry or alternative action

**Given** an idempotent command outcome (rejected but intent fulfilled)
**When** the agent reads the outcome
**Then** the message acknowledges success: "This [entity] was already [action] (by another user). No action needed."
**And** the lifecycle state is IdempotentConfirmed (distinct from Rejected)

**Given** end-to-end agent command round-trip
**When** performance is measured
**Then** command-to-projection read-your-writes P95 < 1500ms on localhost Aspire topology (NFR6)

**Given** the agent surface lifecycle
**When** compared to the web surface lifecycle
**Then** the same five states apply (Idle, Submitting, Acknowledged, Syncing, Confirmed/Rejected)
**And** the same ULID-based idempotency applies
**And** the same domain-specific rejection messages are returned

**Given** a command rejection received by an agent
**When** the rejection response is inspected
**Then** it includes structured data: error code, entity ID, human-readable message, suggested action (retry/abort/alternative), and whether retry is appropriate
**And** the structured schema is documented in the MCP tool manifest
**And** agents can programmatically parse rejections without relying on string matching

**References:** FR52, FR57, NFR6, NFR44-47

---

### Story 8.4: Projection Rendering for Agents

As an LLM agent,
I want to read projection data rendered as structured Markdown consumable through chat surfaces,
So that I can present domain data to users in a readable format without parsing raw JSON.

**Acceptance Criteria:**

**Given** a projection with Default or ActionQueue role hint
**When** the agent reads the projection via MCP resource
**Then** the projection renders as a Markdown table with: column headers from the label resolution chain, formatted cell values following the same data formatting rules as the web surface (locale numbers, date formats, em dash for nulls, humanized enums)

**Given** a projection with StatusOverview role hint
**When** the agent reads the projection
**Then** the projection renders as a Markdown status card with: aggregate counts per badge slot, status labels, and totals

**Given** a projection with Timeline role hint
**When** the agent reads the projection
**Then** the projection renders as a Markdown timeline with: chronological entries, timestamps, status badges as text labels, and event descriptions

**Given** any projection rendering for agents
**When** badge states are included
**Then** badge labels are text-only (no color codes) following the 6-slot semantic mapping (Neutral, Info, Success, Warning, Danger, Accent)
**And** the rendering is consumable by LLM agents through chat surfaces without special formatting requirements

**Given** a projection with zero items
**When** the agent reads the projection
**Then** a meaningful empty state message is returned: "No [entities] found." with available command suggestions if applicable

**References:** FR53, UX-DR35 (data formatting parity), UX-DR44 (contextual subtitles)

---

### Story 8.5: Skill Corpus & Build-Time Agent Support (v1.x-deferrable)

As a developer,
I want a versioned skill corpus that teaches LLM agents how to write FrontComposer domain code, and a benchmark that validates agent code generation quality,
So that AI-assisted development produces compilable, correct microservices on the first attempt.

**Acceptance Criteria:**

**Given** the skill corpus
**When** it is published
**Then** it contains: attribute references, domain-modeling conventions, code generation patterns, and example microservice structures
**And** it is available as both a NuGet package and MCP-discoverable resources at runtime
**And** it is consumable by LLM agents and human developers alike

**Given** an LLM agent with access to the skill corpus
**When** it generates a new microservice from a prompt
**Then** the generated code compiles successfully against the framework
**And** framework-emitted typed partial types guide the agent into a compiler-checked shape
**And** the structural validator confirms the output matches expected patterns

**Given** the skill corpus version
**When** the framework version changes
**Then** a migration guide is required for any change that would break a shipped skill corpus example (FR69)
**And** the corpus is tested against a pinned model version with a structural validator

**Given** the LLM code-generation benchmark (FR73, Epic 10)
**When** the benchmark runs against the skill corpus
**Then** the one-shot generation rate target is >= 80% (NFR85)
**And** the benchmark uses 20 prompts at v1 scope

**References:** FR55, FR58, NFR85, FR69

---

### Story 8.6: Schema Versioning & Multi-Surface Abstraction (v1.x-deferrable)

As a developer,
I want schema hash fingerprints and a rendering abstraction that enable graceful version negotiation across deployments,
So that framework version mismatches between client and server degrade gracefully instead of breaking silently.

**Acceptance Criteria:**

**Given** a projection or MCP tool manifest
**When** the source generator processes it
**Then** a schema hash fingerprint is emitted for the projection and for the MCP tool manifest
**And** the fingerprint enables client/server version comparison

**Given** a deployment where client framework version differs from server framework version
**When** schema hash fingerprints are compared
**Then** matching fingerprints proceed normally
**And** mismatched fingerprints trigger graceful version negotiation (not a crash)

**Given** a schema hash fingerprint mismatch
**When** the migration delta tool runs
**Then** a breaking-change diagnostic is produced describing: what changed, what the impact is, and a remediation path
**And** the diagnostic provides actionable steps, not just detection

**Given** the rendering abstraction contract
**When** it is defined
**Then** it decouples composition logic from surface-specific renderers
**And** a single domain source can drive rendering across web, agent, and future surfaces
**And** v1 ships only the web surface through it, but the contract supports future surfaces without redesign

**References:** FR59, FR60, FR61, NFR48-50

---

**Epic 8 Summary:**
- 6 stories covering all 13 FRs (FR49-61)
- v1 core: Stories 8.1-8.4 (8 FRs: FR49-54, FR56-57)
- v1.x-deferrable: Stories 8.5-8.6 (5 FRs: FR55, FR58-61)
- Relevant NFRs woven into acceptance criteria (NFR6-7, NFR27-28, NFR44-47, NFR85, NFR91)
- Stories are sequentially completable: 8.1 (MCP server) -> 8.2 (hallucination rejection) -> 8.3 (lifecycle) -> 8.4 (projections) -> 8.5 (skill corpus, deferrable) -> 8.6 (schema versioning, deferrable)

---

## Epic 9: Developer Tooling & Documentation

Developer has CLI tools (inspect generator output, migration), build-time drift detection, IDE parity (VS/Rider/VS Code), diagnostic ID ranges with doc links, deprecation with migration paths, and Diataxis-genre documentation site. Built incrementally alongside earlier epics.

### Story 9.1: Build-Time Drift Detection

As a developer,
I want the framework to detect mismatches between my backend domain declarations and the generated UI at build time,
So that I catch breaking changes as compile-time errors instead of discovering them as silent runtime bugs.

**Acceptance Criteria:**

**Given** a [Projection]-annotated type in the domain assembly
**When** the source generator compares the current type shape against the previously generated output
**Then** any drift is surfaced as a compile-time diagnostic (not runtime silent behavior)
**And** the diagnostic identifies: which type changed, what property was added/removed/modified, and the impact on the generated UI

**Given** a domain property is renamed
**When** the build runs
**Then** a diagnostic is emitted: "Property '{OldName}' was expected on {TypeName} but not found. '{NewName}' was added. If this is a rename, update the generated output. See HFC{id}."
**And** the diagnostic includes a documentation link with resolution steps

**Given** a domain property type changes (e.g., string -> int)
**When** the build runs
**Then** a diagnostic is emitted warning that the generated form input and DataGrid column will change rendering behavior
**And** the severity is Warning (not Error) to allow intentional changes to proceed

**Given** a [BoundedContext] name changes
**When** the build runs
**Then** a diagnostic is emitted that navigation sections will be affected
**And** persisted session state referencing the old context name will not restore

**Given** drift detection runs
**When** performance is measured
**Then** drift detection does not add measurable overhead beyond the existing incremental generator pipeline (<500ms budget, NFR8)

**References:** FR7, NFR8, NFR97 (teaching errors at compile time)

---

### Story 9.2: CLI Inspection & Migration Tools

As a developer,
I want CLI tools to inspect what the source generator produced and to apply automated code fixes when upgrading framework versions,
So that I can debug generation issues and upgrade confidently without manual code changes.

**Acceptance Criteria:**

**Given** a developer runs the CLI inspect command for a specific domain type
**When** the command executes
**Then** the source generator output for that type is displayed from a deterministic file path (obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs)
**And** the output includes: generated Razor component, Fluxor state types, domain registration, and any diagnostics emitted

**Given** the developer wants to see all generated output
**When** the CLI inspect command is run without a type filter
**Then** a summary is displayed: count of generated forms, grids, registrations, and any warnings/errors
**And** each generated file is listed with its path

**Given** a framework version upgrade with breaking API changes
**When** the developer runs the CLI migration tool
**Then** Roslyn analyzer code fixes are applied automatically for known migration patterns
**And** each applied fix is reported with: what changed, why, and the diagnostic ID
**And** the developer can review changes before committing (dry-run mode available)

**Given** the migration tool encounters a change it cannot auto-fix
**When** the manual fix is required
**Then** a clear message describes: what needs to change, where, and links to the migration guide

**Given** the CLI tools
**When** they are distributed
**Then** they are available as dotnet global tools or local tools via the framework's NuGet package

**References:** FR63, FR64, NFR77 (deprecation window)

---

### Story 9.3: IDE Parity & Developer Experience

As a developer,
I want equivalent development experience across Visual Studio, JetBrains Rider, and VS Code with C# Dev Kit,
So that I can use my preferred IDE without losing IntelliSense, navigation, or debugging capabilities.

**Acceptance Criteria:**

**Given** Visual Studio 2026 (reference IDE)
**When** a developer works with FrontComposer source-generated types
**Then** IntelliSense provides completions for generated types and their members
**And** hover documentation shows XML doc comments from generated code
**And** go-to-definition navigates to the generated source file
**And** source generator debugging is supported (breakpoints in generator code)

**Given** JetBrains Rider 2026.1+
**When** a developer works with FrontComposer
**Then** all capabilities available in Visual Studio are also available in Rider (parity)
**And** any known Rider-specific limitations are documented

**Given** VS Code with C# Dev Kit
**When** a developer works with FrontComposer
**Then** IntelliSense, hover documentation, and go-to-definition work for generated types
**And** source generator debugging may have limitations (documented)
**And** the experience is sufficient for lightweight-tooling adopters

**Given** any IDE
**When** the developer hovers over a framework attribute (e.g., [Projection], [BoundedContext])
**Then** XML doc comments describe: what the attribute does, what it generates, and a link to documentation

**Given** CS1591 (missing XML doc comments) enforcement
**When** the project is pre-v1.0-rc1
**Then** CS1591 is a warning
**When** the project is at or past v1.0-rc1 (API freeze milestone)
**Then** CS1591 is an error for all types in PublicAPI.Shipped.txt

**References:** FR65, NFR71, NFR92

---

### Story 9.4: Diagnostic ID System & Deprecation Policy

As a developer,
I want every framework diagnostic to resolve to a documentation page, and deprecated APIs to have clear migration paths,
So that I can self-service resolve any issue and plan upgrades without surprises.

**Acceptance Criteria:**

**Given** the diagnostic ID scheme
**When** IDs are assigned
**Then** reserved ranges are enforced per package:
**And** Contracts: HFC0001-0999
**And** SourceTools: HFC1000-1999
**And** Shell: HFC2000-2999
**And** EventStore: HFC3000-3999
**And** Mcp: HFC4000-4999
**And** Aspire: HFC5000-5999

**Given** any diagnostic emitted by the framework
**When** the developer sees the diagnostic ID
**Then** the ID resolves to a consistent, lookup-addressable documentation page
**And** the documentation page includes: problem description, common causes, resolution steps, and code examples

**Given** a framework API is deprecated
**When** the deprecation is applied
**Then** a minimum one-minor-version window is provided before removal (NFR77)
**And** the [Obsolete] message follows convention: "<old> replaced by <new> in v<target>. See HFC<id>. Removed in v<removal>."
**And** the diagnostic ID links to a migration path

**Given** binary compatibility within minor versions
**When** PublicApiAnalyzers run in CI
**Then** accidental breaking changes within a minor version fail CI (NFR69, NFR76)
**And** intentional breaking changes require a major version bump

**References:** FR66, FR67, NFR69, NFR76, NFR77, NFR80

---

### Story 9.5: Diataxis Documentation Site

As a developer,
I want a comprehensive documentation site organized by learning need, with a day-1 customization cookbook,
So that I can find tutorials when learning, how-tos when building, reference when checking, and concepts when understanding.

**Acceptance Criteria:**

**Given** the documentation site
**When** it is generated
**Then** DocFX produces the site (not Blazor-native SSG, NFR95)
**And** the site is organized into four Diataxis genres:
**And** **Tutorials**: step-by-step learning paths (e.g., "Build your first FrontComposer domain")
**And** **How-to guides**: task-oriented recipes (e.g., "How to override a field renderer")
**And** **Reference**: API documentation, attribute catalog, diagnostic ID lookup
**And** **Explanation/Concepts**: architectural decisions, design philosophy, pattern rationale

**Given** the single-source documentation strategy
**When** documentation is authored
**Then** explicit narrative vs. reference section markers separate the two rendering targets
**And** the MCP renderer strips narrative sections (returns reference only)
**And** the DocFX site keeps both narrative and reference
**And** this prevents voice collapse between human docs and agent docs (NFR96)

**Given** v1 launch
**When** the documentation site is published
**Then** the day-1 highest-leverage document is the customization gradient cookbook (NFR98)
**And** the cookbook shows the same problem solved at each of the four gradient levels
**And** it includes copy-pasteable code examples for each level

**Given** a framework change that breaks a shipped skill corpus example
**When** the change is merged
**Then** a migration guide is required regardless of semantic version bucket (FR69)
**And** the migration guide is published on the documentation site linked from the relevant diagnostic ID

**Given** error messages in the framework
**When** they are authored
**Then** the error message template (Expected/Got/Fix/DocsLink) is part of the attribute definition
**And** the source generator test enforces the template is filled in (build will not ship without it, NFR97)

**References:** FR68, FR69, NFR95-98

---

**Epic 9 Summary:**
- 5 stories covering all 8 FRs (FR7, FR63-69)
- Relevant NFRs woven into acceptance criteria (NFR8, NFR69, NFR71, NFR76-77, NFR80, NFR92, NFR95-98)
- Built incrementally: drift detection (with Epic 1 generator), CLI tools (with Epic 1-2), IDE parity (ongoing), diagnostics (with each package), docs (with v1 launch)
- Stories are sequentially completable: 9.1 (drift detection) -> 9.2 (CLI tools) -> 9.3 (IDE parity) -> 9.4 (diagnostics/deprecation) -> 9.5 (documentation site)

---

## Epic 10: Framework Quality & Adopter Confidence

Framework provides test host/utilities for adopters, automated CI gates (accessibility checks, visual specimens, Pact contracts, mutation testing, property-based idempotency, flaky quarantine), LLM code-generation benchmark, and signed releases with SBOM. Built incrementally alongside earlier epics -- quality gates are woven into each phase, not deferred to the end.

### Story 10.1: Adopter Test Host & Component Testing Utilities

As a developer,
I want a test host and utilities that let me write component tests for my customization overrides and auto-generated views,
So that I can verify my customizations work correctly without manually running the application.

**Acceptance Criteria:**

**Given** the framework's test host package
**When** an adopter references it in a test project
**Then** FrontComposerTestBase (optional base class) is available
**And** it pre-configures: Fluxor store with all framework features, InMemoryStorageService, fake IOverrideRegistry, and mock ICommandDispatcher/IQueryExecutor

**Given** an adopter writes a bUnit test for a customization override
**When** the test renders the custom component
**Then** the framework's lifecycle wrapper, density context, theme context, and render context are available
**And** the component renders within the framework's expected environment (Fluxor state, storage, DI)

**Given** an adopter writes a bUnit test for an auto-generated DataGrid view
**When** the test provides mock projection data
**Then** the generated DataGrid renders with correct columns, formatting, badges, and empty states
**And** the test can assert on: column count, column headers, cell values, badge states, and accessibility attributes

**Given** the test utilities
**When** test data is created
**Then** the Builder pattern is available for domain model construction
**And** test naming follows the project convention: {Method}_{Scenario}_{Expected} or Should_{Behavior}_When_{Condition}

**Given** unit test coverage targets
**When** coverage is measured on core framework code (generator core, command pipeline, SignalR reconnection logic)
**Then** line coverage >= 80% (NFR51)
**And** component test coverage on auto-generated Razor components >= 15% line coverage (NFR52)
**And** integration tests: minimum 3 tests per API boundary (NFR53)

**References:** FR71, NFR51-53

---

### Story 10.2: Accessibility CI Gates & Visual Specimen Verification

As a developer,
I want automated accessibility checks and visual specimen verification that block merge on violations,
So that every release maintains WCAG 2.1 AA conformance and visual consistency across themes and densities.

**Acceptance Criteria:**

**Given** a pull request with UI changes
**When** the accessibility CI gate runs
**Then** axe-core runs via Playwright on the type specimen and data formatting specimen views
**And** "serious" or "critical" WCAG violations block merge (NFR37)
**And** contrast verification runs via axe-core
**And** keyboard navigation tests run via Playwright scripted tab-order tests
**And** focus visibility is verified via Playwright screenshot diff

**Given** the type specimen verification view
**When** it renders in CI
**Then** it displays: every type ramp slot, every semantic color token, both Light and Dark themes, all three density levels
**And** it contains: one DataGrid with column headers and six badge states, one flat command form with five-state lifecycle wrapper, one expanded detail view, one multi-level nav group

**Given** the data formatting specimen view
**When** it renders in CI
**Then** a single DataGrid with one row per data type exercises all formatting rules: locale-formatted numbers, absolute and relative timestamps, truncated IDs, null em dashes, collection counts, currency, boolean Yes/No, truncated enums

**Given** specimen screenshots
**When** they are compared against committed baselines
**Then** v1 compares per theme x density (6 specimens: 2 themes x 3 densities). RTL and zoom-level specimens deferred to v1.x
**And** baseline updates require a rationale paragraph and before/after screenshots

**Given** additional accessibility CI checks
**When** the full suite runs
**Then** forced-colors mode emulation is tested
**And** reduced-motion emulation is tested
**And** zoom/reflow at 100%/200%/400% is tested
**And** density parity testing renders specimens 3x (one per density level)

**Given** manual screen reader verification
**When** a release branch is cut
**Then** manual verification with NVDA+Firefox, JAWS+Chrome, VoiceOver+Safari is performed
**And** verification logs are committed to docs/accessibility-verification/

**References:** FR76, FR77, UX-DR32, UX-DR33, UX-DR34, NFR37-38

---

### Story 10.3: Consumer-Driven Contract Tests (Pact)

As a developer,
I want contract tests that verify generated UI components consume EventStore API contracts correctly,
So that API changes never silently break the generated UI and I catch contract drift before deployment.

**Acceptance Criteria:**

**Given** the Pact contract testing setup
**When** contracts are defined
**Then** they are file-based (not Pact Broker), checked into Shell.Tests/Pact/
**And** contracts cover the REST-to-generated-UI seam: command dispatch (POST /api/v1/commands), query execution (POST /api/v1/queries), and ETag handling

**Given** a Pact contract for command dispatch
**When** the consumer test runs
**Then** it verifies: the generated UI sends correctly shaped command payloads, includes ULID message IDs, includes TenantId in headers, and expects 202 Accepted responses

**Given** a Pact contract for query execution
**When** the consumer test runs
**Then** it verifies: the generated UI sends correctly shaped query parameters, sends If-None-Match headers with cached ETags, and correctly handles 200 with data and 304 Not Modified responses

**Given** the provider verification
**When** it runs per release
**Then** the EventStore API provider verifies all consumer contracts
**And** contract violations fail the build (never-cut gate, NFR55)

**Given** Pact implementation timing
**When** Epic 5 (Reliable Real-Time Experience) is being built
**Then** Pact contracts should be authored alongside the EventStore communication stories
**And** they serve as living documentation of the API contract

**References:** FR78, NFR55

---

### Story 10.4: Mutation Testing & Property-Based Testing

As a developer,
I want mutation testing on the source generator and property-based testing for command idempotency,
So that I have confidence the generator produces correct output and commands are replay-safe under all conditions.

**Acceptance Criteria:**

**Given** Stryker.NET mutation testing configuration
**When** mutations are applied to the source generator
**Then** targets are Parse and Transform stages only (not Emit)
**And** kill score >= 80% on the happy-path generation pipeline (NFR56)
**And** kill score >= 60% on error-handling paths
**And** mutations that survive are reviewed and either killed with new tests or documented as acceptable

**Given** Stryker.NET runs
**When** it executes in the nightly CI pipeline
**Then** it completes within the nightly budget (< 45 minutes total, NFR66)
**And** results are published as a CI artifact

**Given** mutation testing timing
**When** Epic 1 (source generator) is complete
**Then** Stryker targets should be configured and initial kill score baselined
**And** the kill score gate ratchets upward over time

**Given** FsCheck property-based testing for command idempotency
**When** test sequences are generated
**Then** replay(commands) == original_outcomes for randomly generated command sequences
**And** CI runs use 1000 sequences with deterministic seed (NFR58)
**And** nightly runs use 10000 sequences with random seed
**And** shrunk failure cases are converted to regression fixtures (deterministic unit tests)

**Given** property-based testing timing
**When** Epic 5 (command resilience) is complete
**Then** FsCheck tests should cover: command replay, duplicate detection, reconnection scenarios
**And** a bounded command vocabulary ensures meaningful test generation

**References:** FR79, FR81, NFR56, NFR58

---

### Story 10.5: Flaky Test Quarantine & CI Governance

As a developer,
I want flaky tests automatically detected and quarantined so they don't erode CI trust,
So that the main CI lane is always reliable and I can confidently treat a red build as a real problem.

**Acceptance Criteria:**

**Given** a test that intermittently fails
**When** the flaky detection system identifies it
**Then** the test is automatically tagged with xUnit Trait("Category", "Quarantined")
**And** the main CI lane excludes quarantined tests
**And** a separate quarantine CI lane runs them and warns on failure (does not block)

**Given** a quarantined test
**When** it passes 5 consecutive nightly runs
**Then** an automated PR is created to remove the Quarantined trait
**And** the test is reintroduced to the main CI lane
**And** if it fails again after reintroduction, it is re-quarantined with increased scrutiny

**Given** zero flaky tests in the main CI lane (NFR57)
**When** the main CI runs
**Then** all tests are deterministic and reliable
**And** any test failure in the main lane is treated as a real issue requiring investigation

**Given** CI pipeline time budgets
**When** pipeline durations are monitored
**Then** inner loop (unit + component) < 5 minutes (NFR64)
**And** full CI (excluding nightly) < 12 minutes (NFR65)
**And** nightly CI < 45 minutes (NFR66)
**And** if full CI exceeds 15 minutes for 3 consecutive days, a mandatory "CI diet" task is auto-created before new feature work (NFR67)

**Given** E2E test requirements
**When** E2E tests are configured
**Then** one Playwright suite per reference microservice covers: happy path, disconnect/reconnect, and rejection rollback (NFR54)

**References:** FR80, NFR54, NFR57, NFR64-67

---

### Story 10.6: LLM Benchmark, Signed Releases & SBOM

As a developer,
I want a nightly LLM code-generation benchmark that validates AI-assisted development quality, and signed releases with supply chain transparency,
So that I can trust the framework's AI development story and verify the provenance of every package I install.

**Acceptance Criteria:**

**Given** the nightly LLM benchmark
**When** it runs on the main branch
**Then** pinned model versions are used with temperature 0 and fixed seed
**And** the prompt corpus contains 20 prompts at v1 scope (50+ at v1.x)
**And** cached prompt-response pairs are stored for regression detection
**And** 4 out of 20 legitimate misses are allowed
**And** a published monthly budget cap limits LLM API costs

**Given** the benchmark ratchet rule
**When** results are evaluated
**Then** the v1 gate = initial baseline measured at week 8 + 5 percentage points grace
**And** the benchmark must not regress below the initial baseline
**And** the 28-day rolling ratchet rule and model transition rules are deferred to v1.x once sufficient data exists (NFR61-62 deferred)

**Given** a release is tagged
**When** the release pipeline runs
**Then** a CycloneDX SBOM is generated for the release (NFR25)
**And** NuGet packages are signed with an OSS-signing certificate (NFR24)
**And** symbol packages (.snupkg) are published for IDE debugging (NFR26)
**And** all packages use lockstep versioning (same version number)

**Given** CI resource monitoring
**When** GitHub Actions billable minutes per release tag exceed 90 minutes OR wall-clock from git tag to nuget.org exceeds 2 hours across 3 consecutive releases
**Then** the package count collapse trigger activates: consider collapsing 8 packages to 5 (NFR100)

**References:** FR73, FR75, NFR24-26, NFR60-63, NFR100

---

**Epic 10 Summary:**
- 6 stories covering all 9 FRs (FR71, FR73, FR75-81)
- Relevant NFRs woven into acceptance criteria (NFR24-26, NFR37-38, NFR51-67, NFR100)
- Relevant UX-DRs addressed (UX-DR32-34)
- Timing alignment noted: each story aligns with the epic where its test target is built
- Stories are sequentially completable: 10.1 (test host) -> 10.2 (accessibility CI) -> 10.3 (Pact) -> 10.4 (mutation/property) -> 10.5 (flaky quarantine) -> 10.6 (LLM benchmark/releases)
