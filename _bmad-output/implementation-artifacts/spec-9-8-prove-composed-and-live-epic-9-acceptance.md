---
title: 'Story 9.8: Prove composed and live Epic 9 acceptance'
type: 'feature'
created: '2026-08-27'
status: done
baseline_commit: '1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3'
baseline_revision: '9d410d223f214f85695b13ede98dc8b63fbfc1c7'
story_id: '9.8'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
warnings: ['oversized']
deferred:
  - summary: >-
      Make the serialized AppHost fallback rebuild the complete candidate dependency graph instead of relying on pre-existing outputs for project references other than EventStore Aspire.
    evidence: |-
      The fallback prebuilds Hexalith.EventStore.Aspire, then builds the AppHost with BuildProjectReferences=false. A future candidate that changes another referenced project could therefore reuse stale output even though this iteration's application sources did not change after the successful development build.
    location: >-
      eng/run-epic9-live-proof.sh:345
    severity: high
  - summary: >-
      Capture structured fallback build invocations and results instead of reconstructing evidence commands and accepting any non-empty serialized-build log.
    evidence: |-
      Runtime metadata is synthesized from a second command list, while artifact validation proves only that apphost-serialized-build.log is non-empty. The new exact-argv and failure-path tests protect this iteration's commands, but the retained evidence does not itself bind executed argv and per-command results.
    location: >-
      eng/run-epic9-live-proof.sh:425
    severity: medium
  - summary: >-
      Apply an explicit redaction policy to the retained serialized build log.
    evidence: |-
      apphost-serialized-build.log retains raw restore and build output, whereas the proof's JSON and command/browser evidence use explicit redaction boundaries. Feed diagnostics could expose environment-specific paths or credential-bearing source details.
    location: >-
      eng/run-epic9-live-proof.sh:348
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Epic 9's implementation is present, but its story spec was deleted after merge and its retained live evidence identifies a dirty pre-implementation commit. Existing composed and artifact checks also leave tenant-scope transition and several live-proof claims weakly asserted.

**Approach:** Restore the Story 9.8 contract at the current clean baseline, close the observable composition and artifact-validation gaps without reopening the FC-NIP design, and produce fresh isolated Aspire/Playwright evidence bound to a committed candidate.

## Boundaries & Constraints

**Always:** Exercise the generated command, pending registration, resolver, scoped indicator service, and already-rendered generated-grid boundaries. Preserve explicit pre-dispatch target identity, callback/polling parity, atomic per-row first-wins provenance, localized `role="status"`/`aria-live="polite"` feedback, and scope-before-read behavior. Start FrontComposer through isolated Aspire orchestration, discover and wait for `counter-web`, retain redacted command/browser artifacts, and stop only the exact AppHost started by the proof. Treat `_bmad-output/implementation-artifacts/sprint-status.yaml` as read-only orchestrator bookkeeping.

**Block If:** The safe isolated live run cannot complete, any required composed scenario cannot be observed through the outer generated-grid surface, or evidence cannot be bound to a committed candidate. Record the exact command and blocker; focused tests never substitute for the live gate.

**Never:** Infer identity from projection nudges, aggregate IDs, visible-row diffs, result payloads, or ambient undeclared context. Do not add public APIs, edit generated `obj/` output or submodules, weaken materiality/scope/first-wins rules, stop or reuse an unrelated AppHost, write or revert sprint tracking, or represent stale/dirty evidence as final acceptance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Composed command matrix | Provider create, row-context update, cross-row/status move, delete; callback and polling | Each crosses generated command through pending/resolver/indicator/grid with its intended indicator or suppression | Unknown identity/materiality and delete suppress only FC-NIP; lifecycle remains truthful |
| Already-rendered grid | Add/materialization, filter/re-query, TTL, clear, tenant or user transition | DOM rerenders automatically, clears stale scope, stays lane-scoped, and preserves first-wins provenance and accessible localized status | No manual render or unrelated state mutation may stand in for notification |
| Live create/update | Fresh exact key absent before dispatch, then create and overlapping updates | Same key materializes, counts converge, one first-wins announcement appears, and materialization dismisses it | Mismatched/pre-seeded key or empty/unlocalized announcement fails |
| Artifact validation | Runtime metadata, browser evidence, redacted logs, JUnit/HTML, screenshot, trace, checksums | Candidate/endpoint correlate and every required observed claim is type/value checked | Missing, contradictory, sensitive, or stale fields fail closed |
| Existing AppHost or live failure | FrontComposer run already exists, or isolated run cannot reach `counter-web` | Unrelated run is untouched and exact failure evidence is retained | Story, Epic 9, FR-13, and FR-26 remain open |

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:1520-1541` -- canonical Story 9.8 Given/When/Then acceptance and live-failure rule.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs:45-361` -- composed callback/polling command matrix, automatic grid invalidations, explicit filter/re-query behavior, and both tenant and user scope transitions.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:651-727,1107-1245` -- read-only generated-command target capture, early callback buffering, and accepted association boundary.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:105-246,375-440` -- read-only single terminal owner and eligible publication boundary.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1040-1088,1433-1494,1884-1889` -- read-only generated-grid subscription, dismissal, and indicator render boundary.
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts:6-170` -- live exact-key create/update, localized first-wins announcement, accessibility, materialization, dismissal, scope, and typed durable evidence producer.
- `tests/e2e/scripts/validate-epic9-artifacts.mjs:1-436` -- retained-bundle/redaction validator; checks candidate and endpoint correlation plus every required typed/value browser claim, checksum path, and sensitive-data boundary.
- `eng/run-epic9-live-proof.sh:1-598` -- safe isolated Aspire start/wait/describe, bounded serialized dependency/AppHost fallback, browser execution, redacted logs, validation, checksums, and exact cleanup.
- `.github/workflows/quality.yml:522-583` -- blocking live lane and 14-day artifact upload; artifact name is the source of truth for refreshed evidence wording.
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md:1-78` -- accepted strict proof record for clean reviewed candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c` and its correlated final bundle.
- `artifacts/epic-9-final-7a573763/` -- accepted checksummed final proof root; historical and development bundles remain preserved separately.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned and strictly read-only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs` -- extend the already-rendered-grid proof to cover both tenant and user transitions and make filter/re-query invalidation observable without manual render.
- [x] `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts` -- assert non-empty localized announcements and emit explicit exact-key, count, accessibility, first-wins, scope, and dismissal claims.
- [x] `tests/e2e/scripts/validate-epic9-artifacts.mjs`, `tests/e2e/scripts/validate-epic9-artifacts.test.mjs`, `tests/e2e/scripts/run-epic9-live-proof.test.mjs`, `tests/e2e/package.json`, and `.github/workflows/quality.yml` -- validate every required claim, redaction boundary, strict preflight, and unrelated-AppHost refusal with positive/negative fixtures, and run those checks in the blocking live job.
- [x] `eng/run-epic9-live-proof.sh` -- keep safe isolated lifecycle behavior and add a strict final-evidence mode that rejects a dirty or mismatched candidate while preserving an explicit development-mode diagnostic run.
- [x] `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` and `artifacts/epic-9-final-7a573763/` -- strict live acceptance passed against clean reviewed candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`; the record retains the exact command, versions, endpoint, result counts, artifact name, checksums, and remaining solution-lane blocker.
- [x] `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md` -- reconcile verification, File List, review findings, and commit scope without touching sprint tracking.

**Acceptance Criteria:**
- Given Stories 9.3-9.7 are independently complete, when the automated composition lane runs, then standalone create, row-context update, cross-row/status move, callback confirmation, and polling cross the generated-command-to-grid chain with the intended indicator disposition.
- Given the grid is already rendered, when add/materialization, filter/re-query, TTL, clear, tenant transition, or user transition occurs, then the DOM updates without manual render, prior-scope state is unreadable, and first-wins accessible localized feedback remains correct.
- Given no unrelated FrontComposer AppHost is running and the candidate is committed, when the isolated live proof runs, then `counter-web` is discovered, the fresh exact key is absent before create and materializes under that key, create/update/dismissal/first-wins evidence passes, and the redacted checksummed bundle records the candidate and endpoint.
- Given an existing AppHost or environment failure, when the proof stops, then it leaves unrelated processes untouched, records the exact command and blocker, and does not substitute focused success for Story 9.8 acceptance.

## Spec Change Log

- 2026-08-28: Committed review repairs as `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`; strict acceptance reran with the supported explicit expected-commit pin and passed at `https://localhost:39831`, producing the accepted `artifacts/epic-9-final-7a573763/` bundle.
- 2026-08-28: Committed the serialized-fallback hardening as `cd67e933b7e381f8da170c7fa6843ff3aae75802`; strict isolated Aspire/Playwright acceptance, final artifact validation, full checksums, and exact AppHost cleanup passed against that clean candidate at `https://localhost:41819`.
- 2026-08-28: Reopened final acceptance because the hardened validator rejects the historical `f4f43fdc` checksum paths and a clean checkout exposed a missing EventStore Aspire dependency in the serialized fallback. Added the bounded dependency prebuild and exact-command fixtures; 85/85 evidence tests and a complete isolated development proof passed before the subsequent strict committed-candidate proof succeeded.
- 2026-08-27: Hardened composed scope/filter proofs, browser claims, artifact validation, CI wiring, and final/development proof modes. A complete isolated development proof passed and is recorded without claiming final acceptance; the clean committed-candidate proof remains open.
- 2026-08-27: Committed the hardened candidate as `f4f43fdc3053e45ffb939b718c670afb4cfcecd0`; strict isolated Aspire/Playwright acceptance, final artifact validation, checksums, and AppHost cleanup all passed against that clean SHA.

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 0, medium 5, low 4)
- defer: 3: (high 1, medium 2, low 0)
- reject: 9: (high 0, medium 4, low 5)
- addressed_findings:
  - `[medium]` `[patch]` Asserted the complete executed argv for both serialized fallback builds.
  - `[medium]` `[patch]` Added dependency-build failure short-circuit coverage.
  - `[medium]` `[patch]` Added AppHost-build failure short-circuit coverage.
  - `[medium]` `[patch]` Corrected strict proof commands to use `FC_EPIC9_EXPECTED_COMMIT`.
  - `[medium]` `[patch]` Reclassified substantive historical Epic 9 delivery work from workflow-only `process` to `shared`.
  - `[low]` `[patch]` Refreshed stale Code Map descriptions for the completed composition and artifact assertions.
  - `[low]` `[patch]` Replaced pending/new-root language with the accepted strict evidence state.
  - `[low]` `[patch]` Clarified original delivery scope versus resumed-review baseline semantics.
  - `[low]` `[patch]` Recorded the successful strict story-artifact validation command and result.

## Design Notes

The implementation merged in `f1b16a25d2a0a32ee437f5d8dfa786577402b416`; its spec was later deleted by `25cd54bd502b933900fceeb439ee7f6238c44553`. Frontmatter `baseline_commit` (`1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3`) is the original restored Story 9.8 delivery-scope base used for commit-range reconciliation. Frontmatter `baseline_revision` (`9d410d223f214f85695b13ede98dc8b63fbfc1c7`) is the HEAD at which this resumed review began; it bounds the resume-review context without resetting ownership or absorbing later submodule pointer movement. Historical and development proof bundles remain preserved, while `artifacts/epic-9-final-7a573763/` is the accepted strict reviewed-candidate evidence.

## Verification

**Commands:**
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:epic-9-evidence` -- expected: browser sources compile and artifact-validator plus proof-runner positive/negative cases pass.
- `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` followed by the built xUnit assembly filtered to `Epic9CompositionTests`, the seeded Counter page contract, and the identifier seal -- expected: all focused acceptance checks pass.
- `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-final-7a573763 FC_EPIC9_REQUIRE_CLEAN=true FC_EPIC9_EXPECTED_COMMIT=7a5737630611b4d54b0180a3fa4c9c4ccd23a28c ./eng/run-epic9-live-proof.sh` followed by `npm --prefix tests/e2e run validate:epic-9-artifacts -- artifacts/epic-9-final-7a573763 --candidate 7a5737630611b4d54b0180a3fa4c9c4ccd23a28c` and `(cd artifacts/epic-9-final-7a573763 && sha256sum -c checksums.sha256)` -- passed: isolated live proof retained the redacted, internally correlated final bundle for the exact clean reviewed candidate and stopped only its owned AppHost.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: broad gate passes, or any pre-existing dependency blocker is recorded separately with its exact output.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md --candidate HEAD` -- expected: Story 9.8 commits and changed paths reconcile with no sprint-status write.

**Results (2026-08-28):** TypeScript typecheck passed; artifact-validator and proof-runner fixtures passed 87/87; the Release Shell.Tests build passed with 0 warnings and 0 errors; `Epic9CompositionTests` passed 2/2; the seeded Counter page and identifier-seal facts each passed 1/1. The historical `f4f43fdc` bundle now fails the current validator because its checksum paths begin with `./`. A clean detached run reproduced the serialized fallback's missing `Hexalith.EventStore.Aspire.dll`; the bounded dependency prebuild fixes that failure. Strict final proof passed Playwright 1/1, artifact validation, full checksums, and exact AppHost cleanup against clean reviewed candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c` at endpoint `https://localhost:39831` in `artifacts/epic-9-final-7a573763/`; `aspire ps --format json` returned `[]` after the run. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md --candidate HEAD` passed before the review commit for candidate `cd67e933b7e381f8da170c7fa6843ff3aae75802`; the final scope gate below reconciles the complete story range through the reviewed candidate without a sprint-status write. The exact solution default lane remains blocked at restore by pre-existing `NU1109` (`FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`; the central catalog selects `FsCheck 3.3.3`).

## Commit Scope Dispositions

- `a7de1113e50b1e98bb1f5d7d9dcacb1ac9b78c32` | `shared` | Substantive historical Epic 9 evidence work predates restoration of Story 9.8 attribution; it touched shared evidence paths but is not workflow-only work or owned by this restored story scope.
- `d928f5f7149feaadf0e384e6e7c90c1472bc0e4d` | `shared` | Story 9.3 command-target contract migration changed the shared e2e package manifest independently of Story 9.8.
- `9ad4312fb93fe0fef389d40e5abbeed241a2d73d` | `shared` | Projection-resilience follow-up work resealed the shared analyzer-policy ledger independently of Story 9.8.
- `9d410d223f214f85695b13ede98dc8b63fbfc1c7` | `process` | Automated blocked-status write-back recorded the prior commit-scope gate result while also carrying unrelated submodule pointer synchronization.

## File List

- `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md`
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `.github/workflows/quality.yml`
- `eng/run-epic9-live-proof.sh`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/scripts/validate-epic9-artifacts.mjs`
- `tests/e2e/scripts/validate-epic9-artifacts.test.mjs`
- `tests/e2e/scripts/run-epic9-live-proof.test.mjs`
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`

## Auto Run Result

Status: done

Summary: Hardened the serialized Aspire fallback by building the missing EventStore Aspire dependency before the no-project-reference AppHost build, aligned the retained command contract, added exact-argv and build-failure regression coverage, and produced strict live evidence against the clean reviewed candidate.

Files changed:

- `eng/run-epic9-live-proof.sh` -- prebuilds the bounded EventStore Aspire dependency and records both fallback build commands.
- `tests/e2e/scripts/run-epic9-live-proof.test.mjs` -- verifies exact fallback argv and both build-failure short-circuit paths.
- `tests/e2e/scripts/validate-epic9-artifacts.mjs` -- expects the two-command serialized fallback evidence sequence.
- `tests/e2e/scripts/validate-epic9-artifacts.test.mjs` -- updates positive and negative artifact fixtures for that sequence.
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` -- records the final reviewed candidate, endpoint, observed claims, and selected checksums.
- `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md` -- records scope dispositions, review triage, verification, deferred risks, and final state.

Review findings: 9 patches applied, 3 items deferred, and 9 items rejected. Patched severity counts were high 0, medium 5, and low 4; follow-up score is `3 × 5 + 1 × 4 = 19`, so `followup_review_recommended` is `true`.

Verification performed:

- TypeScript typecheck passed and Epic 9 evidence/proof-runner tests passed 87/87.
- Release Shell.Tests build passed with 0 warnings and 0 errors; focused composition tests passed 2/2, and the seeded Counter page and identifier seal passed 1/1 each.
- Strict isolated Aspire/Playwright proof passed 1/1 against `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`; artifact validation and every checksum passed, and `aspire ps --format json` returned `[]`.
- Strict story-artifact validation passed with full-SHA commit dispositions and no sprint-status write.
- The broad solution lane reproduced the pre-existing restore-only `NU1109` mismatch between `FsCheck.Xunit.v3 3.3.4` and centrally pinned `FsCheck 3.3.3`.

Residual risks: the three frontmatter `deferred` items cover complete dependency-graph rebuilding, structured executed-command evidence, and serialized-build-log redaction. The broad solution lane remains unavailable until the unrelated central FsCheck pin is corrected.
