---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
status: 'complete'
completedAt: '2026-04-12'
lastStep: 8
partyModeFindings:
  step7Contributors:
    - Winston (Architect) — 3 contract ambiguities (IRenderer skeleton, Fluxor state convention ADR-008, diagnostic condition table); recommended resolve before parallel agent work
    - Amelia (Developer) — W1 scaffold sequence (7 steps), 5 binary decisions resolved (HFC2002=Error, timeout=30s, xUnit v2, CI in W1, EventStore path base)
    - Murat (Test Architect) — test infrastructure conventions (fixtures, naming, builders), field type coverage matrix (29 tests), CI failure thresholds (Stryker 70%→85%, FsCheck 1000), flaky quarantine mechanics, xUnit v2 pin
    - Barry (Quick Flow Solo Dev) — doc usability critique, companion artifacts recommendation (cheat sheet + phase checklist + code PATTERN comments), treat architecture doc as snapshot not living document
  step6Contributors:
    - Winston (Architect) — extraction seam validation, DI smoke test for seam extraction, Pipeline seam-ignorance constraint
    - Amelia (Developer) — 7 MSBuild issues (Directory.Build.props walk-up isolation, consumer.props import chain, API surface gate, Directory.Packages.props day-1 pins, nested submodule path resolution, PrivateAssets="all", ADR-005 reconciliation)
    - Murat (Test Architect) — Pact anchored to Shell.Tests not floating dir, Contracts.Tests multi-TFM execution, FsCheck mapped to SourceTools.Tests, LLM benchmark threshold, flaky quarantine automation, E2E smoke tests (attribute→generator→render)
    - Barry (Quick Flow Solo Dev) — phase-tagged blueprint (W1/W2/v0.1/v0.3), W1 cut to 6 .csproj, deferred Shell.Tests/AppHost/consumer.props to W2, ceremony-to-value ratio enforcement
  step4Contributors:
    - Winston (Architect) — generator output sequencing (Razor+Fluxor v0.1, Specimen v0.2, MCP v0.3), generated manifest for deterministic discovery
    - Amelia (Developer) — generator emits Fluxor as strings (no dependency), Path B subscription wiring (no base class), FlushAsync for IStorageService
    - Murat (Test Architect) — ProjectionRenderer<T> 4-layer defense-in-depth, snapshot tests string-only, registry unit+integration, circuit-breaker error boundary
  step3Contributors:
    - Winston (Architect) — dependency graph correction (Shell/EventStore as peers), ADR-002 simplification (two-file import), ADR-007 diagnostic policy, Contracts CI gate
    - Amelia (Developer) — SetTargetFramework fix, EnforceExtendedAnalyzerRules, test project reference chain, NuGet packaging prep, ILMerge constraint
    - Barry (Quick Flow Solo Dev) — 5 projects for v0.1 not 14, IR pattern is code organization not project boundaries, submodule mode as default
  step2Contributors:
    - Winston (Architect) — Blazor Auto as first-class constraint, multi-tenancy characterization, package dependency graph, MCP interaction contract, DAPR abstraction position, FR prioritization
    - Amelia (Developer) — render mode lifecycle concern, generator diagnostics concern, DI scope divergence, submodule+lockstep conflict, hot reload limitation, ServiceLifetimeMatrix requirement
    - Murat (Test Architect) — source generator IR pattern for testability, Pact cross-repo coordination, SignalR abstraction as load-bearing, testability strategy per concern, test maintenance burden
    - Barry (Quick Flow Solo Dev) — v0.1/v1/v1.x partitioning, 10-12 components realistic for v1, 5-6 for v0.1, defer multi-tenancy/caching/observability/resilience
inputDocuments:
  - _bmad-output/A-Product-Brief/project-brief.md
  - _bmad-output/planning-artifacts/prd/index.md
  - _bmad-output/planning-artifacts/prd-summary-card.md
  - _bmad-output/planning-artifacts/prd-validation-report.md
  - _bmad-output/planning-artifacts/ux-design-specification/index.md
  - _bmad-output/planning-artifacts/research/technical-fluentui-blazor-v5-research-2026-04-06.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-eventstore-front-ui-communication-research-2026-04-06.md
  - _bmad-output/planning-artifacts/research/domain-dotnet-modular-frameworks-event-sourcing-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-event-sourcing-ecosystem-adoption-trends-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-microfrontend-composition-patterns-research-2026-04-11.md
  - _bmad-output/planning-artifacts/research/domain-model-driven-ui-generation-research-2026-04-11/index.md
workflowType: 'architecture'
project_name: 'Hexalith.FrontComposer'
user_name: 'Jerome'
date: '2026-04-12'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

*Reviewed by Party Mode: Winston (Architect), Amelia (Developer), Murat (Test Architect), Barry (Quick Flow Solo Dev). Key enhancements: Blazor Auto elevated to first-class constraint, two new cross-cutting concerns added, testability strategy per concern, phased scope partitioning applied.*

### Requirements Overview

**Functional Requirements:**

82 FRs across 9 capability areas, partitioned by delivery phase:

| Capability Area | FR Count | v0.1 (Week 4) | v1 Ship | v1.x Defer |
|---|---|---|---|---|
| Domain Auto-Generation (FR1-12) | 12 | FR1, FR2, FR3, FR6 | FR4-5, FR7-12 | — |
| Composition Shell & Navigation (FR13-22) | 10 | FR13, FR15 | FR14, FR16-22 | — |
| Command Lifecycle & ES Communication (FR23-38) | 16 | FR23 (basic), FR32 | FR23-36, FR38 | FR37 (multi-IdP) |
| Developer Customization (FR39-48) | 10 | — | FR39-45, FR47-48 | FR42 (full replacement), FR46 |
| Multi-Surface Rendering & Agent (FR49-61) | 13 | FR49 (stub), FR51 (stub) | FR49-56, FR58-61 | FR57 (runtime agent parity) |
| Developer Experience & Tooling (FR62-71) | 10 | FR62 (sample, not template) | FR62-66, FR68, FR70-71 | FR63-64 (CLI), FR67, FR69 |
| Observability (FR72-73) | 2 | — | FR72 (basic), FR73 | FR72 (full OpenTelemetry) |
| Release Automation (FR74-77) | 4 | — | FR74-77 | — |
| Test Infrastructure (FR78-82) | 5 | — | FR78-82 | — |

**v0.1 scope (week 4):** ~10 FRs. Counter domain: `[Command]` + `[Projection]` + `[BoundedContext]` attributes, source generator (1-input-1-output, Razor only), `ICommandService` + `IQueryService`, SignalR subscription, basic lifecycle (spinner, not five-state), hand-rolled MCP stub (1 command, 1 hallucination rejection), 10-prompt LLM benchmark signal.

**v1 ship:** ~55-60 FRs. Full generator (1-source-3-outputs), five-state lifecycle, customization gradient (annotation + template + slot), Hexalith native chat alpha, 3 reference microservices, full test infrastructure.

**v1.x defer:** ~15-20 FRs. Full CLI tools, multi-IdP auth, runtime agent full parity, full OpenTelemetry instrumentation, full-replacement customization level.

**Non-Functional Requirements:**

NFRs that drive architectural decisions, organized by when they become load-bearing:

| NFR Category | Key Constraint | Phase | Architectural Impact |
|---|---|---|---|
| **Performance** | P95 <800ms command-to-confirmed (cold); <500ms incremental generator | v0.1+ | Lifecycle wrapper design; generator caching via `ForAttributeWithMetadataName` |
| **Security** | Zero PII at framework layer; JWT + tenant isolation; MCP hallucination rejection | v1 | Clear data boundary; tenant-scoped everything; typed contract validation |
| **Accessibility** | WCAG 2.1 AA, CI-enforced (axe-core + specimen verification) | v1 | Component model must enforce accessibility contracts; Roslyn analyzers |
| **Reliability** | SignalR auto-reconnect + batched reconciliation; zero silent failures | v1 | Fault-tolerant communication layer with testable abstraction (per Murat) |
| **Testability** | 80% unit on core; Pact contracts; Stryker mutation; FsCheck | v1 | Source generator needs IR extraction pattern; cross-repo Pact coordination |
| **Build/CI** | <5min inner loop, <12min full CI, <45min nightly | v0.1+ | Layered test strategy; incremental-only CI gates; Stryker budget allocation |
| **Deployment** | On-premise, sovereign cloud, Azure/AWS/GCP; zero DAPR bypass | v1 | DAPR-only infrastructure coupling; portable topology |
| **Maintainability** | Lockstep versioning; diagnostic IDs; deprecation policy | v1 | Package dependency graph; clear API surface layers |

### First-Class Architectural Constraints

**1. Blazor Auto Render Mode (elevated from risk to constraint per Winston + Amelia)**

Blazor Auto means components render server-side first (SignalR circuit), then transition to WebAssembly. This is not a deployment detail — it shapes half the architecture:

- **State survival:** The five-state command lifecycle wrapper must handle the Server→WASM handoff. State in the circuit does not automatically transfer. If a command is in `submitting` state during the transition, the SignalR connection drops and re-establishes. The wrapper needs explicit handoff-safe guarantees.
- **DI scope divergence:** Server uses scoped-per-circuit, WASM uses scoped-per-app (effectively singleton). Any service holding mutable state (session context, tenant context, command tracker) behaves differently. Architecture must produce a **ServiceLifetimeMatrix** documenting which services are safe for which lifetime.
- **SignalR connection differences:** Server-side already has a circuit connection; WASM needs to establish one. `[StreamRendering]` interacts with Auto — if a component streams on Server then switches to WASM, the stream terminates. Projection nudges via SignalR need reconnection logic per render mode transition, not just per page load.
- **ETag cache storage:** localStorage is a WASM-only capability. During server-side prerender, the ETag cache strategy must account for no localStorage access. Architecture must specify the storage adapter pattern.
- **`[PersistentState]` attribute** (.NET 10) eliminates ~25 lines of manual state serialization per component — architecture should mandate its use for all cross-render-mode state.

**2. Source Generator as Infrastructure (elevated from feature to constraint per Amelia + Murat)**

The source generator (`IIncrementalGenerator`) is not a feature — it is infrastructure that affects every other feature's debuggability, build characteristics, and testability:

- **Three outputs from one pipeline:** Three `RegisterSourceOutput` calls off one `IncrementalValuesProvider`. If the Razor output has a syntax error, all three fail silently in IDE — no red squiggles, just missing generated files. Diagnostics must be first-class with the HFC diagnostic catalog.
- **IR extraction pattern (per Murat):** The generator must have a pure-function core that maps parsed attribute metadata to an intermediate representation (IR) before emitting C#. Without this, 90% of tests become integration-level `CSharpGeneratorDriver` roundtrips and the 70% unit test target is impossible. Stryker mutation testing requires pure functions that return data, not side-effecting code.
- **Cross-assembly discovery:** If the EventStore domain lives in a separate assembly (it does — Hexalith.EventStore is a git submodule), Roslyn source generators only see the current compilation by default. Architecture must specify whether we use assembly metadata, analyzer+generator pairs, or require domain types to be in the same compilation.
- **Hot reload limitation:** Razor output from source generators is partial-class `.razor.g.cs`, not `.razor`. Blazor tooling hot reload will NOT pick up changes to generator inputs. Developer must rebuild. This is a DX constraint to document, not discover in sprint 2.
- **IDE incremental performance:** The generator must be incremental-pipeline compatible (`ForAttributeWithMetadataName` + incremental caching) or VS/Rider will crawl. Budget: 500ms incremental per domain assembly.

**3. Solo-Maintainer Sustainability (governing constraint per Barry)**

Every architectural decision must pass: "Can Jerome sustain this at 2am after a release for 12 months?"

- **Component count:** Target 10-12 major components for v1, with 5-6 landing in v0.1. Not 15-20.
- **Extension points over implementations:** Design interfaces and abstractions for v1.x features now; implement them later. The architecture should support v0.1 cleanly and not prevent v1, but not solve v1 problems on day one.
- **Test maintenance burden:** Visual specimen baselines (Fluent UI patches can shift 1px), Pact contracts (cross-repo coordination), FsCheck shrunk cases (regression fixtures) — each has ongoing maintenance cost.

### Technical Constraints & Dependencies

**Fixed stack (non-negotiable):**

- .NET 10 + Blazor Auto (Server dev loop, Auto production)
- Fluent UI Blazor v5 (RC1, Feb 2026) — zero-override strategy. **Risk:** RC means breaking changes possible at GA. Architecture must identify which Fluent UI APIs are load-bearing (`FluentLayout`, `DefaultValues`, `FluentDataGrid` native HTML, `<FluentProviders />`) and flag them.
- Hexalith.EventStore — REST API (commands/queries) + SignalR (projection change nudges). Git submodule.
- DAPR 1.17.7+ — all infrastructure through DAPR component bindings
- MCP — agent tool exposure protocol

**DAPR abstraction position (resolved per Winston):**

DAPR IS the abstraction layer. FrontComposer abstracts behind DAPR's APIs, not behind a custom abstraction on top of DAPR. Adding our own wrapper layer on DAPR is over-engineering for a solo maintainer. The "zero direct infrastructure coupling" constraint means: no Redis, Kafka, Postgres, CosmosDB references — all through DAPR components. DAPR itself is a permitted direct dependency.

**EventStore communication contract:**

- Two channels: REST (`POST /api/v1/commands` → 202 Accepted, `POST /api/v1/queries` → 200 + ETag) + SignalR hub (`/projections-hub` → lightweight `ProjectionChanged` nudges)
- Client re-queries via REST with ETag for actual data after nudge
- ULID message IDs for command idempotency
- Constraints: Max 10 `If-None-Match` per request; 1MB max body; no colons in ProjectionType/TenantId/domain names (DAPR actor ID separator)

**Package dependency graph (required artifact per Winston + Amelia):**

```
Hexalith.FrontComposer.Contracts  ← dependency-free, most stable, change-controlled
    ↑
Hexalith.FrontComposer.SourceTools  ← references Contracts for attribute types; analyzer+generator
    ↑
Hexalith.FrontComposer.Shell  ← references Contracts; Fluent UI v5 components
    ↑
Hexalith.FrontComposer.EventStore  ← references Contracts; Hexalith.EventStore client
    ↑
Hexalith.FrontComposer.Mcp  ← references Contracts; MCP server + skill corpus
    ↑
Hexalith.FrontComposer.Aspire  ← references Shell, EventStore; hosting extensions
    ↑
Hexalith.FrontComposer.Testing  ← references all; test utilities
    ↑
Hexalith.FrontComposer  ← meta-package pulling Shell + Contracts + SourceTools + EventStore
```

`Contracts` is the most stable package — changes cascade everywhere. It must be treated as change-controlled with explicit review.

**Submodule + lockstep versioning (per Amelia):** Hexalith.EventStore and Hexalith.Tenants are git submodules. Submodule pins to commit; NuGet packages version independently. CI must reconcile: submodule update triggers NuGet compatibility check. These are two versioning systems — the architecture must specify which is authoritative for API compatibility.

### Multi-Tenancy Characterization (per Winston)

Multi-tenancy in FrontComposer v1 is **logical isolation with tenant discriminator**, not data isolation:

- **Data isolation:** No. DAPR state stores and EventStore are shared; tenant discrimination is at the DAPR actor key level (`{projectionType}:{tenantId}`).
- **UI isolation:** Minimal. Single accent color across all contexts in v1. No per-tenant theming or branding.
- **Configuration isolation:** No. No tenant-specific feature flags in v1.
- **What IS tenant-scoped:** JWT `TenantId` claim propagated through all commands, queries, SignalR subscriptions, ETag cache keys (`{tenantId}:{userId}`), and MCP tool enumeration. Tenant isolation at the framework layer means cross-tenant data visibility is a security bug.

For v0.1, **single-tenant only** (per Barry). Multi-tenant support is v1, not v0.1.

### MCP Interaction Model (per Winston)

The MCP server exposes the domain model as typed agent tools:

- **Commands as tools:** Agent calls `ConsolidateShipments.Execute` with typed parameters → MCP server validates against FluentValidation rules → dispatches to EventStore → returns `{commandId, status: "acknowledged", subscribeUri}`.
- **Projections as resources:** Agent calls `ShipmentProjection.Query` with filter parameters → MCP server queries EventStore → returns structured Markdown table.
- **Lifecycle subscription:** Separate `lifecycle/subscribe` tool for polling state transitions. Terminal states guaranteed; intermediate states advisory.
- **Hallucination rejection:** Unknown tool names rejected at the contract boundary with suggestion + tenant-scoped tool list. Mechanism: schema validation against source-generator-emitted tool manifest (compile-time types, not runtime reflection).
- **Skill corpus:** `Hexalith.FrontComposer.Skills` — Markdown files + attribute references discoverable as MCP resources. Versioned with the framework.

### Cross-Cutting Concerns (11 total)

| # | Concern | Phase | Testability Strategy |
|---|---|---|---|
| 1 | **Multi-tenancy** — TenantId from JWT through all operations; single-tenant in v0.1 | v1 | Multi-tenant fixture with 2+ tenants; isolation assertions on every query |
| 2 | **Five-state command lifecycle** — rendering-agnostic contract across web + chat | v0.1 (basic) → v1 (full) | State machine unit tests; bUnit component tests per state; Playwright lifecycle assertions |
| 3 | **Schema evolution** — schema hashes, version negotiation, graceful degradation | v1 | Bidirectional deserialization matrix (v1.0 event × v1.N code); versioned test fixture archive |
| 4 | **Session persistence** — localStorage, tenant+user scoped | v1 | bUnit does NOT have localStorage — requires abstraction (`IStorageService`) with in-memory test double |
| 5 | **Accessibility enforcement** — WCAG 2.1 AA, Roslyn analyzers + axe-core CI | v1 | Roslyn analyzer tests verify emission conditions per diagnostic; axe-core at Playwright level (test-time, not build-time) |
| 6 | **Teaching error messages** — Expected/Got/Fix/DocsLink template | v0.1+ | Each HFC diagnostic ID has a test verifying emission conditions and message completeness |
| 7 | **LLM compatibility** — typed partials, skill corpus, hallucination rejection | v0.1 (stub) → v1 (full) | LLM benchmark suite (nightly); hallucination rejection unit tests; skill corpus version drift CI check |
| 8 | **Authentication/Authorization** — OIDC/SAML, JWT, RequiresPolicy | v1 | Fake identity provider in integration tests; `[RequiresPolicy]` unit-tested via attribute inspection |
| 9 | **Zero infrastructure coupling** — DAPR only (DAPR IS the permitted abstraction) | v0.1+ | CI automated check for banned direct references; DAPR test kit (`Dapr.Client.Testing`) for doubles |
| 10 | **Render Mode Lifecycle** *(new, per Amelia)* — Server→WASM transition; state, DI scopes, connections | v0.1+ | Render-mode abstraction in bUnit; `ServiceLifetimeMatrix` as architecture artifact; `[PersistentState]` for cross-mode state |
| 11 | **Generator Diagnostics & DX** *(new, per Amelia)* — error messages, incremental rebuild, IDE matrix | v0.1+ | `CSharpGeneratorDriver` integration tests; IDE-specific test fixtures (VS vs Rider vs VS Code); diagnostic catalog completeness check |

**Test infrastructure architectural prerequisites (per Murat):**

1. **Source generator IR pattern** — pure-function core mapping metadata → IR → emitted C#. Required for 70% unit test ratio and Stryker mutation testing.
2. **SignalR testable abstraction** — production `HubConnection` behind an interface/wrapper for fault injection. This is architecture, not test utility. Drives DI registration model.
3. **Pact cross-repo coordination** — file-based Pact contracts checked into the FrontComposer repo (not Pact Broker — solo-maintainer overhead). Provider verification runs in FrontComposer CI against EventStore submodule.
4. **FsCheck conventions** — bounded command vocabulary for shrinkable generation; deterministic seed in CI, random in nightly; shrunk failing cases saved as regression fixtures.
5. **Flaky-test quarantine** — xUnit custom trait + GitHub Actions matrix: main lane (no quarantined tests) + quarantine lane (warnings only) + reintroduction gate (10 consecutive passes).

### v0.1 Architecture Targets (Week 4)

Per Barry's prioritization — the 5 things the architecture must enable by week 4:

1. **Core composition model** — the data structure representing a composable UI surface from event-sourced domain types. The atom everything builds on.
2. **EventStore integration seam** — how projections from Hexalith.EventStore feed into UI state. The novel part. Spike early.
3. **Single surface renderer** — Blazor rendering from event-sourced state end-to-end. Counter domain: `IncrementCommand` form + `CounterProjection` DataGrid.
4. **Command dispatch from UI** — return path closing the loop: user click → command → EventStore → projection update → SignalR nudge → UI refresh.
5. **Package structure** — 5 projects scaffolded with the corrected dependency graph. Cheap to do early, expensive to fix later.

## Starter Template Evaluation

*Enhanced via Advanced Elicitation (Failure Mode Analysis, ADR methodology, Pre-mortem, First Principles, Self-Consistency) and Party Mode (Winston, Amelia, Barry). 7 ADRs established. Project count reduced from 14 to 5 for v0.1.*

### Primary Technology Domain

**.NET 10 monorepo framework** — starts with 5 projects (v0.1), expands to 8 at v0.3 when package boundaries add consumer value. Roslyn source generators, Blazor Auto composition shell, DAPR infrastructure. No existing `dotnet new` template fits.

### Verified Current Versions (April 2026)

| Dependency | Current Version | PRD Assumption | Status |
|---|---|---|---|
| **.NET SDK** | 10.0.5 (GA, LTS) | .NET 10 | Aligned |
| **Fluent UI Blazor** | **v5 still RC/preview**; v4.12.2 stable | v5 GA | **Risk — see ADR-003** |
| **DAPR** | 1.17.4 | 1.17.7+ | **Pin to 1.17.4** |
| **.NET Aspire** | 13.2.1 | Aspire | Aligned |
| **xUnit** | v3 3.2.2 | xUnit | Aligned |
| **bUnit** | 2.7.2 | bUnit | Aligned |
| **Stryker.NET** | 4.14.0 | Stryker | Aligned |
| **Microsoft.CodeAnalysis.CSharp** | Pin ≥4.12.0 | — | Required for .NET 10 syntax in generator parser |

### Structural Architecture Decision Records

**ADR-001: Contracts Multi-Targeting**

- **Decision:** Contracts multi-targets `<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>`. Single source of truth for attribute definitions.
- **Constraint:** Contracts cannot use net10.0-only APIs — attributes and interface contracts don't need them.
- **CI gate:** Build Contracts targeting netstandard2.0 alone as a separate validation step.

**ADR-002: Submodule vs NuGet for EventStore**

- **Decision:** Two-file import pattern. `deps.local.props` (submodule ProjectReferences) and `deps.nuget.props` (PackageReferences). A single boolean in `Directory.Build.props` selects which file to import.
- **Default:** Submodule mode for active development (both local and CI). NuGet mode only for release validation builds.
- **Rationale:** Scattered MSBuild conditionals create invisible failures. Two files, one boolean, full visibility when debugging.

**ADR-003: Fluent UI v5 RC Risk**

- **Decision:** Build on v5 RC. Pin exact version in `Directory.Packages.props`. No abstraction layer.
- **Risk acceptance:** If v5 GA introduces breaking changes, budget 1-2 weeks migration.
- **Mitigation:** Weekly canary build against latest RC drop. Subscribe to fluentui-blazor releases for GA notification.

**ADR-004: Source Generator IR Pattern**

- **Decision:** Three-stage pipeline within a single project (code organization, not project boundaries):
  1. **Parse** — Roslyn `INamedTypeSymbol` → `DomainModel` IR (pure data records). Folder: `SourceTools/Parsing/`
  2. **Transform** — `DomainModel` → surface-specific output models (`RazorModel`, `McpManifestModel`, `SpecimenModel`). Folder: `SourceTools/Transforms/`
  3. **Emit** — output models → string source code. Folder: `SourceTools/Emitters/`
- **Testability:** Parse and Transform are pure functions → 90% unit tests. Emit → snapshot/golden-file tests. Stryker targets Parse and Transform only.

**ADR-005: Progressive Project Structure**

- **v0.1 (week 1-4): 5 projects.** Contracts, SourceTools, Shell (includes EventStore/Mcp/Aspire code until extraction), one test project, one sample. Ship fast, split when pain demands it.
- **v0.3+: Expand to 8 projects** when package boundaries add consumer value. Extract EventStore, Mcp, Aspire, Testing into separate projects. Begin NuGet publishing.
- **Rationale:** 14 projects on day 1 means debugging MSBuild instead of writing generator logic. Jerome IS the boundary enforcement during v0.1.

**ADR-006: Incremental Directory Structure**

- **Week 1:** `src/`, `samples/`, `tests/`
- **Week 4:** add `benchmarks/` when `prompts.json` ships
- **Week 6+:** add `docs/`, `scripts/` when content exists

**ADR-007: Generator Diagnostic Reporting Policy**

- **Decision:** Every generator diagnostic has a unique ID from the HFC range (HFC1000–HFC1999 for SourceTools). Policy:
  - **Error:** Invalid attribute usage that would produce incorrect generated code.
  - **Warning:** Partial matches where generation proceeds with defaults (e.g., unsupported field type → placeholder).
  - **Info:** Successful generation summaries (opt-in via verbosity flag).
- **Message template:** What the generator saw → what it expected → how to fix it → docs link (`HFC{id}`).
- **Enforcement:** `EnforceExtendedAnalyzerRules` = `true` in SourceTools `.csproj`.

### Corrected Package Dependency Graph

Shell and EventStore are **peers**, not a chain. Both depend on Contracts. SourceTools is analyzer-only everywhere.

```
                    Hexalith.FrontComposer.Contracts
                    (net10.0 + netstandard2.0, dependency-free)
                   /            |              \
                  /             |               \
    SourceTools (analyzer)    Shell           EventStore
    (netstandard2.0)        (net10.0)        (net10.0)
                              |    \            |
                              |     \           |
                             Mcp   Aspire ------+
                                     |
                                   Testing
                                     |
                              Meta-package
```

At v0.1, only Contracts + SourceTools + Shell exist as separate projects. EventStore/Mcp/Aspire/Testing code lives inside Shell or the sample until v0.3 extraction.

### Critical Project Reference Patterns

```xml
<!-- Consumer .csproj — how consumers reference the generator -->
<ProjectReference Include="..\..\src\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj" />
<ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  SetTargetFramework="netstandard2.0" />
```

```xml
<!-- Hexalith.FrontComposer.SourceTools.csproj -->
<TargetFramework>netstandard2.0</TargetFramework>
<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
<IsRoslynComponent>true</IsRoslynComponent>
```

```xml
<!-- SourceTools.Tests.csproj — normal reference for testing IR types -->
<ProjectReference Include="..\SourceTools\Hexalith.FrontComposer.SourceTools.csproj" />
<!-- NOT OutputItemType="Analyzer" — tests call Parse/Transform as regular methods -->
```

```xml
<!-- Hexalith.FrontComposer.Contracts.csproj -->
<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
```

**NuGet packaging prep (for v0.3 — add to SourceTools .csproj now):**
```xml
<IncludeBuildOutput>false</IncludeBuildOutput>
<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
<!-- Plus a .targets file placing DLL in analyzers/dotnet/cs/ at pack time -->
```

**Analyzer dependency constraint:** If SourceTools ever needs a runtime dependency beyond `Microsoft.CodeAnalysis.CSharp`, it must be ILMerged or embedded. Analyzers run in an isolated load context.

### v0.1 Solution Structure (Week 1)

```
Hexalith.FrontComposer/
├── src/
│   ├── Hexalith.FrontComposer.Contracts/         # Attributes, typed contracts (multi-target)
│   ├── Hexalith.FrontComposer.SourceTools/        # Roslyn generator
│   │   ├── Parsing/                                # INamedTypeSymbol → DomainModel IR
│   │   ├── Transforms/                             # DomainModel → RazorModel/McpModel/SpecimenModel
│   │   └── Emitters/                               # Output models → source code strings
│   └── Hexalith.FrontComposer.Shell/              # Blazor shell + ES communication + lifecycle
├── samples/
│   └── Counter/
│       ├── Counter.Domain/                         # References Contracts + SourceTools (as Analyzer)
│       ├── Counter.Web/                            # Blazor Auto app
│       └── Counter.AppHost/                        # Aspire AppHost
├── tests/
│   └── Hexalith.FrontComposer.SourceTools.Tests/   # IR pure-function tests + generator integration tests
├── deps.local.props                                # Submodule ProjectReferences (default)
├── deps.nuget.props                                # NuGet PackageReferences (release validation)
├── Directory.Build.props                           # Shared settings + deps import switch
├── Directory.Packages.props                        # Central package management — ALL versions pinned
└── Hexalith.FrontComposer.sln
```

**Render-mode-specific code:** Runtime checks via `OperatingSystem.IsBrowser()` and `IComponentRenderMode`. No separate files per render mode.

### Week 1 Validation Deliverables

1. **"Hello world" generator** — SourceTools emits a `// Generated by FrontComposer` comment in Counter.Domain. Proves the full pipeline: attributes in Contracts → generator in SourceTools → output in consuming project.
2. **Working Aspire topology** — Counter + EventStore + DAPR sidecar all healthy in the Aspire dashboard.
3. **Build time baselines** — `dotnet restore` <2 minutes, `dotnet build` <30 seconds. If violated, reduce project count further.

## Core Architectural Decisions

*10 decisions stress-tested via Advanced Elicitation (self-consistency, failure mode, pre-mortem, red team, first principles) and Party Mode (Winston, Amelia, Murat). Two inconsistencies resolved, generator output sequenced, 4-layer test strategy for critical renderer.*

### Decision Summary

| # | Decision | Choice | Phase |
|---|---|---|---|
| 1 | State management | Fluxor 6.9.0 (inject `IState<T>`, generator-emitted subscribe/dispose — no base class) | v0.1 |
| 2 | Lifecycle integration | Fluxor `CommandLifecycleState` + `<FrontComposerLifecycleWrapper CorrelationId>` | v0.1 (basic) → v1 (full) |
| 3 | SignalR abstraction | `ProjectionSubscriptionService` (behavioral wrapper, single typed registration) | v0.1 |
| 4 | Rendering abstraction | `IRenderer<TModel, TOutput>` strategy + convention-based `ProjectionRenderer<T>` | v0.1 (Razor) → v1 (+ Markdown) |
| 5 | DI model | Stateless domain services + Fluxor. Infrastructure services (HubConnection) exempt. | v0.1 |
| 6 | ETag cache storage | `IStorageService` (5 methods incl. FlushAsync, fire-and-forget + beforeunload drain) | v0.1 |
| 7 | Session persistence | Per-concern Fluxor features (Theme, Density, Navigation, DataGrid, ETagCache, CommandLifecycle) | v1 |
| 8 | Generator discovery | Same-compilation `ForAttributeWithMetadataName` + runtime `IFrontComposerRegistry` + generated manifest | v0.1 |
| 9 | Customization gradient | Hybrid: annotation/template compile-time, slot/replacement runtime via `IOverrideRegistry` | v1 |
| 10 | Theme/density cascade | Fluxor reducer chain (4-tier: user > OS > deployment > factory) | v1 |

### Critical Architectural Mechanisms

**Runtime Composition: Registry + Manifest**

Each microservice's generator emits: (1) a static `RegisterDomain()` method adding renderers, subscriptions, and nav entries to `IFrontComposerRegistry`, and (2) a static manifest entry (JSON fragment or C# constant) for deterministic discovery.

The shell uses the manifest for discovery (explicit load order, subset loading per user role, diagnostic clarity) and calls `RegisterDomain()` for execution. Assembly scanning is the fallback, not the primary path.

```csharp
// Generated by source generator in Counter.Domain
public static class CounterDomainRegistration
{
    // Manifest entry for shell discovery
    public static readonly DomainManifest Manifest = new(
        Name: "Counter",
        BoundedContext: "Counter",
        Projections: ["CounterProjection"],
        Commands: ["IncrementCommand"]);

    // Registration method for runtime wiring
    public static void RegisterDomain(IFrontComposerRegistry registry)
    {
        registry.AddProjection<CounterProjection>(/* IR metadata */);
        registry.AddCommand<IncrementCommand>(/* IR metadata */);
        registry.AddNavGroup("Counter", /* bounded context metadata */);
    }
}

// Developer writes one line in Program.cs
services.AddHexalithDomain<CounterDomain>(); // invokes generated RegisterDomain()
```

**Topology:** v0.1 all domains in-process (monolith via Aspire). At v1, domains loaded via NuGet-published assemblies containing generated registration methods. Same pattern, different packaging.

**Generator Output Sequencing (per Winston)**

The generator emits outputs incrementally across versions:

| Phase | Outputs | Rationale |
|---|---|---|
| **v0.1** | Razor component + Fluxor boilerplate (one logical unit) | Cannot render without state wiring. Minimum useful pair. |
| **v0.2** | + Specimen (static rendering with sample data) | Mechanically simple, immediate developer feedback |
| **v0.3** | + MCP tool descriptor | Depends on MCP surface stabilizing, least coupled to render path |

The generator pipeline (ADR-004) stages:
1. **Parse** — `INamedTypeSymbol` → `DomainModel` IR
2. **Transform** — `DomainModel` → `RazorModel` + `FluxorModel` (v0.1), + `SpecimenModel` (v0.2), + `McpManifestModel` (v0.3)
3. **Emit** — models → source code strings (including component subscribe/dispose wiring)

**Fluxor Integration (no Fluxor dependency in generator)**

The generator emits Fluxor types as **strings** — fully-qualified type names (`Fluxor.IFeature<T>`, `[ReducerMethod]`). The emitted code compiles in the consumer project's context, which has `Fluxor.Blazor.Web` in its dependency graph. SourceTools (netstandard2.0) never loads Fluxor.

For each `[Command]`, the generator produces:
- `CommandLifecycleState` feature scoped by command type
- Actions: `{CommandName}Submitted`, `Acknowledged`, `Confirmed`, `Rejected`
- Reducer handling state transitions
- Effect wiring to `ProjectionSubscriptionService`

**Component Subscription Wiring (Path B — per Amelia)**

Generator emits explicit `IState<T>` subscribe/dispose per component. No `FluxorComponent` base class, no reflection, AOT-friendly:

```csharp
// Generated partial class
public partial class CounterProjectionView : ComponentBase, IDisposable
{
    [Inject] private IState<CounterProjectionState> CounterState { get; set; } = default!;

    protected override void OnInitialized()
    {
        CounterState.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        CounterState.StateChanged -= OnStateChanged;
    }
}
```

This preserves a migration path away from Fluxor — replacing `IState<T>` with any `IObservable<T>`-like interface requires changing the generator template, not every component.

**Convention-Based Rendering**

A single `ProjectionRenderer<T>` handles all projections using IR metadata (field types, role hints, badge mappings). Custom renderers resolved via `IOverrideRegistry` only for gradient overrides. DI never registers per-type renderers.

**Circuit-breaker error boundary (per Murat):** If `ProjectionRenderer<T>` catches an exception during render, it renders a diagnostic panel ("Projection {name} failed to render: {error}. See HFC2001.") instead of throwing and crashing the Blazor circuit. Partial degradation, not full outage.

### Service Classification

| Classification | Rule | Example |
|---|---|---|
| **Domain services** | Stateless. No mutable instance fields. All state in Fluxor. Registered as `Scoped`. | `CommandService`, `QueryService`, renderers |
| **Infrastructure services** | May hold connection handles. Managed by DI lifetime. | `ProjectionSubscriptionService` (owns `HubConnection`) |

**Compile-time enforcement:** Roslyn analyzer **HFC1050** flags mutable fields in framework domain services.

### IStorageService Contract (5 methods)

```csharp
public interface IStorageService
{
    ValueTask<T?> GetAsync<T>(string key);
    ValueTask SetAsync<T>(string key, T value);
    ValueTask RemoveAsync(string key);
    ValueTask<IReadOnlyList<string>> GetKeysAsync(string prefix);
    ValueTask FlushAsync();  // drains pending writes — called on beforeunload
}
```

- Two implementations: `LocalStorageService` (WASM), `InMemoryStorageService` (Server + bUnit)
- `SetAsync` is fire-and-forget internally (async write, doesn't block render path)
- `FlushAsync` called via `beforeunload` JS interop hook in `App.razor` — prevents preference loss on navigation
- LRU eviction internal to localStorage impl (max entries configurable)
- Cache key pattern: `{tenantId}:{userId}:{featureName}:{discriminator}`

### Per-Concern Fluxor Features

| Feature | Persisted | Storage Key Pattern | Eviction |
|---|---|---|---|
| `ThemeState` | Yes | `{tenantId}:{userId}:theme` | None (fixed) |
| `DensityState` | Yes | `{tenantId}:{userId}:density` | None (fixed) |
| `NavigationState` | Yes | `{tenantId}:{userId}:nav` | None (fixed) |
| `DataGridState` | Yes | `{tenantId}:{userId}:grid:{projectionType}` | LRU by projection count |
| `ETagCacheState` | Yes | `{tenantId}:{userId}:etag:{projectionType}` | LRU by entry count |
| `CommandLifecycleState` | **No** (ephemeral) | — | Evicted on terminal state |

### Generator Diagnostics

| ID | Severity | Message |
|---|---|---|
| HFC1001 | Warning | "No [Command] or [Projection] types found in this compilation." |
| HFC1050 | Error | "Framework services must be stateless. Field '{name}' in '{type}' is mutable." |
| HFC2001 | Warning | "Projection {name} failed to render. Circuit-breaker activated." |

### Test Strategy

**Generator Tests (SourceTools.Tests):**
- Snapshot/golden-file tests — string comparison of emitted code against approved baselines
- Fluxor types included as `MetadataReference.CreateFromFile()` in `CSharpGeneratorDriver` compilation — NOT a project reference
- Parse and Transform tested as pure functions (90% of tests)
- Emit tested via snapshots (10% of tests)
- Stryker mutation targets Parse and Transform only

**Registry Tests:**
- Unit: instantiate `IFrontComposerRegistry`, call generated `RegisterDomain()`, assert correct entries
- Integration: boot DI container, resolve registry, assert populated
- Both required — unit catches 80% of defects at 10% of cost

**ProjectionRenderer<T> Tests (4-layer defense-in-depth — per Murat):**

| Layer | What | Volume | Catches |
|---|---|---|---|
| 1. Pure function tests | IR field descriptor → render fragment mapping | 30-50 tests | Field type bugs, edge cases |
| 2. bUnit component tests | 5 archetype projections through full renderer | 5 tests | Render tree sequencing, event bindings |
| 3. Mutation testing (Stryker) | Mutation score ≥85% on ProjectionRenderer<T> | Nightly | Silent regressions, untested paths |
| 4. Golden file baselines | Rendered HTML of archetypes as approved snapshots | 5 baselines | Unknown-unknowns, unintended regressions |

### Failure Mode Mitigations

| Risk | Mitigation |
|---|---|
| Fluxor state loss during Server→WASM | `[PersistentState]` for critical features; specific bUnit render-mode transition test |
| CorrelationId mismatch in lifecycle | ULID-based correlation from command MessageId; unit test asserting 1:1 |
| Silent reconnection failure | Every reconnection path dispatches Fluxor action; fault-injection wrapper verifies |
| No renderer for model type | `IFrontComposerRegistry` startup validation; circuit-breaker diagnostic panel |
| Empty compilation discovery | Build-time warning HFC1001 |
| Mutable service field | Compile-time analyzer HFC1050 |
| Preference loss on navigation | `FlushAsync()` on `beforeunload`; graceful degradation on crash |

### Implementation Sequence

1. **Fluxor setup** (D1) — add Fluxor 6.9.0, configure store
2. **IStorageService** (D6) — 5-method interface + two impls + beforeunload hook
3. **Per-concern features** (D7) — ThemeState, DensityState first
4. **Generator: Parse + Transform + Emit (Razor + Fluxor)** (D8, D1) — v0.1 logical unit
5. **IFrontComposerRegistry + manifest generation** (D8) — runtime composition bridge
6. **ProjectionSubscriptionService** (D3) — SignalR → Fluxor actions
7. **CommandLifecycleState + wrapper** (D2) — five-state lifecycle
8. **Default ProjectionRenderer<T>** (D4) — convention-based, with circuit-breaker
9. **Customization gradient** (D9) — annotation level first
10. **Theme/density cascade** (D10) — Fluxor reducer chain
11. **Roslyn analyzers** (D5) — HFC1050, HFC1001

### Deferred Decisions (v1.x)

- Cross-device preference sync (server-side IStorageService impl)
- Multi-tenant theme/branding (per-tenant accent via Fluxor feature)
- Markdown surface renderer (second IRenderer impl, v0.3 MCP output enables)
- Dashboard composition layout engine
- Cross-context workflow orchestration

## Implementation Patterns & Consistency Rules

*Rules ensuring AI agents write compatible, consistent code. Standard .NET/C# conventions assumed. Focus on FrontComposer-specific patterns where agents could diverge.*

### Naming Patterns

**Generated Code:**

| Element | Pattern | Example |
|---|---|---|
| Generated Razor partial | `{TypeName}.g.razor.cs` | `CounterProjection.g.razor.cs` |
| Generated Fluxor feature | `{TypeName}Feature.g.cs` | `IncrementCommandFeature.g.cs` |
| Generated Fluxor actions | `{TypeName}Actions.g.cs` | `IncrementCommandActions.g.cs` |
| Generated registry | `{BoundedContext}DomainRegistration.g.cs` | `CounterDomainRegistration.g.cs` |
| Generated manifest | `{BoundedContext}DomainManifest.g.cs` | `CounterDomainManifest.g.cs` |
| Generated output folder | `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` | Deterministic, inspectable |

Rule: All generated files end in `.g.cs` or `.g.razor.cs`. Never share name with hand-written files. Live in `obj/`, not `src/`.

**Fluxor Naming:**

| Element | Pattern | Example |
|---|---|---|
| Feature | `{DomainType}Feature` | `CounterProjectionFeature` |
| State record | `{DomainType}State` | `CounterProjectionState` |
| Action (submitted) | `{CommandName}SubmittedAction` | `IncrementCommandSubmittedAction` |
| Action (acknowledged) | `{CommandName}AcknowledgedAction` | `IncrementCommandAcknowledgedAction` |
| Action (confirmed) | `{CommandName}ConfirmedAction` | `IncrementCommandConfirmedAction` |
| Action (rejected) | `{CommandName}RejectedAction` | `IncrementCommandRejectedAction` |
| Reducer | `{DomainType}Reducers` | `CounterProjectionReducers` |
| Effect | `{DomainType}Effects` | `CounterProjectionEffects` |
| Framework feature | `FrontComposer{Concern}Feature` | `FrontComposerThemeFeature` |

Rule: Actions always past-tense (`Submitted`, `Confirmed`) — never imperative. Framework features prefixed `FrontComposer`; generated features use domain type name.

**IStorageService Keys:**

Pattern: `{tenantId}:{userId}:{feature}:{discriminator}`

Examples: `acme:user42:theme`, `acme:user42:grid:OrderProjection`, `acme:user42:etag:CounterProjection`

Rule: Colon separator. Lowercase tenant/user IDs. PascalCase type names. No spaces. Under 256 chars.

**Diagnostic IDs:**

| Range | Package |
|---|---|
| HFC0001–HFC0999 | Contracts |
| HFC1000–HFC1999 | SourceTools |
| HFC2000–HFC2999 | Shell |
| HFC3000–HFC3999 | EventStore |
| HFC4000–HFC4999 | Mcp |
| HFC5000–HFC5999 | Aspire |

Rule: Each ID has one docs page. IDs never reused. New diagnostics take next available ID.

**Namespace Hierarchy:**

```
Hexalith.FrontComposer.Contracts
Hexalith.FrontComposer.Contracts.Rendering
Hexalith.FrontComposer.Contracts.Lifecycle
Hexalith.FrontComposer.SourceTools.Parsing
Hexalith.FrontComposer.SourceTools.Transforms
Hexalith.FrontComposer.SourceTools.Emitters
Hexalith.FrontComposer.Shell.Components
Hexalith.FrontComposer.Shell.State
Hexalith.FrontComposer.Shell.Services
Hexalith.FrontComposer.Shell.Infrastructure
```

Rule: Namespace matches folder path exactly. One type per file (except ≤5 Fluxor action records may share).

### Structure Patterns

**Fluxor Feature Organization:**

```
Shell/State/{Concern}/
├── {Concern}State.cs        # state record
├── {Concern}Actions.cs      # action records
├── {Concern}Reducers.cs     # [ReducerMethod] static methods
└── {Concern}Effects.cs      # [EffectMethod] for async (if needed)
```

**Test Organization:**

```
tests/{Project}.Tests/
├── Parsing/                  # mirrors src/ structure
├── Transforms/
├── Emitters/
│   └── Snapshots/            # golden files (.approved.cs)
├── Integration/
├── Components/               # bUnit
└── State/                    # Fluxor feature tests
```

Rule: Test folder mirrors source. Class = `{SourceClass}Tests`. Snapshots in `Snapshots/` with `.approved.cs`. Integration tests in `Integration/`.

**Customization Gradient Discovery:**

| Level | Discovered By | Must Be In |
|---|---|---|
| Annotation | Generator (compile-time) | Same compilation as domain type |
| Template | Generator (compile-time) | Same compilation as domain type |
| Slot | DI runtime (`IOverrideRegistry`) | Consumer project, registered via `AddSlotOverride<T,TSlot>()` |
| Full replacement | DI runtime (`IOverrideRegistry`) | Consumer project, registered via `AddViewOverride<T,TView>()` |

### Format Patterns

**Command Wire Format:**

```json
POST /api/v1/commands
{
  "messageId": "01HXYZ...",
  "commandType": "IncrementCommand",
  "tenantId": "acme",
  "payload": { },
  "metadata": { }
}
→ 202 Accepted { "messageId": "01HXYZ...", "status": "acknowledged" }
```

Rule: camelCase JSON. MessageId = ULID. TenantId always present. No colons in type names or tenant IDs.

**Diagnostic Message Format (mandatory 4 fields):**

```
HFC{id}: {what happened}
Expected: {what the framework expected}
Got: {what it found}
Fix: {concrete action to resolve}
Docs: https://hexalith.dev/diagnostics/HFC{id}
```

Rule: "Fix" must be actionable ("add [Projection] attribute to {TypeName}"), not generic ("check the docs").

**Fluxor Action Payloads:**

```csharp
public record IncrementCommandSubmittedAction(string CorrelationId, IncrementCommand Command);
public record IncrementCommandConfirmedAction(string CorrelationId, CounterProjection UpdatedProjection);
public record IncrementCommandRejectedAction(string CorrelationId, string Reason, string Resolution);
```

Rule: Actions are immutable records. Always include CorrelationId. Confirmed carries updated projection. Rejected carries Reason + Resolution. Never services or mutable objects.

### Communication Patterns

**SignalR Groups:** `{projectionType}:{tenantId}` (e.g., `CounterProjection:acme`). Server broadcasts nudge only — never full data.

**Logging:** Structured with OpenTelemetry conventions. Always include `CommandType`/`ProjectionType`, `TenantId`, `CorrelationId`. Message template + parameters, never string interpolation. Levels: Debug (dev), Information (flow), Warning (degraded), Error (intervention).

### Process Patterns

**Error Handling — Three Layers:**

| Layer | Pattern |
|---|---|
| Component | Circuit-breaker error boundary → diagnostic panel (HFC2001) |
| Service | Fluxor rejected action with domain-specific message |
| Infrastructure | Reconnection with progressive UX (SignalR loss → ETag polling) |

Rule: No exception ever swallowed. Every error dispatches a Fluxor action or renders a diagnostic. FR30: exactly one user-visible outcome per command.

**Generated View State Pattern:**

```csharp
@if (State.Value.IsLoading)
{
    <FluentSkeleton />
}
else if (State.Value.IsEmpty)
{
    <FrontComposerEmptyState Message="No counter data yet."
                              ActionLabel="Send your first Increment Counter command." />
}
else
{
    // render DataGrid / detail / etc.
}
```

Rule: Loading = per-component `FluentSkeleton` (never full-page). Empty = domain-specific CTA. `FluentProgressRing` reserved for command submission lifecycle only. Every generated view handles 3 states.

### Enforcement

**All AI Agents MUST:**

1. Follow naming tables exactly — no creative variations
2. Use `.g.cs` suffix and `obj/` location for generated code
3. Emit Fluxor actions as past-tense records with CorrelationId
4. Use 4-field diagnostic message format
5. Handle loading/empty/data in every generated projection view
6. Never swallow exceptions — dispatch action or render diagnostic
7. Use `IState<T>` with explicit subscribe/dispose — never `FluxorComponent`
8. Register gradient overrides via typed registration methods
9. Use structured logging with CommandType, TenantId, CorrelationId
10. Follow storage key pattern with colon separator

**Automated Enforcement:**
- Roslyn analyzers: HFC template, mutable fields (HFC1050), missing types (HFC1001)
- Snapshot tests: generated code matches approved baselines
- CI: namespace = folder path, no generated files in `src/`

## Project Structure & Boundaries

*Enhanced via Advanced Elicitation (Failure Mode, Pre-mortem, First Principles, Self-Consistency, Chaos Monkey) and Party Mode (Winston, Amelia, Murat, Barry). Key enhancements: phase-tagged blueprint (W1/W2/v0.1/v0.3), Directory.Build.props walk-up isolation for submodules, nested submodule path resolution, Pact anchored to Shell.Tests, E2E smoke tests added, consumer.props import chain specified, Contracts.Tests multi-targeted.*

### Phase Tagging

Every structural element is tagged with its creation phase. Agents MUST only create elements for the phase they are implementing:

| Tag | When | Trigger |
|---|---|---|
| **W1** | Week 1 | Initial scaffold — ship hello-world generator + working Counter |
| **W2** | Week 2 | Test infrastructure + CI hardening — when Shell has testable logic |
| **v0.1** | Week 3-4 | Full v0.1 delivery — lifecycle, subscriptions, seam abstractions |
| **v0.3** | Package extraction | When package boundaries add consumer value |

### FR Category → Architecture Mapping

| FR Category | v0.1 Location | v0.3+ Extraction Target |
|---|---|---|
| **Domain Auto-Generation** (FR1-12) | `Contracts/` (attributes) + `SourceTools/` (generator) | — (already separate) |
| **Composition Shell & Navigation** (FR13-22) | `Shell/Components/` + `Shell/State/Navigation/` | — (stays in Shell) |
| **Command Lifecycle & ES Communication** (FR23-38) | `Shell/State/CommandLifecycle/` + `Shell/Infrastructure/EventStore/` | `EventStore/` project |
| **Developer Customization** (FR39-48) | `Contracts/` (interfaces) + `Shell/Components/Overrides/` | — (stays split) |
| **Multi-Surface Rendering & Agent** (FR49-61) | `Shell/Components/Rendering/` + `Shell/Infrastructure/Mcp/` | `Mcp/` project |
| **Developer Experience & Tooling** (FR62-71) | `SourceTools/` (diagnostics) + `samples/` | — |
| **Observability** (FR72-73) | `Shell/Infrastructure/Telemetry/` | — (stays in Shell) |
| **Release Automation** (FR74-77) | `.github/workflows/` + `build/` | — |
| **Test Infrastructure** (FR78-82) | `tests/` | `Testing/` project at v0.3 |

### Complete Project Directory Structure

**Solution folders** (for IDE navigation): `src/`, `samples/`, `tests/` — configured in `.sln` to keep VS/Rider navigable as samples grow.

**Project count by phase:**

| Phase | Source | Sample | Test | Total .csproj |
|---|---|---|---|---|
| **W1** | 3 (Contracts, SourceTools, Shell) | 2 (Counter.Domain, Counter.Web) | 1 (SourceTools.Tests) | **6** |
| **W2** | — | 1 (Counter.AppHost) | 2 (Contracts.Tests, Shell.Tests) | **9** |
| **v0.1** | — | — | — | **9** (content grows, not projects) |
| **v0.3** | +4 (EventStore, Mcp, Aspire, Testing) | — | — | **13** |

```
Hexalith.FrontComposer/
│
├── .github/
│   └── workflows/
│       ├── ci.yml                     [W1] # Build + test (single workflow, split later)
│       ├── canary-fluentui.yml        [W2] # Weekly RC canary → creates GitHub issue on failure
│       └── nightly.yml               [W2] # Stryker + FsCheck + LLM benchmark + Pact verification
│
├── build/
│   ├── consumer.props                 [W2] # Shared analyzer ref import for consuming projects
│   └── SourceTools.targets            [v0.3] # NuGet pack: DLL in analyzers/dotnet/cs/
│
├── src/
│   ├── Hexalith.FrontComposer.Contracts/
│   │   ├── Hexalith.FrontComposer.Contracts.csproj   [W1] # net10.0;netstandard2.0 (ADR-001)
│   │   ├── Attributes/                                [W1]
│   │   │   ├── CommandAttribute.cs
│   │   │   ├── ProjectionAttribute.cs
│   │   │   └── BoundedContextAttribute.cs
│   │   ├── Rendering/                                 [W1]
│   │   │   ├── IRenderer.cs                           # IRenderer<TModel, TOutput>
│   │   │   ├── FieldDescriptor.cs                     # IR field metadata for convention rendering
│   │   │   └── RenderHints.cs                         # Badge, currency, date format hints
│   │   ├── Lifecycle/                                 [W1]
│   │   │   ├── CommandLifecycleState.cs               # Five-state enum
│   │   │   └── ICommandLifecycleTracker.cs
│   │   ├── Registration/                              [W1]
│   │   │   ├── IFrontComposerRegistry.cs
│   │   │   ├── DomainManifest.cs
│   │   │   └── IOverrideRegistry.cs
│   │   ├── Storage/                                   [W1]
│   │   │   └── IStorageService.cs                     # 5-method contract incl. FlushAsync
│   │   ├── Communication/                             [W1]
│   │   │   ├── ICommandService.cs
│   │   │   ├── IQueryService.cs
│   │   │   ├── IProjectionSubscription.cs
│   │   │   └── IProjectionChangeNotifier.cs           [v0.1] # Seam→Shell abstraction
│   │   └── Telemetry/                                 [v0.1]
│   │       └── FrontComposerActivitySource.cs         # Static ActivitySource name (shared across packages)
│   │
│   ├── Hexalith.FrontComposer.SourceTools/
│   │   ├── Hexalith.FrontComposer.SourceTools.csproj  [W1] # netstandard2.0, IsRoslynComponent
│   │   │                                              # Microsoft.CodeAnalysis.CSharp PrivateAssets="all"
│   │   ├── FrontComposerGenerator.cs                  [W1] # IIncrementalGenerator thin entry point
│   │   ├── Pipeline/                                  [W2] # Added when generator has multiple stages
│   │   │   └── FrontComposerPipeline.cs               # Composes Parse→Transform→Emit, keeps Generator thin
│   │   │                                              # MUST be seam-ignorant (per Winston)
│   │   ├── Parsing/                                   [W1] # Stage 1: INamedTypeSymbol → DomainModel IR
│   │   │   ├── AttributeParser.cs
│   │   │   ├── DomainModel.cs                         # IR data records
│   │   │   └── FieldTypeMapper.cs
│   │   ├── Transforms/                                [W1] # Stage 2: DomainModel → output models
│   │   │   ├── RazorModelTransform.cs
│   │   │   ├── FluxorModelTransform.cs
│   │   │   ├── RazorModel.cs
│   │   │   └── FluxorModel.cs
│   │   ├── Emitters/                                  [W1] # Stage 3: output models → source strings
│   │   │   ├── RazorEmitter.cs
│   │   │   ├── FluxorFeatureEmitter.cs
│   │   │   ├── FluxorActionsEmitter.cs
│   │   │   ├── RegistrationEmitter.cs
│   │   │   └── ManifestEmitter.cs
│   │   └── Diagnostics/                               [W1]
│   │       ├── DiagnosticDescriptors.cs               # HFC1000–HFC1999 catalog
│   │       └── DiagnosticReporter.cs
│   │
│   └── Hexalith.FrontComposer.Shell/
│       ├── Hexalith.FrontComposer.Shell.csproj        [W1] # net10.0, Fluent UI v5 + Fluxor 6.9.0
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs          [W1] # AddHexalithFrontComposer() — master entry
│       │   ├── EventStoreServiceExtensions.cs          [v0.1] # AddHexalithEventStore() — seam registration
│       │   └── McpServiceExtensions.cs                 [v0.1] # AddHexalithMcp() — seam registration
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── FrontComposerShell.razor            [W1] # Top-level shell with FluentLayout
│       │   │   └── FrontComposerNavigation.razor       [W2] # Registry-driven nav from manifests
│       │   ├── Lifecycle/
│       │   │   ├── FrontComposerLifecycleWrapper.razor      [v0.1] # Five-state command lifecycle
│       │   │   └── FrontComposerLifecycleWrapper.razor.cs
│       │   ├── Rendering/
│       │   │   ├── ProjectionRenderer.razor            [v0.1] # Convention-based + circuit-breaker
│       │   │   ├── ProjectionRenderer.razor.cs
│       │   │   └── FrontComposerEmptyState.razor       [v0.1]
│       │   └── Overrides/
│       │       └── OverrideResolver.cs                 [v0.1]
│       ├── State/                                      # Fluxor features
│       │   ├── Theme/                                  [W2]
│       │   │   ├── ThemeState.cs
│       │   │   ├── ThemeActions.cs
│       │   │   └── ThemeReducers.cs
│       │   ├── Density/                                [W2]
│       │   │   └── ...                                 # Same pattern
│       │   ├── Navigation/                             [W2]
│       │   │   └── ...
│       │   ├── DataGrid/                               [v0.1]
│       │   │   └── ...
│       │   ├── ETagCache/                              [v0.1]
│       │   │   └── ...
│       │   └── CommandLifecycle/                        [v0.1]
│       │       ├── CommandLifecycleActions.cs
│       │       └── CommandLifecycleReducers.cs
│       ├── Services/
│       │   ├── CommandService.cs                       [v0.1] # Stateless, delegates to EventStore seam
│       │   ├── QueryService.cs                         [v0.1]
│       │   ├── FrontComposerRegistry.cs                [W2] # IFrontComposerRegistry impl
│       │   └── ProjectionChangeNotifier.cs             [v0.1] # IProjectionChangeNotifier → Fluxor
│       ├── Infrastructure/
│       │   ├── Storage/
│       │   │   ├── LocalStorageService.cs              [v0.1] # IStorageService — WASM
│       │   │   └── InMemoryStorageService.cs           [v0.1] # IStorageService — Server + bUnit
│       │   │
│       │   │── ── EventStore/                          # ◄ v0.3 EXTRACTION SEAM ──────────
│       │   │   ├── EventStoreCommandClient.cs          [v0.1] # REST POST /api/v1/commands → 202
│       │   │   ├── EventStoreQueryClient.cs            [v0.1] # REST POST /api/v1/queries → 200+ETag
│       │   │   ├── ProjectionSubscriptionService.cs    [v0.1] # SignalR → IProjectionChangeNotifier
│       │   │   └── EventStoreOptions.cs                [v0.1]
│       │   │
│       │   │── ── Mcp/                                 # ◄ v0.3 EXTRACTION SEAM ──────────
│       │   │   ├── McpToolServer.cs                    [v0.1] # MCP protocol handler
│       │   │   ├── McpContractValidator.cs             [v0.1] # Hallucination rejection
│       │   │   └── McpOptions.cs                       [v0.1]
│       │   │
│       │   │── ── Aspire/                              # ◄ v0.3 EXTRACTION SEAM ──────────
│       │   │   └── AspireServiceDefaults.cs            [W2]
│       │   │
│       │   └── Telemetry/
│       │       └── TelemetrySetup.cs                   [v0.1] # OpenTelemetry wiring
│       └── wwwroot/
│           └── js/
│               └── beforeunload.js                     [v0.1] # FlushAsync() interop hook
│
├── samples/
│   └── Counter/
│       ├── Counter.Domain/
│       │   ├── Counter.Domain.csproj                   [W1] # Hardcoded analyzer ref (W1); imports consumer.props (W2+)
│       │   ├── IncrementCommand.cs                     [W1]
│       │   └── CounterProjection.cs                    [W1]
│       ├── Counter.Web/
│       │   ├── Counter.Web.csproj                      [W1] # Blazor Auto, references Shell + Counter.Domain
│       │   ├── Program.cs                              [W1]
│       │   ├── App.razor                               [W1]
│       │   └── Pages/
│       │       └── CounterPage.razor                   [W1]
│       └── Counter.AppHost/
│           ├── Counter.AppHost.csproj                   [W2] # Aspire AppHost
│           └── Program.cs                               [W2]
│
├── tests/
│   ├── Hexalith.FrontComposer.Contracts.Tests/
│   │   ├── Hexalith.FrontComposer.Contracts.Tests.csproj [W2] # Multi-targeted: net10.0;netstandard2.0
│   │   ├── Attributes/
│   │   │   └── AttributeDefinitionTests.cs             # Behavior under both TFMs
│   │   └── Communication/
│   │       └── InterfaceContractTests.cs               # Contract stability
│   │
│   ├── Hexalith.FrontComposer.SourceTools.Tests/
│   │   ├── Hexalith.FrontComposer.SourceTools.Tests.csproj [W1] # Normal ref to SourceTools
│   │   ├── Pipeline/                                       [W2]
│   │   │   └── FrontComposerPipelineTests.cs
│   │   ├── Parsing/                                        [W1]
│   │   │   ├── AttributeParserTests.cs
│   │   │   ├── FieldTypeMapperTests.cs
│   │   │   └── FsCheckParserPropertyTests.cs               [v0.1] # Property tests, nightly random seed
│   │   ├── Transforms/                                     [W1]
│   │   │   ├── RazorModelTransformTests.cs
│   │   │   └── FluxorModelTransformTests.cs
│   │   ├── Emitters/                                       [W1]
│   │   │   ├── RazorEmitterTests.cs
│   │   │   ├── FluxorEmitterTests.cs
│   │   │   └── Snapshots/                               # Golden files (.approved.cs)
│   │   │       ├── CounterProjection.approved.cs
│   │   │       ├── IncrementCommandFeature.approved.cs
│   │   │       └── CounterDomainRegistration.approved.cs
│   │   └── Integration/                                    [W1]
│   │       └── GeneratorDriverTests.cs
│   │
│   └── Hexalith.FrontComposer.Shell.Tests/
│       ├── Hexalith.FrontComposer.Shell.Tests.csproj    [W2] # References Shell + bUnit + Fluxor
│       ├── Components/                                      [W2]
│       │   ├── LifecycleWrapperTests.cs                 [v0.1] # bUnit five-state lifecycle
│       │   ├── ProjectionRendererTests.cs               [v0.1] # 4-layer defense: pure + bUnit archetypes
│       │   ├── NavigationTests.cs                       [W2]
│       │   └── Snapshots/                               [v0.1] # Golden HTML baselines
│       │       └── CounterProjection.approved.html
│       ├── State/                                           [W2]
│       │   ├── ThemeReducerTests.cs
│       │   ├── ETagCacheReducerTests.cs                 [v0.1]
│       │   └── CommandLifecycleReducerTests.cs          [v0.1]
│       ├── Services/
│       │   ├── RegistryTests.cs                         [W2] # Unit: instantiate, register, assert
│       │   ├── RegistryIntegrationTests.cs              [W2] # Boot DI, resolve, assert
│       │   └── SeamExtractionSmokeTests.cs              [v0.1] # AddHexalithEventStore → resolve
│       │                                                # IProjectionChangeNotifier → assert (per Winston)
│       ├── Infrastructure/
│       │   ├── StorageServiceTests.cs                   [v0.1]
│       │   └── SeamEnforcementTests.cs                  [W2] # Architecture: seams only ref Contracts
│       ├── Pact/                                            [v0.1] # Anchored to Shell.Tests (per Murat)
│       │   ├── EventStoreConsumerTests.cs               # Generates pact file as test output
│       │   └── Generated/                               # CI picks up .pact.json from here
│       │       └── .gitkeep
│       └── EndToEnd/                                        [v0.1] # 3-5 smoke tests (per Murat)
│           └── AttributeToRenderSmokeTests.cs           # Contracts attrs → SourceTools gen → bUnit render
│
├── Hexalith.EventStore/                                 # Git submodule (pinned commit)
├── Hexalith.Tenants/                                    # Git submodule (pinned commit)
│   └── Hexalith.EventStore/                             # ⚠ NESTED submodule (see MSBuild constraints)
│
├── deps.local.props                                     [W1] # Submodule ProjectReferences (ADR-002)
├── deps.nuget.props                                     [W1] # NuGet PackageReferences (release validation)
├── Directory.Build.props                                [W1] # Shared settings + deps import switch
├── Directory.Packages.props                             [W1] # Central package management — ALL versions pinned
├── global.json                                          [W1] # .NET SDK 10.0.5 pin
├── nuget.config                                         [W1] # Package sources
├── .editorconfig                                        [W1] # Code style enforcement
├── .gitmodules                                          [W1] # EventStore + Tenants submodule refs
├── .gitignore                                           [W1]
└── Hexalith.FrontComposer.sln                           [W1] # Solution folders: src/, samples/, tests/
```

### MSBuild Constraints (resolve in W1 or debug all sprint)

**1. Directory.Build.props walk-up isolation:**

Both EventStore and Tenants have their own `Directory.Build.props`. MSBuild walks UP the tree. Tenants already does an explicit parent import via `GetPathOfFileAbove`. FrontComposer's root `Directory.Build.props` must guard against double-import from submodule context:

```xml
<!-- Directory.Build.props — top of file -->
<PropertyGroup>
  <FrontComposerRoot>true</FrontComposerRoot>
</PropertyGroup>

<!-- Only import deps switch if we're the root, not imported from submodule -->
<Import Project="deps.local.props" Condition="'$(UseNuGetDeps)' != 'true' AND '$(FrontComposerRoot)' == 'true'" />
<Import Project="deps.nuget.props" Condition="'$(UseNuGetDeps)' == 'true' AND '$(FrontComposerRoot)' == 'true'" />
```

Submodules must have `<ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>` or equivalent to prevent walk-up into FrontComposer's root when built in-solution.

**2. Nested submodule path resolution:**

Tenants contains EventStore as its own submodule (`Hexalith.Tenants/Hexalith.EventStore/`). FrontComposer also has EventStore directly (`Hexalith.EventStore/`). `deps.local.props` MUST specify the **authoritative path** for EventStore ProjectReferences:

```xml
<!-- deps.local.props — EventStore is ALWAYS from root submodule, never Tenants' nested copy -->
<PropertyGroup>
  <EventStorePath>$(MSBuildThisFileDirectory)Hexalith.EventStore</EventStorePath>
</PropertyGroup>
```

**3. consumer.props import chain:**

| Phase | Import mechanism |
|---|---|
| **W1** | Hardcode analyzer reference XML directly in Counter.Domain.csproj |
| **W2** | `build/consumer.props` + `Directory.Build.targets` conditionally imports for `samples/` |
| **v0.3** | `.targets` file shipped inside NuGet package (via `SourceTools.targets`) |

**4. Microsoft.CodeAnalysis.CSharp reference:**

```xml
<!-- SourceTools.csproj -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
```

`PrivateAssets="all"` prevents leaking to consumers. NuGet `.targets` file must NOT list it as a dependency. Analyzer runs in VS/Rider's isolated load context.

**5. Directory.Packages.props day-1 pins:**

Must exist in W1 with at minimum:
- `Microsoft.CodeAnalysis.CSharp` (SourceTools)
- `Fluxor.Blazor.Web` (Shell)
- `Microsoft.FluentUI.AspNetCore.Components` (Shell, pinned exact RC — ADR-003)
- `xunit` + `bUnit` + `FluentAssertions` (tests)
- `Microsoft.NET.Test.Sdk` + `coverlet.collector` (tests)

### Architectural Boundaries

**API Boundaries (EventStore — external service):**

| Boundary | Protocol | Contract | Direction |
|---|---|---|---|
| Command dispatch | REST `POST /api/v1/commands` | 202 Accepted + `{messageId, status}` | Shell → EventStore |
| Query + ETag | REST `POST /api/v1/queries` | 200 + ETag header, max 10 `If-None-Match` | Shell → EventStore |
| Projection nudges | SignalR `/projections-hub` | `ProjectionChanged(projectionType, tenantId)` | EventStore → Shell |
| MCP tools | MCP protocol | Generated tool manifest (compile-time schema) | Agent → Shell/Mcp |
| DAPR bindings | DAPR component API | All infrastructure (state, pubsub, secrets) | Shell → DAPR sidecar |

**Package Boundaries (compile-time enforcement):**

| Package | May Reference | Must NOT Reference |
|---|---|---|
| **Contracts** | Nothing (dependency-free) | Any other FrontComposer package |
| **SourceTools** | Contracts, Microsoft.CodeAnalysis (PrivateAssets=all) | Shell, Fluxor, Fluent UI |
| **Shell** | Contracts, Fluent UI v5, Fluxor | SourceTools (analyzer-only) |
| **EventStore** (v0.3) | Contracts, Hexalith.EventStore client | Shell, Fluxor, Fluent UI |
| **Mcp** (v0.3) | Contracts | Shell, Fluxor, Fluent UI |

**Seam Enforcement [W2]:** `Shell.Tests/Infrastructure/SeamEnforcementTests.cs` verifies seam folders only reference `Hexalith.FrontComposer.Contracts` namespaces. Runs in CI gate.

**Contracts API Surface Gate [W2]:** CI builds Contracts targeting netstandard2.0 in isolation AND compares public API surface against net10.0 build. Any `#if NET10_0` conditional that changes public API is a build error.

**Data Boundaries:**

| Data | Owner | Storage | Scope |
|---|---|---|---|
| Domain state | EventStore (external) | DAPR state stores | `{projectionType}:{tenantId}` |
| ETag cache | Shell (`IStorageService`) | localStorage / InMemory | `{tenantId}:{userId}:{feature}:{discriminator}` |
| UI preferences | Shell (Fluxor) | localStorage via `IStorageService` | `{tenantId}:{userId}:{feature}` |
| Command lifecycle | Shell (Fluxor) | Ephemeral | Per-command, evicted on terminal |
| MCP tool manifest | SourceTools (generated) | Compiled into assembly | Per-domain, version-locked |
| Pact contracts | Shell.Tests/Pact/ | Generated JSON (checked in) | Consumer-driven, verified in CI |

**Extraction Seam Rules (v0.3):**

1. Only references types from `Contracts` — enforced by `SeamEnforcementTests`
2. Communicates with Shell via `Contracts` interfaces (`IProjectionChangeNotifier`, `ICommandService`)
3. Own `*Options.cs` — no shared config with Shell
4. Own `Add{Seam}()` extension method from v0.1 — called internally by `AddHexalithFrontComposer()`
5. At extraction: new `.csproj`, move folder, update master extension method — no code changes inside seam
6. **Smoke test [v0.1]:** `SeamExtractionSmokeTests.cs` registers via `AddHexalithEventStore()`, resolves `IProjectionChangeNotifier`, asserts wiring. Survives extraction unchanged (per Winston).

### Cross-Cutting Concern Locations

| Concern | Primary Location | Secondary | Phase |
|---|---|---|---|
| Multi-tenancy | `Shell/Services/` | Every `*Client.cs` | v0.1 |
| Five-state lifecycle | `Shell/State/CommandLifecycle/` + `Components/Lifecycle/` | Generated features | v0.1 |
| Schema evolution | `Contracts/` | `Shell/Infrastructure/EventStore/` | v0.1 |
| Session persistence | `Shell/State/*/` | `Shell/Infrastructure/Storage/` | v0.1 |
| Accessibility | `Shell/Components/` | CI (axe-core) | v0.1 |
| Teaching errors | `SourceTools/Diagnostics/` | `Shell/Components/Rendering/` | W1 |
| LLM compatibility | `Shell/Infrastructure/Mcp/` | `SourceTools/Emitters/ManifestEmitter` | v0.1 |
| Auth/AuthZ | `Shell/Services/` | `Contracts/` (`[RequiresPolicy]`) | v1 |
| Zero infra coupling | `Shell/Infrastructure/` (DAPR only) | CI scan | W1 |
| Render mode lifecycle | `Shell/Components/` | `Shell/Infrastructure/Storage/` | v0.1 |
| Generator diagnostics | `SourceTools/Diagnostics/` | `SourceTools/Emitters/` | W1 |
| Telemetry | `Contracts/Telemetry/` (shared name) | `Shell/Infrastructure/Telemetry/` | v0.1 |

### Data Flow

```
Developer writes:                    Source Generator produces:
┌─────────────────┐                 ┌──────────────────────────────┐
│ [Command]       │                 │ Razor partial (.g.razor.cs)  │
│ [Projection]    │─── compile ───→│ Fluxor feature (.g.cs)       │
│ [BoundedContext]│                 │ Registration (.g.cs)         │
└─────────────────┘                 │ Manifest (.g.cs)             │
                                    └──────────────────────────────┘

Runtime flow:
┌──────────┐  AddHexalithFrontComposer()  ┌──────────────────┐
│ App      │─────────────────────────────→ │ Registry          │
│ startup  │  (calls AddHexalithEventStore │ (manifest-driven) │
└──────────┘   + AddHexalithMcp internally)└────────┬─────────┘
                                                    │ discovers
                                        ┌───────────▼───────────┐
                                        │ Projection renderers   │
                                        │ Command handlers       │
                                        │ Nav entries             │
                                        │ SignalR subscriptions   │
                                        └───────────┬───────────┘

User interaction:
┌────────┐  click   ┌───────────────┐  REST   ┌────────────┐
│ Razor  │────────→ │ CommandService │───────→ │ EventStore │
│ UI     │          │ (Fluxor action)│         │ (external) │
└────┬───┘          └───────────────┘         └─────┬──────┘
     │                                               │
     │  ◄── Fluxor state ◄── IProjectionChange ◄────┘
     │       update            Notifier    (SignalR nudge
     ▼                                      + ETag re-query)
┌────────────────────────────┐
│ ProjectionRenderer<T>       │
│ (convention-based, updated) │
└────────────────────────────┘
```

### Development Workflow Integration

**Inner Loop (dev server) [W1]:**
- `dotnet watch` on Counter.Web — Blazor Server mode for fast iteration
- SourceTools changes require rebuild (no hot reload for `.g.cs`)
- Aspire dashboard at Counter.AppHost [W2] shows Counter + EventStore + DAPR health

**CI Pipeline (`ci.yml`) [W1 → W2 expansion]:**

| Gate | W1 | W2 |
|---|---|---|
| 1. Contracts netstandard2.0 build | Build only | + API surface comparison |
| 2. Full solution build | Yes | Yes |
| 3. Test execution | SourceTools.Tests only | + Contracts.Tests (multi-TFM) + Shell.Tests |
| 4. Seam enforcement | — | SeamEnforcementTests |
| 5. Banned-reference scan | — | No direct infra refs outside DAPR |

Budget: <5min inner loop, <12min full CI

**Canary (`canary-fluentui.yml`) [W2]:**
- Weekly build against latest Fluent UI v5 RC drop
- On failure: creates GitHub issue with `canary-failure` label + build log link
- On success: silent

**Nightly (`nightly.yml`) [W2 → v0.1 expansion]:**

| Job | W2 | v0.1 |
|---|---|---|
| Stryker mutation (SourceTools Parse+Transform, ≥85%) | Yes | Yes |
| FsCheck random seed (SourceTools.Tests/Parsing/) | — | Yes |
| LLM benchmark (10-prompt, threshold: ≥7/10 correct tool selection) | — | Yes (observability until threshold validated) |
| Pact provider verification vs EventStore submodule | — | Yes |
| Flaky quarantine reintroduction (automated: 10 consecutive green → remove trait) | Yes | Yes |

## Architecture Validation Results

*Validated via Advanced Elicitation (Red Team, Self-Consistency, Chaos Monkey, Devil's Advocate, Pre-mortem — 11 findings) and Party Mode (Winston, Amelia, Murat, Barry — contract ambiguities resolved, binary decisions made, test conventions specified).*

### Coherence Validation ✅

**Decision Compatibility:**

All technology choices verified compatible: Fluxor 6.9.0 + Blazor Auto + .NET 10, Fluent UI v5 RC + Blazor Auto, SourceTools netstandard2.0 + CodeAnalysis 4.12.0 (PrivateAssets="all"), DAPR 1.17.4 + Aspire 13.2.1, IProjectionChangeNotifier decoupling (Contracts interface, Shell implements).

**Version resolution:** xUnit pinned to v2 (not v3) for bUnit 2.7.2 compatibility. Tracked issue for v3 migration when bUnit officially supports xUnit v3 runner. Pin in `Directory.Packages.props`:
```xml
<PackageVersion Include="xunit" Version="2.9.3" />
<PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageVersion Include="bunit" Version="2.7.2" />
```

**Pattern Consistency:** Naming conventions (Step 5) align with structure (Step 6). Phase tags align with implementation sequence. Test organization mirrors source. Diagnostic ID ranges map to packages without overlaps.

**Structure Alignment:** 3 source projects match non-negotiable compile-time separation. Extraction seams have enforcement tests. CI gates cover all structural constraints.

### Requirements Coverage Validation ✅

**82 FRs across 9 categories:** All mapped to architectural components via FR Category → Architecture Mapping table (Step 6).

**8 NFR categories:** All addressed — Performance (lifecycle wrapper + generator caching), Security (tenant-scoped everything + hallucination rejection), Accessibility (Fluent UI + axe-core CI), Reliability (SignalR wrapper + circuit-breaker), Testability (IR pattern + 4-layer defense), Build/CI (phased gates + budgets), Deployment (DAPR-only coupling), Maintainability (dependency graph + diagnostic IDs).

### Implementation Readiness Validation ✅

**Decision Completeness:** 10 core decisions + 8 ADRs (ADR-008 added for Fluxor state convention) + diagnostic condition table (9 entries) + IRenderer skeleton interface.

**Structure Completeness:** Phase-tagged file tree (80+ elements), MSBuild constraints with XML, CI pipeline per phase.

**Pattern Completeness:** 6 naming tables, format patterns with code examples, 3-layer error handling, enforcement rules.

### Contract Resolutions

**Resolution 1: IRenderer Skeleton Interface**

```csharp
// Contracts/Rendering/IRenderer.cs
public interface IRenderer<TModel, TOutput>
{
    TOutput Render(TModel model, RenderContext context);
    bool CanRender(TModel model);
}

// Contracts/Rendering/IProjectionRenderer.cs
public interface IProjectionRenderer<TProjection> : IRenderer<TProjection, RenderFragment>
{
    RenderFragment RenderField(string fieldName, object? value, FieldDescriptor descriptor);
    RenderFragment RenderDataGrid(IReadOnlyList<TProjection> items);
    RenderFragment RenderDetail(TProjection item);
}

// Contracts/Rendering/RenderContext.cs
public record RenderContext(
    string TenantId,
    string UserId,
    RenderMode Mode,       // Server, WebAssembly, Auto
    DensityLevel Density,
    bool IsReadOnly);
```

- `Render` returns `RenderFragment` (composable, Blazor-idiomatic) — not `RenderTreeBuilder`
- Receives full projection state (not diff) — SignalR nudges trigger full re-query
- `RenderField` enables DataGrid column-level rendering from `FieldDescriptor` IR metadata
- `CanRender` supports customization gradient: override registry checks before falling back to convention renderer
- Generator emits code calling `RenderDataGrid` and `RenderDetail`; hand-written `ProjectionRenderer<T>` implements using IR metadata

**Resolution 2: Fluxor State Shape Convention (ADR-008)**

**Decision:** One Feature per domain type. Generated component dispatches. Shell reducers process.

| Element | Rule | Example |
|---|---|---|
| Per-projection Feature | `Feature<{ProjectionType}State>` | `Feature<CounterProjectionState>` |
| Per-command Feature | `Feature<{CommandType}LifecycleState>` | `Feature<IncrementCommandLifecycleState>` |
| State record | Immutable record with typed properties | `record CounterProjectionState(bool IsLoading, CounterProjection? Data, string? Error)` |
| Dispatcher | Generated component calls `Dispatcher.Dispatch(action)` | Generated `CounterProjectionView.razor.cs` |
| Reducer | Static `[ReducerMethod]` in `{Type}Reducers.cs` | `CounterProjectionReducers.cs` |
| Cross-feature | Framework features prefixed `FrontComposer` | `FrontComposerThemeState` |

Binding contract: Generator emits actions in `{Type}Actions.g.cs`. Hand-written reducers in Shell/State/ MUST use those exact action types. No parallel action hierarchies.

**Resolution 3: Diagnostic Condition Table (extends ADR-007)**

| HFC ID | Condition | Severity | Behavior |
|---|---|---|---|
| HFC1001 | No `[Command]` or `[Projection]` in compilation | Warning | Build succeeds, no output generated |
| HFC1002 | Unsupported field type in `[Projection]` | Warning | Falls back to `string` rendering, emits warning |
| HFC1003 | `[Command]` missing required `MessageId` property | Error | Build fails, no output for this type |
| HFC1004 | `[BoundedContext]` attribute on non-static class | Error | Build fails |
| HFC1005 | Duplicate `[Projection]` type name within same compilation | Error | Build fails |
| HFC1050 | Mutable field in framework service | Error | Build fails (Roslyn analyzer) |
| HFC2001 | ProjectionRenderer runtime render failure | Warning (runtime) | Circuit-breaker panel, not crash |
| HFC2002 | Duplicate projection registration in registry | Error (startup) | Throw at DI container build, fail fast |
| HFC2003 | Duplicate nav group name in registry | Info | Merge entries under one group (expected) |

Policy: Unknown/unrecognized types → Warning + fallback. Structural violations → Error + block. Runtime failures → diagnostic panel, never silent swallow. Generator NEVER silently skips types — produces output or reports diagnostic.

### Binary Decisions

| # | Decision | Resolution | Rationale |
|---|---|---|---|
| 1 | HFC2002 severity | **Error** (startup throw) | Duplicate projections cause runtime ambiguity. Fail fast. |
| 2 | Lifecycle timeout | **30 seconds**, configurable via `FrontComposerOptions.CommandTimeoutSeconds` | Reasonable for ES round-trip. After timeout → ETag polling fallback. |
| 3 | bUnit + xUnit v3 | **Pin xUnit v2 initially.** Tracked issue for v3 migration. | Less risk for W1. Migration mechanical when bUnit v3 ships. |
| 4 | CI in W1 scope | **Yes — W1 creates `ci.yml` with gates 1-3.** Gates 4-5 at W2. | CI from day 1 catches breaks immediately. |
| 5 | EventStore path base | **`$(MSBuildThisFileDirectory)Hexalith.EventStore`** in root `deps.local.props` | Absolute from file location = repo root. Tenants' nested copy never referenced. |

### Test Infrastructure Conventions

**Fixture Architecture:**

| Scope | Pattern | Example |
|---|---|---|
| Unit tests | Per-test (no shared state) | Each test creates its own compilation/model |
| Integration tests | Per-class via `IClassFixture<T>` | Shares `CSharpCompilation` setup |
| bUnit tests | Inherit `FrontComposerTestBase` (optional) | Pre-configures Fluxor + InMemoryStorageService + fake registry |
| Property tests | Per-test with explicit `Arbitrary` config | FsCheck generators for `DomainModel` IR |

Shared helpers: project-local in `Helpers/` folder. No shared test utilities project at v0.1. Extract to `Testing` at v0.3 when 3 test projects share >5 helpers.

Test naming: `{MethodUnderTest}_{Scenario}_{ExpectedResult}` or `Should_{Behavior}_When_{Condition}`. Pick one per project, don't mix.

Test data: Builder pattern for domain models:
```csharp
var projection = new CounterProjectionBuilder()
    .WithCount(5)
    .WithLastUpdated(DateTimeOffset.UtcNow)
    .Build();
```

**Field Type Coverage Matrix (SourceTools.Tests/Parsing/FieldTypeMapperTests.cs):**

| Category | Types | Test Count |
|---|---|---|
| Primitives | string, int, long, decimal, double, float, bool | 7 |
| Date/Time | DateTime, DateTimeOffset, DateOnly, TimeOnly | 4 |
| Identity | Guid, ULID | 2 |
| Enum | enum (backed by int) | 1 |
| Nullable | Nullable<T> for each primitive/date/identity | 13 |
| Complex | Nested record (single level) | 1 |
| Collection | `IReadOnlyList<T>` | 1 |
| **Total** | | **29** |

New types added to parser MUST add corresponding test. Stryker flags unmutated code paths.

**CI Failure Thresholds:**

| Gate | Metric | W2 Threshold | v0.1 Target |
|---|---|---|---|
| Stryker (SourceTools Parse+Transform) | Mutation score | ≥70% | ≥85% |
| Stryker (ProjectionRenderer) | Mutation score | ≥60% | ≥75% |
| FsCheck (SourceTools.Tests/Parsing) | Iteration count | 1000 (CI) | 1000 (CI), 10000 (nightly) |
| Pact (EventStore contract) | can-i-deploy | Hard block | Hard block |
| Code coverage (all projects) | Line coverage | Informational | ≥80% unit on core |

**Flaky Quarantine Mechanics:**

- Mark: `[Trait("Category", "Quarantined")]` on flaky test
- CI main lane: `--filter "Category!=Quarantined"` — flaky tests excluded
- CI quarantine lane: runs quarantined, warnings only
- Reintroduction: 5 consecutive nightly passes → automated PR to remove trait
- Tracking: each quarantine addition requires linked GitHub issue with root-cause hypothesis

**Test Clarity Note:** "90% unit" target applies to Transform+Emit (pure functions). Parse tests are integration-level (CSharpGeneratorDriver). This is by design — Roslyn's `INamedTypeSymbol` cannot be meaningfully unit-tested without a compilation context.

### Gap Analysis Results

**Critical Gaps: None.** All blocking decisions resolved.

**Important Gaps (addressed):**

1. ~~bUnit + xUnit v3 compatibility~~ → Resolved: pin xUnit v2
2. ~~IRenderer method signatures~~ → Resolved: skeleton interface
3. ~~Fluxor state shape convention~~ → Resolved: ADR-008
4. ~~Diagnostic condition table~~ → Resolved: 9 entries
5. ~~Test infrastructure conventions~~ → Resolved: fixtures, naming, builders, matrix

**Remaining Important Gaps (deferred, not blocking):**

1. Fluent UI v5 RC→GA migration procedure — resolve when GA date announced
2. Auth/AuthZ ADR (OIDC/SAML provider abstraction) — v1 scope
3. Performance budget per component — post-v0.1 when baselines exist

**Nice-to-Have (deferred):**

4. Blazor ErrorBoundary composition (circuit-breaker inside built-in ErrorBoundary)
5. CORS documentation (DAPR sidecar handles, not Shell)
6. Fluxor DevTools grouping at 15+ features — v1.x

**Stress-Test Findings (integrated):**

7. Lifecycle timeout/polling fallback: 30s → ETag polling (configurable)
8. Registry collision: merge nav groups, throw HFC2002 on duplicate projections
9. Golden HTML baselines: semantic DOM comparison (AngleSharp-based), not exact string
10. Pact verification: CI gate when EventStore submodule pin changes (not just nightly)
11. LLM benchmark: reclassified as observability signal, not quality gate
12. Fluxor critical dependency: version pin documented, migration scope = update templates + re-approve baselines
13. W1 agent focus: only W1-tagged concerns

### Architecture Completeness Checklist

**✅ Requirements Analysis**
- [x] 82 FRs analyzed across 9 capability areas with phase partitioning
- [x] 8 NFR categories mapped to architectural impact
- [x] 3 first-class constraints (Blazor Auto, Source Generator, Solo Maintainer)
- [x] 11 cross-cutting concerns mapped with testability strategy
- [x] v0.1 targets defined (5 deliverables by week 4)

**✅ Architectural Decisions**
- [x] 10 core decisions with versions and rationale
- [x] 8 ADRs with constraints and enforcement (ADR-008 added)
- [x] Corrected dependency graph (Shell/EventStore as peers)
- [x] IRenderer skeleton interface with method signatures
- [x] Fluxor state shape convention binding contract
- [x] Diagnostic condition→severity→code table (9 entries)
- [x] 5 binary decisions resolved

**✅ Implementation Patterns**
- [x] 6 naming tables + diagnostic ranges
- [x] Structure patterns (Fluxor features, test mirroring, customization gradient)
- [x] Format patterns (wire format, diagnostics, actions)
- [x] Process patterns (3-layer error handling, generated view state, circuit-breaker)
- [x] 10 enforcement rules + automated enforcement

**✅ Project Structure**
- [x] Phase-tagged file tree (80+ elements, W1/W2/v0.1/v0.3)
- [x] Project count by phase: 6→9→13
- [x] 5 MSBuild constraints with XML
- [x] Extraction seam rules (5 rules + smoke test)
- [x] CI pipeline phased (W1 gates 1-3, W2 gates 1-5 + canary + nightly)

**✅ Test Infrastructure**
- [x] 3 test projects mapped to production assemblies
- [x] Fixture architecture (per-test/per-class/base class)
- [x] Field type coverage matrix (29 tests)
- [x] CI failure thresholds with ramp targets
- [x] Flaky quarantine mechanics
- [x] xUnit v2 pin with v3 migration tracked

**✅ Validation**
- [x] Coherence validated (decisions, patterns, structure)
- [x] Requirements coverage verified (82/82 FRs, 8/8 NFRs)
- [x] Contract ambiguities resolved (3 resolutions)
- [x] Stress-tested (5 elicitation methods, 13 findings integrated)
- [x] Party Mode reviewed (4 agents, steps 2-7)

### Architecture Readiness Assessment

**Overall Status: READY FOR IMPLEMENTATION**

**Confidence Level: HIGH** — all contract ambiguities resolved, binary decisions made, test conventions specified, stress-tested through 5 elicitation methods and 6 party mode rounds.

**Key Strengths:**
- Phase-tagged structure prevents over-building (solo-maintainer sustainability)
- Extraction seams with enforcement tests make v0.3 mechanical
- IProjectionChangeNotifier decouples seams from Fluxor before extraction
- MSBuild constraints documented with XML (7 issues resolved)
- 4-layer defense-in-depth on highest-risk component
- IR pipeline enables 90% unit test ratio on generator
- Binding contracts (IRenderer, Fluxor state convention) prevent parallel-agent incompatibility

**Companion Artifacts (create separately):**
1. Single-page cheat sheet — "Adding a projection? 3 steps, 2 files"
2. Phase checklist — W1/W2/v0.1 tracking artifact
3. Code `// PATTERN:` comments — canonical examples linking back to ADRs

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Respect phase tags — only create elements tagged for the current phase
- Use implementation patterns consistently (naming tables are mandatory)
- Check MSBuild constraints before modifying any `.csproj` or `.props`
- Seam code must only reference `Hexalith.FrontComposer.Contracts` namespaces
- W1 agents focus on W1-tagged concerns only — do not implement W2/v0.1 elements

**First Implementation Priority:**

W1 day 1: MSBuild spine — `Directory.Build.props` (walk-up isolation), `Directory.Packages.props` (xUnit v2 + Roslyn 4.12.0 + Fluent UI v5 RC pins), `deps.local.props` (authoritative EventStore path) → `dotnet restore` → verify green.

W1 day 1-2: Contracts (attributes + IRenderer skeleton) → SourceTools (generator stub, `ForAttributeWithMetadataName`) → `dotnet build` → verify generator runs with empty output.

W1 day 2-4: Counter.Domain + Counter.Web → SourceTools.Tests (Parse + Emit snapshot) → `ci.yml` (gates 1-3) → validation: generator emits `// Generated by FrontComposer` in Counter.Domain.
