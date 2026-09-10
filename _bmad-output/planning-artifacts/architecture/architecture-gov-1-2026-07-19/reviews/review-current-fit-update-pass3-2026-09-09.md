# Reviewer Gate — current-technology / reality-check lens — pass 3 — 2026-09-09

**Subject:** `ARCHITECTURE-SPINE.md` SHA-256
`a0efa3e02e15cfc1a4bbd73068cdd55ab38342136345d29883bec8b5f2ff9c86`

**Repository baseline:** FrontComposer HEAD
`053b2008307d4e476c0d4329e6c47763c301d43e`; public GitHub.com repository.

## Verdict

**PASS — the reviewed load-bearing mechanisms are current and implementable; Critical 0, High 0.**
The pass-2 fallback deadlock and artifact-redirect contradiction are closed. The same-run fallback
projection matches GitHub's documented and live approval response, including duplicate records and
the absence of an approval timestamp. The single stripped-header artifact redirect matches the only
raw ZIP transport. The exact attestation action produces the closed GitHub workflow predicate the
spine now specifies, and its permission/OIDC/reusable-signer assumptions are supported. Immutable
Releases, `ubuntu-24.04`, and the NuGet normalization/repository-signature rules also match current
primary and live evidence.

The already-declared release-eligibility blockers remain external state, not defects hidden by this
verdict: the repository publish-gate variable was observed as `true`, `main` has no protection
ruleset, the split workflow is not implemented, and acceptance/runbook gates remain open. The spine
halts production rather than ratifying that state.

## Findings

No Critical, High, Medium, or Low current-technology finding in the requested load-bearing scope.

## Pass-2 closure

### FIT2-1 — Closed — same-run fallback approval is reachable

AD-9 no longer requires a candidate-bound PR to merge after CI and thereby advance `main`. The
`workflow_dispatch` input now carries the candidate/CI-bound fallback request, while authority comes
from an exact `production` deployment-approval comment in the same Release run. The publisher creates
the final authorization only after authenticating that run's API review history.

GitHub's official review-history response contains `state`, `comment`, `environments`, and `user`, but
no review-event timestamp. A live read of FrontComposer run `34153315061` returned two byte-equivalent
approved rows for `production`; each environment object included the current environment ID/name and
`can_admins_bypass: false`, and each user included login and numeric ID. AD-9 now correctly collapses
byte-identical projections, rejects unequal duplicates, uses a verifier `observed_at`, and does not
invent an approval time. Exact comments are supported by both the deployment UI and API. This makes
the authorization implementable without changing the candidate or weakening the protected
environment. Primary source: GitHub's
[workflow-run review-history API](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run).

### FIT2-2 — Closed — raw artifact acquisition permits the required 302 only

AD-12 now permits exactly one HTTP 302 from the authenticated Actions archive endpoint to an HTTPS
URL without userinfo/non-default port, strips authorization/cookies/caller headers, follows no second
redirect, suppresses the signed query from logs, streams under the AD-7 cap, and authenticates the
result by run/artifact coordinate and digest. All other redirected content remains forbidden.

That is the platform's actual transport. GitHub documents a 302 to a ZIP URL that expires after one
minute and requires Actions-read access. Live artifact `10046042003` returned HTTP 302 to an HTTPS
`blob.core.windows.net` signed URL; streaming the endpoint produced
`sha256:1e549aabe0506b01629433ad61c6ab14a5506f2ba943ed828fc487f51fe17c79`, exactly equal to the artifact
API digest. Primary source: GitHub's
[Actions artifacts REST API](https://docs.github.com/en/rest/actions/artifacts#download-an-artifact).

## Confirmed current fit

### Exact attestation action, predicate, certificate policy, and permissions

- Tag `actions/attest@v4.2.0` resolves to the spine's exact commit
  `f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6`. Its `action.yml` uses Node 24, accepts
  `subject-checksums`, exposes `bundle-path`, and has the selected
  `push-to-registry`/`create-storage-record`/`show-summary` inputs.
- Exact bundled source at that commit constructs `https://slsa.dev/provenance/v1` with build type
  `https://actions.github.io/buildtypes/workflow/v1`; caller `workflow_ref` becomes
  `externalParameters.workflow`; the GitHub internal parameters are exactly event name, repository
  IDs, and runner environment; resolved dependencies contain the caller repository/ref and `gitCommit`;
  `job_workflow_ref` becomes `runDetails.builder.id`; and run ID/attempt become the invocation URL.
  Those paths and exact values agree with AD-19 lines 940–953.
- With `push-to-registry: false`, storage-record creation is not entered, so no
  `artifact-metadata: write` scope is needed. GitHub's binary-attestation guidance requires
  `contents: read`, `id-token: write`, and `attestations: write`, matching AD-18/AD-19. The bundled
  verifier can enforce the repository, reusable signer workflow/digest, source ref/digest, predicate
  type, issuer, and GitHub-hosted runner before inspecting the signed statement.
- GitHub documents that a reusable job's OIDC token identifies the caller in the standard claims and
  the called reusable in `job_workflow_ref`; the direct-job restriction therefore yields the selected
  `.github/workflows/domain-release.yml@<commit>` signer identity. The revised spine correctly keeps
  environment approval outside the predicate and uses certificate/result fields—not signer-controlled
  predicate values—for the platform identity boundary.

Primary sources:
[pinned `actions/attest` commit](https://github.com/actions/attest/commit/f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6),
[exact action source](https://github.com/actions/attest/tree/f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6),
[artifact-attestation guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations),
[attestation verifier reference](https://cli.github.com/manual/gh_attestation_verify), and
[OIDC with reusable workflows](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-with-reusable-workflows).

### Immutable product and incident-evidence Releases

The repository immutable-release API returned `{enabled:true,enforced_by_owner:false}`, and live
release `v4.4.0` returned `draft:false`, `immutable:true`, and SHA-256 asset digests. GitHub locks the
tag and assets at publication and recommends draft creation, complete asset upload, then one publish,
which is AD-19's sequence. The same facility supports AD-15's separate immutable incident prerelease;
`refs/tags/release-evidence/incident/<run>-<attempt>` is a valid Git ref. GitHub permits later edits to
title/notes and prerelease/latest flags, but not to the evidence assets or tag, so the spine binds the
durable platform boundary rather than mutable presentation metadata. Primary source:
[GitHub immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases).

### GitHub-hosted Ubuntu runner

`ubuntu-24.04` is a current standard GitHub-hosted x64 runner label for public repositories. The
current official image manifest inspected in this review was Ubuntu 24.04.4 LTS image
`20260831.293.1` and included .NET SDK `10.0.400`, matching the repository `global.json`. Pinning the
literal label and rejecting self-hosted/Windows/macOS jobs is mechanically enforceable by workflow
closure checks. The image itself is maintained and changes over time; the spine treats it as the
declared GitHub platform runtime rather than claiming byte-reproducible runner images. Primary
sources: [GitHub runner selection](https://docs.github.com/en/actions/how-tos/write-workflows/choose-where-workflows-run/choose-the-runner-for-a-job)
and the official
[Ubuntu 24.04 image manifest](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md).

### NuGet normalization and repository signing

NuGet removes leading zeroes during normalization, omits a zero fourth component, and removes build
metadata for repository identity. AD-19's exact three-component, no-leading-zero, lowercase,
no-build-metadata subset plus byte-equality with the normalized identity avoids those aliases.

The live NuGet-served `Hexalith.FrontComposer.Schema` `4.4.0` package passed
`dotnet nuget verify --all` as a repository signature from the NuGet.org service index. Compared with
the author-unsigned GitHub Release asset, the served ZIP added only root `.signature.p7s`; there were
no removed members and no byte differences in any common uncompressed member. Thus the spine's
permitted-difference rule is valid for the current feed and package, while still correctly requiring
verification for every package rather than assuming signing globally. Primary sources: NuGet's
[package version reference](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning) and
[signed packages reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference).

## Timeboxed limitations

- The target split publisher, exact fallback request, incident recovery, and golden attestation bundle
  do not exist yet, so this review did not execute an end-to-end publication or create external state.
  Their required live rehearsals and fixtures remain part of the implementation gate.
- The certificate/result projection was checked against current GitHub/OIDC/verifier contracts and the
  exact attestation action's bundled source, not a bundle emitted by the future protected topology.
- NuGet propagation timing after a fresh push was not measured. The reviewed concern was normalized
  identity, repository-signature validity, and member equivalence, all of which were exercised against
  a current published package.

## Method

Read-only except this review file. No spine or memlog edit and no external mutation. Evidence included
the exact pinned action ref/source, official current documentation, authenticated GETs for artifact,
approval, environment, immutable-release, and Release objects, a streamed artifact digest comparison,
the official runner-image manifest, and local verification/comparison of already-published package
bytes. The deterministic spine lint also returned zero findings at the reviewed bytes.

## Finding counts

Critical: **0** · High: **0** · Medium: **0** · Low: **0**.
