# Reviewer Gate — Rubric Walker (UPDATE) — GOV-1 Architecture Spine

## Review basis

| Field | Value |
| --- | --- |
| Target | `ARCHITECTURE-SPINE.md` — GOV-1 dependency provenance |
| Spine SHA-256 | `95cbe2f2754ad3c1d7c896e73822b49edb676c0f5921c4f05fded82519c6e6e8` |
| Status / date | `draft` / `updated: 2026-09-09` |
| Altitude | Epic — GOV-1 |
| Lens | Complete Good-spine checklist in `references/reviewer-gate.md` |
| Mechanical gate | `lint_spine.py`: `ok: true`, 0 findings |

The complete spine was reviewed as a target-state build substrate. The memlog was consulted only to
distinguish ratified decisions, corrected append-only history, and intentionally open external gates.
Existing implementation drift was not promoted into an architecture finding when the spine already
names it and keeps release ineligible.

## Verdict

**FAIL — two High convergence defects remain.** The bounded graph, exact-candidate trust chain,
privilege split, manifest v4 migration, post-release authorization, incident handling, and operational
envelope are unusually complete. The gate still fails because the external reusable contract permits
two incompatible exposure models while another AD pins only one, and the ledger classifier gives a
protected pre-publication authorization failure both incident and non-incident meanings. One Medium
sign-off identity gap and one Low stale cross-reference remain in the tail.

## Finding counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 2 |
| Medium | 1 |
| Low | 1 |
| **Total** | **4** |

## Critical findings

None.

## High findings

### RU-2026-09-09-01 — protected pre-publication authorization failure has two dispositions

- **Severity:** High
- **AD refs:** AD-15
- **Checklist impact:** real divergence points; enforceable Rule; state ownership/mutation.
- **Finding:** the disposition table assigns a gate-true protected pre-publication authorization failure
  with no external bytes to non-incident `rejected-before-publication`. The stated fail-closed precedence
  then assigns *any authorization failure* to incident-bearing `non-compliant` before a non-incident row
  may match. Two literal implementations can therefore emit different durable records for the same
  failed attestation/fallback/final-classification attempt. One leaves the attempt safely rejected; the
  other triggers permanent incident dominance, containment, and Release Owner gate-freeze obligations.
- **Evidence:** AD-15's table (`rejected-before-publication`) and the precedence paragraph immediately
  below it; the `incident` paragraph makes those two outcomes operationally different.
- **Disposition:** **autofix.** Define a named class of expected pre-publication denial that reaches
  `rejected-before-publication` before the generic incident branch, or remove protected authorization
  failure from that row and make it unambiguously `non-compliant`. Add separate fixtures for attestation
  unavailable/failure, fallback absent/invalid, offline/live verify failure, and builder failure.

### RU-2026-09-09-02 — selected reusable path conflicts with the permitted exposure model

- **Severity:** High
- **AD refs:** AD-13, AD-17, AD-19
- **Checklist impact:** real divergence points; enforceable Rule; external ownership boundary.
- **Finding:** AD-17 fixes the Release reusable identity to
  `.github/workflows/domain-release.yml`, and AD-13 requires that selected reusable to implement
  `split-publication-v1`. AD-19 nevertheless permits the contract to be either a default-off opt-in mode
  in the existing reusable *or a sibling reusable*. A sibling selected directly by FrontComposer has a
  different workflow path and cannot satisfy AD-17. Conversely, independently built Builds and
  FrontComposer units can choose the sibling and opt-in APIs respectively while each follows one allowed
  sentence.
- **Evidence:** AD-17's exact `reusable` path; AD-19's “either an opt-in mode ... or a sibling reusable”
  rule; AD-13's active release-row match.
- **Disposition:** **discuss, then autofix.** Choose one public reusable API before external handoff. If
  the existing `domain-release.yml` remains the selected reusable, require the default-off opt-in there
  (a sibling may only be an internal literal-pin implementation detail). If the caller selects a sibling,
  amend AD-17/AD-13 to bind that one exact path and define its closed inputs/outputs.

## Medium/Low tail

### RU-2026-09-09-03 — sign-off omits the authenticated review coordinate

- **Severity:** Medium
- **AD refs:** AD-15; Deferred repository-control hardening
- **Checklist impact:** enforceable Rule; durable state ownership.
- **Finding:** `frontcomposer.release-ledger-signoff.v1` records the observation key, machine-record hash,
  decision, and signer login, but not the repository/PR/head/review identity or the owner-authority
  observation used to authenticate that signer. Its validity rule asks a verifier to discover an
  `APPROVED` review covering “the exact PR head containing the record.” Multiple PRs/reviews, a
  cherry-pick, later review dismissal, or later team membership can make independent historical
  verifiers select different authority evidence.
- **Disposition:** **autofix.** Add an exact GitHub review projection (repository, PR number, head SHA,
  review ID, review state, submitted time) and bind the Release Owner authority source and observation
  time, or define one total immutable selection algorithm with equivalent recorded coordinates.

### RU-2026-09-09-04 — AD-9 cites a Deferred item that no longer exists

- **Severity:** Low
- **AD refs:** AD-9; Deferred
- **Checklist impact:** traceability/hygiene.
- **Finding:** the `helper_version` paragraph says helper content is covered by “Deferred
  evaluator-code acceptance,” but the current Deferred section has no item by that name. The intended
  replacement appears to be the exact-closure/repository-control rule in AD-12 and Deferred
  “Repository-control hardening.”
- **Disposition:** **autofix.** Replace the stale cross-reference with the actual AD-12/AD-18/AD-19
  closure authority or the current Deferred item; do not imply that candidate helper acceptance remains
  an unstated risk exception.

## Good-spine checklist

| Checklist item | Result | Assessment |
| --- | --- | --- |
| Fixes the real divergence points for the level below and misses none | **Partial** | Graph/catalog, candidate identity, privilege split, artifacts, manifest, actors, and incident paths are strongly fixed. RU-01 and RU-02 leave two active cross-unit choices. |
| Every AD Rule is enforceable and prevents its stated divergence | **Partial** | AD-1–AD-14, AD-16, AD-18, and most of AD-15/17/19 are mechanically testable. The conflicting AD-15 branches and reusable-path alternatives prevent one accepting implementation. |
| Nothing under Deferred lets two units diverge | **Met** | Deferred controls either have a compensating exact-blob rule, require a new schema/approval, or block release/integration. Automatic incident-issue creation is non-authorizing; network/retention choices affect availability only. |
| Named technology is verified-current | **Met** | Official Git/Python/GitHub sources are named; observed and upstream Git, Python, .NET SDK, GitHub Actions, and Git object-format facts are date-labeled and non-authorizing. The gate ran on the same date as the checks. |
| Ratifies rather than silently contradicts the brownfield codebase | **Met with explicit transition debt** | The spine deliberately defines a hardened target while Adoption/Deferred halt production and enumerate the source-owner and eight implementation closure bundles. It does not claim those controls are currently implemented. |
| Covers the driving spec's capabilities | **Met with open acceptance gate** | The bounded graph, semantic compatibility, exact package/evidence set, attestation/fallback, manifest, release, verification, ledger, and incident capabilities are covered. PRD D-16/G-8 Product/Release Owner acceptance and response target remain explicitly open and release-blocking. |
| No AD weakens or contradicts an inherited parent invariant | **Met, apart from the local RU-02 inconsistency** | The split strengthens candidate/publication isolation while preserving FR-24 evidence, exact assets, tag identity, unsigned candidate packages, repository signing, pre-publication-only authorization, and ownership. Stale parent/source wording is explicitly gated for reconciliation. |
| Every epic-owned dimension is decided, deferred, or open | **Met** | Boundaries, dependency direction, trust activation, state mutation, data ownership, migration, release environments, credentials, provider fit, retention, incident operations, and no-runtime deployment scope are all addressed. |

## Confirmed-sound decisions

- AD-1–AD-8 and AD-10 remain a closed, deterministic committed-object graph contract with explicit
  boundary, identity grammar, ordering, canonical bytes, semantic/profile ownership, limits, revision
  selection, affected-build behavior, and isolated acquisition.
- AD-9, AD-12, AD-13, AD-16, and AD-18 preserve delayed activation, exact candidate/run coordinates,
  immutable fallback approval, literal source closure, accepted Builds lineage, environment protection,
  and the secretless-builder/candidate-free-publisher privilege boundary.
- AD-14/AD-17 make manifest v4 a genuine one-way migration rather than an in-place v3 extension; v3 is
  byte-preserved audit evidence, and quality/attestation projections are sealed without making quality a
  trust evaluator.
- AD-15's run/result vocabularies, phase-dependent artifact requirements, publication-start marker,
  incident monotonicity, and runbook/re-enable controls are convergent outside RU-01. A missing
  authenticated handoff remains incident-bearing and cannot green later.
- AD-19 keeps candidate source, restore/build/pack/plugins, and candidate release configuration out of
  the protected publisher. Builder evidence is deny-only; the publisher independently authenticates,
  attests/fallback-validates, seals, verifies, and classifies before package/Release mutation.
- The operational/environmental envelope is not silent: GitHub.com constraints, production protection,
  Release Owner authority, emergency stop, no-exception release halt, no-bypass prerequisite, incident
  response, artifacts versus durable evidence, and no runtime/provider expansion are all explicit.

## Gate disposition

Resolve RU-01 and RU-02 before setting the spine back to `status: final`. RU-03 is safe to bundle into
the same closed-schema edit because sign-off cannot be implemented before the no-bypass gate; RU-04 is a
mechanical traceability correction. This review did not edit the spine or any source.
