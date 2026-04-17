# PRD Summary Card — Hexalith.FrontComposer

**Date:** 2026-04-11 | **Author:** Jerome | **Full PRD:** [prd/index.md](prd/index.md)

## What It Is

Multi-surface UI generation framework for event-sourced microservices built on Hexalith.EventStore. One .NET domain model (commands, events, projections) produces two surfaces: a Blazor web shell (Fluent UI v5) for human operators and a Markdown chat interface for LLM agents via MCP. Developers write business rules; the framework composes the rendered experience.

## Three Differentiators

1. **Native event-sourcing alignment** — every UI primitive maps 1:1 to a CQRS concept
2. **Eventual-consistency UX** — five-state command lifecycle with progressive visibility thresholds
3. **AI-native generation** — LLM one-shot scaffolding at >=80% correctness via typed partial types + skill corpus + hallucination rejection

## Measurable Outcomes

| Outcome | v1 Target | 12-month Target |
|---|---|---|
| Onboarding speed (dotnet new to running app) | <=5 min | <=3 min |
| LLM one-shot generation (benchmark pass rate) | >=80% | >=90% |
| Code ceremony (non-domain lines per microservice) | <=10 | <=5 |
| Business user first-task (zero training) | <30 s | <20 s |
| Command lifecycle latency (P95 cold) | <800 ms | <500 ms |
| External adopters | 3 | 15 |

## MVP Scope (v1.0)

- Composition shell (FluentLayout, nav groups, command palette, theme, density, accessibility)
- Auto-generation (forms from commands, DataGrid from projections, lifecycle wrapper)
- Customization gradient (annotation, template, slot, full replacement)
- Multi-surface foundation (rendering abstraction, Hexalith native chat alpha, MCP server)
- EventStore communication (command/query services, SignalR, ETag caching)
- 8 NuGet packages (lockstep v1), 3 reference microservices, DocFX docs

## Slip Cut Order (if timeline extends 6 to 9 months)

1. French localization (~1 wk)
2. OperationsDashboard reference microservice (~2 wk)
3. LLM benchmark threshold lowered (gate stays, number drops)
4. Chat surface alpha downgrade to architected-for (~3-4 wk)
5. Customization gradient full-replacement level (~2 wk)

## Key Constraints

- **Solo maintainer** — sustainability filter governs all scope decisions
- **.NET 10 + Blazor Auto + Fluent UI v5 + DAPR** — non-negotiable stack
- **Open-source, zero revenue** — adoption and ecosystem growth are the metrics
- **v0.1 at week 4** — embarrassing-early internal ship to prove generator + EventStore + LLM hypothesis
