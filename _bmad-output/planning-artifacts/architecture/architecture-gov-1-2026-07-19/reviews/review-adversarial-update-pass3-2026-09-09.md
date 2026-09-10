# Adversarial Divergence Review — GOV-1 Update, Pass 3

- Review date: 2026-09-09
- Lens: adversarial divergence
- Artifact: `ARCHITECTURE-SPINE.md`
- Spine SHA-256: `9dc9ded5670839f24a49ebc4499b6086ddf2590215a178f28311399a01244e5a`
- Deterministic lint: **PASS**, 0 findings
- Verdict: **BLOCK — 2 Critical, 3 High, 2 Medium findings**

All pass-2 findings were re-tested. Their direct fixes are present, but fallback PR authentication and
incident preservation introduce two internally unsatisfiable paths. Three additional High findings
leave attestation, archive, and signoff behavior divergent across otherwise competent implementations.

## Pass-2 closure retest

| Pass-2 finding | Status | Pass-3 result |
| --- | --- | --- |
| ADV2-H1, sibling reusable identity | Closed | Both split jobs must now live directly in the selected reusable; nested/sibling reusable jobs are forbidden. |
| ADV2-H2, fallback actor and durability | Direct concern closed; superseded by ADV3-C1 | The review actor is API-authenticated and the exact v3 record is now a conditional durable asset, but the approval PR changes the candidate it is meant to authorize. |
| ADV2-H3, NuGet-normalized concurrency | Closed | AD-19 fixes a canonical lowercase three-component subset, normalized package equality, and the canonical lock key. |
| ADV2-M1, excess `artifact-metadata` scope | Closed | AD-18 explicitly forbids it. |
| ADV2-M2, signed-field locations | Direct concern closed; see ADV3-H1 | AD-19 pins statement paths and a bundled verifier, but uses an incompatible repository spelling in the exact comparison. |
| ADV2-M3, ZIP ratio arithmetic | Closed | AD-7 fixes numerator, denominator, zero-byte, header, descriptor, ZIP64, and streaming behavior. |

## Critical

### ADV3-C1 — The fallback approval PR creates an infinite main-head/candidate recursion

- **Applies to:** AD-9, AD-13.
- **Contradiction:** AD-9 creates the approval payload only after a specific push-CI run completes and
  requires the payload PR's unchanged head to merge to protected `main`. AD-13 then requires the
  Release candidate and selected CI candidate to equal the live `main` head at dispatch.
- **Failure sequence:** let `C` be the CI candidate. The CI-bound approval file cannot already exist in
  `C`, because its path/body contain `C`'s future run ID and attempt. Merging it creates head `A != C`.
  A Release for `C` now fails AD-13. Running CI for `A` and approving that run requires another file
  and produces `B != A`; the recursion never reaches an eligible head.
- **Divergence:** one publisher accepts the approval merged after `C` and violates exact-current-main;
  another enforces AD-13 and makes fallback permanently unreachable.
- **Falsifier:** execute the prescribed CI → approval PR merge → dispatch sequence and compare the
  authenticated handoff candidate, approval payload candidate, dispatched SHA, and live main SHA. No
  commit can satisfy all four.
- **Required closure:** authenticate fallback outside the candidate branch. One workable shape is a
  workflow-dispatch request bound to the exact fallback digest and the same Release run, approved in
  the protected `production` deployment-review comment. Define the exact GET-approvals projection,
  authenticate user/state/environment/comment, reject unequal records, collapse duplicate
  byte-identical records deterministically, and do not invent an approval timestamp the API does not
  provide. A signed external control record is another option; do not weaken AD-13 current-main.

### ADV3-C2 — Incident preservation requires a second publisher that AD-18 forbids

- **Applies to:** AD-12, AD-15, AD-18.
- **Contradiction:** AD-18 grants publication authority only to the AD-19 publisher/attester. AD-15
  requires `release-incident-preservation.yml` to create a Git tag, prerelease, and Release assets under
  `production`. That recovery job necessarily needs `contents: write` and performs the same GitHub
  Release mutations AD-9/AD-18 classify as publication side effects.
- **Divergence:** Governance can reject the recovery job for holding forbidden authority, making the
  mandatory pre-retry preservation step impossible; or it can permit the job and silently create an
  unregistered second publication actor outside AD-18's sole-actor boundary.
- **Falsifier:** add the minimum `contents: write` permission required by the recovery workflow and run
  the AD-18 static assertion. Passing and failing each contradict one of the two rules.
- **Required closure:** define a separate least-privilege `incident_recovery` authorization stage and
  active-policy closure; amend AD-18 to allow only that candidate-free protected actor to write the
  reserved incident tag/Release namespace, with no NuGet secret, OIDC, attestations, arbitrary tag, or
  product-Release authority. Add hostile namespace and permission-escalation fixtures.

## High

### ADV3-H1 — The exact attestation repository comparison likely rejects the pinned action's own output

- **Applies to:** AD-19.
- **Hole:** the detailed signed-field rule requires
  `externalParameters.workflow.repository == https://github.com/hexalith/hexalith.frontcomposer`,
  while the repository's canonical GitHub name is `Hexalith/Hexalith.FrontComposer`. GitHub's official
  provenance generator constructs this signed field directly as
  `GITHUB_SERVER_URL + "/" + OIDC claims.repository`; it does not lowercase it. A later paragraph also
  names `github.com/hexalith/hexalith.frontcomposer` without the scheme.
- **Divergence:** a byte-strict verifier rejects the official bundle; a normalizing verifier accepts it
  but violates “differently located or unequal fails closed.”
- **Falsifier:** generate the mandatory golden bundle in this repository with the exact pinned action
  and byte-compare the signed repository field to the stated lowercase literal.
- **Required closure:** bind the expected signed value to the exact canonical GitHub/OIDC spelling
  emitted for this repository, separately define any normalized internal identity, and remove the
  redundant conflicting summary paragraph. Pin the golden bundle before integration.
- **Implementation evidence:** GitHub's generator assigns the repository field without normalization:
  [official provenance source](https://github.com/actions/toolkit/blob/main/packages/attest/src/provenance.ts).

### ADV3-H2 — Archive paths are POSIX-safe but not safe on every permitted GitHub-hosted runner

- **Applies to:** AD-7, AD-19, Consistency Conventions.
- **Hole:** publisher runner OS is not pinned. The shared path grammar allows ASCII POSIX names such as
  `CON`, `NUL`, or `file:stream`; these have reserved-device or alternate-data-stream semantics on a
  GitHub-hosted Windows runner. AD-7 then materializes candidate-controlled members to a filesystem.
- **Divergence:** the same conforming archive extracts and hashes normally on Linux but collides,
  aliases, or creates alternate streams on Windows. Denying self-hosted runners does not resolve this.
- **Falsifier:** feed otherwise valid rows named `CON/evidence.json` and `file:stream` to Windows and
  Linux implementations and compare extraction/inventory results.
- **Required closure:** pin the privileged publisher and recovery OS/image and include it in the
  closure/fixtures, or extend path rules with platform-independent reserved-name, colon, trailing
  dot/space, and device-name rejection before materialization.

### ADV3-H3 — The two-PR signoff contract cannot append the “later sign-offs” it permits

- **Applies to:** AD-15.
- **Hole:** the first approval payload has exactly one path derived only from the observation's run
  coordinates. It contains a signer/review-specific authorization, yet AD-15 says later sign-offs may
  append. A second signer has no distinct append-only approval path and would have to replace the first
  payload or invent an ungoverned path. The resulting signoff record's repository path and uniqueness
  key are also not specified.
- **Divergence:** one ledger permits exactly one signoff; another adds signer/review suffixes; another
  replaces the first approval payload. Only the last behavior is clearly forbidden, but all arise from
  the current shape.
- **Falsifier:** attempt two valid owner signoffs for one observation without modifying an existing
  path and without inventing a path absent from the contract.
- **Required closure:** either state exactly one signoff per observation or key approval and signoff
  paths by observation plus immutable signer/review ID; specify the signoff destination, ordering,
  duplicate rule, and exact projection-PR approval evidence.

## Medium

### ADV3-M1 — “Every available” incident evidence is not a closed completeness predicate

`incident-evidence.json` lists only assets the recovery process calls available. It has no required-slot
map or explicit missing/unavailable rows, so later auditors cannot distinguish evidence that never
existed, could not be fetched, or was silently omitted. Define phase-dependent required slots with a
closed `{status, coordinates, sha256}` projection and preserve authenticated absence/error evidence.

### ADV3-M2 — Incident Release recovery after a partial recovery mutation is unspecified

The deterministic incident tag/name makes retries converge, but a crash after creating the tag or
draft Release leaves the next recovery run unable to satisfy “immutable, non-draft” without mutating or
replacing prior state. Define idempotent adoption of an exact existing draft/asset set, or classify the
recovery attempt and prescribe a new evidence-only suffix/attempt identity. This must never permit
product Release or package replacement.

## Gate disposition

Do not finalize or enable split publication while ADV3-C1/C2 or ADV3-H1–H3 are open. The spine also
already records the independent operational Critical blocker that
`HEXALITH_RELEASE_PUBLISH_ENABLED` was observed literal `true`; that known external-state condition is
not double-counted in this review's severity totals. Lower-severity review was concentrated on the
named pass-3 focus areas rather than a fresh audit of unrelated Deferred items.
