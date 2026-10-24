---
title: 'Simplify evidence: story file + CI only, remove committed evidence and its machinery'
type: 'chore'
created: '2026-10-23'
status: 'done'
baseline_commit: '4a4051c2511e5c4b7c0285fbc129c72146c1c275'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Committed evidence (`_bmad-output/implementation-artifacts/evidence/`, 24,610 files, ~5 GB; plus `artifacts/ide-parity/evidence/`, `tests/…Shell.Tests/EndToEnd/evidence/`) dominates checkout size and Git history, and runtime-identity/packet tooling, CI steps and governance tests exist only to seal and re-verify it. The 2026-10-21 storage amendment (Stories 15.6/15.7) adds more machinery instead of removing it.

**Approach:** Same rule as EventStore `6a0b6441f`: proof = the repo's tests + CI; a story/spec records commands, pass/fail counts, commit SHA and CI run link. Every `evidence/` folder becomes ignored local scratch. A pre-commit hook and a CI job reject any tracked/staged path under an `evidence/` folder and any file over 1 MiB (small allowlist). Delete all committed evidence and the tools, tests, CI steps and docs that only produce or re-verify it.

**Owner decisions (2026-10-23):** story-file-only policy; remove all evidence + its machinery; no history rewrite here (later, coordinated with EventStore); supersedes Stories 15.6/15.7 and `sprint-change-proposal-2026-10-21.md`.

## Boundaries & Constraints

**Always:** Work in worktree `../fc-evidence-simplify`, branch `fix/evidence-storage-simplification` (base `4a4051c2`). Keep real product tests, the release pipeline (`release*.yml`, `eng/release_*.py` write only ignored dirs/GitHub assets), CI-time Playwright/a11y validators (`tests/e2e/scripts/*`), Actions artifact uploads, and the IDE-parity drift job. `build` commit type, validated with the repo commitlint.

**Never:** No edit to `ci.yml`, `release.yml`, `release-publish.yml` (byte-pinned in `eng/dependency-graph-policy.json`). No edit to `spec-13-6-verify-responsive-and-assistive-accessibility.md` or `epic-13-context.md` (uncommitted Story 13.6 work in the main checkout). No `src/` behaviour change, submodule change, history rewrite or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Normal commit | staged small source/doc files | pass | N/A |
| Evidence path | staged/tracked path with an `evidence/` segment anywhere, any case (even `git add -f`) | fail, path listed | exit 1 + fix hint |
| Large file | file > 1,048,576 bytes not allowlisted | fail, path + size | exit 1 |
| Allowlisted | `_bmad-output/implementation-artifacts/deferred-work.md` | pass | N/A |
| Gitlink / deletion | submodule bump, deleted file | ignored | N/A |
| Git failure | listing command fails | stop | exit 2 |

</frozen-after-approval>

## Code Map

- Guard: copy EventStore `scripts/check-committed-files.sh` + `tools/tests/test_check_committed_files.py` → `eng/check-committed-files.sh` + `tests/eng/test_check_committed_files.py` (repo Python tests live in `tests/eng/`); widen `is_evidence_path` to any `evidence/` segment; allowlist `deferred-work.md` (1.78 MB live ledger read by `DiagnosticRegistryTests.cs:1828-1860`). `.husky/` has only `commit-msg`; `package.json` has `"prepare": "husky"`.
- Delete content: `_bmad-output/implementation-artifacts/evidence/` (already `git rm --cached`), `artifacts/ide-parity/evidence/*.json`, `tests/…Shell.Tests/EndToEnd/evidence/*.png` + `EndToEnd/2-2-e2e-results.json`, `test-results/` (21 stray Playwright files), `_bmad-output/contracts/analyzer-elevation-*-2026-07-16.binlog.gz` (no reader), `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v{1,2,3}.json`, `_bmad-output/implementation-artifacts/tests/{story-13-6-historical-runtime,epic-13-follow-through-2026-10-22}.json` (+ `mcp-g7-*` if evidence-only).
- Delete tools: `eng/eventstore_runtime_evidence.py`, `eng/pact_provider_apphost_smoke.py` (imports it 66×; AppHost still runs in `epic9-live-acceptance`), `eng/replay_story_13_6.py`, and their `tests/eng/test_*.py`.
- `.github/workflows/quality.yml` (not pinned; nothing depends on it): drop dispatch inputs ~L24-44; L181 replay (restores the single-line `run:` that `CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` expects — fails at HEAD today); steps ~L209-230, L331-361 (Aspire/Dapr install + AppHost smoke), L363-421, L444-471; in L232-329 keep provider build/test/run with `--report-output` under `artifacts/contracts/`; L423-442 → plain `./eng/validate-contract-artifacts.ps1`; L514-524 upload globs. New guard job appended at file end (not between `build-and-test` and `accessibility-visual`: `CiGovernanceTests:215-217` slices there).
- Trim: `eng/validate-contract-artifacts.ps1` (params 5-17, L295-418, summary L425-438); `CiGovernanceTests.cs` (L3980-4697 runtime-identity test, L1277-1281 apphost pin, Gate 2c asserts ~L358-378 to match new steps); `EventStorePactContractTests.cs:692-789` handoff text + regenerate `Pact/provider-verification-handoff.md`; `IdeParityMatrixContractTests.cs` (L116-180, L192-205, evidence asserts L24-37/72-84/106-110, helpers L364-452); `IdeParityConformanceUtilityTests.cs` `EvidencePathNormalization_*` + `IdeParityConformanceHelpers.cs` `IdeParityEvidencePath`; `docs/ide-parity-matrix.{json,md}` evidence fields/column; `docs/reference/ide-parity.md:16`; `docs/reference/pact-contracts.md` L45-203 (pinned by `CiGovernanceTests:4434-4449`); `tests/README.md` 127, 235; `analyzer-elevation-decision-2026-07-16.md` L67-75.
- Repoint: `tests/…/EndToEnd/run-story-2-2-e2e.cjs:10-14` default output → `artifacts/story-2-2-live`.
- Config: `.gitattributes` delete L10-53 (keep L1-9); `.gitignore` delete L88-92, L125-128, L478-479, add `**/evidence/` + `/test-results/` block near L458.
- Planning: `prd.md` L577-579 (§7) → policy, L480 FR-24 pointer, G-3 L35 / FR-29.7 L534 / D-12 L698 → SHA + CI link; `architecture.md` L307 + identity-governance L388-396; `epics.md` 15.6/15.7 superseded, Epic 15 note L375, 13.6 amendment L3644, 16.1 ACs (tuple + CI link in story), 11.24/11.25/13.8 one-line notes; `ux-design.md:396`; `sprint-change-proposal-2026-10-21.md` superseded header; `sprint-status.yaml` 15-6/15-7 → `done` + `# superseded` comment (L227-229; `superseded` is not a sprint status), drop L749-750; `spec-11-25-eventstore-3-106-evidence-reconciliation.md` → `superseded`; one policy line in `spec-13-6-close-automated-verification-gaps.md`, `spec-13-6-complete-automated-accessibility-coverage.md`, `13-6-accessibility-result.md`; `_bmad-output/project-context.md` "Never" rule ~L352.

## Tasks & Acceptance

**Execution:**
- [x] `eng/check-committed-files.sh`, `tests/eng/test_check_committed_files.py` -- guard + unittest covering every matrix row.
- [x] `.husky/pre-commit`, `.husky/pre-merge-commit`, `quality.yml` guard job -- run guard (`--staged` / full tree) and its unittest.
- [x] Delete content and tools; trim tests, workflow, scripts and docs per Code Map; repoint the 2.2 runner.
- [x] `.gitignore`, `.gitattributes` -- evidence blocks replaced.
- [x] Planning + project-context -- policy stated once (PRD §7), others point to it.

**Acceptance Criteria:**
- Given the branch, when `bash eng/check-committed-files.sh` runs, then exit 0.
- Given the branch, when the solution builds Release `-warnaserror`, then 0 warnings/errors, and Shell.Tests (incl. Governance, IdeParity, Pact), `eng.tests.test_validate_story_artifacts` and the `tests/eng` suites run by `quality.yml` pass.
- Given tracked files, when `git grep` searches for `eventstore_runtime_evidence`, `pact_provider_apphost_smoke`, `replay_story_13_6`, then only historical story/spec prose matches.

## Implementation Notes

- Guard: `eng/check-committed-files.sh` matches any `evidence/` directory segment (root or nested, case-insensitive); the allowlist holds only `deferred-work.md`. The Quality `committed-files` job is appended after `debug-gated-tests` and runs the guard over the full tree plus its unittest; `ci.yml` is untouched.
- Gate 2c keeps pacts → provider tests + `live-compatibility` run (report `artifacts/contracts/provider-verification.json`, uploaded as a CI artifact) → `./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` (report exists, non-empty, redaction-clean) → stale-pact guard. The `workflow_dispatch` inputs are gone; a bare trigger stays; concurrency is `quality-${{ github.ref }}`.
- `mcp-g7-candidate*.json` and `mcp-g7-build-test*.txt` (hash manifests and run logs) were deleted; `mcp-g7-security-review-2026-09-26.md` stays because PRD G-7 cites it as the sign-off record.
- Follow-on fixes forced by the deletions: CA1707 identifier inventory resealed 3578 → 3574 in `analyzer-policy-exception-ledger-v1.json` (four removed underscore test names); producer fingerprint of `docs/ide-parity-matrix.md` updated; `sprint-status.yaml` E13R-AI-1/AI-4 `evidence:` repointed from the deleted follow-through JSON to `spec-epic-13-retrospective-follow-through.md`; `docs/accessibility-verification/baseline-change-rationale.md` no longer links the deleted before-images.
- Left in place: `eng/validate-story-artifacts.py` still lists `eng/pact_provider_apphost_smoke.py` in `HISTORICAL_OWNED_MIXED_UNOWNED_PATHS` — the digest-sealed path inventory of historical commit `ee9762e6`, not a reference to the tool. The `13-6-accessibility-result.md` policy line is appended at the end because `main` (`6f6eb898`) inserted a new section right after its title.
- Pre-existing failures at base `4a4051c2` (unchanged inputs, not caused here): `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` and 8 cases in `tests/eng/test_dependency_graph.py` (policy pins `FsCheck.Xunit.v3` 3.5.0, Builds catalog has 3.6.0); `CiGovernanceTests.ToolchainPins_MatchApprovedDotnetAndAspireVersions` (test expects `Aspire.AppHost.Sdk/13.6.1`, AppHost uses 13.6.3).
- Review pass 1 patches (triage rows 1-13): validator switch in CI + governance pin; `tests/eng/test_validate_contract_artifacts.py` (4 cases, run in Gate 2b); new `epic9-live-acceptance` step restarts the prebuilt AppHost and requires 401/403 for a bogus bearer on command submit, command status, query and hub negotiate (checked locally against stubbed `aspire` + fake 401/200 servers; live behaviour CI-only); stale IDE drift text; bare `workflow_dispatch`; guard-job comment; case-insensitive `.gitignore` evidence pattern; pact-contracts local recipe matches CI; G-3/D-12/architecture/16.1 share one closure set; mcp-g7 note; architecture/ux pointers to PRD §7; CONTRIBUTING line; guard test slice bound. Row 14 deferred to `deferred-work.md`.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-10-23; Blind Hunter BH, Edge Case Hunter ECH, Verification Gap VG). Diff: all non-deletion changes since baseline plus the deletion file list outside the evidence folder.

| # | Source | Finding | Verdict | Evidence / route |
|---|--------|---------|---------|------------------|
| 1 | BH, ECH, VG | Gate 2c runs the contract validator without `-RequireProviderVerification`, so the uploaded provider report is never redaction-scanned; `CiGovernanceTests` pins the omission | medium | Confirmed `quality.yml` `run: ./eng/validate-contract-artifacts.ps1`; base passed the switch. Patch: pass the switch, update the pin. |
| 2 | VG | The kept PowerShell redaction checker lost its only tests with `test_eventstore_runtime_evidence.py` | medium | Gap pre-verified (no test runs the script). Patch: port the two deleted cases to `tests/eng/test_validate_contract_artifacts.py`, run it in a Gate 2b step. |
| 3 | BH, VG, ECH | Deleting the AppHost smoke drops the only invalid-bearer 401/403 checks on the AppHost-composed command, status, query and SignalR endpoints; the `epic9` lane is positive-only | medium | Gap pre-verified by grep of `tests/e2e`, `eng/run-epic9-live-proof.sh`. Not a small addition inside the `epic9` proof (no EventStore endpoint there; its command list is the recorded 9.8 proof format). Owner chose a separate CI step: patched as the `epic9-live-acceptance` step "AppHost rejects invalid bearer tokens". |
| 4 | BH, ECH, VG | `jobs/ide-parity-version-revalidation.ps1:252` still asks to refresh deleted evidence manifests | low | Confirmed. Patch: reword to the story-record policy. |
| 5 | BH | `workflow_dispatch` trigger removed with its inputs | low | Spec dropped only the inputs; a bare trigger lets Gate 2c run on a chosen commit. Patch: restore bare trigger. |
| 6 | BH, ECH | `committed-files` comment claims a hook bypass "cannot land" evidence; main has no protection | low | Confirmed (`branches/main/protection` 404, no ruleset). Patch: reword as detection on every push/PR. |
| 7 | BH | `.gitignore` `**/evidence/` is case-sensitive while the guard is not | low | One-line correction. Patch: case-insensitive pattern. |
| 8 | BH | Local recipe in `docs/reference/pact-contracts.md` dropped the provider test-assembly run CI still does | low | Confirmed against `quality.yml`. Patch: restore the step in the recipe. |
| 9 | ECH | Story 16.1 AC cites the `epic9` AppHost result; PRD G-3/D-12 cite only Gate 2c | low | Two authorities differ. Patch: state the same closure set (Quality run: Gate 2c provider + `epic9-live-acceptance`) in PRD G-3/D-12, architecture. |
| 10 | ECH | `mcp-g7-security-review-2026-09-26.md` links the deleted `mcp-g7-*` files and their manifest hash | low | Confirmed. Patch: add the dated policy note there. |
| 11 | ECH | Policy restated in full in architecture/ux/pact-contracts, not once | low | Drift risk on the next policy change. Patch: reduce to pointers to PRD §7 (project-context keeps its Never rule). |
| 12 | BH | Contributor docs do not mention the guard or that hooks need a root `npm ci` | low | Patch: one line in `CONTRIBUTING.md`. |
| 13 | BH | Guard test slices `quality.yml` from `committed-files:` to EOF; a later job breaks it | low | Direct correction. Patch: stop at the next top-level job. |
| 14 | ECH | Shell.Tests `CentralPackageVersions…`/`ToolchainPins…` and `test_dependency_graph.py` fail, so build-and-test stays red | medium | Pre-existing: Quality on main fails at base `4a4051c2` (run list), inputs (`dependency-graph-policy.json`, Builds gitlink, AppHost) untouched by this diff. Defer. |
| 15 | BH | `-RequireProviderVerification` does not check verdict/interaction count/staleness | low | Provider app exits nonzero on any failed interaction and CI deletes the old report first; adding semantic checks adds validation logic. Rejected. |
| 16 | BH, ECH | IDE matrix JSON lost its machine-readable `revalidation-pending` status | low | `docs/ide-parity-matrix.md` states 10.0.402 is not revalidated; restoring the field re-adds evidence metadata and schema. Rejected. |
| 17 | BH, ECH | Open DW entries target deleted code | low | bmad-loop sweep verifies entries against code (same as EventStore triage #11). Rejected. |
| 18 | BH | Spec record lacks the change SHA and CI link | — | Fix edits this spec; results are recorded at presentation. Rejected per workflow. |
| 19 | BH | Allowlist has no size ceiling | low | One fixed entry; a ceiling adds parameters (EventStore triage #19). Rejected. |
| 20 | BH | CI run links/artifacts expire before late sign-off | low | Owner's policy chose commands + counts + SHA + CI link, so outcomes are copied into the story. Rejected (intent). |
| 21 | BH | History rewrite has no tracked follow-up | low | Intent excludes it ("later, coordinated with EventStore"). Rejected. |
| 22 | BH | Guard tests miss spaces/newlines, symlinks, ITA, cat-file failure | false | `-z` parsing handles names; only mode 160000 is skipped; a size-read failure exits 2 (EventStore triage #20). |
| 23 | ECH | Empty or directory `-ProviderVerificationReport` throws instead of reporting | false | Default value is used by every caller; a loud failure on an unreached input is correct. |
| 24 | ECH | A PR commit could add then remove evidence and a non-squash merge lands the blob | low | Range scan adds complexity; the hook covers local commits (EventStore triage #6). Rejected. |
| 25 | ECH | `--staged` during an unresolved conflict exits 2 without naming the path | false | `git commit` refuses unmerged paths before hooks run; manual use fails loudly with exit 2. |
| 26 | ECH | Must row IDE-REMOTE-001 has no trait-tagged test after the deletions | low | Tagging an unrelated test would be fake coverage; no gate counts traits. Rejected. |
| 27 | ECH | AC grep still matches `eng/validate-story-artifacts.py:404` and ledger entries | false | Both are historical records (digest-sealed path inventory of `ee9762e6`; ledger history), not live references. |

## Design Notes

Decisions (agent-owned): keep the release pipeline despite its "evidence" name (GitHub assets, not commits); delete the AppHost smoke rather than untangle it from the runtime-identity module; allowlist `deferred-work.md` rather than split it (the archive is already 764 KB); leave Story 13.6's in-progress spec to its session — the hook will reject new evidence commits there.

## Verification

**Commands:**
- `bash eng/check-committed-files.sh` -- expected: exit 0
- `python3 -m unittest eng.tests.test_validate_story_artifacts` and each `tests/eng/test_*.py` that `quality.yml` runs, plus the guard test -- expected: all pass
- `dotnet build Hexalith.FrontComposer.slnx -c Release -warnaserror -p:NuGetAudit=false` -- expected: 0 errors
- built Shell.Tests and SourceTools.Tests DLLs -- expected: pass (live/Playwright lanes: CI only)

**Results (2026-10-23, local, base `4a4051c2` + this change; CI run link: pending push):**
- `bash eng/check-committed-files.sh` -- exit 0, 6,275 tracked files checked (before the remaining deletions it reported 25 violations: 22 evidence paths, 3 binlogs over 1 MiB).
- `python3 -m unittest tests/eng/test_check_committed_files.py` -- 20/20 passed.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- 222 passed (2 skipped); `python3 eng/validate-story-artifacts.py` -- passed.
- `tests/eng` suites run by `quality.yml` -- pack/prepublish 60, adopter proof, workflow source closure 11, dependency handoff, release contract 22, release evidence v2 15, AppHost dependency modes 7: all passed; `test_dependency_graph.py` 102 run, 1 failure + 7 errors, all the pre-existing FsCheck 3.5.0/3.6.0 catalog drift.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -warnaserror -p:NuGetAudit=false` -- 0 warnings, 0 errors.
- SourceTools.Tests DLL (default-lane trait exclusions) -- 1,411/1,411 passed.
- Shell.Tests DLL (default-lane trait exclusions) -- 3,762 run, 3,760 passed after the CA1707 reseal; the 2 failures are the pre-existing `CentralPackageVersions…` and `ToolchainPins…` facts. Governance + Pact namespaces rerun: 191 run, same 2 failures. `StoryArtifactValidatorGate_IsBlockingAndExact`, red at base, now passes.
- Story 2.2 runner contract (`node --test run-story-2-2-e2e.test.cjs`) -- 6/6 passed. `pwsh ./eng/validate-docs.ps1` -- "Docs validation passed", zero blocking failures (the pwsh host then hung and was stopped).
- Not run locally: `GovernanceBuild` trait lane, live provider verification, Playwright/a11y and Epic 9 live lanes (CI only).
- After review patches (2026-10-24): guard exit 0 (6,280 files); guard tests 20/20; contract-validator tests 4/4; `tests/eng` suites all pass except the deferred `test_dependency_graph.py` (102 run, 1 failure + 7 errors, pre-existing); story validator tests 222 OK (2 skipped) and validator passed; Release `-warnaserror` build 0 warnings/0 errors; solution default lane 6,192/6,194 and Governance lane 536/538 (both failures the deferred pre-existing pins); Contract lane 3/3; `actionlint` clean.
