---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: E11-AI-1 route-contract decision gate
status: applied
approval: user-directed-2026-07-05
scope: Minor
---

# Sprint Change Proposal - E11 Route-Contract Decision

## Section 1 - Issue Summary

Story 11.0 was still open and blocked every Story 11.1+ create-story run. The concrete defect was
that palette and projection empty-state CTA command links targeted `/domain/{kebab}/{kebab}`, while
generated full-page command pages register `/commands/{BoundedContext}/{CommandTypeName}`. No page in
the repo resolves the `/domain/...` command route family today.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Story 11.0 route-contract decision gate. |
| 1.2 Core problem | [x] | Failed approach/contract gap: command activation routes do not match generated command page routes. |
| 1.3 Evidence | [x] | `sprint-status.yaml` E11-AI-1, `epics.md` Story 11.0/11.7, `CommandRouteBuilder`, generated route transform, and existing `/commands/...` tests. |
| 2.1 Current epic impact | [x] | Epic 11 can proceed to Story 11.1+ create-story only after this decision is recorded. |
| 2.2 Epic-level changes | [x] | Story 11.0 changes from open to done; Story 11.7 remains the implementation story. |
| 2.3 Future epic impact | [x] | Stories 11.1-11.6 are unblocked for creation. Stories 11.11-11.14 remain gated by Story 11.8. |
| 2.4 New/remove epics | [N/A] | No epic added or removed. |
| 2.5 Priority/order | [x] | Suggested order remains 11.1 next; Story 11.7 implements the recorded route contract later in order. |
| 3.1 PRD conflicts | [x] | PRD D-3 updated from open to resolved. |
| 3.2 Architecture conflicts | [x] | Contract artifact records the route decision without changing architecture layers. |
| 3.3 UX conflicts | [x] | UX-DR4 command activation must land on real generated pages; Story 11.7 will add the e2e pin. |
| 3.4 Other artifacts | [x] | `sprint-status.yaml` and the E10 retrospective follow-up action are updated. |
| 4.1 Direct adjustment | Viable | Recommended and applied. |
| 4.2 Rollback | Not viable | No implementation rollback is needed. |
| 4.3 MVP review | Not viable | MVP scope is unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] | Captured in this document and the contract artifact. |
| 6.1-6.2 Final review | [x] | Proposal is internally consistent and actionable. |
| 6.3 Approval | [x] | User-directed correction on 2026-07-05. |
| 6.4 Sprint status update | [x] | Story 11.0 and E11-AI-1 marked done. |
| 6.5 Handoff | [x] | Developer can create Story 11.1+; Story 11.7 must implement the contract. |

## Section 3 - Recommended Approach

Use **Direct Adjustment**.

The selected canonical route family is:

```text
/commands/{BoundedContext}/{CommandTypeName}
```

Rationale:

- It is the route family generated command pages already register.
- Existing e2e and generated-command tests already exercise `/commands/Counter/ConfigureCounterCommand`.
- Aligning activation links to generated pages is lower risk than changing emitted page routes to the
  currently unresolved `/domain/...` family.
- Projection routes and EventStore HTTP command endpoints remain untouched.

Effort estimate: Low for the decision record; Medium for Story 11.7 implementation.

Risk level: Low for the decision; Medium for implementation because persisted recent routes may need
transitional compatibility.

## Section 4 - Detailed Change Proposals

### Proposal A - Record FC-ROUTE Contract

Artifact: `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`

OLD:

```text
No canonical command route family is approved. Palette/CTA command links target /domain/...
while generated pages register /commands/...
```

NEW:

```text
Canonical generated command route family:
/commands/{BoundedContext}/{CommandTypeName}

Historical /domain/{bounded-context-kebab}/{command-type-kebab} is non-canonical.
Story 11.7 may add transitional redirect/alias support only if needed for compatibility.
```

Rationale: Story 11.0's acceptance criteria require the route family to be selected and recorded
before downstream Epic 11 story creation.

### Proposal B - Complete Story 11.0 And Unblock Story 11.1+

Artifacts:

- `_bmad-output/implementation-artifacts/11-0-command-projection-route-contract-decision-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
11-0-route-contract-decision-gate: backlog
E11-AI-1 status: open
```

NEW:

```yaml
11-0-route-contract-decision-gate: done
E11-AI-1 status: done
```

Rationale: The decision gate is complete. Story 11.7 still owns implementation, but Story 11.1+
create-story work is no longer blocked by the route-family decision.

### Proposal C - Update PRD And Epic Planning References

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`
- `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
D-3: Open Story 11.0 gate; no default route is approved.
Story 11.0 blocks all Story 11.1+ create-story work.
```

NEW:

```text
D-3: Resolved 2026-07-05. Canonical generated command route family is
/commands/{BoundedContext}/{CommandTypeName}; contract recorded in
_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md.
Story 11.0 is done and unblocks Story 11.1+ create-story work.
```

Rationale: The planning sources must no longer report the gate as open after the contract is
recorded.

## Section 5 - Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent for Story 11.1+ create-story work and later Story 11.7 implementation.

Success criteria:

- Story 11.1+ creation may proceed.
- Story 11.7 cites the FC-ROUTE contract and updates palette/CTA command activation to
  `/commands/{BoundedContext}/{CommandTypeName}`.
- Story 11.7 adds an e2e pin proving command activation lands on an existing generated command page.
