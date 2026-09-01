---
title: 'Harden package audit provenance, family refresh, and catalog BOM'
type: 'bugfix'
created: '2026-09-01'
status: 'in-progress'
baseline_revision: '9d9a6295f8cbf49565cdc521fcbe37b9b937da84'
baseline_commit: '9d9a6295f8cbf49565cdc521fcbe37b9b937da84'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The Builds package audit can claim a revision that does not reproduce its catalog, describes mixed-age incremental evidence as one live snapshot, and appends history for every family after any catalog-byte change. Builds also lacks an executable UTF-8 BOM gate, while the validator timeout fixture races its own PID capture.

**Approach:** Evolve the shared audit contract to bind an exact committed catalog and an explicit complete/incremental refresh partition, preserve unaffected families using family-local fingerprints and origin metadata, enforce the catalog BOM in the existing central validator, and make the Git-shim cleanup test deterministic.

## Boundaries & Constraints

**Always:** Work in `references/Hexalith.Builds`; keep `Props/Directory.Packages.props` byte-identical; retain fail-closed exact Git-object validation, complete catalog coverage, family-aligned decisions, typed history, bounded blob reads, and CI/release ordering. A production audit must identify a real ancestor commit whose raw catalog blob and tracked consumer declarations match the recorded evidence. Reconcile the checked-in audit to the current committed catalog after the contract change.

**Block If:** The claimed catalog commit or required Git blobs are unavailable; the current catalog cannot be reconciled without accepting an unrequested package upgrade or changing a package version; or focused validation exposes a materially different provenance contract that cannot be resolved from the five findings.

**Never:** Edit the deferred-work ledger; stage, commit, push, branch, update a dependency, change catalog versions, weaken validation, initialize nested submodules, or modify FrontComposer's separate CycloneDX release-evidence path. CycloneDX was inspected only to confirm that none of DW-1838, DW-1839, DW-1897, DW-1898, or DW-1906 targets it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Complete audit | No family filter | Every catalog family is live-refreshed and the snapshot partition covers the catalog exactly | Reject missing, duplicate, or overlapping partition members |
| Incremental audit | Prior audit plus explicit changed families | Only requested/affected families are queried and gain history; preserved family decisions, package rows, origins, and history remain unchanged | Reject unknown/duplicate families, missing prior audit, or unrequested family drift |
| Exact provenance | Repository-owned catalog and consumers match an ancestor commit | Audit records that commit, canonical catalog hash, and exact raw catalog-blob SHA-256 | Fail before output on dirty/mismatched bytes; validator rejects unavailable, non-ancestor, or wrong-blob revisions |
| Catalog encoding | Authoritative catalog or fixture | BOM-prefixed strict UTF-8 proceeds to XML/version validation | BOM-free, truncated, or invalid UTF-8 fails with a stable catalog diagnostic |
| Timeout cleanup | Cold non-terminating Git shim with child | One atomic ready/PID record exists before the bounded read timeout; parent and child are proven gone | Fail on startup/ready timeout or surviving owned process, without racing partial PID files |

</intent-contract>

## Code Map

- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:2-9,232-252,844-1115` -- generator parameters, package-family classifier, current HEAD discovery, global-hash preservation bug, typed history append, and v1 emission. Reuse the existing family metadata/consumer fingerprints; replace the global catalog preservation key with an ordered family selection fingerprint and explicit family origin.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1:162-330,1028-1058,1462-1758` -- bounded raw Git reads, top contract, committed catalog/consumer proof, typed history, and preservation validation. Move repository-owned catalog proof outside consumer discovery and validate the new raw-blob/snapshot/family-origin contract.
- `references/Hexalith.Builds/Tools/test-package-version-audit-generator.ps1:163-503` -- deterministic multi-source generator fixtures. Replace the whole-catalog-byte refresh expectation with two-family complete/incremental partition, untouched-family, history-delta, dirty-byte, and invalid-selection cases.
- `references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1:59-307,379-420,884-1004,1220-1261` -- schema fixtures, historical object attacks, non-terminating shim, and workflow-order guards. Extend schema/round-trip negatives and replace the two-file PID race with an atomic ready record and bounded startup margin.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1:86-101` -- existing CI/release catalog gate; enforce exact `EF BB BF` and strict UTF-8 before XML evaluation.
- `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1:14-145` -- make catalog fixtures BOM-bearing by default and add BOM-free/truncated/invalid-UTF-8 negatives.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- migrate to the hardened schema and incrementally refresh exactly the families drifted from current committed catalog; unchanged family histories must not grow.
- `references/Hexalith.Builds/Tools/README.md:73-146` -- document BOM enforcement, exact catalog provenance, complete versus incremental snapshots, family-local origins, and bounded history growth.
- `references/Hexalith.Builds/.github/workflows/ci.yml:44-66` and `references/Hexalith.Builds/.github/workflows/build-release.yml:58-80` -- read-only evidence: existing workflows already run all affected production validators and fixture suites before consumer authority/release.
- `eng/release_prepublish.py:318-329` and `eng/release_evidence.py:3331-3401` -- read-only boundary: independent CycloneDX SBOM generation/hash binding is not a ledger finding in this bundle.

## Tasks & Acceptance

**Execution:**
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- emit schema v2 with explicit `complete`/`incremental` refresh partitions, exact catalog-blob provenance, family-local observation origin/fingerprints, targeted querying, unchanged-family preservation, and deduplicated changed-family history -- make mixed-age evidence truthful and stop catalog-wide growth.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` -- independently validate every new closed-shape invariant and exact committed object, including repository-owned catalogs used with explicit consumer fixtures -- bind claims to reproducible bytes.
- `references/Hexalith.Builds/Tools/test-package-version-audit-generator.ps1` and `references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1` -- add complete/incremental, provenance, cardinality, hostile-shape, and deterministic process-cleanup coverage -- prove DW-1838, DW-1897, DW-1898, and DW-1906.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` and `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- enforce and test the UTF-8 BOM/strict-decoding contract -- close DW-1839 through the existing release gate.
- `references/Hexalith.Builds/Tools/package-version-audit.json` and `references/Hexalith.Builds/Tools/README.md` -- reconcile current evidence and document the operator contract without changing selections -- leave production validation green and usage unambiguous.

**Acceptance Criteria:**
- Given a committed catalog and a prior two-family audit, when one selector changes and that family is incrementally refreshed, then the changed family alone gains one deduplicated family snapshot plus its package snapshots, while the other family remains deep-equal and its history cardinality is unchanged.
- Given complete or incremental output, when deterministic validation runs, then its exact catalog commit/raw blob, canonical hash, snapshot partition, family origins, catalog coverage, consumer bytes, and typed histories all validate fail-closed.
- Given dirty catalog/consumer bytes, a nonexistent/non-ancestor revision, raw-blob drift hidden by BOM/EOL normalization, or an unrequested changed family, when generation or validation runs, then it fails before accepting or writing misleading evidence.
- Given a BOM-free central catalog, when the central validator runs in CI/release shape, then it fails before evaluation; the unchanged authoritative BOM+CRLF catalog and all prior semantic fixtures pass.
- Given repeated non-terminating Git-shim scenarios, when the validator times out, then the atomic ready record identifies both owned PIDs and both processes are terminated without intermittent missing-PID failures.
- Given the reconciled production audit, when all focused catalog/audit gates and the Builds Release build run, then they pass without changing any package selection or FrontComposer CycloneDX artifact contract.

## Spec Change Log

## Review Triage Log

## Design Notes

Keep the existing normalized `catalogSha256` for semantic equality, add an exact raw blob SHA-256 for reproducibility, and make `generatedFromRevision` mean the catalog/consumer commit. The snapshot envelope owns the current run time and exact refreshed/preserved family partition; each family owns the origin revision/time and family-selection/source/package/consumer fingerprints for its current evidence. Git already retains prior whole artifacts, so history records only genuine family refreshes and deduplicates identical prior snapshots.

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1` -- expected: current 286-entry BOM-bearing catalog passes.
- `pwsh -NoProfile -File ./Tools/test-central-package-version-validator.ps1` -- expected: all BOM and semantic fixtures pass.
- `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` -- expected: authoritative identities and shared selectors pass unchanged.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1` -- expected: complete/incremental and bounded-history scenarios pass without live feeds.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1` -- expected: provenance, schema, process cleanup, and workflow guards pass repeatedly.
- `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` -- expected: reconciled production audit passes for the current catalog.
- `dotnet build Hexalith.Builds.slnx --configuration Release` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors; `Props/Directory.Packages.props` has no diff and the deferred-work ledger is untouched.
