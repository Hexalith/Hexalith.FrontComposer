---
title: 'Centralize FrontComposer package versions in Hexalith.Builds'
type: 'refactor'
created: '2026-07-17'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'd739f9b5249017b8959e14a445bc3eaaebf683c0'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer defines 14 NuGet dependency versions after importing the shared `Hexalith.Builds` catalog, so package-version authority is split and release/cache controls do not uniformly track the real catalog.

**Approach:** Move those 14 declarations unchanged into `references/Hexalith.Builds/Props/Directory.Packages.props`, leave the root file as the central-package-management import shim, and reconcile only the sibling declarations that would otherwise duplicate the new shared entries.

## Boundaries & Constraints

**Always:** Preserve every FrontComposer package ID and exact effective version; retain root `ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled`, path fallbacks, and guarded imports; preserve sibling-specific effective versions with `Update` where they differ; keep the shared catalog UTF-8-with-BOM and CRLF; preserve all unrelated dirty-tree and submodule work.

**Ask First:** Any package upgrade/downgrade, removal of a pin because it appears unused, new conditional version policy, or change beyond the listed FrontComposer migration and collision reconciliation.

**Never:** Move MSBuild SDK, .NET SDK, npm, dotnet-tool, product `PackageVersion`, or API compatibility baseline versions into NuGet CPM; alter package references; centralize the remaining submodule-owned catalogs; weaken validation or fallback-approval semantics.

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` -- FrontComposer CPM/import shim; currently owns the 14 declarations to remove.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative shared NuGet catalog receiving the unchanged declarations.
- `references/Hexalith.EventStore/Directory.Packages.props` -- preserves TimeProvider `10.7.0` as an imported-item override.
- `references/Hexalith.Memories/Directory.Packages.props` -- drops two same-version declarations now supplied centrally.
- `references/Hexalith.Parties/Directory.Packages.props` -- preserves MCP ASP.NET Core `1.4.0` as an imported-item override.
- `.github/workflows/quality.yml` -- NuGet cache key must hash both import shim and authoritative catalog.
- `eng/release_evidence.py` -- release-definition drift surface must include the shared catalog without adding it to fallback invalidation.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/{InfrastructureGovernanceTests,CiGovernanceTests}.cs` -- enforce no root-local package versions and the expanded release-definition surface.
- `tests/ci-governance/fixtures/{release-manifest-valid,release-readiness-cases}.json` -- fixture fingerprints follow the authoritative files.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Builds/Props/Directory.Packages.props` -- add the 14 exact literal pins beside their package families without reordering unrelated entries.
- [ ] `Directory.Packages.props` -- remove all local `PackageVersion` item groups while retaining CPM, transitive pinning, and imports.
- [ ] `references/Hexalith.{EventStore,Memories,Parties}/Directory.Packages.props` -- convert differing collisions to `Update` and remove same-version collisions, preserving effective versions.
- [ ] `.github/workflows/quality.yml` and `eng/release_evidence.py` -- hash/fingerprint the shared catalog; keep it outside `FALLBACK_INVALIDATION_FILES`.
- [ ] Governance tests and release fixtures in the Code Map -- enforce the new ownership boundary and update complete release-definition baselines.

**Acceptance Criteria:**
- Given the pre-change FrontComposer catalog, when MSBuild evaluates packages after migration, then the same 14 ID/version pairs occur exactly once from the shared catalog and the root shim contains zero `PackageVersion` items.
- Given sibling source restores, when the shared catalog is imported, then EventStore still resolves TimeProvider `10.7.0`, Parties still resolves MCP ASP.NET Core `1.4.0`, Memories inherits `1.4.1` and `10.8.0`, and no `NU1506`/`NU1109` occurs.
- Given a shared-catalog edit, when cache keys and release evidence are evaluated, then NuGet cache invalidation and release-definition drift detect it while fallback approvals remain unaffected by routine package-version changes.
- Given CPM-ineligible version mechanisms, when the migration completes, then AppHost SDK, .NET SDK, npm/tool, product-version, and package-validation baseline pins are unchanged.

## Spec Change Log

## Design Notes

The moved pins are `BenchmarkDotNet 0.15.8`, `FsCheck.Xunit.v3 3.3.3`, `Microsoft.CodeAnalysis.Workspaces.Common 5.6.0`, `Microsoft.Extensions.Localization 10.0.9`, `Microsoft.Extensions.TimeProvider.Testing 10.8.0`, `ModelContextProtocol.AspNetCore 1.4.1`, `NUlid 1.7.3`, `PactNet 5.0.1`, `System.Collections.Immutable 10.0.10`, `System.ComponentModel.Annotations 5.0.0`, `System.Reactive 7.0.0-rc.1`, `System.Threading.Tasks.Extensions 4.6.3`, `Verify 31.24.2`, and `Verify.XunitV3 31.24.2`.

## Verification

**Commands:**
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` -- shared catalog evaluates with valid NuGet versions.
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- validator regression suite passes.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Debug -m:1 && dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -m:1` -- both dependency modes restore without central-version collisions.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -warnaserror -m:1` -- serialized Release build passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category=Governance"` -- affected governance lane passes.
