# Epic 8: MCP & Agent Integration

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
