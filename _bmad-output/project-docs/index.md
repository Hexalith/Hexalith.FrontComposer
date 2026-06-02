# Hexalith.FrontComposer — Project Documentation Index

> **Generated:** 2026-06-02 · initial deep scan · **for AI-assisted development.**
> This set lives in `_bmad-output/project-docs/` and is intentionally **separate** from the published DocFX site under `docs/` (which is referenced by product code, tests, CI, and validation fixtures and was left untouched).

👉 **This index is the primary entry point for AI agents.** Point brownfield-PRD / planning workflows at this file.

## Project overview

- **Name:** Hexalith.FrontComposer — *"the Hexalith Blazor Front Shell."*
- **Type:** Monolith (one `.slnx` solution); primary `library`/framework + developer tooling, with **source-generator**, **Blazor-UI**, **MCP-server**, and **CLI** facets.
- **Primary language / runtime:** C# on **.NET 10** (SDK `10.0.300`).
- **Architecture:** Source-generation-driven & layered — a contracts kernel → a Roslyn incremental generator → runtime consumers (Blazor shell, MCP server, CLI), bound by schema fingerprints. See [architecture.md](./architecture.md).
- **In one line:** annotate domain types with `[Projection]`/`[Command]` → the generator emits Blazor views, command forms, Fluxor state, DI registration, and an MCP manifest → the shell composes the UI and the MCP server exposes it to AI agents.

## Quick reference

| | |
|---|---|
| **Solution** | [Hexalith.FrontComposer.slnx](Hexalith.FrontComposer.slnx) (XML `.slnx`; never `.sln`) |
| **Tech stack** | .NET 10 · Blazor + FluentUI v5 (`5.0.0-rc.3-26138.1`) · Fluxor 6.9.0 · Roslyn 5.3.0 generator · MCP SDK 1.3.0 · NUlid · SignalR · OIDC |
| **Packages** | Centralized in [Directory.Packages.props](Directory.Packages.props) (`TreatWarningsAsErrors=true`) |
| **Source projects** | Contracts · Schema · SourceTools · Shell · Mcp · Cli · Testing (7) |
| **Tests** | xUnit **v3**, Shouldly, NSubstitute, bUnit, Verify, FsCheck, PactNet, BenchmarkDotNet + Playwright e2e |
| **Generator entry** | `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` |
| **Shell entry** | `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` → `AddHexalithFrontComposerQuickstart()` |
| **MCP entry** | `src/Hexalith.FrontComposer.Mcp/Extensions/...` → `AddFrontComposerMcp()` / `MapFrontComposerMcp()` |
| **CLI** | `frontcomposer inspect` · `frontcomposer migrate` |
| **Build** | `dotnet build Hexalith.FrontComposer.slnx -c Release` |
| **Test (default lane)** | `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` |
| **Release** | semantic-release → signed NuGet packages (no containers) |

## Generated documentation

- [Project Overview](./project-overview.md) — what it is, the core loop, tech stack, the 7 projects
- [Architecture](./architecture.md) — layering, the generation pipeline, runtime composition, MCP boundary, invariants
- [Source Tree Analysis](./source-tree-analysis.md) — annotated directory tree, critical folders, entry points
- [Component Inventory](./component-inventory.md) — Blazor component catalog + per-project public surface
- [API & Contract Surfaces](./api-contracts.md) — source-generator attribute contract + HFC1001–HFC1070 diagnostics, MCP tools/resources, CLI commands
- [Data Model](./data-models.md) — contract records, registration model, schema fingerprint/baseline/delta types
- [Development Guide](./development-guide.md) — prerequisites, build, test lanes, generator debugging, e2e, samples
- [Deployment / Release Guide](./deployment-guide.md) — CI workflows + the semantic-release / NuGet pipeline
- [Contribution Guide](./contribution-guide.md) — commit conventions, branching, review, must-follow rules
- [Scan report (state)](./project-scan-report.json) — machine-readable scan metadata

## Existing documentation (not regenerated here)

- **Published DocFX site** — [docs/](docs/) (Diataxis: `tutorials/`, `how-to/`, `reference/`, `concepts/`, `diagnostics/`, `migrations/`). Build with `dotnet docfx docs/docfx.json`; validate with `pwsh ./eng/validate-docs.ps1`. Concept overview: `docs/concepts/source-generation-and-mcp-split.md`.
- [README.md](README.md) · [CONTRIBUTING.md](CONTRIBUTING.md) (generator debugging) · [ONBOARDING.md](ONBOARDING.md) (team/Claude usage)
- **Submodule docs** (external deps, not in scope): `Hexalith.Commons`, `Hexalith.EventStore`, `Hexalith.Tenants` — each has its own `CLAUDE.md` / `project-context.md`.

## Getting started (contributors)

1. Clone with **root-level** submodules: `git submodule update --init` (do **not** recurse into nested submodules).
2. `dotnet restore Hexalith.FrontComposer.slnx` → `dotnet build … -c Release`.
3. Run the default test lane (command above). For a11y/visual: `cd tests/e2e && npm ci && npm run test:a11y`.
4. Explore the `samples/Counter` Aspire sample to see a generated shell end-to-end.
5. Read [development-guide.md](./development-guide.md) and [contribution-guide.md](./contribution-guide.md) before changing the generator or the published `docs/` site.

## Top facts agents must not miss

1. **Code is generated** into `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` (a public path contract) — edit the generator/domain types, not the `*.g.*` files.
2. **`docs/` is a published, CI-gated site** referenced by 80+ product/test/CI/fixture sites — never overwrite it; generated docs go in `_bmad-output/project-docs/`.
3. **ULIDs, not GUIDs**; **centralized package versions**; **`TreatWarningsAsErrors=true`**; **`.slnx` only**.
4. **Solution-level `dotnet test`** with trait filters (unlike the EventStore submodule's per-project model); `DiffEngine_Disabled=true` for Verify snapshots.
5. **Submodules are root-level only** and must not be modified without approval.
