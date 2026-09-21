---
title: 'Actions 35569823840: Repair and complete the 4.5.1 release'
type: 'bugfix'
created: '2026-09-21'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-docs/deployment-guide.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run `35569823840` planned `4.5.1` and prepared its candidate, but `HEXALITH_RELEASE_PUBLISH_ENABLED=false` made the shared publisher skip Semantic Release. The caller then treated the successful no-op reusable job as publication and failed while verifying a GitHub Release and NuGet packages that did not exist.

**Approach:** Declare the supported publication-authority opt-out on the reusable release call, activate the changed workflow through the required two-push evaluator-policy sequence, temporarily enable the Release Owner publication switch, approve the protected jobs as the configured reviewer, and verify the immutable GitHub Release plus all eight NuGet packages before restoring the freeze.

## Boundaries & Constraints

**Always:** Preserve exact-current-`main` push CI selection, AD-13/AD-15 handoffs, the pinned Builds execution SHA, the eight-package inventory, pack-once candidate, protected `production` environment, exact-SHA immutable GitHub Release, and post-publication byte/signature verification. Treat `require-publication-authority: false` only as replacement of the one-use issue-comment authority with protected-environment approval. Use normal reviewer approval because the environment reports `can_admins_bypass=false`. Restore `HEXALITH_RELEASE_PUBLISH_ENABLED=false` after the attempt.

**Never:** Direct-push packages, weaken the freeze/source/staleness/package/evidence gates, forge approval or handoff data, claim an unavailable admin bypass, combine policy preauthorization and workflow activation in one push, rewrite existing commits/tags, publish anything except semantic-release `4.5.1` from the final exact `main` SHA, or leave the repository publication switch enabled.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Delayed activation | Future `release.yml` blob is not in the active policy | Policy-only push authorizes the exact future closure; a later push activates the workflow | Stop if the first push or committed-object validation rejects either closure |
| Authorized release | Final exact `main` is green, switch is `true`, protected reviewer approves | Semantic Release publishes immutable `v4.5.1` and eight package/symbol pairs | Stop before retry on source drift, failed gate, missing artifact, or partial publication |
| Approval request | GitHub admin bypass is disabled and `jpiquot` is the configured reviewer | Submit the supported pending-deployment approval with an audit comment | Do not misrepresent reviewer approval as admin bypass |
| Completion | Release or evidence reaches any terminal result | Restore the repository switch to `false` and reconcile GitHub/NuGet state | Report every absent or inconsistent package as an incident, not success |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- reusable release call; add the explicit authority opt-out without changing source, environment, package, or evidence gates.
- `eng/dependency-graph-policy.json` -- active delayed-activation registry; preauthorize the exact future Release evaluator closure before the workflow lands.
- `eng/dependency_graph.py` and `eng/workflow_source_closure.py` -- generate and validate closure evidence; reuse instead of hand-editing digests.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pin the explicit opt-out and retained protected-environment safeguards.
- `tools/release-packages.json` and `eng/release-package-inventory.json` -- authoritative eight-package publication boundary; do not change the inventory.
- `.github/workflows/release-evidence.yml` -- existing independent NuGet download, repository-signature, normalized-byte, symbol, SBOM, and manifest verification.

## Tasks & Acceptance

**Execution:**
- [ ] `eng/dependency-graph-policy.json` -- generate and validate a release authorization row for the exact future caller blob and pinned reusable/action closure, then commit and push it separately so delayed activation is real.
- [ ] `.github/workflows/release.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pass `require-publication-authority: false` with reservation inputs empty and add focused governance coverage; validate and land only after the policy push is green.
- [ ] GitHub `main` workflows -- push the existing three local fix commits with the policy landing, then the activation commit; require exact-final-SHA CI, Quality, Commitlint, CodeQL, and governance success.
- [ ] GitHub release controls -- set the publication variable exactly `true`, dispatch Release from final `main`, approve pending `production` deployments as `jpiquot`, wait for Release and Release Evidence, then restore the variable to `false` on every terminal path.
- [ ] GitHub Releases and NuGet.org -- verify `v4.5.1` resolves to the dispatched SHA and all eight inventory IDs expose downloadable `4.5.1` packages with the workflow's signature/content evidence successful.

**Acceptance Criteria:**
- Given the active evaluator policy and final exact `main` SHA, when push workflows complete, then all required checks are green and the Release caller closure is authorized by the prior policy revision.
- Given the user's explicit authority opt-out and the configured protected reviewer approval, when Release completes, then immutable `v4.5.1`, all eight NuGet `4.5.1` packages, and Release Evidence succeed for the same SHA.
- Given success, failure, cancellation, or partial publication, when the attempt becomes terminal, then the publication variable is exactly `false` and the reported package state matches GitHub and NuGet.org.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The authorization bypass is narrow: the reusable workflow's `require-publication-authority=false` disables only its issue-comment reservation gate. GitHub's protected environment remains the human authorization boundary. The repository variable is a separate deny-only switch and must be enabled only for the bounded attempt.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_disposition.py -v` -- expected: governance and release-contract suites pass.
- focused `CiGovernanceTests` assembly invocation under Microsoft.Testing.Platform -- expected: the release authority and protected-environment assertions pass.
- `actionlint .github/workflows/*.yml` and `git diff --check` -- expected: workflow syntax and repository formatting pass.
- `gh run watch <run-id> --exit-status` for exact-source push, Release, and Release Evidence -- expected: every required run succeeds.
- NuGet V3 flat-container and GitHub Release API checks for the eight manifest IDs at `4.5.1` -- expected: all artifacts exist and the immutable tag resolves to the dispatched SHA.
