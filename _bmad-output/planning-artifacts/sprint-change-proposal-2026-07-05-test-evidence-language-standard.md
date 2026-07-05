---
title: 'Sprint Change Proposal - Test evidence language standard'
date: '2026-07-05'
author: 'Administrator (correct-course, Developer role)'
trigger: 'Process action: Standardize local-blocker vs CI-authoritative test evidence language'
mode: 'Batch'
scope_classification: 'Minor'
status: 'approved'
approved_by: 'Administrator'
approved_at: '2026-07-05T11:01:39+02:00'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-3-retro-followthrough.md'
---

# Sprint Change Proposal - Test Evidence Language Standard

## Section 1 - Issue Summary

The approved Epic 3 retrospective follow-through created a process action to standardize test evidence
language across story records and test summaries. The trigger was recurring evidence drift: local
VSTest/MSBuild socket or named-pipe blockers, NuGet/network blockers, direct xUnit v3 in-process
fallback runs, and Playwright/Kestrel/browser blockers were recorded with similar intent but inconsistent
wording.

Concrete evidence already existed in Story 10.3, Story 10.5, Epic 7/Epic 8 retrospectives, and the
aggregate test summary. Newer records generally made the right distinction, but the reusable templates did
not yet enforce the vocabulary everywhere.

## Section 2 - Impact Analysis

Epic impact: no product epic changes. Epic 3, Epic 4, Epic 5, Epic 8, and Epic 10 remain valid.

Story impact: future story records and test summaries now use one evidence shape for blocked exact lanes,
fallback lanes, and CI-authoritative gates. Historical story records are not mass-rewritten.

Artifact impact:

- `.agents/skills/bmad-dev-story/SKILL.md` now names VSTest/MSBuild socket blockers, NuGet/network
  blockers, direct xUnit v3 fallback evidence, and Playwright/Kestrel/browser CI handoffs explicitly.
- `.agents/skills/bmad-story-automator-review/checklist.md` validates the same separation during review.
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` defines the promotion
  rule for story records.
- `tests/README.md` defines the standard lane table and vocabulary.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` carries the standard test-summary table.
- `docs/accessibility-verification/README.md` and
  `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md` clarify browser CI-gate
  handoffs.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` closes the process action with evidence.

Technical impact: documentation/process only. No source code, packages, generated output, or submodule
changes.

## Section 3 - Recommended Approach

Recommended path: Direct Adjustment.

Rollback is not useful because the issue is wording drift, not an implementation defect. MVP review is not
needed because no scope or product requirement changes. The durable fix is to close the already-approved
process action by putting the evidence vocabulary in the places agents actually read before story
promotion, review, and test-summary authoring.

Risk is low. The main risk is overclaiming fallback evidence, so the template explicitly forbids marking a
blocked exact lane as passed because a fallback lane passed.

## Section 4 - Detailed Change Proposals

### Story workflow wording

OLD:

```markdown
- required command attempted
- local result: Passed, Failed, or Blocked with exact blocker
- fallback evidence command/result when local exact lane is blocked
- CI authority: Required, Advisory, or Not applicable
- blocker timing: before test execution or inside the test body
```

NEW:

```markdown
- required command attempted
- local result: Passed, Failed, or Blocked with exact blocker
- blocker timing: before test execution, before browser assertions, or inside the test body
- fallback evidence command/result when local exact lane is blocked
- CI authority: Required, Advisory, or Not applicable
- VSTest/MSBuild socket or named-pipe blockers are local blockers only; direct xUnit v3 in-process evidence
  is fallback unless it is the required lane.
- NuGet/package/network blockers name the blocked service or URI.
- Playwright/Kestrel/browser blockers name fallback evidence and the CI browser/a11y/visual lane owner and
  expected artifact path.
```

Rationale: makes the exact local-blocker vs fallback vs CI-authority split machine-checkable during review.

### Story review checklist

OLD:

```markdown
Record test-count deltas and any pre-existing failing lanes.
```

NEW:

```markdown
Record test-count deltas and any pre-existing failing lanes.
Enforce the standard Test Evidence language with Lane, Required command, Local result, Blocker timing,
Fallback evidence, and CI authority.
```

Rationale: promotion to done now has an explicit evidence-language gate.

### Test summary template

OLD:

```markdown
# Test Automation Summary
```

NEW:

```markdown
# Test Automation Summary

## Evidence Language Standard

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
```

Rationale: new test-summary sections start from the standardized table rather than ad hoc prose.

### Browser handoff wording

OLD:

```markdown
If local Playwright/Kestrel is blocked, record the CI lane, owner, and expected artifact path.
```

NEW:

```markdown
If local Playwright/Kestrel is blocked, record the required command, blocker timing, fallback evidence,
CI lane, owner, and expected artifact path.
```

Rationale: browser CI handoffs need the same exact-command/local-result/fallback fields as .NET lanes.

## Section 5 - Implementation Handoff

Scope classification: Minor.

Route to: Developer agent / Test Architect.

Success criteria:

- Future story records and test summaries can use one table for exact local result, blocker timing,
  fallback evidence, and CI authority.
- VSTest/MSBuild socket or named-pipe blockers are not confused with passing VSTest evidence.
- Direct xUnit v3 in-process runs are recorded as fallback evidence unless required by the story.
- Playwright/Kestrel/browser blockers include a named CI lane, owner, and artifact path when browser proof
  remains required.
- Sprint status records the process action as done with evidence.

## Checklist Results

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering issue | Done | Process action from approved Epic 3 follow-through. |
| 1.2 Core problem | Done | Evidence wording drift across local blockers, fallback runs, and CI gates. |
| 1.3 Evidence | Done | Existing story/test-summary records include VSTest socket, NuGet/network, xUnit fallback, and Playwright/Kestrel blockers. |
| 2.1 Epic viability | Done | Product epics remain valid. |
| 2.2 Epic changes | N/A | No epic scope changes. |
| 2.3 Future impact | Done | Future story/test-summary evidence now has a standard vocabulary. |
| 2.4 New epic needed | N/A | Process action only. |
| 2.5 Priority/order | Done | Implemented before closing the sprint-status action. |
| 3.1 PRD conflict | N/A | No product requirement change. |
| 3.2 Architecture conflict | N/A | No architecture change. |
| 3.3 UI/UX conflict | N/A | Browser evidence handoff only. |
| 3.4 Other artifacts | Done | Workflow, review, test-summary, accessibility, and sprint-status artifacts updated. |
| 4.1 Direct adjustment | Done | Recommended and applied. |
| 4.2 Rollback | N/A | Not useful. |
| 4.3 MVP review | N/A | Not required. |
| 4.4 Selected path | Done | Direct Adjustment. |
| 5.1-5.5 Proposal components | Done | Captured above. |
| 6.1-6.2 Final review | Done | Process-only, actionable, and scoped. |
| 6.3 Approval | Done | Approved explicitly by Administrator on 2026-07-05 at 11:01:39 +02:00. |
| 6.4 Sprint-status update | Done | Process action moved to done with evidence. |
| 6.5 Handoff | Done | Developer/Test Architect process ownership. |

## Completion

Correct Course workflow complete, Administrator.
