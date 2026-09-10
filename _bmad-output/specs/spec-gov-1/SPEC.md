---
id: SPEC-gov-1
companions:
  - owner-decisions.md
  - ../../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/architecture.md
  - ../../contracts/shared-catalog-dependency-governance-2026-07-19.md
  - ../../planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md
  - ../../implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md
  - ../../planning-artifacts/prd.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete,
> preservation-validated contract for what to build, test, and validate. The finalized architecture
> spine is authoritative for mechanism; AD-1 through AD-19 retain their stable IDs.

# GOV-1 Shared-Catalog Compatibility, Dependency Provenance, And Governed Publication

## Why

GOV-1 prevents legitimate dependency-pointer changes from failing solely because their identities
changed while ensuring every release remains reproducible from exact committed inputs. It must also
prevent candidate-controlled code from sharing publication authority: one authenticated CI-tested
candidate is built without secrets, then only pinned candidate-free owner code may attest, classify,
publish, verify, and preserve durable attempt evidence.

## Capabilities

- **CAP-1**
  - **intent:** Validate shared-catalog compatibility semantically while recording a deterministic bounded committed-object dependency graph as provenance.
  - **success:** Compatible pointer advances pass, incompatible catalogs fail with exact edge diagnostics, and graph identity is sealed without a commit or fingerprint allowlist.
- **CAP-2**
  - **intent:** Carry one authenticated CI-tested candidate through governed release and independent verification.
  - **success:** Handoff v3, manifest v4, and ledger v2 together preserve the exact candidate, policy, quality run, evaluator closure, publication candidate, attestation or fallback, assets, and final disposition across every attempt.
- **CAP-3**
  - **intent:** Separate candidate construction from protected product publication.
  - **success:** Candidate-controlled code executes only in a secretless read-only builder, while a candidate-free protected publisher authenticates the artifact as data, performs final attestation or fallback validation, seals and classifies manifest v4, and alone can publish.
- **CAP-4**
  - **intent:** Authorize fallback only for one authenticated Release run and attempt when attestation capability is explicitly unsupported.
  - **success:** The publisher accepts only an unexpired run-bound authorization v3 matching the candidate, CI handoff, active policy, fallback digest, production approval, and Release coordinates; drift or retry requires new authorization.
- **CAP-5**
  - **intent:** Classify every governed Release attempt and preserve durable, monotonic evidence for failures and incidents.
  - **success:** Handoff v3 supplies closed attempt state to the total classifier, ledger observations are append-only, incidents cannot be overwritten by later green observations, and incomplete publication is preserved in an immutable incident evidence Release before retry.
- **CAP-6**
  - **intent:** Gate split-publication adoption on authenticated implementation and review evidence.
  - **success:** The GOV-1 split implementation gate accepts only the canonical conformance packet and approval projection defined by the spine, with every required closure row closed and no Critical or High nonconformance.

## Constraints

- AD-1 through AD-19 are authoritative and retain their current IDs; any summary ambiguity resolves to the finalized spine.
- GOV-1 changes no runtime or public API behavior, generated output, package inventory, dependency versions, or UX.
- Builder results can deny early but never authorize; final publication authority resides only in pinned candidate-free protected publisher code.
- New publication uses `hexalith.release-verification-handoff.v3`, `hexalith.release-evidence.v4`, `hexalith.publication-candidate.v1`, `frontcomposer.release-ledger-record.v2`, and the run-bound fallback authorization v3 defined by the spine. Legacy evidence remains audit-only.
- Same-job candidate build/publication, author signing, production PFX custody, and RFC 3161 author timestamps are superseded. Author-unsigned candidates still require GitHub provenance attestation or the approved unsupported fallback and NuGet.org repository-signature verification.
- Production releases remain halted until Product and Release owners accept AD-19 and the halt posture, a split Builds revision enters the AD-16 lineage, the incident runbook and response target are approved, and the GOV-1 split implementation gate passes.
- `HEXALITH_RELEASE_PUBLISH_ENABLED` is deny-only and must be literal `false` during the halt; it can never authorize publication.

## Non-goals

- Implementing code, policies, tests, workflows, dependency changes, submodule changes, or GitHub settings as part of this specification update.
- Traversing dependency edges below depth 2 or making the BUILD-CAT-1 marker mandatory without separate approval.
- Authorizing a FrontComposer-owned contingency or bounded risk exception without the separate dated owner decision required by the spine.
- Treating source reconciliation, prior green runs, or builder-produced evidence as implementation-gate closure.

## Success signal

An exact candidate with a compatible dependency graph can traverse CI, the secretless builder, the
protected candidate-free publisher, and post-release verification with handoff v3, manifest v4,
run-bound authorization, exact published-byte verification, and durable attempt evidence; a hostile,
drifted, incomplete, or stale path fails closed before publication or becomes an immutable incident.
GOV-1 is complete only when the canonical split implementation gate independently authenticates that
behavior and every owner acceptance condition is closed.

## Open Questions

- Have Product Owner and Release Owner accepted AD-19, the production-release halt, and the current no-exception posture?
- Has a Release Owner or repository administrator set `HEXALITH_RELEASE_PUBLISH_ENABLED` to literal `false` and captured an authenticated API observation?
- Which immutable Hexalith.Builds successor implementing `split-publication-v1` is accepted into the AD-16 lineage?
- What measurable incident acknowledgement and containment target do Product Owner and Release Owner approve, and when will the runbook be approved and exercised?
- When will the Release Owner establish and verify the no-bypass protected-main ruleset required for conformance and ledger approvals?
