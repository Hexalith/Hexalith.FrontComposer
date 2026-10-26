---
title: 'Story 13.6: Repair hosted verification blockers'
type: 'bugfix'
created: '2026-10-25'
status: 'in-progress'
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

**Never:** Weaken gates, duplicate CI, change dependencies or authentication outside the approved local workload-identity amendment, invent manual facts, initialize nested submodules, commit/push or advance pins under this approval.

## Approved local workload-identity amendment — 2026-10-26

The user approved the proposed prerequisite amendment. Configure only the local FrontComposer AppHost/Keycloak workload identity required by the selected EventStore, through the existing platform API and a secret parameter. Provision matching private generated credentials for a confidential service-account client; preserve authentication enforcement, existing user clients and any existing user realm. Minimal host-owned credential/template support and focused regressions are included. Validate a fresh isolated realm, healthy EventStore, all four real 401/403 responses and the existing Epic 9 live proof. Dependency changes, publication and workflow-pin adoption remain outside this approval.

## I/O & Edge-Case Matrix

| Input / state | Required outcome |
| --- | --- |
| Unrelated versus matching DocFX exclusion | Unrelated passes; exact/recursive exclusions fail; missing matcher reports a bounded prerequisite error |
| Four projects; child consumes stdin | All four execute; missing, stale, malformed or zero-test TRX fails |
| Current migration guidance | Positive assertions match executable/manual classification; stale ownership and version/API guards remain |
| Fresh isolated local realm | Matching private workload credentials start healthy EventStore; existing user clients remain unchanged |
| Invalid bearer on four endpoints | Actual 401/403 passes; transport failure, redirects and other HTTP outcomes fail with sanitized diagnostics |

</frozen-after-approval>

## Code Map

- `quality.yml`: tools restore after Gate 2b; bearer probes suppress transport errors. `Test-DocFxExclusion` needs pinned DocFX despite `-SkipDocFx`.
- Shared CI consumes Builds `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, independently of the gitlink. Its loop shares stdin and omits report completeness checks.
- `eng/run-epic9-live-proof.sh`: reuse ownership/privacy and Node-test patterns; do not source its executable or change proof format. Keep rejection separate.

## Tasks & Acceptance

**Execution:**

- [x] `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — restore tools before governance; assert order and retain blocking docs validation.
- [x] `eng/validate-docs.ps1`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — report missing matcher without exposing paths; cover empty cache and existing exclusion cases.
- [x] `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` — replace three stale phrases with executable/versioned migration guides and explicit `--dry-run`/`--apply`; preserve ownership, metadata, API and version guards.
- [ ] `src/Hexalith.FrontComposer.AppHost/Program.cs`, `src/Hexalith.FrontComposer.AppHost/KeycloakRealms/hexalith-realm.json`, minimal host-owned credential/template helpers, and `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` with relevant fixtures — implement the approved local workload-identity amendment; verify secret handling, client/wiring consistency and unchanged existing client contracts.
- [ ] `eng/verify-apphost-invalid-bearer.sh` (new), `tests/e2e/scripts/verify-apphost-invalid-bearer.test.mjs` (new), `tests/e2e/package.json`, `.github/workflows/quality.yml` — extract rejection; diagnose/correct transport failure. Retain Aspire readiness, owned-process cleanup and four paths. Test transport-only fallback, all-unreachable, wrong HTTP status, discovery failure and cleanup; verify live.
- [x] `references/Hexalith.Builds/.github/workflows/domain-ci.yml`, `references/Hexalith.Builds/Tools/test-domain-workflow-test-platforms.ps1` — isolate child stdin; require fresh nonzero reports per project. Test consuming children and missing/stale/malformed/zero-result reports. Prepare active-base-policy/pin adoption for separate publication review.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`, primary `spec-13-6-verify-responsive-and-assistive-accessibility.md` — record commands/counts/revisions/CI links and blockers; retain seven Owner-reported Pass records and qualification gaps.

**Acceptance Criteria:**

- Given clean-runner verification, when checks execute, then matrix cases and relevant regressions pass without relaxed policy.
- Given the reviewed shared repair, when proposing publication, then source/test evidence is concrete; hosted acceptance awaits actual execution.
- Given unmet acceptance, when tracking is reconciled, then parent story/sprint remain in-progress.

## Implementation Notes

- Approved to continue on 2026-10-26. Implementation starts at `0f40ab06304e22c30c467ffb2279333897a79572`; the original baseline remains unchanged. Since planning, Story 15.4 independently updated `eng/validate-docs.ps1` and `DocsSiteValidationTests.cs`. Read and preserve those changes when implementing; they are not owned by this continuation. The spec itself was committed at `4f5ce9e67d0c07cc2ed049a4e8c644c8a448bb8a`. Parent story/sprint already remain in-progress.

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

### Local implementation verification — 2026-10-26

The [current result](13-6-accessibility-result.md#hosted-verification-repair--local-implementation-2026-10-26) owns commands, counts, local revision attribution, historical CI links and the publication/adoption handoff. Focused checks pass: DocsSiteValidationTests 64, CiGovernanceTests 86, migration tests 3, Epic 9 tooling tests 161 (23 rejection-helper checks), and Builds workflow assertions 287. Focused Debug builds and the source AppHost build have zero warnings/errors. Story artifact, shell syntax and both repository diff checks pass.

The rejection helper and regressions are implemented, but its task remains unchecked because live acceptance fails: four paths, eight curl exit-28/HTTP-000 attempts, zero 401/403 responses. Freshly built EventStore exits on missing workload-issuer ClientId/ClientSecret with JWT Authority configured; the current host and local realm provide no workload service account. Owned cleanup passes. The no-authentication-change boundary prevents repairing that prerequisite under this approval. Historical hosted transport cause is not proven. No commit/push, pin adoption or hosted rerun was performed; hosted acceptance and parent story/sprint remain in-progress. The frozen intent and all seven Owner-reported manual Pass records with their qualification gaps are unchanged.


### Parent audit and proposed prerequisite amendment — 2026-10-26

The parent inspected the complete baseline diff, including untracked files and the two Builds changes, and checked the actual verification outputs. The documentation, project-execution and migration matrix rows have passing coverage. Bearer handling has 23 passing harness tests, but the required live 401/403 observations are absent. Keep the live task unchecked and this spec in-progress; the workflow's final review follows completion of that task. Unrelated externally committed Story 15.4/15.5 changes are preserved. Parent whitespace normalization only restores required CRLF in the two JavaScript/TypeScript files; shell files retain LF.

**Proposed amendment, awaiting approval:** permit the minimum local AppHost/Keycloak workload-identity wiring needed by the selected EventStore, while keeping authentication enforcement and existing user clients intact:

- `src/Hexalith.FrontComposer.AppHost/Program.cs`: bind the local workload client through the existing platform `WithEventStoreWorkloadClientCredentials` API using a secret parameter.
- `src/Hexalith.FrontComposer.AppHost/KeycloakRealms/hexalith-realm.json` and minimal host-owned credential/template support if needed: provision the matching confidential service-account client with private generated credentials, reusing the platform's established pattern. Do not copy secrets into tracked files or reset an existing user realm.
- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` and relevant host fixtures: verify client/wiring consistency, secret handling and unchanged existing client contracts.
- Validate a fresh isolated local realm, healthy EventStore, all four real 401/403 responses, and the existing Epic 9 live proof; then resume final review. Dependency changes, publication and workflow-pin adoption remain outside this amendment.

No authentication amendment has been applied or approved. The existing frozen block remains verbatim.


### Amendment approval and resumed implementation — 2026-10-26

The user approved the prerequisite amendment. The frozen block now records that exception; its other restrictions remain. The earlier proposal and missing-credential results are historical. Resume at root `3668b603bf9cd176da86a2f3e020fd8fab2b58af`; external commit `29dcbfdeff32b964c581aa96a1b18d7dab20a71b` recorded the preceding implementation, and the selected Builds checkout is now `09af6ef818beb27490c87c00ed83dd581a7e6d8d`, containing the shared repair. Preserve the original frontmatter baseline and all external changes. The consumed shared-CI pin remains `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; a changed gitlink does not adopt that workflow.

Reuse `HexalithEventStoreTrustedEffectExtensions.WithEventStoreWorkloadClientCredentials` from the selected EventStore Aspire project. Its AppHost's `KeycloakRealmTemplate` and `LocalAuthenticationCredentials` supply a reference pattern, not permission to modify the dependency. Finish the existing live task and new host task, then the remaining Build review. Do not rerun completed unchanged lanes without a new concern; rerun affected host, live and tooling checks.
