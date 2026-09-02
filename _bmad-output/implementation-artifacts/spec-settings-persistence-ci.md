---
title: 'Run the settings-persistence Unicode storage-key regression in CI'
type: 'bugfix'
created: '2026-09-02'
status: 'done'
baseline_revision: '15a00e8d1999892a4bfd6a7f2f355d328576b446'
baseline_commit: '15a00e8d1999892a4bfd6a7f2f355d328576b446'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/.bmad-loop/runs/20260901-072044-3ff7/bundles/settings-persistence-ci/intent.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The blocking `accessibility-visual` workflow typechecks the settings-persistence Playwright spec but never executes its browserless Unicode storage-key regression. A future change to the TypeScript casing mirror could therefore diverge from .NET invariant canonicalization while CI stays green.

**Approach:** Add one cross-platform npm entry point that runs exactly the existing Unicode storage-key test without a web server, invoke it as a blocking step in the existing Playwright workflow job, and extend the current CI governance assertions to pin both the workflow wiring and the focused command.

## Boundaries & Constraints

**Always:** Keep the existing `%C4%B0%CF%83%40example.com` golden vector and .NET `FrontComposerStorageKey.CanonicalizeUser` semantics unchanged; use `cross-env` for Windows-compatible `PLAYWRIGHT_SKIP_WEBSERVER`; execute only Chromium and the named pure helper test; retain normal Playwright failure artifacts and blocking behavior.

**Block If:** Deterministic selection requires moving or rewriting the regression, changing production storage-key canonicalization, or starting the Counter web host.

**Never:** Do not edit `_bmad-output/implementation-artifacts/deferred-work.md`, the bundle ledger, runtime persistence code, package versions or lockfile dependency data, submodules, generated output, or unrelated Playwright suites.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Canonical Unicode identity | `tenant` and trimmed `İΣ@Example.COM` | Focused CI test yields `tenant:%C4%B0%CF%83%40example.com:theme` | Mismatch fails the blocking job |
| Browserless execution | Settings spec also contains browser-dependent tests | Exact-title grep runs only the pure helper regression with Chromium and no web server | Zero or extra selected tests must not be accepted as equivalent wiring |

</intent-contract>

## Code Map

- `.github/workflows/quality.yml:512-581` -- CI-authoritative `accessibility-visual` job; add the non-advisory browserless settings guard beside the existing FC-NIP guard and before the hosted a11y suite.
- `tests/e2e/package.json:17-39` -- existing Playwright script registry and `cross-env` browserless convention; add the exact focused command without changing dependencies or `package-lock.json`.
- `tests/e2e/specs/settings-persistence.spec.ts:25-33` -- read-only pure regression and exact test title selected by the new script; its expected encoded key is the observable contract.
- `tests/e2e/page-objects/settings.page.ts:128-149` -- read-only TypeScript mirror whose simple invariant casing is guarded by the selected regression.
- `src/Hexalith.FrontComposer.Shell/Services/FrontComposerStorageKey.cs:75-85` and `tests/Hexalith.FrontComposer.Shell.Tests/State/StorageKeysTests.cs:26-35` -- read-only .NET runtime authority and independent golden vector; production semantics are out of scope.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:379-464` -- extend the existing accessibility-workflow and browserless-script facts so removal, advisory conversion, or command broadening fails Governance.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- orchestrator-owned, strictly read-only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/e2e/package.json` -- add `test:settings-persistence-storage-key` using `cross-env`, `PLAYWRIGHT_SKIP_WEBSERVER=1`, Chromium, the settings spec, and an anchored exact-title grep so the regression is deterministic and serverless.
- [x] `.github/workflows/quality.yml` -- invoke the new npm script in a named blocking step within `accessibility-visual`, before the hosted accessibility/visual gate.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- extend existing facts to require the named non-advisory workflow step, exact npm invocation, cross-platform environment assignment, and exact focused Playwright command.

**Acceptance Criteria:**
- Given the `accessibility-visual` job runs on Windows, when it reaches browserless contract guards, then it invokes `npm run test:settings-persistence-storage-key` without `continue-on-error` and without starting the specimen host for that test.
- Given the npm guard is invoked, when Playwright discovers tests, then its anchored title filter selects the existing Unicode storage-key regression in Chromium and excludes the browser-dependent settings scenarios.
- Given `SettingsPage.userSegment` no longer matches .NET invariant simple lowercasing for `İΣ@Example.COM`, when the blocking guard runs, then the expected encoded storage key assertion fails CI.
- Given workflow or script wiring is removed, broadened, or made advisory, when Shell Governance tests run, then the existing CI governance facts fail.

## Spec Change Log

## Review Triage Log

### 2026-09-02 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 0, medium 0, low 7)
- defer: 0
- reject: 11: (high 0, medium 0, low 11)
- addressed_findings:
  - `[low]` `[patch]` Raw workflow text could let a commented-out step satisfy Governance; strip YAML comments and bound assertions to the executable `accessibility-visual` job.
  - `[low]` `[patch]` A conditional settings step could stay textually present while never running; reject any step-level `if:` key.
  - `[low]` `[patch]` Alternate or job-level `continue-on-error` could make the regression advisory; reject the key anywhere in the job.
  - `[low]` `[patch]` Extra shell logic could swallow the npm command's exit code; require the extracted step to end with the exact invocation.
  - `[low]` `[patch]` A skipped, focused, or duplicate named regression could evade the intended one-test execution; require exactly one active top-level declaration and one total declaration variant.
  - `[low]` `[patch]` The script's environment assignment did not pin Playwright's serverless configuration branch; require `PLAYWRIGHT_SKIP_WEBSERVER` to resolve `webServer` to `undefined`.
  - `[low]` `[patch]` Workflow lint and deferred-ledger immutability results lacked reproducible command entries; add both commands plus the artifact-scope gate to Verification.

## Implementation Notes

- Added a cross-platform browserless npm command whose anchored Playwright title selects only the existing Unicode storage-key regression.
- Added the command as a non-advisory `accessibility-visual` step before the hosted accessibility and visual suite.
- Extended the existing CI governance facts without adding a new test identifier; they pin step ordering, blocking behavior, cross-platform environment assignment, and the exact focused command.
- Runtime storage canonicalization, the Playwright helper/regression, dependency metadata, submodules, and the deferred-work ledger remain unchanged.

## File List

- `.github/workflows/quality.yml` -- runs the focused settings-persistence storage-key regression in the blocking Playwright job.
- `tests/e2e/package.json` -- defines the deterministic cross-platform browserless Playwright command.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pins the workflow step and exact command in existing Governance facts.
- `_bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- records the intent, implementation, review, and verification evidence for this bundle.

## Verification

**Commands:**
- `npm --prefix tests/e2e run typecheck` -- expected: all Playwright TypeScript compiles.
- `npm --prefix tests/e2e run test:settings-persistence-storage-key` -- expected: exactly 1 browserless Chromium test passes and no web server starts.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release` -- expected: Release build succeeds with zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.QualityWorkflow_PinsAccessibilityVisualGate -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.PlaywrightBrowserlessScripts_UseCrossPlatformEnvironmentAssignment` -- expected: both focused governance facts pass.
- `actionlint .github/workflows/quality.yml` -- expected: the updated workflow is valid.
- `git diff --exit-code -- _bmad-output/implementation-artifacts/deferred-work.md` -- expected: the orchestrator-owned deferred-work ledger is unchanged.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- expected: the freeform story-artifact and File List validation passes.
- `git diff --check` -- expected: no whitespace errors or conflict markers.

**Results:** TypeScript typecheck passed; the browserless guard ran exactly 1 Chromium test and passed; the Shell test project built Release with 0 warnings and 0 errors; both focused governance facts passed; `actionlint .github/workflows/quality.yml`, `git diff --check`, and the deferred-ledger no-diff check passed.

## Auto Run Result

Status: done

Summary: The blocking `accessibility-visual` job now executes the existing Unicode settings-persistence storage-key regression through a deterministic, Windows-compatible, browserless npm command. Existing Governance facts pin the executable workflow step, its ordering and fail-closed behavior, the exact one-test command, the active regression declaration, and Playwright's serverless configuration branch.

Files changed:
- `.github/workflows/quality.yml` -- invokes the focused storage-key regression as a blocking browserless Playwright step.
- `tests/e2e/package.json` -- defines the exact Chromium command with cross-platform serverless environment assignment and anchored test selection.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- prevents commented, conditional, advisory, exit-masked, skipped, duplicated, broadened, or host-backed wiring from satisfying Governance.
- `_bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- records the bundle intent, implementation, review triage, verification, and final result.

Review findings breakdown: 7 low patches applied; 0 items deferred; 11 low findings rejected as disproven, speculative, broader than the verbatim ledger gap, or intentional exact-contract coupling.

Follow-up review recommendation: true. Patched findings were high 0, medium 0, low 7; score = `3 × 0 + 1 × 7 = 7`, meeting the threshold of 5.

Verification: `npm --prefix tests/e2e run typecheck` passed; the focused npm guard passed exactly 1 Chromium test; the affected Shell test project built Release with 0 warnings and 0 errors; both focused xUnit v3 Governance facts passed 2/2; `actionlint`, story-artifact validation, deferred-ledger no-diff validation, and `git diff --check` passed.

Residual risks: The complete hosted Windows `accessibility-visual` job was not run locally. The exact cross-platform command ran locally without starting a web server, and static Governance coverage pins the Windows workflow wiring and serverless configuration. Runtime persistence code, the user-visible settings contract, dependencies, lockfiles, submodules, and the deferred-work ledger were unchanged.
