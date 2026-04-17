# Product Scope

### MVP — Minimum Viable Product (v1.0)

**Scope boundary:** everything required to prove the two-layer composition architecture + the multi-surface claim + the LLM-first discipline on a representative Hexalith.EventStore topology, sized for solo delivery over a ~6-month directional timeline.

**Composition shell (web surface, Blazor Auto)**

- `FluentLayout`-based shell: header (title, theme toggle, Ctrl+K trigger, settings), collapsible sidebar with bounded-context nav groups (2-level max depth), content area, footer.
- Navigation at scale: collapsible groups, command palette (`FrontComposerCommandPalette` wrapping `FluentSearch`), recently visited shortcuts, session persistence (last nav, filters, sort, expanded row).
- Theming: Fluent UI v5 with Teal `#0097A7` default accent overridable at deployment; Dark/Light/System via `<fluent-design-theme>` + LocalStorage persistence.
- Density: three-level preference (Compact / Comfortable / Roomy) with factory hybrid defaults and four-tier precedence rule.
- Accessibility: WCAG 2.1 AA baseline, all 14 commitments from UX spec, CI gates (axe-core, specimen verification).
- `<FluentProviders />` registration, `AddFluentUIComponents(config => ...)` with `DefaultValues`, `IFluentLocalizer` with English + French resource files.

**Auto-generation (web surface)**

- Flat command forms from `[Command]` domain types: type-inferred Fluent components (text, number, select, checkbox, datepicker), FluentValidation-backed `EditContext` integration, domain-language button labels via label resolution chain.
- Action density rules: 0–1 non-derivable fields → inline button; 2–4 fields → compact inline form; 5+ fields → full-page form.
- List views via `FluentDataGrid` from projection types, with virtualized scrolling, status badge column (fixed 6-slot palette), inline action column, session-persisted filters and sort.
- Single-entity detail views via `FluentCard` / `FluentAccordion` with progressive disclosure.
- Empty states with domain-language CTAs ("No orders found. Send your first order command."), per-component loading skeletons (never full-page spinners).
- Five-state command lifecycle wrapper (`FrontComposerLifecycleWrapper`) — progressive visibility thresholds (invisible <300ms, pulse 300ms–2s, text 2–10s, prompt >10s, SignalR-loss fallback with ETag polling); domain-specific rollback messages on rejection; reconnection reconciliation batch with 3s auto-dismissing toast.
- Command+context auto-linking via aggregate relationship metadata.
- Projection role hints — **fixed set of 5**: `ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`. Unknown hints fall back to data table with build-time warning.
- Auto-generation boundary protocol: unsupported field types render as `FrontComposerFieldPlaceholder` with build-time warning and link to customization docs. Zero silent omissions.

**Customization gradient (v1 contract)**

- Annotation-level: `[ProjectionBadge(BadgeSlot.Warning)]`, `[ProjectionRole(Role.ActionQueue)]`, `[DisplayName(...)]`, etc.
- Template-level: typed Razor template overrides bound to domain model contracts.
- Slot-level: typed context-object slot overrides (field value, validation state, metadata).
- Full replacement: complete component swap preserving lifecycle wrapper, accessibility contract, and custom-component accessibility checks.
- Build-time compatibility checks warn when an override's expected contract doesn't match framework version.

**Multi-surface foundation (chat/Markdown surface — v1 architected + alpha ships)**

- Rendering abstraction layer: domain model → `IRenderingContract` → surface-specific renderer (web ships; chat alpha ships).
- Markdown renderer for projections (status cards, tables, timelines) and commands (tool-call stubs with field prompts).
- **Hexalith native chat surface** — working integration that exposes a FrontComposer domain as tools + Markdown projections end-to-end. First renderer to ship.
- MCP server integration: `Hexalith.FrontComposer.McpServer` NuGet tool exposing domain models as typed agent tools (commands as tools, projections as readable resources).
- The other four renderers (Mistral, Claude Code, Cursor, Codex) — **architected-for, at least one smoke test each** by v1 ship, full support in v1.x / v2.

**EventStore communication layer**

- `ICommandService`, `IQueryService`, `ISignalRSubscriptionService` with ETag caching, ULID message IDs, 202 Accepted handling, JWT bearer, multi-tenant `TenantId` propagation.
- `EventStoreSignalRClient` with auto-reconnect and group rejoin.
- Shared NuGet contracts (`Hexalith.FrontComposer.Contracts`) — no JSON schema drift.
- Full error handling matrix (200/202/304/400/401/403/404/409/429) with progressive UX.

**Developer experience**

- `dotnet new hexalith-frontcomposer` project template — scaffolds Aspire AppHost + sample microservice + FrontComposer shell + NuGet references.
- **Three reference microservices** (Counter, Orders, OperationsDashboard) demonstrating the five projection role hint archetypes across a learning arc — minimum viable, full gradient workout, multi-domain composition.
- Quick-start docs (5-minute onboarding), customization gradient docs, architecture overview, LLM benchmark prompts.
- Semantic-release automated versioning from conventional commits.

**Infrastructure & deployment**

- .NET 10, Blazor Server (dev inner loop) + Blazor Auto (production), Fluent UI Blazor v5 (latest stable at ship), DAPR 1.17.7+.
- Keycloak default auth; compatibility with Entra ID, GitHub, Google.
- NuGet package distribution; no direct infrastructure coupling (DAPR abstractions only).
- CI: build, test, specimen verification, axe-core, LLM benchmark suite, deployment topology validation (Azure Container Apps + local Kubernetes).

**Explicit MVP exclusions** (things the UX spec or research call out that do NOT ship in v1):

- Cross-context workflow views (v2).
- Dashboard composition / metric widgets (v2).
- Notification / activity feed (v2).
- Guided wizard mode for multi-step commands (v2).
- Visual configuration tooling (v2).
- Nested object forms beyond flat types (v2).
- Dev-mode overlay (v2).
- Bounded-context sub-branding with multiple accents (v2).
- Cross-device preference sync (v2 — v1 is LocalStorage-only).
- RTL verification (v2 — v1 inherits Fluent UI RTL without explicit tests).
- Full four-renderer chat matrix beyond Hexalith native (v1.x / v2).
- Cross-context nav / search beyond command palette fuzzy match (v2).
- Real-time collaboration indicators (v2).

### Growth Features (Post-MVP, v1.x → v1.5)

- **Full five-renderer chat matrix** — Mistral, Claude Code, Cursor, Codex formally supported with renderer-specific Markdown adapters, feature parity with Hexalith native, renderer-specific demos.
- **Dev-mode overlay** — visualization of auto-generated components, conventions applied, override paths. Discoverability through the framework itself.
- **Dashboard composition** — 12-column responsive grid of projection widgets from projection role hints.
- **Notification / activity feed** — relevance-prioritized (not chronological) SignalR-driven event stream.
- **Cross-context workflow views** — for use cases like "approve this order, check inventory, notify supplier" spanning bounded contexts.
- **Guided wizard mode** for multi-step or high-field-count commands.
- **Nested object forms** with recursive rendering and validation.
- **Cross-device preference sync** via server-side preference store.
- **Explicit RTL verification** via specimen view in Hebrew/Arabic rendering.
- **Bounded-context sub-branding** (first-class multi-accent configuration) if empirical demand justifies.
- **Extended LLM benchmark suite** (50+ prompts, multiple models) with public leaderboard.

### Vision (Future — v2 and beyond)

- **Agent-owned domain loop** — LLM agents not only call commands and read projections but author new microservices end-to-end (commands, events, projections, registration) from conversational intent, using FrontComposer conventions as their scaffolding grammar. The framework becomes an agent IDE for event-sourced systems.
- **Event stream as agent memory substrate** — FrontComposer's chat surface exposes event streams as structured agent memory, aligning with the 2026 convergence of ES + LLM agent memory (Mem0, A-MEM, AxonIQ "agentic era").
- **Real-time multi-user + multi-agent collaboration** — live cursors, shared command queues, conflict-aware optimistic updates across human + agent participants on the same projection.
- **Visual composition editor** — a web-based drag-drop editor that produces FrontComposer-compatible domain definitions, closing the loop for citizen developers and product managers while still emitting the same typed CQRS contracts.
- **Federation of multi-tenant Hexalith deployments** — FrontComposer composes UIs across independent Hexalith.EventStore instances (cross-organization event choreography) without each instance needing to know about the others.
- **Adjacent ES backend adapters** — beyond Hexalith.EventStore, add contract-compatible adapters for Marten, Wolverine, Axon, Kurrent. The composition layer becomes the common frontend story for .NET-adjacent ES ecosystems — but only after the Hexalith story is durable.
