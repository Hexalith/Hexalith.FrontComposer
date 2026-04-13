# Story 1.1: Solution Structure & Build Infrastructure

Status: done

## Story

As a developer,
I want a correctly structured .NET 10 solution with MSBuild spine, central package management, and submodule isolation,
So that all subsequent framework packages build cleanly from a single `dotnet restore && dotnet build`.

## Acceptance Criteria

1. **Given** the repository is cloned with submodules initialized
   **When** `dotnet restore` is run from the repository root
   **Then** restore completes successfully with zero warnings
   **And** Directory.Build.props contains `FrontComposerRoot` property guard preventing walk-up import from EventStore/Tenants submodules
   **And** submodule projects are NOT included directly in `Hexalith.FrontComposer.sln` — they are consumed only via ProjectReference through `deps.local.props`
   **And** the `FrontComposerRoot` condition guard prevents FrontComposer-specific imports from applying when submodule projects are built via ProjectReference

2. **Given** the solution file exists
   **When** the solution structure is inspected
   **Then** solution folders `src/`, `samples/`, `tests/` exist
   **And** Directory.Packages.props pins all required package versions (see "Package Version Pins" table in Dev Notes)
   **And** global.json pins .NET SDK 10.0.103
   **And** .editorconfig and nuget.config are present

3. **Given** the `deps.local.props` and `deps.nuget.props` files exist
   **When** `UseNuGetDeps` is set to `false` (default) in Directory.Build.props
   **Then** `deps.local.props` is imported and `EventStorePath` resolves to the root `Hexalith.EventStore` submodule
   **When** `UseNuGetDeps` is set to `true`
   **Then** `deps.nuget.props` is imported for NuGet package references

4. **Given** the `Hexalith.EventStore` submodule is present
   **When** `dotnet build` targets the submodule projects in isolation
   **Then** the submodule compiles without errors, confirming integration stability

## Tasks / Subtasks

- [x] Task 1: Create solution file and folder structure (AC: #2)
  - [x] 1.1 Create `Hexalith.FrontComposer.sln` with solution folders `src/`, `samples/`, `tests/`
  - [x] 1.2 Create directory structure: `src/`, `samples/Counter/`, `tests/`
  - [x] 1.3 Create placeholder `.csproj` files for all 6 W1 projects (empty, compilable)
- [x] Task 2: Create MSBuild infrastructure files (AC: #1, #2, #3)
  - [x] 2.1 Create `global.json` pinning .NET SDK 10.0.103
  - [x] 2.2 Create `Directory.Build.props` with `FrontComposerRoot` guard and deps import switch
  - [x] 2.3 Create `Directory.Packages.props` with central package management and all version pins
  - [x] 2.4 Create `deps.local.props` with EventStore/Tenants submodule ProjectReference paths
  - [x] 2.5 Create `deps.nuget.props` with NuGet PackageReference equivalents — this file MUST exist even though NuGet packages aren't published yet (MSBuild will crash with "imported project not found" if `UseNuGetDeps=true` and the file is missing). Use placeholder PackageReference entries with `TODO` version comments.
  - [x] 2.6 Create `.editorconfig` matching EventStore conventions
  - [x] 2.7 Create `nuget.config` with package sources
  - [x] 2.8 Create `.gitattributes` with `* text=auto eol=crlf` for cross-platform CI consistency (no `.gitattributes` exists yet)
- [x] Task 3: Create project files with correct references and targets (AC: #1, #2)
  - [x] 3.1 `Hexalith.FrontComposer.Contracts.csproj` — multi-target `net10.0;netstandard2.0`, dependency-free
  - [x] 3.2 `Hexalith.FrontComposer.SourceTools.csproj` — `netstandard2.0`, `IsRoslynComponent=true`, refs Contracts + CodeAnalysis
  - [x] 3.3 `Hexalith.FrontComposer.Shell.csproj` — `net10.0`, `Sdk="Microsoft.NET.Sdk.Razor"`, refs Contracts + Fluent UI + Fluxor. Create a minimal `_Imports.razor` file to prevent Razor SDK warnings on empty project (see Shell Placeholder Pattern below)
  - [x] 3.4 `Counter.Domain.csproj` — `net10.0`, refs Contracts + SourceTools (analyzer)
  - [x] 3.5 `Counter.Web.csproj` — Blazor Auto, refs Shell + Counter.Domain
  - [x] 3.6 `Hexalith.FrontComposer.SourceTools.Tests.csproj` — `net10.0`, refs SourceTools + xUnit + bUnit
- [x] Task 4: Verify environment and submodule isolation (AC: #1, #4)
  - [x] 4.0 Verify `dotnet --version` reports 10.0.103 or later — if not, upgrade .NET SDK before proceeding
  - [x] 4.1 Verify `dotnet restore` completes with zero warnings from repo root (including EventStore transitive package dependencies resolving correctly)
  - [x] 4.2 Verify submodule builds in isolation without walk-up import errors
  - [x] 4.3 Verify `deps.local.props` correctly resolves EventStore path
  - [x] 4.4 Verify `Hexalith.Tenants/Hexalith.EventStore/` is NOT referenced in `deps.local.props` — only root EventStore path is used
- [x] Task 5: Verify `dotnet build` and quality gates from repo root (AC: #1, #2, #4)
  - [x] 5.1 Run `dotnet restore && dotnet build` — zero errors, zero compiler warnings (`TreatWarningsAsErrors=true` enforced)
  - [x] 5.2 Verify switching `UseNuGetDeps=true` changes import behavior (may not build without published packages, but import path must switch)
  - [x] 5.3 Verify SourceTools project compiles with `EnforceExtendedAnalyzerRules=true` — zero analyzer rule violations
  - [x] 5.4 Verify package boundary enforcement: Shell does NOT have a direct ProjectReference to SourceTools (only Counter.Domain has it as analyzer ref)
  - [x] 5.5 Verify empty SourceTools.Tests project compiles and `dotnet test` runs successfully with zero tests (no test runner errors)
  - [x] 5.6 Run `dotnet test` on EventStore submodule (e.g., `dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/`) to confirm xUnit v3 coexistence — zero version conflicts

### Review Findings

- [x] [Review][Patch] `FrontComposerRoot` guard does not enforce submodule import isolation [Directory.Build.props:6]

## Dev Notes

### Critical Architecture Decisions

**ADR-002 (Submodule vs NuGet):** Two-file import pattern. `deps.local.props` = submodule ProjectReferences (default for dev + CI). `deps.nuget.props` = NuGet PackageReferences (release validation only). A single `UseNuGetDeps` boolean in `Directory.Build.props` selects which file. No scattered MSBuild conditionals.

**ADR-005 (Progressive Project Structure):** Exactly 6 `.csproj` files at W1:
- 3 source: Contracts, SourceTools, Shell
- 2 sample: Counter.Domain, Counter.Web
- 1 test: SourceTools.Tests

Do NOT create more projects. Do NOT create Counter.AppHost yet (that is W2).

**ADR-006 (Incremental Directory Structure):** W1 creates only `src/`, `samples/`, `tests/`. Do NOT create `benchmarks/`, `docs/`, `scripts/`, or `build/` directories yet.

### MSBuild Walk-Up Isolation (CRITICAL)

Both EventStore and Tenants submodules have their own `Directory.Build.props`. MSBuild walks UP the tree. FrontComposer's root `Directory.Build.props` MUST guard against double-import:

```xml
<!-- Directory.Build.props — top of file -->
<PropertyGroup>
  <FrontComposerRoot>true</FrontComposerRoot>
</PropertyGroup>

<!-- Only import deps switch if we're the root, not imported from submodule -->
<Import Project="deps.local.props" Condition="'$(UseNuGetDeps)' != 'true' AND '$(FrontComposerRoot)' == 'true'" />
<Import Project="deps.nuget.props" Condition="'$(UseNuGetDeps)' == 'true' AND '$(FrontComposerRoot)' == 'true'" />
```

**Submodule isolation strategy:** Submodule projects are NOT added to `Hexalith.FrontComposer.sln`. They are consumed only via ProjectReference (through `deps.local.props`). When MSBuild resolves a ProjectReference, the referenced project evaluates in its own directory and finds its own `Directory.Build.props` first — walk-up stops there. The `FrontComposerRoot` guard is a belt-and-suspenders measure. You do NOT need to modify files inside the `Hexalith.EventStore/` or `Hexalith.Tenants/` submodule directories — those are external repos. Verified: EventStore's `Directory.Build.props` does not walk up further (no `GetPathOfFileAbove` call).

### Central Package Management Isolation (IMPORTANT)

Both FrontComposer and EventStore enable `ManagePackageVersionsCentrally=true` with separate `Directory.Packages.props` files. This is correct and intentional — MSBuild's CPM feature searches for `Directory.Packages.props` bottom-up, stopping at the first match. When EventStore projects are built via ProjectReference, they find EventStore's `Directory.Packages.props` first and use those pins. FrontComposer projects use FrontComposer's pins. Do NOT attempt to share or merge these files — each submodule's CPM is isolated by design.

### Nested Submodule Path Resolution (CRITICAL)

Tenants contains EventStore as its own submodule (`Hexalith.Tenants/Hexalith.EventStore/`). FrontComposer also has EventStore directly (`Hexalith.EventStore/`). `deps.local.props` MUST specify the authoritative path:

```xml
<!-- deps.local.props — EventStore is ALWAYS from root submodule, never Tenants' nested copy -->
<PropertyGroup>
  <EventStorePath>$(MSBuildThisFileDirectory)Hexalith.EventStore</EventStorePath>
  <TenantsPath>$(MSBuildThisFileDirectory)Hexalith.Tenants</TenantsPath>
</PropertyGroup>
```

**How projects consume these paths:** Individual `.csproj` files use the properties in their ProjectReferences. Example (for future use in Shell when it needs EventStore):
```xml
<ProjectReference Include="$(EventStorePath)/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj" />
```
At W1, no project references EventStore yet — these paths are scaffolded for Story 1.6+ when Counter.Web connects to EventStore.

### Package Version Pins (Directory.Packages.props)

**Version Discrepancy Alert — xUnit v3 Override (Validated Exception to Epic Requirement):** The epics file explicitly states "xUnit 2.9.3 (NOT v3)" due to bUnit 2.7.2 compatibility concerns. However, this restriction is outdated: the EventStore submodule (in this same repo) ALREADY uses xUnit v3 3.2.2 with bUnit 2.7.2 successfully. bUnit 2.7.2 was updated 3/31/2026 and supports xUnit v3. **Use xUnit v3 3.2.2 to match the EventStore submodule** — this ensures consistent test runner behavior across the monorepo. Using v2 would create a split where FrontComposer tests use a different runner than EventStore tests.

**Version Discrepancy Alert:** The architecture doc says "global.json pins .NET SDK 10.0.5". The EventStore submodule pins SDK 10.0.103. The "10.0.5" in the architecture likely refers to the .NET runtime version, not the SDK version. **Pin SDK to 10.0.103** to match the EventStore submodule. Use `"rollForward": "latestPatch"`.

**global.json Authority:** When `dotnet build` runs from FrontComposer root, the root `global.json` is authoritative — submodule `global.json` files are ignored in that context. When building a submodule in isolation (`dotnet build Hexalith.EventStore/Hexalith.EventStore.slnx`), the submodule's own `global.json` applies. Both currently pin 10.0.103 so there is no conflict, but if they diverge in the future, the root pin wins for FrontComposer builds.

Required pins in `Directory.Packages.props`:

| Package | Version | Notes |
|---|---|---|
| `Microsoft.CodeAnalysis.CSharp` | 4.12.0 | SourceTools — `PrivateAssets="all"`. **MUST be exactly 4.12.0, do NOT upgrade to 5.x.** Version 5.3.0 breaks IDE analyzer load context (VS/Rider host CodeAnalysis at a different version). Architecture says `>=4.12.0` but higher versions cause analyzer loading failures in consumer projects. |
| `Fluxor.Blazor.Web` | 6.9.0 | Shell — confirmed latest stable |
| `Microsoft.FluentUI.AspNetCore.Components` | Exact v5 RC pin | Shell — ADR-003. Check NuGet dev feed for latest v5 preview. If v5 not available on public feed, use v4.14.0 and document v5 migration plan. |
| `xunit.v3` | 3.2.2 | Tests — matches EventStore, bUnit 2.7.2 compatible |
| `xunit.v3.assert` | 3.2.2 | Tests — assertion library |
| `xunit.runner.visualstudio` | 3.1.5 | Tests — VS test runner |
| `bunit` | 2.7.2 | Tests — latest stable |
| `Shouldly` | 4.3.0 | Tests — matches EventStore submodule for monorepo assertion consistency. Architecture specifies FluentAssertions but Shouldly preferred to avoid split assertion syntax across the monorepo. |
| `coverlet.collector` | latest stable | Tests — code coverage |
| `Microsoft.NET.Test.Sdk` | latest stable | Tests — test SDK |
| `NSubstitute` | 5.3.0 | Tests — mocking, matches EventStore |

### Project Reference Patterns

**Contracts** (dependency-free, multi-target):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```
*Why multi-target?* SourceTools targets `netstandard2.0` (required for Roslyn analyzers) and references Contracts. If Contracts only targeted `net10.0`, SourceTools couldn't reference it. Multi-targeting bridges the gap.

**SourceTools** (netstandard2.0, Roslyn generator):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
    <!-- PrivateAssets="all" prevents CodeAnalysis from leaking as a transitive dependency
         to consumers. Without it, every project referencing SourceTools would need CodeAnalysis. -->
  </ItemGroup>
</Project>
```

**Shell** (net10.0, Blazor components):
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj" />
    <PackageReference Include="Fluxor.Blazor.Web" />
    <PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" />
  </ItemGroup>
</Project>
```

**Shell Placeholder Pattern:** `Microsoft.NET.Sdk.Razor` without any `.razor` files may emit compiler warnings, violating `TreatWarningsAsErrors=true`. Create a minimal `_Imports.razor` in the Shell project root:
```razor
@* Global imports — populated in Story 1.3 *@
```
This satisfies the Razor SDK's expectations. Do NOT create any component `.razor` files — those come in Story 1.3+.

**Counter.Domain** (consumer pattern — hardcoded analyzer ref for W1):
```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj" />
  <ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    SetTargetFramework="TargetFramework=netstandard2.0" />
  <!-- OutputItemType="Analyzer": treats SourceTools as a Roslyn analyzer, not a library reference.
       ReferenceOutputAssembly="false": don't add SourceTools.dll to Counter.Domain's references.
       SetTargetFramework: forces MSBuild to use the netstandard2.0 build of SourceTools,
       which is required for analyzer host compatibility. Without it, MSBuild may pick the wrong TFM. -->
</ItemGroup>
```

**SourceTools.Tests** (normal ref to SourceTools, NOT analyzer ref):
```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj" />
  <ProjectReference Include="..\..\src\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="xunit.v3.assert" />
  <PackageReference Include="xunit.runner.visualstudio" />
  <PackageReference Include="bunit" />
  <PackageReference Include="Shouldly" />
  <PackageReference Include="NSubstitute" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="coverlet.collector" />
</ItemGroup>
```

### Package Boundary Enforcement

| Package | May Reference | Must NOT Reference |
|---|---|---|
| **Contracts** | Nothing (dependency-free) | Any other FrontComposer package |
| **SourceTools** | Contracts, Microsoft.CodeAnalysis (PrivateAssets=all) | Shell, Fluxor, Fluent UI |
| **Shell** | Contracts, Fluent UI v5, Fluxor | SourceTools (analyzer-only) |

### Code Style (.editorconfig)

Copy the EventStore `.editorconfig` as the baseline. Key rules:
- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- Private fields: `_camelCase`
- Interfaces: `I` prefix
- Async methods: `Async` suffix
- 4 spaces indent, CRLF, UTF-8
- `TreatWarningsAsErrors=true`

### Namespace Hierarchy

```
Hexalith.FrontComposer.Contracts
Hexalith.FrontComposer.Contracts.Rendering
Hexalith.FrontComposer.Contracts.Lifecycle
Hexalith.FrontComposer.Contracts.Registration
Hexalith.FrontComposer.Contracts.Storage
Hexalith.FrontComposer.Contracts.Communication
Hexalith.FrontComposer.SourceTools.Parsing
Hexalith.FrontComposer.SourceTools.Transforms
Hexalith.FrontComposer.SourceTools.Emitters
Hexalith.FrontComposer.SourceTools.Diagnostics
Hexalith.FrontComposer.Shell.Components
Hexalith.FrontComposer.Shell.State
Hexalith.FrontComposer.Shell.Services
Hexalith.FrontComposer.Shell.Infrastructure
```

Rule: Namespace matches folder path exactly. One type per file (except up to 5 related Fluxor action records may share a file). Story 1.2 should create folders matching these namespaces inside Contracts (e.g., `Attributes/`, `Rendering/`, `Lifecycle/`, `Registration/`, `Storage/`, `Communication/`).

### Directory Structure to Create

```
Hexalith.FrontComposer/
├── src/
│   ├── Hexalith.FrontComposer.Contracts/
│   │   └── Hexalith.FrontComposer.Contracts.csproj
│   ├── Hexalith.FrontComposer.SourceTools/
│   │   └── Hexalith.FrontComposer.SourceTools.csproj
│   └── Hexalith.FrontComposer.Shell/
│       ├── Hexalith.FrontComposer.Shell.csproj
│       └── _Imports.razor
├── samples/
│   └── Counter/
│       ├── Counter.Domain/
│       │   └── Counter.Domain.csproj
│       └── Counter.Web/
│           └── Counter.Web.csproj
├── tests/
│   └── Hexalith.FrontComposer.SourceTools.Tests/
│       └── Hexalith.FrontComposer.SourceTools.Tests.csproj
├── deps.local.props
├── deps.nuget.props
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── .editorconfig
├── .gitattributes
└── Hexalith.FrontComposer.sln
```

**Solution folders vs directories:** A directory is a real folder on disk (`src/`, `samples/`, `tests/`). A solution folder is a virtual grouping inside the `.sln` file that appears in VS/Rider Solution Explorer. Both must be created — `dotnet sln` commands create solution folders, filesystem commands create directories. Keep them in sync.

### Deferred Items (Explicitly Out of Scope)

- **ServiceLifetimeMatrix** — The architecture identifies DI scope divergence around Blazor Auto as a cross-cutting concern requiring a ServiceLifetimeMatrix. This is deferred to Story 1.3 (Fluxor State Management) where DI registration patterns are first exercised. Do NOT design or create it in this story.
- **CI pipeline (ci.yml)** — Story 1.7 scope
- **Counter.AppHost (Aspire)** — W2 scope

### Build Quality Gate

`TreatWarningsAsErrors=true` MUST be set in `Directory.Build.props` (matching EventStore convention). This means `dotnet build` must produce **zero compiler warnings** — any warning is a build failure. This is the foundational quality gate for all subsequent stories.

### What NOT To Do

- Do NOT create Counter.AppHost — that is W2 scope
- Do NOT create `build/`, `benchmarks/`, `docs/`, `scripts/` directories — those come later per ADR-006
- Do NOT add Fluxor state features (ThemeState, DensityState) — that is Story 1.3
- Do NOT add attribute classes (CommandAttribute, ProjectionAttribute) — that is Story 1.2
- Do NOT add any source generator code — that is Story 1.4
- Do NOT create Shell component files (razor/razor.cs) — that is Stories 1.3+
- Do NOT reference DAPR SDK or EventStore packages directly — Shell doesn't need them at W1
- Do NOT create CI pipeline (ci.yml) — that is Story 1.7
- Do NOT create more than 6 `.csproj` files
- Do NOT use `.slnx` format — EventStore uses it but the architecture doc specifies `.sln` for FrontComposer
- Do NOT add `IsTrimmable=true` to project files yet — that is validated in Story 1.7
- Do NOT add NuGet packaging metadata (Authors, Company, etc.) to FrontComposer projects yet — focus on build infra only

### Fluent UI v5 RC Strategy

ADR-003 says to build on v5 RC and pin exact version. However, as of April 2026, v5 is still in preview/daily builds (not on public NuGet feed as stable). The EventStore submodule uses v4.14.0.

**Decision path for the dev agent (you have authority to decide at implementation time):**
1. Check if `Microsoft.FluentUI.AspNetCore.Components` v5 preview is available on public NuGet (`nuget.org`). Search for versions matching `5.*`. If yes, pin exact preview version and document the chosen version in a comment in `Directory.Packages.props`.
2. If v5 is only on the dev feed, add the Fluent UI daily builds feed to `nuget.config`. Check the `fluentui-blazor` repo's `nuget.config` on the `dev` branch for the current feed URL (historically `https://pkgs.dev.azure.com/nicogladev/nicogladev/_packaging/nicogladev/nuget/v3/index.json`). Pin the latest v5 daily build version.
3. If v5 packages cannot be resolved at all, fall back to v4.14.0 (matching EventStore submodule) and add a `TODO: Upgrade to Fluent UI v5 when RC available on NuGet` comment in `Directory.Packages.props`. This is an acceptable temporary state — Story 1.6 (Counter sample) is where Fluent UI rendering is first exercised.

**Document your choice:** Whichever path you take, add a comment in `Directory.Packages.props` explaining which path was taken and why, so the next story's dev agent understands the current state.

### Definition of Done (Verification Checklist)

**Story is complete when ALL items below pass AND all tasks are marked complete.**

After implementation, verify:
- [ ] `dotnet --version` reports 10.0.103 or later
- [ ] `dotnet restore` from repo root = zero warnings (including EventStore transitive deps resolving)
- [ ] `dotnet build` from repo root = zero errors AND zero compiler warnings (`TreatWarningsAsErrors=true`)
- [ ] Solution opens in VS 2026 / Rider with correct folder structure
- [ ] `dotnet build Hexalith.EventStore/Hexalith.EventStore.slnx` still works (submodule isolation — FrontComposer build does not break EventStore)
- [ ] `dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/` passes (xUnit v3 coexistence)
- [ ] Switching `UseNuGetDeps=true` changes import path (even if NuGet restore fails due to unpublished packages)
- [ ] All 6 projects listed in .sln under correct solution folders
- [ ] No MSBuild walk-up warnings from submodule context
- [ ] SourceTools project compiles with `EnforceExtendedAnalyzerRules=true` — zero analyzer violations
- [ ] Empty SourceTools.Tests project: `dotnet test` runs successfully with zero tests, no runner errors
- [ ] Package boundaries correct: Shell has no direct reference to SourceTools; Counter.Domain references SourceTools as analyzer only
- [ ] Fluent UI version decision documented in `Directory.Packages.props` comment
- [ ] `.gitattributes` exists with `* text=auto eol=crlf` for cross-platform line ending consistency

### Project Structure Notes

- The solution uses the traditional `.sln` format (not `.slnx`) per architecture specification
- `.gitmodules` already exists with EventStore and Tenants submodules configured
- `.gitignore` already exists at repo root
- `LICENSE` and `README.md` already exist at repo root
- The existing EventStore submodule CLAUDE.md indicates it uses `.slnx` — FrontComposer's `.sln` must not conflict

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 1, Story 1.1]
- [Source: _bmad-output/planning-artifacts/architecture.md — ADR-001 through ADR-007]
- [Source: _bmad-output/planning-artifacts/architecture.md — MSBuild Constraints section]
- [Source: _bmad-output/planning-artifacts/architecture.md — v0.1 Solution Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md — Package Dependency Graph]
- [Source: _bmad-output/planning-artifacts/architecture.md — Naming Conventions]
- [Source: _bmad-output/planning-artifacts/prd.md — Solution Structure & Package Family]
- [Source: _bmad-output/planning-artifacts/prd.md — Language & Runtime Matrix]
- [Source: _bmad-output/planning-artifacts/prd.md — Build Infrastructure & CI/CD Requirements]
- [Source: Hexalith.EventStore/.editorconfig — Code style baseline]
- [Source: Hexalith.EventStore/Directory.Packages.props — Package version reference]
- [Source: Hexalith.EventStore/global.json — SDK version reference]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- .NET SDK 10.0.104 resolved via `rollForward: latestPatch` from global.json pin 10.0.103
- .NET 10 `dotnet new sln` defaults to `.slnx` format; used `--format sln` flag to create classic `.sln`
- Added `LangVersion=latest` to Directory.Build.props to fix CS8630 error (nullable not supported in C# 7.3 for netstandard2.0 targets)
- Counter.Web required minimal `Program.cs` (Web SDK mandates entry point)
- SourceTools Debug build sometimes locked by IDE Roslyn analyzer host (transient); Release build always succeeds cleanly
- Fluent UI v5 RC2 (5.0.0-rc.2-26098.1) found on public NuGet — Path 1 taken per story spec
- Deep recursive submodule init hits Windows path length limits on FrontShell nesting; immediate submodules initialized successfully

### Completion Notes List

- Solution structure created with 6 projects across src/, samples/, tests/ solution folders
- MSBuild infrastructure: global.json, Directory.Build.props (FrontComposerRoot guard + deps switch), Directory.Packages.props (CPM), deps.local.props, deps.nuget.props, .editorconfig, nuget.config, .gitattributes
- Fluent UI v5 RC2 (5.0.0-rc.2-26098.1) pinned from public NuGet feed (Path 1)
- xUnit v3 3.2.2 used to match EventStore submodule — confirmed coexistence (271 EventStore tests pass)
- All acceptance criteria verified: restore zero warnings, build zero errors/warnings, submodule isolation, UseNuGetDeps switch, package boundaries, empty test runner

### File List

- Hexalith.FrontComposer.sln (new)
- global.json (new)
- Directory.Build.props (new)
- Directory.Packages.props (new)
- deps.local.props (new)
- deps.nuget.props (new)
- .editorconfig (new)
- nuget.config (new)
- .gitattributes (new)
- src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj (new)
- src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj (new)
- src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj (new)
- src/Hexalith.FrontComposer.Shell/_Imports.razor (new)
- samples/Counter/Counter.Domain/Counter.Domain.csproj (new)
- samples/Counter/Counter.Web/Counter.Web.csproj (new)
- samples/Counter/Counter.Web/Program.cs (new)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj (new)

### Change Log

- 2026-04-13: Story 1.1 implemented — solution structure, MSBuild infrastructure, 6 project files, all verification gates passed
