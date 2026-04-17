# User Journeys

*Revised after Party Mode review by John (PM), Sally (UX Designer), Winston (Architect), and Murat (Test Architect). The original six-journey set replaced Journey 6 (framework maintainer) with Journey 6 (LLM code generation agent) to make the primary success metric — LLM one-shot generation rate ≥ 80% — visible as a lived user experience rather than an abstract CI gate. Friction beats, JTBD reframings, emotional body-memory moments, and untestable "felt" language have been reconciled against party mode critique. Architectural decisions surfaced during the review are persisted in the PRD frontmatter under `architecturalDecisionsFromPartyMode` and are authoritative for later PRD steps.*

### Persona Reference

| Persona | Role | Type | Journeys | JTBD |
|---|---|---|---|---|
| **Marco** | .NET principal architect, 15yr C#, 7yr DDD/CQRS/ES | Primary: Developer | 1 (happy path), 2 (customization wall) | Ship a POC without drowning in Blazor plumbing |
| **Ayşe** | Ops lead, manages 12, daily user of legacy internal tools | Secondary: Business User | 3 (happy path), 4 (degraded network) | Close her shift at 17:00 without a phone call tomorrow |
| **Atlas** | Claude Code instance at runtime (Ayşe's delegate) | Secondary: LLM Agent (runtime) | 5 | Execute batch domain operations via MCP chat surface |
| **Coda** | Claude Code instance at build time (Marco's IDE agent) | Primary: LLM Agent (build-time) | 6 | Scaffold a new bounded context from natural language |

### Journey 1 — Marco, the .NET Architect (Primary: Developer, Happy Path)

**Persona.** Marco, 38, principal architect at a mid-sized European logistics company. Fifteen years of C#, the last seven deep in DDD, CQRS, and event sourcing. Built two event-sourced backends before — both times, the frontend consumed six weeks per microservice and drifted from the domain model within three releases. He's followed Hexalith.EventStore for six months. **His JTBD: ship a POC that proves the architecture without drowning in Blazor plumbing.**

**Opening scene.** Friday afternoon, six-week POC for a "Shipment Consolidation" bounded context (4 commands, 3 projections). His options: Blazor from scratch (too slow), ABP (architectural mismatch), or this FrontComposer thing he bookmarked. He opens the README.

**Rising action.**
- **0:00** — `dotnet new hexalith-frontcomposer` in a fresh folder. Template scaffolds the Aspire AppHost, a sample Counter microservice, the FrontComposer shell, the `dapr-components/` directory, and the generated `AppHost.cs`. *The three-line registration Marco will soon write is the ceremonial tip; the template does the real wiring.*
- **0:30–2:00** — `dotnet restore` pulls Fluent UI v5 + Hexalith packages. First-run NuGet fetch is not instant. Progress bar. Marco opens Slack, stays a little skeptical. "Huh, is this actually going to work."
- **2:00** — Restore completes. Project opens in Rider. Three files to touch: sample domain (delete), `Program.cs` (three-line registration), Aspire AppHost (one-line `.WithDomain<>`).
- **2:30** — He pastes his existing Shipment Consolidation domain (commands, events, projections, FluentValidation rules) from the backend project.
- **3:00** — F5. Aspire dashboard. DAPR sidecar green. EventStore green. FrontComposer green.
- **3:30** — Browser opens. FluentLayout shell. Teal accent. Dark mode (his OS preference). "Shipment Consolidation" bounded context in the sidebar as an auto-discovered nav group. Under it: three projection views, four command buttons.
- **4:00 — the decision point.** He clicks "Send Consolidate Shipments." The form appears. Four fields. Three are correct — ShipmentIds multi-select, EstimatedDispatchAt `FluentDatePicker`, Notes textarea. The fourth — `PriorityLevel` — renders as a text input, not the dropdown he expected. He's annoyed for two seconds, then realizes: his backend still declares `PriorityLevel` as a string, not the enum he'd been meaning to migrate it to. **The generator pointed at a drift in his domain model that he'd have shipped silently in hand-written Blazor.** He refactors to an enum (30 seconds in the backend), hot-reloads, and the form now renders a dropdown automatically.
- **4:30** — He fills in the form, clicks "Send Consolidate Shipments." Sub-200ms ring. Acknowledged. Sync pulse on the projection row. Confirmed at ~350ms (localhost dev loop — this is an SLO for the dev experience, not a user-facing product claim). The projection list updates in front of his eyes.

**Climax.** Marco stares at the screen. He just shipped a production-worthy UI for an event-sourced bounded context in **about 5 minutes on a fresh machine**, with one drift his old workflow would have missed. The framework didn't do his job; it caught an inconsistency and gave him a tool to fix it once instead of hand-maintaining the fix in two places.

**Resolution.** Monday morning, Marco emails his tech lead: *"I got the Shipment Consolidation frontend working Friday in about 10 minutes after a NuGet restore. I want to demo it to the ops team tomorrow instead of at end of quarter."* He stars the repo, files an issue about multi-accent sub-branding, and joins Discussions. Over the next three weeks he migrates their three existing event-sourced microservices and deletes ~12,000 lines of handwritten Blazor — but only after he's validated the customization gradient works for the two most opinionated views (see Journey 2).

**Emotional arc.** Curious skepticism → restore-stall patience → surprise → **caught-a-drift vindication** → advocacy.

**This journey reveals requirements for:**
- `dotnet new hexalith-frontcomposer` project template scaffolding Aspire AppHost + sample microservice + FrontComposer shell + `dapr-components/` directory
- **Precise registration ceremony honesty**: three lines in `Program.cs` for domain registration, plus generated `AppHost.cs` and `dapr-components/` from the template
- Source-generator-based auto-discovery of domain types (not runtime reflection; compile-time errors on typos)
- Auto-generation of navigation, forms, and DataGrid views from flat domain types
- Type-inferred field rendering for .NET primitives, enums, `DateTimeOffset`, collections
- **Drift detection between backend domain declarations and FrontComposer-rendered UI** — the generator makes mismatches visible rather than silently papering over them
- FluentValidation integration with inline form errors
- Five-state lifecycle wrapper visible at the web surface (click-to-confirmed dev-loop SLO ≤ 500ms on localhost)
- Hot reload compatibility for domain-level changes
- Dark/light theme with system preference detection and LocalStorage persistence
- Accessibility baseline (focus, labels, ARIA) out of the box

---

### Journey 2 — Marco Hits a Customization Wall (Primary: Developer, Edge Case / Recovery)

**Opening scene.** Two weeks into production use, Marco's ops team asks for a small tweak: on the Shipment Consolidation projection list, the `EstimatedDispatchAt` column should render as a *relative time* ("in 3 hours", "overdue by 2 hours") with a red highlight when overdue. The default FrontComposer rendering shows an ISO datetime. Marco assumed the gradient would handle this but doesn't know where to start.

**Rising action.**
- He opens the docs and searches "customize field rendering." The customization gradient page lists four levels: annotation → template → slot → full replacement. This is a slot-level override.
- The docs show a 6-line example: `[ProjectionFieldSlot(nameof(ShipmentProjection.EstimatedDispatchAt))]` on a Razor component that receives a typed `FieldSlotContext<DateTimeOffset>` (value, validation state, metadata).
- He writes the component. **He makes five mistakes** on the first draft — field-name typo, missing `aria-label`, wrong context type parameter `<DateTime>` instead of `<DateTimeOffset>`, forgotten `@inherits` directive, wrong namespace. IntelliSense silently corrects three (the typed partial types and required directives are compiler-enforced). The two that surface as warnings are the two that cannot be caught by types alone.
- **The first warning he initially disagrees with.** The Roslyn analyzer flags the missing `aria-label` as a WCAG 1.3.1 violation. Marco mutters: *"It's a status cell. Screen readers don't need it."* The analyzer's message includes a link to the WCAG criterion and a short JAWS-user scenario: a visually impaired colleague scanning a table of shipments would hear "12:30 PM" with no context for *which column*. He reads it. He agrees. He adds `aria-label="Estimated dispatch: {relativeTime}"`. The analyzer clears. *That's the trust beat — the framework stopped him kindly, and he was wrong.*
- He rebuilds. Hot reload picks up the new component. The column renders. The rest of the projection view, the DataGrid virtualization, the session-persisted filters, the accessibility contract — all unchanged.

**Climax.** Thirty minutes end-to-end, including fighting the analyzer once and losing gracefully. His git diff: 1 new file, 18 lines. His ops team sees the fix the next morning. He tweets a screenshot: *"30 min from 'that's not rendered right' to 'shipped.' And the compiler taught me something about accessibility in the process."*

**Resolution.** Marco now trusts the customization gradient enough to let his team extend FrontComposer independently. He writes an internal page: *"Override anything in FrontComposer — start with the slot gradient, climb only if you must. And read the build warnings; they're right more than you are."*

**Emotional arc.** Mild anxiety → initial disagreement with the analyzer → *pedagogical humility* → confidence → trust.

**This journey reveals requirements for:**
- Customization gradient with four levels, each bound to typed contracts
- Attribute-based discovery of custom field slots via source generator
- Typed `FieldSlotContext<T>` exposing value, validation, metadata
- Compile-time compatibility checks (field-name-to-type binding, gradient version match)
- **Roslyn analyzers that teach, not just block** — WCAG violation messages must link to the criterion AND include a concrete user scenario, not just a citation
- Hot reload compatibility for gradient overrides
- Docs that explicitly teach the "try the lowest gradient first" escalation path
- IntelliSense-driven silent correction for simple mistakes; warnings reserved for judgment calls that require reading

---

### Journey 3 — Ayşe, the Ops Lead (Secondary: Business User, Happy Path)

**Persona.** Ayşe, 31, ops lead at Marco's logistics company. Manages a team of twelve. Her daily tool is a legacy internal web app — tab-based, slow, broken form validation, full-page loading spinners. **Her JTBD is not "process consolidations." It is "close my shift at 17:00 without a 09:00 phone call from the CFO tomorrow morning because a shipment got lost."** Every pending item is a small existential threat. She's learned to triple-check the legacy app because it has silently failed on her twice in three years.

**Opening scene.** Tuesday morning. Marco sends her a link: *"Try the new shipment consolidation tool. Five minutes. Tell me if it works."* She opens it braced for the worst.

**Rising action.**
- **0:00** — She lands on home. Left sidebar shows bounded-context nav groups: *Orders, Shipments, Customers, Inventory*. The labels match her mental model, not an architecture diagram. (The labels come from explicit `[BoundedContext("Shipments")]` attributes Marco authored — the framework defaults to humanized class names, and Marco overrode three of them to match her team's vocabulary. That translation *is* a hidden journey: Marco → Ayşe via annotation, and the framework supports it without a hand-written config file.)
- **0:10** — Clicks *Shipments*. Group expands. *Shipment List* and *Consolidation Queue*. She clicks Consolidation Queue.
- **0:20** — DataGrid loads instantly. Eight rows of pending consolidations, each with a yellow "Pending" badge and an inline "Consolidate" button.
- **0:30 — the body-memory beat.** She hovers her mouse over the first Consolidate button for half a second longer than needed — a habitual "is this actually going to do something?" pause she's developed over three years of legacy-app fatigue. She clicks. Button flashes briefly. Badge transitions Pending → Consolidating → **Consolidated in under 400ms**. **Her hand is already on the next row before her brain has finished processing that the first one worked.** She stops, pauses. She doesn't *consciously* notice the absence of a loading spinner — she notices her own body got ahead of her. She exhales through her nose, a small sound she hasn't made at work in years.
- **1:00** — By the fourth row she's in flow. Click, click, click. No hesitation.
- **1:10 — the skepticism check.** At the midpoint, she refreshes the page. Not because the tool looks wrong — because her legacy-app instincts are still in her fingers and she needs to prove to herself this isn't going to disappear. Session persistence lands her back on the same view, same filters, same sort, same expanded row. She sits back 2cm in her chair. *That's* the moment she trusts it.
- **1:30** — Row six has a red badge. She expands inline. Detail: *"Cannot consolidate — shipment SH-1058 is already in batch CONS-042 scheduled for 14:30."* Domain-specific, names the other batch, names the conflicting time. She clicks "Release and Retry." Row updates. Green badge.
- **2:00** — Eight of eight processed. She moves on.

**Climax.** Ayşe has completed her first task in the new tool in under two minutes. The moments that actually landed emotionally were not "fast UI" but: (1) her hand moving before her mind, (2) the mid-session refresh that proved persistence, (3) the specific error message that named its own resolution. These are body-level trust events, not cognitive ones.

**Resolution.** She messages Marco: *"This is better than what we have. When can my team switch?"* New bounded contexts start appearing in her sidebar over the following weeks — silent capability arrival, subtle "New" badges on first appearance. She starts noticing the rate of improvement. For the first time in years, she feels someone is *building for her* instead of *handing her a tool and walking away*.

**Emotional arc.** Braced resignation → body-memory surprise → **skepticism-check validation** → confidence → valued.

**Testability note (for later non-functional requirements).** The claim "zero training, zero hesitation" is not directly testable without a human cohort. The testable proxies are: scripted Playwright task-completion fixtures measuring p50/p95 wall-clock on the 8-item workflow; navigation backtracks as a hesitation proxy (did the test selector need to retry a click because the UI wasn't ready?); a rage-click detector flagging any same-coordinate click within 400ms as a regression. The "felt" language in this journey describes *the design intent*; the success criteria translate it into measurable proxies.

**This journey reveals requirements for:**
- Bounded-context nav groups with domain-language labels via explicit `[BoundedContext(...)]` annotations (framework defaults to humanized class names; developer overrides refine)
- DataGrid with status badges, inline action column, virtualized scrolling
- Action density rules (0–1 fields → inline button; 2–4 → compact inline form; 5+ → full-page form)
- Five-state command lifecycle with progressive visibility thresholds
- Expand-in-place detail view with inline action (never full-page navigation for detail drill-in)
- **Domain-specific rollback messages that name the conflicting entity and propose a resolution** — not generic "Action failed"
- Session persistence across reloads (nav section, filters, sort, expanded row) — localStorage-based, client-scoped
- Silent capability arrival with "New" badges on first appearance
- Per-component loading states (never full-page spinners)
- **Synthetic Playwright task-completion fixtures** as the testability contract for "zero hesitation" claims

---

### Journey 4 — Ayşe on a Degraded Network (Secondary: Business User, Edge Case / Recovery)

**Opening scene.** Thursday **16:30** at a customer site. **14 pending items to clear before 17:00.** Conference-room WiFi is bad and she knows it. She also knows that if the tool fails mid-dispatch and silently loses a command, she is the one taking the 09:00 phone call tomorrow. **Stakes: go home on time, or spend her Friday morning apologizing to the warehouse.**

**Rising action.**
- First click: sync pulse extends to ~2.5s before confirming. "Still syncing…" text appears inline. Honest, not alarming. She reads it carefully. She does not double-click. Confirmed at 4s. She exhales.
- Second click: sync pulse starts. **WiFi drops entirely.** The lifecycle wrapper detects `HubConnectionState.Disconnected` and immediately shifts: the pulse is replaced with "Connection lost — unable to confirm sync status" in a warning-colored inline note. A small reconnecting indicator appears in the header.
- **The 40-second dropout — her wobble.** Ayşe does not rage-click. Her legacy-app instinct is the opposite: she backs away from the screen. She opens a second browser tab. She doesn't type anything into it — opening it is a visible act of denial. She glances back at the FrontComposer tab every five or six seconds. One thought is circling: *"Is my shipment actually consolidated, or am I about to have a bad Friday?"* She has no way to know. She keeps glancing. She does not click.
- WiFi returns. SignalR reconnects. Under the hood, the lifecycle wrapper re-subscribes to the same projection groups and fires an ETag-gated catch-up query. **Reconciliation runs as a single batched animation sweep** — three stale rows animate with one subtle pass, not fourteen frantic individual flashes. A 3-second auto-dismissing toast: *"Reconnected — data refreshed."* The warning note on row two disappears; row two is now **green (Consolidated)**. The command had landed during the outage. She didn't know. The framework knew and told her.
- She feels something shift in her chest — not relief at a feature, but relief that she will not be making a phone call tomorrow. She processes the remaining 12 items in six minutes. **Laptop closed at 17:04.**

**Climax.** The moment the framework earned its keep is not the reconnection — it's the batched sweep. Fourteen individual row flashes would have looked like chaos; one subtle sweep looks like the system knows what it's doing. That is the difference between "software that recovered" and "software she trusts."

**Resolution.** Ayşe doesn't write a Slack message this time. She just comes in Friday morning, clears the new queue at her desk instead of at a customer site, and doesn't think about the outage again. Silent resilience is the right emotional register for this journey — a tool that makes a hard moment invisible has done its job.

**Emotional arc.** Stakes awareness → mild concern (still syncing) → **honest discomfort during the dropout, wobble and denial, not panic** → relief that is specifically about her shift ending on time.

**Testability note.** Four of the behaviors in this journey are testable without network chaos:
- SignalR disconnect/reconnect: unit-level fault injection around the client (deterministic drop, delay, duplicate, reorder)
- Reconciliation batch behavior: component-level bUnit test asserting one animation sweep for N stale rows, not N individual animations
- Idempotent command outcome: property-based test (FsCheck) asserting `replay(command) == original_outcome` across randomly generated command sequences
- Integration-level: Chromium CDP `Network.emulateNetworkConditions` in Playwright for the offline toggle scripted scenario (deterministic, no toxiproxy for v1; one toxiproxy smoke test reserved for the real TCP-reset case)

**This journey reveals requirements for:**
- SignalR connection state detection via `HubConnectionState` API
- Lifecycle wrapper SignalR-loss fallback with ETag-gated polling
- Progressive visibility thresholds (invisible <300ms, pulse 300ms–2s, text 2–10s, prompt >10s)
- **Batched reconnection reconciliation** as a single animation sweep, not per-row flashes
- Auto-dismissing reconnect toast (3s)
- **Idempotent command outcome handling** — the command landed during disconnect and the framework reconciled without double-execution or user-visible duplication
- **Deterministic fault-injection wrapper around the SignalR client** as the primary test surface for this journey's behaviors (no real network in unit/component tests)

---

### Journey 5 — "Atlas," a Claude Code Agent at 4 PM (Secondary: AI Agent — Runtime)

**Opening scene.** It's **16:40**. Ayşe has already processed six consolidations manually this afternoon, and she is tired. Two more ACME shipments just came in while she was on a call with a supplier. She has 20 minutes before she wants to leave. Instead of clicking through them she opens Claude Code in her terminal and types: *"Consolidate all pending shipments for customer ACME that are overdue by more than 4 hours."*

**Rising action.**
- Claude Code (Atlas) parses the request. The FrontComposer MCP server is running in-process with the composition shell locally and was auto-discovered by Claude Code at session start. Atlas enumerates available tools scoped to the `logistics` tenant.
- **The hallucination reject.** On its first planning turn, Atlas attempts to call `ConsolidateOrderCommand.Execute` — a plausible but wrong name (the actual command is `ConsolidateShipments`). The MCP server rejects the call at the contract boundary: *"Tool `ConsolidateOrderCommand.Execute` not found for tenant `logistics`. Did you mean `ConsolidateShipments.Execute`? Available tools: [list of 12 commands with signatures and descriptions]."* **The typed contract stopped the hallucination before it touched the backend.** Atlas self-corrects on the next turn. No command was ever submitted.
- Atlas calls `ShipmentProjection.Query` with filter parameters: `Status=Pending`, `CustomerId=ACME`, `EstimatedDispatchAt<NOW-4h`. The response is a structured Markdown table rendered inline in Claude Code's terminal — three matching shipments, each with fields, a `Status` badge as a colored Markdown cell, and a machine-readable ID anchor. Claude Code surfaces the table to Ayşe so she can verify what Atlas found *before* it acts.
- Atlas calls `ConsolidateShipments.Execute` three times — one per shipment. Each call uses the **two-call lifecycle pattern**: the command tool returns immediately with `{commandId, status: "acknowledged", subscribeUri}`; a separate `lifecycle/subscribe` tool is polled for state transitions. Atlas sees "Acknowledged" within ~200ms and "Confirmed" within ~1200ms (P95 < 1500ms agent-surface read-your-writes is the committed SLO).
- One of the three returns an idempotent-outcome rollback: *"Shipment SH-1058 was already consolidated (batch CONS-042). No action taken. Projection state is consistent with intent."* The domain-specific rollback message surfaces through the chat surface identically to how it surfaces on the web.
- Atlas composes its final response: *"Consolidated 2 ACME shipments: SH-1042, SH-1061. Both confirmed within 1.2 seconds each. A third match (SH-1058) was already consolidated in batch CONS-042 — no action taken."*
- **Ayşe reads three lines instead of clicking through three consolidations.** Her secondary monitor shows the FrontComposer web DataGrid; two rows have just flipped from yellow to green on their own. **The same commands, the same backend, the same lifecycle, rendered in parallel on both surfaces in real time.** She didn't need to look. She looked anyway.
- **17:02.** Laptop closed. She's out the door.

**Climax.** Not the successful tool calls. Not the dual-surface mirror. The climax is the **hallucination rejection with a suggestion** — because that is the moment the multi-surface claim becomes a *safety* story, not just a rendering story. Agents will hallucinate. The typed contract at the MCP boundary is the fence that makes agent-calling safe enough for Ayşe to delegate without watching every tool invocation.

**Resolution.** Over the next two weeks Ayşe increasingly uses Claude Code for end-of-day batch cleanups. The web surface becomes her authoritative supervisory view. She's not delegating away her judgment — she's delegating away her clicks.

**Emotional arc (Ayşe, the human behind the agent).** Exhausted delegation → verification of Atlas's plan → relief that she will not miss dinner.

**This journey reveals requirements for:**
- **`Hexalith.FrontComposer.McpServer` hosted in-process with the composition shell** (DAPR sidecar deferred to v2)
- Auto-generated MCP tool descriptions from C# attributes via source generator — **same source** as web form labels (single source, three outputs: Razor, MCP manifest, test specimen metadata)
- Typed tool parameters with validation constraints derived from FluentValidation rules
- **Typed-contract hallucination rejection at the MCP boundary**, with a suggestion response listing the correct tool and the full tenant-scoped tool set
- Markdown renderer for projections (tables, status cards, timelines) with agent-friendly structure
- **Two-call lifecycle pattern**: command tool returns `{commandId, status: "acknowledged", subscribeUri}`; separate `lifecycle/subscribe` tool for state transitions via poll or long-poll. Intermediate states advisory; terminal states guaranteed
- Domain-specific idempotent-outcome rollback messages surfacing identically on chat and web
- Tenant-scoped tool enumeration — agents see only tools for their active tenant
- Shared typed NuGet contracts so agent tools and web forms cannot drift

---

### Journey 6 — "Coda," a Claude Code Agent at Build Time (Primary AI Agent: Code Generation)

*This journey replaces the previously drafted "Jerome the framework maintainer" journey. Per party mode critique (John, Sally): the maintainer journey belonged in contributor docs, not in user journeys; meanwhile, the framework's TOP-priority primary success metric — LLM one-shot generation rate ≥ 80% per the product brief — was nowhere in the journey set. This new Journey 6 makes that metric visible as a lived experience, not just a CI gate.*

**Persona.** "Coda" is a Claude Code instance running in Marco's Rider IDE via the FrontComposer MCP integration. Marco has pointed it at a tech lead's one-paragraph request he received five minutes ago in Slack:

> *"We need a new Supplier Invoicing bounded context. Suppliers can submit invoices with line items; we validate them against the corresponding purchase orders; they get approved or rejected. Approved invoices go into a queue for payment. I need a dashboard showing pending approvals. Can you scope it for next sprint?"*

**Opening scene.** Marco pastes the request into Claude Code and says: *"Scaffold this as a FrontComposer microservice. Follow the conventions in the existing Shipment Consolidation project."* He clicks Send and goes to make coffee. He is fully expecting to come back, find a half-broken attempt, and spend the afternoon fixing it.

**Rising action.**
- Coda reads the request. Its first action is to fetch FrontComposer's published skill corpus via MCP: `Hexalith.FrontComposer.Skills` — a set of Markdown files + attribute reference + domain-modeling conventions that ship with every framework release and are discoverable as MCP resources. Coda also reads Marco's existing `ShipmentConsolidation` project for team-specific conventions.
- Coda plans the domain: three commands (`SubmitInvoice`, `ApproveInvoice`, `RejectInvoice`), one aggregate (`Invoice`), two projections (`InvoiceSubmissionQueue` tagged `[ProjectionRole(Role.ActionQueue)]`, `PendingApprovalsDashboard` tagged `[ProjectionRole(Role.Dashboard)]`), event definitions for the full invoice lifecycle, and FluentValidation rules derived from natural-language constraints in the spec ("positive amounts", "at least one line item", "supplier ID required"). Three-line registration in the Aspire AppHost.
- Coda generates **280 lines of C# across 7 files**, following FrontComposer's naming conventions, attribute usage, and aggregate patterns exactly. **The reason it doesn't hallucinate into ambiguity is architectural**: the framework's source generator emits typed partial types for the aggregate/command/projection scaffolds, so Coda is writing *into* a compiler-checked shape rather than free-form. Convention clarity is enforced by types, not by docs.
- Coda runs `dotnet build`. **Two errors**: a missing `using` statement for `Hexalith.FrontComposer.Attributes` and a typo on one event name (`InvoiceApproved` vs `InvoiceAproved`). Coda fixes both on the next turn without prompting.
- Coda runs `dotnet test`. The basic aggregate tests it scaffolded (per the skill files, which prescribe a minimum test shape) all pass.
- Coda runs `dotnet run` against the Aspire AppHost. The composed app starts. Coda uses the runtime MCP server (the same one from Journey 5) to verify that `SupplierInvoicing` appears as a nav group and that the `InvoiceSubmissionQueue` projection is reachable via a query call. Both green.
- Coda writes a summary in the terminal.

**Climax.** Marco returns from the coffee machine **six minutes later**. Claude Code shows: *"Scaffolded Supplier Invoicing bounded context. 7 files, 280 lines. Build passed. Generated tests passed. Bounded context is live at http://localhost:8080/supplier-invoicing. Please review before committing. Notable decisions: `ApproveInvoice` returns the approved invoice DTO in the response payload; let me know if you'd prefer a void return."*

Marco reads the diff. He finds the one convention choice he disagrees with — his team standard is void returns for all commands. **He does not edit the generated code.** Instead, he edits the team's skill file (`.claude/skills/team-conventions.md`) to add *"All commands return void; command outcomes are observed via projections."* He reruns Coda on a test prompt. Coda now produces void-return commands by default. **The correction lives in the skill corpus, not in hand-patches on generated code.** Both Coda on the next task and future human developers now have the same authoritative source of truth.

**This is the primary metric, in flesh and blood**: Coda got a realistic new bounded context compiling, running, and responding on the first planning turn, with two small errors it self-corrected. **That is the 80% one-shot LLM generation rate, measured in minutes instead of benchmarks.**

**Resolution.** Marco commits the generated microservice. The PR lands the same afternoon. Two days later the ops team has a Supplier Invoicing dashboard with inline approve/reject inline actions. Marco's actual contribution to the feature was (1) pasting the tech lead's Slack message, (2) reviewing the diff, and (3) editing one skill file. **His job is taste and review, not typing.**

**Emotional arc (Marco).** Skeptical amusement ("this is not going to work") → surprise at the compile-pass → **taste-and-review mode** → pride.

**This journey reveals requirements for:**
- **`Hexalith.FrontComposer.Skills` NuGet-published skill corpus** — Markdown files + attribute references + domain-modeling conventions, discoverable as MCP resources at runtime, versioned with the framework
- **Convention clarity enforced by typed partial types** emitted from the source generator — so LLMs write *into* a compiler-checked shape rather than hallucinating free-form
- Single source generator → **three outputs**: Razor components, MCP tool manifest, test specimen metadata (Winston's architectural rule: one source, three outputs)
- Attribute-driven validation emission into both C# validators and MCP tool schemas
- **Skill-file-based framework correction pattern** — developers override framework defaults by editing the skill corpus, not by hand-patching generated code. The skill corpus is the authoritative source of truth that both humans and agents read.
- Basic generated-test scaffold per aggregate, prescribed by the skill corpus
- **The LLM benchmark suite as the operationalization of this journey**: nightly on `main` (not per-PR), pinned model versions, temperature 0, rolling 7-day median as the gate, cached prompt-response pairs, published monthly budget cap, 80% threshold with 4/20 prompts allowed to legitimately fail
- This journey is the measurable contract behind the primary success metric — if Coda cannot scaffold a realistic new bounded context on the first planning turn with small self-corrected errors, the framework has failed at its top priority.

---

### Journey Requirements Summary

The six journeys reveal eight clusters of framework capability. Each cluster maps to implementation areas that later PRD sections (domain requirements, functional requirements, non-functional requirements) will formalize.

| Capability Cluster | Revealed by Journeys | Implementation Areas |
|---|---|---|
| **Scaffolding & onboarding** | 1 | Project template, CLI, 5-minute quick-start (first-run NuGet restore included in honest timing), 3 reference microservices (Counter, Orders, OperationsDashboard), precise registration ceremony honesty (3 lines + generated template assets) |
| **Auto-generation & conventions** | 1, 3, 6 | **Source generator (not reflection)**, typed partial types for LLM convention-clarity, field type inference, label resolution chain, action density rules, projection role hints, auto-generation boundary protocol, drift detection between backend domain and rendered UI |
| **Composition shell** | 1, 3, 5 | FluentLayout, collapsible nav groups with domain-language labels via `[BoundedContext]` annotation, command palette, theme toggle, session persistence (localStorage), density preference, accessibility baseline |
| **Command lifecycle & eventual-consistency UX** | 1, 3, 4, 5 | Five-state wrapper, progressive visibility thresholds, SignalR-loss fallback with HubConnectionState detection, **batched reconnection reconciliation** (single animation sweep, not per-row flashes), **idempotent command outcome handling**, domain-specific rollback messages that name entities and propose resolutions |
| **Customization gradient** | 2 | Four-level gradient, typed contracts, compile-time compatibility checks, **Roslyn analyzers that teach** (WCAG citations + concrete user scenarios, not just rule numbers), hot reload, IntelliSense-driven silent correction for simple mistakes |
| **Multi-surface rendering (web + chat/MCP)** | 5, 6 | Rendering abstraction layer, Markdown renderer (tables, status cards, timelines), **in-process MCP server**, **two-call lifecycle pattern (acknowledge-returns + lifecycle/subscribe)**, **typed-contract hallucination rejection with suggestions and tenant-scoped tool list**, shared typed contracts, tenant-scoped tool enumeration |
| **EventStore communication** | 1, 3, 4, 5 | Command/query services, **ETag caching in localStorage scoped by tenant+user with LRU eviction**, JWT, multi-tenancy, SignalR client with fault-injection test wrapper, ULID idempotency |
| **Schema evolution (NEW — from party mode)** | *implied by all journeys, critical for v1 per Winston* | **Source-generator-emitted schema hashes**, projection version negotiation, MCP tool manifest versioning, graceful client/server degradation across deployments |
| **LLM-native code generation** | 6 | **`Hexalith.FrontComposer.Skills` corpus (NuGet-published, MCP-discoverable)**, convention clarity enforced by typed partials, attribute-driven validation emission, **skill-file-based framework correction pattern**, nightly LLM benchmark with pinned models + rolling median + published budget cap + 80% threshold allowing legitimate misses |

**Coverage check:**
- ✅ Primary user (developer) happy path — Journey 1
- ✅ Primary user (developer) edge case / recovery — Journey 2
- ✅ Secondary user (business operator) happy path — Journey 3
- ✅ Secondary user (business operator) edge case / error recovery — Journey 4
- ✅ API / integration consumer — Journey 5 (runtime LLM agent via MCP)
- ✅ **Primary metric journey (LLM build-time code generation)** — Journey 6

**Not covered deliberately:**
- **Framework maintenance / CI governance.** The previously drafted "Jerome the maintainer" journey has been removed per party mode consensus and reframed as non-functional requirements. The CI gates, specimen baseline protocol, semantic-release, and role-hint cap enforcement will be captured in later PRD sections.
- **Pure admin / RBAC configuration user.** FrontComposer inherits Keycloak/Entra/GitHub/Google auth and does not ship its own admin surface. Admin UX, if needed, is auto-generated like any other bounded context.
- **Support / troubleshooting user.** In v1 this maps to the developer persona with dev-mode overlay as a v2 accelerator.
