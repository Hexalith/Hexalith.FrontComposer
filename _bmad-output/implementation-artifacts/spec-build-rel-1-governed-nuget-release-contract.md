---
title: 'BUILD-REL-1: Opt-in governed NuGet release contract for Hexalith.Builds'
type: 'feature'
created: '2026-08-05'
status: 'done'
review_loop_iteration: 0
baseline_commit: '824d7ef100455423aabbcd399c8364074000b2e0'
context:
  - '{project-root}/_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md'
  - '{project-root}/eng/dependency_handoff.py'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Shared `domain-release.yml` / `domain-ci.yml` cannot host FrontComposer's pre-publish FR24 gate, common freeze, or GOV-1 exact-candidate handoffs. Issue 17 closed without the GOV-1 amendment; no accepted 40-hex revision exists.

**Approach:** In the Hexalith.Builds repo (not FrontComposer), extend reusable CI/Release with an opt-in governed mode: signing + attestation candidate phase, skip-not-fail `HEXALITH_RELEASE_PUBLISH_ENABLED` gate, and GOV-1 exact-candidate / evaluator handoff I/O — default-off so existing callers stay unchanged.

## Boundaries & Constraints

**Always:**
- Implement only in `/home/administrator/projects/hexalith/builds` on a feature branch from current `origin/main` (`824d7ef100455423aabbcd399c8364074000b2e0` at planning time — re-resolve before coding).
- Opt-in / backward-compatible: unset governed inputs → today's behavior.
- Preserve `submodules: false` + root-only init (`Github/initialize-build`).
- Signing secrets scoped to governed steps only; never print or persist.
- Minimum permissions: grant `id-token: write` / `attestations: write` only when governed mode is on.
- Freeze compare is exact shell string `"true"` (case-sensitive, untrimmed); missing/malformed → skip Semantic Release and conclude green.
- Governed Release consumes only `release-commit` for checkout/prepare/seal/verify/classify/publish; event head authenticates the CI run only.
- Reusable workflow/action refs used in governed paths are literal 40-hex; local composites load from the reusable-workflow commit.
- Record the accepted immutable revision (workflow + composite blob SHA-256) for FrontComposer G2 / GOV-1 gate update after merge.

**Ask First:**
- Fast-forward/rebase local Builds `main` (currently behind origin) before branching.
- Reopen Hexalith.Builds issue 17 vs file a successor for the GOV-1 amendment.
- Exact public input/secret/output names if they must differ from the G2 request's illustrative `governed-release` / `release-environment` / `nuget-signing-timestamper` shape.
- Whether governed CI must also retarget `@main` composite `uses:` to 40-hex in this PR (GOV-1 closure requires it for governed paths).

**Never:**
- Edit FrontComposer `references/Hexalith.Builds` or any FrontComposer caller wiring in this story.
- Weaken publication identity / `builds-execution-sha` ↔ `job.workflow_sha` checks already present.
- Require attestation for non-governed callers.
- Ship a permanent FrontComposer-owned release fork or re-enable G1 as fallback.
- Recursive submodule init.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Legacy caller | Governed inputs unset | Identical to pre-change release/CI | N/A |
| Freeze off | `HEXALITH_RELEASE_PUBLISH_ENABLED` ≠ exact `true` | Skip Semantic Release; job green with explicit notice | No red run |
| Freeze on + governed | Var `true`, `governed-release: true`, signing secrets present | Candidate packages → attest → env/output for caller `prepareCmd` → publish authorized artifacts only | Fail closed on attest/sign/classify failure; surface partial evidence |
| Missing signing secrets in governed mode | Governed on, secrets absent | Do not publish | Fail with explicit missing-secret error |
| GOV-1 CI handoff | Governed CI inputs (candidate SHA, policy coords, evaluator digest) | Validate `job.workflow_ref`/`job.workflow_sha`; emit provenance for caller `dependency-release-handoff` (`hexalith.dependency-release-handoff.v1`) | Reject mismatched workflow identity / digest |
| GOV-1 Release handoff | CI-handoff candidate, run id/attempt, policy, expected Release evaluator digest | All phases use `release-commit`; caller uploads `release-verification-handoff` (`hexalith.release-verification-handoff.v1`) under `if: always()` | Null/empty closed fields for unavailable release data — never omit artifact |
| Vars resolution | Caller repo/org `vars` for freeze | Prefer documented caller `vars` resolution; if platform cannot, require `publish-enabled` boolean input default `false` | Document verified behavior in workflow docs |

</frozen-after-approval>

## Code Map

**Implementation repo:** `/home/administrator/projects/hexalith/builds` @ `origin/main` `824d7ef100455423aabbcd399c8364074000b2e0` (local `main` was `adbea5da…`, behind 9 — sync before coding).

- `/home/administrator/projects/hexalith/builds/.github/workflows/domain-release.yml` — primary seam: `workflow_call` inputs L5–92, secrets L93–102, permissions L103–107, job `release` + `environment: ${{ inputs.environment-name }}` L110–113, checkout `submodules:false` L115–119, Builds SHA gate L120–139, nested composites from `.hexalith/builds-execution` L147+, `npx semantic-release` ~L302. **Missing:** governed flag, signing secrets, RFC3161 input, freeze gate, attestation phase, GOV-1 candidate/handoff I/O, `id-token`/`attestations`.
- `/home/administrator/projects/hexalith/builds/.github/workflows/domain-ci.yml` — CI reusable; permissions `contents:read` only; **no outputs**; composites still `Hexalith/Hexalith.Builds/Github/{initialize-build,dapr-init}@main` (L135+, L209+, …). **Missing:** governed exact-candidate/policy/evaluator inputs, `workflow_ref`/`workflow_sha` capture/validate, provenance outputs for handoff.
- `/home/administrator/projects/hexalith/builds/Github/initialize-build/` — root-only submodule init (reuse; do not recurse).
- `/home/administrator/projects/hexalith/builds/Github/dapr-init/` — Dapr setup (reuse).
- `/home/administrator/projects/hexalith/builds/Github/publish-containers/` — publication preflight / identity freeze helpers; tests under `Github/publish-containers/tests/` assert current release contract (extend, do not drop).
- `/home/administrator/projects/hexalith/builds/.github/workflows/domain-release.md`, `domain-ci.md`, `ci-cd-standards.md`, `README.md` — caller docs to update for governed mode + freeze + GOV-1 inputs.
- `/home/administrator/projects/hexalith/builds/changes-plan.md` — CHG-B12 notes deferred attestations/OIDC (supersede with BUILD-REL-1).

**Caller contract authority (read-only; do not edit in this story):**
- `/home/administrator/projects/hexalith/frontcomposer/_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — full required change + freeze + GOV-1 amendment.
- `/home/administrator/projects/hexalith/frontcomposer/eng/dependency_handoff.py` L21–22 — `CI_HANDOFF_SCHEMA` / `RELEASE_HANDOFF_SCHEMA`.
- `/home/administrator/projects/hexalith/frontcomposer/eng/release_evidence.py` — requires `attestation_status=attested` + bundle (or sealed `approved-unsupported`).
- `/home/administrator/projects/hexalith/frontcomposer/.github/workflows/release.yml` — current pin `@a5316653…`; FrontComposer wiring is a **follow-up** after accepted revision.

## Tasks & Acceptance

**Execution:**
- [x] `/home/administrator/projects/hexalith/builds` — sync to `origin/main`, create feature branch — clean base matching FrontComposer pin tip
- [x] `/home/administrator/projects/hexalith/builds/.github/workflows/domain-release.yml` — add opt-in governed inputs/secrets; conditional `id-token`/`attestations`; freeze gate before Semantic Release; candidate pack/attest/handoff env for caller prepare; keep legacy path identical when off
- [x] `/home/administrator/projects/hexalith/builds/.github/workflows/domain-ci.yml` — add opt-in governed inputs; validate `job.workflow_ref`/`job.workflow_sha`; emit provenance outputs needed for `hexalith.dependency-release-handoff.v1`; pin governed composite `uses:` to 40-hex if Ask First confirms in-scope
- [x] `/home/administrator/projects/hexalith/builds/Github/**` — any new/adjusted composite for governed candidate/attest/closure evaluation; load only from reusable-workflow commit
- [x] `/home/administrator/projects/hexalith/builds/Github/publish-containers/tests/` (+ new tests as needed) — cover freeze skip-not-fail, governed-off parity, governed-on missing-secret fail-closed, SHA/workflow identity gates
- [x] `/home/administrator/projects/hexalith/builds/.github/workflows/domain-release.md`, `domain-ci.md`, `ci-cd-standards.md` — document governed mode, freeze var hazard (repo vs org shadowing), GOV-1 handoff inputs/outputs
- [x] GitHub Hexalith.Builds issue 17 (or successor) — **deferred this run** (Ask First: code only; no issue reopen/filing). After merge, Release Owner records accepted 40-hex + blob SHA-256 into FrontComposer G2 request separately.

**Acceptance Criteria:**
- Given a caller with all new inputs unset, when `domain-ci.yml` / `domain-release.yml` run, then behavior matches pre-BUILD-REL-1 contracts (including `builds-execution-sha` gate and `submodules: false`).
- Given `HEXALITH_RELEASE_PUBLISH_ENABLED` is missing or not exactly `true`, when the release job reaches the publish gate, then Semantic Release is skipped, the notice is explicit, and the job concludes success.
- Given governed mode is enabled with signing secrets and freeze `true`, when candidate packages are produced, then `actions/attest-build-provenance` runs over those exact packages and the attestation-bundle path is available to the caller's semantic-release prepare lifecycle before any NuGet publish side effect.
- Given governed CI inputs for candidate SHA, policy coordinates, and expected evaluator digest, when CI completes, then workflow identity is validated and provenance sufficient for `hexalith.dependency-release-handoff.v1` is produced for the caller.
- Given governed Release inputs for the authenticated CI-handoff candidate and policy/evaluator coordinates, when Release runs, then every checkout/prepare/seal/verify/classify/publish operation uses `release-commit` only, and durable verification handoff data for `hexalith.release-verification-handoff.v1` can be uploaded by the caller under `if: always()`.
- Given governed mode permissions, when the job runs, then `id-token: write` and `attestations: write` are present only on the governed path; signing secrets never appear in logs or artifacts.
- Given the PR merges, when Release Owner records the revision, then an immutable 40-hex commit plus `domain-ci.yml` / `domain-release.yml` / composite blob SHA-256 values are available for FrontComposer G2 / GOV-1 acceptance.

## Spec Change Log

## Design Notes

### Resolved Ask First (2026-08-05)
- Sync: fast-forward Builds `main` to `origin/main` and branch from it.
- Issue tracking: defer reopen/successor filing; code changes only this run.
- Public names: G2 illustrative (`governed-release`, `nuget-signing-timestamper`, signing secret names as in G2); reuse existing `environment-name` for the protected environment (no second alias required).
- CI composites: pin `Github/*@main` → literal 40-hex in this PR (GOV-1 closure).

- Prefer extending existing `environment-name` rather than inventing a second environment input unless Ask First renames to match G2's `release-environment` alias.
- Freeze must use a shell step (`[[ "$VAR" == "true" ]]`), not GitHub-expression `==` (case-insensitive).
- Builds provides governed **execution context** (secrets, permissions, environment, candidate phase, attestation, provenance). Callers still own evidence logic (`prepareCmd` / handoff JSON). Do not embed FrontComposer `release_evidence.py` in Builds.
- Verify during implementation whether reusable workflows resolve `vars` from the caller; if not, add required `publish-enabled` boolean input default `false` as documented fallback.

## Verification

**Commands:**
- `git -C /home/administrator/projects/hexalith/builds status -sb && git -C /home/administrator/projects/hexalith/builds rev-parse HEAD` — expected: feature branch based on up-to-date `origin/main`
- `python -m pytest /home/administrator/projects/hexalith/builds/Github/publish-containers/tests -q` — expected: all pass, including new freeze/governed cases
- `actionlint` (or repo-equivalent) on edited workflows — expected: clean
- Manual YAML review: governed-off path diff vs `824d7ef` baseline shows no behavior change for legacy inputs

**Manual checks:**
- Issue filing deferred this run — skip issue-body check; Release Owner handles later
- Confirm no FrontComposer submodule or caller workflow files changed in the Builds PR

## Suggested Review Order

**Governed release job**

- Opt-in input and dual-job split so legacy callers never request attestation permissions
  [`domain-release.yml:95`](../../../builds/.github/workflows/domain-release.yml#L95)

- Governed job permissions: `id-token` / `attestations` only on this path
  [`domain-release.yml:471`](../../../builds/.github/workflows/domain-release.yml#L471)

- Freeze gate first: exact `"true"` compare; frozen skips build/candidate/attest/publish green
  [`domain-release.yml:489`](../../../builds/.github/workflows/domain-release.yml#L489)

- Candidate pack/sign, then attest before any publish side effect
  [`domain-release.yml:813`](../../../builds/.github/workflows/domain-release.yml#L813)

- Attest exact candidate packages
  [`domain-release.yml:947`](../../../builds/.github/workflows/domain-release.yml#L947)

- Semantic Release gated on freeze **and** `release-required` (no unattested publish)
  [`domain-release.yml:965`](../../../builds/.github/workflows/domain-release.yml#L965)

**Governed CI provenance**

- Opt-in governed CI inputs and caller-facing provenance outputs
  [`domain-ci.yml:121`](../../../builds/.github/workflows/domain-ci.yml#L121)

- Contract validates workflow identity and exact candidate checkout
  [`domain-ci.yml:221`](../../../builds/.github/workflows/domain-ci.yml#L221)

- Closure evaluation via local composite from approved Builds commit
  [`domain-ci.yml:296`](../../../builds/.github/workflows/domain-ci.yml#L296)

**Closure evaluator**

- Fail closed when computed closure digest ≠ expected evaluator digest
  [`governed_provenance.py:497`](../../../builds/Github/governed-provenance/governed_provenance.py#L497)

- Helpers count toward source ceiling; nested composite bytes hashed
  [`governed_provenance.py:248`](../../../builds/Github/governed-provenance/governed_provenance.py#L248)

**Tests**

- Executed freeze, candidate, digest, symlink, and identity coverage
  [`test_governed_release_workflow.py:1`](../../../builds/Github/publish-containers/tests/test_governed_release_workflow.py#L1)
