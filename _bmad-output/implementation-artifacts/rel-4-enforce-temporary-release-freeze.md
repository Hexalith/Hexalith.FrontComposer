---
title: 'REL-4: Pin Builds-hosted release freeze after caller supersession'
type: 'refactor'
created: '2026-08-09'
status: 'done'
baseline_commit: 'b6b7f342d14521eb49e48cb7e853fb436650a591'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/project-docs/deployment-guide.md'
closureDecision: '1B supersession; 2 partial-waive AC7 caller removal; 3 Builds freeze pin required; 4 deployment-guide rewrite (proposal/sprint-status deferred)'
historicalEvidenceRun: 'https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After exact-source dispatch replaced auto `workflow_run` publishing, REL-4’s caller `freeze-guard` is gone and FrontComposer does not pin the Builds freeze that now owns default-frozen publication—so a Builds pin bump can drop the freeze while FC stays green, and the operator runbook still reads like the old world.

**Approach:** Keep run `29703682203` as historical caller-freeze evidence; treat the pinned Builds `domain-release.yml` exact `HEXALITH_RELEASE_PUBLISH_ENABLED` gate as the standing freeze; add a FrontComposer governance pin on that pinned workflow; rewrite the deployment-guide freeze row for operator dispatch. Publication stays unauthorized.

## Boundaries & Constraints

**Always:**
- Historical evidence URL stays cited: https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203
- Standing freeze = exact POSIX bash match of `HEXALITH_RELEASE_PUBLISH_ENABLED` to `true` in Builds `domain-release.yml` at the exact SHA FrontComposer pins (`uses:` lockstep with `BUILDS_EXECUTION_SHA` / `builds-execution-sha`)
- Caller stays `workflow_dispatch`-only; do not reintroduce caller `freeze-guard` or caller binding of the freeze variable
- Only `release.yml` may `uses:` `domain-release.yml`; no executable `npx semantic-release` / `dotnet nuget push` in other repo workflows
- Partial AC7 waive covers **caller** gate removal only; do not set the variable to `true`
- Docs for this story: freeze **relocated** to Builds, not abandoned

**Ask First:**
- Changing the pinned Builds execution SHA
- Restoring auto-publish (`workflow_run` / push release)
- Authorizing `HEXALITH_RELEASE_PUBLISH_ENABLED=true` or a real release

**Never:**
- Restore caller `freeze-guard` / auto-release shape
- Edit Hexalith.Builds submodule contents (read/pin only)
- Own FR24 pack/sign/evidence or claim REL-AI-1 closed
- Expand into correction-proposal / sprint-status comment rewrites (deferred-work)

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Default frozen | Variable absent / not exact `true` on Builds path | `publish-enabled=false`; Semantic Release skipped; green freeze notice | N/A |
| Exact enable | Variable exactly `true` | Builds may publish under existing production gates | Not authorized by this story |
| Malformed enable | `True` / `1` / `yes` / whitespace / empty | Remains frozen (bash exact match) | N/A |
| Pin regression | Pinned Builds SHA drops freeze contract | New FC governance fact fails | Fail CI |
| Alternate publish path | Non-`release.yml` gains publish `uses:`/`run:` | Existing no-publish-path fact fails | Fail CI |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` — `workflow_dispatch` (L5–6); pin `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` (L17, L307 `uses:`, L315 `builds-execution-sha`); no freeze tokens (read-only unless Ask First pin change).
- `references/Hexalith.Builds/.github/workflows/domain-release.yml` @ pin — **Resolve release publication freeze** / `id: publish-gate` on non-governed `release` (~L370–385) and `governed-release` (~L489–504); env binds `vars.HEXALITH_RELEASE_PUBLISH_ENABLED`; bash `= "true"`; Semantic Release `if` requires `publish-enabled == 'true'` (~L435 / ~L969).
- `tests/.../Governance/CiGovernanceTests.cs` — reuse SHA regex in `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` (L585+); keep `RetiresFreezeOnlyThroughOperatorProductionMigration` (L1222+), `RequiresManualExactSourceCiBeforeProduction` (L1181+), `HaveNoPublishPathOutsideGatedReleaseWorkflow` (L1238+); **add** Builds freeze-bytes pin via `git show {sha}:.github/workflows/domain-release.yml` (same idea as `tests/ci-governance/stage_release_state.py` L234–245).
- `tests/.../Governance/ReleaseModelGovernanceTests.cs` — keep ApprovalMatrix free of the freeze variable (L271–280); do not add it there.
- `_bmad-output/project-docs/deployment-guide.md` — `## Required external configuration` (~L96); rewrite `HEXALITH_RELEASE_PUBLISH_ENABLED` row (~L104) for Builds-hosted freeze under operator dispatch.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- add fact: resolve pinned Builds `domain-release.yml` bytes from `release.yml` `uses` SHA; pin freeze step name, env binding, exact bash `= "true"` match, and Semantic Release `publish-enabled == 'true'` conditions for both paths present in that file -- decision 3 verification gate
- [x] `_bmad-output/project-docs/deployment-guide.md` -- rewrite freeze variable row for Builds publisher + exact-source dispatch; keep name, exact `true`, Owner custody, org-vs-repo shadowing, `NUGET_API_KEY` residual; stop implying live caller `freeze-guard` / post-CI frozen Release as current procedure -- decision 4 (guide only)
- [x] `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md` -- keep supersession + historical record coherent with landed pin/docs when closing -- story package honesty

**Acceptance Criteria:**
- Given the pinned Builds SHA in `release.yml`, when Governance tests run, then FC fails if that SHA’s `domain-release.yml` lacks the standing freeze contract.
- Given current caller `release.yml`, when Governance tests run, then caller still has no `freeze-guard` / no caller freeze-variable binding, stays `workflow_dispatch`-only, and remains the only `domain-release.yml` publish path.
- Given the deployment guide freeze row, when an operator reads it, then it describes Builds-hosted freeze under operator dispatch—not a live caller `freeze-guard` after CI.
- Given story closure, when evidence is recorded, then run `29703682203` remains the historical caller-freeze proof and REL-4 still does not authorize publication.

## Spec Change Log

- 2026-08-09 (review loop 1 → re-plan): Supersession draft from intent_gap decisions `1B / 2 partial-waive / 3 required / 4 rewrite`. KEEP: historical run URL; exact-match semantics; no alternate publish path; no caller re-bind; publication unauthorized; Builds as common mechanism.
- 2026-08-09 (token split [S]): Narrowed to Builds freeze pin + deployment-guide freeze row + story coherence. Deferred: correction-proposal annotate, sprint-status comment align, deployment-guide SHA-citation hygiene (see `deferred-work.md`). Avoids: shipping an oversized multi-doc cleanup under one implementer context.

## Design Notes

Load Builds bytes with `git show {pin}:.github/workflows/domain-release.yml` so the tested artifact is the `uses` SHA, not a divergent submodule working tree. One new CiGovernance fact is enough; do not restore caller freeze pins.

## Historical Implementation Record (2026-07-18, superseded)

Caller `freeze-guard` landed; frozen run https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203. Superseded by exact-source dispatch; standing freeze relocated to pinned Builds publisher.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release` -- expected: 0/0
- Direct xUnit v3 Governance filter for the new Builds freeze pin + `RetiresFreeze…` + `HaveNoPublishPath…` (`DiffEngine_Disabled=true`) -- expected: green
- `rg -n "freeze-guard|HEXALITH_RELEASE_PUBLISH_ENABLED" .github/workflows/release.yml` -- expected: no matches
- Manual: deployment-guide freeze row reads Builds-hosted / operator-dispatch

## Dev Agent Record (2026-08-09 supersession)

Landed Builds freeze pin fact `ReleaseWorkflow_PinsBuildsHostedPublicationFreezeContract` (`git show` of `release.yml` `uses` SHA), rewrote deployment-guide `HEXALITH_RELEASE_PUBLISH_ENABLED` row for Builds-hosted freeze under operator dispatch, and did not restore caller `freeze-guard` or authorize publication. Historical evidence remains https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29703682203. Caller-era fact `ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault` remains superseded/absent.

**Review patches (2026-08-09):** strengthened pin to require co-located exact-true + `publish-enabled=false` + freeze notice per freeze step; every `Semantic Release` step gated; every executable `npx semantic-release` under `publish-enabled == 'true'`; clarified deployment-guide freeze sentence (frozen-path success/notice vs enable condition).

**Verification evidence:** Release `dotnet build` Shell.Tests 0/0; xUnit v3 filter PinsBuildsHosted + RetiresFreeze + HaveNoPublishPath + RequiresManualExactSource → 4/4 passed; `rg` shows no `freeze-guard` / `HEXALITH_RELEASE_PUBLISH_ENABLED` in `.github/workflows/release.yml`.

## Suggested Review Order

**Standing freeze pin**

- Entry: new fact loads pinned Builds bytes and pins fail-closed freeze contract.
  [`CiGovernanceTests.cs:1222`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1222)

- Co-located exact-true, `publish-enabled=false`, and freeze notice per gate body.
  [`CiGovernanceTests.cs:1255`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1255)

- Every Semantic Release step gated; every `npx semantic-release` under publish-enabled.
  [`CiGovernanceTests.cs:1267`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1267)

**Operator runbook**

- Freeze row rewritten for Builds-hosted exact match under operator dispatch.
  [`deployment-guide.md:104`](../project-docs/deployment-guide.md#L104)

**Caller retirement retained**

- Caller still must not bind freeze variable or host freeze-guard.
  [`CiGovernanceTests.cs:1302`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1302)

**Deferred tracking**

- Proposal/sprint-status/SHA hygiene split out of this closure.
  [`deferred-work.md`](deferred-work.md)
