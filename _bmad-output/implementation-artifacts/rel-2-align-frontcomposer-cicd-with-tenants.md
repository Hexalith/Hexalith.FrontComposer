---
created: 2026-07-09
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md
status: backlog
approval: approved-by-administrator-2026-07-09
scope: moderate
---

# REL-2: Align FrontComposer CI/CD With Tenants Reusable Workflows

Status: backlog.

Approval: approved by Administrator on 2026-07-09.

## Story

As a Release Owner and FrontComposer maintainer,
I want FrontComposer primary CI/CD to use the same reusable Hexalith.Builds workflows as Hexalith.Tenants,
so that Hexalith modules share one CI/CD operating model.

## Context

The approved Correct Course proposal
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md`
compares FrontComposer's bespoke CI/CD with the Tenants submodule baseline:

- Tenants CI delegates to `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`.
- Tenants release runs from `workflow_run` after a successful `CI` push to `main` and delegates to
  `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.
- FrontComposer currently has bespoke CI/release workflows and governance tests that pin that model.

REL-1 remains open/review for FR24 evidence obligations, but its July 5 auto-publish-from-`main`
release-model decision is superseded by the approved July 9 Tenants alignment proposal.

## Acceptance Criteria

1. Given `.github/workflows/commitlint.yml`, when workflow triggers are inspected, then it matches
   Tenants by running on PRs and pushes to `main`.

2. Given `.github/workflows/ci.yml`, when workflow structure is inspected, then the primary CI job uses
   `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main` with FrontComposer-specific test
   project inputs and root-only submodule initialization.

3. Given package-consumer validation is enabled, when shared CI runs, then FrontComposer package
   pack/validate/consumer scripts are available and pass.

4. Given FrontComposer Verify snapshots, when tests run in shared CI, then `DiffEngine_Disabled=true`
   is applied so CI cannot hang on diff tooling.

5. Given `.github/workflows/release.yml`, when workflow structure is inspected, then release runs through
   `workflow_run` after successful `CI` push events and delegates to
   `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.

6. Given FR24 evidence requirements, when release alignment is complete, then evidence generation is
   either implemented in the reusable release path or `REL-AI-1` remains open with an explicit
   owner/date/reopen criterion.

7. Given governance tests run, when they inspect CI/CD workflows, then they assert the Tenants-aligned
   model and no longer assert the superseded July 5 auto-publish model.

8. Given FrontComposer-only quality gates are still required by PRD NFR-11, when primary CI/CD is
   simplified, then those gates are either moved to shared reusable workflow support or retained as
   explicitly supplemental quality workflows with CI authority documented.

## Tasks

- [ ] Add FrontComposer-compatible `scripts/` wrappers or extend Hexalith.Builds reusable workflow inputs
      for FrontComposer package validation.
- [ ] Update `.github/workflows/commitlint.yml` to match Tenants PR + push triggers.
- [ ] Replace the primary `.github/workflows/ci.yml` job shape with the Tenants reusable
      `domain-ci.yml` model using FrontComposer inputs.
- [ ] Preserve FrontComposer-specific required gates through shared workflow support or supplemental
      quality workflows: CLI smoke, Governance, contract pacts, docs validation, quarantine evidence,
      CI duration evidence, and Playwright accessibility/visual.
- [ ] Replace `.github/workflows/release.yml` with the Tenants `workflow_run` +
      reusable `domain-release.yml` model.
- [ ] Update CI/release governance tests and release-evidence fixtures.
- [ ] Update REL-1/deployment documentation references so they no longer restore the superseded
      auto-publish model.
- [ ] Run focused governance validation locally and record any GitHub Actions-only execution gaps.

## Implementation Notes

- Do not initialize nested submodules. CI changes must keep root-declared submodule initialization only.
- Do not drop FR24 evidence obligations. If the reusable release path cannot host them in this story,
  keep `REL-AI-1` open with explicit owner/date/reopen criteria.
- Do not treat a direct Tenants file copy as sufficient if FrontComposer-specific required gates become
  untracked.
- Existing modified submodule pointers are out of scope unless the implementation story explicitly owns
  them.

## File List

- `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md`

## Change Log

- 2026-07-09: Created from approved Correct Course proposal. Story is backlog and ready for Release
  Owner / Developer / QA routing.
