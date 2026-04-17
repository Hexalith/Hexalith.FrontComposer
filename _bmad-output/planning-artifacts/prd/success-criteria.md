# Success Criteria

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
