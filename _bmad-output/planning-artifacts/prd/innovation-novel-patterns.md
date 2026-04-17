# Innovation & Novel Patterns

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

**From `research/domain-model-driven-ui-generation-research-2026-04-11/index.md`:**
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
