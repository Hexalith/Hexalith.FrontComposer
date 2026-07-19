# Adversarial Divergence Review — Pass 4

**Target:** Current `ARCHITECTURE-SPINE.md` and GOV-1 companions
**Date:** 2026-07-19
**Verdict:** **PASS — no unresolved Critical or High findings.**

AD-1's conflict with the previously approved complete-reachable wording is the explicitly preserved
Architect + Release Owner ratification gate. It is not counted as an architecture defect in this pass.

## Critical / High Recheck

### Affected builds — closed

- The exact static restore/build argv now carries Release configuration and `UseNuGetDeps=true` through
  both phases.
- An edge-bound module receives the bounded regular-file tree from the exact selected Builds commit,
  covering both the catalog and imported build configuration without nested initialization.
- File/blob/aggregate limits plus mode and path rejection make that extraction fail closed.
- Depth-1 classification precedes and subsumes descendant changes; an absent candidate owner collapses
  to the FrontComposer root proof instead of scheduling an impossible build.

No independently conforming runner can still choose catalog-only versus full required build inputs or
schedule a removed owner differently.

### Canonical handoff and manifest material — closed

- Graph, policy, handoff, and workflow-provenance member sets are explicit and duplicate-member-free.
- Run identifiers and attempts have exact JSON integer types and bounds.
- Repository, path, commit, blob, and digest normalization is fixed.
- Action objects have one uniqueness shape and the ordinal `(repository,path,commit)` order.
- Workflow-definition, fallback, handoff, graph, and outer-seal digest material is explicitly named.

The prior string/integer and action-order alternatives no longer satisfy the spine.

### Policy selection, bootstrap, and release reload — closed

- Ordinary PR/push evaluation uses one immutable base/before policy for both graphs, with delayed
  activation for candidate policy changes.
- Zero/unavailable-before runs cannot become release-eligible.
- Bootstrap is limited to the closed seed, unchanged graph, frozen publication, exact approved digest,
  and Release Owner-controlled repository-variable match.
- Release reloads `eng/dependency-graph-policy.json` from the recorded FrontComposer commit, validates
  its raw SHA-256 and closed schema, and uses those verified bytes rather than an ambient policy.

The authenticated CI artifact is therefore not the sole authority for the claimed policy bytes.

### Trusted CI and release evaluator provenance — closed

- The primary `domain-ci.yml` reusable workflow and release reusable workflow must be literal approved
  40-hex references.
- Runtime caller/reusable refs and SHAs are checked against the literal and sealed coordinates.
- Transitive non-local action references are immutable; Builds-owned local actions execute from the
  exact reusable-workflow checkout.
- Current `@main` evaluator inputs are explicitly non-conforming and cannot authorize the versioned
  release handoff.
- The handoff binds the exact successful push run, candidate commit, evaluator definition, active policy,
  and dependency graph; release binds the exact triggering run and revalidates the candidate equality.

An implementation cannot treat mutable primary CI as a trusted evaluator while claiming conformance.

### Semantic profiles and migration — closed

- Every Builds selector owner has exactly one profile and no default.
- Baseline structural/ownership semantics and every specialized package/import contract are normative,
  not implementation discretion.
- Legacy manifests are audit-only, non-publishable, fallback-ineligible, and never resealed or upgraded
  in place.

## Gate Recommendation

The reviewer gate passes at Critical/High severity. Proceed to the explicit AD-1 human ratification and
source-reconciliation gate; once approved, the spine is sufficiently convergent for GOV-1 development.
