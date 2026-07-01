---
title: 'Sprint Change Proposal - Epic 3 retrospective follow-through'
date: '2026-07-01T17:39:14+02:00'
author: 'Administrator (correct-course, Developer role)'
trigger: '_bmad-output/implementation-artifacts/epic-3-retro-2026-06-04.md'
mode: 'Batch (assumed; no live mode selection supplied)'
scope_classification: 'Moderate (process and evidence hardening; no product replan)'
status: 'approved'
approved_by: 'Administrator'
approved_at: '2026-07-01T17:39:14+02:00'
artifacts_reviewed:
  - '_bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md'
  - '_bmad-output/project-docs/architecture.md'
  - '_bmad-output/project-docs/api-contracts.md'
  - '_bmad-output/project-docs/component-inventory.md'
  - '_bmad-output/project-docs/data-models.md'
  - '_bmad-output/implementation-artifacts/epic-3-retro-2026-06-04.md'
  - '_bmad-output/implementation-artifacts/epic-4-retro-2026-06-05.md'
  - '_bmad-output/implementation-artifacts/epic-5-retro-2026-06-05.md'
  - '_bmad-output/implementation-artifacts/epic-8-retro-2026-06-25.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
---

# Sprint Change Proposal - Epic 3 retrospective follow-through

**Date:** 2026-07-01  
**Trigger:** Epic 3 retrospective, dated 2026-06-04  
**Mode:** Batch, because no live mode preference was supplied  
**Status:** Approved by Administrator on 2026-07-01

## Section 1 - Issue Summary

The Epic 3 retrospective did not identify a product-direction failure. It identified five follow-through
items from the completed Command Authoring & Lifecycle epic:

- E3-AI-1: add automated File List reconciliation.
- E3-AI-2: update brownfield docs that still describe command output, lifecycle, or polling using
  pre-Epic-3 wording.
- E3-AI-3: add an FC-CNC decision note before Story 4.3 implementation.
- E3-AI-4: preserve MessageId-first resolution in Epic 4 retry/concurrency work.
- E3-AI-5: normalize test evidence language for socket/network-blocked local runs.

Current repository state shows that E3-AI-2, E3-AI-3, and E3-AI-4 are closed by later work. Epic 4
recorded the FC-CNC block-not-queue contract, kept retry pre-accept only, reused the same MessageId,
and updated project docs. The June 21 readiness proposal also ratified AR8 budget values and accepted
the Epic 3/4 split.

The remaining issue is process evidence hygiene: File List/story evidence reconciliation remains manual,
and local test-blocker language is still convention-based rather than standardized. This same weakness
continues through Epic 4, Epic 5, and Epic 8 retrospectives.

There is no authored PRD in the planning folder. This analysis uses the `epics.md` requirements
inventory and `frontcomposer-readiness-request-2026-06-03.md` as the PRD surrogate, matching the
source caveat already recorded in `epics.md`.

## Section 2 - Impact Analysis

### Epic Impact

No product epic is invalidated.

- Epic 3 remains done: command generation, FC-CMD identity/correlation, lifecycle UI, EventStore
  status binding, and polling budgets are implemented and documented.
- Epic 4 remains done: destructive confirmation, abandonment guard, FC-CNC, authorization, and
  retry/degraded behavior are implemented and documented.
- Epic 5 and Epic 8 remain done.
- No epic should be added, removed, renumbered, or resequenced.

The open impact is cross-epic process ownership, not feature scope.

### Story Impact

No completed story should be reopened. The proposed adjustment affects future workflow gates and
evidence templates:

- Story files should not reach review with missing changed files in `### File List`.
- QA-generated E2E specs, helper files, package changes, and test summaries should be reconciled
  mechanically.
- Test evidence should distinguish CI-authoritative lanes, local blockers, and local fallbacks in a
  consistent format.

### Artifact Conflicts

| Artifact | Current state | Proposed resolution |
|---|---|---|
| `epic-3-retro-2026-06-04.md` | E3-AI-1 and E3-AI-5 remain only as retro action items | Route them into current process action items |
| `epic-4-retro-2026-06-05.md` | Confirms E3-AI-1 not solved; E3-AI-5 mostly completed but manual | Use this as evidence for a durable workflow change |
| `epic-5-retro-2026-06-05.md` | Confirms File List drift recurred, including a critical review finding | Reinforce reconciliation as a required review gate |
| `epic-8-retro-2026-06-25.md` | Has open E8-AI-3 and E8-AI-5 process/evidence items | Extend these rather than creating a separate product epic |
| `sprint-status.yaml` | Current open action items are Epic 8-scoped only | Add process-level action for standardized test evidence language after approval |
| `sprint-change-proposal-2026-07-01.md` | Already proposes process File List/sentinel hardening for Epic 1 follow-through | Merge or cross-reference this proposal if both are approved |

### Technical Impact

No Shell runtime, SourceTools generator, MCP, EventStore, Tenants, schema fingerprint, package, or
submodule change is required by this proposal.

If the mechanical reconciliation validator from the existing July 1 Epic 1 proposal is approved, it
should satisfy E3-AI-1 as well. This Epic 3 proposal adds the missing E3-AI-5 part: a standard test
evidence template and ownership for local-blocker wording.

## Section 3 - Recommended Approach

**Recommended path: Direct Adjustment with process handoff.**

Rollback is not useful; the command lifecycle implementation is already correct and later epics proved
the intended boundaries. MVP review is not required; the requirements inventory remains valid, and the
June 21 proposal already resolved confirmation debt around AR8 and the Epic 3/4 split.

The right path is to route the two remaining process gaps into the active process backlog:

1. Treat File List/story evidence reconciliation as a shared process action, not an Epic 3-only task.
2. Add a standard test evidence language template that future story records must use when exact local
   lanes are blocked by sockets, named pipes, NuGet/network access, Playwright/Kestrel startup, or
   platform-specific visual baselines.

Effort is low-to-medium. Risk is low if the wording template remains descriptive and does not turn
environment-specific fallback evidence into a false pass.

## Section 4 - Detailed Change Proposals

### 4.1 Sprint action item for test evidence language

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`  
**Section:** `action_items`

OLD:

```yaml
action_items:
  - epic: 8
    action: "Add a reusable visual-component checklist that requires rendered-DOM or computed-style proof for Fluent component layout/CSS changes"
    owner: "Test Architect"
    status: open
  - epic: 8
    action: "Keep shell/navigation reference docs synchronized with Story 8.3 and Story 8.5 public-surface changes"
    owner: "Technical Writer"
    status: open
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
  - epic: 8
    action: "Preserve the accent-as-thread guard as a mandatory Shell governance lane for future chrome work"
    owner: "Architect"
    status: open
  - epic: 8
    action: "Track browser/visual evidence gaps as named CI responsibilities rather than local unknowns"
    owner: "QA Engineer"
    status: open
```

NEW:

```yaml
action_items:
  - epic: 8
    action: "Add a reusable visual-component checklist that requires rendered-DOM or computed-style proof for Fluent component layout/CSS changes"
    owner: "Test Architect"
    status: open
  - epic: 8
    action: "Keep shell/navigation reference docs synchronized with Story 8.3 and Story 8.5 public-surface changes"
    owner: "Technical Writer"
    status: open
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
  - epic: 8
    action: "Preserve the accent-as-thread guard as a mandatory Shell governance lane for future chrome work"
    owner: "Architect"
    status: open
  - epic: 8
    action: "Track browser/visual evidence gaps as named CI responsibilities rather than local unknowns"
    owner: "QA Engineer"
    status: open
  - epic: process
    action: "Standardize local-blocker vs CI-authoritative test evidence language in story records and test summaries, including VSTest socket/network blockers, xUnit in-process fallbacks, and Playwright/browser CI-gate handoffs"
    owner: "Test Architect"
    status: open
```

Rationale: E3-AI-5 was mostly handled manually in later stories, but it is not yet represented as a
durable process action.

### 4.2 Story/test-summary evidence template

**Artifact:** story creation/review guidance or the story-automator workflow documentation  
**Section:** test evidence recording

OLD:

```markdown
- Record test commands and blockers in story notes.
```

NEW:

```markdown
### Test Evidence

| Lane | Required command | Local result | Fallback evidence | CI authority |
|---|---|---|---|---|
| Solution default | `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` | Passed / Failed / Blocked with exact blocker | Direct xUnit lane, focused project lane, typecheck, or N/A | Required / Advisory / Not applicable |

When a lane is blocked locally, record:
- exact command attempted
- exact blocker class and message
- whether the blocker occurs before test execution or inside the test body
- focused fallback command and result
- why CI remains authoritative for the skipped exact lane
```

Rationale: This converts the Epic 3/Epic 4 "local solution-level VSTest remained environment-sensitive"
pattern into a consistent evidence shape.

### 4.3 Merge with existing July 1 process proposal

**Artifact:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md`

If the Epic 1 follow-through proposal is approved first, merge this proposal's E3-AI-5 evidence-language
action into that proposal's Section 4.2 / Section 4.3 process hardening rather than creating duplicate
parallel process actions.

Rationale: E3-AI-1 overlaps the Epic 1/Epic 8 File List reconciliation work. One process fix should
satisfy all retrospectives that found the same defect class.

### 4.4 No PRD, epic, or architecture text changes

No edits are proposed for `epics.md`, the PRD surrogate, or architecture docs:

- AR7 FC-CNC is already owned by Epic 4 and implemented as block-not-queue.
- AR8 budgets are already confirmed as of 2026-06-21.
- The Epic 3/4 split is already accepted as the final v1 structure.
- `architecture.md` and `api-contracts.md` already describe current command safety ordering, FC-CNC,
  retry boundaries, pending polling, and MessageId/correlation identity.

Rationale: Updating these artifacts again would create churn without new information.

## Section 5 - Implementation Handoff

**Scope classification:** Moderate.

This does not need PM or Architect replanning. It needs Developer/Test Architect/QA automation
coordination because the durable fix is workflow behavior, not product code.

### Handoff Recipients

- **Test Architect:** define and approve the standardized test evidence template.
- **QA automation maintainer:** ensure the mechanical File List/story-task reconciliation action
  satisfies Epic 3, Epic 4, Epic 5, and Epic 8 recurrence, not just the latest epic.
- **Developer agent:** after approval, add the `sprint-status.yaml` process action and update workflow
  guidance/templates where story evidence is recorded.

### Success Criteria

- E3-AI-1 is satisfied by a mechanical changed-file/story File List reconciliation gate or explicitly
  tracked by the shared process action until implemented.
- E3-AI-5 is satisfied by a standard evidence template used in future story records and test summaries.
- Local blockers are recorded with exact commands, exact blocker messages, fallback command/results,
  and CI-authority status.
- No product epic is reopened or restructured.
- No duplicate process action is created if the existing July 1 Epic 1 proposal is approved and used
  as the shared process-hardening vehicle.

## Checklist Results

| Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story/issue | Done | Trigger is the Epic 3 retrospective and E3-AI-1 through E3-AI-5. |
| 1.2 Core problem | Done | Process evidence hygiene, not a command-runtime defect. |
| 1.3 Evidence | Done | Epic 4/5/8 retros show File List drift and environment-sensitive test evidence remained recurring. |
| 2.1 Current epic viability | Done | Epic 3 remains complete as originally planned. |
| 2.2 Epic-level changes | Done | No product epic changes; process action only. |
| 2.3 Future epic impact | Done | Future stories should use mechanical reconciliation and standard blocker language. |
| 2.4 New epic needed | N/A | No new product epic required. |
| 2.5 Priority/order | Done | Process hardening should happen before the next automated story run. |
| 3.1 PRD conflict | Done | No authored PRD exists; PRD surrogate remains valid. |
| 3.2 Architecture conflict | Done | No architecture change needed; current docs already reflect command safety contracts. |
| 3.3 UI/UX conflict | N/A | No UI/UX spec update needed. |
| 3.4 Other artifacts | Action-needed | Sprint status and workflow guidance should be updated after approval. |
| 4.1 Direct adjustment | Viable | Recommended. |
| 4.2 Rollback | Not viable | Runtime work is correct and later epics depend on it. |
| 4.3 MVP review | Not viable | MVP and roadmap remain valid. |
| 4.4 Selected path | Done | Direct Adjustment with process handoff. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact and artifact needs | Done | Included above. |
| 5.3 Recommended path | Done | Included above. |
| 5.4 MVP/action plan | Done | MVP unchanged; action plan listed. |
| 5.5 Handoff | Done | Included above. |
| 6.1 Checklist completion | Done | All applicable items addressed. |
| 6.2 Proposal accuracy | Done | Based on loaded artifacts and current repository state. |
| 6.3 User approval | Action-needed | Awaiting explicit approval. |
| 6.4 Sprint-status update | N/A until approved | Proposed action item only; no status changes before approval. |
| 6.5 Handoff confirmation | Action-needed | Awaiting approval/route confirmation. |

## Approval Recorded

Approved by Administrator. Implementation updates applied:

- Added the process-level test evidence action item to `sprint-status.yaml`.
- Updated `bmad-dev-story` completion guidance and checklist so local blockers, fallback evidence,
  and CI authority must be recorded consistently.
- Updated story-automator review validation to check the standardized test evidence language.
