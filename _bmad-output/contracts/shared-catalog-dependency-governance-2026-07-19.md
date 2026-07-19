---
id: FC-DEP-1
title: Shared Catalog Compatibility and Dependency Provenance
status: approved
date: 2026-07-19
approvedBy: Administrator
owners:
  - Product Owner
  - Architect
  - Release Owner
implementationStory: GOV-1
upstreamFollowUp: BUILD-CAT-1
---

# FC-DEP-1: Shared Catalog Compatibility and Dependency Provenance

## Context

FrontComposer Governance tests hard-coded selected historical `Hexalith.Builds` commits. Legitimate
root or nested gitlink advances therefore failed until the expected SHA constants were mechanically
updated, even when every required package and build contract remained compatible. At the same time,
release evidence recorded the FrontComposer commit but not the complete reachable gitlink graph.

Compatibility and provenance are different concerns. A commit SHA precisely identifies inputs but
does not, by itself, state whether a catalog satisfies a consumer contract.

## Decision

1. Compatibility is validated from the shared catalog selected by each actual reachable Builds
   gitlink. Governance loads `Props/Directory.Packages.props` from that exact commit and validates the
   applicable semantic package/import/marker contract.
2. Product Governance tests contain no expected 40-hex submodule SHA allowlist. A compatible catalog
   at a different commit passes; an incompatible catalog fails with owner path, actual commit, and
   semantic mismatch.
3. Root and nested pointer changes produce a deterministic base-to-head dependency-graph diff and run
   the affected module's supported standalone restore/build gate.
4. The sealed release manifest records and verifies the complete reachable gitlink graph. Each edge
   includes normalized repository identity, owner/path, commit, and depth. Builds edges additionally
   include semantic catalog-contract version when available and catalog SHA-256 fingerprint.
5. Traversal is keyed by repository identity plus commit, terminates cycles, reads committed Git trees
   or targeted explicit checkouts, and never recursively initializes nested submodules or moves their
   working-tree HEADs.
6. Hexalith.Builds owns BUILD-CAT-1: introduce a semantic catalog-contract version and canonicalization
   rules. Until supported gitlinks migrate, FrontComposer validates semantic contents directly and
   records fingerprints only as provenance. Making the version marker mandatory requires separate
   approval.

## Consequences

- Pointer advances stop causing false-red compatibility failures solely because their SHA changed.
- Governance broadens from a historical subset to every selected Builds catalog in the reachable
  graph.
- Release evidence becomes reproducible from explicit dependency identities rather than relying on
  implicit Git checkout state.
- The release manifest schema, producer, verifier, fixtures, fallback invalidation, and
  post-publication verifier must change atomically.
- CI gains targeted graph-diff and affected-module cost only when pointers change.
- BUILD-CAT-1 is external coordination and does not authorize editing `references/Hexalith.Builds`
  from the FrontComposer repository.

## Rejected Alternatives

- Keep exact SHA constants: conflates identity with compatibility and requires mechanical churn.
- Remove submodule governance: fails closed neither on incompatible catalogs nor unreviewed drift.
- Use an exact fingerprint allowlist: replaces one identity allowlist with another.
- Roll back current pointers: restores historical identity without proving compatibility.

## Verification

- A different compatible Builds commit passes semantic Governance.
- A catalog with a missing or wrong required value fails with actionable edge diagnostics.
- A pointer change emits the graph diff and runs the affected-module gate.
- Manifest verification fails for missing, duplicate, malformed, non-terminating, or drifted graph
  evidence.
- Pre- and post-publication verification bind the same sealed graph.
- No recursive submodule initialization command is introduced.
