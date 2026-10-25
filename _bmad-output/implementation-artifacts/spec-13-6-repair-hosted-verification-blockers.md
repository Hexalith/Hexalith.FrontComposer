---
title: 'Story 13.6: Repair hosted verification blockers'
type: 'bugfix'
created: '2026-10-25'
status: 'draft'
route: 'dispatch'
story_id: '13.6'
story_key: '13-6-i-ux-e-verify-responsive-and-assistive-accessibility'
review_loop_iteration: 0
baseline_commit: '9c4166e4b7cb45e2512e2b6db887a071f35ce244'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Quality fails documentation/bearer checks; shared CI succeeds without three configured project results.

**Approach:** Repair verification in each owning repository, prove failures cannot silently pass, and distinguish local from hosted results.

## Boundaries & Constraints

**Always:** Preserve Story 15.3 semantics, publication exclusions, authentication checks, test inventory and evidence attribution. Approval includes the two Builds files below; publication/pin adoption needs separate review.

**Never:** Weaken gates, duplicate CI, change authentication/dependencies, invent manual facts, initialize nested submodules, commit/push or advance pins under this approval.

## I/O & Edge-Case Matrix

| Input / state | Required outcome |
| --- | --- |
| Unrelated versus matching DocFX exclusion | Unrelated passes; exact/recursive exclusions fail; missing matcher reports a bounded prerequisite error |
| Four projects; child consumes stdin | All four execute; missing, stale, malformed or zero-test TRX fails |
| Current migration guidance | Positive assertions match executable/manual classification; stale ownership and version/API guards remain |
| Invalid bearer on four endpoints | Actual 401/403 passes; transport failure, redirects and other HTTP outcomes fail with sanitized diagnostics |

</frozen-after-approval>

## Code Map

- `quality.yml`: tools restore after Gate 2b; bearer probes suppress transport errors. `Test-DocFxExclusion` needs pinned DocFX despite `-SkipDocFx`.
- Shared CI consumes Builds `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, independently of the gitlink. Its loop shares stdin and omits report completeness checks.
- `eng/run-epic9-live-proof.sh`: reuse ownership/privacy and Node-test patterns; do not source its executable or change proof format. Keep rejection separate.

## Tasks & Acceptance

**Execution:**

- [ ] `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — restore tools before governance; assert order and retain blocking docs validation.
- [ ] `eng/validate-docs.ps1`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — report missing matcher without exposing paths; cover empty cache and existing exclusion cases.
- [ ] `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` — replace three stale phrases with executable/versioned migration guides and explicit `--dry-run`/`--apply`; preserve ownership, metadata, API and version guards.
- [ ] `eng/verify-apphost-invalid-bearer.sh` (new), `tests/e2e/scripts/verify-apphost-invalid-bearer.test.mjs` (new), `tests/e2e/package.json`, `.github/workflows/quality.yml` — extract rejection; diagnose/correct transport failure. Retain Aspire readiness, owned-process cleanup and four paths. Test transport-only fallback, all-unreachable, wrong HTTP status, discovery failure and cleanup; verify live.
- [ ] `references/Hexalith.Builds/.github/workflows/domain-ci.yml`, `references/Hexalith.Builds/Tools/test-domain-workflow-test-platforms.ps1` — isolate child stdin; require fresh nonzero reports per project. Test consuming children and missing/stale/malformed/zero-result reports. Prepare active-base-policy/pin adoption for separate publication review.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, primary `spec-13-6-verify-responsive-and-assistive-accessibility.md` — record commands/counts/revisions/CI links and blockers; retain seven Owner-reported Pass records and qualification gaps.

**Acceptance Criteria:**

- Given clean-runner verification, when checks execute, then matrix cases and relevant regressions pass without relaxed policy.
- Given the reviewed shared repair, when proposing publication, then source/test evidence is concrete; hosted acceptance awaits actual execution.
- Given unmet acceptance, when tracking is reconciled, then parent story/sprint remain in-progress.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- From `tests/e2e`: `npm run test:story-10-2`; `npm run test:epic-9-evidence`, extended with rejection tests.
- Build SourceTools/Shell tests individually in Debug; run built assemblies with `-class '*DocsSiteValidationTests'` / `-class '*CiGovernanceTests'`, `DiffEngine_Disabled=true`.
- From Builds: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1`.
- Run the extracted script against AppHost; require four 401/403 responses.
- `python3 eng/validate-story-artifacts.py`; `git diff --check` in each changed repository.
- Investigation: wording 0 passed/1 failed; matcher fails without installed tools; synthetic child executes 1/4 versus 4/4 with isolated stdin. Actual hosted consumer/transport cause remains unproven; diagnostics in `/tmp`.
- Current-HEAD [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562358) and [Quality](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40231562097) were running at inspection; no rerun requested.
