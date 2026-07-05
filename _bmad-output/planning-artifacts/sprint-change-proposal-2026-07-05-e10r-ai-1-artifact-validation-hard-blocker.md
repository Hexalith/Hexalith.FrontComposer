---
title: "Sprint Change Proposal - E10R-AI-1 Artifact Validation Hard Blocker"
date: "2026-07-05"
status: approved
change_trigger: "E10R-AI-1"
scope: minor
mode: batch
owner: "QA automation maintainer"
---

# Sprint Change Proposal - E10R-AI-1 Artifact Validation Hard Blocker

## 1. Issue Summary

Epic 10 closed the mechanical story evidence reconciliation work and wired story-automator review completion so a failing story artifact validator returns `artifact_validation_failed` instead of allowing a `done` story status to pass. The Epic 10 retrospective added follow-up `E10R-AI-1`: keep `artifact_validation_failed` as a hard review-completion blocker and track validator false positives as validator fixes rather than manual bypasses.

The triggering risk is process erosion, not a missing runtime feature. If a reviewer treats a suspected false positive as permission to edit story status, sprint status, or review policy by hand, the Story 10.1 gate becomes advisory again. The expected behavior is fail-closed: review completion stays blocked until the validator is corrected, covered, and rerun successfully.

Evidence reviewed:

- `_bmad-output/implementation-artifacts/epic-10-retro-2026-07-05.md` records `E10R-AI-1` and names the success criterion.
- `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md` records the original anti-pattern: a `done` status must not bypass reconciliation.
- `.agents/skills/bmad-story-automator/src/story_automator/core/success_verifiers.py` already runs `_artifact_validation_gate` on done paths and returns `reason: artifact_validation_failed` on non-zero validator exit.
- `.agents/skills/bmad-story-automator-review/instructions.xml`, `.agents/skills/bmad-story-automator-review/checklist.md`, and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` require the validator, but they should make the false-positive handling rule explicit.

## 2. Impact Analysis

Epic impact: Epic 10 remains done. This proposal adds follow-through to an open Epic 10 retrospective action item. It does not reopen Stories 10.1 through 10.5 and does not require a new product epic.

Story impact: No existing story acceptance criteria need to change. A small process/tooling implementation task should update review guidance and, where practical, add a governance regression that proves the instructions still forbid manual bypass. The open `E10R-AI-1` action item should move to `done` only after evidence paths are recorded.

PRD impact: No PRD text change is required. FR-27 already covers tooling-governance follow-through, and this change stays inside that requirement.

Architecture impact: No architecture change is required. The review gate is repository process tooling, not product runtime architecture.

UX impact: No UX artifact change is required.

Technical impact:

- Update review workflow/checklist text to distinguish environmental blockers from validator findings.
- Preserve `success_verifiers.py` fail-closed behavior.
- Treat any validator false positive as work against `eng/validate-story-artifacts.py` and its focused tests.
- Avoid adding a bypass flag, allowlist, or review-contract option that can mark a story done while `artifact_validation_failed` is present.

## 3. Checklist Results

| Item | Status | Finding |
|---|---|---|
| 1.1 Triggering story | Done | Trigger is Epic 10 retrospective action `E10R-AI-1`, rooted in Story 10.1 review-gate behavior. |
| 1.2 Core problem | Done | Process/tooling risk: false positives could be handled by manual status bypass instead of validator fixes. |
| 1.3 Evidence | Done | Retrospective, Story 10.1, review instructions, checklist, and verifier code all reviewed. |
| 2.1 Current epic | Done | Epic 10 stays complete; this is retrospective follow-through. |
| 2.2 Epic-level changes | Done | No new epic. Keep action item under Epic 10. |
| 2.3 Future epics | Done | Epic 11 benefits from the rule but does not need story rewrites. |
| 2.4 New/obsolete epics | N/A | No epic added or removed. |
| 2.5 Priority/order | N/A | No epic resequencing. |
| 3.1 PRD conflicts | Done | No conflict; FR-27 already covers this class. |
| 3.2 Architecture conflicts | N/A | No architecture artifact update needed. |
| 3.3 UX conflicts | N/A | No UX artifact update needed. |
| 3.4 Other artifacts | Done | Review workflow, checklist, validator tests, and sprint status action tracking are affected. |
| 4.1 Direct adjustment | Viable | Low effort, low risk. |
| 4.2 Rollback | Not viable | Reverting Story 10.1 would remove the gate and increase risk. |
| 4.3 MVP review | Not viable | MVP scope is unchanged. |
| 4.4 Recommendation | Done | Direct adjustment. |
| 5.1-5.5 Proposal components | Done | Included below. |
| 6.1-6.2 Final review | Done | Proposal is internally consistent and actionable. |
| 6.3 User approval | Done | Approved by Administrator on 2026-07-05. |
| 6.4 Sprint status | Done | `E10R-AI-1` closes only after implementation evidence paths are recorded. |
| 6.5 Handoff | Done | Route to Developer / QA automation maintainer. |

## 4. Recommended Approach

Choose Direct Adjustment.

This is a minor process hardening change. The gate already exists in code; the remaining work is to make the false-positive rule explicit in durable review instructions and action tracking. No rollback, MVP review, or architecture replan is justified.

Effort estimate: Low.

Risk: Low. The main risk is wording ambiguity. The implementation should not add any bypass switch or policy knob. It should only clarify that a validator finding blocks done, and a false positive becomes a validator defect with a regression test.

Timeline impact: Minimal. This can be implemented directly before the next review automation run that relies on the gate.

## 5. Detailed Change Proposals

### Proposal A - Story Review Reconciliation Checklist

Artifact: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`

Section: Review promotion rule.

OLD:

```markdown
9. Do not promote the story until File List, task claims, documentation sweep, and verification evidence agree.
```

NEW:

```markdown
9. Do not promote the story until File List, task claims, documentation sweep, and verification evidence agree.
10. Treat `artifact_validation_failed` as a hard review-completion blocker. If the validator appears wrong, keep the story out of `done`, fix `eng/validate-story-artifacts.py` or its tests, rerun the validator, and record the fix evidence. Do not manually bypass the failure by editing story status, sprint status, or review policy.
```

Rationale: The current checklist says not to promote on disagreement, but it does not explicitly define the false-positive route. This adds the missing fail-closed rule without changing product scope.

### Proposal B - Review Skill Checklist

Artifact: `.agents/skills/bmad-story-automator-review/checklist.md`

Section: Mechanical validation checklist item.

OLD:

```markdown
- [ ] `python3 eng/validate-story-artifacts.py --story {{story_path}}` passed, or exact blocker recorded
- [ ] Mechanical reconciliation gate enforced before `done`: a non-zero validator exit keeps the story `in-progress` regardless of CRITICAL count (matches `review_completion` `artifact_validation_failed`)
```

NEW:

```markdown
- [ ] `python3 eng/validate-story-artifacts.py --story {{story_path}}` passed. If the command cannot execute, record the exact environmental blocker.
- [ ] Mechanical reconciliation gate enforced before `done`: a non-zero validator exit is `artifact_validation_failed` and keeps the story `in-progress` regardless of CRITICAL count.
- [ ] Any suspected validator false positive is tracked as a validator fix with regression evidence; it is not a manual bypass path.
```

Rationale: The phrase "or exact blocker recorded" can be read too broadly. It should cover command execution blockers, not validator findings.

### Proposal C - Review Workflow Instructions

Artifact: `.agents/skills/bmad-story-automator-review/instructions.xml`

Section: Step 5, status update and sprint tracking.

OLD:

```xml
<action>Set {{gate_passed}} = true when the validator exits 0, false otherwise.</action>
```

NEW:

```xml
<action>Set {{gate_passed}} = true when the validator exits 0, false otherwise.</action>
<action>If the validator exits non-zero and the result appears to be a false positive, keep {{new_status}} out of `done`; track and implement a validator fix with regression evidence, then rerun the validator. Do not manually bypass `artifact_validation_failed` by editing story status, sprint status, or review policy.</action>
```

Rationale: Review instructions already run the gate, but the false-positive rule should be embedded where status is decided.

### Proposal D - Verifier Regression Coverage

Artifact: `eng/tests/test_validate_story_artifacts.py`

Section: Review verifier tests.

OLD:

Existing coverage proves `artifact_validation_failed` prevents done review completion when the validator exits non-zero.

NEW:

Add or preserve a focused regression asserting all of these outcomes:

```text
Given sprint status or story status already says done,
When the validator exits non-zero,
Then review_completion returns verified=false and reason=artifact_validation_failed,
And no contract/source-order setting used by normal review completion marks the story verified.
```

Rationale: The code path is already correct today. The action item closes only when the regression evidence remains present and the review text forbids manual bypass.

### Proposal E - Sprint Status Action Tracking

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `action_items`, `E10R-AI-1`.

OLD:

```yaml
- epic: 10
  action: "E10R-AI-1: Keep artifact_validation_failed as a hard review-completion blocker and track false positives as validator fixes rather than manual bypasses"
  owner: "QA automation maintainer"
  status: open
```

NEW after implementation:

```yaml
- epic: 10
  action: "E10R-AI-1: Keep artifact_validation_failed as a hard review-completion blocker and track false positives as validator fixes rather than manual bypasses"
  owner: "QA automation maintainer"
  status: done
  closed: "2026-07-05"
  evidence:
    - "_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md states that artifact_validation_failed is a hard blocker and false positives require validator fixes."
    - ".agents/skills/bmad-story-automator-review/checklist.md distinguishes environmental command blockers from non-zero validator exits."
    - ".agents/skills/bmad-story-automator-review/instructions.xml keeps false-positive handling out of the manual bypass path."
    - "eng/tests/test_validate_story_artifacts.py preserves review_completion artifact_validation_failed regression coverage."
```

Rationale: Do not close the action item until the durable guidance and regression evidence are in place.

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent / QA automation maintainer.

Responsibilities:

- Developer: apply the checklist and instruction wording changes, preserving existing review automation behavior.
- QA automation maintainer: verify or add focused regression coverage for the fail-closed verifier path and any validator false-positive fix path.
- Product Owner: approve the proposal before implementation and accept the action-item closure evidence.

Success criteria:

- A story cannot finish review when `review_completion` returns `artifact_validation_failed`.
- Review guidance explicitly says validator false positives are fixed in the validator and tests, not bypassed manually.
- No bypass flag, allowlist, status edit, or review-contract escape hatch is introduced.
- `E10R-AI-1` is marked done only with evidence paths in `sprint-status.yaml`.

## 7. Approval

Approval status: approved by Administrator on 2026-07-05.

Implementation handoff: Minor scope routed to Developer / QA automation maintainer for direct application and evidence recording.
