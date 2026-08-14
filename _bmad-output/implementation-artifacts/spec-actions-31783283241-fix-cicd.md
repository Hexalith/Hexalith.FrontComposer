---
title: 'Fix dependency-governance System.CommandLine mirror and restore Builds pin lockstep'
type: 'bugfix'
created: '2026-08-14'
status: 'in-progress'
baseline_commit: '9a3d14b8460ff05ea74d7adbba1547ea9d1ba0b0'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Push CI [31783283241](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31783283241) and HEAD [31784694140](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31784694140) failed `dependency-governance` with `ok=False changes=0 affected=0` while `ci / build-and-test` passed. The artifact error is EventStore→Builds@`606d9f1` `System.CommandLine` expected `2.0.10`, found `2.0.11`. Quality Gate 2b repeats that error; `CiGovernanceTests` also fails because `/pushall` moved the FrontComposer Builds gitlink to `606d9f1` while CI/Release still execute `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`. Sibling catalog version mirrors are the same stale-literal class already removed for `Hexalith*Version`.

**Approach:** Restore the FrontComposer Builds gitlink to the approved execution SHA `99d5a46…`. Stop exact-version-mirroring EventStore/Memories/Parties catalog packages; keep presence plus no-local-override only. Keep exact package pins on `frontcomposer-catalog-v1`. Print the graph `.error` in the collect and enforce steps so the next failure is visible in the job log.

## Boundaries & Constraints

**Always:** Keep FrontComposer exact package pins, catalog structure/ownership checks, affected-module Release/NuGet builds, exact graph/evidence, workflow pin lockstep, and Release `verify-source` fail-closed. Parent-repo gitlink restore only — do not edit files under `references/**`. End state: `git ls-tree HEAD references/Hexalith.Builds` equals every CI/Release/evidence `99d5a46…` coordinate.

**Ask First:** Adopting Builds `606d9f1` as the new execution SHA (two-phase AD-13); changing FrontComposer `selected_catalog_required_packages` versions; making push governance advisory; skipping semantics when the graph digest is unchanged.

**Never:** Edit submodule working trees; weaken provenance; invent evaluator hashes; dispatch a release; touch secrets or `production`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Sibling catalog version move | EventStore/Memories/Parties selected Builds catalog changes a mirrored package version; package still present, no local override | `diff`/`validate` succeed; CI does not require a FrontComposer version-literal edit | FrontComposer exact-pin or structure failures still block |
| Missing sibling required package | Selected catalog drops a presence-required package id | Semantics fail with owner + Builds coordinates | Fail closed |
| FrontComposer pin drift | `frontcomposer-catalog-v1` required package version differs from selected catalog | Semantics fail with the existing expected/found diagnostic | Fail closed |
| Graph error on push | `dependency-graph-diff.json` is `{ok:false,error:…}` | Collect and enforce steps print `error` via `::error::` | Job still fails closed |
| Builds gitlink ≠ execution SHA | Parent gitlink is not `99d5a46…` | `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` fails | Fail closed |

</frozen-after-approval>

## Code Map

- Artifact `/tmp/fc-ci-31783283241/dependency-graph-diff.json` and run 31784694140 — identical `System.CommandLine` `2.0.10` vs `2.0.11` on EventStore@`80d12ef` → Builds@`606d9f1`. `object-acquisition.json` was `ok: true`.
- `eng/dependency-graph-policy.json:85-142` — `eventstore-catalog-v1` / `memories-catalog-v1` / `parties-catalog-v1` still carry exact `selected_catalog_required_packages` (EventStore includes `System.CommandLine: 2.0.10`). Convert those three maps to presence-only names; leave `frontcomposer-catalog-v1` L68-83 exact pins unchanged.
- `eng/dependency_graph.py:1177-1197` `assert_authoritative_package_version`; `1328-1437` `evaluate_semantics` loops exact package versions before `diff_graphs`; `1785-1795` profile schema; `1905-1957` `diff` loads policy at event-base, evaluates base then candidate, and on `GraphError` emits `{ok:false,error}` with no `evidence`.
- `.github/workflows/ci.yml:115-131` summary prints `changes`/`affected` from missing `evidence` (always 0 on error); `225-235` enforce repeats only `exit=`. Print `payload.get("error")` in both places.
- `tests/eng/test_dependency_graph.py:1914` FrontComposer exact-pin regression must stay. Add sibling presence-only: version may move; missing id / local override still fail.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:35-54` Gate 2b `validate` consumer — must go green on current EventStore→Builds@`606d9f1` after the policy change.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:672-750` requires `git ls-tree HEAD references/Hexalith.Builds` == workflow `99d5a46…`. Current HEAD gitlink is `606d9f119965c273104d707b9cc8c179fe648237` (moved in `310a62c7` `/pushall`). Restore the parent pointer only.
- Read-only: `.github/workflows/release.yml` L17/233/286/321/329 and `release-evidence.yml` L233 stay on `99d5a46…`. Do not enable Builds `governed-ci` or retarget `quality.yml` `initialize-build@main` in this story.

## Tasks & Acceptance

**Execution:**
- [ ] Parent `references/Hexalith.Builds` gitlink -- restore recorded commit to `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38` without editing submodule files -- `/pushall` left catalog and execution SHA split.
- [ ] `eng/dependency-graph-policy.json` and `eng/dependency_graph.py` -- sibling profiles use presence-only required package names (plus existing no-local-override); FrontComposer exact pins and all other closed checks stay -- stops EventStore Dependabot from failing every FrontComposer push.
- [ ] `tests/eng/test_dependency_graph.py` -- prove sibling version movement passes and missing/override/FrontComposer pin regressions still fail.
- [ ] `.github/workflows/ci.yml` -- collect and enforce steps emit the JSON `error` field on `ok=false`.

**Acceptance Criteria:**
- Given EventStore's selected Builds catalog has `System.CommandLine` `2.0.11` and the package is still present without a local override, when `validate` or push `diff` runs, then the run is not rejected for a FrontComposer `2.0.10` literal.
- Given a sibling presence-required package is missing or locally overridden, when semantics run, then the gate fails with owner and Builds coordinates.
- Given a `frontcomposer-catalog-v1` required package version drifts, when semantics run, then the existing expected/found failure still blocks.
- Given `dependency-graph-diff.json` contains only `{ok:false,error}`, when the collect or enforce step runs, then the job log contains that `error` text and the job still fails.
- Given HEAD after the gitlink restore, when `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` runs, then the Builds gitlink equals every `99d5a46…` workflow coordinate.

## Spec Change Log

## Verification

**Commands:**
- `git ls-tree HEAD references/Hexalith.Builds` -- expected: `160000 commit 99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`
- `python3 -m unittest tests/eng/test_dependency_graph.py -v` -- expected: all pass, including new sibling presence cases
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds|FullyQualifiedName~CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate"` -- expected: both pass
