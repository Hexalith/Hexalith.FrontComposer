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

`FrontComposerShell` (`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`) mounts the Fluxor `StoreInitializer`, skip links, a `FluentLayout` with Header / Navigation / Content / Footer areas, global keyboard shortcuts (`Ctrl+,` settings, `Ctrl+K` palette), and `FluentProviders`. Generated projection pages render to `FluentDataGrid` with filter/expand/status components; generated commands render through `FcAuthorizedCommandRegion`, generated forms, and `FcLifecycleWrapper`, which surfaces Submitting, Acknowledged, Syncing, Confirmed, Rejected, idempotent-confirmed, and NeedsReview paths. Levels 2–4 customization (`ProjectionTemplate`, field-slot, full-view overrides) let external assemblies inject alternate render fragments. The generated body resolution order is deterministic: Level 4 full-view override → Level 2 projection template → generated default body; Level 3 field slots compose only inside whichever body explicitly delegates to the generated field/row/section/default renderers. Level 3 and Level 4 contract mismatches are registry/startup/runtime diagnostics today, while HFC1050-HFC1055 are build-time SourceTools accessibility analyzer warnings for statically inspectable override components. Development-only contract-mismatch panels render only in DEBUG + `IsDevelopment()`. The shell header's right cluster includes a framework-owned **account control** (`FcAccountMenu` — a `FluentAvatar` opening a Sign in / Sign out menu wired to the framework `/authentication/{challenge,sign-out}` endpoints, reading the principal from `AuthenticationStateProvider`), rendered **always** so it survives adopter `HeaderEnd` customization. The navigation hamburger (`FcHamburgerToggle`) is **always visible** and at Desktop toggles `SidebarCollapsed` (full nav ↔ 48px rail) via `SidebarToggledAction` — **superseding the earlier "D9 / no Desktop hamburger" decision (2026-06)**. The **server-side security wiring** that powers the account control is **framework-owned** (2026-06): a Blazor Server host calls `AddHexalithFrontComposerServerSecurity(...)` (or the granular `AddHexalithFrontComposerAuthentication` + `AddHexalithFrontComposerServerAuthenticationState` + `AddHexalithFrontComposerTokenRelay`) from `Hexalith.FrontComposer.Shell.Extensions`. These helpers replace the quickstart's fail-closed `NullAuthenticationStateProvider` with `ServerAuthenticationStateProvider`, add cascading authentication state, and register the circuit-safe per-user bearer-token relay (`FrontComposerUserTokenStore` / `FrontComposerGatewayAuthorizationHandler`). Domain modules supply only domain-specific security *configuration* (provider choice, claim mapping, authorization policies, which gateways to authorize) — this generic plumbing must not be duplicated per domain module.

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

### 4.1 UI component policy (project-wide governance)

**Every UI page and component across all FrontComposer surfaces** — the framework Shell, the samples,
and the domain consumers (`Hexalith.Tenants.UI`, `Hexalith.EventStore.Admin.UI`) — renders through
**FrontComposer components or Fluent UI Blazor v5**. Raw interactive HTML controls (`<button>`,
`<input>`, `<select>`, `<textarea>`) are **forbidden**: in Fluent UI v5 the design system only styles
its own custom elements (`<fluent-button>`, `<fluent-text-input>`, …), so a native control falls back
to unstyled browser rendering *and* drops the `aria`/`role`/focus affordances Fluent components
guarantee (NFR6). Raw `<a>` navigation links are permitted. (Generalizes ADR-003 from the Tenants-only
rule established by the 2026-06-09/06-16 correct-course passes into a project-wide policy — 2026-06-17.)

The rule is enforced per surface by `…FluentConformanceTests` governance guards
(`[Trait("Category","Governance")]`, blocking lane) that statically scan each surface's `.razor`
sources for raw interactive controls. **Documented carve-outs** — custom-styled, fully-accessible
interactive elements where a Fluent control would cause visual regression — are listed below and
mirrored in each guard's allowlist, so every exception is self-documenting and cannot silently widen:

| Surface | File | Element | Justification |
|---|---|---|---|
| Shell | `Components/Home/FcHomeCard.razor` | full-card link button | framework chrome; `role="link"` + custom keyboard activation; scoped `.fc-home-card-button` CSS; hosts `<h2>` + projection `<ul>` a `FluentButton` cannot contain without regression |
| Counter sample | `Counter.Specimens/FrontComposerTypeSpecimen.razor` | raw-control specimens | the raw controls **are** the a11y/visual specimen fixtures; not a shipped UI page (the guard excludes the `Counter.Specimens` tree entirely) |
| Admin.UI | `Components/ActivityChart.razor` | clickable bar-chart bar | data-visualization element (height-scaled `<div>`); `aria-label` present; `FluentButton` destroys the bar |
| Admin.UI | `Pages/Streams.razor` | inline monospace click-to-copy aggregate-ID cell | grid-cell affordance; `aria-label`/`data-testid`/`stopPropagation` present; `FluentButton` breaks the cell layout |

**No theme redefinition (project-wide).** Hand-authored styles express typography, color, and spacing
through **Fluent UI v5 component parameters** (`FluentText` `Size`/`Weight`/`Color`, `FluentStack`
`Width`/`*Gap`, …) or **Fluent 2 design tokens** (`--colorNeutralForeground*`, `--fontSizeBase*`,
`--lineHeightBase*`). Hand-authored CSS **must not** recreate what a Fluent component already provides
(a heading ramp via `font-size`/`font-weight`/`line-height`, a foreground role via `color:`) and **must
not** use legacy Fluent v4 / FAST tokens (`--type-ramp-*`, `--neutral-foreground-*`, `--neutral-fill-*`,
`--neutral-stroke-*`, `--neutral-layer-*`, `--accent-*`, `--palette-*`, `--design-unit*`,
`--elevation-shadow-*`, `--corner-radius`, `--focus-stroke-*`) — these belong to the previous major
version and do not track the active theme. Custom CSS is allowed only for layout the design system does
not own (flex/grid, gaps, user-agent resets) or a feature Fluent has no component/token for (e.g. the
focusable route-level `<h1>` in `FcPageHeader`). (Directive 2026-06-19; extends ADR-003 + this §4.1.)

This is enforced by a second `…FluentConformanceTests` guard,
`Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog`, which scans every Shell
`.css`/`.razor` source (build output excluded) for legacy kebab-case tokens. **Migration backlog** — the
15 pre-v5 files below are allowlisted, so the guard blocks any *new* legacy-token usage and any
regression of the ~13 already-clean Shell stylesheets. The allowlist may **only shrink**: migrating a
file off legacy tokens requires deleting its entry (a stale-entry assertion fails the build otherwise).
`fc-page-header.css` — whose `--type-ramp-plus-3-*` heading ramp was exactly the recreation this rule
targets — was the trigger for this clause and has been **removed**: `FcPageHeader` now renders its heading
via `FluentText` `Size`/`Weight` parameters (correct-course 2026-06-19), the first backlog burn-down.

| Area | Files (relative to `src/Hexalith.FrontComposer.Shell`) |
|---|---|
| DataGrid / Rendering | `Components/DataGrid/FcNewItemIndicator.razor.css`, `Components/Rendering/FcFieldPlaceholder.razor.css`, `Components/Rendering/FcProjectionLoadingSkeleton.razor.css` |
| DevMode | `Components/DevMode/FcDevModeAnnotation.razor.css`, `Components/DevMode/FcDevModeOverlay.razor.css`, `Components/DevMode/FcDevModeToggleButton.razor.css` |
| EventStore | `Components/EventStore/FcPendingCommandSummary.razor.css`, `Components/EventStore/FcProjectionConnectionStatus.razor.css` |
| Layout / Home / Lifecycle / Diagnostics | `Components/Layout/FrontComposerShell.razor`, `Components/Layout/FrontComposerShell.razor.css` (dynamic `--accent-base-color`), `Components/Home/FcHomeDirectory.razor.css`, `Components/Lifecycle/FcLifecycleWrapper.razor.css`, `Components/Diagnostics/FcCustomizationDiagnosticPanel.razor.css` |
| Global CSS | `wwwroot/css/fc-empty-state.scoped.css`, `wwwroot/css/fc-projection.css` |

### 4.2 Page-section layout pattern (FluentAccordion) — project-wide guideline

**Every titled page section renders inside a `FluentAccordion` / `FluentAccordionItem`.** A *titled
page section* is a content region introduced by its own section heading (`<h2>`/`<h3>`, a
`<header>`/`<section>` carrying a heading, or `Heading=` on a Fluent container) that sits alongside
one or more **sibling** titled regions. When a page or page-like surface (dialog body, detail panel)
presents **two or more** sibling titled sections, those sections are grouped under a single
`FluentAccordion`, one `FluentAccordionItem` per section. The first/primary item defaults
`Expanded="true"` so primary content is never hidden behind a click (NFR6).

This generalizes a pattern the framework already emits — generated projection detail bodies render
`[ProjectionFieldGroup]` buckets as `FluentAccordionItem`s (`ProjectionRoleBodyEmitter`), and
`FcHomeDirectory` collapses zero-urgency contexts into a `FluentAccordion` — into the standard layout
for hand-authored multi-section pages across all surfaces (Shell, samples, `Hexalith.Tenants.UI`,
`Hexalith.EventStore.Admin.UI`). Directive 2026-06-17; reinforces ADR-003 + §4.1.

**Not a section (never converted):** the page-level `<h1>` title, breadcrumb, toolbar, and navigation
chrome; and any page whose primary content is a **single** region — one `FluentDataGrid`, one command
form, one detail view, or a single titled block. A grid-first or visualization-first page keeps its
primary grid/chart always-visible; only genuinely supplementary sibling sections may be
accordion-grouped. Source-generator output already conforms (no emitter change required).

Unlike the §4.1 Fluent-only rule, this is a **design guideline, not a governance-test guard**:
accordion-appropriateness is contextual (single-section and grid-first pages are legitimate
exceptions) and cannot be asserted mechanically without false positives. It is enforced by code review
against this definition, not by a `…FluentConformanceTests` guard.

### 4.3 Layout-component policy (project-wide guideline)

**Hand-authored layout the design system owns is expressed through Fluent v5 layout components, not
bare `<div>` + CSS flex/grid.** This is the layout companion to §4.1 (components) and §4.2 (page
sections), and like §4.2 it is a code-review guideline, not a governance guard.

- A `<div>` whose only role is **one-dimensional flex stacking** (`display:flex` + `flex-direction` +
  `gap`, ± alignment) → **`FluentStack`** (`Orientation`, `HorizontalGap`/`VerticalGap`,
  `HorizontalAlignment`/`VerticalAlignment`, `Wrap`, `Width`). Note `FluentStack` defaults `Width="100%"`
  — set `Width="fit-content"` (or an explicit width) when replacing an `inline-flex` / fixed-width `<div>`.
- A `<div>` forming a **responsive 12-point column grid** → **`FluentGrid`/`FluentGridItem`**
  (`Xs/Sm/Md/Lg` spans, `Spacing`).
- **Page header/nav/content/footer scaffold** → **`FluentLayout`/`FluentLayoutItem`** (already used by
  `FrontComposerShell`).

**Stays a `<div>` — layout the design system does *not* own (never converted):**

- Positioning contexts & overlays (`position: absolute|fixed|sticky`, `inset`, `z-index`) — drawers,
  badges, the dev-mode overlay.
- Visually-hidden / sr-only / `aria-live` regions (clip-rect / off-screen).
- Accessibility/semantic landmarks where the **role/element is the point** (`role="status|alert|region|
  group"`; `role="dialog"` bodies carrying `@onkeydown`/`@ref`; semantic `<header>`/`<section>`/`<aside>`).
  When such a landmark *also* needs flex, the flex moves to a `FluentStack` **nested inside** — the
  landmark element stays.
- **`grid-template-columns: repeat(auto-fill|auto-fit, minmax(...))` card walls.** `FluentGrid` is a
  fixed 12-point breakpoint grid and cannot express content-driven auto-fill; converting changes the
  responsive behavior, so these stay CSS grid.
- **Responsive flow/direction flips via `@media`** (e.g. `flex-direction: column-reverse` at a breakpoint)
  — `FluentStack` has a single static `Orientation`.
- **Gaps/spacing bound to the density token system** (`var(--design-unit*)`, `--fc-spacing-unit`-driven
  scaling) where the value must change with density — moving the token onto a `FluentStack` param would
  hardcode it (breaking density) *or* relocate a legacy `--design-unit*` token into a `.razor`, tripping
  the §4.1 legacy-token guard. Convert only after the underlying token is migrated to a Fluent 2 token.
- Single-child wrappers with no flex/grid (nothing to delegate).

**Generated output already conforms** (the emitter renders through `FluentStack`/accordion; no emitter
change). **Guideline, not a guard** — like §4.2, enforced by code review, *not* a
`…FluentConformanceTests` guard: a regex cannot separate a delegatable flex stack from a
positioning/sr-only/landmark/auto-fill/density-coupled `<div>` without false positives. Conversion
progress is tracked as a **shrink-only backlog** (mirroring §4.1's token-backlog discipline) in the
correct-course proposal that introduced this section.

**RC attribute-splatting caveat.** When a converted `<div>` carried `data-testid`/`role`/`aria-*`/event
handlers, `FluentStack` must splat them onto its root element for the unit/e2e selector and a11y
contracts to survive. This holds on the pinned `5.0.0-rc.3-26138.1` (`FluentStack` captures unmatched
attributes); confirm via the component's own bUnit lane after each conversion. (Mirrors §4.2's
RC-surface caveat.)

**First burn-down (correct-course 2026-06-19).** Four clean Shell conversions landed:
`FcAccountMenu` (`.fc-account-menu`), `FcSettingsDialog` (`.fc-settings-body`), `FcCollapsedNavRail`
(`.fc-collapsed-rail`), and `FcProjectionLoadingSkeleton` (`.fc-projection-skeleton-row`) — each div's
flex moved to a `FluentStack`, the now-redundant flex CSS removed (non-layout rules — padding, width,
borders, the `.razor.css` legacy `--neutral-stroke-rest` in the skeleton header — preserved).

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
