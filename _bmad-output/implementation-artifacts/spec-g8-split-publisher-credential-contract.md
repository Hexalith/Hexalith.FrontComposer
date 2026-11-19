---
title: 'Resolve the G-8 split publisher credential contract'
type: 'feature'
created: '2026-11-19'
status: 'in-progress'
baseline_commit: '76195272d07a0dd8c73ac3d8ffeb7a6b9a60d825'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-17-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Authorize an implementable G-8 credential and workflow boundary for FrontComposer. The current architecture puts both split jobs in an exact Builds reusable workflow. NuGet.org Trusted Publishing validates the calling repository and requires the `job_workflow_ref` prefix to identify that same repository, so the cross-repository Builds job cannot exchange a token under the FrontComposer policy.

## Boundaries & Constraints

Keep the publication flag at literal `false` until the conformance gate and other release prerequisites are satisfied. The builder runs read-only without a protected environment, write authority, OIDC, or publication secrets. The protected publisher authenticates the run-bound handoff and archive before obtaining publication credentials and executes only the approved closure. It consumes the authenticated candidate archive as data. Keep mandatory attestation or a separately approved fallback bound to the final candidate.

## Decision

On 2026-11-19 the user replied “apply recommended” to the permanent FrontComposer-owned OIDC design. Work may proceed on that design. The future protected job stays in FrontComposer's `release-publish.yml` and invokes exact owner-accepted Builds actions. Use a one-hour NuGet credential exchanged in that job only after authentication. Formal approvals of future exact source digests, Builds revision and closure, conformance evidence, and re-enable remain separate.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`, AD-12/13/16/17/18/19, defines the authorization, workflow identity, sealed provenance, and split roles. The current same-reusable requirement bars the recommended topology without an amendment.
- `Hexalith.Builds@7f085bad76da517d85b835f45facd649dfc7bc36`, `.github/workflows/domain-release.yml` and `Github/split-publication/`, supplies the immutable split implementation and uses `secrets.NUGET_API_KEY` in its publisher. The catalog gitlink and release execution revision are separate.
- `.github/workflows/release.yml` and `.github/workflows/release-publish.yml` currently delegate to a FrontComposer-owned workflow that executes source and exchanges OIDC inside `production`. Retain the same-repository identity where the owner-approved design allows it and remove candidate execution from the publisher.
- `.github/CODEOWNERS` assigns the designated Release Owner to protected paths in preparation for the ruleset. The policy and parser in `eng/dependency-graph-policy.json` and `eng/dependency_graph.py` need to represent the Release Owner logins and pinned `incident_recovery` closure before those new policy fields can activate.

## Initial implementation slice

The implementation handoff for this revision is limited to the shared Builds provenance support below. The later rollout remains recorded under Tasks & Acceptance. Work in the standalone Builds repository at `/home/administrator/projects/hexalith/builds` from `7f085bad76da517d85b835f45facd649dfc7bc36`. Read the owning repository baseline from its declared AI.Tools submodule. Preserve all caller behavior, keep the existing runnable workflows untouched, and perform no remote operations or Git writes. Do not edit FrontComposer files.

- [x] `Github/split-publication/manifest.py` validates and seals an additional, tightly scoped topology for FrontComposer. Preserve the current v4 schema and original Builds reusable projection. Permit `reusable` to name exactly `github.com/hexalith/hexalith.frontcomposer` `.github/workflows/release-publish.yml` at the candidate commit only when the caller is that repository's `.github/workflows/release.yml` at the same candidate. Retain `builds_execution_sha` as an independent exact SHA.
- [x] `Github/split-publication/attestation.py` supports selecting the protected same-repository signer from the already validated manifest provenance. Preserve the existing Builds-reusable signer and validate exact signer identity, candidate signer SHA, and allowed branch/ref. GitHub attestation SLSA `builder.id` can report a ref alias for an intra-repo call; constrain it to the expected exact commit or the known GitHub main ref representation only when issuer and cryptographic signer digest are separately checked by the future runner. Do not introduce new production wiring in this slice. A raw DSSE decode cannot verify a signature.
- [x] `Github/split-publication/tests/test_split_publication.py` exercises the legacy and FrontComposer provenance and signer variants, refusals for mismatched repository, workflow, source, or signer SHA, and ensures old results still pass. Use the owning repository test invocation via `run-with-temp` where scratch outputs are needed.

The local follow-up in `Github/split-publication/descriptor.py` and
`handoff.py` binds the real committed CI caller and Builds CI workflow
separately from the FrontComposer release publisher. The archive helper now
handles an unset execution marker and still refuses a present marker.

## Tasks & Acceptance

**Execution**

- [ ] Record the Architect and Release Owner credential decision and exact workflow ownership. Reconcile the affected ADs and source digests for GOV-ACCEPT-1 and GOV-ACCEPT-2.
- [ ] Qualify the selected exact Builds code and publish the owner acceptance with closure digests. Add the no-bypass main ruleset after CODEOWNERS lands, allowing merge commits that preserve approved evidence heads. Install incident preservation and pre-authorize the exact workflow closure before the separate caller activation.
- [ ] Complete GOV-B through GOV-J and the two protected-main conformance PRs under the selected policy. Use a PR author distinct from the authorized Release Owner for the packet requiring that owner's approval.

**Acceptance criteria**

- Given the approved credential contract, when a publisher authenticates, then the repository, job workflow, environment, and executable closure match the approved identity.
- Given the frozen flag or an unauthenticated candidate, when the exact workflow runs, then no publication credential reaches candidate execution and the verifier observes zero product publication effects.
- Given a passing conformance packet, when its exact head and approval projection are merged, then the gate binds the same candidate, Builds execution SHA, policy, and evidence without relaxing any owner prerequisites.

## Implementation Notes

NuGet's [GitHub token policy validator](https://github.com/NuGet/NuGetGallery/blob/main/src/NuGetGallery.Services/Authentication/Federated/GitHubTokenPolicyValidator.cs) checks the calling repository and the prefix and filename in `job_workflow_ref`. [Cross-repository reusable workflow support](https://github.com/NuGet/NuGetGallery/issues/11000) is open. The earlier proposal to add `NuGet/login` directly to the Builds reusable cannot meet that policy check.

Production remained halted after the authenticated flag update and readback on 2026-11-19. The release nonconformance register contains the NC-35 observation. Ruleset and CODEOWNERS activation remain pending. GitHub reported no effective rule for `main` and an unprotected branch at this baseline.

### Local implementation evidence

- The GOV-1 spine records the selected job topology, exact workflow and action
  identities, attestation, credential ordering, and delayed activation. Companion
  pointers and nine working-tree source digests are refreshed. The incident
  runbook still matches its approved raw-file SHA-256.
- Builds manifest, attestation statement, descriptor, and CI handoff validation
  recognize the FrontComposer-owned publisher and preserve the shared Builds
  release identity. The same-repository statement path requires the exact signer
  certificate identity from a separately cryptographically verified result.
- The Builds reader validated the authenticated Actions archive for CI run
  `44395211053`, attempt `1`, and candidate
  `76195272d07a0dd8c73ac3d8ffeb7a6b9a60d825`. Its raw member SHA-256 was
  `1b38ecc9cce51558ef17f6c6a4ba3f0685b57fd18e41323dbe3df2d1dc5a9be0`.
- The active publisher and policy are still legacy. The new identity validation
  awaits the pinned cryptographic verifier and exact upstream acceptance before
  caller activation. GitHub `production` still permits admin bypass, and the
  protected-main ruleset and live conformance remain pending.
