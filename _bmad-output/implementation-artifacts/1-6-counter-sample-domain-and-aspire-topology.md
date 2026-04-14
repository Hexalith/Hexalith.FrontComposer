# Story 1.6: Counter Sample Domain & Aspire Topology

Status: ready-for-dev

## Story

As a developer,
I want a working Counter sample domain running in an Aspire topology that demonstrates end-to-end auto-generation from domain attributes to rendered DataGrid,
So that I can validate the framework works and use it as a reference for building my own domains.

## Acceptance Criteria

1. **Counter.Domain Auto-Generation**
   Given the Counter.Domain project exists with `[BoundedContext("Counter")]`, `[Projection]`-annotated types, and `[Command]`-annotated records
   When `dotnet build` runs
   Then the source generator produces DataGrid components, Fluxor state, and domain registration without errors
   And non-domain code in Counter.Domain is <= 10 lines (NuGet reference, registration)

2. **Counter.Web Project Configuration**
   Given the Counter.Web project references Shell + SourceTools (as analyzer)
   When the project is built
   Then SourceTools is referenced via `OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`, `SetTargetFramework="netstandard2.0"`
   And Contracts is referenced as a normal project reference

3. **Aspire Topology & Domain Registration**
   Given the Counter.AppHost Aspire project exists and Counter.Web calls `AddHexalithDomain<CounterDomain>()` in its Program.cs
   When `dotnet run` is executed on the AppHost
   Then the Aspire topology starts with Counter.Web orchestrated as a project resource
   And Counter.Web's DI initializes domain registration via the generated `CounterDomainRegistration`
   And navigating to the web application shows a basic shell layout with the Counter bounded context in navigation

4. **DataGrid Rendering & UI Compliance**
   Given the application is running
   When the developer navigates to the Counter projection view
   Then a DataGrid renders with auto-generated columns from the projection type
   And the shell uses Fluent UI Blazor v5 with accent color `#0097A7`
   And the zero-override strategy is followed (no custom CSS on Fluent UI components)
   And the basic shell layout follows UX-DR14 (header, navigation sidebar, content area)

5. **Time-to-First-Render (NFR82)**
   Given a cold machine with .NET 10 SDK installed
   When the developer clones the repo and runs the Counter sample
   Then time-to-first-render is <= 5 minutes

6. **Definition of Done**
   - All acceptance criteria 1-5 verified
   - All 139+ existing SourceTools tests passing with zero regressions
   - Integration smoke test added (Counter domain -> 5 generated files)
   - bUnit render test passing (generated DataGrid renders in test context)
   - Fluxor integration test passing (domain registration + state discoverable via DI)
   - Solution builds with zero errors and zero framework warnings
   - Code reviewed per team standards

## Tasks / Subtasks

- [ ] Task 1: Create Counter.Domain with annotated domain types (AC: #1)
  - [ ] 1.1 Add `[BoundedContext("Counter")]` marker attribute to domain namespace
  - [ ] 1.2 Create `CounterProjection.cs` with `[Projection]`: fields `Id` (string), `Count` (int), `LastUpdated` (DateTimeOffset)
  - [ ] 1.3 Create `IncrementCommand.cs` with `[Command]`: fields `MessageId` (string/ULID), `TenantId` (string), `Amount` (int, default 1)
  - [ ] 1.4 Verify non-domain code is <= 10 lines total (NuGet refs + registration only)
  - [ ] 1.5 Verify `dotnet build` generates all 5 output files per type without errors

- [ ] Task 2: Complete Counter.Web with shell, routing, and DI (AC: #2, #4)
  - [ ] 2.1 Implement `Program.cs` with `AddHexalithFrontComposer()`, Fluxor scan, routing, and domain registration
  - [ ] 2.2 Create root layout (`MainLayout.razor`) per UX-DR14: header region, navigation sidebar, content area
  - [ ] 2.3 Add `<Fluxor.Blazor.Web.StoreInitializer />` in root layout
  - [ ] 2.4 Configure Fluent UI Blazor v5 with accent color `#0097A7` and zero-override CSS strategy
  - [ ] 2.5 Add `App.razor` with router and `_Imports.razor` with required usings
  - [ ] 2.6 Wire Counter bounded context into sidebar navigation via generated `CounterDomainRegistration`

- [ ] Task 3: Create Counter.AppHost Aspire project (AC: #3)
  - [ ] 3.1 Create `samples/Counter/Counter.AppHost/Counter.AppHost.csproj` with Aspire AppHost SDK (`<IsAspireHost>true</IsAspireHost>`)
  - [ ] 3.2 Add Counter.AppHost to `Hexalith.FrontComposer.sln`
  - [ ] 3.3 Implement `Program.cs` with `DistributedApplication.CreateBuilder` orchestrating Counter.Web via `AddProject<Projects.Counter_Web>()`
  - [ ] 3.4 Verify `dotnet run` on AppHost starts topology and opens Aspire dashboard
  - [ ] 3.5 Verify Counter bounded context appears in navigation after Counter.Web's DI initializes domain registration

- [ ] Task 4: Create `AddHexalithDomain<T>()` service collection extension (AC: #3, PREREQUISITE)
  - [ ] 4.1 Add `AddHexalithDomain<T>()` generic extension method to Shell's `ServiceCollectionExtensions.cs` (or new file in Shell/Extensions/)
  - [ ] 4.2 Extension must discover and invoke generated `{BoundedContext}DomainRegistration` static registration (see prerequisite note below)
  - [ ] 4.3 Wire into Counter.Web's `Program.cs`: `builder.Services.AddHexalithDomain<CounterDomain>()`
  - [ ] 4.4 Verify domain registration populates navigation group, projections, and commands in composition shell

- [ ] Task 5: Validate end-to-end rendering and performance (AC: #4, #5)
  - [ ] 5.1 Verify DataGrid renders with auto-generated columns: Id (text), Count (right-aligned N0), LastUpdated (short date "d")
  - [ ] 5.2 Verify null values render as em dash (\u2014), booleans as "Yes"/"No"
  - [ ] 5.3 Verify column headers follow label resolution chain: `[Display(Name)]` > humanized CamelCase > raw field name
  - [ ] 5.4 Verify empty state renders meaningful message: "No counter data yet..."
  - [ ] 5.5 Verify time-to-first-render <= 5 minutes on cold machine

- [ ] Task 6: Verify build gates and test suite (AC: #1, #2, #6)
  - [ ] 6.1 Confirm `dotnet build` on full solution completes with zero errors, zero framework warnings
  - [ ] 6.2 Confirm all 139+ existing SourceTools tests still pass (zero regressions)
  - [ ] 6.3 Add integration smoke test: Counter domain attributes -> generator -> verify 5 output files exist and compile without Roslyn errors
  - [ ] 6.4 Add bUnit render test: instantiate generated `CounterProjectionView`, dispatch `CounterProjectionLoadedAction` with test data, assert columns render (Id text, Count right-aligned N0, LastUpdated short date)
  - [ ] 6.5 Add Fluxor integration test: call `AddHexalithDomain<CounterDomain>()`, resolve `IState<CounterProjectionState>` from DI, dispatch actions, verify state transitions
  - [ ] 6.6 Add empty state render test: verify DataGrid shows "No counter data yet..." message when state has no items
  - [ ] 6.7 Verify null value rendering (em dash) and boolean rendering ("Yes"/"No") via bUnit snapshot

## Dev Notes

### Critical Architecture Constraints

**Technical Stack Versions (PINNED in Directory.Packages.props):**
- .NET SDK: 10 (pinned in global.json)
- .NET Aspire: 13.2.1 (exactly)
- Fluent UI Blazor: `5.0.0-rc.2-26098.1` (v5 RC2 per ADR-003)
- Fluxor: 6.9.0
- Roslyn: 4.12.0 (exactly; 5.x breaks IDE analyzer load context)
- xUnit: v3 (3.2.2), bUnit: 2.7.2, Verify: 31.15.0

**SourceTools Analyzer Reference Pattern (Counter.Domain.csproj):**
Already configured correctly in existing scaffold:
```xml
<ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"
    SetTargetFramework="netstandard2.0" />
```
Do NOT change this pattern. Contracts is a normal `<ProjectReference>`.

**Generated File Output Location:**
All generated files go to `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` with these names:
- `{TypeName}.g.razor.cs` -- Blazor DataGrid component with RenderTreeBuilder, IState<T> injection, IDisposable lifecycle
- `{TypeName}Feature.g.cs` -- Fluxor feature with state shape: `record {TypeName}State(bool IsLoading, IReadOnlyList<{TypeName}>? Items, string? Error)`
- `{TypeName}Actions.g.cs` -- Past-tense action records with CorrelationId: `LoadRequestedAction()`, `LoadedAction(Items)`, `LoadFailedAction(Error)`
- `{TypeName}Reducers.g.cs` -- Static reducer methods
- `{TypeName}Registration.g.cs` -- Domain registration grouping projections under bounded context

**Fluxor State Management (ADR-008):**
- Use explicit `IState<T>` injection + subscribe/dispose pattern (NEVER FluxorComponent base class)
- All actions are immutable records with past-tense naming, always include `CorrelationId` property
- ThemeState (Light/Dark/System) and DensityState (Compact/Comfortable/Roomy) already exist in Shell
- CommandLifecycleState is ephemeral (not persisted)
- Persistence via `IStorageService` with key pattern `{tenantId}:{userId}:{featureName}:{discriminator}`
- Initialize Fluxor in Program.cs: `services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly))`
- Include `<Fluxor.Blazor.Web.StoreInitializer />` in root layout

**No Effects Generated (Intentional):**
Generated code is self-contained but NOT autonomous. Actions/Reducers are wired but no HTTP fetch or SignalR subscriptions. Effects are hand-written by consumers or come from Epic 5 integration. Counter will show initial empty state until manual dispatch provides data.

**Fluent UI v5 RC2 Pre-Release Risk:**
Fluent UI Blazor `5.0.0-rc.2-26098.1` is a release candidate, not GA. If RC2 has bugs or API incompatibilities, workarounds may be needed. Document any RC2-specific issues encountered. Upgrade path to v5 GA should be straightforward (API-compatible RC).

### Prerequisites to Create in This Story

**CRITICAL: These APIs do not exist yet and must be created as part of this story:**

1. **`AddHexalithDomain<T>()` extension method** -- Does NOT exist in Shell's `ServiceCollectionExtensions.cs` (which only has `AddHexalithFrontComposer()`). Must be created as a new generic extension that discovers and invokes the generated domain registration. This is a **service collection** extension called inside Counter.Web's `Program.cs`, NOT an Aspire AppHost extension.

2. **Generated `RegisterDomain()` method** -- The current `RegistrationEmitter.cs` produces a partial class with static properties but does NOT emit a `RegisterDomain(IFrontComposerRegistry)` method. Either:
   - (a) Update `RegistrationEmitter` to also generate a `RegisterDomain()` static method, OR
   - (b) Have `AddHexalithDomain<T>()` use the generated static properties directly via reflection or convention
   - Decision: Prefer (a) -- extend the emitter to generate the registration method so the consumption pattern is explicit and type-safe.

3. **`IFrontComposerRegistry` interface** -- Verify this exists in Contracts. If not, create it with the methods needed for domain registration (`AddProjection<T>()`, `AddCommand<T>()`, `AddNavGroup()`).

4. **Blazor infrastructure files** -- Counter.Web currently has only an empty `Program.cs`. Must create from scratch: `App.razor`, `MainLayout.razor`, `_Imports.razor`, `Properties/launchSettings.json`.

**Architecture Boundary (from review):** Domain registration happens INSIDE Counter.Web's DI container via `AddHexalithDomain<T>()`. Counter.AppHost is a PURE orchestration layer -- it only does `builder.AddProject<Projects.Counter_Web>()` and delegates all domain concerns to Counter.Web. Do NOT create domain registration extensions on the Aspire distributed application builder.

### UI & UX Requirements

**Shell Layout (UX-DR14):**
- Header region (top) -- app title, theme toggle
- Navigation sidebar (left) -- Counter bounded context auto-discovered, collapsible at <1366px
- Content area (main) -- DataGrid rendering
- Use `FluentLayout` + `FluentLayoutItem` for structure
- Sidebar: `FluentNav` with "Counter" group, ~240px expanded / ~48px collapsed

**Accent Color & Theming (UX-DR23):**
- Accent color: `#0097A7` (teal) -- applies to active nav indicators, primary buttons
- Zero-override CSS strategy: NO custom CSS on Fluent UI components
- Theme support: Light (default), Dark, System -- persisted in IStorageService

**DataGrid Column Rendering (from Story 1.5 generator):**
| .NET Type | Rendering | Format |
|-----------|-----------|--------|
| string | Text column | raw value |
| int/long | Right-aligned, locale-formatted | N0 |
| decimal/double | Right-aligned, locale-formatted | N2 |
| bool | "Yes" / "No" text | -- |
| DateTime/DateTimeOffset | Short date per CultureInfo | "d" format |
| DateOnly | Short date | "d" format |
| enum | Humanized label (max 30 chars + ellipsis) | -- |
| null values | Em dash (\u2014) | -- |
| Guid | 8-char truncated text | -- |

**Column Header Label Resolution Chain (UX-DR21):**
`[Display(Name)]` > humanized CamelCase (via CamelCaseHumanizer) > raw field name

**Empty State:**
- Message: "No counter data yet. Send your first Increment Counter command."
- Components: `FluentIcon` + `FluentButton` linking to command form
- Centered in content area

**Typography (Fluent UI type ramp, no custom overrides):**
- Nav group header ("Counter"): `Subtitle1`
- View title ("Counter Status"): `Title3`
- Column headers: `Body1Strong`
- Field labels: `Body1Strong`
- Empty state message: `Body1`
- Timestamps: `Body2`

**Display Density (factory default = Compact):**
- Compact: maximizes visible rows (desktop >1366px)
- Comfortable: balanced (tablet <1024px auto-switch)
- Roomy: screen magnifier users

### Counter Domain Model Specification

**Counter.Domain Project (samples/Counter/Counter.Domain/):**
```csharp
// CounterProjection.cs
[BoundedContext("Counter")]
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
Total non-domain code: <= 10 lines (csproj NuGet refs + optional registration line).

**Counter.Web Program.cs Pattern:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHexalithFrontComposer();  // Master extension from Shell
builder.Services.AddHexalithDomain<CounterDomain>();  // Invokes generated RegisterDomain()
// Routing, Razor, etc.
var app = builder.Build();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

**Counter.AppHost Program.cs Pattern (Aspire):**
```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.Counter_Web>("counter-web");
await builder.Build().RunAsync();
```

### Aspire AppHost Setup

**New Project:** `samples/Counter/Counter.AppHost/Counter.AppHost.csproj`
- Use `Aspire.AppHost` SDK (`<IsAspireHost>true</IsAspireHost>`)
- Reference Counter.Web as project
- Must be added to `Hexalith.FrontComposer.sln`
- Aspire version: 13.2.1 (match Directory.Packages.props)

**AppHost is pure orchestration -- NO domain logic:**
- AppHost only does `builder.AddProject<Projects.Counter_Web>("counter-web")`
- All domain registration happens inside Counter.Web's `Program.cs` via `AddHexalithDomain<CounterDomain>()`
- Do NOT create `.WithDomain<T>()` or any domain extensions on the Aspire `DistributedApplicationBuilder`

**NFR82 Cold-Start Definition:**
"Cold machine" for time-to-first-render measurement means: CI agent or clean local machine with .NET 10 SDK installed, no cached NuGet packages, no prior build artifacts. Measure from `git clone` to browser rendering the Counter shell. This is a baseline check, not a hard gate -- do not block the story if a slow network or overloaded CI agent pushes past 5 minutes.

### Fluent UI Component Mapping

| Counter Concept | Fluent UI Component |
|---|---|
| Application shell | `FluentLayout` + `FluentLayoutItem` |
| Sidebar nav | `FluentNav` |
| Counter status table | `FluentDataGrid` (native HTML table in v5) |
| Amount input field | `FluentNumberField` (auto-inferred from int) |
| Submit button | `FluentButton` (Primary) |
| Loading state | `FluentProgressRing` |
| Empty state | Custom with `FluentIcon` + `FluentButton` |
| Hamburger toggle | `FluentLayoutHamburger` |

### Project Structure Notes

**Solution folder layout (after Story 1.6):**
```
Hexalith.FrontComposer/
├── src/
│   ├── Hexalith.FrontComposer.Contracts/       [exists - net10.0;netstandard2.0]
│   ├── Hexalith.FrontComposer.SourceTools/      [exists - netstandard2.0, Roslyn generator]
│   └── Hexalith.FrontComposer.Shell/            [exists - net10.0, Blazor RCL]
├── samples/Counter/
│   ├── Counter.Domain/                          [exists - EMPTY, needs domain types]
│   ├── Counter.Web/                             [exists - EMPTY stub, needs full implementation]
│   └── Counter.AppHost/                         [NEW - Aspire AppHost, must create]
└── tests/
    ├── Hexalith.FrontComposer.SourceTools.Tests/ [exists - 139+ tests passing]
    ├── Hexalith.FrontComposer.Contracts.Tests/   [exists]
    └── Hexalith.FrontComposer.Shell.Tests/       [exists]
```

**MSBuild Walk-Up Isolation:**
- `FrontComposerRoot` property guard prevents submodule import chain leakage
- Counter projects inherit from root `Directory.Build.props` via walk-up
- EventStore/Tenants submodules have own `Directory.Build.props` with `<ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>`
- Authoritative EventStore path in `deps.local.props`: `$(MSBuildThisFileDirectory)Hexalith.EventStore`

### Previous Story Intelligence (Story 1.5)

**Key Learnings:**
- CamelCaseHumanizer: Requires 3+ consecutive uppercase chars before inserting acronym-break space ("OrderIDs" stays together, not "Order I Ds")
- CompilationHelper.cs: Enhanced with proper assembly references for Fluxor/FluentUI/ASP.NET Core -- use this for any new integration tests
- All emitted code validated via `CSharpSyntaxTree.ParseText()` for zero syntax errors
- RS2008 build error: Every new diagnostic MUST have an entry in `AnalyzerReleases.Unshipped.md`
- xUnit: Pass `TestContext.Current.CancellationToken` to all generator/compilation calls
- All Transform output models implement `IEquatable<T>` with value-based equality using `EquatableArray<T>` for collections (required for Roslyn incremental caching)
- BoundedContext grouping: Each projection contributes its own `{TypeName}Registration.g.cs` as partial class; Roslyn merges partials at compile time (prevents "duplicate hint name" crash)

**Test Results from Story 1.5:**
- 139 SourceTools tests pass (78 new + 61 existing)
- 182 total solution tests pass
- Full build: 0 warnings, 0 errors
- 10 snapshot `.verified.txt` files committed

**Patterns to Follow:**
- Snapshot testing with Verify framework: `.verified.txt` files stored alongside test class
- Test naming: `{Method}_{Scenario}_{Expected}`
- Test data builders: `new CounterProjectionBuilder().WithCount(5).Build()`
- Integration tests use `CompilationHelper.CreateCompilation()` for Roslyn context

### Git Intelligence

**Recent commits (last 5):**
1. `8ff3dba` - Update project dependencies, mark story 1-4 done, 1-5 ready
2. `5bd730f` - Include Shell.Tests project, mark Story 1.3 done
3. `4d3b726` - Update .gitignore for cursor AI rules
4. `05e315b` - Merge PR #7 for sprint status
5. `2ef2914` - Add sprint status tracking

**Patterns from git history:**
- Conventional commits style (lowercase imperative descriptions)
- Single-session story implementation with comprehensive test coverage
- Solution file updated when adding new projects

### Post-W1 Note: Template Strategy (UX-DR66)

Counter sample is intentionally minimal and serves as a reference implementation. Post-W1, the shipped project template will use a Task Tracker sample domain instead (list with status badges, inline actions, command form, lifecycle loop, meaningful empty states). Counter remains as a minimal example in documentation only. Do NOT over-engineer the Counter sample for this future migration.

### Downstream Impact (Stories 1.7 and 1.8)

**Story 1.7 (CI Pipeline) expects:**
- Counter.Web fully buildable with Gates 1-3
- Gate 1: Contracts builds (netstandard2.0 isolation)
- Gate 2: Full solution builds (including all Counter.* projects)
- Gate 3: SourceTools.Tests run and pass
- Inner loop SLA: unit + component tests < 5 minutes

**Story 1.8 (Hot Reload) expects:**
- Modifying Counter domain [Projection] attributes triggers incremental rebuild
- Counter.Web DataGrid reflects changes without full restart
- Hot reload latency < 2 seconds (NFR10)
- Incremental rebuild < 500ms per domain assembly (NFR8)

### References

- [Source: _bmad-output/planning-artifacts/epics.md - Epic 1, Story 1.6 section]
- [Source: _bmad-output/planning-artifacts/architecture.md - W1/W2 project structure, Aspire topology, Fluxor patterns]
- [Source: _bmad-output/planning-artifacts/prd.md - FR2, FR3, FR6, FR13, FR38, FR62; NFR8, NFR10, NFR82, NFR83]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md - UX-DR14, UX-DR21, UX-DR23, UX-DR27, UX-DR35, UX-DR56, UX-DR65]
- [Source: _bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md - Generator pipeline, test patterns, code patterns]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
