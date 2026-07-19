---
title: 'Move Parties package-version authority to Hexalith.Builds'
type: 'refactor'
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

**Problem:** `references/Hexalith.Parties/Directory.Packages.props` declares three package versions after importing the authoritative Hexalith.Builds catalog. Those consumer-side declarations split version authority, create a duplicate `Microsoft.AspNetCore.Components.CustomElements` item, and fail the Builds consumer-package-authority validator.

**Approach:** Remove the Parties-local `PackageVersion` item groups and inherit the three declarations already present in `references/Hexalith.Builds/Props/Directory.Packages.props`. Update the FrontComposer governance assertion so it requires shared ownership and rejects future Parties-local operations.

## Boundaries & Constraints

**Always:** Preserve Parties' central-package-management properties and three guarded import paths; retain the Builds catalog's existing authoritative `Include` rows and approved versions (`Microsoft.AspNetCore.Components.CustomElements` `10.0.10`, `ModelContextProtocol` `1.4.1`, and `ModelContextProtocol.AspNetCore` `1.4.1`); keep edits narrow and preserve existing encoding/line endings outside touched lines; leave unrelated workspace and submodule changes untouched.

**Ask First:** Any change to the three shared versions, import precedence, central transitive pinning, catalog override policy, or package references; any additional package-version cleanup discovered outside the three named identities.

**Never:** Copy duplicate `PackageVersion` rows into Builds; downgrade the shared catalog to the former Parties-local `10.0.9`/`1.4.0` values; retain consumer-side `Include` or `Update` overrides; add inline project versions; initialize/update nested submodules; stage, commit, or push.

</frozen-after-approval>

## Code Map

- `references/Hexalith.Parties/Directory.Packages.props` -- consumer import shim containing the three non-authoritative declarations to remove.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative catalog already containing singular `Include` rows for all three identities.
- `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1` -- primary gate forbidding consumer package-version declarations and checking evaluated parity with Builds.
- `references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1` -- approved shared-value contract for the three Builds entries.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- workspace governance currently expecting Parties-local definitions and requiring realignment to shared ownership.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Parties/Directory.Packages.props` -- remove the `Application` and `MCP` package-version item groups while preserving CPM properties and imports.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- assert the three approved shared rows and require zero matching Parties-local operations.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh the governed test-identifier inventory hash after the assertion rewrite.

**Acceptance Criteria:**
- Given the Builds catalog, when it is evaluated, then each named identity occurs once as an unconditional authoritative `Include` at `10.0.10`/`1.4.1`/`1.4.1`.
- Given the Parties wrapper, when its tracked XML is inspected and evaluated, then it contains no `PackageVersion` declarations and inherits the shared catalog without duplicates or overrides.
- Given Builds' central-catalog, approved-value, and consumer-authority validators, when they run against the edited trees, then all pass without package-authority failures.
- Given the Parties Picker and MCP projects, when they build in Release, then they consume the shared versions without `NU1506`, `NU1010`, or downgrade diagnostics.

## Spec Change Log

## Design Notes

This is a deletion-only move in the two catalogs: Builds already owns all three package identities at newer approved versions, so copying the Parties rows would create forbidden duplicates and replacing the Builds rows would impose an unrequested ecosystem-wide downgrade. The only additional edit realigns the existing governance test with that central ownership.

## Verification

**Commands:**
- `pwsh -NoLogo -NoProfile -File ./Tools/validate-central-package-versions.ps1` from `references/Hexalith.Builds` -- expected: the authoritative catalog passes structural/evaluation validation.
- `pwsh -NoLogo -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` from `references/Hexalith.Builds` -- expected: approved shared values pass.
- `pwsh -NoLogo -NoProfile -File ./Tools/validate-consumer-package-authority.ps1 -RepositoryRoot ../Hexalith.Parties -CatalogPath ./Props/Directory.Packages.props` from `references/Hexalith.Builds` -- expected: Parties passes with no consumer declarations or evaluated drift.
- `dotnet build src/Hexalith.Parties.Picker/Hexalith.Parties.Picker.csproj --configuration Release` and the equivalent MCP project command from `references/Hexalith.Parties` -- expected: both builds succeed without central-package diagnostics.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` from the workspace root -- expected: governance tests pass.
