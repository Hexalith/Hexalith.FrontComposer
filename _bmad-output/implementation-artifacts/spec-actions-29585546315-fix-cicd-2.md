---
title: 'Verify CI package-boundary localization alignment'
type: 'bugfix'
created: '2026-07-17'
status: 'done'
review_loop_iteration: 0
baseline_commit: '30fd405a098377f8781c00aa197fbd05e7d6476c'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Tier 1 package-consumer test previously expected `Microsoft.Extensions.Localization.Abstractions` 10.0.9 even though the imported Hexalith.Builds central package catalog pins 10.0.10, causing GitHub Actions runs 29585546315 and 29592698672 to fail. The requested fix is already present in the checked-out history and must be confirmed without disturbing unrelated workspace changes.

**Approach:** Retain the reviewed 10.0.10 fixture constant and its explicit fail-closed assertions, then run the focused Release build and both package-boundary test paths to prove the clean consumer remains deterministic and drift is reported before packing.

## Boundaries & Constraints

**Always:** Keep the work self-contained to `PackageBoundaryTests.cs`; retain a fixed expected version rather than calculating it from the catalog; preserve offline restore, explicit consumer package references, restored-assets checks, Fluent v5 guards, no-network checks, and no-project-reference checks; use `DiffEngine_Disabled=true`; preserve all unrelated workspace changes.

**Ask First:** Any need to alter a root or Hexalith.Builds package version, a submodule pointer, workflow, package baseline, or scope beyond the fixture verification requires human approval.

**Never:** Modify `references/`; weaken or remove the central-version, package dependency, restored-assets, no-network, or no-project-reference assertions; add a network source; address the non-blocking missing `XPlat Code Coverage` collector warning.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Matching dependency governance | Imported catalog and fixture both pin localization abstractions 10.0.10 | The temporary consumer restores and builds using packed FrontComposer packages and the local NuGet fallback; restored assets contain 10.0.10 | Restore/build failure fails the test with captured process output |
| Future central-version drift | The catalog pin differs from the fixture's fixed expected value | The central-version assertion fails before packaging and includes expected and actual values | Do not synchronize the fixture dynamically |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- owns the 10.0.10 localization constant, central catalog assertion, generated offline consumer reference, assets assertion, and mismatch regression test.
- `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` -- `net10.0` Tier 1 test-project entry point for Release validation; no project change is intended.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- read-only imported source whose 10.0.10 pin the fixture must explicitly match.
- `.github/workflows/ci.yml` -- routes this project into the reusable Tier 1 VSTest lane; no workflow edit is intended.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- retain `LocalizationAbstractionsVersion` at 10.0.10 and all of its linked contract assertions; only correct it if the inspected source has unexpectedly regressed.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- retain and execute `CentralPackageVersion_Mismatch_ReportsExpectedAndActualBeforePackaging` so a future catalog drift fails with both versions visible.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` -- run focused Release build and executable-test validation without changing the project.

**Acceptance Criteria:**
- Given the imported central catalog pins `Microsoft.Extensions.Localization.Abstractions` 10.0.10, when the focused clean-consumer test runs, then its catalog assertion, offline restore, consumer build, and assets assertion pass.
- Given the full Tier 1 project is built in Release configuration, when its focused contract tests run, then the stale 10.0.9 localization failure is absent.
- Given the central catalog diverges in a future change, when the mismatch regression runs, then it reports the fixed expected version and differing actual version before any package is created.
- Given repository status and diff are reviewed after validation, when unrelated workspace changes are compared, then no workflow, package catalog, submodule, package baseline, or unrelated file has been changed by this work.

## Spec Change Log

## Design Notes

The fixed value deliberately flows through three independent checks: the imported catalog must match the reviewed contract, the generated consumer declares that version explicitly, and restored assets resolve it. A dynamic lookup would hide dependency-governance drift rather than detect it.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release -m:1` -- expected: Release build completes with no warnings or errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -method 'Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CentralPackageVersion_Mismatch_ReportsExpectedAndActualBeforePackaging'` -- expected: focused regression passes.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -method 'Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences'` -- expected: offline clean-consumer contract passes.
- `git diff --check` -- expected: no whitespace errors in work created for this task.

## Suggested Review Order

**Verification intent**

- Start with the constrained repair and its preserved package-boundary safeguards.
  [`spec-actions-29585546315-fix-cicd-2.md:15`](spec-actions-29585546315-fix-cicd-2.md#L15)

**Fixture contract**

- Confirm one fixed constant governs catalog, consumer, and restored-assets assertions.
  [`PackageBoundaryTests.cs:14`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L14)

- Review the focused regression that exposes expected-versus-actual catalog drift.
  [`PackageBoundaryTests.cs:65`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L65)

- Verify the offline consumer preserves no-network and no-project-reference boundaries.
  [`PackageBoundaryTests.cs:79`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L79)

**Review follow-ups**

- Inspect the three unrelated Hexalith.Builds verification gaps recorded without modifying its submodule.
  [`deferred-work.md:1752`](deferred-work.md#L1752)
