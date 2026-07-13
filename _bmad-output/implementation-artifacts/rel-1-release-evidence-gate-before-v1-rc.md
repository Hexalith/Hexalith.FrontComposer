---
created: 2026-07-05
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md
status: superseded
baseline_commit: 2d1a1290aaf62c10c4db6e70ab36c9e4d8622703
approval: approved-by-administrator-2026-07-05
amendmentApproval: approved-by-administrator-2026-07-05
alignmentApproval: approved-by-administrator-2026-07-09
supersededBy: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md
closedAs: superseded-2026-07-13
obligationsTransferredTo: REL-2
releaseModelDecision: "Superseded 2026-07-09 by the Tenants CI/CD alignment proposal, then CLOSED 2026-07-13: FR24 evidence obligations re-homed into REL-2. FrontComposer release follows the Tenants workflow_run-after-CI-success model through the pristine reusable Hexalith.Builds domain-release; FR24 evidence lives in a supplemental FrontComposer release-evidence.yml (reusing eng/release_evidence.py) plus consumer validation in shared CI. The advisory FR24 layer this story added to release.yml is removed when REL-2 adopts the reusable release workflow."
---

# REL-1: Implement FR24 Release Evidence Gate Before v1.0 RC

Status: **superseded** (closed 2026-07-13). Open FR24 ACs (AC2 signing / AC4–5 gating / AC6
package-consumer validation / AC8 evidence-recording) are transferred to **REL-2**. The advisory,
non-gating evidence layer this story shipped into `.github/workflows/release.yml` is intentionally
removed when REL-2 replaces `release.yml` with the reusable `domain-release.yml`. `REL-AI-1` stays open
under REL-2. See `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`.

Approval: approved by Administrator on 2026-07-05.

Amendment approval: documentation/live-workflow drift handling approved by Administrator on 2026-07-05.

Tenants CI/CD alignment approval: approved by Administrator on 2026-07-09. This supersedes the July 5
auto-publish-from-`main` release-model decision. REL-2 owns the implementation path that moves
FrontComposer to the Tenants `workflow_run` + reusable `domain-release.yml` model while preserving or
rerouting FR24 evidence.

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

**Scope note (owner decision 2026-07-05):** the release model was kept as the deliberate 2026-07-03
**auto-publish-from-`main`** path; REL-1 was rescoped to add an **advisory FR24 evidence layer only**
(no approval gates, no dry-run, signing deferred). Tasks below reflect that scope.

Done (evidence-only):

- [x] Add the FR24 evidence chain to `.github/workflows/release.yml` as advisory/best-effort steps
      around the unchanged auto-publish (test-results, inventory, SBOM, checksums, prepare/seal/verify
      manifest, advisory `classify-release`, evidence upload + GitHub Release attach). `.releaserc.json`
      kept evidence-free so the auto-publish model-guard tests stay valid.
- [x] Reuse `eng/release_evidence.py` commands for inventory, checksums, manifest, verification, and
      readiness classification.
- [x] Add a governance test (`ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating`) that
      **requires** the evidence layer while asserting it stays advisory and does not reintroduce
      dispatch/approval/dry-run gating; the two auto-publish model-guard tests are retained unchanged.
- [x] Reconcile `_bmad-output/project-docs/deployment-guide.md` to describe the current evidence-only
      behavior and an explicit "deferred FR24 targets" delta.
- [x] Run the governance lane locally: `CiGovernanceTests` 41/41 via the direct xUnit v3 runner
      (Release build 0/0). CI is authoritative for the release-workflow *execution*.

Deferred / out of scope (owner decision) — keep `REL-AI-1` OPEN:

- [ ] **AC2 signing:** `dotnet nuget sign` + `dotnet nuget verify --all` (needs a code-signing cert +
      secrets; Release Owner).
- [ ] **AC4–5 gating:** `classify-release --require-publishable`, dry-run default, owner approval, and
      partial-publish incident wiring — intentionally not added under the auto-publish model.
- [ ] **AC6 package-consumer validation:** clean consumer restores locally packed artifacts.
- [ ] **AC8 evidence recording:** Release Owner runs the workflow on a real release and records the
      evidence paths (test-results, inventory, SBOM, checksums, sealed manifest, readiness) before
      `REL-AI-1` can close.

## Implementation Notes

- Do not mark `REL-AI-1` done from this story file alone.
- Do not weaken the root submodule rule; release automation must keep `submodules: false` or an
  equivalent root-only initialization path and must not introduce recursive submodule updates.
- The package inventory may change after Stories 11.11-11.14 if `Contracts.UI` becomes packable; rerun
  package-consumer validation after any package-boundary change.
- Dry-run behavior must fail closed before NuGet or GitHub Release side effects.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (bmad-dev-story)

### Completion Notes

Implemented the **evidence-only** increment of REL-1 per the owner's 2026-07-05 decision (keep the
2026-07-03 auto-publish-from-`main` model; add FR24 evidence only; defer signing).

- Discovered the FR24 evidence tooling (`eng/release_evidence.py` + fixtures) is already complete and
  that a fully gated release workflow exists in history at `ef2823b~1` (deliberately streamlined to the
  minimal auto-publish path on 2026-07-03). Surfaced the conflict; owner chose "keep auto-publish, add
  evidence only".
- Added the advisory FR24 evidence layer to `.github/workflows/release.yml` around the unchanged
  publish. Evidence steps are best-effort (`|| echo ::warning::`) so they never gate/destabilize the
  auto-publish model. Kept `.releaserc.json` unchanged (evidence-free), preserving the two auto-publish
  model-guard governance tests.
- Added `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating` to require the evidence layer
  and assert it stays advisory (no `--require-publishable`, no `workflow_dispatch:`, no `RELEASE_DRY_RUN`,
  no `|| true`).
- Reconciled `deployment-guide.md` (current evidence-only behavior + deferred FR24 delta).

**Verification:** Release build of `Hexalith.FrontComposer.Shell.Tests` = 0 warnings / 0 errors;
`CiGovernanceTests` = **41/41 pass** via the direct xUnit v3 in-process runner (VSTest sockets blocked
locally per repo norm). The `release.yml` YAML parses (20 steps). **CI is authoritative for the
release-workflow execution** — the SBOM/CycloneDX `.slnx` invocation, manifest plumbing, and evidence
upload can only be exercised in GitHub Actions on a real release.

**`REL-AI-1` remains OPEN.** Reopen/close criteria: AC2 signing, AC4–5 gating, AC6 package-consumer
validation, and AC8 (Release Owner runs the workflow once and records evidence paths).

### File List

- `.github/workflows/release.yml` (modified — added advisory FR24 evidence layer; auto-publish unchanged)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (modified — added
  `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating`)
- `_bmad-output/project-docs/deployment-guide.md` (modified — reconciled to evidence-only reality)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — REL-1 status; REL-AI-1 note)
- `_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md` (this story)

## Change Log

- 2026-07-13: **Closed as superseded** by approved Correct Course proposal
  `sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`. FR24 obligations re-homed into
  REL-2 under the 3-layer split-homing architecture (CI consumer-validation + pristine reusable
  domain-release + supplemental release-evidence.yml). `REL-AI-1` remains open under REL-2.
- 2026-07-09: Superseded the July 5 auto-publish release-model decision by approved Correct Course
  proposal `sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md`. REL-2 owns the CI/CD
  alignment implementation. `REL-AI-1` remains open until FR24 evidence obligations are satisfied.
- 2026-07-05: Implemented evidence-only FR24 layer (advisory, non-gating) in `release.yml`; added
  requiring governance test; reconciled deployment-guide; verified `CiGovernanceTests` 41/41 locally.
  Signing/gating/consumer-validation deferred by owner decision; `REL-AI-1` stays open.
