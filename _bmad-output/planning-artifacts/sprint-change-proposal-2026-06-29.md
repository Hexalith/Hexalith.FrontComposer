---
workflow: bmad-correct-course
date: 2026-06-29
mode: Batch
status: Implemented
change_trigger: Debug builds must use Hexalith project references; Release builds must use Hexalith NuGet packages
scope_classification: Moderate
approval: User directive in request, 2026-06-29
---

# Sprint Change Proposal: Configuration-Driven Hexalith Dependency Mode

## 1. Issue Summary

FrontComposer already had a manual `UseNuGetDeps` switch, but the repository did not enforce the
required build-mode contract:

- Debug/source builds must consume local root-declared `references/Hexalith.*` submodules as
  `ProjectReference`s so Hexalith libraries can be debugged.
- Release/package builds must consume published Hexalith NuGet packages so releases are reproducible
  and independent of checked-out submodule source.

Evidence:

- `Directory.Build.props` imported `deps.local.props` unless `UseNuGetDeps=true`; the build
  configuration did not drive dependency mode.
- `Directory.Build.props` referenced `deps.nuget.props`, but the file did not exist.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` directly referenced
  `$(EventStorePath)/src/Hexalith.EventStore.Aspire/...` with no package fallback.
- `Hexalith.FrontComposer.slnx` listed many `references/Hexalith.*` projects as normal solution
  projects, so Release solution builds could compile submodule source even when package mode was
  intended.
- CI and release workflows restored the solution without `Configuration=Release` before running
  Release `--no-restore` build/test steps.

## 2. Impact Analysis

Epic impact: no epic is added, removed, or resequenced. This is a cross-cutting build/release NFR
correction under NFR14.

Story impact: no numbered story text is changed. The change is small enough for direct Developer
implementation, with this proposal as the sprint decision record.

Artifact impact:

- Root MSBuild dependency switch: `Directory.Build.props`, `deps.local.props`, new tracked
  `deps.nuget.props`, plus `.gitignore` unignore for the root file.
- AppHost dependency declaration: conditional EventStore Aspire `ProjectReference` in source mode and
  `PackageReference` in package mode.
- AppHost code: remove the direct `Hexalith.Commons.Aspire` namespace dependency; the DAPR endpoint
  resolver is available through the EventStore Aspire source/package path.
- Central package management: `Hexalith.EventStore.Aspire` pinned to `3.20.0`, matching the checked-out
  EventStore tag and confirmed on nuget.org.
- Solution metadata: direct `references/Hexalith.*` solution project entries are disabled for
  `Release|*`.
- CI/release workflows: Release restore calls pass `-p:Configuration=Release`.
- Release evidence: `deps.nuget.props` is part of release-definition drift detection and fallback
  invalidation.
- Governance tests and BMAD docs pin the new contract.

Technical impact: Release builds now resolve the AppHost EventStore Aspire dependency through NuGet
unless `UseHexalithProjectReferences=true` is explicitly supplied. Debug keeps the local source path.
The legacy `UseNuGetDeps=true|false` inverse switch remains supported for existing scripts.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale: the issue is a build/release policy gap, not a product-scope change. A direct adjustment
keeps the existing dependency-mode intent, adds the missing NuGet path, and pins the behavior with
governance tests.

Effort: Medium. The implementation touches MSBuild, solution metadata, CI/release restore commands,
release evidence governance, docs, and tests.

Risk: Medium. Main risks are conditional restore/build drift and package-version mismatch. Mitigations:
configuration-aware Release restore, central package pin, Release solution exclusion for submodule
projects, and governance coverage.

## 4. Checklist Outcome

- [x] 1.1 Trigger identified: user directive to enforce Debug project references and Release NuGet
  package references for Hexalith libraries.
- [x] 1.2 Core problem defined: manual dependency mode allowed Release builds to keep consuming
  submodule source.
- [x] 1.3 Evidence gathered: missing `deps.nuget.props`, unconditional AppHost EventStore project
  reference, solution-level external project entries, Release restore without `Configuration=Release`.
- [x] 2.1-2.5 Epic impact assessed: no epic replan; NFR14 refined.
- [!] 3.1 PRD impact assessed: no authored PRD exists; `epics.md` remains the requirements inventory
  and was updated as the PRD proxy.
- [x] 3.2 Architecture impact assessed: external dependency section updated.
- [N/A] 3.3 UI/UX impact: no user-facing UI surface change.
- [x] 3.4 Secondary artifacts assessed: CI, release evidence, solution metadata, governance tests, and
  generated BMAD docs updated.
- [x] 4.1 Direct adjustment viable: yes.
- [N/A] 4.2 Rollback path: not useful.
- [N/A] 4.3 MVP review: no MVP scope impact.
- [x] 4.4 Path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal and handoff captured here.
- [x] 6.1-6.5 Final review and handoff: implementation routed to Developer agent in this turn.

## 5. Detailed Change Proposals

### Build Dependency Switch

OLD:

```xml
<Import Project="deps.local.props" Condition="'$(UseNuGetDeps)' != 'true' AND '$(FrontComposerRoot)' == 'true'" />
<Import Project="deps.nuget.props" Condition="'$(UseNuGetDeps)' == 'true' AND '$(FrontComposerRoot)' == 'true'" />
```

NEW:

```xml
<UseHexalithProjectReferences Condition="'$(UseHexalithProjectReferences)' == '' AND '$(Configuration)' == 'Debug'">true</UseHexalithProjectReferences>
<UseHexalithProjectReferences Condition="'$(UseHexalithProjectReferences)' == ''">false</UseHexalithProjectReferences>
<Import Project="deps.local.props" Condition="'$(UseHexalithProjectReferences)' == 'true' AND '$(FrontComposerRoot)' == 'true'" />
<Import Project="deps.nuget.props" Condition="'$(UseHexalithProjectReferences)' != 'true' AND '$(FrontComposerRoot)' == 'true'" />
```

Rationale: makes configuration the default selector while keeping explicit overrides.

### Missing NuGet Dependency Props

OLD:

```text
deps.nuget.props missing
*.nuget.props ignored globally
```

NEW:

```xml
<Project>
  <PropertyGroup>
    <HexalithEventStoreFromSource>false</HexalithEventStoreFromSource>
  </PropertyGroup>
</Project>
```

```gitignore
*.nuget.props
!deps.nuget.props
```

Rationale: the Release/package mode import now resolves to an owned file.

### AppHost EventStore Dependency

OLD:

```xml
<ProjectReference Include="$(EventStorePath)/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj" IsAspireProjectResource="false" />
```

NEW:

```xml
<ProjectReference Include="$(EventStorePath)/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj"
                  IsAspireProjectResource="false"
                  Condition="'$(HexalithEventStoreFromSource)' == 'true'" />
<PackageReference Include="Hexalith.EventStore.Aspire"
                  Condition="'$(HexalithEventStoreFromSource)' != 'true'" />
```

Rationale: Debug/source builds keep the root-declared submodule project reference; Release/package
builds use the published package.

### Central Package Version

OLD:

```text
No PackageVersion for Hexalith.EventStore.Aspire.
```

NEW:

```xml
<PackageVersion Include="Hexalith.EventStore.Aspire" Version="3.20.0" />
```

Rationale: central package management remains the only source of package versions.

### AppHost Commons Aspire Dependency

OLD:

```csharp
using Hexalith.Commons.Aspire;
using Hexalith.EventStore.Aspire;
```

NEW:

```csharp
using Hexalith.EventStore.Aspire;
```

Rationale: `Hexalith.Commons.Aspire` is not published as a NuGet package. The endpoint resolver used
by FrontComposer's AppHost is supplied by `Hexalith.EventStore.Aspire`, so removing the direct Commons
namespace dependency keeps Release package mode complete.

### Solution Release Build Behavior

OLD:

```xml
<Project Path="references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj" />
```

NEW:

```xml
<Project Path="references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj">
  <Build Solution="Release|*" Project="false" />
</Project>
```

Rationale: `references/Hexalith.*` solution entries remain available for source navigation and Debug
work, but Release solution builds cannot compile submodule source by accident.

### CI And Release Restore

OLD:

```sh
dotnet restore Hexalith.FrontComposer.slnx
dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore
```

NEW:

```sh
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release
dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore
```

Rationale: conditional PackageReference/ProjectReference assets must be restored under the same
configuration as the subsequent `--no-restore` Release step.

### Governance Coverage

NEW:

- `HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease`
- `ReleaseSolutionBuild_ExcludesExternalHexalithReferenceProjects`

Rationale: the policy should fail fast if a later edit restores Release project references or drops the
package path.

## 6. Implementation Handoff

Scope classification: Moderate.

Route: Developer agent direct implementation.

Responsibilities:

- Apply configuration-driven dependency mode.
- Add the missing NuGet props file and package pin.
- Disable external Hexalith solution projects in Release.
- Update CI/release restore commands and release evidence drift surface.
- Update governance tests and current BMAD docs.
- Validate Debug source restore and Release package restore/build behavior.

Success criteria:

- Debug restore/build evaluates `HexalithEventStoreFromSource=true` and uses the EventStore Aspire
  project reference.
- Release restore/build evaluates `HexalithEventStoreFromSource=false` and uses
  `Hexalith.EventStore.Aspire` from NuGet.
- `Hexalith.FrontComposer.slnx` does not build direct `references/Hexalith.*` project entries for
  `Release|*`.
- Governance tests pass.
- Release solution build reaches FrontComposer-owned project compilation without building submodule
  source.
