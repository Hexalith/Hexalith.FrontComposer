---
created: 2026-07-19
updated: 2026-09-09
story: GOV-1
owner: Product Owner + Architect + Developer + Release Owner
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md
course_correction: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
decision: _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
architecture_spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
status: in-progress
architecture_status: final
product_release_acceptance: pending
implementation_conformance: open
release_eligibility: blocked
priority: before the next governed production release
baseline_commit: 3786330d241c2d87449fa3e01afc95fc832552df
upstream_catalog_follow_up: BUILD-CAT-1
upstream_release_follow_up: BUILD-REL-1 split-publication successor in the AD-16 lineage
implementation_gate: GOV-1 split implementation gate
---

# GOV-1: Validate Shared-Catalog Compatibility and Seal Dependency Provenance

## Story

As a framework maintainer and Release Owner,
I want semantic shared-catalog compatibility and exact dependency provenance carried through a
privilege-separated release,
so that pointer advances remain reviewable and reproducible while candidate-controlled code cannot
obtain publication authority.

## Why This Story Exists

Historical Governance conflated compatibility with selected commit identity. GOV-1 separates semantic
compatibility from deterministic provenance, then binds the exact CI-tested candidate and its graph to
one release evidence chain. The finalized 2026-09-09 spine additionally closes the publication
privilege gap: candidate construction occurs in a secretless builder, while only pinned candidate-free
owner code in a protected publisher may attest, seal manifest v4, classify, and publish.

The current implementation is not conforming merely because earlier graph, handoff, or manifest work
exists. The checked-in caller still selects a legacy publication-capable path, the target contracts have
advanced to handoff v3 and manifest v4, and the spine's exact implementation gate has not passed.

## Authority And Current State

- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`
  is authoritative. AD-1 through AD-19 keep their stable IDs; this story cannot redefine them.
- The 2026-07-19 FC-DEP-1 approval remains valid for the original graph/provenance decision.
  Administrator adopted the 2026-09-09 architecture direction, but Product Owner and Release Owner
  acceptance of AD-19 and the halt/no-exception posture remains open.
- Hexalith.Builds revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` is the accepted AD-16
  lineage anchor/predecessor. A later owner-accepted immutable revision must implement
  `split-publication-v1` before FrontComposer selects it.
- Production releases remain halted. `HEXALITH_RELEASE_PUBLISH_ENABLED` must be literal `false` as a
  deny-only emergency stop and can never authorize publication. No bounded risk exception exists.

## Acceptance Criteria

1. **Semantic compatibility and bounded provenance.** Given an explicit FrontComposer commit, GOV-1
   collects the complete AD-1 depth-1/depth-2 committed-object graph, validates every selected Builds
   catalog under its exact active-policy semantic profile, and records deterministic graph/catalog
   provenance without a commit or fingerprint allowlist. Compatible pointer advances pass;
   incompatible or unavailable inputs fail with exact owner/edge/catalog diagnostics.

2. **Exact revision and policy authority.** PR and push evidence use AD-8's explicit revisions and
   AD-12's immutable base/before policy. Candidate policy or evaluator changes cannot authorize
   themselves. Every CI, Release, post-release, and incident-recovery evaluator matches one exact
   active-policy row and complete pinned closure; zero/unknown/duplicate/mutable authority fails closed.

3. **Authenticated release selection.** Operator `workflow_dispatch` authenticates the exact live
   `main` SHA against exactly one completed successful push run of both `ci.yml` and `quality.yml`.
   `hexalith.dependency-release-handoff.v1` preserves the sole release candidate, policy, graph, and CI
   evaluator. No tag, ambient checkout, second-hop SHA, later default branch, or diagnostic source proof
   can replace it.

4. **Privilege-separated publication.** The selected immutable Builds reusable exposes exactly one
   `build-publication-candidate` job and one `publish-publication-candidate` job. Candidate-controlled
   code executes only in the secretless/read-only builder and crosses the boundary only through the
   authenticated `hexalith.publication-candidate.v1` archive. Builder output may deny early but cannot
   become a final seal, classification, authorization, or executable publisher input. The protected
   publisher executes only candidate-free pinned owner code and is the sole product-publication actor.

5. **Handoff v3 and manifest v4.** Every authenticated Release run emits
   `hexalith.release-verification-handoff.v3` under `if: always()`. It carries the selected quality run,
   original CI handoff/candidate, policy, publication-candidate coordinates, release state,
   prepublication denial, final `hexalith.release-evidence.v4`, attestation/fallback, exact assets, and
   Release evaluator. V4 seals the graph, policy, selected runs, workflow provenance, and byte-identical
   attestation-or-fallback projection. V1-v3 manifests are audit-only for new publication.

6. **Run-bound fallback authorization.** Fallback is eligible only when the active policy explicitly
   records unsupported attestation capability. The canonical
   `hexalith.attestation-fallback-authorization.v3` matches one Release run/attempt, candidate, CI
   handoff, policy, fallback digest, expiry, and exact protected-environment approval. Retry or any
   candidate/graph/policy/workflow/package-set/run drift requires a new request and approval. Ordinary
   operational failure cannot select fallback.

7. **Attempt truth, durable evidence, and recovery.** The total AD-15 classifier maps every governed
   attempt to exactly one closed disposition. `frontcomposer.release-ledger-record.v2` observations are
   append-only and attempt-keyed; later verification or sign-off cannot erase an incident. Every
   publication-started attempt is checked for partial effects. If the product Release is incomplete or
   absent, the separately authorized candidate-free recovery stage preserves authenticated and
   quarantined evidence in an immutable reserved-namespace prerelease before any retry.

8. **Split implementation gate.** GOV-1 completes only when the spine-defined
   `frontcomposer.gov1-split-conformance.v1` packet and approval projection pass live authentication,
   all eight closure bundles and every required register row are closed, five reviewer lenses report
   PASS with zero Critical/High findings, and both unchanged evidence/approval PRs merge through the
   required no-bypass protected-main process. Stale spine hashes, mixed identities, unavailable source
   evidence, direct pushes, squash/rebase, or an open row fail the gate.

## Implementation Work Remaining

- [x] Finalize and adopt the GOV-1 architecture spine while preserving AD-1 through AD-19.
- [x] Reconcile the parent architecture, FC-DEP-1, this story, the G2 request, PRD projection, and
  nonconformance register with the spine.
- [ ] Record Product Owner and Release Owner acceptance of AD-19, the production-release halt, and the
  current no-exception posture.
- [ ] Set `HEXALITH_RELEASE_PUBLISH_ENABLED` to literal `false` and capture an authenticated API
  observation. This is a Release Owner or repository-administrator action, not part of this document
  update.
- [ ] Obtain the owner-accepted Hexalith.Builds split-reusable revision and activate its exact closure
  through the AD-12/AD-16 two-phase process.
- [ ] Close the eight implementation bundles in the nonconformance register:
  1. split reusable and delayed activation;
  2. caller switch and exact two-job topology;
  3. removal of `production` from candidate/build jobs;
  4. publication-candidate production/authentication and hostile-candidate proof;
  5. handoff-v3, total classifier, and typed-ledger migration;
  6. candidate-free attestation/fallback, final manifest-v4 classification, and publication;
  7. pinned post-release helper execution without ambient/candidate code; and
  8. duplicate destination asset-name rejection.
- [ ] Publish and approve `docs/release-incident-response.md`, including the measurable Product/Release
  acknowledgement and containment target, immutable evidence preservation, credential rotation,
  external-effect inventory, unlisting preference, new-version correction, and re-enable approval.
- [ ] Establish and verify the no-bypass protected-main ruleset required for conformance and append-only
  ledger approvals.
- [ ] Produce the canonical conformance packet, authenticated run/check/reviewer evidence, Release Owner
  approval PR, and distinct approval-projection PR; pass the live validator and close GOV-1.

Existing graph/catalog implementation and historical test evidence may be reused only after it is
revalidated against the finalized spine and included in the gate packet. Prior checkmarks or green runs
do not substitute for current conformance evidence.

## GOV-1 Split Implementation Gate

The exact schema, equality map, validator command, allowed checks, reviewer sidecars, evidence runs,
approval review body, merge requirements, and negative fixtures are defined only in the spine's
Deferred section. The required packet path is:

`_bmad-output/implementation-artifacts/gov-1-split-publication-conformance.json`

The packet PR must add evidence only and leave every implementation, policy, workflow, and prior
evidence byte unchanged relative to its tested `frontcomposer_commit`. The later approval-projection PR
must likewise preserve prior bytes. Release remains ineligible until that gate and every adoption
condition pass.

## Unresolved Owner Decisions

| Decision | Owner | Current disposition | Closure evidence |
| --- | --- | --- | --- |
| Accept AD-19, the production-release halt, and the no-exception posture | Product Owner + Release Owner | Open; architecture adoption does not imply acceptance | Dated decision citing AD-19 and PRD D-16/G-8 |
| Accept the immutable split-reusable successor into AD-16 lineage | Hexalith.Builds owner + Release Owner | Open; `a8a50859…` is predecessor only | Accepted revision and exact closure evidence |
| Apply and verify the literal-false emergency stop | Release Owner or repository administrator | Open; the spine records the last observation as literal `true` | Authenticated API value and server timestamp |
| Approve incident acknowledgement/containment target and runbook | Product Owner + Release Owner | Open | Approved `docs/release-incident-response.md` plus exercise/tabletop evidence |
| Establish the no-bypass protected-main process | Release Owner | Open | Authenticated ruleset/protection evidence |
| Authorize a bounded risk exception | Product Owner + Release Owner + Architect | No exception authorized | Separate dated decision with scope, expiry, evidence, compensating controls, and revocation trigger |

## Non-Goals

- No runtime, public API, generated output, package inventory, dependency-version, or UX change.
- No code, workflow, dependency, submodule, or GitHub-settings change in this specification update.
- No author signing, production PFX, or RFC 3161 author-timestamp requirement. Candidate packages are
  author-unsigned; GitHub provenance attestation or the approved unsupported fallback remains mandatory,
  and NuGet.org repository-signature/normalized-member verification remains binding.
- No same-job candidate build and publication path.
- No graph traversal below depth 2 and no mandatory BUILD-CAT-1 marker without a separate approval.
- No FrontComposer-owned contingency without the separate dated decision required by AD-16.

## References

- Authoritative spine: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.
- Parent architecture: `_bmad-output/planning-artifacts/architecture.md`.
- Decision record: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`.
- Upstream request: `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`.
- Product gates: `_bmad-output/planning-artifacts/prd.md` D-16, G-2, and G-8.
- Implementation register: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md`.

Historical implementation narration and validation receipts remain available in repository history and
the dated architecture review artifacts. They are not current conformance claims.
