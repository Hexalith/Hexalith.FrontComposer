---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05.md
status: proposed
approval: pending
scope: Moderate
---

# Sprint Change Proposal - Implementation Readiness Remediation

## Section 1 - Issue Summary

The 2026-07-05 implementation readiness assessment returned **NEEDS WORK**. It found 13 issues across source-artifact completeness, requirements traceability, UX alignment, and Epic 11 story quality.

The two blocking conditions are:

1. No canonical PRD was discoverable under `_bmad-output/planning-artifacts`, so PRD functional and non-functional requirements could not be extracted or traced.
2. Epic 11 is not implementation-ready because its route-contract decision gate conflicts with its own story order, and Stories 11.8, 11.9, and 11.10 are too broad for safe story execution.

The trigger is not a failed implementation story. It is a planning/readiness finding discovered by `bmad-check-implementation-readiness` on 2026-07-05.

Evidence:

- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`
- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md` (draft source-intake stub, created after the readiness report)
- `_bmad-output/project-docs/architecture.md`
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`
- `_bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md`

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the 2026-07-05 readiness assessment, not a story implementation. |
| 1.2 Core problem | [x] | Planning source traceability is incomplete, and Epic 11 has structural story-readiness defects. |
| 1.3 Evidence | [x] | Readiness report gives concrete discovery, traceability, and Epic 11 findings. |
| 2.1 Current epic impact | [x] | Epic 11 cannot start as written. Epics 1-10 remain usable. Epic 9 remains in progress and unaffected. |
| 2.2 Epic-level changes | [!] | Epic 11 needs a pre-epic route decision and child-story splits for 11.8-11.10. |
| 2.3 Future epic impact | [x] | No existing completed epic is invalidated. Epic 11 backlog and action items need correction. |
| 2.4 New/remove epics | [x] | No new epic is required. The work belongs inside Epic 11 plus source-artifact remediation. |
| 2.5 Priority/order | [!] | Epic 11 order must start with the route-contract decision gate before any 11.x create-story work. |
| 3.1 PRD conflicts | [!] | No canonical PRD exists in the configured planning inventory; traceability cannot be proven. |
| 3.2 Architecture conflicts | [!] | Architecture exists in `_bmad-output/project-docs`, but not as a planning-artifact source discoverable by the readiness workflow. |
| 3.3 UX conflicts | [!] | UX requirements are embedded in `epics.md` and change proposals; no standalone UX planning artifact is discoverable. |
| 3.4 Other artifacts | [!] | `sprint-status.yaml` should be updated only after approval; readiness discovery inputs should be made explicit. |
| 4.1 Direct adjustment | Viable | Recommended. Fix planning sources and restructure Epic 11 without changing MVP scope. |
| 4.2 Rollback | Not viable | No recent implementation needs rollback. |
| 4.3 MVP review | Not viable | MVP scope does not need reduction; Epics 1-10 are mostly ready/done. |
| 4.4 Recommended path | [x] | Hybrid direct adjustment: first source-artifact correction, then Epic 11 backlog reorganization. |
| 5.1-5.5 Proposal components | [x] | Captured in this proposal. |
| 6.1-6.2 Final review | [x] | Proposal is internally consistent and actionable. |
| 6.3 Approval | [!] | Pending Administrator approval. |
| 6.4 Sprint status update | [!] | Do not mutate sprint-status until approval. |
| 6.5 Handoff | [x] | Moderate scope, routed to Product Owner / Developer with Architect and PM decision gates. |

### Epic Impact

Epics 1-10 remain valid. The assessment found them mostly implementation-ready and already completed or in progress according to `_bmad-output/implementation-artifacts/sprint-status.yaml`.

Epic 11 requires restructuring before any story creation starts:

- Story 11.7 is currently both a later ordered story and a required pre-epic decision gate.
- Story 11.8 is a major architectural program, not a single story.
- Story 11.9 groups multiple unrelated helper/layering consolidations.
- Story 11.10 explicitly says it is a program, not a story.
- Several late acceptance criteria use Given/Then without an explicit When/action trigger.

### Artifact Impact

PRD:

- Missing from the readiness report's discovered inventory.
- A draft PRD source-intake stub now exists at `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`, but it is incomplete and nested below the one-level readiness pattern.
- Requirements currently live in `epics.md` as a reverse-engineered inventory.
- A canonical PRD must be completed and made discoverable before traceability can be asserted.

Architecture:

- Architecture exists under `_bmad-output/project-docs/architecture.md`.
- The readiness workflow did not discover it because it searches planning artifacts.
- Either a planning-artifact architecture document should be created, or the readiness workflow/configuration should explicitly include `_bmad-output/project-docs`.

UX:

- UX requirements exist as `UX-DR1` through `UX-DR8` inside `epics.md` and are partly grounded in architecture section 4.
- A canonical UX planning artifact should be created to prevent drift.

Sprint status:

- Epic 11 backlog keys currently mirror the oversized stories.
- Sprint status should be adjusted only after this proposal is approved.

### Technical Impact

This proposal does not require immediate source-code changes. It changes the planning path:

- Complete/promote the draft PRD and create or link canonical architecture and UX planning artifacts.
- Update `epics.md` to reference those artifacts instead of relying on a source caveat alone.
- Split/reorder Epic 11 so create-story and dev-story can execute safely.
- Re-run implementation readiness after the planning changes land.

## Section 3 - Recommended Approach

Use **Direct Adjustment with a planning-source gate**.

Rationale:

- The missing PRD is a traceability defect, not a product-scope defect.
- The brownfield requirements inventory already exists in `epics.md`; it should be promoted into a canonical PRD with source citations instead of invented anew.
- Architecture and UX sources already exist, but not in the configured discovery location.
- Epic 11 can be made implementation-ready by extracting one pre-epic decision and splitting three oversized stories.
- No rollback or MVP reduction is justified.

Effort estimate: Medium.

Risk level: Medium until PRD traceability is created; Low-Medium after the planning artifacts and Epic 11 split are applied.

Timeline impact: one planning pass plus a readiness rerun before Epic 11 starts. Epic 9 can continue if it does not depend on Epic 11 route-contract decisions.

## Section 4 - Detailed Change Proposals

### Proposal A - Complete and Promote Canonical PRD Planning Artifact

Artifacts:

- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`
- `_bmad-output/planning-artifacts/prd.md` or another readiness-discoverable canonical PRD path

Current state:

```text
OLD:
The readiness report found no file under _bmad-output/planning-artifacts matching *prd*.md or *prd*/index.md.

A draft PRD source-intake stub now exists at:
_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md

Current source caveat in epics.md:
No authored PRD/Architecture/UX spec exists. Requirements below are REVERSE-ENGINEERED...
```

Proposed state:

```text
NEW:
Complete the draft PRD and promote or mirror it to a readiness-discoverable canonical path, preferably:
_bmad-output/planning-artifacts/prd.md

Required sections:
- Product context
- MVP scope and non-goals
- Functional requirements FR1-FR22
- Non-functional requirements NFR1-NFR14
- Additional requirements AR1-AR12
- UX requirement references UX-DR1-UX-DR8
- Source trace table mapping each requirement to:
  - _bmad-output/project-docs/project-overview.md
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/api-contracts.md
  - _bmad-output/project-docs/data-models.md
  - _bmad-output/project-docs/component-inventory.md
  - _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md
  - relevant sprint-change-proposal files
```

Rationale:

The existing requirements inventory is useful, but it lives inside `epics.md`. The new PRD draft has only source intake and open discovery items, so it is not yet sufficient as the requirements source of truth. Readiness checks need a product-requirements source that is both complete and discoverable so the FR/NFR coverage matrix can compare PRD requirements to epic coverage rather than treating all FRs as epics-only claims.

Acceptance:

- `bmad-check-implementation-readiness` discovers the PRD under the configured planning path.
- FR1-FR22 and NFR1-NFR14 are traceable to PRD entries.
- The PRD explicitly states that it is a brownfield-derived canonical PRD and does not invent requirements beyond the cited sources.

### Proposal B - Create Discoverable Architecture and UX Planning Artifacts

Artifacts:

- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/ux-design.md`

Current state:

```text
OLD:
Architecture source exists at _bmad-output/project-docs/architecture.md.
UX requirements exist inside _bmad-output/planning-artifacts/epics.md and sprint change proposals.
The readiness inventory finds neither architecture nor UX under planning-artifacts.
```

Proposed state:

```text
NEW:
Create planning-artifact source documents:

_bmad-output/planning-artifacts/architecture.md
- declares _bmad-output/project-docs/architecture.md as the canonical architecture source
- summarizes architecture invariants needed by readiness
- links architecture-quality-review-2026-07-04.md for Epic 11 remediation source

_bmad-output/planning-artifacts/ux-design.md
- extracts UX-DR1 through UX-DR8 as the canonical UX planning source
- cites epics.md, architecture.md section 4, and the change proposals that amended UX-DR3 and UX-DR8
- records that distributed UX changes are accepted only when linked here
```

Rationale:

This avoids using `docs/` as scratch space and keeps BMad generated planning output under `_bmad-output/`, while making the readiness workflow's artifact discovery reliable.

Acceptance:

- Readiness discovery includes architecture and UX documents.
- UX-to-PRD and UX-to-architecture alignment can be validated from canonical planning sources.

### Proposal C - Update `epics.md` Source Caveat After PRD Creation

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: frontmatter `sourceNote` and Overview source caveat.

Current text:

```text
OLD:
No authored PRD/Architecture/UX spec exists. Requirements below are
REVERSE-ENGINEERED from the brownfield documentation set generated by
document-project, plus the 2026-06-03 readiness request.
```

Proposed text:

```text
NEW:
Canonical planning sources now exist under _bmad-output/planning-artifacts:
prd.md, architecture.md, ux-design.md, and epics.md. The PRD remains
brownfield-derived from _bmad-output/project-docs plus the 2026-06-03
readiness request, and every requirement must retain a source trace.
Epics consume those canonical requirements instead of serving as the only
requirements inventory.
```

Rationale:

The caveat was honest when no PRD existed, but keeping it after creating the canonical PRD would preserve the traceability warning. The new wording should keep the brownfield provenance without making epics the sole source.

### Proposal D - Extract Epic 11 Route Decision Into Story 11.0

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Epic 11 introduction and Story 11.7.

Current text:

```text
OLD:
Suggested order: 11.1 -> 11.2 -> 11.4 -> 11.3 -> 11.5 -> 11.6 -> 11.7 (decision) -> 11.9 -> 11.10 -> 11.8 (last).

Decision gates:
Story 11.7 ... due at Epic 11 dev kickoff (no 11.x create-story may start before it is decided).
```

Proposed text:

```text
NEW:
Pre-epic gate: Story 11.0 - Command/projection route-contract decision.
No Story 11.1+ create-story work may start until Story 11.0 is done.

Suggested order:
11.0 -> 11.1 -> 11.2 -> 11.4 -> 11.3 -> 11.5 -> 11.6 -> 11.7 -> split 11.9 children -> split 11.10 children -> split 11.8 children.

Story 11.7 becomes the implementation story for the route contract selected in Story 11.0.
```

New story:

```markdown
### Story 11.0: Command/projection route-contract decision gate

As a Product Owner and Architect,
I want the command route family selected before Epic 11 implementation starts,
So that command activation from the palette and empty-state CTA targets real generated pages.

Acceptance Criteria:

**Given** the current route families:
- projection links `/{bc-lower}/{proj-kebab}`
- palette/CTA command links `/domain/{kebab}/{kebab}`
- generated command pages `/commands/{BC}/{TypeName}`
**When** Architect + Product review the route contract,
**Then** they select one canonical command route family and record the decision in a contract artifact or architecture section.

**Given** the route decision is recorded,
**When** Story 11.7 is created,
**Then** it implements only the selected route contract and adds the e2e route-activation pin.

**Given** Story 11.0 is not done,
**When** any Story 11.1+ create-story is requested,
**Then** the request is blocked with the dated owner and decision status.
```

Rationale:

This removes the contradiction identified by the readiness assessment. It keeps the route decision as a true gate while preserving the actual implementation work as a later story.

### Proposal E - Split Story 11.8 Into Decision and Implementation Stories

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 11.8.

Current summary:

```text
OLD:
Story 11.8 combines:
- Contracts.UI package split
- multiple type relocations
- Fluxor action movement
- QueryRequest decomposition
- project-context and architecture updates
- UX-DR correction
- package compatibility planning
```

Proposed replacement:

```text
NEW:
Replace Story 11.8 with smaller stories:

11.8 Contracts kernel split decision and compatibility plan
11.11 Create Contracts.UI assembly and migrate Blazor rendering surface
11.12 Relocate runtime and testing-owned types out of Contracts
11.13 Decompose QueryRequest through the HFC0001 migration path
11.14 Update architecture, project-context, UX trace, and package-compat documentation
```

Rationale:

The existing Story 11.8 mixes decision, API/package architecture, source migration, deprecation strategy, and documentation. Splitting it lowers API-break risk and gives each story an independently reviewable validation lane.

### Proposal F - Split Story 11.9 By Helper Family

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 11.9.

Current summary:

```text
OLD:
Story 11.9 combines shell layering, route/label relocation, observer primitive convergence,
StorageScopeResolver, SnapshotPublisher<T>, fatal exception guards, HydrationState,
FcJson, RoleBodyHelpers escaping, and architecture tests.
```

Proposed replacement:

```text
NEW:
Replace Story 11.9 with smaller stories:

11.9 Shell layering declaration and route/label relocation
11.15 Storage scope and snapshot publisher consolidation
11.16 Fatal exception, hydration, JSON, and generated-literal helper consolidation
```

Each child story must include:

- observable before/after call-site reduction
- focused regression tests or architecture tests
- a validation lane that can fail for that child story alone
```

Rationale:

This keeps maintainability work user-relevant by tying each consolidation to a visible defect class or guarded invariant.

### Proposal G - Split Story 11.10 Into Three Stories Before Create-Story

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 11.10.

Current text:

```text
OLD:
This is a program, not one story - split at create-story time into
(a) mechanical one-type-per-file split,
(b) LoggerMessage migration,
(c) the enforcement/policy-decision story.
```

Proposed replacement:

```text
NEW:
Remove Story 11.10 as an implementation story and replace it with:

11.17 Mechanical one-type-per-file split
11.18 LoggerMessage migration for warnings and hot paths
11.19 Enforcement and policy alignment
```

Rationale:

The current story already admits it is not a story. The split must happen in the backlog, not during create-story, so estimation, acceptance criteria, file lists, and review lanes are coherent before development starts.

### Proposal H - Normalize Acceptance Criteria Before Story Creation

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- future Story 11.x files under `_bmad-output/implementation-artifacts`

Current state:

```text
OLD:
Several late-epic ACs use Given/Then without an explicit When/action trigger.
Examples: Story 11.2 disposal/wire-contract ACs, Story 11.6, Story 11.9, Story 11.10.
```

Proposed rule:

```text
NEW:
Before any Epic 11 story moves to ready-for-dev, normalize every acceptance criterion to:

Given <state or context>
When <user action, system event, command, test, or verification action>
Then <observable result>
```

Rationale:

This reduces review ambiguity and aligns with the readiness assessment's story-quality recommendation.

### Proposal I - Sprint Status Update After Approval

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Current state:

```yaml
epic-11: backlog
11-1-token-lifecycle-and-circuit-safe-eventstore-auth: backlog
11-2-projection-realtime-resilience: backlog
11-3-mcp-cross-request-lifecycle-and-operability: backlog
11-4-security-validation-hardening: backlog
11-5-dead-css-remediation-and-visual-conformance-guards: backlog
11-6-testing-harness-failure-modes: backlog
11-7-route-contract-unification-decision: backlog
11-8-contracts-kernel-split: backlog
11-9-shell-layering-and-duplication-consolidation: backlog
11-10-convention-alignment-program: backlog
```

Proposed state after approval:

```yaml
epic-11: backlog
11-0-route-contract-decision-gate: backlog
11-1-token-lifecycle-and-circuit-safe-eventstore-auth: backlog
11-2-projection-realtime-resilience: backlog
11-3-mcp-cross-request-lifecycle-and-operability: backlog
11-4-security-validation-hardening: backlog
11-5-dead-css-remediation-and-visual-conformance-guards: backlog
11-6-testing-harness-failure-modes: backlog
11-7-route-contract-implementation: backlog
11-8-contracts-kernel-split-decision-and-compatibility-plan: backlog
11-9-shell-layering-declaration-and-route-label-relocation: backlog
11-11-create-contracts-ui-assembly: backlog
11-12-relocate-runtime-and-testing-owned-contracts-types: backlog
11-13-decompose-queryrequest-with-hfc0001-migration: backlog
11-14-update-architecture-context-ux-and-package-compat-docs: backlog
11-15-storage-scope-and-snapshot-publisher-consolidation: backlog
11-16-fatal-hydration-json-and-generated-literal-helper-consolidation: backlog
11-17-mechanical-one-type-per-file-split: backlog
11-18-loggermessage-migration-for-warnings-and-hot-paths: backlog
11-19-enforcement-and-policy-alignment: backlog
epic-11-retrospective: optional
```

Add or update action items:

```yaml
- epic: 11
  action: "E11-AI-1: Complete Story 11.0 route-contract decision before any Story 11.1+ create-story work."
  owner: "Architect + Product"
  assigned: "2026-07-05"
  due: "before Epic 11 dev kickoff"
  status: open

- epic: 11
  action: "E11-AI-3: Complete/promote the draft PRD, create canonical architecture and UX planning artifacts, and rerun readiness before Epic 11 implementation starts."
  owner: "Product Owner + Developer"
  assigned: "2026-07-05"
  due: "before Epic 11 dev kickoff"
  status: open
```

Rationale:

Sprint status is the execution source of truth. It should not continue to advertise known oversized stories after the proposal is approved.

## Section 5 - Implementation Handoff

Scope classification: **Moderate**.

Route to:

- Product Owner / Developer for PRD completion, planning artifact creation, and backlog reorganization.
- Architect + Product for Story 11.0 route-contract decision.
- Architect + PM for the Contracts kernel split decision and compatibility plan.
- Developer agent only after the planning artifacts are created, Epic 11 is split, and readiness is rerun.

Recommended sequence:

1. Approve this proposal.
2. Complete/promote the draft PRD to `_bmad-output/planning-artifacts/prd.md`, then create `architecture.md` and `ux-design.md`.
3. Update `epics.md` source caveat and Epic 11 story structure.
4. Update `sprint-status.yaml` to match the approved Epic 11 backlog.
5. Complete Story 11.0 route-contract decision.
6. Re-run `bmad-check-implementation-readiness`.
7. Start Epic 11 story creation only after readiness no longer reports the route gate or oversized-story blockers.

Success criteria:

- Readiness discovers PRD, architecture, and UX planning artifacts.
- PRD FR/NFR requirements are traceable to Epic coverage.
- Epic 11 has no pre-epic gate/order contradiction.
- Story 11.8, 11.9, and 11.10 are no longer single oversized implementation stories.
- Late Epic 11 acceptance criteria are normalized to Given/When/Then before ready-for-dev.
- Sprint status reflects the approved backlog.

## Section 6 - Approval State

This proposal is pending Administrator approval.

No source code, `epics.md`, or `sprint-status.yaml` changes should be applied from this proposal until approval is explicit.

Approval question:

Do you approve this Sprint Change Proposal for implementation? Answer `yes`, `no`, or `revise`.
