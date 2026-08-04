---
title: 'Remove author signing from the production package contract'
type: 'refactor'
created: '2026-08-04'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'bb9c41f2590251b2faa806a6e98ab6446b5a4e63'
context:
  - '_bmad-output/implementation-artifacts/spec-align-production-release-with-tenants.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/project-docs/deployment-guide.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer's otherwise-ready production release still requires an author code-signing certificate, PFX secret, password, timestamp service, and local trust-store manipulation. That cost and ceremony are disproportionate for this project and currently block publication.

**Approach:** Publish the already packed and sealed unsigned NuGet candidates, remove author-signing inputs and readiness gates, and retain the supply-chain boundary through exact candidate checksums, SBOM/attestation evidence, immutable GitHub assets, NuGet.org repository-signature verification, and content-equivalence checks that account only for NuGet.org's added `.signature.p7s` entry.

## Boundaries & Constraints

**Always:** Preserve exact-source dispatch, production approval, pack-once behavior, the exact eight-package inventory, sealed raw checksums for GitHub assets, symbol checksums, SBOM, attestation/fallback controls, immutable release/tag-to-SHA checks, NuGet availability checks, and fail-closed partial-publication evidence. Keep historical signed v2 manifest verification readable while making the current exact-source manifest explicitly unsigned-author/no-prepublication-signature.

**Ask First:** A real release dispatch or publication; changes to NuGet.org package-owner signer policy; deletion of existing GitHub secrets or variables; branch, commit, push, PR, dependency, or submodule mutation.

**Never:** Generate or require an author-signing certificate, PFX/password, timestamp service, `dotnet nuget sign`, trust-store modification, or `nupkgs-signed` staging path. Never weaken raw GitHub-asset checksum verification, treat NuGet.org's repository signature as an author signature, ignore any archive difference beyond `.signature.p7s`, or claim publication without all eight packages and the exact release tag.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Prepare | eight unsigned pack-once candidates | seal and publish `nupkgs/*.nupkg` without signing configuration | missing/count/hash drift blocks before push |
| Reconcile | GitHub candidate plus NuGet.org repository-signed download | GitHub raw checksum matches; all non-signature ZIP members match; repository signature verifies | missing signature, unsafe/duplicate ZIP paths, content drift, or failed verification creates an incident and fails |
| Historical audit | sealed v2 signed evidence | legacy signed-manifest rules remain auditable | malformed legacy evidence remains rejected |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml`, `.releaserc.json` -- remove signing inputs and publish/attach the sealed `nupkgs` candidates.
- `.github/workflows/release-evidence.yml` -- reconcile raw GitHub bytes with NuGet.org's repository-signed content without false exact-byte comparisons.
- `eng/release_prepublish.py`, `eng/release_evidence.py` -- delete author-signing phases and define the current unsigned manifest/readiness contract while retaining legacy audit support.
- `eng/release_contract.py` -- provide strict NuGet archive-content and repository-verification contract helpers.
- `tests/eng/`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/` -- executable positive/negative release-policy coverage.
- `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/project-docs/deployment-guide.md`, `_bmad-output/project-context.md` -- replace certificate/timestamp requirements with the resulting repository-signature model.

## Tasks & Acceptance

**Execution:**
- [x] Update workflow, Semantic Release, prepublish, manifest, and evidence paths to consume only the sealed unsigned `nupkgs` candidates and remove signing credentials/readiness artifacts.
- [x] Add fail-closed comparison of canonical non-signature package contents plus independent per-package NuGet.org repository-signature verification.
- [x] Preserve legacy signed-v2 audit behavior and update Python/C# governance tests, fixtures, documentation, and release-definition fingerprints for the current contract.

**Acceptance Criteria:**
- Given exact green main and production approval, when a release is required, then all eight unsigned sealed candidates can publish without any author-signing secret or timestamp variable.
- Given NuGet.org adds its repository signature, when evidence reconciliation runs, then it accepts only `.signature.p7s` as the archive difference and verifies all eight repository signatures; any other difference, missing publication, or signature failure produces a blocking incident.
- Given the change is inspected, then no active workflow, helper, readiness gate, documentation, or governance assertion requires author signing or a `nupkgs-signed` tree, while all non-signing release controls remain effective.

## Spec Change Log

## Design Notes

NuGet.org repository-signs new submissions and adds `.signature.p7s` to an unsigned upload, so its downloadable archive cannot raw-hash-equal the uploaded candidate. Exact raw hashes remain authoritative for the sealed candidate and GitHub Release. NuGet reconciliation compares normalized ZIP member names and uncompressed member bytes excluding only the root signature entry, then requires `dotnet nuget verify --all` evidence identifying a successful repository signature for each expected package.

## Verification

**Commands:**
- `actionlint` for changed workflows; focused Python release/evidence/contract suites including archive/signature mutation cases.
- Release-build affected governance test projects with zero warnings/errors, then invoke built xUnit v3 assemblies directly with focused filters.
- Validate the eight-package manifest against the pinned Builds contract; run `git diff --check` and recursive-submodule/reference scans.

## Suggested Review Order

**Production boundary**

- Release preparation now seals the pack-once candidates without signing credentials.
  [`release.yml:246`](../../.github/workflows/release.yml#L246)

- Candidate preparation, transfer, and publication reject signed or unsafe NuGet archives.
  [`release_prepublish.py:168`](../../eng/release_prepublish.py#L168)

- Explicit package pushes suppress NuGet's automatic duplicate symbol upload.
  [`release_prepublish.py:789`](../../eng/release_prepublish.py#L789)

**Evidence contract**

- Current manifests use unsigned rows while signed v2 evidence remains strictly legacy-only.
  [`release_evidence.py:1406`](../../eng/release_evidence.py#L1406)

- NuGet reconciliation permits only the root repository-signature member as a difference.
  [`release_contract.py:132`](../../eng/release_contract.py#L132)

- Repository verification requires all coordinates and NuGet.org's exact service index.
  [`release_contract.py:150`](../../eng/release_contract.py#L150)

- Compliance now requires structured repository-signature evidence alongside payload equality.
  [`release-evidence.yml:448`](../../.github/workflows/release-evidence.yml#L448)

**Verification and guidance**

- Runtime tests prove unsigned candidate bundling, restoration, and duplicate-symbol prevention.
  [`test_release_prepublish.py:45`](../../tests/eng/test_release_prepublish.py#L45)

- Exact-source v3 preparation now completes a successful seal and live verification round trip.
  [`test_release_evidence_v2.py:339`](../../tests/eng/test_release_evidence_v2.py#L339)

- Archive and signature mutations exercise every new fail-closed comparison boundary.
  [`test_release_contract.py:113`](../../tests/eng/test_release_contract.py#L113)

- Architecture documents raw GitHub identity versus NuGet.org repository-signed content identity.
  [`architecture.md:232`](../planning-artifacts/architecture.md#L232)

- Operator guidance no longer requests author-signing certificate configuration.
  [`deployment-guide.md:54`](../project-docs/deployment-guide.md#L54)

- The active REL-5 owner story now requires NuGet.org signer-policy confirmation and a bounded
  operator dispatch, not certificate procurement.
  [`rel-5-provision-signing-identity-and-first-governed-release.md:15`](rel-5-provision-signing-identity-and-first-governed-release.md#L15)

- The compliance ledger retains historical signature facts while defining the current unsigned
  production prerequisites and repository-signature evidence.
  [`rel-ai-1-release-evidence-ledger.md:49`](rel-ai-1-release-evidence-ledger.md#L49)
