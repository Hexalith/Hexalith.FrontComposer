# Hexalith.FrontComposer — Architecture

> **Generated:** 2026-06-02 · deep scan. See [project-overview.md](./project-overview.md) for the high-level summary.

## 1. Executive summary

FrontComposer is a **source-generation-driven Blazor application framework**. Its architecture is organized around one idea: a leaf **contracts kernel** defines a vocabulary of attributes and types; a **Roslyn incremental generator** reads domain types annotated with that vocabulary and emits all the boilerplate (Blazor views, command forms, Fluxor state, DI registration, MCP manifest); and several **consumers** (the Blazor shell, the MCP server, the CLI) use the generated artifacts at runtime/build-time. The producer and all consumers are bound together by **schema fingerprints** (v1 supports canonical-JSON and SourceTools-blob SHA-256 algorithms) so incompatibilities are detected as **drift** rather than failing silently.

## 2. Layered structure

```
┌──────────────────────────────────────────────────────────────────────────┐
│ LAYER 0 — Contracts kernel  (net10.0 + netstandard2.0, no project deps)    │
│   Hexalith.FrontComposer.Contracts                                         │
│   • Attributes ([Projection],[Command],[BoundedContext],[ProjectionRole]…) │
│   • Communication (ICommandService, IQueryService, lifecycle)              │
│   • Rendering model (Typography, ProjectionContext, slot/template/view)    │
│   • Registration (IFrontComposerRegistry, DomainManifest)                  │
│   • MCP descriptors (McpManifest, McpCommandDescriptor…)                   │
│   • Schema (SchemaFingerprint, baseline, delta) + FcDiagnosticIds          │
│   Hexalith.FrontComposer.Schema  → SchemaMigrationDeltaAnalyzer            │
├──────────────────────────────────────────────────────────────────────────┤
│ LAYER 1 — Producer  (netstandard2.0 analyzer)                              │
│   Hexalith.FrontComposer.SourceTools → FrontComposerGenerator              │
│   Parse (Roslyn → pure IR) → Transform (IR → emit models) → Emit (C#/Razor)│
│   Outputs: per-projection (5 files), per-command (7+page), MCP manifest    │
│   + opt-in drift detection vs a checked-in JSON baseline                   │
├──────────────────────────────────────────────────────────────────────────┤
│ LAYER 2 — Consumers  (net10.0)                                             │
│   Shell  ── Blazor runtime: composes generated views, Fluxor store,        │
│             nav/layout/dialogs, EventStore SignalR/HTTP clients            │
│   Mcp    ── ASP.NET Core MCP server: generated manifest → tools/resources  │
│   Cli    ── frontcomposer tool: inspect + migrate generated output         │
│   Testing── bUnit host + fakes for adopters of the generated components    │
├──────────────────────────────────────────────────────────────────────────┤
│ EXTERNAL (git submodules, root-level only, treated as dependencies)        │
│   Hexalith.Commons · Hexalith.EventStore · Hexalith.Tenants                │
└──────────────────────────────────────────────────────────────────────────┘
```

**Dependency direction:** everything points *down* to `Contracts`. `SourceTools` references only `Contracts` (so it stays netstandard2.0). `Schema` references `Contracts`. `Shell` references `Contracts`. `Mcp` references `Contracts` + `Schema`. `Cli` and `Testing` are effectively leaves at the consumer layer (`Cli` has no project refs; `Testing` wires the runtime fakes).

## 3. The generation pipeline (Layer 1 detail)

`FrontComposerGenerator : IIncrementalGenerator` (in `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`) registers `ForAttributeWithMetadataName` providers for `[Projection]`, `[Command]`, `[ProjectionTemplate]`, then runs **eight `RegisterSourceOutput` pipelines**:

1. **Parse → pure IR.** `AttributeParser` / `CommandParser` / `ProjectionTemplateMarkerParser` convert Roslyn symbols into Roslyn-free, fully-equatable IR (`DomainModel`, `CommandModel`, `PropertyModel`, `EquatableArray<T>`). *No `ISymbol` may escape this stage* — that is the incremental-cache key invariant.
2. **Diagnostics-as-data.** Parsers emit `DiagnosticInfo` records (not Roslyn `Diagnostic`s); these are converted to real diagnostics only inside `RegisterSourceOutput` where `SourceProductionContext` exists.
3. **Transform → Emit.** One transform + one emitter per artifact type produces:
   - **Per `[Projection]` (5 files):** `{T}.g.razor.cs` view (Loading/Empty/Data dispatched by `ProjectionRole`), `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`.
   - **Per `[Command]` (7 non-page files, `.Command` segment, plus optional page):** `CommandForm`, `CommandActions`, `CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, `CommandLifecycleBridge`, plus `CommandPage` when density is `FullPage`.
   - **Compilation-level:** `FrontComposerMcpManifest.g.cs` (tool/resource manifest with schema fingerprints) and the projection-template manifest.
4. **Density rule (spec-locked):** non-derivable property count ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage`. Derivable fields (`MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, or `[DerivedFrom]`) are excluded from forms.
5. **Drift detection (opt-in):** when `HfcDriftDetectionEnabled=true`, the current snapshot is compared to a `frontcomposer.*-baseline*.json` `AdditionalText`; structural/metadata drift raises HFC1065/HFC1066. This pipeline deliberately does **not** depend on `CompilationProvider`.

See [api-contracts.md](./api-contracts.md) for the full attribute→output contract and the HFC1001–HFC1070 diagnostic catalog.

## 4. Runtime composition (Shell)

A consuming app reduces its layout to `<FrontComposerShell>@Body</FrontComposerShell>`. At startup:

- `services.AddHexalithFrontComposerQuickstart()` registers Fluxor (scanning the Shell assembly for every state slice), `IStorageService` (scoped `LocalStorageService`), `IFrontComposerRegistry` (singleton), the command/query services (a stub wrapped by `AuthorizingCommandServiceDecorator`), badge/lifecycle/registry services, and projection slot/template/view-override registries.
- `services.AddHexalithDomain<TMarker>()` reflects each domain assembly for generated `*Registration` types and `[Command]`/`[BoundedContext]` attributes, populating the registry.
- `services.AddHexalithEventStore(...)` swaps the stub for real **SignalR + HTTP EventStore clients** (`EventStoreCommandClient`, `EventStoreQueryClient`, `ProjectionSubscriptionService`) and replaces the default `NullPendingCommandStatusQuery` with the EventStore-backed command-status query.

These three calls are **order- and presence-validated at startup** (Story 1.1): each entry point appends an immutable `IFrontComposerBootstrapMarker` (`TryAddEnumerable`) and registers an idempotent hosted gate (`FrontComposerBootstrapValidationGate`) that runs `FrontComposerBootstrapValidator` in DI-insertion order. A missing foundational `AddHexalithFrontComposerQuickstart()` or a mis-ordered call throws an `InvalidOperationException` naming the offending call — **failing fast at startup instead of with an opaque `IFrontComposerRegistry` DI error at first render.** `AddHexalithDomain<TMarker>()` is optional (an empty shell is valid). The gate depends only on the singleton markers + a logger, so it stays scope-safe under `ValidateScopes=true` (ADR-030).

`FrontComposerShell` (`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`) mounts the Fluxor `StoreInitializer`, skip links, a `FluentLayout` with Header / Navigation / Content / Footer areas, global keyboard shortcuts (`Ctrl+,` settings, `Ctrl+K` palette), and `FluentProviders`. Generated projection pages render to `FluentDataGrid` with filter/expand/status components; generated commands render through `FcAuthorizedCommandRegion`, generated forms, and `FcLifecycleWrapper`, which surfaces Submitting, Acknowledged, Syncing, Confirmed, Rejected, idempotent-confirmed, and NeedsReview paths. Levels 2–4 customization (`ProjectionTemplate`, field-slot, full-view overrides) let external assemblies inject alternate render fragments. The generated body resolution order is deterministic: Level 4 full-view override → Level 2 projection template → generated default body; Level 3 field slots compose only inside whichever body explicitly delegates to the generated field/row/section/default renderers. Level 3 and Level 4 contract mismatches are registry/startup/runtime diagnostics today, while HFC1050-HFC1055 are build-time SourceTools accessibility analyzer warnings for statically inspectable override components. Development-only contract-mismatch panels render only in DEBUG + `IsDevelopment()`.

State is managed in Fluxor slices under `src/Hexalith.FrontComposer.Shell/State/` (Theme, Density, Navigation, CommandPalette, ETagCache, PendingCommands, ProjectionConnection, ReconnectionReconciliation, …) following a **single-writer discipline** (ADR-007): each action type has one dispatch source; effects own persistence and JS interop. EventStore-enabled hosts run command-status polling through a scoped `PendingCommandPollingDriver`; pending-state mutation remains centralized in `PendingCommandPollingCoordinator` and `PendingCommandOutcomeResolver`.

Command submission has an explicit safety boundary. Generated forms validate local input, evaluate
`[RequiresPolicy]` authorization before `BeforeSubmit`, run destructive confirmation or other
`BeforeSubmit` hooks, re-authorize protected commands, then acquire the scoped
`CommandExecutionAdmissionGate` before dispatching lifecycle actions, calling the command service,
or registering pending state. `AuthorizingCommandServiceDecorator` remains the direct-dispatch
backstop for generated and custom callers. FC-CNC v1 blocks later local submits rather than queueing
or batching them. EventStore dispatch retry sits inside `EventStoreCommandClient` after
authorization/tenant resolution and is limited to pre-`202 Accepted` transient failures; it reuses
the same `MessageId` and surfaces retry exhaustion as warning feedback, not as a terminal lifecycle
state.

## 5. AI-agent surface (MCP)

`Hexalith.FrontComposer.Mcp` is an ASP.NET Core adapter (HTTP streamable MCP) that turns the generated `McpManifest` into a live tool/resource surface:

- **Tools** are built dynamically at each `tools/list`: every generated `McpCommandDescriptor` becomes a command tool; plus a fixed `frontcomposer.lifecycle.subscribe` tool for polling command lifecycle.
- **Resources:** projection resources (`frontcomposer://<bounded-context>/projections/<projection-name>`, tenant-scoped, rendered as Markdown) and **skill-corpus** resources (`frontcomposer://skills/<id>`) — the embedded markdown docs under `docs/skills/frontcomposer/**/*.md`.
- **Security is fail-closed:** both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` must be registered or startup throws. Auth/tenant/unknown failures return a single opaque shape so callers can't fingerprint the cause. Server-controlled fields (`TenantId`, `UserId`, `MessageId`, `CorrelationId`) cannot be supplied by agents.
- **Schema negotiation:** `McpSchemaNegotiator` classifies client/server fingerprint pairs (Exact / CompatibleAdditive / CompatibleWarning / Incompatible) and blocks side-effects on mismatch.

This is the "MCP boundary" described in `docs/concepts/source-generation-and-mcp-split.md`.

## 6. Cross-cutting concerns & invariants

| Concern | Mechanism | Invariant |
|---|---|---|
| **Identity** | NUlid | ULIDs (26-char Crockford base32), never GUIDs, for `messageId`/`correlationId`. |
| **Schema integrity** | `CanonicalSchemaMaterial` (SHA-256 canonical JSON) | Pins `JavaScriptEncoder.Create(UnicodeRanges.All)` + a STJ source-gen context; `AbsentValueSentinel = "<absent>"`; `StringComparer.Ordinal` everywhere. Changing any of these invalidates all stored fingerprints. |
| **Incremental caching** | Pure equatable IR + `EquatableArray<T>` | No Roslyn symbols in IR; full structural `Equals`/`GetHashCode`. |
| **Multi-TFM split** | `#if NET10_0_OR_GREATER` | FluentUI-dependent code (e.g. `Typography.cs`) is guarded so the netstandard2.0 analyzer build stays clean. |
| **Generated path** | `GeneratedOutputPathContract.Template` | `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{Type}.g.razor.cs` is a public contract validated in Debug *and* Release. |
| **Diagnostics** | `FcDiagnosticIds` / `DiagnosticDescriptors` | Build-time `HFC1xxx`, runtime `HFC2xxx`; new IDs declared with full XML docs. |
| **Telemetry** | `FrontComposerActivitySource` | OpenTelemetry `ActivitySource`. |
| **Build strictness** | `TreatWarningsAsErrors=true` | Analyzer/style warnings fail the build. |
| **Versioning** | semantic-release + Conventional Commits | See [deployment-guide.md](./deployment-guide.md). |

## 7. Architecturally significant decisions (observed)

- **ADR-003:** Build on FluentUI **v5 RC**, pin the exact version (`5.0.0-rc.3-26138.1`).
- **ADR-007:** Fluxor single-writer discipline per state slice.
- **ADR-030:** Scoped lifetime discipline for storage/effects/auth/tenant accessors.
- **Drift pipeline must not depend on `CompilationProvider`** (decision "P12") — only the trim/AOT advisory legitimately combines it, isolated in its own output.
- **Custom inline SVG icon factory** (`FcFluentIcons`) instead of the FluentUI icons NuGet (no v5-compatible release at authoring time).
- **No third-party CLI framework** — the CLI uses a bespoke option parser and a fixed generated-output path contract.

## 8. External dependencies (submodules)

The three root-level git submodules ([.gitmodules](.gitmodules)) are consumed as **local `ProjectReference`s by default** (via [deps.local.props](deps.local.props)) or as NuGet packages when `UseNuGetDeps=true` (via `deps.nuget.props`). They are **not** part of this documentation scope:

| Submodule | Repo | Role for FrontComposer |
|---|---|---|
| `Hexalith.Commons` | github.com/Hexalith/Hexalith.Commons | Shared primitives (e.g. ULID helpers, value/error patterns) |
| `Hexalith.EventStore` | github.com/Hexalith/Hexalith.EventStore | The CQRS/event-sourcing backend the Shell talks to via SignalR/HTTP |
| `Hexalith.Tenants` | github.com/Hexalith/Hexalith.Tenants | Multi-tenancy primitives |

> Each submodule has its own `CLAUDE.md`/`project-context.md`. Do **not** recurse into nested submodules, and never modify submodule files without explicit approval (changes propagate across the Hexalith ecosystem).
