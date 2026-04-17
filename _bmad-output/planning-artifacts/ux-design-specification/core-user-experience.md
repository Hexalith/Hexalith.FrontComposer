# Core User Experience

### Defining Experience

**The Core Loop: Domain Model → Beautiful Application**

FrontComposer's defining experience is the moment a developer writes a few lines of business rules -- commands, events, projections -- and gets both a running server and a beautiful, fully functional UI. This is not a "scaffold and customize" workflow. The auto-generated application should be production-worthy at first render, not a starting point that needs polish.

**Developer core action:** Register a microservice's domain model with FrontComposer and see its UI appear in the composed application -- navigation entry, command forms, projection views, lifecycle management -- all working, all styled, all connected to the event store. This registration requires **three lines of ceremony**: (1) add the NuGet package, (2) call the registration method in `Program.cs`, (3) include the microservice in the Aspire AppHost. After that, the framework takes over. Be honest about those three lines; under-promise and over-deliver.

**Business user core action:** There is no single dominant action. Business users perform the full spectrum -- browsing lists, acting on items, submitting new commands, checking statuses, drilling into details. This means the entire auto-generated UI surface must be **uniformly competent, selectively excellent**. Every generated view is usable, styled, and production-ready. Views that carry projection role hints (`ActionQueue`, `StatusOverview`) receive additional polish and intent-driven layout. Views without hints default to clean, functional data presentations. Perfection across all surfaces is not the goal for v1; consistent competence with targeted excellence is.

### Platform Strategy

**Production deployment:** Blazor Auto (SSR → WebAssembly transition) for fast initial page loads with full client-side capability after WASM download. SignalR connections transition from server-side to browser-side automatically. This is the mode business users experience.

**Development inner loop:** Blazor Server mode by default for developers. Fast startup, instant hot reload, simple debugging, consistent SignalR behavior. Auto mode adds complexity during development (hot reload differs between SSR/WASM phases, debugging requires mode awareness). Developers work in Server mode; production deploys in Auto mode.

- **Primary interaction:** Desktop-first, mouse and keyboard. The primary users (developers configuring, business users operating) work at desks.
- **Responsive target:** Functional on tablets; usable but not optimized for mobile. Business applications built on FrontComposer are operational tools, not consumer apps.
- **Offline:** Not required. Event-sourced systems with DAPR infrastructure assume always-connected.
- **Accessibility:** WCAG 2.1 AA from day one, inherited from Fluent UI Blazor v5's built-in accessibility (ARIA, keyboard navigation, screen reader support, high contrast mode).

### Effortless Interactions

**What must feel effortless:**

1. **Microservice registration → UI appearance.** Three lines of ceremony (NuGet reference, registration call, Aspire inclusion), then the framework discovers and composes automatically. No manual route configuration, no navigation setup, no layout wiring. If a developer misses a step, they get a helpful compiler error or startup diagnostic -- not a silent failure or runtime `ServiceNotFoundException`.

2. **Command submission → visual confirmation.** A business user clicks an intent button ("Approve Order") and knows instantly that it worked. The five-state lifecycle with timeout escalation handles this silently -- the user never thinks about eventual consistency, async processing, or SignalR. They click, they see confirmation, they move on.

3. **Browsing across bounded contexts.** A business user navigates from Orders to Inventory to Customers without perceiving any boundary. The sidebar groups feel like sections of one application. Theming, interaction patterns, and data density are consistent across all microservices.

4. **Customization without friction.** When a developer needs to override an auto-generated view, the path is obvious: add one annotation for a hint, swap one template for layout changes, replace one slot for field-level customization, or replace the whole component. No documentation diving required -- the dev-mode overlay shows what to override and how.

**What should happen automatically without developer intervention:**

- Navigation entries and hierarchy from microservice registration
- Form field types inferred from C# property types (string → text, bool → toggle, DateTime → date picker, enum → select)
- Validation messages from FluentValidation rules applied to form fields
- Label resolution (display name → resource file → humanized CamelCase)
- Empty states with domain-language messages and creation actions
- Loading states per component (not full-page spinners)
- Command lifecycle management (disable on submit, re-enable on confirm)
- SignalR subscription and projection refresh on change
- ETag caching for query optimization
- Dark/light theme support following Fluent UI tokens

### Critical Success Moments

**Time to First Render: <5 minutes**

The critical success moment is not the first F5 -- it's the *decision to press F5*, which only happens if setup was painless. The onboarding experience defines adoption:

| Minute | Developer Experience |
|--------|---------------------|
| 0:00 | `dotnet new hexalith-frontcomposer` -- project template scaffolds everything |
| 1:00 | Project opens in IDE; Aspire dashboard configuration visible |
| 2:00 | Sample microservice running with domain events flowing |
| 3:00 | Browser opens; composed application with sample views visible |
| 5:00 | Developer modifies a command, sees the UI update automatically |

This "Zero to Render" timeline is the real "I'm never going back" moment -- not that the UI is beautiful (though it is), but that the entire cycle is *fast*. A project template or CLI command must scaffold the complete stack: Aspire config, sample microservice, FrontComposer registration, and sample data.

**The "this just works" moment (Business User):**

A business user opens the application for the first time and finds a clean, familiar-feeling interface (Fluent UI matches their Microsoft 365 experience). They browse a list, expand a row to see details, click "Approve," see instant feedback, and move to the next item. They never wonder "did that work?" and never see a loading spinner for more than a blink. It feels like a product built by a large frontend team, not an auto-generated application.

**The make-or-break interaction:**

The first command submission. If a business user clicks an action button and nothing visible happens for more than a second -- no animation, no state change, no feedback -- trust is broken. The five-state lifecycle with progressive visibility thresholds (nothing <300ms, pulse 300ms-2s, text 2-10s, prompt >10s) exists specifically to protect this moment.

### Experience Principles

1. **Business rules in, beautiful app out.** The framework's value is measured by how little non-domain code a developer writes. Every line of boilerplate the developer must add is a failure of convention.

2. **Uniformly competent, selectively excellent.** Every auto-generated view is usable, styled, and production-ready. Views with projection role hints receive additional polish and intent-driven layout. Consistent quality across all surfaces; targeted excellence where role hints direct it.

3. **Invisible infrastructure.** Eventual consistency, SignalR, ETag caching, microservice composition, navigation routing -- all of it must be invisible to both developers and business users. The framework handles infrastructure; humans handle domain logic and business tasks.

4. **Compiler-guided for developers.** Wrong configuration produces a helpful compiler error or startup diagnostic, not a runtime crash or silent failure. IntelliSense, type safety, and build-time checks guide the developer through setup and customization. The framework is hard to misuse.

5. **Affordance-guided for business users.** Every actionable element looks clickable. Every status is visually distinct. Empty states explain what to do next. The interface communicates its own functionality without requiring training or documentation.

6. **Fast by default, honest when slow.** The happy path feels instant. When it can't be instant (network issues, high load, complex processing), the framework communicates honestly and progressively -- never silent, never stuck.

7. **Five-minute onboarding.** From `dotnet new` to a running composed application with sample data in under 5 minutes. The getting-started experience IS the product experience for a framework.
