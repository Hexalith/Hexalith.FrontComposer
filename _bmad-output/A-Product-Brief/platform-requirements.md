# Platform Requirements: Hexalith FrontComposer

---
stepsCompleted: []
---

## Context

- **Product:** Hexalith FrontComposer
- **Type:** Event-sourced frontend composition framework
- **Tech stack:** C# / .NET / Blazor (Server + WebAssembly) / Fluent UI
- **Infrastructure:** DAPR, Hexalith.EventStore
- **Deployment:** On-premise, sovereign cloud, major cloud providers
- **Technical level:** Very technical (architect/developer audience and creator)

## Technology Stack

| Layer | Technology | Version/Detail |
|---|---|---|
| **Runtime** | .NET | 10 |
| **Frontend Framework** | Blazor | Server + WebAssembly |
| **UI Components** | Fluent UI Blazor | v5 |
| **Backend Framework** | Hexalith.EventStore | -- |
| **Infrastructure** | DAPR | 1.17.7 |
| **Hosting** | .NET Aspire | Orchestration |
| **Authentication** | Keycloak (default) | + Entra ID, GitHub, Google |
| **Package Distribution** | NuGet | -- |

## Testing

| Type | Technology |
|---|---|
| **Unit Tests** | xUnit + Shouldly |
| **Component Tests** | bUnit |
| **E2E Tests** | Playwright |

## CI/CD

- Semantic release versioning (automated from commit conventions)

**Rationale:** Every choice reinforces the core principles -- .NET ecosystem consistency, event-sourcing alignment, deployment sovereignty (Aspire + DAPR abstract away infrastructure), and LLM compatibility (conventional commits + semantic versioning = predictable patterns).

## Integrations

| Integration | Provider | Purpose |
|---|---|---|
| **Event Store** | Hexalith.EventStore | Commands, events, projections |
| **ID Generation** | Hexalith.Commons | Unique ID generation |
| **Infrastructure** | DAPR 1.17.7 | All infrastructure concerns (pub/sub, state, service invocation, secrets, configuration, observability) |
| **Authentication** | Keycloak (default) | + Entra ID, GitHub, Google |

### Architectural Rule
**Do not implement any feature that already exists in DAPR.** Implement DAPR abstractions instead. This includes:
- Pub/sub messaging
- State management
- Service-to-service invocation
- Secrets management
- Configuration
- Observability / distributed tracing

DAPR's extensibility covers future needs through its component model.

## Contact & Support Strategy

| Channel | Purpose |
|---|---|
| **GitHub Issues** | Bug reports, feature requests |
| **GitHub Discussions** | Q&A, community support |
| **README / Docs** | Self-service, getting started |

No contact forms, phone, or chat -- standard open-source community channels.

## Multilingual

- **Framework/docs:** English only
- **Composed UI (built-in):** English + French resource files
- **Extensible:** Standard Blazor i18n (IStringLocalizer / IStringLocalizerFactory) for any language
- **Formatting:** .NET CultureInfo for dates, numbers, currency
- **RTL:** Not required

## Maintenance & Ownership

| Area | Owner | Process |
|---|---|---|
| Code & releases | Jerome | Semantic versioning, NuGet publish |
| Dependencies | Jerome | Regular updates (DAPR, Fluent UI, .NET) |
| Documentation | Jerome | Updated with each release |
| Community issues | Jerome + Contributors | GitHub Issues / Discussions |
| CI/CD pipeline | Jerome | Automated via semantic release |

---

**Status:** Platform Requirements Complete
**Last Updated:** 2026-04-06
