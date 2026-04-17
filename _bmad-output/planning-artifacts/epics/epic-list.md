# Epic List

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
