# Sprint Change Proposal: E8-AI-4 Accent Governance Record

Date: 2026-07-05
Trigger: E8-AI-4 - Require the Shell accent-as-thread governance guard in every future chrome/story verification record
Mode: Batch
Status: approved and implemented
Approval: Administrator, 2026-07-05

## 1. Issue Summary

Epic 8 already shipped the Shell accent-as-thread guard in Story 8.2:
`FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`.
The existing `tests/README.md` also states that Shell chrome or visual verification records must name
that lane.

The open gap was operational: `sprint-status.yaml` still showed E8-AI-4 as open, and the reusable
story verification gates did not require future story records to carry the guard result. A future Shell
chrome or visual story could therefore pass review with the guard remaining implicit.

## 2. Impact Analysis

Epic impact:

- Epic 8 remains done.
- No product scope, PRD requirement, UX requirement, or architecture invariant changes.
- The correction is process governance only.

Story impact:

- Future Shell chrome or visual styling/layout stories must include the guard result in the Dev Agent
  Record, test summary, or review handoff.
- Visual stories that do not touch Shell chrome must record a short N/A rationale rather than silently
  omitting the guard.

Artifact impact:

- `tests/README.md` already contained the high-level rule; no text change was required there.
- `.agents/skills/bmad-dev-story/SKILL.md` now makes the guard result or N/A rationale a blocking
  Definition-of-Done check.
- `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md` now includes the
  guard in the reusable evidence template.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` closes E8-AI-4 with evidence links.

Technical impact:

- No production code changes.
- No tests are required beyond artifact inspection because this is a markdown/workflow governance
  correction.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- The guard already exists and passed in Story 8.2.
- The issue is not missing test logic; it is missing verification-record enforcement.
- Updating the dev-story Definition of Done and visual evidence template is the smallest durable way to
  make every future Shell chrome/story verification record name the lane.

Effort estimate: Low.
Risk level: Low.

## 4. Detailed Change Proposals

### 4.1 Dev-Story Verification Gate

Artifact: `.agents/skills/bmad-dev-story/SKILL.md`

OLD:

```md
Confirm Dev Agent Record or test summary includes standardized Test Evidence for each required lane:
...
```

NEW:

```md
If the story changes Shell chrome or visual styling/layout, confirm the Dev Agent Record or test summary
names `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background` with
Passed/Failed/Blocked result, exact blocker/fallback evidence/CI authority, or a short N/A rationale
when the story is visual but does not touch Shell chrome. Treat omission as a blocking
Definition-of-Done failure.
```

Rationale: This turns the tests README policy into an actual story-completion gate.

### 4.2 Visual Component Evidence Template

Artifact: `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`

OLD:

```md
- Accessibility interaction: <keyboard/hover/touch proof or N/A rationale>
- Visual/browser lane: <local result or CI lane + owner + artifact path>
```

NEW:

```md
- Accessibility interaction: <keyboard/hover/touch proof or N/A rationale>
- Shell accent-as-thread guard: <FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background result, blocker/fallback/CI authority, or N/A rationale>
- Visual/browser lane: <local result or CI lane + owner + artifact path>
```

Rationale: Story authors now have a concrete place to record the E8-AI-4 evidence.

### 4.3 Sprint Status Closure

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 8
    action: "E8-AI-4: Require the Shell accent-as-thread governance guard in every future chrome/story verification record"
    owner: "Architect"
    status: open
```

NEW:

```yaml
  - epic: 8
    action: "E8-AI-4: Require the Shell accent-as-thread governance guard in every future chrome/story verification record"
    owner: "Architect"
    status: done
    closed: "2026-07-05"
    evidence:
      - "tests/README.md requires Shell chrome/visual verification records to name `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`."
      - ".agents/skills/bmad-dev-story/SKILL.md makes omission of the guard result or N/A rationale a blocking Definition-of-Done failure for Shell chrome or visual styling/layout stories."
      - "_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md includes the guard in the reusable story evidence template."
```

Rationale: The action item is no longer only stated; it is routed into future story evidence flow.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | [x] | E8-AI-4 from Epic 8 retrospective follow-through. |
| 1.2 Core problem defined | [x] | Existing guard was not required by future story verification gates. |
| 1.3 Evidence gathered | [x] | Story 8.2 guard, tests README policy, sprint status open row, visual evidence checklist, and dev-story completion gate reviewed. |
| 2.1 Current epic assessed | [x] | Epic 8 remains done. |
| 2.2 Epic-level changes checked | [x] | No epic scope change required. |
| 2.3 Remaining epics reviewed | [x] | No future epic invalidated. Future visual/chrome stories inherit the gate. |
| 2.4 New epic need checked | [N/A] | Not needed. |
| 2.5 Priority/order checked | [x] | Applies before future Shell chrome/visual story promotion. |
| 3.1 PRD impact assessed | [N/A] | No PRD change. |
| 3.2 Architecture impact assessed | [x] | Existing accent-as-thread invariant remains unchanged. |
| 3.3 UX impact assessed | [x] | Existing visual evidence gate extended; no UX behavior change. |
| 3.4 Other artifacts assessed | [x] | Dev-story skill, checklist, sprint status, and tests README reviewed. |
| 4.1 Direct adjustment evaluated | [x] | Viable; effort Low, risk Low. |
| 4.2 Rollback evaluated | [N/A] | No code rollback relevant. |
| 4.3 MVP review evaluated | [N/A] | MVP scope unaffected. |
| 4.4 Path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP change; governance action complete. |
| 5.5 Handoff plan established | [x] | Developer/story authors must follow the dev-story DoD; Architect owns future guard policy. |
| 6.1 Checklist reviewed | [x] | All applicable items covered. |
| 6.2 Proposal accuracy checked | [x] | Based on local artifacts available on 2026-07-05. |
| 6.3 User approval | [x] | Direct Administrator request on 2026-07-05. |
| 6.4 Sprint status update | [x] | E8-AI-4 marked done with evidence. |
| 6.5 Handoff confirmation | [x] | Future dev-story verification records carry the requirement. |

## 6. Implementation Handoff

Scope classification: Minor.

Routed to: Developer/story authors and Architect.

Success criteria:

- Future Shell chrome or visual styling/layout stories name the accent-as-thread governance guard in
  Dev Agent Record, test summary, or review handoff evidence.
- Visual stories outside Shell chrome record an explicit N/A rationale.
- `E8-AI-4` remains closed only while the dev-story DoD and visual evidence checklist preserve this
  requirement.

## 7. Approval and Handoff Log

- 2026-07-05: Approved by Administrator.
- Scope classification: Minor.
- Artifacts updated from this proposal: `.agents/skills/bmad-dev-story/SKILL.md`,
  `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`, and
  `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Routed to: Developer/story authors and Architect.
