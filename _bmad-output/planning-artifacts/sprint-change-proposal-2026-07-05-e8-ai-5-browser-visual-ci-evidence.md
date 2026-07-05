# Sprint Change Proposal: E8-AI-5 Browser/Visual CI Evidence Responsibilities

Date: 2026-07-05
Trigger: E8-AI-5 - Track browser/visual evidence gaps as named CI responsibilities with owner, lane, and closure evidence
Mode: Batch
Status: approved and applied
Approval: Administrator, 2026-07-05

## 1. Issue Summary

Epic 8 closed with repeated local blockers for Playwright/Kestrel browser lanes and Windows visual
baseline updates. The story records were honest about the blockers, but the remaining gaps risked
staying as generic caveats rather than being owned by named CI responsibilities.

Existing evidence:

- The Epic 8 retrospective lists E8-AI-5 and states that visual baselines and Playwright browser
  checks need explicit CI evidence or named follow-up rows.
- The July 1 Epic 8 follow-through proposal sketched E8-CI-1 through E8-CI-3.
- `docs/accessibility-verification/README.md` already has a public Named CI Evidence Follow-Ups
  section.
- The actual CI workflow has an `accessibility-visual` Windows job that runs the Playwright specimen
  accessibility/visual gate and uploads `accessibility-visual-artifacts`.
- The Story 8.3 shell-chrome script exists as `npm --prefix tests/e2e run test:fc-shell-chrome`, but
  it is not currently part of the `accessibility-visual` job; it must remain an explicit responsibility
  row until it is wired, executed as a named CI run, or superseded by approved bUnit evidence.

## 2. Impact Analysis

Epic impact:

- Epic 8 remains done.
- No product scope, PRD requirement, UX behavior, or architecture invariant changes.
- E8-AI-5 becomes a process/evidence closure: tracking is established, while individual E8-CI rows
  remain open until their evidence exists.

Story impact:

- Future visual, hover/focus, touch, or visual-baseline stories must cite a named CI responsibility ID
  when a local browser blocker maps to an existing row.
- Stories must name owner, lane, artifact path, and closure evidence rather than saying only that
  Playwright is locally blocked.

Artifact impact:

- New implementation ledger: `_bmad-output/implementation-artifacts/browser-visual-ci-evidence-responsibilities.md`
- Updated reusable checklist: `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`
- Updated sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- No published docs edit is required for this correction because `docs/accessibility-verification/README.md`
  already contains the public Named CI Evidence Follow-Ups section.

Technical impact:

- No production code changes.
- No CI workflow behavior changes in this correction.
- No tests are required beyond artifact inspection because the change is a planning/process record.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- The issue is not a missing product feature; it is missing operational tracking discipline for known
  browser/visual evidence gaps.
- A dedicated ledger gives each gap a stable row ID, owner, lane, artifact path, and closure rule.
- Sprint status can close E8-AI-5 without pretending the individual rows are already complete.

Effort estimate: Low.
Risk level: Low.
Scope classification: Minor.

## 4. Detailed Change Proposals

### 4.1 Browser/Visual CI Responsibility Ledger

Artifact: `_bmad-output/implementation-artifacts/browser-visual-ci-evidence-responsibilities.md`

NEW:

```md
# Browser/Visual CI Evidence Responsibilities

Status: active tracking ledger
Owner: QA Engineer
Source: E8-AI-5, Epic 8 retrospective follow-through
```

Initial rows:

| ID | Source | Owner | Lane / CI responsibility | Closure rule |
| --- | --- | --- | --- | --- |
| E8-CI-1 | Story 8.1 review follow-up | QA Engineer | Windows visual baseline refresh plus `accessibility-visual` CI job | Close with updated win32 baselines or explicit non-update decision, plus passing CI artifact. |
| E8-CI-2 | Story 8.3 review follow-up | QA Engineer | `test:fc-shell-chrome` as named CI/manual CI responsibility | Close with shell-chrome browser artifact or approved supersession by named bUnit evidence. |
| E8-CI-3 | Story 8.7 review follow-up | QA Engineer | `accessibility-visual` CI job running `test:a11y` | Close with passing protected CI artifact for the status icon touch/focus/hover test. |

Rationale: The row IDs turn "browser blocked locally" into auditable responsibilities without
misstating that all browser evidence is already closed.

### 4.2 Visual Component Evidence Checklist

Artifact: `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`

OLD:

```md
If local Playwright/Kestrel is blocked, record the CI lane, owner, and expected artifact path.
```

NEW:

```md
If local Playwright/Kestrel is blocked, record the CI lane, owner, expected artifact path, and named
responsibility ID when one exists.
```

Rationale: Future stories now cite E8-CI row IDs instead of creating anonymous blocker notes.

### 4.3 Sprint Status Closure

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 8
    action: "E8-AI-5: Track browser/visual evidence gaps as named CI responsibilities with owner, lane, and closure evidence"
    owner: "QA Engineer"
    status: open
```

NEW:

```yaml
  - epic: 8
    action: "E8-AI-5: Track browser/visual evidence gaps as named CI responsibilities with owner, lane, and closure evidence"
    owner: "QA Engineer"
    status: done
    closed: "2026-07-05"
    evidence:
      - "_bmad-output/implementation-artifacts/browser-visual-ci-evidence-responsibilities.md names E8-CI-1 through E8-CI-3 with owner, lane/CI responsibility, expected artifact path, closure evidence, and row status."
      - "docs/accessibility-verification/README.md contains the public Named CI Evidence Follow-Ups section for Epic 8 browser/visual gaps."
      - "_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md requires future visual stories with local browser blockers to cite a named responsibility ID, CI lane, owner, and artifact path when one exists."
    closure_rule: "E8-AI-5 is closed because tracking exists; individual E8-CI rows close only with passing CI artifact evidence, an approved non-update decision, or an approved supersession decision."
```

Rationale: E8-AI-5 owns tracking discipline, not the actual execution of each browser lane.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | [x] | Trigger is E8-AI-5 from Epic 8 retrospective follow-through. |
| 1.2 Core problem defined | [x] | Browser/visual blockers were recorded but not fully routed as named CI responsibilities. |
| 1.3 Evidence gathered | [x] | Reviewed Epic 8 retro, July 1 follow-through proposal, sprint status, visual checklist, CI workflow, e2e scripts, Story 8.1/8.3/8.7 evidence. |
| 2.1 Current epic assessed | [x] | Epic 8 remains done. |
| 2.2 Epic-level changes checked | [x] | No Epic 8 scope change required. |
| 2.3 Remaining epics reviewed | [x] | Epic 11 visual-conformance work can reuse the named responsibility pattern. |
| 2.4 New epic need checked | [N/A] | Not needed. |
| 2.5 Priority/order checked | [x] | Applies before future visual/chrome story promotion. |
| 3.1 PRD impact assessed | [N/A] | No PRD requirement change. |
| 3.2 Architecture impact assessed | [x] | No architecture invariant change; supports visual evidence governance. |
| 3.3 UX impact assessed | [x] | UX behavior unchanged; browser/visual proof expectations become explicit. |
| 3.4 Other artifacts assessed | [x] | CI workflow, Playwright scripts, accessibility verification docs, checklist, and sprint status reviewed. |
| 4.1 Direct adjustment evaluated | [x] | Viable; effort Low, risk Low. |
| 4.2 Rollback evaluated | [N/A] | No shipped implementation should be reverted. |
| 4.3 MVP review evaluated | [N/A] | MVP scope unaffected. |
| 4.4 Path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP change; tracking ledger and sprint-status closure applied. |
| 5.5 Handoff plan established | [x] | QA Engineer owns E8-CI rows; future story authors cite row IDs. |
| 6.1 Checklist reviewed | [x] | All applicable sections covered. |
| 6.2 Proposal accuracy checked | [x] | Based on local artifacts on 2026-07-05. |
| 6.3 User approval | [x] | Approved by Administrator on 2026-07-05. |
| 6.4 Sprint status update | [x] | E8-AI-5 marked done with evidence and closure rule. |
| 6.5 Handoff confirmation | [x] | QA Engineer and future story authors have concrete row IDs and closure rules. |

## 6. Implementation Handoff

Scope classification: Minor.

Routed to:

- QA Engineer: maintain E8-CI-1 through E8-CI-3 and close rows only with valid evidence.
- Developer/story authors: cite the named row ID whenever a future visual story hits an existing
  browser/visual blocker.
- Test Architect: keep the visual-component checklist aligned with this ledger.

Success criteria:

- `E8-AI-5` is marked done only as a tracking closure.
- E8-CI rows remain visible and cannot be silently collapsed into "locally blocked."
- Future visual story evidence names row ID, CI lane, owner, artifact path, and closure evidence.

## 7. Approval and Handoff Log

- 2026-07-05: Approved by Administrator.
- 2026-07-05: Proposal applied in-tree.
- Artifacts modified: sprint status and visual evidence checklist.
- Artifact added: browser/visual CI responsibility ledger.
