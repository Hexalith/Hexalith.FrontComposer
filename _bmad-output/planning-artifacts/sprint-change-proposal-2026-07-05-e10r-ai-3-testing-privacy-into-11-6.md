---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: E10R-AI-3 Testing evidence privacy lessons into Epic 11.6
status: approved
approval: explicit-user-approval
approved_by: Administrator
approved_at: 2026-07-05T12:42:49+02:00
applied: true
scope: Minor
---

# Sprint Change Proposal - E10R-AI-3 Testing Privacy Into Story 11.6

## Section 1 - Issue Summary

Epic 10 closed Testing evidence redaction work, but its retrospective added `E10R-AI-3`: carry the
privacy lesson into Epic 11.6 before the Testing harness is expanded for rejection, timeout, stall, and
per-request query/page-loader outcomes.

The concrete lesson from Story 10.5 is that privacy evidence must cover identifier-bearing keys, not only
scalar values. The Senior Developer Review found and fixed a tenant/user leak through JSON property names,
including dictionary keys. The Testing host contract also states that Testing evidence must not log raw
external paths in failure messages, but Story 11.6 did not yet make path evidence explicit.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Trigger is Epic 10 retrospective action `E10R-AI-3`, sourced from Story 10.5 review evidence. |
| 1.2 Core problem | [x] | New requirement from retrospective learning: Story 11.6 must preserve Testing evidence privacy while expanding harness behavior. |
| 1.3 Evidence | [x] | Story 10.5 review fixed tenant/user property-name leaks; `tests/test-summary.md` records property-name/dictionary-key coverage; the Testing host contract names external-path safety. |
| 2.1 Current epic impact | [x] | Epic 11 remains viable; Story 11.6 needs one added privacy acceptance criterion. |
| 2.2 Epic-level changes | [x] | No new epic or story is needed. Story 11.6 scope is refined. |
| 2.3 Future epic impact | [x] | Future Story 11.6 create/dev work must include privacy pins when touching evidence-emitting harness paths. |
| 2.4 New/remove epics | [N/A] | No epic added, removed, or redefined. |
| 2.5 Priority/order | [x] | Epic 11 order is unchanged. This is a readiness refinement before Story 11.6 creation. |
| 3.1 PRD conflicts | [x] | PRD FR-22, FR-27, NFR-6, and SM-5 already cover redacted Testing evidence; no PRD text change required. |
| 3.2 Architecture conflicts | [x] | No architecture-layer change. |
| 3.3 UX conflicts | [N/A] | Testing harness privacy does not alter UI/UX specifications. |
| 3.4 Other artifacts | [x] | `sprint-status.yaml` closes `E10R-AI-3` with evidence. |
| 4.1 Direct adjustment | Viable | Recommended and applied. |
| 4.2 Rollback | Not viable | No completed work should be rolled back. |
| 4.3 MVP review | Not viable | MVP/v1 scope remains unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] | Captured in this document. |
| 6.1-6.2 Final review | [x] | Proposal is scoped, actionable, and consistent with FR-22/FR-27. |
| 6.3 Approval | [x] | User-directed correction on 2026-07-05. |
| 6.4 Sprint status update | [x] | `E10R-AI-3` marked done. |
| 6.5 Handoff | [x] | Test Architect / Developer must apply the added Story 11.6 AC during create/dev. |

## Section 3 - Recommended Approach

Use **Direct Adjustment**.

Update Story 11.6, not the PRD or architecture. The product requirements already say the Testing package
must capture redacted evidence and must not expose support-sensitive data. The missing item is story-level
acceptance language that forces the future harness expansion to preserve the exact privacy classes learned
in Story 10.5.

Effort estimate: Low.

Risk level: Low. The change only tightens acceptance criteria before Story 11.6 is created or implemented.

## Section 4 - Detailed Change Proposals

### Proposal A - Amend Story 11.6 Acceptance Criteria

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```markdown
**Given** the shipped Testing surface (currently 2 test files for 11 files),
**When** builders, assertions, or fakes are changed,
**Then** `Builders` / `Assertions` / fakes get direct surface tests and `PublicAPI.Shipped.txt` is updated intentionally.
```

NEW:

```markdown
**Given** the shipped Testing surface (currently 2 test files for 11 files),
**When** builders, assertions, or fakes are changed,
**Then** `Builders` / `Assertions` / fakes get direct surface tests and `PublicAPI.Shipped.txt` is updated intentionally.

**Given** Story 10.5's Testing evidence privacy findings and the Testing host contract,
**When** Story 11.6 changes fake services, per-request callbacks, builders, assertions, or fault/evidence
paths that emit diagnostic or assertion evidence,
**Then** the default Testing lane preserves redaction for configured tenant/user identifiers in JSON values
and property names, including dictionary keys, preserves structural redaction of token/secret/password keyed
values, and proves raw external/local paths are absent or replaced with bounded repository-relative or redacted
markers wherever the harness emits paths.
```

Rationale: Story 11.6 will expand the Testing harness. Its acceptance criteria must prevent that expansion
from weakening Story 10.5's privacy guarantees or ignoring external-path evidence when a harness path emits
paths.

### Proposal B - Close `E10R-AI-3`

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- epic: 10
  action: "E10R-AI-3: Carry Testing evidence privacy lessons into Epic 11.6, including property-name and dictionary-key cases plus external-path evidence where the harness emits paths"
  owner: "Test Architect"
  status: open
```

NEW:

```yaml
- epic: 10
  action: "E10R-AI-3: Carry Testing evidence privacy lessons into Epic 11.6, including property-name and dictionary-key cases plus external-path evidence where the harness emits paths"
  owner: "Test Architect"
  status: done
  closed: "2026-07-05"
  evidence:
    - "_bmad-output/planning-artifacts/epics.md Story 11.6 now requires property-name, dictionary-key, and external/local path evidence coverage when Testing harness changes emit evidence."
    - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-e10r-ai-3-testing-privacy-into-11-6.md records the correct-course analysis and handoff."
```

Rationale: The retrospective action is complete once Story 11.6 carries the privacy lesson and the
correct-course proposal records the handoff.

## Section 5 - Implementation Handoff

Scope classification: **Minor**.

Route to: Test Architect / Developer agent for Story 11.6 create/dev.

Success criteria:

- Story 11.6 create-story includes the added privacy acceptance criterion.
- Story 11.6 implementation preserves default-lane redaction coverage for tenant/user values, property
  names, dictionary keys, token/secret/password keyed values, and external/local path evidence where paths
  are emitted.
- Public API and README/how-to/contract docs are updated only if Story 11.6 changes adopter-facing Testing
  behavior or public surface.

Correct Course workflow complete, Administrator.
