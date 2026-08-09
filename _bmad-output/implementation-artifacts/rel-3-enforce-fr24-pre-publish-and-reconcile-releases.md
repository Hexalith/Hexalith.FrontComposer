---
title: 'REL-3 residual — unsigned FR24 fail-closed review gaps'
type: 'bugfix'
created: '2026-07-15'
updated: '2026-08-09'
status: 'done'
review_loop_iteration: 1
baseline_commit: '5c284c89d37dfc3d39593962631e376bd4c5e033'
story: REL-3
history_archive: _bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.history.md
context:
  - _bmad-output/implementation-artifacts/spec-remove-author-signing-requirement.md
  - _bmad-output/project-context.md
---

<frozen-after-approval reason="human-owned residual intent after 2026-08-09 Option 1 — do not modify unless human renegotiates">

## Intent

**Problem:** Under the unsigned-author / NuGet.org repository-signature FR24 model, publication
verify is weaker than independent evidence, AD-15 can soft-defer when published without a sealed
manifest, orchestrator docs/wiring are stale, and disposition / retroactive-auth gates are
source-text pins only.

**Approach:** Close only those fail-closed and verification gaps; do not restore author signing or
reopen REL-5 / GOV-1 closure.

## Boundaries & Constraints

**Always:** Pack-once sealed unsigned `nupkgs/*`; sealed manifest + readiness before push;
evidence GitHub checksum + NuGet.org repository-signature + content equality (exclude only
`.signature.p7s`); REL-4/Builds freeze; fail-closed partial-publish incidents.

**Ask First:** Real release/dispatch; signer-policy or secret changes; git push/PR/submodule work;
reintroducing author signing; changing classify-at-publish vs sealed-readiness-only.

**Never:** Author-sign / `nupkgs-signed` / trust-store mutation; weaken GitHub checksums; treat
repository signature as author signature; claim REL-AI-1 closed without REL-5 evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Publish verify | `immutable=false` or empty assets | `verify-publication` fails | Fail before AD-15 success |
| AD-15 published | `published=true`, missing manifest | Job exit 1 | No exit-2 success |
| Disposition | Completed `release / release` | `governed-attempt=true` proven | Misclassify fails test |
| Retroactive auth | Downloaded `publish_authorized=false` | Verifier fails closed | Incident recorded |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` `verify-publication` ~353-356 — add `immutable==true`, non-empty assets, `--require-immutable` (parity with `release-evidence.yml` ~352-388).
- `.github/workflows/release.yml` `emit-verification-handoff` ~445-518 — drop `|| true` soft path; fill `assets.json`; hard-fail when `published=true` without sealed-manifest coords (not exit 2).
- `eng/dependency_handoff.py` ~326-335, ~650-663 — sealed handoff rejects null manifest; do not soft-succeed deferred when published.
- `eng/release_prepublish.py` docstring 1-31 vs `.releaserc.json` 20-22 and `release.yml` prepare-candidate (`prepare`/`bundle`); `cmd_verify_prepared` 650-664 / `cmd_publish` 718-738 — pin classify contract; KEEP `_validate_unsigned_candidates`.
- `CiGovernanceTests.ReleaseEvidenceWorkflow_IndependentlyVerifiesPublishedArtifacts` / `ReleaseModelGovernanceTests.VerificationWorkflow_DownloadedEvidence_CannotAuthorizeReleaseRetroactively` — replace source-text-only pins; reuse `tests/ci-governance/stage_release_state.py` patterns.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- make `verify-publication` require immutable + non-empty assets + `--require-immutable`.
- [x] `.github/workflows/release.yml` -- fail-close AD-15 when `published=true` without candidate/manifest; populate `assets.json`.
- [x] `eng/release_prepublish.py` -- fix docstring to prepare/bundle + restore/verify-prepared/publish (unsigned).
- [x] `eng/release_prepublish.py` + tests -- pin sealed-readiness-only (default) or Ask First before adding re-classify.
- [x] Governance tests -- fixture/runtime proof for disposition + `publish_authorized` gate (I/O matrix).

**Acceptance Criteria:**
- Given a non-immutable or empty-asset release, when `verify-publication` runs, then it fails closed like independent evidence.
- Given `published=true` without sealed manifest coords, when AD-15 runs, then the job exits 1 (no deferred success).
- Given orchestrator docs and workflow/releaserc wiring, when inspected, then they match and show no author-sign path.
- Given unauthorized downloaded readiness or misclassified disposition, when governance proof runs, then it fails without relying only on substring presence.

## Spec Change Log

- 2026-08-09 (Option 1): Align to unsigned FR24; archive full epic to `*.history.md`. KEEP unsigned pack-once, sealed manifest, evidence repo-sig/content equality, Builds freeze, no author-sign.
- 2026-08-09 ([S] split): Active surface = T8 residuals only; deferred historical epic maintenance, REL-5/REL-AI-1, GOV-1/BUILD-REL-1, T7 ledger URLs.

## Design Notes

Default pin sealed-readiness-only through publish in the same run. Re-classify at publish changes the authorization clock — Ask First before enabling.

## Verification

**Commands:**
- Release-build Shell.Tests; run filters for `ReleaseEvidenceWorkflow_*`, `VerificationWorkflow_*`, new disposition/auth fixtures — green, 0 warnings.
- `python3 -m unittest` touched `tests/eng/test_release_*` — green.
- `actionlint` on changed workflows if available; `git diff --check` — clean.

**Manual checks:**
- Parity diff of `verify-publication` vs evidence immutable step; AD-15 never soft-succeeds when published without manifest.

## Suggested Review Order

**Publication verify parity**

- Fail closed on mutable or empty-asset GitHub Releases before AD-15.
  [`release.yml:354`](../../.github/workflows/release.yml#L354)

- Shared contract rejects non-immutable releases and empty asset lists.
  [`release_contract.py:291`](../../eng/release_contract.py#L291)

**AD-15 fail-closed**

- Treat release-job success as a publication attempt so AD-15 cannot soft-defer.
  [`release.yml:451`](../../.github/workflows/release.yml#L451)

- Materialize confined candidate-bound asset digests for the sealed handoff.
  [`dependency_handoff.py:407`](../../eng/dependency_handoff.py#L407)

**Evidence disposition / auth**

- Extracted classifier drives governed-attempt outputs for the evidence workflow.
  [`release_disposition.py:29`](../../eng/release_disposition.py#L29)

- Runtime gate for downloaded sealed readiness including fallback-approved.
  [`release_disposition.py:118`](../../eng/release_disposition.py#L118)

- Evidence workflow invokes classify and require-published-readiness helpers.
  [`release-evidence.yml:133`](../../.github/workflows/release-evidence.yml#L133)

**Orchestrator contract**

- Docstring pins prepare/bundle vs restore/verify-prepared/publish and sealed-readiness-only.
  [`release_prepublish.py:1`](../../eng/release_prepublish.py#L1)

**Tests**

- Disposition topology, fallback-approved, and github-output proofs.
  [`test_release_disposition.py:1`](../../tests/eng/test_release_disposition.py#L1)

- Materialize path/digest negatives and AD-15 hard-fail after publication.
  [`test_dependency_handoff.py:1`](../../tests/eng/test_dependency_handoff.py#L1)
