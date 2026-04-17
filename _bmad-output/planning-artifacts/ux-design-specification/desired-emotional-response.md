# Desired Emotional Response

### Framework Metaphor

**"Write the recipe, the kitchen builds itself."**

FrontComposer's emotional promise in one sentence. Developers write business rules (the recipe); the framework composes the entire application (the kitchen). This metaphor anchors all communication -- README, docs, conference talks, onboarding. It captures empowerment (you write the creative part), invisible infrastructure (the kitchen handles the rest), and the relationship transformation (the baker and the customer both benefit from the speed).

### Primary Emotional Goals

**Two primary principles govern all emotional design decisions:**

1. **Confusion is failure** -- the filter for every design choice
2. **Design for the relationship** -- the north star for emotional success

All other emotional goals (empowerment, familiarity, consistency, feedback, capability arrival) are supporting principles that serve these two. If confusion is eliminated and the developer-business user relationship is transformed, all other emotional goals follow.

**Developer: Empowerment**
The primary emotional payoff is not relief ("thank god I don't have to build UI") but empowerment ("now I can do more with less"). FrontComposer should make developers feel like their domain expertise has been amplified -- they're not avoiding frontend work, they're transcending it. Their business rules *become* the UI. This distinction matters: relief is passive and fades; empowerment is active and compounds.

**Business User: Invisible competence**
The application should feel so natural that business users never think about the tool itself. The emotional goal is not delight or surprise -- it's the quiet confidence of a tool that just works. They feel efficient, oriented, and in control.

**Aspirational outcome: Feeling valued.** When FrontComposer enables frequent feature updates, business users may feel valued -- "someone is building this for me, fast." This connects primarily to the *quality evolution of existing views* (views getting smarter over time through projection role hints and customization), not just the arrival of new features. Note: this emotion depends on team velocity, not just framework capability. FrontComposer *enables* it but cannot guarantee it. It is an aspirational outcome, not a design target.

**Relational: Dissolving the developer-business user tension**
FrontComposer's deepest emotional impact transforms the relationship between developers and business users from adversarial to collaborative. Today: business user asks for features, developer says "it'll take six weeks," both feel frustrated. With FrontComposer: developer writes business rules in the morning, the UI is already live. The shared moment of "wait, that's already done?" is the emotional climax -- the developer feels empowered, the business user feels valued, both feel they're finally on the same team. Proxy metric: time from "developer writes domain code" to "business user sees it in production." Target: <1 day.

**The primary emotional differentiator:** No competing Blazor framework addresses eventual consistency UX *at all*. DIY approaches and Oqtane/ABP assume synchronous CRUD. FrontComposer is the only option that designs the emotional experience of asynchronous command processing end-to-end -- the five-state lifecycle, timeout escalation, rollback messages, and reconnection handling. This is not just a design challenge; it is the core emotional value proposition against all alternatives.

**Infrastructure visibility distinction:** "Invisible infrastructure" (Experience Principle #3) applies to *runtime behavior* -- business users never see SignalR, ETag caching, or DAPR. Developers, however, should see infrastructure *when configuring* through dev-mode overlay, diagnostics, and typed contracts. Invisible to users ≠ invisible to developers. Empowerment requires understanding; understanding requires visibility at the right moments.

### Emotional Journey Mapping

**Developer Journey:**

| Stage | Emotion | Trigger | Regression Trigger |
|-------|---------|---------|-------------------|
| Discovery | Curious skepticism | "Auto-generated UI for event sourcing? That never works well." | -- |
| First run | Surprise → Empowerment | Three lines of code produce a running app with beautiful UI in <5 minutes | -- |
| Customization | Confidence | Adding one annotation upgrades a view; replacing one slot changes one field. The gradient works. | Hitting a customization cliff where the override takes hours, not minutes → drops to Skepticism |
| Production use | Trust | Auto-generated views handle edge cases. Lifecycle states protect users. ETag caching just works. | A silent failure in production (skipped field, wrong optimistic state) → drops to Skepticism |
| Advocacy | Pride | "I built this entire multi-microservice app and wrote almost no frontend code." | Framework update breaks an override contract → drops to Frustration |

**Recovery design:** Each regression trigger must have a designed recovery path. Customization cliff → dev-mode overlay explains the override path. Silent failure → auto-generation boundary protocol surfaces the issue with actionable guidance. Contract break → build-time compatibility warning before deployment.

**Business User Journey:**

| Stage | Emotion | Trigger | Regression Trigger |
|-------|---------|---------|-------------------|
| First use | Familiarity | Fluent UI looks like Microsoft 365 -- immediately comfortable | -- |
| Core tasks | Confidence | Click "Approve," see instant feedback. No ambiguity about what happened. | Command rejection after optimistic update with generic "Action failed" message → drops to Distrust |
| Daily use | Efficiency | Navigate across bounded contexts seamlessly. Find items fast via command palette. | -- |
| Week 2: Friction discovery | Patience or frustration | The micro-interactions they repeat 50 times a day must survive scrutiny. Sort, filter, search, inline action -- if any top-10 action requires >2 clicks to *begin*, frustration surfaces here. | Having to re-apply filters and navigation every session → drops to Frustration |
| Sustained use | Valued | New capabilities appear naturally in the sidebar. Silent arrival signals continuous investment. | Features stop arriving; framework's enabling effect invisible → drops to Indifference |
| Habitual use | Invisibility | The tool disappears from consciousness. It's just how work gets done. | Inconsistent new views that break established patterns → drops to Confusion |
| Error/slow | Calm trust | Sync indicator appears, explains the state, offers action. Never confused, never stuck. | -- |

**Error Emotional Sequence:** When something goes wrong (command rejection, stale data, disconnection), the target emotional progression is: **Surprise → Understanding → Trust**. Not: Surprise → Panic → Distrust. This requires: (1) immediate visible acknowledgment that something happened, (2) domain-specific explanation of what went wrong ("Approval failed: insufficient inventory"), (3) clear next action ("The order has been returned to Pending"). Generic error messages ("Action failed") break this sequence at step 2.

**Idempotent Outcome UX:** When a command is rejected but the end state matches the user's intent (e.g., User B approves an order already approved by User A), the rollback message should acknowledge the intent was fulfilled: "This order was already approved (by another user). No action needed." Emotionally: vindication, not rejection. The user wanted it approved; it's approved.

**Reconnection Reconciliation UX:** When SignalR reconnects after a gap, batch-update all stale items with a single subtle animation sweep (not individual per-row flashes). Show a brief "Reconnected -- data refreshed" toast that auto-dismisses in 3 seconds. The emotional goal during reconnection is calm resolution, not celebration.

**Schema Evolution Resilience:** When a registered projection type disappears or changes after a deployment, the framework should: (1) detect the mismatch at startup and log a clear diagnostic, (2) show an explicit "This section is being updated" message to business users instead of empty/stale data, (3) invalidate all cached ETags for the affected projection type. The emotional goal: graceful degradation during deployment transitions, not silent breakage.

### Micro-Emotions

**Critical micro-emotions to cultivate:**

| Micro-Emotion | Where It Matters | How to Create It |
|---------------|------------------|------------------|
| **Confidence** (vs. Confusion) | Every command submission, every navigation action | Five-state lifecycle feedback; clear affordances; domain-language labels |
| **Competence** (vs. Helplessness) | Developer configuring overrides; business user completing tasks | Compiler-guided setup; IntelliSense; clear empty states with next actions |
| **Orientation** (vs. Lost) | Navigating 10+ bounded contexts; returning to app after absence | Collapsible nav groups; command palette; recently visited; breadcrumbs; **user context persistence** (restore last nav, filters, sort on return) |
| **Trust** (vs. Skepticism) | First auto-generated form; first async command; first command rejection | Validation feedback inline; lifecycle states honest about timing; no silent failures; **domain-specific rollback messages** |
| **Flow** (vs. Interruption) | Business user processing a queue of items | Inline actions on list rows; expand-in-place details; no unnecessary page navigations; **session persistence preserves working context** |
| **Valued** (vs. Ignored) | Business user noticing new features arriving continuously | Silent capability arrival with data-gating and subtle "New" badge; new bounded contexts appear like new data |

**The cardinal sin: Confusion.**

Confusion is the worst emotion either user can experience. For the developer, confusion means "I don't know how to configure this" -- they'll abandon the framework for a library they understand. For the business user, confusion means "I don't know if my action worked" -- they'll double-click, navigate away, or call support. Every design decision must be filtered through: **"Does this risk confusing either audience?"**

**Objective confusion definition:** A user is confused when they (1) take an unintended action, or (2) fail to take an intended action within 5 seconds. "Had to think for a moment" is not confusion; "clicked the wrong thing" or "didn't know what to click" is.

Confusion manifests as:
- Silent failures (command submitted but no visible feedback)
- Silent omission (auto-generation skips a field without explanation)
- Ambiguous state (is this data current or stale?)
- Unclear path (how do I override this one field?)
- Magic behavior (something changed but I don't know why)
- Inconsistent patterns (this list works differently from that list)
- Surprising visual change (a view suddenly looks different because a role hint was added -- transitions between default and role-hinted views must feel evolutionary, not jarring, eased by animations)

### Auto-Generation Boundary Protocol

When the framework encounters a type it cannot auto-generate a field for (e.g., `Dictionary<string, List<Address>>`, complex nested objects, custom value types):

1. **Never silently omit.** The field must be visually present in the form.
2. **Render a visible placeholder** with the field name and type annotation.
3. **Display a clear message:** "This field requires a custom renderer. See: [link to customization gradient docs]."
4. **Emit a build-time warning** so the developer sees the gap during compilation, not at runtime.
5. **In dev-mode overlay,** highlight unsupported fields with a distinct visual indicator.

Silent omission violates Principles 1 (confidence through progressive feedback), 3 (confusion is failure), and 4 (progressive honesty) simultaneously. It is the single most likely source of developer confusion in the auto-generation system.

### Design Implications

| Emotional Goal | UX Design Approach |
|----------------|-------------------|
| **Empowerment** (developer) | Three-line ceremony, then the framework takes over. Customization gradient with typed contracts. Build-time errors that teach, not punish. Dev-mode overlay for infrastructure visibility. |
| **Invisible competence** (business user) | Fluent UI familiarity. Consistent patterns across all bounded contexts. Zero training required for basic tasks. User context persistence restores last working state on return. |
| **Valued** (business user) | New bounded contexts and capabilities arrive silently -- gated on projection data availability (don't show empty nav entries). Subtle "New" badge on first appearance, disappears after first click. No banners, no modals. Developer velocity becomes visible through the experience itself. |
| **Relational harmony** (both) | The framework's speed transforms the developer-business user dynamic. Target: <1 day from domain code to production UI. Fast iteration replaces long request-to-delivery cycles. |
| **Confidence** (both) | Every action produces visible feedback within 300ms. No silent failures. Domain language on every button and label. Domain-specific rollback messages on command rejection -- never generic "Action failed." |
| **Orientation** (both) | Command palette for instant navigation. Breadcrumbs in content area. Collapsible nav with visual indicators for active section. Session persistence: remember last nav section, filters, sort order. |
| **Flow** (business user) | Action density rules minimize page transitions. Inline expand keeps context. Lists remember scroll position and filters. Minimize **context switches** (mental model changes from leaving one view for another) -- context switches cause more friction than raw click count. |
| **Trust** (both) | Honest timing indicators. ETag freshness visible in dev mode. Compiler errors for misconfiguration. Predictable, consistent conventions. Error emotional sequence: Surprise → Understanding → Trust. |
| **Anti-confusion** (both) | Every auto-generated component behaves identically regardless of which microservice produced it. Visual transitions between view modes are animated and evolutionary, never jarring. Auto-generation boundary protocol surfaces unsupported fields visibly. No exceptions, no special cases. |

**Consistency boundaries:** Consistency is enforced for *behavior* (layout structure, spacing, interaction patterns, lifecycle states, navigation model). Consistency is *customizable* for visual identity (accent colors per bounded context, custom column renderers, projection role hints, field-level slot overrides). Consistency applies to how things work, not to every visual detail.

**Floor-before-ceiling ordering:** Uniform competence is the floor; selective excellence is the ceiling. All default views must pass the quality bar before any role-hinted views receive extra polish. In practice: fix a rendering issue in the default table before polishing the `ActionQueue` layout.

### Emotional Design Principles

**Primary Principles** (the two that subsume all others):

1. **Confusion is failure.** Any moment of confusion -- developer or business user -- is a design failure to be fixed, not a documentation gap to be covered. If a user is confused, the framework is wrong. Confusion is objectively defined: user takes an unintended action OR fails to take an intended action within 5 seconds. This is the filter for every design decision.

2. **Design for the relationship.** FrontComposer's emotional success is measured not just by how each individual feels, but by how it transforms the dynamic between developers and business users. Target metric: <1 day from domain code to production UI. The framework succeeds when both audiences feel they're on the same team. This is the north star for emotional direction.

**Supporting Principles** (serve the two primary principles):

3. **Empowerment over relief.** The framework amplifies developer capability, not compensates for developer weakness. The emotional message is "you can do more" not "you don't have to do this."

4. **Confidence through progressive feedback.** Every user action produces a visible response within 300ms. The system is never silent. When things are slow or broken, escalate communication progressively: invisible → subtle → explicit → actionable. Never jump from "everything's fine" to "something's wrong." Even "I'm working on it" is better than nothing.

5. **Familiarity as foundation.** Fluent UI is chosen not just for aesthetics but for emotional comfort -- business users already know this visual language from Microsoft 365. Don't fight it; lean into it completely.

6. **Consistency is trust.** Every auto-generated view from every microservice follows the same behavioral patterns, the same spacing, the same interaction model. Trust accumulates through predictability and breaks through inconsistency. Consistency applies to behavior and layout; visual identity details are customizable.

7. **Silent capability arrival.** New features and bounded contexts appear in the application as naturally as new data -- gated on projection data availability, marked with a subtle "New" badge on first appearance. Care expressed as absence of ceremony. Developer velocity becomes visible to business users through the experience itself, not through release notes.

### Measurable Emotional Design Requirements

1. **Minimize context switches for top-10 business user actions** (sort, filter, search, inline action, navigate, expand, submit, confirm, dismiss, return). The primary friction metric is **context switches** (mental model changes from leaving one view for another), not raw click count. A user who clicks three times within the same visual context (expand row → edit field → submit inline) experiences less friction than a user who clicks twice but navigates to a different page. Design for ≤2 clicks to *begin* any top-10 action AND zero unnecessary context switches.
2. **User context persistence** across sessions: last active navigation section, last applied filters per DataGrid, last sort order, last expanded row. On return, the user lands where they left off. This is a v1 requirement. **Architectural note:** Session persistence touches every component and requires a first-class persistence model designed into the framework architecture, not bolted on later.
3. **Domain-specific rollback messages** for every command rejection scenario. No generic "Action failed" messages. Format: "[What failed]: [Why]. [What happened to the data]." Example: "Approval failed: insufficient inventory. The order has been returned to Pending." For idempotent outcomes (rejected but intent fulfilled): acknowledge success, not failure.
4. **Auto-generation boundary visibility** for every unsupported field type. Zero silent omissions.
5. **New capability arrival gating** -- navigation entries for new bounded contexts appear only when at least one projection contains data. Subtle "New" badge on first appearance, removed after first visit.
6. **Reconnection reconciliation** -- batch-update stale items with single animation sweep on SignalR reconnect; brief auto-dismissing "Reconnected" toast. No per-row flashes.
7. **Schema evolution resilience** -- detect projection type mismatches at startup; show "This section is being updated" instead of empty/stale data; invalidate affected ETag caches.
