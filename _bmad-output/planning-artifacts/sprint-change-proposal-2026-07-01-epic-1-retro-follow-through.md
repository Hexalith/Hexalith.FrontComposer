# Sprint Change Proposal: Epic 1 Retro Follow-Through

Date: 2026-07-01
Trigger: `_bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md`
Mode: Correct Course
Status: approved-implemented
Approved by: Administrator
Approved: 2026-07-01

> Note: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md` already exists for a
> separate row-level new-item producer follow-up. This Epic 1 follow-through uses a collision-free
> filename.

## 1. Issue Summary

Epic 1 closed the shell foundation, but its retrospective carried process and artifact hygiene
follow-ups that were still partially unresolved:

- File List and test-count drift recurred after QA automation and review passes.
- Raw authoring sentinels leaked into story or test-summary markdown artifacts.
- FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording needed durable follow-up tracking after later
  readiness work.
- The FC-DOC settings component page gap remained open even though Story 1.6 is complete.

The issue does not invalidate Epic 1 implementation. It is a governance and documentation correction
for completed artifacts and future story automation.

## 2. Impact Analysis

Epic impact:

- Epic 1 remains done.
- No runtime product behavior changes are required.
- Sprint tracking needs explicit open actions for residual wording decisions and process automation.

Story impact:

- Existing story records keep their completion status.
- Story artifact validation should become a required completion/review gate for future story work.

Artifact impact:

- Contract docs need dated tracking notes for residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS
  ownership.
- Component docs need the missing Settings reference page.
- BMAD dev, QA automation, and review instructions need a mechanical artifact-validation guard.

Technical impact:

- Add a local validator for raw authoring sentinels and optional changed-file vs story File List
  reconciliation.
- Remove known raw sentinel leaks from existing markdown artifacts.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Scope classification: Moderate.

Rationale:

- The work is bounded to planning/docs/process artifacts and does not require reopening completed
  stories.
- A small validator plus workflow gates directly addresses the repeated failure class.
- Residual product/UX wording questions remain explicit sprint actions rather than hidden assumptions.

## 4. Implemented Changes

### 4.1 Artifact Hygiene

- Removed raw authoring-sentinel tails from:
  - `_bmad-output/implementation-artifacts/1-1-bootstrap-a-minimal-bootable-shell.md`
  - `_bmad-output/implementation-artifacts/tests/1-3-test-summary.md`
- Added `eng/validate-story-artifacts.py` to scan `_bmad-output` and `docs` for raw authoring
  sentinels and, when story context is supplied, compare changed files with the story File List.

### 4.2 Workflow Gates

- Updated `.agents/skills/bmad-dev-story/SKILL.md` and checklist guidance so story completion runs
  `python3 eng/validate-story-artifacts.py`.
- Updated `.agents/skills/bmad-qa-generate-e2e-tests/SKILL.md` so QA-generated artifacts are recorded
  in active story File Lists and validated.
- Updated `.agents/skills/bmad-story-automator-review/instructions.xml` and checklist guidance so review
  treats validator failures as findings.

### 4.3 Contract and Sprint Tracking

- Added dated follow-up tracking to:
  - `_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md`
  - `_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md`
  - `_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md`
  - `_bmad-output/contracts/fc-settings-persistence-2026-06-03.md`
- Added sprint-status actions for:
  - mechanical File List/test-summary reconciliation
  - durable authoring-sentinel guarding
  - residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions
  - closing the settings component page gap

### 4.4 Component Documentation

- Added `docs/reference/components/settings.md`.
- Updated `docs/reference/components/index.md` so Settings is listed and the prior read-only-MVP
  component-doc gap is closed.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering artifact identified | [x] | Epic 1 retrospective dated 2026-06-03. |
| 1.2 Core problem defined | [x] | Artifact drift and residual documentation/process follow-up. |
| 1.3 Evidence gathered | [x] | Retro, story/test summaries, sprint status, contracts, and component docs checked locally. |
| 2.1 Epic impact assessed | [x] | Epic 1 stays done; no runtime rollback. |
| 2.2 Story impact assessed | [x] | No completed story reopened. Future story gates updated. |
| 2.3 Artifact impact assessed | [x] | Contracts, sprint status, component docs, and BMAD workflows updated. |
| 3.1 PRD impact assessed | [N/A] | No authored PRD exists; readiness request/epics remain the requirements proxy. |
| 3.2 Architecture impact assessed | [x] | No architecture invariant changed. |
| 3.3 UX impact assessed | [x] | Settings documentation closes the visible FC-DOC gap; residual wording tracked. |
| 4.1 Direct adjustment evaluated | [x] | Selected and applied. |
| 4.2 Rollback evaluated | [N/A] | No implementation rollback needed. |
| 4.3 MVP review evaluated | [N/A] | MVP scope is unaffected. |
| 5.1 Proposal created | [x] | This file records the approved Epic 1 change. |
| 5.2 Approval captured | [x] | Approved by Administrator on 2026-07-01. |
| 5.3 Implementation applied | [x] | See Section 4. |
| 5.4 Verification run | [x] | Artifact validator and diff hygiene checks run; docs validation has known external blockers. |

## 6. Implementation Result

Route outcome:

- Developer / QA automation: validator and workflow gates implemented.
- Technical Writer: Settings component reference page added and FC-DOC gap closed.
- Product/UX/Tenants owners: residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions remain
  tracked as open sprint action items.

Success criteria:

- Known raw authoring-sentinel leaks are removed.
- Future story completion/review has a mechanical guard for raw sentinels and File List drift.
- Settings is present in published component docs.
- Residual wording questions are explicit and owned rather than implicit.

## 7. Approval Record

Approved by Administrator on 2026-07-01.

Approved path: Continue. The planning/documentation/process updates above were applied.
