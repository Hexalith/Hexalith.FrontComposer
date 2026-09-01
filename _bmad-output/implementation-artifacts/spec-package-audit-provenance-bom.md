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
deferred:
  - summary: >-
      The generated audit artifact is written with platform line endings and no
      .gitattributes rule pins them, so regenerating on Windows rewrites all
      ~170k lines.
    evidence: |-
      Tools/audit-central-package-versions.ps1 terminates the document with
      [Environment]::NewLine and ConvertTo-Json indents with the platform newline;
      Tools/.gitattributes pins only Props/Directory.Packages.props. The committed
      Tools/package-version-audit.json contains zero CR bytes because it was last
      generated on Linux. Pre-existing: the prior Set-Content -Encoding utf8 write
      had the same platform dependence, so this change did not introduce it.
    location: >-
      references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:1756
    severity: medium
  - summary: >-
      Audit history growth has no retention, pruning, or size policy, and the
      artifact is already ~9.9 MB.
    evidence: |-
      Every refreshed family appends family and package historicalContext records
      on each incremental run, and the validator round-trips each record for
      duplicate detection, so both file size and validation cost grow without
      bound. Pre-existing: the v1 contract also appended history, and this change
      only narrowed which families append.
    location: >-
      references/Hexalith.Builds/Tools/package-version-audit.json
    severity: medium
  - summary: >-
      Almost all stored package history is still v1-schema and is therefore exempt
      from the new origin ancestry, chronology, and duplicate-identity invariants.
    evidence: |-
      The v2 contract accepts both hexalith.package-audit-*-history.v1 and .v2
      records, but only v2 records carry an origin, and Assert-HistoricalOrigin
      only validates records that have one. The vast majority of committed package
      history predates v2, so the new integrity guarantees cover only the records
      written since this change. Pre-existing data, not introduced by it.
    location: >-
      references/Hexalith.Builds/Tools/validate-package-version-audit.ps1
    severity: low
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
- `_bmad-output/implementation-artifacts/deferred-work.md` - Uncommitted orchestrator sweep bookkeeping that closes the five ledger entries this bundle resolved; the ledger is owned by the orchestrator and this bundle neither edited nor re-opened any entry.

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

### 2026-09-01 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 8: (high 2, medium 5, low 1)
- defer: 3: (high 0, medium 2, low 1)
- reject: 14: (high 0, medium 6, low 8)
- addressed_findings:
  - `[high]` `[patch]` Consumer `declarationSha256` binds committed blob bytes, but the validator additionally required the worktree file to hash to that same value, so any EOL-normalizing checkout (Windows `core.autocrlf=true`, or a `text` attribute on `*.csproj`) made the shipped audit unvalidatable. The consumer leg now proves the worktree against the audited revision the way Git compares tracked content, mirroring the two-branch treatment the catalog leg already had, and keeps the raw-byte comparison only when the revision cannot supply the blob.
  - `[high]` `[patch]` The rewritten timeout-cleanup fixture replaced the intermittent PID-capture failure with an intermittent wall-clock failure on the same scenario: it failed when total elapsed reached 20 s, a stopwatch spanning pwsh cold start, up to 15 s of readiness polling, and the 250 ms margin. It was observed failing at 20.37 s under load during this pass and passing on re-run. The measured window now starts at the readiness handshake and both it and the `WaitForExit` bound derive from `$GitBlobReadTimeoutSeconds`.
  - `[medium]` `[patch]` Added the missing repository-owned negative fixture for the `catalogRawSha256` committed-blob comparison; every prior validator fixture placed its catalog outside the repository, so only the worktree branch was exercised and the repo-owned branch that guards the shipped artifact in CI could have been deleted with the suite still green.
  - `[medium]` `[patch]` The repository-owned generator fixture's `eol=crlf` setup was inert -- `checkout-index` does not overwrite existing files and the fixture files had no trailing line ending -- and the fixture never validated the audit it generated. It now removes the worktree copies before checkout, asserts the normalized CRLF bytes, and runs the validator on its own output, which is the regression test for the finding above.
  - `[medium]` `[patch]` An incremental refresh over a pre-v2 prior audit skipped every closed-shape prior assertion and copied unvalidated rows verbatim into a v2 document. It now fails closed and directs the operator to a complete refresh, with a fixture proving no output is written.
  - `[medium]` `[patch]` Gave the generator's bounded Git-blob reader the same shim seam the validator already had and added a timeout scenario that asserts the diagnostic, process-tree termination of both owned processes, and atomic preservation of the prior output; previously only its byte bound was exercised.
  - `[medium]` `[patch]` Made the generator's blob read fail closed when `Process.Start` reports failure without throwing; it previously returned zero bytes with no failure, and those bytes would have been hashed into `catalogRawSha256`/`declarationSha256`. The validator's copy already failed closed here.
  - `[low]` `[patch]` Exposed `-PriorAuditPath` as documented operator surface rather than `DontShow`, documented the blob-read time and size bounds and the v2-prior requirement, and repaired the ragged mid-sentence wraps in the rewritten README paragraphs.

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

Implemented audit schema v2 with exact committed-catalog provenance, complete/incremental family partitions, targeted refreshes, family-local origins and fingerprints, bounded and deduplicated histories, a strict UTF-8 BOM gate, and deterministic Git-shim cleanup. Reconciled the production audit without changing a package selection: 4 families are refreshed, 137 are preserved, and only `hexalith-eventstore` gains history. A follow-up review pass then hardened the provenance contract's portability, its process bounds, and the fixtures that were supposed to prove them.

Files changed:

- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- hardened generation, preservation, bounded Git reads, incremental request scope, history, and atomic output; incremental refresh now requires a v2 prior, the blob reader fails closed when it cannot start, and it accepts the same test shim seam the validator has.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` -- independently validates exact provenance, typed closed shapes, origins, partitions, histories, containment, and process cleanup; consumer declarations are now proved against the audited revision the way Git compares tracked content instead of by raw worktree bytes.
- `references/Hexalith.Builds/Tools/test-package-version-audit-generator.ps1` -- complete/incremental and hostile generator coverage, now 102 scenarios: the repository fixture genuinely diverges worktree bytes from committed blobs and validates its own audit, and the bounded reader's timeout and process-tree cleanup are covered.
- `references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1` -- hostile validation and repeated timeout cleanup coverage, now 102 scenarios: adds the repository-owned raw-catalog-blob negative and measures the bounded window from the readiness handshake rather than from harness start.
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` -- enforces the UTF-8 BOM and strict UTF-8 decoding before XML evaluation.
- `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- covers BOM-bearing, BOM-free, truncated, invalid-UTF-8, and semantic catalogs in 17 scenarios.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- records the reconciled 286-package, 141-family audit with exact HEAD blob identity; unchanged by the follow-up pass.
- `references/Hexalith.Builds/Tools/README.md` -- documents the encoding, provenance, refresh, origin, history, and operational contracts, including the normalization-aware consumer proof, the v2-prior requirement, and the blob-read bounds.
- `_bmad-output/implementation-artifacts/spec-package-audit-provenance-bom.md` -- records the implementation contract, commit scope, review triage, verification, and result.

Review findings across both passes: 32 patches applied (24 in the first pass, 8 in the follow-up), 3 items deferred, and 17 items rejected. The follow-up pass deferred the artifact's platform-dependent line endings, its unbounded history growth, and the pre-v2 history records that the new origin invariants cannot cover -- all three pre-existing rather than caused by this change. Rejected claims included the two original passes' direct worktree/raw-blob equality and schema-less legacy history restorations, a broader CycloneDX reading not grounded in any of the five ledger entries, and follow-up noise such as the `-Family` parameter naming (an `Alias` already provides it), a SHA-256 object-format assumption, missing `#Requires` directives, and deep-clone performance. Follow-up review is recommended: this pass's patched findings were high 2, medium 5, low 1; score `3 x 5 + 1 = 16`, and high-severity patches were present.

Verification performed (all commands run from `references/Hexalith.Builds` after the follow-up patches):

- `validate-central-package-versions.ps1` passed for 286 entries; `test-central-package-version-validator.ps1` passed 17 scenarios.
- `test-authoritative-package-catalog.ps1` passed for 50 approved identities and 3 shared versions.
- `test-package-version-audit-generator.ps1` passed 102 scenarios, including the new normalizing-checkout validation, the pre-v2 incremental rejection, and the generator timeout/process-cleanup scenario.
- `test-package-version-audit-validator.ps1` passed 102 scenarios in 458 s, including the repository-owned raw-blob negative and three timeout-cleanup repetitions.
- `validate-package-version-audit.ps1` passed for 286 packages, 141 families, and 1 source against the unchanged production audit.
- `dotnet build Hexalith.Builds.slnx --configuration Release` succeeded with 0 warnings and 0 errors.
- `git diff --check` reported no whitespace errors; `Props/Directory.Packages.props`, `Tools/package-version-audit.json`, and the deferred-work ledger were not modified by this pass.

Residual risks:

- Package-feed observations remain point-in-time evidence. Deterministic fixtures perform no live NuGet queries, so the checked-in audit stays bound to its recorded observation time and source diagnostics.
- 137 of the 141 families in the shipped audit carry origins migrated from the v1 envelope rather than fresh v2 observations. This is the intent's incremental reading and is now truthfully labeled, but a complete refresh is still what would make the shipped snapshot single-aged.
- The consumer-declaration and repository-owned catalog paths are now covered by fixtures on Linux only; CI runs `ubuntu-latest`, so a Windows `core.autocrlf` checkout is proved by construction rather than by a CI lane.
