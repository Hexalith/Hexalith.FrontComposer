---
title: 'Run current tests and fix failures'
type: 'bugfix'
created: '2026-07-31'
status: 'done'
review_loop_iteration: 0
baseline_commit: '9df19c7acac93100eb043d7fc7373fe678c8f34e'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/tests/README.md'
  - '{project-root}/.github/workflows/quality.yml'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The clean repository at `9df19c7a` lacks a current local test baseline after workflow, governance-test, analyzer-ledger, and dependency-gitlink changes. Failures could therefore be hiding on `main`.

**Approach:** Run locally reproducible CI-authoritative build and test lanes, followed by relevant advisory lanes. Fix each proven defect at its owning FrontComposer surface, then rerun the focused reproducer and affected broad lane.

## Boundaries & Constraints

**Always:** Use `Hexalith.FrontComposer.slnx`, Release, warnings as errors, `DiffEngine_Disabled=true`, and the repository's solution-level trait filters. Preserve central package management, root-submodule boundaries, generator/UI/security invariants, and fail-closed governance. Separate assertion failures from environmental blockers and record exact fallback evidence.

**Ask First:** Halt before changing dependencies, submodules/gitlinks, CI/release workflows, Verify/PublicAPI/Pact/Playwright baselines, human-owned evidence, or a legitimate product contract.

**Never:** Do not weaken checks or assertions; quarantine, skip, delete, or broadly suppress failures; initialize nested submodules; edit generated output; blindly accept baselines; or include provider-spend, mutation, or 10,000-case nightly runs in the default loop.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Green baseline | Configured lanes execute | Report commands/counts; leave sources unchanged | Confirm tracked state |
| Reproducible failure | Build or test fails | Patch minimally; rerun focused then broad | Preserve first evidence; avoid unrelated edits |
| Environmental blocker | Lane produces no intended signal | Run narrowest valid fallback | Report command, error, timing, fallback, and CI authority |
| Gated drift | Baseline, dependency, gitlink, or workflow appears stale | Stop without rewriting it | Present exact paths and evidence |

</frozen-after-approval>

## Code Map

- `Hexalith.FrontComposer.slnx` and root props -- Release dependency graph and build surface.
- `.github/workflows/quality.yml` and `tests/README.md` -- lane, filter, and evidence authority.
- `tests/Hexalith.FrontComposer.*.Tests/` -- .NET unit, component, generator, governance, contract, and performance tests.
- `tests/eng/` and `eng/` -- Python governance engines and fixtures.
- `tests/e2e/` and `samples/Counter/Counter.Web/` -- Playwright tests, host, baselines, and validators.
- `src/`, `samples/`, and matching tests -- eligible failure-owned fix surfaces.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.FrontComposer.slnx` -- Release/package-validation restore and build -- establish compiler, analyzer, package, and dependency health.
- [x] `tests/Hexalith.FrontComposer.*.Tests/` -- run blocking Governance, Contract, and default filters, then advisory palette/performance/quarantine filters -- cover configured .NET categories with correct authority.
- [x] `tests/eng/` -- run current Python governance fixtures -- validate non-.NET policy logic.
- [x] `tests/e2e/` -- typecheck, build Counter, run the blocking Chromium accessibility/visual lane and validators, then broader Chromium coverage if stable -- cover browser behavior without baseline rewrites.
- [x] `src/`, `samples/`, `tests/`, or `eng/` -- apply only evidence-driven fixes with regression coverage when behavior changes -- prevent recurrence.
- [x] `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures-2.md` -- record commands, results, blockers, fallbacks, and changed paths -- provide an auditable handoff.

**Acceptance Criteria:**
- Given prerequisites are available, when blocking .NET, Python, and browser lanes run, then they pass without warnings, stale pacts, or unapproved baseline drift.
- Given a reproducible defect, when its minimal fix is applied, then focused and affected broad lanes pass.
- Given an environmental blocker, when fallbacks finish, then blocked authority remains distinct from passing focused evidence.
- Given advisory results, when reported, then they do not weaken or obscure blocking gates.

## Spec Change Log

- 2026-07-31 -- Release/package-validation restore and build passed with 0 warnings and 0 errors. The blocking Governance lane then exposed an approval-gated semantic dependency-policy mismatch introduced by the baseline commit's `references/Hexalith.Builds` gitlink advance; execution stopped before changing the expected-version contract.
- 2026-07-31 -- Human approval authorized updating `eng/dependency-graph-policy.json` so `HexalithTenantsVersion` matches the selected `5.3.0` catalog. The focused validator then exposed a second unapproved expected-package change (`FsCheck.Xunit.v3` `3.3.3` to `3.3.4`), so execution stopped at the new approval boundary.
- 2026-07-31 -- Human approval authorized the remaining selected-catalog expectation updates for FsCheck, Localization, Verify, and Verify.XunitV3. Focused validation and all blocking/advisory lanes completed; no Pact, PublicAPI, Verify, or Playwright baseline changed.

## Verification

**Results:**

- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore` -- passed; build completed in 15.76 seconds with 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category=Governance" --results-directory ./TestResults/quick-dev-governance --logger "trx;LogFileName=test-results-governance.trx"` -- failed after test execution. Shell Governance: 193 passed, 2 failed; other projects reported 161 passed and no failed tests. Both failures reproduce `HexalithTenantsVersion expected '3.2.18', found '5.3.0'`.
- `python3 eng/dependency_graph.py --root . validate --commit 9df19c7acac93100eb043d7fc7373fe678c8f34e` -- failed with the same semantic-policy diagnostic. Baseline commit `9df19c7a` advanced `references/Hexalith.Builds` from `79f82acc9cb9259ddcb90217c89bc72024ab7f72` (catalog value `3.2.18`) to `b529b665a6f076d07d218266ab74ca211f34f5a7` (catalog value `5.3.0`) without updating `eng/dependency-graph-policy.json`, which still requires `3.2.18`.
- Approved fix -- `eng/dependency-graph-policy.json` now requires `HexalithTenantsVersion` `5.3.0`, matching the selected Builds catalog.
- `python3 eng/dependency_graph.py --root . validate --commit 9df19c7acac93100eb043d7fc7373fe678c8f34e` after the approved fix -- failed at the next semantic check: `FsCheck.Xunit.v3 expected version '3.3.3', found '3.3.4'`. The prior Builds catalog `79f82acc` selected `3.3.3`; baseline catalog `b529b665` selects `3.3.4`.
- Approved final policy fix -- updated only the five human-approved semantic expectations in `eng/dependency-graph-policy.json`: `HexalithTenantsVersion` `3.2.18` to `5.3.0`, `FsCheck.Xunit.v3` `3.3.3` to `3.3.4`, `Microsoft.Extensions.Localization` `10.0.9` to `10.0.10`, and `Verify` / `Verify.XunitV3` `31.24.2` to `31.27.0`.
- `python3 eng/dependency_graph.py --root . validate --commit 9df19c7acac93100eb043d7fc7373fe678c8f34e` -- passed after the approved policy updates; 43 edges collected and all 7 semantic selectors validated.
- `python3 -m unittest tests/eng/test_dependency_graph.py -v` -- passed, 24 tests.
- Governance rerun -- passed, 356 tests across the six projects containing Governance tests (Shell 195, SourceTools 140, MCP 8, Contracts 6, CLI 6, Bench 1).
- Contract lane -- passed, 3 tests. `pwsh -NoProfile -File ./eng/validate-contract-artifacts.ps1` passed, and `git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact` confirmed no stale pact drift.
- Default blocking .NET lane -- passed, 4,188 tests across 8 projects.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_pack_release_packages.py -v` -- passed, 33 tests.
- Advisory .NET lanes -- palette passed 4 tests; performance passed 26 tests; quarantine filter completed with an explicit zero-test result because no tests currently carry `Category=Quarantined`.
- `npm --prefix tests/e2e run typecheck` -- passed. `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj --configuration Release` passed with 0 warnings and 0 errors.
- Blocking Chromium accessibility/visual lane -- command exited successfully with 21 first-attempt passes and one retry-only visual pass. Both artifact validators passed, no baseline changed, and the focused `visual baseline light compact` rerun passed first attempt.
- Broader Chromium lane -- command exited successfully with 115 first-attempt passes and two retry-only lifecycle passes. A focused rerun of the affected lifecycle and policy specs passed all 3 tests first attempt. The retry-only results remain advisory flake evidence; assertions and baselines were not weakened or rewritten.
- Changed paths -- `eng/dependency-graph-policy.json`, this implementation spec, and the review-appended entries in `deferred-work.md`. Concurrent checkout drift at `references/Hexalith.EventStore` and `references/Hexalith.Parties` was preserved and excluded from the change.
- Final drift check -- no Pact or Playwright snapshot diff; `git diff --check` passed for tracked changes. No environmental blocker remains.

**Exact additional lane commands (in execution order):**

- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category=e2e-palette" --results-directory ./TestResults/quick-dev-palette --logger "trx;LogFilePrefix=test-results-e2e-palette"`
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category=Performance" --results-directory ./TestResults/quick-dev-performance --logger "trx;LogFilePrefix=test-results-performance"`
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category=Quarantined" --results-directory ./TestResults/quick-dev-quarantine --logger "trx;LogFilePrefix=test-results-quarantine"`
- `npm --prefix tests/e2e run validate:visual-governance && npm --prefix tests/e2e run validate:a11y-artifacts`
- From `tests/e2e`: `CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npx playwright test specs/specimen-accessibility.spec.ts --project=chromium --grep "visual baseline light compact"`
- `CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npm --prefix tests/e2e run test:chromium`
- From `tests/e2e`: `CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npx playwright test specs/lifecycle.spec.ts specs/policy-gated-command-authorization.spec.ts --project=chromium`

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category=Governance"` -- expected: passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-build --filter "Category=Contract"` -- expected: passes without pact drift.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: default lane passes.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_pack_release_packages.py` -- expected: passes.
- `npm --prefix tests/e2e run typecheck && CI=true ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true npm --prefix tests/e2e run test:a11y` -- expected: passes.
- `npm --prefix tests/e2e run validate:visual-governance && npm --prefix tests/e2e run validate:a11y-artifacts` -- expected: evidence validates.

## Suggested Review Order

**Catalog compatibility**

- Match the accepted Tenants catalog property selected by the current Builds gitlink.
  [`dependency-graph-policy.json:33`](../../eng/dependency-graph-policy.json#L33)

- Pin test and localization packages so semantic catalog drift remains fail-closed.
  [`dependency-graph-policy.json:37`](../../eng/dependency-graph-policy.json#L37)

- Keep Verify's paired packages aligned with selected snapshot tooling.
  [`dependency-graph-policy.json:48`](../../eng/dependency-graph-policy.json#L48)

**Verification and scope**

- Trace failure discovery, human approvals, and successful validation.
  [`spec-run-all-tests-fix-failures-2.md:68`](spec-run-all-tests-fix-failures-2.md#L68)

- Reproduce every advisory and browser rerun with exact invocations.
  [`spec-run-all-tests-fix-failures-2.md:95`](spec-run-all-tests-fix-failures-2.md#L95)

- Confirm concurrent submodule checkout drift stayed excluded from this repair.
  [`spec-run-all-tests-fix-failures-2.md:92`](spec-run-all-tests-fix-failures-2.md#L92)

**Deferred review findings**

- Review context and EventStore gaps explicitly kept outside this repair.
  [`deferred-work.md:1867`](deferred-work.md#L1867)
