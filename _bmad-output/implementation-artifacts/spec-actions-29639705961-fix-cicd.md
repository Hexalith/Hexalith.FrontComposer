---
title: 'Fix CI: remediate AngleSharp NU1902 audit failure blocking Restore'
type: 'bugfix'
created: '2026-07-18'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '5c284c89d37dfc3d39593962631e376bd4c5e033'
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
- `references/Hexalith.Builds` (gitlink) -- bump from `e64ae34e50086ae55d47971d70897d579ff18c25` to `337f023` (`fix/anglesharp-nu1902-decoupled`), a cherry-pick of the AngleSharp pin onto the pre-bump base, not `Hexalith.Builds` `main` tip -- see Spec Change Log.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:36` -- existing test whose `expectedVersions` dict enumerates only the 14 migrated pairs (not a closed-catalog assertion); adding `AngleSharp` elsewhere in the shared file does not touch this test.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- add `<PackageVersion Include="AngleSharp" Version="1.5.2" />` with a one-line comment citing the advisory -- remediates CVE-2026-54570 at its source instead of masking it.
- [x] Commit (`fix: pin AngleSharp to 1.5.2 to remediate GHSA-pgww-w46g-26qg`) and push directly to `Hexalith.Builds` `main` -- makes the patched pin resolvable at a commit SHA (human-approved direct path for this repo).
- [x] `references/Hexalith.Builds` gitlink in FrontComposer -- bump to the new commit, on a new `fix/` branch -- pulls the patched catalog into FrontComposer's restore graph.
- [x] Open a PR from that branch targeting `main` -- respects FrontComposer's no-direct-commit-to-`main` rule. (PR #69, then corrected via follow-up PR #70 -- see Spec Change Log.)

**Acceptance Criteria:**
- Given `dotnet restore Hexalith.FrontComposer.slnx` on the fix branch, when Restore evaluates the three previously-failing projects, then it completes with no `NU1902` and resolves `AngleSharp` >= 1.5.0.
- Given the FrontComposer root `Directory.Packages.props`, when inspected after the change, then it still contains zero `PackageVersion` elements.
- Given the `Category=Governance` trait filter, when run after the change, then `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` still passes unmodified.
- Given the opened PR, when its CI run executes the `build-and-test` job, then `Restore` succeeds (previously red at that exact step in run 29639705961).

## Spec Change Log

- 2026-07-18: PR #69 merged (by human) pointing the gitlink at `Hexalith.Builds` `main` tip (`2542a64`), before a discovered decoupling correction could be pushed. That tip had, in the meantime, fast-forwarded past an unrelated in-flight EventStore 3.71.0 catalog bump not yet consistent with FrontComposer's own `EventStore`/`Tenants`/`Parties` submodule pins, breaking `main` CI with `NU1109`/`NU1102`. Opened follow-up PR #70 re-pointing the gitlink at `Hexalith.Builds@337f023` (the `AngleSharp` pin cherry-picked onto the pre-bump base, branch `fix/anglesharp-nu1902-decoupled`), verified clean (restore exit 0, Governance 167/167) before pushing. KEEP: the AngleSharp pin itself and the zero-local-PackageVersion root shim were correct from the start; only the gitlink target needed correction.

## Design Notes

`bunit` 2.8.4-preview declares `AngleSharp` as a minimum-version NuGet dependency (`>= 1.4.0`), and no newer `bunit` release exists to float it higher. With `CentralPackageTransitivePinningEnabled=true` and no explicit `AngleSharp` entry, NuGet resolves exactly to that floor — the vulnerable 1.4.0. Adding an explicit `PackageVersion` for `AngleSharp` is the standard CPM mechanism to override a transitive floor without touching `bunit` itself.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx` -- expected: completes with no `NU1902` diagnostics.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- expected: all Governance tests pass, including the central-package-version ownership test.
- `gh run view --repo Hexalith/Hexalith.FrontComposer <new-run-id>` on the opened PR -- expected: `build-and-test` Restore step green.

