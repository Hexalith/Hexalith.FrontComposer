# GOV-1 Final Input Reconciliation — PRD

**Input:** `_bmad-output/planning-artifacts/prd.md`  
**Target:** ratified GOV-1 architecture spine  
**Verdict:** changes required before final handoff. Four clauses still say "complete reachable" and the
PRD does not yet bind the ratified closed policy, bounded extraction, authenticated evaluator handoff,
or manifest-v2 workflow provenance.

Line numbers below refer to the 2026-07-19 PRD revision inspected for this reconciliation.

## Required Changes

### 1. Requirement status map — line 107

Replace the FR-24 gate summary so `manifest-bound` is not artifact-only:

> **FR-24 — Release governance / publication gate:** Release Owner proves the exact NuGet/GitHub
> artifacts were inventory- and consumer-validated, signed, timestamped, verified, checksummed, and
> bound with the complete defined dependency graph, immutable dependency policy, and authenticated
> CI/release workflow provenance in a valid sealed v2 manifest before classification and publication.

### 2. FR-24 consequences — line 367

Replace the old complete-reachable sentence with:

> The required `hexalith.release-evidence.v2` manifest seals the complete defined
> `hexalith.dependency-graph.v1`: every gitlink at the explicit FrontComposer commit (depth 1) and every
> gitlink in each exact root-selected repository commit (depth 2), with no deeper v1 edges. It also
> seals every selected Builds commit and raw catalog SHA-256/optional contract marker, the immutable
> dependency-policy coordinates and digest, and canonical authenticated CI/release workflow
> provenance. Legacy manifests are audit-only and never publishable or fallback-eligible.

This is the primary replacement for the PRD's old "complete reachable root and nested" product
requirement.

### 3. NFR-12 and NFR-13 — lines 436–437

Replace both bullets with the closed release and bounded dependency contracts:

> **NFR-12 Release evidence:** signed and timestamped NuGet packages, symbols, SBOM, exact package
> inventory, consumer validation, checksums, a valid sealed `hexalith.release-evidence.v2` manifest,
> and `publish_authorized=true` are blocking pre-publication requirements. The manifest binds the exact
> published bytes, complete defined v1 dependency graph, immutable dependency policy, authenticated
> CI run/handoff, and immutable CI/release evaluator definitions. Its fallback digest binds the graph,
> policy, and canonical workflow-definition digest.
>
> **NFR-13 Dependency governance:** compatibility is established from versioned semantic
> shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence, never
> historical commit or fingerprint allowlists. `hexalith.dependency-graph.v1` is exactly depth 1–2 as
> defined in FR-24. Collection reads exact committed objects under the immutable base/before policy,
> emits deterministic graph diffs, never recursively initializes nested submodules, and fails closed
> above 4,096 edges, 64 MiB raw `ls-tree` output per owner commit, 1 MiB per committed `.gitmodules`
> blob, or 4 MiB per catalog blob. A materialized Builds contract tree permits only bounded regular
> files: at most 16,384 files, 16 MiB per blob, and 256 MiB total; unsafe paths/modes and exceeded limits
> fail before extraction.

### 4. Dependency constraint — line 444

Replace the current open-ended dependency-policy bullet with:

> **Dependency policy:** one closed, versioned FrontComposer-owned policy defines trusted repository
> identities/paths, semantic profiles, affected-module argv/evidence-only dispositions, and limits.
> PR evaluation uses the exact base-commit policy and push evaluation the exact non-zero before-commit
> policy for both graphs; candidate policy changes cannot authorize themselves and activate only from
> a later base, apart from the one-time frozen, digest-approved bootstrap. Missing/unknown mappings,
> profiles, commands, objects, or selected catalogs fail closed. Exact commits and raw fingerprints are
> provenance, not compatibility allowlists.

### 5. Success metric SM-2a — line 484

Replace the complete-reachable metric with:

> **SM-2a: Dependency provenance** — every publish-capable release seals and live-verifies the complete
> defined depth-1/2 `hexalith.dependency-graph.v1`, immutable active policy, and authenticated immutable
> CI/release workflow provenance. Compatible pointer advances pass semantic-profile and affected-module
> gates without product-test commit/fingerprint allowlist edits; malformed, incomplete, drifted,
> over-limit, or legacy evidence cannot authorize publication. Validates FR-24, NFR-12, and NFR-13.

### 6. Risk mitigations — lines 505–506

Replace the two mitigation clauses so the PRD covers the ratified handoff and defined boundary:

> **Risk: Workflow success is confused with release readiness.** Mitigation: authenticate the exact
> successful main-push CI run/head and its single canonical dependency handoff; require 40-hex-pinned CI
> and release reusable workflows plus their transitive action closure; seal their canonical definition
> digest; include it in fallback invalidation; and fail before publication on any metadata, provenance,
> manifest, or classification mismatch.
>
> **Risk: Gitlink identity is confused with shared-catalog compatibility.** Mitigation: validate every
> Builds selector inside the complete defined depth-1/2 v1 graph against its closed semantic profile,
> graph-diff pointer changes, run each statically mapped affected module at most once with the exact
> bounded Builds contract tree, and seal exact graph/catalog identities solely as provenance.

### 7. Decision register D-11 — line 534

Replace the old reachable-identity decision summary with:

> **Resolved and ratified 2026-07-19:** compatibility is semantic-profile plus affected-module
> Release/NuGet proof; provenance is the exact complete defined depth-1/2
> `hexalith.dependency-graph.v1`. The sealed v2 manifest additionally binds the immutable active policy,
> authenticated CI handoff, and immutable CI/release evaluator/action definition digest. Collection and
> Builds-tree extraction use the ratified fail-closed ceilings; exact commits and catalog fingerprints
> never become compatibility allowlists. Contract:
> `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`; spine:
> `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.

Keep the existing owner and blocking columns.

## Terminology Sweep

After these replacements, a case-insensitive PRD search for `complete reachable`, `every reachable`,
and `exact reachable` must return no GOV-1 requirement. Use consistently:

- **complete defined v1 graph** for the evidence boundary;
- **depth 1 root gitlinks + depth 2 direct gitlinks from exact root-selected commits** for its expansion;
- **semantic compatibility** versus **exact provenance** for the policy distinction;
- **immutable active base/before policy** for trust/profile/command selection; and
- **authenticated handoff / immutable workflow-definition provenance** for the CI-to-release seam.

Lines 443 and 476 remain compatible as written: root-declared repository policy and the prohibition on
recursive/nested submodule management still apply.

