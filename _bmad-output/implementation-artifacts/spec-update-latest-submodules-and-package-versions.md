---
title: 'Update Root Submodules and FrontComposer Package Versions'
type: 'chore'
created: '2026-09-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1e9348e45f4545192e5b74b44d011fc3a9ba4e61'
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
- [x] Root submodules -- re-confirm fetched tips, retain unchanged roots, advance Builds for the scoped catalog update, and reconcile EventStore after its cached `origin/main` advanced during verification.
- [x] `references/Hexalith.Builds` -- update scoped catalog families and exception inventory, commit through the required human/authorized boundary, regenerate the official audit, and run all catalog/audit/exception validators.
- [x] Root SDK, AppHost, workflow, test, policy, tool, documentation, and npm surfaces -- align exact mirrors and lockfiles; remediate major-version compatibility without broadening scope.
- [x] Release/package and Debug/source graphs -- restore, build, test, and prove that selected package versions and project references resolve as intended.

**Acceptance Criteria:**
- Given freshly fetched remotes, when root submodule state is checked, then all eight gitlinks equal `origin/main`, all roots are clean, and nested submodules remain uninitialized.
- Given current official registries, when dependency validation runs, then every scoped FrontComposer dependency resolves to the version named above with no local NuGet override or stale exact mirror.
- Given the updated AppHost and test tooling, when Release/package, Debug/source, npm typecheck/runner, governance, and package-audit checks run, then they pass without warnings or weakened gates.
- Given catalog-only Builds movement, when repository governance is inspected, then reusable CI/release execution SHAs and sealed runtime identity evidence remain unchanged.

## Implementation Notes

- Updated the scoped Builds catalog and exception inventory to Aspire `13.5.4`, preview integrations `13.5.4-preview.1.26464.4`, Microsoft.NET.Test.Sdk `18.10.1`, Verify `33.0.2`, and FrontComposer's dotnet-stryker exception `5.0.0`; regenerated the official incremental audit.
- Updated FrontComposer's .NET SDK, AppHost SDK, Aspire CLI, governance mirrors, package policy, active documentation, npm manifests, and lockfiles. Playwright is `1.63.0` and `@types/node` is `26.6.1`.
- Verify `33.0.2` introduced SponsorCheck owner-mode enforcement. The owner-approved supported pair `Verify_GitHubSponsorAccount=jpiquot` and `Verify_SponsorshipStart=2026-09-17` is applied through `tests/Directory.Build.props`; the approved five-year term ending `2031-09-17` is recorded separately as repository metadata because SponsorCheck's licensed-until mode is mutually exclusive with sponsor accounts and capped at one year. A governance regression test rejects a conflicting licensed-until assertion or a bypass.
- Updated the legacy Story 2.2 Playwright harness for the FluentAccordion DOM, interactive-shell and document-title readiness, exact Story 2.2 Axe surfaces, unsafe-return-path coverage, fail-closed runtime/evidence handling, standalone Release builds, and portable isolated evidence. The blocking `accessibility-visual` quality job now independently installs its legacy dependencies and Chromium payload, runs the result-contract fixture and live browser gate unconditionally, and uploads the evidence.
- During verification the shared repository advanced externally to root `f20a1fc73c6c9be184b6a10949b54799e978e3cb` and Builds `04d961759994396132bb2b113ee465b64740a543`, both matching `origin/main`. The EventStore cached `origin/main` later advanced to `b5541259058320a0a7f1db19038709cbd02dad85`, so the root gitlink was reconciled from `27cc17f37774dde958a4d5bf9ba1e9b8d04ce07e` to that current clean tip. The Builds tip contains the generated audit but its externally supplied commit message fails pinned commitlint with `type-empty` and `subject-empty`; history was not rewritten.
- The existing Keycloak TLS readiness failure remained after the Aspire upgrade; `counter-web` was healthy. This was present before the edits and was not weakened or hidden.

## Spec Change Log

- 2026-09-17: Implemented the scoped dependency updates, applied the owner-approved Verify SponsorCheck assertion, reconciled the newly advanced EventStore root gitlink, and completed the Release/package and Debug/source verification matrix.
- 2026-09-17: Closed the matrix-audit gap by repairing and gating the legacy Story 2.2 live Playwright path under Playwright `1.63.0` without changing product UI.
- 2026-09-17: Applied the review patch: hardened the legacy gate and harness, widened the Epic 9 active SDK assertion to the permitted feature band, made sponsorship override detection repository-wide and XML-aware, and aligned active contributor/version documentation.

## Review Triage Log

| Finding | Verdict | Evidence and route |
|---|---|---|
| Approved sponsorship term was recorded but not expiry-gated | medium | Confirmed: the static value alone would permit the assertion after `2031-09-17`. Patched the governance test to fail after the inclusive approved end date. |
| Alternate SponsorCheck modes were not comprehensively rejected | medium | Confirmed against Verify `33.0.2` targets. Patched the governance test to reject licensed, ignored, exemption, private, OpenCollective, and Polar modes. |
| A test project could override the shared sponsorship properties | medium | Confirmed for repository-owned project/props/targets files; command-line globals remain caller-controlled. Patched the governance test to parse all repository-owned MSBuild XML outside ignored submodule/generated/output trees and reject local overrides outside `tests/Directory.Build.props`. |
| Story 11.27 tracking was changed by this implementation | false | The tracking state arrived in the externally advanced root `f20a1fc73c6c9be184b6a10949b54799e978e3cb`; this continuation did not edit those files, so the baseline diff does not establish attribution to this worktree implementation. |
| The root-submodule task text still claimed no gitlinks moved | low | Confirmed documentation drift after EventStore advanced. Corrected the task record; Builds and EventStore movements remain explicit in the implementation notes and verification evidence. |
| The development guide still named Verify `32.0.0` | medium | Confirmed stale active documentation. Patched the guide to `33.0.2`. |
| The legacy Story 2.2 browser path lacked upgrade evidence | medium | Confirmed. Patched the FluentAccordion selectors, interactive/title readiness, keyboard close interaction, Story 2.2 Axe scope, and isolated evidence output; added a blocking, governance-pinned quality-workflow invocation. The live Playwright `1.63.0` run now passes with 8 passed, 5 intentionally skipped, and 0 failed scenarios. |
| Verification-gap: the blocking live gate does not run `npm run test:runner` | medium | Confirmed and patched: the workflow runs the result-contract fixture unconditionally after legacy dependency installation, before the legacy browser install/live gate; governance pins its command, ordering, and blocking behavior. |
| Blind: `resultExitCode` accepts skips, unknown statuses, partial sets, and empty arrays | false | The live producer has only `pass`, `fail`, and explicit `skipped` paths; setup failures throw and exit nonzero, scenario failures emit `fail`, and the six skips are intentional pre-existing exclusions. Unknown, partial, and empty live results are not reachable in the reviewed implementation. |
| Blind: S3 popover submission is skipped despite the known auto-close defect | medium | Confirmed as pre-existing `DW-1108`: the product integration defect predates this dependency update and requires Shell lifecycle work rather than a Playwright-version compatibility patch. Route: defer. |
| Blind: S5 LastUsed prefill is skipped despite the known Counter integration defect | medium | Confirmed as pre-existing `DW-1109`: the product integration defect predates this dependency update and requires subscriber/storage tracing outside dependency compatibility. Route: defer. |
| Blind: S7 unsafe absolute `returnPath` coverage was converted to a skip | medium | Confirmed and patched: direct navigation with an encoded absolute URL now asserts the breadcrumb link resolves to `/`; the focused live run passed S7. |
| Blind: the live runner does not exercise the known Escape-key popover defect | medium | Confirmed by the checked-in Story 2.2 evidence. The product event-propagation defect predates this dependency update and is not caused by Playwright `1.63.0`. Route: defer. |
| Blind: console errors and uncaught `pageerror` events remain evidence-only | medium | Confirmed and patched: console errors and uncaught page errors now set the scenario to `fail` while warnings remain diagnostic-only. |
| Blind: screenshot capture failure leaves the scenario passing | medium | Confirmed and patched: screenshot and page-close failures now set the scenario to `fail` and retain their diagnostics. |
| Blind: advertised selectors are recorded without being asserted before Axe | medium | Confirmed and patched: S6 waits for its form, every Axe scenario waits for every advertised selector, and Axe includes those exact Story 2.2 surfaces. |
| Blind: Chromium is installed only through the separate maintained workspace | low | Confirmed and patched: the legacy workspace installs Chromium through its own manifest before browser launch; governance pins the command and ordering. |
| Blind: the quality workflow omits the legacy runner contract fixture | medium | Confirmed and patched with the same unconditional, governance-pinned result-contract step as the verification-gap finding. |
| Blind: the Epic 9 SDK check requires exact `10.0.401` despite `latestPatch` | medium | Confirmed and patched: the active SDK must be in `[10.0.401, 10.0.500)`, while the installed-SDK checks still prove both `10.0.302` and `10.0.401` are present. |
| Blind: sponsorship expiry is enforced only by the Shell governance test | medium | Carried from the approved-term expiry finding: mandatory governance fails after `2031-09-17`, preventing a green repository gate; focused local builds remain outside that release-governance assertion. No second patch or defer. |
| Blind: repository-local sponsorship override scanning is text-only and tests-tree-only | medium | Confirmed and patched: the guard recursively parses repository-owned `*.csproj`, `*.props`, and `*.targets`, compares XML element local names, excludes only the canonical shared assertion, and skips submodule/generated/output trees. |
| Blind: contributor guidance omits the newly blocking legacy browser lane | low | Confirmed and patched in `tests/README.md`, project context, and the development guide with the independent install, browser, result-contract, and live commands. |
| Blind: refreshed dependency docs retain stale runtime and bUnit versions | low | Confirmed and patched across active project-context, overview, development, and scan-report mirrors to runtime `10.0.12` and bUnit `2.11.3`; historical evidence was untouched. |
| Blind: Story 11.27 tracking regressed in the baseline diff | medium | Confirmed in the externally advanced root commit, not introduced by this worktree implementation. Reverting it would overwrite concurrent user history. Route: defer for the Story 11.27 owner. |
| Edge: exact Epic 9 SDK equality rejects a valid later patch | medium | Confirmed and patched with the same `[10.0.401, 10.0.500)` active-SDK range as the blind finding. |
| Edge: custom-output screenshot paths are relative to the harness directory | low | Confirmed and patched: screenshot references are relative to `outputDir`; the focused custom-output run emitted only `evidence/*.png` paths. |
| Edge: popover visibility lookup errors are swallowed as closed | medium | Confirmed and patched: the locator must reach `hidden` state within the bounded timeout, and locator/page errors fail the scenario. |
| Edge: fixed 250 ms popover closure creates healthy-run flakes | medium | Confirmed and patched with Playwright's awaited hidden-state transition and a 10-second bound. |
| Edge: Axe can run before page-specific content is ready | medium | Confirmed and patched with exact selector readiness before each scan. |
| Edge: direct Verify-consuming builds can still evaluate sponsorship after the approved term | medium | Carried from the approved-term expiry finding: the repository's mandatory governance test is the recorded release enforcement, and no second patch is applied in this review loop. |
| Edge: XML attributes can evade sponsorship override detection | medium | Confirmed and patched by inspecting parsed XML element local names, including elements carrying attributes or namespaces. |
| Edge: imports outside the tests tree can evade sponsorship override detection | medium | Confirmed and patched by scanning all repository-owned MSBuild inputs rather than only `tests`. |
| Edge: comments or CDATA can falsely trigger sponsorship override detection | low | Confirmed and patched: comments and CDATA are not XML elements and no longer match. |
| Edge: direct `npm run story2.2:e2e` now requires a prior Release build | medium | Confirmed and patched: `dotnet run --configuration Release` performs the required build; the focused standalone invocation passed. |
| Edge: main-landmark Axe scoping excludes shell chrome | medium | Confirmed as exposure of pre-existing shell-level `nested-interactive` findings, not a Playwright-version regression. The legacy gate will target its declared Story 2.2 surfaces; shell-chrome remediation routes to deferred work. |
| Edge: runtime and bUnit documentation remains stale | low | Confirmed and patched with the same active-documentation alignment as the blind finding. |

## Verification

**Commands:**
- Builds package-version validator and fixture suites -- expected: catalog, audit, exceptions, family alignment, and downgrade guards pass.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:UseNuGetDeps=true -p:EnableFrontComposerPackageValidation=true` then Release build -- expected: warning-free package graph.
- Debug AppHost build with `UseHexalithProjectReferences=true` plus focused governance/package-boundary tests -- expected: source graph and exact mirrors pass.
- `npm ci` in all three workspaces, E2E typecheck, legacy runner, and relevant Playwright lane -- expected: updated locks and browser tooling pass.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py` -- expected: governance remains green.
- `git diff --check` and root/nested submodule status -- expected: clean formatting and only intended root-level submodule state.

**Results:**

- PASS -- Builds direct catalog/audit/exception/consumer validators; audit generator fixtures (111 scenarios); audit-validator fixtures (103 scenarios).
- PASS -- Debug/source AppHost build with 0 warnings and 0 errors; Aspire SDK `13.5.4` started and `counter-web` became healthy.
- PASS -- npm installs, E2E typecheck, the focused maintained Playwright lane, the legacy result-contract fixtures (2/2), and the patched standalone Story 2.2 browser gate under Playwright `1.63.0` (13 scenarios: 8 passed, 5 intentionally skipped, 0 failed). Its custom-output JSON used portable `evidence/*.png` screenshot paths.
- PASS -- 157 Python governance tests, 5 package-boundary tests, the Verify sponsorship governance regression test, tool restore, Stryker 5 executable check, and `git diff --check`.
- PASS -- all eight root submodules are clean and equal their cached `origin/main`; the newly advanced EventStore root gitlink resolves to `b5541259058320a0a7f1db19038709cbd02dad85`; nested submodules remain uninitialized.
- PASS -- serialized Release/package restore and build completed with 0 warnings and 0 errors. SponsorCheck accepted the owner-approved account/start assertion in both Verify-consuming test projects and reported informational `SC017` messages only.
- PASS -- focused review-patch checks: harness syntax; workflow YAML and project-scan JSON parsing; `QualityWorkflow_PinsAccessibilityVisualGate`, `VerifySponsorship_OwnerApproval_IsAppliedThroughSharedTestConfiguration`, and `ToolchainPins_MatchApprovedDotnetAndAspireVersions` (1/1 each).
