---
title: 'Bump Latest Hexalith Module NuGet Packages'
type: 'refactor'
created: '2026-08-15'
status: 'in-progress'
baseline_commit: '726cf20190429e1953e064b59ef8d23203029fa4'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Builds catalog still selects EventStore `3.94.1` and Memories `2.21.1` while nuget.org lists aligned stable `3.95.0` (13/13 EventStore packages) and `2.21.3` (3/3 Memories packages). FrontComposer already points `references/Hexalith.Builds` at `58987900cff1e1f67c7f66966023789a104bc349` while CI/Release execution remains `3f0e3595be693fce56a37648c0bd0f89390f5fd3`, so a catalog re-pin must also converge those coordinates.

**Approach:** Advance only those two Hexalith families in the Builds catalog and matching audit rows. After a human-created Builds commit, re-pin FrontComposer's Builds gitlink and in-scope workflow/evaluator coordinates to that exact commit. Leave already-latest families and external pins unchanged.

## Boundaries & Constraints

**Always:** Edit version authority in Hexalith.Builds; keep each family aligned on one `Hexalith*Version`; refresh audit `selectedVersion` to the actual catalog pin with listed nuget.org evidence; keep FrontComposer's root wrapper version-free and do not mirror `Hexalith*Version` point values in `selected_catalog_required_properties`. After the Builds commit exists, the FrontComposer gitlink, in-scope CI/Release/release-evidence pins, and active `evaluator_authorizations` must name that same SHA. Retain historical evaluator rows that published-release provenance still requires. If push evaluation needs a prior policy commit, authorize the future closure before moving gitlink/workflow bytes. Update root gitlinks with `git -c submodule.recurse=false submodule update --init`.

**Ask First:** Creating or pushing the Builds commit; advancing EventStore or Memories source gitlinks; changing Chatbot `1.80.0`; changing external-package pins; dropping historical evaluator rows.

**Never:** Add FrontComposer-local `PackageVersion` or `Hexalith*Version` overrides; bump FrontComposer's own `4.1.1` catalog pin; accept an internal downgrade or a stable-to-prerelease move; edit nested submodules; use recursive or remote submodule updates; claim Story 11.24 complete; land with gitlink and execution SHA still diverged.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| EventStore minor | Catalog `3.94.1`, nuget.org stable `3.95.0` on all 13 family packages | `HexalithEventStoreVersion` and all 13 EventStore rows become `3.95.0` | Fail if any EventStore package is unpublished or split |
| Memories patch | Catalog `2.21.1`, nuget.org stable `2.21.3` on all 3 family packages | `HexalithMemoriesVersion` and all 3 Memories rows become `2.21.3` | Fail if any Memories package is unpublished or split |
| Already latest | Commons `2.30.0`, Polymorphic `1.19.2`, Tenants `5.4.1`, Parties `1.0.0`, FrontComposer `4.1.1` | Properties stay unchanged | N/A |
| Unpublished Chatbot | `Hexalith.Chatbot.Contracts` 404 on nuget.org | Keep `1.80.0` | Do not invent a replacement |
| Internal downgrade | A proposed pin below the audited floor | Builds audit validator rejects it | Name package and compared versions |
| Coordinate drift | Gitlink `58987900…` vs execution `3f0e3595…`, then a new catalog commit | Landing gitlink, in-scope workflow pins, and active evaluator rows all name the new Builds commit | `CiGovernanceTests` fails if any in-scope pin differs from the gitlink |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:6-13,40-52,67-69` -- family properties and EventStore/Memories `PackageVersion` rows. Current defaults: EventStore `3.94.1`, Memories `2.21.1`. Authority for this bump.
- `references/Hexalith.Builds/Directory.Packages.props:1-3` -- thin re-export; do not add versions here.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- EventStore.Contracts `selectedVersion` `3.94.1` (~4360); Memories.Contracts `2.21.1` (~4889). Must match the new catalog pins.
- `references/Hexalith.Builds/Tools/README.md:67-111` -- official bump path: `audit-central-package-versions.ps1` → apply accepted internal versions → `validate-package-version-audit.ps1`, `test-package-version-audit-generator.ps1`, `test-package-version-audit-validator.ps1`.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` and `Tools/test-authoritative-package-catalog.ps1` -- structural catalog checks; they do not pin literals.
- `Directory.Packages.props:1-14` -- FrontComposer version-free import shim. Read-only for this change.
- `eng/dependency-graph-policy.json:54-83,346-end` -- shape-only six `Hexalith*Version` names and empty `selected_catalog_required_properties`; active evaluator rows name `3f0e3595…` (historical `0a3508b3…`). Do not mirror the new point values. Append/regenerate active closures for the integrated Builds commit; keep historical rows that published releases still need.
- `eng/dependency_handoff.py` -- `draft-evaluator --stage {ci|release|post_release}` regenerates caller blob, reusable commit, action commits, `closure_digest`, and `definition_digest`.
- `.github/workflows/ci.yml:25`, `release.yml:17,233,286,321,329`, `release-evidence.yml:233` -- in-scope execution identities currently `3f0e3595be693fce56a37648c0bd0f89390f5fd3`. Must equal the Builds gitlink after landing.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:778-807` -- gitlink SHA must equal `release.yml` `BUILDS_EXECUTION_SHA`, `ci.yml` `domain-ci.yml@`, and `release-evidence.yml` Builds `ref`.
- Current Builds gitlink: `58987900cff1e1f67c7f66966023789a104bc349`. Do not treat this SHA as the execution contract.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- set `HexalithEventStoreVersion` to `3.95.0` and `HexalithMemoriesVersion` to `2.21.3` -- these are the only Hexalith families behind nuget.org stable.
- [x] `references/Hexalith.Builds/Tools/package-version-audit.json` -- refresh EventStore and Memories internal rows so `selectedVersion` equals the new catalog pins and records listed nuget.org evidence -- the validator requires exact catalog↔audit match.
- [x] HALT for a human-created or human-approved Builds commit, then advance the root `references/Hexalith.Builds` gitlink to that exact commit -- FrontComposer CI reads the gitlink, not a dirty submodule tree. Integrated Builds commit: `7867d8fc7bcc3c906b16f0867f6555d8bec5432d` (working tree; HEAD gitlink still `58987900…` until the parent commit).
- [ ] `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/workflows/release-evidence.yml`, and `eng/dependency-graph-policy.json` -- move in-scope execution pins and active evaluator closures to the integrated Builds commit; keep historical evaluator rows that published-release provenance still requires -- `CiGovernanceTests` requires pins to equal the gitlink, and current `58987900…` vs `3f0e3595…` drift must not survive landing.
- [x] Confirm FrontComposer `Directory.Packages.props` and `selected_catalog_required_properties` stay version-free -- a compatible catalog re-pin must not reintroduce local mirrors.

**Acceptance Criteria:**
- Given nuget.org lists EventStore `3.95.0` and Memories `2.21.3` on every family package, when Builds evaluates the catalog, then those families are aligned on those versions and every other Hexalith family property is unchanged.
- Given the refreshed audit, when Builds audit and structural catalog validators run, then they pass and still reject an internal downgrade.
- Given the integrated Builds commit, when FrontComposer imports the catalog and evaluates CI/Release provenance, then it inherits the new pins with no local `PackageVersion` override, and every in-scope execution pin plus every active authorized evaluator closure names that exact commit.

## Spec Change Log

## Design Notes

`CiGovernanceTests` binds catalog gitlink and workflow execution SHA as one FrontComposer landing coordinate. Catalog authority still lives in Builds; the SHA lockstep is how FrontComposer is allowed to consume the new catalog, not a second version-mirror.

## Verification

**Commands:**
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1`, `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1`, `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1` -- expected: all pass after EventStore/Memories audit rows match `3.95.0` / `2.21.3`.
- `python3 eng/dependency_graph.py --root . validate --commit HEAD` -- expected: `ok=true` after the Builds gitlink and active evaluator closures match.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~CiGovernanceTests|FullyQualifiedName~CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds"` -- expected: gitlink equals in-scope pins; catalog inherited.
- `DiffEngine_Disabled=true dotnet restore Hexalith.FrontComposer.slnx` then `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- expected: restore uses EventStore `3.95.0` and Memories `2.21.3`; build is warning-free.
