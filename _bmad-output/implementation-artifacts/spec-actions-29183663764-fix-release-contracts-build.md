---
title: 'Fix Release run 29183663764 Contracts prerequisite'
type: 'bugfix'
created: '2026-07-12'
status: 'done'
review_loop_iteration: 0
baseline_commit: '723e3e1640dfb7e083b31b6481f88f8461500d10'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run `29183663764` invokes `Contracts.Tests` after only its compatible `net10.0` project-reference target has built. The suite deliberately inspects and packs the `netstandard2.0` Contracts output with `--no-build`, so the missing DLL causes `File.Exists` and `NU5026` failures before publishing can proceed.

**Approach:** Establish the same explicit Contracts `netstandard2.0` prerequisite already used by CI Gate 1 before Release starts its per-project test loop. Keep the prerequisite blocking and add governance coverage that locks the command, target framework, and ordering.

## Boundaries & Constraints

**Always:** Restore the `.slnx` before using `--no-restore`; build only `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` for `netstandard2.0` in Release; place the named prerequisite step before `Run release tests`; keep it blocking; preserve `DiffEngine_Disabled=true`, the per-project test inventory, release evidence, and semantic-release ordering.

**Ask First:** Expanding the prerequisite into a full solution build, changing Release triggers or publishing policy, changing test-project order/inventory, making any Release step advisory, or including the existing `Hexalith.AI.Tools` pointer change.

**Never:** Make the Contracts tests self-build, remove their intentional `--no-build` packaging check, hide `NU5026`, add `continue-on-error`, use a legacy `.sln`, recurse into nested submodules, or modify submodule contents.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Fresh Release runner | NuGet cache may exist, but Contracts `obj` and `bin/Release/netstandard2.0` do not | Restore solution assets, build the Contracts `netstandard2.0` DLL, then start release tests | Restore/build failure blocks tests and publishing |
| Contracts boundary tests | `net10.0` test project references a dual-target Contracts project | Required `netstandard2.0` DLL exists and `dotnet pack --no-build` succeeds | Missing output remains a hard test failure |
| Workflow drift | A future edit moves or weakens the prerequisite | Governance test fails on missing command, wrong TFM/configuration, advisory flag, or invalid ordering | CI blocks the regression |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- Release entry point; currently starts per-project tests without producing the out-of-band Contracts target.
- `.github/workflows/ci.yml` -- Existing Gate 1 restore/build sequence to mirror; evidence-only, no change expected.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- Static workflow contract tests and named-step extraction helper.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsKernelOwnershipTests.cs` -- Requires the prebuilt `netstandard2.0` assembly; evidence-only.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsPackageBoundaryTests.cs` -- Packs both Contracts TFMs with `--no-build`; evidence-only.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- add a named blocking restore/build prerequisite immediately before Release tests, matching CI Gate 1 -- guarantee the dual-target artifact exists on fresh runners.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- assert the prerequisite's command, target, configuration, non-advisory status, and ordering before the Contracts test lane -- prevent silent workflow regression.

**Acceptance Criteria:**
- Given a clean `netstandard2.0` output directory, when the Release prerequisite commands run, then the Contracts DLL exists at `bin/Release/netstandard2.0`.
- Given that prerequisite output, when `Contracts.Tests` runs with the Release filter, then all tests pass and package-boundary packing does not emit `NU5026`.
- Given the Release workflow text, when governance tests inspect it, then the prerequisite is blocking, targets `netstandard2.0` Release, and precedes `Run release tests` and semantic-release.
- Given the completed patch, when workflow scope is inspected, then triggers, permissions, evidence, publishing, and submodule configuration remain unchanged.

## Spec Change Log

## Design Notes

The prerequisite is intentionally out-of-band. A `net10.0` test project does not build the second target of a multi-target project reference, while the boundary tests intentionally use `--no-build` to prove packaging from sanctioned Release outputs. Mirroring CI Gate 1 avoids hidden test-order coupling and avoids broadening Release into a redundant full-solution build.

## Verification

**Commands:**
- `dotnet clean src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 --configuration Release -m:1 /nr:false` -- expected: the prerequisite output is absent before verification begins.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release` -- expected: fresh Release assets restore.
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 --configuration Release --no-restore -m:1 /nr:false` -- expected: prerequisite DLL is produced with 0 warnings/errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --filter "Category!=Quarantined"` -- expected: all Contracts tests pass.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false` -- expected: workflow governance tests compile with 0 warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.ReleaseWorkflow_BuildsContractsNetStandard20BeforeContractTests` -- expected: focused workflow guard passes.
- `printf '%s\n' 'fix(release): build contracts test prerequisite' | npx commitlint --verbose` -- expected: zero problems and zero warnings.

**Observed results (2026-07-12):**
- Cleaned the Contracts `netstandard2.0` output, then restored and rebuilt it with 0 warnings and 0 errors; the expected DLL was created.
- Contracts tests passed 200/200, including the `--no-build` package-boundary path that failed in Release run `29183663764`.
- Shell governance build passed with 0 warnings and 0 errors; the focused workflow guard passed 1/1 and the full `CiGovernanceTests` class passed 44/44.
- `actionlint`, `git diff --check`, and the proposed Conventional Commit subject all passed.

## Suggested Review Order

**Release prerequisite**

- Build the missing dual-target artifact immediately before its consuming test lane.
  [`release.yml:82`](../../.github/workflows/release.yml#L82)

- Preserve the existing blocking per-project Release inventory and evidence flow.
  [`release.yml:87`](../../.github/workflows/release.yml#L87)

**Workflow regression guard**

- Lock job scope, exact commands, blocking semantics, adjacency, and Contracts test inclusion.
  [`CiGovernanceTests.cs:271`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L271)

- Extract only the actual `jobs.release` block before evaluating step order.
  [`CiGovernanceTests.cs:1827`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1827)

- Require exactly the two executable prerequisite commands without error swallowing.
  [`CiGovernanceTests.cs:1843`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1843)
