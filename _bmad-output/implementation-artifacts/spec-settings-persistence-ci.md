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
  - summary: >-
      The seven browser-driven tests in `settings-persistence.spec.ts` -- the AC1 keyboard entry
      point and the AC2 persist-across-reload claims -- are executed by no CI lane at all.
    evidence: |-
      `test:a11y` resolves to `playwright test specs/specimen-accessibility.spec.ts --project=chromium`,
      and no other workflow step names this spec, so after this bundle the only executed test in the
      file is the pure storage-key helper. The file's own header presents AC1/AC2 as the gaps it
      exists to close. Pre-existing: no lane ever ran them; this bundle's intent scoped execution to
      the single Unicode regression and its Block If clause forbids starting the Counter web host.
    location: >-
      tests/e2e/specs/settings-persistence.spec.ts:34-213
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

### 2026-09-03 -- Operational escalation resolution
- No feature-intent, acceptance-criteria, or test-matrix change was required. The run paused after successful review because bmad-loop 0.11.1 generated a non-Conventional sweep commit subject.
- For this paused sweep, the local `[scm]` commit template is `docs(bmad-loop): resolve {story_key}`. Clear the template before future non-sweep story runs so their semantic commit type remains specific to their change.

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

### 2026-09-02 -- Second follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 4: (high 0, medium 2, low 2)
- defer: 1: (high 0, medium 0, low 1)
- reject: 23: (high 0, medium 0, low 23)
- addressed_findings:
  - `[medium]` `[patch]` The golden-vector and declaration pins were counted file-wide, so relocating the executed assertion into a differently titled sibling test kept both counts at 1 while the `--grep`-selected test asserted nothing. Proved by mutation: Playwright reported `1 passed` and exited 0. Both the assertion and its input are now bound to the selected test's own block, sliced from the pinned declaration to its top-level terminator.
  - `[medium]` `[patch]` The .NET half of the mirror pin was a whole-file `ShouldContain` over `StorageKeysTests.cs`, satisfied by the literal surviving in a comment, an unused constant, or a `[Fact(Skip = ...)]` -- so the runtime authority could stop executing while this fact stayed green. It is now bound to a non-skipped `[Fact]` block that must contain both the frozen input and the `key.ShouldBe(...)` line.
  - `[low]` `[patch]` The frozen Unicode input identity was unpinned on both sides, so a coordinated input-plus-mirror retune could keep the golden vector green. Both blocks now pin the same `\u0130\u03A3@Example.COM` literal -- **case-sensitively**: Shouldly's `string.ShouldContain` defaults to `Case.Insensitive`, which silently defeated the casing pin until the falsification mutation exposed it.
  - `[low]` `[patch]` `test.info().skip()` remained a body-level skip route that the `test.skip(`/`test.fixme(` bans did not cover, because the pinned signature takes no `testInfo` argument. Proved by mutation: Playwright reported `1 skipped` and exited 0. It is now forbidden inside the selected test block.

## Implementation Notes

- Added a cross-platform browserless npm command whose anchored Playwright title selects only the existing Unicode storage-key regression.
- Added the command as a non-advisory `accessibility-visual` step before the hosted accessibility and visual suite.
- Extended the existing CI governance facts without adding a new test identifier; they pin step ordering, blocking behavior, cross-platform environment assignment, and the exact focused command.
- Runtime storage canonicalization, the Playwright helper/regression, dependency metadata, submodules, and the deferred-work ledger remain unchanged.
- Follow-up review pass hardened the guard against the two ways it could have stayed green while guarding nothing: a skipped selected test, and a retuned expectation literal. Both mirrors of the Unicode golden vector are now pinned to a single governance constant.
- The workflow job header is pinned unconditional, and the settings step is bounded by its own step block rather than by the next `- name:` boundary.
- Second follow-up review pass closed the remaining relocation and liveness holes: the executed assertion and the frozen input are now bound to the selected test's own block on the TypeScript side, and to a non-skipped `[Fact]` block on the .NET side, so neither mirror can be moved, retuned, or disabled without reddening Governance.

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

**Falsification proofs (second follow-up review pass, 2026-09-02):** each mutation was applied, observed, and reverted.

| Mutation | Observed |
|----------|----------|
| Executed assertion relocated into a differently titled sibling test | Playwright **exits 0** (`1 passed`) with the selected test asserting nothing; Governance now **fails**: `the golden-vector assertion must live inside the selected regression` |
| `[Fact]` on `BuildKey_UnicodeEmailIdentity_MatchesInvariantRuntimeGoldenVector` changed to `[Fact(Skip = ...)]` | Governance **fails**: `the .NET runtime authority must stay an unconditional, non-skipped [Fact]` (the previous whole-file `ShouldContain` stayed green) |
| Frozen input retuned to `\u0130\u03A3@example.com` | Governance **fails**: `the selected regression must keep the frozen Unicode input identity` -- only after the pin was made `Case.Sensitive`; with Shouldly's default `Case.Insensitive` this mutation passed |
| `test.info().skip(true, ...)` added to the selected regression body | Playwright **exits 0** (`1 skipped`); Governance now **fails**: `the selected regression must not be able to skip itself at runtime` |

**Results (second follow-up review pass, 2026-09-02):** `npm --prefix tests/e2e run typecheck` passed; the browserless guard ran exactly 1 Chromium test and passed (`1 passed`, exit 0, no web server); the Shell test project built Release with 0 warnings and 0 errors; all 80 `CiGovernanceTests` facts passed (Total: 80, Failed: 0) and all 17 `StorageKeysTests` facts passed (Total: 17, Failed: 0, Skipped: 0); `actionlint .github/workflows/quality.yml` passed; the range-scoped deferred-ledger check passed (`LEDGER_RANGE_CLEAN_OK`); `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` passed; `git diff --check` reported no whitespace errors; every falsification mutation in the table above was reverted, the project rebuilt, and the baseline re-verified green.

**Results (first follow-up review pass):** TypeScript typecheck passed; the browserless guard ran exactly 1 Chromium test and passed (exit 0, no web server); the Shell test project built Release with 0 warnings and 0 errors; all 80 `CiGovernanceTests` facts passed (Total: 80, Failed: 0), including both focused facts; `actionlint .github/workflows/quality.yml` passed; the range-scoped deferred-ledger check passed (`LEDGER_RANGE_CLEAN_OK`); `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` passed once the orchestrator-owned ledger edit was declared under `## Documented Unrelated Changes`; `git diff --check` reported no whitespace errors; every falsification mutation above was reverted and the baseline re-verified green.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` - orchestrator-owned sweep bookkeeping. The bmad-loop sweep flipped this bundle's source entry to `status: done 2026-09-02` with a `resolution-undo` token in the working tree after the delivery commit. It is not this story's authorship, this story's commit range does not touch it, and the intent's Never list makes it strictly read-only here.

## Auto Run Result

Status: done

Summary: Second follow-up review pass over the delivered change. The blocking `accessibility-visual` job still executes the existing Unicode settings-persistence storage-key regression through a deterministic, Windows-compatible, browserless npm command. The previous pass proved the guard could not be silently skipped or its expectation retuned; this pass found that both of those pins were still counted *file-wide* rather than inside the artifact they claim to protect, and closed the three remaining ways the blocking guard could have stayed green while guarding nothing: relocating the executed assertion into a sibling test, disabling the .NET runtime authority behind `[Fact(Skip = ...)]`, and self-skipping at runtime via `test.info().skip()`. It also pinned the frozen Unicode *input* identity, which had been unpinned on both sides.

Files changed:
- `.github/workflows/quality.yml` -- unchanged this pass; still invokes the focused storage-key regression as a blocking browserless Playwright step with its dated rationale comment.
- `tests/e2e/package.json` -- unchanged this pass; still defines the exact Chromium command with cross-platform serverless environment assignment and anchored test selection.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- the executed assertion and the frozen input are now bound to the selected test's own block; the .NET mirror is bound to a non-skipped `[Fact]` block rather than a whole-file substring; `test.info(` is forbidden in the selected test body; both input pins are `Case.Sensitive`.
- `_bmad-output/implementation-artifacts/spec-settings-persistence-ci.md` -- records the bundle intent, implementation, three review passes, nine falsification proofs, three deferred items, and final result.

Review findings breakdown: 4 patches applied (high 0, medium 2, low 2); 1 item deferred (low, pre-existing: the seven browser-driven tests in the same spec file are executed by no CI lane, since `test:a11y` resolves only to `specimen-accessibility.spec.ts`); 23 findings rejected -- disproven by inspection or by running them (`expect.soft(` cannot match the pinned `expect(key).toBe(` regex, so it reddens rather than hides; a `test.describe` wrapper indents the declaration and breaks the column-0 pin; `test.slow()` does not skip; neither composed fixture is auto or skipping; a non-matching `--grep` exits 1, so both the triplicated grep literal and Playwright's unpinned title-path format fail closed; the Windows `cmd.exe` quoting concern was already disproven and is fail-closed regardless; the "no proof the guard fails" claim is answered by nine recorded mutations), fail-closed-by-design (the deliberately broad `continue-on-error:` rejection, the explicit-`false` variant, the whitespace-exact source regexes, the un-stripped `//` comments, and the file-wide skip bans all red on benign edits but never green on a real one), blocked by an established constraint (splitting into a new named `[Fact]` would trip the GOV-1/11.19 analyzer-policy test-identifier ledger, which this bundle must not reseal), speculative future edits (`needs:` cascade, `globalSetup`/`globalTeardown`, two-space block scalars), out of scope on the intent's own authority (full semantic mirror equivalence and extra vectors require rewriting the regression, which the Block If clause forbids; step reordering for fail-fast was never specified), or cosmetic (comment wording that matches the intent contract's own phrasing, an undocumented ordering rationale, an unreachable `IndexOf` -1 path).

Follow-up review recommendation: true. Patched findings were high 0, medium 2, low 2; score = `3 x 2 + 1 x 2 = 8`, meeting the threshold of 5.

Verification: `npm --prefix tests/e2e run typecheck` passed; the focused npm guard passed exactly 1 Chromium test with no web server; the Shell test project built Release with 0 warnings and 0 errors; all 80 `CiGovernanceTests` facts and all 17 `StorageKeysTests` facts passed with 0 skipped; four new falsification mutations each produced the expected red and were reverted with the project rebuilt and the baseline re-verified green; `actionlint`, the range-scoped deferred-ledger check, story-artifact validation, and `git diff --check` passed.

Residual risks: The complete hosted Windows `accessibility-visual` job was not run locally -- the exact cross-platform command ran on Linux without starting a web server, and static Governance coverage pins the Windows workflow wiring and the serverless configuration branch. The guard remains a single-vector regression by design: it now proves that the same frozen input produces the same frozen key on both sides and that both assertions actually execute, but a change to `FrontComposerStorageKey` semantics for any *other* input, or a coordinated edit that moves both pinned literals together, is excluded by the intent's Never list rather than by a guard. Every new pin is a source-text assertion, so a reformat of either pinned block reddens a blocking lane with no behavioral change. The three deferred items above remain open.
