---
title: 'Bump Latest Hexalith Module NuGet Packages'
type: 'refactor'
created: '2026-08-14'
status: 'in-progress'
baseline_commit: '7100bd52493846e93303b355ea8cae1ae23ea875'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Builds catalog still selects EventStore `3.94.0` and Memories `2.20.7` while nuget.org lists stable `3.94.1` and `2.21.1`. The checked-in audit still records EventStore `3.93.0`, so catalog and audit already disagree.

**Approach:** Advance only those two Hexalith families in Builds `Props/Directory.Packages.props`, refresh the matching internal audit rows, then re-pin FrontComposer's Builds gitlink to the resulting Builds commit. Leave already-latest families and external pins unchanged.

## Boundaries & Constraints

**Always:** Edit version authority in Hexalith.Builds; keep each family aligned on one `Hexalith*Version`; refresh audit `selectedVersion` to the actual catalog pin with listed nuget.org evidence; keep FrontComposer's root wrapper version-free; treat the catalog gitlink and the immutable Builds execution SHA (`99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`) as separate contracts.

**Ask First:** Creating or pushing the Builds commit; advancing EventStore or Memories source gitlinks; changing Chatbot `1.80.0` (nuget.org 404); changing external-package pins, workflow execution SHA, or evaluator closures.

**Never:** Add FrontComposer-local `PackageVersion` or `Hexalith*Version` overrides; bump FrontComposer's own `4.1.1` catalog pin; accept an internal downgrade or a stable-to-prerelease move; edit nested submodules; use recursive or remote submodule updates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| EventStore patch | Catalog `3.94.0`, nuget.org stable `3.94.1` | `HexalithEventStoreVersion` and all 13 EventStore rows become `3.94.1` | Fail if any EventStore package is unpublished or split |
| Memories minor | Catalog `2.20.7`, nuget.org stable `2.21.1` | `HexalithMemoriesVersion` and all 3 Memories rows become `2.21.1` | Fail if any Memories package is unpublished or split |
| Already latest | Commons `2.30.0`, Polymorphic `1.19.2`, Tenants `5.4.1`, Parties `1.0.0`, FrontComposer `4.1.1` | Properties stay unchanged | N/A |
| Unpublished Chatbot | `Hexalith.Chatbot.Contracts` 404 on nuget.org | Keep `1.80.0` | Do not invent a replacement |
| Internal downgrade | A proposed pin below the audited floor | Builds audit validator rejects it | Name package and compared versions |
| Audit lag | Catalog moves without matching `selectedVersion` | Validator fails closed | Refresh only the advanced internal families |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:6-13,40-52,67-69` -- family properties and EventStore/Memories `PackageVersion` rows. Current defaults: EventStore `3.94.0`, Memories `2.20.7`.
- `references/Hexalith.Builds/Directory.Packages.props:1-3` -- thin re-export; do not add versions here.
- `references/Hexalith.Builds/Hexalith.Package.props` -- packaging metadata only; not version authority.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- EventStore rows still `selectedVersion` `3.93.0` (e.g. `Hexalith.EventStore.Contracts` ~4360); Memories rows still `2.20.7` (~4889). Must match the new catalog pins.
- `references/Hexalith.Builds/Tools/README.md:67-111` -- official bump path: audit → apply accepted internal versions → run the three audit validators.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` -- internal rows need exact catalog↔audit match, monotonic floor, family alignment, stable-channel guard.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` and `Tools/test-authoritative-package-catalog.ps1` -- structural catalog checks; they do not pin literals.
- `Directory.Packages.props:6-13` -- FrontComposer version-free import shim. Read-only for this change.
- `eng/dependency-graph-policy.json:54-83` -- shape-only `Hexalith*Version` names; empty `selected_catalog_required_properties`. Do not mirror the new point values.
- `.github/workflows/ci.yml:25`, `release.yml:17,321,329`, `release-evidence.yml:233` -- execution SHA `99d5a46…`. Leave unless the human advances that coordinate.
- Current Builds gitlink: `606d9f119965c273104d707b9cc8c179fe648237`.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- set `HexalithEventStoreVersion` to `3.94.1` and `HexalithMemoriesVersion` to `2.21.1` -- these are the only Hexalith families behind nuget.org stable.
- [x] `references/Hexalith.Builds/Tools/package-version-audit.json` -- refresh EventStore and Memories internal rows so `selectedVersion` equals the new catalog pins and records listed nuget.org evidence -- the validator requires exact catalog↔audit match and already rejects EventStore `3.94.0` vs audit `3.93.0`.
- [ ] HALT for a human-created or human-approved Builds commit, then advance the root `references/Hexalith.Builds` gitlink to that exact commit -- FrontComposer CI reads the gitlink, not a dirty submodule tree.
- [x] Confirm FrontComposer-owned catalog/policy/workflow files stay version-free and keep execution SHA `99d5a46…` -- a compatible catalog re-pin must not reintroduce local mirrors.

**Acceptance Criteria:**
- Given nuget.org lists EventStore `3.94.1` and Memories `2.21.1`, when Builds evaluates the catalog, then those families are aligned on those versions and every other Hexalith family property is unchanged.
- Given the refreshed audit, when Builds audit and structural catalog validators run, then they pass and still reject an internal downgrade.
- Given the integrated Builds commit, when FrontComposer imports the catalog and runs dependency-graph validation, then it inherits the new pins with no local `PackageVersion` override.

## Spec Change Log

## Verification

**Commands:**
- `pwsh -File references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` plus `pwsh -File references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` plus `pwsh -File references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1` -- expected: pass after the catalog and audit agree.
- `python3 eng/dependency_graph.py --root . validate --commit HEAD` -- expected: `ok=true` after the Builds gitlink is updated.
- `DiffEngine_Disabled=true dotnet restore Hexalith.FrontComposer.slnx` then `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- expected: restore uses EventStore `3.94.1` and Memories `2.21.1`; build is warning-free.
