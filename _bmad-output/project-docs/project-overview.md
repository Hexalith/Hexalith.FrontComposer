# Hexalith.FrontComposer — Project Overview

> **Generated:** 2026-06-02 · **Scan mode:** initial / deep · **Scope:** FrontComposer own code (`src/`, `tests/`, `samples/`). The three git submodules (Hexalith.Commons, Hexalith.EventStore, Hexalith.Tenants) are treated as external dependencies — see [data-models.md](./data-models.md) and [architecture.md](./architecture.md).
>
> This documentation set is generated **for AI-assisted development**. It lives in `_bmad-output/project-docs/` and is intentionally separate from the published DocFX site under `docs/`.

## What it is

**Hexalith.FrontComposer** is *"the Hexalith Blazor Front Shell"* — a .NET 10 framework that **generates admin/operations front-ends from annotated domain types**. You annotate plain C# projection (read-model) and command types with attributes such as `[Projection]` and `[Command]`; a single **Roslyn incremental source generator** emits the Blazor views, command forms, Fluxor state plumbing, DI registrations, and an **MCP manifest**. A runtime **Blazor shell** composes the generated UI into a complete application frame, and an **MCP server** exposes the very same domain surface to AI agents.

In one sentence: *write domain types → get a Blazor UI, an MCP tool surface, and CLI tooling, all kept in sync by code generation and schema-fingerprint drift detection.*

## The core loop

```
[Projection]/[Command] domain types          (you write these)
            │
            ▼  Roslyn incremental generator (Hexalith.FrontComposer.SourceTools)
            │
   ┌────────┴───────────────────────────────────────────────┐
   ▼                    ▼                     ▼               ▼
 Blazor views      Fluxor state         DI registration   MCP manifest
 + command forms   (features/actions/   (RegisterDomain)  (FrontComposerMcpManifest.g.cs)
                    reducers)
   │                    │                     │               │
   └──────────┬─────────┴─────────────────────┘               │
              ▼                                                ▼
   Blazor Front Shell (Hexalith.FrontComposer.Shell)   MCP server (Hexalith.FrontComposer.Mcp)
   composes nav + layout + DataGrids + dialogs,        exposes commands as tools + projections
   talks to EventStore via SignalR/HTTP                & docs (skill corpus) as resources to agents
```

The `frontcomposer` **CLI** (`Hexalith.FrontComposer.Cli`) lets you `inspect` the generated output and diagnostics and `migrate` it across versions. The **Testing** library (`Hexalith.FrontComposer.Testing`) gives adopters a pre-wired bUnit host with deterministic fakes for testing generated components.

## Technology stack

| Category | Technology | Version | Notes |
|---|---|---|---|
| Runtime / SDK | .NET | 10 (SDK `10.0.300`, `rollForward: latestPatch`) | pinned in [global.json](global.json) |
| Language | C# `latest` | — | `Nullable`, `ImplicitUsings` enabled; **`TreatWarningsAsErrors=true`** ([Directory.Build.props](Directory.Build.props)) |
| Target frameworks | `net10.0`; `net10.0` + `netstandard2.0` | — | Contracts & SourceTools multi-target so the Roslyn analyzer host (netstandard2.0) can reference contracts |
| UI | Microsoft.FluentUI.AspNetCore.Components (FluentUI v5) | `5.0.0-rc.3-26138.1` | exact pin (ADR-003); v5 RC |
| State management | Fluxor.Blazor.Web | `6.9.0` | single-writer discipline per state slice |
| Source generation | Microsoft.CodeAnalysis.CSharp (Roslyn) | `5.3.0` | incremental generator on netstandard2.0 |
| MCP | ModelContextProtocol.AspNetCore | `1.3.0` | HTTP streamable transport |
| Identifiers | NUlid (ULID) | `1.7.3` | **ULIDs, never GUIDs**, for correlation IDs |
| Real-time | Microsoft.AspNetCore.SignalR.Client | `10.0.8` | EventStore projection subscriptions |
| Auth | Microsoft.AspNetCore.Authentication.OpenIdConnect | `10.0.8` | host-owned OIDC |
| Reactive | System.Reactive | `6.1.0` | badge-count producer/consumer isolation |
| Orchestration | Aspire.Hosting.AppHost | `13.4.0` | local topology |
| Testing | xUnit **v3** `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit `2.7.2`, Verify `31.19.0`, FsCheck.Xunit.v3 `3.3.3`, PactNet `5.0.1`, BenchmarkDotNet `0.15.8`, coverlet `10.0.1` | — | see [development-guide.md](./development-guide.md) |
| Packages | Centralized | — | [Directory.Packages.props](Directory.Packages.props), `ManagePackageVersionsCentrally=true` |

## Repository classification

- **Repository type:** Monolith — one cohesive .NET solution ([Hexalith.FrontComposer.slnx](Hexalith.FrontComposer.slnx)) with 7 source projects, 7 test projects, and 1 sample.
- **Primary project type:** `library` (NuGet-published .NET framework) with **source-generator**, **Blazor-UI**, **MCP-server**, and **CLI** facets.
- **Architecture style:** Source-generation-driven, layered: a leaf *contracts* kernel → a *generator* that emits code → a *runtime shell* + *MCP adapter* that consume the generated artifacts; schema **fingerprints** bind producer (generator) and consumers (shell, MCP, CLI) together.

## The 7 source projects

| Project | Role | Type / TFM |
|---|---|---|
| `Hexalith.FrontComposer.Contracts` | Shared kernel: attributes, command/query/lifecycle interfaces, rendering model, `DomainManifest`, MCP descriptors, schema fingerprint/baseline/delta types, diagnostic IDs | library, `net10.0`+`netstandard2.0` |
| `Hexalith.FrontComposer.Schema` | Schema family names + stateless `SchemaMigrationDeltaAnalyzer` (compat decisions) | library |
| `Hexalith.FrontComposer.SourceTools` | The single Roslyn incremental generator (`FrontComposerGenerator`) + drift detection | analyzer, `netstandard2.0` |
| `Hexalith.FrontComposer.Shell` | The Blazor front-shell UI library (layout, nav, DataGrid, dialogs, Fluxor state, EventStore clients) | Blazor library, `net10.0` |
| `Hexalith.FrontComposer.Mcp` | ASP.NET Core MCP server exposing commands/projections/docs to AI agents | web library, `net10.0` |
| `Hexalith.FrontComposer.Cli` | `frontcomposer` dotnet tool: `inspect` + `migrate` | tool, `net10.0` |
| `Hexalith.FrontComposer.Testing` | Adopter bUnit test host + deterministic fakes | library, `net10.0` |

## Documentation map

| Document | Contents |
|---|---|
| [architecture.md](./architecture.md) | System architecture, layering, the generation pipeline, cross-cutting invariants |
| [source-tree-analysis.md](./source-tree-analysis.md) | Annotated directory tree, critical folders, entry points |
| [component-inventory.md](./component-inventory.md) | Per-project public surface + the full Blazor component catalog |
| [api-contracts.md](./api-contracts.md) | The "API" surfaces: source-generator attribute contract + emitted files + HFC diagnostics, MCP tools/resources, CLI commands |
| [data-models.md](./data-models.md) | Domain/contract data model: `DomainManifest`, schema contracts, fingerprints, baselines, deltas |
| [development-guide.md](./development-guide.md) | Prerequisites, build, test tiers, source-generator debugging, local run |
| [deployment-guide.md](./deployment-guide.md) | CI workflows + the semantic-release / NuGet publish pipeline |
| [contribution-guide.md](./contribution-guide.md) | Commit conventions, branching, review process, generator-debugging rules |
| [index.md](./index.md) | Master navigation index (primary AI entry point) |

## Key facts an agent should never miss

1. **Code is generated.** Most Blazor views, command forms, Fluxor state, and the MCP manifest are *emitted* into `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`. Don't hand-edit generated files; change the source generator or the annotated domain types. The generated-output path is a **public contract** (`GeneratedOutputPathContract.Template`).
2. **ULIDs, not GUIDs**, for `messageId`/`correlationId` (NUlid; 26-char Crockford base32).
3. **`TreatWarningsAsErrors=true`** everywhere — analyzer/style warnings break the build.
4. **Centralized package versions** — never add a `Version=` to a `.csproj`; edit [Directory.Packages.props](Directory.Packages.props).
5. **Submodules are root-level only** and must not be modified without approval; `docs/` is a published site (do not treat it as scratch space).
6. **Schema fingerprints** bind generator output to runtime/MCP/CLI; changing canonical serialization silently invalidates baselines.
