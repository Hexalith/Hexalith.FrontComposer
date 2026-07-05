---
title: Sprint Change Proposal - PRD-AI-1 Open Question and Assumption Disposition
status: approved
date: 2026-07-05
owner: Product Owner
---

# Sprint Change Proposal: PRD-AI-1

## 1. Issue Summary

The post-correction readiness report left PRD-AI-1 open: resolve, route, or explicitly accept PRD open questions and assumptions, while promoting the PRD status only after Product approval.

The canonical PRD already resolved most readiness questions through later decisions, but one stale gate remained: D-4 still treated FC-NIP row identity as open even though Story 9.1 and the FC-NIP contract now approve the payload source. The PRD also listed assumptions without explicit disposition.

## 2. Impact Analysis

Epic impact:

- Epic 9 remains valid. Story 9.1 is the completed FC-NIP decision gate, and Story 9.2 is the implementation evidence gate.
- Epics 10 and 11 remain unchanged.
- No new epic or story is required.

Artifact impact:

- PRD sections 5.0, 5.3, 5.7, 12, and 13 need updates.
- The BMad run PRD copy needs the same updates so it does not drift from the canonical PRD.
- `epics.md` needs a trace note that Story 9.1 is done and Story 9.2 is implementation evidence.
- `sprint-status.yaml` can mark PRD-AI-1 done because all questions are now resolved, routed, or accepted. Product approval promotes the PRD frontmatter to `status: approved-for-v1-readiness`.

Technical impact:

- No source code, package, architecture, UX, or test implementation change is required.

## Checklist Summary

| Checklist area | Status | Notes |
| --- | --- | --- |
| 1. Trigger and context | [x] | Trigger is PRD-AI-1 in `sprint-status.yaml`; evidence comes from the 2026-07-05 readiness follow-up, current PRD D-4/D-9, Story 9.1, and the FC-NIP contract. |
| 2. Epic impact | [x] | Epic 9 remains valid; Story 9.1 is done and Story 9.2 remains the implementation evidence gate. No epic reorder or new epic required. |
| 3. Artifact conflicts | [x] | PRD and BMad PRD copy had stale FC-NIP and assumption dispositions. Epics needed a trace note. No architecture/UX conflict found. |
| 4. Path forward | [x] | Direct Adjustment selected. Rollback and MVP review are not applicable. |
| 5. Proposal components | [x] | Issue, impact, recommendation, edit proposals, and handoff are captured in this file. |
| 6. Final review / approval | [x] | Product approval was supplied on 2026-07-05; D-9 is resolved and PRD status is promoted. |

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Make a scoped planning-document update:

- Resolve D-4 to the approved FC-NIP payload source.
- Resolve D-9 with Product approval and promote PRD status to `approved-for-v1-readiness`.
- Explicitly accept A1 and A2 with their disposition.
- Mark PRD-AI-1 done in sprint status because the action item is complete and final PRD approval is resolved.

Effort: Low.
Risk: Low.
Timeline impact: None.

## 4. Detailed Change Proposals

PRD:

OLD:

```markdown
| D-4 | FC-NIP row identity payload source | Product + Architecture | Open Story 9.1 gate; automatic row marking remains disabled unless payload evidence exists. | Blocks Story 9.2 producer/consumer wiring. |
```

NEW:

```markdown
| D-4 | FC-NIP row identity payload source | Product + Architecture | Resolved 2026-07-05: approved source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains lifecycle/status by `MessageId`, not row identity. Contract: `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`. | Story 9.2 implements and proves runtime metadata plus producer/consumer behavior; no remaining decision gate. |
```

PRD status:

OLD:

```yaml
status: draft
```

NEW:

```yaml
status: approved-for-v1-readiness
```

Rationale: Product approval was supplied on 2026-07-05, so the PRD status is promoted without implying v1.0 release publication approval.

Assumptions:

- A1 is accepted as a v1.0 requirement and routed to FR-22 / Story 11.6.
- A2 is accepted as the v1.0 product-form-factor assumption and validated through SM-1 and SM-2.

Epics:

- Add a Story 9.1 decision-status note so Epic 9 no longer appears decision-blocked.

Sprint status:

- Mark PRD-AI-1 `done`; D-9 is resolved by Product approval.

## 5. Implementation Handoff

Scope classification: Minor.

Handoff:

- Product Owner: no further PRD-AI-1 action; D-9 is resolved.
- Developer / planning maintainer: use the approved PRD as the v1 readiness planning source.

Success criteria:

- PRD open questions are resolved or routed.
- PRD assumptions carry explicit dispositions.
- PRD status is `approved-for-v1-readiness`.
- PRD-AI-1 no longer appears as an open follow-up.
