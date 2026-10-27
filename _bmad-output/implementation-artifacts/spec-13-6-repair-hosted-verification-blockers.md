---
title: 'Story 13.6: Repair hosted verification blockers'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
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

## Approved isolated-readiness amendment — 2026-10-26

The user approved the isolated-readiness proposal recorded below. Extend the local identity work with opt-in, owned disposable Redis/Dapr isolation through the selected platform's existing component-path and endpoint APIs. Preserve existing datastores, other AppHosts and ordinary development behavior. In that verified isolated environment only, take and verify a real backup, establish writer/retry-worker quiescence, invoke the existing authenticated writer-v2 activation API with actual source identity and attestations, and verify persisted marker state. Require direct healthy application health/readiness, the four rejection outcomes and the unchanged Epic 9 proof before final review. Minimal host-owned support, verification-runner changes and focused regressions are approved. Dependency edits, shared-store maintenance, publication and pin adoption remain excluded. Report an actual missing platform isolation capability before expanding that boundary.

## I/O & Edge-Case Matrix

| Input / state | Required outcome |
| --- | --- |
| Unrelated versus matching DocFX exclusion | Unrelated passes; exact/recursive exclusions fail; missing matcher reports a bounded prerequisite error |
| Four projects; child consumes stdin | All four execute; missing, stale, malformed or zero-test TRX fails |
| Current migration guidance | Positive assertions match executable/manual classification; stale ownership and version/API guards remain |
| Fresh isolated local realm | Matching private workload credentials start healthy EventStore; existing user clients remain unchanged |
| Disposable readiness topology | Only owned isolated resources are mutated; verified backup/quiescence and persisted v2 activation lead to healthy application health/readiness; unknown/shared resources are refused and owned resources are cleaned |
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
- [x] `src/Hexalith.FrontComposer.AppHost/Program.cs`, `src/Hexalith.FrontComposer.AppHost/KeycloakRealms/hexalith-realm.json`, minimal host-owned credential/template helpers, and `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` with relevant fixtures — implement the approved local workload-identity amendment; verify secret handling, client/wiring consistency and unchanged existing client contracts.
- [x] `src/Hexalith.FrontComposer.AppHost/Program.cs`, minimal host-owned isolation support, `eng/verify-apphost-invalid-bearer.sh` or a minimal companion runner, and focused tests beside the existing Node/host tests — implement the approved disposable-readiness amendment using existing platform APIs; verify isolation/refusal/cleanup, actual backup and quiescence, authenticated activation and persisted marker, healthy application health/readiness, then rerun the four-route verifier and unchanged Epic 9 proof.
- [x] `eng/verify-apphost-invalid-bearer.sh` (new), `tests/e2e/scripts/verify-apphost-invalid-bearer.test.mjs` (new), `tests/e2e/package.json`, `.github/workflows/quality.yml` — extract rejection; diagnose/correct transport failure. Retain Aspire readiness, owned-process cleanup and four paths. Test transport-only fallback, all-unreachable, wrong HTTP status, discovery failure and cleanup; verify live.
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

Independent review used `/tmp/fc-13-6-independent-review-cwfgu783/changes.diff` (298,280 bytes; SHA-256 `3b5fe941e7f349d5590973c0af4e8393eb289b0e85eec12ee9141ede9e7a2499`) with all three context-free layers. Each finding is judged separately before grouping.

| ID / layer | Verdict | Evidence and route |
| --- | --- | --- |
| B1 / blind: pre-activation state binding | false | The alleged shared-store route is absent from this implementation: `create_configuration` generates every component with the owned Redis endpoint/password; `LocalReadinessTopology.Add` passes the non-null private state path; selected `AddHexalithEventStore` uses that exact `LocalPath` and selects shared fallback only for a null/blank path. The runner accepts no external store input, and actual owned-sidecar resource/config arguments were independently verified during the live proof. Component names alone would not prove arbitrary configuration, but the cited misconfiguration is not reachable in the current composition. Reject; no speculative process-inspection guard added. |
| B2 / blind: Docker inspection failure | medium | Cleanup treats every nonzero inspect exit as absence, including daemon/permission failures, then deletes private configuration and reports success. Confirm absence using a successful Docker query; retain private recovery state on unverifiable cleanup. Patch. |
| B3 / blind: SIGTERM cancellation | medium | No Python SIGTERM handler exists, so default termination bypasses `run`/`owned_apphost` finally blocks while detached resources can survive. Route cancellation through bounded owned cleanup and test the actual signal path. Patch. |
| B4 / blind: subprocess timeout | medium | `subprocess.run(timeout=...)` kills the direct shell; bearer/proof scripts own EXIT/TERM cleanup traps, which SIGKILL bypasses. The outer finally then refuses remaining-AppHost cleanup. Allow bounded graceful child termination before fallback and test timeout cleanup. Patch. |
| B5 / blind: initial unlocked build | medium | `run` checks processes and builds before `owned_apphost` takes the shared lock; a lock holder still preparing its proof has no AppHost yet and can have its binaries replaced. Move preflight/build under the existing lifecycle lock. Patch. |
| B6 / blind: incomplete health inventory | medium | Both health validators accept a nonempty subset, including final `self` alone; this can certify missing prerequisite checks. Require the selected platform's necessary component names in both validators while allowing additional healthy checks; exercise missing components. Patch. |
| B7 / blind: contradictory TRX summary | medium | All four validators inspect total/executed and per-test outcomes but ignore summary outcome and failure/error counters. A zero-exit report with passed results plus a failed summary is accepted. Validate summary/counter consistency and extend the runtime harness. Patch. |
| B8 / blind: malformed JSON shape | low | `validate_health(200, [], False)` raises AttributeError; similar wrong-shaped discovery/token payloads can raise TypeError/IndexError outside the bounded top-level handler. These fail closed but lose the promised sanitized diagnostic. Extend the bounded exception handling and exercise malformed response shapes. Direct patch. |
| B9 / blind: persistent Keycloak compatibility | medium | Ordinary local startup unconditionally calls the renderer, which aborts when the previously supported persistent switch is true. Preserve the pre-existing ordinary persistent-realm path without generating/replacing its credentials; keep disposable verification's fresh-realm requirement and the renderer's refusal. This requires no persistent-realm provisioning or mutation. Patch. |
| B10 / blind: cold image cache | low | `docker image inspect` is a deliberate prerequisite before recording image identity, but a missing image currently yields only a generic operation failure. Report the exact public image prerequisite/action without pulling images or changing dependencies. Direct diagnostic patch. |
| B11 / blind: diagnostics-retained claim | low | `invoke` writes output only when `log` is supplied, yet reports retained diagnostics for every failure. Make the statement conditional; do not retain secret-bearing command output by default. Direct wording patch. |
| E1 / edge: subprocess timeout | medium | Same independently verified shell-trap bypass as B4; each finding is retained here before grouping. Patch with B4. |
| E2 / edge: initial unlocked build | medium | Same independently verified build-before-lock ordering as B5; the review's held-lock probe observed one build before refusal. Patch with B5. |
| V1 / verification: marker caller coverage | medium | Pre-verified regression gap: removing `validate_marker` only from `activate` leaves all 19 Python tests passing; the only activation test exits before backup. Add activation-path success and mismatched persisted-marker cases with no successful evidence on rejection. Patch. |
| V2 / verification: C# phase decisions | medium | Pre-verified regression gap: the sole executing C# health-check test stops at a redirect; Python tests cover a different validator. Add valid maintenance/final phases and rejected component/readiness cases through the existing loopback HTTP fixture. Patch. |

B4/E1 share the child-termination root cause; B5/E2 share the preflight-lock root cause. Other surviving findings remain separate. All fixes are local corrections, add no public surface and address demonstrated states within the approved intent; no intent/spec loopback or deferred entry is needed. Re-engage the original implementation agent for the patches, then repeat affected checks and the complete disposable live rehearsal on the patched candidate.


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

### Amendment implementation and final local evidence — 2026-10-26

The [amendment result](13-6-accessibility-result.md#approved-local-workload-identity--2026-10-26) records the private generated realm, secret parameter/platform wiring, unchanged user clients and final scope-preservation correction. Focused host tests pass 22/22; the source AppHost build has zero warnings/errors; unchanged tooling passes 161/161. The final bearer helper observes four real 401 responses (five attempts, including one permitted transport fallback) and cleans its owned topology. The unchanged Epic 9 proof passes its one Chromium test, candidate integrity and artifact validation on `5b8a860744667fd09848d6a21bf592df2f592322` plus the two-file scope-preservation patch in explicit development mode. Earlier proof `a545702318164b45985c45c8dc00ce61e0c3b79a` plus the then-dirty amendment and the later external commit are separately attributed. The result owns exact commands, log/artifact paths and patch identity.

Fresh local token checks return 200 for the workload client with expected audience/operation, 401 for an invalid secret, and 200 for the existing user client requesting `openid profile email`. Generated private realm directories are removed after owned shutdown; unrelated AppHosts are preserved. This resolves the earlier missing-credential and user-scope failures without changing the checked-in realm or authentication enforcement.

The host task and fresh-realm matrix row remain **incomplete**: Aspire labels EventStore Running/Healthy, but its direct `/health` response is 503/Unhealthy, with Dapr sidecar/actor-placement startup conditions and an inactive projection-delivery writer-v2 marker. The result records bounded component evidence and the selected platform's existing readiness/cutover file/API map. No shared-store marker activation, invented maintenance attestation, dependency change or readiness relaxation is authorized or performed. The existing disposable test-lease pattern is only a follow-up reuse option. The separate live bearer task is now complete. Publication/pin adoption, hosted qualification and manual/independent acceptance gaps remain; the repair and parent story/sprint stay in-progress. Final Build review remains pending the failed full-health matrix row.

### Parent audit and proposed isolated-readiness amendment — 2026-10-26

The parent read the baseline diff, the two authorized Builds-file changes, the final source correction and the actual verification outputs. Documentation exclusions, four-project execution/report completeness, migration semantics and invalid-bearer outcomes have passing coverage. Host tests and real token exchanges prove identity/client preservation, but the fresh-realm row cannot pass while full application health is 503. The frozen acceptance, original baseline and in-progress statuses remain unchanged; no final code review or completion is claimed.

**Proposed scope, awaiting approval:** complete the remaining health requirement in an explicitly owned disposable topology, without activating or altering the current shared datastore.

- `src/Hexalith.FrontComposer.AppHost/Program.cs` and minimal host-owned support: add opt-in verification isolation using the selected platform's existing component-path and Dapr endpoint seams. Bind the participating services consistently to owned Redis/Dapr resources and ports; preserve ordinary development configuration and the approved identity behavior.
- `eng/verify-apphost-invalid-bearer.sh` or a minimal companion verification runner, with focused tests beside the existing Node/host tests: establish ownership, wait for actual component health, refuse unknown/shared resources, retain the four rejection checks and verify cleanup. Do not duplicate the existing Epic 9 proof or weaken its evidence contract.
- Reuse the selected EventStore cutover API and disposable-rehearsal design only after verifying the new store is isolated, taking and verifying its real backup, and establishing writer/retry-worker quiescence. Record the actual source identity and attestations; verify the persisted v2 marker. Do not copy test-fixture placeholder attestations or alter any existing marker.
- Require direct application health/readiness HTTP 200 with healthy components, all four actual 401/403 responses and the unchanged Epic 9 proof on the final candidate; then finish the parent matrix audit and independent Build review. Dependency edits, shared-store maintenance, publication and pin adoption remain excluded. If the selected platform cannot provide isolation through its existing APIs, report that specific owner boundary before expanding it.

This proposal extends the approved local-identity scope into disposable datastore readiness. The selected platform's [cutover runbook](../../references/Hexalith.EventStore/docs/operations/projection-delivery-v2-cutover.md) makes the maintenance and no-downgrade boundary explicit. No part of the proposed isolation/cutover work has been executed.

### Isolated-readiness approval and resumed implementation — 2026-10-26

The user approved the proposal above. Its awaiting-approval statement is now historical; the frozen block records the authorized extension and preserves all other boundaries. Resume at root `5b8a860744667fd09848d6a21bf592df2f592322` with the existing five-file working changes preserved. Keep the original baseline and parent story/sprint in-progress. Complete the remaining host/readiness tasks and matrix audit, then run the independent Build review. The prior passing unchanged checks need no repeated execution unless new edits or findings affect them.

### Isolated-readiness implementation and local completion — 2026-10-26

The [final isolated result](13-6-accessibility-result.md#approved-isolated-readiness--2026-10-26) records the opt-in gateway-plus-Counter topology, exact ownership/refusal/cleanup, real native backup and no-domain/control-state inventory, verified maintenance quiescence, authenticated writer-v2 activation and persisted marker readback. Ordinary gateway workers are restored before direct `/health` and `/ready` both return 200/Healthy with all nine components Healthy. Four invalid-bearer routes return 401 on their first endpoints. The unchanged Epic 9 proof passes one Chromium test, HEAD/status integrity and artifact validation on root `5b8a860744667fd09848d6a21bf592df2f592322` plus the recorded frozen implementation inputs in explicit development mode. This reduced topology does not certify the unchanged ordinary Tenants/Parties/Admin composition.

Final source builds have zero warnings/errors; host tests pass 25/25, and tooling passes 163 Node plus 19 Python tests. The result owns exact commands, raw paths, source/provider identities, backup reference, prior failed attempts and successful cleanup. Generated credentials remain private and temporary; actual fresh workload exchange succeeds. Selected EventStore remains clean at `aa46e2526ab33b2f4b220478c8acc097f7ad01b0`; no existing datastore, dependency, consumed workflow pin or unrelated AppHost was changed. The two remaining local execution tasks are now checked. Independent Build review is pending; repair and parent story/sprint remain in-progress, with hosted/publication and manual/independent acceptance boundaries unchanged.


### Parent matrix audit — 2026-10-26

The parent read the complete baseline diff, all untracked implementation files and the two authorized Builds-file changes, then reviewed the final delta and actual passing outputs. Unrelated external Story 15.4/15.5 work is preserved and is not attributed to this repair. Every local execution task is complete; review follows this audit.

| Frozen matrix row | Executed evidence and result |
| --- | --- |
| DocFX exclusions and missing matcher | DocsSiteValidationTests: 64 passed, including unrelated, exact/recursive exclusions and empty matcher-cache diagnostics; `/tmp/story13-6-docs-tests-fixed.log`. |
| Four projects and complete fresh TRX | Builds workflow harness: 287 assertions passed, including a stdin-consuming child across four projects and missing/stale/malformed/zero-test reports; `/tmp/story13-6-builds-checks.log`. |
| Current migration guidance and retained guards | Migration suite: 3 passed; `/tmp/story13-6-migration-tests.log`. Governance suite: 86 passed; `/tmp/story13-6-governance-tests.log`. |
| Fresh isolated local realm | Host suite: 25 passed, including secret/client preservation; `/tmp/story13-6-readiness-host-tests-final.log`. The final owned live run also completed workload token exchange and direct healthy EventStore checks. |
| Owned disposable readiness | Python runner suite: 19 passed, including unknown ownership, existing state and cleanup refusals; `/tmp/story13-6-readiness-tooling-tests-final.log`. Final live evidence verifies the real backup, quiescence, activation HTTP 200, exact persisted v2 marker, ordinary workers restored and direct health/readiness HTTP 200; `/tmp/story13-6-isolated-readiness-47bq6vic/`. |
| Four invalid-bearer outcomes | Node tooling: 163 passed, including 25 rejection-helper cases covering transport, redirects, wrong HTTP and sanitized diagnostics. Final live verifier records four first-attempt HTTP 401 responses; `/tmp/story13-6-isolated-readiness-47bq6vic/invalid-bearer.log`. |

The unchanged Epic 9 live proof also passed its Chromium test, candidate integrity and artifact validation. The parent independently verified all 12 frozen implementation-input hashes, the retained backup checksum, no remaining owned FrontComposer AppHost or verification containers, and removal of private configuration/realm directories. Story artifact validation and both repository diff checks pass. This is local development evidence on the recorded dirty candidate and reduced gateway-plus-Counter topology. Parent story/sprint remain in-progress; hosted, manual, independent-owner and publication/pin-adoption gates remain open.


### Review resolution and completion — 2026-10-26

All three independent review layers completed. Every finding is recorded in the Review Triage Log; the original implementation agent applied the twelve correction groups, the parent read the entire patch delta, and all fourteen surviving individual reports are resolved. One finding was refuted; no new work was deferred. The [reviewed completion result](13-6-accessibility-result.md#independent-review-and-patched-local-completion--2026-10-26) owns exact commands, revision/input hashes, passing counts and raw evidence.

Parent reruns pass: 31 host tests, 163 Node plus 31 Python tests, and 335 Builds assertions. The complete disposable live rehearsal also passes on the reviewed frozen inputs: fresh workload identity, real verified backup/quiescence, activation HTTP 200 and persisted v2 marker, ordinary healthy application health/readiness, four first-attempt 401 responses, unchanged Epic 9 Chromium proof and artifact/integrity checks, and independently verified owned cleanup. Final source builds have zero warnings/errors. The earlier unchanged Docs 64, governance 86 and migration 3 results remain valid for their unchanged inputs.

This repair spec is done. Its frozen acceptance explicitly keeps parent Story 13.6 and sprint in-progress while hosted/manual/independent acceptance is unmet, so the generic parent-to-review transition is not applied. The approved no-commit/no-push boundary also takes precedence over the workflow's generic local-commit step. No dependency or consumed workflow pin changed, no hosted run was requested, and no existing user/shared datastore was modified. All original parent acceptance gaps and manual qualification limits remain. Final artifact and both repository diff checks pass.
