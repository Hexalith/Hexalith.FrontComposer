---
title: 'fix CI restore and publish the FrontComposer release'
type: 'bugfix'
created: '2026-10-18'
status: 'in-progress'
route: 'dispatch'
baseline_commit: 'c4f5c12e32182739ce1478a6cdefe602c4daeb18'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Push CI on main fails during `dotnet restore` of `Hexalith.FrontComposer.slnx` because `Hexalith.Commons.Aspire` promotes NuGet audit warning NU1902 to an error for transitive `MessagePack` 2.5.302 ([GHSA-qhrr-8q5h-9q3h](https://github.com/advisories/GHSA-qhrr-8q5h-9q3h)). Release cannot start until that exact main SHA has a successful `ci.yml` run, so the eight FrontComposer packages are not published.

**Approach:** Lift that project's MessagePack dependency to the patched 2.5.303 release, push the owning Commons commit and the FrontComposer submodule pointer, wait for green push CI, dispatch Release from that main SHA, approve the production environment as the required reviewer, and confirm the eight packages exist on NuGet.org.

## Boundaries & Constraints

**Always:** Keep the audit enabled. Stay on MessagePack 2.5.x so Aspire.Hosting 13.6.1's 2.5.302 minimum stays binary-compatible. Commit in `Hexalith.Commons` first, then move only the FrontComposer gitlink. Release must be `workflow_dispatch` from `refs/heads/main` after `ci.yml` succeeds for that SHA. Approve the `production` environment through the GitHub API; `prevent_self_review` is false and the authenticated user is the required reviewer.

**Never:** Do not suppress NU1902, disable `NuGetAudit`, or pass `WarningsNotAsErrors` to hide 2.5.302. Do not retarget MessagePack to the central 3.1.12 pin. Do not change Quality's browser or accessibility failures; they are outside the `ci.yml` gate this release uses. Do not initialize nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Restore | `Hexalith.Commons.Aspire` with Aspire.Hosting 13.6.1 | Resolved MessagePack is 2.5.303 or newer on the 2.5 line, and restore exits 0 | A missing 2.5.303 package or a new NU1902 stops the change |
| Release gate | Pushed main SHA | `ci.yml` push run for that SHA succeeds, then Release is dispatched | Do not dispatch while CI is red or main has moved |
| Publication | Release `prepare-candidate` and `publish` waiting on `production` | Reviewer approval lets publish continue | If approval is rejected, stop and report the run URL |
| NuGet check | Eight package ids at the Semantic Release version | Each id/version is listed on NuGet.org | Report any id that is absent |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Directory.Build.props` — nearest props for this project. Sets `TreatWarningsAsErrors` and does not import shared Hexalith props, so `WarningsNotAsErrors` for NU1901–NU1904 never applies. Do not broaden this file to import shared props.
- `references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj` — direct references are `Aspire.Hosting` and `CommunityToolkit.Aspire.Hosting.Dapr` only. This is the file to change.
- `references/Hexalith.Builds/Props/Directory.Packages.props` line 203 — central `MessagePack` is already 3.1.12. Commons package management does not enable transitive pinning, so Aspire.Hosting still restores 2.5.302. Do not change the central pin.
- `~/.nuget/packages/aspire.hosting/13.6.1/aspire.hosting.nuspec` — dependency `MessagePack` minimum `2.5.302`.
- `.github/workflows/ci.yml` — calls `Hexalith.Builds` `domain-ci.yml`; restore is plain `dotnet restore`. Do not edit the reusable workflow.
- `.github/workflows/release.yml` — `workflow_dispatch` only; `verify-source` requires `refs/heads/main` and a successful push `ci.yml` run; `prepare-candidate` and publish use environment `production`.
- `.github/workflows/release-publish.yml` — publishes when `vars.HEXALITH_RELEASE_PUBLISH_ENABLED` is true. Do not change publication flags.
- `tools/release-packages.json` — the eight ids: Cli, Contracts, Contracts.UI, Mcp, Schema, Shell, SourceTools, Testing.
- Failed evidence: CI run `39123696668` and the later same-SHA-family failure `39132846856` (`ci / build-and-test`, NU1902 on `Hexalith.Commons.Aspire.csproj`).

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj` -- add a direct `MessagePack` reference overridden to `2.5.303` -- Aspire.Hosting's minimum is vulnerable and this project does not transitively pin
- [x] Commons repository -- commit, validate with that repo's commitlint, and push to its `main` -- FrontComposer CI checks out the recorded submodule commit
- [x] FrontComposer gitlink `references/Hexalith.Commons` -- point at that commit, commit, validate with FrontComposer commitlint, and push `main` -- Release dispatches only from current main
- [x] GitHub Actions `ci.yml` -- wait until the push run for that exact SHA succeeds -- Release rejects a red or missing CI handoff
- [x] Release workflow -- `workflow_dispatch` on that SHA, approve the pending `production` deployments, and query NuGet.org for the eight package ids at the released version -- this is the publication check

**Acceptance Criteria:**
- Given Aspire.Hosting 13.6.1, when `Hexalith.Commons.Aspire` restores, then MessagePack resolves to 2.5.303 and NU1902 is absent.
- Given the pushed main SHA, when `ci.yml` completes, then `ci / build-and-test` succeeds.
- Given that green CI run, when Release is dispatched and the production environment is approved, then the eight FrontComposer packages for the Semantic Release version are present on NuGet.org.

## Implementation Notes

- 2026-10-18: Main had already moved to `c4f5c12e32182739ce1478a6cdefe602c4daeb18`. Commons `5c23c818` already references central MessagePack 3.1.12, which is past the GHSA-qhrr-8q5h-9q3h fix (3.1.9). Restore no longer reports NU1902. The live `ci.yml` failure is ApiCompat CP0002 against Shell 4.5.0, so the MessagePack 2.5.303 retarget was not applied.
- Restored the published six-argument constructors on `ProjectionFallbackRefreshScheduler` and `LoadPageEffects`, and registered `LoadPageEffects` with an explicit factory so optional localization does not drop user-context validation.
- `dotnet pack` of the eight release projects with `FrontComposerPackageValidationBaselineVersion=4.5.0` exited 0. `ShellConstructorCompatibilityTests` passed 3/3.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet restore references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj` -- expected: exit 0 and no NU1902; assets show MessagePack 2.5.303
- `gh run view` for the push `ci.yml` run -- expected: `ci / build-and-test` success
- `gh run list --workflow release.yml` and NuGet.org registration for the eight ids -- expected: Release success and each package version listed
