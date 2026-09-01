---
title: 'Harden package audit provenance, family refresh, and catalog BOM'
type: 'bugfix'
created: '2026-09-01'
status: 'done'
baseline_revision: '9d9a6295f8cbf49565cdc521fcbe37b9b937da84'
baseline_commit: '9d9a6295f8cbf49565cdc521fcbe37b9b937da84'
review_loop_iteration: 0
followup_review_recommended: true
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

## File List

- `_bmad-output/implementation-artifacts/spec-package-audit-provenance-bom.md` -- implementation contract, review evidence, and auto-run result.
- `references/Hexalith.Builds` -- owning submodule pointer for the package-audit, catalog-validation, fixture, evidence, and documentation changes.

## Commit Scope Dispositions

- `06b058f831b799f70a0adfcb4a36afd03fd9bcc8` | `shared` | External orchestration grouped this bundle's spec and Builds pointer with an independently completed EventStore submodule pointer update while verification was running.

## Documented Unrelated Changes

- `references/Hexalith.EventStore` - Exact-path legacy-gate classification for the independently completed pointer update documented by the full-SHA shared disposition above; this bundle did not modify or review EventStore.

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

### 2026-09-01 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 24: (high 6, medium 16, low 2)
- defer: 0
- reject: 3: (high 0, medium 1, low 2)
- addressed_findings:
  - `[high]` `[patch]` Compared repository-owned catalog and consumer declarations to the claimed revision, with staged-change fixtures.
  - `[high]` `[patch]` Validated preserved v2 evidence and recomputed every family-local fingerprint before copying it.
  - `[medium]` `[patch]` Included per-source diagnostics in package metadata fingerprints and covered diagnostic drift.
  - `[medium]` `[patch]` Bounded generator Git-blob reads by time and bytes, with oversized-blob coverage.
  - `[medium]` `[patch]` Made audit output replacement atomic so the prior/default audit survives failed generation.
  - `[medium]` `[patch]` Required `schemaVersion` to be the JSON integer `2` rather than a coercible string.
  - `[high]` `[patch]` Bound refreshed-family origins to the current snapshot revision and time while preserving prior origins for preserved families.
  - `[medium]` `[patch]` Rejected incremental snapshots with an empty refreshed-family partition.
  - `[high]` `[patch]` Required current and historical origin revisions to exist and be ancestors of the generated-from revision.
  - `[medium]` `[patch]` Enforced UTC origin chronology relative to the snapshot.
  - `[medium]` `[patch]` Enforced closed shapes, exact JSON types, and absolute URIs for source records.
  - `[medium]` `[patch]` Enforced closed shapes and exact JSON types for consumer-evidence envelopes and entries.
  - `[medium]` `[patch]` Validated v2 historical origin timestamp, revision, and hash formats.
  - `[medium]` `[patch]` Validated historical source-result values and configured-source coverage.
  - `[medium]` `[patch]` Rejected duplicate family and package history records.
  - `[medium]` `[patch]` Required explicit consumer fixtures to remain beneath the audit directory.
  - `[high]` `[patch]` Made owned process-tree termination and exit waits fail closed.
  - `[high]` `[patch]` Added an atomic readiness handshake and three timeout-cleanup repetitions.
  - `[medium]` `[patch]` Made malformed or nonnumeric ready/PID records controlled fixture failures.
  - `[medium]` `[patch]` Enforced ordinal family-selector identity and rejected case variants.
  - `[medium]` `[patch]` Deduplicated history using complete origin identity.
  - `[medium]` `[patch]` Preserved JSON nulls in historical stable/prerelease fields.
  - `[low]` `[patch]` Proved incremental generation contacts only requested-family package endpoints and rejects unrequested drift before package requests.
  - `[low]` `[patch]` Added package-history deep-equality and cardinality assertions for repeated identical refreshes.

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

## Auto Run Result

Implemented audit schema v2 with exact committed-catalog provenance, complete/incremental family partitions, targeted refreshes, family-local origins and fingerprints, bounded and deduplicated histories, a strict UTF-8 BOM gate, and deterministic Git-shim cleanup. Reconciled the production audit without changing a package selection: 4 families are refreshed, 137 are preserved, and only `hexalith-eventstore` gains history.

Files changed:

- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- hardened generation, preservation, bounded Git reads, incremental request scope, history, and atomic output.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` -- independently validates exact provenance, typed closed shapes, origins, partitions, histories, containment, and process cleanup.
- `references/Hexalith.Builds/Tools/test-package-version-audit-generator.ps1` -- expanded complete/incremental and hostile generator coverage to 96 scenarios.
- `references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1` -- expanded hostile validation and repeated timeout cleanup coverage to 101 scenarios.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` -- enforces the UTF-8 BOM and strict UTF-8 decoding before XML evaluation.
- `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- covers BOM-bearing, BOM-free, truncated, invalid-UTF-8, and semantic catalogs in 17 scenarios.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- records the reconciled 286-package, 141-family audit with exact HEAD blob identity.
- `references/Hexalith.Builds/Tools/README.md` -- documents the encoding, provenance, refresh, origin, history, and operational contracts.
- `_bmad-output/implementation-artifacts/spec-package-audit-provenance-bom.md` -- records the implementation contract, commit scope, review triage, verification, and result.

Review findings: 24 patches applied, 0 items deferred, and 3 items rejected. Rejected claims were direct worktree/raw-blob equality despite required Git EOL normalization, restoration of unsupported schema-less legacy histories despite the typed fail-closed contract, and a broader CycloneDX implementation reading not grounded in any of the five authoritative ledger entries. Follow-up review is recommended: patched findings were high 6, medium 16, low 2; score `3 × 16 + 2 = 50`, and high-severity patches were present.

Verification performed:

- Central catalog validation passed for 286 entries; central validator fixtures passed 17 scenarios.
- Authoritative catalog validation passed for 50 identities and 3 shared versions.
- Generator fixtures passed 96 scenarios; validator fixtures passed 101 scenarios, including three timeout-cleanup repetitions.
- Production audit validation passed for 286 packages, 141 families, and 1 source.
- Release build passed with 0 warnings and 0 errors; six PowerShell scripts parsed cleanly.
- Exact audit raw SHA equals the HEAD catalog blob SHA; snapshot mode is incremental with 4 refreshed and 137 preserved families.
- Relative to the first v2 audit, only `hexalith-eventstore` gained family history (16 to 17) and its 13 package histories gained one entry; all preserved decisions are otherwise unchanged apart from the diagnostic-bound metadata hash migration.
- `git diff --check` passed; the catalog, deferred ledger, CycloneDX paths, and both Git indexes remained unchanged.

Residual risk: package-feed observations are point-in-time evidence. Deterministic fixtures do not perform live NuGet queries; the checked-in production audit remains subject to its recorded observation time and source diagnostics.
