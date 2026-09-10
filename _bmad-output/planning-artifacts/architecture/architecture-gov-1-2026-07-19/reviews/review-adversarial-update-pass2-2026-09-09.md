# Adversarial Divergence Review — GOV-1 Update, Pass 2

- Review date: 2026-09-09
- Lens: adversarial divergence, closure verification
- Artifact: `ARCHITECTURE-SPINE.md`
- Spine SHA-256: `6c4ab34cea9a3be2237e7d9f7017effcfc292fe6326eb464e075f8ee2bd0948b`
- Deterministic lint: **PASS**, 0 findings
- Verdict: **BLOCK — 0 Critical, 3 High, 3 Medium findings**

The prior Critical finding and all five prior High findings are substantively addressed. The three
prior Medium findings are also closed. Pass 2 found three new cross-rule divergences introduced or
made material by the fixes: nested reusable identity, fallback authorization custody, and the new
version-keyed concurrency boundary.

## Prior-finding closure

| Prior finding | Status | Pass-2 evidence |
| --- | --- | --- |
| ADV-C1, job-scoped authority sequencing | Closed | AD-18 now states the real job-scoped model and relies on candidate-free pinned code plus no credential/write call before bounded authentication. |
| ADV-H1, reusable fallback | Closed | AD-9 defines v2 authorization, 24-hour expiry, capability predicate, explicit retry semantics, and no-side-effect reuse boundary. |
| ADV-H2, attestation identity | Closed | AD-19 pins the action, issuer, repository/ref/candidate, caller, reusable, run/attempt, environment, predicate, and exact subject set. |
| ADV-H3, runtime-loaded code | Closed | AD-12 expressly bans runtime executable acquisition and names hostile fixtures. |
| ADV-H4, unbounded candidate archive | Closed | AD-7 adds raw/member/expanded/ratio/path caps and pre-extraction streaming checks. |
| ADV-H5, integer domain | Closed | AD-5 fixes the interoperable safe-integer range and boundary fixtures. |
| ADV-M1, durable descriptor/handoff | Closed | AD-19 makes the raw candidate archive, descriptor, and CI handoff mandatory Release assets. |
| ADV-M2, asset-name grammar | Closed | AD-19 fixes a bounded conservative ASCII grammar and combined collision check. |
| ADV-M3, timestamp spelling | Closed | Consistency Conventions fix UTC second-precision `Z` spelling. |

## Critical

None.

## High

### ADV2-H1 — Permitted sibling reusable workflows contradict the pinned attestation job identity

- **Applies to:** AD-17, AD-19.
- **Hole:** AD-19 permits a sibling workflow as an internal literal-pin implementation detail, but its
  attestation predicate simultaneously requires the job reusable to be AD-17's
  `.github/workflows/domain-release.yml`. GitHub's OIDC model identifies the reusable workflow that
  actually defines the job in `job_workflow_ref`; caller fields continue to describe the caller.
  A publisher job delegated to a sibling therefore presents the sibling path/commit, not the wrapper.
- **Divergence:** one Builds owner inlines both jobs in `domain-release.yml`; another delegates the
  publisher to an allowed pinned sibling reusable. The first can satisfy the attestation predicate;
  the second cannot without ignoring or rewriting the observed job identity.
- **Falsifier:** move only `publish-publication-candidate` to a pinned nested reusable and inspect its
  OIDC claim/bundle. If `job_workflow_ref` names the sibling, the two permitted topologies do not share
  one acceptance predicate.
- **Required closure:** forbid sibling reusable workflows from defining either split role (local
  composite actions may remain in the exact closure), or add the exact effective leaf reusable
  path/commit to policy, AD-17 provenance, AD-19 predicates, and topology fixtures.
- **Platform evidence:** [GitHub OIDC reusable-workflow semantics](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-with-reusable-workflows?apiVersion=2022-11-28).

### ADV2-H2 — Fallback authorization is self-attributed and its trust bytes are not durable

- **Applies to:** AD-9, AD-15, AD-19.
- **Hole:** `issued_by` is a string inside a mutable repository variable; no signature, immutable PR
  approval, or authenticated variable-change actor is bound to the authorization. The new durable
  asset list also omits the raw fallback-authorization record, while manifest/handoff retain only its
  SHA-256. A later variable replacement plus run-artifact expiry destroys the bytes needed to audit
  reason, issuer, and expiry.
- **Divergence:** one verifier trusts the asserted login and retains only the digest; another requires
  authenticated Release Owner authorship and preserves the canonical record. Both can satisfy the
  current shapes, but only one proves the exception was authorized.
- **Falsifier:** under an owner-approved `attestation_capability: unsupported` policy, write a canonical
  variable naming an authorized owner from a different variable-writer identity, publish through the
  owner-approved environment, then replace the variable and let run artifacts expire. Current durable
  Release evidence proves only that unknown bytes once hashed to the recorded value.
- **Required closure:** authenticate the exact authorization bytes with an immutable approved-PR/API
  record or signature whose actor is in `release_owner_logins`; require the raw canonical authorization
  as a uniquely named Release asset iff fallback is used, and bind its digest byte-exactly in the
  manifest, handoff, and post-release verifier.
- **Platform evidence:** repository variables are mutable through a separate Variables-write API and
  the returned value exposes timestamps, not an author identity: [GitHub Actions variables API](https://docs.github.com/en/rest/actions/variables).

### ADV2-H3 — The concurrency key uses an unnormalized version string for a normalized NuGet identity

- **Applies to:** AD-15, AD-19.
- **Hole:** `release_plan.version` is only a nonempty string, yet it becomes the literal tag and
  `governed-release-<version>` concurrency key. NuGet treats distinct spellings as the same repository
  version (`1`, `1.0`, `1.0.0`, `1.0.0.0`; case differences in prerelease labels; build metadata
  removal). Two Release attempts targeting one NuGet identity can therefore obtain different locks and
  pass literal version/tag comparisons.
- **Divergence:** one implementation canonicalizes with `NuGetVersion`; another preserves Semantic
  Release text. Both satisfy literal equality within their own descriptor/manifest but disagree about
  collision, existence, and retry identity.
- **Falsifier:** dispatch concurrent release plans for NuGet-equivalent spellings such as `1.0.0` and
  `1.0.0.0`, or `1.0.0-alpha` and `1.0.0-Alpha`. If they acquire different concurrency groups while
  the repository treats package identities as equal, the no-race boundary is bypassed.
- **Required closure:** define one accepted canonical NuGet version grammar/normalization, reject any
  noncanonical input before the publisher job, compare embedded package versions by normalized NuGet
  identity, and derive concurrency/existence/tag fields from the canonical value. Add equivalence and
  collision vectors.
- **Platform evidence:** [Microsoft's NuGet version normalization rules](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning).

## Medium

### ADV2-M1 — `artifact-metadata: write` remains allowlisted although the pinned invocation disables storage records

AD-18 permits this scope while AD-19 sets both `push-to-registry` and `create-storage-record` false.
The pinned action documents that storage records require registry push, so the scope is not required by
the specified path. Remove it from the caller allowlist and protected job to preserve the “required
subset” invariant. See the pinned [action input contract](https://github.com/actions/attest/blob/f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6/action.yml).

### ADV2-M2 — Attestation semantics are closed, but the exact signed-field locations are not

AD-19 names every required value but does not say whether each is checked in the signed statement,
certificate/OIDC extension, or both. Pin a verification profile over the Sigstore bundle/SLSA JSON
paths and extension identities; otherwise independently built verifiers can accept different signed
representations of the same semantic label.

### ADV2-M3 — ZIP compression-ratio arithmetic remains implementation-defined

AD-7 supplies ceilings but not the numerator/denominator fields, zero-compressed-size behavior, data
descriptor handling, or whether central-directory overhead participates in the aggregate ratio. Pin
the calculation and add zero-size, data-descriptor, boundary, and contradictory-header vectors.

## Gate disposition

Keep the production halt and do not finalize `split-publication-v1` while ADV2-H1 through ADV2-H3 are
open. The prior ADV findings need no further change at this hash. Lower-severity checking was limited
to the touched attestation/archive/durable-evidence surfaces; no claim is made that every unrelated
Deferred item was re-audited in pass 2.
