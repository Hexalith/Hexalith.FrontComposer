# Adversarial Divergence Review — GOV-1 Update, Pass 5

- Review date: 2026-09-09
- Lens: adversarial divergence, focused final recheck
- Artifact: `ARCHITECTURE-SPINE.md`
- Spine SHA-256: `4a765adbf2026396d9d1258163a41f155522d8c3a7304d6d6857533a5a37624f`
- Deterministic lint: **PASS**, 0 findings
- Verdict: **PASS — 0 Critical, 0 High**

This pass re-tested ADV4-H1–H3 and their immediate trust boundaries only. All three are closed, and
the fixes introduce no new Critical or High implementer divergence.

## Prior-finding closure

| Finding | Status | Adversarial result |
| --- | --- | --- |
| ADV4-H1 — fallback approval replay across rerun attempts | Closed | AD-9 requires the exact request digest, current Release run ID and attempt in the authenticated `production` review comment; the derived authorization must byte-match those authenticated current coordinates. A retry requires a new request and deployment approval, and the negative fixture expressly denies attempt-1 approval reuse for attempt 2. Duplicate and unequal approval projections fail closed. |
| ADV4-H2 — missing conformance equality map and hostile base | Closed | The implementation gate now equates ordinary producer heads, handoff candidates, policy identity, selected Builds commit, caller blob, Release/verification coordinates, artifact ownership, and committed output bytes. The hostile run must be the sole-child fixture delta of `frontcomposer_commit`, with exact tree/file hashes and path confinement. Mixed-coordinate negative packets are mandatory. |
| ADV4-H3 — unauthenticated reviewer PASS and packet approval | Closed | Every review has a duplicate-rejecting sidecar whose canonical header is embedded in the report, bound to the report digest, authenticated run/artifact sources, spine, FrontComposer/Builds/policy identities, and zero-finding PASS. The packet PR uses an exact canonical approval body, authenticated API reviewer identity from `release_owner_logins`, unchanged reviewed head, and protected non-squash merge; a distinct protected PR durably projects the authenticated review identity before the gate can pass. |

## Focused red-team result

### Critical

None.

### High

None.

- **Run-bound fallback:** the approval cannot move `main`, authorization is bound to the current
  Release attempt, unavailable/ambiguous API data fails closed, and the retry falsifier is explicit.
- **Incident recovery:** `incident_recovery` remains candidate-free and separately approved, has only
  reserved-namespace Release evidence authority, and is statically denied NuGet, OIDC, product-tag,
  candidate-code, and broader permission paths.
- **GitHub provenance and path semantics:** case-preserved GitHub/OIDC fields, exact signed predicate
  locations, invocation attempt, and GitHub-hosted `ubuntu-24.04` execution remove the prior identity
  and cross-platform interpretation forks.
- **Multi-owner signoff:** signer- and review-qualified paths, per-observation/per-owner uniqueness,
  authenticated two-PR projection, and append-only ordering prevent owner collisions or replacement.
- **Conformance packet:** authenticated raw outputs and machine review sidecars are transitively tied
  to one implementation, policy, caller, Builds evaluator, and hostile-base delta; the separate
  approval record closes the packet self-approval gap.

## Gate disposition

The configured adversarial-divergence lens may report PASS at the reviewed hash. This focused pass
does not reclassify the spine's separately documented operational release-gate condition and did not
seek or carry lower-severity editorial improvements.
