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
  - summary: >-
      The validator only enables historical Git blob reads inside the
      repository-owned catalog branch, so an out-of-repository catalog combined with
      git-ls-files consumer discovery silently drops every declaration to the
      EOL-unsafe raw-byte comparison.
    evidence: |-
      $historicalBlobReadsAvailable is initialised to $false and set to $true only
      within "if ($catalogIsRepositoryOwned -and $generatedFromRevision -cmatch ...)".
      Blob-read availability actually depends on the revision being an available
      ancestor, not on catalog ownership. Hoisting that determination out of the
      catalog branch would newly subject every existing out-of-repository fixture to
      the ancestor check, so it is not a safe review-pass patch. The shipped artifact
      uses a repository-owned catalog and is unaffected today.
    location: >-
      references/Hexalith.Builds/Tools/validate-package-version-audit.ps1:1274
    severity: medium
  - summary: >-
      The emitted familyDecisions and packages arrays are ordered with culture-aware,
      case-insensitive Sort-Object while every fingerprint sorts case-sensitively.
    evidence: |-
      The document is finalised with "Sort-Object family" and "Sort-Object id", whose
      default comparison is culture-sensitive, whereas packageIds, refreshedFamilies,
      preservedFamilies and all four origin fingerprints use Sort-Object -CaseSensitive.
      Regenerating under a different culture can reorder the 141-family / 286-package
      arrays without any evidence changing. Aligning them risks reordering the 9.9 MB
      committed artifact, which is out of scope for a review pass that must not touch
      package selections.
    location: >-
      references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:1769
    severity: medium
  - summary: >-
      The audit artifact contract changed incompatibly but every commit in the bundle
      is typed fix(...) with no breaking-change marker.
    evidence: |-
      schemaVersion moved 1 -> 2, top-level auditedAtUtc moved into the snapshot
      envelope, and the family preservation envelope was replaced by origin. Commits
      3b8bac2, cfc6550 and fe3f0b7 are all "fix(audit): ..." with no "!" and no
      BREAKING CHANGE footer, so semantic-release will cut a patch version of a
      submodule other Hexalith repositories consume. Commit 7eaa21e in the same range
      also carries no Conventional Commits type at all.
    location: >-
      references/Hexalith.Builds
    severity: medium
  - summary: >-
      The checked-in audit artifact is not re-derivable by any shipped code path.
    evidence: |-
      Tools/package-version-audit.json is snapshot.mode incremental with 4 refreshed
      and 137 preserved families whose origins were back-filled from the previous v1
      audit's global revision and time. That back-fill can only happen on an
      incremental run over a v1 prior, which the generator now rejects with
      "incremental refresh requires a schemaVersion 2 prior audit"; a complete refresh
      would instead stamp all 141 origins with the current revision and time. The
      artifact is forward-refreshable but cannot be reproduced.
    location: >-
      references/Hexalith.Builds/Tools/package-version-audit.json
    severity: medium
  - summary: >-
      A refreshed family's prior historicalContext is copied forward without passing
      the closed-shape prior-history contract.
    evidence: |-
      Assert-PriorFamilyHistoryContract and Assert-PriorPackageHistoryContract are
      reached only through Assert-PriorV2PreservedFamily, which runs for preserved
      families. A refreshed family's prior history records are copied verbatim, so
      unknown or wrong-typed legacy fields can reach an emitted v2 document; the
      independent validator rejects it afterwards, but the generator will have written
      output it cannot validate.
    location: >-
      references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:1608
    severity: low
  - summary: >-
      Preserved family observations have no staleness ceiling, and nothing requires the
      committed artifact to ever be a complete refresh.
    evidence: |-
      The shipped artifact preserves 137 of 141 families against two distinct origin
      revision/timestamp pairs. Neither the generator, the validator, nor CI bounds how
      long a family may remain preserved, so the feed observations backing most of a
      freshness tool's own artifact can age indefinitely while every gate stays green.
    location: >-
      references/Hexalith.Builds/Tools/package-version-audit.json
    severity: low
  - summary: >-
      Dead v1-migration residue remains in the generator's preserved-family branch.
    evidence: |-
      The preserved branch still contains a path that, when a preserved decision has no
      origin, removes preservation and synthesizes an origin from the prior audit's
      global revision/time. Incremental refresh now requires a v2 prior, and
      Assert-PriorV2PreservedFamily's closed shape both requires origin and forbids
      preservation, so that path can no longer execute. It is the migration path the
      shipped artifact actually took, left behind after the guard closed it.
    location: >-
      references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:1509
    severity: low
  - summary: >-
      Several new fail-closed paths still have no fixture.
    evidence: |-
      Untested: "evaluated catalog contains no package families"; the prior/current
      family-absence pair; the preserved-family "source scope changed without being
      requested" and "consumer evidence changed without being requested" guards;
      Assert-RepositoryPathMatchesRevision's "could not be compared" branch; the
      generator's Get-GitBlobBytes "could not start" branch; and the validator's
      "Snapshot partition contains unknown family" and "Complete snapshot mode must
      refresh every family and preserve none" rules. The suites cover the mirror-image
      cases but not these.
    location: >-
      references/Hexalith.Builds/Tools
    severity: low
  - summary: >-
      Validation and generation cost scale with total stored history rather than with
      the refreshed family set on the incremental fast path.
    evidence: |-
      The validator serializes every history record with ConvertTo-Json -Depth 20
      -Compress for duplicate detection (5,756 records on the real artifact) and scans
      the full package list several times per family across 141 families; the generator
      re-verifies each preserved family's fingerprints twice and JSON round-trips every
      preserved package row. An incremental refresh that touches 4 families still pays
      full-document cost. Related to, but distinct from, the unbounded-history entry
      above.
    location: >-
      references/Hexalith.Builds/Tools/validate-package-version-audit.ps1
    severity: low
  - summary: >-
      The new UTF-8 BOM gate is unconditional for a shared submodule whose sibling
      catalogs are BOM-free, with no opt-out or migration note.
    evidence: |-
      validate-central-package-versions.ps1 now hard-fails any catalog without the
      exact EF BB BF prefix. Only Props/Directory.Packages.props, Hexalith.Commons and
      Hexalith.PolymorphicSerializations carry a BOM today; this repository's own root
      Directory.Packages.props, plus FrontComposer, EventStore, Memories, Parties and
      Tenants, are BOM-free. Nothing breaks yet because both workflow call sites use
      the default Props/ catalog, but the two files in this repository now sit under
      different byte contracts.
    location: >-
      references/Hexalith.Builds/Tools/validate-central-package-versions.ps1:86
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
- `references/Hexalith.Memories` - Uncommitted gitlink advance (`29581caf` -> `8ed18ed6`) made by a concurrent actor in this workspace during the follow-up review pass; this bundle did not request, modify, or review Memories.
- `references/Hexalith.Tenants` - Uncommitted gitlink advance (`073b945c` -> `ed0c0d68`) made by the same concurrent actor during the follow-up review pass; this bundle did not request, modify, or review Tenants.
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

### 2026-09-01 — Review pass (follow-up 2)
- intent_gap: 0
- bad_spec: 0
- patch: 10: (high 1, medium 7, low 2)
- defer: 10: (high 0, medium 4, low 6)
- reject: 8: (high 0, medium 3, low 5)
- addressed_findings:
  - `[high]` `[patch]` `test-package-version-audit-validator.ps1` was deterministically red on a clean tree, and `ci.yml` runs it unconditionally. The rewritten timeout fixture measured readiness-to-process-exit against a `GitBlobReadTimeoutSeconds + 12` bound, but the scenario validates a copy of the 9.9 MB production audit, so that window is dominated by full-document validation (~26 s measured) rather than by the 3-second read bound; all three repetitions failed with exit 137. The measured window is now readiness to both owned processes being gone, which is the bounded read plus the validator's own 2-second kill confirmation; process exit is guarded only for liveness. The suite now passes 103 scenarios.
  - `[medium]` `[patch]` The validator latched `$historicalBlobReadsAvailable` to `$false` on the first consumer-declaration blob-read failure, so one transient Git failure cascaded every remaining declaration into the raw worktree-byte comparison this change exists to avoid. The failure is now recorded per declaration without disabling revision binding for the rest, and a declaration whose own read failed no longer also reports a spurious raw-byte mismatch.
  - `[medium]` `[patch]` The validator's dirty-declaration check ran `git diff --quiet` without `--no-ext-diff` and reported every non-zero exit as drift, so a configured `diff.external` or a Git invocation error was reported to the operator as declaration drift. It now mirrors the generator's `Assert-RepositoryPathMatchesRevision`: exit 1 is dirty, anything else is "could not be compared" with the Git output.
  - `[medium]` `[patch]` A missing or JSON-null prior `origin`, `snapshot`, `consumerEvidence`, family decision, or array element failed parameter binding with an untyped runtime exception instead of the designed fail-closed diagnostic, because the mandatory parameters rejected `$null` before `Assert-ExactObjectShape`'s own null branch could run. `[AllowNull()]` on the prior-contract entry points makes that branch reachable, and three new tamper fixtures assert the exact typed messages.
  - `[medium]` `[patch]` Nothing covered the validator's `is dirty relative to generated-from revision` branch for consumer declarations, which became the only remaining worktree-versus-revision binding once the raw-byte comparison moved behind it; deleting it would have left the suite green. Added a repository scenario that edits a tracked declaration in the worktree without committing.
  - `[medium]` `[patch]` No generator fixture exercised the v1 branch of the prior preserved-family contract, although 1,856 of 1,861 family-history and 3,900 of 3,940 package-history records in the shipped artifact are v1, so that branch is the one every real incremental refresh traverses. Legacy v1 family and package history is now stamped onto the preserved fixture family, which brings it under the existing deep-equality assertions, plus two tampers proving a v1 record carrying a v2 origin is rejected.
  - `[medium]` `[patch]` The validator harness's `Get-FamilySelectionFingerprint` sorted rows by `id` case-insensitively while both production copies sort the projected `"id|version"` material case-sensitively. The orders differ whenever one id is a case variant or a prefix of another, so the harness could certify a fingerprint production never computes. The harness copy now mirrors production exactly.
  - `[medium]` `[patch]` `Tools/README.md` presented `-GitBlobReadTimeoutSeconds` and `-GitBlobReadMaxBytes` as operator overrides although both remain `DontShow`, documented `-Family` without saying it is an alias of the canonical `-ChangedFamily`, omitted the accepted ranges, and never stated the commit-first requirement the new pre-flight comparison imposes on the edit-then-regenerate loop. All four are corrected.
  - `[low]` `[patch]` The generator harness's `Read-ReadyProcessRecord` returned `$null` for a malformed readiness record, indistinguishable from "not ready yet", so a corrupt handshake degraded into a 30-second poll and a misleading "never observed the shim readiness handshake". It now returns the validator harness's typed `Valid`/`Diagnostic` shape and the call site reports it.
  - `[low]` `[patch]` The repository-provenance fixture's comment justified its `Remove-Item` with "checkout-index does not overwrite files already present" while the next line passes `--force`, which does overwrite; the comment now states the invariant the fixture actually depends on (re-materialization is what applies the `eol=crlf` attribute).

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

Status: done
Blocking condition: none

### Implemented change

This run was a follow-up review pass over the completed bundle (`9d9a6295..HEAD`, plus the
`references/Hexalith.Builds` range `9d77ed7..HEAD`). It found the delivered change's own
fixture suite red on a clean tree and repaired that plus nine further defects; no
intent gap and no spec defect were found, so `<intent-contract>` and the plan were not
amended and no implementation loopback was triggered.

The substantive fix: the timeout-cleanup fixture that this bundle rewrote was asserting
the wrong thing. Its bounded window ran from the shim readiness handshake to validator
process exit, but the scenario validates a copy of the 9.9 MB production audit, so that
window measures full-document validation cost (~26 s) rather than the 3-second Git blob
read bound it names. All three repetitions failed with exit 137 against the 15-second
bound, deterministically, and `references/Hexalith.Builds/.github/workflows/ci.yml` runs
that suite unconditionally. The window now ends when both owned processes are gone, which
is the read bound plus the validator's own kill confirmation and is independent of
document size; a regression that let the shim run to its 30-second sleep still fails it.

The remaining fixes harden the validator's consumer-declaration path (no cross-declaration
cascade after one failed blob read; `--no-ext-diff` and a real exit-1-versus-exit->1 split
on the dirty check), make the generator's prior-audit contract produce its designed typed
diagnostics instead of parameter-binding exceptions, close three fixture gaps (uncommitted
declaration drift, legacy v1 history on a preserved family, typed-diagnostic tampers),
align the validator harness's fingerprint helper and the generator harness's readiness
record with their production and sibling counterparts, and correct four inaccurate
statements in `Tools/README.md`.

### Files changed

- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1` — per-declaration blob-read failure handling and a Git-faithful dirty check.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` — `[AllowNull()]` on the prior-contract entry points so null/absent prior objects reach their typed diagnostics.
- `references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1` — bounded-read window reworked, uncommitted-declaration-drift scenario added, fingerprint helper aligned with production.
- `references/Hexalith.Builds/Tools/test-package-version-audit-generator.ps1` — legacy v1 history on the preserved fixture family, three typed-diagnostic tampers, out-of-process tamper invocation, readiness-record helper aligned with the validator harness, corrected fixture comment.
- `references/Hexalith.Builds/Tools/README.md` — hidden-parameter, alias, range, commit-first, and history-growth wording corrected.
- `_bmad-output/implementation-artifacts/spec-package-audit-provenance-bom.md` — this pass's triage log, deferred entries, and result.
- `references/Hexalith.Builds` — superproject gitlink advanced to carry the above.

### Review findings breakdown

- Patches applied: 10 — high 1, medium 7, low 2.
- Items deferred: 10 — medium 4, low 6 (appended to frontmatter `deferred`, now 13 entries total).
- Items rejected: 8 — medium 3, low 5. Rejected as noise or as deliberate design: the
  `^\.\.` catalog-path containment concern (already neutralized by the exact equality of
  `catalogPath` against the computed relative path that precedes it); an unbounded prior-audit
  read (the prior audit is a repository-owned artifact of the same size as the generator's own
  output); an empty output directory at a filesystem root (`Split-Path -Parent` returns the
  root, not empty); the removal of the catalog binding on preserved decisions and of the legacy
  untyped-history conversion (both deliberate consequences of family-local preservation and the
  v2-prior requirement); the pre-v2 origin-synthesis path (unreachable — incremental rejects a
  pre-v2 prior first); shim-helper duplication as such (the divergence was patched; sharing them
  needs a module the suites deliberately avoid); and the absence of a `git diff` dirty check on
  the catalog leg (the catalog is bound by `catalogRawSha256` against the committed blob, and a
  BOM/EOL-only worktree edit passing the normalized `catalogSha256` is the documented intent).

### Follow-up review recommendation

`true`. Patched findings this pass: high 1, medium 7, low 2. A high-severity patch alone sets
the flag; the score is `3 x 7 + 1 x 2 = 23`, which also clears the threshold of 5.

### Verification performed

All commands run from `references/Hexalith.Builds` after the patches:

- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1` — passed, 286 entries.
- `pwsh -NoProfile -File ./Tools/test-central-package-version-validator.ps1` — passed, 17 scenarios.
- `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` — passed, 50 approved identities and 3 shared versions.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1` — passed, 111 scenarios (was 102; +9 from this pass's fixtures). Before the patches this suite passed at 102.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1` — passed, 103 scenarios. Before the patches this suite **failed with 9 errors** across all three timeout repetitions.
- `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` — passed for 286 packages, 141 families, 1 source.
- `dotnet build Hexalith.Builds.slnx --configuration Release` — build succeeded, 0 warnings, 0 errors.
- `git diff --check` — clean. `Props/Directory.Packages.props` is byte-identical to the bundle baseline `9d77ed7`, and no package selection changed.
- `python3 eng/validate-story-artifacts.py --story <this spec>` — passed (freeform legacy gate; the spec carries no canonical dotted story ID).

### Residual risks

- The validator fixture suite now runs about 12 minutes locally because the three timeout
  repetitions wait for the validator to finish validating a production-sized audit instead of
  being killed at 15 seconds. The bounded-read assertion itself is unchanged in strictness.
- The bounded-read margin (`GitBlobReadTimeoutSeconds + 8`) is a wall-clock budget for process
  termination and remains machine-speed sensitive in principle, though it is now independent of
  audit size, which was the actual cause of the failure.
- Ten deferred entries remain open in this spec's frontmatter. Four are medium: the validator's
  catalog-ownership gating of blob reads, culture-sensitive ordering of the emitted arrays, the
  missing breaking-change marker for the `schemaVersion` 1 -> 2 contract change, and the fact
  that the checked-in artifact is not re-derivable by any shipped code path.
- Environment note, not a defect in the change: at 12:21 local an external process in this
  workspace committed this run's in-progress worktree edits to `references/Hexalith.Builds` as
  `fe3f0b7` ("fix(audit): enhance parameter handling and validation in audit scripts"). This run
  did not issue that commit. Nothing was lost and the final state was verified after it, but the
  commit captured a half-finished edit set, and its message does not describe a reviewed unit.
  A concurrent actor also advanced the uncommitted `references/Hexalith.Memories`
  (`29581caf` -> `8ed18ed6`) and `references/Hexalith.Tenants` (`073b945c` -> `ed0c0d68`)
  gitlinks mid-run; both were clean when this pass started. They are left uncommitted and
  declared under Documented Unrelated Changes rather than folded into this bundle. A second
  Claude session (`frontcomposer-cb`) is working the EventStore/Builds version-pin and
  governance-ledger reseal in this same checkout; it has been told this pass is finished with
  `references/Hexalith.Builds`.
