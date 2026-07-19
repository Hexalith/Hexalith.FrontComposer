---
title: 'Make Parties package authority standalone-safe'
type: 'bugfix'
created: '2026-07-19'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: '68cb94eb42a5c5b0814d0eb8ff78ff0ee4d05df9'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Parties/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `references/Hexalith.Parties/Directory.Packages.props` previously overrode three shared package versions. Removing those declarations centralizes authority but breaks standalone Parties checkouts because its root-declared Builds gitlink still selects `041897f0`, which lacks `Microsoft.AspNetCore.Components.CustomElements`.

**Approach:** Keep Parties free of local `PackageVersion` rows, advance its root-declared `references/Hexalith.Builds` gitlink to `c177c66af5d3f509328c2f568dc0737fe9f89e4e`, and make umbrella and standalone evaluation inherit the same three shared versions. Update FrontComposer governance, guidance, and the parent Parties gitlink to represent and verify that topology.

## Boundaries & Constraints

**Always:** Preserve Parties' central-package-management properties and three guarded import paths; retain the Builds catalog's existing authoritative `Include` rows and approved versions (`Microsoft.AspNetCore.Components.CustomElements` `10.0.10`, `ModelContextProtocol` `1.4.1`, and `ModelContextProtocol.AspNetCore` `1.4.1`); initialize/update only Parties' root-declared Builds dependency; use the minimum local Conventional Commits needed to represent the Parties Builds gitlink and FrontComposer Parties gitlink; leave unrelated work untouched; do not push.

**Ask First:** Any change to the three shared versions, import precedence, central transitive pinning, catalog override policy, package references, or nested dependency other than the approved Parties Builds gitlink.

**Never:** Copy duplicate rows into Builds; downgrade Builds to the former Parties-local `10.0.9`/`1.4.0` values; retain consumer-side `Include`/`Update` operations; add inline project versions; stage or commit unrelated paths; push, force-push, or rewrite remote history.

</frozen-after-approval>

## Code Map

- `references/Hexalith.Parties/Directory.Packages.props` -- versionless consumer wrapper whose local rows are already removed.
- `references/Hexalith.Parties/references/Hexalith.Builds` -- Parties-owned Builds gitlink; must advance from deficient `041897f0` to compatible `c177c66`.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- unchanged authoritative catalog containing all three approved rows.
- `references/Hexalith.Parties/_bmad-output/project-context.md` -- foundational guidance still naming the superseded effective versions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- ownership and gitlink guard; the Parties assertion must run independently of the pre-existing catalog line-ending guard.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- governed test-identifier inventory affected by the final assertion shape.
- `references/Hexalith.Parties` -- FrontComposer gitlink that must record the standalone-safe Parties commit.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Parties/Directory.Packages.props` -- remove the local Application/MCP package-version groups while retaining CPM properties and imports.
- [x] `references/Hexalith.Parties/references/Hexalith.Builds` -- advance only the Builds gitlink to full commit `c177c66af5d3f509328c2f568dc0737fe9f89e4e`.
- [x] `references/Hexalith.Parties/_bmad-output/project-context.md` -- align MCP and CustomElements guidance to `1.4.1` and `10.0.10`.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- add an independently executable Parties ownership test that requires the compatible gitlink, all three imports/shared rows, and zero local `PackageVersion` declarations.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh the final governed identifier count/hash.
- [x] `references/Hexalith.Parties` -- advance the FrontComposer gitlink to the local standalone-safe Parties commit.

**Acceptance Criteria:**
- Given umbrella and standalone-equivalent layouts, when Parties evaluates central package versions, then the three identities occur exactly once at `10.0.10`/`1.4.1`/`1.4.1` with no sibling fallback dependency.
- Given the Parties repository, when package authority is inspected, then its Builds gitlink is exactly `c177c66`, its wrapper retains three guarded imports, and no tracked consumer XML declares `PackageVersion` or inline versions.
- Given FrontComposer governance, when the focused Parties ownership test runs independently, then it validates the shared rows, compatible Parties Builds gitlink, and absence of every local `PackageVersion` declaration.
- Given Picker and MCP consumers in the standalone-equivalent topology, when their Release builds and focused tests run, then they pass without `NU1010`, `NU1506`, or downgrade diagnostics.

## Spec Change Log

- 2026-07-19, loop 1: Adversarial review found that the uninitialized nested Builds checkout made umbrella validation false-green while standalone Parties selected deficient `041897f0`. Human approved advancing Parties' Builds gitlink. The spec now requires a standalone-equivalent topology and an independently executable ownership guard. KEEP: shared `10.0.10`/`1.4.1` rows, zero consumer overrides, guarded imports, unrelated-work preservation, and no further push.

## Design Notes

`c177c66` matches FrontComposer's Builds gitlink and contains the CustomElements row introduced by `96c83fc`; its package catalog is unchanged from that introducing commit. A standalone verification tree must materialize Builds only under `Parties/references/Hexalith.Builds` and place Parties deeply enough that both sibling fallback paths are absent.

## Verification

**Commands:**
- `pwsh -NoLogo -NoProfile -File ./Tools/validate-central-package-versions.ps1` and `./Tools/test-authoritative-package-catalog.ps1` from `references/Hexalith.Builds` -- expected: 283-entry structure and 48 approved values pass unchanged.
- Run `validate-consumer-package-authority.ps1` against a temporary Parties archive with Builds `c177c66` materialized only at `references/Hexalith.Builds` -- expected: every tracked consumer project inherits the nested catalog with no authority violations.
- `dotnet build src/Hexalith.Parties.Picker/Hexalith.Parties.Picker.csproj --configuration Release -p:UseNuGetDeps=true` and `dotnet build src/Hexalith.Parties.Mcp/Hexalith.Parties.Mcp.csproj --configuration Release -p:UseNuGetDeps=true` in the standalone-equivalent tree -- expected: zero package-version diagnostics.
- Run the Picker and MCP test projects in Release, then invoke `InfrastructureGovernanceTests.PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` and `AnalyzerPolicyGovernanceTests` through the built xUnit v3 executable -- expected: all focused tests pass.
- `git diff --check` in Parties and FrontComposer -- expected: no whitespace errors; the broader Governance lane's pre-existing Builds bare-LF failure is reported separately.

**Observed 2026-07-19:** Builds validation passed for 283 central entries and 48 approved values. The isolated Parties tree validated 29 consumer projects and evaluated the three effective versions exactly once at `10.0.10`/`1.4.1`/`1.4.1`. Picker and MCP Release builds passed with zero warnings/errors; their test projects passed 171/171 and 57/57. The focused Parties ownership fact and analyzer-policy governance test passed; the final test identifier inventory is 6189 tokens with SHA-256 `7e8bd7e70f36f514e5a6f4a98c4be5114611fb9896f1e94d94305b0ee63d717a`.
