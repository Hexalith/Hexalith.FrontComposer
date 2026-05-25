---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-identify-targets', 'step-03-generate-tests', 'step-03c-aggregate', 'step-04-validate-and-summarize']
lastStep: 'step-04-validate-and-summarize'
lastSaved: '2026-05-20T19:06:58+02:00'
author: 'Murat (TEA)'
storyId: '12.4'
storyKey: '12-4-trusted-release-evidence-dry-run'
scope: 'Apply F3 + F4 + F5 from the Story 12.4 test review (test-review-12-4.md)'
detectedStack: 'fullstack (.NET xUnit v3 + Shouldly + Playwright E2E)'
testFramework: 'xUnit v3 + Shouldly + NSubstitute; Playwright + TypeScript + axe-core; PactNet contract assets'
executionMode: 'BMad-Integrated'
filesModified:
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs
helpersAdded:
  - 'FindStepBlockContaining(workflow, needle): scoped step-block search (F4 prerequisite)'
  - 'ExtractJobPermissionsBlock(workflow, jobId): job-scoped permissions extraction (F4 prerequisite)'
testsAffected:
  - 'Story12_4_Def102_FallbackApprovedAtEqualsExpiresAtFixture_IsPresentAndBlocked (added — red-phase)'
  - 'Story12_4_Def103_FallbackApprovedAtExactly365DaysOldFixture_IsPresentAndBlocked (added — red-phase)'
  - 'Story12_4_Def104_PartialPublishRecoveredAndFullFixtures_ArePresentAndRequireRerunReview (added — red-phase)'
  - 'Story12_4_Def105_StringBooleanSymmetryFixtures_ArePresentAndBlocked (added — red-phase)'
  - 'Story12_4_Def106_DangerousEvidenceFixtures_CoverCredentialedUrlsAndSigningMaterial (added — red-phase)'
  - 'Story12_4_Def107_FallbackAffectedArtifactMismatchFixture_IsPresentAndBlocked (added — red-phase)'
  - 'Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow (refactored — F4)'
  - 'Story12_4_Def14_AttestationsWritePermission_IsRestored (refactored — F4)'
  - 'Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent (extended — F3 classifier round-trip)'
  - 'Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked (rebuilt — F5 single-axis manifest)'
inputDocuments:
  - _bmad-output/test-artifacts/test-reviews/test-review-12-4.md
  - _bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md
  - _bmad-output/test-artifacts/atdd-checklist-12-4-trusted-release-evidence-dry-run.md
  - tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
  - eng/release_evidence.py
  - .github/workflows/release.yml
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md
  - .claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md
---

# Automation Expansion Preflight — Create Run 2026-05-20

## Step 1 — Preflight & Context

Detected stack: fullstack.

Framework readiness: passed.

- Frontend/browser automation: `tests/e2e/playwright.config.ts` plus `tests/e2e/package.json` with `@playwright/test`, `@axe-core/playwright`, TypeScript, and faker.
- Backend/unit automation: solution `Hexalith.FrontComposer.slnx` plus xUnit test projects under `tests/Hexalith.FrontComposer.*.Tests`.
- Existing Pact coverage: `tests/Hexalith.FrontComposer.Shell.Tests/Pact` and `PactNet` in the Shell test project.
- Existing E2E conventions: `data-testid` is configured as the Playwright test id attribute; current specs include smoke, lifecycle, sidebar responsiveness, density transitions, and specimen accessibility/visual coverage.

Execution mode: BMad-integrated.

Loaded BMad context and artifacts:

- `_bmad-output/project-context.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/prd/index.md`
- `_bmad-output/planning-artifacts/epics/index.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Existing story artifacts under `_bmad-output/implementation-artifacts`
- Existing test artifacts under `_bmad-output/test-artifacts`

Loaded knowledge fragments:

- Core: `test-levels-framework.md`, `test-priorities-matrix.md`, `data-factories.md`, `selective-testing.md`, `ci-burn-in.md`, `test-quality.md`
- Playwright utilities: `overview.md`, `api-request.md`, `network-recorder.md`, `auth-session.md`, `intercept-network-call.md`, `recurse.md`, `log.md`, `file-utils.md`, `burn-in.md`, `network-error-monitor.md`, `fixtures-composition.md`
- Traditional baseline: `fixture-architecture.md`, `network-first.md`
- Specialized: `contract-testing.md` because Pact assets are present and Pact.js utilities are disabled
- Browser automation: `playwright-cli.md` because `tea_browser_automation` is `auto`

Relevant TEA config:

- `tea_use_playwright_utils: true`
- `tea_use_pactjs_utils: false`
- `tea_pact_mcp: none`
- `tea_browser_automation: auto`
- `test_stack_type: auto`

Preflight notes:

- Submodules are present in the workspace; no recursive submodule initialization or update was performed.
- The existing `automation-summary.md` contains a prior completed Story 12.4 run. This section records the new Create-mode preflight before target selection.

## Step 2 — Coverage Plan

Selected target: Story 12.4 release-evidence classifier red-phase automation for the newest deferred review gaps, `CR-12-4-Def102` through `CR-12-4-Def107`.

Why this target:

- The sprint status marks Epic 12 complete, but `deferred-work.md` contains fresh Story 12.4 follow-ups from 2026-05-20 that are explicit test gaps.
- Existing red-phase scaffolds already live in `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs`, outside the class-level Governance trait, with per-method `Quarantined` metadata. Reusing this pattern avoids main-lane breakage.
- The gaps map directly to `eng/release_evidence.py` and `tests/ci-governance/fixtures/release-readiness-cases.json`; no provider endpoint map is required because this target is not a consumer-driven Pact generation task.
- Browser exploration was attempted with `playwright-cli -s=tea-automate open http://127.0.0.1:5070`, but no app was listening (`ERR_CONNECTION_REFUSED`). The session was closed with `playwright-cli -s=tea-automate close`, so target selection relies on source and artifact analysis.

Coverage plan:

| Target | Test Level | Priority | Planned Spec | Justification |
| --- | --- | --- | --- | --- |
| Def102 fallback `approved_at >= expires_at` boundary | Unit/integration governance | P1 | Red-phase classifier fixture assertion that a case with equal timestamps exists and blocks | Regression pin for fallback approval validity; contained to release evidence classifier |
| Def103 exact 365-day approval boundary | Unit/integration governance | P1 | Red-phase fixture/assertion that a 365-day-old approval is represented and blocks | Pins off-by-one behavior named in round-13 review |
| Def104 `partial_publish_state` values `recovered` and `full` | Unit/integration governance | P1 | Red-phase cases requiring both states to classify as `rerun-review` / blocked | Prevents owner-review bypass after partial/recovered publish states |
| Def105 asymmetric string boolean coverage | Unit/integration governance | P2 | Red-phase required cases for string true approval and string false concurrency | Completes typed parsing matrix without duplicating existing string false/true axes |
| Def106 dangerous evidence patterns for credentialed URLs and signing material | Unit/integration governance | P0 | Red-phase cases requiring raw evidence with credentialed URL and certificate/private-key marker to block | Security/redaction gap; high impact if unsafe evidence leaks into artifacts |
| Def107 fallback affected artifact mismatch | Unit/integration governance | P0 | Red-phase case requiring fallback approval for artifact X while manifest ships artifact Y to block | Release authorization integrity gap tied to AC34 invalidation trigger |

Out of scope for this automation pass:

- Implementing `eng/release_evidence.py` behavior changes. These red-phase tests intentionally define the missing contracts and remain quarantined until the matching implementation lands.
- Pact provider endpoint mapping. Pact assets exist, but Pact.js utilities are disabled and the chosen target is release governance, not consumer-provider contract generation.
- E2E browser tests. The sample app was not running and the selected gaps are classifier/governance contracts, not UI flows.

## Step 3C — Test Generation Aggregate

Generation mode: sequential.

Reason: `tea_execution_mode` is `auto`, but subagent delegation was not authorized in this turn, so the API, E2E, and backend workers were represented as sequential worker outputs and aggregated through the BMad schema.

Worker outputs:

- API: 0 tests. No API endpoint tests generated because the selected targets are backend release-evidence classifier contracts.
- E2E: 0 tests. Browser exploration found no running app at `http://127.0.0.1:5070`, and the selected targets are not UI flows.
- Backend: 6 quarantined red-phase tests in `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs`.

Generated backend tests:

| Test | Priority | Contract |
| --- | --- | --- |
| `Story12_4_Def102_FallbackApprovedAtEqualsExpiresAtFixture_IsPresentAndBlocked` | P1 | Requires an exact `approved_at == expires_at` fallback fixture and classifier block. |
| `Story12_4_Def103_FallbackApprovedAtExactly365DaysOldFixture_IsPresentAndBlocked` | P1 | Requires an exact 365-day fallback approval boundary fixture and classifier block. |
| `Story12_4_Def104_PartialPublishRecoveredAndFullFixtures_ArePresentAndRequireRerunReview` | P1 | Requires `recovered` and `full` partial-publish states to route to `rerun-review`, block, and deny publish. |
| `Story12_4_Def105_StringBooleanSymmetryFixtures_ArePresentAndBlocked` | P2 | Requires inverse string-boolean fixtures for approval and concurrent publish parsing. |
| `Story12_4_Def106_DangerousEvidenceFixtures_CoverCredentialedUrlsAndSigningMaterial` | P0 | Requires credentialed URL and PEM signing-material raw evidence fixtures to block as unsafe evidence. |
| `Story12_4_Def107_FallbackAffectedArtifactMismatchFixture_IsPresentAndBlocked` | P0 | Requires fallback affected-artifact mismatch to block. |

Shared helpers added to the red-phase test class:

- `ReleaseReadinessFixturesPath`
- `RequireFixtureCase`
- `ClassifyReleaseReadinessFixtures`
- `RequireClassifierResult`
- `BlockingReasonsContain`
- `DeleteIfExists`

Summary statistics saved to `_bmad-output/test-artifacts/automation-temp/tea-automate-summary-2026-05-20T19-06-58-482+02-00.json`.

Summary:

- Stack type: fullstack (.NET xUnit v3 + Shouldly + Playwright E2E)
- Total tests generated: 6
- API tests: 0
- E2E tests: 0
- Backend tests: 6 in 1 file
- Fixtures created: 0
- Priority coverage: P0 = 2, P1 = 3, P2 = 1, P3 = 0
- Performance: baseline (no parallel speedup)

## Step 4 — Validation & Summary

Checklist status:

- Framework readiness: passed. Playwright E2E scaffolding and xUnit backend test projects are present.
- Coverage mapping: passed. The generated tests map to `CR-12-4-Def102` through `CR-12-4-Def107`.
- Test quality and structure: passed. Tests follow the existing quarantined red-phase pattern, use deterministic fixture lookup and classifier round trips, and clean temp classifier outputs with `try/finally`.
- Fixtures/factories/helpers: N/A for new fixtures/factories. Helper methods were added inside `Story12_4_RedPhaseDefTests` because the target is release-governance fixture validation, not Playwright data setup.
- CLI sessions cleaned up: passed. The `playwright-cli -s=tea-automate close` command was run after the refused browser-open attempt.
- Temp artifacts: passed. Worker outputs and summary JSON were moved from `/tmp` into `_bmad-output/test-artifacts/automation-temp/`.

Validation commands:

```powershell
dotnet build tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release
```

Result: build succeeded with 0 warnings and 0 errors.

```powershell
dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Story12_4_Def102|FullyQualifiedName~Story12_4_Def103|FullyQualifiedName~Story12_4_Def104|FullyQualifiedName~Story12_4_Def105|FullyQualifiedName~Story12_4_Def106|FullyQualifiedName~Story12_4_Def107"
```

Result: 6 failed, 0 passed, 0 skipped. This is the expected red-phase outcome; each failure occurs at `RequireFixtureCase(...)` because the named release-readiness fixture does not yet exist.

Failure axes:

| Test | Current red reason |
| --- | --- |
| Def102 | Missing `fallback-approved-at-equals-expires-at` fixture. |
| Def103 | Missing `fallback-approved-at-365-day-boundary` fixture. |
| Def104 | Missing `partial-publish-state-recovered` fixture. |
| Def105 | Missing `string-true-approval` fixture. |
| Def106 | Missing `credentialed-url-leakage` fixture. |
| Def107 | Missing `fallback-affected-artifact-mismatch` fixture. |

```powershell
dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Story12_4_Def10&Category!=Quarantined"
```

Result: no tests matched, confirming the newly added Def102-Def107 tests remain excluded from non-quarantined lanes.

Files updated:

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs`
- `_bmad-output/test-artifacts/automation-summary.md`
- `_bmad-output/test-artifacts/automation-temp/tea-automate-api-tests-2026-05-20T19-06-58-482+02-00.json`
- `_bmad-output/test-artifacts/automation-temp/tea-automate-e2e-tests-2026-05-20T19-06-58-482+02-00.json`
- `_bmad-output/test-artifacts/automation-temp/tea-automate-backend-tests-2026-05-20T19-06-58-482+02-00.json`
- `_bmad-output/test-artifacts/automation-temp/tea-automate-summary-2026-05-20T19-06-58-482+02-00.json`

Recommended next workflow: implement the green-phase release-evidence fixture and classifier changes for Def102-Def107, then run `bmad-testarch-test-review` in Validate mode against the changed tests.

# Automation Expansion — Story 12.4 Test-Review Findings F3 + F4 + F5

## Goal

Apply the three "Strongly Recommended" findings from [test-review-12-4.md](test-reviews/test-review-12-4.md):

- **F3** — Add classifier round-trip to Def22 so it pins both fixture shape AND classifier enforcement (mirror Def25's gold-standard pattern).
- **F4** — Refactor both Def14 tests from raw `Contains`/`IndexOf` on the full workflow text to structured step-body and job-permissions assertions.
- **F5** — Rebuild the Def23 manifest fixture so only `release_definition_fingerprints: {}` is the failing axis (eliminates seal-hash conflation).

## What changed

### Helpers added

Two new private static helpers next to the existing `ExtractNamedStep` in [CiGovernanceTests.cs](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs):

- **`FindStepBlockContaining(string workflow, string needle)`** — returns the workflow step block (between `      - name:`/`      - uses:` boundaries) whose body contains `needle`. Used by F4 to assert the attestation step exists as a real wired step, not just text in a comment or doc anchor.

- **`ExtractJobPermissionsBlock(string workflow, string jobId)`** — returns the contents of the named job's `permissions:` block (4-space-indented key, 6-space-indented entries), stopping at the next 2-space-indented job header so sibling jobs are never read. Used by F4 to scope `attestations: write` / `id-token: write` assertions to the release job specifically.

### F4 — Def14 tests refactored

**`Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`**
- Old: `workflow.Contains("actions/attest-build-provenance@v2")` + `IndexOf("Run semantic-release")` ordering
- New: `FindStepBlockContaining(workflow, "actions/attest-build-provenance@v2")` returns the actual step block; assert it is non-empty, has no `if: false`, no `continue-on-error: true`; ordering anchored to `ExtractNamedStep(workflow, "Run semantic-release (live publish)")` rather than the first textual match of "Run semantic-release"
- Closes three F4 blind spots: comments/doc anchors, step-level skip flags, ambiguous "Run semantic-release" positions

**`Story12_4_Def14_AttestationsWritePermission_IsRestored`**
- Old: `workflow.Contains("attestations: write")` + `workflow.Contains("id-token: write")` on the full file text
- New: `ExtractJobPermissionsBlock(workflow, "release")` scopes to the release job's permissions block; assertions distinguish workflow-level from job-level permission scope; also asserts the narrowed `attestations: read` from round-8 CR-12-4-P189 is REPLACED (not duplicated)
- Closes the F4 partial-green footgun where workflow-level `attestations: write` could shadow job-level `attestations: read`

### F3 — Def22 classifier round-trip

`Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent` gains a second phase after the existing fixture-shape assertions. The new block:
- Runs `python eng/release_evidence.py classify-fixtures --fixtures … --output <temp>` against the live fixtures file
- Asserts the result entry named `dry-run-with-side-effect-attempt` exists, classifies as `blocked`, and reports `publish_authorized=false`
- Wraps the temp output file in `try/finally` + `File.Delete` (F1/F2 cleanup pattern applied at the new call site)
- Mirrors the pattern that makes Def25 the gold-standard test in this file (fixture shape + classifier round-trip + typed diagnostic)

Today the new block is structurally unreachable because the existing first assertion (`matchingCase.ShouldNotBeNull`) already fails RED until Def22 adds the fixture. Once Def22 lands, the round-trip runs and pins the AC24 contract end-to-end.

### F5 — Def23 single-axis manifest

`Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked` rebuilt:

**Before (F5):** The manifest fixture had `seal.hash="0"` and `packages: []`. Today's exit-code assertion passed for the wrong reason — the seal check fired ("manifest seal with sha256 hash is required") and the empty-packages check fired ("package rows are required"). The fingerprints contract was never exercised on the exit-code axis.

**After (F5):** The fixture is an unsealed manifest with all top-level fields valid (concrete sha256 hashes for sbom/benchmark), a single fully-valid package row with all `REQUIRED_ROW_FIELDS` populated and `signing_status=verified`, `timestamp_status=verified`, `attestation_status=attested`, and a sha256-shaped checksum that doesn't start with `nupkgs/`. The test then shells out to `python eng/release_evidence.py seal-manifest --manifest <unsealed> --output <sealed>` so the helper itself computes the canonical seal hash (avoiding C#-side re-implementation of Python's `json.dumps(sort_keys=True, separators=(",",":"))` canonicalization).

Net effect: today, `verify-manifest --no-root` on the sealed fixture exits 0 (all checks pass; the fingerprints check is gated by `root is not None`). The test fails RED on `result.ExitCode.ShouldNotBe(0)` — the precise contract Def23 closes. After Def23 lands, the fingerprints check fires under `--no-root`, exit becomes non-zero, the diagnostic contains `release_definition_fingerprints`, and the test transitions cleanly RED → GREEN.

Both temp files (unsealed + sealed) cleaned up via `try/finally`.

## Validation

### Build

```
cd D:\Hexalith.FrontComposer
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release
# Build succeeded. 0 Warning(s) 0 Error(s) — Time Elapsed 00:00:19.61
```

### RED-phase verification

```
dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj `
  --configuration Release --no-build `
  --filter "FullyQualifiedName~Story12_4_Def"
# Failed: 5, Passed: 0, Skipped: 0, Total: 5
```

Each test fails on the precise axis pinned by its Def item:

| Test | Fail axis (post-refactor) |
|---|---|
| Def14 T1 | `attestStep.ShouldNotBeNullOrEmpty` — step missing |
| Def14 T2 | `jobPermissions.Contains("attestations: write")` is False — job-scoped, not file-wide |
| Def22 | `matchingCase.ShouldNotBeNull` — fixture missing (F3 round-trip is unreachable until fixture lands, by design) |
| Def23 | `result.ExitCode.ShouldNotBe(0)` — manifest is otherwise valid; only fingerprints gate fires |
| Def25 | `matchingCase.ShouldNotBeNull` — fixture missing (unchanged from prior; F1/F2 cleanup only) |

### Main-lane exclusion

```
dotnet test ... --filter "FullyQualifiedName~Story12_4_Def&Category!=Quarantined"
# (No test matches the given run settings.)
```

All 5 tests remain correctly quarantined.

## Scope guardrails honored

- No changes to `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, `eng/release-package-inventory.json`, or `tests/ci-governance/fixtures/release-readiness-cases.json` (red-phase invariant: tests describe the contract, green-phase patches change the implementation)
- No new packages, no analyzer changes, no DI changes
- Same convention as the rest of the file: `string.Contains(value, StringComparison.Ordinal).Should[True|False](message)` rather than `string.ShouldContain(value, message)` (Shouldly version doesn't have the 2-arg customMessage overload for those extension methods on string)
- CRLF, UTF-8, 4-space indent, file-scoped namespace, existing brace/spacing style preserved
- No submodule (`Hexalith.EventStore/**`, `Hexalith.Tenants/**`) edits

## Findings left open (deferred per review)

- **F6** — `GetBoolean()` fragility on Def22/Def25 (low; fixture-author drift unlikely)
- **F7** — Pin exact diagnostic string for Def23 (depends on the verbiage the Def23 author picks; add when Def23 lands)
- **F8** — `.Single` → `.SingleOrDefault` + Shouldly on Def25 (polish)
- **F9** — Shared fixture-lookup helper across Def22/25 (polish; opportunistic)
- **F10** — Process timeout hardening (infra-level; future story)
- **Shared `TempFile` helper** — first considered post-F1/F2; would now retrofit four call sites (Def22 output, Def23 unsealed, Def23 sealed, Def25 output). Reasonable next consolidation if this pattern grows further.

## Recommended next steps

1. **Commit** as one focused change — F3+F4+F5 are conceptually a single "tighten the red-phase contracts" PR. (F1+F2 already applied earlier and untouched here.)
2. **Apply F-Def14 green-phase**: wire the `actions/attest-build-provenance@v2` step into `release.yml` before "Run semantic-release (live publish)", restore `attestations: write` + `id-token: write` to the release job permissions. Both Def14 tests should turn GREEN together; remove the `[Trait("Category", "Quarantined")]` and metadata comment in the same change.
3. **Apply F-Def22 green-phase**: add the `dry-run-with-side-effect-attempt` fixture case to `tests/ci-governance/fixtures/release-readiness-cases.json`. The F3 round-trip will then exercise the classifier contract.
4. **Apply F-Def23 green-phase**: extend `verify-manifest --no-root` in `eng/release_evidence.py` to enforce the `release_definition_fingerprints` contract (either by lifting the `if root is not None and root_exists:` gate for the fingerprints check, or by adding a dedicated `--require-fingerprints` flag).
5. **Apply F-Def25 green-phase**: add the `packages-empty-array` fixture; ensure `manifest_diagnostics` queues "package rows are required" for the empty-array path identical to the null path.
6. **Re-run** `/bmad-testarch-test-review` in Validate mode after green-phase work to confirm fixes hold.

---

_Automation expansion completed 2026-05-20. Murat signing off — 3 review findings closed, all 5 red-phase tests still RED on the correct contract, 0 build warnings, 0 build errors._
