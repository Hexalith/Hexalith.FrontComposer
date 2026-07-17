---
title: 'Fix CI package-boundary localization version drift'
type: 'bugfix'
created: '2026-07-17'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'd739f9b5249017b8959e14a445bc3eaaebf683c0'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run 29585546315, and the follow-up run 29592698672 on current `main`, fail the Tier 1 VSTest lane because the clean package-consumer test expects `Microsoft.Extensions.Localization.Abstractions` 10.0.9 while the imported Hexalith.Builds central package catalog now pins 10.0.10. Restore, Release build, package-consumer validation, and every other executed Tier 1 test pass.

**Approach:** Update the clean-consumer fixture's single localization-abstractions version constant to 10.0.10. Preserve its exact central-version assertion, explicit offline package reference, restored-assets assertion, and no-network/no-project-reference checks so future dependency drift remains visible.

## Boundaries & Constraints

**Always:** Keep the change self-contained in the failing package-boundary test; retain the fixed expected-version contract rather than deriving it dynamically; preserve deterministic offline restore and the existing Fluent v5 package guards; run with `DiffEngine_Disabled=true`; preserve unrelated workspace changes.

**Ask First:** Any need to change a root or Hexalith.Builds package version, a submodule pointer, a workflow, a package baseline, or the scope beyond the blocking failure requires human approval.

**Never:** Modify files under `references/`; weaken or remove the central-version, package dependency, restored-assets, no-network, or no-project-reference assertions; add a network source; address the non-blocking missing `XPlat Code Coverage` collector warning as part of this fix.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Matching dependency governance | Shared catalog and fixture both pin localization abstractions 10.0.10 | The temporary consumer restores and builds only from packed FrontComposer packages plus the local NuGet fallback, and its assets contain 10.0.10 | Any restore/build failure fails the test with captured process output |
| Future central-version drift | Shared catalog no longer matches the fixed fixture expectation | The explicit central-version assertion fails before packaging | Report expected and actual versions; do not silently synchronize at runtime |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- owns the stale `LocalizationAbstractionsVersion` constant and uses it for the central catalog assertion, offline consumer package reference, and restored-assets assertion.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- read-only source of the imported 10.0.10 central pin; no submodule edit is allowed.
- `.github/workflows/ci.yml` -- supplies `Hexalith.FrontComposer.Testing.Tests` to the reusable Tier 1 VSTest lane; no workflow edit is required.
- `_bmad-output/implementation-artifacts/spec-actions-29182697666-fix-cicd.md` -- continuity record for the deterministic offline clean-consumer contract that must remain preserved.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- change `LocalizationAbstractionsVersion` from 10.0.9 to 10.0.10 so all three linked assertions match the imported catalog while retaining the fail-closed fixture design.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- add a focused mismatch regression test for the existing central-version assertion so the future-drift matrix row is exercised without weakening the fixed-version contract.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` -- perform focused Release build/test validation without changing the project.

**Acceptance Criteria:**
- Given the imported central package catalog pins `Microsoft.Extensions.Localization.Abstractions` 10.0.10, when the focused clean-consumer test runs, then its catalog assertion, offline package restore, consumer build, and assets assertion all pass.
- Given the full `Hexalith.FrontComposer.Testing.Tests` Tier 1 project runs in Release configuration, when the CI-equivalent VSTest command executes, then all 58 tests pass and the stale localization-version failure is absent.
- Given a future central catalog version differs from the fixed fixture expectation, when the clean-consumer test runs, then it fails with the expected-versus-actual central-version mismatch rather than masking drift.
- Given the completed change is inspected, when repository status and diff are reviewed, then no workflow, root package catalog, submodule content or pointer, package baseline, or unrelated file was modified by this task.

## Spec Change Log

## Design Notes

The constant intentionally feeds three checks: the imported central catalog must equal the reviewed version, the generated consumer requests that same version explicitly, and `project.assets.json` must resolve it. Updating the one constant repairs the dependency-governance fixture without converting a pinned compatibility contract into a self-fulfilling dynamic check.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release -m:1` -- expected: Release build succeeds with no warnings or errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -method 'Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences'` -- expected: the focused offline package-consumer contract passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --no-build --configuration Release --logger 'trx;LogFileName=Hexalith.FrontComposer.Testing.Tests.trx' --results-directory TestResults/Hexalith.FrontComposer.Testing.Tests --collect:'XPlat Code Coverage'` -- expected: all 58 project tests pass; the pre-existing missing collector diagnostic may remain non-blocking.
- `git diff --check` -- expected: no whitespace errors.

## Suggested Review Order

- Aligns the reviewed fixture with the imported central package version.
  [`PackageBoundaryTests.cs:14`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L14)

- Proves future catalog drift reports both expected and actual versions before packaging.
  [`PackageBoundaryTests.cs:64`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L64)

- Preserves offline packing, restore, build, and resolved-assets verification.
  [`PackageBoundaryTests.cs:79`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L79)
