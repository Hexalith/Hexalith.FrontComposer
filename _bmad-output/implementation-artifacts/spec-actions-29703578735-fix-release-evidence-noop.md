---
title: 'Keep frozen releases green in the evidence workflow'
type: 'bugfix'
created: '2026-07-19'
status: 'in-review'
baseline_commit: 'b0254994e279a21d0496d6b3286d6524eebb14b4'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The latest FrontComposer Release run `29703573330` correctly skipped publication because the REL-4 freeze is still active, but the dependent Release Evidence run `29703578735` failed while resolving the release tag. Its no-publication path invokes `gh release list --json tagName,targetCommitish`; `targetCommitish` is not a supported `gh release list` JSON field, so the command exits non-zero and turns an expected frozen no-op into a failed CI/CD result.

**Approach:** Replace the unsupported CLI field query with a supported GitHub Releases API query using the API field `target_commitish`. Preserve the fail-closed orphaned-release check: an API probe failure must fail, an orphaned release must create a typed incident and fail, and a confirmed absence of publication must complete green.

## Boundaries & Constraints

**Always:** Keep REL-4 publication frozen by default; keep Release Evidence read-only and triggered for every Release conclusion; preserve tag-based verification for real publication, deleted-tag partial-publication detection, typed evidence artifacts, and the exact head SHA comparison.

**Ask First:** Any request to enable `HEXALITH_RELEASE_PUBLISH_ENABLED`, remove or weaken the freeze, change the reusable release workflow, alter signing/attestation/approval behavior, or modify a submodule requires Release Owner approval and a separate decision.

**Never:** Do not make a frozen run publish; do not suppress the API error with `|| true`, `continue-on-error`, or an empty fallback; do not use unsupported `gh release list` JSON fields; do not change `references/Hexalith.EventStore` or other submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|-----------------------------|----------------|
| FROZEN_NO_TAG | Upstream Release conclusion is skipped/failed and no tag or release targets the head | Evidence run records no publication side effect and exits green | Preserve the no-op summary and upload metadata |
| API_FAILURE | No tag resolves but the GitHub Releases API probe fails | Do not claim no publication; fail the evidence job | Emit an actionable error and retain forensic metadata |
| ORPHANED_RELEASE | No tag resolves but a GitHub Release targets the head | Create `partial-publish-incident.json` and fail | Require owner-led reconciliation |
| PUBLISHED_TAG | A tag resolves to the Release head or its release commit parent | Download and independently verify the exact published bytes | Fail on missing, divergent, unsigned, or unauthorized artifacts |

</frozen-after-approval>

## Code Map

- `.github/workflows/release-evidence.yml` -- independent post-publication verifier; its tag-resolution no-op branch currently uses the unsupported field.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- governance assertions for Release Evidence trigger, read-only posture, no-op behavior, and forbidden publish paths.
- `eng/release_evidence.py` -- typed partial-publication incident writer used by the workflow; unchanged by this fix.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release-evidence.yml` -- query release targets through `gh api` with `target_commitish`, explicitly fail when the probe cannot complete, and retain the incident path -- make the intended frozen no-op executable.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pin the supported API query, reject the obsolete `targetCommitish` field, and require the probe-failure guard -- prevent regression.

**Acceptance Criteria:**
- Given a Release run is frozen and no publication side effect exists, when Release Evidence resolves its tag, then the evidence workflow completes successfully with a no-publication summary.
- Given no tag resolves and the Releases API cannot be queried, when the resolver runs, then the workflow fails rather than asserting that no publication occurred.
- Given a release targets the head without a resolving tag, when the resolver runs, then it writes the typed partial-publication incident and fails.
- Given a real release tag resolves, when the workflow runs, then the existing independent byte/signature/manifest verification path is unchanged.

## Design Notes

`gh release list --json` exposes only the CLI's supported fields; GitHub's REST release object exposes the needed `target_commitish` property. The API query is kept inside an explicit `if ! ...; then` guard so `set -e` cannot turn a successful zero-result query into a failure, while a real API outage remains fail-closed.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~CiGovernanceTests.ReleaseEvidenceWorkflow` with `DiffEngine_Disabled=true` -- expected: pass.
- `gh run view 29703578735 --repo Hexalith/Hexalith.FrontComposer --json conclusion,jobs` -- expected: historical failure identifies only the obsolete JSON field; the next frozen run is green.
