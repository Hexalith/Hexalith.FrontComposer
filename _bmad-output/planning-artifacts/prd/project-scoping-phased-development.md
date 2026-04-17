# Project Scoping & Phased Development

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
