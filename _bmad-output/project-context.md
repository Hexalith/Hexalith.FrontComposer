---
project_name: 'Hexalith.FrontComposer'
user_name: 'Administrator'
date: '2026-06-02'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'code_quality', 'workflow_rules', 'critical_rules']
status: 'complete'
rule_count: 77
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **.NET 10** — SDK pinned `10.0.300` (`rollForward: latestPatch`, `global.json`); all projects
  `Nullable`+`ImplicitUsings` enabled, **`TreatWarningsAsErrors=true`**, `LangVersion=latest`
  (`Directory.Build.props`)
- **Multi-TFM split:** `Hexalith.FrontComposer.Contracts` and `.SourceTools` target
  **`net10.0` + `netstandard2.0`** so the Roslyn analyzer host can reference Contracts; every
  other project is `net10.0` only
- **Roslyn** `Microsoft.CodeAnalysis.CSharp(.Workspaces/.Workspaces.Common)` **5.3.0** — pinned;
  the generator/IDE host is sensitive to it. Don't bump outside a story that owns the compat work
  (`PrivateAssets="all"` is set per-project, not in `Directory.Packages.props`)
- **Blazor UI:** `Microsoft.FluentUI.AspNetCore.Components` **`5.0.0-rc.3-26138.1`** — FluentUI v5
  **RC**, exact pin (ADR-003). No FluentUI icons NuGet — a custom inline-SVG `FcFluentIcons` factory
- **State:** `Fluxor.Blazor.Web` **6.9.0** — single-writer discipline per slice (ADR-007)
- **MCP:** `ModelContextProtocol.AspNetCore` **1.3.0** — HTTP streamable transport
- **Identity:** `NUlid` **1.7.3** — ULIDs (26-char Crockford base32), **never GUIDs**, for
  `messageId`/`correlationId`
- **Real-time / Auth:** `Microsoft.AspNetCore.SignalR.Client` **10.0.8** (EventStore projection
  subscriptions); `…Authentication.OpenIdConnect` **10.0.8** (host-owned OIDC)
- **Reactive:** `System.Reactive` **6.1.0** — badge-count producer/consumer isolation; declared
  explicitly so a FluentUI minor bump can't silently drop it
- **netstandard2.0 backports** (in-box on net10.0; pulled only for the analyzer TFM):
  `System.Collections.Immutable` 10.0.8, `System.Text.Json` 10.0.8,
  `System.Threading.Tasks.Extensions` 4.6.3, `System.ComponentModel.Annotations` 5.0.0
- **Orchestration:** `Aspire.Hosting.AppHost` **13.4.2** — a SINGLE AppHost,
  `src/Hexalith.FrontComposer.AppHost`, orchestrates the whole local stack (keycloak, eventstore +
  admin/admin-ui, tenants, tenants-ui, and the `counter-web` sample) via DAPR. FrontComposer itself
  ships no deployed service; the sample shell (`samples/Counter/Counter.Web`) also runs standalone
  for the a11y/visual specimen gate.
- **Testing:** xUnit **v3** `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit `2.7.2`,
  **Verify** `31.19.0` (use `Verify.XunitV3`, NOT `Verify.Xunit`), **FsCheck.Xunit.v3** `3.3.3`,
  **PactNet** `5.0.1`, **BenchmarkDotNet** `0.15.8`, coverlet `10.0.1`, `Microsoft.NET.Test.Sdk`
  `18.6.0`, `Microsoft.Extensions.TimeProvider.Testing` `10.6.0`
- **All versions centralized** in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`)
  — never add `Version=` to a `.csproj`
- **Ships as signed NuGet packages** (`.nupkg`+`.snupkg`); **no Dockerfiles/containers** (unlike the
  EventStore submodule). semantic-release drives versioning from Conventional Commits

## Critical Implementation Rules

### C# Language-Specific Rules

- **File-scoped namespaces** (`namespace X.Y.Z;`), **namespace = folder path**; `using` directives
  **outside** the namespace, System directives sorted first; **Allman braces** (newline before `{`)
- **NO file copyright/license headers** — 0 of 483 handwritten files have one. Do **not** add
  MIT/ITANEO headers (matches EventStore/Tenants; **opposite of `Hexalith.Commons`**)
- **`sealed record` / `sealed class` by default** — Contracts are overwhelmingly `sealed record`
  (immutable descriptors, rendering models, schema types) or `sealed class`; use plain
  `record`/`class` only when inheritance is intended; `readonly record struct` for small values
- **`ConfigureAwait(false)` on every awaited call** — CA2007 is `warning` in `.editorconfig`, but
  `TreatWarningsAsErrors=true` makes a missing one break the build (used pervasively across `src/`)
- **Nullable enabled** — validate public-boundary args with `ArgumentNullException.ThrowIfNull(...)`
  / `ArgumentException.ThrowIfNullOrWhiteSpace(...)`
- **Naming (`.editorconfig`, `warning` → error via TWAE):** `I`-prefixed interfaces, `_camelCase`
  private fields, **`Async` suffix** on async methods
- **ULIDs, never GUIDs** for `messageId`/`correlationId` — generate via the `IUlidFactory`
  abstraction (NUlid), not `Guid.NewGuid()`; don't `Guid.TryParse` these fields
- **XML docs:** `GenerateDocumentationFile=true`, **but the doc-comment warnings (0419, 1570–1574,
  1591, 1734) are `NoWarn`'d repo-wide** in `src/Directory.Build.props` — missing docs do **not**
  break the build today. They ARE expected on public-API surfaces (`Contracts/{Attributes,
  Rendering,Mcp,Conformance}`, where `.editorconfig` re-raises CS1591 to `warning`) because that's
  the v1.0 API-freeze target; owned `PublicAPI*.Shipped.txt` baselines pin strict public surfaces
- **Multi-TFM guard:** net10/FluentUI-only code in a multi-targeted project (Contracts) must sit
  behind `#if NET10_0_OR_GREATER` (e.g. `Typography.cs`) so the netstandard2.0 analyzer build stays clean
- **Formatting:** 4-space indent, **CRLF**, UTF-8, final newline, trim trailing whitespace

### Source-Generator Rules (Producer — `Hexalith.FrontComposer.SourceTools`)

- **One incremental generator:** `FrontComposerGenerator : IIncrementalGenerator`; pipeline is
  **Parse → pure IR → Transform → Emit**, fed by `ForAttributeWithMetadataName` for `[Projection]`,
  `[Command]`, `[ProjectionTemplate]`
- **IR must be pure & fully equatable — no `ISymbol` may escape the parse stage** (the
  incremental-cache key invariant). Use `EquatableArray<T>` for collections; hand-write
  `Equals`/`GetHashCode`. A missing field silently breaks caching (`DomainModel`/`CommandModel`/`PropertyModel`)
- **Diagnostics travel as `DiagnosticInfo` data**, converted to Roslyn `Diagnostic` **only inside
  `RegisterSourceOutput`**. Never create a Roslyn `Diagnostic` in the parse/transform stages
- **Don't hand-edit generated code** — change the generator or the annotated types. Output lands in
  `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`; this path is a **public contract**
  (`GeneratedOutputPathContract.Template`), validated in **Debug *and* Release**
- **Emitted artifacts:** per `[Projection]` → 5 files (`{T}.g.razor.cs` + `Feature`/`Actions`/
  `Reducers`/`Registration`); per `[Command]` → 7 non-page files (`.Command` segment:
  `CommandForm`, `CommandActions`, `CommandLifecycleFeature`, `CommandRegistration`,
  `CommandRenderer`, `CommandLastUsedSubscriber`, `CommandLifecycleBridge`) plus `CommandPage` when
  density is `FullPage`; compilation-level `FrontComposerMcpManifest.g.cs` + projection-template manifest
- **Customization precedence:** generated projection body resolution is Level 4 full-view override →
  Level 2 `[ProjectionTemplate]` → generated default body. Level 3 field slots compose only when the
  selected body delegates to the generated field/row/section/default renderers. Do not change this
  order outside an FC-CUST contract story.
- **Customization diagnostics phase:** HFC1038-HFC1041 and HFC1043-HFC1045 are currently
  call-site/startup/runtime registry diagnostics for Level 3/4 registrations, not proven SourceTools
  build diagnostics. HFC1050-HFC1055 are build-time SourceTools analyzer warnings over Level 2
  templates and Level 3/4 registration-discovered components.
- **Density rule is spec-locked:** non-derivable property count ≤1 → `Inline`, 2–4 → `CompactInline`,
  ≥5 → `FullPage`. Derivable fields (`MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps,
  `[DerivedFrom]`) are excluded from forms. Change only via a story/ADR
- **Drift pipeline must NOT depend on `CompilationProvider`** (decision P12; only the trim/AOT
  advisory may, isolated). Opt-in `HfcDriftDetectionEnabled=true` compares the snapshot to a
  checked-in `frontcomposer.*-baseline*.json` `AdditionalText` → HFC1065/HFC1066
- **New diagnostics:** add a `public const string` to `FcDiagnosticIds` (Contracts) + a
  `DiagnosticDescriptor` (SourceTools) with full XML docs; bands are **`HFC1xxx` = build-time,
  `HFC2xxx` = runtime**; document each under `docs/diagnostics/HFCxxxx.md`
- **`Debugger.Launch()` is forbidden in `src/**/*.cs`** (IDE-parity suite source-scans for it) —
  contributor-local branches only, removed before review

### Blazor Shell & Fluxor Rules (Consumer — `Hexalith.FrontComposer.Shell`)

- **Startup wiring order:** `AddHexalithFrontComposerQuickstart()` (Fluxor + storage + registry +
  command/query stubs + badge/lifecycle/slot/template/view registries) → `AddHexalithDomain<TMarker>()`
  (reflects domain assemblies for generated `*Registration` types) → `AddHexalithEventStore(...)`
  (swaps the stub for real SignalR+HTTP clients). App layout reduces to `<FrontComposerShell>@Body</FrontComposerShell>`
- **Fluxor single-writer discipline (ADR-007):** each action type has exactly one dispatch source;
  **effects own persistence + JS interop**, reducers stay pure. Slices live under `Shell/State/`
- **Scoped-lifetime discipline (ADR-030)** for storage/effects/auth/tenant accessors — never capture
  them in singletons
- **Fluent-only UI (project-wide):** every `.razor` page/component — Shell, samples, **and** domain
  consumers (Tenants.UI, EventStore Admin.UI) — uses **FrontComposer or Fluent v5 components**, never raw
  `<button>/<input>/<select>/<textarea>` (Fluent v5 leaves them unstyled and strips NFR6 a11y). Raw `<a>`
  nav links are allowed. Enforced per surface by `…FluentConformanceTests` Governance guards; documented
  carve-outs (Shell `FcHomeCard`, `Counter.Specimens` fixtures, Admin.UI `ActivityChart` bar + `Streams`
  aggregate-id-copy cell) are listed in `architecture.md` §4.1 and each guard's allowlist
- **No theme redefinition (project-wide):** express typography/color/spacing via **Fluent v5 component
  params** (`FluentText` `Size`/`Weight`/`Color`, `FluentStack` `Width`/`*Gap`) or **Fluent 2 tokens**
  (`--colorNeutralForeground*`, `--fontSizeBase*`, `--lineHeightBase*`). Hand-authored CSS must **not**
  recreate what a Fluent component provides (a heading ramp via `font-size`/`font-weight`/`line-height`, a
  foreground role via `color:`) nor use **legacy v4/FAST tokens** (`--type-ramp-*`, `--neutral-*`,
  `--accent-*`, `--palette-*`). Custom CSS only for layout the design system doesn't own (flex/grid, gaps,
  UA resets) or a Fluent-absent feature (e.g. the focusable route `<h1>` in `FcPageHeader`). Guarded by
  `FluentConformanceTests.Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog`
  (migration backlog **fully burned down 2026-06-19** — the allowlist is now empty, so the guard blocks
  **any** legacy token anywhere in the Shell; density-coupled spacing uses `--fc-spacing-unit`, not the
  undefined legacy `--design-unit`). See `architecture.md` §4.1
- **Page sections use FluentAccordion (project-wide guideline):** when a page/dialog/panel has **2+
  sibling titled sections** (heading-introduced content regions), group them in one `FluentAccordion`
  with one `FluentAccordionItem` per section; first item `Expanded="true"`. NOT sections: page `<h1>`
  title, breadcrumb, toolbar, nav chrome, and **single-region** pages (one `FluentDataGrid`/form/detail
  view) — never collapse a page's sole primary content. Grid-/viz-first pages keep the grid/chart
  always-visible. Generated output already conforms (field groups → accordion items). This is a
  **guideline enforced by review, not a Governance guard** (unlike the Fluent-only rule). See
  `architecture.md` §4.2
- **Layout uses Fluent v5 layout components (project-wide guideline):** a `<div>` whose only job is
  one-dimensional flex stacking (`display:flex`+`gap`) → `FluentStack`; a responsive 12-col grid →
  `FluentGrid`; page header/nav/content/footer chrome → `FluentLayout` (already used by
  `FrontComposerShell`). `FluentStack` defaults `Width="100%"` — set `Width="fit-content"`/explicit width
  when replacing an `inline-flex`/fixed-width div, and confirm it splats `data-testid`/`role`/`aria-*`
  (it does on the pinned RC). **Keep a `<div>`** for positioning/overlays, sr-only/`aria-live` regions,
  `role`/semantic-element landmarks (nest the `FluentStack` inside), `auto-fill`/`auto-fit minmax()` card
  walls (`FluentGrid` can't express them), and `@media` direction flips. Density-coupled gaps
  (`--fc-spacing-unit`) CAN now convert — pass the `calc(var(--fc-spacing-unit, 4px) * N)` as the
  `FluentStack` `*Gap` **string** param so density scaling survives (as done for `FcHomeDirectory`/
  `FcDensityPreviewPanel` on 2026-06-19); the legacy undefined `--design-unit` no longer exists. Guideline-by-review (no guard); shrink-only backlog (tracked
  candidates resolved 2026-06-19: `FcHomeDirectory`/`FcDensityPreviewPanel`/`FcPendingCommandSummary`/
  `FcCommandPalette` converted; `FcPaletteResultList` option rows kept as a documented exclusion). See
  `architecture.md` §4.3
- **Icons:** use the custom inline-SVG `FcFluentIcons` factory, **not** a FluentUI icons NuGet
- **NFR17 tripwire:** a new `IStorageService.SetAsync` call site in `Shell/State/` requires updating
  the tripwire whitelist + the story compliance matrix
- **Dev-only customization panel:** contract mismatch diagnostics may render through
  `FcCustomizationDiagnosticPanel` only when both gates hold: DEBUG build and
  `IHostEnvironment.IsDevelopment()`. Production/Staging and Release builds must not register or
  render the mismatch provider.

### MCP Server Rules (Consumer — `Hexalith.FrontComposer.Mcp`)

- **Fail-closed security:** both `IFrontComposerMcpTenantToolGate` and
  `IFrontComposerMcpResourceVisibilityGate` MUST be registered or **startup throws**. Auth/tenant/
  unknown failures return one **opaque** shape (callers can't fingerprint the cause)
- **Server-controlled fields are never accepted from tool input:** `TenantId`, `UserId`, `MessageId`,
  `CorrelationId` are injected server-side
- **Tools are built dynamically at each `tools/list`** from generated `McpCommandDescriptor`s + the
  fixed `frontcomposer.lifecycle.subscribe`. Resources: projections
  (`frontcomposer://<bounded-context>/projections/<projection-name>`, tenant-scoped Markdown) + skill-corpus
  (`frontcomposer://skills/<id>` from `docs/skills/frontcomposer/**/*.md`)
- **Schema negotiation:** `McpSchemaNegotiator` classifies fingerprint pairs (Exact /
  CompatibleAdditive / CompatibleWarning / Incompatible) and **blocks side-effects on mismatch**

### Schema Fingerprint & Integrity Rules

- **Fingerprints (SHA-256 over the supported v1 canonical-JSON or SourceTools-blob algorithms) bind producer ↔ all consumers.**
  `CanonicalSchemaMaterial` pins `JavaScriptEncoder.Create(UnicodeRanges.All)`, a STJ source-gen
  context, `AbsentValueSentinel = "<absent>"`, and `StringComparer.Ordinal` everywhere
- **Changing the encoder / sentinel / comparer / canonical serialization silently invalidates every
  stored fingerprint & baseline** — don't touch `CanonicalSchemaMaterial` without owning baseline regeneration
- **Don't relax the `SchemaBaselineProvenance` safe-identifier regex** — it is a security boundary

### Testing Rules

- **Stack:** xUnit **v3** (`xunit.v3`) + **Shouldly** (`ShouldBe`/`ShouldThrow` — never raw
  `Assert.*`) + **NSubstitute** mocks; **bUnit** for Blazor components; **Verify** snapshots;
  **FsCheck** property tests; **PactNet** for the EventStore REST boundary
- **Run SOLUTION-level `dotnet test` + trait filters** —
  `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  **This is the OPPOSITE of the EventStore submodule's per-project rule** — follow FrontComposer's model here
- **`DiffEngine_Disabled=true` is REQUIRED** when running tests — otherwise a Verify snapshot
  mismatch launches a diff tool and hangs CI/local runs
- **Trait categories:** `Governance`, `Contract`, `Performance`, `e2e-palette`, `NightlyProperty`,
  `Quarantined`. CI runs Governance + the default lane as **blocking**; palette/perf/quarantine are
  advisory/warning-only
- **Naming:** test files are **plural `{Class}Tests.cs`** (matches Tenants, not Commons' singular);
  methods are three-part **`Subject_Scenario_Expectation`**
- **Generator tests** go through `CompilationHelper.CreateCompilation()`; **Blazor component tests**
  use `GeneratedComponentTestBase` / `AddFrontComposerTestHost` with `JSInterop.Mode = Loose`
- **Verify snapshots:** use `Verify.XunitV3` (NOT `Verify.Xunit`); `.verified.txt` files are committed
  and updated **intentionally**
- **Public-API baselines:** `PublicAPI.Shipped.txt` (Testing library) is enforced by
  `PackageBoundaryTests`; `PublicAPI.FcTbl.Shipped.txt` (focused Shell FC-TBL DataGrid surface)
  is enforced by `FcTblPackageBoundaryTests` — update baselines intentionally when owned public
  surfaces change
- **Pacts:** CI **fails on a stale pact diff** (`tests/.../Shell.Tests/Pact`) — regenerate and commit
  intentional contract changes
- **Benchmarks** live ONLY in the separate `Shell.Tests.Bench` exe under
  `[Trait("Category","Performance")]`; use `FakeTimeProvider` for deterministic timer-driven tests
- **e2e (a11y/visual):** Playwright workspace in `tests/e2e` (`npm ci` → `npm run test:a11y`); builds
  the `samples/Counter/Counter.Web` specimen host with `Hexalith__FrontComposer__Specimens__Enabled=true`
- **All configured tests must pass before a change is done**

### Code Quality & Style Rules

- **`.slnx` only** (`Hexalith.FrontComposer.slnx`) — never create or use a `.sln`
- **`TreatWarningsAsErrors=true` everywhere** — analyzer/style warnings fail the build; fix them,
  don't blanket-suppress (a scoped `#pragma`/`SuppressMessage` with justification only when truly warranted)
- **No third-party analyzer packages** — FrontComposer relies on the **built-in .NET/Roslyn analyzers
  + TWAE**, NOT the SonarAnalyzer/StyleCop/Roslynator stack used by `Hexalith.Commons`. Don't add them
- **`.editorconfig` scaffolding-phase severities:** `CA1062`, `CA1822`, `CA2007` are `warning`
  ("escalate once mature") but TWAE promotes them to build-breakers; `CA1014` is disabled (`none`)
- **Centralized package versions** — never add `Version=` to a `.csproj`; edit
  `Directory.Packages.props` (`PrivateAssets="all"` on analyzer/Roslyn refs lives in the project file)
- **Dependency direction points DOWN to `Contracts`** — `SourceTools` references **only** `Contracts`
  (to stay netstandard2.0-clean); `Schema`/`Shell` → Contracts; `Mcp` → Contracts + Schema;
  `Cli`/`Testing` are leaves. Never add a reference that pulls net10-only deps into `SourceTools` or
  the netstandard2.0 face of `Contracts`
- **Package validation** is opt-in via `EnableFrontComposerPackageValidation=true` (baseline `0.1.0`,
  `Directory.Build.targets`) — used at release to catch breaking API/package changes
- **No third-party CLI framework** — the `frontcomposer` CLI uses a bespoke option parser + the fixed
  generated-output path contract; don't add System.CommandLine et al.

### Development Workflow Rules

- **Conventional Commits (required — semantic-release):** `feat`→minor, `fix`/`perf`→patch,
  `feat!`/`BREAKING CHANGE:`→major; `docs`/`refactor`/`test`/`chore`/`build`/`ci`/`style`→no release.
  Imperative, lowercase, no trailing period. **Don't use `feat` for refactors** (false minor bump +
  publish). commitlint validates the **PR title AND every commit** (husky `commit-msg` hook + CI)
- **Branches:** `feat/<desc>`, `fix/<desc>`, `docs/<desc>`. **No direct commits to `main`** — feature
  branches + PRs targeting `main`
- **Code review is mandatory per story** — run `/bmad-code-review` (adversarial: Blind Hunter / Edge
  Case Hunter / Acceptance Auditor) before flipping to Done; `/bmad-dev-story` is the main build
  command. Budget for review-found rework; **verify any CRITICAL finding before acting on it**
- **Submodules: root-level only** (Commons, EventStore, Tenants). **Never `--init --recursive`** or
  recurse into nested submodules; **never modify submodule files without explicit approval** (changes
  propagate across the Hexalith ecosystem). Build defaults to local `ProjectReference`s
  (`deps.local.props`); `-p:UseNuGetDeps=true` switches to published NuGet (`deps.nuget.props`)
- **`docs/` is a PUBLISHED DocFX site** (Diataxis), CI-gated (Gate 2d, `pwsh ./eng/validate-docs.ps1`)
  and referenced by product code, tests, CI & fixtures — **do NOT use it as scratch space**.
  Generated/BMAD docs go to `_bmad-output/` (this file lives there)
- **Skill-corpus docs** (`docs/skills/frontcomposer/**/*.md`) are embedded into the MCP server — they
  must satisfy the front-matter + `agent-reference` section contract and are snippet/reference-validated
- **CI** (`ci.yml`, push/PR to `main`): commitlint · Gate 1 (Contracts on netstandard2.0) · Gate 2
  (full solution Release) · CLI pack+smoke · Governance & Contract tests · docs validation · unit+bUnit
  coverage · a11y/visual. Verify locally before pushing: `dotnet build -c Release` clean + default lane green
- **Release** (`release.yml`, merge to `main`): semantic-release → build/pack/**sign**/SBOM/evidence
  → publish signed `.nupkg`+`.snupkg` to nuget.org + GitHub Release. `RELEASE_DRY_RUN` defaults
  **true** (rehearsal); `eng/release-package-inventory.json` pins the exact expected package set

### Critical Don't-Miss Rules

**Never:**
- **Never hand-edit generated code** (`obj/**/generated/HexalithFrontComposer/`) — change the
  generator or the annotated types; that path is a public contract
- **Never let an `ISymbol` escape the generator's parse stage** — it breaks incremental caching
  (keep IR pure & `EquatableArray`-based)
- **Never change `CanonicalSchemaMaterial`** (encoder / sentinel / `StringComparer.Ordinal` /
  serialization) without regenerating baselines — it silently invalidates every fingerprint
- **Never add `Version=` to a `.csproj`** — versions live in `Directory.Packages.props`
- **Never create/use a `.sln`** (`.slnx` only); **never** make per-project `dotnet test` the rule —
  FrontComposer is **solution-level + trait filters**
- **Never add copyright headers** (this repo has none) or the Sonar/StyleCop/Roslynator stack
  (built-in analyzers only)
- **Never use a GUID for `messageId`/`correlationId`** — ULIDs via `IUlidFactory`
- **Never `feat:` a refactor** (false minor bump + NuGet publish); **never commit directly to `main`**
- **Never recurse into nested submodules** (`--init --recursive`) or modify submodule files without
  explicit approval
- **Never use `docs/` as scratch space** — it's the published, CI-gated DocFX site; generated docs go
  to `_bmad-output/`
- **Never leave `Debugger.Launch()` in `src/**/*.cs`** — the IDE-parity suite fails on it
- **Never start the MCP server without both fail-closed gates registered**, and never accept
  `TenantId`/`UserId`/`MessageId`/`CorrelationId` from agent tool input

**Always:**
- **Always `ConfigureAwait(false)`** on awaits (CA2007 → build error via TWAE)
- **Always run tests with `DiffEngine_Disabled=true`** (else Verify hangs)
- **Always guard net10/FluentUI code with `#if NET10_0_OR_GREATER`** in multi-targeted projects
- **Always update `.verified.txt` snapshots, owned `PublicAPI*.Shipped.txt` baselines, and pacts intentionally** —
  CI fails on stale ones
- **Always build Release clean** (`TreatWarningsAsErrors=true`) and run `/bmad-code-review` before a
  story is Done
- **Always keep the dependency direction pointing down to `Contracts`** (`SourceTools` references only
  `Contracts`)

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code in `Hexalith.FrontComposer`.
- Follow ALL rules exactly as documented; when in doubt, prefer the more restrictive option.
- There is **no root `CLAUDE.md`** in this repo — this file plus the BMAD docs under
  `_bmad-output/project-docs/` (architecture, api-contracts, data-models, dev/contribution/deployment
  guides) are the primary agent context.
- The three submodules (`Hexalith.Commons`, `Hexalith.EventStore`, `Hexalith.Tenants`) are **external
  dependencies** with their own `CLAUDE.md`/`project-context.md`, and their rules differ from this
  repo's (e.g. Commons uses copyright headers + the Sonar/StyleCop/Roslynator stack; EventStore runs
  tests per-project). **Don't apply sibling-repo rules here.**

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update when the tech stack, analyzer policy, generator/IR contract, MCP gates, schema
  canonicalization, test lanes, or release pipeline change.
- Remove rules that become obvious over time.

Last Updated: 2026-06-02
