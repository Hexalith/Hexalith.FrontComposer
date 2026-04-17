# Executive Summary

### Project Vision

Hexalith FrontComposer is the missing frontend layer for event-sourced microservices. It provides a unified, convention-driven Blazor frontend that automatically composes polished UIs for all Hexalith.EventStore microservices -- enabling .NET developers to expose rich interfaces with minimal code while delivering business users a seamless experience across the entire application landscape.

The framework operates through a two-layer composition architecture. The first layer treats the domain model as a UI contract: each microservice's commands, events, and projections are introspected to auto-generate intent-driven forms, observation panels, and detail views. The second layer is a composition shell that discovers and composes these UI fragments from all microservices into one coherent application -- handling navigation, layout, consistency, and Fluent UI theming.

The architecture is optimized for predictable, consistent patterns that accelerate both human developers and LLM code generation. The tech stack is C# / .NET 10 / Blazor (Server + WebAssembly) / Fluent UI Blazor v5 / DAPR, communicating with Hexalith.EventStore through REST API (commands/queries) and SignalR (real-time projection change notifications).

### Target Users

**Primary: .NET Developers**
Backend-focused developers with strong DDD/CQRS/event sourcing expertise but limited frontend comfort. They want to expose microservice functionality through polished UIs with minimal effort and no frontend specialization required. Their primary frustration is boilerplate UI code, keeping UIs in sync with evolving domain models, and the architectural complexity of composing multiple microservice UIs. FrontComposer serves them by making the domain model the single source of truth for both backend and frontend. Critically, developers need a smooth **customization gradient** -- not a binary switch between auto-generated and fully custom -- so that escaping conventions is trivial at any granularity (whole view, section, or single field).

**Secondary: Business Users**
End users of applications built with FrontComposer who interact with the composed UI daily. They think in **workflows** that span bounded contexts ("approve this order, check inventory, notify supplier"), not in data views. They expect a seamless, coherent application experience where domain groupings feel natural and meaningful -- the shell is translucent, not invisible. Their frustration is inconsistent UI across modules, slow feature delivery due to frontend bottlenecks, and confusing navigation when multiple microservices are stitched together.

### Key Design Challenges

1. **Eventual consistency UX** -- Commands are processed asynchronously (202 Accepted). The UI must handle five lifecycle states as **progressive system health indicators** -- invisible on the happy path, increasingly visible under degraded conditions. Timing thresholds: show nothing if confirmed <300ms, show sync pulse if 300ms-2s, show explicit "Still syncing..." text if 2-10s, show action prompt with refresh option if >10s, fall back to polling with ETag if SignalR connection lost. Double-submission prevention and clear visual feedback are non-negotiable.

2. **The customization gradient** -- Auto-generated UIs must transition smoothly to custom implementations through **contract-based customization**: each override level (annotation, template, slot, full replacement) binds to a typed contract, not to the framework's internal DOM structure. Slot overrides receive a typed context object (field value, validation state, metadata). Templates receive a typed model. Build-time compatibility checks warn when an override's expected contract doesn't match the current framework version. Developers must be able to replace a single field's rendering without rewriting the entire view.

3. **Seamless multi-service composition** -- Business users must perceive meaningful domain groupings (Orders, Inventory, Customers) without knowing they're bounded contexts. The shell should include composition intelligence: priority-based navigation, dashboard widgets from projection metadata, and attention-routing for state changes requiring action. **Navigation at scale** (10+ bounded contexts) requires: collapsible nav groups, maximum 2-level depth, command palette (Ctrl+K) for direct access, and recently visited shortcuts.

4. **Command + context pattern** -- Command forms should never appear without the relevant projection context. **Aggregate relationship metadata** (which commands affect which projections) enables auto-linking: when a user submits a command, the affected projection is visible and live-updating via SignalR, closing the loop between intent and observation. The framework introspects domain model relationships to determine these links automatically.

5. **Developer-business user continuum** -- Developer conventions and business user experience are not separate design problems. Every convention must be validated against the business user experience it produces. If a convention generates an awkward UI by default, the convention is wrong.

6. **Shell-first architecture** -- The composition shell (navigation, layout, theming, lifecycle management) is the primary deliverable. Auto-generation is the accelerator built on top. A developer who overrides every view should still benefit enormously from FrontComposer's shell capabilities alone.

### Design Opportunities

1. **Intent-driven UI language** -- Since the underlying model is commands (intents) and projections (observations), the UI vocabulary should reflect this: action buttons named after domain intents ("Send Order," "Approve Request," never "Submit" or "Save"), projection views presented as observation panels, not generic data tables. Domain language in every interaction.

2. **Intelligent defaults from domain metadata** -- FrontComposer's knowledge of the domain model enables auto-generated UIs that are smarter than scaffolding: contextual labels, appropriate input types, validation messages from FluentValidation rules, intelligent field grouping, meaningful empty states ("No orders found. Create one?"), and proper loading states ("Loading projections..."). A **label resolution chain** ensures field labels are always human-readable: (1) explicit display name annotation, (2) resource file lookup, (3) humanized CamelCase ("OrderDate" → "Order Date"), (4) raw field name as last resort.

3. **Action density rules** -- The framework auto-determines command rendering based on field count and context derivability: commands with 0-1 non-derivable fields render as **inline buttons** on list rows, commands with 2-4 fields render as **compact inline forms**, and commands with 5+ fields render as **full-page forms**. This eliminates the one-size-fits-all form pattern and makes common actions feel instant.

4. **Projection role hints** -- A small, permanently capped set (5-7) of rendering hints that upgrade auto-generated views from generic tables to role-appropriate patterns: `ActionQueue` (items needing user action), `StatusOverview` (aggregate status dashboard), `DetailRecord` (single-entity detail), `Timeline` (chronological events), `Dashboard` (metric widgets). Unknown roles default to data table. One annotation, massive UX improvement -- the first rung of the customization gradient.

5. **List-detail inline pattern** -- Default DataGrid rows that expand to show projection detail views inline, reducing navigation depth and preserving context. Business users can scan a list and drill into details without losing their place.

6. **Lifecycle as composition wrapper** -- Command lifecycle state management (idle → submitting → acknowledged → syncing → confirmed) is implemented as a cross-cutting composition wrapper, not per-component logic. Custom components inherit lifecycle behavior automatically. The wrapper is preserved even when developers override the inner component through the customization gradient.

7. **Real-time activity awareness** -- The SignalR projection change pattern naturally supports a notification/activity feed showing recent changes across subscribed projections. This enables multi-user awareness and reduces the need for manual refresh.

8. **Developer-mode overlay** -- A dev-mode visualization layer showing which components are auto-generated, what conventions produced them, and how to override them. Discoverability through the framework itself, not just external documentation.

### V1 UX Scope Boundary

The v1 UX scope is deliberately constrained to deliver a tight, polished experience:

- **In scope:** Composition shell (navigation, layout, theming, lifecycle wrapper), flat command forms, list DataGrid views, single-entity detail views, five-state command lifecycle with timeout escalation, empty/loading states, contract-based granular component replaceability, command+context auto-linking via aggregate metadata, domain-language action labels, action density rules, label resolution chain, navigation at scale (collapsible groups, command palette), projection role hints (5-7 capped)
- **V2 candidates:** Cross-context workflow views, dashboard composition, notification/activity feed, guided wizard mode for complex commands, visual configuration tooling, nested object forms, dev-mode overlay
- **Core principles:** (1) Granular replaceability at any level (view, section, field) via typed contracts is an architectural requirement. (2) The shell is valuable without auto-generation -- it's the foundation, not a wrapper. (3) Projection role hints are permanently capped at 5-7; beyond that, use template overrides.
