---
title: 'Repair CI and Quality, then produce the governed 4.2.0 release'
type: 'bugfix'
created: '2026-08-30'
status: 'done'
baseline_commit: 'f84b68b4e147238f28ca70219f19233d4b4b64d1'
review_loop_iteration: 0
context:
  - '_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Runs 33264036185 and 33264035739 fail because .NET 10 invokes xUnit 4 through the unsupported VSTest path, Windows executes a Unix-only Playwright environment assignment, and the Epic 9 live catch-up channel lets a stale circuit subscriber abort projection materialization. These failures prevent the authenticated CI handoff required for a release.

**Approach:** Migrate every active CI and release-preflight test command to Microsoft.Testing.Platform (MTP), make Playwright scripts cross-platform, isolate catch-up notifications per Blazor circuit with regression coverage, update governance authorization for changed workflow bytes, and land the fix through the repository's governed PR path. After exact-source workflows are green, dispatch and verify the governed `v4.2.0` release using the configured reviewer approvals.

## Boundaries & Constraints

**Always:** Preserve the pinned reusable workflow execution SHA `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, Release/Release Evidence workflow bytes, signing and immutable-release gates, package parity, user data, and root submodule boundaries. Retain test reports and coverage evidence under MTP. Keep `main` unchanged from the captured green SHA while release and evidence run.

**Ask First:** Halt if a required secret is unavailable, semantic-release no longer plans `4.2.0`, policy tooling cannot authorize the exact new caller bytes, a protected deployment cannot be approved by the configured reviewer, or a fix requires changing release workflow/evidence bytes or submodule contents.

**Never:** Bypass authorization, disable or forge gates/evidence, use admin bypass, publish directly with the NuGet key, skip failing tests, push directly to `main`, or initialize/update nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| .NET test lanes | SDK 10.0.400 and xUnit 4 | MTP runs filters, TRX reporting, and coverage without VSTest errors | Fail the lane on test/report/coverage failure |
| Windows visual lane | `npm run test:fc-nip` | Environment is set portably and Playwright produces artifacts | Preserve failure artifacts and fail on test error |
| Live catch-up | Multiple/retried Blazor circuits | Each circuit observes only its own commands and materializes created rows | A stale subscriber cannot block another circuit |
| Release handoff | Green push CI for exact `main` SHA | AD-13 is policy-authenticated and `v4.2.0` follows both normal approvals | Stop on identity, approval, publication, or evidence mismatch |

</frozen-after-approval>

## Code Map

- `global.json`, `.github/workflows/{ci,quality,nightly,quarantine-governance-nightly}.yml` -- runner selection and CI commands.
- `eng/run-lifecycle-property-suite.ps1`, `eng/release_prepublish.py` -- non-workflow and release-preflight test entry points.
- `tests/Directory.Build.props`, `tests/eng/test_release_prepublish.py`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- MTP extensions and contract coverage.
- `.github/scripts/ci_governance.py`, `tests/eng/test_ci_governance.py`, `tests/ci-governance/fixtures/mtp-quarantine/**` -- fail-closed MTP report, coverage, and quarantine evidence contracts.
- `tests/e2e/package.json`, `tests/e2e/package-lock.json` -- portable Playwright environment setup.
- `samples/Counter/Counter.Web/{Program,CounterCommandProjectionCatchUpChannel}.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Generated/{CounterStoryVerificationTests,CommandTargetGeneratedFormTests}.cs` -- catch-up lifetime, subscriber isolation, observable state, and bounded-dispatch regression coverage.
- `eng/dependency-graph-policy.json` -- exact workflow-byte authorization.
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json`, `eng/eventstore_runtime_evidence.py`, `tests/eng/test_eventstore_runtime_evidence.py` -- current approved root identity separated from the immutable historical Story 11.24 capture.

## File List

- `.gitignore`
- `.github/scripts/ci_governance.py`
- `.github/workflows/ci.yml`
- `.github/workflows/nightly.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/quarantine-governance-nightly.yml`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json`
- `_bmad-output/implementation-artifacts/spec-actions-33264036185-33264035739-fix-cicd-release.md`
- `_bmad-output/project-context.md`
- `eng/dependency-graph-policy.json`
- `eng/eventstore_runtime_evidence.py`
- `eng/release_prepublish.py`
- `eng/run-lifecycle-property-suite.ps1`
- `global.json`
- `samples/Counter/Counter.Web/CounterCommandProjectionCatchUpChannel.cs`
- `samples/Counter/Counter.Web/Program.cs`
- `tests/Directory.Build.props`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/README.md`
- `tests/ci-governance/fixtures/mtp-quarantine/malformed/malformed.trx`
- `tests/ci-governance/fixtures/mtp-quarantine/nested-a/module-a.trx`
- `tests/ci-governance/fixtures/mtp-quarantine/nested-b/deeper/module-b.trx`
- `tests/ci-governance/fixtures/mtp-quarantine/zero/zero.trx`
- `tests/e2e/package-lock.json`
- `tests/e2e/package.json`
- `tests/eng/test_ci_governance.py`
- `tests/eng/test_eventstore_runtime_evidence.py`
- `tests/eng/test_release_prepublish.py`

## Documented Unrelated Workspace State

- `_bmad-output/implementation-artifacts/deferred-work.md` - concurrent EventStore 3.100.0 work; not owned by this spec.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-100-0.md` - concurrent EventStore 3.100.0 spec; not owned by this spec.
- `references/Hexalith.Builds` - concurrent checkout movement for EventStore 3.100.0; the root gitlink is unchanged by this spec.
- `references/Hexalith.EventStore` - concurrent checkout movement to EventStore 3.100.0; the root gitlink is unchanged by this spec.

## Tasks & Acceptance

**Execution:**
- [x] Select MTP globally and convert all active workflow, nightly, script, and prepublication invocations to supported filters/reporting/coverage; update governing tests and focused documentation.
- [x] Add the smallest maintained cross-platform environment helper, regenerate the lockfile, and cover the Windows script contract.
- [x] Scope the catch-up channel to a circuit and add tests proving scope isolation and non-interference.
- [x] Generate/validate policy authorization for the changed CI caller bytes while preserving release identities.
- [ ] Run focused and full verification, commit with pinned commitlint validation, open a PR, and merge only after checks pass.
- [ ] Because evaluator authorization is active-base policy, land a second unchanged-workflow follow-up if needed so a later push CI can emit authenticated AD-13 evidence.
- [ ] Verify all applicable exact-source workflows and flaky-governance succeed; dispatch Release, approve both `production` deployments normally, and verify Release Evidence, tag, immutable assets, and all eight NuGet packages at `4.2.0`.

**Acceptance Criteria:**
- Given the repaired exact-source `main`, when push workflows run, then CI, Quality, CodeQL, Commitlint, and downstream governance complete successfully with authentic artifacts.
- Given a green authenticated CI handoff and unchanged `main`, when Release is dispatched and both configured reviews are approved, then Release and Release Evidence succeed and `v4.2.0` resolves to that SHA with eight published packages.

## Spec Change Log

- 2026-08-30: With explicit human approval, corrected the frozen reusable-workflow SHA from the planning transcription error `4eb33928a3e294b303bac5a09bfafcf2ea5459a4` to the repository's actual pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; this avoids changing or validating against a nonexistent identity.
- 2026-08-30: With explicit human approval after an Ask First halt, authorized FrontComposer-owned EventStore evidence/tests to reflect the existing root gitlink `38967215e6c1b13e77f2b0006efd95d88d7ad7b8`; the submodule pointer and contents remain unchanged.
- 2026-08-30: Review patch made MTP artifacts fail closed, hardened subscriber/timing regressions and runner documentation, and preserved the immutable Story 11.24 capture while recording the separately approved current root runtime tuple.

## Design Notes

Changing `ci.yml` changes its caller digest. The first landing authorizes those bytes; because dependency governance evaluates the event's base policy, a subsequent unchanged-workflow landing may be required before AD-13 becomes release-eligible. This is a policy transition, not an authorization bypass.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore` -- solution succeeds.
- `dotnet test <each test project> -c Release --no-build` plus focused MTP filter/report/coverage commands -- all test paths succeed.
- `npm ci && npm test` in `tests/e2e`, plus the Windows `test:fc-nip` workflow lane -- scripts and artifacts succeed.
- `python3 -m unittest discover -s tests/eng -p 'test_*.py'` and release contract/governance checks -- policies and preflight pass.
- Pinned commitlint CLI against each exact commit message and PR title -- validation succeeds before use.
- `gh run watch <run-id> --exit-status` for exact-source workflows, Release, and Release Evidence -- all conclude `success`.

## Suggested Review Order

**MTP execution and evidence**

- Start with the authoritative Quality lanes and their fail-closed evidence gates.
  [`quality.yml:253`](../../.github/workflows/quality.yml#L253)

- The reusable CI caller selects MTP while preserving the approved execution pin.
  [`ci.yml:25`](../../.github/workflows/ci.yml#L25)

- Repository-wide runner selection makes every active `dotnet test` entry point consistent.
  [`global.json:1`](../../global.json#L1)

- Central parsing proves distinct modules, nonzero tests, and valid Cobertura reports.
  [`ci_governance.py:151`](../../.github/scripts/ci_governance.py#L151)

- Test projects inherit the MTP coverage extension without affecting production projects.
  [`Directory.Build.props:1`](../../tests/Directory.Build.props#L1)

**Live catch-up isolation**

- Circuit-scoped registration removes cross-circuit subscriber interference.
  [`Program.cs:79`](../../samples/Counter/Counter.Web/Program.cs#L79)

- Per-handler publication prevents one stale subscriber suppressing live handlers.
  [`CounterCommandProjectionCatchUpChannel.cs:82`](../../samples/Counter/Counter.Web/CounterCommandProjectionCatchUpChannel.cs#L82)

- Behavioral tests cover separate scopes, subscriber faults, and observable materialization.
  [`CounterStoryVerificationTests.cs:64`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs#L64)

**Governed identity and release readiness**

- Exact CI caller bytes are authorized without changing reusable release identities.
  [`dependency-graph-policy.json:438`](../../eng/dependency-graph-policy.json#L438)

- Current EventStore approval is separated from immutable historical capture evidence.
  [`eventstore_runtime_evidence.py:81`](../../eng/eventstore_runtime_evidence.py#L81)

- Release prepublication uses the same MTP exclusions and deterministic per-project reports.
  [`release_prepublish.py:285`](../../eng/release_prepublish.py#L285)

**Portability and regression contracts**

- Browserless Playwright scripts use portable environment assignment on Windows and Unix.
  [`package.json:20`](../../tests/e2e/package.json#L20)

- Timing verification measures through observed dispatch under the original one-second bound.
  [`CommandTargetGeneratedFormTests.cs:675`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs#L675)

- Authoritative project guidance records native MTP filters, reports, and coverage behavior.
  [`project-context.md:218`](../project-context.md#L218)
