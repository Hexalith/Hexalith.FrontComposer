---
title: 'Run all configured tests and fix failures'
type: 'bugfix'
created: '2026-07-08T00:00:00+02:00'
status: 'draft'
review_loop_iteration: 0
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
- [ ] `Hexalith.FrontComposer.slnx` -- restore and build Release before test execution -- confirms compile/analyzer baseline.
- [ ] `.github/workflows/ci.yml` and `tests/README.md` -- mirror the configured local blocking lanes -- avoids inventing a different definition of "all tests".
- [ ] `tests/Hexalith.FrontComposer.*.Tests/` -- run .NET governance, contract, and default lanes with `DiffEngine_Disabled=true`; use focused reruns for diagnosis -- isolates failures while keeping final evidence broad.
- [ ] `tests/e2e/` -- run Playwright install-sensitive validation where local tooling permits: typecheck, a11y, visual governance, and a11y artifact validation -- covers browser-facing configured tests.
- [ ] `src/`, `samples/`, and matching test files -- patch only code or fixtures implicated by failures -- keeps blast radius tied to evidence.
- [ ] `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures.md` -- record completion notes only through workflow status updates -- keeps this run auditable.

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
