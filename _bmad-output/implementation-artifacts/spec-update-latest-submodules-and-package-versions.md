---
title: 'Update Root Submodules and FrontComposer Package Versions'
type: 'chore'
created: '2026-09-16'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer's eight root submodules need a fresh remote comparison, while its directly consumed NuGet, Aspire, .NET SDK, dotnet-tool, and npm versions trail current published releases. Leaving these surfaces split creates different Debug/source and Release/package dependency graphs.

**Approach:** Confirm each root gitlink against its freshly fetched `origin/main`, then advance FrontComposer-owned dependency surfaces to the latest published compatible channel, accepting requested major upgrades and fixing resulting compatibility issues. Keep NuGet authority in Hexalith.Builds, align all required mirrors and locks, and validate both Release/package and Debug/source modes.

## Boundaries & Constraints

**Always:** Update only the eight root-declared submodules; preserve uninitialized nested submodules. Treat FrontComposer's direct dependency set as scope: .NET SDK `10.0.401`, Aspire `13.5.4`, EventStore `3.106.0`, Microsoft.NET.Test.Sdk `18.10.1`, Verify `33.0.2`, dotnet-stryker `5.0.0`, Playwright `1.63.0`, and `@types/node` `26.6.1`. Keep manifest/lock pairs and exact governance mirrors aligned. Use the Builds catalog/audit/exception workflow and preserve its BOM/CRLF. Start and inspect Aspire before edits and revalidate it after AppHost-affecting changes.

**Never:** Initialize nested submodules; use recursive or remote submodule updates; add root or project-local NuGet version overrides; downgrade Fluent UI v5 RC or other intentional prerelease tracks to older stable majors; update shared-catalog packages unused by FrontComposer; rewrite sealed/historical runtime identity evidence; move Builds CI/release execution SHAs merely because the catalog gitlink changes; commit, push, publish, or clean user work without separate authorization.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Root submodules | Freshly fetched root worktrees equal `origin/main` | Gitlinks remain unchanged and all eight roots stay clean | Halt on dirty, divergent, or non-fast-forward movement |
| Direct packages | Registry reports newer stable or selected-channel releases | Catalog, manifests, locks, exceptions, and mirrors agree on accepted versions | Retain only with concrete incompatibility evidence and record it |
| Shared catalog | Unused packages also have newer versions | Leave them unchanged | Do not expand into unrelated ecosystem consumers |
| Major update | Verify 33 or Stryker 5 breaks a consumer/configuration | Apply focused compatibility fixes and tests | Report an irreducible upstream incompatibility instead of weakening gates |

</frozen-after-approval>

## Code Map

- `.gitmodules`, `references/*` -- eight allowed root submodules; remote comparison found every gitlink clean and equal to `origin/main`.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- sole NuGet version authority; update Aspire, EventStore, test SDK, and Verify families here.
- `references/Hexalith.Builds/Tools/package-version-{audit,exceptions}.json` and `Tools/*package-version*.ps1` -- official freshness provenance, non-CPM pins, structural validators, and fixture suites.
- `global.json`, `.config/dotnet-tools.json`, `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` -- .NET SDK, Stryker, and AppHost SDK pins.
- `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- active SDK/Aspire CLI mirrors; leave reusable Builds execution coordinates alone.
- `eng/dependency-graph-policy.json`, `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- exact Verify and test-SDK catalog expectations.
- `package.json` plus `tests/e2e/package.json` and `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package.json` with their locks -- npm authority; root is already current, two test workspaces need Playwright and Node types.
- `_bmad-output/project-context.md` and current project docs -- update active version statements only; retain dated evidence and immutable runtime identity packets.

## Tasks & Acceptance

**Execution:**
- [ ] Root submodules -- re-confirm fetched tips and preserve current gitlinks because all eight already match `origin/main`.
- [ ] `references/Hexalith.Builds` -- update scoped catalog families and exception inventory, commit through the required human/authorized boundary, regenerate the official audit, and run all catalog/audit/exception validators.
- [ ] Root SDK, AppHost, workflow, test, policy, tool, documentation, and npm surfaces -- align exact mirrors and lockfiles; remediate major-version compatibility without broadening scope.
- [ ] Release/package and Debug/source graphs -- restore, build, test, and prove that selected package versions and project references resolve as intended.

**Acceptance Criteria:**
- Given freshly fetched remotes, when root submodule state is checked, then all eight gitlinks equal `origin/main`, all roots are clean, and nested submodules remain uninitialized.
- Given current official registries, when dependency validation runs, then every scoped FrontComposer dependency resolves to the version named above with no local NuGet override or stale exact mirror.
- Given the updated AppHost and test tooling, when Release/package, Debug/source, npm typecheck/runner, governance, and package-audit checks run, then they pass without warnings or weakened gates.
- Given catalog-only Builds movement, when repository governance is inspected, then reusable CI/release execution SHAs and sealed runtime identity evidence remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- Builds package-version validator and fixture suites -- expected: catalog, audit, exceptions, family alignment, and downgrade guards pass.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:UseNuGetDeps=true -p:EnableFrontComposerPackageValidation=true` then Release build -- expected: warning-free package graph.
- Debug AppHost build with `UseHexalithProjectReferences=true` plus focused governance/package-boundary tests -- expected: source graph and exact mirrors pass.
- `npm ci` in all three workspaces, E2E typecheck, legacy runner, and relevant Playwright lane -- expected: updated locks and browser tooling pass.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py` -- expected: governance remains green.
- `git diff --check` and root/nested submodule status -- expected: clean formatting and only intended root-level submodule state.
