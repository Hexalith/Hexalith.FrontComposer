# Recommendations

### Technology Adoption Strategy

1. **Anchor on JSON Schema 2020-12 as the canonical IR.** Convert every input format (OpenAPI 3.1/3.2, DataAnnotations, EF Core metadata, GraphQL SDL, Pydantic/Zod for polyglot) to this canonical form. The schema is the source of truth.
2. **Adopt the JSON Forms pattern: data schema + UI schema + rules + renderer registry.** This is the architectural blueprint — proven in the JS ecosystem, transferable to Blazor.
3. **Build on `DynamicComponent` for runtime rendering and Roslyn incremental generators for compile-time scaffolding.** Use `ForAttributeWithMetadataName` for the attribute-driven generator path. Both paths are needed; pick per scenario.
4. **Ship an MCP server built on the .NET 10 MCP C# SDK.** This is the agentic interoperability surface. Every agentic IDE can drive FrontComposer through it.
5. **Compose on Microsoft Agent Framework 1.0** for any multi-step UI-generation or modification workflows rather than building custom orchestration.
6. **Integrate with Blazor Web App's unified render modes** (Static SSR, Interactive Server, Interactive WASM, Auto) — FrontComposer components must be render-mode-agnostic so the host app chooses the right trade-off per feature.
7. **Use structured output (JSON Schema constrained decoding)** for LLM-assisted schema authoring. Support OpenAI, Anthropic, Azure OpenAI, and local models through a common adapter.
8. **Ship accessibility-by-default.** Every component template meets WCAG 2.2 AA out of the box. Automated a11y tests are part of the standard project template.

### Innovation Roadmap (12 → 18 → 24 months)

**0–6 months (MVP):**
- JSON Schema canonical IR + bidirectional conversion (OpenAPI, DataAnnotations).
- Runtime renderer on top of `DynamicComponent`, with adapters for MudBlazor and FluentUI Blazor.
- Roslyn source generator for compile-time scaffolding from `[UiSchema]` attributes.
- Basic MCP server exposing `scaffold-from-schema`, `validate-accessibility`, `render-view`.
- Starter project template with `spec.md`, `AGENTS.md`, `llms.txt`, `CLAUDE.md`, `.cursorrules` — all auto-generated.
- WCAG 2.2 AA baseline components covering the most common form controls.

**6–12 months (production readiness):**
- Additional renderer adapters (Radzen, Blazorise, DevExpress — commercial partnership).
- LLM-assisted schema authoring via structured output (OpenAI, Anthropic, Azure OpenAI).
- Integration with Microsoft Agent Framework as a callable tool.
- ABP Framework bridge: read ABP entity definitions, emit FrontComposer UI schemas.
- Radzen bridge: consume Radzen's scaffolding output, augment with FrontComposer UI schemas.
- Server-driven UI pattern: serve UI descriptors from a Blazor endpoint, render client-side.
- Telemetry integration with .NET Aspire.

**12–24 months (differentiation):**
- Multi-agent orchestration (Agent Framework graph workflows) for complex UI-generation tasks.
- Live spec-driven regeneration: watching the `spec.md` and regenerating views on change.
- Cross-project schema registry (an optional shared service for large organizations).
- AsyncAPI-driven event-bound UIs.
- Accessibility evaluation beyond WCAG 2.2 (tracking WCAG 3.0 drafts).
- Enterprise SBOM, ISO/IEC 42001 alignment, Secure-by-Design commitments.

### Risk Mitigation

- **Microsoft absorption risk:** Open-source-first positioning, MIT or Apache 2.0 license, explicit alignment with Microsoft's own tools and protocols. Be a first-class citizen of the .NET ecosystem so Microsoft has less incentive to build a competitor.
- **Ecosystem fragmentation risk:** Ship adapters for the top three component libraries (MudBlazor, FluentUI Blazor, Radzen) out of the box; community-contributed adapters for the rest.
- **Standards fragmentation risk (LLM conventions):** Generate all major formats from a single source of truth; update as conventions consolidate.
- **Schema fidelity risk:** Canonical IR with explicit loss boundaries; comprehensive round-trip tests; clear documentation of lossy conversions.
- **Regulatory risk:** Accessibility-by-default as an architectural invariant; AI Act technical documentation from day one; explicit provider/deployer boundary in docs.
- **Execution risk:** The window is open now (MCP + Agent Framework + .NET 10 just shipped). Move fast on MVP; prefer working software to polish.
- **Adoption risk:** Strong alignment with BMAD-METHOD, Kiro, and GitHub Spec Kit conventions so FrontComposer composes with the rest of the spec-driven toolchain.

_Sources:_ [Microsoft Agent Framework on GitHub](https://github.com/microsoft/agent-framework), [.NET Conf 2025 Recap](https://devblogs.microsoft.com/dotnet/dotnet-conf-2025-recap/), [Visual Studio Magazine — .NET 10 AI Integration](https://visualstudiomagazine.com/articles/2025/11/12/net-10-arrives-with-ai-integration-performance-boosts-and-new-tools.aspx), [MCP Specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25), [Roslyn Source Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)

---
