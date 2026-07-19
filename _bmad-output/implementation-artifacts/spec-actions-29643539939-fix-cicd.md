---
title: 'Fix CI restore catalog compatibility for current FrontComposer dependencies'
type: 'bugfix'
created: '2026-07-18'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: 'd8ea9c32fb7bba3ab26da7a6b87b7edb947f5714'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run [29643539939](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29643539939) fails in the shared CI workflow's `Restore` step because the shared `Hexalith.Builds` catalog requested `Hexalith.Tenants.Client` `3.15.1`, which NuGet.org does not publish; the newest available release is `3.2.18`. The accepted `Hexalith.Builds` advance to `8fd1b07` corrects that version, while the later current-`main` run 29644694613 additionally exposes an obsolete Parties `MinVer` build dependency and a missing direct Picker package version.

**Approach:** Retain the accepted Builds and Tenants submodule advances, remove the obsolete MinVer integration because release versions come from semantic-release, and restore the Picker's direct CustomElements package version in the Parties catalog. Validate the unmodified CI restore command without hiding compatibility failures through source project references or workflow-only overrides.

## Boundaries & Constraints

**Always:** Preserve `Directory.Packages.props` as FrontComposer's import-only shim with zero `PackageVersion` operations; keep CI/CD Release/package-oriented; retain the accepted Builds head `8fd1b07` with its published Tenants `3.2.18` correction and the accepted Tenants head `5f16001`; use semantic-release rather than MinVer for Parties release versions; retain `Microsoft.AspNetCore.Components.CustomElements` `10.0.9` as the direct Picker dependency version; retain the existing patched `AngleSharp` `1.5.2` entry.

**Ask First:** Before choosing a different Tenants package version, reintroducing MinVer, or replacing the accepted Builds or Tenants heads, stop for a new decision.

**Never:** Do not change `.github/workflows/ci.yml` to force project references or to inject a one-off version property; do not add, update, remove, or conditionally mask `PackageVersion` items in FrontComposer; do not add a MinVer package or configuration back to Parties; do not suppress NuGet errors, initialize nested submodules, or change unrelated catalog versions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Original restore failure | CI checks out the accepted Builds gitlink and runs `dotnet restore Hexalith.FrontComposer.slnx` | `Hexalith.Tenants.Client` resolves to `3.2.18`; Restore exits zero with no `NU1102` for Tenants | Fail the gate with the unresolved package ID/version if NuGet metadata is inconsistent |
| Current Parties revision | Parties no longer references MinVer and Picker references `Microsoft.AspNetCore.Components.CustomElements` | The Parties catalog supplies the direct Picker version and Restore has no `NU1010` | Fail Restore if Picker lacks exactly one catalog version or a MinVer reference remains |
| Catalog ownership | FrontComposer's root package props remains an import shim | Governance continues to find zero root `PackageVersion` operations | Reject any attempted root-local workaround |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds` -- accepted root-declared gitlink advance at `8fd1b07`; its catalog restores the published Tenants version `3.2.18`.
- `references/Hexalith.Tenants` -- accepted root-declared gitlink advance at `5f16001`; preserve it without further edits.
- `references/Hexalith.Parties/Directory.Build.props` -- has the obsolete MinVer configuration and `PackageReference` to remove because semantic-release owns versioning.
- `references/Hexalith.Parties/Directory.Packages.props` -- Parties-specific central package catalog; restore the direct Picker CustomElements version and the governed lower MCP package overrides here.
- `references/Hexalith.Parties/src/Hexalith.Parties.Picker/Hexalith.Parties.Picker.csproj` -- directly requires `Microsoft.AspNetCore.Components.CustomElements` and remains unchanged.
- `Directory.Packages.props` -- FrontComposer import shim; its zero-local-version boundary is enforced and must remain unchanged.
- `.github/workflows/ci.yml` -- delegates the failing `dotnet restore Hexalith.FrontComposer.slnx` gate to `Hexalith.Builds` and is intentionally not a remediation surface.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- package ownership and nested Builds gitlink guard; assert the accepted published Tenants version, restored Parties package contract, absent MinVer configuration, and distinct submodule pins.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- records the fail-closed test identifier inventory; refresh its test-source hash when this intentional governance-test change alters that inventory.

## Tasks & Acceptance

**Execution:**

- [x] `references/Hexalith.Builds` and `references/Hexalith.Tenants` -- retain the accepted advanced heads `8fd1b07` and `5f16001` without modifying their content -- preserves the independently supplied published-Tenants correction and related dependency bumps.
- [x] `references/Hexalith.Parties/Directory.Build.props` -- remove the MinVer override, tag/pre-release configuration, and private `PackageReference` -- semantic-release, rather than MinVer, owns release version calculation and Restore no longer needs a MinVer central package version.
- [x] `references/Hexalith.Parties/Directory.Packages.props` -- restore the unconditional `Microsoft.AspNetCore.Components.CustomElements` package version `10.0.9` and the existing `ModelContextProtocol`/`ModelContextProtocol.AspNetCore` `1.4.0` overrides -- satisfies the existing Picker dependency and the governed Parties MCP compatibility contract under central package management.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- split the former shared Memories/Parties nested Builds expectation, pin Memories to its accepted `437c4c02` advance, and prove the published Tenants/Parties package configuration -- keeps the governance invariants aligned with the separately versioned submodules and guards the complete restore fix.
- [ ] `references/Hexalith.Parties` gitlink -- record the reviewed Parties correction alongside the accepted Builds and Tenants heads without resetting any of those three submodules -- makes the complete restore-compatible set reproducible in CI.
- [x] `Directory.Packages.props` -- inspect without modifying it -- proves the remedy did not bypass the import-only catalog boundary.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh the test-source inventory hash for the intentional governance-test update -- keeps the analyzer-policy gate fail-closed without masking source drift.

**Acceptance Criteria:**

- Given the accepted Builds gitlink and root-declared submodules, when CI runs its unchanged `dotnet restore Hexalith.FrontComposer.slnx` command, then the Restore step exits successfully without `NU1102` for `Hexalith.Tenants.Client` or `NU1010` for `Microsoft.AspNetCore.Components.CustomElements`.
- Given FrontComposer's root `Directory.Packages.props`, when the infrastructure-governance test inspects it, then it contains zero `PackageVersion` operations and retains the shared-catalog import.
- Given the current Parties catalog, when the dependency graph is inspected, then no `MinVer` `PackageReference`, override, tag-prefix, or pre-release configuration remains.
- Given the current Parties catalog, when the infrastructure-governance test reads it, then both MCP package overrides remain at `1.4.0`.
- Given the accepted Memories submodule head, when the infrastructure-governance test reads its nested Builds gitlink, then it expects `437c4c02619cfb3fff7792796e5d76d25c7521ad`, independently from the unchanged Parties and EventStore pins.
- Given the FrontComposer fix commit, when its parent gitlinks are inspected, then the accepted Builds and Tenants heads plus the reviewed Parties correction are retained without unrelated pointer changes.

## Spec Change Log

- 2026-07-18: Governance verification found that the same Parties cleanup had also removed its required lower `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` overrides. Restored those `1.4.0` overrides with the Picker package version; this avoids a passing Restore followed by a deterministic governance failure. KEEP: MinVer remains removed per the human semantic-release decision.
- 2026-07-18: The next governance assertion exposed a stale shared Memories/Parties nested-Builds constant after the accepted Memories advance. Split the constants and pinned Memories to `437c4c02`; this avoids rejecting a valid accepted gitlink while retaining independent checks for all three nested dependencies.
- 2026-07-18: Hardened the same governance test to assert `HexalithTenantsVersion=3.2.18`, the restored Parties CustomElements/MCP entries, and absent MinVer integration. This gives each CI-failure matrix row a passing automated guard in addition to the actual Restore command.
- 2026-07-18: Refreshed the analyzer-policy ledger's test-source hash after that intentional governance-test update changed the deterministic identifier inventory (the token count remains `6188`). This preserves the guard's closed-world detection of subsequent unreviewed test-source drift.

## Design Notes

The accepted Builds update corrects the original unavailable-Tenants catalog value. The current Parties failure is a separate packaging cleanup error: CustomElements remains a direct Picker dependency and the project intentionally preserves lower MCP `1.4.0` compatibility pins, while MinVer must be removed entirely because semantic-release calculates package release versions. This is narrower and more durable than moving CI to source references, which would violate the repository's CI/package-validation contract and conceal future release incompatibilities.

## Verification

**Commands:**

- `dotnet restore Hexalith.FrontComposer.slnx --force-evaluate` -- expected: CI-equivalent Restore succeeds with no `NU1102` or `NU1010` diagnostics.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true --force-evaluate` -- expected: the Release/package restore succeeds against the same catalog.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- expected: the central-package ownership guard passes and the root shim remains version-free.
