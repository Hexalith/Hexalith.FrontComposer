---
title: 'Centralize FrontComposer package versions in Hexalith.Builds'
type: 'refactor'
created: '2026-07-18'
status: 'in-progress'
review_loop_iteration: 1
baseline_commit: '06b39738d95429b33ed85f3fcf1c9a1dfc2fbe14'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer defines 14 NuGet dependency versions after importing the shared `Hexalith.Builds` catalog, so package-version authority is split and release/cache controls do not uniformly track the real catalog. Memories and Parties also need standalone-safe access to the migrated pins rather than relying on FrontComposer's sibling fallback.

**Approach:** Move those 14 declarations unchanged into `references/Hexalith.Builds/Props/Directory.Packages.props`, leave the root file as the central-package-management import shim, reconcile sibling collisions, and advance the Memories and Parties nested Builds gitlinks to the catalog commit containing the pins.

## Boundaries & Constraints

**Always:** Preserve every FrontComposer package ID and exact effective version; retain root `ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled`, path fallbacks, and guarded imports; preserve sibling-specific effective versions with `Update` where they differ; set the Memories and Parties nested `Hexalith.Builds` gitlinks to `cfafcbf1e904138b435b63ba4fd79f86b8dda069`; keep the shared catalog UTF-8-with-BOM and CRLF; preserve all unrelated dirty-tree and submodule work. The selected gitlink update explicitly accepts Memories `Hexalith.Tenants` `3.2.13` to `3.2.15` and Parties `Hexalith.Commons` `2.28.1` to `2.28.2`, `Hexalith.PolymorphicSerializations` `1.16.3` to `1.16.5`, `Hexalith.EventStore` `3.68.1` to `3.70.0`, and `Hexalith.Tenants` `3.2.11` to `3.2.15`.

**Ask First:** Any package upgrade/downgrade beyond the explicitly accepted gitlink effects, removal of a pin because it appears unused, new conditional version policy, or change beyond the listed migration, collision reconciliation, gitlink updates, and regression coverage.

**Never:** Move MSBuild SDK, .NET SDK, npm, dotnet-tool, product `PackageVersion`, or API compatibility baseline versions into NuGet CPM; alter package references; add conditional local compatibility pins; centralize the remaining submodule-owned catalogs; weaken validation or fallback-approval semantics.

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` and `references/Hexalith.Builds/Props/Directory.Packages.props` -- FrontComposer import shim and authoritative shared NuGet catalog.
- `references/Hexalith.{EventStore,Memories,Parties}/Directory.Packages.props` -- preserve differing overrides and remove same-version collisions.
- `references/Hexalith.{Memories,Parties}/references/Hexalith.Builds` -- nested gitlinks advance to the catalog commit containing the migrated pins for standalone restores.
- `.github/workflows/quality.yml`, `eng/release_evidence.py`, and `tests/ci-governance/fixtures/*.json` -- cache and release-definition fingerprints.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/{InfrastructureGovernanceTests,CiGovernanceTests}.cs` and `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- ownership, policy, release, and identifier governance.

## Tasks & Acceptance

**Execution:**
- [x] Catalog migration and collision reconciliation -- move the 14 exact pins centrally, retain the root shim, and preserve sibling effective versions.
- [x] Cache, release-evidence, fixture, ownership, and identifier baselines -- track the authoritative shared catalog without changing fallback invalidation.
- [ ] `references/Hexalith.{Memories,Parties}/references/Hexalith.Builds` -- advance both nested gitlinks to `cfafcbf1e904138b435b63ba4fd79f86b8dda069` so standalone checkouts import the centralized pins.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- harden ownership matching for case-insensitive `Include`/`Update`, retain forbidden-provider checks for the migrated set, and assert sibling overrides plus nested catalog compatibility.

**Acceptance Criteria:**
- Given the pre-change FrontComposer catalog, when MSBuild evaluates packages after migration, then the same 14 ID/version pairs occur exactly once from the shared catalog and the root shim contains zero `PackageVersion` items.
- Given sibling source restores, when the shared catalog is imported, then EventStore still resolves TimeProvider `10.7.0`, Parties still resolves MCP ASP.NET Core `1.4.0`, Memories inherits `1.4.1` and `10.8.0`, and no `NU1506`/`NU1109` occurs.
- Given standalone Memories and Parties checkouts with their declared nested submodules, when restore evaluates CPM, then the nested Builds catalog contains the migrated pins and restore succeeds without `NU1010`.
- Given a shared-catalog edit, when cache keys and release evidence are evaluated, then NuGet cache invalidation and release-definition drift detect it while fallback approvals remain unaffected by routine package-version changes.
- Given CPM-ineligible version mechanisms, when the migration completes, then AppHost SDK, .NET SDK, npm/tool, product-version, and package-validation baseline pins are unchanged.

## Spec Change Log

## Verification

**Commands:**
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` -- shared catalog evaluates with valid NuGet versions.
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- validator regression suite passes.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Debug -m:1 && dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -m:1` -- both dependency modes restore without central-version collisions.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -warnaserror -m:1` -- serialized Release build passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category=Governance"` -- affected governance lane passes.
- Isolated Memories and Parties restores with nested Builds materialized at each declared gitlink -- both restores pass without `NU1010`, `NU1506`, or `NU1109`.
