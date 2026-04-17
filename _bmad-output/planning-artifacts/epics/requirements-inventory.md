# Requirements Inventory

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
