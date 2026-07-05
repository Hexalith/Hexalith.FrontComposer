---
created: 2026-07-05
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md
status: ready-for-dev
approval: approved-by-administrator-2026-07-05
amendmentApproval: approved-by-administrator-2026-07-05
---

# REL-1: Implement FR24 Release Evidence Gate Before v1.0 RC

Status: ready-for-dev

Approval: approved by Administrator on 2026-07-05.

Amendment approval: documentation/live-workflow drift handling approved by Administrator on 2026-07-05.

## Story

As a Release Owner,
I want the v1.0 release pipeline to produce and verify signed packages, symbols, SBOM, checksums, a
sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation,
so that FrontComposer cannot publish a v1.0 RC without auditable FR24 evidence.

## Context

`REL-AI-1` is the sprint-status action that owns PRD FR24 / Release Governance Gate RG-1. The planning
artifacts already assign ownership, but the live release implementation still follows a minimal
pack-and-push path:

- `.releaserc.json` packs with `eng/pack_release_packages.py` and pushes `nupkgs/*.nupkg` plus
  `nupkgs/*.snupkg`.
- `.github/workflows/release.yml` validates package inventory before semantic-release but does not run
  the full evidence chain.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` currently asserts that
  signing, SBOM, readiness classification, dry-run gating, partial-publish incidents, and signed-package
  directories are absent from the release config.
- `eng/release_evidence.py` already contains reusable commands for package inventory, checksums,
  test-results, manifest preparation/sealing/verification, release-budget, partial-publish incident
  recording, and release classification.
- `_bmad-output/project-docs/deployment-guide.md` currently describes the desired full release
  pipeline as if it already exists. Treat that as documentation/live-workflow drift to reconcile, not
  as evidence that the gate is implemented.

This story implements the executable FR24 gate. `REL-AI-1` remains open until the Release Owner records
evidence paths or an approved fallback with reopen criteria.

## Acceptance Criteria

1. Given the release workflow runs on `main` or a trusted manual RC context, when semantic-release
   prepares a release, then it builds the explicit `eng/release-package-inventory.json` package set,
   produces all required `.nupkg` and `.snupkg` files, and rejects unexpected packable projects or
   package-id drift.

2. Given packages are produced, when signing is configured, then `.nupkg` packages are signed, RFC 3161
   timestamp evidence is verified with `dotnet nuget verify --all`, and unsigned or timestamp-missing
   packages block publication.

3. Given release evidence is prepared, when the evidence chain runs, then SBOM JSON, checksums,
   test-results evidence, package inventory, signing verification, `prepare-manifest`, `seal-manifest`,
   and `verify-manifest` outputs are produced under `release-evidence/` and bound to commit SHA, tag,
   run id, workflow ref, package-set fingerprint, and version.

4. Given the release is a dry run, fork, unprotected ref, missing approval, missing attestation, failed
   tests, invalid manifest, missing symbols, missing SBOM, checksum mismatch, or failed package-consumer
   validation, when `classify-release --require-publishable` runs, then publish side effects are blocked
   and the readiness output explains the blocking reason.

5. Given live publish is approved, when NuGet push begins, then publish uses signed `.nupkg` artifacts and
   required `.snupkg` symbol packages, records any partial-publish incident before exit, and attaches the
   full evidence bundle as GitHub Release assets.

6. Given the packages are built, when package-consumer validation runs, then a clean consumer project or
   representative Hexalith adopter path restores the local packages, references the expected package set,
   builds with package validation enabled where applicable, and proves the documented bootstrap/package
   boundary path before RC.

7. Given governance tests run, when release workflow/configuration is inspected, then tests require the
   FR24 gate (`CycloneDX` or approved SBOM command, signing, checksums, manifest verification,
   `classify-release`, dry-run guard, partial-publish incident handling, GitHub Release evidence assets,
   and package-consumer validation) instead of asserting those items are absent.

8. Given Release Owner review completes, when all evidence paths are recorded, then `REL-AI-1` may move to
   `done`; otherwise it remains `open` with the blocking evidence gap.

## Tasks

- [ ] Update `.releaserc.json` and/or release scripts so prepare/publish produce the FR24 evidence chain
      before any publish side effect.
- [ ] Update `.github/workflows/release.yml` to run trusted release evidence steps with safe dry-run and
      approval behavior.
- [ ] Reuse `eng/release_evidence.py` commands for inventory, checksums, manifest, verification,
      readiness classification, partial-publish incident handling, and release-budget evidence.
- [ ] Add package-consumer validation using locally packed artifacts as a NuGet source.
- [ ] Flip release governance tests from "full gate absent" to "full gate required".
- [ ] Reconcile release/deployment docs so they distinguish current release behavior from the FR24
      target until the gate is implemented; after implementation, update them with actual evidence
      paths, approval/fallback inputs, dry-run behavior, operator commands, and package-consumer
      validation output.
- [ ] Run the relevant governance tests and a release dry-run evidence pass.
- [ ] Record evidence paths or approved fallback in sprint status before closing `REL-AI-1`.

## Implementation Notes

- Do not mark `REL-AI-1` done from this story file alone.
- Do not weaken the root submodule rule; release automation must keep `submodules: false` or an
  equivalent root-only initialization path and must not introduce recursive submodule updates.
- The package inventory may change after Stories 11.11-11.14 if `Contracts.UI` becomes packable; rerun
  package-consumer validation after any package-boundary change.
- Dry-run behavior must fail closed before NuGet or GitHub Release side effects.

## Dev Agent Record

### Agent Model Used

TBD

### Completion Notes

Pending implementation.

### File List

Pending implementation.
