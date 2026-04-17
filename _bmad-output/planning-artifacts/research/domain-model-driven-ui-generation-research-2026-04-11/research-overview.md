# Research Overview

This report investigates the convergence of three industry movements — **schema-first / model-driven UI generation**, **convention-over-configuration frameworks**, and the emerging pattern of **LLM-friendly architectural conventions** — to inform product and architecture decisions for **Hexalith.FrontComposer**, a Blazor/.NET-centric UI composition framework.

**Thesis validated.** The three movements are collapsing into a single architectural trajectory: codebases organized around declarative domain models with strong conventions that both *generate* UIs automatically and serve as a *high-signal substrate* for LLM-assisted development. The convergence is not speculative — it is already visible in the product design of Kiro, Refine AI, Cursor 3, Microsoft Agent Framework, and the rapid adoption of JSON Schema / MCP / llms.txt conventions across the industry.

**Window confirmed.** Every piece FrontComposer needs — JSON Schema 2020-12 as canonical model format, `DynamicComponent` + Roslyn source generators as codegen mechanisms, Microsoft Agent Framework 1.0 as runtime orchestrator, MCP C# SDK as agent interop layer, Blazor United as render model, WCAG 2.2 AA as accessibility baseline — shipped into .NET between **November 2025 and April 2026**. The .NET/Blazor ecosystem has a clear, unoccupied gap for a schema-first, spec-driven, LLM-friendly UI composition layer. No incumbent (ABP Framework, Radzen, MudBlazor, Power Apps) currently occupies that intersection.

**For the executive summary, full findings, strategic recommendations, and the 0-6-12-24-month roadmap, see [Research Synthesis and Strategic Recommendations](#research-synthesis-and-strategic-recommendations) at the end of this document.**

**Methodology:** Current web research (April 2026), multi-source validation for market/adoption claims, confidence levels flagged where data is uncertain, and direct citation of ~100+ primary sources (specs, vendor docs, analyst reports, developer surveys).
