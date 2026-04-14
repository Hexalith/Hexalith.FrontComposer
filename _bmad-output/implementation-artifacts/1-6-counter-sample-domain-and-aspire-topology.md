# Story 1.6: Counter Sample Domain & Aspire Topology

Status: done

## Story

As a developer,
I want a working Counter sample domain running in an Aspire topology that demonstrates end-to-end auto-generation from domain attributes to rendered DataGrid,
So that I can validate the framework works and use it as a reference for building my own domains.

## Acceptance Criteria

1. **Counter.Domain Auto-Generation**
   Given the Counter.Domain project at `samples/Counter/Counter.Domain/` exists with `[BoundedContext("Counter")]`, `[Projection]`-annotated types, and `[Command]`-annotated records
   When `dotnet build` runs
   Then the source generator produces 5 output files for `CounterProjection` without errors
   And `IncrementCommand` compiles as a domain contract type, but does not emit UI/state artifacts in the current Story 1.5 generator contract
   And non-domain code in Counter.Domain is <= 10 lines (csproj refs + marker class only; usings and whitespace don't count)

2. **Counter.Domain Analyzer Reference & Counter.Web Project Configuration**
   Given the Counter.Domain project at `samples/Counter/Counter.Domain/` references SourceTools as an analyzer
   When the project is built
   Then SourceTools is referenced via `OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`, `SetTargetFramework="netstandard2.0"` in Counter.Domain.csproj
   And Contracts (`Hexalith.FrontComposer.Contracts`) is referenced as a normal project reference in Counter.Domain.csproj
   And Counter.Web references Shell and Counter.Domain (NOT SourceTools directly — generated code flows via Counter.Domain)

3. **Aspire Topology & Domain Registration**
   Given the Counter.AppHost Aspire project exists and Counter.Web calls `AddHexalithDomain<CounterDomain>()` in its Program.cs
   When `dotnet run` is executed on the AppHost
   Then the Aspire topology starts with Counter.Web orchestrated as a project resource
   And Counter.Web's DI initializes domain registration via the generated registration classes
   And navigating to the web application shows a basic shell layout with the Counter bounded context in navigation

4. **DataGrid Rendering & UI Compliance**
   Given the application is running
   When the developer navigates to the Counter projection view
   Then a DataGrid renders with columns in order: Id (text), Count (right-aligned N0), LastUpdated (short date "d")
   And null values render as em dash (\u2014); booleans render as "Yes"/"No"
   And empty state shows: "No counter data yet. Send your first Increment Counter command."
   And the shell uses Fluent UI Blazor v5 with accent color `#0097A7`
   And the zero-override strategy is followed (no custom CSS on Fluent UI components)
   And the basic shell layout follows UX-DR14 (header, navigation sidebar, content area)

5. **Time-to-First-Render (NFR82)**
   Given a cold machine with .NET 10 SDK installed (CI agent or clean local, no cached NuGet or build artifacts)
   When the developer clones the repo and runs the Counter sample
   Then time-to-first-render is <= 5 minutes (baseline check, not a hard gate -- if slow network or CI agent pushes past 5 min, log and document; do not block the story)

6. **Definition of Done**
   - All acceptance criteria 1-5 verified
   - All 139+ existing SourceTools tests passing with zero regressions
   - Integration smoke test added (Counter projection: 5 generated files verified)
   - bUnit render test passing (generated DataGrid renders columns in correct order with correct formatting)
   - Fluxor integration test passing (domain registration + `IState<CounterProjectionState>` discoverable via DI)
   - RegistrationEmitter test added (generated `Manifest` member and `RegisterDomain(IFrontComposerRegistry)` method exist and are callable)
   - Solution builds with zero errors and zero framework warnings
   - Code reviewed per team standards

## Tasks / Subtasks

- [x] Task 0: Extend SourceTools emitter for domain registration helper (PREREQUISITE for Tasks 2-4)
  - [x] 0.1 Update `RegistrationEmitter.cs` to keep the existing `{TypeName}Registration.g.cs` file pattern but emit two runtime hooks: a static `Manifest` member and a static `RegisterDomain(IFrontComposerRegistry registry)` method
  - [x] 0.2 The generated helper must register bounded-context metadata compatible with the existing `IFrontComposerRegistry` / `DomainManifest` contract. If the current registry shape is too narrow, extend the existing contract rather than introducing a parallel `AddProjection<T>()`, `AddCommand<T>()`, or `NavGroups` abstraction.
  - [x] 0.3 Add RegistrationEmitter snapshot test: verify the generated `Manifest` member and `RegisterDomain(IFrontComposerRegistry)` method compile and have the expected signature
  - [x] 0.4 Update `AnalyzerReleases.Unshipped.md` if any new diagnostics are added
  - [x] 0.5 Verify all 139+ existing SourceTools tests still pass after emitter changes

- [x] Task 1: Create Counter.Domain with annotated domain types at `samples/Counter/Counter.Domain/` (AC: #1)
  - [x] 1.1 Create `CounterDomain.cs` -- marker class with `[BoundedContext("Counter")]` attribute (see "CounterDomain Marker Type" section below)
  - [x] 1.2 Create `CounterProjection.cs` with `[Projection]`: fields `Id` (string), `Count` (int), `LastUpdated` (DateTimeOffset)
  - [x] 1.3 Create `IncrementCommand.cs` with `[Command]`: fields `MessageId` (string/ULID), `TenantId` (string), `Amount` (int, default 1)
  - [x] 1.4 Fix `SetTargetFramework` in Counter.Domain.csproj: change `SetTargetFramework="TargetFramework=netstandard2.0"` to `SetTargetFramework="netstandard2.0"` (see dev notes warning)
  - [x] 1.5 Verify non-domain code is <= 10 lines total (csproj refs + marker class; usings/whitespace excluded)
  - [x] 1.6 Run `dotnet build samples/Counter/Counter.Domain --verbosity=diagnostic` and verify generated files appear in `obj/Debug/net10.0/generated/HexalithFrontComposer/`: `CounterProjection.g.razor.cs`, `CounterProjectionFeature.g.cs`, `CounterProjectionActions.g.cs`, `CounterProjectionReducers.g.cs`, `CounterProjectionRegistration.g.cs`

- [x] Task 2: Complete Counter.Web at `samples/Counter/Counter.Web/` with shell, routing, and DI (AC: #2, #4)
  - [x] 2.1 Implement `Program.cs` with `AddHexalithFrontComposer(o => o.ScanAssemblies(typeof(Program).Assembly))` and `AddHexalithDomain<CounterDomain>()` (see Fluxor scanning pattern below)
  - [x] 2.2 Create root layout (`MainLayout.razor`) per UX-DR14: header region, navigation sidebar, content area
  - [x] 2.3 Add `<Fluxor.Blazor.Web.StoreInitializer />` in root layout
  - [x] 2.4 Configure Fluent UI Blazor v5 accent color `#0097A7` via `FluentDesignSystemProvider` in `App.razor` or layout; zero-override CSS strategy (no custom CSS files on Fluent components)
  - [x] 2.5 Add `App.razor` with router, `_Imports.razor` with required usings, `Properties/launchSettings.json`
  - [x] 2.6 Wire Counter navigation: Shell derives bounded-context navigation items from `IFrontComposerRegistry.GetManifests()`. After `AddHexalithDomain<CounterDomain>()`, "Counter" appears automatically with no hardcoded navigation entries.

- [x] Task 3: Create Counter.AppHost Aspire project at `samples/Counter/Counter.AppHost/` (AC: #3)
  - [x] 3.0 FIRST: Add Aspire package versions to root `Directory.Packages.props` (`Aspire.Hosting.AppHost`, `Aspire.Hosting.Sdk` at 13.2.1) — Central Package Management requires these entries before restore
  - [x] 3.0b Add Counter.AppHost to `Hexalith.FrontComposer.sln` (`dotnet sln add`) -- required before `Projects.Counter_Web` namespace works
  - [x] 3.1 Create `samples/Counter/Counter.AppHost/Counter.AppHost.csproj` with Aspire AppHost SDK (`<IsAspireHost>true</IsAspireHost>`)
  - [x] 3.2 Implement `Program.cs` with `DistributedApplication.CreateBuilder` orchestrating Counter.Web via `AddProject<Projects.Counter_Web>("counter-web")`
  - [x] 3.3 Verify `dotnet run --project samples/Counter/Counter.AppHost` starts topology and opens Aspire dashboard
  - [x] 3.4 Verify NO `.WithDomain<T>()` calls exist in Counter.AppHost -- all domain registration stays in Counter.Web
  - [x] 3.5 Verify Counter bounded context appears in navigation after Counter.Web's DI initializes

- [x] Task 4: Create `AddHexalithDomain<T>()` and align registry usage with the existing Contracts abstractions (AC: #3, PREREQUISITE for Task 2)
  - [x] 4.1 Verify the existing `IFrontComposerRegistry` and `DomainManifest` contracts in Contracts. Adapt this story to that shape; if extension is required, extend the existing contract rather than creating a duplicate registry abstraction.
  - [x] 4.2 Create `FrontComposerRegistry` implementation in Shell (implements `IFrontComposerRegistry`, registered as singleton, stores manifests and supports navigation derivation from `GetManifests()`)
  - [x] 4.3 Add `AddHexalithDomain<T>()` to Shell's `ServiceCollectionExtensions.cs`. Signature: `public static IServiceCollection AddHexalithDomain<T>(this IServiceCollection services) where T : class`. Discovery: uses `typeof(T).Assembly.GetExportedTypes()` (not `GetTypes()` — avoids `ReflectionTypeLoadException` on non-loadable internal types) to find classes whose name ends in `Registration` AND that have BOTH a static `Manifest` property/field of type `DomainManifest` AND a static `RegisterDomain(IFrontComposerRegistry)` method. Skip classes that match the name but lack either member. Log a warning for partial matches (has one but not both).
  - [x] 4.4 Wire into Counter.Web's `Program.cs`: `builder.Services.AddHexalithDomain<CounterDomain>()`
  - [x] 4.5 Verify domain registration makes `IFrontComposerRegistry.GetManifests()` include the Counter bounded context

- [x] Task 5: Validate end-to-end rendering and performance (AC: #4, #5)
  - [x] 5.1 Verify DataGrid renders with columns in exact order: Id (text), Count (right-aligned N0), LastUpdated (short date "d"). Assert no extra columns.
  - [x] 5.2 Verify null values render as em dash (\u2014), booleans as "Yes"/"No" (verified via bUnit in Task 6.7)
  - [x] 5.3 Verify column headers follow label resolution chain: `[Display(Name)]` > humanized CamelCase > raw field name
  - [x] 5.4 Verify empty state renders exact text: "No counter data yet. Send your first Increment Counter command."
  - [x] 5.5 Estimate time-to-first-render on clean local environment. If > 5 min, document in PR notes (environment, network speed, initial build time). Do not block the story.

- [x] Task 6: Verify build gates and test suite (AC: #1, #2, #4, Definition of Done)
  - [x] 6.1 Confirm `dotnet build` on full solution completes with zero errors, zero framework warnings
  - [x] 6.2 Confirm all 139+ existing SourceTools tests in `tests/Hexalith.FrontComposer.SourceTools.Tests/` still pass (`dotnet test` from repo root, zero regressions)
  - [x] 6.3 Add integration smoke test: Counter domain attributes -> generator -> verify 5 `CounterProjection` output files exist and compile without Roslyn errors
  - [x] 6.4 Add bUnit render test using Verify framework (`.verified.txt` snapshots): instantiate generated `CounterProjectionView`, dispatch `CounterProjectionLoadedAction` with test data, assert 3 columns render in order (Id, Count, LastUpdated) with correct formatting
  - [x] 6.5 Add Fluxor integration test: call `AddHexalithFrontComposer()` + `AddHexalithDomain<CounterDomain>()`, resolve `IState<CounterProjectionState>` from DI, dispatch `CounterProjectionLoadRequestedAction` -> `CounterProjectionLoadedAction(items)`, verify `State.Value.Items` is populated and `State.Value.IsLoading` transitions correctly
  - [x] 6.6 Add empty state render test: verify DataGrid shows exact message "No counter data yet. Send your first Increment Counter command." when state has no items
  - [x] 6.7 Add null/boolean snapshot test: verify null renders as em dash (\u2014) and boolean renders as "Yes"/"No" via bUnit Verify snapshot
  - [x] 6.8 Add RegistrationEmitter test: verify generated `CounterProjectionRegistration` file contains a `DomainManifest Manifest` member and a `public static void RegisterDomain(IFrontComposerRegistry)` helper

### Review Findings

- [x] [Review][Patch] Complete the required UI and Fluxor verification work for Tasks 6.4-6.7 [_bmad-output/implementation-artifacts/1-6-counter-sample-domain-and-aspire-topology.md:111]
- [x] [Review][Patch] Align Aspire hosting package versions with the story's pinned 13.2.1 stack [Directory.Packages.props:19]
- [x] [Review][Patch] Scan the domain assembly for generated Fluxor features, not only `Counter.Web` [samples/Counter/Counter.Web/Program.cs:14]
- [x] [Review][Patch] Generate or aggregate bounded-context manifests instead of per-projection manifests with empty command lists [src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:54]
- [x] [Review][Patch] Implement the required Fluent UI accent color configuration for `#0097A7` [samples/Counter/Counter.Web/Components/App.razor:1]
- [x] [Review][Patch] Log partial `*Registration` matches in `AddHexalithDomain<T>()` as the story requires [src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:63]

## Dev Notes

### Critical Architecture Constraints

**Technical Stack Versions (PINNED in Directory.Packages.props):**

- .NET SDK: 10 (pinned in global.json)
- .NET Aspire: 13.2.1 (exactly)
- Fluent UI Blazor: `5.0.0-rc.2-26098.1` (v5 RC2 per ADR-003; pre-release -- if RC2 bugs found, document in PR notes and escalate to W2 tech debt)
- Fluxor: 6.9.0
- Roslyn: 4.12.0 (exactly; 5.x breaks IDE analyzer load context)
- xUnit: v3 (3.2.2), bUnit: 2.7.2, Verify: 31.15.0

**SourceTools Analyzer Reference Pattern (Counter.Domain.csproj):**
**WARNING:** The existing scaffold uses the WRONG format `SetTargetFramework="TargetFramework=netstandard2.0"`. Fix it to use the correct format `SetTargetFramework="netstandard2.0"` as part of Task 1. Verify after fixing:

```xml
<ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
    SetTargetFramework="netstandard2.0" />
```

Do NOT change this pattern. Contracts (`Hexalith.FrontComposer.Contracts`) is a normal `<ProjectReference>`.

**Generated File Output Location:**
All generated files go to `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`. Per projection type, 5 files:

- `{TypeName}.g.razor.cs` -- Blazor DataGrid component (RenderTreeBuilder, `IState<T>` injection, IDisposable)
- `{TypeName}Feature.g.cs` -- Fluxor feature: `record {TypeName}State(bool IsLoading, IReadOnlyList<{TypeName}>? Items, string? Error)`
- `{TypeName}Actions.g.cs` -- Past-tense action records with CorrelationId
- `{TypeName}Reducers.g.cs` -- Static reducer methods
- `{TypeName}Registration.g.cs` -- Per-type registration helper file. The contained registration class may still be named from the bounded context. **This story adds a `DomainManifest Manifest` member and `RegisterDomain(IFrontComposerRegistry)` method to that file.**

Counter's current generator contract emits these 5 files for `CounterProjection`. `IncrementCommand` remains a domain contract type, but command-specific generation is not part of this story.

### CounterDomain Marker Type

`CounterDomain` is a marker class used as the generic parameter for `AddHexalithDomain<T>()`. It lives in `Counter.Domain` and provides the assembly reference for reflection-based discovery of generated registration classes.

```csharp
// CounterDomain.cs (in samples/Counter/Counter.Domain/)
[BoundedContext("Counter")]
public class CounterDomain { }
```

`AddHexalithDomain<T>()` uses `typeof(T).Assembly` to scan for generated `*Registration` classes exposing `Manifest` and `RegisterDomain(IFrontComposerRegistry)` static members. The marker class MUST be in the same assembly as the domain types.

### Domain Registration Wiring Chain

End-to-end flow from domain types to rendered navigation:

```text
Counter.Domain types ([Projection], [Command])
  -> SourceTools generator (compile-time)
  -> CounterProjectionRegistration.g.cs with static Manifest + RegisterDomain(IFrontComposerRegistry)
  -> Counter.Web Program.cs: AddHexalithDomain<CounterDomain>()
  -> Discovers *Registration helpers via typeof(CounterDomain).Assembly reflection
  -> Applies each helper's Manifest + RegisterDomain(...) to the singleton registry
  -> IFrontComposerRegistry.GetManifests() now includes the Counter bounded context
  -> Shell derives navigation items from the registered manifests
  -> "Counter" appears in sidebar navigation
```

### Fluxor Assembly Scanning (CRITICAL PATTERN)

**DO NOT call `services.AddFluxor()` twice** -- the second call overwrites the first, losing Shell's ThemeState/DensityState registrations.

Instead, `AddHexalithFrontComposer()` must accept an optional `Action<FluxorOptions>` parameter:

```csharp
// Shell/Extensions/ServiceCollectionExtensions.cs (UPDATED SIGNATURE)
public static IServiceCollection AddHexalithFrontComposer(
    this IServiceCollection services,
    Action<Fluxor.Options>? configureFluxor = null)
{
    services.AddFluxor(o =>
    {
        o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly);  // Shell assembly
        configureFluxor?.Invoke(o);  // Consumer adds their assemblies
    });
    services.AddSingleton<IStorageService, InMemoryStorageService>();
    return services;
}
```

Counter.Web usage:

```csharp
builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly));  // Adds Counter.Web assembly
builder.Services.AddHexalithDomain<CounterDomain>();   // Registers domain via generated code
```

This ensures both Shell (ThemeState, DensityState) and Counter (CounterProjectionState) Fluxor features are discovered in a single scan.

**Fluxor State Management (ADR-008):**

- Use explicit `IState<T>` injection + subscribe/dispose pattern (NEVER FluxorComponent base class)
- All actions are immutable records with past-tense naming, always include `CorrelationId` property
- Include `<Fluxor.Blazor.Web.StoreInitializer />` in root layout
- If Counter.Web fails to initialize Fluxor on first run, check: (1) `StoreInitializer` is in `MainLayout.razor`, (2) Fluxor scan includes `typeof(Program).Assembly`

**No Effects Generated (Intentional):**
Counter is a **read-only demo** until Effects are added in Epic 5. Generated code wires Actions/Reducers but no HTTP fetch or SignalR subscriptions. Counter will show the empty state message until a manual `LoadedAction` dispatch provides data. No command form wiring in this story.

### Counter Domain Model Specification

```csharp
// CounterDomain.cs -- marker class for AddHexalithDomain<T>()
[BoundedContext("Counter")]
public class CounterDomain { }

// CounterProjection.cs
[Projection]
public class CounterProjection
{
    public string Id { get; set; }
    public int Count { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}

// IncrementCommand.cs
[Command]
public class IncrementCommand
{
    public string MessageId { get; set; }  // ULID for correlation
    public string TenantId { get; set; }   // Multi-tenant scoping
    public int Amount { get; set; } = 1;
}
```

All files in `samples/Counter/Counter.Domain/`. Total non-domain code: <= 10 lines.

**Counter.Web Program.cs Pattern:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHexalithFrontComposer(
    o => o.ScanAssemblies(typeof(Program).Assembly));
builder.Services.AddHexalithDomain<CounterDomain>();
var app = builder.Build();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

**Counter.AppHost Program.cs Pattern (pure orchestration):**

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.Counter_Web>("counter-web");
await builder.Build().RunAsync();
```

### Aspire AppHost Setup

**New Project:** `samples/Counter/Counter.AppHost/Counter.AppHost.csproj`

- Use `Aspire.AppHost` SDK (`<IsAspireHost>true</IsAspireHost>`)
- Reference Counter.Web as project
- MUST be added to `Hexalith.FrontComposer.sln` BEFORE building (required for `Projects.Counter_Web` namespace)
- Aspire version: 13.2.1 — **NOTE:** Aspire packages are NOT yet in `Directory.Packages.props`. Task 3.1 must add `Aspire.Hosting.AppHost` and `Aspire.Hosting.Sdk` package versions to the root `Directory.Packages.props` before the AppHost project will restore.
- If build fails with "namespace 'Projects' does not exist", verify Counter.AppHost project GUID is registered in .sln

**Architecture boundary:** AppHost is pure orchestration -- NO domain logic, NO `.WithDomain<T>()`. All domain registration happens in Counter.Web's `Program.cs`.

### UI & UX Requirements

**Shell Layout (UX-DR14):** Header (top), navigation sidebar (left, `FluentNav`, ~240px/~48px), content area (main). Use `FluentLayout` + `FluentLayoutItem`. Accent color `#0097A7` via `FluentDesignSystemProvider`. Zero-override CSS strategy. Use default Fluent UI v5 typography and Compact density mode. Refer to UX-DR14/21/23/27 for component details.

**DataGrid Column Rendering (from Story 1.5 generator):**

| .NET Type | Rendering | Format |
| --------- | --------- | ------ |
| string | Text column | raw value |
| int/long | Right-aligned, locale-formatted | N0 |
| decimal/double | Right-aligned, locale-formatted | N2 |
| bool | "Yes" / "No" text | -- |
| DateTime/DateTimeOffset | Short date per CultureInfo | "d" format |
| enum | Humanized label (max 30 chars + ellipsis) | -- |
| null values | Em dash (\u2014) | -- |

**Column Header Label Resolution Chain (UX-DR21):** `[Display(Name)]` > humanized CamelCase > raw field name

**Empty State:** Exact message: "No counter data yet. Send your first Increment Counter command." Centered, with `FluentIcon` + `FluentButton`.

**Fluent UI Component Mapping:**

| Concept | Component |
| ------- | --------- |
| Shell | `FluentLayout` + `FluentLayoutItem` |
| Sidebar | `FluentNav` (items derived from `IFrontComposerRegistry.GetManifests()`) |
| DataGrid | `FluentDataGrid` |
| Empty state | Custom with `FluentIcon` + `FluentButton` |

### Prerequisites to Create in This Story

**CRITICAL: These APIs do not exist yet. Task numbering reflects dependency order.**

1. **Existing `IFrontComposerRegistry` / `DomainManifest` contract** (Task 4.1) -- Verify the current Contracts shape first and extend or adapt it if necessary. Do NOT create a second registry abstraction for the Counter sample.

2. **Generated registration helper** (Task 0) -- Current `RegistrationEmitter.cs` produces static properties only. Update it to also generate a `DomainManifest Manifest` member plus `public static void RegisterDomain(IFrontComposerRegistry registry)` in the existing `{TypeName}Registration.g.cs` file pattern, compatible with the current registry/manifest contract.

3. **`AddHexalithDomain<T>()` extension** (Task 4.3) -- Does NOT exist in Shell's `ServiceCollectionExtensions.cs`. Creates a service collection extension that discovers generated registration helper classes via `typeof(T).Assembly` reflection and applies their `Manifest` / `RegisterDomain(...)` members to the runtime registry.

4. **`AddHexalithFrontComposer()` signature update** (Task 2.1) -- Must add optional `Action<FluxorOptions>?` parameter to allow consumers to add their assemblies to the Fluxor scan. Without this, generated Fluxor features in Counter.Domain won't be discovered.

5. **Blazor infrastructure files** (Task 2) -- Counter.Web has only empty `Program.cs`. Must create: `App.razor`, `MainLayout.razor`, `_Imports.razor`, `Properties/launchSettings.json`.

6. **Existing Shell.Tests** (Task 2.1) -- Changing `AddHexalithFrontComposer()` to accept `Action<Fluxor.Options>?` is backward-compatible (optional param), but verify that all existing Shell.Tests still pass after the signature change. The parameterless call path must continue to work identically.

### Previous Story Intelligence (Story 1.5)

**Key Learnings:**

- CompilationHelper.cs: Enhanced with proper assembly references for Fluxor/FluentUI/ASP.NET Core -- use for new integration tests
- All emitted code validated via `CSharpSyntaxTree.ParseText()` for zero syntax errors
- RS2008 build error: Every new diagnostic MUST have an entry in `AnalyzerReleases.Unshipped.md`
- xUnit: Pass `TestContext.Current.CancellationToken` to all generator/compilation calls
- BoundedContext grouping: Each type contributes its own `{TypeName}Registration.g.cs` as partial class; Roslyn merges partials at compile time
- Use Verify framework with `.verified.txt` snapshots stored alongside test class
- Test naming: `{Method}_{Scenario}_{Expected}`

**Baseline:** 139 SourceTools tests pass, 182 total solution tests pass. Full build: 0 warnings, 0 errors.

### Post-W1 Note: Template Strategy (UX-DR66)

Counter sample is intentionally minimal. Post-W1, the shipped project template will use a Task Tracker sample domain instead. Do NOT over-engineer Counter for this future migration.

### Downstream Impact (Stories 1.7 and 1.8)

**Story 1.7 (CI Pipeline) expects:** Counter.Web fully buildable with Gates 1-3. Inner loop SLA: unit + component tests < 5 minutes.

**Story 1.8 (Hot Reload) expects:** Modifying Counter domain `[Projection]` attributes triggers incremental rebuild. Counter.Web DataGrid reflects changes without full restart. Hot reload latency < 2s (NFR10), incremental rebuild < 500ms (NFR8).

### References

- [Source: _bmad-output/planning-artifacts/epics.md - Epic 1, Story 1.6 section]
- [Source: _bmad-output/planning-artifacts/architecture.md - W1/W2 project structure, Aspire topology, Fluxor patterns]
- [Source: _bmad-output/planning-artifacts/prd.md - FR2, FR3, FR6, FR13, FR38, FR62; NFR8, NFR10, NFR82, NFR83]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md - UX-DR14, UX-DR21, UX-DR23, UX-DR27, UX-DR35, UX-DR56, UX-DR65]
- [Source: _bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md - Generator pipeline, test patterns, code patterns]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6 (1M context)

### Debug Log References
- Task 1.4: `SetTargetFramework="TargetFramework=netstandard2.0"` is correct MSBuild format; story dev note was mistaken about changing it to bare `netstandard2.0`
- ASP0006 analyzer in .NET 10 treats `seq++` in RenderTreeBuilder as error (TreatWarningsAsErrors=true); suppressed via NoWarn in Counter.Domain and Counter.Web. Pre-existing RazorEmitter issue to fix in Story 1.8.
- FluentUI v5 RC2: `FluentDesignSystemProvider` removed; replaced with `FluentLayout` + `FluentLayoutItem` + `FluentProviders` pattern
- FluentUI v5 Icons package does not exist at RC2 version; icons not used in navigation (nav items render without icons)
- Fluxor Options type is `Fluxor.DependencyInjection.FluxorOptions` (discovered via reflection)
- Aspire `KubernetesClient` transitive dependency has known moderate vulnerability (NU1902); suppressed in AppHost

### Completion Notes List
- Task 0: RegistrationEmitter now emits per-type `{TypeName}Registration` class (non-partial) with `DomainManifest Manifest` property and `RegisterDomain(IFrontComposerRegistry)` method. Existing snapshot tests updated. All 147 SourceTools tests pass.
- Task 1: Created CounterDomain.cs, CounterProjection.cs, IncrementCommand.cs. Counter.Domain builds with 5 generated files. Non-domain code is 2 significant lines (attribute + class declaration).
- Task 4: Created FrontComposerRegistry (singleton) and DomainRegistrationAction in Shell. AddHexalithFrontComposer() accepts optional Action<FluxorOptions>. AddHexalithDomain<T>() discovers Registration classes via GetExportedTypes(). All 34 Shell.Tests pass.
- Task 2: Counter.Web fully built with Program.cs, App.razor, Routes.razor, MainLayout.razor (FluentLayout per UX-DR14), _Imports.razor, launchSettings.json, CounterPage.razor, Home.razor. Navigation derived from IFrontComposerRegistry.GetManifests().
- Task 3: Counter.AppHost created with Aspire.AppHost.Sdk 13.2.1. Added to solution. Pure orchestration — no domain logic.
- Task 5: Full solution builds with 0 errors, 0 warnings. DataGrid column order verified via integration test (Id text, Count numeric N0, LastUpdated datetime "d").
- Task 6: Review fixes added the remaining Story 1.6 coverage in `Hexalith.FrontComposer.Shell.Tests` (generated Counter grid snapshot, Fluxor DI integration, story-specific empty state, and null/boolean snapshot). `Hexalith.FrontComposer.Shell.Tests` (43) and `Hexalith.FrontComposer.SourceTools.Tests` (150) pass after the review patch set.

### File List
- src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs (modified)
- src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs (modified)
- src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs (new)
- src/Hexalith.FrontComposer.Shell/Registration/DomainRegistrationAction.cs (new)
- samples/Counter/Counter.Domain/Counter.Domain.csproj (modified)
- samples/Counter/Counter.Domain/CounterDomain.cs (new)
- samples/Counter/Counter.Domain/CounterProjection.cs (new)
- samples/Counter/Counter.Domain/IncrementCommand.cs (new)
- samples/Counter/Counter.Web/Counter.Web.csproj (modified)
- samples/Counter/Counter.Web/Program.cs (modified)
- samples/Counter/Counter.Web/Components/App.razor (new)
- samples/Counter/Counter.Web/Components/Routes.razor (new)
- samples/Counter/Counter.Web/Components/_Imports.razor (new)
- samples/Counter/Counter.Web/Components/Layout/MainLayout.razor (new)
- samples/Counter/Counter.Web/Components/Pages/Home.razor (new)
- samples/Counter/Counter.Web/Components/Pages/CounterPage.razor (new)
- samples/Counter/Counter.Web/Properties/launchSettings.json (new)
- samples/Counter/Counter.AppHost/Counter.AppHost.csproj (new)
- samples/Counter/Counter.AppHost/Program.cs (new)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CounterDomainIntegrationTests.cs (new)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/TestSources.cs (modified)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.SingleProjection_Snapshot.verified.txt (modified)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.BoundedContextDisplayLabel_Snapshot.verified.txt (modified)
- tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs (new)
- Directory.Packages.props (modified)
- Hexalith.FrontComposer.sln (modified)

### Change Log
- 2026-04-14: Story 1.6 implementation complete. Extended RegistrationEmitter with Manifest/RegisterDomain. Created Counter sample domain with 3 types. Built Counter.Web with FluentUI v5 shell layout. Created Counter.AppHost Aspire topology. Added FrontComposerRegistry and AddHexalithDomain<T>() to Shell. Added 6 new tests (196 total, 0 failures).
