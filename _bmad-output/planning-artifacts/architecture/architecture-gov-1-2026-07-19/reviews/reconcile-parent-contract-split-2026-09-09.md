---
review: reconcile-parent-contract-split
date: 2026-09-09
intent: update-finalize-input-reconciliation
spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
spineSha256: 95cbe2f2754ad3c1d7c896e73822b49edb676c0f5921c4f05fded82519c6e6e8
inputs:
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
  - _bmad-output/planning-artifacts/prd.md
mutations: report-only
verdict: pass-no-critical-high
criticalFindings: 0
highFindings: 0
---

# Parent, contract, and PRD reconciliation — split-publication update

## Verdict

**PASS at the recorded spine hash: zero Critical, zero High findings.** The latest spine closes prior
F1-F5 and H1-H5. No quiet parent, FC-DEP-1, or current-PRD requirement is silently dropped. Where the
new architecture supersedes stale same-job/v3 source text, it names the conflict, preserves the durable
invariant, and blocks integration until the owning source is reconciled.

This is an architecture-input reconciliation only. It does not claim implementation conformance,
Product/Release acceptance, G-8 closure, or release eligibility.

## Prior F1-F5 closure

| Finding | Status | Current spine evidence |
| --- | --- | --- |
| F1 exact prepared-package validation/evidence | Closed | Inherited invariant line 69 and AD-19 lines 716-720 require inventory, exact-package tests, consumer validation, checksums, SBOM, symbols, and complete descriptor inventory over one prepared set. |
| F2 NuGet repository-signing comparison | Closed | AD-19 lines 760-763 allow only root `.signature.p7s`, require a valid repository signature, and compare every other normalized ZIP member. |
| F3 GitHub tag-to-candidate binding | Closed | AD-19 lines 757-760 require an immutable non-draft Release whose tag resolves to the AD-13 candidate. |
| F4 deferred handoff incident semantics | Closed | AD-15 lines 557 and 577-581 make `deferred-no-ci-handoff` terminal and incident-bearing, matching FC-DEP-1 decision 15. |
| F5 ownership projection | Closed | AD-16 distinguishes Builds reusable-code/minimum-permission ownership, FrontComposer schema/output/operation ownership, and Release Owner approval/credential/exception/containment ownership. |

## H1-H5 closure

### H1 — selected quality run in the manifest

Closed. AD-17 advances new publication to `hexalith.release-evidence.v4` and seals `quality_run` as the
exact AD-13 projection (lines 633-645). It remains denial evidence outside
`workflow_provenance.definition_digest`, preserving AD-13's non-trust-source rule. AD-14 makes v3
audit-only and preserves its historical bytes.

### H2 — exact manifest attestation/fallback binding

Closed. Manifest v4 names the exact top-level `attestation` member and reuses the closed AD-15
`{status, bundle, fallback_fingerprints_sha256}` shape byte-for-byte (AD-17 lines 637-645). AD-14/AD-17
require atomic producer, verifier, fixture, and fallback migration rather than mutating v3 in place.

### H3 — pre-authorization attestation side effect

Closed for spine handoff. AD-9 lines 256-265 explicitly classifies attestation minting as the sole
pre-authorization external **evidence registration**, not package/Release publication: it cannot mutate
candidate bytes or create a tag, Release, asset, or NuGet version and cannot authorize. The parent must
adopt this narrow distinction before integration; Deferred lines 927-938 make that reconciliation and
release ineligibility explicit. The conflict is surfaced and gated, not silently overridden.

### H4 — D-16/G-8 acceptance and interim posture

Closed. “Adoption and Release Eligibility” lines 75-84 states that architecture direction does not
close D-16/G-8 or imply Product/Release acceptance. It defines
`HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the authoritative deny-only emergency stop, halts production
releases, authorizes no bounded-risk exception, and fixes the approvers and contents of any future
exception. Deferred lines 919-926 keeps the approved runbook and measurable response target open and
integration-blocking.

### H5 — Release Owner ledger sign-off

Closed. AD-15 lines 586-597 keeps the machine observation immutable and defines a separate closed,
append-only `frontcomposer.release-ledger-signoff.v1` record bound to the observation's canonical
SHA-256 and an authenticated Release Owner `APPROVED` review. Missing sign-off remains visibly pending;
later sign-offs cannot rewrite an observation or turn an incident green. Its dependency on a no-bypass
ruleset is explicit and implementation-blocking.

## State and delivery consistency

- `publication_started` is an owner-controlled marker immediately before the first NuGet or GitHub
  Release mutation, not publisher job start or attestation registration; job result and external state
  cross-check it.
- The builder transfers only authenticated packages, evidence, and bounded release metadata. Final
  attestation, manifest v4 sealing, live/offline verification, classification, and authorization remain
  in pinned candidate-free publisher code.
- The total disposition mapping keeps deferred/missing/non-compliant/partial states incident-bearing,
  preserves disjoint non-incident pre-publication states, and never lets a rerun weaken an incident.
- Exact package evidence, tag binding, NuGet normalized-content verification, immutable evidence, and
  three-way ownership remain intact.

## Deferred source drift — correctly gated

The following are not remaining spine findings because the spine explicitly identifies them and blocks
integration until their owners reconcile them:

- `architecture.md` still projects the same-job production preparation/Semantic Release mechanism and
  manifest v3. It must adopt split publication, manifest v4, and the evidence-registration distinction
  while preserving exact-artifact, tag, NuGet, and ownership invariants.
- FC-DEP-1 decisions 13-15 still project candidate checkout through publication and manifest v3. Add the
  split under the next stable decision ID; do not renumber decisions 1-16 or mutate historical evidence.
- The current PRD accurately keeps D-16/G-8 open but still names manifest v3. Its target evidence schema
  must advance to v4 before implementation.
- The GOV-1 story, G2 request, and nonconformance register remain source-owner work under Deferred lines
  927-938, including the eight split implementation closure bundles.

## Lower-severity notes

No additional Medium/Low source contradiction changes this verdict at the recorded hash.

