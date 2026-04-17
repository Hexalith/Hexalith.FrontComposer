# Defining Experience

### The Core Interaction

**"Define your domain. See your application."**

This is FrontComposer's defining interaction -- the moment that, if nailed, makes everything else follow. A developer writes business rules (commands, events, projections), registers the domain with three lines of ceremony, and a complete, beautiful, functional application appears. No frontend code. No layout configuration. No route setup.

The business user's parallel defining experience is the inverse: they open the app and complete their first task without any awareness of the underlying architecture. Browse a list, act on an item, see confirmation. The tool is invisible; the work gets done.

Both sides of this interaction must be excellent. They are not independent -- the developer experience produces the business user experience. The quality of auto-generation directly determines the quality of the business user's first impression.

### User Mental Model

**Developer mental model:** "I build domain models. Someone else builds the UI." FrontComposer replaces "someone else" with the framework itself. The developer's mental model doesn't need to change -- they continue thinking in commands, events, and projections. The framework translates their existing mental model into UI without requiring them to learn a new one.

**Key mental model alignment:** Developers already think in CQRS terms. FrontComposer's UX concepts map directly:
- Command → form (intent submission)
- Projection → list/detail view (observation)
- Aggregate → bounded context group (navigation section)
- Event → activity feed entry (v2)

This 1:1 mapping means developers never learn "FrontComposer concepts" -- they already know them by different names.

**Business user mental model:** "This is an application I use for my job." They have no awareness of microservices, bounded contexts, or event sourcing. Their mental model is task-oriented: "I need to approve orders," "I need to check inventory," "I need to find a customer." The navigation groups, command forms, and projection views must map to these tasks, not to architectural concepts.

**Current solutions and their friction:**
- **DIY Blazor:** Developer builds everything from scratch. Full control but weeks of boilerplate per microservice. UI drifts from domain model as both evolve independently.
- **Oqtane/ABP:** Scaffolding helps but imposes CRUD paradigms. Developer fights the framework when the domain doesn't fit CRUD. Business user gets inconsistent UX across modules built by different teams.
- **No frontend (CLI/API only):** Zero frontend effort but zero business user accessibility. Limits adoption to technical audiences.

### Success Criteria

| Criterion | Measurement | Target |
|-----------|-------------|--------|
| **Time to first render** | From `dotnet new` to browser showing composed app | <5 minutes |
| **Lines of non-domain code** | Code written by developer that isn't business rules | <10 lines per microservice (registration, Aspire config) |
| **Business user first-task completion** | Time from app open to completing first action (e.g., approving an order) | <30 seconds, zero training |
| **Auto-generation coverage** | Percentage of views that work without custom code for flat commands/projections | 100% for v1-scoped types (flat primitives, enums, DateTime, bool) |
| **Customization time** | Time to override one field's rendering in an otherwise auto-generated form | <5 minutes |
| **Command lifecycle confidence** | Business user successfully completes first async command without confusion | 100% (no double-clicks, no "did it work?" hesitation) |

### Novel vs. Established Patterns

**FrontComposer combines established patterns in a novel way:**

| Pattern | Established/Novel | Source |
|---------|------------------|--------|
| Sidebar navigation with groups | Established | Azure Portal, Notion, every admin panel |
| DataGrid with inline expand | Established | GitHub, Azure Portal, enterprise UX |
| Command forms from data models | Established | Scaffolding in Rails, Django, ABP |
| Command palette (Ctrl+K) | Established | GitHub, VS Code, Notion |
| Dark/light theme toggle | Established | Fluent UI, every modern app |
| **Auto-generation from CQRS domain models** | **Novel** | No existing framework does this for event-sourced systems |
| **Five-state eventual consistency lifecycle** | **Novel** | Azure Portal notifications are the closest, but not designed for CQRS |
| **Projection role hints upgrading view patterns** | **Novel** | No equivalent -- one annotation changing the entire view archetype |
| **Command+context auto-linking via aggregate metadata** | **Novel** | No framework auto-links command forms to affected projections |
| **Composition shell for microservice UI fragments** | **Novel for event sourcing** | Micro-frontends exist but none designed for CQRS/ES |

**Teaching strategy for novel patterns:** The novel patterns require zero user education because they are invisible by design. The developer doesn't learn "five-state lifecycle" -- they register a domain and the lifecycle works automatically. The business user doesn't learn "projection role hints" -- they see a well-designed view. The novel patterns are framework capabilities, not user-facing concepts. The only novel concept the developer must learn is the registration API itself -- and that's three lines of code.

### Experience Mechanics

**Developer Flow: Registration → First Render**

```
1. INITIATION
   Developer runs: dotnet new hexalith-frontcomposer
   → Template scaffolds: Aspire AppHost, sample Counter microservice,
     FrontComposer shell, all NuGet references

2. MODIFICATION (optional)
   Developer opens the sample Counter domain:
   - IncrementCounter command (1 field: Amount)
   - CounterProjection (fields: Id, Count, LastUpdated)
   Developer modifies or adds their own commands/projections

3. REGISTRATION
   In the microservice's Program.cs:
   services.AddHexalithDomain<CounterDomain>();
   In the Aspire AppHost:
   builder.AddFrontComposer().WithDomain<CounterDomain>();

4. LAUNCH
   Developer presses F5 (or dotnet run)
   → Aspire orchestrates: EventStore + DAPR sidecar + FrontComposer shell
   → Browser opens to composed application

5. FIRST RENDER
   Developer sees:
   - FluentLayout shell with header (theme toggle, app title) and sidebar
   - "Counter" nav group in sidebar (auto-discovered from domain registration)
   - "Send Increment Counter" form (auto-generated from IncrementCounter command)
     - Amount field (FluentNumberField, inferred from int type)
     - "Send Increment Counter" button (domain-language label)
   - "Counter Status" DataGrid (auto-generated from CounterProjection)
     - Columns: Id, Count, Last Updated
     - Empty state: "No counter data yet. Send your first Increment Counter command."

6. FIRST INTERACTION
   Developer enters Amount: 1, clicks "Send Increment Counter"
   → Button shows FluentProgressRing (submitting state)
   → 200ms later: button returns to normal (acknowledged)
   → Subtle sync pulse on DataGrid (syncing state)
   → 400ms later: DataGrid refreshes with new row (confirmed via SignalR)
   → Developer sees the complete loop: command → event → projection → UI update
```

**Business User Flow: Open App → Complete First Task**

```
1. INITIATION
   Business user opens browser to app URL
   → FluentLayout loads: familiar Fluent UI chrome, sidebar with domain groups
   → Session persistence restores last nav section (or home on first visit)

2. ORIENTATION
   Sidebar shows collapsible groups: Orders, Inventory, Customers...
   User clicks "Orders" → group expands to show:
   - Order List (DataGrid view)
   - Send Order (command form, if action density = full page)

3. BROWSING
   User clicks "Order List"
   → DataGrid loads with projection data
   → Status badges inline: "Pending" (amber), "Approved" (green)
   → Session-persisted filters restore: "Status = Pending" (if previously set)
   → User scans the list, sees 3 pending approvals

4. ACTING
   User sees "Approve" button inline on a Pending order row
   (action density: 0 non-derivable fields)
   → Clicks "Approve"
   → Button shows micro-animation (submitting, <200ms)
   → Row status badge transitions: "Pending" → "Approved" (optimistic update)
   → Subtle sync pulse on row (syncing)
   → 400ms later: sync pulse disappears (confirmed via SignalR)
   → User moves to next row

5. INVESTIGATING (optional)
   For a complex order, user clicks the row to expand detail view inline
   → FluentAccordion expands below the row
   → Detail fields: order lines, customer info, shipping details
   → "Modify Shipping" button appears (action density: compact inline form)
   → User modifies and submits within the expanded context
   → Row collapses back after confirmation

6. COMPLETION
   User has processed all pending approvals
   → DataGrid now shows all "Approved" status badges
   → User navigates to next task via sidebar or command palette (Ctrl+K)
```
