---
title: 'Fix CI: remediate AngleSharp NU1902 audit failure blocking Restore'
type: 'bugfix'
created: '2026-07-18'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'afb39847f313b41266635149baafb602362f1e8e'
implementation_base: '5c284c89d37dfc3d39593962631e376bd4c5e033'
implementation_head: 'afb39847f313b41266635149baafb602362f1e8e'
review_ranges:
  frontcomposer: '5c284c89d37dfc3d39593962631e376bd4c5e033..afb39847f313b41266635149baafb602362f1e8e'
  builds: 'e64ae34e50086ae55d47971d70897d579ff18c25..337f02322b6eb9d78769b6003fad82d3ccb49488'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run [29639705961](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29639705961) fails at `Restore` with `NU1902` (warning-as-error): the transitively-resolved `AngleSharp` 1.4.0 — floored by `bunit` 2.8.4-preview's minimum dependency, no newer `bunit` exists — carries a known moderate XSS advisory ([GHSA-pgww-w46g-26qg](https://github.com/advisories/GHSA-pgww-w46g-26qg), fixed in AngleSharp 1.5.0). This blocks restore of `Hexalith.FrontComposer.Testing`, `...Testing.Tests`, and `...Shell.Tests`.

**Approach:** Pin `AngleSharp` to `1.5.2` (latest stable; compatible with `AngleSharp.Css`'s `[1.0.0,2.0.0)` range and `AngleSharp.Diffing`'s `>=1.1.2` floor) in the shared `Hexalith.Builds` package catalog — the only place FrontComposer's own governance now permits package-version pins — then bump FrontComposer's submodule gitlink to consume it. Human approved this cross-repo path over suppression.

## Boundaries & Constraints

**Always:** Remediate via a patched version pin, never suppression; keep FrontComposer's root `Directory.Packages.props` at zero local `PackageVersion` items (governance-enforced by `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog`); use Conventional Commits in both repos; land the FrontComposer-side gitlink bump on a `fix/` branch + PR, never commit directly to `main`.

**Ask First:** Anything beyond the single `AngleSharp` pin — other advisories, unrelated version bumps, other siblings' `Hexalith.Builds` gitlinks (EventStore/Memories/Parties), or reordering existing catalog entries.

**Never:** Use `NuGetAuditSuppress`, `NoWarn`, or `NuGetAudit=false` to hide the advisory; add a local `PackageVersion` item to FrontComposer's root import shim; touch nested submodules; modify the 14 already-migrated pairs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Restore after fix | `dotnet restore Hexalith.FrontComposer.slnx` on the bumped gitlink | AngleSharp resolves to 1.5.2; `Testing`, `Testing.Tests`, `Shell.Tests` restore cleanly | N/A |
| Governance re-check | `Category=Governance` test run after the change | Root shim still has zero `PackageVersion` items; migrated-pairs assertions unaffected | Test fails loudly if a local pin was mistakenly added to root |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` -- shared catalog; add the `AngleSharp` pin near the existing alphabetical `A`-block (after `AdaptiveCards.Templating`, before `AspNet.Security.OAuth.Amazon`).
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` -- format-only validator (blank/`v`-prefixed/malformed version, unresolved MSBuild expr); no ordering requirement, confirmed safe for the new entry.
- `Directory.Packages.props` (FrontComposer root) -- import shim; must remain untouched/empty of `PackageVersion` items.
- `references/Hexalith.Builds` (gitlink) -- bump from `e64ae34e50086ae55d47971d70897d579ff18c25` to the new commit containing the pin.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:36` -- existing test whose `expectedVersions` dict enumerates only the 14 migrated pairs (not a closed-catalog assertion); adding `AngleSharp` elsewhere in the shared file does not touch this test.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- add `<PackageVersion Include="AngleSharp" Version="1.5.2" />` with a one-line comment citing the advisory -- remediates CVE-2026-54570 at its source instead of masking it.
- [x] Commit (`fix: pin AngleSharp to 1.5.2 to remediate GHSA-pgww-w46g-26qg`) and push directly to `Hexalith.Builds` `main` -- makes the patched pin resolvable at a commit SHA (human-approved direct path for this repo).
- [x] `references/Hexalith.Builds` gitlink in FrontComposer -- bump to the new commit, on a new `fix/` branch -- pulls the patched catalog into FrontComposer's restore graph.
- [x] Open a PR from that branch targeting `main` -- respects FrontComposer's no-direct-commit-to-`main` rule.

**Acceptance Criteria:**
- Given `dotnet restore Hexalith.FrontComposer.slnx` on the fix branch, when Restore evaluates the three previously-failing projects, then it completes with no `NU1902` and resolves `AngleSharp` >= 1.5.0.
- Given the FrontComposer root `Directory.Packages.props`, when inspected after the change, then it still contains zero `PackageVersion` elements.
- Given the `Category=Governance` trait filter, when run after the change, then `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` still passes unmodified.
- Given the opened PR, when its CI run executes the `build-and-test` job, then `Restore` succeeds (previously red at that exact step in run 29639705961).

## Design Notes

`bunit` 2.8.4-preview declares `AngleSharp` as a minimum-version NuGet dependency (`>= 1.4.0`), and no newer `bunit` release exists to float it higher. With `CentralPackageTransitivePinningEnabled=true` and no explicit `AngleSharp` entry, NuGet resolves exactly to that floor — the vulnerable 1.4.0. Adding an explicit `PackageVersion` for `AngleSharp` is the standard CPM mechanism to override a transitive floor without touching `bunit` itself.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx` -- expected: completes with no `NU1902` diagnostics.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- expected: all Governance tests pass, including the central-package-version ownership test.
- `gh run view --repo Hexalith/Hexalith.FrontComposer 29641083386 --job 88071623493` on PR #70 -- expected: `build-and-test` Restore step and downstream gates green.

**Review bounds:**
- FrontComposer implementation range: `5c284c89d37dfc3d39593962631e376bd4c5e033..afb39847f313b41266635149baafb602362f1e8e`.
- Builds implementation range: `e64ae34e50086ae55d47971d70897d579ff18c25..337f02322b6eb9d78769b6003fad82d3ccb49488`.
- Local evidence environment: .NET SDK `10.0.302` on Ubuntu `26.04` under WSL2 (`linux-x64`), bound to FrontComposer HEAD `afb39847f313b41266635149baafb602362f1e8e` and the Builds range above.

**Local results (2026-07-18):**
- Merge proof: `git merge-base c00f487d^1 c00f487d^2` returned `5c284c89`; merge `c00f487d` retains parents `9417e69b` and `4bf40adc`, and `git merge-base --is-ancestor 4bf40adc main` exited `0`. The Builds gitlinks are base `e64ae34e`, ours `2542a648`, theirs/resolution `337f0232`; follow-up merge `afb39847` has `337f0232` on both parents and in its result.
- Builds-content proof: `2542a648` and `337f0232` have the same stable patch ID, `36ae36fe3aba53ef56591f043c51ad3ffde40fab`. `337f0232` is based directly on `e64ae34e`; its only diff is the advisory comment plus `<PackageVersion Include="AngleSharp" Version="1.5.2" />`. The locally recorded Builds `origin/main` contains both Builds histories. No fetch was run.
- `dotnet restore Hexalith.FrontComposer.slnx` -- failed before NuGet audit with `MSB3202` because the intentionally uninitialized nested Commons/EventStore/Memories/Tenants project-reference files are absent; the task forbids initializing them.
- `dotnet restore Hexalith.FrontComposer.slnx --property:Configuration=Release` -- passed for all solution projects with no `NU1902`. The restored assets for `Hexalith.FrontComposer.Testing`, `Hexalith.FrontComposer.Testing.Tests`, and `Hexalith.FrontComposer.Shell.Tests` each contain `AngleSharp/1.5.2`.
- Literal local Quality restore command from `.github/workflows/quality.yml:70`, `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true`, passed with no `NU1902`.
- Augmented local CI-parity restore, `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true --force-evaluate`, also passed and forced reevaluation of all solution projects. The shared-catalog validator passed all 267 evaluated `PackageVersion` entries.
- Root XML check -- `Directory.Packages.props` contains `0` `PackageVersion` elements.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- `164/167` passed. The absent nested submodule files fail `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` and `CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease`; the third failure, `CiGovernanceTests.SemanticReleaseAnalyzer_ConventionalCommitsMatrix_SelectsExpectedReleaseTypes`, is an unrelated commitlint assertion. The broad local Governance lane is therefore not green.
- Focused functional fallback, `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined&Category!=Governance&Category!=Contract"`, reached the functional suites: `Hexalith.FrontComposer.Shell.Tests` passed `2175` tests with one unrelated allocation failure, `FrontComposerHotPathLogTests.DisabledIdentifierEvent_AfterWarmup_AllocatesNothing`; `Hexalith.FrontComposer.Testing.Tests` passed `56` tests with two absent-nested-Builds failures, `CentralPackageVersion_Mismatch_ReportsExpectedAndActualBeforePackaging` and `CleanTemporaryConsumer_RestoresFromPackedNupkgs_WithoutRepoRelativeProjectReferences`.
- Matrix coverage fallback for the central-package ownership row -- reconstructed an isolated temporary repository fixture from FrontComposer HEAD's exact gitlinks: Builds `337f02322b6eb9d78769b6003fad82d3ccb49488`, EventStore `f180c5fdda59bf1914429bb369234fabf7ce33de`, Memories `93af830c533e0816507e498aedc1402f2e90a562`, and Parties `1e2ec0aaa7c2f1f7cb14ed53c5bb256d38e9c21`. Without initializing or modifying nested submodules, the unmodified test was invoked from the fixture with `dotnet test-bin/Hexalith.FrontComposer.Shell.Tests.dll -method "Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog"`: `1/1` passed, `0` failed, `0` skipped. This isolated result is fallback evidence only; the live full Gate 2b result below is authoritative.

**Live read-only evidence (2026-07-18):**
- FrontComposer [PR #70](https://github.com/Hexalith/Hexalith.FrontComposer/pull/70) targeted `main` from `fix/actions-29639705961-anglesharp-nu1902-followup` at `5fdb60fc509b635a223cc9fd45825952949526dd` and merged as `afb39847f313b41266635149baafb602362f1e8e`.
- PR CI [run 29641083386](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29641083386), job `88071623493`, succeeded through Restore, Build, consumer validation, and Tier-1 tests.
- Final `main` CI [run 29641217198](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29641217198), job `88071970786`, succeeded through the same Restore, Build, consumer-validation, and Tier-1 gates.
- Final `main` Quality [run 29641217096](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29641217096) passed `Gate 2b: Infrastructure governance and telemetry contracts`; the workflow failed later at unrelated Gate 2d documentation validation.
- Builds commit [`337f02322b6eb9d78769b6003fad82d3ccb49488`](https://github.com/Hexalith/Hexalith.Builds/commit/337f02322b6eb9d78769b6003fad82d3ccb49488) is an ancestor of live `main`; the live comparison reported `main` ten commits ahead and zero behind.

## Suggested Review Order

**Remediation path**

- Pin the patched transitive dependency once in the shared catalog.
  [`Directory.Packages.props:89`](../../../Hexalith.Builds/Props/Directory.Packages.props#L89)

- Preserve FrontComposer's import-only package catalog boundary.
  [`Directory.Packages.props:6`](../../Directory.Packages.props#L6)

**Verification trail**

- Confirm shared-catalog ownership remains enforced without local pins.
  [`InfrastructureGovernanceTests.cs:36`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L36)

- Review exact commits, fallback diagnostics, and authoritative live CI evidence.
  [`spec-actions-29639705961-fix-cicd.md:91`](spec-actions-29639705961-fix-cicd.md#L91)

**Live-CI verification (2026-07-18):**
- PR `#69` merged into `main` at `2026-07-18T10:26:04Z`. Its gating CI run ([29640829809](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29640829809)) still showed `Restore` red, but the failures were `NU1102`/`NU1109` on `Hexalith.Parties.UI`/`Hexalith.FrontComposer.UI`/`Hexalith.Parties.AdminPortal` (unrelated to `AngleSharp`) -- zero `NU1902`/`AngleSharp` diagnostics anywhere in that log.
- Latest `main` CI run ([29642386372](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29642386372), commit `4ee92501`): `Restore` step log confirmed **zero** `NU1902`/`AngleSharp` errors, and the three projects named in this spec's I/O matrix -- `Hexalith.FrontComposer.Testing`, `Hexalith.FrontComposer.Testing.Tests`, `Hexalith.FrontComposer.Shell.Tests` -- all restored successfully.
- This task's scoped remediation (`AngleSharp` -> `1.5.2`, zero `NU1902`) is therefore confirmed live in CI, and this spec's Acceptance Criteria are met at their stated scope.
- **Residual, out-of-scope finding:** the whole-solution `Restore` step is still red on `main` because of an unrelated `NU1102`: the centrally-pinned `Hexalith.Tenants.Client >= 3.15.1` is not published on nuget.org (only up to `3.2.16` exists), affecting `Hexalith.Parties.UI`, `Hexalith.FrontComposer.UI`, `Hexalith.Parties.AdminPortal`, and `Hexalith.Parties`. This appears introduced by the later, unrelated commit `4ee92501` ("update submodule references... to their latest commits"), which bumped the `Hexalith.Tenants` gitlink. Fixing it would require touching another sibling's `Hexalith.Builds`-managed version/gitlink, which this spec's frozen boundaries require asking first about -- flagged separately rather than actioned here.

