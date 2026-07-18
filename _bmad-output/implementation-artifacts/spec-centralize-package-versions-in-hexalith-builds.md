---
title: 'Centralize FrontComposer package versions in Hexalith.Builds'
type: 'refactor'
created: '2026-07-18'
status: 'done'
review_loop_iteration: 2
baseline_commit: 'f49af5d0c85fe9c911095e665aed1c5ac91d44db'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer defines 14 NuGet dependency versions after importing the shared `Hexalith.Builds` catalog, so package-version authority is split and release/cache controls do not uniformly track the real catalog. Memories and Parties also need standalone-safe access to the migrated pins rather than relying on FrontComposer's sibling fallback.

**Approach:** Move those declarations unchanged into `references/Hexalith.Builds/Props/Directory.Packages.props`, retain the root import shim, remove or override sibling collisions, and keep each affected sibling on a Builds commit containing the pins.

## Boundaries & Constraints

**Always:** Retain root CPM, transitive pinning, path fallbacks, and guarded imports; keep authoritative shared pins as unconditional `Include` items with no later `Remove`; preserve Parties MCP ASP.NET Core `1.4.0`; use nested Builds `e64ae34e50086ae55d47971d70897d579ff18c25` for EventStore and Memories and `cfafcbf1e904138b435b63ba4fd79f86b8dda069` for Parties; keep the shared catalog UTF-8 BOM/CRLF; preserve unrelated work. Accepted effective changes are EventStore TimeProvider `10.7.0→10.8.0`, System.CommandLine `2.0.9→2.0.10`, MCP `1.4.0→1.4.1`, and its nested Hexalith.EventStore `3.70.0→3.70.1`; Memories nested Hexalith.EventStore `3.70.0→3.70.1` and Tenants `3.2.13→3.2.15`; Parties nested Commons `2.28.1→2.28.2`, PolymorphicSerializations `1.16.3→1.16.5`, EventStore `3.68.1→3.70.0`, and Tenants `3.2.11→3.2.15`.

**Ask First:** Any unlisted version change, apparently-unused pin removal, or new conditional version policy.

**Never:** Move SDK, npm/tool, product, or API-baseline versions into CPM; alter package references; add compatibility pins; centralize other submodule catalogs; weaken validation or fallback approvals.

</frozen-after-approval>

## Code Map

- Root/shared/sibling `Directory.Packages.props` files and affected nested Builds gitlinks -- ownership, overrides, and standalone compatibility.
- `.github/workflows/quality.yml`, `eng/release_evidence.py`, and release fixtures -- cache and release-definition fingerprints.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/*.cs` and the analyzer ledger -- ownership, policy, release, and identifier governance.

## Tasks & Acceptance

**Execution:**
- [x] Catalog migration and collision reconciliation -- move the 14 exact pins centrally, retain the root shim, and preserve sibling effective versions.
- [x] Cache, release-evidence, fixture, ownership, and identifier baselines -- track the authoritative shared catalog without changing fallback invalidation.
- [x] Affected sibling catalogs and nested Builds gitlinks -- use the approved inherited versions, Parties override, and compatible commits.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- require unconditional shared `Include` pins with no masking `Remove`; assert all accepted sibling versions/gitlinks, encoding, case-insensitive overrides, provider filtering, and both scanner operation forms.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh the governed identifier hash after final test layout.

**Acceptance Criteria:**
- Given the pre-change FrontComposer catalog, the same 14 pairs occur exactly once as unconditional shared `Include` items, no matching `Remove` exists, and the root has zero local `PackageVersion` items.
- Given sibling evaluation, EventStore inherits TimeProvider `10.8.0`, System.CommandLine `2.0.10`, and MCP `1.4.1`; Memories inherits TimeProvider `10.8.0` and MCP ASP.NET Core `1.4.1`; Parties resolves MCP ASP.NET Core `1.4.0`; restores have no `NU1010`, `NU1506`, or `NU1109`.
- Given standalone EventStore, Memories, and Parties trees with their declared Builds commits, representative restores succeed with those effective versions.
- Given a shared-catalog edit, when cache keys and release evidence are evaluated, then NuGet cache invalidation and release-definition drift detect it while fallback approvals remain unaffected by routine package-version changes.
- Given CPM-ineligible version mechanisms, when the migration completes, then AppHost SDK, .NET SDK, npm/tool, product-version, and package-validation baseline pins are unchanged.

## Spec Change Log

- 2026-07-18, loop 2: Human accepted EventStore's centralized upgrades and newer compatible Builds gitlinks. Preserve the validated migration, fingerprints, root shim, Parties override, and standalone restores.

## Design Notes

The 14 migrated pairs are `BenchmarkDotNet 0.15.8`; `FsCheck.Xunit.v3 3.3.3`; `Microsoft.CodeAnalysis.Workspaces.Common 5.6.0`; `Microsoft.Extensions.Localization 10.0.9`; `Microsoft.Extensions.TimeProvider.Testing 10.8.0`; `ModelContextProtocol.AspNetCore 1.4.1`; `NUlid 1.7.3`; `PactNet 5.0.1`; `System.Collections.Immutable 10.0.10`; `System.ComponentModel.Annotations 5.0.0`; `System.Reactive 7.0.0-rc.1`; `System.Threading.Tasks.Extensions 4.6.3`; `Verify 31.24.2`; `Verify.XunitV3 31.24.2`.

## Verification

**Commands:**
- Run both `references/Hexalith.Builds/Tools/*central-package-version*.ps1` validators -- catalog and regression suite pass.
- Restore `Hexalith.FrontComposer.slnx` in Debug and Release, then serialized Release build with warnings as errors -- all pass without CPM diagnostics.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category=Governance"` -- governance passes.
- Isolated `git archive` trees for EventStore, Memories, and Parties with Builds materialized at each declared gitlink; restore representative CLI/MCP/test projects -- all pass without CPM diagnostics.

## Suggested Review Order

**Intent and ownership**

- Start with the approved centralized versions, compatible gitlinks, and preserved override.
  [`spec-centralize-package-versions-in-hexalith-builds.md:17`](./spec-centralize-package-versions-in-hexalith-builds.md#L17)

- See the end-to-end ownership and sibling-version governance entry point.
  [`InfrastructureGovernanceTests.cs:36`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L36)

**MSBuild edge handling**

- Compound and wildcard item specs are matched with NuGet-style case insensitivity.
  [`InfrastructureGovernanceTests.cs:463`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L463)

- Authoritative pins reject updates, removals, exclusions, conditions, and branch selection.
  [`InfrastructureGovernanceTests.cs:479`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L479)

- Parties' lower MCP override must remain singular and unconditional.
  [`InfrastructureGovernanceTests.cs:502`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L502)

**Regression evidence**

- Negative fixtures exercise every newly rejected operation shape.
  [`InfrastructureGovernanceTests.cs:266`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L266)

- The fail-closed identifier inventory records the final test layout.
  [`analyzer-policy-exception-ledger-v1.json:74`](../contracts/analyzer-policy-exception-ledger-v1.json#L74)
