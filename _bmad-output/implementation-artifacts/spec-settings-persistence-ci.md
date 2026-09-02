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
deferred:
  - summary: >-
      The browserless Playwright guards share `playwright-report/` and `test-results/` with the
      hosted `test:a11y` run, so a browserless failure leaves artifacts that the `if: always()`
      accessibility-artifact validators then misreport.
    evidence: |-
      `tests/e2e/playwright.config.ts` defaults `OUTPUT_DIR`/`HTML_REPORT_DIR`/`JUNIT_PATH` to the
      same paths for every run, and no browserless step in `accessibility-visual` overrides
      `FC_E2E_OUTPUT_DIR`/`FC_E2E_HTML_REPORT_DIR`/`FC_E2E_JUNIT_PATH`. Pre-existing: the
      `Run FC-NIP contract guards (browserless)` step has had this property since it landed; the
      settings guard follows the same established convention rather than introducing it.
    location: >-
      .github/workflows/quality.yml (accessibility-visual browserless steps)
    severity: low
  - summary: >-
      Three of the four `SettingsPage` storage-key mirror paths are still typechecked but never
      executed in CI.
    evidence: |-
      `tenantSegment`, the non-email branch of `userSegment`, and the `!'()*` re-encoding in
      `escapeDataString` mirror `FrontComposerStorageKey` with no executing CI lane, while the
      corresponding .NET paths are covered by `StorageKeysTests`. This bundle's intent scoped the
      fix to the single existing Unicode regression, so closing the remaining three is separate work.
    location: >-
      tests/e2e/page-objects/settings.page.ts:126-158
    severity: low
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

### 2026-09-02 -- Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 0, medium 2, low 5)
- defer: 2: (high 0, medium 0, low 2)
- reject: 12: (high 0, medium 0, low 12)
- addressed_findings:
  - `[medium]` `[patch]` The declaration-line pin could not detect a body-level `test.skip()`/`test.fixme()`: Playwright exits 0 when its only selected test is skipped, so the blocking guard would stay green while guarding nothing. Governance now forbids `test.skip(`/`test.fixme(`/`test.only(` in the selected spec; proved by mutation (Playwright exit 0, fact now fails).
  - `[medium]` `[patch]` Nothing pinned the executed expectation, so a coordinated mirror-plus-expectation edit could retune the TypeScript golden vector while `StorageKeysTests` kept the original .NET literal -- the exact divergence the intent exists to prevent. Governance now pins one `unicodeStorageKeyGoldenVector` constant against both the Playwright `expect(key).toBe(...)` line and `StorageKeysTests.cs`.
  - `[low]` `[patch]` A job-level `if:` on `accessibility-visual` would skip every step while all step-scoped assertions still passed; the job header is now asserted unconditional.
  - `[low]` `[patch]` `ExtractNamedStep` ends a slice only at the next `- name:`, so a bare `- uses:` neighbour would be absorbed and red the exit-code-tail assertion for an unrelated edit; the guard is now bound with the existing `FindStepBlockContaining` helper plus its own name pin.
  - `[low]` `[patch]` The new workflow step and the new governance blocks carried none of the dated rationale comments every sibling in both files uses; added.
  - `[low]` `[patch]` AC3 and AC4 were asserted but never demonstrated red; five falsification mutations are now applied, recorded in Verification, and reverted.
  - `[low]` `[patch]` The deferred-ledger immutability evidence had become false (the orchestrator rewrote the entry after delivery) and the story-artifact validator result was missing; the check is re-scoped to this bundle's commit range, the orchestrator's edit is declared under `## Documented Unrelated Changes`, and the validator outcome is recorded.

## Implementation Notes

- Added a cross-platform browserless npm command whose anchored Playwright title selects only the existing Unicode storage-key regression.
- Added the command as a non-advisory `accessibility-visual` step before the hosted accessibility and visual suite.
- Extended the existing CI governance facts without adding a new test identifier; they pin step ordering, blocking behavior, cross-platform environment assignment, and the exact focused command.
- Runtime storage canonicalization, the Playwright helper/regression, dependency metadata, submodules, and the deferred-work ledger remain unchanged.
- Follow-up review pass hardened the guard against the two ways it could have stayed green while guarding nothing: a skipped selected test, and a retuned expectation literal. Both mirrors of the Unicode golden vector are now pinned to a single governance constant.
- The workflow job header is pinned unconditional, and the settings step is bounded by its own step block rather than by the next `- name:` boundary.

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
- `git diff --exit-code 15a00e8d1999892a4bfd6a7f2f355d328576b446..HEAD -- _bmad-output/implementation-artifacts/deferred-work.md` -- expected: this bundle's own commit range never touches the orchestrator-owned deferred-work ledger. Scoped to the range, not the working tree: the orchestrator legitimately rewrites that file's entry status after delivery, so a working-tree check is not a reproducible statement about this story.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- expected: the freeform story-artifact and File List validation passes.
- `git diff --check` -- expected: no whitespace errors or conflict markers.

**Falsification proofs (follow-up review pass, 2026-09-02):** each mutation was applied, observed, and reverted.

| Mutation | Observed |
|----------|----------|
| Body-level `test.skip()` added to the selected regression | Playwright exits **0** (the green-hole this pass closes); `PlaywrightBrowserlessScripts_UseCrossPlatformEnvironmentAssignment` now **fails** on `"test.skip("` |
| `toLowerInvariantSimple` loses its `U+0130` exemption (AC3) | Guard **fails**, `Received: "tenant:i%CC%87%CF%83%40example.com:theme"`, exit **1** |
| Spec expectation retuned to the drifted key | Governance **fails**: `the frozen Unicode golden vector must remain the executed assertion` |
| Blocking workflow step removed (AC4) | `QualityWorkflow_PinsAccessibilityVisualGate` **fails**: `workflow is missing the named step` |
| `if: false` added at the `accessibility-visual` job level | Governance **fails**: `the accessibility-visual job must not be conditionally skipped` |

**Results:** TypeScript typecheck passed; the browserless guard ran exactly 1 Chromium test and passed (exit 0, no web server); the Shell test project built Release with 0 warnings and 0 errors; all 80 `CiGovernanceTests` facts passed (Total: 80, Failed: 0), including both focused facts; `actionlint .github/workflows/quality.yml` passed; the range-scoped deferred-ledger check passed (`LEDGER_RANGE_CLEAN_OK`); `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` passed once the orchestrator-owned ledger edit was declared under `## Documented Unrelated Changes`; `git diff --check` reported no whitespace errors; every falsification mutation above was reverted and the baseline re-verified green.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` - orchestrator-owned sweep bookkeeping. The bmad-loop sweep flipped this bundle's source entry to `status: done 2026-09-02` with a `resolution-undo` token in the working tree after the delivery commit. It is not this story's authorship, this story's commit range does not touch it, and the intent's Never list makes it strictly read-only here.

## Auto Run Result

Status: done

Summary: Follow-up review pass over the delivered change. The blocking `accessibility-visual` job still executes the existing Unicode settings-persistence storage-key regression through a deterministic, Windows-compatible, browserless npm command. This pass closed the two ways that guard could have stayed green while guarding nothing -- a body-level `test.skip()` on the selected regression, and a retuned expectation literal that would have let the TypeScript mirror drift away from the .NET authority -- pinned the job header unconditional, bounded the step slice correctly, and replaced asserted-but-undemonstrated acceptance criteria with recorded falsification proofs.

Files changed:
- `.github/workflows/quality.yml` -- invokes the focused storage-key regression as a blocking browserless Playwright step, now with the dated rationale comment the sibling FC-NIP guard carries.
- `tests/e2e/package.json` -- defines the exact Chromium command with cross-platform serverless environment assignment and anchored test selection.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pins the wiring and, after this pass, also forbids a skipped/fixmed/focused selected regression, pins one golden-vector constant against both the Playwright expectation and `StorageKeysTests`, rejects a job-level `if:`, and bounds the settings step by its own block.
- `_bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- records the bundle intent, implementation, two review passes, falsification evidence, and final result.

Review findings breakdown: 7 patches applied (medium 2, low 5); 2 items deferred (both low, both pre-existing: shared Playwright artifact directories across browserless and hosted runs, and the three still-unexecuted `SettingsPage` mirror paths); 12 findings rejected -- disproven (`^`/`$` in the `--grep` argument are literal inside a cmd.exe quoted string and the script contains no `%`; a mangled pattern would exit 1 anyway, so selection is fail-closed at the runner), redundant (`ExtractNamedStep` already asserts job membership for the file-scoped `ShouldContain` checks), fail-closed-by-design (the deliberately broad `continue-on-error:` rejection and the whitespace-exact source regexes red on benign edits but never green on a real one), out of scope on the intent's own authority (extra Unicode/tenant vectors require rewriting the regression, which the Block If clause forbids; a full differential `BuildKey` linkage exceeds "the workflow wiring and the focused command", and the single-constant pin closes the in-scope half), or correct workflow semantics (`review_loop_iteration: 0` is the mandated reset for a follow-up review of a `done` spec; an empty Spec Change Log is correct when no bad_spec loopback occurred).

Follow-up review recommendation: true. Patched findings were high 0, medium 2, low 5; score = `3 x 2 + 1 x 5 = 11`, meeting the threshold of 5.

Verification: `npm --prefix tests/e2e run typecheck` passed; the focused npm guard passed exactly 1 Chromium test with no web server; the Shell test project built Release with 0 warnings and 0 errors; all 80 `CiGovernanceTests` facts passed; five falsification mutations each produced the expected red and were reverted with the baseline re-verified green; `actionlint`, the range-scoped deferred-ledger check, story-artifact validation, and `git diff --check` passed.

Residual risks: The complete hosted Windows `accessibility-visual` job was not run locally -- the exact cross-platform command ran on Linux without starting a web server, and static Governance coverage pins the Windows workflow wiring and the serverless configuration branch. The guard remains a single-vector regression by design: it proves the TypeScript mirror still produces the frozen key, and `StorageKeysTests` independently proves .NET does, but a change to `FrontComposerStorageKey` plus its own .NET test would move both pins together -- excluded by the intent's Never list rather than by a guard. The two deferred items above remain open. Runtime persistence code, the user-visible settings contract, dependencies, lockfiles, submodules, and the deferred-work ledger were unchanged.
