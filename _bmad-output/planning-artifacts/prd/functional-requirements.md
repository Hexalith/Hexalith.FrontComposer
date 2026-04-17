# Functional Requirements

*82 FRs across 9 capability areas. Revised after Party Mode round + Advanced Elicitation pre-mortem triage. Each FR states WHAT capability exists (WHO can do WHAT), not HOW. Implementation details, timing thresholds, and quality metrics are deferred to §Non-Functional Requirements. This is THE CAPABILITY CONTRACT: features not listed here will not exist in v1 unless explicitly added.*

*Party Mode validated coverage against all 6 journeys, 4 innovations, §Product Scope MVP, §Developer Tool Requirements, and §Project Scoping. Pre-mortem triage added 16 MUST FRs (4 were missing FRs for existing commitments, 12 were genuine gaps). 5 DEFER items are noted as v1.x placeholders at the end. FR37 merged into FR57 per Amelia.*

### Domain Auto-Generation

- **FR1**: Developer can mark a command record with `[Command]` and have the framework generate a corresponding form component at compile time.
- **FR2**: Developer can mark a projection type with `[Projection]` and have the framework generate a DataGrid view at compile time.
- **FR3**: Developer can declare bounded contexts with `[BoundedContext(name)]` and have the framework group them as navigation sections with optional domain-language display labels.
- **FR4**: Developer can tag a projection with `[ProjectionRole(role)]` to signal its rendering role hint (ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard — capped at 5).
- **FR5**: Developer can tag a status enum value with `[ProjectionBadge(slot)]` and have the framework render a semantic status badge from a configurable palette.
- **FR6**: Framework can infer field rendering from .NET types (primitives, enums, DateTimeOffset, collections) and select the appropriate input component without developer intervention.
- **FR7**: Framework can detect drift between backend domain declarations and generated UI at build time, surfacing mismatches as compile-time diagnostics rather than runtime silent behavior.
- **FR8**: Framework can select command rendering density — inline button (0–1 non-derivable fields), compact inline form (2–4 fields), or full-page form (5+ fields) — based on field count.
- **FR9**: Framework can produce an explicit placeholder for unsupported field types with a build-time warning and documentation link, so that unsupported types never render silently.
- **FR10**: Framework can surface developer-provided field descriptions as contextual help (tooltips, inline labels) in generated views, so business users understand column and field meanings without developer intervention.
- **FR11**: Framework can render meaningful empty states with domain-language guidance and contextual calls-to-action for projections with no data.
- **FR12**: Business user can filter, sort, and text-search within auto-generated projection DataGrid views, with filter/sort state persisted across sessions.

### Composition Shell & Navigation

- **FR13**: Developer can install the framework's composition shell with a single NuGet meta-package reference and register a domain with a minimal registration ceremony.
- **FR14**: Developer can configure the shell's theme with a customizable accent color overridable at deployment.
- **FR15**: Business user can toggle between Light, Dark, and System themes and have the preference persist across sessions.
- **FR16**: Business user can select display density (Compact, Comfortable, Roomy) and have the preference apply across all generated views.
- **FR17**: Business user can navigate between bounded contexts via a collapsible sidebar with nav groups up to two levels of hierarchy depth.
- **FR18**: Business user can invoke a command palette via keyboard shortcut (Ctrl+K) to fuzzy-search commands, projections, and recently visited views across all bounded contexts.
- **FR19**: Business user can resume a prior session with last-visited navigation section, applied filters, sort order, and expanded rows restored.
- **FR20**: Business user can expand an entity row in place for detail view without navigating away from the projection context.
- **FR21**: Framework can surface newly available bounded contexts or commands with a "New" badge on first appearance after a framework or domain update.
- **FR22**: Framework can preserve in-progress command form state across connection interruptions, restoring unsaved field values after reconnection.

### Command Lifecycle, Event Store Communication & Eventual-Consistency UX

- **FR23**: Framework can render a five-state command lifecycle (idle → submitting → acknowledged → syncing → confirmed) with progressive visibility thresholds (thresholds defined in §NFRs).
- **FR24**: Framework can detect SignalR connection loss and display a warning-colored connection-lost inline note without disrupting the user's in-flight workflow.
- **FR25**: Framework can reconnect to SignalR after network restoration, rejoin subscribed projection groups, and fire an ETag-gated catch-up query to reconcile stale projection state.
- **FR26**: Framework can batch stale projection updates from reconnection into a single animation sweep, rather than per-row flashes.
- **FR27**: Framework can display a short auto-dismissing reconnect notification after successful reconciliation.
- **FR28**: Framework can surface domain-specific command rejection messages that name the conflicting entity and propose a concrete resolution, rather than generic error messages.
- **FR29**: Framework can handle idempotent command outcomes: a command landing during a disconnect produces the correct terminal state on reconciliation without user-visible duplication.
- **FR30**: For each command submission, the framework shall emit exactly one user-visible outcome (success, rejection, or error notification) — never silently fail, never produce duplicate user-visible effects.
- **FR31**: Framework can fall back to ETag-gated polling when SignalR is unavailable, preserving correctness of the projection view under degraded network conditions.
- **FR32**: Framework can abstract all event-store communication behind swappable service contracts supporting command dispatch, query execution, and real-time subscription, without direct coupling to infrastructure providers.
- **FR33**: Framework can cache query results per tenant and user in client-side storage with ETag validation and bounded eviction, as an opportunistic cache where correctness comes from server reconciliation.
- **FR34**: Framework can handle the full HTTP response matrix (200, 202, 304, 400, 401, 403, 404, 409, 429) with progressive user-facing UX tailored to each response class.
- **FR35**: Framework can propagate tenant context from JWT bearer tokens through all command and query operations, enforcing tenant isolation at the framework layer.
- **FR36**: Framework can generate unique message identifiers for command idempotency, ensuring replay-safe handling with deterministic duplicate detection.
- **FR37**: Framework can integrate with Keycloak, Microsoft Entra ID, GitHub, and Google identity providers via standard OIDC/SAML flows without shipping a custom authentication UI.
- **FR38**: Developer can register a domain with the Aspire hosting builder via a typed extension method (e.g., `.WithDomain<T>()`), completing the registration ceremony alongside `Program.cs` registration.

### Developer Customization & Override System

- **FR39**: Developer can override field rendering via declarative attributes without writing custom components (annotation level).
- **FR40**: Developer can override component rendering via typed Razor templates bound to domain model contracts (template level).
- **FR41**: Developer can override a specific projection field via a typed slot declaration using refactor-safe lambda expressions (slot level).
- **FR42**: Developer can replace a generated component entirely with a custom implementation while preserving the framework's lifecycle wrapper, accessibility contract, and custom-component accessibility checks (full-replacement level).
- **FR43**: Framework can validate customization overrides against the framework version at build time, warning when an override's expected contract doesn't match the installed framework version.
- **FR44**: Framework can provide hot reload support for all four customization gradient levels without application restart.
- **FR45**: Framework can ensure customization and generation errors include sufficient context for developer self-service resolution: what was expected, what was found, how to fix it, and a diagnostic ID linking to a documentation page.
- **FR46**: Developer can apply authorization policies to commands via declarative attributes that integrate with ASP.NET Core authorization middleware.
- **FR47**: Framework can isolate rendering failures in customization gradient overrides to the affected component via error boundaries, preventing one faulty override from crashing the composition shell, with a diagnostic ID in the fallback UI.
- **FR48**: Framework can enforce at build time that no generated or framework code references infrastructure provider types directly, routing all communication through framework abstractions.

### Multi-Surface Rendering & Agent Integration

- **FR49**: Framework can expose a domain model as typed agent tools via an in-process Model Context Protocol server hosted alongside the composition shell.
- **FR50**: Framework can emit typed MCP tool parameters with validation constraints derived from domain validation rules, preventing schema divergence between web and agent surfaces.
- **FR51**: Framework can reject MCP tool calls referencing unknown tool names at the contract boundary, returning a suggestion response with the correct tool name and the full tenant-scoped tool list — stopping hallucinations at the fence.
- **FR52**: Framework can expose commands as MCP tools using a two-call lifecycle pattern where the command invocation returns an acknowledgment with a subscription URI, and state transitions are exposed via a separate lifecycle tool with guaranteed terminal states.
- **FR53**: Framework can render projections as Markdown tables, status cards, and timelines consumable by LLM agents through chat surfaces.
- **FR54**: Framework can enumerate MCP tools scoped to the agent's active tenant, so agents see only the tools available for their authorization scope.
- **FR55**: Framework can publish a versioned skill corpus containing attribute references, domain-modeling conventions, and code generation patterns as both a NuGet package and MCP-discoverable resources at runtime, consumable by LLM agents and human developers.
- **FR56**: Framework can produce typed NuGet contracts shared between backend, web surface, and MCP surface from a single source, with auto-generated MCP tool descriptions derived from the same source as web form labels — preventing schema drift across modalities.
- **FR57**: LLM agent (runtime) can issue commands and read projections against a FrontComposer-registered domain with the same lifecycle semantics and rollback messages as the web surface.
- **FR58**: LLM agent (build-time) can produce compilable, structurally-valid microservice code from a fixed prompt corpus, with framework-emitted typed partial types guiding the agent into a compiler-checked shape. Tested against a pinned model version with a structural validator.
- **FR59**: Framework can emit schema hash fingerprints per projection and MCP tool manifest, enabling graceful client/server version negotiation when framework versions diverge across deployments.
- **FR60**: Framework can produce a migration delta or breaking-change diagnostic when a schema hash fingerprint differs from the previously deployed version, providing a remediation path rather than detection alone.
- **FR61**: Framework can define a rendering abstraction contract that decouples composition logic from surface-specific renderers, enabling multi-surface rendering from a single domain source even when v1 ships only one surface through it.

### Developer Experience & Tooling

- **FR62**: Developer can scaffold a new FrontComposer project via a project template that provides a ready-to-run local development topology with all framework integrations pre-wired.
- **FR63**: Developer can use a CLI tool to inspect source-generator output for a specific domain type at a deterministic file path.
- **FR64**: Developer can run a CLI migration tool to apply Roslyn analyzer code fixes for cross-version framework upgrades.
- **FR65**: Developer can use Visual Studio, JetBrains Rider, or VS Code with C# Dev Kit and receive equivalent IntelliSense, hover documentation, go-to-definition, and source generator debugging experience.
- **FR66**: Framework can reserve a dedicated diagnostic ID range per package, so any framework diagnostic resolves to a consistent, lookup-addressable documentation page.
- **FR67**: Framework can deprecate an API with a minimum one-minor-version window and publish deprecation messages that link to a migration path via diagnostic ID.
- **FR68**: Framework can publish documentation in four Diátaxis genres — tutorials, how-to, reference, and explanation/concepts — as a generated documentation site.
- **FR69**: Framework can require a migration guide for any change that would break a shipped skill corpus example, regardless of semantic version bucket.
- **FR70**: Developer can incrementally rebuild source-generator output on domain attribute change via hot reload without full application restart.
- **FR71**: Framework can provide a test host and utilities for generated components, enabling adopters to write component tests for their customization gradient overrides and auto-generated views.

### Observability

- **FR72**: Framework can emit structured logging events from the lifecycle wrapper and runtime services following OpenTelemetry semantic conventions, enabling end-to-end distributed tracing from user click → backend → projection update → SignalR → UI update.
- **FR73**: Framework can run a nightly LLM code-generation benchmark as a quality gate with pinned model versions and a rolling-median threshold.

### Release Automation & Supply Chain

- **FR74**: Framework can produce semantic version releases automatically from conventional commits.
- **FR75**: Framework can generate a CycloneDX SBOM per release and publish signed NuGet packages with symbol packages for IDE debugging.
- **FR76**: Framework can verify accessibility conformance via automated CI checks, blocking merge on serious or critical WCAG violations.
- **FR77**: Framework can verify visual specimens per release across theme, density, and language-direction dimensions, failing merge on unexplained drift.

### Test Infrastructure & Quality Gates

- **FR78**: Framework can verify generated UI components consume EventStore API contracts correctly via consumer-driven contract tests at the REST-to-generated-UI seam.
- **FR79**: Framework can verify source generator correctness via mutation testing, ensuring that code mutations in the generator produce detectable failures in generated output.
- **FR80**: Framework can automatically detect, isolate, and quarantine flaky tests into a separate CI lane with a reintroduction gate.
- **FR81**: Framework can verify command idempotency via property-based testing with randomly generated command sequences, ensuring replay-safety across reconnect scenarios.
- **FR82**: Framework can expose a test harness for simulating SignalR connection faults (drop, delay, partial delivery, reorder) without requiring a live server.

### Deferred Capabilities (v1.x Placeholders)

*The following were proposed in Party Mode but triaged as DEFER by pre-mortem analysis. They are listed here as v1.x placeholders so they remain visible to the roadmap without expanding v1 scope.*

- **D1**: Framework can display a generation summary after source-generator execution, showing counts of generated forms, grids, and flagged issues. *(Nice-to-have; FR9 placeholder warnings provide the critical path.)*
- **D2**: Business user can select multiple projection rows and issue a batch command. *(Not in §Product Scope MVP; not in any journey.)*
- **D3**: Business user can export filtered projection data as CSV or clipboard content. *(Not in scope; common enough to plan for v1.x.)*
- **D4**: Framework can gate incomplete generator features behind preview flags to keep main shippable during iterative development. *(Solo maintainer can use preprocessor directives; formal flags add overhead.)*
- **D5**: Framework can detect source-generator compilation speed and component render-time regressions via performance benchmarks on hot paths. *(Real pain but Jerome notices in own dev loop; BenchmarkDotNet adds CI complexity.)*

### Rejected Proposal

- **Domain-level notifications / activity feed.** Rejected: §Product Scope MVP exclusions explicitly list "Notification / activity feed (v2)." Adding it as a v1 FR would contradict the scoping section.
