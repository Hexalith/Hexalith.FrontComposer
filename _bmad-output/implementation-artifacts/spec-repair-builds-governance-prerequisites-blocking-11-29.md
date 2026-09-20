---
title: 'Repair Builds Governance Prerequisites Blocking Story 11.29'
type: 'bugfix'
created: '2026-09-20'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-eventstore-builds-runtime-compatibility-successor.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-update-latest-submodules-and-package-versions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repository-wide governance is internally inconsistent after the EventStore successor merge and a later Builds pointer advance: active successor surfaces still select Builds `59862a00d72ef8c7b3e3be020fa967ebd89507a0`, root `HEAD` pins `410bd595f9e1c0f686e1edde7699517c47c8f126`, and FrontComposer policy expects Verify `33.1.0` although the selected Builds audit deliberately retains `33.0.2` pending compatibility validation.

**Approach:** Retain Builds `410bd595f9e1c0f686e1edde7699517c47c8f126` as the current successor target, align every active target surface to that existing root gitlink, restore the FrontComposer policy mirror to the Builds-owned Verify `33.0.2` catalog, and validate the exact committed-object governance path without changing Story 11.29.

## Boundaries & Constraints

**Always:** Treat the Builds catalog and its audit as package-version authority; preserve sealed EventStore identity v1-v3 bytes and historical approval tuples; keep the active packet at Builds `4f522a8caa62ad82584bdf56d54e16109b717b1c`; update only the root-declared Builds submodule if a pointer change is chosen; keep active workflow, validator, test, documentation, and policy values coherent.

**Never:** Weaken, skip, exclude, or baseline-waive either failing test; initialize nested submodules; use recursive or remote submodule updates; alter Story 11.29's scheduler, row signature, focused tests, frozen spec, or sprint status; rewrite historical evidence; update unrelated dependencies; stage, commit, or push without explicit authorization.

**Decision (2026-09-20):** Retain root gitlink `410bd595f9e1c0f686e1edde7699517c47c8f126`, which preserves the Builds catalog's FrontComposer `4.5.0` self-version, and supersede only the unsealed active successor target formerly set to `59862a00d72ef8c7b3e3be020fa967ebd89507a0`. Preserve the frozen predecessor spec as historical decision evidence. Create one scoped local prerequisite commit containing only this governance repair and its implementation record; do not push it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current successor validation | Checkout, workflow target, runtime validator, docs, and governance expectation select Builds `410bd595...` | Exact target validation and current-compatibility governance pass while sealed v1-v3 remain unchanged | Any coordinate drift fails closed with bounded diagnostics |
| Central catalog validation | Selected Builds catalog pins Verify `33.0.2` and the audit retains `33.1.0` as unapproved | FrontComposer semantic policy expects `33.0.2`; dependency-graph validation passes from committed blobs | Do not promote `33.1.0` without compatibility evidence |
| Uncommitted governance repair | Worktree differs from committed `HEAD` | Focused source-level checks may run, but committed-object acceptance remains red until an authorized commit exists | Report the commit-bound result; do not bypass `HEAD` validation |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds` -- root gitlink and selected catalog; `410bd595...` descends from `59862a00...`, retains Verify `33.0.2`, and advances the FrontComposer family to `4.5.0`.
- `eng/dependency-graph-policy.json` -- FrontComposer-owned semantic mirror; its Verify pair is the stale `33.1.0` merge result.
- `tests/Directory.Build.props` -- owner-approved SponsorCheck configuration; its explanatory comment still labels the retained package family as Verify `33.1`.
- `eng/eventstore_runtime_evidence.py` -- active successor Builds target used by exact-tuple validation.
- `.github/workflows/quality.yml` -- hosted successor target and validator arguments.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- current-versus-historical identity guard, including exact `HEAD` gitlink validation.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- invokes dependency validation against committed `HEAD`; do not weaken this boundary.
- `docs/reference/pact-contracts.md` -- operator-facing current successor coordinates.
- `_bmad-output/implementation-artifacts/spec-11-29-fallback-refresh-and-view-registration-correctness.md` -- protected prerequisite consumer; read-only and outside this change.

## Tasks & Acceptance

**Execution:**
- [ ] `eng/eventstore_runtime_evidence.py`, `.github/workflows/quality.yml`, `CiGovernanceTests.cs`, and `docs/reference/pact-contracts.md` -- replace the obsolete active successor target with the existing `410bd595...` gitlink while preserving historical tuples and the root submodule pointer.
- [ ] `eng/dependency-graph-policy.json` and `tests/Directory.Build.props` -- restore Verify and Verify.XunitV3 expectations to Builds-owned `33.0.2` and correct the adjacent active explanatory evidence.
- [ ] Repository history -- create only the authorized local prerequisite commit needed for committed-object governance; do not include Story 11.29 or unrelated paths, and do not push.

**Acceptance Criteria:**
- Given the ratified Builds revision and its catalog, when both named governance facts run, then they pass for exact current-provenance and catalog-authority reasons without exclusions.
- Given the completed prerequisite, when the applicable default Shell lane runs, then every selected test passes with zero skips.
- Given the repaired repository, when the required Release solution build and whitespace check run, then the build has zero warnings/errors and `git diff --check` is clean.
- Given immutable historical EventStore evidence and protected Story 11.29 files, when the final diff is inspected, then neither surface changed.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: Shell test assembly builds with zero warnings/errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -noLogo -noColor -parallel none -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval -method Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` -- expected: 2 passed, 0 failed, 0 skipped against the committed repair.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -noLogo -noColor -parallel none -notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined` -- expected: complete applicable default Shell lane passes with no skips.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors.
