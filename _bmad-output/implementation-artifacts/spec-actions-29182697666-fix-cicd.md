---
title: 'Fix CI run 29182697666 package consumers'
type: 'bugfix'
created: '2026-07-12'
status: 'done'
review_loop_iteration: 0
baseline_commit: '619e135ec85cc7971f667010de2c9b25bb524432'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions CI run `29182697666` fails both commitlint and Gate 3a. The pushed `main` commit omits the required space after `feat:`, the offline Testing-package consumer treats a fallback localization version as `NU1603`, and the SourceTools package consumer resolves forbidden Fluent UI v4 assets that fail with `BLAZOR106`.

**Approach:** Keep commitlint strict and repair the package-consumer fixtures by explicitly selecting the repository's governed dependency versions. Both clean consumers must resolve Blazor Fluent UI v5, reject any v4 component or icon graph, and the offline consumer must select the available localization abstraction version without restoring from the network.

## Boundaries & Constraints

**Always:** Preserve the offline/no-repository-reference purpose of the Testing consumer; pin `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` to `5.0.0-rc.4-26180.1`; pin `Microsoft.Extensions.Localization.Abstractions` to `10.0.9` where the offline bUnit consumer needs deterministic resolution; inspect `project.assets.json` to prove v5 is present and v4 is absent; use a valid Conventional Commit subject with a space after the colon when this repair is delivered.

**Ask First:** Changing any repository-wide dependency version, allowing network restore in the offline consumer, changing commitlint/workflow policy, amending published `main` history, or including existing submodule pointer changes in the repair.

**Never:** Use or tolerate Fluent UI v4; weaken `TreatWarningsAsErrors`, `NU1603`, commitlint, or the package-boundary assertions; initialize nested submodules; modify files inside existing submodules; fold the broader REL-2 CI/CD redesign into this focused repair.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Offline Testing consumer | bUnit requests localization `>=10.0.0`; fallback contains governed `10.0.9` | Restore selects `10.0.9`, then package-only build succeeds without HTTP sources | Any `NU1603`, network source, or repo-relative project reference fails the test |
| SourceTools package consumer | Transitive graph could select Fluent UI `4.14.3` | Direct v5 component and icon references win; generated Razor consumer builds | Any v4 component/icon entry or `BLAZOR106` fails the test |
| Published malformed commit | Current `main` HEAD is `feat:Update...` | Commitlint configuration remains unchanged; the repair is delivered with a valid conventional subject so the latest-commit check passes | Do not relax rules or rewrite published history |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- Builds the offline package-only Testing consumer; currently lacks an explicit localization abstraction pin and complete component/icon asset assertions.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- Builds the generated Razor package consumer; currently lacks explicit Fluent v5 references and restored-graph assertions.
- `commitlint.config.mjs` -- Existing strict Conventional Commit policy; evidence-only, no change expected.
- `.github/workflows/ci.yml` -- Runs `commitlint --last` and the solution default lane; evidence-only, no change expected.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- add the governed localization abstraction reference and assert exact v5 component/icon assets with no v4 entries -- remove cache-dependent `NU1603` while closing both halves of the Fluent package-family guard.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- explicitly reference both Fluent UI v5 packages and assert v5 component/icon assets with no v4 entries -- make the generated consumer deterministic and enforce the user's v5-only rule.
- [x] `commitlint.config.mjs` and `.github/workflows/ci.yml` -- verify they remain unchanged and validate a compliant repair subject -- preserve the existing CI quality gate.

**Acceptance Criteria:**
- Given the two affected test assemblies are built in Release, when their failing package-consumer methods run, then both pass against isolated temporary consumers.
- Given the SourceTools consumer assets file, when package resolution is inspected, then both Fluent component and icon packages are `5.0.0-rc.4-26180.1` and neither package has a `4.*` entry.
- Given the Testing consumer uses only its local package source and global fallback, when restore runs with warnings as errors, then localization abstractions resolve to `10.0.9` without `NU1603`.
- Given the repair's proposed commit subject, when commitlint evaluates it, then it passes without relaxing repository rules.

## Spec Change Log

## Design Notes

The direct package references are deliberate test-fixture constraints, not product dependency changes. Commit `1de43942` established the same v5 pin-and-assets-assertion pattern for the Testing clean consumer after an equivalent CI-only `BLAZOR106` failure; this repair applies that precedent to the SourceTools consumer and extends deterministic offline resolution for bUnit's localization dependency.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: solution builds with warnings as errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -method Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests.CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences` -- expected: one test passes.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -method Hexalith.FrontComposer.SourceTools.Tests.Integration.PackagedAnalyzerConsumerTests.PackagedAnalyzer_ContractsOnlyPayload_GeneratedShellConsumerCompiles` -- expected: one test passes with no v4 assets.
- `printf '%s\n' 'fix(ci): pin clean consumers to fluent v5' | npx commitlint --verbose` -- expected: zero problems and zero warnings.

**Observed results (2026-07-12):**
- Release solution build passed with 0 warnings and 0 errors.
- Both focused package-consumer methods passed, 1/1 each.
- The exact Gate 3a solution command passed all 3,986 selected tests; the benchmark assembly correctly had no matching default-lane tests.
- The proposed Conventional Commit subject passed with 0 problems and 0 warnings; commitlint configuration and CI workflow remained unchanged.

## Suggested Review Order

**Fluent v5 resolution**

- Pin both v5 packages at the failing SourceTools consumer entry point.
  [`PackagedAnalyzerConsumerTests.cs:83`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs#L83)

- Reject any v4 component or icon resolution in the restored graph.
  [`PackagedAnalyzerConsumerTests.cs:133`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs#L133)

**Package metadata and central governance**

- Prove packed Contracts.UI and Shell dependencies remain exact Fluent v5.
  [`PackagedAnalyzerConsumerTests.cs:31`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs#L31)

- Bind fixture constants independently to centrally governed package versions.
  [`PackagedAnalyzerConsumerTests.cs:163`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs#L163)

**Offline Testing consumer**

- Select localization 10.0.9 explicitly without weakening offline restore.
  [`PackageBoundaryTests.cs:102`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L102)

- Verify package metadata, v5 assets, localization, and no-v4 exclusivity.
  [`PackageBoundaryTests.cs:82`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L82)

**Deferred pre-existing cleanup**

- Track effective NuGet cache discovery and temporary-directory cleanup separately.
  [`deferred-work.md:1483`](deferred-work.md#L1483)
