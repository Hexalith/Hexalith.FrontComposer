# Sprint Change Proposal: E10R-AI-2 Contract-Doc Verification After Review Fixes

Date: 2026-07-05
Trigger: E10R-AI-2 from `_bmad-output/implementation-artifacts/sprint-status.yaml`
Mode: Batch
Status: approved and applied
Approval: Administrator explicitly approved the E10R-AI-2 correction on 2026-07-05 at 12:42:40 +02:00.

## 1. Issue Summary

The Epic 10 retrospective found a documentation process gap: implementation review fixes can change
behavior after the story's initial documentation pass, leaving contract documents aligned with the
pre-review design rather than the final implementation.

Concrete evidence came from Story 10.5. The Testing host contract still described the older
delimiter-aware redaction behavior until the retrospective verification pass corrected it to match the
post-review structural JSON DOM redaction plus whole-payload tenant/user replacement behavior.

The requested correction is to add contract-doc verification to post-story or post-epic sweeps whenever
review fixes change implementation behavior.

## 2. Impact Analysis

Epic impact:

- Epic 10 remains done. No implementation story is reopened.
- The open Epic 10 retrospective action `E10R-AI-2` is closed by strengthening the reusable sweep and
  review-promotion checklists.
- Epic 11 can consume the improved process directly, especially Stories 11.3, 11.6, 11.7, and 11.11-11.14
  where contract docs and implementation behavior are tightly coupled.

Story impact:

- Future story review-to-done promotion now treats behavior-changing review fixes as a documentation drift
  trigger.
- Story records must name the contract docs checked and either update them or record a no-update rationale.
- Historical story records are not mass-rewritten.

Artifact impact:

- `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Technical impact:

- Documentation and BMAD process artifacts only.
- No runtime code, public API baseline, package, generated output, published DocFX page, or submodule change.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- The underlying behavior has already been fixed where drift was found.
- The remaining gap is a reusable process trigger for future review-time behavior changes.
- Rollback does not apply because no implementation needs to be reverted.
- MVP review does not apply because product scope and release-readiness requirements remain unchanged.

Effort estimate: Low.
Risk level: Low.
Scope classification: Minor.

## 4. Detailed Change Proposals

### 4.1 Doc-Drift Sweep Checklist

Artifact: `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md`

OLD:

```md
Use this checklist after any story, review fix, or post-epic sweep changes a public component surface,
route contract, CLI output, diagnostic metadata, generated-output shape, MCP descriptor, or adopter-facing
behavior.
```

NEW:

```md
Use this checklist after any story, review fix, or post-epic sweep changes a public component surface,
route contract, CLI output, diagnostic metadata, generated-output shape, MCP descriptor, or adopter-facing
behavior. Also use it whenever a review fix changes implementation behavior governed by a contract document,
even if the original story tasks already updated documentation before review.
```

Additional checklist rule:

```md
If a review fix changed implementation behavior, verify contract documents explicitly:
- Search `_bmad-output/contracts/**` first for the affected contract family.
- Also check `_bmad-output/project-docs/api-contracts.md`, `docs/reference/**`, and package README files
  when they make current contract claims.
- Update any contract document that still describes the pre-review design.
- If no contract document exists or no update is needed, record the no-update decision and rationale.
```

Rationale: contract docs are the highest-risk stale artifact when review fixes change behavior after the
initial story implementation.

### 4.2 Story Review Reconciliation Checklist

Artifact: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`

OLD:

```md
If the story or review fix changes a public component surface, route contract, CLI output, diagnostic
metadata, or adopter-facing behavior, complete the doc-drift sweep checklist and record the result in the
story evidence.
```

NEW:

```md
If the story or review fix changes a public component surface, route contract, CLI output, diagnostic
metadata, generated-output shape, MCP descriptor, adopter-facing behavior, or any implementation behavior
governed by a contract document, complete the doc-drift sweep checklist and record the result in the story
evidence. Behavior-changing review fixes must explicitly name the contract docs checked and either update
them or record a no-update rationale.
```

Rationale: the promotion checklist is the point where behavior-changing review fixes can be blocked from
moving to done without contract-doc verification.

### 4.3 Sprint Status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 10
    action: "E10R-AI-2: Add contract-doc verification to post-story or post-epic sweeps whenever review fixes change implementation behavior"
    owner: "Technical Writer"
    status: open
```

NEW:

```yaml
  - epic: 10
    action: "E10R-AI-2: Add contract-doc verification to post-story or post-epic sweeps whenever review fixes change implementation behavior"
    owner: "Technical Writer"
    status: done
    closed: "2026-07-05"
    evidence:
      - "_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md requires explicit contract-doc verification when review fixes change implementation behavior."
      - "_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md makes behavior-changing review fixes a doc-drift trigger and requires contract-doc update/no-update evidence."
      - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e10r-ai-2-contract-doc-verification.md records the correct-course decision and handoff."
```

Rationale: closes the action with evidence paths.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | [x] | Trigger is E10R-AI-2 from Epic 10 retrospective follow-through. |
| 1.2 Core problem defined | [x] | Review fixes can change behavior after initial docs updates, leaving contract docs stale. |
| 1.3 Evidence gathered | [x] | Epic 10 retro records the Testing host contract drift after Story 10.5 review fixes. |
| 2.1 Current epic assessed | [x] | Epic 10 remains complete; only retrospective action process closure is needed. |
| 2.2 Epic-level changes checked | [x] | No epic scope change required. |
| 2.3 Remaining epics reviewed | [x] | Epic 11 benefits from the tightened process, especially contract-heavy stories. |
| 2.4 New epic need checked | [N/A] | No new epic needed. |
| 2.5 Priority/order checked | [x] | Close before Epic 11 behavior/contract stories proceed. |
| 3.1 PRD impact assessed | [x] | Supports FR23 and FR25 without changing product scope. |
| 3.2 Architecture impact assessed | [N/A] | No architecture decision or component boundary change. |
| 3.3 UX impact assessed | [N/A] | No UI/UX behavior change. |
| 3.4 Other artifacts assessed | [x] | Sweep checklist, review checklist, sprint status, and proposal updated. |
| 4.1 Direct adjustment evaluated | [x] | Viable, low effort, low risk. |
| 4.2 Rollback evaluated | [N/A] | No implementation rollback needed. |
| 4.3 MVP review evaluated | [N/A] | MVP/v1.0 scope unaffected. |
| 4.4 Path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP change; process action only. |
| 5.5 Handoff plan established | [x] | Technical Writer owns future contract-doc verification in sweeps. |
| 6.1 Checklist reviewed | [x] | All applicable items covered. |
| 6.2 Proposal accuracy checked | [x] | Based on local artifacts on 2026-07-05. |
| 6.3 User approval | [x] | Administrator requested E10R-AI-2 execution. |
| 6.4 Sprint status update | [x] | E10R-AI-2 marked done with evidence. |
| 6.5 Handoff confirmation | [x] | Future review-promotion checks route through the doc-drift checklist. |

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Technical Writer, with Developer agent support during review-promotion sweeps.

Success criteria:

- Behavior-changing review fixes trigger the doc-drift sweep even when the original story already had a
  documentation pass.
- Contract docs under `_bmad-output/contracts/**` are checked first for any affected contract family.
- Story evidence names contract docs checked and records updated documents or no-update rationale.
- E10R-AI-2 is marked done in sprint status with evidence paths.

## 7. Approval and Handoff Log

- 2026-07-05: Administrator requested E10R-AI-2 execution.
- 2026-07-05T12:42:40+02:00: Administrator explicitly approved the proposal.
- 2026-07-05: Proposal applied in-tree.
- Artifacts modified: doc-drift sweep checklist, story review reconciliation checklist, sprint status, and
  this sprint change proposal.
