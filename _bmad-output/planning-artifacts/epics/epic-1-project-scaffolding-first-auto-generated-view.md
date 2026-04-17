# Epic 1: Project Scaffolding & First Auto-Generated View

Developer can scaffold a FrontComposer project, register a domain with minimal ceremony, and see an auto-generated DataGrid from [Projection]-annotated types running in an Aspire topology.

### Story 1.1: Solution Structure & Build Infrastructure

As a developer,
I want a correctly structured .NET 10 solution with MSBuild spine, central package management, and submodule isolation,
So that all subsequent framework packages build cleanly from a single `dotnet restore && dotnet build`.

**Acceptance Criteria:**

**Given** the repository is cloned with submodules initialized
**When** `dotnet restore` is run from the repository root
**Then** restore completes successfully with zero warnings
**And** Directory.Build.props contains FrontComposerRoot property guard preventing walk-up import from EventStore/Tenants submodules
**And** submodules have ImportDirectoryBuildProps=false set

**Given** the solution file exists
**When** the solution structure is inspected
**Then** solution folders src/, samples/, tests/ exist
**And** Directory.Packages.props pins: Microsoft.CodeAnalysis.CSharp >=4.12.0, Fluxor.Blazor.Web 6.9.0, Fluent UI Blazor v5 (exact RC pin), xUnit 2.9.3, bUnit 2.7.2, FluentAssertions, coverlet.collector
**And** global.json pins .NET SDK 10.0.5
**And** .editorconfig and nuget.config are present

**Given** the deps.local.props and deps.nuget.props files exist
**When** UseNuGetDeps is set to false (default) in Directory.Build.props
**Then** deps.local.props is imported and EventStorePath resolves to the root Hexalith.EventStore submodule
**When** UseNuGetDeps is set to true
**Then** deps.nuget.props is imported for NuGet package references

**Given** the Hexalith.EventStore submodule is present
**When** `dotnet build` targets the submodule projects in isolation
**Then** the submodule compiles without errors, confirming integration stability

**References:** FR13, FR62 (partial), NFR93 (.NET 10 only), Architecture W1 Day 1

---

### Story 1.2: Contracts Package with Core Abstractions

As a developer,
I want a Contracts package containing domain model attributes, renderer abstractions, and storage service contracts,
So that all other framework packages and adopter code can depend on stable, change-controlled contracts.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer.Contracts project exists
**When** the project is built
**Then** it multi-targets net10.0 and netstandard2.0
**And** it has zero package dependencies (dependency-free)
**And** it compiles successfully under both target frameworks

**Given** the Contracts package is referenced
**When** a developer inspects available attributes
**Then** the following attributes are defined: [BoundedContext(name)], [Projection], [Command], [ProjectionRole(role)], [ProjectionBadge(slot)], [Display(Name)]
**And** ProjectionRole supports ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard (capped at 5)
**And** BadgeSlot supports Neutral, Info, Success, Warning, Danger, Accent (6 slots)

**Given** the Contracts package is referenced
**When** a developer inspects renderer abstractions
**Then** IRenderer<TModel, TOutput> interface is defined with RenderField, RenderDataGrid, RenderDetail methods
**And** IProjectionRenderer<TProjection> extends IRenderer
**And** RenderContext record is defined with TenantId, UserId, Mode, DensityLevel, IsReadOnly properties

**Given** the Contracts package is referenced
**When** a developer inspects storage abstractions
**Then** IStorageService is defined with 5 methods: GetAsync<T>, SetAsync<T>, RemoveAsync, GetKeysAsync, FlushAsync
**And** InMemoryStorageService implementation exists for Server-side and bUnit testing
**And** cache key pattern follows {tenantId}:{userId}:{featureName}:{discriminator}

**Given** the IRenderer and IStorageService contracts
**When** they are designed in this story
**Then** they are provisional -- designed with awareness of Fluxor state patterns (Story 1.3) but may be hardened after Fluxor setup validates the state shape
**And** any contract changes in Story 1.3 are applied as non-breaking additions (new methods/properties), not redesigns

**References:** FR3, FR6, FR13 (partial), UX-DR56 (Fc naming convention), Architecture ADR-001

---

### Story 1.3: Fluxor State Management Foundation

As a developer,
I want Fluxor state management configured with base feature infrastructure and explicit subscription patterns,
So that all generated and custom components use a consistent, AOT-friendly state management approach.

**Acceptance Criteria:**

**Given** the Shell project references Fluxor.Blazor.Web 6.9.0
**When** Fluxor is registered in the DI container
**Then** registration completes without errors
**And** Fluxor is configured for Blazor Server and Blazor Auto render modes

**Given** a component needs to subscribe to state
**When** the component uses the framework's subscription pattern
**Then** it uses explicit IState<T> inject with subscribe/dispose
**And** FluxorComponent base class is NEVER used (AOT-friendly requirement)
**And** subscription cleanup occurs on component dispose

**Given** the base state features are initialized
**When** the application starts
**Then** ThemeState feature exists with Light/Dark/System values (default: Light)
**And** DensityState feature exists with Compact/Comfortable/Roomy values (default: Comfortable for forms, Compact for DataGrids)
**And** all actions are immutable records with past-tense naming (e.g., ThemeChanged, DensityChanged)
**And** all actions include a CorrelationId property

**Given** ThemeState or DensityState changes
**When** the state is persisted
**Then** persistence uses IStorageService (InMemoryStorageService in dev/test)
**And** CommandLifecycleState is excluded from persistence (ephemeral only)

**References:** Architecture ADR-008, UX-DR23 (color system baseline), UX-DR27 (density baseline)

---

### Story 1.4: Source Generator - Parse Stage

As a developer,
I want a Roslyn incremental source generator that parses domain model attributes into a typed intermediate representation,
So that the framework can reason about domain types at compile time with a testable, pure-function core.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer.SourceTools project exists
**When** the project is built
**Then** it targets netstandard2.0 only
**And** IsRoslynComponent=true and EnforceExtendedAnalyzerRules=true are set
**And** it references Contracts (for attribute types) and Microsoft.CodeAnalysis.CSharp (PrivateAssets="all")
**And** it does NOT reference Shell, Fluxor, or Fluent UI

**Given** a C# type is annotated with [Projection]
**When** the Parse stage runs via ForAttributeWithMetadataName
**Then** the INamedTypeSymbol is extracted into a DomainModel IR record
**And** the IR captures: type name, namespace, properties (name, type, nullability), applied attributes ([BoundedContext], [ProjectionRole], [ProjectionBadge], [Display])
**And** the Parse function is pure (no side effects, no Compilation references in output)

**Given** a test project with known [Projection]-annotated types
**When** Parse stage snapshot tests run
**Then** golden file output (.approved.cs) matches expected DomainModel IR for each test type
**And** the field type coverage matrix includes: string, int, long, decimal, bool, DateTime, DateTimeOffset, DateOnly, enum, Guid, nullable variants, and collections (List<T>, IEnumerable<T>)

**Given** the generator encounters an unsupported field type
**When** the Parse stage processes it
**Then** a diagnostic HFC1001 is emitted (What/Expected/Got/Fix/DocsLink format)
**And** the field is included in the IR with an IsUnsupported flag

**References:** FR2 (partial), FR6 (partial), FR9 (partial), NFR8 (<500ms incremental), Architecture 3-stage pipeline

---

### Story 1.5: Source Generator - Transform & Emit Stages

As a developer,
I want the source generator to transform parsed domain models into Blazor DataGrid components with field type inference, label resolution, and data formatting,
So that annotating a type with [Projection] produces a fully rendered, correctly formatted DataGrid at compile time.

**Acceptance Criteria:**

**Given** a DomainModel IR from the Parse stage
**When** the Transform stage runs
**Then** it produces output models for: a Razor DataGrid component, Fluxor feature/actions/reducers, and a BoundedContext domain registration
**And** Fluxor types are emitted as fully-qualified name strings (no Fluxor dependency in SourceTools)

**Given** output models from Transform
**When** the Emit stage runs
**Then** generated files are named: {TypeName}.g.razor.cs, {TypeName}Feature.g.cs, {TypeName}Actions.g.cs, {BoundedContext}DomainRegistration.g.cs
**And** all generated files go to obj/{Config}/{TFM}/generated/HexalithFrontComposer/
**And** namespaces match folder paths exactly

**Given** a [Projection]-annotated type with various .NET property types
**When** the generated DataGrid renders
**Then** string fields render as text columns
**And** int/long/decimal fields render as right-aligned locale-formatted columns
**And** bool fields render as "Yes"/"No" text
**And** DateTime/DateTimeOffset fields render as short date per CultureInfo
**And** enum fields render as humanized labels (max 30 chars with ellipsis)
**And** null values render as em dash (--) in all columns
**And** column headers use the label resolution chain: [Display(Name)] > humanized CamelCase > raw field name

**Given** a type annotated with [BoundedContext("Orders")]
**When** the generator runs
**Then** it produces a domain registration grouping all projections under the "Orders" navigation section
**And** bounded context display labels support optional domain-language overrides

**Given** the Emit stage output
**When** snapshot tests run
**Then** golden HTML output (.approved.html) matches expected rendered structure using AngleSharp semantic DOM comparison

**References:** FR2, FR3, FR6, UX-DR21 (label resolution), UX-DR35 (data formatting), UX-DR65 (null handling)

---

### Story 1.6: Counter Sample Domain & Aspire Topology

As a developer,
I want a working Counter sample domain running in an Aspire topology that demonstrates end-to-end auto-generation from domain attributes to rendered DataGrid,
So that I can validate the framework works and use it as a reference for building my own domains.

**Acceptance Criteria:**

**Given** the Counter.Domain project exists with [BoundedContext("Counter")], [Projection]-annotated types, and [Command]-annotated records
**When** `dotnet build` runs
**Then** the source generator produces DataGrid components, Fluxor state, and domain registration without errors
**And** non-domain code in Counter.Domain is <= 10 lines (NuGet reference, registration)

**Given** the Counter.Web project references Shell + SourceTools (as analyzer)
**When** the project is built
**Then** SourceTools is referenced via OutputItemType="Analyzer", ReferenceOutputAssembly="false", SetTargetFramework="netstandard2.0"
**And** Contracts is referenced as a normal project reference

**Given** the Counter.AppHost Aspire project exists
**When** `dotnet run` is executed on the AppHost
**Then** the Aspire topology starts with all services registered
**And** the developer can register the domain via .WithDomain<T>() typed extension method
**And** navigating to the web application shows a basic shell layout with the Counter bounded context in navigation

**Given** the application is running
**When** the developer navigates to the Counter projection view
**Then** a DataGrid renders with auto-generated columns from the projection type
**And** the shell uses Fluent UI Blazor v5 with accent color #0097A7
**And** the zero-override strategy is followed (no custom CSS on Fluent UI components)
**And** the basic shell layout follows UX-DR14 (header, navigation sidebar, content area)

**Given** a cold machine with .NET 10 SDK installed
**When** the developer clones the repo and runs the Counter sample
**Then** time-to-first-render is <= 5 minutes (NFR82)

**Given** the project template strategy (UX-DR66)
**When** the shipped project template is finalized (post-W1)
**Then** the Counter sample is replaced with a Task Tracker sample domain demonstrating: list with status badges (To Do amber, Done green), inline action buttons (Complete), a command form (Create Task), lifecycle loop, and meaningful empty states with 3-5 seeded sample tasks
**And** the Counter remains as a minimal example in documentation only

**References:** FR13, FR38, FR62, NFR82, NFR83, UX-DR14 (basic), UX-DR23 (accent color), UX-DR57, UX-DR59, UX-DR66

---

### Story 1.7: CI Pipeline & Semantic Release

As a developer,
I want a CI pipeline with build gates and semantic versioning from conventional commits,
So that every merge is validated and releases are automated with lockstep package versioning.

**Acceptance Criteria:**

**Given** a pull request is opened
**When** the ci.yml workflow runs
**Then** Gate 1 passes: Contracts builds successfully targeting netstandard2.0 in isolation
**And** Gate 2 passes: full solution builds successfully (all projects)
**And** Gate 3 passes: SourceTools.Tests run and pass

**Given** the CI pipeline runs
**When** the inner loop (unit + component tests) completes
**Then** total execution time is < 5 minutes (NFR64)
**And** trim warnings fail the build (IsTrimmable="true" on all framework assemblies, NFR68)

**Given** CI gates are configured
**When** they are first introduced (Epic 1 scope)
**Then** gates run in advisory mode (report but do not block merges)
**And** a note documents that gates will be hardened to blocking in Epic 2

**Given** a conventional commit is merged to main
**When** the semantic-release pipeline runs
**Then** a version number is computed from commit messages (feat/fix/breaking)
**And** all framework packages receive the same version number (lockstep versioning, NFR75)
**And** the conventional commit-msg hook validates commit message format

**Given** the inner loop development experience
**When** a developer runs local unit + component tests
**Then** total execution time is < 5 minutes and is treated as a non-negotiable quality gate from day one (NFR64)
**And** if the inner loop exceeds 5 minutes at any point during Epic 1, it is treated as a blocking issue before new feature work
**And** test infrastructure (fixtures, harnesses, base classes) must be frictionless enough that skipping tests feels harder than running them

**References:** FR74, NFR64, NFR68, NFR70, NFR75, NFR99, Architecture W1 CI gates 1-3

---

### Story 1.8: Hot Reload & Fluent UI Contingency

As a developer,
I want domain attribute changes to trigger incremental source generator rebuilds with hot reload support, and a documented contingency plan for Fluent UI v5 GA migration,
So that my development inner loop is fast and I'm protected against upstream Fluent UI breaking changes.

**Acceptance Criteria:**

**Given** a running application with hot reload enabled
**When** a developer adds or modifies a [Projection] attribute on a domain type
**Then** the source generator incrementally rebuilds affected output
**And** the updated DataGrid reflects the change without full application restart
**And** hot reload latency is < 2 seconds (NFR10)

**Given** a domain attribute change triggers the incremental generator
**When** the rebuild completes
**Then** only the affected domain assembly is regenerated (not the full solution)
**And** rebuild time per domain assembly is < 500ms (NFR8)

**Given** Fluent UI Blazor v5 is pinned at an exact RC version
**When** a new RC or GA release is published upstream
**Then** the documented contingency plan covers: version pin update procedure, load-bearing APIs to validate (FluentLayout, DefaultValues, FluentDataGrid, FluentProviders), expected migration effort (1-2 weeks budget), and rollback procedure
**And** the canary build preparation (canary-fluentui.yml) is documented for implementation in Epic 3 (W2 scope)

**Given** a developer inspects generator output
**When** hot reload limitations apply (e.g., generic type changes, new attribute additions requiring full restart)
**Then** the limitation is documented and a build-time message indicates "Full restart required for this change type"

**References:** FR70, NFR8, NFR10, UX-DR61, Architecture hot reload limitations

---

**Epic 1 Summary:**
- 8 stories covering all 8 FRs (FR2, FR3, FR6, FR13, FR38, FR62, FR70, FR74)
- Relevant NFRs woven into acceptance criteria (NFR8, NFR10, NFR64, NFR68, NFR75, NFR82, NFR83, NFR93, NFR99)
- Relevant UX-DRs addressed (UX-DR14, UX-DR21, UX-DR23, UX-DR35, UX-DR56, UX-DR57, UX-DR59, UX-DR61, UX-DR65)
- Stories are sequentially completable: 1.1 (build infra) -> 1.2 (contracts) -> 1.3 (Fluxor) -> 1.4 (parse) -> 1.5 (transform/emit) -> 1.6 (sample) -> 1.7 (CI) -> 1.8 (hot reload)

---
