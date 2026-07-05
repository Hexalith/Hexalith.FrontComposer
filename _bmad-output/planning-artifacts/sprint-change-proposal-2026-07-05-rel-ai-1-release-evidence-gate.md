---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
status: approved
scope: Moderate
owner: Release Owner
approval: approved-by-administrator-2026-07-05
amended: "2026-07-05: clarified release documentation/live-workflow drift and added Proposal G."
amendmentApproval: approved-by-administrator-2026-07-05
---

# Sprint Change Proposal - REL-AI-1 Release Evidence Gate

## Section 1 - Issue Summary

`REL-AI-1` is open in `_bmad-output/implementation-artifacts/sprint-status.yaml` and is currently the
named owner path for PRD FR24 / Release Governance Gate RG-1.

The planning artifacts already make FR24 visible:

- `prd.md` marks FR-24 as a release-governance v1 gate.
- `epics.md` maps FR24 to Release Governance Gate RG-1 and `REL-AI-1`.
- `sprint-status.yaml` assigns `REL-AI-1` to the Release Owner before the v1.0 release candidate.

The issue is that the live release implementation does not yet satisfy the full gate. The current
`.releaserc.json` only packs and pushes `nupkgs/*.nupkg` and `nupkgs/*.snupkg`; it does not sign
packages, generate SBOM, calculate checksums, prepare/seal/verify a release manifest, classify
publish readiness, or attach the full evidence bundle to the GitHub Release. The release workflow
currently validates package inventory and attests that inventory before semantic-release, but it does
not run the full FR24 evidence chain. Governance tests currently pin that minimal behavior by
asserting that `CycloneDX`, `dotnet nuget sign`, `classify-release`, `RELEASE_DRY_RUN`,
`partial-publish-incident`, `gh attestation`, and `nupkgs-signed` are absent from the release config.

This means `REL-AI-1` cannot be closed by marking the action done. It needs a focused release
governance implementation story or task that wires the existing `eng/release_evidence.py` capabilities
into the real release pipeline and proves package-consumer validation before v1.0 RC.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the open release action `REL-AI-1`, not a failed implementation story. |
| 1.2 Core problem | [x] | FR24 ownership exists in planning, but the implemented release pipeline and tests still pin an older minimal publish path. |
| 1.3 Evidence | [x] | Evidence: `sprint-status.yaml`, `prd.md`, `epics.md`, `.releaserc.json`, `.github/workflows/release.yml`, `eng/release_evidence.py`, `eng/release-package-inventory.json`, and `CiGovernanceTests`. |
| 2.1 Current epic impact | [x] | No product epic is invalidated; this is release governance. |
| 2.2 Epic-level changes | [!] | `epics.md` should keep RG-1 but add the implementation-story trigger when workflow/code gaps remain. |
| 2.3 Future epic impact | [x] | Epic 11 package-boundary stories depend on package-consumer validation evidence before RC but are not blocked from story creation solely by REL-AI-1. |
| 2.4 New/remove epics | [x] | No new epic is required. Add a focused release-governance story/task under the release gate. |
| 2.5 Priority/order | [!] | REL-AI-1 must close before v1.0 RC publication, after final package inventory is known and before live publish side effects. |
| 3.1 PRD conflicts | [!] | FR24 is present but should explicitly name package-consumer validation and publish-blocking evidence paths. |
| 3.2 Architecture conflicts | [x] | Architecture does not need structural change. Release artifacts sit outside runtime architecture. |
| 3.3 UX conflicts | [N/A] | No UI/UX impact. |
| 3.4 Other artifacts | [!] | Release workflow, semantic-release config, governance tests, release docs, and package-consumer validation scripts/tests need updates. |
| 4.1 Direct adjustment | Viable | Recommended: create a focused release-governance implementation story and update traceability text. |
| 4.2 Rollback | Not viable | No completed product work should be rolled back. |
| 4.3 MVP review | Not viable | MVP/v1 scope is unchanged; this is a publication gate. |
| 4.4 Recommended path | [x] | Direct Adjustment with a release implementation handoff. |
| 5.1-5.5 Proposal components | [x] | Captured below. |
| 6.1-6.2 Final review | [x] | Proposal is actionable and scoped. |
| 6.3 Approval | [x] | Approved by Administrator on 2026-07-05. |
| 6.4 Sprint status update | [x] | Applied to `sprint-status.yaml`; `REL-AI-1` remains open until evidence paths exist. |
| 6.5 Handoff | [x] | Moderate scope: Release Owner + Developer + QA/Test Architect. |

### Epic Impact

No feature epic should be reopened. FR24 is already mapped to Release Governance Gate RG-1, which is
the right ownership model.

The gate should produce one focused release-governance implementation story, tentatively `REL-1`,
because code/workflow/test changes are required before the gate can close. If the team uses only
release actions rather than numbered release stories, the same acceptance criteria should be attached
directly to `REL-AI-1`.

### Artifact Conflicts

PRD:

- FR24 currently says signed package artifacts and evidence, but package-consumer validation is only
  explicit in Success Metric SM-2 and `REL-AI-1`.
- The PRD decision register correctly blocks v1.0 publication on FR24 ownership; it should also name
  the evidence-path exit criteria.

Epics:

- RG-1 is present and correctly tied to `REL-AI-1`.
- Add a sentence that open workflow/product-code gaps produce the focused `REL-1` implementation
  story before RC.

Release implementation:

- `.releaserc.json` currently runs `eng/pack_release_packages.py` and directly pushes unsigned
  `nupkgs/*.nupkg` and `nupkgs/*.snupkg`.
- `.github/workflows/release.yml` currently validates `release-evidence/package-inventory.json` and
  attests that file before semantic-release, but does not run the full manifest/readiness chain.
- `eng/release_evidence.py` already has commands for inventory, checksums, test-results,
  prepare-manifest, seal-manifest, verify-manifest, release-budget, partial-publish-incident, and
  classify-release.
- `CiGovernanceTests` currently assert the absence of the full release gate and must be updated to
  require it.
- `_bmad-output/project-docs/deployment-guide.md` already describes the desired signed-package,
  SBOM, checksum, manifest, readiness, and GitHub Release asset flow as if it is implemented. That
  documentation is ahead of the live workflow and must either be corrected to current state or
  reconciled when `REL-1` implements the gate.

## Section 3 - Recommended Approach

Use **Direct Adjustment** with a focused release-governance implementation story.

Rationale:

- The requirement and owner are already in the planning artifacts; the gap is the executable release
  gate and evidence closure criteria.
- A new feature epic would be excessive and would blur release ownership.
- A status-only change would be inaccurate because the current workflow and governance tests do not
  produce the required FR24 evidence.
- The existing release evidence helper should be reused rather than replaced.

Effort estimate: Medium.

Risk level: Medium. The changes touch publish automation and governance tests. The safe path is to
default to dry-run/non-publish behavior until Release Owner approval, signed package verification,
manifest verification, and package-consumer validation all pass.

Timeline impact: Must complete before v1.0 RC. This should run after package-boundary work such as
Stories 11.11-11.14 is implemented, or be rerun after any package inventory/public API change.

## Section 4 - Detailed Change Proposals

### Proposal A - Tighten PRD FR24 Exit Criteria

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: `#### FR-24: Ship signed package artifacts with evidence`

OLD:

```text
FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, evidence chain, and GitHub Release assets.

Consequences:
- Conventional commits determine version bump.
- Release dry-run defaults to safe non-publish behavior.
- Package inventory and readiness classification gate publication.
```

NEW:

```text
FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation evidence.

Consequences:
- Conventional commits determine version bump.
- Release dry-run defaults to safe non-publish behavior and cannot publish package or GitHub Release side effects.
- Package inventory, signing/timestamp verification, symbol package presence, SBOM presence, checksum coverage, manifest verification, release-readiness classification, and package-consumer validation gate publication.
- `REL-AI-1` can be marked done only when the Release Owner records evidence paths for every FR24 artifact or records an approved fallback with explicit reopen criteria.
```

Rationale: This brings the PRD wording into alignment with `REL-AI-1` and SM-2, and makes closure
criteria concrete.

### Proposal B - Add Release Gate Implementation Story Trigger

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Release Governance Gate RG-1 (FR24)`

OLD:

```text
If workflow or product-code changes are required to produce that evidence, create a focused implementation
story before publication.
```

NEW:

```text
If workflow, release-helper, governance-test, or product-code changes are required to produce that evidence,
create a focused release-governance implementation story (`REL-1` or the team's equivalent) before RC
publication. The story must close the gap between `.releaserc.json`, `.github/workflows/release.yml`,
`eng/release_evidence.py`, governance tests, release docs, and package-consumer validation evidence.
```

Rationale: The current repository state proves such a story is required; the gate should name the
affected artifact families.

### Proposal C - Add `REL-1` Release Governance Story

New artifact: `_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md`

Proposed story:

```text
# REL-1: Implement FR24 release evidence gate before v1.0 RC

As a Release Owner,
I want the v1.0 release pipeline to produce and verify signed packages, symbols, SBOM, checksums,
a sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation,
So that FrontComposer cannot publish a v1.0 RC without auditable FR24 evidence.

Acceptance Criteria:

1. Given the release workflow runs on main or trusted manual RC context,
   When semantic-release prepares a release,
   Then it builds the explicit `eng/release-package-inventory.json` package set, produces all required
   `.nupkg` and `.snupkg` files, and rejects unexpected packable projects or package-id drift.

2. Given packages are produced,
   When signing is configured,
   Then `.nupkg` packages are signed, RFC 3161 timestamp evidence is verified with
   `dotnet nuget verify --all`, and unsigned or timestamp-missing packages block publication.

3. Given release evidence is prepared,
   When the evidence chain runs,
   Then SBOM JSON, checksums, test-results evidence, package inventory, signing verification,
   `prepare-manifest`, `seal-manifest`, and `verify-manifest` outputs are produced under
   `release-evidence/` and bound to commit SHA, tag, run id, workflow ref, package set fingerprint,
   and version.

4. Given the release is a dry run, fork, unprotected ref, missing approval, missing attestation, failed
   tests, invalid manifest, missing symbols, missing SBOM, checksum mismatch, or failed package-consumer
   validation,
   When `classify-release --require-publishable` runs,
   Then publish side effects are blocked and the readiness output explains the blocking reason.

5. Given live publish is approved,
   When NuGet push begins,
   Then publish uses signed `.nupkg` artifacts and required `.snupkg` symbol packages, records any
   partial-publish incident before exit, and attaches the full evidence bundle as GitHub Release assets.

6. Given the packages are built,
   When package-consumer validation runs,
   Then a clean consumer project or representative Hexalith adopter path restores the local packages,
   references the expected package set, builds with package validation enabled where applicable, and
   proves the documented bootstrap/package boundary path before RC.

7. Given governance tests run,
   When release workflow/configuration is inspected,
   Then tests require the FR24 gate (`CycloneDX` or approved SBOM command, signing, checksums,
   manifest verification, classify-release, dry-run guard, partial-publish incident handling,
   GitHub Release evidence assets, and package-consumer validation) instead of asserting those items
   are absent.

8. Given Release Owner review completes,
   When all evidence paths are recorded,
   Then `REL-AI-1` may move to `done`; otherwise it remains `open` with the blocking evidence gap.
```

Rationale: The release gate needs implementation work and test reversal. This story isolates that
work from product feature epics.

### Proposal D - Update Sprint Status With Evidence Closure Fields

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- epic: release
  action: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
  owner: "Release Owner"
  assigned: "2026-07-05"
  due: "before v1.0 release candidate"
  status: open
```

NEW:

```yaml
- epic: release
  action: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
  owner: "Release Owner"
  assigned: "2026-07-05"
  due: "before v1.0 release candidate"
  status: open
  implementation_story: "REL-1"
  evidence_required:
    - "validated package inventory"
    - "signed .nupkg verification with timestamp"
    - "required .snupkg symbol packages"
    - "SBOM"
    - "checksums"
    - "sealed release manifest and verify-manifest output"
    - "release-readiness classification"
    - "GitHub Release assets or dry-run asset evidence"
    - "package-consumer validation"
  closure_rule: "Move to done only after Release Owner records evidence paths or approved fallback with reopen criteria."
```

Rationale: The action remains open until actual evidence exists. Adding closure metadata prevents
future status-only closure.

### Proposal E - Flip Release Governance Tests To The New Contract

Artifacts:

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `.releaserc.json`
- `.github/workflows/release.yml`

Current test posture:

```text
Tests currently assert the release config does not contain:
- CycloneDX
- dotnet nuget sign
- gh attestation
- classify-release
- nupkgs-signed
- RELEASE_DRY_RUN
- partial-publish-incident
- --require-publishable
```

Proposed posture:

```text
Tests should require:
- package inventory before publish
- SBOM generation or approved SBOM command
- package signing and timestamp verification
- checksum generation over signed packages, symbols, SBOM, and evidence files
- prepare-manifest, seal-manifest, and verify-manifest
- classify-release --require-publishable before publish
- dry-run fail-closed behavior
- partial-publish incident capture before failed exits
- GitHub Release asset list including signed packages, symbols, checksums, sealed manifest, readiness,
  test-results, SBOM, signing verification, and attestation/fallback evidence
- package-consumer validation before `REL-AI-1` can close
```

Rationale: Governance tests are currently the strongest signal that the live release path is still
minimal. They must become the guard for FR24 rather than the guard against it.

### Proposal F - Add Package-Consumer Validation

Artifacts to create or update:

- `eng/` release validation script or test fixture
- release workflow
- governance tests
- release/deployment docs, including the current `_bmad-output/project-docs/deployment-guide.md`
  overstatement of the full gate

Proposed validation:

```text
Build a clean temporary consumer project using the locally packed release artifacts as a NuGet source.
Reference the expected package set, including the final package inventory after Contracts.UI decisions.
Run restore and build with package validation enabled where applicable.
Exercise the documented bootstrap/package-boundary path at minimum:
- Contracts-only consumer does not inherit UI/runtime dependencies after the approved split is implemented.
- Shell/UI consumer can reference Shell and compile the documented bootstrap surface.
- MCP/Schema/Testing packages restore and expose expected public package references.
Record the command, package source path, produced package versions, and result file under release-evidence/.
```

Rationale: FR24 is about package-consumer safety, not only publisher-side artifacts. This is also the
catch point for Story 11.11-11.14 package-boundary changes.

### Proposal G - Reconcile Release Documentation With Live Workflow State

Artifacts to update:

- `_bmad-output/project-docs/deployment-guide.md`
- published release/deployment docs under `docs/`, if they repeat the same release-pipeline claims
- `REL-1` completion notes and Release Owner evidence records

OLD:

```text
Release documentation presents the full signed package, SBOM, checksums, manifest/evidence chain,
readiness classification, and GitHub Release asset pipeline as the current release behavior.
```

NEW:

```text
Until `REL-1` lands, release documentation must distinguish current behavior from the required FR24
target. After `REL-1` lands, the docs must name the actual commands, generated evidence paths,
approval/fallback inputs, dry-run behavior, and package-consumer validation result expected before
`REL-AI-1` can close.
```

Rationale: Release documentation is itself part of the evidence chain. It must not imply that the
publication gate exists before the release workflow and governance tests enforce it.

## Section 5 - Implementation Handoff

Scope classification: **Moderate**.

Route to:

- Release Owner: owns `REL-AI-1`, approves closure, and records evidence paths.
- Developer: implements `REL-1` release workflow/config/script changes.
- QA/Test Architect: updates governance tests and package-consumer validation coverage.
- Product Owner: confirms PRD FR24 wording and v1.0 RC gate acceptance.

Recommended sequence:

1. Approve this proposal.
2. Add `REL-1` or equivalent release-governance implementation story.
3. Tighten PRD FR24 wording and `epics.md` RG-1 trigger text.
4. Add `REL-AI-1` evidence closure metadata in sprint status.
5. Implement release workflow/config changes using existing `eng/release_evidence.py` commands.
6. Add package-consumer validation.
7. Flip governance tests to require the FR24 gate.
8. Reconcile release/deployment docs so current behavior and FR24 target behavior are not conflated.
9. Run release dry-run evidence generation.
10. Release Owner records evidence paths and marks `REL-AI-1` done only if all required evidence exists.

Success criteria:

- `REL-AI-1` remains open until evidence exists.
- Release workflow cannot publish v1.0 RC without valid package inventory, signed packages, symbols,
  SBOM, checksums, sealed manifest, readiness classification, GitHub Release evidence assets, and
  package-consumer validation.
- Governance tests fail if the release config regresses to the current minimal pack-and-push path.
- Package-consumer validation catches package inventory or boundary drift before RC.

## Section 6 - Approval State

This proposal was approved by Administrator on 2026-07-05.

This run amended the approved proposal to call out that deployment/release documentation already
describes the target FR24 gate while the live workflow still implements the minimal pack-and-push path.
Administrator approved this amendment on 2026-07-05.

Approved planning changes were applied to `prd.md`, `epics.md`, and `sprint-status.yaml`.
The focused release-governance implementation story was created at
`_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md`.

No live release workflow, semantic-release configuration, product source code, or governance test code was
modified by this proposal. Those implementation changes are routed to `REL-1`.

Handoff completion:

- Scope: Moderate.
- Routed to: Release Owner, Developer, QA/Test Architect, and Product Owner.
- Next gate: implement `REL-1`; keep `REL-AI-1` open until evidence paths or approved fallback are recorded.
