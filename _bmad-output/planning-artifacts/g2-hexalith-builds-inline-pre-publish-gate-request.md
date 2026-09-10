# Required upstream dependency (G2 / BUILD-REL-1): privilege-separated governed NuGet release contract

- **Raised by:** REL-2 on 2026-07-13; made mandatory by REL-3/REL-4 on 2026-07-15; amended by
  ratified GOV-1 on 2026-07-19 and the finalized AD-19 architecture on 2026-09-09.
- **Target repository:** [Hexalith/Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds).
- **Target surface:** `.github/workflows/domain-release.yml` and only the literal-commit local
  composite/JavaScript actions in its authorized closure.
- **Status:** successor revision required. Hexalith.Builds issue 17 was reopened and its immutable
  revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` was owner-accepted on 2026-08-08. That revision is
  the AD-16 lineage anchor/predecessor; it is not evidence of the AD-19 split reusable.
- **Current FrontComposer execution pin:** `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`.
- **Required next upstream outcome:** an owner-accepted immutable revision in the AD-16 lineage that
  implements `split-publication-v1` exactly as the finalized GOV-1 spine requires.
- **Owners:** Hexalith.Builds owner + Release Owner for upstream acceptance; FrontComposer Architecture,
  Product Owner, and Release Owner for adoption.
- **Issue:** <https://github.com/Hexalith/Hexalith.Builds/issues/17> and acceptance record
  <https://github.com/Hexalith/Hexalith.Builds/issues/17#issuecomment-5226651759>.
- **Release impact:** the checked-in FrontComposer caller remains on a legacy publication-capable path.
  Production releases remain halted; no current release is made compliant by protected-environment
  approval alone.
- **Repository boundary:** FrontComposer does not edit or commit shared Hexalith.Builds source. Upstream
  implementation and acceptance occur in the owning repository; FrontComposer later pins and
  pre-authorizes the accepted immutable closure through AD-12 delayed activation.

## Why

FR-24 requires the exact prepared packages and evidence to be validated, authorized, published, and
verified without allowing candidate-controlled code to hold product-publication authority. The legacy
and previously proposed governed jobs execute candidate restore/build/release behavior in a protected,
write-capable context. Environment approval authorizes a job; it does not isolate its credentials from
candidate code.

The accepted successor therefore separates candidate construction from publication. Compatibility,
graph provenance, exact-candidate handoffs, mandatory attestation or approved fallback, and durable
attempt evidence remain intact; only pinned candidate-free owner code may make the final authorization
and publication decision.

## Required Change

The selected immutable `domain-release.yml` must provide an opt-in/default-off
`split-publication-v1` mode. Existing callers remain behaviorally unchanged until they explicitly
select it. The reusable defines both fixed jobs directly; neither role may be delegated to a nested or
sibling reusable.

### 1. Secretless candidate builder

The job named `build-publication-candidate`:

- has no environment, publication secret, OIDC/attestation permission, or write scope;
- checks out only the authenticated AD-13 candidate with `submodules: false`;
- initializes only dependencies declared by FrontComposer's root `.gitmodules`, never recursively or
  through nested submodules;
- executes candidate restore/build/pack/release-planning only in this read-only boundary;
- prepares one package set and runs the FR-24 inventory, exact-package tests, consumer validation,
  checksums, SBOM, symbol evidence, and diagnostic early-denial checks against it;
- emits exactly one `publication-candidate-<run_id>-<run_attempt>` artifact containing the closed
  `hexalith.publication-candidate.v1` descriptor and every declared package/evidence/metadata byte; and
- never publishes, mints an attestation, seals a final manifest, or emits an authorization consumed by
  publication. Builder readiness and classification are denial-only diagnostics.

### 2. Protected candidate-free publisher

The job named `publish-publication-candidate`:

- is the only product-publication actor and runs under the caller-selected protected `production`
  environment;
- receives only its minimum token permissions and the environment-scoped NuGet credential; it never
  uses `secrets: inherit`;
- executes only the pinned owner-controlled code in the selected reusable/action closure and never
  checks out FrontComposer, imports candidate helpers, or executes an archive/package lifecycle hook,
  release configuration, plugin, installer, or candidate command;
- downloads the raw builder artifact by authenticated Release run/attempt, name, ID, and digest;
  enforces the AD-7 ZIP limits before extraction; verifies the descriptor, candidate, CI handoff,
  policy, evaluator, release plan, file inventory, sizes, digests, paths, and destination-name
  uniqueness; and treats every candidate byte as non-executable data;
- mints and verifies GitHub provenance attestation over every authenticated package digest, or validates
  the AD-9 run-bound approved-unsupported fallback when the active policy explicitly records
  `attestation_capability: unsupported`;
- prepares and seals `hexalith.release-evidence.v4`, performs complete offline/live verification and
  classification, and requires `publish_authorized=true` before setting `publication_started` and
  making the first NuGet or GitHub Release mutation;
- publishes only descriptor/manifest-authorized bytes, creates an immutable non-draft GitHub Release
  whose tag resolves to the authenticated candidate, and verifies NuGet.org repository signatures plus
  byte-equivalent normalized ZIP members other than the root `.signature.p7s`; and
- preserves the raw publication-candidate archive, descriptor, CI handoff, final manifest, and exactly
  one attestation bundle or fallback authorization as mandatory durable Release assets.

Attestation minting over authenticated package digests is evidence registration, not product
publication, and cannot authorize by itself. Author signing, a production PFX, and RFC 3161 author
timestamping were removed from the release contract by REL-5 on 2026-08-04. Candidate packages remain
author-unsigned and NuGet.org repository-signs uploads.

### 3. Exact handoffs, policy, and evaluator identity

- Release selection remains operator `workflow_dispatch` on the exact live `main` SHA and authenticates
  exactly one completed successful push run of both `ci.yml` and `quality.yml` for that candidate.
- CI emits `hexalith.dependency-release-handoff.v1`; its authenticated candidate remains the sole
  release-candidate authority.
- Every Release attempt emits `hexalith.release-verification-handoff.v3` under `if: always()`, carrying
  the selected quality run, original CI handoff, candidate, active policy, publication-candidate
  coordinate, release state, final manifest v4, attestation/fallback, assets, denial reason, and exact
  Release evaluator. The deferred sentinel is valid only when no CI handoff was authenticated.
- CI, Release, post-release, and incident-recovery evaluators must match one exact active-policy row and
  complete literal-commit closure. Candidate or later ambient default-branch code cannot make a
  publication, verification, ledger, or incident-recovery decision.
- The reusable reference, `builds-execution-sha`, and the `.hexalith/builds-execution` checkout refs in
  the Release closure are the same immutable AD-16 lineage commit.

### 4. Attempt truth, fallback, and incident recovery

- `frontcomposer.release-ledger-record.v2` assigns every authenticated attempt exactly one closed
  disposition. Observations are append-only and keyed by immutable Release/verification coordinates;
  an incident can never be replaced or weakened by a later green rerun or sign-off.
- Fallback authorization uses the canonical
  `hexalith.attestation-fallback-authorization.v3` record and binds one Release run/attempt, candidate,
  CI handoff, active policy, fallback digest, expiry, and exact `production` deployment approval. A
  retry or any graph/policy/workflow/package-set/run drift requires a new request and approval.
- If publication started but no complete immutable product Release exists, the separately authorized
  candidate-free `incident_recovery` closure preserves authenticated/quarantined evidence in a unique
  immutable reserved-namespace prerelease. It has no NuGet credential or product-release authority.
- Before retry or later dispatch, the Release Owner follows `docs/release-incident-response.md`: record
  acknowledgement and containment, set the publish gate false, preserve immutable evidence, rotate
  possibly exposed credentials, inventory every external effect, prefer unlisting, and issue any
  correction under a new version. Re-enable only after documented remediation, independent
  verification, and Release Owner approval.

## Deny-Only Publication Stop

`HEXALITH_RELEASE_PUBLISH_ENABLED` remains a common deny-only control. Any value other than literal
`true` prevents publication; it never grants authority and never compensates for an invalid candidate,
policy, evaluator, handoff, manifest, attestation/fallback, or privilege boundary. During the current
halt, FrontComposer requires literal `false`. A Release Owner or repository administrator must set and
verify that value through an authenticated API observation before any release approval or dispatch.

## GOV-1 Split Implementation Gate

The upstream revision is necessary but not sufficient. FrontComposer may claim adoption only after the
spine-defined **GOV-1 split implementation gate** passes. The gate requires the canonical
`frontcomposer.gov1-split-conformance.v1` packet at
`_bmad-output/implementation-artifacts/gov-1-split-publication-conformance.json`, a spine hash, exact
FrontComposer/Builds/policy/caller identities, authenticated checks and evidence runs, five passing
review lenses, a closed nonconformance register, an unchanged protected-main evidence PR with Release
Owner approval, and a distinct approval-projection PR. Stale, mixed, unavailable, direct-push,
squashed/rebased, or incomplete evidence fails closed.

The eight implementation closure bundles are:

1. Owner-accepted split reusable plus AD-12 delayed activation.
2. Caller switch and exact two-job topology.
3. Removal of `production` from candidate/build jobs.
4. Publication-candidate production/authentication and hostile-candidate credential fixtures.
5. Handoff-v3, total classifier, and typed ledger migration.
6. Candidate-free final attestation/fallback, manifest-v4 classification, and publication.
7. Pinned post-release helper execution with no ambient or candidate helper.
8. Duplicate destination asset-name rejection.

## Unresolved Owner Decisions

- **Product Owner + Release Owner:** accept or revise AD-19, the production-release halt, and the current
  no-exception posture (PRD D-16/G-8).
- **Hexalith.Builds owner + Release Owner:** identify and accept the immutable split-reusable successor
  entering AD-16 lineage.
- **Release Owner or repository administrator:** set the deny-only gate to literal `false` and record the
  authenticated observation.
- **Product Owner + Release Owner:** approve a measurable incident acknowledgement/containment target
  and the incident runbook before integration.
- **Release Owner:** establish and verify the no-bypass protected-main ruleset required for conformance
  and ledger approvals.
- **Product Owner + Release Owner + Architect:** no bounded risk exception exists. Any future exception
  requires a separate dated decision naming scope, expiry, evidence, compensating controls, and
  revocation trigger.

## Bounded Contingency

If the shared contract cannot land before a required release, stop. A FrontComposer-owned contingency
is permitted only by a new dated Architect + Release Owner decision that records equivalent
graph/policy/evaluator/split-role/handoff/manifest/attestation/evidence proofs, scope, approvers, expiry,
revocation and migration triggers, and closure through the same implementation gate. GOV-1 grants no
such contingency.
