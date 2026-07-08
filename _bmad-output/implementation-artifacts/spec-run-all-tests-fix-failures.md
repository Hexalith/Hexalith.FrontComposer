---
title: 'Run all configured tests and fix failures'
type: 'bugfix'
created: '2026-07-08T00:00:00+02:00'
status: 'done'
review_loop_iteration: 0
baseline_commit: '1e0662767a6df9da971b0ed50b450524d5a4782f'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/tests/README.md'
  - '{project-root}/.github/workflows/ci.yml'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** The current repository test state is unknown and may contain regressions that block local confidence or CI. The user wants the test suite run and any failures fixed.

**Approach:** Execute the configured FrontComposer validation lanes from the repository root, triage failures to their owned source or test surface, and make the smallest code or test updates needed to restore green results.

## Boundaries & Constraints

**Always:** Use `Hexalith.FrontComposer.slnx`, Release configuration, and `DiffEngine_Disabled=true` for Verify-backed .NET lanes. Keep edits in owned FrontComposer files, preserve central package management, warnings-as-errors, Fluent/FrontComposer UI rules, and the source-generator contracts. Prefer focused reruns after each fix, then rerun the failing broad lane.

**Ask First:** Stop before intentionally updating Verify snapshots, PublicAPI baselines, Pact files, Playwright visual baselines, package versions, submodule contents, or generated artifacts. Stop before changing CI definitions unless a failure proves the workflow itself is wrong.

**Never:** Do not initialize nested submodules, use `.sln`, weaken analyzers or `TreatWarningsAsErrors`, bypass failures with broad suppressions, remove or quarantine tests to get green, edit `references/Hexalith.*` contents, or hand-edit generated output under `obj/**/generated/HexalithFrontComposer/`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| green baseline | Restore, build, .NET test lanes, and browser lanes pass | Report the commands run and leave code unchanged except incidental test artifacts ignored by git | Confirm clean or expected git status |
| code or test failure | A lane fails with compiler, analyzer, unit, bUnit, source-generator, MCP, CLI, or browser assertion output | Identify the failing contract, patch the owned implementation or test fixture, and rerun focused then broad validation | Preserve the first failure evidence and avoid speculative unrelated edits |
| environmental blocker | Tooling, browser install, sockets, NuGet, Docker, or local host startup prevents a lane from producing signal | Record the exact blocker, run the nearest valid fallback when available, and distinguish blocked local evidence from passing CI authority | Do not claim the blocked lane passed |
| approval-gated artifact drift | Snapshot, PublicAPI, Pact, or visual baseline differs | Halt with the exact artifact path and reason an update appears required | Wait for human approval before updating |

</frozen-after-approval>

## Code Map

- `Hexalith.FrontComposer.slnx` -- root solution for restore, build, and solution-level .NET test lanes.
- `.github/workflows/ci.yml` -- authoritative local shape for governance, contract, default, advisory, and browser validation commands.
- `tests/README.md` -- test architecture, evidence vocabulary, and local blocker reporting rules.
- `tests/Hexalith.FrontComposer.*.Tests/` -- xUnit v3, Shouldly, bUnit, source-generator, CLI, MCP, and contract test projects likely to surface .NET failures.
- `tests/e2e/` -- Playwright TypeScript accessibility, visual governance, and artifact validation workspace.
- `src/` and `samples/` -- owned production and specimen host code to patch when failures identify real regressions.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.FrontComposer.slnx` -- restore and build Release before test execution -- confirms compile/analyzer baseline; package-mode restore is locally blocked by missing NuGet feed/package availability and source-reference fallback passed.
- [x] `.github/workflows/ci.yml` and `tests/README.md` -- mirror the configured test lanes, not every non-test CI gate -- avoids inventing a different definition of "all tests".
- [x] `tests/Hexalith.FrontComposer.*.Tests/` -- run .NET governance, contract, and default lanes with `DiffEngine_Disabled=true`; use focused reruns for diagnosis -- isolates failures while keeping final evidence broad.
- [x] `tests/e2e/` -- run Playwright install-sensitive validation where local tooling permits: typecheck, a11y, visual governance, and a11y artifact validation -- covers browser-facing configured tests.
- [x] `src/`, `samples/`, and matching test files -- patch only code or fixtures implicated by failures -- triage found no source or fixture fix was required.
- [x] `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures.md` -- record completion notes only through workflow status updates -- keeps this run auditable.

**Acceptance Criteria:**
- Given the repository is restored, when Release build runs, then it completes without warnings or errors.
- Given `DiffEngine_Disabled=true`, when the configured governance, contract, and default .NET lanes run, then they pass or a local environmental blocker is documented with fallback evidence.
- Given browser tooling is available, when the Playwright typecheck, a11y, visual-governance, and artifact-validation commands run, then they pass or an exact local blocker is documented.
- Given any failure requires changing snapshots, pacts, PublicAPI baselines, visual baselines, packages, submodules, or CI behavior, when that requirement is discovered, then implementation halts for human approval before making that change.

## Spec Change Log

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx` -- expected: restore succeeds for the root solution.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore` -- expected: build succeeds with warnings as errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category=Governance"` -- expected: governance lane passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter "Category=Contract"` -- expected: contract lane passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: default .NET lane passes.
- `npm --prefix tests/e2e run typecheck` -- expected: Playwright TypeScript project typechecks.
- `npm --prefix tests/e2e run test:a11y` -- expected: accessibility, keyboard, media, zoom, and visual specimen gate passes.
- `npm --prefix tests/e2e run validate:visual-governance` -- expected: visual baseline governance passes.
- `npm --prefix tests/e2e run validate:a11y-artifacts` -- expected: accessibility artifacts validate.

**Results (2026-07-08):**

| Lane | Required Command | Local Result | Blocker Timing | Fallback Evidence | CI Authority |
| --- | --- | --- | --- | --- | --- |
| Package-mode restore | `dotnet restore Hexalith.FrontComposer.slnx` | Blocked: `NU1102` for `Hexalith.FrontComposer.Shell`, `Hexalith.FrontComposer.Contracts`, and `Hexalith.FrontComposer.Schema` 1.7.0; `NU1101` for `Hexalith.Commons.Http`, `Hexalith.Commons.ServiceDefaults`, and `Hexalith.Tenants.Client` from `nuget.org` | Before build and test execution | `dotnet restore Hexalith.FrontComposer.slnx -p:UseHexalithProjectReferences=true` passed | Required for package-mode CI/feed parity |
| Release build fallback | `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -p:UseHexalithProjectReferences=true` | Passed, 0 warnings, 0 errors | N/A | N/A | Advisory for package-mode CI; valid local source-reference build signal |
| Governance tests | `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build -p:UseHexalithProjectReferences=true --filter "Category=Governance"` | Passed: SourceTools 131, Shell 117 | N/A | N/A | Advisory for package-mode CI; required local policy signal with source references |
| Contract tests | `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build -p:UseHexalithProjectReferences=true --filter "Category=Contract"` | Passed: Shell 3 | N/A | N/A | Advisory for package-mode CI; required local contract signal with source references |
| Default .NET tests | `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build -p:UseHexalithProjectReferences=true --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` | Passed: 3900 tests | N/A | N/A | Advisory for package-mode CI; required local default-lane signal with source references |
| Advisory .NET categories | `Category=e2e-palette`, `Category=NightlyProperty`, `Category=Performance`, `Category=Quarantined` with the same Release/source-reference test shape | Passed: Shell 4, Shell 1, SourceTools 6 + Shell bench 2, and zero quarantined matches | N/A | No TRX/quarantine summary generated because these were direct local checks without CI result directories | Advisory |
| Browser typecheck | `npm --prefix tests/e2e run typecheck` | Passed | N/A | N/A | Advisory; local workspace dependencies were already installed |
| Browser a11y CI gate | `CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npm --prefix tests/e2e run test:a11y` | Passed: 21 Chromium tests | N/A | Artifacts under `tests/e2e/playwright-report/` and `tests/e2e/test-results/` | Required in CI for clean install/browser setup parity |
| Browser full Chromium suite | `CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npm --prefix tests/e2e run test:chromium` | Passed: 115 Chromium tests | N/A | Artifacts under `tests/e2e/playwright-report/` and `tests/e2e/test-results/`; full all-browser run was not attempted because CI installs Chromium only | Advisory beyond CI a11y gate |
| Browser artifact validation | `npm --prefix tests/e2e run validate:visual-governance` and `npm --prefix tests/e2e run validate:a11y-artifacts` | Passed after browser runs; no committed visual baseline changes detected | N/A | `tests/e2e/playwright-report/`, `tests/e2e/test-results/junit.xml` | Required in CI for generated artifact parity |

Final tracked tree state: only `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures.md` is modified relative to baseline `1e0662767a6df9da971b0ed50b450524d5a4782f`; no source, test, pact, snapshot, PublicAPI, submodule, or visual-baseline files changed.

## Suggested Review Order

- Confirm the workflow state and baseline commit first.
  [`spec-run-all-tests-fix-failures.md:5`](spec-run-all-tests-fix-failures.md#L5)

- Check task completion wording around package-mode blocker handling.
  [`spec-run-all-tests-fix-failures.md:50`](spec-run-all-tests-fix-failures.md#L50)

- Review the blocker table and source-reference fallback boundary.
  [`spec-run-all-tests-fix-failures.md:83`](spec-run-all-tests-fix-failures.md#L83)

- Verify the final tree-state claim before committing.
  [`spec-run-all-tests-fix-failures.md:96`](spec-run-all-tests-fix-failures.md#L96)
