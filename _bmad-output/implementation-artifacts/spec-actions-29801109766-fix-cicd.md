---
title: 'Fix Release reusable-workflow permissions and CI governance drift'
type: 'bugfix'
created: '2026-07-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: '7870526090a8596082e3df034ecacf4c07881a04'
context: ['{project-root}/references/Hexalith.Builds/.github/workflows/domain-release.md', '{project-root}/_bmad-output/project-docs/deployment-guide.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release runs `29801109766` and `29804820898` ended in `startup_failure` with zero jobs: the caller grants `actions: none`, but the pinned Hexalith.Builds workflow requires `actions: read`. Quality run `29804662064` additionally exposes a false release-SHA/gitlink equality rule and stale analyzer inventory after the merged logging tests.

**Approach:** Grant and regression-test job-level `actions: read`. Preserve the reviewed release SHA by enforcing the authoritative `uses:@sha == builds-execution-sha` contract, remove the contradicted gitlink equality, correct the runbook, and refresh the analyzer inventory last.

## Boundaries & Constraints

**Always:** Preserve the CI-success/push guards, `freeze-guard`, exact publish-variable behavior, existing permissions, `test-projects: ''`, explicit `NUGET_API_KEY`, and NuGet-only posture. Keep both execution literals as one reviewed lowercase 40-hex SHA; validate `actions: read` inside `jobs.release.permissions`.

**Ask First:** Changing the reviewed release SHA, enabling publication, changing Release Owner controls/secrets, modifying a submodule, or expanding beyond the three evidenced runs requires approval.

**Never:** Do not bypass the freeze; use a mutable reusable revision; add containers, dispatch, inherited secrets, or error suppression; grant the permission only at workflow level; or equate the independent Builds gitlink with the approved release SHA.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Frozen release after green CI | Job grants `actions: read`; publish variable is not exactly `true` | Call validates, guard succeeds, release skips green without publication | Permission mismatch is blocked by governance |
| Reviewed release identity | Both execution literals match; gitlink differs | Governance passes without changing approved tooling | Invalid or unequal literals fail |
| Test inventory changes | Tracked test tokens/lines change | Ledger records final count/hash | Later drift fails closed |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- failing caller, permission map, and approved SHA pair.
- `references/Hexalith.Builds/.github/workflows/domain-release.yml` and `.md` -- read-only callee contract and caller example.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- release guard and job-permission extractor.
- `_bmad-output/project-docs/deployment-guide.md` -- runbook with the contradicted lockstep claim.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- Gate 2b identifier inventory.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- grant job-level `actions: read` and document two-way SHA equality plus gitlink independence.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- scope the permission assertion to `release`, remove gitlink equality, retain exact SHA-pair validation.
- [x] `_bmad-output/project-docs/deployment-guide.md` -- replace both three-way claims with the authoritative model.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh only final test inventory count/hash.

**Acceptance Criteria:**
- Given green push CI, when GitHub validates Release, then jobs are created; while frozen, the guard is green and publication skips.
- Given no job-level `actions: read`, when the focused guard runs, then it fails despite occurrences elsewhere.
- Given an equal exact SHA pair and different gitlink, when Gate 2b runs, then release governance passes without changing reviewed tooling.
- Given final tracked tests, when analyzer governance runs, then inventory has no drift.

## Spec Change Log

- 2026-07-21: Review tightened permission assertions against commented YAML and clarified release operations.

## Design Notes

Explicit job permissions make omitted scopes `none`, and reusable workflows cannot elevate them; the grant therefore belongs in `release`. The SHA pair selects approved release tooling, while the gitlink independently selects release-build/catalog content.

## Verification

**Commands:**
- `actionlint -no-color .github/workflows/release.yml` -- expected: no syntax/expression findings (supplementary; it cannot validate remote permission compatibility).
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release` -- expected: clean warnings-as-errors build.
- Run the built xUnit v3 assembly with `DiffEngine_Disabled=true` filtered to `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` -- expected: pass.
- Run the built xUnit v3 assembly with `DiffEngine_Disabled=true -class Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests` -- expected: pass with no ledger drift.
- Run the CI Gate 2b Governance command from `.github/workflows/quality.yml` -- expected: all configured Governance tests pass.

**Manual checks (if no CLI):**
- After merge, first confirm the publish variable is not exactly `true`; then confirm the next push-CI-triggered Release run creates `freeze-guard`, concludes non-startup-failure, remains frozen, and produces no publication side effect.

## Suggested Review Order

**Release permission and identity contract**

- Grant the callee's minimum required permission without weakening the freeze gate.
  [`release.yml:72`](../../.github/workflows/release.yml#L72)

- Keep approved execution literals equal while treating the gitlink independently.
  [`release.yml:83`](../../.github/workflows/release.yml#L83)

**Operational model**

- State the independent release-build dependency model in the workflow inventory.
  [`deployment-guide.md:84`](../project-docs/deployment-guide.md#L84)

- Explain job-scoped permission inheritance and pinned initializer behavior.
  [`deployment-guide.md:118`](../project-docs/deployment-guide.md#L118)

**Regression guards**

- Enforce exact immutable execution literals without coupling them to the gitlink.
  [`CiGovernanceTests.cs:448`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L448)

- Reject commented or misplaced permission text using active-line assertions.
  [`CiGovernanceTests.cs:468`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L468)

- Seal the final identifier inventory consumed by Gate 2b.
  [`analyzer-policy-exception-ledger-v1.json:72`](../contracts/analyzer-policy-exception-ledger-v1.json#L72)
