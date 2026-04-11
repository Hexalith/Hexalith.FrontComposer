---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments: []
workflowType: 'research'
lastStep: 5
research_type: 'domain'
research_topic: 'Competitive landscape of .NET modular application frameworks (Oqtane, ABP, Piral, Module Federation) and event-sourced backend support'
research_goals: 'Deep dive into how mature .NET application frameworks compare architecturally, specifically how each handles (or fails to handle) event-sourced backends, to inform Hexalith.FrontComposer positioning'
user_name: 'Jerome'
date: '2026-04-11'
web_research_enabled: true
source_verification: true
---

# Research Report: domain

**Date:** 2026-04-11
**Author:** Jerome
**Research Type:** domain

---

## Research Overview

This report examines the competitive landscape of **.NET modular application frameworks** and **micro-frontend composition models** — specifically Oqtane, ABP Framework, Piral, and Webpack Module Federation — with a deep focus on how each architecture handles (or fails to handle) **event-sourced backends**.

The goal is to inform architectural decisions for **Hexalith.FrontComposer**: a Blazor-based front-end composer designed to work naturally with event-sourced backends (command/query split, projections, eventual consistency, optimistic concurrency).

**Research Type:** Domain / technical-domain
**Snapshot Date:** 2026-04-11
**Methodology:**
- Web search with source verification for all architectural claims
- Multi-source validation when sources disagree
- Confidence levels marked where evidence is thin
- Focus on current (April 2026) state, not training-data defaults

---

## Domain Research Scope Confirmation

**Research Topic:** Competitive landscape of .NET modular application frameworks (Oqtane, ABP, Piral, Module Federation) and event-sourced backend support

**Research Goals:** Deep dive into how mature .NET application frameworks compare architecturally, specifically how each handles (or fails to handle) event-sourced backends, to inform Hexalith.FrontComposer positioning

**Domain Research Scope:**

- Framework landscape — Oqtane, ABP, Piral, Module Federation (+ honorable mentions)
- Architectural comparison — module boundary, composition model, isolation, versioning, shared state
- Backend coupling assumptions — CRUD/EF Core defaults vs async projections, outbox, event streams
- Event-sourcing fit-gap — command/query UI split, eventual consistency UX, projection staleness, optimistic concurrency
- Community & maturity signals — GitHub activity, release cadence, commercial backing, production users

**Research Methodology:**

- All claims verified against current public sources
- Multi-source validation for critical architectural claims
- Confidence level framework for uncertain information
- Focus on April 2026 state

**Scope Confirmed:** 2026-04-11

---

## Industry / Ecosystem Analysis

> **Note on scope:** Because this is a *technical-domain* research topic (frameworks, not a consumer market), the "industry" lens focuses on **ecosystem size, adoption signals, and maturity indicators** rather than dollar-value market metrics. No paid-report figures exist that segment .NET modular-framework adoption at this resolution, so confidence levels are attached to softer signals.

### Ecosystem Map (April 2026)

The .NET modular / micro-frontend framework ecosystem clusters into four distinct architectural families:

1. **.NET modular monoliths (server-led):** ABP Framework, Oqtane, Orchard Core, DotNetNuke
2. **Micro-frontend portal frameworks (JS-led):** Piral, Single-SPA, Qiankun, Bit — _Piral is typically named among the top-tier_ ([SunBPO Solutions, 2025](https://sunbposolutions.com/micro-frontend-frameworks-scalable-web-apps/))
3. **Build-tool runtime federation:** Webpack Module Federation 2.0, Rspack MF, Native Federation (Angular/Nx)
4. **Hybrid server-composition approaches:** .NET Aspire orchestration + Blazor rendering, Blorc, custom Razor Class Library (RCL) compositions

Hexalith.FrontComposer's design space sits at the intersection of **(1) server-led modular Blazor** and **(4) hybrid composition**, while pursuing an explicit event-sourcing contract absent from all four families.

### Maturity & Release Cadence

| Framework | Current version (2026-04) | Cadence | Commercial backer | .NET Foundation |
|---|---|---|---|---|
| **Oqtane** | 10.00.04 | Major version tracks each .NET major release | Community, Shaun Walker | ✅ Member |
| **ABP Framework** | 10.0.1 + LeptonX 5.0.1 | Major tracks .NET; frequent minors | Volosoft (ABP Commercial) | ❌ (commercial) |
| **Piral** | 1.x line, active | Rolling releases | smapiot / Piral Cloud | ❌ |
| **Module Federation** | **2.0 stable (April 2026)** | Runtime decoupled from bundler | Independent MF team + Rspack | ❌ |

Sources:
- Oqtane release notes: [Oqtane v6.1.3](https://github.com/oqtane/oqtane.framework/releases/tag/v6.1.3), [Oqtane v6.2.1](https://github.com/oqtane/oqtane.framework/releases/tag/v6.2.1), [Oqtane Docs v10.00.04](https://docs.oqtane.org/)
- ABP releases: [ABP Release Notes](https://abp.io/docs/latest/release-info/release-notes), [ABP GitHub releases](https://github.com/abpframework/abp/releases)
- Piral: [Piral GitHub](https://github.com/smapiot/piral), [Piral Docs](https://docs.piral.io/)
- Module Federation 2.0 stable announcement: [InfoQ, April 2026](https://www.infoq.com/news/2026/04/module-federation-2-stable/), [Rspack MF guide](https://rspack.rs/guide/features/module-federation), [module-federation.io](https://module-federation.io/)

### Production Adoption Signals

**Oqtane** — Production-verified deployments at [oqtane.org](https://www.oqtane.org/), [blazorcms.net](https://www.blazorcms.net/), and [blazorkit.net](https://www.blazorkit.net/). Positioned as a Blazor CMS + application framework. Featured on Microsoft's *On .NET Live* ([Microsoft Learn](https://learn.microsoft.com/en-us/shows/on-dotnet/on-dotnet-live-exploring-oqtane-for-blazor-and-dotnet-maui)). Adoption is concentrated in CMS-style portals rather than domain-heavy line-of-business apps. _Confidence: high on CMS use-case, medium on enterprise LOB._

**ABP Framework** — Dual OSS + commercial model, strong enterprise footprint via ABP Commercial, LeptonX UI theme, and ABP Studio tooling. First-class modular-monolith templates, including a dedicated Modular Monolith tutorial ([ABP.IO modular-monolith architecture](https://abp.io/architecture/modular-monolith)). DDD is a first-class citizen with base classes for aggregates, repositories, domain services ([ABP GitHub](https://github.com/abpframework/abp)). _Confidence: high — the most commercially mature .NET modular framework._

**Piral** — Leading portal-style micro-frontend framework; isolation via "pilet" modules; shared-dependency contract and extension slots. Offline-first "emulator" developer experience. ([Piral.io](https://www.piral.io/), [LogRocket: Creating micro-frontends with Piral](https://blog.logrocket.com/creating-micro-frontends-piral/)). React/TypeScript-first — **no native .NET/Blazor story**. _Confidence: high on JS/TS adoption, none on .NET native support._

**Module Federation 2.0** — Became stable in April 2026 with runtime **decoupled from the bundler**, enabling cross-bundler (webpack ↔ Rspack) module sharing, dynamic TS type hints, Chrome devtools, runtime plugins, and preloading ([InfoQ](https://www.infoq.com/news/2026/04/module-federation-2-stable/)). Rspack builds report 5–10× faster than webpack ([dev.to](https://dev.to/ibrahimshamma99/rspack-with-module-federation-v2-is-the-future-3g89)). **Not .NET/Blazor native** — requires JS-interop bridges. _Confidence: high on JS ecosystem dominance, high on .NET gap._

### Market Structure & Segmentation

The segmentation is defined by **module boundary + runtime**:

| Segment | Module unit | Runtime boundary | Typical use |
|---|---|---|---|
| Server-composed Blazor | Assembly / RCL / Oqtane Module | AssemblyLoadContext or single AppDomain | CMS, portals, LOB |
| Server-composed ASP.NET | ABP Module (assembly + migration bundle) | Single process, modular monolith | Enterprise LOB |
| Client-federated JS | Remote (MF) / Pilet (Piral) | Browser runtime, ESM-level | SPA portals |
| Hybrid | iframe or Web Component wrapper | Browser + multiple servers | Mixed-stack migrations |

### Industry Trends (April 2026 snapshot)

- **Runtime decoupling of the composition layer** — Module Federation 2.0 explicitly decouples runtime from webpack. The same pattern is emerging in .NET via ALC (AssemblyLoadContext) unloadability and Aspire-orchestrated module deployment.
- **Modular monolith resurgence** — ABP and Microsoft Docs both prominently document the modular-monolith pattern as a pragmatic middle ground between monolith and microservices ([ABP modular monolith architecture](https://abp.io/architecture/modular-monolith)).
- **Event-sourcing remains a specialized niche** — Microsoft's own guidance frames event sourcing + CQRS as a complexity-carrying pattern reserved for specific domains ([Microsoft Learn: Event Sourcing Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing), [Microsoft Learn: Simplified CQRS+DDD](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)).
- **Projection-centric read models** become a first-class concern in event-driven modular architectures ([Event-Driven.io: Projections and read models](https://event-driven.io/en/projections_and_read_models_in_event_driven_architecture/), [CodeOpinion: Projections in Event Sourcing](https://codeopinion.com/projections-in-event-sourcing-build-any-model-you-want/)).
- **The Blazor modular ecosystem has no popular, opinionated framework for event-sourced front-ends** — this is the architectural gap this research is tracking.

### Competitive Dynamics

- **Market concentration:** In the .NET Blazor modular segment, **Oqtane and ABP effectively form a duopoly**. ABP dominates LOB / enterprise; Oqtane dominates CMS / portal.
- **Competitive intensity:** Low on the .NET side (few framework-grade alternatives), high on the JS micro-frontend side (MF2, Piral, Single-SPA, Qiankun, Bit).
- **Barriers to entry:** High — enterprise-grade modular frameworks require multi-tenancy, permissions, module lifecycle, migrations, theme systems, and tooling. ABP has invested ~10 years; Oqtane ~7 years.
- **Innovation pressure:** Concentrated around **(a) .NET 10 feature adoption**, **(b) Blazor interactive render modes (SSR/Server/WASM/Auto)**, and **(c) cross-bundler federation** on the JS side.
- **The structural gap:** **No incumbent framework treats an event-sourced backend as the default assumption.** All four frameworks assume (or default to) CRUD/EF Core-style persistence. _Confidence: high — confirmed across all sources._

---

## Competitive Landscape — Per-Framework Deep Dive

### 1. Oqtane — Modular Blazor Application Framework

**Category:** Server-composed .NET modular monolith; Blazor-native portal/CMS
**Module unit:** .NET assembly (one `IModule` class per assembly)
**Composition model:** Dynamic page composition — pages host module instances, each module instance is an `IModuleControl` Razor component; multiple instances of the same module can live on different pages and persist state independently.
**Runtime boundary:** Single process, assemblies loaded via Oqtane's `Dependencies` property on `IModule`; assemblies + symbols are bundled into a zip on startup and downloaded to the client for WASM/Auto render modes ([Oqtane Blog: Assembly Loading in Blazor and .NET Core](https://www.oqtane.org/blog/!/11/assembly-loading-in-blazor-and--net-core)).
**Module discovery:** Registered in a `ModuleDefinition` table, key = `namespace,assembly`. First `IModule` class in an assembly defines the module; each assembly contains exactly one module definition but may contain multiple `IModuleControl` components ([Oqtane Docs: IModule](https://docs.oqtane.org/api/Oqtane.Modules.IModule.html), [Discussion #4236](https://github.com/oqtane/oqtane.framework/discussions/4236)).
**Versioning & migrations:** Modules carry **independent version numbers** from the Oqtane core. SQL scripts embedded in the module assembly, executed on install/upgrade from the currently-installed version to the new version. Oqtane 3.1+ added **Site Migration** capability — EF-Core-migration-style codified site changes ([Oqtane Docs: Database Migrations](https://docs.oqtane.org/guides/migrations/database-migration.html)).
**Backward compatibility:** Modules/themes built for prior versions **continue to function on 10.0**; framework maintains explicit backward-compat commitment and seamless upgrade path ([Oqtane 10.0 announcement](https://www.oqtane.org/blog/!/132/announcing-oqtane-10-0-0-for-net-10)).
**Multi-tenancy:** First-class multi-site support baked into the core.
**Render modes:** Static SSR, Blazor Server, Blazor WebAssembly, Blazor Auto, Blazor Hybrid/.NET MAUI — all supported.
**Backend assumption:** EF Core with provider-agnostic SQL scripts per module. No event-store, no event-sourcing primitives. All state is row-based, schema-migrated, mutable.
**Sources:** [Oqtane GitHub](https://github.com/oqtane/oqtane.framework), [Oqtane Docs v10.00.04](https://docs.oqtane.org/), [Routable Modules blog](https://www.oqtane.org/blog/!/62/routable-modules)

**Architectural strengths for FrontComposer comparison:**
- Battle-tested page-composition model with multiple-instances-per-page
- Strong extensibility: themes, containers, modules, interfaces all pluggable
- Multi-tenant from day one
- Independent module versioning (dependencies isolation)

**Architectural limits for event-sourced backends:**
- Module lifecycle assumes synchronous `Install/Upgrade/Uninstall` via SQL scripts — no story for **projection rebuild** on module upgrade
- No abstraction for **command submission vs query read-model reads**; modules typically wire their own EF `DbContext`
- No optimistic-concurrency or stream-version propagation between modules
- No eventual-consistency UX primitives (stale-read indicators, retry-on-version-conflict, etc.)

---

### 2. ABP Framework — Enterprise Modular Monolith

**Category:** Server-composed .NET modular monolith/microservice framework; ASP.NET Core-native
**Module unit:** Class derived from `AbpModule` (one per .NET assembly), split into **framework modules** (caching, emailing, validation, tenancy, auth) and **application modules** (business verticals with entities, services, APIs, UI) ([ABP Docs: Modularity](https://abp.io/docs/latest/framework/architecture/modularity/basics), [DeepWiki: Modularity and DI](https://deepwiki.com/abpframework/abp/2.3-event-bus-and-distributed-events)).
**Composition model:** Dependency graph computed from `[DependsOn(...)]` attributes on module classes. ABP investigates the graph at startup and initializes/shuts down modules in topological order.
**Lifecycle hooks:** `ConfigureServices`, `OnPreApplicationInitialization`, `OnApplicationInitialization`, `OnPostApplicationInitialization`, `OnApplicationShutdown` — all with async variants. Rich, ordered lifecycle with framework-level guarantees.
**Dependency injection:** Conventional auto-registration (by interface, lifetime marker interfaces `ITransientDependency`, `IScopedDependency`, `ISingletonDependency`) with ABP module system as the single registration host.
**DDD first-class:** Aggregates, entities, repositories, domain services, application services, data filters (tenant + soft-delete) — built into framework base types ([GitHub: Module-Development-Basics.md](https://github.com/abpframework/abp/blob/b9f3c30067c21bb3a9eb691ddbcf574585bc84b4/docs/en/Module-Development-Basics.md)).
**Distributed event bus + outbox/inbox:** ABP ships a **built-in transactional outbox/inbox pattern**. Distributed events are saved into the database inside the same transaction as the entity changes; a background worker delivers them to the message broker with retry. Transactional inbox guarantees at-most-once handler execution. Outbox/inbox supports **EF Core and MongoDB** out of the box ([ABP Docs: Distributed Event Bus](https://abp.io/docs/latest/framework/infrastructure/event-bus/distributed), [PR #10008 — Outbox & Inbox patterns](https://github.com/abpframework/abp/pull/10008)).
**Integration with CAP / MassTransit:** `EasyAbp.Abp.EventBus.CAP` bridges ABP's event bus to CAP's outbox; however, ABP's multi-connection-string support conflicts with CAP's single-connection model ([EasyAbp.Abp.EventBus.CAP](https://easyabp.io/modules/Abp.EventBus.CAP/)). ABP's own outbox is recommended over MassTransit for most cases ([ABP support: MassTransit outbox with AbpDbContext](https://abp.io/support/questions/8701/Masstransit-outbox-pattern-with-AbpDbContext)).
**Backend assumption:** **State-based DDD persistence** (entity snapshots via repository) — NOT event-sourced. Outbox is for publishing domain events as side-effects of state changes, not for persisting state as events. The well-known GitHub issue [#57 — CQRS infrastructure](https://github.com/abpframework/abp/issues/57) remains ABP's canonical record that a built-in CQRS/event-sourcing layer is not part of the framework.
**Sources:** [ABP GitHub](https://github.com/abpframework/abp), [ABP Docs: Modularity](https://abp.io/docs/latest/framework/architecture/modularity/basics), [ABP.IO: Modular Monolith Architecture](https://abp.io/architecture/modular-monolith), [ABP Enterprise Blueprint ADR](https://abp.io/architecture-decision-record-adr-ai-ready)

**Architectural strengths for FrontComposer comparison:**
- The gold standard for **.NET modular-monolith engineering**: dependency graph, lifecycle ordering, DI auto-registration
- **Domain events are native**, with ordering, inheritance, tenant-scoping, and unit-of-work integration
- **Transactional outbox is first-class** — the single most important piece of event-sourcing-adjacent infrastructure that incumbents actually ship
- Commercial tooling (ABP Studio) lowers the ceremony of creating modules

**Architectural limits for event-sourced backends:**
- **State-based DDD, not event-sourced** — repositories return the current entity state, not a replayed event stream. Projections are the application's responsibility, not a framework primitive.
- **No first-class CQRS split** in the framework — application services mix commands and queries. The community has asked for CQRS since [#57](https://github.com/abpframework/abp/issues/57); the ABP team's stance has historically been "use application services with explicit intent, CQRS is a pattern not a framework".
- **Outbox is for integration events**, not for persisting the aggregate. No abstraction for event streams, snapshots, upcasting, projection rebuild, or stream-version-aware concurrency.
- **Eventual consistency UX is not addressed** at the framework level: no read-model staleness indicator, no correlation of command → projection-applied, no "wait for projection to catch up" primitive.

---

### 3. Piral — Micro-Frontend Portal Framework

**Category:** Runtime-composed JS/TS micro-frontend framework with optional Blazor bridge
**Module unit:** **Pilet** — an independently-developed, independently-deployed module loaded at runtime into a Piral **app shell** (portal).
**Composition model:** The Piral shell provides layout, routing, tiles, extensions, menus, notifications, modals, dashboards. Pilets register contributions into shell "slots" without knowing about each other.
**Extension mechanism:** **Extensions** are a producer-consumer, name-based pattern. Producers register components into named containers; consumers reference the container by slot name. The sharing is **indirect** — the consumer never references the producing pilet's name. This is the cleanest inter-module decoupling model of the four frameworks examined ([Piral GitHub README](https://github.com/smapiot/piral/blob/develop/README.md), [Piral Docs: Sharing between pilets](https://docs.piral.io/tutorials/12-sharing-between-pilets)).
**Shared dependencies:** Pilets declare what they provide/use; shared dependencies are resolved by the shell and deduplicated. (Similar in spirit to Module Federation shared scope, but shell-orchestrated rather than bundler-orchestrated.)
**Isolation:** Each pilet is isolated at load time — "A pilet is isolated (developed and handled) and will never destroy your application." Errors in one pilet should not take down the shell.
**Developer experience:** Offline-first **emulator** — pilets are developed directly against a special edition of the shell, without needing the live shell deployed.
**Blazor integration ([Piral.Blazor](https://blazor.piral.io/)):**
- **Piral.Blazor.Core** — consumes Blazor components inside a JS-based Piral shell (pilets are .NET assemblies loaded by a JS shell)
- **Piral.Blazor.Orchestrator** — an ASP.NET Core host that runs server-side Blazor pilets, aimed at large-scale micro-frontend solutions ([smapiot/Piral.Blazor.Server](https://github.com/smapiot/Piral.Blazor.Server))
- By default a pilet is treated as an **isolated assembly** — preventing pilet-to-pilet conflicts
- `Piral.Blazor.DevServer` runs under the Piral CLI with **.NET Hot-Reload** and on-demand pilet replacement
**Backend assumption:** Piral is **backend-agnostic by design** — it says nothing about how pilets fetch data. Each pilet brings its own HTTP client, state store, and backend contract. There is no framework-level story for event-sourced backends (or any backend).
**Sources:** [Piral.io](https://www.piral.io/), [Piral Docs](https://docs.piral.io/), [LogRocket: Creating micro-frontends with Piral](https://blog.logrocket.com/creating-micro-frontends-piral/), [Blazor.Piral.io concepts](https://blazor.piral.io/getting-started/concepts)

**Architectural strengths for FrontComposer comparison:**
- The extension-slot producer/consumer model with **indirect component sharing** is architecturally elegant — the strongest decoupling of the four
- **Independent pilet deployment** without shell redeploy (the core promise of micro-frontends)
- Piral.Blazor bridges into the .NET world without forcing Blazor devs to adopt a JS toolchain for the shell side
- The emulator developer experience is unmatched by any .NET framework

**Architectural limits for event-sourced backends:**
- Piral is deliberately backend-agnostic — it provides **no contract** for how a pilet talks to a backend
- No notion of command/query split, read models, projections, or eventual consistency in the framework surface area
- Cross-pilet state coordination is ad-hoc (shared stores, Redux bridges) and has **no link to backend event streams**
- Version-skew between pilets and backend event schemas must be solved per-pilet

---

### 4. Webpack Module Federation 2.0 — Runtime Module Sharing

**Category:** Bundler-adjacent runtime module-sharing protocol, now bundler-decoupled
**Module unit:** **Remote** (exposes modules) and **Host** (consumes modules); both sides are independent Webpack/Rspack builds.
**Composition model:** A host declares `remotes` mapping to remote URLs; Webpack/Rspack generates a runtime loader that fetches the remote's `remoteEntry.js`, resolves shared dependencies, and dynamically imports the exposed module.
**Version 2.0 (stable April 2026):** The runtime has been **extracted from Webpack into a standalone SDK** — `@module-federation/enhanced/runtime` with `init()` + `loadRemote()` APIs that do **not require a build tool at all** ([Module Federation 2.0 GitHub discussion #2397](https://github.com/module-federation/core/discussions/2397), [InfoQ: Module Federation 2.0 stable](https://www.infoq.com/news/2026/04/module-federation-2-stable/)).
**Shared modules:** Shared deps support `singleton`, `requiredVersion`, `eager`, `shareScope`, `import`, `allowNodeModulesSuffixMatch`. Singleton mode ensures a shared dep loads once; higher-version negotiation is built in ([module-federation.io: shared](https://module-federation.io/configure/shared)).
**Runtime plugin system:** Plugins intercept the load lifecycle — preloading, auth, logging, dynamic TS type hints, Chrome devtools integration ([Module Federation Runtime API](https://module-federation.io/guide/runtime/runtime-api), [runtimePlugins config](https://module-federation.io/configure/runtimeplugins)).
**Rspack integration:** First-class. 5–10× build speedup vs webpack. Cross-bundler sharing (Rspack host ↔ webpack remote and vice versa) works transparently ([Rspack MF blog](https://rspack.rs/blog/module-federation-added-to-rspack.html)).
**Blazor / .NET story:** **None natively.** The open ASP.NET Core issue [dotnet/aspnetcore#42486 — support webpack module federation flow](https://github.com/dotnet/aspnetcore/issues/42486) remains open; the framework team has not committed to supporting MF. Community experiments:
- **Genezini's prototype** — Angular-Module-Federation wrapper that exposes Blazor components through MF by generating an Angular shim at compile time ([Playing with module federation and Blazor components](https://blog.genezini.com/p/playing-with-module-federation-and-blazor-components/))
- **Multi-Blazor-WASM-on-a-page** runs into known race conditions: `window.Blazor` and `window.DotNet` are overwritten when multiple startup scripts load simultaneously ([dotnet/aspnetcore#48974](https://github.com/dotnet/aspnetcore/issues/48974))
- Several tpeczek blog posts and binick.blog explorations document viable-but-hand-rolled approaches
**Backend assumption:** MF is a **code-delivery protocol**, not an application framework. It has zero opinion about backend contracts, state management, routing, or composition semantics at the application layer.
**Sources:** [Module Federation docs](https://module-federation.io/), [Rspack Module Federation guide](https://rspack.rs/guide/features/module-federation), [On .NET Live: Micro Frontends with Blazor](https://learn.microsoft.com/en-us/shows/on-dotnet/on-dotnet-live-micro-frontends-with-blazor)

**Architectural strengths for FrontComposer comparison:**
- The only framework on this list where **the composition runtime is fully decoupled from any build tool** (as of MF2.0 April 2026)
- Mature shared-dependency negotiation including version fallback
- Rich runtime plugin ecosystem
- Cross-bundler portability

**Architectural limits for event-sourced backends:**
- Solves **code delivery**, not **composition semantics** — there is no application-level contract for modules
- No Blazor/.NET story at the framework level; bridging requires either iframe, JS-interop, or an Angular wrapper
- No notion of backend contracts at all — event sourcing or otherwise
- Race conditions with multi-instance Blazor WASM are unresolved in the framework

---

### Comparative Architecture Matrix

| Dimension | Oqtane | ABP Framework | Piral | Module Federation 2.0 |
|---|---|---|---|---|
| **Native platform** | Blazor / .NET | ASP.NET Core / any UI | JS/TS (React) + Blazor bridge | JS/TS (any framework) |
| **Module unit** | Assembly with `IModule` | `AbpModule`-derived class | Pilet | Remote exposing modules |
| **Composition runtime** | Server process + WASM download | Single process (modular monolith) | JS shell (runtime) | Bundler-independent runtime SDK |
| **Dependency resolution** | `Dependencies` property | `[DependsOn]` dep graph | Shell-orchestrated shared deps | Shared scope + version negotiation |
| **Module isolation** | Shared AppDomain | Shared AppDomain | Isolated assembly (Blazor) / sandbox (JS) | Separate remote builds |
| **Lifecycle hooks** | Install / Upgrade / Uninstall scripts | Pre/Post Init + Shutdown (async) | Pilet setup function | init / loadRemote / runtime plugins |
| **Versioning strategy** | Per-module SQL-version-script chain | ABP core + NuGet packages | npm pilet versions | `requiredVersion` + semver negotiation |
| **Multi-tenancy** | First-class | First-class | Not framework-level | Not framework-level |
| **DDD support** | None built-in | First-class (aggregates, repos, domain services) | None | None |
| **Domain events** | None | First-class local + distributed bus | None | None |
| **Transactional outbox** | None | **First-class (EF Core + Mongo)** | None | None |
| **Event sourcing** | None | None | None | None |
| **CQRS split** | None | None — see [issue #57](https://github.com/abpframework/abp/issues/57) | None | None |
| **Eventual-consistency UX primitives** | None | None | None | None |
| **Backward compat promise** | Explicit, seamless upgrade | Major-version breaking changes possible | Rolling | Runtime protocol stable |
| **Commercial backing** | Community + .NET Foundation | Volosoft (ABP Commercial) | smapiot / Piral Cloud | Independent + Rspack team |

### Positioning Mapping

Plotting the four frameworks on two axes — **backend opinionation** (no opinion → full DDD+outbox) and **composition boundary** (in-process → runtime-federated):

- **ABP:** high backend opinionation, in-process composition (modular monolith sweet spot)
- **Oqtane:** medium backend opinionation (Blazor + EF Core assumed), in-process composition with WASM download
- **Piral:** low backend opinionation, runtime composition (JS shell)
- **Module Federation 2.0:** zero backend opinionation, runtime composition (bundler-independent)

**The empty quadrant:** **high backend opinionation (event-sourced, CQRS, projections, eventual-consistency primitives) + runtime composition (independently deployable modules)**. No incumbent framework occupies it.

This is the architectural wedge Hexalith.FrontComposer can target — a Blazor-native composition framework that treats **event-sourced backends as the default**, not an afterthought.

### Ecosystem and Partnership Observations

- **Oqtane** is the closest thing the Blazor community has to a "WordPress for Blazor" — ecosystem of modules and themes, .NET Foundation governance, but no enterprise commercial entity behind it.
- **ABP Commercial (Volosoft)** is the enterprise reference for .NET modular monoliths — customer base skews toward LOB and internal systems; ABP Studio is the tooling moat.
- **Piral** has the most mature micro-frontend developer experience (emulator, CLI, feed service, Piral Cloud) but lives in the React/TS world; its Blazor bridge is solid but not the default path.
- **Module Federation 2.0** has become the de-facto runtime standard for JS-side composition. Its decoupling from webpack in April 2026 is a structural change worth tracking — it opens the door for non-JS consumers (including .NET) to adopt the MF runtime contract directly.

---

## Event-Sourcing Fit-Gap Analysis

> **Note on reframing:** This section repurposes the original "Regulatory Focus" step for a technical-domain topic. Instead of *compliance frameworks*, we evaluate each framework against an **event-sourcing requirements scorecard** — the contractual obligations a framework must meet to be a good host for an event-sourced backend.

### Event-Sourcing Requirements Scorecard

What a framework must provide (or get out of the way of) to play well with an event-sourced backend:

| # | Requirement | Why it matters |
|---|---|---|
| **R1** | **Command / Query split** | Commands must target the write side (append to event streams); queries must target projections (read models). Mixing them bypasses event sourcing. ([Microsoft Learn: CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)) |
| **R2** | **Stream-version optimistic concurrency** | Every command carries an `expectedVersion`; conflict detection is server-side CAS. The UI must be able to receive and re-submit on conflict. ([Eventide: Expected Version and Concurrency](http://docs.eventide-project.org/user-guide/writing/expected-version.html), [Event-Driven.io: Optimistic concurrency](https://event-driven.io/en/optimistic_concurrency_for_pessimistic_times/)) |
| **R3** | **Projection staleness / read-your-writes** | The UI needs primitives for "wait until projection has caught up" or "optimistic UI + reconcile on projection-applied" ([CodeOpinion: Eventual Consistency is a UX Nightmare](https://codeopinion.com/eventual-consistency-is-a-ux-nightmare/), [EventSourcingDB: Read-Model Consistency and Lag](https://docs.eventsourcingdb.io/best-practices/read-model-consistency-and-lag/)) |
| **R4** | **Projection lifecycle** | Register, build, rebuild, version, and hot-swap projections without redeploying the host. Modules adding a projection should not take down the shell. |
| **R5** | **Event-schema evolution / upcasting** | Old events must continue to replay against new aggregate/projection code. Framework should expose an upcaster hook or accept event-schema versioning per module. |
| **R6** | **Module-to-event-stream mapping** | Each module owns zero or more aggregate streams and zero or more projections. Framework must provide a contract for stream ownership that aligns with module boundaries. |
| **R7** | **Async UX primitives** | Stale-read indicators, "pending" states bound to command correlation IDs, retry-on-conflict, progressive confirmation ([Medium: Eventual Consistency in the UI](https://medium.com/@nusretasinanovic/eventual-consistency-in-the-ui-64b29e645e11)). |

---

### Fit-Gap Scoring

Legend: ✅ native / 🟡 community-possible / ❌ absent

| Requirement | Oqtane | ABP Framework | Piral | Module Federation 2.0 |
|---|---|---|---|---|
| **R1. Command/Query split** | ❌ | 🟡 (via MediatR integration) | ❌ | ❌ |
| **R2. Optimistic concurrency** | ❌ | ❌ | ❌ | ❌ |
| **R3. Read-your-writes** | ❌ | ❌ | ❌ | ❌ |
| **R4. Projection lifecycle** | ❌ | ❌ | ❌ | ❌ |
| **R5. Schema evolution / upcasting** | ❌ | ❌ | ❌ | ❌ |
| **R6. Module→stream mapping** | 🟡 (1 module = 1 assembly, but no stream contract) | 🟡 (1 `AbpModule` = many aggregates, no stream contract) | 🟡 (1 pilet = backend-agnostic) | ❌ |
| **R7. Async UX primitives** | ❌ | ❌ | ❌ | ❌ |

**Headline finding:** Across four major frameworks and seven event-sourcing requirements, there are **zero native `✅` cells**. Every row is either a gap or requires a community-driven workaround. This confirms the "empty quadrant" hypothesis from the competitive landscape section.

---

### Per-Framework Gap Commentary

#### Oqtane — gap commentary

- **R1–R3:** Oqtane modules build their own `DbContext` (or connection) and access data directly. There is no interception point where a command/query router could sit.
- **R4:** Projection lifecycle is conceptually compatible with Oqtane's install/upgrade script model, **but** the hook is a one-shot SQL script, not a streaming event consumer. Hot-swap is not in the model.
- **R5:** No event schema concept; modules assume the current DB schema is authoritative.
- **R6:** Oqtane's assembly-per-module rule is actually a **structural asset** — a natural home for stream ownership if Hexalith layered a stream-contract over `IModule`.
- **R7:** Oqtane's interactive render mode machinery (SignalR-backed Server / WASM Auto) could, in principle, host optimistic-UI primitives, but nothing in the framework names them.

**Verdict:** Architecturally adjacent to what's needed, but every event-sourcing primitive is absent. Hexalith would need to build R1–R7 on top.

#### ABP Framework — gap commentary

- **R1:** The [ABP Community article "Implementing CQRS with MediatR in ABP"](https://abp.io/community/articles/implementing-cqrs-with-mediatr-in-abp-xiqz2iio) is the *official* path: it explicitly states ABP **does not provide a built-in CQRS library** and recommends plugging in MediatR. This is a well-documented pattern, hence 🟡, but it is **not a framework feature**.
- **R2:** ABP's repositories assume state-based persistence; they expose a unit-of-work and change tracker, not a stream-version CAS. Some commercial users bridge this via MassTransit sagas, but that's aggregate-adjacent, not stream-versioned.
- **R3:** ABP's transactional outbox solves *publishing* integration events atomically with a state write. It does **not** solve **read-your-writes at the UI layer** — once the event is published, the read projection still catches up asynchronously with no UI-facing correlation mechanism.
- **R4:** ABP has no concept of a projection. The closest analog is a domain-event handler that writes to a secondary store; rebuild requires manually replaying from an external event store (which ABP doesn't provide).
- **R5:** ABP's entity migrations handle schema evolution; there is no event upcaster.
- **R6:** `AbpModule` is well-suited to owning streams if Hexalith layered a stream contract on top.
- **R7:** None. ABP's application service pattern returns DTOs synchronously. Any async UX is application-code responsibility.

**Verdict:** The strongest infrastructure foundation of the four (modularity + outbox + DDD), but **nothing above the persistence line** supports event sourcing natively. ABP + MediatR + Marten (community stack) is viable — and also exactly what Hexalith.FrontComposer could pre-package into an opinionated framework.

#### Piral — gap commentary

- **R1–R7:** Piral is *deliberately* silent on backend contracts. Its "no opinion" stance is a feature for React/JS teams; it is a **gap** for Blazor teams building against event-sourced backends.
- Piral.Blazor.Orchestrator is the most interesting angle: it runs server-side Blazor pilets in an ASP.NET Core host, which **could** be layered with event-sourcing primitives. But the Piral project itself won't provide them.
- **Inter-pilet extension slots** could, in principle, carry pending-state or optimistic-UI contracts, but the Piral team treats slots as UI-component-sharing, not data-contract-sharing.

**Verdict:** Piral is a **distribution and composition** framework; it is not in the business of backend opinionation. Hexalith could learn a lot from Piral's extension-slot model and pilet isolation, but would need to bring the entire event-sourcing layer itself.

#### Module Federation 2.0 — gap commentary

- MF is a **code delivery protocol**, not an application framework. Its relationship to event sourcing is zero.
- The interesting architectural observation is that **MF 2.0's runtime decoupling from webpack** (April 2026) means the MF runtime contract is now theoretically consumable from non-bundler environments, including Blazor. If Hexalith wanted to reach *out* to JS micro-frontends, speaking MF's runtime protocol is a reasonable interop story.
- MF's shared-dependency version negotiation (`singleton`, `requiredVersion`) is conceptually similar to the **shared event-schema version negotiation** Hexalith would need for R5. This is a useful *design inspiration*, not a drop-in.

**Verdict:** Out of scope as a backend-aware framework. Relevant for Hexalith only as a distribution mechanism / JS interop story.

---

### Compliance Frameworks (Event-Sourcing Maturity Bands)

Reframing "compliance frameworks" as **maturity bands** a framework can achieve for event-sourced backend support:

**Band 0 — Agnostic.** Framework has no backend opinion. Developers wire their own event store, projections, concurrency, UX. _(Piral, Module Federation 2.0)_

**Band 1 — Infrastructure-aware.** Framework provides reliable publishing (outbox), idempotent consumers (inbox), and distributed-event ordering — but persistence is still state-based. _(ABP Framework)_

**Band 2 — Module-aware.** Framework treats modules as first-class units that can own data and lifecycle, but backend is CRUD-style. _(Oqtane, ABP)_

**Band 3 — Stream-aware.** Framework provides command/query split, stream-version concurrency, projection registration, and schema evolution as first-class primitives. **No incumbent framework operates in this band.**

**Band 4 — UX-aware.** Framework provides async UX primitives bound to command correlation — progressive confirmation, stale-read markers, retry-on-conflict, optimistic UI reconciliation. **No incumbent framework operates in this band.**

Hexalith.FrontComposer's target is **Bands 3 and 4** — this is the positioning differentiation.

---

### Data Protection and Audit Implications

(Where regulatory concerns are genuinely relevant for event sourcing.)

Event-sourced backends have a **natural architectural affinity for audit and compliance**:

- **Immutable event log** = inherent audit trail; every state change is recorded with actor, timestamp, and command intent.
- **Replayable projections** = ability to reconstruct historical read models for subpoena, audit, or regulatory inquiry.
- **Schema evolution via upcasting** = long-lived event history remains readable across years of schema drift, which is valuable for regulations requiring multi-year data retention (GDPR Art. 30, SOX §404, HIPAA §164.316).

But event sourcing also has a **GDPR Right-to-Erasure tension**:

- The "forget me" right conflicts with immutable event streams. Standard mitigations: **crypto-shredding** (encrypt PII per subject, delete the key), **tombstone events** (append an erasure event, treat the stream as redacted), or **projection-only PII** (keep PII out of the event stream entirely).
- None of the four frameworks examined have opinions on this; any .NET event-sourcing framework built on top of Hexalith will need an explicit story.

Sources: [Event-Driven.io: Projections and read models](https://event-driven.io/en/projections_and_read_models_in_event_driven_architecture/), [AWS Prescriptive Guidance: Event Sourcing Pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/event-sourcing.html)

---

### Implementation Considerations for Hexalith.FrontComposer

Based on the fit-gap analysis, the primitives Hexalith.FrontComposer should own (and that no incumbent provides):

1. **Command-submission contract** with `expectedVersion`, correlation ID, and timeout.
2. **Query abstraction** bound to projection identity (not arbitrary repository method).
3. **Projection registration API** per module with lifecycle (register, build, rebuild, version, deprecate) and hot-swap on module upgrade.
4. **Event-schema evolution hook** (upcasters or event-version tags) scoped per module.
5. **Stream-ownership declaration** on module metadata (which aggregate streams this module reads/writes).
6. **Correlation-aware UX primitives** — Razor components for pending/stale/conflict states bound to in-flight commands.
7. **GDPR story** — explicit guidance on crypto-shredding and PII-in-projection vs event stream.

Everything on this list is absent from Oqtane, ABP, Piral, and MF 2.0.

### Risk Assessment (for Hexalith positioning)

- **Risk 1 — ABP commercial momentum.** ABP is the strongest .NET modular monolith incumbent. If ABP were to ship a built-in CQRS + event-sourcing module (which issues [#57](https://github.com/abpframework/abp/issues/57) and [#2405](https://github.com/abpframework/abp/issues/2405) track), the differentiation window could narrow. _Current signal: ABP's official stance remains "CQRS is a pattern, use MediatR" — no roadmap commitment to native event sourcing._
- **Risk 2 — Microsoft ships first-party.** Microsoft has repeatedly declined to standardize a CQRS/ES framework in .NET; they point to Azure primitives (Event Hubs, Cosmos DB change feed) + DIY. _Signal: low probability of .NET team entering this space._
- **Risk 3 — Niche perception.** Event sourcing remains a complexity-carrying specialized pattern in Microsoft's own guidance ([Simplified CQRS+DDD](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)). Positioning must address the "is this overkill for me?" objection directly.
- **Risk 4 — Oqtane copy.** Oqtane could absorb event-sourcing primitives via a module extension if community pressure rose — but its CMS-centric DNA makes this unlikely organically.

---

## Technical Trends and Innovation

### Emerging Technologies (April 2026 snapshot)

#### Blazor in .NET 10 — "Finally Feels Complete"
- **Interactive render modes** — Static SSR, Interactive Server, Interactive WebAssembly, Interactive Auto — now all stable and composable. Interactive Auto begins with Server SignalR rendering and transitions to WebAssembly once the bundle is downloaded ([Microsoft Learn: Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)).
- **Streaming SSR ("SSR+")** — placeholder-driven progressive content delivery for long-running server work, patched into the DOM via the existing response ([Chris Sainty: Server-side and Streaming Rendering](https://chrissainty.com/blazor-in-dotnet-8-server-side-and-streaming-rendering/), [dotnet-guide: Streaming Rendering & Progressive Enhancement](https://www.dotnet-guide.com/tutorials/blazor/ssr-interactive-islands/)).
- **`[PersistentState]` attribute** — .NET 10's most impactful Blazor improvement: replaces ~25 lines of hand-rolled state serialization with a single attribute, eliminates the double-render problem and redundant API calls during prerendering/hydration ([dotnetwebacademy: Blazor Prerendering Finally SOLVED](https://dotnetwebacademy.substack.com/p/net-10-finally-fixes-prerendering)).
- **Interactive Islands pattern** — static-first pages with targeted interactive components, analogous to Astro-style islands in the JS world ([dotnet-guide: Interactive Islands](https://www.dotnet-guide.com/articles/blazor-interactive-islands-explained/)).

**Implication for FrontComposer:** Streaming rendering is the natural surface for **projection-applied latency display** — placeholders in SSR can wait for a projection to catch up to a just-submitted command, eliminating the round-trip-to-poll pattern. `[PersistentState]` simplifies hydration of optimistic-UI state across the SSR → interactive boundary.

#### Dapr — Virtual Actors + Event-Driven Building Blocks
- **Sidecar architecture** decouples distributed-system concerns from application code ([Dapr Docs: Overview](https://docs.dapr.io/concepts/overview/)).
- **Virtual actor pattern** — single-threaded per-actor execution, automatic GC when idle — conceptually aligned with the aggregate-stream model in event sourcing ([Dapr: Actors overview](https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/)).
- **Sekiban added a Dapr runtime option in May 2025**, with the Orleans integration landing February 2025 — confirming the crossover between actor frameworks and opinionated ES frameworks ([Sekiban GitHub](https://github.com/J-Tech-Japan/Sekiban), [Speaker Deck: Orleans + Dapr + Sekiban](https://speakerdeck.com/tomohisa/distributed-applications-made-with-microsoft-orleans-and-dapr-and-event-sourcing-using-sekiban)).
- **Hexalith.Framework** is itself explicitly built on ASP.NET Core + Dapr for modular multi-tenant applications ([Hexalith GitHub](https://github.com/Hexalith/Hexalith)) — so FrontComposer inherits the Dapr-sidecar assumption.

**Implication for FrontComposer:** The virtual-actor model is the right abstraction for aggregate stream ownership. A module declaring "I own streams of type `Customer`" can be mapped to Dapr actors at the host layer, with FrontComposer's UI primitives speaking directly to the actor contract.

#### .NET Aspire — The Modular Monolith Orchestrator
- Aspire has become **the primary tool for building modular monoliths in .NET in 2026** ([AspireSoftwareConsultancy: Aspire & Modular Monoliths 2026 Architecture Guide](https://aspiresoftwareconsultancy.com/dotnet-aspire-modular-monolith/)).
- AppHost orchestration + automatic service discovery + first-class observability means that **"module = project in Aspire AppHost"** is a legitimate composition model.
- Industry reports (cited in 2026 benchmarks) show ~40% cloud-infra-cost reduction when moving from fragmented microservices to Aspire-managed modular monoliths ([Medium/Asad: .NET Aspire Explained](https://medium.com/@asad072/net-aspire-explained-modern-orchestration-for-cloud-native-net-applications-fa49fe3deed3), [Cigen: Monolith to Microservices with .NET Aspire](https://www.cigen.io/insights/from-monolith-to-microservices-with-net-aspire-a-modernization-roadmap-for-net-apps)).

**Implication for FrontComposer:** Aspire is the orchestration layer; FrontComposer is the composition layer above it. Modules declared to FrontComposer can map cleanly to Aspire resources, giving developers a single mental model (Aspire AppHost manifest ↔ FrontComposer module registry).

#### The .NET Event-Sourcing Library Layer Is Mature
The .NET ecosystem has at least six credible, actively-maintained event-sourcing libraries:

| Library | Persistence | Posture | Notable trait |
|---|---|---|---|
| **Marten** (JasperFx) | PostgreSQL | Document DB + event store hybrid | Rich projections, widely adopted ([martendb.io](https://martendb.io/events/)) |
| **Sekiban** (J-Tech-Japan) | Cosmos DB / Postgres / DynamoDB | Opinionated ES + CQRS | Orleans + Dapr integration ([Sekiban GitHub](https://github.com/J-Tech-Japan/Sekiban)) |
| **Eventuous** | Any (pluggable) | Clean, minimal ES library | Focus on core ES primitives ([eventuous.dev](https://eventuous.dev/docs/persistence/event-store/)) |
| **EventFlow** | Any (pluggable) | Async/await-first CQRS + ES + DDD | Mature, opinionated ([EventFlow GitHub](https://github.com/eventflow/EventFlow)) |
| **Revo** | Any (pluggable) | ES + CQRS + DDD | Framework-style ([revoframework/Revo](https://github.com/revoframework/Revo)) |
| **EventSourcing.NetCore** (Oskar Dudycz) | Examples/patterns | Tutorial repository | Canonical reference ([oskardudycz/EventSourcing.NetCore](https://github.com/oskardudycz/EventSourcing.NetCore)) |

**Critical observation:** Every single one of these libraries operates **below the UI layer**. None of them is a front-end composition framework. None of them ships UI primitives for eventual-consistency UX. This is the **horizontal gap** across the entire .NET event-sourcing ecosystem — and it is exactly what Hexalith.FrontComposer addresses.

### Digital Transformation Patterns

- **Modular monolith resurgence** — confirmed by ABP, Aspire, and industry benchmarks — represents a pragmatic reaction against unnecessary microservice decomposition.
- **Event sourcing moving up the stack** — from infrastructure pattern to first-class application concern, driven by audit/compliance pressure, AI-agent replay needs, and temporal-query use cases.
- **Composition layer is the new frontier** — the industry has solved code delivery (MF 2.0), sidecar runtimes (Dapr), orchestration (Aspire), and persistence (Marten/Sekiban). The remaining unsolved layer is **opinionated UI composition for event-sourced domains**.

### Innovation Patterns

- **Runtime decoupling from build tools** (MF 2.0) — enables future non-JS consumers of the same composition protocol.
- **Attribute-based state persistence** (`[PersistentState]`) — reduces glue code by orders of magnitude and enables new patterns.
- **Actor-framework + ES convergence** (Sekiban + Orleans/Dapr) — aligns concurrency model with persistence model.
- **Streaming SSR + placeholder hydration** — opens a UX pathway for projection-lag display that wasn't possible in earlier Blazor versions.

### Future Outlook

**12-month horizon (April 2026 → April 2027):**
- Module Federation 2.0 runtime plugin ecosystem will expand; bridges to .NET will emerge from community (low-confidence on timing).
- ABP is unlikely to ship native CQRS/ES (stable stance).
- Dapr actor model will deepen in the .NET ecosystem as Hexalith and Sekiban both rely on it.
- Blazor in .NET 11 (preview late 2026) will likely add further streaming-rendering refinements.

**24-month horizon:**
- Expect a consolidation of .NET event-sourcing libraries (the 6 listed above) as Aspire-native templates mature.
- The "opinionated composition framework" layer — where FrontComposer lives — has no visible incumbent forming.

### Implementation Opportunities (for Hexalith.FrontComposer)

1. **Blazor-native composition with Dapr actor stream ownership** — unique quadrant, no incumbent.
2. **Streaming-SSR-integrated projection-lag display** — leverages .NET 10 primitives directly.
3. **Aspire-aware module registry** — modules are declared once, visible to both Aspire orchestration and FrontComposer composition.
4. **MediatR interoperability** — meet ABP-community users where they already are; offer an upgrade path from "CQRS-via-MediatR" to "CQRS + ES + UI composition via FrontComposer".
5. **Pluggable event-store backend** (Marten / Sekiban / EventStoreDB) — follow the Eventuous/EventFlow pattern of being persistence-agnostic.
6. **Piral-style extension slots for Blazor** — learn from Piral's producer/consumer decoupling and bring it to Blazor natively.

### Challenges and Risks

- **Adoption friction** — event sourcing remains a niche pattern. Onboarding must make the "why event sourcing" case upfront.
- **Competing with "good enough" state-based DDD** — ABP's state-based approach works for most LOB apps; FrontComposer must target domains where temporal queries, audit, or AI-agent replay justify the complexity.
- **Blazor itself as a ceiling** — Blazor's enterprise adoption, while growing, is still smaller than React's. FrontComposer should not foreclose a future "FrontComposer.React" or "FrontComposer.Angular" contract.
- **Maintaining neutrality** across the 6+ .NET event-sourcing libraries while shipping an opinionated default.

---

## Recommendations

### Technology Adoption Strategy (for Hexalith.FrontComposer)

1. **Pin the positioning to Band 3 + Band 4.** The research confirms the empty quadrant is real and defensible. Lead with "the first Blazor composition framework that treats event-sourced backends as the default" rather than with generic "modular Blazor framework" language — the latter is crowded (Oqtane, ABP).
2. **Ship a Marten-first opinionated default**, with documented paths to Sekiban, EventStoreDB, and a generic stream contract. Marten has the strongest community pull and the cleanest projection story in .NET.
3. **Adopt Dapr virtual actors for stream ownership** — inherit Hexalith.Framework's existing assumption and expose it as a module-authoring primitive.
4. **Integrate with .NET Aspire AppHost** as the orchestration layer; declare FrontComposer modules once, visible to both sides.
5. **Borrow Piral's extension-slot producer/consumer model** for inter-module UI contracts. This is the most elegant decoupling pattern in the space.
6. **Offer a MediatR interoperability mode** — the ABP-community "CQRS via MediatR" users are the largest pool of potential migrators.

### Innovation Roadmap Signals

- **Short-term (0–6 months):** Lock down the module contract, stream ownership declaration, command+expectedVersion shape, and projection registration API. These are the R1–R6 gaps from the scorecard.
- **Mid-term (6–12 months):** Ship the Razor component library for async UX primitives (pending, stale, conflict, retry). This is R7 and is the hardest thing for competitors to copy because it touches component design, not just infrastructure.
- **Long-term (12+ months):** Interop protocols — MF 2.0 runtime bridge for JS micro-frontend consumers, and a declarative module manifest format compatible with Aspire AppHost.

### Risk Mitigation

- **Watch ABP issue [#57](https://github.com/abpframework/abp/issues/57) and [#2405](https://github.com/abpframework/abp/issues/2405) quarterly** for any change in ABP's stance on built-in CQRS/ES. If ABP commits to shipping native CQRS, FrontComposer's wedge narrows — pivot to doubling down on R7 (async UX primitives) where framework-level imitation is hardest.
- **Engage the Marten, Sekiban, and Dapr communities early** to ensure FrontComposer is seen as an *amplifier* of their work, not a competitor.
- **Address GDPR / Right-to-Erasure explicitly** in framework documentation — crypto-shredding patterns, projection-only PII guidance. This is the single most common objection to event sourcing in regulated domains, and a clear answer is a differentiator.
- **Publish the "is this overkill?" decision tree** openly — event sourcing's niche perception can only be overcome by honest framing of when to use it and when not to.

---

## Research Synthesis — Key Insights

1. **The .NET modular framework space is a duopoly** (ABP for LOB, Oqtane for CMS) with a single-player micro-frontend portal (Piral, JS-first with a Blazor bridge) and a build-tool federation protocol (MF 2.0, now bundler-independent but no .NET native story).
2. **Every one of these frameworks assumes CRUD/state-based persistence.** ABP's transactional outbox is the closest any incumbent gets to event-sourcing infrastructure — but it solves *publishing integration events*, not *persisting state as events*.
3. **The scorecard is brutal:** 4 frameworks × 7 event-sourcing requirements = zero native `✅` cells. Every gap is either absent or requires community-driven workarounds.
4. **The empty quadrant is real** — "high backend opinionation (event-sourced, CQRS, projections, eventual-consistency primitives) + runtime composition (independently deployable Blazor modules)" has no incumbent.
5. **The .NET event-sourcing library layer is mature** (Marten, Sekiban, EventFlow, Eventuous, Revo, EventSourcing.NetCore) — but none of them is a UI composition framework. The horizontal gap across the ecosystem is the UI/composition layer.
6. **Microsoft's own roadmap (Blazor .NET 10, Aspire, Dapr) aligns structurally with what FrontComposer needs:** streaming SSR + `[PersistentState]` for projection-lag UX, Aspire for orchestration, Dapr actors for stream ownership.
7. **Positioning clarity:** Hexalith.FrontComposer should target **"the first Blazor composition framework built for event-sourced domains"** — Bands 3 and 4 of the event-sourcing maturity ladder. This is a defensible, unique position in April 2026.

---

## Sources

**Oqtane:**
- [Oqtane GitHub](https://github.com/oqtane/oqtane.framework)
- [Oqtane Docs v10.00.04](https://docs.oqtane.org/)
- [Oqtane 10.0 announcement](https://www.oqtane.org/blog/!/132/announcing-oqtane-10-0-0-for-net-10)
- [Oqtane Blog: Assembly Loading in Blazor and .NET Core](https://www.oqtane.org/blog/!/11/assembly-loading-in-blazor-and--net-core)
- [Oqtane Routable Modules blog](https://www.oqtane.org/blog/!/62/routable-modules)
- [Oqtane Discussion #4236: Module packaging constraints](https://github.com/oqtane/oqtane.framework/discussions/4236)
- [Oqtane Docs: IModule interface](https://docs.oqtane.org/api/Oqtane.Modules.IModule.html)
- [Oqtane Docs: Database Migrations](https://docs.oqtane.org/guides/migrations/database-migration.html)

**ABP Framework:**
- [ABP GitHub](https://github.com/abpframework/abp)
- [ABP.IO: Modular Monolith Architecture](https://abp.io/architecture/modular-monolith)
- [ABP Docs: Modularity basics](https://abp.io/docs/latest/framework/architecture/modularity/basics)
- [ABP Docs: Distributed Event Bus](https://abp.io/docs/latest/framework/infrastructure/event-bus/distributed)
- [ABP PR #10008: Outbox & Inbox patterns](https://github.com/abpframework/abp/pull/10008)
- [ABP Community: Implementing CQRS with MediatR in ABP](https://abp.io/community/articles/implementing-cqrs-with-mediatr-in-abp-xiqz2iio)
- [ABP Issue #57: CQRS infrastructure](https://github.com/abpframework/abp/issues/57)
- [ABP Issue #2405: Integration with CQRS and MediatR](https://github.com/abpframework/abp/issues/2405)
- [ABP Enterprise Blueprint ADR](https://abp.io/architecture-decision-record-adr-ai-ready)
- [DeepWiki: ABP Modularity and DI](https://deepwiki.com/abpframework/abp/2.3-event-bus-and-distributed-events)
- [EasyAbp: Abp.EventBus.CAP](https://easyabp.io/modules/Abp.EventBus.CAP/)

**Piral:**
- [Piral.io](https://www.piral.io/)
- [Piral GitHub](https://github.com/smapiot/piral)
- [Piral Docs](https://docs.piral.io/)
- [Piral Docs: Sharing between pilets](https://docs.piral.io/tutorials/12-sharing-between-pilets)
- [Piral.Blazor](https://blazor.piral.io/)
- [Piral.Blazor.Server GitHub](https://github.com/smapiot/Piral.Blazor.Server)
- [LogRocket: Creating micro-frontends with Piral](https://blog.logrocket.com/creating-micro-frontends-piral/)

**Module Federation 2.0:**
- [Module Federation docs](https://module-federation.io/)
- [InfoQ: Module Federation 2.0 stable (April 2026)](https://www.infoq.com/news/2026/04/module-federation-2-stable/)
- [Module Federation 2.0 release discussion](https://github.com/module-federation/core/discussions/2397)
- [Rspack: Module Federation guide](https://rspack.rs/guide/features/module-federation)
- [Rspack: Module Federation added to Rspack](https://rspack.rs/blog/module-federation-added-to-rspack.html)
- [Module Federation: shared modules](https://module-federation.io/configure/shared)
- [Module Federation: runtime API](https://module-federation.io/guide/runtime/runtime-api)
- [dotnet/aspnetcore#42486: Blazor support for webpack module federation](https://github.com/dotnet/aspnetcore/issues/42486)
- [dotnet/aspnetcore#48974: multi-Blazor-WASM race condition](https://github.com/dotnet/aspnetcore/issues/48974)
- [Genezini: Playing with module federation and Blazor](https://blog.genezini.com/p/playing-with-module-federation-and-blazor-components/)
- [On .NET Live: Micro Frontends with Blazor](https://learn.microsoft.com/en-us/shows/on-dotnet/on-dotnet-live-micro-frontends-with-blazor)

**Event Sourcing / CQRS / UX patterns:**
- [Microsoft Learn: CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Microsoft Learn: Event Sourcing Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Microsoft Learn: Simplified CQRS+DDD in microservices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)
- [Event-Driven.io: Optimistic concurrency for pessimistic times](https://event-driven.io/en/optimistic_concurrency_for_pessimistic_times/)
- [Event-Driven.io: Projections and read models](https://event-driven.io/en/projections_and_read_models_in_event_driven_architecture/)
- [Event-Driven.io: Eventual consistency and idempotency in MongoDB projections](https://event-driven.io/en/dealing_with_eventual_consistency_and_idempotency_in_mongodb_projections/)
- [Eventide: Expected Version and Concurrency](http://docs.eventide-project.org/user-guide/writing/expected-version.html)
- [Eventuous: Event store](https://eventuous.dev/docs/persistence/event-store/)
- [CodeOpinion: Eventual Consistency is a UX Nightmare](https://codeopinion.com/eventual-consistency-is-a-ux-nightmare/)
- [CodeOpinion: Projections in Event Sourcing](https://codeopinion.com/projections-in-event-sourcing-build-any-model-you-want/)
- [Medium: Eventual Consistency in the UI](https://medium.com/@nusretasinanovic/eventual-consistency-in-the-ui-64b29e645e11)
- [EventSourcingDB: Read-Model Consistency and Lag](https://docs.eventsourcingdb.io/best-practices/read-model-consistency-and-lag/)
- [Marten: Event Store](https://martendb.io/events/)
- [Marten: Projections](https://martendb.io/events/projections/)
- [Sekiban GitHub](https://github.com/J-Tech-Japan/Sekiban)
- [EventFlow GitHub](https://github.com/eventflow/EventFlow)
- [Revo Framework GitHub](https://github.com/revoframework/Revo)
- [Oskar Dudycz: EventSourcing.NetCore](https://github.com/oskardudycz/EventSourcing.NetCore)
- [AWS Prescriptive Guidance: Event Sourcing Pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/event-sourcing.html)

**.NET 10 / Blazor / Aspire / Dapr:**
- [Microsoft Learn: Blazor render modes (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)
- [dotnet-guide: Streaming Rendering, Auto Render Mode & Progressive Enhancement](https://www.dotnet-guide.com/tutorials/blazor/ssr-interactive-islands/)
- [dotnet-guide: Interactive Islands explained](https://www.dotnet-guide.com/articles/blazor-interactive-islands-explained/)
- [dotnetwebacademy: Blazor Prerendering Finally SOLVED in .NET 10](https://dotnetwebacademy.substack.com/p/net-10-finally-fixes-prerendering)
- [Chris Sainty: Server-side and Streaming Rendering](https://chrissainty.com/blazor-in-dotnet-8-server-side-and-streaming-rendering/)
- [Dapr.io](https://dapr.io/)
- [Dapr Docs: Overview](https://docs.dapr.io/concepts/overview/)
- [Dapr Docs: Actors overview](https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/)
- [Speaker Deck: Distributed applications with Orleans + Dapr + Sekiban](https://speakerdeck.com/tomohisa/distributed-applications-made-with-microsoft-orleans-and-dapr-and-event-sourcing-using-sekiban)
- [AspireSoftwareConsultancy: .NET Aspire & Modular Monoliths — 2026 Architecture Guide](https://aspiresoftwareconsultancy.com/dotnet-aspire-modular-monolith/)
- [Cigen: Monolith to Microservices with .NET Aspire](https://www.cigen.io/insights/from-monolith-to-microservices-with-net-aspire-a-modernization-roadmap-for-net-apps)
- [Medium/Asad: .NET Aspire Explained](https://medium.com/@asad072/net-aspire-explained-modern-orchestration-for-cloud-native-net-applications-fa49fe3deed3)
- [Hexalith GitHub](https://github.com/Hexalith/Hexalith)

---

**Research workflow status:** All 5 domain research steps completed. Document is ready for use in downstream workflows (PRD creation, architecture specification, positioning decisions).

