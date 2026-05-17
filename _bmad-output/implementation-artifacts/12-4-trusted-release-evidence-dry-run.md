# Story 12.4: Trusted Release Evidence Dry Run

Status: review

> **Epic 12** - Release Certification and Evidence Alignment. This story proves the release-evidence path in a trusted context before v1 publication. It applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 10.6 added the release-evidence contract for package inventory, symbols, SBOM, checksums, signing, attestation fallback, sealed manifest, and release budget evidence. Story 11.7 then tightened CI/release ordering and credentialed side-effect risk. The remaining release-certification gap is not more release prose: it is a trusted-context dry run that proves the current `.github/workflows/release.yml`, `.releaserc.json`, and `eng/release_evidence.py` path cannot produce a false release-ready record.

Story 12.4 is the narrow release-owner proof pass. It must run or simulate the release path in a trusted main/release context without irreversible publish/tag/release mutation, capture bounded evidence, and record any unavailable signing, attestation, timestamp, NuGet, GitHub Release, or publication path as either a release blocker or an explicitly approved fallback.

---

## Story

As a maintainer,
I want release evidence proven in a trusted context,
so that signing, SBOM, checksums, symbols, attestations, package inventory, and publication ordering cannot produce a false release-ready record.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 12.4 evidence and know whether the v1 release workflow is ready to publish, blocked by a missing trusted path, or ready only with named and approved fallbacks.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary release files | `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, and `eng/release-package-inventory.json`. |
| Current workflow state | Release runs on `main` push or `workflow_dispatch`, checks out root-level submodules only, runs release tests, generates inventory/SBOM/checksums/signing verification, seals a manifest, publishes through semantic-release, and records release-budget evidence. |
| Trusted-context proof | Dry run must happen from trusted `main`/protected release context or a deliberately equivalent local proof that cannot exercise credentialed writes. PR/fork/untrusted contexts are read-only candidate evidence. |
| No irreversible side effects | Do not create tags, push changelog commits, publish NuGet packages, create GitHub Releases, upload attestations, or mutate external state during the dry run unless the step is explicitly approved as part of a real release. |
| Blocking checks before publish | Package inventory, blocking tests, SBOM, symbol package inventory, checksums, signing verification, attestation/approved fallback, redaction scan, sealed manifest verification, and rerun/partial-publish policy must pass before any publish/tag/release mutation. |
| External paths | Missing certificate, NuGet key, timestamp authority, GitHub attestation support, GitHub Release permission, or NuGet publish path becomes a release blocker unless an approved fallback has owner, evidence, release-note impact, expiry/revalidation trigger, and reopen event. |
| Evidence safety | Evidence must be bounded and sanitized: no certificate material, passwords, API keys, OIDC tokens, NuGet keys, tenant/user values, command payloads, local absolute paths, raw response bodies, or unbounded workflow logs. |
| Current package inventory | Expected packable packages are `Hexalith.FrontComposer.Cli`, `Contracts`, `Mcp`, `Schema`, `Shell`, and `Testing`; `SourceTools` is intentionally non-packable with an exception. |
| Scope guardrail | Do not redesign semantic-release, package topology, CI lane strategy, MCP, EventStore provider behavior, accessibility evidence, docs-site generation, or submodule strategy unless the dry run proves a named release blocker. |
| Release classification | Final output must be one of `ready`, `blocked`, or `fallback-approved`; candidate PR/fork/local evidence can never authorize publishing. |
| Approval contract | NuGet publish, tag/changelog push, GitHub Release creation, attestation upload, attestation fallback, partial-publish recovery, and failed-release rerun need named owner approval before side effects. |
| Negative fixture proof | The implementation must include deterministic fixtures proving false release-ready records fail closed without real publishing, GitHub Release, or attestation network calls. |
| Replay and drift proof | Release readiness must bind the exact workflow, semantic-release config, helper code, inventory, package artifacts, manifest seal, and approval evidence used by the run; stale or replayed evidence cannot authorize publishing. |
| Validation | Run release evidence helpers and static workflow checks; run broader tests only if source/workflow changes require them. |

Start here: T1 snapshot release workflow and package inventory -> T2 prove no side effects before checks -> T3 execute trusted dry-run or equivalent local candidate proof -> T4 verify manifest/signing/SBOM/checksum/evidence contracts -> T5 record blockers/fallbacks -> T6 update release-readiness notes and Dev Agent Record.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `.github/workflows/release.yml`, `.releaserc.json`, and `eng/release_evidence.py` define release behavior | Story 12.4 starts | The implementer records the current release graph, publish-capable steps, required secrets/vars, permissions, package inventory, and side-effect boundaries before edits. |
| AC2 | The release workflow can run on `main` or `workflow_dispatch` | A dry run is planned | The plan distinguishes trusted write-capable context from PR/fork/local candidate evidence and names which paths cannot be proven without a real release context. |
| AC3 | Release tests run before package publication | Workflow evidence is inspected | A release-ready result is invalid if tests are skipped, filtered to zero, missing TRX evidence, or allowed to pass after publish/sign/tag/release mutation. |
| AC4 | Package inventory is generated | The dry run executes | Inventory matches `eng/release-package-inventory.json`, unexpected packable projects fail closed, and per-project `Version` drift is rejected. |
| AC5 | Package output is built | Evidence is recorded | Every packable package has a lockstep version, expected `.nupkg`, required `.snupkg`, package id, project path, commit SHA, and symbol exception status. |
| AC6 | SBOM generation runs | Evidence is recorded | A concrete SBOM file and SHA-256 hash are present before manifest sealing; partial or missing SBOM evidence cannot be treated as complete. |
| AC7 | NuGet package signing runs | Evidence is recorded | Signed `.nupkg` artifacts are verified with timestamp evidence before publish; unsigned packages, missing timestamp, invalid certificate, or missing signing verification blocks release. |
| AC8 | Credential, certificate, timestamp, NuGet, or GitHub Release path is unavailable | The dry run completes | The missing path is recorded as a release blocker or approved fallback with owner, likelihood, impact, release risk, evidence, expiry/revalidation trigger, and reopen event. |
| AC9 | GitHub artifact attestations are supported | Release evidence is prepared | Attestations are generated and verified before release readiness is claimed, with the required least-privilege permissions documented. |
| AC10 | GitHub artifact attestations are unavailable or deliberately unsupported | Release evidence is prepared | The approved fallback explains why attestation is unavailable and keeps checksums, NuGet signatures, SBOM, commit SHA, tag, run id, and workflow ref as blocking provenance evidence. |
| AC11 | Checksums are generated | The manifest is prepared | Checksums cover signed `.nupkg`, required `.snupkg`, SBOM, signing verification, and fallback note artifacts without placeholder hashes. |
| AC12 | The sealed manifest is created | Verification runs | Manifest rows bind package id, version, commit SHA, artifact path, checksum, symbol artifact or exception, SBOM component, signing status, attestation status, publish status, tag, run id, workflow ref, and seal hash. |
| AC13 | A manifest field is missing, placeholder, stale, or points to unsigned artifacts | Verification runs | Release publication fails before any irreversible mutation and records bounded diagnostics. |
| AC14 | Release publication partially succeeds or a rerun is attempted | Release governance evaluates state | The workflow records a partial-publish incident or blocks rerun; it never silently rebuilds different artifacts under the same release evidence. |
| AC15 | Evidence paths are supplied by tool output or workflow metadata | Paths are normalized | Paths must stay under the approved evidence root, reject traversal and absolute paths, and publish logical artifact names rather than local filesystem paths. |
| AC16 | Release evidence includes tool output, package metadata, markdown, or diagnostics | Evidence is committed or attached | Output is escaped, bounded, and scanned for workflow-command injection, markdown control text, secrets, tokens, local paths, tenant/user values, raw response bodies, and unbounded logs. |
| AC17 | Workflow permissions are reviewed | The dry run is classified | Read-only jobs remain `contents: read`; write scopes are limited to release/tag, package, OIDC, and attestation steps that need them. |
| AC18 | The workflow checks out submodules | Release validation runs | Checkout remains root-level only (`submodules: true`) and no recursive nested submodule initialization/update is introduced. |
| AC19 | Release budget evidence is generated | The dry run or real release completes | Release minutes, tag-to-nuget latency, package count, run id, publish status, and package-count-collapse trigger state are recorded or explicitly marked unavailable. |
| AC20 | The dry run completes | Story evidence is updated | Story 12.4 records whether release evidence is ready, blocked, or ready with approved fallback, and names all residual release gates. |
| AC21 | Story 12.4 closes | Validation runs | Release evidence helper commands, manifest checks, status-artifact consistency, and `git diff --check` outcomes are recorded; source/workflow tests are run if touched. |
| AC22 | A release context is classified | Evidence is evaluated | The classifier records event name, ref, upstream/fork status, token permissions, dry-run flag, explicit approval flag, and rerun/partial-release state, then maps the run to `trusted-main-or-release`, `pr-same-repo`, `fork-pr`, `local-candidate`, `rerun-review`, or `approved-fallback`. |
| AC23 | A final `ready` result is requested | Release evidence is evaluated | `ready` is allowed only from machine-validated, normalized, sanitized, sealed evidence generated in a trusted write-capable context; workflow intent, branch name alone, or file presence cannot produce `ready`. |
| AC24 | A dry run or candidate run reaches side-effect-capable steps | Workflow and semantic-release guards execute | NuGet publish, tag/changelog push, GitHub Release creation, and attestation upload are unreachable unless a sealed `ready` classification and explicit owner approval are present; normal dry runs may only create local files, workflow artifacts, log annotations, and summaries. |
| AC25 | A release owner reviews the evidence | Story or workflow evidence is summarized | The decision output contains final classification, release action grouped blocking reasons, context class, approval status and approver identity when relevant, whether candidate evidence was used, and the next required owner action. |
| AC26 | An approval-dependent release action is possible | Evidence is prepared | The approval matrix covers NuGet publish, tag/changelog push, GitHub Release creation, attestation upload, attestation fallback, partial-publish recovery, and rerun after failed or partial release, including owner, mechanism, evidence, and blocking/fallback effect. |
| AC27 | Attestation or another external path uses fallback evidence | Release readiness is classified | Fallback is visibly classified as `fallback-approved`, never equivalent to full `ready`, and records reason, approver, timestamp, affected artifact/version, scope, expiry or revalidation trigger, reopen event, release-note impact, and sanitized evidence pointer. |
| AC28 | False release-ready conditions are tested | Validation fixtures run | Deterministic fixtures cover missing inventory package, skipped tests, zero tests, unsigned package, stale or missing timestamp, missing SBOM, checksum mismatch, unsealed manifest, PR/fork/local candidate context, recursive submodule command, path leakage, token-like leakage, and dry-run side-effect attempts. |
| AC29 | Evidence redaction is tested with hostile input | Evidence output is generated | Token-shaped values, Windows and Unix absolute paths, usernames, tenant/user identifiers, credentialed URLs, raw log fragments, environment dumps, signing material markers, and workflow-command strings are sanitized or block release classification deterministically. |
| AC30 | Release evidence is reused, rerun, or reviewed after release-file changes | Readiness is evaluated | Evidence binds hashes or equivalent immutable identifiers for `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, `eng/release-package-inventory.json`, package version inputs, and generated artifacts; drift makes the result `blocked` or requires a fresh run. |
| AC31 | Semantic-release, workflow shell steps, or helper output disagree on final state | The release-owner summary is produced | The final state is derived from one typed classification contract and cannot be inferred independently by prose, exit-code masking, job summary text, or file presence. |
| AC32 | A signed or checksummed package is repacked, moved, renamed, or regenerated after manifest sealing | Publish eligibility is checked | Publish/tag/release steps verify that the exact artifact digests and logical names in the sealed manifest are the ones being published; any post-seal artifact mutation blocks release. |
| AC33 | Two release runs, reruns, or manual attempts target the same version | Release governance evaluates concurrency | The workflow records run id, attempt, commit SHA, package version, tag, and previous publish state, then blocks ambiguous concurrent or stale rerun evidence before side effects. |
| AC34 | An approved fallback is carried forward to a later release attempt | Release readiness is classified | Fallback approval is invalidated by expiry, release-definition drift, package-set drift, evidence-contract changes, or changed affected artifacts unless explicitly reapproved. |
| AC35 | Helper commands produce warnings, partial JSON, empty evidence arrays, or sanitized diagnostics | CI and story evidence consume the results | Non-success command states are machine-readable, non-zero where blocking, and rendered in the summary as `blocked` or `unavailable`; warnings cannot be collapsed into `ready`. |

---

## Tasks / Subtasks

- [x] T1. Capture current release graph and trusted-context assumptions (AC1, AC2, AC17, AC18)
  - [x] Review `.github/workflows/release.yml` from checkout through release budget monitor.
  - [x] Review `.releaserc.json` prepare and publish commands for build, pack, SBOM, signing, checksum, manifest, NuGet push, and GitHub Release asset ordering.
  - [x] Review `eng/release_evidence.py` command contracts: `inventory`, `checksums`, `prepare-manifest`, `seal-manifest`, `verify-manifest`, `release-budget`, and `path-check`.
  - [x] Record required secrets/vars: `GITHUB_TOKEN`, `NUGET_API_KEY`, `NUGET_SIGNING_CERTIFICATE_PATH`, `NUGET_SIGNING_CERTIFICATE_PASSWORD`, `NUGET_SIGNING_TIMESTAMPER`, and `ATTESTATION_UNSUPPORTED`.
  - [x] Confirm root-level submodule checkout only; do not add recursive checkout or `git submodule update --init --recursive`.
  - [x] Record release-definition fingerprints for workflow, semantic-release config, release helper, inventory file, package version inputs, and any approval/fallback source consumed by the run.

- [x] T2. Prove ordering before irreversible side effects (AC3, AC13, AC14)
  - [x] Build a step-order table that names the first irreversible operation and every blocking check that must precede it.
  - [x] Verify blocking tests, package inventory, SBOM, signing verification, checksums, manifest sealing, manifest verification, attestation/fallback state, and redaction checks complete before NuGet push, tag/changelog push, GitHub Release creation, or attestation upload.
  - [x] Put side-effect guards adjacent to each side-effect-capable workflow or semantic-release operation, not only inside shared helper code.
  - [x] Make the side-effect phase depend on a sealed `ready` classification plus explicit owner approval.
  - [x] Record partial-publish/rerun behavior and manual reconciliation requirements for package, tag, changelog, GitHub Release, and attestation drift.
  - [x] Verify semantic-release cannot derive readiness from independent job-summary prose, environment variables, or helper output files unless the typed classification contract is sealed and valid.

- [x] T3. Execute dry-run evidence generation without publish mutation (AC4-AC7, AC11, AC12, AC15, AC22-AC24)
  - [x] Run package inventory:

```powershell
python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\story-12-4-package-inventory.json
```

  - [x] If local pack/SBOM/signing cannot run safely because credentials are absent, record the unavailable path as candidate evidence and do not fake success.
  - [x] Generate a candidate checksum/manifest fixture only from concrete local artifacts under an approved evidence root; never use placeholder hashes as passing evidence.
  - [x] Run `verify-manifest` against the candidate manifest or record why no valid manifest can be produced outside a trusted release run.
  - [x] Assert `trusted-main-or-release`, `pr-same-repo`, `fork-pr`, `local-candidate`, `rerun-review`, and `approved-fallback` fixture contexts map to the expected final classification.
  - [x] Prove candidate PR/fork/local evidence cannot authorize NuGet publish, tag/changelog push, GitHub Release creation, or attestation upload.
  - [x] Prove post-seal artifact mutation, stale release-definition fingerprints, and mismatched publish artifact digests block release classification.

- [x] T4. Resolve attestation, signing, and external dependency states (AC8-AC10, AC16, AC25-AC27)
  - [x] Determine whether GitHub artifact attestations are supported in the repository context; if supported, identify the required pre-publish attestation and verification step.
  - [x] If `ATTESTATION_UNSUPPORTED=true` remains the path, record the owner, approval evidence, expiry/revalidation trigger, and whether release notes must mention checksum/signature/SBOM provenance without attestations.
  - [x] Classify timestamp-authority, NuGet, GitHub Release, SBOM metadata, and signing-certificate unavailability as fail-closed blockers or approved fallbacks.
  - [x] Create the approval matrix for NuGet publish, tag/changelog push, GitHub Release creation, attestation upload, attestation fallback, partial-publish recovery, and rerun after failed or partial release.
  - [x] Generate a release-owner decision output with `ready`, `blocked`, or `fallback-approved`, grouped reasons, approval identity, context class, and next action.
  - [x] Run or document a redaction scan over generated release evidence.
  - [x] Record fallback invalidation triggers: expiry, workflow/helper/config drift, package-set drift, evidence-contract drift, affected artifact change, and release-owner revocation.

- [x] T5. Update release-readiness artifacts (AC19, AC20)
  - [x] Update this story's Dev Agent Record with command outcomes, changed files, final release classification, blockers, approved fallbacks, and residual risks.
  - [x] Update `deferred-work.md` only if Story 12.4 closes or routes current release-evidence rows.
  - [x] Add release-readiness notes only under bounded repository artifacts; do not attach raw workflow logs.
  - [x] Keep Story 12.5 accessibility/stakeholder evidence and Story 12.3 provider behavior outside this story.

- [x] T6. Validate completion (AC16, AC21)
  - [x] Run required negative fixtures for missing package inventory, skipped/zero tests, unsigned or untimestamped packages, missing SBOM, checksum mismatch, unsealed manifest, untrusted contexts, recursive submodule commands, redaction leakage, dry-run side-effect attempts, stale release-definition fingerprints, post-seal artifact mutation, concurrent same-version runs, stale fallback approval, and warning/partial-output exit-code masking.
  - [x] Run status-artifact consistency.
  - [x] Run `git diff --check`.
  - [x] Run focused release governance tests if workflow/helper source changed.
  - [x] Run broader main-lane tests only if production code, package metadata, or workflow ordering changed beyond evidence docs.

### Review Findings

_Code review 2026-05-16 (bmad-code-review). Layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor. Reviewed commit range `1d173a1^..d36201f` (story creation through implementation). 8 files, 1267 insertions / 38 deletions._

- [x] [Review][Decision][Resolved 2026-05-16] CR-12-4-D1 `publish_authorized=True` for `fallback-approved` [`eng/release_evidence.py:712`] — Decision: keep current behavior. `fallback-approved` authorizes publish with visibility-only differentiation (decision JSON + release-owner summary). The unified publish gate is intentional; release owners see the residual risk in the typed output. Dismissed.
- [x] [Review][Decision→Patch][Applied 2026-05-17] CR-12-4-D2 Same-version legitimate rerun is permanently blocked [`eng/release_evidence.py:228`] — Decision: fresh dispatch only. `rerun-review` is never publish-authorized; patch CR-12-4-P14 adds a typed `next_owner_action` to the decision output explaining "rerun-review contexts are never publish-authorized; create a fresh dispatch or new tag to retry".
- [x] [Review][Decision→Patch][Applied 2026-05-17] CR-12-4-D3 Negative fixtures inject blocking signals directly into `checks.*` [`tests/ci-governance/fixtures/release-readiness-cases.json`] — Decision: patch CR-12-4-P15 adds real-file post-seal mutation coverage, workflow concurrency signal assertions, and workflow/publish wiring assertions so the fixture surface is not only classifier-last-mile JSON.

- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P1 [CRITICAL] publishCmd `||` chain writes `partial-publish-incident.json` even when no NuGet push was attempted [`.releaserc.json:12`] — replaced the combined push chain with phase-specific package and symbol push guards that emit a typed `partial-publish-incident` only after a NuGet push phase fails.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P2 [CRITICAL] Repo-variable `vars.RELEASE_OWNER_APPROVED` (and `vars.RELEASE_APPROVER`) bypasses per-release approval on `push: main` [`.github/workflows/release.yml:28-32`] — release owner approval now comes only from `workflow_dispatch` input; `push: main` remains blocked unless a named manual approval path is used.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P3 [CRITICAL] Four `BLOCKING_CHECKS` are default-pass in argparse and never populated by publishCmd [`eng/release_evidence.py:961-977`, `.releaserc.json:12`] — CLI defaults now fail closed; publish classification reads `release-evidence` machine outputs and scans bounded evidence before authorizing.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P4 [CRITICAL] `prepare-manifest --attestation-status` defaults to `"approved-unsupported"`; not forwarded from `RELEASE_ATTESTATION_STATUS` [`eng/release_evidence.py:738`, `.releaserc.json:11`] — semantic-release now forwards `RELEASE_ATTESTATION_STATUS` into manifest preparation and publish classification.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P5 [CRITICAL] `prepare_manifest` never writes `timestamp_status` to package rows; `manifest_diagnostics` defaults missing field to `"verified"` [`eng/release_evidence.py:200, 501-512`] — already resolved by CR-12-4-P24; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P6 [CRITICAL] Three readiness signals hardcoded `"false"` in publishCmd: `--release-definition-drift`, `--post-seal-artifact-mutation`, `--concurrent-same-version` [`.releaserc.json:12`] — manifest verification now recomputes release-definition and artifact digests; workflow records a concurrency guard signal that must be passed to publish classification.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P7 [HIGH] verify-manifest before publish doesn't recompute sha256 of `nupkgs-signed/*.nupkg` vs sealed manifest [`.releaserc.json:12`, `eng/release_evidence.py:780-797`] — `verify-manifest --root` now rejects missing or mutated sealed artifacts.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P8 [HIGH] `benchmark_summary_hash` default `"candidate-benchmark-summary"` placeholder passes `manifest_diagnostics` [`eng/release_evidence.py:206-208, 737`] — placeholder default removed; semantic-release writes pre-publish release-budget evidence and passes its sha256.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P9 [HIGH] No real attestation generation step, but env defaults `RELEASE_ATTESTATION_STATUS` to `"attested"` when `ATTESTATION_UNSUPPORTED!=true` [`.github/workflows/release.yml:30`] — workflow defaults to governed `approved-unsupported` and still blocks unless the fallback variable is explicitly approved.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P10 [HIGH] publishCmd hardcodes `--from-fork "false"` and omits `--dry-run` [`.releaserc.json:12`] — publish classification now consumes `RELEASE_FROM_FORK` and `RELEASE_DRY_RUN`.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P11 [HIGH] `test_status` defaults to `"passed"` and `test_count=1` if `--test-results` arg is missing [`eng/release_evidence.py:858, 968-977`] — classifier defaults now fail closed and require TRX-backed test evidence.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P12 [HIGH] `fallback_complete` parses `expires_at` with `dt.date.fromisoformat`; rejects any ISO datetime string [`eng/release_evidence.py:644-647`] — already resolved by CR-12-4-P18; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P13 [HIGH] `int(context.get("run_attempt", 1) or 1)` and `int(os.environ.get("GITHUB_RUN_ATTEMPT", "1"))` raise unhandled `ValueError` on non-numeric input [`eng/release_evidence.py:226, 947`] — already resolved by CR-12-4-P19/P22; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M1 [MEDIUM] `manifest_diagnostics` lockstep version drift partial enforcement [`eng/release_evidence.py:568-571`] — manifest diagnostics now independently flags missing package version evidence before strict version comparison.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M2 [MEDIUM] `classify_context` `partial_publish_state` capitalization bug [`eng/release_evidence.py:606-607`] — already resolved by null-safe lowercase normalization; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M3 [MEDIUM] `DANGEROUS_EVIDENCE_PATTERNS` regex coverage gaps [`eng/release_evidence.py:510-517`] — already resolved by expanded token/path patterns; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M4 [MEDIUM] `classify-release --require-publishable` exits 1 for both "blocked release" and "tool crash" [`eng/release_evidence.py` argparse + main] — unexpected helper crashes now exit 2 with a crash diagnostic while blocked releases remain exit 1.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M5 [MEDIUM] Approval truthiness only accepts literal `"true"` case-insensitive [`eng/release_evidence.py:825`] — approval parsing now accepts the documented approval-domain literals and rejects unrecognized values with typed diagnostics.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M6 [MEDIUM] `classify_context` priority order shadows `fork-pr` behind `rerun-review` [`eng/release_evidence.py:228-231`] — already resolved by prioritizing fork context before rerun review; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-M7 [MEDIUM] `release_definition_fingerprints` computed but never compared to a sealed baseline [`eng/release_evidence.py:622-627, 733`] — already resolved by embedding fingerprints in the sealed manifest and verifying current fingerprints against baseline.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-L1 [LOW] Duplicate diagnostics for `sbom_hash=""` [`eng/release_evidence.py:203-207`] — existing diagnostics now suppress the concrete-hash diagnostic when the hash is missing; revalidated in this pass.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-L2 [LOW] `release-readiness.json` and `test-results.json` are GitHub Release assets but not in the sealed manifest's checksum list [`.releaserc.json:24-25`] — `test-results.json` is now included in checksum inputs and the typed decision output lists advisory release artifacts.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P14 [HIGH] Add typed `next_owner_action` message for rerun-review classifications [`eng/release_evidence.py` `classify_release_payload` / `next_owner_action`] — rerun classifications now instruct owners to create a fresh dispatch or new tag; fixture output asserts the message.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P15 [MEDIUM] Add end-to-end fixtures that exercise real workflow→helper signal production [`tests/ci-governance/fixtures/` + new test harness] — added governance test coverage for real post-seal artifact mutation, workflow concurrency signal wiring, publish guard wiring, and rerun owner-action output.

- [x] [Review][Defer] CR-12-4-Def1 [MEDIUM] `deep_merge` cannot remove keys or override-to-empty-dict [`eng/release_evidence.py:550-556, 157-177`] — `override=={}` falls back to base instead of producing an empty dict, and there is no sentinel for key deletion. Fixture-design limitation, not a release-evidence correctness bug. Deferred: tangential to release-readiness; pick up if a future fixture needs missing-field coverage.

- [x] [Review][Dismiss] CR-12-4-D1 fallback-approved publish authorization kept unified — see resolution above.
- [x] [Review][Dismiss] TRX status string coupling fragile (Blind Hunter LOW) — current behavior fails closed if a new TRX status string appears, because `test_payload.get("status",...) == "valid"` only promotes to `"passed"` and `BLOCKING_CHECKS["test_status"]=="passed"`. No correctness defect.
- [x] [Review][Dismiss] `pr-same-repo`/`fork-pr` fixtures don't match production workflow triggers (Acceptance Auditor LOW) — release.yml only listens to `push: main` + `workflow_dispatch`; the fixtures are defense-in-depth for future trigger expansion. No defect.

_Follow-up code review 2026-05-16 (bmad-code-review). Layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor. Reviewed scope: uncommitted in-flight patches (`git diff HEAD`, 3 files, +129/-27) applied after the prior review block was written. Verifies whether the uncommitted work resolves prior CR items P1-P15/M1-M7/L1-L2 and whether it introduces new defects. CR resolution snapshot: M1/M2/M3/M6/L1 fully resolved; M5/M7/P5/P8/P12/P13 partially resolved (see new items below); P1/P2/P3/P4/P6/P7/P9/P10/P11/L2/P14/P15 not touched._

- [x] [Review][Decision][Resolved 2026-05-16] CR-12-4-D4 [HIGH] P5 half-fix: `timestamp_status` is now fail-closed in `manifest_diagnostics` but `prepare_manifest` still does not write the field. Decision: **land both halves now** — also patch `prepare_manifest` to emit `timestamp_status` from signing/timestamper output. See patch CR-12-4-P24.
- [x] [Review][Decision][Resolved 2026-05-16] CR-12-4-D5 [MEDIUM] M7 partial-fix: `fingerprint_diff` helper added but never invoked; no sealed baseline storage exists. Decision: **wire it up now** — embed baseline fingerprints in the sealed manifest itself; `verify-manifest` recomputes and calls `fingerprint_diff`. See patch CR-12-4-P25.

- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P16 [CRITICAL] `fallback_complete` tuple-return regression silently authorizes any dict-shaped fallback (incomplete, expired, or malformed) [`eng/release_evidence.py:384`] — the diff at L222-247 changed `fallback_complete` signature to `tuple[bool, str | None]`, but the sole caller at L384 still does `if isinstance(fallback, dict) and fallback_complete(fallback):`. A non-empty tuple is always truthy, so `(False, "fallback expired on …")` evaluates as True and the run takes the `fallback-approved` branch instead of `blocking`. Violates D19/AC27/AC34. Fix: unpack — `complete, diag = fallback_complete(fallback); if isinstance(fallback, dict) and complete: ...; else: blocking.append(diag or "unsupported-attestation fallback is missing, stale, or incomplete")`.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P17 [HIGH] `manifest_diagnostics` crashes on JSON `"packages": null` [`eng/release_evidence.py:228`] — new line `versions = {row.get("version") for row in rows if isinstance(row, dict)}` runs unconditionally after the existence check queues `"package rows are required"`; if `rows is None`, the set comprehension raises `TypeError` and the helper dies with an unhandled traceback instead of returning the typed diagnostic. Fix: guard `versions = {...} if isinstance(rows, list) else set()`, or move the comprehension inside the existing `if isinstance(rows, list)` block.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P18 [HIGH] `parse_expiry` strips timezone before extracting date → off-by-one across non-UTC offsets [`eng/release_evidence.py:118-122, 317`] — `dt.datetime.fromisoformat(normalized).date()` returns wall-clock date; an expiry like `2026-05-16T00:00:00+14:00` reads as `2026-05-16` even though the UTC instant is already `2026-05-15T10:00:00Z` (already past). `fallback_complete` then compares against `dt.datetime.now(dt.timezone.utc).date()`, so far-eastern offsets read valid for up to ~24h after UTC expiry and far-western offsets expire up to a day early. Fix: `.astimezone(dt.timezone.utc).date()` before comparison; reject or normalize naive datetimes explicitly.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P19 [HIGH] `safe_run_attempt` diagnostic is silently discarded in `classify_context` [`eng/release_evidence.py:207`] — `run_attempt, _ = safe_run_attempt(context.get("run_attempt", 1))` throws away the typed diagnostic the helper was added to surface. A non-numeric `run_attempt` (e.g., `"abc"`, present-but-empty `""`) silently coerces to 1 and the run is classified `trusted-main-or-release` instead of `rerun-review`, with no breadcrumb in `grouped_reasons`/`helper_state`. Undoes the typed-diagnostic intent of P13 and violates AC35 (non-success helper states must be machine-readable). Fix: return the diagnostic from `classify_context`, or append it to a context-diagnostic list the caller collates into `helper_state`/`grouped_reasons`.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P20 [HIGH] `parse_strict_bool` accepts approval-domain literals (`approved`/`denied`) for non-approval security flags, and silently coerces empty string to False [`eng/release_evidence.py:78-79, 98-107, 204-206`] — `TRUTHY_LITERALS={true,1,yes,approved}` and `FALSY_LITERALS={false,0,no,denied,""}` are reused for `ref_protected`, `from_fork`, `dry_run`. An operator setting `from_fork=approved` or `dry_run=denied` is silently coerced. An env var present-but-empty is silently False with no diagnostic — fork-PR gate bypassed silently if env drifts. Fix: shrink the literal sets to `{"true"}/{"false"}` for non-approval callers (or pass a domain-specific allowlist), AND emit a typed diagnostic when the value is unrecognized for security-sensitive flags.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P21 [HIGH] `parse_strict_bool` not used by argparse boolean flags in `classify-release` [`eng/release_evidence.py` argparse handlers ~L664-707] — evidence-file path (`classify_context`) uses `parse_strict_bool` for `from_fork`/`ref_protected`/`dry_run`; CLI-flag path still uses literal `str(args.X).lower() == "true"` for `--from-fork`, `--owner-approved`, etc. Identical evidence read via two paths produces different boolean interpretations. M5's stated fix ("accept the documented truthy set OR reject with a typed diagnostic") is incomplete. Fix: route CLI flag parsing through `parse_strict_bool` consistently (or use `argparse` `type=parse_strict_bool` adapter).
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P22 [MEDIUM] argparse `type=int, default=int(os.environ.get("GITHUB_RUN_ATTEMPT", "1"))` crashes at parser-construction time if env is empty or non-numeric [`eng/release_evidence.py` argparse ~L839] — P13's `safe_run_attempt` helper only covered the `classify_context` call site; the argparse default is evaluated when the parser is constructed and bypasses the helper entirely. Fix: compute the default via `safe_run_attempt(os.environ.get("GITHUB_RUN_ATTEMPT", "1"))[0]`, or wrap parser construction in try/except that falls back to 1 and surfaces a typed diagnostic.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P23 [MEDIUM] Story `Review Findings` block lists CR-12-4-D2/D3 as `[Resolved 2026-05-16]` resolved-by-P14/P15, but P14 and P15 are still unchecked and untouched by this diff [`_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md:14-15, 39-40`] — risk of premature closure if the story moves to merge while P14/P15 are still pending implementation. Fix: either complete P14/P15 in this round (preferred), or reword the D2/D3 resolution rows to "Decision accepted; resolution pending P14/P15 implementation" so the audit trail does not claim done-state for unimplemented patches.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P24 [HIGH] Land the second half of P5: extend `prepare_manifest` to write `timestamp_status` into package rows from the signing/timestamper output (resolves CR-12-4-D4) [`eng/release_evidence.py` `prepare_manifest` body ~L560-571] — pair the strict `manifest_diagnostics` check (already in this diff) with a real source. Implementation: accept a `--signing-verification` JSON argument (produced by `dotnet nuget verify` output post-signing) keyed by package id; for each row, look up the package's verification record and write `timestamp_status="verified"` only when the signing report shows a valid RFC 3161 countersignature. Default to `"missing"` when the record is absent.
- [x] [Review][Patch][Applied 2026-05-17] CR-12-4-P25 [MEDIUM] Land the wiring of `fingerprint_diff` (resolves CR-12-4-D5) [`eng/release_evidence.py` `prepare_manifest`, `verify-manifest`, classifier flow] — embed `release_definition_fingerprints` output into the sealed manifest at prepare time (new field `release_definition_fingerprints`); `verify-manifest` recomputes the fingerprints from disk and calls `fingerprint_diff(current, manifest["release_definition_fingerprints"])`; surface any drift as a typed `release_definition_drift=true` signal feeding the existing `BLOCKING_CHECKS["release_definition_drift"]` gate. Also fix `fingerprint_diff`'s symmetric "missing" sentinel: when either side records `"missing"`, that file is treated as drift regardless of equality (so deleting a required file with no baseline still fails closed).

- [x] [Review][Defer] CR-12-4-Def2 [LOW] `safe_run_attempt` accepts float and truncates via `int(1.9) == 1` [`eng/release_evidence.py:184-191`] — asymmetric with `"1.9"` string rejection. Very low likelihood in practice (JSON producers send ints; env vars are strings caught by the try/except). Deferred: real but tangential; pick up when a JSON producer is found that sends float `run_attempt` values.

- [x] [Review][Dismiss] CR-12-4-D2 / D3 follow-up note — restated in CR-12-4-P23 above; no separate defect.
- [x] [Review][Dismiss] Windows-path regex `[\\/]` only matches forward slash (Edge Case Hunter CRITICAL) — verified incorrect: `re.compile(r"[\\/]")` matches both `C:\Users\foo` and `C:/Users/foo` (regex char-class `\\` is literal backslash). No defect.
- [x] [Review][Dismiss] Token regex misses speculative `ghb_`/`ghpat_` prefixes (Blind Hunter HIGH) — `gho_/ghp_/ghs_/ghu_/ghr_` and `github_pat_` are the official prefix set; `ghb_`/`ghpat_` are not real. M3 correctly resolved.
- [x] [Review][Dismiss] `fork-pr`/`rerun-review` priority swap drops rerun classification (Blind Hunter HIGH) — intentional M6 choice; both classes are equally non-publish-authorized via `context_class != "trusted-main-or-release"` at L392. Safety preserved; audit trail prefers the more relevant fork root cause.
- [x] [Review][Dismiss] `fingerprint_diff` inconsistent diagnostic vocabulary for symmetric drift (Blind Hunter MEDIUM) — style nit; secondary to the open D5 decision on whether the helper is wired up at all.
- [x] [Review][Dismiss] `manifest_diagnostics` lockstep `{None}` set has `len==1`, no drift flagged (Blind Hunter MEDIUM) — all-missing-versions case is caught by the per-row `REQUIRED_ROW_FIELDS` loop emitting `"<pkg>: missing version"`. Lockstep gate correctly limited to inter-row drift.
- [x] [Review][Dismiss] `manifest_diagnostics` empty-string sbom/benchmark hash silently passes second guard (Blind Hunter MEDIUM) — upstream `if not manifest.get(field)` loop catches `""` first. Defense-in-depth narrowed but single line of defense remains; acceptable.
- [x] [Review][Dismiss] `parse_expiry` empty-string diagnostic is unreachable from `fallback_complete` (Blind Hunter LOW) — caller's `missing` check catches `""` before `parse_expiry` runs. Dead branch is harmless.
- [x] [Review][Dismiss] Operator-controlled `expires_at` echoed into diagnostics via `sanitize()` (Edge Case Hunter LOW) — `sanitize()` HTML-escapes and length-caps to 600 chars; reducing to 64 chars is YAGNI for v1.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | A successful story-status or release-workflow run is not release evidence unless required artifacts are concrete, bounded, and verified before publish. | Story 10.6 and Story 11.7 both identified false release-ready records as the main risk. |
| D2 | Trusted write-capable context and local/PR candidate evidence are different classes of proof. | Credentials, OIDC attestations, protected refs, and publication permissions cannot be honestly proven from untrusted branches. |
| D3 | Publish/tag/release mutation must be the last phase after blocking checks. | Package inventory, SBOM, signatures, checksums, manifest, tests, and redaction must gate irreversible external side effects. |
| D4 | Attestation unsupported is an approved fallback, not a silent pass. | GitHub attestation support depends on repository plan/visibility/permissions and must be documented when unavailable. |
| D5 | The sealed manifest is the release truth source. | It binds package ids, versions, checksums, SBOM, signatures, attestation state, commit, tag, run id, and workflow ref into one auditable record. |
| D6 | Evidence is hostile input. | Tool output, package metadata, markdown, and diagnostics can leak secrets or inject misleading workflow summaries unless escaped and bounded. |
| D7 | Missing signing, timestamp, NuGet, GitHub Release, or attestation paths are release blockers unless explicitly accepted. | A dry run that cannot prove the external path must not imply release readiness. |
| D8 | No recursive nested submodule commands are needed. | Release checkout must honor the root-level submodule policy already encoded in project rules. |
| D9 | `ready` requires trusted context plus sealed machine evidence; branch name or workflow intent is insufficient. | The party-mode review identified false release-ready classification as the highest-risk architecture failure. |
| D10 | Candidate PR, fork, and local evidence may inform blockers but cannot authorize publishing. | Untrusted or non-credentialed contexts cannot prove protected credentials, OIDC, NuGet, GitHub Release, or attestation behavior. |
| D11 | Irreversible operations need adjacent guards and owner approval. | Guarding only in shared helper code leaves semantic-release or workflow steps able to drift into side effects. |
| D12 | Approved fallback remains `fallback-approved`, not full `ready`. | Release owners must see the residual risk instead of receiving a visually equivalent success state. |
| D13 | Negative fixtures are mandatory but bounded. | The story needs deterministic fail-closed proof without real package publishing, GitHub Release, or network-dependent attestation calls. |
| D14 | Release-owner output is part of the contract. | The release owner needs a concise decision artifact, not only raw workflow evidence. |
| D15 | Release evidence must bind release-definition inputs as well as artifacts. | A sealed manifest is not enough if the workflow, semantic-release config, helper logic, inventory, package versions, or approval source drift after evidence generation. |
| D16 | The sealed manifest owns publish inputs after the final artifact build. | Post-seal repacking, renaming, moving, or regenerating package artifacts can otherwise publish bytes that were never signed, checksummed, or attested. |
| D17 | Final release state is a single typed classification contract. | Independent prose summaries, exit-code-only checks, and marker files can disagree and create a false release-ready record. |
| D18 | Same-version reruns and concurrent runs are fail-closed until reconciled. | Release jobs can otherwise reuse stale evidence or race a partial publish under the same package version and tag. |
| D19 | Fallback approval is scoped and invalidated by material drift. | An attestation or signing fallback accepted for one artifact set cannot silently authorize a later changed release. |
| D20 | Warning, unavailable, and partial-output states are blocking unless explicitly classified. | Sanitized or bounded diagnostics must not accidentally downgrade a failed proof into a successful release-ready summary. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `.github/workflows/release.yml` | Update possible | Only if ordering, permissions, redaction, attestation, submodule, or dry-run behavior is proven unsafe. |
| `.releaserc.json` | Update possible | Semantic-release prepare/publish command ordering, assets, or manifest verification. |
| `eng/release_evidence.py` | Update possible | Release evidence validation, manifest, checksum, path, budget, or redaction helper behavior. |
| `eng/release-package-inventory.json` | Update possible | Only if package inventory drift is deliberate and release-owner approved. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update possible | Final release-evidence row state or accepted fallback metadata if Story 12.4 closes release rows. |
| `_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md` | Update | Dev Agent Record, validation evidence, blockers/fallbacks, changed files, and completion notes. |
| `artifacts/release/**` | Create/update possible | Bounded dry-run evidence. Do not commit secrets, raw logs, or unbounded outputs. |

No unrelated MCP, EventStore provider, accessibility, stakeholder acceptance, docs-site generation, package topology redesign, or nested submodule changes should be made by default.

---

## Implementation Enforcement Map

| Surface | Enforcement responsibility |
| --- | --- |
| `.github/workflows/release.yml` | Classify trusted/candidate/fallback context; keep least-privilege permissions; keep root-level submodule checkout only; gate dry-run behavior; ensure side-effect steps depend on sealed `ready` plus owner approval. |
| `.releaserc.json` | Ensure semantic-release prepare/publish behavior cannot push tags, changelog commits, GitHub Releases, or package assets from candidate evidence mode, stale fingerprints, partial output, or post-seal artifact drift. |
| `eng/release_evidence.py` | Validate inventory, test results, SBOM, signatures, timestamps, checksums, attestation/fallback state, manifest sealing, path normalization, redaction, release-definition fingerprints, post-seal artifact identity, concurrency/rerun state, and final classification. |
| `eng/release-package-inventory.json` | Remain the authoritative package set and symbol/SBOM/signing expectation source; unexpected packable projects fail closed. |
| Story evidence / release-owner notes | Present `ready`, `blocked`, or `fallback-approved`, grouped blocking reasons, approvals, context class, sanitized evidence pointers, and next owner action. |

---

## Project Structure Notes

- Release workflow lives in `.github/workflows/release.yml`; release orchestration is split between workflow steps and `.releaserc.json`.
- Release evidence helpers live in `eng/release_evidence.py`; expected package inventory lives in `eng/release-package-inventory.json`.
- Current release evidence output root is `release-evidence` in workflow context; story dry-run evidence may use `artifacts/release` if committed or referenced.
- Use repository-relative paths in evidence and story records. Do not paste local absolute paths, raw workflow logs, certificate paths, secrets, tokens, tenant/user values, command payloads, or raw HTTP/API responses.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

---

## Testing Strategy

- Start with static release graph review and `eng/release_evidence.py inventory`.
- Exercise `checksums`, `prepare-manifest`, `seal-manifest`, `verify-manifest`, and `path-check` against controlled fixture artifacts if real package/sign/SBOM artifacts are unavailable.
- Add focused tests or helper fixtures only if a helper defect is found. Do not broaden into a release-system rewrite.
- Keep negative fixtures small and deterministic; do not invoke real NuGet publish, GitHub Release APIs, external signing services, or network-dependent attestation services in unit tests.
- Required context fixtures: `trusted-main-or-release`, `pr-same-repo`, `fork-pr`, `local-candidate`, `rerun-review`, and `approved-fallback`, each with explicit expected classification.
- Required failure fixtures: missing inventory package, skipped tests, zero tests, unsigned package, stale or missing timestamp, missing SBOM, checksum mismatch, unsealed manifest, recursive submodule command, path leakage, token-like leakage, hostile markdown/workflow-command output, dry-run side-effect attempt, stale workflow/helper/config fingerprint, post-seal package mutation, concurrent same-version run, stale fallback approval, and partial helper output with misleading success text.
- Suggested validation commands, adjusted only if implementation changes the CLI contract:

```powershell
python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\story-12-4-package-inventory.json
python eng\release_evidence.py path-check --root . --path artifacts\release\story-12-4-package-inventory.json
python eng\release_evidence.py verify-manifest --manifest artifacts\release\story-12-4-manifest.json
dotnet test --configuration Release
```

- Run workflow lint/static inspection when workflow edits occur.
- Run `git diff --check` and status-artifact consistency before review.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 10.6 | Story 12.4 | Release evidence contract: package inventory, SBOM, signatures, checksums, symbols, attestation/fallback, sealed manifest, release budget, and redaction. |
| Story 11.7 | Story 12.4 | CI/release ordering and irreversible side-effect governance must be proven, not inferred. |
| `.github/workflows/release.yml` | Release owner | Workflow permissions, test ordering, attestation fallback, semantic-release, and release-budget evidence define the trusted release path. |
| `.releaserc.json` | Release owner | Prepare/publish commands define exact artifact creation, signing, verification, NuGet push, and GitHub Release asset behavior. |
| `eng/release_evidence.py` | Release workflow and story dry run | Helper commands are the executable oracle for inventory, paths, checksums, manifest verification, and release budget evidence. |
| Story 12.4 | Story 12.5 | Trusted release evidence does not satisfy manual accessibility or stakeholder acceptance evidence. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Manual accessibility, localization/RTL/AT, real-device, and stakeholder acceptance evidence. | Story 12.5 |
| Provider-backed pending-command status readiness. | Story 12.3 |
| Package-count collapse evaluation if three consecutive release budget breaches occur. | Release owner after Story 12.4 evidence |
| Real attestation implementation if approved fallback expires or GitHub support becomes available. | Release owner / CI governance follow-up |
| Release dashboard that rolls Pact, mutation, accessibility, quarantine, and release evidence into one view. | Future release dashboard/process story |
| Exact attestation fallback approving authority, evidence-retention location, release-budget blocking threshold, and partial-publish recovery policy. | Release owner / architecture decision before publish authorization |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.4`] - story statement, acceptance criteria, and Epic 12 scope.
- [Source: `_bmad-output/implementation-artifacts/10-6-llm-benchmark-signed-releases-and-sbom.md`] - release evidence contract, sealed manifest, SBOM, signing, attestation fallback, budget, and redaction hardening.
- [Source: `_bmad-output/implementation-artifacts/11-7-eventstore-reliability-and-ci-governance-follow-ups.md`] - release ordering, trusted context, and irreversible side-effect governance.
- [Source: `.github/workflows/release.yml`] - current release workflow, permissions, tests, package inventory, attestation fallback, semantic-release, and budget evidence.
- [Source: `.releaserc.json`] - semantic-release prepare/publish commands and GitHub Release assets.
- [Source: `eng/release_evidence.py`] - executable release evidence validation helpers.
- [Source: `eng/release-package-inventory.json`] - expected package inventory and symbol requirements.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR74-FR75`] - semantic release, SBOM, signed package, and symbol package requirements.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Release-Automation`] - release automation, SBOM, signing, and package compatibility requirements.
- [Source: `_bmad-output/project-context.md`] - project rules for release evidence, redaction, central package management, and submodules.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.
- [Official: GitHub Docs, artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations) - current attestation/provenance model.
- [Official: GitHub Docs, using artifact attestations](https://docs.github.com/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds) - workflow permission and verification context.
- [Official: Microsoft Learn, NuGet signed packages](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference) - NuGet signing and timestamp expectations.
- [Official: CycloneDX .NET](https://github.com/CycloneDX/cyclonedx-dotnet) - current .NET SBOM tool used by the workflow.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-4-trusted-release-evidence-dry-run` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 3, and selected the first backlog story `12-4-trusted-release-evidence-dry-run`.
- 2026-05-13: Starting release evidence audit identified current release workflow files `.github/workflows/release.yml`, `.releaserc.json`, helper `eng/release_evidence.py`, inventory `eng/release-package-inventory.json`, and expected packable package set `Cli`, `Contracts`, `Mcp`, `Schema`, `Shell`, and `Testing`.
- 2026-05-13T21:27:32+02:00: `/bmad-party-mode 12-4-trusted-release-evidence-dry-run; review;` with Winston, Amelia, John, and Murat. All four reviewers recommended `needs-story-update` before development, focused on release context classification, owner approvals, adjacent side-effect guards, fallback visibility, deterministic negative fixtures, redaction, root-only submodule proof, and release-owner decision output.
- 2026-05-14T11:36:04+02:00: `/bmad-advanced-elicitation 12-4-trusted-release-evidence-dry-run` applied two-batch pre-dev hardening for release-definition drift, post-seal artifact identity, typed classification consistency, same-version rerun/concurrency, fallback invalidation, and partial-output masking risks.
- 2026-05-15: Reviewed `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, `eng/release-package-inventory.json`, and `Directory.Packages.props`; release graph now records typed readiness classification before any NuGet publish, GitHub Release asset upload, tag/changelog push, or fallback-authorized side effect.
- 2026-05-15: Added `test-results`, `classify-release`, and `classify-fixtures` helper commands. The decision contract is `frontcomposer.release-readiness.v1` and emits `ready`, `blocked`, or `fallback-approved`, context class, grouped reasons, owner approval identity, sanitized raw evidence, release-definition fingerprints, and `publish_authorized`.
- 2026-05-15: Added deterministic release-readiness fixtures for trusted, fallback, PR, fork, local, rerun, stale/drift, unsealed, unsigned, untimestamped, missing SBOM, checksum mismatch, recursive submodule command, hostile output, token/path leakage, dry-run side-effect attempt, concurrent same-version run, stale fallback approval, and partial helper output cases.
- 2026-05-15: Validation commands run: `python -m py_compile eng\release_evidence.py`; `python eng\release_evidence.py classify-fixtures --fixtures tests\ci-governance\fixtures\release-readiness-cases.json --output artifacts\release\story-12-4-readiness-fixtures.json`; `python eng\release_evidence.py verify-manifest --manifest tests\ci-governance\fixtures\release-manifest-valid.json --output artifacts\release\story-12-4-valid-manifest-check.json`; `python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\story-12-4-package-inventory.json`; `python eng\release_evidence.py path-check --root . --name artifacts/release/story-12-4-package-inventory.json --output artifacts\release\story-12-4-path-check.json`; release workflow YAML parse and `.releaserc.json` JSON parse.
- 2026-05-15: `python jobs\preflight-code-review.py --repo . --out artifacts\release\story-12-4-status-consistency.json` reported status-artifact consistency OK across 73 story keys and failed only its working-tree-cleanliness check because Story 12.4 files were intentionally dirty.
- 2026-05-15: `git diff --check` passed. `python -m json.tool .releaserc.json > $null` passed. Focused governance tests passed: `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter FullyQualifiedName~CiGovernanceTests`.
- 2026-05-15: Broad release-filtered solution lane passed after rerunning serially to avoid Windows testhost file locks: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" --no-restore -m:1`.
- 2026-05-17: Resolved prior-round review findings CR-12-4-P1..P15, M1..M7, L1..L2. Release publish now requires workflow-dispatch owner approval, machine-derived evidence-root checks, explicit fork/dry-run/concurrency signals, manifest artifact digest verification, pre-publish release-budget hash evidence, and phase-specific partial-publish incident output.
- 2026-05-17: Validation commands run: `python -m py_compile eng\release_evidence.py`; `python -m json.tool .releaserc.json > $null`; `python eng\release_evidence.py classify-fixtures --fixtures tests\ci-governance\fixtures\release-readiness-cases.json --output artifacts\release\story-12-4-readiness-fixtures.json`; `python eng\release_evidence.py verify-manifest --manifest tests\ci-governance\fixtures\release-manifest-valid.json --output artifacts\release\story-12-4-valid-manifest-check.json`; `python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\story-12-4-package-inventory.json`; `python eng\release_evidence.py path-check --root . --name artifacts/release/story-12-4-package-inventory.json --output artifacts\release\story-12-4-path-check.json`; `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter FullyQualifiedName~CiGovernanceTests --no-restore`; `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" --no-restore -m:1`.
- 2026-05-17: `python jobs\preflight-code-review.py --repo . --out artifacts\release\story-12-4-status-consistency.json` reported status-artifact consistency OK across 74 story keys and failed only working-tree-cleanliness because this Story 12.4 implementation is intentionally dirty.

### Completion Notes List

- 2026-05-13: Created the Story 12.4 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.
- 2026-05-13: Party-mode review findings applied inline. Added release context matrix requirements, release-ready derivation, owner approval matrix, side-effect contract, fallback classification, deterministic negative fixtures, implementation enforcement map, validation commands, and release-owner decision output. Final recommendation after applying low-risk updates: `ready-for-dev`.
- 2026-05-14: Advanced elicitation findings applied inline. Added replay/drift proof, release-definition fingerprints, post-seal artifact identity, single typed classification, concurrency/rerun reconciliation, fallback invalidation, and helper warning/partial-output fail-closed guardrails. Final recommendation remains `ready-for-dev`.
- 2026-05-15: Implemented the trusted release-readiness dry-run guardrail. Release readiness is now machine-classified by `eng/release_evidence.py classify-release`; `ready` requires trusted main/release context, sealed manifest verification, passing tests, valid inventory, SBOM, signing, timestamp, checksum, redaction, release-definition, rerun/concurrency, and owner approval evidence. Candidate PR/fork/local evidence remains blocked for publish authorization.
- 2026-05-15: Release workflow now requires a named release-owner approval gate before semantic-release can run. Semantic-release publish now verifies the sealed manifest and typed readiness decision immediately before NuGet push; partial-publish incident generation is scoped to the actual publish subphase rather than pre-publish guard failures.
- 2026-05-15: Final dry-run classification outcome for local Story 12.4 evidence is `blocked`/candidate by design; the committed positive fixture proves `ready` only for trusted context with explicit approval, and proves `fallback-approved` only when unsupported-attestation fallback metadata is complete and unexpired. No real NuGet publish, tag/changelog push, GitHub Release creation, or attestation upload was attempted.
- 2026-05-15: `deferred-work.md` was not updated because this story did not close or route a current deferred-work ledger row. Story 12.3 provider behavior and Story 12.5 accessibility/stakeholder evidence stayed out of scope.
- 2026-05-17: Resolved the remaining Story 12.4 review follow-ups. The release helper now fails closed for missing publish evidence, verifies sealed artifact digests against disk, separates approval-domain booleans from security booleans, gives rerun owners a typed fresh-dispatch/new-tag action, and emits advisory artifact metadata. The release workflow/config now removes repository-variable publish approval bypasses, forwards fork/dry-run/concurrency evidence, records pre-publish budget hash evidence, and emits phase-specific partial-publish incident JSON only from failed NuGet push phases.

### Party-Mode Review Trace

| Field | Value |
| --- | --- |
| ISO date and time | 2026-05-13T21:27:32+02:00 |
| Selected story key | `12-4-trusted-release-evidence-dry-run` |
| Command / skill invocation | `/bmad-party-mode 12-4-trusted-release-evidence-dry-run; review;` |
| Participating BMAD agents | Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect) |
| Findings summary | Initial consensus was `needs-story-update`: the story needed explicit release-context classification, owner approval boundaries, adjacent side-effect guards, fallback visibility, release-owner decision output, file-level enforcement map, deterministic negative fixtures, hostile redaction cases, and root-only submodule proof before development. |
| Changes applied | Added AC22-AC29, Decisions D9-D14, implementation enforcement map, release classification and approval guidance in the cheat sheet, side-effect gating subtasks, classification/fallback/approval tasks, required negative fixtures, validation command examples, and release-owner output requirements. |
| Findings deferred | Exact attestation fallback approving authority, evidence-retention location, release-budget blocking threshold, and partial-publish recovery policy remain release-owner / architecture decisions before publish authorization. |
| Final recommendation | `ready-for-dev` |

### Advanced Elicitation

| Field | Value |
| --- | --- |
| ISO date and time | 2026-05-14T11:36:04+02:00 |
| Selected story key | `12-4-trusted-release-evidence-dry-run` |
| Command / skill invocation | `/bmad-advanced-elicitation 12-4-trusted-release-evidence-dry-run` |
| Batch 1 method names | Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation |
| Reshuffled Batch 2 method names | Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records |
| Findings summary | The story was strong on side-effect ordering but still exposed release-owner ambiguity if old evidence is replayed after workflow/helper drift, if semantic-release and helper summaries disagree, if artifacts mutate after manifest sealing, if same-version reruns race partial publication, if fallbacks outlive their evidence scope, or if partial helper output is rendered as success prose. |
| Changes applied | Added AC30-AC35, Decisions D15-D20, release-definition fingerprinting, post-seal artifact identity checks, single typed final classification, same-version concurrency/rerun reconciliation, fallback invalidation triggers, partial-output fail-closed handling, and expanded negative-fixture requirements. |
| Findings deferred | Exact fallback approving authority, evidence-retention location, release-budget blocking threshold, and partial-publish recovery policy remain release-owner / architecture decisions before publish authorization. |
| Final recommendation | `ready-for-dev` |

### Change Log

- 2026-05-13: Created Story 12.4 and marked ready-for-dev.
- 2026-05-13: Party-mode review applied; hardened release classification, approval, side-effect, fallback, fixture, redaction, and owner-output guardrails.
- 2026-05-14: Advanced elicitation applied; added replay/drift, post-seal identity, typed classification, rerun/concurrency, fallback invalidation, and partial-output fail-closed guardrails.
- 2026-05-15: Implemented trusted release-readiness classification, owner approval gating, sealed-manifest verification hardening, negative fixtures, release workflow side-effect guards, and validation evidence; moved story to review.
- 2026-05-17: Addressed remaining Story 12.4 code review findings CR-12-4-P1..P15, M1..M7, and L1..L2; moved story to review after focused and release-filtered solution validation passed.

### File List

- `_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `.github/workflows/release.yml`
- `.releaserc.json`
- `eng/release_evidence.py`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/ci-governance/fixtures/release-manifest-valid.json`
- `tests/ci-governance/fixtures/release-readiness-cases.json`
