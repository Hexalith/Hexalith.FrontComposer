---
title: Sprint Change Proposal - Align FrontComposer CI/CD With Tenants
status: approved
created: 2026-07-09
owner: Administrator
approvedBy: Administrator
approvedAt: 2026-07-09T15:21:06+02:00
mode: batch
scope: moderate
trigger:
  - "Administrator request: frontcomposer CI/CD should be the same as tenants submodule CI/CD"
baseline:
  frontcomposer_ci: .github/workflows/ci.yml
  frontcomposer_release: .github/workflows/release.yml
  tenants_ci: references/Hexalith.Tenants/.github/workflows/ci.yml
  tenants_release: references/Hexalith.Tenants/.github/workflows/release.yml
---

# Sprint Change Proposal: Align FrontComposer CI/CD With Tenants

## 1. Issue Summary

FrontComposer's primary CI/CD workflow shape has drifted from the Hexalith.Tenants module.
Tenants delegates the primary CI and release flows to shared reusable workflows in
`Hexalith.Builds`:

- `references/Hexalith.Tenants/.github/workflows/ci.yml` uses
  `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`.
- `references/Hexalith.Tenants/.github/workflows/release.yml` runs after a successful
  `CI` workflow on `main` via `workflow_run`, then uses
  `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.

FrontComposer currently has a bespoke `.github/workflows/ci.yml` with inline
commitlint, build, CLI smoke, governance, contract pact, docs, default, palette,
performance, quarantine, duration, and Playwright accessibility/visual gates.
Its `.github/workflows/release.yml` publishes on `push` to `main` and carries a
custom advisory FR24 evidence chain.

The requested correction is to make FrontComposer's CI/CD match the Tenants
submodule model rather than maintaining a separate primary pipeline shape.

## 2. Impact Analysis

### Epic Impact

- **Release Governance / PRD FR-24:** affected. The current FrontComposer
  release model was deliberately kept as auto-publish-from-`main` on 2026-07-05
  by `REL-1`. Aligning with Tenants supersedes that decision by making release
  run only after a successful CI workflow.
- **PRD NFR-11 / NFR-12:** affected. The v1.0 release gate still requires
  default tests, Governance, Contract, docs validation, e2e accessibility/visual
  evidence, package inventory, package-consumer validation, SBOM, checksums, and
  release evidence. These gates must either move into reusable workflows or stay
  in explicitly named supplemental FrontComposer quality workflows.
- **Epic 7 / Epic 10 tooling governance:** affected because CLI smoke, drift,
  diagnostics, text/JSON parity, and evidence-redaction gates currently depend
  on bespoke workflow steps and governance tests.
- **Epic 11 remediation:** affected because governance tests currently assert the
  existing CI and release workflow model.

No product-facing PRD feature scope changes are required. This is a CI/CD and
release-governance course correction.

### Story Impact

Current or affected stories:

- `REL-1: Implement FR24 Release Evidence Gate Before v1.0 RC` must be amended
  or superseded because its front matter and notes explicitly preserve the
  2026-07-03 auto-publish-from-`main` model.
- A new implementation story should be added, tentatively:
  `REL-2: Align FrontComposer CI/CD With Tenants Reusable Workflows`.
- Governance tests under
  `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
  and `Story12_4_RedPhaseDefTests.cs` need intentional updates because several
  tests currently pin the bespoke model.

### Artifact Conflicts

- **PRD:** no requirement rewrite is needed, but D-6 / FR-24 evidence ownership
  must note that the release trigger changes from direct push to CI-success
  `workflow_run`.
- **Epics:** no epic resequencing is needed. Add a Release Governance change note
  rather than reopening completed epics.
- **Architecture:** no runtime architecture change. CI/CD architecture shifts
  toward shared `Hexalith.Builds` workflow ownership.
- **UX:** no UX artifact impact.
- **CI/CD:** primary impact. `.github/workflows/ci.yml`,
  `.github/workflows/release.yml`, `.github/workflows/commitlint.yml`,
  workflow-governance tests, release-evidence tests, package validation scripts,
  and documentation must be updated together.

### Technical Impact

Key differences observed:

| Surface | FrontComposer current | Tenants baseline | Required correction |
| --- | --- | --- | --- |
| CI trigger | `push`, `pull_request` | `push`, `pull_request`, scheduled nightly | Add schedule if matching Tenants literally. |
| CI job shape | bespoke `commitlint`, `build-and-test`, `accessibility-visual` jobs | single reusable `ci` job | Replace primary CI with reusable `domain-ci.yml` call or extend shared workflow to host FrontComposer gates. |
| Commitlint | inline in CI plus separate PR-only workflow | separate reusable workflow on PR and push | Remove inline CI commitlint and make `.github/workflows/commitlint.yml` match Tenants. |
| Release trigger | direct `push` to `main` | `workflow_run` after successful CI push | Change release trigger and guard to Tenants model. |
| Release job shape | bespoke release steps and advisory FR24 evidence | reusable `domain-release.yml` | Use reusable release workflow; preserve or relocate FR24 evidence. |
| Package validation scripts | `eng/pack_release_packages.py`; no `scripts/validate-*` wrappers | `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py` | Add FrontComposer-compatible wrappers or extend shared workflows to accept repo-specific commands. |
| Verify snapshots | requires `DiffEngine_Disabled=true` | reusable workflow does not set it today | Add shared workflow env support or ensure all FrontComposer test invocations set it. |
| Playwright a11y/visual | bespoke Windows job | not present in Tenants CI | Move into shared workflow extension or a supplemental FrontComposer quality workflow. |

## 3. Recommended Approach

Selected path: **Direct Adjustment with backlog coordination**.

Scope classification: **Moderate**.

Rationale:

- The requested direction is concrete and feasible, but it reverses a recent
  release-model decision and touches governance tests, release evidence, and
  package-consumer validation.
- A pure file-copy from Tenants would drop FrontComposer-specific release gates
  required by PRD NFR-11 / NFR-12 unless those gates are relocated first.
- No rollback of product code or MVP reduction is needed.

Recommended implementation sequence:

1. Add or adapt FrontComposer package-validation scripts so the shared Tenants
   workflow inputs can run package and consumer validation successfully.
2. Update `.github/workflows/commitlint.yml` to match Tenants, including `push`
   to `main`.
3. Replace `.github/workflows/ci.yml` with the Tenants-style reusable
   `domain-ci.yml` call using FrontComposer inputs.
4. Move FrontComposer-only gates not supported by `domain-ci.yml` into one of:
   a shared `Hexalith.Builds` workflow extension, or a clearly supplemental
   `frontcomposer-quality.yml` workflow that is not the primary CI/CD path.
5. Replace `.github/workflows/release.yml` with the Tenants-style
   `workflow_run` + `domain-release.yml` call.
6. Update governance tests and release evidence docs to assert the new
   Tenants-aligned model.
7. Run the governance lane and the closest local validation lanes. CI remains
   authoritative for GitHub Actions execution.

Effort estimate: **Medium**.

Risk level: **Medium**.

Timeline impact: one focused implementation story, plus a release-owner review
before marking `REL-AI-1` or the new alignment story done.

## 4. Detailed Change Proposals

### 4.1 Workflow Change: Commitlint

File: `.github/workflows/commitlint.yml`

OLD:

```yaml
on:
  pull_request:
    branches: [main]
```

NEW:

```yaml
# Runs on PRs and on direct pushes to main: semantic-release derives versions
# solely from commit messages, and this repository pushes to main directly, so
# a PR-only gate can be bypassed.
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
```

Rationale: match Tenants and keep semantic-release commit validation on both PR
and direct-main paths.

### 4.2 Workflow Change: CI

File: `.github/workflows/ci.yml`

OLD:

```yaml
jobs:
  commitlint:
    runs-on: ubuntu-latest
    ...
  build-and-test:
    runs-on: ubuntu-latest
    ...
  accessibility-visual:
    runs-on: windows-latest
    ...
```

NEW:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * *'

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  ci:
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main
    with:
      solution: Hexalith.FrontComposer.slnx
      unit-test-projects: |
        tests/Hexalith.FrontComposer.Cli.Tests
        tests/Hexalith.FrontComposer.Contracts.Tests
        tests/Hexalith.FrontComposer.Mcp.Tests
        tests/Hexalith.FrontComposer.Shell.Tests
        tests/Hexalith.FrontComposer.SourceTools.Tests
        tests/Hexalith.FrontComposer.Testing.Tests
      run-consumer-validation: true
```

Required implementation notes before this can be merged:

- `domain-ci.yml` currently invokes `scripts/pack-release-packages.py`,
  `scripts/validate-nuget-packages.py`, and
  `scripts/validate-consumer-package-references.py` when
  `run-consumer-validation: true`. FrontComposer must add compatible wrappers or
  Hexalith.Builds must accept repo-specific pack/validation commands.
- FrontComposer tests need `DiffEngine_Disabled=true`; add support in the
  reusable workflow or use another shared mechanism before replacing the bespoke
  test commands.
- Decide where current FrontComposer-specific gates move:
  - CLI package smoke.
  - Governance-only lane.
  - Contract pact validation and stale pact diff check.
  - `eng/validate-docs.ps1`.
  - Quarantine evidence summary.
  - CI duration evidence.
  - Playwright accessibility/visual specimen gate.

Rationale: this is the Tenants primary CI model, adapted with FrontComposer test
projects and package-validation needs.

### 4.3 Workflow Change: Release

File: `.github/workflows/release.yml`

OLD:

```yaml
on:
  push:
    branches: [main]
...
jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - name: Run release tests
      - name: Record release test evidence
      - name: Run semantic-release (live publish)
      - name: Record FR24 release evidence bundle
      ...
```

NEW:

```yaml
name: Release

# Runs after CI succeeds on main (workflow_run) so the release never duplicates
# the CI gate and can never publish from a commit whose CI failed.
on:
  workflow_run:
    workflows: [CI]
    types: [completed]
    branches: [main]

concurrency:
  group: release-${{ github.ref }}
  cancel-in-progress: false

permissions:
  contents: read

jobs:
  release:
    if: >-
      github.event.workflow_run.conclusion == 'success' &&
      github.event.workflow_run.event == 'push'
    permissions:
      contents: write
      issues: write
      pull-requests: write
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
    with:
      solution: Hexalith.FrontComposer.slnx
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

Required implementation notes before this can be merged:

- Decide whether FR24 evidence generation moves into `domain-release.yml`,
  remains a supplemental FrontComposer workflow after release, or is deferred
  with an explicit `REL-AI-1` open-state note.
- Update release governance tests that currently require direct `push` release
  and forbid `workflow_run`.
- Reconcile `REL-1` and deployment documentation to state that the July 5
  auto-publish decision is superseded.

Rationale: match Tenants release semantics: publish only after the primary CI
workflow succeeds for a push to `main`.

### 4.4 Story Change: Supersede REL-1 Release Model

Artifact: `_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md`

OLD:

```yaml
releaseModelDecision: "2026-07-05 — keep the Jul 3 auto-publish-from-main model; add FR24 evidence only (no approval gates, no dry-run). Signing/cert deferred; attestation provenance + advisory classify only. Reverses REL-1's original gated-dispatch AC scope by owner decision."
```

NEW:

```yaml
releaseModelDecision: "Superseded 2026-07-09 by the Tenants CI/CD alignment proposal: FrontComposer release follows the Tenants workflow_run-after-CI-success model through Hexalith.Builds domain-release. FR24 evidence remains required before v1.0 RC and must be moved to the reusable release path or tracked as an explicit release-owner follow-up."
```

Rationale: the requested alignment reverses the recent auto-publish model. The
story record must make that explicit so future agents do not restore the
superseded behavior.

### 4.5 New Story Proposal: REL-2

Artifact to add after approval:
`_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md`

Proposed story:

```markdown
# REL-2: Align FrontComposer CI/CD With Tenants Reusable Workflows

As a Release Owner and FrontComposer maintainer,
I want FrontComposer primary CI/CD to use the same reusable Hexalith.Builds
workflows as Hexalith.Tenants,
so that Hexalith modules share one CI/CD operating model.

## Acceptance Criteria

1. Given `.github/workflows/commitlint.yml`, when workflow triggers are inspected,
   then it matches Tenants by running on PRs and pushes to `main`.
2. Given `.github/workflows/ci.yml`, when workflow structure is inspected, then
   the primary CI job uses `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`
   with FrontComposer-specific test project inputs and root-only submodule
   initialization.
3. Given package-consumer validation is enabled, when shared CI runs, then
   FrontComposer package pack/validate/consumer scripts are available and pass.
4. Given FrontComposer Verify snapshots, when tests run in shared CI, then
   `DiffEngine_Disabled=true` is applied so CI cannot hang on diff tooling.
5. Given `.github/workflows/release.yml`, when workflow structure is inspected,
   then release runs through `workflow_run` after successful `CI` push events and
   delegates to `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.
6. Given FR24 evidence requirements, when release alignment is complete, then
   evidence generation is either implemented in the reusable release path or
   `REL-AI-1` remains open with an explicit owner/date/reopen criterion.
7. Given governance tests run, when they inspect CI/CD workflows, then they assert
   the Tenants-aligned model and no longer assert the superseded July 5
   auto-publish model.
8. Given FrontComposer-only quality gates are still required by PRD NFR-11, when
   primary CI/CD is simplified, then those gates are either moved to shared
   reusable workflow support or retained as explicitly supplemental quality
   workflows with CI authority documented.
```

Rationale: this keeps the requested alignment implementable without losing the
release evidence and quality gates required for v1.0 readiness.

## 5. Checklist Summary

- [N/A] 1.1 Triggering story: no single story triggered the change; the trigger
  is the direct Administrator request to align with Tenants CI/CD.
- [x] 1.2 Core problem: FrontComposer uses bespoke primary CI/CD while Tenants
  uses shared reusable Hexalith.Builds domain workflows.
- [x] 1.3 Evidence gathered: compared FrontComposer and Tenants workflow files,
  reusable Hexalith.Builds workflows, REL-1 release model notes, PRD FR24 /
  NFR-11 / NFR-12, and CI governance tests.
- [x] 2.1 Current epic impact assessed: Release Governance / FR24 and tooling
  governance are affected.
- [x] 2.2 Epic-level changes: no new epic; add a release-governance story.
- [x] 2.3 Remaining epics reviewed: Epic 7, 10, and 11 have gate/test impacts
  but no sequence change.
- [x] 2.4 Future epic invalidation checked: none invalidated.
- [x] 2.5 Epic priority checked: no resequencing needed.
- [x] 3.1 PRD conflict checked: FR24 and NFR-11/NFR-12 require preserving
  evidence and quality gates.
- [x] 3.2 Architecture conflict checked: no runtime architecture conflict.
- [N/A] 3.3 UX conflict checked: no UX impact.
- [x] 3.4 Other artifacts checked: workflows, package scripts, governance tests,
  docs, and release-evidence story records require updates.
- [x] 4.1 Direct adjustment evaluated: viable with moderate coordination.
- [N/A] 4.2 Rollback evaluated: not useful.
- [N/A] 4.3 MVP review evaluated: not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment with backlog coordination.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact documented: no MVP scope reduction.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal reviewed for consistency.
- [x] 6.3 User approval received from Administrator on 2026-07-09.
- [x] 6.4 Sprint status update: REL-2 backlog entry added for implementation routing.
- [x] 6.5 Next steps and handoff plan defined.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Route to:

- **Release Owner:** approve superseding the July 5 auto-publish release model
  and decide where FR24 evidence lives after release becomes reusable.
- **Developer agent:** implement workflow/script/test changes.
- **QA/Test Architect:** verify governance-test updates preserve required
  quality gates and CI authority.

Implementation tasks after approval:

1. Add FrontComposer-compatible `scripts/` wrappers or extend Hexalith.Builds
   reusable workflow inputs for FrontComposer package validation.
2. Update commitlint, CI, and release workflows to the Tenants primary shape.
3. Preserve FrontComposer-specific required gates through shared workflow support
   or supplemental quality workflows.
4. Update governance tests and release evidence fixtures.
5. Update `REL-1`, add `REL-2`, and update `sprint-status.yaml` if the story is
   accepted into the active sprint.
6. Run the focused governance lane locally, then rely on GitHub Actions for the
   authoritative workflow execution.

Success criteria:

- FrontComposer primary `.github/workflows/ci.yml` and
  `.github/workflows/release.yml` follow the Tenants reusable workflow model.
- Commitlint runs on PR and push like Tenants.
- Release cannot publish from a `main` push whose CI failed.
- Root-declared submodule policy remains intact; no recursive submodule updates.
- FrontComposer v1.0 release gates remain accounted for, either in shared
  workflows or explicitly supplemental workflows.
- Governance tests pass with the new model.

## 7. Approval

Approved by Administrator on 2026-07-09.

Approval disposition:

- Scope: Moderate.
- Routed to: Release Owner, Developer agent, and QA/Test Architect.
- Backlog artifact: `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md`.
- Sprint status: `rel-2-align-frontcomposer-cicd-with-tenants: backlog`.

## 8. Handoff

Correct Course approval is complete. Implementation is routed through REL-2.
REL-1 remains open/review because its FR24 evidence obligations are not closed;
its July 5 auto-publish release-model decision is superseded by this proposal.
