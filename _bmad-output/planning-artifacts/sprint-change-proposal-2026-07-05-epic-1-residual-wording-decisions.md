---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: "Drive residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions to confirmed or dated owned follow-up."
status: approved
scope: Minor
owner: FrontComposer + Product/UX + Tenants author
approval: approved-by-administrator-2026-07-05
---

# Sprint Change Proposal - Epic 1 Residual Wording Decisions

## Section 1 - Issue Summary

Epic 1 was marked done, but sprint status still carried one residual action:

> Drive residual FC-A11Y, FC-L10N, FC-DOC, and FC-SETTINGS wording decisions to confirmed or dated owned follow-up.

The issue is not missing implementation. The four contract artifacts already document the shipped behavior and test evidence, but several front-matter and confirmation sections still used `escalated` / `pending` wording from the original June 3 confirm-and-document pass. Under the 2026-06-21 contract-confirmation Definition of Done, "escalated with an owner" alone is not enough. Each residual decision must either be confirmed or routed to a dated owner.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is an Epic 1 retrospective action item, not a failed implementation story. |
| 1.2 Core problem | [x] | Residual wording still implied pending decisions even where behavior and evidence were already settled. |
| 1.3 Evidence | [x] | Evidence comes from the four FC contract artifacts, published component docs, accessibility evidence pack, `epics.md`, and `sprint-status.yaml`. |
| 2.1 Current epic impact | [x] | Epic 1 remains done; no story is reopened. |
| 2.2 Epic-level changes | [x] | Add an Epic 1 disposition note to `epics.md`. |
| 2.3 Future epic impact | [x] | Future stories keep using the confirmed FC gates. FC-A11Y visual/manual evidence remains a release-readiness follow-up. |
| 2.4 New/remove epics | [x] | No new epic or story required. |
| 2.5 Priority/order | [x] | Close the sprint action now; FC-A11Y visual/manual sign-off remains due before v1.0 RC readiness classification. |
| 3.1 PRD conflicts | [N/A] | PRD decision register does not need a new product decision. |
| 3.2 Architecture conflicts | [N/A] | No architecture change. |
| 3.3 UX conflicts | [x] | FC-A11Y visual/manual release sign-off remains owned by Product/UX + Release Owner. |
| 3.4 Other artifacts | [x] | Contract artifacts, `epics.md`, and `sprint-status.yaml` updated. |
| 4.1 Direct adjustment | Viable | Recommended. |
| 4.2 Rollback | Not viable | No implementation rollback needed. |
| 4.3 MVP review | Not viable | MVP/v1 scope unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] | Captured below. |
| 6.1-6.2 Final review | [x] | Proposal is specific and actionable. |
| 6.3 Approval | [x] | Approved by Administrator on 2026-07-05. |
| 6.4 Sprint status update | [x] | Epic 1 residual action marked done with evidence. |
| 6.5 Handoff | [x] | Minor scope: no implementation story; Release Owner carries only the FC-A11Y visual/manual release evidence follow-up. |

## Section 3 - Recommended Approach

Use **Direct Adjustment**.

Rationale:

- The behavior is already implemented and tested; this is a planning/status correction.
- Marking every item merely `done` would hide the one real release-level follow-up: visual/manual accessibility sign-off.
- Creating a new epic or reopening Epic 1 would add churn without changing product scope.

Effort estimate: Low.
Risk level: Low.
Timeline impact: none for implementation. FC-A11Y release-level sign-off remains due before v1.0 RC readiness classification.

## Section 4 - Detailed Change Proposals

### FC-A11Y

Old state:

```text
status: escalated
Open: Is the three-layer gate complete? Who signs off visual-design accessibility?
```

New state:

```text
status: confirmed-with-release-follow-up
Confirmed: the story ready-gate is the three-layer automated gate: shell-frame bUnit invariants, HFC1050-HFC1055 override diagnostics, and CI-owned axe/browser specimen lane.
Follow-up: Product/UX + Release Owner own visual/manual accessibility sign-off before v1.0 RC readiness classification, tracked through the accessibility evidence pack.
```

### FC-L10N

Old state:

```text
status: escalated
Open: density-preview sample-string scope and domain-label fallback ownership.
```

New state:

```text
status: confirmed
Confirmed: density-preview sample strings are out of FC-L10N scope; domain labels are host-owned via Display/localizer and the shell provides no domain-label fallback.
```

### FC-DOC

Old state:

```text
status: escalated
Open: cross-link convention; settings page scope.
```

New state:

```text
status: confirmed
Confirmed: published pages summarize contract behavior inline, link only to published siblings, and this contract ledger records the _bmad-output mapping. DataGrid and settings docs are authored.
```

### FC-SETTINGS

Old state:

```text
status: confirmed, with literal AC3 wording escalated.
```

New state:

```text
status: confirmed
Confirmed: AC3 means one persistence writer per slice plus one DOM writer per side-effect.
```

## Section 5 - Implementation Handoff

Scope classification: Minor.

Handoff:

- Developer / planning maintainer: use the updated contract artifacts and `epics.md` note as the source for future story creation.
- Release Owner + Product/UX: carry only the FC-A11Y visual/manual evidence gate before v1.0 RC readiness classification.

Success criteria:

- No FC-A11Y, FC-L10N, FC-DOC, or FC-SETTINGS contract remains merely `escalated` because of wording.
- Sprint status no longer lists the Epic 1 residual action as open.
- Any remaining non-automated accessibility judgment is dated and owned rather than hidden as "pending."
