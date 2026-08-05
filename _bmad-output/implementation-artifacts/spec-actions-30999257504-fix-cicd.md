---
title: 'Restore green CI after Builds/Memories advance'
type: 'bugfix'
created: '2026-08-05'
status: 'done'
review_loop_iteration: 0
baseline_commit: '5e544e712e6386b148292ce2e1cf1bfada46cd5a'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After `build(deps): advance Builds and Memories submodule pointers` (`5e544e71`), CI run [30999257504](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30999257504) and Quality run [30999257333](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30999257333) fail: dependency-governance / Gate 2b reject stale `HexalithEventStoreVersion` `3.91.0` (Builds catalog is `3.91.1`), and Windows `accessibility-visual` cannot checkout EventStore evidence paths that exceed MAX_PATH.

**Approach:** Synchronize the FrontComposer catalog policy to the already-selected Builds pin `3.91.1`, and scope the Windows a11y job submodule init to `references/Hexalith.Builds` only so EventStore long paths are never materialized.

## Boundaries & Constraints

**Always:** Keep `windows-latest` for `accessibility-visual` (Win32 visual baselines); retain process-scoped `core.symlinks=false` (no global git config); keep checkout `submodules: false`; leave root-declared EventStore/Builds/Memories gitlinks unchanged in this fix; leave unrelated dirty worktree files untouched.

**Ask First:** Moving a11y to Linux; enabling `core.longpaths` as the primary fix; changing EventStore/Builds/Memories gitlinks; editing EventStore evidence trees; disabling or making advisory the a11y/visual gate; bumping any catalog property other than `HexalithEventStoreVersion`.

**Never:** Full `initialize-build` on the Windows a11y job; recursive nested submodule init; rewriting EventStore `_bmad-output` evidence inside FrontComposer; skipping governance validation; committing the pre-existing dirty BUILD-REL-1 / bmad-loop / `.gitignore` worktree changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Catalog sync | Builds gitlink `bd94f7fe…` catalogs `HexalithEventStoreVersion=3.91.1`; policy expects `3.91.1` | `dependency_graph.py validate` and Gate 2b governance tests pass | Stale `3.91.0` fails closed with expected/found text |
| Windows a11y init | `accessibility-visual` on `windows-latest` | Only `references/Hexalith.Builds` is initialized; Counter specimen builds via NuGet | EventStore MAX_PATH paths are never checked out |
| Governance pin | `CiGovernanceTests.QualityWorkflow_PinsAccessibilityVisualGate` | Pins Builds-only init + `core.symlinks=false`; rejects global git config and silent gate drop | Test fails if full `initialize-build` returns |

</frozen-after-approval>

## Code Map

- `eng/dependency-graph-policy.json` -- `profiles.frontcomposer-catalog-v1.selected_catalog_required_properties.HexalithEventStoreVersion` still `"3.91.0"`; sole required edit for catalog drift (precedent `30b4821e`).
- `references/Hexalith.Builds/Props/Directory.Packages.props` @ `bd94f7fe…` -- observed catalog `HexalithEventStoreVersion=3.91.1` (read-only).
- `eng/dependency_graph.py` -- `assert_selected_catalog_property` produces the CI error text; `validate` loads working-tree policy (must be committed for push governance).
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` / `PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` shell out to `python3 eng/dependency_graph.py validate` (no hardcoded versions).
- `tests/eng/test_dependency_graph.py` -- `test_all_governed_selected_catalog_properties_match_and_mutations_fail` reads expected values from policy JSON.
- `.github/workflows/quality.yml` -- `accessibility-visual` (~L417–490): `initialize-build@main` inits all root submodules; replace with path-scoped Builds init.
- `references/Hexalith.Builds/Github/initialize-build/action.yml` -- full `submodule update --init` (do not change; stop using it on this job).
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- `QualityWorkflow_PinsAccessibilityVisualGate` (~L196–221) extracts step `Initialize build submodules` and pins `GIT_CONFIG_*` / forbids `git config --global`; retarget pins to Builds-only init.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` -- set `HexalithEventStoreVersion` from `3.91.0` to `3.91.1` to match pinned Builds catalog `bd94f7fe…`.
- [x] `.github/workflows/quality.yml` -- in `accessibility-visual`, replace `Hexalith/Hexalith.Builds/Github/initialize-build@main` with process-scoped `git -c submodule.recurse=false submodule update --init references/Hexalith.Builds`, keeping `core.symlinks=false` via `GIT_CONFIG_*`.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- update `QualityWorkflow_PinsAccessibilityVisualGate` to require Builds-only init (path + no full `initialize-build`), retain symlink env pins and non-advisory a11y steps.

**Acceptance Criteria:**
- Given HEAD `5e544e71` plus this fix, when `python3 eng/dependency_graph.py --root . validate --commit "$(git rev-parse HEAD)"` runs, then it exits 0 with no `HexalithEventStoreVersion` expected/found mismatch.
- Given the updated Quality workflow, when `accessibility-visual` initializes submodules on Windows, then only `references/Hexalith.Builds` is checked out and EventStore long-path evidence is never materialized.
- Given `CiGovernanceTests.QualityWorkflow_PinsAccessibilityVisualGate`, when the workflow YAML is scanned, then it fails closed if full `initialize-build` or global git config returns, or if Builds-only init / `core.symlinks=false` is removed.

## Spec Change Log

## Verification

**Commands:**
- `python3 eng/dependency_graph.py --root . validate --commit "$(git rev-parse HEAD)"` -- expected: exit 0.
- `python3 -m unittest tests.eng.test_dependency_graph -v` -- expected: all pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "FullyQualifiedName~InfrastructureGovernanceTests|FullyQualifiedName~QualityWorkflow_PinsAccessibilityVisualGate"` -- expected: focused governance facts pass.
- `actionlint .github/workflows/quality.yml` -- expected: no diagnostics (if `actionlint` is available).
- `git diff --check` -- expected: no whitespace errors on intentionally staged files only.

**Manual checks (if no CLI):**
- Confirm dirty BUILD-REL-1 / bmad-loop / `.gitignore` files remain unstaged and untouched.
- Confirm no EventStore/Builds/Memories gitlink changes appear in the fix commit.

## Suggested Review Order

**Catalog pin**

- Align FrontComposer policy with Builds `HexalithEventStoreVersion` `3.91.1`.
  [`dependency-graph-policy.json:34`](../../eng/dependency-graph-policy.json#L34)

**Windows a11y submodule scope**

- Builds-only init under bash with process-scoped `core.symlinks=false`.
  [`quality.yml:428`](../../.github/workflows/quality.yml#L428)

**Governance pins**

- Fail closed if full init, EventStore, or missing bash shell returns.
  [`CiGovernanceTests.cs:216`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L216)
