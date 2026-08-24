---
title: 'Keep frozen releases green in the evidence workflow'
type: 'bugfix'
created: '2026-07-19'
status: 'done'
baseline_commit: 'b0254994e279a21d0496d6b3286d6524eebb14b4'
review_loop_iteration: 0
resolution: 'implemented-then-superseded'
implementation_commit: '550cb0602d506d9fd008a8c09f2cca6b328ec1e3'
superseded_by: '3ebbdce987b2d74340be66b26bc284aa59c9233e'
reconciled_by: '_bmad-output/implementation-artifacts/spec-reconcile-frozen-release-evidence-review.md'
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

## Reconciliation Resolution (2026-08-24)

**Resolution:** `implemented-then-superseded`.

Successor record: [Reconcile the superseded frozen release-evidence review](spec-reconcile-frozen-release-evidence-review.md).

### Exact historical implementation scope

The implementation is the exact one-commit range
`b0254994e279a21d0496d6b3286d6524eebb14b4..550cb0602d506d9fd008a8c09f2cca6b328ec1e3`.
Git confirms that `b0254994e279a21d0496d6b3286d6524eebb14b4` is the sole parent of
`550cb0602d506d9fd008a8c09f2cca6b328ec1e3`. Its complete eight-path
`git diff-tree --name-status` scope is:

- `M .github/workflows/release-evidence.yml`
- `A _bmad-output/implementation-artifacts/review-prompt-actions-29703578735-blind-hunter.md`
- `A _bmad-output/implementation-artifacts/review-prompt-actions-29703578735-edge-case-hunter.md`
- `A _bmad-output/implementation-artifacts/review-prompt-actions-29703578735-verification-gap.md`
- `A _bmad-output/implementation-artifacts/spec-actions-29703578735-fix-release-evidence-noop.md`
- `M references/Hexalith.EventStore`
- `M references/Hexalith.Memories`
- `M tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

The workflow and Governance-test paths contain the historical functional fix; the spec
and three review prompts contain its review record. The two gitlinks were also present in
that same historical commit and are therefore listed for scope honesty, but this closure
does not attribute them to the release-evidence mechanism or make any current submodule
change. No later repository change is included in this one-commit implementation scope.

### Historical operational proof and limits

Observed on 2026-08-24, post-fix run attempt 1 at
`https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29704283540`
concluded `success` at exact head
`550cb0602d506d9fd008a8c09f2cca6b328ec1e3`. Its
`verify-published-release` job (database ID `88238525952`) succeeded: `Resolve release
tag` and `Upload verification evidence artifact` completed successfully, while the five
published GitHub/NuGet download, manifest, signature, and ledger-verification steps were
skipped. This proves that the expected historical workflow branch completed green. It
does not independently prove that no external publication side effect existed.

That run did **not** exercise either the old Releases API-failure branch or the old
orphaned-release/partial-publication branch, so it supplies no operational proof for those
branches. On 2026-08-24, the Actions artifacts query returned `total_count: 1` and
identified artifact `release-verification-29704283540-1` as ID `8447402914` with
`expired: true`; the archived job-log request returned HTTP 410. Those expired bytes are
not treated as retained proof. The claim above is deliberately limited to the still
available run, head, job, and step metadata.

The historical Governance source added
`ReleaseEvidenceWorkflow_TagResolver_CoversNoOpAndPublicationMatrix`, including clean,
API-failure, orphaned-release, and published rows, so the old failure and orphan branches
were implemented and covered in source. Surviving CI evidence does not prove that matrix
ran or passed. Attempt 1 of CI run `29704185078` succeeded at the implementation SHA, but
its exact `ci.yml` selected Tier 1 projects and explicitly excluded `Shell.Tests`, where
that Governance matrix lived. Attempt 1 of Quality run `29704184914` failed at `Gate 2b:
Infrastructure governance and telemetry contracts`; later default-lane steps were
skipped. On 2026-08-24, the CI artifacts query returned one expired artifact (ID
`8447395908`), the Quality artifacts query returned four expired artifacts (IDs
`8447430106`, `8447397277`, `8447396391`, and `8447396299`), and both archived build-job
log requests returned HTTP 410. Therefore the old API-failure and orphan branches remain
classified as implemented historically but not operationally proven by surviving evidence.

### Supersession chain and current architecture

- `90c5dcb9af3ff4cf0c243c5af1a06295b09ca175` later expanded frozen-run handling into authenticated Release-run topology classification and a broader no-attempt publication probe while still using the REST `target_commitish` field.
- `3ebbdce987b2d74340be66b26bc284aa59c9233e` superseded that mechanism with the exact-source operator-dispatch release design and removed the old `target_commitish`/`targetCommitish`/`gh release list` resolver surface from both the workflow and its Governance tests.
- `fd1f5b624d5dfee8f0d17da349ad6868553c68a1` is the follow-on attribution anchor that hardened the authenticated disposition contract and introduced `eng/release_disposition.py`; it did not restore the obsolete query.

The current workflow authenticates the completed operator Release run and classifies its
topology through `eng/release_disposition.py`. A non-governed attempt records an explicit
non-publication disposition; a governed attempt authenticates the exact prepared candidate
and independently verifies its immutable GitHub Release, tag, durable assets, NuGet bytes,
and repository signatures. This is current architecture context, not a claim that the July
`target_commitish` mechanism remains an invariant or that the repository is currently
frozen.

Focused current checks on 2026-08-24 ran
`python3 -m unittest tests/eng/test_release_disposition.py tests/eng/test_release_contract.py`:
all 29 tests passed. This verifies the current classifier and immutable-release contract,
not the expired historical Governance execution.
