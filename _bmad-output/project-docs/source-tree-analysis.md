# Hexalith.FrontComposer — Source Tree Analysis

> **Generated:** 2026-06-02 · deep scan. Annotated tree of FrontComposer's own code; submodules shown but not expanded.

## Repository root

```
frontcomposer/
├── Hexalith.FrontComposer.slnx     # ★ Solution (XML .slnx format — never use .sln)
├── global.json                     # SDK pin: 10.0.300, rollForward latestPatch
├── Directory.Build.props           # LangVersion=latest, Nullable, ImplicitUsings, TreatWarningsAsErrors=true; deps switch
├── Directory.Build.targets         # opt-in NuGet package validation (ApiCompat)
├── Directory.Packages.props        # ★ Centralized package versions (single source of truth)
├── deps.local.props                # Debug/source submodule ProjectReference paths
├── deps.nuget.props                # Release/package NuGet dependency mode
├── nuget.config
├── package.json / package-lock.json# semantic-release + husky + e2e test scripts (Node tooling, not an app)
├── commitlint.config.mjs           # Conventional Commits enforcement
├── .releaserc.json                 # ★ semantic-release pipeline (build/pack/SBOM/sign/attest/publish)
├── README.md                       # (very short)
├── CONTRIBUTING.md                 # source-generator debugging guidance
├── ONBOARDING.md                   # team/Claude usage onboarding (informational)
│
├── src/                            # ★ 7 source projects (see below)
├── tests/                          # ★ 7 xUnit v3 test projects + e2e/ (Playwright)
├── samples/                        # IdeParityCounter sample domain
│
├── eng/                            # build/release engineering scripts (validate-docs.ps1, release_evidence.py, llm_benchmark.py)
├── jobs/                           # scheduled job scripts (ide-parity-version-revalidation.ps1)
├── docs/                           # ★ PUBLISHED DocFX site (Diataxis) — NOT this BMAD output; leave intact
├── artifacts/                      # build/doc artifacts output (e.g. artifacts/docs/*)
├── release-evidence/               # semantic-release evidence (SBOM, manifests, checksums, attestations)
├── evtest/                         # (event/experimental test assets)
│
├── .github/workflows/              # ★ CI: ci, release, nightly, ide-parity-revalidation, mutation-property-nightly, flaky-test-governance, quarantine-governance-nightly
├── .husky/                         # commit-msg hook (commitlint)
├── .mcp.json                       # dev MCP servers: fluent-ui-blazor, aspire
├── .devcontainer/ .config/ .codex/ .agents/ .claude/ .dotnet-home/   # tooling & agent config
├── _bmad/                          # BMAD module config + scripts
├── _bmad-output/project-docs/      # ← THIS generated documentation set
│
└── references/                     # root-declared submodules (external deps) — not expanded
    ├── Hexalith.AI.Tools/
    ├── Hexalith.Builds/
    ├── Hexalith.Commons/
    ├── Hexalith.EventStore/
    ├── Hexalith.Memories/
    ├── Hexalith.PolymorphicSerializations/
    └── Hexalith.Tenants/
```

`★` = critical for understanding/operating the project.

## `src/` — the 7 source projects

```
src/
├── Hexalith.FrontComposer.Contracts/      # LAYER 0 — leaf kernel (net10.0 + netstandard2.0)
│   ├── Attributes/        # [Projection],[Command],[BoundedContext],[ProjectionRole],[ProjectionBadge],
│   │                      #   [ColumnPriority],[ProjectionFieldGroup],[Destructive],[RequiresPolicy],[Icon],[Currency],[RelativeTime]…
│   ├── Communication/     # ICommandService, IQueryService, CommandResult, typed exceptions
│   ├── Lifecycle/         # CommandLifecycleState machine, IUlidFactory, McpLifecycleStateNames
│   ├── Rendering/         # Typography (FcTypoToken), ProjectionContext, slot/template/view descriptors, render contracts
│   ├── Registration/      # IFrontComposerRegistry, DomainManifest (generator output target)
│   ├── Mcp/               # McpManifest, McpCommandDescriptor, McpResourceDescriptor…
│   ├── Schema/            # SchemaFingerprint, CanonicalSchemaMaterial, baseline/delta records, SchemaContractFamily
│   ├── Diagnostics/       # FcDiagnosticIds (HFC0001–HFC4001), dev-mode customization diagnostics
│   ├── Conformance/       # GeneratedOutputPathContract (public generated-path template)
│   ├── Badges/ Shortcuts/ Storage/ Telemetry/ DevMode/   # service contracts
│   └── ContractsMetadata.cs   # TypographyMappingVersion pin
│
├── Hexalith.FrontComposer.Schema/         # LAYER 0 — thin lib on Contracts
│   ├── SchemaContractFamilyNames.cs       # enum → canonical kebab name
│   └── Diagnostics/SchemaMigrationDeltaAnalyzer.cs   # stateless baseline-vs-current delta engine
│
├── Hexalith.FrontComposer.SourceTools/    # LAYER 1 — the Roslyn generator (netstandard2.0)
│   ├── FrontComposerGenerator.cs          # ★ the single [Generator]; wires parse→transform→emit pipelines
│   ├── Parsing/    # AttributeParser, CommandParser, ProjectionTemplateMarkerParser; IR: DomainModel, EquatableArray, FieldTypeMapper
│   ├── Transforms/ # IR → emitter models (RazorModel, FluxorModel, CommandFluxorModel, RegistrationModel, SchemaFingerprintTransform…)
│   ├── Emitters/   # RazorEmitter, FluxorEmitter, RegistrationEmitter, McpManifestEmitter, CommandFormEmitter…
│   └── Diagnostics/# DiagnosticDescriptors (HFC1001–HFC1070), accessibility analyzers, hot-reload classifier
│   └── Drift/      # DriftDetection.cs — options, baseline load/parse, comparison, fact types
│
├── Hexalith.FrontComposer.Shell/          # LAYER 2 — Blazor front shell (net10.0)
│   ├── Components/   # ★ Blazor components (see component-inventory.md): Layout, Navigation, Badges,
│   │                 #   DataGrid, Forms, Home, EventStore, Rendering, DevMode, Diagnostics, Pages, Icons
│   ├── Extensions/   # ★ ServiceCollectionExtensions — AddHexalithFrontComposer(Quickstart), AddHexalithDomain, AddHexalithEventStore
│   ├── State/        # Fluxor feature/state/reducer/effects per slice (Theme, Density, Navigation, CommandPalette, ETagCache, PendingCommands, ProjectionConnection, ReconnectionReconciliation…)
│   ├── Infrastructure/  # EventStore SignalR/HTTP clients, Tenancy, Storage (localStorage), Telemetry
│   ├── Services/     # Auth, authorization evaluator/gate, badge count, customization registries, derived values, lifecycle, projection slot/template/view registries
│   ├── Options/      # FcShellOptions, FrontComposerAuthenticationOptions + validators
│   ├── Badges/ Shortcuts/ Registration/ Routing/
│   └── wwwroot/      # js/ ES modules (fc-density, fc-keyboard, fc-layout-breakpoints, fc-prefers-color-scheme, fc-datagrid, fc-expandinrow…) + css/
│
├── Hexalith.FrontComposer.Mcp/            # LAYER 2 — ASP.NET Core MCP server (net10.0)
│   ├── Extensions/   # ★ AddFrontComposerMcp (DI + MCP SDK), MapFrontComposerMcp (endpoint)
│   ├── Invocation/   # CommandInvoker, ProjectionReader, LifecycleTracker, ToolAdmissionService
│   ├── Schema/       # SchemaNegotiation (McpSchemaNegotiator), runtime baseline/aggregator
│   ├── Rendering/    # Markdown projection renderer
│   ├── Skills/       # ★ SkillCorpus.cs — parser/loader/validators + skill resource provider
│   ├── docs/skills/frontcomposer/   # markdown corpus embedded as assembly resources
│   └── FrontComposerMcpDescriptorRegistry.cs, HttpFrontComposerMcpAgentContextAccessor.cs, FrontComposerMcpOptions.cs
│
├── Hexalith.FrontComposer.Cli/            # LAYER 2 — frontcomposer dotnet tool (net10.0, flat folder)
│   ├── Program.cs            # top-level statements, Ctrl+C handling → CliApplication.RunAsync
│   ├── CliApplication.cs     # ★ command dispatch + help + exit-code rules
│   ├── InspectCommand.cs     # inspect: read generated output + *.diagnostics.json sidecars
│   ├── MigrationCommand.cs   # migrate: MigrationCatalog edges + Roslyn code-fix planner/applier
│   ├── ExitCodes.cs, OutputSanitizer.cs, CommandOptions.cs, ProjectSelection.cs, PathUtilities.cs
│   └── README.md             # user-facing CLI docs (exit codes, JSON schema, migration notes)
│
└── Hexalith.FrontComposer.Testing/        # LAYER 2 — adopter bUnit test host (net10.0, publishable)
    ├── FrontComposerTestBase.cs           # abstract BunitContext base for adopter tests
    ├── FrontComposerTestHostBuilder.cs    # ★ AddFrontComposerTestHost wiring + fakes
    ├── Evidence.cs                        # evidence records + RedactedEvidenceFormatter
    └── (Test* fakes, data builders, GeneratedProjectionAssertions, CommandEvidenceAssertions)
```

## `tests/` — test projects (xUnit v3)

```
tests/
├── Hexalith.FrontComposer.Contracts.Tests/     # contracts-layer unit tests (no bUnit)
├── Hexalith.FrontComposer.Shell.Tests/         # ★ largest: bUnit component tests, Fluxor E2E, Pact, a11y,
│                                               #   Architecture/ tripwires, Governance/, SlotMappingRegressionTests
├── Hexalith.FrontComposer.Shell.Tests.Bench/   # standalone BenchmarkDotNet exe (PaletteScorerBench)
├── Hexalith.FrontComposer.SourceTools.Tests/   # generator tests: CompilationHelper + Verify snapshots, drift
├── Hexalith.FrontComposer.Mcp.Tests/           # MCP hosting/auth/manifest/skill/schema/invocation tests (NSubstitute)
├── Hexalith.FrontComposer.Cli.Tests/           # CLI inspect/migrate via CliApplication.RunAsync + CliFixture
├── Hexalith.FrontComposer.Testing.Tests/       # self-verification + PackageBoundaryTests (public API lock)
└── e2e/                                         # Playwright e2e (npm) — a11y + visual suites (see package.json)
```

## Entry points

| Entry point | File | Purpose |
|---|---|---|
| **Source generator** | `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` → `Initialize()` | Hooks into every consuming compilation; the producer of all generated code. |
| **Shell DI** | `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` → `AddHexalithFrontComposer*` | How an app adopts the shell. |
| **Shell UI root** | `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` | The composed application frame. |
| **MCP server** | `src/Hexalith.FrontComposer.Mcp/Extensions/...` → `AddFrontComposerMcp` / `MapFrontComposerMcp` | Stands up the MCP endpoint (default `/mcp`). |
| **CLI** | `src/Hexalith.FrontComposer.Cli/Program.cs` → `CliApplication.RunAsync` | `frontcomposer inspect` / `frontcomposer migrate`. |
| **Test host** | `src/Hexalith.FrontComposer.Testing/FrontComposerTestHostBuilder.cs` → `AddFrontComposerTestHost` | Adopter-facing bUnit harness. |

## Critical folders to know

1. **`Directory.Packages.props`** — the only place package versions live.
2. **`src/Hexalith.FrontComposer.SourceTools/`** — the generator; the source of all `*.g.*` files.
3. **`src/Hexalith.FrontComposer.Contracts/`** — the vocabulary the generator and runtime share.
4. **`src/Hexalith.FrontComposer.Shell/Components/`** + **`State/`** — the runtime UI and its Fluxor store.
5. **`src/Hexalith.FrontComposer.Mcp/Skills/`** + **`docs/skills/frontcomposer/`** — the AI-agent doc surface.
6. **`docs/`** — the **published DocFX site** (referenced by product code, tests, CI, validation fixtures). Treat as production content; do not intermix generated scratch docs here.
7. **`eng/`** + **`.github/workflows/`** + **`.releaserc.json`** — build, validation, and release automation.
