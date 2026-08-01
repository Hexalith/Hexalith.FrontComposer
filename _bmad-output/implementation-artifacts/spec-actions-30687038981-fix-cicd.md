---
title: 'Fix CI Testing package consumer Test SDK drift'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'c413f12f158356598ce153e295a3b54e21a9d982'
context: ['{project-root}/_bmad-output/planning-artifacts/architecture.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `30687038981` fails its only blocking job because the offline Testing-package consumer fixture requests `Microsoft.NET.Test.Sdk` `18.7.0`, while the selected Hexalith.Builds catalog and fresh runner cache contain `18.8.1`. NuGet raises `NU1603`, which the fixture correctly promotes to an error through `TreatWarningsAsErrors`.

**Approach:** Align the fixture's explicit Test SDK pin with the selected shared catalog and add the same pre-packaging catalog assertion already used for its Fluent and localization pins. Keep the consumer offline and preserve the realistic test-project dependency surface.

## Boundaries & Constraints

**Always:** Change only the FrontComposer-owned package-boundary test. Preserve the local packed-package source, global fallback cache, `TreatWarningsAsErrors`, `--no-http-cache`, and explicit Test SDK reference. Represent `18.8.1` once as a named constant, verify it against the selected shared catalog before packaging, and use it in the generated consumer project.

**Ask First:** Changing a root submodule pointer, modifying `references/Hexalith.Builds`, changing shared package versions, removing or adding consumer dependencies, or broadening the work beyond run `30687038981` requires approval.

**Never:** Do not suppress or ignore `NU1603`, add an HTTP package source, dynamically accept any catalog value, edit production Testing-package metadata, remove the Test SDK reference, weaken warnings-as-errors, or modify files inside `references/`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Selected catalog matches fixture | Shared catalog pins Test SDK `18.8.1`; fresh fallback cache contains that restored version | Offline consumer restores and builds against the exact pin | Any restore/build failure remains blocking with captured stdout/stderr |
| Future catalog drift | Shared catalog Test SDK version differs from the fixture constant | Test fails before packaging with expected and actual versions | Do not fall through to an opaque `NU1603` restore failure |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- owns fixed consumer dependency pins, shared-catalog assertions, offline package restore, and downstream build verification.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- read-only source of truth currently selecting `Microsoft.NET.Test.Sdk` `18.8.1`.
- `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` -- demonstrates the repository test project's versionless centrally managed Test SDK reference.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- add a named `Microsoft.NET.Test.Sdk` `18.8.1` constant, validate it through `AssertCentralPackageVersion`, and interpolate it into the clean consumer fixture so its offline restore uses the selected catalog generation and future drift fails explicitly.

**Acceptance Criteria:**
- Given the catalog selected by the current Builds gitlink, when the focused package-boundary test runs on a fresh-cache-equivalent setup, then the clean consumer restores and builds without `NU1603` or network package sources.
- Given a future Test SDK catalog change without a matching fixture update, when the test starts, then the catalog assertion reports expected and actual versions before package creation or consumer restore.
- Given the repair, when the Testing test project builds in Release and both focused PackageBoundaryTests execute, then warnings-as-errors remains enabled and all checks pass.

## Spec Change Log

## Design Notes

The fixed constant is intentional: Fluent, localization, and Test SDK versions form a reviewed clean-consumer fixture. An early catalog assertion preserves drift detection while replacing the current indirect restore warning with an actionable mismatch. The shared catalog and its submodule remain untouched.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release -m:1 /nr:false` -- expected: clean warnings-as-errors build.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -method 'Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CentralPackageVersion_Mismatch_ReportsExpectedAndActualBeforePackaging'` -- expected: pass and retain explicit mismatch diagnostics.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -method 'Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences'` -- expected: pass with offline restore and build.

## Suggested Review Order

**Catalog contract**

- Fail before packaging when the clean-consumer pin drifts from the selected catalog.
  [`PackageBoundaryTests.cs:88`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L88)

- Emit the offline consumer with the single reviewed Test SDK version.
  [`PackageBoundaryTests.cs:125`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L125)

**Regression evidence**

- Exercise expected/actual diagnostics for localization and Test SDK mismatches.
  [`PackageBoundaryTests.cs:65`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L65)

- Prove restore resolved the exact selected Test SDK package version.
  [`PackageBoundaryTests.cs:189`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L189)
