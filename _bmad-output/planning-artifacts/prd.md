---
keyDecisions:
  - "Multi-surface rendering from ONE event-sourced domain contract (web Blazor + Markdown chat) — Innovation 1, the moat"
  - "LLM one-shot generation ≥80% is the TOP primary metric — validated by nightly benchmark with 28-day ratchet, never advisory"
  - "8 NuGet packages lockstep-versioned for v1; collapse to 5 if release toil exceeds CI triggers (GitHub Actions >90min or git-tag-to-nuget >2hrs)"
  - "Solo-maintainer sustainability filter governs ALL scope decisions — if Jerome can't sustain it at 2am after a release for 12 months, it's out"
  - "v0.1 at week 4 proves generator + EventStore + MCP stub + 10-prompt LLM signal; cookbook page moves to week 6"
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-01b-continue
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
lastEdited: '2026-04-12'
editHistory:
  - date: '2026-04-12'
    changes: 'Post-validation edits: added v0.1 acceptance tests (5 items), added Tone & Language subsection to Developer Tool Requirements, created prd-summary-card.md companion document'
partyModeRound3Findings:
  contributors:
    - John (PM) — v0.1 too conservative, cookbook rule performative, LLM-benchmark-last is brand damage, experience-first framing for adopter acquisition
    - Amelia (Developer Agent) — v0.1 92-107 hrs floor with no buffer, cut dotnet new template → samples/Counter.sln, three-line registration is the weakest Never-Cut link, add benchmark harness to v0.1
    - Barry (Quick Flow Solo Dev) — cookbook is procrastination cosplay cut it, Never-Cut list too long shrink to 5, scoping section still too much PRD
    - Winston (Architect) — reduce generator to 1-src-1-output in v0.1, two-call MCP lifecycle stays Never-Cut (single-call breaks every ES command), 4-hour release cap unmeasurable, replace with GitHub Actions billable minutes + git-tag-to-nuget wall clock
    - Murat (Test Architect) — LLM benchmark last-resort advisory is how gates die, move to cut #3 with "gate lower never advisory" framing, first measurement at week 8 not week 12, add Pact contracts + Stryker.NET mutation + flaky-test quarantine lane to Never-Cut
  appliedDecisions:
    v01ContractRevision: "v0.1 reduces generator to 1-source-1-output (Razor only), cuts dotnet new template in favor of samples/Counter.sln in repo, adds minimal benchmarks/llm-oneshot/prompts.json + scripts/score.ps1 (10-prompt directional signal), adds hand-rolled MCP round-trip stub (1 command/1 success path/1 hallucination rejection), defers cookbook page to week 6 as acceptance test"
    llmBenchmarkPositionMoved: "LLM benchmark moved from slip cut #5 (last-resort advisory) to slip cut #3 (lower-the-number gate). Murat's reframing adopted verbatim: gate at lower threshold, never advisory. First directional measurement at week 8 not week 12. Week-12 dry-run determines v1.0 gate threshold = measured number + 5pp grace. Published trend-up commitment to ≥80% by v1.5."
    week8MeasurementTrigger: "LLM benchmark first directional measurement at week 8, not week 12. Purpose: non-gating signal. If <50% at week 8, rewrite attribute DSL while runway exists. If ≥65%, full speed. Addresses Murat's round-2 directive 'validate before committed' which week-12 violated."
    releaseWorkMetricReplaced: "4-hour release-work cap replaced with two measurable CI signals per Winston: (a) GitHub Actions billable minutes per release tag >90 min, (b) wall-clock from git tag to nuget.org live >2 hours across 3 consecutive releases. Either fires → collapse 8→5 packages."
    neverCutListRevised: "Added Pact contract tests (REST ↔ generated UI), Stryker.NET mutation testing on source generator, flaky-test quarantine lane per Murat. Removed three-line registration ceremony from Never-Cut per Winston + Amelia — moved to quality-debt with v1.1-at-latest tightening. Consolidated conventional commits + semantic release + SBOM + NuGet signing into single 'Release automation & supply chain' item per Barry. Rejected Barry's proposal to demote batched reconnection reconciliation — Winston's Innovation-2-integrity argument applies analogously."
    mvpFramingDualVoice: "Retained validated-learning primary as internal engineering frame per original draft. Added adopter-facing frame per John: README headline leads experience-first ('multi-surface UI for one event-sourced contract'), validated-learning is the engineering discipline underneath, not the public narrative."
    scopingSectionCompressed: "Compressed from ~1,900 to ~1,400 words per Barry's meta-critique. Removed duplication with §Product Scope. Growth Phase 2 and Vision Phase 3 subsections deleted since §Product Scope lines 300-321 already enumerate them. Strategic overlay retained where actionable decisions are added."
  openDisagreements:
    - "Three different answers on MCP-in-v0.1: John (yes — hand-rolled 1 round-trip + n=10 signal), Winston (no — 1-src-1-output generator only, MCP is v0.3), Amelia (no but add benchmark harness). Resolution: John's hand-rolled MCP + Amelia's benchmark harness both adopted; Winston's generator-only proposal accepted for the generator itself. v0.1 includes a SMALL MCP stub just for the signal, not the real MCP server."
    - "Barry's proposal to demote batched reconnection reconciliation to 'flicker known-issue in v1.0' was rejected. Innovation 2 (eventual-consistency UX as emotional differentiator) requires the batched sweep per Journey 4 trust contract. Applying Winston's analogous argument for two-call MCP: the reconciliation is what distinguishes the framework from 'another Blazor app with SignalR'."
    - "John's experience-first MVP reframing was adopted as dual-voice: validated-learning stays internal, experience-first becomes adopter-facing. John's point about brand-damage of public benchmark renegotiation is accommodated via Murat's 'lower gate, never advisory' — the number is honest from week 8 onward, not a last-minute renegotiation."
    - "Barry's 2-page compression target (~750 words) was partially honored. Section is now ~1,400 words, compressed from 1,900. Further compression is left to Step 11 Polish. Barry's meta-critique ('the PRD must not become the work') is retained as a footer comment in the section itself."
partyModeRound2Findings:
  contributors:
    - Winston (Architect) — package count, API stability tiering, versioning, generator budget, trim costs, missing diagnostic contract
    - Amelia (Developer Agent) — discoverability, slot ergonomics, generator black-box, missing week-one features
    - Paige (Technical Writer) — skill corpus voice collapse, teaching errors as build-enforcement, DocFX decision, Diátaxis concepts genre
    - Barry (Quick Flow Solo Dev) — anti-ceremony meta-critique, runway preservation, "PRD must not become the work"
  appliedDecisions:
    packageCountReduction: "Collapsed from 11 to 8 headline packages. Merged Generators+Analyzers→SourceTools, McpServer+Skills→Mcp. Templates spun out as standalone dotnet new registration."
    layer4ExperimentalTier: "Runtime services (Layer 4) marked [Experimental] through v1.1, stable at v1.2. Attribute surface (Layer 1) and gradient contracts (Layer 3) remain stable at v1.0."
    referenceMicroservicesCount: "Cut from 5 to 3 — Counter (minimum viable), Orders (full gradient workout), OperationsDashboard (multi-domain composition). Forms a learning arc rather than 5 shallow one-each-role demos."
    versioningModel: "Lockstep as a v1 constraint documented in README; v2 escape hatch splits into core (Contracts+Shell+SourceTools lockstep) + satellites (EventStore+Mcp+Aspire+Testing independent)."
    documentationToolchain: "DocFX (firm decision, not deferred). Dogfooding Blazor-native SSG reserved as v2 aspiration if adopter feedback justifies."
    teachingErrorsEnforcement: "Moved from discipline to compile-time enforcement. Error message template (Expected/Got/Fix/DocsLink) is part of the attribute definition and enforced by source generator test. Build won't ship without the template filled in."
    voiceCollapsePreventionInSkillCorpus: "Single source, two renderings. Explicit narrative vs reference section markers in Markdown front-matter. MCP renderer strips narrative; DocFX site keeps both."
    conceptsLayerAsFourthDocGenre: "Added Diátaxis 'explanation/concepts' genre to the docs strategy alongside tutorials/how-to/reference. Without it, developers file bug reports instead of reasoning about edge cases."
    migrationTriggerRule: "Migration guide required on any change that would make a shipped skill corpus example fail to compile — regardless of semver bucket. Minors and patches can require migration guides."
    slotErgonomicsImprovement: "Changed from [ProjectionFieldSlot(nameof(X.Field))] to [ProjectionFieldSlot<TProjection>(x => x.Field)] — generic type parameter + lambda for refactor safety."
    generatorBudgetRevision: "500ms incremental unchanged; full solution rebuild 2s → 4s for 50-aggregate reference domain. CI gates on incremental number only."
    diagnosticIdScheme: "Reserved ID ranges per package: HFC0001-0999 Contracts, 1000-1999 SourceTools, 2000-2999 Shell, 3000-3999 EventStore, 4000-4999 Mcp, 5000-5999 Aspire."
    deprecationPolicy: "One minor version minimum deprecation window. [Obsolete] message convention: '<old> is replaced by <new> in v<target>. See HFC<id> for migration. Will be removed in v<removal>.'"
    authorizationSurface: "[RequiresPolicy(policyName)] added to Layer 1 attribute surface. Integrates with standard ASP.NET Core authorization policies. Missing from first draft was an oversight."
    generatorBlackBoxFix: ".g.cs output path guarantee + dotnet hexalith dump-generated <Type> CLI for inspecting emitted code."
    highestLeverageDayOneDoc: "Customization gradient cookbook — the single page showing the same problem solved at each gradient level. Write it BEFORE the framework code. It is a design tool: if you cannot write it clearly, the gradient design is broken."
    ciGateRelocation: "Most CI gates (test coverage %, visual regression, axe-core, LLM benchmark cadence, deployment topology, SBOM, signing, Pact, mutation, BenchmarkDotNet) relocated from Step 7 to Step 10 (Non-Functional Requirements) to stop conflating consumer-visible surface with internal quality gates."
    soloMaintainerSustainabilityFilter: "Added as PRD-wide discipline. Every requirement must pass the test: 'Can a single maintainer sustain this over a 6-month v1 AND a 12-month v1.x without the CI matrix eating the time to build the actual framework?' Barry's meta-critique stands: the PRD must not become the work."
  openDisagreements:
    - "Amelia's two-tier versioning model (compile-contract lockstep + floating satellites) was NOT adopted for v1; Winston's pure lockstep with v2 escape hatch was chosen instead. Amelia's approach is preserved for v2 consideration."
    - "Barry advocated 3 packages total (vs Winston's 7 and this PRD's 8). The 8-package decision is defensible but must be validated against real solo-maintainer load in the first 3 months; contingency: collapse to 5 if maintenance load is unsustainable."
architecturalDecisionsFromPartyMode:
  mcpHosting: "in-process with composition shell in v1 (DAPR sidecar is v2 option)"
  mcpLifecyclePattern: "two-call — command tool returns {commandId, status: acknowledged, subscribeUri}; separate lifecycle/subscribe tool (poll or long-poll) for state transitions. Intermediate states advisory, terminal states guaranteed."
  autoDiscoveryMechanism: "source generators (not runtime reflection); one generator emits projection/command registry + MCP tool manifest + form metadata from same attributes; single source three outputs"
  etagPersistence: "localStorage scoped by tenant+user with size cap and LRU eviction; opportunistic cache; no cross-device sync via DAPR state; reconciliation query must be correct from full refetch if ETag missing"
  schemaEvolution: "v1-critical (not v2). Source generator emits schema hashes; MCP tool manifest versions alongside projections; UI gracefully degrades when client/server schema versions diverge."
  registrationCeremonyPrecision: "three lines in Program.cs for domain registration + generated AppHost.cs + dapr-components/ directory from template. The template does the heavy lifting; the call is the ceremonial tip."
  llmBenchmarkCadence: "nightly on main, NOT per-PR; pinned model versions; temperature 0 with fixed seed; rolling 7-day median as the gate; cached prompt-response pairs re-run only on prompt-set or target-API changes; published monthly budget cap; 80% threshold means 4/20 prompts can legitimately fail"
  typedContractHallucinationRejection: "MCP server rejects unknown tool names at the contract boundary with suggestions and tenant-scoped tool list; typed contract stops hallucinations before they reach the backend"
  testPyramid: "70% unit (xUnit+Shouldly) / 20% component (bUnit) / 8% integration (incl. SignalR fault injection, one toxiproxy smoke) / 2% E2E (Playwright, one per reference microservice + specimen + axe-core)"
  missingTestCoverage: "Pact contract tests between REST surface and generated UI (non-negotiable for a framework); Stryker.NET mutation testing on the code generator; flaky-test quarantine lane from day one; BenchmarkDotNet performance regression gate on hot path"
  degradedNetworkTestStrategy: "fault-injection wrapper around SignalR client at unit level (90% coverage); Chromium CDP Network.emulateNetworkConditions at Playwright level; single toxiproxy docker-compose smoke test for TCP reset reconnection; FsCheck property-based testing for command idempotency"
  partyModeContributors:
    - John (PM) — JTBD framing, MVP scope challenges, primary-metric visibility demand
    - Sally (UX Designer) — emotional arc critique, body-memory micro-moments, narrative friction beats
    - Winston (Architect) — five architectural decisions above
    - Murat (Test Architect) — testability triage, test pyramid, risk profile, degraded-network strategy
visionContext:
  oneLineVision: "Multi-surface UI generation framework for event-sourced microservices — one Hexalith.EventStore domain model produces a polished Blazor web shell and a Markdown chat interface, usable by humans and LLM agents alike."
  differentiators:
    - "Native event-sourcing alignment (structural) — CQRS primitives map 1:1 to UI across surfaces"
    - "Eventual-consistency UX as a first-class experience (emotional) — rendering-agnostic five-state lifecycle"
    - "AI-native UI generation (strategic) — multi-surface renderer; vertical integration across EventStore → domain model → surface is the moat"
  coreInsight: "Surface layer — domain model is already a UI contract, rendered into web (Fluent UI) and chat (Markdown) as two incarnations of one contract. Strategic layer — vertical integration across Hexalith.EventStore + multi-surface composer is the moat no external tool can replicate."
  whyNow:
    - ".NET 10 + Blazor Auto + Fluent UI Blazor v5 RC1 (Feb 2026) — mature web substrate"
    - "MCP shipped as a standard (late 2025) — stable protocol for exposing domain models as agent tools"
    - "LLM agents need structured UI, not just APIs — chat-rendered Markdown is the agent's screen"
    - "Hexalith.EventStore production-ready and needs a frontend to unlock ecosystem adoption"
    - "Frontend-for-ES gap is wide; agent-UI-for-ES gap is wider — no incumbent recognizes the latter"
    - "Competitive window: Oqtane/ABP could ship an ES story in 12-18 months but neither is positioned for agent-surface rendering"
  chatRendererTargets:
    - Hexalith (native)
    - Mistral
    - Claude Code
    - Cursor
    - Codex
  v1ScopeInterpretation: "Web surface ships in v1; chat/Markdown surface architected-for in v1 with at least one target (likely Hexalith native); full five-renderer matrix lands in v1.x / v2. Subject to confirmation in executive summary."
classification:
  projectType: developer_tool
  projectTypeSecondary: web_app
  domain: general
  domainSecondaryTag: event-sourced frontend framework / LLM-optimized conventions
  complexity: high
  complexityNote: high technical, low regulatory
  projectContext: greenfield
inputDocuments:
  - _bmad-output/A-Product-Brief/project-brief.md
  - _bmad-output/A-Product-Brief/content-language.md
  - _bmad-output/A-Product-Brief/inspiration-analysis.md
  - _bmad-output/A-Product-Brief/platform-requirements.md
  - _bmad-output/A-Product-Brief/visual-direction.md
  - _bmad-output/planning-artifacts/research/technical-fluentui-blazor-v5-research-2026-04-06.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-eventstore-front-ui-communication-research-2026-04-06.md
  - _bmad-output/planning-artifacts/research/domain-dotnet-modular-frameworks-event-sourcing-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-event-sourcing-ecosystem-adoption-trends-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-microfrontend-composition-patterns-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-model-driven-ui-generation-research-2026-04-11.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
documentCounts:
  briefs: 5
  research: 6
  uxSpecs: 1
  projectDocs: 0
  brainstorming: 0
workflowType: 'prd'
---

# Product Requirements Document - Hexalith.FrontComposer

**Author:** Jerome
**Date:** 2026-04-11

> **Readers:** Skip to §Executive Summary below. The YAML frontmatter above contains machine-readable decision history from the collaborative discovery process (3 rounds of Party Mode review with PM, Architect, Developer, Test Architect, and Quick Flow Solo Dev agents).

## Executive Summary

Hexalith.FrontComposer is a multi-surface UI generation framework for event-sourced microservices built on Hexalith.EventStore. From a single .NET domain model — commands, events, aggregates, projections — it produces two coherent, production-worthy surfaces: a polished Blazor web shell (Fluent UI Blazor v5, desktop-first, responsive) for human operators at a desk, and a Markdown chat interface usable by humans in conversational threads and by LLM agents consuming the framework as typed tools. The same CQRS primitives render in both surfaces: a command becomes a web form *or* a structured Markdown tool call; a projection becomes a FluentDataGrid *or* a Markdown status table; an aggregate becomes a sidebar nav group *or* a scoped tool namespace. Developers write business rules; the framework composes the rendered experience.

The framework targets two audiences. **.NET developers** with deep DDD/CQRS/event-sourcing expertise but shallow frontend comfort get a three-line registration ceremony (NuGet reference, `Program.cs` call, Aspire inclusion) and — within five minutes of `dotnet new hexalith-frontcomposer` — a running composed application with working navigation, auto-generated command forms, projection views, lifecycle management, and accessibility baseline. **Business users and LLM agents** encounter the same CQRS-aligned experience without needing to understand the architecture: humans browse lists, act on items, drill in without losing context, and never see silent failures or ambiguous async states; agents call commands as tools and read projections as structured Markdown, with typed contracts shared end-to-end via NuGet.

The problem FrontComposer solves is asymmetric and currently unsolved. Hexalith.EventStore — and every other event-sourced .NET backend — produces commands, events, and projections that have no coherent frontend story: Oqtane and ABP impose CRUD paradigms that break decoupling and domain boundaries; DIY Blazor means weeks of boilerplate per microservice and a UI that drifts from the evolving domain model; MCP-exposed APIs serve agents but abandon business users entirely; and no incumbent addresses the single most-cited event-sourcing frontend pain point — eventual consistency UX — as a first-class designed experience. Research confirms no existing framework treats event-sourced backends as the default assumption, and the agent-UI-for-event-sourcing gap is even wider than the human-UI gap. FrontComposer closes both.

### What Makes This Special

Three differentiators stack structurally, and they reinforce each other because the same domain model feeds all three.

**1. Native event-sourcing alignment (structural).** Every UI primitive maps to a CQRS concept — command → form/tool-call, projection → view/status-card, aggregate → nav-group/tool-namespace, event → activity stream. The customization gradient (annotation → template → slot → full replacement) binds to typed contracts, not framework internals, so developers escape conventions at any granularity without losing shell or lifecycle guarantees. The composition shell is valuable on its own — a developer who overrides every view still inherits navigation, theming, command palette, lifecycle wrapper, and accessibility baseline. Auto-generation is the accelerator on top of a standalone shell, not a scaffold-and-discard starting point.

**2. Eventual-consistency UX as a designed-for-you experience (emotional).** FrontComposer ships a rendering-agnostic five-state command lifecycle (idle → submitting → acknowledged → syncing → confirmed) with progressive visibility thresholds calibrated to human perception: invisible under 300ms, subtle sync pulse 300ms–2s, explicit "Still syncing…" text 2–10s, action prompt with refresh above 10s, graceful SignalR-loss fallback with ETag-gated polling. Reconnection reconciliation batches stale updates into a single animation sweep; command rejection produces domain-specific rollback messages ("Approval failed: insufficient inventory. The order has been returned to Pending."), never generic "Action failed." The same lifecycle contract drives both surfaces — web renders pulses and MessageBars; chat renders progressive Markdown status blocks. No other Blazor framework, and no other chat-UI framework, addresses async command UX end-to-end. It is the framework's most emotionally load-bearing differentiator.

**3. AI-native UI generation (strategic).** FrontComposer is built AI-first, and AI-first is the headline positioning — not a build-time discipline. The framework exposes the domain model as typed tools consumable by LLM agents through Hexalith's native chat surface and through Mistral, Claude Code, Cursor, and Codex. Consistent conventions, typed contracts, predictable patterns, and the MCP protocol are first-class architectural requirements so agents generate correct microservice + composition code on the first attempt and render projections faithfully in conversational context. Vertical integration across Hexalith.EventStore → domain model → multi-surface renderer is the moat: component libraries can generate forms; chat frameworks can render Markdown; only Hexalith owns both backend and composer, and only that combination produces the same CQRS-aligned experience across web and chat from one typed contract. External tools can copy features; they cannot copy the integration depth.

**Core insight (two layers).** *Surface layer:* the domain model is already a UI contract — commands are forms, projections are views, aggregates are nav groups, events are activity — and the renderer is pluggable, with web (Fluent UI) and chat (Markdown) as two incarnations of one contract. *Strategic layer:* vertical integration is the moat. Hexalith owns both the backend framework and the multi-surface composer; the depth of alignment between domain model, event store, and rendered surface — in any modality — is impossible for external tools to replicate.

**Why now.** The substrate crystallized between November 2025 and April 2026: .NET 10 shipped with Blazor Auto render modes; Fluent UI Blazor v5 RC1 landed with DefaultValues, IFluentLocalizer, FluentLayout, and the MCP Server; MCP stabilized as a cross-vendor standard consumed by Claude Code, Cursor, VS Code, Microsoft Agent Framework, and the emerging agent ecosystem; Hexalith.EventStore reached production-readiness; and the practitioner consensus hardened that LLM agents need structured UI, not just raw APIs, to be useful against complex domain systems. Competitive timing is a narrow but open window: Oqtane and ABP could ship a credible event-sourcing story in 12–18 months, but neither is architecturally positioned to add agent-surface rendering, and once a multi-surface claim is staked on the Hexalith vertical, the asymmetry is durable.

## Project Classification

| Field | Value |
|---|---|
| **Project Type** | `developer_tool` (NuGet-distributed Blazor composition framework); secondary surface: `web_app` (composed Blazor application delivered by the framework) |
| **Domain** | `general` (horizontal framework, no regulated vertical); secondary tag: *event-sourced frontend framework / LLM-optimized conventions / multi-surface rendering (web + chat)* |
| **Complexity** | `high` technical (CQRS semantics, eventual-consistency UX, multi-tenancy, DAPR, multi-surface rendering, typed customization gradient, zero-override design system with CI-enforced visual specimen verification, LLM-first architecture); `low` regulatory (WCAG 2.1 AA accessibility, open-source licensing, no PII handling at framework layer) |
| **Project Context** | `greenfield` (no existing FrontComposer code; Hexalith.EventStore present as external git submodule dependency, not the project under specification) |

## Success Criteria

### User Success

**Developer (primary user — .NET / DDD / ES practitioner)**

- **Time-to-first-render ≤ 5 minutes** from `dotnet new hexalith-frontcomposer` to a running composed application with sample microservice, navigation, forms, DataGrid, and working command lifecycle in the browser. Measured as median across a cold-machine onboarding cohort.
- **Non-domain code per microservice ≤ 10 lines** (NuGet reference, `Program.cs` registration, Aspire inclusion, plus minor configuration). Measured across the three reference microservices shipped with the framework (Counter, Orders, OperationsDashboard).
- **Customization time ≤ 5 minutes** to override one field's rendering in an otherwise auto-generated form via the customization gradient (annotation, template, or slot). Measured from gradient docs benchmark.
- **LLM one-shot generation rate ≥ 80%** — given a FrontComposer-aware system prompt, Claude Code / Cursor / Codex can author a new microservice (command + projection + registration) that compiles, runs, and composes correctly on the first attempt in at least 8 of 10 benchmark prompts. This is the framework's **top-priority primary metric** per the product brief.
- **Customization-cliff zero events** — no reported case during the first 6 months of adoption where a developer had to fork, patch, or inject CSS to achieve a reasonable customization target. Silent failure to provide an escape hatch is a framework bug.

**Business user (secondary — operator of the composed web application)**

- **First-task completion < 30 seconds, zero training** — a new business user opens the app, identifies a pending item, completes a domain action (e.g., "Approve Order"), and moves to the next item without reading help text or calling support.
- **Command lifecycle confidence 100%** — zero observed double-submits, zero observed "did it work?" hesitations in user research sessions. Measured against the five-state lifecycle under both happy-path and degraded network conditions.
- **Context-switch budget for top-10 actions ≤ 2 clicks to *begin*, zero unnecessary navigations** — per the UX spec's measurable emotional requirement. Measured by UX task timings.
- **Session resumption delight** — on return, the user lands on their last navigation section with last filters, sort order, and expanded row restored. Measured as a yes/no checklist item per reference microservice.

**LLM agent (secondary — consumer via chat/Markdown surface and MCP)**

- **Tool-call correctness ≥ 95%** — given a FrontComposer-registered domain exposed through Hexalith's native chat surface, an agent can list commands, submit them with valid payloads, and read projection status from the Markdown renderer in at least 19 of 20 benchmark interactions.
- **Agent-surface read-your-writes latency P95 < 1500ms** — from agent-issued command to agent-observable projection update via the Markdown chat surface (includes SignalR + ETag + render path).

### Business Success

**6-month targets (v1 ship + initial adoption)**

- **Open-source release of FrontComposer v1** on NuGet + GitHub with working quick-start, documented customization gradient, and the composed shell ready to host Hexalith.EventStore microservices.
- **First three external adopters** — individual developers or small teams outside Hexalith who ship a production microservice composed by FrontComposer within 6 months of v1 release. Measured by public repos, Discord/Slack self-reports, or issue-tracker traces.
- **At least one reference microservice per bounded-context archetype** — flat-command + list projection, multi-field command with validation, action-queue projection role hint, detail-record projection role hint, status-overview projection role hint. These double as the onboarding sample set and as LLM training exemplars.
- **Community health signals** — ≥20 GitHub stars, ≥5 issues filed (indicates someone tried it), ≥3 external contributions (PRs, issues, discussions). Small-number targets reflect solo-maintainer realism, not ambition cap.

**12-month targets (v1.x growth + chat surface maturity)**

- **Full five-renderer chat matrix operational** — Hexalith, Mistral, Claude Code, Cursor, Codex all able to discover and interact with a FrontComposer domain via MCP + Markdown. At least one demo per renderer, committed to the docs site.
- **Architect-reported adoption of at least one Hexalith.EventStore + FrontComposer stack** in a team or organization that Jerome does not personally know. Proxy: a public blog post, conference talk, or sponsored feature.
- **LLM-generation rate ≥ 90%** on the expanded benchmark suite (50+ prompts covering realistic domain patterns, not just the v1 reference set).

**18-month vision (competitive durability)**

- **The multi-surface claim is staked and defensible** — FrontComposer is named in practitioner discussion of "event-sourced frontend" AND "AI-agent UI for .NET" as the reference implementation. No incumbent (Oqtane, ABP, Piral) has shipped a comparable multi-surface story.

### Technical Success

**v1 quality gates (non-negotiable for ship)**

- **Unit test coverage ≥ 80%** on framework code (Hexalith.FrontComposer.* assemblies), using xUnit + Shouldly per the platform requirements.
- **Component test coverage ≥ 70%** on auto-generated Razor components, using bUnit.
- **E2E test coverage of the three reference microservices** (Counter, Orders, OperationsDashboard) using Playwright, including happy path + SignalR disconnect/reconnect scenarios + command rejection rollback + schema evolution resilience.
- **CI-enforced type specimen verification** — the visual regression baseline committed per the UX spec, rendered across theme (Light/Dark) × density (Compact/Comfortable/Roomy) × language direction (LTR; RTL in v2), fails merge on unexplained drift.
- **CI-enforced accessibility gates** — `axe-core` via Playwright fails build on "serious" or "critical" violations; manual screen-reader verification (NVDA, JAWS, VoiceOver) logged per release.
- **WCAG 2.1 AA conformance** on all auto-generated output, verified against the specimen view and the 14 commitments in the UX spec.
- **Command-to-UI-update latency P95 < 800ms (cold actor), P50 < 400ms (warm actor)** in the web surface, from button click to confirmed state in the UI (Blazor Server, localhost Aspire topology). Cold = first command after app restart; warm = subsequent commands within same session. Single-aggregate command, single projection update. See §Non-Functional Requirements for full measurement methodology.
- **LLM code generation benchmark suite** — automated CI job that runs 20 representative prompts through Claude Code / Cursor / Codex against the current framework version and fails if correctness drops below threshold. This is the mechanism that operationalizes the LLM-first priority.
- **Deployment topology validation** — framework runs identically on-premise (bare Aspire), sovereign cloud (generic Kubernetes), and major cloud providers (Azure Container Apps, AWS ECS/EKS, GCP Cloud Run). CI includes at least Azure Container Apps and a local Kubernetes target.
- **Zero direct coupling to non-DAPR infrastructure** — automated check asserting no direct references to Redis/Kafka/Postgres/CosmosDB from framework code. All infrastructure concerns flow through DAPR components.

### Measurable Outcomes

| Outcome | Metric | Target (v1 ship) | Target (12 months) |
|---|---|---|---|
| **Onboarding speed** | Median time from `dotnet new` to running composed app | ≤ 5 min | ≤ 3 min |
| **Code ceremony** | Lines of non-domain code per microservice | ≤ 10 | ≤ 5 |
| **LLM one-shot generation** | Benchmark pass rate (20 prompts v1, 50 prompts v1.x) | ≥ 80% | ≥ 90% |
| **Customization speed** | Median time to override a single field via gradient | ≤ 5 min | ≤ 2 min |
| **Business user first-task** | Time to complete first action, zero training | < 30 s | < 20 s |
| **Command lifecycle latency** | P95 click → confirmed state (web, cold actor) | < 800 ms | < 500 ms |
| **Agent-surface read-your-writes** | P95 command → projection via Markdown chat | < 1500 ms | < 800 ms |
| **Test coverage** | Unit / component / E2E | 80 / 70 / 3 refs | 85 / 80 / 5+ refs |
| **Accessibility** | WCAG 2.1 AA conformance on specimen | 100% | 100% |
| **Chat renderer coverage** | Targets with working demo | 1 (Hexalith) | 5 (all) |
| **External adopters** | Independent production users | 3 | 15 |
| **Community signals** | GitHub stars / PRs / discussions | 20 / 3 / 10 | 200 / 20 / 50 |
| **Competitive durability** | Incumbents shipping comparable multi-surface ES story | 0 | 0 |

## Product Scope

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

## User Journeys

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

## Domain-Specific Requirements

### Framework-as-Foundation Constraints

Hexalith.FrontComposer is classified in the `general` domain because it is **horizontal infrastructure**: a framework for building event-sourced UIs across any vertical. It is not tied to healthcare, fintech, govtech, energy, or any other regulated sector. This positioning is a deliberate architectural commitment, not an absence of ambition — and it carries specific design rules that all later PRD sections must honor.

**Why horizontal:** Adopters will use FrontComposer to build UIs for domains FrontComposer itself has no expertise in — clinical trial management (HIPAA), payment processing (PCI-DSS), grid SCADA (NERC CIP), legal discovery (ABA ethics), aerospace telemetry (ITAR/DO-178C). The framework's job is to make those verticals *possible*, not to pre-bake them. A framework that assumes a vertical embeds assumptions that rule out every other vertical.

**Architectural commitments that preserve vertical-neutrality:**

| Commitment | Why it preserves vertical-neutrality |
|---|---|
| **No PII at the framework layer** | FrontComposer persists only UI preference state (theme, density, nav, filters) in client `localStorage`. All business data — including any regulated personal, health, financial, or classified data — lives in the adopter's microservices and Hexalith.EventStore. The framework never touches it. |
| **DAPR-only infrastructure coupling** | All infrastructure concerns (state store, pub/sub, secrets, config, observability) route through DAPR component bindings. Adopters swap DAPR components to meet their regulatory backend requirements (FIPS-validated crypto, in-country state stores, air-gapped pub/sub) without touching framework code. |
| **Shared typed contracts via NuGet, not schemas-on-the-wire** | Domain contracts travel as compiled C# types. There is no JSON schema surface the framework controls, so compliance-mandated data shapes (e.g., HL7 FHIR resources, ISO 20022 payment messages) are owned entirely by adopter microservices. |
| **Zero client-side business logic in auto-generated components** | Command validation, authorization, and business rules are enforced server-side by the adopter's microservices via FluentValidation + DAPR actor-based aggregates. The framework never client-validates anything that would need to also be validated server-side, and it never makes authorization decisions. Client-side ETag cache is hint-only; correctness comes from server queries. |
| **WCAG 2.1 AA baseline, not just aspiration** | Regulated verticals (govtech under Section 508, education under ADA, European public sector under EAA) require verifiable accessibility. FrontComposer enforces the baseline in CI (axe-core + specimen verification + manual screen-reader verification) so adopters inherit conformance rather than having to prove it themselves for framework-generated UI. |
| **Keycloak + Entra + GitHub + Google auth compatibility, no custom auth UI** | FrontComposer does not ship its own identity UI. Adopters bring their regulated IdP (e.g., Entra with Conditional Access policies, Keycloak in a customer's private tenant) and FrontComposer integrates via standard OIDC/SAML flows. |
| **Self-hostable on-premise, sovereign cloud, and major cloud providers** | No vendor lock-in at any layer. Adopters in data-residency-constrained verticals (public sector, healthcare, defense) can deploy the entire stack in-country on their chosen infrastructure. |

**Explicit non-commitments — what FrontComposer will NOT ship (adopter microservice responsibility):**

- **No vertical-specific audit logging primitives.** HIPAA §164.312(b), SOX audit trails, PCI DSS Requirement 10 — all are adopter responsibility. FrontComposer's command/event history is observable via the event store; shaping it into vertical-compliant audit records is the adopter's job.
- **No regulated-data classification, tagging, or DLP integration.** Fields marked as PHI, PII, PCI, CUI, or export-controlled are adopter-domain concerns. FrontComposer renders whatever types the domain declares.
- **No vertical-specific consent or retention management.** GDPR Article 17 right-to-erasure, CCPA opt-out, HIPAA authorization workflows — all are adopter responsibility. The framework's crypto-shredding-awareness story (referenced in the ES ecosystem research) is a v1.x / v2 enhancement, not v1 scope, and even when delivered it will be a cache-invalidation hook rather than a consent engine.
- **No vertical-specific data validation libraries.** FluentValidation handles structure; domain-semantic validation (valid ICD-10 code, valid ISO 4217 currency, valid FAR clause reference) is adopter responsibility.
- **No built-in encryption at rest beyond what DAPR state stores provide.** Adopter choice of DAPR state store determines encryption-at-rest posture; FIPS-validated backends, HSM-backed stores, etc. are configuration choices, not framework features.

**The horizontal/vertical boundary rule:** If a feature would help adopters in one vertical but be inappropriate, unused, or confusing in another, it belongs in a vertical-specific extension package (e.g., a hypothetical `Hexalith.FrontComposer.Healthcare` community module), not in the core framework. Future PRD sections must filter candidate features against this rule before admitting them to v1 scope. **A framework that is 80% horizontal and 20% vertical is neither — it is a horizontal framework with confusing baggage.**

**Implication for later PRD sections:** Functional and non-functional requirements must be written in vertical-neutral language. No requirement may assume a specific compliance regime, data type, or regulatory workflow. The test for inclusion is: *"Would an adopter building a healthcare EHR and an adopter building a fintech payment rail both need this, or only one of them?"* Features that help only one belong outside the core framework.

## Innovation & Novel Patterns

### Detected Innovation Areas

FrontComposer's novelty clusters in four distinct areas, each with a different risk profile and different validation path. These are triaged in descending order of *how load-bearing the innovation is to the framework's value proposition*.

#### Innovation 1 — Multi-Surface UI Generation from One Event-Sourced Domain Contract (Structural)

**What's new.** A single .NET domain model — commands, events, aggregates, projections — is the source for *two rendered surfaces simultaneously*: a Blazor web shell for human operators AND a Markdown chat interface for humans and LLM agents via MCP. Both surfaces share the same typed contracts, the same five-state command lifecycle, the same domain-specific rollback messages, and the same backend. There is no second source of truth, no duplicated rendering logic, no parallel "agent API" running alongside a "web API."

**What existed before.** Component libraries generate forms from data models (Radzen, MudBlazor scaffolding). Chat frameworks render Markdown from tool calls (Vercel AI SDK, LangChain). Event-sourcing libraries provide CQRS primitives (Marten, Wolverine, Axon). **No existing framework combines all three — typed domain contract → multi-surface rendering → event-sourced backend — in a single coherent system.** The ES ecosystem research explicitly confirmed the gap: "no incumbent even recognizes the agent-UI-for-ES problem."

**Novelty rating.** **High / structural.** This is the headline innovation of the framework. Component libraries cannot retrofit it because they don't own the backend; chat frameworks cannot retrofit it because they don't own the domain model; ES libraries cannot retrofit it because they don't own the UI layer. Vertical integration across Hexalith.EventStore + FrontComposer is the architectural moat.

#### Innovation 2 — Eventual-Consistency UX as a Rendering-Agnostic First-Class Design Contract (Experiential)

**What's new.** The five-state command lifecycle (idle → submitting → acknowledged → syncing → confirmed) with progressive visibility thresholds (invisible <300ms, pulse 300ms–2s, text 2–10s, action prompt >10s, SignalR-loss fallback with ETag-gated polling) is a **rendering-agnostic contract**: the same lifecycle semantics surface as Fluent UI pulses on the web surface and as progressive Markdown status blocks on the chat surface. Reconnection reconciliation batches stale updates into a single animation sweep (not per-row flashes). Domain-specific rollback messages ("Approval failed: insufficient inventory. The order has been returned to Pending.") replace generic error messages on both surfaces.

**What existed before.** Azure Portal's notification bell is the closest existing pattern — a global async operation tracker. Redux-style optimistic update/rollback is common in SPAs. But **no existing framework treats eventual-consistency UX as a first-class designed experience end-to-end**: the ES ecosystem research documented 12 practitioner-reported frontend pain points, with eventual-consistency UX as the single most-cited, and confirmed that no library solves it comprehensively. Oqtane and ABP assume synchronous CRUD. DIY Blazor means hand-building this per project, which teams don't.

**Novelty rating.** **High / experiential.** This is the innovation that *keeps* users after the multi-surface innovation *attracts* them. It is also the most emotionally load-bearing: it is the reason a business user like Ayşe (Journey 3/4) trusts the framework through a 40-second WiFi dropout without rage-clicking or losing work.

#### Innovation 3 — LLM-Native Code Generation via Typed Partial Types + Skill Corpus + Hallucination Rejection (Strategic / AI-Native)

**What's new.** FrontComposer is built so that an LLM agent like Claude Code can scaffold a new event-sourced bounded context from a natural-language specification on the first planning turn with ≥ 80% one-shot correctness. This is achieved through four reinforcing architectural decisions:

1. **Source-generator-emitted typed partial types** for every aggregate / command / projection scaffold. LLMs write *into* a compiler-checked shape rather than free-form C#, so they cannot hallucinate into ambiguity.
2. **`Hexalith.FrontComposer.Skills` — a NuGet-published, MCP-discoverable skill corpus** (Markdown + attribute references + convention patterns) that ships with every framework release. LLMs consume it directly as MCP resources; humans consume it as docs. One source of truth, read by both audiences.
3. **Typed-contract hallucination rejection at the MCP boundary.** When an agent calls a tool name that doesn't exist (e.g., `ConsolidateOrderCommand` instead of `ConsolidateShipments`), the MCP server rejects it at the boundary with a suggestion and the full tenant-scoped tool list. The command never reaches the backend; the agent self-corrects on the next turn. **The typed contract stops hallucinations at the fence, not at the wreckage.**
4. **Skill-file-based framework correction pattern.** When a developer disagrees with a framework default (e.g., "our team wants void command returns"), the correction lives in the skill corpus, not in hand-patches on generated code. Both humans and agents read the corrected skill file on the next iteration. This is a *teachable framework*, not a configurable one.

**What existed before.** LLMs can generate code from docs and examples — Cursor, Claude Code, Copilot all do this. Source generators exist in the .NET ecosystem (Roslyn generators). MCP is a standard protocol. **What does not exist is the integration of all four patterns in a framework where LLM correctness is a first-class architectural commitment backed by a CI benchmark gate.** The model-driven UI research confirmed that no existing .NET/Blazor framework occupies the "schema-first, spec-driven, LLM-friendly UI composition" intersection.

**Novelty rating.** **High / strategic.** This is the innovation that operationalizes the #1 success metric (LLM one-shot generation rate ≥ 80%). Without this set of patterns, the primary metric is aspirational. With it, the primary metric becomes a testable contract validated nightly in CI.

#### Innovation 4 — Convention-DSL via Typed Attributes Rather than a Separate Spec Language (Tactical)

**What's new.** Rather than creating a new declarative language for UI specification (as Kiro with `spec.md` + EARS format, or JSON Forms with UI Schema, or React Server Components with server directives), FrontComposer uses **C# attributes as the declarative DSL**: `[ProjectionRole(Role.ActionQueue)]`, `[ProjectionBadge(BadgeSlot.Warning)]`, `[ProjectionFieldSlot(nameof(X))]`, `[BoundedContext("Shipments")]`. The "language" is discoverable via IntelliSense, validated by the compiler, versioned by NuGet, and read by LLMs via the same attribute reflection that drives the web surface. **The DSL is the type system.**

**What existed before.** JSON Schema-driven form generators exist (JSON Forms, RJSF). YAML/Markdown spec formats exist (Kiro's EARS, OpenAPI, GraphQL SDL). DSLs built on Roslyn exist (Marten's method-based API). **What does not exist is the deliberate decision to treat typed C# attributes + source-generated partials as the primary declarative surface for an event-sourced UI framework, with the goal of compile-time correctness for both humans and LLMs.**

**Novelty rating.** **Medium / tactical.** This is a quieter innovation — it's a deliberate *non-invention* of a new language in favor of leveraging what C# already provides. Its value is conservative but load-bearing: without it, innovations 1, 2, and 3 would require a parallel spec format that would drift from the code.

### Market Context & Competitive Landscape

The four innovations above are validated against current competitive and practitioner-literature research commissioned specifically for this PRD. Key findings from the four domain research documents already loaded into the input set:

**From `domain-microfrontend-composition-patterns-research-2026-04-11.md`:**
> *"MFE in 2026 is mature on the JavaScript side and structurally thin on the .NET side. Microsoft ships the primitives (Blazor render modes, YARP, .NET Aspire, Blazor Custom Elements) but deliberately does not ship a shell/host framework — leaving that exact gap for Hexalith.FrontComposer to fill."*

**From `domain-dotnet-modular-frameworks-event-sourcing-research-2026-04-11.md`:**
> *"No incumbent framework treats an event-sourced backend as the default assumption. All four frameworks examined [Oqtane, ABP, Piral, Module Federation 2.0] assume (or default to) CRUD/EF Core-style persistence. Confidence: high — confirmed across all sources."*

**From `domain-event-sourcing-ecosystem-adoption-trends-research-2026-04-11.md`:**
> *"The central strategic finding for the Hexalith.FrontComposer project is that the frontend layer is the single most open sub-segment of the ES ecosystem. Commercial vendors sell databases. OSS libraries sell domain models. Frontend frameworks assume CRUD + REST. No one is responsible for the projection-to-UI binding, the correlation between a user's command and the real-time projection update it triggers, or the schema-drift that leaks silently into views when events evolve. The first credible ES-aware frontend framework would face almost no incumbents."*

**From `domain-model-driven-ui-generation-research-2026-04-11.md`:**
> *"The .NET/Blazor ecosystem has a clear, unoccupied gap for a schema-first, spec-driven, LLM-friendly UI composition layer. No incumbent (ABP Framework, Radzen, MudBlazor, Power Apps) currently occupies that intersection."*

**Combined market signal.** Four independent research documents, each targeting a different slice of the competitive landscape (micro-frontend composition, .NET modular frameworks, ES ecosystem, model-driven UI generation), converge on the same finding: **the intersection where FrontComposer positions is unoccupied, the timing window is April 2026 forward, and the vertical-integration advantage (owning both Hexalith.EventStore and FrontComposer) is structurally durable against incumbent response for at least 12–18 months.**

**Competitive response modeling:**

| Incumbent | Likely response to FrontComposer's innovations | Timeline | Severity |
|---|---|---|---|
| **Oqtane** | Could add ES semantics as a module; modular monolith culture resists adopting an opinionated ES framework wholesale | 12–24 months, if at all | Low — CMS-centric adoption base, not ES-aligned |
| **ABP Framework** | Already resists CQRS/ES at the core (GitHub issue #57 unresolved since 2018); could add an ES extension module; very unlikely to ship multi-surface chat rendering | 18–36 months if they try; may not try | Medium — largest .NET modular framework commercially, but paradigm mismatch is deep |
| **Fluent UI Blazor v5** | Microsoft ships primitives, not a framework; MCP Server shows they're paying attention to AI-native tooling; unlikely to build the shell itself | Microsoft does not compete in this space | Low — Microsoft is a supplier, not a competitor |
| **Piral** | Could add Blazor support at depth, but React/TS-first culture and no ES alignment make this a non-threat | Unlikely | Low |
| **Kurrent / AxonIQ** | Own the backend story; no frontend composition play; could partner with FrontComposer rather than compete | N/A | None (potential partners) |
| **New entrant** | Most likely threat: a well-funded startup building an "AI-native event-sourced frontend" from scratch | 12–24 months | Medium — watch for funding signals in this niche |

### Validation Approach

Each of the four innovations has a distinct validation path. These are *not* the same as the Success Criteria's CI-gate measurements — these are the *innovation-specific* validation commitments that prove the novel claims are real.

**Innovation 1 (Multi-Surface) validation:**

- **Reference implementation**: Three reference microservices (Counter, Orders, OperationsDashboard — per Success Criteria) must each render correctly on *both* the web surface AND the Hexalith native chat surface from the same domain code. This is demonstrated end-to-end in the v1 quick-start.
- **Cross-surface consistency test**: A scripted test issues the same command via the web surface and via the MCP chat surface, asserts both surfaces transition through the same lifecycle states, surface the same rollback messages on rejection, and update the backing projection to the same state.
- **Dual-surface demo video**: A short (3–5 min) demo recording for the documentation site showing a command flowing through both surfaces in real time.
- **Failure mode**: If users adopting FrontComposer only use the web surface and ignore the chat surface, or vice versa, the multi-surface innovation is under-validated. This is a v1.x adoption-tracking concern.

**Innovation 2 (Eventual-Consistency UX) validation:**

- **Degraded-network test suite** (per party mode testing strategy): SignalR fault injection at unit level for 90% coverage; Chromium CDP `Network.emulateNetworkConditions` at Playwright level for scripted offline scenarios; single toxiproxy smoke test for real TCP reset. This is the most rigorously testable of the four innovations.
- **Scripted Playwright task-completion fixtures** measuring p50/p95 wall-clock, navigation backtracks (hesitation proxy), rage-click detection (same-coordinate click within 400ms = regression).
- **Property-based testing** (FsCheck) for command idempotency: generate random command sequences, assert `replay(command) == original_outcome`.
- **User research sessions** (post-v1, not gating): cohort of 5–10 business users from at least two independent adopters observed completing tasks during simulated network instability. This is qualitative confirmation of the quantitative measurements.

**Innovation 3 (LLM-Native Generation) validation:**

- **Nightly LLM benchmark suite** (per party mode / Murat's cadence): 20 prompts through pinned versions of Claude Code / Cursor / Codex, temperature 0, fixed seed, rolling 7-day median against the ≥80% one-shot threshold, cached prompt-response pairs, published monthly budget cap. Benchmark is a CI gate on `main` (not per-PR) so flakiness doesn't block daily development.
- **Live-reference generation**: The v1 quick-start includes at least one "scaffold a new bounded context" task performed by an LLM agent on a fresh machine, recorded as evidence that the innovation works in the wild, not just in CI.
- **Hallucination-rejection unit tests**: Deterministic tests at the MCP server layer asserting that invalid tool names return suggestion responses with the correct tenant-scoped tool list, not 500 errors or silent failures.
- **Skill corpus versioning test**: Assert that a framework version bump that changes a convention produces a versioned skill corpus update that LLMs can consume, with no silent drift.

**Innovation 4 (Convention-DSL via Typed Attributes) validation:**

- **IntelliSense completeness check**: Every attribute in the DSL surface must produce correct IntelliSense completion, hover docs, and usage examples in Visual Studio / Rider / VS Code.
- **Compile-time error quality audit**: Every misuse of a framework attribute (wrong parameters, missing required metadata, invalid enum values, typos on member references) must produce a build error that *teaches* rather than just fails. This is verified manually for a representative set of 20 common misuse patterns before each release.
- **Source-generator round-trip test**: The same attribute declaration must produce consistent outputs across Razor, MCP manifest, and test specimen metadata. Divergence is a regression.

### Risk Mitigation

The four innovations have distinct risk profiles. Mitigations below are specific to the innovation, not generic project risks.

**Innovation 1 — Multi-Surface risk:**

- **Risk**: The chat surface is under-valued by early adopters and slows web-surface polish. Jerome spends effort on a surface no one uses.
- **Mitigation**: Ship Hexalith's native chat surface as the v1 reference with *one* compelling demo. Defer the other four chat renderers (Mistral, Claude Code runtime, Cursor, Codex) to v1.x based on actual adoption signals. If chat surface has fewer than 20 independent users by month 9, reassess whether to continue investing or to reframe the chat surface as v2.
- **Fallback**: If multi-surface fails entirely as a value proposition, FrontComposer is still a credible standalone event-sourced Blazor framework — innovations 2, 3, 4 stand on their own.

**Innovation 2 — Eventual-Consistency UX risk:**

- **Risk**: The five-state lifecycle is over-engineered for simple use cases and the progressive thresholds annoy users whose backend is fast enough that the machinery is invisible work.
- **Mitigation**: Thresholds are invisible on the happy path by design (pulse only appears >300ms). Zero user-facing configuration required. Opt-out is not needed because the lifecycle is invisible when it isn't needed.
- **Fallback**: If users report the syncing text as distracting in specific verticals, convert the 2–10s "Still syncing..." text to an opt-out via density preference or user settings. No structural change required.

**Innovation 3 — LLM-Native Generation risk:** (HIGHEST RISK)

- **Risk 1**: LLM benchmarks are flaky and the gate gets disabled within 3 months (Murat's explicit warning from party mode).
- **Mitigation 1**: Nightly, not per-PR. Pinned models. Fixed seeds where available. Rolling 7-day median, not single-run. Published budget cap. Cached prompt-response pairs. 4/20 legitimate misses allowed. This is the party-mode-validated discipline; it must be followed from day one.
- **Risk 2**: Frontier models move faster than FrontComposer versions, and a model update breaks the benchmark suddenly.
- **Mitigation 2**: Pin model versions explicitly (e.g., `claude-sonnet-4-6-2026-xx`, not `latest`). Model version upgrades are a deliberate, scheduled, documented event. Run both the old and new model for 1 week during transitions to catch regressions.
- **Risk 3**: The skill corpus grows stale because Jerome updates framework conventions faster than he updates the skill files, and LLMs start hallucinating against outdated guidance.
- **Mitigation 3**: Skill corpus is versioned with the framework. Framework releases that change conventions must update the skill corpus in the same PR. A CI check asserts that the skill corpus references current attribute names (fails build on drift).
- **Risk 4**: The primary success metric (≥80% one-shot generation rate) is aspirational and the reality at v1 ship is closer to 50%.
- **Mitigation 4**: Validate before v1 ship by running prompts through Claude Code against a mock domain model (John's direct challenge from party mode). If the 80% threshold is not achievable at v1 ship, lower the v1 threshold to a demonstrated number (e.g., 65%) and commit to ≥80% for v1.5. Never publish an aspirational threshold as a shipping metric.

**Innovation 4 — Convention-DSL risk:**

- **Risk**: New attributes creep into the surface as adopters request them, the DSL bloats, and the clarity that enables LLM one-shot generation is lost.
- **Mitigation**: Projection role hints are permanently capped at 5–7 slots (already committed in UX spec). Every new DSL attribute must demonstrate it cannot be achieved via an existing attribute or via the customization gradient. New attributes are reviewed against the horizontal/vertical rule from the Domain-Specific Requirements section.
- **Fallback**: If the DSL surface is judged too sparse for a specific domain, that is what the customization gradient's template / slot / full-replacement levels are for. The DSL is deliberately small.

## Developer Tool Specific Requirements

*Revised after Party Mode round 2 (Winston, Amelia, Paige, Barry). Package family collapsed from 11 to 8 headline packages, Layer 4 runtime services marked `[Experimental]` through v1.1, reference microservices cut from 5 to 3, documentation toolchain decided (DocFX), teaching errors moved from discipline to compile-time enforcement, most CI gates relocated to Non-Functional Requirements (Step 10), and a solo-maintainer sustainability filter introduced as a PRD-wide constraint all subsequent steps must honor.*

### Solo-Maintainer Sustainability Filter

Every requirement in this section and later sections must pass the test: **"Can a single maintainer sustain this over a 6-month v1 AND a 12-month v1.x without the CI matrix eating the time to build the actual framework?"**

Party Mode round 2 flagged the initial Step 7 draft as over-engineered for solo delivery. The core critique: it was specifying definition-of-done bars before the source generator existed. This filter is now a PRD-wide discipline:

- **Commitments that survive solo maintenance are the real commitments.** Everything else is v1.x+ aspiration and must be labeled as such.
- **Ceremony is the enemy.** Every NuGet package, every CI gate, every doc page, every test suite is a maintenance surface. The question is not "is this a good idea?" but "is this a good idea that can still be maintained at 2am after a release?"
- **Ship something embarrassing early rather than something perfect late.** Target a usable v0.1 at week 4 of implementation, not a perfect v1.0 at month 6. Iteration over specification.
- **The PRD is a hypothesis, not a contract.** Requirements documented here are the current best understanding; any of them can be replaced by implementation reality.

### Project-Type Overview

Hexalith.FrontComposer ships as a family of NuGet packages consumed by .NET developers building event-sourced microservice frontends on Hexalith.EventStore. The framework's "product surface" is the combination of: (1) NuGet packages, (2) the C# attribute and service API, (3) source-generator-emitted partial types that shape consumer code, (4) the `dotnet new hexalith-frontcomposer` project template, (5) the in-process MCP server exposing domain models as typed agent tools plus the skill corpus, and (6) the DocFX-generated documentation site. Each surface must be designed together — a weakness in any one undermines the others.

### Language Matrix

| Language / Runtime | v1 Support | Rationale |
|---|---|---|
| **C# on .NET 10** | ✅ First-class, primary | Framework is implemented in C# targeting .NET 10; all consumer APIs, partial types, and source generators target this runtime. |
| **F# on .NET 10** | ⚠️ Usable, not tested | F# can consume the framework's C# API but will not benefit from typed partial types or attribute-driven customization gradient ergonomically. |
| **VB.NET** | ❌ Not supported | Attribute surface assumes C# idioms. Build against VB.NET projects is untested. |
| **.NET 8 / .NET 9** | ❌ Not v1 | .NET 10 features are load-bearing. Back-porting adds unsustainable maintenance burden. |
| **Blazor Server (dev loop) + Blazor Auto (production)** | ✅ Primary | Per UX spec. |
| **Blazor WebAssembly standalone** | ✅ Supported, not primary | Works via the same Shell but is not the default configuration. |
| **Blazor Hybrid (MAUI/WPF/WinForms WebView)** | ❌ Out of scope for v1 | Known Fluent UI Blazor v5 integration gaps; insufficient testing capacity for solo maintainer. |

### NuGet Package Family

**8 headline packages**, collapsed from an earlier 11-package draft per Party Mode critique (Winston). `.Contracts` stays separate and dependency-free so domain assemblies can reference it without pulling Blazor. `.Generators` + `.Analyzers` merged into `.SourceTools`. `.McpServer` + `.Skills` merged into `.Mcp` (the skill corpus IS the MCP server's payload; version skew between them is the most painful possible failure). `.Templates` ships as a standalone `dotnet new` registration, not counted in the runtime family.

| Package | Purpose | Audience |
|---|---|---|
| **`Hexalith.FrontComposer`** | Meta-package that pulls in Shell + Contracts + SourceTools + EventStore. Default install for new projects. | All consumers |
| **`Hexalith.FrontComposer.Contracts`** | Typed attributes, gradient context types, rendering contract interfaces. **Tiny, dependency-free.** Domain assemblies reference this without pulling Blazor. | All consumers (both server-side domain assemblies and client-side shell) |
| **`Hexalith.FrontComposer.Shell`** | Composition shell, lifecycle wrapper, nav groups, command palette, session persistence, density preference, theme toggle. | Web surface consumers |
| **`Hexalith.FrontComposer.SourceTools`** | Roslyn source generators (typed partial types, one-source-three-outputs) + Roslyn analyzers (gradient compatibility, WCAG violations, build-time teaching errors). Merged from `.Generators` + `.Analyzers`. | All consumers (analyzer/generator reference, not runtime dependency) |
| **`Hexalith.FrontComposer.EventStore`** | Hexalith.EventStore integration layer. `ICommandService`, `IQueryService`, `ISignalRSubscriptionService`, ETag caching. | Consumers using Hexalith.EventStore as the backend (v1: all consumers) |
| **`Hexalith.FrontComposer.Mcp`** | In-process MCP server + skill corpus. Exposes domain models as typed agent tools + Markdown projection resources. Two-call lifecycle pattern. Typed-contract hallucination rejection. | Consumers wanting the chat surface / LLM-native story |
| **`Hexalith.FrontComposer.Aspire`** | .NET Aspire hosting extensions, `.WithDomain<T>()`, DAPR component templates. | Consumers using Aspire (v1: all consumers) |
| **`Hexalith.FrontComposer.Testing`** | xUnit + Shouldly + bUnit + Playwright + FsCheck test utilities, projection snapshot testing helpers, SignalR fault-injection wrappers. | Framework contributors + adopter test suites |

**Separately distributed:**

- **`Hexalith.FrontComposer.Templates`** — `dotnet new` project template. Versions on its own cadence, not part of the runtime family.
- **`Hexalith.FrontComposer.Cli`** (as `dotnet tool`) — includes `dotnet hexalith dump-generated <Type>` for inspecting source-generated output (per Amelia's fix for the generator-black-box pain point) and `dotnet hexalith migrate` for Roslyn-analyzer-driven cross-version migrations.

**Package distribution rules:**

- Published to nuget.org with semantic versioning via semantic-release from conventional commits.
- Pre-release versions use NuGet's prerelease suffix convention.
- Package signing with an OSS-signing certificate for stable releases. Pre-releases may be unsigned.
- SBOM generation (CycloneDX) per release. (Detail captured in Step 10 NFRs.)
- Symbols (`.snupkg`) published for IDE debugging.

**Contingency note:** Barry's Party Mode critique argued for 3 packages maximum. The 8-package choice is defensible but must be validated against real solo-maintainer load in the first 3 months of implementation. If maintenance overhead is unsustainable, collapse to 5 (meta + Contracts + Shell + SourceTools + EventStore) with Mcp/Aspire/Testing as optional installs under the meta-package umbrella.

### Versioning Model

**Lockstep versioning is a v1 constraint**, not a permanent rule. All 8 headline packages version together — a `Shell 1.3.0` is only compatible with `Contracts 1.3.0`, not `1.2.0` or `1.4.0`. Cross-package version mismatches are a build error enforced by the meta-package's dependency constraints.

**Rationale:** For a solo maintainer shipping frequently, independent per-package versioning is a matrix-testing nightmare that will consume the time the framework itself needs. Lockstep is the honest solo-dev trade-off.

**v2 escape hatch:** at v2.0, split into **compile contract** (Contracts, Shell, SourceTools — lockstep) and **satellites** (EventStore, Mcp, Aspire, Testing — independent within a compatible range). Adopters in v1 who want partial upgrades are told explicitly in the README: *"Pin the meta-package. This is v1. Partial upgrade paths arrive in v2."*

Binary compatibility within a minor version is enforced via `PublicApiAnalyzers` that fails CI on accidental breaking changes. Detail captured in Step 10 NFRs.

### Installation Methods

**Primary: NuGet meta-package**

```bash
dotnet add package Hexalith.FrontComposer
```

Pulls in Shell + Contracts + SourceTools + EventStore. Opt-in packages (`.Mcp`, `.Aspire`, `.Testing`) are added explicitly.

**Project template**

```bash
dotnet new install Hexalith.FrontComposer.Templates
dotnet new hexalith-frontcomposer -n MyCompany.MyProject
```

Scaffolds: Aspire AppHost, Counter sample microservice, FrontComposer shell registration, DAPR components directory, `.mcp.json` for Claude Code / Cursor integration, `.gitignore`, `README.md`, `docker-compose.yml` for one-command local topology startup.

**Global CLI tool**

```bash
dotnet tool install -g Hexalith.FrontComposer.Cli --prerelease
```

Provides: `dotnet hexalith dump-generated <Type>` (inspect source-generator output for a specific domain type — fixes Amelia's generator-black-box concern), `dotnet hexalith migrate` (run Roslyn analyzers and apply safe code fixes for cross-version migrations).

### API Surface

Five concentric layers. 90% of usage sits in Layer 1. Layer 4 is explicitly `[Experimental]` through v1.1, with stability committed at v1.2, because runtime service signatures will learn from real adoption in the first 6 months.

**Layer 1 — Attribute-driven declarative surface (STABLE after v1.0)**

| Attribute / Type | Purpose |
|---|---|
| `[Command]` | Marks a record as a domain command. Implies form generation, MCP tool emission, lifecycle wrapper. |
| `[Projection]` | Marks a type as a read model projection. Implies DataGrid / detail generation and MCP resource exposure. |
| `[BoundedContext(name)]` | Associates a domain with a nav group with optional display label (domain language, not architecture name). Build-time check validates the name is not a typo of an existing BC in the solution. |
| `[ProjectionRole(role)]` | Rendering role hint: `ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`. Permanently capped at 5 slots. |
| `[ProjectionBadge(slot)]` | Maps a status enum value to a semantic badge slot: `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Accent`. |
| `[ProjectionFieldSlot<TProjection>(x => x.Field)]` | Applied to a custom Blazor component; declares it as a slot-level override for a specific projection field. **Uses generic type parameter + lambda expression for refactor safety** — not `nameof` strings. This ergonomic change was made after Amelia's Party Mode critique. |
| `[RequiresPolicy(policyName)]` | Command-level authorization attribute. Integrates with ASP.NET Core authorization policies. **Added to Layer 1 per Party Mode (Amelia)** — missing it from the first draft was an oversight; without this attribute, security is an adopter's manual wiring job. |
| `[DisplayName]`, `[Description]` | Standard .NET attributes consumed by the label resolution chain. |

**Layer 2 — Registration services (STABLE after v1.0)**

```csharp
// In each microservice's Program.cs
services.AddHexalithDomain<TDomain>();

// In the Aspire AppHost
builder.AddFrontComposerShell()
       .WithDomain<TDomainA>()
       .WithDomain<TDomainB>();

// Optional chat surface
services.AddFrontComposerMcp();  // adds the in-process MCP server + skill corpus
```

Three lines per microservice + four to six lines of shell-level setup. Ten lines total for a single-microservice project; twenty for a multi-microservice shell project.

**Layer 3 — Customization gradient contracts (STABLE after v1.0)**

Four levels, each bound to typed contracts:

```csharp
// Annotation level (Layer 1)
[ProjectionRole(Role.ActionQueue)]
public record ShipmentProjection { ... }

// Template level — typed Razor template
<FrontComposerViewTemplate For="ShipmentProjection">
    ...custom layout...
</FrontComposerViewTemplate>

// Slot level — typed generic field override (Amelia's improved ergonomics)
[ProjectionFieldSlot<ShipmentProjection>(x => x.EstimatedDispatchAt)]
public partial class RelativeTimeSlot : IFieldSlot<DateTimeOffset>
{
    // FieldSlotContext<DateTimeOffset> is injected via source generator
    // IntelliSense exposes: Value, Validation, Metadata, Formatters
}

// Full replacement — complete view override preserving lifecycle wrapper + accessibility contract
[ProjectionView(For = typeof(ShipmentProjection))]
public partial class ShipmentDashboard : ComponentBase
{
    // Framework injects lifecycle wrapper + accessibility contract automatically
}
```

Compared to the earlier draft: **generic type parameter + lambda expression replaces the `nameof` string** to preserve refactor safety. `FieldSlotContext<T>` exposes `Value`, `Validation`, `Metadata`, and `Formatters` as discoverable IntelliSense properties so consumers never need to consult docs for basic slot usage.

**Layer 4 — Runtime services (`[Experimental]` through v1.1, STABLE at v1.2)**

`ICommandService`, `IQueryService`, `ISignalRSubscriptionService`, `ILifecycleWrapper`, `IRenderingContract`, `ISkillCorpus`, `IMcpToolManifest`.

These are the interfaces most likely to learn from real adoption. SignalR fault semantics, tenant scoping edge cases, and command-envelope shape changes are all expected in the first 6 months. Rather than commit to stability prematurely, v1 marks Layer 4 with `[Experimental]` (the Roslyn `RequiresPreviewFeaturesAttribute` or equivalent with an `RS`-prefix diagnostic), warns consumers who reference runtime services directly, and commits to stability at v1.2 after one field tour.

**Adopters who reference Layer 4 directly during v1.0–v1.1** acknowledge the experimental nature and accept that signature changes may occur in minor versions. Most adopters never touch Layer 4; they operate at Layers 1–3.

**Layer 5 — Source generator outputs (internal, not a stable contract)**

Typed partial types and generator-emitted metadata. Adopters must not reference generator output shape. Framework contributors operate here. `.g.cs` output paths are deterministic and documented so developers debugging generator output can find the emitted code.

### Reference Microservices (3, not 5)

Per Party Mode (Amelia): **three deep microservices that form a learning arc** beat five shallow one-role-each demos. The cut from 5 to 3 also reflects Barry's runway-preservation critique — 5 reference microservices with standalone repos + samples branches + tutorial pages is 5 products masquerading as samples, each with its own backlog, issues, and docs.

| Reference | Domain | What It Teaches |
|---|---|---|
| **`CounterDomain`** | Increment / reset / snapshot a counter | Minimum viable usage. 10-minute read. Every Layer 1 attribute used exactly once, nothing more. This is Journey 1's first-render exemplar. |
| **`OrdersDomain`** | Place / approve / reject / ship orders with validation, status badges, inline actions | The full gradient workout. Commands with FluentValidation, `ActionQueue` projection, `DetailRecord` expand-in-place, slot-level `[ProjectionFieldSlot]` override for the order timestamp column, `[RequiresPolicy]` authorization on the approve command. This is Journey 2 and Journey 3's exemplar. |
| **`OperationsDashboardDomain`** | Multi-domain composition (cross-references `OrdersDomain` + a minimal `InventoryDomain` snippet) | How bounded contexts compose in the shell. `Dashboard` role hint. Multi-domain navigation and session persistence. This is Journey 5 and Journey 6's exemplar for agent interaction across multiple bounded contexts. |

The two archetypes that were cut (a full `InventoryDomain` and `CustomersDomain`) are demonstrated in the Orders microservice as feature diffs rather than standalone projects. If community demand justifies them later, they become v2 additions.

All three reference microservices live in a **single monorepo** alongside the framework code, in a `samples/` directory — not separate repositories (per Barry's "monorepo. period." prescription for solo-maintainer sanity).

### Documentation Strategy

**DocFX. Not Blazor-native SSG.** Party Mode (Paige) decision: dogfooding Blazor for a Blazor framework is a side quest that will eat three weeks and deliver worse search. DocFX is boring, battle-tested, generates API reference from XML comments for free, integrates with the .NET toolchain, and is the right choice for a solo-maintained v1. The Blazor-dogfood docs story is a v2 aspiration if adopter feedback justifies it.

**Single source, two renderings, explicit narrative vs reference section markers.**

The skill corpus (consumed by LLM agents via MCP) and the human-facing documentation site are rendered from the same Markdown source files, but with section markers in the front-matter. The MCP renderer strips narrative sections and exposes only reference material. The DocFX-generated site keeps both. This prevents **voice collapse** — the failure mode Paige identified where writing for two audiences produces docs that are complete but unreadable for humans because the LLM (stricter reader) wins the implicit tie-break.

```markdown
---
narrative: true   # included on human docs site, stripped from MCP skill corpus
---

## Narrative: Why does `[ProjectionRole]` exist?

Early drafts of FrontComposer tried to infer view type from projection shape alone.
That failed for projections that could be rendered as either a queue OR a dashboard...

<!-- reference section follows -->

## Reference

`[ProjectionRole(Role role)]`

Applies a rendering role hint...
```

**Four documentation genres (Diátaxis), not three.**

Per Paige: a framework that ships with only reference + how-to + tutorials produces developers who can *use* the thing but can't *reason* about it. The missing genre is **explanation / concepts**:

| Genre | Purpose | Audience |
|---|---|---|
| **Tutorials** | Learn-by-doing, guided first experience | First-time users (Journey 1 Marco) |
| **How-to** | Task-oriented recipes | Developers solving specific problems (Journey 2 Marco) |
| **Reference** | Technical specification | Developers looking up API details (humans + LLM agents) |
| **Explanation / Concepts** | Why does this exist, what problem does it solve, how should you reason about it | Developers hitting edge cases and needing to understand the design |

The concepts layer is a v1 ship commitment, not a v1.x addition. Without it, adopters file bug reports instead of solving edge cases themselves.

**Teaching errors enforced at compile time, not by discipline.**

Per Paige: a solo maintainer cannot sustain "every error message must be teaching" as a discipline. It degrades to 40% compliance by month six. The fix is to make the error message template **part of the attribute definition**, enforced at compile time by a source generator test. You cannot ship a new attribute without filling in `Expected`, `Got`, `Fix`, and `DocsLink` fields. The build won't let you. This moves the burden from discipline to build-enforcement — the only version of this commitment that survives contact with a tired maintainer at 11pm.

**Migration guide trigger: skill-corpus-compile-break, not major-version-bump.**

Per Paige: breaking changes happen in minors, not just majors. The right trigger for a migration guide is "any change that would make a shipped skill corpus example fail to compile." Regardless of semver bucket, when that trigger fires, the PR must include:
1. Docs page describing the change and its reason
2. Old → new code example
3. Roslyn analyzer flagging old usage with a fix-it, shipped at least one minor version before the bump
4. Updated skill corpus in the same PR
5. Nightly LLM benchmark validating that generation correctness holds on the upgraded version for at least one week before release

**Day-1 highest-leverage doc: customization gradient cookbook.**

Per Paige: the customization gradient cookbook is the day-1 highest-leverage doc — the single page showing the same problem (relative-time rendering for a `DateTimeOffset` field) solved at each gradient level. *Revised in §Project Scoping per Party Mode round 3 (Barry + John):* the prose cookbook moves to **week 6** as an acceptance test *after* the gradient API exists, not before framework code. The week-4 design-time check is instead: *"can Jerome sketch the four gradient level signatures as C# interfaces in under 30 minutes without hand-waving?"* — writability of code, not prose.

### Developer-Visible Technical Constraints

These are the constraints Marco and his team see directly during dev-loop. **The broader CI gate matrix** (unit/component/E2E test coverage percentages, visual regression, axe-core, LLM benchmark cadence, deployment topology validation, SBOM, NuGet signing, Pact contract tests, mutation testing, performance regression gates) **is deliberately relocated to Non-Functional Requirements (Step 10)** to avoid conflating "what the consumer sees" with "how the framework is quality-gated internally." This relocation is part of the solo-maintainer sustainability filter discipline — Step 7 is for developer-tool surface, not CI plumbing.

**Hot reload** is a first-class commitment. Hot reload must work for: domain attribute changes, customization gradient overrides, Razor component edits. Hot reload breakage is a critical bug on par with a runtime crash.

**IDE matrix** — Visual Studio 2026 is the reference IDE. JetBrains Rider 2026.1+ must have parity (IntelliSense, hover docs, go-to-definition, generator debugging). VS Code with C# Dev Kit must work for lightweight-tooling adopters. IDE differences in source generator output are a known pain point; the framework ships a `rider-specific` test fixture validating Rider's handling of generator outputs.

**Source generator performance budget** — 500ms incremental per domain assembly, **4s** full solution rebuild for a 50-aggregate reference domain (per Winston's revision; the original 2s target was optimistic). CI gates on the incremental number, not the full rebuild — incremental is the developer-productivity metric that matters, full rebuild is a vanity metric. Implementation uses `IIncrementalGenerator` with `ForAttributeWithMetadataName` for cache efficiency.

**Trim compatibility** — all framework assemblies must be trim-compatible with Blazor WebAssembly AOT. `IsTrimmable="true"` in all project files; trim warnings block CI. Three known trim-hostile dependencies (FluentValidation, DAPR SDK, Fluent UI v5) require front-loaded evaluation in week 2 with pass/fail criteria. Full evaluation protocol and budget in §Non-Functional Requirements → Build, CI & Release → Trim Compatibility.

**`.g.cs` output path guarantee + `dotnet hexalith dump-generated <Type>` CLI** — per Amelia's generator-black-box concern. Developers debugging generator output must have a deterministic file path to find the emitted code AND a one-command CLI to dump the three outputs (Razor partial + MCP manifest + test specimen metadata) to disk for inspection. Without this, debugging the generator is debugging Hexalith itself, which is an unacceptable dev experience.

**Diagnostic ID scheme** — per Winston. Every framework diagnostic reserves an ID range per package (HFC0001–HFC5999). Each diagnostic maps to a docs page. Full range table in §Non-Functional Requirements → Maintainability.

**Structured logging contract** — per Winston. OpenTelemetry semantic conventions for end-to-end tracing. Full specification in §Non-Functional Requirements → Maintainability.

**Deprecation policy** — per Winston. One minor version minimum deprecation window. `[Obsolete]` messages link to diagnostic ID and migration path. Full convention in §Non-Functional Requirements → Maintainability.

### Tone & Language

Framework-generated UI text and developer-facing messages follow four attributes from the product brief:

| Attribute | Means | Example |
|---|---|---|
| **Technical & precise** | Use correct domain terminology (commands, projections, aggregates). No hand-waving. | Button: "Send Command" not "Submit" |
| **Concise & direct** | Short labels, clear messages, no filler. | Loading: "Loading projections..." not "Please wait..." |
| **Confident & authoritative** | Opinionated framework, opinionated voice. Clear guidance, not hedging. | Error: "Command `CreateOrder` failed: aggregate not found" not "Something went wrong" |
| **Helpful without patronizing** | Explain what happened and how to fix it. Don't dumb down, don't assume memorized docs. | Empty state: "No projections registered. Add a projection to see data here." not "Nothing here" |

**Rules:** Use domain language consistently. Be specific in error messages — name the entity/command that failed. Provide actionable guidance in empty states and errors. Never use vague or generic messages.

### Implementation Considerations (Dev-Loop Only)

The following are dev-loop concerns developers touch daily. Broader implementation concerns (SBOM, package signing, release automation, CI pipeline specifics) are captured in Step 10 NFRs.

**Build-time error quality** — every build-time error from the framework must include: (a) what the generator or analyzer saw, (b) what it expected, (c) how to fix it, (d) a diagnostic ID linking to the docs page. Enforced at compile time via the error template commitment above.

**Conventional commits enforcement** — a commit-msg hook (ships with project template) and CI lint step. Semantic release depends on clean commit messages. The hook is installed by the `dotnet new hexalith-frontcomposer` template so adopters inherit discipline without effort.

**Issue triage templates** — bug report, feature request, adopter question templates in `.github/ISSUE_TEMPLATE/`. Contribution guide explicit about what kinds of PRs are welcome (fixes + docs yes, new framework features require design discussion).

**Single-maintainer sustainability policy** documented in CONTRIBUTING.md: response-time expectations (not commitments), bus-factor acknowledgment, fork-friendliness. Adopters in regulated industries need to know the risk they are taking on when building production systems on a solo-maintained OSS framework.

### Solo-Maintainer Sustainability Verdict

The original Step 7 draft failed Party Mode round 2's sustainability filter. This revision collapses the package count (11 → 8), moves CI gates to Step 10, cuts reference microservices (5 → 3), defers Layer 4 stability to v1.2, and explicitly adopts monorepo structure. **Barry's meta-critique stands: the PRD must not become the work.** Subsequent PRD steps must honor the solo-maintainer sustainability filter, which is why it sits at the top of this section as a PRD-wide discipline rather than as a passing comment.

## Project Scoping & Phased Development

*Strategic overlay on §Product Scope. Revised after Party Mode round-3 review (John, Amelia, Barry, Winston, Murat). The Solo-Maintainer Sustainability Filter (§Developer Tool Specific Requirements) is the governing constraint.*

### MVP Strategy & Philosophy

**Internal engineering frame: validated-learning MVP.** FrontComposer v1 is a hypothesis about two structural claims the four research documents could not pre-validate: (1) LLMs can scaffold event-sourced microservices at ≥80% one-shot correctness given typed partial types + MCP skill corpus + hallucination rejection, and (2) a single domain contract can render coherently across Blazor web and Markdown chat surfaces. Both require a running framework, a benchmark suite, and real adopters.

**Adopter-facing frame: experience-first.** Per Party Mode round-3 (John): the README headline is *"multi-surface UI for one event-sourced contract"* — not "we're validating a hypothesis." Validated-learning is the internal engineering discipline; it is not the community narrative. Adopter acquisition leads with experience; engineering rigor is the scaffolding underneath.

**Explicitly NOT this MVP:** problem-solving (gap already confirmed by 4 research docs), revenue (OSS), or platform (no proven adoption load).

**Resources:** solo maintainer, ~6-month directional v1, ~12-month v1.x horizon.

### v0.1 — The Embarrassing-Early Ship (Week 4 Target)

Internal de-risk milestone, not a public release. It proves the generator infrastructure, the EventStore integration path, AND the LLM hypothesis — the last item was absent from the first draft and was the sharpest Party Mode round-3 critique.

**v0.1 contract (revised per round-3):**

| Included | Excluded |
|---|---|
| Counter domain: `IncrementCommand` + `CounterProjection` | All other reference microservices |
| `[Command]`, `[Projection]`, `[BoundedContext]` attributes only | Full Layer 1 surface |
| Source generator: **1 input → 1 output (Razor only)** — not yet 1-source-3-outputs (per Winston: 3-output generator is a v0.3 milestone) | MCP manifest + test specimen emissions (v0.3) |
| `samples/Counter.sln` checked into the repo (per Amelia) | `dotnet new hexalith-frontcomposer` template — deferred to v0.2, reclaims ~15 hours |
| `ICommandService` + `IQueryService` via Hexalith.EventStore | Full command/query service surface |
| SignalR subscription → live projection update | Reconnect logic, ETag cache, batched reconciliation |
| Plain spinner on command submit | Five-state lifecycle wrapper (v0.2) |
| **Hand-rolled MCP round-trip stub** (per John): 1 command, 1 success path, 1 hallucination-rejection path | Full MCP server, skill corpus, two-call lifecycle |
| **`benchmarks/llm-oneshot/prompts.json` + `scripts/score.ps1`** (per John + Amelia): 10-prompt directional signal. n=10 is a *signal*, not a benchmark. ~6-10 hours. | Production nightly benchmark (week 16 wire-up stays as planned) |
| `Hexalith.FrontComposer.Contracts` + `Hexalith.FrontComposer.Shell` (2 of 8 packages) | The other 6 packages |
| README (one page) | Docs site, cookbook page (moves to week 6 as acceptance test *after* gradient exists) |

**The cookbook page moves OUT of v0.1** (per Barry + John). Writing docs for code that doesn't exist is procrastination, not de-risking. The real week-4 design-time check is *"can Jerome sketch the four gradient level signatures as C# interfaces in under 30 minutes without hand-waving?"* If yes, the gradient is real. If not, the gradient is broken and stops the framework until fixed. The prose cookbook is written at week 6 *after* the API works, as the acceptance test that the gradient is explainable to a human.

**The LLM benchmark harness moves INTO v0.1** (per John + Amelia). The primary metric cannot be real at week 16 when "validated-learning primary" is claimed at week 0. A 10-prompt smoke + `score.ps1` at week 4 is a 6-10 hour addition that prevents "validated-learning" from becoming retroactive marketing copy. A 2/10 result at week 4 means rewrite the attribute DSL with real runway. 7/10 means full speed ahead.

**v0.1 acceptance tests (week 4 — "is it done?" checklist):**

1. `dotnet build samples/Counter.sln` compiles with zero errors and zero framework warnings.
2. `dotnet run` on Counter sample opens Aspire dashboard; FrontComposer shell renders in browser with Counter bounded context visible in sidebar nav, `IncrementCommand` form submitting successfully, and `CounterProjection` DataGrid updating via SignalR.
3. `scripts/score.ps1` executes against `benchmarks/llm-oneshot/prompts.json` (10 prompts) and exits with a numeric score (pass/fail threshold not gated at v0.1 — directional signal only).
4. Hand-rolled MCP stub accepts `IncrementCommand` tool call and returns `{commandId, status: "acknowledged"}`; rejects a hallucinated tool name (e.g., `IncrementCounter.Execute`) and returns a suggestion response with the correct tool name.
5. Gradient design-time check: Jerome can sketch the four gradient level signatures (`[Command]` annotation, `FrontComposerViewTemplate`, `ProjectionFieldSlot<T>`, `ProjectionView`) as C# interfaces in under 30 minutes without hand-waving.

Any red item at week 4 = stop and fix before proceeding to v0.2.

**Pre-flight verification (week 0, before any code is written)** — per Amelia's show-stopper risk list:
1. `Microsoft.CodeAnalysis.CSharp` .NET 10 alignment for `IIncrementalGenerator` authoring.
2. Fluent UI Blazor v5 `<FluentDataGrid TGridItem="...">` generic-type-parameter resolution at generator-emit time.
3. DAPR 1.17.7+ .NET 10 target availability.

Any red light = timeline renegotiated before week 1 begins. Pin `Directory.Packages.props` day 1.

### Must-Have Traceability (v1.0 Ship-Gated)

Every must-have traces to a journey AND a success criterion. Items failing either link are cut. The §Product Scope MVP v1.0 inventory stands; this matrix flags which items are Never-Cut vs. cuttable under slip.

| Area | Journey | Success Criterion | Slip Status |
|---|---|---|---|
| Source-generator chain (1-src-3-outputs at v1.0, 1-src-1-output at v0.1) | 1, 2, 6 | Time-to-first-render; LLM ≥80% | **Never-cut** |
| Five-state lifecycle wrapper + progressive thresholds | 3, 4, 5 | P95 <800ms cold, P50 <400ms warm; lifecycle confidence | **Never-cut** |
| Batched reconnection reconciliation | 4 | Zero rage-clicks; trust contract | **Never-cut** (rejecting Barry's "flicker known-issue" slip — Winston's Innovation-2-integrity argument applies analogously) |
| WCAG 2.1 AA baseline + CI axe-core + teaching Roslyn analyzers | 2, 3, 4 | 100% WCAG conformance | **Never-cut** |
| MCP server (in-process) with typed-contract hallucination rejection | 5, 6 | Tool-call correctness ≥95% | **Never-cut** |
| Two-call MCP lifecycle pattern (acknowledged + subscribe) | 5 | Agent read-your-writes P95 <1500ms | **Never-cut** (Winston: single-call breaks every interesting ES command; deferring invalidates Innovation 1) |
| `Hexalith.FrontComposer.Skills` corpus published as MCP resource | 6 | LLM ≥80% | **Never-cut** |
| **Pact contract tests between REST surface and generated UI** | 1, 6 | Generated-UI correctness, cross-layer invariant | **Never-cut** *(Murat round-3 addition)* |
| **Stryker.NET mutation testing on source generator** | 1, 6 | Silent-bug prevention in generator output | **Never-cut** *(Murat round-3 addition)* |
| **Flaky-test quarantine lane** | — | Honors Murat round-1 directive | **Never-cut** *(Murat round-3 addition)* |
| **Release automation & supply chain** (conventional commits + semantic release + SBOM + NuGet signing — consolidated as one item per Barry) | — | Cadence sustainability, supply-chain integrity | **Never-cut** |
| **LLM benchmark nightly gate** | 6 | LLM ≥80% (or honest lower-floor + trend-up) | **Gated at lower threshold, never advisory** (Murat) |
| Customization gradient (annotation + template + slot) | 2 | Customization time ≤5min; zero customization-cliff | Full-replacement level is slip cut #5 |
| 3 reference microservices (Counter, Orders, OperationsDashboard) | 1, 3, 6 | Adopter onboarding + LLM training exemplars | OperationsDashboard is slip cut #2 |
| Chat surface alpha (Hexalith native) | 5, 6 | 1 chat renderer at v1 | Downgrade to "architected-for" is slip cut #4 |
| **Three-line registration ceremony + template heavy-lifting** | 1, 6 | 5-min quick-start | **Moved OFF Never-Cut** (Winston + Amelia): tighten at v1.0 if possible, v1.1 at latest; quality debt, not ship-blocker |
| 8 NuGet packages (lockstep v1) | — | Package family discoverability | Collapse to 5 per contingency trigger below |
| EN + FR localization | — | i18n reference implementation | **First cut** under slip |

### Month-3 Pivot Triggers (Measurable)

**Trigger 1 — Package count collapse 8 → 5.** Replaces the subjective 4-hour release-work cap (Winston: unmeasurable). Two CI-queryable signals:

- GitHub Actions billable minutes per release tag exceed **90 minutes**, OR
- Wall-clock from `git tag` to `nuget.org shows package` exceeds **2 hours** across 3 consecutive releases.

Either signal fires → collapse `.Mcp`, `.Aspire`, `.Testing` into optional installs under the meta-package umbrella. Pre-planned; decision is mechanical not debated.

**Trigger 2 — LLM benchmark achievability.** First directional measurement at **week 8**, not week 12 (Murat: week 12 is already committed; too late to pivot). Week-8 reading is NOT a gate — it is a signal against a scrappy harness running against a half-built generator with stubbed surfaces. Interpretations:

- Week-8 <50% → rewrite attribute DSL while runway exists. 16 weeks of real optionality.
- Week-8 50–65% → continue building; re-measure at week 12 dry-run against full gradient.
- Week-8 ≥65% → full speed.

**Week-12 dry-run determines the v1.0 gate threshold = measured number + 5pp grace.** Published with a trend-up commitment to ≥80% by v1.5. Per Murat: *"gate at a lower threshold, never advisory."* A gate at 60% preserves the CI discipline, the cached prompt corpus, the pinned model versions, and the nightly cadence. Advisory would kill the apparatus within one sprint.

**Trigger 3 — Chat renderer slippage.** If chat surface alpha cannot ship by week 24 on a feasible path, v1 commitment downgrades from *"Hexalith native chat alpha ships"* to *"chat surface architected-for, no running renderer"*. Multi-surface claim preserved as a typed contract. Hexalith native alpha → v1.1.

### Slip Cut Order (6 → 9 months)

Applied in strict sequence. **The LLM benchmark gate is cut #3, not cut #5** — Murat's round-3 reframing: *"last-resort advisory is how gates die."* The cut is always "lower the number," never "remove the gate."

1. **First cut — French localization.** Ship EN-only. `IFluentLocalizer` infrastructure stays; only FR resource files defer to v1.1. ~1 week reclaimed.
2. **Second cut — OperationsDashboard reference microservice.** Ship 2 (Counter + Orders). OperationsDashboard's multi-domain-composition story → v1.1. ~2 weeks reclaimed.
3. **Third cut — LLM benchmark threshold lowered to week-8-measured floor + 5pp grace.** Published transparently with a v1.5 trend-up commitment. **Gate stays; number drops.** Never advisory. ~0 weeks reclaimed directly — this cut reclaims *confidence* and prevents panic-mode rewrites in the final weeks.
4. **Fourth cut — Chat surface working alpha.** Downgrade from "Hexalith native alpha ships" to "architected-for, no running renderer." Public narrative: *"v1 stakes the multi-surface claim as a typed contract; v1.1 ships the first running renderer."* Rendering abstraction layer still ships. ~3-4 weeks reclaimed.
5. **Fifth cut — Customization gradient full-replacement level.** Keep annotation + template + slot (the common 90% path). Full-replacement (*"the escape hatch the framework promised"*) → v1.1 with documented workaround. This cut risks the "no customization cliff" commitment and must be accompanied by a cookbook workaround pattern. ~2 weeks reclaimed.

**Three-line registration ceremony** sits between cuts 2 and 3 as quality debt: if registration has silently become 5 lines by week 20, it is tightened by v1.1 at the latest — not a v1.0 ship-blocker (per Winston + Amelia round-3 demotion from Never-Cut).

### Risk Responses (Scoping-Adjacent Only)

Full risk inventory lives in §Innovation & Novel Patterns → Risk Mitigation. This table captures only the scope-adjacent responses:

| Risk | Scope Response |
|---|---|
| Source generator performance budget blown (>500ms incremental / >4s full-rebuild) | Optimize via `ForAttributeWithMetadataName` + incremental caching; if unfixable, defer Layer 1 advanced attributes to v1.1 |
| Trim-hostile dependencies (FluentValidation reflection, DAPR SDK, Fluent UI dynamic resolution) | Budget the 2-3 weeks of `DynamicallyAccessedMembers` annotation work Jerome has not yet booked. If unfixable in time, ship without Blazor WebAssembly AOT at v1.0; Server + Auto modes ship as primary |
| LLM vendor deprecates MCP or pivots agent protocols | Chat surface becomes plain Markdown without MCP; multi-surface claim survives; delivery mechanism adapts to whatever replaces MCP |
| Solo maintainer burnout / attention split | `CONTRIBUTING.md` documents single-maintainer policy: response-time expectations (not commitments), bus-factor acknowledgment, fork-friendliness. Adopters in regulated industries informed of the risk |

---

*Round-3 meta-critique (Barry): "1,900 words is the PRD becoming the work." This section compressed from ~1,900 to ~1,400 words. Duplication with §Product Scope removed; strategic overlay retained. Further compression deferred to Step 11 Polish. Barry's underlying rule — **the PRD must not become the work** — is the governing constraint for every subsequent step.*

## Functional Requirements

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

## Non-Functional Requirements

*Consolidated from §Success Criteria, §Developer Tool Specific Requirements, §Innovation Risk Mitigation, and Party Mode rounds 1–3. Revised after Party Mode NFR validation (Winston, Murat, Amelia) + Advanced Elicitation failure-mode analysis. Each NFR is specific and measurable. Categories without relevance to this product are omitted.*

### Performance

**User-facing latency (web surface)**

| Metric | Target (v1) | Target (v1.x) | Measurement |
|---|---|---|---|
| Command click → confirmed state (**P95, cold actor**) | < 800 ms | < 500 ms | Playwright task timer, localhost Aspire. Cold = first command after app restart. |
| Command click → confirmed state (**P50, warm actor**) | < 400 ms | < 300 ms | Same. Warm = subsequent commands within same session. |
| First interactive render | < 300 ms | < 200 ms | Custom `Performance.mark('hfc-shell-interactive')` in `OnAfterRender(firstRender: true)` of root shell component, validated via Playwright `performance.getEntriesByName()`. NOT Lighthouse CI (which does not understand Blazor Server's two-phase render). |
| DataGrid render with 500 virtualized rows (P95) | < 300 ms | < 200 ms | bUnit render benchmark |
| Command palette search response | < 100 ms | < 50 ms | Synthetic keystroke-to-results timer |

*Click-to-confirmed qualifications: single-aggregate command, single projection update. Cross-aggregate or multi-stream commands are not bound by this target. The progressive visibility thresholds handle the UX for longer operations.*

**Agent-surface latency (chat/MCP)**

| Metric | Target (v1) | Target (v1.x) | Measurement |
|---|---|---|---|
| Agent command → projection read-your-writes (P95) | < 1500 ms | < 800 ms | MCP tool-call round-trip benchmark, localhost |
| MCP hallucination-rejection response time (P95) | < 100 ms | < 50 ms | Unit test timer on rejection path |

**Source generator performance (developer-facing)**

| Metric | Target (v1) | Measurement |
|---|---|---|
| Incremental rebuild per domain assembly | < 500 ms | CI gate on incremental timing via `IIncrementalGenerator` diagnostics |
| Full solution rebuild, 50-aggregate reference domain | < 4 s | CI benchmark; gate on incremental only, full-rebuild is advisory |
| Hot reload latency for domain attribute change | < 2 s | Manual verification against dev-loop SLO |

**Progressive visibility thresholds (lifecycle wrapper)**

| Lifecycle phase | Threshold | Behavior |
|---|---|---|
| Happy path | < 300 ms | No lifecycle indicator visible (invisible to user) |
| Brief delay | 300 ms – 2 s | Subtle sync pulse animation |
| Moderate delay | 2 s – 10 s | Explicit "Still syncing…" inline text |
| Extended delay | > 10 s | Action prompt with manual refresh option |
| Connection lost | Immediate on `HubConnectionState.Disconnected` | Warning-colored inline note; ETag polling fallback |
| Reconnection | On `HubConnectionState.Reconnected` | Batched animation sweep + 3 s auto-dismissing toast |

### Security & Data Handling

**Framework-layer data posture**

- Framework persists ONLY UI preference state (theme, density, nav, filters, sort) in client-side storage. Zero PII, zero business data at the framework layer.
- All business data lives in adopter microservices and Hexalith.EventStore. Framework never reads, writes, or caches it beyond ETag-validated query results.
- ETag cache entries contain projection snapshots scoped to `{tenantId}:{userId}` with bounded eviction. Cache is opportunistic; correctness comes from server queries.

**Authentication & authorization**

- OIDC/SAML integration with Keycloak, Microsoft Entra ID, GitHub, Google. No custom auth UI.
- JWT bearer tokens propagated through all command and query operations.
- Tenant isolation enforced at framework layer via `TenantId` from JWT claims.
- `[RequiresPolicy]` attributes integrate with ASP.NET Core authorization middleware. Missing policies produce build-time warnings.

**Supply chain integrity**

- Stable NuGet packages signed with OSS-signing certificate.
- CycloneDX SBOM generated per release.
- Symbols (`.snupkg`) published for IDE debugging.

**MCP security boundary**

- Typed-contract hallucination rejection: unknown tool names rejected with suggestion + tenant-scoped tool list. Command never reaches backend.
- Cross-tenant tool visibility is a security bug.

### Accessibility

**Baseline: WCAG 2.1 AA conformance on all auto-generated output.**

| Commitment | Enforcement |
|---|---|
| All generated forms have associated `<label>` elements | axe-core CI gate |
| All interactive elements keyboard-navigable | Manual screen-reader verification per release |
| Color contrast ≥ 4.5:1 (text), ≥ 3:1 (UI components) | axe-core CI gate |
| Focus management on navigation transitions | Playwright focus-tracking assertions |
| ARIA landmarks, roles, and live regions | axe-core CI gate + manual audit |
| Screen reader compatibility (NVDA, JAWS, VoiceOver) | Manual verification checklist, logged in release notes |
| Custom overrides checked for a11y contract compliance | Build-time Roslyn analyzer with WCAG citation + user scenario |

**CI enforcement:**

- axe-core via Playwright fails build on "serious" or "critical" violations.
- Visual specimen baseline: Light/Dark × Compact/Comfortable/Roomy. Unexplained drift fails merge. RTL deferred to v2.

### Reliability & Resilience

**SignalR connection lifecycle**

- Auto-reconnect with exponential backoff.
- Automatic group rejoin + ETag-gated catch-up query on reconnection.
- Batched reconciliation: N stale rows as one sweep, not N individual flashes.
- Auto-dismissing "Reconnected — data refreshed" toast (3 s).
- In-progress form state preserved across connection interruptions.

**Command reliability**

- Every submission produces exactly one user-visible outcome: success, rejection, or error notification.
- Idempotent handling via ULID message IDs with deterministic duplicate detection.
- Domain-specific rejection messages name conflicting entity and propose resolution.
- Zero silent failures across all surfaces.

**Serialized schema stability**

- All persisted event schemas and MCP tool schemas must be **bidirectionally compatible** within a major version:
  - **Backward-compatible reads:** new code (v1.3) must successfully deserialize events written by any prior minor version (v1.0, v1.1, v1.2).
  - **Forward-compatible serialization:** old code (v1.0) must tolerate unknown fields in events written by newer versions (v1.3). Unknown fields are ignored, not rejected.
- Schema evolution tests required: bidirectional deserialization matrix covering `v1.0 event × v1.N code` and `v1.N event × v1.0 code` for all shipped minor versions.
- Migration delta or breaking-change diagnostic emitted when schema hash diverges from prior deployed version.

### Testability & Quality Gates

**Test coverage floors (not ratios)**

*Murat round: ratios are descriptive, floors are enforceable. The pyramid split (70/20/8/2) is a design intent; the floors below are the CI gates.*

| Level | Coverage Floor | Tooling | Scope |
|---|---|---|---|
| Unit | ≥ 80% line coverage on core framework code (generator core, command pipeline, SignalR reconnection logic) | xUnit + Shouldly, measured by `dotnet-coverage` | Business logic, attribute parsing, contract validation |
| Component | ≥ 15% line coverage on auto-generated Razor components | bUnit, measured by `dotnet-coverage` | Generated form rendering, DataGrid binding, lifecycle states |
| Integration | Minimum 3 tests per API boundary | xUnit + SignalR fault injection | EventStore ↔ framework seam, reconnection behavior |
| E2E | One suite per reference microservice | Playwright | Happy path + disconnect/reconnect + rejection rollback |

*"Core framework code" defined narrowly: `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell/Lifecycle`, `Hexalith.FrontComposer.EventStore/SignalR`. Scaffolding, DI wiring, and Razor template boilerplate are excluded from the 80% denominator.*

**Innovation-critical test types (Never-Cut)**

| Test Type | Purpose | Gate |
|---|---|---|
| Pact contract tests (REST ↔ generated UI) | Verify generated components consume EventStore API contracts correctly | Consumer-driven; provider verification per release |
| Stryker.NET mutation testing on source generator | Ensure mutations produce detectable failures | ≥ 80% kill score on happy-path generation pipeline; ≥ 60% on error-handling paths |
| Flaky-test quarantine lane | Automatic detection, isolation, separate CI lane, reintroduction gate | Zero flaky tests in main CI lane |
| FsCheck property-based testing (command idempotency) | Verify replay-safety across random command sequences | `replay(commands) == original_outcomes` for 1000 generated sequences |
| SignalR fault-injection test wrapper | Simulate drop, delay, partial delivery, reorder without live server | 90% of reconnection behaviors tested at unit level |

**LLM benchmark quality gate**

| Parameter | Value |
|---|---|
| Cadence | Nightly on `main`, NOT per-PR |
| Model versions | Pinned explicitly; upgrades are deliberate, scheduled events |
| Temperature | 0, fixed seed where available |
| Initial gate | Week-8 measured median + 5pp grace |
| **Ratchet rule** | Gate = max(initial gate, trailing **28-day** median minus 3pp). Monotonically non-decreasing — gate never drops. |
| **Model transition** | Ratchet pauses during model transitions. Gate reverts to pre-transition value until 7 days of new-model data stabilize. Fresh calibration then sets the new floor. |
| Prompt corpus | 20 prompts v1, 50+ prompts v1.x; cached prompt-response pairs |
| Legitimate misses | 4/20 may legitimately fail at v1 |
| Budget | Published monthly budget cap for LLM API costs |

### Build, CI & Release

**CI pipeline time targets**

| Tier | Target | Contents |
|---|---|---|
| Inner loop (unit + component) | < 5 min | xUnit + bUnit + Shouldly |
| Full CI (excluding nightly) | < 12 min | Inner loop + integration + Pact verification + Stryker incremental (changed files only) + axe-core + specimen verification |
| Nightly | < 45 min | Full CI + LLM benchmark + full Stryker |

**CI time enforcement:** automated CI step checks "did this run exceed threshold?" and creates a GitHub issue on breach. If full CI exceeds **15 minutes for 3 consecutive days**, a mandatory "CI diet" task is auto-created before new feature work. CI time is treated like flakiness: it compounds if ignored.

**Build enforcement**

- Trim warnings block CI (`IsTrimmable="true"` on all framework assemblies).
- `PublicApiAnalyzers` fail CI on accidental breaking changes within a minor version.
- Conventional commit-msg hook shipped with project template; CI lint validates.
- `CS1591` (missing XML doc comments): warning from day one; **error after the v1.0-rc1 PR** (API freeze milestone). All types in `PublicAPI.Shipped.txt` must have `<summary>` XML doc.

**Trim compatibility (front-loaded, week 2 evaluation)**

Per Amelia + Winston: trim budget is front-loaded, not back-loaded. Week-2 evaluation with pass/fail criteria:

| Dependency | Week-2 Evaluation | Pass/Fail Criterion | If Fail |
|---|---|---|---|
| FluentValidation | `dotnet publish -p:PublishTrimmed=true -p:TrimMode=full` against test project | ILC warnings = 0 | **Defer to v1.1.** Ship v1 with `<SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>` scoped to FluentValidation. Document as known limitation. |
| DAPR SDK | Same test | ILC warnings = 0 | Budget 2 weeks for pin + regression. |
| Fluent UI Blazor v5 | Same test | ILC warnings = 0 | Budget 3 weeks for `[DynamicallyAccessedMembers]` wrapper. If upstream won't merge, maintain wrapper layer. |

Total trim budget: **3 weeks front-loaded** (weeks 2–4). FluentValidation deferred if it exceeds 1 week. No back-loaded trim surprises.

**Release automation**

- Semantic-release from conventional commits.
- NuGet prerelease suffix convention.
- CycloneDX SBOM per release. Stable releases signed.
- Symbols published.

### Deployment & Portability

| Topology | CI Validation | Status |
|---|---|---|
| Local development (Aspire) | Primary dev experience | CI target |
| On-premise (local Kubernetes) | CI target | Validated |
| Azure Container Apps | CI target | Validated |
| AWS ECS/EKS | Manual verification at v1 | Not CI-gated |
| GCP Cloud Run | Manual verification at v1 | Not CI-gated |
| Sovereign cloud (generic K8s) | Implied by on-premise target | Validated |

**Zero direct infrastructure coupling:** automated CI check — no direct references to Redis, Kafka, Postgres, CosmosDB, or DAPR SDK types from framework assemblies. All infrastructure through DAPR component bindings.

### Maintainability & Sustainability

**Versioning**

- Lockstep across all 8 packages for v1. Cross-package mismatch = build error.
- v2 escape hatch: compile-contract lockstep + satellite independence. Decision data-driven at month 18.
- Binary compatibility within minor versions enforced by `PublicApiAnalyzers`.

**Deprecation policy**

- One minor version minimum window.
- `[Obsolete]` convention: "`<old>` replaced by `<new>` in v`<target>`. See HFC`<id>`. Removed in v`<removal>`."
- Migration guide for any change breaking a shipped skill corpus example, regardless of semver bucket.

**Structured logging**

- OpenTelemetry semantic conventions. End-to-end tracing: click → backend → projection → SignalR → UI.
- Compatible with Grafana, Jaeger, Application Insights.

**Diagnostic ID scheme**

| Package | Range |
|---|---|
| Contracts | HFC0001–HFC0999 |
| SourceTools | HFC1000–HFC1999 |
| Shell | HFC2000–HFC2999 |
| EventStore | HFC3000–HFC3999 |
| Mcp | HFC4000–HFC4999 |
| Aspire | HFC5000–HFC5999 |

**Solo-maintainer sustainability filter** (PRD-wide): every quality gate, CI check, doc page, and test suite must survive the question *"Can Jerome sustain this at 2am after a release for 12 months?"*
