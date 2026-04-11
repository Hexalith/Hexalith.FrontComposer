# Product Brief: Hexalith FrontComposer

## Strategic Summary

FrontComposer is the missing frontend layer for event-sourced microservices. It takes the domain models developers already build with Hexalith.EventStore -- commands, events, projections -- and automatically composes them into a polished, unified Blazor application. Developers write domain code; the UI follows.

Two audiences, one framework. .NET developers who are experts in DDD/CQRS but shouldn't need to become frontend specialists just to expose their microservices. And business users who deserve a seamless, consistent experience regardless of how many microservices are running underneath -- they should never feel the seams.

The unfair advantage is vertical integration: FrontComposer owns both sides of the equation -- the backend framework and the frontend composer -- making the depth of alignment between domain model, event store, and UI impossible for external tools to replicate. The architecture is optimized for LLM code generation first, with delivery velocity as a natural consequence. This is not a component library bolted on top; it's a complete, opinionated application framework built for the event-sourcing ecosystem.

## Vision

Provide a unified, convention-driven Blazor frontend that automatically composes polished UIs for all Hexalith.EventStore microservices -- enabling developers to expose rich interfaces with minimal code while delivering business users a seamless, best-practice experience across the entire application landscape.

**Key Insights from Discussion:**
- **Dual audience:** Developers (minimal effort to expose UI) and business users (consistent, polished UX)
- **Deep event-sourcing integration:** Leveraging knowledge of commands, events, and projections to auto-generate intelligent UIs -- the core differentiator vs generic micro-frontend approaches
- **Convention over code:** Both auto-generated views from the domain model AND custom Blazor components when needed
- **Tech stack:** C# / .NET / Blazor with Fluent UI components
- **Open-source ambition:** Intended for adoption by other teams building on Hexalith.EventStore within 6 months

## Positioning

**Positioning Statement:**
For .NET developers and teams building distributed microservices with Hexalith.EventStore who need a frontend that respects their DDD/CQRS/event-sourcing architecture, Hexalith FrontComposer is an event-sourced frontend composition framework that delivers polished, unified UIs with minimal code per microservice. Unlike Oqtane or ABP, which impose CRUD paradigms that break decoupling and domain boundaries, FrontComposer natively understands commands, events, and projections -- preserving the distributed, domain-driven architecture teams chose in the first place.

**Components:**

- **Target Customer:** .NET developers and teams building distributed, event-sourced microservices with Hexalith.EventStore
- **Their Need:** A frontend that natively aligns with their distributed, DDD/CQRS/ES architecture without introducing coupling or paradigm mismatch
- **Product Category:** Event-sourced frontend composition framework (Blazor)
- **Key Benefit:** Architectural alignment -- the UI respects distribution, decoupling, and domain boundaries with minimal code per microservice
- **Alternatives:** Oqtane, ABP (mature but CRUD-oriented application frameworks)
- **Differentiator:** Native event-sourcing integration -- understands commands, events, and projections, preserving microservices constraints that traditional frameworks break by design

**Strategic Rationale:**
Event-sourced systems are inherently distributed, domain-driven, and decoupled. Existing frontend frameworks like Oqtane and ABP are more mature but fundamentally CRUD-oriented, forcing an architectural mismatch that undermines the very benefits teams adopted event sourcing for. FrontComposer fills this gap by being purpose-built for the Hexalith.EventStore ecosystem, trading breadth of maturity for depth of architectural alignment.

## Business Model

**Model:** Open-source, community-driven (B2B + B2C hybrid)

**Adoption Pattern:**
- **Individual developers** discover and adopt FrontComposer for their own projects (bottom-up, organic)
- **Teams/organizations** standardize on it as part of their Hexalith.EventStore microservice architecture (top-down, championed internally)

**Revenue Model:** None -- purely open-source. Value is ecosystem growth around Hexalith.

**Implications:**
- Success measured by adoption, community contributions, and ecosystem health -- not revenue
- Developer experience and documentation are critical drivers for organic growth
- Low barrier to entry is essential -- developers must be productive within minutes
- Community trust and transparency are key to sustained adoption

## Business Customers

**Ideal Organization Profile:**
- **Type:** Greenfield projects -- teams building new distributed systems, not migrating legacy
- **Deployment philosophy:** Sovereignty and flexibility -- must run on-premise, sovereign cloud, or major cloud providers (Azure, AWS, GCP) without vendor lock-in

**Decision-Making Structure:**
- **Architect (buyer):** Chooses the stack, evaluates architectural fit with DDD/CQRS/ES principles
- **Individual developer (buyer + user):** Discovers FrontComposer, advocates for adoption, and builds with it daily

**Key Adoption Drivers:**
- Vendor independence and deployment freedom
- Architectural alignment with event-sourced, distributed systems
- Open-source transparency -- no lock-in at any layer

## Target Users

### Primary User: .NET Developer

- **Profile:** Backend-focused developer, strong in DDD/CQRS/event sourcing, less comfortable with frontend/UI patterns
- **Frustrations:**
  - Boilerplate UI code -- wiring up forms, lists, views for every command/projection
  - Frontend skill gap -- deep backend expertise but uncomfortable with UI/UX
  - Keeping UI in sync with evolving domain models -- command changes break the frontend
  - Architectural complexity of composing multiple microservice UIs into one coherent app
- **Goal:** Expose microservice functionality through a polished UI with minimal effort and no frontend expertise required
- **Current solution:** Build Blazor apps from scratch or use mismatched frameworks (Oqtane/ABP)

### Secondary User: Business User

- **Profile:** End user of applications built with FrontComposer -- interacts with the composed UI daily
- **Frustrations:**
  - Inconsistent UI across modules/microservices
  - Slow or incomplete features due to frontend development bottleneck
  - Confusing navigation when the app is multiple microservices stitched together
- **Goal:** A seamless, coherent application experience -- doesn't care about the architecture, just wants it to work well and feel like one product
- **Current solution:** Suffers through fragmented, inconsistent UIs or waits for developers to hand-build polished frontends

## Product Concept

**Core Structural Idea:** Two-layer composition architecture

1. **Domain Model as UI Contract:** Each microservice's commands, events, and projections are introspected to auto-generate forms, lists, and views. Developers don't build UI -- they build domain models, and the UI follows automatically.

2. **Composition Shell:** FrontComposer acts as a host that discovers and composes auto-generated (or custom) UI fragments from all microservices into one coherent application -- handling navigation, layout, consistency, and Fluent UI theming.

**Implementation Principle:** Convention over code -- the domain model is the single source of truth for both backend and frontend.

**Rationale:** Solves complexity at both levels: developers don't need frontend skills (layer 1), and business users get a unified experience regardless of how many microservices are underneath (layer 2).

**Concrete Example:** A developer creates a new microservice with an `OrderCommand` and an `OrderProjection`. Without writing any UI code, FrontComposer discovers those, generates a submission form and a list view, and integrates them into the shell's navigation -- all following Fluent UI best practices.

**Features That Stem From Concept:**
- Auto-generated command forms from domain commands
- Auto-generated list/detail views from projections
- Shell-managed navigation, layout, and theming
- Custom Blazor component override when auto-generation isn't enough
- Microservice discovery and registration

## Success Criteria

### Primary Metrics

1. **LLM Code Generation Compatibility:** FrontComposer's conventions and patterns are so consistent and well-structured that LLMs can reliably generate correct microservice code with UI integration. The architecture is optimized for machine-readability -- predictable patterns, minimal ambiguity, consistent conventions.

2. **Delivery Velocity:** Near-zero frontend effort for standard views. Domain model definition is the only required input -- UI follows automatically. Massive reduction in time-to-feature for microservice teams.

### Secondary Metrics

3. **UX Quality:** Business users get a consistent, polished, accessible experience across all modules -- no fragmented feel regardless of how many microservices are composed.

4. **Reliability:** Comprehensive unit and E2E test coverage for both FrontComposer itself and the generated UIs.

5. **Adoption:** Working open-source tool with community adoption within 6 months.

### Strategic Note
LLM compatibility is the top priority. This means the architecture must be optimized for machine-readability first -- delivery velocity then becomes a natural consequence of that design choice.

## Competitive Landscape

### Alternatives

| Alternative | Strength | Weakness vs FrontComposer |
|---|---|---|
| **DIY with component libraries** (Fluent UI, MudBlazor) | Familiar, full control, no dependency | Repetitive boilerplate, inconsistency across microservices, no composition, no ES alignment |
| **Oqtane / ABP** | Mature, feature-rich application frameworks | CRUD-oriented, architectural mismatch with event-sourcing, breaks DDD/CQRS principles |
| **Do nothing** (MCP, CLI, APIs) | Zero frontend effort, viable for technical users | No business user experience, limits adoption to technical audiences |

### Unfair Advantage
FrontComposer owns both sides -- the backend framework (Hexalith.EventStore) and the frontend composer. This vertical integration means the depth of alignment between domain model, event store, and UI is impossible for external tools to replicate. It's not a component library -- it's a complete opinionated application framework with microservices composition built in.

### Reality Check
Even if component libraries add scaffolding features, they can only generate views from models. They can't compose microservices, understand bounded contexts, or align with event-sourcing semantics. The advantage is structural, not feature-level.

## Constraints

### Fixed Parameters (non-negotiable)
- **Tech stack:** C# / .NET / Blazor (Server + WebAssembly) / Fluent UI
- **Backend:** Hexalith.EventStore
- **Infrastructure:** DAPR for infrastructure abstraction -- no direct coupling to message brokers, state stores, or pub/sub providers
- **Deployment:** Must run on-premise, sovereign cloud, and major cloud providers
- **Architecture:** DDD / CQRS / Event Sourcing / Microservices -- no compromise on these principles
- **License:** Open-source

### Flexible Parameters
- **Timeline:** 6 months directional, not a hard deadline
- **Scope:** Can prioritize features incrementally -- MVP first
- **Brand:** No existing brand guidelines beyond Fluent UI design system
- **Community:** Solo project for now, open to contributors over time

### Key Implication
Solo project with a directional timeline means aggressive prioritization is essential. The MVP needs to demonstrate the core value (domain model as UI contract + composition shell) without trying to cover every edge case.

## Platform & Device Strategy

- **Type:** Responsive Web Application (Blazor Server + WebAssembly)
- **Device priority:** Desktop-first, responsive down to tablets/mobile
- **Primary interaction:** Mouse and keyboard
- **Accessibility:** WCAG compliance from day one -- screen readers, keyboard navigation, leveraging Fluent UI's built-in accessibility support
- **Connectivity:** Always-connected -- no offline requirement
- **Infrastructure:** DAPR abstraction layer, deployable on-premise / sovereign cloud / major cloud providers

**Rationale:** Desktop-first aligns with the primary users (developers and business users managing complex domain operations). Blazor's dual rendering mode (Server + WebAssembly) provides deployment flexibility. Always-connected is a safe assumption given the distributed microservices architecture and DAPR dependency.

## Tone of Voice

**Attributes:**

1. **Technical & precise:** Use correct domain terminology (commands, projections, aggregates). No hand-waving.
2. **Concise & direct:** Short labels, clear error messages, no unnecessary words.
3. **Confident & authoritative:** Opinionated framework, opinionated voice. Clear guidance, not hedging.
4. **Helpful without being patronizing:** Explain what happened and how to fix it. Don't dumb down, don't assume memorized docs.

**Examples:**

| Context | Don't | Do |
|---|---|---|
| Button | Submit | Send Command |
| Error | Something went wrong | Command `CreateOrder` failed: aggregate not found |
| Empty state | Nothing here | No projections registered. Add a projection to see data here. |
| Loading | Please wait... | Loading projections... |
| Success | Done! | Command dispatched successfully |

**Do's:**
- Use domain language consistently (command, projection, aggregate, event)
- Be specific in error messages -- include the entity/command that failed
- Provide actionable guidance in empty states and errors

**Don'ts:**
- Don't use vague or generic messages
- Don't be chatty or use filler words
- Don't patronize -- the audience is technical

---

**Status:** Product Brief Complete
**Next Phase:** Content & Language Strategy, then Visual Direction, Platform Requirements
**Last Updated:** 2026-04-06
